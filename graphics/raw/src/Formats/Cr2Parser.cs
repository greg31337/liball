namespace raw;

internal class Cr2Structure
{
    internal RawCr2Header? Header;
    internal TiffImageDescriptor? RawImage;
    internal Cr2RawImage[] RawImages;
    internal long ThumbnailOffset;
    internal int ThumbnailLength;
    internal int ThumbnailWidth;
    internal int ThumbnailHeight;
    internal RawDiagnostic[] Diagnostics;

    internal Cr2Structure()
    {
        RawImages = System.Array.Empty<Cr2RawImage>();
        ThumbnailOffset = -1;
        Diagnostics = System.Array.Empty<RawDiagnostic>();
    }
}

internal class Cr2RawImage
{
    internal TiffImageDescriptor? Descriptor;
    internal int ImageIndex;
    internal int PlaneIndex;

    internal Cr2RawImage(TiffImageDescriptor? descriptor, int imageIndex, int planeIndex)
    {
        Descriptor = descriptor;
        ImageIndex = imageIndex;
        PlaneIndex = planeIndex;
    }
}

internal static class Cr2Parser
{
    internal static Cr2Structure Parse(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, RawOpenOptions options)
    {
        if (source.Length < 16)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The CR2 header is truncated.", source.Length);
        }
        if (graph == null || graph.Header == null || graph.Header.BigTiff)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "CR2 requires a classic TIFF container.", 0);
        }
        System.ReadOnlySpan<byte> bytes = source.Span;
        if (bytes[8] != 0x43 || bytes[9] != 0x52)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidSignature, "The CR2 header signature is invalid.", 8);
        }
        int major = bytes[10];
        int minor = bytes[11];
        if (major != 2 || minor != 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The CR2 header version is not supported.", 10);
        }
        uint rawOffsetValue = EndianReader.ReadUInt32(bytes.Slice(12, 4), graph.Header.ByteOrder);
        long rawOffset = rawOffsetValue;
        if (rawOffset == 0 || rawOffset >= source.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "The CR2 raw IFD offset is outside the bounded source.", 12);
        }
        TiffIfd? rawIfd = graph.FindIfd("IFD3");
        if (rawIfd == null || rawIfd.Offset != rawOffsetValue)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The CR2 header must identify the fourth root TIFF directory as its raw IFD.", rawOffset);
        }
        TiffImageDescriptor? rawImage = FindImage(graph, rawIfd);
        if (rawImage == null && rawIfd.FindEntry(256) == null && rawIfd.FindEntry(257) == null)
        {
            rawImage = CreateDimensionlessRawImage(source, graph, rawIfd, options);
        }
        ValidateRawIfd(rawIfd, rawImage, rawOffset);

        Cr2Structure result = new Cr2Structure();
        result.Header = new RawCr2Header(graph.Header.ByteOrder, major, minor, CheckedMath.ToInt64(graph.Header.FirstIfdOffset), rawOffset, rawIfd.Path);
        result.RawImage = rawImage;
        result.RawImages = DiscoverRawImages(graph, rawImage);
        System.Collections.Generic.List<RawDiagnostic> diagnostics = new System.Collections.Generic.List<RawDiagnostic>();
        DiscoverThumbnail(source, graph, options, result, diagnostics);
        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    internal static TiffImageDescriptor CreateDimensionlessRawImage(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, TiffIfd rawIfd, RawOpenOptions options)
    {
        if (rawIfd.FindEntry(259) == null || rawIfd.FindEntry(273) == null || rawIfd.FindEntry(279) == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "An early dimensionless CR2 raw IFD requires compression and strip offsets.", rawIfd.AbsoluteOffset);
        }
        TiffImageDescriptor descriptor = TiffImageData.DescribeIfd(rawIfd, graph.Images.Length, options.Limits);
        if (descriptor.CompressionCode != 6 || descriptor.Blocks.Length != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The reviewed dimensionless CR2 layout requires one lossless-JPEG strip.", rawIfd.AbsoluteOffset);
        }
        TiffDataBlock block = descriptor.Blocks[0];
        RawReadProfile profile = RawBuiltInProfileCatalogue.CreateCanonEos20d();
        CanonEarlyRawHeader header = CanonEarlyRawLayout.Inspect(source, CheckedMath.ToInt64(block.Offset), CheckedMath.ToInt64(block.ByteCount), profile, options.Limits, options.CancellationToken);
        descriptor.Width = (ulong)header.StoredWidth;
        descriptor.Height = (ulong)header.StoredHeight;
        descriptor.BitsPerSample = new ulong[] { (ulong)header.Precision };
        descriptor.SamplesPerPixel = 1;
        descriptor.PhotometricCode = 32803;
        descriptor.Photometric = RawPhotometricInterpretation.ColorFilterArray;
        TiffImageDescriptor[] images = new TiffImageDescriptor[graph.Images.Length + 1];
        System.Array.Copy(graph.Images, images, graph.Images.Length);
        images[images.Length - 1] = descriptor;
        graph.Images = images;
        return descriptor;
    }

    internal static RawImageKind GetImageKind(Cr2Structure structure, TiffImageDescriptor descriptor)
    {
        Cr2RawImage? raw = FindRawImage(structure, descriptor);
        if (raw != null)
        {
            return raw.PlaneIndex == 0 ? RawImageKind.Raw : RawImageKind.Auxiliary;
        }
        if (descriptor.TransparencyMask)
        {
            return RawImageKind.Mask;
        }
        if (descriptor.ReducedResolution)
        {
            return RawImageKind.Thumbnail;
        }
        return RawImageKind.Preview;
    }

    internal static void MarkRawPayloads(System.Collections.Generic.List<RawPayloadBlock> payloads, int firstPayload, int planeIndex)
    {
        int index = firstPayload;
        while (index < payloads.Count)
        {
            RawPayloadBlock old = payloads[index];
            payloads[index] = new RawPayloadBlock(RawPayloadKind.RawImage, old.Path, old.Offset, old.Length, old.ImageIndex, planeIndex, old.BlockIndex, RawCompression.LosslessJpeg, false, old.PreserveByteExact);
            index++;
        }
    }

    internal static Cr2RawImage? FindRawImage(Cr2Structure structure, TiffImageDescriptor descriptor)
    {
        if (structure == null || descriptor == null || structure.RawImages == null)
        {
            return null;
        }
        int index = 0;
        while (index < structure.RawImages.Length)
        {
            Cr2RawImage raw = structure.RawImages[index];
            if (raw != null && raw.Descriptor == descriptor)
            {
                return raw;
            }
            index++;
        }
        return null;
    }

    internal static Cr2RawImage[] DiscoverRawImages(TiffIfdGraph graph, TiffImageDescriptor? primary)
    {
        System.Collections.Generic.List<Cr2RawImage> images = new System.Collections.Generic.List<Cr2RawImage>();
        images.Add(new Cr2RawImage(primary, 0, 0));
        TiffImageDescriptor? groupPrimary = primary;
        int imageIndex = 0;
        int planeIndex = 1;
        int rootIndex = 4;
        TiffIfd? ifd = graph.FindIfd("IFD" + rootIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
        while (ifd != null)
        {
            TiffImageDescriptor? candidate = FindImage(graph, ifd);
            if (!IsAdditionalRawImage(candidate))
            {
                break;
            }
            ValidateRawIfd(ifd, candidate, ifd.AbsoluteOffset);
            if (SameRawPlaneLayout(groupPrimary, candidate))
            {
                images.Add(new Cr2RawImage(candidate, imageIndex, planeIndex));
                planeIndex++;
            }
            else
            {
                imageIndex++;
                planeIndex = 1;
                groupPrimary = candidate;
                images.Add(new Cr2RawImage(candidate, imageIndex, 0));
            }
            rootIndex++;
            ifd = graph.FindIfd("IFD" + rootIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return images.ToArray();
    }

    internal static bool IsAdditionalRawImage(TiffImageDescriptor? descriptor)
    {
        if (descriptor == null || descriptor.Ifd == null || descriptor.CompressionCode != 6 || descriptor.Width == 0 || descriptor.Height == 0)
        {
            return false;
        }
        ulong[] slices = TiffImageData.GetUnsignedValues(descriptor.Ifd.FindEntry(50752));
        ulong[] rawTypes = TiffImageData.GetUnsignedValues(descriptor.Ifd.FindEntry(50885));
        return slices.Length == 3 && rawTypes.Length == 1 && (rawTypes[0] == 1 || rawTypes[0] == 4);
    }

    internal static bool SameRawPlaneLayout(TiffImageDescriptor? first, TiffImageDescriptor? second)
    {
        if (first == null || second == null || first.Width != second.Width || first.Height != second.Height || first.CompressionCode != second.CompressionCode || first.SamplesPerPixel != second.SamplesPerPixel || !SameUnsignedValues(first.BitsPerSample, second.BitsPerSample))
        {
            return false;
        }
        ulong[] firstSlices = TiffImageData.GetUnsignedValues(first.Ifd.FindEntry(50752));
        ulong[] secondSlices = TiffImageData.GetUnsignedValues(second.Ifd.FindEntry(50752));
        ulong[] firstTypes = TiffImageData.GetUnsignedValues(first.Ifd.FindEntry(50885));
        ulong[] secondTypes = TiffImageData.GetUnsignedValues(second.Ifd.FindEntry(50885));
        return SameUnsignedValues(firstSlices, secondSlices) && SameUnsignedValues(firstTypes, secondTypes);
    }

    internal static bool SameUnsignedValues(ulong[] first, ulong[] second)
    {
        if (first == null || second == null || first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static RawDiagnostic[] CombineDiagnostics(RawDiagnostic[] first, RawDiagnostic[] second)
    {
        RawDiagnostic[] combined = new RawDiagnostic[first.Length + second.Length];
        System.Array.Copy(first, 0, combined, 0, first.Length);
        System.Array.Copy(second, 0, combined, first.Length, second.Length);
        return combined;
    }

    internal static RawThumbnail? CreateThumbnail(System.ReadOnlyMemory<byte> source, Cr2Structure structure, System.Collections.Generic.List<RawPayloadBlock> payloads, int thumbnailIndex)
    {
        if (structure.ThumbnailOffset < 0)
        {
            return null;
        }
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.Thumbnail, "IFD1/JPEGInterchange", structure.ThumbnailOffset, structure.ThumbnailLength, -1, -1, -1, RawCompression.BaselineJpeg, true, true);
        payloads.Add(payload);
        byte[] encoded = source.Slice((int)structure.ThumbnailOffset, structure.ThumbnailLength).ToArray();
        return new RawThumbnail(thumbnailIndex, RawThumbnailKind.Jpeg, structure.ThumbnailWidth, structure.ThumbnailHeight, RawOrientation.Unknown, "image/jpeg", encoded, payload, null);
    }

    internal static TiffImageDescriptor? FindImage(TiffIfdGraph graph, TiffIfd ifd)
    {
        int index = 0;
        while (index < graph.Images.Length)
        {
            if (graph.Images[index].Ifd == ifd)
            {
                return graph.Images[index];
            }
            index++;
        }
        return null;
    }

    internal static void ValidateRawIfd(TiffIfd ifd, TiffImageDescriptor? image, long offset)
    {
        if (image == null || ifd.FindEntry(259) == null || ifd.FindEntry(273) == null || ifd.FindEntry(279) == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The CR2 raw IFD is missing required image or strip fields.", offset);
        }
        if (image.Width == 0 || image.Height == 0 || image.CompressionCode != 6 || image.StripOffsets.Length == 0 || image.StripOffsets.Length != image.StripByteCounts.Length || image.TileOffsets.Length != 0 || image.TileByteCounts.Length != 0 || image.Blocks.Length == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The CR2 raw IFD must describe nonempty lossless-JPEG strips.", offset);
        }
    }

    internal static void DiscoverThumbnail(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, RawOpenOptions options, Cr2Structure result, System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        TiffIfd? ifd = graph.FindIfd("IFD1");
        if (ifd == null)
        {
            return;
        }
        TiffEntry? offsetEntry = ifd.FindEntry(513);
        TiffEntry? lengthEntry = ifd.FindEntry(514);
        if (offsetEntry == null && lengthEntry == null)
        {
            return;
        }
        ulong[] offsets = TiffImageData.GetUnsignedValues(offsetEntry);
        ulong[] lengths = TiffImageData.GetUnsignedValues(lengthEntry);
        if (offsets.Length != 1 || lengths.Length != 1 || offsets[0] == 0 || lengths[0] == 0 || offsets[0] > int.MaxValue || lengths[0] > int.MaxValue || offsets[0] > (ulong)source.Length || lengths[0] > (ulong)source.Length - offsets[0])
        {
            HandleOptionalThumbnailFailure(options, ifd, diagnostics);
            return;
        }
        int offset = (int)offsets[0];
        int length = (int)lengths[0];
        JpegDocument jpeg = JpegParser.Parse(source.Slice(offset, length), options.Limits, options.CancellationToken);
        if (jpeg.Frames.Count != 1 || jpeg.Frames[0].Width <= 0 || jpeg.Frames[0].Height <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The CR2 IFD1 JPEG thumbnail has invalid frame structure.", offset);
        }
        result.ThumbnailOffset = offset;
        result.ThumbnailLength = length;
        result.ThumbnailWidth = jpeg.Frames[0].Width;
        result.ThumbnailHeight = jpeg.Frames[0].Height;
    }

    internal static void HandleOptionalThumbnailFailure(RawOpenOptions options, TiffIfd ifd, System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        if (!options.Tolerant)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The CR2 IFD1 JPEG thumbnail offset and length must form one valid pair.", ifd.AbsoluteOffset);
        }
        diagnostics.Add(new RawDiagnostic(RawDiagnosticCode.RecoveryApplied, RawSeverity.Warning, RawFormat.Cr2, ifd.AbsoluteOffset, ifd.Path, 513, "Cr2Parser", "The malformed optional IFD1 JPEG thumbnail was omitted.", true));
    }
}
