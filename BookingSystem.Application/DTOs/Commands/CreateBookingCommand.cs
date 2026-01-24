namespace BookingSystem.Application.DTOs.Commands;

public class CreateBookingCommand
{
    public Guid TrackingId { get; set; } = Guid.NewGuid();
    public int UserId { get; set; }
    public int HotelId { get; set; }
    public int RoomTypeId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int GuestCount { get; set; }
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
}