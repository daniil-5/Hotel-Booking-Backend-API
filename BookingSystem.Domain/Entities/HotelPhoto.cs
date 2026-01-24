namespace BookingSystem.Domain.Entities;

public class HotelPhoto : BaseEntity
{
    public int HotelId { get; set; }
    public Hotel Hotel { get; set; }
    public string Url { get; set; }
    public string PublicId { get; set; }
    public string Description { get; set; }
    public bool IsMain { get; set; }
}
