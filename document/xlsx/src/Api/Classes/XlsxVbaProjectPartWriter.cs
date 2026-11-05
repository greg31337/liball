using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes vba project part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxVbaProjectPartWriter : IXlsxVbaProjectPartWriter
{
    /// <summary>
    /// Initializes a new vba project part writer with its default state.
    /// </summary>
    public XlsxVbaProjectPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxVbaProjectPartWriteResult WriteBytes(XlsxVbaProjectPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxVbaProjectPartWriteResult result = WriteStream(stream, part);
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
    public XlsxVbaProjectPartWriteResult WriteStream(Stream stream, XlsxVbaProjectPart part)
    {
        XlsxVbaProjectPartWriteResult result = ValidateVbaProjectPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        stream.Write(part.Data, 0, part.Data.Length);
        return result;
    }

    internal static XlsxVbaProjectPartWriteResult ValidateVbaProjectPart(XlsxVbaProjectPart part)
    {
        XlsxVbaProjectPartWriteResult result = new XlsxVbaProjectPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "vbaProjectWriter.partNull", "VBA project part must not be null."));
            return result;
        }

        if (part.PartPath == null || part.PartPath.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "vbaProjectWriter.partPathMissing", "VBA project part must have a package path."));
        }

        if (part.Data == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "vbaProjectWriter.dataNull", "VBA project data must not be null."));
        }

        return result;
    }
}
