using icc;

namespace icc.Tests;

public class ColorTransformExecutionTests
{
    [Test]
    public void SingleColorExecutionClipsOutputReportsGamutAndTracesEveryStage()
    {
        PipelineStage matrix = MatrixStage(2, 2, new double[]
        {
            2.0, 0.0, 0.1,
            0.0, 2.0, -0.1
        }, "affine conversion");
        PipelineStage identity = new PipelineStage
        {
            Kind = PipelineStageKind.Identity,
            InputChannels = 2,
            OutputChannels = 2,
            Description = "identity handoff"
        };
        IccEncodingRange[] destinationRanges = new IccEncodingRange[]
        {
            Range(0.0, 1.0),
            Range(0.2, 0.8)
        };
        IccTransformOptions options = new IccTransformOptions { ClipOutput = true, EnableTrace = true };
        IccColorTransform transform = Configure(new PipelineStage[] { matrix, identity }, 2, 2, destinationRanges, options, IccTagSignatures.AToB1);

        matrix.Matrix!.Values[0] = 100.0;
        destinationRanges[0].Maximum = 9.0;
        options.ClipOutput = false;
        options.EnableTrace = false;
        float[] output = transform.TransformSingleColor(new float[] { 0.5f, 0.25f });
        IccTransformTrace trace = transform.Core.Plan.Trace!;

        Assert.Multiple(() =>
        {
            Assert.That(transform.GetSourceChannelCount(), Is.EqualTo(2));
            Assert.That(transform.GetDestinationChannelCount(), Is.EqualTo(2));
            Assert.That(output, Is.EqualTo(new float[] { 1.0f, 0.4f }).Within(0.000001f));
            Assert.That(transform.Core.LastGamutStatus, Is.EqualTo(IccGamutStatus.Outside));
            Assert.That(trace.Source.Components, Is.EqualTo(new double[] { 0.5, 0.25 }));
            Assert.That(trace.Intermediates, Has.Length.EqualTo(2));
            Assert.That(trace.Intermediates[0].Components, Is.EqualTo(new double[] { 1.1, 0.4 }).Within(1e-14));
            Assert.That(trace.Intermediates[1].Components, Is.EqualTo(new double[] { 1.1, 0.4 }).Within(1e-14));
            Assert.That(trace.Destination.Components, Is.EqualTo(new double[] { 1.0, 0.4 }).Within(1e-14));
            Assert.That(trace.Messages, Is.EqualTo(new string[] { "affine conversion", "identity handoff" }));
            Assert.That(transform.Core.Plan.Stages.Select(stage => stage.TagSignature), Is.EqualTo(new uint[] { IccTagSignatures.AToB1, IccTagSignatures.AToB1 }));
            Assert.That(transform.Core.Plan.Options.ClipOutput, Is.True);
            Assert.That(transform.Core.Plan.Options.EnableTrace, Is.True);
        });
    }

    [Test]
    public void ClippingCanBeDisabledWhilePreClippingGamutStatusRemainsAvailable()
    {
        PipelineStage stage = MatrixStage(1, 1, new double[] { 2.0, 0.5 }, "extended output");
        IccTransformOptions options = new IccTransformOptions { ClipOutput = false, EnableTrace = false };
        IccColorTransform transform = Configure(new PipelineStage[] { stage }, 1, 1, new IccEncodingRange[] { Range(0.0, 1.0) }, options, 0);

        float[] output = transform.TransformSingleColor(new float[] { 0.5f });

        Assert.Multiple(() =>
        {
            Assert.That(output, Is.EqualTo(new float[] { 1.5f }));
            Assert.That(transform.Core.LastGamutStatus, Is.EqualTo(IccGamutStatus.Outside));
            Assert.That(transform.Core.Plan.Trace, Is.Null);
        });
    }

    [Test]
    public void DestinationRangesDistinguishInsideBoundaryOutsideAndUnknownResults()
    {
        IccTransformOptions options = new IccTransformOptions { ClipOutput = false };
        PipelineStage identity = new PipelineStage { Kind = PipelineStageKind.Identity, InputChannels = 1, OutputChannels = 1 };
        IccColorTransform ranged = Configure(new PipelineStage[] { identity }, 1, 1, new IccEncodingRange[] { Range(0.0, 1.0) }, options, 0);
        IccColorTransform unknown = Configure(new PipelineStage[] { identity }, 1, 1, new IccEncodingRange[0], options, 0);

        ranged.TransformSingleColor(new float[] { 0.5f });
        IccGamutStatus inside = ranged.Core.LastGamutStatus;
        ranged.TransformSingleColor(new float[] { 1.0f });
        IccGamutStatus boundary = ranged.Core.LastGamutStatus;
        ranged.TransformSingleColor(new float[] { 1.5f });
        IccGamutStatus outside = ranged.Core.LastGamutStatus;
        unknown.TransformSingleColor(new float[] { 0.5f });

        Assert.Multiple(() =>
        {
            Assert.That(inside, Is.EqualTo(IccGamutStatus.Inside));
            Assert.That(boundary, Is.EqualTo(IccGamutStatus.Boundary));
            Assert.That(outside, Is.EqualTo(IccGamutStatus.Outside));
            Assert.That(unknown.Core.LastGamutStatus, Is.EqualTo(IccGamutStatus.Unknown));
        });
    }

    [Test]
    public void ProbeAndThrowingExecutionValidateExactChannelsAndFiniteInputs()
    {
        PipelineStage identity = new PipelineStage { Kind = PipelineStageKind.Identity, InputChannels = 2, OutputChannels = 2 };
        IccColorTransform transform = Configure(new PipelineStage[] { identity }, 2, 2,
            new IccEncodingRange[] { Range(0.0, 1.0), Range(0.0, 1.0) }, new IccTransformOptions(), 0);

        IccOperationResult valid = transform.ProbeTransformColor(new float[] { 0.25f, 0.75f });
        IccOperationResult shortInput = transform.ProbeTransformColor(new float[] { 0.25f });
        IccOperationResult nonFinite = transform.ProbeTransformColor(new float[] { 0.25f, float.NaN });

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(shortInput.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(shortInput.Message, Does.Contain("requires 2"));
            Assert.That(nonFinite.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(nonFinite.Message, Does.Contain("channel 1"));
            Assert.That(() => transform.TransformSingleColor(new float[] { 0.25f }), Throws.TypeOf<IccTransformException>());
            Assert.That(() => transform.TransformSingleColor(new float[] { 0.25f, float.PositiveInfinity }), Throws.TypeOf<IccTransformException>());
        });
    }

    [Test]
    public void ConfigurationRejectsEndpointRangeAndStageMismatchesBeforeReplacingAValidPlan()
    {
        PipelineStage identity = new PipelineStage { Kind = PipelineStageKind.Identity, InputChannels = 1, OutputChannels = 1 };
        IccColorTransform transform = Configure(new PipelineStage[] { identity }, 1, 1, new IccEncodingRange[] { Range(0.0, 1.0) }, new IccTransformOptions(), 0);
        PipelineStage wrongEndpoint = new PipelineStage { Kind = PipelineStageKind.Identity, InputChannels = 2, OutputChannels = 2 };

        Assert.Multiple(() =>
        {
            Assert.That(() => transform.ConfigureCompiledPlan(new PipelineStage[] { wrongEndpoint }, 1, 2, new IccEncodingRange[0], new IccEncodingRange[0], new IccTransformOptions(), 0), Throws.TypeOf<IccTransformException>());
            Assert.That(() => transform.ConfigureCompiledPlan(new PipelineStage[] { identity }, 1, 1, new IccEncodingRange[0], new IccEncodingRange[] { Range(1.0, 0.0) }, new IccTransformOptions(), 0), Throws.TypeOf<IccTransformException>());
            Assert.That(transform.TransformSingleColor(new float[] { 0.5f }), Is.EqualTo(new float[] { 0.5f }));
        });
    }

    [Test]
    public void ExecutionRejectsFiniteDoubleResultsThatCannotBeRepresentedAsFloat()
    {
        PipelineStage stage = MatrixStage(1, 1, new double[] { double.MaxValue, 0.0 }, "oversized output");
        IccColorTransform transform = Configure(new PipelineStage[] { stage }, 1, 1, new IccEncodingRange[0], new IccTransformOptions { ClipOutput = false }, 0);

        Assert.That(() => transform.TransformSingleColor(new float[] { 1.0f }), Throws.TypeOf<IccTransformException>().With.Message.Contains("finite float"));
    }

    [Test]
    public void InterleavedExecutionHonorsBothStridesAndAggregatesGamutStatus()
    {
        PipelineStage stage = MatrixStage(2, 1, new double[] { 1.0, 1.0, 0.0 }, "sum channels");
        IccColorTransform transform = Configure(new PipelineStage[] { stage }, 2, 1,
            new IccEncodingRange[] { Range(0.0, 1.0) }, new IccTransformOptions { ClipOutput = true }, 0);
        float[] source = new float[] { 0.1f, 0.2f, 99.0f, 0.4f, 0.6f, 99.0f, 0.8f, 0.7f };
        float[] destination = new float[] { -9.0f, -8.0f, -9.0f, -8.0f, -9.0f };

        transform.TransformInterleavedColors(source, 3, destination, 2, 3);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new float[] { 0.3f, -8.0f, 1.0f, -8.0f, 1.0f }).Within(0.000001f));
            Assert.That(transform.Core.LastGamutStatus, Is.EqualTo(IccGamutStatus.Outside));
            Assert.That(transform.Core.Plan.Trace, Is.Null);
        });
    }

    [Test]
    public void InterleavedExecutionSnapshotsDestructivelyOverlappingSourceAndDestination()
    {
        PipelineStage identity = new PipelineStage { Kind = PipelineStageKind.Identity, InputChannels = 2, OutputChannels = 2 };
        IccColorTransform transform = Configure(new PipelineStage[] { identity }, 2, 2,
            new IccEncodingRange[0], new IccTransformOptions(), 0);
        float[] buffer = new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f };

        transform.TransformInterleavedColors(buffer.AsSpan(0, 6), 2, buffer.AsSpan(0, 8), 3, 3);

        Assert.That(buffer, Is.EqualTo(new float[] { 1.0f, 2.0f, 3.0f, 3.0f, 4.0f, 6.0f, 5.0f, 6.0f }));
    }

    [Test]
    public void InterleavedExecutionRequiresExactBuffersValidStridesAndNonnegativeCount()
    {
        PipelineStage stage = MatrixStage(2, 1, new double[] { 1.0, 1.0, 0.0 }, "sum channels");
        IccColorTransform transform = Configure(new PipelineStage[] { stage }, 2, 1,
            new IccEncodingRange[0], new IccTransformOptions(), 0);
        float[] validSource = new float[] { 0.1f, 0.2f, 9.0f, 0.3f, 0.4f };
        float[] validDestination = new float[] { -1.0f, -2.0f, -1.0f };

        Assert.Multiple(() =>
        {
            Assert.That(() => transform.TransformInterleavedColors(new float[4], 3, validDestination, 2, 2), Throws.TypeOf<IccTransformException>());
            Assert.That(() => transform.TransformInterleavedColors(new float[6], 3, validDestination, 2, 2), Throws.TypeOf<IccTransformException>());
            Assert.That(() => transform.TransformInterleavedColors(validSource, 3, new float[2], 2, 2), Throws.TypeOf<IccTransformException>());
            Assert.That(() => transform.TransformInterleavedColors(validSource, 3, new float[4], 2, 2), Throws.TypeOf<IccTransformException>());
            Assert.That(() => transform.TransformInterleavedColors(validSource, 1, validDestination, 2, 2), Throws.TypeOf<IccTransformException>());
            Assert.That(() => transform.TransformInterleavedColors(validSource, 3, validDestination, 0, 2), Throws.TypeOf<IccTransformException>());
            Assert.That(() => transform.TransformInterleavedColors(Array.Empty<float>(), 2, Array.Empty<float>(), 1, -1), Throws.TypeOf<IccTransformException>());
            Assert.That(() => transform.TransformInterleavedColors(Array.Empty<float>(), int.MaxValue, validDestination, 2, 2), Throws.TypeOf<IccTransformException>().With.Message.Contains("managed size"));
            Assert.That(validDestination, Is.EqualTo(new float[] { -1.0f, -2.0f, -1.0f }));
        });
    }

    [Test]
    public void InterleavedExecutionValidatesEveryInputBeforeWritingAnyOutput()
    {
        PipelineStage identity = new PipelineStage { Kind = PipelineStageKind.Identity, InputChannels = 2, OutputChannels = 2 };
        IccColorTransform transform = Configure(new PipelineStage[] { identity }, 2, 2,
            new IccEncodingRange[0], new IccTransformOptions(), 0);
        float[] source = new float[] { 0.1f, 0.2f, float.NaN, 0.3f, float.PositiveInfinity };
        float[] destination = new float[] { 7.0f, 8.0f, 9.0f, 10.0f };

        Assert.Multiple(() =>
        {
            Assert.That(() => transform.TransformInterleavedColors(source, 3, destination, 2, 2),
                Throws.TypeOf<IccTransformException>().With.Message.Contains("colour 1, channel 1"));
            Assert.That(destination, Is.EqualTo(new float[] { 7.0f, 8.0f, 9.0f, 10.0f }));
        });
    }

    [Test]
    public void EmptyInterleavedExecutionRequiresEmptyBuffersAndClearsExecutionState()
    {
        PipelineStage identity = new PipelineStage { Kind = PipelineStageKind.Identity, InputChannels = 1, OutputChannels = 1 };
        IccColorTransform transform = Configure(new PipelineStage[] { identity }, 1, 1,
            new IccEncodingRange[] { Range(0.0, 1.0) }, new IccTransformOptions { EnableTrace = true }, 0);
        transform.TransformInterleavedColors(new float[] { 0.5f }, 1, new float[1], 1, 1);

        transform.TransformInterleavedColors(Array.Empty<float>(), 1, Array.Empty<float>(), 1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(transform.Core.LastGamutStatus, Is.EqualTo(IccGamutStatus.Unknown));
            Assert.That(transform.Core.Plan.Trace, Is.Null);
            Assert.That(() => transform.TransformInterleavedColors(new float[1], 1, Array.Empty<float>(), 1, 0), Throws.TypeOf<IccTransformException>());
            Assert.That(() => transform.TransformInterleavedColors(Array.Empty<float>(), 1, new float[1], 1, 0), Throws.TypeOf<IccTransformException>());
        });
    }

    [Test]
    public void InterleavedTraceDescribesTheLastColorAndItsClippedDestination()
    {
        PipelineStage stage = MatrixStage(1, 1, new double[] { 2.0, 0.0 }, "double channel");
        IccColorTransform transform = Configure(new PipelineStage[] { stage }, 1, 1,
            new IccEncodingRange[] { Range(0.0, 1.0) }, new IccTransformOptions { ClipOutput = true, EnableTrace = true }, 0);
        float[] destination = new float[2];

        transform.TransformInterleavedColors(new float[] { 0.25f, 0.75f }, 1, destination, 1, 2);
        IccTransformTrace trace = transform.Core.Plan.Trace!;

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new float[] { 0.5f, 1.0f }));
            Assert.That(transform.Core.LastGamutStatus, Is.EqualTo(IccGamutStatus.Outside));
            Assert.That(trace.Source.Components, Is.EqualTo(new double[] { 0.75 }));
            Assert.That(trace.Intermediates, Has.Length.EqualTo(1));
            Assert.That(trace.Intermediates[0].Components, Is.EqualTo(new double[] { 1.5 }));
            Assert.That(trace.Destination.Components, Is.EqualTo(new double[] { 1.0 }));
        });
    }

    [Test]
    public void InterleavedExecutionReusesScratchAcrossAllColors()
    {
        PipelineStage identity = new PipelineStage { Kind = PipelineStageKind.Identity, InputChannels = 1, OutputChannels = 1 };
        IccColorTransform transform = Configure(new PipelineStage[] { identity }, 1, 1,
            new IccEncodingRange[0], new IccTransformOptions(), 0);
        float[] oneSource = new float[1];
        float[] oneDestination = new float[1];
        float[] manySource = new float[100];
        float[] manyDestination = new float[100];
        int warmup = 0;
        while (warmup < 10)
        {
            transform.TransformInterleavedColors(oneSource, 1, oneDestination, 1, 1);
            transform.TransformInterleavedColors(manySource, 1, manyDestination, 1, 100);
            warmup++;
        }

        long beforeOne = GC.GetAllocatedBytesForCurrentThread();
        transform.TransformInterleavedColors(oneSource, 1, oneDestination, 1, 1);
        long oneAllocation = GC.GetAllocatedBytesForCurrentThread() - beforeOne;
        long beforeMany = GC.GetAllocatedBytesForCurrentThread();
        transform.TransformInterleavedColors(manySource, 1, manyDestination, 1, 100);
        long manyAllocation = GC.GetAllocatedBytesForCurrentThread() - beforeMany;

        Assert.That(manyAllocation, Is.EqualTo(oneAllocation));
    }

    private static IccColorTransform Configure(PipelineStage[] stages, int sourceChannels, int destinationChannels,
        IccEncodingRange[] destinationRanges, IccTransformOptions options, uint tagSignature)
    {
        IccColorTransform transform = new IccColorTransform();
        transform.ConfigureCompiledPlan(stages, sourceChannels, destinationChannels, NormalizedRanges(sourceChannels), destinationRanges, options, tagSignature);
        return transform;
    }

    private static PipelineStage MatrixStage(int inputChannels, int outputChannels, double[] values, string description)
    {
        return new PipelineStage
        {
            Kind = PipelineStageKind.Matrix,
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            Matrix = new IccMatrix { Rows = outputChannels, Columns = inputChannels + 1, Values = values },
            Description = description
        };
    }

    private static IccEncodingRange Range(double minimum, double maximum)
    {
        return new IccEncodingRange { Minimum = minimum, Maximum = maximum };
    }

    private static IccEncodingRange[] NormalizedRanges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = Range(0.0, 1.0);
            index++;
        }
        return ranges;
    }
}
