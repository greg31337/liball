using System;

namespace markdown;

/// <summary>
/// Provides safe diagnostics, precise source-map queries, and source-preserving rendering for a document.
/// </summary>
public class MarkdownDocumentOperations : IMarkdownDocumentOperations
{
    internal MarkdownDocument Document;

    /// <summary>Initializes operations for an existing document.</summary>
    /// <param name="document">The document to inspect or update.</param>
    public MarkdownDocumentOperations(MarkdownDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        Document = document;
    }

    /// <summary>Returns the document operated on by this instance.</summary>
    /// <returns>The active document.</returns>
    public MarkdownDocument GetDocument() { return Document; }
    /// <summary>Returns the document diagnostic count.</summary>
    /// <returns>The diagnostic count.</returns>
    public int GetDiagnosticCount() { return Document.Diagnostics.GetCount(); }
    /// <summary>Reports whether document diagnostics exist.</summary>
    /// <returns>The diagnostic state.</returns>
    public bool HasDiagnostics() { return Document.Diagnostics.HasDiagnostics(); }
    /// <summary>Reports whether at least one error diagnostic exists.</summary>
    /// <returns>The error state.</returns>
    public bool HasErrors() { return Document.Diagnostics.HasErrors(); }
    /// <summary>Returns a detached diagnostic by index.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic copy.</returns>
    public MarkdownDiagnostic GetDiagnosticAt(int index) { return Document.Diagnostics.Get(index); }
    /// <summary>Returns detached copies of all document diagnostics.</summary>
    /// <returns>The diagnostic copies.</returns>
    public MarkdownDiagnostic[] GetDiagnostics() { return Document.Diagnostics.GetDiagnostics(); }
    /// <summary>Finds diagnostics whose half-open spans contain an offset.</summary>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The matching diagnostic copies.</returns>
    public MarkdownDiagnostic[] FindDiagnosticsContainingOffset(int offset)
    {
        return MarkdownDiagnosticQueries.FindContaining(Document.Diagnostics, offset);
    }
    /// <summary>Finds diagnostics whose spans intersect a query span.</summary>
    /// <param name="span">The half-open query span.</param>
    /// <returns>The matching diagnostic copies.</returns>
    public MarkdownDiagnostic[] FindDiagnosticsIntersectingSpan(MarkdownSourceSpan span)
    {
        return MarkdownDiagnosticQueries.FindIntersecting(Document.Diagnostics, span);
    }
    /// <summary>Adds a detached diagnostic copy.</summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    public void AddDiagnostic(MarkdownDiagnostic diagnostic) { Document.Diagnostics.Add(diagnostic); }
    /// <summary>Creates and adds a diagnostic.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="span">The diagnostic source span.</param>
    public void AddDiagnosticValues(
        MarkdownDiagnosticSeverity severity,
        string code,
        string message,
        MarkdownSourceSpan span)
    {
        Document.Diagnostics.AddDiagnostic(severity, code, message, span);
    }
    /// <summary>Creates and adds a source-named diagnostic.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="span">The diagnostic source span.</param>
    /// <param name="sourceName">The canonical source name.</param>
    public void AddDiagnosticValuesWithSourceName(
        MarkdownDiagnosticSeverity severity,
        string code,
        string message,
        MarkdownSourceSpan span,
        string sourceName)
    {
        Document.Diagnostics.AddDiagnosticWithSourceName(severity, code, message, span, sourceName);
    }
    /// <summary>Removes all document diagnostics.</summary>
    public void ClearDiagnostics() { Document.Diagnostics.Clear(); }
    /// <summary>Returns detached entries for every mapped non-empty node span.</summary>
    /// <returns>The source-map entries.</returns>
    public MarkdownSourceMapEntry[] GetSourceMapEntries()
    {
        return new MarkdownSourceMap(Document).GetEntries();
    }
    /// <summary>Finds source-map entries containing an offset.</summary>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The matching entries from outermost to innermost.</returns>
    public MarkdownSourceMapEntry[] FindSourceMapEntriesContainingOffset(int offset)
    {
        return new MarkdownSourceMap(Document).FindContaining(offset);
    }
    /// <summary>Finds source-map entries containing a source position.</summary>
    /// <param name="position">The source position whose offset is queried.</param>
    /// <returns>The matching entries from outermost to innermost.</returns>
    public MarkdownSourceMapEntry[] FindSourceMapEntriesContainingPosition(MarkdownSourcePosition position)
    {
        return new MarkdownSourceMap(Document).FindContainingPosition(position);
    }
    /// <summary>Finds source-map entries intersecting a half-open source span.</summary>
    /// <param name="span">The query span.</param>
    /// <returns>The intersecting entries.</returns>
    public MarkdownSourceMapEntry[] FindSourceMapEntriesIntersectingSpan(MarkdownSourceSpan span)
    {
        return new MarkdownSourceMap(Document).FindIntersecting(span);
    }
    /// <summary>Finds the deepest source-map entry containing an offset.</summary>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The deepest detached entry, or <c>null</c>.</returns>
    public MarkdownSourceMapEntry? FindDeepestSourceMapEntryAtOffset(int offset)
    {
        return new MarkdownSourceMap(Document).FindDeepestContaining(offset);
    }
    /// <summary>Finds the deepest source-map entry containing a position.</summary>
    /// <param name="position">The source position whose offset is queried.</param>
    /// <returns>The deepest detached entry, or <c>null</c>.</returns>
    public MarkdownSourceMapEntry? FindDeepestSourceMapEntryAtPosition(MarkdownSourcePosition position)
    {
        return new MarkdownSourceMap(Document).FindDeepestContainingPosition(position);
    }
    /// <summary>Finds the deepest node containing an offset.</summary>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The mapped node, or <c>null</c>.</returns>
    public MarkdownNode? FindDeepestNodeAtOffset(int offset)
    {
        return new MarkdownSourceMap(Document).FindDeepestNode(offset);
    }
    /// <summary>Finds mapped nodes of a kind containing an offset.</summary>
    /// <param name="kind">The node kind.</param>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The matching node references.</returns>
    public MarkdownNode[] FindNodesAtOffset(MarkdownNodeKind kind, int offset)
    {
        return new MarkdownSourceMap(Document).FindNodes(kind, offset);
    }
    /// <summary>Checks whether a half-open source span contains an offset.</summary>
    /// <param name="span">The source span.</param>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>Whether the offset is contained.</returns>
    public bool DoesSourceSpanContainOffset(MarkdownSourceSpan span, int offset)
    {
        MarkdownDiagnosticQueries.ValidateSpan(span);
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        return span.ContainsOffset(offset);
    }
    /// <summary>Checks whether two half-open source spans intersect.</summary>
    /// <param name="first">The first source span.</param>
    /// <param name="second">The second source span.</param>
    /// <returns>Whether the spans intersect.</returns>
    public bool DoSourceSpansIntersect(MarkdownSourceSpan first, MarkdownSourceSpan second)
    {
        return MarkdownDiagnosticQueries.SpansIntersect(first, second);
    }
    /// <summary>Returns the first original line-ending sequence, or an empty string.</summary>
    /// <returns>The original line ending.</returns>
    public string GetOriginalLineEnding()
    {
        if (Document.Root.Trivia != null && Document.Root.Trivia.LineEnding.Length > 0)
        {
            return Document.Root.Trivia.LineEnding;
        }

        return MarkdownTriviaCapture.FindLineEnding(Document.Source);
    }
    /// <summary>Renders unchanged trivia and regenerated edited regions using CommonMark settings.</summary>
    /// <returns>The source-preserving Markdown.</returns>
    public string RenderSourcePreserving()
    {
        return RenderSourcePreservingWithPipeline(MarkdownPipeline.CommonMarkPreset());
    }
    /// <summary>Renders unchanged trivia and regenerated edited regions using supplied settings.</summary>
    /// <param name="pipeline">The rendering pipeline.</param>
    /// <returns>The source-preserving Markdown.</returns>
    public string RenderSourcePreservingWithPipeline(MarkdownPipeline pipeline)
    {
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        MarkdownOptions options = pipeline.GetOptions();
        options.TrackTrivia = true;
        string lineEnding = GetOriginalLineEnding();
        if (lineEnding.Length > 0)
        {
            options.LineEnding = lineEnding;
        }

        MarkdownPipeline renderingPipeline = MarkdownPipeline.Create(
            options,
            pipeline.GetBlockRules(),
            pipeline.GetInlineRules(),
            pipeline.GetTransformRules(),
            pipeline.GetHtmlRenderRules());
        return MarkdownRenderer.Render(Document, renderingPipeline);
    }
}
