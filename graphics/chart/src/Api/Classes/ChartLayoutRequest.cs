namespace chart;
/// <summary>
/// Represents a layout request used by the chart model or rendering pipeline.
/// </summary>
public class ChartLayoutRequest : IChartLayoutRequest
{
    internal ChartLayoutItemKind Kind;
    internal string Id;
    internal ChartSide Side;
    internal double Width;
    internal double Height;
    internal double Spacing;
    internal bool Overlay;
    /// <summary>
    /// Initializes a layout request from the supplied kind, id, side, width, height, spacing, and overlay.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <param name = "overlay">The overlay.</param>
    public ChartLayoutRequest(ChartLayoutItemKind kind, string id, ChartSide side, double width, double height, double spacing, bool overlay)
    {
        ValidateKind(kind);
        ChartPlot.ValidateId(id);
        ValidateLength(width, "width");
        ValidateLength(height, "height");
        ValidateLength(spacing, "spacing");
        Kind = kind;
        Id = id;
        Side = side;
        Width = width;
        Height = height;
        Spacing = spacing;
        Overlay = overlay;
    }

    internal static ChartLayoutRequest CreateLegend(string id, ChartSide side, double width, double height, double spacing)
    {
        return new ChartLayoutRequest(ChartLayoutItemKind.Legend, id, side, width, height, spacing, false);
    }

    internal static ChartLayoutRequest CreateColorbar(string id, ChartSide side, double width, double height, double spacing)
    {
        return new ChartLayoutRequest(ChartLayoutItemKind.Colorbar, id, side, width, height, spacing, false);
    }

    internal static ChartLayoutRequest CreateOverlayLegend(string id, ChartSide side, double width, double height, double spacing)
    {
        return new ChartLayoutRequest(ChartLayoutItemKind.Legend, id, side, width, height, spacing, true);
    }

    internal static ChartLayoutRequest CreateOverlayColorbar(string id, ChartSide side, double width, double height, double spacing)
    {
        return new ChartLayoutRequest(ChartLayoutItemKind.Colorbar, id, side, width, height, spacing, true);
    }

    internal static ChartLayoutRequest CreateAnnotation(string id, ChartSide side, double width, double height, double spacing, bool overlay)
    {
        return new ChartLayoutRequest(ChartLayoutItemKind.Annotation, id, side, width, height, spacing, overlay);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartLayoutRequest Copy()
    {
        return new ChartLayoutRequest(Kind, Id, Side, Width, Height, Spacing, Overlay);
    }

    internal static void ValidateKind(ChartLayoutItemKind kind)
    {
        if (kind != ChartLayoutItemKind.Legend && kind != ChartLayoutItemKind.Colorbar && kind != ChartLayoutItemKind.Annotation)
        {
            throw new System.ArgumentException("Layout requests must be legends, colorbars, or annotations.");
        }
    }

    internal static void ValidateLength(double value, string name)
    {
        ChartLinearScale.ValidateFinite(value, name);
        if (value < 0.0)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }

    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    public ChartLayoutItemKind GetKind()
    {
        return Kind;
    }

    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetKind(ChartLayoutItemKind value)
    {
        Kind = value;
    }

    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    public string GetId()
    {
        return Id;
    }

    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetId(string value)
    {
        Id = value;
    }

    /// <summary>Returns the current side.</summary>
    /// <returns>The current field value.</returns>
    public ChartSide GetSide()
    {
        return Side;
    }

    /// <summary>Replaces the current side.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSide(ChartSide value)
    {
        Side = value;
    }

    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    public double GetWidth()
    {
        return Width;
    }

    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetWidth(double value)
    {
        Width = value;
    }

    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    public double GetHeight()
    {
        return Height;
    }

    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHeight(double value)
    {
        Height = value;
    }

    /// <summary>Returns the current spacing.</summary>
    /// <returns>The current field value.</returns>
    public double GetSpacing()
    {
        return Spacing;
    }

    /// <summary>Replaces the current spacing.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSpacing(double value)
    {
        Spacing = value;
    }

    /// <summary>Returns the current overlay.</summary>
    /// <returns>The current field value.</returns>
    public bool GetOverlay()
    {
        return Overlay;
    }

    /// <summary>Replaces the current overlay.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetOverlay(bool value)
    {
        Overlay = value;
    }
}
