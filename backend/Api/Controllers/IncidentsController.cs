using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Data;
using Api.DTOs;
using Api.Entities;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TextEmbeddingService _embed;
    private readonly SimilarityService _sim;

    public IncidentsController(
        AppDbContext db,
        TextEmbeddingService embed,
        SimilarityService sim
    ) => (_db, _embed, _sim) = (db, embed, sim);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncidentDto dto)
    {
        var vec = _embed.Transform(dto.Description);
        var incident = new Incident
        {
            Description = dto.Description,
            Timestamp = dto.Timestamp == default ? DateTimeOffset.UtcNow : dto.Timestamp,
            Location = dto.Location,
            Category = dto.Category,
            Status = dto.Status,
            TextVector = vec,
        };

        // Check for possible duplicate (threshold tune: 0.85 ~ 0.95)
        var (match, score) = await _sim.FindBestMatchAsync(vec, dto.Location);
        await _db.Incidents.AddAsync(incident);
        await _db.SaveChangesAsync();

        return Ok(
            new
            {
                created = incident,
                possibleDuplicate = match is not null && score >= 0.9,
                duplicateOf = match?.Id,
                similarity = score,
            }
        );
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var items = await _db
            .Incidents.AsNoTracking()
            .OrderByDescending(i => i.Timestamp)
            .Skip(skip)
            .Take(take)
            .Select(i => new
            {
                i.Id,
                i.Description,
                i.Timestamp,
                i.Location,
                i.Category,
                i.Status,
            })
            .ToListAsync();
        return Ok(items);
    }
}
