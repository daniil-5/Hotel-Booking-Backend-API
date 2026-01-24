using System.Linq.Expressions;
using BookingSystem.Application.DTOs.Hotel;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Mappers;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Application.Services;

public class HotelService : IHotelService
{
    private readonly IHotelRepository _hotelRepository;

    private readonly IRepository<Amenity> _amenityRepository;

    public HotelService(IHotelRepository hotelRepository, IRepository<Amenity> amenityRepository)
    {
        _hotelRepository = hotelRepository;
        _amenityRepository = amenityRepository;
    }

    public async Task<HotelDto> CreateHotelAsync(CreateHotelDto hotelDto)
    {
        var hotel = hotelDto.ToEntity();
        await _hotelRepository.AddAsync(hotel);
        return hotel.ToDto();
    }

    public async Task<HotelDto> UpdateHotelAsync(UpdateHotelDto hotelDto)
    {
        var existingHotel = await _hotelRepository.GetByIdAsync(hotelDto.Id,
                include: q => q.Include(h => h.Amenities));

        if (existingHotel == null)
            throw new KeyNotFoundException($"Hotel with ID {hotelDto.Id} not found");

        existingHotel.Name = hotelDto.Name;
        existingHotel.Description = hotelDto.Description;
        existingHotel.Location = hotelDto.Location;
        existingHotel.Rating = hotelDto.Rating;
        existingHotel.BasePrice = hotelDto.BasePrice;
        existingHotel.UpdatedAt = DateTime.UtcNow;

        if (hotelDto.Amenities != null)
        {
            existingHotel.Amenities.Clear();

            var amenityNames = hotelDto.Amenities.Select(a => a.Name).Distinct().ToList();

            var existingAmenities = await _amenityRepository.GetAllAsync(a => amenityNames.Contains(a.Name));

            foreach (var amenityDto in hotelDto.Amenities)
            {
                if (existingHotel.Amenities.Any(a => a.Name.Equals(amenityDto.Name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var existing = existingAmenities.FirstOrDefault(a => a.Name.Equals(amenityDto.Name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existingHotel.Amenities.Add(existing);
                }
                else
                {
                    existingHotel.Amenities.Add(new Amenity
                    {
                        Name = amenityDto.Name,
                        Description = amenityDto.Description
                    });
                }
            }
        }

        await _hotelRepository.UpdateAsync(existingHotel);
        return existingHotel.ToDto();
    }
    public async Task DeleteHotelAsync(int id)
    {
        var hotel = await _hotelRepository.GetByIdAsync(id);
        if (hotel == null)
            throw new KeyNotFoundException($"Hotel with ID {id} not found");

        await _hotelRepository.DeleteAsync(id);
    }

    public async Task<HotelDto> GetHotelByIdAsync(int id)
    {
        var hotel = await _hotelRepository.GetByIdAsync(id,
            include: query => query
                .Include(h => h.RoomTypes.Where(rt => !rt.IsDeleted))
                .Include(h => h.Photos.Where(p => !p.IsDeleted))
                .Include(h => h.Amenities));

        return hotel?.ToDto();
    }

    public async Task<IEnumerable<HotelDto>> GetAllHotelsAsync()
    {
        var hotels = await _hotelRepository.GetAllAsync(
            predicate: h => !h.IsDeleted,
            include: query => query
                .Include(h => h.RoomTypes.Where(rt => !rt.IsDeleted))
                .Include(h => h.Photos.Where(p => !p.IsDeleted))
                .Include(h => h.Amenities));

        return hotels.Select(h => h.ToDto()).ToList();
    }

    public async Task<HotelSearchResultDto> SearchHotelsAsync(HotelSearchDto searchDto)
    {
        Expression<Func<Domain.Entities.Hotel, bool>> filter = hotel => !hotel.IsDeleted;

        if (!string.IsNullOrEmpty(searchDto.Name))
            filter = filter.And(h => h.Name.ToLower().Contains(searchDto.Name.ToLower()));

        if (!string.IsNullOrEmpty(searchDto.Location))
            filter = filter.And(h => h.Location.ToLower().Contains(searchDto.Location.ToLower()));

        if (searchDto.MinRating.HasValue)
            filter = filter.And(h => h.Rating >= searchDto.MinRating.Value);

        if (searchDto.MaxRating.HasValue)
            filter = filter.And(h => h.Rating <= searchDto.MaxRating.Value);

        if (searchDto.MinPrice.HasValue)
            filter = filter.And(h => h.BasePrice >= searchDto.MinPrice.Value);

        if (searchDto.MaxPrice.HasValue)
            filter = filter.And(h => h.BasePrice <= searchDto.MaxPrice.Value);

        if (searchDto.RoomTypeId.HasValue)
            filter = filter.And(h => h.RoomTypes.Any(rt => rt.Id == searchDto.RoomTypeId.Value && !rt.IsDeleted));

        if (searchDto.Amenities != null && searchDto.Amenities.Any())
        {
            foreach (var amenityName in searchDto.Amenities)
            {
                filter = filter.And(h => h.Amenities.Any(a => a.Name == amenityName.Name));
            }
        }

        Func<IQueryable<Hotel>, IOrderedQueryable<Hotel>> orderBy = null;
        switch (searchDto.SortBy?.ToLower() ?? "rating")
        {
            case "name":
                orderBy = query => searchDto.SortDescending ? query.OrderByDescending(h => h.Name) : query.OrderBy(h => h.Name);
                break;
            case "location":
                orderBy = query => searchDto.SortDescending ? query.OrderByDescending(h => h.Location) : query.OrderBy(h => h.Location);
                break;
            case "price":
                orderBy = query => searchDto.SortDescending ? query.OrderByDescending(h => h.BasePrice) : query.OrderBy(h => h.BasePrice);
                break;
            case "rating":
            default:
                orderBy = query => searchDto.SortDescending ? query.OrderByDescending(h => h.Rating) : query.OrderBy(h => h.Rating);
                break;
        }

        var (hotels, totalCount) = await _hotelRepository.SearchHotelsAsync(
            filter,
            orderBy,
            searchDto.PageNumber,
            searchDto.PageSize,
            includeRoomTypes: true,
            includePhotos: true,
            includeAmenities: true
        );

        var totalPages = (int)Math.Ceiling(totalCount / (double)searchDto.PageSize);
        var hasPrevious = searchDto.PageNumber > 1;
        var hasNext = searchDto.PageNumber < totalPages;

        return new HotelSearchResultDto
        {
            Hotels = hotels.Select(h => h.ToDto()).ToList(),
            TotalCount = totalCount,
            PageNumber = searchDto.PageNumber,
            PageSize = searchDto.PageSize,
            TotalPages = totalPages,
            HasPrevious = hasPrevious,
            HasNext = hasNext
        };
    }

}
