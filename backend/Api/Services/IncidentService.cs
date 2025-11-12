using Api.Abstractions;
using Api.Data;
using Api.DTOs;
using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class IncidentService : IIncidentService
{
    private readonly AppDbContext _db;
    private readonly TextEmbeddingService _embedding;

    public IncidentService(AppDbContext db, TextEmbeddingService embedding)
    {
        _db = db;
        _embedding = embedding;
    }

    public Task<List<Incident>> ListAllIncident(int skip = 0, int take = 50)
    {
        if (take <= 0)
        {
            take = 50;
        }

        take = Math.Min(take, 200);
        skip = Math.Max(0, skip);

        return _db
            .Incidents.AsNoTracking()
            .OrderByDescending(i => i.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public Task<Incident?> GetIncidentByIdAsync(Guid id)
    {
        return _db.Incidents.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Incident> CreateIncidentAsync(
        IncidentDto incident,
        float[]? textVector = null
    )
    {
        var entity = new Incident
        {
            Description = incident.Description ?? string.Empty,
            Location = incident.Location ?? string.Empty,
            Category = incident.Category ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(incident.Status) ? "open" : incident.Status,
        };

        entity.TextVector = textVector ?? _embedding.Transform(entity.Description);

        await _db.Incidents.AddAsync(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<Incident?> UpdateIncidentAsync(
        Guid id,
        IncidentDto incident,
        float[]? textVector = null
    )
    {
        var existing = await _db.Incidents.FirstOrDefaultAsync(i => i.Id == id);
        if (existing is null)
        {
            return null;
        }

        var newDescription = incident.Description ?? string.Empty;
        var descriptionChanged = !string.Equals(
            existing.Description,
            newDescription,
            StringComparison.Ordinal
        );

        existing.Description = newDescription;

        if (incident.Location is not null)
        {
            existing.Location = incident.Location;
        }

        if (incident.Category is not null)
        {
            existing.Category = incident.Category;
        }

        if (!string.IsNullOrWhiteSpace(incident.Status))
        {
            existing.Status = incident.Status;
        }

        if (descriptionChanged)
        {
            existing.TextVector = textVector ?? _embedding.Transform(existing.Description);
        }

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteIncidentAsync(Guid id)
    {
        var existing = await _db.Incidents.FirstOrDefaultAsync(i => i.Id == id);
        if (existing is null)
        {
            return false;
        }

        _db.Incidents.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
