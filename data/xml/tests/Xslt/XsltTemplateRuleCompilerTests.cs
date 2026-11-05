using xml;

namespace xml.Tests;

[TestFixture]
public class XsltTemplateRuleCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void ComputesDefaultPrioritiesFromPatternSyntax()
    {
        string content =
            Template("any-item", ".") +
            Template("predicate", ".[true()]") +
            Template("root", "/") +
            Template("name", "item") +
            Template("name-wildcard", "*:item") +
            Template("wildcard", "*") +
            Template("named-element", "element(item)") +
            Template("typed-element", "element(*, xs:string)") +
            Template("named-typed-element", "element(item, xs:string)") +
            Template("document-element", "document-node(element(root))") +
            Template("processing", "processing-instruction('go')") +
            Template("path", "root/item") +
            Template("intersection", "item intersect *");
        XsltStylesheetPlan plan = Compile(Stylesheet(content, "xmlns:xs='http://www.w3.org/2001/XMLSchema'"), new RuleResolver());

        Assert.Multiple(() =>
        {
            Assert.That(Priority(plan, "any-item"), Is.EqualTo("-1"));
            Assert.That(Priority(plan, "predicate"), Is.EqualTo("1"));
            Assert.That(Priority(plan, "root"), Is.EqualTo("-0.5"));
            Assert.That(Priority(plan, "name"), Is.EqualTo("0"));
            Assert.That(Priority(plan, "name-wildcard"), Is.EqualTo("-0.25"));
            Assert.That(Priority(plan, "wildcard"), Is.EqualTo("-0.5"));
            Assert.That(Priority(plan, "named-element"), Is.EqualTo("0"));
            Assert.That(Priority(plan, "typed-element"), Is.EqualTo("0"));
            Assert.That(Priority(plan, "named-typed-element"), Is.EqualTo("0.25"));
            Assert.That(Priority(plan, "document-element"), Is.EqualTo("0"));
            Assert.That(Priority(plan, "processing"), Is.EqualTo("0"));
            Assert.That(Priority(plan, "path"), Is.EqualTo("0.5"));
            Assert.That(Priority(plan, "intersection"), Is.EqualTo("0"));
        });
    }

    [Test]
    public void SplitsDefaultPriorityUnionsButKeepsExplicitPriorityUnionsWhole()
    {
        string content = Template("split", "*:item | element(item)") +
            "<xsl:template name='whole' match='*:item | element(item)' priority='0.75'/>";
        XsltStylesheetPlan plan = Compile(Stylesheet(content, ""), new RuleResolver());
        XsltTemplateRulePlan[] split = Rules(plan, "split");
        XsltTemplateRulePlan[] whole = Rules(plan, "whole");

        Assert.Multiple(() =>
        {
            Assert.That(split.Select(rule => rule.Pattern.Source.Trim()), Is.EqualTo(new[] { "*:item", "element(item)" }));
            Assert.That(split.Select(rule => rule.Priority), Is.EqualTo(new[] { "-0.25", "0" }));
            Assert.That(split.Select(rule => rule.AlternativeOrder), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(whole, Has.Length.EqualTo(1));
            Assert.That(whole[0].Priority, Is.EqualTo("0.75"));
            Assert.That(whole[0].Pattern.Source, Does.Contain("|"));
        });
    }

    [Test]
    public void GivesSchemaDeclarationTestsTheirSpecificDefaultPriority()
    {
        XmlSchemaSet schemas = CompileSchema();
        string source = Stylesheet(Template("schema", "schema-element(s:root)"), "xmlns:s='urn:s'");
        XsltStylesheetPlan plan = CompileSchemaAware(source, schemas);

        Assert.That(Priority(plan, "schema"), Is.EqualTo("0.25"));
    }

    [Test]
    public void SelectsByPriorityThenDeclarationOrderAndReportsRequestedWarning()
    {
        string content =
            "<xsl:mode name='work' warning-on-multiple-match='yes'/>" +
            "<xsl:template name='specific' match='item' mode='work'/>" +
            "<xsl:template name='explicit' match='*' mode='work' priority='2'/>" +
            "<xsl:template name='later' match='item' mode='work' priority='2'/>";
        XsltStylesheetPlan plan = Compile(Stylesheet(content, ""), new RuleResolver());
        XsltTemplateSelectionResult result = Select(plan, "Q{}work", Element("item"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Template!.Name, Is.EqualTo("Q{}later"));
            Assert.That(result.Rule!.Priority, Is.EqualTo("2"));
            Assert.That(result.Ambiguous, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(XsltErrorCodes.XTDE0540));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(XmlDiagnosticSeverity.Warning));
            Assert.That(result.Diagnostics[0].RelatedLocation, Is.Not.Null);
        });
    }

    [Test]
    public void RaisesDynamicErrorWhenModeRejectsMultipleMatches()
    {
        string content =
            "<xsl:mode name='work' on-multiple-match='fail'/>" +
            "<xsl:template name='first' match='item' mode='work'/>" +
            "<xsl:template name='second' match='item' mode='work'/>";
        XsltStylesheetPlan plan = Compile(Stylesheet(content, ""), new RuleResolver());

        XsltDynamicException error = Assert.Throws<XsltDynamicException>(() => Select(plan, "Q{}work", Element("item")))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0540));
            Assert.That(error.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
            Assert.That(error.Location, Is.Not.Null);
        });
    }

    [Test]
    public void HigherImportPrecedenceWinsBeforePriority()
    {
        RuleResolver resolver = new RuleResolver();
        resolver.Resources.Add("https://example.test/imported.xsl", Stylesheet("<xsl:template name='imported' match='item' priority='100'/>", ""));
        string principal = Stylesheet(
            "<xsl:import href='imported.xsl'/><xsl:template name='principal' match='item' priority='-100'/>", "");
        XsltStylesheetPlan plan = Compile(principal, resolver);

        XsltTemplateSelectionResult result = Select(plan, "#unnamed", Element("item"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Template!.Name, Is.EqualTo("Q{}principal"));
            Assert.That(result.Rule!.Priority, Is.EqualTo("-100"));
            Assert.That(result.Rule.ImportPrecedence, Is.GreaterThan(Rules(plan, "imported").Single().ImportPrecedence));
        });
    }

    [Test]
    public void FiltersRulesByModeAndAddsAllModeRulesToEveryIndex()
    {
        string content =
            "<xsl:mode name='a'/><xsl:mode name='b'/>" +
            "<xsl:template name='all' match='item' mode='#all' priority='1'/>" +
            "<xsl:template name='only-a' match='item' mode='a' priority='2'/>" +
            "<xsl:template name='only-b' match='item' mode='b' priority='3'/>";
        XsltStylesheetPlan plan = Compile(Stylesheet(content, ""), new RuleResolver());
        XmlNode item = Element("item");

        Assert.Multiple(() =>
        {
            Assert.That(Select(plan, "Q{}a", item).Template!.Name, Is.EqualTo("Q{}only-a"));
            Assert.That(Select(plan, "Q{}b", item).Template!.Name, Is.EqualTo("Q{}only-b"));
            Assert.That(Select(plan, "#unnamed", item).Template!.Name, Is.EqualTo("Q{}all"));
            Assert.That(plan.TemplateRuleIndexes.Single(index => index.Mode == "Q{}a").Rules.Select(rule => rule.Template.Name), Is.EqualTo(new[] { "Q{}all", "Q{}only-a" }));
        });
    }

    [Test]
    public void UsesNameAndKindBucketsBeforeEvaluatingPatterns()
    {
        string content = Template("a", "a") + Template("b", "b") + Template("c", "c") + Template("elements", "*") + Template("anything", ".[true()]");
        XsltStylesheetPlan plan = Compile(Stylesheet(content, ""), new RuleResolver());

        XsltTemplateSelectionResult result = Select(plan, "#unnamed", Element("a"));

        Assert.Multiple(() =>
        {
            Assert.That(result.InspectedRuleCount, Is.EqualTo(3));
            Assert.That(result.InspectedRuleCount, Is.LessThan(plan.TemplateRuleIndexes.Single(index => index.Mode == "#unnamed").Rules.Length));
            Assert.That(result.Template!.Name, Is.EqualTo("Q{}anything"));
        });
    }

    [Test]
    public void SelectsPredicatePatternsForAtomicItems()
    {
        string content = "<xsl:template name='atomic' match='.[. instance of xs:string]'/>";
        XsltStylesheetPlan plan = Compile(Stylesheet(content, "xmlns:xs='http://www.w3.org/2001/XMLSchema'"), new RuleResolver());
        XdmAtomicValue value = new XdmFactory().CreateXdmStringAtomicValue("value", XdmAtomicType.String);
        XdmSequence candidate = new XdmSequenceOperations().CreateXdmAtomicSequence(value);

        XsltTemplateSelectionResult result = new XsltTemplateSelector().SelectXsltTemplateRule(plan, "#unnamed", candidate, new XPathDynamicContext());

        Assert.That(result.Template!.Name, Is.EqualTo("Q{}atomic"));
    }

    private static string Template(string name, string match)
    {
        return "<xsl:template name='" + name + "' match=\"" + match.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;") + "\"/>";
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XsltStylesheetPlan Compile(string source, RuleResolver resolver)
    {
        XmlResolverPolicy policy = new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            AllowedSchemes = ["https"],
            MaximumDepth = 16,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 1048576
        };
        XsltCompileOptions options = new XsltCompileOptions { BaseUri = "https://example.test/main.xsl", ResolverPolicy = policy };
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver);
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltStylesheetPlan CompileSchemaAware(string source, XmlSchemaSet schemas)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ConformanceLevel = XsltConformanceLevel.SchemaAware,
            SchemaSet = schemas,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, new XmlDefaultResolver());
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XmlSchemaSet CompileSchema()
    {
        string schema = "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='urn:s' xmlns:s='urn:s'>" +
            "<xs:element name='root' type='xs:string'/></xs:schema>";
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions
        {
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };
        return new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Text = schema, BaseUri = "https://example.test/schema.xsd" }], options, new XmlDefaultResolver());
    }

    private static string Priority(XsltStylesheetPlan plan, string templateName)
    {
        return Rules(plan, templateName).Single().Priority;
    }

    private static XsltTemplateRulePlan[] Rules(XsltStylesheetPlan plan, string templateName)
    {
        return plan.TemplateRuleIndexes.Single(index => index.Mode == "#unnamed").Rules.Where(rule => rule.Template.Name == "Q{}" + templateName).ToArray();
    }

    private static XsltTemplateSelectionResult Select(XsltStylesheetPlan plan, string mode, XmlNode node)
    {
        XdmSequence candidate = new XdmSequenceOperations().CreateXdmNodeSequence(node);
        return new XsltTemplateSelector().SelectXsltTemplateRule(plan, mode, candidate, new XPathDynamicContext());
    }

    private static XmlNode Element(string name)
    {
        XmlDocument document = new XmlParser().ParseXmlDocument(new XmlInput { Text = "<" + name + "/>" }, new XmlParserOptions(), null, null, null);
        return document.DocumentElement!;
    }

    private sealed class RuleResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>(StringComparer.Ordinal);

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
