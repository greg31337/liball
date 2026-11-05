using System.Text;
using email;

namespace email.Tests;

public class EmailAuthenticationResultsTests
{
    [Test]
    public void ParsesAuthservVersionMethodsReasonsPropertiesAndComments()
    {
        const string input = "mx.example 1 (edge verifier); spf=pass reason=\"sender allowed\" smtp.mailfrom=user@example.com; dkim/2=policy header.d=example.com header.s=selector x.vendor=opaque";

        EmailAuthenticationResultsParseResult result = Parse(input, EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        EmailAuthenticationResults value = result.GetValue()!;
        Assert.That(value!.GetAuthenticationServiceIdentifier(), Is.EqualTo("mx.example"));
        Assert.That(value.HasVersion(), Is.True);
        Assert.That(value.GetVersion(), Is.EqualTo(1));
        Assert.That(value.GetCommentCount(), Is.EqualTo(1));
        Assert.That(value.GetComment(0).GetValue(), Is.EqualTo("edge verifier"));
        Assert.That(value.GetResultCount(), Is.EqualTo(2));

        EmailAuthenticationMethodResult spf = value.GetResult(0);
        Assert.That(spf.GetMethod(), Is.EqualTo("spf"));
        Assert.That(spf.GetResultKind(), Is.EqualTo(EmailAuthenticationResultKind.Pass));
        Assert.That(spf.HasReason(), Is.True);
        Assert.That(spf.GetReason(), Is.EqualTo("sender allowed"));
        Assert.That(spf.WasReasonQuoted(), Is.True);
        Assert.That(spf.GetPropertyCount(), Is.EqualTo(1));
        Assert.That(spf.GetProperty(0).GetPropertyType(), Is.EqualTo("smtp"));
        Assert.That(spf.GetProperty(0).GetProperty(), Is.EqualTo("mailfrom"));
        Assert.That(spf.GetProperty(0).GetValue(), Is.EqualTo("user@example.com"));

        EmailAuthenticationMethodResult dkim = value.GetResult(1);
        Assert.That(dkim.GetMethod(), Is.EqualTo("dkim"));
        Assert.That(dkim.HasVersion(), Is.True);
        Assert.That(dkim.GetVersion(), Is.EqualTo(2));
        Assert.That(dkim.GetResultKind(), Is.EqualTo(EmailAuthenticationResultKind.Policy));
        Assert.That(dkim.GetPropertyCount(), Is.EqualTo(3));
        Assert.That(dkim.GetProperty(2).GetPropertyType(), Is.EqualTo("x"));
        Assert.That(dkim.GetProperty(2).GetProperty(), Is.EqualTo("vendor"));
        Assert.That(dkim.GetProperty(2).GetValue(), Is.EqualTo("opaque"));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
    }

    [Test]
    public void OversizedVersionsSaturateWithoutOverflowing()
    {
        EmailAuthenticationResultsParseResult result = Parse("mx.example 2147483648; dkim/999999999999=pass", EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue()!.GetVersion(), Is.EqualTo(int.MaxValue));
        Assert.That(result.GetValue()!.GetResult(0).GetVersion(), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void ParsesNoAuthenticationMarker()
    {
        EmailAuthenticationResultsParseResult result = Parse("example.org 1; none (no checks)", EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue()!.HasNoResults(), Is.True);
        Assert.That(result.GetValue()!.GetResultCount(), Is.Zero);
        Assert.That(result.GetValue()!.GetComment(0).GetValue(), Is.EqualTo("no checks"));
    }

    [Test]
    public void ExtractsNestedAndEscapedCommentsWithoutChangingRawBytes()
    {
        const string input = "mx.example; dkim=pass (outer (nested) escaped\\) text) header.d=example.com";

        EmailAuthenticationResults value = Parse(input, EmailStructuredValueParseMode.Strict).GetValue()!;

        Assert.That(value!.GetResult(0).GetCommentCount(), Is.EqualTo(1));
        Assert.That(value.GetResult(0).GetComment(0).GetValue(), Is.EqualTo("outer (nested) escaped) text"));
        Assert.That(Ascii(value.GetResult(0).GetComment(0).GetRawBytes()), Is.EqualTo("(outer (nested) escaped\\) text)"));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
    }

    [Test]
    public void PreservesUnknownMethodResultAndPropertyExtensions()
    {
        EmailAuthenticationResultsParseResult result = Parse("auth.example; x-auth=x-result x-type.x-prop=blob", EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        EmailAuthenticationMethodResult method = result.GetValue()!.GetResult(0);
        Assert.That(method.GetNormalizedMethod(), Is.EqualTo("x-auth"));
        Assert.That(method.GetResultKind(), Is.EqualTo(EmailAuthenticationResultKind.Unknown));
        Assert.That(method.GetNormalizedResult(), Is.EqualTo("x-result"));
        Assert.That(method.GetProperty(0).GetNormalizedPropertyType(), Is.EqualTo("x-type"));
        Assert.That(method.GetProperty(0).GetNormalizedProperty(), Is.EqualTo("x-prop"));
    }

    [Test]
    public void RecoversMalformedSegmentsWithModeSpecificSeverity()
    {
        const string input = "auth.example; spf pass; dkim=pass header.d=example.com";

        EmailAuthenticationResultsParseResult tolerant = Parse(input, EmailStructuredValueParseMode.Tolerant);
        EmailAuthenticationResultsParseResult strict = Parse(input, EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.GetValue()!.GetResultCount(), Is.EqualTo(1));
        Assert.That(tolerant.GetValue()!.GetResult(0).GetMethod(), Is.EqualTo("dkim"));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.AuthenticationResultsMalformed), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(Ascii(tolerant.GetValue()!.GetRawBytes()), Is.EqualTo(input));
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void RetainsMalformedPropertyDataOnItsMethod()
    {
        const string input = "auth.example; spf=pass smtp.mailfrom";

        EmailAuthenticationResultsParseResult result = Parse(input, EmailStructuredValueParseMode.Tolerant);

        Assert.That(result.GetValue()!.GetResultCount(), Is.EqualTo(1));
        Assert.That(result.GetValue()!.GetResult(0).IsMalformed(), Is.True);
        Assert.That(result.GetValue()!.GetResult(0).GetPropertyCount(), Is.Zero);
        Assert.That(Ascii(result.GetValue()!.GetResult(0).GetRawBytes()), Is.EqualTo(" spf=pass smtp.mailfrom"));
    }

    [Test]
    public void HonorsInitialSourcePosition()
    {
        EmailAuthenticationParserOptions options = new EmailAuthenticationParserOptions();
        options.SetMode(EmailStructuredValueParseMode.Strict);
        EmailAuthenticationResultsParseResult result = new EmailAuthenticationResultsParser().ParseWithInputAndOptionsAndInitialPosition(Bytes("auth.example; spf=pass smtp.mailfrom=a@example"), options, new EmailSourcePosition(50, 4, 7));

        Assert.That(result.GetValue()!.GetSourcePosition().GetByteOffset(), Is.EqualTo(50));
        Assert.That(result.GetValue()!.GetResult(0).GetSourcePosition().GetByteOffset(), Is.EqualTo(64));
        Assert.That(result.GetValue()!.GetResult(0).GetProperty(0).GetSourcePosition().GetByteOffset(), Is.EqualTo(73));
    }

    [Test]
    public void EnforcesResultPropertyAndInputLimits()
    {
        EmailAuthenticationParserOptions resultOptions = new EmailAuthenticationParserOptions();
        resultOptions.SetMaximumResults(1);
        EmailAuthenticationResultsParseResult resultLimit = new EmailAuthenticationResultsParser().ParseWithInputAndOptions(Bytes("a; spf=pass; dkim=pass"), resultOptions);
        EmailAuthenticationParserOptions propertyOptions = new EmailAuthenticationParserOptions();
        propertyOptions.SetMaximumProperties(1);
        EmailAuthenticationResultsParseResult propertyLimit = new EmailAuthenticationResultsParser().ParseWithInputAndOptions(Bytes("a; dkim=pass header.d=a header.s=b"), propertyOptions);
        EmailAuthenticationParserOptions inputOptions = new EmailAuthenticationParserOptions();
        inputOptions.SetMaximumInputBytes(3);
        EmailAuthenticationResultsParseResult inputLimit = new EmailAuthenticationResultsParser().ParseWithInputAndOptions(Bytes("auth; none"), inputOptions);

        Assert.That(resultLimit.HasFatalError(), Is.True);
        Assert.That(resultLimit.GetValue()!.GetResultCount(), Is.EqualTo(1));
        Assert.That(resultLimit.IsComplete(), Is.False);
        Assert.That(propertyLimit.HasFatalError(), Is.True);
        Assert.That(propertyLimit.GetValue()!.GetResult(0).GetPropertyCount(), Is.EqualTo(1));
        Assert.That(inputLimit.HasValue(), Is.False);
        Assert.That(inputLimit.GetIssues().ContainsCode(EmailIssueCode.AuthenticationValueLimitExceeded), Is.True);
    }

    [Test]
    public void CanonicalFormattingRoundTripsSemantics()
    {
        EmailAuthenticationResults parsed = Parse("MX.Example 1 (edge); SPF=pass reason=\"sender allowed\" smtp.mailfrom=user@example.com; X-AUTH=X-OK x.kind=opaque", EmailStructuredValueParseMode.Strict).GetValue()!;

        EmailAuthenticationFormatOptions options = new EmailAuthenticationFormatOptions();
        options.SetMaximumLineLength(998);
        EmailAuthenticationFormatResult formatted = parsed!.FormatWithOptions(options);
        EmailAuthenticationResultsParseResult reparsed = new EmailAuthenticationResultsParser().ParseWithInputAndMode(formatted.GetBytes(), EmailStructuredValueParseMode.Strict);

        Assert.That(formatted.IsSuccess(), Is.True);
        Assert.That(Ascii(formatted.GetBytes()), Is.EqualTo("MX.Example 1 (edge); spf=pass reason=\"sender allowed\" smtp.mailfrom=\"user@example.com\"; x-auth=x-ok x.kind=opaque"));
        Assert.That(reparsed.IsSuccess(), Is.True);
        Assert.That(reparsed.GetValue()!.GetResultCount(), Is.EqualTo(2));
        Assert.That(reparsed.GetValue()!.GetResult(0).GetReason(), Is.EqualTo("sender allowed"));
        Assert.That(reparsed.GetValue()!.GetResult(0).GetProperty(0).GetValue(), Is.EqualTo("user@example.com"));
    }

    [Test]
    public void FormatterFoldsBeforeStructuredPiecesAndCanOmitComments()
    {
        EmailAuthenticationResults value = Parse("auth.example (comment); dkim=pass header.d=long-domain.example header.s=selector", EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailAuthenticationFormatOptions options = new EmailAuthenticationFormatOptions();
        options.SetMaximumLineLength(35);
        options.SetIncludeComments(false);

        EmailAuthenticationFormatResult formatted = value!.FormatWithOptions(options);

        Assert.That(formatted.IsSuccess(), Is.True);
        Assert.That(formatted.GetFoldCount(), Is.GreaterThan(0));
        Assert.That(Ascii(formatted.GetBytes()), Does.Not.Contain("comment"));
        Assert.That(Ascii(formatted.GetBytes()), Does.Contain("\r\n\t"));
        Assert.That(new EmailAuthenticationResultsParser().ParseWithInputAndMode(formatted.GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
    }

    [Test]
    public void FormatterEnforcesOutputLimitWithoutReturningPartialBytes()
    {
        EmailAuthenticationResults value = Parse("auth.example; dkim=pass header.d=example.com", EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailAuthenticationFormatOptions options = new EmailAuthenticationFormatOptions();
        options.SetMaximumOutputBytes(10);

        EmailAuthenticationFormatResult formatted = value!.FormatWithOptions(options);

        Assert.That(formatted.HasFatalError(), Is.True);
        Assert.That(formatted.GetByteCount(), Is.Zero);
        Assert.That(formatted.GetIssues().ContainsCode(EmailIssueCode.AuthenticationOutputLimitExceeded), Is.True);
    }

    [Test]
    public void ReturnedRawArraysAreDefensiveCopies()
    {
        EmailAuthenticationResults value = Parse("auth.example; spf=pass smtp.mailfrom=example.com", EmailStructuredValueParseMode.Strict).GetValue()!;
        byte[] raw = value!.GetRawBytes();
        byte[] propertyRaw = value.GetResult(0).GetProperty(0).GetRawBytes();
        raw[0] = (byte)'X';
        propertyRaw[0] = (byte)'X';

        Assert.That(Ascii(value.GetRawBytes()), Does.StartWith("auth"));
        Assert.That(Ascii(value.GetResult(0).GetProperty(0).GetRawBytes()), Does.StartWith("smtp"));
    }

    static EmailAuthenticationResultsParseResult Parse(string value, EmailStructuredValueParseMode mode)
    {
        return new EmailAuthenticationResultsParser().ParseWithInputAndMode(Bytes(value), mode);
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
