using System.Linq.Expressions;
using BookingSystem.Domain.Entities;
namespace BookingSystem.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersOrderedByRegistrationDateAsync();
    Task<IEnumerable<User>> GetUsersWithNoBookingsAsync();
    Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<int> userIds);
}
