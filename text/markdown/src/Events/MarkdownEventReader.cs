using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Reads a parsed tree as a forward-only sequence of enter and leave events.
/// </summary>
internal class MarkdownEventReader
{
    internal List<MarkdownEvent> Events;
    internal int Position;
    internal MarkdownEvent Current;
    internal string Source;
    internal MarkdownDocument? Document;
    internal MarkdownPipeline Pipeline;

    /// <summary>
    /// Initializes an event reader for source parsed with the CommonMark preset.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    internal MarkdownEventReader(string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        Events = new List<MarkdownEvent>();
        Position = -1;
        Current = new MarkdownEvent();
        Source = "";
        Document = null;
        Pipeline = MarkdownPipeline.CommonMarkPreset();
        ParseSource(source, Pipeline);
    }

    /// <summary>
    /// Creates an event reader for source parsed with a specific pipeline.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    internal static MarkdownEventReader Create(string source, MarkdownPipeline pipeline)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownEventReader reader = new MarkdownEventReader("");
        reader.Events.Clear();
        reader.Pipeline = pipeline.Copy();
        reader.ParseSource(source, reader.Pipeline);
        return reader;
    }

    /// <summary>
    /// Creates an event reader for a parsed document.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    internal static MarkdownEventReader Create(MarkdownDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        MarkdownEventReader reader = new MarkdownEventReader("");
        reader.Events.Clear();
        reader.Document = document;
        reader.BuildEvents(document.Root);
        return reader;
    }

    /// <summary>
    /// Creates an event reader for an AST subtree.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    internal static MarkdownEventReader Create(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        MarkdownEventReader reader = new MarkdownEventReader("");
        reader.Events.Clear();
        reader.Document = null;
        reader.BuildEvents(node);
        return reader;
    }

    /// <summary>
    /// Advances to the next event in the precomputed sequence.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool Read()
    {
        if (Position + 1 >= Events.Count)
        {
            Position = Events.Count;
            Current = new MarkdownEvent();
            return false;
        }

        Position = Position + 1;
        Current = Events[Position].Copy();
        return true;
    }

    /// <summary>
    /// Returns current.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownEvent"/> value.</returns>
    internal MarkdownEvent GetCurrent()
    {
        return Current.Copy();
    }

    /// <summary>
    /// Moves the reader back before the first event so the sequence can be read again.
    /// </summary>
    internal void Reset()
    {
        Position = -1;
        Current = new MarkdownEvent();
    }

    /// <summary>
    /// Returns count.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetCount()
    {
        return Events.Count;
    }

    /// <summary>
    /// Returns the parsed or supplied document, when this reader represents a document.
    /// </summary>
    /// <returns>The document, or <c>null</c> when this reader was created from a standalone node.</returns>
    internal MarkdownDocument? GetDocument()
    {
        return Document;
    }

    /// <summary>
    /// Returns pipeline.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipeline"/> value.</returns>
    internal MarkdownPipeline GetPipeline()
    {
        return Pipeline.Copy();
    }

    /// <summary>
    /// Returns diagnostics.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownDiagnostic[] GetDiagnostics()
    {
        if (Document == null)
        {
            return new MarkdownDiagnostic[0];
        }

        return Document.GetDiagnostics();
    }

    /// <summary>
    /// Reports whether at least one diagnostic has been recorded.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasDiagnostics()
    {
        if (Document == null)
        {
            return false;
        }

        return Document.HasDiagnostics();
    }

    /// <summary>
    /// Reports whether at least one error diagnostic has been recorded.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasErrors()
    {
        if (Document == null)
        {
            return false;
        }

        return Document.HasErrors();
    }

    /// <summary>
    /// Returns events.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownEvent[] GetEvents()
    {
        MarkdownEvent[] events = new MarkdownEvent[Events.Count];
        int index = 0;
        while (index < Events.Count)
        {
            events[index] = Events[index].Copy();
            index = index + 1;
        }

        return events;
    }

    /// <summary>
    /// Creates the complete depth-first event sequence for the supplied source or tree.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal static MarkdownEvent[] ReadAll(MarkdownDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        MarkdownEventReader reader = Create(document);
        return reader.GetEvents();
    }

    /// <summary>
    /// Creates the complete depth-first event sequence for the supplied source or tree.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal static MarkdownEvent[] ReadAll(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        MarkdownEventReader reader = Create(node);
        return reader.GetEvents();
    }

    /// <summary>
    /// Creates the complete depth-first event sequence for the supplied source or tree.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal static MarkdownEvent[] ReadAll(string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        MarkdownEventReader reader = new MarkdownEventReader(source);
        return reader.GetEvents();
    }

    /// <summary>
    /// Creates the complete depth-first event sequence for the supplied source or tree.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal static MarkdownEvent[] ReadAll(string source, MarkdownPipeline pipeline)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownEventReader reader = Create(source, pipeline);
        return reader.GetEvents();
    }

    internal void ParseSource(string source, MarkdownPipeline pipeline)
    {
        MarkdownParseContext context = MarkdownParser.CreateContext(source, pipeline);
        MarkdownParser.Run(context);
        Source = context.Source;
        Document = context.Document;
        Pipeline = context.Pipeline.Copy();
        BuildEvents(context.Document.Root);
    }

    internal void BuildEvents(MarkdownNode root)
    {
        MarkdownNodeVisitor visitor = new MarkdownNodeVisitor();
        visitor.EnterNode = context =>
        {
            Events.Add(MarkdownEvent.Create(MarkdownEventKind.EnterNode, context.Node, context.Depth, context.ChildIndex));
            return MarkdownTreeWalkResult.Continue;
        };
        visitor.LeaveNode = context =>
        {
            Events.Add(MarkdownEvent.Create(MarkdownEventKind.LeaveNode, context.Node, context.Depth, context.ChildIndex));
        };

        MarkdownTreeWalker.Walk(root, visitor);
    }
}
