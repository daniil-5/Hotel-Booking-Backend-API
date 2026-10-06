using Dapper;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace BookingSystem.ApiTests;

/// <summary>
/// Фабрика приложения в памяти: пересоздаёт базу Booking_db по схеме и сиду
/// из db/, поднимает API с теми же Redis и MongoDB (задаются окружением).
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public const string MasterConnection =
        "Host=localhost;Port=5433;Username=my_user;Password=1111;Database=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var master = Environment.GetEnvironmentVariable("BOOKING_TEST_CONNECTION") ?? MasterConnection;
        var app = master.Replace("Database=postgres", "Database=booking_db");

        ResetDatabase(master);

        ResetAuthLimits(
            Environment.GetEnvironmentVariable("TEST_REDIS") ?? "localhost:6379");

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", app);
        builder.UseSetting("ConnectionStrings:Redis",
            Environment.GetEnvironmentVariable("TEST_REDIS") ?? "localhost:6379");
        builder.UseSetting("ConnectionStrings:MongoDbConnection",
            Environment.GetEnvironmentVariable("TEST_MONGO") ?? "mongodb://admin:password123@localhost:27017");
        builder.UseSetting("Logging:LogLevel:Default", "Warning");

        // appsettings.json исключён из репозитория .gitignore-ом — тестовое
        // окружение задаёт все необходимые настройки явно
        builder.UseSetting("JWT:Secret", "ci_test_secret_key_at_least_32_chars_long");
        builder.UseSetting("JWT:Issuer", "BookingSystem");
        builder.UseSetting("JWT:Audience", "BookingSystemUsers");
        builder.UseSetting("JWT:DurationInDays", "7");
        builder.UseSetting("MongoDbSettings:DatabaseName", "BookingSystem_Logs");
        builder.UseSetting("MongoDbSettings:UserActionsCollectionName", "UserActionAudits");
        builder.UseSetting("MongoDbSettings:LogsTtlDays", "1");
        builder.UseSetting("CloudinarySettings:CloudName", "test-cloud");
        builder.UseSetting("CloudinarySettings:ApiKey", "000000000000000");
        builder.UseSetting("CloudinarySettings:ApiSecret", "test-secret");
    }

    /// Счётчики неудачных входов из прошлых прогонов не должны блокировать тесты
    private static void ResetAuthLimits(string redis)
    {
        try
        {
            using var mux = StackExchange.Redis.ConnectionMultiplexer.Connect(redis);
            var db = mux.GetDatabase();
            foreach (var ep in mux.GetEndPoints())
            {
                foreach (var key in mux.GetServer(ep).Keys(pattern: "auth:*").Concat(mux.GetServer(ep).Keys(pattern: "hotel*")))
                {
                    db.KeyDelete(key);
                }
            }
        }
        catch
        {
            // Redis может быть недоступен — тесты с 429 выявят проблему явно
        }
    }

    private static void ResetDatabase(string master)
    {
        using (var conn = new NpgsqlConnection(master))
        {
            conn.Open();
            conn.Execute("DROP DATABASE IF EXISTS booking_db WITH (FORCE); CREATE DATABASE booking_db;");
        }

        var schema = File.ReadAllText(FindFile("01_schema.sql"));
        var seed = File.ReadAllText(FindFile("02_seed.sql"));
        using (var conn = new NpgsqlConnection(master.Replace("Database=postgres", "Database=booking_db")))
        {
            conn.Open();
            conn.Execute(schema);
            conn.Execute(seed);
        }
    }

    private static string FindFile(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "db", name);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"Не найден db/{name}");
    }
}
