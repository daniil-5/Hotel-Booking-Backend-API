using System;

namespace BookingSystem.Application.DTOs.Reports;

public class TimeSeriesReport
{
    public DateTime Date { get; set; }
    public int TotalActions { get; set; }
    public Dictionary<string, int> ActionTypeCounts { get; set; } = null!;
}
