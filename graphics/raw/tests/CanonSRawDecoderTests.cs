using raw;

namespace raw.Tests;

public class CanonSRawDecoderTests
{
    [Test]
    public void Layout422InterpolatesChromaAndAppliesWhiteBalanceDuringRgbReconstruction()
    {
        RawCanonCr2Metadata metadata = Metadata(4, 1, 2, 1, 2, new int[] { 1024, 1024, 1024 }, new int[] { 8 });
        ushort[] packed = new ushort[] { 1000, 1100, 16384, 16384, 1200, 1300, 16768, 16000 };

        ushort[] rgb = CanonSRawDecoder.Reconstruct(packed, metadata, new RawLimits(), CancellationToken.None);

        Assert.That(rgb, Is.EqualTo(new ushort[]
        {
            4000, 4000, 4000,
            3632, 4636, 5168,
            3264, 5276, 6336,
            3664, 5676, 6736
        }));
    }

    [Test]
    public void Layout420InterpolatesHorizontalVerticalAndDiagonalChroma()
    {
        RawCanonCr2Metadata metadata = Metadata(4, 4, 2, 2, 2, new int[] { 256, 256, 256 }, new int[] { 12 });
        ushort[] packed = new ushort[]
        {
            1000, 1000, 1000, 1000, 16384, 16384,
            1000, 1000, 1000, 1000, 16768, 16384,
            1000, 1000, 1000, 1000, 17152, 16384,
            1000, 1000, 1000, 1000, 17536, 16384
        };

        ushort[] rgb = CanonSRawDecoder.Reconstruct(packed, metadata, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(Pixel(rgb, 4, 0, 0), Is.EqualTo(new ushort[] { 1001, 999, 1001 }));
            Assert.That(Pixel(rgb, 4, 1, 0), Is.EqualTo(new ushort[] { 1001, 962, 1193 }));
            Assert.That(Pixel(rgb, 4, 0, 1), Is.EqualTo(new ushort[] { 1001, 926, 1385 }));
            Assert.That(Pixel(rgb, 4, 1, 1), Is.EqualTo(new ushort[] { 1001, 889, 1577 }));
        });
    }

    [Test]
    public void InterpolationEnumDispatchesQualifiedSrawAndMrawLayouts()
    {
        RawCanonCr2Metadata sraw = Metadata(4, 1, 2, 1, 2, new int[] { 1024, 1024, 1024 }, new int[] { 8 });
        ushort[] packed422 = new ushort[] { 1000, 1100, 16384, 16384, 1200, 1300, 16768, 16000 };
        RawCanonCr2Metadata mraw = Metadata(4, 4, 2, 2, 2, new int[] { 256, 256, 256 }, new int[] { 12 });
        ushort[] packed420 = new ushort[]
        {
            1000, 1000, 1000, 1000, 16384, 16384,
            1000, 1000, 1000, 1000, 16768, 16384,
            1000, 1000, 1000, 1000, 17152, 16384,
            1000, 1000, 1000, 1000, 17536, 16384
        };

        RawSrawInterpolationMode mode422 = RawSrawInterpolator.SelectMode(sraw);
        RawSrawInterpolationMode mode420 = RawSrawInterpolator.SelectMode(mraw);
        int horizontal = RawSrawInterpolator.InterpolateChroma(packed422, sraw, mode422, 0, 0, 1, 0, 2);
        int diagonal = RawSrawInterpolator.InterpolateChroma(packed420, mraw, mode420, 0, 0, 1, 1, 4);

        Assert.Multiple(() =>
        {
            Assert.That(mode422, Is.EqualTo(RawSrawInterpolationMode.Horizontal422));
            Assert.That(mode420, Is.EqualTo(RawSrawInterpolationMode.Block420));
            Assert.That(horizontal, Is.EqualTo(192));
            Assert.That(diagonal, Is.EqualTo(577));
            Assert.Throws<RawProfileException>(() => RawSrawInterpolator.InterpolateChroma(packed422, sraw, (RawSrawInterpolationMode)999, 0, 0, 0, 0, 2));
        });
    }

    [Test]
    public void ReconstructionGenerationsUseDistinctQualifiedTransforms()
    {
        ushort[] old = new ushort[3];
        ushort[] middle = new ushort[3];
        ushort[] current = new ushort[3];
        int[] coefficients = new int[] { 1024, 1024, 1024 };

        CanonSRawDecoder.ConvertPixel(1000, 100, -200, coefficients, 0, old, 0);
        CanonSRawDecoder.ConvertPixel(1000, 100, -200, coefficients, 1, middle, 0);
        CanonSRawDecoder.ConvertPixel(1000, 100, -200, coefficients, 2, current, 0);

        Assert.Multiple(() =>
        {
            Assert.That(old, Is.EqualTo(new ushort[] { 1152, 2276, 2352 }));
            Assert.That(middle, Is.EqualTo(new ushort[] { 0, 5744, 6852 }));
            Assert.That(current, Is.EqualTo(new ushort[] { 3200, 4324, 4400 }));
        });
    }

    [Test]
    public void NewGenerationWhiteBalanceInvertsRedAndBlueCoefficients()
    {
        ushort[] colorData = new ushort[82];
        colorData[78] = 2048;
        colorData[79] = 1023;
        colorData[80] = 1024;
        colorData[81] = 512;

        int[] direct = CanonSRawDecoder.ExtractCoefficients(colorData, false);
        int[] inverted = CanonSRawDecoder.ExtractCoefficients(colorData, true);

        Assert.Multiple(() =>
        {
            Assert.That(direct, Is.EqualTo(new int[] { 2048, 1024, 512 }));
            Assert.That(inverted, Is.EqualTo(new int[] { 512, 1024, 2048 }));
        });
    }

    [Test]
    public void SubsampledJpegPlanesAreFlattenedInMcuOrderForBothLayouts()
    {
        RawCanonCr2Metadata layout422 = Metadata(4, 1, 2, 1, 2, new int[] { 1, 1, 1 }, new int[] { 8 });
        LosslessJpegImage jpeg422 = Image(15,
            Plane(4, 1, 2, 1, new ushort[] { 10, 11, 12, 13 }),
            Plane(2, 1, 1, 1, new ushort[] { 20, 21 }),
            Plane(2, 1, 1, 1, new ushort[] { 30, 31 }));
        RawCanonCr2Metadata layout420 = Metadata(4, 2, 2, 2, 2, new int[] { 1, 1, 1 }, new int[] { 12 });
        LosslessJpegImage jpeg420 = Image(15,
            Plane(4, 2, 2, 2, new ushort[] { 10, 11, 12, 13, 14, 15, 16, 17 }),
            Plane(2, 1, 1, 1, new ushort[] { 20, 21 }),
            Plane(2, 1, 1, 1, new ushort[] { 30, 31 }));

        ushort[] first = CanonSRawDecoder.DecodePacked(jpeg422, layout422, new RawLimits(), CancellationToken.None);
        ushort[] second = CanonSRawDecoder.DecodePacked(jpeg420, layout420, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(new ushort[] { 10, 11, 20, 30, 12, 13, 21, 31 }));
            Assert.That(second, Is.EqualTo(new ushort[] { 10, 11, 14, 15, 20, 30, 12, 13, 16, 17, 21, 31 }));
        });
    }

    [Test]
    public void MissingProfileMetadataAndMismatchedPackedGeometryAreRejected()
    {
        RawCanonCr2Metadata missing = new RawCanonCr2Metadata();
        RawCanonCr2Metadata valid = Metadata(4, 1, 2, 1, 2, new int[] { 1024, 1024, 1024 }, new int[] { 8 });

        RawProfileException? profile = Assert.Throws<RawProfileException>(() => CanonSRawDecoder.Reconstruct(new ushort[8], missing, new RawLimits(), CancellationToken.None));
        RawFormatException? length = Assert.Throws<RawFormatException>(() => CanonSRawDecoder.Reconstruct(new ushort[7], valid, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidProfile));
            Assert.That((length ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
        });
    }

    [Test]
    public void ObservedCanonPredictionWrapIsEnabledOnlyByTheSubsampledDecoderEntryPoint()
    {
        LosslessJpegImage image = Image(16, Plane(2, 1, 1, 1, new ushort[] { 49152, 49153 }));
        image.Width = 2;
        image.Height = 1;
        byte[] encoded = LosslessJpegEncoder.Encode(image, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);
        JpegDocument parsed = JpegParser.Parse(encoded, new RawLimits(), CancellationToken.None);
        encoded[checked((int)(parsed.Frames[0].Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset) + 4] = 15;

        RawFormatException? strict = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(encoded, new RawLimits(), CancellationToken.None));
        LosslessJpegImage canon = LosslessJpegCodec.DecodeCanonSubsampled(encoded, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That((strict ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That(canon.Planes[0].Samples, Is.EqualTo(new ushort[] { 32768, 32769 }));
        });
    }

    private static RawCanonCr2Metadata Metadata(int width, int height, int horizontal, int vertical, int generation, int[] coefficients, int[] slices)
    {
        RawCanonCr2Metadata metadata = new RawCanonCr2Metadata();
        metadata.IsSubsampled = true;
        metadata.SensorWidth = width;
        metadata.SensorHeight = height;
        metadata.SubsamplingHorizontal = horizontal;
        metadata.SubsamplingVertical = vertical;
        metadata.SRawGeneration = generation;
        metadata.SRawHue = ((horizontal * vertical) - 1) >> 1;
        metadata.SRawCoefficients = coefficients;
        metadata.SliceWidths = slices;
        return metadata;
    }

    private static LosslessJpegImage Image(int precision, params LosslessJpegPlane[] planes)
    {
        LosslessJpegImage image = new LosslessJpegImage();
        image.Precision = precision;
        image.Planes.AddRange(planes);
        return image;
    }

    private static LosslessJpegPlane Plane(int width, int height, int horizontal, int vertical, ushort[] samples)
    {
        LosslessJpegPlane plane = new LosslessJpegPlane();
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = horizontal;
        plane.VerticalSampling = vertical;
        plane.Predictor = 1;
        plane.PointTransform = 0;
        plane.Samples = samples;
        return plane;
    }

    private static ushort[] Pixel(ushort[] samples, int width, int x, int y)
    {
        int offset = (y * width + x) * 3;
        return new ushort[] { samples[offset], samples[offset + 1], samples[offset + 2] };
    }
}
