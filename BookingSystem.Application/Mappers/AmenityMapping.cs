using BookingSystem.Application.DTOs.Amenity;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Mappers
{
    public static class AmenityMapping
    {
        public static AmenityDto ToDto(this Amenity amenity)
        {
            if (amenity == null) return null;

            return new AmenityDto
            {
                Id = amenity.Id,
                Name = amenity.Name,
                Description = amenity.Description
            };
        }

        public static Amenity ToEntity(this CreateAmenityDto dto)
        {
            if (dto == null) return null;

            return new Amenity
            {
                Name = dto.Name,
                Description = dto.Description
            };
        }
    }
}