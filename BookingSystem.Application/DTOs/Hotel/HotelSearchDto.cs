using BookingSystem.Application.DTOs.Amenity;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.DTOs.Hotel;

public class HotelSearchDto
{
    public string? Name { get; set; }
    public string? Location { get; set; }
    public decimal? MinRating { get; set; }
    public decimal? MaxRating { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? RoomTypeId { get; set; }
    public ICollection<AmenityDto> Amenities { get; set; } = new List<AmenityDto>();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "Rating";
    public bool SortDescending { get; set; } = true;
}