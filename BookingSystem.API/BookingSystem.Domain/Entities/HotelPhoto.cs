using System.ComponentModel.DataAnnotations.Schema;

namespace BookingSystem.Domain.Entities;

[Table("hotel_photos")]
public class HotelPhoto : BaseEntity
{
    [Column("hotel_id")]
    public int HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;
    [Column("url")]
    public string Url { get; set; } = null!;
    [Column("public_id")]
    public string? PublicId { get; set; } = null!;
    [Column("description")]
    public string? Description { get; set; } = null!;
    [Column("is_main")]
    public bool IsMain { get; set; }
}
