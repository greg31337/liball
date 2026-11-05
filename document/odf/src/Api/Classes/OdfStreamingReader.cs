using System.IO;

namespace odf;

/// <summary>
/// Provides forward-only package-entry and XML event reading.
/// </summary>
public class OdfStreamingReader : IOdfStreamingReader
{
    internal PackageEventReader? PackageReader;
    internal XmlEventReader? XmlReader;
    internal bool Started;

    /// <summary>
    /// Initializes a streaming reader.
    /// </summary>
    public OdfStreamingReader()
    {
    }

    /// <summary>
    /// Opens a package as a forward-only entry event stream.
    /// </summary>
    /// <param name="input">The readable package stream.</param>
    /// <param name="options">The load, selection, security, and ownership options.</param>
    public void OpenPackageStream(Stream input, OdfLoadOptions options)
    {
        RequireNotStarted();
        PackageReader = new PackageEventReader(input, options);
        Started = true;
    }

    /// <summary>
    /// Opens XML as a forward-only XML event stream.
    /// </summary>
    /// <param name="input">The readable XML stream.</param>
    /// <param name="partPath">The package-relative part path, or an empty string for flat XML.</param>
    /// <param name="options">The load, selection, security, and ownership options.</param>
    public void OpenXmlStream(
        Stream input,
        string partPath,
        OdfLoadOptions options)
    {
        RequireNotStarted();
        XmlReader = new XmlEventReader(input, partPath, options);
        Started = true;
    }

    /// <summary>
    /// Reads the next selected event.
    /// </summary>
    /// <returns>The next streaming event.</returns>
    public OdfStreamEvent ReadNextEvent()
    {
        if (PackageReader != null)
        {
            return PackageReader.ReadNextEvent();
        }
        if (XmlReader != null)
        {
            return XmlReader.ReadNextEvent();
        }
        throw new OdfDocumentException(
            "A package or XML stream must be opened before reading.");
    }

    /// <summary>
    /// Reads the expanded bytes of the current package entry.
    /// </summary>
    /// <returns>An independent snapshot of the current entry bytes.</returns>
    public byte[] ReadCurrentPackageEntryBytes()
    {
        if (PackageReader == null)
        {
            throw new OdfPackageException(
                "A package stream must be opened before reading entry bytes.");
        }
        return PackageReader.ReadCurrentEntryBytes();
    }

    /// <summary>
    /// Skips the unread content of the current package entry.
    /// </summary>
    public void SkipCurrentPackageEntry()
    {
        if (PackageReader == null)
        {
            throw new OdfPackageException(
                "A package stream must be opened before skipping an entry.");
        }
        PackageReader.SkipCurrentEntry();
    }

    /// <summary>
    /// Skips the descendants of the current XML element.
    /// </summary>
    public void SkipCurrentXmlSubtree()
    {
        if (XmlReader == null)
        {
            throw new OdfXmlException(
                "An XML stream must be opened before skipping a subtree.");
        }
        XmlReader.SkipCurrentSubtree();
    }

    internal void RequireNotStarted()
    {
        if (Started)
        {
            throw new OdfDocumentException(
                "A streaming reader can open only one input stream.");
        }
    }
}
