using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Infrastructure.Data;
using Dapper;

namespace BookingSystem.Infrastructure.Repositories;

public class UserActionAuditRepository : IUserActionAuditRepository
{
    private readonly DapperDbContext _context;

    public UserActionAuditRepository(DapperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserActionAudit entity)
    {
        using var connection = _context.CreateConnection();
        var sql = "INSERT INTO user_action_audit (user_id, user_action_type, is_success, created_at, updated_at, is_deleted) VALUES (@UserId, @UserActionType::user_action_type, @IsSuccess, @CreatedAt, @UpdatedAt, @IsDeleted)";
        await connection.ExecuteAsync(sql, new { entity.UserId, UserActionType = entity.UserActionType.ToString(), entity.IsSuccess, entity.CreatedAt, entity.UpdatedAt, entity.IsDeleted });
    }

    public async Task<UserActionAudit?> GetByIdAsync(int id)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM user_action_audit WHERE id = @Id";
        return await connection.QuerySingleOrDefaultAsync<UserActionAudit>(sql, new { Id = id });
    }

    public async Task<IEnumerable<UserActionAudit>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM user_action_audit";
        return await connection.QueryAsync<UserActionAudit>(sql);
    }

    public async Task<IEnumerable<UserActionAudit>> GetByUserIdAsync(int userId)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM user_action_audit WHERE user_id = @UserId";
        return await connection.QueryAsync<UserActionAudit>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserActionAudit>> GetByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM user_action_audit WHERE created_at >= @StartDate AND created_at <= @EndDate";
        return await connection.QueryAsync<UserActionAudit>(sql, new { StartDate = startDate, EndDate = endDate });
    }

    public async Task<IEnumerable<UserActionAudit>> GetMostRecentUserActionsAsync()
    {
        using var connection = _context.CreateConnection();
        var sql = @"
          WITH RankedAudits AS (
          SELECT *,
          ROW_NUMBER() OVER(PARTITION BY user_id ORDER BY created_at DESC) as rn
          FROM user_action_audit )
          SELECT *
          FROM RankedAudits
          WHERE rn = 1;
        ";
        return await connection.QueryAsync<UserActionAudit>(sql);
    }

    public async Task<IEnumerable<UserActionAudit>> GetActionsByUserAndTypeAsync(int userId, UserActionType actionType)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM user_action_audit WHERE user_id = @UserId AND user_action_type = @ActionType::user_action_type";
        return await connection.QueryAsync<UserActionAudit>(sql, new { UserId = userId, ActionType = actionType.ToString() });
    }

    public async Task<(IEnumerable<UserActionAudit> Items, int TotalCount)> GetWithPaginationAsync(int pageNumber, int pageSize)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT *
            FROM user_action_audit
            ORDER BY created_at DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM user_action_audit;
        ";

        using var multi = await connection.QueryMultipleAsync(sql, new { Offset = (pageNumber - 1) * pageSize, PageSize = pageSize });

        var items = await multi.ReadAsync<UserActionAudit>();
        var totalCount = await multi.ReadSingleAsync<int>();

        return (items, totalCount);
    }
}
