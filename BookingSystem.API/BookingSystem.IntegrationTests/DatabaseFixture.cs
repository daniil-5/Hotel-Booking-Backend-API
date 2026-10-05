using Dapper;
using Npgsql;

namespace BookingSystem.IntegrationTests;

/// <summary>
/// Фикстура интеграционных тестов: поднимает отдельную базу booking_tests,
/// применяет схему (db/01_schema.sql) и наполняет минимальными
/// детерминированными данными.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public const string DefaultConnection =
        "Host=localhost;Port=5433;Username=my_user;Password=1111;Database=postgres";

    private string _master = Environment.GetEnvironmentVariable("BOOKING_TEST_CONNECTION") ?? DefaultConnection;
    private string _testDb = "booking_tests";

    public string ConnectionString => _master.Replace("Database=postgres", $"Database={_testDb}");

    public async Task InitializeAsync()
    {
        // та же глобальная настройка Dapper, что и в Program.cs приложения
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        await using (var conn = new NpgsqlConnection(_master))
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync(
                $"DROP DATABASE IF EXISTS {_testDb} WITH (FORCE); CREATE DATABASE {_testDb};");
        }

        var schemaSql = File.ReadAllText(
            FindFile("01_schema.sql", AppContext.BaseDirectory));

        await using (var conn = new NpgsqlConnection(ConnectionString))
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync(schemaSql);

            // Минимальные детерминированные данные
            await conn.ExecuteAsync("""
                INSERT INTO users (id, username, email, password_hash, role) VALUES
                (1, 'alice', 'alice@test', 'hash', 1),
                (2, 'bob',   'bob@test',   'hash', 1);
                SELECT setval('users_id_seq', 2);

                INSERT INTO hotels (id, name, location, rating, base_price) VALUES
                (1, 'Отель Альфа', 'Минск', 4.5, 100),
                (2, 'Отель Бета',  'Гомель', 3.9, 80);
                SELECT setval('hotels_id_seq', 2);

                INSERT INTO room_types (id, name, description, capacity, area, hotel_id, base_price) VALUES
                (1, 'Стандарт', '', 2, 20, 1, 100),
                (2, 'Люкс',     '', 4, 40, 2, 200);
                SELECT setval('room_types_id_seq', 2);

                INSERT INTO room_pricings (room_type_id, date, price) VALUES
                (1, CURRENT_DATE + 10, 150),
                (1, CURRENT_DATE + 11, 150);

                INSERT INTO hotel_photos (hotel_id, url, public_id, is_main) VALUES
                (1, 'https://res.cloudinary.com/test/h_main.jpg', 'test/h_main', TRUE),
                (1, 'https://res.cloudinary.com/test/h_second.jpg', 'test/h_second', FALSE);
                """);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public NpgsqlConnection Open()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    /// Поиск файла схемы: из каталога тестов поднимаемся к db/
    private static string FindFile(string name, string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, name)))
        {
            dir = dir.Parent;
            if (dir is not null && Directory.Exists(Path.Combine(dir.FullName, "db")))
            {
                var candidate = Path.Combine(dir.FullName, "db", name);
                if (File.Exists(candidate)) return candidate;
            }
        }
        throw new FileNotFoundException($"Не найден {name}");
    }
}

[CollectionDefinition("database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
