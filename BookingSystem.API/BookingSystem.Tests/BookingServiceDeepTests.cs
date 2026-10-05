using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BookingSystem.Tests;

/// <summary>
/// Юнит-тесты оставшихся веток BookingService: поиск по датам/идентификаторам,
/// ошибки обновления, аудит при всех переходах.
/// </summary>
public class BookingServiceDeepTests
{
    private readonly Mock<IBookingRepository> _repo = new();
    private readonly Mock<IRepository<RoomType>> _rt = new();
    private readonly Mock<IHotelRepository> _hotel = new();
    private readonly Mock<IRepository<RoomPricing>> _pricing = new();
    private readonly Mock<ILoggingService> _log = new();

    private readonly BookingService _svc;

    public BookingServiceDeepTests()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        _svc = new BookingService(_repo.Object, _rt.Object, _hotel.Object,
            _pricing.Object, _log.Object, accessor.Object);

        var hotel = new Hotel { Id = 1, Name = "H", Rating = 4, BasePrice = 100 };
        _hotel.Setup(h => h.GetByIdAsync(1)).ReturnsAsync(hotel);
        _rt.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new RoomType
        { Id = 10, Name = "S", Description = "", Capacity = 3, Area = 20, BasePrice = 100, HotelId = 1, Hotel = hotel });
        _pricing.Setup(p => p.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomPricing, bool>>>()))
            .ReturnsAsync(new List<RoomPricing>());
        _repo.Setup(r => r.HasOverlappingBookingAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
    }

    private static CreateBookingDto Dto(int guests = 1) => new()
    { UserId = 4, HotelId = 1, RoomTypeId = 10, CheckInDate = new DateTime(2030, 5, 1), CheckOutDate = new DateTime(2030, 5, 4), GuestCount = guests };

    [Fact]
    public async Task Update_UnknownBooking_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.UpdateBookingAsync(new UpdateBookingDto
        { Id = 99, CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today.AddDays(1), GuestCount = 1, Status = 1 }));
    }

    [Fact]
    public async Task Update_NewDatesInvalid_Throws()
    {
        var b = new Booking { Id = 3, UserId = 4, HotelId = 1, RoomTypeId = 10, CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today.AddDays(2), GuestCount = 1, Status = 1 };
        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(b);

        await Assert.ThrowsAsync<ArgumentException>(() => _svc.UpdateBookingAsync(new UpdateBookingDto
        { Id = 3, CheckInDate = DateTime.Today.AddDays(9), CheckOutDate = DateTime.Today.AddDays(9), GuestCount = 1, Status = 1 }));
    }

    [Fact]
    public async Task Update_NewDatesConflict_Throws()
    {
        var b = new Booking { Id = 3, UserId = 4, HotelId = 1, RoomTypeId = 10, CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today.AddDays(2), GuestCount = 1, Status = 1 };
        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(b);
        _repo.Setup(r => r.HasOverlappingBookingAsync(10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 3)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BookingSystem.Application.Exceptions.BookingConflictException>(() => _svc.UpdateBookingAsync(new UpdateBookingDto
        { Id = 3, CheckInDate = DateTime.Today.AddDays(10), CheckOutDate = DateTime.Today.AddDays(14), GuestCount = 1, Status = 1 }));
    }

    [Fact]
    public async Task Update_GuestsOverCapacity_Throws()
    {
        var b = new Booking { Id = 3, UserId = 4, HotelId = 1, RoomTypeId = 10, CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today.AddDays(2), GuestCount = 1, Status = 1 };
        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(b);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.UpdateBookingAsync(new UpdateBookingDto
        { Id = 3, CheckInDate = b.CheckInDate, CheckOutDate = b.CheckOutDate, GuestCount = 10, Status = 1 }));
    }

    [Fact]
    public async Task Cancel_Unknown_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.CancelBookingAsync(99));
    }

    [Fact]
    public async Task UpdateStatus_UnknownBooking_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.UpdateBookingStatusAsync(99, 3));
    }

    [Fact]
    public async Task UpdateStatus_Valid_TransitionsLog()
    {
        var b = new Booking { Id = 8, UserId = 4, Status = 1 };
        _repo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(b);

        var res = await _svc.UpdateBookingStatusAsync(8, 5);

        Assert.Equal(5, res.Status);
        _log.Verify(l => l.LogActionAsync(4, UserActionType.BookingUpdated,
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_Success_LogsAudit()
    {
        var b = new Booking { Id = 9, UserId = 4, Status = 1 };
        _repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(b);

        await _svc.CancelBookingAsync(9);

        _log.Verify(l => l.LogActionAsync(4, UserActionType.BookingUpdated,
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task GetByDates_ProxiesRepository()
    {
        var list = new[] { new Booking { Id = 1 }, new Booking { Id = 2 } };
        _repo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
            .ReturnsAsync(list);

        var res = await _svc.GetBookingsByDateRangeAsync(new DateTime(2030, 1, 1), new DateTime(2030, 12, 31));

        Assert.Equal(2, res.Count());
    }

    [Fact]
    public async Task Create_PriceFromSeasonalCalendar_FullCoverage()
    {
        _pricing.Setup(p => p.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomPricing, bool>>>()))
            .ReturnsAsync(new List<RoomPricing>
            {
                new() { RoomTypeId = 10, Date = new DateTime(2030, 5, 1), Price = 200 },
                new() { RoomTypeId = 10, Date = new DateTime(2030, 5, 2), Price = 210 },
                new() { RoomTypeId = 10, Date = new DateTime(2030, 5, 3), Price = 220 },
            });

        var res = await _svc.CreateBookingAsync(Dto());

        Assert.Equal(630, res.TotalPrice);
        _log.Verify(l => l.LogActionAsync(4, UserActionType.BookingCreated,
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Create_UnknownRoomType_Throws()
    {
        _rt.Setup(r => r.GetByIdAsync(77)).ReturnsAsync((RoomType?)null);

        var dto = Dto();
        dto.RoomTypeId = 77;

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.CreateBookingAsync(dto));
    }

}
