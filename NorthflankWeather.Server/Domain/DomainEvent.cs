namespace NorthflankWeather.Server.Domain;

using MediatR;

public abstract record DomainEvent : INotification;
