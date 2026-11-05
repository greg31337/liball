namespace uri.Tests;

public class AsciiCaseTests
{
    [Test]
    public void FoldingChangesOnlyAsciiLetters()
    {
        for (int code = 0; code <= 255; code++)
        {
            char value = (char)code;
            char expectedLower = value;
            char expectedUpper = value;
            if (value >= 'A' && value <= 'Z')
            {
                expectedLower = (char)(value + 32);
            }

            if (value >= 'a' && value <= 'z')
            {
                expectedUpper = (char)(value - 32);
            }

            Assert.That(AsciiCase.FoldToLower(value), Is.EqualTo(expectedLower), $"lower U+{code:X4}");
            Assert.That(AsciiCase.FoldToUpper(value), Is.EqualTo(expectedUpper), $"upper U+{code:X4}");
        }
    }

    [TestCase(null, null, true)]
    [TestCase(null, "", false)]
    [TestCase("", "", true)]
    [TestCase("HTTP", "http", true)]
    [TestCase("UrN:Example", "uRn:eXAMPLE", true)]
    [TestCase("http", "https", false)]
    [TestCase("same", "sam", false)]
    [TestCase("I", "ı", false)]
    [TestCase("Ä", "ä", false)]
    public void EqualityUsesOnlyOrdinalAsciiCaseFolding(string? left, string? right, bool expected)
    {
        Assert.That(AsciiCase.EqualsIgnoreCase(left, right), Is.EqualTo(expected));
    }

    [Test]
    public void ComparisonUsesOrdinalAsciiOrderAndNullOrdering()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AsciiCase.CompareIgnoreCase(null, null), Is.Zero);
            Assert.That(AsciiCase.CompareIgnoreCase(null, "a"), Is.LessThan(0));
            Assert.That(AsciiCase.CompareIgnoreCase("a", null), Is.GreaterThan(0));
            Assert.That(AsciiCase.CompareIgnoreCase("HTTP", "http"), Is.Zero);
            Assert.That(AsciiCase.CompareIgnoreCase("a", "B"), Is.LessThan(0));
            Assert.That(AsciiCase.CompareIgnoreCase("B", "a"), Is.GreaterThan(0));
            Assert.That(AsciiCase.CompareIgnoreCase("abc", "ABCD"), Is.LessThan(0));
            Assert.That(AsciiCase.CompareIgnoreCase("ABCD", "abc"), Is.GreaterThan(0));
            Assert.That(AsciiCase.CompareIgnoreCase("Ä", "ä"), Is.LessThan(0));
        });
    }
}
