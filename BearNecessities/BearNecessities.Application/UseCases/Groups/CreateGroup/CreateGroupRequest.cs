namespace BearNecessities.Application.UseCases.Groups.CreateGroup;

public record CreateGroupRequest(
    string Name,
    string? Description,
    string? City,
    string? Country,
    decimal Latitude,
    decimal Longitude);
