using System;

namespace log;

/// <summary>
/// Defines creation of logger-bound duration measurements using an injectable .NET time source.
/// </summary>
public interface ILoggerDuration
{
    /// <summary>Begins a duration measurement that writes through this logger when completed.</summary>
    LogDuration BeginDuration(LogLevel level, string? messageTemplate, object?[]? values, string? durationFieldName, TimeProvider? timeProvider);
}
