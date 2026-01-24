using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.DTOs.Commands;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Settings;
using BookingSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingSystem.Application.Decorators;

public class CachedBookingService : IBookingService
{
    private readonly IBookingService _bookingService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedBookingService> _logger;

    private const string BOOKING_BY_ID_KEY = "booking:id:{0}";
    private const string BOOKINGS_BY_USER_KEY = "bookings:user:{0}";
    private const string BOOKINGS_BY_HOTEL_KEY = "bookings:hotel:{0}";
    private const string BOOKINGS_BY_ROOMTYPE_KEY = "bookings:roomtype:{0}";
    private const string BOOKINGS_BY_DATERANGE_KEY = "bookings:daterange:{0}:{1}";
    private const string ALL_BOOKINGS_KEY = "bookings:all";
    private const string ROOM_AVAILABILITY_KEY = "availability:roomtype:{0}:hotel:{1}:{2}:{3}";
    private const string BOOKING_PREFIX = "booking";
    private const string BOOKINGS_PREFIX = "bookings";

    private static readonly TimeSpan BookingCacheExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AvailabilityCacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ListCacheExpiration = TimeSpan.FromMinutes(8);

    public CachedBookingService(
        IBookingService bookingService,
        ICacheService cacheService,
        ILogger<CachedBookingService> logger)
    {
        _bookingService = bookingService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Guid> CreateBookingAsync(CreateBookingDto bookingDto)
    {
        _logger.LogInformation("Initiating booking request for user {UserId}", bookingDto.UserId);

        return await _bookingService.CreateBookingAsync(bookingDto);
    }

    public async Task<BookingResponseDto?> GetBookingByTrackingIdAsync(Guid trackingId)
    {
        return await _bookingService.GetBookingByTrackingIdAsync(trackingId);
    }

    public async Task<BookingResponseDto> UpdateBookingAsync(UpdateBookingDto dto)
    {
        _logger.LogInformation("Updating booking with ID: {BookingId}", dto.Id);

        var currentBooking = await GetBookingByIdAsync(dto.Id);

        var updatedBooking = await _bookingService.UpdateBookingAsync(dto);

        await CacheBooking(updatedBooking);
        await InvalidateBookingRelatedCaches(updatedBooking);

        if (currentBooking != null &&
            (currentBooking.CheckInDate != updatedBooking.CheckInDate ||
             currentBooking.CheckOutDate != updatedBooking.CheckOutDate))
        {
            await InvalidateAvailabilityCache(updatedBooking.RoomTypeId, updatedBooking.HotelId);
        }

        _logger.LogInformation("Booking updated and cache refreshed for ID: {BookingId}", updatedBooking.Id);
        return updatedBooking;
    }

    public async Task DeleteBookingAsync(int id)
    {
        _logger.LogInformation("Deleting booking with ID: {BookingId}", id);

        var bookingToDelete = await GetBookingByIdAsync(id);

        await _bookingService.DeleteBookingAsync(id);

        await _cacheService.RemoveAsync(string.Format(BOOKING_BY_ID_KEY, id));

        if (bookingToDelete != null)
        {
            await InvalidateBookingRelatedCaches(bookingToDelete);
            await InvalidateAvailabilityCache(bookingToDelete.RoomTypeId, bookingToDelete.HotelId);
        }

        _logger.LogInformation("Booking deleted and cache invalidated for ID: {BookingId}", id);
    }

    public async Task<BookingResponseDto> GetBookingByIdAsync(int id)
    {
        var cacheKey = string.Format(BOOKING_BY_ID_KEY, id);

        var cachedBooking = await _cacheService.GetAsync<BookingResponseDto>(cacheKey);
        if (cachedBooking != null)
        {
            _logger.LogDebug("Booking found in cache for ID: {BookingId}", id);
            return cachedBooking;
        }

        _logger.LogDebug("Booking not found in cache, fetching from database for ID: {BookingId}", id);

        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking != null)
        {
            await CacheBooking(booking);
            _logger.LogDebug("Booking cached for ID: {BookingId}", id);
        }

        return booking;
    }

    public async Task<IEnumerable<BookingResponseDto>> GetAllBookingsAsync()
    {
        var cacheKey = ALL_BOOKINGS_KEY;

        var cachedBookings = await _cacheService.GetAsync<IEnumerable<BookingResponseDto>>(cacheKey);
        if (cachedBookings != null)
        {
            _logger.LogDebug("All bookings found in cache");
            return cachedBookings;
        }

        _logger.LogDebug("All bookings not found in cache, fetching from database");

        var bookings = await _bookingService.GetAllBookingsAsync();

        await _cacheService.SetAsync(cacheKey, bookings, ListCacheExpiration);

        foreach (var booking in bookings)
        {
            await CacheBooking(booking);
        }

        _logger.LogDebug("All bookings cached, count: {BookingCount}", bookings.Count());
        return bookings;
    }

    public async Task<IEnumerable<BookingResponseDto>> GetBookingsByUserIdAsync(int userId)
    {
        var cacheKey = string.Format(BOOKINGS_BY_USER_KEY, userId);

        var cachedBookings = await _cacheService.GetAsync<IEnumerable<BookingResponseDto>>(cacheKey);
        if (cachedBookings != null)
        {
            _logger.LogDebug("User bookings found in cache for user: {UserId}", userId);
            return cachedBookings;
        }

        _logger.LogDebug("User bookings not found in cache, fetching from database for user: {UserId}", userId);

        var bookings = await _bookingService.GetBookingsByUserIdAsync(userId);

        await _cacheService.SetAsync(cacheKey, bookings, ListCacheExpiration);

        foreach (var booking in bookings)
        {
            await CacheBooking(booking);
        }

        _logger.LogDebug("User bookings cached for user: {UserId}, count: {BookingCount}", userId, bookings.Count());
        return bookings;
    }

    public async Task<IEnumerable<BookingResponseDto>> GetBookingsByHotelIdAsync(int hotelId)
    {
        var cacheKey = string.Format(BOOKINGS_BY_HOTEL_KEY, hotelId);

        var cachedBookings = await _cacheService.GetAsync<IEnumerable<BookingResponseDto>>(cacheKey);
        if (cachedBookings != null)
        {
            _logger.LogDebug("Hotel bookings found in cache for hotel: {HotelId}", hotelId);
            return cachedBookings;
        }

        _logger.LogDebug("Hotel bookings not found in cache, fetching from database for hotel: {HotelId}", hotelId);

        var bookings = await _bookingService.GetBookingsByHotelIdAsync(hotelId);

        await _cacheService.SetAsync(cacheKey, bookings, ListCacheExpiration);

        foreach (var booking in bookings)
        {
            await CacheBooking(booking);
        }

        _logger.LogDebug("Hotel bookings cached for hotel: {HotelId}, count: {BookingCount}", hotelId, bookings.Count());
        return bookings;
    }

    public async Task<IEnumerable<BookingResponseDto>> GetBookingsByRoomTypeIdAsync(int roomTypeId)
    {
        var cacheKey = string.Format(BOOKINGS_BY_ROOMTYPE_KEY, roomTypeId);

        var cachedBookings = await _cacheService.GetAsync<IEnumerable<BookingResponseDto>>(cacheKey);
        if (cachedBookings != null)
        {
            _logger.LogDebug("RoomType bookings found in cache for roomType: {RoomTypeId}", roomTypeId);
            return cachedBookings;
        }

        _logger.LogDebug("RoomType bookings not found in cache, fetching from database for roomType: {RoomTypeId}", roomTypeId);

        var bookings = await _bookingService.GetBookingsByRoomTypeIdAsync(roomTypeId);

        await _cacheService.SetAsync(cacheKey, bookings, ListCacheExpiration);

        foreach (var booking in bookings)
        {
            await CacheBooking(booking);
        }

        _logger.LogDebug("RoomType bookings cached for roomType: {RoomTypeId}, count: {BookingCount}", roomTypeId, bookings.Count());
        return bookings;
    }

    public async Task<IEnumerable<BookingResponseDto>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = string.Format(BOOKINGS_BY_DATERANGE_KEY,
            startDate.ToString("yyyy-MM-dd"),
            endDate.ToString("yyyy-MM-dd"));

        var cachedBookings = await _cacheService.GetAsync<IEnumerable<BookingResponseDto>>(cacheKey);
        if (cachedBookings != null)
        {
            _logger.LogDebug("Date range bookings found in cache for range: {StartDate} - {EndDate}", startDate, endDate);
            return cachedBookings;
        }

        _logger.LogDebug("Date range bookings not found in cache, fetching from database for range: {StartDate} - {EndDate}", startDate, endDate);

        var bookings = await _bookingService.GetBookingsByDateRangeAsync(startDate, endDate);

        await _cacheService.SetAsync(cacheKey, bookings, TimeSpan.FromMinutes(5));

        foreach (var booking in bookings)
        {
            await CacheBooking(booking);
        }

        _logger.LogDebug("Date range bookings cached for range: {StartDate} - {EndDate}, count: {BookingCount}",
            startDate, endDate, bookings.Count());
        return bookings;
    }

    public async Task<bool> CheckRoomTypeAvailabilityAsync(int roomTypeId, int hotelId, DateTime checkInDate, DateTime checkOutDate, int? excludeBookingId = null)
    {
        var cacheKey = string.Format(ROOM_AVAILABILITY_KEY,
            roomTypeId,
            hotelId,
            checkInDate.ToString("yyyy-MM-dd"),
            checkOutDate.ToString("yyyy-MM-dd"));

        if (excludeBookingId.HasValue)
        {
            cacheKey += $":exclude:{excludeBookingId.Value}";
        }

        var cachedAvailabilityStr = await _cacheService.GetAsync<string>(cacheKey);
        if (!bool.TryParse(cachedAvailabilityStr, out var cachedAvailability))
        {
            _logger.LogWarning("Cached room availability string could not be parsed: {CachedValue}", cachedAvailabilityStr);
        }

        _logger.LogDebug("Room availability not found in cache, checking database for roomType: {RoomTypeId}, hotel: {HotelId}", roomTypeId, hotelId);

        var availability = await _bookingService.CheckRoomTypeAvailabilityAsync(roomTypeId, hotelId, checkInDate, checkOutDate, excludeBookingId);

        await _cacheService.SetAsync(cacheKey, availability.ToString(), AvailabilityCacheExpiration);

        _logger.LogDebug("Room availability cached for roomType: {RoomTypeId}, hotel: {HotelId}, available: {Available}",
            roomTypeId, hotelId, availability);

        return availability;
    }

    public async Task<BookingResponseDto> CancelBookingAsync(int id)
    {
        _logger.LogInformation("Cancelling booking with ID: {BookingId}", id);

        var cancelledBooking = await _bookingService.CancelBookingAsync(id);

        await CacheBooking(cancelledBooking);
        await InvalidateBookingRelatedCaches(cancelledBooking);
        await InvalidateAvailabilityCache(cancelledBooking.RoomTypeId, cancelledBooking.HotelId);

        _logger.LogInformation("Booking cancelled and cache updated for ID: {BookingId}", id);
        return cancelledBooking;
    }

    public async Task<BookingResponseDto> UpdateBookingStatusAsync(int id, int statusCode)
    {
        _logger.LogInformation("Updating booking status for ID: {BookingId} to status: {StatusCode}", id, statusCode);

        var updatedBooking = await _bookingService.UpdateBookingStatusAsync(id, statusCode);

        await CacheBooking(updatedBooking);
        await InvalidateBookingRelatedCaches(updatedBooking);

        _logger.LogInformation("Booking status updated and cache refreshed for ID: {BookingId}", id);
        return updatedBooking;
    }

    private async Task CacheBooking(BookingResponseDto booking)
    {
        if (booking == null) return;

        var cacheKey = string.Format(BOOKING_BY_ID_KEY, booking.Id);
        await _cacheService.SetAsync(cacheKey, booking, BookingCacheExpiration);
    }

    private async Task InvalidateBookingRelatedCaches(BookingResponseDto booking)
    {
        if (booking == null) return;

        var tasks = new List<Task>
        {
            _cacheService.RemoveAsync(ALL_BOOKINGS_KEY),
            _cacheService.RemoveAsync(string.Format(BOOKINGS_BY_USER_KEY, booking.UserId)),
            _cacheService.RemoveAsync(string.Format(BOOKINGS_BY_HOTEL_KEY, booking.HotelId)),
            _cacheService.RemoveAsync(string.Format(BOOKINGS_BY_ROOMTYPE_KEY, booking.RoomTypeId)),
            _cacheService.RemoveByPrefixAsync("bookings:daterange:")
        };

        await Task.WhenAll(tasks);
        _logger.LogDebug("Booking-related caches invalidated for booking ID: {BookingId}", booking.Id);
    }

    private async Task InvalidateAvailabilityCache(int roomTypeId, int hotelId)
    {
        var availabilityPattern = $"availability:roomtype:{roomTypeId}:hotel:{hotelId}:*";
        await _cacheService.RemoveByPatternAsync(availabilityPattern);

        _logger.LogDebug("Availability cache invalidated for roomType: {RoomTypeId}, hotel: {HotelId}", roomTypeId, hotelId);
    }

    public async Task InvalidateAllBookingCachesAsync()
    {
        var tasks = new List<Task>
        {
            _cacheService.RemoveByPrefixAsync(BOOKING_PREFIX),
            _cacheService.RemoveByPrefixAsync(BOOKINGS_PREFIX),
            _cacheService.RemoveByPrefixAsync("availability:")
        };

        await Task.WhenAll(tasks);
        _logger.LogInformation("All booking caches invalidated");
    }
}

