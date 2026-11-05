using xml;

namespace xml.Tests;

[TestFixture]
public class XsltFormatCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void CompilesAndMergesDecimalFormatsPropertyByProperty()
    {
        FormatResolver resolver = new FormatResolver();
        resolver.Resources.Add(
            "https://example.test/base.xsl",
            Stylesheet("<xsl:decimal-format name='f:number' decimal-separator=',' grouping-separator='.' infinity='Forever'/>", "xmlns:f='urn:formats'"));
        string source = Stylesheet(
            "<xsl:import href='base.xsl'/>" +
            "<xsl:decimal-format name='f:number' decimal-separator=':' NaN='NotANumber'/>" +
            "<xsl:variable name='formatted' select=\"format-number(12, '0', 'f:number')\"/>",
            "xmlns:f='urn:formats'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source, resolver));
        XsltDecimalFormatPlan format = stylesheet.DecimalFormats.Single(item => item.Name == "Q{urn:formats}number");

        Assert.Multiple(() =>
        {
            Assert.That(stylesheet.DecimalFormats.Any(item => item.Name == "#default"), Is.True);
            Assert.That(format.Declarations, Has.Length.EqualTo(2));
            Assert.That(format.DecimalSeparator, Is.EqualTo(":"));
            Assert.That(format.GroupingSeparator, Is.EqualTo("."));
            Assert.That(format.Infinity, Is.EqualTo("Forever"));
            Assert.That(format.NaN, Is.EqualTo("NotANumber"));
            Assert.That(format.ZeroDigit, Is.EqualTo("0"));
            Assert.That(stylesheet.StaticContexts.All(context => context.DecimalFormats.Any(name => name.NamespaceUri == "urn:formats" && name.LocalName == "number")), Is.True);
            Assert.That(stylesheet.Variables.Single(item => item.Name.LocalName == "formatted").SelectPlan, Is.Not.Null);
        });
    }

    [TestCase("<xsl:decimal-format decimal-separator=','/><xsl:decimal-format decimal-separator=';'/>", XsltErrorCodes.XTSE1290)]
    [TestCase("<xsl:decimal-format decimal-separator=',' grouping-separator=','/>", XsltErrorCodes.XTSE1300)]
    [TestCase("<xsl:decimal-format decimal-separator='ab'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:decimal-format zero-digit='1'/>", XsltErrorCodes.XTSE1295)]
    public void RejectsInvalidDecimalFormats(string declarations, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declarations, "")))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void ExpandsCharacterMapsDepthFirstWithLaterEntriesWinning()
    {
        string source = Stylesheet(
            "<xsl:character-map name='combined' use-character-maps='base accents'>" +
            "<xsl:output-character character='x' string='local-x'/>" +
            "</xsl:character-map>" +
            "<xsl:character-map name='base'><xsl:output-character character='x' string='base-x'/></xsl:character-map>" +
            "<xsl:character-map name='accents'><xsl:output-character character='é' string='accent'/><xsl:output-character character='😀' string='face'/></xsl:character-map>",
            "");

        XsltCharacterMapPlan map = Plan(Compile(source)).CharacterMaps.Single(item => item.Name == "Q{}combined");

        Assert.Multiple(() =>
        {
            Assert.That(map.UsedMaps.Select(item => item.Name), Is.EqualTo(new[] { "Q{}base", "Q{}accents" }));
            Assert.That(map.EffectiveEntries, Has.Length.EqualTo(3));
            Assert.That(map.EffectiveEntries.Single(item => item.CodePoint == (uint)'x').Replacement, Is.EqualTo("local-x"));
            Assert.That(map.EffectiveEntries.Single(item => item.CodePoint == 0xE9).Replacement, Is.EqualTo("accent"));
            Assert.That(map.EffectiveEntries.Single(item => item.CodePoint == 0x1F600).Replacement, Is.EqualTo("face"));
            Assert.That(map.EffectiveEntries.All(item => item.MapName!.LocalName == "combined" || item.MapName.LocalName == "accents"), Is.True);
        });
    }

    [Test]
    public void HigherPrecedenceCharacterMapReplacesTheLowerDeclaration()
    {
        FormatResolver resolver = new FormatResolver();
        resolver.Resources.Add("https://example.test/base.xsl", Stylesheet("<xsl:character-map name='map'><xsl:output-character character='a' string='low'/></xsl:character-map>", ""));
        string source = Stylesheet("<xsl:import href='base.xsl'/><xsl:character-map name='map'><xsl:output-character character='b' string='high'/></xsl:character-map>", "");

        XsltCharacterMapPlan map = Plan(Compile(source, resolver)).CharacterMaps.Single();

        Assert.Multiple(() =>
        {
            Assert.That(map.Declarations, Has.Length.EqualTo(2));
            Assert.That(map.Declarations.Count(item => item.IsEffective), Is.EqualTo(1));
            Assert.That(map.EffectiveEntries.Single().CodePoint, Is.EqualTo((uint)'b'));
        });
    }

    [TestCase("<xsl:character-map><xsl:output-character character='a' string='x'/></xsl:character-map>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:character-map name='a'/><xsl:character-map name='a'/>", XsltErrorCodes.XTSE1580)]
    [TestCase("<xsl:character-map name='a' use-character-maps='missing'/>", XsltErrorCodes.XTSE1590)]
    [TestCase("<xsl:character-map name='a' use-character-maps='b'/><xsl:character-map name='b' use-character-maps='a'/>", XsltErrorCodes.XTSE1600)]
    [TestCase("<xsl:character-map name='a'><xsl:output-character character='ab' string='x'/></xsl:character-map>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:character-map name='a'><xsl:output-character character='a'/></xsl:character-map>", XsltErrorCodes.XTSE0010)]
    public void RejectsInvalidCharacterMaps(string declarations, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declarations, "")))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void MergesOutputDefinitionsAndBindsCharacterMaps()
    {
        string source = Stylesheet(
            "<xsl:character-map name='first'><xsl:output-character character='a' string='A'/></xsl:character-map>" +
            "<xsl:character-map name='second'><xsl:output-character character='a' string='AA'/><xsl:output-character character='b' string='B'/></xsl:character-map>" +
            "<xsl:output name='o:json' method='json' indent='yes' cdata-section-elements='a:item' use-character-maps='first'/>" +
            "<xsl:output name='o:json' indent='true' cdata-section-elements='b:other a:item' suppress-indentation='a:item' use-character-maps='second'/>",
            "xmlns:o='urn:outputs' xmlns:a='urn:a' xmlns:b='urn:b'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));
        XsltOutputFormatPlan output = stylesheet.OutputFormats.Single(item => item.Name == "Q{urn:outputs}json");

        Assert.Multiple(() =>
        {
            Assert.That(stylesheet.OutputFormats.Any(item => item.Name == "#default"), Is.True);
            Assert.That(output.Declarations, Has.Length.EqualTo(2));
            Assert.That(output.HasMethod, Is.True);
            Assert.That(output.Options.Method, Is.EqualTo(XmlSerializationMethod.Json));
            Assert.That(output.Options.Indent, Is.True);
            Assert.That(output.BuildTree, Is.False);
            Assert.That(output.Options.CDataSectionElements!.Select(Name), Is.EqualTo(new[] { "Q{urn:a}item", "Q{urn:b}other" }));
            Assert.That(output.Options.SuppressIndentation!.Select(Name), Is.EqualTo(new[] { "Q{urn:a}item" }));
            Assert.That(output.CharacterMapNames, Is.EqualTo(new[] { "Q{}first", "Q{}second" }));
            Assert.That(output.CharacterMaps.Select(item => item.Name), Is.EqualTo(output.CharacterMapNames));
            Assert.That(output.Options.UseCharacterMaps, Has.Length.EqualTo(2));
            Assert.That(output.Options.UseCharacterMaps!.Single(item => item.CodePoint == (uint)'a').Replacement, Is.EqualTo("AA"));
        });
    }

    [TestCase("<xsl:output method='xml'/><xsl:output method='text'/>", XsltErrorCodes.XTSE1560)]
    [TestCase("<xsl:output method='unknown'/>", XsltErrorCodes.XTSE1570)]
    [TestCase("<xsl:output use-character-maps='missing'/>", XsltErrorCodes.XTSE1590)]
    [TestCase("<xsl:output indent='perhaps'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:output html-version='five'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:output>not-empty</xsl:output>", XsltErrorCodes.XTSE0260)]
    public void RejectsInvalidOutputDefinitions(string declarations, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declarations, "")))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void CompilesWhitespaceNameTestsWithPrecedenceAndSpecificity()
    {
        FormatResolver resolver = new FormatResolver();
        resolver.Resources.Add("https://example.test/base.xsl", Stylesheet("<xsl:strip-space elements='* p:*'/>", "xmlns:p='urn:p'"));
        string source = Stylesheet(
            "<xsl:import href='base.xsl'/>" +
            "<xsl:preserve-space elements='item *:local p:* Q{urn:q}*'/>",
            "xmlns:p='urn:p' xpath-default-namespace='urn:default'");

        XsltWhitespaceRulePlan[] rules = Plan(Compile(source, resolver)).WhitespaceRules;

        Assert.Multiple(() =>
        {
            Assert.That(rules, Has.Length.EqualTo(6));
            Assert.That(rules.Single(item => item.Source == "item").Name, Is.EqualTo("Q{urn:default}item"));
            Assert.That(rules.Single(item => item.Source == "item").Priority, Is.EqualTo(2));
            Assert.That(rules.Single(item => item.Source == "*:local").Kind, Is.EqualTo(XsltWhitespaceNameTestKind.LocalName));
            Assert.That(rules.Count(item => item.Name == "Q{urn:p}*"), Is.EqualTo(2));
            Assert.That(rules.Single(item => item.Source == "Q{urn:q}*").Kind, Is.EqualTo(XsltWhitespaceNameTestKind.Namespace));
            Assert.That(rules.Where(item => item.Name == "Q{urn:p}*").Select(item => item.ImportPrecedence), Is.Ordered.Ascending);
        });
    }

    [TestCase("<xsl:strip-space/>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:strip-space elements='p:*'/>", XsltErrorCodes.XTSE0280)]
    [TestCase("<xsl:strip-space elements='item'/><xsl:preserve-space elements='item'/>", XsltErrorCodes.XTSE0270)]
    public void RejectsInvalidWhitespaceDeclarations(string declarations, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declarations, "")))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    private static string Name(XmlName name)
    {
        return "Q{" + name.NamespaceUri + "}" + name.LocalName;
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source, IXmlResourceResolver? resolver = null)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = resolver == null ? XmlResourceAccess.Denied : XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver == null ? new XmlDefaultResolver() : resolver);
    }

    private sealed class FormatResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>();

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string absolute = new Uri(new Uri(baseUri!), systemIdentifier!).AbsoluteUri;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                Input = new XmlInput { Text = Resources[absolute] },
                RequestedUri = systemIdentifier,
                AbsoluteUri = absolute
            };
        }
    }
}
