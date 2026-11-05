using icc;

namespace icc.Tests;

public class V4MpeCompleteCoverageTests
{
    [Test]
    public void CompleteKnownPipelineSharesOffsetsRoundTripsAndExecutesDeterministically()
    {
        IccCurve gamma = FormulaCurve(0, 2.0, 1.0, 0.0, 0.0);
        IccProcessingElement sharedCurves = CurveSet(gamma, gamma);
        IccProcessingElement matrix = Matrix(2, 3, new[]
        {
            1.0, 0.0, 0.125,
            0.0, 1.0, 0.25,
            0.5, 0.25, 0.125
        });
        IccProcessingElement clut = Clut(3, 3, new[] { 2, 2, 2 },
            IdentityThreeChannelClutValues());
        IccMultiProcessElements source = Pipeline(2, 3,
            Acs(true, 2, new byte[] { 1, 2, 3 }), sharedCurves,
            sharedCurves, matrix, clut, Acs(false, 3, new byte[] { 4, 5 }));

        byte[] encoded = MultiProcessElementsTagCodec.SerializeMultiProcessElements(source);
        int curveElementOffset = (int)ReadUInt32(encoded, 24);
        TagDecodeState state = Decode(encoded);
        IccMultiProcessElements decoded = state.Data.MultiProcessElements!;
        byte[] firstWriteback = MultiProcessElementsTagCodec.SerializeMultiProcessElements(decoded);
        byte[] secondWriteback = MultiProcessElementsTagCodec.SerializeMultiProcessElements(decoded);
        MpePipelinePlan plan = MpePipelineCompiler.Compile(
            Profile(IccColorSpaceSignatures.TwoColor),
            Tag(IccTagSignatures.DToB0, decoded),
            IccTransformDirection.DeviceToPcs,
            IccInterpolationKind.Tetrahedral);
        double[] output = Execute(plan, new[] { 0.5, 0.25 });

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.IsExecutable, Is.True);
            Assert.That(decoded.Elements.Select(element => element.Kind), Is.EqualTo(new[]
            {
                IccProcessingElementKind.BeginAcs,
                IccProcessingElementKind.CurveSet,
                IccProcessingElementKind.CurveSet,
                IccProcessingElementKind.Matrix,
                IccProcessingElementKind.Clut,
                IccProcessingElementKind.EndAcs
            }));
            Assert.That(ReadUInt32(encoded, 24), Is.EqualTo(ReadUInt32(encoded, 32)));
            Assert.That(ReadUInt32(encoded, 28), Is.EqualTo(ReadUInt32(encoded, 36)));
            Assert.That(ReadUInt32(encoded, curveElementOffset + 12),
                Is.EqualTo(ReadUInt32(encoded, curveElementOffset + 20)));
            Assert.That(decoded.Elements[1], Is.SameAs(decoded.Elements[2]));
            Assert.That(decoded.Elements[1].Curves[0],
                Is.SameAs(decoded.Elements[1].Curves[1]));
            Assert.That(firstWriteback, Is.EqualTo(encoded));
            Assert.That(secondWriteback, Is.EqualTo(encoded));
            Assert.That(plan.Stages.Select(stage => stage.Kind), Is.EqualTo(new[]
            {
                PipelineStageKind.Identity,
                PipelineStageKind.Curves,
                PipelineStageKind.Curves,
                PipelineStageKind.Matrix,
                PipelineStageKind.Clut,
                PipelineStageKind.Identity
            }));
            Assert.That(output, Is.EqualTo(new[]
            {
                0.1875, 0.25390625, 0.1572265625
            }).Within(1e-12));
        });
    }

    [TestCase(IccTagSignatures.DToB0, true)]
    [TestCase(IccTagSignatures.DToB1, true)]
    [TestCase(IccTagSignatures.DToB2, true)]
    [TestCase(IccTagSignatures.DToB3, true)]
    [TestCase(IccTagSignatures.BToD0, false)]
    [TestCase(IccTagSignatures.BToD1, false)]
    [TestCase(IccTagSignatures.BToD2, false)]
    [TestCase(IccTagSignatures.BToD3, false)]
    public void EveryV4MpeIntentSlotCompilesInItsRequiredDirection(
        uint signature, bool forward)
    {
        IccMultiProcessElements source = forward
            ? Pipeline(2, 3, Matrix(2, 3, new[]
            {
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.5, 0.5, 0.0
            }))
            : Pipeline(3, 2, Matrix(3, 2, new[]
            {
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0
            }));
        TagDecodeState state = Decode(
            MultiProcessElementsTagCodec.SerializeMultiProcessElements(source));
        IccTransformDirection direction = forward
            ? IccTransformDirection.DeviceToPcs
            : IccTransformDirection.PcsToDevice;

        MpePipelinePlan plan = MpePipelineCompiler.Compile(
            Profile(IccColorSpaceSignatures.TwoColor),
            Tag(signature, state.Data.MultiProcessElements!), direction,
            IccInterpolationKind.Multilinear);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(plan.TagSignature, Is.EqualTo(signature));
            Assert.That(plan.Direction, Is.EqualTo(direction));
            Assert.That(plan.SourceChannels, Is.EqualTo(forward ? 2 : 3));
            Assert.That(plan.DestinationChannels, Is.EqualTo(forward ? 3 : 2));
        });
    }

    [Test]
    public void EveryV4SegmentFormulaSelectorRoundTripsAndExecutes()
    {
        IccProcessingElement curves = CurveSet(
            FormulaCurve(0, 2.0, 1.0, 0.0, 0.0),
            FormulaCurve(1, 1.0, 1.0, 9.0, 1.0, 0.0),
            FormulaCurve(2, 1.0, 2.0, 1.0, 0.0, 0.0));
        byte[] encoded = MultiProcessElementsTagCodec.SerializeMultiProcessElements(
            Pipeline(3, 3, curves));
        TagDecodeState state = Decode(encoded);
        MpePipelinePlan plan = MpePipelineCompiler.Compile(
            Profile(IccColorSpaceSignatures.Rgb),
            Tag(IccTagSignatures.DToB3, state.Data.MultiProcessElements!),
            IccTransformDirection.DeviceToPcs,
            IccInterpolationKind.Multilinear);

        double[] output = Execute(plan, new[] { 0.25, 0.5, 0.75 });

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements!.Elements[0].Curves
                .Select(curve => curve.Segments[0].FormulaType),
                Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(output, Is.EqualTo(new[]
            {
                0.0625,
                Math.Log10(5.5),
                Math.Pow(2.0, 0.75)
            }).Within(1e-12));
            Assert.That(MultiProcessElementsTagCodec.SerializeMultiProcessElements(
                state.Data.MultiProcessElements), Is.EqualTo(encoded));
        });
    }

    [Test]
    public void SampledSegmentUsesPrecedingFormulaAtItsFirstBreakpoint()
    {
        IccCurve segmented = SegmentedCurve(
            FormulaSegment(double.NegativeInfinity, 0, 1.0, 0.0, 0.0, 0.125),
            SampledSegment(0.25, 0.25, 0.5, 0.75),
            FormulaSegment(0.75, 0, 1.0, 0.0, 0.0, 0.875));
        byte[] encoded = MultiProcessElementsTagCodec.SerializeMultiProcessElements(
            Pipeline(3, 3, CurveSet(segmented, segmented, segmented)));
        TagDecodeState state = Decode(encoded);
        MpePipelinePlan plan = MpePipelineCompiler.Compile(
            Profile(IccColorSpaceSignatures.Rgb),
            Tag(IccTagSignatures.DToB1, state.Data.MultiProcessElements!),
            IccTransformDirection.DeviceToPcs,
            IccInterpolationKind.Multilinear);

        double[] atFirstBreak = Execute(plan, new[] { 0.25, 0.25, 0.25 });
        double[] insideSampled = Execute(plan, new[] { 0.5, 0.5, 0.5 });
        double[] atLastBreak = Execute(plan, new[] { 0.75, 0.75, 0.75 });
        double[] afterLastBreak = Execute(plan, new[] { 0.7501, 0.7501, 0.7501 });

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(atFirstBreak, Is.EqualTo(new[] { 0.125, 0.125, 0.125 })
                .Within(1e-12));
            Assert.That(insideSampled, Is.EqualTo(new[] { 0.375, 0.375, 0.375 })
                .Within(1e-12));
            Assert.That(atLastBreak, Is.EqualTo(new[] { 0.75, 0.75, 0.75 })
                .Within(1e-12));
            Assert.That(afterLastBreak, Is.EqualTo(new[] { 0.875, 0.875, 0.875 })
                .Within(1e-12));
        });
    }

    [Test]
    public void FutureElementIsPreservedDeterministicallyAndPreventsCompilation()
    {
        IccProcessingElement future = FutureElement(0x76346675, 3, 3,
            new byte[] { 0xde, 0xad, 0xbe, 0xef, 1 });
        IccMultiProcessElements source = Pipeline(3, 3,
            Acs(true, 3, new byte[] { 7 }), future,
            Acs(false, 3, new byte[] { 8 }));
        byte[] encoded = MultiProcessElementsTagCodec.SerializeMultiProcessElements(source);
        TagDecodeState state = Decode(encoded);
        IccMultiProcessElements decoded = state.Data.MultiProcessElements!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("retained losslessly"));
            Assert.That(decoded.IsExecutable, Is.False);
            Assert.That(decoded.Elements[1].Kind, Is.EqualTo(IccProcessingElementKind.Unknown));
            Assert.That(decoded.Elements[1].RawData.Bytes, Is.EqualTo(future.RawData.Bytes));
            Assert.That(MultiProcessElementsTagCodec.SerializeMultiProcessElements(decoded),
                Is.EqualTo(encoded));
            Assert.Throws<IccTransformException>(() => MpePipelineCompiler.Compile(
                Profile(IccColorSpaceSignatures.Rgb),
                Tag(IccTagSignatures.DToB0, decoded),
                IccTransformDirection.DeviceToPcs,
                IccInterpolationKind.Multilinear));
        });
    }

    [Test]
    public void DecoderReportsFirstIntermediateAndLastChannelMismatches()
    {
        IccMultiProcessElements source = Pipeline(2, 3,
            Acs(true, 2, new byte[0]),
            Matrix(2, 3, new[]
            {
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 0.0
            }),
            Acs(false, 3, new byte[0]));
        byte[] valid = MultiProcessElementsTagCodec.SerializeMultiProcessElements(source);

        byte[] firstMismatch = valid.ToArray();
        WriteUInt16(firstMismatch, 8, 1);
        byte[] intermediateMismatch = valid.ToArray();
        int endOffset = (int)ReadUInt32(intermediateMismatch, 32);
        WriteUInt16(intermediateMismatch, endOffset + 8, 2);
        WriteUInt16(intermediateMismatch, endOffset + 10, 2);
        byte[] lastMismatch = valid.ToArray();
        WriteUInt16(lastMismatch, 10, 2);

        Assert.Multiple(() =>
        {
            AssertInvalid(firstMismatch, "first processing element");
            AssertInvalid(intermediateMismatch, "previous element");
            AssertInvalid(lastMismatch, "last processing element");
        });
    }

    private static IccProfile Profile(uint dataColorSpace)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version4;
        profile.Header.DataColorSpaceSignature = dataColorSpace;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        return profile;
    }

    private static IccTag Tag(uint signature, IccMultiProcessElements pipeline)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.MultiProcessElements;
        data.MultiProcessElements = pipeline;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.Data = data;
        return tag;
    }

    private static IccMultiProcessElements Pipeline(int inputChannels,
        int outputChannels, params IccProcessingElement[] elements)
    {
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = inputChannels;
        pipeline.OutputChannels = outputChannels;
        pipeline.InputRanges = FullRanges(inputChannels);
        pipeline.OutputRanges = FullRanges(outputChannels);
        pipeline.Elements = elements;
        pipeline.IsExecutable = elements.All(element =>
            element.Kind != IccProcessingElementKind.Unknown);
        return pipeline;
    }

    private static IccProcessingElement CurveSet(params IccCurve[] curves)
    {
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = IccProcessingElementSignatures.CurveSet;
        element.Kind = IccProcessingElementKind.CurveSet;
        element.InputChannels = curves.Length;
        element.OutputChannels = curves.Length;
        element.Curves = curves;
        return element;
    }

    private static IccProcessingElement Matrix(int inputChannels,
        int outputChannels, double[] values)
    {
        IccMatrix matrix = new IccMatrix();
        matrix.Rows = outputChannels;
        matrix.Columns = inputChannels + 1;
        matrix.Values = values;
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = IccProcessingElementSignatures.Matrix;
        element.Kind = IccProcessingElementKind.Matrix;
        element.InputChannels = inputChannels;
        element.OutputChannels = outputChannels;
        element.Matrix = matrix;
        return element;
    }

    private static IccProcessingElement Clut(int inputChannels,
        int outputChannels, int[] gridPoints, double[] values)
    {
        IccClut clut = new IccClut();
        clut.InputChannels = inputChannels;
        clut.OutputChannels = outputChannels;
        clut.GridPoints = gridPoints;
        clut.Values = values;
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = IccProcessingElementSignatures.Clut;
        element.Kind = IccProcessingElementKind.Clut;
        element.InputChannels = inputChannels;
        element.OutputChannels = outputChannels;
        element.Clut = clut;
        return element;
    }

    private static IccProcessingElement Acs(bool begin, int channels, byte[] data)
    {
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = begin ? IccProcessingElementSignatures.BeginAcs
            : IccProcessingElementSignatures.EndAcs;
        element.Kind = begin ? IccProcessingElementKind.BeginAcs
            : IccProcessingElementKind.EndAcs;
        element.InputChannels = channels;
        element.OutputChannels = channels;
        element.AcsSignature = 0x74657374;
        element.AcsData = data;
        return element;
    }

    private static IccProcessingElement FutureElement(uint signature,
        int inputChannels, int outputChannels, byte[] data)
    {
        byte[] raw = new byte[12 + data.Length];
        WriteUInt32(raw, 0, signature);
        WriteUInt16(raw, 8, (ushort)inputChannels);
        WriteUInt16(raw, 10, (ushort)outputChannels);
        data.CopyTo(raw, 12);
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = signature;
        element.Kind = IccProcessingElementKind.Unknown;
        element.InputChannels = inputChannels;
        element.OutputChannels = outputChannels;
        element.RawData.TypeSignature = signature;
        element.RawData.HasTypeSignature = true;
        element.RawData.Bytes = raw;
        return element;
    }

    private static IccCurve FormulaCurve(int formulaType, params double[] parameters)
    {
        return SegmentedCurve(FormulaSegment(double.NegativeInfinity,
            formulaType, parameters));
    }

    private static IccCurve SegmentedCurve(params IccCurveSegment[] segments)
    {
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Segmented;
        curve.Segments = segments;
        return curve;
    }

    private static IccCurveSegment FormulaSegment(double breakpoint,
        int formulaType, params double[] parameters)
    {
        IccCurveSegment segment = new IccCurveSegment();
        segment.Kind = IccCurveSegmentKind.Formula;
        segment.BreakPoint = breakpoint;
        segment.FormulaType = formulaType;
        segment.Parameters = parameters;
        return segment;
    }

    private static IccCurveSegment SampledSegment(double breakpoint,
        params double[] samples)
    {
        IccCurveSegment segment = new IccCurveSegment();
        segment.Kind = IccCurveSegmentKind.Sampled;
        segment.BreakPoint = breakpoint;
        segment.Samples = samples;
        return segment;
    }

    private static IccEncodingRange[] FullRanges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            IccEncodingRange range = new IccEncodingRange();
            range.Minimum = float.MinValue;
            range.Maximum = float.MaxValue;
            ranges[index] = range;
            index++;
        }

        return ranges;
    }

    private static double[] IdentityThreeChannelClutValues()
    {
        return new[]
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

    private static TagDecodeState Decode(byte[] payload)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length,
            0, IccVersionKind.Version4, true);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static double[] Execute(MpePipelinePlan plan, double[] input)
    {
        double[] output = new double[plan.DestinationChannels];
        int valueLength = PipelineExecutor.GetRequiredValueScratchLength(plan.Stages);
        int indexLength = PipelineExecutor.GetRequiredClutIndexScratchLength(plan.Stages);
        int weightLength = PipelineExecutor.GetRequiredClutWeightScratchLength(plan.Stages);
        PipelineExecutor.ExecutePipeline(plan.Stages, input, output,
            new double[valueLength], new double[valueLength],
            new int[indexLength], new double[weightLength]);
        return output;
    }

    private static void AssertInvalid(byte[] payload, string message)
    {
        TagDecodeState state = Decode(payload);
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(state.Result.Message, Does.Contain(message).IgnoreCase);
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
    }

    private static void WriteUInt16(byte[] destination, int offset, ushort value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
