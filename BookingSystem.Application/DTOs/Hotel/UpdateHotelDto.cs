using BookingSystem.Application.DTOs.Amenity;

namespace BookingSystem.Application.DTOs.Hotel;

public class UpdateHotelDto : CreateHotelDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public decimal Rating { get; set; }
    public decimal BasePrice { get; set; }
    public ICollection<Domain.Entities.RoomType> RoomTypes { get; set; } = new List<Domain.Entities.RoomType>();
    public ICollection<Domain.Entities.Hotel> Photos { get; set; } = new List<Domain.Entities.Hotel>();
    public ICollection<AmenityDto> Amenities { get; set; } = new List<AmenityDto>();
}