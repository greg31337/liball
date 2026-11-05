namespace ttf.Tests;

internal class MyanmarSyllableRecognizerTests
{
    static UnicodeData UnicodeData = UnicodeData.LoadDefault();

    [Test]
    public void MapsUnicodeDataAndMyanmarOverridesToMachineCategories()
    {
        MyanmarSyllableMap map = Recognize(
            "\u1000\u1021\u1037\u1039\u200C\u200D\u1038" +
            "\u1040\u25CC\u1032\u1004\u102D\u102F\u1031" +
            "\u102C\u103A\u103E\u103C\u103D\u103B\u1063" +
            "\uFE00\u1060\u1065\u1067A");
        MyanmarSyllabicCategory[] expected =
            new MyanmarSyllabicCategory[]
        {
            MyanmarSyllabicCategory.Consonant,
            MyanmarSyllabicCategory.IndependentVowel,
            MyanmarSyllabicCategory.DotBelow,
            MyanmarSyllabicCategory.Halant,
            MyanmarSyllabicCategory.NonJoiner,
            MyanmarSyllabicCategory.Joiner,
            MyanmarSyllabicCategory.SyllableModifier,
            MyanmarSyllabicCategory.Placeholder,
            MyanmarSyllabicCategory.DottedCircle,
            MyanmarSyllabicCategory.A,
            MyanmarSyllabicCategory.Ra,
            MyanmarSyllabicCategory.VowelAbove,
            MyanmarSyllabicCategory.VowelBelow,
            MyanmarSyllabicCategory.VowelPre,
            MyanmarSyllabicCategory.VowelPost,
            MyanmarSyllabicCategory.Asat,
            MyanmarSyllabicCategory.MedialHa,
            MyanmarSyllabicCategory.MedialRa,
            MyanmarSyllabicCategory.MedialWa,
            MyanmarSyllabicCategory.MedialYa,
            MyanmarSyllabicCategory.PwoTone,
            MyanmarSyllabicCategory.VariationSelector,
            MyanmarSyllabicCategory.MedialLa,
            MyanmarSyllabicCategory.Consonant,
            MyanmarSyllabicCategory.VowelPost,
            MyanmarSyllabicCategory.Other
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
    public void RecognizesAFullMyanmarConsonantSyllable()
    {
        string text = "\u1004\u103A\u1039\u1000\uFE00" +
            "\u1039\u1001\uFE01\u103A\u103B\u103A\u103C" +
            "\u103D\u103E\u1060\u103A\u1031\uFE00\u102D" +
            "\u102F\u1032\u1037\u103A\u102C\u103E\u1060" +
            "\u103A\u102D\u1032\u1037\u103A\u1038\u1063" +
            "\u1032\u1037\u103A\u200D";
        ShapingBuffer buffer = CreateBuffer(text);
        MyanmarSyllableMap map = MyanmarSyllableRecognizer.Recognize(
            buffer,
            UnicodeData,
            1);

        Assert.Multiple(() =>
        {
            AssertSyllable(
                map,
                0,
                0,
                buffer.GetCount(),
                MyanmarSyllableType.Consonant);
            Assert.That(map.HasBrokenSyllable(), Is.False);
            Assert.That(map.GetRuneSyllableIndex(buffer.GetCount() - 1),
                Is.Zero);
            Assert.That(buffer.GetSyllable(0),
                Is.EqualTo(buffer.GetSyllable(buffer.GetCount() - 1)));
        });
    }

    [Test]
    public void RecognizesHalantTerminatedAndPlaceholderSyllables()
    {
        MyanmarSyllableMap halant = Recognize(
            "\u1000\u1039\u1001\uFE00\u1039");
        MyanmarSyllableMap placeholder = Recognize("\u1040\u1031");

        Assert.Multiple(() =>
        {
            AssertSyllable(
                halant,
                0,
                0,
                5,
                MyanmarSyllableType.Consonant);
            AssertSyllable(
                placeholder,
                0,
                0,
                2,
                MyanmarSyllableType.Consonant);
        });
    }

    [TestCase("\u1004\u103A\u1039\u1031", 4)]
    [TestCase("\u1031\u102D", 2)]
    [TestCase("\u1039", 1)]
    [TestCase("\u103A\u103C", 2)]
    public void DetectsBrokenMyanmarClusters(string text, int length)
    {
        MyanmarSyllableMap map = Recognize(text);

        Assert.Multiple(() =>
        {
            AssertSyllable(
                map,
                0,
                0,
                length,
                MyanmarSyllableType.Broken);
            Assert.That(map.HasBrokenSyllable(), Is.True);
        });
    }

    [Test]
    public void KeepsStandaloneJoinersAndNonMyanmarTextSeparate()
    {
        MyanmarSyllableMap map = Recognize("\u200D\u200CA\u1000");

        Assert.Multiple(() =>
        {
            Assert.That(map.GetSyllableCount(), Is.EqualTo(4));
            AssertSyllable(
                map,
                0,
                0,
                1,
                MyanmarSyllableType.NonMyanmar);
            AssertSyllable(
                map,
                1,
                1,
                1,
                MyanmarSyllableType.NonMyanmar);
            AssertSyllable(
                map,
                2,
                2,
                1,
                MyanmarSyllableType.NonMyanmar);
            AssertSyllable(
                map,
                3,
                3,
                1,
                MyanmarSyllableType.Consonant);
        });
    }

    [Test]
    public void SupportsEmptyInputAndValidatesLimitsAndArguments()
    {
        ShapingBuffer emptyBuffer = CreateBuffer("");
        MyanmarSyllableMap empty = MyanmarSyllableRecognizer.Recognize(
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
                MyanmarSyllableRecognizer.Recognize(
                    limited,
                    UnicodeData,
                    1));
            Assert.That(limited.GetSyllable(0), Is.Zero);
            Assert.That(limited.GetSyllable(1), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                MyanmarSyllableRecognizer.Recognize(
                    null!,
                    UnicodeData,
                    1));
            Assert.Throws<ArgumentNullException>(() =>
                MyanmarSyllableRecognizer.Recognize(
                    limited,
                    null!,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MyanmarSyllableRecognizer.Recognize(
                    limited,
                    UnicodeData,
                    -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetCategory(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetSyllableType(0));
        });
    }

    static MyanmarSyllableMap Recognize(string text)
    {
        return MyanmarSyllableRecognizer.Recognize(
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
        MyanmarSyllableMap map,
        int index,
        int start,
        int length,
        MyanmarSyllableType type)
    {
        Assert.That(map.GetSyllableStart(index), Is.EqualTo(start));
        Assert.That(map.GetSyllableLength(index), Is.EqualTo(length));
        Assert.That(map.GetSyllableEnd(index), Is.EqualTo(start + length));
        Assert.That(map.GetSyllableType(index), Is.EqualTo(type));
    }
}
