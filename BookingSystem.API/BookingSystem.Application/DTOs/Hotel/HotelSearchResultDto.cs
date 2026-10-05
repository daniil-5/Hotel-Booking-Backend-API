using BookingSystem.Application.DTOs.Hotel;

namespace BookingSystem.Application.DTOs.Hotel;

public class HotelSearchResultDto
{
    public IEnumerable<HotelDto> Hotels { get; set; } = new List<HotelDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
