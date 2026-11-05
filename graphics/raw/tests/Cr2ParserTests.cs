using raw;

namespace raw.Tests;

public class Cr2ParserTests
{
    [Test]
    public void OpenParsesHeaderValidatesIfd3AndDiscoversCanonImages()
    {
        Cr2Fixture fixture = BuildCr2(true);
        RawOpenOptions options = new RawOpenOptions();
        options.ReadPreviews = true;

        RawDocument document = RawCodec.Open(fixture.Bytes, null, options);
        RawImageInfo? rawImage = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(image => image.Kind == RawImageKind.Raw);
        RawThumbnail jpegThumbnail = document.Thumbnails.Single(thumbnail => thumbnail.Kind == RawThumbnailKind.Jpeg);

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header, Is.Not.Null);
            Assert.That((document.FormatDetails.Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).ByteOrder, Is.EqualTo(RawByteOrder.LittleEndian));
            Assert.That(document.FormatDetails.Cr2Header.MajorVersion, Is.EqualTo(2));
            Assert.That(document.FormatDetails.Cr2Header.MinorVersion, Is.Zero);
            Assert.That(document.FormatDetails.Cr2Header.FirstIfdOffset, Is.EqualTo(16));
            Assert.That(document.FormatDetails.Cr2Header.RawIfdOffset, Is.EqualTo((long)fixture.RawIfd.Offset));
            Assert.That(document.FormatDetails.Cr2Header.RawIfdPath, Is.EqualTo("IFD3"));
            Assert.That(document.Images.Count(image => image.Kind == RawImageKind.Preview), Is.EqualTo(2));
            Assert.That(rawImage.Compression, Is.EqualTo(RawCompression.LosslessJpeg));
            Assert.That((rawImage.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Compression, Is.EqualTo(RawCompression.LosslessJpeg));
            Assert.That(rawImage.Payload.Decodable, Is.False);
            Assert.That(jpegThumbnail.Width, Is.EqualTo(8));
            Assert.That(jpegThumbnail.Height, Is.EqualTo(8));
            Assert.That((jpegThumbnail.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Path, Is.EqualTo("IFD1/JPEGInterchange"));
            Assert.That(jpegThumbnail.EncodedBytes, Is.EqualTo(fixture.Jpeg));
        });
    }

    [Test]
    public void UnsupportedVersionIsStillRecognizedAsCr2AndRejectedPrecisely()
    {
        Cr2Fixture fixture = BuildCr2(true);
        fixture.Bytes[10] = 3;

        RawUnsupportedException? exception = Assert.Throws<RawUnsupportedException>(() => RawCodec.Open(fixture.Bytes, null, new RawOpenOptions()));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedFormat));
            Assert.That(exception.Offset, Is.EqualTo(10));
        });
    }

    [Test]
    public void RawHeaderOffsetMustIdentifyTheFourthRootIfd()
    {
        Cr2Fixture fixture = BuildCr2(true);
        EndianWriter.WriteUInt32(fixture.Bytes.AsSpan(12, 4), (uint)fixture.PreviewIfd.Offset, RawByteOrder.LittleEndian);

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => RawCodec.Open(fixture.Bytes, null, new RawOpenOptions()));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidContainer));
    }

    [Test]
    public void ZeroRawOffsetIsRecognizedAsMalformedCr2()
    {
        Cr2Fixture fixture = BuildCr2(true);
        EndianWriter.WriteUInt32(fixture.Bytes.AsSpan(12, 4), 0, RawByteOrder.LittleEndian);

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => RawCodec.Open(fixture.Bytes, null, new RawOpenOptions()));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That(exception.Offset, Is.EqualTo(12));
        });
    }

    [Test]
    public void TolerantModeOmitsMalformedOptionalIfd1ThumbnailWithDiagnostic()
    {
        Cr2Fixture fixture = BuildCr2(false);
        RawOpenOptions strict = new RawOpenOptions();
        RawOpenOptions tolerant = new RawOpenOptions();
        tolerant.Tolerant = true;
        tolerant.ReadPreviews = true;

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => RawCodec.Open(fixture.Bytes, null, strict));
        RawDocument recovered = RawCodec.Open(fixture.Bytes, null, tolerant);

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidContainer));
            Assert.That(recovered.Diagnostics.Any(diagnostic => diagnostic.Code == RawDiagnosticCode.RecoveryApplied && diagnostic.Recovered), Is.True);
            Assert.That(recovered.Thumbnails.Any(thumbnail => thumbnail.Kind == RawThumbnailKind.Jpeg), Is.False);
        });
    }

    [Test]
    public void ValidationFacadeRejectsOrReportsTheNamedThumbnailRecoveryByMode()
    {
        Cr2Fixture fixture = BuildCr2(false);
        RawValidationOptions strict = new RawValidationOptions();
        RawValidationOptions tolerant = new RawValidationOptions();
        tolerant.Strict = false;
        tolerant.AllowRecovery = true;

        Assert.Throws<RawFormatException>(() => RawCodec.Validate(fixture.Bytes, null, strict));
        RawValidationReport report = RawCodec.Validate(fixture.Bytes, null, tolerant);
        RawValidationMessage? recovered = (report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(value => value.Code == RawDiagnosticCode.RecoveryApplied);

        Assert.Multiple(() =>
        {
            Assert.That(recovered.Category, Is.EqualTo(RawValidationCategory.Structural));
            Assert.That(recovered.Severity, Is.EqualTo(RawSeverity.Warning));
            Assert.That(recovered.Recovered, Is.True);
            Assert.That(report.Messages.Any(value => value.Severity == RawSeverity.Error && value.Context.StartsWith("IFD1", StringComparison.Ordinal)), Is.False);
        });
    }

    [Test]
    public void TolerantThumbnailRecoveryDoesNotHideAnUnrelatedIfdError()
    {
        Cr2Fixture fixture = BuildCr2(false);
        RawOpenOptions open = new RawOpenOptions();
        open.Tolerant = true;
        RawDocument document = RawCodec.Open(fixture.Bytes, null, open);
        (((document.TiffGraph ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindIfd("IFD1") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(65001) ?? throw new System.InvalidOperationException("A required value is unavailable.")).EntryOffset = -1;
        RawValidationOptions validation = new RawValidationOptions();
        validation.Strict = false;
        validation.AllowRecovery = true;

        RawValidationReport report = document.Validate(validation);

        Assert.Multiple(() =>
        {
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(value => value.Code == RawDiagnosticCode.RecoveryApplied && value.Recovered)), Is.True);
            Assert.That(report.Messages.Any(value => value.Severity == RawSeverity.Error && value.Context == "IFD1/Tag0xFDE9"), Is.True);
            Assert.That(report.IsValid(), Is.False);
        });
    }

    [Test]
    public void DualPixelRawIfdRemainsAnAuxiliaryPlaneAndAllPreviewBytesAreExact()
    {
        Cr2Fixture fixture = BuildCr2(true, true);
        RawOpenOptions options = new RawOpenOptions();
        options.ReadPreviews = true;

        RawDocument document = RawCodec.Open(fixture.Bytes, null, options);
        RawImageInfo? primary = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(image => image.Kind == RawImageKind.Raw);
        RawImageInfo secondary = document.Images.Single(image => image.Kind == RawImageKind.Auxiliary);
        RawPayloadBlock[] rawPayloads = document.PayloadMap.Find(RawPayloadKind.RawImage);

        Assert.Multiple(() =>
        {
            Assert.That((primary.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).PlaneIndex, Is.Zero);
            Assert.That((secondary.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).PlaneIndex, Is.EqualTo(1));
            Assert.That(RawCodec.SelectCanonRawImage(document, 0, 0), Is.SameAs(primary));
            Assert.That(RawCodec.SelectCanonRawImage(document, 0, 1), Is.SameAs(secondary));
            Assert.That(rawPayloads.Select(payload => payload.PlaneIndex), Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(document.ExtractPayload(primary.Payload), Is.EqualTo(new byte[] { 0xff, 0xd8, 0xff, 0xc3, 0xd9 }));
            Assert.That(document.ExtractPayload(secondary.Payload), Is.EqualTo(fixture.SecondaryRaw));
            Assert.That(document.Thumbnails.Length, Is.EqualTo(3));
            Assert.That(document.Thumbnails.Any(thumbnail => thumbnail.EncodedBytes.SequenceEqual(new byte[] { 21, 0, 0, 0, 0 })), Is.True);
            Assert.That(document.Thumbnails.Any(thumbnail => thumbnail.EncodedBytes.SequenceEqual(new byte[] { 42, 0, 0, 0, 0 })), Is.True);
            Assert.That(document.Thumbnails.Any(thumbnail => thumbnail.EncodedBytes.SequenceEqual(fixture.Jpeg)), Is.True);
            Assert.That(document.Thumbnails.Any(thumbnail => thumbnail.EncodedBytes.SequenceEqual(fixture.SecondaryRaw)), Is.False);
        });
    }

    private static Cr2Fixture BuildCr2(bool completeThumbnail)
    {
        return BuildCr2(completeThumbnail, false);
    }

    private static Cr2Fixture BuildCr2(bool completeThumbnail, bool secondaryRaw)
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        builder.SetHeaderPadding(8);
        SyntheticTiffIfd preview0 = builder.AddIfd();
        SyntheticTiffIfd thumbnail = builder.AddIfd();
        SyntheticTiffIfd preview2 = builder.AddIfd();
        SyntheticTiffIfd rawIfd = builder.AddIfd();
        SyntheticTiffIfd? auxiliaryIfd = secondaryRaw ? builder.AddIfd() : null;
        builder.SetNextIfd(preview0, thumbnail);
        builder.SetNextIfd(thumbnail, preview2);
        builder.SetNextIfd(preview2, rawIfd);
        if (secondaryRaw)
        {
            builder.SetNextIfd(rawIfd, auxiliaryIfd);
        }

        SyntheticTiffEntry preview0Offset = AddImage(builder, preview0, 1, 1, 1, 8, new byte[] { 21, 0, 0, 0, 0 }, out SyntheticTiffEntry preview0Data);
        byte[] jpeg = CreateJpeg();
        SyntheticTiffEntry jpegOffset = AddLong(builder, thumbnail, 513, 0);
        if (completeThumbnail)
        {
            AddLong(builder, thumbnail, 514, (uint)jpeg.Length);
        }
        SyntheticTiffEntry jpegData = builder.AddField(thumbnail, 65001, TiffFieldType.Undefined, (ulong)jpeg.Length, jpeg);
        SyntheticTiffEntry preview2Offset = AddImage(builder, preview2, 1, 1, 1, 8, new byte[] { 42, 0, 0, 0, 0 }, out SyntheticTiffEntry preview2Data);
        SyntheticTiffEntry rawOffset = AddImage(builder, rawIfd, 4, 2, 6, 14, new byte[] { 0xff, 0xd8, 0xff, 0xc3, 0xd9 }, out SyntheticTiffEntry rawData);
        byte[] secondaryBytes = new byte[] { 0xff, 0xd8, 0xff, 0xc3, 1, 0xd9 };
        SyntheticTiffEntry? auxiliaryOffset = null;
        SyntheticTiffEntry? auxiliaryData = null;
        if (secondaryRaw)
        {
            builder.AddField(rawIfd, 50752, TiffFieldType.Short, 3, Shorts(new ushort[] { 1, 2, 2 }));
            AddLong(builder, rawIfd, 50885, 1);
            auxiliaryOffset = AddImage(builder, auxiliaryIfd, 4, 2, 6, 14, secondaryBytes, out auxiliaryData);
            builder.AddField(auxiliaryIfd, 50752, TiffFieldType.Short, 3, Shorts(new ushort[] { 1, 2, 2 }));
            AddLong(builder, auxiliaryIfd, 50885, 1);
        }

        byte[] bytes = builder.Build();
        bytes[8] = 0x43;
        bytes[9] = 0x52;
        bytes[10] = 2;
        bytes[11] = 0;
        EndianWriter.WriteUInt32(bytes.AsSpan(12, 4), (uint)rawIfd.Offset, RawByteOrder.LittleEndian);
        PatchLong(bytes, preview0, preview0Offset, (uint)preview0Data.ValueOffset);
        PatchLong(bytes, thumbnail, jpegOffset, (uint)jpegData.ValueOffset);
        PatchLong(bytes, preview2, preview2Offset, (uint)preview2Data.ValueOffset);
        PatchLong(bytes, rawIfd, rawOffset, (uint)rawData.ValueOffset);
        if (secondaryRaw)
        {
            PatchLong(bytes, auxiliaryIfd, auxiliaryOffset, (uint)(auxiliaryData ?? throw new System.InvalidOperationException("A required value is unavailable.")).ValueOffset);
        }
        return new Cr2Fixture(bytes, jpeg, rawIfd, preview2, secondaryRaw ? secondaryBytes : System.Array.Empty<byte>());
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
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, RawByteOrder.LittleEndian);
        return builder.AddField(ifd, tag, TiffFieldType.Long, 1, bytes);
    }

    private static void AddShort(SyntheticTiffBuilder builder, SyntheticTiffIfd? ifd, ushort tag, ushort value)
    {
        byte[] bytes = new byte[2];
        EndianWriter.WriteUInt16(bytes, value, RawByteOrder.LittleEndian);
        builder.AddField(ifd, tag, TiffFieldType.Short, 1, bytes);
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

    private static void PatchLong(byte[] bytes, SyntheticTiffIfd? ifd, SyntheticTiffEntry? entry, uint value)
    {
        int entryIndex = (ifd ?? throw new System.InvalidOperationException("A required value is unavailable.")).Entries.IndexOf((entry ?? throw new System.InvalidOperationException("A required value is unavailable.")));
        int slotOffset = checked((int)ifd.Offset + 2 + entryIndex * 12 + 8);
        EndianWriter.WriteUInt32(bytes.AsSpan(slotOffset, 4), value, RawByteOrder.LittleEndian);
    }

    private static byte[] CreateJpeg()
    {
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = 8;
        image.Height = 8;
        image.Precision = 8;
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = 1;
        plane.Width = 8;
        plane.Height = 8;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = Enumerable.Repeat((byte)128, 64).ToArray();
        image.Planes.Add(plane);
        return BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(80, 1, 1, 0), new RawLimits(), CancellationToken.None);
    }
}

internal class Cr2Fixture
{
    internal byte[] Bytes;
    internal byte[] Jpeg;
    internal SyntheticTiffIfd RawIfd;
    internal SyntheticTiffIfd PreviewIfd;
    internal byte[] SecondaryRaw;

    internal Cr2Fixture(byte[] bytes, byte[] jpeg, SyntheticTiffIfd rawIfd, SyntheticTiffIfd previewIfd, byte[] secondaryRaw)
    {
        Bytes = bytes;
        Jpeg = jpeg;
        RawIfd = rawIfd;
        PreviewIfd = previewIfd;
        SecondaryRaw = secondaryRaw;
    }
}
