using System.Text.RegularExpressions;
using fake;

namespace fake.Tests;

[TestFixture]
public class PatternGeneratorTests
{
    [Test]
    public void FromPatternGeneratesTheDocumentedCoreSubset()
    {
        TextProvider text = Fake.Create(3901).Text();
        const string pattern = @"^INV-[A-Z]{3}-\d{4}-\w[._]\s.$";
        Regex expected = new Regex(@"^INV-[A-Z]{3}-\d{4}-[0-9A-Za-z_][._][ \t\r\n][ -~]$", RegexOptions.CultureInvariant);

        for (int index = 0; index < 100; index++)
        {
            Assert.That(expected.IsMatch(text.FromPattern(pattern)), Is.True);
        }
    }

    [Test]
    public void AlternationAndNestedGroupsSelectWholeBranches()
    {
        TextProvider text = Fake.Create(3902).Text();
        Regex expected = new Regex(@"^(cat|dog|fox)-(red|blue)-(ab|c){2}$", RegexOptions.CultureInvariant);

        for (int index = 0; index < 100; index++)
        {
            Assert.That(expected.IsMatch(text.FromPattern("^(cat|dog|fox)-(red|blue)-(ab|c){2}$")), Is.True);
        }
    }

    [Test]
    public void CharacterClassesSupportRangesDeduplicationAndNegation()
    {
        TextProvider text = Fake.Create(3903).Text();
        Regex expected = new Regex(@"^[A-C][3-5][^A-Za-z0-9]{10}X$", RegexOptions.CultureInvariant);

        for (int index = 0; index < 50; index++)
        {
            string actual = text.FromPattern("^[A-CA][3-5][^A-Za-z0-9]{10}[XX]$");
            Assert.That(expected.IsMatch(actual), Is.True);
        }
    }

    [Test]
    public void ShorthandComplementsStayWithinPrintableAscii()
    {
        TextProvider text = Fake.Create(3904).Text();

        for (int index = 0; index < 100; index++)
        {
            string actual = text.FromPattern(@"^\D\W\S$");

            Assert.That(actual, Has.Length.EqualTo(3));
            Assert.That(actual[0], Is.InRange(' ', '~').And.Not.InRange('0', '9'));
            Assert.That(actual[1], Is.InRange(' ', '~'));
            Assert.That(char.IsLetterOrDigit(actual[1]) || actual[1] == '_', Is.False);
            Assert.That(actual[2], Is.InRange('!', '~'));
        }
    }

    [Test]
    public void EscapesProduceControlCharactersAndLiteralMetacharacters()
    {
        TextProvider text = Fake.Create(3905).Text();
        const string pattern = @"^\(\)\[\]\{\}\.\+\*\?\|\^\$\\\-\t\r\n$";

        string actual = text.FromPattern(pattern);

        Assert.That(actual, Is.EqualTo("()[]{}.+*?|^$\\-\t\r\n"));
    }

    [Test]
    public void UnicodeLiteralScalarsCanBeGroupedAndRepeated()
    {
        TextProvider text = Fake.Create(3906).Text();

        string actual = text.FromPattern("^(🙂){2}東京$");

        Assert.That(actual, Is.EqualTo("🙂🙂東京"));
        Assert.That(text.GraphemeCount(actual), Is.EqualTo(4));
    }

    [Test]
    public void QuantifiersRespectExactClosedAndConfiguredOpenBounds()
    {
        TextProvider text = Fake.Create(3907).Text();
        Regex expected = new Regex("^a{3}b{2,4}c{1,5}$", RegexOptions.CultureInvariant);

        for (int index = 0; index < 100; index++)
        {
            Assert.That(expected.IsMatch(text.FromPatternFromPatternAndMaximumRepetitions("^a{3}b{2,4}c{1,}$", 5)), Is.True);
        }

        for (int index = 0; index < 100; index++)
        {
            string actual = text.FromPatternFromPatternAndMaximumRepetitions("^x?y*z+$", 3);
            Assert.That(Regex.IsMatch(actual, "^x?y{0,3}z{1,3}$"), Is.True);
        }
    }

    [Test]
    public void EmptyPatternsGroupsAndAlternationBranchesAreSupported()
    {
        TextProvider text = Fake.Create(3908).Text();

        Assert.That(text.FromPattern(""), Is.Empty);
        Assert.That(text.FromPattern("()"), Is.Empty);
        for (int index = 0; index < 50; index++)
        {
            Assert.That(text.FromPattern("^(|a)(b|)$"), Does.Match("^(|a)(b|)$"));
        }
    }

    [Test]
    public void FromPatternIsReproducibleForTheSameSeed()
    {
        TextProvider first = Fake.Create(3909).Text();
        TextProvider second = Fake.Create(3909).Text();
        string[] patterns = new[]
        {
            "[A-Z]{2,8}",
            "(red|green|blue)-\\d+",
            "[^A-Z]{4}",
            "(ab|cd|ef){1,5}"
        };

        foreach (string pattern in patterns)
        {
            Assert.That(first.FromPattern(pattern), Is.EqualTo(second.FromPattern(pattern)), pattern);
        }
    }

    [TestCase("(", 0)]
    [TestCase(")", 0)]
    [TestCase("\\", 0)]
    [TestCase("\\q", 0)]
    [TestCase("[]", 0)]
    [TestCase("[abc", 0)]
    [TestCase("[z-a]", 2)]
    [TestCase("(?:a)", 0)]
    [TestCase("*a", 0)]
    [TestCase("a**", 2)]
    [TestCase("a{}", 1)]
    [TestCase("a{3,2}", 1)]
    [TestCase("a{11}", 1)]
    [TestCase("a{2", 1)]
    [TestCase("a^b", 1)]
    [TestCase("a$b", 1)]
    [TestCase("[é]", 1)]
    [TestCase("[a-\\d]", 2)]
    public void InvalidPatternsReportThePatternAndFailingIndex(string pattern, int index)
    {
        TextProvider text = Fake.Create(3910).Text();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => text.FromPattern(pattern));

        Assert.That(exception.ParamName, Is.EqualTo("pattern"));
        Assert.That(exception.Message, Does.Contain($"index {index}"));
    }

    [Test]
    public void FromPatternValidatesNullMalformedTextAndRepetitionConfiguration()
    {
        TextProvider text = Fake.Create(3911).Text();
        string malformed = new string(new[] { 'a', '\uD800' });

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => text.FromPattern(null)).ParamName, Is.EqualTo("pattern"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.FromPattern(malformed)).ParamName, Is.EqualTo("pattern"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.FromPatternFromPatternAndMaximumRepetitions("a", 0)).ParamName, Is.EqualTo("maximumRepetitions"));
        });
    }

    [Test]
    public void ParserAndWorstCaseOutputAreBoundedBeforeGeneration()
    {
        FakeOptions options = FakeOptions.Create(3912L);
        options.MaximumCount = 100;
        options.MaximumOutputSize = 20;
        TextProvider text = Fake.Create(options).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.FromPattern("(ab){10}"), Has.Length.EqualTo(20));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.FromPattern("(ab{5}){4}")).ParamName, Is.EqualTo("pattern"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.FromPatternFromPatternAndMaximumRepetitions("(a|b{21})", 25)).ParamName, Is.EqualTo("pattern"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.FromPatternFromPatternAndMaximumRepetitions("a", 101)).ParamName, Is.EqualTo("maximumRepetitions"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.FromPattern(new string('a', 101))).ParamName, Is.EqualTo("pattern"));
        });
    }

    [Test]
    public void CharacterClassAndNestingComplexityAreBounded()
    {
        FakeOptions options = FakeOptions.Create(3913L);
        options.MaximumCount = 10;
        TextProvider text = Fake.Create(options).Text();
        TextProvider nesting = Fake.Create(3913).Text();
        string deeplyNested = new string('(', 65) + "a" + new string(')', 65);

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentException>(() => text.FromPatternFromPatternAndMaximumRepetitions("[ -~]", 10)).ParamName, Is.EqualTo("pattern"));
            Assert.That(Assert.Throws<ArgumentException>(() => nesting.FromPatternFromPatternAndMaximumRepetitions(deeplyNested, 10)).ParamName, Is.EqualTo("pattern"));
        });
    }

    [Test]
    public void InvalidOversizedAndLiteralPatternsConsumeNoEntropy()
    {
        FakeOptions options = FakeOptions.Create(3914L);
        options.MaximumOutputSize = 20;
        TextProvider actual = Fake.Create(options).Text();
        TextProvider expectedPosition = Fake.Create(options).Text();

        Assert.Throws<ArgumentException>(() => actual.FromPattern("a**"));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.FromPatternFromPatternAndMaximumRepetitions("a{21}", 21));
        actual.FromPattern("literal🙂{2}[X]");

        Assert.That(actual.FromPattern("[A-Z]{10}"), Is.EqualTo(expectedPosition.FromPattern("[A-Z]{10}")));
    }

    [Test]
    public void CryptographicModeSupportsBoundedPatternGeneration()
    {
        FakeOptions options = FakeOptions.Create(3915L);
        options.RandomMode = RandomMode.Cryptographic;
        TextProvider text = Fake.Create(options).Text();

        string actual = text.FromPattern("^(red|green|blue)-[A-Z]{4}-\\d{6}$");

        Assert.That(actual, Does.Match("^(red|green|blue)-[A-Z]{4}-[0-9]{6}$"));
    }
}
