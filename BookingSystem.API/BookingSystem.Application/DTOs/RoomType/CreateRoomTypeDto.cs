using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.RoomType;

public class CreateRoomTypeDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public int? Floor { get; set; }
    public int HotelId { get; set; }
}
