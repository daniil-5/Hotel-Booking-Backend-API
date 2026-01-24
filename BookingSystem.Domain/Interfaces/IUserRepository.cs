using System.Linq.Expressions;
using BookingSystem.Domain.Entities;
namespace BookingSystem.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User> GetByEmailAsync(string email);

    Task<(IEnumerable<User> users, int totalCount)> SearchUsersAsync(
        Expression<Func<User, bool>> filter,
        Func<IQueryable<User>, IOrderedQueryable<User>> orderBy, int pageNumber, int pageSize);
}
