namespace uri.Tests;

public class IriToUriConverterTests
{
    [Test]
    public void ConversionEncodesEveryInternationalizedComponentAsStrictUtf8()
    {
        string iri = "HTTP://usér@例え.テスト:443/α/𐀀?β\uE000#γ";
        string expected = "HTTP://us%C3%A9r@%E4%BE%8B%E3%81%88." +
            "%E3%83%86%E3%82%B9%E3%83%88:443/%CE%B1/%F0%90%80%80" +
            "?%CE%B2%EE%80%80#%CE%B3";

        UriTextResult result = IriConverterCore.ConvertIriToUri(
            iri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, expected);
    }

    [Test]
    public void ConversionUsesCanonicalUtf8ForEveryScalarLength()
    {
        UriTextResult result = IriConverterCore.ConvertIriToUri(
            "x:\u00A0\u0800𐀀".AsSpan(),
            new OperationLimits());

        AssertSuccess(result, "x:%C2%A0%E0%A0%80%F0%90%80%80");
    }

    [Test]
    public void ExistingPercentTripletsRemainByteForByteUnchanged()
    {
        string iri = "x://us%65r@h%6fst/%CE%B1%2f?%e4%BE%8B#%FF";

        UriTextResult result = IriConverterCore.ConvertIriToUri(
            iri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, iri);
    }

    [Test]
    public void ComponentSpecificUriReservedCharactersRemainSyntax()
    {
        string iri = "x://u:p!$&'()*+,;=@host!$&'()*+,;=/a:b@c!$&'()*+,;=/" +
            "?q:/?@!$&'()*+,;=#f:/?@!$&'()*+,;=";

        UriTextResult result = IriConverterCore.ConvertIriToUri(
            iri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, iri);
    }

    [Test]
    public void GenericRegisteredNameConversionUsesPercentEncodingNotIdnMapping()
    {
        UriTextResult result = IriConverterCore.ConvertIriToUri(
            "https://例え.テスト/".AsSpan(),
            new OperationLimits());

        AssertSuccess(
            result,
            "https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/");
        Assert.That(result.Text, Does.Not.Contain("xn--"));
    }

    [TestCase("x://192.0.2.128/道", "x://192.0.2.128/%E9%81%93")]
    [TestCase("x://[2001:db8::1]/道", "x://[2001:db8::1]/%E9%81%93")]
    [TestCase("x://[vF.a:b]/道", "x://[vF.a:b]/%E9%81%93")]
    public void StrictUriHostKindsRemainUnchanged(string iri, string expected)
    {
        UriTextResult result = IriConverterCore.ConvertIriToUri(
            iri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, expected);
    }

    [TestCase("")]
    [TestCase("?")]
    [TestCase("#")]
    [TestCase("?#")]
    [TestCase("x://")]
    [TestCase("x:///;?#")]
    public void EmptyAndDelimiterOnlyComponentsRemainLossless(string iri)
    {
        UriTextResult result = IriConverterCore.ConvertIriToUri(
            iri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, iri);
    }

    [Test]
    public void AsciiOnlyIriReferencesRemainUnchanged()
    {
        string iri = "scheme://user:pass@example.test:080/a/b;c?x=1&y=2#part";

        UriTextResult result = IriConverterCore.ConvertIriToUri(
            iri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, iri);
    }

    [Test]
    public void ConvertedTextIsAStrictUriReference()
    {
        UriTextResult converted = IriConverterCore.ConvertIriToUri(
            "https://例え.テスト/道?問\uE000#片".AsSpan(),
            new OperationLimits());

        UriParseResult parsed = UriParserCore.ParseReference(converted.Text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(converted.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Reference.OriginalText, Is.EqualTo(converted.Text));
            Assert.That(parsed.Reference.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName));
        });
    }

    [Test]
    public void SpanConversionConsumesOnlyTheProvidedSlice()
    {
        string iri = "x:道?問#片";
        string padded = "before" + iri + "after";

        UriTextResult result = IriConverterCore.ConvertIriToUri(
            padded.AsSpan(6, iri.Length),
            new OperationLimits());

        AssertSuccess(result, "x:%E9%81%93?%E5%95%8F#%E7%89%87");
    }

    [TestCase("x:bad space", UriDiagnosticCode.InvalidPath, UriComponent.Path, 5)]
    [TestCase("x:道%G0", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path, 4)]
    [TestCase("x:道#\uE000", UriDiagnosticCode.InvalidFragment, UriComponent.Fragment, 4)]
    public void InvalidIriInputFailsBeforeConversion(
        string iri,
        UriDiagnosticCode code,
        UriComponent component,
        int offset)
    {
        UriTextResult result = IriConverterCore.ConvertIriToUri(
            iri.AsSpan(),
            new OperationLimits());

        AssertFailure(result, code, component, offset);
    }

    [Test]
    public void MalformedUtf16FailsBeforeUtf8Encoding()
    {
        string iri = "x:" + '\uD800';

        UriTextResult result = IriConverterCore.ConvertIriToUri(
            iri.AsSpan(),
            new OperationLimits());

        AssertFailure(result, UriDiagnosticCode.InvalidUnicode, UriComponent.Path, 2);
    }

    [Test]
    public void InputAndOutputLimitsProduceIriDiagnostics()
    {
        OperationLimits inputLimits = new OperationLimits();
        inputLimits.MaxInputCharacters = 2;
        OperationLimits exactOutputLimits = new OperationLimits();
        exactOutputLimits.MaxOutputCharacters = 8;
        OperationLimits shortOutputLimits = new OperationLimits();
        shortOutputLimits.MaxOutputCharacters = 7;

        UriTextResult input = IriConverterCore.ConvertIriToUri(
            "x:é".AsSpan(),
            inputLimits);
        UriTextResult exact = IriConverterCore.ConvertIriToUri(
            "x:é".AsSpan(),
            exactOutputLimits);
        UriTextResult output = IriConverterCore.ConvertIriToUri(
            "x:é".AsSpan(),
            shortOutputLimits);

        Assert.Multiple(() =>
        {
            AssertFailure(input, UriDiagnosticCode.LimitExceeded, UriComponent.Input, 2);
            AssertSuccess(exact, "x:%C3%A9");
            AssertFailure(output, UriDiagnosticCode.LimitExceeded, UriComponent.Output, 7);
        });
    }

    [Test]
    public void MissingOrNegativeLimitsFailWithoutPartialOutput()
    {
        OperationLimits negativeOutput = new OperationLimits();
        negativeOutput.MaxOutputCharacters = -1;

        UriTextResult missing = IriConverterCore.ConvertIriToUri(
            "x:道".AsSpan(),
            null);
        UriTextResult negative = IriConverterCore.ConvertIriToUri(
            "x:道".AsSpan(),
            negativeOutput);

        Assert.Multiple(() =>
        {
            AssertFailure(missing, UriDiagnosticCode.InvalidInput, UriComponent.Input, 0);
            AssertFailure(negative, UriDiagnosticCode.LimitExceeded, UriComponent.Output, 0);
            Assert.That(missing.Text, Is.Empty);
            Assert.That(negative.Text, Is.Empty);
        });
    }

    private static void AssertSuccess(UriTextResult result, string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Length, Is.EqualTo(expected.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        UriTextResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int offset)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        });
    }
}
