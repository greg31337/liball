using fake;

namespace fake.Tests;

[TestFixture]
public class CharacterStringTests
{
    [Test]
    public void BuiltInAsciiStringsAreDeterministicAndUseExactAlphabets()
    {
        PrimitiveProvider first = Fake.Create(3301).Primitive();
        PrimitiveProvider second = Fake.Create(3301).Primitive();
        string mixed = first.Alpha(20000);
        string upper = first.AlphaFromCountAndUppercase(20000, true);
        string lower = first.AlphaFromCountAndUppercase(20000, false);
        string alphaNumeric = first.AlphaNumeric(20000);
        string symbols = first.Symbol(20000);
        string whitespace = first.Whitespace(5000);
        string printable = first.Printable(30000);

        Assert.Multiple(() =>
        {
            Assert.That(mixed, Is.EqualTo(second.Alpha(20000)));
            Assert.That(upper, Is.EqualTo(second.AlphaFromCountAndUppercase(20000, true)));
            Assert.That(lower, Is.EqualTo(second.AlphaFromCountAndUppercase(20000, false)));
            Assert.That(alphaNumeric, Is.EqualTo(second.AlphaNumeric(20000)));
            Assert.That(symbols, Is.EqualTo(second.Symbol(20000)));
            Assert.That(whitespace, Is.EqualTo(second.Whitespace(5000)));
            Assert.That(printable, Is.EqualTo(second.Printable(30000)));
        });

        AssertAlphabet(mixed, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        AssertAlphabet(upper, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        AssertAlphabet(lower, "abcdefghijklmnopqrstuvwxyz");
        AssertAlphabet(alphaNumeric, "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        AssertAlphabet(symbols, Symbols());
        AssertAlphabet(whitespace, "\t\n\v\f\r ");
        AssertAlphabet(printable, PrintableAscii());
    }

    [Test]
    public void BuiltInSpanWritersMatchAllocatedGeneration()
    {
        PrimitiveProvider first = Fake.Create(3302).Primitive();
        PrimitiveProvider second = Fake.Create(3302).Primitive();
        char[] mixed = new char[80];
        char[] upper = new char[80];
        char[] lower = new char[80];
        char[] alphaNumeric = new char[80];
        char[] symbols = new char[80];
        char[] whitespace = new char[80];
        char[] printable = new char[80];

        Assert.Multiple(() =>
        {
            Assert.That(first.WriteAlpha(mixed.AsSpan()), Is.EqualTo(80));
            Assert.That(first.WriteAlphaFromDestinationAndUppercase(upper.AsSpan(), true), Is.EqualTo(80));
            Assert.That(first.WriteAlphaFromDestinationAndUppercase(lower.AsSpan(), false), Is.EqualTo(80));
            Assert.That(first.WriteAlphaNumeric(alphaNumeric.AsSpan()), Is.EqualTo(80));
            Assert.That(first.WriteSymbol(symbols.AsSpan()), Is.EqualTo(80));
            Assert.That(first.WriteWhitespace(whitespace.AsSpan()), Is.EqualTo(80));
            Assert.That(first.WritePrintable(printable.AsSpan()), Is.EqualTo(80));
        });

        Assert.Multiple(() =>
        {
            Assert.That(new string(mixed), Is.EqualTo(second.Alpha(80)));
            Assert.That(new string(upper), Is.EqualTo(second.AlphaFromCountAndUppercase(80, true)));
            Assert.That(new string(lower), Is.EqualTo(second.AlphaFromCountAndUppercase(80, false)));
            Assert.That(new string(alphaNumeric), Is.EqualTo(second.AlphaNumeric(80)));
            Assert.That(new string(symbols), Is.EqualTo(second.Symbol(80)));
            Assert.That(new string(whitespace), Is.EqualTo(second.Whitespace(80)));
            Assert.That(new string(printable), Is.EqualTo(second.Printable(80)));
        });
    }

    [Test]
    public void CustomAlphabetSupportsDistinctBmpCharactersWithReplacement()
    {
        PrimitiveProvider first = Fake.Create(3303).Primitive();
        PrimitiveProvider second = Fake.Create(3303).Primitive();
        const string alphabet = "AéΩ中";

        string actual = first.FromCharacters(4000, alphabet);

        Assert.That(actual, Is.EqualTo(second.FromCharacters(4000, alphabet)));
        AssertAlphabet(actual, alphabet);
    }

    [Test]
    public void SingleCharacterAlphabetCanRepeatWithReplacement()
    {
        PrimitiveProvider primitive = Fake.Create(3304).Primitive();

        Assert.That(primitive.FromCharacters(100, "x"), Is.EqualTo(new string('x', 100)));
    }

    [Test]
    public void WithoutReplacementProducesUniformlyShuffledDistinctCharacters()
    {
        PrimitiveProvider first = Fake.Create(3305).Primitive();
        PrimitiveProvider second = Fake.Create(3305).Primitive();
        const string alphabet = "0123456789ABCDEF";

        string actual = first.FromCharactersFromCountAndAlphabetAndWithReplacement(alphabet.Length, alphabet, false);

        Assert.That(actual, Is.EqualTo(second.FromCharactersFromCountAndAlphabetAndWithReplacement(alphabet.Length, alphabet, false)));
        Assert.That(actual.Distinct().Count(), Is.EqualTo(alphabet.Length));
        Assert.That(actual.Order(), Is.EqualTo(alphabet.Order()));
        Assert.That(actual, Is.Not.EqualTo(alphabet));
    }

    [Test]
    public void CustomAlphabetSpanWritersSupportBothReplacementModes()
    {
        PrimitiveProvider first = Fake.Create(3306).Primitive();
        PrimitiveProvider second = Fake.Create(3306).Primitive();
        const string alphabet = "abcdefghij";
        char[] repeated = new char[100];
        char[] unique = new char[8];

        Assert.That(first.WriteFromCharacters(repeated.AsSpan(), alphabet), Is.EqualTo(100));
        Assert.That(new string(repeated), Is.EqualTo(second.FromCharacters(100, alphabet)));

        Assert.That(first.WriteFromCharactersFromDestinationAndAlphabetAndWithReplacement(unique.AsSpan(), alphabet, false), Is.EqualTo(8));
        Assert.That(new string(unique), Is.EqualTo(second.FromCharactersFromCountAndAlphabetAndWithReplacement(8, alphabet, false)));
        Assert.That(unique.Distinct().Count(), Is.EqualTo(unique.Length));
    }

    [Test]
    public void CustomAlphabetAllowsDistinctWhitespaceAndCaseVariants()
    {
        PrimitiveProvider primitive = Fake.Create(3307).Primitive();
        const string alphabet = " \t\r\naA";

        string actual = primitive.FromCharacters(1000, alphabet);

        AssertAlphabet(actual, alphabet);
    }

    [Test]
    public void CustomAlphabetRejectsNullEmptyDuplicateAndSurrogateInputs()
    {
        PrimitiveProvider actual = Fake.Create(3308).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3308).Primitive();
        char[] destination = new char[3];

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => actual.FromCharacters(1, null)).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.FromCharacters(1, "")).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.FromCharacters(1, "abca")).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.FromCharacters(1, "a\ud800b")).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.FromCharacters(1, "a\udc00b")).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.FromCharacters(1, "a😀b")).ParamName, Is.EqualTo("alphabet"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.WriteFromCharacters(destination.AsSpan(), "abb")).ParamName, Is.EqualTo("alphabet"));
        });

        Assert.That(destination, Has.All.EqualTo('\0'));
        Assert.That(actual.Alpha(50), Is.EqualTo(expectedPosition.Alpha(50)));
    }

    [Test]
    public void InsufficientAlphabetFailsAtomicallyWithoutReplacement()
    {
        PrimitiveProvider actual = Fake.Create(3309).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3309).Primitive();
        char[] destination = new char[] { '#', '#', '#', '#' };

        ArgumentOutOfRangeException stringException = Assert.Throws<ArgumentOutOfRangeException>(() => actual.FromCharactersFromCountAndAlphabetAndWithReplacement(4, "abc", false));
        ArgumentOutOfRangeException spanException = Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteFromCharactersFromDestinationAndAlphabetAndWithReplacement(destination.AsSpan(), "abc", false));

        Assert.That(stringException.ParamName, Is.EqualTo("count"));
        Assert.That(spanException.ParamName, Is.EqualTo("count"));
        Assert.That(destination, Is.EqualTo(new char[] { '#', '#', '#', '#' }));
        Assert.That(actual.AlphaNumeric(50), Is.EqualTo(expectedPosition.AlphaNumeric(50)));
    }

    [Test]
    public void StringMethodsRejectNegativeCountsBeforeEntropy()
    {
        PrimitiveProvider actual = Fake.Create(3310).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3310).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Alpha(-1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.AlphaNumeric(-1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Symbol(-1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Whitespace(-1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Printable(-1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.FromCharacters(-1, "abc")).ParamName, Is.EqualTo("count"));
        });

        Assert.That(actual.Alpha(50), Is.EqualTo(expectedPosition.Alpha(50)));
    }

    [Test]
    public void CharacterStringsHonorConfiguredOutputLimitBeforeEntropy()
    {
        FakeOptions options = FakeOptions.Create(3311L);
        options.MaximumOutputSize = 5;
        PrimitiveProvider actual = Fake.Create(options).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(options).Primitive();
        char[] destination = new char[6];

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Alpha(6)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.AlphaNumeric(6)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Symbol(6)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Whitespace(6)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Printable(6)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.FromCharacters(6, "abc")).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteAlpha(destination.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteAlphaNumeric(destination.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteSymbol(destination.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteWhitespace(destination.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WritePrintable(destination.AsSpan())).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WriteFromCharacters(destination.AsSpan(), "abc")).ParamName, Is.EqualTo("destination"));
        });

        Assert.That(destination, Has.All.EqualTo('\0'));
        Assert.That(actual.Alpha(5), Is.EqualTo(expectedPosition.Alpha(5)));
    }

    [Test]
    public void CustomAlphabetHonorsConfiguredInputLimitBeforeEntropy()
    {
        FakeOptions options = FakeOptions.Create(3312L);
        options.MaximumCount = 3;
        PrimitiveProvider actual = Fake.Create(options).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(options).Primitive();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => actual.FromCharacters(1, "abcd"));

        Assert.That(exception.ParamName, Is.EqualTo("alphabet"));
        Assert.That(actual.Alpha(20), Is.EqualTo(expectedPosition.Alpha(20)));
    }

    [Test]
    public void EmptyCharacterOperationsDoNotConsumeEntropy()
    {
        PrimitiveProvider actual = Fake.Create(3313).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3313).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Alpha(0), Is.Empty);
            Assert.That(actual.AlphaFromCountAndUppercase(0, true), Is.Empty);
            Assert.That(actual.AlphaNumeric(0), Is.Empty);
            Assert.That(actual.Symbol(0), Is.Empty);
            Assert.That(actual.Whitespace(0), Is.Empty);
            Assert.That(actual.Printable(0), Is.Empty);
            Assert.That(actual.FromCharacters(0, "abc"), Is.Empty);
            Assert.That(actual.FromCharactersFromCountAndAlphabetAndWithReplacement(0, "abc", false), Is.Empty);
            Assert.That(actual.WriteAlpha(Span<char>.Empty), Is.Zero);
            Assert.That(actual.WriteAlphaNumeric(Span<char>.Empty), Is.Zero);
            Assert.That(actual.WriteSymbol(Span<char>.Empty), Is.Zero);
            Assert.That(actual.WriteWhitespace(Span<char>.Empty), Is.Zero);
            Assert.That(actual.WritePrintable(Span<char>.Empty), Is.Zero);
            Assert.That(actual.WriteFromCharacters(Span<char>.Empty, "abc"), Is.Zero);
            Assert.That(actual.WriteFromCharactersFromDestinationAndAlphabetAndWithReplacement(Span<char>.Empty, "abc", false), Is.Zero);
        });

        Assert.That(actual.Printable(50), Is.EqualTo(expectedPosition.Printable(50)));
    }

    static void AssertAlphabet(string actual, string alphabet)
    {
        Assert.That(actual.All(alphabet.Contains), Is.True);
        Assert.That(actual.Distinct().Order(), Is.EqualTo(alphabet.Distinct().Order()));
    }

    static string Symbols()
    {
        return new string(Enumerable.Range(33, 94).Select(value => (char)value).Where(character => !char.IsLetterOrDigit(character)).ToArray());
    }

    static string PrintableAscii()
    {
        return new string(Enumerable.Range(32, 95).Select(value => (char)value).ToArray());
    }
}
