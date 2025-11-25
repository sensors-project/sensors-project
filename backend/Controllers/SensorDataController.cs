using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;
using System.Text;
using System.Text.Json;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorDataController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;
    private readonly ILogger<SensorDataController> _logger;

    public SensorDataController(MongoDbService mongoDbService, ILogger<SensorDataController> logger)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
    }

    /// <summary>
    /// Get sensor data with filtering, sorting and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<SensorData>>> GetSensorData(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? sensorType,
        [FromQuery] int? sensorId,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDescending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var filter = new SensorDataFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            SensorType = sensorType,
            SensorId = sensorId,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = Math.Min(pageSize, 1000)
        };

        var result = await _mongoDbService.GetFilteredDataAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Export sensor data as JSON
    /// </summary>
    [HttpGet("export/json")]
    public async Task<IActionResult> ExportJson(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? sensorType,
        [FromQuery] int? sensorId,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDescending = false)
    {
        var filter = new SensorDataFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            SensorType = sensorType,
            SensorId = sensorId,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var data = await _mongoDbService.GetAllDataAsync(filter);
        
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", "sensor_data.json");
    }

    /// <summary>
    /// Export sensor data as CSV
    /// </summary>
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? sensorType,
        [FromQuery] int? sensorId,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDescending = false)
    {
        var filter = new SensorDataFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            SensorType = sensorType,
            SensorId = sensorId,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var data = await _mongoDbService.GetAllDataAsync(filter);
        
        var csv = new StringBuilder();
        csv.AppendLine("Id,SensorId,SensorType,Value,Unit,Timestamp");
        
        foreach (var item in data)
        {
            csv.AppendLine($"{item.Id},{item.SensorId},{item.SensorType},{item.Value},{item.Unit},{item.Timestamp:O}");
        }
        
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", "sensor_data.csv");
    }

    /// <summary>
    /// Get available sensor types
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<List<string>>> GetSensorTypes()
    {
        var types = await _mongoDbService.GetSensorTypesAsync();
        return Ok(types);
    }

    /// <summary>
    /// Get sensor IDs, optionally filtered by sensor type
    /// </summary>
    [HttpGet("sensors")]
    public async Task<ActionResult<List<int>>> GetSensorIds([FromQuery] string? sensorType)
    {
        var ids = await _mongoDbService.GetSensorIdsAsync(sensorType);
        return Ok(ids);
    }
}
