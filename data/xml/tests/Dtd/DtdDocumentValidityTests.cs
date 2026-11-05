using xml;

namespace xml.Tests;

[TestFixture]
public class DtdDocumentValidityTests
{
    [TestCase("<!ELEMENT root EMPTY><!ELEMENT root ANY>", "Element type 'root'")]
    [TestCase("<!ELEMENT root ANY><!NOTATION image SYSTEM 'first'><!NOTATION image SYSTEM 'second'>", "Notation 'image'")]
    public void RejectsDuplicateElementAndNotationDeclarations(string subset, string expectedMessage)
    {
        string text = "<!DOCTYPE root [" + subset + "]><root/>";

        XmlValidationException failure = Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, null))!;

        Assert.That(failure.Message, Does.Contain(expectedMessage));
        Assert.That(failure.Message, Does.Contain("declared more than once"));
    }

    [Test]
    public void AccumulatesEveryDuplicateDeclarationValidityError()
    {
        string subset = "<!ELEMENT root EMPTY><!ELEMENT root ANY>" +
            "<!NOTATION image SYSTEM 'first'><!NOTATION image SYSTEM 'second'>";
        string text = "<!DOCTYPE root [" + subset + "]><root/>";
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.Dtd;
        options.AccumulateValidationDiagnostics = true;

        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, null, null, null);

        Assert.That(document.Diagnostics, Has.Length.EqualTo(2));
        Assert.That(document.Diagnostics![0].Message, Does.Contain("Element type 'root'"));
        Assert.That(document.Diagnostics[1].Message, Does.Contain("Notation 'image'"));
    }

    [Test]
    public void KeepsFirstDuplicateDeclarationsWhenValidationIsDisabled()
    {
        string subset = "<!ELEMENT root EMPTY><!ELEMENT root ANY>" +
            "<!NOTATION image SYSTEM 'first'><!NOTATION image SYSTEM 'second'>";

        XmlDocument document = Parse("<!DOCTYPE root [" + subset + "]><root/>", XmlValidationMode.None, null);

        Assert.That(document.DocumentType!.ElementDeclarations, Has.Length.EqualTo(1));
        Assert.That(document.DocumentType.ElementDeclarations![0].ContentModel, Is.EqualTo("EMPTY"));
        Assert.That(document.DocumentType.NotationDeclarations, Has.Length.EqualTo(1));
        Assert.That(document.DocumentType.NotationDeclarations![0].SystemIdentifier, Is.EqualTo("first"));
    }

    [TestCase("<!ELEMENT root ANY>", "<root><child/></root>")]
    [TestCase("<!ELEMENT child EMPTY>", "<root><child/></root>")]
    [TestCase("<!ELEMENT root ANY>", "<root extra='value'/>")]
    public void RejectsUndeclaredElementsAndAttributes(string subset, string content)
    {
        string text = "<!DOCTYPE root [" + subset + "]>" + content;

        Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, null));
    }

    [Test]
    public void AcceptsDeclaredElementsAndAttributes()
    {
        string subset = "<!ELEMENT root (child)><!ELEMENT child EMPTY><!ATTLIST child value CDATA #REQUIRED>";

        XmlDocument document = Parse("<!DOCTYPE root [" + subset + "]><root><child value='ready'/></root>", XmlValidationMode.Dtd, null);

        Assert.That(document.DocumentElement!.Children![0].Attributes![0].Value, Is.EqualTo("ready"));
    }

    [Test]
    public void ValidatesNotationAttributeDeclarationsAndValues()
    {
        string subset = "<!ELEMENT root ANY><!NOTATION png SYSTEM 'image/png'><!NOTATION svg SYSTEM 'image/svg+xml'><!ATTLIST root format NOTATION (png|svg) #REQUIRED>";

        XmlDocument document = Parse("<!DOCTYPE root [" + subset + "]><root format='svg'/>", XmlValidationMode.Dtd, null);

        Assert.That(document.DocumentElement!.Attributes![0].Value, Is.EqualTo("svg"));
    }

    [TestCase("<!ELEMENT root ANY><!ATTLIST root format NOTATION (missing) #IMPLIED>")]
    [TestCase("<!ELEMENT root ANY><!NOTATION first SYSTEM 'first'><!NOTATION second SYSTEM 'second'><!ATTLIST root a NOTATION (first) #IMPLIED b NOTATION (second) #IMPLIED>")]
    [TestCase("<!ELEMENT root EMPTY><!NOTATION png SYSTEM 'image/png'><!ATTLIST root format NOTATION (png) #IMPLIED>")]
    public void RejectsInvalidNotationAttributeDeclarations(string subset)
    {
        Assert.Throws<XmlValidationException>(() => Parse("<!DOCTYPE root [" + subset + "]><root/>", XmlValidationMode.Dtd, null));
    }

    [Test]
    public void RequiresUnparsedEntityNotationsToBeDeclared()
    {
        string invalid = "<!DOCTYPE root [<!ELEMENT root ANY><!ENTITY logo SYSTEM 'logo.png' NDATA png>]><root/>";
        string valid = "<!DOCTYPE root [<!ELEMENT root ANY><!ENTITY logo SYSTEM 'logo.png' NDATA png><!NOTATION png SYSTEM 'image/png'>]><root/>";

        Assert.Throws<XmlValidationException>(() => Parse(invalid, XmlValidationMode.Dtd, null));
        Assert.That(Parse(valid, XmlValidationMode.Dtd, null).DocumentElement, Is.Not.Null);
    }

    [Test]
    public void SkipsDocumentDeclarationValidityChecksWhenValidationIsDisabled()
    {
        string text = "<!DOCTYPE root [<!ELEMENT declared EMPTY><!ENTITY logo SYSTEM 'logo.png' NDATA missing><!ATTLIST unused format NOTATION (missing) #IMPLIED>]><root extra='value'><child/></root>";

        XmlDocument document = Parse(text, XmlValidationMode.None, null);

        Assert.That(document.DocumentElement!.Children![0].Name!.QualifiedName, Is.EqualTo("child"));
    }

    [Test]
    public void RejectsExternallyDeclaredEntityReferencesInStandaloneDocuments()
    {
        DocumentValidityResolver resolver = new DocumentValidityResolver();
        resolver.ExternalSubset = "<!ELEMENT root (#PCDATA)><!ENTITY message 'external'>";
        string yes = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root SYSTEM 'root.dtd'><root>&message;</root>";
        string no = "<?xml version='1.0' standalone='no'?><!DOCTYPE root SYSTEM 'root.dtd'><root>&message;</root>";

        Assert.Throws<XmlWellFormednessException>(() => Parse(yes, XmlValidationMode.Dtd, resolver));
        Assert.That(Parse(no, XmlValidationMode.Dtd, resolver).DocumentElement!.Children![0].Value, Is.EqualTo("external"));
    }

    [Test]
    public void DetectsIndirectExternalEntityDependenciesInStandaloneDocuments()
    {
        DocumentValidityResolver resolver = new DocumentValidityResolver();
        resolver.ExternalSubset = "<!ELEMENT root (#PCDATA)><!ENTITY external 'value'>";
        string text = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root SYSTEM 'root.dtd' [<!ENTITY local '&external;'>]><root>&local;</root>";

        Assert.Throws<XmlWellFormednessException>(() => Parse(text, XmlValidationMode.Dtd, resolver));
    }

    [Test]
    public void AllowsReferencesToInternallyDeclaredExternalEntitiesInStandaloneContent()
    {
        DocumentValidityResolver resolver = new DocumentValidityResolver();
        resolver.GeneralEntity = "value";
        string text = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root [<!ELEMENT root (#PCDATA)><!ENTITY message SYSTEM 'message.ent'>]><root>&message;</root>";

        Assert.That(Parse(text, XmlValidationMode.Dtd, resolver).DocumentElement!.Children![0].Value, Is.EqualTo("value"));
    }

    [Test]
    public void RejectsInternalDefaultsThatDependOnExternallyDeclaredEntitiesWhenStandalone()
    {
        DocumentValidityResolver resolver = new DocumentValidityResolver();
        resolver.ExternalSubset = "<!ELEMENT root ANY><!ENTITY external 'value'>";
        string text = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root SYSTEM 'root.dtd' [<!ATTLIST root added CDATA '&external;'>]><root/>";

        Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, resolver));
    }

    [Test]
    public void EnforcesStandaloneWhitespaceForExternalElementContentDeclarations()
    {
        DocumentValidityResolver resolver = new DocumentValidityResolver();
        resolver.ExternalSubset = "<!ELEMENT root (child)><!ELEMENT child EMPTY>";
        string spaced = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root SYSTEM 'root.dtd'><root> \n<child/></root>";
        string compact = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root SYSTEM 'root.dtd'><root><child/></root>";
        string nonStandalone = "<?xml version='1.0' standalone='no'?><!DOCTYPE root SYSTEM 'root.dtd'><root> \n<child/></root>";

        Assert.Throws<XmlValidationException>(() => Parse(spaced, XmlValidationMode.Dtd, resolver));
        Assert.That(Parse(compact, XmlValidationMode.Dtd, resolver).DocumentElement, Is.Not.Null);
        Assert.That(Parse(nonStandalone, XmlValidationMode.Dtd, resolver).DocumentElement, Is.Not.Null);
    }

    [Test]
    public void TreatsElementDeclarationsInsideParameterEntitiesAsExternalForStandalone()
    {
        string subset = "<!ENTITY % model \"<!ELEMENT root (child)>\">%model;<!ELEMENT child EMPTY>";
        string text = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root [" + subset + "]><root> <child/></root>";

        Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, null));
    }

    [Test]
    public void ValidatesElementsAndAttributesInsideEntityReplacementMarkup()
    {
        string validSubset = "<!ELEMENT root ANY><!ELEMENT child EMPTY><!ATTLIST child value CDATA #REQUIRED><!ENTITY generated \"<child value='ok'/>\">";
        string invalidElementSubset = "<!ELEMENT root ANY><!ENTITY generated '<child/>'>";
        string invalidAttributeSubset = "<!ELEMENT root ANY><!ELEMENT child EMPTY><!ENTITY generated \"<child extra='bad'/>\">";

        Assert.That(Parse("<!DOCTYPE root [" + validSubset + "]><root>&generated;</root>", XmlValidationMode.Dtd, null).DocumentElement, Is.Not.Null);
        Assert.Throws<XmlValidationException>(() => Parse("<!DOCTYPE root [" + invalidElementSubset + "]><root>&generated;</root>", XmlValidationMode.Dtd, null));
        Assert.Throws<XmlValidationException>(() => Parse("<!DOCTYPE root [" + invalidAttributeSubset + "]><root>&generated;</root>", XmlValidationMode.Dtd, null));
    }

    private static XmlDocument Parse(string text, XmlValidationMode validationMode, IXmlResourceResolver? resolver)
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
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        options.ResolverPolicy = policy;
        return new XmlParser().ParseXmlDocument(input, options, resolver, null, null);
    }

    private sealed class DocumentValidityResolver : IXmlResourceResolver
    {
        public string ExternalSubset = "";
        public string GeneralEntity = "";

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = kind == XmlResourceKind.ExternalSubset ? ExternalSubset : GeneralEntity;
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = kind;
            resource.Access = XmlResourceAccess.Allowed;
            resource.Input = input;
            resource.AbsoluteUri = kind == XmlResourceKind.ExternalSubset ? "urn:root.dtd" : "urn:message.ent";
            return resource;
        }
    }
}
