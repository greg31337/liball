using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfMetadata"/>.
/// </summary>
public interface IPdfMetadata
{
    /// <summary>
    /// Sets the document title.
    /// </summary>
    /// <param name="title">The title stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when title is null.</exception>
    void SetTitle(string title);

    /// <summary>
    /// Sets the document author.
    /// </summary>
    /// <param name="author">The author stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when author is null.</exception>
    void SetAuthor(string author);

    /// <summary>
    /// Sets the document subject.
    /// </summary>
    /// <param name="subject">The subject stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when subject is null.</exception>
    void SetSubject(string subject);

    /// <summary>
    /// Sets the document keywords.
    /// </summary>
    /// <param name="keywords">The keywords stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when keywords is null.</exception>
    void SetKeywords(string keywords);

    /// <summary>
    /// Sets the application or component that created the source document.
    /// </summary>
    /// <param name="creator">The creator stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when creator is null.</exception>
    void SetCreator(string creator);

    /// <summary>
    /// Sets the application or component that produced the PDF.
    /// </summary>
    /// <param name="producer">The producer stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when producer is null.</exception>
    void SetProducer(string producer);

    /// <summary>
    /// Sets the document creation date.
    /// </summary>
    /// <param name="creationDate">The creation date stored in the document metadata.</param>
    void SetCreationDate(DateTime creationDate);

    /// <summary>
    /// Sets the document modification date.
    /// </summary>
    /// <param name="modificationDate">The modification date stored in the document metadata.</param>
    void SetModificationDate(DateTime modificationDate);

    /// <summary>
    /// Sets the complete XMP packet written to the document metadata stream as UTF-8.
    /// </summary>
    /// <param name="xmpMetadata">The XMP XML packet, or an empty string to omit the metadata stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when XMP metadata is null.</exception>
    void SetXmpMetadata(string xmpMetadata);

    /// <summary>
    /// Determines whether the current object has values.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    bool HasValues();
}
