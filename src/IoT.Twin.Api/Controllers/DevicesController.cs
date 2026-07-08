using IoT.Twin.Application.DTOs;
using IoT.Twin.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IoT.Twin.Api.Controllers;

/// <summary>
/// Device management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DevicesController : ControllerBase
{
    private readonly ReadingService _readingService;

    public DevicesController(ReadingService readingService)
        => _readingService = readingService;

    /// <summary>
    /// Get all device IDs
    /// </summary>
    /// <returns>List of unique device identifiers</returns>
    /// <response code="200">Returns list of device IDs</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetDevices()
        => Ok(await _readingService.GetDeviceIdsAsync());

    /// <summary>
    /// Get latest reading for a device
    /// </summary>
    /// <param name="deviceId">Device identifier (e.g., PUMP-01, FAN-03)</param>
    /// <param name="metric">Optional metric filter (temperature, vibration, pressure)</param>
    /// <returns>Latest reading for the specified device</returns>
    /// <response code="200">Returns the latest reading</response>
    /// <response code="404">Device not found</response>
    [HttpGet("{deviceId}/latest")]
    [ProducesResponseType(typeof(ReadingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingDto>> GetLatest(
        string deviceId,
        [FromQuery] string? metric = null)
    {
        var result = await _readingService.GetLatestAsync(deviceId, metric);
        return result == null ? NotFound() : Ok(result);
    }
}
