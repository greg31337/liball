namespace uri.Tests;

public class UriToIriConverterTests
{
    [Test]
    public void ConversionDecodesEveryInternationalizedComponentFromStrictUtf8()
    {
        string uri = "HTTP://us%C3%A9r@%E4%BE%8B%E3%81%88." +
            "%E3%83%86%E3%82%B9%E3%83%88:443/%CE%B1/%F0%90%80%80" +
            "?%CE%B2%EE%80%80#%CE%B3";
        string expected = "HTTP://usér@例え.テスト:443/α/𐀀?β\uE000#γ";

        UriTextResult result = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, expected);
    }

    [Test]
    public void ConversionDecodesEveryCanonicalUtf8ScalarLength()
    {
        UriTextResult result = IriConverterCore.ConvertUriToIri(
            "x:%C2%A0%E0%A0%80%F0%90%80%80".AsSpan(),
            new OperationLimits());

        AssertSuccess(result, "x:\u00A0\u0800𐀀");
    }

    [Test]
    public void EncodedAsciiUnreservedCharactersDecodeSafely()
    {
        UriTextResult result = IriConverterCore.ConvertUriToIri(
            "x:%41%7a%30%2D%2e%5F%7e".AsSpan(),
            new OperationLimits());

        AssertSuccess(result, "x:Az0-._~");
    }

    [Test]
    public void EncodedReservedCharactersAndPercentRemainEncoded()
    {
        string uri = "x:%3A%2F%3F%23%5B%5D%40%21%24%26%27%28%29%2A%2B%2C%3B%3D%25";

        UriTextResult result = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, uri);
    }

    [Test]
    public void EncodedIllegalAsciiAndC1ControlsRemainEncoded()
    {
        string uri = "x:%00%20%7F%80%C2%80%C2%9F%C2%A0";
        string expected = "x:%00%20%7F%80%C2%80%C2%9F\u00A0";

        UriTextResult result = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, expected);
    }

    [TestCase("%80")]
    [TestCase("%C0%AF")]
    [TestCase("%E0%80%AF")]
    [TestCase("%ED%A0%80")]
    [TestCase("%F0%80%80%AF")]
    [TestCase("%F4%90%80%80")]
    [TestCase("%F5%80%80%80")]
    [TestCase("%C2")]
    [TestCase("%E2%82")]
    [TestCase("%F0%90%80")]
    public void InvalidOrIncompleteUtf8SequencesRemainByteForByteUnchanged(
        string encoded)
    {
        string uri = "x:" + encoded;

        UriTextResult result = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, uri);
    }

    [Test]
    public void InvalidPrefixDoesNotPreventAFollowingValidSequenceFromDecoding()
    {
        UriTextResult result = IriConverterCore.ConvertUriToIri(
            "x:%F0%9F%C3%A9".AsSpan(),
            new OperationLimits());

        AssertSuccess(result, "x:%F0%9Fé");
    }

    [Test]
    public void IprivateDecodesOnlyInsideTheQueryProduction()
    {
        string uri = "x://u%EE%80%80@h%EE%80%80/p%EE%80%80" +
            "?q%EE%80%80#f%EE%80%80";
        string expected = "x://u%EE%80%80@h%EE%80%80/p%EE%80%80" +
            "?q\uE000#f%EE%80%80";

        UriTextResult result = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, expected);
    }

    [TestCase("%EF%B7%90")]
    [TestCase("%EF%BF%BE")]
    [TestCase("%EF%BF%BF")]
    [TestCase("%F4%8F%BF%BE")]
    [TestCase("%F4%8F%BF%BF")]
    public void CodePointsExcludedFromUcscharRemainEncoded(string encoded)
    {
        string uri = "x:" + encoded;

        UriTextResult result = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, uri);
    }

    [TestCase("%e2%80%8e")]
    [TestCase("%e2%80%8f")]
    [TestCase("%e2%80%aa")]
    [TestCase("%e2%80%ab")]
    [TestCase("%e2%80%ac")]
    [TestCase("%e2%80%ad")]
    [TestCase("%e2%80%ae")]
    public void Rfc3987BidiFormattingCharactersRemainEncoded(string encoded)
    {
        string uri = "x:" + encoded;

        UriTextResult result = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, uri);
    }

    [TestCase("x://192.0.2.128/path")]
    [TestCase("x://[2001:db8::1]/path")]
    [TestCase("x://[vF.a:b]/path")]
    public void StrictUriHostKindsRemainUnchanged(string uri)
    {
        UriTextResult result = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, uri);
    }

    [TestCase("")]
    [TestCase("?")]
    [TestCase("#")]
    [TestCase("?#")]
    [TestCase("x://")]
    [TestCase("x:///;?#")]
    [TestCase("scheme://user@example.test:080/a/b?x=1#part")]
    public void LiteralAsciiAndEmptyComponentsRemainLossless(string uri)
    {
        UriTextResult result = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        AssertSuccess(result, uri);
    }

    [Test]
    public void ConvertedTextIsAStrictIriReference()
    {
        string uri = "https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9%E3%83%88/" +
            "%E9%81%93?%E5%95%8F#%E7%89%87";
        UriTextResult converted = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        IriParseResult parsed = IriParserCore.ParseReference(converted.Text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(converted.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Reference.OriginalText, Is.EqualTo(converted.Text));
            Assert.That(parsed.Reference.Authority.Host, Is.EqualTo("例え.テスト"));
        });
    }

    [Test]
    public void SafeUnreservedNormalizationRoundTripsBackToAUri()
    {
        OperationLimits limits = new OperationLimits();
        UriTextResult iri = IriConverterCore.ConvertUriToIri(
            "x:%41%C3%A9%2F".AsSpan(),
            limits);
        UriTextResult uri = IriConverterCore.ConvertIriToUri(
            iri.Text.AsSpan(),
            limits);

        Assert.Multiple(() =>
        {
            AssertSuccess(iri, "x:Aé%2F");
            AssertSuccess(uri, "x:A%C3%A9%2F");
        });
    }

    [Test]
    public void SpanConversionConsumesOnlyTheProvidedSlice()
    {
        string uri = "x:%E9%81%93?%E5%95%8F#%E7%89%87";
        string padded = "before" + uri + "after";

        UriTextResult result = IriConverterCore.ConvertUriToIri(
            padded.AsSpan(6, uri.Length),
            new OperationLimits());

        AssertSuccess(result, "x:道?問#片");
    }

    [TestCase("x:%", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path, 2)]
    [TestCase("x:%G0", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path, 3)]
    [TestCase("x:bad space", UriDiagnosticCode.InvalidPath, UriComponent.Path, 5)]
    [TestCase("x:é", UriDiagnosticCode.InvalidPath, UriComponent.Path, 2)]
    public void InvalidUriInputFailsBeforeDecoding(
        string uri,
        UriDiagnosticCode code,
        UriComponent component,
        int offset)
    {
        UriTextResult result = IriConverterCore.ConvertUriToIri(
            uri.AsSpan(),
            new OperationLimits());

        AssertFailure(result, code, component, offset);
    }

    [Test]
    public void InputAndOutputLimitsApplyToTheSelectedSpanAndDecodedText()
    {
        OperationLimits inputLimits = new OperationLimits();
        inputLimits.MaxInputCharacters = 7;
        OperationLimits exactOutputLimits = new OperationLimits();
        exactOutputLimits.MaxOutputCharacters = 3;
        OperationLimits shortOutputLimits = new OperationLimits();
        shortOutputLimits.MaxOutputCharacters = 2;

        UriTextResult input = IriConverterCore.ConvertUriToIri(
            "x:%C3%A9".AsSpan(),
            inputLimits);
        UriTextResult exact = IriConverterCore.ConvertUriToIri(
            "x:%C3%A9".AsSpan(),
            exactOutputLimits);
        UriTextResult output = IriConverterCore.ConvertUriToIri(
            "x:%C3%A9".AsSpan(),
            shortOutputLimits);

        Assert.Multiple(() =>
        {
            AssertFailure(input, UriDiagnosticCode.LimitExceeded, UriComponent.Input, 7);
            AssertSuccess(exact, "x:é");
            AssertFailure(output, UriDiagnosticCode.LimitExceeded, UriComponent.Output, 2);
        });
    }

    [Test]
    public void MissingOrNegativeLimitsFailWithoutPartialText()
    {
        OperationLimits negativeOutput = new OperationLimits();
        negativeOutput.MaxOutputCharacters = -1;

        UriTextResult missing = IriConverterCore.ConvertUriToIri(
            "x:%C3%A9".AsSpan(),
            null);
        UriTextResult negative = IriConverterCore.ConvertUriToIri(
            "x:%C3%A9".AsSpan(),
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
