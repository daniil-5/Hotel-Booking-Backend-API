namespace BookingSystem.Application.DTOs.Booking;

public class LocationStatistic
{
    public string Location { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal TotalSpent { get; set; }
}
