namespace ttf.Tests;

public class ShapingBufferTests
{
    [Test]
    public void CopiesRunesClustersDirectionAndDefaultState()
    {
        System.Text.Rune[] runes = new System.Text.Rune[]
        {
            new System.Text.Rune('A'),
            new System.Text.Rune(0x1F600)
        };
        int[] starts = new int[] { 0, 1 };
        int[] lengths = new int[] { 1, 2 };
        RuneBuffer input = new RuneBuffer(runes, starts, lengths, 3, 0);
        ShapingBuffer buffer = new ShapingBuffer(
            input,
            ShapingDirection.RightToLeft);
        runes[0] = new System.Text.Rune('Z');
        starts[1] = 0;
        lengths[1] = 1;

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetCount(), Is.EqualTo(2));
            Assert.That(buffer.GetSourceLength(), Is.EqualTo(3));
            Assert.That(buffer.GetDirection(),
                Is.EqualTo(ShapingDirection.RightToLeft));
            Assert.That(buffer.IsHorizontal(), Is.True);
            Assert.That(buffer.IsForward(), Is.False);
            Assert.That(buffer.GetRune(0).Value, Is.EqualTo('A'));
            Assert.That(buffer.GetScalarValue(1), Is.EqualTo(0x1F600));
            Assert.That(buffer.GetRune(1).Value, Is.EqualTo(0x1F600));
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(1));
            Assert.That(buffer.GetClusterLength(1), Is.EqualTo(2));
            Assert.That(buffer.GetClusterEnd(1), Is.EqualTo(3));
            Assert.That(buffer.HasGlyphId(0), Is.False);
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(-1));
            Assert.That(buffer.GetMask(0), Is.Zero);
            Assert.That(buffer.GetSyllable(0), Is.Zero);
            Assert.That(buffer.HasLigature(0), Is.False);
            Assert.That(buffer.GetLigatureComponentCount(0), Is.EqualTo(1));
            Assert.That(buffer.GetLigatureComponentIndex(0), Is.Zero);
            Assert.That(buffer.GetAttachmentParent(0), Is.EqualTo(-1));
            Assert.That(buffer.GetAttachmentType(0),
                Is.EqualTo(ShapingAttachmentType.None));
            Assert.That(buffer.IsUnsafeToBreak(0), Is.False);
        });
    }

    [Test]
    public void StoresEveryMutableShapingState()
    {
        ShapingBuffer buffer = CreateBuffer("abc");

        buffer.SetDirection(ShapingDirection.TopToBottom);
        buffer.SetRune(0, new System.Text.Rune('x'));
        buffer.SetGlyphId(0, 42);
        buffer.SetCluster(0, 0, 2);
        buffer.SetMask(0, 0x05UL);
        buffer.AddMask(0, 0x0AUL);
        buffer.RemoveMask(0, 0x04UL);
        buffer.SetSyllable(0, 7);
        buffer.SetLigature(0, 12, 3, 1);
        buffer.SetAttachment(2, 0, ShapingAttachmentType.Mark);
        buffer.SetUnsafeToBreak(0, true);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetDirection(),
                Is.EqualTo(ShapingDirection.TopToBottom));
            Assert.That(buffer.IsHorizontal(), Is.False);
            Assert.That(buffer.IsForward(), Is.True);
            Assert.That(buffer.GetRune(0).Value, Is.EqualTo('x'));
            Assert.That(buffer.HasGlyphId(0), Is.True);
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(42));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(2));
            Assert.That(buffer.GetMask(0), Is.EqualTo(0x0BUL));
            Assert.That(buffer.GetSyllable(0), Is.EqualTo(7));
            Assert.That(buffer.HasLigature(0), Is.True);
            Assert.That(buffer.GetLigatureId(0), Is.EqualTo(12));
            Assert.That(buffer.GetLigatureComponentCount(0), Is.EqualTo(3));
            Assert.That(buffer.GetLigatureComponentIndex(0), Is.EqualTo(1));
            Assert.That(buffer.GetAttachmentParent(2), Is.Zero);
            Assert.That(buffer.GetAttachmentType(2),
                Is.EqualTo(ShapingAttachmentType.Mark));
            Assert.That(buffer.IsUnsafeToBreak(0), Is.True);
        });

        buffer.ClearGlyphId(0);
        buffer.ClearLigature(0);
        buffer.ClearAttachment(2);
        buffer.SetUnsafeToBreak(0, false);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.HasGlyphId(0), Is.False);
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(-1));
            Assert.That(buffer.HasLigature(0), Is.False);
            Assert.That(buffer.GetLigatureComponentCount(0), Is.EqualTo(1));
            Assert.That(buffer.GetAttachmentParent(2), Is.EqualTo(-1));
            Assert.That(buffer.GetAttachmentType(2),
                Is.EqualTo(ShapingAttachmentType.None));
            Assert.That(buffer.IsUnsafeToBreak(0), Is.False);
        });
    }

    [Test]
    public void ReordersAllItemStateAndRemapsAttachments()
    {
        ShapingBuffer buffer = CreateBuffer("abc");
        int index = 0;
        while (index < buffer.GetCount())
        {
            buffer.SetGlyphId(index, 10 + index);
            buffer.SetMask(index, (ulong)(1 << index));
            buffer.SetSyllable(index, 20 + index);
            buffer.SetLigature(index, 30 + index, 3, index);
            buffer.SetDefaultIgnorable(index, index == 2);
            buffer.SetUnsafeToBreak(index, index == 2);
            index++;
        }

        buffer.SetAttachment(2, 0, ShapingAttachmentType.Cursive);
        int[] permutation = new int[] { 2, 0, 1 };
        buffer.Reorder(permutation);
        permutation[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(ToText(buffer), Is.EqualTo("cab"));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(12));
            Assert.That(buffer.GetClusterStart(0), Is.EqualTo(2));
            Assert.That(buffer.GetMask(0), Is.EqualTo(4UL));
            Assert.That(buffer.GetSyllable(0), Is.EqualTo(22));
            Assert.That(buffer.GetLigatureId(0), Is.EqualTo(32));
            Assert.That(buffer.GetLigatureComponentIndex(0), Is.EqualTo(2));
            Assert.That(buffer.GetAttachmentParent(0), Is.EqualTo(1));
            Assert.That(buffer.GetAttachmentType(0),
                Is.EqualTo(ShapingAttachmentType.Cursive));
            Assert.That(buffer.IsDefaultIgnorable(0), Is.True);
            Assert.That(buffer.IsUnsafeToBreak(0), Is.True);
            Assert.That(buffer.GetGlyphId(1), Is.EqualTo(10));
            Assert.That(buffer.GetClusterStart(2), Is.EqualTo(1));
        });
    }

    [Test]
    public void InsertsSyntheticRunesAndRemapsExistingAttachments()
    {
        ShapingBuffer buffer = CreateBuffer("abc");
        buffer.SetGlyphId(2, 42);
        buffer.SetMask(2, 8);
        buffer.SetAttachment(0, 2, ShapingAttachmentType.Mark);

        buffer.InsertRune(1, new System.Text.Rune('x'), 2);

        Assert.Multiple(() =>
        {
            Assert.That(ToText(buffer), Is.EqualTo("axbc"));
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(2));
            Assert.That(buffer.GetClusterLength(1), Is.EqualTo(1));
            Assert.That(buffer.HasGlyphId(1), Is.False);
            Assert.That(buffer.GetMask(1), Is.Zero);
            Assert.That(buffer.GetLigatureComponentCount(1), Is.EqualTo(1));
            Assert.That(buffer.GetAttachmentParent(1), Is.EqualTo(-1));
            Assert.That(buffer.GetGlyphId(3), Is.EqualTo(42));
            Assert.That(buffer.GetMask(3), Is.EqualTo(8));
            Assert.That(buffer.GetAttachmentParent(0), Is.EqualTo(3));
        });
    }

    [Test]
    public void MergesNonMonotonicSourceClustersWithoutChangingItems()
    {
        RuneBuffer normalized = UnicodeNormalizer.Normalize(
            Decode("A\u0315\u0300"),
            UnicodeData.LoadDefault(),
            UnicodeNormalizationMode.Nfd,
            3);
        ShapingBuffer buffer = new ShapingBuffer(
            normalized,
            ShapingDirection.LeftToRight);

        buffer.MergeClusters(1, 3);

        Assert.Multiple(() =>
        {
            Assert.That(ToText(buffer), Is.EqualTo("A\u0300\u0315"));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(1));
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(1));
            Assert.That(buffer.GetClusterLength(1), Is.EqualTo(2));
            Assert.That(buffer.GetClusterStart(2), Is.EqualTo(1));
            Assert.That(buffer.GetClusterLength(2), Is.EqualTo(2));
        });
    }

    [Test]
    public void RejectsAttachmentCyclesAndSupportsClearingAChain()
    {
        ShapingBuffer buffer = CreateBuffer("abc");
        buffer.SetAttachment(1, 0, ShapingAttachmentType.Mark);
        buffer.SetAttachment(2, 1, ShapingAttachmentType.Mark);

        Assert.Throws<ArgumentException>(() => buffer.SetAttachment(
            0,
            2,
            ShapingAttachmentType.Mark));

        buffer.ClearAttachment(1);
        buffer.SetAttachment(0, 2, ShapingAttachmentType.Cursive);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetAttachmentParent(1), Is.EqualTo(-1));
            Assert.That(buffer.GetAttachmentParent(0), Is.EqualTo(2));
            Assert.That(buffer.GetAttachmentType(0),
                Is.EqualTo(ShapingAttachmentType.Cursive));
        });
    }

    [Test]
    public void SupportsAnEmptyBuffer()
    {
        ShapingBuffer buffer = CreateBuffer("");

        buffer.Reorder(new int[0]);
        buffer.SetDirection(ShapingDirection.BottomToTop);

        Assert.That(buffer.GetCount(), Is.Zero);
        Assert.That(buffer.GetSourceLength(), Is.Zero);
        Assert.That(buffer.IsHorizontal(), Is.False);
        Assert.That(buffer.IsForward(), Is.False);
    }

    [Test]
    public void RemovalPreservesStateAndRemapsOrClearsAttachments()
    {
        ShapingBuffer retainedParent = CreateBuffer("abcd");
        retainedParent.SetGlyphId(3, 13);
        retainedParent.SetMask(3, 8);
        retainedParent.SetSyllable(3, 4);
        retainedParent.SetLigature(3, 9, 2, 1);
        retainedParent.SetDefaultIgnorable(3, true);
        retainedParent.SetUnsafeToBreak(3, true);
        retainedParent.SetAttachment(3, 2, ShapingAttachmentType.Mark);

        retainedParent.Remove(1);

        Assert.Multiple(() =>
        {
            Assert.That(ToText(retainedParent), Is.EqualTo("acd"));
            Assert.That(retainedParent.GetGlyphId(2), Is.EqualTo(13));
            Assert.That(retainedParent.GetMask(2), Is.EqualTo(8));
            Assert.That(retainedParent.GetSyllable(2), Is.EqualTo(4));
            Assert.That(retainedParent.GetLigatureId(2), Is.EqualTo(9));
            Assert.That(retainedParent.IsDefaultIgnorable(2), Is.True);
            Assert.That(retainedParent.IsUnsafeToBreak(2), Is.True);
            Assert.That(retainedParent.GetAttachmentParent(2), Is.EqualTo(1));
        });

        ShapingBuffer removedParent = CreateBuffer("abc");
        removedParent.SetAttachment(2, 1, ShapingAttachmentType.Cursive);
        removedParent.Remove(1);

        Assert.That(removedParent.GetAttachmentParent(1), Is.EqualTo(-1));
        Assert.That(removedParent.GetAttachmentType(1),
            Is.EqualTo(ShapingAttachmentType.None));
    }

    [Test]
    public void ValidatesStateAndPermutationArguments()
    {
        ShapingBuffer buffer = CreateBuffer("abc");
        RuneBuffer invalidRanges = new RuneBuffer(
            new System.Text.Rune[] { new System.Text.Rune('a') },
            new int[] { 1 },
            new int[] { 1 },
            1,
            0);
        RuneBuffer invalidEmpty = new RuneBuffer(
            new System.Text.Rune[0],
            new int[0],
            new int[0],
            -1,
            0);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => new ShapingBuffer(
                null!,
                ShapingDirection.LeftToRight));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShapingBuffer(
                Decode("a"),
                (ShapingDirection)4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShapingBuffer(
                invalidRanges,
                ShapingDirection.LeftToRight));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShapingBuffer(
                invalidEmpty,
                ShapingDirection.LeftToRight));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.GetRune(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.Remove(3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.InsertRune(
                    -1,
                    new System.Text.Rune('x'),
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.InsertRune(
                    4,
                    new System.Text.Rune('x'),
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.InsertRune(
                    0,
                    new System.Text.Rune('x'),
                    3));
            Assert.Throws<ArgumentNullException>(() =>
                buffer.RemoveItems(null!));
            Assert.Throws<ArgumentException>(() => buffer.RemoveItems(
                new bool[] { false }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetGlyphId(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetGlyphId(0, 0x10000));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetCluster(0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetCluster(0, 2, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetSyllable(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetLigature(0, 0, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetLigature(0, 1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetLigature(0, 1, 2, 2));
            Assert.Throws<ArgumentException>(() => buffer.SetAttachment(
                0,
                0,
                ShapingAttachmentType.Mark));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetAttachment(0, 1, ShapingAttachmentType.None));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.SetDirection((ShapingDirection)9));
            Assert.Throws<ArgumentNullException>(() => buffer.Reorder(null!));
            Assert.Throws<ArgumentException>(() => buffer.Reorder(
                new int[] { 0, 1 }));
            Assert.Throws<ArgumentException>(() => buffer.Reorder(
                new int[] { 0, 1, 1 }));
            Assert.Throws<ArgumentException>(() => buffer.Reorder(
                new int[] { 0, 1, 3 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.MergeClusters(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                buffer.MergeClusters(0, 4));
        });
    }

    static ShapingBuffer CreateBuffer(string text)
    {
        return new ShapingBuffer(
            Decode(text),
            ShapingDirection.LeftToRight);
    }

    static RuneBuffer Decode(string text)
    {
        return Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
    }

    static string ToText(ShapingBuffer buffer)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < buffer.GetCount())
        {
            builder.Append(buffer.GetRune(index).ToString());
            index++;
        }

        return builder.ToString();
    }
}
