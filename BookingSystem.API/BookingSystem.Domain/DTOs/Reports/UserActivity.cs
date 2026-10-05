using MongoDB.Bson.Serialization.Attributes;

namespace BookingSystem.Domain.DTOs.Reports;

public class UserActivity
{
    [BsonId]
    public object Id { get; set; } = null!;
    public int TotalActions { get; set; }
    public IEnumerable<ActionTypeCount> ActionTypeCounts { get; set; } = null!;
}
