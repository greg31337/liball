using icc;

namespace icc.Tests;

public class PipelineExecutorTests
{
    [Test]
    public void PipelineExecutesCurvesMatrixOffsetsClippingChannelDefaultsAndIdentityInOrder()
    {
        PipelineStage curves = new PipelineStage
        {
            Kind = PipelineStageKind.Curves,
            InputChannels = 2,
            OutputChannels = 2,
            Curves = new IccCurve[]
            {
                new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 2.0 },
                new IccCurve()
            }
        };
        PipelineStage matrix = new PipelineStage
        {
            Kind = PipelineStageKind.Matrix,
            InputChannels = 2,
            OutputChannels = 2,
            Matrix = new IccMatrix
            {
                Rows = 2,
                Columns = 3,
                Values = new double[]
                {
                    2.0, 0.0, 0.1,
                    0.0, 1.0, -0.1
                }
            }
        };
        PipelineStage clip = new PipelineStage
        {
            Kind = PipelineStageKind.Clip,
            InputChannels = 2,
            OutputChannels = 2,
            Ranges = new IccEncodingRange[]
            {
                new IccEncodingRange { Minimum = 0.0, Maximum = 0.55 },
                new IccEncodingRange { Minimum = 0.2, Maximum = 0.65 }
            }
        };
        PipelineStage channelMap = new PipelineStage
        {
            Kind = PipelineStageKind.ChannelMap,
            InputChannels = 2,
            OutputChannels = 3,
            ChannelOrder = new int[] { 1, -1, 0 },
            ChannelDefaults = new double[] { 0.0, 0.25, 0.0 }
        };
        PipelineStage identity = new PipelineStage
        {
            Kind = PipelineStageKind.Identity,
            InputChannels = 3,
            OutputChannels = 3
        };
        PipelineStage[] stages = new PipelineStage[] { curves, matrix, clip, channelMap, identity };
        double[] output = new double[4] { -1.0, -1.0, -1.0, 99.0 };
        int scratchLength = PipelineExecutor.GetRequiredValueScratchLength(stages);

        PipelineExecutor.ExecutePipeline(stages, new double[] { 0.5, 0.8 }, output, new double[scratchLength], new double[scratchLength], new int[0], new double[0]);

        Assert.Multiple(() =>
        {
            Assert.That(output[0], Is.EqualTo(0.65).Within(1e-15));
            Assert.That(output[1], Is.EqualTo(0.25));
            Assert.That(output[2], Is.EqualTo(0.55).Within(1e-15));
            Assert.That(output[3], Is.EqualTo(99.0));
            Assert.That(scratchLength, Is.EqualTo(3));
        });
    }

    [Test]
    public void ClutStageUsesItsExplicitInterpolationSelection()
    {
        IccClut clut = new IccClut
        {
            InputChannels = 3,
            OutputChannels = 1,
            GridPoints = new int[] { 2, 2, 2 },
            Values = new double[] { 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0 }
        };
        PipelineStage stage = new PipelineStage
        {
            Kind = PipelineStageKind.Clut,
            InputChannels = 3,
            OutputChannels = 1,
            Clut = clut,
            Interpolation = IccInterpolationKind.Multilinear
        };
        PipelineStage[] stages = new PipelineStage[] { stage };
        double[] input = new double[] { 0.8, 0.5, 0.2 };
        double[] output = new double[1];
        double[] firstScratch = new double[3];
        double[] secondScratch = new double[3];
        int[] indexScratch = new int[PipelineExecutor.GetRequiredClutIndexScratchLength(stages)];
        double[] weightScratch = new double[PipelineExecutor.GetRequiredClutWeightScratchLength(stages)];

        PipelineExecutor.ExecutePipeline(stages, input, output, firstScratch, secondScratch, indexScratch, weightScratch);
        double multilinear = output[0];
        stage.Interpolation = IccInterpolationKind.Tetrahedral;
        PipelineExecutor.ExecutePipeline(stages, input, output, firstScratch, secondScratch, indexScratch, weightScratch);

        Assert.Multiple(() =>
        {
            Assert.That(multilinear, Is.EqualTo(0.08).Within(1e-14));
            Assert.That(output[0], Is.Zero);
            Assert.That(indexScratch, Has.Length.EqualTo(6));
            Assert.That(weightScratch, Has.Length.EqualTo(3));
        });
    }

    [TestCase(IccPcsEncodingKind.Version2)]
    [TestCase(IccPcsEncodingKind.Version4)]
    public void PcsStagesNormalizeAndDenormalizeLabWithoutTemporaryArrays(IccPcsEncodingKind encoding)
    {
        PipelineStage normalize = new PipelineStage
        {
            Kind = PipelineStageKind.PcsNormalize,
            InputChannels = 3,
            OutputChannels = 3,
            PcsKind = IccPcsKind.Lab,
            PcsEncoding = encoding
        };
        PipelineStage denormalize = new PipelineStage
        {
            Kind = PipelineStageKind.PcsDenormalize,
            InputChannels = 3,
            OutputChannels = 3,
            PcsKind = IccPcsKind.Lab,
            PcsEncoding = encoding
        };
        double[] actual = new double[] { 50.0, 0.0, 127.0 };
        double[] normalized = new double[3];
        double[] roundTrip = new double[3];
        double[] firstScratch = new double[3];
        double[] secondScratch = new double[3];

        PipelineExecutor.ExecutePipeline(new PipelineStage[] { normalize }, actual, normalized, firstScratch, secondScratch, new int[0], new double[0]);
        PipelineExecutor.ExecutePipeline(new PipelineStage[] { denormalize }, normalized, roundTrip, firstScratch, secondScratch, new int[0], new double[0]);

        double[] expected = PcsConverter.NormalizePcs(actual, IccPcsKind.Lab, encoding);
        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.EqualTo(expected).Within(1e-15));
            Assert.That(roundTrip, Is.EqualTo(actual).Within(1e-12));
        });
    }

    [Test]
    public void VersionFivePcsStagesPreserveDirectExtendedRangeValues()
    {
        PipelineStage directPcs = new PipelineStage
        {
            Kind = PipelineStageKind.PcsNormalize,
            InputChannels = 3,
            OutputChannels = 3,
            PcsKind = IccPcsKind.Lab,
            PcsEncoding = IccPcsEncodingKind.Version5
        };
        double[] input = new double[] { 120.0, -160.0, 180.0 };
        double[] output = new double[3];

        PipelineExecutor.ExecutePipeline(new PipelineStage[] { directPcs }, input, output, new double[3], new double[3], new int[0], new double[0]);

        Assert.That(output, Is.EqualTo(input));
    }

    [Test]
    public void EmptyPipelineIsAnExactIdentityAndPreservesExtraOutputValues()
    {
        double[] input = new double[] { -0.0, 2.0 };
        double[] output = new double[] { 7.0, 7.0, 123.0 };

        PipelineExecutor.ExecutePipeline(new PipelineStage[0], input, output, new double[0], new double[0], new int[0], new double[0]);

        Assert.Multiple(() =>
        {
            Assert.That(BitConverter.DoubleToInt64Bits(output[0]), Is.EqualTo(BitConverter.DoubleToInt64Bits(-0.0)));
            Assert.That(output[1], Is.EqualTo(2.0));
            Assert.That(output[2], Is.EqualTo(123.0));
        });
    }

    [Test]
    public void RepeatedPipelineExecutionDoesNotAllocatePerSample()
    {
        PipelineStage identity = new PipelineStage
        {
            Kind = PipelineStageKind.Identity,
            InputChannels = 3,
            OutputChannels = 3
        };
        PipelineStage[] stages = new PipelineStage[] { identity };
        double[] input = new double[] { 0.1, 0.2, 0.3 };
        double[] output = new double[3];
        double[] firstScratch = new double[3];
        double[] secondScratch = new double[3];
        int[] emptyIndexes = new int[0];
        double[] emptyWeights = new double[0];
        PipelineExecutor.ExecutePipeline(stages, input, output, firstScratch, secondScratch, emptyIndexes, emptyWeights);

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        int sample = 0;
        while (sample < 100)
        {
            input[0] = sample / 100.0;
            PipelineExecutor.ExecutePipeline(stages, input, output, firstScratch, secondScratch, emptyIndexes, emptyWeights);
            sample++;
        }
        long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

        Assert.Multiple(() =>
        {
            Assert.That(allocatedAfter, Is.EqualTo(allocatedBefore));
            Assert.That(output, Is.EqualTo(input));
        });
    }

    [Test]
    public void UnsupportedAndMalformedStagesAreRejectedBeforeOutputIsChanged()
    {
        PipelineStage identity = new PipelineStage
        {
            Kind = PipelineStageKind.Identity,
            InputChannels = 2,
            OutputChannels = 2
        };
        PipelineStage unsupported = new PipelineStage
        {
            Kind = PipelineStageKind.Unsupported,
            InputChannels = 2,
            OutputChannels = 2,
            Description = "future calculator operation"
        };
        PipelineStage discontinuous = new PipelineStage
        {
            Kind = PipelineStageKind.Identity,
            InputChannels = 3,
            OutputChannels = 3
        };
        PipelineStage badMap = new PipelineStage
        {
            Kind = PipelineStageKind.ChannelMap,
            InputChannels = 2,
            OutputChannels = 1,
            ChannelOrder = new int[] { 2 },
            ChannelDefaults = new double[] { 0.0 }
        };
        PipelineStage badClip = new PipelineStage
        {
            Kind = PipelineStageKind.Clip,
            InputChannels = 2,
            OutputChannels = 2,
            Ranges = new IccEncodingRange[]
            {
                new IccEncodingRange { Minimum = 1.0, Maximum = 0.0 },
                new IccEncodingRange()
            }
        };
        double[] output = new double[] { 81.0, 82.0 };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => PipelineExecutor.ExecutePipeline(new PipelineStage[] { identity, unsupported }, new double[2], output, new double[2], new double[2], new int[0], new double[0]));
            Assert.Throws<IccTransformException>(() => PipelineExecutor.ExecutePipeline(new PipelineStage[] { identity, discontinuous }, new double[2], output, new double[3], new double[3], new int[0], new double[0]));
            Assert.Throws<IccTransformException>(() => PipelineExecutor.ExecutePipeline(new PipelineStage[] { badMap }, new double[2], output, new double[2], new double[2], new int[0], new double[0]));
            Assert.Throws<IccTransformException>(() => PipelineExecutor.ExecutePipeline(new PipelineStage[] { badClip }, new double[2], output, new double[2], new double[2], new int[0], new double[0]));
            Assert.Throws<IccTransformException>(() => PipelineExecutor.ExecutePipeline(new PipelineStage[] { identity }, new double[] { double.NaN, 0.0 }, output, new double[2], new double[2], new int[0], new double[0]));
            Assert.Throws<IccTransformException>(() => PipelineExecutor.ExecutePipeline(new PipelineStage[] { identity }, new double[2], output, new double[1], new double[2], new int[0], new double[0]));
            Assert.That(output, Is.EqualTo(new double[] { 81.0, 82.0 }));
        });
    }
}
