using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.Interfaces;

public interface IUserActionAuditRepository
{
    Task AddAsync(UserActionAudit entity);
    Task<UserActionAudit?> GetByIdAsync(int id);
    Task<IEnumerable<UserActionAudit>> GetAllAsync();
    Task<IEnumerable<UserActionAudit>> GetByUserIdAsync(int userId);
    Task<IEnumerable<UserActionAudit>> GetByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate);
    Task<IEnumerable<UserActionAudit>> GetMostRecentUserActionsAsync();
    Task<IEnumerable<UserActionAudit>> GetActionsByUserAndTypeAsync(int userId, UserActionType actionType);
    Task<(IEnumerable<UserActionAudit> Items, int TotalCount)> GetWithPaginationAsync(int pageNumber, int pageSize);
}
