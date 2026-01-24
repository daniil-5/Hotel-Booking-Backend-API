using BookingSystem.Application.DTOs.Amenity;

namespace BookingSystem.Application.Interfaces;

public interface IAmenityService
{
    Task<AmenityDto> CreateAmenityAsync(CreateAmenityDto amenityDto);
    Task<AmenityDto> UpdateAmenityAsync(UpdateAmenityDto amenityDto);
    Task DeleteAmenityAsync(int id);
    Task<AmenityDto> GetAmenityByIdAsync(int id);
    Task<IEnumerable<AmenityDto>> GetAllAmenitiesAsync();
}
