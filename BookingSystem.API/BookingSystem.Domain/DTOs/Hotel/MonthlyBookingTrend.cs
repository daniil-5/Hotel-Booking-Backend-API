namespace BookingSystem.Domain.DTOs.Hotel
{
    public class MonthlyBookingTrend
    {
        public required int HotelId { get; set; }
        public required string HotelName { get; set; }
        public required DateTime Month { get; set; }
        public required int BookingCount { get; set; }
        public required decimal Revenue { get; set; }
        public required decimal AverageBookingValue { get; set; }
        public required int UniqueCustomers { get; set; }
        public required int TotalGuests { get; set; }
    }
}
