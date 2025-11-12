using System.IO;
using Api.Abstractions;
using Api.Constants;
using Api.DTOs;
using Api.Entities;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;
    private readonly TextEmbeddingService _embeddingService;
    private readonly SimilarityService _similarityService;
    private readonly WasteClassificationService _classifier;

    public IncidentsController(
        IIncidentService incidents,
        TextEmbeddingService embedding,
        SimilarityService similarity,
        WasteClassificationService classifier
    )
    {
        _incidentService = incidents;
        _embeddingService = embedding;
        _similarityService = similarity;
        _classifier = classifier;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _incidentService.ListAllIncident(skip, take);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncidentDto dto)
    {
        var vector = _embeddingService.Transform(dto.Description);
        var classification = _classifier.Predict(dto.Description);
        var (match, score) = await _similarityService.FindBestMatchAsync(vector, dto.Location);
        var created = await _incidentService.CreateIncidentAsync(dto, vector);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            new
            {
                created,
                possibleDuplicate = match is not null && score >= 0.9,
                duplicateOf = match?.Id,
                similarity = score,
                classification,
            }
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] IncidentDto dto)
    {
        var vector = _embeddingService.Transform(dto.Description);
        var updated = await _incidentService.UpdateIncidentAsync(id, dto, vector);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpGet("{id:guid}/similar")]
    public async Task<IActionResult> Similar(Guid id, [FromQuery] int take = 3)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident is null)
        {
            return NotFound();
        }

        var vector = incident.TextVector ?? _embeddingService.Transform(incident.Description);
        var matches = await _similarityService.FindTopMatchesAsync(
            vector,
            incident.Location,
            incident.Id,
            take
        );

        return Ok(
            matches.Select(m => new
            {
                m.incident.Id,
                m.incident.Description,
                m.incident.Timestamp,
                m.incident.Location,
                m.incident.Category,
                m.incident.Status,
                similarity = m.score,
            })
        );
    }

    [HttpGet("{id:guid}/classification")]
    public async Task<IActionResult> Classification(Guid id)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident is null)
        {
            return NotFound();
        }

        var result = _classifier.Predict(incident.Description);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var removed = await _incidentService.DeleteIncidentAsync(id);
        return removed ? NoContent() : NotFound();
    }

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportCsv([FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("CSV file is required.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        using var stream = file.OpenReadStream();

        var incidents = extension switch
        {
            FileTypes.Csv => await _incidentService.ImportIncidentsFromCsvAsync(stream),
            _ => throw new ArgumentException("Unsupported file format."),
        };

        return Ok(new { imported = incidents.Count });
    }
}
