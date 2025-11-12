using Api.DTOs;
using Api.Entities;

namespace Api.Abstractions;

public interface IIncidentService
{
    Task<List<Incident>> ListAllIncident();
    Task<Incident?> GetIncidentByIdAsync(Guid id);
    Task<Incident> CreateIncidentAsync(IncidentDto incident);
    Task<Incident?> UpdateIncidentAsync(Guid id, IncidentDto incident);
    Task<bool> DeleteIncidentAsync(Guid id);
}
