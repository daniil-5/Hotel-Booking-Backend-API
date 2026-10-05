using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Application.Services;

public class LoggingService : ILoggingService
{
    private readonly ILogRepository _logRepository;
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogRepository logRepository, ILogger<LoggingService> logger)
    {
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task LogActionAsync(int? userId, UserActionType actionType, string message, string? ipAddress = null, string? requestPath = null, string? httpMethod = null)
    {
        var log = new LogEntity
        {
            UserId = userId,
            ActionType = actionType,
            Message = message,
            Level = "Information",
            IpAddress = ipAddress,
            RequestPath = requestPath,
            HttpMethod = httpMethod,
            Timestamp = DateTime.UtcNow
        };
        await _logRepository.AddLogAsync(log);
        _logger.LogInformation("Logged action: {ActionType} for user {UserId}", actionType, userId);
    }

    public async Task LogErrorAsync(Exception ex, int? userId = null, string? ipAddress = null, string? requestPath = null, string? httpMethod = null)
    {
        var log = new LogEntity
        {
            UserId = userId,
            Message = ex.Message,
            Level = "Error",
            Exception = ex.ToString(),
            IpAddress = ipAddress,
            RequestPath = requestPath,
            HttpMethod = httpMethod,
            Timestamp = DateTime.UtcNow
        };
        await _logRepository.AddLogAsync(log);
        _logger.LogError(ex, "Logged error: {Message}", ex.Message);
    }

    public async Task<IEnumerable<LogEntity>> GetLogsAsync(DateTime? startDate, DateTime? endDate, int? userId, string? eventType)
    {
        return await _logRepository.GetLogsAsync(startDate, endDate, userId, eventType);
    }
}
