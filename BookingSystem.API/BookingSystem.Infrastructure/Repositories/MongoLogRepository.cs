using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Domain.Other;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BookingSystem.Infrastructure.Repositories;

public class MongoLogRepository : ILogRepository
{
    private readonly IMongoCollection<LogEntity> _logCollection;

    public MongoLogRepository(IConfiguration configuration, IOptions<MongoDbSettings> mongoDbSettings)
    {
        var settings = mongoDbSettings.Value;
        var connectionString = configuration.GetConnectionString("MongoDbConnection");
        var mongoClient = new MongoClient(connectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);
        _logCollection = mongoDatabase.GetCollection<LogEntity>(settings.UserActionsCollectionName);

        // Drop the index if it exists to ensure the new TTL is applied
        try
        {
            _logCollection.Indexes.DropOne("timestamp_1");
        }
        catch (MongoCommandException ex) when (ex.Code == 27) // IndexNotFound
        {
            // Index didn't exist, so we don't need to do anything
        }

        var indexKeysDefinition = Builders<LogEntity>.IndexKeys.Ascending(x => x.Timestamp);
        var indexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(settings.LogsTtlDays) };
        var indexModel = new CreateIndexModel<LogEntity>(indexKeysDefinition, indexOptions);
        _logCollection.Indexes.CreateOne(indexModel);
    }

    public async Task AddLogAsync(LogEntity log)
    {
        await _logCollection.InsertOneAsync(log);
    }

    public async Task<IEnumerable<LogEntity>> GetLogsAsync(DateTime? startDate, DateTime? endDate, int? userId, string? eventType)
    {
        var filterBuilder = Builders<LogEntity>.Filter;
        var filter = filterBuilder.Empty;

        if (startDate.HasValue)
        {
            filter &= filterBuilder.Gte(log => log.Timestamp, startDate.Value);
        }

        if (endDate.HasValue)
        {
            filter &= filterBuilder.Lte(log => log.Timestamp, endDate.Value);
        }

        if (userId.HasValue)
        {
            filter &= filterBuilder.Eq(log => log.UserId, userId.Value);
        }

        if (!string.IsNullOrEmpty(eventType)
            && Enum.TryParse<Domain.Enums.UserActionType>(eventType, ignoreCase: true, out var parsed))
        {
            filter &= filterBuilder.Eq(log => log.ActionType, parsed);
        }

        return await _logCollection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Domain.DTOs.Reports.TopUser>> GetUserActivityReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var matchFilter = new BsonDocument("action_type", new BsonDocument("$ne", BsonNull.Value));

        if (startDate.HasValue || endDate.HasValue)
        {
            var dateFilter = new BsonDocument();
            if (startDate.HasValue)
            {
                dateFilter.Add("$gte", startDate.Value);
            }
            if (endDate.HasValue)
            {
                dateFilter.Add("$lte", endDate.Value);
            }
            matchFilter.Add("timestamp", dateFilter);
        }

        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$match", matchFilter),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument { { "user_id", "$user_id" }, { "action_type", "$action_type" } } },
                { "count", new BsonDocument("$sum", 1) },
                { "LastActionTimestamp", new BsonDocument("$max", "$timestamp") }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$_id.user_id" },
                { "TotalActions", new BsonDocument("$sum", "$count") },
                { "LastActionTimestamp", new BsonDocument("$max", "$LastActionTimestamp") },
                { "ActionTypeCounts", new BsonDocument("$push", new BsonDocument
                    {
                        { "ActionType", "$_id.action_type" },
                        { "Count", "$count" }
                    })
                }
            }),
            new BsonDocument("$sort", new BsonDocument("TotalActions", -1)),
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "UserId", "$_id" },
                { "TotalActions", "$TotalActions" },
                { "LastActionTimestamp", "$LastActionTimestamp" },
                { "ActionTypeCounts", "$ActionTypeCounts" }
            })
        };

        var result = await _logCollection.Aggregate<BookingSystem.Domain.DTOs.Reports.TopUser>(pipeline).ToListAsync();
        return result;
    }

    public async Task<IEnumerable<BookingSystem.Domain.DTOs.Reports.TopUser>> GetTopUsersReportAsync()
    {
        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$match", new BsonDocument("action_type", new BsonDocument("$ne", BsonNull.Value))),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument { { "user_id", "$user_id" }, { "action_type", "$action_type" } } },
                { "count", new BsonDocument("$sum", 1) },
                { "LastActionTimestamp", new BsonDocument("$max", "$timestamp") }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$_id.user_id" },
                { "TotalActions", new BsonDocument("$sum", "$count") },
                { "LastActionTimestamp", new BsonDocument("$max", "$LastActionTimestamp") },
                { "ActionTypeCounts", new BsonDocument("$push", new BsonDocument
                    {
                        { "ActionType", "$_id.action_type" },
                        { "Count", "$count" }
                    })
                }
            }),
            new BsonDocument("$sort", new BsonDocument("TotalActions", -1)),
            new BsonDocument("$limit", 10),
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "UserId", "$_id" },
                { "TotalActions", "$TotalActions" },
                { "LastActionTimestamp", "$LastActionTimestamp" },
                { "ActionTypeCounts", "$ActionTypeCounts" }
            })
        };

        var result = await _logCollection.Aggregate<BookingSystem.Domain.DTOs.Reports.TopUser>(pipeline).ToListAsync();
        return result;
    }

    public async Task<IEnumerable<BookingSystem.Domain.DTOs.Reports.OperationDistribution>> GetOperationDistributionReportAsync()
    {
        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$match", new BsonDocument("action_type", new BsonDocument("$ne", BsonNull.Value))),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$action_type" },
                { "Count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("Count", -1))
        };

        var result = await _logCollection.Aggregate<BookingSystem.Domain.DTOs.Reports.OperationDistribution>(pipeline).ToListAsync();
        return result;
    }

    public async Task<IEnumerable<BookingSystem.Domain.DTOs.Reports.TimeSeries>> GetTimeSeriesReportAsync()
    {
        var groupBy = new BsonDocument
        {
            { "year", new BsonDocument("$year", "$timestamp") },
            { "month", new BsonDocument("$month", "$timestamp") },
            { "day", new BsonDocument("$dayOfMonth", "$timestamp") }
        };

        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$match", new BsonDocument("action_type", new BsonDocument("$ne", BsonNull.Value))),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument { { "period", groupBy }, { "action_type", "$action_type" } } },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$_id.period" },
                { "TotalActions", new BsonDocument("$sum", "$count") },
                { "ActionTypeCounts", new BsonDocument("$push", new BsonDocument
                    {
                        { "ActionType", "$_id.action_type" },
                        { "Count", "$count" }
                    })
                }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var result = await _logCollection.Aggregate<BookingSystem.Domain.DTOs.Reports.TimeSeries>(pipeline).ToListAsync();
        return result;
    }

    public async Task<IEnumerable<Domain.DTOs.Reports.Anomaly>> GetAnomalyReportAsync(int threshold, int windowInMinutes)
    {
        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "user_id", "$user_id" },
                        { "time_window", new BsonDocument("$dateTrunc", new BsonDocument
                            {
                                { "date", "$timestamp" },
                                { "unit", "minute" },
                                { "binSize", windowInMinutes }
                            })
                        }
                    }
                },
                { "TotalActions", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$match", new BsonDocument
            {
                { "TotalActions", new BsonDocument("$gt", threshold) }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "UserId", "$_id.user_id" },
                { "TotalActions", "$TotalActions" },
                { "WindowStart", "$_id.time_window" },
                { "WindowEnd", new BsonDocument("$add", new BsonArray { "$_id.time_window", windowInMinutes * 60 * 1000 }) }
            })
        };

        var result = await _logCollection.Aggregate<BookingSystem.Domain.DTOs.Reports.Anomaly>(pipeline).ToListAsync();
        return result;
    }
}
