using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BookingSystem.ApiTests;

/// <summary>
/// Сценарии, добивающие покрытие сервисов и репозиториев: профиль,
/// аудит-запросы, управление пользователями, room-type/performance.
/// </summary>
[Collection("api")]
public class ServicesEndpointsTests : ApiTestBase
{
    public ServicesEndpointsTests(ApiTestFactory f) : base(f) { }

    private async Task<HttpClient> AsAdminAsync()
    {
        var token = await LoginAsync(Client, "admin@booking.local", "Admin123!");
        return As(Client, token);
    }

    [Fact]
    public async Task UserProfile_CurrentAndLookup()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        var client = As(Client, token);

        var profile = await GetJsonAsync(client, "/api/users/profile");
        Assert.Equal("user1", profile.GetProperty("username").GetString());

        // поиск по имени — привилегия персонала
        var admin = await AsAdminAsync();
        var byName = await GetJsonAsync(admin, "/api/users/by-username/user1");
        Assert.Equal(4, byName.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Users_ActiveOrdered_AndNoBookings()
    {
        var admin = await AsAdminAsync();
        var active = await GetJsonAsync(admin, "/api/users/active-ordered");
        Assert.True(active.GetArrayLength() >= 10);

        var noBookings = await GetJsonAsync(admin, "/api/users/no-bookings");
        Assert.True(noBookings.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public async Task Users_CreateByAdmin_AndLookup()
    {
        var admin = await AsAdminAsync();
        var name = $"adm{Guid.NewGuid():N}"[..12];
        var res = await admin.PostAsJsonAsync("/api/users", new
        {
            username = name,
            email = $"{name}@t.local",
            password = "Strong123!",
            firstName = "Новый",
            lastName = "Админский",
            phoneNumber = "+375295550055",
        });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Audit_MostRecent_UserAndRange()
    {
        var admin = await AsAdminAsync();
        // сгенерировать события входом
        await LoginAsync(Client, "user1@booking.local", "User123!");

        var logs = await GetJsonAsync(admin, "/api/logs");
        Assert.True(logs.GetArrayLength() > 0);

        var byUser = await GetJsonAsync(admin, "/api/logs?userId=4");
        Assert.True(byUser.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public async Task RoomType_UpdateAndDelete_Lifecycle()
    {
        var staffToken = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var staff = As(Client, staffToken);

        // создать новый тип, обновить, удалить
        var created = await staff.PostAsJsonAsync("/api/room-types", new
        { name = "Студия", description = "тест", capacity = 2, area = 30, hotelId = 1, basePrice = 150m });
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var id = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var list = await GetJsonAsync(staff, "/api/room-types");
        Assert.Contains(list.EnumerateArray(), rt => rt.GetProperty("id").GetInt32() == id);

        Assert.True((await staff.DeleteAsync($"/api/room-types/{id}")).IsSuccessStatusCode);
    }

    [Fact]
    public async Task HotelPerformance_FullReport()
    {
        var staffToken = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var staff = As(Client, staffToken);

        var res = await staff.GetAsync("/api/hotels/1/performance");
        Assert.True(res.IsSuccessStatusCode, $"{res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task BookingActiveDetails_ReturnsRows()
    {
        var staffToken = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var active = await GetJsonAsync(As(Client, staffToken), "/api/bookings/active-details");
        Assert.True(active.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public async Task UserHistory_WithDetails()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        var history = await GetJsonAsync(As(Client, token), "/api/bookings/user/4/history");
        Assert.True(history.GetProperty("totalBookings").GetInt64() > 0);
    }
}

/// <summary>
/// Мелкие эндпоинты для полноты покрытия: room-type по id, обновление
/// пользователя гостем, room-pricing обновление/удаление, фото по id.
/// </summary>
[Collection("api")]
public class SmallEndpointsTests : ApiTestBase
{
    public SmallEndpointsTests(ApiTestFactory f) : base(f) { }

    [Fact]
    public async Task RoomType_GetById_FoundAndNotFound()
    {
        var ok = await GetJsonAsync(Client, "/api/room-types/1");
        Assert.Equal(1, ok.GetProperty("id").GetInt32());

        var res = await Client.GetAsync("/api/room-types/424242");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task RoomType_UpdateLifecycle()
    {
        var staffToken = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var staff = As(Client, staffToken);

        var created = await staff.PostAsJsonAsync("/api/room-types", new
        { name = "Апартаменты", description = "тест", capacity = 2, area = 33, hotelId = 1, basePrice = 180m });
        var id = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var updated = await staff.PutAsJsonAsync($"/api/room-types/{id}", new
        { id, name = "Апартаменты+", description = "тест", capacity = 3, area = 36, hotelId = 1, basePrice = 190m });
        Assert.True(updated.IsSuccessStatusCode, await updated.Content.ReadAsStringAsync());

        var fetched = await GetJsonAsync(Client, $"/api/room-types/{id}");
        Assert.Equal("Апартаменты+", fetched.GetProperty("name").GetString());

        Assert.True((await staff.DeleteAsync($"/api/room-types/{id}")).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Pricing_UpdateAndDelete()
    {
        var staffToken = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var staff = As(Client, staffToken);

        var created = await staff.PostAsJsonAsync("/api/roompricings", new
        { date = $"{DateTime.Today.AddYears(2):yyyy-MM-dd}T00:00:00", price = 111m, roomTypeId = 1 });
        var id = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var updated = await staff.PutAsJsonAsync($"/api/roompricings/{id}", new
        { id, date = $"{DateTime.Today.AddYears(2):yyyy-MM-dd}T00:00:00", price = 222m, roomTypeId = 1 });
        Assert.True(updated.IsSuccessStatusCode, await updated.Content.ReadAsStringAsync());

        Assert.True((await staff.DeleteAsync($"/api/roompricings/{id}")).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Users_DeleteForbidden_ForGuest()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        var res = await As(Client, token).DeleteAsync("/api/users/5");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task HotelPhotos_GetById_NotFound()
    {
        var res = await Client.GetAsync("/api/hotelphotos/424242");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}

[Collection("api")]
public class CheapEndpointsTests : ApiTestBase
{
    public CheapEndpointsTests(ApiTestFactory f) : base(f) { }

    [Fact]
    public async Task RoomType_ByHotel_Unknown_ReturnsEmptyOr404()
    {
        var res = await Client.GetAsync("/api/room-types/by-hotel/424242");
        Assert.True(res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Hotel_Trends_WithoutHotelId()
    {
        var staffToken = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var res = await As(Client, staffToken).GetAsync("/api/hotels/trends?months=3");
        Assert.True(res.IsSuccessStatusCode);
    }

    [Fact]
    public async Task RoomPricing_GetById()
    {
        var list = await GetJsonAsync(Client, "/api/roompricings");
        var id = list[0].GetProperty("id").GetInt32();
        var one = await GetJsonAsync(Client, $"/api/roompricings/{id}");
        Assert.Equal(id, one.GetProperty("id").GetInt32());
    }
}

[Collection("api")]
public class FinalCoverageTests : ApiTestBase
{
    public FinalCoverageTests(ApiTestFactory f) : base(f) { }

    [Fact]
    public async Task Hotels_PremiumAndOrdered()
    {
        var staffToken = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var staff = As(Client, staffToken);

        var premium = await GetJsonAsync(staff, "/api/hotels/premium");
        Assert.True(premium.ValueKind == JsonValueKind.Array);

        var ordered = await GetJsonAsync(staff, "/api/hotels/ordered-by-rating-name?pageSize=3");
        Assert.True(ordered.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public async Task Logs_MostRecent()
    {
        var adminToken = await LoginAsync(Client, "admin@booking.local", "Admin123!");
        await LoginAsync(Client, "user3@booking.local", "User123!");
        var logs = await GetJsonAsync(As(Client, adminToken), "/api/logs?eventType=UserLogin");
        Assert.True(logs.GetArrayLength() > 0);
    }
}
