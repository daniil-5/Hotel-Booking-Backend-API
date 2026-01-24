using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingSystem.Infrastructure.Repositories
{
    public class HotelRepository : BaseRepository<Hotel>, IHotelRepository
    {
        private readonly AppDbContext _context;

        public HotelRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Hotel> hotels, int totalCount)> SearchHotelsAsync(
            Expression<Func<Hotel, bool>> filter = null,
            Func<IQueryable<Hotel>, IOrderedQueryable<Hotel>> orderBy = null,
            int pageNumber = 1,
            int pageSize = 10,
            bool includeRoomTypes = false,
            bool includePhotos = false,
            bool includeAmenities = false)
        {
            IQueryable<Hotel> query = _context.Hotels.Where(h => !h.IsDeleted);

            if (includeRoomTypes)
                query = query.Include(h => h.RoomTypes.Where(rt => !rt.IsDeleted));

            if (includePhotos)
                query = query.Include(h => h.Photos.Where(p => !p.IsDeleted));

            if (includeAmenities)
                query = query.Include(h => h.Amenities.Where(a => !a.IsDeleted));

            if (filter != null)
                query = query.Where(filter);

            var totalCount = await query.CountAsync();

            if (orderBy != null)
                query = orderBy(query);
            else
                query = query.OrderByDescending(h => h.Rating);

            var hotels = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (hotels, totalCount);
        }

        public async Task<IEnumerable<Hotel>> GetHotelsWithDetailsAsync(
            Expression<Func<Hotel, bool>> filter = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            IQueryable<Hotel> query = _context.Hotels
                .Include(h => h.RoomTypes)
                .Include(h => h.Photos)
                .Include(h => h.Amenities)
                .Where(h => !h.IsDeleted);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query
                .OrderByDescending(h => h.Rating)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Hotel> GetHotelWithDetailsAsync(int id)
        {
            return await _context.Hotels
                .Include(h => h.RoomTypes.Where(rt => !rt.IsDeleted))
                .Include(h => h.Photos.Where(p => !p.IsDeleted))
                .Include(h => h.Amenities.Where(a => !a.IsDeleted)) // Added Amenities
                .Include(h => h.RoomTypes)
                .ThenInclude(rt => rt.Pricing.Where(p =>
                        p.Date >= DateTime.UtcNow &&
                        p.Date <= DateTime.UtcNow.AddDays(90)))
                .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);
        }

        public async Task<IEnumerable<Hotel>> SearchHotelsByAvailabilityAsync(
            string location,
            DateTime checkIn,
            DateTime checkOut,
            int guests,
            int pageNumber = 1,
            int pageSize = 10)
        {
            if (checkIn >= checkOut)
                throw new ArgumentException("Check-out date must be after check-in date");

            if (checkIn.Date < DateTime.UtcNow.Date)
                throw new ArgumentException("Check-in date cannot be in the past");

            var query = _context.Hotels
                .Include(h => h.RoomTypes.Where(rt => !rt.IsDeleted && rt.Capacity >= guests))
                .Include(h => h.Photos.Where(p => !p.IsDeleted && p.IsMain))
                .Include(h => h.Amenities)
                .Where(h => !h.IsDeleted);

            if (!string.IsNullOrEmpty(location))
            {
                location = location.ToLower();
                query = query.Where(h => h.Location.ToLower().Contains(location));
            }

            var hotels = await query.ToListAsync();

            var availableHotels = new List<Hotel>();

            foreach (var hotel in hotels)
            {
                bool hasAvailableRoomType = false;

                var suitableRoomTypes = hotel.RoomTypes.Where(rt => rt.Capacity >= guests);

                foreach (var roomType in suitableRoomTypes)
                {
                    var bookingCount = await _context.Bookings.CountAsync(b =>
                        b.RoomTypeId == roomType.Id &&
                        b.Status != (int)BookingStatus.Cancelled &&
                        b.CheckInDate < checkOut &&
                        b.CheckOutDate > checkIn);

                    var totalInventory = roomType.Count;

                    if (totalInventory > bookingCount)
                    {
                        hasAvailableRoomType = true;
                        break;
                    }
                }

                if (hasAvailableRoomType)
                {
                    availableHotels.Add(hotel);
                }
            }

            return availableHotels
                .OrderByDescending(h => h.Rating)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }
    }
}