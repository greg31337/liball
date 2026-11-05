namespace odf;

/// <summary>
/// Describes an external or inline XML Schema declaration used by an XForms model.
/// </summary>
public class OdfXFormsSchemaReference
{
    /// <summary>The preserved external schema IRI, or an empty string for inline schema.</summary>
    public string Location = "";
    /// <summary>Whether this describes an inline XML Schema element.</summary>
    public bool IsInline;
    /// <summary>Whether the schema location is external to the package or flat document.</summary>
    public bool IsExternal;
    /// <summary>The resolved package path for a package-relative schema.</summary>
    public string PackagePath = "";
    /// <summary>Whether the referenced package entry exists.</summary>
    public bool PackageEntryExists;
    /// <summary>The model or inline schema node carrying the declaration.</summary>
    public OdfNode? Node;
    internal string OriginalLocation = "";
}
