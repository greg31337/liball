using System.IO;

namespace odf;

/// <summary>
/// Reads standard OpenDocument XML parts into universal XML trees.
/// </summary>
public interface IOdfXmlReader
{
    /// <summary>
    /// Reads an XML part from a file-system path.
    /// </summary>
    /// <param name="path">The path to read.</param>
    /// <param name="partPath">The package-relative part path, or an empty string for flat XML.</param>
    /// <param name="options">The load options.</param>
    /// <returns>The materialized standard XML part.</returns>
    public OdfPart ReadXmlPath(
        string path,
        string partPath,
        OdfLoadOptions options);
    /// <summary>
    /// Reads an XML part from a caller-supplied stream.
    /// </summary>
    /// <param name="input">The readable XML stream.</param>
    /// <param name="partPath">The package-relative part path, or an empty string for flat XML.</param>
    /// <param name="options">The load and stream-ownership options.</param>
    /// <returns>The materialized standard XML part.</returns>
    public OdfPart ReadXmlStream(
        Stream input,
        string partPath,
        OdfLoadOptions options);
    /// <summary>
    /// Reads an XML part from complete input bytes.
    /// </summary>
    /// <param name="data">The complete XML bytes.</param>
    /// <param name="partPath">The package-relative part path, or an empty string for flat XML.</param>
    /// <param name="options">The load options.</param>
    /// <returns>The materialized standard XML part.</returns>
    public OdfPart ReadXmlBytes(
        byte[] data,
        string partPath,
        OdfLoadOptions options);
}
