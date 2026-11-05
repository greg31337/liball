using raw;

namespace raw.Tests;

public class YCbCrConverterTests
{
    [Test]
    public void LimitedRangeBt601ReferenceVectorsConvertAndClipToRgb()
    {
        YCbCrImageData source = Image(3, 1, 8, 1, 1,
            BytePlane(3, 1, new byte[] { 16, 235, 81 }),
            BytePlane(3, 1, new byte[] { 128, 128, 90 }),
            BytePlane(3, 1, new byte[] { 128, 128, 240 }));
        YCbCrConversionParameters parameters = new YCbCrConversionParameters(0.299, 0.114, 16, 235, 128, 240, 128, 240, YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear);

        RgbByteImage result = YCbCrConverter.Convert(source, parameters, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(3));
            Assert.That(result.Height, Is.EqualTo(1));
            Assert.That(result.Samples, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255, 254, 0, 0 }));
        });
    }

    [Test]
    public void CenteredAndCositedPositionsChangeBilinearAndNearestChromaPhases()
    {
        YCbCrImageData source = Image(4, 1, 8, 2, 1,
            BytePlane(4, 1, new byte[] { 128, 128, 128, 128 }),
            BytePlane(2, 1, new byte[] { 128, 255 }),
            BytePlane(2, 1, new byte[] { 128, 128 }));
        YCbCrConversionParameters centeredBilinear = FullRange(YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear);
        YCbCrConversionParameters cositedBilinear = FullRange(YCbCrChromaPositioning.Cosited, YCbCrUpsamplingMode.Bilinear);
        YCbCrConversionParameters centeredNearest = FullRange(YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.NearestNeighbor);
        YCbCrConversionParameters cositedNearest = FullRange(YCbCrChromaPositioning.Cosited, YCbCrUpsamplingMode.NearestNeighbor);

        RgbByteImage first = YCbCrConverter.Convert(source, centeredBilinear, new RawLimits(), CancellationToken.None);
        RgbByteImage second = YCbCrConverter.Convert(source, cositedBilinear, new RawLimits(), CancellationToken.None);
        RgbByteImage third = YCbCrConverter.Convert(source, centeredNearest, new RawLimits(), CancellationToken.None);
        RgbByteImage fourth = YCbCrConverter.Convert(source, cositedNearest, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(Blue(first), Is.EqualTo(new byte[] { 128, 184, 255, 255 }));
            Assert.That(Blue(second), Is.EqualTo(new byte[] { 128, 241, 255, 255 }));
            Assert.That(Blue(third), Is.EqualTo(new byte[] { 128, 128, 255, 255 }));
            Assert.That(Blue(fourth), Is.EqualTo(new byte[] { 128, 255, 255, 255 }));
        });
    }

    [Test]
    public void BaselineJpegPreviewPlanesFlowThroughSubsampledConversion()
    {
        BaselineJpegImage preview = new BaselineJpegImage();
        preview.Width = 5;
        preview.Height = 3;
        preview.Precision = 8;
        preview.Planes.Add(BaselinePlane(1, 5, 3, 2, 2, Enumerable.Repeat((byte)76, 15).ToArray()));
        preview.Planes.Add(BaselinePlane(2, 3, 2, 1, 1, Enumerable.Repeat((byte)85, 6).ToArray()));
        preview.Planes.Add(BaselinePlane(3, 3, 2, 1, 1, Enumerable.Repeat((byte)255, 6).ToArray()));

        byte[] encoded = BaselineJpegEncoder.Encode(preview, new BaselineJpegEncodeParameters(100, 2, 2, 0), new RawLimits(), CancellationToken.None);
        BaselineJpegImage decoded = BaselineJpegCodec.Decode(encoded, new RawLimits(), CancellationToken.None);
        RgbByteImage result = YCbCrConverter.Convert(decoded, FullRange(YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear), new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(((result.Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Where((value, index) => index % 3 == 0) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.All.GreaterThanOrEqualTo(253));
            Assert.That(result.Samples.Where((value, index) => index % 3 != 0), Is.All.LessThanOrEqualTo(1));
        });
    }

    [Test]
    public void FourteenBitLosslessCanonStyleComponentsUseCallerRangesAndRgbClipping()
    {
        LosslessJpegImage source = new LosslessJpegImage();
        source.Width = 2;
        source.Height = 1;
        source.Precision = 14;
        source.Planes.Add(LosslessPlane(1, new ushort[] { 8192, 8192 }));
        source.Planes.Add(LosslessPlane(2, new ushort[] { 8192, 0 }));
        source.Planes.Add(LosslessPlane(3, new ushort[] { 8192, 16383 }));
        YCbCrConversionParameters parameters = new YCbCrConversionParameters(0.299, 0.114, 0, 16383, 8192, 16383, 8192, 16383, YCbCrChromaPositioning.Cosited, YCbCrUpsamplingMode.NearestNeighbor);

        RgbByteImage result = YCbCrConverter.Convert(source, parameters, new RawLimits(), CancellationToken.None);

        Assert.That(result.Samples, Is.EqualTo(new byte[] { 128, 128, 128, 255, 81, 0 }));
    }

    [Test]
    public void CoefficientsSelectTheExpectedColorMatrix()
    {
        YCbCrImageData source = Image(1, 1, 8, 1, 1,
            BytePlane(1, 1, new byte[] { 128 }),
            BytePlane(1, 1, new byte[] { 160 }),
            BytePlane(1, 1, new byte[] { 192 }));
        YCbCrConversionParameters bt601 = FullRange(YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.NearestNeighbor);
        YCbCrConversionParameters bt709 = new YCbCrConversionParameters(0.2126, 0.0722, 0, 255, 128, 255, 128, 255, YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.NearestNeighbor);

        RgbByteImage first = YCbCrConverter.Convert(source, bt601, new RawLimits(), CancellationToken.None);
        RgbByteImage second = YCbCrConverter.Convert(source, bt709, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Samples, Is.EqualTo(new byte[] { 218, 71, 185 }));
            Assert.That(second.Samples, Is.EqualTo(new byte[] { 229, 92, 187 }));
        });
    }

    [Test]
    public void InvalidRangesGeometrySamplesModesLimitsAndCancellationAreRejected()
    {
        YCbCrImageData valid = Image(2, 2, 8, 1, 1,
            BytePlane(2, 2, new byte[4]), BytePlane(2, 2, new byte[4]), BytePlane(2, 2, new byte[4]));
        YCbCrImageData badGeometry = Image(2, 2, 8, 2, 2,
            BytePlane(2, 2, new byte[4]), BytePlane(2, 2, new byte[4]), BytePlane(1, 1, new byte[1]));
        YCbCrImageData badSample = Image(1, 1, 7, 1, 1,
            WordPlane(1, 1, new ushort[] { 128 }), WordPlane(1, 1, new ushort[] { 0 }), WordPlane(1, 1, new ushort[] { 0 }));
        YCbCrConversionParameters badWeights = new YCbCrConversionParameters(0.9, 0.2, 0, 255, 128, 255, 128, 255, YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear);
        YCbCrConversionParameters badRange = new YCbCrConversionParameters(0.299, 0.114, 255, 0, 128, 255, 128, 255, YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear);
        YCbCrConversionParameters badMode = FullRange((YCbCrChromaPositioning)99, YCbCrUpsamplingMode.Bilinear);
        RawLimits limits = new RawLimits();
        limits.MaxDecodedBytes = 27;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => YCbCrConverter.Convert((YCbCrImageData?)null, FullRange(YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => YCbCrConverter.Convert(valid, null, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => YCbCrConverter.Convert(valid, badWeights, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => YCbCrConverter.Convert(valid, badRange, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => YCbCrConverter.Convert(valid, badMode, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => YCbCrConverter.Convert(badGeometry, FullRange(YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => YCbCrConverter.Convert(badSample, FullRange(YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawLimitException>(() => YCbCrConverter.Convert(valid, FullRange(YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear), limits, CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => YCbCrConverter.Convert(valid, FullRange(YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear), new RawLimits(), cancellation.Token));
        });
    }

    private static YCbCrConversionParameters FullRange(YCbCrChromaPositioning positioning, YCbCrUpsamplingMode upsampling)
    {
        return new YCbCrConversionParameters(0.299, 0.114, 0, 255, 128, 255, 128, 255, positioning, upsampling);
    }

    private static byte[]? Blue(RgbByteImage image)
    {
        return (image.Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Where((value, index) => index % 3 == 2).ToArray();
    }

    private static YCbCrImageData Image(int width, int height, int precision, int horizontal, int vertical, YCbCrPlaneData y, YCbCrPlaneData cb, YCbCrPlaneData cr)
    {
        YCbCrImageData image = new YCbCrImageData();
        image.Width = width;
        image.Height = height;
        image.Precision = precision;
        image.HorizontalSubsampling = horizontal;
        image.VerticalSubsampling = vertical;
        image.Y = y;
        image.Cb = cb;
        image.Cr = cr;
        return image;
    }

    private static YCbCrPlaneData BytePlane(int width, int height, byte[] samples)
    {
        YCbCrPlaneData plane = new YCbCrPlaneData();
        plane.Width = width;
        plane.Height = height;
        plane.ByteSamples = samples;
        return plane;
    }

    private static YCbCrPlaneData WordPlane(int width, int height, ushort[] samples)
    {
        YCbCrPlaneData plane = new YCbCrPlaneData();
        plane.Width = width;
        plane.Height = height;
        plane.WordSamples = samples;
        return plane;
    }

    private static BaselineJpegPlane BaselinePlane(byte identifier, int width, int height, int horizontal, int vertical, byte[] samples)
    {
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = identifier;
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = horizontal;
        plane.VerticalSampling = vertical;
        plane.Samples = samples;
        return plane;
    }

    private static LosslessJpegPlane LosslessPlane(byte identifier, ushort[] samples)
    {
        LosslessJpegPlane plane = new LosslessJpegPlane();
        plane.Identifier = identifier;
        plane.Width = 2;
        plane.Height = 1;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = samples;
        return plane;
    }
}
