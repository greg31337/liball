namespace xml;

/// <summary>
/// Compiles XPath 3.1 expressions against an explicit static context.
/// </summary>
public interface IXPathCompiler
{
    /// <summary>
    /// Compiles an XPath expression.
    /// </summary>
    /// <param name="expression">The XPath source text.</param>
    /// <param name="context">The static context.</param>
    /// <returns>The immutable compiled expression.</returns>
    XPathCompiledExpression CompileXPath(string expression, XPathStaticContext context);
}

