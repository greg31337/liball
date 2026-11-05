namespace ttf.Tests;

internal class KhmerSyllableRecognizerTests
{
    static UnicodeData UnicodeData = UnicodeData.LoadDefault();

    [Test]
    public void MapsUnicodeDataAndKhmerOverridesToMachineCategories()
    {
        KhmerSyllableMap map = Recognize(
            "\u1780\u17A3\u17D2\u200C\u200D\u17D9\u25CC" +
            "\u179A\u17B7\u17BB\u17C1\u17B6\u17CC\u17C6\u17C7");

        KhmerSyllabicCategory[] expected =
            new KhmerSyllabicCategory[]
        {
            KhmerSyllabicCategory.Consonant,
            KhmerSyllabicCategory.Vowel,
            KhmerSyllabicCategory.Coeng,
            KhmerSyllabicCategory.NonJoiner,
            KhmerSyllabicCategory.Joiner,
            KhmerSyllabicCategory.Placeholder,
            KhmerSyllabicCategory.DottedCircle,
            KhmerSyllabicCategory.Ra,
            KhmerSyllabicCategory.VowelAbove,
            KhmerSyllabicCategory.VowelBelow,
            KhmerSyllabicCategory.VowelPre,
            KhmerSyllabicCategory.VowelPost,
            KhmerSyllabicCategory.Robatic,
            KhmerSyllabicCategory.XGroup,
            KhmerSyllabicCategory.YGroup
        };

        Assert.Multiple(() =>
        {
            Assert.That(map.GetRuneCount(), Is.EqualTo(expected.Length));
            int index = 0;
            while (index < expected.Length)
            {
                Assert.That(map.GetCategory(index),
                    Is.EqualTo(expected[index]),
                    "category at " + index);
                index++;
            }
        });
    }

    [Test]
    public void RecognizesAFullKhmerConsonantSyllable()
    {
        string text = "\u1780\u17CC\u17D2\u179A" +
            "\u17C1\u17BB\u17B7\u17B6\u17C7";
        ShapingBuffer buffer = CreateBuffer(text);
        KhmerSyllableMap map = KhmerSyllableRecognizer.Recognize(
            buffer,
            UnicodeData,
            1);

        Assert.Multiple(() =>
        {
            AssertSyllable(map, 0, 0, 9, KhmerSyllableType.Consonant);
            Assert.That(map.HasBrokenSyllable(), Is.False);
            Assert.That(map.GetRuneSyllableIndex(8), Is.Zero);
            Assert.That(buffer.GetSyllable(0),
                Is.EqualTo(buffer.GetSyllable(8)));
            Assert.That(buffer.GetSyllable(0) & 15,
                Is.EqualTo((int)KhmerSyllableType.Consonant));
        });
    }

    [TestCase("\u17D2\u1780", 2)]
    [TestCase("\u17C1\u17C6", 2)]
    [TestCase("\u17CC", 1)]
    [TestCase("\u17D2", 1)]
    public void DetectsBrokenKhmerClusters(string text, int length)
    {
        KhmerSyllableMap map = Recognize(text);

        Assert.Multiple(() =>
        {
            AssertSyllable(map, 0, 0, length,
                KhmerSyllableType.Broken);
            Assert.That(map.HasBrokenSyllable(), Is.True);
        });
    }

    [Test]
    public void AcceptsPlaceholdersAndDottedCirclesAsBases()
    {
        KhmerSyllableMap placeholder = Recognize("\u17D9\u17C1");
        KhmerSyllableMap dotted = Recognize("\u25CC\u17B7");

        Assert.Multiple(() =>
        {
            AssertSyllable(placeholder, 0, 0, 2,
                KhmerSyllableType.Consonant);
            AssertSyllable(dotted, 0, 0, 2,
                KhmerSyllableType.Consonant);
        });
    }

    [Test]
    public void SeparatesKhmerBrokenAndNonKhmerClusters()
    {
        KhmerSyllableMap map = Recognize(
            "\u1780\u17D2\u1781\u17C1\u17D2A");

        Assert.Multiple(() =>
        {
            Assert.That(map.GetSyllableCount(), Is.EqualTo(3));
            AssertSyllable(map, 0, 0, 4,
                KhmerSyllableType.Consonant);
            AssertSyllable(map, 1, 4, 1,
                KhmerSyllableType.Broken);
            AssertSyllable(map, 2, 5, 1,
                KhmerSyllableType.NonKhmer);
            Assert.That(map.GetRuneSyllableIndex(5), Is.EqualTo(2));
        });
    }

    [Test]
    public void SupportsEmptyInputAndValidatesLimitsAndArguments()
    {
        ShapingBuffer emptyBuffer = CreateBuffer("");
        KhmerSyllableMap empty = KhmerSyllableRecognizer.Recognize(
            emptyBuffer,
            UnicodeData,
            0);
        ShapingBuffer limited = CreateBuffer("AB");

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetRuneCount(), Is.Zero);
            Assert.That(empty.GetSyllableCount(), Is.Zero);
            Assert.That(empty.HasBrokenSyllable(), Is.False);
            Assert.Throws<FontLimitException>(() =>
                KhmerSyllableRecognizer.Recognize(
                    limited,
                    UnicodeData,
                    1));
            Assert.That(limited.GetSyllable(0), Is.Zero);
            Assert.That(limited.GetSyllable(1), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                KhmerSyllableRecognizer.Recognize(
                    null!,
                    UnicodeData,
                    1));
            Assert.Throws<ArgumentNullException>(() =>
                KhmerSyllableRecognizer.Recognize(
                    limited,
                    null!,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                KhmerSyllableRecognizer.Recognize(
                    limited,
                    UnicodeData,
                    -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetCategory(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetSyllableType(0));
        });
    }

    static KhmerSyllableMap Recognize(string text)
    {
        return KhmerSyllableRecognizer.Recognize(
            CreateBuffer(text),
            UnicodeData,
            text.Length + 1);
    }

    static ShapingBuffer CreateBuffer(string text)
    {
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        return new ShapingBuffer(input, ShapingDirection.LeftToRight);
    }

    static void AssertSyllable(
        KhmerSyllableMap map,
        int index,
        int start,
        int length,
        KhmerSyllableType type)
    {
        Assert.That(map.GetSyllableStart(index), Is.EqualTo(start));
        Assert.That(map.GetSyllableLength(index), Is.EqualTo(length));
        Assert.That(map.GetSyllableEnd(index), Is.EqualTo(start + length));
        Assert.That(map.GetSyllableType(index), Is.EqualTo(type));
    }
}
