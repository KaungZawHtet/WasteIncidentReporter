using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class TrendService
{
    private readonly AppDbContext _db;

    public TrendService(AppDbContext db) => _db = db;

    public async Task<object> GetDailyCountsAsync(int days = 14)
    {
        var start = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-days), TimeSpan.Zero);
        var data = await _db
            .Incidents.AsNoTracking()
            .Where(i => i.Timestamp >= start)
            .GroupBy(i => i.Timestamp.Date)
            .Select(g => new { day = g.Key, count = g.Count() })
            .OrderBy(x => x.day)
            .ToListAsync();

        // Optional spike flag: compare last day vs mean of previous 7
        if (data.Count >= 8)
        {
            var last = data[^1].count;
            var prev7 = data.Skip(Math.Max(0, data.Count - 8))
                .Take(7)
                .Select(x => x.count)
                .ToArray();
            var mean = prev7.Average();
            var std = Math.Sqrt(prev7.Select(c => Math.Pow(c - mean, 2)).Average());
            var z = std == 0 ? 0 : (last - mean) / std;
            return new
            {
                data,
                lastDayZScore = z,
                spike = z >= 2.0,
            };
        }
        return new { data, spike = false };
    }

    public async Task<object> TopCategoriesAsync(int top = 5)
    {
        var cats = await _db
            .Incidents.AsNoTracking()
            .GroupBy(i => i.Category)
            .Select(g => new { category = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(top)
            .ToListAsync();
        return cats;
    }

    public async Task<string> AdminSummaryAsync()
    {
        var weekStart = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-7), TimeSpan.Zero);
        var weekCount = await _db.Incidents.CountAsync(i => i.Timestamp >= weekStart);
        var top = await TopCategoriesAsync(3) as IEnumerable<dynamic>;
        var topStr = string.Join(", ", top!.Select(t => $"{t.category} ({t.count})"));
        return $"Last 7 days: {weekCount} incidents. Top types: {topStr}.";
    }
}
