using BookingSystem.Domain.Entities;
using BookingSystem.Domain.DTOs.Reports;

namespace BookingSystem.Domain.Interfaces;

public interface ILogRepository
{
    Task AddLogAsync(LogEntity log);
    Task<IEnumerable<LogEntity>> GetLogsAsync(DateTime? startDate, DateTime? endDate, int? userId, string? eventType);
    Task<IEnumerable<TopUser>> GetUserActivityReportAsync(DateTime? startDate, DateTime? endDate);
    Task<IEnumerable<TopUser>> GetTopUsersReportAsync();
    Task<IEnumerable<OperationDistribution>> GetOperationDistributionReportAsync();
    Task<IEnumerable<TimeSeries>> GetTimeSeriesReportAsync();
    Task<IEnumerable<Anomaly>> GetAnomalyReportAsync(int threshold, int windowInMinutes);
}
