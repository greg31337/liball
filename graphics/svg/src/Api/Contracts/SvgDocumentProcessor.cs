namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgDocumentProcessor"/>.</summary>
public interface ISvgDocumentProcessor
{
    /// <summary>Processes a document with an empty default job list.</summary>
    /// <param name="document">The editable document.</param>
    /// <returns>The completed processing result.</returns>
    SvgProcessResult ProcessWithDocument(SvgDocument document);
    /// <summary>Processes a document using ordered jobs and bounded convergence.</summary>
    /// <param name="document">The editable document.</param>
    /// <param name="options">The jobs, bounds, callbacks, and dry-run behavior.</param>
    /// <returns>The completed or atomically rolled-back processing result.</returns>
    SvgProcessResult ProcessWithDocumentAndOptions(SvgDocument document, SvgProcessOptions options);
}
