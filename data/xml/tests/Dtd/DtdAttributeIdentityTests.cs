using xml;

namespace xml.Tests;

[TestFixture]
public class DtdAttributeIdentityTests
{
    [Test]
    public void AcceptsUniqueIdsAndDeferredForwardIdReferences()
    {
        string subset = "<!ELEMENT root ANY><!ELEMENT item ANY>" +
            "<!ATTLIST root references IDREFS #REQUIRED>" +
            "<!ATTLIST item identifier ID #REQUIRED>";
        string text = "<!DOCTYPE root [" + subset + "]><root references='later first'><item identifier='first'/><item identifier='later'/></root>";

        XmlDocument document = Parse(text, XmlValidationMode.Dtd, null);

        Assert.That(document.DocumentElement!.Attributes![0].Value, Is.EqualTo("later first"));
    }

    [Test]
    public void RejectsDuplicateIdValues()
    {
        string subset = "<!ELEMENT root ANY><!ELEMENT item ANY><!ATTLIST item identifier ID #REQUIRED>";
        string text = "<!DOCTYPE root [" + subset + "]><root><item identifier='same'/><item identifier='same'/></root>";

        XmlValidationException failure = Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, null))!;

        Assert.That(failure.Message, Does.Contain("occurs more than once"));
    }

    [Test]
    public void RejectsUnresolvedIdReferencesOnlyAfterTheDocumentIsComplete()
    {
        string subset = "<!ELEMENT root ANY><!ATTLIST root reference IDREF #REQUIRED>";
        string text = "<!DOCTYPE root [" + subset + "]><root reference='missing'/>";
        RecordingIdentityReporter reporter = new RecordingIdentityReporter();

        XmlValidationException failure = Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, reporter))!;

        Assert.That(failure.Message, Does.Contain("does not match any ID"));
        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.DtdValidity));
        Assert.That(failure.Location!.CharacterOffset, Is.EqualTo(text.IndexOf("<root reference", StringComparison.Ordinal)));
    }

    [TestCase("<!ATTLIST root first ID #IMPLIED second ID #IMPLIED>")]
    [TestCase("<!ATTLIST root identifier ID 'default'>")]
    [TestCase("<!ATTLIST root identifier ID #FIXED 'default'>")]
    public void EnforcesIdDeclarationRestrictions(string declaration)
    {
        string text = "<!DOCTYPE root [<!ELEMENT root ANY>" + declaration + "]><root first='one' second='two' identifier='value'/>";

        Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, null));
    }

    [TestCase("<!ATTLIST unused reference IDREF '1bad'>")]
    [TestCase("<!ATTLIST unused references IDREFS 'good 1bad'>")]
    [TestCase("<!ATTLIST unused entity ENTITY '1bad'>")]
    [TestCase("<!ATTLIST unused entities ENTITIES 'good 1bad'>")]
    [TestCase("<!ATTLIST unused token NMTOKEN 'bad/value'>")]
    [TestCase("<!ATTLIST unused tokens NMTOKENS 'good bad/value'>")]
    public void ValidatesTokenizedDefaultSyntaxEvenWhenTheElementDoesNotAppear(string declaration)
    {
        string text = "<!DOCTYPE root [<!ELEMENT root ANY>" + declaration + "]><root/>";

        Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, null));
    }

    [Test]
    public void AcceptsEveryValidNameAndNmtokenFamily()
    {
        string subset = "<!ELEMENT root ANY><!ATTLIST root " +
            "identifier ID #REQUIRED " +
            "reference IDREF #REQUIRED " +
            "references IDREFS #REQUIRED " +
            "token NMTOKEN #REQUIRED " +
            "tokens NMTOKENS #REQUIRED>";
        string text = "<!DOCTYPE root [" + subset + "]><root identifier='p:id' reference='p:id' references='p:id p:id' token='1:start-name' tokens='one 2:two'/>";

        XmlDocument document = Parse(text, XmlValidationMode.Dtd, null);

        Assert.That(document.DocumentElement!.Attributes![3].Value, Is.EqualTo("1:start-name"));
        Assert.That(document.DocumentElement.Attributes[4].Value, Is.EqualTo("one 2:two"));
    }

    [TestCase("ID", "1bad")]
    [TestCase("IDREF", "1bad")]
    [TestCase("IDREFS", "valid 1bad")]
    [TestCase("NMTOKEN", "bad/value")]
    [TestCase("NMTOKENS", "valid bad/value")]
    [TestCase("NMTOKENS", "valid&#xA;other")]
    public void RejectsInvalidTokenizedInstanceSyntax(string typeName, string value)
    {
        string subset = "<!ELEMENT root ANY><!ATTLIST root value " + typeName + " #REQUIRED>";
        string text = "<!DOCTYPE root [" + subset + "]><root value='" + value + "'/>";

        Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, null));
    }

    [Test]
    public void ValidatesEntityAndEntitiesAgainstUnparsedDeclarations()
    {
        string subset = "<!ELEMENT root ANY>" +
            "<!NOTATION png SYSTEM 'image/png'>" +
            "<!ENTITY first SYSTEM 'first.png' NDATA png>" +
            "<!ENTITY second SYSTEM 'second.png' NDATA png>" +
            "<!ATTLIST root image ENTITY #REQUIRED images ENTITIES #REQUIRED>";
        string text = "<!DOCTYPE root [" + subset + "]><root image='first' images='first second'/>";

        XmlDocument document = Parse(text, XmlValidationMode.Dtd, null);

        Assert.That(document.DocumentElement!.Attributes![0].Value, Is.EqualTo("first"));
        Assert.That(document.DocumentElement.Attributes[1].Value, Is.EqualTo("first second"));
    }

    [TestCase("missing")]
    [TestCase("parsed")]
    [TestCase("externalParsed")]
    public void RejectsEntityValuesThatDoNotNameUnparsedEntities(string value)
    {
        string subset = "<!ELEMENT root ANY>" +
            "<!ENTITY parsed 'content'>" +
            "<!ENTITY externalParsed SYSTEM 'parsed.xml'>" +
            "<!ATTLIST root image ENTITY #REQUIRED>";
        string text = "<!DOCTYPE root [" + subset + "]><root image='" + value + "'/>";

        Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, null));
    }

    [Test]
    public void ChecksEntityDefaultsWhenTheyAreApplied()
    {
        string validSubset = "<!ELEMENT root ANY><!NOTATION png SYSTEM 'image/png'><!ENTITY logo SYSTEM 'logo.png' NDATA png><!ATTLIST root image ENTITY 'logo'>";
        string invalidSubset = "<!ELEMENT root ANY><!ATTLIST root image ENTITY 'missing'>";

        Assert.That(Parse("<!DOCTYPE root [" + validSubset + "]><root/>", XmlValidationMode.Dtd, null).DocumentElement!.Attributes![0].Value, Is.EqualTo("logo"));
        Assert.Throws<XmlValidationException>(() => Parse("<!DOCTYPE root [" + invalidSubset + "]><root/>", XmlValidationMode.Dtd, null));
    }

    [Test]
    public void IncludesIdsFromElementMarkupInEntityReplacementText()
    {
        string subset = "<!ELEMENT root ANY><!ELEMENT item ANY>" +
            "<!ATTLIST root reference IDREF #REQUIRED><!ATTLIST item identifier ID #REQUIRED>" +
            "<!ENTITY generated \"<item identifier='fromEntity'/>\">";
        string text = "<!DOCTYPE root [" + subset + "]><root reference='fromEntity'>&generated;</root>";

        Assert.That(Parse(text, XmlValidationMode.Dtd, null).DocumentElement, Is.Not.Null);
    }

    [Test]
    public void SkipsIdentityAndTokenValidityChecksOutsideDtdValidation()
    {
        string subset = "<!ELEMENT root ANY><!ATTLIST root identifier ID 'default' reference IDREF #IMPLIED token NMTOKEN #IMPLIED>";
        string text = "<!DOCTYPE root [" + subset + "]><root reference='missing' token='bad/value'/>";

        XmlDocument document = Parse(text, XmlValidationMode.None, null);

        Assert.That(document.DocumentElement!.Attributes, Has.Length.EqualTo(3));
    }

    private static XmlDocument Parse(string text, XmlValidationMode validationMode, IXmlErrorReporter? reporter)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        input.BaseUri = "urn:document";
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = validationMode;
        return new XmlParser().ParseXmlDocument(input, options, null, reporter, null);
    }

    private sealed class RecordingIdentityReporter : IXmlErrorReporter
    {
        public XmlDiagnostic? Diagnostic;

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }
    }
}
