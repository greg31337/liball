using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads in cell picture metadata part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxInCellPictureMetadataPartReader : IXlsxInCellPictureMetadataPartReader
{
    /// <summary>
    /// Initializes a new in cell picture metadata part reader with its default state.
    /// </summary>
    public XlsxInCellPictureMetadataPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The in cell picture metadata part produced by the operation.</returns>
    public XlsxInCellPictureMetadataPart ReadString(string partPath, string contentType, string xml)
    {
        if (xml == null)
        {
            return ReadBytes(partPath, contentType, null);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxInCellPictureMetadataPart part = ReadBytes(partPath, contentType, bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The in cell picture metadata part produced by the operation.</returns>
    public XlsxInCellPictureMetadataPart ReadBytes(string partPath, string? contentType, byte[]? bytes)
    {
        XlsxInCellPictureMetadataPart part = new XlsxInCellPictureMetadataPart();
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
        part.IsSheetMetadata = part.PartPath == XlsxPackagePaths.SheetMetadata;
        part.IsRichDataPart = part.PartPath.StartsWith(XlsxPackagePaths.RichDataDirectory, System.StringComparison.Ordinal);
        if (bytes == null)
        {
            part.Data = new byte[0];
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inCellPictureMetadata.dataNull", "In-cell picture metadata part data must not be null."));
            part.IsValid = part.Validation.IsValid;
            return part;
        }

        part.Data = new byte[bytes.Length];
        Array.Copy(bytes, part.Data, bytes.Length);
        part.RawXml = Encoding.UTF8.GetString(part.Data);
        ReadRootMetadata(part);
        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static void ReadRootMetadata(XlsxInCellPictureMetadataPart part)
    {
        if (part.RawXml == null || part.RawXml.Length == 0)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inCellPictureMetadata.xmlEmpty", "In-cell picture metadata part must contain XML text."));
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(part.RawXml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        part.RootElementName = reader.LocalName;
                        part.RootNamespaceUri = reader.NamespaceURI;
                        return;
                    }

                    reader.Read();
                }
            }
        }

        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "inCellPictureMetadata.missingRoot", "In-cell picture metadata part must contain a root element."));
    }
}
