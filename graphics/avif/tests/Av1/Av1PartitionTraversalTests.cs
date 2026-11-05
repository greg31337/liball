namespace avif.Tests;

public class Av1PartitionTraversalTests
{
    [Test]
    public void EveryLegalTopLevelPartitionCoversOne64x64SuperblockExactly()
    {
        Av1PartitionType[] partitions = new Av1PartitionType[]
        {
            Av1PartitionType.None,
            Av1PartitionType.Horizontal,
            Av1PartitionType.Vertical,
            Av1PartitionType.Split,
            Av1PartitionType.HorizontalA,
            Av1PartitionType.HorizontalB,
            Av1PartitionType.VerticalA,
            Av1PartitionType.VerticalB,
            Av1PartitionType.Horizontal4,
            Av1PartitionType.Vertical4
        };
        int[] expectedBlockCounts = new int[] { 1, 2, 2, 4, 3, 3, 3, 3, 4, 4 };
        int partitionIndex = 0;
        while (partitionIndex < partitions.Length)
        {
            Av1PartitionTraversal traversal = CreateTraversal(64, 64, false);
            Av1PartitionStep root = traversal.TakeNext();
            AvifResultCode submit = traversal.SubmitPartition(partitions[partitionIndex]);
            List<Av1PartitionEvent> blocks = DrainTraversal(traversal);
            int[,] coverage = CreateCoverage(blocks, 16, 16);

            Assert.Multiple(() =>
            {
                Assert.That(root.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That((root.Event ?? throw new InvalidOperationException("Invalid internal state.")).Kind, Is.EqualTo(Av1PartitionEventKind.Partition));
                Assert.That(root.Event.BlockSize, Is.EqualTo(Av1BlockSize.Block64x64));
                Assert.That(root.Event.Forced, Is.False);
                Assert.That(submit, Is.EqualTo(AvifResultCode.Success));
                Assert.That(blocks, Has.Count.EqualTo(expectedBlockCounts[partitionIndex]));
                AssertCoverageIsOne(coverage);
            });

            traversal.Close();
            partitionIndex++;
        }
    }

    [Test]
    public void PartitionLegalityMatchesEverySquareSizeAndRectangularSubsize()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Av1PartitionRules.IsAllowed(
                Av1BlockSize.Block4x4, Av1PartitionType.None), Is.True);
            Assert.That(Av1PartitionRules.IsAllowed(
                Av1BlockSize.Block4x4, Av1PartitionType.Split), Is.False);
            Assert.That(Av1PartitionRules.IsAllowed(
                Av1BlockSize.Block8x8, Av1PartitionType.Horizontal), Is.True);
            Assert.That(Av1PartitionRules.IsAllowed(
                Av1BlockSize.Block8x8, Av1PartitionType.HorizontalA), Is.False);
            Assert.That(Av1PartitionRules.IsAllowed(
                Av1BlockSize.Block16x16, Av1PartitionType.Horizontal4), Is.True);
            Assert.That(Av1PartitionRules.IsAllowed(
                Av1BlockSize.Block64x64, Av1PartitionType.Vertical4), Is.True);
            Assert.That(Av1PartitionRules.IsAllowed(
                Av1BlockSize.Block128x128, Av1PartitionType.HorizontalA), Is.True);
            Assert.That(Av1PartitionRules.IsAllowed(
                Av1BlockSize.Block128x128, Av1PartitionType.Horizontal4), Is.False);
            Assert.That(Av1PartitionRules.IsAllowed(
                Av1BlockSize.Block128x64, Av1PartitionType.None), Is.False);
        });

        int squareIndex = 0;
        Av1BlockSize[] squares = new Av1BlockSize[]
        {
            Av1BlockSize.Block4x4,
            Av1BlockSize.Block8x8,
            Av1BlockSize.Block16x16,
            Av1BlockSize.Block32x32,
            Av1BlockSize.Block64x64,
            Av1BlockSize.Block128x128
        };
        while (squareIndex < squares.Length)
        {
            int partition = 0;
            while (partition <= (int)Av1PartitionType.Vertical4)
            {
                Av1BlockSize subsize = Av1ConversionTables.GetPartitionSubsize(
                    (Av1PartitionType)partition,
                    squares[squareIndex]);
                Assert.That(
                    Av1PartitionRules.IsAllowed(
                        squares[squareIndex],
                        (Av1PartitionType)partition),
                    Is.EqualTo(subsize != Av1BlockSize.Invalid));
                partition++;
            }

            squareIndex++;
        }
    }

    [Test]
    public void EveryLegal128x128PartitionCoversTheSuperblockExactly()
    {
        Av1PartitionType[] partitions = new Av1PartitionType[]
        {
            Av1PartitionType.None,
            Av1PartitionType.Horizontal,
            Av1PartitionType.Vertical,
            Av1PartitionType.Split,
            Av1PartitionType.HorizontalA,
            Av1PartitionType.HorizontalB,
            Av1PartitionType.VerticalA,
            Av1PartitionType.VerticalB
        };
        int[] expectedBlockCounts = new int[] { 1, 2, 2, 4, 3, 3, 3, 3 };
        int index = 0;
        while (index < partitions.Length)
        {
            Av1PartitionTraversal traversal = CreateTraversal(128, 128, true);
            Av1PartitionStep root = traversal.TakeNext();
            AvifResultCode submit = traversal.SubmitPartition(partitions[index]);
            List<Av1PartitionEvent> blocks = DrainTraversal(traversal);
            int[,] coverage = CreateCoverage(blocks, 32, 32);

            Assert.Multiple(() =>
            {
                Assert.That((root.Event ?? throw new InvalidOperationException("Invalid internal state.")).BlockSize,
                    Is.EqualTo(Av1BlockSize.Block128x128));
                Assert.That(submit, Is.EqualTo(AvifResultCode.Success));
                Assert.That(blocks, Has.Count.EqualTo(expectedBlockCounts[index]));
                AssertCoverageIsOne(coverage);
            });

            traversal.Close();
            index++;
        }
    }

    [Test]
    public void SuperblocksTraverseInRasterOrderFor64And128Modes()
    {
        List<Av1PartitionEvent> blocks64 = DrainWithNone(
            CreateTraversal(192, 128, false));
        List<Av1PartitionEvent> blocks128 = DrainWithNone(
            CreateTraversal(256, 128, true));

        Assert.Multiple(() =>
        {
            Assert.That(GetCoordinates(blocks64), Is.EqualTo(new int[]
            {
                0, 0,
                0, 16,
                0, 32,
                16, 0,
                16, 16,
                16, 32
            }));
            Assert.That(GetSuperblockIndexes(blocks64),
                Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5 }));
            Assert.That(AllBlocksAre(blocks64, Av1BlockSize.Block64x64), Is.True);
            Assert.That(GetCoordinates(blocks128), Is.EqualTo(new int[]
            {
                0, 0,
                0, 32
            }));
            Assert.That(GetSuperblockIndexes(blocks128),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(AllBlocksAre(blocks128, Av1BlockSize.Block128x128), Is.True);
        });
    }

    [Test]
    public void FrameEdgesForcePartitionsAndClipVisibleBlockGeometry()
    {
        Av1PartitionTraversal traversal = CreateTraversal(68, 68, false);
        List<Av1PartitionEvent> blocks = new List<Av1PartitionEvent>();
        bool sawHorizontal = false;
        bool sawVertical = false;
        bool sawSplit = false;
        while (true)
        {
            Av1PartitionStep step = traversal.TakeNext();
            Assert.That(step.Code, Is.EqualTo(AvifResultCode.Success));
            if (step.Complete)
            {
                break;
            }

            if ((step.Event ?? throw new InvalidOperationException("Invalid internal state.")).Kind == Av1PartitionEventKind.Partition)
            {
                if (step.Event.Forced)
                {
                    sawHorizontal |= step.Event.Partition == Av1PartitionType.Horizontal;
                    sawVertical |= step.Event.Partition == Av1PartitionType.Vertical;
                    sawSplit |= step.Event.Partition == Av1PartitionType.Split;
                }
                else
                {
                    Assert.That(traversal.SubmitPartition(Av1PartitionType.None),
                        Is.EqualTo(AvifResultCode.Success));
                }
            }
            else
            {
                blocks.Add(step.Event);
            }
        }

        int[,] coverage = CreateCoverage(blocks, 18, 18);
        Assert.Multiple(() =>
        {
            Assert.That(sawHorizontal, Is.True);
            Assert.That(sawVertical, Is.True);
            Assert.That(sawSplit, Is.True);
            AssertCoverageIsOne(coverage);
            Assert.That(HasClippedBlock(blocks), Is.True);
        });

        traversal.Close();
    }

    [Test]
    public void TileTraversalUsesLocalAlignedBoundsWithoutCrossingNeighbors()
    {
        ParsedAv1FrameHeader header = CreateHeader(128, 64);
        (header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns = 2;
        header.Tiles.TileColumnsLog2 = 1;
        header.Tiles.MiColumnStarts = new int[] { 0, 16, 32 };
        Av1PartitionTraversal traversal = new Av1PartitionTraversal(
            CreateSequence(false),
            header,
            1,
            new AvifSecurityLimits());
        List<Av1PartitionEvent> blocks = DrainWithNone(traversal);

        Assert.Multiple(() =>
        {
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks[0].MiRow, Is.Zero);
            Assert.That(blocks[0].MiColumn, Is.EqualTo(16));
            Assert.That(blocks[0].VisibleMiWidth, Is.EqualTo(16));
            Assert.That(blocks[0].VisibleMiHeight, Is.EqualTo(16));
            Assert.That(blocks[0].SuperblockIndex, Is.Zero);
        });

        traversal.Close();
    }

    [Test]
    public void InvalidDecisionsStateAndDimensionsAreRejectedDeterministically()
    {
        Av1PartitionTraversal traversal128 = CreateTraversal(128, 128, true);
        Assert.That(traversal128.SubmitPartition(Av1PartitionType.None),
            Is.EqualTo(AvifResultCode.InvalidState));
        Av1PartitionStep root = traversal128.TakeNext();
        Av1PartitionStep blocked = traversal128.TakeNext();
        AvifResultCode invalid = traversal128.SubmitPartition(
            Av1PartitionType.Horizontal4);

        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxWidth = 32;
        Av1PartitionTraversal limited = new Av1PartitionTraversal(
            CreateSequence(false),
            CreateHeader(64, 64),
            0,
            limits);
        Av1PartitionStep limitedStep = limited.TakeNext();

        ParsedAv1FrameHeader unalignedHeader = CreateHeader(128, 64);
        (unalignedHeader.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns = 2;
        unalignedHeader.Tiles.TileColumnsLog2 = 1;
        unalignedHeader.Tiles.MiColumnStarts = new int[] { 0, 15, 32 };
        Av1PartitionTraversal unaligned = new Av1PartitionTraversal(
            CreateSequence(false),
            unalignedHeader,
            1,
            new AvifSecurityLimits());
        ParsedAv1FrameHeader mismatchedHeader = CreateHeader(64, 64);
        (mismatchedHeader.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiColumns = 15;
        (mismatchedHeader.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).MiColumnStarts = new int[] { 0, 15 };
        Av1PartitionTraversal mismatched = new Av1PartitionTraversal(
            CreateSequence(false),
            mismatchedHeader,
            0,
            new AvifSecurityLimits());

        Assert.Multiple(() =>
        {
            Assert.That((root.Event ?? throw new InvalidOperationException("Invalid internal state.")).BlockSize, Is.EqualTo(Av1BlockSize.Block128x128));
            Assert.That(blocked.Code, Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(invalid, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(traversal128.TakeNext().Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(limitedStep.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(unaligned.TakeNext().Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(mismatched.TakeNext().Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
        });

        traversal128.Close();
        limited.Close();
        unaligned.Close();
        mismatched.Close();
    }

    static Av1PartitionTraversal CreateTraversal(
        int width,
        int height,
        bool use128)
    {
        return new Av1PartitionTraversal(
            CreateSequence(use128),
            CreateHeader(width, height),
            0,
            new AvifSecurityLimits());
    }

    static List<Av1PartitionEvent> DrainWithNone(Av1PartitionTraversal traversal)
    {
        List<Av1PartitionEvent> blocks = new List<Av1PartitionEvent>();
        while (true)
        {
            Av1PartitionStep step = traversal.TakeNext();
            Assert.That(step.Code, Is.EqualTo(AvifResultCode.Success));
            if (step.Complete)
            {
                break;
            }

            if ((step.Event ?? throw new InvalidOperationException("Invalid internal state.")).Kind == Av1PartitionEventKind.Partition)
            {
                if (!step.Event.Forced)
                {
                    Assert.That(traversal.SubmitPartition(Av1PartitionType.None),
                        Is.EqualTo(AvifResultCode.Success));
                }
            }
            else
            {
                blocks.Add(step.Event);
            }
        }

        return blocks;
    }

    static List<Av1PartitionEvent> DrainTraversal(Av1PartitionTraversal traversal)
    {
        List<Av1PartitionEvent> blocks = new List<Av1PartitionEvent>();
        while (true)
        {
            Av1PartitionStep step = traversal.TakeNext();
            Assert.That(step.Code, Is.EqualTo(AvifResultCode.Success));
            if (step.Complete)
            {
                break;
            }

            if ((step.Event ?? throw new InvalidOperationException("Invalid internal state.")).Kind == Av1PartitionEventKind.Partition)
            {
                if (!step.Event.Forced)
                {
                    Assert.That(traversal.SubmitPartition(Av1PartitionType.None),
                        Is.EqualTo(AvifResultCode.Success));
                }
            }
            else
            {
                blocks.Add(step.Event);
            }
        }

        return blocks;
    }

    static int[,] CreateCoverage(
        List<Av1PartitionEvent> blocks,
        int rows,
        int columns)
    {
        int[,] coverage = new int[rows, columns];
        int blockIndex = 0;
        while (blockIndex < blocks.Count)
        {
            Av1PartitionEvent block = blocks[blockIndex];
            int row = block.MiRow;
            while (row < block.MiRow + block.VisibleMiHeight)
            {
                int column = block.MiColumn;
                while (column < block.MiColumn + block.VisibleMiWidth)
                {
                    coverage[row, column]++;
                    column++;
                }

                row++;
            }

            blockIndex++;
        }

        return coverage;
    }

    static void AssertCoverageIsOne(int[,] coverage)
    {
        int row = 0;
        while (row < coverage.GetLength(0))
        {
            int column = 0;
            while (column < coverage.GetLength(1))
            {
                Assert.That(coverage[row, column], Is.EqualTo(1),
                    $"coverage at ({row}, {column})");
                column++;
            }

            row++;
        }
    }

    static int[] GetCoordinates(List<Av1PartitionEvent> blocks)
    {
        int[] coordinates = new int[blocks.Count * 2];
        int index = 0;
        while (index < blocks.Count)
        {
            coordinates[index * 2] = blocks[index].MiRow;
            coordinates[index * 2 + 1] = blocks[index].MiColumn;
            index++;
        }

        return coordinates;
    }

    static int[] GetSuperblockIndexes(List<Av1PartitionEvent> blocks)
    {
        int[] indexes = new int[blocks.Count];
        int index = 0;
        while (index < blocks.Count)
        {
            indexes[index] = blocks[index].SuperblockIndex;
            index++;
        }

        return indexes;
    }

    static bool AllBlocksAre(List<Av1PartitionEvent> blocks, Av1BlockSize size)
    {
        int index = 0;
        while (index < blocks.Count)
        {
            if (blocks[index].BlockSize != size)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool HasClippedBlock(List<Av1PartitionEvent> blocks)
    {
        int index = 0;
        while (index < blocks.Count)
        {
            Av1PartitionEvent block = blocks[index];
            if (block.VisibleMiWidth
                    < Av1ConversionTables.GetBlockMiWidth(block.BlockSize)
                || block.VisibleMiHeight
                    < Av1ConversionTables.GetBlockMiHeight(block.BlockSize))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static ParsedAv1FrameHeader CreateHeader(int width, int height)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.Size = new Av1FrameSize();
        header.Size.FrameWidth = width;
        header.Size.FrameHeight = height;
        header.Size.UpscaledWidth = width;
        header.Size.RenderWidth = width;
        header.Size.RenderHeight = height;
        header.Size.MiColumns = 2 * ((width + 7) >> 3);
        header.Size.MiRows = 2 * ((height + 7) >> 3);
        header.Tiles = new Av1TileInfo();
        header.Tiles.TileColumns = 1;
        header.Tiles.TileRows = 1;
        header.Tiles.MiColumnStarts = new int[] { 0, header.Size.MiColumns };
        header.Tiles.MiRowStarts = new int[] { 0, header.Size.MiRows };
        return header;
    }

    static ParsedAv1SequenceHeader CreateSequence(bool use128)
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
        features.Use128x128Superblock = use128;
        return new ParsedAv1SequenceHeader(
            0,
            false,
            false,
            null,
            null,
            false,
            new Av1OperatingPoint[] { new Av1OperatingPoint(0, 0, 0) },
            512,
            512,
            features,
            false,
            false,
            true,
            true,
            true,
            0,
            2,
            2,
            2,
            true,
            false,
            false,
            false,
            System.Array.Empty<byte>());
    }
}
