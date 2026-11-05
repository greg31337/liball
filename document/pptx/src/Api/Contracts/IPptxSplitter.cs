using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxSplitter"/>.
/// </summary>
public interface IPptxSplitter
{
    /// <summary>
    /// Creates independent documents from the selected slides in this splitter.
    /// </summary>
    /// <param name="source">The source value to use.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    List<PptxDocument> Split(PptxPresentation source);
    /// <summary>
    /// Creates independent documents from the selected slides in this splitter.
    /// </summary>
    /// <param name="sourceDocument">The source document value to use.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sourceDocument</c> is <c>null</c>.</exception>
    List<PptxDocument> SplitDocument(PptxDocument sourceDocument);
    /// <summary>
    /// Creates one independent document per slide supplied by a presentation source.
    /// </summary>
    /// <param name="source">The source that supplies the presentation content.</param>
    /// <returns>The resulting documents in presentation order.</returns>
    List<PptxDocument> SplitSource(IPptxPresentationSource source);
    /// <summary>
    /// Creates independent documents from the selected slides in this splitter.
    /// </summary>
    /// <param name="source">The source value to use.</param>
    /// <param name="startIndex">The start index value to use.</param>
    /// <param name="count">The count value to use.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument SplitRange(PptxPresentation source, int startIndex, int count);
    /// <summary>
    /// Creates independent documents from the selected slides in this splitter.
    /// </summary>
    /// <param name="sourceDocument">The source document value to use.</param>
    /// <param name="startIndex">The start index value to use.</param>
    /// <param name="count">The count value to use.</param>
    /// <returns>The resulting document value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sourceDocument</c> is <c>null</c>.</exception>
    PptxDocument SplitDocumentRange(PptxDocument sourceDocument, int startIndex, int count);
    /// <summary>
    /// Creates an independent document from a selected range supplied by a presentation source.
    /// </summary>
    /// <param name="source">The source that supplies the presentation content.</param>
    /// <param name="startIndex">The zero-based index of the first slide.</param>
    /// <param name="count">The number of slides to include.</param>
    /// <returns>The resulting document.</returns>
    PptxDocument SplitSourceRange(IPptxPresentationSource source, int startIndex, int count);
}
