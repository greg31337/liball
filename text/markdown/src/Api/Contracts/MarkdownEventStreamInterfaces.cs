using System.IO;

namespace markdown;

/// <summary>
/// Defines a forward-only pull reader over Markdown enter and leave events.
/// </summary>
public interface IMarkdownEventReader
{
    /// <summary>Advances to the next event.</summary>
    /// <returns><c>true</c> when an event is available; otherwise, <c>false</c>.</returns>
    bool Read();
    /// <summary>Returns the mutable current event snapshot without allocating another copy.</summary>
    /// <returns>The current event, or an event whose kind is <c>None</c> outside the sequence.</returns>
    MarkdownEvent GetCurrent();
    /// <summary>Moves the reader before its first event.</summary>
    void Reset();
    /// <summary>Returns the parsed or supplied document when the reader has one.</summary>
    /// <returns>The document, or <c>null</c> for a standalone-node reader.</returns>
    MarkdownDocument? GetDocument();
    /// <summary>Returns the original source supplied to a source reader.</summary>
    /// <returns>The source, or an empty string for an AST reader.</returns>
    string GetSource();
    /// <summary>Returns detached parse diagnostics.</summary>
    /// <returns>The diagnostics in stable order.</returns>
    MarkdownDiagnostic[] GetDiagnostics();
    /// <summary>Reports whether parsing produced diagnostics.</summary>
    /// <returns><c>true</c> when diagnostics exist; otherwise, <c>false</c>.</returns>
    bool HasDiagnostics();
    /// <summary>Reports whether parsing produced errors.</summary>
    /// <returns><c>true</c> when error diagnostics exist; otherwise, <c>false</c>.</returns>
    bool HasErrors();
}

/// <summary>
/// Defines incremental validation, AST construction, and rendering for Markdown events.
/// </summary>
public interface IMarkdownEventWriter
{
    /// <summary>Writes one enter, leave, or no-op event.</summary>
    /// <param name="item">The event to validate and apply.</param>
    void Write(MarkdownEvent item);
    /// <summary>Consumes the remaining events from a pull reader.</summary>
    /// <param name="reader">The reader to consume.</param>
    void WriteFromReader(IMarkdownEventReader reader);
    /// <summary>Returns the number of nodes that remain open.</summary>
    /// <returns>The open-node count.</returns>
    int GetOpenNodeCount();
    /// <summary>Validates completion and returns the constructed document.</summary>
    /// <returns>The constructed document.</returns>
    MarkdownDocument FinishDocument();
    /// <summary>Renders the completed events as HTML using CommonMark defaults.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    void WriteHtml(TextWriter writer);
    /// <summary>Renders the completed events as HTML using a supplied pipeline.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteHtmlWithPipeline(TextWriter writer, MarkdownPipeline pipeline);
    /// <summary>Renders the completed events as Markdown using CommonMark defaults.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    void WriteMarkdown(TextWriter writer);
    /// <summary>Renders the completed events as Markdown using a supplied pipeline.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteMarkdownWithPipeline(TextWriter writer, MarkdownPipeline pipeline);
    /// <summary>Renders the completed events as plain text using CommonMark defaults.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    void WritePlainText(TextWriter writer);
    /// <summary>Renders the completed events as plain text using a supplied pipeline.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WritePlainTextWithPipeline(TextWriter writer, MarkdownPipeline pipeline);
    /// <summary>Renders the completed events as a debug tree using CommonMark defaults.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    void WriteDebugTree(TextWriter writer);
    /// <summary>Renders the completed events as a debug tree using a supplied pipeline.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteDebugTreeWithPipeline(TextWriter writer, MarkdownPipeline pipeline);
    /// <summary>Renders the completed events as an XML-like AST using CommonMark defaults.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    void WriteXmlAst(TextWriter writer);
    /// <summary>Renders the completed events as an XML-like AST using a supplied pipeline.</summary>
    /// <param name="writer">The caller-owned destination writer.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    void WriteXmlAstWithPipeline(TextWriter writer, MarkdownPipeline pipeline);
}
