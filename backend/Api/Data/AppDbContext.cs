using System.Text.Json;
using Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<Incident> Incidents => Set<Incident>();

    public AppDbContext(DbContextOptions<AppDbContext> opts)
        : base(opts) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var vectorConverter = new ValueConverter<float[]?, string?>(
            v => SerializeVector(v),
            v => DeserializeVector(v)
        );

        b.Entity<Incident>()
            .Property(x => x.TextVector)
            .HasConversion(vectorConverter)
            .HasColumnType("jsonb"); // Postgres jsonb for float[]
    }

    private static string? SerializeVector(float[]? vector) =>
        vector is null ? null : JsonSerializer.Serialize(vector);

    private static float[]? DeserializeVector(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
    }
}
