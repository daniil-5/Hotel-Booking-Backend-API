using System.Linq.Expressions;
using System.Reflection;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Repositories
{
    public class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;
        private readonly PropertyInfo _isDeletedProperty;

        public BaseRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
            _isDeletedProperty = typeof(T).GetProperty("IsDeleted");
        }

        protected IQueryable<T> ApplySoftDeleteFilter(IQueryable<T> query)
        {
            if (_isDeletedProperty != null)
            {
                var parameter = Expression.Parameter(typeof(T), "entity");
                var property = Expression.Property(parameter, _isDeletedProperty);
                var falseValue = Expression.Constant(false);
                var condition = Expression.Equal(property, falseValue);
                var lambda = Expression.Lambda<Func<T, bool>>(condition, parameter);

                query = query.Where(lambda);
            }

            return query;
        }

        public async Task<T> GetByIdAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);

            if (entity != null && _isDeletedProperty != null)
            {
                var isDeleted = (bool)_isDeletedProperty.GetValue(entity);
                if (isDeleted)
                {
                    return null;
                }
            }

            return entity;
        }

        public async Task<T> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>>? include = null)
        {
            var query = _dbSet.AsQueryable();
            query = ApplySoftDeleteFilter(query);

            if (include != null)
            {
                query = include(query);
            }

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, "Id");
            var constant = Expression.Constant(id);
            var equal = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equal, parameter);

            return await query.FirstOrDefaultAsync(lambda);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            var query = _dbSet.AsNoTracking().AsQueryable();
            query = ApplySoftDeleteFilter(query);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
        {
            var query = _dbSet.Where(predicate);
            query = ApplySoftDeleteFilter(query);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>> predicate = null,
            Func<IQueryable<T>, IQueryable<T>> include = null)
        {
            IQueryable<T> query = _dbSet;
            query = ApplySoftDeleteFilter(query);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (include != null)
            {
                query = include(query);
            }

            return await query.ToListAsync();
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            var query = _dbSet.AsQueryable();
            query = ApplySoftDeleteFilter(query);
            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<T> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>> include)
        {
            IQueryable<T> query = _dbSet;
            query = ApplySoftDeleteFilter(query);

            if (include != null)
            {
                query = include(query);
            }

            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
        {
            var query = _dbSet.AsQueryable();
            query = ApplySoftDeleteFilter(query);

            if (predicate == null)
            {
                return await query.CountAsync();
            }

            return await query.CountAsync(predicate);
        }

        public async Task<int> CountAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>> include)
        {
            IQueryable<T> query = _dbSet;
            query = ApplySoftDeleteFilter(query);

            if (include != null)
            {
                query = include(query);
            }

            return await query.CountAsync(predicate);
        }

        public IQueryable<T> GetQueryable()
        {
            var query = _dbSet.AsQueryable();
            return ApplySoftDeleteFilter(query);
        }

        public async Task AddAsync(T entity)
        {
            if (_isDeletedProperty != null)
            {
                _isDeletedProperty.SetValue(entity, false);
            }

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                if (_isDeletedProperty != null)
                {
                    _isDeletedProperty.SetValue(entity, true);

                    var updatedAtProperty = typeof(T).GetProperty("UpdatedAt");
                    if (updatedAtProperty != null && updatedAtProperty.PropertyType == typeof(DateTime?))
                    {
                        updatedAtProperty.SetValue(entity, DateTime.UtcNow);
                    }

                    _dbSet.Update(entity);
                }
                else
                {
                    _dbSet.Remove(entity);
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task DeletePermanentlyAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }
    }
}
