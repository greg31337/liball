using ttf;

namespace ttf.Tests;

internal class SfntGposAttachmentTests
{
    [Test]
    public void ResolvesForwardMarkChainsAfterAllPositioning()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 2, 3 });
        buffer.SetXAdvance(0, 500);
        buffer.SetXAdvance(1, 20);
        buffer.AttachMark(1, 0, 100, 200);
        buffer.AttachMark(2, 1, 25, 30);
        buffer.ApplyAdjustment(
            0,
            new SfntGposAdjustment(10, 5, 0, 0));

        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetXPlacement(0), Is.EqualTo(10));
            Assert.That(buffer.GetYPlacement(0), Is.EqualTo(5));
            Assert.That(buffer.GetXPlacement(1), Is.EqualTo(-390));
            Assert.That(buffer.GetYPlacement(1), Is.EqualTo(205));
            Assert.That(buffer.GetXPlacement(2), Is.EqualTo(-385));
            Assert.That(buffer.GetYPlacement(2), Is.EqualTo(235));
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(500));
            Assert.That(buffer.GetXAdvance(1), Is.EqualTo(20));
        });
    }

    [Test]
    public void ResolvesBackwardMarkChainsAcrossInterveningAdvances()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 2, 3 });
        buffer.SetXAdvance(0, 20);
        buffer.SetXAdvance(1, 500);
        buffer.AttachMark(1, 2, 100, 200);
        buffer.AttachMark(0, 1, 25, 30);
        buffer.ApplyAdjustment(
            2,
            new SfntGposAdjustment(10, 5, 0, 0));

        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Backward);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetXPlacement(1), Is.EqualTo(610));
            Assert.That(buffer.GetYPlacement(1), Is.EqualTo(205));
            Assert.That(buffer.GetXPlacement(0), Is.EqualTo(655));
            Assert.That(buffer.GetYPlacement(0), Is.EqualTo(235));
            Assert.That(buffer.GetXAdvance(0), Is.EqualTo(20));
            Assert.That(buffer.GetXAdvance(1), Is.EqualTo(500));
        });
    }

    [Test]
    public void PropagatesCursiveOffsetsBeforeResolvingAttachedMarks()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 2, 3, 4 });
        buffer.ApplyAdjustment(
            0,
            new SfntGposAdjustment(0, 5, 0, 0));
        buffer.AttachCursive(1, 0, 30);
        buffer.AttachCursive(2, 1, 40);
        buffer.AttachMark(3, 2, 15, 20);

        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetYPlacement(1), Is.EqualTo(35));
            Assert.That(buffer.GetYPlacement(2), Is.EqualTo(75));
            Assert.That(buffer.GetXPlacement(3), Is.EqualTo(15));
            Assert.That(buffer.GetYPlacement(3), Is.EqualTo(95));
        });
    }

    [Test]
    public void ReversesLongCursiveChainsWithoutRecursiveStackGrowth()
    {
        const int glyphCount = 20000;
        int[] glyphIds = new int[glyphCount];
        int index = 0;

        while (index < glyphIds.Length)
        {
            glyphIds[index] = index;
            index += 1;
        }

        SfntGposPositionBuffer buffer =
            SfntGposPositionBuffer.Create(glyphIds);
        index = 1;

        while (index < glyphCount)
        {
            buffer.AttachCursive(index, index - 1, 1);
            index += 1;
        }

        buffer.AttachCursive(glyphCount - 1, 0, 2);
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetAttachmentParent(0), Is.EqualTo(-1));
            Assert.That(buffer.GetAttachmentParent(glyphCount - 1),
                Is.EqualTo(0));
            Assert.That(buffer.GetAttachmentParent(glyphCount - 2),
                Is.EqualTo(glyphCount - 1));
            Assert.That(buffer.GetAttachmentParent(1), Is.EqualTo(2));
        });
    }

    [Test]
    public void RejectsAttachmentCyclesWithoutChangingPositions()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 2 });
        buffer.AttachMark(1, 0, 100, 200);
        buffer.AttachMark(0, 1, 25, 30);

        Assert.Throws<FontFormatException>(() =>
            buffer.ResolveAttachments(
                SfntLayoutTraversalDirection.Forward));
        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetXPlacement(0), Is.EqualTo(25));
            Assert.That(buffer.GetYPlacement(0), Is.EqualTo(30));
            Assert.That(buffer.GetXPlacement(1), Is.EqualTo(100));
            Assert.That(buffer.GetYPlacement(1), Is.EqualTo(200));
        });
    }

    [Test]
    public void ValidatesResolutionDirectionAndFreezesResolvedPositions()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1 });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            buffer.ResolveAttachments(
                (SfntLayoutTraversalDirection)10));
        buffer.ResolveAttachments(SfntLayoutTraversalDirection.Forward);
        Assert.Throws<InvalidOperationException>(() =>
            buffer.ApplyAdjustment(
                0,
                new SfntGposAdjustment(1, 2, 3, 4)));
    }
}
