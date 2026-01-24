using BookingSystem.Application.DTOs.RoomPricing;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Mappers
{
    public static class RoomPricingMapping
    {
        public static RoomPricingDto ToDto(this RoomPricing pricing)
        {
            if (pricing == null) return null;

            return new RoomPricingDto
            {
                Id = pricing.Id,
                RoomTypeId = pricing.RoomTypeId,
                Date = pricing.Date,
                Price = pricing.Price,
                CreatedAt = pricing.CreatedAt,
                UpdatedAt = pricing.UpdatedAt
            };
        }
        public static RoomPricing ToEntity(this CreateRoomPricingDto dto)
        {
            if (dto == null) return null;

            return new RoomPricing
            {
                RoomTypeId = dto.RoomTypeId,
                Date = dto.Date,
                Price = dto.Price
            };
        }
    }
}