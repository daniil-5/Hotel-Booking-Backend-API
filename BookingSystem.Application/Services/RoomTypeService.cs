using BookingSystem.Application.DTOs.RoomType;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Application.Mappers;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Services;

public class RoomTypeService : IRoomTypeService
{
    private readonly IRepository<Domain.Entities.RoomType> _roomTypeRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly IRepository<Domain.Entities.Booking> _bookingRepository;

    public RoomTypeService(
        IRepository<Domain.Entities.RoomType> roomTypeRepository,
        IHotelRepository hotelRepository,
        IRepository<Domain.Entities.Booking> bookingRepository)
    {
        _roomTypeRepository = roomTypeRepository;
        _hotelRepository = hotelRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<RoomTypeDto> CreateRoomTypeAsync(CreateRoomTypeDto roomTypeDto)
    {
        var hotel = await _hotelRepository.GetByIdAsync(roomTypeDto.HotelId);
        if (hotel == null)
            throw new KeyNotFoundException($"Hotel with ID {roomTypeDto.HotelId} not found.");

        var roomType = roomTypeDto.ToEntity();

        await _roomTypeRepository.AddAsync(roomType);
        return roomType.ToDto();
    }

    public async Task<RoomTypeDto> UpdateRoomTypeAsync(UpdateRoomTypeDto roomTypeDto)
    {
        var hotel = await _hotelRepository.GetByIdAsync(roomTypeDto.HotelId);
        if (hotel == null)
            throw new KeyNotFoundException($"Hotel with ID {roomTypeDto.HotelId} not found.");

        var existingRoomType = await _roomTypeRepository.GetByIdAsync(roomTypeDto.Id);
        if (existingRoomType == null)
            throw new KeyNotFoundException($"RoomType with ID {roomTypeDto.Id} not found.");

        existingRoomType.Name = roomTypeDto.Name;
        existingRoomType.Description = roomTypeDto.Description;
        existingRoomType.Capacity = roomTypeDto.Capacity;
        existingRoomType.BasePrice = roomTypeDto.BasePrice;
        existingRoomType.Area = roomTypeDto.Area;
        existingRoomType.Floor = roomTypeDto.Floor;
        existingRoomType.Count = roomTypeDto.Count;
        existingRoomType.HotelId = roomTypeDto.HotelId;

        await _roomTypeRepository.UpdateAsync(existingRoomType);
        return existingRoomType.ToDto();
    }

    public async Task DeleteRoomTypeAsync(int id)
    {
        var roomType = await _roomTypeRepository.GetByIdAsync(id);
        if (roomType == null)
            throw new KeyNotFoundException($"RoomType with ID {id} not found.");

        var activeBookings = await _bookingRepository.CountAsync(b =>
            b.RoomTypeId == id &&
            b.Status != (int)BookingStatus.Cancelled &&
            b.CheckOutDate > DateTime.UtcNow);

        if (activeBookings > 0)
        {
            throw new InvalidOperationException("Cannot delete room type that has active bookings.");
        }

        await _roomTypeRepository.DeleteAsync(id);
    }

    public async Task<RoomTypeDto> GetRoomTypeByIdAsync(int id)
    {
        var roomType = await _roomTypeRepository.GetByIdAsync(id);
        return roomType?.ToDto();
    }

    public async Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync()
    {
        var roomTypes = await _roomTypeRepository.GetAllAsync();
        return roomTypes.Select(rt => rt.ToDto()).ToList();
    }

    public async Task<IEnumerable<RoomTypeDto>> GetRoomTypesByHotelIdAsync(int hotelId)
    {
        var hotel = await _hotelRepository.GetByIdAsync(hotelId);
        if (hotel == null)
            throw new KeyNotFoundException($"Hotel with ID {hotelId} not found.");

        var roomTypes = await _roomTypeRepository.GetAllAsync(rt => rt.HotelId == hotelId && !rt.IsDeleted);

        return roomTypes.Select(rt => rt.ToDto()).ToList();
    }
}