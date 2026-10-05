using System.ComponentModel.DataAnnotations.Schema;
using BookingSystem.Domain.Enums;
namespace BookingSystem.Domain.Entities;

[Table("bookings")]
public class Booking : BaseEntity
{
    [Column("room_type_id")]
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;
    [Column("user_id")]
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public Hotel Hotel { get; set; } = null!;
    [Column("hotel_id")]
    public int HotelId { get; set; }
    [Column("check_in_date")]
    public DateTime CheckInDate { get; set; }
    [Column("check_out_date")]
    public DateTime CheckOutDate { get; set; }
    [Column("guest_count")]
    public int GuestCount { get; set; }
    [Column("total_price")]
    public decimal TotalPrice { get; set; }
    [Column("status")]
    public int Status { get; set; } = (int)BookingStatus.Pending;
}
