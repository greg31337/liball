namespace svg;

/// <summary>Contains a bounded keyTimes or keyPoints parse result.</summary>
public class SvgKeyValueListParseResult
{
    /// <summary>Whether the complete list was valid.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgTimingParseError Error;
    /// <summary>The parsed value prefix.</summary>
    public double[] Values;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a key-value list result and copies its values.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="values">The parsed values.</param>
    /// <param name="errorOffset">The failure offset.</param>
    public SvgKeyValueListParseResult(bool success, SvgTimingParseError error, double[] values, int errorOffset)
    {
        Success = success;
        Error = error;
        Values = (double[])values.Clone();
        ErrorOffset = errorOffset;
    }
}

/// <summary>Represents one source-mapped cubic Bézier key spline.</summary>
public class SvgKeySpline
{
    /// <summary>The first control-point x coordinate.</summary>
    public double X1;
    /// <summary>The first control-point y coordinate.</summary>
    public double Y1;
    /// <summary>The second control-point x coordinate.</summary>
    public double X2;
    /// <summary>The second control-point y coordinate.</summary>
    public double Y2;
    /// <summary>The spline starting source offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive spline ending source offset.</summary>
    public int EndOffset;

    /// <summary>Creates a normalized source-mapped key spline.</summary>
    /// <param name="x1">The first x coordinate.</param>
    /// <param name="y1">The first y coordinate.</param>
    /// <param name="x2">The second x coordinate.</param>
    /// <param name="y2">The second y coordinate.</param>
    /// <param name="startOffset">The starting offset.</param>
    /// <param name="endOffset">The ending offset.</param>
    public SvgKeySpline(double x1, double y1, double x2, double y2, int startOffset, int endOffset)
    {
        if (!InUnitRange(x1) || !InUnitRange(y1) || !InUnitRange(x2) || !InUnitRange(y2) || startOffset < 0 || endOffset <= startOffset) throw new System.ArgumentOutOfRangeException("x1");
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    internal static bool InUnitRange(double value)
    {
        return SvgFiniteMath.IsFinite(value) && value >= 0 && value <= 1;
    }
}

/// <summary>Contains a bounded keySplines parse result.</summary>
public class SvgKeySplineListParseResult
{
    /// <summary>Whether the complete list was valid.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgTimingParseError Error;
    /// <summary>The parsed spline prefix.</summary>
    public System.Collections.Generic.List<SvgKeySpline> Splines;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a key-spline result and copies its splines.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="splines">The parsed splines.</param>
    /// <param name="errorOffset">The failure offset.</param>
    public SvgKeySplineListParseResult(bool success, SvgTimingParseError error, System.Collections.Generic.List<SvgKeySpline> splines, int errorOffset)
    {
        Success = success;
        Error = error;
        Splines = new System.Collections.Generic.List<SvgKeySpline>(splines);
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a parsed animation keyword enum value.</summary>
public class SvgAnimationKeywordParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgTimingParseError Error;
    /// <summary>The enum's integer value.</summary>
    public int Value;
    /// <summary>The recognized canonical keyword.</summary>
    public string Text;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates an animation keyword result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="value">The enum integer value.</param>
    /// <param name="text">The canonical keyword.</param>
    /// <param name="errorOffset">The failure offset.</param>
    public SvgAnimationKeywordParseResult(bool success, SvgTimingParseError error, int value, string text, int errorOffset)
    {
        Success = success;
        Error = error;
        Value = value;
        Text = text;
        ErrorOffset = errorOffset;
    }
}
