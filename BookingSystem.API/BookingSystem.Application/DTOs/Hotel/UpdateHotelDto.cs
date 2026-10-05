using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Hotel;

public class UpdateHotelDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Location { get; set; }
    public required double Rating { get; set; }
    public required decimal BasePrice { get; set; }
}
