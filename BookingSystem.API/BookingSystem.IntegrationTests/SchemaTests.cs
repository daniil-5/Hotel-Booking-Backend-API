using Dapper;
using Npgsql;

namespace BookingSystem.IntegrationTests;

/// <summary>
/// Интеграционные тесты схемы базы данных: ограничения целостности,
/// триггеры, хранимые процедуры и функции (средний уровень пирамиды).
/// Каждый тест выполняется в собственной транзакции и откатывается,
/// что обеспечивает полную изоляцию тестов.
/// </summary>
[Collection("database")]
public class SchemaTests : IDisposable
{
    private readonly DatabaseFixture _db;
    private readonly NpgsqlConnection _conn;
    private readonly NpgsqlTransaction _tx;

    public SchemaTests(DatabaseFixture db)
    {
        _db = db;
        _conn = db.Open();
        _tx = _conn.BeginTransaction();
    }

    public void Dispose()
    {
        _tx.Rollback();
        _conn.Dispose();
    }

    private int Exec(string sql, object? param = null) => _conn.Execute(sql, param, _tx);
    private T Scalar<T>(string sql, object? param = null) => _conn.ExecuteScalar<T>(sql, param, _tx);
    private T Single<T>(string sql, object? param = null) => _conn.QuerySingle<T>(sql, param, _tx);
    private IEnumerable<T> Query<T>(string sql, object? param = null) => _conn.Query<T>(sql, param, _tx);
    private void Call(string sql, object? param = null) => _conn.Execute(sql, param, _tx);

    /// Ожидаемая ошибка СУБД: выполняется в savepoint, чтобы транзакция
    /// теста осталась работоспособной после исключения.
    private PostgresException ExpectPgError(string sql, object? param = null)
    {
        Exec("SAVEPOINT expect_error");
        var ex = Assert.Throws<PostgresException>(() => Exec(sql, param));
        Exec("ROLLBACK TO SAVEPOINT expect_error");
        return ex;
    }

    // ---------- ограничения целостности ----------

    [Fact]
    public void Roles_AreSeeded()
    {
        var roles = Query<(int, string)>("SELECT id, role_name FROM role ORDER BY id");
        Assert.Equal(new[] { (1, "Guest"), (2, "Manager"), (3, "Admin") }, roles);
    }

    [Fact]
    public void RatingCheck_ConstrainsRange()
    {
        var ex = ExpectPgError("UPDATE hotels SET rating = 7 WHERE id = 1");
        Assert.Equal("23514", ex.SqlState); // check_violation
    }

    [Fact]
    public void RoomPricing_IsUniquePerDateAndType()
    {
        var ex = ExpectPgError("INSERT INTO room_pricings (room_type_id, date, price) VALUES (1, CURRENT_DATE + 10, 999)");
        Assert.Equal("23505", ex.SqlState); // unique_violation
    }

    [Fact]
    public void ExcludeConstraint_RejectsOverlappingActiveBooking()
    {
        Exec("INSERT INTO bookings (user_id, hotel_id, room_type_id, check_in_date, check_out_date, guest_count, total_price, status) " +
             "VALUES (1, 1, 1, CURRENT_DATE + 5, CURRENT_DATE + 10, 1, 500, 1)");

        var ex = ExpectPgError(
            "INSERT INTO bookings (user_id, hotel_id, room_type_id, check_in_date, check_out_date, guest_count, total_price, status) " +
            "VALUES (2, 1, 1, CURRENT_DATE + 8, CURRENT_DATE + 12, 1, 400, 1)");
        Assert.Equal("23P01", ex.SqlState); // exclusion_violation
    }

    [Fact]
    public void CancelledBooking_DoesNotBlockNewBooking()
    {
        Exec("INSERT INTO bookings (user_id, hotel_id, room_type_id, check_in_date, check_out_date, guest_count, total_price, status) " +
             "VALUES (1, 1, 2, CURRENT_DATE + 5, CURRENT_DATE + 9, 1, 800, 4)"); // отменено

        Exec("INSERT INTO bookings (user_id, hotel_id, room_type_id, check_in_date, check_out_date, guest_count, total_price, status) " +
             "VALUES (2, 1, 2, CURRENT_DATE + 6, CURRENT_DATE + 8, 1, 400, 1)"); // пересекающиеся даты — допустимо
        Assert.True(true);
    }

    // ---------- триггеры ----------

    [Fact]
    public void UpdatedAtTrigger_RefreshesTimestamp()
    {
        var before = Scalar<DateTime>("SELECT updated_at FROM hotels WHERE id = 1");
        Thread.Sleep(50);
        Exec("UPDATE hotels SET name = name WHERE id = 1");
        var after = Scalar<DateTime>("SELECT updated_at FROM hotels WHERE id = 1");

        Assert.True(after > before, $"updated_at должен обновиться: {before:O} -> {after:O}");
    }

    [Fact]
    public void AuditTrigger_LogsBookingLifecycle()
    {
        var id = Single<int>(
            "INSERT INTO bookings (user_id, hotel_id, room_type_id, check_in_date, check_out_date, guest_count, total_price, status) " +
            "VALUES (1, 1, 1, CURRENT_DATE + 40, CURRENT_DATE + 43, 1, 300, 1) RETURNING id");

        Assert.True(Scalar<int>(
            "SELECT count(*) FROM user_action_audit WHERE user_id = 1 AND user_action_type = 'BookingCreated'") >= 1,
            "триггер должен записать BookingCreated");

        Exec("UPDATE bookings SET status = 3 WHERE id = @id", new { id });
        Assert.True(Scalar<int>(
            "SELECT count(*) FROM user_action_audit WHERE user_id = 1 AND user_action_type = 'BookingUpdated'") >= 1,
            "триггер должен записать BookingUpdated при смене статуса");
    }

    [Fact]
    public void MainPhotoTrigger_KeepsSingleMainPhoto()
    {
        Exec("INSERT INTO hotel_photos (hotel_id, url, is_main) VALUES (1, 'u1', TRUE)");
        Exec("INSERT INTO hotel_photos (hotel_id, url, is_main) VALUES (1, 'u2', TRUE)");

        Assert.Equal(1, Scalar<int>(
            "SELECT count(*) FROM hotel_photos WHERE hotel_id = 1 AND is_main = TRUE"));
    }

    // ---------- хранимые процедуры ----------

    [Fact]
    public void SpCreateBooking_CalculatesSeasonalPrice()
    {
        // календарь: +10 и +11 = 150; остальные дни — базовая 100
        Call("CALL sp_create_booking(1, 1, 1, CURRENT_DATE + 10, CURRENT_DATE + 13, 2, NULL)");
        var id = Single<int>(
            "SELECT id FROM bookings WHERE room_type_id = 1 AND check_in_date = CURRENT_DATE + 10");

        Assert.Equal(150 + 150 + 100, Scalar<decimal>("SELECT total_price FROM bookings WHERE id = @id", new { id }));
        Assert.Equal(1, Scalar<int>("SELECT status FROM bookings WHERE id = @id", new { id }));
    }

    [Fact]
    public void SpCreateBooking_RejectsCapacityOverflow()
    {
        var ex = ExpectPgError("CALL sp_create_booking(1, 1, 1, CURRENT_DATE + 60, CURRENT_DATE + 63, 5, NULL)");
        Assert.Contains("местимост", ex.MessageText);
    }

    [Fact]
    public void SpCancelBooking_AllowsOwnerButNotStranger()
    {
        Call("CALL sp_create_booking(2, 2, 2, CURRENT_DATE + 70, CURRENT_DATE + 74, 2, NULL)");
        var own = Single<int>("SELECT id FROM bookings WHERE user_id = 2 AND check_in_date = CURRENT_DATE + 70");
        Call("CALL sp_cancel_booking(@id, 2, FALSE)", new { id = own });
        Assert.Equal(4, Scalar<int>("SELECT status FROM bookings WHERE id = @id", new { id = own }));

        // чужой пользователь (не владелец и не админ) отменять не может
        Call("CALL sp_create_booking(2, 2, 2, CURRENT_DATE + 80, CURRENT_DATE + 84, 2, NULL)");
        var stranger = Single<int>("SELECT id FROM bookings WHERE user_id = 2 AND check_in_date = CURRENT_DATE + 80");
        var ex = ExpectPgError("CALL sp_cancel_booking(@id, 1, FALSE)", new { id = stranger });
        Assert.Contains("прав", ex.MessageText);

        // администратор может
        Call("CALL sp_cancel_booking(@id, 1, TRUE)", new { id = stranger });
        Assert.Equal(4, Scalar<int>("SELECT status FROM bookings WHERE id = @id", new { id = stranger }));
    }

    // ---------- функции ----------

    [Fact]
    public void SearchHotels_FiltersByLocationAndRating()
    {
        var rows = Query<(string, decimal)>(
            "SELECT hotel_name, rating FROM search_hotels('минск', 4.0, NULL, NULL)").ToList();
        Assert.Single(rows);
        Assert.Equal(("Отель Альфа", 4.5m), rows[0]);
    }

    [Fact]
    public void SearchHotels_FiltersByMaxPrice()
    {
        Assert.Equal(1, Scalar<int>("SELECT count(*) FROM search_hotels(NULL, NULL, 150, NULL)"));
    }

    [Fact]
    public void GetHotelStatistics_AggregatesRevenue()
    {
        Exec("INSERT INTO bookings (user_id, hotel_id, room_type_id, check_in_date, check_out_date, guest_count, total_price, status) " +
             "VALUES (1, 1, 1, CURRENT_DATE - 10, CURRENT_DATE - 7, 2, 300, 3)"); // подтверждено

        var row = Single<(string, long, long, decimal)>(
            "SELECT hotel_name, total_bookings, confirmed_bookings, total_revenue " +
            "FROM get_hotel_statistics() WHERE hotel_name = 'Отель Альфа'");
        Assert.Equal("Отель Альфа", row.Item1);
        Assert.True(row.Item2 >= 1);
        Assert.True(row.Item4 >= 300);
    }

    [Fact]
    public void ActiveBookingsView_ShowsOnlyActive()
    {
        Call("CALL sp_create_booking(1, 2, 2, CURRENT_DATE + 90, CURRENT_DATE + 93, 2, NULL)");
        var id = Single<int>("SELECT id FROM bookings WHERE check_in_date = CURRENT_DATE + 90");
        Exec("UPDATE bookings SET status = 4 WHERE id = @id", new { id });

        Assert.Equal(0, Scalar<int>(
            "SELECT count(*) FROM v_active_bookings WHERE check_in_date = CURRENT_DATE + 90"));
    }
}
