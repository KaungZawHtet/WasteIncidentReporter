using Api.Abstractions;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidents;
    private readonly TextEmbeddingService _embedding;
    private readonly SimilarityService _similarity;

    public IncidentsController(
        IIncidentService incidents,
        TextEmbeddingService embedding,
        SimilarityService similarity
    )
    {
        _incidents = incidents;
        _embedding = embedding;
        _similarity = similarity;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var items = await _incidents.ListAllIncident(skip, take);
        return Ok(
            items.Select(i => new
            {
                i.Id,
                i.Description,
                i.Timestamp,
                i.Location,
                i.Category,
                i.Status,
            })
        );
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var incident = await _incidents.GetIncidentByIdAsync(id);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncidentDto dto)
    {
        var vector = _embedding.Transform(dto.Description);
        var (match, score) = await _similarity.FindBestMatchAsync(vector, dto.Location);
        var created = await _incidents.CreateIncidentAsync(dto, vector);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            new
            {
                created,
                possibleDuplicate = match is not null && score >= 0.9,
                duplicateOf = match?.Id,
                similarity = score,
            }
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] IncidentDto dto)
    {
        var vector = _embedding.Transform(dto.Description);
        var updated = await _incidents.UpdateIncidentAsync(id, dto, vector);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var removed = await _incidents.DeleteIncidentAsync(id);
        return removed ? NoContent() : NotFound();
    }
}
