using System;
using System.IO;

namespace markdown;

/// <summary>
/// Resolves caller-controlled Markdown include requests without requiring file-system access from the library.
/// </summary>
/// <remarks>
/// Include processing recognizes a standalone line containing <c>!INCLUDE "name"</c>; single-quoted names and up to three leading spaces are also accepted.
/// </remarks>
public interface IMarkdownIncludeResolver
{
    /// <summary>Resolves one include request.</summary>
    /// <param name="context">The requested name, containing source name, and nesting depth.</param>
    /// <returns>A found result with a canonical source name and content, or a result whose <c>Found</c> field is <c>false</c>.</returns>
    MarkdownIncludeResult Resolve(MarkdownIncludeContext context);
}

/// <summary>
/// Decides whether a rewritten link or image destination may be emitted.
/// </summary>
public interface IMarkdownUriPolicy
{
    /// <summary>Returns whether the evaluated destination is allowed.</summary>
    /// <param name="context">The destination, classification, node, and destination role.</param>
    /// <returns><c>true</c> to emit the destination; otherwise, <c>false</c>.</returns>
    bool AllowUri(MarkdownUriPolicyContext context);
}

/// <summary>
/// Selects the explicit rendering decision for a raw HTML literal.
/// </summary>
public interface IMarkdownRawHtmlPolicy
{
    /// <summary>Returns the raw HTML rendering decision.</summary>
    /// <param name="context">The literal, node, built-in decision, and rendering settings.</param>
    /// <returns>The rendering decision.</returns>
    MarkdownRawHtmlDecision DecideRawHtml(MarkdownRawHtmlPolicyContext context);
}

/// <summary>
/// Defines the primary instance-based Markdown parsing and rendering API.
/// </summary>
public interface IMarkdown
{
    /// <summary>Returns the advanced data, processing, analysis, and tree services for this façade.</summary>
    /// <returns>The data services implemented by this façade.</returns>
    IMarkdownDataServices GetDataServices();

    /// <summary>Creates a mutable CommonMark pipeline builder.</summary>
    /// <returns>The pipeline builder.</returns>
    IMarkdownPipelineBuilder CreatePipelineBuilder();

    /// <summary>Creates a CommonMark pipeline.</summary>
    /// <returns>The configured pipeline.</returns>
    MarkdownPipeline CreateCommonMarkPipeline();

    /// <summary>Creates a GitHub Flavored Markdown pipeline.</summary>
    /// <returns>The configured pipeline.</returns>
    MarkdownPipeline CreateGfmPipeline();

    /// <summary>Creates a pipeline containing all supported extensions.</summary>
    /// <returns>The configured pipeline.</returns>
    MarkdownPipeline CreateAdvancedPipeline();

    /// <summary>Creates a pipeline from caller-supplied options.</summary>
    /// <param name="options">The options to copy.</param>
    /// <returns>The configured pipeline.</returns>
    MarkdownPipeline CreatePipelineFromOptions(MarkdownOptions options);

    /// <summary>Creates a streaming Markdown generator with LF line endings.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    /// <returns>The Markdown writer.</returns>
    IMarkdownWriter CreateMarkdownWriter(TextWriter writer);

    /// <summary>Creates a streaming Markdown generator using a supplied pipeline's formatting limits.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The Markdown writer.</returns>
    IMarkdownWriter CreateMarkdownWriterWithPipeline(TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Creates an in-memory Markdown builder with LF line endings.</summary>
    /// <returns>The Markdown builder.</returns>
    IMarkdownBuilder CreateMarkdownBuilder();

    /// <summary>Creates an in-memory Markdown builder using a supplied pipeline's formatting limits.</summary>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The Markdown builder.</returns>
    IMarkdownBuilder CreateMarkdownBuilderWithPipeline(MarkdownPipeline pipeline);

    /// <summary>Builds a document from a balanced generated event array.</summary>
    /// <param name="events">The generated events.</param>
    /// <returns>A detached Markdown document.</returns>
    MarkdownDocument CreateDocumentFromEvents(MarkdownEvent[] events);

    /// <summary>Builds a document from generated top-level nodes.</summary>
    /// <param name="nodes">The generated top-level nodes, which are cloned.</param>
    /// <returns>A detached Markdown document.</returns>
    MarkdownDocument CreateDocumentFromNodes(MarkdownNode[] nodes);

    /// <summary>Creates a pull event reader over source parsed with CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The pull reader.</returns>
    IMarkdownEventReader CreateEventReaderFromSource(string source);

    /// <summary>Creates a pull event reader over source parsed with a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The pull reader.</returns>
    IMarkdownEventReader CreateEventReaderFromSourceWithPipeline(string source, MarkdownPipeline pipeline);

    /// <summary>Creates a pull event reader over a document AST.</summary>
    /// <param name="document">The document to traverse.</param>
    /// <returns>The pull reader.</returns>
    IMarkdownEventReader CreateEventReaderFromDocument(MarkdownDocument document);

    /// <summary>Creates a pull event reader over an AST subtree.</summary>
    /// <param name="node">The subtree root.</param>
    /// <returns>The pull reader.</returns>
    IMarkdownEventReader CreateEventReaderFromNode(MarkdownNode node);

    /// <summary>Creates an incremental validating event writer.</summary>
    /// <returns>The event writer.</returns>
    IMarkdownEventWriter CreateEventWriter();

    /// <summary>Parses source using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The parsed document.</returns>
    MarkdownDocument ParseSource(string source);

    /// <summary>Parses source using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The parsed document.</returns>
    MarkdownDocument ParseSourceWithPipeline(string source, MarkdownPipeline pipeline);

    /// <summary>Parses the remaining content of a reader using CommonMark defaults.</summary>
    /// <param name="reader">The reader to consume without disposing it.</param>
    /// <returns>The parsed document.</returns>
    MarkdownDocument ParseReader(TextReader reader);

    /// <summary>Parses the remaining content of a reader using a supplied pipeline.</summary>
    /// <param name="reader">The reader to consume without disposing it.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The parsed document.</returns>
    MarkdownDocument ParseReaderWithPipeline(TextReader reader, MarkdownPipeline pipeline);

    /// <summary>Parses a character memory region using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source memory.</param>
    /// <returns>The parsed document.</returns>
    MarkdownDocument ParseMemory(ReadOnlyMemory<char> source);

    /// <summary>Parses a character memory region using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source memory.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The parsed document.</returns>
    MarkdownDocument ParseMemoryWithPipeline(ReadOnlyMemory<char> source, MarkdownPipeline pipeline);

    /// <summary>Renders source as HTML using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The rendered HTML.</returns>
    string RenderHtmlSource(string source);

    /// <summary>Renders source as HTML using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The rendered HTML.</returns>
    string RenderHtmlSourceWithPipeline(string source, MarkdownPipeline pipeline);

    /// <summary>Renders a document as HTML using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <returns>The rendered HTML.</returns>
    string RenderHtmlDocument(MarkdownDocument document);

    /// <summary>Renders a document as HTML using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The rendered HTML.</returns>
    string RenderHtmlDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline);

    /// <summary>Writes source as HTML using CommonMark defaults without disposing the writer.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    void WriteHtmlSource(string source, TextWriter writer);

    /// <summary>Writes source as HTML using a supplied pipeline without disposing the writer.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteHtmlSourceWithPipeline(string source, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Writes a document as HTML using CommonMark defaults without disposing the writer.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    void WriteHtmlDocument(MarkdownDocument document, TextWriter writer);

    /// <summary>Writes a document as HTML using a supplied pipeline without disposing the writer.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteHtmlDocumentWithPipeline(MarkdownDocument document, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Writes a balanced Markdown event sequence as HTML using CommonMark defaults.</summary>
    /// <param name="events">The enter and leave events to render.</param>
    /// <param name="writer">The destination writer.</param>
    void WriteHtmlEvents(MarkdownEvent[] events, TextWriter writer);

    /// <summary>Writes a balanced Markdown event sequence as HTML using a supplied pipeline.</summary>
    /// <param name="events">The enter and leave events to render.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteHtmlEventsWithPipeline(MarkdownEvent[] events, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Renders source as normalized Markdown.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The normalized Markdown.</returns>
    string RenderMarkdownSource(string source);

    /// <summary>Renders source as normalized Markdown using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The normalized Markdown.</returns>
    string RenderMarkdownSourceWithPipeline(string source, MarkdownPipeline pipeline);

    /// <summary>Renders a document as normalized Markdown.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <returns>The normalized Markdown.</returns>
    string RenderMarkdownDocument(MarkdownDocument document);

    /// <summary>Renders a document as normalized Markdown using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The normalized Markdown.</returns>
    string RenderMarkdownDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline);

    /// <summary>Writes source as normalized Markdown using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    void WriteMarkdownSource(string source, TextWriter writer);

    /// <summary>Writes source as normalized Markdown using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteMarkdownSourceWithPipeline(string source, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Writes a document as normalized Markdown using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    void WriteMarkdownDocument(MarkdownDocument document, TextWriter writer);

    /// <summary>Writes a document as normalized Markdown using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteMarkdownDocumentWithPipeline(MarkdownDocument document, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Renders source as plain text.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The plain text.</returns>
    string RenderPlainTextSource(string source);

    /// <summary>Renders source as plain text using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The plain text.</returns>
    string RenderPlainTextSourceWithPipeline(string source, MarkdownPipeline pipeline);

    /// <summary>Renders a document as plain text.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <returns>The plain text.</returns>
    string RenderPlainTextDocument(MarkdownDocument document);

    /// <summary>Renders a document as plain text using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The plain text.</returns>
    string RenderPlainTextDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline);

    /// <summary>Writes source as plain text using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    void WritePlainTextSource(string source, TextWriter writer);

    /// <summary>Writes source as plain text using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WritePlainTextSourceWithPipeline(string source, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Writes a document as plain text using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    void WritePlainTextDocument(MarkdownDocument document, TextWriter writer);

    /// <summary>Writes a document as plain text using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WritePlainTextDocumentWithPipeline(MarkdownDocument document, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Renders source as a debug tree using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The debug-tree representation.</returns>
    string RenderDebugTreeSource(string source);

    /// <summary>Renders source as a debug tree using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The debug-tree representation.</returns>
    string RenderDebugTreeSourceWithPipeline(string source, MarkdownPipeline pipeline);

    /// <summary>Renders a document as a debug tree using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <returns>The debug-tree representation.</returns>
    string RenderDebugTreeDocument(MarkdownDocument document);

    /// <summary>Renders a document as a debug tree using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The debug-tree representation.</returns>
    string RenderDebugTreeDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline);

    /// <summary>Writes source as a debug tree using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    void WriteDebugTreeSource(string source, TextWriter writer);

    /// <summary>Writes source as a debug tree using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteDebugTreeSourceWithPipeline(string source, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Writes a document as a debug tree using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    void WriteDebugTreeDocument(MarkdownDocument document, TextWriter writer);

    /// <summary>Writes a document as a debug tree using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteDebugTreeDocumentWithPipeline(MarkdownDocument document, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Renders source as an XML-like AST using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The XML-like AST representation.</returns>
    string RenderXmlAstSource(string source);

    /// <summary>Renders source as an XML-like AST using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The XML-like AST representation.</returns>
    string RenderXmlAstSourceWithPipeline(string source, MarkdownPipeline pipeline);

    /// <summary>Renders a document as an XML-like AST using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <returns>The XML-like AST representation.</returns>
    string RenderXmlAstDocument(MarkdownDocument document);

    /// <summary>Renders a document as an XML-like AST using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The XML-like AST representation.</returns>
    string RenderXmlAstDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline);

    /// <summary>Writes source as an XML-like AST using CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    void WriteXmlAstSource(string source, TextWriter writer);

    /// <summary>Writes source as an XML-like AST using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteXmlAstSourceWithPipeline(string source, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Writes a document as an XML-like AST using CommonMark defaults.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    void WriteXmlAstDocument(MarkdownDocument document, TextWriter writer);

    /// <summary>Writes a document as an XML-like AST using a supplied pipeline.</summary>
    /// <param name="document">The Markdown document.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteXmlAstDocumentWithPipeline(MarkdownDocument document, TextWriter writer, MarkdownPipeline pipeline);

    /// <summary>Normalizes Markdown source.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The normalized Markdown.</returns>
    string NormalizeSource(string source);

    /// <summary>Normalizes Markdown source using a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The normalized Markdown.</returns>
    string NormalizeSourceWithPipeline(string source, MarkdownPipeline pipeline);

    /// <summary>Normalizes a document using CommonMark defaults.</summary>
    /// <param name="document">The document to normalize.</param>
    /// <returns>The normalized Markdown.</returns>
    string NormalizeDocument(MarkdownDocument document);

    /// <summary>Normalizes a document using a supplied pipeline.</summary>
    /// <param name="document">The document to normalize.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The normalized Markdown.</returns>
    string NormalizeDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline);

    /// <summary>Renders child nodes through an active HTML context.</summary>
    /// <param name="context">The active render context.</param>
    /// <param name="node">The parent node.</param>
    void RenderHtmlChildren(HtmlRenderContext context, MarkdownNode node);

    /// <summary>Writes the configured line ending through an active HTML context.</summary>
    /// <param name="context">The active render context.</param>
    void WriteHtmlLineEnding(HtmlRenderContext context);
}

/// <summary>
/// Defines advanced data-model creation and document-inspection operations.
/// </summary>
public interface IMarkdownDataServices
{
    /// <summary>Creates exception-less URI and raw-HTML safety evaluation operations.</summary>
    /// <returns>The safety operations.</returns>
    IMarkdownSafetyOperations CreateSafetyOperations();

    /// <summary>Creates normalization, transform, reference-resolution, and rewrite operations.</summary>
    /// <returns>The processing operations.</returns>
    IMarkdownProcessingOperations CreateProcessingOperations();

    /// <summary>Creates safe diagnostics, source-map, and source-preserving operations for a document.</summary>
    /// <param name="document">The document to inspect or update.</param>
    /// <returns>The document operations.</returns>
    IMarkdownDocumentOperations CreateDocumentOperations(MarkdownDocument document);

    /// <summary>Creates partial and incremental parsing operations for editor scenarios.</summary>
    /// <returns>The editor operations.</returns>
    IMarkdownEditorOperations CreateEditorOperations();

    /// <summary>Creates cloning, search, and traversal operations for Markdown trees.</summary>
    /// <returns>The tree operations.</returns>
    IMarkdownTreeOperations CreateTreeOperations();

    /// <summary>Creates invariant-preserving operations for an AST node.</summary>
    /// <param name="node">The node to inspect or edit.</param>
    /// <returns>The node operations.</returns>
    IMarkdownNodeOperations CreateNodeOperations(MarkdownNode node);

    /// <summary>Creates a typed block wrapper around an existing node.</summary>
    /// <param name="node">The block node to wrap.</param>
    /// <returns>The typed block wrapper.</returns>
    MarkdownBlockNode CreateBlockNode(MarkdownNode node);

    /// <summary>Creates a typed inline wrapper around an existing node.</summary>
    /// <param name="node">The inline node to wrap.</param>
    /// <returns>The typed inline wrapper.</returns>
    MarkdownInlineNode CreateInlineNode(MarkdownNode node);

    /// <summary>Creates a named extension wrapper around an existing node.</summary>
    /// <param name="extensionName">The extension name.</param>
    /// <param name="node">The extension node to wrap.</param>
    /// <returns>The named extension wrapper.</returns>
    MarkdownExtensionNode CreateExtensionNode(string extensionName, MarkdownNode node);

    /// <summary>Analyzes an existing document.</summary>
    /// <param name="document">The document to analyze.</param>
    /// <returns>The aggregate analysis.</returns>
    MarkdownDocumentAnalysis AnalyzeDocument(MarkdownDocument document);

    /// <summary>Analyzes an existing document with configurable TOC and reading-time settings.</summary>
    /// <param name="document">The document to analyze.</param>
    /// <param name="options">The analysis options.</param>
    /// <returns>The aggregate analysis.</returns>
    MarkdownDocumentAnalysis AnalyzeDocumentWithOptions(MarkdownDocument document, MarkdownAnalysisOptions options);

    /// <summary>Analyzes an existing document using a supplied pipeline and default analysis settings.</summary>
    /// <param name="document">The document to analyze.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The aggregate analysis.</returns>
    MarkdownDocumentAnalysis AnalyzeDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline);

    /// <summary>Analyzes an existing document with rendering and analysis settings.</summary>
    /// <param name="document">The document to analyze.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <param name="options">The analysis options.</param>
    /// <returns>The aggregate analysis.</returns>
    MarkdownDocumentAnalysis AnalyzeDocumentWithPipelineAndOptions(
        MarkdownDocument document,
        MarkdownPipeline pipeline,
        MarkdownAnalysisOptions options);

    /// <summary>Parses and analyzes source with CommonMark defaults.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The aggregate analysis.</returns>
    MarkdownDocumentAnalysis AnalyzeSource(string source);

    /// <summary>Parses and analyzes source with configurable TOC and reading-time settings.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="options">The analysis options.</param>
    /// <returns>The aggregate analysis.</returns>
    MarkdownDocumentAnalysis AnalyzeSourceWithOptions(string source, MarkdownAnalysisOptions options);

    /// <summary>Parses and analyzes source with a supplied pipeline.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The aggregate analysis.</returns>
    MarkdownDocumentAnalysis AnalyzeSourceWithPipeline(string source, MarkdownPipeline pipeline);

    /// <summary>Parses and analyzes source with rendering and analysis settings.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <param name="options">The analysis options.</param>
    /// <returns>The aggregate analysis.</returns>
    MarkdownDocumentAnalysis AnalyzeSourceWithPipelineAndOptions(
        string source,
        MarkdownPipeline pipeline,
        MarkdownAnalysisOptions options);

    /// <summary>Reads the depth-first event sequence for a document.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>The detached event snapshots.</returns>
    MarkdownEvent[] ReadDocumentEvents(MarkdownDocument document);

    /// <summary>Reads the depth-first event sequence for an AST subtree.</summary>
    /// <param name="node">The subtree root to read.</param>
    /// <returns>The detached event snapshots.</returns>
    MarkdownEvent[] ReadNodeEvents(MarkdownNode node);

    /// <summary>Parses source with CommonMark defaults and reads its depth-first event sequence.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <returns>The detached event snapshots.</returns>
    MarkdownEvent[] ReadSourceEvents(string source);

    /// <summary>Parses source with a supplied pipeline and reads its depth-first event sequence.</summary>
    /// <param name="source">The Markdown source.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The detached event snapshots.</returns>
    MarkdownEvent[] ReadSourceEventsWithPipeline(string source, MarkdownPipeline pipeline);

    /// <summary>Creates a named rule registration.</summary>
    /// <param name="name">The stable rule name.</param>
    /// <param name="rule">The caller-supplied rule object.</param>
    /// <returns>The rule registration.</returns>
    MarkdownRuleRegistration CreateRuleRegistration(string name, object rule);

    /// <summary>Builds source-map entries for a document.</summary>
    /// <param name="document">The document to map.</param>
    /// <returns>The detached source-map entries.</returns>
    MarkdownSourceMapEntry[] GetSourceMapEntries(MarkdownDocument document);

    /// <summary>Classifies a URI using the renderer safety policy.</summary>
    /// <param name="uri">The URI text to classify.</param>
    /// <returns>The URI classification.</returns>
    MarkdownUriClassification ClassifyUri(string uri);
}

/// <summary>
/// Defines exception-less URI classification and raw-HTML decision helpers.
/// </summary>
public interface IMarkdownSafetyOperations
{
    /// <summary>Evaluates a possibly null link destination using a supplied pipeline.</summary>
    /// <param name="destination">The destination, or <c>null</c>.</param>
    /// <param name="pipeline">The safety and rewrite settings.</param>
    /// <returns>The evaluation result.</returns>
    MarkdownUriEvaluation EvaluateLinkDestination(string? destination, MarkdownPipeline pipeline);
    /// <summary>Evaluates a possibly null image destination using a supplied pipeline.</summary>
    /// <param name="destination">The destination, or <c>null</c>.</param>
    /// <param name="pipeline">The safety and rewrite settings.</param>
    /// <returns>The evaluation result.</returns>
    MarkdownUriEvaluation EvaluateImageDestination(string? destination, MarkdownPipeline pipeline);
    /// <summary>Classifies a possibly null URI without throwing for missing or unsafe input.</summary>
    /// <param name="destination">The destination, or <c>null</c>.</param>
    /// <returns>The evaluation containing the safe-default classification.</returns>
    MarkdownUriEvaluation ClassifyDestinationOrDefault(string? destination);
    /// <summary>Evaluates a possibly null raw HTML literal using a supplied pipeline.</summary>
    /// <param name="literal">The raw HTML literal, or <c>null</c>.</param>
    /// <param name="pipeline">The safety settings.</param>
    /// <returns>The explicit filtering decision.</returns>
    MarkdownRawHtmlEvaluation EvaluateRawHtml(string? literal, MarkdownPipeline pipeline);
    /// <summary>Applies the selected raw-HTML decision with output-size enforcement.</summary>
    /// <param name="literal">The raw HTML literal, or <c>null</c>.</param>
    /// <param name="pipeline">The safety and output-limit settings.</param>
    /// <returns>The bounded rendered text.</returns>
    string RenderRawHtmlLiteral(string? literal, MarkdownPipeline pipeline);
}

/// <summary>
/// Defines normalization, ordered transforms, reference resolution, and link and image rewrite operations.
/// </summary>
public interface IMarkdownProcessingOperations
{
    /// <summary>Renders a document as normalized CommonMark.</summary>
    /// <param name="document">The document to normalize.</param>
    /// <returns>The normalized Markdown.</returns>
    string NormalizeDocument(MarkdownDocument document);

    /// <summary>Renders a document as normalized Markdown using a supplied pipeline.</summary>
    /// <param name="document">The document to normalize.</param>
    /// <param name="pipeline">The rendering pipeline.</param>
    /// <returns>The normalized Markdown.</returns>
    string NormalizeDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline);

    /// <summary>Applies one document transform using CommonMark settings.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="transform">The transform to apply.</param>
    /// <returns>The same document instance.</returns>
    MarkdownDocument ApplyDocumentTransform(MarkdownDocument document, IMarkdownTransform transform);

    /// <summary>Applies one document transform using supplied pipeline settings.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The same document instance.</returns>
    MarkdownDocument ApplyDocumentTransformWithPipeline(MarkdownDocument document, IMarkdownTransform transform, MarkdownPipeline pipeline);

    /// <summary>Applies document transforms in array order using supplied pipeline settings.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="transforms">The transforms in application order.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The same document instance.</returns>
    MarkdownDocument ApplyDocumentTransformsInOrder(MarkdownDocument document, IMarkdownTransform[] transforms, MarkdownPipeline pipeline);

    /// <summary>Applies one node transform in preorder using CommonMark settings.</summary>
    /// <param name="document">The document whose nodes are visited.</param>
    /// <param name="transform">The node transform to apply.</param>
    /// <returns>The same document instance.</returns>
    MarkdownDocument ApplyNodeTransform(MarkdownDocument document, IMarkdownNodeTransform transform);

    /// <summary>Applies one node transform in preorder using supplied pipeline settings.</summary>
    /// <param name="document">The document whose nodes are visited.</param>
    /// <param name="transform">The node transform to apply.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The same document instance.</returns>
    MarkdownDocument ApplyNodeTransformWithPipeline(MarkdownDocument document, IMarkdownNodeTransform transform, MarkdownPipeline pipeline);

    /// <summary>Applies node transforms in array order, with each transform visiting a preorder snapshot.</summary>
    /// <param name="document">The document whose nodes are visited.</param>
    /// <param name="transforms">The transforms in application order.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The same document instance.</returns>
    MarkdownDocument ApplyNodeTransformsInOrder(MarkdownDocument document, IMarkdownNodeTransform[] transforms, MarkdownPipeline pipeline);

    /// <summary>Applies transforms registered in a pipeline in registration order.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="pipeline">The pipeline containing the ordered transforms.</param>
    /// <returns>The same document instance.</returns>
    MarkdownDocument ApplyPipelineTransforms(MarkdownDocument document, MarkdownPipeline pipeline);

    /// <summary>Resolves link and image reference nodes against the first matching definition.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <returns>The number of references resolved.</returns>
    int ResolveReferences(MarkdownDocument document);

    /// <summary>Rewrites every link-reference definition destination, then resolves dependent references.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="rewriter">The destination callback.</param>
    /// <returns>The number of definitions offered to the callback.</returns>
    int RewriteLinkDefinitions(MarkdownDocument document, IMarkdownDestinationRewriter rewriter);

    /// <summary>Rewrites every link node destination.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="rewriter">The destination callback.</param>
    /// <returns>The number of links offered to the callback.</returns>
    int RewriteLinkDestinations(MarkdownDocument document, IMarkdownDestinationRewriter rewriter);

    /// <summary>Rewrites labels on reference-style image nodes, then resolves their destinations.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="rewriter">The reference-label callback.</param>
    /// <returns>The number of image references offered to the callback.</returns>
    int RewriteImageReferences(MarkdownDocument document, IMarkdownReferenceRewriter rewriter);
}

/// <summary>
/// Defines safe document diagnostics, precise source-map queries, and source-preserving rendering.
/// </summary>
public interface IMarkdownDocumentOperations
{
    /// <summary>Returns the document operated on by this instance.</summary>
    /// <returns>The active document.</returns>
    MarkdownDocument GetDocument();
    /// <summary>Returns the document diagnostic count.</summary>
    /// <returns>The diagnostic count.</returns>
    int GetDiagnosticCount();
    /// <summary>Reports whether document diagnostics exist.</summary>
    /// <returns>The diagnostic state.</returns>
    bool HasDiagnostics();
    /// <summary>Reports whether at least one error diagnostic exists.</summary>
    /// <returns>The error state.</returns>
    bool HasErrors();
    /// <summary>Returns a detached diagnostic by index.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic copy.</returns>
    MarkdownDiagnostic GetDiagnosticAt(int index);
    /// <summary>Returns detached copies of all document diagnostics.</summary>
    /// <returns>The diagnostic copies.</returns>
    MarkdownDiagnostic[] GetDiagnostics();
    /// <summary>Finds diagnostics whose half-open spans contain an offset.</summary>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The matching diagnostic copies.</returns>
    MarkdownDiagnostic[] FindDiagnosticsContainingOffset(int offset);
    /// <summary>Finds diagnostics whose spans intersect a query span.</summary>
    /// <param name="span">The half-open query span.</param>
    /// <returns>The matching diagnostic copies.</returns>
    MarkdownDiagnostic[] FindDiagnosticsIntersectingSpan(MarkdownSourceSpan span);
    /// <summary>Adds a detached diagnostic copy.</summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    void AddDiagnostic(MarkdownDiagnostic diagnostic);
    /// <summary>Creates and adds a diagnostic.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="span">The diagnostic source span.</param>
    void AddDiagnosticValues(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span);
    /// <summary>Creates and adds a source-named diagnostic.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="span">The diagnostic source span.</param>
    /// <param name="sourceName">The canonical source name.</param>
    void AddDiagnosticValuesWithSourceName(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span, string sourceName);
    /// <summary>Removes all document diagnostics.</summary>
    void ClearDiagnostics();
    /// <summary>Returns detached entries for every mapped non-empty node span.</summary>
    /// <returns>The source-map entries.</returns>
    MarkdownSourceMapEntry[] GetSourceMapEntries();
    /// <summary>Finds source-map entries containing an offset.</summary>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The matching entries from outermost to innermost.</returns>
    MarkdownSourceMapEntry[] FindSourceMapEntriesContainingOffset(int offset);
    /// <summary>Finds source-map entries containing a source position.</summary>
    /// <param name="position">The source position whose offset is queried.</param>
    /// <returns>The matching entries from outermost to innermost.</returns>
    MarkdownSourceMapEntry[] FindSourceMapEntriesContainingPosition(MarkdownSourcePosition position);
    /// <summary>Finds source-map entries intersecting a half-open source span.</summary>
    /// <param name="span">The query span.</param>
    /// <returns>The intersecting entries.</returns>
    MarkdownSourceMapEntry[] FindSourceMapEntriesIntersectingSpan(MarkdownSourceSpan span);
    /// <summary>Finds the deepest source-map entry containing an offset.</summary>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The deepest detached entry, or <c>null</c>.</returns>
    MarkdownSourceMapEntry? FindDeepestSourceMapEntryAtOffset(int offset);
    /// <summary>Finds the deepest source-map entry containing a position.</summary>
    /// <param name="position">The source position whose offset is queried.</param>
    /// <returns>The deepest detached entry, or <c>null</c>.</returns>
    MarkdownSourceMapEntry? FindDeepestSourceMapEntryAtPosition(MarkdownSourcePosition position);
    /// <summary>Finds the deepest node containing an offset.</summary>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The mapped node, or <c>null</c>.</returns>
    MarkdownNode? FindDeepestNodeAtOffset(int offset);
    /// <summary>Finds mapped nodes of a kind containing an offset.</summary>
    /// <param name="kind">The node kind.</param>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The matching node references.</returns>
    MarkdownNode[] FindNodesAtOffset(MarkdownNodeKind kind, int offset);
    /// <summary>Checks whether a half-open source span contains an offset.</summary>
    /// <param name="span">The source span.</param>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>Whether the offset is contained.</returns>
    bool DoesSourceSpanContainOffset(MarkdownSourceSpan span, int offset);
    /// <summary>Checks whether two half-open source spans intersect.</summary>
    /// <param name="first">The first source span.</param>
    /// <param name="second">The second source span.</param>
    /// <returns>Whether the spans intersect.</returns>
    bool DoSourceSpansIntersect(MarkdownSourceSpan first, MarkdownSourceSpan second);
    /// <summary>Returns the first original line-ending sequence, or an empty string.</summary>
    /// <returns>The original line ending.</returns>
    string GetOriginalLineEnding();
    /// <summary>Renders unchanged trivia and regenerated edited regions using CommonMark settings.</summary>
    /// <returns>The source-preserving Markdown.</returns>
    string RenderSourcePreserving();
    /// <summary>Renders unchanged trivia and regenerated edited regions using supplied settings.</summary>
    /// <param name="pipeline">The rendering pipeline.</param>
    /// <returns>The source-preserving Markdown.</returns>
    string RenderSourcePreservingWithPipeline(MarkdownPipeline pipeline);
}

/// <summary>
/// Defines contextual partial parsing and correctness-preserving incremental reparsing operations.
/// </summary>
public interface IMarkdownEditorOperations
{
    /// <summary>Parses the blocks intersecting an absolute source range using CommonMark defaults.</summary>
    /// <param name="source">The complete Markdown source.</param>
    /// <param name="startOffset">The absolute range start.</param>
    /// <param name="length">The range length.</param>
    /// <returns>A document fragment with absolute source spans.</returns>
    MarkdownDocument ParsePartialSource(string source, int startOffset, int length);
    /// <summary>Parses the blocks intersecting an absolute source range using a supplied pipeline.</summary>
    /// <param name="source">The complete Markdown source.</param>
    /// <param name="startOffset">The absolute range start.</param>
    /// <param name="length">The range length.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>A document fragment with absolute source spans.</returns>
    MarkdownDocument ParsePartialSourceWithPipeline(string source, int startOffset, int length, MarkdownPipeline pipeline);
    /// <summary>Applies a source change and reparses a document using CommonMark defaults.</summary>
    /// <param name="document">The document to update in place.</param>
    /// <param name="change">The contiguous source replacement.</param>
    /// <returns>The incremental parse result.</returns>
    MarkdownIncrementalParseResult ReparseDocument(MarkdownDocument document, MarkdownTextChange change);
    /// <summary>Applies a source change and reparses a document using a supplied pipeline.</summary>
    /// <param name="document">The document to update in place.</param>
    /// <param name="change">The contiguous source replacement.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The incremental parse result.</returns>
    MarkdownIncrementalParseResult ReparseDocumentWithPipeline(MarkdownDocument document, MarkdownTextChange change, MarkdownPipeline pipeline);
}

/// <summary>
/// Defines cloning, kind-based search, and depth-first traversal operations for Markdown trees.
/// </summary>
public interface IMarkdownTreeOperations
{
    /// <summary>Deeply clones a node and all descendants.</summary>
    /// <param name="node">The node to clone.</param>
    /// <returns>The detached node clone.</returns>
    MarkdownNode CloneNode(MarkdownNode node);
    /// <summary>Deeply clones a document, its tree, and diagnostics.</summary>
    /// <param name="document">The document to clone.</param>
    /// <returns>The detached document clone.</returns>
    MarkdownDocument CloneDocument(MarkdownDocument document);
    /// <summary>Finds the first matching node in preorder.</summary>
    /// <param name="root">The subtree root to include in the search.</param>
    /// <param name="kind">The node kind to find.</param>
    /// <returns>The first matching node, or <c>null</c>.</returns>
    MarkdownNode? FindFirstInNode(MarkdownNode root, MarkdownNodeKind kind);
    /// <summary>Finds the first matching document node in preorder.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The node kind to find.</param>
    /// <returns>The first matching node, or <c>null</c>.</returns>
    MarkdownNode? FindFirstInDocument(MarkdownDocument document, MarkdownNodeKind kind);
    /// <summary>Finds every matching node in preorder.</summary>
    /// <param name="root">The subtree root to include in the search.</param>
    /// <param name="kind">The node kind to find.</param>
    /// <returns>The matching nodes in preorder.</returns>
    MarkdownNode[] FindAllInNode(MarkdownNode root, MarkdownNodeKind kind);
    /// <summary>Finds every matching document node in preorder.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The node kind to find.</param>
    /// <returns>The matching nodes in preorder.</returns>
    MarkdownNode[] FindAllInDocument(MarkdownDocument document, MarkdownNodeKind kind);
    /// <summary>Walks a subtree with preorder enter and postorder leave callbacks.</summary>
    /// <param name="root">The subtree root to walk.</param>
    /// <param name="visitor">The visitor receiving traversal callbacks.</param>
    /// <returns>The traversal result.</returns>
    MarkdownTreeWalkResult WalkNode(MarkdownNode root, IMarkdownNodeVisitor visitor);
    /// <summary>Walks a document with preorder enter and postorder leave callbacks.</summary>
    /// <param name="document">The document to walk.</param>
    /// <param name="visitor">The visitor receiving traversal callbacks.</param>
    /// <returns>The traversal result.</returns>
    MarkdownTreeWalkResult WalkDocument(MarkdownDocument document, IMarkdownNodeVisitor visitor);
}

/// <summary>
/// Defines invariant-preserving inspection and editing operations for one Markdown AST node.
/// </summary>
public interface IMarkdownNodeOperations
{
    /// <summary>Returns the node operated on by this instance.</summary>
    /// <returns>The active node.</returns>
    MarkdownNode GetNode();
    /// <summary>Returns the node literal.</summary>
    /// <returns>The current literal text.</returns>
    string GetLiteral();
    /// <summary>Sets the node literal and invalidates affected captured trivia.</summary>
    /// <param name="literal">The replacement literal.</param>
    void SetLiteral(string literal);
    /// <summary>Returns the number of direct children.</summary>
    /// <returns>The child count.</returns>
    int GetChildCount();
    /// <summary>Reports whether the node has direct children.</summary>
    /// <returns>The child state.</returns>
    bool HasChildren();
    /// <summary>Returns a direct child by index.</summary>
    /// <param name="index">The zero-based child index.</param>
    /// <returns>The child node.</returns>
    MarkdownNode GetChildAt(int index);
    /// <summary>Returns a detached array containing the direct child references.</summary>
    /// <returns>The child references.</returns>
    MarkdownNode[] GetChildren();
    /// <summary>Appends a child, reparenting it when necessary.</summary>
    /// <param name="child">The child to append.</param>
    void AppendChild(MarkdownNode child);
    /// <summary>Prepends a child, reparenting it when necessary.</summary>
    /// <param name="child">The child to prepend.</param>
    void PrependChild(MarkdownNode child);
    /// <summary>Inserts a child at an index, reparenting it when necessary.</summary>
    /// <param name="index">The insertion index.</param>
    /// <param name="child">The child to insert.</param>
    void InsertChildAt(int index, MarkdownNode child);
    /// <summary>Inserts a child before an attached child.</summary>
    /// <param name="existingChild">The attached child.</param>
    /// <param name="newChild">The child to insert.</param>
    void InsertChildBefore(MarkdownNode existingChild, MarkdownNode newChild);
    /// <summary>Inserts a child after an attached child.</summary>
    /// <param name="existingChild">The attached child.</param>
    /// <param name="newChild">The child to insert.</param>
    void InsertChildAfter(MarkdownNode existingChild, MarkdownNode newChild);
    /// <summary>Replaces an attached child.</summary>
    /// <param name="existingChild">The attached child to replace.</param>
    /// <param name="newChild">The replacement child.</param>
    /// <returns>The detached replaced child.</returns>
    MarkdownNode ReplaceChild(MarkdownNode existingChild, MarkdownNode newChild);
    /// <summary>Removes a child when it is attached to this node.</summary>
    /// <param name="child">The child to remove.</param>
    /// <returns>Whether the child was removed.</returns>
    bool RemoveChild(MarkdownNode child);
    /// <summary>Removes and returns the child at an index.</summary>
    /// <param name="index">The zero-based child index.</param>
    /// <returns>The detached child.</returns>
    MarkdownNode RemoveChildAt(int index);
    /// <summary>Detaches every direct child.</summary>
    void ClearChildren();
    /// <summary>Detaches the active node from its parent.</summary>
    /// <returns>Whether the node was detached.</returns>
    bool Detach();
    /// <summary>Reports whether the active node has a parent.</summary>
    /// <returns>The parent state.</returns>
    bool HasParent();
    /// <summary>Returns the parent, or <c>null</c> while detached.</summary>
    /// <returns>The parent node or <c>null</c>.</returns>
    MarkdownNode? GetParent();
    /// <summary>Returns the first child, or <c>null</c>.</summary>
    /// <returns>The first child or <c>null</c>.</returns>
    MarkdownNode? GetFirstChild();
    /// <summary>Returns the last child, or <c>null</c>.</summary>
    /// <returns>The last child or <c>null</c>.</returns>
    MarkdownNode? GetLastChild();
    /// <summary>Returns the previous sibling, or <c>null</c>.</summary>
    /// <returns>The previous sibling or <c>null</c>.</returns>
    MarkdownNode? GetPreviousSibling();
    /// <summary>Returns the next sibling, or <c>null</c>.</summary>
    /// <returns>The next sibling or <c>null</c>.</returns>
    MarkdownNode? GetNextSibling();
    /// <summary>Returns the node index in its parent, or minus one.</summary>
    /// <returns>The zero-based index, or minus one.</returns>
    int GetIndexInParent();
    /// <summary>Returns the number of attributes.</summary>
    /// <returns>The attribute count.</returns>
    int GetAttributeCount();
    /// <summary>Reports whether the node has attributes.</summary>
    /// <returns>The attribute state.</returns>
    bool HasAttributes();
    /// <summary>Reports whether a named attribute exists.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>Whether the attribute exists.</returns>
    bool ContainsAttribute(string name);
    /// <summary>Returns a detached attribute copy, or <c>null</c>.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The attribute copy or <c>null</c>.</returns>
    MarkdownAttribute? GetAttribute(string name);
    /// <summary>Returns an attribute value, or an empty string when absent.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The attribute value.</returns>
    string GetAttributeValue(string name);
    /// <summary>Returns detached copies of all attributes.</summary>
    /// <returns>The attribute copies.</returns>
    MarkdownAttribute[] GetAttributes();
    /// <summary>Adds or replaces an attribute.</summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    void SetAttribute(string name, string value);
    /// <summary>Removes a named attribute.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>Whether an attribute was removed.</returns>
    bool RemoveAttribute(string name);
    /// <summary>Removes every attribute.</summary>
    void ClearAttributes();
    /// <summary>Reports whether captured source trivia is attached.</summary>
    /// <returns>The trivia state.</returns>
    bool HasTrivia();
    /// <summary>Returns a detached trivia copy, or <c>null</c>.</summary>
    /// <returns>The trivia copy or <c>null</c>.</returns>
    MarkdownTrivia? GetTrivia();
    /// <summary>Returns captured source text, or an empty string.</summary>
    /// <returns>The captured source text.</returns>
    string GetTriviaText();
    /// <summary>Returns the captured original line-ending sequence, or an empty string.</summary>
    /// <returns>The original line ending.</returns>
    string GetTriviaLineEnding();
    /// <summary>Returns the captured opening delimiter, or an empty string.</summary>
    /// <returns>The opening delimiter.</returns>
    string GetOpeningDelimiter();
    /// <summary>Returns the captured closing delimiter, or an empty string.</summary>
    /// <returns>The closing delimiter.</returns>
    string GetClosingDelimiter();
    /// <summary>Attaches a detached trivia copy and invalidates ancestor trivia.</summary>
    /// <param name="trivia">The trivia to attach.</param>
    void SetTrivia(MarkdownTrivia trivia);
    /// <summary>Clears node trivia and invalidates ancestor trivia.</summary>
    void ClearTrivia();
    /// <summary>Returns the number of node-local diagnostics.</summary>
    /// <returns>The diagnostic count.</returns>
    int GetDiagnosticCount();
    /// <summary>Reports whether node-local diagnostics exist.</summary>
    /// <returns>The diagnostic state.</returns>
    bool HasDiagnostics();
    /// <summary>Returns detached copies of node-local diagnostics.</summary>
    /// <returns>The diagnostic copies.</returns>
    MarkdownDiagnostic[] GetDiagnostics();
    /// <summary>Returns a detached node-local diagnostic by index.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic copy.</returns>
    MarkdownDiagnostic GetDiagnosticAt(int index);
    /// <summary>Finds node-local diagnostics whose half-open spans contain an offset.</summary>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The matching diagnostic copies.</returns>
    MarkdownDiagnostic[] FindDiagnosticsContainingOffset(int offset);
    /// <summary>Finds node-local diagnostics whose spans intersect a query span.</summary>
    /// <param name="span">The half-open query span.</param>
    /// <returns>The matching diagnostic copies.</returns>
    MarkdownDiagnostic[] FindDiagnosticsIntersectingSpan(MarkdownSourceSpan span);
    /// <summary>Adds a detached copy of a node-local diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    void AddDiagnostic(MarkdownDiagnostic diagnostic);
    /// <summary>Creates and adds a node-local diagnostic.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="span">The diagnostic source span.</param>
    void AddDiagnosticValues(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span);
    /// <summary>Removes every node-local diagnostic.</summary>
    void ClearDiagnostics();
}

/// <summary>
/// Defines read-only operations exposed by a configured Markdown pipeline.
/// </summary>
public interface IMarkdownPipeline
{
    /// <summary>Returns a detached options copy.</summary>
    /// <returns>The options copy.</returns>
    MarkdownOptions GetOptions();
    /// <summary>Returns a detached feature-set copy.</summary>
    /// <returns>The feature-set copy.</returns>
    MarkdownFeatureSet GetFeatures();
    /// <summary>Returns detached block-rule registration snapshots.</summary>
    /// <returns>The registrations in evaluation order.</returns>
    MarkdownRuleRegistration[] GetBlockRuleRegistrations();
    /// <summary>Returns detached inline-rule registration snapshots.</summary>
    /// <returns>The registrations in evaluation order.</returns>
    MarkdownRuleRegistration[] GetInlineRuleRegistrations();
    /// <summary>Returns detached transform registration snapshots.</summary>
    /// <returns>The registrations in evaluation order.</returns>
    MarkdownRuleRegistration[] GetTransformRegistrations();
    /// <summary>Returns detached HTML rendering-rule registration snapshots.</summary>
    /// <returns>The registrations in evaluation order.</returns>
    MarkdownRuleRegistration[] GetHtmlRenderRuleRegistrations();
    /// <summary>Reports whether CommonMark is enabled.</summary>
    /// <returns>The feature state.</returns>
    bool UsesCommonMark();
    /// <summary>Reports whether GFM is enabled.</summary>
    /// <returns>The feature state.</returns>
    bool UsesGfm();
    /// <summary>Reports whether advanced extensions are enabled.</summary>
    /// <returns>The feature state.</returns>
    bool UsesAdvancedExtensions();
    /// <summary>Reports whether raw HTML is allowed.</summary>
    /// <returns>The option state.</returns>
    bool AllowsRawHtml();
    /// <summary>Reports whether unsafe HTML is allowed.</summary>
    /// <returns>The option state.</returns>
    bool AllowsUnsafeHtml();
    /// <summary>Reports whether unsafe links are allowed.</summary>
    /// <returns>The option state.</returns>
    bool AllowsUnsafeLinks();
    /// <summary>Reports whether source trivia is tracked.</summary>
    /// <returns>The option state.</returns>
    bool TracksTrivia();
}

/// <summary>
/// Defines the supported mutable pipeline-building operations.
/// </summary>
public interface IMarkdownPipelineBuilder
{
    /// <summary>Returns a detached options copy.</summary>
    /// <returns>The options copy.</returns>
    MarkdownOptions GetOptions();
    /// <summary>Builds an independent pipeline.</summary>
    /// <returns>The configured pipeline.</returns>
    MarkdownPipeline Build();
    /// <summary>Returns detached block-rule registration snapshots.</summary>
    /// <returns>The registrations in evaluation order.</returns>
    MarkdownRuleRegistration[] GetBlockRuleRegistrations();
    /// <summary>Returns detached inline-rule registration snapshots.</summary>
    /// <returns>The registrations in evaluation order.</returns>
    MarkdownRuleRegistration[] GetInlineRuleRegistrations();
    /// <summary>Returns detached transform registration snapshots.</summary>
    /// <returns>The registrations in evaluation order.</returns>
    MarkdownRuleRegistration[] GetTransformRegistrations();
    /// <summary>Returns detached HTML rendering-rule registration snapshots.</summary>
    /// <returns>The registrations in evaluation order.</returns>
    MarkdownRuleRegistration[] GetHtmlRenderRuleRegistrations();
    /// <summary>Replaces the builder options.</summary>
    /// <param name="options">The options to copy.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetOptions(MarkdownOptions options);
    /// <summary>Replaces every syntax feature flag.</summary>
    /// <param name="features">The feature set to copy.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetFeatures(MarkdownFeatureSet features);
    /// <summary>Enables the CommonMark preset.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder UseCommonMark();
    /// <summary>Enables the GFM preset.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder UseGfm();
    /// <summary>Enables all supported extensions.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder UseAdvancedExtensions();
    /// <summary>Disables all syntax features.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder UseZero();
    /// <summary>Disables raw and unsafe HTML rendering.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder DisableHtml();
    /// <summary>Allows raw HTML rendering.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder AllowRawHtml();
    /// <summary>Allows unsafe raw HTML rendering.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder AllowUnsafeHtml();
    /// <summary>Allows unsafe link destinations.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder AllowUnsafeLinks();
    /// <summary>Sets the policy used to allow or reject rewritten link destinations.</summary>
    /// <param name="policy">The link URI policy.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetLinkUriPolicy(IMarkdownUriPolicy policy);
    /// <summary>Clears the custom link URI policy.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearLinkUriPolicy();
    /// <summary>Sets the policy used to allow or reject rewritten image destinations.</summary>
    /// <param name="policy">The image URI policy.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetImageUriPolicy(IMarkdownUriPolicy policy);
    /// <summary>Clears the custom image URI policy.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearImageUriPolicy();
    /// <summary>Sets the render-time link destination rewrite hook.</summary>
    /// <param name="rewriter">The destination rewriter.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RewriteLinkUrisWith(IMarkdownDestinationRewriter rewriter);
    /// <summary>Clears the render-time link destination rewrite hook.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearLinkUriRewriter();
    /// <summary>Sets the render-time image destination rewrite hook.</summary>
    /// <param name="rewriter">The destination rewriter.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RewriteImageUrisWith(IMarkdownDestinationRewriter rewriter);
    /// <summary>Clears the render-time image destination rewrite hook.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearImageUriRewriter();
    /// <summary>Sets the explicit raw-HTML filtering policy.</summary>
    /// <param name="policy">The raw-HTML policy.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetRawHtmlPolicy(IMarkdownRawHtmlPolicy policy);
    /// <summary>Clears the custom raw-HTML policy.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearRawHtmlPolicy();
    /// <summary>Enables source trivia tracking.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder TrackTrivia();
    /// <summary>Enables XHTML-style empty elements.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder UseXhtml();
    /// <summary>Enables hard wrapping of soft source breaks.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder UseHardWrap();
    /// <summary>Configures deterministic rendering.</summary>
    /// <param name="value">The option value.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetDeterministicOutput(bool value);
    /// <summary>Configures soft-break rendering.</summary>
    /// <param name="mode">The soft-break mode.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetSoftBreakMode(MarkdownSoftBreakMode mode);
    /// <summary>Enables CJK-aware soft breaks.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder UseCjkSoftBreaks();
    /// <summary>Configures generated line endings.</summary>
    /// <param name="lineEnding">The line-ending sequence.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetLineEnding(string lineEnding);
    /// <summary>Maps an emoji shortcode to replacement text.</summary>
    /// <param name="name">The shortcode name.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder MapEmojiShortcode(string name, string replacement);
    /// <summary>Clears all emoji shortcode mappings.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearEmojiShortcodes();
    /// <summary>Registers a custom container name.</summary>
    /// <param name="name">The container name.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RegisterCustomContainer(string name);
    /// <summary>Clears all custom container registrations.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearCustomContainers();
    /// <summary>Registers a custom fenced-block information key.</summary>
    /// <param name="infoKey">The information-string key.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RegisterCustomFencedBlock(string infoKey);
    /// <summary>Clears all custom fenced-block registrations.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearCustomFencedBlocks();
    /// <summary>Registers a custom inline delimiter pair.</summary>
    /// <param name="name">The delimiter name.</param>
    /// <param name="openingDelimiter">The opening delimiter.</param>
    /// <param name="closingDelimiter">The closing delimiter.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RegisterCustomInlineDelimiter(string name, string openingDelimiter, string closingDelimiter);
    /// <summary>Clears all custom inline delimiter registrations.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearCustomInlineDelimiters();
    /// <summary>Sets the maximum AST nesting depth.</summary>
    /// <param name="value">The positive limit.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetMaximumNestingDepth(int value);
    /// <summary>Sets the maximum input size.</summary>
    /// <param name="value">The positive limit.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetMaximumInputSize(int value);
    /// <summary>Sets the maximum retained reference definitions.</summary>
    /// <param name="value">The positive limit.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetMaximumReferenceDefinitions(int value);
    /// <summary>Sets the maximum generated output size.</summary>
    /// <param name="value">The positive limit.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetMaximumOutputSize(int value);
    /// <summary>Sets the normalized Markdown paragraph wrapping width.</summary>
    /// <param name="value">Zero to disable wrapping, or a positive line width.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetMaximumLineWidth(int value);
    /// <summary>Enables include preprocessing with a caller-supplied resolver.</summary>
    /// <param name="resolver">The include resolver.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder UseIncludes(IMarkdownIncludeResolver resolver);
    /// <summary>Disables include preprocessing.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder DisableIncludes();
    /// <summary>Sets the canonical root source name used for relative includes and attribution.</summary>
    /// <param name="sourceName">The root source name.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetIncludeRootName(string sourceName);
    /// <summary>Sets the maximum include nesting depth.</summary>
    /// <param name="value">The positive depth limit.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder SetMaximumIncludeDepth(int value);
    /// <summary>Adds a block rule.</summary>
    /// <param name="rule">The rule to add.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder AddBlockRule(IMarkdownBlockRule rule);
    /// <summary>Inserts a block rule before a named registration.</summary>
    /// <param name="existingName">The registration that will follow the inserted rule.</param>
    /// <param name="rule">The rule to insert.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder InsertBlockRuleBefore(string existingName, IMarkdownBlockRule rule);
    /// <summary>Inserts a block rule after a named registration.</summary>
    /// <param name="existingName">The registration that will precede the inserted rule.</param>
    /// <param name="rule">The rule to insert.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder InsertBlockRuleAfter(string existingName, IMarkdownBlockRule rule);
    /// <summary>Replaces a named block-rule registration in place.</summary>
    /// <param name="existingName">The registration to replace.</param>
    /// <param name="rule">The replacement rule.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ReplaceBlockRule(string existingName, IMarkdownBlockRule rule);
    /// <summary>Removes a named block-rule registration.</summary>
    /// <param name="name">The registration name.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RemoveBlockRule(string name);
    /// <summary>Removes every block-rule registration.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearBlockRules();
    /// <summary>Adds an inline rule.</summary>
    /// <param name="rule">The rule to add.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder AddInlineRule(IMarkdownInlineRule rule);
    /// <summary>Inserts an inline rule before a named registration.</summary>
    /// <param name="existingName">The registration that will follow the inserted rule.</param>
    /// <param name="rule">The rule to insert.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder InsertInlineRuleBefore(string existingName, IMarkdownInlineRule rule);
    /// <summary>Inserts an inline rule after a named registration.</summary>
    /// <param name="existingName">The registration that will precede the inserted rule.</param>
    /// <param name="rule">The rule to insert.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder InsertInlineRuleAfter(string existingName, IMarkdownInlineRule rule);
    /// <summary>Replaces a named inline-rule registration in place.</summary>
    /// <param name="existingName">The registration to replace.</param>
    /// <param name="rule">The replacement rule.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ReplaceInlineRule(string existingName, IMarkdownInlineRule rule);
    /// <summary>Removes a named inline-rule registration.</summary>
    /// <param name="name">The registration name.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RemoveInlineRule(string name);
    /// <summary>Removes every inline-rule registration.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearInlineRules();
    /// <summary>Adds a transform.</summary>
    /// <param name="transform">The transform to add.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder AddTransform(IMarkdownTransform transform);
    /// <summary>Inserts a transform before a named registration.</summary>
    /// <param name="existingName">The registration that will follow the inserted transform.</param>
    /// <param name="transform">The transform to insert.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder InsertTransformBefore(string existingName, IMarkdownTransform transform);
    /// <summary>Inserts a transform after a named registration.</summary>
    /// <param name="existingName">The registration that will precede the inserted transform.</param>
    /// <param name="transform">The transform to insert.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder InsertTransformAfter(string existingName, IMarkdownTransform transform);
    /// <summary>Replaces a named transform registration in place.</summary>
    /// <param name="existingName">The registration to replace.</param>
    /// <param name="transform">The replacement transform.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ReplaceTransform(string existingName, IMarkdownTransform transform);
    /// <summary>Removes a named transform registration.</summary>
    /// <param name="name">The registration name.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RemoveTransform(string name);
    /// <summary>Removes every transform registration.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearTransforms();
    /// <summary>Adds an HTML render rule.</summary>
    /// <param name="rule">The rule to add.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder AddHtmlRenderRule(IHtmlRenderRule rule);
    /// <summary>Inserts an HTML rendering rule before a named registration.</summary>
    /// <param name="existingName">The registration that will follow the inserted rule.</param>
    /// <param name="rule">The rule to insert.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder InsertHtmlRenderRuleBefore(string existingName, IHtmlRenderRule rule);
    /// <summary>Inserts an HTML rendering rule after a named registration.</summary>
    /// <param name="existingName">The registration that will precede the inserted rule.</param>
    /// <param name="rule">The rule to insert.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder InsertHtmlRenderRuleAfter(string existingName, IHtmlRenderRule rule);
    /// <summary>Replaces a named HTML rendering-rule registration in place.</summary>
    /// <param name="existingName">The registration to replace.</param>
    /// <param name="rule">The replacement rule.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ReplaceHtmlRenderRule(string existingName, IHtmlRenderRule rule);
    /// <summary>Removes a named HTML rendering-rule registration.</summary>
    /// <param name="name">The registration name.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RemoveHtmlRenderRule(string name);
    /// <summary>Removes every HTML rendering-rule registration.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearHtmlRenderRules();
    /// <summary>Registers a callback for fenced and indented code blocks.</summary>
    /// <param name="handler">The callback that writes handled nodes.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RenderCodeBlocksWith(Func<HtmlRenderContext, MarkdownNode, bool> handler);
    /// <summary>Clears the code-block callback.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearCodeBlockRenderHook();
    /// <summary>Registers a callback for links.</summary>
    /// <param name="handler">The callback that writes handled nodes.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RenderLinksWith(Func<HtmlRenderContext, MarkdownNode, bool> handler);
    /// <summary>Clears the link callback.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearLinkRenderHook();
    /// <summary>Registers a callback for images.</summary>
    /// <param name="handler">The callback that writes handled nodes.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RenderImagesWith(Func<HtmlRenderContext, MarkdownNode, bool> handler);
    /// <summary>Clears the image callback.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearImageRenderHook();
    /// <summary>Registers a callback for block and inline raw HTML.</summary>
    /// <param name="handler">The callback that writes handled nodes.</param>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder RenderRawHtmlWith(Func<HtmlRenderContext, MarkdownNode, bool> handler);
    /// <summary>Clears the raw-HTML callback.</summary>
    /// <returns>This builder.</returns>
    MarkdownPipelineBuilder ClearRawHtmlRenderHook();
}

/// <summary>
/// Defines the controlled operations available to a block parsing rule.
/// </summary>
public interface IMarkdownBlockParseContext
{
    /// <summary>Reports whether a current line is available.</summary>
    /// <returns>The current-line state.</returns>
    bool HasCurrentLine();
    /// <summary>Returns the source line count.</summary>
    /// <returns>The line count.</returns>
    int GetLineCount();
    /// <summary>Advances to the next source line.</summary>
    /// <returns>Whether another line remains.</returns>
    bool AdvanceLine();
    /// <summary>Returns the current line index.</summary>
    /// <returns>The line index.</returns>
    int GetLineIndex();
    /// <summary>Returns the current line text.</summary>
    /// <returns>The current line.</returns>
    string GetCurrentLineText();
    /// <summary>Returns the current line span.</summary>
    /// <returns>The current span.</returns>
    MarkdownSourceSpan GetCurrentLineSpan();
    /// <summary>Returns the document being built.</summary>
    /// <returns>The active document.</returns>
    MarkdownDocument GetDocument();
    /// <summary>Returns the active pipeline.</summary>
    /// <returns>The active pipeline.</returns>
    MarkdownPipeline GetPipeline();
    /// <summary>Appends a block node.</summary>
    /// <param name="node">The node to append.</param>
    void AppendBlock(MarkdownNode node);
    /// <summary>Adds a parse diagnostic.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="span">The source span.</param>
    void AddDiagnostic(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span);
}

/// <summary>
/// Defines the controlled operations available to an inline parsing rule.
/// </summary>
public interface IMarkdownInlineParseContext
{
    /// <summary>Reports whether a current character is available.</summary>
    /// <returns>The current-character state.</returns>
    bool HasCurrentCharacter();
    /// <summary>Returns the current character.</summary>
    /// <returns>The current character.</returns>
    char GetCurrentCharacter();
    /// <summary>Advances one character.</summary>
    /// <returns>Whether another character remains.</returns>
    bool Advance();
    /// <summary>Advances several characters.</summary>
    /// <param name="count">The number of characters.</param>
    /// <returns>Whether another character remains.</returns>
    bool AdvanceBy(int count);
    /// <summary>Returns the unconsumed source.</summary>
    /// <returns>The remaining text.</returns>
    string GetRemainingText();
    /// <summary>Appends an inline node.</summary>
    /// <param name="node">The node to append.</param>
    void AppendInline(MarkdownNode node);
    /// <summary>Returns the inline source.</summary>
    /// <returns>The source text.</returns>
    string GetSource();
    /// <summary>Returns the current source position.</summary>
    /// <returns>The source position.</returns>
    int GetPosition();
    /// <summary>Returns the active inline container.</summary>
    /// <returns>The container node.</returns>
    MarkdownNode GetContainer();
    /// <summary>Returns the active pipeline.</summary>
    /// <returns>The active pipeline.</returns>
    MarkdownPipeline GetPipeline();
    /// <summary>Adds a parse diagnostic.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="span">The source span.</param>
    void AddDiagnostic(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span);
}

/// <summary>
/// Defines escaped and size-limited HTML writer operations.
/// </summary>
public interface IHtmlWriter
{
    /// <summary>Writes trusted HTML text.</summary>
    /// <param name="text">The text to write.</param>
    void Write(string text);
    /// <summary>Writes escaped text content.</summary>
    /// <param name="text">The text to write.</param>
    void WriteEscapedText(string text);
    /// <summary>Writes escaped attribute content.</summary>
    /// <param name="text">The text to write.</param>
    void WriteEscapedAttribute(string text);
}

/// <summary>
/// Defines a named block parser rule that can consume source lines and append block nodes.
/// </summary>
public interface IMarkdownBlockRule
{
    /// <summary>
    /// Returns the stable name used to register and order this rule.
    /// </summary>
    /// <returns>The stable rule name.</returns>
    string GetName();

    /// <summary>
    /// Attempts to parse a block at the current source line.
    /// </summary>
    /// <param name="context">The active block parsing context.</param>
    /// <returns><c>true</c> when the rule consumed input; otherwise, <c>false</c>.</returns>
    bool TryParse(MarkdownBlockParseContext context);
}

/// <summary>
/// Defines a named inline parser rule that can consume source characters and append inline nodes.
/// </summary>
public interface IMarkdownInlineRule
{
    /// <summary>
    /// Returns the stable name used to register and order this rule.
    /// </summary>
    /// <returns>The stable rule name.</returns>
    string GetName();

    /// <summary>
    /// Returns characters that can begin this rule outside the built-in Markdown punctuation set.
    /// </summary>
    /// <remarks>
    /// The text fallback uses these characters to end plain-text runs so the rule is offered the matching position. Return an empty string when no additional trigger is needed.
    /// </remarks>
    /// <returns>The additional trigger characters.</returns>
    string GetTriggerCharacters();

    /// <summary>
    /// Attempts to parse an inline node at the current source position.
    /// </summary>
    /// <param name="context">The active inline parsing context.</param>
    /// <returns><c>true</c> when the rule consumed input; otherwise, <c>false</c>.</returns>
    bool TryParse(MarkdownInlineParseContext context);
}

/// <summary>
/// Defines a named transformation that can inspect or change a parsed document.
/// </summary>
public interface IMarkdownTransform
{
    /// <summary>
    /// Returns the stable name used to register and order this transform.
    /// </summary>
    /// <returns>The stable transform name.</returns>
    string GetName();

    /// <summary>
    /// Applies the transform to a document.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="context">The context shared by the current operation.</param>
    void Apply(MarkdownDocument document, MarkdownTransformContext context);
}

/// <summary>
/// Defines a named transformation applied to nodes in preorder.
/// </summary>
public interface IMarkdownNodeTransform
{
    /// <summary>Returns the stable node-transform name.</summary>
    /// <returns>The stable transform name.</returns>
    string GetNodeTransformName();

    /// <summary>Applies the transformation to one node.</summary>
    /// <param name="node">The node to inspect or mutate.</param>
    /// <param name="context">The context shared by the current operation.</param>
    void ApplyToNode(MarkdownNode node, MarkdownTransformContext context);
}

/// <summary>
/// Rewrites a destination for a selected link, image, or link-reference-definition node.
/// </summary>
public interface IMarkdownDestinationRewriter
{
    /// <summary>Returns the replacement destination.</summary>
    /// <remarks>The callback may also edit other metadata on the supplied node.</remarks>
    /// <param name="node">The node whose destination is being rewritten.</param>
    /// <param name="destination">The current destination.</param>
    /// <returns>The non-null replacement destination.</returns>
    string RewriteDestination(MarkdownNode node, string destination);
}

/// <summary>
/// Rewrites the source label of a reference-style link or image node.
/// </summary>
public interface IMarkdownReferenceRewriter
{
    /// <summary>Returns the replacement source reference label.</summary>
    /// <param name="node">The reference-style node being rewritten.</param>
    /// <param name="referenceLabel">The current source label.</param>
    /// <returns>The non-null replacement label.</returns>
    string RewriteReference(MarkdownNode node, string referenceLabel);
}

/// <summary>
/// Defines a named HTML rendering rule that can replace built-in rendering for a node.
/// </summary>
public interface IHtmlRenderRule
{
    /// <summary>
    /// Returns the stable name used to register and order this rule.
    /// </summary>
    /// <returns>The stable rule name.</returns>
    string GetName();

    /// <summary>
    /// Attempts to render a node.
    /// </summary>
    /// <remarks>
    /// The rule writes through the supplied context and returns <c>true</c> when it handled the node. Returning <c>false</c> allows the next custom rule or the built-in renderer to run.
    /// </remarks>
    /// <param name="context">The active HTML rendering context.</param>
    /// <param name="node">The node offered to the rule.</param>
    /// <returns><c>true</c> when the rule rendered the node; otherwise, <c>false</c>.</returns>
    bool TryRender(HtmlRenderContext context, MarkdownNode node);
}

/// <summary>
/// Receives depth-first enter and leave callbacks from <see cref="MarkdownTreeWalker"/>.
/// </summary>
public interface IMarkdownNodeVisitor
{
    /// <summary>
    /// Visits a node before its children.
    /// </summary>
    /// <param name="context">The current traversal context.</param>
    /// <returns>The traversal action to perform.</returns>
    MarkdownTreeWalkResult VisitEnter(MarkdownVisitContext context);

    /// <summary>
    /// Visits a node after its children unless traversal was stopped.
    /// </summary>
    /// <param name="context">The current traversal context.</param>
    void VisitLeave(MarkdownVisitContext context);
}
