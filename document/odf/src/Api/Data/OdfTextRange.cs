namespace odf;

/// <summary>
/// Describes a point marker or a resolved or unresolved text range.
/// </summary>
public class OdfTextRange
{
    /// <summary>
    /// The marker family.
    /// </summary>
    public OdfTextRangeKind Kind;
    /// <summary>
    /// The marker name, range identifier, or bibliography identifier.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The XML ID on the point or start marker, when present.
    /// </summary>
    public string XmlId = "";
    /// <summary>
    /// Whether the marker uses separate start and end nodes.
    /// </summary>
    public bool IsRange;
    /// <summary>
    /// Whether a unique, ordered endpoint pair was resolved.
    /// </summary>
    public bool IsResolved;
    /// <summary>
    /// Whether the only matching end precedes the start.
    /// </summary>
    public bool IsReversed;
    /// <summary>
    /// Whether duplicate start or end endpoints make the range ambiguous.
    /// </summary>
    public bool HasDuplicateEndpoint;
    /// <summary>
    /// The XML part containing the marker or range start.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The point or range-start node, when present.
    /// </summary>
    public OdfNode? StartNode;
    /// <summary>
    /// The range-end node, or the point node for a point marker.
    /// </summary>
    public OdfNode? EndNode;
    /// <summary>
    /// The structured run for the point or range start, when present.
    /// </summary>
    public OdfTextRun? StartRun;
    /// <summary>
    /// The structured run for the range end, or the point run.
    /// </summary>
    public OdfTextRun? EndRun;
    /// <summary>
    /// The block containing the point or range start, when present.
    /// </summary>
    public OdfTextBlock? StartBlock;
    /// <summary>
    /// The block containing the range end, or the point block.
    /// </summary>
    public OdfTextBlock? EndBlock;
}
