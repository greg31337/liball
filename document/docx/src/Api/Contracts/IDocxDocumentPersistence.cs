using System.IO;

namespace docx;

/// <summary>
/// Provides creation, loading, and saving entry points for DOCX documents.
/// </summary>
public interface IDocxDocumentPersistence
{
    /// <summary>
    /// Creates a document backed by a minimal WordprocessingML package.
    /// </summary>
    /// <returns>The created document.</returns>
    DocxDocument CreateDocument();

    /// <summary>
    /// Loads a DOCX document from a stream with the supplied options.
    /// </summary>
    /// <param name="stream">The package stream. The caller retains ownership of the stream.</param>
    /// <param name="options">The package loading options.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    DocxDocument OpenDocument(Stream stream, DocxLoadOptions options);

    /// <summary>
    /// Loads a DOCX document from a file with the supplied options.
    /// </summary>
    /// <param name="path">The path of the package file.</param>
    /// <param name="options">The package loading options.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    DocxDocument OpenDocumentFile(string path, DocxLoadOptions options);

    /// <summary>
    /// Saves a DOCX document to a stream with the supplied options.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="stream">The destination stream. The caller retains ownership of the stream.</param>
    /// <param name="options">The package saving options.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SaveDocument(DocxDocument document, Stream stream, DocxSaveOptions options);

    /// <summary>
    /// Saves a DOCX document to a file with the supplied options.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The package saving options.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SaveDocumentFile(DocxDocument document, string path, DocxSaveOptions options);
}
