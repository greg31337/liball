using System.Text;

namespace pop3.Tests;

public class Pop3NumberParserTests
{
    [TestCase("0", 0)]
    [TestCase("+42", 42)]
    [TestCase("-42", -42)]
    [TestCase("9223372036854775807", long.MaxValue)]
    [TestCase("-9223372036854775808", long.MinValue)]
    [TestCase("-0", 0)]
    public void SignedDecimalParserAcceptsTheCompleteInt64Range(string text, long expected)
    {
        Pop3NumberParseResult result = Parse(text, long.MinValue, long.MaxValue);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("+")]
    [TestCase("-")]
    [TestCase(" 1")]
    [TestCase("1 ")]
    [TestCase("1.0")]
    [TestCase("--1")]
    [TestCase("9223372036854775808")]
    [TestCase("-9223372036854775809")]
    [TestCase("12x")]
    public void SignedDecimalParserReportsInvalidInputWithoutThrowing(string text)
    {
        Pop3NumberParseResult result = Parse(text, long.MinValue, long.MaxValue);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Value, Is.Zero);
    }

    [Test]
    public void SignedDecimalParserAppliesCallerRangeWithoutThrowing()
    {
        Assert.That(Parse("10", 10, 20).Success, Is.True);
        Assert.That(Parse("9", 10, 20).Success, Is.False);
        Assert.That(Parse("21", 10, 20).Success, Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => Parse("10", 20, 10));
    }

    [Test]
    public void SignedDecimalParserAcceptsExactCallerRangeEndpoints()
    {
        Assert.That(Parse("-10", -10, 10).Success, Is.True);
        Assert.That(Parse("10", -10, 10).Success, Is.True);
        Assert.That(Parse("0", 0, 0).Success, Is.True);
        Assert.That(Parse("-11", -10, 10).Success, Is.False);
        Assert.That(Parse("11", -10, 10).Success, Is.False);
    }

    [TestCaseSource(nameof(InvalidDecimalOctets))]
    public void SignedDecimalParserRejectsNonAsciiAndControlOctets(byte[] value)
    {
        Pop3NumberParseResult result = new Pop3NumberParser().ParseSignedDecimal(
            value,
            long.MinValue,
            long.MaxValue);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Value, Is.Zero);
    }

    static IEnumerable<TestCaseData> InvalidDecimalOctets()
    {
        yield return new TestCaseData(new byte[] { 0 });
        yield return new TestCaseData(new byte[] { 49, 9 });
        yield return new TestCaseData(new byte[] { 49, 127 });
        yield return new TestCaseData(new byte[] { 49, 194, 169 });
    }

    static Pop3NumberParseResult Parse(string text, long minimum, long maximum)
    {
        return new Pop3NumberParser().ParseSignedDecimal(Encoding.ASCII.GetBytes(text), minimum, maximum);
    }
}
