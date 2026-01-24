using BookingSystem.Application.DTOs.Amenity;
using BookingSystem.Application.DTOs.Hotel;
using BookingSystem.Application.DTOs.HotelPhoto;
using BookingSystem.Application.DTOs.RoomType;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Mappers
{
    public static class HotelMapping
    {
        public static HotelDto ToDto(this Hotel hotel)
        {
            if (hotel == null) return null;

            return new HotelDto
            {
                Id = hotel.Id,
                Name = hotel.Name,
                Description = hotel.Description,
                Location = hotel.Location,
                Rating = hotel.Rating,
                BasePrice = hotel.BasePrice,
                
                Amenities = hotel.Amenities?
                    .Select(a => a.ToDto())
                    .ToList() ?? new List<AmenityDto>(),
                
                RoomTypes = hotel.RoomTypes?
                    .Where(rt => !rt.IsDeleted)
                    .Select(rt => rt.ToDto())
                    .ToList() ?? new List<RoomTypeDto>(),
                
                Photos = hotel.Photos?
                    .Where(p => !p.IsDeleted)
                    .Select(p => p.ToDto())
                    .ToList() ?? new List<HotelPhotoDto>(),
                
                CreatedAt = hotel.CreatedAt,
                UpdatedAt = hotel.UpdatedAt,
            };
        }
        public static Hotel ToEntity(this CreateHotelDto dto)
        {
            if (dto == null) return null;

            return new Hotel
            {
                Name = dto.Name,
                Description = dto.Description,
                Location = dto.Location,
                Rating = dto.Rating,
                BasePrice = dto.BasePrice,
                Amenities = new List<Amenity>() 
            };
        }
    }
}