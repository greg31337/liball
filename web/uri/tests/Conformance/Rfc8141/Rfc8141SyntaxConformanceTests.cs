namespace uri.Tests;

public class Rfc8141SyntaxConformanceTests
{
    [TestCase(
        "urn:example:a123,z456",
        "a123,z456",
        false,
        "",
        false,
        "",
        false,
        "")]
    [TestCase(
        "urn:example:foo-bar-baz-qux?+CCResolve:cc=uk",
        "foo-bar-baz-qux",
        true,
        "CCResolve:cc=uk",
        false,
        "",
        false,
        "")]
    [TestCase(
        "urn:example:weather?=op=map&lat=39.56&lon=-104.85&datetime=1969-07-21T02:56:15Z",
        "weather",
        false,
        "",
        true,
        "op=map&lat=39.56&lon=-104.85&datetime=1969-07-21T02:56:15Z",
        false,
        "")]
    [TestCase(
        "urn:example:foo-bar-baz-qux#somepart",
        "foo-bar-baz-qux",
        false,
        "",
        false,
        "",
        true,
        "somepart")]
    public void SectionsTwoAndThreeExamplesMatchTheNamestringGrammar(
        string text,
        string namespaceSpecificString,
        bool hasRComponent,
        string rComponent,
        bool hasQComponent,
        string qComponent,
        bool hasFComponent,
        string fComponent)
    {
        Urn urn = Parse(text, UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(urn.NamespaceSpecificString, Is.EqualTo(
                namespaceSpecificString), text);
            Assert.That(urn.HasRComponent, Is.EqualTo(hasRComponent), text);
            Assert.That(urn.RComponent, Is.EqualTo(rComponent), text);
            Assert.That(urn.HasQComponent, Is.EqualTo(hasQComponent), text);
            Assert.That(urn.QComponent, Is.EqualTo(qComponent), text);
            Assert.That(urn.HasFComponent, Is.EqualTo(hasFComponent), text);
            Assert.That(urn.FComponent, Is.EqualTo(fComponent), text);
            Assert.That(urn.OriginalText, Is.EqualTo(text), text);
        });
    }

    [TestCase("urn:example:a", false, false, false)]
    [TestCase("urn:example:a?+r", true, false, false)]
    [TestCase("urn:example:a?=q", false, true, false)]
    [TestCase("urn:example:a#", false, false, true)]
    [TestCase("urn:example:a?+r?=q", true, true, false)]
    [TestCase("urn:example:a?+r#f", true, false, true)]
    [TestCase("urn:example:a?=q#f", false, true, true)]
    [TestCase("urn:example:a?+r?=q#f", true, true, true)]
    public void NamestringAcceptsEveryOptionalComponentCombination(
        string text,
        bool hasRComponent,
        bool hasQComponent,
        bool hasFComponent)
    {
        Urn urn = Parse(text, UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(urn.AssignedName, Is.EqualTo("urn:example:a"), text);
            Assert.That(urn.HasRComponent, Is.EqualTo(hasRComponent), text);
            Assert.That(urn.HasQComponent, Is.EqualTo(hasQComponent), text);
            Assert.That(urn.HasFComponent, Is.EqualTo(hasFComponent), text);
        });
    }

    [Test]
    public void NssAndOptionalComponentsAcceptTheirCompleteAsciiProductions()
    {
        const string pchar = "AZaz09-._~!$&'()*+,;=:@%41";
        string text = "UrN:Example:" + pchar + "/path//tail" +
            "?+" + pchar + "/?resolver" +
            "?=" + pchar + "/?query" +
            "#" + pchar + "/?fragment";

        Urn urn = Parse(text, UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(urn.Scheme, Is.EqualTo("UrN"));
            Assert.That(urn.NamespaceIdentifier, Is.EqualTo("Example"));
            Assert.That(urn.NamespaceSpecificString, Is.EqualTo(
                pchar + "/path//tail"));
            Assert.That(urn.RComponent, Is.EqualTo(
                pchar + "/?resolver"));
            Assert.That(urn.QComponent, Is.EqualTo(
                pchar + "/?query"));
            Assert.That(urn.FComponent, Is.EqualTo(
                pchar + "/?fragment"));
        });
    }

    [Test]
    public void NidAcceptsTwoAndThirtyTwoCharacterBoundaries()
    {
        string maximum = "a" + new string('b', 30) + "9";

        Urn minimum = Parse(
            "urn:a9:value",
            UrnValidationMode.GenericSyntax);
        Urn interior = Parse(
            "URN:Example-123:value",
            UrnValidationMode.GenericSyntax);
        Urn maximumResult = Parse(
            "urn:" + maximum + ":value",
            UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(minimum.NamespaceIdentifier, Is.EqualTo("a9"));
            Assert.That(interior.NamespaceIdentifier, Is.EqualTo(
                "Example-123"));
            Assert.That(maximumResult.NamespaceIdentifier, Is.EqualTo(
                maximum));
        });
    }

    [TestCase("urn::value", 4, 0)]
    [TestCase("urn:a:value", 4, 1)]
    [TestCase("urn:-ab:value", 4, 1)]
    [TestCase("urn:ab-:value", 6, 1)]
    [TestCase("urn:a_b:value", 5, 1)]
    [TestCase("urn:aé:value", 5, 1)]
    [TestCase("urn:ab", 6, 0)]
    public void NidRejectsValuesOutsideItsLdhAndLengthGrammar(
        string text,
        int offset,
        int length)
    {
        AssertFailure(
            text,
            UrnValidationMode.GenericSyntax,
            UriDiagnosticCode.InvalidNamespaceIdentifier,
            UriComponent.UrnNamespaceIdentifier,
            offset,
            length);
    }

    [Test]
    public void NidRejectsTheThirtyThirdCharacterPrecisely()
    {
        string tooLong = "a" + new string('b', 31) + "9";

        AssertFailure(
            "urn:" + tooLong + ":value",
            UrnValidationMode.GenericSyntax,
            UriDiagnosticCode.InvalidNamespaceIdentifier,
            UriComponent.UrnNamespaceIdentifier,
            36,
            1);
    }

    [TestCase("urn:a1:value")]
    [TestCase("urn:x-test:value")]
    [TestCase("urn:ab-test:value")]
    public void GenericModeDoesNotApplyRegistrationRestrictions(string text)
    {
        Urn urn = Parse(text, UrnValidationMode.GenericSyntax);

        Assert.That(urn.OriginalText, Is.EqualTo(text));
    }

    [TestCase("urn:a1:value", 4, 2)]
    [TestCase("urn:x-test:value", 4, 2)]
    [TestCase("urn:ab-test:value", 4, 3)]
    [TestCase("urn:xn--test:value", 4, 3)]
    [TestCase("urn:urn-0:value", 4, 5)]
    public void RegisteredModeAppliesRfcNamespaceRegistrationRestrictions(
        string text,
        int offset,
        int length)
    {
        AssertFailure(
            text,
            UrnValidationMode.RegisteredNamespace,
            UriDiagnosticCode.InvalidNamespaceIdentifier,
            UriComponent.UrnNamespaceIdentifier,
            offset,
            length);
    }

    [TestCase("urn:example:", 12, 0)]
    [TestCase("urn:example:/path", 12, 1)]
    [TestCase("urn:example:a b", 13, 1)]
    [TestCase("urn:example:a[b", 13, 1)]
    [TestCase("urn:example:aé", 13, 1)]
    public void NssRejectsEmptyLeadingSlashAndNonWireCharacters(
        string text,
        int offset,
        int length)
    {
        AssertFailure(
            text,
            UrnValidationMode.GenericSyntax,
            UriDiagnosticCode.InvalidNamespaceSpecificString,
            UriComponent.UrnNamespaceSpecificString,
            offset,
            length);
    }

    [Test]
    public void EncodedSlashCanBeginNssWhileRawSlashCannot()
    {
        Urn encoded = Parse(
            "urn:example:%2Fpath",
            UrnValidationMode.GenericSyntax);
        Urn mixedCase = Parse(
            "urn:example:%2fpath",
            UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.NamespaceSpecificString, Is.EqualTo(
                "%2Fpath"));
            Assert.That(mixedCase.NamespaceSpecificString, Is.EqualTo(
                "%2fpath"));
        });
    }

    [TestCase(
        "urn:example:a%",
        UriComponent.UrnNamespaceSpecificString,
        13)]
    [TestCase(
        "urn:example:a%G0",
        UriComponent.UrnNamespaceSpecificString,
        14)]
    [TestCase(
        "urn:example:a%0G",
        UriComponent.UrnNamespaceSpecificString,
        15)]
    [TestCase(
        "urn:example:a?+r%",
        UriComponent.UrnRComponent,
        16)]
    [TestCase(
        "urn:example:a?=q%G0",
        UriComponent.UrnQComponent,
        17)]
    [TestCase(
        "urn:example:a#f%0G",
        UriComponent.UrnFComponent,
        17)]
    public void MalformedPercentTripletsReportTheirOwningComponentAndOctet(
        string text,
        UriComponent component,
        int offset)
    {
        AssertFailure(
            text,
            UrnValidationMode.GenericSyntax,
            UriDiagnosticCode.InvalidPercentEncoding,
            component,
            offset,
            1);
    }

    [Test]
    public void ValidPercentTripletsRemainLexicallyUnchangedEverywhere()
    {
        const string text =
            "urn:example:%2f%AF%00?+r%2f?=q%Af#f%00";

        Urn urn = Parse(text, UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(urn.NamespaceSpecificString, Is.EqualTo(
                "%2f%AF%00"));
            Assert.That(urn.RComponent, Is.EqualTo("r%2f"));
            Assert.That(urn.QComponent, Is.EqualTo("q%Af"));
            Assert.That(urn.FComponent, Is.EqualTo("f%00"));
            Assert.That(urn.OriginalText, Is.EqualTo(text));
        });
    }

    [TestCase("urn:example:a?", 13)]
    [TestCase("urn:example:a?x", 13)]
    [TestCase("urn:example:a??", 13)]
    public void BareQuestionMarkCannotIntroduceAnOptionalComponent(
        string text,
        int offset)
    {
        AssertFailure(
            text,
            UrnValidationMode.GenericSyntax,
            UriDiagnosticCode.InvalidUrnComponentOrder,
            UriComponent.Urn,
            offset,
            1);
    }

    [TestCase(
        "urn:example:a?+",
        UriComponent.UrnRComponent,
        15)]
    [TestCase(
        "urn:example:a?+?=q",
        UriComponent.UrnRComponent,
        15)]
    [TestCase(
        "urn:example:a?=",
        UriComponent.UrnQComponent,
        15)]
    [TestCase(
        "urn:example:a?+r?=#f",
        UriComponent.UrnQComponent,
        18)]
    public void RAndQDelimitersRequireNonEmptyComponents(
        string text,
        UriComponent component,
        int offset)
    {
        AssertFailure(
            text,
            UrnValidationMode.GenericSyntax,
            UriDiagnosticCode.InvalidUrn,
            component,
            offset,
            0);
    }

    [Test]
    public void DelimiterRecognitionFollowsTheRqfGrammarOrder()
    {
        Urn ordered = Parse(
            "urn:example:a?+r?=q#f",
            UrnValidationMode.GenericSyntax);
        Urn qData = Parse(
            "urn:example:a?=q?+not-an-r-component",
            UrnValidationMode.GenericSyntax);
        Urn fragmentData = Parse(
            "urn:example:a#f?+not-r?=not-q",
            UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(ordered.RComponent, Is.EqualTo("r"));
            Assert.That(ordered.QComponent, Is.EqualTo("q"));
            Assert.That(ordered.FComponent, Is.EqualTo("f"));
            Assert.That(qData.HasRComponent, Is.False);
            Assert.That(qData.QComponent, Is.EqualTo(
                "q?+not-an-r-component"));
            Assert.That(fragmentData.HasRComponent, Is.False);
            Assert.That(fragmentData.HasQComponent, Is.False);
            Assert.That(fragmentData.FComponent, Is.EqualTo(
                "f?+not-r?=not-q"));
        });
    }

    private static Urn Parse(
        string text,
        UrnValidationMode validationMode)
    {
        UrnParseOptions options = new UrnParseOptions();
        options.ValidationMode = validationMode;
        IUrnParser parser = new UrnParser();

        UrnParseResult result = parser.TryParseUrn(text, options);

        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        return result.Urn;
    }

    private static void AssertFailure(
        string text,
        UrnValidationMode validationMode,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length)
    {
        UrnParseOptions options = new UrnParseOptions();
        options.ValidationMode = validationMode;
        IUrnParser parser = new UrnParser();

        UrnParseResult result = parser.TryParseUrn(text, options);

        Assert.That(result.Diagnostic, Is.Not.Null, text);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, text);
            Assert.That(result.Urn.OriginalText, Is.Empty, text);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn), text);
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code), text);
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error), text);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(
                component), text);
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(
                offset), text);
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(
                length), text);
        });
    }
}
