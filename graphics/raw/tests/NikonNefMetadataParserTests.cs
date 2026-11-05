using raw;

namespace raw.Tests;

public class NikonNefMetadataParserTests
{
    [Test]
    public void ExperimentalProfileExtractsNikonSensorCalibrationLensAndShootingMetadata()
    {
        ProfiledNefFixture fixture = BuildProfiledNef(false);
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;

        RawDocument document = RawCodec.Open(fixture.Bytes, null, options);
        RawNikonNefMetadata? nikon = (document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Nikon;
        RawImageInfo? rawImage = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(image => image.Kind == RawImageKind.Raw);

        Assert.Multiple(() =>
        {
            Assert.That(document.ProfileName, Is.EqualTo(NikonNefMetadataParser.Z8LosslessProfile));
            Assert.That(nikon, Is.Not.Null);
            Assert.That((nikon ?? throw new System.InvalidOperationException("A required value is unavailable.")).Model, Is.EqualTo("NIKON Z 8"));
            Assert.That(nikon.StoredWidth, Is.EqualTo(8280));
            Assert.That(nikon.StoredHeight, Is.EqualTo(5520));
            Assert.That((nikon.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(12));
            Assert.That(nikon.ActiveArea.Width, Is.EqualTo(8256));
            Assert.That((nikon.DefaultCrop ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(12));
            Assert.That(nikon.DefaultCrop.Width, Is.EqualTo(8256));
            Assert.That(nikon.MaskedAreas.Single().Height, Is.EqualTo(8));
            Assert.That((nikon.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Rggb));
            Assert.That(nikon.BlackLevelRepeatWidth, Is.EqualTo(2));
            Assert.That(nikon.BlackLevelRepeatHeight, Is.EqualTo(2));
            Assert.That(nikon.BlackLevels, Is.EqualTo(new double[] { 512, 513, 514, 515 }));
            Assert.That(nikon.WhiteLevels, Is.EqualTo(new double[] { 16383 }));
            Assert.That(nikon.LinearizationTable, Is.EqualTo(new ushort[] { 0, 1024, 4096, 16383 }));
            Assert.That(nikon.MakerNoteVersion, Is.EqualTo("0200"));
            Assert.That(nikon.ColorBalanceVersion, Is.EqualTo("0103"));
            Assert.That(nikon.ColorBalanceDecoded, Is.True);
            Assert.That(nikon.ColorBalanceEncrypted, Is.False);
            Assert.That(nikon.ColorBalanceChannelOrder, Is.EqualTo("RGBG"));
            Assert.That(nikon.AsShotWhiteBalance, Is.EqualTo(new double[] { 1000, 2000, 1000, 1500 }));
            Assert.That(nikon.TiffCompressionCode, Is.EqualTo(34713));
            Assert.That(nikon.NefCompressionCode, Is.EqualTo(3));
            Assert.That(nikon.Compression, Is.EqualTo(RawCompression.NikonLossless));
            Assert.That(nikon.LensType, Is.EqualTo(6));
            Assert.That(nikon.LensSpecification.Select(value => value.ToDouble()), Is.EqualTo(new double[] { 24, 70, 2.8, 4 }));
            Assert.That(nikon.LensDataVersion, Is.EqualTo("0204"));
            Assert.That(nikon.LensData, Is.EqualTo(fixture.LensData));
            Assert.That(nikon.Quality, Is.EqualTo("RAW"));
            Assert.That(nikon.WhiteBalanceName, Is.EqualTo("Auto"));
            Assert.That(nikon.FocusMode, Is.EqualTo("AF-S"));
            Assert.That(nikon.NoiseReduction, Is.EqualTo("Normal"));
            Assert.That(nikon.IsoValues, Is.EqualTo(new int[] { 0, 200 }));
            Assert.That(nikon.ShootingMode, Is.EqualTo(5));
            Assert.That(nikon.ShutterCount, Is.EqualTo(12345));
            Assert.That(rawImage.BitDepth, Is.EqualTo(14));
            Assert.That((rawImage.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, Is.EqualTo(8256));
            Assert.That((rawImage.DefaultCrop ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(12));
            Assert.That(rawImage.Compression, Is.EqualTo(RawCompression.NikonLossless));
            Assert.That((rawImage.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Compression, Is.EqualTo(RawCompression.NikonLossless));
            Assert.That(rawImage.Payload.Decodable, Is.False);
            Assert.That(document.Capabilities.Decoded, Is.False);
            Assert.That(rawImage.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void ExperimentalNikonProfileRequiresOptInAndRejectsConflictingGeometry()
    {
        ProfiledNefFixture fixture = BuildProfiledNef(false);
        RawDocument blocked = RawCodec.Open(fixture.Bytes, null, new RawOpenOptions());
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        ProfiledNefFixture conflicting = BuildProfiledNef(true);

        RawProfileException? exception = Assert.Throws<RawProfileException>(() => RawCodec.Open(conflicting.Bytes, null, options));

        Assert.Multiple(() =>
        {
            Assert.That(blocked.ProfileName, Is.Empty);
            Assert.That((blocked.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Nikon, Is.Null);
            Assert.That(blocked.Diagnostics.Any(diagnostic => diagnostic.Code == RawDiagnosticCode.UnsupportedProfile && diagnostic.Severity == RawSeverity.Warning), Is.True);
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidProfile));
        });
    }

    static ProfiledNefFixture BuildProfiledNef(bool conflictingActiveArea)
    {
        byte[] payload = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        NikonMetadataNoteFixture note = CreateNikonMakerNote();
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        SyntheticTiffIfd rawIfd = builder.AddIfd();
        SyntheticTiffIfd exif = builder.AddIfd();
        builder.AddField(root, 271, TiffFieldType.Ascii, 18, System.Text.Encoding.ASCII.GetBytes("NIKON CORPORATION\0"));
        builder.AddField(root, 272, TiffFieldType.Ascii, 10, System.Text.Encoding.ASCII.GetBytes("NIKON Z 8\0"));
        builder.AddPointer(root, 330, TiffFieldType.Long, rawIfd);
        builder.AddPointer(root, 34665, TiffFieldType.Long, exif);
        AddLong(builder, rawIfd, 256, 8280);
        AddLong(builder, rawIfd, 257, 5520);
        AddShort(builder, rawIfd, 258, 14);
        AddShort(builder, rawIfd, 259, 34713);
        AddShort(builder, rawIfd, 262, 32803);
        SyntheticTiffEntry stripOffset = AddLong(builder, rawIfd, 273, 0);
        AddShort(builder, rawIfd, 277, 1);
        AddLong(builder, rawIfd, 278, 5520);
        AddLong(builder, rawIfd, 279, (uint)payload.Length);
        builder.AddField(rawIfd, 33421, TiffFieldType.Short, 2, Shorts(new ushort[] { 2, 2 }));
        builder.AddField(rawIfd, 33422, TiffFieldType.Byte, 4, new byte[] { 0, 1, 1, 2 });
        builder.AddField(rawIfd, 50712, TiffFieldType.Short, 4, Shorts(new ushort[] { 0, 1024, 4096, 16383 }));
        builder.AddField(rawIfd, 50713, TiffFieldType.Short, 2, Shorts(new ushort[] { 2, 2 }));
        builder.AddField(rawIfd, 50714, TiffFieldType.Short, 4, Shorts(new ushort[] { 512, 513, 514, 515 }));
        builder.AddField(rawIfd, 50717, TiffFieldType.Short, 1, Shorts(new ushort[] { 16383 }));
        builder.AddField(rawIfd, 50719, TiffFieldType.Short, 2, Shorts(new ushort[] { 12, 8 }));
        builder.AddField(rawIfd, 50720, TiffFieldType.Short, 2, Shorts(new ushort[] { 8256, 5504 }));
        ushort activeRight = conflictingActiveArea ? (ushort)8267 : (ushort)8268;
        builder.AddField(rawIfd, 50829, TiffFieldType.Short, 4, Shorts(new ushort[] { 8, 12, 5512, activeRight }));
        builder.AddField(rawIfd, 50830, TiffFieldType.Short, 4, Shorts(new ushort[] { 0, 0, 8, 8280 }));
        SyntheticTiffEntry payloadEntry = builder.AddField(rawIfd, 65000, TiffFieldType.Undefined, (ulong)payload.Length, payload);
        builder.AddField(exif, 37500, TiffFieldType.Undefined, (ulong)note.Bytes.Length, note.Bytes);
        byte[] bytes = builder.Build();
        PatchLong(bytes, rawIfd, stripOffset, (uint)payloadEntry.ValueOffset);
        return new ProfiledNefFixture(bytes, note.LensData);
    }

    static NikonMetadataNoteFixture CreateNikonMakerNote()
    {
        const int count = 15;
        int dataPosition = 18 + 2 + count * 12 + 4;
        byte[] whiteBalance = System.Text.Encoding.ASCII.GetBytes("Auto\0");
        byte[] focusMode = System.Text.Encoding.ASCII.GetBytes("AF-S\0");
        byte[] noiseReduction = System.Text.Encoding.ASCII.GetBytes("Normal\0");
        byte[] lens = Rationals(new uint[] { 24, 1, 70, 1, 28, 10, 40, 10 });
        byte[] colorBalance = new byte[32];
        System.Text.Encoding.ASCII.GetBytes("0103").CopyTo(colorBalance, 0);
        EndianWriter.WriteUInt16(colorBalance.AsSpan(20, 2), 1000, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(colorBalance.AsSpan(22, 2), 2000, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(colorBalance.AsSpan(24, 2), 1000, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(colorBalance.AsSpan(26, 2), 1500, RawByteOrder.LittleEndian);
        byte[] lensData = new byte[] { (byte)'0', (byte)'2', (byte)'0', (byte)'4', 9, 8, 7, 6 };
        byte[] capture = new byte[] { 41, 42, 43, 44, 45 };
        byte[] edit = new byte[] { 51, 52, 53, 54, 55, 56 };
        int totalData = whiteBalance.Length + focusMode.Length + lens.Length + noiseReduction.Length + colorBalance.Length + lensData.Length + capture.Length + edit.Length;
        byte[] bytes = new byte[dataPosition + totalData];
        System.Text.Encoding.ASCII.GetBytes("Nikon\0").CopyTo(bytes, 0);
        bytes[6] = 2;
        bytes[10] = (byte)'I';
        bytes[11] = (byte)'I';
        EndianWriter.WriteUInt16(bytes.AsSpan(12, 2), 42, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(14, 4), 8, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(18, 2), count, RawByteOrder.LittleEndian);
        WriteInline(bytes, 0, 0x0001, RawTagType.Undefined, 4, System.Text.Encoding.ASCII.GetBytes("0200"));
        WriteInline(bytes, 1, 0x0002, RawTagType.Short, 2, Shorts(new ushort[] { 0, 200 }));
        WriteInline(bytes, 2, 0x0004, RawTagType.Ascii, 4, System.Text.Encoding.ASCII.GetBytes("RAW\0"));
        int position = dataPosition;
        position = WriteExternal(bytes, 3, 0x0005, RawTagType.Ascii, whiteBalance, position);
        position = WriteExternal(bytes, 4, 0x0007, RawTagType.Ascii, focusMode, position);
        WriteInline(bytes, 5, 0x0083, RawTagType.Byte, 1, new byte[] { 6 });
        position = WriteExternal(bytes, 6, 0x0084, RawTagType.Rational, lens, position);
        WriteInline(bytes, 7, 0x0089, RawTagType.Short, 1, Shorts(new ushort[] { 5 }));
        WriteInline(bytes, 8, 0x0093, RawTagType.Short, 1, Shorts(new ushort[] { 3 }));
        position = WriteExternal(bytes, 9, 0x0095, RawTagType.Ascii, noiseReduction, position);
        position = WriteExternal(bytes, 10, 0x0097, RawTagType.Undefined, colorBalance, position);
        position = WriteExternal(bytes, 11, 0x0098, RawTagType.Undefined, lensData, position);
        byte[] shutter = new byte[4];
        EndianWriter.WriteUInt32(shutter, 12345, RawByteOrder.LittleEndian);
        WriteInline(bytes, 12, 0x00a7, RawTagType.Long, 1, shutter);
        position = WriteExternal(bytes, 13, 0x0e01, RawTagType.Undefined, capture, position);
        WriteExternal(bytes, 14, 0x0e13, RawTagType.Undefined, edit, position);
        return new NikonMetadataNoteFixture(bytes, lensData);
    }

    static int WriteExternal(byte[] bytes, int index, ushort tag, RawTagType type, byte[] value, int valuePosition)
    {
        int count = value.Length / ElementSize(type);
        WriteEntryHeader(bytes, index, tag, type, count);
        int entryPosition = 20 + index * 12;
        EndianWriter.WriteUInt32(bytes.AsSpan(entryPosition + 8, 4), (uint)(valuePosition - 10), RawByteOrder.LittleEndian);
        value.CopyTo(bytes, valuePosition);
        return valuePosition + value.Length;
    }

    static void WriteInline(byte[] bytes, int index, ushort tag, RawTagType type, int count, byte[] value)
    {
        WriteEntryHeader(bytes, index, tag, type, count);
        int entryPosition = 20 + index * 12;
        value.CopyTo(bytes, entryPosition + 8);
    }

    static void WriteEntryHeader(byte[] bytes, int index, ushort tag, RawTagType type, int count)
    {
        int position = 20 + index * 12;
        EndianWriter.WriteUInt16(bytes.AsSpan(position, 2), tag, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(position + 2, 2), (ushort)type, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(position + 4, 4), (uint)count, RawByteOrder.LittleEndian);
    }

    static int ElementSize(RawTagType type)
    {
        if (type == RawTagType.Short) return 2;
        if (type == RawTagType.Long) return 4;
        if (type == RawTagType.Rational) return 8;
        return 1;
    }

    static byte[] Rationals(uint[] values)
    {
        byte[] bytes = new byte[values.Length * 4];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt32(bytes.AsSpan(index * 4, 4), values[index], RawByteOrder.LittleEndian);
        }
        return bytes;
    }

    static byte[] Shorts(ushort[] values)
    {
        byte[] bytes = new byte[values.Length * 2];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], RawByteOrder.LittleEndian);
        }
        return bytes;
    }

    static SyntheticTiffEntry AddLong(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, ushort tag, uint value)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, RawByteOrder.LittleEndian);
        return builder.AddField(ifd, tag, TiffFieldType.Long, 1, bytes);
    }

    static void AddShort(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, ushort tag, ushort value)
    {
        builder.AddField(ifd, tag, TiffFieldType.Short, 1, Shorts(new ushort[] { value }));
    }

    static void PatchLong(byte[] bytes, SyntheticTiffIfd ifd, SyntheticTiffEntry entry, uint value)
    {
        int entryIndex = ifd.Entries.IndexOf(entry);
        int slotOffset = checked((int)ifd.Offset + 2 + entryIndex * 12 + 8);
        EndianWriter.WriteUInt32(bytes.AsSpan(slotOffset, 4), value, RawByteOrder.LittleEndian);
    }
}

internal class ProfiledNefFixture
{
    internal byte[] Bytes;
    internal byte[] LensData;

    internal ProfiledNefFixture(byte[] bytes, byte[] lensData)
    {
        Bytes = bytes;
        LensData = lensData;
    }
}

internal class NikonMetadataNoteFixture
{
    internal byte[] Bytes;
    internal byte[] LensData;

    internal NikonMetadataNoteFixture(byte[] bytes, byte[] lensData)
    {
        Bytes = bytes;
        LensData = lensData;
    }
}
