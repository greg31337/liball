using System.Collections.Generic;

namespace image;

internal class PsdDocumentParser
{
    internal const int HeaderSize = 26;
    internal const int PsdVersion = 1;
    internal const int PsbVersion = 2;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "psd.reader.null", "Photoshop parser received a null reader.", "PSD", -1, 0);
            return document;
        }

        document.ByteLength = reader.Length;
        PsdHeader? header = ReadHeader(document, reader);
        if (header == null)
        {
            Finish(document, reader);
            return document;
        }

        AddHeaderDirectory(document, header);
        if (!header.SignatureValid || !header.VersionValid)
        {
            Finish(document, reader);
            return document;
        }

        document.Format = ImageFormatMapping.CreateInfo(header.Version == PsbVersion ? ImageFormatKind.Psb : ImageFormatKind.Psd);
        ImageMetadataFrame? frame = AddFrame(document, header, options);
        List<PsdSection> sections = new List<PsdSection>();
        PsdSection? colorMode = ReadLengthSection(document, reader, "ColorModeData", "Color Mode Data", 4, options);
        AddSection(document, sections, colorMode);
        if (colorMode == null || colorMode.Truncated)
        {
            Finish(document, reader);
            return document;
        }
        PsdMetadataExtractor.ExtractColorModeData(document, reader, header, colorMode, options);

        PsdSection? resources = ReadLengthSection(document, reader, "ImageResources", "Image Resources", 4, options);
        AddSection(document, sections, resources);
        if (resources == null || resources.Truncated)
        {
            Finish(document, reader);
            return document;
        }
        PsdMetadataExtractor.ExtractImageResources(document, reader, resources, options);

        int layerLengthSize = header.Version == PsbVersion ? 8 : 4;
        PsdSection? layers = ReadLengthSection(document, reader, "LayerAndMask", "Layer and Mask Information", layerLengthSize, options);
        AddSection(document, sections, layers);
        if (layers == null || layers.Truncated)
        {
            Finish(document, reader);
            return document;
        }
        PsdMetadataExtractor.ExtractLayerSummary(document, reader, header, layers, options);

        PsdSection imageData = ReadImageDataSection(document, reader, options);
        AddSection(document, sections, imageData);
        if (frame != null && imageData != null && imageData.CompressionValid)
        {
            frame.Compression = CompressionKind(imageData.Compression);
        }

        Finish(document, reader);
        return document;
    }

    internal static PsdHeader? ReadHeader(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (!reader.SeekAbsolute(0) || !reader.CanRead(HeaderSize))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.header.truncated", "Photoshop file header was truncated.", "Header", 0, HeaderSize);
            return null;
        }

        PsdHeader header = new PsdHeader();
        header.Signature = MetadataStringReader.DecodeLatin1(reader.ReadBytes(4));
        EndianBinaryReader endian = new EndianBinaryReader(reader, false);
        header.Version = endian.ReadUnsigned16();
        byte[] reserved = reader.ReadBytes(6);
        header.ChannelCount = endian.ReadUnsigned16();
        header.Height = endian.ReadUnsigned32();
        header.Width = endian.ReadUnsigned32();
        header.BitDepth = endian.ReadUnsigned16();
        header.ColorMode = endian.ReadUnsigned16();
        header.SignatureValid = header.Signature == "8BPS";
        header.VersionValid = header.Version == PsdVersion || header.Version == PsbVersion;
        header.ReservedValid = AllZero(reserved);
        header.ChannelCountValid = header.ChannelCount >= 1 && header.ChannelCount <= 56;
        uint maximumDimension = header.Version == PsbVersion ? 300000U : 30000U;
        header.DimensionsValid = header.Width >= 1 && header.Height >= 1 && header.Width <= maximumDimension && header.Height <= maximumDimension;
        header.BitDepthValid = header.BitDepth == 1 || header.BitDepth == 8 || header.BitDepth == 16 || header.BitDepth == 32;
        header.ColorModeValid = IsColorModeValid(header.ColorMode);
        ValidateHeader(document, header);
        return header;
    }

    internal static void ValidateHeader(ImageMetadataDocument document, PsdHeader header)
    {
        if (!header.SignatureValid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "psd.signature.invalid", "Photoshop file signature was invalid.", "Header", 0, 4);
        }

        if (!header.VersionValid)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "psd.version.unsupported", "Photoshop file version was not PSD version 1 or PSB version 2.", "Header");
        }

        if (!header.ReservedValid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "psd.reserved.invalid", "Photoshop reserved header bytes were not zero.", "Header", 6, 6);
        }

        if (!header.ChannelCountValid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "psd.channels.invalid", "Photoshop channel count was outside the supported range of 1 through 56.", "Header", 12, 2);
        }

        if (!header.DimensionsValid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "psd.dimensions.invalid", "Photoshop dimensions were zero or exceeded the format version limit.", "Header", 14, 8);
        }

        if (!header.BitDepthValid)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "psd.depth.unsupported", "Photoshop bit depth was not 1, 8, 16, or 32 bits per channel.", "Header");
        }

        if (!header.ColorModeValid)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "psd.color-mode.unsupported", "Photoshop color mode was not a defined file color mode.", "Header");
        }
    }

    internal static PsdSection? ReadLengthSection(ImageMetadataDocument document, BoundedStreamReader reader, string group, string name, int lengthFieldSize, ImageMetadataReaderOptions options)
    {
        long offset = reader.Position;
        if (!reader.CanRead(lengthFieldSize))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.section.length.truncated", "Photoshop " + name + " section length was truncated.", group, offset, lengthFieldSize);
            return null;
        }

        EndianBinaryReader endian = new EndianBinaryReader(reader, false);
        ulong length = lengthFieldSize == 8 ? endian.ReadUnsigned64() : endian.ReadUnsigned32();
        PsdSection section = new PsdSection();
        section.Group = group;
        section.Name = name;
        section.Offset = offset;
        section.LengthFieldSize = lengthFieldSize;
        section.DataOffset = reader.Position;
        section.DeclaredLength = length;
        long remaining = reader.Remaining();
        section.AvailableLength = length > (ulong)remaining ? remaining : (long)length;
        section.Truncated = length > (ulong)remaining;
        section.Limited = length > ToUnsignedLength(GetMaxSegmentBytes(options));
        if (section.Limited)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "psd.section.length.limit", "Photoshop " + name + " section exceeded the configured segment limit.", group, section.DataOffset, ToLong(length));
        }

        if (section.Truncated)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.section.truncated", "Photoshop " + name + " section exceeded the available file data.", group, section.DataOffset, ToLong(length));
            reader.SeekAbsolute(reader.AccessibleLength());
            return section;
        }

        long next = section.DataOffset + (long)length;
        if (!reader.SeekAbsolute(next))
        {
            section.Truncated = true;
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.section.seek", "Photoshop parser could not reach the end of the " + name + " section.", group, section.DataOffset, ToLong(length));
        }

        return section;
    }

    internal static PsdSection ReadImageDataSection(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        PsdSection section = new PsdSection();
        section.Group = "ImageData";
        section.Name = "Image Data";
        section.Offset = reader.Position;
        section.DataOffset = reader.Position;
        section.DeclaredLength = (ulong)reader.Remaining();
        section.AvailableLength = reader.Remaining();
        section.Limited = section.DeclaredLength > ToUnsignedLength(GetMaxSegmentBytes(options));
        if (section.Limited)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "psd.section.length.limit", "Photoshop Image Data section exceeded the configured segment limit.", section.Group, section.DataOffset, section.AvailableLength);
        }

        if (!reader.CanRead(2))
        {
            section.Truncated = true;
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.image-data.compression.truncated", "Photoshop image data compression field was truncated.", section.Group, section.DataOffset, 2);
            return section;
        }

        EndianBinaryReader endian = new EndianBinaryReader(reader, false);
        section.Compression = endian.ReadUnsigned16();
        section.CompressionValid = section.Compression >= 0 && section.Compression <= 3;
        if (!section.CompressionValid)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "psd.image-data.compression.unsupported", "Photoshop image data used an unsupported compression method.", section.Group);
        }

        return section;
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, PsdHeader header)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Psd, "Header", "Photoshop File Header");
        directory.Path = "PSD/Header";
        directory.Offset = 0;
        directory.Length = HeaderSize;
        AddTag(directory, "Psd.Header.Signature", "Signature", MetadataValue.FromString(header.Signature), 0, 4, header.Signature);
        AddTag(directory, "Psd.Header.Version", "Version", MetadataValue.FromInteger(header.Version), 4, 2, header.Version == PsbVersion ? "PSB" : header.Version == PsdVersion ? "PSD" : "Unknown");
        AddTag(directory, "Psd.Header.ReservedValid", "Reserved Bytes Valid", MetadataValue.FromBoolean(header.ReservedValid), 6, 6, "");
        AddTag(directory, "Psd.Header.ChannelCount", "Channel Count", MetadataValue.FromInteger(header.ChannelCount), 12, 2, "");
        AddTag(directory, "Psd.Header.Height", "Height", MetadataValue.FromUnsignedInteger(header.Height), 14, 4, "");
        AddTag(directory, "Psd.Header.Width", "Width", MetadataValue.FromUnsignedInteger(header.Width), 18, 4, "");
        AddTag(directory, "Psd.Header.BitDepth", "Bit Depth", MetadataValue.FromInteger(header.BitDepth), 22, 2, "");
        AddTag(directory, "Psd.Header.ColorMode", "Color Mode", MetadataValue.FromInteger(header.ColorMode), 24, 2, ColorModeName(header.ColorMode));
    }

    internal static void AddSection(ImageMetadataDocument document, List<PsdSection> sections, PsdSection? section)
    {
        if (document == null || section == null)
        {
            return;
        }

        section.Directory = document.AddDirectoryWithName(MetadataFamily.Psd, section.Group, section.Name);
        section.Directory.Path = "PSD/" + section.Group;
        section.Directory.Offset = section.Offset;
        section.Directory.Length = section.LengthFieldSize + section.AvailableLength;
        string prefix = "Psd." + section.Group;
        if (section.LengthFieldSize > 0)
        {
            AddTag(section.Directory, prefix + ".DeclaredLength", "Declared Length", MetadataValue.FromUnsignedInteger(section.DeclaredLength), section.Offset, section.LengthFieldSize, "");
            AddTag(section.Directory, prefix + ".LengthFieldSize", "Length Field Size", MetadataValue.FromInteger(section.LengthFieldSize), section.Offset, section.LengthFieldSize, "");
        }

        AddTag(section.Directory, prefix + ".DataOffset", "Data Offset", MetadataValue.FromInteger(section.DataOffset), section.Offset, section.LengthFieldSize, "");
        AddTag(section.Directory, prefix + ".AvailableLength", "Available Length", MetadataValue.FromInteger(section.AvailableLength), section.DataOffset, section.AvailableLength, "");
        AddTag(section.Directory, prefix + ".Truncated", "Truncated", MetadataValue.FromBoolean(section.Truncated), section.DataOffset, section.AvailableLength, "");
        AddTag(section.Directory, prefix + ".Limited", "Limited", MetadataValue.FromBoolean(section.Limited), section.DataOffset, section.AvailableLength, "");
        if (section.Group == "ImageData" && section.Compression >= 0)
        {
            AddTag(section.Directory, prefix + ".Compression", "Compression", MetadataValue.FromInteger(section.Compression), section.DataOffset, 2, CompressionName(section.Compression));
        }

        sections.Add(section);
    }

    internal static ImageMetadataFrame? AddFrame(ImageMetadataDocument document, PsdHeader header, ImageMetadataReaderOptions options)
    {
        if (!header.ChannelCountValid || !header.DimensionsValid || !header.BitDepthValid || !header.ColorModeValid)
        {
            return null;
        }

        if (document.Frames.Count >= GetMaxFrameCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "psd.frame.count.limit", "Photoshop frame count exceeded the configured limit.", "Header", 0, HeaderSize);
            return null;
        }

        ImageMetadataFrame frame = document.AddFrameWithDimensions((int)header.Width, (int)header.Height);
        frame.BitDepth = header.BitDepth;
        frame.ChannelCount = header.ChannelCount;
        int baseChannels = BaseChannelCount(header.ColorMode);
        frame.HasAlpha = baseChannels > 0 && header.ChannelCount > baseChannels;
        frame.ColorSpace = ColorSpaceKind(header.ColorMode);
        frame.Orientation = ImageOrientation.Normal;
        return frame;
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string display)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = display;
        return tag;
    }

    internal static bool AllZero(byte[] bytes)
    {
        if (bytes == null || bytes.Length != 6)
        {
            return false;
        }

        foreach (byte value in bytes)
        {
            if (value != 0)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsColorModeValid(int value)
    {
        return value == 0 || value == 1 || value == 2 || value == 3 || value == 4 || value == 7 || value == 8 || value == 9;
    }

    internal static string ColorModeName(int value)
    {
        if (value == 0) return "Bitmap";
        if (value == 1) return "Grayscale";
        if (value == 2) return "Indexed";
        if (value == 3) return "RGB";
        if (value == 4) return "CMYK";
        if (value == 7) return "Multichannel";
        if (value == 8) return "Duotone";
        if (value == 9) return "Lab";
        return "Unknown";
    }

    internal static ImageColorSpaceKind ColorSpaceKind(int value)
    {
        if (value == 0 || value == 1 || value == 8) return ImageColorSpaceKind.Gray;
        if (value == 2) return ImageColorSpaceKind.Indexed;
        if (value == 3) return ImageColorSpaceKind.Rgb;
        if (value == 4) return ImageColorSpaceKind.Cmyk;
        if (value == 9) return ImageColorSpaceKind.Lab;
        return ImageColorSpaceKind.Unknown;
    }

    internal static int BaseChannelCount(int value)
    {
        if (value == 0 || value == 1 || value == 2 || value == 8) return 1;
        if (value == 3 || value == 9) return 3;
        if (value == 4) return 4;
        return 0;
    }

    internal static string CompressionName(int value)
    {
        if (value == 0) return "Raw";
        if (value == 1) return "RLE";
        if (value == 2) return "ZIP";
        if (value == 3) return "ZIP with Prediction";
        return "Unknown";
    }

    internal static ImageCompressionKind CompressionKind(int value)
    {
        if (value == 0) return ImageCompressionKind.None;
        if (value == 1) return ImageCompressionKind.Rle;
        if (value == 2 || value == 3) return ImageCompressionKind.Deflate;
        return ImageCompressionKind.Unknown;
    }

    internal static ulong ToUnsignedLength(long value)
    {
        return value < 0 ? 0 : (ulong)value;
    }

    internal static long ToLong(ulong value)
    {
        return value > 9223372036854775807 ? 9223372036854775807 : (long)value;
    }

    internal static long GetMaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static int GetMaxFrameCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxFrameCount : options.MaxFrameCount;
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
