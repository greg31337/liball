namespace svg;

/// <summary>Configures bounded shape geometry and CSS geometry-property resolution.</summary>
public class SvgShapeResolveOptions
{
    /// <summary>The typed accessor used for length and path-length attributes.</summary>
    public SvgTypedAttributeAccessor Attributes;
    /// <summary>The optional computed style whose SVG 2 geometry properties take precedence.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The viewport and font context used to resolve lengths and percentages.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>The point and path-segment limits.</summary>
    public SvgGeometryLimits GeometryLimits;
    /// <summary>The maximum character count of one numeric token.</summary>
    public int MaxTokenCharacters;
    /// <summary>The maximum total character count of a CSS <c>path()</c> value.</summary>
    public long MaxCssPathCharacters;
    /// <summary>The maximum token count of a CSS <c>path()</c> value.</summary>
    public int MaxCssPathTokens;
    /// <summary>Whether the containing rendered subtree remains active.</summary>
    public bool AncestorRendered;

    /// <summary>Creates secure deterministic shape-resolution defaults.</summary>
    public SvgShapeResolveOptions()
    {
        Attributes = new SvgTypedAttributeAccessor();
        ComputedStyle = null;
        LengthContext = new SvgLengthContext();
        GeometryLimits = new SvgGeometryLimits();
        MaxTokenCharacters = 4096;
        MaxCssPathCharacters = 16777216;
        MaxCssPathTokens = 64;
        AncestorRendered = true;
    }
}

/// <summary>Contains resolved user-space geometry for one SVG shape element.</summary>
public class SvgShapeResult
{
    /// <summary>The source shape element.</summary>
    public SvgNode? Node;
    /// <summary>The element version used during resolution.</summary>
    public long NodeVersion;
    /// <summary>The supported shape kind.</summary>
    public SvgShapeKind Kind;
    /// <summary>Whether SVG 2 computed geometry properties supplied applicable values.</summary>
    public bool UsesComputedGeometry;
    /// <summary>Whether the element and all resolved ancestors have non-none display.</summary>
    public bool Displayed;
    /// <summary>Whether computed visibility permits painting.</summary>
    public bool Visible;
    /// <summary>Whether valid nonempty geometry was recovered.</summary>
    public bool HasGeometry;
    /// <summary>Whether geometry, display, and visibility permit a rendered contribution.</summary>
    public bool Renderable;
    /// <summary>Whether the shape has an implicitly closed outline.</summary>
    public bool Closed;
    /// <summary>The resolved horizontal position for rectangles.</summary>
    public double X;
    /// <summary>The resolved vertical position for rectangles.</summary>
    public double Y;
    /// <summary>The resolved rectangle width.</summary>
    public double Width;
    /// <summary>The resolved rectangle height.</summary>
    public double Height;
    /// <summary>The resolved horizontal center for circles and ellipses.</summary>
    public double Cx;
    /// <summary>The resolved vertical center for circles and ellipses.</summary>
    public double Cy;
    /// <summary>The resolved circle radius.</summary>
    public double R;
    /// <summary>The used horizontal ellipse or corner radius.</summary>
    public double Rx;
    /// <summary>The used vertical ellipse or corner radius.</summary>
    public double Ry;
    /// <summary>The resolved line starting x coordinate.</summary>
    public double X1;
    /// <summary>The resolved line starting y coordinate.</summary>
    public double Y1;
    /// <summary>The resolved line ending x coordinate.</summary>
    public double X2;
    /// <summary>The resolved line ending y coordinate.</summary>
    public double Y2;
    /// <summary>The recovered alternating x and y point coordinates.</summary>
    public double[] Points;
    /// <summary>The complete recovered point count.</summary>
    public int PointCount;
    /// <summary>The recovered path data, or <c>null</c> when CSS specifies <c>none</c>.</summary>
    public SvgPathData? PathData;
    /// <summary>Whether an authored path-length calibration value is present.</summary>
    public bool HasPathLength;
    /// <summary>The nonnegative authored path-length calibration, or <c>null</c>.</summary>
    public SvgPathLength? PathLength;
    /// <summary>Located diagnostics in discovery order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether resolution completed without error diagnostics.</summary>
    public bool Success;

    internal SvgShapeResult(SvgNode? node)
    {
        Node = node;
        NodeVersion = node == null ? 0 : node.Version;
        Kind = SvgShapeKind.None;
        UsesComputedGeometry = false;
        Displayed = false;
        Visible = false;
        HasGeometry = false;
        Renderable = false;
        Closed = false;
        X = 0;
        Y = 0;
        Width = 0;
        Height = 0;
        Cx = 0;
        Cy = 0;
        R = 0;
        Rx = 0;
        Ry = 0;
        X1 = 0;
        Y1 = 0;
        X2 = 0;
        Y2 = 0;
        Points = new double[0];
        PointCount = 0;
        PathData = null;
        HasPathLength = false;
        PathLength = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Converts this resolved basic shape to absolute path commands.</summary>
    /// <returns>The generated path or a stable conversion failure.</returns>
    internal SvgShapePathResult ToPath()
    {
        return new SvgShapePathConverter().ConvertWithShape(this);
    }

    /// <summary>Converts this resolved basic shape within an explicit command limit.</summary>
    /// <param name="maximumCommands">The positive maximum generated command count.</param>
    /// <returns>The generated path or a stable conversion failure.</returns>
    internal SvgShapePathResult ToPathWithLimit(int maximumCommands)
    {
        return new SvgShapePathConverter().ConvertWithShapeAndMaximumCommands(this, maximumCommands);
    }

    /// <summary>Computes combined fill, stroke, marker, world, and effect-region bounds.</summary>
    /// <param name="options">The component, transform, and effect options.</param>
    /// <returns>The component and final world-space bounds.</returns>
    internal SvgDrawingBoundsResult GetDrawingBounds(SvgDrawingBoundsOptions options)
    {
        return new SvgDrawingBounds().Shape(this, options);
    }

    /// <summary>Computes local fill-only bounds for this resolved basic shape.</summary>
    /// <returns>The explicitly empty or finite local bounds.</returns>
    internal SvgDrawingBoundsResult GetDefaultDrawingBounds()
    {
        return new SvgDrawingBounds().Shape(this, new SvgDrawingBoundsOptions());
    }

    /// <summary>Tests fill, stroke, bounds, clip, and mask geometry at a query point.</summary>
    /// <param name="point">The finite local or transformed query point.</param>
    /// <param name="options">The pointer-event geometry and effect options.</param>
    /// <returns>The component hit decision or stable failure.</returns>
    internal SvgGeometryHitTestResult HitTest(SvgPoint point, SvgGeometryHitTestOptions options)
    {
        return new SvgHitTesting().Shape(this, point, options);
    }
}
