namespace Api.Entities;

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // free text
    public string Status { get; set; } = "open"; // open/resolved/etc.

    // Persisted dense vector (optional): JSON float[]
    public float[]? TextVector { get; set; }
}
