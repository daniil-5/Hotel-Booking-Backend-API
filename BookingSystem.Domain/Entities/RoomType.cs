namespace BookingSystem.Domain.Entities;

public class RoomType : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal BasePrice { get; set; }
    public int Capacity { get; set; } = 2;
    public decimal Area { get; set; }
    public int? Floor { get; set; }
    public int Count { get; set; }

    public int BedCount { get; set; }
    public int HotelId { get; set; }
    public Hotel Hotel { get; set; }
    public ICollection<RoomPricing> Pricing { get; set; } = new List<RoomPricing>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}