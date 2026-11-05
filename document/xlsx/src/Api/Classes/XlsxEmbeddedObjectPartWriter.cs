using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes embedded object part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxEmbeddedObjectPartWriter : IXlsxEmbeddedObjectPartWriter
{
    /// <summary>
    /// Initializes a new embedded object part writer with its default state.
    /// </summary>
    public XlsxEmbeddedObjectPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxEmbeddedObjectPartWriteResult WriteBytes(XlsxEmbeddedObjectPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxEmbeddedObjectPartWriteResult result = WriteStream(stream, part);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
            }

            return result;
        }
    }

    /// <summary>
    /// Validates the input model and writes its serialized representation to the supplied stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxEmbeddedObjectPartWriteResult WriteStream(Stream stream, XlsxEmbeddedObjectPart part)
    {
        XlsxEmbeddedObjectPartWriteResult result = ValidateEmbeddedObjectPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        stream.Write(part.Data, 0, part.Data.Length);
        return result;
    }

    internal static XlsxEmbeddedObjectPartWriteResult ValidateEmbeddedObjectPart(XlsxEmbeddedObjectPart part)
    {
        XlsxEmbeddedObjectPartWriteResult result = new XlsxEmbeddedObjectPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "embeddedObjectWriter.partNull", "Embedded object part must not be null."));
            return result;
        }

        if (part.PartPath == null || part.PartPath.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "embeddedObjectWriter.partPathMissing", "Embedded object part must have a package path."));
        }

        if (part.Data == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "embeddedObjectWriter.dataNull", "Embedded object part data must not be null."));
        }

        return result;
    }
}
