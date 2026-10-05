namespace BookingSystem.Application.DTOs.RoomPricing;

public class RoomPricingDto
{
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
