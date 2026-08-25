using AlbumViajes.Application.Abstractions;

namespace AlbumViajes.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    // PostgreSQL guarda timestamptz con precision de microsegundo, mientras que
    // DateTimeOffset llega a los 100 nanosegundos. Sin truncar, la misma entidad
    // devuelve una hora al crearla (en memoria) y otra al releerla (desde la
    // base). Se recorta en el origen para que ambas coincidan siempre.
    private const long TicksPerMicrosecond = 10;

    public DateTimeOffset UtcNow => Truncate(DateTimeOffset.UtcNow);

    private static DateTimeOffset Truncate(DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % TicksPerMicrosecond));
}
