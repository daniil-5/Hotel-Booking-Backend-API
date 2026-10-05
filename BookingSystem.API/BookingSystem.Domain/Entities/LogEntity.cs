using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.Entities;

public class LogEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("level")]
    public string Level { get; set; } = null!;

    [BsonElement("message")]
    public string Message { get; set; } = null!;

    [BsonElement("user_id")]
    public int? UserId { get; set; }

    [BsonElement("action_type")]
    [BsonRepresentation(BsonType.String)]
    public UserActionType? ActionType { get; set; }

    [BsonElement("ip_address")]
    public string? IpAddress { get; set; }

    [BsonElement("request_path")]
    public string? RequestPath { get; set; }

    [BsonElement("http_method")]
    public string? HttpMethod { get; set; }

    [BsonElement("exception")]
    public string? Exception { get; set; }
}
