using System.Globalization;
using Api.Abstractions;
using Api.Data;
using Api.DTOs;
using Api.Entities;
using Api.Utilities;
using CsvHelper;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class IncidentService : IIncidentService
{
    private readonly AppDbContext _db;
    private readonly TextEmbeddingService _embedding;
    private readonly WasteClassificationService _classifier;

    public IncidentService(
        AppDbContext db,
        TextEmbeddingService embedding,
        WasteClassificationService classifier
    )
    {
        _db = db;
        _embedding = embedding;
        _classifier = classifier;
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
            Timestamp = incident.Timestamp ?? DateTimeOffset.UtcNow,
        };

        if (string.IsNullOrWhiteSpace(entity.Category))
        {
            var prediction = _classifier.Predict(entity.Description);
            entity.Category = prediction.Label;
        }

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
        else if (string.IsNullOrWhiteSpace(existing.Category))
        {
            var classification = _classifier.Predict(existing.Description);
            existing.Category = classification.Label;
        }

        if (incident.Timestamp.HasValue)
        {
            existing.Timestamp = incident.Timestamp.Value;
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

    public async Task<List<Incident>> ImportIncidentsFromCsvAsync(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions =
                CsvHelper.Configuration.TrimOptions.Trim
                | CsvHelper.Configuration.TrimOptions.InsideQuotes,
            IgnoreBlankLines = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<IncidentCsvMap>();

        var records = new List<IncidentCsvRecord>();
        await foreach (var record in csv.GetRecordsAsync<IncidentCsvRecord>())
        {
            records.Add(record);
        }

        var incidents = records
            .Where(r => !string.IsNullOrWhiteSpace(r.Description))
            .AsParallel()
            .Select(record =>
            {
                var timestamp = DateTimeOffset.UtcNow;
                if (
                    !string.IsNullOrWhiteSpace(record.Timestamp)
                    && DateTimeOffset.TryParse(record.Timestamp, out var parsedTs)
                )
                {
                    timestamp = parsedTs;
                }

                var description = record.Description!;
                var category = string.IsNullOrWhiteSpace(record.Category)
                    ? _classifier.Predict(description).Label
                    : record.Category!;
                var vector = _embedding.Transform(description);

                return new Incident
                {
                    Description = description,
                    Location = record.Location ?? string.Empty,
                    Category = category,
                    Status = string.IsNullOrWhiteSpace(record.Status) ? "open" : record.Status!,
                    Timestamp = timestamp,
                    TextVector = vector,
                };
            })
            .ToList();

        if (incidents.Count == 0)
        {
            return incidents;
        }

        await _db.Incidents.AddRangeAsync(incidents);
        await _db.SaveChangesAsync();
        return incidents;
    }
}
