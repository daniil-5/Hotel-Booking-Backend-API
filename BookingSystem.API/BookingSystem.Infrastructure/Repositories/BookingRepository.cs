using Dapper;
using BookingSystem.Domain.DTOs.Booking;
using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly DapperDbContext _context;
    public BookingRepository(DapperDbContext context) { _context = context; }

    public async Task<Booking> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Booking>(
            "select * from bookings where id = @id and is_deleted = false", new { id });
    }

    public async Task<bool> HasOverlappingBookingAsync(int roomTypeId, DateTime checkInDate, DateTime checkOutDate, int? excludeBookingId = null)
    {
        using var conn = _context.CreateConnection();
        var sql = @"
            SELECT COUNT(*) > 0
            FROM bookings b
            WHERE b.room_type_id = @roomTypeId
              AND b.is_deleted = false
              AND b.status IN (1, 2, 3)
              AND (@excludeBookingId IS NULL OR b.id <> @excludeBookingId)
              AND daterange(@checkIn::date, @checkOut::date)
                  && daterange(b.check_in_date::date, b.check_out_date::date)";
        return await conn.ExecuteScalarAsync<bool>(sql,
            new { roomTypeId, checkIn = checkInDate.Date, checkOut = checkOutDate.Date, excludeBookingId });
    }

    public async Task<IEnumerable<Booking>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Booking>(
            "select * from bookings where is_deleted = false");
    }

    public async Task<IEnumerable<Booking>> GetAllAsync(System.Linq.Expressions.Expression<Func<Booking, bool>> predicate)
    {
        var allBookings = await GetAllAsync();
        return allBookings.Where(predicate.Compile());
    }

    public Task<Booking> GetByIdAsync(int id, Func<IQueryable<Booking>, IQueryable<Booking>> include)
        => throw new NotSupportedException();

    public Task<IEnumerable<Booking>> GetAllAsync(System.Linq.Expressions.Expression<Func<Booking, bool>> predicate = null, Func<IQueryable<Booking>, IQueryable<Booking>> include = null)
        => throw new NotSupportedException();

    public async Task AddAsync(Booking entity)
    {
        using var conn = _context.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
@"insert into bookings (user_id, room_type_id, hotel_id, check_in_date, check_out_date, guest_count, status, total_price, created_at, is_deleted)
 values (@UserId, @RoomTypeId, @HotelId, @CheckInDate, @CheckOutDate, @GuestCount, @Status, @TotalPrice, @CreatedAt, false)
 returning id", entity);
        entity.Id = id;
    }

    public async Task UpdateAsync(Booking entity)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
@"update bookings
   set check_in_date=@CheckInDate, check_out_date=@CheckOutDate,
       status=@Status, total_price=@TotalPrice, updated_at=@UpdatedAt
 where id=@Id and is_deleted=false", entity);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("update bookings set is_deleted=true, updated_at=now() where id=@id", new { id });
    }

    public async Task<Booking> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<Booking, bool>> predicate)
        => (await GetAllAsync()).FirstOrDefault(predicate.Compile());

    public Task<Booking> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<Booking, bool>> predicate, Func<IQueryable<Booking>, IQueryable<Booking>> include)
        => throw new NotSupportedException();

    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Booking, bool>> predicate = null)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("select count(*) from bookings where is_deleted=false");
    }

    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Booking, bool>> predicate, Func<IQueryable<Booking>, IQueryable<Booking>> include)
        => throw new NotSupportedException();

    public IQueryable<Booking> GetQueryable() => throw new NotSupportedException();

    public async Task<IEnumerable<BookingDetails>> GetBookingsWithDetailsAsync(int? userId = null, int? hotelId = null, int? status = null)

    {

        using var conn = _context.CreateConnection();

        var sql = @"

            SELECT 

                b.id AS Id,

                b.check_in_date AS CheckInDate,

                b.check_out_date AS CheckOutDate,

                b.guest_count AS GuestCount,

                b.status AS Status,

                b.total_price AS TotalPrice,

                b.created_at AS CreatedAt,

                EXTRACT(DAY FROM (b.check_out_date - b.check_in_date)) AS NightCount,

                u.id AS UserId,

                u.username AS Username,

                u.email AS UserEmail,

                u.first_name || ' ' || u.last_name AS FullName,

                u.phone_number AS PhoneNumber,

                h.id AS HotelId,

                h.name AS HotelName,

                h.location AS HotelLocation,

                h.rating AS HotelRating,

                rt.id AS RoomTypeId,

                rt.name AS RoomTypeName,

                rt.capacity AS RoomTypeCapacity,

                rt.area AS RoomArea,

                rt.base_price AS RoomBasePrice,

                (SELECT url FROM hotel_photos WHERE hotel_id = h.id AND is_main = true AND is_deleted = false LIMIT 1) AS MainPhotoUrl

            FROM bookings b

            INNER JOIN users u ON b.user_id = u.id

            INNER JOIN hotels h ON b.hotel_id = h.id

            INNER JOIN room_types rt ON b.room_type_id = rt.id

            WHERE b.is_deleted = false

            AND u.is_deleted = false

            AND h.is_deleted = false

            AND rt.is_deleted = false

            AND (@userId IS NULL OR b.user_id = @userId)

            AND (@hotelId IS NULL OR b.hotel_id = @hotelId)

            AND (@status IS NULL OR b.status = @status)

            ORDER BY b.created_at DESC";

        return await conn.QueryAsync<BookingDetails>(sql, new { userId, hotelId, status });

    }



    public async Task<object> GetUserBookingHistoryAsync(int userId)

    {

        using var conn = _context.CreateConnection();

        var statsSql = @"

            SELECT 

                u.id AS UserId,

                u.username AS Username,

                u.email AS Email,

                u.first_name || ' ' || u.last_name AS FullName,

                COUNT(b.id) AS TotalBookings,

                COUNT(CASE WHEN b.status = 1 THEN 1 END) AS CompletedBookings,

                COUNT(CASE WHEN b.status = 2 THEN 1 END) AS CancelledBookings,

                COALESCE(SUM(b.total_price), 0) AS TotalSpent,

                COALESCE(AVG(b.total_price), 0) AS AverageBookingValue,

                MIN(b.created_at) AS FirstBookingDate,

                MAX(b.created_at) AS LastBookingDate,

                COUNT(DISTINCT b.hotel_id) AS UniqueHotelsVisited

            FROM users u

            LEFT JOIN bookings b ON u.id = b.user_id AND b.is_deleted = false

            WHERE u.id = @userId AND u.is_deleted = false

            GROUP BY u.id, u.username, u.email, u.first_name, u.last_name";

        var history = await conn.QuerySingleOrDefaultAsync<dynamic>(statsSql, new { userId });

        if (history == null) return null;



        var bookingsSql = @"

            SELECT 

                b.id AS Id,

                b.check_in_date AS CheckInDate,

                b.check_out_date AS CheckOutDate,

                b.status AS Status,

                b.total_price AS TotalPrice,

                h.name AS HotelName,

                h.location AS Location,

                rt.name AS RoomTypeName

            FROM bookings b

            INNER JOIN hotels h ON b.hotel_id = h.id

            INNER JOIN room_types rt ON b.room_type_id = rt.id

            WHERE b.user_id = @userId AND b.is_deleted = false

            ORDER BY b.created_at DESC

            LIMIT 10";

        history.RecentBookings = (await conn.QueryAsync<dynamic>(bookingsSql, new { userId })).ToList();



        var locationsSql = @"

            SELECT 

                h.location AS Location,

                COUNT(b.id) AS BookingCount,

                SUM(b.total_price) AS TotalSpent

            FROM bookings b

            INNER JOIN hotels h ON b.hotel_id = h.id

            WHERE b.user_id = @userId AND b.is_deleted = false

            GROUP BY h.location

            ORDER BY BookingCount DESC

            LIMIT 5";

        history.FavoriteLocations = (await conn.QueryAsync<dynamic>(locationsSql, new { userId })).ToList();



        return history;

    }

    public async Task<IEnumerable<BookingSystem.Domain.DTOs.Booking.ActiveBookingDetailsDto>> GetActiveBookingsWithDetailsAsync()
    {
        using var conn = _context.CreateConnection();

        var sql = @"
            SELECT
                b.id AS Id,
                b.check_in_date AS CheckInDate,
                b.check_out_date AS CheckOutDate,
                b.guest_count AS GuestCount,
                b.total_price AS TotalPrice,
                CASE b.status
                    WHEN 0 THEN 'Pending'
                    WHEN 1 THEN 'Confirmed'
                    WHEN 2 THEN 'Cancelled'
                    ELSE 'Unknown'
                END AS Status,
                b.created_at AS CreatedAt,
                u.id AS UserId,
                u.username AS Username,
                u.email AS UserEmail,
                h.id AS HotelId,
                h.name AS HotelName,
                h.location AS HotelLocation,
                rt.id AS RoomTypeId,
                rt.name AS RoomTypeName,
                rt.capacity AS RoomTypeCapacity,
                rt.base_price AS RoomTypeBasePrice
            FROM bookings b
            INNER JOIN users u ON b.user_id = u.id
            INNER JOIN hotels h ON b.hotel_id = h.id
            INNER JOIN room_types rt ON b.room_type_id = rt.id
            WHERE b.is_deleted = FALSE
              AND b.status IN (0, 1) -- Pending (0) or Confirmed (1)
            ORDER BY b.check_in_date ASC;";

        return await conn.QueryAsync<BookingSystem.Domain.DTOs.Booking.ActiveBookingDetailsDto>(sql);
    }

}

