namespace odf;

/// <summary>
/// Describes one local-name, embedded, external, or definition source for a font face.
/// </summary>
public class OdfFontFaceSource
{
    /// <summary>
    /// The locally installed font name for a name source.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The source IRI for a URI or definition source.
    /// </summary>
    public string Href = "";
    /// <summary>
    /// The preserved XLink type, normally <c>simple</c>.
    /// </summary>
    public string XLinkType = "";
    /// <summary>
    /// The preserved XLink actuate value.
    /// </summary>
    public string Actuate = "";
    /// <summary>
    /// The declared font format names in source order.
    /// </summary>
    public string[] Formats = [];
    /// <summary>
    /// Whether this source is represented by <c>svg:definition-src</c>.
    /// </summary>
    public bool IsDefinitionSource;
    /// <summary>
    /// Whether the source refers to a package resource.
    /// </summary>
    public bool IsEmbedded;
    /// <summary>
    /// Whether the source is external to the package or flat document.
    /// </summary>
    public bool IsExternal;
    /// <summary>
    /// The resolved package path for an embedded source.
    /// </summary>
    public string ResolvedPath = "";
    /// <summary>
    /// Whether the resolved embedded package resource exists.
    /// </summary>
    public bool ResourceExists;
    /// <summary>
    /// The underlying source XML element, when loaded or written.
    /// </summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal string OriginalName = "";
    internal string OriginalHref = "";
    internal string OriginalXLinkType = "";
    internal string OriginalActuate = "";
    internal string[] OriginalFormats = [];
    internal bool OriginalIsDefinitionSource;
}
