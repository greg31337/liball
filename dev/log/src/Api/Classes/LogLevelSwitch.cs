namespace log;

/// <summary>
/// Stores a thread-safe minimum level that can be changed while loggers are running.
/// </summary>
public class LogLevelSwitch : ILogLevelSwitch
{
    internal LogLevel Level;
    internal object SyncRoot = new object();

    /// <summary>
    /// Initializes a new level switch.
    /// </summary>
    public LogLevelSwitch()
    {
        Level = LogLevel.Info;
        SyncRoot = new object();
    }

    /// <summary>
    /// Creates a level switch with the supplied minimum level.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>The resulting log level switch.</returns>
    internal static LogLevelSwitch Create(LogLevel level)
    {
        LogLevelSwitch levelSwitch = new LogLevelSwitch();
        levelSwitch.SetLevel(level);
        return levelSwitch;
    }

    /// <summary>
    /// Returns the current minimum level while holding the switch lock.
    /// </summary>
    /// <returns>The normalized current level.</returns>
    public LogLevel GetLevel()
    {
        lock (SyncRoot)
        {
            return Level;
        }
    }

    /// <summary>
    /// Atomically replaces the minimum level.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    public void SetLevel(LogLevel level)
    {
        lock (SyncRoot)
        {
            Level = LogLevelSwitch.NormalizeLevel(level);
        }
    }

    /// <summary>
    /// Reports whether an event level meets the current minimum.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    public bool IsEnabled(LogLevel level)
    {
        LogLevel minimumLevel = GetLevel();
        LogLevel normalizedLevel = LogLevelSwitch.NormalizeLevel(level);

        if (minimumLevel == LogLevel.Off)
        {
            return false;
        }

        if (normalizedLevel == LogLevel.Off)
        {
            return false;
        }

        return (int)normalizedLevel >= (int)minimumLevel;
    }

    internal static LogLevel NormalizeLevel(LogLevel level)
    {
        int value = (int)level;
        if (value < (int)LogLevel.Trace)
        {
            return LogLevel.Trace;
        }

        if (value > (int)LogLevel.Off)
        {
            return LogLevel.Off;
        }

        return level;
    }
}
