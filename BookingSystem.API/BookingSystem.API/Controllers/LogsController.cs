using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILoggingService _loggingService;

    public LogsController(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLogs([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int? userId, [FromQuery] string? eventType)
    {
        var logs = await _loggingService.GetLogsAsync(startDate, endDate, userId, eventType);
        return Ok(logs);
    }
}
