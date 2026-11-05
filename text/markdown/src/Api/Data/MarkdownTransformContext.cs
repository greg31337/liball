using System;

namespace markdown;

/// <summary>
/// Supplies a transform with the document, pipeline settings, source text, and shared diagnostics.
/// </summary>
/// <remarks>
/// Transforms share the document's diagnostic collection. The source text is available during normal parsing and is an empty string when a context is created from an already parsed document.
/// </remarks>
public class MarkdownTransformContext
{
    /// <summary>
    /// Stores the source value used by this instance.
    /// </summary>
    public string Source;
    /// <summary>
    /// Stores the document value used by this instance.
    /// </summary>
    public MarkdownDocument Document;
    /// <summary>
    /// Stores the diagnostics value used by this instance.
    /// </summary>
    public MarkdownDiagnosticCollection Diagnostics;
    /// <summary>
    /// Stores the pipeline value used by this instance.
    /// </summary>
    public MarkdownPipeline Pipeline;
    /// <summary>
    /// Stores the options value used by this instance.
    /// </summary>
    public MarkdownOptions Options;

    internal static MarkdownTransformContext Create(MarkdownParseContext parseContext)
    {
        if (parseContext == null)
        {
            throw new ArgumentNullException("parseContext");
        }

        MarkdownTransformContext context = new MarkdownTransformContext(parseContext.Document, parseContext.Pipeline);
        context.Source = parseContext.Source;
        context.Diagnostics = parseContext.Diagnostics;
        return context;
    }

    /// <summary>
    /// Initializes a new <see cref="MarkdownTransformContext"/> instance.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="pipeline">The pipeline whose parsing, transformation, and rendering settings are used.</param>
    public MarkdownTransformContext(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        Source = "";
        Document = document;
        Diagnostics = document.Diagnostics;
        Pipeline = pipeline.Copy();
        Options = pipeline.GetOptions();
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
