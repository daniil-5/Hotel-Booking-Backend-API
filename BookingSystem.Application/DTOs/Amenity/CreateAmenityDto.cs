using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Amenity;

public class CreateAmenityDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    [StringLength(500)]
    public string Description { get; set; }
}