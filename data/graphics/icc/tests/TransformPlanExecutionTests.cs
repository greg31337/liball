using icc;

namespace icc.Tests;

public class TransformPlanExecutionTests
{
    [Test]
    public void GrayPlanSupportsSingleStridedOverlappingAndTraceOperations()
    {
        IccProfile profile = Profile(IccVersionKind.Version4, IccColorSpaceSignatures.Gray);
        profile.Tags = new IccTag[] { CurveTag(IccTagSignatures.GrayTrc) };
        MatrixTrcPlan plan = MatrixTrcCompiler.Compile(profile, IccTransformDirection.DeviceToPcs);
        float value = 0.25f;

        AssertExecutionSurface(Configure(plan.Stages, plan.SourceChannels, plan.DestinationChannels,
            NormalizedRanges(plan.SourceChannels), NormalizedRanges(plan.DestinationChannels), 0),
            new float[] { value }, new float[]
            {
                value * (float)IccStandardColorimetry.D50X,
                value * (float)IccStandardColorimetry.D50Y,
                value * (float)IccStandardColorimetry.D50Z
            });
    }

    [Test]
    public void RgbMatrixTrcPlanSupportsSingleStridedOverlappingAndTraceOperations()
    {
        IccProfile profile = Profile(IccVersionKind.Version4, IccColorSpaceSignatures.Rgb);
        profile.Tags = new IccTag[]
        {
            CurveTag(IccTagSignatures.RedTrc),
            CurveTag(IccTagSignatures.GreenTrc),
            CurveTag(IccTagSignatures.BlueTrc),
            XyzTag(IccTagSignatures.RedMatrixColumn, 1.0, 0.0, 0.0),
            XyzTag(IccTagSignatures.GreenMatrixColumn, 0.0, 1.0, 0.0),
            XyzTag(IccTagSignatures.BlueMatrixColumn, 0.0, 0.0, 1.0)
        };
        MatrixTrcPlan plan = MatrixTrcCompiler.Compile(profile, IccTransformDirection.DeviceToPcs);
        float[] input = new float[] { 0.2f, 0.4f, 0.6f };

        AssertExecutionSurface(Configure(plan.Stages, plan.SourceChannels, plan.DestinationChannels,
            NormalizedRanges(plan.SourceChannels), NormalizedRanges(plan.DestinationChannels), 0), input, input);
    }

    [Test]
    public void LutPlanSupportsSingleStridedOverlappingAndTraceOperations()
    {
        IccProfile profile = Profile(IccVersionKind.Version4, IccColorSpaceSignatures.Rgb);
        IccMultiStageLut lut = new IccMultiStageLut
        {
            InputChannels = 3,
            OutputChannels = 3,
            BCurves = IdentityCurves(3),
            PcsEncoding = IccPcsEncodingKind.Version4
        };
        IccTag tag = LutTag(IccTagSignatures.AToB1, lut);
        LutPipelinePlan plan = LutPipelineCompiler.Compile(profile, tag, IccTransformDirection.DeviceToPcs,
            IccInterpolationKind.Multilinear);
        float[] input = new float[] { 0.2f, 0.4f, 0.6f };
        float maximum = (float)PcsConverter.XyzMaximum;
        float[] expected = new float[] { input[0] * maximum, input[1] * maximum, input[2] * maximum };

        AssertExecutionSurface(Configure(plan.Stages, plan.SourceChannels, plan.DestinationChannels,
            plan.SourceRanges, plan.DestinationRanges, plan.TagSignature), input, expected);
    }

    [Test]
    public void VersionFourMpePlanSupportsSingleStridedOverlappingAndTraceOperations()
    {
        AssertMpeExecutionSurface(IccVersionKind.Version4, IccTagSignatures.DToB1);
    }

    [Test]
    public void VersionFiveMpePlanSupportsSingleStridedOverlappingAndTraceOperations()
    {
        AssertMpeExecutionSurface(IccVersionKind.Version5, IccTagSignatures.AToB1);
    }

    private static void AssertMpeExecutionSurface(IccVersionKind version, uint signature)
    {
        IccProfile profile = Profile(version, IccColorSpaceSignatures.Rgb);
        IccTag tag = MpeTag(signature);
        MpePipelinePlan plan = MpePipelineCompiler.Compile(profile, tag, IccTransformDirection.DeviceToPcs,
            IccInterpolationKind.Multilinear);
        float[] input = new float[] { 0.2f, 0.4f, 0.6f };

        AssertExecutionSurface(Configure(plan.Stages, plan.SourceChannels, plan.DestinationChannels,
            plan.SourceRanges, plan.DestinationRanges, plan.TagSignature), input, input);
    }

    private static void AssertExecutionSurface(IccColorTransform transform, float[] input, float[] expected)
    {
        IIccColorTransform api = transform;
        IccOperationResult probe = api.ProbeTransformColor(input);
        float[] single = api.TransformSingleColor(input);
        int sourceChannels = input.Length;
        int destinationChannels = expected.Length;
        int sourceStride = sourceChannels + 1;
        int destinationStride = destinationChannels + 1;
        float[] stridedSource = new float[(sourceStride * 2) - 1];
        float[] stridedDestination = Enumerable.Repeat(-1.0f, (destinationStride * 2) - 1).ToArray();
        CopyColor(input, stridedSource, 0);
        CopyColor(input, stridedSource, sourceStride);
        api.TransformInterleavedColors(stridedSource, sourceStride, stridedDestination, destinationStride, 2);

        int packedSourceLength = sourceChannels * 2;
        int packedDestinationLength = destinationChannels * 2;
        float[] overlap = Enumerable.Repeat(-1.0f, System.Math.Max(packedSourceLength, packedDestinationLength + 1)).ToArray();
        CopyColor(input, overlap, 0);
        CopyColor(input, overlap, sourceChannels);
        api.TransformInterleavedColors(overlap.AsSpan(0, packedSourceLength), sourceChannels,
            overlap.AsSpan(1, packedDestinationLength), destinationChannels, 2);
        IccTransformTrace? trace = transform.Core.Plan.Trace;

        Assert.Multiple(() =>
        {
            Assert.That(api.GetSourceChannelCount(), Is.EqualTo(sourceChannels));
            Assert.That(api.GetDestinationChannelCount(), Is.EqualTo(destinationChannels));
            Assert.That(probe.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(single, Is.EqualTo(expected).Within(0.000002f));
            Assert.That(stridedDestination.AsSpan(0, destinationChannels).ToArray(), Is.EqualTo(expected).Within(0.000002f));
            Assert.That(stridedDestination[destinationChannels], Is.EqualTo(-1.0f));
            Assert.That(stridedDestination.AsSpan(destinationStride, destinationChannels).ToArray(), Is.EqualTo(expected).Within(0.000002f));
            Assert.That(overlap.AsSpan(1, destinationChannels).ToArray(), Is.EqualTo(expected).Within(0.000002f));
            Assert.That(overlap.AsSpan(1 + destinationChannels, destinationChannels).ToArray(), Is.EqualTo(expected).Within(0.000002f));
            Assert.That(trace, Is.Not.Null);
            Assert.That(trace!.Source.Components, Is.EqualTo(input.Select(value => (double)value).ToArray()).Within(0.000002));
            Assert.That(trace.Destination.Components, Is.EqualTo(expected.Select(value => (double)value).ToArray()).Within(0.000002));
            Assert.That(trace.Intermediates, Is.Not.Empty);
        });
    }

    private static void CopyColor(float[] source, float[] destination, int offset)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index++;
        }
    }

    private static IccColorTransform Configure(PipelineStage[] stages, int sourceChannels, int destinationChannels,
        IccEncodingRange[] sourceRanges, IccEncodingRange[] destinationRanges, uint tagSignature)
    {
        IccColorTransform transform = new IccColorTransform();
        transform.ConfigureCompiledPlan(stages, sourceChannels, destinationChannels, sourceRanges, destinationRanges,
            new IccTransformOptions { ClipOutput = false, EnableTrace = true }, tagSignature);
        return transform;
    }

    private static IccProfile Profile(IccVersionKind version, uint dataSpace)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = version;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.DataColorSpaceSignature = dataSpace;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        return profile;
    }

    private static IccTag CurveTag(uint signature)
    {
        return new IccTag
        {
            Signature = signature,
            Data = new IccTagData
            {
                Kind = IccTagDataKind.Curve,
                Curve = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 1.0 }
            }
        };
    }

    private static IccTag XyzTag(uint signature, double x, double y, double z)
    {
        return new IccTag
        {
            Signature = signature,
            Data = new IccTagData
            {
                Kind = IccTagDataKind.XyzArray,
                XyzValues = new IccXyzNumber[] { new IccXyzNumber { X = x, Y = y, Z = z } }
            }
        };
    }

    private static IccTag LutTag(uint signature, IccMultiStageLut lut)
    {
        IccTagData data = new IccTagData { Kind = IccTagDataKind.Lut, MultiStageLut = lut };
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.LutAToB;
        return new IccTag { Signature = signature, Data = data };
    }

    private static IccTag MpeTag(uint signature)
    {
        IccMultiProcessElements pipeline = new IccMultiProcessElements
        {
            InputChannels = 3,
            OutputChannels = 3,
            InputRanges = FullRanges(3),
            OutputRanges = FullRanges(3),
            Elements = new IccProcessingElement[]
            {
                new IccProcessingElement
                {
                    Signature = IccProcessingElementSignatures.CurveSet,
                    Kind = IccProcessingElementKind.CurveSet,
                    InputChannels = 3,
                    OutputChannels = 3,
                    Curves = SegmentedIdentityCurves(3)
                }
            },
            IsExecutable = true
        };
        IccTagData data = new IccTagData { Kind = IccTagDataKind.MultiProcessElements, MultiProcessElements = pipeline };
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        return new IccTag { Signature = signature, Data = data };
    }

    private static IccCurve[] IdentityCurves(int count)
    {
        IccCurve[] curves = new IccCurve[count];
        int index = 0;
        while (index < count)
        {
            curves[index] = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 1.0 };
            index++;
        }
        return curves;
    }

    private static IccCurve[] SegmentedIdentityCurves(int count)
    {
        IccCurve[] curves = new IccCurve[count];
        int index = 0;
        while (index < count)
        {
            IccCurveSegment segment = new IccCurveSegment
            {
                Kind = IccCurveSegmentKind.Formula,
                FormulaType = 0,
                BreakPoint = double.NegativeInfinity,
                Parameters = new double[] { 1.0, 1.0, 0.0, 0.0 }
            };
            curves[index] = new IccCurve { Kind = IccCurveKind.Segmented, Segments = new IccCurveSegment[] { segment } };
            index++;
        }
        return curves;
    }

    private static IccEncodingRange[] NormalizedRanges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = new IccEncodingRange { Minimum = 0.0, Maximum = 1.0 };
            index++;
        }
        return ranges;
    }

    private static IccEncodingRange[] FullRanges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = new IccEncodingRange { Minimum = float.MinValue, Maximum = float.MaxValue };
            index++;
        }
        return ranges;
    }
}
