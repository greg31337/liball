namespace ttf.Tests;

internal class IndicSyllableRecognizerTests
{
    static UnicodeData UnicodeData = UnicodeData.LoadDefault();

    [Test]
    public void MapsUnicodeIndicCategoriesToShapingCategories()
    {
        IndicSyllableMap map = Recognize(
            "\u0930\u0915\u0905\u093C\u094D\u200C\u200D" +
            "\u093F\u0902\u0951\u25CC\u093D\u00A0",
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetCategory(0),
                Is.EqualTo(IndicSyllabicCategory.Ra));
            Assert.That(map.GetCategory(1),
                Is.EqualTo(IndicSyllabicCategory.Consonant));
            Assert.That(map.GetCategory(2),
                Is.EqualTo(IndicSyllabicCategory.Vowel));
            Assert.That(map.GetCategory(3),
                Is.EqualTo(IndicSyllabicCategory.Nukta));
            Assert.That(map.GetCategory(4),
                Is.EqualTo(IndicSyllabicCategory.Halant));
            Assert.That(map.GetCategory(5),
                Is.EqualTo(IndicSyllabicCategory.NonJoiner));
            Assert.That(map.GetCategory(6),
                Is.EqualTo(IndicSyllabicCategory.Joiner));
            Assert.That(map.GetCategory(7),
                Is.EqualTo(IndicSyllabicCategory.Matra));
            Assert.That(map.GetCategory(8),
                Is.EqualTo(IndicSyllabicCategory.SyllableModifier));
            Assert.That(map.GetCategory(9),
                Is.EqualTo(IndicSyllabicCategory.Accent));
            Assert.That(map.GetCategory(10),
                Is.EqualTo(IndicSyllabicCategory.DottedCircle));
            Assert.That(map.GetCategory(11),
                Is.EqualTo(IndicSyllabicCategory.Symbol));
            Assert.That(map.GetCategory(12),
                Is.EqualTo(IndicSyllabicCategory.Placeholder));
        });
    }

    [Test]
    public void MapsIndicPositionalCategoriesWithoutLosingCompoundValues()
    {
        IndicSyllableMap map = Recognize(
            "\u093F\u0947\u0941\u093E\u0AC9",
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetPosition(0),
                Is.EqualTo(IndicPositionalCategory.Left));
            Assert.That(map.GetPosition(1),
                Is.EqualTo(IndicPositionalCategory.Top));
            Assert.That(map.GetPosition(2),
                Is.EqualTo(IndicPositionalCategory.Bottom));
            Assert.That(map.GetPosition(3),
                Is.EqualTo(IndicPositionalCategory.Right));
            Assert.That(map.GetPosition(4),
                Is.EqualTo(IndicPositionalCategory.TopAndRight));
        });
    }

    [TestCase("\u09BF", IndicPositionalCategory.Left)]
    [TestCase("\u0A3F", IndicPositionalCategory.Left)]
    [TestCase("\u0ABF", IndicPositionalCategory.Left)]
    [TestCase("\u0B3F", IndicPositionalCategory.Top)]
    [TestCase("\u0BC6", IndicPositionalCategory.Left)]
    [TestCase("\u0C46", IndicPositionalCategory.Top)]
    [TestCase("\u0CC6", IndicPositionalCategory.Top)]
    [TestCase("\u0D46", IndicPositionalCategory.Left)]
    public void PreservesPositionsAcrossIndicScripts(
        string text,
        IndicPositionalCategory expected)
    {
        IndicSyllableMap map = Recognize(
            text,
            IndicShapingModel.Version1);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetCategory(0),
                Is.EqualTo(IndicSyllabicCategory.Matra));
            Assert.That(map.GetPosition(0),
                Is.EqualTo(expected));
        });
    }

    [TestCase("\u0915", 1)]
    [TestCase("\u0915\u094D\u0937\u093F", 4)]
    [TestCase("\u0915\u094D\u200D\u0937\u093F", 5)]
    public void RecognizesConsonantSyllables(string text, int length)
    {
        ShapingBuffer buffer = CreateBuffer(text);
        IndicSyllableMap map = IndicSyllableRecognizer.Recognize(
            buffer,
            UnicodeData,
            IndicShapingModel.Version2,
            10);

        Assert.Multiple(() =>
        {
            AssertSyllable(map, 0, 0, length,
                IndicSyllableType.Consonant);
            Assert.That(map.GetRuneSyllableIndex(length - 1), Is.Zero);
            Assert.That(buffer.GetSyllable(0),
                Is.EqualTo(buffer.GetSyllable(length - 1)));
            Assert.That(buffer.GetSyllable(0) & 15,
                Is.EqualTo((int)IndicSyllableType.Consonant));
            Assert.That(map.HasBrokenSyllable(), Is.False);
        });
    }

    [Test]
    public void UsesTheLongerVowelTailWhenJoinerStartsAHalantGroup()
    {
        IndicSyllableMap map = Recognize(
            "\u0905\u200D\u094D\u0915",
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetSyllableCount(), Is.EqualTo(1));
            AssertSyllable(map, 0, 0, 4, IndicSyllableType.Vowel);
        });
    }

    [TestCase("\u0930\u094D\u0905", IndicSyllableType.Vowel)]
    [TestCase("\u0930\u094D\u25CC", IndicSyllableType.Standalone)]
    [TestCase("\u0930\u094D\u093F", IndicSyllableType.Broken)]
    public void UsesTheLongestSyllableAlternative(
        string text,
        IndicSyllableType expected)
    {
        IndicSyllableMap map = Recognize(
            text,
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetSyllableCount(), Is.EqualTo(1));
            AssertSyllable(map, 0, 0, 3, expected);
        });
    }

    [Test]
    public void RecognizesVowelStandaloneAndSymbolClusters()
    {
        IndicSyllableMap vowels = Recognize(
            "\u0905\u0902",
            IndicShapingModel.Version2);
        IndicSyllableMap standalone = Recognize(
            "\u25CC\u093F",
            IndicShapingModel.Version2);
        IndicSyllableMap symbols = Recognize(
            "\u093D\u0951",
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            AssertSyllable(vowels, 0, 0, 2,
                IndicSyllableType.Vowel);
            AssertSyllable(standalone, 0, 0, 2,
                IndicSyllableType.Standalone);
            AssertSyllable(symbols, 0, 0, 2,
                IndicSyllableType.Symbol);
        });
    }

    [TestCase("\u093F\u0902", 2)]
    [TestCase("\u094D\u200C", 2)]
    [TestCase("\u0902", 1)]
    public void DetectsBrokenClusters(string text, int length)
    {
        ShapingBuffer buffer = CreateBuffer(text);
        IndicSyllableMap map = IndicSyllableRecognizer.Recognize(
            buffer,
            UnicodeData,
            IndicShapingModel.Version2,
            10);

        Assert.Multiple(() =>
        {
            AssertSyllable(map, 0, 0, length,
                IndicSyllableType.Broken);
            Assert.That(map.HasBrokenSyllable(), Is.True);
            Assert.That(buffer.GetSyllable(0) & 15,
                Is.EqualTo((int)IndicSyllableType.Broken));
        });
    }

    [Test]
    public void TreatsAnIsolatedPostSyllableModifierAsNonIndic()
    {
        IndicSyllableMap map = Recognize(
            "\u00B2",
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetCategory(0),
                Is.EqualTo(IndicSyllabicCategory.SyllableModifierPost));
            AssertSyllable(map, 0, 0, 1, IndicSyllableType.NonIndic);
            Assert.That(map.HasBrokenSyllable(), Is.False);
        });
    }

    [TestCase("1\u093F")]
    [TestCase("\u00A0\u093F")]
    [TestCase("\u2015\u093F")]
    [TestCase("\u25FB\u093F")]
    public void RecognizesPlaceholdersAsStandaloneClusters(string text)
    {
        IndicSyllableMap map = Recognize(
            text,
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetCategory(0),
                Is.EqualTo(IndicSyllabicCategory.Placeholder));
            AssertSyllable(map, 0, 0, 2,
                IndicSyllableType.Standalone);
            Assert.That(map.HasBrokenSyllable(), Is.False);
        });
    }

    [Test]
    public void SeparatesBrokenAndValidSyllables()
    {
        IndicSyllableMap map = Recognize(
            "\u093F\u0915\u0905A",
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetSyllableCount(), Is.EqualTo(4));
            AssertSyllable(map, 0, 0, 1, IndicSyllableType.Broken);
            AssertSyllable(map, 1, 1, 1, IndicSyllableType.Consonant);
            AssertSyllable(map, 2, 2, 1, IndicSyllableType.Vowel);
            AssertSyllable(map, 3, 3, 1, IndicSyllableType.NonIndic);
            Assert.That(map.GetRuneSyllableIndex(0), Is.Zero);
            Assert.That(map.GetRuneSyllableIndex(1), Is.EqualTo(1));
            Assert.That(map.GetRuneSyllableIndex(2), Is.EqualTo(2));
            Assert.That(map.GetRuneSyllableIndex(3), Is.EqualTo(3));
        });
    }

    [Test]
    public void TreatsEachNonIndicRuneAsAStableCluster()
    {
        IndicSyllableMap map = Recognize(
            "A !",
            IndicShapingModel.Version1);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetSyllableCount(), Is.EqualTo(3));
            AssertSyllable(map, 0, 0, 1, IndicSyllableType.NonIndic);
            AssertSyllable(map, 1, 1, 1, IndicSyllableType.NonIndic);
            AssertSyllable(map, 2, 2, 1, IndicSyllableType.NonIndic);
            Assert.That(map.HasBrokenSyllable(), Is.False);
        });
    }

    [Test]
    public void UsesIdenticalRecognitionForVersion1AndVersion2()
    {
        string text = "\u0930\u094D\u0915\u094D\u0937\u093F" +
            "\u0905\u0902\u093F";
        IndicSyllableMap version1 = Recognize(
            text,
            IndicShapingModel.Version1);
        IndicSyllableMap version2 = Recognize(
            text,
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(version1.GetModel(),
                Is.EqualTo(IndicShapingModel.Version1));
            Assert.That(version2.GetModel(),
                Is.EqualTo(IndicShapingModel.Version2));
            Assert.That(version1.GetSyllableCount(),
                Is.EqualTo(version2.GetSyllableCount()));
            int index = 0;
            while (index < version1.GetSyllableCount())
            {
                Assert.That(version1.GetSyllableStart(index),
                    Is.EqualTo(version2.GetSyllableStart(index)));
                Assert.That(version1.GetSyllableLength(index),
                    Is.EqualTo(version2.GetSyllableLength(index)));
                Assert.That(version1.GetSyllableType(index),
                    Is.EqualTo(version2.GetSyllableType(index)));
                index++;
            }
        });
    }

    [Test]
    public void WrapsEncodedSyllableSerialWithoutLosingStableIndexes()
    {
        ShapingBuffer buffer = CreateBuffer("ABCDEFGHIJKLMNOP");
        IndicSyllableMap map = IndicSyllableRecognizer.Recognize(
            buffer,
            UnicodeData,
            IndicShapingModel.Version2,
            16);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetSyllableCount(), Is.EqualTo(16));
            Assert.That(map.GetRuneSyllableIndex(15), Is.EqualTo(15));
            Assert.That(buffer.GetSyllable(0),
                Is.EqualTo(buffer.GetSyllable(15)));
            Assert.That(buffer.GetSyllable(14),
                Is.Not.EqualTo(buffer.GetSyllable(15)));
        });
    }

    [Test]
    public void SupportsEmptyInputAndEnforcesLimitsAndArguments()
    {
        ShapingBuffer emptyBuffer = CreateBuffer("");
        IndicSyllableMap empty = IndicSyllableRecognizer.Recognize(
            emptyBuffer,
            UnicodeData,
            IndicShapingModel.Version2,
            0);
        ShapingBuffer buffer = CreateBuffer("\u0915");
        ShapingBuffer partial = CreateBuffer("AB");

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetRuneCount(), Is.Zero);
            Assert.That(empty.GetSyllableCount(), Is.Zero);
            Assert.That(empty.HasBrokenSyllable(), Is.False);
            Assert.Throws<FontLimitException>(() =>
                IndicSyllableRecognizer.Recognize(
                    buffer,
                    UnicodeData,
                    IndicShapingModel.Version2,
                    0));
            Assert.Throws<FontLimitException>(() =>
                IndicSyllableRecognizer.Recognize(
                    partial,
                    UnicodeData,
                    IndicShapingModel.Version2,
                    1));
            Assert.That(partial.GetSyllable(0), Is.Zero);
            Assert.That(partial.GetSyllable(1), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                IndicSyllableRecognizer.Recognize(
                    null!,
                    UnicodeData,
                    IndicShapingModel.Version2,
                    1));
            Assert.Throws<ArgumentNullException>(() =>
                IndicSyllableRecognizer.Recognize(
                    buffer,
                    null!,
                    IndicShapingModel.Version2,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                IndicSyllableRecognizer.Recognize(
                    buffer,
                    UnicodeData,
                    (IndicShapingModel)2,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                IndicSyllableRecognizer.Recognize(
                    buffer,
                    UnicodeData,
                    IndicShapingModel.Version2,
                    -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetCategory(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetPosition(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetRuneSyllableIndex(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetSyllableType(0));
        });
    }

    static IndicSyllableMap Recognize(
        string text,
        IndicShapingModel model)
    {
        return IndicSyllableRecognizer.Recognize(
            CreateBuffer(text),
            UnicodeData,
            model,
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
        IndicSyllableMap map,
        int index,
        int start,
        int length,
        IndicSyllableType type)
    {
        Assert.That(map.GetSyllableStart(index), Is.EqualTo(start));
        Assert.That(map.GetSyllableLength(index), Is.EqualTo(length));
        Assert.That(map.GetSyllableEnd(index), Is.EqualTo(start + length));
        Assert.That(map.GetSyllableType(index), Is.EqualTo(type));
    }
}
