namespace xml;

/// <summary>Provides the baseline resolver for externally registered XPath functions.</summary>
public class XPathFunctionResolver : IXPathFunctionResolver
{
    /// <summary>Creates an XPath function resolver.</summary>
    public XPathFunctionResolver()
    {
    }

    /// <summary>Resolves an XPath function.</summary>
    public IXPathFunction? ResolveXPathFunction(XmlName name, int arity, XPathStaticContext context)
    {
        if (name == null || name.LocalName == null || name.LocalName.Length == 0)
        {
            throw new System.ArgumentException("An expanded function name is required.", "name");
        }

        if (arity < 0)
        {
            throw new System.ArgumentOutOfRangeException("arity");
        }

        if (context == null)
        {
            throw new System.ArgumentNullException("context");
        }

        return null;
    }
}
