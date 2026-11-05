using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxMerger values.
/// </summary>
public class PptxMerger : IPptxMerger
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxMerger()
    {
    }

    /// <summary>
    /// Combines the supplied presentation content with this merger.
    /// </summary>
    /// <param name="destination">The destination value to use.</param>
    /// <param name="source">The source value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    public int MergePresentations(PptxPresentation destination, PptxPresentation source)
    {
        return PptxMergerCore.Merge(destination, source);
    }

    /// <summary>
    /// Combines the supplied presentation content with this merger.
    /// </summary>
    /// <param name="destination">The destination value to use.</param>
    /// <param name="sourceDocument">The source document value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sourceDocument</c> is <c>null</c>.</exception>
    public int MergeDocumentIntoPresentation(PptxPresentation destination, PptxDocument sourceDocument)
    {
        if (sourceDocument == null)
        {
            throw new System.ArgumentNullException("sourceDocument");
        }

        return PptxMergerCore.Merge(destination, sourceDocument);
    }

    /// <summary>
    /// Combines presentation content supplied through a presentation source with the destination.
    /// </summary>
    /// <param name="destination">The destination value to use.</param>
    /// <param name="source">The source that supplies the presentation content.</param>
    /// <returns>The number of imported slides.</returns>
    public int MergeSourceIntoPresentation(PptxPresentation destination, IPptxPresentationSource source)
    {
        return PptxMergerCore.Merge(destination, source);
    }

    /// <summary>
    /// Combines the supplied presentation content with this merger.
    /// </summary>
    /// <param name="first">The first value to use.</param>
    /// <param name="second">The second value to use.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument MergeDocuments(PptxDocument first, PptxDocument second)
    {
        return PptxMergerCore.Merge(first, second);
    }

    /// <summary>
    /// Returns a merged clone of the first document using content supplied by the second source.
    /// </summary>
    /// <param name="first">The document to clone as the merge destination.</param>
    /// <param name="second">The source that supplies the presentation content to append.</param>
    /// <returns>The merged document.</returns>
    public PptxDocument MergeDocumentWithSource(PptxDocument first, IPptxPresentationSource second)
    {
        return PptxMergerCore.Merge(first, second);
    }
}
