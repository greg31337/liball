using raw;

namespace raw.Tests;

public class CanonEarlyRawLayoutTests
{
    [Test]
    public void HalfHeightDoubleWidthTransformRequiresAnExplicitProfileQuirk()
    {
        CanonEarlyRawHeader standard = Header(4, 1, 2);
        CanonEarlyRawHeader legacy = Header(4, 1, 2);
        RawReadProfile standardProfile = Profile(8, 1, CanonEarlyRawLayout.RawOffsetQuirk);
        RawReadProfile legacyProfile = Profile(4, 2, CanonEarlyRawLayout.RawOffsetQuirk, CanonEarlyRawLayout.HalfHeightDoubleWidthQuirk);

        CanonEarlyRawLayout.ResolveGeometry(standard, standardProfile);
        CanonEarlyRawLayout.ResolveGeometry(legacy, legacyProfile);

        Assert.Multiple(() =>
        {
            Assert.That(standard.StoredWidth, Is.EqualTo(8));
            Assert.That(standard.StoredHeight, Is.EqualTo(1));
            Assert.That(legacy.StoredWidth, Is.EqualTo(4));
            Assert.That(legacy.StoredHeight, Is.EqualTo(2));
        });
    }

    [Test]
    public void InvalidHalfHeightGeometryIsRejectedEvenWhenTheProfileNamesTheQuirk()
    {
        CanonEarlyRawHeader header = Header(3, 2, 2);
        RawReadProfile profile = Profile(3, 4, CanonEarlyRawLayout.RawOffsetQuirk, CanonEarlyRawLayout.HalfHeightDoubleWidthQuirk);

        RawProfileException? exception = Assert.Throws<RawProfileException>(() => CanonEarlyRawLayout.ResolveGeometry(header, profile));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidProfile));
    }

    [Test]
    public void RawOffsetInspectionStopsAtTheFirstEoiAndIgnoresFollowingContainerPadding()
    {
        byte[] jpeg = Encode(4, 1, new ushort[] { 0, 2, 4, 6 }, new ushort[] { 1, 3, 5, 7 });
        byte[] source = new byte[jpeg.Length + 8];
        jpeg.CopyTo(source, 0);
        int index = jpeg.Length;
        while (index < source.Length)
        {
            source[index] = (index & 1) == 0 ? (byte)0xcc : (byte)0xdd;
            index++;
        }
        RawReadProfile profile = Profile(4, 2, CanonEarlyRawLayout.RawOffsetQuirk, CanonEarlyRawLayout.HalfHeightDoubleWidthQuirk);

        CanonEarlyRawHeader header = CanonEarlyRawLayout.Inspect(source, 0, source.Length, profile, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(header.Length, Is.EqualTo(jpeg.Length));
            Assert.That(header.CodedWidth, Is.EqualTo(4));
            Assert.That(header.CodedHeight, Is.EqualTo(1));
            Assert.That(header.ComponentCount, Is.EqualTo(2));
            Assert.That(header.StoredWidth, Is.EqualTo(4));
            Assert.That(header.StoredHeight, Is.EqualTo(2));
        });
    }

    [Test]
    public void RawOffsetInspectionRejectsMissingProfileAndTruncatedJpeg()
    {
        byte[] jpeg = Encode(4, 1, new ushort[] { 0, 2, 4, 6 }, new ushort[] { 1, 3, 5, 7 });
        RawReadProfile unsupported = Profile(8, 1);
        RawReadProfile supported = Profile(8, 1, CanonEarlyRawLayout.DimensionlessIfdQuirk);

        RawProfileException? profile = Assert.Throws<RawProfileException>(() => CanonEarlyRawLayout.Inspect(jpeg, 0, jpeg.Length, unsupported, new RawLimits(), CancellationToken.None));
        RawFormatException? truncated = Assert.Throws<RawFormatException>(() => CanonEarlyRawLayout.Inspect(jpeg, 0, jpeg.Length - 1, supported, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((truncated ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
        });
    }

    [Test]
    public void LegacyTiffDocumentFacadeDecodesHalfHeightProfileAndRetainsExactSamples()
    {
        byte[] jpeg = Encode(4, 1, new ushort[] { 100, 102, 104, 106 }, new ushort[] { 101, 103, 105, 107 });
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.RawImage, "CanonMakerNote/RawDataOffset", 0, jpeg.Length, 0, 0, 0, RawCompression.LosslessJpeg, true, true);
        RawImageInfo info = ImageInfo(payload);
        RawDocument document = new RawDocument(RawFormat.Tiff, CanonCr2MetadataParser.Eos1dProfile, null, new RawImageInfo[] { info }, null,
            new RawPayloadMap(jpeg.Length, new RawPayloadBlock[] { payload }), new RawCapabilities(true, true, true, false, false, false, false), null, true, 1, false, true);
        RawCanonCr2Metadata metadata = new RawCanonCr2Metadata();
        metadata.ProfileId = CanonCr2MetadataParser.Eos1dProfile;
        metadata.SensorWidth = 4;
        metadata.SensorHeight = 2;
        metadata.Cfa = info.Cfa;
        metadata.SliceWidths = new int[] { 4 };
        metadata.RawDataOffset = 0;
        metadata.LosslessJpegCodedWidth = 4;
        metadata.LosslessJpegCodedHeight = 1;
        metadata.LosslessJpegComponentCount = 2;
        metadata.HalfHeightDoubleWidth = true;
        document.AttachCanonCr2Metadata(metadata);
        document.AttachSource(jpeg, false, null);
        RawDecodeOptions options = new RawDecodeOptions();
        options.PreserveStoredSamples = true;

        RawSensorImage decoded = document.Decode(options);

        Assert.Multiple(() =>
        {
            Assert.That((decoded.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(new uint[] { 100, 101, 102, 103, 104, 105, 106, 107 }));
            Assert.That(decoded.Info.Width, Is.EqualTo(4));
            Assert.That(decoded.Info.Height, Is.EqualTo(2));
            Assert.That(decoded.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void EarlyCanonTagReaderHandlesInlineAndTiffRelativeValuesWithoutClaimingRelocation()
    {
        byte[] bytes = new byte[48];
        EndianWriter.WriteUInt16(bytes.AsSpan(0, 2), 2, RawByteOrder.LittleEndian);
        WriteEntry(bytes, 2, 0x0081, TiffFieldType.Long, 1, 1234);
        WriteEntry(bytes, 14, 0x00e0, TiffFieldType.Short, 3, 136);
        EndianWriter.WriteUInt16(bytes.AsSpan(36, 2), 10, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(38, 2), 20, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(40, 2), 30, RawByteOrder.LittleEndian);
        RawMakerNote note = new RawMakerNote(bytes, "Canon", "Canon", 100, -1, "IFD0/ExifIFD", RawByteOrder.LittleEndian, RawMakerNoteOffsetBase.Unknown, -1, -1, null, false);

        RawCanonMakerNoteTag? offset = CanonEarlyRawLayout.FindTag(note, 0x0081);
        RawCanonMakerNoteTag? sensor = CanonEarlyRawLayout.FindTag(note, 0x00e0);

        Assert.Multiple(() =>
        {
            Assert.That((offset ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt32Values(), Is.EqualTo(new uint[] { 1234 }));
            Assert.That((sensor ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values(), Is.EqualTo(new ushort[] { 10, 20, 30 }));
            Assert.That(offset.Writable, Is.False);
            Assert.That(sensor.Writable, Is.False);
        });
    }

    private static CanonEarlyRawHeader Header(int width, int height, int components)
    {
        CanonEarlyRawHeader header = new CanonEarlyRawHeader();
        header.CodedWidth = width;
        header.CodedHeight = height;
        header.ComponentCount = components;
        return header;
    }

    private static RawReadProfile Profile(int width, int height, params string[] quirks)
    {
        RawReadProfile profile = new RawReadProfile();
        profile.Geometry.StoredWidth = width;
        profile.Geometry.StoredHeight = height;
        profile.Codec.StoredWordBits = 12;
        profile.Codec.Quirks = quirks;
        return profile;
    }

    private static byte[] Encode(int width, int height, ushort[] first, ushort[] second)
    {
        LosslessJpegImage image = new LosslessJpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 12;
        image.Planes.Add(Plane(1, width, height, first));
        image.Planes.Add(Plane(2, width, height, second));
        return LosslessJpegEncoder.Encode(image, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);
    }

    private static LosslessJpegPlane Plane(byte identifier, int width, int height, ushort[] samples)
    {
        LosslessJpegPlane plane = new LosslessJpegPlane();
        plane.Identifier = identifier;
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = samples;
        return plane;
    }

    private static RawImageInfo ImageInfo(RawPayloadBlock payload)
    {
        RawCfaPattern cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Grbg);
        return new RawImageInfo(0, RawImageKind.Raw, 4, 2, 12, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.LosslessJpeg, RawOrientation.Normal,
            new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) },
            new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, 4, 2, 12, 4) }, cfa,
            new RawArea(RawAreaKind.StoredImage, 0, 0, 4, 2), new RawArea(RawAreaKind.Active, 0, 0, 4, 2), new RawArea(RawAreaKind.DefaultCrop, 0, 0, 4, 2), payload);
    }

    private static void WriteEntry(byte[] bytes, int position, ushort id, TiffFieldType type, uint count, uint value)
    {
        EndianWriter.WriteUInt16(bytes.AsSpan(position, 2), id, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(position + 2, 2), (ushort)type, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(position + 4, 4), count, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(position + 8, 4), value, RawByteOrder.LittleEndian);
    }
}
