using System.IO;

namespace odf;

/// <summary>
/// Writes materialized standard OpenDocument XML parts.
/// </summary>
public interface IOdfXmlWriter
{
    /// <summary>
    /// Writes an XML part to a file-system path.
    /// </summary>
    /// <param name="part">The XML part to write.</param>
    /// <param name="path">The destination path.</param>
    /// <param name="options">The save options.</param>
    public void WriteXmlPath(
        OdfPart part,
        string path,
        OdfSaveOptions options);
    /// <summary>
    /// Writes an XML part to a caller-supplied stream.
    /// </summary>
    /// <param name="part">The XML part to write.</param>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="options">The save and stream-ownership options.</param>
    public void WriteXmlStream(
        OdfPart part,
        Stream output,
        OdfSaveOptions options);
    /// <summary>
    /// Writes an XML part to a new byte array.
    /// </summary>
    /// <param name="part">The XML part to write.</param>
    /// <param name="options">The save options.</param>
    /// <returns>The complete XML bytes.</returns>
    public byte[] WriteXmlBytes(OdfPart part, OdfSaveOptions options);
}
