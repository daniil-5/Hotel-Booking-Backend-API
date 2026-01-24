using BookingSystem.Application.DTOs.Amenity;
using BookingSystem.Application.DTOs.HotelPhoto;
using BookingSystem.Application.DTOs.RoomType;

namespace BookingSystem.Application.DTOs.Hotel;

public class HotelDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public decimal Rating { get; set; }
    public decimal BasePrice { get; set; }
    public ICollection<RoomTypeDto> RoomTypes { get; set; } = new List<RoomTypeDto>();
    public ICollection<HotelPhotoDto> Photos { get; set; } = new List<HotelPhotoDto>();
    public ICollection<AmenityDto> Amenities { get; set; } = new List<AmenityDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
