namespace BookingSystem.Application.DTOs.Booking;

public class BookingResponseDto
{
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    public int UserId { get; set; }
    public int HotelId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int GuestCount { get; set; }
    public decimal TotalPrice { get; set; }
    public int Status { get; set; }
    public Guid TrackingId { get; set; }
}
