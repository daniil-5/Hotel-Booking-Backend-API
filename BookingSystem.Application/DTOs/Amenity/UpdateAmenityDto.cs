using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Amenity;

public class UpdateAmenityDto : CreateAmenityDto
{
    [Required]
    public int Id { get; set; }
}
