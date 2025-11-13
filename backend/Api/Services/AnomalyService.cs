using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class AnomalyService
{
    private readonly AppDbContext _db;

    public AnomalyService(AppDbContext db) => _db = db;

    public sealed record DailyAnomaly(DateTime Day, int Count, double ZScore, bool IsAnomaly);

    public async Task<IReadOnlyList<DailyAnomaly>> DetectDailySpikesAsync(
        int days = 30,
        int window = 7,
        double threshold = 2.0
    )
    {
        if (window < 3)
        {
            window = 3;
        }

        var start = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-days), TimeSpan.Zero);
        var raw = await _db
            .Incidents.AsNoTracking()
            .Where(i => i.Timestamp >= start)
            .GroupBy(i => i.Timestamp.Date)
            .Select(g => new { day = g.Key, count = g.Count() })
            .OrderBy(x => x.day)
            .ToListAsync();

        var ordered = raw.Select(x => (x.day, x.count)).OrderBy(x => x.day).ToList();
        var results = new List<DailyAnomaly>(ordered.Count);

        for (var i = 0; i < ordered.Count; i++)
        {
            var (day, count) = ordered[i];
            var history = ordered
                .Take(i)
                .Reverse()
                .Take(window)
                .Select(x => (double)x.count)
                .ToList();

            if (history.Count < window)
            {
                results.Add(new DailyAnomaly(day, count, 0, false));
                continue;
            }

            var mean = history.Average();
            var variance = history.Average(v => Math.Pow(v - mean, 2));
            var std = Math.Sqrt(variance);
            var z = std == 0 ? 0 : (count - mean) / std;
            results.Add(new DailyAnomaly(day, count, z, z >= threshold));
        }

        return results;
    }
}
