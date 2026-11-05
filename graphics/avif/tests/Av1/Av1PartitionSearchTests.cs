namespace avif.Tests;

public class Av1PartitionSearchTests
{
    [Test]
    public void ExhaustiveSearchSelectsLowestCostLegalAsymmetricPartition()
    {
        Av1PartitionSearchRequest request = CreateRequest(
            Av1BlockSize.Block16x16,
            Av1PartitionSearchEffort.Exhaustive,
            4,
            4);
        request.RateDistortionLambdaQ8 = 256;
        request.PartitionRateCosts[(int)Av1PartitionType.HorizontalA] = 3;
        request.BlockCandidates = new Av1PartitionBlockCandidate[]
        {
            Block(0, 0, Av1BlockSize.Block16x16, 100, 0, 0),
            Block(0, 0, Av1BlockSize.Block16x8, 80, 1, 1),
            Block(2, 0, Av1BlockSize.Block16x8, 1, 1, 2),
            Block(0, 0, Av1BlockSize.Block8x16, 80, 1, 3),
            Block(0, 2, Av1BlockSize.Block8x16, 80, 1, 4),
            Block(0, 0, Av1BlockSize.Block8x8, 5, 2, 5),
            Block(0, 2, Av1BlockSize.Block8x8, 5, 2, 6),
            Block(2, 0, Av1BlockSize.Block8x8, 30, 2, 7),
            Block(2, 2, Av1BlockSize.Block8x8, 30, 2, 8)
        };

        Av1PartitionSearchResult result = Av1PartitionSearcher.Search(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.BestDecision ?? throw new InvalidOperationException("Invalid internal state.")).Partition,
                Is.EqualTo(Av1PartitionType.HorizontalA));
            Assert.That(result.BestDecision.Distortion, Is.EqualTo(11));
            Assert.That(result.BestDecision.EstimatedRate, Is.EqualTo(8));
            Assert.That(result.BestDecision.RateDistortionScore, Is.EqualTo(19));
            Assert.That(result.BestDecision.Blocks, Has.Length.EqualTo(3));
            Assert.That(GetBlockGeometry(result.BestDecision.Blocks),
                Is.EqualTo(new int[]
                {
                    0, 0, (int)Av1BlockSize.Block8x8,
                    0, 2, (int)Av1BlockSize.Block8x8,
                    2, 0, (int)Av1BlockSize.Block16x8
                }));
        });
    }

    [Test]
    public void EffortLevelsExpandThePartitionCandidateSetPredictably()
    {
        Av1PartitionBlockCandidate[] blocks = new Av1PartitionBlockCandidate[]
        {
            Block(0, 0, Av1BlockSize.Block16x16, 40, 0, 0),
            Block(0, 0, Av1BlockSize.Block16x8, 5, 0, 1),
            Block(2, 0, Av1BlockSize.Block16x8, 5, 0, 2),
            Block(0, 0, Av1BlockSize.Block8x8, 10, 0, 3),
            Block(0, 2, Av1BlockSize.Block8x8, 10, 0, 4),
            Block(2, 0, Av1BlockSize.Block8x8, 10, 0, 5),
            Block(2, 2, Av1BlockSize.Block8x8, 10, 0, 6),
            Block(0, 0, Av1BlockSize.Block16x4, 1, 0, 7),
            Block(1, 0, Av1BlockSize.Block16x4, 1, 0, 8),
            Block(2, 0, Av1BlockSize.Block16x4, 1, 0, 9),
            Block(3, 0, Av1BlockSize.Block16x4, 1, 0, 10)
        };

        Av1PartitionSearchResult fast = SearchWithEffort(
            Av1PartitionSearchEffort.Fast,
            blocks);
        Av1PartitionSearchResult balanced = SearchWithEffort(
            Av1PartitionSearchEffort.Balanced,
            blocks);
        Av1PartitionSearchResult thorough = SearchWithEffort(
            Av1PartitionSearchEffort.Thorough,
            blocks);
        Av1PartitionSearchResult exhaustive = SearchWithEffort(
            Av1PartitionSearchEffort.Exhaustive,
            blocks);

        Assert.Multiple(() =>
        {
            Assert.That((fast.BestDecision ?? throw new InvalidOperationException("Invalid internal state.")).Partition,
                Is.EqualTo(Av1PartitionType.None));
            Assert.That((balanced.BestDecision ?? throw new InvalidOperationException("Invalid internal state.")).Partition,
                Is.EqualTo(Av1PartitionType.Horizontal));
            Assert.That((thorough.BestDecision ?? throw new InvalidOperationException("Invalid internal state.")).Partition,
                Is.EqualTo(Av1PartitionType.Horizontal));
            Assert.That((exhaustive.BestDecision ?? throw new InvalidOperationException("Invalid internal state.")).Partition,
                Is.EqualTo(Av1PartitionType.Horizontal4));
        });
    }

    [Test]
    public void PruningPreservesRateDistortionResultAndStableTieBreaks()
    {
        object selected = new object();
        Av1PartitionBlockCandidate[] blocks = new Av1PartitionBlockCandidate[]
        {
            BlockWithValue(0, 0, Av1BlockSize.Block16x16, 11, 9, 0, new object()),
            BlockWithValue(0, 0, Av1BlockSize.Block16x16, 10, 10, 8, selected),
            Block(0, 0, Av1BlockSize.Block16x8, 100, 0, 0),
            Block(2, 0, Av1BlockSize.Block16x8, 100, 0, 1),
            Block(0, 0, Av1BlockSize.Block8x8, 100, 0, 2),
            Block(0, 2, Av1BlockSize.Block8x8, 100, 0, 3),
            Block(2, 0, Av1BlockSize.Block8x8, 100, 0, 4),
            Block(2, 2, Av1BlockSize.Block8x8, 100, 0, 5)
        };
        Av1PartitionSearchRequest prunedRequest = CreateRequest(
            Av1BlockSize.Block16x16,
            Av1PartitionSearchEffort.Balanced,
            4,
            4);
        prunedRequest.RateDistortionLambdaQ8 = 256;
        prunedRequest.BlockCandidates = blocks;
        Av1PartitionSearchRequest completeRequest = CreateRequest(
            Av1BlockSize.Block16x16,
            Av1PartitionSearchEffort.Balanced,
            4,
            4);
        completeRequest.RateDistortionLambdaQ8 = 256;
        completeRequest.BlockCandidates = blocks;
        completeRequest.EnablePruning = false;

        Av1PartitionSearchResult pruned = Av1PartitionSearcher.Search(prunedRequest);
        Av1PartitionSearchResult complete = Av1PartitionSearcher.Search(completeRequest);

        Assert.Multiple(() =>
        {
            Assert.That(pruned.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((pruned.BestDecision ?? throw new InvalidOperationException("Invalid internal state.")).Partition,
                Is.EqualTo(Av1PartitionType.None));
            Assert.That(pruned.BestDecision.Blocks[0].Value, Is.SameAs(selected));
            Assert.That(pruned.BestDecision.RateDistortionScore, Is.EqualTo(20));
            Assert.That(pruned.PrunedPartitionCount, Is.GreaterThan(0));
            Assert.That(complete.PrunedPartitionCount, Is.Zero);
            Assert.That((complete.BestDecision ?? throw new InvalidOperationException("Invalid internal state.")).Partition,
                Is.EqualTo(pruned.BestDecision.Partition));
            Assert.That(complete.BestDecision.RateDistortionScore,
                Is.EqualTo(pruned.BestDecision.RateDistortionScore));
        });
    }

    [Test]
    public void SplitSearchRecursesAndAccumulatesChildCosts()
    {
        Av1PartitionSearchRequest request = CreateRequest(
            Av1BlockSize.Block16x16,
            Av1PartitionSearchEffort.Fast,
            4,
            4);
        request.PartitionRateCosts[(int)Av1PartitionType.Split] = 2;
        request.BlockCandidates = new Av1PartitionBlockCandidate[]
        {
            Block(0, 0, Av1BlockSize.Block16x16, 100, 0, 0),
            Block(0, 0, Av1BlockSize.Block8x8, 2, 1, 1),
            Block(0, 2, Av1BlockSize.Block8x8, 3, 1, 2),
            Block(2, 0, Av1BlockSize.Block8x8, 4, 1, 3),
            Block(2, 2, Av1BlockSize.Block8x8, 5, 1, 4)
        };

        Av1PartitionSearchResult result = Av1PartitionSearcher.Search(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.BestDecision ?? throw new InvalidOperationException("Invalid internal state.")).Partition,
                Is.EqualTo(Av1PartitionType.Split));
            Assert.That(result.BestDecision.Children, Has.Length.EqualTo(4));
            Assert.That(GetChildPartitions(result.BestDecision.Children),
                Is.EqualTo(new Av1PartitionType[]
                {
                    Av1PartitionType.None,
                    Av1PartitionType.None,
                    Av1PartitionType.None,
                    Av1PartitionType.None
                }));
            Assert.That(result.BestDecision.Distortion, Is.EqualTo(14));
            Assert.That(result.BestDecision.EstimatedRate, Is.EqualTo(6));
        });
    }

    [Test]
    public void FrameEdgeUsesTheNormativeForcedPartition()
    {
        Av1PartitionSearchRequest request = CreateRequest(
            Av1BlockSize.Block64x64,
            Av1PartitionSearchEffort.Fast,
            8,
            16);
        request.BlockCandidates = new Av1PartitionBlockCandidate[]
        {
            Block(0, 0, Av1BlockSize.Block64x32, 7, 2, 0)
        };

        Av1PartitionSearchResult result = Av1PartitionSearcher.Search(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.BestDecision ?? throw new InvalidOperationException("Invalid internal state.")).Partition,
                Is.EqualTo(Av1PartitionType.Horizontal));
            Assert.That(result.BestDecision.Blocks, Has.Length.EqualTo(1));
            Assert.That(result.BestDecision.Blocks[0].BlockSize,
                Is.EqualTo(Av1BlockSize.Block64x32));
            Assert.That(result.EvaluatedPartitionCount, Is.EqualTo(1));
        });
    }

    static Av1PartitionSearchResult SearchWithEffort(
        Av1PartitionSearchEffort effort,
        Av1PartitionBlockCandidate[] blocks)
    {
        Av1PartitionSearchRequest request = CreateRequest(
            Av1BlockSize.Block16x16,
            effort,
            4,
            4);
        request.BlockCandidates = blocks;
        return Av1PartitionSearcher.Search(request);
    }

    static Av1PartitionSearchRequest CreateRequest(
        Av1BlockSize blockSize,
        Av1PartitionSearchEffort effort,
        int rowEnd,
        int columnEnd)
    {
        Av1PartitionSearchRequest request = new Av1PartitionSearchRequest();
        request.MiRow = 0;
        request.MiColumn = 0;
        request.RowEnd = rowEnd;
        request.ColumnEnd = columnEnd;
        request.BlockSize = blockSize;
        request.Effort = effort;
        return request;
    }

    static Av1PartitionBlockCandidate Block(
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        long distortion,
        int rate,
        int order)
    {
        return BlockWithValue(
            miRow,
            miColumn,
            blockSize,
            distortion,
            rate,
            order,
            null);
    }

    static Av1PartitionBlockCandidate BlockWithValue(
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        long distortion,
        int rate,
        int order,
        object? value)
    {
        return new Av1PartitionBlockCandidate
        {
            MiRow = miRow,
            MiColumn = miColumn,
            BlockSize = blockSize,
            Distortion = distortion,
            EstimatedRate = rate,
            Order = order,
            Value = value
        };
    }

    static int[] GetBlockGeometry(Av1PartitionBlockCandidate[] blocks)
    {
        int[] geometry = new int[blocks.Length * 3];
        int index = 0;
        while (index < blocks.Length)
        {
            geometry[index * 3] = blocks[index].MiRow;
            geometry[index * 3 + 1] = blocks[index].MiColumn;
            geometry[index * 3 + 2] = (int)blocks[index].BlockSize;
            index++;
        }
        return geometry;
    }

    static Av1PartitionType[] GetChildPartitions(
        Av1PartitionDecision[] children)
    {
        Av1PartitionType[] partitions = new Av1PartitionType[children.Length];
        int index = 0;
        while (index < children.Length)
        {
            partitions[index] = children[index].Partition;
            index++;
        }
        return partitions;
    }
}
