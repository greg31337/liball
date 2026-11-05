namespace svg;

/// <summary>Associates a positioned glyph font identifier with one parsed font and output em size.</summary>
public class SvgTextFontResource
{
    /// <summary>The identifier retained on shaped glyphs.</summary>
    public int Identifier;
    /// <summary>The parsed font that owns the glyph identifiers.</summary>
    public SvgFont Font;
    /// <summary>The positive output em size used to scale design-unit outlines.</summary>
    public double EmSize;

    /// <summary>Creates one font resource association.</summary>
    /// <param name="identifier">The shaped-glyph font identifier.</param>
    /// <param name="font">The successfully parsed font.</param>
    /// <param name="emSize">The positive output em size.</param>
    public SvgTextFontResource(int identifier, SvgFont font, double emSize)
    {
        Identifier = identifier;
        Font = font;
        EmSize = emSize;
    }
}

/// <summary>Configures generated underline, overline, and line-through geometry.</summary>
public class SvgTextDecorationOptions
{
    /// <summary>Whether underline geometry is generated.</summary>
    public bool Underline;
    /// <summary>Whether overline geometry is generated.</summary>
    public bool Overline;
    /// <summary>Whether line-through geometry is generated.</summary>
    public bool LineThrough;
    /// <summary>The nonnegative decoration thickness as a fraction of font size.</summary>
    public double ThicknessFactor;
    /// <summary>The underline baseline offset as a fraction of font size.</summary>
    public double UnderlineOffsetFactor;
    /// <summary>The overline baseline offset as a fraction of font size.</summary>
    public double OverlineOffsetFactor;
    /// <summary>The line-through baseline offset as a fraction of font size.</summary>
    public double LineThroughOffsetFactor;

    /// <summary>Creates disabled decoration defaults with conventional metric fractions.</summary>
    public SvgTextDecorationOptions()
    {
        Underline = false;
        Overline = false;
        LineThrough = false;
        ThicknessFactor = 0.0625;
        UnderlineOffsetFactor = 0.125;
        OverlineOffsetFactor = -0.8;
        LineThroughOffsetFactor = -0.3;
    }
}

/// <summary>Configures bounded glyph conversion, bounds, decorations, and extraction.</summary>
public class SvgTextGeometryOptions
{
    /// <summary>The font resources available to positioned glyphs.</summary>
    public System.Collections.Generic.List<SvgTextFontResource> Fonts;
    /// <summary>The optional generated text decorations.</summary>
    public SvgTextDecorationOptions Decorations;
    /// <summary>The nonnegative full stroke width applied when computing stroke bounds.</summary>
    public double StrokeWidth;
    /// <summary>Whether suppressed open-textPath overflow glyphs retain geometry.</summary>
    public bool IncludeInvisibleGlyphs;
    /// <summary>The maximum accepted positioned glyph count.</summary>
    public int MaximumGlyphs;
    /// <summary>The maximum aggregate glyph and decoration path command count.</summary>
    public int MaximumPathCommands;
    /// <summary>The bounded TrueType outline decoder options.</summary>
    public SvgTrueTypeGlyphOptions TrueTypeOptions;
    /// <summary>The bounded CFF outline decoder options.</summary>
    public SvgCffOutlineOptions CffOptions;
    /// <summary>The cancellation token observed during synchronous processing.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates bounded fill-only text geometry defaults.</summary>
    public SvgTextGeometryOptions()
    {
        Fonts = new System.Collections.Generic.List<SvgTextFontResource>();
        Decorations = new SvgTextDecorationOptions();
        StrokeWidth = 0;
        IncludeInvisibleGlyphs = false;
        MaximumGlyphs = 1000000;
        MaximumPathCommands = 2000000;
        TrueTypeOptions = new SvgTrueTypeGlyphOptions();
        CffOptions = new SvgCffOutlineOptions();
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Configures bounded text glyph or cluster hit testing.</summary>
public class SvgTextHitTestOptions
{
    /// <summary>The glyph-ink, glyph-logical, or cluster-logical query mode.</summary>
    public SvgTextHitTestMode Mode;
    /// <summary>The positive glyph-outline flattening tolerance.</summary>
    public double Tolerance;
    /// <summary>The nonnegative maximum adaptive subdivision depth.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The positive maximum flattened edge count per glyph query.</summary>
    public int MaximumSegments;

    /// <summary>Creates bounded glyph-ink hit-testing defaults.</summary>
    public SvgTextHitTestOptions()
    {
        Mode = SvgTextHitTestMode.GlyphInk;
        Tolerance = 0.01;
        MaximumSubdivisionDepth = 32;
        MaximumSegments = 2000000;
    }
}

/// <summary>Contains one converted positioned glyph and its bounds.</summary>
public class SvgTextGlyphGeometry
{
    /// <summary>The zero-based positioned glyph index.</summary>
    public int Index;
    /// <summary>The source addressable-character index.</summary>
    public int CharacterIndex;
    /// <summary>The source UTF-16 start index.</summary>
    public int Utf16Start;
    /// <summary>The source UTF-16 code-unit length.</summary>
    public int Utf16Length;
    /// <summary>The source text represented by this glyph cluster.</summary>
    public string Text;
    /// <summary>The independently transformed glyph outline.</summary>
    public SvgPathData? Path;
    /// <summary>The transformed logical character cell.</summary>
    public SvgRect? LogicalBounds;
    /// <summary>The transformed filled outline bounds.</summary>
    public SvgRect? InkBounds;
    /// <summary>The transformed stroked outline bounds.</summary>
    public SvgRect? StrokeBounds;
    /// <summary>Whether this glyph participates in rendering and hit testing.</summary>
    public bool Visible;
    /// <summary>The zero-based cluster geometry index.</summary>
    public int ClusterIndex;

    internal SvgTextGlyphGeometry()
    {
        Index = -1;
        CharacterIndex = -1;
        Utf16Start = 0;
        Utf16Length = 0;
        Text = "";
        Path = null;
        LogicalBounds = null;
        InkBounds = null;
        StrokeBounds = null;
        Visible = false;
        ClusterIndex = -1;
    }
}

/// <summary>Contains source-cluster text, glyph membership, bounds, and reading order.</summary>
public class SvgTextClusterGeometry
{
    /// <summary>The zero-based cluster index in positioned glyph order.</summary>
    public int Index;
    /// <summary>The source UTF-16 start index.</summary>
    public int Utf16Start;
    /// <summary>The source UTF-16 code-unit length.</summary>
    public int Utf16Length;
    /// <summary>The exact normalized source substring.</summary>
    public string Text;
    /// <summary>The positioned glyph indices belonging to this cluster.</summary>
    public System.Collections.Generic.List<int> GlyphIndices;
    /// <summary>The union of member logical bounds.</summary>
    public SvgRect? LogicalBounds;
    /// <summary>The union of member filled outline bounds.</summary>
    public SvgRect? InkBounds;
    /// <summary>The zero-based accessible logical reading-order position.</summary>
    public int ReadingOrder;

    internal SvgTextClusterGeometry()
    {
        Index = -1;
        Utf16Start = 0;
        Utf16Length = 0;
        Text = "";
        GlyphIndices = new System.Collections.Generic.List<int>();
        LogicalBounds = null;
        InkBounds = null;
        ReadingOrder = -1;
    }
}

/// <summary>Contains one generated decoration segment and its bounds.</summary>
public class SvgTextDecorationGeometry
{
    /// <summary>The decoration line kind.</summary>
    public SvgTextDecorationKind Kind;
    /// <summary>The positioned glyph whose advance generated this segment.</summary>
    public int GlyphIndex;
    /// <summary>The transformed filled rectangular segment.</summary>
    public SvgPathData? Path;
    /// <summary>The transformed fill bounds.</summary>
    public SvgRect? Bounds;
    /// <summary>The optional stroked segment bounds.</summary>
    public SvgRect? StrokeBounds;

    internal SvgTextDecorationGeometry()
    {
        Kind = SvgTextDecorationKind.Underline;
        GlyphIndex = -1;
        Path = null;
        Bounds = null;
        StrokeBounds = null;
    }
}

/// <summary>Contains one topmost glyph or cluster hit-test result.</summary>
public class SvgTextHitTestResult
{
    /// <summary>Whether hit testing completed successfully.</summary>
    public bool Success;
    /// <summary>The stable hit-testing outcome.</summary>
    public SvgTextGeometryError Error;
    /// <summary>Whether the requested geometry contains the point.</summary>
    public bool Hit;
    /// <summary>The zero-based glyph index, or minus one.</summary>
    public int GlyphIndex;
    /// <summary>The zero-based cluster index, or minus one.</summary>
    public int ClusterIndex;
    /// <summary>The source addressable-character index, or minus one.</summary>
    public int CharacterIndex;
    /// <summary>The query point.</summary>
    public SvgPoint Point;

    internal SvgTextHitTestResult(SvgPoint point)
    {
        Success = false;
        Error = SvgTextGeometryError.None;
        Hit = false;
        GlyphIndex = -1;
        ClusterIndex = -1;
        CharacterIndex = -1;
        Point = point;
    }
}

/// <summary>Contains converted text paths, component bounds, extraction, and reading order.</summary>
public class SvgTextGeometryResult
{
    /// <summary>Whether all requested text geometry completed successfully.</summary>
    public bool Success;
    /// <summary>The stable text geometry outcome.</summary>
    public SvgTextGeometryError Error;
    /// <summary>A deterministic failure explanation, or empty string.</summary>
    public string Message;
    /// <summary>The converted positioned glyph geometry.</summary>
    public System.Collections.Generic.List<SvgTextGlyphGeometry> Glyphs;
    /// <summary>The grouped source-cluster geometry.</summary>
    public System.Collections.Generic.List<SvgTextClusterGeometry> Clusters;
    /// <summary>The generated decoration segments.</summary>
    public System.Collections.Generic.List<SvgTextDecorationGeometry> Decorations;
    /// <summary>The cluster indices in accessible logical source order.</summary>
    public System.Collections.Generic.List<int> AccessibleReadingOrder;
    /// <summary>The unique source clusters concatenated in positioned glyph order.</summary>
    public string ExtractedText;
    /// <summary>The unique source clusters concatenated in logical source order.</summary>
    public string AccessibleText;
    /// <summary>The union of glyph logical cells.</summary>
    public SvgRect LogicalBounds;
    /// <summary>The union of glyph and decoration fill bounds.</summary>
    public SvgRect InkBounds;
    /// <summary>The union of optional glyph and decoration stroke bounds.</summary>
    public SvgRect StrokeBounds;
    /// <summary>All glyph and decoration paths combined without Boolean merging.</summary>
    public SvgPathData? Path;

    internal SvgTextGeometryResult()
    {
        Success = false;
        Error = SvgTextGeometryError.None;
        Message = "";
        Glyphs = new System.Collections.Generic.List<SvgTextGlyphGeometry>();
        Clusters = new System.Collections.Generic.List<SvgTextClusterGeometry>();
        Decorations = new System.Collections.Generic.List<SvgTextDecorationGeometry>();
        AccessibleReadingOrder = new System.Collections.Generic.List<int>();
        ExtractedText = "";
        AccessibleText = "";
        LogicalBounds = new SvgTextGeometry().EmptyBounds();
        InkBounds = new SvgTextGeometry().EmptyBounds();
        StrokeBounds = new SvgTextGeometry().EmptyBounds();
        Path = null;
    }

    /// <summary>Hit-tests converted glyph ink using bounded defaults.</summary>
    /// <param name="point">The finite query point.</param>
    /// <returns>The topmost glyph result or a stable failure.</returns>
    internal SvgTextHitTestResult HitTest(SvgPoint point)
    {
        return new SvgTextGeometry().HitTest(this, point, new SvgTextHitTestOptions());
    }

    /// <summary>Hit-tests converted glyph or cluster geometry with explicit bounds.</summary>
    /// <param name="point">The finite query point.</param>
    /// <param name="options">The hit-test mode and flattening limits.</param>
    /// <returns>The topmost glyph or cluster result or a stable failure.</returns>
    internal SvgTextHitTestResult HitTestWithOptions(SvgPoint point, SvgTextHitTestOptions options)
    {
        return new SvgTextGeometry().HitTest(this, point, options);
    }
}
