using System.Text;
using BookingSystem.Application.Interfaces;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("user-activity")]
    public async Task<IActionResult> GetUserActivityReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string format = "json")
    {
        var report = await _reportingService.GetUserActivityReportAsync(startDate, endDate);
        return await FormatAndReturn(report, format, "user_activity_report");
    }

    [HttpGet("top-users")]
    public async Task<IActionResult> GetTopUsersReport(string format = "json")
    {
        var report = await _reportingService.GetTopUsersReportAsync();
        return await FormatAndReturn(report, format, "top_users_report");
    }

    [HttpGet("operation-distribution")]
    public async Task<IActionResult> GetOperationDistributionReport(string format = "json")
    {
        var report = await _reportingService.GetOperationDistributionReportAsync();
        return await FormatAndReturn(report, format, "operation_distribution_report");
    }

    [HttpGet("time-series")]
    public async Task<IActionResult> GetTimeSeriesReport(string format = "json")
    {
        var report = await _reportingService.GetTimeSeriesReportAsync();
        return await FormatAndReturn(report, format, "time_series_report");
    }

    [HttpGet("anomalies")]
    public async Task<IActionResult> GetAnomalyReport(string format = "json", [FromQuery] int threshold = 20, [FromQuery] int windowInMinutes = 1)
    {
        var report = await _reportingService.GetAnomalyReportAsync(threshold, windowInMinutes);
        return await FormatAndReturn(report, format, "anomaly_report");
    }

    private async Task<IActionResult> FormatAndReturn<T>(IEnumerable<T> data, string format, string fileName)
    {
        if (format.ToLower() == "csv")
        {
            var memoryStream = new MemoryStream();
            await using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, 1024, true))
            await using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
            {
                await csv.WriteRecordsAsync(data);
            }
            memoryStream.Position = 0;
            return File(memoryStream, "text/csv", $"{fileName}.csv");
        }
        else
        {
            return Ok(data);
        }
    }
}
