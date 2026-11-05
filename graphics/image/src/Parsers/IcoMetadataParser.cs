using System.Collections.Generic;
using System.IO;

namespace image;

internal class IcoMetadataParser
{
    internal const int HeaderSize = 6;
    internal const int EntrySize = 16;
    internal const int IconType = 1;
    internal const int CursorType = 2;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);

        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "ico.reader.null", "ICO/CUR parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ico.header.seek", "ICO/CUR header could not be reached.", "Header", 0, HeaderSize);
            Finish(document, reader);
            return document;
        }

        if (reader.Remaining() < HeaderSize)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ico.header.truncated", "ICO/CUR header was truncated.", "Header", 0, HeaderSize);
            Finish(document, reader);
            return document;
        }

        byte[] header = reader.ReadBytes(HeaderSize);
        ushort reserved = BmpHeaderParser.ReadUnsigned16(header, 0);
        ushort type = BmpHeaderParser.ReadUnsigned16(header, 2);
        ushort count = BmpHeaderParser.ReadUnsigned16(header, 4);
        if (reserved != 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ico.reserved.invalid", "ICO/CUR reserved header field was not zero.", "Header", 0, 2);
        }

        if (type != IconType && type != CursorType)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ico.type.invalid", "ICO/CUR resource type was not icon or cursor.", "Header", 2, 2);
            Finish(document, reader);
            return document;
        }

        document.Format = ImageFormatMapping.CreateInfo(type == CursorType ? ImageFormatKind.Cur : ImageFormatKind.Ico);
        AddHeaderDirectory(document, reserved, type, count);
        AddRawBlock(document, options, "Header", "ICO/CUR Header", 0, header);

        if (count == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ico.count.zero", "ICO/CUR image count was zero.", "Header", 4, 2);
            Finish(document, reader);
            return document;
        }

        long directoryLength = HeaderSize + (long)count * EntrySize;
        if (directoryLength > document.ByteLength || reader.Remaining() < (long)count * EntrySize)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ico.directory.truncated", "ICO/CUR directory entries were truncated.", "Directory", HeaderSize, (long)count * EntrySize);
            Finish(document, reader);
            return document;
        }

        List<IcoDirectoryEntry> entries = ReadEntries(document, reader, type, count, options);
        foreach (IcoDirectoryEntry entry in entries)
        {
            ParseItem(document, reader, entry, type, directoryLength, options);
        }

        Finish(document, reader);
        return document;
    }

    internal static List<IcoDirectoryEntry> ReadEntries(ImageMetadataDocument document, BoundedStreamReader reader, int type, int count, ImageMetadataReaderOptions options)
    {
        List<IcoDirectoryEntry> entries = new List<IcoDirectoryEntry>();
        int maximum = MaxFrameCount(options);
        if (count > maximum)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "ico.frame.limit", "ICO/CUR image count exceeded the configured frame limit.", "Directory", HeaderSize, (long)count * EntrySize);
        }

        int index = 0;
        while (index < count)
        {
            byte[] data = reader.ReadBytes(EntrySize);
            if (index < maximum)
            {
                IcoDirectoryEntry entry = DecodeEntry(data, index);
                entries.Add(entry);
                ValidateEntry(document, entry, type);
            }

            index = index + 1;
        }

        return entries;
    }

    internal static IcoDirectoryEntry DecodeEntry(byte[] data, int index)
    {
        IcoDirectoryEntry entry = new IcoDirectoryEntry();
        entry.Index = index;
        entry.Width = data[0] == 0 ? 256 : data[0];
        entry.Height = data[1] == 0 ? 256 : data[1];
        entry.ColorCount = data[2];
        entry.Reserved = data[3];
        entry.PlanesOrHotspotX = BmpHeaderParser.ReadUnsigned16(data, 4);
        entry.BitCountOrHotspotY = BmpHeaderParser.ReadUnsigned16(data, 6);
        entry.DataSize = BmpHeaderParser.ReadUnsigned32(data, 8);
        entry.DataOffset = BmpHeaderParser.ReadUnsigned32(data, 12);
        return entry;
    }

    internal static void ValidateEntry(ImageMetadataDocument document, IcoDirectoryEntry entry, int type)
    {
        long offset = HeaderSize + (long)entry.Index * EntrySize;
        if (entry.Reserved != 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "ico.entry.reserved.nonzero", "ICO/CUR directory entry reserved field was not zero.", "DirectoryEntry", offset + 3, 1);
        }

        if (entry.DataSize == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ico.item.size.zero", "ICO/CUR item byte length was zero.", "DirectoryEntry", offset + 8, 4);
        }

        if (type == CursorType && (entry.PlanesOrHotspotX >= entry.Width || entry.BitCountOrHotspotY >= entry.Height))
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "cur.hotspot.outside", "CUR hotspot was outside the image bounds.", "DirectoryEntry", offset + 4, 4);
        }
    }

    internal static void ParseItem(ImageMetadataDocument document, BoundedStreamReader reader, IcoDirectoryEntry entry, int type, long directoryLength, ImageMetadataReaderOptions options)
    {
        ImageMetadataFrame frame = AddItemFrame(document, entry, type);
        MetadataDirectory entryDirectory = AddEntryDirectory(frame, entry, type);
        if (entry.DataSize == 0)
        {
            return;
        }

        ulong end = (ulong)entry.DataOffset + (ulong)entry.DataSize;
        if (end < entry.DataOffset || end > (ulong)document.ByteLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ico.item.outside", "ICO/CUR item range exceeded the stream length.", "Item", entry.DataOffset, entry.DataSize);
            return;
        }

        if ((ulong)entry.DataOffset < (ulong)directoryLength)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ico.item.inside-directory", "ICO/CUR item data started inside the directory.", "Item", entry.DataOffset, entry.DataSize);
            return;
        }

        if ((long)entry.DataSize > MaxSegmentBytes(options) || entry.DataSize > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "ico.item.limit", "ICO/CUR item exceeded the configured segment limit.", "Item", entry.DataOffset, entry.DataSize);
            return;
        }

        if (!reader.SeekAbsolute(entry.DataOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ico.item.seek", "ICO/CUR item could not be reached.", "Item", entry.DataOffset, entry.DataSize);
            return;
        }

        byte[] data = reader.ReadBytes((int)entry.DataSize);
        AddRawBlock(document, options, "Item", "ICO/CUR Image Item", entry.DataOffset, data);
        if (HasPngSignature(data))
        {
            AddEncodingTag(entryDirectory, entry, "PNG");
            ParsePngItem(document, frame, entry, data, options);
        }
        else
        {
            AddEncodingTag(entryDirectory, entry, "DIB");
            ParseDibItem(document, frame, entry, data, options);
        }
    }

    internal static ImageMetadataFrame AddItemFrame(ImageMetadataDocument document, IcoDirectoryEntry entry, int type)
    {
        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Role = entry.Index == 0 ? ImageMetadataRole.Primary : ImageMetadataRole.Page;
        frame.Width = entry.Width;
        frame.Height = entry.Height;
        frame.CanvasWidth = entry.Width;
        frame.CanvasHeight = entry.Height;
        frame.BitDepth = type == IconType ? entry.BitCountOrHotspotY : 0;
        frame.Orientation = ImageOrientation.Normal;
        return document.AddFrame(frame);
    }

    internal static MetadataDirectory AddEntryDirectory(ImageMetadataFrame frame, IcoDirectoryEntry entry, int type)
    {
        MetadataDirectory directory = frame.AddDirectoryWithName(MetadataFamily.Ico, "DirectoryEntry", type == CursorType ? "Cursor Directory Entry" : "Icon Directory Entry");
        directory.Path = "ICO/Item/" + entry.Index.ToString() + "/DirectoryEntry";
        directory.Offset = HeaderSize + (long)entry.Index * EntrySize;
        directory.Length = EntrySize;
        directory.RepeatIndex = entry.Index;
        AddTag(directory, "Ico.Entry.Index", "Item Index", MetadataValue.FromInteger(entry.Index), directory.Offset, EntrySize, "");
        AddTag(directory, "Ico.Entry.Width", "Width", MetadataValue.FromInteger(entry.Width), directory.Offset, 1, "");
        AddTag(directory, "Ico.Entry.Height", "Height", MetadataValue.FromInteger(entry.Height), directory.Offset + 1, 1, "");
        AddTag(directory, "Ico.Entry.ColorCount", "Color Count", MetadataValue.FromInteger(entry.ColorCount), directory.Offset + 2, 1, "");
        AddTag(directory, "Ico.Entry.Reserved", "Reserved", MetadataValue.FromInteger(entry.Reserved), directory.Offset + 3, 1, "");
        if (type == CursorType)
        {
            AddTag(directory, "Cur.Entry.HotspotX", "Hotspot X", MetadataValue.FromInteger(entry.PlanesOrHotspotX), directory.Offset + 4, 2, "");
            AddTag(directory, "Cur.Entry.HotspotY", "Hotspot Y", MetadataValue.FromInteger(entry.BitCountOrHotspotY), directory.Offset + 6, 2, "");
        }
        else
        {
            AddTag(directory, "Ico.Entry.Planes", "Color Planes", MetadataValue.FromInteger(entry.PlanesOrHotspotX), directory.Offset + 4, 2, "");
            AddTag(directory, "Ico.Entry.BitCount", "Bits Per Pixel", MetadataValue.FromInteger(entry.BitCountOrHotspotY), directory.Offset + 6, 2, "");
        }

        AddTag(directory, "Ico.Entry.DataSize", "Item Byte Length", MetadataValue.FromUnsignedInteger(entry.DataSize), directory.Offset + 8, 4, "");
        AddTag(directory, "Ico.Entry.DataOffset", "Item Data Offset", MetadataValue.FromUnsignedInteger(entry.DataOffset), directory.Offset + 12, 4, "");
        return directory;
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, ushort reserved, ushort type, ushort count)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Ico, "Header", type == CursorType ? "CUR Header" : "ICO Header");
        directory.Path = "ICO/Header";
        directory.Offset = 0;
        directory.Length = HeaderSize;
        AddTag(directory, "Ico.Header.Reserved", "Reserved", MetadataValue.FromInteger(reserved), 0, 2, "");
        AddTag(directory, "Ico.Header.Type", "Resource Type", MetadataValue.FromInteger(type), 2, 2, type == CursorType ? "Cursor" : "Icon");
        AddTag(directory, "Ico.Header.Count", "Image Count", MetadataValue.FromInteger(count), 4, 2, "");
    }

    internal static void AddEncodingTag(MetadataDirectory directory, IcoDirectoryEntry entry, string encoding)
    {
        AddTag(directory, "Ico.Entry.Encoding", "Image Encoding", MetadataValue.FromString(encoding), entry.DataOffset, 4, encoding);
    }

    internal static void ParsePngItem(ImageMetadataDocument document, ImageMetadataFrame frame, IcoDirectoryEntry entry, byte[] data, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(data);
        BoundedStreamReader embeddedReader = new BoundedStreamReader(stream, data.Length, data.Length, diagnostics);
        ImageMetadataDocument embedded = PngMetadataParser.Parse(embeddedReader, ImageFormatMapping.CreateInfo(ImageFormatKind.Png), options);
        if (embedded.Frames.Count > 0)
        {
            CopyFrameFields(frame, embedded.Frames[0]);
            if (frame.Width != entry.Width || frame.Height != entry.Height)
            {
                MetadataDiagnostic.AddWarningAt(document.Diagnostics, "ico.png.dimensions.mismatch", "Embedded PNG dimensions differed from the ICO/CUR directory entry.", "PNG", entry.DataOffset, entry.DataSize);
            }

            frame.Width = entry.Width;
            frame.Height = entry.Height;
            frame.CanvasWidth = entry.Width;
            frame.CanvasHeight = entry.Height;
        }

        MergeEmbeddedDocument(document, frame, embedded, entry.DataOffset, "PNG");
    }

    internal static void ParseDibItem(ImageMetadataDocument document, ImageMetadataFrame frame, IcoDirectoryEntry entry, byte[] data, ImageMetadataReaderOptions options)
    {
        if (data.Length < 4)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ico.dib-size.truncated", "Embedded ICO/CUR DIB header size was truncated.", "DIBHeader", entry.DataOffset, 4);
            return;
        }

        uint headerSize = BmpHeaderParser.ReadUnsigned32(data, 0);
        string headerType = BmpHeaderParser.HeaderType(headerSize);
        if (headerType.Length == 0)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "ico.dib-size.unsupported", "Embedded ICO/CUR DIB header size was not supported.", "DIBHeader");
            return;
        }

        if (headerSize > data.Length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "ico.dib-header.truncated", "Embedded ICO/CUR DIB header was truncated.", "DIBHeader", entry.DataOffset, headerSize);
            return;
        }

        BmpDibHeader header = BmpHeaderParser.DecodeDibHeader(BmpHeaderParser.CopyBytes(data, 0, (int)headerSize), headerType);
        byte[] synthetic = CreateSyntheticBmp(data, headerSize);
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(synthetic);
        BoundedStreamReader embeddedReader = new BoundedStreamReader(stream, synthetic.Length, synthetic.Length, diagnostics);
        ImageMetadataDocument embedded = BmpMetadataParser.Parse(embeddedReader, ImageFormatMapping.CreateInfo(ImageFormatKind.Bmp), options);
        if (embedded.Frames.Count > 0)
        {
            CopyFrameFields(frame, embedded.Frames[0]);
            if (header.BitCount == 32)
            {
                frame.HasAlpha = true;
                frame.ChannelCount = 4;
            }

            frame.Width = entry.Width;
            frame.Height = entry.Height;
            frame.CanvasWidth = entry.Width;
            frame.CanvasHeight = entry.Height;
        }

        int dibHeight = BmpMetadataExtractor.AbsoluteDimension(header.Height);
        int imageHeight = dibHeight / 2;
        if (dibHeight == 0 || (dibHeight & 1) != 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "ico.dib-height.invalid", "Embedded ICO/CUR DIB height did not contain equal XOR and AND mask heights.", "DIBHeader", entry.DataOffset + 8, 4);
        }
        else if (header.Width != entry.Width || imageHeight != entry.Height)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "ico.dib.dimensions.mismatch", "Embedded DIB dimensions differed from the ICO/CUR directory entry.", "DIBHeader", entry.DataOffset + 4, 8);
        }

        MergeEmbeddedDocument(document, frame, embedded, (long)entry.DataOffset - BmpHeaderParser.FileHeaderSize, "DIB");
    }

    internal static byte[] CreateSyntheticBmp(byte[] dib, uint headerSize)
    {
        byte[] bytes = new byte[BmpHeaderParser.FileHeaderSize + dib.Length];
        bytes[0] = 66;
        bytes[1] = 77;
        WriteUnsigned32(bytes, 2, (uint)bytes.Length);
        uint dibPixelOffset = CalculateDibPixelOffset(dib, headerSize);
        uint pixelOffset = (uint)bytes.Length;
        if (dibPixelOffset <= uint.MaxValue - BmpHeaderParser.FileHeaderSize)
        {
            pixelOffset = BmpHeaderParser.FileHeaderSize + dibPixelOffset;
        }

        if (pixelOffset > bytes.Length)
        {
            pixelOffset = (uint)bytes.Length;
        }

        WriteUnsigned32(bytes, 10, pixelOffset);
        BmpHeaderParser.CopyRange(dib, 0, bytes, BmpHeaderParser.FileHeaderSize, dib.Length);
        return bytes;
    }

    internal static uint CalculateDibPixelOffset(byte[] dib, uint headerSize)
    {
        uint offset = headerSize;
        if (headerSize == 40 && dib.Length >= 20)
        {
            uint compression = BmpHeaderParser.ReadUnsigned32(dib, 16);
            if (compression == BmpMetadataExtractor.BiBitfields)
            {
                offset = offset + 12;
            }
            else if (compression == BmpMetadataExtractor.BiAlphaBitfields)
            {
                offset = offset + 16;
            }
        }

        if (dib.Length >= 12)
        {
            ushort bitCount = headerSize == 12 ? BmpHeaderParser.ReadUnsigned16(dib, 10) : BmpHeaderParser.ReadUnsigned16(dib, 14);
            uint colors = 0;
            if (headerSize >= 40 && dib.Length >= 36)
            {
                colors = BmpHeaderParser.ReadUnsigned32(dib, 32);
            }

            if (colors == 0 && bitCount > 0 && bitCount <= 8)
            {
                colors = (uint)1 << bitCount;
            }

            uint entrySize = headerSize == 12 ? 3u : 4u;
            ulong paletteBytes = (ulong)colors * entrySize;
            if (paletteBytes <= 4294967295 - offset)
            {
                offset = offset + (uint)paletteBytes;
            }
        }

        return offset;
    }

    internal static void MergeEmbeddedDocument(ImageMetadataDocument document, ImageMetadataFrame frame, ImageMetadataDocument embedded, long shift, string encoding)
    {
        foreach (MetadataDirectory directory in embedded.Directories)
        {
            if (encoding != "DIB" || directory.Group != "FileHeader")
            {
                ShiftDirectory(directory, shift, frame.Index, "ICO/Item/" + frame.Index.ToString() + "/" + encoding);
                frame.AddDirectory(directory);
            }
        }

        if (embedded.Frames.Count > 0)
        {
            foreach (MetadataDirectory directory in embedded.Frames[0].Directories)
            {
                ShiftDirectory(directory, shift, frame.Index, "ICO/Item/" + frame.Index.ToString() + "/" + encoding);
                frame.AddDirectory(directory);
            }
        }

        foreach (MetadataProfile profile in embedded.Profiles)
        {
            if (profile.Offset >= 0)
            {
                profile.Offset = profile.Offset + shift;
            }

            document.AddProfile(profile);
        }

        foreach (MetadataRawBlock block in embedded.RawBlocks)
        {
            if (encoding != "DIB" || block.BlockType != "FileHeader")
            {
                if (block.Offset >= 0)
                {
                    block.Offset = block.Offset + shift;
                }

                document.AddRawBlock(block);
            }
        }

        foreach (MetadataDiagnostic diagnostic in embedded.Diagnostics)
        {
            if (diagnostic.Offset >= 0)
            {
                diagnostic.Offset = diagnostic.Offset + shift;
            }

            document.AddDiagnostic(diagnostic);
        }
    }

    internal static void ShiftDirectory(MetadataDirectory directory, long shift, int frameIndex, string prefix)
    {
        if (directory.Offset >= 0)
        {
            directory.Offset = directory.Offset + shift;
        }

        directory.FrameIndex = frameIndex;
        directory.Path = prefix + "/" + directory.Path;
        foreach (MetadataTag tag in directory.Tags)
        {
            if (tag.Offset >= 0)
            {
                tag.Offset = tag.Offset + shift;
            }
        }

        foreach (MetadataDirectory child in directory.Children)
        {
            ShiftDirectory(child, shift, frameIndex, prefix);
        }
    }

    internal static void CopyFrameFields(ImageMetadataFrame target, ImageMetadataFrame source)
    {
        target.Width = source.Width;
        target.Height = source.Height;
        target.CanvasWidth = source.CanvasWidth;
        target.CanvasHeight = source.CanvasHeight;
        target.BitDepth = source.BitDepth;
        target.ChannelCount = source.ChannelCount;
        target.HasAlpha = source.HasAlpha;
        target.IsAnimated = source.IsAnimated;
        target.DurationMilliseconds = source.DurationMilliseconds;
        target.Compression = source.Compression;
        target.ColorSpace = source.ColorSpace;
        target.Orientation = source.Orientation;
    }

    internal static bool HasPngSignature(byte[] data)
    {
        if (data == null || data.Length < PngChunkTraverser.Signature.Length)
        {
            return false;
        }

        int index = 0;
        while (index < PngChunkTraverser.Signature.Length)
        {
            if (data[index] != PngChunkTraverser.Signature[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string display)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = display;
        tag.Confidence = 100;
        return tag;
    }

    internal static void AddRawBlock(ImageMetadataDocument document, ImageMetadataReaderOptions options, string blockType, string name, long offset, byte[] data)
    {
        if (!BmpHeaderParser.CaptureRawBlocks(options))
        {
            return;
        }

        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Ico, blockType, name, offset, data.Length);
        if (BmpHeaderParser.CaptureRawBlockData(options))
        {
            if ((long)data.Length > BmpHeaderParser.MaxRawBlockBytes(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "ico.raw-block.limit", "ICO/CUR block exceeded the configured raw block limit.", blockType, offset, data.Length);
            }
            else
            {
                block.Data = BmpHeaderParser.CopyBytes(data, 0, data.Length);
            }
        }
    }

    internal static void WriteUnsigned32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value & 255);
        data[offset + 1] = (byte)((value >> 8) & 255);
        data[offset + 2] = (byte)((value >> 16) & 255);
        data[offset + 3] = (byte)(value >> 24);
    }

    internal static int MaxFrameCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxFrameCount : options.MaxFrameCount;
    }

    internal static long MaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static void Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        TiffHeaderParser.CopyReaderDiagnostics(document, reader);
        ImageMetadataNormalizerCore.BuildSummary(document);
    }
}
