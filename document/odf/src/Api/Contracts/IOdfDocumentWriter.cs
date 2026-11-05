using System.IO;

namespace odf;

/// <summary>
/// Writes package and flat OpenDocument documents.
/// </summary>
public interface IOdfDocumentWriter
{
    /// <summary>
    /// Writes a document to a file-system path.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="path">The destination path.</param>
    /// <param name="options">The save options.</param>
    public void WriteDocumentPath(
        OdfDocument document,
        string path,
        OdfSaveOptions options);
    /// <summary>
    /// Writes a document to a caller-supplied stream.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="options">The save and stream-ownership options.</param>
    public void WriteDocumentStream(
        OdfDocument document,
        Stream output,
        OdfSaveOptions options);
    /// <summary>
    /// Writes a document to a new byte array.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="options">The save options.</param>
    /// <returns>The complete package or flat XML bytes.</returns>
    public byte[] WriteDocumentBytes(
        OdfDocument document,
        OdfSaveOptions options);
}
