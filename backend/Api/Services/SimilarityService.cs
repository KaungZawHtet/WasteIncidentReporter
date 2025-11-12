using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        // Optional: locality filter improves precision
        var q = _db.Incidents.AsNoTracking().Where(i => i.TextVector != null);
        if (!string.IsNullOrWhiteSpace(nearLocation))
        {
            q = q.Where(i => i.Location == nearLocation);
        }

        var candidates = await q.ToListAsync();
        Incident? best = null;
        float bestScore = 0;
        foreach (var c in candidates)
        {
            var s = Cosine(vector, c.TextVector!);
            if (s > bestScore)
            {
                bestScore = s;
                best = c;
            }
        }
        return (best, bestScore);
    }
}
