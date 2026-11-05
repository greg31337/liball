using System;

namespace log;

/// <summary>
/// Describes time-based and size-based file rolling.
/// </summary>
public class LogRollingPolicy
{
    /// <summary>
    /// The calendar interval used for file names; None disables time-based rolling.
    /// </summary>
    public LogRollingInterval Interval;
    /// <summary>
    /// The byte threshold for rolling; zero disables size-based rolling.
    /// </summary>
    public long MaximumFileSizeBytes;

    /// <summary>
    /// Creates a policy with rolling disabled.
    /// </summary>
    public LogRollingPolicy()
    {
        Interval = LogRollingInterval.None;
        MaximumFileSizeBytes = 0;
    }

    /// <summary>
    /// Creates a rolling policy with its optional behavior disabled.
    /// </summary>
    /// <returns>The resulting log rolling policy.</returns>
    internal static LogRollingPolicy None()
    {
        return new LogRollingPolicy();
    }

    /// <summary>
    /// Creates a rolling policy using interval.
    /// </summary>
    /// <param name="interval">The UTC calendar interval used for rolling.</param>
    /// <returns>The resulting log rolling policy.</returns>
    internal static LogRollingPolicy ByInterval(LogRollingInterval interval)
    {
        LogRollingPolicy policy = new LogRollingPolicy();
        policy.Interval = LogRollingPolicy.NormalizeInterval(interval);
        policy.MaximumFileSizeBytes = 0;
        return policy;
    }

    /// <summary>
    /// Creates a rolling policy using size.
    /// </summary>
    /// <param name="maximumFileSizeBytes">The byte threshold that triggers a roll.</param>
    /// <returns>The resulting log rolling policy.</returns>
    internal static LogRollingPolicy BySize(long maximumFileSizeBytes)
    {
        LogRollingPolicy policy = new LogRollingPolicy();
        policy.Interval = LogRollingInterval.None;
        policy.MaximumFileSizeBytes = LogRollingPolicy.NormalizeMaximumFileSize(maximumFileSizeBytes);
        return policy;
    }

    /// <summary>
    /// Creates a rolling policy using interval and size.
    /// </summary>
    /// <param name="interval">The UTC calendar interval used for rolling.</param>
    /// <param name="maximumFileSizeBytes">The byte threshold that triggers a roll.</param>
    /// <returns>The resulting log rolling policy.</returns>
    internal static LogRollingPolicy ByIntervalAndSize(LogRollingInterval interval, long maximumFileSizeBytes)
    {
        LogRollingPolicy policy = new LogRollingPolicy();
        policy.Interval = LogRollingPolicy.NormalizeInterval(interval);
        policy.MaximumFileSizeBytes = LogRollingPolicy.NormalizeMaximumFileSize(maximumFileSizeBytes);
        return policy;
    }

    internal static LogResult Apply(LogSink sink, LogEvent logEvent)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.rolling.null", "rolling sink cannot be null");
        }

        if (!sink.HasRollingPolicy || sink.RollingPolicy == null)
        {
            sink.ActiveFilePath = sink.FilePath;
            return LogResult.Success();
        }

        string periodKey = LogRollingPolicy.PeriodKey(sink.RollingPolicy.Interval, logEvent.Timestamp);
        if (periodKey != sink.RollingPeriodKey)
        {
            FileLogSink.Close(sink);
            sink.RollingPeriodKey = periodKey;
            sink.RollingSequence = 0;
            sink.ActiveFilePath = LogRollingPolicy.BuildPath(sink.FilePath, sink.RollingPeriodKey, sink.RollingSequence);
            sink.FileOpenedOnce = false;
            return LogResult.Success();
        }

        if (sink.ActiveFilePath.Length == 0)
        {
            sink.ActiveFilePath = LogRollingPolicy.BuildPath(sink.FilePath, sink.RollingPeriodKey, sink.RollingSequence);
        }

        if (LogRollingPolicy.ShouldRollBySize(sink))
        {
            if (sink.RollingSequence == int.MaxValue)
            {
                return LogResult.FailureCode("sink.rolling.sequence.exhausted", "rolling file sequence is exhausted");
            }

            FileLogSink.Close(sink);
            sink.RollingSequence = sink.RollingSequence + 1;
            sink.ActiveFilePath = LogRollingPolicy.BuildPath(sink.FilePath, sink.RollingPeriodKey, sink.RollingSequence);
            sink.FileOpenedOnce = false;
        }

        return LogResult.Success();
    }

    internal static bool ShouldRollBySize(LogSink sink)
    {
        if (sink == null || sink.RollingPolicy == null)
        {
            return false;
        }

        if (sink.RollingPolicy.MaximumFileSizeBytes <= 0)
        {
            return false;
        }

        string path = FileLogSink.CurrentPath(sink);
        if (path.Length == 0 || !System.IO.File.Exists(path))
        {
            return false;
        }

        System.IO.FileInfo fileInfo = new System.IO.FileInfo(path);
        return fileInfo.Length >= sink.RollingPolicy.MaximumFileSizeBytes;
    }

    internal static string BuildPath(string filePath, string periodKey, int sequence)
    {
        string normalizedPath = FileLogSink.NormalizePath(filePath);
        if (normalizedPath.Length == 0)
        {
            return "";
        }

        string directory = "";
        string? nullableDirectory = System.IO.Path.GetDirectoryName(normalizedPath);
        if (nullableDirectory != null)
        {
            directory = nullableDirectory;
        }
        string name = System.IO.Path.GetFileNameWithoutExtension(normalizedPath);
        string extension = System.IO.Path.GetExtension(normalizedPath);
        string suffix = "";
        string normalizedPeriodKey = LogResult.NormalizeText(periodKey);
        if (normalizedPeriodKey.Length > 0)
        {
            suffix = suffix + "-" + normalizedPeriodKey;
        }

        if (sequence > 0)
        {
            suffix = suffix + "-" + sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        string fileName = name + suffix + extension;
        if (directory.Length == 0)
        {
            return fileName;
        }

        return System.IO.Path.Combine(directory, fileName);
    }

    internal static string PeriodKey(LogRollingInterval interval, DateTimeOffset timestamp)
    {
        LogRollingInterval normalizedInterval = LogRollingPolicy.NormalizeInterval(interval);
        DateTime utcTimestamp = timestamp.UtcDateTime;
        if (normalizedInterval == LogRollingInterval.Hour)
        {
            return utcTimestamp.ToString("yyyyMMddHH", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (normalizedInterval == LogRollingInterval.Day)
        {
            return utcTimestamp.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (normalizedInterval == LogRollingInterval.Month)
        {
            return utcTimestamp.ToString("yyyyMM", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (normalizedInterval == LogRollingInterval.Year)
        {
            return utcTimestamp.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }

        return "";
    }

    internal static long NormalizeMaximumFileSize(long maximumFileSizeBytes)
    {
        if (maximumFileSizeBytes < 0)
        {
            return 0;
        }

        return maximumFileSizeBytes;
    }

    internal static LogRollingInterval NormalizeInterval(LogRollingInterval interval)
    {
        if (interval == LogRollingInterval.Hour || interval == LogRollingInterval.Day || interval == LogRollingInterval.Month || interval == LogRollingInterval.Year)
        {
            return interval;
        }

        return LogRollingInterval.None;
    }
}
