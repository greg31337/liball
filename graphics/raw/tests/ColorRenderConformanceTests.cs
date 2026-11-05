using raw;

namespace raw.Tests;

public class ColorRenderConformanceTests
{
    [Test]
    public void PublishedRgbToXyzMatricesMapEveryPrimaryBackToItsUnitVector()
    {
        AssertPublishedPrimaries(RawColorSpace.Srgb, RawColorSpace.XyzD65, new double[]
        {
            0.4124564, 0.3575761, 0.1804375,
            0.2126729, 0.7151522, 0.0721750,
            0.0193339, 0.1191920, 0.9503041
        });
        AssertPublishedPrimaries(RawColorSpace.DisplayP3, RawColorSpace.XyzD65, new double[]
        {
            0.48657095, 0.26566769, 0.19821729,
            0.22897456, 0.69173852, 0.07928691,
            0.0, 0.04511338, 1.04394437
        });
        AssertPublishedPrimaries(RawColorSpace.AdobeRgb, RawColorSpace.XyzD65, new double[]
        {
            0.5767309, 0.1855540, 0.1881852,
            0.2973769, 0.6273491, 0.0752741,
            0.0270343, 0.0706872, 0.9911085
        });
        AssertPublishedPrimaries(RawColorSpace.ProPhotoRgb, RawColorSpace.XyzD50, new double[]
        {
            0.7976749, 0.1351917, 0.0313534,
            0.2880402, 0.7118741, 0.0000857,
            0.0, 0.0, 0.8252100
        });
        AssertPublishedPrimaries(RawColorSpace.Rec2020, RawColorSpace.XyzD65, new double[]
        {
            0.6369580, 0.1446169, 0.1688810,
            0.2627002, 0.6779981, 0.0593017,
            0.0, 0.0280727, 1.0609851
        });
        AssertPublishedPrimaries(RawColorSpace.AcesCg, RawColorSpace.XyzD65, new double[]
        {
            0.66245418, 0.13400421, 0.15618769,
            0.27222872, 0.67408177, 0.05368952,
            -0.00557465, 0.00406073, 1.01033910
        });
    }

    [Test]
    public void SyntheticNeutralAndColorTargetsMatchIndependentSrgbImageOracle()
    {
        double[] targetLinear = new double[]
        {
            0.0, 0.0, 0.0,
            0.18, 0.18, 0.18,
            0.9, 0.9, 0.9,
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0,
            0.75, 0.25, 0.10,
            0.10, 0.60, 0.90
        };
        double[] srgbToXyz = new double[]
        {
            0.4124564, 0.3575761, 0.1804375,
            0.2126729, 0.7151522, 0.0721750,
            0.0193339, 0.1191920, 0.9503041
        };
        float[] cameraSamples = new float[targetLinear.Length];
        byte[] expected = new byte[targetLinear.Length];
        int pixel = 0;
        while (pixel < targetLinear.Length / 3)
        {
            int index = pixel * 3;
            double red = targetLinear[index];
            double green = targetLinear[index + 1];
            double blue = targetLinear[index + 2];
            cameraSamples[index] = (float)(srgbToXyz[0] * red + srgbToXyz[1] * green + srgbToXyz[2] * blue);
            cameraSamples[index + 1] = (float)(srgbToXyz[3] * red + srgbToXyz[4] * green + srgbToXyz[5] * blue);
            cameraSamples[index + 2] = (float)(srgbToXyz[6] * red + srgbToXyz[7] * green + srgbToXyz[8] * blue);
            expected[index] = IndependentByte(IndependentSrgb(red));
            expected[index + 1] = IndependentByte(IndependentSrgb(green));
            expected[index + 2] = IndependentByte(IndependentSrgb(blue));
            pixel++;
        }
        RawSensorImage sensor = RgbSensor(targetLinear.Length / 3, cameraSamples, new RawColorProfile[] { IdentityD65Profile() });
        RawRenderOptions options = new RawRenderOptions();
        options.DemosaicMode = RawDemosaicMode.None;
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;
        options.ColorSpace = RawColorSpace.Srgb;
        options.TransferFunction = RawTransferFunction.Srgb;
        options.GamutMode = RawGamutMode.Clip;
        options.OutputBitDepth = 8;
        options.ApplyOrientation = false;

        RawRgbImage result = RawRenderer.Render(sensor, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.ByteSamples, Is.EqualTo(expected));
            Assert.That(result.Width, Is.EqualTo(8));
            Assert.That(result.ColorSpace, Is.EqualTo(RawColorSpace.Srgb));
            Assert.That(result.TransferFunction, Is.EqualTo(RawTransferFunction.Srgb));
        });
    }

    [Test]
    public void PublishedTransferFunctionVectorsMatchReferenceValues()
    {
        double[] inputs = new double[] { 0.0, 0.001, 0.0031308, 0.018, 0.18, 0.5, 1.0 };
        double[] srgb = new double[] { 0.0, 0.01292, 0.040449936, 0.142825681303039, 0.461356129500442, 0.735356983052449, 1.0 };
        double[] rec2020 = new double[] { 0.0, 0.0045, 0.0140886, 0.081, 0.408848108891225, 0.705435553055618, 1.0 };
        double[] proPhoto = new double[] { 0.0, 0.016, 0.0406160236108343, 0.107326495147998, 0.385711424751138, 0.680395000087188, 1.0 };
        double[] gamma = new double[] { 0.0, 0.0432876128108306, 0.0727212794870913, 0.161043072507470, 0.458656446864381, 0.729740052840723, 1.0 };

        int index = 0;
        while (index < inputs.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(RawOutputColorProcessor.Encode(inputs[index], RawTransferFunction.Srgb, 2.2), Is.EqualTo(srgb[index]).Within(0.000000000001));
                Assert.That(RawOutputColorProcessor.Encode(inputs[index], RawTransferFunction.Rec2020, 2.2), Is.EqualTo(rec2020[index]).Within(0.000000000001));
                Assert.That(RawOutputColorProcessor.Encode(inputs[index], RawTransferFunction.ProPhotoRgb, 2.2), Is.EqualTo(proPhoto[index]).Within(0.000000000001));
                Assert.That(RawOutputColorProcessor.Encode(inputs[index], RawTransferFunction.Gamma, 2.2), Is.EqualTo(gamma[index]).Within(0.000000000001));
            });
            index++;
        }
    }

    [Test]
    public void ParsedMatrixTrcIccFixtureRoundTripsColorSamples()
    {
        RawIccProfile fixture = RawIccProfile.Parse(IccCodecTests.CreateProfile(), new RawLimits());
        RawRgbImage encoded = IccImage(new float[] { 0.2F, 0.5F, 0.8F, 0.35F });

        RawRgbImage xyz = RawRenderer.ConvertIccToXyz(encoded, fixture, new RawIccTransformOptions());
        RawRgbImage roundTrip = RawRenderer.ConvertXyzToIcc(xyz, fixture, new RawIccTransformOptions());

        Assert.Multiple(() =>
        {
            Assert.That(fixture.HasMatrixTrc(), Is.True);
            Assert.That(xyz.ColorSpace, Is.EqualTo(RawColorSpace.XyzD50));
            Assert.That(roundTrip.FloatingSamples, Is.EqualTo(encoded.FloatingSamples).Within(0.00003F));
            Assert.That(roundTrip.FloatingSamples[3], Is.EqualTo(0.35F));
        });
    }

    [Test]
    public void CompleteRendererPreservesDistinctHighlightPolicies()
    {
        float[] samples = new float[]
        {
            0.4F, 0.2F, 0.1F,
            0.7F, 0.2F, 0.1F,
            0.4F, 0.2F, 0.1F
        };
        RawSensorImage sensor = RgbSensor(3, samples, null);

        RawRgbImage clipped = RawRenderer.Render(sensor, RawOptions(RawHighlightMode.Clip));
        RawRgbImage blended = RawRenderer.Render(sensor, RawOptions(RawHighlightMode.Blend));
        RawRgbImage reconstructed = RawRenderer.Render(sensor, RawOptions(RawHighlightMode.Reconstruct));

        Assert.Multiple(() =>
        {
            Assert.That(Pixel(clipped, 1), Is.EqualTo(new float[] { 1.0F, 0.4F, 0.2F }).Within(0.000001F));
            Assert.That(Pixel(blended, 1), Is.EqualTo(new float[] { 1.0F, 0.285714F, 0.142857F }).Within(0.000002F));
            Assert.That(Pixel(reconstructed, 1), Is.EqualTo(new float[] { 0.8F, 0.4F, 0.2F }).Within(0.000002F));
        });
    }

    private static void AssertPublishedPrimaries(RawColorSpace target, RawColorSpace sourceSpace, double[] rgbToXyz)
    {
        RawRenderOptions options = new RawRenderOptions();
        options.ColorSpace = target;
        options.TransferFunction = RawTransferFunction.Linear;
        options.ChromaticAdaptation = RawChromaticAdaptationMode.None;
        options.OutputBitDepth = 32;
        int primary = 0;
        while (primary < 3)
        {
            float[] xyz = new float[] { (float)rgbToXyz[primary], (float)rgbToXyz[3 + primary], (float)rgbToXyz[6 + primary] };
            RawRgbImage converted = RawRenderer.ConvertOutputColor(XyzImage(sourceSpace, xyz), options);
            float[] expected = new float[] { primary == 0 ? 1.0F : 0.0F, primary == 1 ? 1.0F : 0.0F, primary == 2 ? 1.0F : 0.0F };
            Assert.That(converted.FloatingSamples, Is.EqualTo(expected).Within(0.000003F), target + " primary " + primary);
            primary++;
        }
    }

    private static double IndependentSrgb(double value)
    {
        return value <= 0.0031308 ? value * 12.92 : 1.055 * Math.Pow(value, 1.0 / 2.4) - 0.055;
    }

    private static byte IndependentByte(double value)
    {
        double bounded = value < 0.0 ? 0.0 : value > 1.0 ? 1.0 : value;
        return (byte)Math.Floor(bounded * 255.0 + 0.5);
    }

    private static RawRenderOptions RawOptions(RawHighlightMode highlightMode)
    {
        RawRenderOptions options = new RawRenderOptions();
        options.DemosaicMode = RawDemosaicMode.None;
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;
        options.HighlightMode = highlightMode;
        options.ExposureCompensation = 1.0;
        options.ColorSpace = RawColorSpace.RawCamera;
        options.TransferFunction = RawTransferFunction.Linear;
        options.OutputBitDepth = 32;
        options.ApplyOrientation = false;
        return options;
    }

    private static RawSensorImage RgbSensor(int width, float[] samples, RawColorProfile[]? profiles)
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        RawPlaneInfo[] planes = new RawPlaneInfo[]
        {
            new RawPlaneInfo(0, "R", RawChannelKind.Red, width, 1, 16, width),
            new RawPlaneInfo(1, "G", RawChannelKind.Green, width, 1, 16, width),
            new RawPlaneInfo(2, "B", RawChannelKind.Blue, width, 1, 16, width)
        };
        RawImageInfo info = new RawImageInfo(0, RawImageKind.Raw, width, 1, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.Rgb, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, 1), null, null, null);
        return new RawSensorImage(info, null, samples, width * 3, true, null, null, null, null, null, null, profiles, null);
    }

    private static RawColorProfile IdentityD65Profile()
    {
        return new RawColorProfile("Identity D65", RawColorIlluminant.D65, 0.0, RawMatrixMath.Identity(), null, null, null, RawChromaticAdaptation.WhitePoint(RawColorIlluminant.D65, 0.0), null);
    }

    private static RawRgbImage XyzImage(RawColorSpace colorSpace, float[] samples)
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("X", RawChannelKind.Custom),
            new RawChannelDescription("Y", RawChannelKind.Custom),
            new RawChannelDescription("Z", RawChannelKind.Custom)
        };
        return new RawRgbImage(1, 1, 3, 3, null, null, samples, channels, colorSpace, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
    }

    private static RawRgbImage IccImage(float[] samples)
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue),
            new RawChannelDescription("A", RawChannelKind.Alpha)
        };
        return new RawRgbImage(1, 1, 4, 4, null, null, samples, channels, RawColorSpace.IccRgb, RawTransferFunction.Icc, RawOrientation.Normal, false, true, null);
    }

    private static float[] Pixel(RawRgbImage image, int x)
    {
        int index = x * image.ChannelCount;
        return new float[] { image.FloatingSamples[index], image.FloatingSamples[index + 1], image.FloatingSamples[index + 2] };
    }
}
