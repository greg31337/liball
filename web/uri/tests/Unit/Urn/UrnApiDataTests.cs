namespace uri.Tests;

public class UrnApiDataTests
{
    [Test]
    public void UrnConstructorProvidesEmptyOptionalComponentSafeDefaults()
    {
        Urn urn = new Urn();

        Assert.Multiple(() =>
        {
            Assert.That(urn.Scheme, Is.Empty);
            Assert.That(urn.NamespaceIdentifier, Is.Empty);
            Assert.That(urn.NamespaceSpecificString, Is.Empty);
            Assert.That(urn.AssignedName, Is.Empty);
            Assert.That(urn.HasRComponent, Is.False);
            Assert.That(urn.RComponent, Is.Empty);
            Assert.That(urn.HasQComponent, Is.False);
            Assert.That(urn.QComponent, Is.Empty);
            Assert.That(urn.HasFComponent, Is.False);
            Assert.That(urn.FComponent, Is.Empty);
            Assert.That(urn.OriginalText, Is.Empty);
        });
    }

    [Test]
    public void UrnModelPreservesAssignedNameAndOptionalComponentPresence()
    {
        Urn urn = new Urn();
        urn.Scheme = "URN";
        urn.NamespaceIdentifier = "example";
        urn.NamespaceSpecificString = "animal:ferret:nose";
        urn.AssignedName = "URN:example:animal:ferret:nose";
        urn.HasRComponent = true;
        urn.RComponent = "resolver";
        urn.HasQComponent = true;
        urn.QComponent = "query=value";
        urn.HasFComponent = true;
        urn.FComponent = "fragment";
        urn.OriginalText =
            "URN:example:animal:ferret:nose?+resolver?=query=value#fragment";

        Assert.Multiple(() =>
        {
            Assert.That(urn.Scheme, Is.EqualTo("URN"));
            Assert.That(urn.NamespaceIdentifier, Is.EqualTo("example"));
            Assert.That(urn.NamespaceSpecificString, Is.EqualTo(
                "animal:ferret:nose"));
            Assert.That(urn.AssignedName, Is.EqualTo(
                "URN:example:animal:ferret:nose"));
            Assert.That(urn.HasRComponent, Is.True);
            Assert.That(urn.RComponent, Is.EqualTo("resolver"));
            Assert.That(urn.HasQComponent, Is.True);
            Assert.That(urn.QComponent, Is.EqualTo("query=value"));
            Assert.That(urn.HasFComponent, Is.True);
            Assert.That(urn.FComponent, Is.EqualTo("fragment"));
            Assert.That(urn.OriginalText, Is.EqualTo(
                "URN:example:animal:ferret:nose?+resolver?=query=value#fragment"));
        });
    }

    [Test]
    public void ParseResultsRetainSuccessAndFailureState()
    {
        Urn parsedUrn = new Urn();
        parsedUrn.OriginalText = "urn:example:test";
        UriDiagnostic diagnostic = CreateDiagnostic(
            UriDiagnosticCode.InvalidUrn,
            UriComponent.Urn);
        UrnParseResult success = new UrnParseResult(true, parsedUrn, null);
        Urn emptyUrn = new Urn();
        UrnParseResult failure = new UrnParseResult(
            false,
            emptyUrn,
            diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.Urn, Is.SameAs(parsedUrn));
            Assert.That(success.Diagnostic, Is.Null);
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Urn, Is.SameAs(emptyUrn));
            Assert.That(failure.Urn.OriginalText, Is.Empty);
            Assert.That(failure.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    [Test]
    public void EquivalenceResultsRetainEqualDifferentAndFailureState()
    {
        UriDiagnostic diagnostic = CreateDiagnostic(
            UriDiagnosticCode.UnsupportedNamespace,
            UriComponent.UrnNamespaceSpecificString);
        UrnEquivalenceResult equal = new UrnEquivalenceResult(
            true,
            true,
            UrnEquivalenceLevel.AssignedName,
            UriComponent.None,
            null);
        UrnEquivalenceResult different = new UrnEquivalenceResult(
            true,
            false,
            UrnEquivalenceLevel.Exact,
            UriComponent.UrnQComponent,
            null);
        UrnEquivalenceResult failure = new UrnEquivalenceResult(
            false,
            false,
            UrnEquivalenceLevel.NamespaceSpecific,
            UriComponent.None,
            diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(equal.Success, Is.True);
            Assert.That(equal.Equal, Is.True);
            Assert.That(equal.Level, Is.EqualTo(
                UrnEquivalenceLevel.AssignedName));
            Assert.That(equal.DifferentComponent, Is.EqualTo(
                UriComponent.None));
            Assert.That(equal.Diagnostic, Is.Null);
            Assert.That(different.Success, Is.True);
            Assert.That(different.Equal, Is.False);
            Assert.That(different.Level, Is.EqualTo(
                UrnEquivalenceLevel.Exact));
            Assert.That(different.DifferentComponent, Is.EqualTo(
                UriComponent.UrnQComponent));
            Assert.That(different.Diagnostic, Is.Null);
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Equal, Is.False);
            Assert.That(failure.Level, Is.EqualTo(
                UrnEquivalenceLevel.NamespaceSpecific));
            Assert.That(failure.DifferentComponent, Is.EqualTo(
                UriComponent.None));
            Assert.That(failure.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    [Test]
    public void UrnEnumsExposeEveryPolicyChoiceAndStableValue()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetValues<UrnValidationMode>(), Is.EqualTo(
                new UrnValidationMode[]
                {
                    UrnValidationMode.GenericSyntax,
                    UrnValidationMode.RegisteredNamespace
                }));
            Assert.That((int)UrnValidationMode.GenericSyntax, Is.EqualTo(0));
            Assert.That((int)UrnValidationMode.RegisteredNamespace,
                Is.EqualTo(1));
            Assert.That(Enum.GetValues<UrnEquivalenceLevel>(), Is.EqualTo(
                new UrnEquivalenceLevel[]
                {
                    UrnEquivalenceLevel.Exact,
                    UrnEquivalenceLevel.AssignedName,
                    UrnEquivalenceLevel.NamespaceSpecific
                }));
            Assert.That((int)UrnEquivalenceLevel.Exact, Is.EqualTo(0));
            Assert.That((int)UrnEquivalenceLevel.AssignedName, Is.EqualTo(1));
            Assert.That((int)UrnEquivalenceLevel.NamespaceSpecific,
                Is.EqualTo(2));
            Assert.That(Enum.GetValues<UrnLocatorCollisionMode>(), Is.EqualTo(
                new UrnLocatorCollisionMode[]
                {
                    UrnLocatorCollisionMode.Reject,
                    UrnLocatorCollisionMode.KeepLocator,
                    UrnLocatorCollisionMode.ReplaceLocator
                }));
            Assert.That((int)UrnLocatorCollisionMode.Reject, Is.EqualTo(0));
            Assert.That((int)UrnLocatorCollisionMode.KeepLocator,
                Is.EqualTo(1));
            Assert.That((int)UrnLocatorCollisionMode.ReplaceLocator,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void UrnOptionsProvideIndependentSafeDefaultsAndMutablePolicies()
    {
        UrnParseOptions firstParse = new UrnParseOptions();
        UrnParseOptions secondParse = new UrnParseOptions();
        UrnLocatorMergeOptions firstMerge = new UrnLocatorMergeOptions();
        UrnLocatorMergeOptions secondMerge = new UrnLocatorMergeOptions();
        ExampleNamespaceHandler handler = new ExampleNamespaceHandler();

        firstParse.Limits.MaxInputCharacters = 12;
        firstParse.ValidationMode = UrnValidationMode.RegisteredNamespace;
        firstParse.NamespaceHandler = handler;
        firstMerge.Limits.MaxOutputCharacters = 24;
        firstMerge.QueryCollisionMode = UrnLocatorCollisionMode.KeepLocator;
        firstMerge.FragmentCollisionMode =
            UrnLocatorCollisionMode.ReplaceLocator;

        Assert.Multiple(() =>
        {
            Assert.That(firstParse.Limits, Is.Not.SameAs(secondParse.Limits));
            Assert.That(firstParse.Limits.MaxInputCharacters, Is.EqualTo(12));
            Assert.That(firstParse.ValidationMode, Is.EqualTo(
                UrnValidationMode.RegisteredNamespace));
            Assert.That(firstParse.NamespaceHandler, Is.SameAs(handler));
            Assert.That(secondParse.Limits.MaxInputCharacters,
                Is.EqualTo(1048576));
            Assert.That(secondParse.ValidationMode, Is.EqualTo(
                UrnValidationMode.GenericSyntax));
            Assert.That(secondParse.NamespaceHandler, Is.Null);
            Assert.That(firstMerge.Limits, Is.Not.SameAs(secondMerge.Limits));
            Assert.That(firstMerge.Limits.MaxOutputCharacters, Is.EqualTo(24));
            Assert.That(firstMerge.QueryCollisionMode, Is.EqualTo(
                UrnLocatorCollisionMode.KeepLocator));
            Assert.That(firstMerge.FragmentCollisionMode, Is.EqualTo(
                UrnLocatorCollisionMode.ReplaceLocator));
            Assert.That(secondMerge.QueryCollisionMode, Is.EqualTo(
                UrnLocatorCollisionMode.Reject));
            Assert.That(secondMerge.FragmentCollisionMode, Is.EqualTo(
                UrnLocatorCollisionMode.Reject));
        });
    }

    [Test]
    public void NamespaceHandlerContractCarriesValidationNormalizationAndComparison()
    {
        IUrnNamespaceHandler handler = new ExampleNamespaceHandler();

        UriDiagnostic? valid = handler.ValidateNamespaceSpecificString(
            "example",
            "abc");
        UriDiagnostic? invalid = handler.ValidateNamespaceSpecificString(
            "other",
            "abc");
        UriTextResult normalized = handler.NormalizeNamespaceSpecificString(
            "example",
            "AbC");
        UrnEquivalenceResult equal =
            handler.CompareNamespaceSpecificStrings(
                "example",
                "AbC",
                "abc");
        UrnEquivalenceResult different =
            handler.CompareNamespaceSpecificStrings(
                "example",
                "abc",
                "xyz");

        Assert.Multiple(() =>
        {
            Assert.That(valid, Is.Null);
            Assert.That(invalid, Is.Not.Null);
            Assert.That(invalid!.Domain, Is.EqualTo(DiagnosticDomain.Urn));
            Assert.That(invalid.Code, Is.EqualTo(
                UriDiagnosticCode.UnsupportedNamespace));
            Assert.That(normalized.Success, Is.True);
            Assert.That(normalized.Text, Is.EqualTo("abc"));
            Assert.That(normalized.Length, Is.EqualTo(3));
            Assert.That(equal.Success, Is.True);
            Assert.That(equal.Equal, Is.True);
            Assert.That(equal.Level, Is.EqualTo(
                UrnEquivalenceLevel.NamespaceSpecific));
            Assert.That(equal.DifferentComponent, Is.EqualTo(
                UriComponent.None));
            Assert.That(different.Success, Is.True);
            Assert.That(different.Equal, Is.False);
            Assert.That(different.DifferentComponent, Is.EqualTo(
                UriComponent.UrnNamespaceSpecificString));
        });
    }

    [Test]
    public void UrnExceptionsRetainTheirDiagnosticAndMessage()
    {
        UriDiagnostic syntaxDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.InvalidUrn,
            UriComponent.Urn);
        UriDiagnostic equivalenceDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.UnsupportedNamespace,
            UriComponent.UrnNamespaceSpecificString);

        UrnSyntaxException syntax = new UrnSyntaxException(syntaxDiagnostic);
        UrnEquivalenceException equivalence =
            new UrnEquivalenceException(equivalenceDiagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(syntax.Diagnostic, Is.SameAs(syntaxDiagnostic));
            Assert.That(syntax.Message, Is.EqualTo(syntaxDiagnostic.Message));
            Assert.That(equivalence.Diagnostic,
                Is.SameAs(equivalenceDiagnostic));
            Assert.That(equivalence.Message, Is.EqualTo(
                equivalenceDiagnostic.Message));
        });
    }

    private static UriDiagnostic CreateDiagnostic(
        UriDiagnosticCode code,
        UriComponent component)
    {
        return new UriDiagnostic(
            DiagnosticDomain.Urn,
            code,
            DiagnosticSeverity.Error,
            component,
            new TextRange(0, 1),
            "URN operation failed.");
    }

    private class ExampleNamespaceHandler : IUrnNamespaceHandler
    {
        public UriDiagnostic? ValidateNamespaceSpecificString(
            string namespaceIdentifier,
            string namespaceSpecificString)
        {
            if (namespaceIdentifier == "example" &&
                namespaceSpecificString.Length > 0)
            {
                return null;
            }

            return CreateDiagnostic(
                UriDiagnosticCode.UnsupportedNamespace,
                UriComponent.UrnNamespaceSpecificString);
        }

        public UriTextResult NormalizeNamespaceSpecificString(
            string namespaceIdentifier,
            string namespaceSpecificString)
        {
            UriDiagnostic? diagnostic = ValidateNamespaceSpecificString(
                namespaceIdentifier,
                namespaceSpecificString);
            if (diagnostic != null)
            {
                return new UriTextResult(false, "", 0, diagnostic);
            }

            string text = namespaceSpecificString.ToLowerInvariant();
            return new UriTextResult(true, text, text.Length, null);
        }

        public UrnEquivalenceResult CompareNamespaceSpecificStrings(
            string namespaceIdentifier,
            string leftNamespaceSpecificString,
            string rightNamespaceSpecificString)
        {
            UriDiagnostic? diagnostic = ValidateNamespaceSpecificString(
                namespaceIdentifier,
                leftNamespaceSpecificString);
            if (diagnostic != null)
            {
                return new UrnEquivalenceResult(
                    false,
                    false,
                    UrnEquivalenceLevel.NamespaceSpecific,
                    UriComponent.None,
                    diagnostic);
            }

            bool equal = string.Equals(
                leftNamespaceSpecificString,
                rightNamespaceSpecificString,
                StringComparison.OrdinalIgnoreCase);
            UriComponent differentComponent = UriComponent.None;
            if (!equal)
            {
                differentComponent =
                    UriComponent.UrnNamespaceSpecificString;
            }

            return new UrnEquivalenceResult(
                true,
                equal,
                UrnEquivalenceLevel.NamespaceSpecific,
                differentComponent,
                null);
        }
    }
}
