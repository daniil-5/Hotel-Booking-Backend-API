namespace BookingSystem.Application.DTOs.RoomPricing;

public class CreateRoomPricingDto
{
    public int RoomTypeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
}
