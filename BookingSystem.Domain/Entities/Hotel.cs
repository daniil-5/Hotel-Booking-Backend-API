namespace BookingSystem.Domain.Entities;

public class Hotel : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public decimal Rating { get; set; }
    public decimal BasePrice { get; set; }
    public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
    public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
    public ICollection<HotelPhoto> Photos { get; set; } = new List<HotelPhoto>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
