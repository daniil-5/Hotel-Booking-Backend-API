namespace BookingSystem.Domain.DTOs.Reports;

public class Anomaly
{
    public int UserId { get; set; }
    public int TotalActions { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
}
