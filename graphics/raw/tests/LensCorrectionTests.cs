using raw;

namespace raw.Tests;

public class LensCorrectionTests
{
    [Test]
    public void ScalarGainGridIsBilinearlyInterpolatedWithoutMutatingInput()
    {
        RawImageInfo info = GrayInfo(3, 3);
        RawSensorImage image = Sensor(info, Enumerable.Repeat(0.2F, 9).ToArray());
        RawGainMap map = new RawGainMap(0, 0, 3, 3, 2, 2, 1, new double[] { 1, 2, 3, 4 });
        image.GainMaps = new RawGainMap[] { map };
        RawLensCorrectionOptions options = new RawLensCorrectionOptions { ApplyGainMaps = true };

        RawSensorImage result = RawRenderer.CorrectLens(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[]
            {
                0.2F, 0.3F, 0.4F,
                0.4F, 0.5F, 0.6F,
                0.6F, 0.7F, 0.8F
            }).Within(0.000001F));
            Assert.That(image.FloatingSamples, Is.All.EqualTo(0.2F));
            Assert.That(result.FloatingSamples, Is.Not.SameAs(image.FloatingSamples));
            Assert.That(result.GainMaps, Is.SameAs(image.GainMaps));
            Assert.That(result.GainMapsApplied, Is.True);
            Assert.That(result.ChromaticAberrationApplied, Is.False);
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void GainMapCanProvideIndependentPlanesForEveryCfaPhase()
    {
        RawImageInfo info = CfaInfo(4, 4);
        RawSensorImage image = Sensor(info, Enumerable.Repeat(0.1F, 16).ToArray());
        image.GainMaps = new RawGainMap[]
        {
            new RawGainMap(0, 0, 4, 4, 1, 1, 4, new double[] { 2, 3, 4, 5 })
        };
        RawLensCorrectionOptions options = new RawLensCorrectionOptions { ApplyGainMaps = true };

        RawSensorImage result = RawRenderer.CorrectLens(image, options);

        Assert.That(result.FloatingSamples, Is.EqualTo(new float[]
        {
            0.2F, 0.3F, 0.2F, 0.3F,
            0.4F, 0.5F, 0.4F, 0.5F,
            0.2F, 0.3F, 0.2F, 0.3F,
            0.4F, 0.5F, 0.4F, 0.5F
        }).Within(0.000001F));
    }

    [Test]
    public void ChromaticAberrationResamplesOnlyMatchingRedAndBlueCfaPhases()
    {
        RawImageInfo info = CfaInfo(10, 10);
        float[] samples = new float[100];
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                RawCfaColor color = (info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColor(x, y);
                samples[y * 10 + x] = color == RawCfaColor.Red ? x / 10.0F : color == RawCfaColor.Blue ? y / 10.0F : 0.5F;
            }
        }
        RawSensorImage image = Sensor(info, samples);
        image.ChromaticAberration = new RawChromaticAberration(0, 0, 0.5, 1.0);
        RawLensCorrectionOptions options = new RawLensCorrectionOptions { ApplyChromaticAberration = true };

        RawSensorImage result = RawRenderer.CorrectLens(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples[4 * 10 + 4], Is.EqualTo(0.2F).Within(0.000001F));
            Assert.That(result.FloatingSamples[8 * 10 + 8], Is.EqualTo(0.4F).Within(0.000001F));
            Assert.That(result.FloatingSamples[5 * 10 + 5], Is.EqualTo(0.5F));
            Assert.That(result.FloatingSamples[4 * 10 + 5], Is.EqualTo(0.5F));
            Assert.That(image.FloatingSamples[4 * 10 + 4], Is.EqualTo(0.4F));
            Assert.That(result.ChromaticAberration, Is.SameAs(image.ChromaticAberration));
            Assert.That(result.ChromaticAberrationApplied, Is.True);
            Assert.That(result.GainMapsApplied, Is.False);
        });
    }

    [Test]
    public void DisabledCorrectionReturnsEquivalentOwnedFloatingSamples()
    {
        RawImageInfo info = GrayInfo(3, 2);
        ushort[] samples = new ushort[] { 0, 13107, 26214, 39321, 52428, 65535 };
        RawSensorImage image = new RawSensorImage(info, samples, null, 3, true, null, null, null, null, null, null, null, null);

        RawSensorImage result = RawRenderer.CorrectLens(image, new RawLensCorrectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 0, 0.2F, 0.4F, 0.6F, 0.8F, 1 }).Within(0.000001F));
            Assert.That(result.IntegerSamples, Is.Empty);
            Assert.That(result.OwnsSamples, Is.True);
            Assert.That(result.GainMapsApplied, Is.False);
            Assert.That(result.ChromaticAberrationApplied, Is.False);
        });
    }

    [Test]
    public void RequestedCorrectionsRequireCompleteMetadataAndCoverage()
    {
        RawImageInfo grayInfo = GrayInfo(3, 3);
        RawSensorImage missingGain = Sensor(grayInfo, Enumerable.Repeat(0.5F, 9).ToArray());
        RawLensCorrectionOptions gainOptions = new RawLensCorrectionOptions { ApplyGainMaps = true };
        RawSensorImage partialGain = Sensor(grayInfo, Enumerable.Repeat(0.5F, 9).ToArray());
        partialGain.GainMaps = new RawGainMap[] { new RawGainMap(0, 0, 2, 2, 1, 1, 1, new double[] { 1 }) };
        RawSensorImage invalidGain = Sensor(grayInfo, Enumerable.Repeat(0.5F, 9).ToArray());
        invalidGain.GainMaps = new RawGainMap[] { new RawGainMap(0, 0, 3, 3, 1, 1, 1, new double[] { 0 }) };
        RawImageInfo cfaInfo = CfaInfo(4, 4);
        RawSensorImage missingCa = Sensor(cfaInfo, Enumerable.Repeat(0.5F, 16).ToArray());
        RawLensCorrectionOptions caOptions = new RawLensCorrectionOptions { ApplyChromaticAberration = true };

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.CorrectLens(missingGain, gainOptions));
            Assert.Throws<RawFormatException>(() => RawRenderer.CorrectLens(partialGain, gainOptions));
            Assert.Throws<RawFormatException>(() => RawRenderer.CorrectLens(invalidGain, gainOptions));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.CorrectLens(missingCa, caOptions));
        });
    }

    [Test]
    public void CancellationAndRepeatedApplicationAreRejected()
    {
        RawImageInfo info = GrayInfo(3, 3);
        RawSensorImage image = Sensor(info, Enumerable.Repeat(0.25F, 9).ToArray());
        image.GainMaps = new RawGainMap[] { new RawGainMap(0, 0, 3, 3, 1, 1, 1, new double[] { 2 }) };
        RawLensCorrectionOptions apply = new RawLensCorrectionOptions { ApplyGainMaps = true };
        RawSensorImage applied = RawRenderer.CorrectLens(image, apply);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawLensCorrectionOptions cancelled = new RawLensCorrectionOptions { CancellationToken = cancellation.Token };

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => RawRenderer.CorrectLens(applied, apply));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.CorrectLens(image, cancelled));
        });
    }

    private static RawSensorImage Sensor(RawImageInfo info, float[] samples)
    {
        RawSensorImage image = new RawSensorImage(info, null, samples, info.Width, true, null, null, null, null, null, null, null, null);
        image.CropModel = new RawCropModel(info, null);
        return image;
    }

    private static RawImageInfo CfaInfo(int width, int height)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, 16, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.None, RawOrientation.Normal, channels, planes, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb), new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), new RawArea(RawAreaKind.Active, 0, 0, width, height), null, null);
    }

    private static RawImageInfo GrayInfo(int width, int height)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, width, height, 16, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }
}
