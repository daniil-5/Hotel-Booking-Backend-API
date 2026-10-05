using BookingSystem.Application.DTOs.RoomType;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.Application.Services;

public class RoomTypeService : IRoomTypeService
{
    private readonly IRepository<Domain.Entities.RoomType> _roomTypeRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly ILoggingService _loggingService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RoomTypeService(
        IRepository<Domain.Entities.RoomType> roomTypeRepository,
        IHotelRepository hotelRepository,
        ILoggingService loggingService,
        IHttpContextAccessor httpContextAccessor)
    {
        _roomTypeRepository = roomTypeRepository;
        _hotelRepository = hotelRepository;
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RoomTypeDto> CreateRoomTypeAsync(CreateRoomTypeDto roomTypeDto)
    {
        // Verify hotel exists
        var hotel = await _hotelRepository.GetByIdAsync(roomTypeDto.HotelId);
        if (hotel == null)
        {
            await _loggingService.LogErrorAsync(new KeyNotFoundException($"Hotel with ID {roomTypeDto.HotelId} not found."), GetCurrentUserId(),
                                                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                _httpContextAccessor.HttpContext?.Request.Path,
                                                _httpContextAccessor.HttpContext?.Request.Method);
            throw new KeyNotFoundException($"Hotel with ID {roomTypeDto.HotelId} not found.");
        }

        var roomType = new Domain.Entities.RoomType
        {
            Name = roomTypeDto.Name,
            Description = roomTypeDto.Description,
            Capacity = roomTypeDto.Capacity,
            BasePrice = roomTypeDto.BasePrice,
            Area = (int)roomTypeDto.Area,
            HotelId = roomTypeDto.HotelId,
            Hotel = hotel
        };

        await _roomTypeRepository.AddAsync(roomType);

        var userId = GetCurrentUserId();
        await _loggingService.LogActionAsync(userId, UserActionType.RoomTypesCreated, $"RoomType {roomType.Name} created with ID {roomType.Id} for Hotel {roomType.HotelId}.",
                                              _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                              _httpContextAccessor.HttpContext?.Request.Path,
                                              _httpContextAccessor.HttpContext?.Request.Method);

        return MapToDto(roomType);
    }

    public async Task<RoomTypeDto> UpdateRoomTypeAsync(UpdateRoomTypeDto roomTypeDto)
    {
        var hotel = await _hotelRepository.GetByIdAsync(roomTypeDto.HotelId);
        if (hotel == null)
        {
            await _loggingService.LogErrorAsync(new KeyNotFoundException($"Hotel with ID {roomTypeDto.HotelId} not found."), GetCurrentUserId(),
                                                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                _httpContextAccessor.HttpContext?.Request.Path,
                                                _httpContextAccessor.HttpContext?.Request.Method);
            throw new KeyNotFoundException($"Hotel with ID {roomTypeDto.HotelId} not found.");
        }

        var existingRoomType = await _roomTypeRepository.GetByIdAsync(roomTypeDto.Id);
        if (existingRoomType == null)
        {
            await _loggingService.LogErrorAsync(new KeyNotFoundException($"RoomType with ID {roomTypeDto.Id} not found."), GetCurrentUserId(),
                                                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                _httpContextAccessor.HttpContext?.Request.Path,
                                                _httpContextAccessor.HttpContext?.Request.Method);
            throw new KeyNotFoundException($"RoomType with ID {roomTypeDto.Id} not found.");
        }

        existingRoomType.Name = roomTypeDto.Name;
        existingRoomType.Description = roomTypeDto.Description;
        existingRoomType.Capacity = roomTypeDto.Capacity;
        existingRoomType.BasePrice = roomTypeDto.BasePrice;
        existingRoomType.Area = (int)roomTypeDto.Area;
        existingRoomType.HotelId = roomTypeDto.HotelId;

        await _roomTypeRepository.UpdateAsync(existingRoomType);

        var userId = GetCurrentUserId();
        await _loggingService.LogActionAsync(userId, UserActionType.RoomTypesUpdated, $"RoomType {existingRoomType.Name} with ID {existingRoomType.Id} updated.",
                                              _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                              _httpContextAccessor.HttpContext?.Request.Path,
                                              _httpContextAccessor.HttpContext?.Request.Method);

        return MapToDto(existingRoomType);
    }

    public async Task DeleteRoomTypeAsync(int id)
    {
        var roomType = await _roomTypeRepository.GetByIdAsync(id);
        if (roomType == null)
        {
            await _loggingService.LogErrorAsync(new KeyNotFoundException($"RoomType with ID {id} not found."), GetCurrentUserId(),
                                                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                _httpContextAccessor.HttpContext?.Request.Path,
                                                _httpContextAccessor.HttpContext?.Request.Method);
            throw new KeyNotFoundException($"RoomType with ID {id} not found.");
        }


        await _roomTypeRepository.DeleteAsync(id);

        var userId = GetCurrentUserId();
        await _loggingService.LogActionAsync(userId, UserActionType.RoomTypesDeleted, $"RoomType with ID {id} deleted.",
                                              _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                              _httpContextAccessor.HttpContext?.Request.Path,
                                              _httpContextAccessor.HttpContext?.Request.Method);
    }

    public async Task<RoomTypeDto> GetRoomTypeByIdAsync(int id)
    {
        var roomType = await _roomTypeRepository.GetByIdAsync(id);

        if (roomType == null)
        {
            return null;
        }

        return MapToDto(roomType);
    }

    public async Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync()
    {
        var roomTypes = await _roomTypeRepository.GetAllAsync();
        return roomTypes.Select(MapToDto).ToList();
    }
    public async Task<IEnumerable<RoomTypeDto>> GetRoomTypesByHotelIdAsync(int hotelId)
    {
        // First check if the hotel exists
        var hotel = await _hotelRepository.GetByIdAsync(hotelId);
        if (hotel == null)
        {
            await _loggingService.LogErrorAsync(new KeyNotFoundException($"Hotel with ID {hotelId} not found."), GetCurrentUserId(),
                                                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                _httpContextAccessor.HttpContext?.Request.Path,
                                                _httpContextAccessor.HttpContext?.Request.Method);
            throw new KeyNotFoundException($"Hotel with ID {hotelId} not found.");
        }

        // Get all room types for this hotel
        var roomTypes = await _roomTypeRepository.GetAllAsync(
            rt => rt.HotelId == hotelId && !rt.IsDeleted);

        return roomTypes.Select(MapToDto).ToList();
    }

    private static RoomTypeDto MapToDto(Domain.Entities.RoomType roomType)
    {
        return new RoomTypeDto
        {
            Id = roomType.Id,
            Name = roomType.Name,
            Description = roomType.Description,
            BasePrice = roomType.BasePrice,
            Capacity = roomType.Capacity,
            Area = roomType.Area,
            HotelId = roomType.HotelId
        };
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return -1; // Or throw an exception if user ID is always expected
    }
}
