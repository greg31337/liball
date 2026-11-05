namespace log;

internal class LogSinkThresholds
{
    internal bool HasMinimumLevel;
    internal LogLevel MinimumLevel;
    internal object SyncRoot = new object();

    internal LogSinkThresholds()
    {
        HasMinimumLevel = false;
        MinimumLevel = LogLevel.Trace;
        SyncRoot = new object();
    }

    internal void SetMinimumLevel(LogLevel level)
    {
        lock (SyncRoot)
        {
            HasMinimumLevel = true;
            MinimumLevel = LogLevelSwitch.NormalizeLevel(level);
        }
    }

    internal void ClearMinimumLevel()
    {
        lock (SyncRoot)
        {
            HasMinimumLevel = false;
            MinimumLevel = LogLevel.Trace;
        }
    }

    internal bool IsEnabled(LogLevel level)
    {
        lock (SyncRoot)
        {
            if (!HasMinimumLevel)
            {
                return LogSinkThresholds.IsLevelEnabled(level, LogLevel.Trace);
            }

            return LogSinkThresholds.IsLevelEnabled(level, MinimumLevel);
        }
    }

    internal static bool IsLevelEnabled(LogLevel level, LogLevel minimumLevel)
    {
        LogLevel normalizedLevel = LogLevelSwitch.NormalizeLevel(level);
        LogLevel normalizedMinimumLevel = LogLevelSwitch.NormalizeLevel(minimumLevel);

        if (normalizedLevel == LogLevel.Off)
        {
            return false;
        }

        if (normalizedMinimumLevel == LogLevel.Off)
        {
            return false;
        }

        return (int)normalizedLevel >= (int)normalizedMinimumLevel;
    }
}
