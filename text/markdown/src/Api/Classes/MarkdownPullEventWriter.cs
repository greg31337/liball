using System;
using System.Collections.Generic;
using System.IO;

namespace markdown;

/// <summary>
/// Validates incremental Markdown events and builds an AST that can be rendered directly.
/// </summary>
public class MarkdownPullEventWriter : IMarkdownEventWriter
{
    internal MarkdownEventWriter Writer;
    internal List<MarkdownEvent> OpenEvents;
    internal List<int> NextChildIndexes;
    internal MarkdownDocument? FinishedDocument;

    /// <summary>Initializes an empty incremental event writer.</summary>
    public MarkdownPullEventWriter()
    {
        Writer = new MarkdownEventWriter();
        OpenEvents = new List<MarkdownEvent>();
        NextChildIndexes = new List<int>();
        FinishedDocument = null;
    }

    /// <summary>Writes one enter, leave, or no-op event.</summary>
    /// <param name="item">The event to validate and apply.</param>
    public void Write(MarkdownEvent item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        if (FinishedDocument != null)
        {
            throw new InvalidOperationException("Cannot write events after the document is finished.");
        }

        if (item.EventKind == MarkdownEventKind.None)
        {
            Writer.Write(item);
            return;
        }

        if (item.EventKind == MarkdownEventKind.EnterNode)
        {
            ValidateEnter(item);
            Writer.Write(item);
            if (OpenEvents.Count > 0)
            {
                int parentIndex = NextChildIndexes.Count - 1;
                NextChildIndexes[parentIndex] = NextChildIndexes[parentIndex] + 1;
            }

            OpenEvents.Add(item.Copy());
            NextChildIndexes.Add(0);
            return;
        }

        if (item.EventKind == MarkdownEventKind.LeaveNode)
        {
            ValidateLeave(item);
            Writer.Write(item);
            OpenEvents.RemoveAt(OpenEvents.Count - 1);
            NextChildIndexes.RemoveAt(NextChildIndexes.Count - 1);
            return;
        }

        throw new ArgumentException("Unsupported markdown event kind.");
    }

    /// <summary>Consumes the remaining events from a pull reader.</summary>
    /// <param name="reader">The reader to consume.</param>
    public void WriteFromReader(IMarkdownEventReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        while (reader.Read())
        {
            Write(reader.GetCurrent());
        }
    }

    /// <summary>Returns the number of nodes that remain open.</summary>
    /// <returns>The open-node count.</returns>
    public int GetOpenNodeCount() { return OpenEvents.Count; }

    /// <summary>Validates completion and returns the constructed document.</summary>
    /// <returns>The constructed document.</returns>
    public MarkdownDocument FinishDocument()
    {
        if (FinishedDocument != null)
        {
            return FinishedDocument;
        }

        MarkdownDocument document = Writer.Finish();
        FinishedDocument = document;
        return document;
    }

    /// <summary>Renders the completed events as HTML using CommonMark defaults.</summary><param name="writer">The destination.</param>
    public void WriteHtml(TextWriter writer)
    {
        ValidateDestination(writer);
        HtmlRenderer.Render(FinishDocument(), writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Renders the completed events as HTML using a supplied pipeline.</summary><param name="writer">The destination.</param><param name="pipeline">The pipeline.</param>
    public void WriteHtmlWithPipeline(TextWriter writer, MarkdownPipeline pipeline)
    {
        ValidateDestinationAndPipeline(writer, pipeline);
        HtmlRenderer.Render(FinishDocument(), writer, pipeline);
    }

    /// <summary>Renders the completed events as Markdown using CommonMark defaults.</summary><param name="writer">The destination.</param>
    public void WriteMarkdown(TextWriter writer)
    {
        ValidateDestination(writer);
        MarkdownRenderer.Render(FinishDocument(), writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Renders the completed events as Markdown using a supplied pipeline.</summary><param name="writer">The destination.</param><param name="pipeline">The pipeline.</param>
    public void WriteMarkdownWithPipeline(TextWriter writer, MarkdownPipeline pipeline)
    {
        ValidateDestinationAndPipeline(writer, pipeline);
        MarkdownRenderer.Render(FinishDocument(), writer, pipeline);
    }

    /// <summary>Renders the completed events as plain text using CommonMark defaults.</summary><param name="writer">The destination.</param>
    public void WritePlainText(TextWriter writer)
    {
        ValidateDestination(writer);
        PlainTextRenderer.Render(FinishDocument(), writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Renders the completed events as plain text using a supplied pipeline.</summary><param name="writer">The destination.</param><param name="pipeline">The pipeline.</param>
    public void WritePlainTextWithPipeline(TextWriter writer, MarkdownPipeline pipeline)
    {
        ValidateDestinationAndPipeline(writer, pipeline);
        PlainTextRenderer.Render(FinishDocument(), writer, pipeline);
    }

    /// <summary>Renders the completed events as a debug tree using CommonMark defaults.</summary><param name="writer">The destination.</param>
    public void WriteDebugTree(TextWriter writer)
    {
        ValidateDestination(writer);
        DebugTreeRenderer.Render(FinishDocument(), writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Renders the completed events as a debug tree using a supplied pipeline.</summary><param name="writer">The destination.</param><param name="pipeline">The pipeline.</param>
    public void WriteDebugTreeWithPipeline(TextWriter writer, MarkdownPipeline pipeline)
    {
        ValidateDestinationAndPipeline(writer, pipeline);
        DebugTreeRenderer.Render(FinishDocument(), writer, pipeline);
    }

    /// <summary>Renders the completed events as an XML-like AST using CommonMark defaults.</summary><param name="writer">The destination.</param>
    public void WriteXmlAst(TextWriter writer)
    {
        ValidateDestination(writer);
        XmlAstRenderer.Render(FinishDocument(), writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Renders the completed events as an XML-like AST using a supplied pipeline.</summary><param name="writer">The destination.</param><param name="pipeline">The pipeline.</param>
    public void WriteXmlAstWithPipeline(TextWriter writer, MarkdownPipeline pipeline)
    {
        ValidateDestinationAndPipeline(writer, pipeline);
        XmlAstRenderer.Render(FinishDocument(), writer, pipeline);
    }

    internal void ValidateEnter(MarkdownEvent item)
    {
        int expectedDepth = OpenEvents.Count;
        if (item.Depth != expectedDepth)
        {
            throw new InvalidOperationException("Enter event depth does not match the open-node depth.");
        }

        if (OpenEvents.Count == 0)
        {
            if (item.ChildIndex != -1)
            {
                throw new InvalidOperationException("A top-level enter event must use child index -1.");
            }

            return;
        }

        int expectedChildIndex = NextChildIndexes[NextChildIndexes.Count - 1];
        if (item.ChildIndex != expectedChildIndex)
        {
            throw new InvalidOperationException("Enter event child index is not contiguous.");
        }
    }

    internal void ValidateLeave(MarkdownEvent item)
    {
        if (OpenEvents.Count == 0)
        {
            throw new InvalidOperationException("Cannot leave a node when no node is open.");
        }

        MarkdownEvent enter = OpenEvents[OpenEvents.Count - 1];
        if (item.NodeKind != enter.NodeKind
            || item.Depth != enter.Depth
            || item.ChildIndex != enter.ChildIndex)
        {
            throw new InvalidOperationException("Leave event does not match the current open node.");
        }
    }

    internal static void ValidateDestination(TextWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }
    }

    internal static void ValidateDestinationAndPipeline(TextWriter writer, MarkdownPipeline pipeline)
    {
        ValidateDestination(writer);
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }
    }
}
