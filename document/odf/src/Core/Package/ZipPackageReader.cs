using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace odf;

internal static class ZipPackageReader
{
    const uint LocalHeaderSignature = 67324752;
    const uint CentralHeaderSignature = 33639248;
    const uint EndRecordSignature = 101010256;
    const uint Zip64EndRecordSignature = 101075792;
    const uint Zip64LocatorSignature = 117853008;
    const uint CentralSignatureRecordSignature = 84233040;
    const ushort Zip64ExtraFieldIdentifier = 1;
    const ushort Utf8NameFlag = 2048;
    const ushort EncryptedFlag = 1;

    internal static ZipPackageInspection InspectPath(string path)
    {
        return InspectPathWithLimits(path, new OdfSecurityLimits(), 0);
    }

    internal static ZipPackageInspection InspectPathWithLimits(
        string path,
        OdfSecurityLimits limits,
        int nestedPackageDepth)
    {
        byte[] data = File.ReadAllBytes(path);
        return InspectBytesWithSourceName(data, limits, nestedPackageDepth, path);
    }

    internal static ZipPackageInspection InspectStream(Stream stream)
    {
        return InspectStreamWithLimits(stream, new OdfSecurityLimits(), 0);
    }

    internal static ZipPackageInspection InspectStreamWithLimits(
        Stream stream,
        OdfSecurityLimits limits,
        int nestedPackageDepth)
    {
        using (MemoryStream buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            return InspectBytesWithLimits(buffer.ToArray(), limits, nestedPackageDepth);
        }
    }

    internal static ZipPackageInspection InspectBytes(byte[] data)
    {
        return InspectBytesWithLimits(data, new OdfSecurityLimits(), 0);
    }

    internal static ZipPackageInspection InspectBytesWithLimits(
        byte[] data,
        OdfSecurityLimits limits,
        int nestedPackageDepth)
    {
        return InspectBytesWithSourceName(data, limits, nestedPackageDepth, "");
    }

    internal static ZipPackageInspection InspectBytesWithSourceName(
        byte[] data,
        OdfSecurityLimits limits,
        int nestedPackageDepth,
        string sourceName)
    {
        ZipPackageInspection inspection = new ZipPackageInspection();
        inspection.NestedPackageDepth = nestedPackageDepth;
        if (!HaveValidPackageLimits(limits))
        {
            AddFatalDiagnostic(inspection, "Package security limits cannot be negative.");
            return inspection;
        }
        if (nestedPackageDepth < 0)
        {
            AddFatalDiagnostic(inspection, "The nested package depth cannot be negative.");
            return inspection;
        }
        if (nestedPackageDepth > limits.MaximumEmbeddedPackageDepth)
        {
            AddLimitDiagnostic(
                inspection,
                OdfDiagnosticCodes.InvalidPackage,
                $"Nested package depth {nestedPackageDepth} exceeds the configured limit {limits.MaximumEmbeddedPackageDepth}.",
                "");
            return inspection;
        }
        ZipDirectoryInfo directory = ReadDirectoryInfo(data, inspection);
        if (!directory.IsValid)
        {
            return inspection;
        }
        inspection.UsesZip64 = directory.UsesZip64;
        inspection.DirectoryOffset = directory.DirectoryOffset;
        if (directory.EntryCount > limits.MaximumPackageEntries)
        {
            AddLimitDiagnostic(
                inspection,
                OdfDiagnosticCodes.EntryLimitExceeded,
                $"ZIP entry count {directory.EntryCount} exceeds the configured limit {limits.MaximumPackageEntries}.",
                "");
            return inspection;
        }
        if (directory.EntryCount > int.MaxValue)
        {
            AddFatalDiagnostic(inspection, "The ZIP central directory contains too many entries to inspect.");
            return inspection;
        }
        if (directory.DirectoryOffset < 0 || directory.DirectoryOffset > data.LongLength)
        {
            AddFatalDiagnostic(inspection, "The ZIP central-directory offset is outside the package.");
            return inspection;
        }
        if (directory.DirectorySize < 0 || directory.DirectorySize > data.LongLength - directory.DirectoryOffset)
        {
            AddFatalDiagnostic(inspection, "The ZIP central-directory size is outside the package.");
            return inspection;
        }
        if (directory.DirectoryOffset > int.MaxValue || directory.DirectorySize > int.MaxValue)
        {
            AddFatalDiagnostic(inspection, "The ZIP central directory exceeds the supported in-memory range.");
            return inspection;
        }

        int cursor = (int)directory.DirectoryOffset;
        int directoryEnd = cursor + (int)directory.DirectorySize;
        List<OdfPackageEntry> entries = new List<OdfPackageEntry>();
        Dictionary<string, int> names = new Dictionary<string, int>(StringComparer.Ordinal);
        Dictionary<string, string> canonicalNames = new Dictionary<string, string>(StringComparer.Ordinal);
        long totalExpandedSize = 0;
        int entryIndex = 0;
        while (entryIndex < (int)directory.EntryCount)
        {
            if (!HasRange(data, cursor, 46) || ReadUInt32(data, cursor) != CentralHeaderSignature)
            {
                AddFatalDiagnostic(inspection, "A ZIP central-directory entry is missing or malformed.");
                inspection.Entries = entries.ToArray();
                return inspection;
            }

            ushort flags = ReadUInt16(data, cursor + 8);
            ushort compressionCode = ReadUInt16(data, cursor + 10);
            uint expectedCrc = ReadUInt32(data, cursor + 16);
            uint compressed32 = ReadUInt32(data, cursor + 20);
            uint expanded32 = ReadUInt32(data, cursor + 24);
            int nameLength = ReadUInt16(data, cursor + 28);
            int extraLength = ReadUInt16(data, cursor + 30);
            int commentLength = ReadUInt16(data, cursor + 32);
            ushort diskNumber = ReadUInt16(data, cursor + 34);
            uint localOffset32 = ReadUInt32(data, cursor + 42);
            int recordLength = 46 + nameLength + extraLength + commentLength;
            if (!HasRange(data, cursor, recordLength) || cursor + recordLength > directoryEnd)
            {
                AddFatalDiagnostic(inspection, "A ZIP central-directory entry extends beyond the directory.");
                inspection.Entries = entries.ToArray();
                return inspection;
            }

            bool needsExpanded64 = expanded32 == uint.MaxValue;
            bool needsCompressed64 = compressed32 == uint.MaxValue;
            bool needsOffset64 = localOffset32 == uint.MaxValue;
            bool needsDisk64 = diskNumber == ushort.MaxValue;
            bool usesZip64 = needsExpanded64 || needsCompressed64 || needsOffset64 || needsDisk64;
            long expandedSize = expanded32;
            long compressedSize = compressed32;
            long localHeaderOffset = localOffset32;
            if (usesZip64)
            {
                Zip64EntryInfo zip64 = ReadZip64EntryInfo(
                    data,
                    cursor + 46 + nameLength,
                    extraLength,
                    needsExpanded64,
                    needsCompressed64,
                    needsOffset64,
                    needsDisk64);
                if (!zip64.IsValid)
                {
                    AddFatalDiagnostic(inspection, "A ZIP64 entry is missing required ZIP64 extra-field values.");
                    inspection.Entries = entries.ToArray();
                    return inspection;
                }
                if (needsExpanded64)
                {
                    expandedSize = zip64.ExpandedSize;
                }
                if (needsCompressed64)
                {
                    compressedSize = zip64.CompressedSize;
                }
                if (needsOffset64)
                {
                    localHeaderOffset = zip64.LocalHeaderOffset;
                }
                inspection.UsesZip64 = true;
            }

            int nameOffset = cursor + 46;
            bool usesUtf8 = (flags & Utf8NameFlag) != 0;
            bool validNameEncoding = usesUtf8
                ? IsValidUtf8(data, nameOffset, nameLength)
                : IsAscii(data, nameOffset, nameLength);
            string path = DecodeName(data, nameOffset, nameLength, usesUtf8);
            PackagePathValue packagePath = PackagePathRules.CanonicalizeEntryPath(path);
            OdfPackageEntry entry = new OdfPackageEntry
            {
                Path = path,
                CanonicalPath = packagePath.CanonicalPath,
                CompressionMethod = GetCompressionMethod(compressionCode),
                CompressionMethodCode = compressionCode,
                CompressedSize = compressedSize,
                ExpandedSize = expandedSize,
                Crc32 = expectedCrc,
                LocalHeaderOffset = localHeaderOffset,
                CentralExtraFieldLength = extraLength,
                UsesUtf8Name = usesUtf8,
                UsesZip64 = usesZip64,
                UsesDataDescriptor = (flags & 8) != 0,
                IsDirectory = path.EndsWith("/", StringComparison.Ordinal),
                IsCanonicalPath = packagePath.IsCanonical,
                IsEncrypted = (flags & EncryptedFlag) != 0
            };

            if (!validNameEncoding)
            {
                AddEntryDiagnostic(
                    inspection,
                    OdfDiagnosticCodes.InvalidEntryPath,
                    "The ZIP entry name is not valid UTF-8 or ASCII-compatible ZIP text.",
                    path);
            }
            if (packagePath.IsTraversal)
            {
                AddEntryDiagnostic(
                    inspection,
                    OdfDiagnosticCodes.PathTraversal,
                    "The ZIP entry path attempts to escape or bypass the package root.",
                    path);
            }
            else if (!packagePath.IsValid || !packagePath.IsCanonical)
            {
                AddEntryDiagnostic(
                    inspection,
                    OdfDiagnosticCodes.InvalidEntryPath,
                    "The ZIP entry path is not in canonical package-relative form.",
                    path);
            }
            bool duplicateName = names.ContainsKey(path);
            if (duplicateName)
            {
                AddEntryDiagnostic(
                    inspection,
                    OdfDiagnosticCodes.DuplicateEntry,
                    "The ZIP package contains more than one entry with the same name.",
                    path);
            }
            else
            {
                names.Add(path, entryIndex);
            }
            if (packagePath.CanonicalPath.Length > 0)
            {
                if (canonicalNames.ContainsKey(packagePath.CanonicalPath))
                {
                    string relatedPath = canonicalNames[packagePath.CanonicalPath];
                    if (!string.Equals(path, relatedPath, StringComparison.Ordinal))
                    {
                        AddCollisionDiagnostic(
                            inspection,
                            path,
                            relatedPath,
                            packagePath.CanonicalPath);
                    }
                }
                else
                {
                    canonicalNames.Add(packagePath.CanonicalPath, path);
                }
            }
            bool withinLimits = ValidateEntryLimits(
                entry,
                inspection,
                limits,
                totalExpandedSize);
            if (!validNameEncoding || !packagePath.IsValid)
            {
                withinLimits = false;
            }
            if (entry.CompressionMethod == OdfCompressionMethod.Unsupported)
            {
                AddEntryDiagnostic(
                    inspection,
                    OdfDiagnosticCodes.UnsupportedCompression,
                    $"ZIP compression method {compressionCode} is not supported.",
                    path);
            }
            else if (!entry.IsEncrypted && withinLimits)
            {
                ValidateEntryCrc(data, entry, inspection, limits, totalExpandedSize);
            }
            if (withinLimits)
            {
                totalExpandedSize = totalExpandedSize + entry.ExpandedSize;
            }

            entries.Add(entry);
            cursor = cursor + recordLength;
            entryIndex = entryIndex + 1;
        }

        if (!IsValidDirectoryRemainder(data, cursor, directoryEnd))
        {
            AddFatalDiagnostic(inspection, "The ZIP central-directory size does not match its entries.");
        }
        inspection.Entries = entries.ToArray();
        MimetypeInspection mimetype = MimetypeRules.InspectPackage(
            data,
            inspection.Entries,
            sourceName);
        ApplyMimetypeInspection(inspection, mimetype);
        OdfManifest? manifest = ManifestParser.ParsePackageManifest(
            data,
            inspection.Entries);
        if (manifest != null)
        {
            ApplyManifestInspection(inspection, manifest);
        }
        return inspection;
    }

    static ZipDirectoryInfo ReadDirectoryInfo(byte[] data, ZipPackageInspection inspection)
    {
        ZipDirectoryInfo result = new ZipDirectoryInfo();
        int endOffset = FindEndRecord(data);
        if (endOffset < 0)
        {
            AddFatalDiagnostic(inspection, "The ZIP end-of-central-directory record is missing.");
            return result;
        }

        ushort diskNumber = ReadUInt16(data, endOffset + 4);
        ushort directoryDisk = ReadUInt16(data, endOffset + 6);
        ushort entriesOnDisk = ReadUInt16(data, endOffset + 8);
        ushort totalEntries = ReadUInt16(data, endOffset + 10);
        uint directorySize32 = ReadUInt32(data, endOffset + 12);
        uint directoryOffset32 = ReadUInt32(data, endOffset + 16);
        bool sentinelZip64 = diskNumber == ushort.MaxValue
            || directoryDisk == ushort.MaxValue
            || entriesOnDisk == ushort.MaxValue
            || totalEntries == ushort.MaxValue
            || directorySize32 == uint.MaxValue
            || directoryOffset32 == uint.MaxValue;
        bool hasLocator = HasRange(data, endOffset - 20, 20)
            && ReadUInt32(data, endOffset - 20) == Zip64LocatorSignature;
        if (sentinelZip64 || hasLocator)
        {
            if (!hasLocator)
            {
                AddFatalDiagnostic(inspection, "The ZIP64 locator is missing.");
                return result;
            }
            uint zip64Disk = ReadUInt32(data, endOffset - 16);
            ulong zip64OffsetValue = ReadUInt64(data, endOffset - 12);
            uint diskCount = ReadUInt32(data, endOffset - 4);
            if (zip64Disk != 0 || diskCount != 1 || zip64OffsetValue > int.MaxValue)
            {
                AddFatalDiagnostic(inspection, "Multi-disk or out-of-range ZIP64 packages are not supported.");
                return result;
            }
            int zip64Offset = (int)zip64OffsetValue;
            if (!HasRange(data, zip64Offset, 56) || ReadUInt32(data, zip64Offset) != Zip64EndRecordSignature)
            {
                AddFatalDiagnostic(inspection, "The ZIP64 end-of-central-directory record is missing or malformed.");
                return result;
            }
            ulong recordSize = ReadUInt64(data, zip64Offset + 4);
            if (recordSize < 44 || recordSize > (ulong)(data.LongLength - zip64Offset - 12))
            {
                AddFatalDiagnostic(inspection, "The ZIP64 end-of-central-directory size is invalid.");
                return result;
            }
            uint currentDisk = ReadUInt32(data, zip64Offset + 16);
            uint currentDirectoryDisk = ReadUInt32(data, zip64Offset + 20);
            ulong currentDiskEntries = ReadUInt64(data, zip64Offset + 24);
            ulong allEntries = ReadUInt64(data, zip64Offset + 32);
            ulong directorySize64 = ReadUInt64(data, zip64Offset + 40);
            ulong directoryOffset64 = ReadUInt64(data, zip64Offset + 48);
            if (currentDisk != 0 || currentDirectoryDisk != 0 || currentDiskEntries != allEntries)
            {
                AddFatalDiagnostic(inspection, "Multi-disk ZIP64 packages are not supported.");
                return result;
            }
            if (allEntries > long.MaxValue || directorySize64 > long.MaxValue || directoryOffset64 > long.MaxValue)
            {
                AddFatalDiagnostic(inspection, "ZIP64 directory values exceed the supported range.");
                return result;
            }
            result.EntryCount = (long)allEntries;
            result.DirectorySize = (long)directorySize64;
            result.DirectoryOffset = (long)directoryOffset64;
            result.UsesZip64 = true;
            result.IsValid = true;
            return result;
        }

        if (diskNumber != 0 || directoryDisk != 0 || entriesOnDisk != totalEntries)
        {
            AddFatalDiagnostic(inspection, "Multi-disk ZIP packages are not supported.");
            return result;
        }
        result.EntryCount = totalEntries;
        result.DirectorySize = directorySize32;
        result.DirectoryOffset = directoryOffset32;
        result.IsValid = true;
        return result;
    }

    static Zip64EntryInfo ReadZip64EntryInfo(
        byte[] data,
        int extraOffset,
        int extraLength,
        bool needsExpanded,
        bool needsCompressed,
        bool needsOffset,
        bool needsDisk)
    {
        Zip64EntryInfo result = new Zip64EntryInfo();
        int cursor = extraOffset;
        int end = extraOffset + extraLength;
        while (cursor + 4 <= end)
        {
            ushort identifier = ReadUInt16(data, cursor);
            int fieldLength = ReadUInt16(data, cursor + 2);
            cursor = cursor + 4;
            if (fieldLength > end - cursor)
            {
                return result;
            }
            if (identifier == Zip64ExtraFieldIdentifier)
            {
                int valueCursor = cursor;
                int valueEnd = cursor + fieldLength;
                if (needsExpanded)
                {
                    if (!ReadZip64Long(data, valueCursor, valueEnd, result, 1))
                    {
                        return result;
                    }
                    valueCursor = valueCursor + 8;
                }
                if (needsCompressed)
                {
                    if (!ReadZip64Long(data, valueCursor, valueEnd, result, 2))
                    {
                        return result;
                    }
                    valueCursor = valueCursor + 8;
                }
                if (needsOffset)
                {
                    if (!ReadZip64Long(data, valueCursor, valueEnd, result, 3))
                    {
                        return result;
                    }
                    valueCursor = valueCursor + 8;
                }
                if (needsDisk && valueCursor + 4 > valueEnd)
                {
                    return result;
                }
                if (needsDisk && ReadUInt32(data, valueCursor) != 0)
                {
                    return result;
                }
                result.IsValid = true;
                return result;
            }
            cursor = cursor + fieldLength;
        }
        return result;
    }

    static bool ReadZip64Long(byte[] data, int offset, int end, Zip64EntryInfo result, int target)
    {
        if (offset + 8 > end)
        {
            return false;
        }
        ulong value = ReadUInt64(data, offset);
        if (value > long.MaxValue)
        {
            return false;
        }
        if (target == 1)
        {
            result.ExpandedSize = (long)value;
        }
        else if (target == 2)
        {
            result.CompressedSize = (long)value;
        }
        else
        {
            result.LocalHeaderOffset = (long)value;
        }
        return true;
    }

    static bool HaveValidPackageLimits(OdfSecurityLimits limits)
    {
        return limits.MaximumPackageEntries >= 0
            && limits.MaximumTotalExpandedBytes >= 0
            && limits.MaximumSingleEntryBytes >= 0
            && limits.MaximumCompressionRatio >= 0
            && limits.MaximumPathLength >= 0
            && limits.MaximumEmbeddedPackageDepth >= 0;
    }

    static bool ValidateEntryLimits(
        OdfPackageEntry entry,
        ZipPackageInspection inspection,
        OdfSecurityLimits limits,
        long totalExpandedSize)
    {
        bool valid = true;
        if (entry.Path.Length > limits.MaximumPathLength)
        {
            AddLimitDiagnostic(
                inspection,
                OdfDiagnosticCodes.InvalidEntryPath,
                $"ZIP entry path length {entry.Path.Length} exceeds the configured limit {limits.MaximumPathLength}.",
                entry.Path);
            valid = false;
        }
        if (entry.ExpandedSize > limits.MaximumSingleEntryBytes)
        {
            AddLimitDiagnostic(
                inspection,
                OdfDiagnosticCodes.ExpandedSizeLimitExceeded,
                $"Expanded ZIP entry size {entry.ExpandedSize} exceeds the single-entry limit {limits.MaximumSingleEntryBytes}.",
                entry.Path);
            valid = false;
        }
        long remainingTotal = limits.MaximumTotalExpandedBytes - totalExpandedSize;
        if (entry.ExpandedSize > remainingTotal)
        {
            AddLimitDiagnostic(
                inspection,
                OdfDiagnosticCodes.ExpandedSizeLimitExceeded,
                $"Expanded ZIP package size exceeds the configured total limit {limits.MaximumTotalExpandedBytes}.",
                entry.Path);
            valid = false;
        }
        if (ExceedsCompressionRatio(
            entry.ExpandedSize,
            entry.CompressedSize,
            limits.MaximumCompressionRatio))
        {
            AddLimitDiagnostic(
                inspection,
                OdfDiagnosticCodes.CompressionRatioExceeded,
                $"ZIP entry compression ratio exceeds the configured limit {limits.MaximumCompressionRatio}.",
                entry.Path);
            valid = false;
        }
        return valid;
    }

    static void ValidateEntryCrc(
        byte[] data,
        OdfPackageEntry entry,
        ZipPackageInspection inspection,
        OdfSecurityLimits limits,
        long totalExpandedSize)
    {
        if (entry.LocalHeaderOffset < 0 || entry.LocalHeaderOffset > int.MaxValue)
        {
            AddEntryDiagnostic(inspection, OdfDiagnosticCodes.InvalidPackage, "The ZIP local-header offset is invalid.", entry.Path);
            return;
        }
        int localOffset = (int)entry.LocalHeaderOffset;
        if (!HasRange(data, localOffset, 30) || ReadUInt32(data, localOffset) != LocalHeaderSignature)
        {
            AddEntryDiagnostic(inspection, OdfDiagnosticCodes.InvalidPackage, "The ZIP local header is missing or malformed.", entry.Path);
            return;
        }
        int localNameLength = ReadUInt16(data, localOffset + 26);
        int localExtraLength = ReadUInt16(data, localOffset + 28);
        int contentOffset = localOffset + 30 + localNameLength + localExtraLength;
        if (entry.CompressedSize > int.MaxValue || !HasRange(data, contentOffset, (int)entry.CompressedSize))
        {
            AddEntryDiagnostic(inspection, OdfDiagnosticCodes.InvalidPackage, "The ZIP entry data extends beyond the package.", entry.Path);
            return;
        }

        ZipCrcResult actual;
        long maximumExpansion = GetMaximumExpansion(
            entry.CompressedSize,
            limits,
            totalExpandedSize);
        if (entry.CompressionMethod == OdfCompressionMethod.Stored)
        {
            if (entry.CompressedSize > maximumExpansion)
            {
                ReportActualExpansionLimit(
                    inspection,
                    entry,
                    limits,
                    totalExpandedSize,
                    entry.CompressedSize);
                return;
            }
            actual = ComputeStoredCrc(data, contentOffset, (int)entry.CompressedSize);
        }
        else
        {
            actual = ComputeDeflatedCrc(
                data,
                contentOffset,
                (int)entry.CompressedSize,
                maximumExpansion);
        }
        if (actual.LimitExceeded)
        {
            ReportActualExpansionLimit(
                inspection,
                entry,
                limits,
                totalExpandedSize,
                actual.ExpandedSize);
            return;
        }
        if (actual.ExpandedSize != entry.ExpandedSize)
        {
            AddEntryDiagnostic(inspection, OdfDiagnosticCodes.InvalidPackage, "The expanded ZIP entry size does not match the central directory.", entry.Path);
            return;
        }
        if (actual.Crc32 != entry.Crc32)
        {
            AddEntryDiagnostic(inspection, OdfDiagnosticCodes.CrcMismatch, "The expanded ZIP entry CRC-32 does not match the central directory.", entry.Path);
            return;
        }
        entry.HasValidCrc = true;
    }

    static ZipCrcResult ComputeStoredCrc(byte[] data, int offset, int length)
    {
        ZipCrcResult result = new ZipCrcResult();
        uint crc = uint.MaxValue;
        int index = 0;
        while (index < length)
        {
            crc = UpdateCrcByte(crc, data[offset + index]);
            index = index + 1;
        }
        result.Crc32 = ~crc;
        result.ExpandedSize = length;
        return result;
    }

    static ZipCrcResult ComputeDeflatedCrc(
        byte[] data,
        int offset,
        int length,
        long maximumExpansion)
    {
        ZipCrcResult result = new ZipCrcResult();
        uint crc = uint.MaxValue;
        long expandedSize = 0;
        byte[] buffer = new byte[8192];
        using (MemoryStream compressed = new MemoryStream(data, offset, length, false))
        using (DeflateStream inflater = new DeflateStream(compressed, CompressionMode.Decompress, false))
        {
            int readLength = GetExpansionReadLength(maximumExpansion, expandedSize, buffer.Length);
            int count = inflater.Read(buffer, 0, readLength);
            while (count != 0)
            {
                int index = 0;
                while (index < count)
                {
                    crc = UpdateCrcByte(crc, buffer[index]);
                    index = index + 1;
                }
                expandedSize = expandedSize + count;
                if (expandedSize > maximumExpansion)
                {
                    result.LimitExceeded = true;
                    break;
                }
                readLength = GetExpansionReadLength(maximumExpansion, expandedSize, buffer.Length);
                count = inflater.Read(buffer, 0, readLength);
            }
        }
        result.Crc32 = ~crc;
        result.ExpandedSize = expandedSize;
        return result;
    }

    static int GetExpansionReadLength(long maximumExpansion, long expandedSize, int bufferLength)
    {
        long remaining = maximumExpansion - expandedSize;
        if (remaining < bufferLength)
        {
            return (int)remaining + 1;
        }
        return bufferLength;
    }

    static long GetMaximumExpansion(
        long compressedSize,
        OdfSecurityLimits limits,
        long totalExpandedSize)
    {
        long maximum = limits.MaximumSingleEntryBytes;
        long remainingTotal = limits.MaximumTotalExpandedBytes - totalExpandedSize;
        if (remainingTotal < maximum)
        {
            maximum = remainingTotal;
        }
        long ratioMaximum = GetRatioExpansionLimit(
            compressedSize,
            limits.MaximumCompressionRatio);
        if (ratioMaximum < maximum)
        {
            maximum = ratioMaximum;
        }
        return maximum;
    }

    static long GetRatioExpansionLimit(long compressedSize, int maximumRatio)
    {
        if (compressedSize == 0 || maximumRatio == 0)
        {
            return 0;
        }
        if (compressedSize > long.MaxValue / maximumRatio)
        {
            return long.MaxValue;
        }
        return compressedSize * maximumRatio;
    }

    static bool ExceedsCompressionRatio(
        long expandedSize,
        long compressedSize,
        int maximumRatio)
    {
        if (compressedSize == 0)
        {
            return expandedSize > 0;
        }
        long quotient = expandedSize / compressedSize;
        long remainder = expandedSize % compressedSize;
        return quotient > maximumRatio
            || quotient == maximumRatio && remainder > 0;
    }

    static void ReportActualExpansionLimit(
        ZipPackageInspection inspection,
        OdfPackageEntry entry,
        OdfSecurityLimits limits,
        long totalExpandedSize,
        long actualExpandedSize)
    {
        bool reported = false;
        if (actualExpandedSize > limits.MaximumSingleEntryBytes
            || actualExpandedSize > limits.MaximumTotalExpandedBytes - totalExpandedSize)
        {
            AddLimitDiagnostic(
                inspection,
                OdfDiagnosticCodes.ExpandedSizeLimitExceeded,
                "Expanded ZIP entry data exceeds a configured size limit.",
                entry.Path);
            reported = true;
        }
        if (ExceedsCompressionRatio(
            actualExpandedSize,
            entry.CompressedSize,
            limits.MaximumCompressionRatio))
        {
            AddLimitDiagnostic(
                inspection,
                OdfDiagnosticCodes.CompressionRatioExceeded,
                $"Actual ZIP entry compression ratio exceeds the configured limit {limits.MaximumCompressionRatio}.",
                entry.Path);
            reported = true;
        }
        if (!reported)
        {
            AddLimitDiagnostic(
                inspection,
                OdfDiagnosticCodes.ExpandedSizeLimitExceeded,
                "Expanded ZIP entry data exceeds the configured package limits.",
                entry.Path);
        }
    }

    static uint UpdateCrcByte(uint crc, byte value)
    {
        uint current = crc ^ value;
        int bit = 0;
        while (bit < 8)
        {
            if ((current & 1) != 0)
            {
                current = (current >> 1) ^ 3988292384;
            }
            else
            {
                current = current >> 1;
            }
            bit = bit + 1;
        }
        return current;
    }

    static OdfCompressionMethod GetCompressionMethod(ushort code)
    {
        if (code == 0)
        {
            return OdfCompressionMethod.Stored;
        }
        if (code == 8)
        {
            return OdfCompressionMethod.Deflated;
        }
        return OdfCompressionMethod.Unsupported;
    }

    static string DecodeName(byte[] data, int offset, int length, bool usesUtf8)
    {
        if (usesUtf8)
        {
            return Encoding.UTF8.GetString(data, offset, length);
        }
        return Encoding.Latin1.GetString(data, offset, length);
    }

    static bool IsAscii(byte[] data, int offset, int length)
    {
        int index = 0;
        while (index < length)
        {
            if (data[offset + index] > 127)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsValidUtf8(byte[] data, int offset, int length)
    {
        int index = 0;
        int end = offset + length;
        while (offset + index < end)
        {
            byte first = data[offset + index];
            if (first <= 127)
            {
                index = index + 1;
                continue;
            }
            if (first >= 194 && first <= 223)
            {
                if (!HasContinuation(data, offset + index + 1, end))
                {
                    return false;
                }
                index = index + 2;
                continue;
            }
            if (first >= 224 && first <= 239)
            {
                if (offset + index + 2 >= end)
                {
                    return false;
                }
                byte second = data[offset + index + 1];
                byte third = data[offset + index + 2];
                if (second < 128 || second > 191 || third < 128 || third > 191)
                {
                    return false;
                }
                if (first == 224 && second < 160)
                {
                    return false;
                }
                if (first == 237 && second > 159)
                {
                    return false;
                }
                index = index + 3;
                continue;
            }
            if (first >= 240 && first <= 244)
            {
                if (offset + index + 3 >= end)
                {
                    return false;
                }
                byte second = data[offset + index + 1];
                byte third = data[offset + index + 2];
                byte fourth = data[offset + index + 3];
                if (second < 128 || second > 191 || third < 128 || third > 191 || fourth < 128 || fourth > 191)
                {
                    return false;
                }
                if (first == 240 && second < 144)
                {
                    return false;
                }
                if (first == 244 && second > 143)
                {
                    return false;
                }
                index = index + 4;
                continue;
            }
            return false;
        }
        return true;
    }

    static bool HasContinuation(byte[] data, int offset, int end)
    {
        return offset < end && data[offset] >= 128 && data[offset] <= 191;
    }

    static bool IsValidDirectoryRemainder(byte[] data, int offset, int end)
    {
        if (offset == end)
        {
            return true;
        }
        if (!HasRange(data, offset, 6) || ReadUInt32(data, offset) != CentralSignatureRecordSignature)
        {
            return false;
        }
        int signatureLength = ReadUInt16(data, offset + 4);
        return offset + 6 + signatureLength == end;
    }

    static int FindEndRecord(byte[] data)
    {
        if (data.Length < 22)
        {
            return -1;
        }
        int minimum = data.Length - 65557;
        if (minimum < 0)
        {
            minimum = 0;
        }
        int offset = data.Length - 22;
        while (offset >= minimum)
        {
            if (ReadUInt32(data, offset) == EndRecordSignature)
            {
                int commentLength = ReadUInt16(data, offset + 20);
                if (offset + 22 + commentLength == data.Length)
                {
                    return offset;
                }
            }
            offset = offset - 1;
        }
        return -1;
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

    static ulong ReadUInt64(byte[] data, int offset)
    {
        return ReadUInt32(data, offset) | ((ulong)ReadUInt32(data, offset + 4) << 32);
    }

    static void AddEntryDiagnostic(
        ZipPackageInspection inspection,
        string code,
        string message,
        string path)
    {
        OdfLocation location = new OdfLocation();
        location.PartPath = path;
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            code,
            message,
            OdfValidationSeverity.Error,
            location,
            null,
            OdfVersion.Unknown);
        AppendDiagnostic(inspection, diagnostic);
        inspection.IsValid = false;
    }

    static void ApplyMimetypeInspection(
        ZipPackageInspection inspection,
        MimetypeInspection mimetype)
    {
        inspection.MediaType = mimetype.MediaType;
        inspection.ManifestMediaType = mimetype.ManifestMediaType;
        inspection.DocumentKind = mimetype.DocumentKind;
        inspection.HasMimetype = mimetype.HasMimetype;
        inspection.MimetypeIsValid = mimetype.MimetypeIsValid;
        inspection.MediaTypeWasInferred = mimetype.MediaTypeWasInferred;
        foreach (OdfDiagnostic diagnostic in mimetype.Diagnostics)
        {
            AppendDiagnostic(inspection, diagnostic);
            if (diagnostic.Severity == OdfValidationSeverity.Error
                || diagnostic.Severity == OdfValidationSeverity.Fatal)
            {
                inspection.IsValid = false;
            }
        }
    }

    static void ApplyManifestInspection(
        ZipPackageInspection inspection,
        OdfManifest manifest)
    {
        inspection.Manifest = manifest;
        foreach (OdfDiagnostic diagnostic in manifest.Diagnostics)
        {
            AppendDiagnostic(inspection, diagnostic);
            if (diagnostic.Severity == OdfValidationSeverity.Error
                || diagnostic.Severity == OdfValidationSeverity.Fatal)
            {
                inspection.IsValid = false;
            }
        }
    }

    static void AddLimitDiagnostic(
        ZipPackageInspection inspection,
        string code,
        string message,
        string path)
    {
        OdfLocation location = new OdfLocation();
        location.PartPath = path;
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            code,
            message,
            OdfValidationSeverity.Fatal,
            location,
            null,
            OdfVersion.Unknown);
        AppendDiagnostic(inspection, diagnostic);
        inspection.IsValid = false;
    }

    static void AddCollisionDiagnostic(
        ZipPackageInspection inspection,
        string path,
        string relatedPath,
        string canonicalPath)
    {
        OdfLocation location = new OdfLocation();
        location.PartPath = path;
        OdfLocation related = new OdfLocation();
        related.PartPath = relatedPath;
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.PathNormalizationCollision,
            $"ZIP entry paths normalize to the same package path '{canonicalPath}'.",
            OdfValidationSeverity.Error,
            location,
            related,
            OdfVersion.Unknown);
        AppendDiagnostic(inspection, diagnostic);
        inspection.IsValid = false;
    }

    static void AddFatalDiagnostic(ZipPackageInspection inspection, string message)
    {
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.InvalidPackage,
            message,
            OdfValidationSeverity.Fatal,
            null,
            null,
            OdfVersion.Unknown);
        AppendDiagnostic(inspection, diagnostic);
        inspection.IsValid = false;
    }

    static void AppendDiagnostic(ZipPackageInspection inspection, OdfDiagnostic diagnostic)
    {
        OdfDiagnostic normalized = OdfDiagnosticFactory.Normalize(
            diagnostic,
            OdfVersion.Unknown);
        OdfDiagnostic[] updated = new OdfDiagnostic[inspection.Diagnostics.Length + 1];
        Array.Copy(inspection.Diagnostics, updated, inspection.Diagnostics.Length);
        updated[updated.Length - 1] = normalized;
        inspection.Diagnostics = updated;
    }
}
