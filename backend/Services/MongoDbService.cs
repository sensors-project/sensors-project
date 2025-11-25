using MongoDB.Driver;
using backend.Models;

namespace backend.Services;

public class MongoDbService
{
    private readonly IMongoCollection<SensorData> _sensorDataCollection;
    private readonly ILogger<MongoDbService> _logger;

    public MongoDbService(IConfiguration configuration, ILogger<MongoDbService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["MONGODB_CONNECTION_STRING"] 
            ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
            ?? "mongodb://root:example@localhost:27017/?authSource=admin";
        
        var databaseName = configuration["MONGODB_DATABASE"] 
            ?? Environment.GetEnvironmentVariable("MONGODB_DATABASE")
            ?? "sensorsdb";

        _logger.LogInformation("Connecting to MongoDB at {ConnectionString}", connectionString);
        
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _sensorDataCollection = database.GetCollection<SensorData>("sensor_data");
        
        // Create indexes for better query performance
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        try
        {
            var indexKeys = Builders<SensorData>.IndexKeys
                .Ascending(x => x.SensorType)
                .Ascending(x => x.SensorId)
                .Descending(x => x.Timestamp);
            
            _sensorDataCollection.Indexes.CreateOne(new CreateIndexModel<SensorData>(indexKeys));
            _logger.LogInformation("MongoDB indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create indexes");
        }
    }

    public async Task InsertAsync(SensorData sensorData)
    {
        await _sensorDataCollection.InsertOneAsync(sensorData);
        _logger.LogDebug("Inserted sensor data: {SensorType} - {Value}", sensorData.SensorType, sensorData.Value);
    }

    public async Task<PaginatedResult<SensorData>> GetFilteredDataAsync(SensorDataFilter filter)
    {
        var filterBuilder = Builders<SensorData>.Filter;
        var filters = new List<FilterDefinition<SensorData>>();

        if (filter.StartDate.HasValue)
        {
            filters.Add(filterBuilder.Gte(x => x.Timestamp, filter.StartDate.Value));
        }

        if (filter.EndDate.HasValue)
        {
            filters.Add(filterBuilder.Lte(x => x.Timestamp, filter.EndDate.Value));
        }

        if (!string.IsNullOrEmpty(filter.SensorType))
        {
            filters.Add(filterBuilder.Eq(x => x.SensorType, filter.SensorType));
        }

        if (filter.SensorId.HasValue)
        {
            filters.Add(filterBuilder.Eq(x => x.SensorId, filter.SensorId.Value));
        }

        var combinedFilter = filters.Count > 0 
            ? filterBuilder.And(filters) 
            : filterBuilder.Empty;

        var totalCount = await _sensorDataCollection.CountDocumentsAsync(combinedFilter);

        var sortBuilder = Builders<SensorData>.Sort;
        SortDefinition<SensorData> sort = filter.SortBy?.ToLower() switch
        {
            "sensorid" => filter.SortDescending 
                ? sortBuilder.Descending(x => x.SensorId) 
                : sortBuilder.Ascending(x => x.SensorId),
            "sensortype" => filter.SortDescending 
                ? sortBuilder.Descending(x => x.SensorType) 
                : sortBuilder.Ascending(x => x.SensorType),
            "value" => filter.SortDescending 
                ? sortBuilder.Descending(x => x.Value) 
                : sortBuilder.Ascending(x => x.Value),
            _ => filter.SortDescending 
                ? sortBuilder.Descending(x => x.Timestamp) 
                : sortBuilder.Ascending(x => x.Timestamp)
        };

        var skip = (filter.Page - 1) * filter.PageSize;
        
        var data = await _sensorDataCollection
            .Find(combinedFilter)
            .Sort(sort)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync();

        return new PaginatedResult<SensorData>
        {
            Data = data,
            TotalCount = (int)totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<SensorData>> GetAllDataAsync(SensorDataFilter filter)
    {
        var filterBuilder = Builders<SensorData>.Filter;
        var filters = new List<FilterDefinition<SensorData>>();

        if (filter.StartDate.HasValue)
        {
            filters.Add(filterBuilder.Gte(x => x.Timestamp, filter.StartDate.Value));
        }

        if (filter.EndDate.HasValue)
        {
            filters.Add(filterBuilder.Lte(x => x.Timestamp, filter.EndDate.Value));
        }

        if (!string.IsNullOrEmpty(filter.SensorType))
        {
            filters.Add(filterBuilder.Eq(x => x.SensorType, filter.SensorType));
        }

        if (filter.SensorId.HasValue)
        {
            filters.Add(filterBuilder.Eq(x => x.SensorId, filter.SensorId.Value));
        }

        var combinedFilter = filters.Count > 0 
            ? filterBuilder.And(filters) 
            : filterBuilder.Empty;

        var sortBuilder = Builders<SensorData>.Sort;
        SortDefinition<SensorData> sort = filter.SortBy?.ToLower() switch
        {
            "sensorid" => filter.SortDescending 
                ? sortBuilder.Descending(x => x.SensorId) 
                : sortBuilder.Ascending(x => x.SensorId),
            "sensortype" => filter.SortDescending 
                ? sortBuilder.Descending(x => x.SensorType) 
                : sortBuilder.Ascending(x => x.SensorType),
            "value" => filter.SortDescending 
                ? sortBuilder.Descending(x => x.Value) 
                : sortBuilder.Ascending(x => x.Value),
            _ => filter.SortDescending 
                ? sortBuilder.Descending(x => x.Timestamp) 
                : sortBuilder.Ascending(x => x.Timestamp)
        };

        return await _sensorDataCollection
            .Find(combinedFilter)
            .Sort(sort)
            .ToListAsync();
    }

    public async Task<List<string>> GetSensorTypesAsync()
    {
        return await _sensorDataCollection
            .Distinct(x => x.SensorType, Builders<SensorData>.Filter.Empty)
            .ToListAsync();
    }

    public async Task<List<int>> GetSensorIdsAsync(string? sensorType = null)
    {
        var filter = string.IsNullOrEmpty(sensorType) 
            ? Builders<SensorData>.Filter.Empty 
            : Builders<SensorData>.Filter.Eq(x => x.SensorType, sensorType);
            
        return await _sensorDataCollection
            .Distinct(x => x.SensorId, filter)
            .ToListAsync();
    }
}
