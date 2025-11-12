using Api.Data;
using Api.Entities;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Test;

public class SimilarityServiceTests
{
    private static AppDbContext BuildContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task FindBestMatchAsync_ReturnsHighestCosineCandidate()
    {
        await using var ctx = BuildContext(
            nameof(FindBestMatchAsync_ReturnsHighestCosineCandidate)
        );
        var incidents = new[]
        {
            new Incident
            {
                Description = "Plastic bottles in park",
                Location = "Park",
                TextVector = new float[] { 1, 0, 0 },
            },
            new Incident
            {
                Description = "Oil spill reported",
                Location = "Harbor",
                TextVector = new float[] { 0, 1, 0 },
            },
        };
        ctx.Incidents.AddRange(incidents);
        await ctx.SaveChangesAsync();

        var service = new SimilarityService(ctx);

        var (match, score) = await service.FindBestMatchAsync(new[] { 0f, 0.99f, 0.01f });

        Assert.NotNull(match);
        Assert.Equal("Harbor", match!.Location);
        Assert.True(score > 0.9f);
    }

    [Fact]
    public async Task FindBestMatchAsync_RespectsLocationFilter()
    {
        await using var ctx = BuildContext(nameof(FindBestMatchAsync_RespectsLocationFilter));
        ctx.Incidents.AddRange(
            new Incident
            {
                Description = "Overflowing bins downtown",
                Location = "Downtown",
                TextVector = new float[] { 0.9f, 0.1f },
            },
            new Incident
            {
                Description = "River litter",
                Location = "Riverbank",
                TextVector = new float[] { 0.1f, 0.9f },
            }
        );
        await ctx.SaveChangesAsync();

        var service = new SimilarityService(ctx);

        var (match, score) = await service.FindBestMatchAsync(
            new[] { 0.9f, 0.1f },
            nearLocation: "Riverbank"
        );

        Assert.Equal("Riverbank", match?.Location);
        Assert.True(score < 0.5f); // filter keeps weaker candidate because other location excluded
    }
}
