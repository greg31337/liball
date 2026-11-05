using System;
using System.Collections.Generic;

namespace xml;

internal class TreeMutation
{
    internal TreeInvariants Invariants;
    internal IdIndex IdentifierIndex;

    internal TreeMutation()
    {
        Invariants = new TreeInvariants();
        IdentifierIndex = new IdIndex();
    }

    internal XmlNode CreateXmlTreeNodeOperation(XmlDocument document, XmlNodeKind kind, XmlName? name, string? value)
    {
        Guard.NotNull(document, "document");
        if (document.IsReadOnly)
        {
            throw new InvalidOperationException("A node cannot be created for a read-only document.");
        }

        if ((int)kind < (int)XmlNodeKind.Document || (int)kind > (int)XmlNodeKind.DocumentType)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        XmlNode result = new XmlNode();
        result.OwnerDocument = document;
        result.Kind = kind;
        result.Name = CopyXmlMutationName(name);
        result.Value = kind == XmlNodeKind.Attribute && IdentifierIndex.IsXmlIdName(result.Name) ? IdentifierIndex.CollapseXmlIdentifierWhitespace(value) : value;
        result.Children = [];
        if (kind == XmlNodeKind.Element)
        {
            result.Attributes = [];
            result.NamespaceBindings = [];
        }

        result.Identity = AllocateXmlMutationIdentity(document);
        result.MutationVersion = document.MutationVersion;
        result.IsReadOnly = false;
        return result;
    }

    internal XmlNode CloneXmlTreeNodeOperation(XmlNode node, bool deep)
    {
        Guard.NotNull(node, "node");
        XmlDocument? document = node.OwnerDocument;
        if (document == null)
        {
            throw new ArgumentException("A cloned node must have an owner document.", "node");
        }

        ValidateXmlMutationSource(node);
        return CloneXmlMutationSubtree(node, document, deep, null);
    }

    internal XmlNode ImportXmlTreeNodeOperation(XmlDocument document, XmlNode node, bool deep)
    {
        Guard.NotNull(document, "document");
        Guard.NotNull(node, "node");
        if (document.IsReadOnly)
        {
            throw new InvalidOperationException("A node cannot be imported into a read-only document.");
        }

        ValidateXmlMutationSource(node);
        return CloneXmlMutationSubtree(node, document, deep, null);
    }

    internal XmlNode AdoptXmlTreeNodeOperation(XmlDocument document, XmlNode node)
    {
        Guard.NotNull(document, "document");
        Guard.NotNull(node, "node");
        if (document.IsReadOnly)
        {
            throw new InvalidOperationException("A node cannot be adopted into a read-only document.");
        }

        if (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment)
        {
            throw new ArgumentException("A document container cannot be adopted.", "node");
        }

        Invariants.EnsureXmlTreeMutationAllowed(node);
        ValidateXmlMutationSource(node);
        XmlNode? oldParent = node.Parent;
        XmlDocument? optionalOldDocument = node.OwnerDocument;
        if (optionalOldDocument == null)
        {
            throw new ArgumentException("An adopted node must have an owner document.", "node");
        }

        XmlDocument oldDocument = optionalOldDocument;
        XmlNode[]? oldChildren = null;
        XmlNode[]? oldAttributes = null;
        if (oldParent != null)
        {
            Invariants.EnsureXmlTreeMutationAllowed(oldParent);
            if (node.Kind == XmlNodeKind.Attribute)
            {
                oldAttributes = RemoveXmlMutationItem(oldParent.Attributes, node, "node");
                Invariants.ValidateXmlAttributeSequence(oldParent, oldAttributes);
            }
            else
            {
                oldChildren = RemoveXmlMutationItem(oldParent.Children, node, "node");
                Invariants.ValidateXmlChildSequence(oldParent, oldChildren);
                ValidateXmlMutationDocumentMetadata(oldParent, oldChildren);
            }
        }

        if (oldParent != null)
        {
            if (node.Kind == XmlNodeKind.Attribute)
            {
                oldParent.Attributes = oldAttributes;
            }
            else
            {
                oldParent.Children = oldChildren;
                UpdateXmlMutationDocumentMetadata(oldParent, oldChildren == null ? [] : oldChildren);
            }
        }

        if (oldParent == null && oldDocument == document)
        {
            return node;
        }

        node.Parent = null;
        if (oldDocument != document)
        {
            TransferXmlMutationOwnership(node, document);
            long oldVersion = AdvanceXmlMutationVersion(oldDocument);
            if (oldParent != null)
            {
                oldParent.MutationVersion = oldVersion;
            }

            long newVersion = AdvanceXmlMutationVersion(document);
            SetXmlMutationSubtreeVersion(node, newVersion);
        }
        else
        {
            CompleteXmlMutationChange(document, oldParent, node, null);
        }

        return node;
    }

    internal XmlNode AppendXmlTreeChildOperation(XmlNode parent, XmlNode child)
    {
        Guard.NotNull(parent, "parent");
        Guard.NotNull(child, "child");
        Invariants.ValidateXmlChildPlacement(parent, child);
        XmlDocument? optionalDocument = parent.OwnerDocument;
        if (optionalDocument == null)
        {
            throw new ArgumentException("A parent node must have an owner document.", "parent");
        }

        XmlDocument document = optionalDocument;
        XmlNode? sourceParent = child.Parent;
        XmlNode[] targetSource = parent.Children == null ? [] : parent.Children;
        if (sourceParent == parent && targetSource.Length > 0 && targetSource[targetSource.Length - 1] == child)
        {
            return child;
        }

        XmlNode[] targetBase = sourceParent == parent ? RemoveXmlMutationItem(targetSource, child, "child") : targetSource;
        XmlNode[] targetCandidate = InsertXmlMutationItem(targetBase, child, targetBase.Length);
        XmlNode[]? sourceCandidate = null;
        if (sourceParent != null && sourceParent != parent)
        {
            Invariants.EnsureXmlTreeMutationAllowed(sourceParent);
            sourceCandidate = RemoveXmlMutationItem(sourceParent.Children, child, "child");
            Invariants.ValidateXmlChildSequence(sourceParent, sourceCandidate);
            ValidateXmlMutationDocumentMetadata(sourceParent, sourceCandidate);
        }

        Invariants.ValidateXmlChildSequence(parent, targetCandidate);
        ValidateXmlMutationDocumentMetadata(parent, targetCandidate);
        if (sourceParent != null && sourceParent != parent)
        {
            sourceParent.Children = sourceCandidate;
            UpdateXmlMutationDocumentMetadata(sourceParent, sourceCandidate == null ? [] : sourceCandidate);
        }

        parent.Children = targetCandidate;
        child.Parent = parent;
        UpdateXmlMutationDocumentMetadata(parent, targetCandidate);
        CompleteXmlMutationChange(document, parent, child, sourceParent);
        return child;
    }

    internal XmlNode InsertXmlTreeBeforeOperation(XmlNode parent, XmlNode node, XmlNode reference)
    {
        Guard.NotNull(parent, "parent");
        Guard.NotNull(node, "node");
        Guard.NotNull(reference, "reference");
        Invariants.ValidateXmlChildPlacement(parent, node);
        XmlDocument? optionalDocument = parent.OwnerDocument;
        if (optionalDocument == null)
        {
            throw new ArgumentException("A parent node must have an owner document.", "parent");
        }

        XmlDocument document = optionalDocument;
        XmlNode[] targetSource = parent.Children == null ? [] : parent.Children;
        if (IndexXmlMutationItem(targetSource, reference) < 0 || reference.Parent != parent)
        {
            throw new ArgumentException("The reference node is not a child of the parent.", "reference");
        }

        if (node == reference)
        {
            return node;
        }

        XmlNode? sourceParent = node.Parent;
        if (sourceParent == parent)
        {
            int nodeIndex = IndexXmlMutationItem(targetSource, node);
            int existingReferenceIndex = IndexXmlMutationItem(targetSource, reference);
            if (nodeIndex >= 0 && nodeIndex + 1 == existingReferenceIndex)
            {
                return node;
            }
        }

        XmlNode[] targetBase = sourceParent == parent ? RemoveXmlMutationItem(targetSource, node, "node") : targetSource;
        int referenceIndex = IndexXmlMutationItem(targetBase, reference);
        XmlNode[] targetCandidate = InsertXmlMutationItem(targetBase, node, referenceIndex);
        XmlNode[]? sourceCandidate = null;
        if (sourceParent != null && sourceParent != parent)
        {
            Invariants.EnsureXmlTreeMutationAllowed(sourceParent);
            sourceCandidate = RemoveXmlMutationItem(sourceParent.Children, node, "node");
            Invariants.ValidateXmlChildSequence(sourceParent, sourceCandidate);
            ValidateXmlMutationDocumentMetadata(sourceParent, sourceCandidate);
        }

        Invariants.ValidateXmlChildSequence(parent, targetCandidate);
        ValidateXmlMutationDocumentMetadata(parent, targetCandidate);
        if (sourceParent != null && sourceParent != parent)
        {
            sourceParent.Children = sourceCandidate;
            UpdateXmlMutationDocumentMetadata(sourceParent, sourceCandidate == null ? [] : sourceCandidate);
        }

        parent.Children = targetCandidate;
        node.Parent = parent;
        UpdateXmlMutationDocumentMetadata(parent, targetCandidate);
        CompleteXmlMutationChange(document, parent, node, sourceParent);
        return node;
    }

    internal XmlNode ReplaceXmlTreeNodeOperation(XmlNode parent, XmlNode replacement, XmlNode existing)
    {
        Guard.NotNull(parent, "parent");
        Guard.NotNull(replacement, "replacement");
        Guard.NotNull(existing, "existing");
        Invariants.ValidateXmlChildPlacement(parent, replacement);
        XmlDocument? optionalDocument = parent.OwnerDocument;
        if (optionalDocument == null)
        {
            throw new ArgumentException("A parent node must have an owner document.", "parent");
        }

        XmlDocument document = optionalDocument;
        Invariants.EnsureXmlTreeMutationAllowed(existing);
        XmlNode[] targetSource = parent.Children == null ? [] : parent.Children;
        if (IndexXmlMutationItem(targetSource, existing) < 0 || existing.Parent != parent)
        {
            throw new ArgumentException("The existing node is not a child of the parent.", "existing");
        }

        if (replacement == existing)
        {
            return existing;
        }

        XmlNode? sourceParent = replacement.Parent;
        XmlNode[] targetBase = sourceParent == parent ? RemoveXmlMutationItem(targetSource, replacement, "replacement") : targetSource;
        int existingIndex = IndexXmlMutationItem(targetBase, existing);
        XmlNode[] targetCandidate = ReplaceXmlMutationItem(targetBase, replacement, existingIndex);
        XmlNode[]? sourceCandidate = null;
        if (sourceParent != null && sourceParent != parent)
        {
            Invariants.EnsureXmlTreeMutationAllowed(sourceParent);
            sourceCandidate = RemoveXmlMutationItem(sourceParent.Children, replacement, "replacement");
            Invariants.ValidateXmlChildSequence(sourceParent, sourceCandidate);
            ValidateXmlMutationDocumentMetadata(sourceParent, sourceCandidate);
        }

        Invariants.ValidateXmlChildSequence(parent, targetCandidate);
        ValidateXmlMutationDocumentMetadata(parent, targetCandidate);
        if (sourceParent != null && sourceParent != parent)
        {
            sourceParent.Children = sourceCandidate;
            UpdateXmlMutationDocumentMetadata(sourceParent, sourceCandidate == null ? [] : sourceCandidate);
        }

        parent.Children = targetCandidate;
        replacement.Parent = parent;
        existing.Parent = null;
        UpdateXmlMutationDocumentMetadata(parent, targetCandidate);
        CompleteXmlMutationChange(document, parent, replacement, existing);
        if (sourceParent != null && sourceParent != parent)
        {
            sourceParent.MutationVersion = document.MutationVersion;
        }

        return existing;
    }

    internal void ReplaceXmlTreeNodeWithSequenceOperation(XmlNode parent, XmlNode existing, XmlNode[] replacements)
    {
        Guard.NotNull(parent, "parent");
        Guard.NotNull(existing, "existing");
        Guard.NotNull(replacements, "replacements");
        Invariants.EnsureXmlTreeMutationAllowed(parent);
        Invariants.EnsureXmlTreeMutationAllowed(existing);
        XmlDocument? optionalDocument = parent.OwnerDocument;
        if (optionalDocument == null)
        {
            throw new ArgumentException("A parent node must have an owner document.", "parent");
        }

        XmlDocument document = optionalDocument;
        XmlNode[] source = parent.Children == null ? [] : parent.Children;
        int existingIndex = IndexXmlMutationItem(source, existing);
        if (existingIndex < 0 || existing.Parent != parent)
        {
            throw new ArgumentException("The existing node is not a child of the parent.", "existing");
        }

        int replacementIndex = 0;
        while (replacementIndex < replacements.Length)
        {
            XmlNode replacement = replacements[replacementIndex];
            if (replacement.Parent != null)
            {
                throw new ArgumentException("A replacement sequence node must be detached.", "replacements");
            }

            Invariants.ValidateXmlChildPlacement(parent, replacement);
            replacementIndex = replacementIndex + 1;
        }

        XmlNode[] candidate = new XmlNode[source.Length - 1 + replacements.Length];
        if (existingIndex > 0)
        {
            Array.Copy(source, 0, candidate, 0, existingIndex);
        }

        if (replacements.Length > 0)
        {
            Array.Copy(replacements, 0, candidate, existingIndex, replacements.Length);
        }

        if (existingIndex + 1 < source.Length)
        {
            Array.Copy(source, existingIndex + 1, candidate, existingIndex + replacements.Length, source.Length - existingIndex - 1);
        }

        Invariants.ValidateXmlChildSequence(parent, candidate);
        ValidateXmlMutationDocumentMetadata(parent, candidate);
        parent.Children = candidate;
        existing.Parent = null;
        replacementIndex = 0;
        while (replacementIndex < replacements.Length)
        {
            replacements[replacementIndex].Parent = parent;
            replacementIndex = replacementIndex + 1;
        }

        UpdateXmlMutationDocumentMetadata(parent, candidate);
        long version = AdvanceXmlMutationVersion(document);
        parent.MutationVersion = version;
        existing.MutationVersion = version;
        replacementIndex = 0;
        while (replacementIndex < replacements.Length)
        {
            SetXmlMutationSubtreeVersion(replacements[replacementIndex], version);
            replacementIndex = replacementIndex + 1;
        }
    }

    internal XmlNode RemoveXmlTreeNodeOperation(XmlNode parent, XmlNode child)
    {
        Guard.NotNull(parent, "parent");
        Guard.NotNull(child, "child");
        Invariants.EnsureXmlTreeMutationAllowed(parent);
        Invariants.EnsureXmlTreeMutationAllowed(child);
        XmlDocument? optionalDocument = parent.OwnerDocument;
        if (optionalDocument == null)
        {
            throw new ArgumentException("A parent node must have an owner document.", "parent");
        }

        XmlDocument document = optionalDocument;
        if (child.Parent != parent)
        {
            throw new ArgumentException("The child parent link does not identify the supplied parent.", "child");
        }

        XmlNode[] candidate = RemoveXmlMutationItem(parent.Children, child, "child");
        Invariants.ValidateXmlChildSequence(parent, candidate);
        ValidateXmlMutationDocumentMetadata(parent, candidate);
        parent.Children = candidate;
        child.Parent = null;
        UpdateXmlMutationDocumentMetadata(parent, candidate);
        CompleteXmlMutationChange(document, parent, child, null);
        return child;
    }

    internal void ValidateXmlMutationSource(XmlNode node)
    {
        XmlDocument? document = node.OwnerDocument;
        if (document == null)
        {
            throw new ArgumentException("A source node must have an owner document.", "node");
        }

        HashSet<XmlNode> ancestors = new HashSet<XmlNode>();
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        ValidateXmlMutationSubtree(node, node.Parent, document, ancestors, visited);
    }

    internal void ValidateXmlMutationSubtree(XmlNode node, XmlNode? expectedParent, XmlDocument document, HashSet<XmlNode> ancestors, HashSet<XmlNode> visited)
    {
        if (ancestors.Contains(node) || !visited.Add(node))
        {
            throw new ArgumentException("A source subtree cannot contain cycles or repeated nodes.", "node");
        }

        if (node.Parent != expectedParent || node.OwnerDocument != document)
        {
            throw new ArgumentException("A source subtree has inconsistent parent or owner-document links.", "node");
        }

        ancestors.Add(node);
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        if (node.Kind == XmlNodeKind.Element)
        {
            Invariants.ValidateXmlAttributeSequence(node, attributes);
        }
        else if (attributes.Length != 0)
        {
            throw new ArgumentException("Only an element source node can contain attributes.", "node");
        }

        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            ValidateXmlMutationSubtree(attributes[attributeIndex], node, document, ancestors, visited);
            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            if (!Invariants.IsXmlLegalChildKind(node.Kind, children[childIndex].Kind))
            {
                throw new ArgumentException("A source subtree contains an illegal child node kind.", "node");
            }

            ValidateXmlMutationSubtree(children[childIndex], node, document, ancestors, visited);
            childIndex = childIndex + 1;
        }

        ancestors.Remove(node);
    }

    internal XmlNode CloneXmlMutationSubtree(XmlNode source, XmlDocument document, bool deep, XmlNode? parent)
    {
        XmlNode copy = new XmlNode();
        copy.Kind = source.Kind;
        copy.Name = CopyXmlMutationName(source.Name);
        copy.Value = source.Value;
        copy.Parent = parent;
        copy.OwnerDocument = document;
        copy.NamespaceBindings = CopyXmlMutationNamespaces(source.NamespaceBindings);
        copy.Range = CopyXmlMutationRange(source.Range);
        copy.TypeInfo = CopyXmlMutationTypeInfo(source.TypeInfo);
        copy.Identity = AllocateXmlMutationIdentity(document);
        copy.MutationVersion = document.MutationVersion;
        copy.IsReadOnly = false;
        copy.IncludeHistory = CopyXmlMutationHistory(source.IncludeHistory);
        if (source.Attributes == null)
        {
            copy.Attributes = null;
        }
        else
        {
            copy.Attributes = new XmlNode[source.Attributes.Length];
            int attributeIndex = 0;
            while (attributeIndex < source.Attributes.Length)
            {
                copy.Attributes[attributeIndex] = CloneXmlMutationSubtree(source.Attributes[attributeIndex], document, true, copy);
                attributeIndex = attributeIndex + 1;
            }
        }

        if (!deep || source.Children == null)
        {
            copy.Children = [];
        }
        else
        {
            copy.Children = new XmlNode[source.Children.Length];
            int childIndex = 0;
            while (childIndex < source.Children.Length)
            {
                copy.Children[childIndex] = CloneXmlMutationSubtree(source.Children[childIndex], document, true, copy);
                childIndex = childIndex + 1;
            }
        }

        return copy;
    }

    internal void TransferXmlMutationOwnership(XmlNode node, XmlDocument document)
    {
        node.OwnerDocument = document;
        node.Identity = AllocateXmlMutationIdentity(document);
        node.MutationVersion = document.MutationVersion;
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            TransferXmlMutationOwnership(attributes[attributeIndex], document);
            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            TransferXmlMutationOwnership(children[childIndex], document);
            childIndex = childIndex + 1;
        }
    }

    internal XmlNode[] RemoveXmlMutationItem(XmlNode[]? source, XmlNode item, string parameterName)
    {
        XmlNode[] items = source == null ? [] : source;
        int index = IndexXmlMutationItem(items, item);
        if (index < 0)
        {
            throw new ArgumentException("The node does not belong to the expected parent collection.", parameterName);
        }

        XmlNode[] result = new XmlNode[items.Length - 1];
        if (index > 0)
        {
            Array.Copy(items, 0, result, 0, index);
        }

        if (index + 1 < items.Length)
        {
            Array.Copy(items, index + 1, result, index, items.Length - index - 1);
        }

        return result;
    }

    internal XmlNode[] InsertXmlMutationItem(XmlNode[] source, XmlNode item, int index)
    {
        if (index < 0 || index > source.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        XmlNode[] result = new XmlNode[source.Length + 1];
        if (index > 0)
        {
            Array.Copy(source, 0, result, 0, index);
        }

        result[index] = item;
        if (index < source.Length)
        {
            Array.Copy(source, index, result, index + 1, source.Length - index);
        }

        return result;
    }

    internal XmlNode[] ReplaceXmlMutationItem(XmlNode[] source, XmlNode item, int index)
    {
        if (index < 0 || index >= source.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        XmlNode[] result = new XmlNode[source.Length];
        Array.Copy(source, result, source.Length);
        result[index] = item;
        return result;
    }

    internal int IndexXmlMutationItem(XmlNode[] source, XmlNode item)
    {
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == item)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void ValidateXmlMutationDocumentMetadata(XmlNode parent, XmlNode[] children)
    {
        if (parent.Kind != XmlNodeKind.Document)
        {
            return;
        }

        XmlDocument? document = parent.OwnerDocument;
        if (document == null)
        {
            throw new ArgumentException("A document node requires an owner document.", "parent");
        }

        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.DocumentType && document.DocumentType == null)
            {
                throw new ArgumentException("A document-type node requires retained document-type metadata.", "children");
            }

            index = index + 1;
        }
    }

    internal void UpdateXmlMutationDocumentMetadata(XmlNode parent, XmlNode[] children)
    {
        if (parent.Kind != XmlNodeKind.Document || parent.OwnerDocument == null)
        {
            return;
        }

        XmlDocument document = parent.OwnerDocument;
        document.DocumentElement = null;
        bool hasDocumentType = false;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                document.DocumentElement = children[index];
            }
            else if (children[index].Kind == XmlNodeKind.DocumentType)
            {
                hasDocumentType = true;
            }

            index = index + 1;
        }

        if (!hasDocumentType)
        {
            document.DocumentType = null;
        }
    }

    internal long AllocateXmlMutationIdentity(XmlDocument document)
    {
        long identity = document.NextIdentity;
        long discovered = FindNextXmlMutationIdentity(document);
        if (identity < discovered)
        {
            identity = discovered;
        }

        document.NextIdentity = identity + 1;
        return identity;
    }

    internal long AdvanceXmlMutationVersion(XmlDocument document)
    {
        document.MutationVersion = document.MutationVersion + 1;
        return document.MutationVersion;
    }

    internal void CompleteXmlMutationChange(XmlDocument document, XmlNode? first, XmlNode? second, XmlNode? third)
    {
        long version = AdvanceXmlMutationVersion(document);
        if (first != null)
        {
            first.MutationVersion = version;
        }

        if (second != null)
        {
            second.MutationVersion = version;
        }

        if (third != null)
        {
            third.MutationVersion = version;
        }
    }

    internal void SetXmlMutationSubtreeVersion(XmlNode node, long version)
    {
        List<XmlNode> pending = new List<XmlNode>();
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        pending.Add(node);
        while (pending.Count > 0)
        {
            int last = pending.Count - 1;
            XmlNode item = pending[last];
            pending.RemoveAt(last);
            if (visited.Add(item))
            {
                item.MutationVersion = version;
                AddXmlMutationNodes(pending, item.Attributes);
                AddXmlMutationNodes(pending, item.Children);
            }
        }
    }

    internal long FindNextXmlMutationIdentity(XmlDocument document)
    {
        long maximum = 0;
        XmlNode? root = document.DocumentNode;
        if (root != null)
        {
            List<XmlNode> pending = new List<XmlNode>();
            HashSet<XmlNode> visited = new HashSet<XmlNode>();
            pending.Add(root);
            while (pending.Count > 0)
            {
                int last = pending.Count - 1;
                XmlNode item = pending[last];
                pending.RemoveAt(last);
                if (visited.Add(item))
                {
                    if (item.Identity > maximum)
                    {
                        maximum = item.Identity;
                    }

                    AddXmlMutationNodes(pending, item.Attributes);
                    AddXmlMutationNodes(pending, item.Children);
                }
            }
        }

        return maximum + 1;
    }

    internal void AddXmlMutationNodes(List<XmlNode> target, XmlNode[]? source)
    {
        if (source == null)
        {
            return;
        }

        int index = 0;
        while (index < source.Length)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    internal XmlName? CopyXmlMutationName(XmlName? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlName copy = new XmlName();
        copy.Prefix = source.Prefix;
        copy.LocalName = source.LocalName;
        copy.NamespaceUri = source.NamespaceUri;
        copy.QualifiedName = source.QualifiedName;
        return copy;
    }

    internal XmlNode[]? CopyXmlMutationHistory(XmlNode[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlNode[] copy = new XmlNode[source.Length];
        if (source.Length > 0)
        {
            Array.Copy(source, copy, source.Length);
        }

        return copy;
    }

    internal XmlSchemaTypeInfo? CopyXmlMutationTypeInfo(XmlSchemaTypeInfo? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlSchemaTypeInfo copy = new XmlSchemaTypeInfo();
        copy.TypeName = CopyXmlMutationName(source.TypeName);
        copy.TypeDefinition = source.TypeDefinition;
        copy.Declaration = source.Declaration;
        copy.MemberTypeName = CopyXmlMutationName(source.MemberTypeName);
        copy.AtomicType = source.AtomicType;
        copy.Validity = source.Validity;
        copy.IsNilled = source.IsNilled;
        copy.IsDefaulted = source.IsDefaulted;
        copy.IsId = source.IsId;
        copy.IsIdReference = source.IsIdReference;
        copy.NormalizedValue = source.NormalizedValue;
        copy.ActualValue = source.ActualValue;
        copy.ActualValues = source.ActualValues == null ? null : (XdmAtomicValue[])source.ActualValues.Clone();
        return copy;
    }

    internal XmlNamespaceBinding[]? CopyXmlMutationNamespaces(XmlNamespaceBinding[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlNamespaceBinding[] copies = new XmlNamespaceBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlNamespaceBinding copy = new XmlNamespaceBinding();
            copy.Prefix = source[index].Prefix;
            copy.NamespaceUri = source[index].NamespaceUri;
            copy.IsUndeclaration = source[index].IsUndeclaration;
            copy.Range = CopyXmlMutationRange(source[index].Range);
            copies[index] = copy;
            index = index + 1;
        }

        return copies;
    }

    internal SourceRange? CopyXmlMutationRange(SourceRange? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceRange copy = new SourceRange();
        copy.Start = CopyXmlMutationLocation(source.Start);
        copy.End = CopyXmlMutationLocation(source.End);
        return copy;
    }

    internal SourceLocation? CopyXmlMutationLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation copy = new SourceLocation();
        copy.SourceUri = source.SourceUri;
        copy.CharacterOffset = source.CharacterOffset;
        copy.ByteOffset = source.ByteOffset;
        copy.Line = source.Line;
        copy.Column = source.Column;
        return copy;
    }
}
