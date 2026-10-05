namespace BookingSystem.Domain.DTOs.Booking;

public class ActiveBookingDetailsDto
{
    // Booking Details
    public int BookingId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int GuestCount { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } // e.g., "Pending", "Confirmed"
    public DateTime CreatedAt { get; set; }

    // User Details
    public int UserId { get; set; }
    public string Username { get; set; }
    public string UserEmail { get; set; }

    // Hotel Details
    public int HotelId { get; set; }
    public string HotelName { get; set; }
    public string HotelLocation { get; set; }

    // Room Type Details
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; }
    public int RoomTypeCapacity { get; set; }
    public decimal RoomTypeBasePrice { get; set; }
}
