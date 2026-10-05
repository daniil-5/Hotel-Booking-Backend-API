using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;
using BookingSystem.Domain.DTOs.Booking;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BookingSystem.Tests;

/// <summary>
/// Юнит-тесты сервисов: недостающие ветки UserService, BookingService,
/// UserActionAuditService (пробросы в репозитории и маппинг).
/// </summary>
public class ServicesCoverageTests
{
    // ---------- UserService ----------

    private static (UserService Svc, Mock<IUserRepository> Repo, Mock<ILoggingService> Log) UserServiceOf()
    {
        var repo = new Mock<IUserRepository>();
        var log = new Mock<ILoggingService>();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        return (new UserService(repo.Object, log.Object, accessor.Object), repo, log);
    }

    private static User User(int id = 4, int role = 1) => new()
    { Id = id, Username = "u", Email = "u@t", PasswordHash = "h", Role = role };

    [Fact]
    public async Task UserService_GetAllAndGetById_MapToDto()
    {
        var (svc, repo, _) = UserServiceOf();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { User(1), User(2) });
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(User(7, 2));

        Assert.Equal(2, (await svc.GetAllUsersAsync()).Count());
        var one = await svc.GetUserByIdAsync(7);
        Assert.Equal("Manager", one.Role);
    }

    [Fact]
    public async Task UserService_Delete_CallsRepository()
    {
        var (svc, repo, _) = UserServiceOf();
        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(User(5));
        repo.Setup(r => r.DeleteAsync(5)).Returns(Task.CompletedTask);

        await svc.DeleteUserAsync(5);

        repo.Verify(r => r.DeleteAsync(5), Times.Once);
    }

    [Fact]
    public async Task UserService_VerifyPassword_WrongHash_False()
    {
        var (svc, repo, _) = UserServiceOf();
        repo.Setup(r => r.GetByEmailAsync("a@b.c")).ReturnsAsync((User?)null);

        Assert.False(await svc.VerifyUserPasswordAsync("a@b.c", "x"));
    }

    // ---------- BookingService: пробросы и оставшиеся ветки ----------

    private static (BookingService Svc, Mock<IBookingRepository> Repo, Mock<IRepository<RoomType>> Rt, Mock<IRepository<RoomPricing>> Prc) BookingOf()
    {
        var repo = new Mock<IBookingRepository>();
        var rt = new Mock<IRepository<RoomType>>();
        var hotel = new Mock<IHotelRepository>();
        var pricing = new Mock<IRepository<RoomPricing>>();
        var log = new Mock<ILoggingService>();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var svc = new BookingService(repo.Object, rt.Object, hotel.Object, pricing.Object, log.Object, accessor.Object);

        var hotelEntity = new Hotel { Id = 1, Name = "H", Rating = 4, BasePrice = 100 };
        hotel.Setup(h => h.GetByIdAsync(1)).ReturnsAsync(hotelEntity);
        rt.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new RoomType
        { Id = 10, Name = "S", Description = "", Capacity = 3, Area = 20, BasePrice = 100, HotelId = 1, Hotel = hotelEntity });
        pricing.Setup(p => p.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomPricing, bool>>>()))
            .ReturnsAsync(new List<RoomPricing>());
        repo.Setup(r => r.HasOverlappingBookingAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
        return (svc, repo, rt, pricing);
    }

    private static CreateBookingDto Dto(int guests = 2) => new()
    { UserId = 4, HotelId = 1, RoomTypeId = 10, CheckInDate = new DateTime(2030, 5, 1), CheckOutDate = new DateTime(2030, 5, 4), GuestCount = guests };

    [Fact]
    public async Task BookingService_SeasonalPrice_FallsBackToBaseForGap()
    {
        var (svc, _, _, pricing) = BookingOf();
        // цена есть только на первый день: остальные — по базовой
        pricing.Setup(p => p.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomPricing, bool>>>()))
            .ReturnsAsync(new List<RoomPricing> { new() { RoomTypeId = 10, Date = new DateTime(2030, 5, 1), Price = 250 } });

        var b = await svc.CreateBookingAsync(Dto());

        Assert.Equal(250 + 100 + 100, b.TotalPrice);
    }

    [Fact]
    public async Task BookingService_UpdateBooking_SameDates_DoesNotRecheckAvailability()
    {
        var (svc, repo, _, _) = BookingOf();
        var existing = new Booking
        { Id = 3, UserId = 4, HotelId = 1, RoomTypeId = 10, CheckInDate = new DateTime(2030, 5, 1), CheckOutDate = new DateTime(2030, 5, 4), GuestCount = 2, TotalPrice = 300, Status = 1 };
        repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);

        var result = await svc.UpdateBookingAsync(new UpdateBookingDto
        { Id = 3, CheckInDate = existing.CheckInDate, CheckOutDate = existing.CheckOutDate, GuestCount = 3, Status = 1 });

        Assert.Equal(3, result.GuestCount);
        repo.Verify(r => r.HasOverlappingBookingAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task BookingService_PassThroughQueries()
    {
        var (svc, repo, _, _) = BookingOf();
        var from = new DateTime(2030, 1, 1);
        var to = new DateTime(2030, 2, 1);
        var bookings = new[] { new Booking { Id = 1, UserId = 4 } };
        repo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>())).ReturnsAsync(bookings);
        repo.Setup(r => r.GetBookingsWithDetailsAsync(null, null, null))
            .ReturnsAsync(new List<BookingDetails>());
        repo.Setup(r => r.GetActiveBookingsWithDetailsAsync())
            .ReturnsAsync(new List<ActiveBookingDetailsDto>());

        Assert.Single(await svc.GetBookingsByDateRangeAsync(from, to));
        Assert.Single(await svc.GetBookingsByUserIdAsync(4));
        Assert.Single(await svc.GetBookingsByRoomTypeIdAsync(10));
        Assert.Single(await svc.GetBookingsByHotelIdAsync(1));
        await svc.GetBookingsWithDetailsAsync();
        await svc.GetActiveBookingsWithDetailsAsync();
        repo.Verify(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()), Times.Exactly(4));
    }

    [Fact]
    public async Task BookingService_UpdateStatus_ValidTransition()
    {
        var (svc, repo, _, _) = BookingOf();
        var existing = new Booking { Id = 8, UserId = 4, Status = 1 };
        repo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(existing);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);

        var updated = await svc.UpdateBookingStatusAsync(8, 3);

        Assert.Equal(3, updated.Status);
    }

    [Fact]
    public async Task BookingService_DeleteUnknown_Throws()
    {
        var (svc, repo, _, _) = BookingOf();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteBookingAsync(99));
    }

    // ---------- UserActionAuditService ----------

    [Fact]
    public async Task AuditService_ProxiesRepository()
    {
        var repo = new Mock<IUserActionAuditRepository>();
        var entity = new UserActionAudit { UserId = 4, UserActionType = UserActionType.UserLogin, IsSuccess = true };
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { entity });

        var svc = new UserActionAuditService(repo.Object);

        await svc.AuditActionAsync(entity.UserId, entity.UserActionType, entity.IsSuccess);
        var all = await svc.GetAllAsync();

        Assert.Single(all);
        repo.Verify(r => r.AddAsync(It.IsAny<UserActionAudit>()), Times.Once);
    }
}

/// <summary>
/// Оставшиеся ветки UserService: конфликты обновления, смена пароля, удаление.
/// </summary>
public class UserServiceDeepTests
{
    private static (UserService Svc, Mock<IUserRepository> Repo, Mock<ILoggingService> Log) Make()
    {
        var repo = new Mock<IUserRepository>();
        var log = new Mock<ILoggingService>();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        return (new UserService(repo.Object, log.Object, accessor.Object), repo, log);
    }

    private static User User(int id = 4, int role = 1) => new()
    { Id = id, Username = "user1", Email = "u@t.local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Old123!"), Role = role };

    private static UpdateUserDto Update(int id = 4, string username = "user1", string email = "u@t.local") => new()
    { Id = id, Username = username, Email = email, FirstName = "N", LastName = "S", PhoneNumber = "+" };

    [Fact]
    public async Task Update_UnknownUser_Throws()
    {
        var (svc, repo, _) = Make();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<ApplicationException>(() => svc.UpdateUserAsync(Update(99)));
    }

    [Fact]
    public async Task Update_EmailTakenByOther_Throws()
    {
        var (svc, repo, _) = Make();
        repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(User(4));
        repo.Setup(r => r.GetByEmailAsync("taken@t.local")).ReturnsAsync(User(5, 1));

        await Assert.ThrowsAsync<BookingSystem.Application.Exceptions.DuplicateUserException>(
            () => svc.UpdateUserAsync(Update(4, email: "taken@t.local")));
    }

    [Fact]
    public async Task Update_UsernameTakenByOther_Throws()
    {
        var (svc, repo, _) = Make();
        repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(User(4));
        repo.Setup(r => r.GetByEmailAsync("busy")).ReturnsAsync(User(6, 1));

        await Assert.ThrowsAsync<BookingSystem.Application.Exceptions.DuplicateUserException>(
            () => svc.UpdateUserAsync(Update(4, username: "busy")));
    }

    [Fact]
    public async Task Update_SelfSameValues_Succeeds()
    {
        var (svc, repo, _) = Make();
        var user = User(4);
        repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(user);
        repo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var res = await svc.UpdateUserAsync(Update(4, username: "user1", email: "u@t.local"));

        Assert.Equal("user1", res.Username);
        repo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.PhoneNumber == "+")), Times.Once);
    }

    [Fact]
    public async Task Delete_UnknownUser_Throws()
    {
        var (svc, repo, _) = Make();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<ApplicationException>(() => svc.DeleteUserAsync(99));
    }

    [Fact]
    public async Task Delete_Known_DeletesAndLogs()
    {
        var (svc, repo, log) = Make();
        repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(User(4));
        repo.Setup(r => r.DeleteAsync(4)).Returns(Task.CompletedTask);

        await svc.DeleteUserAsync(4);

        repo.Verify(r => r.DeleteAsync(4), Times.Once);
        log.Verify(l => l.LogActionAsync(4, UserActionType.UserDeleted,
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsFalse()
    {
        var (svc, repo, _) = Make();
        repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(User(4));

        var ok = await svc.ChangePasswordAsync(new ChangePasswordDto
        { UserId = 4, CurrentPassword = "Wrong!", NewPassword = "New1234!", ConfirmPassword = "New1234!" });

        Assert.False(ok);
    }

    [Fact]
    public async Task ChangePassword_ConfirmationMismatch_Throws()
    {
        var (svc, repo, _) = Make();
        repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(User(4));

        await Assert.ThrowsAsync<ApplicationException>(() => svc.ChangePasswordAsync(new ChangePasswordDto
        { UserId = 4, CurrentPassword = "Old123!", NewPassword = "New1234!", ConfirmPassword = "Other999!" }));
    }

    [Fact]
    public async Task ChangePassword_UnknownUser_Throws()
    {
        var (svc, repo, _) = Make();
        repo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<ApplicationException>(() => svc.ChangePasswordAsync(new ChangePasswordDto
        { UserId = 42, CurrentPassword = "x", NewPassword = "y", ConfirmPassword = "y" }));
    }

    [Fact]
    public async Task ChangePassword_Success_UpdatesHash()
    {
        var (svc, repo, log) = Make();
        var user = User(4);
        repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(user);
        repo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var ok = await svc.ChangePasswordAsync(new ChangePasswordDto
        { UserId = 4, CurrentPassword = "Old123!", NewPassword = "New1234!", ConfirmPassword = "New1234!" });

        Assert.True(ok);
        Assert.True(BCrypt.Net.BCrypt.Verify("New1234!", user.PasswordHash));
        log.Verify(l => l.LogActionAsync(4, UserActionType.ChangePassword,
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Lookups_ProxyRepository()
    {
        var (svc, repo, _) = Make();
        repo.Setup(r => r.GetByEmailAsync("u@t.local")).ReturnsAsync(User(4));
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { User(1), User(2) });

        Assert.NotNull(await svc.GetUserByUsernameAsync("u@t.local"));
        Assert.Equal(2, (await svc.GetAllUsersAsync()).Count());
    }
}
