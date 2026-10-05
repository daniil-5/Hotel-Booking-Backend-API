namespace BookingSystem.Domain.DTOs.Reports;

public class TopUser
{
    public int? UserId { get; set; }
    public int TotalActions { get; set; }
    public DateTime LastActionTimestamp { get; set; }
    public IEnumerable<ActionTypeCount> ActionTypeCounts { get; set; } = null!;
}
