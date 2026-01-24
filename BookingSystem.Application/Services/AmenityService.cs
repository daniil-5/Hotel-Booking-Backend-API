using BookingSystem.Application.DTOs.Amenity;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Mappers;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;

namespace BookingSystem.Application.Services;

public class AmenityService : IAmenityService
{
    private readonly IRepository<Amenity> _amenityRepository;

    public AmenityService(IRepository<Amenity> amenityRepository)
    {
        _amenityRepository = amenityRepository;
    }

    public async Task<AmenityDto> CreateAmenityAsync(CreateAmenityDto amenityDto)
    {
        var existing = await _amenityRepository.GetAllAsync(a => a.Name.ToLower() == amenityDto.Name.ToLower());
        if (existing.Any())
        {
            throw new InvalidOperationException($"Amenity '{amenityDto.Name}' already exists.");
        }

        var amenity = amenityDto.ToEntity();

        await _amenityRepository.AddAsync(amenity);

        return amenity.ToDto();
    }

    public async Task<AmenityDto> UpdateAmenityAsync(UpdateAmenityDto amenityDto)
    {
        var existingAmenity = await _amenityRepository.GetByIdAsync(amenityDto.Id);
        if (existingAmenity == null)
            throw new KeyNotFoundException($"Amenity with ID {amenityDto.Id} not found");

        existingAmenity.Name = amenityDto.Name;
        existingAmenity.Description = amenityDto.Description;

        await _amenityRepository.UpdateAsync(existingAmenity);

        return existingAmenity.ToDto();
    }

    public async Task DeleteAmenityAsync(int id)
    {
        var amenity = await _amenityRepository.GetByIdAsync(id);
        if (amenity == null)
            throw new KeyNotFoundException($"Amenity with ID {id} not found");

        await _amenityRepository.DeleteAsync(id);
    }

    public async Task<AmenityDto> GetAmenityByIdAsync(int id)
    {
        var amenity = await _amenityRepository.GetByIdAsync(id);
        return amenity?.ToDto();
    }

    public async Task<IEnumerable<AmenityDto>> GetAllAmenitiesAsync()
    {
        var amenities = await _amenityRepository.GetAllAsync();
        return amenities.Select(a => a.ToDto()).ToList();
    }
}
