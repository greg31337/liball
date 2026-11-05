namespace xml;

/// <summary>
/// Invokes one registered XPath extension function.
/// </summary>
public interface IXPathFunction
{
    /// <summary>
    /// Invokes the function with an explicit binding, arguments, and dynamic context.
    /// </summary>
    /// <param name="binding">The registered function binding.</param>
    /// <param name="arguments">The argument sequences in parameter order.</param>
    /// <param name="context">The dynamic evaluation context.</param>
    /// <returns>The function result sequence.</returns>
    XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context);
}
