namespace xml;

/// <summary>
/// Carries an XSLT package dependency reference.
/// </summary>
public class XsltPackageReference
{
    /// <summary>
    /// The package name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The requested package version or range.
    /// </summary>
    public string? Version;
    /// <summary>
    /// The package location URI.
    /// </summary>
    public string? LocationUri;
    /// <summary>
    /// The accepted component visibility.
    /// </summary>
    public XsltComponentVisibility Visibility;
    /// <summary>
    /// Whether the package dependency is optional.
    /// </summary>
    public bool IsOptional;
}

