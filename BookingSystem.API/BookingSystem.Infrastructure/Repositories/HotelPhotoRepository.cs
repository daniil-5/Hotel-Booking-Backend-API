
using Dapper;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.Infrastructure.Repositories;

public class HotelPhotoRepository : IRepository<HotelPhoto>
{
    private readonly DapperDbContext _context;
    public HotelPhotoRepository(DapperDbContext context) { _context = context; }

    public async Task<HotelPhoto> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<HotelPhoto>(
            "select * from hotel_photos where id=@id and is_deleted=false", new { id });
    }

    public async Task<IEnumerable<HotelPhoto>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<HotelPhoto>("select * from hotel_photos where is_deleted=false");
    }

    public async Task<IEnumerable<HotelPhoto>> GetAllAsync(System.Linq.Expressions.Expression<Func<HotelPhoto, bool>> predicate)
    {
        var allPhotos = await GetAllAsync();
        return allPhotos.Where(predicate.Compile());
    }

    public Task<HotelPhoto> GetByIdAsync(int id, Func<IQueryable<HotelPhoto>, IQueryable<HotelPhoto>> include)
        => throw new NotSupportedException();

    public Task<IEnumerable<HotelPhoto>> GetAllAsync(System.Linq.Expressions.Expression<Func<HotelPhoto, bool>> predicate = null, Func<IQueryable<HotelPhoto>, IQueryable<HotelPhoto>> include = null)
        => throw new NotSupportedException();

    public async Task AddAsync(HotelPhoto entity)
    {
        using var conn = _context.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
@"insert into hotel_photos (hotel_id, public_id, url, description, created_at, is_deleted, is_main)
 values (@HotelId, @PublicId, @Url, @Description, @CreatedAt, false, @IsMain)
 returning id", entity);
        entity.Id = id;
    }

    public async Task UpdateAsync(HotelPhoto entity)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
@"update hotel_photos
   set hotel_id=@HotelId, public_id=@PublicId, url=@Url, description=@Description, is_main=@IsMain, updated_at=@UpdatedAt
 where id=@Id and is_deleted=false", entity);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("update hotel_photos set is_deleted=true, updated_at=now() where id=@id", new { id });
    }

    public async Task<HotelPhoto> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<HotelPhoto, bool>> predicate)
        => (await GetAllAsync()).FirstOrDefault(predicate.Compile());

    public Task<HotelPhoto> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<HotelPhoto, bool>> predicate, Func<IQueryable<HotelPhoto>, IQueryable<HotelPhoto>> include)
        => throw new NotSupportedException();

    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<HotelPhoto, bool>> predicate = null)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("select count(*) from hotel_photos where is_deleted=false");
    }

    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<HotelPhoto, bool>> predicate, Func<IQueryable<HotelPhoto>, IQueryable<HotelPhoto>> include)
        => throw new NotSupportedException();

    public IQueryable<HotelPhoto> GetQueryable() => throw new NotSupportedException();
}
