namespace Authentication.Application.Abstractions.Time;

internal static class Timestamps
{
    /// <summary>
    /// Alinha a precisao a do PostgreSQL (microssegundos), garantindo que o valor retornado
    /// logo apos a escrita seja identico ao lido do banco posteriormente.
    /// </summary>
    public static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMicrosecond));
}
