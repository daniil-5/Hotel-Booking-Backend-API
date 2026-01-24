using System.ComponentModel.DataAnnotations;
using BookingSystem.Application.DTOs.Amenity;
using BookingSystem.Application.DTOs.HotelPhoto;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.DTOs.Hotel;

public class CreateHotelDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; }

    [Range(0, 5.0)]
    public decimal Rating { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal BasePrice { get; set; }
    
    [Required]
    public ICollection<AmenityDto> Amenities { get; set; } = new List<AmenityDto>();
    
    public ICollection<Domain.Entities.RoomType> RoomTypes { get; set; } = new List<Domain.Entities.RoomType>();
    
    public ICollection<HotelPhotoDto> Photos { get; set; } = new List<HotelPhotoDto>();
}