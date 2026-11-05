namespace svg;

/// <summary>Configures transformed stroke containment and bounds.</summary>
public class SvgStrokeGeometryOptions
{
    /// <summary>The required stroke style and security limits.</summary>
    public SvgStrokeStyle Style;
    /// <summary>The optional transform from path user coordinates to query and bounds coordinates.</summary>
    public SvgMatrix? Transform;
    /// <summary>Whether stroke metrics scale with the transform.</summary>
    public SvgStrokeVectorEffect VectorEffect;

    /// <summary>Creates ordinary untransformed stroke-geometry defaults.</summary>
    public SvgStrokeGeometryOptions()
    {
        Style = new SvgStrokeStyle();
        Transform = null;
        VectorEffect = SvgStrokeVectorEffect.None;
    }
}

/// <summary>Contains a non-throwing stroke-containment result.</summary>
public class SvgStrokeContainmentResult
{
    /// <summary>Whether containment completed successfully.</summary>
    public bool Success;
    /// <summary>The stable stroke failure.</summary>
    public SvgStrokeError Error;
    /// <summary>Whether the closed stroked region contains the query point.</summary>
    public bool Contains;

    /// <summary>Creates a stroke-containment result.</summary>
    /// <param name="success">Whether containment succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="contains">Whether the stroke contains the point.</param>
    public SvgStrokeContainmentResult(bool success, SvgStrokeError error, bool contains)
    {
        Success = success;
        Error = error;
        Contains = contains;
    }
}

/// <summary>Contains finite transformed stroke bounds.</summary>
public class SvgStrokeBoundsResult
{
    /// <summary>Whether bounds computation succeeded.</summary>
    public bool Success;
    /// <summary>The stable stroke failure.</summary>
    public SvgStrokeError Error;
    /// <summary>The finite bounds, explicitly empty for an empty stroke.</summary>
    public SvgRect? Bounds;

    /// <summary>Creates a stroke-bounds result.</summary>
    /// <param name="success">Whether bounds computation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="bounds">The finite bounds.</param>
    public SvgStrokeBoundsResult(bool success, SvgStrokeError error, SvgRect? bounds)
    {
        Success = success;
        Error = error;
        Bounds = bounds;
    }
}
