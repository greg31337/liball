namespace xml;

/// <summary>
/// Resolves XPath extension functions without delegates.
/// </summary>
public interface IXPathFunctionResolver
{
    /// <summary>
    /// Resolves a function implementation by expanded name and arity.
    /// </summary>
    /// <param name="name">The expanded function name.</param>
    /// <param name="arity">The function arity.</param>
    /// <param name="context">The static compilation context.</param>
    /// <returns>The function implementation, or null when it is unavailable.</returns>
    IXPathFunction? ResolveXPathFunction(XmlName name, int arity, XPathStaticContext context);
}
