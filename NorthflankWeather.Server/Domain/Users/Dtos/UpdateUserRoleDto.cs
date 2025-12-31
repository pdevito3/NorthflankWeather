namespace NorthflankWeather.Server.Domain.Users.Dtos;

public sealed record UpdateUserRoleDto
{
    public string Role { get; init; } = default!;
}
