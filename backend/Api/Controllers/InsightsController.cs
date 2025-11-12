using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/insights")]
public class InsightsController : ControllerBase
{
    private readonly TrendService _trend;
    private readonly AnomalyService _anomaly;

    public InsightsController(TrendService trend, AnomalyService anomaly)
    {
        _trend = trend;
        _anomaly = anomaly;
    }

    [HttpGet("trends")]
    public async Task<IActionResult> Trends() => Ok(await _trend.GetDailyCountsAsync());

    [HttpGet("top-categories")]
    public async Task<IActionResult> TopCategories() => Ok(await _trend.TopCategoriesAsync());

    [HttpGet("admin-summary")]
    public async Task<IActionResult> AdminSummary() =>
        Ok(new { summary = await _trend.AdminSummaryAsync() });

    [HttpGet("anomalies")]
    public async Task<IActionResult> Anomalies([FromQuery] int days = 30) =>
        Ok(await _anomaly.DetectDailySpikesAsync(days));
}
