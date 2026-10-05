// HotelRepository.cs

using BookingSystem.Domain.DTOs.Hotel;
using BookingSystem.Domain.DTOs.RoomType;
using Dapper;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.Infrastructure.Repositories;

public class HotelRepository : IHotelRepository
{
    private readonly DapperDbContext _context;
    public HotelRepository(DapperDbContext context) { _context = context; }

    public async Task<Hotel> GetByIdAsync(int id) => await GetHotelWithDetailsAsync(id);

    public async Task<IEnumerable<Hotel>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Hotel>("select * from hotels where is_deleted=false");
    }

    public async Task<IEnumerable<Hotel>> GetAllAsync(System.Linq.Expressions.Expression<Func<Hotel, bool>> predicate)
    {
        var allHotels = await GetAllAsync();
        return allHotels.Where(predicate.Compile());
    }

    public Task<Hotel> GetByIdAsync(int id, Func<IQueryable<Hotel>, IQueryable<Hotel>> include)
        => throw new NotSupportedException();

    public Task<IEnumerable<Hotel>> GetAllAsync(System.Linq.Expressions.Expression<Func<Hotel, bool>> predicate = null, Func<IQueryable<Hotel>, IQueryable<Hotel>> include = null)
        => throw new NotSupportedException();

    public async Task AddAsync(Hotel entity)
    {
        using var conn = _context.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
@"insert into hotels (name, description, location, rating, base_price, created_at, is_deleted)
 values (@Name, @Description, @Location, @Rating, @BasePrice, @CreatedAt, false)
 returning id", entity);
        entity.Id = id;
    }

    public async Task UpdateAsync(Hotel entity)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
@"update hotels
   set name=@Name, description=@Description, location=@Location, rating=@Rating, base_price=@BasePrice, updated_at=@UpdatedAt
 where id=@Id and is_deleted=false", entity);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("update hotels set is_deleted=true, updated_at=now() where id=@id", new { id });
    }

    public async Task<Hotel> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<Hotel, bool>> predicate)
        => (await GetAllAsync()).FirstOrDefault(predicate.Compile());

    public Task<Hotel> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<Hotel, bool>> predicate, Func<IQueryable<Hotel>, IQueryable<Hotel>> include)
        => throw new NotSupportedException();

    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Hotel, bool>> predicate = null)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("select count(*) from hotels where is_deleted=false");
    }

    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Hotel, bool>> predicate, Func<IQueryable<Hotel>, IQueryable<Hotel>> include)
        => throw new NotSupportedException();

    public IQueryable<Hotel> GetQueryable() => throw new NotSupportedException();

    public async Task<(IEnumerable<Hotel> hotels, int totalCount)> SearchHotelsAsync(
        string name = null, string location = null,
        decimal? minRating = null, decimal? maxRating = null,
        decimal? minPrice = null, decimal? maxPrice = null,
        int pageNumber = 1, int pageSize = 10, string sortBy = "Rating")
    {
        using var conn = _context.CreateConnection();
        var offset = (pageNumber - 1) * pageSize;

        var where = " where h.is_deleted = false";
        if (!string.IsNullOrWhiteSpace(name))
            where += " and h.name ILIKE @name";
        if (!string.IsNullOrWhiteSpace(location))
            where += " and h.location ILIKE @location";
        if (minRating != null)
            where += " and h.rating >= @minRating";
        if (maxRating != null)
            where += " and h.rating <= @maxRating";

        var join = "";
        if (minPrice != null || maxPrice != null || sortBy.Equals("Price", StringComparison.OrdinalIgnoreCase))
        {
            join = @" left join room_types rt on rt.hotel_id = h.id and rt.is_deleted = false";
            if (minPrice != null)
                where += " and rt.base_price >= @minPrice";
            if (maxPrice != null)
                where += " and rt.base_price <= @maxPrice";
        }

        var orderBy = sortBy switch
        {
            "Price" => " min(rt.base_price) asc",
            "Name" => " h.name asc",
            _ => " h.rating desc, h.id asc",
        };

        var sql = $@"select distinct on (h.id) h.*
                     from hotels h{join}{where}
                     order by h.id";
        var countSql = $@"select count(*) from (
                             select distinct h.id
                             from hotels h{join}{where}
                         ) t";

        var items = await conn.QueryAsync<Hotel>(sql, new
        {
            name = name != null ? $"%{name}%" : null,
            location = location != null ? $"%{location}%" : null,
            minRating,
            maxRating,
            minPrice,
            maxPrice,
            pageSize,
            offset,
        });
        var total = await conn.ExecuteScalarAsync<int>(countSql, new
        {
            name = name != null ? $"%{name}%" : null,
            location = location != null ? $"%{location}%" : null,
            minRating,
            maxRating,
            minPrice,
            maxPrice,
        });

        // сортировка и пагинация в памяти: выборка ограничена условиями фильтрации
        IOrderedEnumerable<Hotel> sorted;
        if (sortBy.Equals("Price", StringComparison.OrdinalIgnoreCase))
        {
            // «от X руб./ночь» — минимальная базовая цена типа номера отеля
            var minPrices = await conn.QueryAsync<(int HotelId, decimal MinPrice)>(
                "select rt.hotel_id, min(rt.base_price) " +
                "from room_types rt where rt.is_deleted = false " +
                "and rt.hotel_id = ANY(@ids) group by rt.hotel_id",
                new { ids = items.Select(h => h.Id).ToArray() });
            var byHotel = minPrices.ToDictionary(r => r.HotelId, r => r.MinPrice);
            sorted = items.OrderBy(h => byHotel.TryGetValue(h.Id, out var p) ? p : decimal.MaxValue);
        }
        else if (sortBy.Equals("Name", StringComparison.OrdinalIgnoreCase))
        {
            sorted = items.OrderBy(h => h.Name, StringComparer.Ordinal);
        }
        else
        {
            sorted = items.OrderByDescending(h => h.Rating);
        }

        var page = sorted.Skip(offset).Take(pageSize).ToList();

        // фотографии для страницы результатов (главная — первой)
        var pageIds = page.Select(h => h.Id).ToArray();
        if (pageIds.Length > 0)
        {
            var photos = await conn.QueryAsync<HotelPhoto>(
                "select * from hotel_photos where hotel_id = ANY(@ids) and is_deleted = false " +
                "order by is_main desc, id",
                new { ids = pageIds });
            var byHotel = photos.GroupBy(p => p.HotelId)
                .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var h in page)
            {
                if (byHotel.TryGetValue(h.Id, out var list))
                    h.Photos = list;
            }
        }

        return (page, total);
    }

    public async Task<IEnumerable<Hotel>> GetHotelsWithDetailsAsync(
        System.Linq.Expressions.Expression<Func<Hotel, bool>> filter = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var (hotels, _) = await SearchHotelsAsync(pageNumber: pageNumber, pageSize: pageSize);
        return hotels;
    }

    public async Task<Hotel> GetHotelWithDetailsAsync(int id)
    {
        using var conn = _context.CreateConnection();

        var hotel = await conn.QuerySingleOrDefaultAsync<Hotel>(
            "select * from hotels where id=@id and is_deleted=false", new { id });
        if (hotel == null) return null;

        var roomTypes = await conn.QueryAsync<RoomType>(
            "select * from room_types where hotel_id=@hotelId and is_deleted=false",
            new { hotelId = id });
        hotel.RoomTypes = roomTypes.ToList();

        var photos = await conn.QueryAsync<HotelPhoto>(
            "select * from hotel_photos where hotel_id=@hotelId and is_deleted=false",
            new { hotelId = id });
        hotel.Photos = photos.ToList();

        return hotel;
    }

    public Task<IEnumerable<Hotel>> SearchHotelsByAvailabilityAsync(
        string location, DateTime checkIn, DateTime checkOut, int guests,
        int pageNumber = 1, int pageSize = 10)
    {
        return Task.FromResult<IEnumerable<Hotel>>(Array.Empty<Hotel>());
    }

    public async Task<IEnumerable<HotelStatistics>> GetHotelsWithStatisticsAsync()
    {
        using var conn = _context.CreateConnection();

        var sql = @"
        SELECT 
            h.id AS HotelId,
            h.name AS HotelName,
            h.location AS Location,
            h.rating AS Rating,
            h.base_price AS BasePrice,
            COUNT(DISTINCT b.id) AS TotalBookings,
            COUNT(DISTINCT CASE WHEN b.status = 1 THEN b.id END) AS ConfirmedBookings,
            COUNT(DISTINCT CASE WHEN b.status = 2 THEN b.id END) AS CancelledBookings,
            COALESCE(SUM(b.total_price), 0) AS TotalRevenue,
            COALESCE(AVG(b.total_price), 0) AS AverageBookingPrice,
            COUNT(DISTINCT rt.id) AS TotalRoomTypes,
            COUNT(DISTINCT hp.id) AS TotalPhotos
        FROM hotels h
        LEFT JOIN room_types rt ON h.id = rt.hotel_id AND rt.is_deleted = false
        LEFT JOIN bookings b ON h.id = b.hotel_id AND b.is_deleted = false
        LEFT JOIN hotel_photos hp ON h.id = hp.hotel_id AND hp.is_deleted = false
        WHERE h.is_deleted = false
        GROUP BY h.id, h.name, h.location, h.rating, h.base_price
        ORDER BY TotalRevenue DESC";

        return await conn.QueryAsync<HotelStatistics>(sql);
    }

    public async Task<IEnumerable<HotelAvailability>> SearchAvailableHotelsAsync(
        string location,
        DateTime checkIn,
        DateTime checkOut,
        int guestCount)
    {
        using var conn = _context.CreateConnection();

        var sql = @"
        SELECT 
            h.id AS HotelId,
            h.name AS HotelName,
            h.location AS Location,
            h.rating AS Rating,
            h.description AS Description,
            rt.id AS RoomTypeId,
            rt.name AS RoomTypeName,
            rt.capacity AS Capacity,
            rt.base_price AS Price,
            rt.area AS Area,
            COUNT(DISTINCT hp.id) AS PhotoCount,
            (
                SELECT COUNT(*)
                FROM bookings b2
                WHERE b2.room_type_id = rt.id
                AND b2.is_deleted = false
                AND b2.status IN (0, 1)
                AND (
                    (b2.check_in_date <= @checkIn AND b2.check_out_date >= @checkIn)
                    OR (b2.check_in_date <= @checkOut AND b2.check_out_date >= @checkOut)
                    OR (b2.check_in_date >= @checkIn AND b2.check_out_date <= @checkOut)
                )
            ) AS BookedCount
        FROM hotels h
        INNER JOIN room_types rt ON h.id = rt.hotel_id AND rt.is_deleted = false
        LEFT JOIN hotel_photos hp ON h.id = hp.hotel_id AND hp.is_deleted = false
        WHERE h.is_deleted = false
        AND h.location ILIKE @location
        AND rt.capacity >= @guestCount
        GROUP BY h.id, h.name, h.location, h.rating, h.description, 
                 rt.id, rt.name, rt.capacity, rt.base_price, rt.area
        HAVING COUNT(DISTINCT hp.id) > 0
        ORDER BY h.rating DESC, rt.base_price ASC";

        return await conn.QueryAsync<HotelAvailability>(sql, new
        {
            location = $"%{location}%",
            checkIn,
            checkOut,
            guestCount
        });
    }

    public async Task<IEnumerable<HotelRanking>> GetHotelsRankedByLocationAsync()
    {
        using var conn = _context.CreateConnection();

        var sql = @"
        WITH hotel_booking_stats AS (
            SELECT 
                h.id,
                h.name,
                h.location,
                h.rating,
                h.base_price,
                COUNT(b.id) AS booking_count,
                SUM(b.total_price) AS total_revenue
            FROM hotels h
            LEFT JOIN bookings b ON h.id = b.hotel_id AND b.is_deleted = false
            WHERE h.is_deleted = false
            GROUP BY h.id, h.name, h.location, h.rating, h.base_price
        )
        SELECT 
            id AS HotelId,
            name AS HotelName,
            location AS Location,
            rating AS Rating,
            base_price AS BasePrice,
            booking_count AS BookingCount,
            total_revenue AS TotalRevenue,
            RANK() OVER (PARTITION BY location ORDER BY booking_count DESC) AS RankInLocation,
            RANK() OVER (ORDER BY total_revenue DESC) AS OverallRevenueRank,
            ROUND(
                (booking_count::numeric / NULLIF(SUM(booking_count) OVER (PARTITION BY location), 0)) * 100, 
                2
            ) AS MarketShareInLocation
        FROM hotel_booking_stats
        ORDER BY location, RankInLocation";

        return await conn.QueryAsync<HotelRanking>(sql);
    }

    public async Task<HotelPerformanceReport> GetHotelPerformanceReportAsync(int hotelId)
    {
        using var conn = _context.CreateConnection();

        var hotelSql = @"
        SELECT 
            h.id AS HotelId,
            h.name AS HotelName,
            h.location AS Location,
            h.rating AS Rating,
            h.base_price AS BasePrice,
            COUNT(DISTINCT b.id) AS TotalBookings,
            COALESCE(SUM(b.total_price), 0) AS TotalRevenue,
            COALESCE(AVG(b.total_price), 0) AS AverageBookingValue,
            COUNT(DISTINCT b.user_id) AS UniqueCustomers,
            COUNT(DISTINCT rt.id) AS TotalRoomTypes
        FROM hotels h
        LEFT JOIN room_types rt ON h.id = rt.hotel_id AND rt.is_deleted = false
        LEFT JOIN bookings b ON h.id = b.hotel_id AND b.is_deleted = false
        WHERE h.id = @hotelId AND h.is_deleted = false
        GROUP BY h.id, h.name, h.location, h.rating, h.base_price";

        var hotel = await conn.QuerySingleOrDefaultAsync<HotelPerformanceReport>(hotelSql, new { hotelId });

        if (hotel == null) return null;

        var roomTypesSql = @"
        SELECT 
            rt.id AS RoomTypeId,
            rt.name AS RoomTypeName,
            rt.capacity AS Capacity,
            rt.base_price AS BasePrice,
            COUNT(b.id) AS BookingCount,
            COALESCE(SUM(b.total_price), 0) AS Revenue,
            COALESCE(AVG(b.total_price), 0) AS AveragePrice,
            COUNT(CASE WHEN b.status = 1 THEN 1 END) AS ConfirmedCount,
            COUNT(CASE WHEN b.status = 2 THEN 1 END) AS CancelledCount,
            ROUND(
                (COUNT(CASE WHEN b.status = 2 THEN 1 END)::numeric / 
                 NULLIF(COUNT(b.id), 0)) * 100, 
                2
            ) AS CancellationRate
        FROM room_types rt
        LEFT JOIN bookings b ON rt.id = b.room_type_id AND b.is_deleted = false
        WHERE rt.hotel_id = @hotelId AND rt.is_deleted = false
        GROUP BY rt.id, rt.name, rt.capacity, rt.base_price
        ORDER BY Revenue DESC";

        hotel.RoomTypePerformance = (await conn.QueryAsync<RoomTypePerformance>(roomTypesSql, new { hotelId })).ToList();

        return hotel;
    }

    public async Task<IEnumerable<MonthlyBookingTrend>> GetMonthlyBookingTrendsAsync(int? hotelId = null, int months = 12)
    {
        using var conn = _context.CreateConnection();

        var sql = @"
        SELECT 
            h.id AS HotelId,
            h.name AS HotelName,
            DATE_TRUNC('month', b.created_at) AS Month,
            COUNT(b.id) AS BookingCount,
            SUM(b.total_price) AS Revenue,
            AVG(b.total_price) AS AverageBookingValue,
            COUNT(DISTINCT b.user_id) AS UniqueCustomers,
            SUM(b.guest_count) AS TotalGuests
        FROM hotels h
        INNER JOIN bookings b ON h.id = b.hotel_id
        WHERE h.is_deleted = false 
        AND b.is_deleted = false
        AND b.created_at >= CURRENT_TIMESTAMP - make_interval(months => @months)
        AND (@hotelId IS NULL OR h.id = @hotelId)
        GROUP BY h.id, h.name, DATE_TRUNC('month', b.created_at)
        ORDER BY h.id, Month DESC";

        return await conn.QueryAsync<MonthlyBookingTrend>(sql, new { hotelId, months });
    }

    public async Task<IEnumerable<Hotel>> GetHotelsOrderedByRatingAndNameAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Hotel>("SELECT * FROM hotels WHERE is_deleted = FALSE ORDER BY rating DESC, name ASC");
    }

    public async Task<IEnumerable<Hotel>> GetPremiumHotelsAsync()
    {
        using var conn = _context.CreateConnection();
        var sql = @"
        SELECT h.*
        FROM hotels h
        WHERE h.is_deleted = FALSE
          AND h.base_price > (SELECT AVG(base_price) FROM hotels WHERE is_deleted = FALSE)
        ORDER BY h.base_price DESC;";
        return await conn.QueryAsync<Hotel>(sql);
    }
}
