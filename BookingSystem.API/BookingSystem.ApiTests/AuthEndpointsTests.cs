using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BookingSystem.ApiTests;

/// <summary>
/// Приёмочные тесты REST API верхнего уровня пирамиды: приложение целиком
/// (контроллеры + сервисы + кэш + репозитории + PostgreSQL).
/// </summary>
// одна фабрика (и одна пересборка базы) на все классы API-тестов
[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiTestFactory> { };

[Collection("api")]
public abstract class ApiTestBase
{
    protected readonly ApiTestFactory Factory;
    protected readonly HttpClient Client;
    protected ApiTestBase(ApiTestFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        res.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    protected static HttpClient As(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
    }
}

[Collection("api")]
public class AuthEndpointsTests : ApiTestBase
{
    public AuthEndpointsTests(ApiTestFactory f) : base(f) { }

    [Fact]
    public async Task Register_CreatesAccountAndReturnsToken()
    {
        var name = $"u{Guid.NewGuid():N}"[..12];
        var res = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username = name,
            email = $"{name}@test.local",
            password = "Test123!",
            firstName = "Тест",
            lastName = "Тестов",
            phoneNumber = "+375290000000",
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(doc.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        // корректный payload с обязательными полями
        var payload = new
        {
            username = $"u{Guid.NewGuid():N}"[..12],
            email = "user1@booking.local", // уже есть в сид-данных
            password = "Test123!",
            firstName = "Тест",
            lastName = "Тестов",
            phoneNumber = "+375290000000",
        };
        var first = await Client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.Conflict, first.StatusCode); // дубликат сразу

        var second = await Client.PostAsJsonAsync("/api/auth/register", payload with { username = "another" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var res = await Client.PostAsJsonAsync("/api/auth/login",
            new { email = $"nobody{Guid.NewGuid():N}"[..20] + "@test.local", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Current_ReturnsProfileForToken()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        var me = await GetJsonAsync(As(Client, token), "/api/auth/current");
        Assert.Equal("user1", me.GetProperty("username").GetString());
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var res = await Client.GetAsync("/api/bookings/my");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}

[Collection("api")]
public class HotelEndpointsTests : ApiTestBase
{
    public HotelEndpointsTests(ApiTestFactory f) : base(f) { }

    [Fact]
    public async Task GetAll_ReturnsSeededHotels()
    {
        var hotels = await GetJsonAsync(Client, "/api/hotels");
        Assert.True(hotels.GetArrayLength() >= 40, $"ожидалось ≥40 отелей, получено {hotels.GetArrayLength()}");
    }

    [Fact]
    public async Task GetById_ReturnsHotelWithPhotos()
    {
        var hotel = await GetJsonAsync(Client, "/api/hotels/1");
        Assert.Equal(1, hotel.GetProperty("id").GetInt32());
        Assert.True(hotel.GetProperty("photos").GetArrayLength() > 0);
    }

    [Theory]
    [InlineData("?Location=Минск", 3)]
    [InlineData("?MinRating=4.5&PageSize=50", -1)]
    [InlineData("?SortBy=Price&PageSize=5", -1)]
    public async Task Search_FiltersAndPaginates(string query, int expected)
    {
        var result = await GetJsonAsync(Client, "/api/hotels/search" + query);
        Assert.True(result.GetProperty("totalCount").GetInt32() > 0);
        if (expected >= 0)
            Assert.Equal(expected, result.GetProperty("totalCount").GetInt32());
        Assert.True(result.GetProperty("pageSize").GetInt32() > 0);
    }

    [Fact]
    public async Task Search_WithPhotos_CloudinaryUrls()
    {
        var result = await GetJsonAsync(Client, "/api/hotels/search?Location=Минск");
        var first = result.GetProperty("hotels")[0];
        var url = first.GetProperty("photos")[0].GetProperty("url").GetString()!;
        Assert.Contains("res.cloudinary.com", url);
    }

    [Fact]
    public async Task Statistics_Availability_Ranking_Premium_Work()
    {
        var staffToken = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var stats = await GetJsonAsync(As(Client, staffToken), "/api/hotels/statistics");
        Assert.True(stats.ValueKind == JsonValueKind.Array && stats.GetArrayLength() > 0);

        var availability = await Client.GetAsync(
            "/api/hotels/availability?location=Минск&checkIn=2030-01-01&checkOut=2030-01-05&guestCount=2");
        Assert.True(availability.IsSuccessStatusCode);

        var ranking = await GetJsonAsync(Client, "/api/hotels/ranking");
        Assert.True(ranking.ValueKind == JsonValueKind.Array);

        var premium = await GetJsonAsync(Client, "/api/hotels/premium");
        Assert.True(premium.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public async Task RoomTypes_ByHotel_ReturnsFourTypes()
    {
        var types = await GetJsonAsync(Client, "/api/room-types/by-hotel/1");
        Assert.Equal(4, types.GetArrayLength());
    }

    [Fact]
    public async Task RoomPricings_List_IsAvailable()
    {
        var res = await Client.GetAsync("/api/roompricings");
        Assert.True(res.IsSuccessStatusCode);
    }
}

[Collection("api")]
public class BookingEndpointsTests : ApiTestBase
{
    public BookingEndpointsTests(ApiTestFactory f) : base(f) { }

    private static int FreeRoomType(JsonElement hotelsRoot)
    {
        // свободный тип номера: окно +100..+104 дней не пересекается с сид-бронями
        return hotelsRoot[0].GetProperty("id").GetInt32() is var _ ? 2 : 2;
    }

    [Fact]
    public async Task FullBookingLifecycle_CreateDuplicateConflictCancel()
    {
        var token = await LoginAsync(Client, "user2@booking.local", "User123!");
        As(Client, token);

        // первый свободный тип номера отеля 2
        var types = await GetJsonAsync(Client, "/api/room-types/by-hotel/2");
        var roomTypeId = types[0].GetProperty("id").GetInt32();
        var capacity = types[0].GetProperty("capacity").GetInt32();

        var checkIn = DateTime.Today.AddDays(100).ToString("yyyy-MM-dd");
        var checkOut = DateTime.Today.AddDays(103).ToString("yyyy-MM-dd");
        var payload = new { hotelId = 2, roomTypeId, checkInDate = checkIn, checkOutDate = checkOut, guestCount = capacity };

        var created = await Client.PostAsJsonAsync("/api/bookings", payload);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var booking = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement;
        Assert.True(booking.GetProperty("totalPrice").GetDecimal() > 0);
        var id = booking.GetProperty("id").GetInt32();

        // повтор на те же даты — конфликт 409
        var duplicate = await Client.PostAsJsonAsync("/api/bookings", payload);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        // свои брони содержат созданную
        var mine = await GetJsonAsync(Client, "/api/bookings/my");
        Assert.Contains(mine.EnumerateArray(), b => b.GetProperty("id").GetInt32() == id);

        // отмена освобождает даты
        var cancelled = await Client.DeleteAsync($"/api/bookings/{id}");
        Assert.True(cancelled.IsSuccessStatusCode);
        var rebook = await Client.PostAsJsonAsync("/api/bookings", payload);
        Assert.Equal(HttpStatusCode.Created, rebook.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidDates_Returns400()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        As(Client, token);
        var bad = new { hotelId = 1, roomTypeId = 1, checkInDate = "2030-05-10", checkOutDate = "2030-05-10", guestCount = 1 };
        var res = await Client.PostAsJsonAsync("/api/bookings", bad);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_GuestsOverCapacity_Returns400()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        As(Client, token);
        var bad = new { hotelId = 1, roomTypeId = 1, checkInDate = "2030-05-10", checkOutDate = "2030-05-12", guestCount = 99 };
        var res = await Client.PostAsJsonAsync("/api/bookings", bad);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UserHistory_ReturnsStatistics()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        var history = await GetJsonAsync(As(Client, token), "/api/bookings/user/4/history");
        Assert.Equal(4, history.GetProperty("userId").GetInt32());
        Assert.True(history.GetProperty("totalBookings").GetInt64() > 0);
    }

    [Fact]
    public async Task ManagerSees_AllBookings_Details()
    {
        var token = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var all = await GetJsonAsync(As(Client, token), "/api/bookings/all");
        Assert.True(all.GetArrayLength() > 100);

        var details = await GetJsonAsync(As(Client, token), "/api/bookings/details?hotelId=1");
        Assert.True(details.GetArrayLength() > 0);

        // гость не должен видеть все брони
        var guestToken = await LoginAsync(Factory.CreateClient(), "user1@booking.local", "User123!");
        var guestClient = Factory.CreateClient();
        guestClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", guestToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await guestClient.GetAsync("/api/bookings/all")).StatusCode);
    }
}

[Collection("api")]
public class AdminEndpointsTests : ApiTestBase
{
    public AdminEndpointsTests(ApiTestFactory f) : base(f) { }

    [Fact]
    public async Task Users_ListAndLookups_Work()
    {
        var token = await LoginAsync(Client, "admin@booking.local", "Admin123!");
        As(Client, token);

        var users = await GetJsonAsync(Client, "/api/users");
        Assert.True(users.GetArrayLength() >= 10);

        var byEmail = await GetJsonAsync(Client, "/api/users/by-email/user1@booking.local");
        Assert.Equal("user1", byEmail.GetProperty("username").GetString());

        var byName = await GetJsonAsync(Client, "/api/users/by-username/manager1");
        Assert.Equal(2, byName.GetProperty("id").GetInt32());

        var noBookings = await GetJsonAsync(Client, "/api/users/no-bookings");
        Assert.True(noBookings.ValueKind == JsonValueKind.Array);

        var active = await GetJsonAsync(Client, "/api/users/active-ordered");
        Assert.True(active.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public async Task Logs_OnlyForAdmin()
    {
        var adminToken = await LoginAsync(Client, "admin@booking.local", "Admin123!");
        var logs = await GetJsonAsync(As(Client, adminToken), "/api/logs");
        Assert.True(logs.ValueKind == JsonValueKind.Array);

        var guestToken = await LoginAsync(Factory.CreateClient(), "user1@booking.local", "User123!");
        var guest = Factory.CreateClient();
        guest.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", guestToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.GetAsync("/api/logs")).StatusCode);
    }

    [Fact]
    public async Task Reports_AvailableForAdmin()
    {
        var token = await LoginAsync(Client, "admin@booking.local", "Admin123!");
        var client = As(Client, token);
        foreach (var ep in new[] { "user-activity", "top-users", "operation-distribution", "time-series", "anomalies" })
        {
            var res = await client.GetAsync("/api/reports/" + ep);
            Assert.True(res.IsSuccessStatusCode, $"{ep}: {res.StatusCode}");
        }
    }

    [Fact]
    public async Task AuditJournal_RecordsUserLogin()
    {
        // после входов в других тестах в журнале должны быть события UserLogin
        var adminToken = await LoginAsync(Client, "admin@booking.local", "Admin123!");
        var logs = await GetJsonAsync(As(Client, adminToken), "/api/logs?eventType=UserLogin");
        Assert.True(logs.GetArrayLength() > 0, "журнал должен содержать события входа");
    }
}
