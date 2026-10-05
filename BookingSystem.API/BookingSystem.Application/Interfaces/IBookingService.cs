using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Domain.DTOs.Booking;

namespace BookingSystem.Application.Interfaces;

public interface IBookingService
{
    Task<BookingResponseDto> GetBookingByIdAsync(int id);
    Task<IEnumerable<BookingResponseDto>> GetAllBookingsAsync();
    Task<BookingResponseDto> CreateBookingAsync(CreateBookingDto bookingDto);
    Task<BookingResponseDto> UpdateBookingAsync(UpdateBookingDto dto);
    Task DeleteBookingAsync(int id);
    Task<IEnumerable<BookingResponseDto>> GetBookingsByUserIdAsync(int userId);
    Task<bool> CheckRoomTypeAvailabilityAsync(int roomTypeId, int hotelId, DateTime checkInDate, DateTime checkOutDate, int? excludeBookingId = null);
    Task<BookingResponseDto> CancelBookingAsync(int id);
    Task<IEnumerable<BookingResponseDto>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<BookingResponseDto> UpdateBookingStatusAsync(int id, int statusCode);
    Task<IEnumerable<BookingResponseDto>> GetBookingsByRoomTypeIdAsync(int roomTypeId);
    Task<IEnumerable<BookingResponseDto>> GetBookingsByHotelIdAsync(int hotelId);
    Task<IEnumerable<BookingDetails>> GetBookingsWithDetailsAsync(int? userId = null, int? hotelId = null, int? status = null);
    Task<UserBookingHistory> GetUserBookingHistoryAsync(int userId);
    Task<IEnumerable<BookingSystem.Domain.DTOs.Booking.ActiveBookingDetailsDto>> GetActiveBookingsWithDetailsAsync();
}
