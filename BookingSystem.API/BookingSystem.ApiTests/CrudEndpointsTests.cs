using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BookingSystem.ApiTests;

/// <summary>
/// CRUD-сценарии персонала: отели, типы номеров, календарь цен, фотографии,
/// пользователи, изменение и отмена бронирований.
/// </summary>
[Collection("api")]
public class CrudEndpointsTests : ApiTestBase
{
    public CrudEndpointsTests(ApiTestFactory f) : base(f) { }

    private async Task<HttpClient> AsManagerAsync()
    {
        var token = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        return As(Client, token);
    }

    private async Task<HttpClient> AsAdminAsync()
    {
        var token = await LoginAsync(Client, "admin@booking.local", "Admin123!");
        return As(Client, token);
    }

    // ---------- отели ----------

    [Fact]
    public async Task HotelCrud_FullCycle()
    {
        var staff = await AsManagerAsync();

        var created = await staff.PostAsJsonAsync("/api/hotels", new
        {
            name = "Тест-Отель CRUD",
            description = "Описание",
            location = "Минск",
            rating = 4.2,
            basePrice = 120m,
        });
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var hotel = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement;
        var id = hotel.GetProperty("id").GetInt32();

        var updated = await staff.PutAsJsonAsync($"/api/hotels/{id}", new
        {
            id,
            name = "Тест-Отель CRUD+",
            description = "Описание+",
            location = "Минск",
            rating = 4.6,
            basePrice = 130m,
        });
        Assert.True(updated.IsSuccessStatusCode);

        // гость видит обновление
        var fetched = await GetJsonAsync(Client, $"/api/hotels/{id}");
        Assert.Equal("Тест-Отель CRUD+", fetched.GetProperty("name").GetString());

        var deleted = await staff.DeleteAsync($"/api/hotels/{id}");
        Assert.True(deleted.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/hotels/{id}")).StatusCode);
    }

    [Fact]
    public async Task HotelCreate_ForbiddenForGuest()
    {
        var guestToken = await LoginAsync(Client, "user1@booking.local", "User123!");
        var res = await As(Client, guestToken).PostAsJsonAsync("/api/hotels", new
        { name = "X", description = "Y", location = "Z", rating = 5, basePrice = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ---------- типы номеров и календарь цен ----------

    [Fact]
    public async Task RoomTypeAndPricingCrud()
    {
        var staff = await AsManagerAsync();

        var typeRes = await staff.PostAsJsonAsync("/api/room-types", new
        {
            name = "Тест-Люкс",
            description = "просторный",
            capacity = 3,
            area = 40,
            hotelId = 1,
            basePrice = 250m,
        });
        Assert.True(typeRes.IsSuccessStatusCode, await typeRes.Content.ReadAsStringAsync());
        var type = JsonDocument.Parse(await typeRes.Content.ReadAsStringAsync()).RootElement;
        var typeId = type.GetProperty("id").GetInt32();

        var putType = await staff.PutAsJsonAsync($"/api/room-types/{typeId}", new
        {
            id = typeId,
            name = "Тест-Люкс+",
            description = "просторный",
            capacity = 4,
            area = 45,
            hotelId = 1,
            basePrice = 270m,
        });
        Assert.True(putType.IsSuccessStatusCode);

        var priceRes = await staff.PostAsJsonAsync("/api/roompricings", new
        {
            date = $"{DateTime.Today.AddYears(1):yyyy-MM-dd}T00:00:00",
            price = 333m,
            roomTypeId = typeId,
        });
        Assert.True(priceRes.IsSuccessStatusCode, await priceRes.Content.ReadAsStringAsync());
        var priceId = JsonDocument.Parse(await priceRes.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var prices = await GetJsonAsync(Client, "/api/roompricings");
        Assert.Contains(prices.EnumerateArray(), p => p.GetProperty("id").GetInt32() == priceId);

        Assert.True((await staff.DeleteAsync($"/api/roompricings/{priceId}")).IsSuccessStatusCode);
        Assert.True((await staff.DeleteAsync($"/api/room-types/{typeId}")).IsSuccessStatusCode);
    }

    // ---------- фотографии ----------

    [Fact]
    public async Task Photos_ListAndGetById()
    {
        var staff = await AsManagerAsync();
        var photos = await GetJsonAsync(staff, "/api/hotelphotos/hotel/1");
        Assert.True(photos.GetArrayLength() >= 2);
        var id = photos[0].GetProperty("id").GetInt32();

        var one = await GetJsonAsync(Client, $"/api/hotelphotos/{id}");
        Assert.Equal(id, one.GetProperty("id").GetInt32());

        var all = await GetJsonAsync(staff, "/api/hotelphotos");
        Assert.True(all.GetArrayLength() > 0);
    }

    // ---------- пользователи ----------

    [Fact]
    public async Task UserUpdate_ByAdmin()
    {
        var admin = await AsAdminAsync();
        var res = await admin.PutAsJsonAsync("/api/users/5", new
        {
            id = 5,
            username = "user2",
            email = "user2@booking.local",
            firstName = "Дмитрий",
            lastName = "Себелев",
            phoneNumber = "+375299999999",
        });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());

        var user = await GetJsonAsync(admin, "/api/users/5");
        Assert.Equal("+375299999999", user.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task ChangePassword_AndRelogin()
    {
        var token = await LoginAsync(Client, "user7@booking.local", "User123!");
        var client = As(Client, token);

        var changed = await client.PostAsJsonAsync("/api/users/change-password", new
        { userId = 10, currentPassword = "User123!", newPassword = "NewPass123!", confirmPassword = "NewPass123!" });
        Assert.True(changed.IsSuccessStatusCode, await changed.Content.ReadAsStringAsync());

        // старый пароль больше не подходит, новый — работает
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.PostAsJsonAsync("/api/auth/login",
            new { email = "user7@booking.local", password = "User123!" })).StatusCode);
        var relogin = await Client.PostAsJsonAsync("/api/auth/login",
            new { email = "user7@booking.local", password = "NewPass123!" });
        Assert.True(relogin.IsSuccessStatusCode);

        // вернуть обратно
        var back = await Client.PostAsJsonAsync("/api/auth/login",
            new { email = "user7@booking.local", password = "NewPass123!" });
        var tok2 = JsonDocument.Parse(await back.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString();
        await As(Client, tok2).PostAsJsonAsync("/api/users/change-password",
            new { userId = 10, currentPassword = "NewPass123!", newPassword = "User123!", confirmPassword = "User123!" });
    }

    // ---------- брони ----------

    [Fact]
    public async Task Booking_GetAndUpdate()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        var client = As(Client, token);

        var mine = await GetJsonAsync(client, "/api/bookings/my");
        var bookingId = mine[0].GetProperty("id").GetInt32();
        var hotelId = mine[0].GetProperty("hotelId").GetInt32();
        var roomTypeId = mine[0].GetProperty("roomTypeId").GetInt32();

        var one = await GetJsonAsync(client, $"/api/bookings/{bookingId}");
        Assert.Equal(bookingId, one.GetProperty("id").GetInt32());

        // перенос на далёкие свободные даты
        var updated = await client.PutAsJsonAsync($"/api/bookings/{bookingId}", new
        {
            id = bookingId,
            hotelId,
            roomTypeId,
            checkInDate = $"{DateTime.Today.AddDays(300):yyyy-MM-dd}T00:00:00",
            checkOutDate = $"{DateTime.Today.AddDays(303):yyyy-MM-dd}T00:00:00",
            guestCount = 1,
            status = 1,
        });
        Assert.True(updated.IsSuccessStatusCode, await updated.Content.ReadAsStringAsync());
        var moved = await GetJsonAsync(client, $"/api/bookings/{bookingId}");
        Assert.Equal(1, moved.GetProperty("guestCount").GetInt32());
    }

    [Fact]
    public async Task Booking_DetailsAndActiveLists()
    {
        var staff = await AsManagerAsync();
        var details = await GetJsonAsync(staff, "/api/bookings/details");
        Assert.True(details.GetArrayLength() > 0);

        var active = await GetJsonAsync(staff, "/api/bookings/active-details");
        Assert.True(active.ValueKind == JsonValueKind.Array);
    }

    // ---------- аналитика отелей ----------

    [Fact]
    public async Task HotelAnalytics_Endpoints()
    {
        var staff = await AsManagerAsync();

        var trends = await GetJsonAsync(staff, "/api/hotels/trends?hotelId=1&months=6");
        Assert.True(trends.ValueKind == JsonValueKind.Array);

        var performance = await GetJsonAsync(staff, "/api/hotels/1/performance");
        Assert.True(performance.ValueKind == JsonValueKind.Object);

        var ordered = await GetJsonAsync(staff, "/api/hotels/ordered-by-rating-name?pageSize=5");
        Assert.True(ordered.ValueKind == JsonValueKind.Array);
    }

    // ---------- журнал и отчёты ----------

    [Fact]
    public async Task Logs_FilteredByDate()
    {
        var admin = await AsAdminAsync();
        var from = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
        var logs = await GetJsonAsync(admin, $"/api/logs?startDate={from}&endDate={to}");
        Assert.True(logs.GetArrayLength() > 0);
    }

    [Fact]
    public async Task RoomTypes_ListEndpoint()
    {
        var types = await GetJsonAsync(Client, "/api/room-types");
        Assert.True(types.GetArrayLength() >= 160);
    }
}
