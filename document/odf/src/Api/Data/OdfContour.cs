namespace odf;

/// <summary>
/// Describes a polygon or path used to clip and wrap frame content.
/// </summary>
public class OdfContour
{
    /// <summary>The contour geometry kind.</summary>
    public OdfContourKind Kind;
    /// <summary>The contour width.</summary>
    public string Width = "";
    /// <summary>The contour height.</summary>
    public string Height = "";
    /// <summary>The contour local coordinate system.</summary>
    public OdfSvgViewBox ViewBox = new OdfSvgViewBox();
    /// <summary>The polygon geometry when <see cref="Kind"/> is polygon.</summary>
    public OdfSvgPointList PointList = new OdfSvgPointList();
    /// <summary>The path geometry when <see cref="Kind"/> is path.</summary>
    public OdfSvgPathData Path = new OdfSvgPathData();
    /// <summary>The preserved recreate-on-edit value.</summary>
    public string RecreateOnEditValue = "";
    /// <summary>Whether the contour is recreated when its object is edited.</summary>
    public bool RecreateOnEdit;
    /// <summary>Whether the recreate-on-edit value is valid.</summary>
    public bool IsRecreateOnEditValid = true;
    /// <summary>The underlying contour element.</summary>
    public OdfNode? Node;
    internal string OriginalWidth = "";
    internal string OriginalHeight = "";
    internal string OriginalRecreateOnEditValue = "";
    internal bool OriginalRecreateOnEdit;
}
