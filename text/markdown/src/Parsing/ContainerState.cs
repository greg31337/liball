using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownContainerState
{
    internal MarkdownNode Node;
    internal int Indent;
    internal int MarkerColumn;
    internal string Marker;
    internal bool IsOpen;

    internal MarkdownContainerState()
    {
        Node = MarkdownNode.Create(MarkdownNodeKind.Document);
        Indent = 0;
        MarkerColumn = 0;
        Marker = "";
        IsOpen = true;
    }

    internal static MarkdownContainerState Create(MarkdownNode node, int indent, int markerColumn, string marker)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (indent < 0)
        {
            throw new ArgumentOutOfRangeException("indent");
        }

        if (markerColumn < 0)
        {
            throw new ArgumentOutOfRangeException("markerColumn");
        }

        if (marker == null)
        {
            throw new ArgumentNullException("marker");
        }

        MarkdownContainerState state = new MarkdownContainerState();
        state.Node = node;
        state.Indent = indent;
        state.MarkerColumn = markerColumn;
        state.Marker = marker;
        return state;
    }

    internal MarkdownContainerState Copy()
    {
        MarkdownContainerState copy = Create(Node, Indent, MarkerColumn, Marker);
        copy.IsOpen = IsOpen;
        return copy;
    }

    internal void Close()
    {
        IsOpen = false;
    }
}

internal class MarkdownContainerStack
{
    internal List<MarkdownContainerState> Items;

    internal MarkdownContainerStack()
    {
        Items = new List<MarkdownContainerState>();
    }

    internal int GetCount()
    {
        return Items.Count;
    }

    internal bool HasOpenContainers()
    {
        return Items.Count > 0;
    }

    internal MarkdownContainerState GetAt(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return Items[index].Copy();
    }

    internal MarkdownContainerState[] GetStates()
    {
        MarkdownContainerState[] states = new MarkdownContainerState[Items.Count];
        int index = 0;

        while (index < Items.Count)
        {
            states[index] = Items[index].Copy();
            index = index + 1;
        }

        return states;
    }

    internal void Push(MarkdownContainerState state)
    {
        if (state == null)
        {
            throw new ArgumentNullException("state");
        }

        Items.Add(state.Copy());
    }

    internal void PushNode(MarkdownNode node, int indent, int markerColumn, string marker)
    {
        Push(MarkdownContainerState.Create(node, indent, markerColumn, marker));
    }

    internal MarkdownContainerState? Peek()
    {
        if (Items.Count == 0)
        {
            return null;
        }

        return Items[Items.Count - 1].Copy();
    }

    internal MarkdownContainerState? Pop()
    {
        if (Items.Count == 0)
        {
            return null;
        }

        MarkdownContainerState state = Items[Items.Count - 1];
        Items.RemoveAt(Items.Count - 1);
        state.Close();
        return state.Copy();
    }

    internal void Clear()
    {
        int index = 0;
        while (index < Items.Count)
        {
            Items[index].Close();
            index = index + 1;
        }

        Items.Clear();
    }
}
