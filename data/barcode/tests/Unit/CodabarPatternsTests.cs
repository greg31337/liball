namespace barcode.Tests;

public class CodabarPatternsTests
{
    static readonly string[] ExpectedWideNarrowPatterns = new string[]
    {
        "nnnnnww",
        "nnnnwwn",
        "nnnwnnw",
        "wwnnnnn",
        "nnwnnwn",
        "wnnnnwn",
        "nwnnnnw",
        "nwnnwnn",
        "nwwnnnn",
        "wnnwnnn",
        "nnnwwnn",
        "nnwwnnn",
        "wnnnwnw",
        "wnwnnnw",
        "wnwnwnn",
        "nnwnwnw",
        "nnwwnwn",
        "nwnwnnw",
        "nnnwnww",
        "nnnwwwn"
    };

    [Test]
    public void CompleteTableMatchesIndependentSevenElementOracle()
    {
        Assert.That(ExpectedWideNarrowPatterns, Has.Length.EqualTo(20));
        Assert.That(CodabarPatterns.Alphabet, Has.Length.EqualTo(20));
        int index = 0;
        while (index < CodabarPatterns.Alphabet.Length)
        {
            char character = CodabarPatterns.Alphabet[index];
            string actual = ToWideNarrowString(CodabarPatterns.GetWideElements(character));
            Assert.That(actual, Is.EqualTo(ExpectedWideNarrowPatterns[index]), $"Character {character}");
            index++;
        }
    }

    [Test]
    public void EveryPatternHasSevenElementsExpectedWideCountAndUniqueAssignment()
    {
        HashSet<int> patterns = new HashSet<int>();
        int index = 0;
        while (index < CodabarPatterns.Alphabet.Length)
        {
            char character = CodabarPatterns.Alphabet[index];
            bool[] elements = CodabarPatterns.GetWideElements(character);
            int[] widths = CodabarPatterns.GetCanonicalWidths(character);
            int expectedWideCount = index < 12 ? 2 : 3;

            Assert.Multiple(() =>
            {
                Assert.That(elements, Has.Length.EqualTo(7));
                Assert.That(elements.Count(value => value), Is.EqualTo(expectedWideCount));
                Assert.That(widths, Has.Length.EqualTo(7));
                Assert.That(widths.Count(width => width == 2), Is.EqualTo(expectedWideCount));
                Assert.That(widths.Count(width => width == 1), Is.EqualTo(7 - expectedWideCount));
                Assert.That(widths.Sum(), Is.EqualTo(7 + expectedWideCount));
                Assert.That(patterns.Add(CodabarPatterns.GetCharacterPatternBits(character)), Is.True);
            });
            index++;
        }

        Assert.That(patterns, Has.Count.EqualTo(20));
    }

    [Test]
    public void ValuesPatternsAndCharactersRoundTripEveryAssignment()
    {
        int value = 0;
        while (value < CodabarPatterns.CharacterCount)
        {
            char character = CodabarPatterns.GetCharacter(value);
            int pattern = CodabarPatterns.GetCharacterPatternBits(character);

            Assert.Multiple(() =>
            {
                Assert.That(character, Is.EqualTo(CodabarPatterns.Alphabet[value]));
                Assert.That(CodabarPatterns.GetValue(character), Is.EqualTo(value));
                Assert.That(CodabarPatterns.FindCharacterForPatternBits(pattern), Is.EqualTo(character));
            });
            value++;
        }
    }

    [TestCase('0', 0, 0x03)]
    [TestCase('3', 3, 0x60)]
    [TestCase('9', 9, 0x48)]
    [TestCase('-', 10, 0x0C)]
    [TestCase('$', 11, 0x18)]
    [TestCase(':', 12, 0x45)]
    [TestCase('/', 13, 0x51)]
    [TestCase('.', 14, 0x54)]
    [TestCase('+', 15, 0x15)]
    [TestCase('A', 16, 0x1A)]
    [TestCase('B', 17, 0x29)]
    [TestCase('C', 18, 0x0B)]
    [TestCase('D', 19, 0x0E)]
    public void StandardValueOrderAndMasksAreStable(
        char character,
        int expectedValue,
        int expectedPattern)
    {
        Assert.Multiple(() =>
        {
            Assert.That(CodabarPatterns.GetValue(character), Is.EqualTo(expectedValue));
            Assert.That(CodabarPatterns.GetCharacter(expectedValue), Is.EqualTo(character));
            Assert.That(CodabarPatterns.GetCharacterPatternBits(character), Is.EqualTo(expectedPattern));
        });
    }

    [TestCase('A', 'T')]
    [TestCase('B', 'N')]
    [TestCase('C', '*')]
    [TestCase('D', 'E')]
    public void CommonTerminalAliasesMapOneToOneToCanonicalPatterns(char canonical, char alias)
    {
        Assert.Multiple(() =>
        {
            Assert.That(CodabarPatterns.IsStartStopCharacter(canonical), Is.True);
            Assert.That(CodabarPatterns.IsStartStopAlias(alias), Is.True);
            Assert.That(CodabarPatterns.IsStartStopInput(alias, true), Is.True);
            Assert.That(CodabarPatterns.IsStartStopInput(alias, false), Is.False);
            Assert.That(CodabarPatterns.NormalizeStartStop(alias, true), Is.EqualTo(canonical));
            Assert.That(CodabarPatterns.NormalizeStartStop(canonical, false), Is.EqualTo(canonical));
            Assert.That(CodabarPatterns.GetStartStopAlias(canonical), Is.EqualTo(alias));
            Assert.That(
                CodabarPatterns.GetInputPatternBits(alias, true),
                Is.EqualTo(CodabarPatterns.GetCharacterPatternBits(canonical)));
        });
    }

    [Test]
    public void DataAlphabetClassificationAndValidationAreStrict()
    {
        Assert.DoesNotThrow(() => CodabarPatterns.ValidateDataText(CodabarPatterns.DataCharacters));
        int index = 0;
        while (index < CodabarPatterns.DataCharacters.Length)
        {
            char character = CodabarPatterns.DataCharacters[index];
            Assert.Multiple(() =>
            {
                Assert.That(CodabarPatterns.IsDataCharacter(character), Is.True);
                Assert.That(CodabarPatterns.IsStartStopCharacter(character), Is.False);
                Assert.That(
                    CodabarPatterns.GetInputPatternBits(character, false),
                    Is.EqualTo(CodabarPatterns.GetCharacterPatternBits(character)));
            });
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(() => CodabarPatterns.ValidateDataText(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CodabarPatterns.ValidateDataText(""), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CodabarPatterns.ValidateDataText("12A3"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => CodabarPatterns.ValidateDataText("12e3"), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void ReturnedArraysCannotMutateCanonicalAssignments()
    {
        bool[] wide = CodabarPatterns.GetWideElements('A');
        int[] widths = CodabarPatterns.GetCanonicalWidths('A');
        wide[0] = true;
        widths[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(ToWideNarrowString(CodabarPatterns.GetWideElements('A')), Is.EqualTo("nnwwnwn"));
            Assert.That(
                CodabarPatterns.GetCanonicalWidths('A'),
                Is.EqualTo(new int[] { 1, 1, 2, 2, 1, 2, 1 }));
        });
    }

    [Test]
    public void ConstantsAgreeWithChecksumAlphabetAndDescribePatternShape()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CodabarPatterns.Alphabet, Is.EqualTo(CodabarModulo16.CharacterValues));
            Assert.That(CodabarPatterns.DataCharacters, Is.EqualTo(CodabarModulo16.DataCharacters));
            Assert.That(CodabarPatterns.StartStopCharacters, Is.EqualTo(CodabarModulo16.StartStopCharacters));
            Assert.That(CodabarPatterns.StartStopAliases, Is.EqualTo("TN*E"));
            Assert.That(CodabarPatterns.CharacterCount, Is.EqualTo(20));
            Assert.That(CodabarPatterns.DataCharacterCount, Is.EqualTo(16));
            Assert.That(CodabarPatterns.StartStopCharacterCount, Is.EqualTo(4));
            Assert.That(CodabarPatterns.ElementCount, Is.EqualTo(7));
            Assert.That(CodabarPatterns.BarElementCount, Is.EqualTo(4));
            Assert.That(CodabarPatterns.SpaceElementCount, Is.EqualTo(3));
            Assert.That(CodabarPatterns.MinimumWideElementCount, Is.EqualTo(2));
            Assert.That(CodabarPatterns.MaximumWideElementCount, Is.EqualTo(3));
            Assert.That(CodabarPatterns.CanonicalNarrowWidth, Is.EqualTo(1));
            Assert.That(CodabarPatterns.CanonicalWideWidth, Is.EqualTo(2));
        });
    }

    [Test]
    public void TablesRejectInvalidValuesCharactersAliasesAndUnassignedPatterns()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => CodabarPatterns.GetCharacter(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.GetCharacter(20), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.GetValue('T'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.GetValue('_'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.GetCharacterPatternBits('T'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.GetCanonicalWidths('_'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.GetInputPatternBits('T', false), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.NormalizeStartStop('T', false), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.NormalizeStartStop('0', true), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.GetStartStopAlias('0'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.FindCharacterForPatternBits(0x00), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarPatterns.FindCharacterForPatternBits(0x7F), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static string ToWideNarrowString(bool[] wideElements)
    {
        return new string(wideElements.Select(value => value ? 'w' : 'n').ToArray());
    }
}
