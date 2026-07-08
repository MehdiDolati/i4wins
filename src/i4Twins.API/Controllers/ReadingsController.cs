using Microsoft.AspNetCore.Mvc;
using i4Twins.Application.DTOs;
using i4Twins.Application.Interfaces;

namespace i4Twins.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly IReadingService _readingService;
    private readonly ILogger<ReadingsController> _logger;

    public ReadingsController(IReadingService readingService, ILogger<ReadingsController> logger)
    {
        _readingService = readingService;
        _logger = logger;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] string? filePath = null)
    {
        filePath ??= Path.Combine(Directory.GetCurrentDirectory(), "data", "readings.jsonl");

        if (!System.IO.File.Exists(filePath))
            return BadRequest(new { error = $"File not found: {filePath}" });

        try
        {
            var report = await _readingService.ProcessFileAsync(filePath);
            return Ok(new
            {
                message = "File processed successfully",
                report = report
            });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file");
            return StatusCode(500, new { error = "An error occurred while processing the file" });
        }
    }

    [HttpGet("aggregate")]
    public async Task<IActionResult> GetAggregatedData(
        [FromQuery] string deviceId,
        [FromQuery] string metric,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int bucketSeconds = 60)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return BadRequest(new { error = "DeviceId is required" });

        if (string.IsNullOrWhiteSpace(metric))
            return BadRequest(new { error = "Metric is required" });

        try
        {
            var result = await _readingService.GetAggregatedDataAsync(
                deviceId, metric, from, to, bucketSeconds);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aggregated data");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}