namespace svg;

/// <summary>Contains one decoded TrueType outline or phantom point.</summary>
public class SvgTrueTypeGlyphPoint
{
    /// <summary>The x coordinate in font design units.</summary>
    public double X;
    /// <summary>The y coordinate in font design units.</summary>
    public double Y;
    /// <summary>Whether this point lies on the quadratic outline.</summary>
    public bool OnCurve;
    /// <summary>Whether this point is one of the four metric phantom points.</summary>
    public bool Phantom;

    internal SvgTrueTypeGlyphPoint(double x, double y, bool onCurve, bool phantom)
    {
        X = x;
        Y = y;
        OnCurve = onCurve;
        Phantom = phantom;
    }
}

/// <summary>Contains one closed decoded TrueType contour.</summary>
public class SvgTrueTypeGlyphContour
{
    /// <summary>The first point index in the expanded glyph point list.</summary>
    public int StartPointIndex;
    /// <summary>The last point index in the expanded glyph point list.</summary>
    public int EndPointIndex;
    /// <summary>The ordered on-curve and off-curve contour points.</summary>
    public System.Collections.Generic.List<SvgTrueTypeGlyphPoint> Points;

    internal SvgTrueTypeGlyphContour(int startPointIndex, int endPointIndex)
    {
        StartPointIndex = startPointIndex;
        EndPointIndex = endPointIndex;
        Points = new System.Collections.Generic.List<SvgTrueTypeGlyphPoint>();
    }
}

/// <summary>Describes one decoded component record in a composite glyph.</summary>
public class SvgTrueTypeGlyphComponent
{
    /// <summary>The referenced child glyph index.</summary>
    public int GlyphIndex;
    /// <summary>The original component flags.</summary>
    public int Flags;
    /// <summary>Whether signed XY placement arguments were used.</summary>
    public bool UsesXYOffset;
    /// <summary>The resolved x translation after offset or point alignment.</summary>
    public double OffsetX;
    /// <summary>The resolved y translation after offset or point alignment.</summary>
    public double OffsetY;
    /// <summary>The x-to-x transform coefficient.</summary>
    public double A;
    /// <summary>The x-to-y transform coefficient.</summary>
    public double B;
    /// <summary>The y-to-x transform coefficient.</summary>
    public double C;
    /// <summary>The y-to-y transform coefficient.</summary>
    public double D;
    /// <summary>Whether this component supplies the composite's phantom metrics.</summary>
    public bool UsesMetrics;

    internal SvgTrueTypeGlyphComponent()
    {
        GlyphIndex = 0;
        Flags = 0;
        UsesXYOffset = false;
        OffsetX = 0;
        OffsetY = 0;
        A = 1;
        B = 0;
        C = 0;
        D = 1;
        UsesMetrics = false;
    }
}

/// <summary>Configures bounded TrueType glyph expansion.</summary>
public class SvgTrueTypeGlyphOptions
{
    /// <summary>The shared font security limits, including expanded points per glyph.</summary>
    public SvgFontLimits Limits;
    /// <summary>The maximum recursive component depth accepted by the decoder.</summary>
    public int MaximumCompositeDepth;
    /// <summary>The maximum total component records visited for one decoded glyph.</summary>
    public int MaximumComponents;

    /// <summary>Creates bounded defaults for one TrueType glyph decode.</summary>
    public SvgTrueTypeGlyphOptions()
    {
        Limits = new SvgFontLimits();
        MaximumCompositeDepth = 64;
        MaximumComponents = 4096;
    }
}

/// <summary>Contains one expanded unhinted TrueType glyph outline and its SVG path.</summary>
public class SvgTrueTypeGlyphOutline
{
    /// <summary>The decoded glyph index.</summary>
    public int GlyphIndex;
    /// <summary>Whether the source glyph description was composite.</summary>
    public bool Composite;
    /// <summary>Whether the glyph had no outline data or contour points.</summary>
    public bool Empty;
    /// <summary>The source glyph minimum x coordinate.</summary>
    public int XMin;
    /// <summary>The source glyph minimum y coordinate.</summary>
    public int YMin;
    /// <summary>The source glyph maximum x coordinate.</summary>
    public int XMax;
    /// <summary>The source glyph maximum y coordinate.</summary>
    public int YMax;
    /// <summary>The expanded outline points, excluding phantom points.</summary>
    public System.Collections.Generic.List<SvgTrueTypeGlyphPoint> Points;
    /// <summary>The expanded closed contours.</summary>
    public System.Collections.Generic.List<SvgTrueTypeGlyphContour> Contours;
    /// <summary>The four horizontal and vertical metric phantom points.</summary>
    public System.Collections.Generic.List<SvgTrueTypeGlyphPoint> PhantomPoints;
    /// <summary>The component records directly contained by this glyph.</summary>
    public System.Collections.Generic.List<SvgTrueTypeGlyphComponent> Components;
    /// <summary>The number of skipped TrueType instruction bytes on this glyph.</summary>
    public int InstructionLength;
    /// <summary>The glyph whose metrics supplied the phantom points.</summary>
    public int MetricsGlyphIndex;
    /// <summary>Whether parsed vertical metrics supplied the last two phantom points.</summary>
    public bool HasVerticalMetrics;
    /// <summary>The expanded quadratic SVG path in font design coordinates.</summary>
    public SvgPathData? Path;

    internal SvgTrueTypeGlyphOutline(int glyphIndex)
    {
        GlyphIndex = glyphIndex;
        Composite = false;
        Empty = false;
        XMin = 0;
        YMin = 0;
        XMax = 0;
        YMax = 0;
        Points = new System.Collections.Generic.List<SvgTrueTypeGlyphPoint>();
        Contours = new System.Collections.Generic.List<SvgTrueTypeGlyphContour>();
        PhantomPoints = new System.Collections.Generic.List<SvgTrueTypeGlyphPoint>();
        Components = new System.Collections.Generic.List<SvgTrueTypeGlyphComponent>();
        InstructionLength = 0;
        MetricsGlyphIndex = glyphIndex;
        HasVerticalMetrics = false;
        Path = null;
    }
}

/// <summary>Contains a completed or failed bounded TrueType glyph decode.</summary>
public class SvgTrueTypeGlyphResult
{
    /// <summary>Whether glyph expansion and path conversion completed.</summary>
    public bool Success;
    /// <summary>The stable decoding outcome.</summary>
    public SvgTrueTypeGlyphError Error;
    /// <summary>The expanded glyph outline, or <c>null</c> after failure.</summary>
    public SvgTrueTypeGlyphOutline? Outline;

    internal SvgTrueTypeGlyphResult(bool success, SvgTrueTypeGlyphError error, SvgTrueTypeGlyphOutline? outline)
    {
        Success = success;
        Error = error;
        Outline = outline;
    }
}
