using System.Collections.Generic;
using System.Text;

namespace image;

internal class CiffParserState
{
    internal bool LittleEndian;
    internal int TagCount;
    internal int DirectoryCount;
    internal List<long> DirectoryOffsets;

    internal CiffParserState(bool littleEndian)
    {
        LittleEndian = littleEndian;
        TagCount = 0;
        DirectoryCount = 0;
        DirectoryOffsets = new List<long>();
    }
}

internal class CiffMetadataParser
{
    internal const int HeaderLength = 14;
    internal const int DirectoryFooterLength = 4;
    internal const int DirectoryCountLength = 2;
    internal const int DirectoryEntryLength = 10;
    internal const int InlineValueLength = 8;
    internal const int TypeMask = 14336;
    internal const int LocationMask = 49152;
    internal const int InlineLocation = 16384;
    internal const int TagIdMask = 16383;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);

        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "ciff.reader.null", "CIFF parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ciff.header.seek", "CIFF header could not be reached.", "CIFF", 0, HeaderLength);
            return Finish(document, reader);
        }

        byte[] header = reader.ReadBytes(HeaderLength);
        if (header.Length < HeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ciff.header.truncated", "CIFF header was truncated.", "CIFF", 0, HeaderLength);
            return Finish(document, reader);
        }

        bool littleEndian = header[0] == 73 && header[1] == 73;
        bool bigEndian = header[0] == 77 && header[1] == 77;
        if (!littleEndian && !bigEndian)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ciff.byte.order.invalid", "CIFF byte order marker was invalid.", "CIFF", 0, 2);
            return Finish(document, reader);
        }

        if (!HasSignature(header))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ciff.signature.invalid", "CIFF HEAPCCDR signature was invalid.", "CIFF", 6, 8);
            return Finish(document, reader);
        }

        long heapOffset = ReadUnsigned32(header, 2, littleEndian);
        AddHeaderDirectory(document, littleEndian, heapOffset);
        AddProfileDirectory(document, heapOffset);
        CaptureRawBlock(document, reader, options, "Header", "CIFF Header", 0, HeaderLength);

        if (heapOffset < HeaderLength)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ciff.heap.offset.invalid", "CIFF heap offset pointed inside the fixed header.", "CIFF", 2, 4);
            return Finish(document, reader);
        }

        if (heapOffset > reader.AccessibleLength())
        {
            AddOutsideRangeDiagnostic(document, reader, "ciff.heap.offset.outside", "CIFF heap offset exceeded the accessible source length.", heapOffset, 0);
            return Finish(document, reader);
        }

        long heapLength = reader.AccessibleLength() - heapOffset;
        CiffParserState state = new CiffParserState(littleEndian);
        MetadataDirectory? root = ParseDirectory(document, reader, options, state, heapOffset, heapLength, 0, "Root", "CIFF Root Directory", "CIFF/Root");
        if (root != null)
        {
            document.AddDirectory(root);
        }

        ImageMetadataNormalizerCore.BuildSummary(document);
        return Finish(document, reader);
    }

    internal static MetadataDirectory? ParseDirectory(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, CiffParserState state, long regionOffset, long regionLength, int depth, string group, string name, string path)
    {
        if (depth >= MaxNestingDepth(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "ciff.directory.depth.limit", "CIFF directory nesting exceeded the configured limit.", "CIFF", regionOffset, regionLength);
            return null;
        }

        if (state.DirectoryCount >= MaxDirectoryCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "ciff.directory.count.limit", "CIFF directory count exceeded the configured limit.", "CIFF", regionOffset, regionLength);
            return null;
        }

        if (regionLength < DirectoryFooterLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ciff.directory.footer.truncated", "CIFF directory footer was truncated.", "CIFF", regionOffset, regionLength);
            return null;
        }

        byte[] footer = ReadRange(reader, regionOffset + regionLength - DirectoryFooterLength, DirectoryFooterLength);
        if (footer.Length < DirectoryFooterLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ciff.directory.footer.truncated", "CIFF directory footer was truncated.", "CIFF", regionOffset + regionLength - DirectoryFooterLength, DirectoryFooterLength);
            return null;
        }

        long tableRelativeOffset = ReadUnsigned32(footer, 0, state.LittleEndian);
        if (tableRelativeOffset > regionLength - DirectoryCountLength)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ciff.directory.offset.outside", "CIFF directory table offset exceeded its containing region.", "CIFF", regionOffset + regionLength - DirectoryFooterLength, DirectoryFooterLength);
            return null;
        }

        long tableOffset = regionOffset + tableRelativeOffset;
        if (HasVisited(state.DirectoryOffsets, tableOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ciff.directory.loop", "CIFF directory table was referenced more than once.", "CIFF", tableOffset, 0);
            return null;
        }

        byte[] countBytes = ReadRange(reader, tableOffset, DirectoryCountLength);
        if (countBytes.Length < DirectoryCountLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ciff.directory.count.truncated", "CIFF directory entry count was truncated.", "CIFF", tableOffset, DirectoryCountLength);
            return null;
        }

        int entryCount = ReadUnsigned16(countBytes, 0, state.LittleEndian);
        long tableLength = DirectoryCountLength + (long)entryCount * DirectoryEntryLength;
        long availableTableLength = regionLength - DirectoryFooterLength - tableRelativeOffset;
        if (availableTableLength < 0 || tableLength > availableTableLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ciff.directory.entries.truncated", "CIFF directory entries exceeded their containing region.", "CIFF", tableOffset, tableLength);
            return null;
        }

        if (state.TagCount + entryCount > MaxTagCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "ciff.directory.tag.count.limit", "CIFF tag count exceeded the configured limit.", "CIFF", tableOffset, entryCount);
            return null;
        }

        state.DirectoryOffsets.Add(tableOffset);
        state.DirectoryCount = state.DirectoryCount + 1;
        MetadataDirectory directory = MetadataDirectory.Create(MetadataFamily.Raw, group, name);
        directory.Path = path;
        directory.Offset = tableOffset;
        directory.Length = regionLength - tableRelativeOffset;
        CaptureRawBlock(document, reader, options, "Directory", name, tableOffset, directory.Length);

        int index = 0;
        while (index < entryCount)
        {
            long recordRelativeOffset = tableRelativeOffset + DirectoryCountLength + (long)index * DirectoryEntryLength;
            long recordOffset = regionOffset + recordRelativeOffset;
            byte[] record = ReadRange(reader, recordOffset, DirectoryEntryLength);
            if (record.Length < DirectoryEntryLength)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "ciff.directory.entry.truncated", "CIFF directory entry was truncated.", "CIFF", recordOffset, DirectoryEntryLength);
                return directory;
            }

            state.TagCount = state.TagCount + 1;
            ParseEntry(document, reader, options, state, directory, record, recordOffset, recordRelativeOffset, regionOffset, regionLength, depth, index);
            index = index + 1;
        }

        return directory;
    }

    internal static void ParseEntry(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, CiffParserState state, MetadataDirectory directory, byte[] record, long recordOffset, long recordRelativeOffset, long regionOffset, long regionLength, int depth, int index)
    {
        int rawTag = ReadUnsigned16(record, 0, state.LittleEndian);
        int tagId = rawTag & TagIdMask;
        int location = rawTag & LocationMask;
        long valueLength;
        long valueRelativeOffset;

        if (location == InlineLocation)
        {
            valueLength = InlineValueLength;
            valueRelativeOffset = recordRelativeOffset + 2;
        }
        else if (location == 0)
        {
            valueLength = ReadUnsigned32(record, 2, state.LittleEndian);
            valueRelativeOffset = ReadUnsigned32(record, 6, state.LittleEndian);
            if (!IsExternalRangeValid(valueRelativeOffset, valueLength, recordRelativeOffset, regionLength))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ciff.entry.range.invalid", "CIFF entry data range was invalid or overlapped its directory record.", "CIFF", recordOffset, DirectoryEntryLength);
                return;
            }
        }
        else
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ciff.entry.location.invalid", "CIFF entry used an unsupported data-location flag.", "CIFF", recordOffset, DirectoryEntryLength);
            return;
        }

        long valueOffset = regionOffset + valueRelativeOffset;
        int type = rawTag & TypeMask;
        string entryName = TagName(tagId);
        if (type == TypeMask)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ciff.entry.type.invalid", "CIFF entry used an invalid value type.", "CIFF", recordOffset, DirectoryEntryLength);
        }
        if (type == 10240 || type == 12288)
        {
            string childGroup = DirectoryGroup(tagId);
            MetadataDirectory? child = ParseDirectory(document, reader, options, state, valueOffset, valueLength, depth + 1, childGroup, entryName + " Directory", directory.Path + "/" + childGroup);
            if (child != null)
            {
                child.RepeatIndex = index;
                directory.AddChild(child);
                MetadataTag directoryTag = AddTag(directory, tagId, entryName, MetadataValue.FromDirectory(child), recordOffset, DirectoryEntryLength, new byte[0]);
                directoryTag.DisplayValue = "Directory with " + child.Tags.Count.ToString() + " entries";
            }
            return;
        }

        byte[] valueBytes = ReadValue(document, reader, options, tagId, valueOffset, valueLength);
        MetadataValue value = DecodeValue(valueBytes, valueLength, type, state.LittleEndian);
        MetadataTag tag = AddTag(directory, tagId, entryName, value, valueOffset, valueLength, valueBytes);
        if (valueBytes.Length == 0 && valueLength > 0)
        {
            tag.DisplayValue = "Binary data " + valueLength.ToString() + " bytes";
        }

        AddKnownMetadata(document, reader, options, state, tagId, valueOffset, valueLength, valueBytes);
    }

    internal static byte[] ReadValue(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, int tagId, long offset, long length)
    {
        if (length == 0)
        {
            return new byte[0];
        }

        if (tagId == 8197)
        {
            return new byte[0];
        }

        long maximum = MaxSegmentBytes(options);
        if (length > maximum || length > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "ciff.value.limit", "CIFF entry value exceeded the configured segment limit.", "CIFF", offset, length);
            return new byte[0];
        }

        byte[] bytes = ReadRange(reader, offset, (int)length);
        if (bytes.Length < length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ciff.value.truncated", "CIFF entry value was truncated.", "CIFF", offset, length);
        }
        return bytes;
    }

    internal static MetadataValue DecodeValue(byte[] bytes, long declaredLength, int type, bool littleEndian)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return MetadataValue.FromBytes(new byte[0]);
        }

        if (type == 2048)
        {
            return MetadataValue.FromString(DecodeAscii(bytes));
        }

        if (type == 4096)
        {
            if (bytes.Length == 2)
            {
                return MetadataValue.FromUnsignedInteger((ulong)ReadUnsigned16(bytes, 0, littleEndian));
            }
            return DecodeUnsignedList(bytes, 2, littleEndian);
        }

        if (type == 6144)
        {
            if (bytes.Length == 4)
            {
                return MetadataValue.FromUnsignedInteger(ReadUnsigned32(bytes, 0, littleEndian));
            }
            return DecodeUnsignedList(bytes, 4, littleEndian);
        }

        if (type == 0 && bytes.Length == 1)
        {
            return MetadataValue.FromUnsignedInteger(bytes[0]);
        }

        return MetadataValue.FromBytes(bytes);
    }

    internal static MetadataValue DecodeUnsignedList(byte[] bytes, int elementSize, bool littleEndian)
    {
        ulong[] values = new ulong[bytes.Length / elementSize];
        int offset = 0;
        int index = 0;
        while (offset + elementSize <= bytes.Length)
        {
            ulong item = elementSize == 2 ? (ulong)ReadUnsigned16(bytes, offset, littleEndian) : ReadUnsigned32(bytes, offset, littleEndian);
            values[index] = item;
            offset = offset + elementSize;
            index = index + 1;
        }
        return MetadataValue.FromUnsignedIntegerArray(values);
    }

    internal static void AddKnownMetadata(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, CiffParserState state, int tagId, long offset, long length, byte[] bytes)
    {
        if (tagId == 2058 && bytes.Length > 0)
        {
            List<string> strings = DecodeAsciiStrings(bytes);
            if (strings.Count > 0)
            {
                AddMappedTag(document, "Tiff.IFD0.Make", "Make", 271, strings[0], offset, bytes.Length);
            }
            if (strings.Count > 1)
            {
                AddMappedTag(document, "Tiff.IFD0.Model", "Model", 272, strings[1], offset + strings[0].Length + 1, bytes.Length - strings[0].Length - 1);
            }
        }

        if (tagId == 6160 && bytes.Length >= 8)
        {
            long width = ReadUnsigned32(bytes, 0, state.LittleEndian);
            long height = ReadUnsigned32(bytes, 4, state.LittleEndian);
            if (width > 0 && height > 0 && width <= 2147483647 && height <= 2147483647)
            {
                ImageMetadataFrame frame = document.AddFrameWithDimensions((int)width, (int)height);
                frame.Index = document.Frames.Count - 1;
            }
        }

        if (tagId == 8199 || tagId == 8200)
        {
            AddJpegProfile(document, reader, options, tagId, offset, length, bytes);
        }
    }

    internal static void AddJpegProfile(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, int tagId, long offset, long length, byte[] bytes)
    {
        string shortName = tagId == 8199 ? "JpgFromRaw" : "ThumbnailImage";
        string name = tagId == 8199 ? "CIFF JPEG Preview" : "CIFF Thumbnail";
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Thumbnail;
        profile.Role = tagId == 8199 ? ImageMetadataRole.Preview : ImageMetadataRole.Thumbnail;
        profile.Family = MetadataFamily.Thumbnail;
        profile.Key = "Thumbnail.CIFF." + shortName;
        profile.Name = name;
        profile.MimeType = "image/jpeg";
        profile.Offset = offset;
        profile.Length = length;
        long maximum = MaxProfileBytes(options);
        if (length <= maximum && length <= 2147483647)
        {
            profile.Data = bytes;
            if (profile.Data.Length != length)
            {
                profile.Data = ReadRange(reader, offset, (int)length);
            }
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "ciff.thumbnail.profile.limit", "CIFF JPEG preview exceeded the configured profile limit.", "CIFF", offset, length);
        }
        document.AddProfile(profile);
        CaptureRawBlock(document, reader, options, shortName, name, offset, length);
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, int tagId, string name, MetadataValue value, long offset, long length, byte[] rawValue)
    {
        string keyName = name;
        if (name.Length == 0)
        {
            keyName = "Tag" + tagId.ToString("X4");
        }
        MetadataTag tag = directory.AddTagWithValue("Raw.CIFF." + directory.Group + "." + keyName, name, value);
        tag.NumericId = tagId;
        tag.Offset = offset;
        tag.Length = length;
        tag.Confidence = 100;
        tag.RawValue = rawValue;
        return tag;
    }

    internal static void AddMappedTag(ImageMetadataDocument document, string key, string name, int numericId, string text, long offset, long length)
    {
        MetadataDirectory directory = FindMappedIfd(document);
        MetadataTag tag = MetadataTag.Create(key, name, MetadataFamily.Tiff, MetadataValue.FromString(text));
        tag.Group = "IFD0";
        tag.NumericId = numericId;
        tag.Offset = offset;
        tag.Length = length;
        tag.Confidence = 100;
        directory.AddTag(tag);
    }

    internal static MetadataDirectory FindMappedIfd(ImageMetadataDocument document)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory.Family == MetadataFamily.Tiff && directory.Group == "IFD0" && directory.Path == "CIFF/Mapped/IFD0")
            {
                return directory;
            }
        }
        MetadataDirectory mapped = document.AddDirectoryWithName(MetadataFamily.Tiff, "IFD0", "CIFF Camera Identification");
        mapped.Path = "CIFF/Mapped/IFD0";
        return mapped;
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, bool littleEndian, long heapOffset)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Raw, "Header", "CIFF Header");
        directory.Path = "CIFF/Header";
        directory.Offset = 0;
        directory.Length = HeaderLength;
        MetadataTag byteOrder = directory.AddTagWithValue("Raw.CIFF.Header.ByteOrder", "Byte Order", MetadataValue.FromString(littleEndian ? "LittleEndian" : "BigEndian"));
        byteOrder.Offset = 0;
        byteOrder.Length = 2;
        MetadataTag offset = directory.AddTagWithValue("Raw.CIFF.Header.HeapOffset", "Heap Offset", MetadataValue.FromInteger(heapOffset));
        offset.Offset = 2;
        offset.Length = 4;
        MetadataTag signature = directory.AddTagWithValue("Raw.CIFF.Header.Signature", "Signature", MetadataValue.FromString("HEAPCCDR"));
        signature.Offset = 6;
        signature.Length = 8;
    }

    internal static void AddProfileDirectory(ImageMetadataDocument document, long heapOffset)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Raw, "Profile", "CRW RAW Profile");
        directory.Path = "Raw/Profile";
        directory.Offset = 0;
        directory.Length = document.ByteLength;
        directory.AddTagWithValue("Raw.Profile.Format", "RAW Format", MetadataValue.FromString("CRW"));
        directory.AddTagWithValue("Raw.Profile.Container", "Container", MetadataValue.FromString("CIFF"));
        directory.AddTagWithValue("Raw.Profile.HeaderVariant", "Header Variant", MetadataValue.FromString("HEAPCCDR"));
        directory.AddTagWithValue("Raw.Profile.HeapOffset", "Heap Offset", MetadataValue.FromInteger(heapOffset));
    }

    internal static void CaptureRawBlock(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, string blockType, string name, long offset, long length)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }
        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Raw, blockType, name, offset, length);
        if (!CaptureRawBlockData(options))
        {
            return;
        }
        long maximum = MaxRawBlockBytes(options);
        if (length > maximum || length > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "ciff.raw.block.limit", "CIFF raw metadata block exceeded the configured raw block limit.", blockType, offset, length);
            return;
        }
        block.Data = ReadRange(reader, offset, (int)length);
    }

    internal static bool IsExternalRangeValid(long valueOffset, long valueLength, long recordOffset, long regionLength)
    {
        if (valueOffset < 0 || valueLength < 0 || valueOffset > regionLength || valueLength > regionLength - valueOffset)
        {
            return false;
        }
        if (valueOffset < recordOffset)
        {
            return valueLength <= recordOffset - valueOffset;
        }
        return valueOffset >= recordOffset + DirectoryEntryLength;
    }

    internal static byte[] ReadRange(BoundedStreamReader reader, long offset, int length)
    {
        if (reader == null || length < 0 || !reader.SeekAbsolute(offset))
        {
            return new byte[0];
        }
        return reader.ReadBytes(length);
    }

    internal static bool HasSignature(byte[] bytes)
    {
        byte[] signature = new byte[] { 72, 69, 65, 80, 67, 67, 68, 82 };
        int index = 0;
        while (index < signature.Length)
        {
            if (bytes[6 + index] != signature[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static int ReadUnsigned16(byte[] bytes, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return bytes[offset] | bytes[offset + 1] << 8;
        }
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    internal static uint ReadUnsigned32(byte[] bytes, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return (uint)bytes[offset] | (uint)bytes[offset + 1] << 8 | (uint)bytes[offset + 2] << 16 | (uint)bytes[offset + 3] << 24;
        }
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    internal static string DecodeAscii(byte[] bytes)
    {
        int length = 0;
        while (length < bytes.Length && bytes[length] != 0)
        {
            length = length + 1;
        }
        return Encoding.ASCII.GetString(bytes, 0, length);
    }

    internal static List<string> DecodeAsciiStrings(byte[] bytes)
    {
        List<string> values = new List<string>();
        int start = 0;
        int index = 0;
        while (index <= bytes.Length)
        {
            if (index == bytes.Length || bytes[index] == 0)
            {
                if (index > start)
                {
                    values.Add(Encoding.ASCII.GetString(bytes, start, index - start));
                }
                start = index + 1;
            }
            index = index + 1;
        }
        return values;
    }

    internal static string TagName(int tagId)
    {
        switch (tagId)
        {
            case 0: return "NullRecord";
            case 1: return "FreeBytes";
            case 50: return "CanonColorInfo1";
            case 2053: return "UserComment";
            case 2058: return "CanonRawMakeModel";
            case 2059: return "CanonFirmwareVersion";
            case 2060: return "ComponentVersion";
            case 2061: return "ROMOperationMode";
            case 2064: return "OwnerName";
            case 2069: return "CanonImageType";
            case 2070: return "OriginalFileName";
            case 2071: return "ThumbnailFileName";
            case 4106: return "TargetImageType";
            case 4112: return "ShutterReleaseMethod";
            case 4113: return "ShutterReleaseTiming";
            case 4118: return "ReleaseSetting";
            case 4124: return "BaseISO";
            case 4136: return "CanonFlashInfo";
            case 4137: return "CanonFocalLength";
            case 4138: return "CanonShotInfo";
            case 4140: return "CanonColorInfo2";
            case 4141: return "CanonCameraSettings";
            case 4144: return "WhiteSample";
            case 4145: return "SensorInfo";
            case 4147: return "CustomFunctions";
            case 4152: return "CanonAFInfo";
            case 4243: return "CanonFileInfo";
            case 4265: return "ColorBalance";
            case 4270: return "ColorTemperature";
            case 4276: return "ColorSpace";
            case 4277: return "RawJpgInfo";
            case 6147: return "ImageFormat";
            case 6148: return "RecordID";
            case 6150: return "SelfTimerTime";
            case 6151: return "TargetDistanceSetting";
            case 6155: return "SerialNumber";
            case 6158: return "TimeStamp";
            case 6160: return "ImageInfo";
            case 6163: return "FlashInfo";
            case 6164: return "MeasuredEV";
            case 6167: return "FileNumber";
            case 6168: return "ExposureInfo";
            case 6196: return "CanonModelID";
            case 6197: return "DecoderTable";
            case 6203: return "SerialNumberFormat";
            case 8197: return "RawData";
            case 8199: return "JpgFromRaw";
            case 8200: return "ThumbnailImage";
            case 10244: return "ImageDescription";
            case 10247: return "CameraObject";
            case 12290: return "ShootingRecord";
            case 12291: return "MeasuredInfo";
            case 12292: return "CameraSpecification";
            case 12298: return "ImageProps";
            case 12299: return "ExifInformation";
            default: return "Tag" + tagId.ToString("X4");
        }
    }

    internal static string DirectoryGroup(int tagId)
    {
        string name = TagName(tagId);
        if (name.Length == 0)
        {
            return "Directory" + tagId.ToString("X4");
        }
        return name;
    }

    internal static bool HasVisited(List<long> offsets, long offset)
    {
        foreach (long value in offsets)
        {
            if (value == offset)
            {
                return true;
            }
        }
        return false;
    }

    internal static int MaxNestingDepth(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxNestingDepth : options.MaxNestingDepth;
    }

    internal static int MaxDirectoryCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxIfdCount : options.MaxIfdCount;
    }

    internal static int MaxTagCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static long MaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static long MaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }

    internal static long MaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxRawBlockBytes : options.MaxRawBlockBytes;
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        return options == null || options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        return options != null && options.CaptureRawBlockData;
    }

    internal static void AddOutsideRangeDiagnostic(ImageMetadataDocument document, BoundedStreamReader reader, string code, string message, long offset, long length)
    {
        if (reader.AccessibleLength() < reader.Length)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, code, message, "CIFF", offset, length);
        }
        else
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, code, message, "CIFF", offset, length);
        }
    }

    internal static ImageMetadataDocument Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader != null && reader.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
            {
                document.Diagnostics.Add(diagnostic);
            }
            reader.SeekAbsolute(0);
        }
        return document;
    }
}
