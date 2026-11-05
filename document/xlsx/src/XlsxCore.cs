using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Represents package path data used by the public workbook API.
/// </summary>
internal static class XlsxPackagePath
{
    internal static XlsxPackagePathResult NormalizePartPath(string path)
    {
        return NormalizePackagePath(path, false);
    }

    internal static XlsxValidationResult ValidatePartPath(string path)
    {
        XlsxPackagePathResult result = NormalizePartPath(path);
        return result.Validation;
    }

    internal static bool IsValidPartPath(string path)
    {
        XlsxPackagePathResult result = NormalizePartPath(path);
        return result.Validation.IsValid;
    }

    internal static string GetZipEntryName(string path)
    {
        XlsxPackagePathResult result = NormalizePartPath(path);
        if (!result.Validation.IsValid)
        {
            throw new XlsxException("Package path is invalid.", null);
        }

        return result.Path.Substring(1);
    }

    internal static XlsxPackagePathResult ResolveRelationshipTarget(string sourcePartPath, string? target, bool isExternal)
    {
        XlsxPackagePathResult result = new XlsxPackagePathResult();
        result.Input = target;
        result.IsExternal = isExternal;

        if (target == null || target.Length == 0)
        {
            result.Validation.AddError("relationship.target.empty", "Relationship target must not be empty.");
            return result;
        }

        if (isExternal)
        {
            result.ExternalTarget = target;
            return result;
        }

        int fragmentIndex = target.IndexOf('#');
        string targetPath = target;
        if (fragmentIndex >= 0)
        {
            targetPath = target.Substring(0, fragmentIndex);
            result.Fragment = target.Substring(fragmentIndex + 1);
        }

        if (LooksLikeExternalUri(targetPath))
        {
            result.Validation.AddError("relationship.target.externalModeRequired", "External relationship targets must use external target mode.");
            return result;
        }

        string combinedPath = "";
        if (targetPath.Length == 0)
        {
            combinedPath = sourcePartPath;
        }
        else if (targetPath[0] == '/')
        {
            combinedPath = targetPath;
        }
        else
        {
            string baseDirectory = GetRelationshipBaseDirectory(sourcePartPath, result.Validation);
            if (!result.Validation.IsValid)
            {
                return result;
            }

            combinedPath = baseDirectory + targetPath;
        }

        XlsxPackagePathResult normalized = NormalizePackagePath(combinedPath, true);
        result.Path = normalized.Path;
        foreach (XlsxDiagnostic diagnostic in normalized.Validation.Diagnostics)
        {
            result.Validation.AddDiagnostic(diagnostic);
        }

        return result;
    }

    internal static XlsxPackagePathResult NormalizePackagePath(string path, bool allowParentSegments)
    {
        XlsxPackagePathResult result = new XlsxPackagePathResult();
        result.Input = path;

        if (path == null || path.Length == 0)
        {
            result.Validation.AddError("path.empty", "Package path must not be empty.");
            return result;
        }

        if (LooksLikeExternalUri(path))
        {
            result.Validation.AddError("path.externalUri", "Package path must not be an external URI.");
            return result;
        }

        if (path.IndexOf('#') >= 0)
        {
            result.Validation.AddError("path.fragment", "Package path must not include a fragment.");
            return result;
        }

        if (path.IndexOf('?') >= 0)
        {
            result.Validation.AddError("path.query", "Package path must not include a query string.");
            return result;
        }

        if (path.IndexOf('\\') >= 0)
        {
            result.Validation.AddError("path.backslash", "Package path must use forward slashes.");
            return result;
        }

        if (!ValidatePercentEscapes(path, result.Validation)) { return result; }

        string absolutePath = path;
        if (absolutePath[0] != '/')
        {
            absolutePath = "/" + absolutePath;
        }

        string[] rawSegments = absolutePath.Split('/');
        List<string> segments = new List<string>();
        int index = 1;
        while (index < rawSegments.Length)
        {
            string segment = rawSegments[index];
            if (segment.Length == 0)
            {
                result.Validation.AddError("path.emptySegment", "Package path must not contain empty segments.");
                return result;
            }

            if (segment == ".")
            {
                index = index + 1;
                continue;
            }

            if (segment == "..")
            {
                if (!allowParentSegments)
                {
                    result.Validation.AddError("path.parentSegment", "Package path must not contain parent directory segments.");
                    return result;
                }

                if (segments.Count == 0)
                {
                    result.Validation.AddError("path.traversal", "Package path must not traverse outside the package root.");
                    return result;
                }

                segments.RemoveAt(segments.Count - 1);
                index = index + 1;
                continue;
            }

            if (!IsValidSegment(segment))
            {
                result.Validation.AddError("path.invalidSegment", "Package path contains an invalid segment.");
                return result;
            }

            segments.Add(segment);
            index = index + 1;
        }

        if (segments.Count == 0)
        {
            result.Validation.AddError("path.root", "Package path must identify a part, not the package root.");
            return result;
        }

        result.Path = BuildAbsolutePath(segments);
        return result;
    }

    internal static bool ValidatePercentEscapes(string path, XlsxValidationResult validation)
    {
        int index = 0;
        while (index < path.Length)
        {
            if (path[index] != '%') { index = index + 1; continue; }
            if (index + 2 >= path.Length || !IsHexDigit(path[index + 1]) || !IsHexDigit(path[index + 2]))
            {
                validation.AddError("path.percentEncoding", "Package path contains an invalid percent escape.");
                return false;
            }
            int value = HexDigitValue(path[index + 1]) * 16 + HexDigitValue(path[index + 2]);
            if (value == 0x2F || value == 0x5C)
            {
                validation.AddError("path.escapedSeparator", "Package path must not contain percent-escaped separators.");
                return false;
            }
            if (value == 0x2E || value == 0x3A)
            {
                validation.AddError("path.escapedDelimiter", "Package path must not contain percent-escaped dot or colon delimiters.");
                return false;
            }
            if (value <= 31 || value == 127)
            {
                validation.AddError("path.escapedControl", "Package path must not contain percent-escaped control characters.");
                return false;
            }
            index = index + 3;
        }
        return true;
    }

    internal static bool IsHexDigit(char value)
    {
        return (value >= '0' && value <= '9') || (value >= 'A' && value <= 'F') || (value >= 'a' && value <= 'f');
    }

    internal static int HexDigitValue(char value)
    {
        if (value >= '0' && value <= '9') { return value - '0'; }
        if (value >= 'A' && value <= 'F') { return value - 'A' + 10; }
        return value - 'a' + 10;
    }

    internal static string GetPartDirectory(string sourcePartPath)
    {
        if (sourcePartPath == null || sourcePartPath.Length == 0 || sourcePartPath == "/")
        {
            return "/";
        }

        string sourcePath = GetRelationshipSourcePartPath(sourcePartPath);
        if (sourcePath == "/")
        {
            return "/";
        }

        XlsxPackagePathResult source = NormalizePackagePath(sourcePath, false);
        if (!source.Validation.IsValid)
        {
            return "/";
        }

        int slashIndex = source.Path.LastIndexOf('/');
        if (slashIndex <= 0)
        {
            return "/";
        }

        return source.Path.Substring(0, slashIndex + 1);
    }

    internal static string GetRelationshipBaseDirectory(string sourcePartPath, XlsxValidationResult validation)
    {
        string sourcePath = GetRelationshipSourcePartPath(sourcePartPath);
        if (sourcePath == "/")
        {
            return "/";
        }

        XlsxPackagePathResult source = NormalizePackagePath(sourcePath, false);
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            validation.AddDiagnostic(diagnostic);
        }

        if (!validation.IsValid)
        {
            return "";
        }

        int slashIndex = source.Path.LastIndexOf('/');
        if (slashIndex <= 0)
        {
            return "/";
        }

        return source.Path.Substring(0, slashIndex + 1);
    }

    internal static string GetRelationshipSourcePartPath(string sourcePartPath)
    {
        if (sourcePartPath == null || sourcePartPath.Length == 0 || sourcePartPath == "/")
        {
            return "/";
        }

        if (sourcePartPath == XlsxPackagePaths.PackageRelationships)
        {
            return "/";
        }

        int relationshipDirectoryIndex = sourcePartPath.LastIndexOf("/_rels/");
        if (relationshipDirectoryIndex >= 0 && sourcePartPath.EndsWith(".rels"))
        {
            string beforeRelationshipDirectory = sourcePartPath.Substring(0, relationshipDirectoryIndex);
            string relationshipFileName = sourcePartPath.Substring(relationshipDirectoryIndex + 7);
            string sourceFileName = relationshipFileName.Substring(0, relationshipFileName.Length - 5);
            return beforeRelationshipDirectory + "/" + sourceFileName;
        }

        return sourcePartPath;
    }

    internal static string GetRelationshipPartPath(string sourcePartPath)
    {
        if (sourcePartPath == null || sourcePartPath.Length == 0 || sourcePartPath == "/" || sourcePartPath == XlsxPackagePaths.PackageRelationships)
        {
            return XlsxPackagePaths.PackageRelationships;
        }

        if (sourcePartPath.EndsWith(".rels") && sourcePartPath.IndexOf("/_rels/") >= 0)
        {
            XlsxPackagePathResult relationshipPart = NormalizePackagePath(sourcePartPath, false);
            if (relationshipPart.Validation.IsValid)
            {
                return relationshipPart.Path;
            }

            return "";
        }

        XlsxPackagePathResult source = NormalizePackagePath(sourcePartPath, false);
        if (!source.Validation.IsValid)
        {
            return "";
        }

        int slashIndex = source.Path.LastIndexOf('/');
        string directory = "/";
        if (slashIndex > 0)
        {
            directory = source.Path.Substring(0, slashIndex + 1);
        }

        string fileName = source.Path.Substring(slashIndex + 1);
        return directory + "_rels/" + fileName + ".rels";
    }

    internal static bool LooksLikeExternalUri(string path)
    {
        if (path == null)
        {
            return false;
        }

        int colonIndex = path.IndexOf(':');
        if (colonIndex <= 0)
        {
            return false;
        }

        int slashIndex = path.IndexOf('/');
        if (slashIndex >= 0 && slashIndex < colonIndex)
        {
            return false;
        }

        int index = 0;
        while (index < colonIndex)
        {
            char value = path[index];
            bool valid = false;
            if (value >= 'A' && value <= 'Z')
            {
                valid = true;
            }

            if (value >= 'a' && value <= 'z')
            {
                valid = true;
            }

            if (index > 0 && value >= '0' && value <= '9')
            {
                valid = true;
            }

            if (index > 0 && (value == '+' || value == '-' || value == '.'))
            {
                valid = true;
            }

            if (!valid)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidSegment(string segment)
    {
        if (segment == null || segment.Length == 0)
        {
            return false;
        }

        if (segment.IndexOf(':') >= 0)
        {
            return false;
        }

        int index = 0;
        while (index < segment.Length)
        {
            char value = segment[index];
            if (value <= 31 || value == 127)
            {
                return false;
            }

            if (value == '"' || value == '<' || value == '>' || value == '|' || value == '*')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string BuildAbsolutePath(List<string> segments)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < segments.Count)
        {
            builder.Append('/');
            builder.Append(segments[index]);
            index = index + 1;
        }

        return builder.ToString();
    }
}

internal class XlsxPackageEntry
{
    internal string Path;
    internal string ZipEntryName;
    internal long CompressedBytes;
    internal long UncompressedBytes;
    internal byte[] Data;

    internal XlsxPackageEntry()
    {
        Path = "";
        ZipEntryName = "";
        CompressedBytes = 0;
        UncompressedBytes = 0;
        Data = new byte[0];
    }

    internal string GetText()
    {
        return Encoding.UTF8.GetString(Data);
    }

    internal void SetText(string text)
    {
        Data = Encoding.UTF8.GetBytes(text);
        UncompressedBytes = Data.Length;
    }
}

internal class XlsxZipPackageReadResult
{
    internal bool IsValid;
    internal XlsxValidationResult Validation;
    internal List<XlsxPackageEntry> Entries;
    internal int EntryCount;
    internal long TotalCompressedBytes;
    internal long TotalUncompressedBytes;

    internal XlsxZipPackageReadResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Entries = new List<XlsxPackageEntry>();
        EntryCount = 0;
        TotalCompressedBytes = 0;
        TotalUncompressedBytes = 0;
    }

    /// <summary>
    /// Adds an existing diagnostic and marks the result invalid when its severity is <see cref="XlsxDiagnosticSeverity.Error"/>.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to append.</param>
    public void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal XlsxPackageEntry? GetEntry(string path)
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(path);
        if (!normalized.Validation.IsValid)
        {
            return null;
        }

        foreach (XlsxPackageEntry entry in Entries)
        {
            if (entry.Path == normalized.Path)
            {
                return entry;
            }
        }

        return null;
    }
}

internal class XlsxZipPackageReader
{
    internal XlsxZipPackageReadResult ReadFile(string filePath, XlsxReaderOptions options)
    {
        using (FileStream stream = File.OpenRead(filePath))
        {
            return ReadStream(stream, options);
        }
    }

    internal XlsxZipPackageReadResult ReadBytes(byte[] bytes, XlsxReaderOptions options)
    {
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(stream, options);
        }
    }

    internal XlsxZipPackageReadResult ReadStream(Stream stream, XlsxReaderOptions options)
    {
        XlsxReaderOptions readerOptions = options;
        if (readerOptions == null)
        {
            readerOptions = new XlsxReaderOptions();
        }

        XlsxZipPackageReadResult result = new XlsxZipPackageReadResult();
        XlsxValidationResult optionValidation = readerOptions.Validate();
        foreach (XlsxDiagnostic diagnostic in optionValidation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        if (!result.IsValid)
        {
            return result;
        }

        if (!HasZipHeader(stream))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "zip.invalidArchive", "Workbook package must be a readable ZIP archive."));
            return result;
        }

        Dictionary<string, bool> seenPaths = new Dictionary<string, bool>();
        bool entryLimitReported = false;
        bool compressedLimitReported = false;
        bool uncompressedLimitReported = false;
        bool partLimitReported = false;
        bool ratioLimitReported = false;

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            foreach (ZipArchiveEntry zipEntry in archive.Entries)
            {
                result.EntryCount = result.EntryCount + 1;
                result.TotalCompressedBytes = result.TotalCompressedBytes + zipEntry.CompressedLength;
                result.TotalUncompressedBytes = result.TotalUncompressedBytes + zipEntry.Length;

                if (!entryLimitReported && result.EntryCount > readerOptions.MaxZipEntries)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "zip.maxEntries", "ZIP entry count exceeds the configured limit."));
                    entryLimitReported = true;
                }

                if (!compressedLimitReported && result.TotalCompressedBytes > readerOptions.MaxCompressedBytes)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "zip.maxCompressedBytes", "ZIP compressed byte count exceeds the configured limit."));
                    compressedLimitReported = true;
                }

                if (!uncompressedLimitReported && result.TotalUncompressedBytes > readerOptions.MaxUncompressedBytes)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "zip.maxUncompressedBytes", "ZIP uncompressed byte count exceeds the configured limit."));
                    uncompressedLimitReported = true;
                }

                if (!partLimitReported && zipEntry.Length > readerOptions.SecurityOptions.MaxPartBytes)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "zip.maxPartBytes", "ZIP part size exceeds the configured limit."));
                    partLimitReported = true;
                }

                double compressionRatio = zipEntry.CompressedLength == 0 ? (zipEntry.Length == 0 ? 1 : double.MaxValue) : (double)zipEntry.Length / (double)zipEntry.CompressedLength;
                if (!ratioLimitReported && compressionRatio > readerOptions.SecurityOptions.MaxCompressionRatio)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "zip.maxCompressionRatio", "ZIP part compression ratio exceeds the configured limit."));
                    ratioLimitReported = true;
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

                XlsxPackageEntry entry = new XlsxPackageEntry();
                entry.Path = normalizedPath.Path;
                entry.ZipEntryName = zipEntry.FullName;
                entry.CompressedBytes = zipEntry.CompressedLength;
                entry.UncompressedBytes = zipEntry.Length;
                if (result.TotalCompressedBytes <= readerOptions.MaxCompressedBytes && result.TotalUncompressedBytes <= readerOptions.MaxUncompressedBytes && zipEntry.Length <= readerOptions.SecurityOptions.MaxPartBytes && compressionRatio <= readerOptions.SecurityOptions.MaxCompressionRatio)
                {
                    using (Stream entryStream = zipEntry.Open())
                    {
                        using (MemoryStream entryBuffer = new MemoryStream())
                        {
                            entryStream.CopyTo(entryBuffer);
                            entry.Data = entryBuffer.ToArray();
                        }
                    }
                }

                if (LooksLikeXml(entry.Data) && (entry.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || entry.Path.EndsWith(".rels", StringComparison.OrdinalIgnoreCase)))
                {
                    XlsxXmlReadLimitResult xmlLimits = XlsxXmlReaderHelpers.ValidateBytesLimits(entry.Data, readerOptions, entry.Path);
                    foreach (XlsxDiagnostic diagnostic in xmlLimits.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
                }

                result.Entries.Add(entry);
            }
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static bool LooksLikeXml(byte[] data)
    {
        if (data == null || data.Length == 0) { return false; }
        int index = 0;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) { index = 3; }
        while (index < data.Length && (data[index] == 0x09 || data[index] == 0x0A || data[index] == 0x0D || data[index] == 0x20)) { index = index + 1; }
        if (index < data.Length && data[index] == 0x3C) { return true; }
        if (data.Length >= index + 2 && ((data[index] == 0xFF && data[index + 1] == 0xFE) || (data[index] == 0xFE && data[index + 1] == 0xFF))) { return true; }
        return false;
    }

    internal static bool HasZipHeader(Stream stream)
    {
        if (stream == null || !stream.CanSeek)
        {
            return true;
        }

        long position = stream.Position;
        byte[] header = new byte[4];
        int read = stream.Read(header, 0, header.Length);
        stream.Position = position;
        if (read < 4)
        {
            return false;
        }

        bool hasLocalFileHeader = header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04;
        bool hasEmptyArchiveHeader = header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x05 && header[3] == 0x06;
        if (!hasLocalFileHeader && !hasEmptyArchiveHeader)
        {
            return false;
        }

        return HasZipEndOfCentralDirectory(stream, position);
    }

    internal static bool HasZipEndOfCentralDirectory(Stream stream, long startPosition)
    {
        long availableLength = stream.Length - startPosition;
        if (availableLength < 22)
        {
            return false;
        }

        long maximumTailLength = 65557;
        long tailLengthValue = availableLength < maximumTailLength ? availableLength : maximumTailLength;
        int tailLength = (int)tailLengthValue;
        byte[] tail = new byte[tailLength];
        stream.Position = stream.Length - tailLength;
        int totalRead = 0;
        while (totalRead < tailLength)
        {
            int currentRead = stream.Read(tail, totalRead, tailLength - totalRead);
            if (currentRead == 0)
            {
                stream.Position = startPosition;
                return false;
            }
            totalRead = totalRead + currentRead;
        }
        stream.Position = startPosition;

        int index = tailLength - 22;
        while (index >= 0)
        {
            if (tail[index] == 0x50 && tail[index + 1] == 0x4B && tail[index + 2] == 0x05 && tail[index + 3] == 0x06)
            {
                int commentLength = tail[index + 20] + (tail[index + 21] * 256);
                if (index + 22 + commentLength == tailLength)
                {
                    return true;
                }
            }
            index = index - 1;
        }
        return false;
    }
}

/// <summary>
/// Represents encryption detector data used by the public workbook API.
/// </summary>
internal static class XlsxEncryptionDetector
{
    internal static bool IsCompoundFileHeader(byte[] header)
    {
        if (header == null || header.Length < 8)
        {
            return false;
        }

        return header[0] == 0xD0 &&
            header[1] == 0xCF &&
            header[2] == 0x11 &&
            header[3] == 0xE0 &&
            header[4] == 0xA1 &&
            header[5] == 0xB1 &&
            header[6] == 0x1A &&
            header[7] == 0xE1;
    }

    internal static bool IsEncryptedPackageBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 8)
        {
            return false;
        }

        byte[] header = new byte[8];
        for (int index = 0; index < 8; index++)
        {
            header[index] = bytes[index];
        }

        return IsCompoundFileHeader(header);
    }

    internal static bool IsEncryptedPackageStream(Stream stream)
    {
        if (stream == null || !stream.CanSeek)
        {
            return false;
        }

        long position = stream.Position;
        byte[] header = new byte[8];
        int read = stream.Read(header, 0, header.Length);
        stream.Position = position;
        if (read < header.Length)
        {
            return false;
        }

        return IsCompoundFileHeader(header);
    }

    internal static XlsxEncryptionInfo ReadEncryptionInfo(byte[] bytes)
    {
        XlsxEncryptionInfo info = new XlsxEncryptionInfo();
        if (bytes == null || bytes.Length < 4)
        {
            info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptionInfo.tooShort", "EncryptionInfo stream is too short."));
            return info;
        }

        info.MajorVersion = ReadUInt16LittleEndian(bytes, 0);
        info.MinorVersion = ReadUInt16LittleEndian(bytes, 2);
        info.IsAgile = info.MajorVersion == 4 && info.MinorVersion == 4;
        info.IsStandard = !info.IsAgile && info.MinorVersion == 2 && (info.MajorVersion == 2 || info.MajorVersion == 3 || info.MajorVersion == 4);
        if (info.IsAgile)
        {
            info.EncryptionKind = "Agile";
            info.Kind = XlsxPackageEncryptionKind.Agile;
        }
        else if (info.IsStandard)
        {
            info.EncryptionKind = "Standard";
            info.Kind = XlsxPackageEncryptionKind.Standard;
        }
        else
        {
            info.EncryptionKind = "Unknown";
            info.Kind = XlsxPackageEncryptionKind.Unknown;
            info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, "encryptionInfo.versionUnknown", "EncryptionInfo version is not recognized as Agile or Standard encryption."));
        }

        return info;
    }

    internal static ushort ReadUInt16LittleEndian(byte[] bytes, int offset)
    {
        int value = bytes[offset] | (bytes[offset + 1] << 8);
        return (ushort)value;
    }
}

internal class XlsxZipPackageWriteResult
{
    internal bool IsValid;
    internal XlsxValidationResult Validation;
    internal int EntryCount;
    internal long TotalUncompressedBytes;
    internal byte[] Bytes;

    internal XlsxZipPackageWriteResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        EntryCount = 0;
        TotalUncompressedBytes = 0;
        Bytes = new byte[0];
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}

internal class XlsxZipPackageWriter
{
    internal XlsxZipPackageWriteResult WriteFile(string filePath, List<XlsxPackageEntry> entries, XlsxWriterOptions options)
    {
        XlsxZipPackageWriteResult result = WriteBytes(entries, options);
        if (result.IsValid)
        {
            File.WriteAllBytes(filePath, result.Bytes);
        }

        return result;
    }

    internal XlsxZipPackageWriteResult WriteBytes(List<XlsxPackageEntry> entries, XlsxWriterOptions? options)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxZipPackageWriteResult result = WriteStream(stream, entries, options);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
            }

            return result;
        }
    }

    internal XlsxZipPackageWriteResult WriteStream(Stream stream, List<XlsxPackageEntry> entries, XlsxWriterOptions? options)
    {
        XlsxWriterOptions? writerOptions = options;
        if (writerOptions == null)
        {
            writerOptions = new XlsxWriterOptions();
        }

        XlsxZipPackageWriteResult result = new XlsxZipPackageWriteResult();
        XlsxValidationResult optionValidation = writerOptions.Validate();
        foreach (XlsxDiagnostic diagnostic in optionValidation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        if (entries == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "writer.entriesNull", "Package entries must not be null."));
            return result;
        }

        List<XlsxPackageEntry> normalizedEntries = NormalizeEntriesForWrite(entries, result);
        if (normalizedEntries.Count > writerOptions.MaxZipEntries)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "writer.maxZipEntriesExceeded", "ZIP entry count exceeds the configured writer limit."));
        }

        long totalUncompressedBytes = 0;
        foreach (XlsxPackageEntry entry in normalizedEntries)
        {
            totalUncompressedBytes = totalUncompressedBytes + entry.Data.Length;
        }

        if (totalUncompressedBytes > writerOptions.MaxUncompressedBytes)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "writer.maxUncompressedBytesExceeded", "ZIP uncompressed byte count exceeds the configured writer limit."));
        }

        if (!result.IsValid)
        {
            return result;
        }

        if (writerOptions.DeterministicOutput)
        {
            normalizedEntries.Sort(CompareEntriesByPath);
        }

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (XlsxPackageEntry entry in normalizedEntries)
            {
                ZipArchiveEntry zipEntry = archive.CreateEntry(entry.ZipEntryName, writerOptions.CompressionLevel);
                if (writerOptions.DeterministicOutput)
                {
                    zipEntry.LastWriteTime = new System.DateTimeOffset(1980, 1, 1, 0, 0, 0, System.TimeSpan.Zero);
                }

                using (Stream entryStream = zipEntry.Open())
                {
                    entryStream.Write(entry.Data, 0, entry.Data.Length);
                }

                result.EntryCount = result.EntryCount + 1;
                result.TotalUncompressedBytes = result.TotalUncompressedBytes + entry.Data.Length;
            }
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static List<XlsxPackageEntry> NormalizeEntriesForWrite(List<XlsxPackageEntry> entries, XlsxZipPackageWriteResult result)
    {
        List<XlsxPackageEntry> normalizedEntries = new List<XlsxPackageEntry>();
        Dictionary<string, bool> seenPaths = new Dictionary<string, bool>();
        foreach (XlsxPackageEntry sourceEntry in entries)
        {
            if (sourceEntry == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "writer.entryNull", "Package entry must not be null."));
                continue;
            }

            string sourcePath = sourceEntry.Path;
            if (sourcePath == null || sourcePath.Length == 0)
            {
                sourcePath = sourceEntry.ZipEntryName;
            }

            XlsxPackagePathResult pathResult = XlsxPackagePath.NormalizePartPath(sourcePath);
            foreach (XlsxDiagnostic diagnostic in pathResult.Validation.Diagnostics)
            {
                diagnostic.SetLocation(sourcePath, "", "");
                result.AddDiagnostic(diagnostic);
            }

            if (!pathResult.Validation.IsValid)
            {
                continue;
            }

            if (seenPaths.ContainsKey(pathResult.Path))
            {
                XlsxDiagnostic duplicateDiagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "writer.duplicateEntry", "Package contains duplicate part entries.");
                duplicateDiagnostic.SetLocation(pathResult.Path, "", "");
                result.AddDiagnostic(duplicateDiagnostic);
                continue;
            }

            seenPaths.Add(pathResult.Path, true);

            XlsxPackageEntry normalizedEntry = new XlsxPackageEntry();
            normalizedEntry.Path = pathResult.Path;
            normalizedEntry.ZipEntryName = XlsxPackagePath.GetZipEntryName(pathResult.Path);
            if (sourceEntry.Data == null)
            {
                normalizedEntry.Data = new byte[0];
            }
            else
            {
                normalizedEntry.Data = sourceEntry.Data;
            }

            normalizedEntry.UncompressedBytes = normalizedEntry.Data.Length;
            normalizedEntries.Add(normalizedEntry);
        }

        return normalizedEntries;
    }

    internal static int CompareEntriesByPath(XlsxPackageEntry left, XlsxPackageEntry right)
    {
        return string.Compare(left.Path, right.Path, System.StringComparison.Ordinal);
    }
}

internal class XlsxXmlReadLimitResult
{
    internal bool IsValid;
    internal XlsxValidationResult Validation;
    internal int MaxObservedDepth;
    internal int MaxObservedTokenCharacters;
    internal int MaxObservedAttributes;
    internal long TotalObservedTextCharacters;

    internal XlsxXmlReadLimitResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        MaxObservedDepth = 0;
        MaxObservedTokenCharacters = 0;
        MaxObservedAttributes = 0;
        TotalObservedTextCharacters = 0;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}

internal class XlsxXmlReaderHelpers
{
    internal static XmlReaderSettings CreateSafeReaderSettings()
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.IgnoreComments = false;
        settings.IgnoreWhitespace = true;
        return settings;
    }

    internal static XmlReader CreateSafeReader(Stream stream, XlsxReaderOptions options)
    {
        return XmlReader.Create(stream, CreateSafeReaderSettings());
    }

    internal static XlsxXmlReadLimitResult ValidateStringLimits(string xml, XlsxReaderOptions options, string partPath)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        return ValidateBytesLimits(bytes, options, partPath);
    }

    internal static XlsxXmlReadLimitResult ValidateBytesLimits(byte[] bytes, XlsxReaderOptions options, string partPath)
    {
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ValidateStreamLimits(stream, options, partPath);
        }
    }

    internal static XlsxXmlReadLimitResult ValidateStreamLimits(Stream stream, XlsxReaderOptions options, string partPath)
    {
        XlsxReaderOptions readerOptions = options;
        if (readerOptions == null)
        {
            readerOptions = new XlsxReaderOptions();
        }

        XlsxXmlReadLimitResult result = new XlsxXmlReadLimitResult();
        XlsxValidationResult optionValidation = readerOptions.Validate();
        foreach (XlsxDiagnostic diagnostic in optionValidation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        if (!result.IsValid)
        {
            return result;
        }

        bool depthReported = false;
        bool tokenReported = false;
        bool attributesReported = false;
        bool totalTextReported = false;
        using (XmlReader reader = CreateSafeReader(stream, readerOptions))
        {
            while (reader.Read())
            {
                int tokenCharacters = MeasureCurrentTokenCharacters(reader);
                if (reader.NodeType == XmlNodeType.Element && reader.Depth > result.MaxObservedDepth)
                {
                    result.MaxObservedDepth = reader.Depth;
                }

                if (tokenCharacters > result.MaxObservedTokenCharacters)
                {
                    result.MaxObservedTokenCharacters = tokenCharacters;
                }

                if (reader.NodeType == XmlNodeType.Element && reader.AttributeCount > result.MaxObservedAttributes) { result.MaxObservedAttributes = reader.AttributeCount; }
                if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA) { result.TotalObservedTextCharacters = result.TotalObservedTextCharacters + reader.Value.Length; }

                if (!depthReported && reader.NodeType == XmlNodeType.Element && reader.Depth > readerOptions.MaxXmlDepth)
                {
                    XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "xml.maxDepth", "XML depth exceeds the configured limit.");
                    diagnostic.SetLocation(partPath, reader.LocalName, "");
                    result.AddDiagnostic(diagnostic);
                    depthReported = true;
                }

                if (!tokenReported && tokenCharacters > readerOptions.MaxXmlTokenCharacters)
                {
                    XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "xml.maxTokenCharacters", "XML token size exceeds the configured limit.");
                    diagnostic.SetLocation(partPath, reader.LocalName, "");
                    result.AddDiagnostic(diagnostic);
                    tokenReported = true;
                }

                if (!attributesReported && reader.NodeType == XmlNodeType.Element && reader.AttributeCount > readerOptions.SecurityOptions.MaxXmlAttributesPerElement)
                {
                    XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "xml.maxAttributes", "XML attribute count exceeds the configured limit.");
                    diagnostic.SetLocation(partPath, reader.LocalName, "");
                    result.AddDiagnostic(diagnostic);
                    attributesReported = true;
                }

                if (!totalTextReported && result.TotalObservedTextCharacters > readerOptions.SecurityOptions.MaxXmlTotalTextCharacters)
                {
                    XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "xml.maxTotalTextCharacters", "XML total text size exceeds the configured limit.");
                    diagnostic.SetLocation(partPath, reader.LocalName, "");
                    result.AddDiagnostic(diagnostic);
                    totalTextReported = true;
                }
            }
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static int MeasureCurrentTokenCharacters(XmlReader reader)
    {
        int count = 0;
        if (reader.NodeType == XmlNodeType.Element || reader.NodeType == XmlNodeType.EndElement)
        {
            count = count + reader.Name.Length;
        }

        if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA || reader.NodeType == XmlNodeType.Comment || reader.NodeType == XmlNodeType.ProcessingInstruction)
        {
            count = count + reader.Value.Length;
        }

        if (reader.NodeType == XmlNodeType.Element && reader.HasAttributes)
        {
            while (reader.MoveToNextAttribute())
            {
                count = count + reader.Name.Length;
                count = count + reader.Value.Length;
            }

            reader.MoveToElement();
        }

        return count;
    }
}

internal class XlsxRelationshipTypeCompatibility
{
    internal const string TransitionalPrefix = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/";
    internal const string StrictPrefix = "http://purl.oclc.org/ooxml/officeDocument/relationships/";

    internal static string NormalizeRelationshipType(string? relationshipType)
    {
        string value = XlsxCellValueConverter.NormalizeText(relationshipType);
        if (value.StartsWith(StrictPrefix, StringComparison.Ordinal))
        {
            return TransitionalPrefix + value.Substring(StrictPrefix.Length);
        }
        return value;
    }

    internal static bool MatchesRelationshipType(string? actualType, string? expectedType)
    {
        return NormalizeRelationshipType(actualType) == NormalizeRelationshipType(expectedType);
    }

    internal static string? GetRelationshipAttribute(XmlReader reader, string localName)
    {
        string? value = reader.GetAttribute(localName, XlsxNamespaces.OfficeDocumentRelationships);
        if (value != null)
        {
            return value;
        }
        return reader.GetAttribute(localName, XlsxNamespaces.StrictOfficeDocumentRelationships);
    }

    internal static bool IsRelationshipNamespace(string? namespaceUri)
    {
        return namespaceUri == XlsxNamespaces.OfficeDocumentRelationships || namespaceUri == XlsxNamespaces.StrictOfficeDocumentRelationships;
    }
}

internal class XlsxXmlNamespace
{
    internal string Prefix;
    internal string NamespaceUri;

    internal XlsxXmlNamespace()
    {
        Prefix = "";
        NamespaceUri = "";
    }
}

internal class XlsxXmlNamespaceMap
{
    internal List<XlsxXmlNamespace> Namespaces;

    internal XlsxXmlNamespaceMap()
    {
        Namespaces = new List<XlsxXmlNamespace>();
    }

    internal void AddNamespace(string prefix, string namespaceUri)
    {
        if (ContainsNamespace(namespaceUri))
        {
            return;
        }

        XlsxXmlNamespace item = new XlsxXmlNamespace();
        item.Prefix = prefix;
        item.NamespaceUri = namespaceUri;
        Namespaces.Add(item);
    }

    internal string RequirePrefix(string namespaceUri, string preferredPrefix)
    {
        if (ContainsNamespace(namespaceUri))
        {
            return GetPrefix(namespaceUri);
        }

        string prefix = preferredPrefix;
        if (prefix == null)
        {
            prefix = "";
        }

        if (!IsPrefixUsedByDifferentNamespace(prefix, namespaceUri))
        {
            AddNamespace(prefix, namespaceUri);
            return prefix;
        }

        int suffix = 1;
        while (true)
        {
            string candidate = prefix + suffix.ToString();
            if (!IsPrefixUsedByDifferentNamespace(candidate, namespaceUri))
            {
                AddNamespace(candidate, namespaceUri);
                return candidate;
            }

            suffix = suffix + 1;
        }
    }

    internal string GetPrefix(string namespaceUri)
    {
        foreach (XlsxXmlNamespace item in Namespaces)
        {
            if (item.NamespaceUri == namespaceUri)
            {
                return item.Prefix;
            }
        }

        return "";
    }

    internal bool ContainsNamespace(string namespaceUri)
    {
        foreach (XlsxXmlNamespace item in Namespaces)
        {
            if (item.NamespaceUri == namespaceUri)
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsPrefixUsedByDifferentNamespace(string prefix, string namespaceUri)
    {
        foreach (XlsxXmlNamespace item in Namespaces)
        {
            if (item.Prefix == prefix && item.NamespaceUri != namespaceUri)
            {
                return true;
            }
        }

        return false;
    }
}

internal class XlsxXmlWriterHelpers
{
    internal static XmlWriterSettings CreateStableWriterSettings()
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Encoding = new UTF8Encoding(false);
        settings.OmitXmlDeclaration = false;
        settings.Indent = false;
        settings.CloseOutput = false;
        return settings;
    }

    internal static XlsxXmlNamespaceMap CreateOpenXmlNamespaceMap()
    {
        XlsxXmlNamespaceMap map = new XlsxXmlNamespaceMap();
        map.AddNamespace("r", XlsxNamespaces.OfficeDocumentRelationships);
        map.AddNamespace("mc", XlsxNamespaces.MarkupCompatibility);
        map.AddNamespace("a", XlsxNamespaces.Drawing);
        map.AddNamespace("xdr", XlsxNamespaces.SpreadsheetDrawing);
        map.AddNamespace("c", XlsxNamespaces.Chart);
        return map;
    }

    internal static void WriteNamespaceDeclarations(XmlWriter writer, XlsxXmlNamespaceMap map)
    {
        List<XlsxXmlNamespace> namespaces = CopyNamespaces(map);
        namespaces.Sort(CompareNamespacesByPrefix);
        foreach (XlsxXmlNamespace item in namespaces)
        {
            if (item.Prefix == null || item.Prefix.Length == 0)
            {
                writer.WriteAttributeString("xmlns", item.NamespaceUri);
            }
            else
            {
                writer.WriteAttributeString("xmlns", item.Prefix, "http://www.w3.org/2000/xmlns/", item.NamespaceUri);
            }
        }
    }

    internal static void WriteStartElement(XmlWriter writer, XlsxXmlNamespaceMap map, string namespaceUri, string localName)
    {
        string prefix = "";
        if (namespaceUri != null && namespaceUri.Length > 0)
        {
            prefix = map.GetPrefix(namespaceUri);
            if (prefix == "" && map.IsPrefixUsedByDifferentNamespace("", namespaceUri))
            {
                prefix = map.RequirePrefix(namespaceUri, "ns");
            }

            if (prefix == "" && !map.IsPrefixUsedByDifferentNamespace("", namespaceUri))
            {
                map.AddNamespace("", namespaceUri);
            }
        }

        writer.WriteStartElement(prefix, localName, namespaceUri);
    }

    internal static void WriteAttributeString(XmlWriter writer, XlsxXmlNamespaceMap map, string namespaceUri, string preferredPrefix, string localName, string value)
    {
        if (namespaceUri == null || namespaceUri.Length == 0)
        {
            writer.WriteAttributeString(localName, value);
            return;
        }

        string prefix = map.RequirePrefix(namespaceUri, preferredPrefix);
        writer.WriteAttributeString(prefix, localName, namespaceUri, value);
    }

    internal static List<XlsxXmlNamespace> CopyNamespaces(XlsxXmlNamespaceMap map)
    {
        List<XlsxXmlNamespace> namespaces = new List<XlsxXmlNamespace>();
        foreach (XlsxXmlNamespace item in map.Namespaces)
        {
            namespaces.Add(item);
        }

        return namespaces;
    }

    internal static int CompareNamespacesByPrefix(XlsxXmlNamespace left, XlsxXmlNamespace right)
    {
        return string.Compare(left.Prefix, right.Prefix, System.StringComparison.Ordinal);
    }
}

/// <summary>
/// Represents a1 column name converter data used by the public workbook API.
/// </summary>
internal static class XlsxA1ColumnNameConverter
{
    /// <summary>
    /// Parses the supplied text into the corresponding typed model without using the current culture.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1ColumnNameResult Parse(string text)
    {
        if (text == null)
        {
            return Parse(System.ReadOnlySpan<char>.Empty);
        }

        System.ReadOnlySpan<char> span = text;
        return Parse(span);
    }

    /// <summary>
    /// Parses the supplied text into the corresponding typed model without using the current culture.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1ColumnNameResult Parse(System.ReadOnlySpan<char> text)
    {
        XlsxA1ColumnNameResult result = ParseLeading(text);
        if (!result.IsValid)
        {
            return result;
        }

        if (result.CharactersConsumed != text.Length)
        {
            result.AddError("a1.columnName.invalid", "A1 column name must contain only letters.");
        }

        return result;
    }

    /// <summary>
    /// Parses a valid value at the beginning of the supplied text and reports how many characters were consumed.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1ColumnNameResult ParseLeading(System.ReadOnlySpan<char> text)
    {
        XlsxA1ColumnNameResult result = new XlsxA1ColumnNameResult();
        if (text.Length == 0)
        {
            result.AddError("a1.columnName.empty", "A1 column name must not be empty.");
            return result;
        }

        int index = 0;
        int column = 0;
        while (index < text.Length && XlsxXmlValueParser.IsAsciiLetter(text[index]))
        {
            int letter = XlsxXmlValueParser.GetUpperAsciiLetterNumber(text[index]);
            if (column > (XlsxXmlValueParser.MaxSpreadsheetColumns - letter) / 26)
            {
                result.AddError("a1.columnName.range", "A1 column name exceeds the worksheet column limit.");
                return result;
            }

            column = column * 26 + letter;
            index = index + 1;
        }

        if (index == 0)
        {
            result.AddError("a1.columnName.invalid", "A1 column name must start with a letter.");
            return result;
        }

        if (column <= 0 || column > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            result.AddError("a1.columnName.range", "A1 column name exceeds the worksheet column limit.");
            return result;
        }

        result.Column = column;
        result.CharactersConsumed = index;
        result.Text = FormatString(column);
        return result;
    }

    /// <summary>
    /// Performs the format operation using the current model state.
    /// </summary>
    /// <param name="column">The column value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1ColumnNameResult Format(int column)
    {
        int length = GetFormattedLength(column);
        if (length == 0)
        {
            XlsxA1ColumnNameResult result = new XlsxA1ColumnNameResult();
            result.AddError("a1.columnName.range", "A1 column index must be between 1 and the worksheet column limit.");
            return result;
        }

        char[] buffer = new char[length];
        System.Span<char> destination = buffer;
        return Format(column, destination);
    }

    /// <summary>
    /// Performs the format operation using the current model state.
    /// </summary>
    /// <param name="column">The column value used by the operation.</param>
    /// <param name="destination">The destination value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1ColumnNameResult Format(int column, System.Span<char> destination)
    {
        XlsxA1ColumnNameResult result = new XlsxA1ColumnNameResult();
        int length = GetFormattedLength(column);
        if (length == 0)
        {
            result.AddError("a1.columnName.range", "A1 column index must be between 1 and the worksheet column limit.");
            return result;
        }

        if (destination.Length < length)
        {
            result.BufferTooSmall = true;
            result.AddError("a1.columnName.bufferTooSmall", "A1 column name destination buffer is too small.");
            return result;
        }

        int current = column;
        int index = length - 1;
        while (index >= 0)
        {
            current = current - 1;
            destination[index] = (char)('A' + current % 26);
            current = current / 26;
            index = index - 1;
        }

        result.Column = column;
        result.CharactersWritten = length;
        result.Text = new string(destination.Slice(0, length));
        return result;
    }

    /// <summary>
    /// Performs the format string operation using the current model state.
    /// </summary>
    /// <param name="column">The column value used by the operation.</param>
    /// <returns>The text produced by the operation.</returns>
    internal static string FormatString(int column)
    {
        XlsxA1ColumnNameResult result = Format(column);
        if (!result.IsValid)
        {
            return "";
        }

        return result.Text;
    }

    internal static int GetFormattedLength(int column)
    {
        if (column <= 0 || column > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            return 0;
        }

        int length = 0;
        int current = column;
        while (current > 0)
        {
            length = length + 1;
            current = (current - 1) / 26;
        }

        return length;
    }
}

/// <summary>
/// Represents a1 cell reference converter data used by the public workbook API.
/// </summary>
internal static class XlsxA1CellReferenceConverter
{
    /// <summary>
    /// Parses the supplied text into the corresponding typed model without using the current culture.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1CellReferenceResult Parse(string? text)
    {
        if (text == null)
        {
            return Parse(System.ReadOnlySpan<char>.Empty);
        }

        System.ReadOnlySpan<char> span = text;
        return Parse(span);
    }

    /// <summary>
    /// Parses the supplied text into the corresponding typed model without using the current culture.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1CellReferenceResult Parse(System.ReadOnlySpan<char> text)
    {
        XlsxA1CellReferenceResult result = ParseLeading(text);
        if (!result.IsValid)
        {
            return result;
        }

        if (result.CharactersConsumed != text.Length)
        {
            result.AddError("a1.cellReference.invalid", "A1 cell reference contains trailing characters.");
        }

        return result;
    }

    /// <summary>
    /// Parses a valid value at the beginning of the supplied text and reports how many characters were consumed.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1CellReferenceResult ParseLeading(System.ReadOnlySpan<char> text)
    {
        XlsxA1CellReferenceResult result = new XlsxA1CellReferenceResult();
        if (text.Length == 0)
        {
            result.AddError("a1.cellReference.empty", "A1 cell reference must not be empty.");
            return result;
        }

        int index = 0;
        bool absoluteColumn = false;
        bool absoluteRow = false;
        if (text[index] == '$')
        {
            absoluteColumn = true;
            index = index + 1;
        }

        int column = ReadColumnName(result, text, index);
        if (!result.IsValid)
        {
            return result;
        }

        index = index + result.CharactersConsumed;
        if (index < text.Length && text[index] == '$')
        {
            absoluteRow = true;
            index = index + 1;
        }

        int rowStart = index;
        int row = ReadRowNumber(result, text, index);
        if (!result.IsValid)
        {
            return result;
        }

        index = index + (result.CharactersConsumed - rowStart);
        SetReference(result, row, column, absoluteRow, absoluteColumn, index);
        return result;
    }

    internal static int ReadColumnName(XlsxA1CellReferenceResult result, System.ReadOnlySpan<char> text, int start)
    {
        int index = start;
        int column = 0;
        while (index < text.Length && XlsxXmlValueParser.IsAsciiLetter(text[index]))
        {
            int letter = XlsxXmlValueParser.GetUpperAsciiLetterNumber(text[index]);
            if (column > (XlsxXmlValueParser.MaxSpreadsheetColumns - letter) / 26)
            {
                result.AddError("a1.cellReference.columnRange", "A1 cell reference column exceeds the worksheet column limit.");
                return 0;
            }

            column = column * 26 + letter;
            index = index + 1;
        }

        if (index == start)
        {
            result.AddError("a1.cellReference.column", "A1 cell reference must contain a column name.");
            return 0;
        }

        if (column <= 0 || column > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            result.AddError("a1.cellReference.columnRange", "A1 cell reference column exceeds the worksheet column limit.");
            return 0;
        }

        result.CharactersConsumed = index - start;
        return column;
    }

    internal static int ReadRowNumber(XlsxA1CellReferenceResult result, System.ReadOnlySpan<char> text, int start)
    {
        int index = start;
        int row = 0;
        while (index < text.Length && XlsxXmlValueParser.IsAsciiDigit(text[index]))
        {
            int digit = text[index] - '0';
            if (row > (XlsxXmlValueParser.MaxSpreadsheetRows - digit) / 10)
            {
                result.AddError("a1.cellReference.rowRange", "A1 cell reference row exceeds the worksheet row limit.");
                return 0;
            }

            row = row * 10 + digit;
            index = index + 1;
        }

        if (index == start)
        {
            result.AddError("a1.cellReference.row", "A1 cell reference must contain a row number.");
            return 0;
        }

        if (row <= 0 || row > XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            result.AddError("a1.cellReference.rowRange", "A1 cell reference row must be within the worksheet row limit.");
            return 0;
        }

        result.CharactersConsumed = index;
        return row;
    }

    internal static void SetReference(XlsxA1CellReferenceResult result, int row, int column, bool absoluteRow, bool absoluteColumn, int charactersConsumed)
    {
        result.Row = row;
        result.Column = column;
        result.IsAbsoluteRow = absoluteRow;
        result.IsAbsoluteColumn = absoluteColumn;
        result.CharactersConsumed = charactersConsumed;
        result.Reference.Row = row;
        result.Reference.Column = column;
        result.Reference.IsAbsoluteRow = absoluteRow;
        result.Reference.IsAbsoluteColumn = absoluteColumn;
        result.Text = FormatString(row, column, absoluteRow, absoluteColumn);
        result.Reference.Text = result.Text;
    }

    /// <summary>
    /// Performs the format operation using the current model state.
    /// </summary>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1CellReferenceResult Format(XlsxCellReferenceValue value)
    {
        if (value == null)
        {
            XlsxA1CellReferenceResult result = new XlsxA1CellReferenceResult();
            result.AddError("a1.cellReference.null", "A1 cell reference value must not be null.");
            return result;
        }

        return Format(value.Row, value.Column, value.IsAbsoluteRow, value.IsAbsoluteColumn);
    }

    /// <summary>
    /// Performs the format operation using the current model state.
    /// </summary>
    /// <param name="row">The row value used by the operation.</param>
    /// <param name="column">The column value used by the operation.</param>
    /// <param name="absoluteRow">The absolute row value used by the operation.</param>
    /// <param name="absoluteColumn">The absolute column value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1CellReferenceResult Format(int row, int column, bool absoluteRow, bool absoluteColumn)
    {
        int length = GetFormattedLength(row, column, absoluteRow, absoluteColumn);
        if (length == 0)
        {
            XlsxA1CellReferenceResult result = new XlsxA1CellReferenceResult();
            result.AddError("a1.cellReference.range", "A1 cell reference row and column must be within worksheet limits.");
            return result;
        }

        char[] buffer = new char[length];
        System.Span<char> destination = buffer;
        return Format(row, column, absoluteRow, absoluteColumn, destination);
    }

    /// <summary>
    /// Performs the format operation using the current model state.
    /// </summary>
    /// <param name="row">The row value used by the operation.</param>
    /// <param name="column">The column value used by the operation.</param>
    /// <param name="absoluteRow">The absolute row value used by the operation.</param>
    /// <param name="absoluteColumn">The absolute column value used by the operation.</param>
    /// <param name="destination">The destination value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1CellReferenceResult Format(int row, int column, bool absoluteRow, bool absoluteColumn, System.Span<char> destination)
    {
        XlsxA1CellReferenceResult result = new XlsxA1CellReferenceResult();
        int length = GetFormattedLength(row, column, absoluteRow, absoluteColumn);
        if (length == 0)
        {
            result.AddError("a1.cellReference.range", "A1 cell reference row and column must be within worksheet limits.");
            return result;
        }

        if (destination.Length < length)
        {
            result.BufferTooSmall = true;
            result.AddError("a1.cellReference.bufferTooSmall", "A1 cell reference destination buffer is too small.");
            return result;
        }

        int index = 0;
        if (absoluteColumn)
        {
            destination[index] = '$';
            index = index + 1;
        }

        XlsxA1ColumnNameResult columnResult = XlsxA1ColumnNameConverter.Format(column, destination.Slice(index));
        if (!columnResult.IsValid)
        {
            result.AddError("a1.cellReference.columnRange", "A1 cell reference column exceeds the worksheet column limit.");
            return result;
        }

        index = index + columnResult.CharactersWritten;
        if (absoluteRow)
        {
            destination[index] = '$';
            index = index + 1;
        }

        index = WriteRowNumber(row, destination, index);
        result.Row = row;
        result.Column = column;
        result.IsAbsoluteRow = absoluteRow;
        result.IsAbsoluteColumn = absoluteColumn;
        result.Reference.Row = row;
        result.Reference.Column = column;
        result.Reference.IsAbsoluteRow = absoluteRow;
        result.Reference.IsAbsoluteColumn = absoluteColumn;
        result.CharactersWritten = length;
        result.Text = new string(destination.Slice(0, length));
        result.Reference.Text = result.Text;
        return result;
    }

    /// <summary>
    /// Performs the format string operation using the current model state.
    /// </summary>
    /// <param name="row">The row value used by the operation.</param>
    /// <param name="column">The column value used by the operation.</param>
    /// <param name="absoluteRow">The absolute row value used by the operation.</param>
    /// <param name="absoluteColumn">The absolute column value used by the operation.</param>
    /// <returns>The text produced by the operation.</returns>
    internal static string FormatString(int row, int column, bool absoluteRow, bool absoluteColumn)
    {
        XlsxA1CellReferenceResult result = Format(row, column, absoluteRow, absoluteColumn);
        if (!result.IsValid)
        {
            return "";
        }

        return result.Text;
    }

    internal static int GetFormattedLength(int row, int column, bool absoluteRow, bool absoluteColumn)
    {
        if (row <= 0 || row > XlsxXmlValueParser.MaxSpreadsheetRows || column <= 0 || column > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            return 0;
        }

        int length = XlsxA1ColumnNameConverter.GetFormattedLength(column) + GetRowNumberLength(row);
        if (absoluteRow)
        {
            length = length + 1;
        }

        if (absoluteColumn)
        {
            length = length + 1;
        }

        return length;
    }

    internal static int GetRowNumberLength(int row)
    {
        if (row <= 0)
        {
            return 0;
        }

        int length = 0;
        int current = row;
        while (current > 0)
        {
            length = length + 1;
            current = current / 10;
        }

        return length;
    }

    internal static int WriteRowNumber(int row, System.Span<char> destination, int start)
    {
        int length = GetRowNumberLength(row);
        int index = start + length - 1;
        int current = row;
        while (index >= start)
        {
            destination[index] = (char)('0' + current % 10);
            current = current / 10;
            index = index - 1;
        }

        return start + length;
    }
}

/// <summary>
/// Represents a1 range reference converter data used by the public workbook API.
/// </summary>
internal static class XlsxA1RangeReferenceConverter
{
    /// <summary>
    /// Parses the supplied text into the corresponding typed model without using the current culture.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1RangeReferenceResult Parse(string? text)
    {
        if (text == null)
        {
            return Parse(System.ReadOnlySpan<char>.Empty);
        }

        System.ReadOnlySpan<char> span = text;
        return Parse(span);
    }

    /// <summary>
    /// Parses the supplied text into the corresponding typed model without using the current culture.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1RangeReferenceResult Parse(System.ReadOnlySpan<char> text)
    {
        XlsxA1RangeReferenceResult result = ParseLeading(text);
        if (!result.IsValid)
        {
            return result;
        }

        if (result.CharactersConsumed != text.Length)
        {
            result.AddError("a1.rangeReference.invalid", "A1 range reference contains trailing characters.");
        }

        return result;
    }

    /// <summary>
    /// Parses a valid value at the beginning of the supplied text and reports how many characters were consumed.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1RangeReferenceResult ParseLeading(System.ReadOnlySpan<char> text)
    {
        XlsxA1RangeReferenceResult result = new XlsxA1RangeReferenceResult();
        if (text.Length == 0)
        {
            result.AddError("a1.rangeReference.empty", "A1 range reference must not be empty.");
            return result;
        }

        XlsxA1CellReferenceResult first = XlsxA1CellReferenceConverter.ParseLeading(text);
        if (!first.IsValid)
        {
            AddCellDiagnostics(result, first);
            return result;
        }

        CopyReference(first.Reference, result.First);
        int index = first.CharactersConsumed;
        bool hasLast = false;
        if (index < text.Length && text[index] == ':')
        {
            hasLast = true;
            index = index + 1;
            XlsxA1CellReferenceResult last = XlsxA1CellReferenceConverter.ParseLeading(text.Slice(index));
            if (!last.IsValid)
            {
                AddCellDiagnostics(result, last);
                return result;
            }

            CopyReference(last.Reference, result.Last);
            index = index + last.CharactersConsumed;
        }

        SetRange(result, hasLast, index);
        return result;
    }

    internal static void AddCellDiagnostics(XlsxA1RangeReferenceResult result, XlsxA1CellReferenceResult cellResult)
    {
        foreach (XlsxDiagnostic diagnostic in cellResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyReference(XlsxCellReferenceValue source, XlsxCellReferenceValue target)
    {
        target.Text = source.Text;
        target.Row = source.Row;
        target.Column = source.Column;
        target.IsAbsoluteRow = source.IsAbsoluteRow;
        target.IsAbsoluteColumn = source.IsAbsoluteColumn;
    }

    internal static void SetRange(XlsxA1RangeReferenceResult result, bool hasLast, int charactersConsumed)
    {
        result.HasLast = hasLast;
        result.Range.HasLast = hasLast;
        result.CharactersConsumed = charactersConsumed;
        if (!hasLast)
        {
            CopyReference(result.First, result.Last);
            result.Text = XlsxA1CellReferenceConverter.FormatString(result.First.Row, result.First.Column, result.First.IsAbsoluteRow, result.First.IsAbsoluteColumn);
        }
        else
        {
            result.Text = FormatString(result.Range);
        }

        result.Range.Text = result.Text;
    }

    /// <summary>
    /// Performs the format operation using the current model state.
    /// </summary>
    /// <param name="range">The range value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1RangeReferenceResult Format(XlsxA1RangeReferenceValue range)
    {
        if (range == null)
        {
            XlsxA1RangeReferenceResult result = new XlsxA1RangeReferenceResult();
            result.AddError("a1.rangeReference.null", "A1 range reference value must not be null.");
            return result;
        }

        int length = GetFormattedLength(range);
        if (length == 0)
        {
            XlsxA1RangeReferenceResult result = new XlsxA1RangeReferenceResult();
            result.AddError("a1.rangeReference.range", "A1 range reference endpoints must be within worksheet limits.");
            return result;
        }

        char[] buffer = new char[length];
        System.Span<char> destination = buffer;
        return Format(range, destination);
    }

    /// <summary>
    /// Performs the format operation using the current model state.
    /// </summary>
    /// <param name="range">The range value used by the operation.</param>
    /// <param name="destination">The destination value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxA1RangeReferenceResult Format(XlsxA1RangeReferenceValue range, System.Span<char> destination)
    {
        XlsxA1RangeReferenceResult result = new XlsxA1RangeReferenceResult();
        if (range == null)
        {
            result.AddError("a1.rangeReference.null", "A1 range reference value must not be null.");
            return result;
        }

        int length = GetFormattedLength(range);
        if (length == 0)
        {
            result.AddError("a1.rangeReference.range", "A1 range reference endpoints must be within worksheet limits.");
            return result;
        }

        if (destination.Length < length)
        {
            result.BufferTooSmall = true;
            result.AddError("a1.rangeReference.bufferTooSmall", "A1 range reference destination buffer is too small.");
            return result;
        }

        int index = 0;
        XlsxA1CellReferenceResult first = XlsxA1CellReferenceConverter.Format(range.First.Row, range.First.Column, range.First.IsAbsoluteRow, range.First.IsAbsoluteColumn, destination.Slice(index));
        if (!first.IsValid)
        {
            result.AddError("a1.rangeReference.range", "A1 range reference first endpoint is invalid.");
            return result;
        }

        index = index + first.CharactersWritten;
        CopyReference(first.Reference, result.First);
        if (range.HasLast)
        {
            destination[index] = ':';
            index = index + 1;
            XlsxA1CellReferenceResult last = XlsxA1CellReferenceConverter.Format(range.Last.Row, range.Last.Column, range.Last.IsAbsoluteRow, range.Last.IsAbsoluteColumn, destination.Slice(index));
            if (!last.IsValid)
            {
                result.AddError("a1.rangeReference.range", "A1 range reference last endpoint is invalid.");
                return result;
            }

            index = index + last.CharactersWritten;
            CopyReference(last.Reference, result.Last);
        }
        else
        {
            CopyReference(result.First, result.Last);
        }

        result.HasLast = range.HasLast;
        result.Range.HasLast = range.HasLast;
        result.CharactersWritten = length;
        result.Text = new string(destination.Slice(0, length));
        result.Range.Text = result.Text;
        CopyReference(result.First, result.Range.First);
        CopyReference(result.Last, result.Range.Last);
        return result;
    }

    /// <summary>
    /// Performs the format string operation using the current model state.
    /// </summary>
    /// <param name="range">The range value used by the operation.</param>
    /// <returns>The text produced by the operation.</returns>
    internal static string FormatString(XlsxA1RangeReferenceValue range)
    {
        XlsxA1RangeReferenceResult result = Format(range);
        if (!result.IsValid)
        {
            return "";
        }

        return result.Text;
    }

    internal static int GetFormattedLength(XlsxA1RangeReferenceValue range)
    {
        if (range == null || range.First == null)
        {
            return 0;
        }

        int firstLength = XlsxA1CellReferenceConverter.GetFormattedLength(range.First.Row, range.First.Column, range.First.IsAbsoluteRow, range.First.IsAbsoluteColumn);
        if (firstLength == 0)
        {
            return 0;
        }

        if (!range.HasLast)
        {
            return firstLength;
        }

        if (range.Last == null)
        {
            return 0;
        }

        int lastLength = XlsxA1CellReferenceConverter.GetFormattedLength(range.Last.Row, range.Last.Column, range.Last.IsAbsoluteRow, range.Last.IsAbsoluteColumn);
        if (lastLength == 0)
        {
            return 0;
        }

        return firstLength + 1 + lastLength;
    }
}

/// <summary>
/// Represents r1 c1 reference converter data used by the public workbook API.
/// </summary>
internal static class XlsxR1C1ReferenceConverter
{
    /// <summary>
    /// Parses the supplied text into the corresponding typed model without using the current culture.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxR1C1ReferenceResult Parse(string text)
    {
        if (text == null)
        {
            return Parse(System.ReadOnlySpan<char>.Empty);
        }

        System.ReadOnlySpan<char> span = text;
        return Parse(span);
    }

    /// <summary>
    /// Parses the supplied text into the corresponding typed model without using the current culture.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxR1C1ReferenceResult Parse(System.ReadOnlySpan<char> text)
    {
        XlsxR1C1ReferenceResult result = ParseLeading(text);
        if (!result.IsValid)
        {
            return result;
        }

        if (result.CharactersConsumed != text.Length)
        {
            result.AddError("r1c1.reference.invalid", "R1C1 reference contains trailing characters.");
        }

        return result;
    }

    /// <summary>
    /// Parses a valid value at the beginning of the supplied text and reports how many characters were consumed.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxR1C1ReferenceResult ParseLeading(System.ReadOnlySpan<char> text)
    {
        XlsxR1C1ReferenceResult result = new XlsxR1C1ReferenceResult();
        if (text.Length == 0)
        {
            result.AddError("r1c1.reference.empty", "R1C1 reference must not be empty.");
            return result;
        }

        int index = 0;
        if (!IsMarker(text[index], 'R'))
        {
            result.AddError("r1c1.reference.rowMarker", "R1C1 reference must start with R.");
            return result;
        }

        index = index + 1;
        index = ReadAxis(result, text, index, true);
        if (!result.IsValid)
        {
            return result;
        }

        if (index >= text.Length || !IsMarker(text[index], 'C'))
        {
            result.AddError("r1c1.reference.columnMarker", "R1C1 reference must contain a C marker.");
            return result;
        }

        index = index + 1;
        index = ReadAxis(result, text, index, false);
        if (!result.IsValid)
        {
            return result;
        }

        SetReference(result, index);
        return result;
    }

    internal static int ReadAxis(XlsxR1C1ReferenceResult result, System.ReadOnlySpan<char> text, int start, bool rowAxis)
    {
        if (start < text.Length && text[start] == '[')
        {
            return ReadRelativeAxis(result, text, start, rowAxis);
        }

        if (start < text.Length && XlsxXmlValueParser.IsAsciiDigit(text[start]))
        {
            return ReadAbsoluteAxis(result, text, start, rowAxis);
        }

        if (rowAxis)
        {
            result.HasRow = false;
            result.IsRelativeRow = true;
            result.Row = 0;
        }
        else
        {
            result.HasColumn = false;
            result.IsRelativeColumn = true;
            result.Column = 0;
        }

        return start;
    }

    internal static int ReadRelativeAxis(XlsxR1C1ReferenceResult result, System.ReadOnlySpan<char> text, int start, bool rowAxis)
    {
        int index = start + 1;
        int sign = 1;
        if (index < text.Length && (text[index] == '-' || text[index] == '+'))
        {
            if (text[index] == '-')
            {
                sign = -1;
            }

            index = index + 1;
        }

        int digitStart = index;
        int value = 0;
        int limit = GetRelativeLimit(rowAxis);
        while (index < text.Length && XlsxXmlValueParser.IsAsciiDigit(text[index]))
        {
            int digit = text[index] - '0';
            if (value > (limit - digit) / 10)
            {
                AddAxisRangeError(result, rowAxis);
                return index;
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        if (index == digitStart)
        {
            AddAxisInvalidError(result, rowAxis);
            return index;
        }

        if (index >= text.Length || text[index] != ']')
        {
            result.AddError("r1c1.reference.invalid", "R1C1 relative reference must close with ].");
            return index;
        }

        value = value * sign;
        if (rowAxis)
        {
            result.Row = value;
            result.HasRow = true;
            result.IsRelativeRow = true;
        }
        else
        {
            result.Column = value;
            result.HasColumn = true;
            result.IsRelativeColumn = true;
        }

        return index + 1;
    }

    internal static int ReadAbsoluteAxis(XlsxR1C1ReferenceResult result, System.ReadOnlySpan<char> text, int start, bool rowAxis)
    {
        int index = start;
        int value = 0;
        int limit = GetAbsoluteLimit(rowAxis);
        while (index < text.Length && XlsxXmlValueParser.IsAsciiDigit(text[index]))
        {
            int digit = text[index] - '0';
            if (value > (limit - digit) / 10)
            {
                AddAxisRangeError(result, rowAxis);
                return index;
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        if (value <= 0 || value > limit)
        {
            AddAxisRangeError(result, rowAxis);
            return index;
        }

        if (rowAxis)
        {
            result.Row = value;
            result.HasRow = true;
            result.IsRelativeRow = false;
        }
        else
        {
            result.Column = value;
            result.HasColumn = true;
            result.IsRelativeColumn = false;
        }

        return index;
    }

    internal static void SetReference(XlsxR1C1ReferenceResult result, int charactersConsumed)
    {
        result.CharactersConsumed = charactersConsumed;
        result.Reference.Row = result.Row;
        result.Reference.Column = result.Column;
        result.Reference.HasRow = result.HasRow;
        result.Reference.HasColumn = result.HasColumn;
        result.Reference.IsRelativeRow = result.IsRelativeRow;
        result.Reference.IsRelativeColumn = result.IsRelativeColumn;
        result.Text = FormatString(result.Reference);
        result.Reference.Text = result.Text;
    }

    /// <summary>
    /// Performs the format operation using the current model state.
    /// </summary>
    /// <param name="reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxR1C1ReferenceResult Format(XlsxR1C1ReferenceValue reference)
    {
        if (reference == null)
        {
            XlsxR1C1ReferenceResult result = new XlsxR1C1ReferenceResult();
            result.AddError("r1c1.reference.null", "R1C1 reference value must not be null.");
            return result;
        }

        int length = GetFormattedLength(reference);
        if (length == 0)
        {
            XlsxR1C1ReferenceResult result = new XlsxR1C1ReferenceResult();
            result.AddError("r1c1.reference.range", "R1C1 reference values must be within worksheet limits.");
            return result;
        }

        char[] buffer = new char[length];
        System.Span<char> destination = buffer;
        return Format(reference, destination);
    }

    /// <summary>
    /// Performs the format operation using the current model state.
    /// </summary>
    /// <param name="reference">An A1-style cell or range reference.</param>
    /// <param name="destination">The destination value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxR1C1ReferenceResult Format(XlsxR1C1ReferenceValue reference, System.Span<char> destination)
    {
        XlsxR1C1ReferenceResult result = new XlsxR1C1ReferenceResult();
        if (reference == null)
        {
            result.AddError("r1c1.reference.null", "R1C1 reference value must not be null.");
            return result;
        }

        int length = GetFormattedLength(reference);
        if (length == 0)
        {
            result.AddError("r1c1.reference.range", "R1C1 reference values must be within worksheet limits.");
            return result;
        }

        if (destination.Length < length)
        {
            result.BufferTooSmall = true;
            result.AddError("r1c1.reference.bufferTooSmall", "R1C1 reference destination buffer is too small.");
            return result;
        }

        int index = 0;
        destination[index] = 'R';
        index = index + 1;
        index = WriteAxis(reference.Row, reference.HasRow, reference.IsRelativeRow, true, destination, index);
        destination[index] = 'C';
        index = index + 1;
        index = WriteAxis(reference.Column, reference.HasColumn, reference.IsRelativeColumn, false, destination, index);
        CopyReference(reference, result.Reference);
        result.Row = reference.Row;
        result.Column = reference.Column;
        result.HasRow = reference.HasRow;
        result.HasColumn = reference.HasColumn;
        result.IsRelativeRow = reference.IsRelativeRow;
        result.IsRelativeColumn = reference.IsRelativeColumn;
        result.CharactersWritten = length;
        result.Text = new string(destination.Slice(0, length));
        result.Reference.Text = result.Text;
        return result;
    }

    /// <summary>
    /// Performs the format string operation using the current model state.
    /// </summary>
    /// <param name="reference">An A1-style cell or range reference.</param>
    /// <returns>The text produced by the operation.</returns>
    internal static string FormatString(XlsxR1C1ReferenceValue reference)
    {
        XlsxR1C1ReferenceResult result = Format(reference);
        if (!result.IsValid)
        {
            return "";
        }

        return result.Text;
    }

    internal static int WriteAxis(int value, bool hasValue, bool relative, bool rowAxis, System.Span<char> destination, int start)
    {
        int index = start;
        if (!hasValue)
        {
            return index;
        }

        if (relative)
        {
            destination[index] = '[';
            index = index + 1;
            index = WriteSignedInteger(value, destination, index);
            destination[index] = ']';
            index = index + 1;
            return index;
        }

        return WriteUnsignedInteger(value, destination, index);
    }

    internal static int WriteSignedInteger(int value, System.Span<char> destination, int start)
    {
        int index = start;
        int current = value;
        if (value < 0)
        {
            destination[index] = '-';
            index = index + 1;
            current = 0 - value;
        }

        return WriteUnsignedInteger(current, destination, index);
    }

    /// <summary>
    /// Validates and writes unsigned integer in the requested representation.
    /// </summary>
    /// <param name="value">The value value used by the operation.</param>
    /// <param name="destination">The destination value used by the operation.</param>
    /// <param name="start">The start value used by the operation.</param>
    /// <returns>The int value produced by the operation.</returns>
    internal static int WriteUnsignedInteger(int value, System.Span<char> destination, int start)
    {
        int length = GetUnsignedLength(value);
        int index = start + length - 1;
        int current = value;
        while (index >= start)
        {
            destination[index] = (char)('0' + current % 10);
            current = current / 10;
            index = index - 1;
        }

        return start + length;
    }

    internal static int GetFormattedLength(XlsxR1C1ReferenceValue reference)
    {
        if (reference == null)
        {
            return 0;
        }

        if (!IsValidAxis(reference.Row, reference.HasRow, reference.IsRelativeRow, true))
        {
            return 0;
        }

        if (!IsValidAxis(reference.Column, reference.HasColumn, reference.IsRelativeColumn, false))
        {
            return 0;
        }

        return 1 + GetAxisLength(reference.Row, reference.HasRow, reference.IsRelativeRow) + 1 + GetAxisLength(reference.Column, reference.HasColumn, reference.IsRelativeColumn);
    }

    internal static int GetAxisLength(int value, bool hasValue, bool relative)
    {
        if (!hasValue)
        {
            return 0;
        }

        if (relative)
        {
            return GetSignedLength(value) + 2;
        }

        return GetUnsignedLength(value);
    }

    internal static int GetSignedLength(int value)
    {
        if (value < 0)
        {
            return 1 + GetUnsignedLength(0 - value);
        }

        return GetUnsignedLength(value);
    }

    internal static int GetUnsignedLength(int value)
    {
        if (value == 0)
        {
            return 1;
        }

        int length = 0;
        int current = value;
        while (current > 0)
        {
            length = length + 1;
            current = current / 10;
        }

        return length;
    }

    internal static bool IsValidAxis(int value, bool hasValue, bool relative, bool rowAxis)
    {
        if (!hasValue)
        {
            return true;
        }

        int limit = GetRelativeLimit(rowAxis);
        if (!relative)
        {
            limit = GetAbsoluteLimit(rowAxis);
            return value > 0 && value <= limit;
        }

        return value >= 0 - limit && value <= limit;
    }

    internal static int GetAbsoluteLimit(bool rowAxis)
    {
        if (rowAxis)
        {
            return XlsxXmlValueParser.MaxSpreadsheetRows;
        }

        return XlsxXmlValueParser.MaxSpreadsheetColumns;
    }

    internal static int GetRelativeLimit(bool rowAxis)
    {
        return GetAbsoluteLimit(rowAxis);
    }

    internal static void AddAxisRangeError(XlsxR1C1ReferenceResult result, bool rowAxis)
    {
        if (rowAxis)
        {
            result.AddError("r1c1.reference.rowRange", "R1C1 row value is outside the worksheet row limit.");
            return;
        }

        result.AddError("r1c1.reference.columnRange", "R1C1 column value is outside the worksheet column limit.");
    }

    internal static void AddAxisInvalidError(XlsxR1C1ReferenceResult result, bool rowAxis)
    {
        if (rowAxis)
        {
            result.AddError("r1c1.reference.row", "R1C1 relative row value must contain digits.");
            return;
        }

        result.AddError("r1c1.reference.column", "R1C1 relative column value must contain digits.");
    }

    internal static bool IsMarker(char value, char marker)
    {
        if (value == marker)
        {
            return true;
        }

        if (value == marker + 32)
        {
            return true;
        }

        return false;
    }

    internal static void CopyReference(XlsxR1C1ReferenceValue source, XlsxR1C1ReferenceValue target)
    {
        target.Text = source.Text;
        target.Row = source.Row;
        target.Column = source.Column;
        target.HasRow = source.HasRow;
        target.HasColumn = source.HasColumn;
        target.IsRelativeRow = source.IsRelativeRow;
        target.IsRelativeColumn = source.IsRelativeColumn;
    }
}

/// <summary>
/// Represents xml value parser data used by the public workbook API.
/// </summary>
internal static class XlsxXmlValueParser
{
    /// <summary>
    /// Identifies the max spreadsheet rows OOXML value.
    /// </summary>
    internal const int MaxSpreadsheetRows = 1048576;
    /// <summary>
    /// Identifies the max spreadsheet columns OOXML value.
    /// </summary>
    internal const int MaxSpreadsheetColumns = 16384;

    internal static XlsxXmlValueParseResult ParseBoolean(string? text)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        string value = NormalizeValueText(text);
        if (value == "1" || value == "true")
        {
            result.BooleanValue = true;
            return result;
        }

        if (value == "0" || value == "false")
        {
            result.BooleanValue = false;
            return result;
        }

        result.AddError("xml.boolean.invalid", "Boolean XML value must be 0, 1, true, or false.");
        return result;
    }

    internal static XlsxXmlValueParseResult ParseInteger(string text)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        string value = NormalizeValueText(text);
        if (value.Length == 0)
        {
            result.AddError("xml.integer.empty", "Integer XML value must not be empty.");
            return result;
        }

        int index = 0;
        bool negative = false;
        if (value[0] == '-' || value[0] == '+')
        {
            negative = value[0] == '-';
            index = 1;
        }

        if (index >= value.Length)
        {
            result.AddError("xml.integer.invalid", "Integer XML value must contain digits.");
            return result;
        }

        long limit = 2147483647;
        if (negative)
        {
            limit = 2147483648;
        }

        long number = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (!IsAsciiDigit(current))
            {
                result.AddError("xml.integer.invalid", "Integer XML value contains an invalid character.");
                return result;
            }

            number = number * 10 + (current - '0');
            if (number > limit)
            {
                result.AddError("xml.integer.overflow", "Integer XML value is outside the Int32 range.");
                return result;
            }

            index = index + 1;
        }

        if (negative)
        {
            result.IntValue = (int)(0 - number);
        }
        else
        {
            result.IntValue = (int)number;
        }

        return result;
    }

    internal static XlsxXmlValueParseResult ParseUnsignedInteger(string? text)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        string value = NormalizeValueText(text);
        if (value.Length == 0)
        {
            result.AddError("xml.unsignedInteger.empty", "Unsigned integer XML value must not be empty.");
            return result;
        }

        uint number = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (!IsAsciiDigit(current))
            {
                result.AddError("xml.unsignedInteger.invalid", "Unsigned integer XML value contains an invalid character.");
                return result;
            }

            uint digit = (uint)(current - '0');
            if (number > 429496729 || (number == 429496729 && digit > 5))
            {
                result.AddError("xml.unsignedInteger.overflow", "Unsigned integer XML value is outside the UInt32 range.");
                return result;
            }

            number = number * 10 + digit;
            index = index + 1;
        }

        result.UnsignedIntValue = number;
        return result;
    }

    internal static XlsxXmlValueParseResult ParseDouble(string text)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        string value = NormalizeValueText(text);
        if (value.Length == 0)
        {
            result.AddError("xml.double.empty", "Double XML value must not be empty.");
            return result;
        }

        int index = 0;
        double sign = 1;
        if (value[0] == '-' || value[0] == '+')
        {
            if (value[0] == '-')
            {
                sign = -1;
            }

            index = 1;
        }

        double number = 0;
        int digitCount = 0;
        while (index < value.Length && IsAsciiDigit(value[index]))
        {
            number = number * 10 + (value[index] - '0');
            digitCount = digitCount + 1;
            index = index + 1;
        }

        if (index < value.Length && value[index] == '.')
        {
            index = index + 1;
            double scale = 0.1;
            while (index < value.Length && IsAsciiDigit(value[index]))
            {
                number = number + (value[index] - '0') * scale;
                scale = scale * 0.1;
                digitCount = digitCount + 1;
                index = index + 1;
            }
        }

        if (digitCount == 0)
        {
            result.AddError("xml.double.invalid", "Double XML value must contain digits.");
            return result;
        }

        int exponentSign = 1;
        int exponent = 0;
        if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
        {
            index = index + 1;
            if (index < value.Length && (value[index] == '-' || value[index] == '+'))
            {
                if (value[index] == '-')
                {
                    exponentSign = -1;
                }

                index = index + 1;
            }

            int exponentDigits = 0;
            while (index < value.Length && IsAsciiDigit(value[index]))
            {
                if (exponent < 400)
                {
                    exponent = exponent * 10 + (value[index] - '0');
                }

                exponentDigits = exponentDigits + 1;
                index = index + 1;
            }

            if (exponentDigits == 0)
            {
                result.AddError("xml.double.invalid", "Double XML value has an invalid exponent.");
                return result;
            }
        }

        if (index != value.Length)
        {
            result.AddError("xml.double.invalid", "Double XML value contains an invalid character.");
            return result;
        }

        double parsed = sign * number;
        if (exponent != 0)
        {
            parsed = parsed * System.Math.Pow(10, exponentSign * exponent);
        }

        if (double.IsInfinity(parsed) || double.IsNaN(parsed))
        {
            result.AddError("xml.double.overflow", "Double XML value is outside the finite Double range.");
            return result;
        }

        result.DoubleValue = parsed;
        return result;
    }

    internal static XlsxXmlValueParseResult ParseDate(string text)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        string value = NormalizeValueText(text);
        if (value.Length != 10)
        {
            result.AddError("xml.date.invalid", "Date XML value must use yyyy-MM-dd format.");
            return result;
        }

        if (!SetDateValue(result, value, 0, System.DateTimeKind.Unspecified))
        {
            return result;
        }

        return result;
    }

    internal static XlsxXmlValueParseResult ParseDateTime(string text)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        string value = NormalizeValueText(text);
        if (value.Length == 10)
        {
            if (!SetDateValue(result, value, 0, System.DateTimeKind.Unspecified))
            {
                return result;
            }

            return result;
        }

        if (value.Length < 19 || value[10] != 'T')
        {
            result.AddError("xml.dateTime.invalid", "DateTime XML value must use ISO date or date-time format.");
            return result;
        }

        int year = ParseFixedDigits(value, 0, 4);
        int month = ParseFixedDigits(value, 5, 2);
        int day = ParseFixedDigits(value, 8, 2);
        int hour = ParseFixedDigits(value, 11, 2);
        int minute = ParseFixedDigits(value, 14, 2);
        int second = ParseFixedDigits(value, 17, 2);
        if (value[4] != '-' || value[7] != '-' || value[13] != ':' || value[16] != ':' || year < 0 || month < 0 || day < 0 || hour < 0 || minute < 0 || second < 0)
        {
            result.AddError("xml.dateTime.invalid", "DateTime XML value must use ISO date-time separators.");
            return result;
        }

        if (!IsValidDate(year, month, day) || hour > 23 || minute > 59 || second > 59)
        {
            result.AddError("xml.dateTime.range", "DateTime XML value contains an out-of-range date or time component.");
            return result;
        }

        int index = 19;
        int millisecond = 0;
        if (index < value.Length && value[index] == '.')
        {
            index = index + 1;
            int digitIndex = 0;
            if (index >= value.Length || !IsAsciiDigit(value[index]))
            {
                result.AddError("xml.dateTime.invalid", "DateTime XML value has an invalid fractional second.");
                return result;
            }

            while (index < value.Length && IsAsciiDigit(value[index]))
            {
                if (digitIndex < 3)
                {
                    int factor = 100;
                    if (digitIndex == 1)
                    {
                        factor = 10;
                    }

                    if (digitIndex == 2)
                    {
                        factor = 1;
                    }

                    millisecond = millisecond + (value[index] - '0') * factor;
                }

                digitIndex = digitIndex + 1;
                index = index + 1;
            }
        }

        System.DateTimeKind kind = System.DateTimeKind.Unspecified;
        if (index < value.Length)
        {
            if (value[index] == 'Z' && index + 1 == value.Length)
            {
                kind = System.DateTimeKind.Utc;
                index = index + 1;
            }
            else if ((value[index] == '+' || value[index] == '-') && index + 6 == value.Length && value[index + 3] == ':')
            {
                int offsetHour = ParseFixedDigits(value, index + 1, 2);
                int offsetMinute = ParseFixedDigits(value, index + 4, 2);
                if (offsetHour < 0 || offsetHour > 14 || offsetMinute < 0 || offsetMinute > 59)
                {
                    result.AddError("xml.dateTime.timeZone", "DateTime XML value has an invalid time zone offset.");
                    return result;
                }

                index = index + 6;
            }
            else
            {
                result.AddError("xml.dateTime.timeZone", "DateTime XML value has an invalid time zone suffix.");
                return result;
            }
        }

        if (index != value.Length)
        {
            result.AddError("xml.dateTime.invalid", "DateTime XML value contains trailing characters.");
            return result;
        }

        result.DateTimeValue = new System.DateTime(year, month, day, hour, minute, second, millisecond, kind);
        return result;
    }

    internal static XlsxXmlValueParseResult ParseColor(string text)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        string value = NormalizeValueText(text);
        if (value.StartsWith("#"))
        {
            value = value.Substring(1);
        }

        if (value.Length != 6 && value.Length != 8)
        {
            result.AddError("xml.color.invalid", "Color XML value must be six or eight hexadecimal characters.");
            return result;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!IsHexDigit(value[index]))
            {
                result.AddError("xml.color.invalid", "Color XML value contains a non-hexadecimal character.");
                return result;
            }

            index = index + 1;
        }

        result.ColorValue.HasRgb = true;
        result.ColorValue.Rgb = value.ToUpperInvariant();
        result.TextValue = result.ColorValue.Rgb;
        return result;
    }

    internal static XlsxXmlValueParseResult ParseCellReference(string text)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        string value = NormalizeValueText(text);
        if (value.Length == 0)
        {
            result.AddError("xml.cellReference.empty", "Cell reference XML value must not be empty.");
            return result;
        }

        int index = 0;
        bool absoluteColumn = false;
        bool absoluteRow = false;
        if (value[index] == '$')
        {
            absoluteColumn = true;
            index = index + 1;
        }

        int column = 0;
        int columnLetters = 0;
        while (index < value.Length && IsAsciiLetter(value[index]))
        {
            column = column * 26 + GetUpperAsciiLetterNumber(value[index]);
            columnLetters = columnLetters + 1;
            index = index + 1;
        }

        if (columnLetters == 0)
        {
            result.AddError("xml.cellReference.column", "Cell reference XML value must contain a column name.");
            return result;
        }

        if (index < value.Length && value[index] == '$')
        {
            absoluteRow = true;
            index = index + 1;
        }

        int row = 0;
        int rowDigits = 0;
        while (index < value.Length && IsAsciiDigit(value[index]))
        {
            row = row * 10 + (value[index] - '0');
            rowDigits = rowDigits + 1;
            index = index + 1;
            if (row > MaxSpreadsheetRows)
            {
                result.AddError("xml.cellReference.rowRange", "Cell reference row exceeds the worksheet row limit.");
                return result;
            }
        }

        if (rowDigits == 0)
        {
            result.AddError("xml.cellReference.row", "Cell reference XML value must contain a row number.");
            return result;
        }

        if (index != value.Length)
        {
            result.AddError("xml.cellReference.invalid", "Cell reference XML value contains trailing characters.");
            return result;
        }

        if (row <= 0)
        {
            result.AddError("xml.cellReference.rowRange", "Cell reference row must be greater than zero.");
            return result;
        }

        if (column <= 0 || column > MaxSpreadsheetColumns)
        {
            result.AddError("xml.cellReference.columnRange", "Cell reference column exceeds the worksheet column limit.");
            return result;
        }

        result.CellReferenceValue.Row = row;
        result.CellReferenceValue.Column = column;
        result.CellReferenceValue.IsAbsoluteRow = absoluteRow;
        result.CellReferenceValue.IsAbsoluteColumn = absoluteColumn;
        result.CellReferenceValue.Text = XlsxXmlValueWriter.WriteCellReference(result.CellReferenceValue);
        result.TextValue = result.CellReferenceValue.Text;
        return result;
    }

    internal static XlsxXmlValueParseResult ParseRelationshipId(string? text)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        string value = NormalizeValueText(text);
        if (value.Length == 0)
        {
            result.AddError("xml.relationshipId.empty", "Relationship Id XML value must not be empty.");
            return result;
        }

        if (!IsXmlNameStartCharacter(value[0]))
        {
            result.AddError("xml.relationshipId.invalid", "Relationship Id XML value must start with a name character.");
            return result;
        }

        int index = 1;
        while (index < value.Length)
        {
            if (!IsXmlNameCharacter(value[index]))
            {
                result.AddError("xml.relationshipId.invalid", "Relationship Id XML value contains an invalid name character.");
                return result;
            }

            index = index + 1;
        }

        result.TextValue = value;
        return result;
    }

    internal static string NormalizeValueText(string? text)
    {
        if (text == null)
        {
            return "";
        }

        return text.Trim();
    }

    internal static bool SetDateValue(XlsxXmlValueParseResult result, string value, int start, System.DateTimeKind kind)
    {
        int year = ParseFixedDigits(value, start, 4);
        int month = ParseFixedDigits(value, start + 5, 2);
        int day = ParseFixedDigits(value, start + 8, 2);
        if (value[start + 4] != '-' || value[start + 7] != '-' || year < 0 || month < 0 || day < 0)
        {
            result.AddError("xml.date.invalid", "Date XML value must use yyyy-MM-dd separators.");
            return false;
        }

        if (!IsValidDate(year, month, day))
        {
            result.AddError("xml.date.range", "Date XML value contains an out-of-range date component.");
            return false;
        }

        result.DateTimeValue = new System.DateTime(year, month, day, 0, 0, 0, kind);
        return true;
    }

    internal static int ParseFixedDigits(string value, int start, int count)
    {
        if (value == null || start < 0 || count <= 0 || start + count > value.Length)
        {
            return -1;
        }

        int result = 0;
        int index = 0;
        while (index < count)
        {
            char current = value[start + index];
            if (!IsAsciiDigit(current))
            {
                return -1;
            }

            result = result * 10 + (current - '0');
            index = index + 1;
        }

        return result;
    }

    internal static bool IsValidDate(int year, int month, int day)
    {
        if (year < 1 || year > 9999 || month < 1 || month > 12)
        {
            return false;
        }

        int maxDay = GetDaysInMonth(year, month);
        if (day < 1 || day > maxDay)
        {
            return false;
        }

        return true;
    }

    internal static int GetDaysInMonth(int year, int month)
    {
        switch (month)
        {
            case 2:
                if (IsLeapYear(year))
                {
                    return 29;
                }

                return 28;
            case 4:
            case 6:
            case 9:
            case 11:
                return 30;
            default:
                return 31;
        }
    }

    internal static bool IsLeapYear(int year)
    {
        if (year % 400 == 0)
        {
            return true;
        }

        if (year % 100 == 0)
        {
            return false;
        }

        if (year % 4 == 0)
        {
            return true;
        }

        return false;
    }

    internal static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsAsciiLetter(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return true;
        }

        if (value >= 'a' && value <= 'z')
        {
            return true;
        }

        return false;
    }

    internal static int GetUpperAsciiLetterNumber(char value)
    {
        if (value >= 'a' && value <= 'z')
        {
            return value - 'a' + 1;
        }

        return value - 'A' + 1;
    }

    internal static bool IsHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return true;
        }

        if (value >= 'A' && value <= 'F')
        {
            return true;
        }

        if (value >= 'a' && value <= 'f')
        {
            return true;
        }

        return false;
    }

    internal static bool IsXmlNameStartCharacter(char value)
    {
        if (IsAsciiLetter(value))
        {
            return true;
        }

        if (value == '_')
        {
            return true;
        }

        return false;
    }

    internal static bool IsXmlNameCharacter(char value)
    {
        if (IsXmlNameStartCharacter(value) || IsAsciiDigit(value))
        {
            return true;
        }

        if (value == '-' || value == '.')
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Validates and writes xml value data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
internal static class XlsxXmlValueWriter
{
    internal static string WriteBoolean(bool value)
    {
        if (value)
        {
            return "1";
        }

        return "0";
    }

    internal static string WriteInteger(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string WriteUnsignedInteger(uint value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string WriteDouble(double value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    internal static string WriteDate(System.DateTime value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    internal static string WriteDateTime(System.DateTime value)
    {
        return XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind);
    }

    internal static string WriteColor(XlsxSpreadsheetColor value)
    {
        if (value == null || !value.HasRgb)
        {
            return "";
        }

        return value.Rgb.ToUpperInvariant();
    }

    internal static string WriteCellReference(XlsxCellReferenceValue value)
    {
        if (value == null)
        {
            return "";
        }

        return WriteCellReference(value.Row, value.Column, value.IsAbsoluteRow, value.IsAbsoluteColumn);
    }

    internal static string WriteCellReference(int row, int column, bool absoluteRow, bool absoluteColumn)
    {
        return XlsxA1CellReferenceConverter.FormatString(row, column, absoluteRow, absoluteColumn);
    }

    internal static string WriteColumnName(int column)
    {
        return XlsxA1ColumnNameConverter.FormatString(column);
    }

    internal static string WriteRelationshipId(string value)
    {
        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseRelationshipId(value);
        if (!result.IsValid)
        {
            return "";
        }

        return result.TextValue;
    }
}

/// <summary>
/// Represents date serial converter data used by the public workbook API.
/// </summary>
internal static class XlsxDateSerialConverter
{
    /// <summary>
    /// Identifies the ticks per day OOXML value.
    /// </summary>
    internal const long TicksPerDay = 864000000000;

    /// <summary>
    /// Converts serial into the corresponding typed representation.
    /// </summary>
    /// <param name="serialValue">The serial value value used by the operation.</param>
    /// <param name="use1904DateSystem">The use1904 date system value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxDateSerialConversionResult FromSerial(double serialValue, bool use1904DateSystem)
    {
        XlsxDateSerialConversionResult result = ValidateSerial(serialValue, use1904DateSystem);
        if (!result.IsValid)
        {
            return result;
        }

        int wholeDays = (int)System.Math.Floor(serialValue);
        double fraction = serialValue - wholeDays;
        long ticks = FractionToTicks(fraction);
        if (ticks >= TicksPerDay)
        {
            wholeDays = wholeDays + 1;
            ticks = ticks - TicksPerDay;
        }

        result.SerialValue = serialValue;
        result.Is1904DateSystem = use1904DateSystem;
        result.HasTimeFraction = ticks > 0;
        if (use1904DateSystem)
        {
            result.DateTimeValue = new System.DateTime(1904, 1, 1).AddDays(wholeDays).AddTicks(ticks);
            return result;
        }

        if (wholeDays == 60)
        {
            result.DateTimeValue = new System.DateTime(1900, 2, 28).AddTicks(ticks);
            result.IsExcel1900LeapDay = true;
            return result;
        }

        int adjustedDays = wholeDays;
        if (wholeDays > 60)
        {
            adjustedDays = wholeDays - 1;
        }

        result.DateTimeValue = new System.DateTime(1899, 12, 31).AddDays(adjustedDays).AddTicks(ticks);
        return result;
    }

    /// <summary>
    /// Converts serial to the requested OOXML-compatible representation.
    /// </summary>
    /// <param name="dateTimeValue">The date time value value used by the operation.</param>
    /// <param name="use1904DateSystem">The use1904 date system value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxDateSerialConversionResult ToSerial(System.DateTime dateTimeValue, bool use1904DateSystem)
    {
        XlsxDateSerialConversionResult result = new XlsxDateSerialConversionResult();
        result.DateTimeValue = dateTimeValue;
        result.Is1904DateSystem = use1904DateSystem;
        System.DateTime day = new System.DateTime(dateTimeValue.Year, dateTimeValue.Month, dateTimeValue.Day);
        long timeTicks = dateTimeValue.Ticks - day.Ticks;
        result.HasTimeFraction = timeTicks > 0;
        double fraction = (double)timeTicks / (double)TicksPerDay;
        if (use1904DateSystem)
        {
            System.DateTime baseDate = new System.DateTime(1904, 1, 1);
            if (day < baseDate)
            {
                result.AddError("dateSerial.dateRange", "Date is before the 1904 date system base date.");
                return result;
            }

            System.TimeSpan span = day - baseDate;
            result.SerialValue = span.TotalDays + fraction;
            return result;
        }

        System.DateTime base1900 = new System.DateTime(1899, 12, 31);
        if (day < base1900)
        {
            result.AddError("dateSerial.dateRange", "Date is before the 1900 date system base date.");
            return result;
        }

        System.TimeSpan days = day - base1900;
        double serialValue = days.TotalDays + fraction;
        if (day >= new System.DateTime(1900, 3, 1))
        {
            serialValue = serialValue + 1;
        }

        result.SerialValue = serialValue;
        return result;
    }

    internal static XlsxDateSerialConversionResult CreateExcel1900LeapDay(double timeFraction)
    {
        XlsxDateSerialConversionResult result = new XlsxDateSerialConversionResult();
        if (timeFraction < 0 || timeFraction >= 1 || double.IsNaN(timeFraction) || double.IsInfinity(timeFraction))
        {
            result.AddError("dateSerial.timeFractionRange", "Excel 1900 leap day time fraction must be greater than or equal to zero and less than one.");
            return result;
        }

        long ticks = FractionToTicks(timeFraction);
        result.SerialValue = 60 + ((double)ticks / (double)TicksPerDay);
        result.DateTimeValue = new System.DateTime(1900, 2, 28).AddTicks(ticks);
        result.Is1904DateSystem = false;
        result.IsExcel1900LeapDay = true;
        result.HasTimeFraction = ticks > 0;
        return result;
    }

    internal static XlsxDateSerialConversionResult ValidateSerial(double serialValue, bool use1904DateSystem)
    {
        XlsxDateSerialConversionResult result = new XlsxDateSerialConversionResult();
        result.SerialValue = serialValue;
        result.Is1904DateSystem = use1904DateSystem;
        if (double.IsNaN(serialValue) || double.IsInfinity(serialValue))
        {
            result.AddError("dateSerial.invalid", "Date serial value must be a finite number.");
            return result;
        }

        if (serialValue < 0)
        {
            result.AddError("dateSerial.range", "Date serial value must not be negative.");
            return result;
        }

        if (serialValue > 4000000)
        {
            result.AddError("dateSerial.range", "Date serial value is outside the supported DateTime range.");
            return result;
        }

        int wholeDays = (int)System.Math.Floor(serialValue);
        if (!IsSupportedWholeDay(wholeDays, use1904DateSystem))
        {
            result.AddError("dateSerial.range", "Date serial value is outside the supported DateTime range.");
        }

        return result;
    }

    internal static bool IsSupportedWholeDay(int wholeDays, bool use1904DateSystem)
    {
        if (wholeDays < 0)
        {
            return false;
        }

        System.DateTime baseDate = new System.DateTime(1904, 1, 1);
        int adjustedDays = wholeDays;
        if (!use1904DateSystem)
        {
            baseDate = new System.DateTime(1899, 12, 31);
            if (wholeDays > 60)
            {
                adjustedDays = wholeDays - 1;
            }
        }

        System.TimeSpan maxSpan = System.DateTime.MaxValue.Date - baseDate;
        return adjustedDays <= maxSpan.TotalDays;
    }

    internal static long FractionToTicks(double fraction)
    {
        return (long)System.Math.Round(fraction * (double)TicksPerDay);
    }
}

/// <summary>
/// Represents number format parser data used by the public workbook API.
/// </summary>
internal static class XlsxNumberFormatParser
{
    /// <summary>
    /// Performs the parse built in operation using the current model state.
    /// </summary>
    /// <param name="builtInId">The built in id value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxNumberFormatParseResult ParseBuiltIn(uint builtInId)
    {
        XlsxNumberFormatParseResult result = new XlsxNumberFormatParseResult();
        string formatCode = GetBuiltInFormatCode(builtInId);
        if (formatCode.Length == 0 && builtInId != 0)
        {
            result.AddError("numberFormat.builtInUnknown", "Built-in number format id is not recognized.");
            result.BuiltInId = builtInId;
            result.IsBuiltIn = true;
            return result;
        }

        XlsxNumberFormatParseResult parsed = ParseCustom(formatCode);
        AddParseDiagnostics(result, parsed);
        result.FormatCode = formatCode;
        result.BuiltInId = builtInId;
        result.IsBuiltIn = true;
        result.Sections = parsed.Sections;
        return result;
    }

    /// <summary>
    /// Performs the parse custom operation using the current model state.
    /// </summary>
    /// <param name="formatCode">The format code value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxNumberFormatParseResult ParseCustom(string formatCode)
    {
        XlsxNumberFormatParseResult result = new XlsxNumberFormatParseResult();
        if (formatCode == null)
        {
            result.AddError("numberFormat.formatNull", "Number format code must not be null.");
            return result;
        }

        result.FormatCode = formatCode;
        List<string> sections = SplitSections(result, formatCode);
        if (!result.IsValid)
        {
            return result;
        }

        foreach (string sectionText in sections)
        {
            XlsxNumberFormatSection section = ParseSection(result, sectionText);
            result.Sections.Add(section);
        }

        return result;
    }

    internal static List<string> SplitSections(XlsxNumberFormatParseResult result, string formatCode)
    {
        List<string> sections = new List<string>();
        StringBuilder builder = new StringBuilder();
        bool inQuote = false;
        bool inBracket = false;
        int index = 0;
        while (index < formatCode.Length)
        {
            char current = formatCode[index];
            if (current == '"')
            {
                inQuote = !inQuote;
                builder.Append(current);
            }
            else if (!inQuote && current == '[')
            {
                inBracket = true;
                builder.Append(current);
            }
            else if (!inQuote && current == ']')
            {
                inBracket = false;
                builder.Append(current);
            }
            else if (!inQuote && !inBracket && current == ';')
            {
                sections.Add(builder.ToString());
                builder.Length = 0;
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        if (inQuote)
        {
            result.AddError("numberFormat.quoteUnclosed", "Number format contains an unterminated quoted literal.");
            return sections;
        }

        if (inBracket)
        {
            result.AddError("numberFormat.bracketUnclosed", "Number format contains an unterminated bracket expression.");
            return sections;
        }

        sections.Add(builder.ToString());
        if (sections.Count > 4)
        {
            result.AddError("numberFormat.tooManySections", "Number format must not contain more than four sections.");
        }

        return sections;
    }

    internal static XlsxNumberFormatSection ParseSection(XlsxNumberFormatParseResult result, string sectionText)
    {
        XlsxNumberFormatSection section = new XlsxNumberFormatSection();
        section.FormatCode = sectionText;
        int index = ReadSectionMetadata(section, sectionText);
        while (index < sectionText.Length)
        {
            index = ReadToken(result, section, sectionText, index);
            if (!result.IsValid)
            {
                return section;
            }
        }

        return section;
    }

    internal static int ReadSectionMetadata(XlsxNumberFormatSection section, string sectionText)
    {
        int index = 0;
        while (index < sectionText.Length && sectionText[index] == '[')
        {
            int close = FindClosingBracket(sectionText, index + 1);
            if (close < 0)
            {
                return index;
            }

            string metadata = sectionText.Substring(index + 1, close - index - 1);
            if (IsConditionMetadata(metadata))
            {
                section.Condition = metadata;
                section.HasCondition = true;
            }
            else
            {
                section.Color = metadata;
                section.HasColor = metadata.Length > 0;
            }

            index = close + 1;
        }

        return index;
    }

    internal static int ReadToken(XlsxNumberFormatParseResult result, XlsxNumberFormatSection section, string text, int index)
    {
        char current = text[index];
        if (current == '"')
        {
            return ReadQuotedLiteral(result, section, text, index);
        }

        if (current == '\\')
        {
            return ReadEscapedLiteral(section, text, index);
        }

        if (current == '_')
        {
            return ReadTwoCharacterToken(section, text, index, XlsxNumberFormatTokenKind.Spacing);
        }

        if (current == '*')
        {
            return ReadTwoCharacterToken(section, text, index, XlsxNumberFormatTokenKind.Fill);
        }

        if (current == '0' || current == '#' || current == '?')
        {
            return ReadRepeatedToken(section, text, index, XlsxNumberFormatTokenKind.NumberPlaceholder);
        }

        if (current == '.')
        {
            AddToken(section, XlsxNumberFormatTokenKind.DecimalPoint, ".");
            return index + 1;
        }

        if (current == ',')
        {
            AddToken(section, XlsxNumberFormatTokenKind.ThousandsSeparator, ",");
            return index + 1;
        }

        if (current == '%')
        {
            AddToken(section, XlsxNumberFormatTokenKind.Percent, "%");
            return index + 1;
        }

        if (current == '@')
        {
            AddToken(section, XlsxNumberFormatTokenKind.TextPlaceholder, "@");
            return index + 1;
        }

        if (IsExponentStart(text, index))
        {
            AddToken(section, XlsxNumberFormatTokenKind.Exponent, text.Substring(index, 2));
            return index + 2;
        }

        if (IsDateTimeFormatCharacter(current))
        {
            return ReadRepeatedToken(section, text, index, XlsxNumberFormatTokenKind.DateTimeCode);
        }

        AddToken(section, XlsxNumberFormatTokenKind.Literal, current.ToString());
        return index + 1;
    }

    internal static int ReadQuotedLiteral(XlsxNumberFormatParseResult result, XlsxNumberFormatSection section, string text, int index)
    {
        int start = index + 1;
        int current = start;
        while (current < text.Length && text[current] != '"')
        {
            current = current + 1;
        }

        if (current >= text.Length)
        {
            result.AddError("numberFormat.quoteUnclosed", "Number format contains an unterminated quoted literal.");
            return text.Length;
        }

        AddToken(section, XlsxNumberFormatTokenKind.Literal, text.Substring(start, current - start));
        return current + 1;
    }

    internal static int ReadEscapedLiteral(XlsxNumberFormatSection section, string text, int index)
    {
        if (index + 1 >= text.Length)
        {
            AddToken(section, XlsxNumberFormatTokenKind.Literal, "\\");
            return index + 1;
        }

        AddToken(section, XlsxNumberFormatTokenKind.Literal, text.Substring(index + 1, 1));
        return index + 2;
    }

    internal static int ReadTwoCharacterToken(XlsxNumberFormatSection section, string text, int index, XlsxNumberFormatTokenKind kind)
    {
        if (index + 1 >= text.Length)
        {
            AddToken(section, XlsxNumberFormatTokenKind.Literal, text.Substring(index, 1));
            return index + 1;
        }

        AddToken(section, kind, text.Substring(index, 2));
        return index + 2;
    }

    internal static int ReadRepeatedToken(XlsxNumberFormatSection section, string text, int index, XlsxNumberFormatTokenKind kind)
    {
        int current = index + 1;
        while (current < text.Length && text[current] == text[index])
        {
            current = current + 1;
        }

        AddToken(section, kind, text.Substring(index, current - index));
        return current;
    }

    internal static void AddToken(XlsxNumberFormatSection section, XlsxNumberFormatTokenKind kind, string text)
    {
        XlsxNumberFormatToken token = new XlsxNumberFormatToken();
        token.Kind = kind;
        token.Text = text;
        section.Tokens.Add(token);
    }

    internal static int FindClosingBracket(string text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            if (text[index] == ']')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsConditionMetadata(string text)
    {
        if (text == null || text.Length == 0)
        {
            return false;
        }

        return text[0] == '<' || text[0] == '>' || text[0] == '=';
    }

    internal static bool IsExponentStart(string text, int index)
    {
        if (index + 1 >= text.Length)
        {
            return false;
        }

        char current = text[index];
        char next = text[index + 1];
        return (current == 'E' || current == 'e') && (next == '+' || next == '-');
    }

    internal static bool IsDateTimeFormatCharacter(char value)
    {
        char upper = value;
        if (value >= 'a' && value <= 'z')
        {
            upper = (char)(value - 32);
        }

        return upper == 'Y' || upper == 'M' || upper == 'D' || upper == 'H' || upper == 'S' || upper == 'A' || upper == 'P';
    }

    internal static void AddParseDiagnostics(XlsxNumberFormatParseResult result, XlsxNumberFormatParseResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static string GetBuiltInFormatCode(uint builtInId)
    {
        switch (builtInId)
        {
            case 0: return "General";
            case 1: return "0";
            case 2: return "0.00";
            case 3: return "#,##0";
            case 4: return "#,##0.00";
            case 9: return "0%";
            case 10: return "0.00%";
            case 11: return "0.00E+00";
            case 12: return "# ?/?";
            case 13: return "# ??/??";
            case 14: return "m/d/yy";
            case 15: return "d-mmm-yy";
            case 16: return "d-mmm";
            case 17: return "mmm-yy";
            case 18: return "h:mm AM/PM";
            case 19: return "h:mm:ss AM/PM";
            case 20: return "h:mm";
            case 21: return "h:mm:ss";
            case 22: return "m/d/yy h:mm";
            case 37: return "#,##0 ;(#,##0)";
            case 38: return "#,##0 ;[Red](#,##0)";
            case 39: return "#,##0.00;(#,##0.00)";
            case 40: return "#,##0.00;[Red](#,##0.00)";
            case 45: return "mm:ss";
            case 46: return "[h]:mm:ss";
            case 47: return "mm:ss.0";
            case 48: return "##0.0E+0";
            case 49: return "@";
            default: return "";
        }
    }
}

/// <summary>
/// Represents formatted text renderer data used by the public workbook API.
/// </summary>
internal static class XlsxFormattedTextRenderer
{
    /// <summary>
    /// Performs the render cell value operation using the current model state.
    /// </summary>
    /// <param name="value">The value value used by the operation.</param>
    /// <param name="formatCode">The format code value used by the operation.</param>
    /// <param name="use1904DateSystem">The use1904 date system value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxFormattedTextRenderResult RenderCellValue(XlsxCellValue value, string formatCode, bool use1904DateSystem)
    {
        XlsxFormattedTextRenderResult result = new XlsxFormattedTextRenderResult();
        if (value == null)
        {
            result.AddError("formattedText.valueNull", "Cell value must not be null.");
            return result;
        }

        if (value.Kind == XlsxCellValueKind.Blank)
        {
            result.Text = "";
            return result;
        }

        if (value.Kind == XlsxCellValueKind.Boolean)
        {
            if (value.Boolean)
            {
                result.Text = "TRUE";
            }
            else
            {
                result.Text = "FALSE";
            }

            return result;
        }

        if (value.Kind == XlsxCellValueKind.Error)
        {
            if (value.HasErrorKind)
            {
                result.Text = XlsxCellValue.GetErrorText(value.ErrorKind);
            }
            else
            {
                result.Text = value.Text;
            }
            return result;
        }

        if (value.Kind == XlsxCellValueKind.Text || value.Kind == XlsxCellValueKind.InlineString || value.Kind == XlsxCellValueKind.RichText)
        {
            result.Text = RenderText(value.Text, formatCode);
            return result;
        }

        if (value.Kind == XlsxCellValueKind.Number)
        {
            return RenderNumber(value.Number, formatCode, use1904DateSystem);
        }

        if (value.Kind == XlsxCellValueKind.Decimal)
        {
            if (NormalizeFormatCode(formatCode) == "General")
            {
                result.Text = XlsxCellValueConverter.GetExactText(value, value.Decimal.ToString(CultureInfo.InvariantCulture));
                return result;
            }
            return RenderNumber((double)value.Decimal, formatCode, use1904DateSystem);
        }

        if (value.Kind == XlsxCellValueKind.Duration)
        {
            double days = (double)value.Duration.Ticks / 864000000000d;
            return RenderNumber(days, formatCode, use1904DateSystem);
        }

        if (value.Kind == XlsxCellValueKind.DateTime)
        {
            return RenderDateTime(value.DateTime, formatCode);
        }

        if (value.Kind == XlsxCellValueKind.FormulaCachedValue)
        {
            if (value.HasTypedCachedValue && value.TypedCachedValue != null)
            {
                return RenderCellValue(value.TypedCachedValue, formatCode, use1904DateSystem);
            }
            result.Text = value.CachedValue;
            return result;
        }

        result.Text = value.RawValue;
        return result;
    }

    internal static XlsxFormattedTextRenderResult RenderNumber(double number, string formatCode, bool use1904DateSystem)
    {
        XlsxFormattedTextRenderResult result = new XlsxFormattedTextRenderResult();
        string code = NormalizeFormatCode(formatCode);
        if (code == "General")
        {
            result.Text = number.ToString("G15", CultureInfo.InvariantCulture);
            return result;
        }

        XlsxNumberFormatParseResult parseResult = XlsxNumberFormatParser.ParseCustom(code);
        AddFormatDiagnostics(result, parseResult);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxNumberFormatSection section = SelectNumberSection(parseResult, number);
        if (SectionHasDateTime(section))
        {
            XlsxDateSerialConversionResult dateResult = XlsxDateSerialConverter.FromSerial(number, use1904DateSystem);
            AddDateDiagnostics(result, dateResult);
            if (!result.IsValid)
            {
                return result;
            }

            result.Text = RenderDateTimeWithSection(dateResult.DateTimeValue, section);
            return result;
        }

        result.Text = RenderNumberWithSection(number, section);
        return result;
    }

    internal static XlsxFormattedTextRenderResult RenderDateTime(System.DateTime dateTime, string formatCode)
    {
        XlsxFormattedTextRenderResult result = new XlsxFormattedTextRenderResult();
        string code = NormalizeFormatCode(formatCode);
        XlsxNumberFormatParseResult parseResult = XlsxNumberFormatParser.ParseCustom(code);
        AddFormatDiagnostics(result, parseResult);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxNumberFormatSection section = parseResult.Sections[0];
        result.Text = RenderDateTimeWithSection(dateTime, section);
        return result;
    }

    internal static string RenderText(string text, string formatCode)
    {
        string value = XlsxCellValueConverter.NormalizeText(text);
        XlsxNumberFormatParseResult parseResult = XlsxNumberFormatParser.ParseCustom(NormalizeFormatCode(formatCode));
        if (!parseResult.IsValid || parseResult.Sections.Count < 4)
        {
            return value;
        }

        return ReplaceAtPlaceholder(parseResult.Sections[3].FormatCode, value);
    }

    internal static XlsxNumberFormatSection SelectNumberSection(XlsxNumberFormatParseResult parseResult, double number)
    {
        if (number > 0 || parseResult.Sections.Count == 1)
        {
            return parseResult.Sections[0];
        }

        if (number < 0)
        {
            if (parseResult.Sections.Count > 1)
            {
                return parseResult.Sections[1];
            }

            return parseResult.Sections[0];
        }

        if (parseResult.Sections.Count > 2)
        {
            return parseResult.Sections[2];
        }

        return parseResult.Sections[0];
    }

    internal static string RenderNumberWithSection(double number, XlsxNumberFormatSection section)
    {
        if (!SectionHasNumericToken(section))
        {
            return RenderLiteralSection(section);
        }

        double value = number;
        bool negative = value < 0;
        if (negative)
        {
            value = 0 - value;
        }

        int percentCount = CountTokenKind(section, XlsxNumberFormatTokenKind.Percent);
        int index = 0;
        while (index < percentCount)
        {
            value = value * 100;
            index = index + 1;
        }

        int decimals = CountDecimalPlaces(section);
        bool useThousands = CountTokenKind(section, XlsxNumberFormatTokenKind.ThousandsSeparator) > 0;
        string numericFormat = "F" + decimals.ToString(CultureInfo.InvariantCulture);
        if (useThousands)
        {
            numericFormat = "N" + decimals.ToString(CultureInfo.InvariantCulture);
        }

        string core = value.ToString(numericFormat, CultureInfo.InvariantCulture);
        string prefix = GetLiteralPrefix(section);
        string suffix = GetLiteralSuffix(section);
        index = 0;
        while (index < percentCount)
        {
            suffix = suffix + "%";
            index = index + 1;
        }

        if (negative && prefix.Length == 0 && suffix.Length == 0)
        {
            prefix = "-";
        }

        return prefix + core + suffix;
    }

    internal static bool SectionHasNumericToken(XlsxNumberFormatSection section)
    {
        foreach (XlsxNumberFormatToken token in section.Tokens)
        {
            if (IsNumericToken(token))
            {
                return true;
            }
        }

        return false;
    }

    internal static string RenderLiteralSection(XlsxNumberFormatSection section)
    {
        StringBuilder builder = new StringBuilder();
        foreach (XlsxNumberFormatToken token in section.Tokens)
        {
            if (token.Kind == XlsxNumberFormatTokenKind.Literal)
            {
                builder.Append(token.Text);
            }
        }

        return builder.ToString();
    }

    internal static string RenderDateTimeWithSection(System.DateTime dateTime, XlsxNumberFormatSection section)
    {
        StringBuilder builder = new StringBuilder();
        string text = StripMetadata(section.FormatCode);
        bool twelveHour = ContainsAmPm(text);
        int index = 0;
        char previousCode = '\0';
        while (index < text.Length)
        {
            if (index + 5 <= text.Length && IsAmPm(text, index))
            {
                if (dateTime.Hour < 12)
                {
                    builder.Append("AM");
                }
                else
                {
                    builder.Append("PM");
                }

                previousCode = 'p';
                index = index + 5;
                continue;
            }

            char current = text[index];
            if (current == '"')
            {
                index = AppendQuotedLiteral(builder, text, index);
                continue;
            }

            if (current == '\\')
            {
                if (index + 1 < text.Length)
                {
                    builder.Append(text[index + 1]);
                    index = index + 2;
                }
                else
                {
                    index = index + 1;
                }

                continue;
            }

            if (IsDateTimeFormatCharacterForRender(current))
            {
                int end = ReadRepeatedDateCode(text, index);
                string code = text.Substring(index, end - index);
                builder.Append(RenderDateTimeCode(dateTime, code, previousCode, NextDateCode(text, end), twelveHour));
                previousCode = ToLowerAscii(current);
                index = end;
                continue;
            }

            builder.Append(current);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string RenderDateTimeCode(System.DateTime dateTime, string code, char previousCode, char nextCode, bool twelveHour)
    {
        char kind = ToLowerAscii(code[0]);
        if (kind == 'y')
        {
            if (code.Length <= 2)
            {
                return (dateTime.Year % 100).ToString("00", CultureInfo.InvariantCulture);
            }

            return dateTime.Year.ToString("0000", CultureInfo.InvariantCulture);
        }

        if (kind == 'd')
        {
            if (code.Length == 1)
            {
                return dateTime.Day.ToString(CultureInfo.InvariantCulture);
            }

            return dateTime.Day.ToString("00", CultureInfo.InvariantCulture);
        }

        if (kind == 'h')
        {
            int hour = dateTime.Hour;
            if (twelveHour)
            {
                hour = hour % 12;
                if (hour == 0)
                {
                    hour = 12;
                }
            }

            if (code.Length == 1)
            {
                return hour.ToString(CultureInfo.InvariantCulture);
            }

            return hour.ToString("00", CultureInfo.InvariantCulture);
        }

        if (kind == 's')
        {
            if (code.Length == 1)
            {
                return dateTime.Second.ToString(CultureInfo.InvariantCulture);
            }

            return dateTime.Second.ToString("00", CultureInfo.InvariantCulture);
        }

        if (kind == 'm')
        {
            int value = dateTime.Month;
            if (previousCode == 'h' || previousCode == 's' || nextCode == 's')
            {
                value = dateTime.Minute;
            }

            if (code.Length == 1)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("00", CultureInfo.InvariantCulture);
        }

        return "";
    }

    internal static string ReplaceAtPlaceholder(string formatCode, string value)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < formatCode.Length)
        {
            if (formatCode[index] == '@')
            {
                builder.Append(value);
            }
            else if (formatCode[index] != '"')
            {
                builder.Append(formatCode[index]);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string NormalizeFormatCode(string formatCode)
    {
        if (formatCode == null || formatCode.Length == 0)
        {
            return "General";
        }

        return formatCode;
    }

    internal static bool SectionHasDateTime(XlsxNumberFormatSection section)
    {
        foreach (XlsxNumberFormatToken token in section.Tokens)
        {
            if (token.Kind == XlsxNumberFormatTokenKind.DateTimeCode)
            {
                return true;
            }
        }

        return false;
    }

    internal static int CountTokenKind(XlsxNumberFormatSection section, XlsxNumberFormatTokenKind kind)
    {
        int count = 0;
        foreach (XlsxNumberFormatToken token in section.Tokens)
        {
            if (token.Kind == kind)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static int CountDecimalPlaces(XlsxNumberFormatSection section)
    {
        bool afterDecimal = false;
        int count = 0;
        foreach (XlsxNumberFormatToken token in section.Tokens)
        {
            if (token.Kind == XlsxNumberFormatTokenKind.DecimalPoint)
            {
                afterDecimal = true;
            }
            else if (afterDecimal && token.Kind == XlsxNumberFormatTokenKind.NumberPlaceholder)
            {
                count = count + token.Text.Length;
            }
            else if (afterDecimal && token.Kind != XlsxNumberFormatTokenKind.ThousandsSeparator)
            {
                return count;
            }
        }

        return count;
    }

    internal static string GetLiteralPrefix(XlsxNumberFormatSection section)
    {
        StringBuilder builder = new StringBuilder();
        foreach (XlsxNumberFormatToken token in section.Tokens)
        {
            if (IsNumericToken(token))
            {
                return builder.ToString();
            }

            if (token.Kind == XlsxNumberFormatTokenKind.Literal)
            {
                builder.Append(token.Text);
            }
        }

        return builder.ToString();
    }

    internal static string GetLiteralSuffix(XlsxNumberFormatSection section)
    {
        StringBuilder builder = new StringBuilder();
        bool seenNumeric = false;
        foreach (XlsxNumberFormatToken token in section.Tokens)
        {
            if (IsNumericToken(token))
            {
                builder.Length = 0;
                seenNumeric = true;
            }
            else if (seenNumeric && token.Kind == XlsxNumberFormatTokenKind.Literal)
            {
                builder.Append(token.Text);
            }
        }

        return builder.ToString();
    }

    internal static bool IsNumericToken(XlsxNumberFormatToken token)
    {
        return token.Kind == XlsxNumberFormatTokenKind.NumberPlaceholder || token.Kind == XlsxNumberFormatTokenKind.DecimalPoint || token.Kind == XlsxNumberFormatTokenKind.ThousandsSeparator || token.Kind == XlsxNumberFormatTokenKind.Percent || token.Kind == XlsxNumberFormatTokenKind.Exponent;
    }

    internal static string StripMetadata(string formatCode)
    {
        int index = 0;
        while (index < formatCode.Length && formatCode[index] == '[')
        {
            int close = XlsxNumberFormatParser.FindClosingBracket(formatCode, index + 1);
            if (close < 0)
            {
                return formatCode;
            }

            string metadata = formatCode.Substring(index + 1, close - index - 1);
            if (XlsxNumberFormatParser.IsConditionMetadata(metadata) || IsColorName(metadata))
            {
                index = close + 1;
            }
            else
            {
                return formatCode.Substring(index);
            }
        }

        return formatCode.Substring(index);
    }

    internal static bool IsColorName(string value)
    {
        return value == "Black" || value == "Blue" || value == "Cyan" || value == "Green" || value == "Magenta" || value == "Red" || value == "White" || value == "Yellow";
    }

    internal static bool IsAmPm(string text, int index)
    {
        return index + 5 <= text.Length && ToLowerAscii(text[index]) == 'a' && ToLowerAscii(text[index + 1]) == 'm' && text[index + 2] == '/' && ToLowerAscii(text[index + 3]) == 'p' && ToLowerAscii(text[index + 4]) == 'm';
    }

    internal static bool ContainsAmPm(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (IsAmPm(text, index))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int AppendQuotedLiteral(StringBuilder builder, string text, int index)
    {
        int current = index + 1;
        while (current < text.Length && text[current] != '"')
        {
            builder.Append(text[current]);
            current = current + 1;
        }

        if (current < text.Length)
        {
            return current + 1;
        }

        return current;
    }

    internal static int ReadRepeatedDateCode(string text, int index)
    {
        int current = index + 1;
        char first = ToLowerAscii(text[index]);
        while (current < text.Length && ToLowerAscii(text[current]) == first)
        {
            current = current + 1;
        }

        return current;
    }

    internal static char NextDateCode(string text, int index)
    {
        int current = index;
        while (current < text.Length)
        {
            if (IsDateTimeFormatCharacterForRender(text[current]))
            {
                return ToLowerAscii(text[current]);
            }

            current = current + 1;
        }

        return '\0';
    }

    internal static char ToLowerAscii(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return (char)(value + 32);
        }

        return value;
    }

    internal static bool IsDateTimeFormatCharacterForRender(char value)
    {
        char lower = ToLowerAscii(value);
        return lower == 'y' || lower == 'm' || lower == 'd' || lower == 'h' || lower == 's';
    }

    internal static void AddFormatDiagnostics(XlsxFormattedTextRenderResult result, XlsxNumberFormatParseResult parseResult)
    {
        foreach (XlsxDiagnostic diagnostic in parseResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddDateDiagnostics(XlsxFormattedTextRenderResult result, XlsxDateSerialConversionResult dateResult)
    {
        foreach (XlsxDiagnostic diagnostic in dateResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}

internal class XlsxStyleIndexMap
{
    internal uint OldIndex;
    internal uint NewIndex;

    internal XlsxStyleIndexMap()
    {
        OldIndex = 0;
        NewIndex = 0;
    }
}

internal class XlsxNumberFormatIdMap
{
    internal uint OldId;
    internal uint NewId;

    internal XlsxNumberFormatIdMap()
    {
        OldId = 0;
        NewId = 0;
    }
}

internal class XlsxStyleInternResult
{
    internal bool IsValid;
    internal XlsxValidationResult Validation;
    internal uint NumberFormatsRemoved;
    internal uint FontsRemoved;
    internal uint FillsRemoved;
    internal uint BordersRemoved;
    internal uint CellStyleFormatsRemoved;
    internal uint CellFormatsRemoved;
    internal uint DifferentialFormatsRemoved;
    internal List<XlsxNumberFormatIdMap> NumberFormatIdMap;
    internal List<XlsxStyleIndexMap> FontIndexMap;
    internal List<XlsxStyleIndexMap> FillIndexMap;
    internal List<XlsxStyleIndexMap> BorderIndexMap;
    internal List<XlsxStyleIndexMap> CellStyleFormatIndexMap;
    internal List<XlsxStyleIndexMap> CellFormatIndexMap;
    internal List<XlsxStyleIndexMap> DifferentialFormatIndexMap;

    internal XlsxStyleInternResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        NumberFormatsRemoved = 0;
        FontsRemoved = 0;
        FillsRemoved = 0;
        BordersRemoved = 0;
        CellStyleFormatsRemoved = 0;
        CellFormatsRemoved = 0;
        DifferentialFormatsRemoved = 0;
        NumberFormatIdMap = new List<XlsxNumberFormatIdMap>();
        FontIndexMap = new List<XlsxStyleIndexMap>();
        FillIndexMap = new List<XlsxStyleIndexMap>();
        BorderIndexMap = new List<XlsxStyleIndexMap>();
        CellStyleFormatIndexMap = new List<XlsxStyleIndexMap>();
        CellFormatIndexMap = new List<XlsxStyleIndexMap>();
        DifferentialFormatIndexMap = new List<XlsxStyleIndexMap>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}

internal class XlsxStyleInternPool
{
    internal XlsxStyleInternResult Deduplicate(XlsxStylesPart part)
    {
        XlsxStyleInternResult result = new XlsxStyleInternResult();
        XlsxStylesPartWriteResult validation = XlsxStylesPartWriter.ValidateStylesPart(part);
        foreach (XlsxDiagnostic diagnostic in validation.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        if (!result.IsValid)
        {
            return result;
        }

        uint numberFormatsOriginalCount = (uint)part.NumberFormats.Count;
        uint fontsOriginalCount = (uint)part.Fonts.Count;
        uint fillsOriginalCount = (uint)part.Fills.Count;
        uint bordersOriginalCount = (uint)part.Borders.Count;
        uint cellStyleFormatsOriginalCount = (uint)part.CellStyleFormats.Count;
        uint cellFormatsOriginalCount = (uint)part.CellFormats.Count;
        uint differentialFormatsOriginalCount = (uint)part.DifferentialFormats.Count;
        DeduplicateNumberFormats(part, result);
        ApplyNumberFormatIdMaps(part.CellStyleFormats, result.NumberFormatIdMap);
        ApplyNumberFormatIdMaps(part.CellFormats, result.NumberFormatIdMap);
        DeduplicateRawStyleElements(part.Fonts, result.FontIndexMap);
        DeduplicateRawStyleElements(part.Fills, result.FillIndexMap);
        DeduplicateRawStyleElements(part.Borders, result.BorderIndexMap);
        ApplyCellFormatIndexMaps(part.CellStyleFormats, result);
        ApplyCellFormatIndexMaps(part.CellFormats, result);
        DeduplicateCellFormatElements(part.CellStyleFormats, result.CellStyleFormatIndexMap);
        DeduplicateCellFormatElements(part.CellFormats, result.CellFormatIndexMap);
        DeduplicateRawStyleElements(part.DifferentialFormats, result.DifferentialFormatIndexMap);
        result.NumberFormatsRemoved = numberFormatsOriginalCount - (uint)part.NumberFormats.Count;
        result.FontsRemoved = fontsOriginalCount - (uint)part.Fonts.Count;
        result.FillsRemoved = fillsOriginalCount - (uint)part.Fills.Count;
        result.BordersRemoved = bordersOriginalCount - (uint)part.Borders.Count;
        result.CellStyleFormatsRemoved = cellStyleFormatsOriginalCount - (uint)part.CellStyleFormats.Count;
        result.CellFormatsRemoved = cellFormatsOriginalCount - (uint)part.CellFormats.Count;
        result.DifferentialFormatsRemoved = differentialFormatsOriginalCount - (uint)part.DifferentialFormats.Count;
        SyncTypedStyleCollections(part);
        SyncCounts(part);
        return result;
    }

    internal static void SyncTypedStyleCollections(XlsxStylesPart part)
    {
        part.TypedFonts.Clear(); foreach (XlsxRawStyleElement item in part.Fonts) { part.TypedFonts.Add(XlsxStylesPartReader.ReadTypedFont(item.RawXml)); }
        part.TypedFills.Clear(); foreach (XlsxRawStyleElement item in part.Fills) { part.TypedFills.Add(XlsxStylesPartReader.ReadTypedFill(item.RawXml)); }
        part.TypedBorders.Clear(); foreach (XlsxRawStyleElement item in part.Borders) { part.TypedBorders.Add(XlsxStylesPartReader.ReadTypedBorder(item.RawXml)); }
        part.TypedDifferentialFormats.Clear(); foreach (XlsxRawStyleElement item in part.DifferentialFormats) { part.TypedDifferentialFormats.Add(XlsxStylesPartReader.ReadTypedDifferentialStyle(item.RawXml)); }
    }

    internal uint GetOrAddNumberFormat(XlsxStylesPart part, XlsxNumberFormatDefinition format)
    {
        XlsxNumberFormatDefinition? existing = FindEquivalentNumberFormat(part.NumberFormats, format);
        if (existing != null)
        {
            return existing.NumberFormatId;
        }

        if (!format.HasNumberFormatId)
        {
            format.NumberFormatId = FindNextCustomNumberFormatId(part);
            format.HasNumberFormatId = true;
        }

        part.NumberFormats.Add(format);
        part.NumberFormatsCount = (uint)part.NumberFormats.Count;
        part.HasNumberFormatsCount = true;
        return format.NumberFormatId;
    }

    internal uint GetOrAddFont(XlsxStylesPart part, XlsxRawStyleElement font)
    {
        return GetOrAddRawStyleElement(part.Fonts, font, 1, part);
    }

    internal uint GetOrAddFill(XlsxStylesPart part, XlsxRawStyleElement fill)
    {
        return GetOrAddRawStyleElement(part.Fills, fill, 2, part);
    }

    internal uint GetOrAddBorder(XlsxStylesPart part, XlsxRawStyleElement border)
    {
        return GetOrAddRawStyleElement(part.Borders, border, 3, part);
    }

    internal uint GetOrAddCellFormat(XlsxStylesPart part, XlsxCellFormatDefinition format)
    {
        int index = FindEquivalentCellFormatIndex(part.CellFormats, format);
        if (index >= 0)
        {
            return (uint)index;
        }

        part.CellFormats.Add(format);
        part.CellFormatsCount = (uint)part.CellFormats.Count;
        part.HasCellFormatsCount = true;
        return (uint)(part.CellFormats.Count - 1);
    }

    internal uint GetOrAddRawStyleElement(List<XlsxRawStyleElement> items, XlsxRawStyleElement item, int target, XlsxStylesPart part)
    {
        int index = FindEquivalentRawStyleIndex(items, item);
        if (index >= 0)
        {
            return (uint)index;
        }

        items.Add(item);
        SetRawCollectionCount(part, target, (uint)items.Count);
        return (uint)(items.Count - 1);
    }

    internal static void DeduplicateNumberFormats(XlsxStylesPart part, XlsxStyleInternResult result)
    {
        List<XlsxNumberFormatDefinition> unique = new List<XlsxNumberFormatDefinition>();
        foreach (XlsxNumberFormatDefinition format in part.NumberFormats)
        {
            XlsxNumberFormatDefinition? existing = FindEquivalentNumberFormat(unique, format);
            if (existing != null)
            {
                if (format.HasNumberFormatId && existing.HasNumberFormatId)
                {
                    AddNumberFormatIdMap(result.NumberFormatIdMap, format.NumberFormatId, existing.NumberFormatId);
                }
            }
            else
            {
                unique.Add(format);
                if (format.HasNumberFormatId)
                {
                    AddNumberFormatIdMap(result.NumberFormatIdMap, format.NumberFormatId, format.NumberFormatId);
                }
            }
        }

        part.NumberFormats.Clear();
        foreach (XlsxNumberFormatDefinition format in unique)
        {
            part.NumberFormats.Add(format);
        }
    }

    internal static void DeduplicateRawStyleElements(List<XlsxRawStyleElement> items, List<XlsxStyleIndexMap> maps)
    {
        List<XlsxRawStyleElement> unique = new List<XlsxRawStyleElement>();
        int oldIndex = 0;
        foreach (XlsxRawStyleElement item in items)
        {
            int existingIndex = FindEquivalentRawStyleIndex(unique, item);
            if (existingIndex >= 0)
            {
                AddIndexMap(maps, (uint)oldIndex, (uint)existingIndex);
            }
            else
            {
                unique.Add(item);
                AddIndexMap(maps, (uint)oldIndex, (uint)(unique.Count - 1));
            }

            oldIndex = oldIndex + 1;
        }

        items.Clear();
        foreach (XlsxRawStyleElement item in unique)
        {
            items.Add(item);
        }
    }

    internal static void DeduplicateCellFormatElements(List<XlsxCellFormatDefinition> formats, List<XlsxStyleIndexMap> maps)
    {
        List<XlsxCellFormatDefinition> unique = new List<XlsxCellFormatDefinition>();
        int oldIndex = 0;
        foreach (XlsxCellFormatDefinition format in formats)
        {
            int existingIndex = FindEquivalentCellFormatIndex(unique, format);
            if (existingIndex >= 0)
            {
                AddIndexMap(maps, (uint)oldIndex, (uint)existingIndex);
            }
            else
            {
                unique.Add(format);
                AddIndexMap(maps, (uint)oldIndex, (uint)(unique.Count - 1));
            }

            oldIndex = oldIndex + 1;
        }

        formats.Clear();
        foreach (XlsxCellFormatDefinition format in unique)
        {
            formats.Add(format);
        }
    }

    internal static void ApplyCellFormatIndexMaps(List<XlsxCellFormatDefinition> formats, XlsxStyleInternResult result)
    {
        foreach (XlsxCellFormatDefinition format in formats)
        {
            if (format.HasFontId)
            {
                format.FontId = MapIndex(result.FontIndexMap, format.FontId);
            }

            if (format.HasFillId)
            {
                format.FillId = MapIndex(result.FillIndexMap, format.FillId);
            }

            if (format.HasBorderId)
            {
                format.BorderId = MapIndex(result.BorderIndexMap, format.BorderId);
            }
        }
    }

    internal static void ApplyNumberFormatIdMaps(List<XlsxCellFormatDefinition> formats, List<XlsxNumberFormatIdMap> maps)
    {
        foreach (XlsxCellFormatDefinition format in formats)
        {
            if (format.HasNumberFormatId)
            {
                format.NumberFormatId = MapNumberFormatId(maps, format.NumberFormatId);
            }
        }
    }

    internal static XlsxNumberFormatDefinition? FindEquivalentNumberFormat(List<XlsxNumberFormatDefinition> formats, XlsxNumberFormatDefinition format)
    {
        foreach (XlsxNumberFormatDefinition existing in formats)
        {
            if (NumberFormatsEquivalent(existing, format))
            {
                return existing;
            }
        }

        return null;
    }

    internal static int FindEquivalentRawStyleIndex(List<XlsxRawStyleElement> items, XlsxRawStyleElement item)
    {
        int index = 0;
        foreach (XlsxRawStyleElement existing in items)
        {
            if (RawStyleElementsEquivalent(existing, item))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindEquivalentCellFormatIndex(List<XlsxCellFormatDefinition> formats, XlsxCellFormatDefinition format)
    {
        int index = 0;
        foreach (XlsxCellFormatDefinition existing in formats)
        {
            if (CellFormatsEquivalent(existing, format))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool NumberFormatsEquivalent(XlsxNumberFormatDefinition left, XlsxNumberFormatDefinition right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (NormalizeStyleText(left.FormatCode) != NormalizeStyleText(right.FormatCode))
        {
            return false;
        }

        return AttributesEquivalent(left.UnknownAttributes, right.UnknownAttributes);
    }

    internal static bool RawStyleElementsEquivalent(XlsxRawStyleElement left, XlsxRawStyleElement right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (NormalizeStyleText(left.RawXml) != NormalizeStyleText(right.RawXml))
        {
            return false;
        }

        return AttributesEquivalent(left.UnknownAttributes, right.UnknownAttributes);
    }

    internal static bool CellFormatsEquivalent(XlsxCellFormatDefinition left, XlsxCellFormatDefinition right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (left.NumberFormatId != right.NumberFormatId || left.HasNumberFormatId != right.HasNumberFormatId)
        {
            return false;
        }

        if (left.FontId != right.FontId || left.HasFontId != right.HasFontId)
        {
            return false;
        }

        if (left.FillId != right.FillId || left.HasFillId != right.HasFillId)
        {
            return false;
        }

        if (left.BorderId != right.BorderId || left.HasBorderId != right.HasBorderId)
        {
            return false;
        }

        if (left.FormatId != right.FormatId || left.HasFormatId != right.HasFormatId)
        {
            return false;
        }

        if (left.ApplyNumberFormat != right.ApplyNumberFormat || left.HasApplyNumberFormat != right.HasApplyNumberFormat)
        {
            return false;
        }

        if (left.ApplyFont != right.ApplyFont || left.HasApplyFont != right.HasApplyFont)
        {
            return false;
        }

        if (left.ApplyFill != right.ApplyFill || left.HasApplyFill != right.HasApplyFill)
        {
            return false;
        }

        if (left.ApplyBorder != right.ApplyBorder || left.HasApplyBorder != right.HasApplyBorder)
        {
            return false;
        }

        if (left.HasAlignment != right.HasAlignment || (left.HasAlignment && !StyleAlignmentsEquivalent(left.Alignment, right.Alignment)))
        {
            return false;
        }

        if (left.HasProtection != right.HasProtection || (left.HasProtection && !StyleProtectionsEquivalent(left.Protection, right.Protection)))
        {
            return false;
        }

        if (NormalizeStyleText(left.RawXml) != NormalizeStyleText(right.RawXml))
        {
            return false;
        }

        return AttributesEquivalent(left.UnknownAttributes, right.UnknownAttributes);
    }

    internal static bool StyleAlignmentsEquivalent(XlsxStyleAlignment left, XlsxStyleAlignment right)
    {
        if (left == null || right == null) { return left == right; }
        return left.Horizontal == right.Horizontal && left.Vertical == right.Vertical && left.TextRotation == right.TextRotation && left.HasTextRotation == right.HasTextRotation && left.WrapText == right.WrapText && left.HasWrapText == right.HasWrapText && left.ShrinkToFit == right.ShrinkToFit && left.HasShrinkToFit == right.HasShrinkToFit && left.Indent == right.Indent && left.HasIndent == right.HasIndent && left.ReadingOrder == right.ReadingOrder && left.HasReadingOrder == right.HasReadingOrder;
    }

    internal static bool StyleProtectionsEquivalent(XlsxStyleProtection left, XlsxStyleProtection right)
    {
        if (left == null || right == null) { return left == right; }
        return left.Locked == right.Locked && left.HasLocked == right.HasLocked && left.Hidden == right.Hidden && left.HasHidden == right.HasHidden;
    }

    internal static bool AttributesEquivalent(List<XlsxXmlAttribute> left, List<XlsxXmlAttribute> right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Count)
        {
            XlsxXmlAttribute leftAttribute = left[index];
            XlsxXmlAttribute rightAttribute = right[index];
            if (leftAttribute.NamespaceUri != rightAttribute.NamespaceUri || leftAttribute.LocalName != rightAttribute.LocalName || leftAttribute.Prefix != rightAttribute.Prefix || leftAttribute.Value != rightAttribute.Value)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static uint MapIndex(List<XlsxStyleIndexMap> maps, uint oldIndex)
    {
        foreach (XlsxStyleIndexMap map in maps)
        {
            if (map.OldIndex == oldIndex)
            {
                return map.NewIndex;
            }
        }

        return oldIndex;
    }

    internal static uint MapNumberFormatId(List<XlsxNumberFormatIdMap> maps, uint oldId)
    {
        foreach (XlsxNumberFormatIdMap map in maps)
        {
            if (map.OldId == oldId)
            {
                return map.NewId;
            }
        }

        return oldId;
    }

    internal static void AddIndexMap(List<XlsxStyleIndexMap> maps, uint oldIndex, uint newIndex)
    {
        XlsxStyleIndexMap map = new XlsxStyleIndexMap();
        map.OldIndex = oldIndex;
        map.NewIndex = newIndex;
        maps.Add(map);
    }

    internal static void AddNumberFormatIdMap(List<XlsxNumberFormatIdMap> maps, uint oldId, uint newId)
    {
        foreach (XlsxNumberFormatIdMap existing in maps)
        {
            if (existing.OldId == oldId)
            {
                existing.NewId = newId;
                return;
            }
        }

        XlsxNumberFormatIdMap map = new XlsxNumberFormatIdMap();
        map.OldId = oldId;
        map.NewId = newId;
        maps.Add(map);
    }

    internal static void SyncCounts(XlsxStylesPart part)
    {
        if (part.HasNumberFormatsCount || part.NumberFormats.Count > 0)
        {
            part.NumberFormatsCount = (uint)part.NumberFormats.Count;
            part.HasNumberFormatsCount = true;
        }

        if (part.HasFontsCount || part.Fonts.Count > 0)
        {
            part.FontsCount = (uint)part.Fonts.Count;
            part.HasFontsCount = true;
        }

        if (part.HasFillsCount || part.Fills.Count > 0)
        {
            part.FillsCount = (uint)part.Fills.Count;
            part.HasFillsCount = true;
        }

        if (part.HasBordersCount || part.Borders.Count > 0)
        {
            part.BordersCount = (uint)part.Borders.Count;
            part.HasBordersCount = true;
        }

        if (part.HasCellStyleFormatsCount || part.CellStyleFormats.Count > 0)
        {
            part.CellStyleFormatsCount = (uint)part.CellStyleFormats.Count;
            part.HasCellStyleFormatsCount = true;
        }

        if (part.HasCellFormatsCount || part.CellFormats.Count > 0)
        {
            part.CellFormatsCount = (uint)part.CellFormats.Count;
            part.HasCellFormatsCount = true;
        }

        if (part.HasDifferentialFormatsCount || part.DifferentialFormats.Count > 0)
        {
            part.DifferentialFormatsCount = (uint)part.DifferentialFormats.Count;
            part.HasDifferentialFormatsCount = true;
        }
    }

    internal static void SetRawCollectionCount(XlsxStylesPart part, int target, uint count)
    {
        if (target == 1)
        {
            part.FontsCount = count;
            part.HasFontsCount = true;
        }
        else if (target == 2)
        {
            part.FillsCount = count;
            part.HasFillsCount = true;
        }
        else if (target == 3)
        {
            part.BordersCount = count;
            part.HasBordersCount = true;
        }
    }

    internal static uint FindNextCustomNumberFormatId(XlsxStylesPart part)
    {
        uint next = 164;
        foreach (XlsxNumberFormatDefinition format in part.NumberFormats)
        {
            if (format.HasNumberFormatId && format.NumberFormatId >= next)
            {
                next = format.NumberFormatId + 1;
            }
        }

        return next;
    }

    internal static string NormalizeStyleText(string? text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }
}

/// <summary>
/// Represents markup compatibility data used by the public workbook API.
/// </summary>
internal static class XlsxMarkupCompatibility
{
    internal static XlsxMarkupCompatibilityAttributes ReadAttributes(XmlReader reader)
    {
        XlsxMarkupCompatibilityAttributes attributes = new XlsxMarkupCompatibilityAttributes();
        if (!reader.HasAttributes)
        {
            return attributes;
        }

        while (reader.MoveToNextAttribute())
        {
            if (reader.Prefix == "xmlns" || reader.Name == "xmlns")
            {
                XlsxXmlNamespace namespaceDeclaration = new XlsxXmlNamespace();
                if (reader.Name == "xmlns")
                {
                    namespaceDeclaration.Prefix = "";
                }
                else
                {
                    namespaceDeclaration.Prefix = reader.LocalName;
                }

                namespaceDeclaration.NamespaceUri = reader.Value;
                attributes.NamespaceDeclarations.Add(namespaceDeclaration);
            }
            else if (reader.NamespaceURI == XlsxNamespaces.MarkupCompatibility)
            {
                if (reader.LocalName == "Ignorable")
                {
                    attributes.Ignorable = reader.Value;
                }
                else if (reader.LocalName == "ProcessContent")
                {
                    attributes.ProcessContent = reader.Value;
                }
                else if (reader.LocalName == "PreserveElements")
                {
                    attributes.PreserveElements = reader.Value;
                }
                else if (reader.LocalName == "PreserveAttributes")
                {
                    attributes.PreserveAttributes = reader.Value;
                }
                else if (reader.LocalName == "MustUnderstand")
                {
                    attributes.MustUnderstand = reader.Value;
                }
                else
                {
                    XlsxXmlAttribute attribute = CreateAttributeFromReader(reader);
                    attributes.UnknownAttributes.Add(attribute);
                }
            }
        }

        reader.MoveToElement();
        return attributes;
    }

    internal static void WriteNamespaceDeclarations(XmlWriter writer, XlsxMarkupCompatibilityAttributes attributes)
    {
        if (attributes == null)
        {
            return;
        }

        List<XlsxXmlNamespace> namespaces = new List<XlsxXmlNamespace>();
        foreach (XlsxXmlNamespace namespaceDeclaration in attributes.NamespaceDeclarations)
        {
            namespaces.Add(namespaceDeclaration);
        }

        namespaces.Sort(XlsxXmlWriterHelpers.CompareNamespacesByPrefix);
        foreach (XlsxXmlNamespace namespaceDeclaration in namespaces)
        {
            if (namespaceDeclaration.Prefix == null || namespaceDeclaration.Prefix.Length == 0)
            {
                writer.WriteAttributeString("xmlns", namespaceDeclaration.NamespaceUri);
            }
            else
            {
                writer.WriteAttributeString("xmlns", namespaceDeclaration.Prefix, "http://www.w3.org/2000/xmlns/", namespaceDeclaration.NamespaceUri);
            }
        }
    }

    internal static void WriteAttributes(XmlWriter writer, XlsxXmlNamespaceMap map, XlsxMarkupCompatibilityAttributes attributes)
    {
        if (attributes == null)
        {
            return;
        }

        if (attributes.Ignorable.Length > 0)
        {
            XlsxXmlWriterHelpers.WriteAttributeString(writer, map, XlsxNamespaces.MarkupCompatibility, "mc", "Ignorable", attributes.Ignorable);
        }

        if (attributes.ProcessContent.Length > 0)
        {
            XlsxXmlWriterHelpers.WriteAttributeString(writer, map, XlsxNamespaces.MarkupCompatibility, "mc", "ProcessContent", attributes.ProcessContent);
        }

        if (attributes.PreserveElements.Length > 0)
        {
            XlsxXmlWriterHelpers.WriteAttributeString(writer, map, XlsxNamespaces.MarkupCompatibility, "mc", "PreserveElements", attributes.PreserveElements);
        }

        if (attributes.PreserveAttributes.Length > 0)
        {
            XlsxXmlWriterHelpers.WriteAttributeString(writer, map, XlsxNamespaces.MarkupCompatibility, "mc", "PreserveAttributes", attributes.PreserveAttributes);
        }

        if (attributes.MustUnderstand.Length > 0)
        {
            XlsxXmlWriterHelpers.WriteAttributeString(writer, map, XlsxNamespaces.MarkupCompatibility, "mc", "MustUnderstand", attributes.MustUnderstand);
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, attributes.UnknownAttributes);
    }

    internal static XlsxAlternateContent ReadAlternateContent(XmlReader reader)
    {
        string rawXml = reader.ReadOuterXml();
        return ReadAlternateContentString(rawXml);
    }

    internal static XlsxAlternateContent ReadAlternateContentString(string xml)
    {
        XlsxAlternateContent content = new XlsxAlternateContent();
        content.RawXml = "";
        if (xml == null || xml.Length == 0)
        {
            content.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "mc.alternateContent.empty", "AlternateContent XML must not be empty."));
            return content;
        }

        content.RawXml = xml;
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                bool sawRoot = false;
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
                        if (reader.LocalName != "AlternateContent" || reader.NamespaceURI != XlsxNamespaces.MarkupCompatibility)
                        {
                            content.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "mc.alternateContent.invalidRoot", "AlternateContent XML must use the markup compatibility namespace."));
                        }

                        reader.Read();
                        continue;
                    }

                    if (reader.Depth == 1 && reader.NamespaceURI == XlsxNamespaces.MarkupCompatibility && reader.LocalName == "Choice")
                    {
                        content.ChoiceCount = content.ChoiceCount + 1;
                        reader.ReadOuterXml();
                        continue;
                    }

                    if (reader.Depth == 1 && reader.NamespaceURI == XlsxNamespaces.MarkupCompatibility && reader.LocalName == "Fallback")
                    {
                        content.FallbackCount = content.FallbackCount + 1;
                        reader.ReadOuterXml();
                        continue;
                    }

                    reader.ReadOuterXml();
                }

                if (!sawRoot)
                {
                    content.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "mc.alternateContent.missingRoot", "AlternateContent XML must contain a root element."));
                }
            }
        }

        content.IsValid = content.Validation.IsValid;
        return content;
    }

    internal static void WriteAlternateContent(XmlWriter writer, XlsxAlternateContent content)
    {
        if (content == null || content.RawXml == null || content.RawXml.Length == 0)
        {
            return;
        }

        writer.WriteRaw(content.RawXml);
    }

    internal static XlsxXmlAttribute CreateAttributeFromReader(XmlReader reader)
    {
        XlsxXmlAttribute attribute = new XlsxXmlAttribute();
        attribute.NamespaceUri = reader.NamespaceURI;
        attribute.LocalName = reader.LocalName;
        attribute.Prefix = reader.Prefix;
        attribute.Value = reader.Value;
        return attribute;
    }
}

internal class XlsxStreamingWorkbookPartBytes
{
    internal byte[]? ContentTypes;
    internal byte[]? PackageRelationships;
    internal byte[]? Workbook;
    internal byte[]? WorkbookRelationships;
    internal byte[]? CoreProperties;
    internal byte[]? ExtendedProperties;
    internal byte[]? CustomProperties;

    internal XlsxStreamingWorkbookPartBytes()
    {
        ContentTypes = null;
        PackageRelationships = null;
        Workbook = null;
        WorkbookRelationships = null;
        CoreProperties = null;
        ExtendedProperties = null;
        CustomProperties = null;
    }
}

internal class XlsxFormulaAggregateState
{
    internal double Total;
    internal double Minimum;
    internal double Maximum;
    internal int Count;

    internal XlsxFormulaAggregateState()
    {
        Total = 0;
        Minimum = 0;
        Maximum = 0;
        Count = 0;
    }
}

internal class XlsxFormulaLogicalState
{
    internal bool SawValue;
    internal bool Result;

    internal XlsxFormulaLogicalState()
    {
        SawValue = false;
        Result = false;
    }
}

internal class XlsxFormulaNumberList
{
    internal List<double> Numbers;

    internal XlsxFormulaNumberList()
    {
        Numbers = new List<double>();
    }
}

internal class XlsxFormulaCalculationEngine
{
    internal XlsxWorksheetPart? Worksheet;
    internal XlsxWorkbook? Workbook;
    internal string WorksheetName;
    internal XlsxFormulaCalculationOptions Options;
    internal Dictionary<string, XlsxFormulaCalculationValue> Variables;
    internal List<string> ResolvingDefinedNames;
    internal XlsxFormulaCalculationResult? CurrentResult;

    internal XlsxFormulaCalculationEngine()
    {
        Worksheet = null;
        Workbook = null;
        WorksheetName = "";
        Options = new XlsxFormulaCalculationOptions();
        Variables = new Dictionary<string, XlsxFormulaCalculationValue>(System.StringComparer.OrdinalIgnoreCase);
        ResolvingDefinedNames = new List<string>();
        CurrentResult = null;
    }

    internal static XlsxFormulaCalculationEngine CreateForWorksheet(XlsxWorksheetPart worksheet)
    {
        XlsxFormulaCalculationEngine engine = new XlsxFormulaCalculationEngine();
        engine.Worksheet = worksheet;
        return engine;
    }

    internal static XlsxFormulaCalculationEngine CreateForWorkbook(XlsxWorkbook workbook, string sheetName, XlsxFormulaCalculationOptions options)
    {
        XlsxFormulaCalculationEngine engine = new XlsxFormulaCalculationEngine();
        engine.Workbook = workbook;
        engine.WorksheetName = XlsxCellValueConverter.NormalizeText(sheetName);
        engine.Options = options;
        engine.Worksheet = engine.FindWorksheet(engine.WorksheetName);
        return engine;
    }

    internal XlsxFormulaCalculationResult EvaluateFormula(string formula)
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationResult();
        result.Formula = XlsxCellValueConverter.NormalizeText(formula);
        Variables.Clear();
        ResolvingDefinedNames.Clear();
        CurrentResult = result;
        XlsxFormulaAnalysisResult analysis = XlsxFormulaService.AnalyzeFormulaCore(formula);
        CopyFormulaAnalysisReport(result, analysis);
        if (!analysis.IsValid)
        {
            result.Value = XlsxFormulaCalculationValue.CreateError("#VALUE!");
            return result;
        }
        if (analysis.HasUnsafeFunction && !Options.AllowUnsafeFunctions)
        {
            result.AddError("formulaCalculation.unsafeFunctionBlocked", "Formula contains a function classified as unsafe by the calculation policy.");
            result.Value = XlsxFormulaCalculationValue.CreateError("#BLOCKED!");
            return result;
        }
        if (analysis.HasVolatileFunction && !Options.AllowVolatileFunctions)
        {
            result.AddError("formulaCalculation.volatileFunctionBlocked", "Formula contains a volatile function disabled by the calculation policy.");
            result.Value = XlsxFormulaCalculationValue.CreateError("#BLOCKED!");
            return result;
        }
        if (analysis.CrossSheetReferences.Count > 0 && !Options.AllowCrossSheetReferences)
        {
            result.AddError("formulaCalculation.crossSheetReferenceBlocked", "Formula contains a cross-sheet reference disabled by the calculation policy.");
            result.Value = XlsxFormulaCalculationValue.CreateError("#REF!");
            return result;
        }
        XlsxFormulaParseResult parseResult = new XlsxFormulaParser().Parse(formula);
        if (!parseResult.IsValid)
        {
            result.Value = XlsxFormulaCalculationValue.CreateError("#VALUE!");
            return result;
        }

        if (!parseResult.HasRoot || parseResult.Root == null)
        {
            result.AddError("formulaCalculation.rootMissing", "Formula parse result must contain a root node.");
            result.Value = XlsxFormulaCalculationValue.CreateError("#VALUE!");
            return result;
        }

        result.Value = EvaluateNode(parseResult.Root);
        return result;
    }

    internal XlsxFormulaCalculationValue EvaluateNode(XlsxFormulaNode node)
    {
        if (node == null)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        if (node.Kind == XlsxFormulaNodeKind.Literal)
        {
            return EvaluateLiteral(node.Text);
        }

        if (node.Kind == XlsxFormulaNodeKind.Reference)
        {
            return EvaluateReference(node.Text);
        }

        if (node.Kind == XlsxFormulaNodeKind.Range)
        {
            return EvaluateRange(node);
        }

        if (node.Kind == XlsxFormulaNodeKind.UnaryOperation)
        {
            return EvaluateUnary(node);
        }

        if (node.Kind == XlsxFormulaNodeKind.BinaryOperation)
        {
            return EvaluateBinary(node);
        }

        if (node.Kind == XlsxFormulaNodeKind.FunctionCall)
        {
            return EvaluateFunction(node);
        }

        if (node.Kind == XlsxFormulaNodeKind.ArrayConstant)
        {
            return EvaluateArrayConstant(node);
        }

        if (node.Kind == XlsxFormulaNodeKind.Name)
        {
            if (Variables.ContainsKey(node.Text))
            {
                return Variables[node.Text];
            }

            return EvaluateDefinedName(node.Text);
        }

        if (node.Kind == XlsxFormulaNodeKind.ExternalReference)
        {
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }

        if (node.Kind == XlsxFormulaNodeKind.StructuredReference)
        {
            return XlsxFormulaCalculationValue.CreateError("#NAME?");
        }

        return XlsxFormulaCalculationValue.CreateError("#VALUE!");
    }

    internal XlsxFormulaCalculationValue EvaluateDefinedName(string name)
    {
        XlsxWorkbookDefinedName? definition = FindDefinedName(name);
        if (definition == null)
        {
            return XlsxFormulaCalculationValue.CreateError("#NAME?");
        }

        string key = GetDefinedNameKey(definition);
        if (ContainsResolvingDefinedName(key))
        {
            if (CurrentResult != null)
            {
                CurrentResult.AddError("formulaCalculation.definedNameCircular", "Defined name formulas must not contain a circular name reference.");
            }
            return XlsxFormulaCalculationValue.CreateError("#NAME?");
        }

        ResolvingDefinedNames.Add(key);
        XlsxFormulaCalculationValue value = EvaluateDefinedNameText(definition.Text);
        ResolvingDefinedNames.RemoveAt(ResolvingDefinedNames.Count - 1);
        return value;
    }

    internal XlsxWorkbookDefinedName? FindDefinedName(string name)
    {
        if (Workbook == null || Workbook.WorkbookPart == null || Workbook.WorkbookPart.DefinedNames == null)
        {
            return null;
        }

        int sheetIndex = GetDefinedNameScopeIndex();
        XlsxWorkbookDefinedName? globalDefinition = null;
        foreach (XlsxWorkbookDefinedName definition in Workbook.WorkbookPart.DefinedNames)
        {
            if (definition != null && string.Equals(XlsxCellValueConverter.NormalizeText(definition.Name), name, System.StringComparison.OrdinalIgnoreCase))
            {
                if (definition.HasLocalSheetId && definition.LocalSheetId == sheetIndex)
                {
                    return definition;
                }
                if (!definition.HasLocalSheetId && globalDefinition == null)
                {
                    globalDefinition = definition;
                }
            }
        }
        return globalDefinition;
    }

    internal int GetDefinedNameScopeIndex()
    {
        if (Workbook == null || Workbook.Worksheets == null)
        {
            return -1;
        }

        int index = 0;
        while (index < Workbook.Worksheets.Count)
        {
            if (Workbook.Worksheets[index] == Worksheet || string.Equals(XlsxFormulaService.GetSheetName(Workbook, index), WorksheetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static string GetDefinedNameKey(XlsxWorkbookDefinedName definition)
    {
        string key = XlsxCellValueConverter.NormalizeText(definition.Name).ToUpperInvariant();
        if (definition.HasLocalSheetId)
        {
            key = definition.LocalSheetId.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + key;
        }
        else
        {
            key = "GLOBAL:" + key;
        }
        return key;
    }

    internal bool ContainsResolvingDefinedName(string key)
    {
        foreach (string item in ResolvingDefinedNames)
        {
            if (string.Equals(item, key, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    internal XlsxFormulaCalculationValue EvaluateDefinedNameText(string text)
    {
        string formula = XlsxCellValueConverter.NormalizeText(text);
        if (formula.StartsWith("=", System.StringComparison.Ordinal))
        {
            formula = formula.Substring(1);
        }
        if (formula.Length == 0)
        {
            return XlsxFormulaCalculationValue.CreateError("#NAME?");
        }

        XlsxFormulaAnalysisResult analysis = XlsxFormulaService.AnalyzeFormulaCore(formula);
        if (CurrentResult != null)
        {
            CopyFormulaAnalysisReport(CurrentResult, analysis);
        }
        if (!analysis.IsValid)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }
        if (analysis.HasUnsafeFunction && !Options.AllowUnsafeFunctions)
        {
            if (CurrentResult != null)
            {
                CurrentResult.AddError("formulaCalculation.unsafeFunctionBlocked", "Defined name formula contains a function classified as unsafe by the calculation policy.");
            }
            return XlsxFormulaCalculationValue.CreateError("#BLOCKED!");
        }
        if (analysis.HasVolatileFunction && !Options.AllowVolatileFunctions)
        {
            if (CurrentResult != null)
            {
                CurrentResult.AddError("formulaCalculation.volatileFunctionBlocked", "Defined name formula contains a volatile function disabled by the calculation policy.");
            }
            return XlsxFormulaCalculationValue.CreateError("#BLOCKED!");
        }
        if (analysis.CrossSheetReferences.Count > 0 && !Options.AllowCrossSheetReferences)
        {
            if (CurrentResult != null)
            {
                CurrentResult.AddError("formulaCalculation.crossSheetReferenceBlocked", "Defined name formula contains a cross-sheet reference disabled by the calculation policy.");
            }
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }

        XlsxFormulaParseResult parsed = new XlsxFormulaParser().Parse(formula);
        if (!parsed.IsValid || !parsed.HasRoot || parsed.Root == null)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }
        return EvaluateNode(parsed.Root);
    }

    internal XlsxFormulaCalculationValue EvaluateLiteral(string text)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(text);
        if (normalized.Length == 0)
        {
            return XlsxFormulaCalculationValue.CreateBlank();
        }

        if (normalized[0] == '"')
        {
            return XlsxFormulaCalculationValue.CreateText(UnquoteFormulaString(normalized));
        }

        if (IsFormulaErrorText(normalized))
        {
            return XlsxFormulaCalculationValue.CreateError(normalized);
        }

        if (string.Equals(normalized, "TRUE", System.StringComparison.OrdinalIgnoreCase))
        {
            return XlsxFormulaCalculationValue.CreateBoolean(true);
        }

        if (string.Equals(normalized, "FALSE", System.StringComparison.OrdinalIgnoreCase))
        {
            return XlsxFormulaCalculationValue.CreateBoolean(false);
        }

        XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseDouble(normalized);
        if (parseResult.IsValid)
        {
            return XlsxFormulaCalculationValue.CreateNumber(parseResult.DoubleValue);
        }

        return XlsxFormulaCalculationValue.CreateError("#VALUE!");
    }

    internal XlsxFormulaCalculationValue EvaluateReference(string text)
    {
        XlsxReferenceResolutionResult resolved = XlsxWorkbookReferenceService.ParseA1Core(text);
        if (resolved.IsExternal)
        {
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }
        XlsxWorksheetPart? targetWorksheet = Worksheet;
        if (resolved.IsValid && resolved.HasSheetName && Workbook != null)
        {
            if (!Options.AllowCrossSheetReferences)
            {
                return XlsxFormulaCalculationValue.CreateError("#REF!");
            }
            targetWorksheet = FindWorksheet(resolved.FirstSheetName);
        }

        string referenceText = GetFormulaReferenceTarget(text);
        if (referenceText == "#REF!")
        {
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }

        XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(referenceText);
        if (!reference.IsValid)
        {
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }

        if (targetWorksheet == null)
        {
            return XlsxFormulaCalculationValue.CreateBlank();
        }

        XlsxWorksheetCell? cell = targetWorksheet.GetCellAt(reference.Row, reference.Column);
        if (cell == null)
        {
            return XlsxFormulaCalculationValue.CreateBlank();
        }

        XlsxCellValueResult valueResult = XlsxCellValueConverter.FromWorksheetCell(cell);
        if (!valueResult.IsValid)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        return FromCellValue(valueResult.Value);
    }

    internal XlsxFormulaCalculationValue EvaluateRange(XlsxFormulaNode node)
    {
        if (node.Children.Count < 2)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxReferenceResolutionResult resolvedRange = XlsxWorkbookReferenceService.ParseA1Core(node.Text);
        if (resolvedRange.IsExternal)
        {
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }
        XlsxWorksheetPart? targetWorksheet = Worksheet;
        string rangeSheetName = "";
        if (resolvedRange.IsValid && resolvedRange.HasSheetName)
        {
            rangeSheetName = resolvedRange.FirstSheetName;
            if (Workbook != null)
            {
                if (!Options.AllowCrossSheetReferences)
                {
                    return XlsxFormulaCalculationValue.CreateError("#REF!");
                }
                targetWorksheet = FindWorksheet(rangeSheetName);
            }
        }

        string firstText = GetFormulaReferenceTarget(node.Children[0].Text);
        string lastText = GetFormulaReferenceTarget(node.Children[1].Text);
        XlsxA1CellReferenceResult first = XlsxA1CellReferenceConverter.Parse(firstText);
        XlsxA1CellReferenceResult last = XlsxA1CellReferenceConverter.Parse(lastText);
        if (!first.IsValid || !last.IsValid)
        {
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }

        int minRow = first.Row;
        int maxRow = last.Row;
        int minColumn = first.Column;
        int maxColumn = last.Column;
        if (minRow > maxRow)
        {
            int temporary = minRow;
            minRow = maxRow;
            maxRow = temporary;
        }

        if (minColumn > maxColumn)
        {
            int temporary = minColumn;
            minColumn = maxColumn;
            maxColumn = temporary;
        }

        int cellCount = (maxRow - minRow + 1) * (maxColumn - minColumn + 1);
        if (cellCount > 10000)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue range = XlsxFormulaCalculationValue.CreateRange();
        range.Rows = maxRow - minRow + 1;
        range.Columns = maxColumn - minColumn + 1;
        int row = minRow;
        while (row <= maxRow)
        {
            int column = minColumn;
            while (column <= maxColumn)
            {
                string reference = XlsxA1CellReferenceConverter.FormatString(row, column, false, false);
                if (targetWorksheet == null)
                {
                    range.Items.Add(XlsxFormulaCalculationValue.CreateBlank());
                }
                else
                {
                    XlsxWorksheetCell? cell = targetWorksheet.GetCellAt(row, column);
                    if (cell == null)
                    {
                        range.Items.Add(XlsxFormulaCalculationValue.CreateBlank());
                    }
                    else
                    {
                        XlsxCellValueResult valueResult = XlsxCellValueConverter.FromWorksheetCell(cell);
                        if (valueResult.IsValid)
                        {
                            range.Items.Add(FromCellValue(valueResult.Value));
                        }
                        else
                        {
                            range.Items.Add(XlsxFormulaCalculationValue.CreateError("#VALUE!"));
                        }
                    }
                }
                column = column + 1;
            }

            row = row + 1;
        }

        return range;
    }

    internal XlsxFormulaCalculationValue EvaluateUnary(XlsxFormulaNode node)
    {
        XlsxFormulaCalculationValue operand = XlsxFormulaCalculationValue.CreateBlank();
        if (node.Children.Count > 0)
        {
            operand = EvaluateNode(node.Children[0]);
        }

        if (operand.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return operand;
        }

        if (node.Operator == "@")
        {
            return Scalarize(operand);
        }

        double number = CoerceToNumber(Scalarize(operand));
        if (double.IsNaN(number))
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        if (node.Operator == "-")
        {
            return XlsxFormulaCalculationValue.CreateNumber(0 - number);
        }

        if (node.Operator == "%")
        {
            return XlsxFormulaCalculationValue.CreateNumber(number / 100);
        }

        return XlsxFormulaCalculationValue.CreateNumber(number);
    }

    internal XlsxFormulaCalculationValue EvaluateBinary(XlsxFormulaNode node)
    {
        XlsxFormulaCalculationValue left = XlsxFormulaCalculationValue.CreateBlank();
        XlsxFormulaCalculationValue right = XlsxFormulaCalculationValue.CreateBlank();
        if (node.Children.Count > 0)
        {
            left = Scalarize(EvaluateNode(node.Children[0]));
        }

        if (node.Children.Count > 1)
        {
            right = Scalarize(EvaluateNode(node.Children[1]));
        }

        XlsxFormulaCalculationValue? error = FirstError(left, right);
        if (error != null)
        {
            return error;
        }

        if (node.Operator == "&")
        {
            return EvaluateConcatenation(left, right);
        }

        if (IsComparisonOperator(node.Operator))
        {
            return EvaluateComparison(left, right, node.Operator);
        }

        return EvaluateArithmetic(left, right, node.Operator);
    }

    internal XlsxFormulaCalculationValue EvaluateArithmetic(XlsxFormulaCalculationValue left, XlsxFormulaCalculationValue right, string op)
    {
        double leftNumber = CoerceToNumber(left);
        double rightNumber = CoerceToNumber(right);
        if (double.IsNaN(leftNumber) || double.IsNaN(rightNumber))
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        if (op == "+")
        {
            return XlsxFormulaCalculationValue.CreateNumber(leftNumber + rightNumber);
        }

        if (op == "-")
        {
            return XlsxFormulaCalculationValue.CreateNumber(leftNumber - rightNumber);
        }

        if (op == "*")
        {
            return XlsxFormulaCalculationValue.CreateNumber(leftNumber * rightNumber);
        }

        if (op == "/")
        {
            if (rightNumber == 0)
            {
                return XlsxFormulaCalculationValue.CreateError("#DIV/0!");
            }

            return XlsxFormulaCalculationValue.CreateNumber(leftNumber / rightNumber);
        }

        if (op == "^")
        {
            return XlsxFormulaCalculationValue.CreateNumber(System.Math.Pow(leftNumber, rightNumber));
        }

        return XlsxFormulaCalculationValue.CreateError("#VALUE!");
    }

    internal XlsxFormulaCalculationValue EvaluateConcatenation(XlsxFormulaCalculationValue left, XlsxFormulaCalculationValue right)
    {
        string leftText = CoerceToText(left);
        string rightText = CoerceToText(right);
        return XlsxFormulaCalculationValue.CreateText(leftText + rightText);
    }

    internal XlsxFormulaCalculationValue EvaluateComparison(XlsxFormulaCalculationValue left, XlsxFormulaCalculationValue right, string op)
    {
        int comparison = CompareValues(left, right);
        if (op == "=")
        {
            return XlsxFormulaCalculationValue.CreateBoolean(comparison == 0);
        }

        if (op == "<>")
        {
            return XlsxFormulaCalculationValue.CreateBoolean(comparison != 0);
        }

        if (op == "<")
        {
            return XlsxFormulaCalculationValue.CreateBoolean(comparison < 0);
        }

        if (op == "<=")
        {
            return XlsxFormulaCalculationValue.CreateBoolean(comparison <= 0);
        }

        if (op == ">")
        {
            return XlsxFormulaCalculationValue.CreateBoolean(comparison > 0);
        }

        if (op == ">=")
        {
            return XlsxFormulaCalculationValue.CreateBoolean(comparison >= 0);
        }

        return XlsxFormulaCalculationValue.CreateError("#VALUE!");
    }

    internal XlsxFormulaCalculationValue EvaluateFunction(XlsxFormulaNode node)
    {
        string name = XlsxFormulaService.NormalizeFutureFunctionName(node.Operator);
        if (name == "SUM" || name == "AVERAGE" || name == "COUNT" || name == "MIN" || name == "MAX")
        {
            return EvaluateAggregateFunction(node, name);
        }

        if (name == "MEDIAN" || name == "STDEV.S" || name == "VAR.S" || name == "PERCENTILE.INC" || name == "LARGE" || name == "SMALL")
        {
            return EvaluateStatisticalFunction(node, name);
        }

        if (name == "PMT" || name == "FV" || name == "PV" || name == "NPV")
        {
            return EvaluateFinancialFunction(node, name);
        }

        if (name == "LET")
        {
            return EvaluateLetFunction(node);
        }

        if (name == "LAMBDA")
        {
            return EvaluateLambdaFunction(node);
        }

        if (name == "IF")
        {
            return EvaluateIfFunction(node);
        }

        if (name == "IFERROR")
        {
            return EvaluateIfErrorFunction(node);
        }

        if (name == "AND" || name == "OR" || name == "NOT")
        {
            return EvaluateLogicalFunction(node, name);
        }

        if (name == "LEN" || name == "LEFT" || name == "RIGHT" || name == "MID" || name == "UPPER" || name == "LOWER" || name == "TRIM" || name == "CONCAT" || name == "TEXTJOIN")
        {
            return EvaluateTextFunction(node, name);
        }

        if (name == "DATE" || name == "YEAR" || name == "MONTH" || name == "DAY" || name == "TIME")
        {
            return EvaluateDateTimeFunction(node, name);
        }

        if (name == "ABS" || name == "ROUND" || name == "INT" || name == "SQRT" || name == "POWER" || name == "MOD")
        {
            return EvaluateMathFunction(node, name);
        }

        if (name == "INDEX" || name == "MATCH" || name == "VLOOKUP" || name == "XLOOKUP")
        {
            return EvaluateLookupFunction(node, name);
        }

        if (name == "SEQUENCE" || name == "FILTER" || name == "TRANSPOSE")
        {
            return EvaluateDynamicArrayFunction(node, name);
        }

        return XlsxFormulaCalculationValue.CreateError("#NAME?");
    }

    internal XlsxWorksheetPart? FindWorksheet(string sheetName)
    {
        if (Workbook == null || Workbook.Worksheets == null)
        {
            return Worksheet;
        }
        int index = 0;
        while (index < Workbook.Worksheets.Count)
        {
            if (string.Equals(XlsxFormulaService.GetSheetName(Workbook, index), sheetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return Workbook.Worksheets[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static void CopyFormulaAnalysisReport(XlsxFormulaCalculationResult result, XlsxFormulaAnalysisResult analysis)
    {
        foreach (XlsxDiagnostic diagnostic in analysis.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
        foreach (string name in analysis.UnsupportedFunctions)
        {
            XlsxFormulaService.AddUnique(result.UnsupportedFunctions, name);
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, "formulaCalculation.functionUnsupported", "Formula function is not supported by the built-in calculation engine: " + name + "."));
        }
        foreach (string name in analysis.FutureFunctions)
        {
            XlsxFormulaService.AddUnique(result.FutureFunctions, name);
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, "formulaCalculation.futureSyntax", "Formula uses a future-function prefix: " + name + "."));
        }
        foreach (string name in analysis.VolatileFunctions)
        {
            XlsxFormulaService.AddUnique(result.VolatileFunctions, name);
        }
        foreach (string name in analysis.UnsafeFunctions)
        {
            XlsxFormulaService.AddUnique(result.UnsafeFunctions, name);
        }
        foreach (string reference in analysis.ExternalReferences)
        {
            XlsxFormulaService.AddUnique(result.ExternalReferences, reference);
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, "formulaCalculation.externalReferenceUnsupported", "External-workbook references are not evaluated: " + reference + "."));
        }
    }

    internal XlsxFormulaCalculationValue EvaluateStatisticalFunction(XlsxFormulaNode node, string name)
    {
        XlsxFormulaNumberList list = new XlsxFormulaNumberList();
        int argumentLimit = node.Children.Count;
        if (name == "PERCENTILE.INC" || name == "LARGE" || name == "SMALL")
        {
            argumentLimit = node.Children.Count - 1;
        }

        int index = 0;
        while (index < argumentLimit)
        {
            XlsxFormulaCalculationValue value = EvaluateNode(node.Children[index]);
            XlsxFormulaCalculationValue? error = CollectNumericValues(value, list);
            if (error != null)
            {
                return error;
            }

            index = index + 1;
        }

        if (list.Numbers.Count == 0)
        {
            return XlsxFormulaCalculationValue.CreateError("#DIV/0!");
        }

        list.Numbers.Sort();
        if (name == "MEDIAN")
        {
            return EvaluateMedian(list);
        }

        if (name == "VAR.S" || name == "STDEV.S")
        {
            return EvaluateSampleVariance(list, name);
        }

        if (node.Children.Count < 2)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        double selector = CoerceToNumber(Scalarize(EvaluateNode(node.Children[node.Children.Count - 1])));
        if (name == "PERCENTILE.INC")
        {
            return EvaluatePercentileInc(list, selector);
        }

        if (name == "LARGE")
        {
            return EvaluateRankedValue(list, (int)selector, false);
        }

        if (name == "SMALL")
        {
            return EvaluateRankedValue(list, (int)selector, true);
        }

        return XlsxFormulaCalculationValue.CreateError("#NAME?");
    }

    internal XlsxFormulaCalculationValue EvaluateMedian(XlsxFormulaNumberList list)
    {
        int count = list.Numbers.Count;
        int middle = count / 2;
        if (count % 2 == 1)
        {
            return XlsxFormulaCalculationValue.CreateNumber(list.Numbers[middle]);
        }

        return XlsxFormulaCalculationValue.CreateNumber((list.Numbers[middle - 1] + list.Numbers[middle]) / 2);
    }

    internal XlsxFormulaCalculationValue EvaluateSampleVariance(XlsxFormulaNumberList list, string name)
    {
        if (list.Numbers.Count < 2)
        {
            return XlsxFormulaCalculationValue.CreateError("#DIV/0!");
        }

        double total = 0;
        foreach (double number in list.Numbers)
        {
            total = total + number;
        }

        double average = total / list.Numbers.Count;
        double sumSquares = 0;
        foreach (double number in list.Numbers)
        {
            double difference = number - average;
            sumSquares = sumSquares + difference * difference;
        }

        double variance = sumSquares / (list.Numbers.Count - 1);
        if (name == "STDEV.S")
        {
            return XlsxFormulaCalculationValue.CreateNumber(System.Math.Sqrt(variance));
        }

        return XlsxFormulaCalculationValue.CreateNumber(variance);
    }

    internal XlsxFormulaCalculationValue EvaluatePercentileInc(XlsxFormulaNumberList list, double percentile)
    {
        if (double.IsNaN(percentile) || percentile < 0 || percentile > 1)
        {
            return XlsxFormulaCalculationValue.CreateError("#NUM!");
        }

        if (list.Numbers.Count == 1)
        {
            return XlsxFormulaCalculationValue.CreateNumber(list.Numbers[0]);
        }

        double position = (list.Numbers.Count - 1) * percentile;
        int lower = (int)System.Math.Floor(position);
        int upper = (int)System.Math.Ceiling(position);
        if (lower == upper)
        {
            return XlsxFormulaCalculationValue.CreateNumber(list.Numbers[lower]);
        }

        double fraction = position - lower;
        return XlsxFormulaCalculationValue.CreateNumber(list.Numbers[lower] + (list.Numbers[upper] - list.Numbers[lower]) * fraction);
    }

    internal XlsxFormulaCalculationValue EvaluateRankedValue(XlsxFormulaNumberList list, int rank, bool smallest)
    {
        if (rank <= 0 || rank > list.Numbers.Count)
        {
            return XlsxFormulaCalculationValue.CreateError("#NUM!");
        }

        if (smallest)
        {
            return XlsxFormulaCalculationValue.CreateNumber(list.Numbers[rank - 1]);
        }

        return XlsxFormulaCalculationValue.CreateNumber(list.Numbers[list.Numbers.Count - rank]);
    }

    internal XlsxFormulaCalculationValue EvaluateFinancialFunction(XlsxFormulaNode node, string name)
    {
        if (name == "NPV")
        {
            return EvaluateNpvFunction(node);
        }

        if (node.Children.Count < 3)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        double rate = CoerceToNumber(Scalarize(EvaluateNode(node.Children[0])));
        double periods = CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
        double payment = 0;
        double presentValue = 0;
        double futureValue = 0;
        double type = 0;
        if (name == "PMT")
        {
            presentValue = CoerceToNumber(Scalarize(EvaluateNode(node.Children[2])));
            if (node.Children.Count > 3)
            {
                futureValue = CoerceToNumber(Scalarize(EvaluateNode(node.Children[3])));
            }
        }
        else
        {
            payment = CoerceToNumber(Scalarize(EvaluateNode(node.Children[2])));
            if (name == "FV" && node.Children.Count > 3)
            {
                presentValue = CoerceToNumber(Scalarize(EvaluateNode(node.Children[3])));
            }

            if (name == "PV" && node.Children.Count > 3)
            {
                futureValue = CoerceToNumber(Scalarize(EvaluateNode(node.Children[3])));
            }
        }

        if (node.Children.Count > 4)
        {
            type = CoerceToNumber(Scalarize(EvaluateNode(node.Children[4])));
        }

        if (periods == 0)
        {
            return XlsxFormulaCalculationValue.CreateError("#NUM!");
        }

        if (name == "PMT")
        {
            return XlsxFormulaCalculationValue.CreateNumber(CalculatePmt(rate, periods, presentValue, futureValue, type));
        }

        if (name == "FV")
        {
            return XlsxFormulaCalculationValue.CreateNumber(CalculateFv(rate, periods, payment, presentValue, type));
        }

        if (name == "PV")
        {
            return XlsxFormulaCalculationValue.CreateNumber(CalculatePv(rate, periods, payment, futureValue, type));
        }

        return XlsxFormulaCalculationValue.CreateError("#NAME?");
    }

    internal XlsxFormulaCalculationValue EvaluateNpvFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 2)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        double rate = CoerceToNumber(Scalarize(EvaluateNode(node.Children[0])));
        double total = 0;
        int period = 1;
        int index = 1;
        while (index < node.Children.Count)
        {
            XlsxFormulaNumberList list = new XlsxFormulaNumberList();
            XlsxFormulaCalculationValue? error = CollectNumericValues(EvaluateNode(node.Children[index]), list);
            if (error != null)
            {
                return error;
            }

            foreach (double number in list.Numbers)
            {
                total = total + number / System.Math.Pow(1 + rate, period);
                period = period + 1;
            }

            index = index + 1;
        }

        return XlsxFormulaCalculationValue.CreateNumber(total);
    }

    internal XlsxFormulaCalculationValue EvaluateLetFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 3 || node.Children.Count % 2 == 0)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        int index = 0;
        while (index < node.Children.Count - 1)
        {
            XlsxFormulaNode nameNode = node.Children[index];
            if (nameNode.Kind != XlsxFormulaNodeKind.Name || nameNode.Text == null || nameNode.Text.Length == 0)
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            Variables[nameNode.Text] = EvaluateNode(node.Children[index + 1]);
            index = index + 2;
        }

        return EvaluateNode(node.Children[node.Children.Count - 1]);
    }

    internal XlsxFormulaCalculationValue EvaluateLambdaFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 1)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue lambda = XlsxFormulaCalculationValue.CreateLambda();
        int index = 0;
        while (index < node.Children.Count - 1)
        {
            XlsxFormulaNode parameter = node.Children[index];
            if (parameter.Kind != XlsxFormulaNodeKind.Name || parameter.Text == null || parameter.Text.Length == 0)
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            lambda.LambdaParameters.Add(parameter.Text);
            index = index + 1;
        }

        lambda.LambdaBody = node.Children[node.Children.Count - 1];
        lambda.Text = XlsxFormulaWriter.WriteNodeText(lambda.LambdaBody);
        return lambda;
    }

    internal XlsxFormulaCalculationValue EvaluateIfFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 2)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue condition = Scalarize(EvaluateNode(node.Children[0]));
        if (condition.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return condition;
        }

        if (CoerceToBoolean(condition))
        {
            return EvaluateNode(node.Children[1]);
        }

        if (node.Children.Count > 2)
        {
            return EvaluateNode(node.Children[2]);
        }

        return XlsxFormulaCalculationValue.CreateBoolean(false);
    }

    internal XlsxFormulaCalculationValue EvaluateIfErrorFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 2)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue value = EvaluateNode(node.Children[0]);
        if (Scalarize(value).Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return EvaluateNode(node.Children[1]);
        }

        return value;
    }

    internal XlsxFormulaCalculationValue EvaluateLogicalFunction(XlsxFormulaNode node, string name)
    {
        if (name == "NOT")
        {
            if (node.Children.Count < 1)
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            XlsxFormulaCalculationValue value = Scalarize(EvaluateNode(node.Children[0]));
            if (value.Kind == XlsxFormulaCalculationValueKind.Error)
            {
                return value;
            }

            return XlsxFormulaCalculationValue.CreateBoolean(!CoerceToBoolean(value));
        }

        XlsxFormulaLogicalState state = new XlsxFormulaLogicalState();
        state.Result = name == "AND";
        foreach (XlsxFormulaNode child in node.Children)
        {
            XlsxFormulaCalculationValue value = EvaluateNode(child);
            XlsxFormulaCalculationValue? error = ApplyLogicalValue(value, name, state);
            if (error != null)
            {
                return error;
            }
        }

        if (!state.SawValue)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        return XlsxFormulaCalculationValue.CreateBoolean(state.Result);
    }

    internal XlsxFormulaCalculationValue? ApplyLogicalValue(XlsxFormulaCalculationValue value, string name, XlsxFormulaLogicalState state)
    {
        if (value.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return value;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Range)
        {
            foreach (XlsxFormulaCalculationValue item in value.Items)
            {
                XlsxFormulaCalculationValue? error = ApplyLogicalValue(item, name, state);
                if (error != null)
                {
                    return error;
                }
            }

            return null;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Blank || value.Kind == XlsxFormulaCalculationValueKind.Text)
        {
            return null;
        }

        state.SawValue = true;
        bool boolean = CoerceToBoolean(value);
        if (name == "AND" && !boolean)
        {
            state.Result = false;
        }

        if (name == "OR" && boolean)
        {
            state.Result = true;
        }

        return null;
    }

    internal XlsxFormulaCalculationValue EvaluateTextFunction(XlsxFormulaNode node, string name)
    {
        if (name == "CONCAT")
        {
            return EvaluateConcatFunction(node);
        }

        if (name == "TEXTJOIN")
        {
            return EvaluateTextJoinFunction(node);
        }

        if (node.Children.Count < 1)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue first = Scalarize(EvaluateNode(node.Children[0]));
        if (first.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return first;
        }

        string text = CoerceToText(first);
        if (name == "LEN")
        {
            return XlsxFormulaCalculationValue.CreateNumber(text.Length);
        }

        if (name == "UPPER")
        {
            return XlsxFormulaCalculationValue.CreateText(text.ToUpperInvariant());
        }

        if (name == "LOWER")
        {
            return XlsxFormulaCalculationValue.CreateText(text.ToLowerInvariant());
        }

        if (name == "TRIM")
        {
            return XlsxFormulaCalculationValue.CreateText(text.Trim());
        }

        if (name == "LEFT")
        {
            int count = 1;
            if (node.Children.Count > 1)
            {
                count = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
            }

            return XlsxFormulaCalculationValue.CreateText(LeftText(text, count));
        }

        if (name == "RIGHT")
        {
            int count = 1;
            if (node.Children.Count > 1)
            {
                count = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
            }

            return XlsxFormulaCalculationValue.CreateText(RightText(text, count));
        }

        if (name == "MID")
        {
            if (node.Children.Count < 3)
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            int start = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
            int count = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[2])));
            return XlsxFormulaCalculationValue.CreateText(MidText(text, start, count));
        }

        return XlsxFormulaCalculationValue.CreateError("#NAME?");
    }

    internal XlsxFormulaCalculationValue EvaluateConcatFunction(XlsxFormulaNode node)
    {
        StringBuilder builder = new StringBuilder();
        foreach (XlsxFormulaNode child in node.Children)
        {
            XlsxFormulaCalculationValue? error = AppendTextArgument(builder, EvaluateNode(child), "", false, false);
            if (error != null)
            {
                return error;
            }
        }

        return XlsxFormulaCalculationValue.CreateText(builder.ToString());
    }

    internal XlsxFormulaCalculationValue EvaluateTextJoinFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 3)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue delimiterValue = Scalarize(EvaluateNode(node.Children[0]));
        XlsxFormulaCalculationValue ignoreValue = Scalarize(EvaluateNode(node.Children[1]));
        if (delimiterValue.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return delimiterValue;
        }

        if (ignoreValue.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return ignoreValue;
        }

        string delimiter = CoerceToText(delimiterValue);
        bool ignoreEmpty = CoerceToBoolean(ignoreValue);
        StringBuilder builder = new StringBuilder();
        bool appended = false;
        int index = 2;
        while (index < node.Children.Count)
        {
            XlsxFormulaCalculationValue? error = AppendTextArgument(builder, EvaluateNode(node.Children[index]), delimiter, ignoreEmpty, appended);
            if (error != null)
            {
                return error;
            }

            if (builder.Length > 0)
            {
                appended = true;
            }

            index = index + 1;
        }

        return XlsxFormulaCalculationValue.CreateText(builder.ToString());
    }

    internal XlsxFormulaCalculationValue EvaluateDateTimeFunction(XlsxFormulaNode node, string name)
    {
        if (name == "DATE")
        {
            if (node.Children.Count < 3)
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            int year = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[0])));
            int month = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
            int day = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[2])));
            if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1 || day > System.DateTime.DaysInMonth(year, month))
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            XlsxDateSerialConversionResult serial = XlsxDateSerialConverter.ToSerial(new System.DateTime(year, month, day), false);
            if (!serial.IsValid)
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            return XlsxFormulaCalculationValue.CreateNumber(serial.SerialValue);
        }

        if (name == "TIME")
        {
            if (node.Children.Count < 3)
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            double hour = CoerceToNumber(Scalarize(EvaluateNode(node.Children[0])));
            double minute = CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
            double second = CoerceToNumber(Scalarize(EvaluateNode(node.Children[2])));
            return XlsxFormulaCalculationValue.CreateNumber((hour * 3600 + minute * 60 + second) / 86400);
        }

        if (node.Children.Count < 1)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        double number = CoerceToNumber(Scalarize(EvaluateNode(node.Children[0])));
        XlsxDateSerialConversionResult date = XlsxDateSerialConverter.FromSerial(number, false);
        if (!date.IsValid)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        if (name == "YEAR")
        {
            return XlsxFormulaCalculationValue.CreateNumber(date.DateTimeValue.Year);
        }

        if (name == "MONTH")
        {
            return XlsxFormulaCalculationValue.CreateNumber(date.DateTimeValue.Month);
        }

        if (name == "DAY")
        {
            return XlsxFormulaCalculationValue.CreateNumber(date.DateTimeValue.Day);
        }

        return XlsxFormulaCalculationValue.CreateError("#NAME?");
    }

    internal XlsxFormulaCalculationValue EvaluateMathFunction(XlsxFormulaNode node, string name)
    {
        if (node.Children.Count < 1)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        double number = CoerceToNumber(Scalarize(EvaluateNode(node.Children[0])));
        if (double.IsNaN(number))
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        if (name == "ABS")
        {
            return XlsxFormulaCalculationValue.CreateNumber(System.Math.Abs(number));
        }

        if (name == "INT")
        {
            return XlsxFormulaCalculationValue.CreateNumber(System.Math.Floor(number));
        }

        if (name == "SQRT")
        {
            if (number < 0)
            {
                return XlsxFormulaCalculationValue.CreateError("#NUM!");
            }

            return XlsxFormulaCalculationValue.CreateNumber(System.Math.Sqrt(number));
        }

        if (name == "ROUND")
        {
            int digits = 0;
            if (node.Children.Count > 1)
            {
                digits = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
            }

            if (digits < 0 || digits > 15)
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            decimal decimalNumber = (decimal)number;
            decimal rounded = decimal.Round(decimalNumber, digits, System.MidpointRounding.AwayFromZero);
            return XlsxFormulaCalculationValue.CreateNumber((double)rounded);
        }

        if (name == "POWER")
        {
            if (node.Children.Count < 2)
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            double power = CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
            return XlsxFormulaCalculationValue.CreateNumber(System.Math.Pow(number, power));
        }

        if (name == "MOD")
        {
            if (node.Children.Count < 2)
            {
                return XlsxFormulaCalculationValue.CreateError("#VALUE!");
            }

            double divisor = CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
            if (divisor == 0)
            {
                return XlsxFormulaCalculationValue.CreateError("#DIV/0!");
            }

            return XlsxFormulaCalculationValue.CreateNumber(number - divisor * System.Math.Floor(number / divisor));
        }

        return XlsxFormulaCalculationValue.CreateError("#NAME?");
    }

    internal XlsxFormulaCalculationValue EvaluateLookupFunction(XlsxFormulaNode node, string name)
    {
        if (name == "INDEX")
        {
            return EvaluateIndexFunction(node);
        }

        if (name == "MATCH")
        {
            return EvaluateMatchFunction(node);
        }

        if (name == "VLOOKUP")
        {
            return EvaluateVLookupFunction(node);
        }

        if (name == "XLOOKUP")
        {
            return EvaluateXLookupFunction(node);
        }

        return XlsxFormulaCalculationValue.CreateError("#NAME?");
    }

    internal XlsxFormulaCalculationValue EvaluateIndexFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 2)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue array = EvaluateNode(node.Children[0]);
        if (array.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return array;
        }

        if (array.Kind != XlsxFormulaCalculationValueKind.Range)
        {
            array = CreateSingleValueRange(array);
        }

        int row = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
        int column = 1;
        if (node.Children.Count > 2)
        {
            column = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[2])));
        }

        return GetRangeItem(array, row, column);
    }

    internal XlsxFormulaCalculationValue EvaluateMatchFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 2)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue lookup = Scalarize(EvaluateNode(node.Children[0]));
        XlsxFormulaCalculationValue range = EvaluateNode(node.Children[1]);
        if (lookup.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return lookup;
        }

        if (range.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return range;
        }

        if (range.Kind != XlsxFormulaCalculationValueKind.Range)
        {
            range = CreateSingleValueRange(range);
        }

        int index = FindRangeMatch(lookup, range);
        if (index <= 0)
        {
            return XlsxFormulaCalculationValue.CreateError("#N/A");
        }

        return XlsxFormulaCalculationValue.CreateNumber(index);
    }

    internal XlsxFormulaCalculationValue EvaluateVLookupFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 3)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue lookup = Scalarize(EvaluateNode(node.Children[0]));
        XlsxFormulaCalculationValue table = EvaluateNode(node.Children[1]);
        int column = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[2])));
        if (lookup.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return lookup;
        }

        if (table.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return table;
        }

        if (table.Kind != XlsxFormulaCalculationValueKind.Range || column <= 0 || column > table.Columns)
        {
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }

        int row = 1;
        while (row <= table.Rows)
        {
            XlsxFormulaCalculationValue candidate = GetRangeItem(table, row, 1);
            if (CompareValues(lookup, candidate) == 0)
            {
                return GetRangeItem(table, row, column);
            }

            row = row + 1;
        }

        return XlsxFormulaCalculationValue.CreateError("#N/A");
    }

    internal XlsxFormulaCalculationValue EvaluateXLookupFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 3)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue lookup = Scalarize(EvaluateNode(node.Children[0]));
        XlsxFormulaCalculationValue lookupArray = EvaluateNode(node.Children[1]);
        XlsxFormulaCalculationValue returnArray = EvaluateNode(node.Children[2]);
        if (lookup.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return lookup;
        }

        if (lookupArray.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return lookupArray;
        }

        if (returnArray.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return returnArray;
        }

        if (lookupArray.Kind != XlsxFormulaCalculationValueKind.Range)
        {
            lookupArray = CreateSingleValueRange(lookupArray);
        }

        if (returnArray.Kind != XlsxFormulaCalculationValueKind.Range)
        {
            returnArray = CreateSingleValueRange(returnArray);
        }

        int index = FindRangeMatch(lookup, lookupArray);
        if (index <= 0)
        {
            return XlsxFormulaCalculationValue.CreateError("#N/A");
        }

        if (index > returnArray.Items.Count)
        {
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }

        return returnArray.Items[index - 1];
    }

    internal XlsxFormulaCalculationValue EvaluateDynamicArrayFunction(XlsxFormulaNode node, string name)
    {
        if (name == "SEQUENCE")
        {
            return EvaluateSequenceFunction(node);
        }

        if (name == "FILTER")
        {
            return EvaluateFilterFunction(node);
        }

        if (name == "TRANSPOSE")
        {
            return EvaluateTransposeFunction(node);
        }

        return XlsxFormulaCalculationValue.CreateError("#NAME?");
    }

    internal XlsxFormulaCalculationValue EvaluateSequenceFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 1)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        int rows = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[0])));
        int columns = 1;
        double start = 1;
        double step = 1;
        if (node.Children.Count > 1)
        {
            columns = (int)CoerceToNumber(Scalarize(EvaluateNode(node.Children[1])));
        }

        if (node.Children.Count > 2)
        {
            start = CoerceToNumber(Scalarize(EvaluateNode(node.Children[2])));
        }

        if (node.Children.Count > 3)
        {
            step = CoerceToNumber(Scalarize(EvaluateNode(node.Children[3])));
        }

        if (rows <= 0 || columns <= 0 || rows * columns > 10000)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue range = XlsxFormulaCalculationValue.CreateRange();
        range.Rows = rows;
        range.Columns = columns;
        int index = 0;
        while (index < rows * columns)
        {
            range.Items.Add(XlsxFormulaCalculationValue.CreateNumber(start + step * index));
            index = index + 1;
        }

        return range;
    }

    internal XlsxFormulaCalculationValue EvaluateFilterFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 2)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue array = EvaluateNode(node.Children[0]);
        XlsxFormulaCalculationValue include = EvaluateNode(node.Children[1]);
        if (array.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return array;
        }

        if (include.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return include;
        }

        if (array.Kind != XlsxFormulaCalculationValueKind.Range || include.Kind != XlsxFormulaCalculationValueKind.Range || array.Items.Count != include.Items.Count)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue filtered = XlsxFormulaCalculationValue.CreateRange();
        filtered.Columns = array.Columns;
        int index = 0;
        while (index < array.Items.Count)
        {
            if (index < include.Items.Count && CoerceToBoolean(include.Items[index]))
            {
                filtered.Items.Add(array.Items[index]);
            }

            index = index + 1;
        }

        if (filtered.Items.Count == 0)
        {
            if (node.Children.Count > 2)
            {
                return EvaluateNode(node.Children[2]);
            }

            return XlsxFormulaCalculationValue.CreateError("#CALC!");
        }

        if (filtered.Columns <= 0)
        {
            filtered.Columns = 1;
        }

        filtered.Rows = (filtered.Items.Count + filtered.Columns - 1) / filtered.Columns;
        return filtered;
    }

    internal XlsxFormulaCalculationValue EvaluateTransposeFunction(XlsxFormulaNode node)
    {
        if (node.Children.Count < 1)
        {
            return XlsxFormulaCalculationValue.CreateError("#VALUE!");
        }

        XlsxFormulaCalculationValue array = EvaluateNode(node.Children[0]);
        if (array.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return array;
        }

        if (array.Kind != XlsxFormulaCalculationValueKind.Range)
        {
            return array;
        }

        XlsxFormulaCalculationValue transposed = XlsxFormulaCalculationValue.CreateRange();
        transposed.Rows = array.Columns;
        transposed.Columns = array.Rows;
        int column = 1;
        while (column <= array.Columns)
        {
            int row = 1;
            while (row <= array.Rows)
            {
                transposed.Items.Add(GetRangeItem(array, row, column));
                row = row + 1;
            }

            column = column + 1;
        }

        return transposed;
    }

    internal XlsxFormulaCalculationValue EvaluateAggregateFunction(XlsxFormulaNode node, string name)
    {
        XlsxFormulaAggregateState state = new XlsxFormulaAggregateState();
        foreach (XlsxFormulaNode child in node.Children)
        {
            XlsxFormulaCalculationValue value = EvaluateNode(child);
            XlsxFormulaCalculationValue? error = AddAggregateValue(value, name, state);
            if (error != null)
            {
                return error;
            }
        }

        if (name == "COUNT")
        {
            return XlsxFormulaCalculationValue.CreateNumber(state.Count);
        }

        if (state.Count == 0)
        {
            if (name == "SUM")
            {
                return XlsxFormulaCalculationValue.CreateNumber(0);
            }

            return XlsxFormulaCalculationValue.CreateError("#DIV/0!");
        }

        if (name == "AVERAGE")
        {
            return XlsxFormulaCalculationValue.CreateNumber(state.Total / state.Count);
        }

        if (name == "MIN")
        {
            return XlsxFormulaCalculationValue.CreateNumber(state.Minimum);
        }

        if (name == "MAX")
        {
            return XlsxFormulaCalculationValue.CreateNumber(state.Maximum);
        }

        return XlsxFormulaCalculationValue.CreateNumber(state.Total);
    }

    internal XlsxFormulaCalculationValue? AddAggregateValue(XlsxFormulaCalculationValue value, string name, XlsxFormulaAggregateState state)
    {
        if (value.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return value;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Range)
        {
            foreach (XlsxFormulaCalculationValue item in value.Items)
            {
                XlsxFormulaCalculationValue? error = AddAggregateValue(item, name, state);
                if (error != null)
                {
                    return error;
                }
            }

            return null;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Number || value.Kind == XlsxFormulaCalculationValueKind.Boolean)
        {
            double number = CoerceToNumber(value);
            state.Total = state.Total + number;
            if (state.Count == 0 || number < state.Minimum)
            {
                state.Minimum = number;
            }

            if (state.Count == 0 || number > state.Maximum)
            {
                state.Maximum = number;
            }

            state.Count = state.Count + 1;
        }

        return null;
    }

    internal XlsxFormulaCalculationValue? CollectNumericValues(XlsxFormulaCalculationValue value, XlsxFormulaNumberList list)
    {
        if (value.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return value;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Range)
        {
            foreach (XlsxFormulaCalculationValue item in value.Items)
            {
                XlsxFormulaCalculationValue? error = CollectNumericValues(item, list);
                if (error != null)
                {
                    return error;
                }
            }

            return null;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Number || value.Kind == XlsxFormulaCalculationValueKind.Boolean)
        {
            list.Numbers.Add(CoerceToNumber(value));
        }

        return null;
    }

    internal static double CalculatePmt(double rate, double periods, double presentValue, double futureValue, double type)
    {
        if (rate == 0)
        {
            return (0 - presentValue - futureValue) / periods;
        }

        double factor = System.Math.Pow(1 + rate, periods);
        return (0 - rate * (futureValue + presentValue * factor)) / ((1 + rate * type) * (factor - 1));
    }

    internal static double CalculateFv(double rate, double periods, double payment, double presentValue, double type)
    {
        if (rate == 0)
        {
            return 0 - presentValue - payment * periods;
        }

        double factor = System.Math.Pow(1 + rate, periods);
        return 0 - presentValue * factor - payment * (1 + rate * type) * (factor - 1) / rate;
    }

    internal static double CalculatePv(double rate, double periods, double payment, double futureValue, double type)
    {
        if (rate == 0)
        {
            return 0 - futureValue - payment * periods;
        }

        double factor = System.Math.Pow(1 + rate, periods);
        return (0 - futureValue - payment * (1 + rate * type) * (factor - 1) / rate) / factor;
    }

    internal XlsxFormulaCalculationValue EvaluateArrayConstant(XlsxFormulaNode node)
    {
        XlsxFormulaCalculationValue range = XlsxFormulaCalculationValue.CreateRange();
        range.Rows = 1;
        range.Columns = node.Children.Count;
        foreach (XlsxFormulaNode child in node.Children)
        {
            range.Items.Add(EvaluateNode(child));
        }

        return range;
    }

    internal XlsxFormulaCalculationValue FromCellValue(XlsxCellValue value)
    {
        if (value == null)
        {
            return XlsxFormulaCalculationValue.CreateBlank();
        }

        if (value.Kind == XlsxCellValueKind.Number)
        {
            return XlsxFormulaCalculationValue.CreateNumber(value.Number);
        }

        if (value.Kind == XlsxCellValueKind.Boolean)
        {
            return XlsxFormulaCalculationValue.CreateBoolean(value.Boolean);
        }

        if (value.Kind == XlsxCellValueKind.Text || value.Kind == XlsxCellValueKind.InlineString || value.Kind == XlsxCellValueKind.RichText)
        {
            return XlsxFormulaCalculationValue.CreateText(value.Text);
        }

        if (value.Kind == XlsxCellValueKind.Error)
        {
            return XlsxFormulaCalculationValue.CreateError(value.Text);
        }

        if (value.Kind == XlsxCellValueKind.FormulaCachedValue)
        {
            return FromCachedFormulaValue(value.CachedValue, value.CachedType);
        }

        return XlsxFormulaCalculationValue.CreateBlank();
    }

    internal XlsxFormulaCalculationValue FromCachedFormulaValue(string cachedValue, string cachedType)
    {
        string type = XlsxCellValueConverter.NormalizeText(cachedType);
        string value = XlsxCellValueConverter.NormalizeText(cachedValue);
        if (type == "b")
        {
            if (value == "1" || string.Equals(value, "TRUE", System.StringComparison.OrdinalIgnoreCase))
            {
                return XlsxFormulaCalculationValue.CreateBoolean(true);
            }

            return XlsxFormulaCalculationValue.CreateBoolean(false);
        }

        if (type == "str")
        {
            return XlsxFormulaCalculationValue.CreateText(value);
        }

        if (type == "e")
        {
            return XlsxFormulaCalculationValue.CreateError(value);
        }

        XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseDouble(value);
        if (parseResult.IsValid)
        {
            return XlsxFormulaCalculationValue.CreateNumber(parseResult.DoubleValue);
        }

        return XlsxFormulaCalculationValue.CreateBlank();
    }

    internal static XlsxFormulaCalculationValue CreateSingleValueRange(XlsxFormulaCalculationValue value)
    {
        XlsxFormulaCalculationValue range = XlsxFormulaCalculationValue.CreateRange();
        range.Rows = 1;
        range.Columns = 1;
        range.Items.Add(value);
        return range;
    }

    internal static XlsxFormulaCalculationValue GetRangeItem(XlsxFormulaCalculationValue range, int row, int column)
    {
        if (range == null || range.Kind != XlsxFormulaCalculationValueKind.Range || row <= 0 || column <= 0 || row > range.Rows || column > range.Columns)
        {
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }

        int index = (row - 1) * range.Columns + column - 1;
        if (index < 0 || index >= range.Items.Count)
        {
            return XlsxFormulaCalculationValue.CreateError("#REF!");
        }

        return range.Items[index];
    }

    internal static int FindRangeMatch(XlsxFormulaCalculationValue lookup, XlsxFormulaCalculationValue range)
    {
        if (range == null || range.Kind != XlsxFormulaCalculationValueKind.Range)
        {
            return 0;
        }

        int index = 0;
        while (index < range.Items.Count)
        {
            if (CompareValues(lookup, range.Items[index]) == 0)
            {
                return index + 1;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static XlsxFormulaCalculationValue Scalarize(XlsxFormulaCalculationValue? value)
    {
        if (value == null)
        {
            return XlsxFormulaCalculationValue.CreateBlank();
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Range)
        {
            if (value.Items.Count == 0)
            {
                return XlsxFormulaCalculationValue.CreateBlank();
            }

            return value.Items[0];
        }

        return value;
    }

    internal static XlsxFormulaCalculationValue? FirstError(XlsxFormulaCalculationValue? left, XlsxFormulaCalculationValue? right)
    {
        if (left != null && left.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return left;
        }

        if (right != null && right.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return right;
        }

        return null;
    }

    internal static double CoerceToNumber(XlsxFormulaCalculationValue? value)
    {
        if (value == null || value.Kind == XlsxFormulaCalculationValueKind.Blank)
        {
            return 0;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Number)
        {
            return value.Number;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Boolean)
        {
            if (value.Boolean)
            {
                return 1;
            }

            return 0;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Text)
        {
            if (value.Text == null || value.Text.Length == 0)
            {
                return 0;
            }

            XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseDouble(value.Text);
            if (parseResult.IsValid)
            {
                return parseResult.DoubleValue;
            }
        }

        return double.NaN;
    }

    internal static bool CoerceToBoolean(XlsxFormulaCalculationValue value)
    {
        if (value == null || value.Kind == XlsxFormulaCalculationValueKind.Blank)
        {
            return false;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Boolean)
        {
            return value.Boolean;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Number)
        {
            return value.Number != 0;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Text)
        {
            if (string.Equals(value.Text, "TRUE", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value.Text, "FALSE", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            double number = CoerceToNumber(value);
            if (!double.IsNaN(number))
            {
                return number != 0;
            }
        }

        return false;
    }

    internal static XlsxFormulaCalculationValue? AppendTextArgument(StringBuilder builder, XlsxFormulaCalculationValue value, string delimiter, bool ignoreEmpty, bool alreadyAppended)
    {
        if (value.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return value;
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Range)
        {
            bool appended = alreadyAppended;
            foreach (XlsxFormulaCalculationValue item in value.Items)
            {
                XlsxFormulaCalculationValue? error = AppendTextArgument(builder, item, delimiter, ignoreEmpty, appended);
                if (error != null)
                {
                    return error;
                }

                if (builder.Length > 0)
                {
                    appended = true;
                }
            }

            return null;
        }

        string text = CoerceToText(value);
        if (ignoreEmpty && text.Length == 0)
        {
            return null;
        }

        if (alreadyAppended && delimiter != null && delimiter.Length > 0)
        {
            builder.Append(delimiter);
        }

        builder.Append(text);
        return null;
    }

    internal static string LeftText(string text, int count)
    {
        if (count <= 0)
        {
            return "";
        }

        if (count >= text.Length)
        {
            return text;
        }

        return text.Substring(0, count);
    }

    internal static string RightText(string text, int count)
    {
        if (count <= 0)
        {
            return "";
        }

        if (count >= text.Length)
        {
            return text;
        }

        return text.Substring(text.Length - count);
    }

    internal static string MidText(string text, int start, int count)
    {
        if (start <= 0 || count <= 0 || start > text.Length)
        {
            return "";
        }

        int zeroBased = start - 1;
        int available = text.Length - zeroBased;
        int actual = count;
        if (actual > available)
        {
            actual = available;
        }

        return text.Substring(zeroBased, actual);
    }

    internal static string CoerceToText(XlsxFormulaCalculationValue? value)
    {
        if (value == null || value.Kind == XlsxFormulaCalculationValueKind.Blank)
        {
            return "";
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Number)
        {
            return XlsxXmlValueWriter.WriteDouble(value.Number);
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Boolean)
        {
            if (value.Boolean)
            {
                return "TRUE";
            }

            return "FALSE";
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Text)
        {
            return XlsxCellValueConverter.NormalizeText(value.Text);
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return value.Error;
        }

        return "";
    }

    internal static int CompareValues(XlsxFormulaCalculationValue? left, XlsxFormulaCalculationValue? right)
    {
        double leftNumber = CoerceToNumber(left);
        double rightNumber = CoerceToNumber(right);
        if (!double.IsNaN(leftNumber) && !double.IsNaN(rightNumber))
        {
            if (leftNumber < rightNumber)
            {
                return -1;
            }

            if (leftNumber > rightNumber)
            {
                return 1;
            }

            return 0;
        }

        return string.Compare(CoerceToText(left), CoerceToText(right), System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsComparisonOperator(string op)
    {
        return op == "=" || op == "<>" || op == "<=" || op == ">=" || op == "<" || op == ">";
    }

    internal static string GetFormulaReferenceTarget(string text)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(text);
        int bang = normalized.LastIndexOf('!');
        if (bang >= 0 && bang + 1 < normalized.Length)
        {
            return normalized.Substring(bang + 1);
        }

        return normalized;
    }

    internal static bool IsFormulaErrorText(string text)
    {
        return text == "#DIV/0!" || text == "#VALUE!" || text == "#REF!" || text == "#NAME?" || text == "#NULL!" || text == "#NUM!" || text == "#N/A" || text == "#GETTING_DATA" || text == "#CALC!" || text == "#SPILL!";
    }

    internal static string UnquoteFormulaString(string text)
    {
        if (text == null || text.Length < 2 || text[0] != '"' || text[text.Length - 1] != '"')
        {
            return XlsxCellValueConverter.NormalizeText(text);
        }

        StringBuilder builder = new StringBuilder();
        int index = 1;
        while (index < text.Length - 1)
        {
            if (text[index] == '"' && index + 1 < text.Length - 1 && text[index + 1] == '"')
            {
                builder.Append("\"");
                index = index + 2;
            }
            else
            {
                builder.Append(text[index]);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal static void CopyFormulaParseDiagnostics(XlsxFormulaCalculationResult result, XlsxFormulaParseResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}

internal class XlsxWorksheetRangeCellSnapshot
{
    internal int RowOffset;
    internal int ColumnOffset;
    internal bool HasCell;
    internal XlsxWorksheetCell Cell;

    internal XlsxWorksheetRangeCellSnapshot()
    {
        RowOffset = 0;
        ColumnOffset = 0;
        HasCell = false;
        Cell = new XlsxWorksheetCell();
    }
}

internal class XlsxWorksheetRangeRowSnapshot
{
    internal int OriginalRowIndex;
    internal int OriginalOrder;
    internal XlsxWorksheetCell SortCell;
    internal bool HasSortCell;
    internal List<XlsxWorksheetRangeCellSnapshot> Cells;

    internal XlsxWorksheetRangeRowSnapshot()
    {
        OriginalRowIndex = 0;
        OriginalOrder = 0;
        SortCell = new XlsxWorksheetCell();
        HasSortCell = false;
        Cells = new List<XlsxWorksheetRangeCellSnapshot>();
    }
}

internal class XlsxStructuralSqrefUpdateResult
{
    internal string Reference;
    internal int UpdatedCount;
    internal int RemovedCount;

    internal XlsxStructuralSqrefUpdateResult()
    {
        Reference = "";
        UpdatedCount = 0;
        RemovedCount = 0;
    }
}

internal class XlsxWorksheetColumnStoreResult
{
    internal bool IsValid;
    internal XlsxValidationResult Validation;
    internal XlsxWorksheetColumn Column;
    internal bool HasColumn;
    internal bool Removed;

    internal XlsxWorksheetColumnStoreResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Column = new XlsxWorksheetColumn();
        HasColumn = false;
        Removed = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }
}

internal class XlsxWorksheetColumnMetadataStore
{
    internal Dictionary<int, XlsxWorksheetColumn> Columns;
    internal int ColumnCount;
    internal bool HasColumns;
    internal int MinColumn;
    internal int MaxColumn;

    internal XlsxWorksheetColumnMetadataStore()
    {
        Columns = new Dictionary<int, XlsxWorksheetColumn>();
        ColumnCount = 0;
        HasColumns = false;
        MinColumn = 0;
        MaxColumn = 0;
    }

    internal XlsxWorksheetColumnStoreResult SetColumn(int columnIndex, XlsxWorksheetColumn column)
    {
        return SetColumnRange(columnIndex, columnIndex, column);
    }

    internal XlsxWorksheetColumnStoreResult SetColumnRange(int minColumn, int maxColumn, XlsxWorksheetColumn column)
    {
        XlsxWorksheetColumnStoreResult result = ValidateSetRange(minColumn, maxColumn, column);
        if (!result.IsValid)
        {
            return result;
        }

        int columnIndex = minColumn;
        while (columnIndex <= maxColumn)
        {
            XlsxWorksheetColumn clone = CloneColumnForIndex(column, columnIndex);
            if (!Columns.ContainsKey(columnIndex))
            {
                ColumnCount = ColumnCount + 1;
            }

            Columns[columnIndex] = clone;
            UpdateBoundsAfterSet(columnIndex);
            columnIndex = columnIndex + 1;
        }

        result.Column = Columns[minColumn];
        result.HasColumn = true;
        return result;
    }

    internal XlsxWorksheetColumn? GetColumn(int columnIndex)
    {
        if (!IsValidColumnIndex(columnIndex))
        {
            return null;
        }

        if (!Columns.ContainsKey(columnIndex))
        {
            return null;
        }

        return Columns[columnIndex];
    }

    internal bool ContainsColumn(int columnIndex)
    {
        return GetColumn(columnIndex) != null;
    }

    internal XlsxWorksheetColumnStoreResult GetOrCreateColumn(int columnIndex)
    {
        XlsxWorksheetColumn? existing = GetColumn(columnIndex);
        if (existing != null)
        {
            XlsxWorksheetColumnStoreResult existingResult = new XlsxWorksheetColumnStoreResult();
            existingResult.Column = existing;
            existingResult.HasColumn = true;
            return existingResult;
        }

        XlsxWorksheetColumn column = new XlsxWorksheetColumn();
        return SetColumn(columnIndex, column);
    }

    internal XlsxWorksheetColumnStoreResult RemoveColumn(int columnIndex)
    {
        XlsxWorksheetColumnStoreResult result = ValidateColumnIndex(columnIndex);
        if (!result.IsValid)
        {
            return result;
        }

        if (!Columns.ContainsKey(columnIndex))
        {
            return result;
        }

        result.Column = Columns[columnIndex];
        result.HasColumn = true;
        result.Removed = Columns.Remove(columnIndex);
        if (result.Removed)
        {
            ColumnCount = ColumnCount - 1;
            RecalculateBounds();
        }

        return result;
    }

    internal XlsxWorksheetColumnStoreResult LoadWorksheetPart(XlsxWorksheetPart part)
    {
        Clear();
        XlsxWorksheetColumnStoreResult result = new XlsxWorksheetColumnStoreResult();
        if (part == null)
        {
            result.AddError("columnMetadataStore.partNull", "Worksheet part must not be null.");
            return result;
        }

        return LoadColumns(part.Columns);
    }

    internal XlsxWorksheetColumnStoreResult LoadColumns(List<XlsxWorksheetColumn> columns)
    {
        XlsxWorksheetColumnStoreResult result = new XlsxWorksheetColumnStoreResult();
        if (columns == null)
        {
            result.AddError("columnMetadataStore.columnsNull", "Worksheet columns list must not be null.");
            return result;
        }

        foreach (XlsxWorksheetColumn column in columns)
        {
            if (column == null)
            {
                result.AddError("columnMetadataStore.columnNull", "Worksheet columns must not contain null entries.");
            }
            else
            {
                XlsxWorksheetColumnStoreResult setResult = SetColumnRange((int)column.Min, (int)column.Max, column);
                AddStoreDiagnostics(result, setResult);
            }
        }

        return result;
    }

    internal XlsxWorksheetColumnStoreResult ApplyToWorksheetPart(XlsxWorksheetPart part)
    {
        XlsxWorksheetColumnStoreResult result = new XlsxWorksheetColumnStoreResult();
        if (part == null)
        {
            result.AddError("columnMetadataStore.partNull", "Worksheet part must not be null.");
            return result;
        }

        part.Columns.Clear();
        List<XlsxWorksheetColumn> merged = CreateMergedColumns();
        foreach (XlsxWorksheetColumn column in merged)
        {
            part.Columns.Add(column);
        }

        return result;
    }

    internal List<XlsxWorksheetColumn> CreateMergedColumns()
    {
        List<XlsxWorksheetColumn> merged = new List<XlsxWorksheetColumn>();
        List<int> keys = GetSortedColumnIndexes();
        XlsxWorksheetColumn? current = null;
        int previousIndex = 0;
        foreach (int columnIndex in keys)
        {
            XlsxWorksheetColumn source = Columns[columnIndex];
            if (current == null)
            {
                current = CloneColumnForRange(source, columnIndex, columnIndex);
                previousIndex = columnIndex;
            }
            else if (columnIndex == previousIndex + 1 && AreEquivalent(current, source))
            {
                current.Max = (uint)columnIndex;
                previousIndex = columnIndex;
            }
            else
            {
                merged.Add(current);
                current = CloneColumnForRange(source, columnIndex, columnIndex);
                previousIndex = columnIndex;
            }
        }

        if (current != null)
        {
            merged.Add(current);
        }

        return merged;
    }

    internal List<int> GetSortedColumnIndexes()
    {
        List<int> keys = new List<int>();
        foreach (KeyValuePair<int, XlsxWorksheetColumn> item in Columns)
        {
            keys.Add(item.Key);
        }

        keys.Sort();
        return keys;
    }

    internal void Clear()
    {
        Columns.Clear();
        ColumnCount = 0;
        HasColumns = false;
        MinColumn = 0;
        MaxColumn = 0;
    }

    internal XlsxWorksheetColumnStoreResult ValidateSetRange(int minColumn, int maxColumn, XlsxWorksheetColumn column)
    {
        XlsxWorksheetColumnStoreResult result = ValidateColumnRange(minColumn, maxColumn);
        if (!result.IsValid)
        {
            return result;
        }

        if (column == null)
        {
            result.AddError("columnMetadataStore.columnNull", "Worksheet column metadata must not be null.");
        }

        return result;
    }

    internal XlsxWorksheetColumnStoreResult ValidateColumnRange(int minColumn, int maxColumn)
    {
        XlsxWorksheetColumnStoreResult result = new XlsxWorksheetColumnStoreResult();
        if (!IsValidColumnIndex(minColumn) || !IsValidColumnIndex(maxColumn))
        {
            result.AddError("columnMetadataStore.columnRange", "Worksheet column range must be within worksheet limits.");
            return result;
        }

        if (minColumn > maxColumn)
        {
            result.AddError("columnMetadataStore.columnRangeOrder", "Worksheet column range minimum must be less than or equal to maximum.");
        }

        return result;
    }

    internal XlsxWorksheetColumnStoreResult ValidateColumnIndex(int columnIndex)
    {
        XlsxWorksheetColumnStoreResult result = new XlsxWorksheetColumnStoreResult();
        if (!IsValidColumnIndex(columnIndex))
        {
            result.AddError("columnMetadataStore.columnRange", "Worksheet column index must be within worksheet limits.");
        }

        return result;
    }

    internal static bool IsValidColumnIndex(int columnIndex)
    {
        if (columnIndex <= 0 || columnIndex > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            return false;
        }

        return true;
    }

    internal void UpdateBoundsAfterSet(int columnIndex)
    {
        if (!HasColumns)
        {
            HasColumns = true;
            MinColumn = columnIndex;
            MaxColumn = columnIndex;
            return;
        }

        if (columnIndex < MinColumn)
        {
            MinColumn = columnIndex;
        }

        if (columnIndex > MaxColumn)
        {
            MaxColumn = columnIndex;
        }
    }

    internal void RecalculateBounds()
    {
        HasColumns = false;
        MinColumn = 0;
        MaxColumn = 0;
        foreach (KeyValuePair<int, XlsxWorksheetColumn> item in Columns)
        {
            UpdateBoundsAfterSet(item.Key);
        }
    }

    internal static XlsxWorksheetColumn CloneColumnForIndex(XlsxWorksheetColumn source, int columnIndex)
    {
        return CloneColumnForRange(source, columnIndex, columnIndex);
    }

    internal static XlsxWorksheetColumn CloneColumnForRange(XlsxWorksheetColumn source, int minColumn, int maxColumn)
    {
        XlsxWorksheetColumn clone = new XlsxWorksheetColumn();
        clone.Min = (uint)minColumn;
        clone.Max = (uint)maxColumn;
        clone.Width = source.Width;
        clone.HasWidth = source.HasWidth;
        clone.Style = source.Style;
        clone.HasStyle = source.HasStyle;
        clone.Hidden = source.Hidden;
        clone.HasHidden = source.HasHidden;
        clone.BestFit = source.BestFit;
        clone.HasBestFit = source.HasBestFit;
        clone.CustomWidth = source.CustomWidth;
        clone.HasCustomWidth = source.HasCustomWidth;
        clone.OutlineLevel = source.OutlineLevel;
        clone.HasOutlineLevel = source.HasOutlineLevel;
        clone.Collapsed = source.Collapsed;
        clone.HasCollapsed = source.HasCollapsed;
        foreach (XlsxXmlAttribute attribute in source.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(CloneAttribute(attribute));
        }

        return clone;
    }

    internal static XlsxXmlAttribute CloneAttribute(XlsxXmlAttribute source)
    {
        XlsxXmlAttribute clone = new XlsxXmlAttribute();
        clone.NamespaceUri = source.NamespaceUri;
        clone.LocalName = source.LocalName;
        clone.Prefix = source.Prefix;
        clone.Value = source.Value;
        return clone;
    }

    internal static bool AreEquivalent(XlsxWorksheetColumn left, XlsxWorksheetColumn right)
    {
        if (left.Width != right.Width || left.HasWidth != right.HasWidth)
        {
            return false;
        }

        if (left.Style != right.Style || left.HasStyle != right.HasStyle)
        {
            return false;
        }

        if (left.Hidden != right.Hidden || left.HasHidden != right.HasHidden)
        {
            return false;
        }

        if (left.BestFit != right.BestFit || left.HasBestFit != right.HasBestFit)
        {
            return false;
        }

        if (left.CustomWidth != right.CustomWidth || left.HasCustomWidth != right.HasCustomWidth)
        {
            return false;
        }

        if (left.OutlineLevel != right.OutlineLevel || left.HasOutlineLevel != right.HasOutlineLevel)
        {
            return false;
        }

        if (left.Collapsed != right.Collapsed || left.HasCollapsed != right.HasCollapsed)
        {
            return false;
        }

        return AreAttributesEquivalent(left.UnknownAttributes, right.UnknownAttributes);
    }

    internal static bool AreAttributesEquivalent(List<XlsxXmlAttribute> left, List<XlsxXmlAttribute> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Count)
        {
            XlsxXmlAttribute leftAttribute = left[index];
            XlsxXmlAttribute rightAttribute = right[index];
            if (leftAttribute.NamespaceUri != rightAttribute.NamespaceUri || leftAttribute.LocalName != rightAttribute.LocalName || leftAttribute.Prefix != rightAttribute.Prefix || leftAttribute.Value != rightAttribute.Value)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void AddStoreDiagnostics(XlsxWorksheetColumnStoreResult result, XlsxWorksheetColumnStoreResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}

internal class XlsxWorksheetRowStoreResult
{
    internal bool IsValid;
    internal XlsxValidationResult Validation;
    internal XlsxWorksheetRow Row;
    internal bool HasRow;
    internal bool Removed;

    internal XlsxWorksheetRowStoreResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Row = new XlsxWorksheetRow();
        HasRow = false;
        Removed = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }
}

internal class XlsxWorksheetRowMetadataStore
{
    internal Dictionary<int, XlsxWorksheetRow> Rows;
    internal int RowCount;
    internal bool HasRows;
    internal int MinRow;
    internal int MaxRow;

    internal XlsxWorksheetRowMetadataStore()
    {
        Rows = new Dictionary<int, XlsxWorksheetRow>();
        RowCount = 0;
        HasRows = false;
        MinRow = 0;
        MaxRow = 0;
    }

    internal XlsxWorksheetRowStoreResult SetRow(int rowIndex, XlsxWorksheetRow row)
    {
        XlsxWorksheetRowStoreResult result = ValidateSet(rowIndex, row);
        if (!result.IsValid)
        {
            return result;
        }

        if (!Rows.ContainsKey(rowIndex))
        {
            RowCount = RowCount + 1;
        }

        row.RowIndex = (uint)rowIndex;
        Rows[rowIndex] = row;
        UpdateBoundsAfterSet(rowIndex);
        result.Row = row;
        result.HasRow = true;
        return result;
    }

    internal XlsxWorksheetRow? GetRow(int rowIndex)
    {
        if (!IsValidRowIndex(rowIndex))
        {
            return null;
        }

        if (!Rows.ContainsKey(rowIndex))
        {
            return null;
        }

        return Rows[rowIndex];
    }

    internal bool ContainsRow(int rowIndex)
    {
        return GetRow(rowIndex) != null;
    }

    internal XlsxWorksheetRowStoreResult GetOrCreateRow(int rowIndex)
    {
        XlsxWorksheetRow? existing = GetRow(rowIndex);
        if (existing != null)
        {
            XlsxWorksheetRowStoreResult existingResult = new XlsxWorksheetRowStoreResult();
            existingResult.Row = existing;
            existingResult.HasRow = true;
            return existingResult;
        }

        XlsxWorksheetRow row = new XlsxWorksheetRow();
        return SetRow(rowIndex, row);
    }

    internal XlsxWorksheetRowStoreResult RemoveRow(int rowIndex)
    {
        XlsxWorksheetRowStoreResult result = ValidateRowIndex(rowIndex);
        if (!result.IsValid)
        {
            return result;
        }

        if (!Rows.ContainsKey(rowIndex))
        {
            return result;
        }

        result.Row = Rows[rowIndex];
        result.HasRow = true;
        result.Removed = Rows.Remove(rowIndex);
        if (result.Removed)
        {
            RowCount = RowCount - 1;
            RecalculateBounds();
        }

        return result;
    }

    internal XlsxWorksheetRowStoreResult LoadWorksheetPart(XlsxWorksheetPart part)
    {
        Clear();
        XlsxWorksheetRowStoreResult result = new XlsxWorksheetRowStoreResult();
        if (part == null)
        {
            result.AddError("rowMetadataStore.partNull", "Worksheet part must not be null.");
            return result;
        }

        return LoadRows(part.Rows);
    }

    internal XlsxWorksheetRowStoreResult LoadRows(List<XlsxWorksheetRow> rows)
    {
        XlsxWorksheetRowStoreResult result = new XlsxWorksheetRowStoreResult();
        if (rows == null)
        {
            result.AddError("rowMetadataStore.rowsNull", "Worksheet rows list must not be null.");
            return result;
        }

        int sequence = 1;
        foreach (XlsxWorksheetRow row in rows)
        {
            if (row == null)
            {
                result.AddError("rowMetadataStore.rowNull", "Worksheet rows must not contain null entries.");
            }
            else
            {
                int rowIndex = (int)row.RowIndex;
                if (rowIndex == 0)
                {
                    rowIndex = sequence;
                    row.RowIndex = (uint)rowIndex;
                }

                XlsxWorksheetRowStoreResult setResult = SetRow(rowIndex, row);
                AddStoreDiagnostics(result, setResult);
            }

            sequence = sequence + 1;
        }

        return result;
    }

    internal XlsxWorksheetRowStoreResult ApplyToWorksheetPart(XlsxWorksheetPart part)
    {
        XlsxWorksheetRowStoreResult result = new XlsxWorksheetRowStoreResult();
        if (part == null)
        {
            result.AddError("rowMetadataStore.partNull", "Worksheet part must not be null.");
            return result;
        }

        part.Rows.Clear();
        List<int> keys = GetSortedRowIndexes();
        foreach (int rowIndex in keys)
        {
            part.Rows.Add(Rows[rowIndex]);
        }

        return result;
    }

    internal List<int> GetSortedRowIndexes()
    {
        List<int> keys = new List<int>();
        foreach (KeyValuePair<int, XlsxWorksheetRow> item in Rows)
        {
            keys.Add(item.Key);
        }

        keys.Sort();
        return keys;
    }

    internal void Clear()
    {
        Rows.Clear();
        RowCount = 0;
        HasRows = false;
        MinRow = 0;
        MaxRow = 0;
    }

    internal XlsxWorksheetRowStoreResult ValidateSet(int rowIndex, XlsxWorksheetRow row)
    {
        XlsxWorksheetRowStoreResult result = ValidateRowIndex(rowIndex);
        if (!result.IsValid)
        {
            return result;
        }

        if (row == null)
        {
            result.AddError("rowMetadataStore.rowNull", "Worksheet row must not be null.");
        }

        return result;
    }

    internal XlsxWorksheetRowStoreResult ValidateRowIndex(int rowIndex)
    {
        XlsxWorksheetRowStoreResult result = new XlsxWorksheetRowStoreResult();
        if (!IsValidRowIndex(rowIndex))
        {
            result.AddError("rowMetadataStore.rowRange", "Worksheet row index must be within worksheet limits.");
        }

        return result;
    }

    internal static bool IsValidRowIndex(int rowIndex)
    {
        if (rowIndex <= 0 || rowIndex > XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            return false;
        }

        return true;
    }

    internal void UpdateBoundsAfterSet(int rowIndex)
    {
        if (!HasRows)
        {
            HasRows = true;
            MinRow = rowIndex;
            MaxRow = rowIndex;
            return;
        }

        if (rowIndex < MinRow)
        {
            MinRow = rowIndex;
        }

        if (rowIndex > MaxRow)
        {
            MaxRow = rowIndex;
        }
    }

    internal void RecalculateBounds()
    {
        HasRows = false;
        MinRow = 0;
        MaxRow = 0;
        foreach (KeyValuePair<int, XlsxWorksheetRow> item in Rows)
        {
            UpdateBoundsAfterSet(item.Key);
        }
    }

    internal static void AddStoreDiagnostics(XlsxWorksheetRowStoreResult result, XlsxWorksheetRowStoreResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}

/// <summary>
/// Represents cell value converter data used by the public workbook API.
/// </summary>
internal static class XlsxCellValueConverter
{
    internal static XlsxCellValueResult FromWorksheetCell(XlsxWorksheetCell? cell)
    {
        XlsxCellValueResult result = new XlsxCellValueResult();
        if (cell == null)
        {
            result.AddError("cellValue.cellNull", "Worksheet cell must not be null.");
            return result;
        }

        string type = NormalizeText(cell.Type);
        string value = NormalizeText(cell.Value);
        if (cell.HasFormula || NormalizeText(cell.Formula).Length > 0)
        {
            string formulaText = cell.Formula;
            if (cell.HasFormula && cell.FormulaInfo != null && cell.FormulaInfo.Text != null && cell.FormulaInfo.Text.Length > 0)
            {
                formulaText = cell.FormulaInfo.Text;
            }

            if (cell.HasFormula && cell.FormulaInfo != null && cell.FormulaInfo.CachedValue != null && cell.FormulaInfo.CachedValue.Length > 0 && value.Length == 0)
            {
                value = cell.FormulaInfo.CachedValue;
            }

            if (cell.HasFormula && cell.FormulaInfo != null && cell.FormulaInfo.CachedType != null && cell.FormulaInfo.CachedType.Length > 0 && type.Length == 0)
            {
                type = cell.FormulaInfo.CachedType;
            }

            result.Value = XlsxCellValue.CreateFormulaCachedValue(formulaText, value, type);
            XlsxWorksheetCell cachedCell = new XlsxWorksheetCell();
            cachedCell.Type = type;
            cachedCell.Value = value;
            XlsxCellValueResult cachedResult = FromWorksheetCell(cachedCell);
            foreach (XlsxDiagnostic diagnostic in cachedResult.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }
            result.Value.TypedCachedValue = cachedResult.Value;
            result.Value.HasTypedCachedValue = cachedResult.Value != null;
            return result;
        }

        if (type.Length == 0 || type == "n")
        {
            ReadNumberOrBlank(result, value);
            return result;
        }

        if (type == "s")
        {
            ReadSharedString(result, value);
            return result;
        }

        if (type == "inlineStr")
        {
            if (cell.HasInlineString && cell.InlineString != null && cell.InlineString.IsRichText)
            {
                result.Value = XlsxCellValue.CreateRichTextValue(cell.InlineString);
                return result;
            }

            result.Value = XlsxCellValue.CreateInlineString(cell.InlineStringText);
            return result;
        }

        if (type == "str")
        {
            result.Value = XlsxCellValue.CreateText(value);
            return result;
        }

        if (type == "b")
        {
            ReadBoolean(result, value);
            return result;
        }

        if (type == "e")
        {
            result.Value = XlsxCellValue.CreateError(value);
            return result;
        }

        if (type == "d")
        {
            ReadDateTime(result, value);
            return result;
        }

        result.Value = XlsxCellValue.CreateUnknownRawValue(type, value);
        return result;
    }

    internal static XlsxCellValueResult ApplyToWorksheetCell(XlsxCellValue value, XlsxWorksheetCell cell)
    {
        XlsxCellValueResult result = new XlsxCellValueResult();
        if (value == null)
        {
            result.AddError("cellValue.valueNull", "Cell value must not be null.");
            return result;
        }

        if (cell == null)
        {
            result.AddError("cellValue.cellNull", "Worksheet cell must not be null.");
            return result;
        }

        ClearTypedCellContent(cell);
        if (value.Kind == XlsxCellValueKind.Blank)
        {
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.Number)
        {
            cell.Type = "n";
            cell.Value = GetExactText(value, XlsxXmlValueWriter.WriteDouble(value.Number));
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.Decimal)
        {
            cell.Type = "n";
            cell.Value = GetExactText(value, value.Decimal.ToString(CultureInfo.InvariantCulture));
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.Text)
        {
            cell.Type = "str";
            cell.Value = NormalizeText(value.Text);
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.SharedString)
        {
            cell.Type = "s";
            cell.Value = XlsxXmlValueWriter.WriteUnsignedInteger(value.SharedStringIndex);
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.InlineString)
        {
            cell.Type = "inlineStr";
            cell.InlineStringText = NormalizeText(value.Text);
            cell.InlineString = XlsxInlineString.CreateText(value.Text);
            cell.HasInlineString = true;
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.Boolean)
        {
            cell.Type = "b";
            cell.Value = XlsxXmlValueWriter.WriteBoolean(value.Boolean);
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.Error)
        {
            cell.Type = "e";
            if (value.HasErrorKind)
            {
                cell.Value = XlsxCellValue.GetErrorText(value.ErrorKind);
            }
            else
            {
                cell.Value = FirstText(value.Text, value.RawValue);
            }
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.DateTime)
        {
            cell.Type = "d";
            cell.Value = GetExactText(value, XlsxXmlValueWriter.WriteDateTime(value.DateTime));
            XlsxXmlValueParseResult parsedDate = XlsxXmlValueParser.ParseDateTime(cell.Value);
            AddParseDiagnostics(result, parsedDate);
            if (!result.IsValid)
            {
                return result;
            }
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.Duration)
        {
            cell.Type = "n";
            decimal durationDays = (decimal)value.Duration.Ticks / 864000000000m;
            cell.Value = GetExactText(value, durationDays.ToString(CultureInfo.InvariantCulture));
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.FormulaCachedValue)
        {
            cell.Formula = NormalizeText(value.Formula);
            cell.FormulaInfo = XlsxCellFormula.CreateNormal(value.Formula);
            cell.HasFormula = true;
            if (value.HasTypedCachedValue && value.TypedCachedValue != null)
            {
                XlsxWorksheetCell cachedCell = new XlsxWorksheetCell();
                XlsxCellValueResult cachedResult = ApplyToWorksheetCell(value.TypedCachedValue, cachedCell);
                foreach (XlsxDiagnostic diagnostic in cachedResult.Validation.Diagnostics)
                {
                    result.AddDiagnostic(diagnostic);
                }
                if (!result.IsValid || cachedCell.HasFormula || cachedCell.HasInlineString)
                {
                    if (result.IsValid)
                    {
                        result.AddError("cellValue.formulaCacheKind", "Typed formula cached values must be scalar cell values.");
                    }
                    return result;
                }
                cell.Type = NormalizeText(cachedCell.Type);
                cell.Value = cachedCell.Value;
                cell.FormulaInfo.CachedValue = cell.Value;
                cell.FormulaInfo.CachedType = cell.Type;
                value.CachedValue = cell.Value;
                value.CachedType = cell.Type;
            }
            else
            {
                cell.Type = NormalizeText(value.CachedType);
                cell.FormulaInfo.CachedValue = NormalizeText(value.CachedValue);
                cell.FormulaInfo.CachedType = NormalizeText(value.CachedType);
                cell.Value = NormalizeText(value.CachedValue);
            }
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.RichText)
        {
            cell.Type = "inlineStr";
            cell.InlineStringText = NormalizeText(value.Text);
            cell.InlineString = XlsxInlineString.CreateText(value.Text);
            cell.InlineString.IsRichText = true;
            foreach (XlsxSharedStringRichTextRun run in value.RichTextRuns)
            {
                cell.InlineString.RichTextRuns.Add(XlsxSharedStringTableWriter.CloneRun(run));
            }
            foreach (XlsxPhoneticText phoneticText in value.PhoneticTexts)
            {
                cell.InlineString.PhoneticTexts.Add(XlsxSharedStringTableWriter.ClonePhoneticText(phoneticText));
            }
            cell.InlineString.PhoneticProperties = XlsxCellValue.ClonePhoneticProperties(value.PhoneticProperties);
            cell.InlineString.HasPhoneticProperties = value.HasPhoneticProperties;
            if (value.RichTextRawXml != null && value.RichTextRawXml.Length > 0)
            {
                cell.InlineString.UnknownXml.Add(value.RichTextRawXml);
            }

            cell.HasInlineString = true;
            result.Value = value;
            return result;
        }

        if (value.Kind == XlsxCellValueKind.UnknownRawValue)
        {
            cell.Type = NormalizeText(value.RawType);
            cell.Value = NormalizeText(value.RawValue);
            result.Value = value;
            return result;
        }

        result.AddError("cellValue.kindInvalid", "Cell value kind is not supported.");
        result.Value = value;
        return result;
    }

    internal static string NormalizeText(string? text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }

    internal static void AddParseDiagnostics(XlsxCellValueResult result, XlsxXmlValueParseResult parseResult)
    {
        foreach (XlsxDiagnostic diagnostic in parseResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void ClearTypedCellContent(XlsxWorksheetCell cell)
    {
        cell.Type = "";
        cell.Value = "";
        cell.Formula = "";
        cell.FormulaInfo = new XlsxCellFormula();
        cell.HasFormula = false;
        cell.InlineStringText = "";
        cell.InlineString = new XlsxInlineString();
        cell.HasInlineString = false;
    }

    internal static string FirstText(string first, string second)
    {
        string normalizedFirst = NormalizeText(first);
        if (normalizedFirst.Length > 0)
        {
            return normalizedFirst;
        }

        return NormalizeText(second);
    }

    internal static string GetExactText(XlsxCellValue value, string fallback)
    {
        if (value.HasExactText && value.ExactText != null && value.ExactText.Length > 0)
        {
            return value.ExactText;
        }
        return fallback;
    }

    internal static void ReadNumberOrBlank(XlsxCellValueResult result, string value)
    {
        if (value.Length == 0)
        {
            result.Value = XlsxCellValue.CreateBlank();
            return;
        }

        XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseDouble(value);
        AddParseDiagnostics(result, parseResult);
        if (parseResult.IsValid)
        {
            result.Value = XlsxCellValue.CreateNumber(parseResult.DoubleValue);
            result.Value.RawValue = value;
            result.Value.ExactText = value;
            result.Value.HasExactText = true;
        }
        else
        {
            result.Value = XlsxCellValue.CreateUnknownRawValue("n", value);
        }
    }

    internal static void ReadSharedString(XlsxCellValueResult result, string value)
    {
        XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseUnsignedInteger(value);
        AddParseDiagnostics(result, parseResult);
        if (parseResult.IsValid)
        {
            result.Value = XlsxCellValue.CreateSharedString(parseResult.UnsignedIntValue);
        }
        else
        {
            result.Value = XlsxCellValue.CreateUnknownRawValue("s", value);
        }
    }

    internal static void ReadBoolean(XlsxCellValueResult result, string value)
    {
        XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseBoolean(value);
        AddParseDiagnostics(result, parseResult);
        if (parseResult.IsValid)
        {
            result.Value = XlsxCellValue.CreateBoolean(parseResult.BooleanValue);
        }
        else
        {
            result.Value = XlsxCellValue.CreateUnknownRawValue("b", value);
        }
    }

    internal static void ReadDateTime(XlsxCellValueResult result, string value)
    {
        XlsxXmlValueParseResult parseResult = XlsxXmlValueParser.ParseDateTime(value);
        AddParseDiagnostics(result, parseResult);
        if (parseResult.IsValid)
        {
            result.Value = XlsxCellValue.CreateDateTime(parseResult.DateTimeValue);
            result.Value.ExactText = value;
            result.Value.HasExactText = true;
        }
        else
        {
            result.Value = XlsxCellValue.CreateUnknownRawValue("d", value);
        }
    }
}

internal class XlsxCellStoreResult
{
    internal bool IsValid;
    internal XlsxValidationResult Validation;
    internal XlsxWorksheetCell Cell;
    internal bool HasCell;
    internal bool Removed;

    internal XlsxCellStoreResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Cell = new XlsxWorksheetCell();
        HasCell = false;
        Removed = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }
}

internal interface IXlsxCellStore
{
    XlsxCellStoreResult SetCell(int row, int column, XlsxWorksheetCell? cell);
    XlsxCellStoreResult SetCellByReference(string reference, XlsxWorksheetCell? cell);
    XlsxWorksheetCell? GetCell(int row, int column);
    XlsxWorksheetCell? GetCellByReference(string reference);
    bool ContainsCell(int row, int column);
    bool ContainsCellByReference(string reference);
    XlsxCellStoreResult GetOrCreateCell(int row, int column);
    XlsxCellStoreResult RemoveCell(int row, int column);
    XlsxCellStoreResult RemoveCellByReference(string reference);
    void Clear();
    XlsxCellStoreResult LoadWorksheetPart(XlsxWorksheetPart part);
    XlsxCellStoreResult LoadRows(List<XlsxWorksheetRow> rows);
    int GetCellCount();
    bool GetHasCells();
    int GetMinRow();
    int GetMaxRow();
    int GetMinColumn();
    int GetMaxColumn();
}

internal class XlsxSparseCellStore : IXlsxCellStore
{
    internal Dictionary<int, Dictionary<int, XlsxWorksheetCell>> Rows;
    internal int CellCount;
    internal bool HasCells;
    internal int MinRow;
    internal int MaxRow;
    internal int MinColumn;
    internal int MaxColumn;

    internal XlsxSparseCellStore()
    {
        Rows = new Dictionary<int, Dictionary<int, XlsxWorksheetCell>>();
        CellCount = 0;
        HasCells = false;
        MinRow = 0;
        MaxRow = 0;
        MinColumn = 0;
        MaxColumn = 0;
    }

    public XlsxCellStoreResult SetCell(int row, int column, XlsxWorksheetCell? cell)
    {
        XlsxCellStoreResult result = ValidateSet(row, column, cell);
        if (!result.IsValid || cell == null)
        {
            return result;
        }

        Dictionary<int, XlsxWorksheetCell> rowCells = GetOrCreateRow(row);
        if (!rowCells.ContainsKey(column))
        {
            CellCount = CellCount + 1;
        }

        if (cell.Reference == null || cell.Reference.Length == 0)
        {
            cell.Reference = XlsxA1CellReferenceConverter.FormatString(row, column, false, false);
        }

        rowCells[column] = cell;
        UpdateBoundsAfterSet(row, column);
        result.Cell = cell;
        result.HasCell = true;
        return result;
    }

    public XlsxCellStoreResult SetCellByReference(string reference, XlsxWorksheetCell? cell)
    {
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            AddReferenceDiagnostics(result, referenceResult);
            return result;
        }

        if (cell != null && (cell.Reference == null || cell.Reference.Length == 0))
        {
            cell.Reference = referenceResult.Text;
        }

        XlsxCellStoreResult setResult = SetCell(referenceResult.Row, referenceResult.Column, cell);
        AddStoreDiagnostics(result, setResult);
        result.Cell = setResult.Cell;
        result.HasCell = setResult.HasCell;
        return result;
    }

    public XlsxWorksheetCell? GetCell(int row, int column)
    {
        if (!IsValidCoordinate(row, column))
        {
            return null;
        }

        if (!Rows.ContainsKey(row))
        {
            return null;
        }

        Dictionary<int, XlsxWorksheetCell> rowCells = Rows[row];
        if (!rowCells.ContainsKey(column))
        {
            return null;
        }

        return rowCells[column];
    }

    public XlsxWorksheetCell? GetCellByReference(string reference)
    {
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            return null;
        }

        return GetCell(referenceResult.Row, referenceResult.Column);
    }

    public bool ContainsCell(int row, int column)
    {
        return GetCell(row, column) != null;
    }

    public bool ContainsCellByReference(string reference)
    {
        return GetCellByReference(reference) != null;
    }

    public XlsxCellStoreResult GetOrCreateCell(int row, int column)
    {
        XlsxWorksheetCell? existing = GetCell(row, column);
        if (existing != null)
        {
            XlsxCellStoreResult existingResult = new XlsxCellStoreResult();
            existingResult.Cell = existing;
            existingResult.HasCell = true;
            return existingResult;
        }

        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        return SetCell(row, column, cell);
    }

    public XlsxCellStoreResult RemoveCell(int row, int column)
    {
        XlsxCellStoreResult result = ValidateCoordinate(row, column);
        if (!result.IsValid)
        {
            return result;
        }

        if (!Rows.ContainsKey(row))
        {
            return result;
        }

        Dictionary<int, XlsxWorksheetCell> rowCells = Rows[row];
        if (!rowCells.ContainsKey(column))
        {
            return result;
        }

        result.Cell = rowCells[column];
        result.HasCell = true;
        result.Removed = rowCells.Remove(column);
        if (result.Removed)
        {
            CellCount = CellCount - 1;
            if (rowCells.Count == 0)
            {
                Rows.Remove(row);
            }

            RecalculateBounds();
        }

        return result;
    }

    public XlsxCellStoreResult RemoveCellByReference(string reference)
    {
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            AddReferenceDiagnostics(result, referenceResult);
            return result;
        }

        XlsxCellStoreResult removeResult = RemoveCell(referenceResult.Row, referenceResult.Column);
        AddStoreDiagnostics(result, removeResult);
        result.Cell = removeResult.Cell;
        result.HasCell = removeResult.HasCell;
        result.Removed = removeResult.Removed;
        return result;
    }

    public void Clear()
    {
        Rows.Clear();
        CellCount = 0;
        HasCells = false;
        MinRow = 0;
        MaxRow = 0;
        MinColumn = 0;
        MaxColumn = 0;
    }

    public XlsxCellStoreResult LoadWorksheetPart(XlsxWorksheetPart part)
    {
        Clear();
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        if (part == null)
        {
            result.AddError("sparseCellStore.partNull", "Worksheet part must not be null.");
            return result;
        }

        return LoadRows(part.Rows);
    }

    public XlsxCellStoreResult LoadRows(List<XlsxWorksheetRow> rows)
    {
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        if (rows == null)
        {
            result.AddError("sparseCellStore.rowsNull", "Worksheet rows list must not be null.");
            return result;
        }

        int rowIndex = 0;
        foreach (XlsxWorksheetRow row in rows)
        {
            if (row == null)
            {
                result.AddError("sparseCellStore.rowNull", "Worksheet rows must not contain null entries.");
            }
            else
            {
                LoadRow(result, row, rowIndex);
            }

            rowIndex = rowIndex + 1;
        }

        return result;
    }

    internal void LoadRow(XlsxCellStoreResult result, XlsxWorksheetRow row, int rowIndex)
    {
        if (row.Cells == null)
        {
            result.AddError("sparseCellStore.cellsNull", "Worksheet row cell lists must not be null.");
            return;
        }

        int cellIndex = 0;
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (cell == null)
            {
                result.AddError("sparseCellStore.cellNull", "Worksheet cells must not contain null entries.");
            }
            else if (cell.Reference == null || cell.Reference.Length == 0)
            {
                result.AddError("sparseCellStore.referenceMissing", "Worksheet cells loaded into a sparse store must have A1 references.");
            }
            else
            {
                XlsxCellStoreResult setResult = SetCellByReference(cell.Reference, cell);
                AddStoreDiagnostics(result, setResult);
            }

            cellIndex = cellIndex + 1;
        }
    }

    internal Dictionary<int, XlsxWorksheetCell> GetOrCreateRow(int row)
    {
        if (Rows.ContainsKey(row))
        {
            return Rows[row];
        }

        Dictionary<int, XlsxWorksheetCell> rowCells = new Dictionary<int, XlsxWorksheetCell>();
        Rows[row] = rowCells;
        return rowCells;
    }

    internal XlsxCellStoreResult ValidateSet(int row, int column, XlsxWorksheetCell? cell)
    {
        XlsxCellStoreResult result = ValidateCoordinate(row, column);
        if (!result.IsValid)
        {
            return result;
        }

        if (cell == null)
        {
            result.AddError("sparseCellStore.cellNull", "Sparse cell store cell must not be null.");
        }

        return result;
    }

    internal XlsxCellStoreResult ValidateCoordinate(int row, int column)
    {
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        if (row <= 0 || row > XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            result.AddError("sparseCellStore.rowRange", "Sparse cell store row must be within worksheet limits.");
        }

        if (column <= 0 || column > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            result.AddError("sparseCellStore.columnRange", "Sparse cell store column must be within worksheet limits.");
        }

        return result;
    }

    internal static bool IsValidCoordinate(int row, int column)
    {
        if (row <= 0 || row > XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            return false;
        }

        if (column <= 0 || column > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            return false;
        }

        return true;
    }

    internal void UpdateBoundsAfterSet(int row, int column)
    {
        if (!HasCells)
        {
            HasCells = true;
            MinRow = row;
            MaxRow = row;
            MinColumn = column;
            MaxColumn = column;
            return;
        }

        if (row < MinRow)
        {
            MinRow = row;
        }

        if (row > MaxRow)
        {
            MaxRow = row;
        }

        if (column < MinColumn)
        {
            MinColumn = column;
        }

        if (column > MaxColumn)
        {
            MaxColumn = column;
        }
    }

    internal void RecalculateBounds()
    {
        HasCells = false;
        MinRow = 0;
        MaxRow = 0;
        MinColumn = 0;
        MaxColumn = 0;
        foreach (KeyValuePair<int, Dictionary<int, XlsxWorksheetCell>> rowItem in Rows)
        {
            foreach (KeyValuePair<int, XlsxWorksheetCell> cellItem in rowItem.Value)
            {
                UpdateBoundsAfterSet(rowItem.Key, cellItem.Key);
            }
        }
    }

    public int GetCellCount()
    {
        return CellCount;
    }

    public bool GetHasCells()
    {
        return HasCells;
    }

    public int GetMinRow()
    {
        return MinRow;
    }

    public int GetMaxRow()
    {
        return MaxRow;
    }

    public int GetMinColumn()
    {
        return MinColumn;
    }

    public int GetMaxColumn()
    {
        return MaxColumn;
    }

    internal static void AddReferenceDiagnostics(XlsxCellStoreResult result, XlsxA1CellReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddStoreDiagnostics(XlsxCellStoreResult result, XlsxCellStoreResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}

internal class XlsxDenseCellStore : IXlsxCellStore
{
    internal XlsxWorksheetCell?[,] Cells;
    internal bool IsInitialized;
    internal int RowCount;
    internal int ColumnCount;
    internal int CellCount;
    internal int MaxCellCount;
    internal bool HasCells;
    internal int MinRow;
    internal int MaxRow;
    internal int MinColumn;
    internal int MaxColumn;

    internal XlsxDenseCellStore()
    {
        Cells = new XlsxWorksheetCell[0, 0];
        IsInitialized = false;
        RowCount = 0;
        ColumnCount = 0;
        CellCount = 0;
        MaxCellCount = 1000000;
        HasCells = false;
        MinRow = 0;
        MaxRow = 0;
        MinColumn = 0;
        MaxColumn = 0;
    }

    internal XlsxCellStoreResult Initialize(int rowCount, int columnCount)
    {
        XlsxCellStoreResult result = ValidateSize(rowCount, columnCount);
        if (!result.IsValid)
        {
            return result;
        }

        Cells = new XlsxWorksheetCell[rowCount, columnCount];
        IsInitialized = true;
        RowCount = rowCount;
        ColumnCount = columnCount;
        CellCount = 0;
        HasCells = false;
        MinRow = 0;
        MaxRow = 0;
        MinColumn = 0;
        MaxColumn = 0;
        return result;
    }

    internal XlsxCellStoreResult Resize(int rowCount, int columnCount)
    {
        XlsxCellStoreResult result = ValidateSize(rowCount, columnCount);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorksheetCell?[,] newCells = new XlsxWorksheetCell[rowCount, columnCount];
        if (IsInitialized)
        {
            int rowsToCopy = RowCount;
            int columnsToCopy = ColumnCount;
            if (rowCount < rowsToCopy)
            {
                rowsToCopy = rowCount;
            }

            if (columnCount < columnsToCopy)
            {
                columnsToCopy = columnCount;
            }

            int row = 0;
            while (row < rowsToCopy)
            {
                int column = 0;
                while (column < columnsToCopy)
                {
                    newCells[row, column] = Cells[row, column];
                    column = column + 1;
                }

                row = row + 1;
            }
        }

        Cells = newCells;
        IsInitialized = true;
        RowCount = rowCount;
        ColumnCount = columnCount;
        RecalculateBounds();
        return result;
    }

    internal XlsxCellStoreResult EnsureCapacity(int row, int column)
    {
        XlsxCellStoreResult result = ValidateWorksheetCoordinate(row, column);
        if (!result.IsValid)
        {
            return result;
        }

        if (!IsInitialized)
        {
            return Initialize(row, column);
        }

        if (row <= RowCount && column <= ColumnCount)
        {
            return result;
        }

        int newRowCount = RowCount;
        int newColumnCount = ColumnCount;
        if (row > newRowCount)
        {
            newRowCount = row;
        }

        if (column > newColumnCount)
        {
            newColumnCount = column;
        }

        return Resize(newRowCount, newColumnCount);
    }

    public XlsxCellStoreResult SetCell(int row, int column, XlsxWorksheetCell? cell)
    {
        XlsxCellStoreResult result = ValidateSet(row, column, cell);
        if (!result.IsValid || cell == null)
        {
            return result;
        }

        XlsxCellStoreResult capacityResult = EnsureCapacity(row, column);
        AddStoreDiagnostics(result, capacityResult);
        if (!result.IsValid)
        {
            return result;
        }

        int rowIndex = row - 1;
        int columnIndex = column - 1;
        if (Cells[rowIndex, columnIndex] == null)
        {
            CellCount = CellCount + 1;
        }

        if (cell.Reference == null || cell.Reference.Length == 0)
        {
            cell.Reference = XlsxA1CellReferenceConverter.FormatString(row, column, false, false);
        }

        Cells[rowIndex, columnIndex] = cell;
        UpdateBoundsAfterSet(row, column);
        result.Cell = cell;
        result.HasCell = true;
        return result;
    }

    public XlsxCellStoreResult SetCellByReference(string reference, XlsxWorksheetCell? cell)
    {
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            AddReferenceDiagnostics(result, referenceResult);
            return result;
        }

        if (cell != null && (cell.Reference == null || cell.Reference.Length == 0))
        {
            cell.Reference = referenceResult.Text;
        }

        XlsxCellStoreResult setResult = SetCell(referenceResult.Row, referenceResult.Column, cell);
        AddStoreDiagnostics(result, setResult);
        result.Cell = setResult.Cell;
        result.HasCell = setResult.HasCell;
        return result;
    }

    public XlsxWorksheetCell? GetCell(int row, int column)
    {
        if (!IsInitialized)
        {
            return null;
        }

        if (!IsValidDenseCoordinate(row, column))
        {
            return null;
        }

        return Cells[row - 1, column - 1];
    }

    public XlsxWorksheetCell? GetCellByReference(string reference)
    {
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            return null;
        }

        return GetCell(referenceResult.Row, referenceResult.Column);
    }

    public bool ContainsCell(int row, int column)
    {
        return GetCell(row, column) != null;
    }

    public bool ContainsCellByReference(string reference)
    {
        return GetCellByReference(reference) != null;
    }

    public XlsxCellStoreResult GetOrCreateCell(int row, int column)
    {
        XlsxWorksheetCell? existing = GetCell(row, column);
        if (existing != null)
        {
            XlsxCellStoreResult existingResult = new XlsxCellStoreResult();
            existingResult.Cell = existing;
            existingResult.HasCell = true;
            return existingResult;
        }

        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        return SetCell(row, column, cell);
    }

    public XlsxCellStoreResult RemoveCell(int row, int column)
    {
        XlsxCellStoreResult result = ValidateWorksheetCoordinate(row, column);
        if (!result.IsValid)
        {
            return result;
        }

        if (!IsInitialized || row > RowCount || column > ColumnCount)
        {
            return result;
        }

        int rowIndex = row - 1;
        int columnIndex = column - 1;
        XlsxWorksheetCell? cell = Cells[rowIndex, columnIndex];
        if (cell == null)
        {
            return result;
        }

        Cells[rowIndex, columnIndex] = null;
        CellCount = CellCount - 1;
        result.Cell = cell;
        result.HasCell = true;
        result.Removed = true;
        RecalculateBounds();
        return result;
    }

    public XlsxCellStoreResult RemoveCellByReference(string reference)
    {
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            AddReferenceDiagnostics(result, referenceResult);
            return result;
        }

        XlsxCellStoreResult removeResult = RemoveCell(referenceResult.Row, referenceResult.Column);
        AddStoreDiagnostics(result, removeResult);
        result.Cell = removeResult.Cell;
        result.HasCell = removeResult.HasCell;
        result.Removed = removeResult.Removed;
        return result;
    }

    internal void ClearCells()
    {
        if (!IsInitialized)
        {
            return;
        }

        int row = 0;
        while (row < RowCount)
        {
            int column = 0;
            while (column < ColumnCount)
            {
                Cells[row, column] = null;
                column = column + 1;
            }

            row = row + 1;
        }

        CellCount = 0;
        HasCells = false;
        MinRow = 0;
        MaxRow = 0;
        MinColumn = 0;
        MaxColumn = 0;
    }

    public void Clear()
    {
        Cells = new XlsxWorksheetCell[0, 0];
        IsInitialized = false;
        RowCount = 0;
        ColumnCount = 0;
        CellCount = 0;
        HasCells = false;
        MinRow = 0;
        MaxRow = 0;
        MinColumn = 0;
        MaxColumn = 0;
    }

    public XlsxCellStoreResult LoadWorksheetPart(XlsxWorksheetPart part)
    {
        Clear();
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        if (part == null)
        {
            result.AddError("denseCellStore.partNull", "Worksheet part must not be null.");
            return result;
        }

        return LoadRows(part.Rows);
    }

    public XlsxCellStoreResult LoadRows(List<XlsxWorksheetRow> rows)
    {
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        if (rows == null)
        {
            result.AddError("denseCellStore.rowsNull", "Worksheet rows list must not be null.");
            return result;
        }

        foreach (XlsxWorksheetRow row in rows)
        {
            if (row == null)
            {
                result.AddError("denseCellStore.rowNull", "Worksheet rows must not contain null entries.");
            }
            else
            {
                LoadRow(result, row);
            }
        }

        return result;
    }

    internal void LoadRow(XlsxCellStoreResult result, XlsxWorksheetRow row)
    {
        if (row.Cells == null)
        {
            result.AddError("denseCellStore.cellsNull", "Worksheet row cell lists must not be null.");
            return;
        }

        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (cell == null)
            {
                result.AddError("denseCellStore.cellNull", "Worksheet cells must not contain null entries.");
            }
            else if (cell.Reference == null || cell.Reference.Length == 0)
            {
                result.AddError("denseCellStore.referenceMissing", "Worksheet cells loaded into a dense store must have A1 references.");
            }
            else
            {
                LoadReferencedCell(result, cell);
            }
        }
    }

    internal void LoadReferencedCell(XlsxCellStoreResult result, XlsxWorksheetCell cell)
    {
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(cell.Reference);
        if (!referenceResult.IsValid)
        {
            AddReferenceDiagnostics(result, referenceResult);
            return;
        }

        XlsxCellStoreResult setResult = SetCell(referenceResult.Row, referenceResult.Column, cell);
        AddStoreDiagnostics(result, setResult);
    }

    internal XlsxCellStoreResult ValidateSet(int row, int column, XlsxWorksheetCell? cell)
    {
        XlsxCellStoreResult result = ValidateWorksheetCoordinate(row, column);
        if (!result.IsValid)
        {
            return result;
        }

        if (cell == null)
        {
            result.AddError("denseCellStore.cellNull", "Dense cell store cell must not be null.");
        }

        return result;
    }

    internal XlsxCellStoreResult ValidateWorksheetCoordinate(int row, int column)
    {
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        if (row <= 0 || row > XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            result.AddError("denseCellStore.rowRange", "Dense cell store row must be within worksheet limits.");
        }

        if (column <= 0 || column > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            result.AddError("denseCellStore.columnRange", "Dense cell store column must be within worksheet limits.");
        }

        return result;
    }

    internal XlsxCellStoreResult ValidateSize(int rowCount, int columnCount)
    {
        XlsxCellStoreResult result = new XlsxCellStoreResult();
        if (rowCount <= 0 || rowCount > XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            result.AddError("denseCellStore.rowCountRange", "Dense cell store row count must be within worksheet limits.");
        }

        if (columnCount <= 0 || columnCount > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            result.AddError("denseCellStore.columnCountRange", "Dense cell store column count must be within worksheet limits.");
        }

        long cellCount = (long)rowCount * (long)columnCount;
        if (cellCount > MaxCellCount)
        {
            result.AddError("denseCellStore.sizeTooLarge", "Dense cell store dimensions exceed the configured cell allocation limit.");
        }

        return result;
    }

    internal bool IsValidDenseCoordinate(int row, int column)
    {
        if (row <= 0 || column <= 0)
        {
            return false;
        }

        if (row > RowCount || column > ColumnCount)
        {
            return false;
        }

        return true;
    }

    internal void UpdateBoundsAfterSet(int row, int column)
    {
        if (!HasCells)
        {
            HasCells = true;
            MinRow = row;
            MaxRow = row;
            MinColumn = column;
            MaxColumn = column;
            return;
        }

        if (row < MinRow)
        {
            MinRow = row;
        }

        if (row > MaxRow)
        {
            MaxRow = row;
        }

        if (column < MinColumn)
        {
            MinColumn = column;
        }

        if (column > MaxColumn)
        {
            MaxColumn = column;
        }
    }

    internal void RecalculateBounds()
    {
        CellCount = 0;
        HasCells = false;
        MinRow = 0;
        MaxRow = 0;
        MinColumn = 0;
        MaxColumn = 0;
        if (!IsInitialized)
        {
            return;
        }

        int row = 1;
        while (row <= RowCount)
        {
            int column = 1;
            while (column <= ColumnCount)
            {
                if (Cells[row - 1, column - 1] != null)
                {
                    CellCount = CellCount + 1;
                    UpdateBoundsAfterSet(row, column);
                }

                column = column + 1;
            }

            row = row + 1;
        }
    }

    public int GetCellCount()
    {
        return CellCount;
    }

    public bool GetHasCells()
    {
        return HasCells;
    }

    public int GetMinRow()
    {
        return MinRow;
    }

    public int GetMaxRow()
    {
        return MaxRow;
    }

    public int GetMinColumn()
    {
        return MinColumn;
    }

    public int GetMaxColumn()
    {
        return MaxColumn;
    }

    internal static void AddReferenceDiagnostics(XlsxCellStoreResult result, XlsxA1CellReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddStoreDiagnostics(XlsxCellStoreResult result, XlsxCellStoreResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}

internal class XlsxWorksheetStorageMetrics
{
    internal int CellCount;
    internal int MaxRow;
    internal int MaxColumn;
    internal long DenseCellCount;
    internal double FillRatio;

    internal XlsxWorksheetStorageMetrics()
    {
        CellCount = 0;
        MaxRow = 0;
        MaxColumn = 0;
        DenseCellCount = 0;
        FillRatio = 0;
    }
}

internal class XlsxWorksheetStorageSelectionResult
{
    internal bool IsValid;
    internal XlsxValidationResult Validation;
    internal XlsxWorksheetStorageKind Kind;
    internal XlsxWorksheetStorageMetrics Metrics;
    internal IXlsxCellStore? Store;
    internal bool HasStore;
    internal bool IsStreaming;

    internal XlsxWorksheetStorageSelectionResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Kind = XlsxWorksheetStorageKind.Unknown;
        Metrics = new XlsxWorksheetStorageMetrics();
        Store = null;
        HasStore = false;
        IsStreaming = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }
}

internal class XlsxWorksheetStorageSelector
{
    internal XlsxWorksheetStorageSelectionResult SelectWorksheetPart(XlsxWorksheetPart part)
    {
        XlsxWorksheetStorageOptions options = new XlsxWorksheetStorageOptions();
        return SelectWorksheetPartWithOptions(part, options);
    }

    internal XlsxWorksheetStorageSelectionResult SelectWorksheetPartWithOptions(XlsxWorksheetPart part, XlsxWorksheetStorageOptions options)
    {
        XlsxWorksheetStorageSelectionResult result = new XlsxWorksheetStorageSelectionResult();
        if (part == null)
        {
            result.AddError("worksheetStorage.partNull", "Worksheet part must not be null.");
            return result;
        }

        if (options == null)
        {
            options = new XlsxWorksheetStorageOptions();
        }

        ValidateOptions(result, options);
        if (!result.IsValid)
        {
            return result;
        }

        result.Metrics = ComputeMetrics(part);
        XlsxWorksheetStorageKind kind = ChooseKind(result.Metrics, options);
        if (kind == XlsxWorksheetStorageKind.Sparse)
        {
            LoadSparse(result, part);
        }
        else if (kind == XlsxWorksheetStorageKind.Dense)
        {
            LoadDense(result, part, options);
        }
        else if (kind == XlsxWorksheetStorageKind.Streaming)
        {
            SelectStreaming(result);
        }

        return result;
    }

    internal static void ValidateOptions(XlsxWorksheetStorageSelectionResult result, XlsxWorksheetStorageOptions options)
    {
        int forceCount = 0;
        if (options.ForceSparse)
        {
            forceCount = forceCount + 1;
        }

        if (options.ForceDense)
        {
            forceCount = forceCount + 1;
        }

        if (options.ForceStreaming)
        {
            forceCount = forceCount + 1;
        }

        if (forceCount > 1)
        {
            result.AddError("worksheetStorage.forceConflict", "Only one worksheet storage force option can be enabled.");
        }

        if (options.MaxDenseCellCount <= 0)
        {
            result.AddError("worksheetStorage.maxDenseCellCountRange", "MaxDenseCellCount must be greater than zero.");
        }

        if (options.DenseFillRatioThreshold <= 0 || options.DenseFillRatioThreshold > 1)
        {
            result.AddError("worksheetStorage.denseFillRatioRange", "DenseFillRatioThreshold must be greater than zero and at most one.");
        }

        if (options.StreamingCellCountThreshold <= 0)
        {
            result.AddError("worksheetStorage.streamingThresholdRange", "StreamingCellCountThreshold must be greater than zero.");
        }
    }

    internal static XlsxWorksheetStorageKind ChooseKind(XlsxWorksheetStorageMetrics metrics, XlsxWorksheetStorageOptions options)
    {
        if (options.ForceSparse)
        {
            return XlsxWorksheetStorageKind.Sparse;
        }

        if (options.ForceDense)
        {
            return XlsxWorksheetStorageKind.Dense;
        }

        if (options.ForceStreaming)
        {
            return XlsxWorksheetStorageKind.Streaming;
        }

        if (metrics.CellCount >= options.StreamingCellCountThreshold)
        {
            return XlsxWorksheetStorageKind.Streaming;
        }

        if (metrics.CellCount > 0 && metrics.DenseCellCount <= options.MaxDenseCellCount && metrics.FillRatio >= options.DenseFillRatioThreshold)
        {
            return XlsxWorksheetStorageKind.Dense;
        }

        return XlsxWorksheetStorageKind.Sparse;
    }

    internal static XlsxWorksheetStorageMetrics ComputeMetrics(XlsxWorksheetPart part)
    {
        XlsxWorksheetStorageMetrics metrics = new XlsxWorksheetStorageMetrics();
        if (part.Rows == null)
        {
            return metrics;
        }

        foreach (XlsxWorksheetRow row in part.Rows)
        {
            if (row == null || row.Cells == null)
            {
                continue;
            }

            foreach (XlsxWorksheetCell cell in row.Cells)
            {
                AddCellToMetrics(metrics, cell);
            }
        }

        if (metrics.MaxRow > 0 && metrics.MaxColumn > 0)
        {
            metrics.DenseCellCount = (long)metrics.MaxRow * (long)metrics.MaxColumn;
            if (metrics.DenseCellCount > 0)
            {
                metrics.FillRatio = (double)metrics.CellCount / (double)metrics.DenseCellCount;
            }
        }

        return metrics;
    }

    internal static void AddCellToMetrics(XlsxWorksheetStorageMetrics metrics, XlsxWorksheetCell cell)
    {
        if (cell == null || cell.Reference == null || cell.Reference.Length == 0)
        {
            return;
        }

        XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(cell.Reference);
        if (!reference.IsValid)
        {
            return;
        }

        metrics.CellCount = metrics.CellCount + 1;
        if (reference.Row > metrics.MaxRow)
        {
            metrics.MaxRow = reference.Row;
        }

        if (reference.Column > metrics.MaxColumn)
        {
            metrics.MaxColumn = reference.Column;
        }
    }

    internal static void LoadSparse(XlsxWorksheetStorageSelectionResult result, XlsxWorksheetPart part)
    {
        result.Kind = XlsxWorksheetStorageKind.Sparse;
        result.Store = new XlsxSparseCellStore();
        LoadStore(result, part);
    }

    internal static void LoadDense(XlsxWorksheetStorageSelectionResult result, XlsxWorksheetPart part, XlsxWorksheetStorageOptions options)
    {
        result.Kind = XlsxWorksheetStorageKind.Dense;
        XlsxDenseCellStore store = new XlsxDenseCellStore();
        store.MaxCellCount = options.MaxDenseCellCount;
        result.Store = store;
        LoadStore(result, part);
    }

    internal static void SelectStreaming(XlsxWorksheetStorageSelectionResult result)
    {
        result.Kind = XlsxWorksheetStorageKind.Streaming;
        result.IsStreaming = true;
    }

    internal static void LoadStore(XlsxWorksheetStorageSelectionResult result, XlsxWorksheetPart part)
    {
        if (result.Store == null)
        {
            return;
        }

        XlsxCellStoreResult loadResult = result.Store.LoadWorksheetPart(part);
        AddStoreDiagnostics(result, loadResult);
        result.HasStore = true;
    }

    internal static void AddStoreDiagnostics(XlsxWorksheetStorageSelectionResult result, XlsxCellStoreResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}

/// <summary>
/// Reads sparkline groups data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
internal static class XlsxSparklineGroupsReader
{
    internal static void ReadWorksheetExtensionList(XlsxWorksheetPart part, string rawXml)
    {
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Office2010Spreadsheet && reader.LocalName == "sparklineGroup")
                    {
                        string groupXml = reader.ReadOuterXml();
                        XlsxSparklineGroup group = ReadGroup(groupXml);
                        part.SparklineGroups.Add(group);
                        part.HasSparklineGroups = true;
                        continue;
                    }

                    reader.Read();
                }
            }
        }
    }

    internal static XlsxSparklineGroup ReadGroup(string rawXml)
    {
        XlsxSparklineGroup group = new XlsxSparklineGroup();
        group.RawXml = rawXml;
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Office2010Spreadsheet)
                    {
                        if (reader.LocalName == "sparklineGroup")
                        {
                            group.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("type"));
                            if (group.Type.Length == 0)
                            {
                                group.Type = "line";
                            }

                            group.DisplayEmptyCellsAs = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("displayEmptyCellsAs"));
                        }
                        else if (reader.LocalName == "sparkline")
                        {
                            string sparklineXml = reader.ReadOuterXml();
                            group.Sparklines.Add(ReadSparkline(sparklineXml));
                            continue;
                        }
                    }

                    reader.Read();
                }
            }
        }

        return group;
    }

    internal static XlsxSparkline ReadSparkline(string rawXml)
    {
        XlsxSparkline sparkline = new XlsxSparkline();
        sparkline.RawXml = rawXml;
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && (reader.NamespaceURI == XlsxNamespaces.OfficeExcel || reader.NamespaceURI == XlsxNamespaces.Spreadsheet))
                    {
                        if (reader.LocalName == "f")
                        {
                            sparkline.Formula = XlsxCellValueConverter.NormalizeText(reader.ReadElementContentAsString());
                            continue;
                        }

                        if (reader.LocalName == "sqref")
                        {
                            sparkline.CellReference = XlsxCellValueConverter.NormalizeText(reader.ReadElementContentAsString());
                            continue;
                        }
                    }

                    reader.Read();
                }
            }
        }

        return sparkline;
    }
}

/// <summary>
/// Validates and writes sparkline groups data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
internal static class XlsxSparklineGroupsWriter
{
    /// <summary>
    /// Identifies the sparkline extension uri OOXML value.
    /// </summary>
    internal const string SparklineExtensionUri = "{05C60535-1F16-4fd2-B633-F4F36F0B64E0}";

    internal static void WriteSparklineExtension(XmlWriter writer, List<XlsxSparklineGroup> groups)
    {
        writer.WriteStartElement("", "ext", XlsxNamespaces.Spreadsheet);
        writer.WriteAttributeString("uri", SparklineExtensionUri);
        writer.WriteStartElement("x14", "sparklineGroups", XlsxNamespaces.Office2010Spreadsheet);
        writer.WriteAttributeString("xmlns", "xm", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.OfficeExcel);
        foreach (XlsxSparklineGroup group in groups)
        {
            WriteGroup(writer, group);
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteGroup(XmlWriter writer, XlsxSparklineGroup group)
    {
        if (group.RawXml != null && group.RawXml.Length > 0)
        {
            writer.WriteRaw(group.RawXml);
            return;
        }

        writer.WriteStartElement("x14", "sparklineGroup", XlsxNamespaces.Office2010Spreadsheet);
        writer.WriteAttributeString("type", NormalizeSparklineType(group.Type));
        writer.WriteAttributeString("displayEmptyCellsAs", NormalizeDisplayEmptyCellsAs(group.DisplayEmptyCellsAs));
        writer.WriteStartElement("x14", "sparklines", XlsxNamespaces.Office2010Spreadsheet);
        foreach (XlsxSparkline sparkline in group.Sparklines)
        {
            WriteSparkline(writer, sparkline);
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteSparkline(XmlWriter writer, XlsxSparkline sparkline)
    {
        if (sparkline.RawXml != null && sparkline.RawXml.Length > 0)
        {
            writer.WriteRaw(sparkline.RawXml);
            return;
        }

        writer.WriteStartElement("x14", "sparkline", XlsxNamespaces.Office2010Spreadsheet);
        writer.WriteStartElement("xm", "f", XlsxNamespaces.OfficeExcel);
        writer.WriteString(XlsxCellValueConverter.NormalizeText(sparkline.Formula));
        writer.WriteEndElement();
        writer.WriteStartElement("xm", "sqref", XlsxNamespaces.OfficeExcel);
        writer.WriteString(XlsxCellValueConverter.NormalizeText(sparkline.CellReference));
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static bool IsSparklineExtension(XlsxExtension extension)
    {
        return extension != null && extension.Uri == SparklineExtensionUri;
    }

    internal static bool IsSupportedSparklineType(string type)
    {
        string value = NormalizeSparklineType(type);
        return value == "line" || value == "column" || value == "stacked";
    }

    internal static string NormalizeSparklineType(string type)
    {
        string value = XlsxCellValueConverter.NormalizeText(type);
        if (value.Length == 0)
        {
            return "line";
        }

        return value;
    }

    internal static string NormalizeDisplayEmptyCellsAs(string value)
    {
        string text = XlsxCellValueConverter.NormalizeText(value);
        if (text.Length == 0)
        {
            return "gap";
        }

        return text;
    }
}

internal class XlsxMinimumPackageValidator
{
    internal XlsxValidationResult ValidateBytes(byte[] bytes, XlsxReaderOptions options)
    {
        XlsxReaderOptions readerOptions = options;
        if (readerOptions == null)
        {
            readerOptions = new XlsxReaderOptions();
        }

        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(bytes, readerOptions);
        XlsxContentTypesPart? contentTypes = null;
        XlsxRelationshipsPart? packageRelationships = null;

        XlsxPackageEntry? contentTypesEntry = package.GetEntry(XlsxPackagePaths.ContentTypes);
        if (contentTypesEntry != null)
        {
            contentTypes = new XlsxContentTypesReader().ReadBytes(contentTypesEntry.Data);
        }

        XlsxPackageEntry? relationshipsEntry = package.GetEntry(XlsxPackagePaths.PackageRelationships);
        if (relationshipsEntry != null)
        {
            packageRelationships = new XlsxPackageRelationshipsReader().ReadBytes(relationshipsEntry.Data, readerOptions);
        }

        return ValidateMinimumXlsxPackage(package, contentTypes, packageRelationships);
    }

    internal XlsxValidationResult ValidateMinimumXlsxPackage(XlsxZipPackageReadResult package, XlsxContentTypesPart? contentTypes, XlsxRelationshipsPart? packageRelationships)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (package == null)
        {
            result.AddError("minimum.packageNull", "ZIP package read result must not be null.");
            return result;
        }

        AddDiagnostics(result, package.Validation);
        ValidateRequiredPackageParts(result, package);
        ValidateContentTypes(result, contentTypes);
        ValidatePackageRelationships(result, packageRelationships);
        return result;
    }

    internal static void ValidateRequiredPackageParts(XlsxValidationResult result, XlsxZipPackageReadResult package)
    {
        if (package.GetEntry(XlsxPackagePaths.ContentTypes) == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "minimum.missingContentTypesPart", "Minimum XLSX package must contain [Content_Types].xml.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "", "");
            result.AddDiagnostic(diagnostic);
        }

        if (package.GetEntry(XlsxPackagePaths.PackageRelationships) == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "minimum.missingPackageRelationshipsPart", "Minimum XLSX package must contain _rels/.rels.");
            diagnostic.SetLocation(XlsxPackagePaths.PackageRelationships, "", "");
            result.AddDiagnostic(diagnostic);
        }

        if (package.GetEntry(XlsxPackagePaths.Workbook) == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "minimum.missingWorkbookPart", "Minimum XLSX package must contain xl/workbook.xml.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "", "");
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateContentTypes(XlsxValidationResult result, XlsxContentTypesPart? contentTypes)
    {
        if (contentTypes == null)
        {
            result.AddError("minimum.contentTypesNull", "Content types part could not be parsed.");
            return;
        }

        AddDiagnostics(result, contentTypes.Validation);

        XlsxContentTypeDefault? relationshipsDefault = contentTypes.GetDefault("rels");
        if (relationshipsDefault == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "minimum.relationshipsDefaultMissing", "Content types must declare the rels default content type.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Default", "Extension");
            result.AddDiagnostic(diagnostic);
        }
        else if (relationshipsDefault.ContentType != XlsxContentTypes.Relationships)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "minimum.relationshipsDefaultWrongContentType", "The rels default content type is incorrect.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Default", "ContentType");
            result.AddDiagnostic(diagnostic);
        }

        XlsxContentTypeOverride? workbookOverride = contentTypes.GetOverride(XlsxPackagePaths.Workbook);
        if (workbookOverride == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "minimum.workbookOverrideMissing", "Content types must contain an override for xl/workbook.xml.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Override", "PartName");
            result.AddDiagnostic(diagnostic);
        }
        else if (!IsSupportedWorkbookContentType(workbookOverride.ContentType))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "minimum.workbookContentTypeUnsupported", "Minimum XLSX package must use the XLSX workbook content type.");
            diagnostic.SetLocation(XlsxPackagePaths.ContentTypes, "Override", "ContentType");
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static bool IsSupportedWorkbookContentType(string? contentType)
    {
        return contentType == XlsxContentTypes.Workbook ||
            contentType == XlsxContentTypes.MacroEnabledWorkbook ||
            contentType == XlsxContentTypes.TemplateWorkbook ||
            contentType == XlsxContentTypes.MacroEnabledTemplateWorkbook;
    }

    internal static void ValidatePackageRelationships(XlsxValidationResult result, XlsxRelationshipsPart? packageRelationships)
    {
        if (packageRelationships == null)
        {
            result.AddError("minimum.relationshipsNull", "Package relationships part could not be parsed.");
            return;
        }

        AddDiagnostics(result, packageRelationships.Validation);

        XlsxRelationship? officeDocument = packageRelationships.GetFirstByType(XlsxRelationshipTypes.OfficeDocument);
        if (officeDocument == null)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "minimum.officeDocumentRelationshipMissing", "Package relationships must contain an officeDocument relationship.");
            diagnostic.SetLocation(XlsxPackagePaths.PackageRelationships, "Relationship", "Type");
            result.AddDiagnostic(diagnostic);
            return;
        }

        bool external = officeDocument.IsExternal || officeDocument.TargetMode == "External";
        XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget("/", officeDocument.Target, external);
        foreach (XlsxDiagnostic diagnostic in target.Validation.Diagnostics)
        {
            diagnostic.SetLocation(XlsxPackagePaths.PackageRelationships, "Relationship", "Target");
            diagnostic.SetRelationshipId(officeDocument.Id);
            result.AddDiagnostic(diagnostic);
        }

        if (external)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "minimum.officeDocumentRelationshipExternal", "Package officeDocument relationship must be internal.");
            diagnostic.SetLocation(XlsxPackagePaths.PackageRelationships, "Relationship", "TargetMode");
            diagnostic.SetRelationshipId(officeDocument.Id);
            result.AddDiagnostic(diagnostic);
            return;
        }

        if (target.Validation.IsValid && target.Path != XlsxPackagePaths.Workbook)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "minimum.officeDocumentTargetWrong", "Package officeDocument relationship must target xl/workbook.xml.");
            diagnostic.SetLocation(XlsxPackagePaths.PackageRelationships, "Relationship", "Target");
            diagnostic.SetRelationshipId(officeDocument.Id);
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddDiagnostics(XlsxValidationResult target, XlsxValidationResult source)
    {
        if (source == null)
        {
            return;
        }

        foreach (XlsxDiagnostic diagnostic in source.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }
}

/// <summary>
/// Represents workbook comparer data used by the public workbook API.
/// </summary>
internal static class XlsxWorkbookComparer
{
    /// <summary>
    /// Performs the compare operation using the current model state.
    /// </summary>
    /// <param name="left">The left value used by the operation.</param>
    /// <param name="right">The right value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookCompareResult Compare(XlsxWorkbook? left, XlsxWorkbook? right)
    {
        return Compare(left, right, new XlsxWorkbookCompareOptions());
    }

    /// <summary>
    /// Performs the compare operation using the current model state.
    /// </summary>
    /// <param name="left">The left value used by the operation.</param>
    /// <param name="right">The right value used by the operation.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookCompareResult Compare(XlsxWorkbook? left, XlsxWorkbook? right, XlsxWorkbookCompareOptions? options)
    {
        XlsxWorkbookCompareResult result = new XlsxWorkbookCompareResult();
        if (left == null || right == null)
        {
            if (left == null)
            {
                result.AddError("workbookCompare.leftNull", "Left workbook must not be null.");
            }

            if (right == null)
            {
                result.AddError("workbookCompare.rightNull", "Right workbook must not be null.");
            }

            return result;
        }

        XlsxWorkbookCompareOptions? effectiveOptions = options;
        if (effectiveOptions == null)
        {
            effectiveOptions = new XlsxWorkbookCompareOptions();
        }

        if (effectiveOptions.ComparePackageVariant)
        {
            CompareText(result, "workbookCompare.packageVariant", "/packageVariant", "Workbook package variants differ.", left.PackageVariant.ToString(), right.PackageVariant.ToString());
        }

        if (effectiveOptions.CompareWorkbookStructure)
        {
            CompareWorkbookParts(result, left.WorkbookPart, right.WorkbookPart);
        }

        if (effectiveOptions.CompareDocumentProperties)
        {
            CompareDocumentProperties(result, left, right);
        }

        if (effectiveOptions.CompareRelationships)
        {
            CompareRelationships(result, "packageRelationships", left.PackageRelationships, right.PackageRelationships);
            CompareRelationships(result, "workbookRelationships", left.WorkbookRelationships, right.WorkbookRelationships);
        }

        if (effectiveOptions.CompareWorksheetCells)
        {
            CompareWorksheets(result, left.Worksheets, right.Worksheets);
        }

        if (effectiveOptions.CompareUnknownParts)
        {
            CompareUnknownParts(result, left.UnknownParts, right.UnknownParts);
        }

        return result;
    }

    internal static void CompareWorkbookParts(XlsxWorkbookCompareResult result, XlsxWorkbookPart left, XlsxWorkbookPart right)
    {
        if (left == null || right == null)
        {
            CompareNullState(result, "workbookCompare.workbookPartMissing", "/xl/workbook.xml", "Workbook part presence differs.", left, right);
            return;
        }

        CompareText(result, "workbookCompare.workbookNamespace", "/xl/workbook.xml", "Workbook namespace differs.", left.NamespaceUri, right.NamespaceUri);
        CompareSheets(result, left.Sheets, right.Sheets);
        CompareDefinedNames(result, left.DefinedNames, right.DefinedNames);
        CompareText(result, "workbookCompare.calculationMode", "/xl/workbook.xml/calcPr", "Workbook calculation mode differs.", GetCalculationMode(left), GetCalculationMode(right));
    }

    internal static void CompareSheets(XlsxWorkbookCompareResult result, List<XlsxWorkbookSheet> left, List<XlsxWorkbookSheet> right)
    {
        int leftCount = CountWorkbookSheets(left);
        int rightCount = CountWorkbookSheets(right);
        CompareText(result, "workbookCompare.sheetCount", "/xl/workbook.xml/sheets", "Workbook sheet counts differ.", leftCount.ToString(CultureInfo.InvariantCulture), rightCount.ToString(CultureInfo.InvariantCulture));
        int count = Min(leftCount, rightCount);
        int index = 0;
        while (index < count)
        {
            XlsxWorkbookSheet leftSheet = left[index];
            XlsxWorkbookSheet rightSheet = right[index];
            string path = "/xl/workbook.xml/sheets/sheet[" + index.ToString(CultureInfo.InvariantCulture) + "]";
            CompareText(result, "workbookCompare.sheetName", path + "/@name", "Workbook sheet names differ.", GetSheetName(leftSheet), GetSheetName(rightSheet));
            CompareText(result, "workbookCompare.sheetState", path + "/@state", "Workbook sheet states differ.", GetSheetState(leftSheet), GetSheetState(rightSheet));
            CompareText(result, "workbookCompare.sheetRelationshipId", path + "/@r:id", "Workbook sheet relationship ids differ.", GetSheetRelationshipId(leftSheet), GetSheetRelationshipId(rightSheet));
            index = index + 1;
        }
    }

    internal static void CompareDefinedNames(XlsxWorkbookCompareResult result, List<XlsxWorkbookDefinedName> left, List<XlsxWorkbookDefinedName> right)
    {
        Dictionary<string, XlsxWorkbookDefinedName> leftMap = BuildDefinedNameMap(left);
        Dictionary<string, XlsxWorkbookDefinedName> rightMap = BuildDefinedNameMap(right);
        CompareText(result, "workbookCompare.definedNameCount", "/xl/workbook.xml/definedNames", "Workbook defined name counts differ.", leftMap.Count.ToString(CultureInfo.InvariantCulture), rightMap.Count.ToString(CultureInfo.InvariantCulture));
        foreach (string key in leftMap.Keys)
        {
            if (!rightMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.definedNameMissingRight", "/xl/workbook.xml/definedNames/" + key, "Defined name is missing from the right workbook.", key, "");
            }
            else
            {
                CompareText(result, "workbookCompare.definedNameValue", "/xl/workbook.xml/definedNames/" + key, "Defined name values differ.", leftMap[key].Text, rightMap[key].Text);
            }
        }

        foreach (string key in rightMap.Keys)
        {
            if (!leftMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.definedNameMissingLeft", "/xl/workbook.xml/definedNames/" + key, "Defined name is missing from the left workbook.", "", key);
            }
        }
    }

    internal static void CompareDocumentProperties(XlsxWorkbookCompareResult result, XlsxWorkbook left, XlsxWorkbook right)
    {
        CompareText(result, "workbookCompare.corePropertiesPresence", "/docProps/core.xml", "Core properties presence differs.", left.HasCoreProperties.ToString(), right.HasCoreProperties.ToString());
        CompareText(result, "workbookCompare.extendedPropertiesPresence", "/docProps/app.xml", "Extended properties presence differs.", left.HasExtendedProperties.ToString(), right.HasExtendedProperties.ToString());
        CompareText(result, "workbookCompare.customPropertiesPresence", "/docProps/custom.xml", "Custom properties presence differs.", left.HasCustomProperties.ToString(), right.HasCustomProperties.ToString());
        if (left.CoreProperties != null && right.CoreProperties != null)
        {
            CompareText(result, "workbookCompare.coreCreator", "/docProps/core.xml/dc:creator", "Core property creators differ.", left.CoreProperties.Creator, right.CoreProperties.Creator);
            CompareText(result, "workbookCompare.coreTitle", "/docProps/core.xml/dc:title", "Core property titles differ.", left.CoreProperties.Title, right.CoreProperties.Title);
            CompareText(result, "workbookCompare.coreLastModifiedBy", "/docProps/core.xml/cp:lastModifiedBy", "Core property lastModifiedBy values differ.", left.CoreProperties.LastModifiedBy, right.CoreProperties.LastModifiedBy);
        }

        if (left.ExtendedProperties != null && right.ExtendedProperties != null)
        {
            CompareText(result, "workbookCompare.extendedManager", "/docProps/app.xml/Manager", "Extended property managers differ.", left.ExtendedProperties.Manager, right.ExtendedProperties.Manager);
            CompareText(result, "workbookCompare.extendedCompany", "/docProps/app.xml/Company", "Extended property companies differ.", left.ExtendedProperties.Company, right.ExtendedProperties.Company);
        }

        CompareCustomProperties(result, left.CustomProperties, right.CustomProperties);
    }

    internal static void CompareCustomProperties(XlsxWorkbookCompareResult result, XlsxCustomPropertiesPart left, XlsxCustomPropertiesPart right)
    {
        Dictionary<string, XlsxCustomProperty> leftMap = BuildCustomPropertyMap(left);
        Dictionary<string, XlsxCustomProperty> rightMap = BuildCustomPropertyMap(right);
        CompareText(result, "workbookCompare.customPropertyCount", "/docProps/custom.xml", "Custom property counts differ.", leftMap.Count.ToString(CultureInfo.InvariantCulture), rightMap.Count.ToString(CultureInfo.InvariantCulture));
        foreach (string key in leftMap.Keys)
        {
            if (!rightMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.customPropertyMissingRight", "/docProps/custom.xml/" + key, "Custom property is missing from the right workbook.", key, "");
            }
            else
            {
                CompareText(result, "workbookCompare.customPropertyValue", "/docProps/custom.xml/" + key, "Custom property values differ.", GetCustomPropertyValue(leftMap[key]), GetCustomPropertyValue(rightMap[key]));
            }
        }

        foreach (string key in rightMap.Keys)
        {
            if (!leftMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.customPropertyMissingLeft", "/docProps/custom.xml/" + key, "Custom property is missing from the left workbook.", "", key);
            }
        }
    }

    internal static void CompareRelationships(XlsxWorkbookCompareResult result, string scope, XlsxRelationshipsPart left, XlsxRelationshipsPart right)
    {
        Dictionary<string, XlsxRelationship> leftMap = BuildRelationshipMap(left);
        Dictionary<string, XlsxRelationship> rightMap = BuildRelationshipMap(right);
        CompareText(result, "workbookCompare.relationshipCount", "/" + scope, "Relationship counts differ.", leftMap.Count.ToString(CultureInfo.InvariantCulture), rightMap.Count.ToString(CultureInfo.InvariantCulture));
        foreach (string key in leftMap.Keys)
        {
            if (!rightMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.relationshipMissingRight", "/" + scope + "/" + key, "Relationship is missing from the right workbook.", key, "");
            }
            else
            {
                CompareText(result, "workbookCompare.relationshipType", "/" + scope + "/" + key + "/@Type", "Relationship types differ.", leftMap[key].Type, rightMap[key].Type);
                CompareText(result, "workbookCompare.relationshipTarget", "/" + scope + "/" + key + "/@Target", "Relationship targets differ.", leftMap[key].Target, rightMap[key].Target);
                CompareText(result, "workbookCompare.relationshipTargetMode", "/" + scope + "/" + key + "/@TargetMode", "Relationship target modes differ.", leftMap[key].TargetMode, rightMap[key].TargetMode);
            }
        }

        foreach (string key in rightMap.Keys)
        {
            if (!leftMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.relationshipMissingLeft", "/" + scope + "/" + key, "Relationship is missing from the left workbook.", "", key);
            }
        }
    }

    internal static void CompareWorksheets(XlsxWorkbookCompareResult result, List<XlsxWorksheetPart> left, List<XlsxWorksheetPart> right)
    {
        Dictionary<string, XlsxWorksheetPart> leftMap = BuildWorksheetMap(left);
        Dictionary<string, XlsxWorksheetPart> rightMap = BuildWorksheetMap(right);
        CompareText(result, "workbookCompare.worksheetPartCount", "/xl/worksheets", "Loaded worksheet part counts differ.", leftMap.Count.ToString(CultureInfo.InvariantCulture), rightMap.Count.ToString(CultureInfo.InvariantCulture));
        foreach (string key in leftMap.Keys)
        {
            if (!rightMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.worksheetMissingRight", key, "Loaded worksheet part is missing from the right workbook.", key, "");
            }
            else
            {
                CompareWorksheetCells(result, key, leftMap[key], rightMap[key]);
            }
        }

        foreach (string key in rightMap.Keys)
        {
            if (!leftMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.worksheetMissingLeft", key, "Loaded worksheet part is missing from the left workbook.", "", key);
            }
        }
    }

    internal static void CompareWorksheetCells(XlsxWorkbookCompareResult result, string worksheetPath, XlsxWorksheetPart left, XlsxWorksheetPart right)
    {
        Dictionary<string, string> leftMap = BuildWorksheetCellMap(left);
        Dictionary<string, string> rightMap = BuildWorksheetCellMap(right);
        CompareText(result, "workbookCompare.cellCount", worksheetPath + "/sheetData", "Worksheet populated cell counts differ.", leftMap.Count.ToString(CultureInfo.InvariantCulture), rightMap.Count.ToString(CultureInfo.InvariantCulture));
        foreach (string key in leftMap.Keys)
        {
            if (!rightMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.cellMissingRight", worksheetPath + "/" + key, "Worksheet cell is missing from the right workbook.", leftMap[key], "");
            }
            else
            {
                CompareText(result, "workbookCompare.cellValue", worksheetPath + "/" + key, "Worksheet cell values differ.", leftMap[key], rightMap[key]);
            }
        }

        foreach (string key in rightMap.Keys)
        {
            if (!leftMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.cellMissingLeft", worksheetPath + "/" + key, "Worksheet cell is missing from the left workbook.", "", rightMap[key]);
            }
        }
    }

    internal static void CompareUnknownParts(XlsxWorkbookCompareResult result, XlsxUnknownPartStore left, XlsxUnknownPartStore right)
    {
        Dictionary<string, XlsxUnknownPart> leftMap = BuildUnknownPartMap(left);
        Dictionary<string, XlsxUnknownPart> rightMap = BuildUnknownPartMap(right);
        CompareText(result, "workbookCompare.unknownPartCount", "/unknownParts", "Unknown part counts differ.", leftMap.Count.ToString(CultureInfo.InvariantCulture), rightMap.Count.ToString(CultureInfo.InvariantCulture));
        foreach (string key in leftMap.Keys)
        {
            if (!rightMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.unknownPartMissingRight", key, "Unknown part is missing from the right workbook.", key, "");
            }
            else
            {
                CompareText(result, "workbookCompare.unknownPartContentType", key, "Unknown part content types differ.", leftMap[key].ContentType, rightMap[key].ContentType);
                CompareText(result, "workbookCompare.unknownPartBytes", key, "Unknown part bytes differ.", GetByteSignature(leftMap[key].Data), GetByteSignature(rightMap[key].Data));
            }
        }

        foreach (string key in rightMap.Keys)
        {
            if (!leftMap.ContainsKey(key))
            {
                result.AddDifference("workbookCompare.unknownPartMissingLeft", key, "Unknown part is missing from the left workbook.", "", key);
            }
        }
    }

    internal static Dictionary<string, XlsxWorkbookDefinedName> BuildDefinedNameMap(List<XlsxWorkbookDefinedName> names)
    {
        Dictionary<string, XlsxWorkbookDefinedName> map = new Dictionary<string, XlsxWorkbookDefinedName>(System.StringComparer.Ordinal);
        if (names == null)
        {
            return map;
        }

        int index = 0;
        while (index < names.Count)
        {
            XlsxWorkbookDefinedName name = names[index];
            if (name != null)
            {
                string key = XlsxCellValueConverter.NormalizeText(name.Name) + "#" + name.HasLocalSheetId.ToString() + "#" + name.LocalSheetId.ToString(CultureInfo.InvariantCulture);
                map[key] = name;
            }

            index = index + 1;
        }

        return map;
    }

    internal static Dictionary<string, XlsxCustomProperty> BuildCustomPropertyMap(XlsxCustomPropertiesPart part)
    {
        Dictionary<string, XlsxCustomProperty> map = new Dictionary<string, XlsxCustomProperty>(System.StringComparer.OrdinalIgnoreCase);
        if (part == null || part.Properties == null)
        {
            return map;
        }

        foreach (XlsxCustomProperty property in part.Properties)
        {
            if (property != null)
            {
                map[XlsxCellValueConverter.NormalizeText(property.Name)] = property;
            }
        }

        return map;
    }

    internal static Dictionary<string, XlsxRelationship> BuildRelationshipMap(XlsxRelationshipsPart part)
    {
        Dictionary<string, XlsxRelationship> map = new Dictionary<string, XlsxRelationship>(System.StringComparer.Ordinal);
        if (part == null || part.Relationships == null)
        {
            return map;
        }

        foreach (XlsxRelationship relationship in part.Relationships)
        {
            if (relationship != null)
            {
                map[XlsxCellValueConverter.NormalizeText(relationship.Id)] = relationship;
            }
        }

        return map;
    }

    internal static Dictionary<string, XlsxWorksheetPart> BuildWorksheetMap(List<XlsxWorksheetPart> worksheets)
    {
        Dictionary<string, XlsxWorksheetPart> map = new Dictionary<string, XlsxWorksheetPart>(System.StringComparer.Ordinal);
        if (worksheets == null)
        {
            return map;
        }

        int index = 0;
        while (index < worksheets.Count)
        {
            XlsxWorksheetPart worksheet = worksheets[index];
            if (worksheet != null)
            {
                string key = worksheet.PartPath;
                if (key == null || key.Length == 0)
                {
                    key = "/worksheet[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                }

                map[key] = worksheet;
            }

            index = index + 1;
        }

        return map;
    }

    internal static Dictionary<string, string> BuildWorksheetCellMap(XlsxWorksheetPart worksheet)
    {
        Dictionary<string, string> map = new Dictionary<string, string>(System.StringComparer.Ordinal);
        if (worksheet == null || worksheet.Rows == null)
        {
            return map;
        }

        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            if (row != null && row.Cells != null)
            {
                int cellSequence = 1;
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null)
                    {
                        string? reference = cell.Reference;
                        if (reference == null || reference.Length == 0)
                        {
                            reference = XlsxA1CellReferenceConverter.FormatString(worksheet.GetEffectiveRowIndex(row, rowSequence), worksheet.GetEffectiveCellColumn(cell, cellSequence), false, false);
                        }

                        map[reference] = GetCellSignature(cell);
                    }

                    cellSequence = cellSequence + 1;
                }
            }

            rowSequence = rowSequence + 1;
        }

        return map;
    }

    internal static Dictionary<string, XlsxUnknownPart> BuildUnknownPartMap(XlsxUnknownPartStore store)
    {
        Dictionary<string, XlsxUnknownPart> map = new Dictionary<string, XlsxUnknownPart>(System.StringComparer.Ordinal);
        if (store == null || store.Parts == null)
        {
            return map;
        }

        foreach (XlsxUnknownPart part in store.Parts)
        {
            if (part != null)
            {
                map[XlsxCellValueConverter.NormalizeText(part.Path)] = part;
            }
        }

        return map;
    }

    internal static string GetCellSignature(XlsxWorksheetCell cell)
    {
        if (cell == null)
        {
            return "";
        }

        return XlsxCellValueConverter.NormalizeText(cell.Type) + "|" +
            XlsxCellValueConverter.NormalizeText(cell.Value) + "|" +
            XlsxCellValueConverter.NormalizeText(cell.Formula) + "|" +
            XlsxCellValueConverter.NormalizeText(cell.InlineStringText);
    }

    internal static string GetCustomPropertyValue(XlsxCustomProperty property)
    {
        if (property == null)
        {
            return "";
        }

        if (property.HasTextValue)
        {
            return property.TextValue;
        }

        if (property.HasIntValue)
        {
            return property.IntValue.ToString(CultureInfo.InvariantCulture);
        }

        if (property.HasDoubleValue)
        {
            return XlsxXmlValueWriter.WriteDouble(property.DoubleValue);
        }

        if (property.HasBoolValue)
        {
            return XlsxXmlValueWriter.WriteBoolean(property.BoolValue);
        }

        if (property.HasDateTimeValue)
        {
            return XlsxXmlValueWriter.WriteDateTime(property.DateTimeValue);
        }

        return XlsxCellValueConverter.NormalizeText(property.RawValueXml);
    }

    internal static string GetByteSignature(byte[] bytes)
    {
        if (bytes == null)
        {
            return "null";
        }

        int hash = 17;
        int index = 0;
        while (index < bytes.Length)
        {
            long nextHash = (long)hash * 31L + bytes[index];
            long wrappedHash = nextHash & 4294967295L;
            if (wrappedHash > int.MaxValue)
            {
                hash = (int)(wrappedHash - 4294967296L);
            }
            else
            {
                hash = (int)wrappedHash;
            }

            index = index + 1;
        }

        return bytes.Length.ToString(CultureInfo.InvariantCulture) + ":" + hash.ToString(CultureInfo.InvariantCulture);
    }

    internal static void CompareText(XlsxWorkbookCompareResult result, string code, string path, string message, string? left, string? right)
    {
        string normalizedLeft = XlsxCellValueConverter.NormalizeText(left);
        string normalizedRight = XlsxCellValueConverter.NormalizeText(right);
        if (normalizedLeft != normalizedRight)
        {
            result.AddDifference(code, path, message, normalizedLeft, normalizedRight);
        }
    }

    internal static void CompareNullState(XlsxWorkbookCompareResult result, string code, string path, string message, object? left, object? right)
    {
        string leftValue = left == null ? "null" : "present";
        string rightValue = right == null ? "null" : "present";
        if (leftValue != rightValue)
        {
            result.AddDifference(code, path, message, leftValue, rightValue);
        }
    }

    internal static int CountWorkbookSheets(List<XlsxWorkbookSheet> items)
    {
        if (items == null)
        {
            return 0;
        }

        return items.Count;
    }

    internal static int Min(int left, int right)
    {
        if (left < right)
        {
            return left;
        }

        return right;
    }

    internal static string? GetSheetName(XlsxWorkbookSheet sheet)
    {
        if (sheet == null)
        {
            return "";
        }

        return sheet.Name;
    }

    internal static string? GetSheetState(XlsxWorkbookSheet sheet)
    {
        if (sheet == null)
        {
            return "";
        }

        return sheet.State;
    }

    internal static string? GetSheetRelationshipId(XlsxWorkbookSheet sheet)
    {
        if (sheet == null)
        {
            return "";
        }

        return sheet.RelationshipId;
    }

    internal static string? GetCalculationMode(XlsxWorkbookPart part)
    {
        if (part == null || !part.HasCalculationProperties || part.CalculationProperties == null)
        {
            return "";
        }

        return part.CalculationProperties.CalcMode;
    }
}
