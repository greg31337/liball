namespace xml;

/// <summary>
/// Compiles XSLT stylesheets and packages.
/// </summary>
public interface IXsltCompiler
{
    /// <summary>
    /// Compiles an XSLT stylesheet or package.
    /// </summary>
    /// <param name="stylesheet">The principal stylesheet input.</param>
    /// <param name="options">The compilation options.</param>
    /// <param name="resolver">The module and package resolver.</param>
    /// <returns>The immutable executable.</returns>
    XsltExecutable CompileXslt(XmlInput stylesheet, XsltCompileOptions options, IXmlResourceResolver resolver);
}

