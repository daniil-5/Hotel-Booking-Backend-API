using BookingSystem.Domain.DTOs.Booking;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Domain.Interfaces;

public interface IBookingRepository : IRepository<Booking>
{
    Task<IEnumerable<BookingDetails>> GetBookingsWithDetailsAsync(int? userId = null, int? hotelId = null, int? status = null);
    Task<object> GetUserBookingHistoryAsync(int userId);
    Task<IEnumerable<ActiveBookingDetailsDto>> GetActiveBookingsWithDetailsAsync();
    Task<bool> HasOverlappingBookingAsync(int roomTypeId, DateTime checkInDate, DateTime checkOutDate, int? excludeBookingId = null);
}
