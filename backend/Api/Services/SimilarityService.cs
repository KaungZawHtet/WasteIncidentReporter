using Api.Data;
using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class SimilarityService
{
    private readonly AppDbContext _db;

    public SimilarityService(AppDbContext db) => _db = db;

    private static float Cosine(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            return 0f;
        double dot = 0,
            na = 0,
            nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        return denom == 0 ? 0f : (float)(dot / denom);
    }

    public async Task<(Incident? match, float score)> FindBestMatchAsync(
        float[] vector,
        string? nearLocation = null
    )
    {
        var matches = await FindTopMatchesAsync(vector, nearLocation, null, 1);
        return matches.Count == 0 ? (null, 0f) : (matches[0].incident, matches[0].score);
    }

    public async Task<IReadOnlyList<(Incident incident, float score)>> FindTopMatchesAsync(
        float[] vector,
        string? nearLocation = null,
        Guid? excludeId = null,
        int take = 3
    )
    {
        take = Math.Clamp(take, 1, 10);
        var q = _db.Incidents.AsNoTracking().Where(i => i.TextVector != null);
        if (!string.IsNullOrWhiteSpace(nearLocation))
        {
            q = q.Where(i => i.Location == nearLocation);
        }

        if (excludeId.HasValue)
        {
            q = q.Where(i => i.Id != excludeId.Value);
        }

        var candidates = await q.ToListAsync();
        var scored = candidates
            .Select(c => (incident: c, score: Cosine(vector, c.TextVector!)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(take)
            .ToList();

        return scored;
    }
}
