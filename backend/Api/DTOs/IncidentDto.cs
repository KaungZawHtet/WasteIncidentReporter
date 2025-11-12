namespace Api.DTOs;

public class IncidentDto
{
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
}
