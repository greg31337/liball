using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads custom xml part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxCustomXmlPartReader : IXlsxCustomXmlPartReader
{
    /// <summary>
    /// Initializes a new custom xml part reader with its default state.
    /// </summary>
    public XlsxCustomXmlPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The custom xml part produced by the operation.</returns>
    public XlsxCustomXmlPart ReadString(string partPath, string contentType, string xml)
    {
        if (xml == null)
        {
            return ReadBytes(partPath, contentType, null);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxCustomXmlPart part = ReadBytes(partPath, contentType, bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The custom xml part produced by the operation.</returns>
    public XlsxCustomXmlPart ReadBytes(string partPath, string? contentType, byte[]? bytes)
    {
        XlsxCustomXmlPart part = new XlsxCustomXmlPart();
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
        part.IsPropertiesPart = part.ContentType == XlsxContentTypes.CustomXmlProperties || part.PartPath.EndsWith("Props.xml", System.StringComparison.OrdinalIgnoreCase);
        if (bytes == null)
        {
            part.Data = new byte[0];
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customXml.dataNull", "Custom XML part data must not be null."));
            part.IsValid = part.Validation.IsValid;
            return part;
        }

        part.Data = bytes;
        part.RawXml = Encoding.UTF8.GetString(bytes);
        ReadRootMetadata(part);
        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static void ReadRootMetadata(XlsxCustomXmlPart part)
    {
        if (part.RawXml == null || part.RawXml.Length == 0)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customXml.xmlEmpty", "Custom XML part must contain XML text."));
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

        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "customXml.missingRoot", "Custom XML part must contain a root element."));
    }
}
