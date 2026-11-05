using raw;

namespace raw.Tests;

public class CanonCr2MetadataParserTests
{
    [Test]
    public void ExperimentalProfileExtractsCanonSensorCalibrationQualityAndSlices()
    {
        byte[] source = BuildProfiledCr2(15, 132);
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;

        RawDocument document = RawCodec.Open(source, null, options);
        RawDocument metadataOnly = RawCodec.OpenMetadata(source, null, options);
        RawDocument previewsOnly = RawCodec.OpenPreviews(source, null, options);
        RawOpenOptions rawOnlyOptions = new RawOpenOptions();
        rawOnlyOptions.AllowExperimentalProfiles = true;
        rawOnlyOptions.ReadMetadata = false;
        rawOnlyOptions.ReadPreviews = false;
        RawDocument rawOnly = RawCodec.Open(source, null, rawOnlyOptions);
        RawCanonCr2Metadata? canon = (document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Canon;
        RawImageInfo? rawImage = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(image => image.Kind == RawImageKind.Raw);

        Assert.Multiple(() =>
        {
            Assert.That(document.ProfileName, Is.EqualTo(CanonCr2MetadataParser.Eos6DMarkIiProfile));
            Assert.That(canon, Is.Not.Null);
            Assert.That((canon ?? throw new System.InvalidOperationException("A required value is unavailable.")).Model, Is.EqualTo("Canon EOS 6D Mark II"));
            Assert.That(canon.ModelId, Is.EqualTo("2147484678"));
            Assert.That(canon.Quality, Is.EqualTo(4));
            Assert.That(canon.SRawQuality, Is.Zero);
            Assert.That(canon.SensorWidth, Is.EqualTo(6384));
            Assert.That(canon.SensorHeight, Is.EqualTo(4224));
            Assert.That((canon.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(132));
            Assert.That(canon.ActiveArea.Y, Is.EqualTo(56));
            Assert.That(canon.ActiveArea.Width, Is.EqualTo(6240));
            Assert.That(canon.ActiveArea.Height, Is.EqualTo(4160));
            Assert.That((canon.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Rggb));
            Assert.That(canon.ColorDataVersion, Is.EqualTo(15));
            Assert.That(canon.AsShotWhiteBalance, Is.EqualTo(new double[] { 2000, 1024, 1024, 1800 }));
            Assert.That(canon.BlackLevels, Is.EqualTo(new double[] { 512, 513, 514, 515 }));
            Assert.That(canon.WhiteLevels, Is.EqualTo(new double[] { 15000 }));
            Assert.That(canon.RawSliceValues, Is.EqualTo(new int[] { 0, 0, 6384 }));
            Assert.That(canon.SliceWidths, Is.EqualTo(new int[] { 6384 }));
            Assert.That(document.Capabilities.Decoded, Is.True);
            Assert.That(document.Capabilities.Rendered, Is.True);
            Assert.That(document.Capabilities.PreservingRewrite, Is.True);
            Assert.That(document.Capabilities.Encoded, Is.False);
            Assert.That(document.Capabilities.VendorConfirmed, Is.False);
            Assert.That(metadataOnly.Capabilities.Decoded, Is.False);
            Assert.That(metadataOnly.Capabilities.Rendered, Is.False);
            Assert.That(metadataOnly.Capabilities.PreservingRewrite, Is.False);
            Assert.That(previewsOnly.Capabilities.Decoded, Is.False);
            Assert.That(previewsOnly.Capabilities.Rendered, Is.False);
            Assert.That(previewsOnly.Capabilities.PreservingRewrite, Is.False);
            Assert.That(rawOnly.MetadataLoaded, Is.False);
            Assert.That(rawOnly.RawPayloadAvailable, Is.True);
            Assert.That((rawOnly.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Canon, Is.Not.Null);
            Assert.That(rawOnly.Capabilities.Decoded, Is.True);
            Assert.That(rawOnly.Capabilities.Rendered, Is.True);
            Assert.That(rawOnly.Capabilities.PreservingRewrite, Is.False);
            Assert.That((document.Images.Single(image => image.Kind == RawImageKind.Raw).Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Decodable, Is.True);
            Assert.That(rawImage.BitDepth, Is.EqualTo(14));
            Assert.That(rawImage.Photometric, Is.EqualTo(RawPhotometricInterpretation.ColorFilterArray));
            Assert.That((rawImage.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Rggb));
            Assert.That((rawImage.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(132));
            Assert.That((rawImage.DefaultCrop ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawAreaKind.DefaultCrop));
            Assert.That(rawImage.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void MatchingExperimentalProfileRequiresExplicitOpenOptIn()
    {
        byte[] source = BuildProfiledCr2(15, 132);

        RawDocument document = RawCodec.Open(source, null, new RawOpenOptions());

        Assert.Multiple(() =>
        {
            Assert.That(document.ProfileName, Is.Empty);
            Assert.That((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Canon, Is.Null);
            Assert.That(document.Diagnostics.Any(diagnostic => diagnostic.Code == RawDiagnosticCode.UnsupportedProfile && diagnostic.Severity == RawSeverity.Warning), Is.True);
        });
    }

    [Test]
    public void UnreviewedColorDataVersionIsRejectedAfterProfileMatch()
    {
        byte[] source = BuildProfiledCr2(14, 132);
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;

        RawProfileException? exception = Assert.Throws<RawProfileException>(() => RawCodec.Open(source, null, options));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
    }

    [Test]
    public void SensorBordersMustAgreeWithSelectedProfile()
    {
        byte[] source = BuildProfiledCr2(15, 131);
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;

        RawProfileException? exception = Assert.Throws<RawProfileException>(() => RawCodec.Open(source, null, options));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidProfile));
    }

    [TestCase(false, CanonCr2MetadataParser.Eos6DMarkIiSRawProfile, 3120, 2082, 2, 1)]
    [TestCase(true, CanonCr2MetadataParser.Eos6DMarkIiMRawProfile, 4680, 3132, 2, 2)]
    public void SubsampledProfilesDetectModeAndExposeReconstructedRgbGeometry(bool mraw, string profile, int width, int height, int horizontal, int vertical)
    {
        byte[] source = BuildSubsampledCr2(mraw);
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;

        RawDocument document = RawCodec.Open(source, null, options);
        RawCanonCr2Metadata? canon = (document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Canon;
        RawImageInfo? rawImage = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(image => image.Kind == RawImageKind.Raw);

        Assert.Multiple(() =>
        {
            Assert.That(document.ProfileName, Is.EqualTo(profile));
            Assert.That((canon ?? throw new System.InvalidOperationException("A required value is unavailable.")).IsSubsampled, Is.True);
            Assert.That(canon.SRawQuality, Is.EqualTo(mraw ? 1 : 2));
            Assert.That(canon.SubsamplingHorizontal, Is.EqualTo(horizontal));
            Assert.That(canon.SubsamplingVertical, Is.EqualTo(vertical));
            Assert.That(canon.SRawGeneration, Is.EqualTo(2));
            Assert.That(canon.SRawHue, Is.EqualTo(((horizontal * vertical) - 1) >> 1));
            Assert.That(canon.SRawCoefficients, Is.EqualTo(new int[] { 512, 1024, 2048 }));
            Assert.That(canon.WhiteLevels, Is.EqualTo(new double[] { 32767 }));
            Assert.That(rawImage.Width, Is.EqualTo(width));
            Assert.That(rawImage.Height, Is.EqualTo(height));
            Assert.That(rawImage.BitDepth, Is.EqualTo(16));
            Assert.That(rawImage.Photometric, Is.EqualTo(RawPhotometricInterpretation.Rgb));
            Assert.That(rawImage.Channels.Select(item => item.Kind), Is.EqualTo(new RawChannelKind[] { RawChannelKind.Red, RawChannelKind.Green, RawChannelKind.Blue }));
            Assert.That(rawImage.Cfa, Is.Null);
            Assert.That(rawImage.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void ProfileMetadataAndDecodeCapabilityApplyToEveryDualPixelPlane()
    {
        byte[] source = BuildProfiledCr2(15, 132, true);
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;

        RawDocument document = RawCodec.Open(source, null, options);
        RawImageInfo? primary = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(image => image.Kind == RawImageKind.Raw);
        RawImageInfo secondary = document.Images.Single(image => image.Kind == RawImageKind.Auxiliary);

        Assert.Multiple(() =>
        {
            Assert.That((primary.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Decodable, Is.True);
            Assert.That((secondary.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Decodable, Is.True);
            Assert.That(secondary.Payload.PlaneIndex, Is.EqualTo(1));
            Assert.That(secondary.Photometric, Is.EqualTo(RawPhotometricInterpretation.ColorFilterArray));
            Assert.That((secondary.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Rggb));
            Assert.That((secondary.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(132));
            Assert.That(secondary.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    private static byte[] BuildProfiledCr2(ushort colorVersion, short activeLeft)
    {
        return BuildProfiledCr2(colorVersion, activeLeft, false);
    }

    private static byte[] BuildProfiledCr2(ushort colorVersion, short activeLeft, bool secondaryRaw)
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        builder.SetHeaderPadding(8);
        SyntheticTiffIfd ifd0 = builder.AddIfd();
        SyntheticTiffIfd ifd1 = builder.AddIfd();
        SyntheticTiffIfd ifd2 = builder.AddIfd();
        SyntheticTiffIfd ifd3 = builder.AddIfd();
        SyntheticTiffIfd? ifd4 = secondaryRaw ? builder.AddIfd() : null;
        SyntheticTiffIfd exif = builder.AddIfd();
        builder.SetNextIfd(ifd0, ifd1);
        builder.SetNextIfd(ifd1, ifd2);
        builder.SetNextIfd(ifd2, ifd3);
        if (secondaryRaw)
        {
            builder.SetNextIfd(ifd3, ifd4);
        }

        SyntheticTiffEntry previewOffset = AddImage(builder, ifd0, 1, 1, 1, 8, new byte[] { 1, 0, 0, 0, 0 }, out SyntheticTiffEntry previewData);
        builder.AddField(ifd0, 271, TiffFieldType.Ascii, 6, System.Text.Encoding.ASCII.GetBytes("Canon\0"));
        byte[] model = System.Text.Encoding.ASCII.GetBytes("Canon EOS 6D Mark II\0");
        builder.AddField(ifd0, 272, TiffFieldType.Ascii, (ulong)model.Length, model);
        builder.AddPointer(ifd0, 34665, TiffFieldType.Long, exif);
        SyntheticTiffEntry rawOffset = AddImage(builder, ifd3, 6384, 4224, 6, 14, new byte[] { 0xff, 0xd8, 0xff, 0xc3, 0xd9 }, out SyntheticTiffEntry rawData);
        builder.AddField(ifd3, 50752, TiffFieldType.Short, 3, Shorts(new ushort[] { 0, 0, 6384 }));
        SyntheticTiffEntry? auxiliaryOffset = null;
        SyntheticTiffEntry? auxiliaryData = null;
        if (secondaryRaw)
        {
            builder.AddField(ifd3, 50885, TiffFieldType.Long, 1, UInt32(1));
            auxiliaryOffset = AddImage(builder, ifd4, 6384, 4224, 6, 14, new byte[] { 0xff, 0xd8, 0xff, 0xc3, 1, 0xd9 }, out auxiliaryData);
            builder.AddField(ifd4, 50752, TiffFieldType.Short, 3, Shorts(new ushort[] { 0, 0, 6384 }));
            builder.AddField(ifd4, 50885, TiffFieldType.Long, 1, UInt32(1));
        }

        CanonNoteFixture note = BuildCanonNote(colorVersion, activeLeft);
        SyntheticTiffEntry makerEntry = builder.AddField(exif, 37500, TiffFieldType.Undefined, (ulong)note.Bytes.Length, note.Bytes);
        byte[] bytes = builder.Build();
        bytes[8] = 0x43;
        bytes[9] = 0x52;
        bytes[10] = 2;
        bytes[11] = 0;
        EndianWriter.WriteUInt32(bytes.AsSpan(12, 4), (uint)ifd3.Offset, RawByteOrder.LittleEndian);
        PatchLong(bytes, ifd0, previewOffset, (uint)previewData.ValueOffset);
        PatchLong(bytes, ifd3, rawOffset, (uint)rawData.ValueOffset);
        if (secondaryRaw)
        {
            PatchLong(bytes, ifd4, auxiliaryOffset, (uint)(auxiliaryData ?? throw new System.InvalidOperationException("A required value is unavailable.")).ValueOffset);
        }
        PatchCanonOffsets(bytes, checked((int)makerEntry.ValueOffset), note.OffsetSlots, note.DataPositions);
        return bytes;
    }

    private static byte[] BuildSubsampledCr2(bool mraw)
    {
        int width = mraw ? 4680 : 3120;
        int height = mraw ? 3132 : 2082;
        int activeTop = mraw ? 12 : 2;
        ushort quality = mraw ? (ushort)1 : (ushort)2;
        ushort[] slices = mraw ? new ushort[] { 4, 2808, 2808 } : new ushort[] { 3, 1560, 1560 };
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        builder.SetHeaderPadding(8);
        SyntheticTiffIfd ifd0 = builder.AddIfd();
        SyntheticTiffIfd ifd1 = builder.AddIfd();
        SyntheticTiffIfd ifd2 = builder.AddIfd();
        SyntheticTiffIfd ifd3 = builder.AddIfd();
        SyntheticTiffIfd exif = builder.AddIfd();
        builder.SetNextIfd(ifd0, ifd1);
        builder.SetNextIfd(ifd1, ifd2);
        builder.SetNextIfd(ifd2, ifd3);

        SyntheticTiffEntry previewOffset = AddImage(builder, ifd0, 1, 1, 1, 8, new byte[] { 1, 0, 0, 0, 0 }, out SyntheticTiffEntry previewData);
        builder.AddField(ifd0, 271, TiffFieldType.Ascii, 6, System.Text.Encoding.ASCII.GetBytes("Canon\0"));
        byte[] model = System.Text.Encoding.ASCII.GetBytes("Canon EOS 6D Mark II\0");
        builder.AddField(ifd0, 272, TiffFieldType.Ascii, (ulong)model.Length, model);
        builder.AddPointer(ifd0, 34665, TiffFieldType.Long, exif);
        SyntheticTiffEntry rawOffset = AddImage(builder, ifd3, (uint)width, (uint)height, 6, 15, new byte[] { 0xff, 0xd8, 0xff, 0xc3, 0xd9 }, out SyntheticTiffEntry rawData);
        builder.AddField(ifd3, 50752, TiffFieldType.Short, 3, Shorts(slices));
        builder.AddField(ifd3, 50885, TiffFieldType.Long, 1, UInt32(4));

        CanonNoteFixture note = BuildCanonNote(15, 0, width, height, activeTop, width - 1, height - 1, quality);
        SyntheticTiffEntry makerEntry = builder.AddField(exif, 37500, TiffFieldType.Undefined, (ulong)note.Bytes.Length, note.Bytes);
        byte[] bytes = builder.Build();
        bytes[8] = 0x43;
        bytes[9] = 0x52;
        bytes[10] = 2;
        bytes[11] = 0;
        EndianWriter.WriteUInt32(bytes.AsSpan(12, 4), (uint)ifd3.Offset, RawByteOrder.LittleEndian);
        PatchLong(bytes, ifd0, previewOffset, (uint)previewData.ValueOffset);
        PatchLong(bytes, ifd3, rawOffset, (uint)rawData.ValueOffset);
        PatchCanonOffsets(bytes, checked((int)makerEntry.ValueOffset), note.OffsetSlots, note.DataPositions);
        return bytes;
    }

    private static CanonNoteFixture BuildCanonNote(ushort colorVersion, short activeLeft)
    {
        return BuildCanonNote(colorVersion, activeLeft, 6384, 4224, 56, 6371, 4215, 0);
    }

    private static CanonNoteFixture BuildCanonNote(ushort colorVersion, short activeLeft, int width, int height, int activeTop, int activeRight, int activeBottom, ushort srawQuality)
    {
        short[] settings = new short[47];
        settings[3] = 4;
        settings[46] = (short)srawQuality;
        short[] sensor = new short[13];
        sensor[1] = (short)width;
        sensor[2] = (short)height;
        sensor[5] = activeLeft;
        sensor[6] = (short)activeTop;
        sensor[7] = (short)activeRight;
        sensor[8] = (short)activeBottom;
        sensor[9] = -1;
        sensor[10] = -1;
        sensor[11] = -1;
        sensor[12] = -1;
        ushort[] color = new ushort[1602];
        color[0] = colorVersion;
        color[0x3f] = 2000;
        color[0x40] = 1024;
        color[0x41] = 1024;
        color[0x42] = 1800;
        color[78] = 2048;
        color[79] = 1023;
        color[80] = 1024;
        color[81] = 512;
        color[0x30a] = 512;
        color[0x30b] = 513;
        color[0x30c] = 514;
        color[0x30d] = 515;
        color[0x30e] = srawQuality == 0 ? (ushort)15000 : (ushort)8191;

        byte[][] values = new byte[][]
        {
            SignedShorts(settings),
            System.Text.Encoding.ASCII.GetBytes("1.0.2\0"),
            UInt32(2147484678),
            SignedShorts(sensor),
            Shorts(color)
        };
        ushort[] ids = new ushort[] { 0x0001, 0x0007, 0x0010, 0x00e0, 0x4001 };
        TiffFieldType[] types = new TiffFieldType[] { TiffFieldType.SignedShort, TiffFieldType.Ascii, TiffFieldType.Long, TiffFieldType.SignedShort, TiffFieldType.Short };
        uint[] counts = new uint[] { 47, 6, 1, 13, 1602 };
        int headerLength = 2 + ids.Length * 12 + 4;
        int position = headerLength;
        byte[] bytes = new byte[headerLength + values[0].Length + values[1].Length + values[3].Length + values[4].Length];
        EndianWriter.WriteUInt16(bytes.AsSpan(0, 2), (ushort)ids.Length, RawByteOrder.LittleEndian);
        List<int> offsetSlots = new List<int>();
        List<int> dataPositions = new List<int>();
        for (int index = 0; index < ids.Length; index++)
        {
            int entry = 2 + index * 12;
            EndianWriter.WriteUInt16(bytes.AsSpan(entry, 2), ids[index], RawByteOrder.LittleEndian);
            EndianWriter.WriteUInt16(bytes.AsSpan(entry + 2, 2), (ushort)types[index], RawByteOrder.LittleEndian);
            EndianWriter.WriteUInt32(bytes.AsSpan(entry + 4, 4), counts[index], RawByteOrder.LittleEndian);
            if (values[index].Length <= 4)
            {
                values[index].CopyTo(bytes, entry + 8);
            }
            else
            {
                offsetSlots.Add(entry + 8);
                dataPositions.Add(position);
                values[index].CopyTo(bytes, position);
                position += values[index].Length;
            }
        }
        return new CanonNoteFixture(bytes, offsetSlots.ToArray(), dataPositions.ToArray());
    }

    private static void PatchCanonOffsets(byte[] bytes, int makerOffset, int[] slots, int[] positions)
    {
        for (int index = 0; index < slots.Length; index++)
        {
            EndianWriter.WriteUInt32(bytes.AsSpan(makerOffset + slots[index], 4), (uint)(makerOffset + positions[index]), RawByteOrder.LittleEndian);
        }
    }

    private static SyntheticTiffEntry AddImage(SyntheticTiffBuilder builder, SyntheticTiffIfd? ifd, uint width, uint height, ushort compression, ushort bits, byte[] payload, out SyntheticTiffEntry data)
    {
        AddLong(builder, ifd, 256, width);
        AddLong(builder, ifd, 257, height);
        AddShort(builder, ifd, 258, bits);
        AddShort(builder, ifd, 259, compression);
        AddShort(builder, ifd, 262, compression == 6 ? (ushort)32803 : (ushort)1);
        SyntheticTiffEntry offset = AddLong(builder, ifd, 273, 0);
        AddShort(builder, ifd, 277, 1);
        AddLong(builder, ifd, 278, height);
        AddLong(builder, ifd, 279, (uint)payload.Length);
        data = builder.AddField(ifd, 65000, TiffFieldType.Undefined, (ulong)payload.Length, payload);
        return offset;
    }

    private static SyntheticTiffEntry AddLong(SyntheticTiffBuilder builder, SyntheticTiffIfd? ifd, ushort tag, uint value)
    {
        return builder.AddField(ifd, tag, TiffFieldType.Long, 1, UInt32(value));
    }

    private static void AddShort(SyntheticTiffBuilder builder, SyntheticTiffIfd? ifd, ushort tag, ushort value)
    {
        builder.AddField(ifd, tag, TiffFieldType.Short, 1, Shorts(new ushort[] { value }));
    }

    private static void PatchLong(byte[] bytes, SyntheticTiffIfd? ifd, SyntheticTiffEntry? entry, uint value)
    {
        int index = (ifd ?? throw new System.InvalidOperationException("A required value is unavailable.")).Entries.IndexOf((entry ?? throw new System.InvalidOperationException("A required value is unavailable.")));
        int offset = checked((int)ifd.Offset + 2 + index * 12 + 8);
        EndianWriter.WriteUInt32(bytes.AsSpan(offset, 4), value, RawByteOrder.LittleEndian);
    }

    private static byte[] UInt32(uint value)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, RawByteOrder.LittleEndian);
        return bytes;
    }

    private static byte[] Shorts(ushort[] values)
    {
        byte[] bytes = new byte[values.Length * 2];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], RawByteOrder.LittleEndian);
        }
        return bytes;
    }

    private static byte[] SignedShorts(short[] values)
    {
        byte[] bytes = new byte[values.Length * 2];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteInt16(bytes.AsSpan(index * 2, 2), values[index], RawByteOrder.LittleEndian);
        }
        return bytes;
    }
}

internal class CanonNoteFixture
{
    internal byte[] Bytes;
    internal int[] OffsetSlots;
    internal int[] DataPositions;

    internal CanonNoteFixture(byte[] bytes, int[] offsetSlots, int[] dataPositions)
    {
        Bytes = bytes;
        OffsetSlots = offsetSlots;
        DataPositions = dataPositions;
    }
}
