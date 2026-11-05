
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlNewlineTests
{
    [Test]
    public void NewlineRecognitionIsExhaustiveAcrossUtf16CodeUnits()
    {
        for (int code = 0; code <= char.MaxValue; code++)
        {
            char value = (char)code;
            bool expected = value == '\r' || value == '\n';
            Assert.That(HtmlNewlines.IsNewline(value), Is.EqualTo(expected), $"U+{code:X4}");
            Assert.That(HtmlNewlines.GetNormalizedCharacter(value), Is.EqualTo(value == '\r' ? '\n' : value), $"normalize U+{code:X4}");
        }
    }

    [Test]
    public void SequenceLengthDistinguishesCrLfLoneCrAndLf()
    {
        string input = "a\r\nb\rc\nd";

        Assert.That(HtmlNewlines.GetSequenceLength(input.AsSpan(), 0), Is.EqualTo(0));
        Assert.That(HtmlNewlines.GetSequenceLength(input.AsSpan(), 1), Is.EqualTo(2));
        Assert.That(HtmlNewlines.GetSequenceLength(input.AsSpan(), 4), Is.EqualTo(1));
        Assert.That(HtmlNewlines.GetSequenceLength(input.AsSpan(), 6), Is.EqualTo(1));
        Assert.That(HtmlNewlines.GetSequenceLength(input.AsSpan(), input.Length), Is.EqualTo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlNewlines.GetSequenceLength(input.AsSpan(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlNewlines.GetSequenceLength(input.AsSpan(), input.Length + 1));
    }

    [TestCase("", "")]
    [TestCase("plain", "plain")]
    [TestCase("\r", "\n")]
    [TestCase("\r\n", "\n")]
    [TestCase("\n", "\n")]
    [TestCase("a\r\nb\rc\nd", "a\nb\nc\nd")]
    [TestCase("\r\r\n\n", "\n\n\n")]
    public void NormalizeFoldsEveryHtmlNewlineForm(string input, string expected)
    {
        string result = HtmlNewlines.Normalize(input);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(HtmlNewlines.GetNormalizedLength(input.AsSpan()), Is.EqualTo(expected.Length));
    }

    [Test]
    public void NormalizeReturnsOriginalStringWhenNoCarriageReturnExists()
    {
        string input = "already\nnormalized";

        Assert.That(HtmlNewlines.Normalize(input), Is.SameAs(input));
        Assert.Throws<ArgumentNullException>(() => HtmlNewlines.Normalize(null));
    }
}
