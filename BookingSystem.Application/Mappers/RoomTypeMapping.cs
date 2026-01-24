using BookingSystem.Application.DTOs.RoomType;

namespace BookingSystem.Application.Mappers
{
    public static class RoomTypeMapping
    {
        public static RoomTypeDto ToDto(this Domain.Entities.RoomType roomType)
        {
            if (roomType == null) return null;

            return new RoomTypeDto
            {
                Id = roomType.Id,
                Name = roomType.Name,
                Description = roomType.Description,
                BasePrice = roomType.BasePrice,
                Capacity = roomType.Capacity,
                Area = roomType.Area,
                Floor = roomType.Floor,
                Count = roomType.Count,
                HotelId = roomType.HotelId
            };
        }
        public static Domain.Entities.RoomType ToEntity(this CreateRoomTypeDto dto)
        {
            if (dto == null) return null;

            return new Domain.Entities.RoomType
            {
                Name = dto.Name,
                Description = dto.Description,
                Capacity = dto.Capacity,
                BasePrice = dto.BasePrice,
                Area = dto.Area,
                Floor = dto.Floor,
                Count = dto.Count,
                HotelId = dto.HotelId
            };
        }
    }
}