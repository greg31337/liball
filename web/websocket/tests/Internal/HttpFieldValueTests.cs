using websocket;

namespace websocket.Tests;

public class HttpFieldValueTests
{
    [Test]
    public void OptionalWhitespaceSkipsOnlySpaceAndHorizontalTab()
    {
        const string source = "x \t  value\r";

        Assert.Multiple(() =>
        {
            Assert.That(HttpFieldValue.SkipOptionalWhitespace(source, 0), Is.Zero);
            Assert.That(HttpFieldValue.SkipOptionalWhitespace(source, 1), Is.EqualTo(5));
            Assert.That(HttpFieldValue.SkipOptionalWhitespace(source, source.Length - 1), Is.EqualTo(source.Length - 1));
            Assert.That(HttpFieldValue.SkipOptionalWhitespace(source, source.Length), Is.EqualTo(source.Length));
            Assert.That(HttpFieldValue.SkipOptionalWhitespace(source, -1), Is.EqualTo(-1));
            Assert.That(HttpFieldValue.SkipOptionalWhitespace(source, source.Length + 1), Is.EqualTo(-1));
        });
    }

    [TestCase("", "")]
    [TestCase("plain", "plain")]
    [TestCase(" \t plain value\t ", "plain value")]
    [TestCase("\t \t", "")]
    public void TrimsOnlyOuterOptionalWhitespace(string source, string expected)
    {
        Assert.That(HttpFieldValue.TrimOptionalWhitespace(source), Is.EqualTo(expected));
    }

    [Test]
    public void AcceptsSafeHttpFieldValueCharacters()
    {
        var obsText = new string(new[] { (char)128, (char)255 });

        Assert.Multiple(() =>
        {
            Assert.That(HttpFieldValue.IsSafe(""), Is.True);
            Assert.That(HttpFieldValue.IsSafe("token, value; quoted=\"yes\"\tmore"), Is.True);
            Assert.That(HttpFieldValue.IsSafe(obsText), Is.True);
        });
    }

    [TestCase("unsafe\0value")]
    [TestCase("unsafe\rvalue")]
    [TestCase("unsafe\nvalue")]
    [TestCase("unsafe\u0001value")]
    [TestCase("unsafe\u001fvalue")]
    [TestCase("unsafe\u007fvalue")]
    [TestCase("unsafe\u0100value")]
    public void RejectsInjectionControlsDeleteAndNonOctetText(string value)
    {
        Assert.That(HttpFieldValue.IsSafe(value), Is.False);
    }
}
