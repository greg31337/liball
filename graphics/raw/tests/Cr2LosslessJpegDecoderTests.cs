using raw;

namespace raw.Tests;

public class Cr2LosslessJpegDecoderTests
{
    [Test]
    public void ComponentSamplesAreFlattenedInFrameOrderBeforeUnslicing()
    {
        LosslessJpegImage jpeg = Image(2, 1, 14,
            Plane(1, 2, 1, new ushort[] { 0, 4 }),
            Plane(2, 2, 1, new ushort[] { 1, 5 }),
            Plane(3, 2, 1, new ushort[] { 2, 6 }),
            Plane(4, 2, 1, new ushort[] { 3, 7 }));

        ushort[] samples = Cr2LosslessJpegDecoder.DecodeSamples(jpeg, 4, 2, new int[] { 2, 2 }, 4, 14, 1, new RawLimits(), CancellationToken.None);

        Assert.That(samples, Is.EqualTo(new ushort[] { 0, 1, 4, 5, 2, 3, 6, 7 }));
    }

    [Test]
    public void OddStoredDimensionsAreUnslicedWithoutDroppingTailSamples()
    {
        LosslessJpegImage jpeg = Image(5, 1, 12,
            Plane(1, 5, 1, new ushort[] { 0, 3, 6, 9, 12 }),
            Plane(2, 5, 1, new ushort[] { 1, 4, 7, 10, 13 }),
            Plane(3, 5, 1, new ushort[] { 2, 5, 8, 11, 14 }));

        ushort[] samples = Cr2LosslessJpegDecoder.DecodeSamples(jpeg, 5, 3, new int[] { 2, 3 }, 3, 12, 1, new RawLimits(), CancellationToken.None);

        Assert.That(samples, Is.EqualTo(new ushort[]
        {
            0, 1, 6, 7, 8,
            2, 3, 9, 10, 11,
            4, 5, 12, 13, 14
        }));
    }

    [Test]
    public void SliceTableMustContainPositiveWidthsThatExactlyCoverTheImage()
    {
        RawFormatException? missing = Assert.Throws<RawFormatException>(() => Cr2LosslessJpegDecoder.Unslice(new ushort[8], 4, 2, System.Array.Empty<int>(), new RawLimits(), CancellationToken.None));
        RawFormatException? zero = Assert.Throws<RawFormatException>(() => Cr2LosslessJpegDecoder.Unslice(new ushort[8], 4, 2, new int[] { 2, 0, 2 }, new RawLimits(), CancellationToken.None));
        RawFormatException? shortTable = Assert.Throws<RawFormatException>(() => Cr2LosslessJpegDecoder.Unslice(new ushort[8], 4, 2, new int[] { 3 }, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((missing ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((zero ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((shortTable ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
        });
    }

    [Test]
    public void DecodedComponentCountAndGeometryMustMatchExactly()
    {
        LosslessJpegImage tooShort = Image(2, 1, 14,
            Plane(1, 2, 1, new ushort[] { 0, 4 }),
            Plane(2, 2, 1, new ushort[] { 1, 5 }),
            Plane(3, 2, 1, new ushort[] { 2, 6 }),
            Plane(4, 2, 1, new ushort[] { 3, 7 }));
        LosslessJpegImage mismatched = Image(2, 1, 14,
            Plane(1, 2, 1, new ushort[] { 0, 1 }),
            Plane(2, 1, 2, new ushort[] { 2, 3 }),
            Plane(3, 2, 1, new ushort[] { 4, 5 }),
            Plane(4, 2, 1, new ushort[] { 6, 7 }));

        RawFormatException? count = Assert.Throws<RawFormatException>(() => Cr2LosslessJpegDecoder.DecodeSamples(tooShort, 5, 2, new int[] { 5 }, 4, 14, 1, new RawLimits(), CancellationToken.None));
        RawFormatException? geometry = Assert.Throws<RawFormatException>(() => Cr2LosslessJpegDecoder.DecodeSamples(mismatched, 4, 2, new int[] { 4 }, 4, 14, 1, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((count ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((geometry ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void ProfileSliceValidationRejectsAWellFormedButUnexpectedLayout()
    {
        RawProfileException? exception = Assert.Throws<RawProfileException>(() => Cr2LosslessJpegDecoder.ValidateProfileSlices(new int[] { 2, 2 }, new int[] { 4 }, 4));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidProfile));
    }

    [Test]
    public void DocumentFacadeDecodesProfiledCr2AndPreservesExactSamples()
    {
        LosslessJpegImage jpeg = Image(2, 1, 14,
            Plane(1, 2, 1, new ushort[] { 100, 104 }),
            Plane(2, 2, 1, new ushort[] { 101, 105 }),
            Plane(3, 2, 1, new ushort[] { 102, 106 }),
            Plane(4, 2, 1, new ushort[] { 103, 107 }));
        byte[] encoded = LosslessJpegEncoder.Encode(jpeg, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);
        RawDocument document = Document(encoded);
        RawDecodeOptions options = new RawDecodeOptions();
        options.PreserveStoredSamples = true;

        RawSensorImage decoded = document.Decode(options);

        Assert.Multiple(() =>
        {
            Assert.That((decoded.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(new uint[] { 100, 101, 102, 103, 104, 105, 106, 107 }));
            Assert.That(decoded.IntegerSamples[0], Is.EqualTo(Normalize(100, 14)));
            Assert.That(decoded.IntegerSamples[7], Is.EqualTo(Normalize(107, 14)));
            Assert.That((decoded.BlackLevel ?? throw new System.InvalidOperationException("A required value is unavailable.")).Values[0], Is.EqualTo(10.0));
            Assert.That(decoded.BlackLevel.GetValue(1, 1, 0), Is.EqualTo(13.0));
            Assert.That(decoded.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void DecodeOptionsSelectLogicalRawImagesAndDistinctDualPixelPlanes()
    {
        byte[] primaryBytes = EncodeSamples(100);
        byte[] secondaryBytes = EncodeSamples(500);
        byte[] nextImageBytes = EncodeSamples(900);
        byte[] source = primaryBytes.Concat(secondaryBytes).Concat(nextImageBytes).ToArray();
        RawPayloadBlock primaryPayload = new RawPayloadBlock(RawPayloadKind.RawImage, "IFD3/Strip0", 0, primaryBytes.Length, 0, 0, 0, RawCompression.LosslessJpeg, true, true);
        RawPayloadBlock secondaryPayload = new RawPayloadBlock(RawPayloadKind.RawImage, "IFD4/Strip0", primaryBytes.Length, secondaryBytes.Length, 1, 1, 0, RawCompression.LosslessJpeg, true, true);
        RawPayloadBlock nextImagePayload = new RawPayloadBlock(RawPayloadKind.RawImage, "IFD5/Strip0", primaryBytes.Length + secondaryBytes.Length, nextImageBytes.Length, 2, 0, 0, RawCompression.LosslessJpeg, true, true);
        RawImageInfo primary = Info(0, RawImageKind.Raw, primaryPayload);
        RawImageInfo secondary = Info(1, RawImageKind.Auxiliary, secondaryPayload);
        RawImageInfo nextImage = Info(2, RawImageKind.Raw, nextImagePayload);
        RawDocument document = MultiImageDocument(source, new RawImageInfo[] { primary, secondary, nextImage }, new RawPayloadBlock[] { primaryPayload, secondaryPayload, nextImagePayload });

        RawDecodeOptions options = new RawDecodeOptions();
        RawSensorImage first = document.Decode(options);
        options.PlaneIndex = 1;
        RawSensorImage secondPlane = document.Decode(options);
        options.ImageIndex = 1;
        options.PlaneIndex = 0;
        RawSensorImage secondImage = document.Decode(options);
        options.PlaneIndex = 1;
        RawFormatException? unavailable = Assert.Throws<RawFormatException>(() => document.Decode(options));
        IRawCodec codec = new RawCodecService();
        RawRenderOptions renderOptions = new RawRenderOptions();
        renderOptions.DemosaicMode = RawDemosaicMode.Bilinear;
        renderOptions.WhiteBalanceMode = RawWhiteBalanceMode.None;
        renderOptions.ColorSpace = RawColorSpace.RawCamera;
        renderOptions.TransferFunction = RawTransferFunction.Linear;
        renderOptions.OutputBitDepth = 32;
        renderOptions.ApplyOrientation = false;
        RawRgbImage firstRendered = codec.Render(document, renderOptions);
        renderOptions.PlaneIndex = 1;
        RawRgbImage secondPlaneRendered = codec.Render(document, renderOptions);
        renderOptions.ImageIndex = 1;
        renderOptions.PlaneIndex = 0;
        RawRgbImage secondImageRendered = codec.Render(document, renderOptions);

        Assert.Multiple(() =>
        {
            Assert.That(first.Info, Is.SameAs(primary));
            Assert.That(first.IntegerSamples[0], Is.EqualTo(Normalize(100, 14)));
            Assert.That(secondPlane.Info, Is.SameAs(secondary));
            Assert.That(secondPlane.IntegerSamples[0], Is.EqualTo(Normalize(500, 14)));
            Assert.That(secondImage.Info, Is.SameAs(nextImage));
            Assert.That(secondImage.IntegerSamples[0], Is.EqualTo(Normalize(900, 14)));
            Assert.That((unavailable ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That(firstRendered.FloatingSamples[0], Is.EqualTo((100.0F - 10.0F) / (16383.0F - 10.0F)).Within(0.000001F));
            Assert.That(firstRendered.FloatingSamples[0], Is.LessThan(secondPlaneRendered.FloatingSamples[0]));
            Assert.That(secondPlaneRendered.FloatingSamples[0], Is.LessThan(secondImageRendered.FloatingSamples[0]));
            Assert.That(firstRendered.Width, Is.EqualTo(4));
            Assert.That(firstRendered.Height, Is.EqualTo(2));
        });
    }

    private static byte[] EncodeSamples(ushort start)
    {
        LosslessJpegImage jpeg = Image(2, 1, 14,
            Plane(1, 2, 1, new ushort[] { start, (ushort)(start + 4) }),
            Plane(2, 2, 1, new ushort[] { (ushort)(start + 1), (ushort)(start + 5) }),
            Plane(3, 2, 1, new ushort[] { (ushort)(start + 2), (ushort)(start + 6) }),
            Plane(4, 2, 1, new ushort[] { (ushort)(start + 3), (ushort)(start + 7) }));
        return LosslessJpegEncoder.Encode(jpeg, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);
    }

    private static RawImageInfo Info(int index, RawImageKind kind, RawPayloadBlock payload)
    {
        return new RawImageInfo(index, kind, 4, 2, 14, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.LosslessJpeg, RawOrientation.Normal,
            new RawChannelDescription[] { new RawChannelDescription("Mosaic", RawChannelKind.Custom) },
            new RawPlaneInfo[] { new RawPlaneInfo(0, "Mosaic", RawChannelKind.Custom, 4, 2, 14, 4) },
            RawCfaPattern.CreateBayer(RawCfaLayout.Rggb),
            new RawArea(RawAreaKind.StoredImage, 0, 0, 4, 2),
            new RawArea(RawAreaKind.Active, 0, 0, 4, 2),
            new RawArea(RawAreaKind.DefaultCrop, 0, 0, 4, 2), payload);
    }

    private static RawDocument MultiImageDocument(byte[] source, RawImageInfo[] images, RawPayloadBlock[] payloads)
    {
        RawDocument document = new RawDocument(RawFormat.Cr2, CanonCr2MetadataParser.Eos6DMarkIiProfile, null, images, null,
            new RawPayloadMap(source.Length, payloads),
            new RawCapabilities(true, true, true, true, false, false, false), null, true, 1, false, true);
        RawCanonCr2Metadata metadata = new RawCanonCr2Metadata();
        metadata.ProfileId = CanonCr2MetadataParser.Eos6DMarkIiProfile;
        metadata.SensorWidth = 4;
        metadata.SensorHeight = 2;
        metadata.ActiveArea = images[0].ActiveArea;
        metadata.Cfa = images[0].Cfa;
        metadata.BlackLevels = new double[] { 10, 11, 12, 13 };
        metadata.WhiteLevels = new double[] { 16383 };
        metadata.SliceWidths = new int[] { 4 };
        document.AttachCanonCr2Metadata(metadata);
        document.AttachSource(source, false, null);
        return document;
    }

    private static RawDocument Document(byte[] encoded)
    {
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.RawImage, "IFD3/Strips", 0, encoded.Length, 0, 0, 0, RawCompression.LosslessJpeg, true, false);
        RawImageInfo info = new RawImageInfo(0, RawImageKind.Raw, 4, 2, 14, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.LosslessJpeg, RawOrientation.Normal,
            new RawChannelDescription[] { new RawChannelDescription("Mosaic", RawChannelKind.Custom) },
            new RawPlaneInfo[] { new RawPlaneInfo(0, "Mosaic", RawChannelKind.Custom, 4, 2, 14, 4) },
            RawCfaPattern.CreateBayer(RawCfaLayout.Rggb),
            new RawArea(RawAreaKind.StoredImage, 0, 0, 4, 2),
            new RawArea(RawAreaKind.Active, 0, 0, 4, 2),
            new RawArea(RawAreaKind.DefaultCrop, 0, 0, 4, 2), payload);
        RawDocument document = new RawDocument(RawFormat.Cr2, CanonCr2MetadataParser.Eos6DMarkIiProfile, null, new RawImageInfo[] { info }, null,
            new RawPayloadMap(encoded.Length, new RawPayloadBlock[] { payload }),
            new RawCapabilities(true, true, true, false, false, false, false), null, true, 1, false, true);
        RawCanonCr2Metadata metadata = new RawCanonCr2Metadata();
        metadata.ProfileId = CanonCr2MetadataParser.Eos6DMarkIiProfile;
        metadata.SensorWidth = 4;
        metadata.SensorHeight = 2;
        metadata.ActiveArea = info.ActiveArea;
        metadata.Cfa = info.Cfa;
        metadata.BlackLevels = new double[] { 10, 11, 12, 13 };
        metadata.WhiteLevels = new double[] { 16383 };
        metadata.SliceWidths = new int[] { 4 };
        document.AttachCanonCr2Metadata(metadata);
        document.AttachSource(encoded, false, null);
        return document;
    }

    private static ushort Normalize(ushort value, int precision)
    {
        uint maximum = (1U << precision) - 1U;
        return (ushort)(((uint)value * ushort.MaxValue + maximum / 2U) / maximum);
    }

    private static LosslessJpegImage Image(int width, int height, int precision, params LosslessJpegPlane[] planes)
    {
        LosslessJpegImage image = new LosslessJpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = precision;
        image.Planes.AddRange(planes);
        return image;
    }

    private static LosslessJpegPlane Plane(byte identifier, int width, int height, ushort[] samples)
    {
        LosslessJpegPlane plane = new LosslessJpegPlane();
        plane.Identifier = identifier;
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Predictor = 1;
        plane.PointTransform = 0;
        plane.Samples = samples;
        return plane;
    }
}
