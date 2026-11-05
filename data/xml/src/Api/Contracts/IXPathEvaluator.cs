namespace xml;

/// <summary>
/// Evaluates compiled XPath expressions against an explicit dynamic context.
/// </summary>
public interface IXPathEvaluator
{
    /// <summary>
    /// Evaluates a compiled XPath expression.
    /// </summary>
    /// <param name="expression">The compiled expression.</param>
    /// <param name="context">The dynamic context.</param>
    /// <returns>The evaluation result.</returns>
    XPathEvaluationResult EvaluateXPath(XPathCompiledExpression expression, XPathDynamicContext context);
}

