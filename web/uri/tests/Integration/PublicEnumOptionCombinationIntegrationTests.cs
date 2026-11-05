namespace uri.Tests;

public class PublicEnumOptionCombinationIntegrationTests
{
    [Test]
    public void EveryPublicEnumMemberRoundTripsThroughItsRuntimeName()
    {
        System.Type[] enumTypes = typeof(UriParser).Assembly.GetTypes()
            .Where(type =>
                type.IsPublic &&
                type.IsEnum &&
                type.Namespace == "uri")
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.That(enumTypes, Is.Not.Empty);
        foreach (System.Type enumType in enumTypes)
        {
            System.Array values = System.Enum.GetValues(enumType);
            Assert.That(values.Length, Is.GreaterThan(0), enumType.Name);
            foreach (object value in values)
            {
                string name = System.Enum.GetName(enumType, value)!;
                object parsed = System.Enum.Parse(enumType, name);

                Assert.Multiple(() =>
                {
                    Assert.That(name, Is.Not.Empty, enumType.Name);
                    Assert.That(System.Enum.IsDefined(enumType, value),
                        Is.True, enumType.Name + "." + name);
                    Assert.That(parsed, Is.EqualTo(value),
                        enumType.Name + "." + name);
                });
            }
        }
    }

    [Test]
    public void EveryIriNormalizationAndIdnModeCombinationChangesPredictably()
    {
        const string input = "https://例え.test/path/cafe\u0301";
        const string unicodeHost =
            "%E4%BE%8B%E3%81%88.test";
        const string asciiHost = "xn--r8jz45g.test";
        IIriConverter converter = new IriConverter();

        foreach (UnicodeNormalizationMode normalizationMode in
            System.Enum.GetValues<UnicodeNormalizationMode>())
        {
            foreach (IriIdnMode idnMode in
                System.Enum.GetValues<IriIdnMode>())
            {
                IriConversionOptions options = new IriConversionOptions();
                options.NormalizationMode = normalizationMode;
                options.IdnMode = idnMode;

                UriTextResult result = converter.TryConvertIriToUri(
                    input,
                    options);
                string label = normalizationMode + "/" + idnMode;
                if (normalizationMode ==
                    UnicodeNormalizationMode.DiagnoseNfc)
                {
                    Assert.That(result.Diagnostic, Is.Not.Null, label);
                    Assert.Multiple(() =>
                    {
                        Assert.That(result.Success, Is.False, label);
                        Assert.That(result.Diagnostic!.Code, Is.EqualTo(
                            UriDiagnosticCode.NormalizationRequired), label);
                        Assert.That(result.Diagnostic.Component, Is.EqualTo(
                            UriComponent.Path), label);
                    });
                    continue;
                }

                string expectedPath = "cafe%CC%81";
                if (normalizationMode ==
                    UnicodeNormalizationMode.NormalizeNfc)
                {
                    expectedPath = "caf%C3%A9";
                }

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, label);
                    Assert.That(result.Text, Is.EqualTo(
                        "https://" + unicodeHost + "/path/" + expectedPath),
                        label);
                    Assert.That(result.Diagnostic, Is.Null, label);
                });
            }
        }

        IriConversionOptions noIdn = new IriConversionOptions();
        IriConversionException unavailable = Assert.Throws<
            IriConversionException>(() => converter.ConvertHostToAscii(
                "例え.test",
                noIdn))!;
        IriConversionOptions platform = new IriConversionOptions();
        platform.IdnMode = IriIdnMode.Platform;
        string mapped = converter.ConvertHostToAscii("例え.test", platform);

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidInput));
            Assert.That(mapped, Is.EqualTo(asciiHost));
        });
    }

    [Test]
    public void QueryDelimiterAndFormEncodingOptionsComposeAsACrossProduct()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        char[] delimiters = new char[] { '&', ';' };
        bool[] formModes = new bool[] { false, true };
        UriQueryParameter[] parameters = new UriQueryParameter[]
        {
            new UriQueryParameter("a b", true, "c+d e"),
            new UriQueryParameter("flag", false, "")
        };

        foreach (char delimiter in delimiters)
        {
            foreach (bool formMode in formModes)
            {
                UriQueryOptions options = new UriQueryOptions();
                options.PairDelimiter = delimiter;
                options.FormUrlEncoded = formMode;

                UriQueryResult composed = codec.ComposeQuery(
                    parameters,
                    options);
                string firstPair = "a%20b=c+d%20e";
                if (formMode)
                {
                    firstPair = "a+b=c%2Bd+e";
                }

                string expected = firstPair + delimiter + "flag";
                UriQueryResult parsed = codec.ParseQuery(expected, options);
                string label = delimiter + "/" + formMode;

                Assert.Multiple(() =>
                {
                    Assert.That(composed.Success, Is.True, label);
                    Assert.That(composed.Text, Is.EqualTo(expected), label);
                    Assert.That(composed.PairDelimiter, Is.EqualTo(delimiter),
                        label);
                    Assert.That(parsed.Success, Is.True, label);
                    Assert.That(parsed.Parameters, Has.Length.EqualTo(2),
                        label);
                    Assert.That(parsed.Parameters[0].Name, Is.EqualTo("a b"),
                        label);
                    Assert.That(parsed.Parameters[0].Value,
                        Is.EqualTo("c+d e"), label);
                });
            }
        }
    }

    [Test]
    public void EveryRelativizationModeAndSupportedComparisonLevelCompose()
    {
        const string baseUri = "http://example.com/a/b/c/d";
        const string targetUri = "http://example.com/x";
        IUriRelativizer relativizer = new UriRelativizer();
        UriComparisonLevel[] supportedLevels = new UriComparisonLevel[]
        {
            UriComparisonLevel.Exact,
            UriComparisonLevel.Syntax,
            UriComparisonLevel.SchemeProfile
        };

        foreach (UriRelativizationMode mode in
            System.Enum.GetValues<UriRelativizationMode>())
        {
            foreach (UriComparisonLevel comparisonLevel in supportedLevels)
            {
                UriRelativizationOptions options =
                    new UriRelativizationOptions();
                options.Mode = mode;
                options.ComparisonLevel = comparisonLevel;

                UriTextResult result = relativizer.TryRelativizeReference(
                    baseUri,
                    targetUri,
                    options);
                string label = mode + "/" + comparisonLevel;

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, label);
                    Assert.That(result.Text, Is.EqualTo(
                        ExpectedRelativization(mode)), label);
                    Assert.That(result.Diagnostic, Is.Null, label);
                });
            }
        }

        UriComparisonLevel[] iriOnlyLevels = new UriComparisonLevel[]
        {
            UriComparisonLevel.IriUnicode,
            UriComparisonLevel.IriMappedUri
        };
        foreach (UriComparisonLevel comparisonLevel in iriOnlyLevels)
        {
            UriRelativizationOptions options =
                new UriRelativizationOptions();
            options.ComparisonLevel = comparisonLevel;
            UriTextResult result = relativizer.TryRelativizeReference(
                baseUri,
                targetUri,
                options);

            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, comparisonLevel.ToString());
                Assert.That(result.Diagnostic!.Code, Is.EqualTo(
                    UriDiagnosticCode.InvalidInput),
                    comparisonLevel.ToString());
            });
        }
    }

    [Test]
    public void EveryUrnQueryAndFragmentCollisionModeCombinationIsIndependent()
    {
        IUrnLocatorUtility utility = new UrnLocatorUtility();
        foreach (UrnLocatorCollisionMode queryMode in
            System.Enum.GetValues<UrnLocatorCollisionMode>())
        {
            foreach (UrnLocatorCollisionMode fragmentMode in
                System.Enum.GetValues<UrnLocatorCollisionMode>())
            {
                UrnLocatorMergeOptions options =
                    new UrnLocatorMergeOptions();
                options.QueryCollisionMode = queryMode;
                options.FragmentCollisionMode = fragmentMode;
                string label = queryMode + "/" + fragmentMode;

                if (queryMode == UrnLocatorCollisionMode.Reject)
                {
                    UriResolutionException exception = Assert.Throws<
                        UriResolutionException>(() =>
                            utility.ApplyUrnComponentsToLocator(
                                "urn:example:value?=urn-query#urn-fragment",
                                "https://example.test/?locator-query" +
                                "#locator-fragment",
                                options))!;
                    Assert.That(exception.Diagnostic.Component,
                        Is.EqualTo(UriComponent.Query), label);
                    continue;
                }

                if (fragmentMode == UrnLocatorCollisionMode.Reject)
                {
                    UriResolutionException exception = Assert.Throws<
                        UriResolutionException>(() =>
                            utility.ApplyUrnComponentsToLocator(
                                "urn:example:value?=urn-query#urn-fragment",
                                "https://example.test/?locator-query" +
                                "#locator-fragment",
                                options))!;
                    Assert.That(exception.Diagnostic.Component,
                        Is.EqualTo(UriComponent.Fragment), label);
                    continue;
                }

                string query = SelectCollisionValue(
                    queryMode,
                    "locator-query",
                    "urn-query");
                string fragment = SelectCollisionValue(
                    fragmentMode,
                    "locator-fragment",
                    "urn-fragment");
                string locator = utility.ApplyUrnComponentsToLocator(
                    "urn:example:value?=urn-query#urn-fragment",
                    "https://example.test/?locator-query#locator-fragment",
                    options);

                Assert.That(locator, Is.EqualTo(
                    "https://example.test/?" + query + "#" + fragment),
                    label);
            }
        }
    }

    [Test]
    public void ValidationRequirementsComposeAndRejectImpossibleCombination()
    {
        IUriValidator validator = new UriValidator();
        UriValidationOptions combined = new UriValidationOptions();
        combined.RequireScheme = true;
        combined.RequireAuthority = true;
        combined.RequireHost = true;
        combined.RequirePath = true;
        combined.RequireQuery = true;
        combined.RequireFragment = true;
        UriValidationOptions absolute = new UriValidationOptions();
        absolute.RequireAbsoluteUri = true;
        absolute.RequireAuthority = true;
        absolute.RequireHost = true;
        UriValidationOptions incompatible = new UriValidationOptions();
        incompatible.RequireAbsoluteUri = true;
        incompatible.RequireFragment = true;

        UriValidationResult combinedResult = validator.ValidateReference(
            "x://host/path?query#fragment",
            combined);
        UriValidationResult absoluteResult = validator.ValidateReference(
            "x://host/path?query",
            absolute);
        UriValidationResult textFailure = validator.ValidateReference(
            "x:path",
            incompatible);
        UriReference reference = new UriParser().ParseReference(
            "x:path",
            new UriParseOptions());
        UriValidationResult modelFailure = validator.ValidateComponents(
            reference,
            incompatible);
        UriDiagnostic[] allFailures = validator.ValidateAllDiagnostics(
            reference,
            incompatible);

        Assert.Multiple(() =>
        {
            Assert.That(combinedResult.Success, Is.True);
            Assert.That(absoluteResult.Success, Is.True);
            AssertIncompatibleOptions(textFailure.Diagnostic);
            AssertIncompatibleOptions(modelFailure.Diagnostic);
            Assert.That(allFailures, Has.Length.EqualTo(1));
            AssertIncompatibleOptions(allFailures[0]);
        });
    }

    private static string ExpectedRelativization(UriRelativizationMode mode)
    {
        if (mode == UriRelativizationMode.Shortest ||
            mode == UriRelativizationMode.AbsolutePath)
        {
            return "/x";
        }

        if (mode == UriRelativizationMode.NetworkPath)
        {
            return "//example.com/x";
        }

        if (mode == UriRelativizationMode.AbsoluteUri)
        {
            return "http://example.com/x";
        }

        return "../../../x";
    }

    private static string SelectCollisionValue(
        UrnLocatorCollisionMode mode,
        string locatorValue,
        string urnValue)
    {
        if (mode == UrnLocatorCollisionMode.ReplaceLocator)
        {
            return urnValue;
        }

        return locatorValue;
    }

    private static void AssertIncompatibleOptions(UriDiagnostic? diagnostic)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.IncompatibleOptions));
            Assert.That(diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error));
            Assert.That(diagnostic.Component, Is.EqualTo(UriComponent.Input));
            Assert.That(diagnostic.Range.Offset, Is.Zero);
            Assert.That(diagnostic.Range.Length, Is.Zero);
            Assert.That(diagnostic.Message, Is.EqualTo(
                DiagnosticFactory.IncompatibleOptionsMessage));
        });
    }
}
