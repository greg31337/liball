using xml;

namespace xml.Tests;

[TestFixture]
public class XsltTemplateCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void CompilesNamedAndMatchTemplateMetadata()
    {
        string source = Stylesheet(
            "<xsl:template name='m:entry' match='item' mode='m:primary #default' priority='+02.50' as='element()*' visibility='final'>" +
            "<xsl:context-item use='optional' as='element()'/><xsl:param name='input' as='xs:string' required='yes'/>" +
            "<result value='{$input}'><xsl:value-of select='.'/></result></xsl:template>",
            "xmlns:m='urn:modes' xmlns:xs='http://www.w3.org/2001/XMLSchema' default-mode='m:fallback'");

        XsltTemplatePlan template = Plan(Compile(source)).Templates.Single();

        Assert.Multiple(() =>
        {
            Assert.That(template.IsNamed, Is.True);
            Assert.That(template.IsTemplateRule, Is.True);
            Assert.That(template.Name, Is.EqualTo("Q{urn:modes}entry"));
            Assert.That(template.Match, Is.EqualTo("item"));
            Assert.That(template.MatchPlan, Is.Not.Null);
            Assert.That(template.MatchPlan!.IsDeferred, Is.False);
            Assert.That(template.MatchPlan.EquivalentExpression, Is.Not.Null);
            Assert.That(template.Modes.Select(mode => mode.Name), Is.EqualTo(new[] { "Q{urn:modes}primary", "Q{urn:modes}fallback" }));
            Assert.That(template.HasExplicitPriority, Is.True);
            Assert.That(template.PrioritySource, Is.EqualTo("+02.50"));
            Assert.That(template.Priority, Is.EqualTo("2.5"));
            Assert.That(template.ReturnType!.NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Element));
            Assert.That(template.ReturnType.Occurrence, Is.EqualTo("*"));
            Assert.That(template.Visibility, Is.EqualTo(XsltComponentVisibility.Final));
            Assert.That(template.Component, Is.Not.Null);
            Assert.That(template.ContextItem!.Use, Is.EqualTo(XsltContextItemUse.Optional));
            Assert.That(template.ContextItem.Type!.NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Element));
            Assert.That(template.Parameters.Single().Name.LocalName, Is.EqualTo("input"));
            Assert.That(template.Body.Items.Single().Kind, Is.EqualTo(XsltSequenceItemKind.LiteralResultElement));
            Assert.That(template.Body.Items.Single().AttributeTemplates, Has.Length.EqualTo(1));
            Assert.That(template.Body.Items.Single().Content!.Items.Single().Kind, Is.EqualTo(XsltSequenceItemKind.Instruction));
        });
    }

    [Test]
    public void CompilesNamedTemplateContextAndReturnType()
    {
        string source = Stylesheet("<xsl:template name='entry' as='empty-sequence()'><xsl:context-item use='absent'/></xsl:template>", "");

        XsltTemplatePlan template = Plan(Compile(source)).Templates.Single();

        Assert.Multiple(() =>
        {
            Assert.That(template.IsNamed, Is.True);
            Assert.That(template.IsTemplateRule, Is.False);
            Assert.That(template.Modes, Is.Empty);
            Assert.That(template.HasExplicitPriority, Is.False);
            Assert.That(template.ReturnType!.EmptySequence, Is.True);
            Assert.That(template.ContextItem!.Use, Is.EqualTo(XsltContextItemUse.Absent));
            Assert.That(template.Body.Items, Is.Empty);
        });
    }

    [Test]
    public void UsesEffectiveDefaultModeForMatchTemplate()
    {
        string source = Stylesheet("<xsl:template match='node()'/>", "xmlns:m='urn:modes' default-mode='m:view'");

        XsltTemplatePlan template = Plan(Compile(source)).Templates.Single();

        Assert.Multiple(() =>
        {
            Assert.That(template.IsNamed, Is.False);
            Assert.That(template.IsTemplateRule, Is.True);
            Assert.That(template.Modes.Single().LexicalName, Is.EqualTo("#default"));
            Assert.That(template.Modes.Single().Name, Is.EqualTo("Q{urn:modes}view"));
            Assert.That(template.Component, Is.Null);
        });
    }

    [Test]
    public void CompilesNestedSequenceConstructorStructure()
    {
        string source = Stylesheet(
            "<xsl:template match='/'>" +
            "<xsl:text>  kept  </xsl:text><xsl:variable name='value' select='1'/>" +
            "<result>{$value}<xsl:if test='$value'><inner code='{$value}'/></xsl:if></result>" +
            "</xsl:template>",
            "expand-text='yes'");

        XsltTemplatePlan template = Plan(Compile(source)).Templates.Single();
        XsltSequenceItemPlan text = template.Body.Items[0];
        XsltSequenceItemPlan variable = template.Body.Items[1];
        XsltSequenceItemPlan result = template.Body.Items[2];
        XsltSequenceItemPlan conditional = result.Content!.Items[1];
        XsltSequenceItemPlan inner = conditional.Content!.Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(template.Body.Items.Select(item => item.Kind), Is.EqualTo(new[]
            {
                XsltSequenceItemKind.Instruction,
                XsltSequenceItemKind.Variable,
                XsltSequenceItemKind.LiteralResultElement
            }));
            Assert.That(text.LocalName, Is.EqualTo("text"));
            Assert.That(text.Content!.Items.Single().Node.Value, Is.EqualTo("  kept  "));
            Assert.That(variable.Variable, Is.Not.Null);
            Assert.That(variable.Variable!.Name.LocalName, Is.EqualTo("value"));
            Assert.That(result.Content.Items[0].TextTemplate, Is.Not.Null);
            Assert.That(result.Content.Items[0].TextTemplate!.Parts.Any(part => part.ExpressionPlan != null), Is.True);
            Assert.That(conditional.Kind, Is.EqualTo(XsltSequenceItemKind.Instruction));
            Assert.That(inner.Kind, Is.EqualTo(XsltSequenceItemKind.LiteralResultElement));
            Assert.That(inner.AttributeTemplates.Single().Parts.Any(part => part.ExpressionPlan != null), Is.True);
        });
    }

    [Test]
    public void CreatesSyntheticTemplateForSimplifiedStylesheet()
    {
        string source = "<page xmlns:xsl='" + Xsl + "' xsl:version='3.0'><title>Welcome</title></page>";

        XsltTemplatePlan template = Plan(Compile(source)).Templates.Single();

        Assert.Multiple(() =>
        {
            Assert.That(template.IsSynthetic, Is.True);
            Assert.That(template.IsTemplateRule, Is.True);
            Assert.That(template.Match, Is.EqualTo("/"));
            Assert.That(template.Modes.Single().Name, Is.EqualTo("#unnamed"));
            Assert.That(template.Body.Items.Single().LocalName, Is.EqualTo("page"));
            Assert.That(template.Body.Items.Single().Content!.Items.Single().LocalName, Is.EqualTo("title"));
        });
    }

    [Test]
    public void AllowsAbstractNamedTemplateWithOnlyAHeader()
    {
        string source = Package("<xsl:template name='hook' visibility='abstract'><xsl:context-item use='optional'/><xsl:param name='value'/></xsl:template>");

        XsltTemplatePlan template = Plan(CompileLibrary(source)).Templates.Single();

        Assert.Multiple(() =>
        {
            Assert.That(template.IsAbstract, Is.True);
            Assert.That(template.Parameters, Has.Length.EqualTo(1));
            Assert.That(template.Body.Items, Is.Empty);
        });
    }

    [TestCase("<xsl:template/>", XsltErrorCodes.XTSE0500)]
    [TestCase("<xsl:template name='' match='/'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:template name='entry' mode='#unnamed'/>", XsltErrorCodes.XTSE0500)]
    [TestCase("<xsl:template match='/' visibility='private'/>", XsltErrorCodes.XTSE0500)]
    [TestCase("<xsl:template match='   '/>", XsltErrorCodes.XTSE0340)]
    [TestCase("<xsl:template match='/' priority='not-a-decimal'/>", XsltErrorCodes.XTSE0530)]
    [TestCase("<xsl:template match='/' mode='   '/>", XsltErrorCodes.XTSE0550)]
    [TestCase("<xsl:template match='/' mode='#all named'/>", XsltErrorCodes.XTSE0550)]
    [TestCase("<xsl:template match='/' mode='named named'/>", XsltErrorCodes.XTSE0550)]
    [TestCase("<xsl:template name='entry'><xsl:context-item use='absent' as='item()'/></xsl:template>", XsltErrorCodes.XTSE3089)]
    [TestCase("<xsl:template name='entry'><xsl:context-item use='sometimes'/></xsl:template>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:template match='/'><xsl:context-item use='optional'/></xsl:template>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:template name='entry'><xsl:param name='value'/><xsl:context-item/></xsl:template>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:template name='entry'><result/><xsl:param name='value'/></xsl:template>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:template name='entry'><xsl:template name='nested'/></xsl:template>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:template name='entry' as='xs:missing'/>", XPathErrorCodes.XPST0051)]
    public void ReportsInvalidTemplateDeclarations(string declaration, string expectedCode)
    {
        string namespaces = declaration.Contains("xs:") ? "xmlns:xs='http://www.w3.org/2001/XMLSchema'" : "";

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declaration, namespaces)))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
            Assert.That(error.Location, Is.Not.Null);
        });
    }

    [Test]
    public void RejectsAbstractTemplateWithASequenceConstructor()
    {
        string source = Package("<xsl:template name='hook' visibility='abstract'><result/></xsl:template>");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => CompileLibrary(source))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0020));
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static string Package(string content)
    {
        return "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:test' package-version='1' version='3.0'>" + content + "</xsl:package>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source)
    {
        XsltCompileOptions options = Options();
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, new XmlDefaultResolver());
    }

    private static XsltExecutable CompileLibrary(string source)
    {
        XsltCompileOptions options = Options();
        return new XsltStylesheetLoader(options, new XmlDefaultResolver(), new XsltPackageCompilationContext(), true).CompileXsltStylesheet(
            new XmlInput { Text = source, BaseUri = "https://example.test/main.xsl" });
    }

    private static XsltCompileOptions Options()
    {
        return new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Denied,
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
    }
}
