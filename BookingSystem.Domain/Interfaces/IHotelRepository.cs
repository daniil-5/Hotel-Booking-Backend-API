using System.Linq.Expressions;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Domain.Interfaces
{
    public interface IHotelRepository : IRepository<Hotel>
    {
        Task<(IEnumerable<Hotel> hotels, int totalCount)> SearchHotelsAsync(
            Expression<Func<Hotel, bool>> filter = null,
            Func<IQueryable<Hotel>, IOrderedQueryable<Hotel>> orderBy = null,
            int pageNumber = 1,
            int pageSize = 10,
            bool includeRoomTypes = false,
            bool includePhotos = false,
            bool includeAmenities = false);
            
        Task<IEnumerable<Hotel>> GetHotelsWithDetailsAsync(
            Expression<Func<Hotel, bool>> filter = null, 
            int pageNumber = 1, 
            int pageSize = 10);
            
        Task<Hotel> GetHotelWithDetailsAsync(int id);
        
        Task<IEnumerable<Hotel>> SearchHotelsByAvailabilityAsync(
            string location, 
            DateTime checkIn, 
            DateTime checkOut, 
            int guests, 
            int pageNumber = 1, 
            int pageSize = 10);
    }
}