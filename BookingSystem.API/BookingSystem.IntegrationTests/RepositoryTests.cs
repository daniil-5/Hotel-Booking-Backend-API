using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Repositories;
using Dapper;
using Npgsql;

namespace BookingSystem.IntegrationTests;

/// <summary>
/// Интеграционные тесты репозиториев (Dapper-слой) поверх реальной
/// базы данных: поиск отелей с фотографиями, проверка пересечений
/// бронирований, пагинация журнала аудита.
/// </summary>
[Collection("database")]
public class RepositoryTests
{
    private readonly DatabaseFixture _db;
    private readonly DapperDbContext _context;

    public RepositoryTests(DatabaseFixture db)
    {
        _db = db;
        var ds = new NpgsqlDataSourceBuilder(db.ConnectionString);
        _context = new DapperDbContext(ds.Build());
    }

    private NpgsqlConnection Open()
    {
        var conn = new NpgsqlConnection(_db.ConnectionString);
        conn.Open();
        return conn;
    }

    // ---------- поиск отелей ----------

    [Fact]
    public async Task HotelRepository_Search_LoadsPhotosForResults()
    {
        var repo = new HotelRepository(_context);
        var (hotels, total) = await repo.SearchHotelsAsync(location: "Минск");

        Assert.Equal(1, total);
        var hotel = hotels.Single();
        Assert.Equal("Отель Альфа", hotel.Name);

        // главная фотография — первой, ссылки в полном виде
        Assert.Equal(2, hotel.Photos.Count);
        Assert.Equal("https://res.cloudinary.com/test/h_main.jpg", hotel.Photos.First().Url);
        Assert.True(hotel.Photos.First().IsMain);
    }

    [Fact]
    public async Task HotelRepository_Search_FiltersByPriceAndSorts()
    {
        var repo = new HotelRepository(_context);

        var (_, total) = await repo.SearchHotelsAsync(maxPrice: 150m);
        Assert.Equal(1, total); // Стандарт отеля Альфа за 100

        var (byPrice, _) = await repo.SearchHotelsAsync(sortBy: "Price");
        Assert.Equal("Отель Альфа", byPrice.First().Name); // дешёвый первым

        var (byRating, _) = await repo.SearchHotelsAsync(sortBy: "Rating");
        Assert.Equal("Отель Альфа", byRating.First().Name); // 4.5 > 3.9
    }

    // ---------- пересечения бронирований ----------

    [Fact]
    public async Task BookingRepository_HasOverlappingBooking_DetectsAndExcludes()
    {
        using var conn = Open();
        var id = conn.ExecuteScalar<int>(
            "INSERT INTO bookings (user_id, hotel_id, room_type_id, check_in_date, check_out_date, guest_count, total_price, status) " +
            "VALUES (1, 1, 1, CURRENT_DATE + 30, CURRENT_DATE + 34, 1, 400, 1) RETURNING id");
        try
        {
            var repo = new BookingRepository(_context);
            var today = DateTime.Today;

            // пересечение с активной бронью
            Assert.True(await repo.HasOverlappingBookingAsync(1, today.AddDays(31), today.AddDays(33)));
            // без пересечения
            Assert.False(await repo.HasOverlappingBookingAsync(1, today.AddDays(40), today.AddDays(44)));
            // своя бронь исключается из проверки (смена дат существующей брони)
            Assert.False(await repo.HasOverlappingBookingAsync(1, today.AddDays(30), today.AddDays(34), excludeBookingId: id));
        }
        finally
        {
            conn.Execute($"DELETE FROM bookings WHERE id = {id}");
        }
    }

    // ---------- журнал аудита ----------

    [Fact]
    public async Task AuditRepository_AddAndPaginate_Works()
    {
        var repo = new UserActionAuditRepository(_context);
        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(new UserActionAudit
            {
                UserId = 1,
                UserActionType = UserActionType.UserLogin,
                IsSuccess = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        var (items, total) = await repo.GetWithPaginationAsync(pageNumber: 1, pageSize: 3);

        Assert.Equal(3, items.Count());          // размер страницы
        Assert.True(total >= 5);                 // второй result set (count) читается
        Assert.Contains(items, a => a.UserActionType == UserActionType.UserLogin);

        using var conn = Open();
        conn.Execute("DELETE FROM user_action_audit WHERE user_action_type = 'UserLogin'");
    }

    [Fact]
    public async Task AuditRepository_FilterByUserAndType()
    {
        var repo = new UserActionAuditRepository(_context);
        await repo.AddAsync(new UserActionAudit
        {
            UserId = 2,
            UserActionType = UserActionType.ChangePassword,
            IsSuccess = false,
            CreatedAt = DateTime.UtcNow,
        });

        var rows = await repo.GetActionsByUserAndTypeAsync(2, UserActionType.ChangePassword);

        Assert.Contains(rows, a => a.IsSuccess == false);
        Assert.DoesNotContain(rows, a => a.UserActionType != UserActionType.ChangePassword);

        using var conn = Open();
        conn.Execute("DELETE FROM user_action_audit WHERE user_action_type = 'ChangePassword'");
    }
}
