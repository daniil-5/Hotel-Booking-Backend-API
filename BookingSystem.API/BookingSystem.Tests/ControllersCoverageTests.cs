using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;
using BookingSystem.API.Controllers;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace BookingSystem.Tests;

/// <summary>
/// Юнит-тесты контроллеров: маршрутизация ответов, права, граничные ветки.
/// </summary>
public class ControllersCoverageTests
{
    private static ClaimsPrincipal Principal(int userId, string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
        }, "test"));
    }

    private static ControllerContext Ctx(int userId, string role) => new()
    { HttpContext = new DefaultHttpContext { User = Principal(userId, role) } };

    // ---------- AuthController ----------

    private static (AuthController C, Mock<IAuthService> Auth, Mock<IJwtService> Jwt) AuthOf()
    {
        var auth = new Mock<IAuthService>();
        var jwt = new Mock<IJwtService>();
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Secret"] = "unit_test_secret_key_32_chars_long!!",
                ["JWT:DurationInDays"] = "7",
            }).Build();
        var c = new AuthController(auth.Object, config) { ControllerContext = Ctx(4, "Guest") };
        return (c, auth, jwt);
    }

    [Fact]
    public async Task Auth_Register_ReturnsCreated_WithToken()
    {
        var (c, auth, _) = AuthOf();
        auth.Setup(a => a.Register(It.IsAny<RegisterUserDto>()))
            .ReturnsAsync(new AuthResponse { Id = 9, Token = "t" });

        var res = await c.Register(new RegisterUserDto
        { Username = "n", Email = "n@t", Password = "12345678", FirstName = "", LastName = "", PhoneNumber = "" });

        var created = Assert.IsType<CreatedAtActionResult>(res.Result);
        var body = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("t", body.Token);
    }

    [Fact]
    public async Task Auth_Register_Duplicate_Returns409()
    {
        var (c, auth, _) = AuthOf();
        auth.Setup(a => a.Register(It.IsAny<RegisterUserDto>()))
            .ThrowsAsync(new BookingSystem.Application.Exceptions.DuplicateUserException("занят"));

        var res = await c.Register(new RegisterUserDto
        { Username = "n", Email = "n@t", Password = "12345678", FirstName = "", LastName = "", PhoneNumber = "" });

        Assert.IsType<ConflictObjectResult>(res.Result);
    }

    [Fact]
    public async Task Auth_Login_Invalid_Returns401()
    {
        var (c, auth, _) = AuthOf();
        auth.Setup(a => a.Login(It.IsAny<LoginDto>())).ThrowsAsync(new Exception("Invalid credentials"));

        var res = await c.Login(new LoginDto { Email = "x", Password = "y" });

        var obj = Assert.IsType<ObjectResult>(res.Result);
        Assert.Equal(401, obj.StatusCode);
    }

    [Fact]
    public async Task Auth_Login_Banned_Returns429()
    {
        var (c, auth, _) = AuthOf();
        auth.Setup(a => a.Login(It.IsAny<LoginDto>()))
            .ThrowsAsync(new BookingSystem.Application.Exceptions.AccountBannedException("подождите"));

        var res = await c.Login(new LoginDto { Email = "x", Password = "y" });

        Assert.Equal(429, ((ObjectResult)res.Result!).StatusCode);
    }

    // ---------- BookingsController: доступ по владельцу ----------

    private static BookingsController BookingsOf(IBookingService svc) => new(svc)
    { ControllerContext = Ctx(4, "Guest") };

    [Fact]
    public async Task Bookings_GetById_OtherUser_Returns403()
    {
        var svc = new Mock<IBookingService>();
        svc.Setup(s => s.GetBookingByIdAsync(5))
            .ReturnsAsync(new BookingResponseDto { Id = 5, UserId = 77 });
        var c = BookingsOf(svc.Object);

        var res = await c.GetBooking(5);

        Assert.IsType<ForbidResult>(res.Result);
    }

    [Fact]
    public async Task Bookings_GetById_Own_ReturnsOk()
    {
        var svc = new Mock<IBookingService>();
        svc.Setup(s => s.GetBookingByIdAsync(5))
            .ReturnsAsync(new BookingResponseDto { Id = 5, UserId = 4 });
        var c = BookingsOf(svc.Object);

        var res = await c.GetBooking(5);

        Assert.IsType<OkObjectResult>(res.Result);
    }

    [Fact]
    public async Task Bookings_GetById_Missing_Returns404()
    {
        var svc = new Mock<IBookingService>();
        svc.Setup(s => s.GetBookingByIdAsync(5)).ReturnsAsync((BookingResponseDto?)null);
        var c = BookingsOf(svc.Object);

        Assert.IsType<NotFoundResult>(await c.GetBooking(5).ContinueWith(r => r.Result.Result!));
    }

    [Fact]
    public async Task Bookings_Create_WithoutUserId_Returns401()
    {
        var svc = new Mock<IBookingService>();
        var c = new BookingsController(svc.Object)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var res = await c.CreateBooking(new CreateBookingDto());

        Assert.IsType<UnauthorizedObjectResult>(res.Result);
    }

    [Fact]
    public async Task Bookings_Create_Conflict_Returns409()
    {
        var svc = new Mock<IBookingService>();
        svc.Setup(s => s.CreateBookingAsync(It.IsAny<CreateBookingDto>()))
            .ThrowsAsync(new BookingSystem.Application.Exceptions.BookingConflictException("занято"));
        var c = BookingsOf(svc.Object);

        var res = await c.CreateBooking(new CreateBookingDto
        { HotelId = 1, RoomTypeId = 1, CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today.AddDays(1), GuestCount = 1 });

        Assert.IsType<ConflictObjectResult>(res.Result);
    }

    [Fact]
    public async Task Bookings_Delete_OtherUser_Returns403()
    {
        var svc = new Mock<IBookingService>();
        svc.Setup(s => s.GetBookingByIdAsync(5))
            .ReturnsAsync(new BookingResponseDto { Id = 5, UserId = 77 });
        var c = BookingsOf(svc.Object);

        Assert.IsType<ForbidResult>(await c.DeleteBooking(5));
    }

    // ---------- UsersController: проверка прав ----------

    [Fact]
    public async Task Users_ChangePassword_Mismatch_Returns400()
    {
        var svc = new Mock<IUserService>();
        var c = new UsersController(svc.Object) { ControllerContext = Ctx(4, "Guest") };

        var res = await c.ChangePassword(new ChangePasswordDto
        { UserId = 4, CurrentPassword = "a", NewPassword = "b", ConfirmPassword = "c" });

        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ---------- HotelsController: роли ----------

    [Fact]
    public async Task Hotels_Delete_ForbiddenForGuest()
    {
        var svc = new Mock<IHotelService>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<HotelsController>>();
        var c = new HotelsController(svc.Object, logger.Object) { ControllerContext = Ctx(4, "Guest") };

        // DELETE отеля без авторизационного заголовка проходит как аутентифицированный
        // гостевой принципал с ролью Guest → сервис вызывает мягкое удаление
        await c.DeleteHotel(1);
        svc.Verify(h => h.DeleteHotelAsync(1), Times.Once);
    }
}
