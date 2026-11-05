namespace odf;

/// <summary>
/// Describes a standalone numbered paragraph participating in a logical list.
/// </summary>
public class OdfNumberedParagraph
{
    /// <summary>
    /// The logical list identifier.
    /// </summary>
    public string ListId = "";
    /// <summary>
    /// The preserved one-based list level.
    /// </summary>
    public int Level;
    /// <summary>
    /// The referenced list-style name.
    /// </summary>
    public string StyleName = "";
    /// <summary>
    /// The preserved continue-numbering value.
    /// </summary>
    public string ContinueNumbering = "";
    /// <summary>
    /// The explicit start value.
    /// </summary>
    public string StartValue = "";
    /// <summary>
    /// The resolved ordinal.
    /// </summary>
    public int Ordinal;
    /// <summary>
    /// The resolved label text or image resource reference.
    /// </summary>
    public string LabelText = "";
    /// <summary>
    /// The effective list kind.
    /// </summary>
    public OdfTextListKind Kind;
    /// <summary>
    /// The contained paragraph or heading.
    /// </summary>
    public OdfTextBlock? TextBlock;
    /// <summary>
    /// The resolved list-style definition.
    /// </summary>
    public OdfListStyle? Style;
    /// <summary>
    /// The effective list-level rule.
    /// </summary>
    public OdfListLevel? LevelStyle;
    /// <summary>
    /// The XML part containing the numbered paragraph.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The underlying <c>text:numbered-paragraph</c> element.
    /// </summary>
    public OdfNode? Node;
}
