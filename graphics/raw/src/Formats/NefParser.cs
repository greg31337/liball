namespace raw;

internal class NefRawImage
{
    internal TiffImageDescriptor Descriptor;
    internal int PlaneIndex;

    internal NefRawImage(TiffImageDescriptor descriptor, int planeIndex)
    {
        Descriptor = descriptor;
        PlaneIndex = planeIndex;
    }
}

internal class NefStructure
{
    internal NefRawImage[] RawImages;

    internal NefStructure(NefRawImage[] rawImages)
    {
        RawImages = rawImages;
    }
}

internal static class NefParser
{
    internal static NefStructure Parse(TiffIfdGraph graph)
    {
        if (graph == null || graph.Header == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "NEF requires a TIFF IFD graph.", 0);
        }
        System.Collections.Generic.List<NefRawImage> rawImages = new System.Collections.Generic.List<NefRawImage>();
        int index = 0;
        while (index < graph.Images.Length)
        {
            TiffImageDescriptor descriptor = graph.Images[index];
            if (IsRawImage(descriptor))
            {
                rawImages.Add(new NefRawImage(descriptor, rawImages.Count));
            }
            index++;
        }
        if (rawImages.Count == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The identified NEF does not contain a structurally supported raw IFD.", graph.Header.BaseOffset);
        }
        return new NefStructure(rawImages.ToArray());
    }

    internal static bool IsRawImage(TiffImageDescriptor descriptor)
    {
        if (descriptor == null || descriptor.Ifd == null || descriptor.Width == 0 || descriptor.Height == 0 || descriptor.Blocks.Length == 0)
        {
            return false;
        }
        if (descriptor.Photometric == RawPhotometricInterpretation.ColorFilterArray || descriptor.Photometric == RawPhotometricInterpretation.LinearRaw || descriptor.CompressionCode == 34713)
        {
            return true;
        }
        if (!descriptor.ReducedResolution && descriptor.SamplesPerPixel == 1 && (descriptor.Photometric == RawPhotometricInterpretation.BlackIsZero || descriptor.Photometric == RawPhotometricInterpretation.WhiteIsZero) && descriptor.BitsPerSample.Length != 0 && descriptor.BitsPerSample[0] > 8)
        {
            return true;
        }
        if (!descriptor.ReducedResolution && descriptor.SamplesPerPixel == 3 && (descriptor.Photometric == RawPhotometricInterpretation.Rgb || descriptor.Photometric == RawPhotometricInterpretation.YCbCr) && descriptor.BitsPerSample.Length != 0 && descriptor.BitsPerSample[0] > 8)
        {
            return true;
        }
        return descriptor.Ifd.FindEntry(33421) != null && descriptor.Ifd.FindEntry(33422) != null;
    }

    internal static RawImageKind GetImageKind(NefStructure structure, TiffImageDescriptor descriptor)
    {
        NefRawImage? raw = FindRawImage(structure, descriptor);
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

    internal static NefRawImage? FindRawImage(NefStructure structure, TiffImageDescriptor descriptor)
    {
        if (structure == null || descriptor == null || structure.RawImages == null)
        {
            return null;
        }
        int index = 0;
        while (index < structure.RawImages.Length)
        {
            NefRawImage raw = structure.RawImages[index];
            if (raw != null && raw.Descriptor == descriptor)
            {
                return raw;
            }
            index++;
        }
        return null;
    }

    internal static void MarkRawPayloads(System.Collections.Generic.List<RawPayloadBlock> payloads, int firstPayload, int planeIndex)
    {
        int index = firstPayload;
        while (index < payloads.Count)
        {
            RawPayloadBlock old = payloads[index];
            payloads[index] = new RawPayloadBlock(RawPayloadKind.RawImage, old.Path, old.Offset, old.Length, old.ImageIndex, planeIndex, old.BlockIndex, old.Compression, old.Decodable, old.PreserveByteExact);
            index++;
        }
    }

    internal static void AddCaptureEditPayloads(System.Collections.Generic.List<RawPayloadBlock> payloads, RawMetadata? metadata, TiffIfdGraph graph, long sourceLength)
    {
        if (metadata == null || metadata.MakerNotes == null)
        {
            return;
        }
        int noteIndex = 0;
        while (noteIndex < metadata.MakerNotes.Length)
        {
            RawMakerNote note = metadata.MakerNotes[noteIndex];
            if (note != null && note.Signature == "NikonEmbeddedTiff" && note.NikonTags != null)
            {
                int tagIndex = 0;
                while (tagIndex < note.NikonTags.Length)
                {
                    RawNikonMakerNoteTag tag = note.NikonTags[tagIndex];
                    if (tag != null && IsCaptureEditTag(tag.Id) && tag.Value != null && tag.Value.RawBytes.Length > 0)
                    {
                        long absolute = CheckedMath.Add(note.Offset, tag.ValuePosition);
                        long offset = absolute - graph.ContainerStart;
                        long length = tag.Value.RawBytes.LongLength;
                        if (offset < 0 || offset > sourceLength || length > sourceLength - offset)
                        {
                            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "A Nikon Capture/Edit payload exceeds the bounded NEF source.", absolute);
                        }
                        string path = note.IfdPath + "/MakerNote" + noteIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + tag.Name;
                        payloads.Add(new RawPayloadBlock(RawPayloadKind.Metadata, path, offset, length, -1, -1, -1, RawCompression.None, false, true));
                    }
                    tagIndex++;
                }
            }
            noteIndex++;
        }
    }

    internal static bool IsCaptureEditTag(ushort id)
    {
        return id == 0x0E01 || id == 0x0E09 || id == 0x0E0E || id == 0x0E13 || id == 0x0E1E;
    }
}
