namespace svg;

/// <summary>Performs invariant-preserving node and attribute mutations without global state.</summary>
public class SvgNodeApi : ISvgNodeApi
{
    /// <summary>Creates a stateless node mutation API.</summary>
    public SvgNodeApi()
    {
    }

    /// <summary>Appends a detached child to a document or element.</summary>
    /// <param name="parent">The destination document or element.</param>
    /// <param name="child">The detached non-document child.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult AppendChild(SvgNode? parent, SvgNode? child)
    {
        if (parent == null || child == null) return Failure(SvgMutationError.InvalidArgument, "A parent and child are required.");
        return InsertDetachedChild(parent, parent.Children.Count, child);
    }

    /// <summary>Inserts a detached child at a zero-based child index.</summary>
    /// <param name="parent">The destination document or element.</param>
    /// <param name="index">The insertion index from zero through the child count.</param>
    /// <param name="child">The detached non-document child.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult InsertChild(SvgNode? parent, int index, SvgNode? child)
    {
        if (parent == null || child == null) return Failure(SvgMutationError.InvalidArgument, "A parent and child are required.");
        return InsertDetachedChild(parent, index, child);
    }

    /// <summary>Replaces one direct child with a detached node.</summary>
    /// <param name="parent">The current parent.</param>
    /// <param name="current">The direct child to replace.</param>
    /// <param name="replacement">The detached replacement.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult ReplaceChild(SvgNode? parent, SvgNode? current, SvgNode? replacement)
    {
        if (parent == null || current == null || replacement == null) return Failure(SvgMutationError.InvalidArgument, "A parent, current child, and replacement are required.");
        SvgMutationResult? parentFailure = ValidateParent(parent);
        if (parentFailure != null) return parentFailure;
        int index = FindChild(parent, current);
        if (index < 0 || !object.ReferenceEquals(current.Parent, parent)) return Failure(SvgMutationError.NodeNotChild, "The current node is not a direct child of the supplied parent.");
        if (!object.ReferenceEquals(current.OwnerDocument, parent.OwnerDocument)) return Failure(SvgMutationError.InvalidOwnership, "The current child and parent ownership fields are inconsistent.");
        if (object.ReferenceEquals(current, replacement)) return NodeSuccess(replacement, null);
        if (replacement.Kind == SvgNodeKind.Document) return Failure(SvgMutationError.InvalidChild, "A document node cannot be inserted as a child.");
        if (replacement.Parent != null) return Failure(SvgMutationError.NodeAlreadyAttached, "The replacement node is already attached; move it explicitly first.");
        if (WouldCreateCycle(parent, replacement)) return Failure(SvgMutationError.Cycle, "The replacement would create an ancestry cycle.");
        SvgMutationResult? rootFailure = ValidateDocumentElement(parent, replacement, current);
        if (rootFailure != null) return rootFailure;
        SvgDocument? sourceDocument = replacement.OwnerDocument;
        SvgDocument? targetDocument = parent.OwnerDocument;
        EnsureCanInvalidate(null, parent, sourceDocument, targetDocument);
        long beforeVersion = targetDocument == null ? parent.Version : targetDocument.Version;
        PrepareRecording(sourceDocument, targetDocument);
        CaptureNodeForRecording(sourceDocument, targetDocument, replacement);
        parent.Children[index] = replacement;
        current.Parent = null;
        replacement.Parent = parent;
        UpdateDocumentElementAfterReplacement(parent, current, replacement);
        long[] versions = CompleteInvalidation(null, parent, sourceDocument, targetDocument);
        AssignNodeOwner(current, null, versions[1]);
        AssignNodeOwner(replacement, targetDocument, versions[1]);
        SvgChange change = CreateNodeChange(SvgChangeKind.NodeReplace, replacement, parent, parent, index, index, beforeVersion, versions[1]);
        change.RemovedNode = current;
        RecordChange(sourceDocument, targetDocument, change);
        return NodeSuccess(replacement, current);
    }

    /// <summary>Removes one direct child and clears ownership throughout its subtree.</summary>
    /// <param name="parent">The current parent.</param>
    /// <param name="child">The direct child to remove.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult RemoveChild(SvgNode? parent, SvgNode? child)
    {
        if (parent == null || child == null) return Failure(SvgMutationError.InvalidArgument, "A parent and child are required.");
        SvgMutationResult? parentFailure = ValidateParent(parent);
        if (parentFailure != null) return parentFailure;
        int index = FindChild(parent, child);
        if (index < 0 || !object.ReferenceEquals(child.Parent, parent)) return Failure(SvgMutationError.NodeNotChild, "The node is not a direct child of the supplied parent.");
        SvgDocument? oldDocument = child.OwnerDocument;
        if (!object.ReferenceEquals(oldDocument, parent.OwnerDocument)) return Failure(SvgMutationError.InvalidOwnership, "The child and parent ownership fields are inconsistent.");
        EnsureCanInvalidate(parent, null, oldDocument, null);
        long beforeVersion = oldDocument == null ? parent.Version : oldDocument.Version;
        PrepareRecording(oldDocument, null);
        parent.Children.RemoveAt(index);
        child.Parent = null;
        ClearDocumentElement(parent, child);
        long[] versions = CompleteInvalidation(parent, null, oldDocument, null);
        AssignNodeOwner(child, null, versions[0]);
        RecordChange(oldDocument, null, CreateNodeChange(SvgChangeKind.NodeRemove, child, parent, null, index, -1, beforeVersion, versions[0]));
        return NodeSuccess(child, child);
    }

    /// <summary>Moves an attached child to a final zero-based index under another document or element.</summary>
    /// <param name="child">The attached child to move.</param>
    /// <param name="newParent">The destination document or element.</param>
    /// <param name="index">The final destination index, or the destination count to append.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult MoveChild(SvgNode? child, SvgNode? newParent, int index)
    {
        if (child == null || newParent == null) return Failure(SvgMutationError.InvalidArgument, "A child and destination parent are required.");
        SvgMutationResult? parentFailure = ValidateParent(newParent);
        if (parentFailure != null) return parentFailure;
        SvgNode? oldParent = child.Parent;
        if (oldParent == null) return Failure(SvgMutationError.NodeNotChild, "Only an attached child can be moved.");
        int oldIndex = FindChild(oldParent, child);
        if (oldIndex < 0) return Failure(SvgMutationError.InvalidOwnership, "The child parent link is inconsistent with the child collection.");
        if (index < 0 || index > newParent.Children.Count) return Failure(SvgMutationError.IndexOutOfRange, "The destination child index is outside the valid range.");
        if (WouldCreateCycle(newParent, child)) return Failure(SvgMutationError.Cycle, "The move would create an ancestry cycle.");
        SvgMutationResult? rootFailure = ValidateDocumentElement(newParent, child, object.ReferenceEquals(oldParent, newParent) ? child : null);
        if (rootFailure != null) return rootFailure;
        SvgDocument? oldDocument = child.OwnerDocument;
        SvgDocument? newDocument = newParent.OwnerDocument;
        if (!object.ReferenceEquals(oldDocument, oldParent.OwnerDocument)) return Failure(SvgMutationError.InvalidOwnership, "The child and old parent ownership fields are inconsistent.");
        int finalIndex = index;
        if (object.ReferenceEquals(oldParent, newParent) && finalIndex == newParent.Children.Count) finalIndex = newParent.Children.Count - 1;
        if (object.ReferenceEquals(oldParent, newParent) && oldIndex == finalIndex) return NodeSuccess(child, null);
        EnsureCanInvalidate(oldParent, newParent, oldDocument, newDocument);
        long beforeVersion = oldDocument == null ? oldParent.Version : oldDocument.Version;
        PrepareRecording(oldDocument, newDocument);
        oldParent.Children.RemoveAt(oldIndex);
        ClearDocumentElement(oldParent, child);
        if (finalIndex > newParent.Children.Count) finalIndex = newParent.Children.Count;
        newParent.Children.Insert(finalIndex, child);
        child.Parent = newParent;
        SetDocumentElement(newParent, child);
        long[] versions = CompleteInvalidation(oldParent, newParent, oldDocument, newDocument);
        AssignNodeOwner(child, newDocument, versions[1]);
        RecordChange(oldDocument, newDocument, CreateNodeChange(SvgChangeKind.NodeMove, child, oldParent, newParent, oldIndex, finalIndex, beforeVersion, versions[1]));
        return NodeSuccess(child, null);
    }

    /// <summary>Creates an independent detached copy of a node.</summary>
    /// <param name="node">The node to copy.</param>
    /// <param name="deep">Whether descendants are copied recursively.</param>
    /// <returns>A generated node with copied attributes and, when requested, descendants.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public SvgNode CloneNode(SvgNode node, bool deep)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        return CloneNodeCore(node, deep);
    }

    /// <summary>Returns the textual content of a node.</summary>
    /// <param name="node">The node whose textual content is read.</param>
    /// <returns>The node value for character-like nodes, concatenated descendant text for containers, or an empty string for a doctype.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public string GetTextContent(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (node.Kind == SvgNodeKind.Text || node.Kind == SvgNodeKind.CData || node.Kind == SvgNodeKind.Comment || node.Kind == SvgNodeKind.ProcessingInstruction) return node.Value;
        if (node.Kind == SvgNodeKind.Doctype) return "";
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        AppendTextContent(node, builder);
        return builder.ToString();
    }

    /// <summary>Replaces the textual content of an element or character-like node.</summary>
    /// <param name="node">The element or character-like node to change.</param>
    /// <param name="value">The replacement text.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult SetTextContent(SvgNode? node, string? value)
    {
        if (node == null || value == null) return Failure(SvgMutationError.InvalidArgument, "A node and text value are required.");
        if (node.Kind == SvgNodeKind.Text || node.Kind == SvgNodeKind.CData || node.Kind == SvgNodeKind.Comment || node.Kind == SvgNodeKind.ProcessingInstruction)
        {
            if (node.Value == value) return NodeSuccess(node, null);
            SvgDocument? valueDocument = node.OwnerDocument;
            EnsureCanInvalidate(node, null, valueDocument, null);
            long valueBeforeVersion = valueDocument == null ? node.Version : valueDocument.Version;
            string scalarOldValue = node.Value;
            PrepareRecording(valueDocument, valueDocument);
            node.Value = value;
            long[] valueVersions = CompleteInvalidation(node, null, valueDocument, null);
            node.Version = valueVersions[0];
            SvgChange valueChange = CreateNodeChange(SvgChangeKind.Text, node, node.Parent, node.Parent, -1, -1, valueBeforeVersion, valueVersions[0]);
            valueChange.OldValue = scalarOldValue;
            valueChange.NewValue = value;
            RecordChange(valueDocument, valueDocument, valueChange);
            return NodeSuccess(node, null);
        }
        if (node.Kind != SvgNodeKind.Element) return Failure(SvgMutationError.InvalidParent, "Only elements and character-like nodes have writable text content.");
        if (TextReplacementIsNoOp(node, value)) return NodeSuccess(node, null);
        SvgDocument? document = node.OwnerDocument;
        EnsureCanInvalidate(node, null, document, null);
        long beforeVersion = document == null ? node.Version : document.Version;
        string oldValue = GetTextContent(node);
        PrepareRecording(document, document);
        System.Collections.Generic.List<SvgNode> removed = new System.Collections.Generic.List<SvgNode>();
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            removed.Add(node.Children[childIndex]);
            childIndex = childIndex + 1;
        }
        node.Children.Clear();
        SvgNode? text = null;
        if (value.Length != 0)
        {
            text = new SvgNode(SvgNodeKind.Text, "", "", "", "", value);
            text.Parent = node;
            node.Children.Add(text);
        }
        long[] versions = CompleteInvalidation(node, null, document, null);
        int removedIndex = 0;
        while (removedIndex < removed.Count)
        {
            removed[removedIndex].Parent = null;
            AssignNodeOwner(removed[removedIndex], null, versions[0]);
            removedIndex = removedIndex + 1;
        }
        if (text != null) AssignNodeOwner(text, document, versions[0]);
        SvgChange textChange = CreateNodeChange(SvgChangeKind.Text, node, node.Parent, node.Parent, -1, -1, beforeVersion, versions[0]);
        textChange.OldValue = oldValue;
        textChange.NewValue = value;
        RecordChange(document, document, textChange);
        return NodeSuccess(node, null);
    }

    /// <summary>Recursively merges adjacent text nodes and removes empty text nodes.</summary>
    /// <param name="node">The container subtree to normalize.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult NormalizeText(SvgNode? node)
    {
        if (node == null) return Failure(SvgMutationError.InvalidArgument, "A node is required.");
        if (node.Kind != SvgNodeKind.Document && node.Kind != SvgNodeKind.Element) return NodeSuccess(node, null);
        if (!NeedsTextNormalization(node)) return NodeSuccess(node, null);
        SvgDocument? document = node.OwnerDocument;
        EnsureCanInvalidate(node, null, document, null);
        long beforeVersion = document == null ? node.Version : document.Version;
        string oldValue = GetTextContent(node);
        PrepareRecording(document, document);
        System.Collections.Generic.List<SvgNode> removed = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> changed = new System.Collections.Generic.List<SvgNode>();
        NormalizeTextCore(node, removed, changed);
        long[] versions = CompleteInvalidation(node, null, document, null);
        int removedIndex = 0;
        while (removedIndex < removed.Count)
        {
            AssignNodeOwner(removed[removedIndex], null, versions[0]);
            removedIndex = removedIndex + 1;
        }
        int changedIndex = 0;
        while (changedIndex < changed.Count)
        {
            changed[changedIndex].IsDirty = true;
            changed[changedIndex].Version = versions[0];
            changed[changedIndex].MarkSourceRewritten();
            changedIndex = changedIndex + 1;
        }
        SvgChange normalizeChange = CreateNodeChange(SvgChangeKind.Text, node, node.Parent, node.Parent, -1, -1, beforeVersion, versions[0]);
        normalizeChange.OldValue = oldValue;
        normalizeChange.NewValue = GetTextContent(node);
        normalizeChange.Reason = "normalize-adjacent-text";
        RecordChange(document, document, normalizeChange);
        return NodeSuccess(node, null);
    }

    /// <summary>Returns ancestors from the direct parent through the outermost document node.</summary>
    /// <param name="node">The node whose ancestors are inspected.</param>
    /// <returns>An independently allocated nearest-first snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> GetAncestors(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        SvgNode? current = node.Parent;
        while (current != null)
        {
            result.Add(current);
            current = current.Parent;
        }
        return result;
    }

    /// <summary>Returns all descendants in preorder document order.</summary>
    /// <param name="node">The node whose descendants are inspected.</param>
    /// <returns>An independently allocated snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> GetDescendants(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        AppendDescendants(node, result);
        return result;
    }

    /// <summary>Returns the immediately preceding sibling, or <c>null</c>.</summary>
    /// <param name="node">The node whose sibling is inspected.</param>
    /// <returns>The preceding sibling, or <c>null</c> when none exists.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public SvgNode? GetPreviousSibling(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (node.Parent == null) return null;
        int index = FindChild(node.Parent, node);
        if (index <= 0) return null;
        return node.Parent.Children[index - 1];
    }

    /// <summary>Returns the immediately following sibling, or <c>null</c>.</summary>
    /// <param name="node">The node whose sibling is inspected.</param>
    /// <returns>The following sibling, or <c>null</c> when none exists.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public SvgNode? GetNextSibling(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (node.Parent == null) return null;
        int index = FindChild(node.Parent, node);
        if (index < 0 || index + 1 >= node.Parent.Children.Count) return null;
        return node.Parent.Children[index + 1];
    }

    /// <summary>Returns all preceding siblings in document order.</summary>
    /// <param name="node">The node whose siblings are inspected.</param>
    /// <returns>An independently allocated snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> GetPreviousSiblings(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        if (node.Parent == null) return result;
        int index = FindChild(node.Parent, node);
        int siblingIndex = 0;
        while (siblingIndex < index)
        {
            result.Add(node.Parent.Children[siblingIndex]);
            siblingIndex = siblingIndex + 1;
        }
        return result;
    }

    /// <summary>Returns all following siblings in document order.</summary>
    /// <param name="node">The node whose siblings are inspected.</param>
    /// <returns>An independently allocated snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> GetNextSiblings(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        if (node.Parent == null) return result;
        int index = FindChild(node.Parent, node) + 1;
        while (index > 0 && index < node.Parent.Children.Count)
        {
            result.Add(node.Parent.Children[index]);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Tests whether one node is a strict ancestor of another node.</summary>
    /// <param name="ancestor">The possible ancestor.</param>
    /// <param name="node">The possible descendant.</param>
    /// <returns><c>true</c> when the ancestry relation exists.</returns>
    public bool IsAncestor(SvgNode? ancestor, SvgNode? node)
    {
        if (ancestor == null || node == null) return false;
        SvgNode? current = node.Parent;
        while (current != null)
        {
            if (object.ReferenceEquals(current, ancestor)) return true;
            current = current.Parent;
        }
        return false;
    }

    /// <summary>Tests whether one node is a strict descendant of another node.</summary>
    /// <param name="descendant">The possible descendant.</param>
    /// <param name="node">The possible ancestor.</param>
    /// <returns><c>true</c> when the ancestry relation exists.</returns>
    public bool IsDescendant(SvgNode descendant, SvgNode node)
    {
        return IsAncestor(node, descendant);
    }

    /// <summary>Resolves a namespace prefix in the node's in-scope namespace context.</summary>
    /// <param name="node">The context node.</param>
    /// <param name="prefix">The prefix, or an empty string for the default namespace.</param>
    /// <returns>The namespace URI, or an empty string when no nonempty binding exists.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public string LookupNamespaceUri(SvgNode node, string prefix)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (prefix == null) throw new System.ArgumentNullException("prefix");
        if (prefix == "xml") return SvgNamespaces.Xml;
        if (prefix == "xmlns") return SvgNamespaces.Xmlns;
        SvgNode? element = GetNamespaceContextElement(node);
        while (element != null)
        {
            int attributeIndex = 0;
            while (attributeIndex < element.Attributes.Count)
            {
                SvgAttribute attribute = element.Attributes[attributeIndex];
                if (IsNamespaceDeclaration(attribute) && GetDeclaredPrefix(attribute) == prefix) return attribute.Value;
                attributeIndex = attributeIndex + 1;
            }
            if (element.Prefix == prefix && element.NamespaceUri.Length != 0) return element.NamespaceUri;
            element = GetNamespaceContextElement(element.Parent);
        }
        return "";
    }

    /// <summary>Finds an in-scope prefix for a namespace URI.</summary>
    /// <param name="node">The context node.</param>
    /// <param name="namespaceUri">The namespace URI.</param>
    /// <returns>The nearest prefix, an empty string for a matching default namespace, or an empty string when no prefix exists.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public string LookupPrefix(SvgNode node, string namespaceUri)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (namespaceUri == null) throw new System.ArgumentNullException("namespaceUri");
        if (namespaceUri == SvgNamespaces.Xml) return "xml";
        if (namespaceUri == SvgNamespaces.Xmlns) return "xmlns";
        System.Collections.Generic.List<string> shadowed = new System.Collections.Generic.List<string>();
        SvgNode? element = GetNamespaceContextElement(node);
        while (element != null)
        {
            int attributeIndex = 0;
            while (attributeIndex < element.Attributes.Count)
            {
                SvgAttribute attribute = element.Attributes[attributeIndex];
                if (IsNamespaceDeclaration(attribute))
                {
                    string declaredPrefix = GetDeclaredPrefix(attribute);
                    if (!ContainsString(shadowed, declaredPrefix))
                    {
                        shadowed.Add(declaredPrefix);
                        if (attribute.Value == namespaceUri) return declaredPrefix;
                    }
                }
                attributeIndex = attributeIndex + 1;
            }
            if (!ContainsString(shadowed, element.Prefix))
            {
                shadowed.Add(element.Prefix);
                if (element.NamespaceUri == namespaceUri) return element.Prefix;
            }
            element = GetNamespaceContextElement(element.Parent);
        }
        return "";
    }

    /// <summary>Tests whether a namespace URI is the context node's default namespace.</summary>
    /// <param name="node">The context node.</param>
    /// <param name="namespaceUri">The namespace URI.</param>
    /// <returns><c>true</c> when the default namespace resolves to the supplied URI.</returns>
    public bool IsDefaultNamespace(SvgNode? node, string? namespaceUri)
    {
        if (node == null || namespaceUri == null) return false;
        return LookupNamespaceUri(node, "") == namespaceUri;
    }

    /// <summary>Finds the nearest inherited attribute in the XML namespace.</summary>
    /// <param name="node">The node whose element context is inspected.</param>
    /// <param name="localName">The case-sensitive XML attribute local name.</param>
    /// <returns>The nearest matching attribute, or <c>null</c> when absent.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public SvgAttribute? GetInheritedXmlAttribute(SvgNode node, string localName)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (localName == null) throw new System.ArgumentNullException("localName");
        SvgNode? element = GetNamespaceContextElement(node);
        while (element != null)
        {
            SvgAttribute? attribute = GetDirectAttribute(element, SvgNamespaces.Xml, localName);
            if (attribute != null) return attribute;
            element = GetNamespaceContextElement(element.Parent);
        }
        return null;
    }

    /// <summary>Returns the nearest inherited authored <c>xml:base</c> value or the document base URI.</summary>
    /// <param name="node">The node whose base context is inspected.</param>
    /// <returns>The nearest authored value, the document base URI, or an empty string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public string LookupXmlBase(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        SvgAttribute? attribute = GetInheritedXmlAttribute(node, SvgAttributeNames.Base);
        if (attribute != null) return attribute.Value;
        return node.OwnerDocument == null ? "" : node.OwnerDocument.BaseUri;
    }

    /// <summary>Resolves the document base URI and each ancestor <c>xml:base</c> value.</summary>
    /// <param name="node">The node whose effective base URI is resolved.</param>
    /// <returns>The effective URI text, or the innermost unresolved reference when no absolute base is available.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public string ResolveXmlBase(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        string effective = node.OwnerDocument == null ? "" : node.OwnerDocument.BaseUri;
        System.Collections.Generic.List<SvgNode> elements = new System.Collections.Generic.List<SvgNode>();
        SvgNode? element = GetNamespaceContextElement(node);
        while (element != null)
        {
            elements.Add(element);
            element = GetNamespaceContextElement(element.Parent);
        }
        int index = elements.Count - 1;
        while (index >= 0)
        {
            SvgAttribute? attribute = GetDirectAttribute(elements[index], SvgNamespaces.Xml, SvgAttributeNames.Base);
            if (attribute != null) effective = ResolveUriReference(effective, attribute.Value);
            index = index - 1;
        }
        return effective;
    }

    /// <summary>Returns the nearest inherited <c>xml:lang</c> value.</summary>
    /// <param name="node">The node whose language context is inspected.</param>
    /// <returns>The inherited language tag or an empty string when unspecified or explicitly reset.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public string LookupXmlLanguage(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        SvgAttribute? attribute = GetInheritedXmlAttribute(node, SvgAttributeNames.Lang);
        return attribute == null ? "" : attribute.Value;
    }

    /// <summary>Returns the nearest inherited XML white-space intention.</summary>
    /// <param name="node">The node whose white-space context is inspected.</param>
    /// <returns>The inherited mode, the default when absent, or <c>Invalid</c> for an invalid nearest value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    public SvgXmlSpaceMode LookupXmlSpace(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        SvgAttribute? attribute = GetInheritedXmlAttribute(node, SvgAttributeNames.Space);
        if (attribute == null || attribute.Value == "default") return SvgXmlSpaceMode.Default;
        if (attribute.Value == "preserve") return SvgXmlSpaceMode.Preserve;
        return SvgXmlSpaceMode.Invalid;
    }

    /// <summary>Returns inherited legacy processing flags and SVG 2 CSS priority metadata.</summary>
    /// <param name="node">The node whose white-space context is inspected.</param>
    /// <param name="profile">The SVG text compatibility profile.</param>
    /// <returns>A newly allocated policy for later text layout.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>profile</c> is invalid.</exception>
    public SvgTextWhitespacePolicy GetTextWhitespacePolicy(SvgNode node, SvgTextWhitespaceProfile profile)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (profile < SvgTextWhitespaceProfile.Svg2 || profile > SvgTextWhitespaceProfile.Svg11) throw new System.ArgumentOutOfRangeException("profile");
        SvgAttribute? attribute = GetInheritedXmlAttribute(node, SvgAttributeNames.Space);
        SvgXmlSpaceMode mode = LookupXmlSpace(node);
        return new SvgTextWhitespacePolicy(profile, mode, attribute);
    }

    /// <summary>Applies the SVG 1.1 legacy algorithm to one text string using inherited <c>xml:space</c>.</summary>
    /// <param name="node">The node providing the inherited white-space context.</param>
    /// <param name="value">The XML-normalized character data to process.</param>
    /// <returns>A newly allocated processed text string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public string ApplyLegacyTextWhitespace(SvgNode node, string value)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (value == null) throw new System.ArgumentNullException("value");
        bool preserve = LookupXmlSpace(node) == SvgXmlSpaceMode.Preserve;
        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        int index = 0;
        bool previousSpace = false;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n')
            {
                if (current == '\r' && index + 1 < value.Length && value[index + 1] == '\n') index = index + 1;
                if (preserve) builder.Append(' ');
                index = index + 1;
                continue;
            }
            if (current == '\t') current = ' ';
            if (!preserve && current == ' ')
            {
                if (builder.Length != 0 && !previousSpace)
                {
                    builder.Append(' ');
                    previousSpace = true;
                }
                index = index + 1;
                continue;
            }
            builder.Append(current);
            previousSpace = false;
            index = index + 1;
        }
        if (!preserve && builder.Length != 0 && builder[builder.Length - 1] == ' ') builder.Length = builder.Length - 1;
        return builder.ToString();
    }

    /// <summary>Tests two node subtrees for namespace-aware semantic equality.</summary>
    /// <param name="first">The first node.</param>
    /// <param name="second">The second node.</param>
    /// <returns><c>true</c> when node kinds, expanded names, values, unordered attributes, and ordered descendants are equal.</returns>
    public bool DeepEquals(SvgNode? first, SvgNode? second)
    {
        if (object.ReferenceEquals(first, second)) return true;
        if (first == null || second == null) return false;
        return DeepEqualsCore(first, second);
    }

    internal SvgMutationResult AdoptIntoDocument(SvgDocument? document, SvgNode? node)
    {
        if (document == null || node == null) return Failure(SvgMutationError.InvalidArgument, "A document and node are required.");
        if (node.Kind == SvgNodeKind.Document) return Failure(SvgMutationError.InvalidChild, "A document node cannot be adopted.");
        SvgNode? oldParent = node.Parent;
        SvgDocument? oldDocument = node.OwnerDocument;
        if (oldParent != null)
        {
            int oldIndex = FindChild(oldParent, node);
            if (oldIndex < 0 || !object.ReferenceEquals(oldParent.OwnerDocument, oldDocument)) return Failure(SvgMutationError.InvalidOwnership, "The node parent and document ownership fields are inconsistent.");
        }
        if (oldParent == null && object.ReferenceEquals(oldDocument, document)) return NodeSuccess(node, null);
        EnsureCanInvalidate(oldParent, null, oldDocument, document);
        int oldIndexForChange = oldParent == null ? -1 : FindChild(oldParent, node);
        long beforeVersion = oldDocument == null ? document.Version : oldDocument.Version;
        PrepareRecording(oldDocument, document);
        CaptureNodeForRecording(oldDocument, document, node);
        if (oldParent != null)
        {
            int oldIndex = FindChild(oldParent, node);
            oldParent.Children.RemoveAt(oldIndex);
            ClearDocumentElement(oldParent, node);
            node.Parent = null;
        }
        long[] versions = CompleteInvalidation(oldParent, null, oldDocument, document);
        AssignNodeOwner(node, document, versions[1]);
        RecordChange(oldDocument, document, CreateNodeChange(SvgChangeKind.NodeMove, node, oldParent, null, oldIndexForChange, -1, beforeVersion, versions[1]));
        return NodeSuccess(node, null);
    }

    /// <summary>Appends a detached attribute to an element.</summary>
    /// <param name="element">The destination element.</param>
    /// <param name="attribute">The detached attribute.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult AppendAttribute(SvgNode? element, SvgAttribute? attribute)
    {
        if (element == null || attribute == null) return Failure(SvgMutationError.InvalidArgument, "An element and attribute are required.");
        return InsertDetachedAttribute(element, element.Attributes.Count, attribute);
    }

    /// <summary>Inserts a detached attribute at a zero-based ordered index.</summary>
    /// <param name="element">The destination element.</param>
    /// <param name="index">The insertion index from zero through the attribute count.</param>
    /// <param name="attribute">The detached attribute.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult InsertAttribute(SvgNode? element, int index, SvgAttribute? attribute)
    {
        if (element == null || attribute == null) return Failure(SvgMutationError.InvalidArgument, "An element and attribute are required.");
        return InsertDetachedAttribute(element, index, attribute);
    }

    /// <summary>Replaces one owned attribute with a detached attribute.</summary>
    /// <param name="element">The owning element.</param>
    /// <param name="current">The attribute to replace.</param>
    /// <param name="replacement">The detached replacement attribute.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult ReplaceAttribute(SvgNode? element, SvgAttribute? current, SvgAttribute? replacement)
    {
        if (element == null || current == null || replacement == null) return Failure(SvgMutationError.InvalidArgument, "An element, current attribute, and replacement are required.");
        if (element.Kind != SvgNodeKind.Element) return Failure(SvgMutationError.InvalidParent, "Only elements can own attributes.");
        int index = FindAttribute(element, current);
        if (index < 0 || !object.ReferenceEquals(current.OwnerElement, element)) return Failure(SvgMutationError.AttributeNotOwned, "The current attribute is not owned by the supplied element.");
        if (!object.ReferenceEquals(current.OwnerDocument, element.OwnerDocument)) return Failure(SvgMutationError.InvalidOwnership, "The current attribute and element ownership fields are inconsistent.");
        if (object.ReferenceEquals(current, replacement)) return AttributeSuccess(replacement, null);
        if (replacement.OwnerElement != null) return Failure(SvgMutationError.AttributeAlreadyAttached, "The replacement attribute is already attached; move it explicitly first.");
        if (HasDuplicateAttribute(element, replacement, current)) return Failure(SvgMutationError.DuplicateAttribute, "The element already has an attribute with the replacement expanded name.");
        SvgDocument? sourceDocument = replacement.OwnerDocument;
        SvgDocument? targetDocument = element.OwnerDocument;
        EnsureCanInvalidate(null, element, sourceDocument, targetDocument);
        long beforeVersion = targetDocument == null ? element.Version : targetDocument.Version;
        PrepareRecording(sourceDocument, targetDocument);
        CaptureAttributeForRecording(sourceDocument, targetDocument, replacement);
        element.Attributes[index] = replacement;
        current.OwnerElement = null;
        replacement.OwnerElement = element;
        long[] versions = CompleteInvalidation(null, element, sourceDocument, targetDocument);
        AssignAttributeOwner(current, null, null, versions[1]);
        AssignAttributeOwner(replacement, element, targetDocument, versions[1]);
        SvgChange change = CreateAttributeChange(SvgChangeKind.AttributeReplace, replacement, element, element, index, index, beforeVersion, versions[1]);
        change.RemovedAttribute = current;
        RecordChange(sourceDocument, targetDocument, change);
        return AttributeSuccess(replacement, current);
    }

    /// <summary>Removes one owned attribute and clears its ownership.</summary>
    /// <param name="element">The owning element.</param>
    /// <param name="attribute">The attribute to remove.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult RemoveAttribute(SvgNode? element, SvgAttribute? attribute)
    {
        if (element == null || attribute == null) return Failure(SvgMutationError.InvalidArgument, "An element and attribute are required.");
        if (element.Kind != SvgNodeKind.Element) return Failure(SvgMutationError.InvalidParent, "Only elements can own attributes.");
        int index = FindAttribute(element, attribute);
        if (index < 0 || !object.ReferenceEquals(attribute.OwnerElement, element)) return Failure(SvgMutationError.AttributeNotOwned, "The attribute is not owned by the supplied element.");
        SvgDocument? oldDocument = attribute.OwnerDocument;
        if (!object.ReferenceEquals(oldDocument, element.OwnerDocument)) return Failure(SvgMutationError.InvalidOwnership, "The attribute and element ownership fields are inconsistent.");
        EnsureCanInvalidate(element, null, oldDocument, null);
        long beforeVersion = oldDocument == null ? element.Version : oldDocument.Version;
        PrepareRecording(oldDocument, null);
        element.Attributes.RemoveAt(index);
        attribute.OwnerElement = null;
        long[] versions = CompleteInvalidation(element, null, oldDocument, null);
        AssignAttributeOwner(attribute, null, null, versions[0]);
        RecordChange(oldDocument, null, CreateAttributeChange(SvgChangeKind.AttributeRemove, attribute, element, null, index, -1, beforeVersion, versions[0]));
        return AttributeSuccess(attribute, attribute);
    }

    /// <summary>Moves an attached attribute to a final zero-based index on another element.</summary>
    /// <param name="attribute">The attached attribute to move.</param>
    /// <param name="newOwner">The destination element.</param>
    /// <param name="index">The final destination index, or the destination count to append.</param>
    /// <returns>The mutation result.</returns>
    public SvgMutationResult MoveAttribute(SvgAttribute? attribute, SvgNode? newOwner, int index)
    {
        if (attribute == null || newOwner == null) return Failure(SvgMutationError.InvalidArgument, "An attribute and destination element are required.");
        if (newOwner.Kind != SvgNodeKind.Element) return Failure(SvgMutationError.InvalidParent, "Only elements can own attributes.");
        SvgNode? oldOwner = attribute.OwnerElement;
        if (oldOwner == null) return Failure(SvgMutationError.AttributeNotOwned, "Only an attached attribute can be moved.");
        int oldIndex = FindAttribute(oldOwner, attribute);
        if (oldIndex < 0) return Failure(SvgMutationError.InvalidOwnership, "The attribute owner link is inconsistent with the attribute collection.");
        if (index < 0 || index > newOwner.Attributes.Count) return Failure(SvgMutationError.IndexOutOfRange, "The destination attribute index is outside the valid range.");
        if (HasDuplicateAttribute(newOwner, attribute, object.ReferenceEquals(oldOwner, newOwner) ? attribute : null)) return Failure(SvgMutationError.DuplicateAttribute, "The destination already has an attribute with the same expanded name.");
        SvgDocument? oldDocument = attribute.OwnerDocument;
        SvgDocument? newDocument = newOwner.OwnerDocument;
        if (!object.ReferenceEquals(oldDocument, oldOwner.OwnerDocument)) return Failure(SvgMutationError.InvalidOwnership, "The attribute and old owner ownership fields are inconsistent.");
        int finalIndex = index;
        if (object.ReferenceEquals(oldOwner, newOwner) && finalIndex == newOwner.Attributes.Count) finalIndex = newOwner.Attributes.Count - 1;
        if (object.ReferenceEquals(oldOwner, newOwner) && oldIndex == finalIndex) return AttributeSuccess(attribute, null);
        EnsureCanInvalidate(oldOwner, newOwner, oldDocument, newDocument);
        long beforeVersion = oldDocument == null ? oldOwner.Version : oldDocument.Version;
        PrepareRecording(oldDocument, newDocument);
        oldOwner.Attributes.RemoveAt(oldIndex);
        if (finalIndex > newOwner.Attributes.Count) finalIndex = newOwner.Attributes.Count;
        newOwner.Attributes.Insert(finalIndex, attribute);
        attribute.OwnerElement = newOwner;
        long[] versions = CompleteInvalidation(oldOwner, newOwner, oldDocument, newDocument);
        AssignAttributeOwner(attribute, newOwner, newDocument, versions[1]);
        RecordChange(oldDocument, newDocument, CreateAttributeChange(SvgChangeKind.AttributeMove, attribute, oldOwner, newOwner, oldIndex, finalIndex, beforeVersion, versions[1]));
        return AttributeSuccess(attribute, null);
    }

    static SvgNode CloneNodeCore(SvgNode node, bool deep)
    {
        SvgNode clone = new SvgNode(node.Kind, node.QualifiedName, node.Prefix, node.LocalName, node.NamespaceUri, node.Value);
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count)
        {
            SvgAttribute sourceAttribute = node.Attributes[attributeIndex];
            SvgAttribute cloneAttribute = new SvgAttribute(sourceAttribute.QualifiedName, sourceAttribute.Prefix, sourceAttribute.LocalName, sourceAttribute.NamespaceUri, sourceAttribute.Value);
            cloneAttribute.OwnerElement = clone;
            clone.Attributes.Add(cloneAttribute);
            attributeIndex = attributeIndex + 1;
        }
        if (deep)
        {
            int childIndex = 0;
            while (childIndex < node.Children.Count)
            {
                SvgNode childClone = CloneNodeCore(node.Children[childIndex], true);
                childClone.Parent = clone;
                clone.Children.Add(childClone);
                childIndex = childIndex + 1;
            }
        }
        return clone;
    }

    static void AppendTextContent(SvgNode node, System.Text.StringBuilder builder)
    {
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            SvgNode child = node.Children[childIndex];
            if (child.Kind == SvgNodeKind.Text || child.Kind == SvgNodeKind.CData) builder.Append(child.Value);
            else if (child.Kind == SvgNodeKind.Element || child.Kind == SvgNodeKind.Document) AppendTextContent(child, builder);
            childIndex = childIndex + 1;
        }
    }

    static bool TextReplacementIsNoOp(SvgNode node, string value)
    {
        if (value.Length == 0) return node.Children.Count == 0;
        return node.Children.Count == 1 && node.Children[0].Kind == SvgNodeKind.Text && node.Children[0].Value == value;
    }

    static bool NeedsTextNormalization(SvgNode node)
    {
        bool previousWasText = false;
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            SvgNode child = node.Children[childIndex];
            if (child.Kind == SvgNodeKind.Text)
            {
                if (child.Value.Length == 0 || previousWasText) return true;
                previousWasText = true;
            }
            else
            {
                previousWasText = false;
                if ((child.Kind == SvgNodeKind.Element || child.Kind == SvgNodeKind.Document) && NeedsTextNormalization(child)) return true;
            }
            childIndex = childIndex + 1;
        }
        return false;
    }

    static void NormalizeTextCore(SvgNode node, System.Collections.Generic.List<SvgNode> removed, System.Collections.Generic.List<SvgNode> changed)
    {
        int childIndex = 0;
        SvgNode? previousText = null;
        bool containerChanged = false;
        while (childIndex < node.Children.Count)
        {
            SvgNode child = node.Children[childIndex];
            if (child.Kind == SvgNodeKind.Text)
            {
                if (child.Value.Length == 0)
                {
                    node.Children.RemoveAt(childIndex);
                    child.Parent = null;
                    removed.Add(child);
                    containerChanged = true;
                    continue;
                }
                if (previousText != null)
                {
                    previousText.Value = previousText.Value + child.Value;
                    if (!ContainsNode(changed, previousText)) changed.Add(previousText);
                    node.Children.RemoveAt(childIndex);
                    child.Parent = null;
                    removed.Add(child);
                    containerChanged = true;
                    continue;
                }
                previousText = child;
            }
            else
            {
                previousText = null;
                if (child.Kind == SvgNodeKind.Element || child.Kind == SvgNodeKind.Document) NormalizeTextCore(child, removed, changed);
            }
            childIndex = childIndex + 1;
        }
        if (containerChanged) node.MarkSourceRewritten();
    }

    static bool ContainsNode(System.Collections.Generic.List<SvgNode> nodes, SvgNode node)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], node)) return true;
            index = index + 1;
        }
        return false;
    }

    static void AppendDescendants(SvgNode node, System.Collections.Generic.List<SvgNode> result)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        int childIndex = node.Children.Count - 1;
        while (childIndex >= 0)
        {
            pending.Add(node.Children[childIndex]);
            childIndex = childIndex - 1;
        }
        while (pending.Count != 0)
        {
            int last = pending.Count - 1;
            SvgNode current = pending[last];
            pending.RemoveAt(last);
            result.Add(current);
            childIndex = current.Children.Count - 1;
            while (childIndex >= 0)
            {
                pending.Add(current.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
    }

    static SvgNode? GetNamespaceContextElement(SvgNode? node)
    {
        SvgNode? current = node;
        while (current != null && current.Kind != SvgNodeKind.Element) current = current.Parent;
        return current;
    }

    static SvgAttribute? GetDirectAttribute(SvgNode element, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < element.Attributes.Count)
        {
            SvgAttribute attribute = element.Attributes[index];
            if (attribute.NamespaceUri == namespaceUri && attribute.LocalName == localName) return attribute;
            index = index + 1;
        }
        return null;
    }

    static string ResolveUriReference(string baseUri, string reference)
    {
        return SvgReferenceUri.Resolve(baseUri, reference);
    }

    static bool IsNamespaceDeclaration(SvgAttribute attribute)
    {
        return attribute.NamespaceUri == SvgNamespaces.Xmlns || attribute.QualifiedName == "xmlns" || attribute.Prefix == "xmlns";
    }

    static string GetDeclaredPrefix(SvgAttribute attribute)
    {
        if (attribute.Prefix == "xmlns") return attribute.LocalName;
        return "";
    }

    static bool ContainsString(System.Collections.Generic.List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value) return true;
            index = index + 1;
        }
        return false;
    }

    static bool DeepEqualsCore(SvgNode first, SvgNode second)
    {
        if (first.Kind != second.Kind) return false;
        if (first.NamespaceUri != second.NamespaceUri || first.LocalName != second.LocalName || first.Value != second.Value) return false;
        if (first.Attributes.Count != second.Attributes.Count || first.Children.Count != second.Children.Count) return false;
        bool[] matchedAttributes = new bool[second.Attributes.Count];
        int firstAttributeIndex = 0;
        while (firstAttributeIndex < first.Attributes.Count)
        {
            SvgAttribute firstAttribute = first.Attributes[firstAttributeIndex];
            bool matched = false;
            int secondAttributeIndex = 0;
            while (secondAttributeIndex < second.Attributes.Count)
            {
                SvgAttribute secondAttribute = second.Attributes[secondAttributeIndex];
                if (!matchedAttributes[secondAttributeIndex] && firstAttribute.NamespaceUri == secondAttribute.NamespaceUri && firstAttribute.LocalName == secondAttribute.LocalName && firstAttribute.Value == secondAttribute.Value)
                {
                    matchedAttributes[secondAttributeIndex] = true;
                    matched = true;
                    break;
                }
                secondAttributeIndex = secondAttributeIndex + 1;
            }
            if (!matched) return false;
            firstAttributeIndex = firstAttributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < first.Children.Count)
        {
            if (!DeepEqualsCore(first.Children[childIndex], second.Children[childIndex])) return false;
            childIndex = childIndex + 1;
        }
        return true;
    }

    SvgMutationResult InsertDetachedChild(SvgNode parent, int index, SvgNode child)
    {
        SvgMutationResult? parentFailure = ValidateParent(parent);
        if (parentFailure != null) return parentFailure;
        if (index < 0 || index > parent.Children.Count) return Failure(SvgMutationError.IndexOutOfRange, "The child index is outside the valid insertion range.");
        if (child.Kind == SvgNodeKind.Document) return Failure(SvgMutationError.InvalidChild, "A document node cannot be inserted as a child.");
        if (child.Parent != null) return Failure(SvgMutationError.NodeAlreadyAttached, "The child is already attached; move it explicitly instead.");
        if (WouldCreateCycle(parent, child)) return Failure(SvgMutationError.Cycle, "The insertion would create an ancestry cycle.");
        SvgMutationResult? rootFailure = ValidateDocumentElement(parent, child, null);
        if (rootFailure != null) return rootFailure;
        SvgDocument? sourceDocument = child.OwnerDocument;
        SvgDocument? targetDocument = parent.OwnerDocument;
        EnsureCanInvalidate(null, parent, sourceDocument, targetDocument);
        long beforeVersion = targetDocument == null ? parent.Version : targetDocument.Version;
        PrepareRecording(sourceDocument, targetDocument);
        CaptureNodeForRecording(sourceDocument, targetDocument, child);
        parent.Children.Insert(index, child);
        child.Parent = parent;
        SetDocumentElement(parent, child);
        long[] versions = CompleteInvalidation(null, parent, sourceDocument, targetDocument);
        AssignNodeOwner(child, targetDocument, versions[1]);
        RecordChange(sourceDocument, targetDocument, CreateNodeChange(SvgChangeKind.NodeInsert, child, null, parent, -1, index, beforeVersion, versions[1]));
        return NodeSuccess(child, null);
    }

    SvgMutationResult InsertDetachedAttribute(SvgNode element, int index, SvgAttribute attribute)
    {
        if (element.Kind != SvgNodeKind.Element) return Failure(SvgMutationError.InvalidParent, "Only elements can own attributes.");
        if (index < 0 || index > element.Attributes.Count) return Failure(SvgMutationError.IndexOutOfRange, "The attribute index is outside the valid insertion range.");
        if (attribute.OwnerElement != null) return Failure(SvgMutationError.AttributeAlreadyAttached, "The attribute is already attached; move it explicitly instead.");
        if (HasDuplicateAttribute(element, attribute, null)) return Failure(SvgMutationError.DuplicateAttribute, "The element already has an attribute with the same expanded name.");
        SvgDocument? sourceDocument = attribute.OwnerDocument;
        SvgDocument? targetDocument = element.OwnerDocument;
        EnsureCanInvalidate(null, element, sourceDocument, targetDocument);
        long beforeVersion = targetDocument == null ? element.Version : targetDocument.Version;
        PrepareRecording(sourceDocument, targetDocument);
        CaptureAttributeForRecording(sourceDocument, targetDocument, attribute);
        element.Attributes.Insert(index, attribute);
        attribute.OwnerElement = element;
        long[] versions = CompleteInvalidation(null, element, sourceDocument, targetDocument);
        AssignAttributeOwner(attribute, element, targetDocument, versions[1]);
        RecordChange(sourceDocument, targetDocument, CreateAttributeChange(SvgChangeKind.AttributeInsert, attribute, null, element, -1, index, beforeVersion, versions[1]));
        return AttributeSuccess(attribute, null);
    }

    static void PrepareRecording(SvgDocument? oldDocument, SvgDocument? newDocument)
    {
        SvgChangeSet? oldSet = oldDocument == null ? null : oldDocument.ActiveChangeSet;
        SvgChangeSet? newSet = newDocument == null ? null : newDocument.ActiveChangeSet;
        if (oldSet != null)
        {
            oldSet.CaptureDocument(oldDocument);
            oldSet.CaptureDocument(newDocument);
        }
        if (newSet != null && !object.ReferenceEquals(newSet, oldSet))
        {
            newSet.CaptureDocument(oldDocument);
            newSet.CaptureDocument(newDocument);
        }
    }

    static void CaptureNodeForRecording(SvgDocument? oldDocument, SvgDocument? newDocument, SvgNode node)
    {
        SvgChangeSet? oldSet = oldDocument == null ? null : oldDocument.ActiveChangeSet;
        SvgChangeSet? newSet = newDocument == null ? null : newDocument.ActiveChangeSet;
        if (oldSet != null) oldSet.CaptureExternalNode(node);
        if (newSet != null && !object.ReferenceEquals(newSet, oldSet)) newSet.CaptureExternalNode(node);
    }

    static void CaptureAttributeForRecording(SvgDocument? oldDocument, SvgDocument? newDocument, SvgAttribute attribute)
    {
        SvgChangeSet? oldSet = oldDocument == null ? null : oldDocument.ActiveChangeSet;
        SvgChangeSet? newSet = newDocument == null ? null : newDocument.ActiveChangeSet;
        if (oldSet != null) oldSet.CaptureExternalAttribute(attribute);
        if (newSet != null && !object.ReferenceEquals(newSet, oldSet)) newSet.CaptureExternalAttribute(attribute);
    }

    static void RecordChange(SvgDocument? oldDocument, SvgDocument? newDocument, SvgChange change)
    {
        SvgChangeSet? oldSet = oldDocument == null ? null : oldDocument.ActiveChangeSet;
        SvgChangeSet? newSet = newDocument == null ? null : newDocument.ActiveChangeSet;
        if (oldSet != null) oldSet.Record(change);
        if (newSet != null && !object.ReferenceEquals(newSet, oldSet)) newSet.Record(change);
    }

    static SvgChange CreateNodeChange(SvgChangeKind kind, SvgNode? node, SvgNode? oldParent, SvgNode? newParent, int oldIndex, int newIndex, long beforeVersion, long afterVersion)
    {
        SvgChange change = new SvgChange();
        change.Kind = kind;
        change.Node = node;
        change.OldParent = oldParent;
        change.NewParent = newParent;
        change.OldIndex = oldIndex;
        change.NewIndex = newIndex;
        change.Source = node == null ? null : node.SourceSpan;
        change.BeforeVersion = beforeVersion;
        change.AfterVersion = afterVersion;
        return change;
    }

    static SvgChange CreateAttributeChange(SvgChangeKind kind, SvgAttribute? attribute, SvgNode? oldOwner, SvgNode? newOwner, int oldIndex, int newIndex, long beforeVersion, long afterVersion)
    {
        SvgChange change = new SvgChange();
        change.Kind = kind;
        change.Attribute = attribute;
        change.OldParent = oldOwner;
        change.NewParent = newOwner;
        change.OldIndex = oldIndex;
        change.NewIndex = newIndex;
        change.Source = attribute == null ? null : attribute.SourceSpan;
        change.BeforeVersion = beforeVersion;
        change.AfterVersion = afterVersion;
        return change;
    }

    static SvgMutationResult? ValidateParent(SvgNode parent)
    {
        if (parent.Kind != SvgNodeKind.Document && parent.Kind != SvgNodeKind.Element) return Failure(SvgMutationError.InvalidParent, "Only document and element nodes can contain children.");
        if (parent.Kind == SvgNodeKind.Document && (parent.OwnerDocument == null || !object.ReferenceEquals(parent.OwnerDocument.DocumentNode, parent))) return Failure(SvgMutationError.InvalidParent, "The parent is not a document's connected document node.");
        return null;
    }

    static SvgMutationResult? ValidateDocumentElement(SvgNode parent, SvgNode child, SvgNode? replaced)
    {
        if (parent.Kind != SvgNodeKind.Document || child.Kind != SvgNodeKind.Element) return null;
        SvgNode? existing = (parent.OwnerDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DocumentElement;
        if (existing == null || object.ReferenceEquals(existing, child) || object.ReferenceEquals(existing, replaced)) return null;
        return Failure(SvgMutationError.MultipleDocumentElements, "A document can contain only one document element.");
    }

    static bool WouldCreateCycle(SvgNode parent, SvgNode child)
    {
        SvgNode? current = parent;
        while (current != null)
        {
            if (object.ReferenceEquals(current, child)) return true;
            current = current.Parent;
        }
        return false;
    }

    static bool HasDuplicateAttribute(SvgNode element, SvgAttribute attribute, SvgAttribute? ignored)
    {
        int index = 0;
        while (index < element.Attributes.Count)
        {
            SvgAttribute current = element.Attributes[index];
            if (!object.ReferenceEquals(current, ignored) && current.NamespaceUri == attribute.NamespaceUri && current.LocalName == attribute.LocalName) return true;
            index = index + 1;
        }
        return false;
    }

    static int FindChild(SvgNode parent, SvgNode child)
    {
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (object.ReferenceEquals(parent.Children[index], child)) return index;
            index = index + 1;
        }
        return -1;
    }

    static int FindAttribute(SvgNode element, SvgAttribute attribute)
    {
        int index = 0;
        while (index < element.Attributes.Count)
        {
            if (object.ReferenceEquals(element.Attributes[index], attribute)) return index;
            index = index + 1;
        }
        return -1;
    }

    static void ClearDocumentElement(SvgNode parent, SvgNode child)
    {
        if (parent.Kind == SvgNodeKind.Document && object.ReferenceEquals((parent.OwnerDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DocumentElement, child)) parent.OwnerDocument.DocumentElement = null;
    }

    static void SetDocumentElement(SvgNode parent, SvgNode child)
    {
        if (parent.Kind == SvgNodeKind.Document && child.Kind == SvgNodeKind.Element) (parent.OwnerDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DocumentElement = child;
    }

    static void UpdateDocumentElementAfterReplacement(SvgNode parent, SvgNode current, SvgNode replacement)
    {
        if (parent.Kind != SvgNodeKind.Document || !object.ReferenceEquals((parent.OwnerDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DocumentElement, current)) return;
        parent.OwnerDocument.DocumentElement = replacement.Kind == SvgNodeKind.Element ? replacement : null;
    }

    static void EnsureCanInvalidate(SvgNode? oldContainer, SvgNode? newContainer, SvgDocument? oldDocument, SvgDocument? newDocument)
    {
        if (oldDocument != null && oldDocument.Version == System.Int64.MaxValue) throw new System.OverflowException("The source document version cannot be incremented.");
        if (newDocument != null && !object.ReferenceEquals(newDocument, oldDocument) && newDocument.Version == System.Int64.MaxValue) throw new System.OverflowException("The destination document version cannot be incremented.");
        if (oldDocument == null && oldContainer != null && oldContainer.Version == System.Int64.MaxValue) throw new System.OverflowException("The source node version cannot be incremented.");
        if (newDocument == null && newContainer != null && !object.ReferenceEquals(newContainer, oldContainer) && newContainer.Version == System.Int64.MaxValue) throw new System.OverflowException("The destination node version cannot be incremented.");
    }

    static long[] CompleteInvalidation(SvgNode? oldContainer, SvgNode? newContainer, SvgDocument? oldDocument, SvgDocument? newDocument)
    {
        long oldVersion = 0;
        long newVersion = 0;
        if (oldDocument != null) oldVersion = oldDocument.MarkDirty();
        else if (oldContainer != null) oldVersion = oldContainer.MarkDirty();
        if (newDocument != null)
        {
            if (object.ReferenceEquals(newDocument, oldDocument)) newVersion = oldVersion;
            else newVersion = newDocument.MarkDirty();
        }
        else if (newContainer != null)
        {
            if (oldDocument == null && object.ReferenceEquals(newContainer, oldContainer)) newVersion = oldVersion;
            else newVersion = newContainer.MarkDirty();
        }
        if (oldContainer != null)
        {
            oldContainer.IsDirty = true;
            oldContainer.Version = oldVersion;
            oldContainer.MarkSourceRewritten();
        }
        if (newContainer != null)
        {
            newContainer.IsDirty = true;
            newContainer.Version = newVersion;
            newContainer.MarkSourceRewritten();
        }
        return new long[] { oldVersion, newVersion };
    }

    static void AssignNodeOwner(SvgNode node, SvgDocument? document, long version)
    {
        node.MarkSourceRewritten();
        AssignNodeOwnerCore(node, document, version);
    }

    static void AssignNodeOwnerCore(SvgNode node, SvgDocument? document, long version)
    {
        node.OwnerDocument = document;
        node.Version = version;
        node.IsDirty = true;
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count)
        {
            AssignAttributeOwnerCore(node.Attributes[attributeIndex], node, document, version);
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            SvgNode child = node.Children[childIndex];
            child.Parent = node;
            AssignNodeOwnerCore(child, document, version);
            childIndex = childIndex + 1;
        }
    }

    static void AssignAttributeOwner(SvgAttribute attribute, SvgNode? element, SvgDocument? document, long version)
    {
        attribute.MarkSourceRewritten();
        AssignAttributeOwnerCore(attribute, element, document, version);
    }

    static void AssignAttributeOwnerCore(SvgAttribute attribute, SvgNode? element, SvgDocument? document, long version)
    {
        attribute.OwnerElement = element;
        attribute.OwnerDocument = document;
        attribute.Version = version;
        attribute.IsDirty = true;
    }

    static SvgMutationResult NodeSuccess(SvgNode node, SvgNode? removed)
    {
        return new SvgMutationResult(true, SvgMutationError.None, node, null, removed, null, "");
    }

    static SvgMutationResult AttributeSuccess(SvgAttribute attribute, SvgAttribute? removed)
    {
        return new SvgMutationResult(true, SvgMutationError.None, null, attribute, null, removed, "");
    }

    static SvgMutationResult Failure(SvgMutationError error, string message)
    {
        return new SvgMutationResult(false, error, null, null, null, null, message);
    }
}
