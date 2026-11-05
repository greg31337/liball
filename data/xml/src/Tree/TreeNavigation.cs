using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class TreeContextCacheEntry
{
    internal XmlDocument? Document;
    internal long MutationVersion;
    internal string? BaseUri;
    internal string? Language;
    internal XmlSpaceMode SpaceMode;
    internal bool BaseUriComputed;
    internal bool LanguageComputed;
    internal bool SpaceModeComputed;

    internal TreeContextCacheEntry(XmlDocument? document, long mutationVersion)
    {
        Document = document;
        MutationVersion = mutationVersion;
        BaseUri = null;
        Language = null;
        SpaceMode = XmlSpaceMode.Default;
        BaseUriComputed = false;
        LanguageComputed = false;
        SpaceModeComputed = false;
    }
}

internal class TreeNavigation
{
    internal TreeInvariants Invariants;
    internal TreeRangeNavigation RangeNavigation;
    internal Dictionary<XmlNode, TreeContextCacheEntry> ContextCache;
    internal Dictionary<XmlDocument, IdIndex> IdentifierIndexes;

    internal TreeNavigation()
    {
        Invariants = new TreeInvariants();
        RangeNavigation = new TreeRangeNavigation(this);
        ContextCache = new Dictionary<XmlNode, TreeContextCacheEntry>();
        IdentifierIndexes = new Dictionary<XmlDocument, IdIndex>();
    }

    internal XmlNode? FindXmlParentOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        return node.Parent;
    }

    internal XmlNodeSelection SelectXmlChildrenOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        XmlNode[] children = node.Children == null ? [] : node.Children;
        return CreateXmlNavigationSelection(node.OwnerDocument, CopyXmlNavigationNodes(children), null, false);
    }

    internal XmlNodeSelection SelectXmlAncestorsOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        List<XmlNode> ancestors = new List<XmlNode>();
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        XmlNode? current = node.Parent;
        while (current != null)
        {
            if (!visited.Add(current))
            {
                throw new ArgumentException("The node parent chain contains a cycle.", "node");
            }

            ancestors.Insert(0, current);
            current = current.Parent;
        }

        return CreateXmlNavigationSelection(node.OwnerDocument, ancestors.ToArray(), null, false);
    }

    internal XmlNodeSelection SelectXmlDescendantsOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        List<XmlNode> descendants = new List<XmlNode>();
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            AddXmlNavigationSubtree(descendants, children[index], false);
            index = index + 1;
        }

        return CreateXmlNavigationSelection(node.OwnerDocument, descendants.ToArray(), null, false);
    }

    internal XmlNodeSelection SelectXmlFollowingSiblingsOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        XmlNode? parent = node.Parent;
        if (parent == null || node.Kind == XmlNodeKind.Attribute || node.Kind == XmlNodeKind.Namespace)
        {
            return CreateXmlNavigationSelection(node.OwnerDocument, [], null, false);
        }

        XmlNode[] siblings = parent.Children == null ? [] : parent.Children;
        int nodeIndex = IndexXmlNavigationNode(siblings, node);
        if (nodeIndex < 0)
        {
            throw new ArgumentException("The node parent link does not match its parent collection.", "node");
        }

        XmlNode[] result = new XmlNode[siblings.Length - nodeIndex - 1];
        if (result.Length > 0)
        {
            Array.Copy(siblings, nodeIndex + 1, result, 0, result.Length);
        }

        return CreateXmlNavigationSelection(node.OwnerDocument, result, null, false);
    }

    internal XmlNodeSelection SelectXmlPrecedingSiblingsOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        XmlNode? parent = node.Parent;
        if (parent == null || node.Kind == XmlNodeKind.Attribute || node.Kind == XmlNodeKind.Namespace)
        {
            return CreateXmlNavigationSelection(node.OwnerDocument, [], null, false);
        }

        XmlNode[] siblings = parent.Children == null ? [] : parent.Children;
        int nodeIndex = IndexXmlNavigationNode(siblings, node);
        if (nodeIndex < 0)
        {
            throw new ArgumentException("The node parent link does not match its parent collection.", "node");
        }

        XmlNode[] result = new XmlNode[nodeIndex];
        if (result.Length > 0)
        {
            Array.Copy(siblings, result, result.Length);
        }

        return CreateXmlNavigationSelection(node.OwnerDocument, result, null, false);
    }

    internal XmlNodeSelection SelectXmlFollowingOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        XmlNode anchor = XmlNavigationAxisAnchor(node);
        XmlNode root = FindXmlNavigationRoot(anchor);
        List<XmlNode> order = new List<XmlNode>();
        AddXmlNavigationSubtree(order, root, false);
        int anchorIndex = IndexXmlNavigationList(order, anchor);
        if (anchorIndex < 0)
        {
            throw new ArgumentException("The node cannot be located in its tree.", "node");
        }

        List<XmlNode> result = new List<XmlNode>();
        int index = anchorIndex + 1;
        while (index < order.Count)
        {
            if (node.Kind == XmlNodeKind.Attribute || !IsXmlNavigationDescendant(order[index], node))
            {
                result.Add(order[index]);
            }

            index = index + 1;
        }

        return CreateXmlNavigationSelection(node.OwnerDocument, result.ToArray(), null, false);
    }

    internal XmlNodeSelection SelectXmlPrecedingOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        XmlNode anchor = XmlNavigationAxisAnchor(node);
        XmlNode root = FindXmlNavigationRoot(anchor);
        List<XmlNode> order = new List<XmlNode>();
        AddXmlNavigationSubtree(order, root, false);
        int anchorIndex = IndexXmlNavigationList(order, anchor);
        if (anchorIndex < 0)
        {
            throw new ArgumentException("The node cannot be located in its tree.", "node");
        }

        List<XmlNode> result = new List<XmlNode>();
        int index = 0;
        while (index < anchorIndex)
        {
            if (!IsXmlNavigationAncestor(order[index], anchor))
            {
                result.Add(order[index]);
            }

            index = index + 1;
        }

        return CreateXmlNavigationSelection(node.OwnerDocument, result.ToArray(), null, false);
    }

    internal XmlNodeSelection SelectXmlAttributesOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        XmlNode[] attributes = node.Kind == XmlNodeKind.Element && node.Attributes != null ? node.Attributes : [];
        return CreateXmlNavigationSelection(node.OwnerDocument, CopyXmlNavigationNodes(attributes), null, false);
    }

    internal XmlNamespaceBinding[] SelectXmlNamespacesOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        XmlNode? element = FindXmlNavigationContextElement(node);
        if (element == null)
        {
            return [];
        }

        List<XmlNamespaceBinding> result = new List<XmlNamespaceBinding>();
        HashSet<string> hidden = new HashSet<string>(StringComparer.Ordinal);
        XmlNode? current = element;
        while (current != null)
        {
            if (current.Kind == XmlNodeKind.Element)
            {
                XmlNamespaceBinding[] bindings = current.NamespaceBindings == null ? [] : current.NamespaceBindings;
                int index = 0;
                while (index < bindings.Length)
                {
                    XmlNamespaceBinding binding = bindings[index];
                    string prefix = XmlNavigationNamespacePrefix(binding);
                    string? namespaceUri = binding.NamespaceUri;
                    if (hidden.Add(prefix) && !binding.IsUndeclaration && namespaceUri != null && namespaceUri.Length != 0)
                    {
                        result.Add(CopyXmlNavigationBinding(binding));
                    }

                    index = index + 1;
                }
            }

            current = current.Parent;
        }

        if (hidden.Add("xml"))
        {
            XmlNamespaceBinding xml = new XmlNamespaceBinding();
            xml.Prefix = "xml";
            xml.NamespaceUri = XmlNamespaces.Xml;
            xml.IsUndeclaration = false;
            xml.Range = null;
            result.Add(xml);
        }

        return result.ToArray();
    }

    internal XmlNodeSelection SelectXmlRangeOperation(XmlRange range)
    {
        return RangeNavigation.SelectXmlRangeNodes(range);
    }

    internal XmlNode? FindXmlNodeByIdOperation(XmlDocument document, string identifier)
    {
        Guard.NotNull(document, "document");
        Guard.NotNull(identifier, "identifier");
        if (document.DocumentNode == null || identifier.Length == 0)
        {
            return null;
        }

        IdIndex index;
        if (!IdentifierIndexes.ContainsKey(document) || IdentifierIndexes[document].MutationVersion != document.MutationVersion)
        {
            index = new IdIndex();
            index.BuildXmlIdentifierIndex(document);
            IdentifierIndexes[document] = index;
        }
        else
        {
            index = IdentifierIndexes[document];
        }

        return index.FindIndexedXmlNode(identifier);
    }

    internal string GetXmlNodeStringValueOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        if (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment || node.Kind == XmlNodeKind.Element || node.Kind == XmlNodeKind.EntityReference)
        {
            StringBuilder value = new StringBuilder();
            AppendXmlNavigationStringValue(value, node);
            return value.ToString();
        }

        if (node.Kind == XmlNodeKind.DocumentType)
        {
            return "";
        }

        return node.Value == null ? "" : node.Value;
    }

    internal string? GetXmlNodeBaseUriOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        TreeContextCacheEntry entry = FindXmlNavigationContextCacheEntry(node);
        if (entry.BaseUriComputed)
        {
            return entry.BaseUri;
        }

        XmlDocument? document = node.OwnerDocument;
        string? currentBase = null;
        if (document != null)
        {
            currentBase = document.BaseUri == null ? document.DocumentUri : document.BaseUri;
        }

        List<XmlNode> elements = XmlNavigationElementAncestors(node);
        int index = 0;
        while (index < elements.Count)
        {
            string? value = FindXmlNavigationContextAttribute(elements[index], "base");
            if (value != null)
            {
                currentBase = ResolveXmlNavigationBase(currentBase, value);
            }

            index = index + 1;
        }

        entry.BaseUri = currentBase;
        entry.BaseUriComputed = true;
        return currentBase;
    }

    internal string? GetXmlNodeLanguageOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        TreeContextCacheEntry entry = FindXmlNavigationContextCacheEntry(node);
        if (entry.LanguageComputed)
        {
            return entry.Language;
        }

        List<XmlNode> elements = XmlNavigationElementAncestors(node);
        int index = elements.Count - 1;
        while (index >= 0)
        {
            string? value = FindXmlNavigationContextAttribute(elements[index], "lang");
            if (value != null)
            {
                entry.Language = value.Length == 0 ? null : value;
                entry.LanguageComputed = true;
                return entry.Language;
            }

            index = index - 1;
        }

        entry.Language = null;
        entry.LanguageComputed = true;
        return null;
    }

    internal XmlSpaceMode GetXmlNodeSpaceModeOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        TreeContextCacheEntry entry = FindXmlNavigationContextCacheEntry(node);
        if (entry.SpaceModeComputed)
        {
            return entry.SpaceMode;
        }

        List<XmlNode> elements = XmlNavigationElementAncestors(node);
        int index = elements.Count - 1;
        while (index >= 0)
        {
            string? value = FindXmlNavigationContextAttribute(elements[index], "space");
            if (string.Equals(value, "preserve", StringComparison.Ordinal))
            {
                entry.SpaceMode = XmlSpaceMode.Preserve;
                entry.SpaceModeComputed = true;
                return entry.SpaceMode;
            }

            if (string.Equals(value, "default", StringComparison.Ordinal))
            {
                entry.SpaceMode = XmlSpaceMode.Default;
                entry.SpaceModeComputed = true;
                return entry.SpaceMode;
            }

            index = index - 1;
        }

        entry.SpaceMode = XmlSpaceMode.Default;
        entry.SpaceModeComputed = true;
        return entry.SpaceMode;
    }

    internal TreeContextCacheEntry FindXmlNavigationContextCacheEntry(XmlNode node)
    {
        XmlDocument? document = node.OwnerDocument;
        long mutationVersion = document == null ? node.MutationVersion : document.MutationVersion;
        if (ContextCache.ContainsKey(node))
        {
            TreeContextCacheEntry cached = ContextCache[node];
            if (cached.Document == document && cached.MutationVersion == mutationVersion)
            {
                return cached;
            }
        }

        TreeContextCacheEntry entry = new TreeContextCacheEntry(document, mutationVersion);
        ContextCache[node] = entry;
        return entry;
    }

    internal int CompareXmlNodeOrderOperation(XmlNode left, XmlNode right)
    {
        Guard.NotNull(left, "left");
        Guard.NotNull(right, "right");
        if (left == right)
        {
            return 0;
        }

        XmlDocument? document = left.OwnerDocument;
        if (document == null || right.OwnerDocument != document)
        {
            throw new ArgumentException("Document order requires nodes from the same owner document.", "right");
        }

        XmlNode leftRoot = FindXmlNavigationRoot(left);
        XmlNode rightRoot = FindXmlNavigationRoot(right);
        if (leftRoot == rightRoot)
        {
            List<XmlNode> order = new List<XmlNode>();
            AddXmlNavigationSubtree(order, leftRoot, true);
            int leftIndex = IndexXmlNavigationList(order, left);
            int rightIndex = IndexXmlNavigationList(order, right);
            if (leftIndex >= 0 && rightIndex >= 0)
            {
                return leftIndex < rightIndex ? -1 : 1;
            }
        }

        if (left.Identity == right.Identity)
        {
            throw new ArgumentException("Disconnected nodes require distinct document identities.", "right");
        }

        return left.Identity < right.Identity ? -1 : 1;
    }

    internal bool ContainsXmlNodeIdentityOperation(XmlNode left, XmlNode right)
    {
        Guard.NotNull(left, "left");
        Guard.NotNull(right, "right");
        return left == right;
    }

    internal XmlNodeSelection CreateXmlNavigationSelection(XmlDocument? document, XmlNode[] nodes, XmlRange? range, bool frozen)
    {
        XmlNodeSelection result = new XmlNodeSelection();
        result.Document = document;
        result.Nodes = nodes;
        result.Range = range;
        result.MutationVersion = document == null ? 0 : document.MutationVersion;
        result.IsFrozen = frozen;
        return result;
    }

    internal XmlNode[] CopyXmlNavigationNodes(XmlNode[] nodes)
    {
        XmlNode[] result = new XmlNode[nodes.Length];
        Array.Copy(nodes, result, nodes.Length);
        return result;
    }

    internal void AddXmlNavigationSubtree(List<XmlNode> result, XmlNode node, bool includeAttributes)
    {
        result.Add(node);
        if (includeAttributes)
        {
            XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                AddXmlNavigationSubtree(result, attributes[attributeIndex], true);
                attributeIndex = attributeIndex + 1;
            }
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            AddXmlNavigationSubtree(result, children[childIndex], includeAttributes);
            childIndex = childIndex + 1;
        }
    }

    internal XmlNode FindXmlNavigationRoot(XmlNode node)
    {
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        XmlNode current = node;
        while (current.Parent != null)
        {
            if (!visited.Add(current))
            {
                throw new ArgumentException("The node parent chain contains a cycle.", "node");
            }

            current = current.Parent;
        }

        return current;
    }

    internal XmlNode XmlNavigationAxisAnchor(XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Attribute && node.Parent != null)
        {
            return node.Parent;
        }

        return node;
    }

    internal bool IsXmlNavigationDescendant(XmlNode candidate, XmlNode ancestor)
    {
        XmlNode? current = candidate.Parent;
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                throw new ArgumentException("The tree contains a parent cycle.", "candidate");
            }

            current = current.Parent;
        }

        return false;
    }

    internal bool IsXmlNavigationAncestor(XmlNode candidate, XmlNode node)
    {
        XmlNode? current = node.Parent;
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        while (current != null)
        {
            if (current == candidate)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                throw new ArgumentException("The tree contains a parent cycle.", "node");
            }

            current = current.Parent;
        }

        return false;
    }

    internal int IndexXmlNavigationNode(XmlNode[] nodes, XmlNode node)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == node)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int IndexXmlNavigationList(List<XmlNode> nodes, XmlNode node)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == node)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal XmlNode? FindXmlNavigationContextElement(XmlNode node)
    {
        XmlNode? current = node;
        while (current != null)
        {
            if (current.Kind == XmlNodeKind.Element)
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    internal string XmlNavigationNamespacePrefix(XmlNamespaceBinding binding)
    {
        string? prefix = binding.Prefix;
        return prefix == null ? "" : prefix;
    }

    internal XmlNamespaceBinding CopyXmlNavigationBinding(XmlNamespaceBinding source)
    {
        XmlNamespaceBinding result = new XmlNamespaceBinding();
        result.Prefix = source.Prefix;
        result.NamespaceUri = source.NamespaceUri;
        result.IsUndeclaration = source.IsUndeclaration;
        result.Range = CopyXmlNavigationRange(source.Range);
        return result;
    }

    internal SourceRange? CopyXmlNavigationRange(SourceRange? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceRange result = new SourceRange();
        result.Start = CopyXmlNavigationLocation(source.Start);
        result.End = CopyXmlNavigationLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXmlNavigationLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }

    internal void AppendXmlNavigationStringValue(StringBuilder value, XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            if (node.Value != null)
            {
                value.Append(node.Value);
            }

            return;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            AppendXmlNavigationStringValue(value, children[index]);
            index = index + 1;
        }
    }

    internal List<XmlNode> XmlNavigationElementAncestors(XmlNode node)
    {
        List<XmlNode> result = new List<XmlNode>();
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        XmlNode? current = node.Kind == XmlNodeKind.Attribute ? node.Parent : node;
        while (current != null)
        {
            if (!visited.Add(current))
            {
                throw new ArgumentException("The node parent chain contains a cycle.", "node");
            }

            if (current.Kind == XmlNodeKind.Element)
            {
                result.Insert(0, current);
            }

            current = current.Parent;
        }

        return result;
    }

    internal string? FindXmlNavigationContextAttribute(XmlNode element, string localName)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null)
            {
                bool local = string.Equals(name.LocalName, localName, StringComparison.Ordinal);
                bool namespaceName = string.Equals(name.NamespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal);
                bool lexicalName = string.Equals(name.QualifiedName, "xml:" + localName, StringComparison.Ordinal);
                if (local && (namespaceName || lexicalName))
                {
                    return attributes[index].Value == null ? "" : attributes[index].Value;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal string ResolveXmlNavigationBase(string? parentBase, string value)
    {
        Uri reference = UriTools.CreateReference(value);
        if (reference.IsAbsoluteUri)
        {
            return UriTools.CanonicalAbsoluteUri(reference);
        }

        if (parentBase == null)
        {
            return value;
        }

        Uri parent = UriTools.CreateReference(parentBase);
        if (!parent.IsAbsoluteUri)
        {
            return value;
        }

        return UriTools.CanonicalAbsoluteUri(UriTools.ResolveReference(parent, value));
    }
}
