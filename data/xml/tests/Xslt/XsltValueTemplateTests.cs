using xml;

namespace xml.Tests;

[TestFixture]
public class XsltValueTemplateTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void CompilesAndEvaluatesEscapedBracesNestedXPathQuotesAndComments()
    {
        string source = Stylesheet("<xsl:template name='entry' expand-text='yes'><out " +
            "data=\"L{{{map {'k': (1, 2)}?k}}}R\" quoted=\"{'}'}\" comment=\"{(: } (: nested :) :) 3}\">A{{{1 to 3}}}B</out></xsl:template>", "");

        XsltStylesheetPlan plan = Plan(Compile(source));
        XsltValueTemplate data = AttributeTemplate(plan, "data");
        XsltValueTemplate quoted = AttributeTemplate(plan, "quoted");
        XsltValueTemplate comment = AttributeTemplate(plan, "comment");
        XsltValueTemplate text = plan.ValueTemplates.Single(template => template.Kind == XsltValueTemplateKind.Text && template.Source.Contains("1 to 3"));
        XsltValueTemplateEvaluator evaluator = new XsltValueTemplateEvaluator();

        Assert.Multiple(() =>
        {
            Assert.That(evaluator.EvaluateXsltValueTemplate(data, new XPathDynamicContext()), Is.EqualTo("L{1 2}R"));
            Assert.That(evaluator.EvaluateXsltValueTemplate(quoted, new XPathDynamicContext()), Is.EqualTo("}"));
            Assert.That(evaluator.EvaluateXsltValueTemplate(comment, new XPathDynamicContext()), Is.EqualTo("3"));
            Assert.That(evaluator.EvaluateXsltValueTemplate(text, new XPathDynamicContext()), Is.EqualTo("A{1 2 3}B"));
            Assert.That(data.Parts.Count(part => part.Kind == XsltValueTemplatePartKind.Expression), Is.EqualTo(1));
            Assert.That(data.Parts.Single(part => part.Kind == XsltValueTemplatePartKind.Expression).ExpressionPlan, Is.Not.Null);
        });
    }

    [Test]
    public void CapturesNamespaceDefaultNamespaceBaseUriAndStaticVariables()
    {
        string source = Stylesheet("<xsl:variable name='n' static='yes' select='4'/>" +
            "<xsl:template name='entry'><out xmlns:p='urn:prefixed' data='{$n + count(p:item) + count(item)}'/></xsl:template>",
            "xpath-default-namespace='urn:default'");

        XsltValueTemplate template = AttributeTemplate(Plan(Compile(source)), "data");
        XPathPlan expression = template.Parts.Single(part => part.Kind == XsltValueTemplatePartKind.Expression).ExpressionPlan!;

        Assert.Multiple(() =>
        {
            Assert.That(expression.Environment.DefaultElementNamespace, Is.EqualTo("urn:default"));
            Assert.That(expression.Environment.Namespaces.Single(binding => binding.Prefix == "p").NamespaceUri, Is.EqualTo("urn:prefixed"));
            Assert.That(expression.Environment.BaseUri, Is.EqualTo("https://example.test/main.xsl"));
            Assert.That(expression.Environment.Variables.Single().Name!.LocalName, Is.EqualTo("n"));
        });
    }

    [Test]
    public void ParsesOnlyDesignatedXsltAttributesAndAllLiteralResultAttributes()
    {
        string source = Stylesheet("<xsl:template name='entry'><xsl:value-of select=\"'{not-a-template}'\" separator='[{1}]'/>" +
            "<xsl:element name='item-{2}'/><out plain='{3}' xsl:expand-text='no'/></xsl:template>", "");

        XsltStylesheetPlan plan = Plan(Compile(source));

        Assert.Multiple(() =>
        {
            Assert.That(plan.ValueTemplates.Any(template => template.Node.Name!.LocalName == "select"), Is.False);
            Assert.That(plan.ValueTemplates.Any(template => template.Node.Name!.LocalName == "separator"), Is.True);
            Assert.That(plan.ValueTemplates.Any(template => template.Node.Name!.LocalName == "name" && template.Node.Parent!.Name!.LocalName == "element"), Is.True);
            Assert.That(plan.ValueTemplates.Any(template => template.Node.Name!.LocalName == "plain"), Is.True);
            Assert.That(plan.ValueTemplates.Any(template => template.Node.Name!.LocalName == "expand-text"), Is.False);
        });
    }

    [Test]
    public void HonorsInheritedAndOverriddenExpandTextForTextNodes()
    {
        string source = Stylesheet("<xsl:template name='entry'><outer>A{1}<inner xsl:expand-text='no'>B{2}</inner>C{3}</outer></xsl:template>", "expand-text='yes'");

        XsltValueTemplate[] textTemplates = Plan(Compile(source)).ValueTemplates.Where(template => template.Kind == XsltValueTemplateKind.Text).ToArray();

        Assert.That(textTemplates.Select(template => template.Source), Is.EqualTo(new[] { "A{1}", "C{3}" }));
    }

    [Test]
    public void TreatsWhitespaceAndNestedCommentsAsAnEmptyExpression()
    {
        string source = Stylesheet("<xsl:template name='entry'><out data='A{ (: outer (: nested :) :) }B'/></xsl:template>", "");

        XsltValueTemplate template = AttributeTemplate(Plan(Compile(source)), "data");
        XsltValueTemplatePart expression = template.Parts.Single(part => part.Kind == XsltValueTemplatePartKind.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(expression.Value, Is.Empty);
            Assert.That(expression.ExpressionPlan, Is.Null);
            Assert.That(new XsltValueTemplateEvaluator().EvaluateXsltValueTemplate(template, new XPathDynamicContext()), Is.EqualTo("AB"));
        });
    }

    [Test]
    public void UsesFirstItemForBackwardsCompatibleAttributeTemplates()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='1.0'><xsl:template name='entry'><out data='{1 to 3}'/></xsl:template></xsl:stylesheet>";

        XsltValueTemplate template = AttributeTemplate(Plan(Compile(source)), "data");

        Assert.That(new XsltValueTemplateEvaluator().EvaluateXsltValueTemplate(template, new XPathDynamicContext()), Is.EqualTo("1"));
    }

    [Test]
    public void JoinsEveryItemForXslt20AttributeTemplates()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='2.0'><xsl:template name='entry'><out data='{1 to 3}'/></xsl:template></xsl:stylesheet>";

        XsltValueTemplate template = AttributeTemplate(Plan(Compile(source)), "data");

        Assert.That(new XsltValueTemplateEvaluator().EvaluateXsltValueTemplate(template, new XPathDynamicContext()), Is.EqualTo("1 2 3"));
    }

    [Test]
    public void RecordsOrderedHostSourceRangesForEveryPart()
    {
        XsltValueTemplate template = AttributeTemplate(Plan(Compile(Stylesheet("<xsl:template name='entry'><out data='left-{1}-right'/></xsl:template>", ""))), "data");

        Assert.Multiple(() =>
        {
            Assert.That(template.Parts, Has.Length.EqualTo(3));
            Assert.That(template.Parts.All(part => part.Range.Start != null && part.Range.End != null), Is.True);
            Assert.That(template.Parts[0].Range.Start!.SourceUri, Is.EqualTo("https://example.test/main.xsl"));
            Assert.That(template.Parts[0].Range.End!.CharacterOffset, Is.LessThanOrEqualTo(template.Parts[1].Range.Start!.CharacterOffset));
            Assert.That(template.Parts[1].Range.End!.CharacterOffset, Is.LessThanOrEqualTo(template.Parts[2].Range.Start!.CharacterOffset));
        });
    }

    [TestCase("{1", XsltErrorCodes.XTSE0350)]
    [TestCase("1}", XsltErrorCodes.XTSE0370)]
    [TestCase("{1 +}", XPathErrorCodes.XPST0003)]
    [TestCase("{missing:item}", XPathErrorCodes.XPST0081)]
    public void ReportsBraceAndXPathErrorsAtTheTemplatePart(string value, string expectedCode)
    {
        string source = Stylesheet("<xsl:template name='entry'><out data='" + value + "'/></xsl:template>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
            Assert.That(error.Diagnostic.Range, Is.Not.Null);
            Assert.That(error.Location!.SourceUri, Is.EqualTo("https://example.test/main.xsl"));
        });
    }

    private static XsltValueTemplate AttributeTemplate(XsltStylesheetPlan plan, string localName)
    {
        return plan.ValueTemplates.Single(template => template.Kind == XsltValueTemplateKind.Attribute && template.Node.Name!.LocalName == localName);
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0' " + attributes + ">" + content + "</xsl:stylesheet>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl"
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, new XmlDefaultResolver());
    }
}
