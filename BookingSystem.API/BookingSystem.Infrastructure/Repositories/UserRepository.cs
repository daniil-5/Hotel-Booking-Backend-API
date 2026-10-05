using Dapper;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DapperDbContext _context;
    public UserRepository(DapperDbContext context) { _context = context; }

    public async Task<User> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(
            "select * from users where id=@id and is_deleted=false", new { id });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<User>("select * from users where is_deleted=false");
    }

    public async Task<IEnumerable<User>> GetAllAsync(System.Linq.Expressions.Expression<Func<User, bool>> predicate)
    {
        var allUsers = await GetAllAsync();
        return allUsers.Where(predicate.Compile());
    }

    public Task<User> GetByIdAsync(int id, Func<IQueryable<User>, IQueryable<User>> include)
        => throw new NotSupportedException();

    public Task<IEnumerable<User>> GetAllAsync(System.Linq.Expressions.Expression<Func<User, bool>> predicate = null, Func<IQueryable<User>, IQueryable<User>> include = null)
        => throw new NotSupportedException();

    public async Task AddAsync(User entity)
    {
        using var conn = _context.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
@"insert into users (username, email, password_hash, first_name, last_name, phone_number, role, created_at, is_deleted)
 values (@Username, @Email, @PasswordHash, @FirstName, @LastName, @PhoneNumber, @Role, @CreatedAt, false)
 returning id", entity);
        entity.Id = id;
    }

    public async Task UpdateAsync(User entity)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
@"update users
   set username=@Username, email=@Email, password_hash=@PasswordHash, first_name=@FirstName, last_name=@LastName,
       phone_number=@PhoneNumber, role=@Role, updated_at=@UpdatedAt
 where id=@Id and is_deleted=false", entity);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("update users set is_deleted=true, updated_at=now() where id=@id", new { id });
    }

    public async Task<User> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<User, bool>> predicate)
        => (await GetAllAsync()).FirstOrDefault(predicate.Compile());

    public Task<User> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<User, bool>> predicate, Func<IQueryable<User>, IQueryable<User>> include)
        => throw new NotSupportedException();

    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<User, bool>> predicate = null)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("select count(*) from users where is_deleted=false");
    }

    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<User, bool>> predicate, Func<IQueryable<User>, IQueryable<User>> include)
        => throw new NotSupportedException();

    public IQueryable<User> GetQueryable() => throw new NotSupportedException();

    public async Task<User> GetByEmailAsync(string email)
    {
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(
            "select * from users where email=@email and is_deleted=false", new { email });
    }

    public async Task<IEnumerable<User>> GetActiveUsersOrderedByRegistrationDateAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<User>(
            "SELECT id, username, email, first_name, last_name, phone_number, role, created_at, updated_at FROM users WHERE is_deleted = FALSE ORDER BY created_at DESC");
    }

    public async Task<IEnumerable<User>> GetUsersWithNoBookingsAsync()
    {
        using var conn = _context.CreateConnection();
        var sql = @"
            SELECT u.*
            FROM users u
            LEFT JOIN bookings b ON u.id = b.user_id
            WHERE u.is_deleted = FALSE
              AND b.id IS NULL
            ORDER BY u.created_at ASC;";
        return await conn.QueryAsync<User>(sql);
    }

    public async Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<int> userIds)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<User>(
            "select * from users where id = ANY(@userIds) and is_deleted=false", new { userIds });
    }
}
