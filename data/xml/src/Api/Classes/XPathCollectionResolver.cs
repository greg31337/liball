namespace xml;

/// <summary>Provides an empty baseline XPath document and collection resolver.</summary>
public class XPathCollectionResolver : IXPathCollectionResolver
{
    /// <summary>Creates an XPath collection resolver.</summary>
    public XPathCollectionResolver()
    {
    }

    /// <summary>Resolves an XPath document.</summary>
    public XmlDocument? ResolveXPathDocument(string uri, XmlResolverPolicy policy)
    {
        if (uri == null)
        {
            throw new System.ArgumentNullException("uri");
        }

        if (policy == null)
        {
            throw new System.ArgumentNullException("policy");
        }

        return null;
    }

    /// <summary>Resolves an XPath collection.</summary>
    public XdmSequence ResolveXPathCollection(string? uri, XmlResolverPolicy policy)
    {
        if (policy == null)
        {
            throw new System.ArgumentNullException("policy");
        }

        return new XdmSequenceOperations().CreateEmptyXdmSequence();
    }

    /// <summary>Resolves XPath collection member URIs.</summary>
    public string[] ResolveXPathUriCollection(string? uri, XmlResolverPolicy policy)
    {
        if (policy == null)
        {
            throw new System.ArgumentNullException("policy");
        }

        return [];
    }
}
