using fake;

namespace fake.Tests;

[TestFixture]
public class UnicodeTextTests
{
    [Test]
    public void GraphemeCountMatchesCuratedUax29Fixtures()
    {
        TextProvider text = Fake.Create(3501).Text();

        foreach (string line in FixtureLines("grapheme-cases.txt"))
        {
            string[] fields = line.Split('|');
            int expected = int.Parse(fields[0]);
            string value = FromScalars(fields[1]);

            Assert.That(text.GraphemeCount(value), Is.EqualTo(expected), fields[2]);
        }
    }

    [Test]
    public void TruncateStopsAtExtendedGraphemeBoundaries()
    {
        TextProvider text = Fake.Create(3502).Text();
        const string combining = "A\u0308";
        const string family = "👨‍👩‍👧‍👦";
        const string flag = "🇺🇸";
        string value = combining + family + flag + "Z";

        Assert.Multiple(() =>
        {
            Assert.That(text.Truncate(value, 0), Is.Empty);
            Assert.That(text.Truncate(value, 1), Is.EqualTo(combining));
            Assert.That(text.Truncate(value, 2), Is.EqualTo(combining + family));
            Assert.That(text.Truncate(value, 3), Is.EqualTo(combining + family + flag));
            Assert.That(text.Truncate(value, 4), Is.SameAs(value));
            Assert.That(text.Truncate(value, 100), Is.SameAs(value));
        });
    }

    [Test]
    public void TruncatePreservesNormalizationIndependentClusterBoundaries()
    {
        TextProvider text = Fake.Create(3503).Text();
        const string composed = "ÅB";
        const string decomposed = "A\u030AB";

        Assert.That(text.GraphemeCount(composed), Is.EqualTo(2));
        Assert.That(text.GraphemeCount(decomposed), Is.EqualTo(2));
        Assert.That(text.Truncate(composed, 1), Is.EqualTo("Å"));
        Assert.That(text.Truncate(decomposed, 1), Is.EqualTo("A\u030A"));
    }

    [Test]
    public void ClampTruncatesOverlongTextAndPadsShortTextByGrapheme()
    {
        TextProvider first = Fake.Create(3504).Text();
        TextProvider second = Fake.Create(3504).Text();
        const string family = "👨‍👩‍👧‍👦";
        string overlong = "A\u0308" + family + "BC";

        string truncated = first.Clamp(overlong, 1, 2);
        string padded = first.Clamp(family, 5, 8);

        Assert.That(truncated, Is.EqualTo("A\u0308" + family));
        Assert.That(first.GraphemeCount(truncated), Is.EqualTo(2));
        Assert.That(padded, Does.StartWith(family));
        Assert.That(first.GraphemeCount(padded), Is.EqualTo(5));
        Assert.That(padded.Substring(family.Length), Does.Match("^[A-Za-z]{4}$"));

        second.Clamp(overlong, 1, 2);
        Assert.That(padded, Is.EqualTo(second.Clamp(family, 5, 8)));
    }

    [Test]
    public void ClampWithinBoundsReturnsOriginalAndConsumesNoEntropy()
    {
        TextProvider actual = Fake.Create(3505).Text();
        TextProvider expectedPosition = Fake.Create(3505).Text();
        string value = "A\u0308BC";

        string clamped = actual.Clamp(value, 2, 4);

        Assert.That(clamped, Is.SameAs(value));
        Assert.That(actual.Clamp("", 20, 20), Is.EqualTo(expectedPosition.Clamp("", 20, 20)));
    }

    [Test]
    public void ClampEmptyTextCanProduceAnExactMinimum()
    {
        TextProvider text = Fake.Create(3506).Text();

        string actual = text.Clamp("", 12, 12);

        Assert.That(actual, Does.Match("^[A-Za-z]{12}$"));
        Assert.That(text.GraphemeCount(actual), Is.EqualTo(12));
    }

    [Test]
    public void ExplicitNormalizationModesMatchCuratedUax15Fixtures()
    {
        TextProvider text = Fake.Create(3507).Text();

        foreach (string line in FixtureLines("normalization-cases.txt"))
        {
            string[] fields = line.Split('|');
            string source = FromScalars(fields[0]);
            string nfc = FromScalars(fields[1]);
            string nfd = FromScalars(fields[2]);
            string nfkc = FromScalars(fields[3]);
            string nfkd = FromScalars(fields[4]);

            Assert.Multiple(() =>
            {
                Assert.That(text.NormalizeFromValueAndMode(source, NormalizationMode.None), Is.EqualTo(source), fields[5]);
                Assert.That(text.NormalizeFromValueAndMode(source, NormalizationMode.Nfc), Is.EqualTo(nfc), fields[5]);
                Assert.That(text.NormalizeFromValueAndMode(source, NormalizationMode.Nfd), Is.EqualTo(nfd), fields[5]);
                Assert.That(text.NormalizeFromValueAndMode(source, NormalizationMode.Nfkc), Is.EqualTo(nfkc), fields[5]);
                Assert.That(text.NormalizeFromValueAndMode(source, NormalizationMode.Nfkd), Is.EqualTo(nfkd), fields[5]);
            });
        }
    }

    [TestCase(NormalizationMode.Nfc, "é")]
    [TestCase(NormalizationMode.Nfd, "e\u0301")]
    [TestCase(NormalizationMode.Nfkc, "é")]
    [TestCase(NormalizationMode.Nfkd, "e\u0301")]
    public void NormalizeUsesCapturedMode(NormalizationMode mode, string expected)
    {
        FakeOptions options = FakeOptions.Create(3508L);
        options.NormalizationMode = mode;
        TextProvider text = Fake.Create(options).Text();

        Assert.That(text.Normalize("e\u0301"), Is.EqualTo(expected));
        Assert.That(text.IsNormalized(expected), Is.True);
    }

    [Test]
    public void NoneNormalizationPreservesTheOriginalReference()
    {
        TextProvider text = Fake.Create(3509).Text();
        string value = new string(new char[] { 'e', '\u0301' });

        Assert.That(text.NormalizeFromValueAndMode(value, NormalizationMode.None), Is.SameAs(value));
        Assert.That(text.IsNormalizedFromValueAndMode(value, NormalizationMode.None), Is.True);
    }

    [Test]
    public void IsNormalizedDistinguishesCanonicalAndCompatibilityForms()
    {
        TextProvider text = Fake.Create(3510).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.IsNormalizedFromValueAndMode("é", NormalizationMode.Nfc), Is.True);
            Assert.That(text.IsNormalizedFromValueAndMode("e\u0301", NormalizationMode.Nfc), Is.False);
            Assert.That(text.IsNormalizedFromValueAndMode("e\u0301", NormalizationMode.Nfd), Is.True);
            Assert.That(text.IsNormalizedFromValueAndMode("ﬁ", NormalizationMode.Nfc), Is.True);
            Assert.That(text.IsNormalizedFromValueAndMode("ﬁ", NormalizationMode.Nfkc), Is.False);
        });
    }

    [Test]
    public void TextOperationsRejectMalformedUtf16()
    {
        TextProvider text = Fake.Create(3511).Text();
        string high = new string(new char[] { 'A', '\uD800' });
        string low = new string(new char[] { '\uDC00', 'B' });

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentException>(() => text.GraphemeCount(high)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.Truncate(low, 1)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.Clamp(high, 1, 2)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.Normalize(low)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.IsNormalized(high)).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void TextOperationsValidateNullBoundsAndModes()
    {
        TextProvider text = Fake.Create(3512).Text();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => text.GraphemeCount(null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => text.Truncate(null, 1)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => text.Clamp(null, 1, 2)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => text.Normalize(null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Truncate("abc", -1)).ParamName, Is.EqualTo("maximumGraphemes"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Clamp("abc", -1, 2)).ParamName, Is.EqualTo("minimumGraphemes"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Clamp("abc", 3, 2)).ParamName, Is.EqualTo("maximumGraphemes"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.NormalizeFromValueAndMode("abc", (NormalizationMode)999)).ParamName, Is.EqualTo("mode"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.IsNormalizedFromValueAndMode("abc", (NormalizationMode)999)).ParamName, Is.EqualTo("mode"));
        });
    }

    [Test]
    public void TextOperationsHonorConfiguredOutputLimit()
    {
        FakeOptions options = FakeOptions.Create(3513L);
        options.MaximumOutputSize = 2;
        TextProvider text = Fake.Create(options).Text();
        const string family = "👨‍👩‍👧‍👦";

        Assert.Multiple(() =>
        {
            Assert.That(text.GraphemeCount(family), Is.EqualTo(1));
            Assert.That(text.Truncate(family, 0), Is.Empty);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Truncate(family, 1)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Clamp("", 3, 3)).ParamName, Is.EqualTo("minimumGraphemes"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.NormalizeFromValueAndMode("ﬃ", NormalizationMode.Nfkd)).ParamName, Is.EqualTo("value"));
            Assert.That(text.NormalizeFromValueAndMode("e\u0301", NormalizationMode.Nfc), Is.EqualTo("é"));
        });
    }

    [Test]
    public void NonPaddingTextOperationsDoNotConsumeTextEntropy()
    {
        TextProvider actual = Fake.Create(3514).Text();
        TextProvider expectedPosition = Fake.Create(3514).Text();

        actual.GraphemeCount("A\u0308BC");
        actual.Truncate("A\u0308BC", 2);
        actual.Clamp("A\u0308BC", 1, 5);
        actual.NormalizeFromValueAndMode("e\u0301", NormalizationMode.Nfc);
        actual.IsNormalizedFromValueAndMode("é", NormalizationMode.Nfc);

        Assert.That(actual.Clamp("", 30, 30), Is.EqualTo(expectedPosition.Clamp("", 30, 30)));
    }

    static string[] FixtureLines(string name)
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Unicode", name);
        return File.ReadAllLines(path).Where(line => line.Length > 0 && !line.StartsWith('#')).ToArray();
    }

    static string FromScalars(string field)
    {
        if (field.Length == 0)
        {
            return "";
        }
        return string.Concat(field.Split(' ').Select(value => char.ConvertFromUtf32(Convert.ToInt32(value, 16))));
    }
}
