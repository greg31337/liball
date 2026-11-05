namespace odf;

/// <summary>
/// Describes an OpenDocument list or outline style.
/// </summary>
public class OdfListStyle
{
    /// <summary>
    /// The style name.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The optional display name.
    /// </summary>
    public string DisplayName = "";
    /// <summary>
    /// Whether this is a list or outline style.
    /// </summary>
    public OdfListStyleKind Kind;
    /// <summary>
    /// Whether numbering continues consecutively between list levels.
    /// </summary>
    public string ConsecutiveNumbering = "";
    /// <summary>
    /// The XML part containing the style, or an empty string for a flat document.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// Whether the definition belongs to an automatic-styles section.
    /// </summary>
    public bool IsAutomatic;
    /// <summary>
    /// All qualified style attributes and preserved lexical values.
    /// </summary>
    public OdfStyleProperty[] Attributes = [];
    /// <summary>
    /// The ordered level rules.
    /// </summary>
    public OdfListLevel[] Levels = [];
    /// <summary>
    /// Unmodeled non-element content preserved from the style.
    /// </summary>
    public OdfNode[] PassthroughContent = [];
    /// <summary>
    /// The underlying list-style or outline-style element.
    /// </summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal OdfAttribute[] OriginalAttributes = [];
    internal OdfNode[] OriginalLevelNodes = [];
    internal OdfNode[] OriginalPassthroughContent = [];
    internal string OriginalName = "";
    internal string OriginalDisplayName = "";
    internal OdfListStyleKind OriginalKind;
    internal string OriginalConsecutiveNumbering = "";
    internal string OriginalPartPath = "";
    internal bool OriginalIsAutomatic;
}
