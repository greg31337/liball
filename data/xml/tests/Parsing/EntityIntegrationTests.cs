using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class EntityIntegrationTests
{
    [Test]
    public void ExpandsInternalExternalAndParameterDeclaredEntitiesWhileRetainingUnparsedEntities()
    {
        EntityIntegrationResolver resolver = new EntityIntegrationResolver();
        resolver.AddStringResource(XmlResourceKind.GeneralEntity, "external.ent", "B", "urn:external.ent");
        string subset = "<!ENTITY internal 'A'>" +
            "<!ENTITY external SYSTEM 'external.ent'>" +
            "<!ENTITY % generated \"<!ENTITY generated 'C'>\">" +
            "%generated;" +
            "<!NOTATION png SYSTEM 'image/png'>" +
            "<!ENTITY image SYSTEM 'image.png' NDATA png>";

        XmlDocument document = Parse("<!DOCTYPE root [" + subset + "]><root>&internal;&external;&generated;</root>", XmlEntityHandling.Expand, false, resolver, AllowedPolicy(), CreateLimits());

        Assert.That(string.Concat(document.DocumentElement!.Children!.Select(item => item.Value)), Is.EqualTo("ABC"));
        Assert.That(document.DocumentType!.EntityDeclarations, Has.Length.EqualTo(5));
        Assert.That(document.DocumentType.EntityDeclarations!.Any(item => item.IsParameter), Is.True);
        Assert.That(document.DocumentType.EntityDeclarations.Any(item => !item.IsParsed && item.NotationName == "png"), Is.True);
    }

    [TestCase(XmlEntityHandling.PreserveReferences, false)]
    [TestCase(XmlEntityHandling.Expand, true)]
    public void PreservesNestedEntityBoundariesForEitherPreservationControl(XmlEntityHandling handling, bool preserveReferences)
    {
        string subset = "<!ENTITY outer 'A&inner;C'><!ENTITY inner 'B'>";

        XmlDocument document = Parse("<!DOCTYPE root [" + subset + "]><root>&outer;</root>", handling, preserveReferences, null, null, CreateLimits());

        XmlNode outer = document.DocumentElement!.Children![0];
        Assert.That(outer.Kind, Is.EqualTo(XmlNodeKind.EntityReference));
        Assert.That(outer.Name!.QualifiedName, Is.EqualTo("outer"));
        Assert.That(outer.Children, Has.Length.EqualTo(3));
        Assert.That(outer.Children![0].Value, Is.EqualTo("A"));
        XmlNode inner = outer.Children[1];
        Assert.That(inner.Kind, Is.EqualTo(XmlNodeKind.EntityReference));
        Assert.That(inner.Name!.QualifiedName, Is.EqualTo("inner"));
        Assert.That(inner.Children![0].Value, Is.EqualTo("B"));
        Assert.That(outer.Children[2].Value, Is.EqualTo("C"));
    }

    [Test]
    public void DecodesExternalGeneralEntityTextDeclarationsAndUsesTheDeclarationBase()
    {
        EntityIntegrationResolver resolver = new EntityIntegrationResolver();
        resolver.AddStringResource(XmlResourceKind.ExternalSubset, "root.dtd", "<?xml encoding='UTF-8'?><!ENTITY message SYSTEM 'message.ent'>", "https://example.test/dtd/root.dtd");
        byte[] entityBytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("<?xml encoding='UTF-16'?>encoded")).ToArray();
        resolver.AddByteResource(XmlResourceKind.GeneralEntity, "message.ent", entityBytes, "https://example.test/entities/message.ent");

        XmlDocument document = Parse("<!DOCTYPE root SYSTEM 'root.dtd'><root>&message;</root>", XmlEntityHandling.Expand, false, resolver, AllowedPolicy(), CreateLimits());

        Assert.That(document.DocumentElement!.Children![0].Value, Is.EqualTo("encoded"));
        Assert.That(document.DocumentType!.EntityDeclarations![0].BaseUri, Is.EqualTo("https://example.test/dtd/root.dtd"));
        int generalIndex = resolver.Kinds.IndexOf(XmlResourceKind.GeneralEntity);
        Assert.That(generalIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(resolver.BaseUris[generalIndex], Is.EqualTo("https://example.test/dtd/root.dtd"));
    }

    [Test]
    public void ExpandsMarkupFromExternalGeneralEntitiesIntoNodes()
    {
        EntityIntegrationResolver resolver = new EntityIntegrationResolver();
        resolver.AddStringResource(XmlResourceKind.GeneralEntity, "markup.ent", "<bar>baz</bar>", "urn:markup.ent");

        XmlDocument document = Parse("<!DOCTYPE root [<!ENTITY markup SYSTEM 'markup.ent'>]><root>&markup;</root>", XmlEntityHandling.Expand, false, resolver, AllowedPolicy(), CreateLimits());

        XmlNode bar = document.DocumentElement!.Children![0];
        Assert.Multiple(() =>
        {
            Assert.That(bar.Kind, Is.EqualTo(XmlNodeKind.Element));
            Assert.That(bar.Name!.LocalName, Is.EqualTo("bar"));
            Assert.That(bar.Children![0].Value, Is.EqualTo("baz"));
        });
    }

    [Test]
    public void RejectsReferencesToUnparsedEntities()
    {
        string subset = "<!NOTATION png SYSTEM 'image/png'><!ENTITY image SYSTEM 'image.png' NDATA png>";

        XmlWellFormednessException failure = Assert.Throws<XmlWellFormednessException>(() => Parse("<!DOCTYPE root [" + subset + "]><root>&image;</root>", XmlEntityHandling.Expand, false, null, null, CreateLimits()))!;

        Assert.That(failure.Message, Does.Contain("unparsed entity"));
    }

    [TestCase("direct", "<!ENTITY direct '&direct;'>", "&direct;")]
    [TestCase("indirect", "<!ENTITY first '&second;'><!ENTITY second '&first;'>", "&first;")]
    public void RejectsDirectAndIndirectEntityRecursion(string name, string declarations, string reference)
    {
        XmlWellFormednessException failure = Assert.Throws<XmlWellFormednessException>(() => Parse("<!DOCTYPE root [" + declarations + "]><root>" + reference + "</root>", XmlEntityHandling.Expand, false, null, null, CreateLimits()))!;

        Assert.That(failure.Message, Does.Contain("recursive"), name);
    }

    [Test]
    public void EnforcesStandaloneRulesForExternallyDeclaredEntities()
    {
        EntityIntegrationResolver resolver = new EntityIntegrationResolver();
        resolver.AddStringResource(XmlResourceKind.ExternalSubset, "root.dtd", "<!ELEMENT root (#PCDATA)><!ENTITY message 'external'>", "urn:root.dtd");
        string standalone = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root SYSTEM 'root.dtd'><root>&message;</root>";
        string dependent = "<?xml version='1.0' standalone='no'?><!DOCTYPE root SYSTEM 'root.dtd'><root>&message;</root>";

        Assert.Throws<XmlWellFormednessException>(() => ParseValidated(standalone, resolver));
        Assert.That(ParseValidated(dependent, resolver).DocumentElement!.Children![0].Value, Is.EqualTo("external"));
    }

    [TestCase("depth")]
    [TestCase("count")]
    [TestCase("characters")]
    [TestCase("amplification")]
    public void EnforcesEveryGeneralEntityExpansionLimit(string limit)
    {
        XmlSecurityLimits limits = CreateLimits();
        if (limit == "depth")
        {
            limits.MaximumEntityDepth = 0;
        }
        else if (limit == "count")
        {
            limits.MaximumEntityCount = 0;
        }
        else if (limit == "characters")
        {
            limits.MaximumEntityCharacters = 5;
        }
        else
        {
            limits.MaximumEntityAmplification = 1;
        }

        XmlLimitException failure = Assert.Throws<XmlLimitException>(() => Parse("<!DOCTYPE root [<!ENTITY value '12345678'>]><root>&value;</root>", XmlEntityHandling.Expand, false, null, null, limits))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
    }

    [TestCase("count")]
    [TestCase("depth")]
    [TestCase("bytes")]
    public void EnforcesEveryExternalEntityResourceLimit(string limit)
    {
        EntityIntegrationResolver resolver = new EntityIntegrationResolver();
        resolver.AddStringResource(XmlResourceKind.GeneralEntity, "external.ent", "value", "urn:external.ent");
        XmlSecurityLimits limits = CreateLimits();
        if (limit == "count")
        {
            limits.MaximumExternalResources = 0;
        }
        else if (limit == "depth")
        {
            limits.MaximumResourceDepth = 0;
        }
        else
        {
            limits.MaximumResourceBytes = 4;
        }

        Assert.Throws<XmlLimitException>(() => Parse("<!DOCTYPE root [<!ENTITY external SYSTEM 'external.ent'>]><root>&external;</root>", XmlEntityHandling.Expand, false, resolver, AllowedPolicy(), limits));
    }

    private static XmlDocument ParseValidated(string text, IXmlResourceResolver resolver)
    {
        XmlInput input = StringInput(text);
        XmlParserOptions options = ParserOptions(XmlEntityHandling.Expand, false, CreateLimits());
        options.ValidationMode = XmlValidationMode.Dtd;
        options.ResolverPolicy = AllowedPolicy();
        return new XmlParser().ParseXmlDocument(input, options, resolver, null, null);
    }

    private static XmlDocument Parse(string text, XmlEntityHandling handling, bool preserveReferences, IXmlResourceResolver? resolver, XmlResolverPolicy? policy, XmlSecurityLimits limits)
    {
        XmlParserOptions options = ParserOptions(handling, preserveReferences, limits);
        options.ResolverPolicy = policy;
        return new XmlParser().ParseXmlDocument(StringInput(text), options, resolver, null, null);
    }

    private static XmlInput StringInput(string text)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        input.BaseUri = "urn:document";
        return input;
    }

    private static XmlParserOptions ParserOptions(XmlEntityHandling handling, bool preserveReferences, XmlSecurityLimits limits)
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = handling;
        options.ValidationMode = XmlValidationMode.None;
        options.PreserveEntityReferences = preserveReferences;
        options.SecurityLimits = limits;
        return options;
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        return policy;
    }

    private static XmlSecurityLimits CreateLimits()
    {
        XmlSecurityLimits limits = new XmlSecurityLimits();
        limits.MaximumDepth = 64;
        limits.MaximumNodeCount = 1000;
        limits.MaximumAttributesPerElement = 64;
        limits.MaximumNameLength = 256;
        limits.MaximumTextLength = 10000;
        limits.MaximumTokenLength = 10000;
        limits.MaximumInputCharacters = 10000;
        limits.MaximumInputBytes = 10000;
        limits.MaximumDtdCharacters = 10000;
        limits.MaximumEntityDepth = 32;
        limits.MaximumEntityCount = 1000;
        limits.MaximumEntityCharacters = 10000;
        limits.MaximumEntityAmplification = 100;
        limits.MaximumExternalResources = 16;
        limits.MaximumResourceDepth = 16;
        limits.MaximumResourceBytes = 10000;
        limits.CancellationPollingInterval = 8;
        return limits;
    }

    private sealed class EntityIntegrationResolver : IXmlResourceResolver
    {
        public Dictionary<string, XmlResolvedResource> Resources = new Dictionary<string, XmlResolvedResource>(StringComparer.Ordinal);
        public List<XmlResourceKind> Kinds = new List<XmlResourceKind>();
        public List<string?> BaseUris = new List<string?>();

        public void AddStringResource(XmlResourceKind kind, string systemIdentifier, string text, string absoluteUri)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = text;
            AddEntityResource(kind, systemIdentifier, input, absoluteUri);
        }

        public void AddByteResource(XmlResourceKind kind, string systemIdentifier, byte[] bytes, string absoluteUri)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.ByteMemory;
            input.Bytes = bytes;
            AddEntityResource(kind, systemIdentifier, input, absoluteUri);
        }

        public void AddEntityResource(XmlResourceKind kind, string systemIdentifier, XmlInput input, string absoluteUri)
        {
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = kind;
            resource.Access = XmlResourceAccess.Allowed;
            resource.Input = input;
            resource.AbsoluteUri = absoluteUri;
            Resources.Add(((int)kind).ToString() + ":" + systemIdentifier, resource);
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Kinds.Add(kind);
            BaseUris.Add(baseUri);
            string key = ((int)kind).ToString() + ":" + (systemIdentifier == null ? "" : systemIdentifier);
            if (Resources.ContainsKey(key))
            {
                return Resources[key];
            }

            XmlResolvedResource denied = new XmlResolvedResource();
            denied.Kind = kind;
            denied.Access = XmlResourceAccess.Denied;
            return denied;
        }
    }
}
