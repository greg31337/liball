using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads part relationships data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxPartRelationshipsReader : IXlsxPartRelationshipsReader
{
    /// <summary>
    /// Initializes a new part relationships reader with its default state.
    /// </summary>
    public XlsxPartRelationshipsReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "sourcePartPath">The source part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadString(string sourcePartPath, string xml)
    {
        return ReadStringWithOptions(sourcePartPath, xml, new XlsxReaderOptions());
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "sourcePartPath">The source part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadStringWithOptions(string sourcePartPath, string xml, XlsxReaderOptions options)
    {
        string sourcePath = XlsxPackagePath.GetRelationshipSourcePartPath(sourcePartPath);
        string relationshipPartPath = XlsxPackagePath.GetRelationshipPartPath(sourcePartPath);
        if (xml == null || xml.Length == 0)
        {
            return XlsxPackageRelationshipsReader.CreateMissingRootResult(sourcePath, relationshipPartPath);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        return ReadBytes(sourcePartPath, bytes, options);
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "sourcePartPath">The source part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadBytes(string sourcePartPath, byte[] bytes, XlsxReaderOptions options)
    {
        string sourcePath = XlsxPackagePath.GetRelationshipSourcePartPath(sourcePartPath);
        string relationshipPartPath = XlsxPackagePath.GetRelationshipPartPath(sourcePartPath);
        if (bytes == null || bytes.Length == 0)
        {
            return XlsxPackageRelationshipsReader.CreateMissingRootResult(sourcePath, relationshipPartPath);
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(sourcePartPath, stream, options);
        }
    }

    /// <summary>
    /// Parses part relationships from read-only memory.
    /// </summary>
    /// <param name="sourcePartPath">The source part or relationships part path.</param>
    /// <param name="bytes">The OOXML bytes to read.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadMemory(string sourcePartPath, ReadOnlyMemory<byte> bytes, XlsxReaderOptions options)
    {
        return ReadBytes(sourcePartPath, bytes.ToArray(), options);
    }

    /// <summary>
    /// Parses part relationships from a read-only byte span.
    /// </summary>
    /// <param name="sourcePartPath">The source part or relationships part path.</param>
    /// <param name="bytes">The OOXML bytes to read.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadSpan(string sourcePartPath, ReadOnlySpan<byte> bytes, XlsxReaderOptions options)
    {
        return ReadBytes(sourcePartPath, bytes.ToArray(), options);
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "sourcePartPath">The source part path value used by the operation.</param>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadStream(string sourcePartPath, Stream stream, XlsxReaderOptions options)
    {
        string sourcePath = XlsxPackagePath.GetRelationshipSourcePartPath(sourcePartPath);
        string relationshipPartPath = XlsxPackagePath.GetRelationshipPartPath(sourcePartPath);
        return XlsxPackageRelationshipsReader.ReadStreamForPart(sourcePath, relationshipPartPath, stream, options);
    }
}
