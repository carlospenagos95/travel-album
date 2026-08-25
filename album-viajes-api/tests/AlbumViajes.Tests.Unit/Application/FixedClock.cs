using AlbumViajes.Application.Abstractions;

namespace AlbumViajes.Tests.Unit.Application;

/// <summary>Reloj detenido: hace que los tests no dependan de la hora real.</summary>
internal sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}
