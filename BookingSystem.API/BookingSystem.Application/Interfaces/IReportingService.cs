using BookingSystem.Application.DTOs.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingSystem.Application.Interfaces;

public interface IReportingService
{
    Task<IEnumerable<UserActivityReport>> GetUserActivityReportAsync(DateTime? startDate, DateTime? endDate);
    Task<IEnumerable<TopUserReport>> GetTopUsersReportAsync();
    Task<IEnumerable<OperationDistributionReport>> GetOperationDistributionReportAsync();
    Task<IEnumerable<TimeSeriesReport>> GetTimeSeriesReportAsync();
    Task<IEnumerable<AnomalyReport>> GetAnomalyReportAsync(int threshold, int windowInMinutes);
}
