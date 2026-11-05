namespace xml;

/// <summary>
/// Executes compiled XSLT stylesheets and packages.
/// </summary>
public interface IXsltTransformer
{
    /// <summary>
    /// Executes one transformation.
    /// </summary>
    /// <param name="executable">The compiled executable.</param>
    /// <param name="options">The invocation context and destinations.</param>
    /// <returns>The transformation result.</returns>
    XsltTransformResult TransformXslt(XsltExecutable executable, XsltTransformOptions options);
}

