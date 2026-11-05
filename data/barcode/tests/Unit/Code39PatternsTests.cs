namespace barcode.Tests;

public class Code39PatternsTests
{
    static readonly string[] ExpectedWideNarrowPatterns = new string[]
    {
        "nnnwwnwnn",
        "wnnwnnnnw",
        "nnwwnnnnw",
        "wnwwnnnnn",
        "nnnwwnnnw",
        "wnnwwnnnn",
        "nnwwwnnnn",
        "nnnwnnwnw",
        "wnnwnnwnn",
        "nnwwnnwnn",
        "wnnnnwnnw",
        "nnwnnwnnw",
        "wnwnnwnnn",
        "nnnnwwnnw",
        "wnnnwwnnn",
        "nnwnwwnnn",
        "nnnnnwwnw",
        "wnnnnwwnn",
        "nnwnnwwnn",
        "nnnnwwwnn",
        "wnnnnnnww",
        "nnwnnnnww",
        "wnwnnnnwn",
        "nnnnwnnww",
        "wnnnwnnwn",
        "nnwnwnnwn",
        "nnnnnnwww",
        "wnnnnnwwn",
        "nnwnnnwwn",
        "nnnnwnwwn",
        "wwnnnnnnw",
        "nwwnnnnnw",
        "wwwnnnnnn",
        "nwnnwnnnw",
        "wwnnwnnnn",
        "nwwnwnnnn",
        "nwnnnnwnw",
        "wwnnnnwnn",
        "nwwnnnwnn",
        "nwnwnwnnn",
        "nwnwnnnwn",
        "nwnnnwnwn",
        "nnnwnwnwn",
        "nwnnwnwnn"
    };

    [Test]
    public void CompleteTableMatchesIndependentWideNarrowOracle()
    {
        string completeCharacters = Code39Patterns.Alphabet + Code39Patterns.StartStopCharacter;

        Assert.That(ExpectedWideNarrowPatterns, Has.Length.EqualTo(44));
        Assert.That(completeCharacters, Has.Length.EqualTo(44));
        int index = 0;
        while (index < completeCharacters.Length)
        {
            string actual = ToWideNarrowString(
                Code39Patterns.GetWideElements(completeCharacters[index]));
            Assert.That(
                actual,
                Is.EqualTo(ExpectedWideNarrowPatterns[index]),
                $"Character {FormatCharacter(completeCharacters[index])}");
            index++;
        }
    }

    [Test]
    public void EveryPatternHasNineElementsExactlyThreeWideAndUniqueAssignment()
    {
        string completeCharacters = Code39Patterns.Alphabet + Code39Patterns.StartStopCharacter;
        HashSet<int> observedPatterns = new HashSet<int>();
        int index = 0;
        while (index < completeCharacters.Length)
        {
            char character = completeCharacters[index];
            bool[] elements = Code39Patterns.GetWideElements(character);
            int[] widths = Code39Patterns.GetCanonicalWidths(character);
            int wideCount = elements.Count(value => value);

            Assert.Multiple(() =>
            {
                Assert.That(elements, Has.Length.EqualTo(9));
                Assert.That(widths, Has.Length.EqualTo(9));
                Assert.That(wideCount, Is.EqualTo(3), $"Character {FormatCharacter(character)}");
                Assert.That(widths.Count(width => width == 2), Is.EqualTo(3));
                Assert.That(widths.Count(width => width == 1), Is.EqualTo(6));
                Assert.That(widths.Sum(), Is.EqualTo(12));
                Assert.That(observedPatterns.Add(Code39Patterns.GetCharacterPatternBits(character)),
                    Is.True, $"Character {FormatCharacter(character)}");
            });
            index++;
        }
    }

    [Test]
    public void ValueAndPatternMappingsRoundTripAllDataCharacters()
    {
        int value = 0;
        while (value < Code39Patterns.DataCharacterCount)
        {
            char character = Code39Patterns.GetCharacter(value);
            int pattern = Code39Patterns.GetDataPatternBits(value);

            Assert.Multiple(() =>
            {
                Assert.That(character, Is.EqualTo(Code39Patterns.Alphabet[value]));
                Assert.That(Code39Patterns.GetValue(character), Is.EqualTo(value));
                Assert.That(Code39Patterns.GetCharacterPatternBits(character), Is.EqualTo(pattern));
                Assert.That(Code39Patterns.FindCharacterForPatternBits(pattern), Is.EqualTo(character));
            });
            value++;
        }
    }

    [TestCase('0', 0, 0x034)]
    [TestCase('9', 9, 0x064)]
    [TestCase('A', 10, 0x109)]
    [TestCase('Z', 35, 0x0D0)]
    [TestCase('-', 36, 0x085)]
    [TestCase('.', 37, 0x184)]
    [TestCase(' ', 38, 0x0C4)]
    [TestCase('$', 39, 0x0A8)]
    [TestCase('/', 40, 0x0A2)]
    [TestCase('+', 41, 0x08A)]
    [TestCase('%', 42, 0x02A)]
    public void StandardValueOrderAndRepresentativeMasksAreStable(
        char character,
        int expectedValue,
        int expectedPattern)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code39Patterns.GetValue(character), Is.EqualTo(expectedValue));
            Assert.That(Code39Patterns.GetCharacter(expectedValue), Is.EqualTo(character));
            Assert.That(Code39Patterns.GetDataPatternBits(expectedValue), Is.EqualTo(expectedPattern));
        });
    }

    [Test]
    public void AsteriskIsASeparateStartStopAssignment()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code39Patterns.StartStopCharacter, Is.EqualTo('*'));
            Assert.That(Code39Patterns.StartStopPatternBits, Is.EqualTo(0x094));
            Assert.That(Code39Patterns.IsStartStop('*'), Is.True);
            Assert.That(Code39Patterns.IsStandardDataCharacter('*'), Is.False);
            Assert.That(Code39Patterns.GetCharacterPatternBits('*'), Is.EqualTo(0x094));
            Assert.That(Code39Patterns.FindCharacterForPatternBits(0x094), Is.EqualTo('*'));
            Assert.That(ToWideNarrowString(Code39Patterns.GetWideElements('*')), Is.EqualTo("nwnnwnwnn"));
        });
    }

    [Test]
    public void StandardAlphabetValidationAcceptsEveryPermittedCharacter()
    {
        Assert.DoesNotThrow(() => Code39Patterns.ValidateStandardText(Code39Patterns.Alphabet));
        int index = 0;
        while (index < Code39Patterns.Alphabet.Length)
        {
            Assert.That(Code39Patterns.IsStandardDataCharacter(Code39Patterns.Alphabet[index]), Is.True);
            index++;
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("lower")]
    [TestCase("ABC*")]
    [TestCase("ABC_")]
    [TestCase("ABC\t")]
    [TestCase("CAFÉ")]
    public void StandardAlphabetValidationRejectsMissingEmptyAndNonAlphabetText(string? text)
    {
        Assert.That(
            () => Code39Patterns.ValidateStandardText(text!),
            Throws.TypeOf<BarcodeEncodingException>());
    }

    [Test]
    public void ReturnedPatternArraysCannotMutateCanonicalAssignments()
    {
        bool[] wide = Code39Patterns.GetWideElements('A');
        int[] widths = Code39Patterns.GetCanonicalWidths('A');
        wide[0] = false;
        widths[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(ToWideNarrowString(Code39Patterns.GetWideElements('A')), Is.EqualTo("wnnnnwnnw"));
            Assert.That(Code39Patterns.GetCanonicalWidths('A'), Is.EqualTo(new int[] { 2, 1, 1, 1, 1, 2, 1, 1, 2 }));
        });
    }

    [Test]
    public void TableRejectsInvalidValuesCharactersAndUnassignedPatterns()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Code39Patterns.GetCharacter(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Patterns.GetCharacter(43), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Patterns.GetDataPatternBits(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Patterns.GetDataPatternBits(43), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Patterns.GetValue('*'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Patterns.GetValue('a'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Patterns.GetCharacterPatternBits('_'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Patterns.GetCanonicalWidths('_'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Patterns.FindCharacterForPatternBits(0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Patterns.FindCharacterForPatternBits(0x200), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void PatternAlphabetIsSharedWithModulo43ValueAssignment()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code39Patterns.Alphabet, Is.EqualTo("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%"));
            Assert.That(Code39Patterns.Alphabet, Has.Length.EqualTo(43));
            Assert.That(Code39Modulo43.Alphabet, Is.EqualTo(Code39Patterns.Alphabet));
            Assert.That(Code39Patterns.DataCharacterCount, Is.EqualTo(43));
            Assert.That(Code39Patterns.ElementCount, Is.EqualTo(9));
            Assert.That(Code39Patterns.WideElementCount, Is.EqualTo(3));
        });
    }

    static string ToWideNarrowString(bool[] wideElements)
    {
        return new string(wideElements.Select(value => value ? 'w' : 'n').ToArray());
    }

    static string FormatCharacter(char character)
    {
        return character == ' ' ? "<space>" : character.ToString();
    }
}
