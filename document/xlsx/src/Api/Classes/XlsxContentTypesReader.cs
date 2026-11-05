using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads content types data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxContentTypesReader : IXlsxContentTypesReader
{
    /// <summary>
    /// Initializes a new content types reader with its default state.
    /// </summary>
    public XlsxContentTypesReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The content types part produced by the operation.</returns>
    public XlsxContentTypesPart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        return ReadBytes(bytes);
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The content types part produced by the operation.</returns>
    public XlsxContentTypesPart ReadBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult();
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(stream);
        }
    }

    /// <summary>
    /// Parses UTF-8 OOXML from read-only memory.
    /// </summary>
    /// <param name="bytes">The OOXML bytes to read.</param>
    /// <returns>The content types part produced by the operation.</returns>
    public XlsxContentTypesPart ReadMemory(ReadOnlyMemory<byte> bytes)
    {
        return ReadBytes(bytes.ToArray());
    }

    /// <summary>
    /// Parses UTF-8 OOXML from a read-only byte span.
    /// </summary>
    /// <param name="bytes">The OOXML bytes to read.</param>
    /// <returns>The content types part produced by the operation.</returns>
    public XlsxContentTypesPart ReadSpan(ReadOnlySpan<byte> bytes)
    {
        return ReadBytes(bytes.ToArray());
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The content types part produced by the operation.</returns>
    public XlsxContentTypesPart ReadStream(Stream stream)
    {
        XlsxContentTypesPart part = new XlsxContentTypesPart();
        Dictionary<string, bool> defaultExtensions = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
        Dictionary<string, bool> overrideParts = new Dictionary<string, bool>();
        bool sawRoot = false;
        using (XmlReader reader = XmlReader.Create(stream, CreateXmlReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                if (!sawRoot)
                {
                    sawRoot = true;
                    ReadRootElement(part, reader);
                    reader.Read();
                    continue;
                }

                if (reader.Depth == 1 && reader.NamespaceURI == XlsxNamespaces.ContentTypes && reader.LocalName == "Default")
                {
                    ReadDefault(part, reader, defaultExtensions);
                    continue;
                }

                if (reader.Depth == 1 && reader.NamespaceURI == XlsxNamespaces.ContentTypes && reader.LocalName == "Override")
                {
                    ReadOverride(part, reader, overrideParts);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.missingRoot", "Content types XML must contain a Types root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XmlReaderSettings CreateXmlReaderSettings()
    {
        return XlsxXmlReaderHelpers.CreateSafeReaderSettings();
    }

    internal static void ReadRootElement(XlsxContentTypesPart part, XmlReader reader)
    {
        if (reader.LocalName != "Types" || reader.NamespaceURI != XlsxNamespaces.ContentTypes)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.invalidRoot", "Content types XML root must be a Types element in the package content types namespace.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        ReadUnknownAttributes(reader, part.UnknownRootAttributes, "xmlns");
    }

    internal static void ReadDefault(XlsxContentTypesPart part, XmlReader reader, Dictionary<string, bool> defaultExtensions)
    {
        XlsxContentTypeDefault item = new XlsxContentTypeDefault();
        item.Extension = reader.GetAttribute("Extension");
        item.ContentType = reader.GetAttribute("ContentType");
        ReadUnknownAttributes(reader, item.UnknownAttributes, "Extension", "ContentType");
        if (item.Extension == null || item.Extension.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.default.extensionMissing", "Default content type must have an Extension attribute.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Default", "Extension");
            part.AddDiagnostic(diagnostic);
        }

        if (item.ContentType == null || item.ContentType.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.default.contentTypeMissing", "Default content type must have a ContentType attribute.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Default", "ContentType");
            part.AddDiagnostic(diagnostic);
        }

        if (item.Extension != null && item.Extension.Length > 0)
        {
            if (defaultExtensions.ContainsKey(item.Extension))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.default.duplicate", "Content types XML contains a duplicate default extension.");
                diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Default", "Extension");
                part.AddDiagnostic(diagnostic);
            }
            else
            {
                defaultExtensions.Add(item.Extension, true);
                part.Defaults.Add(item);
            }
        }

        reader.ReadOuterXml();
    }

    internal static void ReadOverride(XlsxContentTypesPart part, XmlReader reader, Dictionary<string, bool> overrideParts)
    {
        XlsxContentTypeOverride item = new XlsxContentTypeOverride();
        string? partName = reader.GetAttribute("PartName");
        item.ContentType = reader.GetAttribute("ContentType");
        ReadUnknownAttributes(reader, item.UnknownAttributes, "PartName", "ContentType");
        if (partName == null || partName.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.override.partNameMissing", "Override content type must have a PartName attribute.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Override", "PartName");
            part.AddDiagnostic(diagnostic);
        }
        else
        {
            XlsxPackagePathResult pathResult = XlsxPackagePath.NormalizePartPath(partName);
            foreach (XlsxDiagnostic diagnostic in pathResult.Validation.Diagnostics)
            {
                diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Override", "PartName");
                part.AddDiagnostic(diagnostic);
            }

            item.PartName = pathResult.Path;
        }

        if (item.ContentType == null || item.ContentType.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.override.contentTypeMissing", "Override content type must have a ContentType attribute.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Override", "ContentType");
            part.AddDiagnostic(diagnostic);
        }

        if (item.PartName.Length > 0)
        {
            if (overrideParts.ContainsKey(item.PartName))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.override.duplicate", "Content types XML contains a duplicate override part name.");
                diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Override", "PartName");
                part.AddDiagnostic(diagnostic);
            }
            else
            {
                overrideParts.Add(item.PartName, true);
                part.Overrides.Add(item);
            }
        }

        reader.ReadOuterXml();
    }

    internal static XlsxContentTypesPart CreateMissingRootResult()
    {
        XlsxContentTypesPart part = new XlsxContentTypesPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.missingRoot", "Content types XML must contain a Types root element."));
        return part;
    }

    internal static void ReadUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes, string knownName)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && reader.LocalName == knownName;
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                XlsxXmlAttribute attribute = new XlsxXmlAttribute();
                attribute.NamespaceUri = reader.NamespaceURI;
                attribute.LocalName = reader.LocalName;
                attribute.Prefix = reader.Prefix;
                attribute.Value = reader.Value;
                attributes.Add(attribute);
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes, string knownName1, string knownName2)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known1 = reader.NamespaceURI == "" && reader.LocalName == knownName1;
            bool known2 = reader.NamespaceURI == "" && reader.LocalName == knownName2;
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known1 && !known2 && !namespaceDeclaration)
            {
                XlsxXmlAttribute attribute = new XlsxXmlAttribute();
                attribute.NamespaceUri = reader.NamespaceURI;
                attribute.LocalName = reader.LocalName;
                attribute.Prefix = reader.Prefix;
                attribute.Value = reader.Value;
                attributes.Add(attribute);
            }
        }

        reader.MoveToElement();
    }
}
