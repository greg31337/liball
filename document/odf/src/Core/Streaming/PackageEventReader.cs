using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace odf;

internal class PackageEventReader
{
    const uint LocalHeaderSignature = 67324752;
    const uint CentralHeaderSignature = 33639248;
    const uint EndRecordSignature = 101010256;
    const uint DataDescriptorSignature = 134695760;
    const int EncryptedFlag = 1;
    const int DataDescriptorFlag = 8;
    const int Utf8NameFlag = 2048;
    const int StoredMethod = 0;
    const int DeflatedMethod = 8;
    const int Zip64ExtraIdentifier = 1;

    Stream Input;
    OdfLoadOptions Options;
    OdfStreamSelection Selection;
    OdfSecurityLimits Limits;
    OdfPackageEntry? CurrentEntry;
    byte[] CurrentContent = [];
    bool CurrentConsumed;
    bool CurrentSkipped;
    bool Finished;
    bool InputClosed;
    long Offset;
    long TotalExpandedBytes;
    int EntryCount;

    internal PackageEventReader(Stream input, OdfLoadOptions options)
    {
        if (input == null || !input.CanRead)
        {
            throw new OdfPackageException(
                "A readable package input stream is required.");
        }
        if (options == null || options.StreamSelection == null
            || options.SecurityLimits == null
            || options.Cancellation == null)
        {
            throw new OdfPackageException(
                "Package streaming options are required.");
        }
        ValidateLimits(options.SecurityLimits);
        Input = input;
        Options = options;
        Selection = options.StreamSelection;
        Limits = options.SecurityLimits;
    }

    internal OdfStreamEvent ReadNextEvent()
    {
        CheckCancellation();
        if (Finished)
        {
            return EndOfStreamEvent();
        }
        if (CurrentEntry != null)
        {
            return CompleteCurrentEntry();
        }
        while (!Finished)
        {
            long headerOffset = Offset;
            byte[] signatureBytes = ReadOptionalBytes(4);
            if (signatureBytes.Length == 0)
            {
                return FinishInput();
            }
            if (signatureBytes.Length != 4)
            {
                throw FailPackage(
                    "The package ended inside a ZIP record signature.");
            }
            uint signature = ReadUInt32Value(signatureBytes, 0);
            if (signature == CentralHeaderSignature
                || signature == EndRecordSignature)
            {
                DrainRemainingInput();
                return FinishInput();
            }
            if (signature != LocalHeaderSignature)
            {
                throw FailPackage(
                    "A ZIP local-file header was expected in the forward stream.");
            }
            OdfPackageEntry entry = ReadEntryHeader(headerOffset);
            EntryCount = EntryCount + 1;
            if (EntryCount > Limits.MaximumPackageEntries)
            {
                throw FailPackage(
                    "The package entry count exceeds the configured limit.");
            }
            if (entry.CompressedSize >= 0 && entry.ExpandedSize >= 0)
            {
                ValidateAndAccumulateEntry(entry);
            }
            bool selected = IsEntrySelected(entry);
            if (!selected)
            {
                SkipEntryData(entry);
            }
            else if (Selection.IncludePackageEntryEvents)
            {
                CurrentEntry = entry;
                CurrentContent = [];
                CurrentConsumed = false;
                CurrentSkipped = false;
                return PackageBoundaryEvent(
                    OdfStreamEventKind.PackageEntryStart,
                    entry,
                    false,
                    [],
                    headerOffset);
            }
            else
            {
                SkipEntryData(entry);
            }
        }
        return EndOfStreamEvent();
    }

    internal void SkipCurrentEntry()
    {
        CheckCancellation();
        if (Finished || CurrentEntry == null)
        {
            throw new OdfPackageException(
                "There is no current package entry to skip.");
        }
        if (!CurrentConsumed)
        {
            SkipEntryData(CurrentEntry);
            CurrentConsumed = true;
        }
        CurrentContent = [];
        CurrentSkipped = true;
    }

    internal byte[] ReadCurrentEntryBytes()
    {
        CheckCancellation();
        if (Finished || CurrentEntry == null)
        {
            throw new OdfPackageException(
                "There is no current package entry to read.");
        }
        if (CurrentSkipped)
        {
            return [];
        }
        if (!CurrentConsumed)
        {
            CurrentContent = ReadExpandedEntry(CurrentEntry);
            CurrentConsumed = true;
        }
        byte[] result = new byte[CurrentContent.Length];
        Array.Copy(CurrentContent, result, result.Length);
        return result;
    }

    static void ValidateLimits(OdfSecurityLimits limits)
    {
        if (limits.MaximumPackageEntries < 0
            || limits.MaximumTotalExpandedBytes < 0
            || limits.MaximumSingleEntryBytes < 0
            || limits.MaximumCompressionRatio < 0
            || limits.MaximumPathLength < 0)
        {
            throw new OdfPackageException(
                "Package security limits cannot be negative.");
        }
    }

    OdfStreamEvent CompleteCurrentEntry()
    {
        OdfPackageEntry entry = CurrentEntry ?? new OdfPackageEntry();
        if (!CurrentConsumed)
        {
            if (Selection.IncludeBinaryContent)
            {
                CurrentContent = ReadExpandedEntry(entry);
            }
            else
            {
                SkipEntryData(entry);
            }
            CurrentConsumed = true;
        }
        byte[] content = [];
        if (Selection.IncludeBinaryContent && !CurrentSkipped)
        {
            content = CurrentContent;
        }
        OdfStreamEvent result = PackageBoundaryEvent(
            OdfStreamEventKind.PackageEntryEnd,
            entry,
            CurrentSkipped,
            content,
            Offset);
        CurrentEntry = null;
        CurrentContent = [];
        CurrentConsumed = false;
        CurrentSkipped = false;
        return result;
    }

    OdfPackageEntry ReadEntryHeader(long headerOffset)
    {
        byte[] header = ReadRequiredBytes(
            26,
            "The package ended inside a ZIP local-file header.");
        int flags = ReadUInt16Value(header, 2);
        int compressionMethod = ReadUInt16Value(header, 4);
        uint crc = ReadUInt32Value(header, 10);
        long compressedSize = ReadUInt32Value(header, 14);
        long expandedSize = ReadUInt32Value(header, 18);
        int nameLength = ReadUInt16Value(header, 22);
        int extraLength = ReadUInt16Value(header, 24);
        byte[] nameBytes = ReadRequiredBytes(
            nameLength,
            "The package ended inside a ZIP entry name.");
        byte[] extraBytes = ReadRequiredBytes(
            extraLength,
            "The package ended inside a ZIP local extra field.");
        bool usesZip64 = compressedSize == uint.MaxValue
            || expandedSize == uint.MaxValue;
        if (usesZip64)
        {
            StreamingZip64Sizes sizes = ReadZip64Sizes(
                extraBytes,
                expandedSize == uint.MaxValue,
                compressedSize == uint.MaxValue);
            if (!sizes.IsValid)
            {
                throw FailPackage(
                    "A ZIP64 entry is missing local size information.");
            }
            if (expandedSize == uint.MaxValue)
            {
                expandedSize = sizes.ExpandedSize;
            }
            if (compressedSize == uint.MaxValue)
            {
                compressedSize = sizes.CompressedSize;
            }
        }
        if ((flags & DataDescriptorFlag) != 0
            && compressedSize == 0 && expandedSize == 0)
        {
            compressedSize = -1;
            expandedSize = -1;
        }
        string path = DecodeEntryName(nameBytes, flags);
        PackagePathValue canonical =
            PackagePathRules.CanonicalizeEntryPath(path);
        if (!canonical.IsValid || canonical.IsTraversal)
        {
            throw FailPackage("A package entry path is invalid or unsafe.");
        }
        OdfPackageEntry result = new OdfPackageEntry();
        result.Path = path;
        result.CanonicalPath = canonical.CanonicalPath;
        result.CompressionMethodCode = compressionMethod;
        if (compressionMethod == StoredMethod)
        {
            result.CompressionMethod = OdfCompressionMethod.Stored;
        }
        else if (compressionMethod == DeflatedMethod)
        {
            result.CompressionMethod = OdfCompressionMethod.Deflated;
        }
        else
        {
            result.CompressionMethod = OdfCompressionMethod.Unsupported;
        }
        result.CompressedSize = compressedSize;
        result.ExpandedSize = expandedSize;
        result.Crc32 = crc;
        result.LocalHeaderOffset = headerOffset;
        result.LocalExtraFieldLength = extraLength;
        result.UsesUtf8Name = (flags & Utf8NameFlag) != 0;
        result.UsesZip64 = usesZip64;
        result.UsesDataDescriptor = (flags & DataDescriptorFlag) != 0;
        result.IsDirectory = path.EndsWith("/", StringComparison.Ordinal);
        result.IsCanonicalPath = canonical.IsCanonical;
        result.IsEncrypted = (flags & EncryptedFlag) != 0;
        return result;
    }

    void ValidateEntryMetadata(OdfPackageEntry entry)
    {
        if (entry.IsEncrypted)
        {
            throw FailPackage(
                "Encrypted ZIP entries cannot be read by the package event stream.");
        }
        if (entry.CompressionMethod == OdfCompressionMethod.Unsupported)
        {
            throw FailPackage(
                "The package entry uses an unsupported compression method.");
        }
        if (entry.Path.Length > Limits.MaximumPathLength)
        {
            throw FailPackage(
                "A package entry path exceeds the configured length limit.");
        }
        if (entry.ExpandedSize < 0 || entry.CompressedSize < 0
            || entry.ExpandedSize > Limits.MaximumSingleEntryBytes)
        {
            throw FailPackage(
                "A package entry exceeds the configured expanded-size limit.");
        }
        if (entry.ExpandedSize
            > Limits.MaximumTotalExpandedBytes - TotalExpandedBytes)
        {
            throw FailPackage(
                "The package exceeds the configured total expanded-size limit.");
        }
        if (CompressionRatioExceeded(
            entry.ExpandedSize,
            entry.CompressedSize,
            Limits.MaximumCompressionRatio))
        {
            throw FailPackage(
                "A package entry exceeds the configured compression-ratio limit.");
        }
    }

    void ValidateAndAccumulateEntry(OdfPackageEntry entry)
    {
        ValidateEntryMetadata(entry);
        TotalExpandedBytes = TotalExpandedBytes + entry.ExpandedSize;
    }

    bool IsEntrySelected(OdfPackageEntry entry)
    {
        if (Selection.PartPaths == null || Selection.PartPaths.Length == 0)
        {
            return true;
        }
        int index = 0;
        while (index < Selection.PartPaths.Length)
        {
            string selectedPath = Selection.PartPaths[index];
            if (selectedPath != null
                && (selectedPath == entry.Path
                    || selectedPath == entry.CanonicalPath))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    void SkipEntryData(OdfPackageEntry entry)
    {
        if (entry.CompressedSize < 0 || entry.ExpandedSize < 0)
        {
            ReadUnknownDescriptorData(entry, false);
            return;
        }
        SkipRequiredBytes(
            entry.CompressedSize,
            "The package ended inside compressed entry data.");
        if (entry.UsesDataDescriptor)
        {
            ReadKnownDataDescriptor(entry);
        }
    }

    byte[] ReadExpandedEntry(OdfPackageEntry entry)
    {
        byte[] compressed;
        if (entry.CompressedSize < 0 || entry.ExpandedSize < 0)
        {
            compressed = ReadUnknownDescriptorData(entry, true);
        }
        else
        {
            if (entry.CompressedSize > int.MaxValue
                || entry.ExpandedSize > int.MaxValue)
            {
                throw FailPackage(
                    "The selected package entry is too large to expose as binary content.");
            }
            compressed = ReadRequiredBytes(
                (int)entry.CompressedSize,
                "The package ended inside compressed entry data.");
            if (entry.UsesDataDescriptor)
            {
                ReadKnownDataDescriptor(entry);
            }
        }
        if (entry.CompressedSize > int.MaxValue
            || entry.ExpandedSize > int.MaxValue)
        {
            throw FailPackage(
                "The selected package entry is too large to expose as binary content.");
        }
        byte[] expanded;
        if (entry.CompressionMethod == OdfCompressionMethod.Stored)
        {
            expanded = compressed;
        }
        else
        {
            expanded = InflateEntry(compressed, entry);
        }
        if (expanded.LongLength != entry.ExpandedSize)
        {
            throw FailPackage(
                "The expanded package-entry size does not match its ZIP header.");
        }
        if (entry.Crc32 != 0 && ComputeCrc32(expanded) != entry.Crc32)
        {
            throw FailPackage(
                "The expanded package-entry CRC does not match its ZIP header.");
        }
        entry.HasValidCrc = entry.Crc32 == 0
            || ComputeCrc32(expanded) == entry.Crc32;
        return expanded;
    }

    byte[] InflateEntry(byte[] compressed, OdfPackageEntry entry)
    {
        using (MemoryStream source = new MemoryStream(compressed, false))
        using (DeflateStream inflater = new DeflateStream(
            source,
            CompressionMode.Decompress,
            false))
        using (MemoryStream output = new MemoryStream())
        {
            byte[] buffer = new byte[8192];
            int count = inflater.Read(buffer, 0, buffer.Length);
            while (count != 0)
            {
                CheckCancellation();
                if (output.Length + count > Limits.MaximumSingleEntryBytes
                    || output.Length + count > entry.ExpandedSize)
                {
                    throw FailPackage(
                        "Expanded package-entry data exceeds its allowed size.");
                }
                output.Write(buffer, 0, count);
                count = inflater.Read(buffer, 0, buffer.Length);
            }
            return output.ToArray();
        }
    }

    void ReadKnownDataDescriptor(OdfPackageEntry entry)
    {
        byte[] marker = ReadRequiredBytes(
            4,
            "The package ended before a ZIP data descriptor.");
        uint signature = ReadUInt32Value(marker, 0);
        uint descriptorCrc;
        byte[] descriptor;
        if (signature == DataDescriptorSignature)
        {
            int descriptorLength = entry.UsesZip64 ? 20 : 12;
            descriptor = ReadRequiredBytes(
                descriptorLength,
                "The package ended inside a ZIP data descriptor.");
            descriptorCrc = ReadUInt32Value(descriptor, 0);
        }
        else
        {
            int descriptorLength = entry.UsesZip64 ? 16 : 8;
            byte[] remainder = ReadRequiredBytes(
                descriptorLength,
                "The package ended inside a ZIP data descriptor.");
            descriptor = new byte[descriptorLength + 4];
            Array.Copy(marker, descriptor, 4);
            Array.Copy(remainder, 0, descriptor, 4, remainder.Length);
            descriptorCrc = signature;
        }
        long descriptorCompressed = entry.UsesZip64
            ? ReadInt64Value(descriptor, 4)
            : ReadUInt32Value(descriptor, 4);
        long descriptorExpanded = entry.UsesZip64
            ? ReadInt64Value(descriptor, 12)
            : ReadUInt32Value(descriptor, 8);
        if (descriptorCompressed != entry.CompressedSize
            || descriptorExpanded != entry.ExpandedSize)
        {
            throw FailPackage(
                "A ZIP data descriptor does not match its local entry sizes.");
        }
        entry.Crc32 = descriptorCrc;
    }

    byte[] ReadUnknownDescriptorData(
        OdfPackageEntry entry,
        bool preserveContent)
    {
        using (MemoryStream content = new MemoryStream())
        {
            byte[] one = new byte[1];
            uint marker = 0;
            long compressedCount = 0;
            long maximumCompressed = MaximumCompressedScanLength();
            while (true)
            {
                CheckCancellation();
                int read = Input.Read(one, 0, 1);
                if (read == 0)
                {
                    throw FailPackage(
                        "A ZIP data descriptor could not be found.");
                }
                Offset = Offset + 1;
                compressedCount = compressedCount + 1;
                if (compressedCount > maximumCompressed)
                {
                    throw FailPackage(
                        "Compressed package-entry data exceeds its safe scan limit.");
                }
                if (preserveContent)
                {
                    content.Write(one, 0, 1);
                }
                marker = marker >> 8 | (uint)one[0] << 24;
                if (compressedCount >= 4
                    && marker == DataDescriptorSignature)
                {
                    compressedCount = compressedCount - 4;
                    if (preserveContent)
                    {
                        content.SetLength(content.Length - 4);
                    }
                    byte[] descriptor = ReadRequiredBytes(
                        12,
                        "The package ended inside a ZIP data descriptor.");
                    uint crc = ReadUInt32Value(descriptor, 0);
                    long compressedSize = ReadUInt32Value(descriptor, 4);
                    long expandedSize = ReadUInt32Value(descriptor, 8);
                    if (compressedSize != compressedCount)
                    {
                        throw FailPackage(
                            "A ZIP data descriptor has an invalid compressed size.");
                    }
                    entry.Crc32 = crc;
                    entry.CompressedSize = compressedSize;
                    entry.ExpandedSize = expandedSize;
                    ValidateAndAccumulateEntry(entry);
                    if (preserveContent)
                    {
                        return content.ToArray();
                    }
                    return [];
                }
            }
        }
    }

    long MaximumCompressedScanLength()
    {
        if (Limits.MaximumSingleEntryBytes > long.MaxValue - 1048576)
        {
            return long.MaxValue;
        }
        return Limits.MaximumSingleEntryBytes + 1048576;
    }

    static bool CompressionRatioExceeded(
        long expandedSize,
        long compressedSize,
        int maximumRatio)
    {
        if (expandedSize == 0)
        {
            return false;
        }
        if (compressedSize == 0 || maximumRatio == 0)
        {
            return true;
        }
        if (compressedSize > long.MaxValue / maximumRatio)
        {
            return false;
        }
        return expandedSize > compressedSize * (long)maximumRatio;
    }

    void SkipRequiredBytes(long count, string message)
    {
        byte[] buffer = new byte[8192];
        long remaining = count;
        while (remaining > 0)
        {
            CheckCancellation();
            int requested = buffer.Length;
            if (remaining < requested)
            {
                requested = (int)remaining;
            }
            int read = Input.Read(buffer, 0, requested);
            if (read == 0)
            {
                throw FailPackage(message);
            }
            remaining = remaining - read;
            Offset = Offset + read;
        }
    }

    byte[] ReadRequiredBytes(int count, string message)
    {
        byte[] result = ReadOptionalBytes(count);
        if (result.Length != count)
        {
            throw FailPackage(message);
        }
        return result;
    }

    byte[] ReadOptionalBytes(int count)
    {
        if (count == 0)
        {
            return [];
        }
        byte[] result = new byte[count];
        int total = 0;
        while (total < count)
        {
            CheckCancellation();
            int read = Input.Read(result, total, count - total);
            if (read == 0)
            {
                if (total == 0)
                {
                    return [];
                }
                byte[] partial = new byte[total];
                Array.Copy(result, partial, total);
                return partial;
            }
            total = total + read;
            Offset = Offset + read;
        }
        return result;
    }

    void DrainRemainingInput()
    {
        byte[] buffer = new byte[8192];
        int count = Input.Read(buffer, 0, buffer.Length);
        while (count != 0)
        {
            CheckCancellation();
            Offset = Offset + count;
            count = Input.Read(buffer, 0, buffer.Length);
        }
    }

    static StreamingZip64Sizes ReadZip64Sizes(
        byte[] extra,
        bool needExpanded,
        bool needCompressed)
    {
        StreamingZip64Sizes result = new StreamingZip64Sizes();
        int offset = 0;
        while (offset + 4 <= extra.Length)
        {
            int identifier = ReadUInt16Value(extra, offset);
            int length = ReadUInt16Value(extra, offset + 2);
            offset = offset + 4;
            if (length > extra.Length - offset)
            {
                return result;
            }
            if (identifier == Zip64ExtraIdentifier)
            {
                int valueOffset = offset;
                if (needExpanded)
                {
                    if (valueOffset + 8 > offset + length) return result;
                    result.ExpandedSize = ReadInt64Value(extra, valueOffset);
                    valueOffset = valueOffset + 8;
                }
                if (needCompressed)
                {
                    if (valueOffset + 8 > offset + length) return result;
                    result.CompressedSize = ReadInt64Value(extra, valueOffset);
                }
                result.IsValid = result.ExpandedSize >= 0
                    && result.CompressedSize >= 0;
                return result;
            }
            offset = offset + length;
        }
        return result;
    }

    static string DecodeEntryName(byte[] data, int flags)
    {
        if ((flags & Utf8NameFlag) != 0)
        {
            return Encoding.UTF8.GetString(data);
        }
        return Encoding.ASCII.GetString(data);
    }

    static int ReadUInt16Value(byte[] data, int offset)
    {
        return data[offset] | data[offset + 1] << 8;
    }

    static uint ReadUInt32Value(byte[] data, int offset)
    {
        return (uint)data[offset]
            | (uint)data[offset + 1] << 8
            | (uint)data[offset + 2] << 16
            | (uint)data[offset + 3] << 24;
    }

    static long ReadInt64Value(byte[] data, int offset)
    {
        ulong value = (ulong)data[offset]
            | (ulong)data[offset + 1] << 8
            | (ulong)data[offset + 2] << 16
            | (ulong)data[offset + 3] << 24
            | (ulong)data[offset + 4] << 32
            | (ulong)data[offset + 5] << 40
            | (ulong)data[offset + 6] << 48
            | (ulong)data[offset + 7] << 56;
        if (value > long.MaxValue)
        {
            return -1;
        }
        return (long)value;
    }

    static uint ComputeCrc32(byte[] data)
    {
        uint crc = uint.MaxValue;
        int index = 0;
        while (index < data.Length)
        {
            crc = crc ^ data[index];
            int bit = 0;
            while (bit < 8)
            {
                if ((crc & 1) != 0)
                {
                    crc = 3988292384U ^ (crc >> 1);
                }
                else
                {
                    crc = crc >> 1;
                }
                bit = bit + 1;
            }
            index = index + 1;
        }
        return ~crc;
    }

    OdfStreamEvent PackageBoundaryEvent(
        OdfStreamEventKind kind,
        OdfPackageEntry entry,
        bool skipped,
        byte[] content,
        long byteOffset)
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = kind;
        result.PartPath = entry.CanonicalPath.Length > 0
            ? entry.CanonicalPath
            : entry.Path;
        result.PackageEntry = entry;
        result.BinaryContent = content;
        result.WasSkipped = skipped;
        result.Location.PartPath = result.PartPath;
        result.Location.ByteOffset = byteOffset;
        return result;
    }

    OdfStreamEvent FinishInput()
    {
        Finished = true;
        CloseInputIfOwned();
        return EndOfStreamEvent();
    }

    static OdfStreamEvent EndOfStreamEvent()
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = OdfStreamEventKind.EndOfStream;
        return result;
    }

    void CheckCancellation()
    {
        if (Options.Cancellation.IsCancellationRequested)
        {
            CloseInputIfOwned();
            string reason = Options.Cancellation.Reason;
            if (reason == null || reason.Length == 0)
            {
                throw new OdfPackageException(
                    "Package streaming was cancelled.");
            }
            throw new OdfPackageException(
                "Package streaming was cancelled: " + reason);
        }
    }

    OdfPackageException FailPackage(string message)
    {
        CloseInputIfOwned();
        return new OdfPackageException(message);
    }

    void CloseInputIfOwned()
    {
        if (!InputClosed && !Options.LeaveInputOpen)
        {
            Input.Dispose();
            InputClosed = true;
        }
    }
}

internal class StreamingZip64Sizes
{
    internal long ExpandedSize;
    internal long CompressedSize;
    internal bool IsValid;
}
