namespace BookingSystem.Application.DTOs.Reports;

public class UserActivityReport
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public int TotalActions { get; set; }
    public Dictionary<string, int> ActionTypeCounts { get; set; } = null!;
    public DateTime LastActionTimestamp { get; set; }
}
