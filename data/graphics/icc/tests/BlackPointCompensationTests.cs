using icc;

namespace icc.Tests;

public class BlackPointCompensationTests
{
    [Test]
    public void ExplicitMediaRelativeOptionsApplyIsoMappingAndAreSnapshotted()
    {
        IccProfile source = GrayProfile(0.01);
        IccProfile destination = GrayProfile(0.04);
        IccTransformFactory factory = new IccTransformFactory();
        IccColorTransform uncompensated = (IccColorTransform)factory.CreateProfileTransform(source, destination,
            IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None, IccInterpolationKind.Multilinear);
        IccTransformOptions options = CompensatedOptions();
        IccColorTransform compensated = (IccColorTransform)factory.CreateConfiguredProfileTransform(source, destination, options);

        options.BlackPointCompensation = false;
        options.ClipOutput = false;
        float[] plainValue = uncompensated.TransformSingleColor(new float[] { 0.5f });
        float[] compensatedValue = compensated.TransformSingleColor(new float[] { 0.5f });

        Assert.Multiple(() =>
        {
            Assert.That(plainValue[0], Is.EqualTo(0.484375f).Within(0.000002f));
            Assert.That(compensatedValue[0], Is.EqualTo(0.5f).Within(0.000002f));
            Assert.That(compensated.Core.Options.BlackPointCompensation, Is.True);
            Assert.That(compensated.Core.Options.ClipOutput, Is.True);
            Assert.That(compensated.Core.Stages.Count(stage => stage.Description.Contains("ISO 18619 black-point link")), Is.EqualTo(1));
        });
    }

    [Test]
    public void ExplicitSequenceOptionsCompensateEveryAdjacentProfileLink()
    {
        IccProfile[] profiles = new IccProfile[] { GrayProfile(0.01), GrayProfile(0.025), GrayProfile(0.04) };
        IccColorTransform transform = (IccColorTransform)new IccTransformFactory()
            .CreateConfiguredProfileSequenceTransform(profiles, CompensatedOptions());

        float[] result = transform.TransformSingleColor(new float[] { 0.4f });

        Assert.Multiple(() =>
        {
            Assert.That(result[0], Is.EqualTo(0.4f).Within(0.000003f));
            Assert.That(transform.Core.Stages.Count(stage => stage.Description.Contains("ISO 18619 black-point link")), Is.EqualTo(2));
        });
    }

    [Test]
    public void CompensationRejectsNonRelativeIntentAndIneligibleProfiles()
    {
        IccProfile valid = GrayProfile(0.01);
        IccTransformOptions perceptual = CompensatedOptions();
        perceptual.RenderingIntent = IccRenderingIntent.Perceptual;
        IccProfile ineligible = GrayProfile(0.01);
        ineligible.Header.ProfileClass = IccProfileClass.DeviceLink;
        IccProfile fallbackSource = PerceptualMpeProfile(IccTagSignatures.DToB0);
        IccProfile fallbackDestination = PerceptualMpeProfile(IccTagSignatures.BToD0);
        IccTransformOptions fallback = CompensatedOptions();
        fallback.IntentFallback = IccIntentFallback.AllowAny;
        IccTransformFactory factory = new IccTransformFactory();

        Assert.Multiple(() =>
        {
            Assert.That(() => factory.CreateConfiguredProfileTransform(valid, valid, perceptual),
                Throws.TypeOf<IccTransformException>().With.Message.Contains("media-relative"));
            Assert.That(() => factory.CreateConfiguredProfileTransform(ineligible, valid, CompensatedOptions()),
                Throws.TypeOf<IccTransformException>().With.Message.Contains("not eligible"));
            Assert.That(() => factory.CreateConfiguredProfileTransform(fallbackSource, fallbackDestination, fallback),
                Throws.TypeOf<IccTransformException>().With.Message.Contains("fallback did not select media-relative"));
        });
    }

    [Test]
    public void DarkestDeviceDetectionChecksBothGrayPolarities()
    {
        IccProfile inverse = GrayProfileWithSamples(new double[] { 1.0, 0.01 });
        ProfileDirectionPlan forward = ProfileTransformCompiler.CompileProfileDirection(inverse,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.None, IccInterpolationKind.Multilinear, 0);

        double[] darkest = BlackPointCompensation.FindDarkestDeviceValue(inverse, forward);

        Assert.That(darkest, Is.EqualTo(new double[] { 1.0 }));
    }

    [Test]
    public void LutBasedDestinationUsesItsRoundTripRampForBlackDetection()
    {
        IccProfile source = RelativeMpeProfile();
        IccProfile destination = RelativeMpeProfile();
        IccColorTransform transform = (IccColorTransform)new IccTransformFactory()
            .CreateConfiguredProfileTransform(source, destination, CompensatedOptions());

        float[] result = transform.TransformSingleColor(new float[] { 0.2f, 0.4f, 0.6f });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new float[] { 0.2f, 0.4f, 0.6f }).Within(0.000002f));
            Assert.That(transform.Core.Stages.Any(stage => stage.Description.Contains("map L* 0.000000 to L* 0.000000")), Is.True);
        });
    }

    [Test]
    public void IsoMappingFixesD50WhiteAndCurveFitFindsShadowIntercept()
    {
        double sourceLightness = 9.0;
        double destinationLightness = 24.0;
        PipelineStage mapping = BlackPointCompensation.CreateMappingStage(sourceLightness, destinationLightness, 0);
        double sourceY = BlackPointCompensation.DecodeLightness(sourceLightness);
        double destinationY = BlackPointCompensation.DecodeLightness(destinationLightness);
        double[] mappedBlack = ExecuteStage(mapping, new double[]
        {
            IccStandardColorimetry.D50X * sourceY,
            sourceY,
            IccStandardColorimetry.D50Z * sourceY
        });
        double[] mappedWhite = ExecuteStage(mapping, new double[]
        {
            IccStandardColorimetry.D50X,
            IccStandardColorimetry.D50Y,
            IccStandardColorimetry.D50Z
        });

        double[] inputRamp = new double[BlackPointCompensation.RampPointCount];
        double[] outputRamp = new double[BlackPointCompensation.RampPointCount];
        double intercept = 100.0 * 30.0 / 255.0;
        int index = 0;
        while (index < inputRamp.Length)
        {
            double x = 100.0 * index / 255.0;
            double normalized = (x - intercept) / (100.0 - intercept);
            inputRamp[index] = x;
            outputRamp[index] = 10.0 + (90.0 * normalized * normalized);
            index = index + 1;
        }
        double fitted = BlackPointCompensation.FitDestinationBlack(inputRamp, outputRamp, 0.0, true);

        Assert.Multiple(() =>
        {
            Assert.That(mappedBlack[1], Is.EqualTo(destinationY).Within(0.000000000001));
            Assert.That(mappedWhite, Is.EqualTo(new double[]
            {
                IccStandardColorimetry.D50X,
                IccStandardColorimetry.D50Y,
                IccStandardColorimetry.D50Z
            }).Within(0.000000000001));
            Assert.That(fitted, Is.EqualTo(intercept).Within(0.001));
        });
    }

    private static IccTransformOptions CompensatedOptions()
    {
        IccTransformOptions options = new IccTransformOptions();
        options.RenderingIntent = IccRenderingIntent.MediaRelativeColorimetric;
        options.IntentFallback = IccIntentFallback.None;
        options.Interpolation = IccInterpolationKind.Multilinear;
        options.BlackPointCompensation = true;
        options.ClipOutput = true;
        return options;
    }

    private static IccProfile GrayProfile(double black)
    {
        return GrayProfileWithSamples(new double[] { black, 1.0 });
    }

    private static IccProfile GrayProfileWithSamples(double[] samples)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version4;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Gray;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Tags = new IccTag[]
        {
            new IccTag
            {
                Signature = IccTagSignatures.GrayTrc,
                Data = new IccTagData
                {
                    Kind = IccTagDataKind.Curve,
                    Curve = new IccCurve { Kind = IccCurveKind.Sampled, Samples = samples }
                }
            }
        };
        return profile;
    }

    private static IccProfile PerceptualMpeProfile(uint tagSignature)
    {
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = IccProcessingElementSignatures.CurveSet;
        element.Kind = IccProcessingElementKind.CurveSet;
        element.InputChannels = 3;
        element.OutputChannels = 3;
        element.Curves = new IccCurve[] { IdentitySegmentedCurve(), IdentitySegmentedCurve(), IdentitySegmentedCurve() };
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = 3;
        pipeline.OutputChannels = 3;
        pipeline.InputRanges = FullRanges(3);
        pipeline.OutputRanges = FullRanges(3);
        pipeline.Elements = new IccProcessingElement[] { element };
        pipeline.IsExecutable = true;
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.MultiProcessElements;
        data.MultiProcessElements = pipeline;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version4;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Tags = new IccTag[] { new IccTag { Signature = tagSignature, Data = data } };
        return profile;
    }

    private static IccProfile RelativeMpeProfile()
    {
        IccProfile profile = PerceptualMpeProfile(IccTagSignatures.DToB1);
        IccProfile reverse = PerceptualMpeProfile(IccTagSignatures.BToD1);
        profile.Tags = new IccTag[] { profile.Tags[0], reverse.Tags[0] };
        return profile;
    }

    private static IccEncodingRange[] FullRanges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = new IccEncodingRange { Minimum = 0.0, Maximum = 1.0 };
            index = index + 1;
        }
        return ranges;
    }

    private static IccCurve IdentitySegmentedCurve()
    {
        IccCurveSegment segment = new IccCurveSegment();
        segment.Kind = IccCurveSegmentKind.Formula;
        segment.FormulaType = 0;
        segment.BreakPoint = double.NegativeInfinity;
        segment.Parameters = new double[] { 1.0, 1.0, 0.0, 0.0 };
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Segmented;
        curve.Segments = new IccCurveSegment[] { segment };
        return curve;
    }

    private static double[] ExecuteStage(PipelineStage stage, double[] input)
    {
        double[] output = new double[stage.OutputChannels];
        PipelineExecutor.ExecuteStage(stage, input, output, System.Array.Empty<int>(), System.Array.Empty<double>());
        return output;
    }
}
