namespace xml;

/// <summary>
/// Resolves external XPath variable bindings without delegates.
/// </summary>
public interface IXPathVariableResolver
{
    /// <summary>
    /// Resolves an external variable by expanded name.
    /// </summary>
    /// <param name="name">The expanded variable name.</param>
    /// <param name="context">The dynamic evaluation context.</param>
    /// <returns>The resolved binding, or null when it is unavailable.</returns>
    XPathVariableBinding? ResolveXPathVariable(XmlName name, XPathDynamicContext context);
}
