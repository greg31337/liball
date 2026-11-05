namespace svg;

/// <summary>Configures deterministic bounded stroke-outline construction.</summary>
public class SvgStrokeStyle
{
    /// <summary>The finite nonnegative full stroke width.</summary>
    public double Width;
    /// <summary>The endpoint cap.</summary>
    public SvgStrokeLineCap LineCap;
    /// <summary>The corner join.</summary>
    public SvgStrokeLineJoin LineJoin;
    /// <summary>The finite miter ratio limit, at least one.</summary>
    public double MiterLimit;
    /// <summary>The finite nonnegative authored dash lengths; an empty list means solid.</summary>
    public System.Collections.Generic.List<double> DashArray;
    /// <summary>The finite signed dash offset.</summary>
    public double DashOffset;
    /// <summary>The positive curve and round-outline approximation tolerance.</summary>
    public double Tolerance;
    /// <summary>The nonnegative maximum adaptive subdivision depth.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The positive maximum flattened centerline segment count.</summary>
    public int MaximumFlattenedSegments;
    /// <summary>The positive maximum placed dash fragment count.</summary>
    public int MaximumDashFragments;
    /// <summary>The positive maximum generated outline command count.</summary>
    public int MaximumOutputCommands;

    /// <summary>Creates SVG stroke defaults with secure deterministic limits.</summary>
    public SvgStrokeStyle()
    {
        Width = 1;
        LineCap = SvgStrokeLineCap.Butt;
        LineJoin = SvgStrokeLineJoin.Miter;
        MiterLimit = 4;
        DashArray = new System.Collections.Generic.List<double>();
        DashOffset = 0;
        Tolerance = 0.01;
        MaximumSubdivisionDepth = 32;
        MaximumFlattenedSegments = 2000000;
        MaximumDashFragments = 2000000;
        MaximumOutputCommands = 2000000;
    }
}

/// <summary>Contains an even-length normalized dash pattern and canonical offset.</summary>
public class SvgStrokeDashPatternResult
{
    /// <summary>Whether normalization succeeded.</summary>
    public bool Success;
    /// <summary>The stable dash failure.</summary>
    public SvgStrokeError Error;
    /// <summary>The copied even-length pattern, empty for a solid stroke.</summary>
    public System.Collections.Generic.List<double>? Pattern;
    /// <summary>The canonical offset in the half-open pattern interval.</summary>
    public double Offset;
    /// <summary>The full normalized pattern length.</summary>
    public double PatternLength;
    /// <summary>Whether the pattern paints a solid stroke.</summary>
    public bool IsSolid;

    /// <summary>Creates a normalized dash-pattern result.</summary>
    /// <param name="success">Whether normalization succeeded.</param><param name="error">The failure kind.</param>
    /// <param name="pattern">The even-length pattern.</param><param name="offset">The canonical offset.</param>
    /// <param name="patternLength">The pattern length.</param><param name="isSolid">Whether the stroke is solid.</param>
    public SvgStrokeDashPatternResult(bool success, SvgStrokeError error, System.Collections.Generic.List<double>? pattern, double offset, double patternLength, bool isSolid)
    {
        Success = success; Error = error; Pattern = pattern; Offset = offset; PatternLength = patternLength; IsSolid = isSolid;
    }
}

/// <summary>Describes one on-dash interval in subpath distance coordinates.</summary>
public class SvgStrokeDashPlacement
{
    /// <summary>The zero-based flattened subpath index.</summary>
    public int SubpathIndex;
    /// <summary>The nonnegative local start distance.</summary>
    public double StartDistance;
    /// <summary>The local end distance, possibly beyond subpath length when joined across a closed seam.</summary>
    public double EndDistance;
    /// <summary>Whether this fragment covers a complete closed subpath.</summary>
    public bool Closed;

    /// <summary>Creates one placed on-dash interval.</summary>
    /// <param name="subpathIndex">The subpath index.</param><param name="startDistance">The start distance.</param>
    /// <param name="endDistance">The end distance.</param><param name="closed">Whether the interval is closed.</param>
    public SvgStrokeDashPlacement(int subpathIndex, double startDistance, double endDistance, bool closed)
    {
        SubpathIndex = subpathIndex; StartDistance = startDistance; EndDistance = endDistance; Closed = closed;
    }
}

/// <summary>Contains a generated closed stroke outline and normalized dash information.</summary>
public class SvgStrokeOutlineResult
{
    /// <summary>Whether outline construction succeeded.</summary>
    public bool Success;
    /// <summary>The stable stroke failure.</summary>
    public SvgStrokeError Error;
    /// <summary>The generated nonzero-fill outline, or <c>null</c> after failure.</summary>
    public SvgPathData? PathData;
    /// <summary>The normalized dash pattern, or <c>null</c> after failure.</summary>
    public SvgStrokeDashPatternResult? DashPattern;
    /// <summary>The ordered on-dash placements, or <c>null</c> after failure.</summary>
    public System.Collections.Generic.List<SvgStrokeDashPlacement>? Dashes;

    /// <summary>Creates a stroke-outline result.</summary>
    /// <param name="success">Whether construction succeeded.</param><param name="error">The failure kind.</param>
    /// <param name="pathData">The outline path.</param><param name="dashPattern">The normalized dash pattern.</param><param name="dashes">The dash placements.</param>
    public SvgStrokeOutlineResult(bool success, SvgStrokeError error, SvgPathData? pathData, SvgStrokeDashPatternResult? dashPattern, System.Collections.Generic.List<SvgStrokeDashPlacement>? dashes)
    {
        Success = success; Error = error; PathData = pathData; DashPattern = dashPattern; Dashes = dashes;
    }
}
