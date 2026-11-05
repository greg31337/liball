namespace uri.Tests;

public class IdnHostCoreTests
{
    [TestCase("例え.テスト", "xn--r8jz45g.xn--zckzah")]
    [TestCase("bücher.example", "xn--bcher-kva.example")]
    [TestCase("é.example", "xn--9ca.example")]
    [TestCase("example.test", "example.test")]
    [TestCase("EXAMPLE.test", "EXAMPLE.test")]
    public void PlatformModeMapsUnicodeLabelsToAscii(string host, string expected)
    {
        UriTextResult result = IdnHostCore.ConvertHostToAscii(
            host.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());

        AssertSuccess(result, expected);
    }

    [TestCase("xn--r8jz45g.xn--zckzah", "例え.テスト")]
    [TestCase("xn--bcher-kva.example", "bücher.example")]
    [TestCase("xn--9ca.example", "é.example")]
    [TestCase("example.test", "example.test")]
    public void PlatformModeMapsValidatedAsciiLabelsToUnicode(
        string host,
        string expected)
    {
        UriTextResult result = IdnHostCore.ConvertHostToUnicode(
            host.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());

        AssertSuccess(result, expected);
    }

    [TestCase("例え。テスト", "xn--r8jz45g.xn--zckzah")]
    [TestCase("例え．テスト", "xn--r8jz45g.xn--zckzah")]
    [TestCase("例え｡テスト", "xn--r8jz45g.xn--zckzah")]
    [TestCase("例え.テスト.", "xn--r8jz45g.xn--zckzah.")]
    [TestCase(".", ".")]
    [TestCase("", "")]
    public void RecognizedDnsSeparatorsAndRootStateAreCanonicalized(
        string host,
        string expected)
    {
        UriTextResult result = IdnHostCore.ConvertHostToAscii(
            host.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());

        AssertSuccess(result, expected);
    }

    [Test]
    public void AsciiAndUnicodeConversionsRoundTripThroughValidatedLabels()
    {
        OperationLimits limits = new OperationLimits();
        UriTextResult ascii = IdnHostCore.ConvertHostToAscii(
            "例え.テスト".AsSpan(),
            IriIdnMode.Platform,
            limits);
        UriTextResult unicode = IdnHostCore.ConvertHostToUnicode(
            ascii.Text.AsSpan(),
            IriIdnMode.Platform,
            limits);
        UriTextResult repeatedAscii = IdnHostCore.ConvertHostToAscii(
            unicode.Text.AsSpan(),
            IriIdnMode.Platform,
            limits);

        Assert.Multiple(() =>
        {
            AssertSuccess(ascii, "xn--r8jz45g.xn--zckzah");
            AssertSuccess(unicode, "例え.テスト");
            AssertSuccess(repeatedAscii, ascii.Text);
        });
    }

    [TestCase(".example", 0)]
    [TestCase("example..test", 8)]
    [TestCase("example。.test", 8)]
    [TestCase("-example.test", 0)]
    [TestCase("example-.test", 0)]
    [TestCase("ab--cd.test", 2)]
    [TestCase("under_score.test", 5)]
    [TestCase("percent%25.test", 7)]
    [TestCase("space name.test", 5)]
    [TestCase("xn--.test", 0)]
    [TestCase("xn--a.test", 2)]
    public void InvalidDnsLabelStructureIsRejectedBeforeMapping(
        string host,
        int offset)
    {
        UriTextResult result = IdnHostCore.ConvertHostToAscii(
            host.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());

        AssertFailure(result, UriDiagnosticCode.InvalidIdnLabel, offset);
    }

    [Test]
    public void InvalidUnicodeCategoriesAndMalformedUtf16AreRejectedBeforeMapping()
    {
        string malformed = "a" + '\uD800' + ".test";
        UriTextResult combining = IdnHostCore.ConvertHostToAscii(
            "\u0301a.test".AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());
        UriTextResult privateCharacter = IdnHostCore.ConvertHostToAscii(
            "a\uE000.test".AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());
        UriTextResult bidiControl = IdnHostCore.ConvertHostToAscii(
            "a\u202E.test".AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());
        UriTextResult symbol = IdnHostCore.ConvertHostToAscii(
            "a😀.test".AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());
        UriTextResult invalidUnicode = IdnHostCore.ConvertHostToAscii(
            malformed.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertFailure(combining, UriDiagnosticCode.InvalidIdnLabel, 0);
            AssertFailure(privateCharacter, UriDiagnosticCode.InvalidIdnLabel, 1);
            AssertFailure(bidiControl, UriDiagnosticCode.InvalidBidiText, 1);
            AssertFailure(symbol, UriDiagnosticCode.InvalidIdnLabel, 1);
            AssertFailure(invalidUnicode, UriDiagnosticCode.InvalidUnicode, 1);
        });
    }

    [Test]
    public void LabelAndDomainWireLengthLimitsAreChecked()
    {
        string longAsciiLabel = new string('a', 64) + ".test";
        string longUnicodeLabel = new string('é', 64) + ".test";
        string domainAtLimit = new string('a', 63) + "." +
            new string('b', 63) + "." +
            new string('c', 63) + "." +
            new string('d', 61);
        string longDomain = domainAtLimit + "e";

        UriTextResult asciiLabel = IdnHostCore.ConvertHostToAscii(
            longAsciiLabel.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());
        UriTextResult unicodeLabel = IdnHostCore.ConvertHostToAscii(
            longUnicodeLabel.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());
        UriTextResult exactDomain = IdnHostCore.ConvertHostToAscii(
            domainAtLimit.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());
        UriTextResult excessiveDomain = IdnHostCore.ConvertHostToAscii(
            longDomain.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertFailure(asciiLabel, UriDiagnosticCode.InvalidIdnLabel, 0);
            AssertFailure(unicodeLabel, UriDiagnosticCode.InvalidIdnLabel, 0);
            AssertSuccess(exactDomain, domainAtLimit);
            AssertFailure(excessiveDomain, UriDiagnosticCode.InvalidIdnLabel, 0);
        });
    }

    [Test]
    public void InputAndOutputLimitsProduceIriDiagnostics()
    {
        OperationLimits inputLimits = new OperationLimits();
        inputLimits.MaxInputCharacters = 1;
        OperationLimits exactOutput = new OperationLimits();
        exactOutput.MaxOutputCharacters = 11;
        OperationLimits shortOutput = new OperationLimits();
        shortOutput.MaxOutputCharacters = 10;

        UriTextResult input = IdnHostCore.ConvertHostToAscii(
            "例え".AsSpan(),
            IriIdnMode.Platform,
            inputLimits);
        UriTextResult exact = IdnHostCore.ConvertHostToAscii(
            "例え".AsSpan(),
            IriIdnMode.Platform,
            exactOutput);
        UriTextResult output = IdnHostCore.ConvertHostToAscii(
            "例え".AsSpan(),
            IriIdnMode.Platform,
            shortOutput);

        Assert.Multiple(() =>
        {
            AssertFailure(input, UriDiagnosticCode.LimitExceeded, 1);
            AssertSuccess(exact, "xn--r8jz45g");
            AssertFailure(output, UriDiagnosticCode.LimitExceeded, 10);
        });
    }

    [Test]
    public void MappingModeAndLimitPreconditionsAreExplicitDiagnostics()
    {
        OperationLimits negativeOutput = new OperationLimits();
        negativeOutput.MaxOutputCharacters = -1;
        UriTextResult none = IdnHostCore.ConvertHostToAscii(
            "例え".AsSpan(),
            IriIdnMode.None,
            new OperationLimits());
        UriTextResult unknown = IdnHostCore.ConvertHostToUnicode(
            "xn--r8jz45g".AsSpan(),
            (IriIdnMode)999,
            new OperationLimits());
        UriTextResult missingLimits = IdnHostCore.ConvertHostToAscii(
            "例え".AsSpan(),
            IriIdnMode.Platform,
            null);
        UriTextResult emptyNegative = IdnHostCore.ConvertHostToAscii(
            "".AsSpan(),
            IriIdnMode.Platform,
            negativeOutput);

        Assert.Multiple(() =>
        {
            AssertFailure(none, UriDiagnosticCode.InvalidInput, 0);
            AssertFailure(unknown, UriDiagnosticCode.InvalidInput, 0);
            AssertFailure(missingLimits, UriDiagnosticCode.InvalidInput, 0);
            AssertFailure(emptyNegative, UriDiagnosticCode.LimitExceeded, 0);
        });
    }

    [Test]
    public void SpanInputsDoNotConsumeAdjacentText()
    {
        string host = "例え.テスト";
        string padded = "before" + host + "after";

        UriTextResult result = IdnHostCore.ConvertHostToAscii(
            padded.AsSpan(6, host.Length),
            IriIdnMode.Platform,
            new OperationLimits());

        AssertSuccess(result, "xn--r8jz45g.xn--zckzah");
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
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        });
    }
}
