using BookingSystem.Application.DTOs.Hotel;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Application.Decorators
{
    public class CachedHotelService : IHotelService
    {
        private readonly IHotelService _hotelService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachedHotelService> _logger;

        private const string HOTEL_BY_ID_KEY = "hotel:id:{0}";
        private const string ALL_HOTELS_KEY = "hotels:all";
        private const string HOTEL_SEARCH_KEY = "hotels:search:{0}";
        private const string HOTEL_PREFIX = "hotel";
        private const string HOTELS_PREFIX = "hotels";

        private static readonly TimeSpan HotelCacheExpiration = TimeSpan.FromMinutes(45);
        private static readonly TimeSpan HotelSearchExpiration = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan AllHotelsExpiration = TimeSpan.FromMinutes(30);

        public CachedHotelService(
            IHotelService hotelService,
            ICacheService cacheService,
            ILogger<CachedHotelService> logger)
        {
            _hotelService = hotelService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<HotelDto> CreateHotelAsync(CreateHotelDto hotelDto)
        {
            _logger.LogInformation("Creating new hotel: {HotelName}", hotelDto.Name);

            var newHotel = await _hotelService.CreateHotelAsync(hotelDto);

            await CacheHotel(newHotel);
            await InvalidateListCaches();

            _logger.LogInformation("Hotel created and cached with ID: {HotelId}", newHotel.Id);
            return newHotel;
        }

        public async Task<HotelDto> UpdateHotelAsync(UpdateHotelDto hotelDto)
        {
            _logger.LogInformation("Updating hotel with ID: {HotelId}", hotelDto.Id);

            var updatedHotel = await _hotelService.UpdateHotelAsync(hotelDto);

            await CacheHotel(updatedHotel);
            await InvalidateListCaches();

            _logger.LogInformation("Hotel updated and cache refreshed for ID: {HotelId}", updatedHotel.Id);
            return updatedHotel;
        }

        public async Task DeleteHotelAsync(int id)
        {
            _logger.LogInformation("Deleting hotel with ID: {HotelId}", id);

            await _hotelService.DeleteHotelAsync(id);
            await _cacheService.RemoveAsync(string.Format(HOTEL_BY_ID_KEY, id));
            await InvalidateListCaches();

            _logger.LogInformation("Hotel deleted and cache invalidated for ID: {HotelId}", id);
        }

        public async Task<HotelDto> GetHotelByIdAsync(int id)
        {
            var cacheKey = string.Format(HOTEL_BY_ID_KEY, id);

            var cachedHotel = await _cacheService.GetAsync<HotelDto>(cacheKey);
            if (cachedHotel != null)
            {
                _logger.LogDebug("Hotel found in cache for ID: {HotelId}", id);
                return cachedHotel;
            }

            _logger.LogDebug("Hotel not found in cache, fetching from database for ID: {HotelId}", id);

            var hotel = await _hotelService.GetHotelByIdAsync(id);

            if (hotel != null)
            {
                await CacheHotel(hotel);
                _logger.LogDebug("Hotel cached for ID: {HotelId}", id);
            }

            return hotel;
        }

        public async Task<IEnumerable<HotelDto>> GetAllHotelsAsync()
        {
            var cacheKey = ALL_HOTELS_KEY;

            var cachedHotels = await _cacheService.GetAsync<IEnumerable<HotelDto>>(cacheKey);
            if (cachedHotels != null)
            {
                _logger.LogDebug("All hotels found in cache");
                return cachedHotels;
            }

            _logger.LogDebug("All hotels not found in cache, fetching from database");

            var hotels = await _hotelService.GetAllHotelsAsync();

            await _cacheService.SetAsync(cacheKey, hotels, AllHotelsExpiration);

            foreach (var hotel in hotels)
            {
                await CacheHotel(hotel);
            }

            _logger.LogDebug("All hotels cached, count: {HotelCount}", hotels.Count());
            return hotels;
        }

        public async Task<HotelSearchResultDto> SearchHotelsAsync(HotelSearchDto searchDto)
        {
            var searchKey = GenerateSearchCacheKey(searchDto);
            var cacheKey = string.Format(HOTEL_SEARCH_KEY, searchKey);

            var cachedResult = await _cacheService.GetAsync<HotelSearchResultDto>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Hotel search result found in cache for key: {SearchKey}", searchKey);
                return cachedResult;
            }

            _logger.LogDebug("Hotel search result not found in cache, executing search for key: {SearchKey}", searchKey);

            var searchResult = await _hotelService.SearchHotelsAsync(searchDto);

            await _cacheService.SetAsync(cacheKey, searchResult, HotelSearchExpiration);

            foreach (var hotel in searchResult.Hotels)
            {
                await CacheHotel(hotel);
            }

            _logger.LogDebug("Hotel search result cached for key: {SearchKey}, count: {ResultCount}",
                searchKey, searchResult.Hotels.Count());

            return searchResult;
        }

        private async Task CacheHotel(HotelDto hotel)
        {
            if (hotel == null) return;

            var cacheKey = string.Format(HOTEL_BY_ID_KEY, hotel.Id);
            await _cacheService.SetAsync(cacheKey, hotel, HotelCacheExpiration);
        }

        private async Task InvalidateListCaches()
        {
            var tasks = new List<Task>
            {
                _cacheService.RemoveAsync(ALL_HOTELS_KEY),
                _cacheService.RemoveByPrefixAsync("hotels:search:")
            };

            await Task.WhenAll(tasks);
            _logger.LogDebug("Hotel list caches invalidated");
        }

        private static string GenerateSearchCacheKey(HotelSearchDto searchDto)
        {
            var keyParts = new List<string>
            {
                $"name:{searchDto.Name ?? "null"}",
                $"location:{searchDto.Location ?? "null"}",
                $"minrating:{searchDto.MinRating?.ToString() ?? "null"}",
                $"maxrating:{searchDto.MaxRating?.ToString() ?? "null"}",
                $"minprice:{searchDto.MinPrice?.ToString() ?? "null"}",
                $"maxprice:{searchDto.MaxPrice?.ToString() ?? "null"}",
                $"roomtype:{searchDto.RoomTypeId?.ToString() ?? "null"}",
                $"amenities:{(searchDto.Amenities != null ? string.Join(",", searchDto.Amenities.OrderBy(a => a)) : "null")}",
                $"sort:{searchDto.SortBy ?? "rating"}",
                $"desc:{searchDto.SortDescending}",
                $"page:{searchDto.PageNumber}",
                $"size:{searchDto.PageSize}"
            };

            return string.Join("|", keyParts).ToLower();
        }

        public async Task InvalidateAllHotelCachesAsync()
        {
            var tasks = new List<Task>
            {
                _cacheService.RemoveByPrefixAsync(HOTEL_PREFIX),
                _cacheService.RemoveByPrefixAsync(HOTELS_PREFIX)
            };

            await Task.WhenAll(tasks);
            _logger.LogInformation("All hotel caches invalidated");
        }
    }
}