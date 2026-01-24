using System.Linq.Expressions;

namespace BookingSystem.Domain.Interfaces;

public interface IRepository<T> where T : class
{
        Task<T> GetByIdAsync(int id);
        Task<T> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>> include);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate);
        
        Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>> predicate = null, 
            Func<IQueryable<T>, IQueryable<T>> include = null);
            
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
        Task<int> CountAsync(
            Expression<Func<T, bool>> predicate, 
            Func<IQueryable<T>, IQueryable<T>> include);
        
        IQueryable<T> GetQueryable();
        Task<T?> FindAsync(Expression<Func<T, bool>> predicate);
}