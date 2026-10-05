using BookingSystem.Application.DTOs.HotelPhoto;

namespace BookingSystem.Application.DTOs.Hotel;

public class HotelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal Rating { get; set; }

    public decimal BasePrice { get; set; }

    public ICollection<Domain.Entities.RoomType> RoomTypes { get; set; } = new List<Domain.Entities.RoomType>();

    public ICollection<HotelPhotoDto> Photos { get; set; } = new List<HotelPhotoDto>();


    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
