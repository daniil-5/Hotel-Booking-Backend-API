namespace BookingSystem.Application.DTOs.RoomPricing;

public class UpdateRoomPricingDto
{
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
}
