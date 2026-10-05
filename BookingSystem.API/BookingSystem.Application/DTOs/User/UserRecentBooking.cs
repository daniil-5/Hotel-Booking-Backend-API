namespace BookingSystem.Application.DTOs.User;

public class UserRecentBooking
{
    public int BookingId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Status { get; set; }
    public decimal TotalPrice { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
}
