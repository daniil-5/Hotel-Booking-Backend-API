using BookingSystem.Domain.DTOs.RoomType;

namespace BookingSystem.Domain.DTOs.Hotel
{
    public class HotelPerformanceReport
    {
        public required int HotelId { get; set; }
        public required string HotelName { get; set; }
        public required string Location { get; set; }
        public required double Rating { get; set; }
        public required decimal BasePrice { get; set; }
        public required int TotalBookings { get; set; }
        public required decimal TotalRevenue { get; set; }
        public required decimal AverageBookingValue { get; set; }
        public required int UniqueCustomers { get; set; }
        public required int TotalRoomTypes { get; set; }
        public required List<RoomTypePerformance> RoomTypePerformance { get; set; }
    }
}
