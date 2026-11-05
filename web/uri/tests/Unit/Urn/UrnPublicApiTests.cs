namespace uri.Tests;

public class UrnPublicApiTests
{
    [Test]
    public void ParserExposesThrowingStringAndSpanPaths()
    {
        IUrnParser parser = new UrnParser();
        UrnParseOptions options = new UrnParseOptions();

        Urn parsed = parser.ParseUrn(
            "URN:Example:value?+r?=q#f",
            options);
        UrnParseResult stringResult = parser.TryParseUrn(
            "urn:example:string",
            options);
        UrnParseResult spanResult = parser.TryParseUrnSpan(
            "urn:example:span".AsSpan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Scheme, Is.EqualTo("URN"));
            Assert.That(parsed.NamespaceIdentifier, Is.EqualTo("Example"));
            Assert.That(parsed.NamespaceSpecificString, Is.EqualTo("value"));
            Assert.That(parsed.RComponent, Is.EqualTo("r"));
            Assert.That(parsed.QComponent, Is.EqualTo("q"));
            Assert.That(parsed.FComponent, Is.EqualTo("f"));
            Assert.That(stringResult.Success, Is.True);
            Assert.That(stringResult.Urn.NamespaceSpecificString,
                Is.EqualTo("string"));
            Assert.That(spanResult.Success, Is.True);
            Assert.That(spanResult.Urn.NamespaceSpecificString,
                Is.EqualTo("span"));
        });
    }

    [Test]
    public void ParserReturnsFailuresAndThrowsTypedSyntaxExceptions()
    {
        IUrnParser parser = new UrnParser();
        UrnParseOptions options = new UrnParseOptions();

        UrnParseResult invalid = parser.TryParseUrn(
            "http://example.test",
            options);
        UrnParseResult missing = parser.TryParseUrn(null!, options);
        UrnSyntaxException exception = Assert.Throws<UrnSyntaxException>(() =>
            parser.ParseUrn("urn:x:value", options))!;

        Assert.Multiple(() =>
        {
            AssertParseFailure(
                invalid,
                UriDiagnosticCode.InvalidUrn,
                UriComponent.Urn);
            AssertParseFailure(
                missing,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidNamespaceIdentifier));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(
                UriComponent.UrnNamespaceIdentifier));
        });
    }

    [Test]
    public void ParserAppliesValidationModeLimitsAndNamespaceHandler()
    {
        IUrnParser parser = new UrnParser();
        UrnParseOptions registered = new UrnParseOptions();
        registered.ValidationMode = UrnValidationMode.RegisteredNamespace;
        UrnParseOptions limited = new UrnParseOptions();
        limited.Limits.MaxInputCharacters = 5;
        RecordingNamespaceHandler handler =
            new RecordingNamespaceHandler();
        handler.RejectValidation = true;
        UrnParseOptions handled = new UrnParseOptions();
        handled.NamespaceHandler = handler;

        UrnParseResult registration = parser.TryParseUrn(
            "urn:x-test:value",
            registered);
        UrnParseResult limit = parser.TryParseUrn(
            "urn:example:value",
            limited);
        UrnParseResult namespaceResult = parser.TryParseUrn(
            "URN:EXAMPLE:value",
            handled);

        Assert.Multiple(() =>
        {
            AssertParseFailure(
                registration,
                UriDiagnosticCode.InvalidNamespaceIdentifier,
                UriComponent.UrnNamespaceIdentifier);
            AssertParseFailure(
                limit,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Input);
            AssertParseFailure(
                namespaceResult,
                UriDiagnosticCode.UnsupportedNamespace,
                UriComponent.UrnNamespaceSpecificString);
            Assert.That(handler.ValidationCount, Is.EqualTo(1));
            Assert.That(handler.NamespaceIdentifier, Is.EqualTo("example"));
            Assert.That(handler.ValidatedValue, Is.EqualTo("value"));
        });
    }

    [Test]
    public void NormalizerExposesCompleteAndAssignedNameOperations()
    {
        IUrnNormalizer normalizer = new UrnNormalizer();
        UrnParseOptions options = new UrnParseOptions();
        const string input = "URN:Example:A%af?+R%af?=Q%bc#F%cd";

        string complete = normalizer.NormalizeUrn(input, options);
        string assignedName = normalizer.NormalizeAssignedName(
            input,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(complete, Is.EqualTo(
                "urn:example:A%AF?+R%af?=Q%bc#F%cd"));
            Assert.That(assignedName, Is.EqualTo("urn:example:A%AF"));
        });
    }

    [Test]
    public void NormalizerUsesAnExplicitNamespaceHandler()
    {
        RecordingNamespaceHandler handler =
            new RecordingNamespaceHandler();
        handler.NormalizedValue = "Canonical%ab";
        UrnParseOptions options = new UrnParseOptions();
        options.NamespaceHandler = handler;
        IUrnNormalizer normalizer = new UrnNormalizer();

        string complete = normalizer.NormalizeUrn(
            "URN:EXAMPLE:source?=Query%af",
            options);
        string assignedName = normalizer.NormalizeAssignedName(
            "URN:EXAMPLE:source#fragment",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(complete, Is.EqualTo(
                "urn:example:Canonical%AB?=Query%af"));
            Assert.That(assignedName, Is.EqualTo(
                "urn:example:Canonical%AB"));
            Assert.That(handler.NormalizationCount, Is.EqualTo(2));
            Assert.That(handler.NamespaceIdentifier, Is.EqualTo("example"));
            Assert.That(handler.NormalizedInput, Is.EqualTo("source"));
        });
    }

    [Test]
    public void NormalizerThrowsTypedExceptionsForSyntaxAndLimits()
    {
        IUrnNormalizer normalizer = new UrnNormalizer();
        UrnParseOptions options = new UrnParseOptions();
        UrnParseOptions limited = new UrnParseOptions();
        limited.Limits.MaxOutputCharacters = 5;

        UrnEquivalenceException syntax =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeUrn("not-a-urn", options))!;
        UrnEquivalenceException limit =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeAssignedName(
                    "urn:example:value",
                    limited))!;

        Assert.Multiple(() =>
        {
            Assert.That(syntax.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidUrn));
            Assert.That(limit.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(limit.Diagnostic.Component, Is.EqualTo(
                UriComponent.Output));
        });
    }

    [Test]
    public void NormalizerAppliesComponentLimitsToNamespaceHandlerOutput()
    {
        RecordingNamespaceHandler handler =
            new RecordingNamespaceHandler();
        handler.NormalizedValue = "long-value";
        UrnParseOptions options = new UrnParseOptions();
        options.NamespaceHandler = handler;
        options.Limits.MaxComponentCharacters = 4;
        IUrnNormalizer normalizer = new UrnNormalizer();

        UrnEquivalenceException exception =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeAssignedName(
                    "urn:ex:a",
                    options))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(
                UriComponent.UrnNamespaceSpecificString));
        });
    }

    [Test]
    public void ComparerExposesAllThreeEquivalenceLevels()
    {
        IUrnComparer comparer = new UrnComparer();
        UrnParseOptions options = new UrnParseOptions();
        RecordingNamespaceHandler handler =
            new RecordingNamespaceHandler();
        handler.ComparisonEqual = true;
        UrnParseOptions namespaceOptions = new UrnParseOptions();
        namespaceOptions.NamespaceHandler = handler;

        UrnEquivalenceResult exact = comparer.CompareUrnExact(
            "URN:example:a",
            "urn:example:a",
            options);
        UrnEquivalenceResult assigned =
            comparer.CompareUrnAssignedName(
                "URN:EXAMPLE:a%2f?+r",
                "urn:example:a%2F?=q#f",
                options);
        UrnEquivalenceResult specific =
            comparer.CompareUrnNamespaceSpecific(
                "urn:example:Ab-C",
                "urn:example:abc",
                namespaceOptions);

        Assert.Multiple(() =>
        {
            AssertComparison(
                exact,
                false,
                UrnEquivalenceLevel.Exact,
                UriComponent.Scheme);
            AssertComparison(
                assigned,
                true,
                UrnEquivalenceLevel.AssignedName,
                UriComponent.None);
            AssertComparison(
                specific,
                true,
                UrnEquivalenceLevel.NamespaceSpecific,
                UriComponent.None);
            Assert.That(handler.ComparisonCount, Is.EqualTo(1));
            Assert.That(handler.NamespaceIdentifier, Is.EqualTo("example"));
        });
    }

    [Test]
    public void ComparerReportsMissingHandlerParseAndLimitFailures()
    {
        IUrnComparer comparer = new UrnComparer();
        UrnParseOptions options = new UrnParseOptions();
        UrnParseOptions limited = new UrnParseOptions();
        limited.Limits.MaxInputCharacters = 5;

        UrnEquivalenceResult missingHandler =
            comparer.CompareUrnNamespaceSpecific(
                "urn:example:a",
                "urn:example:A",
                options);
        UrnEquivalenceResult invalid = comparer.CompareUrnExact(
            "not-a-urn",
            "urn:example:a",
            options);
        UrnEquivalenceResult limit =
            comparer.CompareUrnAssignedName(
                "urn:example:a",
                "urn:example:a",
                limited);

        Assert.Multiple(() =>
        {
            AssertComparisonFailure(
                missingHandler,
                UrnEquivalenceLevel.NamespaceSpecific,
                UriDiagnosticCode.UnsupportedNamespace,
                UriComponent.UrnNamespaceSpecificString);
            AssertComparisonFailure(
                invalid,
                UrnEquivalenceLevel.Exact,
                UriDiagnosticCode.InvalidUrn,
                UriComponent.Urn);
            AssertComparisonFailure(
                limit,
                UrnEquivalenceLevel.AssignedName,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Input);
        });
    }

    [Test]
    public void LocatorUtilityAppliesQAndFAndSeparatesAssignedName()
    {
        IUrnLocatorUtility utility = new UrnLocatorUtility();

        string locator = utility.ApplyUrnComponentsToLocator(
            "urn:example:weather?+resolver?=op=map#part",
            "https://weather.example/report",
            new UrnLocatorMergeOptions());
        string assignedName = utility.SeparateAssignedName(
            "URN:Example:value%2f?+r?=q#f",
            new UrnParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(locator, Is.EqualTo(
                "https://weather.example/report?op=map#part"));
            Assert.That(assignedName, Is.EqualTo(
                "URN:Example:value%2f"));
        });
    }

    [Test]
    public void LocatorUtilityExposesCollisionPolicies()
    {
        IUrnLocatorUtility utility = new UrnLocatorUtility();
        UrnLocatorMergeOptions keep = new UrnLocatorMergeOptions();
        keep.QueryCollisionMode = UrnLocatorCollisionMode.KeepLocator;
        keep.FragmentCollisionMode = UrnLocatorCollisionMode.KeepLocator;
        UrnLocatorMergeOptions replace = new UrnLocatorMergeOptions();
        replace.QueryCollisionMode = UrnLocatorCollisionMode.ReplaceLocator;
        replace.FragmentCollisionMode =
            UrnLocatorCollisionMode.ReplaceLocator;

        string kept = utility.ApplyUrnComponentsToLocator(
            "urn:example:value?=urn-query#urn-fragment",
            "https://example.test/?locator-query#locator-fragment",
            keep);
        string replaced = utility.ApplyUrnComponentsToLocator(
            "urn:example:value?=urn-query#urn-fragment",
            "https://example.test/?locator-query#locator-fragment",
            replace);

        Assert.Multiple(() =>
        {
            Assert.That(kept, Is.EqualTo(
                "https://example.test/?locator-query#locator-fragment"));
            Assert.That(replaced, Is.EqualTo(
                "https://example.test/?urn-query#urn-fragment"));
        });
    }

    [Test]
    public void LocatorUtilityThrowsTypedResolutionAndSyntaxFailures()
    {
        IUrnLocatorUtility utility = new UrnLocatorUtility();

        UriResolutionException collision =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:example:value?=urn",
                    "https://example.test/?locator",
                    new UrnLocatorMergeOptions()))!;
        UriResolutionException invalidLocator =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:example:value",
                    "http://[invalid",
                    new UrnLocatorMergeOptions()))!;
        UrnSyntaxException invalidUrn =
            Assert.Throws<UrnSyntaxException>(() =>
                utility.SeparateAssignedName(
                    "not-a-urn",
                    new UrnParseOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(collision.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.UnresolvableReference));
            Assert.That(collision.Diagnostic.Component, Is.EqualTo(
                UriComponent.Query));
            Assert.That(invalidLocator.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidHost));
            Assert.That(invalidUrn.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidUrn));
        });
    }

    private static void AssertParseFailure(
        UrnParseResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Urn.OriginalText, Is.Empty);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        });
    }

    private static void AssertComparison(
        UrnEquivalenceResult result,
        bool equal,
        UrnEquivalenceLevel level,
        UriComponent component)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Equal, Is.EqualTo(equal));
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(component));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertComparisonFailure(
        UrnEquivalenceResult result,
        UrnEquivalenceLevel level,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(
                UriComponent.None));
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        });
    }

    private class RecordingNamespaceHandler : IUrnNamespaceHandler
    {
        public bool RejectValidation;
        public string? NormalizedValue;
        public bool ComparisonEqual;
        public int ValidationCount;
        public int NormalizationCount;
        public int ComparisonCount;
        public string NamespaceIdentifier;
        public string ValidatedValue;
        public string NormalizedInput;

        public RecordingNamespaceHandler()
        {
            RejectValidation = false;
            NormalizedValue = null;
            ComparisonEqual = false;
            ValidationCount = 0;
            NormalizationCount = 0;
            ComparisonCount = 0;
            NamespaceIdentifier = "";
            ValidatedValue = "";
            NormalizedInput = "";
        }

        public UriDiagnostic? ValidateNamespaceSpecificString(
            string namespaceIdentifier,
            string namespaceSpecificString)
        {
            ValidationCount = ValidationCount + 1;
            NamespaceIdentifier = namespaceIdentifier;
            ValidatedValue = namespaceSpecificString;
            if (RejectValidation)
            {
                return CreateNamespaceDiagnostic();
            }

            return null;
        }

        public UriTextResult NormalizeNamespaceSpecificString(
            string namespaceIdentifier,
            string namespaceSpecificString)
        {
            NormalizationCount = NormalizationCount + 1;
            NamespaceIdentifier = namespaceIdentifier;
            NormalizedInput = namespaceSpecificString;
            string value = namespaceSpecificString;
            if (NormalizedValue != null)
            {
                value = NormalizedValue;
            }

            return new UriTextResult(true, value, value.Length, null);
        }

        public UrnEquivalenceResult CompareNamespaceSpecificStrings(
            string namespaceIdentifier,
            string leftNamespaceSpecificString,
            string rightNamespaceSpecificString)
        {
            ComparisonCount = ComparisonCount + 1;
            NamespaceIdentifier = namespaceIdentifier;
            UriComponent component =
                UriComponent.UrnNamespaceSpecificString;
            if (ComparisonEqual)
            {
                component = UriComponent.None;
            }

            return new UrnEquivalenceResult(
                true,
                ComparisonEqual,
                UrnEquivalenceLevel.NamespaceSpecific,
                component,
                null);
        }

        private static UriDiagnostic CreateNamespaceDiagnostic()
        {
            return new UriDiagnostic(
                DiagnosticDomain.Urn,
                UriDiagnosticCode.UnsupportedNamespace,
                DiagnosticSeverity.Error,
                UriComponent.UrnNamespaceSpecificString,
                new TextRange(0, 0),
                "Namespace validation failed.");
        }
    }
}
