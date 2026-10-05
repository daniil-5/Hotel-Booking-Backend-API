using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Interfaces;

public interface IUserActionAuditService
{
    Task<UserActionAudit?> GetByIdAsync(int id);
    Task<IEnumerable<UserActionAudit>> GetAllAsync();
    Task<IEnumerable<UserActionAudit>> GetByUserIdAsync(int userId);
    Task<IEnumerable<UserActionAudit>> GetByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate);
    Task<IEnumerable<UserActionAudit>> GetMostRecentUserActionsAsync();
    Task<IEnumerable<UserActionAudit>> GetActionsByUserAndTypeAsync(int userId, UserActionType actionType);
    Task<(IEnumerable<UserActionAudit> Items, int TotalCount)> GetWithPaginationAsync(int pageNumber, int pageSize);
    Task AuditActionAsync(int userId, UserActionType actionType, bool isSuccess);
}
