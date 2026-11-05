using System;
using System.IO;

namespace markdown;

/// <summary>
/// Provides the simplest entry points for parsing Markdown and rendering it to supported output formats.
/// </summary>
public class Markdown : IMarkdown, IMarkdownDataServices
{
    /// <summary>
    /// Initializes the primary Markdown API.
    /// </summary>
    public Markdown()
    {
    }

    /// <summary>Returns the advanced data, processing, analysis, and tree services for this façade.</summary>
    /// <returns>The data services implemented by this façade.</returns>
    public IMarkdownDataServices GetDataServices()
    {
        return this;
    }

    /// <summary>Creates a mutable CommonMark pipeline builder.</summary>
    /// <returns>The pipeline builder.</returns>
    public IMarkdownPipelineBuilder CreatePipelineBuilder()
    {
        return MarkdownPipelineBuilder.CommonMarkPreset();
    }

    /// <summary>Creates a CommonMark pipeline.</summary>
    /// <returns>The configured pipeline.</returns>
    public MarkdownPipeline CreateCommonMarkPipeline()
    {
        return MarkdownPipeline.CommonMarkPreset();
    }

    /// <summary>Creates a GitHub Flavored Markdown pipeline.</summary>
    /// <returns>The configured pipeline.</returns>
    public MarkdownPipeline CreateGfmPipeline()
    {
        return MarkdownPipeline.GfmPreset();
    }

    /// <summary>Creates a pipeline containing all supported extensions.</summary>
    /// <returns>The configured pipeline.</returns>
    public MarkdownPipeline CreateAdvancedPipeline()
    {
        return MarkdownPipeline.AdvancedPreset();
    }

    /// <summary>Creates a pipeline from caller-supplied options.</summary>
    /// <param name="options">The options to copy.</param>
    /// <returns>The configured pipeline.</returns>
    public MarkdownPipeline CreatePipelineFromOptions(MarkdownOptions options)
    {
        return MarkdownPipeline.Create(options);
    }

    /// <summary>Creates a streaming Markdown generator with LF line endings.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    /// <returns>The Markdown writer.</returns>
    public IMarkdownWriter CreateMarkdownWriter(TextWriter writer)
    {
        return new MarkdownGenerationWriter(writer);
    }

    /// <summary>Creates a streaming Markdown generator using a supplied pipeline's formatting limits.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The Markdown writer.</returns>
    public IMarkdownWriter CreateMarkdownWriterWithPipeline(TextWriter writer, MarkdownPipeline pipeline)
    {
        MarkdownGenerationWriter markdownWriter = new MarkdownGenerationWriter(writer);
        markdownWriter.Configure(pipeline);
        return markdownWriter;
    }

    /// <summary>Creates an in-memory Markdown builder with LF line endings.</summary>
    /// <returns>The Markdown builder.</returns>
    public IMarkdownBuilder CreateMarkdownBuilder()
    {
        return new MarkdownGenerationBuilder();
    }

    /// <summary>Creates an in-memory Markdown builder using a supplied pipeline's formatting limits.</summary>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The Markdown builder.</returns>
    public IMarkdownBuilder CreateMarkdownBuilderWithPipeline(MarkdownPipeline pipeline)
    {
        MarkdownGenerationBuilder builder = new MarkdownGenerationBuilder();
        builder.Configure(pipeline);
        return builder;
    }

    /// <summary>Builds a document from a balanced generated event array.</summary>
    /// <param name="events">The generated events.</param>
    /// <returns>A detached Markdown document.</returns>
    public MarkdownDocument CreateDocumentFromEvents(MarkdownEvent[] events)
    {
        return MarkdownEventWriter.WriteAll(events);
    }

    /// <summary>Builds a document from generated top-level nodes.</summary>
    /// <param name="nodes">The generated top-level nodes, which are cloned.</param>
    /// <returns>A detached Markdown document.</returns>
    public MarkdownDocument CreateDocumentFromNodes(MarkdownNode[] nodes)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException("nodes");
        }

        if (nodes.Length == 1 && nodes[0] != null && nodes[0].Kind == MarkdownNodeKind.Document)
        {
            return MarkdownDocument.Create(nodes[0].CloneNode());
        }

        MarkdownDocument document = new MarkdownDocument();
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == null)
            {
                throw new ArgumentNullException("nodes");
            }

            if (nodes[index].Kind == MarkdownNodeKind.Document)
            {
                throw new ArgumentException("A document node must be the only generated node.");
            }

            document.Root.AppendChild(nodes[index].CloneNode());
            index = index + 1;
        }

        return document;
    }

    /// <summary>Creates a pull event reader over source parsed with CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The pull reader.</returns>
    public IMarkdownEventReader CreateEventReaderFromSource(string source)
    {
        MarkdownPullEventReader reader = new MarkdownPullEventReader();
        reader.ConfigureSource(source, MarkdownPipeline.CommonMarkPreset());
        return reader;
    }

    /// <summary>Creates a pull event reader over source parsed with a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The pull reader.</returns>
    public IMarkdownEventReader CreateEventReaderFromSourceWithPipeline(
        string source,
        MarkdownPipeline pipeline)
    {
        MarkdownPullEventReader reader = new MarkdownPullEventReader();
        reader.ConfigureSource(source, pipeline);
        return reader;
    }

    /// <summary>Creates a pull event reader over a document AST.</summary>
    /// <param name="document">The document to traverse.</param>
    /// <returns>The pull reader.</returns>
    public IMarkdownEventReader CreateEventReaderFromDocument(MarkdownDocument document)
    {
        MarkdownPullEventReader reader = new MarkdownPullEventReader();
        reader.ConfigureDocument(document);
        return reader;
    }

    /// <summary>Creates a pull event reader over an AST subtree.</summary>
    /// <param name="node">The subtree root.</param>
    /// <returns>The pull reader.</returns>
    public IMarkdownEventReader CreateEventReaderFromNode(MarkdownNode node)
    {
        MarkdownPullEventReader reader = new MarkdownPullEventReader();
        reader.ConfigureNode(node);
        return reader;
    }

    /// <summary>Creates an incremental validating event writer.</summary>
    /// <returns>The event writer.</returns>
    public IMarkdownEventWriter CreateEventWriter()
    {
        return new MarkdownPullEventWriter();
    }

    /// <summary>Parses source using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The parsed document.</returns>
    public MarkdownDocument ParseSource(string source)
    {
        return Parse(source);
    }

    /// <summary>Parses source using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The parsed document.</returns>
    public MarkdownDocument ParseSourceWithPipeline(string source, MarkdownPipeline pipeline)
    {
        return Parse(source, pipeline);
    }

    /// <summary>Parses the remaining content of a reader using CommonMark defaults.</summary>
    /// <param name="reader">The reader to consume without disposing it.</param>
    /// <returns>The parsed document.</returns>
    public MarkdownDocument ParseReader(TextReader reader)
    {
        return MarkdownParser.ParseReader(reader, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Parses the remaining content of a reader using a supplied pipeline.</summary>
    /// <param name="reader">The reader to consume without disposing it.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The parsed document.</returns>
    public MarkdownDocument ParseReaderWithPipeline(TextReader reader, MarkdownPipeline pipeline)
    {
        return MarkdownParser.ParseReader(reader, pipeline);
    }

    /// <summary>Parses a character memory region using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source memory.</param>
    /// <returns>The parsed document.</returns>
    public MarkdownDocument ParseMemory(ReadOnlyMemory<char> source)
    {
        return MarkdownParser.ParseMemory(source, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Parses a character memory region using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source memory.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The parsed document.</returns>
    public MarkdownDocument ParseMemoryWithPipeline(ReadOnlyMemory<char> source, MarkdownPipeline pipeline)
    {
        return MarkdownParser.ParseMemory(source, pipeline);
    }

    /// <summary>Renders source as HTML using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The rendered HTML.</returns>
    public string RenderHtmlSource(string source)
    {
        return ToHtml(source);
    }

    /// <summary>Renders source as HTML using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The rendered HTML.</returns>
    public string RenderHtmlSourceWithPipeline(string source, MarkdownPipeline pipeline)
    {
        return ToHtml(source, pipeline);
    }

    /// <summary>Renders a document as HTML using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <returns>The rendered HTML.</returns>
    public string RenderHtmlDocument(MarkdownDocument document)
    {
        return ToHtml(document);
    }

    /// <summary>Renders a document as HTML using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The rendered HTML.</returns>
    public string RenderHtmlDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        return ToHtml(document, pipeline);
    }

    /// <summary>Writes source as HTML using CommonMark defaults without disposing the writer.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    public void WriteHtmlSource(string source, TextWriter writer)
    {
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        MarkdownDocument document = Parse(source, pipeline);
        HtmlRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Writes source as HTML using a supplied pipeline without disposing the writer.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WriteHtmlSourceWithPipeline(string source, TextWriter writer, MarkdownPipeline pipeline)
    {
        MarkdownDocument document = Parse(source, pipeline);
        HtmlRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Writes a document as HTML using CommonMark defaults without disposing the writer.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    public void WriteHtmlDocument(MarkdownDocument document, TextWriter writer)
    {
        HtmlRenderer.Render(document, writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Writes a document as HTML using a supplied pipeline without disposing the writer.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WriteHtmlDocumentWithPipeline(
        MarkdownDocument document,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        HtmlRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Writes a balanced Markdown event sequence as HTML using CommonMark defaults.</summary>
    /// <param name="events">The enter and leave events to render.</param>
    /// <param name="writer">The destination writer.</param>
    public void WriteHtmlEvents(MarkdownEvent[] events, TextWriter writer)
    {
        HtmlRenderer.RenderEvents(events, writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Writes a balanced Markdown event sequence as HTML using a supplied pipeline.</summary>
    /// <param name="events">The enter and leave events to render.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WriteHtmlEventsWithPipeline(
        MarkdownEvent[] events,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        HtmlRenderer.RenderEvents(events, writer, pipeline);
    }

    /// <summary>Renders source as normalized Markdown.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The normalized Markdown.</returns>
    public string RenderMarkdownSource(string source)
    {
        return ToMarkdown(source);
    }

    /// <summary>Renders source as normalized Markdown using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The normalized Markdown.</returns>
    public string RenderMarkdownSourceWithPipeline(string source, MarkdownPipeline pipeline)
    {
        return ToMarkdown(source, pipeline);
    }

    /// <summary>Renders a document as normalized Markdown.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <returns>The normalized Markdown.</returns>
    public string RenderMarkdownDocument(MarkdownDocument document)
    {
        return ToMarkdown(document);
    }

    /// <summary>Renders a document as normalized Markdown using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The normalized Markdown.</returns>
    public string RenderMarkdownDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        return ToMarkdown(document, pipeline);
    }

    /// <summary>Writes source as normalized Markdown using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    public void WriteMarkdownSource(string source, TextWriter writer)
    {
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        MarkdownDocument document = Parse(source, pipeline);
        MarkdownRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Writes source as normalized Markdown using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WriteMarkdownSourceWithPipeline(
        string source,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        MarkdownDocument document = Parse(source, pipeline);
        MarkdownRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Writes a document as normalized Markdown using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    public void WriteMarkdownDocument(MarkdownDocument document, TextWriter writer)
    {
        MarkdownRenderer.Render(document, writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Writes a document as normalized Markdown using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WriteMarkdownDocumentWithPipeline(
        MarkdownDocument document,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        MarkdownRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Renders source as plain text.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The plain text.</returns>
    public string RenderPlainTextSource(string source)
    {
        return ToPlainText(source);
    }

    /// <summary>Renders source as plain text using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The plain text.</returns>
    public string RenderPlainTextSourceWithPipeline(string source, MarkdownPipeline pipeline)
    {
        return ToPlainText(source, pipeline);
    }

    /// <summary>Renders a document as plain text.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <returns>The plain text.</returns>
    public string RenderPlainTextDocument(MarkdownDocument document)
    {
        return ToPlainText(document);
    }

    /// <summary>Renders a document as plain text using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The plain text.</returns>
    public string RenderPlainTextDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        return ToPlainText(document, pipeline);
    }

    /// <summary>Writes source as plain text using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    public void WritePlainTextSource(string source, TextWriter writer)
    {
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        MarkdownDocument document = Parse(source, pipeline);
        PlainTextRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Writes source as plain text using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WritePlainTextSourceWithPipeline(
        string source,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        MarkdownDocument document = Parse(source, pipeline);
        PlainTextRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Writes a document as plain text using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    public void WritePlainTextDocument(MarkdownDocument document, TextWriter writer)
    {
        PlainTextRenderer.Render(document, writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Writes a document as plain text using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WritePlainTextDocumentWithPipeline(
        MarkdownDocument document,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        PlainTextRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Renders source as a debug tree using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The debug-tree representation.</returns>
    public string RenderDebugTreeSource(string source)
    {
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        return DebugTreeRenderer.Render(Parse(source, pipeline), pipeline);
    }

    /// <summary>Renders source as a debug tree using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The debug-tree representation.</returns>
    public string RenderDebugTreeSourceWithPipeline(string source, MarkdownPipeline pipeline)
    {
        return DebugTreeRenderer.Render(Parse(source, pipeline), pipeline);
    }

    /// <summary>Renders a document as a debug tree using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <returns>The debug-tree representation.</returns>
    public string RenderDebugTreeDocument(MarkdownDocument document)
    {
        return DebugTreeRenderer.Render(document);
    }

    /// <summary>Renders a document as a debug tree using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The debug-tree representation.</returns>
    public string RenderDebugTreeDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        return DebugTreeRenderer.Render(document, pipeline);
    }

    /// <summary>Writes source as a debug tree using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    public void WriteDebugTreeSource(string source, TextWriter writer)
    {
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        DebugTreeRenderer.Render(Parse(source, pipeline), writer, pipeline);
    }

    /// <summary>Writes source as a debug tree using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WriteDebugTreeSourceWithPipeline(
        string source,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        DebugTreeRenderer.Render(Parse(source, pipeline), writer, pipeline);
    }

    /// <summary>Writes a document as a debug tree using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    public void WriteDebugTreeDocument(MarkdownDocument document, TextWriter writer)
    {
        DebugTreeRenderer.Render(document, writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Writes a document as a debug tree using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WriteDebugTreeDocumentWithPipeline(
        MarkdownDocument document,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        DebugTreeRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Renders source as an XML-like AST using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The XML-like AST representation.</returns>
    public string RenderXmlAstSource(string source)
    {
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        return XmlAstRenderer.Render(Parse(source, pipeline), pipeline);
    }

    /// <summary>Renders source as an XML-like AST using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The XML-like AST representation.</returns>
    public string RenderXmlAstSourceWithPipeline(string source, MarkdownPipeline pipeline)
    {
        return XmlAstRenderer.Render(Parse(source, pipeline), pipeline);
    }

    /// <summary>Renders a document as an XML-like AST using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <returns>The XML-like AST representation.</returns>
    public string RenderXmlAstDocument(MarkdownDocument document)
    {
        return XmlAstRenderer.Render(document);
    }

    /// <summary>Renders a document as an XML-like AST using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The XML-like AST representation.</returns>
    public string RenderXmlAstDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        return XmlAstRenderer.Render(document, pipeline);
    }

    /// <summary>Writes source as an XML-like AST using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    public void WriteXmlAstSource(string source, TextWriter writer)
    {
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        XmlAstRenderer.Render(Parse(source, pipeline), writer, pipeline);
    }

    /// <summary>Writes source as an XML-like AST using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WriteXmlAstSourceWithPipeline(
        string source,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        XmlAstRenderer.Render(Parse(source, pipeline), writer, pipeline);
    }

    /// <summary>Writes a document as an XML-like AST using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    public void WriteXmlAstDocument(MarkdownDocument document, TextWriter writer)
    {
        XmlAstRenderer.Render(document, writer, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Writes a document as an XML-like AST using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    public void WriteXmlAstDocumentWithPipeline(
        MarkdownDocument document,
        TextWriter writer,
        MarkdownPipeline pipeline)
    {
        XmlAstRenderer.Render(document, writer, pipeline);
    }

    /// <summary>Normalizes Markdown source.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The normalized Markdown.</returns>
    public string NormalizeSource(string source)
    {
        return Normalize(source);
    }

    /// <summary>Normalizes Markdown source using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The normalized Markdown.</returns>
    public string NormalizeSourceWithPipeline(string source, MarkdownPipeline pipeline)
    {
        return Normalize(source, pipeline);
    }

    /// <summary>Normalizes a document using CommonMark defaults.</summary>
    /// <param name="document">The document to normalize.</param>
    /// <returns>The normalized Markdown.</returns>
    public string NormalizeDocument(MarkdownDocument document)
    {
        return Normalize(document);
    }

    /// <summary>Normalizes a document using a supplied pipeline.</summary>
    /// <param name="document">The document to normalize.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The normalized Markdown.</returns>
    public string NormalizeDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        return Normalize(document, pipeline);
    }

    /// <summary>Renders child nodes through an active HTML context.</summary>
    /// <param name="context">The active render context.</param>
    /// <param name="node">The parent node.</param>
    public void RenderHtmlChildren(HtmlRenderContext context, MarkdownNode node)
    {
        HtmlRenderer.RenderChildren(context, node);
    }

    /// <summary>Writes the configured line ending through an active HTML context.</summary>
    /// <param name="context">The active render context.</param>
    public void WriteHtmlLineEnding(HtmlRenderContext context)
    {
        HtmlRenderer.WriteLineEnding(context);
    }

    /// <summary>Creates exception-less URI and raw-HTML safety evaluation operations.</summary>
    /// <returns>The safety operations.</returns>
    public IMarkdownSafetyOperations CreateSafetyOperations()
    {
        return new MarkdownSafetyOperations();
    }

    /// <summary>Creates normalization, transform, reference-resolution, and rewrite operations.</summary>
    /// <returns>The processing operations.</returns>
    public IMarkdownProcessingOperations CreateProcessingOperations()
    {
        return new MarkdownProcessingOperations();
    }

    /// <summary>Creates safe diagnostics, source-map, and source-preserving operations for a document.</summary>
    /// <param name="document">The document to inspect or update.</param>
    /// <returns>The document operations.</returns>
    public IMarkdownDocumentOperations CreateDocumentOperations(MarkdownDocument document)
    {
        return new MarkdownDocumentOperations(document);
    }

    /// <summary>Creates partial and incremental parsing operations for editor scenarios.</summary>
    /// <returns>The editor operations.</returns>
    public IMarkdownEditorOperations CreateEditorOperations()
    {
        return new MarkdownEditorOperations();
    }

    /// <summary>Creates cloning, search, and traversal operations for Markdown trees.</summary>
    /// <returns>The tree operations.</returns>
    public IMarkdownTreeOperations CreateTreeOperations()
    {
        return new MarkdownTreeOperations();
    }

    /// <summary>Creates invariant-preserving operations for an AST node.</summary>
    /// <param name="node">The node to inspect or edit.</param>
    /// <returns>The node operations.</returns>
    public IMarkdownNodeOperations CreateNodeOperations(MarkdownNode node)
    {
        return new MarkdownNodeOperations(node);
    }

    /// <summary>Creates a typed block wrapper around an existing node.</summary>
    /// <param name="node">The block node to wrap.</param>
    /// <returns>The typed block wrapper.</returns>
    public MarkdownBlockNode CreateBlockNode(MarkdownNode node)
    {
        return MarkdownBlockNode.Create(node);
    }

    /// <summary>Creates a typed inline wrapper around an existing node.</summary>
    /// <param name="node">The inline node to wrap.</param>
    /// <returns>The typed inline wrapper.</returns>
    public MarkdownInlineNode CreateInlineNode(MarkdownNode node)
    {
        return MarkdownInlineNode.Create(node);
    }

    /// <summary>Creates a named extension wrapper around an existing node.</summary>
    /// <param name="extensionName">The extension name.</param>
    /// <param name="node">The extension node to wrap.</param>
    /// <returns>The named extension wrapper.</returns>
    public MarkdownExtensionNode CreateExtensionNode(string extensionName, MarkdownNode node)
    {
        return MarkdownExtensionNode.Create(extensionName, node);
    }

    /// <summary>Analyzes an existing document.</summary>
    /// <param name="document">The document to analyze.</param>
    /// <returns>The aggregate analysis.</returns>
    public MarkdownDocumentAnalysis AnalyzeDocument(MarkdownDocument document)
    {
        return MarkdownAnalyzer.Analyze(document);
    }

    /// <summary>Analyzes an existing document with configurable TOC and reading-time settings.</summary>
    /// <param name="document">The document to analyze.</param>
    /// <param name="options">The analysis options.</param>
    /// <returns>The aggregate analysis.</returns>
    public MarkdownDocumentAnalysis AnalyzeDocumentWithOptions(
        MarkdownDocument document,
        MarkdownAnalysisOptions options)
    {
        return MarkdownAnalyzer.AnalyzeConfigured(
            document,
            MarkdownPipeline.CommonMarkPreset(),
            options);
    }

    /// <summary>Analyzes an existing document using a supplied pipeline and default analysis settings.</summary>
    /// <param name="document">The document to analyze.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The aggregate analysis.</returns>
    public MarkdownDocumentAnalysis AnalyzeDocumentWithPipeline(
        MarkdownDocument document,
        MarkdownPipeline pipeline)
    {
        return MarkdownAnalyzer.AnalyzeConfigured(
            document,
            pipeline,
            new MarkdownAnalysisOptions());
    }

    /// <summary>Analyzes an existing document with rendering and analysis settings.</summary>
    /// <param name="document">The document to analyze.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <param name="options">The analysis options.</param>
    /// <returns>The aggregate analysis.</returns>
    public MarkdownDocumentAnalysis AnalyzeDocumentWithPipelineAndOptions(
        MarkdownDocument document,
        MarkdownPipeline pipeline,
        MarkdownAnalysisOptions options)
    {
        return MarkdownAnalyzer.AnalyzeConfigured(document, pipeline, options);
    }

    /// <summary>Parses and analyzes source with CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The aggregate analysis.</returns>
    public MarkdownDocumentAnalysis AnalyzeSource(string source)
    {
        return MarkdownAnalyzer.Analyze(source);
    }

    /// <summary>Parses and analyzes source with configurable TOC and reading-time settings.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="options">The analysis options.</param>
    /// <returns>The aggregate analysis.</returns>
    public MarkdownDocumentAnalysis AnalyzeSourceWithOptions(
        string source,
        MarkdownAnalysisOptions options)
    {
        return MarkdownAnalyzer.AnalyzeConfiguredSource(
            source,
            MarkdownPipeline.CommonMarkPreset(),
            options);
    }

    /// <summary>Parses and analyzes source with a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The aggregate analysis.</returns>
    public MarkdownDocumentAnalysis AnalyzeSourceWithPipeline(
        string source,
        MarkdownPipeline pipeline)
    {
        return MarkdownAnalyzer.Analyze(source, pipeline);
    }

    /// <summary>Parses and analyzes source with rendering and analysis settings.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <param name="options">The analysis options.</param>
    /// <returns>The aggregate analysis.</returns>
    public MarkdownDocumentAnalysis AnalyzeSourceWithPipelineAndOptions(
        string source,
        MarkdownPipeline pipeline,
        MarkdownAnalysisOptions options)
    {
        return MarkdownAnalyzer.AnalyzeConfiguredSource(source, pipeline, options);
    }

    /// <summary>Reads the depth-first event sequence for a document.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>The detached event snapshots.</returns>
    public MarkdownEvent[] ReadDocumentEvents(MarkdownDocument document)
    {
        return MarkdownEventReader.ReadAll(document);
    }

    /// <summary>Reads the depth-first event sequence for an AST subtree.</summary>
    /// <param name="node">The subtree root to read.</param>
    /// <returns>The detached event snapshots.</returns>
    public MarkdownEvent[] ReadNodeEvents(MarkdownNode node)
    {
        return MarkdownEventReader.ReadAll(node);
    }

    /// <summary>Parses source with CommonMark defaults and reads its depth-first event sequence.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The detached event snapshots.</returns>
    public MarkdownEvent[] ReadSourceEvents(string source)
    {
        return MarkdownEventReader.ReadAll(source);
    }

    /// <summary>Parses source with a supplied pipeline and reads its depth-first event sequence.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The detached event snapshots.</returns>
    public MarkdownEvent[] ReadSourceEventsWithPipeline(string source, MarkdownPipeline pipeline)
    {
        return MarkdownEventReader.ReadAll(source, pipeline);
    }

    /// <summary>Creates a named rule registration.</summary>
    /// <param name="name">The stable rule name.</param>
    /// <param name="rule">The caller-supplied rule object.</param>
    /// <returns>The rule registration.</returns>
    public MarkdownRuleRegistration CreateRuleRegistration(string name, object rule)
    {
        return MarkdownRuleRegistration.Create(name, rule);
    }

    /// <summary>Builds source-map entries for a document.</summary>
    /// <param name="document">The document to map.</param>
    /// <returns>The detached source-map entries.</returns>
    public MarkdownSourceMapEntry[] GetSourceMapEntries(MarkdownDocument document)
    {
        MarkdownSourceMap sourceMap = new MarkdownSourceMap(document);
        return sourceMap.GetEntries();
    }

    /// <summary>Classifies a URI using the renderer safety policy.</summary>
    /// <param name="uri">The URI text to classify.</param>
    /// <returns>The URI classification.</returns>
    public MarkdownUriClassification ClassifyUri(string uri)
    {
        return MarkdownUriPolicy.Classify(uri);
    }

    /// <summary>
    /// Creates a pipeline builder initialized with CommonMark and safe rendering defaults.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipelineBuilder"/> value.</returns>
    internal static MarkdownPipelineBuilder CreatePipeline()
    {
        return MarkdownPipelineBuilder.Create();
    }

    /// <summary>
    /// Creates an in-memory Markdown builder.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal static MarkdownBuilder CreateBuilder()
    {
        return new MarkdownBuilder();
    }

    /// <summary>
    /// Creates an in-memory Markdown builder.
    /// </summary>
    /// <param name="lineEnding">The line-ending sequence to emit; use <c>\n</c> or <c>\r\n</c>.</param>
    /// <returns>The resulting <see cref="MarkdownBuilder"/> value.</returns>
    internal static MarkdownBuilder CreateBuilder(string lineEnding)
    {
        return MarkdownBuilder.Create(lineEnding);
    }

    /// <summary>
    /// Parses Markdown into a mutable abstract syntax tree and records recoverable diagnostics on the document.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal static MarkdownDocument Parse(string source)
    {
        return MarkdownParser.Parse(source);
    }

    /// <summary>
    /// Parses Markdown into a mutable abstract syntax tree and records recoverable diagnostics on the document.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal static MarkdownDocument Parse(string source, MarkdownPipeline pipeline)
    {
        return MarkdownParser.Parse(source, pipeline);
    }

    /// <summary>
    /// Renders HTML after applying the selected parsing, transformation, safety, and output settings.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToHtml(string source)
    {
        MarkdownDocument document = Parse(source);
        return HtmlRenderer.Render(document);
    }

    /// <summary>
    /// Renders HTML after applying the selected parsing, transformation, safety, and output settings.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToHtml(string source, MarkdownPipeline pipeline)
    {
        MarkdownDocument document = Parse(source, pipeline);
        return HtmlRenderer.Render(document, pipeline);
    }

    /// <summary>
    /// Renders HTML after applying the selected parsing, transformation, safety, and output settings.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToHtml(MarkdownDocument document)
    {
        return HtmlRenderer.Render(document);
    }

    /// <summary>
    /// Renders HTML after applying the selected parsing, transformation, safety, and output settings.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToHtml(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        return HtmlRenderer.Render(document, pipeline);
    }

    /// <summary>
    /// Renders normalized Markdown using the selected line ending and trivia policy.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToMarkdown(string source)
    {
        MarkdownDocument document = Parse(source);
        return MarkdownRenderer.Render(document);
    }

    /// <summary>
    /// Renders normalized Markdown using the selected line ending and trivia policy.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToMarkdown(string source, MarkdownPipeline pipeline)
    {
        MarkdownDocument document = Parse(source, pipeline);
        return MarkdownRenderer.Render(document, pipeline);
    }

    /// <summary>
    /// Renders normalized Markdown using the selected line ending and trivia policy.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToMarkdown(MarkdownDocument document)
    {
        return MarkdownRenderer.Render(document);
    }

    /// <summary>
    /// Renders normalized Markdown using the selected line ending and trivia policy.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToMarkdown(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        return MarkdownRenderer.Render(document, pipeline);
    }

    /// <summary>
    /// Renders readable text while omitting Markdown delimiters and non-text structure.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToPlainText(string source)
    {
        MarkdownDocument document = Parse(source);
        return PlainTextRenderer.Render(document);
    }

    /// <summary>
    /// Renders readable text while omitting Markdown delimiters and non-text structure.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToPlainText(string source, MarkdownPipeline pipeline)
    {
        MarkdownDocument document = Parse(source, pipeline);
        return PlainTextRenderer.Render(document, pipeline);
    }

    /// <summary>
    /// Renders readable text while omitting Markdown delimiters and non-text structure.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToPlainText(MarkdownDocument document)
    {
        return PlainTextRenderer.Render(document);
    }

    /// <summary>
    /// Renders readable text while omitting Markdown delimiters and non-text structure.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting text.</returns>
    internal static string ToPlainText(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        return PlainTextRenderer.Render(document, pipeline);
    }

    /// <summary>
    /// Returns the same normalized Markdown representation as <see cref="Markdown.ToMarkdown(string)"/>.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <returns>The resulting text.</returns>
    internal static string Normalize(string source)
    {
        return ToMarkdown(source);
    }

    /// <summary>
    /// Returns the same normalized Markdown representation as <see cref="Markdown.ToMarkdown(string)"/>.
    /// </summary>
    /// <param name="source">The Markdown source text.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting text.</returns>
    internal static string Normalize(string source, MarkdownPipeline pipeline)
    {
        return ToMarkdown(source, pipeline);
    }

    /// <summary>
    /// Returns the same normalized Markdown representation as <see cref="Markdown.ToMarkdown(string)"/>.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string Normalize(MarkdownDocument document)
    {
        return ToMarkdown(document);
    }

    /// <summary>
    /// Returns the same normalized Markdown representation as <see cref="Markdown.ToMarkdown(string)"/>.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    /// <returns>The resulting text.</returns>
    internal static string Normalize(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        return ToMarkdown(document, pipeline);
    }
}
