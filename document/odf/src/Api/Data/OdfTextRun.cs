namespace odf;

/// <summary>
/// Describes literal text, an inline container, or a whitespace control.
/// </summary>
public class OdfTextRun
{
    /// <summary>
    /// The structured run kind.
    /// </summary>
    public OdfTextRunKind Kind;
    /// <summary>
    /// The plain text represented by this run and its descendants.
    /// </summary>
    public string Text = "";
    /// <summary>
    /// The referenced text style name.
    /// </summary>
    public string StyleName = "";
    /// <summary>
    /// The preserved whitespace-separated style class names.
    /// </summary>
    public string ClassNames = "";
    /// <summary>
    /// The hyperlink IRI.
    /// </summary>
    public string Href = "";
    /// <summary>
    /// The hyperlink name.
    /// </summary>
    public string LinkName = "";
    /// <summary>
    /// The hyperlink title.
    /// </summary>
    public string LinkTitle = "";
    /// <summary>
    /// The hyperlink target frame name.
    /// </summary>
    public string TargetFrameName = "";
    /// <summary>
    /// The hyperlink show behavior.
    /// </summary>
    public string LinkShow = "";
    /// <summary>
    /// The hyperlink activation behavior.
    /// </summary>
    public string LinkActuate = "";
    /// <summary>
    /// The hyperlink visited-text style name.
    /// </summary>
    public string VisitedStyleName = "";
    /// <summary>
    /// The marker name, range identifier, or bibliography identifier.
    /// </summary>
    public string MarkerName = "";
    /// <summary>
    /// The user-index name.
    /// </summary>
    public string IndexName = "";
    /// <summary>
    /// The explicit index string value.
    /// </summary>
    public string StringValue = "";
    /// <summary>
    /// The first alphabetical-index key.
    /// </summary>
    public string Key1 = "";
    /// <summary>
    /// The second alphabetical-index key.
    /// </summary>
    public string Key2 = "";
    /// <summary>
    /// The preserved index outline level.
    /// </summary>
    public string MarkerOutlineLevel = "";
    /// <summary>
    /// The bibliography entry type.
    /// </summary>
    public string BibliographyType = "";
    /// <summary>
    /// The Ruby annotation text.
    /// </summary>
    public string RubyText = "";
    /// <summary>
    /// The text style applied to the Ruby annotation.
    /// </summary>
    public string RubyTextStyleName = "";
    /// <summary>
    /// All marker or inline attributes for generic access and preservation.
    /// </summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>
    /// The preserved tab-stop reference.
    /// </summary>
    public string TabReference = "";
    /// <summary>
    /// The number of spaces represented by an explicit-space run.
    /// </summary>
    public int SpaceCount = 1;
    /// <summary>
    /// Nested runs for spans, hyperlinks, and preserved inline elements.
    /// </summary>
    public OdfTextRun[] Runs = [];
    /// <summary>
    /// The underlying XML node, when loaded or written.
    /// </summary>
    public OdfNode? Node;
}
