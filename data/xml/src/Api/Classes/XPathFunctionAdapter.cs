namespace xml;

/// <summary>Provides the public XPath extension-function adapter shell.</summary>
public class XPathFunctionAdapter : IXPathFunction
{
    /// <summary>Creates an XPath function adapter.</summary>
    public XPathFunctionAdapter()
    {
    }

    /// <summary>Invokes an XPath extension function.</summary>
    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        return new XdmSequenceOperations().CreateEmptyXdmSequence();
    }
}
