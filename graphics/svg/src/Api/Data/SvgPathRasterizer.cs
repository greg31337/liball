namespace svg;

/// <summary>Configures transformed fill and stroke painting into one pixel surface.</summary>
public class SvgPathRasterOptions
{
    /// <summary>The optional premultiplied fill color; <c>null</c> disables fill painting.</summary>
    public SvgPremultipliedRgba8? FillColor;
    /// <summary>The optional premultiplied stroke color; <c>null</c> disables stroke painting.</summary>
    public SvgPremultipliedRgba8? StrokeColor;
    /// <summary>The nonzero or even-odd fill rule.</summary>
    public SvgFillRule FillRule;
    /// <summary>The fill opacity from zero through one.</summary>
    public double FillOpacity;
    /// <summary>The stroke opacity from zero through one.</summary>
    public double StrokeOpacity;
    /// <summary>The complete fill, stroke, and marker order; markers are retained but not painted by this path operation.</summary>
    public System.Collections.Generic.List<SvgPaintOrderComponent> PaintOrder;
    /// <summary>The computed stroke geometry and security limits.</summary>
    public SvgStrokeStyle StrokeStyle;
    /// <summary>Whether stroke metrics scale with the geometry transform.</summary>
    public SvgStrokeVectorEffect VectorEffect;
    /// <summary>The coverage and curve-precision hint.</summary>
    public SvgShapeRendering ShapeRendering;
    /// <summary>The base transform, clip, sampling, and scan-conversion limits.</summary>
    public SvgRasterOptions RasterOptions;

    /// <summary>Creates a nonzero fill-before-stroke operation with balanced sampling defaults.</summary>
    public SvgPathRasterOptions()
    {
        FillColor = null;
        StrokeColor = null;
        FillRule = SvgFillRule.Nonzero;
        FillOpacity = 1;
        StrokeOpacity = 1;
        PaintOrder = new System.Collections.Generic.List<SvgPaintOrderComponent>();
        PaintOrder.Add(SvgPaintOrderComponent.Fill);
        PaintOrder.Add(SvgPaintOrderComponent.Stroke);
        PaintOrder.Add(SvgPaintOrderComponent.Markers);
        StrokeStyle = new SvgStrokeStyle();
        VectorEffect = SvgStrokeVectorEffect.None;
        ShapeRendering = SvgShapeRendering.Auto;
        RasterOptions = new SvgRasterOptions();
    }
}

/// <summary>Contains ordered fill and stroke scan-conversion results.</summary>
public class SvgPathRasterResult
{
    /// <summary>Whether every requested paint operation succeeded.</summary>
    public bool Success;
    /// <summary>The stable combined outcome.</summary>
    public SvgPathRasterError Error;
    /// <summary>The underlying stroke-outline failure, or no failure.</summary>
    public SvgStrokeError StrokeError;
    /// <summary>The fill scan-conversion result, or <c>null</c> when fill was disabled.</summary>
    public SvgRasterResult? FillResult;
    /// <summary>The stroke scan-conversion result, or <c>null</c> when stroke was disabled.</summary>
    public SvgRasterResult? StrokeResult;
    /// <summary>The number of successful fill or stroke operations.</summary>
    public int OperationsCompleted;
    /// <summary>The sum of edge-sample tests across successful operations.</summary>
    public long WorkUnits;
    /// <summary>The sum of nonzero-coverage pixel writes across successful operations.</summary>
    public long PixelsWritten;

    internal SvgPathRasterResult()
    {
        Success = false;
        Error = SvgPathRasterError.None;
        StrokeError = SvgStrokeError.None;
        FillResult = null;
        StrokeResult = null;
        OperationsCompleted = 0;
        WorkUnits = 0;
        PixelsWritten = 0;
    }
}
