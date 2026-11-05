namespace odf;

/// <summary>
/// Describes inline or externally referenced XForms instance data without retrieving it.
/// </summary>
public class OdfXFormsInstance
{
    /// <summary>The instance identifier.</summary>
    public string Id = "";
    /// <summary>The source IRI from the <c>src</c> attribute.</summary>
    public string Source = "";
    /// <summary>The fallback source IRI from the <c>resource</c> attribute.</summary>
    public string Resource = "";
    /// <summary>The effective source IRI, with <see cref="Source"/> taking precedence.</summary>
    public string EffectiveSource = "";
    /// <summary>Whether the effective source is external to the package or flat document.</summary>
    public bool IsExternal;
    /// <summary>The resolved package path for a package-relative source.</summary>
    public string PackagePath = "";
    /// <summary>Whether the referenced package entry exists.</summary>
    public bool PackageEntryExists;
    /// <summary>The first inline instance-data element, when present.</summary>
    public OdfNode? InlineRoot;
    /// <summary>All inline instance child nodes in source order.</summary>
    public OdfNode[] InlineNodes = [];
    /// <summary>All qualified instance attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The XML part containing the instance.</summary>
    public string PartPath = "";
    /// <summary>The underlying XForms instance element.</summary>
    public OdfNode? Node;
    internal string OriginalId = "";
    internal string OriginalSource = "";
    internal string OriginalResource = "";
}
