namespace xml;

/// <summary>
/// Navigates XML trees, axes, inherited context, and node identity.
/// </summary>
public interface IXmlTreeNavigator
{
    /// <summary><para>Gets the parent node.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The parent, or null.</returns>
    XmlNode? FindXmlParent(XmlNode node);
    /// <summary><para>Selects child nodes.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The selected nodes.</returns>
    XmlNodeSelection SelectXmlChildren(XmlNode node);
    /// <summary><para>Selects ancestor nodes.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The selected nodes.</returns>
    XmlNodeSelection SelectXmlAncestors(XmlNode node);
    /// <summary><para>Selects descendant nodes.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The selected nodes.</returns>
    XmlNodeSelection SelectXmlDescendants(XmlNode node);
    /// <summary><para>Selects following sibling nodes.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The selected nodes.</returns>
    XmlNodeSelection SelectXmlFollowingSiblings(XmlNode node);
    /// <summary><para>Selects preceding sibling nodes.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The selected nodes.</returns>
    XmlNodeSelection SelectXmlPrecedingSiblings(XmlNode node);
    /// <summary><para>Selects nodes on the following axis.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The selected nodes.</returns>
    XmlNodeSelection SelectXmlFollowing(XmlNode node);
    /// <summary><para>Selects nodes on the preceding axis.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The selected nodes.</returns>
    XmlNodeSelection SelectXmlPreceding(XmlNode node);
    /// <summary><para>Selects attribute nodes.</para></summary>
    /// <param name="node">The context element.</param>
    /// <returns>The selected attributes.</returns>
    XmlNodeSelection SelectXmlAttributes(XmlNode node);
    /// <summary><para>Gets in-scope namespace bindings.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The namespace bindings.</returns>
    XmlNamespaceBinding[] SelectXmlNamespaces(XmlNode node);
    /// <summary><para>Selects the nodes covered by a tree range.</para></summary>
    /// <param name="range">The tree range.</param>
    /// <returns>The selected nodes.</returns>
    XmlNodeSelection SelectXmlRange(XmlRange range);
    /// <summary><para>Finds an element by ID.</para></summary>
    /// <param name="document">The document to search.</param>
    /// <param name="identifier">The ID value.</param>
    /// <returns>The element, or null.</returns>
    XmlNode? FindXmlNodeById(XmlDocument document, string identifier);
    /// <summary><para>Gets the string value.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The string value.</returns>
    string GetXmlNodeStringValue(XmlNode node);
    /// <summary><para>Gets the effective base URI.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The base URI, or null.</returns>
    string? GetXmlNodeBaseUri(XmlNode node);
    /// <summary><para>Gets the effective language.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The language, or null.</returns>
    string? GetXmlNodeLanguage(XmlNode node);
    /// <summary><para>Gets the effective whitespace mode.</para></summary>
    /// <param name="node">The context node.</param>
    /// <returns>The whitespace mode.</returns>
    XmlSpaceMode GetXmlNodeSpaceMode(XmlNode node);
    /// <summary><para>Compares document order.</para></summary>
    /// <param name="left">The left node.</param>
    /// <param name="right">The right node.</param>
    /// <returns>A negative, zero, or positive comparison value.</returns>
    int CompareXmlNodeOrder(XmlNode left, XmlNode right);
    /// <summary><para>Tests node identity.</para></summary>
    /// <param name="left">The left node.</param>
    /// <param name="right">The right node.</param>
    /// <returns>True when both values identify the same node.</returns>
    bool ContainsXmlNodeIdentity(XmlNode left, XmlNode right);
}
