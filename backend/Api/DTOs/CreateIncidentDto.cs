using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.DTOs;

public sealed class CreateIncidentDto
{
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
}
