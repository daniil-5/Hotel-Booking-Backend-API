namespace BookingSystem.Domain.Entities;

public class Amenity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
}