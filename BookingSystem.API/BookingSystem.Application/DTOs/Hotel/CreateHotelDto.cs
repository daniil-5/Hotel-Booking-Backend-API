using System.ComponentModel.DataAnnotations;
using BookingSystem.Application.DTOs.HotelPhoto;

namespace BookingSystem.Application.DTOs.Hotel;

public class CreateHotelDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0, 5.0)]
    public decimal Rating { get; set; }

    [Range(0, double.MaxValue)]
    public decimal BasePrice { get; set; }



    [Required]
    public ICollection<Domain.Entities.RoomType> RoomTypes { get; set; } = new List<Domain.Entities.RoomType>();

    [Required]
    public ICollection<HotelPhotoDto> Photos { get; set; } = new List<HotelPhotoDto>();
}
