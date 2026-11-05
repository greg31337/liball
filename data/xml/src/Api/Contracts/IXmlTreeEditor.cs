namespace xml;

/// <summary>
/// Performs validated atomic mutations on XML trees.
/// </summary>
public interface IXmlTreeEditor
{
    /// <summary><para>Creates a node owned by a document.</para></summary>
    /// <param name="document">The owner document.</param><param name="kind">The node kind.</param><param name="name">The optional node name.</param><param name="value">The optional node value.</param>
    /// <returns>The created node.</returns>
    XmlNode CreateXmlTreeNode(XmlDocument document, XmlNodeKind kind, XmlName? name, string? value);
    /// <summary><para>Clones a node.</para></summary>
    /// <param name="node">The node to clone.</param><param name="deep">Whether descendants are cloned.</param>
    /// <returns>The clone.</returns>
    XmlNode CloneXmlTreeNode(XmlNode node, bool deep);
    /// <summary><para>Imports a node into a document.</para></summary>
    /// <param name="document">The target document.</param><param name="node">The source node.</param><param name="deep">Whether descendants are imported.</param>
    /// <returns>The imported node.</returns>
    XmlNode ImportXmlTreeNode(XmlDocument document, XmlNode node, bool deep);
    /// <summary><para>Adopts a node into a document.</para></summary>
    /// <param name="document">The target document.</param><param name="node">The node to adopt.</param>
    /// <returns>The adopted node.</returns>
    XmlNode AdoptXmlTreeNode(XmlDocument document, XmlNode node);
    /// <summary><para>Appends a child.</para></summary>
    /// <param name="parent">The parent node.</param><param name="child">The child node.</param>
    /// <returns>The appended node.</returns>
    XmlNode AppendXmlTreeChild(XmlNode parent, XmlNode child);
    /// <summary><para>Inserts a node before a reference node.</para></summary>
    /// <param name="parent">The parent node.</param><param name="node">The node to insert.</param><param name="reference">The reference child.</param>
    /// <returns>The inserted node.</returns>
    XmlNode InsertXmlTreeBefore(XmlNode parent, XmlNode node, XmlNode reference);
    /// <summary><para>Replaces a child node.</para></summary>
    /// <param name="parent">The parent node.</param><param name="replacement">The replacement node.</param><param name="existing">The child being replaced.</param>
    /// <returns>The removed node.</returns>
    XmlNode ReplaceXmlTreeNode(XmlNode parent, XmlNode replacement, XmlNode existing);
    /// <summary><para>Removes a child node.</para></summary>
    /// <param name="parent">The parent node.</param><param name="child">The child to remove.</param>
    /// <returns>The removed node.</returns>
    XmlNode RemoveXmlTreeNode(XmlNode parent, XmlNode child);
    /// <summary><para>Normalizes adjacent text content.</para></summary>
    /// <param name="node">The subtree root.</param>
    void NormalizeXmlTreeNode(XmlNode node);
    /// <summary><para>Replaces node text content.</para></summary>
    /// <param name="node">The target node.</param><param name="value">The new text content.</param>
    void SetXmlTreeText(XmlNode node, string value);
    /// <summary><para>Sets or replaces an attribute.</para></summary>
    /// <param name="element">The target element.</param><param name="attribute">The attribute node.</param>
    void SetXmlTreeAttribute(XmlNode element, XmlNode attribute);
    /// <summary><para>Removes an attribute by expanded name.</para></summary>
    /// <param name="element">The target element.</param><param name="name">The expanded attribute name.</param>
    /// <returns>The removed attribute, or null.</returns>
    XmlNode? RemoveXmlTreeAttribute(XmlNode element, XmlName name);
    /// <summary><para>Freezes a tree against mutation.</para></summary>
    /// <param name="node">The subtree root.</param><param name="deep">Whether descendants are frozen.</param>
    void FreezeXmlTree(XmlNode node, bool deep);
}

