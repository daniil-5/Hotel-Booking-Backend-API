using BookingSystem.Domain.Enums;
namespace BookingSystem.Domain.Entities;

public class Booking : BaseEntity
{
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public Hotel Hotel { get; set; }
    public int HotelId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int GuestCount { get; set; }
    public decimal TotalPrice { get; set; }
    public int Status { get; set; } = (int)BookingStatus.Pending;
    public Guid TrackingId { get; set; }
}
