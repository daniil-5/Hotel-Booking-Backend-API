namespace BookingSystem.Domain.Other;

public class MongoDbSettings
{
    public string DatabaseName { get; set; } = null!;
    public string UserActionsCollectionName { get; set; } = null!;
    public int LogsTtlDays { get; set; }
}
