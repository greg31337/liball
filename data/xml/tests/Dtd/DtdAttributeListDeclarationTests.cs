using xml;

namespace xml.Tests;

[TestFixture]
public class DtdAttributeListDeclarationTests
{
    [Test]
    public void ParsesEveryNamedAttributeType()
    {
        string source = "<!ATTLIST root " +
            "text CDATA #IMPLIED " +
            "identifier ID #REQUIRED " +
            "reference IDREF 'target' " +
            "references IDREFS #FIXED 'first second' " +
            "entity ENTITY #IMPLIED " +
            "entities ENTITIES #IMPLIED " +
            "token NMTOKEN #IMPLIED " +
            "tokens NMTOKENS #IMPLIED>";
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        DtdSubset subset = parser.LoadDtdSubsets(source, null, null, null);

        Assert.That(subset.AttributeDeclarations, Has.Length.EqualTo(8));
        Assert.That(subset.AttributeDeclarations.Select(item => item.TypeName), Is.EqualTo(new string[] { "CDATA", "ID", "IDREF", "IDREFS", "ENTITY", "ENTITIES", "NMTOKEN", "NMTOKENS" }));
        Assert.That(subset.AttributeDeclarations[0].DefaultKind, Is.EqualTo("#IMPLIED"));
        Assert.That(subset.AttributeDeclarations[1].IsId, Is.True);
        Assert.That(subset.AttributeDeclarations[2].DefaultKind, Is.EqualTo("DEFAULT"));
        Assert.That(subset.AttributeDeclarations[2].DefaultValue, Is.EqualTo("target"));
        Assert.That(subset.AttributeDeclarations[3].DefaultKind, Is.EqualTo("#FIXED"));
        Assert.That(subset.AttributeDeclarations[3].DefaultValue, Is.EqualTo("first second"));
    }

    [Test]
    public void ParsesEnumerationsAndNotationNameLists()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        DtdSubset subset = parser.LoadDtdSubsets("<!ATTLIST root state ( ready | 1-start | a:b ) 'ready' format NOTATION ( png | svg ) #FIXED 'png'>", null, null, null);

        XmlAttributeDeclaration enumeration = subset.AttributeDeclarations[0];
        Assert.That(enumeration.TypeName, Is.EqualTo("ENUMERATION"));
        Assert.That(enumeration.AllowedValues, Is.EqualTo(new string[] { "ready", "1-start", "a:b" }));
        XmlAttributeDeclaration notation = subset.AttributeDeclarations[1];
        Assert.That(notation.TypeName, Is.EqualTo("NOTATION"));
        Assert.That(notation.AllowedValues, Is.EqualTo(new string[] { "png", "svg" }));
    }

    [Test]
    public void RetainsNamesDefaultReferencesAndNamespaceDeclarationStatus()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml11, null, null);

        DtdSubset subset = parser.LoadDtdSubsets("<!ATTLIST p:root xmlns CDATA #IMPLIED xmlns:q CDATA 'urn:q' p:value CDATA '&amp;&#65;'>", null, null, null);

        XmlAttributeDeclaration declaration = subset.AttributeDeclarations[2];
        Assert.That(declaration.ElementName!.QualifiedName, Is.EqualTo("p:root"));
        Assert.That(declaration.ElementName.Prefix, Is.EqualTo("p"));
        Assert.That(declaration.ElementName.LocalName, Is.EqualTo("root"));
        Assert.That(declaration.Name!.QualifiedName, Is.EqualTo("p:value"));
        Assert.That(declaration.Name.Prefix, Is.EqualTo("p"));
        Assert.That(declaration.Name.LocalName, Is.EqualTo("value"));
        Assert.That(declaration.DefaultValue, Is.EqualTo("&A"));
        Assert.That(subset.AttributeDeclarations[0].IsNamespaceDeclaration, Is.True);
        Assert.That(subset.AttributeDeclarations[1].IsNamespaceDeclaration, Is.True);
        Assert.That(declaration.IsNamespaceDeclaration, Is.False);
    }

    [Test]
    public void AppliesFirstAttributeDeclarationPrecedencePerElementAndName()
    {
        string source = "<!ATTLIST root value CDATA 'first'><!ATTLIST root value ID #REQUIRED other CDATA #IMPLIED><!ATTLIST child value ID #IMPLIED><!ATTLIST empty>";
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        DtdSubset subset = parser.LoadDtdSubsets(source, null, null, null);

        Assert.That(subset.AttributeDeclarations, Has.Length.EqualTo(3));
        Assert.That(subset.AttributeDeclarations[0].ElementName!.QualifiedName, Is.EqualTo("root"));
        Assert.That(subset.AttributeDeclarations[0].Name!.QualifiedName, Is.EqualTo("value"));
        Assert.That(subset.AttributeDeclarations[0].TypeName, Is.EqualTo("CDATA"));
        Assert.That(subset.AttributeDeclarations[0].DefaultValue, Is.EqualTo("first"));
        Assert.That(subset.AttributeDeclarations[1].Name!.QualifiedName, Is.EqualTo("other"));
        Assert.That(subset.AttributeDeclarations[2].ElementName!.QualifiedName, Is.EqualTo("child"));
    }

    [Test]
    public void ExpandsAttributeDefinitionsFromExternalParameterEntities()
    {
        AttributeDeclarationResolver resolver = new AttributeDeclarationResolver();
        resolver.Text = "<!ENTITY % attributes 'first CDATA #IMPLIED second (one|two) \"one\"'><!ATTLIST root %attributes;>";
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", "urn:document");

        Assert.That(subset.AttributeDeclarations, Has.Length.EqualTo(2));
        Assert.That(subset.AttributeDeclarations[0].Name!.QualifiedName, Is.EqualTo("first"));
        Assert.That(subset.AttributeDeclarations[1].AllowedValues, Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(subset.AttributeDeclarations[1].DefaultValue, Is.EqualTo("one"));
    }

    [Test]
    public void PublishesRetainedAttributeDeclarationsOnTheDocumentType()
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = "<!DOCTYPE root [<!ELEMENT root ANY><!ATTLIST root status (ready|waiting) #IMPLIED>]><root/>";

        XmlDocument document = new XmlParser().ParseXmlDocument(input, ParserOptions(), null, null, null);

        Assert.That(document.DocumentType!.AttributeDeclarations, Has.Length.EqualTo(1));
        Assert.That(document.DocumentType.AttributeDeclarations![0].Name!.QualifiedName, Is.EqualTo("status"));
        Assert.That(document.DocumentType.AttributeDeclarations[0].AllowedValues, Is.EqualTo(new string[] { "ready", "waiting" }));
    }

    [TestCase("<!ATTLIST>")]
    [TestCase("<!ATTLIST root value>")]
    [TestCase("<!ATTLIST root value unknown #IMPLIED>")]
    [TestCase("<!ATTLIST root value CDATA>")]
    [TestCase("<!ATTLIST root value CDATA # REQUIRED>")]
    [TestCase("<!ATTLIST root value CDATA #FIXED>")]
    [TestCase("<!ATTLIST root value CDATA #FIXED unquoted>")]
    [TestCase("<!ATTLIST root value NOTATION(png) #IMPLIED>")]
    [TestCase("<!ATTLIST root value NOTATION () #IMPLIED>")]
    [TestCase("<!ATTLIST root value NOTATION (1png) #IMPLIED>")]
    [TestCase("<!ATTLIST root value () #IMPLIED>")]
    [TestCase("<!ATTLIST root value (one|) #IMPLIED>")]
    [TestCase("<!ATTLIST root value (one,two) #IMPLIED>")]
    [TestCase("<!ATTLIST root value CDATA 'default'next CDATA #IMPLIED>")]
    [TestCase("<!ATTLISTroot value CDATA #IMPLIED>")]
    [TestCase("<! ATTLIST root value CDATA #IMPLIED>")]
    public void RejectsMalformedAttributeListDeclarations(string declaration)
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        Assert.Throws<FormatException>(() => parser.LoadDtdSubsets(declaration, null, null, null));
    }

    [TestCase("<!ATTLIST root value (one|one) #IMPLIED>")]
    [TestCase("<!ATTLIST root value NOTATION (png|png) #IMPLIED>")]
    public void RejectsDuplicateEnumerationAndNotationTokens(string declaration)
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        FormatException failure = Assert.Throws<FormatException>(() => parser.LoadDtdSubsets(declaration, null, null, null))!;

        Assert.That(failure.Message, Does.Contain("duplicate tokens"));
    }

    [TestCase("<!ATTLIST root value CDATA '<'>")]
    [TestCase("<!ATTLIST root value CDATA '&broken'>")]
    [TestCase("<!ATTLIST root value CDATA '&;'>")]
    [TestCase("<!ATTLIST root value CDATA '&#0;'>")]
    public void RejectsMalformedDefaultAttributeValues(string declaration)
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        Assert.Throws<FormatException>(() => parser.LoadDtdSubsets(declaration, null, null, null));
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        return policy;
    }

    private static XmlParserOptions ParserOptions()
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.None;
        return options;
    }

    private sealed class AttributeDeclarationResolver : IXmlResourceResolver
    {
        public string Text = "";

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = Text;
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = kind;
            resource.Access = XmlResourceAccess.Allowed;
            resource.Input = input;
            resource.AbsoluteUri = "urn:root.dtd";
            return resource;
        }
    }
}
