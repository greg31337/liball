namespace svg;

internal class SvgNodeTransactionState
{
    internal SvgNode Node;
    internal System.Collections.Generic.List<SvgNode> Children;
    internal System.Collections.Generic.List<SvgAttribute> Attributes;
    internal SvgNode? Parent;
    internal SvgDocument? OwnerDocument;
    internal string Value;
    internal SvgSourceMapKind SourceMapKind;
    internal bool IsDirty;
    internal long Version;

    internal SvgNodeTransactionState(SvgNode node)
    {
        Node = node;
        Children = new System.Collections.Generic.List<SvgNode>(node.Children);
        Attributes = new System.Collections.Generic.List<SvgAttribute>(node.Attributes);
        Parent = node.Parent;
        OwnerDocument = node.OwnerDocument;
        Value = node.Value;
        SourceMapKind = node.SourceMapKind;
        IsDirty = node.IsDirty;
        Version = node.Version;
    }
}

internal class SvgAttributeTransactionState
{
    internal SvgAttribute Attribute;
    internal SvgNode? OwnerElement;
    internal SvgDocument? OwnerDocument;
    internal string Value;
    internal SvgSourceMapKind SourceMapKind;
    internal bool IsDirty;
    internal long Version;

    internal SvgAttributeTransactionState(SvgAttribute attribute)
    {
        Attribute = attribute;
        OwnerElement = attribute.OwnerElement;
        OwnerDocument = attribute.OwnerDocument;
        Value = attribute.Value;
        SourceMapKind = attribute.SourceMapKind;
        IsDirty = attribute.IsDirty;
        Version = attribute.Version;
    }
}

internal class SvgDocumentTransactionState
{
    internal SvgDocument Document;
    internal long Version;
    internal bool IsDirty;
    internal SvgNode? DocumentElement;
    internal System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    internal System.Collections.Generic.List<SvgNodeTransactionState> Nodes;
    internal System.Collections.Generic.List<SvgAttributeTransactionState> Attributes;

    internal SvgDocumentTransactionState(SvgDocument document)
    {
        Document = document;
        Version = document.Version;
        IsDirty = document.IsDirty;
        DocumentElement = document.DocumentElement;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        System.Collections.Generic.List<SvgDiagnostic> indexDiagnostics = document.Index.GetDuplicateIdDiagnostics();
        int diagnosticIndex = 0;
        while (diagnosticIndex < document.Diagnostics.Count)
        {
            if (!ContainsDiagnostic(indexDiagnostics, document.Diagnostics[diagnosticIndex])) Diagnostics.Add(document.Diagnostics[diagnosticIndex]);
            diagnosticIndex = diagnosticIndex + 1;
        }
        Nodes = new System.Collections.Generic.List<SvgNodeTransactionState>();
        Attributes = new System.Collections.Generic.List<SvgAttributeTransactionState>();
        CaptureTree(document.DocumentNode);
    }

    internal bool ContainsNode(SvgNode node)
    {
        int index = 0;
        while (index < Nodes.Count)
        {
            if (object.ReferenceEquals(Nodes[index].Node, node)) return true;
            index = index + 1;
        }
        return false;
    }

    internal bool ContainsAttribute(SvgAttribute attribute)
    {
        int index = 0;
        while (index < Attributes.Count)
        {
            if (object.ReferenceEquals(Attributes[index].Attribute, attribute)) return true;
            index = index + 1;
        }
        return false;
    }

    internal void Restore()
    {
        Document.Index.Invalidate();
        System.Collections.Generic.HashSet<SvgNode> originalNodes = new System.Collections.Generic.HashSet<SvgNode>();
        System.Collections.Generic.HashSet<SvgAttribute> originalAttributes = new System.Collections.Generic.HashSet<SvgAttribute>();
        int index = 0;
        while (index < Nodes.Count)
        {
            originalNodes.Add(Nodes[index].Node);
            index = index + 1;
        }
        index = 0;
        while (index < Attributes.Count)
        {
            originalAttributes.Add(Attributes[index].Attribute);
            index = index + 1;
        }
        System.Collections.Generic.List<SvgNode> currentNodes = CollectTree(Document.DocumentNode);
        System.Collections.Generic.List<SvgAttribute> currentAttributes = new System.Collections.Generic.List<SvgAttribute>();
        index = 0;
        while (index < currentNodes.Count)
        {
            int currentAttributeIndex = 0;
            while (currentAttributeIndex < currentNodes[index].Attributes.Count)
            {
                currentAttributes.Add(currentNodes[index].Attributes[currentAttributeIndex]);
                currentAttributeIndex = currentAttributeIndex + 1;
            }
            index = index + 1;
        }
        index = 0;
        while (index < Nodes.Count)
        {
            SvgNodeTransactionState state = Nodes[index];
            if (state.Node.Parent != null && !object.ReferenceEquals(state.Node.Parent, state.Parent)) RemoveNodeReference(state.Node.Parent, state.Node);
            index = index + 1;
        }
        index = 0;
        while (index < Attributes.Count)
        {
            SvgAttributeTransactionState state = Attributes[index];
            if (state.Attribute.OwnerElement != null && !object.ReferenceEquals(state.Attribute.OwnerElement, state.OwnerElement)) RemoveAttributeReference(state.Attribute.OwnerElement, state.Attribute);
            index = index + 1;
        }
        index = 0;
        while (index < currentNodes.Count)
        {
            SvgNode current = currentNodes[index];
            if (!originalNodes.Contains(current))
            {
                RemoveOriginalChildren(current, originalNodes);
                RemoveOriginalAttributes(current, originalAttributes);
            }
            index = index + 1;
        }
        index = 0;
        while (index < Nodes.Count)
        {
            RestoreNode(Nodes[index]);
            index = index + 1;
        }
        index = 0;
        while (index < Attributes.Count)
        {
            RestoreAttribute(Attributes[index]);
            index = index + 1;
        }
        index = 0;
        while (index < currentNodes.Count)
        {
            SvgNode current = currentNodes[index];
            if (!originalNodes.Contains(current) && current.Parent != null && originalNodes.Contains(current.Parent) && !ContainsNodeReference(current.Parent.Children, current))
            {
                current.Parent = null;
                AssignDetachedOwner(current);
            }
            index = index + 1;
        }
        index = 0;
        while (index < currentAttributes.Count)
        {
            SvgAttribute attribute = currentAttributes[index];
            if (!originalAttributes.Contains(attribute) && attribute.OwnerElement != null && originalNodes.Contains(attribute.OwnerElement) && !ContainsAttributeReference(attribute.OwnerElement.Attributes, attribute))
            {
                attribute.OwnerElement = null;
                attribute.OwnerDocument = null;
            }
            index = index + 1;
        }
        Document.Version = Version;
        Document.IsDirty = IsDirty;
        Document.DocumentElement = DocumentElement;
        Document.Diagnostics.Clear();
        index = 0;
        while (index < Diagnostics.Count)
        {
            Document.Diagnostics.Add(Diagnostics[index]);
            index = index + 1;
        }
        Document.Index.Invalidate();
        Document.Index.Refresh();
    }

    void CaptureTree(SvgNode root)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectTree(root);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            SvgNode node = nodes[nodeIndex];
            Nodes.Add(new SvgNodeTransactionState(node));
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                Attributes.Add(new SvgAttributeTransactionState(node.Attributes[attributeIndex]));
                attributeIndex = attributeIndex + 1;
            }
            nodeIndex = nodeIndex + 1;
        }
    }

    internal static System.Collections.Generic.List<SvgNode> CollectTree(SvgNode root)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.HashSet<SvgNode> visited = new System.Collections.Generic.HashSet<SvgNode>();
        stack.Add(root);
        while (stack.Count != 0)
        {
            int last = stack.Count - 1;
            SvgNode node = stack[last];
            stack.RemoveAt(last);
            if (!visited.Add(node)) continue;
            result.Add(node);
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                stack.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        return result;
    }

    internal static void RestoreNode(SvgNodeTransactionState state)
    {
        state.Node.Children.Clear();
        int childIndex = 0;
        while (childIndex < state.Children.Count)
        {
            state.Node.Children.Add(state.Children[childIndex]);
            childIndex = childIndex + 1;
        }
        state.Node.Attributes.Clear();
        int attributeIndex = 0;
        while (attributeIndex < state.Attributes.Count)
        {
            state.Node.Attributes.Add(state.Attributes[attributeIndex]);
            attributeIndex = attributeIndex + 1;
        }
        state.Node.Parent = state.Parent;
        state.Node.OwnerDocument = state.OwnerDocument;
        state.Node.Value = state.Value;
        state.Node.SourceMapKind = state.SourceMapKind;
        state.Node.IsDirty = state.IsDirty;
        state.Node.Version = state.Version;
    }

    internal static void RestoreAttribute(SvgAttributeTransactionState state)
    {
        state.Attribute.OwnerElement = state.OwnerElement;
        state.Attribute.OwnerDocument = state.OwnerDocument;
        state.Attribute.Value = state.Value;
        state.Attribute.SourceMapKind = state.SourceMapKind;
        state.Attribute.IsDirty = state.IsDirty;
        state.Attribute.Version = state.Version;
    }

    internal static void RemoveOriginalChildren(SvgNode node, System.Collections.Generic.HashSet<SvgNode> originals)
    {
        int index = node.Children.Count - 1;
        while (index >= 0)
        {
            if (originals.Contains(node.Children[index])) node.Children.RemoveAt(index);
            index = index - 1;
        }
    }

    internal static void RemoveOriginalAttributes(SvgNode node, System.Collections.Generic.HashSet<SvgAttribute> originals)
    {
        int index = node.Attributes.Count - 1;
        while (index >= 0)
        {
            if (originals.Contains(node.Attributes[index])) node.Attributes.RemoveAt(index);
            index = index - 1;
        }
    }

    internal static void AssignDetachedOwner(SvgNode root)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectTree(root);
        int index = 0;
        while (index < nodes.Count)
        {
            SvgNode node = nodes[index];
            node.OwnerDocument = null;
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                node.Attributes[attributeIndex].OwnerDocument = null;
                node.Attributes[attributeIndex].OwnerElement = node;
                attributeIndex = attributeIndex + 1;
            }
            index = index + 1;
        }
    }

    internal static void RemoveNodeReference(SvgNode parent, SvgNode node)
    {
        int index = parent.Children.Count - 1;
        while (index >= 0)
        {
            if (object.ReferenceEquals(parent.Children[index], node)) parent.Children.RemoveAt(index);
            index = index - 1;
        }
    }

    internal static void RemoveAttributeReference(SvgNode owner, SvgAttribute attribute)
    {
        int index = owner.Attributes.Count - 1;
        while (index >= 0)
        {
            if (object.ReferenceEquals(owner.Attributes[index], attribute)) owner.Attributes.RemoveAt(index);
            index = index - 1;
        }
    }

    internal static bool ContainsNodeReference(System.Collections.Generic.List<SvgNode> nodes, SvgNode node)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], node)) return true;
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsAttributeReference(System.Collections.Generic.List<SvgAttribute> attributes, SvgAttribute attribute)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (object.ReferenceEquals(attributes[index], attribute)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ContainsDiagnostic(System.Collections.Generic.List<SvgDiagnostic> diagnostics, SvgDiagnostic diagnostic)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (object.ReferenceEquals(diagnostics[index], diagnostic)) return true;
            index = index + 1;
        }
        return false;
    }
}

internal class SvgExternalNodeTransactionState
{
    internal SvgNode Root;
    internal System.Collections.Generic.List<SvgNodeTransactionState> Nodes;
    internal System.Collections.Generic.List<SvgAttributeTransactionState> Attributes;

    internal SvgExternalNodeTransactionState(SvgNode root)
    {
        Root = root;
        Nodes = new System.Collections.Generic.List<SvgNodeTransactionState>();
        Attributes = new System.Collections.Generic.List<SvgAttributeTransactionState>();
        System.Collections.Generic.List<SvgNode> nodes = SvgDocumentTransactionState.CollectTree(root);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            SvgNode node = nodes[nodeIndex];
            Nodes.Add(new SvgNodeTransactionState(node));
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                Attributes.Add(new SvgAttributeTransactionState(node.Attributes[attributeIndex]));
                attributeIndex = attributeIndex + 1;
            }
            nodeIndex = nodeIndex + 1;
        }
    }

    internal bool ContainsNode(SvgNode node)
    {
        int index = 0;
        while (index < Nodes.Count)
        {
            if (object.ReferenceEquals(Nodes[index].Node, node)) return true;
            index = index + 1;
        }
        return false;
    }

    internal bool ContainsAttribute(SvgAttribute attribute)
    {
        int index = 0;
        while (index < Attributes.Count)
        {
            if (object.ReferenceEquals(Attributes[index].Attribute, attribute)) return true;
            index = index + 1;
        }
        return false;
    }

    internal void Restore()
    {
        System.Collections.Generic.HashSet<SvgNode> originalNodes = new System.Collections.Generic.HashSet<SvgNode>();
        System.Collections.Generic.HashSet<SvgAttribute> originalAttributes = new System.Collections.Generic.HashSet<SvgAttribute>();
        int index = 0;
        while (index < Nodes.Count)
        {
            originalNodes.Add(Nodes[index].Node);
            index = index + 1;
        }
        index = 0;
        while (index < Attributes.Count)
        {
            originalAttributes.Add(Attributes[index].Attribute);
            index = index + 1;
        }
        System.Collections.Generic.List<SvgNode> currentNodes = SvgDocumentTransactionState.CollectTree(Root);
        System.Collections.Generic.List<SvgAttribute> currentAttributes = new System.Collections.Generic.List<SvgAttribute>();
        index = 0;
        while (index < currentNodes.Count)
        {
            int attributeIndex = 0;
            while (attributeIndex < currentNodes[index].Attributes.Count)
            {
                currentAttributes.Add(currentNodes[index].Attributes[attributeIndex]);
                attributeIndex = attributeIndex + 1;
            }
            index = index + 1;
        }
        index = 0;
        while (index < Nodes.Count)
        {
            SvgNodeTransactionState state = Nodes[index];
            if (state.Node.Parent != null && !object.ReferenceEquals(state.Node.Parent, state.Parent)) SvgDocumentTransactionState.RemoveNodeReference(state.Node.Parent, state.Node);
            index = index + 1;
        }
        index = 0;
        while (index < Attributes.Count)
        {
            SvgAttributeTransactionState state = Attributes[index];
            if (state.Attribute.OwnerElement != null && !object.ReferenceEquals(state.Attribute.OwnerElement, state.OwnerElement)) SvgDocumentTransactionState.RemoveAttributeReference(state.Attribute.OwnerElement, state.Attribute);
            index = index + 1;
        }
        index = 0;
        while (index < currentNodes.Count)
        {
            if (!originalNodes.Contains(currentNodes[index]))
            {
                SvgDocumentTransactionState.RemoveOriginalChildren(currentNodes[index], originalNodes);
                SvgDocumentTransactionState.RemoveOriginalAttributes(currentNodes[index], originalAttributes);
            }
            index = index + 1;
        }
        index = 0;
        while (index < Nodes.Count)
        {
            SvgDocumentTransactionState.RestoreNode(Nodes[index]);
            index = index + 1;
        }
        index = 0;
        while (index < Attributes.Count)
        {
            SvgDocumentTransactionState.RestoreAttribute(Attributes[index]);
            index = index + 1;
        }
        index = 0;
        while (index < currentNodes.Count)
        {
            SvgNode node = currentNodes[index];
            if (!originalNodes.Contains(node) && node.Parent != null && originalNodes.Contains(node.Parent) && !SvgDocumentTransactionState.ContainsNodeReference(node.Parent.Children, node))
            {
                node.Parent = null;
                SvgDocumentTransactionState.AssignDetachedOwner(node);
            }
            index = index + 1;
        }
        index = 0;
        while (index < currentAttributes.Count)
        {
            SvgAttribute attribute = currentAttributes[index];
            if (!originalAttributes.Contains(attribute) && attribute.OwnerElement != null && originalNodes.Contains(attribute.OwnerElement) && !SvgDocumentTransactionState.ContainsAttributeReference(attribute.OwnerElement.Attributes, attribute))
            {
                attribute.OwnerElement = null;
                attribute.OwnerDocument = null;
            }
            index = index + 1;
        }
    }
}

internal class SvgExternalAttributeTransactionState
{
    internal SvgAttributeTransactionState State;

    internal SvgExternalAttributeTransactionState(SvgAttribute attribute)
    {
        State = new SvgAttributeTransactionState(attribute);
    }

    internal void Restore()
    {
        if (State.Attribute.OwnerElement != null && !object.ReferenceEquals(State.Attribute.OwnerElement, State.OwnerElement)) SvgDocumentTransactionState.RemoveAttributeReference(State.Attribute.OwnerElement, State.Attribute);
        SvgDocumentTransactionState.RestoreAttribute(State);
    }
}
