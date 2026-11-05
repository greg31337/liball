namespace uri.Tests;

public class UrnOptionalComponentParserTests
{
    [Test]
    public void AssignedNameWithoutOptionalComponentsPreservesAbsence()
    {
        const string text = "URN:Example:a/b";

        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Urn.Scheme, Is.EqualTo("URN"));
            Assert.That(result.Urn.NamespaceIdentifier, Is.EqualTo("Example"));
            Assert.That(result.Urn.NamespaceSpecificString, Is.EqualTo("a/b"));
            Assert.That(result.Urn.AssignedName, Is.EqualTo(text));
            Assert.That(result.Urn.HasRComponent, Is.False);
            Assert.That(result.Urn.RComponent, Is.Empty);
            Assert.That(result.Urn.HasQComponent, Is.False);
            Assert.That(result.Urn.QComponent, Is.Empty);
            Assert.That(result.Urn.HasFComponent, Is.False);
            Assert.That(result.Urn.FComponent, Is.Empty);
            Assert.That(result.Urn.OriginalText, Is.EqualTo(text));
        });
    }

    [TestCase(
        "urn:example:a?+resolver",
        true,
        "resolver",
        false,
        "",
        false,
        "")]
    [TestCase(
        "urn:example:a?=query",
        false,
        "",
        true,
        "query",
        false,
        "")]
    [TestCase(
        "urn:example:a#",
        false,
        "",
        false,
        "",
        true,
        "")]
    [TestCase(
        "urn:example:a#fragment",
        false,
        "",
        false,
        "",
        true,
        "fragment")]
    [TestCase(
        "urn:example:a?+resolver?=query",
        true,
        "resolver",
        true,
        "query",
        false,
        "")]
    [TestCase(
        "urn:example:a?+resolver#fragment",
        true,
        "resolver",
        false,
        "",
        true,
        "fragment")]
    [TestCase(
        "urn:example:a?=query#fragment",
        false,
        "",
        true,
        "query",
        true,
        "fragment")]
    [TestCase(
        "urn:example:a?+resolver?=query#fragment",
        true,
        "resolver",
        true,
        "query",
        true,
        "fragment")]
    public void EveryOptionalComponentCombinationPreservesPresenceAndValue(
        string text,
        bool hasRComponent,
        string rComponent,
        bool hasQComponent,
        string qComponent,
        bool hasFComponent,
        string fComponent)
    {
        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

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
    public void QuestionMarksRemainDataWithinTheirDefinedComponentBoundaries()
    {
        const string text =
            "urn:example:a?+a?b/c?+d?=q?x/y?=z#f?/tail";

        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Urn.HasRComponent, Is.True);
            Assert.That(result.Urn.RComponent, Is.EqualTo("a?b/c?+d"));
            Assert.That(result.Urn.HasQComponent, Is.True);
            Assert.That(result.Urn.QComponent, Is.EqualTo("q?x/y?=z"));
            Assert.That(result.Urn.HasFComponent, Is.True);
            Assert.That(result.Urn.FComponent, Is.EqualTo("f?/tail"));
        });
    }

    [Test]
    public void QComponentTreatsLaterRDelimiterTextAsData()
    {
        const string text = "urn:example:a?=q?+not-an-r-component";

        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Urn.HasRComponent, Is.False);
            Assert.That(result.Urn.HasQComponent, Is.True);
            Assert.That(result.Urn.QComponent, Is.EqualTo(
                "q?+not-an-r-component"));
        });
    }

    [TestCase("urn:example:a?", 13)]
    [TestCase("urn:example:a?x", 13)]
    [TestCase("urn:example:a??", 13)]
    public void BareQuestionMarkAfterAssignedNameIsRejected(
        string text,
        int offset)
    {
        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        AssertUrnFailure(
            result,
            UriDiagnosticCode.InvalidUrnComponentOrder,
            UriComponent.Urn,
            offset,
            1,
            UrnParserCore.InvalidComponentOrderMessage);
    }

    [TestCase("urn:example:a?+", UriComponent.UrnRComponent, 15)]
    [TestCase("urn:example:a?+#f", UriComponent.UrnRComponent, 15)]
    [TestCase("urn:example:a?+?=q", UriComponent.UrnRComponent, 15)]
    [TestCase("urn:example:a?=", UriComponent.UrnQComponent, 15)]
    [TestCase("urn:example:a?=#f", UriComponent.UrnQComponent, 15)]
    [TestCase("urn:example:a?+r?=", UriComponent.UrnQComponent, 18)]
    [TestCase("urn:example:a?+r?=#f", UriComponent.UrnQComponent, 18)]
    public void RAndQComponentsMustBeNonEmpty(
        string text,
        UriComponent component,
        int offset)
    {
        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        string message = UrnParserCore.InvalidRComponentMessage;
        if (component == UriComponent.UrnQComponent)
        {
            message = UrnParserCore.InvalidQComponentMessage;
        }

        AssertUrnFailure(
            result,
            UriDiagnosticCode.InvalidUrn,
            component,
            offset,
            0,
            message);
    }

    [TestCase(
        "urn:example:a?+/path",
        UriComponent.UrnRComponent,
        15,
        UrnParserCore.InvalidRComponentMessage)]
    [TestCase(
        "urn:example:a?+?path",
        UriComponent.UrnRComponent,
        15,
        UrnParserCore.InvalidRComponentMessage)]
    [TestCase(
        "urn:example:a?=/path",
        UriComponent.UrnQComponent,
        15,
        UrnParserCore.InvalidQComponentMessage)]
    [TestCase(
        "urn:example:a?=?path",
        UriComponent.UrnQComponent,
        15,
        UrnParserCore.InvalidQComponentMessage)]
    public void RAndQComponentsMustStartWithPchar(
        string text,
        UriComponent component,
        int offset,
        string message)
    {
        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        AssertUrnFailure(
            result,
            UriDiagnosticCode.InvalidUrn,
            component,
            offset,
            1,
            message);
    }

    [TestCase(
        "urn:example:a?+x y",
        UriComponent.UrnRComponent,
        16,
        UrnParserCore.InvalidRComponentMessage)]
    [TestCase(
        "urn:example:a?=xé",
        UriComponent.UrnQComponent,
        16,
        UrnParserCore.InvalidQComponentMessage)]
    [TestCase(
        "urn:example:a#x[y",
        UriComponent.UrnFComponent,
        15,
        UrnParserCore.InvalidFComponentMessage)]
    [TestCase(
        "urn:example:a#x#y",
        UriComponent.UrnFComponent,
        15,
        UrnParserCore.InvalidFComponentMessage)]
    public void OptionalComponentsRejectInvalidOrNonAsciiWireCharacters(
        string text,
        UriComponent component,
        int offset,
        string message)
    {
        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        AssertUrnFailure(
            result,
            UriDiagnosticCode.InvalidUrn,
            component,
            offset,
            1,
            message);
    }

    [TestCase(
        "urn:example:a?+x%",
        UriComponent.UrnRComponent,
        16)]
    [TestCase(
        "urn:example:a?+x%?=q",
        UriComponent.UrnRComponent,
        16)]
    [TestCase(
        "urn:example:a?=x%G0",
        UriComponent.UrnQComponent,
        17)]
    [TestCase(
        "urn:example:a?=x%#f",
        UriComponent.UrnQComponent,
        16)]
    [TestCase(
        "urn:example:a#x%0G",
        UriComponent.UrnFComponent,
        17)]
    public void OptionalComponentPercentFailuresUseExactUrnDiagnostics(
        string text,
        UriComponent component,
        int offset)
    {
        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        AssertUrnFailure(
            result,
            UriDiagnosticCode.InvalidPercentEncoding,
            component,
            offset,
            1,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    [Test]
    public void ValidPercentTripletsRemainUnchangedInEveryOptionalComponent()
    {
        const string text = "urn:example:a?+r%2f?=q%Af#f%00";

        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Urn.RComponent, Is.EqualTo("r%2f"));
            Assert.That(result.Urn.QComponent, Is.EqualTo("q%Af"));
            Assert.That(result.Urn.FComponent, Is.EqualTo("f%00"));
            Assert.That(result.Urn.OriginalText, Is.EqualTo(text));
        });
    }

    private static void AssertUrnFailure(
        UrnParseResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Urn.OriginalText, Is.Empty);
            Assert.That(result.Urn.AssignedName, Is.Empty);
            Assert.That(result.Urn.HasRComponent, Is.False);
            Assert.That(result.Urn.HasQComponent, Is.False);
            Assert.That(result.Urn.HasFComponent, Is.False);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
        });
    }
}
