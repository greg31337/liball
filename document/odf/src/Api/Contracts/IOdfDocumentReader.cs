using System.IO;

namespace odf;

/// <summary>
/// Reads package and flat OpenDocument documents.
/// </summary>
public interface IOdfDocumentReader
{
    /// <summary>
    /// Reads a document from a file-system path.
    /// </summary>
    /// <param name="path">The path to read.</param>
    /// <param name="options">The load and stream-ownership options.</param>
    /// <returns>The loaded document.</returns>
    public OdfDocument ReadDocumentPath(string path, OdfLoadOptions options);
    /// <summary>
    /// Reads a document from a caller-supplied stream.
    /// </summary>
    /// <param name="input">The readable document stream.</param>
    /// <param name="sourceName">An optional source name used for media-type inference.</param>
    /// <param name="options">The load and stream-ownership options.</param>
    /// <returns>The loaded document.</returns>
    public OdfDocument ReadDocumentStream(
        Stream input,
        string sourceName,
        OdfLoadOptions options);
    /// <summary>
    /// Reads a document from complete input bytes.
    /// </summary>
    /// <param name="data">The complete package or flat XML bytes.</param>
    /// <param name="sourceName">An optional source name used for media-type inference.</param>
    /// <param name="options">The load options.</param>
    /// <returns>The loaded document.</returns>
    public OdfDocument ReadDocumentBytes(
        byte[] data,
        string sourceName,
        OdfLoadOptions options);
}
