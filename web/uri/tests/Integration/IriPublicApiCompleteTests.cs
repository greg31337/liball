namespace uri.Tests;

public class IriPublicApiCompleteTests
{
    [TestCase("x:/道?問#片", UriReferenceKind.Uri)]
    [TestCase("//例え.test/道", UriReferenceKind.NetworkPathReference)]
    [TestCase("/道", UriReferenceKind.AbsolutePathReference)]
    [TestCase("道/先", UriReferenceKind.RelativePathReference)]
    [TestCase("?問", UriReferenceKind.RelativeReference)]
    [TestCase("#片", UriReferenceKind.SameDocumentReference)]
    [TestCase("", UriReferenceKind.EmptyReference)]
    public void EveryReferenceFormUsesAllPublicReferenceEntryPoints(
        string text,
        UriReferenceKind kind)
    {
        IIriParser parser = new IriParser();
        IriParseOptions options = new IriParseOptions();

        IriReference throwing = parser.ParseIriReference(text, options);
        IriParseResult result = parser.TryParseIriReference(text, options);
        IriParseResult span = parser.TryParseIriReferenceSpan(
            ("before" + text + "after").AsSpan(6, text.Length),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(throwing.Kind, Is.EqualTo(kind), text);
            Assert.That(throwing.OriginalText, Is.EqualTo(text), text);
            AssertParseSuccess(result, kind, text);
            AssertParseSuccess(span, kind, text);
        });
    }

    [Test]
    public void AbsoluteIriEntryPointReturnsItsDistinctForm()
    {
        IIriParser parser = new IriParser();

        IriParseResult result = parser.TryParseAbsoluteIri(
            "x://例え.test/道?問",
            new IriParseOptions());

        AssertParseSuccess(
            result,
            UriReferenceKind.AbsoluteUri,
            "x://例え.test/道?問");
    }

    [Test]
    public void ParserResultEntryPointsReturnCompleteIriDiagnostics()
    {
        IIriParser parser = new IriParser();
        IriParseOptions options = new IriParseOptions();

        IriParseResult text = parser.TryParseIriReference("x:/bad%", options);
        IriParseResult span = parser.TryParseIriReferenceSpan(
            "x:/bad%".AsSpan(),
            options);
        IriParseResult absolute = parser.TryParseAbsoluteIri(
            "x:/é#片",
            options);

        Assert.Multiple(() =>
        {
            AssertDiagnostic(
                text.Diagnostic,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.Path,
                6,
                1,
                DiagnosticFactory.InvalidPercentEncodingMessage);
            AssertDiagnostic(
                span.Diagnostic,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.Path,
                6,
                1,
                DiagnosticFactory.InvalidPercentEncodingMessage);
            AssertDiagnostic(
                absolute.Diagnostic,
                UriDiagnosticCode.InvalidFragment,
                UriComponent.Fragment,
                4,
                1,
                DiagnosticFactory.InvalidFragmentMessage);
        });
    }

    [Test]
    public void EveryParserEntryPointHonorsOptionsAndInputBoundaries()
    {
        IIriParser parser = new IriParser();
        IriParseOptions exact = new IriParseOptions();
        exact.Limits.MaxInputCharacters = 4;
        IriParseOptions shortLimit = new IriParseOptions();
        shortLimit.Limits.MaxInputCharacters = 3;
        IriParseOptions missingLimits = new IriParseOptions();
        missingLimits.Limits = null!;

        IriReference throwing = parser.ParseIriReference("x:ab", exact);
        IriParseResult reference = parser.TryParseIriReference("x:ab", shortLimit);
        IriParseResult span = parser.TryParseIriReferenceSpan(
            "x:ab".AsSpan(),
            shortLimit);
        IriParseResult absolute = parser.TryParseAbsoluteIri("x:ab", shortLimit);
        IriParseResult referenceOptions = parser.TryParseIriReference("x:a", null!);
        IriParseResult spanLimits = parser.TryParseIriReferenceSpan(
            "x:a".AsSpan(),
            missingLimits);
        IriParseResult absoluteOptions = parser.TryParseAbsoluteIri("x:a", null!);

        Assert.Multiple(() =>
        {
            Assert.That(throwing.OriginalText, Is.EqualTo("x:ab"));
            AssertInputLimit(reference.Diagnostic, 3, 1);
            AssertInputLimit(span.Diagnostic, 3, 1);
            AssertInputLimit(absolute.Diagnostic, 3, 1);
            AssertInvalidInput(referenceOptions.Diagnostic);
            AssertInvalidInput(spanLimits.Diagnostic);
            AssertInvalidInput(absoluteOptions.Diagnostic);
        });

        AssertInvalidInputException(() =>
            parser.ParseIriReference(null!, new IriParseOptions()));
        AssertInvalidInputException(() =>
            parser.ParseIriReference("x:a", null!));
        AssertInvalidInputException(() =>
            parser.ParseIriReference("x:a", missingLimits));
    }

    [Test]
    public void GenericConversionKeepsDnsMappingExplicitForBothModes()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions none = new IriConversionOptions();
        IriConversionOptions platform = new IriConversionOptions();
        platform.IdnMode = IriIdnMode.Platform;
        const string iri = "x://bücher.example/café";
        const string expected = "x://b%C3%BCcher.example/caf%C3%A9";

        string noneMapped = converter.ConvertIriToUri(iri, none);
        string platformMapped = converter.ConvertIriToUri(iri, platform);
        string ascii = converter.ConvertHostToAscii("bücher.example", platform);
        string unicode = converter.ConvertHostToUnicode(ascii, platform);
        IriConversionException noneAscii = Assert.Throws<IriConversionException>(() =>
            converter.ConvertHostToAscii("bücher.example", none))!;
        IriConversionException noneUnicode = Assert.Throws<IriConversionException>(() =>
            converter.ConvertHostToUnicode("xn--bcher-kva.example", none))!;

        Assert.Multiple(() =>
        {
            Assert.That(noneMapped, Is.EqualTo(expected));
            Assert.That(platformMapped, Is.EqualTo(expected));
            Assert.That(ascii, Is.EqualTo("xn--bcher-kva.example"));
            Assert.That(unicode, Is.EqualTo("bücher.example"));
            AssertException(
                noneAscii,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Host);
            AssertException(
                noneUnicode,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Host);
        });
    }

    [Test]
    public void EveryNormalizationModeAppliesAcrossBothConversionDirections()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions preserve = new IriConversionOptions();
        IriConversionOptions normalize = new IriConversionOptions();
        normalize.NormalizationMode = UnicodeNormalizationMode.NormalizeNfc;
        IriConversionOptions diagnose = new IriConversionOptions();
        diagnose.NormalizationMode = UnicodeNormalizationMode.DiagnoseNfc;

        string preservedUri = converter.ConvertIriToUri("x:/cafe\u0301", preserve);
        string normalizedUri = converter.ConvertIriToUri("x:/cafe\u0301", normalize);
        UriTextResult diagnosedIri = converter.TryConvertIriToUri(
            "x:/cafe\u0301",
            diagnose);
        string acceptedIri = converter.ConvertIriToUri("x:/café", diagnose);

        string preservedIri = converter.ConvertUriToIri(
            "x:/cafe%CC%81",
            preserve);
        string normalizedIri = converter.ConvertUriToIri(
            "x:/cafe%CC%81",
            normalize);
        IriConversionException diagnosedUri = Assert.Throws<IriConversionException>(() =>
            converter.ConvertUriToIri("x:/cafe%CC%81", diagnose))!;
        string acceptedUri = converter.ConvertUriToIri("x:/caf%C3%A9", diagnose);

        Assert.Multiple(() =>
        {
            Assert.That(preservedUri, Is.EqualTo("x:/cafe%CC%81"));
            Assert.That(normalizedUri, Is.EqualTo("x:/caf%C3%A9"));
            Assert.That(acceptedIri, Is.EqualTo("x:/caf%C3%A9"));
            AssertDiagnostic(
                diagnosedIri.Diagnostic,
                UriDiagnosticCode.NormalizationRequired,
                UriComponent.Path,
                2,
                6,
                IriNormalizerCore.NormalizationRequiredMessage);
            Assert.That(preservedIri, Is.EqualTo("x:/cafe\u0301"));
            Assert.That(normalizedIri, Is.EqualTo("x:/café"));
            Assert.That(acceptedUri, Is.EqualTo("x:/café"));
            AssertException(
                diagnosedUri,
                UriDiagnosticCode.NormalizationRequired,
                UriComponent.Path);
        });
    }

    [Test]
    public void TryConversionRejectsEveryInvalidArgumentAndModeAsAResult()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions missingLimits = new IriConversionOptions();
        missingLimits.Limits = null!;
        IriConversionOptions invalidNormalization = new IriConversionOptions();
        invalidNormalization.NormalizationMode = (UnicodeNormalizationMode)99;
        IriConversionOptions invalidIdn = new IriConversionOptions();
        invalidIdn.IdnMode = (IriIdnMode)99;

        UriTextResult[] results = new UriTextResult[]
        {
            converter.TryConvertIriToUri(null!, new IriConversionOptions()),
            converter.TryConvertIriToUri("x:a", null!),
            converter.TryConvertIriToUri("x:a", missingLimits),
            converter.TryConvertIriToUri("x:a", invalidNormalization),
            converter.TryConvertIriToUri("x:a", invalidIdn)
        };

        foreach (UriTextResult result in results)
        {
            AssertTextFailure(result, UriDiagnosticCode.InvalidInput, UriComponent.Input);
        }
    }

    [Test]
    public void MissingArgumentsThrowFromEveryThrowingConverterMethod()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions missingLimits = new IriConversionOptions();
        missingLimits.Limits = null!;
        TestDelegate[] operations = new TestDelegate[]
        {
            () => converter.ConvertIriToUri(null!, new IriConversionOptions()),
            () => converter.ConvertIriToUri("x:a", null!),
            () => converter.ConvertIriToUri("x:a", missingLimits),
            () => converter.ConvertUriToIri(null!, new IriConversionOptions()),
            () => converter.ConvertUriToIri("x:a", null!),
            () => converter.ConvertUriToIri("x:a", missingLimits),
            () => converter.ConvertHostToAscii(null!, new IriConversionOptions()),
            () => converter.ConvertHostToAscii("host", null!),
            () => converter.ConvertHostToAscii("host", missingLimits),
            () => converter.ConvertHostToUnicode(null!, new IriConversionOptions()),
            () => converter.ConvertHostToUnicode("host", null!),
            () => converter.ConvertHostToUnicode("host", missingLimits)
        };

        foreach (TestDelegate operation in operations)
        {
            IriConversionException exception =
                Assert.Throws<IriConversionException>(operation)!;
            AssertException(
                exception,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
        }
    }

    [Test]
    public void EveryThrowingConverterMethodRetainsItsFailureDiagnostic()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions platform = new IriConversionOptions();
        platform.IdnMode = IriIdnMode.Platform;
        UriTextResult iriResult = converter.TryConvertIriToUri(
            "x:/bad%",
            new IriConversionOptions());

        IriConversionException iri = Assert.Throws<IriConversionException>(() =>
            converter.ConvertIriToUri("x:/bad%", new IriConversionOptions()))!;
        IriConversionException uri = Assert.Throws<IriConversionException>(() =>
            converter.ConvertUriToIri("x:/bad%", new IriConversionOptions()))!;
        IriConversionException ascii = Assert.Throws<IriConversionException>(() =>
            converter.ConvertHostToAscii("-bad", platform))!;
        IriConversionException unicode = Assert.Throws<IriConversionException>(() =>
            converter.ConvertHostToUnicode("xn--a", platform))!;

        Assert.Multiple(() =>
        {
            Assert.That(iri.Diagnostic.Code, Is.EqualTo(iriResult.Diagnostic!.Code));
            Assert.That(iri.Diagnostic.Component, Is.EqualTo(iriResult.Diagnostic.Component));
            Assert.That(iri.Diagnostic.Range.Offset, Is.EqualTo(iriResult.Diagnostic.Range.Offset));
            Assert.That(iri.Diagnostic.Range.Length, Is.EqualTo(iriResult.Diagnostic.Range.Length));
            AssertException(uri, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path);
            AssertException(ascii, UriDiagnosticCode.InvalidIdnLabel, UriComponent.Host);
            AssertException(unicode, UriDiagnosticCode.InvalidIdnLabel, UriComponent.Host);
        });
    }

    [Test]
    public void EveryConversionDirectionHonorsExactInputAndOutputBoundaries()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions iriExact = CreateConversionOptions(3, 8, IriIdnMode.None);
        IriConversionOptions iriInputShort = CreateConversionOptions(2, 8, IriIdnMode.None);
        IriConversionOptions iriOutputShort = CreateConversionOptions(3, 7, IriIdnMode.None);
        IriConversionOptions uriExact = CreateConversionOptions(8, 3, IriIdnMode.None);
        IriConversionOptions uriInputShort = CreateConversionOptions(7, 3, IriIdnMode.None);
        IriConversionOptions uriOutputShort = CreateConversionOptions(8, 2, IriIdnMode.None);
        IriConversionOptions asciiExact = CreateConversionOptions(6, 12, IriIdnMode.Platform);
        IriConversionOptions asciiInputShort = CreateConversionOptions(5, 12, IriIdnMode.Platform);
        IriConversionOptions asciiOutputShort = CreateConversionOptions(6, 11, IriIdnMode.Platform);
        IriConversionOptions unicodeExact = CreateConversionOptions(12, 6, IriIdnMode.Platform);
        IriConversionOptions unicodeInputShort = CreateConversionOptions(11, 6, IriIdnMode.Platform);
        IriConversionOptions unicodeOutputShort = CreateConversionOptions(12, 5, IriIdnMode.Platform);

        Assert.That(converter.ConvertIriToUri("x:é", iriExact), Is.EqualTo("x:%C3%A9"));
        AssertLimitException(
            () => converter.ConvertIriToUri("x:é", iriInputShort),
            UriComponent.Input);
        AssertLimitException(
            () => converter.ConvertIriToUri("x:é", iriOutputShort),
            UriComponent.Output);

        Assert.That(converter.ConvertUriToIri("x:%C3%A9", uriExact), Is.EqualTo("x:é"));
        AssertLimitException(
            () => converter.ConvertUriToIri("x:%C3%A9", uriInputShort),
            UriComponent.Input);
        AssertLimitException(
            () => converter.ConvertUriToIri("x:%C3%A9", uriOutputShort),
            UriComponent.Output);

        Assert.That(converter.ConvertHostToAscii("é.test", asciiExact), Is.EqualTo("xn--9ca.test"));
        AssertLimitException(
            () => converter.ConvertHostToAscii("é.test", asciiInputShort),
            UriComponent.Input);
        AssertLimitException(
            () => converter.ConvertHostToAscii("é.test", asciiOutputShort),
            UriComponent.Output);

        Assert.That(converter.ConvertHostToUnicode("xn--9ca.test", unicodeExact), Is.EqualTo("é.test"));
        AssertLimitException(
            () => converter.ConvertHostToUnicode("xn--9ca.test", unicodeInputShort),
            UriComponent.Input);
        AssertLimitException(
            () => converter.ConvertHostToUnicode("xn--9ca.test", unicodeOutputShort),
            UriComponent.Output);
    }

    [Test]
    public void NormalizerExposesEveryModeIncludingDiagnoseOnlyBehavior()
    {
        IIriNormalizer normalizer = new IriNormalizer();
        IriConversionOptions preserve = new IriConversionOptions();
        IriConversionOptions normalize = new IriConversionOptions();
        normalize.NormalizationMode = UnicodeNormalizationMode.NormalizeNfc;
        IriConversionOptions diagnose = new IriConversionOptions();
        diagnose.NormalizationMode = UnicodeNormalizationMode.DiagnoseNfc;
        const string decomposed = "x:/cafe\u0301";

        string preserved = normalizer.NormalizeIri(decomposed, preserve);
        string normalized = normalizer.NormalizeIri(decomposed, normalize);
        string accepted = normalizer.NormalizeIri("x:/café", diagnose);
        IriConversionException required = Assert.Throws<IriConversionException>(() =>
            normalizer.NormalizeIri(decomposed, diagnose))!;
        UriDiagnostic? diagnostic = normalizer.DiagnoseIriNormalization(
            decomposed,
            normalize);
        UriDiagnostic? valid = normalizer.DiagnoseIriNormalization(
            "x:/café",
            preserve);

        Assert.Multiple(() =>
        {
            Assert.That(preserved, Is.EqualTo(decomposed));
            Assert.That(normalized, Is.EqualTo("x:/café"));
            Assert.That(accepted, Is.EqualTo("x:/café"));
            AssertException(
                required,
                UriDiagnosticCode.NormalizationRequired,
                UriComponent.Path);
            AssertDiagnostic(
                diagnostic,
                UriDiagnosticCode.NormalizationRequired,
                UriComponent.Path,
                2,
                6,
                IriNormalizerCore.NormalizationRequiredMessage);
            Assert.That(valid, Is.Null);
        });
    }

    [Test]
    public void NormalizerMethodsHandleEveryArgumentModeSyntaxAndLimitFailure()
    {
        IIriNormalizer normalizer = new IriNormalizer();
        IriConversionOptions missingLimits = new IriConversionOptions();
        missingLimits.Limits = null!;
        IriConversionOptions invalidNormalization = new IriConversionOptions();
        invalidNormalization.NormalizationMode = (UnicodeNormalizationMode)99;
        IriConversionOptions invalidIdn = new IriConversionOptions();
        invalidIdn.IdnMode = (IriIdnMode)99;
        IriConversionOptions input = CreateConversionOptions(2, 10, IriIdnMode.None);
        IriConversionOptions output = CreateConversionOptions(3, 2, IriIdnMode.None);
        output.NormalizationMode = UnicodeNormalizationMode.NormalizeNfc;

        TestDelegate[] throwing = new TestDelegate[]
        {
            () => normalizer.NormalizeIri(null!, new IriConversionOptions()),
            () => normalizer.NormalizeIri("x:a", null!),
            () => normalizer.NormalizeIri("x:a", missingLimits),
            () => normalizer.NormalizeIri("x:a", invalidNormalization),
            () => normalizer.NormalizeIri("x:a", invalidIdn)
        };
        foreach (TestDelegate operation in throwing)
        {
            IriConversionException exception =
                Assert.Throws<IriConversionException>(operation)!;
            AssertException(
                exception,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
        }

        UriDiagnostic?[] resultFailures = new UriDiagnostic?[]
        {
            normalizer.DiagnoseIriNormalization(null!, new IriConversionOptions()),
            normalizer.DiagnoseIriNormalization("x:a", null!),
            normalizer.DiagnoseIriNormalization("x:a", missingLimits),
            normalizer.DiagnoseIriNormalization("x:a", invalidNormalization),
            normalizer.DiagnoseIriNormalization("x:a", invalidIdn)
        };
        foreach (UriDiagnostic? diagnostic in resultFailures)
        {
            AssertInvalidInput(diagnostic);
        }

        IriConversionException syntax = Assert.Throws<IriConversionException>(() =>
            normalizer.NormalizeIri("x:/bad%", new IriConversionOptions()))!;
        UriDiagnostic? diagnosedSyntax = normalizer.DiagnoseIriNormalization(
            "x:/bad%",
            new IriConversionOptions());
        IriConversionException inputLimit = Assert.Throws<IriConversionException>(() =>
            normalizer.NormalizeIri("x:é", input))!;
        IriConversionException outputLimit = Assert.Throws<IriConversionException>(() =>
            normalizer.NormalizeIri("x:é", output))!;

        Assert.Multiple(() =>
        {
            AssertException(syntax, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path);
            AssertDiagnostic(
                diagnosedSyntax,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.Path,
                6,
                1,
                DiagnosticFactory.InvalidPercentEncodingMessage);
            AssertException(inputLimit, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
            AssertException(outputLimit, UriDiagnosticCode.LimitExceeded, UriComponent.Output);
        });
    }

    [TestCase("x:a", "y:a", UriComponent.Scheme)]
    [TestCase("//host/a", "/a", UriComponent.Authority)]
    [TestCase("//usér@host/", "//autre@host/", UriComponent.UserInfo)]
    [TestCase("//é.test/", "//è.test/", UriComponent.Host)]
    [TestCase("//host:80/", "//host:81/", UriComponent.Port)]
    [TestCase("/café", "/thé", UriComponent.Path)]
    [TestCase("/path?clé", "/path?autre", UriComponent.Query)]
    [TestCase("/path#clé", "/path#autre", UriComponent.Fragment)]
    public void PublicExactComparisonReportsEveryDifferentComponent(
        string left,
        string right,
        UriComponent component)
    {
        IIriComparer comparer = new IriComparer();

        UriComparisonResult forward = comparer.CompareIriExact(
            left,
            right,
            new UriComparisonOptions());
        UriComparisonResult reverse = comparer.CompareIriExact(
            right,
            left,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            AssertComparisonDifference(forward, UriComparisonLevel.Exact, component);
            AssertComparisonDifference(reverse, UriComparisonLevel.Exact, component);
        });
    }

    [Test]
    public void EveryComparisonLevelReturnsCompleteFailureDiagnostics()
    {
        IIriComparer comparer = new IriComparer();

        UriComparisonResult exact = comparer.CompareIriExact(
            "x:/bad%",
            "x:a",
            new UriComparisonOptions());
        UriComparisonResult unicode = comparer.CompareIriUnicode(
            "x:a",
            "x:/bad%",
            new UriComparisonOptions());
        UriComparisonResult mapped = comparer.CompareIriAsUri(
            "x:/bad%",
            "x:a",
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            AssertComparisonFailure(
                exact,
                UriComparisonLevel.Exact,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.Path,
                6,
                1,
                DiagnosticFactory.InvalidPercentEncodingMessage);
            AssertComparisonFailure(
                unicode,
                UriComparisonLevel.IriUnicode,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.Path,
                6,
                1,
                DiagnosticFactory.InvalidPercentEncodingMessage);
            AssertComparisonFailure(
                mapped,
                UriComparisonLevel.IriMappedUri,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.Path,
                6,
                1,
                DiagnosticFactory.InvalidPercentEncodingMessage);
        });
    }

    [Test]
    public void MissingArgumentsFailEveryComparerMethodWithoutThrowing()
    {
        IIriComparer comparer = new IriComparer();
        UriComparisonOptions missingLimits = new UriComparisonOptions();
        missingLimits.Limits = null!;
        UriComparisonResult[] results = new UriComparisonResult[]
        {
            comparer.CompareIriExact(null!, "x:a", new UriComparisonOptions()),
            comparer.CompareIriExact("x:a", null!, new UriComparisonOptions()),
            comparer.CompareIriExact("x:a", "x:a", null!),
            comparer.CompareIriExact("x:a", "x:a", missingLimits),
            comparer.CompareIriUnicode(null!, "x:a", new UriComparisonOptions()),
            comparer.CompareIriUnicode("x:a", null!, new UriComparisonOptions()),
            comparer.CompareIriUnicode("x:a", "x:a", null!),
            comparer.CompareIriUnicode("x:a", "x:a", missingLimits),
            comparer.CompareIriAsUri(null!, "x:a", new UriComparisonOptions()),
            comparer.CompareIriAsUri("x:a", null!, new UriComparisonOptions()),
            comparer.CompareIriAsUri("x:a", "x:a", null!),
            comparer.CompareIriAsUri("x:a", "x:a", missingLimits)
        };
        UriComparisonLevel[] levels = new UriComparisonLevel[]
        {
            UriComparisonLevel.Exact,
            UriComparisonLevel.Exact,
            UriComparisonLevel.Exact,
            UriComparisonLevel.Exact,
            UriComparisonLevel.IriUnicode,
            UriComparisonLevel.IriUnicode,
            UriComparisonLevel.IriUnicode,
            UriComparisonLevel.IriUnicode,
            UriComparisonLevel.IriMappedUri,
            UriComparisonLevel.IriMappedUri,
            UriComparisonLevel.IriMappedUri,
            UriComparisonLevel.IriMappedUri
        };

        for (int index = 0; index < results.Length; index += 1)
        {
            AssertComparisonFailure(
                results[index],
                levels[index],
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input,
                0,
                0,
                DiagnosticFactory.InvalidInputMessage);
        }
    }

    [Test]
    public void EveryComparisonLevelHonorsItsRelevantInputAndOutputLimits()
    {
        IIriComparer comparer = new IriComparer();
        UriComparisonOptions input = new UriComparisonOptions();
        input.Limits.MaxInputCharacters = 2;
        UriComparisonOptions exactOutput = new UriComparisonOptions();
        exactOutput.Limits.MaxOutputCharacters = 0;
        UriComparisonOptions unicodeOutput = new UriComparisonOptions();
        unicodeOutput.Limits.MaxOutputCharacters = 2;
        UriComparisonOptions mappedOutput = new UriComparisonOptions();
        mappedOutput.Limits.MaxOutputCharacters = 7;

        UriComparisonResult exactInput = comparer.CompareIriExact(
            "x:é",
            "x:é",
            input);
        UriComparisonResult unicodeInput = comparer.CompareIriUnicode(
            "x:é",
            "x:é",
            input);
        UriComparisonResult mappedInput = comparer.CompareIriAsUri(
            "x:é",
            "x:é",
            input);
        UriComparisonResult exactIgnoresOutput = comparer.CompareIriExact(
            "x:é",
            "x:é",
            exactOutput);
        UriComparisonResult unicodeOutputFailure = comparer.CompareIriUnicode(
            "x:é",
            "x:é",
            unicodeOutput);
        UriComparisonResult mappedOutputFailure = comparer.CompareIriAsUri(
            "x:é",
            "x:é",
            mappedOutput);

        Assert.Multiple(() =>
        {
            AssertComparisonLimit(exactInput, UriComparisonLevel.Exact, UriComponent.Input);
            AssertComparisonLimit(unicodeInput, UriComparisonLevel.IriUnicode, UriComponent.Input);
            AssertComparisonLimit(mappedInput, UriComparisonLevel.IriMappedUri, UriComponent.Input);
            AssertComparisonEquality(exactIgnoresOutput, UriComparisonLevel.Exact);
            AssertComparisonLimit(
                unicodeOutputFailure,
                UriComparisonLevel.IriUnicode,
                UriComponent.Output);
            AssertComparisonLimit(
                mappedOutputFailure,
                UriComparisonLevel.IriMappedUri,
                UriComponent.Output);
        });
    }

    private static IriConversionOptions CreateConversionOptions(
        int maximumInput,
        int maximumOutput,
        IriIdnMode idnMode)
    {
        IriConversionOptions options = new IriConversionOptions();
        options.Limits.MaxInputCharacters = maximumInput;
        options.Limits.MaxOutputCharacters = maximumOutput;
        options.IdnMode = idnMode;
        return options;
    }

    private static void AssertParseSuccess(
        IriParseResult result,
        UriReferenceKind kind,
        string text)
    {
        Assert.That(result.Success, Is.True, text);
        Assert.That(result.Reference.Kind, Is.EqualTo(kind), text);
        Assert.That(result.Reference.OriginalText, Is.EqualTo(text), text);
        Assert.That(result.Diagnostic, Is.Null, text);
    }

    private static void AssertTextFailure(
        UriTextResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Length, Is.EqualTo(0));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }

    private static void AssertInputLimit(
        UriDiagnostic? diagnostic,
        int offset,
        int length)
    {
        AssertDiagnostic(
            diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input,
            offset,
            length,
            LimitGuard.InputLimitMessage);
    }

    private static void AssertInvalidInput(UriDiagnostic? diagnostic)
    {
        AssertDiagnostic(
            diagnostic,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input,
            0,
            0,
            DiagnosticFactory.InvalidInputMessage);
    }

    private static void AssertInvalidInputException(TestDelegate operation)
    {
        IriSyntaxException exception = Assert.Throws<IriSyntaxException>(operation)!;
        Assert.That(exception.Diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Iri));
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
        Assert.That(exception.Diagnostic.Component, Is.EqualTo(UriComponent.Input));
        Assert.That(exception.Message, Is.EqualTo(exception.Diagnostic.Message));
    }

    private static void AssertLimitException(
        TestDelegate operation,
        UriComponent component)
    {
        IriConversionException exception =
            Assert.Throws<IriConversionException>(operation)!;
        AssertException(exception, UriDiagnosticCode.LimitExceeded, component);
    }

    private static void AssertException(
        IriConversionException exception,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(exception.Diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Iri));
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(exception.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(exception.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(exception.Message, Is.EqualTo(exception.Diagnostic.Message));
    }

    private static void AssertComparisonEquality(
        UriComparisonResult result,
        UriComparisonLevel level)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Equal, Is.True);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertComparisonDifference(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriComponent component)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Equal, Is.False);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(component));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertComparisonLimit(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Equal, Is.False);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }

    private static void AssertComparisonFailure(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Equal, Is.False);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
        AssertDiagnostic(result.Diagnostic, code, component, offset, length, message);
    }

    private static void AssertDiagnostic(
        UriDiagnostic? diagnostic,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diagnostic.Component, Is.EqualTo(component));
        Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(diagnostic.Message, Is.EqualTo(message));
    }
}
