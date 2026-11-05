using icc;

namespace icc.Tests;

public class MpePipelineCompilerTests
{
    [Test]
    public void VersionFourCurveMatrixAndClutPipelineCompilesAndExecutesInOrder()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz);
        IccProcessingElement curves = CurveSet(GammaCurve(2.0));
        IccProcessingElement matrix = Matrix(1, 3, new[] { 1.0, 0.1, 2.0, 0.0, 0.0, 0.3 });
        IccProcessingElement clut = Clut(3, 3, Enumerable.Repeat(2, 3).ToArray(), ConstantClutValues(8, new[] { 0.2, 0.4, 0.6 }));
        IccMultiProcessElements pipeline = Pipeline(1, 3, curves, matrix, clut);

        MpePipelinePlan plan = MpePipelineCompiler.Compile(profile, Tag(IccTagSignatures.DToB1, pipeline), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Tetrahedral);
        double[] actual = Execute(plan, new[] { 0.5 });

        Assert.Multiple(() =>
        {
            Assert.That(plan.TagSignature, Is.EqualTo(IccTagSignatures.DToB1));
            Assert.That(plan.SourceChannels, Is.EqualTo(1));
            Assert.That(plan.DestinationChannels, Is.EqualTo(3));
            Assert.That(plan.Stages.Select(stage => stage.Kind), Is.EqualTo(new[] { PipelineStageKind.Curves, PipelineStageKind.Matrix, PipelineStageKind.Clut }));
            Assert.That(plan.Stages[2].Interpolation, Is.EqualTo(IccInterpolationKind.Tetrahedral));
            Assert.That(actual, Is.EqualTo(new[] { 0.2, 0.4, 0.6 }).Within(1e-14));
        });
    }

    [Test]
    public void ReverseMpePreservesAffineOffsetsAndArbitraryMatrixDimensions()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz);
        IccProcessingElement matrix = Matrix(3, 1, new[] { 0.5, 0.25, 0.125, 0.1 });
        IccMultiProcessElements pipeline = Pipeline(3, 1, matrix);

        MpePipelinePlan plan = MpePipelineCompiler.Compile(profile, Tag(IccTagSignatures.BToD2, pipeline), IccTransformDirection.PcsToDevice, IccInterpolationKind.Multilinear);
        double[] actual = Execute(plan, new[] { 0.2, 0.4, 0.8 });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Direction, Is.EqualTo(IccTransformDirection.PcsToDevice));
            Assert.That(plan.Stages.Single().Matrix!.Rows, Is.EqualTo(1));
            Assert.That(plan.Stages.Single().Matrix!.Columns, Is.EqualTo(4));
            Assert.That(actual[0], Is.EqualTo(0.4).Within(1e-14));
        });
    }

    [Test]
    public void AcsMarkersCompileAsExplicitPassThroughStages()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab);
        IccProcessingElement begin = Acs(true, 3);
        IccProcessingElement curves = CurveSet(GammaCurve(1.0), GammaCurve(1.0), GammaCurve(1.0));
        IccProcessingElement end = Acs(false, 3);
        IccMultiProcessElements pipeline = Pipeline(3, 3, begin, curves, end);

        MpePipelinePlan plan = MpePipelineCompiler.Compile(profile, Tag(IccTagSignatures.DToB0, pipeline), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);
        double[] input = new[] { 0.1, 0.2, 0.3 };

        Assert.Multiple(() =>
        {
            Assert.That(plan.Stages.Select(stage => stage.Kind), Is.EqualTo(new[] { PipelineStageKind.Identity, PipelineStageKind.Curves, PipelineStageKind.Identity }));
            Assert.That(plan.Stages[0].Description, Does.Contain("begin ACS"));
            Assert.That(plan.Stages[2].Description, Does.Contain("end ACS"));
            Assert.That(Execute(plan, input), Is.EqualTo(input).Within(1e-14));
        });
    }

    [Test]
    public void CompiledMpePlanSnapshotsElementDataAndBoundaryRanges()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz);
        IccCurve curve = GammaCurve(2.0);
        IccProcessingElement curves = CurveSet(curve);
        IccProcessingElement matrix = Matrix(1, 3, new[] { 1.0, 0.0, 1.0, 0.0, 1.0, 0.0 });
        IccProcessingElement clut = Clut(3, 3, Enumerable.Repeat(2, 3).ToArray(), ConstantClutValues(8, new[] { 0.2, 0.4, 0.6 }));
        IccMultiProcessElements pipeline = Pipeline(1, 3, curves, matrix, clut);

        MpePipelinePlan plan = MpePipelineCompiler.Compile(profile, Tag(IccTagSignatures.DToB0, pipeline), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);
        curve.Segments[0].Parameters[0] = 8.0;
        matrix.Matrix!.Values[0] = 9.0;
        clut.Clut!.Values[0] = 7.0;
        pipeline.InputRanges[0].Minimum = -99.0;

        Assert.Multiple(() =>
        {
            Assert.That(plan.Stages[0].Curves[0].Segments[0].Parameters[0], Is.EqualTo(2.0));
            Assert.That(plan.Stages[1].Matrix!.Values[0], Is.EqualTo(1.0));
            Assert.That(plan.Stages[2].Clut!.Values[0], Is.EqualTo(0.2));
            Assert.That(plan.SourceRanges[0].Minimum, Is.EqualTo(float.MinValue));
        });
    }

    [Test]
    public void UnknownElementRejectsTheWholePipelineBeforeAnySampleIsChanged()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        IccProcessingElement curves = CurveSet(GammaCurve(2.0), GammaCurve(2.0), GammaCurve(2.0));
        IccProcessingElement unknown = new IccProcessingElement
        {
            Signature = 0x66757472,
            Kind = IccProcessingElementKind.Unknown,
            InputChannels = 3,
            OutputChannels = 3
        };
        IccMultiProcessElements pipeline = Pipeline(3, 3, curves, unknown);
        double[] output = new[] { -1.0, -1.0, -1.0 };

        Assert.Throws<IccTransformException>(() =>
        {
            MpePipelinePlan plan = MpePipelineCompiler.Compile(profile, Tag(IccTagSignatures.DToB3, pipeline), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);
            ExecuteInto(plan, new[] { 0.5, 0.5, 0.5 }, output);
        });

        Assert.That(output, Is.EqualTo(new[] { -1.0, -1.0, -1.0 }));
    }

    [Test]
    public void CompilationRejectsWrongVersionSignatureTopologyRangesAndUnsupportedClutSize()
    {
        IccProfile versionTwo = Profile(IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz);
        versionTwo.Header.Version.Kind = IccVersionKind.Version2;
        IccMultiProcessElements valid = Pipeline(3, 3, CurveSet(GammaCurve(1.0), GammaCurve(1.0), GammaCurve(1.0)));

        IccProcessingElement discontinuousMatrix = Matrix(2, 3, new[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 });
        IccMultiProcessElements discontinuous = Pipeline(3, 3, CurveSet(GammaCurve(1.0), GammaCurve(1.0), GammaCurve(1.0)), discontinuousMatrix);

        IccProcessingElement wrongSignature = CurveSet(GammaCurve(1.0), GammaCurve(1.0), GammaCurve(1.0));
        wrongSignature.Signature = 0x62616420;
        IccMultiProcessElements badKindSignature = Pipeline(3, 3, wrongSignature);

        IccProcessingElement threeDimensionalClut = Clut(3, 3, new[] { 2, 2, 2 }, ConstantClutValues(8, new[] { 0.0, 0.0, 0.0 }));
        IccMultiProcessElements linearMismatch = Pipeline(3, 3, threeDimensionalClut);

        IccMultiProcessElements badRange = Pipeline(3, 3, CurveSet(GammaCurve(1.0), GammaCurve(1.0), GammaCurve(1.0)));
        badRange.InputRanges[1].Minimum = double.NaN;

        uint sixteenChannelSpace = IccColorSpaceSignatures.ExtendedDevicePrefix | 16;
        IccProfile sixteenChannelProfile = Profile(sixteenChannelSpace, IccColorSpaceSignatures.Xyz);
        IccProcessingElement largeClut = Clut(16, 3, Enumerable.Repeat(2, 16).ToArray(), new double[0]);
        IccMultiProcessElements unsupportedClut = Pipeline(16, 3, largeClut);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => MpePipelineCompiler.Compile(versionTwo, Tag(IccTagSignatures.DToB0, valid), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => MpePipelineCompiler.Compile(Profile(IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz), Tag(IccTagSignatures.DToB0, discontinuous), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => MpePipelineCompiler.Compile(Profile(IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz), Tag(IccTagSignatures.DToB0, badKindSignature), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => MpePipelineCompiler.Compile(Profile(IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz), Tag(IccTagSignatures.DToB0, linearMismatch), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Linear));
            Assert.Throws<IccTransformException>(() => MpePipelineCompiler.Compile(Profile(IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz), Tag(IccTagSignatures.DToB0, badRange), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => MpePipelineCompiler.Compile(sixteenChannelProfile, Tag(IccTagSignatures.DToB0, unsupportedClut), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => MpePipelineCompiler.Compile(Profile(IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz), Tag(IccTagSignatures.BToD0, valid), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
        });
    }

    private static double[] Execute(MpePipelinePlan plan, double[] input)
    {
        double[] output = new double[plan.DestinationChannels];
        ExecuteInto(plan, input, output);
        return output;
    }

    private static void ExecuteInto(MpePipelinePlan plan, double[] input, double[] output)
    {
        int valueLength = PipelineExecutor.GetRequiredValueScratchLength(plan.Stages);
        int indexLength = PipelineExecutor.GetRequiredClutIndexScratchLength(plan.Stages);
        int weightLength = PipelineExecutor.GetRequiredClutWeightScratchLength(plan.Stages);
        PipelineExecutor.ExecutePipeline(plan.Stages, input, output, new double[valueLength], new double[valueLength], new int[indexLength], new double[weightLength]);
    }

    private static IccProfile Profile(uint dataColorSpace, uint pcs)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version4;
        profile.Header.DataColorSpaceSignature = dataColorSpace;
        profile.Header.ProfileConnectionSpaceSignature = pcs;
        profile.Header.ProfileConnectionSpace = pcs == IccColorSpaceSignatures.Xyz ? IccPcsKind.Xyz : IccPcsKind.Lab;
        return profile;
    }

    private static IccTag Tag(uint signature, IccMultiProcessElements pipeline)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.MultiProcessElements;
        data.MultiProcessElements = pipeline;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        return new IccTag { Signature = signature, Data = data };
    }

    private static IccMultiProcessElements Pipeline(int inputChannels, int outputChannels, params IccProcessingElement[] elements)
    {
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = inputChannels;
        pipeline.OutputChannels = outputChannels;
        pipeline.InputRanges = FullRanges(inputChannels);
        pipeline.OutputRanges = FullRanges(outputChannels);
        pipeline.Elements = elements;
        pipeline.IsExecutable = true;
        return pipeline;
    }

    private static IccProcessingElement CurveSet(params IccCurve[] curves)
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.CurveSet,
            Kind = IccProcessingElementKind.CurveSet,
            InputChannels = curves.Length,
            OutputChannels = curves.Length,
            Curves = curves
        };
    }

    private static IccProcessingElement Matrix(int inputChannels, int outputChannels, double[] values)
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            Matrix = new IccMatrix { Rows = outputChannels, Columns = inputChannels + 1, Values = values }
        };
    }

    private static IccProcessingElement Clut(int inputChannels, int outputChannels, int[] gridPoints, double[] values)
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Clut,
            Kind = IccProcessingElementKind.Clut,
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            Clut = new IccClut { InputChannels = inputChannels, OutputChannels = outputChannels, GridPoints = gridPoints, Values = values }
        };
    }

    private static IccProcessingElement Acs(bool begin, int channels)
    {
        return new IccProcessingElement
        {
            Signature = begin ? IccProcessingElementSignatures.BeginAcs : IccProcessingElementSignatures.EndAcs,
            Kind = begin ? IccProcessingElementKind.BeginAcs : IccProcessingElementKind.EndAcs,
            InputChannels = channels,
            OutputChannels = channels,
            AcsSignature = 0x74657374,
            AcsData = new byte[] { 1, 2, 3 }
        };
    }

    private static IccCurve GammaCurve(double gamma)
    {
        IccCurveSegment segment = new IccCurveSegment();
        segment.Kind = IccCurveSegmentKind.Formula;
        segment.FormulaType = 0;
        segment.BreakPoint = double.NegativeInfinity;
        segment.Parameters = new[] { gamma, 1.0, 0.0, 0.0 };
        return new IccCurve { Kind = IccCurveKind.Segmented, Segments = new[] { segment } };
    }

    private static IccEncodingRange[] FullRanges(int count)
    {
        return Enumerable.Range(0, count).Select(index => new IccEncodingRange { Minimum = float.MinValue, Maximum = float.MaxValue }).ToArray();
    }

    private static double[] ConstantClutValues(int pointCount, double[] outputs)
    {
        double[] values = new double[pointCount * outputs.Length];
        int point = 0;
        while (point < pointCount)
        {
            outputs.CopyTo(values, point * outputs.Length);
            point++;
        }

        return values;
    }
}
