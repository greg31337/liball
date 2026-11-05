using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes content types data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxContentTypesWriter : IXlsxContentTypesWriter
{
    /// <summary>
    /// Initializes a new content types writer with its default state.
    /// </summary>
    public XlsxContentTypesWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxContentTypesWriteResult WriteString(XlsxContentTypesPart part)
    {
        XlsxContentTypesWriteResult result = WriteBytes(part);
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
    public XlsxContentTypesWriteResult WriteBytes(XlsxContentTypesPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxContentTypesWriteResult result = WriteStream(stream, part);
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
    public XlsxContentTypesWriteResult WriteStream(Stream stream, XlsxContentTypesPart part)
    {
        XlsxContentTypesWriteResult result = ValidateContentTypesPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        XmlWriterSettings settings = CreateXmlWriterSettings();
        List<XlsxContentTypeDefault> defaults = CopyDefaults(part);
        List<XlsxContentTypeOverride> overrides = CopyOverrides(part);
        defaults.Sort(CompareDefaults);
        overrides.Sort(CompareOverrides);
        using (XmlWriter writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "Types", XlsxNamespaces.ContentTypes);
            WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            foreach (XlsxContentTypeDefault item in defaults)
            {
                WriteDefault(writer, item);
            }

            foreach (XlsxContentTypeOverride item in overrides)
            {
                WriteOverride(writer, item);
            }

            foreach (string unknownXml in part.UnknownXml)
            {
                if (unknownXml != null && unknownXml.Length > 0)
                {
                    writer.WriteRaw(unknownXml);
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XmlWriterSettings CreateXmlWriterSettings()
    {
        return XlsxXmlWriterHelpers.CreateStableWriterSettings();
    }

    internal static XlsxContentTypesWriteResult ValidateContentTypesPart(XlsxContentTypesPart part)
    {
        XlsxContentTypesWriteResult result = new XlsxContentTypesWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.partNull", "Content types part must not be null."));
            return result;
        }

        Dictionary<string, bool> defaultExtensions = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
        Dictionary<string, bool> overrideParts = new Dictionary<string, bool>();
        foreach (XlsxContentTypeDefault item in part.Defaults)
        {
            ValidateDefault(result, item, defaultExtensions);
        }

        foreach (XlsxContentTypeOverride item in part.Overrides)
        {
            ValidateOverride(result, item, overrideParts);
        }

        return result;
    }

    internal static void ValidateDefault(XlsxContentTypesWriteResult result, XlsxContentTypeDefault item, Dictionary<string, bool> defaultExtensions)
    {
        if (item == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.default.null", "Default content type entry must not be null."));
            return;
        }

        if (item.Extension == null || item.Extension.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.default.extensionMissing", "Default content type must have an Extension attribute.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Default", "Extension");
            result.AddDiagnostic(diagnostic);
        }

        if (item.ContentType == null || item.ContentType.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.default.contentTypeMissing", "Default content type must have a ContentType attribute.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Default", "ContentType");
            result.AddDiagnostic(diagnostic);
        }

        if (item.Extension != null && item.Extension.Length > 0)
        {
            if (defaultExtensions.ContainsKey(item.Extension))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.default.duplicate", "Content types XML contains a duplicate default extension.");
                diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Default", "Extension");
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                defaultExtensions.Add(item.Extension, true);
            }
        }
    }

    internal static void ValidateOverride(XlsxContentTypesWriteResult result, XlsxContentTypeOverride item, Dictionary<string, bool> overrideParts)
    {
        if (item == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.override.null", "Override content type entry must not be null."));
            return;
        }

        string normalizedPath = "";
        if (item.PartName == null || item.PartName.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.override.partNameMissing", "Override content type must have a PartName attribute.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Override", "PartName");
            result.AddDiagnostic(diagnostic);
        }
        else
        {
            XlsxPackagePathResult pathResult = XlsxPackagePath.NormalizePartPath(item.PartName);
            foreach (XlsxDiagnostic diagnostic in pathResult.Validation.Diagnostics)
            {
                diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Override", "PartName");
                result.AddDiagnostic(diagnostic);
            }

            normalizedPath = pathResult.Path;
        }

        if (item.ContentType == null || item.ContentType.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.override.contentTypeMissing", "Override content type must have a ContentType attribute.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Override", "ContentType");
            result.AddDiagnostic(diagnostic);
        }

        if (normalizedPath.Length > 0)
        {
            if (overrideParts.ContainsKey(normalizedPath))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.override.duplicate", "Content types XML contains a duplicate override part name.");
                diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Override", "PartName");
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                overrideParts.Add(normalizedPath, true);
            }
        }
    }

    internal static List<XlsxContentTypeDefault> CopyDefaults(XlsxContentTypesPart part)
    {
        List<XlsxContentTypeDefault> defaults = new List<XlsxContentTypeDefault>();
        foreach (XlsxContentTypeDefault item in part.Defaults)
        {
            defaults.Add(item);
        }

        return defaults;
    }

    internal static List<XlsxContentTypeOverride> CopyOverrides(XlsxContentTypesPart part)
    {
        List<XlsxContentTypeOverride> overrides = new List<XlsxContentTypeOverride>();
        foreach (XlsxContentTypeOverride item in part.Overrides)
        {
            overrides.Add(item);
        }

        return overrides;
    }

    internal static int CompareDefaults(XlsxContentTypeDefault left, XlsxContentTypeDefault right)
    {
        int result = string.Compare(left.Extension, right.Extension, System.StringComparison.OrdinalIgnoreCase);
        if (result != 0)
        {
            return result;
        }

        return string.Compare(left.Extension, right.Extension, System.StringComparison.Ordinal);
    }

    internal static int CompareOverrides(XlsxContentTypeOverride left, XlsxContentTypeOverride right)
    {
        return string.Compare(left.PartName, right.PartName, System.StringComparison.Ordinal);
    }

    internal static void WriteDefault(XmlWriter writer, XlsxContentTypeDefault item)
    {
        writer.WriteStartElement("", "Default", XlsxNamespaces.ContentTypes);
        writer.WriteAttributeString("Extension", item.Extension);
        writer.WriteAttributeString("ContentType", item.ContentType);
        WriteUnknownAttributes(writer, item.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteOverride(XmlWriter writer, XlsxContentTypeOverride item)
    {
        XlsxPackagePathResult pathResult = XlsxPackagePath.NormalizePartPath(item.PartName);
        writer.WriteStartElement("", "Override", XlsxNamespaces.ContentTypes);
        writer.WriteAttributeString("PartName", pathResult.Path);
        writer.WriteAttributeString("ContentType", item.ContentType);
        WriteUnknownAttributes(writer, item.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteUnknownAttributes(XmlWriter writer, List<XlsxXmlAttribute> attributes)
    {
        foreach (XlsxXmlAttribute attribute in attributes)
        {
            if (attribute.NamespaceUri == null || attribute.NamespaceUri.Length == 0)
            {
                writer.WriteAttributeString(attribute.LocalName, attribute.Value);
            }
            else
            {
                writer.WriteAttributeString(attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
            }
        }
    }
}
