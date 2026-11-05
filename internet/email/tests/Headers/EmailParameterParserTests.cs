using System.Text;
using email;

namespace email.Tests;

public class EmailParameterParserTests
{
    [TestCase("first=one; second=\"two words\"")]
    [TestCase("; first=one; second=\"two words\"")]
    public void ParsesStandaloneOrderedParametersWithOptionalLeadingSemicolon(string input)
    {
        EmailParameterParseResult result = new EmailParameterParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(input), EmailStructuredValueParseMode.Strict);

        Assert.That(result.GetValue().GetCount(), Is.EqualTo(2));
        Assert.That(result.GetValue().GetAt(0).GetName(), Is.EqualTo("first"));
        Assert.That(result.GetValue().GetAt(1).GetName(), Is.EqualTo("second"));
        Assert.That(Text(result.GetValue().GetAt(1).GetValueBytes()), Is.EqualTo("two words"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void PreservesQuotedPairsAndExactRawValue()
    {
        EmailParameterParseResult result = new EmailParameterParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes("filename=\"a\\\"b.txt\""), EmailStructuredValueParseMode.Strict);

        EmailParameter parameter = result.GetValue().GetAt(0);
        Assert.That(Text(parameter.GetRawValueBytes()), Is.EqualTo("\"a\\\"b.txt\""));
        Assert.That(Text(parameter.GetValueBytes()), Is.EqualTo("a\"b.txt"));
        Assert.That(parameter.WasQuoted(), Is.True);
    }

    [Test]
    public void ResolvesStandaloneRfc2231Continuation()
    {
        const string input = "filename*1*=world.txt; filename*0*=UTF-8''hello%20";

        EmailParameterParseResult result = new EmailParameterParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(input), EmailStructuredValueParseMode.Strict);

        EmailRfc2231DecodedParameter decoded = result.GetValue().GetDecoded("filename")!;
        Assert.That(Text(decoded!.GetDecodedBytes()), Is.EqualTo("hello world.txt"));
        Assert.That(decoded.IsContinued(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void EmptyStandaloneParameterSequenceIsValid()
    {
        EmailParameterParseResult result = new EmailParameterParser().ParseWithInputAndMode(new byte[0], EmailStructuredValueParseMode.Strict);

        Assert.That(result.GetValue().GetCount(), Is.Zero);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsComplete(), Is.True);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
