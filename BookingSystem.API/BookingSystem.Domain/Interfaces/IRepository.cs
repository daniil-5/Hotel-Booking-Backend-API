using System.Linq.Expressions;

namespace BookingSystem.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<T> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>> include);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate);

    // Add this new overload for GetAllAsync with include parameter
    Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IQueryable<T>> include = null);

    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);

    // Advanced query operations
    Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<T> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>> include);

    // Counting methods
    Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
    Task<int> CountAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>> include);

    // Direct access to IQueryable for complex queries
    IQueryable<T> GetQueryable();
}
