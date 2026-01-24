namespace BookingSystem.Domain.Entities;

public class RoomPricing : BaseEntity
{
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
}
