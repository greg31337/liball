using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class XdmNodeAccessor
{
    internal TreeNavigation Navigation;
    internal XdmSequenceOperations SequenceOperations;
    internal Dictionary<XmlNode, XdmNode> Wrappers;
    internal Dictionary<XmlNode, XdmNamespaceCacheEntry> NamespaceNodes;
    internal Dictionary<XmlNode, int> TreeOrder;
    internal int NextTreeOrder;

    internal XdmNodeAccessor()
    {
        Navigation = new TreeNavigation();
        SequenceOperations = new XdmSequenceOperations();
        Wrappers = new Dictionary<XmlNode, XdmNode>();
        NamespaceNodes = new Dictionary<XmlNode, XdmNamespaceCacheEntry>();
        TreeOrder = new Dictionary<XmlNode, int>();
        NextTreeOrder = 0;
    }

    internal XdmNode WrapXmlNode(XmlNode node)
    {
        Guard.NotNull(node, "node");
        if (Wrappers.ContainsKey(node))
        {
            return Wrappers[node];
        }

        XdmNodeKind kind = MapXdmNodeKind(node.Kind);
        XdmNode result = new XdmNode(node, kind);
        Wrappers[node] = result;
        RegisterXdmTree(node);
        return result;
    }

    internal XdmNodeKind GetXdmNodeKind(XdmNode node)
    {
        Guard.NotNull(node, "node");
        return node.Kind;
    }

    internal bool HasSameXdmNodeIdentity(XdmNode left, XdmNode right)
    {
        Guard.NotNull(left, "left");
        Guard.NotNull(right, "right");
        return left.Node == right.Node;
    }

    internal int CompareXdmNodeDocumentOrder(XdmNode left, XdmNode right)
    {
        Guard.NotNull(left, "left");
        Guard.NotNull(right, "right");
        if (HasSameXdmNodeIdentity(left, right))
        {
            return 0;
        }

        XmlNode leftRoot = Navigation.FindXmlNavigationRoot(left.Node);
        XmlNode rightRoot = Navigation.FindXmlNavigationRoot(right.Node);
        if (leftRoot == rightRoot)
        {
            List<XdmNode> order = new List<XdmNode>();
            AddXdmDocumentOrder(order, WrapXmlNode(leftRoot), new HashSet<XmlNode>());
            int leftIndex = IndexXdmDocumentOrder(order, left);
            int rightIndex = IndexXdmDocumentOrder(order, right);
            if (leftIndex >= 0 && rightIndex >= 0)
            {
                return leftIndex < rightIndex ? -1 : 1;
            }
        }

        int leftTree = GetXdmTreeOrder(leftRoot);
        int rightTree = GetXdmTreeOrder(rightRoot);
        return leftTree < rightTree ? -1 : 1;
    }

    internal string GetXdmNodeStringValue(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.HasProjectedStringValue)
        {
            return node.ProjectedStringValue == null ? "" : node.ProjectedStringValue;
        }

        XmlSchemaTypeInfo? annotation = GetXdmNodeTypeAnnotation(node);
        if (annotation != null && annotation.NormalizedValue != null && (node.Kind == XdmNodeKind.Element || node.Kind == XdmNodeKind.Attribute))
        {
            return annotation.NormalizedValue;
        }

        return Navigation.GetXmlNodeStringValueOperation(node.Node);
    }

    internal XdmSequence GetXdmNodeTypedValue(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind == XdmNodeKind.Text && node.HasProjectedStringValue)
        {
            XdmAtomicValue value = new XdmTypeConverter().CreateXdmUntypedAtomicValue(GetXdmNodeStringValue(node));
            return SequenceOperations.CreateXdmAtomicSequence(value);
        }

        return new XdmTypeConverter().DeriveXdmNodeTypedValue(node.Node);
    }

    internal XmlName? GetXdmNodeName(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind == XdmNodeKind.Element || node.Kind == XdmNodeKind.Attribute)
        {
            return CopyXdmName(node.Node.Name);
        }

        if (node.Kind == XdmNodeKind.ProcessingInstruction)
        {
            string target = node.Node.Name == null || node.Node.Name.LocalName == null ? "" : node.Node.Name.LocalName;
            return CreateXdmName("", target, "", target);
        }

        if (node.Kind == XdmNodeKind.Namespace)
        {
            string prefix = node.Node.Name == null || node.Node.Name.LocalName == null ? "" : node.Node.Name.LocalName;
            if (prefix.Length == 0)
            {
                return null;
            }

            return CreateXdmName("", prefix, "", prefix);
        }

        return null;
    }

    internal XmlName? GetXdmNodeTypeName(XdmNode node)
    {
        Guard.NotNull(node, "node");
        XmlSchemaTypeInfo? annotation = GetXdmNodeTypeAnnotation(node);
        if (annotation != null && annotation.TypeName != null && (node.Kind == XdmNodeKind.Element || node.Kind == XdmNodeKind.Attribute))
        {
            return CopyXdmName(annotation.TypeName);
        }

        if (node.Kind == XdmNodeKind.Element)
        {
            return CreateXdmSchemaName("untyped");
        }

        if (node.Kind == XdmNodeKind.Attribute || node.Kind == XdmNodeKind.Text)
        {
            return CreateXdmSchemaName("untypedAtomic");
        }

        return null;
    }

    internal XmlSchemaTypeInfo? GetXdmNodeTypeAnnotation(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind == XdmNodeKind.Element || node.Kind == XdmNodeKind.Attribute)
        {
            return node.Node.TypeInfo;
        }

        return null;
    }

    internal XdmSequence GetXdmNodeNilled(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind != XdmNodeKind.Element)
        {
            return SequenceOperations.CreateEmptyXdmSequence();
        }

        XmlSchemaTypeInfo? annotation = GetXdmNodeTypeAnnotation(node);
        bool nilled = annotation != null && annotation.IsNilled;
        XdmAtomicValue value = new XdmAtomicValue();
        value.Type = XdmAtomicType.Boolean;
        value.BooleanValue = nilled;
        value.LexicalValue = nilled ? "true" : "false";
        value.CanonicalValue = value.LexicalValue;
        value.StringValue = value.LexicalValue;
        value.IsImmutable = true;
        return SequenceOperations.CreateXdmAtomicSequence(value);
    }

    internal XdmSequence GetXdmNodeIsId(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind != XdmNodeKind.Element && node.Kind != XdmNodeKind.Attribute)
        {
            return SequenceOperations.CreateEmptyXdmSequence();
        }

        XmlSchemaTypeInfo? annotation = GetXdmNodeTypeAnnotation(node);
        return CreateXdmBooleanSequence(annotation != null && annotation.IsId);
    }

    internal XdmSequence GetXdmNodeIsIdReference(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind != XdmNodeKind.Element && node.Kind != XdmNodeKind.Attribute)
        {
            return SequenceOperations.CreateEmptyXdmSequence();
        }

        XmlSchemaTypeInfo? annotation = GetXdmNodeTypeAnnotation(node);
        return CreateXdmBooleanSequence(annotation != null && annotation.IsIdReference);
    }

    internal string? GetXdmNodeBaseUri(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind == XdmNodeKind.Namespace)
        {
            return null;
        }

        if (node.Kind == XdmNodeKind.Attribute || node.Kind == XdmNodeKind.Comment || node.Kind == XdmNodeKind.Text)
        {
            XdmNode? parent = GetXdmNodeParent(node);
            return parent == null ? null : GetXdmNodeBaseUri(parent);
        }

        return Navigation.GetXmlNodeBaseUriOperation(node.Node);
    }

    internal string? GetXdmNodeDocumentUri(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind != XdmNodeKind.Document || node.Node.OwnerDocument == null)
        {
            return null;
        }

        return node.Node.OwnerDocument.DocumentUri;
    }

    internal XdmNode? GetXdmNodeParent(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.HasProjectedParent)
        {
            return node.ProjectedParent;
        }

        XmlNode? parent = node.Node.Parent;
        while (parent != null && (parent.Kind == XmlNodeKind.EntityReference || parent.Kind == XmlNodeKind.DocumentType))
        {
            parent = parent.Parent;
        }

        return parent == null ? null : WrapXmlNode(parent);
    }

    internal XdmNode[] GetXdmNodeChildren(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind != XdmNodeKind.Document && node.Kind != XdmNodeKind.Element)
        {
            return [];
        }

        List<XmlNode> projected = new List<XmlNode>();
        CollectXdmChildNodes(projected, node.Node.Children == null ? [] : node.Node.Children);
        List<XdmNode> result = new List<XdmNode>();
        int index = 0;
        while (index < projected.Count)
        {
            XmlNode current = projected[index];
            if (current.Kind == XmlNodeKind.Text || current.Kind == XmlNodeKind.CData)
            {
                StringBuilder value = new StringBuilder();
                XmlNode representative = current;
                while (index < projected.Count && (projected[index].Kind == XmlNodeKind.Text || projected[index].Kind == XmlNodeKind.CData))
                {
                    value.Append(projected[index].Value == null ? "" : projected[index].Value);
                    index = index + 1;
                }

                if (value.Length != 0)
                {
                    XdmNode text = WrapXmlNode(representative);
                    text.ProjectedParent = node;
                    text.HasProjectedParent = true;
                    text.ProjectedStringValue = value.ToString();
                    text.HasProjectedStringValue = true;
                    result.Add(text);
                }
            }
            else
            {
                XdmNode child = WrapXmlNode(current);
                child.ProjectedParent = node;
                child.HasProjectedParent = true;
                result.Add(child);
                index = index + 1;
            }
        }

        return result.ToArray();
    }

    internal XdmNode[] GetXdmNodeAttributes(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind != XdmNodeKind.Element)
        {
            return [];
        }

        List<XdmNode> result = new List<XdmNode>();
        XmlNode[] attributes = node.Node.Attributes == null ? [] : node.Node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            if (attribute.Kind != XmlNodeKind.Attribute)
            {
                throw new ArgumentException("An element attribute collection contains a non-attribute node.", "node");
            }

            if (!IsXdmNamespaceDeclaration(attribute))
            {
                XdmNode wrapped = WrapXmlNode(attribute);
                wrapped.ProjectedParent = node;
                wrapped.HasProjectedParent = true;
                result.Add(wrapped);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XdmNode[] GetXdmNodeNamespaces(XdmNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind != XdmNodeKind.Element)
        {
            return [];
        }

        XmlDocument? document = node.Node.OwnerDocument;
        long mutationVersion = document == null ? node.Node.MutationVersion : document.MutationVersion;
        if (NamespaceNodes.ContainsKey(node.Node))
        {
            XdmNamespaceCacheEntry cached = NamespaceNodes[node.Node];
            if (cached.Document == document && cached.MutationVersion == mutationVersion)
            {
                return CopyXdmNodes(cached.Nodes);
            }
        }

        XmlNamespaceBinding[] bindings = Navigation.SelectXmlNamespacesOperation(node.Node);
        XdmNode[] result = new XdmNode[bindings.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            XmlNamespaceBinding binding = bindings[index];
            string prefix = binding.Prefix == null ? "" : binding.Prefix;
            string uri = binding.NamespaceUri == null ? "" : binding.NamespaceUri;
            XmlNode namespaceNode = new XmlNode();
            namespaceNode.Kind = XmlNodeKind.Namespace;
            namespaceNode.Name = CreateXdmName("", prefix, "", prefix);
            namespaceNode.Value = uri;
            namespaceNode.Parent = node.Node;
            namespaceNode.OwnerDocument = document;
            namespaceNode.MutationVersion = mutationVersion;
            XdmNode wrapped = WrapXmlNode(namespaceNode);
            wrapped.ProjectedParent = node;
            wrapped.HasProjectedParent = true;
            result[index] = wrapped;
            index = index + 1;
        }

        NamespaceNodes[node.Node] = new XdmNamespaceCacheEntry(document, mutationVersion, result);
        return CopyXdmNodes(result);
    }

    internal XdmNodeKind MapXdmNodeKind(XmlNodeKind kind)
    {
        if (kind == XmlNodeKind.Document || kind == XmlNodeKind.DocumentFragment)
        {
            return XdmNodeKind.Document;
        }

        if (kind == XmlNodeKind.Element)
        {
            return XdmNodeKind.Element;
        }

        if (kind == XmlNodeKind.Attribute)
        {
            return XdmNodeKind.Attribute;
        }

        if (kind == XmlNodeKind.Namespace)
        {
            return XdmNodeKind.Namespace;
        }

        if (kind == XmlNodeKind.ProcessingInstruction)
        {
            return XdmNodeKind.ProcessingInstruction;
        }

        if (kind == XmlNodeKind.Comment)
        {
            return XdmNodeKind.Comment;
        }

        if (kind == XmlNodeKind.Text || kind == XmlNodeKind.CData)
        {
            return XdmNodeKind.Text;
        }

        throw new ArgumentException("The XML node kind has no corresponding XDM node kind.", "kind");
    }

    internal void CollectXdmChildNodes(List<XmlNode> result, XmlNode[] nodes)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            XmlNode child = nodes[index];
            if (child.Kind == XmlNodeKind.EntityReference)
            {
                CollectXdmChildNodes(result, child.Children == null ? [] : child.Children);
            }
            else if (child.Kind != XmlNodeKind.DocumentType)
            {
                if (child.Kind != XmlNodeKind.Element && child.Kind != XmlNodeKind.ProcessingInstruction && child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.Text && child.Kind != XmlNodeKind.CData)
                {
                    throw new ArgumentException("An XML child cannot be projected into the XDM children property.", "nodes");
                }

                result.Add(child);
            }

            index = index + 1;
        }
    }

    internal void AddXdmDocumentOrder(List<XdmNode> result, XdmNode node, HashSet<XmlNode> visited)
    {
        if (!visited.Add(node.Node))
        {
            throw new ArgumentException("The XML tree contains a cycle.", "node");
        }

        result.Add(node);
        if (node.Kind == XdmNodeKind.Element)
        {
            XdmNode[] namespaces = GetXdmNodeNamespaces(node);
            int namespaceIndex = 0;
            while (namespaceIndex < namespaces.Length)
            {
                result.Add(namespaces[namespaceIndex]);
                namespaceIndex = namespaceIndex + 1;
            }

            XdmNode[] attributes = GetXdmNodeAttributes(node);
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                result.Add(attributes[attributeIndex]);
                attributeIndex = attributeIndex + 1;
            }
        }

        XdmNode[] children = GetXdmNodeChildren(node);
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            AddXdmDocumentOrder(result, children[childIndex], visited);
            childIndex = childIndex + 1;
        }
    }

    internal int IndexXdmDocumentOrder(List<XdmNode> nodes, XdmNode node)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (HasSameXdmNodeIdentity(nodes[index], node))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool IsXdmNamespaceDeclaration(XmlNode attribute)
    {
        XmlName? name = attribute.Name;
        return name != null && string.Equals(name.NamespaceUri, XmlNamespaces.Xmlns, StringComparison.Ordinal);
    }

    internal XmlName? CopyXdmName(XmlName? source)
    {
        if (source == null)
        {
            return null;
        }

        return CreateXdmName(source.Prefix, source.LocalName, source.NamespaceUri, source.QualifiedName);
    }

    internal XmlName CreateXdmName(string? prefix, string? localName, string? namespaceUri, string? qualifiedName)
    {
        XmlName result = new XmlName();
        result.Prefix = prefix;
        result.LocalName = localName;
        result.NamespaceUri = namespaceUri;
        result.QualifiedName = qualifiedName;
        return result;
    }

    internal XmlName CreateXdmSchemaName(string localName)
    {
        return CreateXdmName("xs", localName, XPathNamespaces.XmlSchema, "xs:" + localName);
    }

    internal XdmSequence CreateXdmBooleanSequence(bool boolean)
    {
        XdmAtomicValue value = new XdmAtomicValue();
        value.Type = XdmAtomicType.Boolean;
        value.BooleanValue = boolean;
        value.LexicalValue = boolean ? "true" : "false";
        value.CanonicalValue = value.LexicalValue;
        value.StringValue = value.LexicalValue;
        value.IsImmutable = true;
        return SequenceOperations.CreateXdmAtomicSequence(value);
    }

    internal XdmNode[] CopyXdmNodes(XdmNode[] nodes)
    {
        XdmNode[] result = new XdmNode[nodes.Length];
        Array.Copy(nodes, result, nodes.Length);
        return result;
    }

    internal void RegisterXdmTree(XmlNode node)
    {
        XmlNode root = Navigation.FindXmlNavigationRoot(node);
        if (!TreeOrder.ContainsKey(root))
        {
            TreeOrder[root] = NextTreeOrder;
            NextTreeOrder = NextTreeOrder + 1;
        }
    }

    internal int GetXdmTreeOrder(XmlNode root)
    {
        if (!TreeOrder.ContainsKey(root))
        {
            TreeOrder[root] = NextTreeOrder;
            NextTreeOrder = NextTreeOrder + 1;
        }

        return TreeOrder[root];
    }
}
