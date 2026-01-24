using BookingSystem.Application.DTOs.HotelPhoto;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Mappers
{
    public static class HotelPhotoMapping
    {
        public static HotelPhotoDto ToDto(this HotelPhoto photo)
        {
            if (photo == null) return null;

            return new HotelPhotoDto
            {
                Id = photo.Id,
                HotelId = photo.HotelId,
                Url = photo.Url,
                PublicId = photo.PublicId,
                Description = photo.Description ?? "No description",
                IsMain = photo.IsMain,
                CreatedAt = photo.CreatedAt
            };
        }
        public static HotelPhoto ToEntity(this CreateHotelPhotoDto dto)
        {
            if (dto == null) return null;

            return new HotelPhoto
            {
                HotelId = dto.HotelId,
                Url = dto.Url,
                PublicId = dto.PublicId,
                Description = dto.Description,
                IsMain = dto.IsMain
            };
        }
    }
}