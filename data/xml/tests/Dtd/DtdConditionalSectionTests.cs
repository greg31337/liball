using xml;

namespace xml.Tests;

[TestFixture]
public class DtdConditionalSectionTests
{
    [Test]
    public void ProcessesIncludedDeclarationsAndNestedIncludeSections()
    {
        ConditionalResolver resolver = new ConditionalResolver();
        resolver.AddExternalSubset("root.dtd", "<![ INCLUDE [<!ELEMENT outer EMPTY><![INCLUDE[<!ELEMENT inner EMPTY>]]>]]>");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", "urn:document");

        Assert.That(PhysicalText(subset), Is.EqualTo("<!ELEMENT outer EMPTY><!ELEMENT inner EMPTY>"));
        Assert.That(subset.Tokens.Any(entry => entry.Token.Kind == DtdTokenKind.ConditionalSectionOpen), Is.False);
        Assert.That(subset.Tokens.Any(entry => entry.Token.Kind == DtdTokenKind.ConditionalSectionClose), Is.False);
    }

    [Test]
    public void IgnoresAllMarkupAndReferencesUntilTheMatchingNestedDelimiter()
    {
        string text = "<![IGNORE[%missing; '<![ nested %also; ]]> still <!--unterminated ]]><!ELEMENT kept EMPTY>";
        ConditionalResolver resolver = new ConditionalResolver();
        resolver.AddExternalSubset("root.dtd", text);
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", null);

        Assert.That(PhysicalText(subset), Is.EqualTo("<!ELEMENT kept EMPTY>"));
        Assert.That(subset.Tokens.Any(entry => entry.Token.Name == "missing" || entry.Token.Name == "also"), Is.False);
    }

    [Test]
    public void UsesParameterEntitiesAsConditionalKeywords()
    {
        ConditionalResolver includeResolver = new ConditionalResolver();
        includeResolver.AddExternalSubset("include.dtd", "<![%switch;[<!ELEMENT included EMPTY>]]>");
        DtdParser includeParser = new DtdParser(XmlVersion.Xml10, includeResolver, AllowedPolicy());
        includeParser.RegisterInternalDtdParameterEntity("switch", " INCLUDE ", null);

        ConditionalResolver ignoreResolver = new ConditionalResolver();
        ignoreResolver.AddExternalSubset("ignore.dtd", "<![%switch;[<!ELEMENT ignored EMPTY>]]><!ELEMENT kept EMPTY>");
        DtdParser ignoreParser = new DtdParser(XmlVersion.Xml10, ignoreResolver, AllowedPolicy());
        ignoreParser.RegisterInternalDtdParameterEntity("switch", "%nested;", null);
        ignoreParser.RegisterInternalDtdParameterEntity("nested", "IGNORE", null);

        Assert.That(PhysicalText(includeParser.LoadDtdSubsets(null, null, "include.dtd", null)), Is.EqualTo("<!ELEMENT included EMPTY>"));
        Assert.That(PhysicalText(ignoreParser.LoadDtdSubsets(null, null, "ignore.dtd", null)), Is.EqualTo("<!ELEMENT kept EMPTY>"));
    }

    [Test]
    public void LoadsConditionalKeywordsFromExternalParameterEntities()
    {
        ConditionalResolver resolver = new ConditionalResolver();
        resolver.AddExternalSubset("root.dtd", "<![%switch;[<!ELEMENT included EMPTY>]]>");
        resolver.AddParameterEntity("switch.ent", "<?xml encoding='UTF-8'?>INCLUDE");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());
        parser.RegisterExternalDtdParameterEntity("switch", null, "switch.ent", "urn:root");

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", null);

        Assert.That(PhysicalText(subset), Is.EqualTo("<!ELEMENT included EMPTY>"));
    }

    [Test]
    public void ExpandsParameterEntitiesInsideIncludedSections()
    {
        ConditionalResolver resolver = new ConditionalResolver();
        resolver.AddExternalSubset("root.dtd", "<![INCLUDE[%declaration;]]>");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());
        parser.RegisterInternalDtdParameterEntity("declaration", "<!ELEMENT expanded EMPTY>", "urn:declaration");

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", null);

        Assert.That(PhysicalText(subset), Is.EqualTo("<!ELEMENT expanded EMPTY>"));
        Assert.That(subset.Tokens.Any(entry => entry.ParameterEntityName == "declaration" && !entry.Synthetic), Is.True);
    }

    [Test]
    public void AllowsConditionalSectionsInExternalParameterEntities()
    {
        ConditionalResolver resolver = new ConditionalResolver();
        resolver.AddExternalSubset("root.dtd", "%section;");
        resolver.AddParameterEntity("section.ent", "<![INCLUDE[<!ELEMENT external EMPTY>]]>");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());
        parser.RegisterExternalDtdParameterEntity("section", null, "section.ent", "urn:root");

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", null);

        Assert.That(PhysicalText(subset), Is.EqualTo("<!ELEMENT external EMPTY>"));
        Assert.That(subset.Tokens.Any(entry => entry.SourceKind == DtdSourceKind.ExternalParameterEntity && entry.ParameterEntityName == "section"), Is.True);
    }

    [Test]
    public void RejectsConditionalSectionsInInternalSourcesAndDeclarations()
    {
        DtdParser internalSubset = new DtdParser(XmlVersion.Xml10, null, null);
        Assert.Throws<FormatException>(() => internalSubset.LoadDtdSubsets("<![INCLUDE[<!ELEMENT root EMPTY>]]>", null, null, null));

        ConditionalResolver internalEntityResolver = new ConditionalResolver();
        internalEntityResolver.AddExternalSubset("entity.dtd", "%section;");
        DtdParser internalEntity = new DtdParser(XmlVersion.Xml10, internalEntityResolver, AllowedPolicy());
        internalEntity.RegisterInternalDtdParameterEntity("section", "<![INCLUDE[<!ELEMENT root EMPTY>]]>", null);
        Assert.Throws<FormatException>(() => internalEntity.LoadDtdSubsets(null, null, "entity.dtd", null));

        ConditionalResolver declarationResolver = new ConditionalResolver();
        declarationResolver.AddExternalSubset("declaration.dtd", "<!ELEMENT root (<![INCLUDE[child]]>)>");
        DtdParser declaration = new DtdParser(XmlVersion.Xml10, declarationResolver, AllowedPolicy());
        Assert.Throws<FormatException>(() => declaration.LoadDtdSubsets(null, null, "declaration.dtd", null));
    }

    [TestCase("<![MAYBE[]]>")]
    [TestCase("<![include[]]>")]
    [TestCase("<![INCLUDE extra]]>")]
    [TestCase("<![INCLUDE[")]
    [TestCase("<![IGNORE[unterminated")]
    [TestCase("]]>")]
    public void RejectsMalformedOrUnbalancedConditionalSections(string text)
    {
        ConditionalResolver resolver = new ConditionalResolver();
        resolver.AddExternalSubset("bad.dtd", text);
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        Assert.Throws<FormatException>(() => parser.LoadDtdSubsets(null, null, "bad.dtd", null));
    }

    [Test]
    public void RejectsAKeywordEntityThatExpandsToMoreThanOneKeyword()
    {
        ConditionalResolver resolver = new ConditionalResolver();
        resolver.AddExternalSubset("bad-keyword.dtd", "<![%switch;[]]>");
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());
        parser.RegisterInternalDtdParameterEntity("switch", "INCLUDE IGNORE", null);

        Assert.Throws<FormatException>(() => parser.LoadDtdSubsets(null, null, "bad-keyword.dtd", null));
    }

    [Test]
    public void RequiresEveryConditionalDelimiterFromAnEntityToCloseThere()
    {
        ConditionalResolver openingResolver = new ConditionalResolver();
        openingResolver.AddExternalSubset("opening.dtd", "%part;");
        openingResolver.AddParameterEntity("opening.ent", "<![INCLUDE[<!ELEMENT root EMPTY>");
        DtdParser openingParser = new DtdParser(XmlVersion.Xml10, openingResolver, AllowedPolicy());
        openingParser.RegisterExternalDtdParameterEntity("part", null, "opening.ent", null);
        Assert.Throws<FormatException>(() => openingParser.LoadDtdSubsets(null, null, "opening.dtd", null));

        ConditionalResolver closingResolver = new ConditionalResolver();
        closingResolver.AddExternalSubset("closing.dtd", "<![INCLUDE[%part;]]>");
        closingResolver.AddParameterEntity("closing.ent", "]]>");
        DtdParser closingParser = new DtdParser(XmlVersion.Xml10, closingResolver, AllowedPolicy());
        closingParser.RegisterExternalDtdParameterEntity("part", null, "closing.ent", null);
        Assert.Throws<FormatException>(() => closingParser.LoadDtdSubsets(null, null, "closing.dtd", null));
    }

    [Test]
    public void StillValidatesCharactersInsideIgnoredSections()
    {
        ConditionalResolver xml10Resolver = new ConditionalResolver();
        xml10Resolver.AddExternalSubset("xml10.dtd", "<![IGNORE[\0]]>");
        DtdParser xml10Parser = new DtdParser(XmlVersion.Xml10, xml10Resolver, AllowedPolicy());
        Assert.Throws<FormatException>(() => xml10Parser.LoadDtdSubsets(null, null, "xml10.dtd", null));

        ConditionalResolver xml11Resolver = new ConditionalResolver();
        xml11Resolver.AddExternalSubset("xml11.dtd", "<![IGNORE[\u0001]]>");
        DtdParser xml11Parser = new DtdParser(XmlVersion.Xml11, xml11Resolver, AllowedPolicy());
        Assert.Throws<FormatException>(() => xml11Parser.LoadDtdSubsets(null, null, "xml11.dtd", null));
    }

    [Test]
    public void DoesNotRecognizeConditionalDelimitersInsideLiterals()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        DtdSubset subset = parser.LoadDtdSubsets("<!ENTITY text '<![IGNORE[value]]>'>", null, null, null);

        Assert.That(PhysicalText(subset), Is.EqualTo("<!ENTITY text '<![IGNORE[value]]>'>"));
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

    private sealed class ConditionalResolver : IXmlResourceResolver
    {
        public Dictionary<string, XmlResolvedResource> Resources = new Dictionary<string, XmlResolvedResource>(StringComparer.Ordinal);

        public void AddExternalSubset(string systemIdentifier, string text)
        {
            AddResource(systemIdentifier, text, XmlResourceKind.ExternalSubset);
        }

        public void AddParameterEntity(string systemIdentifier, string text)
        {
            AddResource(systemIdentifier, text, XmlResourceKind.ParameterEntity);
        }

        public void AddResource(string systemIdentifier, string text, XmlResourceKind kind)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = text;
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = kind;
            resource.Access = XmlResourceAccess.Allowed;
            resource.Input = input;
            resource.AbsoluteUri = "urn:" + systemIdentifier;
            Resources.Add(systemIdentifier, resource);
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
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
