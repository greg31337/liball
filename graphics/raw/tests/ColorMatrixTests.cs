using raw;

namespace raw.Tests;

public class ColorMatrixTests
{
    [Test]
    public void ManagedMatrixMathMultipliesInvertsInterpolatesAndTransformsVectors()
    {
        double[] matrix = new double[]
        {
            1, 2, 3,
            0, 1, 4,
            5, 6, 0
        };
        double[] inverse = RawMatrixMath.Invert(matrix);
        double[] identity = RawMatrixMath.Multiply(matrix, inverse);
        double[] vector = RawMatrixMath.MultiplyVector(matrix, new double[] { 1, 2, 3 });
        double[] interpolated = RawMatrixMath.Interpolate(RawMatrixMath.Identity(), RawMatrixMath.Diagonal(3, 3, 3), 0.25);

        Assert.Multiple(() =>
        {
            Assert.That(inverse, Is.EqualTo(new double[] { -24, 18, 5, 20, -15, -4, -5, 4, 1 }).Within(0.000001));
            Assert.That(identity, Is.EqualTo(RawMatrixMath.Identity()).Within(0.000001));
            Assert.That(vector, Is.EqualTo(new double[] { 14, 14, 17 }).Within(0.000001));
            Assert.That(interpolated, Is.EqualTo(RawMatrixMath.Diagonal(1.5, 1.5, 1.5)).Within(0.000001));
            Assert.Throws<RawProfileException>(() => RawMatrixMath.Invert(new double[] { 1, 2, 3, 2, 4, 6, 0, 0, 0 }));
        });
    }

    [Test]
    public void BradfordAndCat02AdaptSourceWhiteExactlyToTargetWhite()
    {
        double[] d65 = RawChromaticAdaptation.WhitePoint(RawColorIlluminant.D65, 0.0);
        double[] d50 = RawChromaticAdaptation.WhitePoint(RawColorIlluminant.D50, 0.0);
        double[] bradford = RawChromaticAdaptation.Create(d65, d50, RawChromaticAdaptationMode.Bradford);
        double[] cat02 = RawChromaticAdaptation.Create(d65, d50, RawChromaticAdaptationMode.Cat02);
        double[] unchanged = RawChromaticAdaptation.Create(d65, d50, RawChromaticAdaptationMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(bradford, Is.EqualTo(new double[]
            {
                1.04781, 0.02289, -0.05013,
                0.02954, 0.99048, -0.01705,
                -0.00923, 0.01504, 0.75213
            }).Within(0.00005));
            Assert.That(RawMatrixMath.MultiplyVector(bradford, d65), Is.EqualTo(d50).Within(0.000001));
            Assert.That(RawMatrixMath.MultiplyVector(cat02, d65), Is.EqualTo(d50).Within(0.000001));
            Assert.That(cat02, Is.Not.EqualTo(bradford).AsCollection);
            Assert.That(unchanged, Is.EqualTo(RawMatrixMath.Identity()));
        });
    }

    [Test]
    public void EffectiveProfileMatrixPrefersForwardAppliesCalibrationAndFallsBackToReduction()
    {
        RawColorProfile forward = Profile("forward", 6504, RawMatrixMath.Diagonal(9, 9, 9));
        forward.ForwardMatrix = RawMatrixMath.Diagonal(2, 4, 8);
        forward.CameraCalibration = RawMatrixMath.Diagonal(0.5, 0.25, 0.125);
        RawColorProfile reduction = Profile("reduction", 6504, null);
        reduction.ReductionMatrix = RawMatrixMath.Diagonal(2, 4, 8);

        double[] forwardEffective = RawCameraColorProcessor.EffectiveMatrix(forward);
        double[] reductionEffective = RawCameraColorProcessor.EffectiveMatrix(reduction);

        Assert.Multiple(() =>
        {
            Assert.That(forwardEffective, Is.EqualTo(RawMatrixMath.Identity()).Within(0.000001));
            Assert.That(reductionEffective, Is.EqualTo(RawMatrixMath.Diagonal(0.5, 0.25, 0.125)).Within(0.000001));
            Assert.That(reduction.HasMatrixProfile(), Is.True);
            Assert.That(reduction.Validate().IsValid(), Is.True);
        });
    }

    [Test]
    public void ThreeIlluminantInterpolationUsesReciprocalTemperatureAndAdjacentProfiles()
    {
        RawColorProfile low = Profile("low", 3000, RawMatrixMath.Diagonal(1, 1, 1));
        RawColorProfile middle = Profile("middle", 6000, RawMatrixMath.Diagonal(2, 2, 2));
        RawColorProfile high = Profile("high", 9000, RawMatrixMath.Diagonal(4, 4, 4));
        middle.WhitePoint = new double[] { 0.9, 1.0, 1.0 };
        high.WhitePoint = new double[] { 1.1, 1.0, 0.8 };

        RawInterpolatedColorProfile selected = RawCameraColorProcessor.InterpolateProfiles(new RawColorProfile[] { high, low, middle }, 7500);
        RawInterpolatedColorProfile twoProfiles = RawCameraColorProcessor.InterpolateProfiles(new RawColorProfile[] { low, middle }, 4000);
        RawInterpolatedColorProfile below = RawCameraColorProcessor.InterpolateProfiles(new RawColorProfile[] { low, middle, high }, 2000);
        RawInterpolatedColorProfile above = RawCameraColorProcessor.InterpolateProfiles(new RawColorProfile[] { low, middle, high }, 12000);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Fraction, Is.EqualTo(0.6).Within(0.000001));
            Assert.That(selected.Matrix, Is.EqualTo(RawMatrixMath.Diagonal(3.2, 3.2, 3.2)).Within(0.000001));
            Assert.That(selected.WhitePoint, Is.EqualTo(new double[] { 1.02, 1.0, 0.88 }).Within(0.000001));
            Assert.That(twoProfiles.Fraction, Is.EqualTo(0.5).Within(0.000001));
            Assert.That(twoProfiles.Matrix, Is.EqualTo(RawMatrixMath.Diagonal(1.5, 1.5, 1.5)).Within(0.000001));
            Assert.That(below.Matrix, Is.EqualTo(RawMatrixMath.Identity()).Within(0.000001));
            Assert.That(above.Matrix, Is.EqualTo(RawMatrixMath.Diagonal(4, 4, 4)).Within(0.000001));
        });
    }

    [Test]
    public void CameraConversionProducesLinearXyzAndPreservesAlpha()
    {
        RawColorProfile identity = Profile("D65 identity", 6504, RawMatrixMath.Identity());
        identity.Illuminant = RawColorIlluminant.D65;
        identity.IlluminantTemperatureKelvin = 6504;
        identity.WhitePoint = RawChromaticAdaptation.WhitePoint(RawColorIlluminant.D65, 0.0);
        RawRenderOptions options = XyzOptions(RawColorSpace.XyzD65, RawChromaticAdaptationMode.None);
        RawRgbImage rgb = Image(3, new float[] { 0.2F, 0.3F, 0.4F });
        RawRgbImage rgba = Image(4, new float[] { 0.2F, 0.3F, 0.4F, 0.75F });

        RawRgbImage converted = RawRenderer.ConvertCameraToXyz(rgb, new RawColorProfile[] { identity }, 6504, options);
        RawRgbImage convertedAlpha = RawRenderer.ConvertCameraToXyz(rgba, new RawColorProfile[] { identity }, 6504, options);

        Assert.Multiple(() =>
        {
            Assert.That(converted.FloatingSamples, Is.EqualTo(new float[] { 0.2F, 0.3F, 0.4F }).Within(0.000001F));
            Assert.That(convertedAlpha.FloatingSamples, Is.EqualTo(new float[] { 0.2F, 0.3F, 0.4F, 0.75F }).Within(0.000001F));
            Assert.That(converted.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "X", "Y", "Z" }));
            Assert.That(converted.ColorSpace, Is.EqualTo(RawColorSpace.XyzD65));
            Assert.That(converted.TransferFunction, Is.EqualTo(RawTransferFunction.Linear));
            Assert.That(converted.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void CameraConversionAdaptsD65WhiteToD50()
    {
        double[] d65 = RawChromaticAdaptation.WhitePoint(RawColorIlluminant.D65, 0.0);
        double[] d50 = RawChromaticAdaptation.WhitePoint(RawColorIlluminant.D50, 0.0);
        RawColorProfile identity = Profile("D65 identity", 6504, RawMatrixMath.Identity());
        identity.Illuminant = RawColorIlluminant.D65;
        identity.WhitePoint = d65;
        RawRenderOptions options = XyzOptions(RawColorSpace.XyzD50, RawChromaticAdaptationMode.Bradford);

        RawRgbImage converted = RawRenderer.ConvertCameraToXyz(Image(3, d65.Select(value => (float)value).ToArray()), new RawColorProfile[] { identity }, 6504, options);

        Assert.That(converted.FloatingSamples, Is.EqualTo(d50.Select(value => (float)value).ToArray()).Within(0.000001F));
    }

    [Test]
    public void InvalidProfilesTargetsTemperaturesInputsAndCancellationAreRejected()
    {
        RawRgbImage image = Image(3, new float[] { 0.2F, 0.3F, 0.4F });
        RawColorProfile profile = Profile("valid", 6504, RawMatrixMath.Identity());
        RawColorProfile duplicate = Profile("duplicate", 6504, RawMatrixMath.Diagonal(2, 2, 2));
        RawColorProfile singular = Profile("singular", 5000, new double[] { 1, 2, 3, 2, 4, 6, 0, 0, 0 });
        RawRenderOptions xyz = XyzOptions(RawColorSpace.XyzD65, RawChromaticAdaptationMode.Bradford);
        RawRenderOptions wrongTarget = XyzOptions(RawColorSpace.Srgb, RawChromaticAdaptationMode.Bradford);
        RawRgbImage wrongInput = Image(3, new float[] { 0.2F, 0.3F, 0.4F });
        wrongInput.ColorSpace = RawColorSpace.Srgb;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawRenderOptions cancelled = XyzOptions(RawColorSpace.XyzD65, RawChromaticAdaptationMode.Bradford);
        cancelled.CancellationToken = cancellation.Token;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawProfileException>(() => RawRenderer.ConvertCameraToXyz(image, Array.Empty<RawColorProfile>(), 6504, xyz));
            Assert.Throws<RawProfileException>(() => RawCameraColorProcessor.InterpolateProfiles(new RawColorProfile[] { profile, duplicate }, 6504));
            Assert.Throws<RawProfileException>(() => RawRenderer.ConvertCameraToXyz(image, new RawColorProfile[] { singular }, 5000, xyz));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ConvertCameraToXyz(image, new RawColorProfile[] { profile }, 6504, wrongTarget));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ConvertCameraToXyz(wrongInput, new RawColorProfile[] { profile }, 6504, xyz));
            Assert.Throws<RawFormatException>(() => RawRenderer.ConvertCameraToXyz(image, new RawColorProfile[] { profile }, 0, xyz));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.ConvertCameraToXyz(image, new RawColorProfile[] { profile }, 6504, cancelled));
        });
    }

    private static RawColorProfile Profile(string name, double temperature, double[]? colorMatrix)
    {
        return new RawColorProfile(name, RawColorIlluminant.Custom, temperature, colorMatrix, null, null, null, null, null);
    }

    private static RawRenderOptions XyzOptions(RawColorSpace space, RawChromaticAdaptationMode adaptation)
    {
        RawRenderOptions options = new RawRenderOptions();
        options.ColorSpace = space;
        options.ChromaticAdaptation = adaptation;
        return options;
    }

    private static RawRgbImage Image(int channels, float[] samples)
    {
        RawChannelDescription[] descriptions;
        if (channels == 4)
        {
            descriptions = new RawChannelDescription[]
            {
                new RawChannelDescription("R", RawChannelKind.Red),
                new RawChannelDescription("G", RawChannelKind.Green),
                new RawChannelDescription("B", RawChannelKind.Blue),
                new RawChannelDescription("A", RawChannelKind.Alpha)
            };
        }
        else
        {
            descriptions = new RawChannelDescription[]
            {
                new RawChannelDescription("R", RawChannelKind.Red),
                new RawChannelDescription("G", RawChannelKind.Green),
                new RawChannelDescription("B", RawChannelKind.Blue)
            };
        }
        return new RawRgbImage(1, 1, channels, channels, null, null, samples, descriptions, RawColorSpace.RawCamera, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
    }
}
