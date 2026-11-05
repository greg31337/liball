using tftp;

namespace tftp.Tests;

public class BlockNumberTests
{
    [Test]
    public void StartsAtDataBlockOneWithoutPreviousData()
    {
        TftpBlockCounter counter = new TftpBlockCounter();

        Assert.Multiple(() =>
        {
            Assert.That(counter.CurrentBlock, Is.EqualTo(1));
            Assert.That(counter.HasPreviousBlock, Is.False);
            Assert.That(counter.ClassifyDataBlock(1),
                Is.EqualTo(TftpBlockClassification.Expected));
            Assert.That(counter.ClassifyDataBlock(0),
                Is.EqualTo(TftpBlockClassification.Unexpected));
            Assert.That(counter.ClassifyDataBlock(2),
                Is.EqualTo(TftpBlockClassification.Unexpected));
        });
    }

    [Test]
    public void DistinguishesInitialAcknowledgementZeroFromDataAndDuplicates()
    {
        TftpBlockCounter counter = new TftpBlockCounter();

        Assert.Multiple(() =>
        {
            Assert.That(counter.ClassifyAcknowledgementBlock(0),
                Is.EqualTo(TftpBlockClassification.HandshakeAcknowledgement));
            Assert.That(counter.ClassifyAcknowledgementBlock(1),
                Is.EqualTo(TftpBlockClassification.Expected));
            Assert.That(counter.ClassifyAcknowledgementBlock(2),
                Is.EqualTo(TftpBlockClassification.Unexpected));
            Assert.That(counter.ClassifyDataBlock(0),
                Is.EqualTo(TftpBlockClassification.Unexpected));
        });
    }

    [Test]
    public void AdvanceTracksExpectedAndPreviousBlocks()
    {
        TftpBlockCounter counter = new TftpBlockCounter();

        ushort advanced = counter.AdvanceBlock();

        Assert.Multiple(() =>
        {
            Assert.That(advanced, Is.EqualTo(2));
            Assert.That(counter.CurrentBlock, Is.EqualTo(2));
            Assert.That(counter.PreviousBlock, Is.EqualTo(1));
            Assert.That(counter.HasPreviousBlock, Is.True);
            Assert.That(counter.ClassifyDataBlock(2),
                Is.EqualTo(TftpBlockClassification.Expected));
            Assert.That(counter.ClassifyDataBlock(1),
                Is.EqualTo(TftpBlockClassification.Previous));
            Assert.That(counter.ClassifyDataBlock(0),
                Is.EqualTo(TftpBlockClassification.Unexpected));
            Assert.That(counter.ClassifyAcknowledgementBlock(2),
                Is.EqualTo(TftpBlockClassification.Expected));
            Assert.That(counter.ClassifyAcknowledgementBlock(1),
                Is.EqualTo(TftpBlockClassification.Previous));
            Assert.That(counter.ClassifyAcknowledgementBlock(0),
                Is.EqualTo(TftpBlockClassification.Unexpected));
        });
    }

    [Test]
    public void AdvancesModulo65536()
    {
        TftpBlockCounter counter = CreateCounterAtBlock(ushort.MaxValue);

        ushort wrapped = counter.AdvanceBlock();
        ushort afterWrapped = counter.AdvanceBlock();

        Assert.Multiple(() =>
        {
            Assert.That(wrapped, Is.Zero);
            Assert.That(afterWrapped, Is.EqualTo(1));
            Assert.That(counter.CurrentBlock, Is.EqualTo(1));
            Assert.That(counter.PreviousBlock, Is.Zero);
        });
    }

    [Test]
    public void ClassifiesExpectedAndPreviousBlocksAtRollover()
    {
        TftpBlockCounter atMaximum = CreateCounterAtBlock(ushort.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(atMaximum.ClassifyDataBlock(ushort.MaxValue),
                Is.EqualTo(TftpBlockClassification.Expected));
            Assert.That(atMaximum.ClassifyDataBlock(ushort.MaxValue - 1),
                Is.EqualTo(TftpBlockClassification.Previous));
            Assert.That(atMaximum.ClassifyDataBlock(0),
                Is.EqualTo(TftpBlockClassification.Unexpected));
        });

        atMaximum.AdvanceBlock();

        Assert.Multiple(() =>
        {
            Assert.That(atMaximum.ClassifyDataBlock(0),
                Is.EqualTo(TftpBlockClassification.Expected));
            Assert.That(atMaximum.ClassifyDataBlock(ushort.MaxValue),
                Is.EqualTo(TftpBlockClassification.Previous));
            Assert.That(atMaximum.ClassifyDataBlock(1),
                Is.EqualTo(TftpBlockClassification.Unexpected));
            Assert.That(atMaximum.ClassifyAcknowledgementBlock(0),
                Is.EqualTo(TftpBlockClassification.Expected));
            Assert.That(atMaximum.ClassifyAcknowledgementBlock(ushort.MaxValue),
                Is.EqualTo(TftpBlockClassification.Previous));
        });
    }

    [Test]
    public void WrappedAcknowledgementZeroBecomesPreviousInsteadOfHandshake()
    {
        TftpBlockCounter counter = CreateCounterAtBlock(ushort.MaxValue);
        counter.AdvanceBlock();
        counter.AdvanceBlock();

        Assert.Multiple(() =>
        {
            Assert.That(counter.CurrentBlock, Is.EqualTo(1));
            Assert.That(counter.PreviousBlock, Is.Zero);
            Assert.That(counter.HasPreviousBlock, Is.True);
            Assert.That(counter.ClassifyAcknowledgementBlock(0),
                Is.EqualTo(TftpBlockClassification.Previous));
            Assert.That(counter.ClassifyAcknowledgementBlock(1),
                Is.EqualTo(TftpBlockClassification.Expected));
        });
    }

    [Test]
    public void FullBlockNumberCycleReturnsToOneWithoutLosingPreviousBlock()
    {
        TftpBlockCounter counter = new TftpBlockCounter();
        int advances = 0;
        while (advances < 65536)
        {
            counter.AdvanceBlock();
            advances = advances + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(counter.CurrentBlock, Is.EqualTo(1));
            Assert.That(counter.PreviousBlock, Is.Zero);
            Assert.That(counter.HasPreviousBlock, Is.True);
            Assert.That(counter.ClassifyDataBlock(1),
                Is.EqualTo(TftpBlockClassification.Expected));
            Assert.That(counter.ClassifyDataBlock(0),
                Is.EqualTo(TftpBlockClassification.Previous));
        });
    }

    private static TftpBlockCounter CreateCounterAtBlock(ushort blockNumber)
    {
        TftpBlockCounter counter = new TftpBlockCounter();
        while (counter.CurrentBlock != blockNumber)
        {
            counter.AdvanceBlock();
        }

        return counter;
    }
}
