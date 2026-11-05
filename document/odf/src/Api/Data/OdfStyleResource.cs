namespace odf;

/// <summary>
/// Describes a reusable style resource or a nested background-image or sound resource.
/// </summary>
public class OdfStyleResource
{
    /// <summary>
    /// The resource kind.
    /// </summary>
    public OdfStyleResourceKind Kind;
    /// <summary>
    /// The resource name, when the resource is named.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The optional user-facing display name.
    /// </summary>
    public string DisplayName = "";
    /// <summary>
    /// The linked resource reference used by image and sound resources.
    /// </summary>
    public string Href = "";
    /// <summary>
    /// The XML part containing the resource, or an empty string for a flat document.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// All qualified resource attributes and their preserved lexical values.
    /// </summary>
    public OdfStyleProperty[] Attributes = [];
    /// <summary>
    /// The ordered resource content, including gradient stops, placeholders, and binary data.
    /// </summary>
    public OdfNode[] Content = [];
    /// <summary>
    /// The typed placeholders for a presentation page-layout resource.
    /// </summary>
    public OdfPresentationPlaceholder[] PresentationPlaceholders = [];
    /// <summary>
    /// The owning element for a nested background image or sound.
    /// </summary>
    public OdfNode? ContainerNode;
    /// <summary>
    /// The underlying resource element, when loaded or written.
    /// </summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal OdfNode? OriginalContainerNode;
    internal OdfAttribute[] OriginalAttributes = [];
    internal OdfNode[] OriginalContent = [];
    internal OdfStyleResourceKind OriginalKind;
    internal string OriginalName = "";
    internal string OriginalDisplayName = "";
    internal string OriginalHref = "";
    internal string OriginalPartPath = "";
}
