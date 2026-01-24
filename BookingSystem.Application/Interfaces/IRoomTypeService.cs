using BookingSystem.Application.DTOs.RoomType;

namespace BookingSystem.Application.Interfaces;

public interface IRoomTypeService
{
   
    Task<RoomTypeDto> CreateRoomTypeAsync(CreateRoomTypeDto roomTypeDto);
    Task<RoomTypeDto> UpdateRoomTypeAsync(UpdateRoomTypeDto roomTypeDto);
    Task DeleteRoomTypeAsync(int id);
    Task<RoomTypeDto> GetRoomTypeByIdAsync(int id);
    Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync();
    Task<IEnumerable<RoomTypeDto>> GetRoomTypesByHotelIdAsync(int hotelId);
    
}