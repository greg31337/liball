using xml;

namespace xml.Tests;

[TestFixture]
public class EntitySecurityTests
{
    [Test]
    public void RecursivelyExpandsGeneralEntitiesAndDetectsCycles()
    {
        XmlDocument expanded = Parse("<!DOCTYPE root [<!ENTITY first 'A&second;C'><!ENTITY second 'B'>]><root>&first;</root>", CreateLimits());

        Assert.That(string.Concat(expanded.DocumentElement!.Children!.Select(node => node.Value)), Is.EqualTo("ABC"));

        XmlSecurityLimits cycleLimits = CreateLimits();
        XmlWellFormednessException failure = Assert.Throws<XmlWellFormednessException>(() => Parse("<!DOCTYPE root [<!ENTITY first '&second;'><!ENTITY second '&first;'>]><root>&first;</root>", cycleLimits))!;
        Assert.That(failure.Message, Does.Contain("recursive"));
    }

    [Test]
    public void EnforcesGeneralEntityDepthAndActivationLimits()
    {
        string text = "<!DOCTYPE root [<!ENTITY first '&second;'><!ENTITY second 'value'>]><root>&first;</root>";
        XmlSecurityLimits depthLimits = CreateLimits();
        depthLimits.MaximumEntityDepth = 1;
        XmlSecurityLimits countLimits = CreateLimits();
        countLimits.MaximumEntityCount = 1;

        XmlLimitException depth = Assert.Throws<XmlLimitException>(() => Parse(text, depthLimits))!;
        XmlLimitException count = Assert.Throws<XmlLimitException>(() => Parse(text, countLimits))!;

        Assert.That(depth.Message, Does.Contain("entity nesting depth"));
        Assert.That(count.Message, Does.Contain("entity activation count"));
    }

    [Test]
    public void EnforcesExpandedCharacterAndAmplificationLimits()
    {
        string text = "<!DOCTYPE root [<!ENTITY x 'four'>]><root>&x;</root>";
        XmlSecurityLimits characterLimits = CreateLimits();
        characterLimits.MaximumEntityCharacters = 3;
        XmlSecurityLimits amplificationLimits = CreateLimits();
        amplificationLimits.MaximumEntityAmplification = 1;

        XmlLimitException characters = Assert.Throws<XmlLimitException>(() => Parse(text, characterLimits))!;
        XmlLimitException amplification = Assert.Throws<XmlLimitException>(() => Parse(text, amplificationLimits))!;

        Assert.That(characters.Message, Does.Contain("expanded entity character count"));
        Assert.That(amplification.Message, Does.Contain("entity amplification"));
    }

    [Test]
    public void EnforcesDtdCharacterAndParameterEntityNestingLimits()
    {
        XmlSecurityLimits dtdLimits = CreateLimits();
        dtdLimits.MaximumDtdCharacters = 5;
        Assert.Throws<XmlLimitException>(() => Parse("<!DOCTYPE root [<!ENTITY x 'value'>]><root/>", dtdLimits));

        XmlSecurityLimits entityLimits = CreateLimits();
        entityLimits.MaximumEntityDepth = 1;
        ParserWorkLimiter limiter = new ParserWorkLimiter(entityLimits, null);
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);
        parser.ConfigureDtdParserWork(limiter);
        parser.RegisterInternalDtdParameterEntity("first", "%second;", null);
        parser.RegisterInternalDtdParameterEntity("second", "<!ELEMENT root EMPTY>", null);

        Assert.Throws<XmlLimitException>(() => parser.LoadDtdSubsets("%first;", null, null, null));
    }

    [Test]
    public void LoadsAllowedExternalGeneralEntitiesWithDeclarationBaseUri()
    {
        EntityResolver resolver = new EntityResolver();
        resolver.AddResource("message.ent", "external value", XmlResourceAccess.Allowed);
        XmlSecurityLimits limits = CreateLimits();

        XmlDocument document = ParseWithResolver("<!DOCTYPE root [<!ENTITY message SYSTEM 'message.ent'>]><root>&message;</root>", limits, resolver, AllowedPolicy());

        Assert.That(string.Concat(document.DocumentElement!.Children!.Select(node => node.Value)), Is.EqualTo("external value"));
        Assert.That(resolver.Kind, Is.EqualTo(XmlResourceKind.GeneralEntity));
        Assert.That(resolver.BaseUri, Is.EqualTo("urn:document"));
    }

    [Test]
    public void TreatsDeniedExternalGeneralEntitiesAsUnresolved()
    {
        EntityResolver resolver = new EntityResolver();
        resolver.AddResource("secret.ent", "secret", XmlResourceAccess.Denied);

        XmlWellFormednessException failure = Assert.Throws<XmlWellFormednessException>(() => ParseWithResolver("<!DOCTYPE root [<!ENTITY secret SYSTEM 'secret.ent'>]><root>&secret;</root>", CreateLimits(), resolver, AllowedPolicy()))!;

        Assert.That(failure.Message, Does.Contain("unresolved"));
        Assert.That(resolver.Kind, Is.EqualTo(XmlResourceKind.GeneralEntity));
    }

    [Test]
    public void EnforcesExternalResourceCountDepthAndByteLimits()
    {
        string text = "<!DOCTYPE root [<!ENTITY message SYSTEM 'message.ent'>]><root>&message;</root>";
        EntityResolver countResolver = new EntityResolver();
        countResolver.AddResource("message.ent", "value", XmlResourceAccess.Allowed);
        XmlSecurityLimits countLimits = CreateLimits();
        countLimits.MaximumExternalResources = 0;
        Assert.Throws<XmlLimitException>(() => ParseWithResolver(text, countLimits, countResolver, AllowedPolicy()));

        EntityResolver depthResolver = new EntityResolver();
        depthResolver.AddResource("message.ent", "value", XmlResourceAccess.Allowed);
        XmlSecurityLimits depthLimits = CreateLimits();
        depthLimits.MaximumResourceDepth = 0;
        Assert.Throws<XmlLimitException>(() => ParseWithResolver(text, depthLimits, depthResolver, AllowedPolicy()));

        EntityResolver byteResolver = new EntityResolver();
        byteResolver.AddResource("message.ent", "value", XmlResourceAccess.Allowed);
        XmlSecurityLimits byteLimits = CreateLimits();
        byteLimits.MaximumResourceBytes = 4;
        Assert.Throws<XmlLimitException>(() => ParseWithResolver(text, byteLimits, byteResolver, AllowedPolicy()));
    }

    private static XmlDocument Parse(string text, XmlSecurityLimits limits)
    {
        return ParseWithResolver(text, limits, null, null);
    }

    private static XmlDocument ParseWithResolver(string text, XmlSecurityLimits limits, IXmlResourceResolver? resolver, XmlResolverPolicy? policy)
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
        options.ValidationMode = XmlValidationMode.None;
        options.SecurityLimits = limits;
        options.ResolverPolicy = policy;
        return new XmlParser().ParseXmlDocument(input, options, resolver, null, null);
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

    private sealed class EntityResolver : IXmlResourceResolver
    {
        public Dictionary<string, XmlResolvedResource> Resources = new Dictionary<string, XmlResolvedResource>(StringComparer.Ordinal);
        public XmlResourceKind Kind;
        public string? BaseUri;

        public void AddResource(string systemIdentifier, string text, XmlResourceAccess access)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = text;
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = XmlResourceKind.GeneralEntity;
            resource.Access = access;
            resource.Input = input;
            resource.AbsoluteUri = "urn:" + systemIdentifier;
            Resources.Add(systemIdentifier, resource);
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Kind = kind;
            BaseUri = baseUri;
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
