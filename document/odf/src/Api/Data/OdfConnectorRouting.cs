namespace odf;

/// <summary>
/// Describes the endpoints and preserved route of a connector shape.
/// </summary>
public class OdfConnectorRouting
{
    /// <summary>The connector route type.</summary>
    public string Type = "";
    /// <summary>The starting shape identifier.</summary>
    public string StartShapeId = "";
    /// <summary>The ending shape identifier.</summary>
    public string EndShapeId = "";
    /// <summary>The starting glue-point identifier.</summary>
    public string StartGluePoint = "";
    /// <summary>The ending glue-point identifier.</summary>
    public string EndGluePoint = "";
    /// <summary>The line-skew lengths.</summary>
    public string LineSkew = "";
    /// <summary>The optional explicit connector path.</summary>
    public string PathData = "";
    /// <summary>The resolved starting shape.</summary>
    public OdfDrawingObject? StartShape;
    /// <summary>The resolved ending shape.</summary>
    public OdfDrawingObject? EndShape;
    /// <summary>Whether the starting shape reference is broken.</summary>
    public bool HasBrokenStartShape;
    /// <summary>Whether the ending shape reference is broken.</summary>
    public bool HasBrokenEndShape;
    internal string OriginalType = "";
    internal string OriginalStartShapeId = "";
    internal string OriginalEndShapeId = "";
    internal string OriginalStartGluePoint = "";
    internal string OriginalEndGluePoint = "";
    internal string OriginalLineSkew = "";
    internal string OriginalPathData = "";
}
