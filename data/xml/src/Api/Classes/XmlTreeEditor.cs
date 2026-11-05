namespace xml;

/// <summary>Provides the public XML tree editor implementation shell.</summary>
public class XmlTreeEditor : IXmlTreeEditor
{
    internal TreeMutation Mutation;
    internal TreeContentMutation ContentMutation;

    /// <summary>Creates an XML tree editor.</summary>
    public XmlTreeEditor()
    {
        Mutation = new TreeMutation();
        ContentMutation = new TreeContentMutation(Mutation);
    }

    /// <summary>Creates a tree node.</summary>
    public XmlNode CreateXmlTreeNode(XmlDocument document, XmlNodeKind kind, XmlName? name, string? value)
    {
        return Mutation.CreateXmlTreeNodeOperation(document, kind, name, value);
    }

    /// <summary>Clones a tree node.</summary>
    public XmlNode CloneXmlTreeNode(XmlNode node, bool deep)
    {
        return Mutation.CloneXmlTreeNodeOperation(node, deep);
    }

    /// <summary>Imports a tree node.</summary>
    public XmlNode ImportXmlTreeNode(XmlDocument document, XmlNode node, bool deep)
    {
        return Mutation.ImportXmlTreeNodeOperation(document, node, deep);
    }

    /// <summary>Adopts a tree node.</summary>
    public XmlNode AdoptXmlTreeNode(XmlDocument document, XmlNode node)
    {
        return Mutation.AdoptXmlTreeNodeOperation(document, node);
    }

    /// <summary>Appends a child node.</summary>
    public XmlNode AppendXmlTreeChild(XmlNode parent, XmlNode child)
    {
        return Mutation.AppendXmlTreeChildOperation(parent, child);
    }

    /// <summary>Inserts a node before a child.</summary>
    public XmlNode InsertXmlTreeBefore(XmlNode parent, XmlNode node, XmlNode reference)
    {
        return Mutation.InsertXmlTreeBeforeOperation(parent, node, reference);
    }

    /// <summary>Replaces a child node.</summary>
    public XmlNode ReplaceXmlTreeNode(XmlNode parent, XmlNode replacement, XmlNode existing)
    {
        return Mutation.ReplaceXmlTreeNodeOperation(parent, replacement, existing);
    }

    /// <summary>Removes a child node.</summary>
    public XmlNode RemoveXmlTreeNode(XmlNode parent, XmlNode child)
    {
        return Mutation.RemoveXmlTreeNodeOperation(parent, child);
    }

    /// <summary>Normalizes a tree node.</summary>
    public void NormalizeXmlTreeNode(XmlNode node)
    {
        ContentMutation.NormalizeXmlTreeNodeOperation(node);
    }

    /// <summary>Sets tree text.</summary>
    public void SetXmlTreeText(XmlNode node, string value)
    {
        ContentMutation.SetXmlTreeTextOperation(node, value);
    }

    /// <summary>Sets a tree attribute.</summary>
    public void SetXmlTreeAttribute(XmlNode element, XmlNode attribute)
    {
        ContentMutation.SetXmlTreeAttributeOperation(element, attribute);
    }

    /// <summary>Removes a tree attribute.</summary>
    public XmlNode? RemoveXmlTreeAttribute(XmlNode element, XmlName name)
    {
        return ContentMutation.RemoveXmlTreeAttributeOperation(element, name);
    }

    /// <summary>Freezes a tree.</summary>
    public void FreezeXmlTree(XmlNode node, bool deep)
    {
        ContentMutation.FreezeXmlTreeOperation(node, deep);
    }
}
