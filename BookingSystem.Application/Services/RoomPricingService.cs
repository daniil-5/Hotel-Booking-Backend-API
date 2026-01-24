using BookingSystem.Application.DTOs.RoomPricing;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Mappers;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;

namespace BookingSystem.Application.Services;

public class RoomPricingService : IRoomPricingService
{
    private readonly IRepository<RoomPricing> _roomPricingRepository;

    public RoomPricingService(IRepository<RoomPricing> roomPricingRepository)
    {
        _roomPricingRepository = roomPricingRepository;
    }

    public async Task<RoomPricingDto> CreateRoomPricingAsync(CreateRoomPricingDto pricingDto)
    {
        var roomPricing = new RoomPricing
        {
            RoomTypeId = pricingDto.RoomTypeId,
            Date = pricingDto.Date,
            Price = pricingDto.Price
        };

        await _roomPricingRepository.AddAsync(roomPricing);
        return roomPricing.ToDto();
    }

    public async Task<RoomPricingDto> UpdateRoomPricingAsync(UpdateRoomPricingDto pricingDto)
    {
        var existingPricing = await _roomPricingRepository.GetByIdAsync(pricingDto.Id);

        existingPricing.RoomTypeId = pricingDto.RoomTypeId;
        existingPricing.Date = pricingDto.Date;
        existingPricing.Price = pricingDto.Price;

        await _roomPricingRepository.UpdateAsync(existingPricing);
        return existingPricing.ToDto();
    }

    public async Task DeleteRoomPricingAsync(int id)
    {
        await _roomPricingRepository.DeleteAsync(id);
    }

    public async Task<RoomPricingDto> GetRoomPricingByIdAsync(int id)
    {
        var pricing = await _roomPricingRepository.GetByIdAsync(id);
        return pricing?.ToDto();
    }

    public async Task<IEnumerable<RoomPricingDto>> GetAllRoomPricingsAsync()
    {
        var pricings = await _roomPricingRepository.GetAllAsync();
        return pricings.Select(p => p.ToDto()).ToList();
    }
}
