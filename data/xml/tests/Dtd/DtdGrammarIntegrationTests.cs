using xml;

namespace xml.Tests;

[TestFixture]
public class DtdGrammarIntegrationTests
{
    [Test]
    public void ParsesEveryDeclarationFormWithCommentsPisAndParameterEntities()
    {
        string subsetText = "<!--before--><?grammar ready?>" +
            "<!ELEMENT root (child)>" +
            "<!ATTLIST root mode (fast|safe) 'safe'>" +
            "<!ENTITY value 'text'>" +
            "<!ENTITY image SYSTEM 'image.bin' NDATA png>" +
            "<!ENTITY % childDeclaration '<!ELEMENT child EMPTY>'>" +
            "%childDeclaration;" +
            "<!NOTATION png PUBLIC '-//Example//Notation PNG//EN' 'image/png'>";
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        DtdSubset subset = parser.LoadDtdSubsets(subsetText, null, null, "urn:document");

        Assert.That(subset.ElementDeclarations.Select(item => item.Name!.QualifiedName), Is.EqualTo(new string[] { "root", "child" }));
        Assert.That(subset.AttributeDeclarations, Has.Length.EqualTo(1));
        Assert.That(subset.AttributeDeclarations[0].AllowedValues, Is.EqualTo(new string[] { "fast", "safe" }));
        Assert.That(subset.EntityDeclarations.Select(item => item.Name), Is.EqualTo(new string[] { "value", "image", "childDeclaration" }));
        Assert.That(subset.EntityDeclarations[1].IsParsed, Is.False);
        Assert.That(subset.EntityDeclarations[2].IsParameter, Is.True);
        Assert.That(subset.NotationDeclarations, Has.Length.EqualTo(1));
        Assert.That(subset.NotationDeclarations[0].PublicIdentifier, Is.EqualTo("-//Example//Notation PNG//EN"));
        Assert.That(subset.NotationDeclarations[0].SystemIdentifier, Is.EqualTo("image/png"));
        Assert.That(subset.Tokens.Count(item => item.Token.Kind == DtdTokenKind.Comment), Is.EqualTo(1));
        Assert.That(subset.Tokens.Count(item => item.Token.Kind == DtdTokenKind.ProcessingInstruction), Is.EqualTo(1));
    }

    [Test]
    public void LoadsNestedExternalParameterEntitiesWithTextDeclarations()
    {
        GrammarResolver resolver = new GrammarResolver();
        resolver.AddResource("root.dtd", XmlResourceKind.ExternalSubset, "<?xml encoding='UTF-8'?><!ENTITY % module SYSTEM 'module.ent'>%module;", "urn:root.dtd");
        resolver.AddResource("module.ent", XmlResourceKind.ParameterEntity, "<?xml version='1.0' encoding='UTF-8'?><!ENTITY % child '<!ELEMENT child EMPTY>'><!ELEMENT root (child)>%child;", "urn:module.ent");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", "urn:document");

        Assert.That(subset.ExternalSubsetLoaded, Is.True);
        Assert.That(subset.ExternalSubsetTextDeclaration, Is.Not.Null);
        Assert.That(subset.ElementDeclarations.Select(item => item.Name!.QualifiedName), Is.EqualTo(new string[] { "root", "child" }));
        Assert.That(subset.Tokens.Any(item => item.SourceKind == DtdSourceKind.ExternalParameterEntity && item.ParameterEntityName == "module"), Is.True);
        Assert.That(resolver.Kinds, Is.EqualTo(new XmlResourceKind[] { XmlResourceKind.ExternalSubset, XmlResourceKind.ParameterEntity }));
    }

    [Test]
    public void ProcessesConditionalSectionsAroundEveryDeclarationKind()
    {
        string externalText = "<![INCLUDE[" +
            "<!ELEMENT root EMPTY>" +
            "<!ATTLIST root mode CDATA #IMPLIED>" +
            "<!ENTITY value 'included'>" +
            "<!NOTATION note SYSTEM 'included'>" +
            "]]><![IGNORE[" +
            "<!ELEMENT ignored ANY>" +
            "<!ATTLIST ignored broken CDATA #REQUIRED>" +
            "<!ENTITY ignoredEntity 'ignored'>" +
            "<!NOTATION ignoredNotation SYSTEM 'ignored'>" +
            "]]>";
        GrammarResolver resolver = new GrammarResolver();
        resolver.AddResource("root.dtd", XmlResourceKind.ExternalSubset, externalText, "urn:root.dtd");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", null);

        Assert.That(subset.ElementDeclarations.Select(item => item.Name!.QualifiedName), Is.EqualTo(new string[] { "root" }));
        Assert.That(subset.AttributeDeclarations.Select(item => item.Name!.QualifiedName), Is.EqualTo(new string[] { "mode" }));
        Assert.That(subset.EntityDeclarations.Select(item => item.Name), Is.EqualTo(new string[] { "value" }));
        Assert.That(subset.NotationDeclarations.Select(item => item.Name), Is.EqualTo(new string[] { "note" }));
    }

    [Test]
    public void GivesInternalSubsetDeclarationsPrecedenceAcrossAllDeclarationKinds()
    {
        string internalText = "<!ELEMENT root EMPTY>" +
            "<!ATTLIST root source CDATA 'internal'>" +
            "<!ENTITY value 'internal'>" +
            "<!NOTATION format SYSTEM 'internal'>";
        string externalText = "<!ELEMENT root ANY>" +
            "<!ATTLIST root source CDATA 'external'>" +
            "<!ENTITY value 'external'>" +
            "<!NOTATION format SYSTEM 'external'>" +
            "<!ELEMENT externalOnly EMPTY>";
        GrammarResolver resolver = new GrammarResolver();
        resolver.AddResource("root.dtd", XmlResourceKind.ExternalSubset, externalText, "urn:root.dtd");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        DtdSubset subset = parser.LoadDtdSubsets(internalText, null, "root.dtd", "urn:document");

        Assert.That(subset.ElementDeclarations, Has.Length.EqualTo(2));
        Assert.That(subset.ElementDeclarations[0].ContentModel, Is.EqualTo("EMPTY"));
        Assert.That(subset.ElementDeclarations[1].Name!.QualifiedName, Is.EqualTo("externalOnly"));
        Assert.That(subset.AttributeDeclarations, Has.Length.EqualTo(1));
        Assert.That(subset.AttributeDeclarations[0].DefaultValue, Is.EqualTo("internal"));
        Assert.That(subset.EntityDeclarations, Has.Length.EqualTo(1));
        Assert.That(subset.EntityDeclarations[0].ReplacementText, Is.EqualTo("internal"));
        Assert.That(subset.NotationDeclarations, Has.Length.EqualTo(1));
        Assert.That(subset.NotationDeclarations[0].SystemIdentifier, Is.EqualTo("internal"));
    }

    [Test]
    public void PublishesExternalGrammarThroughThePublicParser()
    {
        GrammarResolver resolver = new GrammarResolver();
        resolver.AddResource("root.dtd", XmlResourceKind.ExternalSubset, "<?xml encoding='UTF-8'?><!ELEMENT root (#PCDATA)><!ATTLIST root mode CDATA 'ready'><!ENTITY value 'text'><!NOTATION note PUBLIC 'note'>", "urn:root.dtd");
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = "<!DOCTYPE root SYSTEM 'root.dtd'><root>&value;</root>";
        input.BaseUri = "urn:document";
        XmlParserOptions options = ParserOptions();
        options.ResolverPolicy = AllowedPolicy();

        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, resolver, null, null);

        Assert.That(document.DocumentType!.ElementDeclarations, Has.Length.EqualTo(1));
        Assert.That(document.DocumentType.AttributeDeclarations, Has.Length.EqualTo(1));
        Assert.That(document.DocumentType.EntityDeclarations, Has.Length.EqualTo(1));
        Assert.That(document.DocumentType.NotationDeclarations, Has.Length.EqualTo(1));
        Assert.That(document.DocumentElement!.Attributes![0].Value, Is.EqualTo("ready"));
        Assert.That(document.DocumentElement.Children![0].Value, Is.EqualTo("text"));
    }

    [TestCase("<!ELEMENT root EMPTY")]
    [TestCase("<!BOGUS root EMPTY>")]
    [TestCase("orphan")]
    [TestCase(">")]
    [TestCase("<!NOTATION note PUBLIC 'id''system'>")]
    [TestCase("<!ENTITY name SYSTEM 'entity'NDATA note>")]
    [TestCase("<!ATTLIST root value CDATA #IMPLIED extra>")]
    public void RejectsMalformedDeclarationStructure(string subsetText)
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        Assert.Throws<FormatException>(() => parser.LoadDtdSubsets(subsetText, null, null, null));
    }

    [TestCase("<?xml version='1.0'?><!ELEMENT root EMPTY>")]
    [TestCase(" <?xml encoding='UTF-8'?><!ELEMENT root EMPTY>")]
    [TestCase("<?xml encoding='UTF-8' standalone='yes'?><!ELEMENT root EMPTY>")]
    public void RejectsMalformedOrMisplacedExternalTextDeclarations(string externalText)
    {
        GrammarResolver resolver = new GrammarResolver();
        resolver.AddResource("bad.dtd", XmlResourceKind.ExternalSubset, externalText, "urn:bad.dtd");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        Assert.Throws<FormatException>(() => parser.LoadDtdSubsets(null, null, "bad.dtd", null));
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

    private static XmlResolverPolicy AllowedPolicy()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        return policy;
    }

    private sealed class GrammarResolver : IXmlResourceResolver
    {
        public Dictionary<string, XmlResolvedResource> Resources = new Dictionary<string, XmlResolvedResource>(StringComparer.Ordinal);
        public List<XmlResourceKind> Kinds = new List<XmlResourceKind>();

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
            Kinds.Add(kind);
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
