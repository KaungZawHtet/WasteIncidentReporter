using Api.Data;
using Api.Entities;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Test;

public class AnomalyServiceTests
{
    private static AppDbContext BuildContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedIncidentsAsync(AppDbContext ctx, int[] counts, DateTimeOffset start)
    {
        for (var i = 0; i < counts.Length; i++)
        {
            for (var j = 0; j < counts[i]; j++)
            {
                ctx.Incidents.Add(
                    new Incident
                    {
                        Description = $"Incident {i}-{j}",
                        Timestamp = start.AddDays(i),
                    }
                );
            }
        }
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task DetectDailySpikesAsync_FlagsHighOutlierAfterWarmup()
    {
        await using var ctx = BuildContext(nameof(DetectDailySpikesAsync_FlagsHighOutlierAfterWarmup));
        var start = DateTimeOffset.UtcNow.Date.AddDays(-15);
        // baseline varies slightly to keep std > 0 before the spike
        var baseline = new[] { 2, 3, 2, 4, 3, 2, 3 };
        var counts = baseline.Concat(new[] { 15, 2, 2 }).ToArray();
        await SeedIncidentsAsync(ctx, counts, start);

        var service = new AnomalyService(ctx);
        var result = await service.DetectDailySpikesAsync(days: 15, window: 5, threshold: 2.0);

        var spike = result.Last(r => r.Count == 15);
        Assert.True(spike.IsAnomaly);
        Assert.True(spike.ZScore >= 2.0);

        var warmup = result.First();
        Assert.False(warmup.IsAnomaly);
    }

    [Fact]
    public async Task DetectDailySpikesAsync_NoAnomalyForStableSeries()
    {
        await using var ctx = BuildContext(nameof(DetectDailySpikesAsync_NoAnomalyForStableSeries));
        var start = DateTimeOffset.UtcNow.Date.AddDays(-10);
        var counts = Enumerable.Repeat(5, 10).ToArray();
        await SeedIncidentsAsync(ctx, counts, start);

        var service = new AnomalyService(ctx);
        var result = await service.DetectDailySpikesAsync(days: 10, window: 5, threshold: 2.0);

        Assert.All(result, r => Assert.False(r.IsAnomaly));
    }
}
