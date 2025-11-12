using Api.DTOs;
using Api.Entities;

namespace Api.Abstractions;

public interface IIncidentService
{
    Task<List<Incident>> ListAllIncident(int skip = 0, int take = 50);
    Task<Incident?> GetIncidentByIdAsync(Guid id);
    Task<Incident> CreateIncidentAsync(IncidentDto incident, float[]? textVector = null);
    Task<Incident?> UpdateIncidentAsync(Guid id, IncidentDto incident, float[]? textVector = null);
    Task<bool> DeleteIncidentAsync(Guid id);
}
