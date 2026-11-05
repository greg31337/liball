using xml;

namespace xml.Tests;

[TestFixture]
public class DtdDeclarationTests
{
    [Test]
    public void RetainsInternalExternalUnparsedAndNotationDeclarations()
    {
        string subsetText = "<!ENTITY internal '&#65;&kept;'>" +
            "<!ENTITY external SYSTEM 'parsed.ent'>" +
            "<!ENTITY image PUBLIC '-//Example//Image//EN' 'image.bin' NDATA png>" +
            "<!ENTITY % parameter 'choice'>" +
            "<!ENTITY % externalParameter PUBLIC '-//Example//Parameter//EN' 'parameter.ent'>" +
            "<!NOTATION png SYSTEM 'image/png'>" +
            "<!NOTATION text PUBLIC 'text/plain'>";
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        DtdSubset subset = parser.LoadDtdSubsets(subsetText, null, null, "https://example.test/document.xml");

        Assert.That(subset.EntityDeclarations, Has.Length.EqualTo(5));
        XmlEntityDeclaration internalEntity = subset.EntityDeclarations[0];
        Assert.That(internalEntity.Name, Is.EqualTo("internal"));
        Assert.That(internalEntity.ReplacementText, Is.EqualTo("A&kept;"));
        Assert.That(internalEntity.IsExternal, Is.False);
        Assert.That(internalEntity.IsParsed, Is.True);
        Assert.That(internalEntity.IsParameter, Is.False);
        Assert.That(internalEntity.BaseUri, Is.EqualTo("https://example.test/document.xml"));

        XmlEntityDeclaration externalEntity = subset.EntityDeclarations[1];
        Assert.That(externalEntity.SystemIdentifier, Is.EqualTo("parsed.ent"));
        Assert.That(externalEntity.ReplacementText, Is.Null);
        Assert.That(externalEntity.IsExternal, Is.True);
        Assert.That(externalEntity.IsParsed, Is.True);

        XmlEntityDeclaration unparsedEntity = subset.EntityDeclarations[2];
        Assert.That(unparsedEntity.PublicIdentifier, Is.EqualTo("-//Example//Image//EN"));
        Assert.That(unparsedEntity.SystemIdentifier, Is.EqualTo("image.bin"));
        Assert.That(unparsedEntity.NotationName, Is.EqualTo("png"));
        Assert.That(unparsedEntity.IsParsed, Is.False);

        Assert.That(subset.EntityDeclarations[3].IsParameter, Is.True);
        Assert.That(subset.EntityDeclarations[3].ReplacementText, Is.EqualTo("choice"));
        Assert.That(subset.EntityDeclarations[4].IsParameter, Is.True);
        Assert.That(subset.EntityDeclarations[4].IsExternal, Is.True);
        Assert.That(subset.EntityDeclarations[4].PublicIdentifier, Is.EqualTo("-//Example//Parameter//EN"));

        Assert.That(subset.NotationDeclarations, Has.Length.EqualTo(2));
        Assert.That(subset.NotationDeclarations[0].Name, Is.EqualTo("png"));
        Assert.That(subset.NotationDeclarations[0].SystemIdentifier, Is.EqualTo("image/png"));
        Assert.That(subset.NotationDeclarations[0].BaseUri, Is.EqualTo("https://example.test/document.xml"));
        Assert.That(subset.NotationDeclarations[1].PublicIdentifier, Is.EqualTo("text/plain"));
        Assert.That(subset.NotationDeclarations[1].SystemIdentifier, Is.Null);
    }

    [Test]
    public void DeclaredParameterEntitiesAreAvailableToFollowingDeclarations()
    {
        string subsetText = "<!ENTITY % declarations '<!ENTITY generated \"ready\">'>%declarations;";
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        DtdSubset subset = parser.LoadDtdSubsets(subsetText, null, null, "urn:document");

        Assert.That(subset.EntityDeclarations, Has.Length.EqualTo(2));
        Assert.That(subset.EntityDeclarations[0].Name, Is.EqualTo("declarations"));
        Assert.That(subset.EntityDeclarations[1].Name, Is.EqualTo("generated"));
        Assert.That(subset.EntityDeclarations[1].ReplacementText, Is.EqualTo("ready"));
        Assert.That(subset.EntityDeclarations[1].BaseUri, Is.EqualTo("urn:document"));
    }

    [Test]
    public void AppliesFirstDeclarationPrecedenceInSeparateEntityNamespaces()
    {
        string subsetText = "<!ENTITY same 'general-first'>" +
            "<!ENTITY same 'general-second'>" +
            "<!ENTITY % same 'parameter-first'>" +
            "<!ENTITY % same 'parameter-second'>";
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        DtdSubset subset = parser.LoadDtdSubsets(subsetText, null, null, null);

        Assert.That(subset.EntityDeclarations, Has.Length.EqualTo(2));
        Assert.That(subset.EntityDeclarations[0].IsParameter, Is.False);
        Assert.That(subset.EntityDeclarations[0].ReplacementText, Is.EqualTo("general-first"));
        Assert.That(subset.EntityDeclarations[1].IsParameter, Is.True);
        Assert.That(subset.EntityDeclarations[1].ReplacementText, Is.EqualTo("parameter-first"));
    }

    [Test]
    public void UsesThePhysicalDeclarationEntityAsTheBaseUri()
    {
        DeclarationResolver resolver = new DeclarationResolver();
        resolver.AddResource("root.dtd", XmlResourceKind.ExternalSubset, "<!ENTITY % declarations SYSTEM 'declarations.ent'>%declarations;", "https://example.test/dtd/root.dtd");
        resolver.AddResource("declarations.ent", XmlResourceKind.ParameterEntity, "<!ENTITY nested SYSTEM 'nested.xml'>", "https://cdn.example.test/entities/declarations.ent");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", "https://example.test/document.xml");

        Assert.That(subset.EntityDeclarations, Has.Length.EqualTo(2));
        Assert.That(subset.EntityDeclarations[0].Name, Is.EqualTo("declarations"));
        Assert.That(subset.EntityDeclarations[0].BaseUri, Is.EqualTo("https://example.test/dtd/root.dtd"));
        Assert.That(subset.EntityDeclarations[1].Name, Is.EqualTo("nested"));
        Assert.That(subset.EntityDeclarations[1].BaseUri, Is.EqualTo("https://cdn.example.test/entities/declarations.ent"));
        Assert.That(resolver.ParameterEntityBaseUri, Is.EqualTo("https://example.test/dtd/root.dtd"));
    }

    [Test]
    public void PublicParserRetainsDeclarationsAndExpandsAnInternalGeneralEntity()
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = "<!DOCTYPE root [<!ENTITY greeting 'hello'><!NOTATION png SYSTEM 'image/png'>]><root>&greeting;</root>";
        input.BaseUri = "urn:document";
        XmlParserOptions options = ParserOptions();

        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, null, null, null);

        Assert.That(document.DocumentType!.EntityDeclarations, Has.Length.EqualTo(1));
        Assert.That(document.DocumentType.EntityDeclarations![0].Name, Is.EqualTo("greeting"));
        Assert.That(document.DocumentType.NotationDeclarations, Has.Length.EqualTo(1));
        Assert.That(document.DocumentType.NotationDeclarations![0].Name, Is.EqualTo("png"));
        Assert.That(document.DocumentElement!.Children, Has.Length.EqualTo(1));
        Assert.That(document.DocumentElement.Children![0].Value, Is.EqualTo("hello"));
    }

    [TestCase("<!ENTITY missing>")]
    [TestCase("<!ENTITY name PUBLIC 'public-only'>")]
    [TestCase("<!ENTITY name SYSTEM 'bad#fragment'>")]
    [TestCase("<!ENTITY % name SYSTEM 'parameter.ent' NDATA note>")]
    [TestCase("<!NOTATION note>")]
    [TestCase("<!NOTATION note SYSTEM>")]
    [TestCase("<!NOTATION note PUBLIC 'bad['>")]
    public void RejectsMalformedEntityAndNotationDeclarations(string subsetText)
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        Assert.Throws<FormatException>(() => parser.LoadDtdSubsets(subsetText, null, null, null));
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

    private sealed class DeclarationResolver : IXmlResourceResolver
    {
        public Dictionary<string, XmlResolvedResource> Resources = new Dictionary<string, XmlResolvedResource>(StringComparer.Ordinal);
        public string? ParameterEntityBaseUri;

        public void AddResource(string systemIdentifier, XmlResourceKind kind, string text, string absoluteUri)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = text;
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = kind;
            resource.Access = XmlResourceAccess.Allowed;
            resource.Input = input;
            resource.AbsoluteUri = absoluteUri;
            Resources.Add(systemIdentifier, resource);
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            if (kind == XmlResourceKind.ParameterEntity)
            {
                ParameterEntityBaseUri = baseUri;
            }

            if (systemIdentifier != null && Resources.ContainsKey(systemIdentifier))
            {
                return Resources[systemIdentifier];
            }

            XmlResolvedResource denied = new XmlResolvedResource();
            denied.Kind = kind;
            denied.Access = XmlResourceAccess.Denied;
            return denied;
        }
    }
}
