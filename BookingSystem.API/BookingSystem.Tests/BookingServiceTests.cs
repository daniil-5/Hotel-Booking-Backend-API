using AutoFixture;
using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.Exceptions;
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
/// Юнит-тесты правил бронирования (основание пирамиды тестирования):
/// валидация дат, вместимость, пересечение бронирований, статусы.
/// </summary>
public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepo = new();
    private readonly Mock<IRepository<RoomType>> _roomTypeRepo = new();
    private readonly Mock<IHotelRepository> _hotelRepo = new();
    private readonly Mock<IRepository<RoomPricing>> _pricingRepo = new();
    private readonly Mock<ILoggingService> _logging = new();
    private readonly Mock<IHttpContextAccessor> _accessor = new();
    private readonly IFixture _fixture = new Fixture();

    private readonly BookingService _service;
    private readonly RoomType _roomType;
    private readonly Hotel _hotel;

    public BookingServiceTests()
    {
        _accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        _service = new BookingService(_bookingRepo.Object, _roomTypeRepo.Object,
            _hotelRepo.Object, _pricingRepo.Object, _logging.Object, _accessor.Object);

        _hotel = new Hotel { Id = 1, Name = "Тест-отель", Location = "Минск", Rating = 4.5, BasePrice = 100 };
        _roomType = new RoomType
        {
            Id = 10, Name = "Стандарт", Description = "номер", Capacity = 2,
            Area = 22, HotelId = 1, BasePrice = 100, Hotel = _hotel,
        };
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_hotel);
        _roomTypeRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(_roomType);
        _pricingRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomPricing, bool>>>()))
            .ReturnsAsync(new List<RoomPricing>());
        _bookingRepo.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
    }

    private CreateBookingDto ValidDto(DateTime? checkIn = null, DateTime? checkOut = null, int guests = 2,
        int roomTypeId = 10, int hotelId = 1) => new()
    {
        UserId = 4, HotelId = hotelId, RoomTypeId = roomTypeId, GuestCount = guests,
        CheckInDate = checkIn ?? new DateTime(2026, 12, 1),
        CheckOutDate = checkOut ?? new DateTime(2026, 12, 5),
    };

    [Fact]
    public async Task CreateBooking_WithValidData_CreatesPendingBookingWithPrice()
    {
        var result = await _service.CreateBookingAsync(ValidDto());

        Assert.Equal(4 * 100, result.TotalPrice);
        Assert.Equal((int)BookingStatus.Pending, result.Status);
        _bookingRepo.Verify(r => r.AddAsync(It.Is<Booking>(b =>
            b.RoomTypeId == 10 && b.Status == (int)BookingStatus.Pending)), Times.Once);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(3, 3)]
    public async Task CreateBooking_WithInvalidDates_Throws(int inOffset, int outOffset)
    {
        var baseDate = new DateTime(2026, 12, 10);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateBookingAsync(ValidDto(baseDate.AddDays(inOffset), baseDate.AddDays(outOffset))));
    }

    [Fact]
    public async Task CreateBooking_GuestsOverCapacity_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateBookingAsync(ValidDto(guests: _roomType.Capacity + 1)));
    }

    [Fact]
    public async Task CreateBooking_UnknownHotel_Throws()
    {
        _hotelRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Hotel?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateBookingAsync(ValidDto(hotelId: 42)));
    }

    [Fact]
    public async Task CreateBooking_RoomTypeFromAnotherHotel_Throws()
    {
        var stranger = new RoomType { Id = 99, Name = "Чужой", Description = "", Capacity = 2, HotelId = 77, BasePrice = 50, Area = 20, Hotel = _hotel };
        _roomTypeRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(stranger);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateBookingAsync(ValidDto(roomTypeId: 99)));
    }

    [Fact]
    public async Task CreateBooking_DatesOverlappingActiveBooking_ThrowsConflict()
    {
        _bookingRepo.Setup(r => r.HasOverlappingBookingAsync(10,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BookingConflictException>(() => _service.CreateBookingAsync(ValidDto()));
    }

    [Fact]
    public async Task CreateBooking_UsesSeasonalPricing_WhenCalendarHasPrices()
    {
        _pricingRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomPricing, bool>>>()))
            .ReturnsAsync(new List<RoomPricing>
            {
                new() { RoomTypeId = 10, Date = new DateTime(2026, 12, 1), Price = 150 },
                new() { RoomTypeId = 10, Date = new DateTime(2026, 12, 2), Price = 150 },
            });

        var result = await _service.CreateBookingAsync(ValidDto());

        Assert.Equal(2 * 150 + 2 * 100, result.TotalPrice);
    }

    [Fact]
    public async Task UpdateBooking_ChangedDates_ChecksAvailabilityWithExclusion()
    {
        var existing = new Booking
        {
            Id = 7, UserId = 4, HotelId = 1, RoomTypeId = 10,
            CheckInDate = new DateTime(2026, 12, 1), CheckOutDate = new DateTime(2026, 12, 5),
            GuestCount = 2, TotalPrice = 400, Status = 1,
        };
        _bookingRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(existing);
        _bookingRepo.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
        _bookingRepo.Setup(r => r.HasOverlappingBookingAsync(10,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), 7))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BookingConflictException>(() => _service.UpdateBookingAsync(new UpdateBookingDto
        {
            Id = 7,
            CheckInDate = new DateTime(2026, 12, 10),
            CheckOutDate = new DateTime(2026, 12, 15),
            GuestCount = 2,
            Status = 1,
        }));
    }

    [Fact]
    public async Task CancelBooking_SetsCancelledStatus()
    {
        var booking = new Booking
        {
            Id = 55, UserId = 4, HotelId = 1, RoomTypeId = 10,
            CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today.AddDays(2),
            GuestCount = 1, TotalPrice = 200, Status = 1,
        };
        _bookingRepo.Setup(r => r.GetByIdAsync(55)).ReturnsAsync(booking);
        _bookingRepo.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);

        await _service.CancelBookingAsync(55);

        _bookingRepo.Verify(r => r.UpdateAsync(It.Is<Booking>(b =>
            b.Status == (int)BookingStatus.Cancelled)), Times.Once);
    }

    [Fact]
    public async Task UpdateBookingStatus_InvalidCode_Throws()
    {
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Booking { Id = 1, UserId = 4 });

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateBookingStatusAsync(1, 99));
    }

    [Fact]
    public async Task CreateBooking_WritesAuditLog()
    {
        await _service.CreateBookingAsync(ValidDto());

        _logging.Verify(l => l.LogActionAsync(4, UserActionType.BookingCreated,
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

}
