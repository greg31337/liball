using xml;

namespace xml.Tests;

[TestFixture]
public class XsltCompilationConformanceTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void CompilesEveryStylesheetFormAcrossCompatibilityVersions()
    {
        XsltStylesheetPlan stylesheet = Plan(Compile("<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='1.0'><xsl:template name='entry'/></xsl:stylesheet>", null, XsltConformanceLevel.Basic, null));
        XsltStylesheetPlan transform = Plan(Compile("<xsl:transform xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='entry'/></xsl:transform>", null, XsltConformanceLevel.Basic, null));
        XsltStylesheetPlan simplified = Plan(Compile("<result xmlns='urn:result' xmlns:xsl='" + Xsl + "' xsl:version='3.0'/>", null, XsltConformanceLevel.Basic, null));
        XsltStylesheetPlan package = Plan(Compile("<xsl:package xmlns:xsl='" + Xsl + "' name='urn:package' package-version='1' version='4.0'><xsl:template name='entry' visibility='public'/></xsl:package>", null, XsltConformanceLevel.Basic, null));

        Assert.Multiple(() =>
        {
            Assert.That(stylesheet.PrincipalModule.Kind, Is.EqualTo(XsltStylesheetModuleKind.Stylesheet));
            Assert.That(stylesheet.Conformance.HasBackwardsCompatibleCode, Is.True);
            Assert.That(transform.PrincipalModule.Kind, Is.EqualTo(XsltStylesheetModuleKind.Transform));
            Assert.That(simplified.PrincipalModule.Kind, Is.EqualTo(XsltStylesheetModuleKind.Simplified));
            Assert.That(simplified.Templates.Single().Match, Is.EqualTo("/"));
            Assert.That(package.PrincipalModule.Kind, Is.EqualTo(XsltStylesheetModuleKind.Package));
            Assert.That(package.Conformance.HasForwardsCompatibleCode, Is.True);
        });
    }

    [Test]
    public void CompilesEveryTopLevelDeclarationFamily()
    {
        CompilationResolver resolver = new CompilationResolver();
        resolver.Resources.Add("https://example.test/base.xsl", Stylesheet("<xsl:template name='base'/>", ""));
        resolver.Resources.Add("https://example.test/included.xsl", Stylesheet("<xsl:template name='included'/>", ""));
        string schema = "<xs:schema xmlns:xs='" + Xs + "' targetNamespace='urn:types'><xs:simpleType name='Code'><xs:restriction base='xs:string'/></xs:simpleType></xs:schema>";
        string declarations =
            "<xsl:import href='base.xsl'/>" +
            "<xsl:include href='included.xsl'/>" +
            "<xsl:import-schema namespace='urn:types'>" + schema + "</xsl:import-schema>" +
            "<xsl:global-context-item use='optional'/>" +
            "<xsl:decimal-format name='decimal'/>" +
            "<xsl:character-map name='characters'><xsl:output-character character='A' string='B'/></xsl:character-map>" +
            "<xsl:output name='result' method='xml' use-character-maps='characters'/>" +
            "<xsl:namespace-alias stylesheet-prefix='s' result-prefix='r'/>" +
            "<xsl:strip-space elements='discard'/>" +
            "<xsl:preserve-space elements='retain'/>" +
            "<xsl:key name='by-id' match='item' use='@id'/>" +
            "<xsl:accumulator name='count' as='xs:integer' initial-value='0'><xsl:accumulator-rule match='item' select='$value + 1'/></xsl:accumulator>" +
            "<xsl:mode name='work'/>" +
            "<xsl:param name='parameter' select='1'/>" +
            "<xsl:variable name='variable' select='2'/>" +
            "<xsl:attribute-set name='attributes'><xsl:attribute name='class'/></xsl:attribute-set>" +
            "<xsl:function name='f:value'><xsl:sequence select='1'/></xsl:function>" +
            "<xsl:template name='entry' match='item'/>";
        string source = Stylesheet(declarations, "xmlns:xs='" + Xs + "' xmlns:f='urn:functions' xmlns:s='urn:stylesheet' xmlns:r='urn:result'");
        XsltStylesheetPlan stylesheet = Plan(Compile(source, resolver, XsltConformanceLevel.SchemaAware, null));

        resolver.Resources.Add("https://example.test/library.xsl", "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:library' package-version='1' version='3.0'><xsl:template name='library-entry' visibility='public'/></xsl:package>");
        XsltPackageReference reference = new XsltPackageReference
        {
            Name = new XmlName { LocalName = "urn:library" },
            Version = "1",
            LocationUri = "library.xsl"
        };
        string packageSource = "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:main' package-version='1' version='3.0'>" +
            "<xsl:use-package name='urn:library' package-version='1'/>" +
            "<xsl:expose component='template' names='entry' visibility='public'/>" +
            "<xsl:template name='entry' visibility='public'/></xsl:package>";
        XsltStylesheetPlan package = Plan(Compile(packageSource, resolver, XsltConformanceLevel.SchemaAware, [reference]));
        IEnumerable<string> compiledDeclarations = stylesheet.Declarations.Concat(package.Declarations).Select(item => item.LocalName);
        if (stylesheet.Modules.Any(item => item.Relation == XsltStylesheetModuleRelation.Include))
        {
            compiledDeclarations = compiledDeclarations.Concat(["include"]);
        }

        string[] actual = compiledDeclarations.Distinct().OrderBy(item => item).ToArray();

        Assert.That(actual, Is.EqualTo(new[]
        {
            "accumulator", "attribute-set", "character-map", "decimal-format", "expose", "function", "global-context-item", "import",
            "import-schema", "include", "key", "mode", "namespace-alias", "output", "param", "preserve-space", "strip-space", "template",
            "use-package", "variable"
        }));
    }

    [Test]
    public void CompilesEveryInstructionAndHelperKind()
    {
        string[] sources =
        [
            Stylesheet(
                "<xsl:param name='items' select='1 to 3'/><xsl:param name='p' select=\"'value'\"/>" +
                "<xsl:template name='helper'><xsl:context-item use='optional'/><xsl:param name='p'/><xsl:variable name='local' select='$p'/><xsl:sequence select='$local'/></xsl:template>" +
                "<xsl:template match='item'><xsl:apply-templates select='$items'><xsl:sort select='.'/><xsl:with-param name='p' select='$p'/></xsl:apply-templates>" +
                "<xsl:call-template name='helper'><xsl:with-param name='p' select='$p'/></xsl:call-template><xsl:if test='exists($items)'><xsl:value-of select='$p'/></xsl:if>" +
                "<xsl:choose><xsl:when test='empty($items)'><xsl:text>empty</xsl:text></xsl:when><xsl:otherwise><xsl:sequence select='$items'/></xsl:otherwise></xsl:choose>" +
                "<xsl:for-each select='$items'><xsl:sort select='.'/><xsl:sequence select='.'/></xsl:for-each><xsl:perform-sort select='$items'><xsl:sort select='.'/></xsl:perform-sort>" +
                "<xsl:next-match><xsl:with-param name='p' select='$p'/></xsl:next-match><xsl:apply-imports><xsl:with-param name='p' select='$p'/></xsl:apply-imports></xsl:template>", ""),
            Stylesheet(
                "<xsl:template name='construct'><xsl:document><xsl:element name='item'><xsl:attribute name='code' select=\"'v'\"/><xsl:namespace name='p' select=\"'urn:p'\"/>" +
                "<xsl:comment select=\"'note'\"/><xsl:processing-instruction name='go' select=\"'now'\"/></xsl:element></xsl:document><xsl:copy select='.'><xsl:value-of select='.'/></xsl:copy>" +
                "<xsl:copy-of select='.'/><xsl:map><xsl:map-entry key=\"'key'\" select='1'/></xsl:map><xsl:array><xsl:array-member select='1'/><xsl:sequence select='2'/></xsl:array></xsl:template>", ""),
            Stylesheet(
                "<xsl:param name='items' select='1 to 3'/><xsl:template name='flow'><xsl:iterate select='$items'><xsl:param name='state' select='0'/><xsl:on-completion><xsl:sequence select='$state'/></xsl:on-completion>" +
                "<xsl:next-iteration><xsl:with-param name='state' select='$state + 1'/></xsl:next-iteration><xsl:break select='$state'/></xsl:iterate>" +
                "<xsl:for-each-group select='$items' group-starting-with='item'><xsl:sequence select='current-group()'/></xsl:for-each-group>" +
                "<xsl:analyze-string select=\"'abc'\" regex='a'><xsl:matching-substring><xsl:sequence select='.'/></xsl:matching-substring><xsl:non-matching-substring><xsl:sequence select='.'/></xsl:non-matching-substring></xsl:analyze-string>" +
                "<xsl:try><xsl:sequence select='1'/><xsl:catch errors='*'><xsl:sequence select='$err:code'/></xsl:catch></xsl:try>" +
                "<xsl:where-populated><xsl:on-empty><xsl:text>empty</xsl:text></xsl:on-empty><xsl:on-non-empty><xsl:text>full</xsl:text></xsl:on-non-empty></xsl:where-populated></xsl:template>",
                "xmlns:err='http://www.w3.org/2005/xqt-errors'"),
            Stylesheet(
                "<xsl:param name='items' select='1 to 3'/><xsl:template name='advanced'><xsl:evaluate xpath=\"'1 + 1'\" as='item()*' context-item='.'/><xsl:result-document href='result.xml'><xsl:sequence select='$items'/></xsl:result-document>" +
                "<xsl:source-document href='source.xml'><xsl:sequence select='.'/></xsl:source-document><xsl:fork><xsl:sequence select='1'/><xsl:sequence select='2'/></xsl:fork>" +
                "<xsl:merge><xsl:merge-source name='source' select='$items'><xsl:merge-key select='.'/></xsl:merge-source><xsl:merge-action><xsl:sequence select='current-merge-group()'/></xsl:merge-action></xsl:merge>" +
                "<xsl:assert test='exists($items)' select=\"'missing'\"/><xsl:message select=\"'done'\"/><xsl:number select='.' count='item' from='root'/></xsl:template>", ""),
            Stylesheet("<xsl:template name='future'><xsl:future version='4.0'><xsl:fallback><xsl:sequence select='1'/></xsl:fallback></xsl:future></xsl:template>", "")
        ];
        HashSet<XsltInstructionKind> actual = new HashSet<XsltInstructionKind>();

        foreach (string source in sources)
        {
            foreach (XsltInstructionPlan instruction in Plan(Compile(source, null, XsltConformanceLevel.Basic, null)).Instructions)
            {
                actual.Add(instruction.Kind);
            }
        }

        Assert.That(actual.OrderBy(item => item), Is.EqualTo(System.Enum.GetValues<XsltInstructionKind>()));
    }

    [Test]
    public void IntegratesModulePrecedenceStaticEvaluationValueTemplatesAndPatterns()
    {
        CompilationResolver resolver = new CompilationResolver();
        resolver.Resources.Add("https://example.test/base.xsl", Stylesheet("<xsl:variable name='setting' select=\"'base'\"/><xsl:template name='base' match='item' priority='100'/>", ""));
        resolver.Resources.Add("https://example.test/selected.xsl", Stylesheet("<xsl:template name='included' match='included[@ready]'/>", ""));
        string source = Stylesheet(
            "<xsl:import href='base.xsl'/><xsl:param name='enabled' static='yes' select='true()'/><xsl:include _href=\"{'selected.xsl'}\" use-when='$enabled'/>" +
            "<xsl:variable name='setting' select=\"'principal'\"/><xsl:template name='entry' match='item[@active] | other' priority='2'><out data='value-{$setting}'>text-{$setting}</out></xsl:template>",
            "expand-text='yes'");

        XsltStylesheetPlan plan = Plan(Compile(source, resolver, XsltConformanceLevel.Basic, null));
        XsltTemplatePlan entry = plan.Templates.Single(item => item.Name == "Q{}entry");

        Assert.Multiple(() =>
        {
            Assert.That(plan.Levels.Select(item => item.ImportPrecedence), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(plan.Modules.Select(item => item.Relation), Is.EquivalentTo(new[] { XsltStylesheetModuleRelation.Principal, XsltStylesheetModuleRelation.Import, XsltStylesheetModuleRelation.Include }));
            Assert.That(plan.Variables.Single(item => item.ExpandedName == "Q{}setting" && item.IsEffective).Select, Is.EqualTo("'principal'"));
            Assert.That(plan.Templates.Any(item => item.Name == "Q{}included"), Is.True);
            Assert.That(entry.MatchPlan!.IsDeferred, Is.False);
            Assert.That(entry.MatchPlan.Predicates, Is.Not.Empty);
            Assert.That(plan.ValueTemplates.Count(item => item.Kind == XsltValueTemplateKind.Attribute), Is.EqualTo(1));
            Assert.That(plan.ValueTemplates.Count(item => item.Kind == XsltValueTemplateKind.Text), Is.EqualTo(1));
            Assert.That(plan.ValueTemplates.All(item => item.Parts.Any(part => part.ExpressionPlan != null)), Is.True);
        });
    }

    [TestCase("<xsl:template/>", XsltErrorCodes.XTSE0500)]
    [TestCase("<xsl:template match='item['/>", XsltErrorCodes.XTSE0340)]
    [TestCase("<xsl:variable name='same' select='1'/><xsl:variable name='same' select='2'/>", XsltErrorCodes.XTSE0630)]
    [TestCase("<xsl:template name='entry'><out data='{1'/></xsl:template>", XsltErrorCodes.XTSE0350)]
    public void ReportsRepresentativeStaticCompilationErrors(string content, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(content, ""), null, XsltConformanceLevel.Basic, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
            Assert.That(error.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XsltStatic));
            Assert.That(error.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void ReportsCompilationTypeErrorsWithTheXsltTypeCategory()
    {
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:library' package-version='1' version='3.0'><xsl:global-context-item use='required'/></xsl:package>";
        XsltCompileOptions options = Options(XsltConformanceLevel.Basic, null, null);
        XsltStylesheetLoader loader = new XsltStylesheetLoader(options, new XmlDefaultResolver(), new XsltPackageCompilationContext(), true);

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => loader.CompileXsltStylesheet(new XmlInput { Text = source }))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE0590));
            Assert.That(error.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XsltType));
            Assert.That(error.Diagnostic.Range, Is.Not.Null);
        });
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0' " + attributes + ">" + content + "</xsl:stylesheet>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source, CompilationResolver? resolver, XsltConformanceLevel level, XsltPackageReference[]? packages)
    {
        IXmlResourceResolver actualResolver = resolver == null ? new XmlDefaultResolver() : resolver;
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, Options(level, resolver, packages), actualResolver);
    }

    private static XsltCompileOptions Options(XsltConformanceLevel level, CompilationResolver? resolver, XsltPackageReference[]? packages)
    {
        return new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ConformanceLevel = level,
            Packages = packages ?? [],
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = resolver == null ? XmlResourceAccess.Denied : XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
    }

    private sealed class CompilationResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>(System.StringComparer.Ordinal);

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string absolute = new System.Uri(new System.Uri(baseUri!), systemIdentifier!).AbsoluteUri;
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
