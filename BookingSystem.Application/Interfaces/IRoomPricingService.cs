using BookingSystem.Application.DTOs.RoomPricing;

namespace BookingSystem.Application.Interfaces;

public interface IRoomPricingService
{
    Task<RoomPricingDto> CreateRoomPricingAsync(CreateRoomPricingDto pricingDto);
    Task<RoomPricingDto> UpdateRoomPricingAsync(UpdateRoomPricingDto pricingDto);
    Task DeleteRoomPricingAsync(int id);
    Task<RoomPricingDto> GetRoomPricingByIdAsync(int id);
    Task<IEnumerable<RoomPricingDto>> GetAllRoomPricingsAsync();
}