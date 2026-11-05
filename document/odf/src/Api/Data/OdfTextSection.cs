namespace odf;

/// <summary>
/// Describes a named, linked, or protected text section.
/// </summary>
public class OdfTextSection
{
    /// <summary>
    /// The section name.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The section style name.
    /// </summary>
    public string StyleName = "";
    /// <summary>
    /// The section condition expression.
    /// </summary>
    public string Condition = "";
    /// <summary>
    /// The preserved display value.
    /// </summary>
    public string Display = "";
    /// <summary>
    /// Whether the section is protected.
    /// </summary>
    public bool IsProtected;
    /// <summary>
    /// The preserved lexical protected value.
    /// </summary>
    public string ProtectedValue = "";
    /// <summary>
    /// The section protection key.
    /// </summary>
    public string ProtectionKey = "";
    /// <summary>
    /// The protection-key digest algorithm IRI.
    /// </summary>
    public string ProtectionKeyDigestAlgorithm = "";
    /// <summary>
    /// The linked section source IRI.
    /// </summary>
    public string SourceHref = "";
    /// <summary>
    /// The linked source section name.
    /// </summary>
    public string SourceSectionName = "";
    /// <summary>
    /// The linked source filter name.
    /// </summary>
    public string SourceFilterName = "";
    /// <summary>
    /// The source link show behavior.
    /// </summary>
    public string SourceShow = "";
    /// <summary>
    /// The source link activation behavior.
    /// </summary>
    public string SourceActuate = "";
    /// <summary>
    /// The resolved columns from the section style, when present.
    /// </summary>
    public OdfTextColumnSet? Columns;
    /// <summary>
    /// Paragraphs and headings contained directly or indirectly by the section.
    /// </summary>
    public OdfTextBlock[] TextBlocks = [];
    /// <summary>
    /// Direct nested text sections.
    /// </summary>
    public OdfTextSection[] Sections = [];
    /// <summary>
    /// The XML part containing the section.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The underlying <c>text:section</c> element.
    /// </summary>
    public OdfNode? Node;
    /// <summary>
    /// The underlying linked <c>text:section-source</c> element, when present.
    /// </summary>
    public OdfNode? SourceNode;
    internal bool OriginalIsProtected;
}
