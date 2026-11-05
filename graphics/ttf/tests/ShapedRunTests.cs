using ttf;

namespace ttf.Tests;

internal class ShapedRunTests
{
    [Test]
    public void FreezesMergedUnicodeAttributionFlagsAndPositions()
    {
        RuneBuffer source = Decode("fi\u0301x");
        ShapingBuffer buffer = new ShapingBuffer(
            source,
            ShapingDirection.LeftToRight);
        buffer.SetGlyphId(0, 100);
        buffer.SetGlyphId(1, 101);
        buffer.SetGlyphId(2, 102);
        buffer.SetGlyphId(3, 103);
        buffer.MergeClusters(0, 3);
        buffer.SetLigature(0, 7, 2, 0);
        buffer.SetUnsafeToBreak(2, true);
        buffer.SetDefaultIgnorable(2, true);
        buffer.SetAttachment(2, 0, ShapingAttachmentType.Mark);
        buffer.Remove(1);
        SfntGposPositionBuffer positions = SfntGposPositionBuffer.Create(
            new int[] { 100, 102, 103 });
        positions.ApplyAdjustment(
            0,
            new SfntGposAdjustment(10, 20, 500, 60));
        positions.ApplyAdjustment(
            1,
            new SfntGposAdjustment(30, 40, 0, 0));

        ShapedRun run = ShapedRunBuilder.Build(
            buffer,
            source,
            positions);
        buffer.SetGlyphId(0, 999);
        positions.SetXAdvance(0, 900);

        ShapedCluster firstCluster = run.GetCluster(0);
        ShapedGlyph ligature = run.GetGlyph(0);
        ShapedGlyph mark = run.GetGlyph(1);
        Assert.Multiple(() =>
        {
            Assert.That(run.GetSourceLength(), Is.EqualTo(4));
            Assert.That(run.GetDirection(),
                Is.EqualTo(ShapingDirection.LeftToRight));
            Assert.That(run.GetGlyphCount(), Is.EqualTo(3));
            Assert.That(run.GetClusterCount(), Is.EqualTo(2));
            Assert.That(firstCluster.GetUtf16Start(), Is.Zero);
            Assert.That(firstCluster.GetUtf16Length(), Is.EqualTo(3));
            Assert.That(firstCluster.GetUnicodeScalarCount(), Is.EqualTo(3));
            Assert.That(firstCluster.GetUnicodeScalar(0), Is.EqualTo('f'));
            Assert.That(firstCluster.GetUnicodeScalar(1), Is.EqualTo('i'));
            Assert.That(firstCluster.GetUnicodeScalar(2), Is.EqualTo(0x301));
            Assert.That(firstCluster.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(firstCluster.GetGlyphVisualIndex(0), Is.Zero);
            Assert.That(firstCluster.GetGlyphVisualIndex(1), Is.EqualTo(1));
            Assert.That(firstCluster.IsUnsafeToBreak(), Is.True);
            Assert.That(ligature.GetGlyphId(), Is.EqualTo(100));
            Assert.That(ligature.GetUnicodeScalarCount(), Is.EqualTo(3));
            Assert.That(ligature.GetUnicodeScalar(1), Is.EqualTo('i'));
            Assert.That(ligature.GetLigatureComponentCount(), Is.EqualTo(2));
            Assert.That(ligature.GetLigatureComponentIndex(), Is.Zero);
            Assert.That(ligature.IsLigature(), Is.True);
            Assert.That(ligature.GetXAdvance(), Is.EqualTo(500));
            Assert.That(ligature.GetYAdvance(), Is.EqualTo(60));
            Assert.That(ligature.GetXOffset(), Is.EqualTo(10));
            Assert.That(ligature.GetYOffset(), Is.EqualTo(20));
            Assert.That(mark.GetGlyphId(), Is.EqualTo(102));
            Assert.That(mark.IsUnsafeToBreak(), Is.True);
            Assert.That(mark.IsDefaultIgnorable(), Is.True);
            Assert.That((mark.GetFlags() & ShapedGlyphFlags.MarkAttachment),
                Is.EqualTo(ShapedGlyphFlags.MarkAttachment));
            Assert.That(mark.GetXOffset(), Is.EqualTo(30));
            Assert.That(mark.GetYOffset(), Is.EqualTo(40));
        });
    }

    [TestCase(ShapingDirection.LeftToRight, 10, 30)]
    [TestCase(ShapingDirection.TopToBottom, 10, 30)]
    [TestCase(ShapingDirection.RightToLeft, 30, 10)]
    [TestCase(ShapingDirection.BottomToTop, 30, 10)]
    public void MapsLogicalAndVisualOrderForEveryDirection(
        ShapingDirection direction,
        int firstVisualGlyphId,
        int lastVisualGlyphId)
    {
        RuneBuffer source = Decode("abc");
        ShapingBuffer buffer = CreateMappedBuffer(source, direction);
        SfntGposPositionBuffer positions = SfntGposPositionBuffer.Create(
            new int[] { 10, 20, 30 });
        positions.SetXAdvance(0, 100);
        positions.SetXAdvance(1, 200);
        positions.SetXAdvance(2, 300);

        ShapedRun run = ShapedRunBuilder.Build(
            buffer,
            source,
            positions);
        bool forward = direction == ShapingDirection.LeftToRight ||
            direction == ShapingDirection.TopToBottom;

        Assert.Multiple(() =>
        {
            Assert.That(run.GetGlyph(0).GetGlyphId(),
                Is.EqualTo(firstVisualGlyphId));
            Assert.That(run.GetGlyph(2).GetGlyphId(),
                Is.EqualTo(lastVisualGlyphId));
            Assert.That(run.GetVisualGlyphIndex(0),
                Is.EqualTo(forward ? 0 : 2));
            Assert.That(run.GetLogicalGlyphIndex(0),
                Is.EqualTo(forward ? 0 : 2));
            Assert.That(run.GetLogicalGlyph(0).GetGlyphId(), Is.EqualTo(10));
            Assert.That(run.GetGlyph(0).GetLogicalIndex(),
                Is.EqualTo(forward ? 0 : 2));
            Assert.That(run.GetGlyph(0).GetVisualIndex(), Is.Zero);
            Assert.That(run.GetVisualClusterIndex(0),
                Is.EqualTo(forward ? 0 : 2));
            Assert.That(run.GetLogicalClusterIndex(0),
                Is.EqualTo(forward ? 0 : 2));
            Assert.That(run.GetVisualCluster(0).GetUtf16Start(),
                Is.EqualTo(forward ? 0 : 2));
            Assert.That(run.GetGlyph(0).GetXAdvance(),
                Is.EqualTo(forward ? 100 : 300));
        });
    }

    [Test]
    public void RetainsUnicodeClustersThatHaveNoOutputGlyph()
    {
        RuneBuffer source = Decode("a\u200Db");
        ShapingBuffer buffer = new ShapingBuffer(
            source,
            ShapingDirection.LeftToRight);
        buffer.Remove(1);
        buffer.SetGlyphId(0, 1);
        buffer.SetGlyphId(1, 2);

        ShapedRun run = ShapedRunBuilder.Build(buffer, source);

        Assert.Multiple(() =>
        {
            Assert.That(run.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(run.GetClusterCount(), Is.EqualTo(3));
            Assert.That(run.GetCluster(1).GetUtf16Start(), Is.EqualTo(1));
            Assert.That(run.GetCluster(1).GetUtf16Length(), Is.EqualTo(1));
            Assert.That(run.GetCluster(1).GetUnicodeScalarCount(),
                Is.EqualTo(1));
            Assert.That(run.GetCluster(1).GetUnicodeScalar(0),
                Is.EqualTo(0x200D));
            Assert.That(run.GetCluster(1).GetGlyphCount(), Is.Zero);
            Assert.That(run.GetCluster(2).GetGlyphVisualIndex(0),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsIncompleteOrMismatchedOutputState()
    {
        RuneBuffer source = Decode("ab");
        ShapingBuffer incomplete = new ShapingBuffer(
            source,
            ShapingDirection.LeftToRight);
        incomplete.SetGlyphId(0, 1);
        ShapingBuffer complete = CreateMappedBuffer(
            source,
            ShapingDirection.LeftToRight);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                ShapedRunBuilder.Build(incomplete, source));
            Assert.Throws<ArgumentException>(() =>
                ShapedRunBuilder.Build(
                    complete,
                    source,
                    SfntGposPositionBuffer.Create(new int[] { 10 })));
            Assert.Throws<ArgumentException>(() =>
                ShapedRunBuilder.Build(
                    complete,
                    source,
                    SfntGposPositionBuffer.Create(new int[] { 10, 99 })));
            Assert.Throws<ArgumentException>(() =>
                ShapedRunBuilder.Build(complete, Decode("abc")));
        });
    }

    static ShapingBuffer CreateMappedBuffer(
        RuneBuffer source,
        ShapingDirection direction)
    {
        ShapingBuffer buffer = new ShapingBuffer(source, direction);
        int index = 0;
        while (index < buffer.GetCount())
        {
            buffer.SetGlyphId(index, (index + 1) * 10);
            index += 1;
        }

        return buffer;
    }

    static RuneBuffer Decode(string text)
    {
        return Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
    }
}
