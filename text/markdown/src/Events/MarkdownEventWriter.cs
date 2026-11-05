using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Reconstructs a Markdown document from a balanced sequence of enter and leave events.
/// </summary>
internal class MarkdownEventWriter
{
    internal MarkdownDocument Document;
    internal List<MarkdownNode> Stack;
    internal bool HasDocumentRoot;
    internal bool HasWrittenTopLevel;
    internal bool ClosedDocumentRoot;

    /// <summary>
    /// Initializes a new <see cref="MarkdownEventWriter"/> instance.
    /// </summary>
    internal MarkdownEventWriter()
    {
        Document = new MarkdownDocument();
        Stack = new List<MarkdownNode>();
        HasDocumentRoot = false;
        HasWrittenTopLevel = false;
        ClosedDocumentRoot = false;
    }

    /// <summary>
    /// Applies one event to the output tree, opening a node for enter events and closing it for leave events.
    /// </summary>
    /// <param name="item">The event to write.</param>
    internal void Write(MarkdownEvent item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        if (item.EventKind == MarkdownEventKind.None)
        {
            return;
        }

        if (item.EventKind == MarkdownEventKind.EnterNode)
        {
            WriteEnter(item);
            return;
        }

        if (item.EventKind == MarkdownEventKind.LeaveNode)
        {
            WriteLeave(item);
            return;
        }

        throw new ArgumentException("Unsupported markdown event kind.");
    }

    /// <summary>
    /// Validates that every entered node was closed and returns the reconstructed document.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal MarkdownDocument Finish()
    {
        if (Stack.Count > 0)
        {
            throw new InvalidOperationException("Cannot finish an incomplete markdown event stream.");
        }

        return Document;
    }

    /// <summary>
    /// Returns document.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal MarkdownDocument GetDocument()
    {
        return Document;
    }

    /// <summary>
    /// Returns open node count.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetOpenNodeCount()
    {
        return Stack.Count;
    }

    /// <summary>
    /// Writes all.
    /// </summary>
    /// <param name="events">The balanced events to reconstruct.</param>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal static MarkdownDocument WriteAll(MarkdownEvent[] events)
    {
        if (events == null)
        {
            throw new ArgumentNullException("events");
        }

        MarkdownEventWriter writer = new MarkdownEventWriter();
        int index = 0;
        while (index < events.Length)
        {
            writer.Write(events[index]);
            index = index + 1;
        }

        return writer.Finish();
    }

    /// <summary>
    /// Writes all.
    /// </summary>
    /// <param name="reader">The event reader to consume from its current position.</param>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal static MarkdownDocument WriteAll(MarkdownEventReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        MarkdownEventWriter writer = new MarkdownEventWriter();
        while (reader.Read())
        {
            writer.Write(reader.GetCurrent());
        }

        return writer.Finish();
    }

    internal void WriteEnter(MarkdownEvent item)
    {
        if (ClosedDocumentRoot)
        {
            throw new InvalidOperationException("Cannot write after the document root has been closed.");
        }

        MarkdownNode node = CreateNode(item);
        if (Stack.Count == 0)
        {
            WriteTopLevelNode(node);
        }
        else
        {
            if (node.Kind == MarkdownNodeKind.Document)
            {
                throw new InvalidOperationException("Document nodes can only appear at the start of an event stream.");
            }

            Stack[Stack.Count - 1].AppendChild(node);
        }

        Stack.Add(node);
    }

    internal void WriteTopLevelNode(MarkdownNode node)
    {
        if (node.Kind == MarkdownNodeKind.Document)
        {
            if (HasWrittenTopLevel)
            {
                throw new InvalidOperationException("A document node must be the first top-level event.");
            }

            Document = MarkdownDocument.Create(node);
            HasDocumentRoot = true;
            HasWrittenTopLevel = true;
            return;
        }

        if (HasDocumentRoot)
        {
            throw new InvalidOperationException("Cannot append top-level nodes after a document root event.");
        }

        Document.Root.AppendChild(node);
        HasWrittenTopLevel = true;
    }

    internal void WriteLeave(MarkdownEvent item)
    {
        if (Stack.Count == 0)
        {
            throw new InvalidOperationException("Cannot leave a node when no node is open.");
        }

        MarkdownNode node = Stack[Stack.Count - 1];
        if (node.Kind != item.NodeKind)
        {
            throw new InvalidOperationException("Leave event node kind does not match the current open node.");
        }

        Stack.RemoveAt(Stack.Count - 1);
        if (Stack.Count == 0 && HasDocumentRoot)
        {
            ClosedDocumentRoot = true;
        }
    }

    internal MarkdownNode CreateNode(MarkdownEvent item)
    {
        MarkdownNode node = MarkdownNode.Create(item.NodeKind, item.Span);
        if (item.Node != null && item.Node.Kind == item.NodeKind)
        {
            node.Data = item.Node.Data.CopyData();
        }
        node.SetLiteral(item.Literal);

        MarkdownAttribute[] attributes = item.GetAttributes();
        int index = 0;
        while (index < attributes.Length)
        {
            node.SetAttributeCopy(attributes[index]);
            index = index + 1;
        }

        MarkdownTrivia? trivia = item.GetTrivia();
        if (trivia != null)
        {
            node.SetTrivia(trivia);
        }

        return node;
    }
}
