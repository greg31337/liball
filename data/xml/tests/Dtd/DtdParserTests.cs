using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class DtdParserTests
{
    [Test]
    public void LoadsInternalSubsetAndExpandsNestedParameterEntitiesDepthFirst()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);
        parser.RegisterInternalDtdParameterEntity("inner", "<!ELEMENT child EMPTY>", "urn:inner");
        parser.RegisterInternalDtdParameterEntity("outer", "%inner;\n<!ATTLIST child id ID #IMPLIED>", "urn:outer");

        DtdSubset subset = parser.LoadDtdSubsets("%outer;", null, null, "urn:document");

        Assert.That(subset.HasInternalSubset, Is.True);
        Assert.That(subset.ExternalSubsetLoaded, Is.False);
        Assert.That(PhysicalText(subset), Is.EqualTo("<!ELEMENT child EMPTY>\n<!ATTLIST child id ID #IMPLIED>"));
        Assert.That(subset.Tokens.Count(entry => entry.Synthetic), Is.EqualTo(4));
        Assert.That(subset.Tokens.Any(entry => entry.ParameterEntityName == "outer" && entry.EntityDepth == 1), Is.True);
        Assert.That(subset.Tokens.Any(entry => entry.ParameterEntityName == "inner" && entry.EntityDepth == 2), Is.True);
        Assert.That(subset.Tokens.Any(entry => entry.SourceUri == "urn:inner"), Is.True);
    }

    [Test]
    public void AddsParameterEntityBoundarySpacesInsideExternalDeclarations()
    {
        RecordingResolver resolver = new RecordingResolver();
        resolver.AddStringResource("root.dtd", XmlResourceKind.ExternalSubset, "<!ELEMENT root (%content;)>", "https://example.test/root.dtd");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());
        parser.RegisterInternalDtdParameterEntity("content", "child|other", "urn:content");

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", "https://example.test/document.xml");

        DtdTokenEntry[] boundaries = subset.Tokens.Where(entry => entry.Synthetic).ToArray();
        Assert.That(boundaries, Has.Length.EqualTo(2));
        Assert.That(boundaries.All(entry => entry.Token.Kind == DtdTokenKind.Whitespace && entry.Token.Value == " "), Is.True);
        Assert.That(subset.Tokens.Where(entry => entry.ParameterEntityName == "content" && !entry.Synthetic).Select(entry => entry.Token.Lexeme), Is.EqualTo(new string[] { "child", "|", "other" }));
    }

    [Test]
    public void ForbidsParameterEntityReferencesInsideInternalSubsetDeclarations()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);
        parser.RegisterInternalDtdParameterEntity("content", "child", null);

        FormatException failure = Assert.Throws<FormatException>(() => parser.LoadDtdSubsets("<!ELEMENT root (%content;)>", null, null, null))!;

        Assert.That(failure.Message, Does.Contain("internal subset"));

        DtdParser entityValueParser = new DtdParser(XmlVersion.Xml10, null, null);
        Assert.Throws<FormatException>(() => entityValueParser.LoadDtdSubsets("<!ENTITY % outer '%inner;'>", null, null, null));
    }

    [Test]
    public void DoesNotMistakeAParameterEntityDeclarationMarkerForAReference()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        DtdSubset subset = parser.LoadDtdSubsets("<!ENTITY % local 'value'>", null, null, null);

        Assert.That(subset.Tokens.Any(entry => entry.Token.Kind == DtdTokenKind.Symbol && entry.Token.Value == "%"), Is.True);
        Assert.That(subset.Tokens.Any(entry => entry.Token.Kind == DtdTokenKind.ParameterEntityReference), Is.False);
    }

    [Test]
    public void RequiresMarkupDeclarationsToBeginAndEndInOneEntity()
    {
        DtdParser openingParser = new DtdParser(XmlVersion.Xml10, null, null);
        openingParser.RegisterInternalDtdParameterEntity("opening", "<!ELEMENT root EMPTY", null);
        Assert.Throws<FormatException>(() => openingParser.LoadDtdSubsets("%opening;", null, null, null));

        RecordingResolver resolver = new RecordingResolver();
        resolver.AddStringResource("root.dtd", XmlResourceKind.ExternalSubset, "<!ELEMENT root (%closing;)>", "urn:root");
        DtdParser closingParser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());
        closingParser.RegisterInternalDtdParameterEntity("closing", ">", null);
        Assert.Throws<FormatException>(() => closingParser.LoadDtdSubsets(null, null, "root.dtd", null));
    }

    [Test]
    public void DecodesExternalSubsetAndRemovesItsLeadingTextDeclaration()
    {
        string externalText = "<?xml version='1.0' encoding='UTF-8'?>\r\n<!ELEMENT root EMPTY>";
        RecordingResolver resolver = new RecordingResolver();
        resolver.AddByteResource("root.dtd", XmlResourceKind.ExternalSubset, Encoding.UTF8.GetBytes(externalText), "https://example.test/dtd/root.dtd");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        DtdSubset subset = parser.LoadDtdSubsets(null, "-//Example//DTD Root//EN", "root.dtd", "https://example.test/document.xml");

        Assert.That(subset.ExternalSubsetLoaded, Is.True);
        Assert.That(subset.ExternalSubsetUri, Is.EqualTo("https://example.test/dtd/root.dtd"));
        Assert.That(subset.ExternalSubsetTextDeclaration, Is.Not.Null);
        Assert.That(subset.ExternalSubsetTextDeclaration!.EncodingName, Is.EqualTo("UTF-8"));
        Assert.That(PhysicalText(subset), Is.EqualTo("\n<!ELEMENT root EMPTY>"));
        Assert.That(subset.Tokens.All(entry => entry.SourceKind == DtdSourceKind.ExternalSubset), Is.True);
        Assert.That(resolver.Kinds, Is.EqualTo(new XmlResourceKind[] { XmlResourceKind.ExternalSubset }));
        Assert.That(resolver.PublicIdentifiers, Is.EqualTo(new string?[] { "-//Example//DTD Root//EN" }));
        Assert.That(resolver.SystemIdentifiers, Is.EqualTo(new string[] { "root.dtd" }));
        Assert.That(resolver.BaseUris, Is.EqualTo(new string?[] { "https://example.test/document.xml" }));
    }

    [Test]
    public void LoadsAndExpandsAnExternalParameterEntityWithItsOwnTextDeclaration()
    {
        RecordingResolver resolver = new RecordingResolver();
        resolver.AddStringResource("root.dtd", XmlResourceKind.ExternalSubset, "<!ELEMENT root (%model;)>", "https://example.test/root.dtd");
        resolver.AddByteResource("model.ent", XmlResourceKind.ParameterEntity, Encoding.UTF8.GetBytes("<?xml encoding='UTF-8'?>child|other"), "https://example.test/model.ent");
        DtdParser parser = new DtdParser(XmlVersion.Xml11, resolver, AllowedPolicy());
        parser.RegisterExternalDtdParameterEntity("model", null, "model.ent", "https://example.test/root.dtd");

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", "https://example.test/document.xml");

        Assert.That(resolver.Kinds, Is.EqualTo(new XmlResourceKind[] { XmlResourceKind.ExternalSubset, XmlResourceKind.ParameterEntity }));
        Assert.That(subset.Tokens.Where(entry => entry.SourceKind == DtdSourceKind.ExternalParameterEntity && !entry.Synthetic).Select(entry => entry.Token.Lexeme), Is.EqualTo(new string[] { "child", "|", "other" }));
        Assert.That(subset.Tokens.Where(entry => entry.SourceKind == DtdSourceKind.ExternalParameterEntity && !entry.Synthetic).All(entry => entry.SourceUri == "https://example.test/model.ent"), Is.True);
        Assert.That(subset.Tokens.Any(entry => entry.ParameterEntityName == "model" && entry.EntityDepth == 1), Is.True);
    }

    [Test]
    public void RejectsTextDeclarationsOutsideTheStartOfExternalEntities()
    {
        DtdParser internalParser = new DtdParser(XmlVersion.Xml10, null, null);
        Assert.Throws<FormatException>(() => internalParser.LoadDtdSubsets("<?xml encoding='UTF-8'?>", null, null, null));

        RecordingResolver resolver = new RecordingResolver();
        resolver.AddStringResource("misplaced.dtd", XmlResourceKind.ExternalSubset, "<!ELEMENT root EMPTY><?xml encoding='UTF-8'?>", "urn:misplaced");
        DtdParser externalParser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());
        Assert.Throws<FormatException>(() => externalParser.LoadDtdSubsets(null, null, "misplaced.dtd", null));
    }

    [Test]
    public void RejectsInvalidOrIncompatibleExternalTextDeclarations()
    {
        RecordingResolver missingEncodingResolver = new RecordingResolver();
        missingEncodingResolver.AddStringResource("missing.dtd", XmlResourceKind.ExternalSubset, "<?xml version='1.0'?><!ELEMENT root EMPTY>", "urn:missing");
        DtdParser missingEncodingParser = new DtdParser(XmlVersion.Xml10, missingEncodingResolver, AllowedPolicy());
        Assert.Throws<FormatException>(() => missingEncodingParser.LoadDtdSubsets(null, null, "missing.dtd", null));

        RecordingResolver incompatibleResolver = new RecordingResolver();
        incompatibleResolver.AddStringResource("newer.dtd", XmlResourceKind.ExternalSubset, "<?xml version='1.1' encoding='UTF-8'?><!ELEMENT root EMPTY>", "urn:newer");
        DtdParser incompatibleParser = new DtdParser(XmlVersion.Xml10, incompatibleResolver, AllowedPolicy());
        Assert.Throws<FormatException>(() => incompatibleParser.LoadDtdSubsets(null, null, "newer.dtd", null));
    }

    [Test]
    public void SkipsExternalSubsetWhenNoExplicitAllowedResolutionIsAvailable()
    {
        RecordingResolver resolver = new RecordingResolver();
        resolver.AddStringResource("root.dtd", XmlResourceKind.ExternalSubset, "<!ELEMENT root EMPTY>", "urn:root");
        XmlResolverPolicy denied = AllowedPolicy();
        denied.Access = XmlResourceAccess.Denied;
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, denied);

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", null);

        Assert.That(subset.ExternalSubsetLoaded, Is.False);
        Assert.That(subset.Tokens, Is.Empty);
        Assert.That(resolver.Kinds, Is.Empty);
    }

    [Test]
    public void ValidatesRegistrationResolutionAndSingleUse()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);
        Assert.Throws<ArgumentException>(() => parser.RegisterInternalDtdParameterEntity("not a name", "x", null));
        Assert.Throws<ArgumentNullException>(() => parser.RegisterInternalDtdParameterEntity("valid", null!, null));
        Assert.Throws<ArgumentNullException>(() => parser.RegisterExternalDtdParameterEntity("valid", null, null!, null));

        parser.RegisterInternalDtdParameterEntity("choice", "<!ELEMENT first EMPTY>", null);
        parser.RegisterInternalDtdParameterEntity("choice", "<!ELEMENT second EMPTY>", null);
        DtdSubset subset = parser.LoadDtdSubsets("%choice;", null, null, null);
        Assert.That(PhysicalText(subset), Is.EqualTo("<!ELEMENT first EMPTY>"));
        Assert.Throws<InvalidOperationException>(() => parser.LoadDtdSubsets(null, null, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DtdParser((XmlVersion)99, null, null));
    }

    [Test]
    public void RejectsUndeclaredRecursiveAndNonDeclarationReplacementText()
    {
        DtdParser undeclared = new DtdParser(XmlVersion.Xml10, null, null);
        Assert.Throws<FormatException>(() => undeclared.LoadDtdSubsets("%missing;", null, null, null));

        DtdParser recursive = new DtdParser(XmlVersion.Xml10, null, null);
        recursive.RegisterInternalDtdParameterEntity("loop", "%loop;", null);
        Assert.Throws<FormatException>(() => recursive.LoadDtdSubsets("%loop;", null, null, null));

        DtdParser fragment = new DtdParser(XmlVersion.Xml10, null, null);
        fragment.RegisterInternalDtdParameterEntity("fragment", "orphan", null);
        Assert.Throws<FormatException>(() => fragment.LoadDtdSubsets("%fragment;", null, null, null));
    }

    private static string PhysicalText(DtdSubset subset)
    {
        return string.Concat(subset.Tokens.Where(entry => !entry.Synthetic).Select(entry => entry.Token.Lexeme));
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        return policy;
    }

    private sealed class RecordingResolver : IXmlResourceResolver
    {
        public Dictionary<string, XmlResolvedResource> Resources = new Dictionary<string, XmlResolvedResource>(StringComparer.Ordinal);
        public List<XmlResourceKind> Kinds = new List<XmlResourceKind>();
        public List<string?> PublicIdentifiers = new List<string?>();
        public List<string> SystemIdentifiers = new List<string>();
        public List<string?> BaseUris = new List<string?>();

        public void AddStringResource(string systemIdentifier, XmlResourceKind kind, string text, string absoluteUri)
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

        public void AddByteResource(string systemIdentifier, XmlResourceKind kind, byte[] bytes, string absoluteUri)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.ByteMemory;
            input.Bytes = bytes;
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
            PublicIdentifiers.Add(publicIdentifier);
            SystemIdentifiers.Add(systemIdentifier == null ? "" : systemIdentifier);
            BaseUris.Add(baseUri);
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
