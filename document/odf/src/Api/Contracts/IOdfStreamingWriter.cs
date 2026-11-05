using System.IO;

namespace odf;

/// <summary>
/// Writes OpenDocument XML or package content incrementally.
/// </summary>
public interface IOdfStreamingWriter
{
    /// <summary>
    /// Opens an XML event destination.
    /// </summary>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="options">The save and stream-ownership options.</param>
    public void OpenXmlStream(Stream output, OdfSaveOptions options);
    /// <summary>
    /// Opens an OpenDocument package destination.
    /// </summary>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="rootMediaType">The OpenDocument package media type.</param>
    /// <param name="manifestVersion">The manifest version.</param>
    /// <param name="options">The save and stream-ownership options.</param>
    public void OpenPackageStream(
        Stream output,
        string rootMediaType,
        string manifestVersion,
        OdfSaveOptions options);
    /// <summary>
    /// Writes one XML event.
    /// </summary>
    /// <param name="streamEvent">The element, text, comment, processing-instruction, or end event.</param>
    public void WriteXmlEvent(OdfStreamEvent streamEvent);
    /// <summary>
    /// Begins one package entry and returns its writable content stream.
    /// </summary>
    /// <param name="path">The canonical package-relative path.</param>
    /// <param name="mediaType">The entry media type, or an empty string for a directory.</param>
    /// <param name="compressionMethod">The stored or deflated compression method.</param>
    /// <returns>The writable entry stream.</returns>
    public Stream BeginPackageEntry(
        string path,
        string mediaType,
        OdfCompressionMethod compressionMethod);
    /// <summary>
    /// Writes a byte range into the current package entry.
    /// </summary>
    /// <param name="content">The source bytes.</param>
    /// <param name="offset">The zero-based source offset.</param>
    /// <param name="count">The number of bytes to write.</param>
    public void WritePackageContent(
        byte[] content,
        int offset,
        int count);
    /// <summary>
    /// Completes the current package entry.
    /// </summary>
    public void EndPackageEntry();
    /// <summary>
    /// Completes the active XML or package stream.
    /// </summary>
    public void CompleteStream();
}
