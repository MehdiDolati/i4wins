using IoT.Twin.Application.DTOs;
using IoT.Twin.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IoT.Twin.Api.Controllers;

/// <summary>
/// Reading data and reporting endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReadingsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReadingsController(ReportService reportService)
        => _reportService = reportService;

    /// <summary>
    /// Get aggregated statistics (min, max, avg, count)
    /// </summary>
    /// <param name="deviceId">Optional device filter</param>
    /// <param name="metric">Optional metric filter</param>
    /// <param name="from">Optional start time</param>
    /// <param name="to">Optional end time</param>
    /// <returns>Aggregated statistics grouped by device and metric</returns>
    /// <response code="200">Returns aggregation results</response>
    [HttpGet("aggregation")]
    [ProducesResponseType(typeof(List<AggregationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AggregationDto>>> GetAggregation(
        [FromQuery] string? deviceId = null,
        [FromQuery] string? metric = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
        => Ok(await _reportService.GetAggregationAsync(deviceId, metric, from, to));

    /// <summary>
    /// Get time series data
    /// </summary>
    /// <param name="deviceId">Optional device filter</param>
    /// <param name="metric">Optional metric filter</param>
    /// <param name="from">Optional start time</param>
    /// <param name="to">Optional end time</param>
    /// <returns>Time series data points grouped by device and metric</returns>
    /// <response code="200">Returns time series data</response>
    [HttpGet("timeseries")]
    [ProducesResponseType(typeof(List<TimeSeriesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TimeSeriesDto>>> GetTimeSeries(
        [FromQuery] string? deviceId = null,
        [FromQuery] string? metric = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
        => Ok(await _reportService.GetTimeSeriesAsync(deviceId, metric, from, to));

    /// <summary>
    /// Get flagged anomalous readings
    /// </summary>
    /// <param name="deviceId">Optional device filter</param>
    /// <param name="metric">Optional metric filter</param>
    /// <param name="from">Optional start time</param>
    /// <param name="to">Optional end time</param>
    /// <returns>List of readings flagged as anomalies (|value| > 1000)</returns>
    /// <response code="200">Returns anomaly list</response>
    [HttpGet("anomalies")]
    [ProducesResponseType(typeof(List<AnomalyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AnomalyDto>>> GetAnomalies(
        [FromQuery] string? deviceId = null,
        [FromQuery] string? metric = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
        => Ok(await _reportService.GetAnomaliesAsync(deviceId, metric, from, to));
}
