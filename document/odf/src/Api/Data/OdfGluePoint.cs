namespace odf;

/// <summary>
/// Describes an explicit connection point on a drawing shape.
/// </summary>
public class OdfGluePoint
{
    /// <summary>The preserved glue-point identifier.</summary>
    public string IdValue = "";
    /// <summary>The parsed non-negative glue-point identifier.</summary>
    public long Id;
    /// <summary>Whether the identifier is valid.</summary>
    public bool IsIdValid;
    /// <summary>The horizontal glue-point coordinate.</summary>
    public string X = "";
    /// <summary>The vertical glue-point coordinate.</summary>
    public string Y = "";
    /// <summary>The optional alignment hint.</summary>
    public string Align = "";
    /// <summary>The connector escape direction.</summary>
    public string EscapeDirection = "";
    /// <summary>The underlying <c>draw:glue-point</c> element.</summary>
    public OdfNode? Node;
    internal string OriginalIdValue = "";
    internal long OriginalId;
    internal string OriginalX = "";
    internal string OriginalY = "";
    internal string OriginalAlign = "";
    internal string OriginalEscapeDirection = "";
}
