namespace log;

/// <summary>
/// Creates sinks that accept events without retaining or emitting them.
/// </summary>
internal class NullLogSink
{
    internal const string DefaultName = "null";

    /// <summary>
    /// Creates a null sink using default settings.
    /// </summary>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create()
    {
        return NullLogSink.Named(DefaultName);
    }

    /// <summary>
    /// Creates a null sink with the supplied name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Named(string name)
    {
        LogSink sink = LogSink.Named(name);
        sink.IsNullSink = true;
        sink.LastEvent = null;
        sink.SetResult(LogResult.Success());
        return sink;
    }

    /// <summary>
    /// Reports whether the supplied sink discards events.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsNull(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsNullSink;
    }
}
