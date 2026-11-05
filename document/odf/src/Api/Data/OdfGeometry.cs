namespace odf;

/// <summary>
/// Preserves the common lexical geometry of two-dimensional drawing shapes.
/// </summary>
public class OdfGeometry
{
    /// <summary>The first line endpoint horizontal coordinate.</summary>
    public string X1 = "";
    /// <summary>The first line endpoint vertical coordinate.</summary>
    public string Y1 = "";
    /// <summary>The second line endpoint horizontal coordinate.</summary>
    public string X2 = "";
    /// <summary>The second line endpoint vertical coordinate.</summary>
    public string Y2 = "";
    /// <summary>The center horizontal coordinate.</summary>
    public string CenterX = "";
    /// <summary>The center vertical coordinate.</summary>
    public string CenterY = "";
    /// <summary>The circle radius.</summary>
    public string Radius = "";
    /// <summary>The horizontal corner or ellipse radius.</summary>
    public string RadiusX = "";
    /// <summary>The vertical corner or ellipse radius.</summary>
    public string RadiusY = "";
    /// <summary>The legacy rectangle or caption corner radius.</summary>
    public string CornerRadius = "";
    /// <summary>The ellipse section kind.</summary>
    public string EllipseKind = "";
    /// <summary>The ellipse start angle.</summary>
    public string StartAngle = "";
    /// <summary>The ellipse end angle.</summary>
    public string EndAngle = "";
    /// <summary>The preserved polygon or polyline point list.</summary>
    public string Points = "";
    /// <summary>The preserved SVG path data.</summary>
    public string PathData = "";
    /// <summary>The parsed SVG path data.</summary>
    public OdfSvgPathData Path = new OdfSvgPathData();
    /// <summary>The preserved SVG view box.</summary>
    public string ViewBox = "";
    /// <summary>The parsed SVG view box.</summary>
    public OdfSvgViewBox ViewBoxValue = new OdfSvgViewBox();
    /// <summary>The parsed polygon or polyline point list.</summary>
    public OdfSvgPointList PointList = new OdfSvgPointList();
    /// <summary>The caption callout horizontal coordinate.</summary>
    public string CaptionPointX = "";
    /// <summary>The caption callout vertical coordinate.</summary>
    public string CaptionPointY = "";
    /// <summary>The connector routing type.</summary>
    public string ConnectorType = "";
    /// <summary>The connector starting glue-point identifier.</summary>
    public string StartGluePoint = "";
    /// <summary>The connector ending glue-point identifier.</summary>
    public string EndGluePoint = "";
    /// <summary>The connector line-skew lengths.</summary>
    public string LineSkew = "";
    /// <summary>The regular-polygon corner count.</summary>
    public string Corners = "";
    /// <summary>The preserved regular-polygon concavity value.</summary>
    public string ConcaveValue = "";
    /// <summary>Whether the regular polygon is concave.</summary>
    public bool IsConcave;
    /// <summary>Whether the concavity value is valid when specified.</summary>
    public bool IsConcaveValid = true;
    /// <summary>The regular-polygon sharpness.</summary>
    public string Sharpness = "";
    /// <summary>The enhanced geometry of a custom shape.</summary>
    public OdfEnhancedGeometry? EnhancedGeometry;
    /// <summary>The explicit glue points declared by the shape.</summary>
    public OdfGluePoint[] GluePoints = [];
    /// <summary>The connector routing description.</summary>
    public OdfConnectorRouting Routing = new OdfConnectorRouting();
    /// <summary>The element carrying the geometry.</summary>
    public OdfNode? Node;
    internal string OriginalX1 = "";
    internal string OriginalY1 = "";
    internal string OriginalX2 = "";
    internal string OriginalY2 = "";
    internal string OriginalCenterX = "";
    internal string OriginalCenterY = "";
    internal string OriginalRadius = "";
    internal string OriginalRadiusX = "";
    internal string OriginalRadiusY = "";
    internal string OriginalCornerRadius = "";
    internal string OriginalEllipseKind = "";
    internal string OriginalStartAngle = "";
    internal string OriginalEndAngle = "";
    internal string OriginalPoints = "";
    internal string OriginalPathData = "";
    internal string OriginalViewBox = "";
    internal string OriginalCaptionPointX = "";
    internal string OriginalCaptionPointY = "";
    internal string OriginalConnectorType = "";
    internal string OriginalStartGluePoint = "";
    internal string OriginalEndGluePoint = "";
    internal string OriginalLineSkew = "";
    internal string OriginalCorners = "";
    internal string OriginalConcaveValue = "";
    internal bool OriginalIsConcave;
    internal string OriginalSharpness = "";
    internal OdfNode[] OriginalGluePointNodes = [];
}
