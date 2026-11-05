using System;

namespace raw;

internal static class NefWriteProfileQualification
{
    internal static void Require(RawWriteProfile profile, IRawProfileCatalogue catalogue)
    {
        bool builtInUncompressed = profile.Id == RawBuiltInProfileCatalogue.NikonZ7UncompressedWriteProfile && profile.Codec != null && profile.Codec.Name == NefUncompressedDecoder.CodecName;
        bool builtInLossless = profile.Id == RawBuiltInProfileCatalogue.NikonZ8WriteProfile && profile.Codec != null && profile.Codec.Name == NikonCompressionCodec.LosslessCodecName;
        if (builtInUncompressed || builtInLossless)
        {
            return;
        }
        if (profile.ReadProfileId == null || catalogue.FindReadProfile(profile.ReadProfileId) == null)
        {
            throw Unsupported("An additional NEF write profile must reference a registered read profile.");
        }
        RawProfileEvidence? structural = null;
        RawProfileEvidence? application = null;
        int index = 0;
        while (profile.Evidence != null && index < profile.Evidence.Length)
        {
            RawProfileEvidence evidence = profile.Evidence[index];
            if (evidence != null && evidence.Result == RawProfileEvidenceResult.Passed)
            {
                if (evidence.Kind == RawProfileEvidenceKind.Structural && structural == null)
                {
                    structural = evidence;
                }
                else if (evidence.Kind == RawProfileEvidenceKind.Application && application == null)
                {
                    application = evidence;
                }
            }
            index++;
        }
        if (structural == null || application == null || structural.Tool == null || application.Tool == null || application.Version == null || application.Version.Length == 0 || RawProfileMatcher.NormalizeIdentityText(structural.Tool) == RawProfileMatcher.NormalizeIdentityText(application.Tool))
        {
            throw Unsupported("An additional NEF write profile requires passed structural and application checks from independently named tools.");
        }
        if (profile.Fingerprint == null || !NefEncoder.IsSupportedProfile(profile) || profile.Fingerprint.MakerNoteSignature != "NikonEmbeddedTiff" || !Cr2WriteProfileQualification.Contains(profile.Fingerprint.RequiredIfds, "IFD0") || !Cr2WriteProfileQualification.Contains(profile.Fingerprint.RequiredIfds, "IFD0/Exif") || !Cr2WriteProfileQualification.Contains(profile.Fingerprint.RequiredIfds, "IFD0/SubIFD0") || !Cr2WriteProfileQualification.Contains(profile.Fingerprint.RequiredIfds, "IFD0/SubIFD1") || !Cr2WriteProfileQualification.Contains(profile.Fingerprint.RequiredTags, "IFD0/Exif/37500"))
        {
            throw Unsupported("An additional NEF profile must use a supported codec and retain the reviewed raw SubIFD, preview, Exif, and Nikon maker-note requirements.");
        }
    }

    internal static RawUnsupportedException Unsupported(string message)
    {
        return new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, message, -1);
    }
}

internal static class NefEncoder
{
    internal const ushort Uncompressed14MakerCode = 10;
    internal const ushort LosslessMakerCode = 3;
    internal const ushort LossyMakerCode = 4;

    internal static bool IsSupportedProfile(RawWriteProfile? profile)
    {
        if (profile == null || profile.Identity == null || profile.Identity.Format != RawFormat.Nef || profile.Fingerprint == null || profile.Geometry == null || profile.Geometry.Cfa == null || profile.Codec == null)
        {
            return false;
        }
        bool uncompressed = profile.Codec.Name == NefUncompressedDecoder.CodecName && profile.Codec.Compression == 1 && profile.Codec.Packing == RawPacking.Raw14;
        bool lossless = profile.Codec.Name == NikonCompressionCodec.LosslessCodecName && profile.Codec.Compression == 34713 && profile.Codec.Packing == RawPacking.PackedBits;
        bool lossy = profile.Codec.Name == NikonCompressionCodec.LossyCodecName && profile.Codec.Compression == 34713 && profile.Codec.Packing == RawPacking.PackedBits && HasLossyQuirks(profile.Codec.Quirks);
        return (uncompressed || lossless || lossy) && profile.Fingerprint.BitDepth == 14 && profile.Fingerprint.Compression == profile.Codec.Compression && profile.Codec.StoredWordBits == 14 && profile.Codec.Predictor == 0 && profile.Codec.PlanarConfiguration == 1 && profile.Codec.RowAlignment == 1 && profile.Codec.SliceWidths != null && profile.Codec.SliceWidths.Length == 1;
    }

    internal static bool IsLossyProfile(RawWriteProfile? profile)
    {
        return profile != null && profile.Codec != null && profile.Codec.Name == NikonCompressionCodec.LossyCodecName;
    }

    internal static bool HasLossyQuirks(string[] quirks)
    {
        return Cr2WriteProfileQualification.Contains(quirks, NikonCompressionCodec.LossyTableQuirk) && Cr2WriteProfileQualification.Contains(quirks, NikonCompressionCodec.LossyCurveQuirk) && Cr2WriteProfileQualification.Contains(quirks, NikonCompressionCodec.LossyQuantizationQuirk);
    }

    internal static byte[] Encode(RawNefWriteInput input, RawWriteProfile profile, RawWriteOptions options)
    {
        Validate(input, profile, options);
        NikonLosslessEncoding? lossless = null;
        NikonLossyEncoding? lossy = null;
        byte[] raw;
        if (profile.Codec.Name == NikonCompressionCodec.LosslessCodecName)
        {
            lossless = NikonCompressionCodec.EncodeLossless(input.StoredSamples, input.Width, input.Height, input.RowStrideSamples, profile.Fingerprint.BitDepth, profile.Fingerprint.ByteOrder, options.Limits, options.CancellationToken);
            raw = lossless.EntropyBytes;
        }
        else if (profile.Codec.Name == NikonCompressionCodec.LossyCodecName)
        {
            lossy = NikonCompressionCodec.EncodeLossy(input.StoredSamples, input.Width, input.Height, input.RowStrideSamples, profile.Fingerprint.BitDepth, profile.Fingerprint.ByteOrder, options.Limits, options.CancellationToken);
            raw = lossy.EntropyBytes;
        }
        else
        {
            raw = EncodeRaw(input, profile, options);
        }
        byte[] compressionInfo = lossless != null ? lossless.CompressionInfo : lossy != null ? lossy.CompressionInfo : System.Array.Empty<byte>();
        byte[] makerNote = CreateNikonMakerNote(input, profile, compressionInfo);
        TiffLayoutRequest request = CreateRequest(input, profile, raw, makerNote);
        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, options.Limits, options.CancellationToken);
        byte[] output = TiffWriter.WriteToArray(plan, options.Limits, options.CancellationToken);
        ValidateOutput(output, input, profile, options);
        return output;
    }

    internal static RawWritePlan Plan(RawNefWriteInput input, RawWriteProfile profile, RawWriteOptions? options)
    {
        Validate(input, profile, options);
        NikonLosslessEncoding? losslessEncoding = null;
        NikonLossyEncoding? lossyEncoding = null;
        byte[] raw;
        if (profile.Codec.Name == NikonCompressionCodec.LosslessCodecName)
        {
            losslessEncoding = NikonCompressionCodec.EncodeLossless(input.StoredSamples, input.Width, input.Height, input.RowStrideSamples, profile.Fingerprint.BitDepth, profile.Fingerprint.ByteOrder, (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).Limits, options.CancellationToken);
            raw = losslessEncoding.EntropyBytes;
        }
        else if (profile.Codec.Name == NikonCompressionCodec.LossyCodecName)
        {
            lossyEncoding = NikonCompressionCodec.EncodeLossy(input.StoredSamples, input.Width, input.Height, input.RowStrideSamples, profile.Fingerprint.BitDepth, profile.Fingerprint.ByteOrder, (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).Limits, options.CancellationToken);
            raw = lossyEncoding.EntropyBytes;
        }
        else
        {
            raw = EncodeRaw(input, profile, options);
        }
        byte[] compressionInfo = losslessEncoding != null ? losslessEncoding.CompressionInfo : lossyEncoding != null ? lossyEncoding.CompressionInfo : System.Array.Empty<byte>();
        byte[] makerNote = CreateNikonMakerNote(input, profile, compressionInfo);
        TiffLayoutPlan layout = TiffLayoutPlanner.Plan(CreateRequest(input, profile, raw, makerNote), (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).Limits, options.CancellationToken);
        TiffLayoutBlockPlan rawBlock = TiffLayoutPlanner.FindBlock(layout, "raw-subifd0");
        TiffLayoutBlockPlan previewBlock = TiffLayoutPlanner.FindBlock(layout, "preview-subifd1");
        bool lossy = IsLossyProfile(profile);
        bool lossless = profile.Codec.Name == NikonCompressionCodec.LosslessCodecName;
        string rawDescription = lossy ? "Re-encode stored samples with Nikon lossy Huffman tree 3, no split row, and two-to-one curve-index quantization; odd values round upward and the maximum absolute stored-sample error is 1." : lossless ? "Encode stored samples with the selected lossless-compressed NEF codec." : "Pack stored samples with the selected uncompressed NEF codec.";
        RawWriteChange metadataChange = new RawWriteChange(RawWriteChangeKind.Added, "TIFF/Metadata", "Create the profile-defined NEF container, directories, Exif, and Nikon maker-note metadata.", -1, 0, 0, (long)previewBlock.Offset, false);
        RawWriteChange previewChange = new RawWriteChange(RawWriteChangeKind.Added, "IFD0/SubIFD1/Preview", "Add the caller-supplied encoded preview payload.", -1, 0, (long)previewBlock.Offset, previewBlock.Length, false);
        RawWriteChange rawChange = new RawWriteChange(RawWriteChangeKind.Reencoded, "IFD0/SubIFD0/Strip0", rawDescription, -1, 0, (long)rawBlock.Offset, rawBlock.Length, lossy);
        long sampleCount = CheckedMath.Multiply(input.Width, input.Height);
        if (!lossy)
        {
            RawWritePlan losslessPlan = new RawWritePlan(true, layout.PredictedSize, profile.Id, new RawWriteChange[] { metadataChange, previewChange, rawChange }, null);
            losslessPlan.PixelValidation = new RawPixelValidationReport(RawPixelValidationKind.Identity, options.VerifyPixelIdentity, false, false, sampleCount, -1, 0, "Reopen the lossless NEF and require every decoded stored sample to equal its source sample.");
            return losslessPlan;
        }
        int curveLength = (1 << (profile.Fingerprint.BitDepth - 1)) + 1;
        string curveDescription = $"Write Nikon maker-note tag 0x0096 version 0x44 with an explicit {curveLength}-entry monotonic curve mapping index i to min(2*i, 16383); quantization is irreversible and bounded to 1 stored-sample unit.";
        RawWriteChange curveChange = new RawWriteChange(RawWriteChangeKind.Added, "IFD0/Exif/MakerNote/0x0096", curveDescription, -1, 0, -1, 12 + curveLength * 2, true);
        RawWritePlan lossyPlan = new RawWritePlan(true, layout.PredictedSize, profile.Id, new RawWriteChange[] { metadataChange, curveChange, previewChange, rawChange }, null);
        lossyPlan.PixelValidation = new RawPixelValidationReport(RawPixelValidationKind.ErrorBound, true, false, false, sampleCount, -1, (lossyEncoding ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaximumError, "Reopen the explicitly lossy NEF and require every decoded stored sample to remain within the declared absolute-error bound.");
        return lossyPlan;
    }

    internal static void Validate(RawNefWriteInput input, RawWriteProfile profile, RawWriteOptions? options)
    {
        if (input == null || profile == null || options == null || options.Limits == null || options.Mode != RawWriteMode.NewContainer)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A complete new-container NEF request and valid write options are required.", -1);
        }
        if (!IsSupportedProfile(profile))
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidProfile, "The selected NEF profile does not define one reviewed packed, lossless-compressed, or explicitly quantified lossy 14-bit Bayer strip.", -1);
        }
        if (IsLossyProfile(profile) && (!options.AllowLossy || !options.DataLossConsent))
        {
            throw new RawWriteException(RawDiagnosticCode.DataLossConsentRequired, "Lossy NEF encoding requires AllowLossy and explicit data-loss consent.", -1);
        }
        if (!options.Validate().IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A complete new-container NEF request and valid write options are required.", -1);
        }
        if (input.Width != profile.Geometry.StoredWidth || input.Height != profile.Geometry.StoredHeight || input.Width <= 0 || input.Height <= 0 || (input.Width & 1) != 0 || (input.Height & 1) != 0 || profile.Codec.SliceWidths[0] != input.Width || input.RowStrideSamples < input.Width || input.StoredSamples == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "NEF samples must exactly match the selected even Bayer profile geometry and row stride.", -1);
        }
        long required = CheckedMath.Add(CheckedMath.Multiply(input.Height - 1, input.RowStrideSamples), input.Width);
        if (input.StoredSamples.LongLength < required)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "The NEF stored-sample buffer is shorter than its declared geometry.", -1);
        }
        CheckedMath.PixelCount(input.Width, input.Height, options.Limits);
        CheckedMath.DecodedSize(CheckedMath.Multiply(input.Width, input.Height), 4, options.Limits);
        ValidateSamples(input, profile.Fingerprint.BitDepth, options.CancellationToken);
        Cr2Encoder.ValidatePreview(input.Preview, options, "NEF SubIFD preview");
        if (input.Metadata == null || !Cr2Encoder.RequiredAscii(input.Metadata.Software) || !Cr2Encoder.OptionalAscii(input.Metadata.Artist) || !Cr2Encoder.OptionalAscii(input.Metadata.Copyright) || input.Metadata.OriginalTime == null || !input.Metadata.OriginalTime.Valid || !Cr2Encoder.RequiredAscii(input.Metadata.WhiteBalance) || input.Metadata.IsoSensitivity == 0)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "Caller-supplied ASCII software, optional creator/rights text, timestamp, Nikon white balance, and ISO are required.", -1);
        }
    }

    internal static void ValidateSamples(RawNefWriteInput input, int precision, System.Threading.CancellationToken cancellationToken)
    {
        ushort maximum = (ushort)((1 << precision) - 1);
        int y = 0;
        while (y < input.Height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int row = y * input.RowStrideSamples;
            int x = 0;
            while (x < input.Width)
            {
                if (input.StoredSamples[row + x] > maximum)
                {
                    throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A NEF stored sample exceeds the selected profile precision.", -1);
                }
                x++;
            }
            y++;
        }
    }

    internal static byte[] EncodeRaw(RawNefWriteInput input, RawWriteProfile profile, RawWriteOptions? options)
    {
        int rowBytes = BitPacking.GetNamedPackedByteCount(input.Width, profile.Codec.Packing);
        int length = CheckedMath.ToInt32(CheckedMath.Multiply(rowBytes, input.Height));
        CheckedMath.AllocationSize(length, 1, (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).Limits);
        CheckedMath.AllocationSize(input.Width, 4, options.Limits);
        byte[] output = new byte[length];
        uint[] row = new uint[input.Width];
        int y = 0;
        while (y < input.Height)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            int x = 0;
            while (x < input.Width)
            {
                row[x] = input.StoredSamples[y * input.RowStrideSamples + x];
                x++;
            }
            BitPacking.PackNamed(row, profile.Codec.Packing, output.AsSpan(y * rowBytes, rowBytes));
            y++;
        }
        return output;
    }

    internal static TiffLayoutRequest CreateRequest(RawNefWriteInput input, RawWriteProfile profile, byte[] raw, byte[] makerNote)
    {
        RawByteOrder byteOrder = profile.Fingerprint.ByteOrder;
        System.Collections.Generic.List<TiffLayoutEntryInput> ifd0 = new System.Collections.Generic.List<TiffLayoutEntryInput>();
        ifd0.Add(Cr2Encoder.AsciiEntry(271, profile.Identity.Make));
        ifd0.Add(Cr2Encoder.AsciiEntry(272, profile.Identity.Model));
        ifd0.Add(Cr2Encoder.ShortEntry(274, 1, byteOrder));
        ifd0.Add(Cr2Encoder.AsciiEntry(305, input.Metadata.Software));
        if (input.Metadata.Artist.Length != 0) ifd0.Add(Cr2Encoder.AsciiEntry(315, input.Metadata.Artist));
        if (input.Metadata.Copyright.Length != 0) ifd0.Add(Cr2Encoder.AsciiEntry(33432, input.Metadata.Copyright));
        ifd0.Add(LongArrayEntry(330, new uint[] { 0, 0 }, byteOrder, new TiffLayoutReference[]
        {
            new TiffLayoutReference(0, TiffLayoutReferenceKind.Ifd, "IFD0/SubIFD0"),
            new TiffLayoutReference(1, TiffLayoutReferenceKind.Ifd, "IFD0/SubIFD1")
        }));
        ifd0.Add(Cr2Encoder.LongEntry(34665, 0, byteOrder, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Ifd, "IFD0/Exif") }));

        System.Collections.Generic.List<TiffLayoutEntryInput> rawIfd = new System.Collections.Generic.List<TiffLayoutEntryInput>();
        rawIfd.Add(Cr2Encoder.LongEntry(254, 0, byteOrder, null));
        rawIfd.Add(Cr2Encoder.LongEntry(256, (uint)input.Width, byteOrder, null));
        rawIfd.Add(Cr2Encoder.LongEntry(257, (uint)input.Height, byteOrder, null));
        rawIfd.Add(Cr2Encoder.ShortEntry(258, 14, byteOrder));
        rawIfd.Add(Cr2Encoder.ShortEntry(259, (ushort)profile.Codec.Compression, byteOrder));
        rawIfd.Add(Cr2Encoder.ShortEntry(262, 32803, byteOrder));
        rawIfd.Add(Cr2Encoder.LongEntry(273, 0, byteOrder, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "raw-subifd0") }));
        rawIfd.Add(Cr2Encoder.ShortEntry(277, 1, byteOrder));
        rawIfd.Add(Cr2Encoder.LongEntry(278, (uint)input.Height, byteOrder, null));
        rawIfd.Add(Cr2Encoder.LongEntry(279, (uint)raw.Length, byteOrder, null));
        rawIfd.Add(Cr2Encoder.ShortEntry(284, 1, byteOrder));
        rawIfd.Add(Cr2Encoder.ShortArrayEntry(33421, new ushort[] { 2, 2 }, byteOrder));
        rawIfd.Add(ByteArrayEntry(33422, new byte[] { 0, 1, 1, 2 }));
        RawArea? active = profile.Geometry.ActiveArea;
        rawIfd.Add(LongArrayEntry(50829, new uint[] { (uint)(active ?? throw new System.InvalidOperationException("A required value is unavailable.")).Y, (uint)active.X, (uint)(active.Y + active.Height), (uint)(active.X + active.Width) }, byteOrder, null));

        System.Collections.Generic.List<TiffLayoutEntryInput> preview = new System.Collections.Generic.List<TiffLayoutEntryInput>();
        preview.Add(Cr2Encoder.LongEntry(254, 1, byteOrder, null));
        preview.Add(Cr2Encoder.LongEntry(256, (uint)(input.Preview ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, byteOrder, null));
        preview.Add(Cr2Encoder.LongEntry(257, (uint)input.Preview.Height, byteOrder, null));
        preview.Add(Cr2Encoder.ShortEntry(259, 6, byteOrder));
        preview.Add(Cr2Encoder.ShortEntry(262, 2, byteOrder));
        preview.Add(Cr2Encoder.LongEntry(273, 0, byteOrder, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "preview-subifd1") }));
        preview.Add(Cr2Encoder.ShortEntry(277, 3, byteOrder));
        preview.Add(Cr2Encoder.LongEntry(278, (uint)input.Preview.Height, byteOrder, null));
        preview.Add(Cr2Encoder.LongEntry(279, (uint)input.Preview.EncodedBytes.Length, byteOrder, null));

        System.Collections.Generic.List<TiffLayoutEntryInput> exif = new System.Collections.Generic.List<TiffLayoutEntryInput>();
        exif.Add(Cr2Encoder.UndefinedEntry(36864, new byte[] { 0x30, 0x32, 0x33, 0x31 }));
        exif.Add(Cr2Encoder.AsciiEntry(36867, (input.Metadata.OriginalTime ?? throw new System.InvalidOperationException("A required value is unavailable.")).DateTimeText));
        if (input.Metadata.OriginalTime.HasOffset) exif.Add(Cr2Encoder.AsciiEntry(36881, input.Metadata.OriginalTime.OffsetText));
        if (input.Metadata.OriginalTime.Subseconds.Length != 0) exif.Add(Cr2Encoder.AsciiEntry(37521, input.Metadata.OriginalTime.Subseconds));
        exif.Add(Cr2Encoder.UndefinedEntry(37500, makerNote));
        exif.Add(Cr2Encoder.LongEntry(40962, (uint)active.Width, byteOrder, null));
        exif.Add(Cr2Encoder.LongEntry(40963, (uint)active.Height, byteOrder, null));

        return new TiffLayoutRequest(false, byteOrder, "IFD0", new TiffLayoutIfdInput[]
        {
            new TiffLayoutIfdInput("IFD0", ifd0.ToArray(), null),
            new TiffLayoutIfdInput("IFD0/Exif", exif.ToArray(), null),
            new TiffLayoutIfdInput("IFD0/SubIFD0", rawIfd.ToArray(), null),
            new TiffLayoutIfdInput("IFD0/SubIFD1", preview.ToArray(), null)
        }, new TiffLayoutBlockInput[]
        {
            new TiffLayoutBlockInput("preview-subifd1", TiffLayoutBlockKind.Strip, input.Preview.EncodedBytes.Length, input.Preview.EncodedBytes),
            new TiffLayoutBlockInput("raw-subifd0", TiffLayoutBlockKind.Strip, raw.Length, raw)
        });
    }

    internal static byte[] CreateNikonMakerNote(RawNefWriteInput input, RawWriteProfile profile, byte[] compressionInfo)
    {
        bool lossless = profile.Codec.Name == NikonCompressionCodec.LosslessCodecName;
        bool lossy = profile.Codec.Name == NikonCompressionCodec.LossyCodecName;
        bool compressed = lossless || lossy;
        RawByteOrder byteOrder = profile.Fingerprint.ByteOrder;
        byte[][] values = compressed ? new byte[][]
        {
            new byte[] { (byte)'0', (byte)'2', (byte)'0', (byte)'0' },
            Cr2Encoder.UInt16Bytes(new ushort[] { 0, input.Metadata.IsoSensitivity }, byteOrder),
            Cr2Encoder.AsciiBytes("RAW"),
            Cr2Encoder.AsciiBytes(input.Metadata.WhiteBalance),
            Cr2Encoder.UInt16Bytes(new ushort[] { lossless ? LosslessMakerCode : LossyMakerCode }, byteOrder),
            compressionInfo
        } : new byte[][]
        {
            new byte[] { (byte)'0', (byte)'2', (byte)'0', (byte)'0' },
            Cr2Encoder.UInt16Bytes(new ushort[] { 0, input.Metadata.IsoSensitivity }, byteOrder),
            Cr2Encoder.AsciiBytes("RAW"),
            Cr2Encoder.AsciiBytes(input.Metadata.WhiteBalance),
            Cr2Encoder.UInt16Bytes(new ushort[] { Uncompressed14MakerCode }, byteOrder)
        };
        ushort[] ids = compressed ? new ushort[] { 0x0001, 0x0002, 0x0004, 0x0005, 0x0093, 0x0096 } : new ushort[] { 0x0001, 0x0002, 0x0004, 0x0005, 0x0093 };
        TiffFieldType[] types = compressed ? new TiffFieldType[] { TiffFieldType.Undefined, TiffFieldType.Short, TiffFieldType.Ascii, TiffFieldType.Ascii, TiffFieldType.Short, TiffFieldType.Undefined } : new TiffFieldType[] { TiffFieldType.Undefined, TiffFieldType.Short, TiffFieldType.Ascii, TiffFieldType.Ascii, TiffFieldType.Short };
        uint[] counts = compressed ? new uint[] { 4, 2, (uint)values[2].Length, (uint)values[3].Length, 1, (uint)compressionInfo.Length } : new uint[] { 4, 2, (uint)values[2].Length, (uint)values[3].Length, 1 };
        int ifdPosition = 18;
        int headerLength = ifdPosition + 2 + ids.Length * 12 + 4;
        int length = headerLength;
        int sizeIndex = 0;
        while (sizeIndex < values.Length)
        {
            if (values[sizeIndex].Length > 4) length += values[sizeIndex].Length;
            sizeIndex++;
        }
        byte[] bytes = new byte[length];
        byte[] signature = System.Text.Encoding.ASCII.GetBytes("Nikon\0");
        signature.AsSpan().CopyTo(bytes);
        bytes[6] = 2;
        bytes[10] = byteOrder == RawByteOrder.LittleEndian ? (byte)'I' : (byte)'M';
        bytes[11] = bytes[10];
        EndianWriter.WriteUInt16(bytes.AsSpan(12, 2), 42, byteOrder);
        EndianWriter.WriteUInt32(bytes.AsSpan(14, 4), 8, byteOrder);
        EndianWriter.WriteUInt16(bytes.AsSpan(ifdPosition, 2), (ushort)ids.Length, byteOrder);
        int position = headerLength;
        int index = 0;
        while (index < ids.Length)
        {
            int entry = ifdPosition + 2 + index * 12;
            EndianWriter.WriteUInt16(bytes.AsSpan(entry, 2), ids[index], byteOrder);
            EndianWriter.WriteUInt16(bytes.AsSpan(entry + 2, 2), (ushort)types[index], byteOrder);
            EndianWriter.WriteUInt32(bytes.AsSpan(entry + 4, 4), counts[index], byteOrder);
            if (values[index].Length <= 4)
            {
                values[index].AsSpan().CopyTo(bytes.AsSpan(entry + 8, 4));
            }
            else
            {
                EndianWriter.WriteUInt32(bytes.AsSpan(entry + 8, 4), (uint)(position - 10), byteOrder);
                values[index].AsSpan().CopyTo(bytes.AsSpan(position));
                position += values[index].Length;
            }
            index++;
        }
        return bytes;
    }

    internal static RawPixelValidationReport ValidateOutput(byte[] output, RawNefWriteInput input, RawWriteProfile profile, RawWriteOptions options)
    {
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.AllowExperimentalProfiles = true;
        openOptions.ReadPreviews = true;
        RawDocument document = RawCodec.Open(output, null, openOptions);
        RawIfd? rawIfd = document.Metadata == null ? null : document.Metadata.FindIfd("IFD0/SubIFD0");
        RawIfd? exif = document.Metadata == null ? null : document.Metadata.FindIfd("IFD0/Exif");
        RawMakerNote? note = document.Metadata == null || document.Metadata.MakerNotes.Length != 1 ? null : document.Metadata.MakerNotes[0];
        RawImageInfo? rawInfo = FindRawImage(document);
        if (document.Format != RawFormat.Nef || document.TiffGraph == null || rawIfd == null || exif == null || rawInfo == null)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded NEF does not retain its required TIFF, raw SubIFD, and Exif structure.", -1);
        }
        if (document.TiffGraph.FindIfd("IFD0/SubIFD1") == null || document.Thumbnails.Length != 1)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded NEF does not retain its required SubIFD preview.", -1);
        }
        if (note == null || note.Signature != "NikonEmbeddedTiff" || note.OffsetBase != RawMakerNoteOffsetBase.EmbeddedTiffHeader || exif.FindTag(37500) == null)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded NEF does not retain its required Nikon type-3 maker note.", -1);
        }
        if (rawIfd.FindTag(278) == null || (rawIfd.FindTag(278) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt32Values()[0] != (uint)input.Height)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded NEF raw strip geometry changed during output planning.", -1);
        }
        bool builtIn = profile.Id == RawBuiltInProfileCatalogue.NikonZ7UncompressedWriteProfile || profile.Id == RawBuiltInProfileCatalogue.NikonZ8WriteProfile;
        if (builtIn && document.ProfileName != profile.ReadProfileId)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded NEF did not reopen through its selected read profile.", -1);
        }
        if (!options.VerifyPixelIdentity && !IsLossyProfile(profile))
        {
            long skippedCount = CheckedMath.Multiply(input.Width, input.Height);
            return new RawPixelValidationReport(RawPixelValidationKind.Identity, false, false, false, skippedCount, -1, 0, "Decoded stored-pixel identity verification was not requested.");
        }
        return VerifyPixels(document.ExtractPayload(rawInfo.Payload), input, profile, note, options);
    }

    internal static RawImageInfo? FindRawImage(RawDocument document)
    {
        int index = 0;
        while (document != null && document.Images != null && index < document.Images.Length)
        {
            if (document.Images[index] != null && document.Images[index].Kind == RawImageKind.Raw)
            {
                return document.Images[index];
            }
            index++;
        }
        return null;
    }

    internal static RawPixelValidationReport VerifyPixels(byte[] raw, RawNefWriteInput input, RawWriteProfile profile, RawMakerNote note, RawWriteOptions options)
    {
        long sampleCount = CheckedMath.Multiply(input.Width, input.Height);
        if (profile.Codec.Name == NikonCompressionCodec.LosslessCodecName)
        {
            VerifyLosslessPixels(raw, input, profile, note, options);
            return new RawPixelValidationReport(RawPixelValidationKind.Identity, true, true, true, sampleCount, 0, 0, "The reopened lossless-compressed NEF decoded to stored samples identical to the source.");
        }
        if (profile.Codec.Name == NikonCompressionCodec.LossyCodecName)
        {
            int maximumError = VerifyLossyPixels(raw, input, profile, note, options);
            return new RawPixelValidationReport(RawPixelValidationKind.ErrorBound, true, true, true, sampleCount, maximumError, NikonCompressionCodec.LossyMaximumError, "The reopened explicitly lossy NEF stayed within its declared stored-sample absolute-error bound.");
        }
        int rowBytes = BitPacking.GetNamedPackedByteCount(input.Width, profile.Codec.Packing);
        if (raw == null || raw.Length != CheckedMath.ToInt32(CheckedMath.Multiply(rowBytes, input.Height)))
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded NEF raw strip length changed during output planning.", -1);
        }
        CheckedMath.AllocationSize(input.Width, 4, options.Limits);
        uint[] decoded = new uint[input.Width];
        int y = 0;
        while (y < input.Height)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            BitPacking.UnpackNamed(raw.AsSpan(y * rowBytes, rowBytes), input.Width, profile.Codec.Packing, decoded);
            int x = 0;
            while (x < input.Width)
            {
                if (decoded[x] != input.StoredSamples[y * input.RowStrideSamples + x])
                {
                    throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded NEF failed stored-pixel identity verification.", -1);
                }
                x++;
            }
            y++;
        }
        return new RawPixelValidationReport(RawPixelValidationKind.Identity, true, true, true, sampleCount, 0, 0, "The reopened uncompressed NEF decoded to stored samples identical to the source.");
    }

    internal static void VerifyLosslessPixels(byte[] raw, RawNefWriteInput input, RawWriteProfile profile, RawMakerNote note, RawWriteOptions options)
    {
        ushort[] decoded = DecodeCompressedPixels(raw, input, profile, note, options, true);
        int y = 0;
        while (y < input.Height)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            int x = 0;
            while (x < input.Width)
            {
                if (decoded[y * input.Width + x] != input.StoredSamples[y * input.RowStrideSamples + x])
                {
                    throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded lossless NEF failed stored-pixel identity verification.", -1);
                }
                x++;
            }
            y++;
        }
    }

    internal static int VerifyLossyPixels(byte[] raw, RawNefWriteInput input, RawWriteProfile profile, RawMakerNote note, RawWriteOptions options)
    {
        ushort[] decoded = DecodeCompressedPixels(raw, input, profile, note, options, false);
        int maximum = (1 << profile.Fingerprint.BitDepth) - 1;
        int maximumError = 0;
        int y = 0;
        while (y < input.Height)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            int x = 0;
            while (x < input.Width)
            {
                int source = input.StoredSamples[y * input.RowStrideSamples + x];
                int expected = ((source + 1) / 2) * 2;
                if (expected > maximum)
                {
                    expected = maximum;
                }
                int actual = decoded[y * input.Width + x];
                int error = actual > source ? actual - source : source - actual;
                if (error > maximumError)
                {
                    maximumError = error;
                }
                if (actual != expected || error > NikonCompressionCodec.LossyMaximumError)
                {
                    throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded lossy NEF exceeded its declared stored-sample error bound.", -1);
                }
                x++;
            }
            y++;
        }
        return maximumError;
    }

    internal static ushort[] DecodeCompressedPixels(byte[] raw, RawNefWriteInput input, RawWriteProfile profile, RawMakerNote note, RawWriteOptions options, bool lossless)
    {
        RawNikonMakerNoteTag? compressionTag = note == null ? null : note.FindNikonTag(0x0096);
        if (raw == null || raw.Length == 0 || compressionTag == null)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded compressed NEF is missing entropy bytes or compression parameters.", -1);
        }
        NikonCompressionInfo compression = NikonCompressionCodec.ParseInfo(compressionTag.CopyBytes(), (note ?? throw new System.InvalidOperationException("A required value is unavailable.")).ByteOrder, profile.Fingerprint.BitDepth, input.Height);
        if (compression.Lossless != lossless)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded NEF compression parameters disagree with the selected write profile.", -1);
        }
        long count = CheckedMath.PixelCount(input.Width, input.Height, options.Limits);
        CheckedMath.AllocationSize(count, 2, options.Limits);
        ushort[] decoded = new ushort[CheckedMath.ToInt32(count)];
        BoundedInput bounded = BoundedInput.CreateWithAbsoluteOrigin(raw, 0, raw.Length, 0, options.Limits);
        MsbBitReader reader = new MsbBitReader(bounded);
        RawDecodeOptions decodeOptions = new RawDecodeOptions();
        decodeOptions.Limits = options.Limits;
        decodeOptions.CancellationToken = options.CancellationToken;
        decodeOptions.ApplyLinearization = true;
        NikonCompressionCodec.DecodeSamples(reader, decoded, input.Width, input.Height, compression, decodeOptions);
        reader.AlignToByteWithExpectedPadding(0);
        reader.RequireEnd();
        return decoded;
    }

    internal static TiffLayoutEntryInput LongArrayEntry(ushort tag, uint[] values, RawByteOrder byteOrder, TiffLayoutReference[]? references)
    {
        byte[] bytes = new byte[values.Length * 4];
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteUInt32(bytes.AsSpan(index * 4, 4), values[index], byteOrder);
            index++;
        }
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, (ulong)values.Length, bytes, references);
    }

    internal static TiffLayoutEntryInput ByteArrayEntry(ushort tag, byte[] values)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Byte, (ulong)values.Length, values, null);
    }
}
