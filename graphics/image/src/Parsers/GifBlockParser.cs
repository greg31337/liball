using System.Collections.Generic;

namespace image;

internal class GifBlockParser
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);

        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "gif.reader.null", "GIF parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.header.seek", "GIF header could not be reached.", "GIF", 0, 6);
            Finish(document, reader);
            return document;
        }

        string version = ReadHeader(document, reader);
        if (version.Length == 0)
        {
            Finish(document, reader);
            return document;
        }

        if (!ReadLogicalScreen(document, reader))
        {
            Finish(document, reader);
            return document;
        }

        GifParserState state = new GifParserState();
        ReadBlocks(document, reader, options, state);
        GifMetadataExtractor.FinishAnimation(document, state);
        Finish(document, reader);
        return document;
    }

    internal static string ReadHeader(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (!CanRead(document, reader, 6, "gif.header.truncated", "GIF header was truncated.", "Header"))
        {
            return "";
        }

        byte[] bytes = reader.ReadBytes(6);
        string signature = MetadataStringReader.DecodeAsciiRange(bytes, 0, 3, null, "GIF");
        string version = MetadataStringReader.DecodeAsciiRange(bytes, 3, 3, null, "GIF");
        if (signature != "GIF" || (version != "87a" && version != "89a"))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.header.invalid", "GIF header signature or version was invalid.", "Header", 0, 6);
            return "";
        }

        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Gif, "Header", "GIF Header");
        directory.Path = "GIF/Header";
        directory.Offset = 0;
        directory.Length = 6;
        AddTag(directory, "Gif.Header.Signature", "Signature", MetadataValue.FromString(signature), 0, 3, signature);
        AddTag(directory, "Gif.Header.Version", "Version", MetadataValue.FromString(version), 3, 3, version);
        return version;
    }

    internal static bool ReadLogicalScreen(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        long offset = reader.Position;
        if (!CanRead(document, reader, 7, "gif.screen.truncated", "GIF logical screen descriptor was truncated.", "LogicalScreen"))
        {
            return false;
        }

        byte[] bytes = reader.ReadBytes(7);
        int width = ReadUnsigned16(bytes, 0);
        int height = ReadUnsigned16(bytes, 2);
        int packed = bytes[4];
        bool hasGlobalColorTable = (packed & 128) != 0;
        int colorResolution = ((packed >> 4) & 7) + 1;
        bool sorted = (packed & 8) != 0;
        int tableBits = (packed & 7) + 1;
        int tableEntries = 1 << tableBits;
        int backgroundIndex = bytes[5];
        int pixelAspect = bytes[6];

        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Gif, "LogicalScreen", "Logical Screen Descriptor");
        directory.Path = "GIF/LogicalScreen";
        directory.Offset = offset;
        directory.Length = 7;
        AddTag(directory, "Gif.LogicalScreen.Width", "Width", MetadataValue.FromInteger(width), offset, 2, "");
        AddTag(directory, "Gif.LogicalScreen.Height", "Height", MetadataValue.FromInteger(height), offset + 2, 2, "");
        AddTag(directory, "Gif.LogicalScreen.GlobalColorTable", "Global Color Table Present", MetadataValue.FromBoolean(hasGlobalColorTable), offset + 4, 1, "");
        AddTag(directory, "Gif.LogicalScreen.ColorResolution", "Color Resolution", MetadataValue.FromInteger(colorResolution), offset + 4, 1, "");
        AddTag(directory, "Gif.LogicalScreen.SortFlag", "Sort Flag", MetadataValue.FromBoolean(sorted), offset + 4, 1, "");
        AddTag(directory, "Gif.LogicalScreen.GlobalColorTableBits", "Global Color Table Bits", MetadataValue.FromInteger(tableBits), offset + 4, 1, "");
        AddTag(directory, "Gif.LogicalScreen.GlobalColorTableEntries", "Global Color Table Entries", MetadataValue.FromInteger(hasGlobalColorTable ? tableEntries : 0), offset + 4, 1, "");
        AddTag(directory, "Gif.LogicalScreen.BackgroundColorIndex", "Background Color Index", MetadataValue.FromInteger(backgroundIndex), offset + 5, 1, "");
        AddTag(directory, "Gif.LogicalScreen.PixelAspectRatioByte", "Pixel Aspect Ratio Byte", MetadataValue.FromInteger(pixelAspect), offset + 6, 1, "");
        AddTag(directory, "Gif.LogicalScreen.PixelAspectRatio", "Pixel Aspect Ratio", MetadataValue.FromDouble(PixelAspectRatio(pixelAspect)), offset + 6, 1, "");

        if (width == 0 || height == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.screen.dimensions.invalid", "GIF logical screen dimensions must be greater than zero.", "LogicalScreen", offset, 4);
        }

        if (hasGlobalColorTable)
        {
            return ReadColorTable(document, reader, null, "GlobalColorTable", "Global Color Table", tableEntries);
        }

        return true;
    }

    internal static bool ReadColorTable(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataFrame? frame, string group, string name, int entries)
    {
        long offset = reader.Position;
        int byteLength = entries * 3;
        if (!CanRead(document, reader, byteLength, "gif.color-table.truncated", "GIF color table was truncated.", group))
        {
            return false;
        }

        byte[] data = reader.ReadBytes(byteLength);
        MetadataDirectory directory;
        if (frame == null)
        {
            directory = document.AddDirectoryWithName(MetadataFamily.Gif, group, name);
        }
        else
        {
            directory = frame.AddDirectoryWithName(MetadataFamily.Gif, group, name);
        }

        directory.Path = frame == null ? "GIF/" + group : "GIF/Frame/" + frame.Index.ToString() + "/" + group;
        directory.Offset = offset;
        directory.Length = byteLength;
        AddTag(directory, "Gif." + group + ".EntryCount", "Entry Count", MetadataValue.FromInteger(entries), offset, byteLength, "");
        MetadataTag dataTag = AddTag(directory, "Gif." + group + ".Entries", "Color Table Entries", MetadataValue.FromBytes(data), offset, byteLength, "");
        dataTag.RawValue = CopyBytes(data);
        return true;
    }

    internal static void ReadBlocks(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, GifParserState state)
    {
        int blockCount = 0;
        while (reader.Remaining() > 0)
        {
            if (blockCount >= GetMaxBlockCount(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "gif.block.count.limit", "GIF block count exceeded the configured tag count limit.", "GIF", reader.Position, blockCount);
                return;
            }

            long offset = reader.Position;
            int introducer = reader.ReadByte();
            if (introducer == 44)
            {
                if (!ReadImage(document, reader, options, state, offset))
                {
                    return;
                }
            }
            else if (introducer == 33)
            {
                if (!ReadExtension(document, reader, options, state, offset))
                {
                    return;
                }
            }
            else if (introducer == 59)
            {
                AddTrailerDirectory(document, offset);
                if (reader.Remaining() > 0)
                {
                    MetadataDiagnostic.AddWarningAt(document.Diagnostics, "gif.trailing.data", "GIF contained bytes after the trailer.", "Trailer", reader.Position, reader.Remaining());
                }

                return;
            }
            else
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.block.introducer.invalid", "GIF block introducer was not recognized.", "GIF", offset, 1);
                return;
            }

            blockCount = blockCount + 1;
        }

        MetadataDiagnostic.AddTruncated(document.Diagnostics, "gif.trailer.missing", "GIF trailer was missing.", "Trailer", reader.Position, 1);
    }

    internal static bool ReadImage(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, GifParserState state, long offset)
    {
        if (!CanRead(document, reader, 9, "gif.image-descriptor.truncated", "GIF image descriptor was truncated.", "ImageDescriptor"))
        {
            return false;
        }

        byte[] bytes = reader.ReadBytes(9);
        int left = ReadUnsigned16(bytes, 0);
        int top = ReadUnsigned16(bytes, 2);
        int width = ReadUnsigned16(bytes, 4);
        int height = ReadUnsigned16(bytes, 6);
        int packed = bytes[8];
        bool hasLocalColorTable = (packed & 128) != 0;
        bool interlaced = (packed & 64) != 0;
        bool sorted = (packed & 32) != 0;
        int tableBits = (packed & 7) + 1;
        int tableEntries = 1 << tableBits;

        if (document.Frames.Count >= GetMaxFrameCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "gif.frame.limit", "GIF frame count exceeded the configured frame limit.", "ImageDescriptor", offset, 10);
            return false;
        }

        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Width = width;
        frame.Height = height;
        frame.CanvasWidth = LogicalScreenValue(document, "Gif.LogicalScreen.Width");
        frame.CanvasHeight = LogicalScreenValue(document, "Gif.LogicalScreen.Height");
        frame.CanvasX = left;
        frame.CanvasY = top;
        frame.BitDepth = hasLocalColorTable ? tableBits : LogicalScreenValue(document, "Gif.LogicalScreen.GlobalColorTableBits");
        frame.ChannelCount = 1;
        frame.Compression = ImageCompressionKind.Lzw;
        frame.ColorSpace = ImageColorSpaceKind.Indexed;
        frame.SourceColorType = "Indexed-color";
        frame.PixelAspectRatio = ImageMetadataNormalizerCore.DoubleValue(ImageMetadataQueryCore.FindFirst(document, "Gif.LogicalScreen.PixelAspectRatio"));
        frame.IsInterlaced = interlaced;
        document.AddFrame(frame);
        GifMetadataExtractor.ApplyGraphicControl(frame, state);

        MetadataDirectory directory = frame.AddDirectoryWithName(MetadataFamily.Gif, "ImageDescriptor", "Image Descriptor");
        directory.Path = "GIF/Frame/" + frame.Index.ToString() + "/ImageDescriptor";
        directory.Offset = offset;
        directory.Length = 10;
        directory.RepeatIndex = frame.Index;
        AddTag(directory, "Gif.ImageDescriptor.Left", "Left Position", MetadataValue.FromInteger(left), offset + 1, 2, "");
        AddTag(directory, "Gif.ImageDescriptor.Top", "Top Position", MetadataValue.FromInteger(top), offset + 3, 2, "");
        AddTag(directory, "Gif.ImageDescriptor.Width", "Width", MetadataValue.FromInteger(width), offset + 5, 2, "");
        AddTag(directory, "Gif.ImageDescriptor.Height", "Height", MetadataValue.FromInteger(height), offset + 7, 2, "");
        AddTag(directory, "Gif.ImageDescriptor.LocalColorTable", "Local Color Table Present", MetadataValue.FromBoolean(hasLocalColorTable), offset + 9, 1, "");
        AddTag(directory, "Gif.ImageDescriptor.Interlaced", "Interlaced", MetadataValue.FromBoolean(interlaced), offset + 9, 1, "");
        AddTag(directory, "Gif.ImageDescriptor.SortFlag", "Sort Flag", MetadataValue.FromBoolean(sorted), offset + 9, 1, "");
        AddTag(directory, "Gif.ImageDescriptor.LocalColorTableBits", "Local Color Table Bits", MetadataValue.FromInteger(tableBits), offset + 9, 1, "");
        AddTag(directory, "Gif.ImageDescriptor.LocalColorTableEntries", "Local Color Table Entries", MetadataValue.FromInteger(hasLocalColorTable ? tableEntries : 0), offset + 9, 1, "");

        if (width == 0 || height == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.image.dimensions.invalid", "GIF image dimensions must be greater than zero.", "ImageDescriptor", offset + 5, 4);
        }

        if (!FitsLogicalScreen(document, left, top, width, height))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.image.bounds.invalid", "GIF image rectangle exceeded the logical screen.", "ImageDescriptor", offset + 1, 8);
        }

        if (hasLocalColorTable && !ReadColorTable(document, reader, frame, "LocalColorTable", "Local Color Table", tableEntries))
        {
            return false;
        }

        return ReadImageData(document, reader, frame, options);
    }

    internal static bool ReadImageData(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataFrame frame, ImageMetadataReaderOptions options)
    {
        long offset = reader.Position;
        if (!CanRead(document, reader, 1, "gif.image-data.code-size.truncated", "GIF image data LZW code size was truncated.", "ImageData"))
        {
            return false;
        }

        int minimumCodeSize = reader.ReadByte();
        GifSubBlockResult result = ReadSubBlocks(document, reader, options, "ImageData", false);
        MetadataDirectory directory = frame.AddDirectoryWithName(MetadataFamily.Gif, "ImageData", "Table-Based Image Data");
        directory.Path = "GIF/Frame/" + frame.Index.ToString() + "/ImageData";
        directory.Offset = offset;
        directory.Length = reader.Position - offset;
        AddTag(directory, "Gif.ImageData.LzwMinimumCodeSize", "LZW Minimum Code Size", MetadataValue.FromInteger(minimumCodeSize), offset, 1, "");
        AddTag(directory, "Gif.ImageData.SubBlockCount", "Data Sub-block Count", MetadataValue.FromInteger(result.Count), offset + 1, directory.Length - 1, "");
        AddTag(directory, "Gif.ImageData.CompressedByteLength", "Compressed Byte Length", MetadataValue.FromInteger(result.DataLength), offset + 1, directory.Length - 1, "");

        if (minimumCodeSize < 2 || minimumCodeSize > 8)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.image-data.code-size.invalid", "GIF LZW minimum code size must be between 2 and 8.", "ImageData", offset, 1);
        }

        return result.Complete;
    }

    internal static bool ReadExtension(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, GifParserState state, long offset)
    {
        if (!CanRead(document, reader, 1, "gif.extension.label.truncated", "GIF extension label was truncated.", "Extension"))
        {
            return false;
        }

        int label = reader.ReadByte();
        string name = ExtensionName(label);
        int headerLength = 0;
        byte[] headerBytes = new byte[0];
        long headerOffset = reader.Position;
        if (HasExtensionHeader(label))
        {
            if (!CanRead(document, reader, 1, "gif.extension.header-size.truncated", "GIF extension header size was truncated.", name))
            {
                return false;
            }

            headerLength = reader.ReadByte();
            headerOffset = reader.Position;
            if (!CanRead(document, reader, headerLength, "gif.extension.header.truncated", "GIF extension header was truncated.", name))
            {
                return false;
            }

            headerBytes = reader.ReadBytes(headerLength);
            ValidateExtensionHeaderLength(document, label, headerLength, headerOffset - 1);
        }

        GifSubBlockResult result = ReadSubBlocks(document, reader, options, name, true);
        int repeatIndex = ExtensionCount(document, name);
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Gif, name, name);
        directory.Path = "GIF/Extension/" + name + "/" + repeatIndex.ToString();
        directory.Offset = offset;
        directory.Length = reader.Position - offset;
        AddTag(directory, "Gif.Extension.Label", "Extension Label", MetadataValue.FromInteger(label), offset + 1, 1, ExtensionLabelText(label));
        AddTag(directory, "Gif.Extension.HeaderByteLength", "Header Byte Length", MetadataValue.FromInteger(headerLength), headerOffset, headerLength, "");
        AddTag(directory, "Gif.Extension.DataSubBlockCount", "Data Sub-block Count", MetadataValue.FromInteger(result.Count), headerOffset + headerLength, result.EncodedLength, "");
        AddTag(directory, "Gif.Extension.DataByteLength", "Data Byte Length", MetadataValue.FromInteger(result.DataLength), headerOffset + headerLength, result.EncodedLength, "");
        directory.RepeatIndex = repeatIndex;
        GifMetadataExtractor.ExtractExtension(document, directory, label, headerBytes, result, options, state);
        return result.Complete;
    }

    internal static GifSubBlockResult ReadSubBlocks(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, string blockType, bool captureData)
    {
        GifSubBlockResult result = new GifSubBlockResult();
        long start = reader.Position;
        result.Offset = start;
        List<byte> data = new List<byte>();
        List<byte> encoded = new List<byte>();
        bool capturing = captureData;
        bool limitReported = false;
        while (true)
        {
            if (!CanRead(document, reader, 1, "gif.sub-block.size.truncated", "GIF data sub-block size was truncated.", blockType))
            {
                result.EncodedLength = reader.Position - start;
                return result;
            }

            int length = reader.ReadByte();
            if (capturing)
            {
                encoded.Add((byte)length);
            }

            if (length == 0)
            {
                result.Complete = true;
                result.EncodedLength = reader.Position - start;
                SetCapturedData(result, data, encoded, capturing);
                return result;
            }

            if (!CanRead(document, reader, length, "gif.sub-block.data.truncated", "GIF data sub-block payload was truncated.", blockType))
            {
                result.EncodedLength = reader.Position - start;
                return result;
            }

            byte[] payload = new byte[0];
            if (capturing)
            {
                payload = reader.ReadBytes(length);
            }
            else
            {
                reader.SkipBytes(length);
            }

            result.Count = result.Count + 1;
            result.DataLength = result.DataLength + length;
            if (capturing)
            {
                AddBytes(data, payload);
                AddBytes(encoded, payload);
            }

            if (!limitReported && reader.Position - start > GetMaxSegmentBytes(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "gif.sub-block.data.limit", "GIF data sub-block bytes exceeded the configured segment limit.", blockType, start, result.DataLength);
                limitReported = true;
                capturing = false;
                data.Clear();
                encoded.Clear();
            }
        }
    }

    internal static void SetCapturedData(GifSubBlockResult result, List<byte> data, List<byte> encoded, bool captured)
    {
        if (!captured)
        {
            return;
        }

        result.Data = data.ToArray();
        result.EncodedData = encoded.ToArray();
        result.DataAvailable = true;
    }

    internal static void AddBytes(List<byte> target, byte[] source)
    {
        if (target == null || source == null)
        {
            return;
        }

        foreach (byte value in source)
        {
            target.Add(value);
        }
    }

    internal static void ValidateExtensionHeaderLength(ImageMetadataDocument document, int label, int length, long offset)
    {
        int expected = 0;
        if (label == 249)
        {
            expected = 4;
        }
        else if (label == 1)
        {
            expected = 12;
        }
        else if (label == 255)
        {
            expected = 11;
        }

        if (expected > 0 && length != expected)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "gif.extension.header-size.invalid", "GIF extension header size did not match its defined size.", ExtensionName(label), offset, 1);
        }
    }

    internal static bool HasExtensionHeader(int label)
    {
        return label == 249 || label == 1 || label == 255;
    }

    internal static string ExtensionName(int label)
    {
        switch (label)
        {
            case 249:
                return "GraphicControlExtension";
            case 254:
                return "CommentExtension";
            case 1:
                return "PlainTextExtension";
            case 255:
                return "ApplicationExtension";
            default:
                return "Extension";
        }
    }

    internal static string ExtensionLabelText(int label)
    {
        string hex = label.ToString("X2");
        return ExtensionName(label) + " (0x" + hex + ")";
    }

    internal static int ExtensionCount(ImageMetadataDocument document, string group)
    {
        int count = 0;
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Family == MetadataFamily.Gif && directory.Group == group)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static void AddTrailerDirectory(ImageMetadataDocument document, long offset)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Gif, "Trailer", "GIF Trailer");
        directory.Path = "GIF/Trailer";
        directory.Offset = offset;
        directory.Length = 1;
        AddTag(directory, "Gif.Trailer.Present", "Trailer Present", MetadataValue.FromBoolean(true), offset, 1, "");
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string displayValue)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = displayValue;
        return tag;
    }

    internal static bool CanRead(ImageMetadataDocument document, BoundedStreamReader reader, long count, string code, string message, string blockType)
    {
        if (reader != null && reader.CanRead(count))
        {
            return true;
        }

        long offset = reader == null ? -1 : reader.Position;
        MetadataDiagnostic.AddTruncated(document.Diagnostics, code, message, blockType, offset, count);
        return false;
    }

    internal static int ReadUnsigned16(byte[] bytes, int offset)
    {
        if (bytes == null || offset < 0 || offset + 2 > bytes.Length)
        {
            return 0;
        }

        return bytes[offset] | (bytes[offset + 1] << 8);
    }

    internal static double PixelAspectRatio(int value)
    {
        if (value == 0)
        {
            return 0;
        }

        return (value + 15) / 64.0;
    }

    internal static int LogicalScreenValue(ImageMetadataDocument document, string key)
    {
        MetadataTag? tag = ImageMetadataQueryCore.FindFirst(document, key);
        if (tag == null || tag.Value == null || tag.Value.Kind != MetadataValueKind.Integer)
        {
            return 0;
        }

        return (int)tag.Value.IntegerValue;
    }

    internal static bool FitsLogicalScreen(ImageMetadataDocument document, int left, int top, int width, int height)
    {
        int screenWidth = LogicalScreenValue(document, "Gif.LogicalScreen.Width");
        int screenHeight = LogicalScreenValue(document, "Gif.LogicalScreen.Height");
        return left + (long)width <= screenWidth && top + (long)height <= screenHeight;
    }

    internal static byte[] CopyBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            return new byte[0];
        }

        byte[] copy = new byte[bytes.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            copy[index] = bytes[index];
            index = index + 1;
        }

        return copy;
    }

    internal static int GetMaxFrameCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxFrameCount : options.MaxFrameCount;
    }

    internal static int GetMaxBlockCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static long GetMaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static void Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader != null && reader.Source != null && reader.Source.CanSeek)
        {
            reader.SeekAbsolute(0);
        }

        if (reader != null && reader.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
            {
                document.Diagnostics.Add(diagnostic);
            }
        }

        ImageMetadataNormalizerCore.BuildSummary(document);
    }
}

internal class GifSubBlockResult
{
    internal int Count;
    internal long DataLength;
    internal long EncodedLength;
    internal long Offset;
    internal byte[] Data;
    internal byte[] EncodedData;
    internal bool DataAvailable;
    internal bool Complete;

    internal GifSubBlockResult()
    {
        Count = 0;
        DataLength = 0;
        EncodedLength = 0;
        Offset = -1;
        Data = new byte[0];
        EncodedData = new byte[0];
        DataAvailable = false;
        Complete = false;
    }
}

internal class GifParserState
{
    internal bool PendingGraphicControl;
    internal int PendingDelayHundredths;
    internal int PendingDisposalMethod;
    internal bool PendingUserInput;
    internal bool PendingTransparency;
    internal int PendingTransparentColorIndex;
    internal bool LoopExtensionPresent;
    internal int LoopCount;

    internal GifParserState()
    {
        PendingGraphicControl = false;
        PendingDelayHundredths = 0;
        PendingDisposalMethod = 0;
        PendingUserInput = false;
        PendingTransparency = false;
        PendingTransparentColorIndex = 0;
        LoopExtensionPresent = false;
        LoopCount = 0;
    }
}
