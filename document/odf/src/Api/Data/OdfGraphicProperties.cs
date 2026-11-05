namespace odf;

/// <summary>
/// Provides grouped and complete qualified access to effective graphic properties.
/// </summary>
public class OdfGraphicProperties
{
    /// <summary>The style directly referenced by the drawing object.</summary>
    public OdfStyle? Style;
    /// <summary>All effective graphic-property attributes by qualified name.</summary>
    public OdfStyleProperty[] Properties = [];
    /// <summary>Fill and opacity properties.</summary>
    public OdfGraphicFill Fill = new OdfGraphicFill();
    /// <summary>Stroke properties.</summary>
    public OdfGraphicStroke Stroke = new OdfGraphicStroke();
    /// <summary>Line-marker properties.</summary>
    public OdfGraphicMarkers Markers = new OdfGraphicMarkers();
    /// <summary>Shadow properties.</summary>
    public OdfGraphicShadow Shadow = new OdfGraphicShadow();
    /// <summary>Text-wrapping properties.</summary>
    public OdfGraphicWrap Wrap = new OdfGraphicWrap();
    /// <summary>Position, alignment, mirroring, and clipping properties.</summary>
    public OdfGraphicPosition Position = new OdfGraphicPosition();
    /// <summary>Image color-adjustment properties and the direct image filter.</summary>
    public OdfGraphicImageAdjustment Image =
        new OdfGraphicImageAdjustment();
    internal OdfStyleProperty[] OriginalProperties = [];
    internal string OriginalFilterName = "";
}
