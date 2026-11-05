namespace uri.Tests;

public class UrnModelConverterTests
{
    [TestCase("urn:example:a", false, "", false, "")]
    [TestCase("urn:example:a?+r", true, "+r", false, "")]
    [TestCase("urn:example:a?=q", true, "=q", false, "")]
    [TestCase("urn:example:a?+r?=q", true, "+r?=q", false, "")]
    [TestCase("urn:example:a#", false, "", true, "")]
    [TestCase("urn:example:a?+r#f", true, "+r", true, "f")]
    [TestCase("urn:example:a?=q#f", true, "=q", true, "f")]
    [TestCase("urn:example:a?+r?=q#f", true, "+r?=q", true, "f")]
    public void UrnToUriMappingPreservesEveryRqfBoundary(
        string text,
        bool hasQuery,
        string query,
        bool hasFragment,
        string fragment)
    {
        Urn urn = ParseUrn(text);

        UriParseResult result =
            UrnModelConverterCore.ConvertUrnToUriReference(
                urn,
                new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Reference.HasScheme, Is.True);
            Assert.That(result.Reference.Scheme, Is.EqualTo("urn"));
            Assert.That(result.Reference.HasAuthority, Is.False);
            Assert.That(result.Reference.Path, Is.EqualTo("example:a"));
            Assert.That(result.Reference.HasQuery, Is.EqualTo(hasQuery));
            Assert.That(result.Reference.Query, Is.EqualTo(query));
            Assert.That(result.Reference.HasFragment, Is.EqualTo(
                hasFragment));
            Assert.That(result.Reference.Fragment, Is.EqualTo(fragment));
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text));
        });
    }

    [TestCase("urn:example:a", false, "", false, "", false, "")]
    [TestCase("urn:example:a?+r", true, "r", false, "", false, "")]
    [TestCase("urn:example:a?=q", false, "", true, "q", false, "")]
    [TestCase("urn:example:a?+r?=q", true, "r", true, "q", false, "")]
    [TestCase("urn:example:a#", false, "", false, "", true, "")]
    [TestCase("urn:example:a?+r#f", true, "r", false, "", true, "f")]
    [TestCase("urn:example:a?=q#f", false, "", true, "q", true, "f")]
    [TestCase("urn:example:a?+r?=q#f", true, "r", true, "q", true, "f")]
    public void UriToUrnMappingRestoresEveryRqfBoundary(
        string text,
        bool hasRComponent,
        string rComponent,
        bool hasQComponent,
        string qComponent,
        bool hasFComponent,
        string fComponent)
    {
        UriReference reference = ParseUri(text);

        UrnParseResult result =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                reference,
                UrnValidationMode.GenericSyntax,
                new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Urn.AssignedName, Is.EqualTo("urn:example:a"));
            Assert.That(result.Urn.HasRComponent, Is.EqualTo(hasRComponent));
            Assert.That(result.Urn.RComponent, Is.EqualTo(rComponent));
            Assert.That(result.Urn.HasQComponent, Is.EqualTo(hasQComponent));
            Assert.That(result.Urn.QComponent, Is.EqualTo(qComponent));
            Assert.That(result.Urn.HasFComponent, Is.EqualTo(hasFComponent));
            Assert.That(result.Urn.FComponent, Is.EqualTo(fComponent));
            Assert.That(result.Urn.OriginalText, Is.EqualTo(text));
        });
    }

    [Test]
    public void RoundTripPreservesCasePercentSpellingAndQuestionMarkData()
    {
        const string text =
            "URN:Example:a%2fb/c?+r?x%Af?=q?+data%00#f?/tail";
        Urn original = ParseUrn(text);

        UriParseResult uri =
            UrnModelConverterCore.ConvertUrnToUriReference(
                original,
                new OperationLimits());
        UrnParseResult roundTrip =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                uri.Reference,
                UrnValidationMode.GenericSyntax,
                new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(uri.Success, Is.True);
            Assert.That(uri.Reference.Path, Is.EqualTo("Example:a%2fb/c"));
            Assert.That(uri.Reference.Query, Is.EqualTo(
                "+r?x%Af?=q?+data%00"));
            Assert.That(uri.Reference.Fragment, Is.EqualTo("f?/tail"));
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(roundTrip.Urn.OriginalText, Is.EqualTo(text));
            Assert.That(roundTrip.Urn.RComponent, Is.EqualTo("r?x%Af"));
            Assert.That(roundTrip.Urn.QComponent, Is.EqualTo("q?+data%00"));
        });
    }

    [Test]
    public void ConversionUsesActiveFieldsInsteadOfDerivedOriginalText()
    {
        Urn urn = new Urn();
        urn.Scheme = "URN";
        urn.NamespaceIdentifier = "Example";
        urn.NamespaceSpecificString = "value";
        urn.AssignedName = "stale-assigned-name";
        urn.HasQComponent = true;
        urn.QComponent = "new-query";
        urn.OriginalText = "stale-original-text";

        UriParseResult uri =
            UrnModelConverterCore.ConvertUrnToUriReference(
                urn,
                new OperationLimits());
        uri.Reference.Query = "=changed-query";
        uri.Reference.HasFragment = true;
        uri.Reference.Fragment = "";
        uri.Reference.OriginalText = "also-stale";
        UrnParseResult convertedBack =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                uri.Reference,
                UrnValidationMode.GenericSyntax,
                new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(uri.Success, Is.True);
            Assert.That(uri.Reference.OriginalText, Is.EqualTo("also-stale"));
            Assert.That(convertedBack.Success, Is.True);
            Assert.That(convertedBack.Urn.AssignedName, Is.EqualTo(
                "URN:Example:value"));
            Assert.That(convertedBack.Urn.HasQComponent, Is.True);
            Assert.That(convertedBack.Urn.QComponent, Is.EqualTo(
                "changed-query"));
            Assert.That(convertedBack.Urn.HasFComponent, Is.True);
            Assert.That(convertedBack.Urn.FComponent, Is.Empty);
            Assert.That(convertedBack.Urn.OriginalText, Is.EqualTo(
                "URN:Example:value?=changed-query#"));
        });
    }

    [Test]
    public void BothDirectionsHonorTheExactOutputLimitBoundary()
    {
        const string text = "urn:example:a?+r?=q#f";
        Urn urn = ParseUrn(text);
        UriReference reference = ParseUri(text);
        OperationLimits exact = new OperationLimits();
        exact.MaxOutputCharacters = text.Length;
        OperationLimits shortLimit = new OperationLimits();
        shortLimit.MaxOutputCharacters = text.Length - 1;

        UriParseResult exactUri =
            UrnModelConverterCore.ConvertUrnToUriReference(urn, exact);
        UriParseResult shortUri =
            UrnModelConverterCore.ConvertUrnToUriReference(urn, shortLimit);
        UrnParseResult exactUrn =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                reference,
                UrnValidationMode.GenericSyntax,
                exact);
        UrnParseResult shortUrn =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                reference,
                UrnValidationMode.GenericSyntax,
                shortLimit);

        Assert.Multiple(() =>
        {
            Assert.That(exactUri.Success, Is.True);
            Assert.That(exactUrn.Success, Is.True);
            AssertConversionFailure(
                shortUri,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output);
            AssertUrnFailure(
                shortUrn,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output);
        });
    }

    [Test]
    public void InvalidUrnModelsReturnUrnDiagnosticsAndEmptyUriModels()
    {
        Urn wrongScheme = ParseUrn("urn:example:a");
        wrongScheme.Scheme = "http";
        Urn emptyRComponent = ParseUrn("urn:example:a");
        emptyRComponent.HasRComponent = true;

        UriParseResult missingUrn =
            UrnModelConverterCore.ConvertUrnToUriReference(
                null,
                new OperationLimits());
        UriParseResult missingLimits =
            UrnModelConverterCore.ConvertUrnToUriReference(
                wrongScheme,
                null);
        UriParseResult scheme =
            UrnModelConverterCore.ConvertUrnToUriReference(
                wrongScheme,
                new OperationLimits());
        UriParseResult rComponent =
            UrnModelConverterCore.ConvertUrnToUriReference(
                emptyRComponent,
                new OperationLimits());

        AssertConversionFailure(
            missingUrn,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertConversionFailure(
            missingLimits,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertConversionFailure(
            scheme,
            UriDiagnosticCode.InvalidUrn,
            UriComponent.Urn);
        AssertConversionFailure(
            rComponent,
            UriDiagnosticCode.InvalidUrn,
            UriComponent.UrnRComponent);
    }

    [Test]
    public void NonUrnUriModelsAndUnknownValidationModesFailCleanly()
    {
        UriReference http = ParseUri("http://example.com/path");
        UriReference urn = ParseUri("urn:example:a");

        UrnParseResult missingReference =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                null,
                UrnValidationMode.GenericSyntax,
                new OperationLimits());
        UrnParseResult missingLimits =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                urn,
                UrnValidationMode.GenericSyntax,
                null);
        UrnParseResult wrongScheme =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                http,
                UrnValidationMode.GenericSyntax,
                new OperationLimits());
        UrnParseResult invalidMode =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                urn,
                (UrnValidationMode)99,
                new OperationLimits());

        AssertUrnFailure(
            missingReference,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertUrnFailure(
            missingLimits,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertUrnFailure(
            wrongScheme,
            UriDiagnosticCode.InvalidUrn,
            UriComponent.Urn);
        AssertUrnFailure(
            invalidMode,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
    }

    [TestCase("urn:example:a?")]
    [TestCase("urn:example:a?ordinary-query")]
    public void GenericQueriesWithoutUrnDelimitersCannotBecomeUrnComponents(
        string text)
    {
        UriReference reference = ParseUri(text);

        UrnParseResult result =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                reference,
                UrnValidationMode.GenericSyntax,
                new OperationLimits());

        AssertUrnFailure(
            result,
            UriDiagnosticCode.InvalidUrnComponentOrder,
            UriComponent.Urn);
    }

    [Test]
    public void UriToUrnConversionAppliesSelectedRegistrationPolicy()
    {
        UriReference reference = ParseUri("urn:x-test:value");

        UrnParseResult generic =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                reference,
                UrnValidationMode.GenericSyntax,
                new OperationLimits());
        UrnParseResult registered =
            UrnModelConverterCore.ConvertUriReferenceToUrn(
                reference,
                UrnValidationMode.RegisteredNamespace,
                new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(generic.Success, Is.True);
            AssertUrnFailure(
                registered,
                UriDiagnosticCode.InvalidNamespaceIdentifier,
                UriComponent.UrnNamespaceIdentifier);
        });
    }

    private static Urn ParseUrn(string text)
    {
        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);
        Assert.That(result.Success, Is.True);
        return result.Urn;
    }

    private static UriReference ParseUri(string text)
    {
        UriParseResult result = UriParserCore.ParseReference(text.AsSpan());
        Assert.That(result.Success, Is.True);
        return result.Reference;
    }

    private static void AssertConversionFailure(
        UriParseResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reference.OriginalText, Is.Empty);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        });
    }

    private static void AssertUrnFailure(
        UrnParseResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Urn.OriginalText, Is.Empty);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        });
    }
}
