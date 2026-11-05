using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxMerger"/>.
/// </summary>
public interface IPptxMerger
{
    /// <summary>
    /// Combines the supplied presentation content with this merger.
    /// </summary>
    /// <param name="destination">The destination value to use.</param>
    /// <param name="source">The source value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    int MergePresentations(PptxPresentation destination, PptxPresentation source);
    /// <summary>
    /// Combines the supplied presentation content with this merger.
    /// </summary>
    /// <param name="destination">The destination value to use.</param>
    /// <param name="sourceDocument">The source document value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sourceDocument</c> is <c>null</c>.</exception>
    int MergeDocumentIntoPresentation(PptxPresentation destination, PptxDocument sourceDocument);
    /// <summary>
    /// Combines presentation content supplied through a presentation source with the destination.
    /// </summary>
    /// <param name="destination">The destination value to use.</param>
    /// <param name="source">The source that supplies the presentation content.</param>
    /// <returns>The number of imported slides.</returns>
    int MergeSourceIntoPresentation(PptxPresentation destination, IPptxPresentationSource source);
    /// <summary>
    /// Combines the supplied presentation content with this merger.
    /// </summary>
    /// <param name="first">The first value to use.</param>
    /// <param name="second">The second value to use.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument MergeDocuments(PptxDocument first, PptxDocument second);
    /// <summary>
    /// Returns a merged clone of the first document using content supplied by the second source.
    /// </summary>
    /// <param name="first">The document to clone as the merge destination.</param>
    /// <param name="second">The source that supplies the presentation content to append.</param>
    /// <returns>The merged document.</returns>
    PptxDocument MergeDocumentWithSource(PptxDocument first, IPptxPresentationSource second);
}
