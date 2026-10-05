using BookingSystem.Application.DTOs.Hotel;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using BookingSystem.Application.DTOs.Booking;

namespace BookingSystem.Application.Decorators
{
    public class CachedHotelService : IHotelService
    {
        private readonly IHotelService _hotelService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachedHotelService> _logger;
        private readonly ICacheInvalidationPublisher _publisher;

        // Cache key constants
        private const string HOTEL_BY_ID_KEY = "hotel:id:{0}";
        private const string ALL_HOTELS_KEY = "hotels:all";
        private const string HOTEL_SEARCH_KEY = "hotels:search:{0}";
        private const string HOTEL_PREFIX = "hotel";
        private const string HOTELS_PREFIX = "hotels";

        // Cache expiration times - hotel data is relatively stable
        private static readonly TimeSpan HotelCacheExpiration = TimeSpan.FromMinutes(45);
        private static readonly TimeSpan HotelSearchExpiration = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan AllHotelsExpiration = TimeSpan.FromMinutes(30);

        public CachedHotelService(
            IHotelService hotelService,
            ICacheService cacheService,
            ILogger<CachedHotelService> logger,
            ICacheInvalidationPublisher publisher)
        {
            _hotelService = hotelService;
            _cacheService = cacheService;
            _logger = logger;
            _publisher = publisher;
        }

        public async Task<HotelDto> CreateHotelAsync(CreateHotelDto hotelDto)
        {
            _logger.LogInformation("Creating new hotel: {HotelName}", hotelDto.Name);

            var newHotel = await _hotelService.CreateHotelAsync(hotelDto);

            // Cache the new hotel
            await CacheHotel(newHotel);

            // Invalidate list caches
            await InvalidateListCaches();

            await _publisher.PublishAsync("hotel", newHotel.Id.ToString());

            _logger.LogInformation("Hotel created and cached with ID: {HotelId}", newHotel.Id);
            return newHotel;
        }

        public async Task<HotelDto> UpdateHotelAsync(UpdateHotelDto hotelDto)
        {
            _logger.LogInformation("Updating hotel with ID: {HotelId}", hotelDto.Id);

            var updatedHotel = await _hotelService.UpdateHotelAsync(hotelDto);

            // Update cache with new data
            await CacheHotel(updatedHotel);

            // Invalidate list caches as hotel data changed
            await InvalidateListCaches();

            _logger.LogInformation("Hotel updated and cache refreshed for ID: {HotelId}", updatedHotel.Id);

            await _publisher.PublishAsync("hotel", hotelDto.Id.ToString());

            return updatedHotel;
        }

        public async Task DeleteHotelAsync(int id)
        {
            _logger.LogInformation("Deleting hotel with ID: {HotelId}", id);

            await _hotelService.DeleteHotelAsync(id);

            // Remove from cache
            await _cacheService.RemoveAsync(string.Format(HOTEL_BY_ID_KEY, id));

            // Invalidate list caches
            await InvalidateListCaches();

            await _publisher.PublishAsync("hotel", id.ToString());

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

            // Cache the result
            await _cacheService.SetAsync(cacheKey, hotels, AllHotelsExpiration);

            // Also cache individual hotels
            foreach (var hotel in hotels)
            {
                await CacheHotel(hotel);
            }

            _logger.LogDebug("All hotels cached, count: {HotelCount}", hotels.Count());
            return hotels;
        }

        public async Task<HotelSearchResultDto> SearchHotelsAsync(HotelSearchDto searchDto)
        {
            // Generate cache key based on search parameters
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

            // Cache the search result
            await _cacheService.SetAsync(cacheKey, searchResult, HotelSearchExpiration);

            // Also cache individual hotels from the search result
            foreach (var hotel in searchResult.Hotels)
            {
                await CacheHotel(hotel);
            }

            _logger.LogDebug("Hotel search result cached for key: {SearchKey}, count: {ResultCount}",
                searchKey, searchResult.Hotels.Count());

            return searchResult;
        }

        public Task<IEnumerable<HotelStatistics>> GetHotelsStatisticsAsync()
        {
            return _hotelService.GetHotelsStatisticsAsync();
        }

        public Task<IEnumerable<HotelAvailability>> SearchAvailableHotelsAsync(string location, DateTime checkIn, DateTime checkOut, int guestCount)
        {
            return _hotelService.SearchAvailableHotelsAsync(location, checkIn, checkOut, guestCount);
        }

        public Task<IEnumerable<HotelRanking>> GetHotelsRankedByLocationAsync()
        {
            return _hotelService.GetHotelsRankedByLocationAsync();
        }

        public async Task<HotelPerformanceReport?> GetHotelPerformanceReportAsync(int hotelId)
        {
            var cacheKey = $"hotel:performance:{hotelId}";

            var cachedReport = await _cacheService.GetAsync<HotelPerformanceReport>(cacheKey);
            if (cachedReport != null)
            {
                _logger.LogDebug("Hotel performance report found in cache for ID: {HotelId}", hotelId);
                return cachedReport;
            }

            _logger.LogDebug("Hotel performance report not found in cache, fetching from service for ID: {HotelId}", hotelId);

            var report = await _hotelService.GetHotelPerformanceReportAsync(hotelId);

            if (report != null)
            {
                await _cacheService.SetAsync(cacheKey, report, HotelCacheExpiration); // Re-use existing expiration
                _logger.LogDebug("Hotel performance report cached for ID: {HotelId}", hotelId);
            }

            return report;
        }

        public Task<IEnumerable<MonthlyBookingTrend>> GetMonthlyBookingTrendsAsync(int? hotelId = null, int months = 12)
        {
            return _hotelService.GetMonthlyBookingTrendsAsync(hotelId, months);
        }

        public async Task<IEnumerable<HotelDto>> GetHotelsOrderedByRatingAndNameAsync()
        {
            const string cacheKey = "hotels:ordered:rating-name";

            var cachedHotels = await _cacheService.GetAsync<IEnumerable<HotelDto>>(cacheKey);
            if (cachedHotels != null)
            {
                _logger.LogDebug("Hotels ordered by rating and name found in cache");
                return cachedHotels;
            }

            _logger.LogDebug("Hotels ordered by rating and name not found in cache, fetching from database");
            var hotels = await _hotelService.GetHotelsOrderedByRatingAndNameAsync();

            await _cacheService.SetAsync(cacheKey, hotels, AllHotelsExpiration);
            _logger.LogDebug("Hotels ordered by rating and name cached");
            return hotels;
        }

        public async Task<IEnumerable<HotelDto>> GetPremiumHotelsAsync()
        {
            const string cacheKey = "hotels:premium";

            var cachedHotels = await _cacheService.GetAsync<IEnumerable<HotelDto>>(cacheKey);
            if (cachedHotels != null)
            {
                _logger.LogDebug("Premium hotels found in cache");
                return cachedHotels;
            }

            _logger.LogDebug("Premium hotels not found in cache, fetching from database");
            var hotels = await _hotelService.GetPremiumHotelsAsync();

            await _cacheService.SetAsync(cacheKey, hotels, AllHotelsExpiration);
            _logger.LogDebug("Premium hotels cached");
            return hotels;
        }

        #region Private Helper Methods

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

        /// <summary>
        /// Generates a consistent cache key for hotel search parameters
        /// </summary>
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
                $"sort:{searchDto.SortBy ?? "rating"}",
                $"desc:{searchDto.SortDescending}",
                $"page:{searchDto.PageNumber}",
                $"size:{searchDto.PageSize}"
            };

            return string.Join("|", keyParts).ToLower();
        }

        /// <summary>
        /// Invalidates all hotel-related caches (useful for bulk operations)
        /// </summary>
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

        #endregion
    }
}
