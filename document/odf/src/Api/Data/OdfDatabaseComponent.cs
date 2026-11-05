namespace odf;

/// <summary>
/// Describes an inert database form, report, or component collection.
/// </summary>
public class OdfDatabaseComponent
{
    /// <summary>The component kind.</summary>
    public OdfDatabaseComponentKind Kind;
    /// <summary>The component or collection name.</summary>
    public string Name = "";
    /// <summary>The human-readable title.</summary>
    public string Title = "";
    /// <summary>The human-readable description.</summary>
    public string Description = "";
    /// <summary>The linked component resource IRI.</summary>
    public string Href = "";
    /// <summary>The preserved template flag.</summary>
    public string AsTemplate = "";
    /// <summary>Whether the resource IRI is external to the package.</summary>
    public bool IsExternal;
    /// <summary>The resolved package entry path, when package-relative.</summary>
    public string PackagePath = "";
    /// <summary>Whether the resolved package entry exists.</summary>
    public bool PackageEntryExists;
    /// <summary>The nested components and collections.</summary>
    public OdfDatabaseComponent[] Children = [];
    /// <summary>The optional inline document element.</summary>
    public OdfNode? InlineDocument;
    /// <summary>The underlying component or collection element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalTitle = "";
    internal string OriginalDescription = "";
    internal string OriginalHref = "";
    internal string OriginalAsTemplate = "";
}
