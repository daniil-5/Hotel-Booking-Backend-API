namespace BookingSystem.Application.DTOs.Reports;

public class AnomalyReport
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public int TotalActions { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public string AnomalyDescription { get; set; } = null!;
}
