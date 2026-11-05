using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class OdfTreeEditor
{
    OdfNode[] roots;
    OdfNodeIndex index;

    internal OdfTreeEditor(OdfNode[] roots)
    {
        if (roots == null)
        {
            throw new OdfEditException("XML tree roots are required.");
        }
        this.roots = roots;
        index = new OdfNodeIndex(roots);
    }

    internal OdfNode[] GetRoots()
    {
        OdfNode[] result = new OdfNode[roots.Length];
        Array.Copy(roots, result, roots.Length);
        return result;
    }

    internal OdfNode CreateElement(OdfQualifiedName name)
    {
        if (!OdfNameRules.IsQualifiedNameValue(name))
        {
            throw new OdfXmlException("A valid qualified name is required to create an element.");
        }
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = CopyQualifiedName(name)
        };
    }

    internal OdfNode CreateText(string value)
    {
        if (value == null)
        {
            throw new OdfXmlException("Text node content is required.");
        }
        return new OdfNode
        {
            Kind = OdfNodeKind.Text,
            Value = value
        };
    }

    internal OdfNode CreateComment(string value)
    {
        if (value == null
            || value.Contains("--", StringComparison.Ordinal)
            || value.EndsWith("-", StringComparison.Ordinal))
        {
            throw new OdfXmlException("The XML comment content is invalid.");
        }
        return new OdfNode
        {
            Kind = OdfNodeKind.Comment,
            Value = value
        };
    }

    internal OdfNode CreateProcessingInstruction(
        string target,
        string value)
    {
        if (!OdfNameRules.IsName(target)
            || string.Equals(target, "xml", StringComparison.OrdinalIgnoreCase)
            || value == null
            || value.Contains("?>", StringComparison.Ordinal))
        {
            throw new OdfXmlException("The XML processing instruction is invalid.");
        }
        return new OdfNode
        {
            Kind = OdfNodeKind.ProcessingInstruction,
            Name = new OdfQualifiedName
            {
                LocalName = target
            },
            Value = value
        };
    }

    internal void InsertRoot(
        int insertionIndex,
        OdfNode node)
    {
        if (insertionIndex < 0 || insertionIndex > roots.Length)
        {
            throw new OdfEditException("The root insertion index is outside the tree.");
        }
        ValidateAttachable(node, null);
        LinkParents(node, null);
        roots = InsertNodeArray(roots, insertionIndex, node);
        index.UpdateRoots(roots);
    }

    internal void InsertChild(
        OdfNode parent,
        int insertionIndex,
        OdfNode child)
    {
        RequireElement(parent);
        RequireAttached(parent);
        if (insertionIndex < 0 || insertionIndex > parent.Children.Length)
        {
            throw new OdfEditException("The child insertion index is outside the element.");
        }
        ValidateAttachable(child, parent);
        LinkParents(child, parent);
        parent.Children = InsertNodeArray(
            parent.Children,
            insertionIndex,
            child);
        parent.WasEmptyElement = false;
        index.Invalidate();
    }

    internal void ReplaceNode(
        OdfNode existing,
        OdfNode replacement)
    {
        RequireAttached(existing);
        if (existing == replacement)
        {
            return;
        }
        ValidateAttachable(replacement, existing.Parent);
        OdfNode? parent = existing.Parent;
        if (parent == null)
        {
            int rootIndex = FindRootIndex(existing);
            if (rootIndex < 0)
            {
                throw new OdfEditException("The node is not an attached tree root.");
            }
            LinkParents(replacement, null);
            roots = ReplaceNodeArray(roots, rootIndex, replacement);
            existing.Parent = null;
            index.UpdateRoots(roots);
            return;
        }
        int childIndex = FindChildIndex(parent, existing);
        if (childIndex < 0)
        {
            throw new OdfEditException("The node parent does not own the node.");
        }
        LinkParents(replacement, parent);
        parent.Children = ReplaceNodeArray(
            parent.Children,
            childIndex,
            replacement);
        existing.Parent = null;
        index.Invalidate();
    }

    internal void RemoveNode(OdfNode node)
    {
        RequireAttached(node);
        OdfNode? parent = node.Parent;
        if (parent == null)
        {
            int rootIndex = FindRootIndex(node);
            if (rootIndex < 0)
            {
                throw new OdfEditException("The node is not an attached tree root.");
            }
            roots = RemoveNodeArray(roots, rootIndex);
            node.Parent = null;
            index.UpdateRoots(roots);
            return;
        }
        int childIndex = FindChildIndex(parent, node);
        if (childIndex < 0)
        {
            throw new OdfEditException("The node parent does not own the node.");
        }
        parent.Children = RemoveNodeArray(parent.Children, childIndex);
        node.Parent = null;
        index.Invalidate();
    }

    internal void ReparentNode(
        OdfNode node,
        OdfNode? newParent,
        int insertionIndex)
    {
        RequireAttached(node);
        if (newParent != null)
        {
            RequireElement(newParent);
            RequireAttached(newParent);
            if (ContainsSubtree(node, newParent))
            {
                throw new OdfEditException("A node cannot be moved beneath its own subtree.");
            }
        }

        OdfNode? oldParent = node.Parent;
        int targetCount;
        if (newParent == oldParent)
        {
            if (newParent == null)
            {
                targetCount = roots.Length - 1;
            }
            else
            {
                targetCount = newParent.Children.Length - 1;
            }
        }
        else if (newParent == null)
        {
            targetCount = roots.Length;
        }
        else
        {
            targetCount = newParent.Children.Length;
        }
        if (insertionIndex < 0 || insertionIndex > targetCount)
        {
            throw new OdfEditException("The reparenting insertion index is outside the target.");
        }

        if (oldParent == null)
        {
            int oldRootIndex = FindRootIndex(node);
            if (oldRootIndex < 0)
            {
                throw new OdfEditException("The node is not an attached tree root.");
            }
            roots = RemoveNodeArray(roots, oldRootIndex);
        }
        else
        {
            int oldChildIndex = FindChildIndex(oldParent, node);
            if (oldChildIndex < 0)
            {
                throw new OdfEditException("The node parent does not own the node.");
            }
            oldParent.Children = RemoveNodeArray(
                oldParent.Children,
                oldChildIndex);
        }

        if (newParent == null)
        {
            node.Parent = null;
            roots = InsertNodeArray(roots, insertionIndex, node);
        }
        else
        {
            node.Parent = newParent;
            newParent.Children = InsertNodeArray(
                newParent.Children,
                insertionIndex,
                node);
            newParent.WasEmptyElement = false;
        }
        index.UpdateRoots(roots);
    }

    internal OdfNode CloneSubtree(OdfNode node)
    {
        if (node == null)
        {
            throw new OdfEditException("A node is required for cloning.");
        }
        HashSet<OdfNode> visited = new HashSet<OdfNode>();
        return CloneNodeCore(node, null, visited);
    }

    internal OdfNode[] TraverseAllNodes()
    {
        List<OdfNode> result = new List<OdfNode>();
        HashSet<OdfNode> visited = new HashSet<OdfNode>();
        foreach (OdfNode root in roots)
        {
            AddTraversal(root, result, visited);
        }
        return result.ToArray();
    }

    internal OdfNode[] TraverseSubtree(OdfNode node)
    {
        if (node == null)
        {
            throw new OdfEditException("A node is required for traversal.");
        }
        List<OdfNode> result = new List<OdfNode>();
        HashSet<OdfNode> visited = new HashSet<OdfNode>();
        AddTraversal(node, result, visited);
        return result.ToArray();
    }

    internal OdfNode[] SearchQualifiedName(OdfQualifiedName name)
    {
        return index.FindNodesByQualifiedName(name);
    }

    internal OdfNode[] SearchXmlId(string xmlId)
    {
        return index.FindNodesByXmlId(xmlId);
    }

    internal OdfNode[] SearchStyleName(string styleName)
    {
        return index.FindNodesByStyleName(styleName);
    }

    internal OdfNode[] SearchReferences(string target)
    {
        return index.FindNodesReferringTo(target);
    }

    internal OdfNode[] SearchText(string value)
    {
        if (value == null || value.Length == 0)
        {
            return [];
        }
        List<OdfNode> result = new List<OdfNode>();
        foreach (OdfNode node in TraverseAllNodes())
        {
            if (node.Kind == OdfNodeKind.Text
                && node.Value.IndexOf(value, StringComparison.Ordinal) >= 0)
            {
                result.Add(node);
            }
        }
        return result.ToArray();
    }

    internal OdfAttribute? FindAttribute(
        OdfNode node,
        OdfQualifiedName name)
    {
        RequireElement(node);
        if (!OdfNameRules.IsQualifiedNameValue(name))
        {
            throw new OdfXmlException("A valid qualified attribute name is required.");
        }
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (attribute.Name.NamespaceName == name.NamespaceName
                && attribute.Name.LocalName == name.LocalName)
            {
                return attribute;
            }
        }
        return null;
    }

    internal OdfAttribute SetAttribute(
        OdfNode node,
        OdfQualifiedName name,
        string value)
    {
        RequireElement(node);
        if (!OdfNameRules.IsQualifiedNameValue(name) || value == null)
        {
            throw new OdfXmlException("A valid qualified attribute name and value are required.");
        }
        OdfAttribute? existing = FindAttribute(node, name);
        OdfAttribute result;
        if (existing == null)
        {
            result = new OdfAttribute
            {
                Name = CopyQualifiedName(name),
                Value = value
            };
            OdfAttribute[] attributes = new OdfAttribute[node.Attributes.Length + 1];
            Array.Copy(node.Attributes, attributes, node.Attributes.Length);
            attributes[attributes.Length - 1] = result;
            node.Attributes = attributes;
        }
        else
        {
            existing.Name = CopyQualifiedName(name);
            existing.Value = value;
            result = existing;
        }
        if (name.NamespaceName == OdfNamespaces.Xml
            && name.LocalName == "id")
        {
            node.XmlId = value;
            node.Location.XmlId = value;
        }
        if (ContainsAttached(node))
        {
            index.Invalidate();
        }
        return result;
    }

    internal bool RemoveAttribute(
        OdfNode node,
        OdfQualifiedName name)
    {
        RequireElement(node);
        if (!OdfNameRules.IsQualifiedNameValue(name))
        {
            throw new OdfXmlException("A valid qualified attribute name is required.");
        }
        int found = -1;
        int position = 0;
        while (position < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[position];
            if (attribute.Name.NamespaceName == name.NamespaceName
                && attribute.Name.LocalName == name.LocalName)
            {
                found = position;
                break;
            }
            position++;
        }
        if (found < 0)
        {
            return false;
        }
        OdfAttribute[] result = new OdfAttribute[node.Attributes.Length - 1];
        int source = 0;
        int destination = 0;
        while (source < node.Attributes.Length)
        {
            if (source != found)
            {
                result[destination] = node.Attributes[source];
                destination++;
            }
            source++;
        }
        node.Attributes = result;
        if (name.NamespaceName == OdfNamespaces.Xml
            && name.LocalName == "id")
        {
            node.XmlId = "";
            node.Location.XmlId = "";
        }
        if (ContainsAttached(node))
        {
            index.Invalidate();
        }
        return true;
    }

    internal string GetTextContent(OdfNode node)
    {
        if (node == null)
        {
            throw new OdfEditException("A node is required for text extraction.");
        }
        StringBuilder result = new StringBuilder();
        HashSet<OdfNode> visited = new HashSet<OdfNode>();
        AppendText(node, result, visited);
        return result.ToString();
    }

    internal void SetTextContent(
        OdfNode node,
        string value)
    {
        if (node == null || value == null)
        {
            throw new OdfEditException("A node and text value are required.");
        }
        if (node.Kind == OdfNodeKind.Comment
            && (value.Contains("--", StringComparison.Ordinal)
                || value.EndsWith("-", StringComparison.Ordinal)))
        {
            throw new OdfXmlException("The XML comment content is invalid.");
        }
        if (node.Kind == OdfNodeKind.ProcessingInstruction
            && value.Contains("?>", StringComparison.Ordinal))
        {
            throw new OdfXmlException("The XML processing-instruction content is invalid.");
        }
        if (node.Kind == OdfNodeKind.Element)
        {
            foreach (OdfNode child in node.Children)
            {
                child.Parent = null;
            }
            if (value.Length == 0)
            {
                node.Children = [];
            }
            else
            {
                OdfNode text = CreateText(value);
                text.Parent = node;
                node.Children = [text];
            }
            node.WasEmptyElement = false;
        }
        else if (node.Kind == OdfNodeKind.Text
            || node.Kind == OdfNodeKind.Comment
            || node.Kind == OdfNodeKind.ProcessingInstruction)
        {
            node.Value = value;
        }
        else
        {
            throw new OdfEditException("The node kind cannot carry text content.");
        }
        if (ContainsAttached(node))
        {
            index.Invalidate();
        }
    }

    static OdfQualifiedName CopyQualifiedName(OdfQualifiedName source)
    {
        return new OdfQualifiedName
        {
            NamespaceName = source.NamespaceName,
            LocalName = source.LocalName,
            Prefix = source.Prefix
        };
    }

    void ValidateAttachable(
        OdfNode node,
        OdfNode? prohibitedNode)
    {
        if (node == null || node.Parent != null)
        {
            throw new OdfEditException("Only an unattached subtree can be inserted.");
        }
        HashSet<OdfNode> visited = new HashSet<OdfNode>();
        ValidateDetachedSubtree(node, prohibitedNode, visited);
    }

    void ValidateDetachedSubtree(
        OdfNode node,
        OdfNode? prohibitedNode,
        HashSet<OdfNode> visited)
    {
        if (node == prohibitedNode)
        {
            throw new OdfEditException("The insertion would create an XML tree cycle.");
        }
        if (visited.Contains(node))
        {
            throw new OdfEditException("The inserted subtree contains a cycle or shared node.");
        }
        if (ContainsAttached(node))
        {
            throw new OdfEditException("The inserted subtree already belongs to this tree.");
        }
        visited.Add(node);
        foreach (OdfNode child in node.Children)
        {
            ValidateDetachedSubtree(child, prohibitedNode, visited);
        }
    }

    static void LinkParents(
        OdfNode node,
        OdfNode? parent)
    {
        node.Parent = parent;
        foreach (OdfNode child in node.Children)
        {
            LinkParents(child, node);
        }
    }

    bool ContainsAttached(OdfNode searched)
    {
        HashSet<OdfNode> visited = new HashSet<OdfNode>();
        foreach (OdfNode root in roots)
        {
            if (ContainsIdentity(root, searched, visited))
            {
                return true;
            }
        }
        return false;
    }

    static bool ContainsIdentity(
        OdfNode node,
        OdfNode searched,
        HashSet<OdfNode> visited)
    {
        if (node == searched)
        {
            return true;
        }
        if (visited.Contains(node))
        {
            return false;
        }
        visited.Add(node);
        foreach (OdfNode child in node.Children)
        {
            if (ContainsIdentity(child, searched, visited))
            {
                return true;
            }
        }
        return false;
    }

    static bool ContainsSubtree(
        OdfNode root,
        OdfNode searched)
    {
        HashSet<OdfNode> visited = new HashSet<OdfNode>();
        return ContainsIdentity(root, searched, visited);
    }

    void RequireAttached(OdfNode node)
    {
        if (node == null || !ContainsAttached(node))
        {
            throw new OdfEditException("The node is not attached to this XML tree.");
        }
    }

    static void RequireElement(OdfNode node)
    {
        if (node == null || node.Kind != OdfNodeKind.Element)
        {
            throw new OdfEditException("The operation requires an XML element.");
        }
    }

    int FindRootIndex(OdfNode node)
    {
        int position = 0;
        while (position < roots.Length)
        {
            if (roots[position] == node)
            {
                return position;
            }
            position++;
        }
        return -1;
    }

    static int FindChildIndex(
        OdfNode parent,
        OdfNode child)
    {
        int position = 0;
        while (position < parent.Children.Length)
        {
            if (parent.Children[position] == child)
            {
                return position;
            }
            position++;
        }
        return -1;
    }

    static OdfNode[] InsertNodeArray(
        OdfNode[] source,
        int insertionIndex,
        OdfNode node)
    {
        OdfNode[] result = new OdfNode[source.Length + 1];
        int sourceIndex = 0;
        int destination = 0;
        while (destination < result.Length)
        {
            if (destination == insertionIndex)
            {
                result[destination] = node;
            }
            else
            {
                result[destination] = source[sourceIndex];
                sourceIndex++;
            }
            destination++;
        }
        return result;
    }

    static OdfNode[] RemoveNodeArray(
        OdfNode[] source,
        int removalIndex)
    {
        OdfNode[] result = new OdfNode[source.Length - 1];
        int sourceIndex = 0;
        int destination = 0;
        while (sourceIndex < source.Length)
        {
            if (sourceIndex != removalIndex)
            {
                result[destination] = source[sourceIndex];
                destination++;
            }
            sourceIndex++;
        }
        return result;
    }

    static OdfNode[] ReplaceNodeArray(
        OdfNode[] source,
        int replacementIndex,
        OdfNode node)
    {
        OdfNode[] result = new OdfNode[source.Length];
        Array.Copy(source, result, source.Length);
        result[replacementIndex] = node;
        return result;
    }

    static OdfNode CloneNodeCore(
        OdfNode source,
        OdfNode? parent,
        HashSet<OdfNode> visited)
    {
        if (visited.Contains(source))
        {
            throw new OdfEditException("The cloned subtree contains a cycle or shared node.");
        }
        visited.Add(source);
        OdfNode result = new OdfNode
        {
            Kind = source.Kind,
            Parent = parent,
            Value = source.Value,
            XmlId = source.XmlId,
            IsSignificantWhitespace = source.IsSignificantWhitespace,
            WasEmptyElement = source.WasEmptyElement
        };
        if (source.Name != null)
        {
            result.Name = CopyQualifiedName(source.Name);
        }
        OdfNamespaceDeclaration[] declarations = new OdfNamespaceDeclaration[
            source.NamespaceDeclarations.Length];
        int declarationIndex = 0;
        while (declarationIndex < declarations.Length)
        {
            OdfNamespaceDeclaration sourceDeclaration = source.NamespaceDeclarations[
                declarationIndex];
            declarations[declarationIndex] = new OdfNamespaceDeclaration
            {
                Prefix = sourceDeclaration.Prefix,
                NamespaceName = sourceDeclaration.NamespaceName
            };
            declarationIndex++;
        }
        result.NamespaceDeclarations = declarations;
        OdfAttribute[] attributes = new OdfAttribute[source.Attributes.Length];
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            OdfAttribute sourceAttribute = source.Attributes[attributeIndex];
            attributes[attributeIndex] = new OdfAttribute
            {
                Name = CopyQualifiedName(sourceAttribute.Name),
                Value = sourceAttribute.Value
            };
            attributeIndex++;
        }
        result.Attributes = attributes;
        OdfNode[] children = new OdfNode[source.Children.Length];
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            children[childIndex] = CloneNodeCore(
                source.Children[childIndex],
                result,
                visited);
            childIndex++;
        }
        result.Children = children;
        return result;
    }

    static void AddTraversal(
        OdfNode node,
        List<OdfNode> result,
        HashSet<OdfNode> visited)
    {
        if (visited.Contains(node))
        {
            throw new OdfEditException("The XML tree contains a cycle or shared node.");
        }
        visited.Add(node);
        result.Add(node);
        foreach (OdfNode child in node.Children)
        {
            AddTraversal(child, result, visited);
        }
    }

    static void AppendText(
        OdfNode node,
        StringBuilder result,
        HashSet<OdfNode> visited)
    {
        if (visited.Contains(node))
        {
            throw new OdfEditException("The XML tree contains a cycle or shared node.");
        }
        visited.Add(node);
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
            return;
        }
        if (node.Kind != OdfNodeKind.Element)
        {
            return;
        }
        foreach (OdfNode child in node.Children)
        {
            AppendText(child, result, visited);
        }
    }
}
