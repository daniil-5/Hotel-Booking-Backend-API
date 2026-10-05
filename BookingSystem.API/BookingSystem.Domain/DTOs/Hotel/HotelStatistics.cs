namespace BookingSystem.Domain.DTOs.Hotel
{
    public class HotelStatistics
    {
        public required int HotelId { get; set; }
        public required string HotelName { get; set; }
        public required string Location { get; set; }
        public required double Rating { get; set; }
        public required decimal BasePrice { get; set; }
        public required int TotalBookings { get; set; }
        public required int ConfirmedBookings { get; set; }
        public required int CancelledBookings { get; set; }
        public required decimal TotalRevenue { get; set; }
        public required decimal AverageBookingPrice { get; set; }
        public required int TotalRoomTypes { get; set; }
        public required int TotalPhotos { get; set; }
    }
}
