using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxSplitter values.
/// </summary>
public class PptxSplitter : IPptxSplitter
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxSplitter()
    {
    }

    /// <summary>
    /// Creates independent documents from the selected slides in this splitter.
    /// </summary>
    /// <param name="source">The source value to use.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    public List<PptxDocument> Split(PptxPresentation source)
    {
        return PptxSplitterCore.Split(source);
    }

    /// <summary>
    /// Creates independent documents from the selected slides in this splitter.
    /// </summary>
    /// <param name="sourceDocument">The source document value to use.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sourceDocument</c> is <c>null</c>.</exception>
    public List<PptxDocument> SplitDocument(PptxDocument sourceDocument)
    {
        if (sourceDocument == null)
        {
            throw new System.ArgumentNullException("sourceDocument");
        }

        return PptxSplitterCore.Split(sourceDocument);
    }

    /// <summary>
    /// Creates one independent document per slide supplied by a presentation source.
    /// </summary>
    /// <param name="source">The source that supplies the presentation content.</param>
    /// <returns>The resulting documents in presentation order.</returns>
    public List<PptxDocument> SplitSource(IPptxPresentationSource source)
    {
        return PptxSplitterCore.Split(source);
    }

    /// <summary>
    /// Creates independent documents from the selected slides in this splitter.
    /// </summary>
    /// <param name="source">The source value to use.</param>
    /// <param name="startIndex">The start index value to use.</param>
    /// <param name="count">The count value to use.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument SplitRange(PptxPresentation source, int startIndex, int count)
    {
        return PptxSplitterCore.SplitRange(source, startIndex, count);
    }

    /// <summary>
    /// Creates independent documents from the selected slides in this splitter.
    /// </summary>
    /// <param name="sourceDocument">The source document value to use.</param>
    /// <param name="startIndex">The start index value to use.</param>
    /// <param name="count">The count value to use.</param>
    /// <returns>The resulting document value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sourceDocument</c> is <c>null</c>.</exception>
    public PptxDocument SplitDocumentRange(PptxDocument sourceDocument, int startIndex, int count)
    {
        if (sourceDocument == null)
        {
            throw new System.ArgumentNullException("sourceDocument");
        }

        return PptxSplitterCore.SplitRange(sourceDocument, startIndex, count);
    }

    /// <summary>
    /// Creates an independent document from a selected range supplied by a presentation source.
    /// </summary>
    /// <param name="source">The source that supplies the presentation content.</param>
    /// <param name="startIndex">The zero-based index of the first slide.</param>
    /// <param name="count">The number of slides to include.</param>
    /// <returns>The resulting document.</returns>
    public PptxDocument SplitSourceRange(IPptxPresentationSource source, int startIndex, int count)
    {
        return PptxSplitterCore.SplitRange(source, startIndex, count);
    }
}
