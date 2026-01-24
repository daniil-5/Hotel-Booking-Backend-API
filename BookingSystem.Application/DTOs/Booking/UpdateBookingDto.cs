namespace BookingSystem.Application.DTOs.Booking;

public class UpdateBookingDto : CreateBookingDto
{
    public int Id { get; set; }
    public int Status { get; set; }
}