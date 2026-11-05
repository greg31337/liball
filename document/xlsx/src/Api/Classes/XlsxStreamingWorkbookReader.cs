using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads streaming workbook data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxStreamingWorkbookReader : IXlsxStreamingWorkbookReader
{
    /// <summary>
    /// Initializes a new streaming workbook reader with its default state.
    /// </summary>
    public XlsxStreamingWorkbookReader()
    {
    }

    /// <summary>
    /// Opens and parses the requested file and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "filePath">The file-system path to read or write.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookReadResult ReadFile(string filePath)
    {
        return ReadFileWithOptions(filePath, new XlsxReaderOptions());
    }

    /// <summary>
    /// Opens and parses the requested file and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "filePath">The file-system path to read or write.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookReadResult ReadFileWithOptions(string filePath, XlsxReaderOptions options)
    {
        using (FileStream stream = File.OpenRead(filePath))
        {
            return ReadStreamWithOptions(stream, options);
        }
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookReadResult ReadBytes(byte[] bytes)
    {
        return ReadBytesWithOptions(bytes, new XlsxReaderOptions());
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookReadResult ReadBytesWithOptions(byte[] bytes, XlsxReaderOptions options)
    {
        if (bytes == null || bytes.Length == 0)
        {
            XlsxStreamingWorkbookReadResult result = new XlsxStreamingWorkbookReadResult();
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "streamingWorkbook.open.bytesEmpty", "Workbook bytes must not be empty."));
            return result;
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStreamWithOptions(stream, options);
        }
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookReadResult ReadStream(Stream stream)
    {
        return ReadStreamWithOptions(stream, new XlsxReaderOptions());
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxStreamingWorkbookReadResult ReadStreamWithOptions(Stream stream, XlsxReaderOptions options)
    {
        XlsxReaderOptions readerOptions = options;
        if (readerOptions == null)
        {
            readerOptions = new XlsxReaderOptions();
        }

        XlsxStreamingWorkbookReadResult result = new XlsxStreamingWorkbookReadResult();
        if (stream == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "streamingWorkbook.open.streamNull", "Workbook stream must not be null."));
            return result;
        }

        if (!stream.CanRead)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "streamingWorkbook.open.streamNotReadable", "Workbook stream must be readable."));
            return result;
        }

        CopyDiagnostics(result, readerOptions.Validate());
        if (!result.IsValid)
        {
            return result;
        }

        if (XlsxEncryptionDetector.IsEncryptedPackageStream(stream))
        {
            if (readerOptions.Password != null && readerOptions.Password.Length > 0)
            {
                byte[] encryptedBytes;
                using (MemoryStream encryptedBuffer = new MemoryStream())
                {
                    stream.CopyTo(encryptedBuffer);
                    encryptedBytes = encryptedBuffer.ToArray();
                }
                char[] passwordCharacters = readerOptions.Password.ToCharArray();
                XlsxEncryptedPackageResult decrypted = XlsxEncryptedPackageCryptography.Decrypt(encryptedBytes, passwordCharacters);
                Array.Clear(passwordCharacters, 0, passwordCharacters.Length);
                if (!decrypted.IsValid)
                {
                    CopyDiagnostics(result, decrypted.Validation);
                    return result;
                }
                return ReadBytesWithOptions(decrypted.Bytes, readerOptions);
            }
            else
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.encryptedPackageUnsupported", "Encrypted OOXML workbooks are detected, but decryption is not enabled."));
            }

            return result;
        }

        if (!XlsxZipPackageReader.HasZipHeader(stream))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "zip.invalidArchive", "Workbook package must be a readable ZIP archive."));
            return result;
        }

        XlsxStreamingWorkbookPartBytes partBytes = ReadMetadataParts(stream, readerOptions, result);
        if (!result.IsValid)
        {
            return result;
        }

        PopulateWorkbookInfo(result, partBytes, readerOptions);
        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static XlsxStreamingWorkbookPartBytes ReadMetadataParts(Stream stream, XlsxReaderOptions options, XlsxStreamingWorkbookReadResult result)
    {
        XlsxStreamingWorkbookPartBytes partBytes = new XlsxStreamingWorkbookPartBytes();
        Dictionary<string, bool> seenPaths = new Dictionary<string, bool>();
        bool entryLimitReported = false;
        bool compressedLimitReported = false;
        bool metadataLimitReported = false;
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            foreach (ZipArchiveEntry zipEntry in archive.Entries)
            {
                result.WorkbookInfo.PackageEntryCount = result.WorkbookInfo.PackageEntryCount + 1;
                result.WorkbookInfo.TotalCompressedBytes = result.WorkbookInfo.TotalCompressedBytes + zipEntry.CompressedLength;
                result.WorkbookInfo.TotalUncompressedBytes = result.WorkbookInfo.TotalUncompressedBytes + zipEntry.Length;
                if (!entryLimitReported && result.WorkbookInfo.PackageEntryCount > options.MaxZipEntries)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "zip.maxEntries", "ZIP entry count exceeds the configured limit."));
                    entryLimitReported = true;
                }

                if (!compressedLimitReported && result.WorkbookInfo.TotalCompressedBytes > options.MaxCompressedBytes)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "zip.maxCompressedBytes", "ZIP compressed byte count exceeds the configured limit."));
                    compressedLimitReported = true;
                }

                if (zipEntry.FullName.EndsWith("/"))
                {
                    continue;
                }

                XlsxPackagePathResult normalizedPath = XlsxPackagePath.NormalizePartPath(zipEntry.FullName);
                foreach (XlsxDiagnostic diagnostic in normalizedPath.Validation.Diagnostics)
                {
                    diagnostic.SetLocation(zipEntry.FullName, "", "");
                    result.AddDiagnostic(diagnostic);
                }

                if (!normalizedPath.Validation.IsValid)
                {
                    continue;
                }

                if (seenPaths.ContainsKey(normalizedPath.Path))
                {
                    XlsxDiagnostic duplicateDiagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "zip.duplicateEntry", "ZIP package contains duplicate part entries.");
                    duplicateDiagnostic.SetLocation(normalizedPath.Path, "", "");
                    result.AddDiagnostic(duplicateDiagnostic);
                    continue;
                }

                seenPaths.Add(normalizedPath.Path, true);
                if (IsSheetPartPath(normalizedPath.Path))
                {
                    result.WorkbookInfo.SkippedSheetEntryCount = result.WorkbookInfo.SkippedSheetEntryCount + 1;
                    result.WorkbookInfo.SkippedSheetBytes = result.WorkbookInfo.SkippedSheetBytes + zipEntry.Length;
                    continue;
                }

                if (IsSheetRelationshipPartPath(normalizedPath.Path))
                {
                    result.WorkbookInfo.SkippedSheetRelationshipEntryCount = result.WorkbookInfo.SkippedSheetRelationshipEntryCount + 1;
                    result.WorkbookInfo.SkippedSheetRelationshipBytes = result.WorkbookInfo.SkippedSheetRelationshipBytes + zipEntry.Length;
                    continue;
                }

                if (!IsWorkbookMetadataPartPath(normalizedPath.Path))
                {
                    continue;
                }

                if (result.WorkbookInfo.MetadataBytesLoaded + zipEntry.Length > options.MaxUncompressedBytes)
                {
                    if (!metadataLimitReported)
                    {
                        result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "streamingWorkbook.maxMetadataBytes", "Streaming workbook metadata byte count exceeds the configured uncompressed byte limit."));
                        metadataLimitReported = true;
                    }

                    continue;
                }

                byte[] data = ReadEntryBytes(zipEntry);
                result.WorkbookInfo.MetadataBytesLoaded = result.WorkbookInfo.MetadataBytesLoaded + data.Length;
                SetMetadataPartBytes(partBytes, normalizedPath.Path, data);
            }
        }

        return partBytes;
    }

    internal static void PopulateWorkbookInfo(XlsxStreamingWorkbookReadResult result, XlsxStreamingWorkbookPartBytes partBytes, XlsxReaderOptions options)
    {
        XlsxContentTypesPart contentTypes = ReadContentTypes(partBytes, result);
        XlsxStreamingWorkbookInfo info = result.WorkbookInfo;
        info.PackageVariant = XlsxWorkbook.DetectPackageVariant(contentTypes);
        info.WorkbookPart = ReadWorkbookPart(partBytes, result);
        info.PackageRelationships = ReadPackageRelationships(partBytes, options, result);
        info.WorkbookRelationships = ReadWorkbookRelationships(partBytes, options, result);
        info.RelationshipResolution = new XlsxWorkbookRelationshipResolver().Resolve(info.WorkbookPart, info.WorkbookRelationships);
        CopyDiagnostics(result, info.RelationshipResolution.Validation);
        info.HasStyles = info.RelationshipResolution.HasStyles;
        info.HasTheme = info.RelationshipResolution.HasTheme;
        info.HasSharedStrings = info.RelationshipResolution.HasSharedStrings;
        info.HasVbaProject = info.RelationshipResolution.HasVbaProject;
        if (XlsxWorkbook.IsMacroEnabledPackageVariant(info.PackageVariant) && !options.AllowMacroEnabledPackages)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, "workbook.open.macroEnabledPackage", "Workbook package is macro-enabled."));
        }

        ReadDocumentProperties(info, partBytes, result);
    }

    internal static XlsxContentTypesPart ReadContentTypes(XlsxStreamingWorkbookPartBytes partBytes, XlsxStreamingWorkbookReadResult result)
    {
        if (partBytes.ContentTypes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.contentTypesMissing", "Workbook package is missing [Content_Types].xml."));
            return new XlsxContentTypesPart();
        }

        XlsxContentTypesPart part = new XlsxContentTypesReader().ReadBytes(partBytes.ContentTypes);
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static XlsxWorkbookPart ReadWorkbookPart(XlsxStreamingWorkbookPartBytes partBytes, XlsxStreamingWorkbookReadResult result)
    {
        if (partBytes.Workbook == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.workbookMissing", "Workbook package is missing xl/workbook.xml."));
            return new XlsxWorkbookPart();
        }

        XlsxWorkbookPart part = new XlsxWorkbookPartReader().ReadBytes(partBytes.Workbook, new XlsxReaderOptions());
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static XlsxRelationshipsPart ReadPackageRelationships(XlsxStreamingWorkbookPartBytes partBytes, XlsxReaderOptions options, XlsxStreamingWorkbookReadResult result)
    {
        if (partBytes.PackageRelationships == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.open.packageRelationshipsMissing", "Workbook package is missing _rels/.rels."));
            return new XlsxRelationshipsPart();
        }

        XlsxRelationshipsPart part = new XlsxPackageRelationshipsReader().ReadBytes(partBytes.PackageRelationships, options);
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static XlsxRelationshipsPart ReadWorkbookRelationships(XlsxStreamingWorkbookPartBytes partBytes, XlsxReaderOptions options, XlsxStreamingWorkbookReadResult result)
    {
        if (partBytes.WorkbookRelationships == null)
        {
            return XlsxWorkbook.CreateWorkbookRelationships();
        }

        XlsxRelationshipsPart part = new XlsxPartRelationshipsReader().ReadBytes(XlsxPackagePaths.WorkbookRelationships, partBytes.WorkbookRelationships, options);
        CopyDiagnostics(result, part.Validation);
        return part;
    }

    internal static void ReadDocumentProperties(XlsxStreamingWorkbookInfo info, XlsxStreamingWorkbookPartBytes partBytes, XlsxStreamingWorkbookReadResult result)
    {
        if (partBytes.CoreProperties != null)
        {
            info.CoreProperties = new XlsxCorePropertiesReader().ReadBytes(partBytes.CoreProperties);
            info.HasCoreProperties = true;
            CopyDiagnostics(result, info.CoreProperties.Validation);
        }

        if (partBytes.ExtendedProperties != null)
        {
            info.ExtendedProperties = new XlsxExtendedPropertiesReader().ReadBytes(partBytes.ExtendedProperties);
            info.HasExtendedProperties = true;
            CopyDiagnostics(result, info.ExtendedProperties.Validation);
        }

        if (partBytes.CustomProperties != null)
        {
            info.CustomProperties = new XlsxCustomPropertiesReader().ReadBytes(partBytes.CustomProperties);
            info.HasCustomProperties = true;
            CopyDiagnostics(result, info.CustomProperties.Validation);
        }
    }

    internal static byte[] ReadEntryBytes(ZipArchiveEntry zipEntry)
    {
        using (Stream entryStream = zipEntry.Open())
        {
            using (MemoryStream entryBuffer = new MemoryStream())
            {
                entryStream.CopyTo(entryBuffer);
                return entryBuffer.ToArray();
            }
        }
    }

    internal static void SetMetadataPartBytes(XlsxStreamingWorkbookPartBytes partBytes, string path, byte[] data)
    {
        if (path == XlsxPackagePaths.ContentTypes)
        {
            partBytes.ContentTypes = data;
        }
        else if (path == XlsxPackagePaths.PackageRelationships)
        {
            partBytes.PackageRelationships = data;
        }
        else if (path == XlsxPackagePaths.Workbook)
        {
            partBytes.Workbook = data;
        }
        else if (path == XlsxPackagePaths.WorkbookRelationships)
        {
            partBytes.WorkbookRelationships = data;
        }
        else if (path == XlsxPackagePaths.CoreProperties)
        {
            partBytes.CoreProperties = data;
        }
        else if (path == XlsxPackagePaths.ExtendedProperties)
        {
            partBytes.ExtendedProperties = data;
        }
        else if (path == XlsxPackagePaths.CustomProperties)
        {
            partBytes.CustomProperties = data;
        }
    }

    internal static bool IsWorkbookMetadataPartPath(string path)
    {
        return path == XlsxPackagePaths.ContentTypes || path == XlsxPackagePaths.PackageRelationships || path == XlsxPackagePaths.Workbook || path == XlsxPackagePaths.WorkbookRelationships || path == XlsxPackagePaths.CoreProperties || path == XlsxPackagePaths.ExtendedProperties || path == XlsxPackagePaths.CustomProperties;
    }

    internal static bool IsSheetPartPath(string path)
    {
        if (path == null)
        {
            return false;
        }

        return (path.StartsWith(XlsxPackagePaths.WorksheetsDirectory, System.StringComparison.Ordinal) || path.StartsWith(XlsxPackagePaths.ChartsheetsDirectory, System.StringComparison.Ordinal) || path.StartsWith("/xl/dialogsheets/", System.StringComparison.Ordinal) || path.StartsWith("/xl/macrosheets/", System.StringComparison.Ordinal)) && path.EndsWith(".xml", System.StringComparison.Ordinal) && !path.Contains("/_rels/");
    }

    internal static bool IsSheetRelationshipPartPath(string path)
    {
        if (path == null)
        {
            return false;
        }

        return (path.StartsWith("/xl/worksheets/_rels/", System.StringComparison.Ordinal) || path.StartsWith("/xl/chartsheets/_rels/", System.StringComparison.Ordinal) || path.StartsWith("/xl/dialogsheets/_rels/", System.StringComparison.Ordinal) || path.StartsWith("/xl/macrosheets/_rels/", System.StringComparison.Ordinal)) && path.EndsWith(".rels", System.StringComparison.Ordinal);
    }

    internal static void CopyDiagnostics(XlsxStreamingWorkbookReadResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }
}
