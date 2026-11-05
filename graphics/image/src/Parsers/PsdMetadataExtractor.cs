using System.Collections.Generic;

namespace image;

internal class PsdMetadataExtractor
{
    internal const int ResolutionResourceId = 1005;
    internal const int IptcResourceId = 1028;
    internal const int ThumbnailOldResourceId = 1033;
    internal const int ThumbnailResourceId = 1036;
    internal const int IccResourceId = 1039;
    internal const int ExifData1ResourceId = 1058;
    internal const int ExifData3ResourceId = 1059;
    internal const int XmpResourceId = 1060;

    internal static void ExtractColorModeData(ImageMetadataDocument document, BoundedStreamReader reader, PsdHeader header, PsdSection section, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || header == null || section == null)
        {
            return;
        }

        if (section.AvailableLength > 0)
        {
            CaptureRawBlock(document, reader, section.DataOffset, section.AvailableLength, "ColorModeData", "Photoshop Color Mode Data", options);
        }

        string kind = "None";
        if (header.ColorMode == 2)
        {
            kind = "Indexed color table";
            PsdDocumentParser.AddTag(section.Directory, "Psd.ColorModeData.PaletteEntryCount", "Palette Entry Count", MetadataValue.FromInteger(section.AvailableLength / 3), section.DataOffset, section.AvailableLength, "");
            PsdDocumentParser.AddTag(section.Directory, "Psd.ColorModeData.StandardPaletteLength", "Standard Palette Length", MetadataValue.FromBoolean(section.DeclaredLength == 768), section.DataOffset, section.AvailableLength, "");
        }
        else if (header.ColorMode == 8)
        {
            kind = "Duotone specification";
            PsdDocumentParser.AddTag(section.Directory, "Psd.ColorModeData.DuotonePreserved", "Duotone Data Preserved", MetadataValue.FromBoolean(section.AvailableLength > 0), section.DataOffset, section.AvailableLength, "");
        }
        else if (section.AvailableLength > 0)
        {
            kind = "Opaque color mode data";
        }

        PsdDocumentParser.AddTag(section.Directory, "Psd.ColorModeData.Kind", "Color Mode Data Kind", MetadataValue.FromString(kind), section.DataOffset, section.AvailableLength, "");
        SeekSectionEnd(reader, section);
    }

    internal static void ExtractImageResources(ImageMetadataDocument document, BoundedStreamReader reader, PsdSection section, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || section == null)
        {
            return;
        }

        if (section.AvailableLength > 0)
        {
            AddProfile(document, reader, MetadataProfileKind.PhotoshopImageResource, MetadataFamily.Photoshop, "Photoshop.PSD.ImageResources", "Photoshop Image Resources", "application/octet-stream", section.DataOffset, section.AvailableLength, GetMaxProfileBytes(options), "psd.resources.profile.limit", "ImageResources", !section.Limited);
            CaptureRawBlock(document, reader, section.DataOffset, section.AvailableLength, "ImageResources", "Photoshop Image Resources", options);
        }

        List<PhotoshopImageResource> resources = new List<PhotoshopImageResource>();
        if (!section.Limited && section.AvailableLength >= PhotoshopImageResourceParser.MinimumResourceLength)
        {
            resources = PhotoshopImageResourceParser.Parse(document, reader, section.DataOffset, section.AvailableLength, options, "psd.resource", "ImageResources");
        }
        else if (!section.Limited && section.AvailableLength >= 4 && SeekAndHasResourceSignature(reader, section.DataOffset))
        {
            resources = PhotoshopImageResourceParser.Parse(document, reader, section.DataOffset, section.AvailableLength, options, "psd.resource", "ImageResources");
        }

        foreach (PhotoshopImageResource resource in resources)
        {
            MetadataDirectory directory = PhotoshopImageResourceParser.AddDirectory(document, section.Directory, resource, "PSD/ImageResources/" + resource.ResourceId.ToString());
            if ((ulong)resource.Data.Length == resource.DeclaredDataLength)
            {
                ExtractResource(document, reader, directory, resource.ResourceId, resource.DataOffset, resource.DeclaredDataLength, options);
            }
        }

        PsdDocumentParser.AddTag(section.Directory, "Psd.ImageResources.ResourceCount", "Resource Count", MetadataValue.FromInteger(resources.Count), section.DataOffset, section.AvailableLength, "");
        SeekSectionEnd(reader, section);
    }

    internal static void ExtractResource(ImageMetadataDocument document, BoundedStreamReader reader, MetadataDirectory directory, int resourceId, long dataOffset, uint dataLength, ImageMetadataReaderOptions options)
    {
        if (resourceId == ResolutionResourceId)
        {
            ExtractResolution(document, reader, directory, dataOffset, dataLength);
            return;
        }

        if (resourceId == ExifData1ResourceId || resourceId == ExifData3ResourceId)
        {
            AddProfile(document, reader, MetadataProfileKind.Exif, MetadataFamily.Exif, resourceId == ExifData1ResourceId ? "Exif.PSD.Data1" : "Exif.PSD.Data3", resourceId == ExifData1ResourceId ? "EXIF Data 1" : "EXIF Data 3", "image/tiff", dataOffset, dataLength, GetMaxProfileBytes(options), "psd.exif.profile.limit", "ImageResources", true);
            return;
        }

        if (resourceId == XmpResourceId && ShouldParseXmp(options))
        {
            AddXmpProfile(document, reader, dataOffset, dataLength, options);
            return;
        }

        if (resourceId == IptcResourceId)
        {
            AddProfile(document, reader, MetadataProfileKind.Iptc, MetadataFamily.Iptc, "Iptc.PSD", "IPTC IIM", "application/octet-stream", dataOffset, dataLength, GetMaxProfileBytes(options), "psd.iptc.profile.limit", "ImageResources", true);
            byte[] iptc = ReadRange(reader, dataOffset, dataLength);
            IptcIimParser.Parse(document, iptc, dataOffset, "PSD/ImageResources/IPTC", "psd.iptc", "ImageResources", options);
            return;
        }

        if (resourceId == IccResourceId && ShouldParseIcc(options))
        {
            MetadataProfile profile = AddProfile(document, reader, MetadataProfileKind.Icc, MetadataFamily.Icc, "Icc.PSD", "ICC Profile", "application/vnd.iccprofile", dataOffset, dataLength, GetMaxProfileBytes(options), "psd.icc.profile.limit", "ImageResources", true);
            IccProfileParser.ParseProfile(document, profile, options, "PSD/ImageResources/ICC", "ImageResources");
            return;
        }

        if (resourceId == ThumbnailOldResourceId || resourceId == ThumbnailResourceId)
        {
            ExtractThumbnail(document, reader, directory, resourceId, dataOffset, dataLength, options);
        }
    }

    internal static void ExtractResolution(ImageMetadataDocument document, BoundedStreamReader reader, MetadataDirectory directory, long dataOffset, uint dataLength)
    {
        if (dataLength < 16 || !reader.SeekAbsolute(dataOffset) || !reader.CanRead(16))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.resolution.truncated", "Photoshop resolution resource was shorter than 16 bytes.", "ImageResources", dataOffset, dataLength);
            return;
        }

        EndianBinaryReader endian = new EndianBinaryReader(reader, false);
        double horizontal = endian.ReadUnsignedFixed16Dot16();
        int horizontalUnit = endian.ReadUnsigned16();
        int widthUnit = endian.ReadUnsigned16();
        double vertical = endian.ReadUnsignedFixed16Dot16();
        int verticalUnit = endian.ReadUnsigned16();
        int heightUnit = endian.ReadUnsigned16();
        PsdDocumentParser.AddTag(directory, "Psd.Resolution.Horizontal", "Horizontal Resolution", MetadataValue.FromDouble(horizontal), dataOffset, 4, ResolutionUnitName(horizontalUnit));
        PsdDocumentParser.AddTag(directory, "Psd.Resolution.HorizontalUnit", "Horizontal Resolution Unit", MetadataValue.FromInteger(horizontalUnit), dataOffset + 4, 2, ResolutionUnitName(horizontalUnit));
        PsdDocumentParser.AddTag(directory, "Psd.Resolution.WidthUnit", "Width Unit", MetadataValue.FromInteger(widthUnit), dataOffset + 6, 2, DimensionUnitName(widthUnit));
        PsdDocumentParser.AddTag(directory, "Psd.Resolution.Vertical", "Vertical Resolution", MetadataValue.FromDouble(vertical), dataOffset + 8, 4, ResolutionUnitName(verticalUnit));
        PsdDocumentParser.AddTag(directory, "Psd.Resolution.VerticalUnit", "Vertical Resolution Unit", MetadataValue.FromInteger(verticalUnit), dataOffset + 12, 2, ResolutionUnitName(verticalUnit));
        PsdDocumentParser.AddTag(directory, "Psd.Resolution.HeightUnit", "Height Unit", MetadataValue.FromInteger(heightUnit), dataOffset + 14, 2, DimensionUnitName(heightUnit));
    }

    internal static void AddXmpProfile(ImageMetadataDocument document, BoundedStreamReader reader, long dataOffset, uint dataLength, ImageMetadataReaderOptions options)
    {
        long limit = GetMaxXmlBytes(options);
        if (GetMaxProfileBytes(options) < limit)
        {
            limit = GetMaxProfileBytes(options);
        }
        MetadataProfile profile = CreateProfile(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.PSD", "XMP Packet", "application/rdf+xml", dataOffset, dataLength);
        if ((long)dataLength > limit || dataLength > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "psd.xmp.profile.limit", "Photoshop XMP packet exceeded the configured XML limit.", "ImageResources", dataOffset, dataLength);
        }
        else
        {
            profile.Data = ReadRange(reader, dataOffset, dataLength);
            profile.Text = MetadataXmlReader.DecodeXmlBytes(profile.Data, limit, document.Diagnostics, "ImageResources");
        }

        document.AddProfile(profile);
        XmpPacketParser.ParseProfile(document, profile, options, "PSD/ImageResources/XMP");
    }

    internal static void ExtractThumbnail(ImageMetadataDocument document, BoundedStreamReader reader, MetadataDirectory directory, int resourceId, long dataOffset, uint dataLength, ImageMetadataReaderOptions options)
    {
        if (dataLength < 28 || !reader.SeekAbsolute(dataOffset) || !reader.CanRead(28))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.thumbnail.header.truncated", "Photoshop thumbnail resource header was truncated.", "ImageResources", dataOffset, dataLength);
            return;
        }

        EndianBinaryReader endian = new EndianBinaryReader(reader, false);
        uint format = endian.ReadUnsigned32();
        uint width = endian.ReadUnsigned32();
        uint height = endian.ReadUnsigned32();
        uint rowBytes = endian.ReadUnsigned32();
        uint totalSize = endian.ReadUnsigned32();
        uint compressedSize = endian.ReadUnsigned32();
        int bitsPerPixel = endian.ReadUnsigned16();
        int planes = endian.ReadUnsigned16();
        long imageOffset = dataOffset + 28;
        long available = dataLength - 28;
        long imageLength = compressedSize;
        if (imageLength > available)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.thumbnail.data.truncated", "Photoshop thumbnail image data was truncated.", "ImageResources", imageOffset, imageLength);
            imageLength = available;
        }

        PsdDocumentParser.AddTag(directory, "Psd.Thumbnail.Format", "Thumbnail Format", MetadataValue.FromUnsignedInteger(format), dataOffset, 4, format == 1 ? "JPEG RGB" : format == 0 ? "Raw RGB" : "Unknown");
        PsdDocumentParser.AddTag(directory, "Psd.Thumbnail.Width", "Thumbnail Width", MetadataValue.FromUnsignedInteger(width), dataOffset + 4, 4, "");
        PsdDocumentParser.AddTag(directory, "Psd.Thumbnail.Height", "Thumbnail Height", MetadataValue.FromUnsignedInteger(height), dataOffset + 8, 4, "");
        PsdDocumentParser.AddTag(directory, "Psd.Thumbnail.RowBytes", "Thumbnail Row Bytes", MetadataValue.FromUnsignedInteger(rowBytes), dataOffset + 12, 4, "");
        PsdDocumentParser.AddTag(directory, "Psd.Thumbnail.TotalSize", "Thumbnail Total Size", MetadataValue.FromUnsignedInteger(totalSize), dataOffset + 16, 4, "");
        PsdDocumentParser.AddTag(directory, "Psd.Thumbnail.CompressedSize", "Thumbnail Compressed Size", MetadataValue.FromUnsignedInteger(compressedSize), dataOffset + 20, 4, "");
        PsdDocumentParser.AddTag(directory, "Psd.Thumbnail.BitsPerPixel", "Thumbnail Bits Per Pixel", MetadataValue.FromInteger(bitsPerPixel), dataOffset + 24, 2, "");
        PsdDocumentParser.AddTag(directory, "Psd.Thumbnail.Planes", "Thumbnail Planes", MetadataValue.FromInteger(planes), dataOffset + 26, 2, "");
        AddProfile(document, reader, MetadataProfileKind.Thumbnail, MetadataFamily.Thumbnail, resourceId == ThumbnailOldResourceId ? "Thumbnail.PSD.4" : "Thumbnail.PSD.5", "Photoshop Thumbnail", format == 1 ? "image/jpeg" : "application/octet-stream", imageOffset, imageLength, GetMaxProfileBytes(options), "psd.thumbnail.profile.limit", "ImageResources", true);
    }

    internal static void ExtractLayerSummary(ImageMetadataDocument document, BoundedStreamReader reader, PsdHeader header, PsdSection section, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || header == null || section == null)
        {
            return;
        }

        int lengthSize = header.Version == PsdDocumentParser.PsbVersion ? 8 : 4;
        if (section.Limited)
        {
            SeekSectionEnd(reader, section);
            return;
        }

        if (section.AvailableLength == 0)
        {
            PsdDocumentParser.AddTag(section.Directory, "Psd.LayerAndMask.LayerCount", "Layer Count", MetadataValue.FromInteger(0), section.DataOffset, 0, "");
            PsdDocumentParser.AddTag(section.Directory, "Psd.LayerAndMask.ParsedLayerCount", "Parsed Layer Count", MetadataValue.FromInteger(0), section.DataOffset, 0, "");
            SeekSectionEnd(reader, section);
            return;
        }

        if (section.AvailableLength < lengthSize)
        {
            SeekSectionEnd(reader, section);
            return;
        }

        long outerEnd = section.DataOffset + section.AvailableLength;
        reader.SeekAbsolute(section.DataOffset);
        EndianBinaryReader endian = new EndianBinaryReader(reader, false);
        ulong declaredLength = lengthSize == 8 ? endian.ReadUnsigned64() : endian.ReadUnsigned32();
        long available = outerEnd - reader.Position;
        PsdDocumentParser.AddTag(section.Directory, "Psd.LayerAndMask.LayerInfoLength", "Layer Information Length", MetadataValue.FromUnsignedInteger(declaredLength), section.DataOffset, lengthSize, "");
        if (declaredLength == 0)
        {
            PsdDocumentParser.AddTag(section.Directory, "Psd.LayerAndMask.LayerCount", "Layer Count", MetadataValue.FromInteger(0), reader.Position, 0, "");
            PsdDocumentParser.AddTag(section.Directory, "Psd.LayerAndMask.ParsedLayerCount", "Parsed Layer Count", MetadataValue.FromInteger(0), reader.Position, 0, "");
            SeekSectionEnd(reader, section);
            return;
        }

        if (section.AvailableLength < lengthSize + 2)
        {
            SeekSectionEnd(reader, section);
            return;
        }

        if (declaredLength > (ulong)available)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.layer-info.truncated", "Photoshop layer information exceeded the layer and mask section.", "LayerAndMask", reader.Position, PsdDocumentParser.ToLong(declaredLength));
            SeekSectionEnd(reader, section);
            return;
        }

        long infoEnd = reader.Position + (long)declaredLength;
        if (!CanRead(reader, infoEnd, 2))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.layer-count.truncated", "Photoshop layer count was truncated.", "LayerAndMask", reader.Position, 2);
            SeekSectionEnd(reader, section);
            return;
        }

        long countOffset = reader.Position;
        int signedCount = endian.ReadSigned16();
        int layerCount = signedCount < 0 ? -signedCount : signedCount;
        bool mergedTransparency = signedCount < 0;
        PsdDocumentParser.AddTag(section.Directory, "Psd.LayerAndMask.LayerCount", "Layer Count", MetadataValue.FromInteger(layerCount), countOffset, 2, "");
        PsdDocumentParser.AddTag(section.Directory, "Psd.LayerAndMask.MergedTransparency", "Merged Transparency Channel", MetadataValue.FromBoolean(mergedTransparency), countOffset, 2, "");

        int maximum = GetMaxTagCount(options);
        if (maximum < 0)
        {
            maximum = 0;
        }
        int parseCount = layerCount;
        if (parseCount > maximum)
        {
            parseCount = maximum;
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "psd.layer.count.limit", "Photoshop layer count exceeded the configured tag limit.", "LayerAndMask", countOffset, 2);
        }

        int parsed = 0;
        while (parsed < parseCount)
        {
            if (!ReadLayerRecord(document, reader, section, infoEnd, header.Version, parsed))
            {
                break;
            }

            parsed = parsed + 1;
        }

        PsdDocumentParser.AddTag(section.Directory, "Psd.LayerAndMask.ParsedLayerCount", "Parsed Layer Count", MetadataValue.FromInteger(parsed), countOffset, 2, "");
        SeekSectionEnd(reader, section);
    }

    internal static bool ReadLayerRecord(ImageMetadataDocument document, BoundedStreamReader reader, PsdSection section, long infoEnd, int version, int index)
    {
        long start = reader.Position;
        if (!CanRead(reader, infoEnd, 34))
        {
            AddLayerTruncated(document, start, infoEnd - start);
            return false;
        }

        EndianBinaryReader endian = new EndianBinaryReader(reader, false);
        int top = endian.ReadSigned32();
        int left = endian.ReadSigned32();
        int bottom = endian.ReadSigned32();
        int right = endian.ReadSigned32();
        int channelCount = endian.ReadUnsigned16();
        int channelLengthSize = version == PsdDocumentParser.PsbVersion ? 8 : 4;
        long channelEntryBytes = (long)channelCount * (2 + channelLengthSize);
        if (!CanRead(reader, infoEnd, channelEntryBytes + 16))
        {
            AddLayerTruncated(document, start, infoEnd - start);
            return false;
        }

        ulong channelDataLength = 0;
        int channel = 0;
        while (channel < channelCount)
        {
            endian.ReadSigned16();
            ulong length = channelLengthSize == 8 ? endian.ReadUnsigned64() : endian.ReadUnsigned32();
            channelDataLength = AddSaturated(channelDataLength, length);
            channel = channel + 1;
        }

        long blendOffset = reader.Position;
        string blendSignature = MetadataStringReader.DecodeLatin1(reader.ReadBytes(4));
        string blendMode = MetadataStringReader.DecodeLatin1(reader.ReadBytes(4));
        int opacity = reader.ReadByte();
        int clipping = reader.ReadByte();
        int flags = reader.ReadByte();
        reader.ReadByte();
        uint extraLength = endian.ReadUnsigned32();
        long extraOffset = reader.Position;
        if (!CanRead(reader, infoEnd, extraLength))
        {
            AddLayerTruncated(document, extraOffset, extraLength);
            return false;
        }

        long extraEnd = extraOffset + extraLength;
        string? name = ReadLayerName(document, reader, extraEnd);
        if (name == null)
        {
            return false;
        }

        reader.SeekAbsolute(extraEnd);
        MetadataDirectory directory = section.Directory.AddChildWithName(MetadataFamily.Psd, "Layer", name.Length == 0 ? "Layer " + index.ToString() : name);
        directory.Path = "PSD/LayerAndMask/Layer/" + index.ToString();
        directory.Offset = start;
        directory.Length = extraEnd - start;
        directory.RepeatIndex = index;
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Index", "Layer Index", MetadataValue.FromInteger(index), start, 0, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Name", "Layer Name", MetadataValue.FromString(name), extraOffset, extraLength, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Top", "Top", MetadataValue.FromInteger(top), start, 4, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Left", "Left", MetadataValue.FromInteger(left), start + 4, 4, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Bottom", "Bottom", MetadataValue.FromInteger(bottom), start + 8, 4, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Right", "Right", MetadataValue.FromInteger(right), start + 12, 4, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Width", "Width", MetadataValue.FromInteger((long)right - left), start + 4, 12, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Height", "Height", MetadataValue.FromInteger((long)bottom - top), start, 12, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.ChannelCount", "Channel Count", MetadataValue.FromInteger(channelCount), start + 16, 2, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.ChannelDataLength", "Channel Data Length", MetadataValue.FromUnsignedInteger(channelDataLength), start + 18, channelEntryBytes, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.BlendMode", "Blend Mode", MetadataValue.FromString(blendMode), blendOffset + 4, 4, BlendModeName(blendMode));
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Opacity", "Opacity", MetadataValue.FromInteger(opacity), blendOffset + 8, 1, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Clipping", "Clipping", MetadataValue.FromInteger(clipping), blendOffset + 9, 1, clipping == 0 ? "Base" : "Non-base");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Flags", "Flags", MetadataValue.FromInteger(flags), blendOffset + 10, 1, "");
        PsdDocumentParser.AddTag(directory, "Psd.Layer.Visible", "Visible", MetadataValue.FromBoolean((flags & 2) == 0), blendOffset + 10, 1, "");
        if (blendSignature != "8BIM")
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "psd.layer.blend-signature.invalid", "Photoshop layer blend mode signature was invalid.", "LayerAndMask", blendOffset, 4);
        }

        return true;
    }

    internal static string? ReadLayerName(ImageMetadataDocument document, BoundedStreamReader reader, long extraEnd)
    {
        EndianBinaryReader endian = new EndianBinaryReader(reader, false);
        if (!CanRead(reader, extraEnd, 4))
        {
            AddLayerTruncated(document, reader.Position, extraEnd - reader.Position);
            return null;
        }

        uint maskLength = endian.ReadUnsigned32();
        if (!CanRead(reader, extraEnd, maskLength))
        {
            AddLayerTruncated(document, reader.Position, maskLength);
            return null;
        }

        reader.SkipBytes(maskLength);
        if (!CanRead(reader, extraEnd, 4))
        {
            AddLayerTruncated(document, reader.Position, extraEnd - reader.Position);
            return null;
        }

        uint rangesLength = endian.ReadUnsigned32();
        if (!CanRead(reader, extraEnd, rangesLength))
        {
            AddLayerTruncated(document, reader.Position, rangesLength);
            return null;
        }

        reader.SkipBytes(rangesLength);
        if (reader.Position >= extraEnd)
        {
            return "";
        }

        int nameLength = reader.ReadByte();
        if (!CanRead(reader, extraEnd, nameLength))
        {
            AddLayerTruncated(document, reader.Position, nameLength);
            return null;
        }

        string name = MetadataStringReader.DecodeLatin1(reader.ReadBytes(nameLength));
        long storage = PadFour(1 + nameLength);
        long remainingStorage = storage - 1 - nameLength;
        if (!CanRead(reader, extraEnd, remainingStorage))
        {
            AddLayerTruncated(document, reader.Position, remainingStorage);
            return null;
        }

        reader.SkipBytes(remainingStorage);
        return name;
    }

    internal static void AddLayerTruncated(ImageMetadataDocument document, long offset, long length)
    {
        MetadataDiagnostic.AddTruncated(document.Diagnostics, "psd.layer-record.truncated", "Photoshop layer record was truncated.", "LayerAndMask", offset, length);
    }

    internal static MetadataProfile AddProfile(ImageMetadataDocument document, BoundedStreamReader reader, MetadataProfileKind kind, MetadataFamily family, string key, string name, string mimeType, long offset, long length, long limit, string limitCode, string blockType, bool retain)
    {
        MetadataProfile profile = CreateProfile(kind, family, key, name, mimeType, offset, length);
        if (retain && length <= limit && length <= 2147483647)
        {
            profile.Data = ReadRange(reader, offset, length);
        }
        else if (retain)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, limitCode, "Photoshop metadata profile exceeded its configured byte limit.", blockType, offset, length);
        }

        document.AddProfile(profile);
        return profile;
    }

    internal static MetadataProfile CreateProfile(MetadataProfileKind kind, MetadataFamily family, string key, string name, string mimeType, long offset, long length)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = kind;
        profile.Family = family;
        profile.Key = key;
        profile.Name = name;
        profile.MimeType = mimeType;
        profile.Offset = offset;
        profile.Length = length;
        return profile;
    }

    internal static void CaptureRawBlock(ImageMetadataDocument document, BoundedStreamReader reader, long offset, long length, string blockType, string name, ImageMetadataReaderOptions options)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }

        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Psd, blockType, name, offset, length);
        if (!CaptureRawBlockData(options))
        {
            return;
        }

        if (length > GetMaxRawBlockBytes(options) || length > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "psd.raw.block.limit", "Photoshop raw metadata block exceeded the configured raw block limit.", blockType, offset, length);
            return;
        }

        block.Data = ReadRange(reader, offset, length);
    }

    internal static byte[] ReadRange(BoundedStreamReader reader, long offset, long length)
    {
        if (reader == null || length < 0 || length > 2147483647 || !reader.SeekAbsolute(offset) || !reader.CanRead(length))
        {
            return new byte[0];
        }

        return reader.ReadBytes((int)length);
    }

    internal static bool SeekAndHasResourceSignature(BoundedStreamReader reader, long offset)
    {
        if (!reader.SeekAbsolute(offset) || !reader.CanRead(4))
        {
            return false;
        }

        string signature = MetadataStringReader.DecodeLatin1(reader.ReadBytes(4));
        return PhotoshopImageResourceParser.IsResourceSignature(signature);
    }

    internal static bool CanRead(BoundedStreamReader reader, long end, long length)
    {
        if (reader == null || length < 0 || reader.Position > end)
        {
            return false;
        }

        return length <= end - reader.Position && reader.CanRead(length);
    }

    internal static void SeekSectionEnd(BoundedStreamReader reader, PsdSection section)
    {
        if (reader != null && section != null)
        {
            reader.SeekAbsolute(section.DataOffset + section.AvailableLength);
        }
    }

    internal static long PadFour(long value)
    {
        long remainder = value & 3;
        return remainder == 0 ? value : value + 4 - remainder;
    }

    internal static ulong AddSaturated(ulong left, ulong right)
    {
        if (ulong.MaxValue - left < right)
        {
            return ulong.MaxValue;
        }

        return left + right;
    }

    internal static string ResolutionUnitName(int value)
    {
        if (value == 1) return "Pixels per inch";
        if (value == 2) return "Pixels per centimeter";
        return "Unknown";
    }

    internal static string DimensionUnitName(int value)
    {
        if (value == 1) return "Inches";
        if (value == 2) return "Centimeters";
        if (value == 3) return "Points";
        if (value == 4) return "Picas";
        if (value == 5) return "Columns";
        return "Unknown";
    }

    internal static string BlendModeName(string value)
    {
        if (value == "pass") return "Pass Through";
        if (value == "norm") return "Normal";
        if (value == "diss") return "Dissolve";
        if (value == "dark") return "Darken";
        if (value == "mul ") return "Multiply";
        if (value == "lite") return "Lighten";
        if (value == "scrn") return "Screen";
        if (value == "over") return "Overlay";
        if (value == "diff") return "Difference";
        if (value == "hue ") return "Hue";
        if (value == "sat ") return "Saturation";
        if (value == "colr") return "Color";
        if (value == "lum ") return "Luminosity";
        return value;
    }

    internal static long GetMaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }

    internal static long GetMaxXmlBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxXmlBytes : options.MaxXmlBytes;
    }

    internal static long GetMaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxRawBlockBytes : options.MaxRawBlockBytes;
    }

    internal static int GetMaxTagCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        return options == null || options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        return options != null && options.CaptureRawBlockData;
    }

    internal static bool ShouldParseXmp(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseXmp;
    }

    internal static bool ShouldParseIcc(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseIcc;
    }
}
