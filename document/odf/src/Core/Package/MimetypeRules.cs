using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace odf;

internal static class MimetypeRules
{
    const uint LocalHeaderSignature = 67324752;

    internal static MimetypeInspection InspectPackage(
        byte[] packageData,
        OdfPackageEntry[] entries,
        string sourceName)
    {
        MimetypeInspection result = new MimetypeInspection();
        OdfPackageEntry? mimetypeEntry = null;
        OdfPackageEntry? manifestEntry = null;
        long firstOffset = long.MaxValue;
        foreach (OdfPackageEntry entry in entries)
        {
            if (entry.LocalHeaderOffset < firstOffset)
            {
                firstOffset = entry.LocalHeaderOffset;
            }
            if (entry.Path == OdfPartNames.Mimetype
                && (mimetypeEntry == null || entry.LocalHeaderOffset < mimetypeEntry.LocalHeaderOffset))
            {
                mimetypeEntry = entry;
            }
            if (entry.Path == OdfPartNames.Manifest
                && (manifestEntry == null || entry.LocalHeaderOffset < manifestEntry.LocalHeaderOffset))
            {
                manifestEntry = entry;
            }
        }
        foreach (OdfPackageEntry entry in entries)
        {
            entry.IsFirstEntry = entry.LocalHeaderOffset == firstOffset;
        }

        if (manifestEntry != null)
        {
            PackageEntryContent manifestContent = ReadEntryContent(packageData, manifestEntry);
            if (manifestContent.IsAvailable)
            {
                result.ManifestMediaType = ReadRootManifestMediaType(manifestContent.Data);
            }
        }

        string declaredMediaType = "";
        bool mimetypeRulesValid = true;
        if (mimetypeEntry == null)
        {
            AddDiagnostic(
                result,
                OdfDiagnosticCodes.MissingMimetype,
                "The OpenDocument package does not contain a mimetype entry.",
                OdfPartNames.Mimetype);
            mimetypeRulesValid = false;
        }
        else
        {
            result.HasMimetype = true;
            PackageEntryContent mimetypeContent = ReadEntryContent(packageData, mimetypeEntry);
            if (!mimetypeEntry.IsFirstEntry)
            {
                AddDiagnostic(
                    result,
                    OdfDiagnosticCodes.MimetypeNotFirst,
                    "The mimetype entry is not the first local entry in the package.",
                    OdfPartNames.Mimetype);
                mimetypeRulesValid = false;
            }
            if (mimetypeEntry.CompressionMethod != OdfCompressionMethod.Stored)
            {
                AddDiagnostic(
                    result,
                    OdfDiagnosticCodes.MimetypeCompressed,
                    "The mimetype entry must be stored without compression.",
                    OdfPartNames.Mimetype);
                mimetypeRulesValid = false;
            }
            if (mimetypeEntry.LocalExtraFieldLength != 0
                || mimetypeEntry.CentralExtraFieldLength != 0)
            {
                AddDiagnostic(
                    result,
                    OdfDiagnosticCodes.MimetypeExtraField,
                    "The mimetype entry must not contain ZIP extra fields.",
                    OdfPartNames.Mimetype);
                mimetypeRulesValid = false;
            }
            if (mimetypeContent.IsAvailable)
            {
                declaredMediaType = Encoding.UTF8.GetString(mimetypeContent.Data);
            }
            if (!IsDocumentMediaType(declaredMediaType))
            {
                AddDiagnostic(
                    result,
                    OdfDiagnosticCodes.InvalidMimetype,
                    "The mimetype entry does not contain a recognized OpenDocument media type.",
                    OdfPartNames.Mimetype);
                mimetypeRulesValid = false;
            }
        }

        if (IsDocumentMediaType(declaredMediaType))
        {
            result.MediaType = declaredMediaType;
            if (result.ManifestMediaType.Length > 0
                && !string.Equals(
                    declaredMediaType,
                    result.ManifestMediaType,
                    StringComparison.Ordinal))
            {
                AddManifestMismatchDiagnostic(result, declaredMediaType);
                mimetypeRulesValid = false;
            }
        }
        else if (IsDocumentMediaType(result.ManifestMediaType))
        {
            result.MediaType = result.ManifestMediaType;
            result.MediaTypeWasInferred = true;
        }
        else
        {
            result.MediaType = InferMediaTypeFromName(sourceName);
            result.MediaTypeWasInferred = result.MediaType.Length > 0;
        }
        result.DocumentKind = GetDocumentKind(result.MediaType);
        result.MimetypeIsValid = result.HasMimetype && mimetypeRulesValid;
        return result;
    }

    internal static string InferMediaTypeFromName(string? sourceName)
    {
        if (sourceName == null || sourceName.Length == 0)
        {
            return "";
        }
        string extension = Path.GetExtension(sourceName).ToLowerInvariant();
        if (extension == ".odt")
        {
            return OdfMediaTypes.Text;
        }
        if (extension == ".ott")
        {
            return OdfMediaTypes.TextTemplate;
        }
        if (extension == ".odm")
        {
            return OdfMediaTypes.TextMaster;
        }
        if (extension == ".otm")
        {
            return OdfMediaTypes.TextMasterTemplate;
        }
        if (extension == ".ods")
        {
            return OdfMediaTypes.Spreadsheet;
        }
        if (extension == ".ots")
        {
            return OdfMediaTypes.SpreadsheetTemplate;
        }
        if (extension == ".odp")
        {
            return OdfMediaTypes.Presentation;
        }
        if (extension == ".otp")
        {
            return OdfMediaTypes.PresentationTemplate;
        }
        if (extension == ".odg")
        {
            return OdfMediaTypes.Graphics;
        }
        if (extension == ".otg")
        {
            return OdfMediaTypes.GraphicsTemplate;
        }
        if (extension == ".odc")
        {
            return OdfMediaTypes.Chart;
        }
        if (extension == ".otc")
        {
            return OdfMediaTypes.ChartTemplate;
        }
        if (extension == ".odf")
        {
            return OdfMediaTypes.Formula;
        }
        if (extension == ".otf")
        {
            return OdfMediaTypes.FormulaTemplate;
        }
        if (extension == ".odi")
        {
            return OdfMediaTypes.Image;
        }
        if (extension == ".oti")
        {
            return OdfMediaTypes.ImageTemplate;
        }
        if (extension == ".odb")
        {
            return OdfMediaTypes.Base;
        }
        return "";
    }

    static bool IsDocumentMediaType(string mediaType)
    {
        return mediaType == OdfMediaTypes.Base
            || mediaType == OdfMediaTypes.Text
            || mediaType == OdfMediaTypes.TextTemplate
            || mediaType == OdfMediaTypes.TextMaster
            || mediaType == OdfMediaTypes.TextMasterTemplate
            || mediaType == OdfMediaTypes.Spreadsheet
            || mediaType == OdfMediaTypes.SpreadsheetTemplate
            || mediaType == OdfMediaTypes.Presentation
            || mediaType == OdfMediaTypes.PresentationTemplate
            || mediaType == OdfMediaTypes.Graphics
            || mediaType == OdfMediaTypes.GraphicsTemplate
            || mediaType == OdfMediaTypes.Chart
            || mediaType == OdfMediaTypes.ChartTemplate
            || mediaType == OdfMediaTypes.Formula
            || mediaType == OdfMediaTypes.FormulaTemplate
            || mediaType == OdfMediaTypes.Image
            || mediaType == OdfMediaTypes.ImageTemplate;
    }

    internal static OdfDocumentKind GetDocumentKind(string mediaType)
    {
        if (mediaType == OdfMediaTypes.Text)
        {
            return OdfDocumentKind.Text;
        }
        if (mediaType == OdfMediaTypes.TextTemplate)
        {
            return OdfDocumentKind.TextTemplate;
        }
        if (mediaType == OdfMediaTypes.TextMaster)
        {
            return OdfDocumentKind.TextMaster;
        }
        if (mediaType == OdfMediaTypes.TextMasterTemplate)
        {
            return OdfDocumentKind.TextMasterTemplate;
        }
        if (mediaType == OdfMediaTypes.Spreadsheet)
        {
            return OdfDocumentKind.Spreadsheet;
        }
        if (mediaType == OdfMediaTypes.SpreadsheetTemplate)
        {
            return OdfDocumentKind.SpreadsheetTemplate;
        }
        if (mediaType == OdfMediaTypes.Presentation)
        {
            return OdfDocumentKind.Presentation;
        }
        if (mediaType == OdfMediaTypes.PresentationTemplate)
        {
            return OdfDocumentKind.PresentationTemplate;
        }
        if (mediaType == OdfMediaTypes.Graphics)
        {
            return OdfDocumentKind.Drawing;
        }
        if (mediaType == OdfMediaTypes.GraphicsTemplate)
        {
            return OdfDocumentKind.DrawingTemplate;
        }
        if (mediaType == OdfMediaTypes.Chart)
        {
            return OdfDocumentKind.Chart;
        }
        if (mediaType == OdfMediaTypes.ChartTemplate)
        {
            return OdfDocumentKind.ChartTemplate;
        }
        if (mediaType == OdfMediaTypes.Formula)
        {
            return OdfDocumentKind.Formula;
        }
        if (mediaType == OdfMediaTypes.FormulaTemplate)
        {
            return OdfDocumentKind.FormulaTemplate;
        }
        if (mediaType == OdfMediaTypes.Image)
        {
            return OdfDocumentKind.Image;
        }
        if (mediaType == OdfMediaTypes.ImageTemplate)
        {
            return OdfDocumentKind.ImageTemplate;
        }
        if (mediaType == OdfMediaTypes.Base)
        {
            return OdfDocumentKind.Database;
        }
        return OdfDocumentKind.Unknown;
    }

    internal static PackageEntryContent ReadEntryContent(byte[] packageData, OdfPackageEntry entry)
    {
        PackageEntryContent result = new PackageEntryContent();
        if (!entry.HasValidCrc
            || entry.IsEncrypted
            || entry.LocalHeaderOffset < 0
            || entry.LocalHeaderOffset > int.MaxValue
            || entry.CompressedSize < 0
            || entry.CompressedSize > int.MaxValue
            || entry.ExpandedSize < 0
            || entry.ExpandedSize > int.MaxValue)
        {
            return result;
        }
        int localOffset = (int)entry.LocalHeaderOffset;
        if (!HasRange(packageData, localOffset, 30)
            || ReadUInt32(packageData, localOffset) != LocalHeaderSignature)
        {
            return result;
        }
        int nameLength = ReadUInt16(packageData, localOffset + 26);
        int extraLength = ReadUInt16(packageData, localOffset + 28);
        entry.LocalExtraFieldLength = extraLength;
        int contentOffset = localOffset + 30 + nameLength + extraLength;
        if (!HasRange(packageData, contentOffset, (int)entry.CompressedSize))
        {
            return result;
        }
        if (entry.CompressionMethod == OdfCompressionMethod.Stored)
        {
            byte[] content = new byte[(int)entry.CompressedSize];
            Array.Copy(packageData, contentOffset, content, 0, content.Length);
            result.Data = content;
            result.IsAvailable = content.LongLength == entry.ExpandedSize;
            return result;
        }
        if (entry.CompressionMethod != OdfCompressionMethod.Deflated)
        {
            return result;
        }
        using (MemoryStream compressed = new MemoryStream(
            packageData,
            contentOffset,
            (int)entry.CompressedSize,
            false))
        using (DeflateStream inflater = new DeflateStream(
            compressed,
            CompressionMode.Decompress,
            false))
        using (MemoryStream expanded = new MemoryStream())
        {
            inflater.CopyTo(expanded);
            if (expanded.Length != entry.ExpandedSize)
            {
                return result;
            }
            result.Data = expanded.ToArray();
            result.IsAvailable = true;
            return result;
        }
    }

    static string ReadRootManifestMediaType(byte[] manifestData)
    {
        string xml = Encoding.UTF8.GetString(manifestData);
        int cursor = 0;
        while (cursor < xml.Length)
        {
            int elementStart = xml.IndexOf('<', cursor);
            if (elementStart < 0 || elementStart + 1 >= xml.Length)
            {
                return "";
            }
            int elementEnd = FindElementEnd(xml, elementStart + 1);
            if (elementEnd < 0)
            {
                return "";
            }
            char marker = xml[elementStart + 1];
            if (marker != '/' && marker != '!' && marker != '?')
            {
                int nameStart = elementStart + 1;
                int nameEnd = nameStart;
                while (nameEnd < elementEnd && !IsXmlWhitespace(xml[nameEnd])
                    && xml[nameEnd] != '/' && xml[nameEnd] != '>')
                {
                    nameEnd = nameEnd + 1;
                }
                string elementName = xml.Substring(nameStart, nameEnd - nameStart);
                if (LocalName(elementName) == "file-entry")
                {
                    string fullPath = ReadAttributeValue(
                        xml,
                        nameEnd,
                        elementEnd,
                        "full-path");
                    if (fullPath == OdfPartNames.Root)
                    {
                        return ReadAttributeValue(
                            xml,
                            nameEnd,
                            elementEnd,
                            "media-type");
                    }
                }
            }
            cursor = elementEnd + 1;
        }
        return "";
    }

    static int FindElementEnd(string xml, int start)
    {
        char quote = (char)0;
        int index = start;
        while (index < xml.Length)
        {
            char current = xml[index];
            if (quote == 0 && (current == '\'' || current == '"'))
            {
                quote = current;
            }
            else if (quote != 0 && current == quote)
            {
                quote = (char)0;
            }
            else if (quote == 0 && current == '>')
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static string ReadAttributeValue(
        string xml,
        int start,
        int end,
        string localName)
    {
        int index = start;
        while (index < end)
        {
            while (index < end && IsXmlWhitespace(xml[index]))
            {
                index = index + 1;
            }
            if (index >= end || xml[index] == '/')
            {
                return "";
            }
            int nameStart = index;
            while (index < end && !IsXmlWhitespace(xml[index])
                && xml[index] != '=' && xml[index] != '/' && xml[index] != '>')
            {
                index = index + 1;
            }
            string name = xml.Substring(nameStart, index - nameStart);
            while (index < end && IsXmlWhitespace(xml[index]))
            {
                index = index + 1;
            }
            if (index >= end || xml[index] != '=')
            {
                return "";
            }
            index = index + 1;
            while (index < end && IsXmlWhitespace(xml[index]))
            {
                index = index + 1;
            }
            if (index >= end || xml[index] != '\'' && xml[index] != '"')
            {
                return "";
            }
            char quote = xml[index];
            index = index + 1;
            int valueStart = index;
            while (index < end && xml[index] != quote)
            {
                index = index + 1;
            }
            if (index >= end)
            {
                return "";
            }
            string value = xml.Substring(valueStart, index - valueStart);
            index = index + 1;
            if (LocalName(name) == localName)
            {
                return value;
            }
        }
        return "";
    }

    static string LocalName(string qualifiedName)
    {
        int separator = qualifiedName.IndexOf(':');
        if (separator < 0)
        {
            return qualifiedName;
        }
        return qualifiedName.Substring(separator + 1);
    }

    static bool IsXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    static bool HasRange(byte[] data, int offset, int length)
    {
        return offset >= 0 && length >= 0 && offset <= data.Length - length;
    }

    static ushort ReadUInt16(byte[] data, int offset)
    {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)data[offset]
            | ((uint)data[offset + 1] << 8)
            | ((uint)data[offset + 2] << 16)
            | ((uint)data[offset + 3] << 24);
    }

    static void AddDiagnostic(
        MimetypeInspection inspection,
        string code,
        string message,
        string partPath)
    {
        OdfLocation location = new OdfLocation();
        location.PartPath = partPath;
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            code,
            message,
            OdfValidationSeverity.Error,
            location,
            null,
            OdfVersion.Unknown);
        AppendDiagnostic(inspection, diagnostic);
    }

    static void AddManifestMismatchDiagnostic(
        MimetypeInspection inspection,
        string declaredMediaType)
    {
        OdfLocation location = new OdfLocation();
        location.PartPath = OdfPartNames.Manifest;
        OdfLocation related = new OdfLocation();
        related.PartPath = OdfPartNames.Mimetype;
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.ManifestMediaTypeMismatch,
            $"Root manifest media type '{inspection.ManifestMediaType}' does not match mimetype '{declaredMediaType}'.",
            OdfValidationSeverity.Error,
            location,
            related,
            OdfVersion.Unknown);
        AppendDiagnostic(inspection, diagnostic);
    }

    static void AppendDiagnostic(
        MimetypeInspection inspection,
        OdfDiagnostic diagnostic)
    {
        OdfDiagnostic[] updated = new OdfDiagnostic[inspection.Diagnostics.Length + 1];
        Array.Copy(inspection.Diagnostics, updated, inspection.Diagnostics.Length);
        updated[updated.Length - 1] = diagnostic;
        inspection.Diagnostics = updated;
    }
}
