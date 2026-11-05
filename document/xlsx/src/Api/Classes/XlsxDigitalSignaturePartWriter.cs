using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes digital signature part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxDigitalSignaturePartWriter : IXlsxDigitalSignaturePartWriter
{
    /// <summary>
    /// Initializes a new digital signature part writer with its default state.
    /// </summary>
    public XlsxDigitalSignaturePartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxDigitalSignaturePartWriteResult WriteString(XlsxDigitalSignaturePart? part)
    {
        XlsxDigitalSignaturePartWriteResult result = WriteBytes(part);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxDigitalSignaturePartWriteResult WriteBytes(XlsxDigitalSignaturePart? part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxDigitalSignaturePartWriteResult result = WriteStream(stream, part);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                result.Xml = Encoding.UTF8.GetString(result.Bytes);
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
    public XlsxDigitalSignaturePartWriteResult WriteStream(Stream stream, XlsxDigitalSignaturePart? part)
    {
        XlsxDigitalSignaturePartWriteResult result = ValidateDigitalSignaturePart(part);
        if (!result.IsValid || part == null)
        {
            return result;
        }

        stream.Write(part.Data, 0, part.Data.Length);
        return result;
    }

    internal static XlsxDigitalSignaturePartWriteResult ValidateDigitalSignaturePart(XlsxDigitalSignaturePart? part)
    {
        XlsxDigitalSignaturePartWriteResult result = new XlsxDigitalSignaturePartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "digitalSignatureWriter.partNull", "Digital signature part must not be null."));
            return result;
        }

        if (part.PartPath == null || part.PartPath.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "digitalSignatureWriter.partPathMissing", "Digital signature part must have a package path."));
        }

        if (part.Data == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "digitalSignatureWriter.dataNull", "Digital signature part data must not be null."));
        }

        return result;
    }
}
