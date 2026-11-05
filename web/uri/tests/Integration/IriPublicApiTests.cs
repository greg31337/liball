namespace uri.Tests;

public class IriPublicApiTests
{
    [Test]
    public void ParserExposesLosslessUnicodeReferenceParsing()
    {
        IIriParser parser = new IriParser();

        IriReference reference = parser.ParseIriReference(
            "https://usér@éxample.test:443/café?q=résumé#élève",
            new IriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.HasScheme, Is.True);
            Assert.That(reference.Scheme, Is.EqualTo("https"));
            Assert.That(reference.HasAuthority, Is.True);
            Assert.That(reference.Authority.UserInfo, Is.EqualTo("usér"));
            Assert.That(reference.Authority.Host, Is.EqualTo("éxample.test"));
            Assert.That(reference.Authority.Port, Is.EqualTo("443"));
            Assert.That(reference.Path, Is.EqualTo("/café"));
            Assert.That(reference.Query, Is.EqualTo("q=résumé"));
            Assert.That(reference.Fragment, Is.EqualTo("élève"));
        });
    }

    [Test]
    public void ParserResultAndSpanMethodsReturnExpectedOutcomes()
    {
        IIriParser parser = new IriParser();
        string input = "x:/café?q=é#é";
        string padded = "before" + input + "after";

        IriParseResult parsed = parser.TryParseIriReference(
            input,
            new IriParseOptions());
        IriParseResult span = parser.TryParseIriReferenceSpan(
            padded.AsSpan(6, input.Length),
            new IriParseOptions());
        IriParseResult malformed = parser.TryParseIriReference(
            "x:/bad space",
            new IriParseOptions());

        Assert.Multiple(() =>
        {
            AssertParseSuccess(parsed, input);
            AssertParseSuccess(span, input);
            AssertParseFailure(malformed, UriDiagnosticCode.InvalidPath);
        });
    }

    [Test]
    public void AbsoluteIriParsingRequiresSchemeAndForbidsFragment()
    {
        IIriParser parser = new IriParser();

        IriParseResult valid = parser.TryParseAbsoluteIri(
            "https://éxample.test/café?q=é",
            new IriParseOptions());
        IriParseResult relative = parser.TryParseAbsoluteIri(
            "/café",
            new IriParseOptions());
        IriParseResult fragment = parser.TryParseAbsoluteIri(
            "x:/café#é",
            new IriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Reference.Kind, Is.EqualTo(UriReferenceKind.AbsoluteUri));
            AssertParseFailure(relative, UriDiagnosticCode.InvalidScheme);
            AssertParseFailure(fragment, UriDiagnosticCode.InvalidFragment);
        });
    }

    [Test]
    public void ParserReportsInvalidArgumentsLimitsAndThrowingFailures()
    {
        IIriParser parser = new IriParser();
        IriParseOptions missingLimits = new IriParseOptions();
        missingLimits.Limits = null!;
        IriParseOptions shortInput = new IriParseOptions();
        shortInput.Limits.MaxInputCharacters = 2;

        IriParseResult missingText = parser.TryParseIriReference(
            null!,
            new IriParseOptions());
        IriParseResult missingOptions = parser.TryParseIriReference(
            "x:a",
            null!);
        IriParseResult invalidLimits = parser.TryParseAbsoluteIri(
            "x:a",
            missingLimits);
        IriParseResult limited = parser.TryParseIriReference(
            "x:a",
            shortInput);
        IriSyntaxException exception = Assert.Throws<IriSyntaxException>(() =>
            parser.ParseIriReference("x:/bad space", new IriParseOptions()))!;

        Assert.Multiple(() =>
        {
            AssertParseFailure(missingText, UriDiagnosticCode.InvalidInput);
            AssertParseFailure(missingOptions, UriDiagnosticCode.InvalidInput);
            AssertParseFailure(invalidLimits, UriDiagnosticCode.InvalidInput);
            AssertParseFailure(limited, UriDiagnosticCode.LimitExceeded);
            Assert.That(exception.Diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
            Assert.That(exception.Message, Is.EqualTo(exception.Diagnostic.Message));
        });
    }

    [Test]
    public void ConverterMapsIriAndUriTextThroughItsPublicMethods()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions options = new IriConversionOptions();

        string uriText = converter.ConvertIriToUri(
            "x://usér@éxample.test/café?q=é#é",
            options);
        UriTextResult result = converter.TryConvertIriToUri(
            "x:/résumé",
            options);
        string iriText = converter.ConvertUriToIri(
            "x:/caf%C3%A9?q=%C3%A9#%C3%A9",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(uriText, Is.EqualTo(
                "x://us%C3%A9r@%C3%A9xample.test/caf%C3%A9?q=%C3%A9#%C3%A9"));
            AssertTextSuccess(result, "x:/r%C3%A9sum%C3%A9");
            Assert.That(iriText, Is.EqualTo("x:/café?q=é#é"));
        });
    }

    [Test]
    public void ConverterAppliesTheExplicitNormalizationMode()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions normalize = new IriConversionOptions();
        normalize.NormalizationMode = UnicodeNormalizationMode.NormalizeNfc;
        IriConversionOptions diagnose = new IriConversionOptions();
        diagnose.NormalizationMode = UnicodeNormalizationMode.DiagnoseNfc;

        string mapped = converter.ConvertIriToUri("x:/cafe\u0301", normalize);
        string decoded = converter.ConvertUriToIri("x:/e%CC%81", normalize);
        UriTextResult diagnosed = converter.TryConvertIriToUri(
            "x:/cafe\u0301",
            diagnose);

        Assert.Multiple(() =>
        {
            Assert.That(mapped, Is.EqualTo("x:/caf%C3%A9"));
            Assert.That(decoded, Is.EqualTo("x:/é"));
            AssertTextFailure(diagnosed, UriDiagnosticCode.NormalizationRequired);
        });
    }

    [Test]
    public void ConverterProvidesExplicitPlatformIdnHostRoundTrips()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions options = new IriConversionOptions();
        options.IdnMode = IriIdnMode.Platform;

        string ascii = converter.ConvertHostToAscii("bücher.example", options);
        string unicode = converter.ConvertHostToUnicode(ascii, options);

        Assert.Multiple(() =>
        {
            Assert.That(ascii, Is.EqualTo("xn--bcher-kva.example"));
            Assert.That(unicode, Is.EqualTo("bücher.example"));
        });
    }

    [Test]
    public void ConverterReturnsResultsAndThrowsIriConversionExceptions()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions missingLimits = new IriConversionOptions();
        missingLimits.Limits = null!;
        IriConversionOptions noIdn = new IriConversionOptions();
        IriConversionOptions invalidMode = new IriConversionOptions();
        invalidMode.NormalizationMode = (UnicodeNormalizationMode)99;

        UriTextResult malformed = converter.TryConvertIriToUri(
            "x:/bad%",
            new IriConversionOptions());
        UriTextResult missing = converter.TryConvertIriToUri(
            null!,
            new IriConversionOptions());
        UriTextResult invalidOptions = converter.TryConvertIriToUri(
            "x:a",
            missingLimits);
        IriConversionException uriException = Assert.Throws<IriConversionException>(() =>
            converter.ConvertUriToIri("x:/bad%", new IriConversionOptions()))!;
        IriConversionException idnException = Assert.Throws<IriConversionException>(() =>
            converter.ConvertHostToAscii("bücher.example", noIdn))!;
        IriConversionException modeException = Assert.Throws<IriConversionException>(() =>
            converter.ConvertIriToUri("x:a", invalidMode))!;

        Assert.Multiple(() =>
        {
            AssertTextFailure(malformed, UriDiagnosticCode.InvalidPercentEncoding);
            AssertTextFailure(missing, UriDiagnosticCode.InvalidInput);
            AssertTextFailure(invalidOptions, UriDiagnosticCode.InvalidInput);
            Assert.That(uriException.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
            Assert.That(idnException.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(modeException.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(uriException.Message, Is.EqualTo(uriException.Diagnostic.Message));
        });
    }

    [Test]
    public void ConverterEnforcesInputAndOutputLimits()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions input = new IriConversionOptions();
        input.Limits.MaxInputCharacters = 2;
        IriConversionOptions output = new IriConversionOptions();
        output.Limits.MaxOutputCharacters = 7;

        UriTextResult inputFailure = converter.TryConvertIriToUri("x:é", input);
        UriTextResult outputFailure = converter.TryConvertIriToUri("x:é", output);

        Assert.Multiple(() =>
        {
            AssertTextFailure(inputFailure, UriDiagnosticCode.LimitExceeded);
            AssertTextFailure(outputFailure, UriDiagnosticCode.LimitExceeded);
        });
    }

    [Test]
    public void NormalizerPreservesNormalizesAndDiagnosesWithoutMutation()
    {
        IIriNormalizer normalizer = new IriNormalizer();
        IriConversionOptions preserve = new IriConversionOptions();
        IriConversionOptions normalize = new IriConversionOptions();
        normalize.NormalizationMode = UnicodeNormalizationMode.NormalizeNfc;
        string decomposed = "x://e\u0301.test/cafe\u0301?q=e\u0301#e\u0301";

        string preserved = normalizer.NormalizeIri(decomposed, preserve);
        string normalized = normalizer.NormalizeIri(decomposed, normalize);
        UriDiagnostic? diagnostic = normalizer.DiagnoseIriNormalization(
            decomposed,
            preserve);
        UriDiagnostic? valid = normalizer.DiagnoseIriNormalization(
            normalized,
            preserve);

        Assert.Multiple(() =>
        {
            Assert.That(preserved, Is.EqualTo(decomposed));
            Assert.That(normalized, Is.EqualTo("x://é.test/café?q=é#é"));
            Assert.That(diagnostic, Is.Not.Null);
            Assert.That(diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.NormalizationRequired));
            Assert.That(diagnostic.Component, Is.EqualTo(UriComponent.Host));
            Assert.That(valid, Is.Null);
        });
    }

    [Test]
    public void NormalizerReportsExpectedFailuresAndLimits()
    {
        IIriNormalizer normalizer = new IriNormalizer();
        IriConversionOptions diagnose = new IriConversionOptions();
        diagnose.NormalizationMode = UnicodeNormalizationMode.DiagnoseNfc;
        IriConversionOptions missingLimits = new IriConversionOptions();
        missingLimits.Limits = null!;
        IriConversionOptions output = new IriConversionOptions();
        output.NormalizationMode = UnicodeNormalizationMode.NormalizeNfc;
        output.Limits.MaxOutputCharacters = 2;

        IriConversionException required = Assert.Throws<IriConversionException>(() =>
            normalizer.NormalizeIri("x:/e\u0301", diagnose))!;
        IriConversionException limited = Assert.Throws<IriConversionException>(() =>
            normalizer.NormalizeIri("x:e\u0301", output))!;
        UriDiagnostic? missing = normalizer.DiagnoseIriNormalization(
            "x:a",
            missingLimits);
        UriDiagnostic? invalid = normalizer.DiagnoseIriNormalization(
            "x:/bad space",
            new IriConversionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(required.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.NormalizationRequired));
            Assert.That(limited.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(missing, Is.Not.Null);
            Assert.That(missing!.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(invalid, Is.Not.Null);
            Assert.That(invalid!.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
        });
    }

    [Test]
    public void ComparerExposesAllThreeExplicitIriLevels()
    {
        IIriComparer comparer = new IriComparer();
        UriComparisonOptions options = new UriComparisonOptions();

        UriComparisonResult exact = comparer.CompareIriExact(
            "x:/café",
            "x:/cafe\u0301",
            options);
        UriComparisonResult unicode = comparer.CompareIriUnicode(
            "x:/café",
            "x:/cafe\u0301",
            options);
        UriComparisonResult mapped = comparer.CompareIriAsUri(
            "X://EXAMPLE.test/%7e/é",
            "x://example.test/~/%C3%A9",
            options);

        Assert.Multiple(() =>
        {
            AssertComparisonDifference(exact, UriComparisonLevel.Exact, UriComponent.Path);
            AssertComparisonEquality(unicode, UriComparisonLevel.IriUnicode);
            AssertComparisonEquality(mapped, UriComparisonLevel.IriMappedUri);
        });
    }

    [Test]
    public void ComparerReturnsIriFailuresForArgumentsSyntaxAndLimits()
    {
        IIriComparer comparer = new IriComparer();
        UriComparisonOptions missingLimits = new UriComparisonOptions();
        missingLimits.Limits = null!;
        UriComparisonOptions shortInput = new UriComparisonOptions();
        shortInput.Limits.MaxInputCharacters = 2;

        UriComparisonResult missing = comparer.CompareIriExact(
            null!,
            "x:a",
            new UriComparisonOptions());
        UriComparisonResult options = comparer.CompareIriUnicode(
            "x:a",
            "x:a",
            missingLimits);
        UriComparisonResult malformed = comparer.CompareIriAsUri(
            "x:/bad%",
            "x:a",
            new UriComparisonOptions());
        UriComparisonResult limited = comparer.CompareIriExact(
            "x:a",
            "x:a",
            shortInput);

        Assert.Multiple(() =>
        {
            AssertComparisonFailure(missing, UriComparisonLevel.Exact, UriDiagnosticCode.InvalidInput);
            AssertComparisonFailure(options, UriComparisonLevel.IriUnicode, UriDiagnosticCode.InvalidInput);
            AssertComparisonFailure(malformed, UriComparisonLevel.IriMappedUri, UriDiagnosticCode.InvalidPercentEncoding);
            AssertComparisonFailure(limited, UriComparisonLevel.Exact, UriDiagnosticCode.LimitExceeded);
        });
    }

    private static void AssertParseSuccess(IriParseResult result, string text)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertParseFailure(
        IriParseResult result,
        UriDiagnosticCode code)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reference.OriginalText, Is.Empty);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        });
    }

    private static void AssertTextSuccess(UriTextResult result, string text)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(text));
            Assert.That(result.Length, Is.EqualTo(text.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertTextFailure(
        UriTextResult result,
        UriDiagnosticCode code)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        });
    }

    private static void AssertComparisonEquality(
        UriComparisonResult result,
        UriComparisonLevel level)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Equal, Is.True);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertComparisonDifference(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriComponent component)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(component));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertComparisonFailure(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriDiagnosticCode code)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        });
    }
}
