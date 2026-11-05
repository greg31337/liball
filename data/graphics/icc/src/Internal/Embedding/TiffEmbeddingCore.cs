namespace icc;

internal static class TiffEmbeddingCore
{
    internal const int IccProfileTag = 34675;
    internal const int DngVersionTag = 50706;
    internal const int UndefinedType = 7;

    internal static IccEmbeddedFormat DetectFormat(
        System.ReadOnlySpan<byte> carrierBytes)
    {
        if (carrierBytes.Length < 4)
        {
            return IccEmbeddedFormat.Unknown;
        }
        bool littleEndian = carrierBytes[0] == 0x49
            && carrierBytes[1] == 0x49;
        bool bigEndian = carrierBytes[0] == 0x4d
            && carrierBytes[1] == 0x4d;
        if (!littleEndian && !bigEndian)
        {
            return IccEmbeddedFormat.Unknown;
        }
        int version = ReadUInt16(carrierBytes, 2, littleEndian);
        if (version != 42 && version != 43)
        {
            return IccEmbeddedFormat.Unknown;
        }
        bool bigTiff = version == 43;
        if (ContainsDngVersion(carrierBytes, littleEndian, bigTiff))
        {
            return IccEmbeddedFormat.Dng;
        }
        return bigTiff ? IccEmbeddedFormat.BigTiff
            : IccEmbeddedFormat.Tiff;
    }

    internal static bool ContainsDngVersion(
        System.ReadOnlySpan<byte> carrierBytes, bool littleEndian,
        bool bigTiff)
    {
        int headerSize = bigTiff ? 16 : 8;
        int countSize = bigTiff ? 8 : 2;
        int entrySize = bigTiff ? 20 : 12;
        int offsetSize = bigTiff ? 8 : 4;
        int firstOffsetPosition = bigTiff ? 8 : 4;
        if (carrierBytes.Length < headerSize)
        {
            return false;
        }
        if (bigTiff && (ReadUInt16(carrierBytes, 4, littleEndian) != 8
            || ReadUInt16(carrierBytes, 6, littleEndian) != 0))
        {
            return false;
        }

        ulong directoryOffset = ReadUnsigned(carrierBytes,
            firstOffsetPosition, offsetSize, littleEndian);
        System.Collections.Generic.HashSet<ulong> visited =
            new System.Collections.Generic.HashSet<ulong>();
        while (directoryOffset != 0 && visited.Count < 4096)
        {
            if (directoryOffset > int.MaxValue
                || directoryOffset > (ulong)(carrierBytes.Length - countSize)
                || !visited.Add(directoryOffset))
            {
                return false;
            }
            int offset = (int)directoryOffset;
            ulong count = ReadUnsigned(carrierBytes, offset, countSize,
                littleEndian);
            int available = carrierBytes.Length - offset - countSize
                - offsetSize;
            if (available < 0 || count > (ulong)(available / entrySize))
            {
                return false;
            }
            int index = 0;
            while ((ulong)index < count)
            {
                int entryOffset = offset + countSize + (index * entrySize);
                if (ReadUInt16(carrierBytes, entryOffset, littleEndian)
                    == DngVersionTag)
                {
                    return true;
                }
                index = index + 1;
            }
            int nextOffset = offset + countSize + ((int)count * entrySize);
            directoryOffset = ReadUnsigned(carrierBytes, nextOffset,
                offsetSize, littleEndian);
        }
        return false;
    }

    internal static TiffProfileData Analyze(
        System.ReadOnlyMemory<byte> carrierBytes, IccResourceLimits limits)
    {
        TiffProfileData result = new TiffProfileData();
        if (carrierBytes.Length < 8)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The TIFF carrier is too short to contain its header.", 0,
                carrierBytes.Length);
        }
        if (carrierBytes.Span[0] == 0x49 && carrierBytes.Span[1] == 0x49)
        {
            result.IsLittleEndian = true;
        }
        else if (carrierBytes.Span[0] != 0x4d
            || carrierBytes.Span[1] != 0x4d)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The TIFF byte-order marker is invalid.", 0, 2);
        }

        int version = ReadUInt16(carrierBytes.Span, 2,
            result.IsLittleEndian);
        if (version == 43)
        {
            result.IsBigTiff = true;
            if (carrierBytes.Length < 16)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "The BigTIFF header is truncated.", 0,
                    carrierBytes.Length);
            }
            if (ReadUInt16(carrierBytes.Span, 4, result.IsLittleEndian) != 8
                || ReadUInt16(carrierBytes.Span, 6,
                    result.IsLittleEndian) != 0)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "The BigTIFF offset size or reserved header value is invalid.",
                    4, 4);
            }
        }
        else if (version != 42)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The TIFF version identifier is neither classic TIFF nor BigTIFF.",
                2, 2);
        }

        int headerSize = result.IsBigTiff ? 16 : 8;
        int countSize = result.IsBigTiff ? 8 : 2;
        int entrySize = result.IsBigTiff ? 20 : 12;
        int offsetSize = result.IsBigTiff ? 8 : 4;
        int firstOffsetPosition = result.IsBigTiff ? 8 : 4;
        ulong directoryOffset = ReadUnsigned(carrierBytes.Span,
            firstOffsetPosition, offsetSize, result.IsLittleEndian);
        if (directoryOffset == 0)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The TIFF header does not reference an image file directory.",
                firstOffsetPosition, offsetSize);
        }

        System.Collections.Generic.HashSet<ulong> visited =
            new System.Collections.Generic.HashSet<ulong>();
        System.Collections.Generic.List<TiffDirectoryData> directories =
            new System.Collections.Generic.List<TiffDirectoryData>();
        System.Collections.Generic.List<TiffProfileEntry> profiles =
            new System.Collections.Generic.List<TiffProfileEntry>();
        int linkOffset = firstOffsetPosition;
        while (directoryOffset != 0)
        {
            int alignment = result.IsBigTiff ? 8 : 2;
            if (directoryOffset < (ulong)headerSize
                || directoryOffset > int.MaxValue
                || directoryOffset % (ulong)alignment != 0
                || directoryOffset > (ulong)(carrierBytes.Length - countSize))
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "A TIFF IFD offset is outside the carrier or improperly aligned.",
                    linkOffset, offsetSize);
            }
            if (!visited.Add(directoryOffset))
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "The TIFF IFD chain contains a cycle.", linkOffset,
                    offsetSize);
            }
            int currentOffset = (int)directoryOffset;
            ulong encodedCount = ReadUnsigned(carrierBytes.Span,
                currentOffset, countSize, result.IsLittleEndian);
            int available = carrierBytes.Length - currentOffset - countSize
                - offsetSize;
            if (available < 0
                || encodedCount > (ulong)(available / entrySize)
                || encodedCount > int.MaxValue)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "A TIFF IFD entry count extends beyond the carrier.",
                    currentOffset, countSize);
            }
            int entryCount = (int)encodedCount;
            TiffDirectoryData directory = new TiffDirectoryData();
            directory.Offset = currentOffset;
            directory.EntryCount = entryCount;
            directory.EntriesOffset = currentOffset + countSize;
            directory.NextPointerOffset = directory.EntriesOffset
                + (entryCount * entrySize);
            directory.NextOffset = ReadUnsigned(carrierBytes.Span,
                directory.NextPointerOffset, offsetSize,
                result.IsLittleEndian);
            directory.LinkOffset = linkOffset;
            directories.Add(directory);

            bool foundProfile = false;
            int entryIndex = 0;
            while (entryIndex < entryCount)
            {
                int entryOffset = directory.EntriesOffset
                    + (entryIndex * entrySize);
                int tag = ReadUInt16(carrierBytes.Span, entryOffset,
                    result.IsLittleEndian);
                int type = ReadUInt16(carrierBytes.Span, entryOffset + 2,
                    result.IsLittleEndian);
                if (tag == DngVersionTag)
                {
                    result.IsDng = true;
                }
                int typeSize = GetTypeSize(type, result.IsBigTiff);
                if (typeSize == 0)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        $"TIFF tag {tag} uses unsupported field type {type}.",
                        entryOffset + 2, 2);
                }
                ulong valueCount = ReadUnsigned(carrierBytes.Span,
                    entryOffset + 4, result.IsBigTiff ? 8 : 4,
                    result.IsLittleEndian);
                if (valueCount > ulong.MaxValue / (ulong)typeSize)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        $"TIFF tag {tag} has an overflowing value count.",
                        entryOffset + 4, result.IsBigTiff ? 8 : 4);
                }
                ulong byteCount = valueCount * (ulong)typeSize;
                int valueFieldOffset = entryOffset
                    + (result.IsBigTiff ? 12 : 8);
                bool inline = byteCount <= (ulong)offsetSize;
                ulong valueOffset = inline ? (ulong)valueFieldOffset
                    : ReadUnsigned(carrierBytes.Span, valueFieldOffset,
                        offsetSize, result.IsLittleEndian);
                if (byteCount > int.MaxValue || valueOffset > int.MaxValue
                    || valueOffset > (ulong)carrierBytes.Length
                    || byteCount > (ulong)carrierBytes.Length - valueOffset)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        $"TIFF tag {tag} has a value range outside the carrier.",
                        valueFieldOffset, offsetSize);
                }
                if (!inline && valueOffset % (ulong)alignment != 0)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        $"TIFF tag {tag} has an improperly aligned value offset.",
                        valueFieldOffset, offsetSize);
                }

                if (tag == IccProfileTag)
                {
                    if (foundProfile)
                    {
                        return Fail(result, IccOperationStatus.InvalidData,
                            "A TIFF IFD contains duplicate ICC profile tags.",
                            entryOffset, entrySize);
                    }
                    foundProfile = true;
                    if (type != UndefinedType)
                    {
                        return Fail(result, IccOperationStatus.InvalidData,
                            "TIFF tag 34675 must use the UNDEFINED field type.",
                            entryOffset + 2, 2);
                    }
                    if (byteCount > (ulong)limits.MaximumProfileBytes)
                    {
                        return Fail(result,
                            IccOperationStatus.ResourceLimitExceeded,
                            "A TIFF embedded ICC profile exceeds the configured profile-size limit.",
                            valueFieldOffset, offsetSize);
                    }
                    IccOperationResult probe = ProbeProfile(
                        carrierBytes.Slice((int)valueOffset, (int)byteCount),
                        limits);
                    if (probe.Status != IccOperationStatus.Success)
                    {
                        return Fail(result, probe.Status,
                            $"The TIFF tag 34675 profile is invalid: {probe.Message}",
                            (int)valueOffset, (int)byteCount);
                    }
                    TiffProfileEntry profile = new TiffProfileEntry();
                    profile.DirectoryIndex = directories.Count - 1;
                    profile.EntryOffset = entryOffset;
                    profile.ValueFieldOffset = valueFieldOffset;
                    profile.DataOffset = (int)valueOffset;
                    profile.DataLength = (int)byteCount;
                    profile.IsInline = inline;
                    profiles.Add(profile);
                }
                entryIndex = entryIndex + 1;
            }

            linkOffset = directory.NextPointerOffset;
            directoryOffset = directory.NextOffset;
        }

        result.Directories = directories.ToArray();
        result.Profiles = profiles.ToArray();
        result.IsValid = true;
        result.Status = IccOperationStatus.Success;
        string kind = result.IsDng ? "DNG" : result.IsBigTiff
            ? "BigTIFF" : "classic TIFF";
        result.Message = profiles.Count == 0
            ? $"The {kind} IFD chain is valid."
            : $"The {kind} carrier contains {profiles.Count} ICC profile tag(s).";
        return result;
    }

    internal static IccOperationResult ProbeProfile(
        System.ReadOnlyMemory<byte> profileBytes, IccResourceLimits limits)
    {
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.ResourceLimits = limits;
        return new ProfileReaderCore().ProbeMemory(profileBytes, options);
    }

    internal static byte[] InsertProfile(System.ReadOnlySpan<byte> carrierBytes,
        byte[] profileBytes, IccEmbeddedFormat format,
        IccResourceLimits limits)
    {
        TiffProfileData data = Analyze(carrierBytes.ToArray(), limits);
        RequireValidCarrier(data);
        RequireFormat(data, format);
        RequireProfileSize(profileBytes, limits);
        if (data.Profiles.Length != 0)
        {
            throw new IccEmbeddingException(
                "The TIFF carrier already contains an ICC profile tag; use replacement instead.");
        }
        return AppendInsertedProfile(carrierBytes, data, profileBytes,
            limits.MaximumCarrierBytes);
    }

    internal static byte[] ReplaceProfile(
        System.ReadOnlySpan<byte> carrierBytes, int profileIndex,
        byte[] profileBytes, IccEmbeddedFormat format,
        IccResourceLimits limits)
    {
        TiffProfileData data = Analyze(carrierBytes.ToArray(), limits);
        RequireValidCarrier(data);
        RequireFormat(data, format);
        RequireProfileSize(profileBytes, limits);
        TiffProfileEntry profile = RequireProfile(data, profileIndex);
        int alignment = data.IsBigTiff ? 8 : 2;
        int profileOffset = AlignOffset(carrierBytes.Length, alignment);
        long outputLength = (long)profileOffset + profileBytes.Length;
        RequireOutputLength(outputLength, limits.MaximumCarrierBytes,
            data.IsBigTiff ? "BigTIFF" : "TIFF");
        byte[] output = new byte[(int)outputLength];
        carrierBytes.CopyTo(output);
        System.Array.Copy(profileBytes, 0, output, profileOffset,
            profileBytes.Length);
        WriteUnsigned(output, profile.EntryOffset + 4,
            data.IsBigTiff ? 8 : 4, (ulong)profileBytes.Length,
            data.IsLittleEndian);
        WriteUnsigned(output, profile.ValueFieldOffset,
            data.IsBigTiff ? 8 : 4, (ulong)profileOffset,
            data.IsLittleEndian);
        return output;
    }

    internal static byte[] RemoveProfile(System.ReadOnlySpan<byte> carrierBytes,
        int profileIndex, IccEmbeddedFormat format, IccResourceLimits limits)
    {
        TiffProfileData data = Analyze(carrierBytes.ToArray(), limits);
        RequireValidCarrier(data);
        RequireFormat(data, format);
        TiffProfileEntry profile = RequireProfile(data, profileIndex);
        return AppendDirectoryWithoutProfile(carrierBytes, data, profile,
            limits.MaximumCarrierBytes);
    }

    internal static byte[] AppendInsertedProfile(
        System.ReadOnlySpan<byte> carrierBytes, TiffProfileData data,
        byte[] profileBytes, int maximumCarrierBytes)
    {
        TiffDirectoryData directory = data.Directories[0];
        if (!data.IsBigTiff && directory.EntryCount == ushort.MaxValue)
        {
            throw new IccEmbeddingException(
                "The classic TIFF first IFD cannot contain another entry.");
        }
        int alignment = data.IsBigTiff ? 8 : 2;
        int countSize = data.IsBigTiff ? 8 : 2;
        int entrySize = data.IsBigTiff ? 20 : 12;
        int offsetSize = data.IsBigTiff ? 8 : 4;
        int profileOffset = AlignOffset(carrierBytes.Length, alignment);
        int directoryOffset = AlignOffset(profileOffset + profileBytes.Length,
            alignment);
        int newEntryCount = directory.EntryCount + 1;
        long directoryLength = (long)countSize
            + ((long)newEntryCount * entrySize) + offsetSize;
        long outputLength = (long)directoryOffset + directoryLength;
        RequireOutputLength(outputLength, maximumCarrierBytes,
            data.IsBigTiff ? "BigTIFF" : "TIFF");

        byte[] output = new byte[(int)outputLength];
        carrierBytes.CopyTo(output);
        System.Array.Copy(profileBytes, 0, output, profileOffset,
            profileBytes.Length);
        WriteUnsigned(output, directoryOffset, countSize,
            (ulong)newEntryCount, data.IsLittleEndian);
        int sourceIndex = 0;
        int destinationIndex = 0;
        bool inserted = false;
        while (sourceIndex < directory.EntryCount)
        {
            int sourceEntryOffset = directory.EntriesOffset
                + (sourceIndex * entrySize);
            int tag = ReadUInt16(carrierBytes, sourceEntryOffset,
                data.IsLittleEndian);
            if (!inserted && tag > IccProfileTag)
            {
                WriteProfileEntry(output, directoryOffset + countSize
                    + (destinationIndex * entrySize), profileBytes.Length,
                    profileOffset, data);
                destinationIndex = destinationIndex + 1;
                inserted = true;
            }
            carrierBytes.Slice(sourceEntryOffset, entrySize).CopyTo(
                new System.Span<byte>(output, directoryOffset + countSize
                    + (destinationIndex * entrySize), entrySize));
            sourceIndex = sourceIndex + 1;
            destinationIndex = destinationIndex + 1;
        }
        if (!inserted)
        {
            WriteProfileEntry(output, directoryOffset + countSize
                + (destinationIndex * entrySize), profileBytes.Length,
                profileOffset, data);
        }
        int nextPointerOffset = directoryOffset + countSize
            + (newEntryCount * entrySize);
        WriteUnsigned(output, nextPointerOffset, offsetSize,
            directory.NextOffset, data.IsLittleEndian);
        WriteUnsigned(output, directory.LinkOffset, offsetSize,
            (ulong)directoryOffset, data.IsLittleEndian);
        return output;
    }

    internal static byte[] AppendDirectoryWithoutProfile(
        System.ReadOnlySpan<byte> carrierBytes, TiffProfileData data,
        TiffProfileEntry profile, int maximumCarrierBytes)
    {
        TiffDirectoryData directory =
            data.Directories[profile.DirectoryIndex];
        int alignment = data.IsBigTiff ? 8 : 2;
        int countSize = data.IsBigTiff ? 8 : 2;
        int entrySize = data.IsBigTiff ? 20 : 12;
        int offsetSize = data.IsBigTiff ? 8 : 4;
        int directoryOffset = AlignOffset(carrierBytes.Length, alignment);
        int newEntryCount = directory.EntryCount - 1;
        long directoryLength = (long)countSize
            + ((long)newEntryCount * entrySize) + offsetSize;
        long outputLength = (long)directoryOffset + directoryLength;
        RequireOutputLength(outputLength, maximumCarrierBytes,
            data.IsBigTiff ? "BigTIFF" : "TIFF");

        byte[] output = new byte[(int)outputLength];
        carrierBytes.CopyTo(output);
        WriteUnsigned(output, directoryOffset, countSize,
            (ulong)newEntryCount, data.IsLittleEndian);
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < directory.EntryCount)
        {
            int sourceEntryOffset = directory.EntriesOffset
                + (sourceIndex * entrySize);
            if (sourceEntryOffset != profile.EntryOffset)
            {
                carrierBytes.Slice(sourceEntryOffset, entrySize).CopyTo(
                    new System.Span<byte>(output, directoryOffset + countSize
                        + (destinationIndex * entrySize), entrySize));
                destinationIndex = destinationIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
        int nextPointerOffset = directoryOffset + countSize
            + (newEntryCount * entrySize);
        WriteUnsigned(output, nextPointerOffset, offsetSize,
            directory.NextOffset, data.IsLittleEndian);
        WriteUnsigned(output, directory.LinkOffset, offsetSize,
            (ulong)directoryOffset, data.IsLittleEndian);
        return output;
    }

    internal static void WriteProfileEntry(byte[] output, int entryOffset,
        int profileLength, int profileOffset, TiffProfileData data)
    {
        WriteUnsigned(output, entryOffset, 2, IccProfileTag,
            data.IsLittleEndian);
        WriteUnsigned(output, entryOffset + 2, 2, UndefinedType,
            data.IsLittleEndian);
        WriteUnsigned(output, entryOffset + 4, data.IsBigTiff ? 8 : 4,
            (ulong)profileLength, data.IsLittleEndian);
        WriteUnsigned(output, entryOffset + (data.IsBigTiff ? 12 : 8),
            data.IsBigTiff ? 8 : 4, (ulong)profileOffset,
            data.IsLittleEndian);
    }

    internal static int GetTypeSize(int type, bool bigTiff)
    {
        int size = 0;
        switch (type)
        {
            case 1:
            case 2:
            case 6:
            case 7:
                size = 1;
                break;
            case 3:
            case 8:
                size = 2;
                break;
            case 4:
            case 9:
            case 11:
            case 13:
                size = 4;
                break;
            case 5:
            case 10:
            case 12:
                size = 8;
                break;
            case 16:
            case 17:
            case 18:
                size = bigTiff ? 8 : 0;
                break;
        }
        return size;
    }

    internal static TiffProfileEntry RequireProfile(TiffProfileData data,
        int profileIndex)
    {
        if (profileIndex < 0 || profileIndex >= data.Profiles.Length)
        {
            throw new IccEmbeddingException(
                $"The TIFF carrier has no ICC profile at index {profileIndex}.");
        }
        return data.Profiles[profileIndex];
    }

    internal static void RequireValidCarrier(TiffProfileData data)
    {
        if (!data.IsValid)
        {
            throw new IccEmbeddingException(data.Message);
        }
    }

    internal static void RequireFormat(TiffProfileData data,
        IccEmbeddedFormat requestedFormat)
    {
        IccEmbeddedFormat actualFormat = data.IsDng ? IccEmbeddedFormat.Dng
            : data.IsBigTiff ? IccEmbeddedFormat.BigTiff
            : IccEmbeddedFormat.Tiff;
        if (actualFormat != requestedFormat)
        {
            throw new IccEmbeddingException(
                $"The requested {requestedFormat} carrier format does not match the detected {actualFormat} TIFF format.");
        }
    }

    internal static void RequireProfileSize(byte[] profileBytes,
        IccResourceLimits limits)
    {
        if (profileBytes == null || profileBytes.Length
            > limits.MaximumProfileBytes)
        {
            throw new IccEmbeddingException(
                "The ICC profile exceeds the configured TIFF embedding limit.");
        }
    }

    internal static int AlignOffset(int offset, int alignment)
    {
        int remainder = offset % alignment;
        if (remainder == 0)
        {
            return offset;
        }
        long aligned = (long)offset + alignment - remainder;
        if (aligned > int.MaxValue)
        {
            throw new IccEmbeddingException(
                "The aligned TIFF output offset exceeds the managed size limit.");
        }
        return (int)aligned;
    }

    internal static void RequireOutputLength(long outputLength,
        int maximumCarrierBytes, string kind)
    {
        if (outputLength > maximumCarrierBytes
            || outputLength > System.Array.MaxLength)
        {
            throw new IccEmbeddingException(
                $"The updated {kind} carrier exceeds the configured or managed carrier-size limit.");
        }
    }

    internal static TiffProfileData Fail(TiffProfileData result,
        IccOperationStatus status, string message, int offset, int length)
    {
        result.IsValid = false;
        result.Status = status;
        result.Message = message;
        result.ErrorOffset = offset;
        result.ErrorLength = length;
        result.Directories = new TiffDirectoryData[0];
        result.Profiles = new TiffProfileEntry[0];
        return result;
    }

    internal static int ReadUInt16(System.ReadOnlySpan<byte> bytes, int offset,
        bool littleEndian)
    {
        return littleEndian
            ? bytes[offset] | (bytes[offset + 1] << 8)
            : (bytes[offset] << 8) | bytes[offset + 1];
    }

    internal static ulong ReadUnsigned(System.ReadOnlySpan<byte> bytes,
        int offset, int size, bool littleEndian)
    {
        ulong value = 0;
        int index = 0;
        while (index < size)
        {
            int sourceIndex = littleEndian ? size - index - 1 : index;
            value = (value << 8) | bytes[offset + sourceIndex];
            index = index + 1;
        }
        return value;
    }

    internal static void WriteUnsigned(byte[] bytes, int offset, int size,
        ulong value, bool littleEndian)
    {
        int index = 0;
        while (index < size)
        {
            int destinationIndex = littleEndian ? index : size - index - 1;
            bytes[offset + destinationIndex] = (byte)(value & 0xff);
            value = value >> 8;
            index = index + 1;
        }
    }
}
