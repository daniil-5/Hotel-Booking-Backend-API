using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.DTOs.Booking;

public class BookingDetails
{
    public required int Id { get; set; }
    public required int UserId { get; set; }
    public required int HotelId { get; set; }
    public required int RoomTypeId { get; set; }
    public required int GuestCount { get; set; }
    public required DateTime CheckInDate { get; set; }
    public required DateTime CheckOutDate { get; set; }
    public required decimal TotalPrice { get; set; }
    public required string Username { get; set; }
    public required string UserEmail { get; set; }
    public required string FullName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string HotelName { get; set; }
    public required string HotelLocation { get; set; }
    public required double HotelRating { get; set; }
    public required string RoomTypeName { get; set; }
    public required string RoomTypeDescription { get; set; }
    public required int RoomTypeCapacity { get; set; }
    public required string MainPhotoUrl { get; set; }
    public required BookingStatus Status { get; set; }
}
