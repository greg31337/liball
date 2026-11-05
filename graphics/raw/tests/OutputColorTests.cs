using raw;

namespace raw.Tests;

public class OutputColorTests
{
    [TestCase(RawColorSpace.Srgb, RawColorSpace.XyzD65, 0.95047, 1.0, 1.08883)]
    [TestCase(RawColorSpace.DisplayP3, RawColorSpace.XyzD65, 0.95047, 1.0, 1.08883)]
    [TestCase(RawColorSpace.AdobeRgb, RawColorSpace.XyzD65, 0.95047, 1.0, 1.08883)]
    [TestCase(RawColorSpace.ProPhotoRgb, RawColorSpace.XyzD50, 0.96422, 1.0, 0.82521)]
    [TestCase(RawColorSpace.Rec2020, RawColorSpace.XyzD65, 0.95047, 1.0, 1.08883)]
    [TestCase(RawColorSpace.AcesCg, RawColorSpace.XyzD65, 0.95047, 1.0, 1.08883)]
    public void BuiltInRgbTransformsMapSourceWhiteToNeutral(RawColorSpace target, RawColorSpace source, double x, double y, double z)
    {
        RawRenderOptions options = Options(target, RawTransferFunction.Linear);
        options.ChromaticAdaptation = RawChromaticAdaptationMode.Bradford;

        RawRgbImage converted = RawRenderer.ConvertOutputColor(Xyz(source, new float[] { (float)x, (float)y, (float)z }), options);

        Assert.Multiple(() =>
        {
            Assert.That(converted.FloatingSamples, Is.EqualTo(new float[] { 1.0F, 1.0F, 1.0F }).Within(0.0003F));
            Assert.That(converted.ColorSpace, Is.EqualTo(target));
            Assert.That(converted.TransferFunction, Is.EqualTo(RawTransferFunction.Linear));
            Assert.That(converted.RowStrideSamples, Is.EqualTo(3));
            Assert.That(converted.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void PublishedSrgbPrimaryConvertsToUnitRed()
    {
        RawRgbImage xyz = Xyz(RawColorSpace.XyzD65, new float[] { 0.4124564F, 0.2126729F, 0.0193339F });
        RawRenderOptions options = Options(RawColorSpace.Srgb, RawTransferFunction.Linear);
        options.ChromaticAdaptation = RawChromaticAdaptationMode.None;

        RawRgbImage converted = RawRenderer.ConvertOutputColor(xyz, options);

        Assert.That(converted.FloatingSamples, Is.EqualTo(new float[] { 1.0F, 0.0F, 0.0F }).Within(0.000001F));
    }

    [Test]
    public void XyzAdaptationAndRawCameraOutputPreserveExpectedChannels()
    {
        RawRgbImage d65 = Xyz(RawColorSpace.XyzD65, new float[] { 0.95047F, 1.0F, 1.08883F, 0.4F });
        RawRenderOptions d50Options = Options(RawColorSpace.XyzD50, RawTransferFunction.Linear);
        d50Options.ChromaticAdaptation = RawChromaticAdaptationMode.Bradford;
        RawRgbImage adapted = RawRenderer.ConvertOutputColor(d65, d50Options);

        RawChannelDescription[] bgrChannels = new RawChannelDescription[]
        {
            new RawChannelDescription("B", RawChannelKind.Blue),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("A", RawChannelKind.Alpha)
        };
        float[] bgrSamples = new float[] { 0.3F, 0.2F, 0.1F, 0.75F };
        RawRgbImage bgr = new RawRgbImage(1, 1, 4, 4, null, null, bgrSamples, bgrChannels, RawColorSpace.RawCamera, RawTransferFunction.Linear, RawOrientation.Rotate90Clockwise, false, false, null);
        RawRgbImage raw = RawRenderer.ConvertOutputColor(bgr, Options(RawColorSpace.RawCamera, RawTransferFunction.Linear));

        Assert.Multiple(() =>
        {
            Assert.That(adapted.FloatingSamples, Is.EqualTo(new float[] { 0.96422F, 1.0F, 0.82521F, 0.4F }).Within(0.000002F));
            Assert.That(adapted.Channels[0].Name, Is.EqualTo("X"));
            Assert.That(adapted.Channels[3].Kind, Is.EqualTo(RawChannelKind.Alpha));
            Assert.That(raw.FloatingSamples, Is.EqualTo(new float[] { 0.1F, 0.2F, 0.3F, 0.75F }));
            Assert.That(raw.Channels[0].Kind, Is.EqualTo(RawChannelKind.Red));
            Assert.That(raw.Orientation, Is.EqualTo(RawOrientation.Rotate90Clockwise));
            Assert.That(raw.OrientationApplied, Is.False);
            Assert.That(raw.FloatingSamples, Is.Not.SameAs(bgrSamples));
        });
    }

    [Test]
    public void StandardTransferFunctionsMatchReferenceValuesAndPreserveSign()
    {
        double srgb = RawOutputColorProcessor.Encode(0.18, RawTransferFunction.Srgb, 2.2);
        double gamma = RawOutputColorProcessor.Encode(0.25, RawTransferFunction.Gamma, 2.0);
        double rec2020 = RawOutputColorProcessor.Encode(0.18, RawTransferFunction.Rec2020, 2.2);
        double proPhoto = RawOutputColorProcessor.Encode(0.18, RawTransferFunction.ProPhotoRgb, 2.2);

        Assert.Multiple(() =>
        {
            Assert.That(srgb, Is.EqualTo(0.46135613).Within(0.00000001));
            Assert.That(gamma, Is.EqualTo(0.5).Within(0.00000001));
            Assert.That(rec2020, Is.EqualTo(0.40884811).Within(0.00000001));
            Assert.That(proPhoto, Is.EqualTo(0.38571142).Within(0.00000001));
            Assert.That(RawOutputColorProcessor.Encode(-0.18, RawTransferFunction.Srgb, 2.2), Is.EqualTo(-srgb).Within(0.00000001));
            Assert.That(RawOutputColorProcessor.Encode(0.0031308, RawTransferFunction.Srgb, 2.2), Is.EqualTo(0.040449936).Within(0.000000001));
            Assert.That(RawOutputColorProcessor.Encode(0.001, RawTransferFunction.ProPhotoRgb, 2.2), Is.EqualTo(0.016).Within(0.000000001));
        });
    }

    [Test]
    public void PublicConversionAppliesTransferFunctionAndPreservesAlpha()
    {
        RawRgbImage xyz = Xyz(RawColorSpace.XyzD65, new float[] { 0.4124564F, 0.2126729F, 0.0193339F, 0.35F });
        RawRenderOptions options = Options(RawColorSpace.Srgb, RawTransferFunction.Srgb);
        options.ChromaticAdaptation = RawChromaticAdaptationMode.None;

        RawRgbImage converted = RawRenderer.ConvertOutputColor(xyz, options);

        Assert.Multiple(() =>
        {
            Assert.That(converted.FloatingSamples, Is.EqualTo(new float[] { 1.0F, 0.0F, 0.0F, 0.35F }).Within(0.00001F));
            Assert.That(converted.TransferFunction, Is.EqualTo(RawTransferFunction.Srgb));
            Assert.That(converted.Channels[3].Kind, Is.EqualTo(RawChannelKind.Alpha));
        });
    }

    [Test]
    public void InvalidInputsTransfersSamplesAndCancellationAreRejected()
    {
        RawRgbImage xyz = Xyz(RawColorSpace.XyzD65, new float[] { 0.2F, 0.3F, 0.4F });
        RawRgbImage nonlinear = Xyz(RawColorSpace.XyzD65, new float[] { 0.2F, 0.3F, 0.4F });
        nonlinear.TransferFunction = RawTransferFunction.Srgb;
        RawRgbImage notXyz = Xyz(RawColorSpace.XyzD65, new float[] { 0.2F, 0.3F, 0.4F });
        notXyz.Channels[0] = new RawChannelDescription("R", RawChannelKind.Red);
        RawRgbImage nonFinite = Xyz(RawColorSpace.XyzD65, new float[] { float.NaN, 0.3F, 0.4F });
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawRenderOptions cancelled = Options(RawColorSpace.Srgb, RawTransferFunction.Srgb);
        cancelled.CancellationToken = cancellation.Token;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ConvertOutputColor(xyz, Options(RawColorSpace.AcesCg, RawTransferFunction.Srgb)));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ConvertOutputColor(xyz, Options(RawColorSpace.RawCamera, RawTransferFunction.Linear)));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ConvertOutputColor(nonlinear, Options(RawColorSpace.Srgb, RawTransferFunction.Srgb)));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ConvertOutputColor(notXyz, Options(RawColorSpace.Srgb, RawTransferFunction.Srgb)));
            Assert.Throws<RawFormatException>(() => RawRenderer.ConvertOutputColor(nonFinite, Options(RawColorSpace.Srgb, RawTransferFunction.Srgb)));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.ConvertOutputColor(xyz, cancelled));
        });
    }

    [Test]
    public void D60IsAValidProfileIlluminant()
    {
        RawColorProfile profile = new RawColorProfile("ACES D60", RawColorIlluminant.D60, 0.0, RawMatrixMath.Identity(), null, null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Validate().IsValid(), Is.True);
            Assert.That(RawChromaticAdaptation.WhitePoint(RawColorIlluminant.D60, 0.0), Is.EqualTo(new double[] { 0.95265, 1.0, 1.00883 }));
            Assert.That(RawChromaticAdaptation.Temperature(profile), Is.EqualTo(6000.0));
        });
    }

    private static RawRenderOptions Options(RawColorSpace colorSpace, RawTransferFunction transfer)
    {
        RawRenderOptions options = new RawRenderOptions();
        options.ColorSpace = colorSpace;
        options.TransferFunction = transfer;
        options.OutputBitDepth = 32;
        return options;
    }

    private static RawRgbImage Xyz(RawColorSpace colorSpace, float[] samples)
    {
        int channelCount = samples.Length == 4 ? 4 : 3;
        RawChannelDescription[] channels;
        if (channelCount == 4)
        {
            channels = new RawChannelDescription[]
            {
                new RawChannelDescription("X", RawChannelKind.Custom),
                new RawChannelDescription("Y", RawChannelKind.Custom),
                new RawChannelDescription("Z", RawChannelKind.Custom),
                new RawChannelDescription("A", RawChannelKind.Alpha)
            };
        }
        else
        {
            channels = new RawChannelDescription[]
            {
                new RawChannelDescription("X", RawChannelKind.Custom),
                new RawChannelDescription("Y", RawChannelKind.Custom),
                new RawChannelDescription("Z", RawChannelKind.Custom)
            };
        }
        return new RawRgbImage(1, 1, channelCount, channelCount, null, null, samples, channels, colorSpace, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
    }
}
