namespace svg;

/// <summary>Configures bounded SVG textPath projection.</summary>
public class SvgTextPathOptions
{
    /// <summary>The authored absolute or percentage start offset.</summary>
    public double StartOffset;
    /// <summary>Whether StartOffset is a percentage of calibrated path length.</summary>
    public bool StartOffsetIsPercentage;
    /// <summary>Whether PathLength supplies explicit authored calibration.</summary>
    public bool HasPathLength;
    /// <summary>The positive authored path-length calibration.</summary>
    public double PathLength;
    /// <summary>The align or stretch path method.</summary>
    public SvgTextPathMethod Method;
    /// <summary>The exact or automatically distributed spacing mode.</summary>
    public SvgTextPathSpacing Spacing;
    /// <summary>The selected path side and traversal direction.</summary>
    public SvgTextPathSide Side;
    /// <summary>The open-path endpoint overflow behavior.</summary>
    public SvgTextPathOverflow Overflow;
    /// <summary>The positive absolute path-flattening tolerance.</summary>
    public double Tolerance;
    /// <summary>The nonnegative maximum adaptive subdivision depth.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The maximum total flattened path segment count.</summary>
    public int MaximumFlattenedSegments;
    /// <summary>The maximum inline path or converted-shape command count.</summary>
    public int MaximumPathCommands;
    /// <summary>The maximum character count of one inline path number token.</summary>
    public int MaximumTokenCharacters;
    /// <summary>The maximum projected glyph count.</summary>
    public int MaximumGlyphs;
    /// <summary>The maximum point-and-frame query count.</summary>
    public int MaximumPathQueries;
    /// <summary>The cancellation token observed during synchronous layout.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates exact left-side alignment defaults.</summary>
    public SvgTextPathOptions()
    {
        StartOffset = 0;
        StartOffsetIsPercentage = false;
        HasPathLength = false;
        PathLength = 0;
        Method = SvgTextPathMethod.Align;
        Spacing = SvgTextPathSpacing.Exact;
        Side = SvgTextPathSide.Left;
        Overflow = SvgTextPathOverflow.Hidden;
        Tolerance = 0.01;
        MaximumSubdivisionDepth = 32;
        MaximumFlattenedSegments = 2000000;
        MaximumPathCommands = 1000000;
        MaximumTokenCharacters = 4096;
        MaximumGlyphs = 1000000;
        MaximumPathQueries = 2000000;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains one positioned glyph projected into a path frame.</summary>
public class SvgTextPathGlyph
{
    /// <summary>An independent copy of the planar positioned glyph.</summary>
    public SvgPositionedGlyph? Glyph;
    /// <summary>The final glyph origin on or beside the path.</summary>
    public SvgPoint? Point;
    /// <summary>The unit tangent in text traversal direction.</summary>
    public SvgVector? Tangent;
    /// <summary>The unit left normal in text traversal direction.</summary>
    public SvgVector? Normal;
    /// <summary>The final rotation in degrees.</summary>
    public double Rotation;
    /// <summary>The requested or wrapped calibrated origin distance.</summary>
    public double PathDistance;
    /// <summary>The clamped measured actual path distance.</summary>
    public double ActualPathDistance;
    /// <summary>The additional inline scale introduced by method stretch.</summary>
    public double MethodScale;
    /// <summary>Whether the glyph midpoint lies within an open path or the path is closed.</summary>
    public bool Visible;
    /// <summary>Whether a closed path wrapped this glyph origin.</summary>
    public bool Wrapped;
    /// <summary>Whether the glyph midpoint exceeded an open endpoint.</summary>
    public bool Overflowed;

    internal SvgTextPathGlyph()
    {
        Glyph = null;
        Point = null;
        Tangent = null;
        Normal = null;
        Rotation = 0;
        PathDistance = 0;
        ActualPathDistance = 0;
        MethodScale = 1;
        Visible = false;
        Wrapped = false;
        Overflowed = false;
    }
}

/// <summary>Contains measured path metadata and projected text glyphs.</summary>
public class SvgTextPathResult
{
    /// <summary>Whether textPath layout completed successfully.</summary>
    public bool Success;
    /// <summary>The stable textPath outcome.</summary>
    public SvgTextPathError Error;
    /// <summary>A deterministic failure explanation, or empty string.</summary>
    public string Message;
    /// <summary>How path geometry entered layout.</summary>
    public SvgTextPathSourceKind SourceKind;
    /// <summary>The resolved or parsed source path.</summary>
    public SvgPathData? Path;
    /// <summary>The projected glyphs in positioned-text order.</summary>
    public System.Collections.Generic.List<SvgTextPathGlyph> Glyphs;
    /// <summary>The normalized source text represented by the projected glyphs.</summary>
    public string Text;
    /// <summary>The inline and block axis arrangement used for positioning.</summary>
    public SvgWritingMode WritingMode;
    /// <summary>The inline base direction used for positioning.</summary>
    public SvgTextDirection Direction;
    /// <summary>The font size used for logical and decoration metrics.</summary>
    public double FontSize;
    /// <summary>The positive alphabetic ascent used for layout.</summary>
    public double Ascent;
    /// <summary>The positive alphabetic descent used for layout.</summary>
    public double Descent;
    /// <summary>The positive x-height used for layout.</summary>
    public double XHeight;
    /// <summary>The measured actual user-space path length.</summary>
    public double ActualPathLength;
    /// <summary>The calibrated author path length.</summary>
    public double CalibratedPathLength;
    /// <summary>The actual-to-calibrated path-length scale.</summary>
    public double PathLengthScale;
    /// <summary>Whether the single source subpath is closed.</summary>
    public bool Closed;
    /// <summary>The resolved calibrated start offset.</summary>
    public double ResolvedStartOffset;
    /// <summary>The method stretch scale.</summary>
    public double MethodScale;
    /// <summary>The spacing auto adjustment per addressable gap.</summary>
    public double SpacingAdjustment;
    /// <summary>The number of glyphs whose midpoint exceeded an open endpoint.</summary>
    public int OverflowedGlyphs;
    /// <summary>The number of overflow glyphs suppressed by hidden overflow.</summary>
    public int HiddenGlyphs;

    internal SvgTextPathResult(SvgTextPathSourceKind sourceKind)
    {
        Success = false;
        Error = SvgTextPathError.None;
        Message = "";
        SourceKind = sourceKind;
        Path = null;
        Glyphs = new System.Collections.Generic.List<SvgTextPathGlyph>();
        Text = "";
        WritingMode = SvgWritingMode.HorizontalTopToBottom;
        Direction = SvgTextDirection.LeftToRight;
        FontSize = 16;
        Ascent = 12.8;
        Descent = 3.2;
        XHeight = 8;
        ActualPathLength = 0;
        CalibratedPathLength = 0;
        PathLengthScale = 1;
        Closed = false;
        ResolvedStartOffset = 0;
        MethodScale = 1;
        SpacingAdjustment = 0;
        OverflowedGlyphs = 0;
        HiddenGlyphs = 0;
    }
}
