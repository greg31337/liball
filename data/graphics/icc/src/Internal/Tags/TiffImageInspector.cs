namespace icc;

internal class TiffDirectoryEntry
{
    internal bool Found;
    internal bool Duplicated;
    internal int Type;
    internal uint Count;
    internal uint ValueOffset;
    internal int EntryOffset;

    internal TiffDirectoryEntry()
    {
        Found = false;
        Duplicated = false;
        Type = 0;
        Count = 0;
        ValueOffset = 0;
        EntryOffset = 0;
    }
}

internal static class TiffImageInspector
{
    internal const int ImageWidthTag = 256;
    internal const int ImageHeightTag = 257;
    internal const int BitsPerSampleTag = 258;
    internal const int CompressionTag = 259;
    internal const int PhotometricTag = 262;
    internal const int StripOffsetsTag = 273;
    internal const int SamplesPerPixelTag = 277;
    internal const int RowsPerStripTag = 278;
    internal const int StripByteCountsTag = 279;
    internal const int PlanarConfigurationTag = 284;

    internal static EmbeddedImageInspection InspectTiff(System.ReadOnlyMemory<byte> bytes, bool heightMap, int maximumExpandedBytes)
    {
        EmbeddedImageInspection result = new EmbeddedImageInspection();
        if (bytes.Length < 14)
        {
            result.Error = "Embedded TIFF image data is too short to contain a TIFF directory.";
            return result;
        }

        bool littleEndian;
        if (bytes.Span[0] == 73 && bytes.Span[1] == 73)
        {
            littleEndian = true;
        }
        else if (bytes.Span[0] == 77 && bytes.Span[1] == 77)
        {
            littleEndian = false;
        }
        else
        {
            result.Error = "Embedded TIFF image data has an invalid byte-order marker.";
            return result;
        }

        if (ReadTiffUInt16(bytes, 2, littleEndian) != 42)
        {
            result.Error = "Embedded TIFF image data does not contain the classic TIFF identifier.";
            return result;
        }

        uint encodedIfdOffset = ReadTiffUInt32(bytes, 4, littleEndian);
        if (encodedIfdOffset < 8 || encodedIfdOffset > int.MaxValue || encodedIfdOffset > bytes.Length - 2)
        {
            result.Error = "The embedded TIFF directory offset is outside the image bytes.";
            return result;
        }

        int ifdOffset = (int)encodedIfdOffset;
        int entryCount = ReadTiffUInt16(bytes, ifdOffset, littleEndian);
        long directoryEnd = ifdOffset + 2L + ((long)entryCount * 12L) + 4L;
        if (directoryEnd > bytes.Length)
        {
            result.Error = "The embedded TIFF directory extends beyond the image bytes.";
            return result;
        }

        if (ReadTiffUInt32(bytes, (int)directoryEnd - 4, littleEndian) != 0)
        {
            result.Error = "An embedded surface-map TIFF shall contain exactly one image directory.";
            return result;
        }

        TiffDirectoryEntry widthEntry = FindTiffEntry(bytes, ifdOffset, entryCount, ImageWidthTag, littleEndian);
        TiffDirectoryEntry heightEntry = FindTiffEntry(bytes, ifdOffset, entryCount, ImageHeightTag, littleEndian);
        TiffDirectoryEntry bitsEntry = FindTiffEntry(bytes, ifdOffset, entryCount, BitsPerSampleTag, littleEndian);
        TiffDirectoryEntry compressionEntry = FindTiffEntry(bytes, ifdOffset, entryCount, CompressionTag, littleEndian);
        TiffDirectoryEntry photometricEntry = FindTiffEntry(bytes, ifdOffset, entryCount, PhotometricTag, littleEndian);
        TiffDirectoryEntry offsetsEntry = FindTiffEntry(bytes, ifdOffset, entryCount, StripOffsetsTag, littleEndian);
        TiffDirectoryEntry samplesEntry = FindTiffEntry(bytes, ifdOffset, entryCount, SamplesPerPixelTag, littleEndian);
        TiffDirectoryEntry rowsEntry = FindTiffEntry(bytes, ifdOffset, entryCount, RowsPerStripTag, littleEndian);
        TiffDirectoryEntry countsEntry = FindTiffEntry(bytes, ifdOffset, entryCount, StripByteCountsTag, littleEndian);
        TiffDirectoryEntry planarEntry = FindTiffEntry(bytes, ifdOffset, entryCount, PlanarConfigurationTag, littleEndian);
        if (HasDuplicatedTiffField(widthEntry, heightEntry, bitsEntry, compressionEntry, photometricEntry,
            offsetsEntry, samplesEntry, rowsEntry, countsEntry, planarEntry))
        {
            result.Error = "Embedded TIFF image metadata contains a duplicated required field.";
            return result;
        }

        if (!widthEntry.Found || !heightEntry.Found || !bitsEntry.Found || !photometricEntry.Found
            || !offsetsEntry.Found || !countsEntry.Found)
        {
            result.Error = "Embedded TIFF image metadata is missing dimensions, sample depth, photometric interpretation, or strip locations.";
            return result;
        }

        long widthValue = ReadTiffScalar(bytes, widthEntry, littleEndian);
        long heightValue = ReadTiffScalar(bytes, heightEntry, littleEndian);
        long samplesValue = samplesEntry.Found ? ReadTiffScalar(bytes, samplesEntry, littleEndian) : 1;
        long compressionValue = compressionEntry.Found ? ReadTiffScalar(bytes, compressionEntry, littleEndian) : 1;
        long photometricValue = ReadTiffScalar(bytes, photometricEntry, littleEndian);
        long rowsValue = rowsEntry.Found ? ReadTiffScalar(bytes, rowsEntry, littleEndian) : uint.MaxValue;
        long planarValue = planarEntry.Found ? ReadTiffScalar(bytes, planarEntry, littleEndian) : 1;
        if (widthValue <= 0 || widthValue > int.MaxValue || heightValue <= 0 || heightValue > int.MaxValue
            || samplesValue <= 0 || samplesValue > int.MaxValue || rowsValue <= 0 || planarValue != 1)
        {
            result.Error = "Embedded TIFF dimensions, samples, rows-per-strip, or planar configuration are invalid.";
            return result;
        }

        int width = (int)widthValue;
        int height = (int)heightValue;
        int components = (int)samplesValue;
        if ((heightMap && (components != 1 || (photometricValue != 0 && photometricValue != 1)))
            || (!heightMap && (components != 3 || photometricValue != 2)))
        {
            result.Error = heightMap
                ? "An embedded height TIFF shall contain one grayscale component."
                : "An embedded normal TIFF shall contain three RGB components.";
            return result;
        }

        if (!IsSupportedTiffCompression(compressionValue))
        {
            result.Error = "The embedded TIFF uses an unsupported compression method.";
            return result;
        }

        if (bitsEntry.Count != (uint)components)
        {
            result.Error = "Embedded TIFF BitsPerSample metadata shall contain one value for each component.";
            return result;
        }

        int bitsPerSample = 0;
        int component = 0;
        while (component < components)
        {
            long bits = ReadTiffEntryValue(bytes, bitsEntry, component, littleEndian);
            if (bits < 0 || (component != 0 && bits != bitsPerSample))
            {
                result.Error = "Embedded TIFF components shall use one consistent supported sample depth.";
                return result;
            }

            bitsPerSample = (int)bits;
            component = component + 1;
        }

        if ((heightMap && !PngImageInspector.IsValidGrayBitDepth(bitsPerSample))
            || (!heightMap && bitsPerSample != 8 && bitsPerSample != 16))
        {
            result.Error = "The embedded TIFF sample depth is not supported for this surface-map type.";
            return result;
        }

        long rowBytes = ((((long)width * components * bitsPerSample) + 7L) / 8L);
        long expandedSize = rowBytes * height;
        if (expandedSize > maximumExpandedBytes)
        {
            result.LimitExceeded = true;
            result.Error = $"The embedded TIFF expands to {expandedSize} pixel bytes, exceeding the configured maximum of {maximumExpandedBytes} bytes.";
            return result;
        }

        long rowsPerStrip = rowsValue > height ? height : rowsValue;
        long expectedStrips = (height + rowsPerStrip - 1L) / rowsPerStrip;
        if (offsetsEntry.Count != countsEntry.Count || offsetsEntry.Count != expectedStrips)
        {
            result.Error = "Embedded TIFF strip offsets and byte counts shall match the image dimensions and rows-per-strip value.";
            return result;
        }

        int strip = 0;
        while (strip < expectedStrips)
        {
            long stripOffset = ReadTiffEntryValue(bytes, offsetsEntry, strip, littleEndian);
            long stripBytes = ReadTiffEntryValue(bytes, countsEntry, strip, littleEndian);
            if (stripOffset < 0 || stripBytes <= 0 || stripOffset > bytes.Length || stripBytes > bytes.Length - stripOffset)
            {
                result.Error = "An embedded TIFF strip lies outside the encoded image bytes.";
                return result;
            }

            if (compressionValue == 1)
            {
                long firstRow = strip * rowsPerStrip;
                long rowsInStrip = rowsPerStrip < height - firstRow ? rowsPerStrip : height - firstRow;
                if (stripBytes != rowBytes * rowsInStrip)
                {
                    result.Error = "An uncompressed embedded TIFF strip size does not match its pixel dimensions.";
                    return result;
                }
            }

            strip = strip + 1;
        }

        result.Success = true;
        result.Width = width;
        result.Height = height;
        result.Components = components;
        result.IsCompressed = compressionValue != 1;
        return result;
    }

    internal static bool HasDuplicatedTiffField(TiffDirectoryEntry first, TiffDirectoryEntry second,
        TiffDirectoryEntry third, TiffDirectoryEntry fourth, TiffDirectoryEntry fifth,
        TiffDirectoryEntry sixth, TiffDirectoryEntry seventh, TiffDirectoryEntry eighth,
        TiffDirectoryEntry ninth, TiffDirectoryEntry tenth)
    {
        return first.Duplicated || second.Duplicated || third.Duplicated || fourth.Duplicated || fifth.Duplicated
            || sixth.Duplicated || seventh.Duplicated || eighth.Duplicated || ninth.Duplicated || tenth.Duplicated;
    }

    internal static TiffDirectoryEntry FindTiffEntry(System.ReadOnlyMemory<byte> bytes, int ifdOffset, int entryCount, int tag, bool littleEndian)
    {
        TiffDirectoryEntry found = new TiffDirectoryEntry();
        int index = 0;
        while (index < entryCount)
        {
            int entryOffset = ifdOffset + 2 + (index * 12);
            if (ReadTiffUInt16(bytes, entryOffset, littleEndian) == tag)
            {
                if (found.Found)
                {
                    found.Duplicated = true;
                }
                else
                {
                    found.Found = true;
                    found.Type = ReadTiffUInt16(bytes, entryOffset + 2, littleEndian);
                    found.Count = ReadTiffUInt32(bytes, entryOffset + 4, littleEndian);
                    found.ValueOffset = ReadTiffUInt32(bytes, entryOffset + 8, littleEndian);
                    found.EntryOffset = entryOffset;
                }
            }

            index = index + 1;
        }

        return found;
    }

    internal static long ReadTiffScalar(System.ReadOnlyMemory<byte> bytes, TiffDirectoryEntry entry, bool littleEndian)
    {
        if (entry.Count != 1)
        {
            return -1;
        }

        return ReadTiffEntryValue(bytes, entry, 0, littleEndian);
    }

    internal static long ReadTiffEntryValue(System.ReadOnlyMemory<byte> bytes, TiffDirectoryEntry entry, int index, bool littleEndian)
    {
        int elementSize;
        if (entry.Type == 1)
        {
            elementSize = 1;
        }
        else if (entry.Type == 3)
        {
            elementSize = 2;
        }
        else if (entry.Type == 4)
        {
            elementSize = 4;
        }
        else
        {
            return -1;
        }

        if (index < 0 || (uint)index >= entry.Count)
        {
            return -1;
        }

        long totalSize = (long)entry.Count * elementSize;
        long start = totalSize <= 4 ? entry.EntryOffset + 8L : entry.ValueOffset;
        long valueOffset = start + ((long)index * elementSize);
        if (start < 0 || start > bytes.Length || valueOffset < start || valueOffset > bytes.Length - elementSize)
        {
            return -1;
        }

        int offset = (int)valueOffset;
        if (elementSize == 1)
        {
            return bytes.Span[offset];
        }

        if (elementSize == 2)
        {
            return ReadTiffUInt16(bytes, offset, littleEndian);
        }

        return ReadTiffUInt32(bytes, offset, littleEndian);
    }

    internal static bool IsSupportedTiffCompression(long compression)
    {
        return compression == 1 || compression == 5 || compression == 8 || compression == 32773 || compression == 32946;
    }

    internal static ushort ReadTiffUInt16(System.ReadOnlyMemory<byte> bytes, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return (ushort)(bytes.Span[offset] | (bytes.Span[offset + 1] << 8));
        }

        return (ushort)((bytes.Span[offset] << 8) | bytes.Span[offset + 1]);
    }

    internal static uint ReadTiffUInt32(System.ReadOnlyMemory<byte> bytes, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return bytes.Span[offset] | ((uint)bytes.Span[offset + 1] << 8)
                | ((uint)bytes.Span[offset + 2] << 16) | ((uint)bytes.Span[offset + 3] << 24);
        }

        return ((uint)bytes.Span[offset] << 24) | ((uint)bytes.Span[offset + 1] << 16)
            | ((uint)bytes.Span[offset + 2] << 8) | bytes.Span[offset + 3];
    }
}
