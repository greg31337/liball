using icc;

namespace icc.Tests;

public class AbstractProfileConstructionTests
{
    [Test]
    public void BuildsVersionTwoLabAbstractProfileWithClassicLutAndIntentMetadata()
    {
        IccProfileBuildOptions options = AbstractOptions(IccVersionKind.Version2,
            IccColorSpaceSignatures.Lab);
        options.RenderingIntent = IccRenderingIntent.Saturation;
        options.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Text = "Lab creative adjustment" }
        };
        options.AbstractDefinition!.Lut = ClassicIdentityLut();
        IIccProfileBuilder builder = new IccProfileBuilder();

        IccProfile profile = builder.CreateAbstractProfile(options);
        IccProfile repeated = builder.CreateAbstractProfile(options);
        options.AbstractDefinition.Lut.Clut.Values[0] = 0.75;
        IccTag transform = FindTag(profile, IccTagSignatures.AToB0);
        LutPipelinePlan plan = LutPipelineCompiler.Compile(profile, transform,
            IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ProfileClass, Is.EqualTo(IccProfileClass.Abstract));
            Assert.That(profile.Header.DataColorSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.Lab));
            Assert.That(profile.Header.ProfileConnectionSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.Lab));
            Assert.That(profile.Header.RenderingIntentKind,
                Is.EqualTo(IccRenderingIntent.Saturation));
            Assert.That(transform.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.Lut16));
            Assert.That(transform.Data.Lut!.PcsEncoding,
                Is.EqualTo(IccPcsEncodingKind.Version2));
            Assert.That(transform.Data.Lut.Clut.Values[0], Is.Zero);
            Assert.That(plan.PcsEncoding, Is.EqualTo(IccPcsEncodingKind.Version2));
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.DToB0),
                Is.False);
            Assert.That(validation.IsValid, Is.True);
            Assert.That(profile.OriginalBytes, Is.EqualTo(repeated.OriginalBytes));
        });
    }

    [Test]
    public void BuildsVersionFourAbstractProfileFromCurvesMatrixAndClut()
    {
        IccProfileBuildOptions options = AbstractOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Xyz);
        options.AbstractDefinition!.MultiStageLut = MultiStageIdentityLut();

        IccProfile profile = new IccProfileBuilder().CreateAbstractProfile(options);
        IccTag transform = FindTag(profile, IccTagSignatures.AToB0);
        LutPipelinePlan plan = LutPipelineCompiler.Compile(profile, transform,
            IccTransformDirection.DeviceToPcs, IccInterpolationKind.Tetrahedral);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(transform.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.LutAToB));
            Assert.That(transform.Data.MultiStageLut!.PcsEncoding,
                Is.EqualTo(IccPcsEncodingKind.Version4));
            Assert.That(transform.Data.MultiStageLut.ACurves, Has.Length.EqualTo(3));
            Assert.That(transform.Data.MultiStageLut.MCurves, Has.Length.EqualTo(3));
            Assert.That(transform.Data.MultiStageLut.BCurves, Has.Length.EqualTo(3));
            Assert.That(transform.Data.MultiStageLut.Matrix!.Values, Has.Length.EqualTo(12));
            Assert.That(transform.Data.MultiStageLut.Clut!.Values, Has.Length.EqualTo(24));
            Assert.That(plan.Stages.Select(stage => stage.Kind), Is.EqualTo(new[]
            {
                PipelineStageKind.Curves,
                PipelineStageKind.Clut,
                PipelineStageKind.Curves,
                PipelineStageKind.Matrix,
                PipelineStageKind.Curves,
                PipelineStageKind.PcsDenormalize
            }));
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.MultiLocalizedUnicode));
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [Test]
    public void AddsPreferredPerceptualMpeWhileRetainingRequiredLegacyFallback()
    {
        IccProfileBuildOptions options = AbstractOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Xyz);
        options.AbstractDefinition!.MultiStageLut = MultiStageIdentityLut();
        options.AbstractDefinition.MultiProcessElements = MpePipeline();

        IccProfile profile = new IccProfileBuilder().CreateAbstractProfile(options);
        IccTag legacy = FindTag(profile, IccTagSignatures.AToB0);
        IccTag mpe = FindTag(profile, IccTagSignatures.DToB0);
        IntentSelection perceptual = IntentSelector.SelectIcc1Transform(profile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.Perceptual,
            IccIntentFallback.None, false);
        IntentSelection saturation = IntentSelector.SelectIcc1Transform(profile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.Saturation,
            IccIntentFallback.None, false);
        MpePipelinePlan plan = MpePipelineCompiler.Compile(profile, mpe,
            IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);
        double[] output = ExecuteMpe(plan, new double[] { 0.2, 0.4, 0.6 });
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(legacy.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.LutAToB));
            Assert.That(mpe.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.MultiProcessElements));
            Assert.That(mpe.Data.MultiProcessElements!.IsExecutable, Is.True);
            Assert.That(perceptual.Success, Is.True);
            Assert.That(perceptual.TagSignature, Is.EqualTo(IccTagSignatures.DToB0));
            Assert.That(saturation.Success, Is.True);
            Assert.That(saturation.TagSignature, Is.EqualTo(IccTagSignatures.AToB0));
            Assert.That(plan.Stages.Select(stage => stage.Kind), Is.EqualTo(new[]
            {
                PipelineStageKind.Curves,
                PipelineStageKind.Matrix,
                PipelineStageKind.Clut
            }));
            Assert.That(output, Is.EqualTo(new double[] { 0.1, 0.2, 0.3 })
                .Within(0.000001));
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [Test]
    public void AddsIndependentlyOwnedAdaptiveGainMetadataToEligibleRgbProfile()
    {
        IccProfileBuildOptions options = RgbOptions();
        options.AdaptiveGainCurve = AdaptiveGainCurve();

        IccProfile profile = new IccProfileBuilder().CreateRgbProfile(options);
        options.AdaptiveGainCurve.RedNodes[0].Gain = 9.0;
        IccTag tag = FindTag(profile, IccTagSignatures.AdaptiveGainCurve);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(tag.Data.Kind, Is.EqualTo(IccTagDataKind.AdaptiveGainCurve));
            Assert.That(tag.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.AdaptiveGainCurve));
            Assert.That(tag.Data.AdaptiveGainCurve!.TargetHeadroom, Is.EqualTo(2.0));
            Assert.That(tag.Data.AdaptiveGainCurve.RedNodes[0].Gain, Is.EqualTo(1.0));
            Assert.That(tag.Data.AdaptiveGainCurve.RedNodes,
                Is.Not.SameAs(options.AdaptiveGainCurve.RedNodes));
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [Test]
    public void RejectsIncompleteInconsistentAndGenerationInappropriateDefinitions()
    {
        IccProfileBuilder builder = new IccProfileBuilder();
        IccProfileBuildOptions missing = AbstractOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Xyz);
        IccProfileBuildOptions noFallback = AbstractOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Xyz);
        noFallback.AbstractDefinition!.MultiProcessElements = MpePipeline();
        IccProfileBuildOptions duplicateFallback = AbstractOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Xyz);
        duplicateFallback.AbstractDefinition!.Lut = ClassicIdentityLut();
        duplicateFallback.AbstractDefinition.MultiStageLut = MultiStageIdentityLut();
        IccProfileBuildOptions wrongClass = AbstractOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Xyz);
        wrongClass.ProfileClass = IccProfileClass.Display;
        IccProfileBuildOptions mismatchedPcs = AbstractOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Xyz);
        mismatchedPcs.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Lab;
        mismatchedPcs.AbstractDefinition!.MultiStageLut = MultiStageIdentityLut();
        IccProfileBuildOptions versionTwoMultiStage = AbstractOptions(
            IccVersionKind.Version2, IccColorSpaceSignatures.Lab);
        versionTwoMultiStage.AbstractDefinition!.MultiStageLut = MultiStageIdentityLut();
        IccProfileBuildOptions versionTwoMpe = AbstractOptions(IccVersionKind.Version2,
            IccColorSpaceSignatures.Lab);
        versionTwoMpe.AbstractDefinition!.Lut = ClassicIdentityLut();
        versionTwoMpe.AbstractDefinition.MultiProcessElements = MpePipeline();
        IccProfileBuildOptions colorSpaceAdaptive = RgbOptions();
        colorSpaceAdaptive.ProfileClass = IccProfileClass.ColorSpace;
        colorSpaceAdaptive.AdaptiveGainCurve = AdaptiveGainCurve();
        IccProfileBuildOptions imageSpecificAdaptive = RgbOptions();
        imageSpecificAdaptive.AdaptiveGainCurve = AdaptiveGainCurve();
        imageSpecificAdaptive.AdaptiveGainCurve.IsImageSpecific = true;
        imageSpecificAdaptive.AdaptiveGainCurve.Identifier[0] = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateAbstractProfile(missing));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateAbstractProfile(noFallback));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateAbstractProfile(duplicateFallback));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateAbstractProfile(wrongClass));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateAbstractProfile(mismatchedPcs));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateAbstractProfile(versionTwoMultiStage));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateAbstractProfile(versionTwoMpe));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateRgbProfile(colorSpaceAdaptive));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateRgbProfile(imageSpecificAdaptive));
        });
    }

    private static IccProfileBuildOptions AbstractOptions(IccVersionKind version,
        uint colorSpace)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.Abstract;
        options.DataColorSpaceSignature = colorSpace;
        options.ProfileConnectionSpaceSignature = colorSpace;
        options.Version.Kind = version;
        options.Version.Major = version == IccVersionKind.Version2 ? 2 : 4;
        options.Version.Minor = version == IccVersionKind.Version2 ? 4 : 4;
        options.AbstractDefinition = new IccAbstractProfileDefinition();
        return options;
    }

    private static IccLut ClassicIdentityLut()
    {
        IccLut lut = new IccLut();
        lut.InputChannels = 3;
        lut.OutputChannels = 3;
        lut.Matrix.Rows = 3;
        lut.Matrix.Columns = 3;
        lut.Matrix.Values = IdentityMatrixValues();
        lut.InputTables = IdentityTables(2);
        lut.OutputTables = IdentityTables(2);
        lut.Clut.InputChannels = 3;
        lut.Clut.OutputChannels = 3;
        lut.Clut.GridPoints = new int[] { 2, 2, 2 };
        lut.Clut.Values = IdentityClutValues();
        lut.PcsEncoding = IccPcsEncodingKind.Version2;
        return lut;
    }

    private static IccMultiStageLut MultiStageIdentityLut()
    {
        IccMultiStageLut lut = new IccMultiStageLut();
        lut.InputChannels = 3;
        lut.OutputChannels = 3;
        lut.ACurves = IdentityCurves();
        lut.MCurves = IdentityCurves();
        lut.BCurves = IdentityCurves();
        lut.Matrix = new IccMatrix
        {
            Rows = 3,
            Columns = 4,
            Values = new double[]
            {
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0
            }
        };
        lut.Clut = new IccClut
        {
            InputChannels = 3,
            OutputChannels = 3,
            GridPoints = new int[] { 2, 2, 2 },
            Values = IdentityClutValues(),
            Interpolation = IccInterpolationKind.Tetrahedral
        };
        lut.PcsEncoding = IccPcsEncodingKind.Version4;
        return lut;
    }

    private static IccMultiProcessElements MpePipeline()
    {
        IccProcessingElement curves = new IccProcessingElement();
        curves.Signature = IccProcessingElementSignatures.CurveSet;
        curves.Kind = IccProcessingElementKind.CurveSet;
        curves.InputChannels = 3;
        curves.OutputChannels = 3;
        curves.Curves = SegmentedIdentityCurves();

        IccProcessingElement matrix = new IccProcessingElement();
        matrix.Signature = IccProcessingElementSignatures.Matrix;
        matrix.Kind = IccProcessingElementKind.Matrix;
        matrix.InputChannels = 3;
        matrix.OutputChannels = 3;
        matrix.Matrix = new IccMatrix
        {
            Rows = 3,
            Columns = 4,
            Values = new double[]
            {
                0.5, 0.0, 0.0, 0.0,
                0.0, 0.5, 0.0, 0.0,
                0.0, 0.0, 0.5, 0.0
            }
        };

        IccProcessingElement clut = new IccProcessingElement();
        clut.Signature = IccProcessingElementSignatures.Clut;
        clut.Kind = IccProcessingElementKind.Clut;
        clut.InputChannels = 3;
        clut.OutputChannels = 3;
        clut.Clut = new IccClut
        {
            InputChannels = 3,
            OutputChannels = 3,
            GridPoints = new int[] { 2, 2, 2 },
            Values = IdentityClutValues(),
            Interpolation = IccInterpolationKind.Multilinear
        };

        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = 3;
        pipeline.OutputChannels = 3;
        pipeline.InputRanges = FullRanges(3);
        pipeline.OutputRanges = FullRanges(3);
        pipeline.Elements = new IccProcessingElement[] { curves, matrix, clut };
        pipeline.IsExecutable = true;
        return pipeline;
    }

    private static IccCurve[] SegmentedIdentityCurves()
    {
        IccCurve[] curves = new IccCurve[3];
        int index = 0;
        while (index < curves.Length)
        {
            IccCurveSegment segment = new IccCurveSegment();
            segment.Kind = IccCurveSegmentKind.Formula;
            segment.FormulaType = 0;
            segment.BreakPoint = double.NegativeInfinity;
            segment.Parameters = new double[] { 1.0, 1.0, 0.0, 0.0 };
            curves[index] = new IccCurve
            {
                Kind = IccCurveKind.Segmented,
                Segments = new IccCurveSegment[] { segment }
            };
            index++;
        }
        return curves;
    }

    private static IccCurve[] IdentityCurves()
    {
        return new IccCurve[] { new IccCurve(), new IccCurve(), new IccCurve() };
    }

    private static double[][] IdentityTables(int length)
    {
        return new double[][]
        {
            Enumerable.Range(0, length).Select(index => (double)index / (length - 1)).ToArray(),
            Enumerable.Range(0, length).Select(index => (double)index / (length - 1)).ToArray(),
            Enumerable.Range(0, length).Select(index => (double)index / (length - 1)).ToArray()
        };
    }

    private static double[] IdentityMatrixValues()
    {
        return new double[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0
        };
    }

    private static double[] IdentityClutValues()
    {
        return new double[]
        {
            0.0, 0.0, 0.0,
            0.0, 0.0, 1.0,
            0.0, 1.0, 0.0,
            0.0, 1.0, 1.0,
            1.0, 0.0, 0.0,
            1.0, 0.0, 1.0,
            1.0, 1.0, 0.0,
            1.0, 1.0, 1.0
        };
    }

    private static IccEncodingRange[] FullRanges(int count)
    {
        return Enumerable.Range(0, count).Select(index => new IccEncodingRange
        {
            Minimum = float.MinValue,
            Maximum = float.MaxValue
        }).ToArray();
    }

    private static double[] ExecuteMpe(MpePipelinePlan plan, double[] input)
    {
        double[] output = new double[plan.DestinationChannels];
        int valueLength = PipelineExecutor.GetRequiredValueScratchLength(plan.Stages);
        int indexLength = PipelineExecutor.GetRequiredClutIndexScratchLength(plan.Stages);
        int weightLength = PipelineExecutor.GetRequiredClutWeightScratchLength(plan.Stages);
        PipelineExecutor.ExecutePipeline(plan.Stages, input, output,
            new double[valueLength], new double[valueLength], new int[indexLength],
            new double[weightLength]);
        return output;
    }

    private static IccAdaptiveGainCurve AdaptiveGainCurve()
    {
        IccAdaptiveGainCurve curve = new IccAdaptiveGainCurve();
        curve.TargetHeadroom = 2.0;
        curve.AlternateHeadroom = 1.0;
        curve.BackwardCompatibleTargetHeadrooms = new double[] { 1.0, 2.0, 1.0 };
        curve.RedNodes = GainNodes();
        curve.GreenNodes = GainNodes();
        curve.BlueNodes = GainNodes();
        return curve;
    }

    private static IccAdaptiveGainCurveNode[] GainNodes()
    {
        return new IccAdaptiveGainCurveNode[]
        {
            new IccAdaptiveGainCurveNode { Input = 0.0, Gain = 1.0, Slope = 0.0 },
            new IccAdaptiveGainCurveNode { Input = 1.0, Gain = 2.0, Slope = 0.0 }
        };
    }

    private static IccProfileBuildOptions RgbOptions()
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.Display;
        options.RgbDefinition = new IccRgbProfileDefinition();
        options.RgbDefinition.RedPrimary.X = 0.64;
        options.RgbDefinition.RedPrimary.Y = 0.33;
        options.RgbDefinition.GreenPrimary.X = 0.30;
        options.RgbDefinition.GreenPrimary.Y = 0.60;
        options.RgbDefinition.BluePrimary.X = 0.15;
        options.RgbDefinition.BluePrimary.Y = 0.06;
        options.RgbDefinition.WhitePoint.X = IccStandardColorimetry.D65ChromaticityX;
        options.RgbDefinition.WhitePoint.Y = IccStandardColorimetry.D65ChromaticityY;
        options.RgbDefinition.RedCurve = GammaCurve();
        options.RgbDefinition.GreenCurve = GammaCurve();
        options.RgbDefinition.BlueCurve = GammaCurve();
        return options;
    }

    private static IccCurve GammaCurve()
    {
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Gamma;
        curve.Gamma = 2.2;
        return curve;
    }

    private static IccTag FindTag(IccProfile profile, uint signature)
    {
        return profile.Tags.Single(tag => tag.Signature == signature);
    }
}
