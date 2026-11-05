using System;

namespace markdown;

/// <summary>
/// Represents a parsed Markdown document, including its syntax tree and diagnostics.
/// </summary>
public class MarkdownDocument
{
    /// <summary>
    /// The normalized source text used to parse this document, or an empty string for caller-built documents.
    /// </summary>
    public string Source;
    /// <summary>
    /// The document-kind root node that owns every top-level block.
    /// </summary>
    public MarkdownNode Root;
    /// <summary>
    /// Diagnostics collected while parsing and transforming the document.
    /// </summary>
    public MarkdownDiagnosticCollection Diagnostics;
    /// <summary>
    /// Mappings from expanded source regions to the root or included sources that supplied them.
    /// </summary>
    public MarkdownSourceAttribution[] SourceAttributions;

    /// <summary>
    /// Initializes a new <see cref="MarkdownDocument"/> instance.
    /// </summary>
    public MarkdownDocument()
    {
        Source = "";
        Root = MarkdownNode.Create(MarkdownNodeKind.Document);
        Diagnostics = new MarkdownDiagnosticCollection();
        SourceAttributions = new MarkdownSourceAttribution[0];
    }

    /// <summary>
    /// Creates a document with the specified source span.
    /// </summary>
    /// <param name="span">The source span associated with the value.</param>
    internal static MarkdownDocument Create(MarkdownSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        MarkdownDocument document = new MarkdownDocument();
        document.Root = MarkdownNode.Create(MarkdownNodeKind.Document, span);
        return document;
    }

    /// <summary>
    /// Creates a document around an existing document root.
    /// </summary>
    /// <param name="root">The root node whose descendants are read.</param>
    internal static MarkdownDocument Create(MarkdownNode root)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (!MarkdownNodeKindFacts.IsDocumentKind(root.Kind))
        {
            throw new ArgumentException("Document root must use the Document node kind.");
        }

        MarkdownDocument document = new MarkdownDocument();
        document.Root = root;
        return document;
    }

    /// <summary>
    /// Returns root.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownNode"/> value.</returns>
    internal MarkdownNode GetRoot()
    {
        return Root;
    }

    /// <summary>
    /// Creates a deep copy of document.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownDocument"/> value.</returns>
    internal MarkdownDocument CloneDocument()
    {
        MarkdownDocument clone = Create(Root.CloneNode());
        clone.Source = Source;
        MarkdownDiagnostic[] diagnostics = Diagnostics.GetDiagnostics();
        int index = 0;
        while (index < diagnostics.Length)
        {
            clone.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }

        clone.SourceAttributions = new MarkdownSourceAttribution[SourceAttributions.Length];
        index = 0;
        while (index < SourceAttributions.Length)
        {
            clone.SourceAttributions[index] = SourceAttributions[index].Copy();
            index = index + 1;
        }

        return clone;
    }

    /// <summary>
    /// Creates source map.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownSourceMap"/> value.</returns>
    internal MarkdownSourceMap CreateSourceMap()
    {
        return new MarkdownSourceMap(this);
    }

    /// <summary>
    /// Adds diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic value.</param>
    internal void AddDiagnostic(MarkdownDiagnostic diagnostic)
    {
        Diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Adds diagnostic.
    /// </summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The developer-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    internal void AddDiagnosticValues(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span)
    {
        Diagnostics.AddDiagnostic(severity, code, message, span);
    }

    /// <summary>
    /// Returns diagnostics.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownDiagnostic[] GetDiagnostics()
    {
        return Diagnostics.GetDiagnostics();
    }

    /// <summary>
    /// Reports whether at least one diagnostic has been recorded.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasDiagnostics()
    {
        return Diagnostics.HasDiagnostics();
    }

    /// <summary>
    /// Reports whether at least one error diagnostic has been recorded.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasErrors()
    {
        return Diagnostics.HasErrors();
    }
}
