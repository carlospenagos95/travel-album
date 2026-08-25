namespace AlbumViajes.Application.Abstractions;

/// <summary>El tiempo como dependencia inyectada, para que los tests sean deterministas.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
