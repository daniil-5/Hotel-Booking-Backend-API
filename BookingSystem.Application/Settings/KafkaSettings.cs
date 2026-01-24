
namespace BookingSystem.Application.Settings;

public class KafkaSettings
{
    public string BootstrapServers { get; set; }
    public string GroupId { get; set; }
    public KafkaTopics Topics { get; set; }
}

public class KafkaTopics
{
    public string BookingRequests { get; set; }
    public string BookingResults { get; set; }
}
