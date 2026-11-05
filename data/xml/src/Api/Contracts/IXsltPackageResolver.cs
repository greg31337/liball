namespace xml;

/// <summary>
/// Resolves XSLT packages and stylesheet module resources.
/// </summary>
public interface IXsltPackageResolver
{
    /// <summary>
    /// Resolves and compiles a referenced package.
    /// </summary>
    /// <param name="package">The package name, version, and location request.</param>
    /// <param name="options">The compilation options.</param>
    /// <returns>The resolved package executable.</returns>
    XsltExecutable ResolveXsltPackage(XsltPackageReference package, XsltCompileOptions options);

    /// <summary>
    /// Resolves an included or imported stylesheet module.
    /// </summary>
    /// <param name="uri">The module URI.</param>
    /// <param name="baseUri">The referencing module base URI.</param>
    /// <param name="policy">The resource policy.</param>
    /// <returns>The resolved module resource.</returns>
    XmlResolvedResource ResolveXsltModule(string uri, string? baseUri, XmlResolverPolicy policy);
}
