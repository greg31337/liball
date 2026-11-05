using System;
using System.Collections.Generic;

namespace xml;

internal class TreeRangeSpan
{
    internal XmlNode Node;
    internal long Start;
    internal long End;
    internal long ContentStart;
    internal long ContentEnd;

    internal TreeRangeSpan(XmlNode node, long start)
    {
        Node = node;
        Start = start;
        End = start;
        ContentStart = start;
        ContentEnd = start;
    }
}

internal class TreeRangePositionState
{
    internal XmlNode StartContainer;
    internal int StartOffset;
    internal XmlNode EndContainer;
    internal int EndOffset;
    internal long Counter;
    internal long StartPosition;
    internal long EndPosition;
    internal bool FoundStart;
    internal bool FoundEnd;
    internal List<TreeRangeSpan> Spans;

    internal TreeRangePositionState(XmlNode startContainer, int startOffset, XmlNode endContainer, int endOffset)
    {
        StartContainer = startContainer;
        StartOffset = startOffset;
        EndContainer = endContainer;
        EndOffset = endOffset;
        Counter = 0;
        StartPosition = 0;
        EndPosition = 0;
        FoundStart = false;
        FoundEnd = false;
        Spans = new List<TreeRangeSpan>();
    }
}

internal class TreeRangeNavigation
{
    internal TreeNavigation Navigation;

    internal TreeRangeNavigation(TreeNavigation navigation)
    {
        Navigation = navigation;
    }

    internal XmlNodeSelection SelectXmlRangeNodes(XmlRange range)
    {
        Guard.NotNull(range, "range");
        XmlNodeSelection? snapshot = range.Snapshot;
        if (snapshot != null && snapshot.IsFrozen)
        {
            XmlDocument? snapshotDocument = snapshot.Document == null ? range.Document : snapshot.Document;
            XmlNode[] frozenNodes = snapshot.Nodes == null ? [] : Navigation.CopyXmlNavigationNodes(snapshot.Nodes);
            XmlNodeSelection frozen = Navigation.CreateXmlNavigationSelection(snapshotDocument, frozenNodes, range, true);
            frozen.MutationVersion = snapshot.MutationVersion;
            return frozen;
        }

        if (!range.IsValid)
        {
            throw new InvalidOperationException("The XML range is invalid.");
        }

        XmlDocument? optionalDocument = range.Document;
        if (optionalDocument == null)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentException("A live XML range requires a document.", "range");
        }

        XmlDocument document = optionalDocument;
        if (range.MutationVersion != document.MutationVersion)
        {
            InvalidateXmlTreeRange(range);
            throw new InvalidOperationException("The XML range was invalidated by a document mutation.");
        }

        XmlNode? optionalStart = range.StartContainer;
        XmlNode? optionalEnd = range.EndContainer;
        if (optionalStart == null || optionalEnd == null)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentException("A live XML range requires both boundary containers.", "range");
        }

        XmlNode start = optionalStart;
        XmlNode end = optionalEnd;
        if (start.OwnerDocument != document || end.OwnerDocument != document)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentException("Both XML range boundaries must belong to the range document.", "range");
        }

        ValidateXmlRangeOffset(range, start, range.StartOffset, "StartOffset");
        ValidateXmlRangeOffset(range, end, range.EndOffset, "EndOffset");
        bool startAttribute = start.Kind == XmlNodeKind.Attribute || start.Kind == XmlNodeKind.Namespace;
        bool endAttribute = end.Kind == XmlNodeKind.Attribute || end.Kind == XmlNodeKind.Namespace;
        if (startAttribute || endAttribute)
        {
            return SelectXmlAttributeRange(range, document, start, end);
        }

        XmlNode? root = document.DocumentNode;
        if (root == null || Navigation.FindXmlNavigationRoot(start) != root || Navigation.FindXmlNavigationRoot(end) != root)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentException("Both XML range boundaries must be attached to the range document.", "range");
        }

        TreeRangePositionState state = new TreeRangePositionState(start, range.StartOffset, end, range.EndOffset);
        TraverseXmlRangePositions(root, state);
        if (!state.FoundStart || !state.FoundEnd)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentException("An XML range boundary could not be located.", "range");
        }

        if (state.StartPosition > state.EndPosition)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentException("The XML range start must not follow its end.", "range");
        }

        List<XmlNode> selected = new List<XmlNode>();
        if (state.StartPosition != state.EndPosition)
        {
            int index = 0;
            while (index < state.Spans.Count)
            {
                TreeRangeSpan span = state.Spans[index];
                bool fullyCovered = state.StartPosition <= span.Start && span.End <= state.EndPosition;
                bool partialBoundary = IsXmlRangeCharacterNode(span.Node) && (span.Node == start || span.Node == end) && span.ContentStart < state.EndPosition && state.StartPosition < span.ContentEnd;
                if (span.Node != root && (fullyCovered || partialBoundary))
                {
                    selected.Add(span.Node);
                }

                index = index + 1;
            }
        }

        return Navigation.CreateXmlNavigationSelection(document, selected.ToArray(), range, false);
    }

    internal XmlNodeSelection SelectXmlAttributeRange(XmlRange range, XmlDocument document, XmlNode start, XmlNode end)
    {
        if (start != end || start.Kind != XmlNodeKind.Attribute && start.Kind != XmlNodeKind.Namespace)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentException("An attribute or namespace range cannot cross node boundaries.", "range");
        }

        XmlNode? root = document.DocumentNode;
        if (root == null || Navigation.FindXmlNavigationRoot(start) != root)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentException("The XML range attribute must be attached to the range document.", "range");
        }

        if (range.StartOffset > range.EndOffset)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentException("The XML range start must not follow its end.", "range");
        }

        XmlNode[] nodes = range.StartOffset == range.EndOffset ? [] : [start];
        return Navigation.CreateXmlNavigationSelection(document, nodes, range, false);
    }

    internal void ValidateXmlRangeOffset(XmlRange range, XmlNode container, int offset, string parameterName)
    {
        if (offset < 0)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentOutOfRangeException(parameterName);
        }

        int maximum;
        if (IsXmlRangeContainerNode(container))
        {
            maximum = container.Children == null ? 0 : container.Children.Length;
        }
        else
        {
            maximum = container.Value == null ? 0 : container.Value.Length;
        }

        if (offset > maximum)
        {
            InvalidateXmlTreeRange(range);
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    internal void TraverseXmlRangePositions(XmlNode node, TreeRangePositionState state)
    {
        long before = state.Counter;
        state.Counter = state.Counter + 1;
        TreeRangeSpan span = new TreeRangeSpan(node, before);
        state.Spans.Add(span);
        if (IsXmlRangeContainerNode(node))
        {
            RecordXmlRangeContainerBoundary(node, 0, state);
            XmlNode[] children = node.Children == null ? [] : node.Children;
            int index = 0;
            while (index < children.Length)
            {
                TraverseXmlRangePositions(children[index], state);
                RecordXmlRangeContainerBoundary(node, index + 1, state);
                index = index + 1;
            }
        }
        else
        {
            int length = node.Value == null ? 0 : node.Value.Length;
            span.ContentStart = state.Counter;
            int offset = 0;
            while (offset <= length)
            {
                RecordXmlRangeCharacterBoundary(node, offset, state.Counter + offset, state);
                offset = offset + 1;
            }

            state.Counter = state.Counter + length;
            span.ContentEnd = state.Counter;
        }

        state.Counter = state.Counter + 1;
        span.End = state.Counter;
    }

    internal void RecordXmlRangeContainerBoundary(XmlNode node, int offset, TreeRangePositionState state)
    {
        if (!state.FoundStart && node == state.StartContainer && offset == state.StartOffset)
        {
            state.StartPosition = state.Counter;
            state.FoundStart = true;
        }

        if (!state.FoundEnd && node == state.EndContainer && offset == state.EndOffset)
        {
            state.EndPosition = state.Counter;
            state.FoundEnd = true;
        }
    }

    internal void RecordXmlRangeCharacterBoundary(XmlNode node, int offset, long position, TreeRangePositionState state)
    {
        if (!state.FoundStart && node == state.StartContainer && offset == state.StartOffset)
        {
            state.StartPosition = position;
            state.FoundStart = true;
        }

        if (!state.FoundEnd && node == state.EndContainer && offset == state.EndOffset)
        {
            state.EndPosition = position;
            state.FoundEnd = true;
        }
    }

    internal bool IsXmlRangeContainerNode(XmlNode node)
    {
        return node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment || node.Kind == XmlNodeKind.Element || node.Kind == XmlNodeKind.EntityReference;
    }

    internal bool IsXmlRangeCharacterNode(XmlNode node)
    {
        return node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData || node.Kind == XmlNodeKind.Comment || node.Kind == XmlNodeKind.ProcessingInstruction;
    }

    internal void InvalidateXmlTreeRange(XmlRange range)
    {
        range.IsValid = false;
    }
}
