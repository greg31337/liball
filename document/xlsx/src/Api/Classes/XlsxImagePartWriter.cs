using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes image part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxImagePartWriter : IXlsxImagePartWriter
{
    /// <summary>
    /// Initializes a new image part writer with its default state.
    /// </summary>
    public XlsxImagePartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxImagePartWriteResult WriteBytes(XlsxImagePart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxImagePartWriteResult result = WriteStream(stream, part);
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
    public XlsxImagePartWriteResult WriteStream(Stream stream, XlsxImagePart part)
    {
        XlsxImagePartWriteResult result = ValidateImagePart(part);
        if (!result.IsValid)
        {
            return result;
        }

        stream.Write(part.Data, 0, part.Data.Length);
        return result;
    }

    internal static XlsxImagePartWriteResult ValidateImagePart(XlsxImagePart part)
    {
        XlsxImagePartWriteResult result = new XlsxImagePartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "imageWriter.partNull", "Image part must not be null."));
            return result;
        }

        XlsxImagePart validated = new XlsxImagePartReader().ReadBytesWithContentType(part.PartPath, part.ContentType, part.Data);
        foreach (XlsxDiagnostic diagnostic in validated.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        return result;
    }
}
