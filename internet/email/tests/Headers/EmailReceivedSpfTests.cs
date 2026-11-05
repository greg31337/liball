using System.Text;
using email;

namespace email.Tests;

public class EmailReceivedSpfTests
{
    [Test]
    public void ParsesResultCommentKnownParametersAndExtensions()
    {
        const string input = "pass (mx.example: sender permitted) receiver=mx.example; client-ip=192.0.2.1; mechanism=ip4:192.0.2.1; envelope-from=\"user@example.com\"; x-trace=opaque;";

        EmailReceivedSpfParseResult result = Parse(input, EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        EmailReceivedSpf value = result.GetValue()!;
        Assert.That(value!.GetResultKind(), Is.EqualTo(EmailAuthenticationResultKind.Pass));
        Assert.That(value.GetCommentCount(), Is.EqualTo(1));
        Assert.That(value.GetComment(0).GetValue(), Is.EqualTo("mx.example: sender permitted"));
        Assert.That(value.GetParameterCount(), Is.EqualTo(5));
        Assert.That(value.GetFirstParameter("CLIENT-IP")!.GetValue(), Is.EqualTo("192.0.2.1"));
        Assert.That(value.GetFirstParameter("mechanism")!.GetValue(), Is.EqualTo("ip4:192.0.2.1"));
        Assert.That(value.GetFirstParameter("envelope-from")!.WasQuoted(), Is.True);
        Assert.That(value.GetFirstParameter("envelope-from")!.GetValue(), Is.EqualTo("user@example.com"));
        Assert.That(value.GetFirstParameter("x-trace")!.GetValue(), Is.EqualTo("opaque"));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
    }

    [TestCase("pass", EmailAuthenticationResultKind.Pass)]
    [TestCase("fail", EmailAuthenticationResultKind.Fail)]
    [TestCase("softfail", EmailAuthenticationResultKind.SoftFail)]
    [TestCase("neutral", EmailAuthenticationResultKind.Neutral)]
    [TestCase("none", EmailAuthenticationResultKind.None)]
    [TestCase("temperror", EmailAuthenticationResultKind.TempError)]
    [TestCase("permerror", EmailAuthenticationResultKind.PermError)]
    public void RecognizesEverySpfResult(string token, EmailAuthenticationResultKind expected)
    {
        EmailReceivedSpfParseResult result = Parse(token, EmailStructuredValueParseMode.Strict);

        Assert.That(result.GetValue()!.GetResultKind(), Is.EqualTo(expected));
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void UnknownResultIsRetainedWithModeSpecificIssue()
    {
        EmailReceivedSpfParseResult tolerant = Parse("bestguesspass receiver=mx.example", EmailStructuredValueParseMode.Tolerant);
        EmailReceivedSpfParseResult strict = Parse("bestguesspass receiver=mx.example", EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.GetValue()!.GetResult(), Is.EqualTo("bestguesspass"));
        Assert.That(tolerant.GetValue()!.GetResultKind(), Is.EqualTo(EmailAuthenticationResultKind.Unknown));
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.ReceivedSpfMalformed), Is.True);
    }

    [Test]
    public void MalformedPairIsNotExposedAsTrustedParameter()
    {
        const string input = "pass client-ip 192.0.2.1; receiver=mx.example";

        EmailReceivedSpfParseResult result = Parse(input, EmailStructuredValueParseMode.Tolerant);

        Assert.That(result.GetValue()!.GetParameterCount(), Is.EqualTo(1));
        Assert.That(result.GetValue()!.GetFirstParameter("receiver")!.GetValue(), Is.EqualTo("mx.example"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ReceivedSpfMalformed), Is.True);
        Assert.That(Ascii(result.GetValue()!.GetRawBytes()), Is.EqualTo(input));
    }

    [Test]
    public void CanonicalFormatterPreservesCommentsParametersAndExtensions()
    {
        EmailReceivedSpf value = Parse("PASS (allowed) receiver=MX.Example; envelope-from=\"user@example.com\"; x-key=value;", EmailStructuredValueParseMode.Strict).GetValue()!;

        EmailAuthenticationFormatOptions options = new EmailAuthenticationFormatOptions();
        options.SetMaximumLineLength(998);
        EmailAuthenticationFormatResult formatted = value!.FormatWithOptions(options);
        EmailReceivedSpfParseResult reparsed = new EmailReceivedSpfParser().ParseWithInputAndMode(formatted.GetBytes(), EmailStructuredValueParseMode.Strict);

        Assert.That(formatted.IsSuccess(), Is.True);
        Assert.That(Ascii(formatted.GetBytes()), Is.EqualTo("pass (allowed) receiver=MX.Example; envelope-from=\"user@example.com\"; x-key=value;"));
        Assert.That(reparsed.IsSuccess(), Is.True);
        Assert.That(reparsed.GetValue()!.GetParameterCount(), Is.EqualTo(3));
        Assert.That(reparsed.GetValue()!.GetFirstParameter("x-key")!.GetValue(), Is.EqualTo("value"));
    }

    [Test]
    public void FormatterFoldsLongParameterLists()
    {
        EmailReceivedSpf value = Parse("pass receiver=mx.example; client-ip=192.0.2.1; envelope-from=\"user@example.com\";", EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailAuthenticationFormatOptions options = new EmailAuthenticationFormatOptions();
        options.SetMaximumLineLength(30);

        EmailAuthenticationFormatResult formatted = value!.FormatWithOptions(options);

        Assert.That(formatted.GetFoldCount(), Is.GreaterThan(0));
        Assert.That(new EmailReceivedSpfParser().ParseWithInputAndMode(formatted.GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
    }

    [Test]
    public void ParameterLimitReturnsDeterministicPartialValue()
    {
        EmailAuthenticationParserOptions options = new EmailAuthenticationParserOptions();
        options.SetMaximumProperties(1);

        EmailReceivedSpfParseResult result = new EmailReceivedSpfParser().ParseWithInputAndOptions(Bytes("pass receiver=a; client-ip=192.0.2.1"), options);

        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetValue()!.GetParameterCount(), Is.EqualTo(1));
        Assert.That(result.IsComplete(), Is.False);
    }

    static EmailReceivedSpfParseResult Parse(string value, EmailStructuredValueParseMode mode)
    {
        return new EmailReceivedSpfParser().ParseWithInputAndMode(Bytes(value), mode);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Ascii(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
