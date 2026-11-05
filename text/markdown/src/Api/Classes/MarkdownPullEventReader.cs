using System;

namespace markdown;

/// <summary>
/// Pulls Markdown enter and leave events while retaining only depth-bounded traversal state.
/// </summary>
public class MarkdownPullEventReader : IMarkdownEventReader
{
    internal MarkdownEventCursor Cursor;

    /// <summary>
    /// Initializes a reader over an empty document; façade creation methods configure source or AST input.
    /// </summary>
    public MarkdownPullEventReader()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        Cursor = new MarkdownEventCursor(document.Root, document, pipeline, "");
    }

    internal void ConfigureSource(string source, MarkdownPipeline pipeline)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownDocument document = MarkdownParser.Parse(source, pipeline);
        Cursor = new MarkdownEventCursor(document.Root, document, pipeline, source);
    }

    internal void ConfigureDocument(MarkdownDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        Cursor = new MarkdownEventCursor(document.Root, document, pipeline, "");
    }

    internal void ConfigureNode(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        Cursor = new MarkdownEventCursor(
            node,
            null,
            MarkdownPipeline.CommonMarkPreset(),
            "");
    }

    /// <summary>Advances to the next event.</summary>
    /// <returns><c>true</c> when an event is available; otherwise, <c>false</c>.</returns>
    public bool Read() { return Cursor.Read(); }
    /// <summary>Returns the mutable current event snapshot without allocating another copy.</summary>
    /// <returns>The current event.</returns>
    public MarkdownEvent GetCurrent() { return Cursor.GetCurrent(); }
    /// <summary>Moves the reader before its first event.</summary>
    public void Reset() { Cursor.Reset(); }
    /// <summary>Returns the parsed or supplied document when available.</summary>
    /// <returns>The document, or <c>null</c> for standalone nodes.</returns>
    public MarkdownDocument? GetDocument() { return Cursor.Document; }
    /// <summary>Returns the source supplied to a source reader.</summary>
    /// <returns>The source, or an empty string for AST readers.</returns>
    public string GetSource() { return Cursor.Source; }
    /// <summary>Returns detached parse diagnostics.</summary>
    /// <returns>The diagnostics.</returns>
    public MarkdownDiagnostic[] GetDiagnostics() { return Cursor.GetDiagnostics(); }
    /// <summary>Reports whether parse diagnostics exist.</summary>
    /// <returns><c>true</c> when diagnostics exist; otherwise, <c>false</c>.</returns>
    public bool HasDiagnostics() { return Cursor.HasDiagnostics(); }
    /// <summary>Reports whether parse errors exist.</summary>
    /// <returns><c>true</c> when errors exist; otherwise, <c>false</c>.</returns>
    public bool HasErrors() { return Cursor.HasErrors(); }
}
