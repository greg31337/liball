namespace odf;

/// <summary>
/// Describes the kind of a tracked change.
/// </summary>
public class OdfChange
{
    /// <summary>
    /// The identifier referenced by change boundary elements.
    /// </summary>
    public string Id = "";
    /// <summary>
    /// The XML identifier declared on the changed region.
    /// </summary>
    public string XmlId = "";
    /// <summary>
    /// The tracked-change kind.
    /// </summary>
    public OdfChangeKind Kind;
    /// <summary>
    /// The qualified operation name, including an unknown preserved operation.
    /// </summary>
    public OdfQualifiedName? OperationName;
    /// <summary>
    /// The author recorded by <c>office:change-info</c>.
    /// </summary>
    public string Creator = "";
    /// <summary>
    /// The date or date-time recorded by <c>office:change-info</c>.
    /// </summary>
    public string Date = "";
    /// <summary>
    /// Whether <see cref="Date"/> has a valid XML Schema lexical form.
    /// </summary>
    public bool IsDateValid;
    /// <summary>
    /// The plain-text projection of the change comment paragraphs.
    /// </summary>
    public string Comment = "";
    /// <summary>
    /// The plain-text projection stored by a deletion operation.
    /// </summary>
    public string DeletedText = "";
    /// <summary>
    /// The deleted paragraphs and headings stored by a deletion operation.
    /// </summary>
    public OdfTextBlock[] DeletedTextBlocks = [];
    /// <summary>
    /// The plain text between resolved insertion or format-change boundaries.
    /// </summary>
    public string ChangedText = "";
    /// <summary>
    /// Whether the change is referenced by a point boundary.
    /// </summary>
    public bool IsPoint;
    /// <summary>
    /// Whether the change is referenced by start or end boundaries.
    /// </summary>
    public bool IsRange;
    /// <summary>
    /// Whether exactly one ordered point or range boundary was resolved.
    /// </summary>
    public bool IsBoundaryResolved;
    /// <summary>
    /// Whether the unique end boundary precedes the unique start boundary.
    /// </summary>
    public bool IsReversed;
    /// <summary>
    /// Whether duplicate or mixed boundary forms make the reference ambiguous.
    /// </summary>
    public bool HasDuplicateBoundary;
    /// <summary>
    /// The XML part containing the change definition.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The underlying <c>text:changed-region</c> element.
    /// </summary>
    public OdfNode? Node;
    /// <summary>
    /// The insertion, deletion, format-change, or preserved operation element.
    /// </summary>
    public OdfNode? OperationNode;
    /// <summary>
    /// The underlying <c>office:change-info</c> element.
    /// </summary>
    public OdfNode? ChangeInfoNode;
    /// <summary>
    /// The resolved <c>text:change</c> point element.
    /// </summary>
    public OdfNode? PointNode;
    /// <summary>
    /// The resolved <c>text:change-start</c> element.
    /// </summary>
    public OdfNode? StartNode;
    /// <summary>
    /// The resolved <c>text:change-end</c> element.
    /// </summary>
    public OdfNode? EndNode;
    /// <summary>
    /// The text block containing the point or start boundary.
    /// </summary>
    public OdfTextBlock? StartBlock;
    /// <summary>
    /// The text block containing the range end boundary.
    /// </summary>
    public OdfTextBlock? EndBlock;
    internal string OriginalId = "";
    internal string OriginalXmlId = "";
    internal string OriginalCreator = "";
    internal string OriginalDate = "";
    internal string OriginalComment = "";
    internal string OriginalDeletedText = "";
}
