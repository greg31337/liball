using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Evaluates events using built-in level, category, field, sampling, or rate-limit rules.
/// </summary>
public class LogFilter : ILogFilter
{
    internal const string MinimumLevelKind = "minimumLevel";
    internal const string CategoryPrefixKind = "categoryPrefix";
    internal const string FieldExistsKind = "fieldExists";
    internal const string FieldEqualsKind = "fieldEquals";
    internal const string SampleEveryKind = "sampleEvery";
    internal const string RateLimitKind = "rateLimit";

    internal string Kind = "";
    internal LogLevel Level;
    internal string Text = "";
    internal LogValue Value = LogValue.Null();
    internal int Count;
    internal int SeenCount;
    internal TimeSpan Window;
    internal DateTimeOffset WindowStartedAt;
    internal ILogClock Clock = LogClock.SystemClock();
    internal object SyncRoot = new object();
    internal bool IsInterfaceFilter;
    internal ILogFilter? InterfaceFilter;

    internal LogFilter()
    {
        Kind = "";
        Level = LogLevel.Trace;
        Text = "";
        Value = LogValue.Null();
        Count = 0;
        SeenCount = 0;
        Window = TimeSpan.Zero;
        WindowStartedAt = DateTimeOffset.MinValue;
        Clock = LogClock.SystemClock();
        SyncRoot = new object();
        IsInterfaceFilter = false;
        InterfaceFilter = null;
    }

    /// <summary>
    /// Wraps an interface-based filter for use by concrete configuration collections.
    /// </summary>
    /// <param name="filter">The filter implementation to wrap.</param>
    /// <returns>A concrete adapter that delegates event decisions to the supplied filter.</returns>
    internal static LogFilter Wrap(ILogFilter filter)
    {
        LogFilter adapter = new LogFilter();
        adapter.IsInterfaceFilter = true;
        adapter.InterfaceFilter = filter;
        return adapter;
    }

    /// <summary>
    /// Creates a filter that excludes events below the specified level.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>The configured filter.</returns>
    internal static LogFilter MinimumLevel(LogLevel level)
    {
        LogFilter filter = new LogFilter();
        filter.Kind = LogFilter.MinimumLevelKind;
        filter.Level = LogLevelSwitch.NormalizeLevel(level);
        return filter;
    }

    /// <summary>
    /// Creates a deterministic filter that includes one event out of each configured count.
    /// </summary>
    /// <param name="count">The sampling, rate, or item count.</param>
    /// <returns>The configured filter.</returns>
    internal static LogFilter SampleEvery(int count)
    {
        LogFilter filter = new LogFilter();
        filter.Kind = LogFilter.SampleEveryKind;
        filter.Count = LogFilter.NormalizePositiveCount(count);
        return filter;
    }

    /// <summary>
    /// Creates a filter that includes at most the specified number of events per second.
    /// </summary>
    /// <param name="count">The sampling, rate, or item count.</param>
    /// <returns>The configured filter.</returns>
    internal static LogFilter RateLimit(int count)
    {
        return LogFilter.RateLimitWithClock(count, TimeSpan.FromSeconds(1), LogClock.SystemClock());
    }

    /// <summary>
    /// Creates a filter that includes at most the specified number of events in each time window.
    /// </summary>
    /// <param name="count">The sampling, rate, or item count.</param>
    /// <param name="window">The rate-limit time window.</param>
    /// <returns>The configured filter.</returns>
    internal static LogFilter RateLimitWindow(int count, TimeSpan window)
    {
        return LogFilter.RateLimitWithClock(count, window, LogClock.SystemClock());
    }

    /// <summary>
    /// Creates a rate-limit filter using an injected time source.
    /// </summary>
    /// <param name="count">The maximum number of included events per window.</param>
    /// <param name="window">The rate-limit time window.</param>
    /// <param name="clock">The UTC time source.</param>
    /// <returns>The configured filter.</returns>
    internal static LogFilter RateLimitWithClock(int count, TimeSpan window, ILogClock clock)
    {
        LogFilter filter = new LogFilter();
        filter.Kind = LogFilter.RateLimitKind;
        filter.Count = LogFilter.NormalizeNonNegativeCount(count);
        filter.Window = LogFilter.NormalizeWindow(window);
        if (clock == null)
        {
            filter.Clock = LogClock.SystemClock();
        }
        else
        {
            filter.Clock = clock;
        }

        return filter;
    }

    /// <summary>
    /// Creates a minimum-level filter for a logger category and its dotted descendants.
    /// </summary>
    /// <param name="prefix">The logger-name prefix matched on dotted category boundaries.</param>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>The configured filter.</returns>
    internal static LogFilter CategoryPrefix(string prefix, LogLevel level)
    {
        LogFilter filter = new LogFilter();
        filter.Kind = LogFilter.CategoryPrefixKind;
        filter.Level = LogLevelSwitch.NormalizeLevel(level);
        filter.Text = LogName.NormalizeLoggerName(prefix);
        return filter;
    }

    /// <summary>
    /// Creates a filter that includes only events containing the named field.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The configured filter.</returns>
    internal static LogFilter FieldExists(string name)
    {
        LogFilter filter = new LogFilter();
        filter.Kind = LogFilter.FieldExistsKind;
        filter.Text = LogResult.NormalizeText(name);
        return filter;
    }

    /// <summary>
    /// Creates a filter that includes only events whose named field equals the normalized value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The configured filter.</returns>
    internal static LogFilter FieldEquals(string name, object value)
    {
        LogFilter filter = new LogFilter();
        filter.Kind = LogFilter.FieldEqualsKind;
        filter.Text = LogResult.NormalizeText(name);
        filter.Value = LogTemplateBinder.ToLogValue(value);
        return filter;
    }

    /// <summary>
    /// Creates a filter that includes only events whose named field equals a structured value.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The configured filter.</returns>
    internal static LogFilter FieldEqualsValue(string name, LogValue value)
    {
        LogFilter filter = new LogFilter();
        filter.Kind = LogFilter.FieldEqualsKind;
        filter.Text = LogResult.NormalizeText(name);
        if (value == null)
        {
            filter.Value = LogValue.Null();
        }
        else
        {
            filter.Value = value;
        }

        return filter;
    }

    /// <summary>
    /// Evaluates this filter and returns Include, Exclude, or Neutral.
    /// </summary>
    /// <param name="logEvent">The event to evaluate, format, or write.</param>
    /// <returns>The filter's include, exclude, or neutral decision.</returns>
    public LogDecision Decide(LogEvent? logEvent)
    {
        if (logEvent == null)
        {
            return LogDecision.Neutral;
        }

        if (IsInterfaceFilter)
        {
            if (InterfaceFilter == null)
            {
                return LogDecision.Neutral;
            }

            return InterfaceFilter.Decide(logEvent);
        }

        if (Kind == LogFilter.MinimumLevelKind)
        {
            return DecideMinimumLevel(logEvent);
        }

        if (Kind == LogFilter.CategoryPrefixKind)
        {
            return DecideCategoryPrefix(logEvent);
        }

        if (Kind == LogFilter.FieldExistsKind)
        {
            return DecideFieldExists(logEvent);
        }

        if (Kind == LogFilter.FieldEqualsKind)
        {
            return DecideFieldEquals(logEvent);
        }

        if (Kind == LogFilter.SampleEveryKind)
        {
            return DecideSampleEvery();
        }

        if (Kind == LogFilter.RateLimitKind)
        {
            return DecideRateLimit();
        }

        return LogDecision.Neutral;
    }

    internal LogDecision DecideMinimumLevel(LogEvent logEvent)
    {
        LogLevel normalizedEventLevel = LogLevelSwitch.NormalizeLevel(logEvent.Level);

        if (Level == LogLevel.Off)
        {
            return LogDecision.Exclude;
        }

        if (normalizedEventLevel == LogLevel.Off)
        {
            return LogDecision.Exclude;
        }

        if ((int)normalizedEventLevel < (int)Level)
        {
            return LogDecision.Exclude;
        }

        return LogDecision.Include;
    }

    internal LogDecision DecideCategoryPrefix(LogEvent logEvent)
    {
        if (!LogFilter.CategoryMatchesPrefix(logEvent.LoggerName, Text))
        {
            return LogDecision.Neutral;
        }

        return DecideMinimumLevel(logEvent);
    }

    internal LogDecision DecideFieldExists(LogEvent logEvent)
    {
        LogField? field = LogFilter.FindField(logEvent.Fields, Text);
        if (field == null)
        {
            return LogDecision.Exclude;
        }

        return LogDecision.Include;
    }

    internal LogDecision DecideFieldEquals(LogEvent logEvent)
    {
        LogField? field = LogFilter.FindField(logEvent.Fields, Text);
        if (field == null)
        {
            return LogDecision.Exclude;
        }

        if (LogFilter.ValuesEqual(field.Value, Value))
        {
            return LogDecision.Include;
        }

        return LogDecision.Exclude;
    }

    internal LogDecision DecideSampleEvery()
    {
        lock (SyncRoot)
        {
            if (Count <= 1)
            {
                SeenCount = LogResult.IncrementCount(SeenCount);
                return LogDecision.Include;
            }

            if (SeenCount < 0)
            {
                SeenCount = 0;
            }

            if (SeenCount == int.MaxValue)
            {
                SeenCount = SeenCount % Count + 1;
            }
            else
            {
                SeenCount = SeenCount + 1;
            }

            if (SeenCount % Count == 0)
            {
                return LogDecision.Include;
            }

            return LogDecision.Exclude;
        }
    }

    internal LogDecision DecideRateLimit()
    {
        lock (SyncRoot)
        {
            DateTimeOffset now = Clock.UtcNow();
            if (WindowStartedAt == DateTimeOffset.MinValue || now < WindowStartedAt || now - WindowStartedAt >= Window)
            {
                WindowStartedAt = now;
                SeenCount = 0;
            }

            if (Count <= 0)
            {
                SeenCount = LogResult.IncrementCount(SeenCount);
                return LogDecision.Exclude;
            }

            if (SeenCount >= Count)
            {
                SeenCount = LogResult.IncrementCount(SeenCount);
                return LogDecision.Exclude;
            }

            SeenCount = SeenCount + 1;
            return LogDecision.Include;
        }
    }

    internal static LogField? FindField(LogField[]? fields, string name)
    {
        if (fields == null)
        {
            return null;
        }

        foreach (LogField field in fields)
        {
            if (field != null && field.IsValid && LogTemplateRenderer.FieldMatchesPlaceholder(field, name))
            {
                return field;
            }
        }

        return null;
    }

    internal static bool ValuesEqual(LogValue? left, LogValue? right)
    {
        LogValue? normalizedLeft = left;
        LogValue? normalizedRight = right;

        if (normalizedLeft == null)
        {
            normalizedLeft = LogValue.Null();
        }

        if (normalizedRight == null)
        {
            normalizedRight = LogValue.Null();
        }

        if (normalizedLeft.Kind != normalizedRight.Kind)
        {
            return false;
        }

        if (normalizedLeft.Kind == LogValueKind.Null)
        {
            return true;
        }

        if (normalizedLeft.Kind == LogValueKind.String || normalizedLeft.Kind == LogValueKind.Exception || normalizedLeft.Kind == LogValueKind.ObjectText)
        {
            return normalizedLeft.TextValue == normalizedRight.TextValue;
        }

        if (normalizedLeft.Kind == LogValueKind.Bool)
        {
            return normalizedLeft.BoolValue == normalizedRight.BoolValue;
        }

        if (normalizedLeft.Kind == LogValueKind.Integer)
        {
            return normalizedLeft.IntegerValue == normalizedRight.IntegerValue;
        }

        if (normalizedLeft.Kind == LogValueKind.Decimal)
        {
            return normalizedLeft.DecimalValue == normalizedRight.DecimalValue;
        }

        if (normalizedLeft.Kind == LogValueKind.FloatingPoint)
        {
            return normalizedLeft.FloatingPointValue == normalizedRight.FloatingPointValue;
        }

        if (normalizedLeft.Kind == LogValueKind.DateTime)
        {
            return normalizedLeft.DateTimeValue == normalizedRight.DateTimeValue;
        }

        if (normalizedLeft.Kind == LogValueKind.Duration)
        {
            return normalizedLeft.DurationValue == normalizedRight.DurationValue;
        }

        if (normalizedLeft.Kind == LogValueKind.Raw)
        {
            return normalizedLeft.RawValue == normalizedRight.RawValue;
        }

        if (normalizedLeft.Kind == LogValueKind.Array)
        {
            return LogFilter.ArraysEqual(normalizedLeft.ArrayValue, normalizedRight.ArrayValue);
        }

        if (normalizedLeft.Kind == LogValueKind.Dictionary)
        {
            return LogFilter.DictionariesEqual(normalizedLeft.DictionaryValue, normalizedRight.DictionaryValue);
        }

        return false;
    }

    internal static bool ArraysEqual(LogValue[] left, LogValue[] right)
    {
        LogValue[] normalizedLeft = left;
        LogValue[] normalizedRight = right;

        if (normalizedLeft == null)
        {
            normalizedLeft = new LogValue[0];
        }

        if (normalizedRight == null)
        {
            normalizedRight = new LogValue[0];
        }

        if (normalizedLeft.Length != normalizedRight.Length)
        {
            return false;
        }

        int index = 0;
        while (index < normalizedLeft.Length)
        {
            if (!LogFilter.ValuesEqual(normalizedLeft[index], normalizedRight[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool DictionariesEqual(Dictionary<string, LogValue> left, Dictionary<string, LogValue> right)
    {
        Dictionary<string, LogValue> normalizedLeft = left;
        Dictionary<string, LogValue> normalizedRight = right;

        if (normalizedLeft == null)
        {
            normalizedLeft = new Dictionary<string, LogValue>();
        }

        if (normalizedRight == null)
        {
            normalizedRight = new Dictionary<string, LogValue>();
        }

        if (normalizedLeft.Count != normalizedRight.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, LogValue> entry in normalizedLeft)
        {
            if (!normalizedRight.ContainsKey(entry.Key))
            {
                return false;
            }

            if (!LogFilter.ValuesEqual(entry.Value, normalizedRight[entry.Key]))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool CategoryMatchesPrefix(string loggerName, string prefix)
    {
        string normalizedLoggerName = LogName.NormalizeLoggerName(loggerName);
        string normalizedPrefix = LogName.NormalizeLoggerName(prefix);

        if (normalizedPrefix.Length == 0)
        {
            return true;
        }

        if (normalizedLoggerName == normalizedPrefix)
        {
            return true;
        }

        if (normalizedLoggerName.Length <= normalizedPrefix.Length)
        {
            return false;
        }

        if (!normalizedLoggerName.StartsWith(normalizedPrefix))
        {
            return false;
        }

        return normalizedLoggerName[normalizedPrefix.Length] == '.';
    }

    internal static int NormalizePositiveCount(int count)
    {
        if (count < 1)
        {
            return 1;
        }

        return count;
    }

    internal static int NormalizeNonNegativeCount(int count)
    {
        if (count < 0)
        {
            return 0;
        }

        return count;
    }

    internal static TimeSpan NormalizeWindow(TimeSpan window)
    {
        if (window <= TimeSpan.Zero)
        {
            return TimeSpan.FromSeconds(1);
        }

        return window;
    }

    internal static LogDecision DecideAll(LogFilter[] filters, LogEvent logEvent)
    {
        if (filters == null)
        {
            return LogDecision.Neutral;
        }

        bool hasInclude = false;
        foreach (LogFilter filter in filters)
        {
            if (filter == null)
            {
                continue;
            }

            LogDecision decision = filter.Decide(logEvent);
            if (decision == LogDecision.Exclude)
            {
                return LogDecision.Exclude;
            }

            if (decision == LogDecision.Include)
            {
                hasInclude = true;
            }
        }

        if (hasInclude)
        {
            return LogDecision.Include;
        }

        return LogDecision.Neutral;
    }
}
