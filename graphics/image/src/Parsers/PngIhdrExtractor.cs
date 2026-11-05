using System.Collections.Generic;

namespace image;

internal class PngIhdrExtractor
{
    internal static void Extract(ImageMetadataDocument document, List<PngChunk> chunks, ImageMetadataReaderOptions options)
    {
        if (document == null || chunks == null)
        {
            return;
        }

        PngChunk? header = FindHeader(document, chunks);
        if (header == null)
        {
            if (ShouldReportMissingHeader(document))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.ihdr.missing", "PNG did not contain an IHDR chunk.", "IHDR", 8, 0);
            }

            return;
        }

        if (header.Length < 13 || header.Data == null || header.Data.Length < 13)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.ihdr.truncated", "PNG IHDR payload was too short to contain all header fields.", "IHDR", header.DataOffset, 13);
            return;
        }

        if (header.Length != 13)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.ihdr.length.invalid", "PNG IHDR payload length must be exactly 13 bytes.", "IHDR", header.DataOffset, header.Length);
            return;
        }

        uint width = ReadUnsigned32(header.Data, 0);
        uint height = ReadUnsigned32(header.Data, 4);
        int bitDepth = header.Data[8];
        int colorType = header.Data[9];
        int compressionMethod = header.Data[10];
        int filterMethod = header.Data[11];
        int interlaceMethod = header.Data[12];

        MetadataDirectory directory = FindHeaderDirectory(document, header);
        AddHeaderTags(directory, header, width, height, bitDepth, colorType, compressionMethod, filterMethod, interlaceMethod);
        ValidateFields(document, header, width, height, bitDepth, colorType, compressionMethod, filterMethod, interlaceMethod);
        AddFrame(document, header, options, width, height, bitDepth, colorType, compressionMethod, interlaceMethod);
    }

    internal static bool ShouldReportMissingHeader(ImageMetadataDocument document)
    {
        if (document == null || document.Diagnostics == null)
        {
            return true;
        }

        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            if (diagnostic != null && IsChunkStructureDiagnostic(diagnostic.Code))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsChunkStructureDiagnostic(string code)
    {
        return code == "png.chunk.header.truncated"
            || code == "png.chunk.truncated"
            || code == "png.chunk.type.invalid"
            || code == "png.chunk.length.limit"
            || code == "png.chunk.reserved.invalid";
    }

    internal static PngChunk? FindHeader(ImageMetadataDocument document, List<PngChunk> chunks)
    {
        PngChunk? header = null;

        foreach (PngChunk chunk in chunks)
        {
            if (chunk != null && chunk.Type == "IHDR")
            {
                if (header == null)
                {
                    header = chunk;
                    if (chunk.Index != 0)
                    {
                        MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.ihdr.order.invalid", "PNG IHDR chunk was not the first chunk.", "IHDR", chunk.Offset, 12 + chunk.Length);
                    }
                }
                else
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.ihdr.duplicate", "PNG contained more than one IHDR chunk.", "IHDR", chunk.Offset, 12 + chunk.Length);
                }
            }
        }

        return header;
    }

    internal static MetadataDirectory FindHeaderDirectory(ImageMetadataDocument document, PngChunk header)
    {
        if (document == null || document.Directories == null || header == null)
        {
            return new MetadataDirectory();
        }

        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Family == MetadataFamily.Png && directory.Group == "IHDR" && directory.Offset == header.Offset)
            {
                return directory;
            }
        }

        return PngColorExtractor.FindDirectory(document, header);
    }

    internal static void AddHeaderTags(MetadataDirectory directory, PngChunk header, uint width, uint height, int bitDepth, int colorType, int compressionMethod, int filterMethod, int interlaceMethod)
    {
        if (directory == null || header == null)
        {
            return;
        }

        AddTag(directory, "Png.IHDR.Width", "Width", MetadataValue.FromUnsignedInteger(width), header.DataOffset, 4, "");
        AddTag(directory, "Png.IHDR.Height", "Height", MetadataValue.FromUnsignedInteger(height), header.DataOffset + 4, 4, "");
        AddTag(directory, "Png.IHDR.BitDepth", "Bit Depth", MetadataValue.FromInteger(bitDepth), header.DataOffset + 8, 1, "");
        AddTag(directory, "Png.IHDR.ColorType", "Color Type", MetadataValue.FromInteger(colorType), header.DataOffset + 9, 1, ColorTypeName(colorType));
        AddTag(directory, "Png.IHDR.CompressionMethod", "Compression Method", MetadataValue.FromInteger(compressionMethod), header.DataOffset + 10, 1, CompressionMethodName(compressionMethod));
        AddTag(directory, "Png.IHDR.FilterMethod", "Filter Method", MetadataValue.FromInteger(filterMethod), header.DataOffset + 11, 1, FilterMethodName(filterMethod));
        AddTag(directory, "Png.IHDR.InterlaceMethod", "Interlace Method", MetadataValue.FromInteger(interlaceMethod), header.DataOffset + 12, 1, InterlaceMethodName(interlaceMethod));
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string displayValue)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = displayValue;
        return tag;
    }

    internal static void ValidateFields(ImageMetadataDocument document, PngChunk header, uint width, uint height, int bitDepth, int colorType, int compressionMethod, int filterMethod, int interlaceMethod)
    {
        if (!IsValidDimension(width))
        {
            AddInvalidField(document, "png.ihdr.width.invalid", "PNG IHDR width must be between 1 and 2147483647.", header.DataOffset, 4);
        }

        if (!IsValidDimension(height))
        {
            AddInvalidField(document, "png.ihdr.height.invalid", "PNG IHDR height must be between 1 and 2147483647.", header.DataOffset + 4, 4);
        }

        if (!IsValidColorType(colorType))
        {
            AddInvalidField(document, "png.ihdr.color-type.invalid", "PNG IHDR color type was not defined by the PNG specification.", header.DataOffset + 9, 1);
        }
        else if (!IsValidBitDepth(colorType, bitDepth))
        {
            AddInvalidField(document, "png.ihdr.bit-depth.invalid", "PNG IHDR bit depth was not valid for its color type.", header.DataOffset + 8, 1);
        }

        if (compressionMethod != 0)
        {
            AddInvalidField(document, "png.ihdr.compression.invalid", "PNG IHDR compression method must be zero.", header.DataOffset + 10, 1);
        }

        if (filterMethod != 0)
        {
            AddInvalidField(document, "png.ihdr.filter.invalid", "PNG IHDR filter method must be zero.", header.DataOffset + 11, 1);
        }

        if (interlaceMethod != 0 && interlaceMethod != 1)
        {
            AddInvalidField(document, "png.ihdr.interlace.invalid", "PNG IHDR interlace method must be zero or one.", header.DataOffset + 12, 1);
        }
    }

    internal static void AddInvalidField(ImageMetadataDocument document, string code, string message, long offset, long length)
    {
        MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, code, message, "IHDR", offset, length);
    }

    internal static void AddFrame(ImageMetadataDocument document, PngChunk header, ImageMetadataReaderOptions options, uint width, uint height, int bitDepth, int colorType, int compressionMethod, int interlaceMethod)
    {
        if (!IsValidDimension(width) || !IsValidDimension(height))
        {
            return;
        }

        if (GetMaxFrameCount(options) < 1)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "png.frame.limit", "PNG frame count exceeded the configured frame limit.", "IHDR", header.Offset, 12 + header.Length);
            return;
        }

        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Width = (int)width;
        frame.Height = (int)height;
        frame.CanvasWidth = (int)width;
        frame.CanvasHeight = (int)height;
        frame.BitDepth = bitDepth;
        frame.ChannelCount = ChannelCount(colorType);
        frame.HasAlpha = colorType == 4 || colorType == 6;
        frame.Compression = compressionMethod == 0 ? ImageCompressionKind.Deflate : ImageCompressionKind.Unknown;
        frame.ColorSpace = ColorSpace(colorType);
        frame.SourceColorType = ColorTypeName(colorType);
        frame.IsInterlaced = interlaceMethod == 1;
        document.AddFrame(frame);
    }

    internal static uint ReadUnsigned32(byte[] bytes, int offset)
    {
        if (bytes == null || offset < 0 || offset + 4 > bytes.Length)
        {
            return 0;
        }

        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    internal static bool IsValidDimension(uint value)
    {
        return value > 0 && value <= 2147483647;
    }

    internal static bool IsValidColorType(int colorType)
    {
        return colorType == 0 || colorType == 2 || colorType == 3 || colorType == 4 || colorType == 6;
    }

    internal static bool IsValidBitDepth(int colorType, int bitDepth)
    {
        if (colorType == 0)
        {
            return bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8 || bitDepth == 16;
        }

        if (colorType == 3)
        {
            return bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8;
        }

        if (colorType == 2 || colorType == 4 || colorType == 6)
        {
            return bitDepth == 8 || bitDepth == 16;
        }

        return false;
    }

    internal static int ChannelCount(int colorType)
    {
        switch (colorType)
        {
            case 0:
            case 3:
                return 1;
            case 2:
                return 3;
            case 4:
                return 2;
            case 6:
                return 4;
            default:
                return 0;
        }
    }

    internal static ImageColorSpaceKind ColorSpace(int colorType)
    {
        switch (colorType)
        {
            case 0:
            case 4:
                return ImageColorSpaceKind.Gray;
            case 2:
            case 6:
                return ImageColorSpaceKind.Rgb;
            case 3:
                return ImageColorSpaceKind.Indexed;
            default:
                return ImageColorSpaceKind.Unknown;
        }
    }

    internal static string ColorTypeName(int colorType)
    {
        switch (colorType)
        {
            case 0:
                return "Grayscale";
            case 2:
                return "Truecolor";
            case 3:
                return "Indexed-color";
            case 4:
                return "Grayscale with alpha";
            case 6:
                return "Truecolor with alpha";
            default:
                return "Unknown";
        }
    }

    internal static string CompressionMethodName(int compressionMethod)
    {
        return compressionMethod == 0 ? "Deflate" : "Unknown";
    }

    internal static string FilterMethodName(int filterMethod)
    {
        return filterMethod == 0 ? "Adaptive" : "Unknown";
    }

    internal static string InterlaceMethodName(int interlaceMethod)
    {
        if (interlaceMethod == 0)
        {
            return "None";
        }

        if (interlaceMethod == 1)
        {
            return "Adam7";
        }

        return "Unknown";
    }

    internal static int GetMaxFrameCount(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxFrameCount;
        }

        return options.MaxFrameCount;
    }
}
