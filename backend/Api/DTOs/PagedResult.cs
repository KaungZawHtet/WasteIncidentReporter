namespace Api.DTOs;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
