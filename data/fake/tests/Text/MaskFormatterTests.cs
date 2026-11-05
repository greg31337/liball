using fake;

namespace fake.Tests;

[TestFixture]
public class MaskFormatterTests
{
    [Test]
    public void FormatReplacesEachDocumentedTokenAndPreservesLiterals()
    {
        TextProvider text = Fake.Create(3801).Text();

        string actual = text.Format("ID-????-####-****/@[]{}:東京🙂");

        Assert.That(actual, Does.Match("^ID-[A-Za-z]{4}-[0-9]{4}-[A-Za-z0-9]{4}/@\\[\\]\\{\\}:東京🙂$"));
    }

    [Test]
    public void UnknownUnescapedCharactersAreAlwaysLiteral()
    {
        TextProvider text = Fake.Create(3802).Text();
        const string mask = "plain $%&()[]{}:;,._~ 東京🙂";

        Assert.That(text.Format(mask), Is.EqualTo(mask));
    }

    [Test]
    public void BackslashEscapesTokensAndItself()
    {
        TextProvider text = Fake.Create(3803).Text();

        string actual = text.Format("\\#\\?\\*\\\\-##");

        Assert.That(actual, Does.Match("^#\\?\\*\\\\-[0-9]{2}$"));
    }

    [Test]
    public void FormatIsReproducibleForTheSameSeedAndCallOrder()
    {
        TextProvider first = Fake.Create(3804).Text();
        TextProvider second = Fake.Create(3804).Text();
        string[] masks = new[] { "##-??-**", "literal", "*#?*#?", "\\#-####" };

        foreach (string mask in masks)
        {
            Assert.That(first.Format(mask), Is.EqualTo(second.Format(mask)), mask);
        }
    }

    [Test]
    public void WriteFormatMatchesAllocationAndRequiresExactCapacity()
    {
        TextProvider allocating = Fake.Create(3805).Text();
        TextProvider writing = Fake.Create(3805).Text();
        const string mask = "A-\\#-??-\\*-\\\\-##";
        string expected = allocating.Format(mask);
        char[] destination = new char[expected.Length];
        char[] wrong = Enumerable.Repeat('x', expected.Length + 1).ToArray();

        int written = writing.WriteFormat(destination, mask);
        ArgumentException exception = Assert.Throws<ArgumentException>(() => writing.WriteFormat(wrong, mask));

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(destination.Length));
            Assert.That(new string(destination), Is.EqualTo(expected));
            Assert.That(exception.ParamName, Is.EqualTo("destination"));
            Assert.That(wrong, Is.All.EqualTo('x'));
        });
    }

    [Test]
    public void EmptyAndUnicodeLiteralMasksRemainWellFormed()
    {
        TextProvider text = Fake.Create(3806).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.Format(""), Is.Empty);
            Assert.That(text.Format("👨‍👩‍👧‍👦-#"), Does.Match("^👨‍👩‍👧‍👦-[0-9]$"));
            Assert.That(text.GraphemeCount(text.Format("e\u0301-?")), Is.EqualTo(3));
        });
    }

    [Test]
    public void InvalidEscapesAndMalformedUtf16AreRejectedPrecisely()
    {
        TextProvider text = Fake.Create(3807).Text();
        string malformed = new string(new[] { '#', '\uD800' });

        ArgumentException trailing = Assert.Throws<ArgumentException>(() => text.Format("abc\\"));
        ArgumentException unknown = Assert.Throws<ArgumentException>(() => text.Format("a\\xb"));
        ArgumentException unicode = Assert.Throws<ArgumentException>(() => text.Format(malformed));

        Assert.Multiple(() =>
        {
            Assert.That(trailing.ParamName, Is.EqualTo("mask"));
            Assert.That(trailing.Message, Does.Contain("index 3"));
            Assert.That(unknown.ParamName, Is.EqualTo("mask"));
            Assert.That(unknown.Message, Does.Contain("\\x").And.Contain("index 1"));
            Assert.That(unicode.ParamName, Is.EqualTo("mask"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => text.Format(null)).ParamName, Is.EqualTo("mask"));
        });
    }

    [Test]
    public void ValidationFailuresAndLiteralMasksConsumeNoEntropy()
    {
        TextProvider actual = Fake.Create(3808).Text();
        TextProvider expectedPosition = Fake.Create(3808).Text();

        Assert.Throws<ArgumentException>(() => actual.Format("\\q"));
        Assert.Throws<ArgumentException>(() => actual.WriteFormat(new char[2], "###"));
        actual.Format("literal @ token-like $ text");
        actual.Format("\\#\\?\\*\\\\");

        Assert.That(actual.Format("####????****"), Is.EqualTo(expectedPosition.Format("####????****")));
    }

    [Test]
    public void FormatHonorsTheEvaluatedOutputLimitAfterEscaping()
    {
        FakeOptions options = FakeOptions.Create(3809L);
        options.MaximumOutputSize = 4;
        TextProvider text = Fake.Create(options).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.Format("\\\\\\#\\?\\*"), Is.EqualTo("\\#?*"));
            Assert.That(text.Format("####"), Does.Match("^[0-9]{4}$"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Format("#####")).ParamName, Is.EqualTo("mask"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.WriteFormat(new char[5], "#####")).ParamName, Is.EqualTo("mask"));
        });
    }

    [Test]
    public void CryptographicModeSupportsMaskReplacement()
    {
        FakeOptions options = FakeOptions.Create(3810L);
        options.RandomMode = RandomMode.Cryptographic;
        TextProvider text = Fake.Create(options).Text();

        Assert.That(text.Format("####-????-********"), Does.Match("^[0-9]{4}-[A-Za-z]{4}-[A-Za-z0-9]{8}$"));
    }
}
