namespace BookingSystem.Application.DTOs.Reports;

public class TopUserReport
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public int TotalActions { get; set; }
    public DateTime LastActionTimestamp { get; set; }
    public Dictionary<string, int> ActionTypeCounts { get; set; } = null!;
}
