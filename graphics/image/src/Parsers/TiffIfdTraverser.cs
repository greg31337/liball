using System.Collections.Generic;

namespace image;

internal class TiffIfdTraverser
{
    internal const int ClassicCountSize = 2;
    internal const int ClassicEntrySize = 12;
    internal const int ClassicNextOffsetSize = 4;
    internal const int BigTiffCountSize = 8;
    internal const int BigTiffEntrySize = 20;
    internal const int BigTiffNextOffsetSize = 8;
    internal const int TagSubIfds = 330;
    internal const int TagExifIfdPointer = 34665;
    internal const int TagGpsIfdPointer = 34853;
    internal const int TagInteropIfdPointer = 40965;

    internal static void Traverse(ImageMetadataDocument document, BoundedStreamReader reader, bool littleEndian, bool bigTiff, ulong firstIfdOffset, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || firstIfdOffset == 0)
        {
            return;
        }

        List<ulong> visitedOffsets = new List<ulong>();
        ulong currentOffset = firstIfdOffset;
        int depth = 0;

        while (currentOffset != 0)
        {
            if (visitedOffsets.Count >= GetMaxIfdCount(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tiff.ifd.count.limit", "TIFF IFD count exceeded the configured limit.", "TIFF", TiffHeaderParser.ClampOffset(currentOffset), visitedOffsets.Count);
                return;
            }

            if (depth >= GetMaxNestingDepth(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tiff.ifd.depth.limit", "TIFF IFD traversal exceeded the configured nesting depth.", "TIFF", TiffHeaderParser.ClampOffset(currentOffset), depth);
                return;
            }

            if (HasVisited(visitedOffsets, currentOffset))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tiff.ifd.loop", "TIFF IFD chain referenced an already visited offset.", "TIFF", TiffHeaderParser.ClampOffset(currentOffset), 0);
                return;
            }

            visitedOffsets.Add(currentOffset);
            string group = "IFD" + depth.ToString();
            string name = "Image File Directory " + depth.ToString();
            if (depth == 1)
            {
                name = "Thumbnail Image File Directory";
            }

            ulong nextOffset = ReadIfd(document, reader, littleEndian, bigTiff, currentOffset, depth, options, visitedOffsets, group, name, MetadataFamily.Tiff);
            currentOffset = nextOffset;
            depth = depth + 1;
        }
    }

    internal static ulong ReadIfd(ImageMetadataDocument document, BoundedStreamReader reader, bool littleEndian, bool bigTiff, ulong ifdOffset, int index, ImageMetadataReaderOptions options, List<ulong> visitedOffsets, string group, string name, MetadataFamily family)
    {
        int countSize = CountSize(bigTiff);
        int entrySize = EntrySize(bigTiff);
        int nextOffsetSize = NextOffsetSize(bigTiff);

        if (!CanReadRange(document, ifdOffset, countSize, "tiff.ifd.count.truncated"))
        {
            return 0;
        }

        if (!reader.SeekAbsolute(TiffHeaderParser.ClampOffset(ifdOffset)))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tiff.ifd.seek", "TIFF IFD offset could not be reached.", "TIFF", TiffHeaderParser.ClampOffset(ifdOffset), countSize);
            return 0;
        }

        EndianBinaryReader endian = new EndianBinaryReader(reader, littleEndian);
        ulong entryCount = ReadEntryCount(endian, bigTiff);

        if (!endian.LastReadSucceeded)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tiff.ifd.count.truncated", "TIFF IFD entry count was truncated.", "TIFF", TiffHeaderParser.ClampOffset(ifdOffset), countSize);
            return 0;
        }

        int maximumTagCount = GetMaxTagCount(options);
        if (maximumTagCount < 0 || entryCount > (ulong)maximumTagCount)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tiff.ifd.tag.count.limit", "TIFF IFD entry count exceeded the supported tag count.", "TIFF", TiffHeaderParser.ClampOffset(ifdOffset), TiffHeaderParser.ClampOffset(entryCount));
            return 0;
        }

        ulong entryBytes = entryCount * (ulong)entrySize;
        if (entryBytes > 9223372036854775807 || ifdOffset > ulong.MaxValue - (ulong)countSize || ifdOffset + (ulong)countSize > ulong.MaxValue - entryBytes)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tiff.ifd.size.overflow", "TIFF IFD byte size overflowed.", "TIFF", TiffHeaderParser.ClampOffset(ifdOffset), TiffHeaderParser.ClampOffset(entryBytes));
            return 0;
        }

        ulong nextOffsetPosition = ifdOffset + (ulong)countSize + entryBytes;

        if (!CanReadRange(document, nextOffsetPosition, nextOffsetSize, "tiff.ifd.next.offset.truncated"))
        {
            return 0;
        }

        List<TiffIfdEntry> entries = ReadEntries(document, reader, endian, littleEndian, bigTiff, entryCount, ifdOffset, countSize, entrySize, nextOffsetSize);
        if (entries.Count != TiffHeaderParser.ClampOffset(entryCount))
        {
            return 0;
        }

        if (!reader.SeekAbsolute(TiffHeaderParser.ClampOffset(nextOffsetPosition)))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tiff.ifd.next.offset.seek", "TIFF next IFD offset field could not be reached.", "TIFF", TiffHeaderParser.ClampOffset(nextOffsetPosition), nextOffsetSize);
            return 0;
        }

        ulong nextOffset = ReadNextOffset(endian, bigTiff);
        if (!endian.LastReadSucceeded)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tiff.ifd.next.offset.truncated", "TIFF next IFD offset was truncated.", "TIFF", TiffHeaderParser.ClampOffset(nextOffsetPosition), nextOffsetSize);
            return 0;
        }

        MetadataDirectory directory = AddIfdDirectory(document, ifdOffset, index, entryCount, nextOffset, countSize, entrySize, nextOffsetSize, (ulong)countSize + entryBytes + (ulong)nextOffsetSize, group, name, family);
        List<MetadataTag> decodedTags = DecodeEntries(document, reader, directory, entries, littleEndian, nextOffsetSize);
        TiffGeoKeyExtractor.Extract(document, directory, decodedTags, littleEndian);
        TiffMakerNoteExtractor.Extract(document, directory, decodedTags, littleEndian, options);
        TiffThumbnailExtractor.Extract(document, reader, directory, decodedTags, options);
        RouteReferencedIfds(document, reader, littleEndian, bigTiff, options, visitedOffsets, entries, decodedTags, index + 1);
        return nextOffset;
    }

    internal static List<TiffIfdEntry> ReadEntries(ImageMetadataDocument document, BoundedStreamReader reader, EndianBinaryReader endian, bool littleEndian, bool bigTiff, ulong entryCount, ulong ifdOffset, int countSize, int entrySize, int valueOffsetSize)
    {
        List<TiffIfdEntry> entries = new List<TiffIfdEntry>();
        int index = 0;

        while ((ulong)index < entryCount)
        {
            long entryOffset = reader.Position;
            TiffIfdEntry entry = new TiffIfdEntry();
            entry.EntryOffset = entryOffset;
            entry.TagId = endian.ReadUnsigned16();
            bool tagSucceeded = endian.LastReadSucceeded;
            entry.TypeId = endian.ReadUnsigned16();
            bool typeSucceeded = endian.LastReadSucceeded;
            entry.Count = ReadValueCount(endian, bigTiff);
            bool countSucceeded = endian.LastReadSucceeded;
            entry.ValueFieldOffset = reader.Position;
            entry.ValueField = reader.ReadBytes(valueOffsetSize);

            if (!tagSucceeded || !typeSucceeded || !countSucceeded || entry.ValueField.Length != valueOffsetSize)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "tiff.ifd.entry.truncated", "TIFF IFD entry was truncated.", "TIFF", entryOffset, entrySize);
                return entries;
            }

            entry.ValueOrOffset = TiffValueDecoder.ReadUnsigned64(PadValueField(entry.ValueField), 0, littleEndian);
            if (!bigTiff)
            {
                entry.ValueOrOffset = TiffValueDecoder.ReadUnsigned32(entry.ValueField, 0, littleEndian);
            }

            entries.Add(entry);
            index = index + 1;
        }

        return entries;
    }

    internal static List<MetadataTag> DecodeEntries(ImageMetadataDocument document, BoundedStreamReader reader, MetadataDirectory directory, List<TiffIfdEntry> entries, bool littleEndian, int valueOffsetSize)
    {
        List<MetadataTag> decodedTags = new List<MetadataTag>();
        if (entries == null)
        {
            return decodedTags;
        }

        foreach (TiffIfdEntry entry in entries)
        {
            MetadataTag? tag = TiffValueDecoder.DecodeEntry(document, reader, directory, entry, littleEndian, valueOffsetSize);
            if (tag != null)
            {
                decodedTags.Add(tag);
            }
        }

        return decodedTags;
    }

    internal static void RouteReferencedIfds(ImageMetadataDocument document, BoundedStreamReader reader, bool littleEndian, bool bigTiff, ImageMetadataReaderOptions options, List<ulong> visitedOffsets, List<TiffIfdEntry> entries, List<MetadataTag> decodedTags, int depth)
    {
        if (entries == null || decodedTags == null)
        {
            return;
        }

        int index = 0;
        while (index < entries.Count && index < decodedTags.Count)
        {
            TiffIfdEntry entry = entries[index];
            MetadataTag tag = decodedTags[index];
            if (entry != null && tag != null)
            {
                RouteEntry(document, reader, littleEndian, bigTiff, options, visitedOffsets, entry, tag, depth);
            }

            index = index + 1;
        }
    }

    internal static void RouteEntry(ImageMetadataDocument document, BoundedStreamReader reader, bool littleEndian, bool bigTiff, ImageMetadataReaderOptions options, List<ulong> visitedOffsets, TiffIfdEntry entry, MetadataTag tag, int depth)
    {
        if (entry.TagId == TagExifIfdPointer)
        {
            ulong offset = ReadReferenceOffset(entry, tag.RawValue, 0, littleEndian);
            ReadRoutedIfd(document, reader, littleEndian, bigTiff, offset, options, visitedOffsets, depth, MetadataFamily.Exif, "ExifIFD", "EXIF IFD");
            return;
        }

        if (entry.TagId == TagGpsIfdPointer)
        {
            ulong offset = ReadReferenceOffset(entry, tag.RawValue, 0, littleEndian);
            ReadRoutedIfd(document, reader, littleEndian, bigTiff, offset, options, visitedOffsets, depth, MetadataFamily.Gps, "GPS", "GPS IFD");
            return;
        }

        if (entry.TagId == TagInteropIfdPointer)
        {
            ulong offset = ReadReferenceOffset(entry, tag.RawValue, 0, littleEndian);
            ReadRoutedIfd(document, reader, littleEndian, bigTiff, offset, options, visitedOffsets, depth, MetadataFamily.Exif, "Interoperability", "Interoperability IFD");
            return;
        }

        if (entry.TagId == TagSubIfds)
        {
            int offsetIndex = 0;
            while ((ulong)offsetIndex < entry.Count)
            {
                ulong offset = ReadReferenceOffset(entry, tag.RawValue, offsetIndex, littleEndian);
                ReadRoutedIfd(document, reader, littleEndian, bigTiff, offset, options, visitedOffsets, depth, MetadataFamily.Tiff, "SubIFD" + offsetIndex.ToString(), "SubIFD " + offsetIndex.ToString());
                offsetIndex = offsetIndex + 1;
            }
        }
    }

    internal static void ReadRoutedIfd(ImageMetadataDocument document, BoundedStreamReader reader, bool littleEndian, bool bigTiff, ulong ifdOffset, ImageMetadataReaderOptions options, List<ulong> visitedOffsets, int depth, MetadataFamily family, string group, string name)
    {
        if (ifdOffset == 0)
        {
            return;
        }

        if (visitedOffsets.Count >= GetMaxIfdCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tiff.ifd.count.limit", "TIFF IFD count exceeded the configured limit.", "TIFF", TiffHeaderParser.ClampOffset(ifdOffset), visitedOffsets.Count);
            return;
        }

        if (depth >= GetMaxNestingDepth(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tiff.ifd.depth.limit", "TIFF IFD traversal exceeded the configured nesting depth.", "TIFF", TiffHeaderParser.ClampOffset(ifdOffset), depth);
            return;
        }

        if (HasVisited(visitedOffsets, ifdOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tiff.ifd.loop", "TIFF IFD chain referenced an already visited offset.", "TIFF", TiffHeaderParser.ClampOffset(ifdOffset), 0);
            return;
        }

        if (ifdOffset >= (ulong)document.ByteLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tiff.ifd.reference.outside", "TIFF referenced IFD offset exceeded the stream length.", "TIFF", TiffHeaderParser.ClampOffset(ifdOffset), 0);
            return;
        }

        visitedOffsets.Add(ifdOffset);
        ReadIfd(document, reader, littleEndian, bigTiff, ifdOffset, depth, options, visitedOffsets, group, name, family);
    }

    internal static ulong ReadReferenceOffset(TiffIfdEntry entry, byte[] rawValue, int index, bool littleEndian)
    {
        if (entry == null || rawValue == null || index < 0)
        {
            return 0;
        }

        if (entry.TypeId == TiffValueDecoder.TypeShort)
        {
            int offset = index * 2;
            if (offset + 2 > rawValue.Length)
            {
                return 0;
            }

            return TiffValueDecoder.ReadUnsigned16(rawValue, offset, littleEndian);
        }

        if (entry.TypeId == TiffValueDecoder.TypeLong || entry.TypeId == TiffValueDecoder.TypeIfd)
        {
            int offset = index * 4;
            if (offset + 4 > rawValue.Length)
            {
                return 0;
            }

            return TiffValueDecoder.ReadUnsigned32(rawValue, offset, littleEndian);
        }

        if (entry.TypeId == TiffValueDecoder.TypeLong8 || entry.TypeId == TiffValueDecoder.TypeIfd8)
        {
            int offset = index * 8;
            if (offset + 8 > rawValue.Length)
            {
                return 0;
            }

            return TiffValueDecoder.ReadUnsigned64(rawValue, offset, littleEndian);
        }

        return 0;
    }

    internal static MetadataDirectory AddIfdDirectory(ImageMetadataDocument document, ulong ifdOffset, int index, ulong entryCount, ulong nextOffset, int countSize, int entrySize, int nextOffsetSize, ulong length, string group, string name, MetadataFamily family)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(family, group, name);
        directory.Path = "TIFF/" + group;
        directory.Offset = TiffHeaderParser.ClampOffset(ifdOffset);
        directory.Length = TiffHeaderParser.ClampOffset(length);
        directory.RepeatIndex = index;
        string prefix = TiffValueDecoder.PrefixForFamily(family);
        directory.AddTagWithValue(prefix + "." + group + ".EntryCount", "Entry Count", MetadataValue.FromUnsignedInteger(entryCount));
        directory.AddTagWithValue(prefix + "." + group + ".NextIfdOffset", "Next IFD Offset", MetadataValue.FromUnsignedInteger(nextOffset));
        directory.AddTagWithValue(prefix + "." + group + ".CountSize", "Count Size", MetadataValue.FromInteger(countSize));
        directory.AddTagWithValue(prefix + "." + group + ".EntrySize", "Entry Size", MetadataValue.FromInteger(entrySize));
        directory.AddTagWithValue(prefix + "." + group + ".NextOffsetSize", "Next Offset Size", MetadataValue.FromInteger(nextOffsetSize));
        return directory;
    }

    internal static ulong ReadEntryCount(EndianBinaryReader endian, bool bigTiff)
    {
        if (bigTiff)
        {
            return endian.ReadUnsigned64();
        }

        return endian.ReadUnsigned16();
    }

    internal static ulong ReadValueCount(EndianBinaryReader endian, bool bigTiff)
    {
        if (bigTiff)
        {
            return endian.ReadUnsigned64();
        }

        return endian.ReadUnsigned32();
    }

    internal static ulong ReadNextOffset(EndianBinaryReader endian, bool bigTiff)
    {
        if (bigTiff)
        {
            return endian.ReadUnsigned64();
        }

        return endian.ReadUnsigned32();
    }

    internal static byte[] PadValueField(byte[] bytes)
    {
        byte[] result = new byte[8];
        if (bytes == null)
        {
            return result;
        }

        int index = 0;
        while (index < bytes.Length && index < result.Length)
        {
            result[index] = bytes[index];
            index = index + 1;
        }

        return result;
    }

    internal static bool CanReadRange(ImageMetadataDocument document, ulong offset, int length, string code)
    {
        if (document == null)
        {
            return false;
        }

        if (offset > (ulong)document.ByteLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, code, "TIFF IFD offset exceeded the stream length.", "TIFF", TiffHeaderParser.ClampOffset(offset), length);
            return false;
        }

        if ((ulong)length > (ulong)document.ByteLength - offset)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, code, "TIFF IFD range exceeded the stream length.", "TIFF", TiffHeaderParser.ClampOffset(offset), length);
            return false;
        }

        return true;
    }

    internal static bool HasVisited(List<ulong> offsets, ulong offset)
    {
        if (offsets == null)
        {
            return false;
        }

        foreach (ulong visited in offsets)
        {
            if (visited == offset)
            {
                return true;
            }
        }

        return false;
    }

    internal static int CountSize(bool bigTiff)
    {
        if (bigTiff)
        {
            return BigTiffCountSize;
        }

        return ClassicCountSize;
    }

    internal static int EntrySize(bool bigTiff)
    {
        if (bigTiff)
        {
            return BigTiffEntrySize;
        }

        return ClassicEntrySize;
    }

    internal static int NextOffsetSize(bool bigTiff)
    {
        if (bigTiff)
        {
            return BigTiffNextOffsetSize;
        }

        return ClassicNextOffsetSize;
    }

    internal static int GetMaxIfdCount(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxIfdCount;
        }

        return options.MaxIfdCount;
    }

    internal static int GetMaxNestingDepth(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxNestingDepth;
        }

        return options.MaxNestingDepth;
    }

    internal static int GetMaxTagCount(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxTagCount;
        }

        return options.MaxTagCount;
    }
}
