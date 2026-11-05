namespace avif.Tests;

public class Av1EncoderBlockDecisionTests
{
    [Test]
    public void SegmentationBuildsDeterministicQuantizerFilterAndSkipProfiles()
    {
        Av1EncoderBlockDecisionRequest request = CreateRequest(1, 3);
        request.EnableSegmentation = true;
        request.BaseLoopFilterLevels = new int[] { 20, 20, 10, 10 };
        Av1EncoderBlockAnalysis first = Block(0, 0, 0, 80, 10, 5);
        first.TargetLoopFilterLevels = new int[] { 15, 20, 10, 10 };
        Av1EncoderBlockAnalysis skipped = Block(0, 1, 0, 80, 100, 20);
        skipped.TargetLoopFilterLevels = new int[] { 15, 20, 10, 10 };
        skipped.AllowSkip = true;
        skipped.SkipDistortion = 3;
        skipped.SkipRate = 1;
        Av1EncoderBlockAnalysis third = Block(0, 2, 0, 120, 12, 5);
        third.TargetLoopFilterLevels = new int[] { 25, 20, 10, 10 };
        request.Blocks = new Av1EncoderBlockAnalysis[]
        {
            first,
            skipped,
            third
        };

        Av1EncoderBlockDecisionResult result =
            Av1EncoderBlockDecisionEngine.Decide(request);
        Av1BlockModeSyntax skippedSyntax = result.Blocks[1].CreateModeSyntax();

        int quantizer = (int)Av1SegmentFeature.AlternateQuantizer;
        int verticalFilter =
            (int)Av1SegmentFeature.AlternateLoopFilterYVertical;
        int skip = (int)Av1SegmentFeature.Skip;
        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled, Is.True);
            Assert.That(result.Segmentation.UpdateMap, Is.True);
            Assert.That(result.Segmentation.UpdateData, Is.True);
            Assert.That(result.Segmentation.TemporalUpdate, Is.False);
            Assert.That(result.Segmentation.LastActiveSegmentId, Is.EqualTo(2));
            Assert.That(result.Segmentation.SegmentIdPreSkip, Is.True);
            Assert.That(GetSegmentIds(result.Blocks), Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That((result.Segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[0][quantizer], Is.EqualTo(-20));
            Assert.That(result.Segmentation.FeatureData[0][verticalFilter], Is.EqualTo(-5));
            Assert.That((result.Segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[1][skip], Is.True);
            Assert.That(result.Segmentation.FeatureData[2][quantizer], Is.EqualTo(20));
            Assert.That(result.Blocks[1].Skip, Is.True);
            Assert.That(result.Blocks[1].SegmentForcesSkip, Is.True);
            Assert.That(result.Blocks[1].SkipSignaled, Is.False);
            Assert.That(skippedSyntax.SegmentId, Is.EqualTo(1));
            Assert.That(skippedSyntax.Skip, Is.False);
            Assert.That((result.Blocks[2].EntropyContexts ?? throw new InvalidOperationException("Invalid internal state.")).SkipContext, Is.EqualTo(1));
        });
    }

    [Test]
    public void DeltaQuantizerAndLoopFilterTrackSuperblockStateAtConfiguredResolution()
    {
        Av1EncoderBlockDecisionRequest request = CreateRequest(1, 2);
        request.BaseLoopFilterLevels = new int[] { 20, 20, 20, 20 };
        request.EnableDeltaQuantizer = true;
        request.DeltaQuantizerResolution = 2;
        request.EnableDeltaLoopFilter = true;
        request.DeltaLoopFilterResolution = 1;
        request.DeltaLoopFilterMulti = true;
        Av1EncoderBlockAnalysis first = Block(0, 0, 0, 84, 10, 2);
        first.TargetLoopFilterLevels = new int[] { 16, 18, 20, 22 };
        Av1EncoderBlockAnalysis second = Block(0, 1, 1, 116, 10, 2);
        second.TargetLoopFilterLevels = new int[] { 24, 22, 18, 20 };
        request.Blocks = new Av1EncoderBlockAnalysis[] { first, second };

        Av1EncoderBlockDecisionResult result =
            Av1EncoderBlockDecisionEngine.Decide(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaQPresent, Is.True);
            Assert.That(result.Quantization.DeltaQResolution, Is.EqualTo(2));
            Assert.That(result.Quantization.DeltaLoopFilterPresent, Is.True);
            Assert.That(result.Quantization.DeltaLoopFilterResolution, Is.EqualTo(1));
            Assert.That(result.Quantization.DeltaLoopFilterMulti, Is.True);
            Assert.That(result.Blocks[0].DeltaQuantizer, Is.EqualTo(-16));
            Assert.That(result.Blocks[0].EffectiveQIndex, Is.EqualTo(84));
            Assert.That(result.Blocks[0].DeltaLoopFilters,
                Is.EqualTo(new int[] { -4, -2, 0, 2 }));
            Assert.That(result.Blocks[0].EffectiveLoopFilterLevels,
                Is.EqualTo(new int[] { 16, 18, 20, 22 }));
            Assert.That(result.Blocks[1].DeltaQuantizer, Is.EqualTo(32));
            Assert.That(result.Blocks[1].EffectiveQIndex, Is.EqualTo(116));
            Assert.That(result.Blocks[1].DeltaLoopFilters,
                Is.EqualTo(new int[] { 8, 4, -2, -2 }));
            Assert.That(result.Blocks[1].EffectiveLoopFilterLevels,
                Is.EqualTo(new int[] { 24, 22, 18, 20 }));
        });
    }

    [Test]
    public void FrameMatrixAndPerBlockSkipUseAggregateRateDistortionCosts()
    {
        Av1EncoderBlockDecisionRequest request = CreateRequest(1, 2);
        request.EnableQuantizerMatrix = true;
        request.RateDistortionLambdaQ8 = 256;
        Av1EncoderBlockAnalysis first = Block(0, 0, 0, 100, 100, 20);
        SetMatrixCosts(first, 3, 10, 5);
        Av1EncoderBlockAnalysis second = Block(0, 1, 0, 100, 100, 20);
        SetMatrixCosts(second, 3, 10, 5);
        second.AllowSkip = true;
        second.SkipDistortion = 5;
        second.SkipRate = 1;
        request.Blocks = new Av1EncoderBlockAnalysis[] { first, second };

        Av1EncoderBlockDecisionResult result =
            Av1EncoderBlockDecisionEngine.Decide(request);
        int[] residual = new int[]
        {
            4, -2, 1, 0,
            -1, 3, 0, 2,
            2, 0, -3, 1,
            0, 1, -1, 4
        };
        Av1TransformBlockEncodeRequest transformRequest =
            result.Blocks[0].CreateTransformRequest(
                Av1TransformSize.Transform4x4,
                Av1TransformType.DctDct,
                0,
                8,
                0,
                0,
                residual);
        Av1TransformBlockEncodeResult transformed =
            Av1TransformBlockEncoder.Encode(transformRequest);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).UsesQuantizerMatrix, Is.True);
            Assert.That(result.Quantization.MatrixLevelY, Is.EqualTo(3));
            Assert.That(result.Quantization.MatrixLevelU, Is.EqualTo(3));
            Assert.That(result.Quantization.MatrixLevelV, Is.EqualTo(3));
            Assert.That(result.Blocks[0].Skip, Is.False);
            Assert.That(result.Blocks[0].Distortion, Is.EqualTo(10));
            Assert.That(result.Blocks[0].EstimatedRate, Is.EqualTo(5));
            Assert.That(result.Blocks[0].RateDistortionScore, Is.EqualTo(15));
            Assert.That(result.Blocks[1].Skip, Is.True);
            Assert.That(result.Blocks[1].RateDistortionScore, Is.EqualTo(6));
            Assert.That(transformRequest.QIndex, Is.EqualTo(100));
            Assert.That(transformRequest.UseQuantizerMatrix, Is.True);
            Assert.That(transformRequest.MatrixLevel, Is.EqualTo(3));
            Assert.That(transformed.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void NeighborEntropyContextsFollowCommittedBlockState()
    {
        Av1EncoderBlockDecisionRequest request = CreateRequest(2, 2);
        Av1EncoderBlockAnalysis topLeft = Block(0, 0, 0, 100, 10, 1);
        topLeft.CoefficientLevel = 2;
        topLeft.DcSign = 1;
        Av1EncoderBlockAnalysis topRight = SkippedBlock(0, 1);
        Av1EncoderBlockAnalysis bottomLeft = SkippedBlock(1, 0);
        Av1EncoderBlockAnalysis bottomRight = Block(1, 1, 0, 100, 10, 1);
        bottomRight.CoefficientLevel = 3;
        bottomRight.DcSign = -1;
        request.Blocks = new Av1EncoderBlockAnalysis[]
        {
            topLeft,
            topRight,
            bottomLeft,
            bottomRight
        };

        Av1EncoderBlockDecisionResult result =
            Av1EncoderBlockDecisionEngine.Decide(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Blocks[0].EntropyContexts ?? throw new InvalidOperationException("Invalid internal state.")).SkipContext, Is.Zero);
            Assert.That((result.Blocks[1].EntropyContexts ?? throw new InvalidOperationException("Invalid internal state.")).TransformBlockSkipContext,
                Is.EqualTo(2));
            Assert.That((result.Blocks[1].EntropyContexts ?? throw new InvalidOperationException("Invalid internal state.")).DcSignContext, Is.EqualTo(2));
            Assert.That((result.Blocks[2].EntropyContexts ?? throw new InvalidOperationException("Invalid internal state.")).TransformBlockSkipContext,
                Is.EqualTo(2));
            Assert.That((result.Blocks[3].EntropyContexts ?? throw new InvalidOperationException("Invalid internal state.")).SkipContext, Is.EqualTo(2));
            Assert.That((result.Blocks[3].EntropyContexts ?? throw new InvalidOperationException("Invalid internal state.")).TransformBlockSkipContext,
                Is.Zero);
            Assert.That((result.Blocks[3].EntropyContexts ?? throw new InvalidOperationException("Invalid internal state.")).DcSignContext, Is.Zero);
            Assert.That((result.Blocks[3].EntropyContexts ?? throw new InvalidOperationException("Invalid internal state.")).SegmentIdContext,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void OverlappingBlocksAndIllegalDeltaConfigurationAreRejected()
    {
        Av1EncoderBlockDecisionRequest overlap = CreateRequest(1, 2);
        Av1EncoderBlockAnalysis wide = Block(0, 0, 0, 100, 10, 1);
        wide.BlockSize = Av1BlockSize.Block8x4;
        overlap.Blocks = new Av1EncoderBlockAnalysis[]
        {
            wide,
            Block(0, 1, 0, 100, 10, 1)
        };
        Av1EncoderBlockDecisionRequest invalidDeltas = CreateRequest(1, 1);
        invalidDeltas.EnableDeltaLoopFilter = true;
        invalidDeltas.Blocks = new Av1EncoderBlockAnalysis[]
        {
            Block(0, 0, 0, 100, 10, 1)
        };

        Av1EncoderBlockDecisionResult overlapResult =
            Av1EncoderBlockDecisionEngine.Decide(overlap);
        Av1EncoderBlockDecisionResult deltaResult =
            Av1EncoderBlockDecisionEngine.Decide(invalidDeltas);

        Assert.Multiple(() =>
        {
            Assert.That(overlapResult.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(overlapResult.Blocks, Is.Empty);
            Assert.That(deltaResult.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(deltaResult.Blocks, Is.Empty);
        });
    }

    static Av1EncoderBlockDecisionRequest CreateRequest(int rows, int columns)
    {
        Av1EncoderBlockDecisionRequest request =
            new Av1EncoderBlockDecisionRequest();
        request.FrameMiRows = rows;
        request.FrameMiColumns = columns;
        request.BaseQIndex = 100;
        return request;
    }

    static Av1EncoderBlockAnalysis Block(
        int row,
        int column,
        int superblock,
        int targetQIndex,
        long distortion,
        int rate)
    {
        Av1EncoderBlockAnalysis block = new Av1EncoderBlockAnalysis();
        block.MiRow = row;
        block.MiColumn = column;
        block.SuperblockIndex = superblock;
        block.BlockSize = Av1BlockSize.Block4x4;
        block.TargetQIndex = targetQIndex;
        block.CodedDistortion = distortion;
        block.CodedRate = rate;
        block.SkipDistortion = distortion;
        block.SkipRate = rate;
        return block;
    }

    static Av1EncoderBlockAnalysis SkippedBlock(int row, int column)
    {
        Av1EncoderBlockAnalysis block = Block(row, column, 0, 100, 100, 10);
        block.AllowSkip = true;
        block.SkipDistortion = 1;
        block.SkipRate = 1;
        return block;
    }

    static void SetMatrixCosts(
        Av1EncoderBlockAnalysis block,
        int preferredLevel,
        long preferredDistortion,
        int preferredRate)
    {
        block.MatrixDistortions = new long[Av1SyntaxConstants.NumQuantizerMatrixLevels];
        block.MatrixRates = new int[Av1SyntaxConstants.NumQuantizerMatrixLevels];
        int level = 0;
        while (level < Av1SyntaxConstants.NumQuantizerMatrixLevels)
        {
            block.MatrixDistortions[level] = 100;
            block.MatrixRates[level] = 20;
            level++;
        }
        block.MatrixDistortions[preferredLevel] = preferredDistortion;
        block.MatrixRates[preferredLevel] = preferredRate;
    }

    static int[] GetSegmentIds(Av1EncoderBlockDecision[] blocks)
    {
        int[] segmentIds = new int[blocks.Length];
        int index = 0;
        while (index < blocks.Length)
        {
            segmentIds[index] = blocks[index].SegmentId;
            index++;
        }
        return segmentIds;
    }
}
