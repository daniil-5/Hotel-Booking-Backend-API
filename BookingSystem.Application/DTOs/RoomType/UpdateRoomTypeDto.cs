using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.RoomType;

public class UpdateRoomTypeDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; }

    [Required(ErrorMessage = "Bed count is required")]
    [Range(1, 10, ErrorMessage = "Bed count must be between 1 and 10")]
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Area is required")]
    [Range(1, 1000, ErrorMessage = "Area must be between 1 and 1000 square meters")]
    public decimal Area { get; set; }

    public int? Floor { get; set; }

    public int Count { get; set; }

    [Required(ErrorMessage = "Hotel ID is required")]
    public int HotelId { get; set; }

    [Required(ErrorMessage = "Base price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Base price must be a positive value")]
    public decimal BasePrice { get; set; }
}
