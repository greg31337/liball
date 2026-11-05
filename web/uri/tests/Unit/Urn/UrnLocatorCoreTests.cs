namespace uri.Tests;

public class UrnLocatorCoreTests
{
    [Test]
    public void AssignedNameSeparationPreservesLexicalSpelling()
    {
        Urn urn = ParseUrn("URN:Example:a%2f/Path?+resolver?=query#fragment");

        UriTextResult result = UrnLocatorCore.SeparateAssignedName(
            urn,
            new OperationLimits());

        AssertTextSuccess(result, "URN:Example:a%2f/Path");
    }

    [Test]
    public void AssignedNameSeparationUsesActiveFields()
    {
        Urn urn = ParseUrn("urn:example:old?=query");
        urn.Scheme = "URN";
        urn.NamespaceIdentifier = "Changed";
        urn.NamespaceSpecificString = "new%af";
        urn.AssignedName = "stale-assigned-name";
        urn.OriginalText = "stale-original-text";

        UriTextResult result = UrnLocatorCore.SeparateAssignedName(
            urn,
            new OperationLimits());

        AssertTextSuccess(result, "URN:Changed:new%af");
    }

    [Test]
    public void AssignedNameSeparationHonorsExactOutputBoundary()
    {
        Urn urn = ParseUrn("urn:example:value?+r?=q#f");
        const string assignedName = "urn:example:value";
        OperationLimits exact = CreateLimits();
        exact.MaxOutputCharacters = assignedName.Length;
        OperationLimits shortLimit = CreateLimits();
        shortLimit.MaxOutputCharacters = assignedName.Length - 1;

        UriTextResult success = UrnLocatorCore.SeparateAssignedName(
            urn,
            exact);
        UriTextResult failure = UrnLocatorCore.SeparateAssignedName(
            urn,
            shortLimit);

        Assert.Multiple(() =>
        {
            AssertTextSuccess(success, assignedName);
            AssertTextFailure(
                failure,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output);
        });
    }

    [Test]
    public void QAndFComponentsTransferToEmptyLocatorSlots()
    {
        Urn urn = ParseUrn("urn:example:weather?=op=map&lat=39.56#part");
        UriReference locator = ParseUri("https://weather.example/report");

        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                urn,
                locator,
                new UrnLocatorMergeOptions());

        AssertEditSuccess(
            result,
            "https://weather.example/report?op=map&lat=39.56#part");
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference.HasQuery, Is.True);
            Assert.That(result.Reference.Query, Is.EqualTo(
                "op=map&lat=39.56"));
            Assert.That(result.Reference.HasFragment, Is.True);
            Assert.That(result.Reference.Fragment, Is.EqualTo("part"));
            Assert.That(result.Reference, Is.Not.SameAs(locator));
        });
    }

    [Test]
    public void EmptyUrnFragmentPresenceIsTransferred()
    {
        Urn urn = ParseUrn("urn:example:value#");

        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                urn,
                ParseUri("https://example.test/resource"),
                new UrnLocatorMergeOptions());

        AssertEditSuccess(result, "https://example.test/resource#");
        Assert.That(result.Reference.HasFragment, Is.True);
        Assert.That(result.Reference.Fragment, Is.Empty);
    }

    [Test]
    public void RComponentIsNeverAppliedToTheLocator()
    {
        Urn urn = ParseUrn(
            "urn:example:value?+CCResolve:cc=uk?x/y");
        UriReference locator = ParseUri(
            "https://example.test/resource?existing#fragment");
        UrnLocatorMergeOptions options = new UrnLocatorMergeOptions();
        options.QueryCollisionMode = UrnLocatorCollisionMode.KeepLocator;
        options.FragmentCollisionMode = UrnLocatorCollisionMode.KeepLocator;

        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                urn,
                locator,
                options);

        AssertEditSuccess(
            result,
            "https://example.test/resource?existing#fragment");
    }

    [TestCase(
        UrnLocatorCollisionMode.KeepLocator,
        "https://example.test/resource?locator")]
    [TestCase(
        UrnLocatorCollisionMode.ReplaceLocator,
        "https://example.test/resource?urn")]
    public void QueryCollisionPoliciesSelectTheDeclaredValue(
        UrnLocatorCollisionMode mode,
        string expected)
    {
        UrnLocatorMergeOptions options = new UrnLocatorMergeOptions();
        options.QueryCollisionMode = mode;

        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                ParseUrn("urn:example:value?=urn"),
                ParseUri("https://example.test/resource?locator"),
                options);

        AssertEditSuccess(result, expected);
    }

    [TestCase(
        UrnLocatorCollisionMode.KeepLocator,
        "https://example.test/resource#locator")]
    [TestCase(
        UrnLocatorCollisionMode.ReplaceLocator,
        "https://example.test/resource#urn")]
    public void FragmentCollisionPoliciesSelectTheDeclaredValue(
        UrnLocatorCollisionMode mode,
        string expected)
    {
        UrnLocatorMergeOptions options = new UrnLocatorMergeOptions();
        options.FragmentCollisionMode = mode;

        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                ParseUrn("urn:example:value#urn"),
                ParseUri("https://example.test/resource#locator"),
                options);

        AssertEditSuccess(result, expected);
    }

    [Test]
    public void IndependentPoliciesCanKeepQueryAndReplaceFragment()
    {
        UrnLocatorMergeOptions options = new UrnLocatorMergeOptions();
        options.QueryCollisionMode = UrnLocatorCollisionMode.KeepLocator;
        options.FragmentCollisionMode =
            UrnLocatorCollisionMode.ReplaceLocator;

        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                ParseUrn("urn:example:value?=urn-query#urn-fragment"),
                ParseUri(
                    "https://example.test/resource?locator-query#locator-fragment"),
                options);

        AssertEditSuccess(
            result,
            "https://example.test/resource?locator-query#urn-fragment");
    }

    [Test]
    public void RejectPolicyReportsQueryCollisionFirstAndIsAtomic()
    {
        Urn urn = ParseUrn("urn:example:value?=urn-query#urn-fragment");
        UriReference locator = ParseUri(
            "https://example.test/resource?locator-query#locator-fragment");
        string original = Compose(locator);

        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                urn,
                locator,
                new UrnLocatorMergeOptions());

        Assert.Multiple(() =>
        {
            AssertEditFailure(
                result,
                UriDiagnosticCode.UnresolvableReference,
                UriComponent.Query);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(Compose(locator), Is.EqualTo(original));
        });
    }

    [Test]
    public void RejectPolicyReportsFragmentCollision()
    {
        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                ParseUrn("urn:example:value#urn-fragment"),
                ParseUri(
                    "https://example.test/resource#locator-fragment"),
                new UrnLocatorMergeOptions());

        AssertEditFailure(
            result,
            UriDiagnosticCode.UnresolvableReference,
            UriComponent.Fragment);
    }

    [Test]
    public void AbsentUrnComponentsNeverCollideWithLocatorComponents()
    {
        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                ParseUrn("urn:example:value"),
                ParseUri(
                    "https://example.test/resource?locator-query#locator-fragment"),
                new UrnLocatorMergeOptions());

        AssertEditSuccess(
            result,
            "https://example.test/resource?locator-query#locator-fragment");
    }

    [Test]
    public void ExistingEmptyDelimiterCountsAsACollision()
    {
        UrnLocatorMergeOptions replace = new UrnLocatorMergeOptions();
        replace.QueryCollisionMode = UrnLocatorCollisionMode.ReplaceLocator;
        UriEditCoreResult rejected =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                ParseUrn("urn:example:value?=urn"),
                ParseUri("https://example.test/resource?"),
                new UrnLocatorMergeOptions());
        UriEditCoreResult replaced =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                ParseUrn("urn:example:value?=urn"),
                ParseUri("https://example.test/resource?"),
                replace);

        Assert.Multiple(() =>
        {
            AssertEditFailure(
                rejected,
                UriDiagnosticCode.UnresolvableReference,
                UriComponent.Query);
            AssertEditSuccess(
                replaced,
                "https://example.test/resource?urn");
        });
    }

    [Test]
    public void LocatorMergeDeepCopiesAuthorityStorage()
    {
        UriReference locator = ParseUri(
            "https://user@[2001:db8::1]:443/resource");

        UriEditCoreResult result =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                ParseUrn("urn:example:value?=query"),
                locator,
                new UrnLocatorMergeOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Reference.Authority,
            Is.Not.SameAs(locator.Authority));
        Assert.That(result.Reference.Authority.AddressBytes,
            Is.Not.SameAs(locator.Authority.AddressBytes));
        result.Reference.Authority.AddressBytes[0] = 255;
        Assert.That(locator.Authority.AddressBytes[0], Is.EqualTo(32));
    }

    [Test]
    public void LocatorMergeHonorsOutputAndComponentLimits()
    {
        Urn urn = ParseUrn("urn:ex:value?=long-query");
        UriReference locator = ParseUri("x:a");
        UrnLocatorMergeOptions outputOptions = new UrnLocatorMergeOptions();
        outputOptions.Limits.MaxOutputCharacters = 5;
        UrnLocatorMergeOptions componentOptions =
            new UrnLocatorMergeOptions();
        componentOptions.Limits.MaxComponentCharacters = 4;

        UriEditCoreResult output =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                urn,
                locator,
                outputOptions);
        UriEditCoreResult component =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                urn,
                locator,
                componentOptions);

        Assert.Multiple(() =>
        {
            AssertEditFailure(
                output,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output);
            AssertEditFailure(
                component,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.UrnNamespaceSpecificString);
        });
    }

    [Test]
    public void InvalidInputsModelsAndPoliciesFailCleanly()
    {
        Urn urn = ParseUrn("urn:example:value?=query");
        UriReference locator = ParseUri("https://example.test/resource");
        Urn invalidUrn = ParseUrn("urn:example:value");
        invalidUrn.NamespaceSpecificString = "%GG";
        UriReference invalidLocator = ParseUri(
            "https://example.test/resource");
        invalidLocator.Path = "/%GG";
        UrnLocatorMergeOptions invalidOptions =
            new UrnLocatorMergeOptions();
        invalidOptions.QueryCollisionMode = (UrnLocatorCollisionMode)99;

        UriEditCoreResult missingUrn =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                null,
                locator,
                new UrnLocatorMergeOptions());
        UriEditCoreResult missingLocator =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                urn,
                null,
                new UrnLocatorMergeOptions());
        UriEditCoreResult missingOptions =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                urn,
                locator,
                null);
        UriEditCoreResult policy =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                urn,
                locator,
                invalidOptions);
        UriEditCoreResult urnModel =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                invalidUrn,
                locator,
                new UrnLocatorMergeOptions());
        UriEditCoreResult locatorModel =
            UrnLocatorCore.ApplyUrnComponentsToLocator(
                urn,
                invalidLocator,
                new UrnLocatorMergeOptions());

        Assert.Multiple(() =>
        {
            AssertEditFailure(
                missingUrn,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
            AssertEditFailure(
                missingLocator,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
            AssertEditFailure(
                missingOptions,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
            AssertEditFailure(
                policy,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
            AssertEditFailure(
                urnModel,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.UrnNamespaceSpecificString);
            AssertEditFailure(
                locatorModel,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.Path);
        });
    }

    private static Urn ParseUrn(string text)
    {
        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);
        Assert.That(result.Success, Is.True, text);
        return result.Urn;
    }

    private static UriReference ParseUri(string text)
    {
        UriParseResult result = UriParserCore.ParseReference(text.AsSpan());
        Assert.That(result.Success, Is.True, text);
        return result.Reference;
    }

    private static string Compose(UriReference reference)
    {
        UriCompositionResult result =
            UriComposerCore.RecomposeParsedReference(
                reference,
                new OperationLimits());
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        return result.Text;
    }

    private static OperationLimits CreateLimits()
    {
        return new OperationLimits();
    }

    private static void AssertTextSuccess(
        UriTextResult result,
        string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Length, Is.EqualTo(expected.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertTextFailure(
        UriTextResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        });
    }

    private static void AssertEditSuccess(
        UriEditCoreResult result,
        string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Length, Is.EqualTo(expected.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertEditFailure(
        UriEditCoreResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reference.Kind, Is.EqualTo(
                UriReferenceKind.EmptyReference));
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        });
    }
}
