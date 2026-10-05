using Dapper;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.Infrastructure.Repositories;

public class RoomTypeRepository : IRepository<RoomType>
{
    private readonly DapperDbContext _context;
    public RoomTypeRepository(DapperDbContext context) { _context = context; }

    public async Task<RoomType> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<RoomType>(
            "select * from room_types where id=@id and is_deleted=false", new { id });
    }

    public async Task<IEnumerable<RoomType>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<RoomType>("select * from room_types where is_deleted=false");
    }

    public async Task<IEnumerable<RoomType>> GetAllAsync(System.Linq.Expressions.Expression<Func<RoomType, bool>> predicate)
    {
        var allRoomTypes = await GetAllAsync();
        return allRoomTypes.Where(predicate.Compile());
    }

    public Task<RoomType> GetByIdAsync(int id, Func<IQueryable<RoomType>, IQueryable<RoomType>> include)
        => throw new NotSupportedException();

    public Task<IEnumerable<RoomType>> GetAllAsync(System.Linq.Expressions.Expression<Func<RoomType, bool>> predicate = null, Func<IQueryable<RoomType>, IQueryable<RoomType>> include = null)
        => throw new NotSupportedException();

    public async Task AddAsync(RoomType entity)
    {
        using var conn = _context.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
@"insert into room_types (hotel_id, name, description, capacity, base_price, area, created_at, is_deleted)
 values (@HotelId, @Name, @Description, @Capacity, @BasePrice, @Area, @CreatedAt, false)
 returning id", entity);
        entity.Id = id;
    }

    public async Task UpdateAsync(RoomType entity)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
@"update room_types
   set hotel_id=@HotelId, name=@Name, description=@Description, capacity=@Capacity, base_price=@BasePrice, area=@Area, updated_at=@UpdatedAt
 where id=@Id and is_deleted=false", entity);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("update room_types set is_deleted=true, updated_at=now() where id=@id", new { id });
    }

    public async Task<RoomType> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<RoomType, bool>> predicate)
        => (await GetAllAsync()).FirstOrDefault(predicate.Compile());

    public Task<RoomType> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<RoomType, bool>> predicate, Func<IQueryable<RoomType>, IQueryable<RoomType>> include)
        => throw new NotSupportedException();

    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<RoomType, bool>> predicate = null)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("select count(*) from room_types where is_deleted=false");
    }

    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<RoomType, bool>> predicate, Func<IQueryable<RoomType>, IQueryable<RoomType>> include)
        => throw new NotSupportedException();

    public IQueryable<RoomType> GetQueryable() => throw new NotSupportedException();
}
