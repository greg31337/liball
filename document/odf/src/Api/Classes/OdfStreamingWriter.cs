using System.IO;

namespace odf;

/// <summary>
/// Provides incremental XML and OpenDocument package writing.
/// </summary>
public class OdfStreamingWriter : IOdfStreamingWriter
{
    internal PackageEventWriter? PackageWriter;
    internal XmlEventWriter? XmlWriter;
    internal bool Started;

    /// <summary>
    /// Initializes a streaming writer.
    /// </summary>
    public OdfStreamingWriter()
    {
    }

    /// <summary>
    /// Opens an XML event destination.
    /// </summary>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="options">The save and stream-ownership options.</param>
    public void OpenXmlStream(Stream output, OdfSaveOptions options)
    {
        RequireNotStarted();
        XmlWriter = new XmlEventWriter(output, options);
        Started = true;
    }

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
        OdfSaveOptions options)
    {
        RequireNotStarted();
        PackageWriter = new PackageEventWriter(
            output,
            rootMediaType,
            manifestVersion,
            options);
        Started = true;
    }

    /// <summary>
    /// Writes one XML event.
    /// </summary>
    /// <param name="streamEvent">The element, text, comment, processing-instruction, or end event.</param>
    public void WriteXmlEvent(OdfStreamEvent streamEvent)
    {
        if (XmlWriter == null)
        {
            throw new OdfXmlException(
                "An XML stream must be opened before writing events.");
        }
        XmlWriter.WriteEvent(streamEvent);
    }

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
        OdfCompressionMethod compressionMethod)
    {
        if (PackageWriter == null)
        {
            throw new OdfPackageException(
                "A package stream must be opened before starting an entry.");
        }
        return PackageWriter.BeginPackageEntry(
            path,
            mediaType,
            compressionMethod);
    }

    /// <summary>
    /// Writes a byte range into the current package entry.
    /// </summary>
    /// <param name="content">The source bytes.</param>
    /// <param name="offset">The zero-based source offset.</param>
    /// <param name="count">The number of bytes to write.</param>
    public void WritePackageContent(
        byte[] content,
        int offset,
        int count)
    {
        if (PackageWriter == null)
        {
            throw new OdfPackageException(
                "A package stream must be opened before writing entry content.");
        }
        PackageWriter.WritePackageContent(content, offset, count);
    }

    /// <summary>
    /// Completes the current package entry.
    /// </summary>
    public void EndPackageEntry()
    {
        if (PackageWriter == null)
        {
            throw new OdfPackageException(
                "A package stream must be opened before ending an entry.");
        }
        PackageWriter.EndPackageEntry();
    }

    /// <summary>
    /// Completes the active XML or package stream.
    /// </summary>
    public void CompleteStream()
    {
        if (XmlWriter != null)
        {
            XmlWriter.CompleteXml();
            return;
        }
        if (PackageWriter != null)
        {
            PackageWriter.CompletePackage();
            return;
        }
        throw new OdfDocumentException(
            "A package or XML stream must be opened before completion.");
    }

    internal void RequireNotStarted()
    {
        if (Started)
        {
            throw new OdfDocumentException(
                "A streaming writer can open only one output stream.");
        }
    }
}
