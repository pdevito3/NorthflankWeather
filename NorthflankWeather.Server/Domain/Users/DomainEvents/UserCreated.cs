namespace NorthflankWeather.Server.Domain.Users.DomainEvents;

public sealed record UserCreated(User User) : DomainEvent;
