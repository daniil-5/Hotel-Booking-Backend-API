using BookingSystem.Application.DTOs.RoomPricing;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.Application.Services;

public class RoomPricingService : IRoomPricingService
{
    private readonly IRepository<RoomPricing> _roomPricingRepository;
    private readonly IRepository<Domain.Entities.RoomType> _roomTypeRepository;
    private readonly ILoggingService _loggingService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RoomPricingService(IRepository<RoomPricing> roomPricingRepository,
        IRepository<Domain.Entities.RoomType> roomTypeRepository,
        ILoggingService loggingService, IHttpContextAccessor httpContextAccessor)
    {
        _roomTypeRepository = roomTypeRepository;
        _roomPricingRepository = roomPricingRepository;
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RoomPricingDto> CreateRoomPricingAsync(CreateRoomPricingDto pricingDto)
    {
        var roomType = await _roomTypeRepository.GetByIdAsync(pricingDto.RoomTypeId);
        if (roomType == null || roomType.IsDeleted)
        {
            throw new KeyNotFoundException($"Room type with ID {pricingDto.RoomTypeId} not found");
        }

        var roomPricing = new RoomPricing
        {
            RoomTypeId = pricingDto.RoomTypeId,
            Date = pricingDto.Date,
            Price = pricingDto.Price
        };

        await _roomPricingRepository.AddAsync(roomPricing);

        var userId = GetCurrentUserId();
        await _loggingService.LogActionAsync(userId, UserActionType.RoomPricingsCreated, $"Room pricing created for RoomType {roomPricing.RoomTypeId} on {roomPricing.Date} with price {roomPricing.Price}.",
                                              _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                              _httpContextAccessor.HttpContext?.Request.Path,
                                              _httpContextAccessor.HttpContext?.Request.Method);

        return MapToDto(roomPricing);
    }

    public async Task<RoomPricingDto> UpdateRoomPricingAsync(UpdateRoomPricingDto pricingDto)
    {
        var existingPricing = await _roomPricingRepository.GetByIdAsync(pricingDto.Id);
        if (existingPricing == null)
        {
            await _loggingService.LogErrorAsync(new KeyNotFoundException($"RoomPricing with ID {pricingDto.Id} not found."), GetCurrentUserId(),
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
            throw new KeyNotFoundException($"RoomPricing with ID {pricingDto.Id} not found.");
        }

        existingPricing.RoomTypeId = pricingDto.RoomTypeId;
        existingPricing.Date = pricingDto.Date;
        existingPricing.Price = pricingDto.Price;

        await _roomPricingRepository.UpdateAsync(existingPricing);

        var userId = GetCurrentUserId();
        await _loggingService.LogActionAsync(userId, UserActionType.RoomPricingsUpdated, $"Room pricing with ID {existingPricing.Id} updated.",
                                              _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                              _httpContextAccessor.HttpContext?.Request.Path,
                                              _httpContextAccessor.HttpContext?.Request.Method);

        return MapToDto(existingPricing);
    }

    public async Task DeleteRoomPricingAsync(int id)
    {
        var existingPricing = await _roomPricingRepository.GetByIdAsync(id);
        if (existingPricing == null)
        {
            await _loggingService.LogErrorAsync(new KeyNotFoundException($"RoomPricing with ID {id} not found."), GetCurrentUserId(),
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
            throw new KeyNotFoundException($"RoomPricing with ID {id} not found.");
        }

        await _roomPricingRepository.DeleteAsync(id);

        var userId = GetCurrentUserId();
        await _loggingService.LogActionAsync(userId, UserActionType.RoomPricingsDeleted, $"Room pricing with ID {id} deleted.",
                                              _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                              _httpContextAccessor.HttpContext?.Request.Path,
                                              _httpContextAccessor.HttpContext?.Request.Method);
    }

    public async Task<RoomPricingDto> GetRoomPricingByIdAsync(int id)
    {
        var pricing = await _roomPricingRepository.GetByIdAsync(id);
        return pricing != null ? MapToDto(pricing) : null;
    }

    public async Task<IEnumerable<RoomPricingDto>> GetAllRoomPricingsAsync()
    {
        var pricings = await _roomPricingRepository.GetAllAsync();
        return pricings.Select(MapToDto).ToList();
    }

    private static RoomPricingDto MapToDto(RoomPricing pricing)
    {
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

    private int GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return -1;
    }
}
