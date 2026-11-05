namespace uri.Tests;

public class UrnCompletePublicApiTests
{
    [Test]
    public void ParserRejectsInvalidPublicPreconditionsOnEveryEntryPoint()
    {
        IUrnParser parser = new UrnParser();
        UrnParseOptions missingLimits = new UrnParseOptions();
        missingLimits.Limits = null!;
        UrnParseOptions invalidMode = new UrnParseOptions();
        invalidMode.ValidationMode = (UrnValidationMode)99;

        UrnParseResult nullText = parser.TryParseUrn(
            null!,
            new UrnParseOptions());
        UrnParseResult nullOptions = parser.TryParseUrn(
            "urn:example:value",
            null!);
        UrnParseResult nullSpanOptions = parser.TryParseUrnSpan(
            "urn:example:value".AsSpan(),
            null!);
        UrnParseResult nullLimits = parser.TryParseUrn(
            "urn:example:value",
            missingLimits);
        UrnParseResult unsupportedMode = parser.TryParseUrn(
            "urn:example:value",
            invalidMode);
        UrnSyntaxException throwing = Assert.Throws<UrnSyntaxException>(() =>
            parser.ParseUrn(null!, new UrnParseOptions()))!;

        AssertInvalidInput(nullText.Diagnostic);
        AssertInvalidInput(nullOptions.Diagnostic);
        AssertInvalidInput(nullSpanOptions.Diagnostic);
        AssertInvalidInput(nullLimits.Diagnostic);
        AssertInvalidInput(unsupportedMode.Diagnostic);
        AssertInvalidInput(throwing.Diagnostic);
        Assert.That(throwing.Message,
            Is.EqualTo(throwing.Diagnostic.Message));
    }

    [TestCase("urn:abc:x", UriComponent.UrnNamespaceIdentifier)]
    [TestCase("urn:ab:xyz", UriComponent.UrnNamespaceSpecificString)]
    [TestCase("urn:ab:x?+rst", UriComponent.UrnRComponent)]
    [TestCase("urn:ab:x?=qry", UriComponent.UrnQComponent)]
    [TestCase("urn:ab:x#frg", UriComponent.UrnFComponent)]
    public void ParserAppliesTheLimitToEveryUrnComponent(
        string text,
        UriComponent component)
    {
        IUrnParser parser = new UrnParser();
        UrnParseOptions options = new UrnParseOptions();
        options.Limits.MaxComponentCharacters = 2;

        UrnParseResult result = parser.TryParseUrn(text, options);

        AssertFailureDiagnostic(
            result.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            component);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Urn.OriginalText, Is.Empty);
    }

    [Test]
    public void ParserAcceptsExactInputAndComponentLimits()
    {
        const string text = "urn:ab:xy?+rr?=qq#ff";
        IUrnParser parser = new UrnParser();
        UrnParseOptions options = new UrnParseOptions();
        options.Limits.MaxInputCharacters = text.Length;
        options.Limits.MaxComponentCharacters = 2;

        UrnParseResult result = parser.TryParseUrnSpan(
            text.AsSpan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Urn.OriginalText, Is.EqualTo(text));
            Assert.That(result.Urn.AssignedName, Is.EqualTo("urn:ab:xy"));
            Assert.That(result.Urn.HasRComponent, Is.True);
            Assert.That(result.Urn.HasQComponent, Is.True);
            Assert.That(result.Urn.HasFComponent, Is.True);
        });
    }

    [Test]
    public void NormalizerRejectsInvalidInputsOnBothOperations()
    {
        IUrnNormalizer normalizer = new UrnNormalizer();
        UrnParseOptions missingLimits = new UrnParseOptions();
        missingLimits.Limits = null!;
        UrnParseOptions invalidMode = new UrnParseOptions();
        invalidMode.ValidationMode = (UrnValidationMode)99;

        UrnEquivalenceException nullText =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeUrn(
                    null!,
                    new UrnParseOptions()))!;
        UrnEquivalenceException nullOptions =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeAssignedName(
                    "urn:example:value",
                    null!))!;
        UrnEquivalenceException nullLimits =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeUrn(
                    "urn:example:value",
                    missingLimits))!;
        UrnEquivalenceException unsupportedMode =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeAssignedName(
                    "urn:example:value",
                    invalidMode))!;

        AssertInvalidInput(nullText.Diagnostic);
        AssertInvalidInput(nullOptions.Diagnostic);
        AssertInvalidInput(nullLimits.Diagnostic);
        AssertInvalidInput(unsupportedMode.Diagnostic);
    }

    [Test]
    public void NormalizerPropagatesValidationAndNormalizationFailures()
    {
        UriDiagnostic validationDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.UnsupportedNamespace,
            UriComponent.UrnNamespaceSpecificString,
            "Validation rejected the NSS.");
        ConfigurableNamespaceHandler validationHandler =
            new ConfigurableNamespaceHandler();
        validationHandler.ValidationDiagnostic = validationDiagnostic;
        UrnParseOptions validationOptions = new UrnParseOptions();
        validationOptions.NamespaceHandler = validationHandler;

        UriDiagnostic normalizationDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.InvalidUrn,
            UriComponent.UrnNamespaceSpecificString,
            "Normalization failed.");
        ConfigurableNamespaceHandler normalizationHandler =
            new ConfigurableNamespaceHandler();
        normalizationHandler.NormalizationResult = new UriTextResult(
            false,
            "",
            0,
            normalizationDiagnostic);
        UrnParseOptions normalizationOptions = new UrnParseOptions();
        normalizationOptions.NamespaceHandler = normalizationHandler;
        IUrnNormalizer normalizer = new UrnNormalizer();

        UrnEquivalenceException validation =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeUrn(
                    "urn:example:value",
                    validationOptions))!;
        UrnEquivalenceException normalization =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeAssignedName(
                    "urn:example:value",
                    normalizationOptions))!;

        Assert.Multiple(() =>
        {
            Assert.That(validation.Diagnostic,
                Is.SameAs(validationDiagnostic));
            Assert.That(validationHandler.NormalizationCount, Is.Zero);
            Assert.That(normalization.Diagnostic,
                Is.SameAs(normalizationDiagnostic));
            Assert.That(normalizationHandler.NormalizationCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void NormalizerRejectsMalformedNamespaceHandlerResults()
    {
        IUrnNormalizer normalizer = new UrnNormalizer();
        ConfigurableNamespaceHandler nullHandler =
            new ConfigurableNamespaceHandler();
        nullHandler.ReturnNullNormalization = true;
        UrnParseOptions nullOptions = new UrnParseOptions();
        nullOptions.NamespaceHandler = nullHandler;
        ConfigurableNamespaceHandler wrongLengthHandler =
            new ConfigurableNamespaceHandler();
        wrongLengthHandler.NormalizationResult = new UriTextResult(
            true,
            "value",
            4,
            null);
        UrnParseOptions wrongLengthOptions = new UrnParseOptions();
        wrongLengthOptions.NamespaceHandler = wrongLengthHandler;
        ConfigurableNamespaceHandler diagnosticHandler =
            new ConfigurableNamespaceHandler();
        diagnosticHandler.NormalizationResult = new UriTextResult(
            true,
            "value",
            5,
            CreateDiagnostic(
                UriDiagnosticCode.InvalidUrn,
                UriComponent.UrnNamespaceSpecificString,
                "A successful result cannot contain a diagnostic."));
        UrnParseOptions diagnosticOptions = new UrnParseOptions();
        diagnosticOptions.NamespaceHandler = diagnosticHandler;

        UrnEquivalenceException nullResult =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeUrn(
                    "urn:example:value",
                    nullOptions))!;
        UrnEquivalenceException wrongLength =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeUrn(
                    "urn:example:value",
                    wrongLengthOptions))!;
        UrnEquivalenceException diagnostic =
            Assert.Throws<UrnEquivalenceException>(() =>
                normalizer.NormalizeUrn(
                    "urn:example:value",
                    diagnosticOptions))!;

        AssertInvalidInput(nullResult.Diagnostic);
        AssertInvalidInput(wrongLength.Diagnostic);
        AssertFailureDiagnostic(
            diagnostic.Diagnostic,
            UriDiagnosticCode.InvalidUrn,
            UriComponent.UrnNamespaceSpecificString);
    }

    [Test]
    public void ComparerReportsInvalidInputsAtEachEquivalenceLevel()
    {
        IUrnComparer comparer = new UrnComparer();
        UrnParseOptions missingLimits = new UrnParseOptions();
        missingLimits.Limits = null!;
        UrnParseOptions invalidMode = new UrnParseOptions();
        invalidMode.ValidationMode = (UrnValidationMode)99;

        UrnEquivalenceResult exact = comparer.CompareUrnExact(
            null!,
            "urn:example:value",
            new UrnParseOptions());
        UrnEquivalenceResult assigned = comparer.CompareUrnAssignedName(
            "urn:example:value",
            null!,
            missingLimits);
        UrnEquivalenceResult specific =
            comparer.CompareUrnNamespaceSpecific(
                "urn:example:value",
                "urn:example:value",
                null!);
        UrnEquivalenceResult invalid = comparer.CompareUrnExact(
            "urn:example:value",
            "urn:example:value",
            invalidMode);

        AssertComparisonFailure(exact, UrnEquivalenceLevel.Exact);
        AssertComparisonFailure(
            assigned,
            UrnEquivalenceLevel.AssignedName);
        AssertComparisonFailure(
            specific,
            UrnEquivalenceLevel.NamespaceSpecific);
        AssertComparisonFailure(invalid, UrnEquivalenceLevel.Exact);
    }

    [Test]
    public void ComparerPropagatesNamespaceHandlerFailures()
    {
        UriDiagnostic validationDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.UnsupportedNamespace,
            UriComponent.UrnNamespaceSpecificString,
            "Validation rejected the NSS.");
        ConfigurableNamespaceHandler validationHandler =
            new ConfigurableNamespaceHandler();
        validationHandler.ValidationDiagnostic = validationDiagnostic;
        UrnParseOptions validationOptions = new UrnParseOptions();
        validationOptions.NamespaceHandler = validationHandler;

        UriDiagnostic comparisonDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.UnsupportedNamespace,
            UriComponent.UrnNamespaceSpecificString,
            "Comparison failed.");
        ConfigurableNamespaceHandler comparisonHandler =
            new ConfigurableNamespaceHandler();
        comparisonHandler.ComparisonResult = new UrnEquivalenceResult(
            false,
            false,
            UrnEquivalenceLevel.NamespaceSpecific,
            UriComponent.None,
            comparisonDiagnostic);
        UrnParseOptions comparisonOptions = new UrnParseOptions();
        comparisonOptions.NamespaceHandler = comparisonHandler;
        ConfigurableNamespaceHandler nullHandler =
            new ConfigurableNamespaceHandler();
        nullHandler.ReturnNullComparison = true;
        UrnParseOptions nullOptions = new UrnParseOptions();
        nullOptions.NamespaceHandler = nullHandler;
        IUrnComparer comparer = new UrnComparer();

        UrnEquivalenceResult validation =
            comparer.CompareUrnNamespaceSpecific(
                "urn:example:left",
                "urn:example:right",
                validationOptions);
        UrnEquivalenceResult comparison =
            comparer.CompareUrnNamespaceSpecific(
                "urn:example:left",
                "urn:example:right",
                comparisonOptions);
        UrnEquivalenceResult nullResult =
            comparer.CompareUrnNamespaceSpecific(
                "urn:example:left",
                "urn:example:right",
                nullOptions);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Diagnostic,
                Is.SameAs(validationDiagnostic));
            Assert.That(validationHandler.ComparisonCount, Is.Zero);
            Assert.That(comparison.Diagnostic,
                Is.SameAs(comparisonDiagnostic));
            Assert.That(comparisonHandler.ComparisonCount, Is.EqualTo(1));
        });
        AssertComparisonFailure(
            nullResult,
            UrnEquivalenceLevel.NamespaceSpecific);
    }

    [Test]
    public void LocatorUtilityRejectsEveryInvalidPublicPrecondition()
    {
        IUrnLocatorUtility utility = new UrnLocatorUtility();
        UrnLocatorMergeOptions missingLimits =
            new UrnLocatorMergeOptions();
        missingLimits.Limits = null!;
        UrnLocatorMergeOptions invalidQuery =
            new UrnLocatorMergeOptions();
        invalidQuery.QueryCollisionMode = (UrnLocatorCollisionMode)99;
        UrnLocatorMergeOptions invalidFragment =
            new UrnLocatorMergeOptions();
        invalidFragment.FragmentCollisionMode =
            (UrnLocatorCollisionMode)99;

        UriResolutionException nullOptions =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:example:value",
                    "https://example.test/",
                    null!))!;
        UriResolutionException nullLimits =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:example:value",
                    "https://example.test/",
                    missingLimits))!;
        UriResolutionException queryMode =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:example:value",
                    "https://example.test/",
                    invalidQuery))!;
        UriResolutionException fragmentMode =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:example:value",
                    "https://example.test/",
                    invalidFragment))!;
        UriResolutionException nullUrn =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    null!,
                    "https://example.test/",
                    new UrnLocatorMergeOptions()))!;
        UriResolutionException nullLocator =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:example:value",
                    null!,
                    new UrnLocatorMergeOptions()))!;

        AssertInvalidInput(nullOptions.Diagnostic);
        AssertInvalidInput(nullLimits.Diagnostic);
        AssertInvalidInput(queryMode.Diagnostic);
        AssertInvalidInput(fragmentMode.Diagnostic);
        AssertInvalidInput(nullUrn.Diagnostic);
        AssertInvalidInput(nullLocator.Diagnostic);
    }

    [Test]
    public void LocatorUtilityReportsUrnLocatorComponentAndOutputLimits()
    {
        IUrnLocatorUtility utility = new UrnLocatorUtility();
        UrnLocatorMergeOptions urnInputOptions =
            new UrnLocatorMergeOptions();
        urnInputOptions.Limits.MaxInputCharacters = 7;
        UrnLocatorMergeOptions locatorInputOptions =
            new UrnLocatorMergeOptions();
        locatorInputOptions.Limits.MaxInputCharacters = 8;
        UrnLocatorMergeOptions componentOptions =
            new UrnLocatorMergeOptions();
        componentOptions.Limits.MaxComponentCharacters = 2;
        UrnLocatorMergeOptions outputOptions =
            new UrnLocatorMergeOptions();
        outputOptions.Limits.MaxOutputCharacters = 2;

        UriResolutionException urnInput =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:ab:x",
                    "x",
                    urnInputOptions))!;
        UriResolutionException locatorInput =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:ab:x",
                    "abcdefghi",
                    locatorInputOptions))!;
        UriResolutionException component =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:ab:xyz",
                    "x",
                    componentOptions))!;
        UriResolutionException output =
            Assert.Throws<UriResolutionException>(() =>
                utility.ApplyUrnComponentsToLocator(
                    "urn:ab:x?=q",
                    "x",
                    outputOptions))!;

        AssertFailureDiagnostic(
            urnInput.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input);
        AssertFailureDiagnostic(
            locatorInput.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input,
            DiagnosticDomain.Uri);
        AssertFailureDiagnostic(
            component.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.UrnNamespaceSpecificString);
        AssertFailureDiagnostic(
            output.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            DiagnosticDomain.Uri);
    }

    [Test]
    public void SeparateAssignedNameCoversOptionsValidationAndAllLimits()
    {
        IUrnLocatorUtility utility = new UrnLocatorUtility();
        UrnParseOptions missingLimits = new UrnParseOptions();
        missingLimits.Limits = null!;
        UrnParseOptions registered = new UrnParseOptions();
        registered.ValidationMode = UrnValidationMode.RegisteredNamespace;
        UrnParseOptions inputLimited = new UrnParseOptions();
        inputLimited.Limits.MaxInputCharacters = 7;
        UrnParseOptions componentLimited = new UrnParseOptions();
        componentLimited.Limits.MaxComponentCharacters = 2;
        UrnParseOptions outputLimited = new UrnParseOptions();
        outputLimited.Limits.MaxOutputCharacters = 7;

        UrnSyntaxException nullUrn = Assert.Throws<UrnSyntaxException>(() =>
            utility.SeparateAssignedName(
                null!,
                new UrnParseOptions()))!;
        UrnSyntaxException nullOptions =
            Assert.Throws<UrnSyntaxException>(() =>
                utility.SeparateAssignedName(
                    "urn:example:value",
                    null!))!;
        UrnSyntaxException nullLimits =
            Assert.Throws<UrnSyntaxException>(() =>
                utility.SeparateAssignedName(
                    "urn:example:value",
                    missingLimits))!;
        UrnSyntaxException registration =
            Assert.Throws<UrnSyntaxException>(() =>
                utility.SeparateAssignedName(
                    "urn:x-test:value",
                    registered))!;
        UrnSyntaxException input = Assert.Throws<UrnSyntaxException>(() =>
            utility.SeparateAssignedName(
                "urn:ab:x",
                inputLimited))!;
        UrnSyntaxException component =
            Assert.Throws<UrnSyntaxException>(() =>
                utility.SeparateAssignedName(
                    "urn:ab:xyz",
                    componentLimited))!;
        UrnSyntaxException output = Assert.Throws<UrnSyntaxException>(() =>
            utility.SeparateAssignedName(
                "urn:ab:x?+r?=q#f",
                outputLimited))!;

        AssertInvalidInput(nullUrn.Diagnostic);
        AssertInvalidInput(nullOptions.Diagnostic);
        AssertInvalidInput(nullLimits.Diagnostic);
        AssertFailureDiagnostic(
            registration.Diagnostic,
            UriDiagnosticCode.InvalidNamespaceIdentifier,
            UriComponent.UrnNamespaceIdentifier);
        AssertFailureDiagnostic(
            input.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input);
        AssertFailureDiagnostic(
            component.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.UrnNamespaceSpecificString);
        AssertFailureDiagnostic(
            output.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output);
    }

    [Test]
    public void SeparateAssignedNamePropagatesNamespaceHandlerFailure()
    {
        UriDiagnostic handlerDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.UnsupportedNamespace,
            UriComponent.UrnNamespaceSpecificString,
            "Namespace validation failed.");
        ConfigurableNamespaceHandler handler =
            new ConfigurableNamespaceHandler();
        handler.ValidationDiagnostic = handlerDiagnostic;
        UrnParseOptions options = new UrnParseOptions();
        options.NamespaceHandler = handler;
        IUrnLocatorUtility utility = new UrnLocatorUtility();

        UrnSyntaxException exception = Assert.Throws<UrnSyntaxException>(() =>
            utility.SeparateAssignedName(
                "urn:example:value",
                options))!;

        Assert.That(exception.Diagnostic, Is.SameAs(handlerDiagnostic));
    }

    private static void AssertComparisonFailure(
        UrnEquivalenceResult result,
        UrnEquivalenceLevel level)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent,
                Is.EqualTo(UriComponent.None));
        });
        AssertInvalidInput(result.Diagnostic);
    }

    private static void AssertInvalidInput(UriDiagnostic? diagnostic)
    {
        AssertFailureDiagnostic(
            diagnostic,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
    }

    private static void AssertFailureDiagnostic(
        UriDiagnostic? diagnostic,
        UriDiagnosticCode code,
        UriComponent component)
    {
        AssertFailureDiagnostic(
            diagnostic,
            code,
            component,
            DiagnosticDomain.Urn);
    }

    private static void AssertFailureDiagnostic(
        UriDiagnostic? diagnostic,
        UriDiagnosticCode code,
        UriComponent component,
        DiagnosticDomain domain)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic!.Domain, Is.EqualTo(domain));
            Assert.That(diagnostic.Code, Is.EqualTo(code));
            Assert.That(diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.Component, Is.EqualTo(component));
            Assert.That(diagnostic.Range.Offset, Is.GreaterThanOrEqualTo(0));
            Assert.That(diagnostic.Range.Length, Is.GreaterThanOrEqualTo(0));
            Assert.That(diagnostic.Message, Is.Not.Empty);
        });
    }

    private static UriDiagnostic CreateDiagnostic(
        UriDiagnosticCode code,
        UriComponent component,
        string message)
    {
        return new UriDiagnostic(
            DiagnosticDomain.Urn,
            code,
            DiagnosticSeverity.Error,
            component,
            new TextRange(0, 1),
            message);
    }

    private class ConfigurableNamespaceHandler : IUrnNamespaceHandler
    {
        public UriDiagnostic? ValidationDiagnostic;
        public UriTextResult? NormalizationResult;
        public UrnEquivalenceResult? ComparisonResult;
        public bool ReturnNullNormalization;
        public bool ReturnNullComparison;
        public int NormalizationCount;
        public int ComparisonCount;

        public ConfigurableNamespaceHandler()
        {
            ValidationDiagnostic = null;
            NormalizationResult = null;
            ComparisonResult = null;
            ReturnNullNormalization = false;
            ReturnNullComparison = false;
            NormalizationCount = 0;
            ComparisonCount = 0;
        }

        public UriDiagnostic? ValidateNamespaceSpecificString(
            string namespaceIdentifier,
            string namespaceSpecificString)
        {
            return ValidationDiagnostic;
        }

        public UriTextResult NormalizeNamespaceSpecificString(
            string namespaceIdentifier,
            string namespaceSpecificString)
        {
            NormalizationCount = NormalizationCount + 1;
            if (ReturnNullNormalization)
            {
                return null!;
            }

            if (NormalizationResult != null)
            {
                return NormalizationResult;
            }

            return new UriTextResult(
                true,
                namespaceSpecificString,
                namespaceSpecificString.Length,
                null);
        }

        public UrnEquivalenceResult CompareNamespaceSpecificStrings(
            string namespaceIdentifier,
            string leftNamespaceSpecificString,
            string rightNamespaceSpecificString)
        {
            ComparisonCount = ComparisonCount + 1;
            if (ReturnNullComparison)
            {
                return null!;
            }

            if (ComparisonResult != null)
            {
                return ComparisonResult;
            }

            return new UrnEquivalenceResult(
                true,
                false,
                UrnEquivalenceLevel.NamespaceSpecific,
                UriComponent.UrnNamespaceSpecificString,
                null);
        }
    }
}
