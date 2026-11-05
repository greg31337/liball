namespace jpeg.Tests;

internal class ColorSamplingIntegrationTests
{
    [Test]
    public void RgbToYCbCrDownsampleUpsampleAndBack_RoundTripsNeutralChromaPixels()
    {
        global::jpeg.JpegRgbToYCbCrConverter rgbToYCbCr = new global::jpeg.JpegRgbToYCbCrConverter();
        global::jpeg.JpegChromaDownsampler downsampler = new global::jpeg.JpegChromaDownsampler();
        global::jpeg.JpegFastBoxChromaUpsampler upsampler = new global::jpeg.JpegFastBoxChromaUpsampler();
        global::jpeg.JpegYCbCrToRgbConverter yCbCrToRgb = new global::jpeg.JpegYCbCrToRgbConverter();
        byte[] rgb = new byte[]
        {
            0, 0, 0,
            64, 64, 64,
            128, 128, 128,
            255, 255, 255
        };

        byte[] yCbCr = rgbToYCbCr.ConvertPackedRgbToYCbCr(rgb, 2, 2);
        byte[] luminance = ExtractComponent(yCbCr, 0);
        byte[] cb = ExtractComponent(yCbCr, 1);
        byte[] cr = ExtractComponent(yCbCr, 2);
        byte[] downsampledCb = downsampler.Downsample(cb, 2, 2, 2, 1, 1, global::jpeg.JpegChromaDownsamplingFilter.Box);
        byte[] downsampledCr = downsampler.Downsample(cr, 2, 2, 2, 1, 1, global::jpeg.JpegChromaDownsamplingFilter.Box);
        byte[] upsampledCb = upsampler.Upsample(downsampledCb, 1, 1, 1, 2, 2);
        byte[] upsampledCr = upsampler.Upsample(downsampledCr, 1, 1, 1, 2, 2);
        global::jpeg.JpegPlanarImageBuffer planar = CreateYCbCrPlanarImage(luminance, upsampledCb, upsampledCr);

        global::jpeg.JpegInterleavedPixelBuffer roundTrip = yCbCrToRgb.ConvertPlanarToRgb(planar);

        Assert.That(yCbCr, Is.EqualTo(new byte[]
        {
            0, 128, 128,
            64, 128, 128,
            128, 128, 128,
            255, 128, 128
        }));
        Assert.That(downsampledCb, Is.EqualTo(new byte[] { 128 }));
        Assert.That(downsampledCr, Is.EqualTo(new byte[] { 128 }));
        Assert.That(roundTrip.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(roundTrip.Pixels, Is.EqualTo(rgb));
    }

    [Test]
    public void DownsampledChroma_UpsamplesWithFastAndFancyFilters()
    {
        global::jpeg.JpegChromaDownsampler downsampler = new global::jpeg.JpegChromaDownsampler();
        global::jpeg.JpegFastBoxChromaUpsampler fast = new global::jpeg.JpegFastBoxChromaUpsampler();
        global::jpeg.JpegFancyChromaUpsampler fancy = new global::jpeg.JpegFancyChromaUpsampler();

        byte[] downsampled = downsampler.Downsample(
            ChromaDownsamplerTests.CreateFourByFourSource(),
            4,
            4,
            4,
            2,
            2,
            global::jpeg.JpegChromaDownsamplingFilter.Box);
        byte[] fastUpsampled = fast.Upsample(downsampled, 2, 2, 2, 4, 4);
        byte[] fancyUpsampled = fancy.Upsample(downsampled, 2, 2, 2, 4, 4);

        Assert.That(downsampled, Is.EqualTo(new byte[] { 35, 55, 115, 135 }));
        Assert.That(fastUpsampled, Is.EqualTo(new byte[]
        {
            35, 35, 55, 55,
            35, 35, 55, 55,
            115, 115, 135, 135,
            115, 115, 135, 135
        }));
        Assert.That(fancyUpsampled, Is.EqualTo(new byte[]
        {
            35, 40, 50, 55,
            55, 60, 70, 75,
            95, 100, 110, 115,
            115, 120, 130, 135
        }));
    }

    [Test]
    public void Identifier_InfersCommonSubsamplingModesFromFrameSamplingFactors()
    {
        global::jpeg.JpegIdentifier identifier = new global::jpeg.JpegIdentifier(new byte[0]);

        Assert.That(identifier.InferSubsamplingFromFrame(CreateFrame(1, 1, 3)), Is.EqualTo(global::jpeg.JpegSubsampling.Ratio444));
        Assert.That(identifier.InferSubsamplingFromFrame(CreateFrame(2, 1, 3)), Is.EqualTo(global::jpeg.JpegSubsampling.Ratio422));
        Assert.That(identifier.InferSubsamplingFromFrame(CreateFrame(2, 2, 3)), Is.EqualTo(global::jpeg.JpegSubsampling.Ratio420));
        Assert.That(identifier.InferSubsamplingFromFrame(CreateFrame(1, 2, 3)), Is.EqualTo(global::jpeg.JpegSubsampling.Ratio440));
        Assert.That(identifier.InferSubsamplingFromFrame(CreateFrame(4, 1, 3)), Is.EqualTo(global::jpeg.JpegSubsampling.Ratio411));
        Assert.That(identifier.InferSubsamplingFromFrame(CreateFrame(3, 2, 3)), Is.EqualTo(global::jpeg.JpegSubsampling.Custom));
        Assert.That(identifier.InferSubsamplingFromFrame(CreateFrame(1, 1, 1)), Is.EqualTo(global::jpeg.JpegSubsampling.Grayscale));
    }

    public static byte[] ExtractComponent(byte[] packed, int componentIndex)
    {
        byte[] component = new byte[4];
        int index = 0;
        while (index < component.Length)
        {
            component[index] = packed[index * 3 + componentIndex];
            index = index + 1;
        }

        return component;
    }

    public static global::jpeg.JpegPlanarImageBuffer CreateYCbCrPlanarImage(byte[] luminance, byte[] cb, byte[] cr)
    {
        global::jpeg.JpegPlanarImageBuffer image = new global::jpeg.JpegPlanarImageBuffer();
        image.Width = 2;
        image.Height = 2;
        image.ComponentCount = 3;
        image.Components.Add(YCbCrToRgbConverterTests.CreateComponent(2, 2, 2, luminance));
        image.Components.Add(YCbCrToRgbConverterTests.CreateComponent(2, 2, 2, cb));
        image.Components.Add(YCbCrToRgbConverterTests.CreateComponent(2, 2, 2, cr));
        return image;
    }

    public static global::jpeg.JpegFrameHeader CreateFrame(int horizontalSampling, int verticalSampling, int componentCount)
    {
        global::jpeg.JpegFrameHeader frame = new global::jpeg.JpegFrameHeader();
        frame.ComponentCount = componentCount;
        int index = 0;
        while (index < componentCount)
        {
            global::jpeg.JpegFrameComponent component = new global::jpeg.JpegFrameComponent();
            component.Identifier = index + 1;
            component.HorizontalSamplingFactor = 1;
            component.VerticalSamplingFactor = 1;
            if (index == 0)
            {
                component.HorizontalSamplingFactor = horizontalSampling;
                component.VerticalSamplingFactor = verticalSampling;
            }

            frame.Components.Add(component);
            index = index + 1;
        }

        return frame;
    }
}
