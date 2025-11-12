using CsvHelper.Configuration;

namespace Api.Utilities;

public sealed class IncidentCsvRecord
{
    public string? Description { get; set; }
    public string? Timestamp { get; set; }
    public string? Location { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
}

public sealed class IncidentCsvMap : ClassMap<IncidentCsvRecord>
{
    public IncidentCsvMap()
    {
        Map(m => m.Description).Name("description");
        Map(m => m.Timestamp).Name("timestamp").Optional();
        Map(m => m.Location).Name("location").Optional();
        Map(m => m.Category).Name("category").Optional();
        Map(m => m.Status).Name("status").Optional();
    }
}
