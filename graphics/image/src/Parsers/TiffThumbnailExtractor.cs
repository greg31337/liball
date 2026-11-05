using System.Collections.Generic;

namespace image;

internal class TiffThumbnailExtractor
{
    internal const int TagImageWidth = 256;
    internal const int TagImageLength = 257;
    internal const int TagStripOffsets = 273;
    internal const int TagStripByteCounts = 279;
    internal const int TagJpegInterchangeFormat = 513;
    internal const int TagJpegInterchangeFormatLength = 514;

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, MetadataDirectory directory, List<MetadataTag> tags, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || directory == null || tags == null)
        {
            return;
        }

        if (!IsThumbnailCandidate(directory))
        {
            return;
        }

        AddSummaryTags(directory, tags);
        ExtractJpegThumbnail(document, reader, directory, tags, options);
        ExtractStripThumbnail(document, reader, directory, tags, options);
    }

    internal static bool IsThumbnailCandidate(MetadataDirectory directory)
    {
        if (directory == null)
        {
            return false;
        }

        if (directory.Group == "IFD1")
        {
            return true;
        }

        if (directory.Group != null && directory.Group.StartsWith("SubIFD"))
        {
            return true;
        }

        return directory.Family == MetadataFamily.Thumbnail;
    }

    internal static void AddSummaryTags(MetadataDirectory directory, List<MetadataTag> tags)
    {
        MetadataTag? width = FindTag(tags, TagImageWidth);
        MetadataTag? height = FindTag(tags, TagImageLength);

        if (width != null)
        {
            directory.AddTagWithValue("Thumbnail." + directory.Group + ".Width", "Thumbnail Width", MetadataValue.FromUnsignedInteger(FirstUnsignedValue(width)));
        }

        if (height != null)
        {
            directory.AddTagWithValue("Thumbnail." + directory.Group + ".Height", "Thumbnail Height", MetadataValue.FromUnsignedInteger(FirstUnsignedValue(height)));
        }
    }

    internal static void ExtractJpegThumbnail(ImageMetadataDocument document, BoundedStreamReader reader, MetadataDirectory directory, List<MetadataTag> tags, ImageMetadataReaderOptions options)
    {
        MetadataTag? offsetTag = FindTag(tags, TagJpegInterchangeFormat);
        MetadataTag? lengthTag = FindTag(tags, TagJpegInterchangeFormatLength);
        if (offsetTag == null || lengthTag == null)
        {
            return;
        }

        ulong offset = FirstUnsignedValue(offsetTag);
        ulong length = FirstUnsignedValue(lengthTag);
        if (length == 0)
        {
            return;
        }

        byte[] data = ReadThumbnailBytes(document, reader, offset, length, options);
        if ((ulong)data.Length != length)
        {
            return;
        }

        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Thumbnail;
        profile.Family = MetadataFamily.Thumbnail;
        profile.Key = "Thumbnail." + directory.Group + ".JPEG";
        profile.Name = "JPEG Thumbnail";
        profile.MimeType = "image/jpeg";
        profile.Offset = TiffHeaderParser.ClampOffset(offset);
        profile.Length = TiffHeaderParser.ClampOffset(length);
        profile.Data = data;
        document.AddProfile(profile);

        AddRawBlock(document, "JPEG", "JPEG Thumbnail", offset, length, data, options);
        directory.AddTagWithValue("Thumbnail." + directory.Group + ".Format", "Thumbnail Format", MetadataValue.FromString("JPEG"));
        directory.AddTagWithValue("Thumbnail." + directory.Group + ".Offset", "Thumbnail Offset", MetadataValue.FromUnsignedInteger(offset));
        directory.AddTagWithValue("Thumbnail." + directory.Group + ".ByteLength", "Thumbnail Byte Length", MetadataValue.FromUnsignedInteger(length));
    }

    internal static void ExtractStripThumbnail(ImageMetadataDocument document, BoundedStreamReader reader, MetadataDirectory directory, List<MetadataTag> tags, ImageMetadataReaderOptions options)
    {
        MetadataTag? offsetsTag = FindTag(tags, TagStripOffsets);
        MetadataTag? lengthsTag = FindTag(tags, TagStripByteCounts);
        if (offsetsTag == null || lengthsTag == null)
        {
            return;
        }

        List<ulong> offsets = UnsignedValues(offsetsTag);
        List<ulong> lengths = UnsignedValues(lengthsTag);
        int count = offsets.Count;
        if (lengths.Count < count)
        {
            count = lengths.Count;
        }

        int index = 0;
        while (index < count)
        {
            ulong length = lengths[index];
            if (length > 0)
            {
                byte[] data = ReadThumbnailBytes(document, reader, offsets[index], length, options);
                if ((ulong)data.Length == length)
                {
                    AddRawBlock(document, "TIFFStrip", "Thumbnail Strip", offsets[index], length, data, options);
                }
            }

            index = index + 1;
        }

        if (count > 0)
        {
            directory.AddTagWithValue("Thumbnail." + directory.Group + ".Format", "Thumbnail Format", MetadataValue.FromString("TIFF"));
            directory.AddTagWithValue("Thumbnail." + directory.Group + ".StripCount", "Thumbnail Strip Count", MetadataValue.FromInteger(count));
        }
    }

    internal static byte[] ReadThumbnailBytes(ImageMetadataDocument document, BoundedStreamReader reader, ulong offset, ulong length, ImageMetadataReaderOptions options)
    {
        long maximum = GetMaxRawBlockBytes(options);
        if (maximum < 0 || length > 2147483647 || length > (ulong)maximum)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tiff.thumbnail.limit", "TIFF thumbnail byte length exceeded the configured raw block limit.", "TIFF", TiffHeaderParser.ClampOffset(offset), TiffHeaderParser.ClampOffset(length));
            return new byte[0];
        }

        if (!TiffIfdTraverser.CanReadRange(document, offset, (int)length, "tiff.thumbnail.truncated"))
        {
            return new byte[0];
        }

        if (!reader.SeekAbsolute(TiffHeaderParser.ClampOffset(offset)))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tiff.thumbnail.seek", "TIFF thumbnail data offset could not be reached.", "TIFF", TiffHeaderParser.ClampOffset(offset), TiffHeaderParser.ClampOffset(length));
            return new byte[0];
        }

        byte[] bytes = reader.ReadBytes((int)length);
        if ((ulong)bytes.Length != length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tiff.thumbnail.truncated", "TIFF thumbnail data was truncated.", "TIFF", TiffHeaderParser.ClampOffset(offset), TiffHeaderParser.ClampOffset(length));
            return new byte[0];
        }

        return bytes;
    }

    internal static void AddRawBlock(ImageMetadataDocument document, string blockType, string name, ulong offset, ulong length, byte[] data, ImageMetadataReaderOptions options)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }

        MetadataRawBlock rawBlock = new MetadataRawBlock();
        rawBlock.Family = MetadataFamily.Thumbnail;
        rawBlock.BlockType = blockType;
        rawBlock.Name = name;
        rawBlock.Offset = TiffHeaderParser.ClampOffset(offset);
        rawBlock.Length = TiffHeaderParser.ClampOffset(length);
        if (CaptureRawBlockData(options))
        {
            rawBlock.Data = data;
        }

        document.AddRawBlock(rawBlock);
    }

    internal static MetadataTag? FindTag(List<MetadataTag> tags, int numericId)
    {
        foreach (MetadataTag tag in tags)
        {
            if (tag != null && tag.NumericId == numericId)
            {
                return tag;
            }
        }

        return null;
    }

    internal static ulong FirstUnsignedValue(MetadataTag tag)
    {
        List<ulong> values = UnsignedValues(tag);
        if (values.Count == 0)
        {
            return 0;
        }

        return values[0];
    }

    internal static List<ulong> UnsignedValues(MetadataTag tag)
    {
        List<ulong> values = new List<ulong>();
        if (tag == null || tag.Value == null)
        {
            return values;
        }

        if (tag.Value.Kind == MetadataValueKind.UnsignedInteger)
        {
            values.Add(tag.Value.UnsignedIntegerValue);
            return values;
        }

        if (tag.Value.Kind == MetadataValueKind.Integer && tag.Value.IntegerValue >= 0)
        {
            values.Add((ulong)tag.Value.IntegerValue);
            return values;
        }

        if (tag.Value.Kind == MetadataValueKind.UnsignedIntegerArray && tag.Value.UnsignedIntegerValues != null)
        {
            foreach (ulong value in tag.Value.UnsignedIntegerValues)
            {
                values.Add(value);
            }
            return values;
        }

        if (tag.Value.Kind == MetadataValueKind.IntegerArray && tag.Value.IntegerValues != null)
        {
            foreach (long value in tag.Value.IntegerValues)
            {
                if (value >= 0)
                {
                    values.Add((ulong)value);
                }
            }
            return values;
        }

        if (tag.Value.Kind == MetadataValueKind.StringList && tag.Value.TextValues != null)
        {
            foreach (string text in tag.Value.TextValues)
            {
                values.Add(ParseUnsignedInteger(text));
            }
        }

        return values;
    }

    internal static ulong ParseUnsignedInteger(string text)
    {
        if (text == null)
        {
            return 0;
        }

        ulong value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char ch = text[index];
            if (ch < '0' || ch > '9')
            {
                return 0;
            }

            ulong digit = (ulong)(ch - '0');
            if (value > (18446744073709551615UL - digit) / 10UL)
            {
                return 0;
            }

            value = value * 10UL + digit;
            index = index + 1;
        }

        return value;
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().CaptureRawBlocks;
        }

        return options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().CaptureRawBlockData;
        }

        return options.CaptureRawBlockData;
    }

    internal static long GetMaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxRawBlockBytes;
        }

        return options.MaxRawBlockBytes;
    }
}
