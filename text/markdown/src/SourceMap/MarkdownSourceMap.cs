using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Indexes AST nodes by source position for editor and inspection scenarios.
/// </summary>
internal class MarkdownSourceMap
{
    internal MarkdownDocument Document;
    internal List<MarkdownSourceMapEntry> Entries;

    /// <summary>
    /// Initializes a new <see cref="MarkdownSourceMap"/> instance.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    internal MarkdownSourceMap(MarkdownDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        Document = document;
        Entries = new List<MarkdownSourceMapEntry>();
        Build();
    }

    /// <summary>
    /// Returns count.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetCount()
    {
        return Entries.Count;
    }

    /// <summary>
    /// Returns entries.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownSourceMapEntry[] GetEntries()
    {
        MarkdownSourceMapEntry[] entries = new MarkdownSourceMapEntry[Entries.Count];
        int index = 0;
        while (index < Entries.Count)
        {
            entries[index] = Entries[index].Copy();
            index = index + 1;
        }

        return entries;
    }

    /// <summary>
    /// Finds containing.
    /// </summary>
    /// <param name="offset">The zero-based absolute source offset.</param>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownSourceMapEntry[] FindContaining(int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        List<MarkdownSourceMapEntry> matches = new List<MarkdownSourceMapEntry>();
        int index = 0;
        while (index < Entries.Count)
        {
            if (Entries[index].ContainsOffset(offset))
            {
                matches.Add(Entries[index].Copy());
            }

            index = index + 1;
        }

        return matches.ToArray();
    }

    /// <summary>
    /// Finds containing.
    /// </summary>
    /// <param name="position">The source position to locate.</param>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownSourceMapEntry[] FindContainingPosition(MarkdownSourcePosition position)
    {
        if (position == null)
        {
            throw new ArgumentNullException("position");
        }

        return FindContaining(position.Offset);
    }

    /// <summary>
    /// Finds deepest containing.
    /// </summary>
    /// <param name="offset">The zero-based absolute source offset.</param>
    /// <returns>A detached copy of the deepest entry, or <c>null</c> when no entry contains the offset.</returns>
    internal MarkdownSourceMapEntry? FindDeepestContaining(int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        MarkdownSourceMapEntry? best = null;
        int index = 0;
        while (index < Entries.Count)
        {
            MarkdownSourceMapEntry entry = Entries[index];
            if (entry.ContainsOffset(offset) && IsBetterDeepestMatch(entry, best))
            {
                best = entry;
            }

            index = index + 1;
        }

        if (best == null)
        {
            return null;
        }

        return best.Copy();
    }

    /// <summary>
    /// Finds deepest containing.
    /// </summary>
    /// <param name="position">The source position to locate.</param>
    /// <returns>A detached copy of the deepest entry, or <c>null</c> when no entry contains the position.</returns>
    internal MarkdownSourceMapEntry? FindDeepestContainingPosition(MarkdownSourcePosition position)
    {
        if (position == null)
        {
            throw new ArgumentNullException("position");
        }

        return FindDeepestContaining(position.Offset);
    }

    /// <summary>
    /// Finds deepest node.
    /// </summary>
    /// <param name="offset">The zero-based absolute source offset.</param>
    /// <returns>The deepest node, or <c>null</c> when no node contains the offset.</returns>
    internal MarkdownNode? FindDeepestNode(int offset)
    {
        MarkdownSourceMapEntry? entry = FindDeepestContaining(offset);
        if (entry == null)
        {
            return null;
        }

        return entry.Node;
    }

    /// <summary>
    /// Finds deepest node.
    /// </summary>
    /// <param name="position">The source position to locate.</param>
    /// <returns>The deepest node, or <c>null</c> when no node contains the position.</returns>
    internal MarkdownNode? FindDeepestNodePosition(MarkdownSourcePosition position)
    {
        if (position == null)
        {
            throw new ArgumentNullException("position");
        }

        return FindDeepestNode(position.Offset);
    }

    /// <summary>
    /// Finds nodes.
    /// </summary>
    /// <param name="kind">The node kind to match.</param>
    /// <param name="offset">The zero-based absolute source offset.</param>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownNode[] FindNodes(MarkdownNodeKind kind, int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        List<MarkdownNode> nodes = new List<MarkdownNode>();
        int index = 0;
        while (index < Entries.Count)
        {
            MarkdownSourceMapEntry entry = Entries[index];
            if (entry.Node.Kind == kind && entry.ContainsOffset(offset))
            {
                nodes.Add(entry.Node);
            }

            index = index + 1;
        }

        return nodes.ToArray();
    }

    internal MarkdownSourceMapEntry[] FindIntersecting(MarkdownSourceSpan span)
    {
        MarkdownDiagnosticQueries.ValidateSpan(span);
        List<MarkdownSourceMapEntry> matches = new List<MarkdownSourceMapEntry>();
        int index = 0;
        while (index < Entries.Count)
        {
            if (MarkdownDiagnosticQueries.SpansIntersect(Entries[index].Span, span))
            {
                matches.Add(Entries[index].Copy());
            }

            index = index + 1;
        }

        return matches.ToArray();
    }

    /// <summary>
    /// Builds a source map by walking every node in the document.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting <see cref="MarkdownSourceMap"/> value.</returns>
    internal static MarkdownSourceMap FromDocument(MarkdownDocument document)
    {
        return new MarkdownSourceMap(document);
    }

    internal void Build()
    {
        MarkdownNodeVisitor visitor = new MarkdownNodeVisitor();
        visitor.EnterNode = context =>
        {
            if (!context.Node.Span.IsEmpty())
            {
                Entries.Add(MarkdownSourceMapEntry.Create(context.Node, context.Depth, context.ChildIndex));
            }

            return MarkdownTreeWalkResult.Continue;
        };

        MarkdownTreeWalker.Walk(Document, visitor);
    }

    internal static bool IsBetterDeepestMatch(MarkdownSourceMapEntry entry, MarkdownSourceMapEntry? best)
    {
        if (best == null)
        {
            return true;
        }

        if (entry.Depth > best.Depth)
        {
            return true;
        }

        if (entry.Depth == best.Depth && entry.Span.GetLength() < best.Span.GetLength())
        {
            return true;
        }

        return false;
    }
}
