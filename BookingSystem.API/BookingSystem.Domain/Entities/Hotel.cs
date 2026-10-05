using System.ComponentModel.DataAnnotations.Schema;

namespace BookingSystem.Domain.Entities;

[Table("hotels")]
public class Hotel : BaseEntity
{
    [Column("name")]
    public string Name { get; set; } = null!;
    [Column("description")]
    public string Description { get; set; } = null!;
    [Column("location")]
    public string Location { get; set; } = null!;
    [Column("rating")]
    public double Rating { get; set; }
    [Column("base_price")]
    public decimal BasePrice { get; set; }

    public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
    public ICollection<HotelPhoto> Photos { get; set; } = new List<HotelPhoto>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
