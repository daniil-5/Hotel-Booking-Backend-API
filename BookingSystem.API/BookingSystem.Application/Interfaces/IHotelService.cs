using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.DTOs.Hotel;

namespace BookingSystem.Application.Interfaces;

public interface IHotelService
{
    Task<HotelDto> CreateHotelAsync(CreateHotelDto hotelDto);
    Task<HotelDto> UpdateHotelAsync(UpdateHotelDto hotelDto);
    Task DeleteHotelAsync(int id);
    Task<HotelDto> GetHotelByIdAsync(int id);
    Task<IEnumerable<HotelDto>> GetAllHotelsAsync();
    Task<HotelSearchResultDto> SearchHotelsAsync(HotelSearchDto searchDto);
    Task<IEnumerable<HotelStatistics>> GetHotelsStatisticsAsync();
    Task<IEnumerable<HotelAvailability>> SearchAvailableHotelsAsync(string location, DateTime checkIn, DateTime checkOut, int guestCount);
    Task<IEnumerable<HotelRanking>> GetHotelsRankedByLocationAsync();
    Task<HotelPerformanceReport?> GetHotelPerformanceReportAsync(int hotelId);
    Task<IEnumerable<MonthlyBookingTrend>> GetMonthlyBookingTrendsAsync(int? hotelId = null, int months = 12);
    Task<IEnumerable<HotelDto>> GetHotelsOrderedByRatingAndNameAsync();
    Task<IEnumerable<HotelDto>> GetPremiumHotelsAsync();
}
