using System.ComponentModel.DataAnnotations.Schema;

namespace BookingSystem.Domain.Entities;

[Table("room_types")]
public class RoomType : BaseEntity
{
    [Column("hotel_id")]
    public required int HotelId { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("description")]
    public required string Description { get; set; }

    [Column("capacity")]
    public required int Capacity { get; set; }

    [Column("base_price")]
    public required decimal BasePrice { get; set; }

    [Column("area")]
    public required int Area { get; set; }

    public required Hotel Hotel { get; set; } = null!;
}
