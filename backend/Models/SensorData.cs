using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models;

public class SensorData
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("sensorId")]
    public int SensorId { get; set; }

    [BsonElement("sensorType")]
    public string SensorType { get; set; } = string.Empty;

    [BsonElement("value")]
    public double Value { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("unit")]
    public string Unit { get; set; } = string.Empty;
}

public class SensorDataFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SensorType { get; set; }
    public int? SensorId { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

public class PaginatedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
