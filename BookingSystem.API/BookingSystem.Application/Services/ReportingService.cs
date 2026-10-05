using BookingSystem.Application.DTOs.Reports;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingSystem.Application.Services;

public class ReportingService : IReportingService
{
    private readonly ILogRepository _logRepository;
    private readonly IUserRepository _userRepository;

    public ReportingService(ILogRepository logRepository, IUserRepository userRepository)
    {
        _logRepository = logRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<UserActivityReport>> GetUserActivityReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var result = await _logRepository.GetUserActivityReportAsync(startDate, endDate);
        var userIds = result.Select(r => r.UserId).Where(id => id.HasValue)
            .Select(id => id.Value).ToList();
        var users = await _userRepository.GetUsersByIdsAsync(userIds);

        return result.Select(r =>
        {
            var user = users.FirstOrDefault(u => u.Id == r.UserId);
            return new UserActivityReport
            {
                UserId = r.UserId ?? 0,
                Username = user?.Username ?? "Unknown",
                TotalActions = r.TotalActions,
                LastActionTimestamp = r.LastActionTimestamp,
                ActionTypeCounts = r.ActionTypeCounts.ToDictionary(atc => atc.ActionType.ToString(), atc => atc.Count)
            };
        });
    }

    public async Task<IEnumerable<TopUserReport>> GetTopUsersReportAsync()
    {
        var result = await _logRepository.GetTopUsersReportAsync();
        var userIds = result.Select(r => r.UserId).Where(id => id.HasValue)
            .Select(id => id.Value).ToList();
        var users = await _userRepository.GetUsersByIdsAsync(userIds);

        return result.Select(r =>
        {
            var user = users.FirstOrDefault(u => u.Id == r.UserId);
            return new TopUserReport
            {
                UserId = r.UserId ?? 0,
                Username = user?.Username ?? "Unknown",
                TotalActions = r.TotalActions,
                LastActionTimestamp = r.LastActionTimestamp,
                ActionTypeCounts = r.ActionTypeCounts.ToDictionary(atc => atc.ActionType.ToString(), atc => atc.Count)
            };
        });
    }

    public async Task<IEnumerable<OperationDistributionReport>> GetOperationDistributionReportAsync()
    {
        var result = await _logRepository.GetOperationDistributionReportAsync();
        var totalActions = result.Sum(r => r.Count);

        return result.Select(r => new OperationDistributionReport
        {
            ActionType = r.ActionType,
            Count = r.Count,
            Percentage = totalActions > 0 ? (double)r.Count / totalActions * 100 : 0
        });
    }

    public async Task<IEnumerable<TimeSeriesReport>> GetTimeSeriesReportAsync()
    {
        var result = await _logRepository.GetTimeSeriesReportAsync();
        return result.Select(r => new TimeSeriesReport
        {
            Date = new System.DateTime((int)((dynamic)r.Id).year, (int)((dynamic)r.Id).month, (int)((dynamic)r.Id).day),
            TotalActions = r.TotalActions,
            ActionTypeCounts = r.ActionTypeCounts.ToDictionary(atc => atc.ActionType.ToString(), atc => atc.Count)
        });
    }

    public async Task<IEnumerable<AnomalyReport>> GetAnomalyReportAsync(int threshold, int windowInMinutes)
    {
        var result = await _logRepository.GetAnomalyReportAsync(threshold, windowInMinutes);
        var userIds = result.Select(r => r.UserId).ToList();
        var users = await _userRepository.GetUsersByIdsAsync(userIds);

        return result.Select(r =>
        {
            var user = users.FirstOrDefault(u => u.Id == r.UserId);
            return new AnomalyReport
            {
                UserId = r.UserId,
                Username = user?.Username ?? "Unknown",
                TotalActions = r.TotalActions,
                WindowStart = r.WindowStart,
                WindowEnd = r.WindowEnd,
                AnomalyDescription = $"User performed {r.TotalActions} actions in a {windowInMinutes} minute window."
            };
        });
    }
}
