namespace BookingSystem.Application.DTOs.Booking;

public class CreateBookingDto
{
    public int RoomTypeId { get; set; }
    public int UserId { get; set; }
    public int HotelId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int GuestCount { get; set; }
}