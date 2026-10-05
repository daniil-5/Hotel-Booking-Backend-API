using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Interfaces;

public interface ILoggingService
{
    Task LogActionAsync(int? userId, UserActionType actionType, string message, string? ipAddress = null, string? requestPath = null, string? httpMethod = null);
    Task LogErrorAsync(Exception ex, int? userId = null, string? ipAddress = null, string? requestPath = null, string? httpMethod = null);
    Task<IEnumerable<LogEntity>> GetLogsAsync(DateTime? startDate, DateTime? endDate, int? userId, string? eventType);
}
