using BookingSystem.Application.Decorators;
using BookingSystem.Domain.DTOs.Booking;
using BookingSystem.Application.Exceptions;
using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.DTOs.Hotel;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookingSystem.Tests;

/// <summary>
/// Юнит-тесты кэширующих декораторов: попадание/промах кэша,
/// инвалидация при изменениях, блокировка входа после неудачных попыток.
/// </summary>
public class CacheDecoratorTests
{
    private static (CachedHotelService Svc, Mock<IHotelService> Inner, Mock<ICacheService> Cache) HotelDecorator()
    {
        var inner = new Mock<IHotelService>();
        var cache = new Mock<ICacheService>();
        var publisher = new Mock<ICacheInvalidationPublisher>();
        var logger = new Mock<ILogger<CachedHotelService>>();
        return (new CachedHotelService(inner.Object, cache.Object, logger.Object, publisher.Object), inner, cache);
    }

    private static HotelDto Hotel(int id = 1) => new()
    { Id = id, Name = "H", Description = "", Location = "Минск", Rating = 4.5m, BasePrice = 100m };

    [Fact]
    public async Task GetAllHotels_CachesResult_AndSecondCallHitsCache()
    {
        var (svc, inner, cache) = HotelDecorator();
        var data = new List<HotelDto> { Hotel() };
        inner.Setup(i => i.GetAllHotelsAsync()).ReturnsAsync(data);
        cache.Setup(c => c.GetAsync<IEnumerable<HotelDto>>(It.IsAny<string>()))
             .ReturnsAsync((IEnumerable<HotelDto>?)null);

        var first = await svc.GetAllHotelsAsync();
        Assert.Single(first);
        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<IEnumerable<HotelDto>>(), It.IsAny<TimeSpan?>()), Times.Once);

        // второй вызов обслуживается из кэша, сервис не вызывается повторно
        cache.Setup(c => c.GetAsync<IEnumerable<HotelDto>>(It.IsAny<string>())).ReturnsAsync(data);
        inner.Invocations.Clear();
        var second = await svc.GetAllHotelsAsync();
        Assert.Single(second);
        inner.Verify(i => i.GetAllHotelsAsync(), Times.Never);
    }

    [Fact]
    public async Task GetHotelById_CachesHotel()
    {
        var (svc, inner, cache) = HotelDecorator();
        var dto = Hotel(7);
        inner.Setup(i => i.GetHotelByIdAsync(7)).ReturnsAsync(dto);
        cache.Setup(c => c.GetAsync<HotelDto>(It.IsAny<string>())).ReturnsAsync((HotelDto?)null);

        var result = await svc.GetHotelByIdAsync(7);

        Assert.Equal(7, result.Id);
        cache.Verify(c => c.SetAsync(It.IsAny<string>(), dto, It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task SearchHotels_CachesByParameters()
    {
        var (svc, inner, cache) = HotelDecorator();
        var search = new HotelSearchDto { Location = "Минск", PageNumber = 1, PageSize = 10 };
        var result = new HotelSearchResultDto { TotalCount = 3 };
        inner.Setup(i => i.SearchHotelsAsync(search)).ReturnsAsync(result);
        cache.Setup(c => c.GetAsync<HotelSearchResultDto>(It.IsAny<string>())).ReturnsAsync((HotelSearchResultDto?)null);

        var got = await svc.SearchHotelsAsync(search);

        Assert.Equal(3, got.TotalCount);
        cache.Verify(c => c.SetAsync(It.IsAny<string>(), result, It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteHotel_RemovesCache()
    {
        var (svc, inner, cache) = HotelDecorator();
        inner.Setup(i => i.GetHotelByIdAsync(9)).ReturnsAsync(Hotel(9));

        await svc.DeleteHotelAsync(9);

        cache.Verify(c => c.RemoveAsync(It.Is<string>(k => k.Contains("hotel"))), Times.AtLeastOnce);
        cache.Verify(c => c.RemoveAsync("hotels:all"), Times.Once);
    }

    // ---------- кэш бронирований ----------

    private static (CachedBookingService Svc, Mock<IBookingService> Inner, Mock<ICacheService> Cache) BookingDecorator()
    {
        var inner = new Mock<IBookingService>();
        var cache = new Mock<ICacheService>();
        var publisher = new Mock<ICacheInvalidationPublisher>();
        var logger = new Mock<ILogger<CachedBookingService>>();
        return (new CachedBookingService(inner.Object, cache.Object, logger.Object, publisher.Object), inner, cache);
    }

    [Fact]
    public async Task CreateBooking_InvalidatesUserAndHotelCaches()
    {
        var (svc, inner, cache) = BookingDecorator();
        var dto = new CreateBookingDto
        { HotelId = 1, RoomTypeId = 2, UserId = 4, CheckInDate = new DateTime(2030, 1, 1), CheckOutDate = new DateTime(2030, 1, 3), GuestCount = 1 };
        inner.Setup(i => i.CreateBookingAsync(dto)).ReturnsAsync(new BookingResponseDto { Id = 55 });

        await svc.CreateBookingAsync(dto);

        cache.Verify(c => c.RemoveByPrefixAsync("bookings:daterange:"), Times.Once);
        cache.Verify(c => c.RemoveByPrefixAsync("booking_details:"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetBookingById_HitsCacheWithoutService()
    {
        var (svc, inner, cache) = BookingDecorator();
        var cached = new BookingResponseDto { Id = 33 };
        cache.Setup(c => c.GetAsync<BookingResponseDto>(It.IsAny<string>())).ReturnsAsync(cached);

        var result = await svc.GetBookingByIdAsync(33);

        Assert.Equal(33, result.Id);
        inner.Verify(i => i.GetBookingByIdAsync(It.IsAny<int>()), Times.Never);
    }

    // ---------- блокировка входа ----------

    private static CachedUserService UserDecorator(out Mock<IUserService> inner, out Mock<ICacheService> cache)
    {
        inner = new Mock<IUserService>();
        cache = new Mock<ICacheService>();
        var publisher = new Mock<ICacheInvalidationPublisher>();
        var logger = new Mock<ILogger<CachedUserService>>();
        return new CachedUserService(inner.Object, cache.Object, logger.Object, publisher.Object);
    }

    [Fact]
    public async Task VerifyPassword_Blacklisted_ThrowsBanned()
    {
        var svc = UserDecorator(out var inner, out var cache);
        cache.Setup(c => c.ExistsAsync("auth:blacklist:a@b.c")).ReturnsAsync(true);
        cache.Setup(c => c.GetTtlAsync("auth:blacklist:a@b.c")).ReturnsAsync(TimeSpan.FromMinutes(5));

        var ex = await Assert.ThrowsAsync<AccountBannedException>(
            () => svc.VerifyUserPasswordAsync("a@b.c", "x"));
        Assert.Contains("5", ex.Message);
        inner.Verify(i => i.VerifyUserPasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task VerifyPassword_ThirdFailure_CreatesBlacklist()
    {
        var svc = UserDecorator(out var inner, out var cache);
        inner.Setup(i => i.VerifyUserPasswordAsync("a@b.c", "bad")).ReturnsAsync(false);
        cache.Setup(c => c.IncrementAsync("auth:attempts:a@b.c", It.IsAny<TimeSpan?>())).ReturnsAsync(3);

        var ok = await svc.VerifyUserPasswordAsync("a@b.c", "bad");

        Assert.False(ok);
        cache.Verify(c => c.SetAsync("auth:blacklist:a@b.c", "blocked", It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task VerifyPassword_Success_RemovesBlacklist()
    {
        var svc = UserDecorator(out var inner, out var cache);
        inner.Setup(i => i.VerifyUserPasswordAsync("a@b.c", "good")).ReturnsAsync(true);

        Assert.True(await svc.VerifyUserPasswordAsync("a@b.c", "good"));
        cache.Verify(c => c.RemoveAsync("auth:blacklist:a@b.c"), Times.Once);
    }
}

/// <summary>
/// Покрытие остальных запросов CachedBookingService: кэш-промах/попадание,
/// инвалидации при отмене и смене статуса.
/// </summary>
public class CachedBookingQueryTests
{
    private static (CachedBookingService Svc, Mock<IBookingService> Inner, Mock<ICacheService> Cache) Make()
    {
        var inner = new Mock<IBookingService>();
        var cache = new Mock<ICacheService>();
        var publisher = new Mock<ICacheInvalidationPublisher>();
        var logger = new Mock<ILogger<CachedBookingService>>();
        return (new CachedBookingService(inner.Object, cache.Object, logger.Object, publisher.Object), inner, cache);
    }

    [Fact]
    public async Task GetAllBookings_CachesAndHits()
    {
        var (svc, inner, cache) = Make();
        var data = new List<BookingResponseDto> { new() { Id = 1 } };
        inner.Setup(i => i.GetAllBookingsAsync()).ReturnsAsync(data);
        cache.Setup(c => c.GetAsync<IEnumerable<BookingResponseDto>>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<BookingResponseDto>?)null);

        var first = await svc.GetAllBookingsAsync();
        Assert.Single(first);

        cache.Setup(c => c.GetAsync<IEnumerable<BookingResponseDto>>(It.IsAny<string>())).ReturnsAsync(data);
        inner.Invocations.Clear();
        await svc.GetAllBookingsAsync();
        inner.Verify(i => i.GetAllBookingsAsync(), Times.Never);
    }

    [Fact]
    public async Task QueriesByUserHotelRoomTypeDate_CacheResults()
    {
        var (svc, inner, cache) = Make();
        var data = new List<BookingResponseDto> { new() { Id = 2, UserId = 4, HotelId = 1, RoomTypeId = 10 } };
        cache.Setup(c => c.GetAsync<IEnumerable<BookingResponseDto>>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<BookingResponseDto>?)null);
        inner.Setup(i => i.GetBookingsByUserIdAsync(It.IsAny<int>())).ReturnsAsync(data);
        inner.Setup(i => i.GetBookingsByHotelIdAsync(It.IsAny<int>())).ReturnsAsync(data);
        inner.Setup(i => i.GetBookingsByRoomTypeIdAsync(It.IsAny<int>())).ReturnsAsync(data);
        inner.Setup(i => i.GetBookingsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(data);

        Assert.Single(await svc.GetBookingsByUserIdAsync(4));
        Assert.Single(await svc.GetBookingsByHotelIdAsync(1));
        Assert.Single(await svc.GetBookingsByRoomTypeIdAsync(10));
        Assert.Single(await svc.GetBookingsByDateRangeAsync(DateTime.Today, DateTime.Today.AddDays(5)));

        inner.Verify(i => i.GetBookingsByUserIdAsync(4), Times.Once);
        inner.Verify(i => i.GetBookingsByHotelIdAsync(1), Times.Once);
        inner.Verify(i => i.GetBookingsByRoomTypeIdAsync(10), Times.Once);
        inner.Verify(i => i.GetBookingsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task CancelAndUpdateStatus_InvalidateCaches()
    {
        var (svc, inner, cache) = Make();
        inner.Setup(i => i.CancelBookingAsync(7)).ReturnsAsync(new BookingResponseDto { Id = 7, UserId = 4, HotelId = 1, RoomTypeId = 2 });
        inner.Setup(i => i.UpdateBookingStatusAsync(8, 3)).ReturnsAsync(new BookingResponseDto { Id = 8, UserId = 4, HotelId = 1, RoomTypeId = 2 });

        await svc.CancelBookingAsync(7);
        await svc.UpdateBookingStatusAsync(8, 3);

        cache.Verify(c => c.RemoveAsync(It.Is<string>(k => k.Contains("booking"))), Times.AtLeast(2));
        cache.Verify(c => c.RemoveByPrefixAsync("booking_details:"), Times.AtLeast(2));
    }

    [Fact]
    public async Task QueriesWithDetails_ProxyThrough()
    {
        var (svc, inner, cache) = Make();
        cache.Setup(c => c.GetAsync<IEnumerable<BookingDetails>>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<BookingDetails>?)null);
        inner.Setup(i => i.GetBookingsWithDetailsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<BookingDetails>());

        var got = await svc.GetBookingsWithDetailsAsync();

        Assert.NotNull(got);
        inner.Verify(i => i.GetBookingsWithDetailsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task CheckAvailability_Proxies()
    {
        var (svc, inner, _) = Make();
        inner.Setup(i => i.CheckRoomTypeAvailabilityAsync(10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(true);

        Assert.True(await svc.CheckRoomTypeAvailabilityAsync(10, 1, DateTime.Today, DateTime.Today.AddDays(1)));
    }
}
