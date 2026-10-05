using Dapper;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.Infrastructure.Repositories;

public class RoomPricingRepository : IRepository<RoomPricing>
{
    private readonly DapperDbContext _context;
    public RoomPricingRepository(DapperDbContext context) { _context = context; }

    public async Task<RoomPricing> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<RoomPricing>(
            "select * from room_pricings where id=@id and is_deleted=false", new { id });
    }

    public async Task<IEnumerable<RoomPricing>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<RoomPricing>("select * from room_pricings where is_deleted=false");
    }

    public async Task<IEnumerable<RoomPricing>> GetAllAsync(System.Linq.Expressions.Expression<Func<RoomPricing, bool>> predicate)
    {
        var allRoomPricing = await GetAllAsync();
        return allRoomPricing.Where(predicate.Compile());
    }

    public Task<RoomPricing> GetByIdAsync(int id, Func<IQueryable<RoomPricing>, IQueryable<RoomPricing>> include)
        => throw new NotSupportedException();

    public Task<IEnumerable<RoomPricing>> GetAllAsync(System.Linq.Expressions.Expression<Func<RoomPricing, bool>> predicate = null, Func<IQueryable<RoomPricing>, IQueryable<RoomPricing>> include = null)
        => throw new NotSupportedException();

    public async Task AddAsync(RoomPricing entity)
    {
        using var conn = _context.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
@"insert into room_pricings (room_type_id, date, price, created_at, is_deleted)
 values (@RoomTypeId, @Date, @Price, @CreatedAt, false)
 returning id", entity);
        entity.Id = id;
    }

    public async Task UpdateAsync(RoomPricing entity)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
@"update room_pricings
   set room_type_id=@RoomTypeId, date=@Date, price=@Price, updated_at=@UpdatedAt
 where id=@Id and is_deleted=false", entity);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("update room_pricings set is_deleted=true, updated_at=now() where id=@id", new { id });
    }

    public async Task<RoomPricing> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<RoomPricing, bool>> predicate)
        => (await GetAllAsync()).FirstOrDefault(predicate.Compile());

    public Task<RoomPricing> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<RoomPricing, bool>> predicate, Func<IQueryable<RoomPricing>, IQueryable<RoomPricing>> include)
        => throw new NotSupportedException();

    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<RoomPricing, bool>> predicate = null)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("select count(*) from room_pricings where is_deleted=false");
    }

    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<RoomPricing, bool>> predicate, Func<IQueryable<RoomPricing>, IQueryable<RoomPricing>> include)
        => throw new NotSupportedException();

    public IQueryable<RoomPricing> GetQueryable() => throw new NotSupportedException();
}
