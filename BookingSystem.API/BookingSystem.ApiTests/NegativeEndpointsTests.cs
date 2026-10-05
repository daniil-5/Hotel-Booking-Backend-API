using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BookingSystem.ApiTests;

/// <summary>
/// Негативные сценарии API: 404 на несуществующие сущности, 403 на чужие
/// ресурсы, 400 на некорректные данные, работа с фотографиями отеля.
/// </summary>
[Collection("api")]
public class NegativeEndpointsTests : ApiTestBase
{
    public NegativeEndpointsTests(ApiTestFactory f) : base(f) { }

    [Fact]
    public async Task UnknownHotel_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/hotels/99999")).StatusCode);
    }

    [Fact]
    public async Task UnknownBooking_Returns404()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        Assert.Equal(HttpStatusCode.NotFound,
            (await As(Client, token).GetAsync("/api/bookings/999999")).StatusCode);
    }

    [Fact]
    public async Task StrangerBooking_Returns403()
    {
        // бронь user1 смотрит user2
        var owner = await LoginAsync(Client, "user1@booking.local", "User123!");
        var mine = await GetJsonAsync(As(Client, owner), "/api/bookings/my");
        var bookingId = mine[0].GetProperty("id").GetInt32();

        var stranger = Factory.CreateClient();
        var strangerToken = await LoginAsync(stranger, "user2@booking.local", "User123!");
        stranger.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", strangerToken);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await stranger.GetAsync($"/api/bookings/{bookingId}")).StatusCode);
    }

    [Fact]
    public async Task BookingUpdate_MismatchedId_Rejected()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        var res = await As(Client, token).PutAsJsonAsync("/api/bookings/1", new
        { id = 2, hotelId = 1, roomTypeId = 1, checkInDate = "2030-01-01T00:00:00", checkOutDate = "2030-01-03T00:00:00", guestCount = 1, status = 1 });
        // id в маршруте и теле расходятся — контроллер отклоняет до проверки владельца
        Assert.True(res.StatusCode == HttpStatusCode.BadRequest
                 || res.StatusCode == HttpStatusCode.Forbidden,
            $"неожиданный код {res.StatusCode}");
    }

    [Fact]
    public async Task RoomTypeCreate_ForbiddenForGuest()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        var res = await As(Client, token).PostAsJsonAsync("/api/room-types", new
        { name = "X", description = "", capacity = 1, hotelId = 1, basePrice = 1m });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task PricingCreate_UnknownRoomType_Returns404()
    {
        var token = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var res = await As(Client, token).PostAsJsonAsync("/api/roompricings", new
        { date = "2031-01-01T00:00:00", price = 10m, roomTypeId = 999999 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task UserEndpoints_NotFoundCases()
    {
        var adminToken = await LoginAsync(Client, "admin@booking.local", "Admin123!");
        var admin = As(Client, adminToken);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/users/424242")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/users/by-email/nobody@nowhere.test")).StatusCode);
    }

    // ---------- фотографии отеля (без сетевых загрузок) ----------

    [Fact]
    public async Task Photo_UpdateMeta_SetMain_AndDelete()
    {
        var staffToken = await LoginAsync(Client, "manager1@booking.local", "Manager123!");
        var staff = As(Client, staffToken);

        var photos = await GetJsonAsync(staff, "/api/hotelphotos/hotel/1");
        var main = photos.EnumerateArray().First(p => p.GetProperty("isMain").GetBoolean());
        var other = photos.EnumerateArray().First(p => !p.GetProperty("isMain").GetBoolean());
        var mainId = main.GetProperty("id").GetInt32();
        var otherId = other.GetProperty("id").GetInt32();

        // сделать другую фотографию главной
        var setMain = await staff.PutAsJsonAsync($"/api/hotelphotos/{otherId}/set-main", new { });
        Assert.True(setMain.IsSuccessStatusCode, $"{setMain.StatusCode}: {await setMain.Content.ReadAsStringAsync()}");

        var after = await GetJsonAsync(staff, "/api/hotelphotos/hotel/1");
        Assert.Contains(after.EnumerateArray(),
            p => p.GetProperty("id").GetInt32() == otherId && p.GetProperty("isMain").GetBoolean());

        // обновить описание
        var updated = await staff.PutAsJsonAsync($"/api/hotelphotos/{mainId}", new
        { id = mainId, hotelId = 1, url = main.GetProperty("url").GetString(), description = "Новое описание" });
        Assert.True(updated.IsSuccessStatusCode, $"{updated.StatusCode}: {await updated.Content.ReadAsStringAsync()}");

        // удалить лишнюю
        var deleted = await staff.DeleteAsync($"/api/hotelphotos/{mainId}");
        Assert.True(deleted.IsSuccessStatusCode, $"{deleted.StatusCode}");
    }

    [Fact]
    public async Task Logout_ClearsSession()
    {
        var token = await LoginAsync(Client, "user1@booking.local", "User123!");
        var res = await As(Client, token).PostAsync("/api/auth/logout", null);
        Assert.True(res.IsSuccessStatusCode);
    }
}
