using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads embedded object part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxEmbeddedObjectPartReader : IXlsxEmbeddedObjectPartReader
{
    /// <summary>
    /// Initializes a new embedded object part reader with its default state.
    /// </summary>
    public XlsxEmbeddedObjectPartReader()
    {
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The embedded object part produced by the operation.</returns>
    public XlsxEmbeddedObjectPart ReadBytes(string partPath, string? contentType, byte[]? bytes)
    {
        XlsxEmbeddedObjectPart part = new XlsxEmbeddedObjectPart();
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(partPath);
        foreach (XlsxDiagnostic diagnostic in normalized.Validation.Diagnostics)
        {
            diagnostic.SetLocation(partPath, "", "");
            part.AddDiagnostic(diagnostic);
        }

        if (normalized.Validation.IsValid)
        {
            part.PartPath = normalized.Path;
        }
        else
        {
            part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        }

        part.ContentType = XlsxCellValueConverter.NormalizeText(contentType);
        part.IsOleObject = part.ContentType == XlsxContentTypes.OleObject || part.PartPath.EndsWith(".bin", System.StringComparison.OrdinalIgnoreCase);
        part.IsEmbeddedPackage = !part.IsOleObject;
        if (bytes == null)
        {
            part.Data = new byte[0];
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "embeddedObject.dataNull", "Embedded object part data must not be null."));
        }
        else
        {
            part.Data = new byte[bytes.Length];
            Array.Copy(bytes, part.Data, bytes.Length);
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }
}
