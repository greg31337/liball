namespace asn1;

/// <summary>
/// Configures XML Schema to ASN.1 mapping.
/// </summary>
public class AsnXsdImportOptions
{
    /// <summary>
    /// Indicates whether ModuleName contains an explicit generated module name.
    /// </summary>
    public bool HasModuleName;

    /// <summary>
    /// Contains the generated module name when HasModuleName is true.
    /// </summary>
    public string ModuleName = "";

    /// <summary>
    /// Indicates whether relevant XML Schema annotations are retained.
    /// </summary>
    public bool PreserveAnnotations;

    /// <summary>
    /// Contains resource limits for XML Schema loading when supplied.
    /// </summary>
    public AsnResourceLimits? ResourceLimits;
}
