namespace xml;

/// <summary>Provides XML tree navigation and inherited context lookup.</summary>
public class XmlTreeNavigator : IXmlTreeNavigator
{
    internal TreeNavigation Navigation;

    /// <summary>Creates an XML tree navigator.</summary>
    public XmlTreeNavigator()
    {
        Navigation = new TreeNavigation();
    }

    /// <summary>Finds a node parent.</summary>
    public XmlNode? FindXmlParent(XmlNode node)
    {
        return Navigation.FindXmlParentOperation(node);
    }

    /// <summary>Selects child nodes.</summary>
    public XmlNodeSelection SelectXmlChildren(XmlNode node)
    {
        return Navigation.SelectXmlChildrenOperation(node);
    }

    /// <summary>Selects ancestor nodes.</summary>
    public XmlNodeSelection SelectXmlAncestors(XmlNode node)
    {
        return Navigation.SelectXmlAncestorsOperation(node);
    }

    /// <summary>Selects descendant nodes.</summary>
    public XmlNodeSelection SelectXmlDescendants(XmlNode node)
    {
        return Navigation.SelectXmlDescendantsOperation(node);
    }

    /// <summary>Selects following siblings.</summary>
    public XmlNodeSelection SelectXmlFollowingSiblings(XmlNode node)
    {
        return Navigation.SelectXmlFollowingSiblingsOperation(node);
    }

    /// <summary>Selects preceding siblings.</summary>
    public XmlNodeSelection SelectXmlPrecedingSiblings(XmlNode node)
    {
        return Navigation.SelectXmlPrecedingSiblingsOperation(node);
    }

    /// <summary>Selects the following axis.</summary>
    public XmlNodeSelection SelectXmlFollowing(XmlNode node)
    {
        return Navigation.SelectXmlFollowingOperation(node);
    }

    /// <summary>Selects the preceding axis.</summary>
    public XmlNodeSelection SelectXmlPreceding(XmlNode node)
    {
        return Navigation.SelectXmlPrecedingOperation(node);
    }

    /// <summary>Selects attribute nodes.</summary>
    public XmlNodeSelection SelectXmlAttributes(XmlNode node)
    {
        return Navigation.SelectXmlAttributesOperation(node);
    }

    /// <summary>Selects in-scope namespaces.</summary>
    public XmlNamespaceBinding[] SelectXmlNamespaces(XmlNode node)
    {
        return Navigation.SelectXmlNamespacesOperation(node);
    }

    /// <summary>Selects a tree range.</summary>
    public XmlNodeSelection SelectXmlRange(XmlRange range)
    {
        return Navigation.SelectXmlRangeOperation(range);
    }

    /// <summary>Finds a node by identifier.</summary>
    public XmlNode? FindXmlNodeById(XmlDocument document, string identifier)
    {
        return Navigation.FindXmlNodeByIdOperation(document, identifier);
    }

    /// <summary>Gets a node string value.</summary>
    public string GetXmlNodeStringValue(XmlNode node)
    {
        return Navigation.GetXmlNodeStringValueOperation(node);
    }

    /// <summary>Gets a node base URI.</summary>
    public string? GetXmlNodeBaseUri(XmlNode node)
    {
        return Navigation.GetXmlNodeBaseUriOperation(node);
    }

    /// <summary>Gets a node language.</summary>
    public string? GetXmlNodeLanguage(XmlNode node)
    {
        return Navigation.GetXmlNodeLanguageOperation(node);
    }

    /// <summary>Gets a node whitespace mode.</summary>
    public XmlSpaceMode GetXmlNodeSpaceMode(XmlNode node)
    {
        return Navigation.GetXmlNodeSpaceModeOperation(node);
    }

    /// <summary>Compares node document order.</summary>
    public int CompareXmlNodeOrder(XmlNode left, XmlNode right)
    {
        return Navigation.CompareXmlNodeOrderOperation(left, right);
    }

    /// <summary>Tests node identity.</summary>
    public bool ContainsXmlNodeIdentity(XmlNode left, XmlNode right)
    {
        return Navigation.ContainsXmlNodeIdentityOperation(left, right);
    }
}
