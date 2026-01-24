namespace BookingSystem.Application.DTOs.RoomType;

public class RoomTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public decimal Area { get; set; }
    public int Count { get; set; }
    public int? Floor { get; set; }
    public int HotelId { get; set; }
}
