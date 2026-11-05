namespace odf;

/// <summary>
/// Describes an ordered, unordered, image, or outline list in document content.
/// </summary>
public class OdfTextList
{
    /// <summary>
    /// The referenced list-style name.
    /// </summary>
    public string StyleName = "";
    /// <summary>
    /// The preserved continue-numbering value.
    /// </summary>
    public string ContinueNumbering = "";
    /// <summary>
    /// The XML ID of the explicitly continued list.
    /// </summary>
    public string ContinueListId = "";
    /// <summary>
    /// The list XML ID.
    /// </summary>
    public string XmlId = "";
    /// <summary>
    /// The one-based nesting level.
    /// </summary>
    public int Level;
    /// <summary>
    /// The effective list kind at this level.
    /// </summary>
    public OdfTextListKind Kind;
    /// <summary>
    /// The optional list header.
    /// </summary>
    public OdfTextListItem? Header;
    /// <summary>
    /// The ordered list items.
    /// </summary>
    public OdfTextListItem[] Items = [];
    /// <summary>
    /// The containing list item for a nested list.
    /// </summary>
    public OdfTextListItem? ParentItem;
    /// <summary>
    /// The containing list for a nested list.
    /// </summary>
    public OdfTextList? ParentList;
    /// <summary>
    /// The resolved list-style definition.
    /// </summary>
    public OdfListStyle? Style;
    /// <summary>
    /// The effective label rule for this nesting level.
    /// </summary>
    public OdfListLevel? LevelStyle;
    /// <summary>
    /// The resolved preceding list whose numbering is continued.
    /// </summary>
    public OdfTextList? ContinuedList;
    /// <summary>
    /// Whether the requested continuation is missing or ambiguous.
    /// </summary>
    public bool HasBrokenContinuation;
    /// <summary>
    /// Whether this list participates in a continuation cycle.
    /// </summary>
    public bool HasContinuationCycle;
    /// <summary>
    /// The XML part containing the list.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The underlying <c>text:list</c> element.
    /// </summary>
    public OdfNode? Node;
}
