namespace log;

/// <summary>
/// Creates text file sinks with optional rolling, retention, and flush-on-write behavior.
/// </summary>
/// <remarks>
/// Rolling paths are derived from the configured base path. Retention runs after rolling and never deletes the active file.
/// </remarks>
internal class FileLogSink
{
    internal const string DefaultName = "file";

    /// <summary>
    /// Creates a file sink that preserves existing content and writes at the end of the file.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Append(string filePath)
    {
        return FileLogSink.WithFormatter(filePath, TextLogFormatter.Plain(), true);
    }

    /// <summary>
    /// Creates a file sink that replaces existing content when it is first opened.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Overwrite(string filePath)
    {
        return FileLogSink.WithFormatter(filePath, TextLogFormatter.Plain(), false);
    }

    /// <summary>
    /// Creates a file sink configured with the supplied formatter.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <param name="append">Whether the file is opened in append mode.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(string filePath, LogFormatter formatter, bool append)
    {
        LogSink sink = LogSink.Named(DefaultName);
        sink.IsFileSink = true;
        sink.FilePath = FileLogSink.NormalizePath(filePath);
        sink.SinkFormatter = TextWriterLogSink.NormalizeFormatter(formatter);
        sink.FileAppend = append;
        sink.FileOpenedOnce = false;
        sink.FileIsOpen = false;
        sink.FileWriter = null;
        sink.FlushOnWrite = false;
        sink.HasRollingPolicy = false;
        sink.RollingPolicy = LogRollingPolicy.None();
        sink.ActiveFilePath = sink.FilePath;
        sink.RollingPeriodKey = "";
        sink.RollingSequence = 0;
        sink.HasRetentionPolicy = false;
        sink.RetentionPolicy = LogRetentionPolicy.None();
        return sink;
    }

    /// <summary>
    /// Creates a file sink with an interface-based formatter.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <param name="append">Whether the file is opened in append mode.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(string filePath, ILogFormatter formatter, bool append)
    {
        return FileLogSink.WithFormatter(filePath, LogFormatter.Wrap(formatter), append);
    }

    /// <summary>
    /// Creates a file sink configured with the supplied rolling policy.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <param name="append">Whether the file is opened in append mode.</param>
    /// <param name="policy">The rolling or retention policy to apply.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithRollingPolicy(string filePath, LogFormatter formatter, bool append, LogRollingPolicy policy)
    {
        LogSink sink = FileLogSink.WithFormatter(filePath, formatter, append);
        FileLogSink.SetRollingPolicy(sink, policy);
        return sink;
    }

    /// <summary>
    /// Creates a rolling file sink with an interface-based formatter.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <param name="append">Whether the file is opened in append mode.</param>
    /// <param name="policy">The rolling or retention policy to apply.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithRollingPolicy(string filePath, ILogFormatter formatter, bool append, LogRollingPolicy policy)
    {
        return FileLogSink.WithRollingPolicy(filePath, LogFormatter.Wrap(formatter), append, policy);
    }

    /// <summary>
    /// Sets flush on write for this file sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="flushOnWrite">Whether the writer is flushed after every event.</param>
    internal static void SetFlushOnWrite(LogSink sink, bool flushOnWrite)
    {
        if (sink == null)
        {
            return;
        }

        sink.FlushOnWrite = flushOnWrite;
    }

    /// <summary>
    /// Sets formatter for this file sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    internal static void SetFormatter(LogSink sink, LogFormatter formatter)
    {
        if (sink == null)
        {
            return;
        }

        sink.SinkFormatter = TextWriterLogSink.NormalizeFormatter(formatter);
    }

    /// <summary>
    /// Sets an interface-based formatter for this file sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    internal static void SetFormatter(LogSink sink, ILogFormatter formatter)
    {
        FileLogSink.SetFormatter(sink, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Sets rolling policy for this file sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="policy">The rolling or retention policy to apply.</param>
    internal static void SetRollingPolicy(LogSink sink, LogRollingPolicy policy)
    {
        if (sink == null)
        {
            return;
        }

        sink.HasRollingPolicy = true;
        if (policy == null)
        {
            sink.RollingPolicy = LogRollingPolicy.None();
        }
        else
        {
            sink.RollingPolicy = policy;
        }

        sink.RollingPeriodKey = "";
        sink.RollingSequence = 0;
        sink.ActiveFilePath = "";
        sink.FileOpenedOnce = false;
    }

    /// <summary>
    /// Clears rolling policy and restores the default behavior.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    internal static void ClearRollingPolicy(LogSink sink)
    {
        if (sink == null)
        {
            return;
        }

        FileLogSink.Close(sink);
        sink.HasRollingPolicy = false;
        sink.RollingPolicy = LogRollingPolicy.None();
        sink.RollingPeriodKey = "";
        sink.RollingSequence = 0;
        sink.ActiveFilePath = sink.FilePath;
        sink.FileOpenedOnce = false;
    }

    /// <summary>
    /// Sets retention policy for this file sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="policy">The rolling or retention policy to apply.</param>
    internal static void SetRetentionPolicy(LogSink sink, LogRetentionPolicy policy)
    {
        if (sink == null)
        {
            return;
        }

        sink.HasRetentionPolicy = true;
        if (policy == null)
        {
            sink.RetentionPolicy = LogRetentionPolicy.None();
        }
        else
        {
            sink.RetentionPolicy = policy;
        }
    }

    /// <summary>
    /// Clears retention policy and restores the default behavior.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    internal static void ClearRetentionPolicy(LogSink sink)
    {
        if (sink == null)
        {
            return;
        }

        sink.HasRetentionPolicy = false;
        sink.RetentionPolicy = LogRetentionPolicy.None();
    }

    /// <summary>
    /// Reports whether the supplied sink writes to a file.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsFile(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsFileSink;
    }

    internal static LogResult WriteToFile(LogSink sink, LogEvent logEvent)
    {
        LogResult rollingResult = LogRollingPolicy.Apply(sink, logEvent);
        if (!rollingResult.IsSuccess)
        {
            return rollingResult;
        }

        LogResult openResult = FileLogSink.EnsureOpen(sink);
        if (!openResult.IsSuccess)
        {
            return openResult;
        }

        if (sink.FileWriter == null)
        {
            return LogResult.FailureCode("sink.file.writer.missing", "file writer cannot be null");
        }

        LogFormatter formatter = TextWriterLogSink.NormalizeFormatter(sink.SinkFormatter);
        sink.FileWriter.Write(formatter.FormatLine(logEvent));
        if (sink.FlushOnWrite)
        {
            sink.FileWriter.Flush();
        }

        LogRetentionPolicy.Apply(sink, logEvent.Timestamp);
        return LogSink.NormalizeResult(sink.Result);
    }

    internal static LogResult EnsureOpen(LogSink sink)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.file.null", "file sink cannot be null");
        }

        if (sink.FileWriter != null && sink.FileIsOpen)
        {
            return LogResult.Success();
        }

        string targetPath = FileLogSink.CurrentPath(sink);
        if (targetPath.Length == 0)
        {
            return LogResult.FailureCode("sink.file.path.missing", "file path cannot be empty");
        }

        string? directory = System.IO.Path.GetDirectoryName(targetPath);
        if (directory != null && directory.Length > 0)
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        System.IO.FileMode mode = System.IO.FileMode.Append;
        if (!sink.FileAppend && !sink.FileOpenedOnce)
        {
            mode = System.IO.FileMode.Create;
        }

        System.IO.FileStream stream = new System.IO.FileStream(targetPath, mode, System.IO.FileAccess.Write, System.IO.FileShare.Read);
        sink.FileWriter = new System.IO.StreamWriter(stream);
        sink.FileOpenedOnce = true;
        sink.FileIsOpen = true;
        return LogResult.Success();
    }

    /// <summary>
    /// Flushes the active file writer when the supplied sink is a file sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Flush(LogSink sink)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.file.null", "file sink cannot be null");
        }

        if (sink.FileWriter == null)
        {
            return LogResult.Success();
        }

        sink.FileWriter.Flush();
        return LogResult.Success();
    }

    /// <summary>
    /// Flushes and disposes the active file writer.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Close(LogSink sink)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.file.null", "file sink cannot be null");
        }

        if (sink.FileWriter == null)
        {
            sink.FileIsOpen = false;
            return LogResult.Success();
        }

        sink.FileWriter.Flush();
        sink.FileWriter.Dispose();
        sink.FileWriter = null;
        sink.FileIsOpen = false;
        return LogResult.Success();
    }

    internal static string NormalizePath(string filePath)
    {
        return LogResult.NormalizeText(filePath);
    }

    /// <summary>
    /// Returns the active rolled path, or the configured base path before the first roll.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>The normalized or formatted text.</returns>
    internal static string CurrentPath(LogSink sink)
    {
        if (sink == null)
        {
            return "";
        }

        if (sink.ActiveFilePath.Length > 0)
        {
            return sink.ActiveFilePath;
        }

        return sink.FilePath;
    }
}
