namespace ttf.Tests;

internal class EmojiShaperTests
{
    static UnicodeData UnicodeData = UnicodeData.LoadDefault();

    [Test]
    public void SelectsDefaultAndExplicitPresentations()
    {
        EmojiSequenceMap map = Prepare(
            "\U0001F600\u2764\u2764\uFE0F\U0001F600\uFE0E");

        Assert.Multiple(() =>
        {
            AssertSequence(map, 0, 0, 1,
                EmojiSequenceType.EmojiCharacter,
                EmojiPresentation.Emoji, false);
            AssertSequence(map, 1, 1, 1,
                EmojiSequenceType.EmojiCharacter,
                EmojiPresentation.Text, false);
            AssertSequence(map, 2, 2, 2,
                EmojiSequenceType.Variation,
                EmojiPresentation.Emoji, true);
            AssertSequence(map, 3, 4, 2,
                EmojiSequenceType.Variation,
                EmojiPresentation.Text, true);
        });
    }

    [Test]
    public void PreservesModifierAndKeycapSequencesAsUnsafeClusters()
    {
        ShapingBuffer modifierBuffer = CreateBuffer(
            "\U0001F44D\U0001F3FB");
        EmojiSequenceMap modifier = EmojiShaper.Prepare(
            modifierBuffer,
            UnicodeData,
            1);
        ShapingBuffer keycapBuffer = CreateBuffer("1\uFE0F\u20E3");
        EmojiSequenceMap keycap = EmojiShaper.Prepare(
            keycapBuffer,
            UnicodeData,
            1);

        Assert.Multiple(() =>
        {
            AssertSequence(modifier, 0, 0, 2,
                EmojiSequenceType.Modifier,
                EmojiPresentation.Emoji, false);
            Assert.That(modifierBuffer.GetCount(), Is.EqualTo(2));
            Assert.That(modifierBuffer.GetClusterLength(0), Is.EqualTo(4));
            Assert.That(modifierBuffer.IsUnsafeToBreak(1), Is.True);
            AssertSequence(keycap, 0, 0, 3,
                EmojiSequenceType.Keycap,
                EmojiPresentation.Emoji, true);
            Assert.That(keycapBuffer.GetClusterLength(2), Is.EqualTo(3));
            Assert.That(keycapBuffer.IsDefaultIgnorable(1), Is.True);
        });
    }

    [Test]
    public void ExplicitTextPresentationIsNotOverriddenBySequenceTails()
    {
        EmojiSequenceMap modifier = Prepare(
            "\U0001F44D\uFE0E\U0001F3FB");
        EmojiSequenceMap keycap = Prepare("1\uFE0E\u20E3");

        Assert.Multiple(() =>
        {
            AssertSequence(modifier, 0, 0, 2,
                EmojiSequenceType.Variation,
                EmojiPresentation.Text, true);
            Assert.That(modifier.GetSequenceCount(), Is.EqualTo(2));
            AssertSequence(keycap, 0, 0, 2,
                EmojiSequenceType.Variation,
                EmojiPresentation.Text, true);
            Assert.That(keycap.GetSequenceCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void PairsRegionalIndicatorsWithoutAbsorbingAThird()
    {
        EmojiSequenceMap map = Prepare(
            "\U0001F1FA\U0001F1F8\U0001F1E8");

        Assert.Multiple(() =>
        {
            AssertSequence(map, 0, 0, 2,
                EmojiSequenceType.RegionalIndicator,
                EmojiPresentation.Emoji, false);
            AssertSequence(map, 1, 2, 1,
                EmojiSequenceType.EmojiCharacter,
                EmojiPresentation.Emoji, false);
            Assert.That(map.GetRuneSequenceIndex(1), Is.Zero);
            Assert.That(map.GetRuneSequenceIndex(2), Is.EqualTo(1));
        });
    }

    [Test]
    public void PreservesCompletedTagSequencesAndTheirSourceRange()
    {
        string england = "\U0001F3F4\U000E0067\U000E0062" +
            "\U000E0065\U000E006E\U000E0067\U000E007F";
        ShapingBuffer buffer = CreateBuffer(england);
        EmojiSequenceMap map = EmojiShaper.Prepare(
            buffer,
            UnicodeData,
            1);

        Assert.Multiple(() =>
        {
            AssertSequence(map, 0, 0, 7, EmojiSequenceType.Tag,
                EmojiPresentation.Emoji, false);
            Assert.That(buffer.GetCount(), Is.EqualTo(7));
            Assert.That(buffer.GetClusterStart(6), Is.Zero);
            Assert.That(buffer.GetClusterLength(6),
                Is.EqualTo(england.Length));
            Assert.That(buffer.IsDefaultIgnorable(6), Is.True);
            Assert.That(buffer.IsUnsafeToBreak(3), Is.True);
        });
    }

    [Test]
    public void PreservesEmojiZwjSequencesWithSelectorsAndModifiers()
    {
        string sequence = "\U0001F469\U0001F3FD\u200D" +
            "\u2764\uFE0F\u200D\U0001F469";
        ShapingBuffer buffer = CreateBuffer(sequence);
        EmojiSequenceMap map = EmojiShaper.Prepare(
            buffer,
            UnicodeData,
            1);

        Assert.Multiple(() =>
        {
            AssertSequence(map, 0, 0, 7,
                EmojiSequenceType.ZeroWidthJoiner,
                EmojiPresentation.Emoji, true);
            Assert.That(buffer.GetCount(), Is.EqualTo(7));
            Assert.That(buffer.GetClusterLength(0),
                Is.EqualTo(sequence.Length));
            Assert.That(buffer.IsDefaultIgnorable(2), Is.True);
            Assert.That(buffer.IsDefaultIgnorable(4), Is.True);
            Assert.That(buffer.IsUnsafeToBreak(6), Is.True);
        });
    }

    [Test]
    public void LeavesIncompleteTagsOutsideTheEmojiBaseCluster()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\U0001F3F4\U000E0067\U000E0062");
        EmojiSequenceMap map = EmojiShaper.Prepare(
            buffer,
            UnicodeData,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetSequenceCount(), Is.EqualTo(3));
            AssertSequence(map, 0, 0, 1,
                EmojiSequenceType.EmojiCharacter,
                EmojiPresentation.Emoji, false);
            Assert.That(map.GetSequenceType(1),
                Is.EqualTo(EmojiSequenceType.Text));
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(2));
            Assert.That(buffer.IsDefaultIgnorable(1), Is.True);
            Assert.That(buffer.IsUnsafeToBreak(1), Is.False);
        });
    }

    [Test]
    public void SupportsEmptyInputAndValidatesArgumentsAndLimits()
    {
        ShapingBuffer emptyBuffer = CreateBuffer("");
        EmojiSequenceMap empty = EmojiShaper.Prepare(
            emptyBuffer,
            UnicodeData,
            0);
        ShapingBuffer buffer = CreateBuffer("A");

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetSequenceCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() => EmojiShaper.Prepare(
                null!,
                UnicodeData,
                1));
            Assert.Throws<ArgumentNullException>(() => EmojiShaper.Prepare(
                buffer,
                null!,
                1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EmojiShaper.Prepare(buffer, UnicodeData, -1));
            Assert.Throws<FontLimitException>(() =>
                EmojiShaper.Prepare(buffer, UnicodeData, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetSequenceType(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetRuneSequenceIndex(0));
        });
    }

    static EmojiSequenceMap Prepare(string text)
    {
        ShapingBuffer buffer = CreateBuffer(text);
        return EmojiShaper.Prepare(buffer, UnicodeData, buffer.GetCount());
    }

    static ShapingBuffer CreateBuffer(string text)
    {
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        return new ShapingBuffer(input, ShapingDirection.LeftToRight);
    }

    static void AssertSequence(
        EmojiSequenceMap map,
        int sequenceIndex,
        int start,
        int length,
        EmojiSequenceType type,
        EmojiPresentation presentation,
        bool explicitPresentation)
    {
        Assert.That(map.GetSequenceStart(sequenceIndex), Is.EqualTo(start));
        Assert.That(map.GetSequenceLength(sequenceIndex), Is.EqualTo(length));
        Assert.That(map.GetSequenceType(sequenceIndex), Is.EqualTo(type));
        Assert.That(map.GetPresentation(sequenceIndex),
            Is.EqualTo(presentation));
        Assert.That(map.HasExplicitPresentation(sequenceIndex),
            Is.EqualTo(explicitPresentation));
    }
}
