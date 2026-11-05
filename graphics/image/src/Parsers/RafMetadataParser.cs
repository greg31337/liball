using System.Collections.Generic;
using System.Text;

namespace image;

internal class RafMetadataParser
{
    internal const int MinimumHeaderLength = 108;
    internal const int MaximumHeaderLength = 148;
    internal const int SignatureLength = 16;
    internal const int ModelOffset = 28;
    internal const int ModelLength = 32;
    internal const int VersionOffset = 60;
    internal const int VersionLength = 4;
    internal const int MRawDescriptorOffset = 72;
    internal const int PreviewDescriptorOffset = 84;
    internal const int RafDescriptorOffset = 92;
    internal const int FujiIfdDescriptorOffset = 100;
    internal const int RafSecondDescriptorOffset = 120;
    internal const int FujiIfdSecondDescriptorOffset = 128;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "raf.reader.null", "RAF parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        if (reader.AccessibleLength() < MinimumHeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "raf.header.truncated", "RAF wrapper header was truncated.", "RAF", 0, MinimumHeaderLength);
            return Finish(document, reader);
        }

        byte[] header = ReadRange(reader, 0, MinimumHeaderLength);
        if (header.Length < MinimumHeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "raf.header.truncated", "RAF wrapper header was truncated.", "RAF", 0, MinimumHeaderLength);
            return Finish(document, reader);
        }

        if (!HasSignature(header))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "raf.signature.invalid", "RAF wrapper signature was invalid.", "RAF", 0, SignatureLength);
            return Finish(document, reader);
        }

        long previewOffset = ReadUnsigned32(header, PreviewDescriptorOffset);
        long previewLength = ReadUnsigned32(header, PreviewDescriptorOffset + 4);
        long headerLength = HeaderLength(reader, previewOffset);
        if (headerLength > header.Length)
        {
            header = ReadRange(reader, 0, (int)headerLength);
        }

        AddHeaderDirectory(document, header, headerLength);
        AddProfileDirectory(document, previewOffset, previewLength);
        CaptureRawBlock(document, reader, options, "Header", "RAF Wrapper Header", 0, headerLength);

        long rafOffset = ReadDescriptorOffset(header, RafDescriptorOffset);
        long rafLength = ReadDescriptorLength(header, RafDescriptorOffset);
        long tiffOffset = ReadDescriptorOffset(header, FujiIfdDescriptorOffset);
        long tiffLength = ReadDescriptorLength(header, FujiIfdDescriptorOffset);
        long rafSecondOffset = ReadDescriptorOffset(header, RafSecondDescriptorOffset);
        long rafSecondLength = ReadDescriptorLength(header, RafSecondDescriptorOffset);
        long tiffSecondOffset = ReadDescriptorOffset(header, FujiIfdSecondDescriptorOffset);
        long tiffSecondLength = ReadDescriptorLength(header, FujiIfdSecondDescriptorOffset);

        ParseRafDirectory(document, reader, options, rafOffset, rafLength, 0);
        ParseRafDirectory(document, reader, options, rafSecondOffset, rafSecondLength, 1);
        ParseEmbeddedJpeg(document, reader, options, previewOffset, previewLength);
        ParseEmbeddedTiff(document, reader, options, tiffOffset, tiffLength, 0);
        if (tiffSecondOffset != tiffOffset || tiffSecondLength != tiffLength)
        {
            ParseEmbeddedTiff(document, reader, options, tiffSecondOffset, tiffSecondLength, 1);
        }

        document.Format = BasicMetadataParser.CopyFormat(format);
        document.ByteLength = reader.Length;
        ImageMetadataNormalizerCore.BuildSummary(document);
        return Finish(document, reader);
    }

    internal static void ParseRafDirectory(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, long offset, long length, int index)
    {
        if (offset == 0 || length == 0)
        {
            return;
        }
        if (!ValidateRange(document, reader, "raf.directory.range.invalid", "RAF proprietary directory exceeded the accessible source range.", offset, length))
        {
            return;
        }
        if (length < 4)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "raf.directory.count.truncated", "RAF proprietary directory count was truncated.", "RAFDirectory", offset, length);
            return;
        }

        byte[] countBytes = ReadRange(reader, offset, 4);
        long entryCount = ReadUnsigned32(countBytes, 0);
        if (entryCount > MaxTagCount(options) || entryCount > 255)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "raf.directory.tag.count.limit", "RAF proprietary directory tag count exceeded the supported limit.", "RAFDirectory", offset, entryCount);
            return;
        }

        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Raw, index == 0 ? "RAF" : "RAF" + index.ToString(), index == 0 ? "RAF Proprietary Directory" : "RAF Proprietary Directory " + index.ToString());
        directory.Path = index == 0 ? "RAF/Directory" : "RAF/Directory" + index.ToString();
        directory.Offset = offset;
        directory.Length = length;
        CaptureRawBlock(document, reader, options, "RAFDirectory", directory.Name, offset, length);
        long position = offset + 4;
        long end = offset + length;
        int entryIndex = 0;
        while (entryIndex < entryCount)
        {
            if (position > end - 4)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "raf.directory.entry.truncated", "RAF proprietary directory entry header was truncated.", "RAFDirectory", position, 4);
                return;
            }
            byte[] entryHeader = ReadRange(reader, position, 4);
            int tagId = ReadUnsigned16(entryHeader, 0);
            int valueLength = ReadUnsigned16(entryHeader, 2);
            long valueOffset = position + 4;
            if (valueLength > end - valueOffset)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "raf.directory.value.truncated", "RAF proprietary directory value exceeded its containing range.", "RAFDirectory", valueOffset, valueLength);
                return;
            }
            if (valueLength > MaxSegmentBytes(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "raf.directory.value.limit", "RAF proprietary directory value exceeded the configured segment limit.", "RAFDirectory", valueOffset, valueLength);
                position = valueOffset + valueLength;
                entryIndex = entryIndex + 1;
                continue;
            }
            byte[] valueBytes = ReadRange(reader, valueOffset, valueLength);
            MetadataValue value = DecodeRafValue(tagId, valueBytes);
            string name = RafTagName(tagId);
            MetadataTag tag = directory.AddTagWithValue("Raw.RAF." + directory.Group + "." + name, name, value);
            tag.NumericId = tagId;
            tag.Offset = valueOffset;
            tag.Length = valueLength;
            tag.RawValue = valueBytes;
            tag.Confidence = 100;
            AddRawDimensions(document, tagId, valueBytes);
            position = valueOffset + valueLength;
            entryIndex = entryIndex + 1;
        }
    }

    internal static void ParseEmbeddedJpeg(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, long offset, long length)
    {
        if (offset == 0 || length == 0)
        {
            return;
        }
        if (!ValidateRange(document, reader, "raf.preview.range.invalid", "RAF JPEG preview exceeded the accessible source range.", offset, length))
        {
            return;
        }
        byte[] signature = ReadRange(reader, offset, 2);
        if (signature.Length < 2 || signature[0] != 255 || signature[1] != 216)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "raf.preview.signature.invalid", "RAF preview range did not contain a JPEG image.", "JPEG", offset, length);
            return;
        }

        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        if (!reader.SeekAbsolute(offset))
        {
            return;
        }
        long limit = length;
        if (reader.RemainingLimit() < limit)
        {
            limit = reader.RemainingLimit();
        }
        BoundedStreamReader embeddedReader = new BoundedStreamReader(reader.Source, length, limit, diagnostics);
        ImageMetadataDocument embedded = JpegMetadataParser.Parse(embeddedReader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), options);
        int previewFrameIndex = document.Frames.Count;
        foreach (ImageMetadataFrame frame in embedded.Frames)
        {
            if (frame != null)
            {
                frame.Role = ImageMetadataRole.Preview;
            }
        }
        RawTiffProfileParser.RelocateDocument(embedded, offset);
        PrefixDocumentPaths(embedded, "RAF/Preview");
        MergeEmbeddedDocument(document, embedded);
        foreach (MetadataProfile profile in embedded.Profiles)
        {
            if (profile.Kind == MetadataProfileKind.Exif)
            {
                ParseEmbeddedTiff(document, reader, options, profile.Offset, profile.Length, -1);
            }
        }
        AddPreviewProfile(document, reader, options, offset, length, embedded.Frames.Count > 0 ? previewFrameIndex : -1);
        CaptureRawBlock(document, reader, options, "JPEG", "RAF JPEG Preview", offset, length);
    }

    internal static void ParseEmbeddedTiff(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, long offset, long length, int index)
    {
        if (offset == 0 || length == 0)
        {
            return;
        }
        if (!ValidateRange(document, reader, "raf.tiff.range.invalid", "RAF TIFF metadata range exceeded the accessible source range.", offset, length))
        {
            return;
        }
        if (length < 8)
        {
            return;
        }
        byte[] signature = ReadRange(reader, offset, 4);
        bool classic = IsClassicTiff(signature);
        bool big = IsBigTiff(signature);
        if (!classic && !big)
        {
            return;
        }

        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        if (!reader.SeekAbsolute(offset))
        {
            return;
        }
        long limit = length;
        if (reader.RemainingLimit() < limit)
        {
            limit = reader.RemainingLimit();
        }
        BoundedStreamReader embeddedReader = new BoundedStreamReader(reader.Source, length, limit, diagnostics);
        ImageFormatKind kind = big ? ImageFormatKind.BigTiff : ImageFormatKind.Tiff;
        ImageMetadataDocument embedded = TiffMetadataParser.Parse(embeddedReader, ImageFormatMapping.CreateInfo(kind), options);
        RawTiffProfileParser.RelocateDocument(embedded, offset);
        string prefix = "RAF/FujiIFD";
        if (index < 0)
        {
            prefix = "RAF/Preview/EXIF";
        }
        else if (index > 0)
        {
            prefix = "RAF/FujiIFD" + index.ToString();
        }
        PrefixDocumentPaths(embedded, prefix);
        MergeEmbeddedDocument(document, embedded);
        if (index < 0)
        {
            CaptureRawBlock(document, reader, options, "EXIF", "RAF Preview EXIF Metadata", offset, length);
        }
        else
        {
            CaptureRawBlock(document, reader, options, "FujiIFD", index == 0 ? "RAF TIFF Metadata" : "RAF TIFF Metadata " + index.ToString(), offset, length);
        }
    }

    internal static void MergeEmbeddedDocument(ImageMetadataDocument document, ImageMetadataDocument embedded)
    {
        int frameOffset = document.Frames.Count;
        foreach (MetadataDirectory directory in embedded.Directories)
        {
            document.AddDirectory(directory);
        }
        foreach (ImageMetadataFrame frame in embedded.Frames)
        {
            ImageMetadataFrame added = document.AddFrame(frame);
            foreach (MetadataDirectory directory in added.Directories)
            {
                SetFrameIndex(directory, added.Index);
            }
        }
        foreach (MetadataProfile profile in embedded.Profiles)
        {
            if (profile.SourceFrameIndex >= 0)
            {
                profile.SourceFrameIndex = profile.SourceFrameIndex + frameOffset;
            }
            document.AddProfile(profile);
        }
        foreach (MetadataRawBlock block in embedded.RawBlocks)
        {
            document.AddRawBlock(block);
        }
        foreach (MetadataDiagnostic diagnostic in embedded.Diagnostics)
        {
            document.AddDiagnostic(diagnostic);
        }
    }

    internal static void PrefixDocumentPaths(ImageMetadataDocument document, string prefix)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            PrefixDirectoryPath(directory, prefix);
        }
        foreach (ImageMetadataFrame frame in document.Frames)
        {
            foreach (MetadataDirectory directory in frame.Directories)
            {
                PrefixDirectoryPath(directory, prefix);
            }
        }
    }

    internal static void PrefixDirectoryPath(MetadataDirectory directory, string prefix)
    {
        directory.Path = prefix + "/" + directory.Path;
        foreach (MetadataDirectory child in directory.Children)
        {
            PrefixDirectoryPath(child, prefix);
        }
    }

    internal static void SetFrameIndex(MetadataDirectory directory, int frameIndex)
    {
        directory.FrameIndex = frameIndex;
        foreach (MetadataDirectory child in directory.Children)
        {
            SetFrameIndex(child, frameIndex);
        }
    }

    internal static void AddRawDimensions(ImageMetadataDocument document, int tagId, byte[] bytes)
    {
        if (tagId != 256 && tagId != 273 && tagId != 289 || bytes.Length < 4)
        {
            return;
        }
        int height = ReadUnsigned16(bytes, 0);
        int width = ReadUnsigned16(bytes, 2);
        if (width <= 0 || height <= 0)
        {
            return;
        }
        if (document.Frames.Count == 0)
        {
            ImageMetadataFrame frame = document.AddFrameWithDimensions(width, height);
            frame.Compression = ImageCompressionKind.Unknown;
        }
    }

    internal static MetadataValue DecodeRafValue(int tagId, byte[] bytes)
    {
        if (bytes.Length == 4 && IsSizeTag(tagId))
        {
            return MetadataValue.FromUnsignedIntegerArray(new ulong[] { (ulong)ReadUnsigned16(bytes, 2), (ulong)ReadUnsigned16(bytes, 0) });
        }
        if (bytes.Length == 4)
        {
            return MetadataValue.FromUnsignedInteger(ReadUnsigned32(bytes, 0));
        }
        if (bytes.Length == 2)
        {
            return MetadataValue.FromUnsignedInteger((ulong)ReadUnsigned16(bytes, 0));
        }
        return MetadataValue.FromBytes(bytes);
    }

    internal static bool IsSizeTag(int tagId)
    {
        return tagId == 256 || tagId == 272 || tagId == 273 || tagId == 274 || tagId == 275 || tagId == 277 || tagId == 280 || tagId == 281 || tagId == 289;
    }

    internal static string RafTagName(int tagId)
    {
        switch (tagId)
        {
            case 256: return "RawImageFullSize";
            case 272: return "RawImageCropTopLeft";
            case 273: return "RawImageCroppedSize";
            case 277: return "RawImageAspectRatio";
            case 279: return "RawZoomActive";
            case 280: return "RawZoomTopLeft";
            case 281: return "RawZoomSize";
            case 289: return "RawImageSize";
            case 304: return "FujiLayout";
            case 305: return "XTransLayout";
            case 8192: return "WhiteBalanceAuto";
            case 12272: return "WhiteBalanceAsShot";
            case 37376: return "RelativeExposure";
            case 38480: return "RawExposureBias";
            case 49152: return "RAFData";
            default: return "Tag" + tagId.ToString("X4");
        }
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, byte[] header, long headerLength)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Raw, "Header", "RAF Wrapper Header");
        directory.Path = "RAF/Header";
        directory.Offset = 0;
        directory.Length = headerLength;
        AddHeaderTag(directory, "Signature", "Signature", MetadataValue.FromString(DecodeAscii(header, 0, SignatureLength)), 0, SignatureLength);
        AddHeaderTag(directory, "FileVersion", "File Version", MetadataValue.FromString(DecodeAscii(header, 16, 4)), 16, 4);
        AddHeaderTag(directory, "CameraIdentifier", "Camera Identifier", MetadataValue.FromString(DecodeAscii(header, 20, 8)), 20, 8);
        AddHeaderTag(directory, "CameraMake", "Camera Make", MetadataValue.FromString("FUJIFILM"), 0, SignatureLength);
        AddHeaderTag(directory, "CameraModel", "Camera Model", MetadataValue.FromString(DecodeAscii(header, ModelOffset, ModelLength)), ModelOffset, ModelLength);
        AddHeaderTag(directory, "DirectoryVersion", "Directory Version", MetadataValue.FromString(DecodeAscii(header, VersionOffset, VersionLength)), VersionOffset, VersionLength);
        AddDescriptorTags(directory, header, "MRaw", "M-RAW", MRawDescriptorOffset);
        AddDescriptorTags(directory, header, "Preview", "JPEG Preview", PreviewDescriptorOffset);
        AddDescriptorTags(directory, header, "RafDirectory", "RAF Directory", RafDescriptorOffset);
        AddDescriptorTags(directory, header, "FujiIfd", "Fuji IFD", FujiIfdDescriptorOffset);
        AddDescriptorTags(directory, header, "RafDirectory2", "RAF Directory 2", RafSecondDescriptorOffset);
        AddDescriptorTags(directory, header, "FujiIfd2", "Fuji IFD 2", FujiIfdSecondDescriptorOffset);
    }

    internal static void AddDescriptorTags(MetadataDirectory directory, byte[] header, string keyName, string name, int offset)
    {
        if (offset + 8 > header.Length)
        {
            return;
        }
        AddHeaderTag(directory, keyName + "Offset", name + " Offset", MetadataValue.FromUnsignedInteger(ReadUnsigned32(header, offset)), offset, 4);
        AddHeaderTag(directory, keyName + "Length", name + " Length", MetadataValue.FromUnsignedInteger(ReadUnsigned32(header, offset + 4)), offset + 4, 4);
    }

    internal static void AddHeaderTag(MetadataDirectory directory, string keyName, string name, MetadataValue value, long offset, long length)
    {
        MetadataTag tag = directory.AddTagWithValue("Raw.RAF.Header." + keyName, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.Confidence = 100;
    }

    internal static void AddProfileDirectory(ImageMetadataDocument document, long previewOffset, long previewLength)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Raw, "Profile", "RAF RAW Profile");
        directory.Path = "Raw/Profile";
        directory.Offset = 0;
        directory.Length = document.ByteLength;
        directory.AddTagWithValue("Raw.Profile.Format", "RAW Format", MetadataValue.FromString("RAF"));
        directory.AddTagWithValue("Raw.Profile.Container", "Container", MetadataValue.FromString("RAF"));
        directory.AddTagWithValue("Raw.Profile.HeaderVariant", "Header Variant", MetadataValue.FromString("FUJIFILMCCD-RAW"));
        directory.AddTagWithValue("Raw.Profile.PreviewOffset", "Preview Offset", MetadataValue.FromInteger(previewOffset));
        directory.AddTagWithValue("Raw.Profile.PreviewLength", "Preview Length", MetadataValue.FromInteger(previewLength));
    }

    internal static void AddPreviewProfile(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, long offset, long length, int sourceFrameIndex)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Thumbnail;
        profile.Role = ImageMetadataRole.Preview;
        profile.SourceFrameIndex = sourceFrameIndex;
        profile.Family = MetadataFamily.Thumbnail;
        profile.Key = "Thumbnail.RAF.Preview";
        profile.Name = "RAF JPEG Preview";
        profile.MimeType = "image/jpeg";
        profile.Offset = offset;
        profile.Length = length;
        long maximum = MaxProfileBytes(options);
        if (length <= maximum && length <= 2147483647)
        {
            profile.Data = ReadRange(reader, offset, (int)length);
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "raf.preview.profile.limit", "RAF JPEG preview exceeded the configured profile limit.", "JPEG", offset, length);
        }
        document.AddProfile(profile);
    }

    internal static void CaptureRawBlock(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, string blockType, string name, long offset, long length)
    {
        if (!CaptureRawBlocks(options) || length <= 0)
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
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "raf.raw.block.limit", "RAF raw metadata block exceeded the configured raw block limit.", blockType, offset, length);
            return;
        }
        block.Data = ReadRange(reader, offset, (int)length);
    }

    internal static bool ValidateRange(ImageMetadataDocument document, BoundedStreamReader reader, string code, string message, long offset, long length)
    {
        long available = reader.AccessibleLength();
        if (offset < 0 || length < 0 || offset > available || length > available - offset)
        {
            if (available < reader.Length)
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, code, message, "RAF", offset, length);
            }
            else
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, code, message, "RAF", offset, length);
            }
            return false;
        }
        return true;
    }

    internal static long HeaderLength(BoundedStreamReader reader, long previewOffset)
    {
        long length = reader.AccessibleLength();
        if (length > MaximumHeaderLength)
        {
            length = MaximumHeaderLength;
        }
        if (previewOffset > 0 && length > previewOffset)
        {
            length = previewOffset;
        }
        if (length < MinimumHeaderLength)
        {
            length = MinimumHeaderLength;
        }
        return length;
    }

    internal static long ReadDescriptorOffset(byte[] header, int offset)
    {
        return offset + 8 <= header.Length ? ReadUnsigned32(header, offset) : 0;
    }

    internal static long ReadDescriptorLength(byte[] header, int offset)
    {
        return offset + 8 <= header.Length ? ReadUnsigned32(header, offset + 4) : 0;
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
        byte[] signature = Encoding.ASCII.GetBytes("FUJIFILMCCD-RAW");
        if (bytes == null || bytes.Length < SignatureLength)
        {
            return false;
        }
        int index = 0;
        while (index < signature.Length)
        {
            if (bytes[index] != signature[index])
            {
                return false;
            }
            index = index + 1;
        }
        return bytes[15] == 0 || bytes[15] == 32;
    }

    internal static bool IsClassicTiff(byte[] bytes)
    {
        return bytes.Length >= 4 && (bytes[0] == 73 && bytes[1] == 73 && bytes[2] == 42 && bytes[3] == 0 || bytes[0] == 77 && bytes[1] == 77 && bytes[2] == 0 && bytes[3] == 42);
    }

    internal static bool IsBigTiff(byte[] bytes)
    {
        return bytes.Length >= 4 && (bytes[0] == 73 && bytes[1] == 73 && bytes[2] == 43 && bytes[3] == 0 || bytes[0] == 77 && bytes[1] == 77 && bytes[2] == 0 && bytes[3] == 43);
    }

    internal static int ReadUnsigned16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    internal static uint ReadUnsigned32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    internal static string DecodeAscii(byte[] bytes, int offset, int length)
    {
        int actual = 0;
        while (actual < length && offset + actual < bytes.Length && bytes[offset + actual] != 0)
        {
            actual = actual + 1;
        }
        return Encoding.ASCII.GetString(bytes, offset, actual).TrimEnd();
    }

    internal static int MaxTagCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static long MaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }

    internal static long MaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
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

    internal static ImageMetadataDocument Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader != null && reader.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
            {
                document.AddDiagnostic(diagnostic);
            }
            reader.SeekAbsolute(0);
        }
        return document;
    }
}
