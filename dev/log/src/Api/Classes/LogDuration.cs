using System;

namespace log;

/// <summary>
/// Measures elapsed time with a monotonic .NET time source and emits one logger event when completed.
/// </summary>
/// <remarks>
/// Completion is idempotent. The emitted event passes through the originating logger's level checks, context, filters, sinks, and child pipelines.
/// </remarks>
public class LogDuration : ILogDuration
{
    internal Logger Logger;
    internal LogLevel Level;
    internal string MessageTemplate;
    internal object?[] Values;
    internal string FieldName;
    internal TimeProvider TimeProvider;
    internal long StartTimestamp;
    internal TimeSpan Elapsed;
    internal bool IsCompleted;
    internal LogResult Result;
    internal object SyncRoot;

    internal LogDuration(Logger logger, LogLevel level, string? messageTemplate, object?[]? values, string? durationFieldName, TimeProvider? timeProvider)
    {
        Logger = logger;
        Level = level;
        MessageTemplate = LogResult.NormalizeText(messageTemplate);
        Values = LogDuration.CopyValues(values);
        FieldName = LogDuration.NormalizeFieldName(durationFieldName);
        if (timeProvider == null)
        {
            TimeProvider = TimeProvider.System;
        }
        else
        {
            TimeProvider = timeProvider;
        }

        StartTimestamp = TimeProvider.GetTimestamp();
        Elapsed = TimeSpan.Zero;
        IsCompleted = false;
        Result = LogResult.Success();
        SyncRoot = new object();
    }

    /// <summary>
    /// Completes the measurement and emits its event exactly once.
    /// </summary>
    /// <returns>The first completion result, reused by later calls.</returns>
    public LogResult Complete()
    {
        lock (SyncRoot)
        {
            if (IsCompleted)
            {
                return Result;
            }

            long endTimestamp = TimeProvider.GetTimestamp();
            Elapsed = TimeProvider.GetElapsedTime(StartTimestamp, endTimestamp);
            IsCompleted = true;
            Result = Logger.WriteDurationCompletion(Level, MessageTemplate, Values, FieldName, Elapsed);
            return Result;
        }
    }

    /// <summary>
    /// Reports whether completion has occurred.
    /// </summary>
    /// <returns>true after the first completion call; otherwise, false.</returns>
    public bool GetIsCompleted()
    {
        lock (SyncRoot)
        {
            return IsCompleted;
        }
    }

    /// <summary>
    /// Returns the measured duration, or zero before completion.
    /// </summary>
    /// <returns>The completed elapsed duration.</returns>
    public TimeSpan GetElapsed()
    {
        lock (SyncRoot)
        {
            return Elapsed;
        }
    }

    /// <summary>
    /// Returns the result from completion.
    /// </summary>
    /// <returns>The completion result, or an initial success result before completion.</returns>
    public LogResult GetResult()
    {
        lock (SyncRoot)
        {
            return Result;
        }
    }

    /// <summary>
    /// Returns the structured duration field name.
    /// </summary>
    /// <returns>The normalized field name.</returns>
    public string GetFieldName()
    {
        return FieldName;
    }

    internal static object?[] CopyValues(object?[]? values)
    {
        if (values == null)
        {
            return new object[0];
        }

        object?[] copy = new object?[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static string NormalizeFieldName(string? fieldName)
    {
        string normalizedName = LogResult.NormalizeText(fieldName).Trim();
        if (normalizedName.Length == 0)
        {
            return "Duration";
        }

        return normalizedName;
    }
}
