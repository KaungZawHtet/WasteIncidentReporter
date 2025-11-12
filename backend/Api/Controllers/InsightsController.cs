using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/insights")]
public class InsightsController : ControllerBase
{
    private readonly TrendService _trend;

    public InsightsController(TrendService trend) => _trend = trend;

    [HttpGet("trends")]
    public async Task<IActionResult> Trends() => Ok(await _trend.GetDailyCountsAsync());

    [HttpGet("top-categories")]
    public async Task<IActionResult> TopCategories() => Ok(await _trend.TopCategoriesAsync());

    [HttpGet("admin-summary")]
    public async Task<IActionResult> AdminSummary() =>
        Ok(new { summary = await _trend.AdminSummaryAsync() });
}
