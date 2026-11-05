using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes in cell picture metadata part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxInCellPictureMetadataPartWriter : IXlsxInCellPictureMetadataPartWriter
{
    /// <summary>
    /// Initializes a new in cell picture metadata part writer with its default state.
    /// </summary>
    public XlsxInCellPictureMetadataPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxInCellPictureMetadataPartWriteResult WriteString(XlsxInCellPictureMetadataPart? part)
    {
        XlsxInCellPictureMetadataPartWriteResult result = WriteBytes(part);
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
    public XlsxInCellPictureMetadataPartWriteResult WriteBytes(XlsxInCellPictureMetadataPart? part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxInCellPictureMetadataPartWriteResult result = WriteStream(stream, part);
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
    public XlsxInCellPictureMetadataPartWriteResult WriteStream(Stream stream, XlsxInCellPictureMetadataPart? part)
    {
        XlsxInCellPictureMetadataPartWriteResult result = ValidateInCellPictureMetadataPart(part);
        if (!result.IsValid || part == null)
        {
            return result;
        }

        stream.Write(part.Data, 0, part.Data.Length);
        return result;
    }

    internal static XlsxInCellPictureMetadataPartWriteResult ValidateInCellPictureMetadataPart(XlsxInCellPictureMetadataPart? part)
    {
        XlsxInCellPictureMetadataPartWriteResult result = new XlsxInCellPictureMetadataPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inCellPictureMetadataWriter.partNull", "In-cell picture metadata part must not be null."));
            return result;
        }

        if (part.PartPath == null || part.PartPath.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inCellPictureMetadataWriter.partPathMissing", "In-cell picture metadata part must have a package path."));
        }

        if (part.Data == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inCellPictureMetadataWriter.dataNull", "In-cell picture metadata part data must not be null."));
        }

        return result;
    }
}
