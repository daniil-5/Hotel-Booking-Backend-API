using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingSystem.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        private readonly AppDbContext _dbContext;

        public UserRepository(AppDbContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task<(IEnumerable<User> users, int totalCount)> SearchUsersAsync(
            Expression<Func<User, bool>> filter = null,
            Func<IQueryable<User>, IOrderedQueryable<User>> orderBy = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var query = _dbContext.Users.AsQueryable();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            query = query.Where(u => !u.IsDeleted);

            var totalCount = await query.CountAsync();

            var orderedQuery = orderBy != null
                ? orderBy(query)
                : query.OrderByDescending(u => u.CreatedAt);

            var users = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<User> GetUserWithDetailsAsync(int userId)
        {
            return await _dbContext.Users
                .Include(u => u.Bookings.Where(b => !b.IsDeleted))
                    .ThenInclude(b => b.Hotel)
                .Include(u => u.Bookings.Where(b => !b.IsDeleted))
                    .ThenInclude(b => b.RoomType)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbContext.Users.AnyAsync(u => u.Email == email && !u.IsDeleted);
        }
    }
}