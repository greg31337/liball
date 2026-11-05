using System.IO;

namespace odf;

/// <summary>
/// Reads selected OpenDocument content as a forward-only event stream.
/// </summary>
public interface IOdfStreamingReader
{
    /// <summary>
    /// Opens a package as a forward-only entry event stream.
    /// </summary>
    /// <param name="input">The readable package stream.</param>
    /// <param name="options">The load, selection, security, and ownership options.</param>
    public void OpenPackageStream(Stream input, OdfLoadOptions options);
    /// <summary>
    /// Opens XML as a forward-only XML event stream.
    /// </summary>
    /// <param name="input">The readable XML stream.</param>
    /// <param name="partPath">The package-relative part path, or an empty string for flat XML.</param>
    /// <param name="options">The load, selection, security, and ownership options.</param>
    public void OpenXmlStream(
        Stream input,
        string partPath,
        OdfLoadOptions options);
    /// <summary>
    /// Reads the next selected event.
    /// </summary>
    /// <returns>The next streaming event.</returns>
    public OdfStreamEvent ReadNextEvent();
    /// <summary>
    /// Reads the expanded bytes of the current package entry.
    /// </summary>
    /// <returns>An independent snapshot of the current entry bytes.</returns>
    public byte[] ReadCurrentPackageEntryBytes();
    /// <summary>
    /// Skips the unread content of the current package entry.
    /// </summary>
    public void SkipCurrentPackageEntry();
    /// <summary>
    /// Skips the descendants of the current XML element.
    /// </summary>
    public void SkipCurrentXmlSubtree();
}
