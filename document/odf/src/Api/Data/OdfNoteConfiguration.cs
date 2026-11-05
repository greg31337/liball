namespace odf;

/// <summary>
/// Describes footnote or endnote numbering, styling, placement, and continuation notices.
/// </summary>
public class OdfNoteConfiguration
{
    /// <summary>
    /// The configured note class.
    /// </summary>
    public OdfNoteKind Kind;
    /// <summary>
    /// The text style used for citations in the main text.
    /// </summary>
    public string CitationStyleName = "";
    /// <summary>
    /// The text style used for citations in note bodies.
    /// </summary>
    public string CitationBodyStyleName = "";
    /// <summary>
    /// The default paragraph style used by note bodies.
    /// </summary>
    public string DefaultStyleName = "";
    /// <summary>
    /// The master-page name used by endnotes.
    /// </summary>
    public string MasterPageName = "";
    /// <summary>
    /// The first numbering value.
    /// </summary>
    public string StartValue = "";
    /// <summary>
    /// The note number format.
    /// </summary>
    public string NumberFormat = "";
    /// <summary>
    /// The note number prefix.
    /// </summary>
    public string NumberPrefix = "";
    /// <summary>
    /// The note number suffix.
    /// </summary>
    public string NumberSuffix = "";
    /// <summary>
    /// The preserved letter-synchronization value.
    /// </summary>
    public string NumberLetterSync = "";
    /// <summary>
    /// The numbering restart scope, such as document, chapter, or page.
    /// </summary>
    public string StartNumberingAt = "";
    /// <summary>
    /// The footnote placement value.
    /// </summary>
    public string FootnotesPosition = "";
    /// <summary>
    /// The forward continuation notice text.
    /// </summary>
    public string ForwardNotice = "";
    /// <summary>
    /// The backward continuation notice text.
    /// </summary>
    public string BackwardNotice = "";
    /// <summary>
    /// All qualified configuration attributes.
    /// </summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>
    /// The XML part containing the configuration.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The underlying notes-configuration element.
    /// </summary>
    public OdfNode? Node;
    /// <summary>
    /// The underlying forward continuation notice element.
    /// </summary>
    public OdfNode? ForwardNoticeNode;
    /// <summary>
    /// The underlying backward continuation notice element.
    /// </summary>
    public OdfNode? BackwardNoticeNode;
    internal string OriginalForwardNotice = "";
    internal string OriginalBackwardNotice = "";
}
