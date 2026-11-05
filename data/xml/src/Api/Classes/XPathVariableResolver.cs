namespace xml;

/// <summary>Provides a resolver over variables present in a dynamic context.</summary>
public class XPathVariableResolver : IXPathVariableResolver
{
    /// <summary>Creates an XPath variable resolver.</summary>
    public XPathVariableResolver()
    {
    }

    /// <summary>Resolves an XPath variable.</summary>
    public XPathVariableBinding? ResolveXPathVariable(XmlName name, XPathDynamicContext context)
    {
        if (name == null || name.LocalName == null || name.LocalName.Length == 0)
        {
            throw new System.ArgumentException("An expanded variable name is required.", "name");
        }

        if (context == null)
        {
            throw new System.ArgumentNullException("context");
        }

        XPathVariableBinding[] variables = context.Variables == null ? [] : context.Variables;
        int index = variables.Length - 1;
        while (index >= 0)
        {
            XPathVariableBinding? candidate = variables[index];
            if (candidate != null && SameXPathVariableName(candidate.Name, name))
            {
                return new XdmFunctionOperations().FreezeXPathVariableBinding(candidate);
            }

            index = index - 1;
        }

        return null;
    }

    internal bool SameXPathVariableName(XmlName? left, XmlName right)
    {
        if (left == null)
        {
            return false;
        }

        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return left.LocalName == right.LocalName && leftNamespace == rightNamespace;
    }
}
