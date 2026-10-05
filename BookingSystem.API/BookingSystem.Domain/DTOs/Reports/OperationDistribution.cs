using BookingSystem.Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace BookingSystem.Domain.DTOs.Reports;

public class OperationDistribution
{
    [BsonId]
    public UserActionType ActionType { get; set; }
    public int Count { get; set; }
}
