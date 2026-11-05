namespace svg;

/// <summary>Represents one finite signed clock value in seconds.</summary>
public class SvgClockValue
{
    /// <summary>The finite signed duration in seconds.</summary>
    public double Seconds;
    /// <summary>The authored source text.</summary>
    public string Source;
    /// <summary>The first non-whitespace source offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive non-whitespace ending offset.</summary>
    public int EndOffset;

    /// <summary>Creates a source-mapped clock value.</summary>
    /// <param name="seconds">The finite seconds.</param>
    /// <param name="source">The authored source.</param>
    /// <param name="startOffset">The starting offset.</param>
    /// <param name="endOffset">The ending offset.</param>
    public SvgClockValue(double seconds, string source, int startOffset, int endOffset)
    {
        if (!SvgFiniteMath.IsFinite(seconds) || source == null || startOffset < 0 || endOffset <= startOffset || endOffset > source.Length) throw new System.ArgumentOutOfRangeException("seconds");
        Seconds = SvgFiniteMath.NormalizeZero(seconds);
        Source = source;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }
}

/// <summary>Contains a non-throwing clock-value parse result.</summary>
public class SvgClockParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgTimingParseError Error;
    /// <summary>The parsed clock value.</summary>
    public SvgClockValue? Clock;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a clock parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="clock">The parsed clock value.</param>
    /// <param name="errorOffset">The failure offset.</param>
    public SvgClockParseResult(bool success, SvgTimingParseError error, SvgClockValue? clock, int errorOffset)
    {
        Success = success;
        Error = error;
        Clock = clock;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Represents one parsed SVG timing specifier.</summary>
public class SvgTiming
{
    /// <summary>The timing specifier kind.</summary>
    public SvgTimingKind Kind;
    /// <summary>The signed offset in seconds.</summary>
    public double OffsetSeconds;
    /// <summary>The optional referenced element identifier.</summary>
    public string BaseId;
    /// <summary>The sync marker or event name.</summary>
    public string Symbol;
    /// <summary>The positive repeat iteration, or zero.</summary>
    public int RepeatIteration;
    /// <summary>The decoded access-key scalar, or an empty string.</summary>
    public string AccessKey;
    /// <summary>The exact text inside <c>wallclock(...)</c>, or an empty string.</summary>
    public string Wallclock;
    /// <summary>The complete authored source.</summary>
    public string Source;
    /// <summary>The specifier starting offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive specifier ending offset.</summary>
    public int EndOffset;
    /// <summary>Whether the mutable timing state was valid when created.</summary>
    public bool IsValid;

    /// <summary>Creates one source-preserving timing specifier.</summary>
    /// <param name="kind">The timing kind.</param>
    /// <param name="offsetSeconds">The signed offset.</param>
    /// <param name="baseId">The optional base identifier.</param>
    /// <param name="symbol">The event or synchronization symbol.</param>
    /// <param name="repeatIteration">The repeat iteration.</param>
    /// <param name="accessKey">The access key.</param>
    /// <param name="wallclock">The preserved wallclock payload.</param>
    /// <param name="source">The complete source.</param>
    /// <param name="startOffset">The starting offset.</param>
    /// <param name="endOffset">The ending offset.</param>
    public SvgTiming(SvgTimingKind kind, double offsetSeconds, string baseId, string symbol, int repeatIteration, string accessKey, string wallclock, string source, int startOffset, int endOffset)
    {
        if (!SvgFiniteMath.IsFinite(offsetSeconds) || baseId == null || symbol == null || accessKey == null || wallclock == null || source == null || startOffset < 0 || endOffset <= startOffset || endOffset > source.Length || repeatIteration < 0) throw new System.ArgumentOutOfRangeException("kind");
        Kind = kind;
        OffsetSeconds = SvgFiniteMath.NormalizeZero(offsetSeconds);
        BaseId = baseId;
        Symbol = symbol;
        RepeatIteration = repeatIteration;
        AccessKey = accessKey;
        Wallclock = wallclock;
        Source = source;
        StartOffset = startOffset;
        EndOffset = endOffset;
        IsValid = true;
    }
}

/// <summary>Contains one timing-specifier parse result.</summary>
public class SvgTimingParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgTimingParseError Error;
    /// <summary>The parsed timing specifier.</summary>
    public SvgTiming? Timing;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a timing parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="timing">The parsed timing.</param>
    /// <param name="errorOffset">The failure offset.</param>
    public SvgTimingParseResult(bool success, SvgTimingParseError error, SvgTiming? timing, int errorOffset)
    {
        Success = success;
        Error = error;
        Timing = timing;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a bounded begin or end timing list parse result.</summary>
public class SvgTimingListParseResult
{
    /// <summary>Whether the complete list parsed successfully.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgTimingParseError Error;
    /// <summary>The parsed timing prefix.</summary>
    public System.Collections.Generic.List<SvgTiming> Items;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a timing-list result and copies its items.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="items">The parsed items.</param>
    /// <param name="errorOffset">The failure offset.</param>
    public SvgTimingListParseResult(bool success, SvgTimingParseError error, System.Collections.Generic.List<SvgTiming> items, int errorOffset)
    {
        Success = success;
        Error = error;
        Items = new System.Collections.Generic.List<SvgTiming>(items);
        ErrorOffset = errorOffset;
    }
}
