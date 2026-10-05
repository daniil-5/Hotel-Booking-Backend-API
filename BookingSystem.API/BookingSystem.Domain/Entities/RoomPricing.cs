using System.ComponentModel.DataAnnotations.Schema;

namespace BookingSystem.Domain.Entities;

[Table("room_pricings")]
public class RoomPricing : BaseEntity
{
    [Column("room_type_id")]
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;
    [Column("date")]
    public DateTime Date { get; set; }
    [Column("price")]
    public decimal Price { get; set; }
}
