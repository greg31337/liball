using System.Xml;

namespace pdf;

/// <summary>
/// Contains validated intrinsic dimensions and view-box metadata loaded from an SVG document.
/// </summary>
public class PdfSvgData
{
    /// <summary>
    /// Stores the intrinsic width in PDF points.
    /// </summary>
    public double Width;
    /// <summary>
    /// Stores the intrinsic height in PDF points.
    /// </summary>
    public double Height;
    /// <summary>
    /// Indicates whether the SVG declares an explicit view box.
    /// </summary>
    public bool HasViewBox;
    /// <summary>
    /// Stores the horizontal view-box origin in SVG user units.
    /// </summary>
    public double ViewBoxX;
    /// <summary>
    /// Stores the vertical view-box origin in SVG user units.
    /// </summary>
    public double ViewBoxY;
    /// <summary>
    /// Stores the view-box width in SVG user units.
    /// </summary>
    public double ViewBoxWidth;
    /// <summary>
    /// Stores the view-box height in SVG user units.
    /// </summary>
    public double ViewBoxHeight;
    internal XmlElement Root;

    internal PdfSvgData(double width, double height, bool hasViewBox, double viewBoxX, double viewBoxY, double viewBoxWidth, double viewBoxHeight, XmlElement root)
    {
        Width = width;
        Height = height;
        HasViewBox = hasViewBox;
        ViewBoxX = viewBoxX;
        ViewBoxY = viewBoxY;
        ViewBoxWidth = viewBoxWidth;
        ViewBoxHeight = viewBoxHeight;
        Root = root;
    }
}
