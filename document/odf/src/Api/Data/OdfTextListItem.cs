namespace odf;

/// <summary>
/// Describes a document list item or list header.
/// </summary>
public class OdfTextListItem
{
    /// <summary>
    /// Whether this item is a non-numbered list header.
    /// </summary>
    public bool IsHeader;
    /// <summary>
    /// The explicit item start value.
    /// </summary>
    public string StartValue = "";
    /// <summary>
    /// The item-specific list-style override name.
    /// </summary>
    public string StyleOverride = "";
    /// <summary>
    /// The resolved one-based ordinal, or zero for a header or unordered item.
    /// </summary>
    public int Ordinal;
    /// <summary>
    /// The resolved label text or image resource reference.
    /// </summary>
    public string LabelText = "";
    /// <summary>
    /// Paragraphs and headings directly contained by the item.
    /// </summary>
    public OdfTextBlock[] TextBlocks = [];
    /// <summary>
    /// Lists directly nested in this item.
    /// </summary>
    public OdfTextList[] Lists = [];
    /// <summary>
    /// The containing list.
    /// </summary>
    public OdfTextList? List;
    /// <summary>
    /// The effective list-level label configuration.
    /// </summary>
    public OdfListLevel? LevelStyle;
    /// <summary>
    /// The underlying list-item or list-header element.
    /// </summary>
    public OdfNode? Node;
}
