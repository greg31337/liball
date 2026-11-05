using xml;

namespace xml.Tests;

[TestFixture]
public class XsltInstructionCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void CompilesCoreControlInstructionsExpressionsTypesAndNames()
    {
        string source = Stylesheet(
            "<xsl:param name='items' select='1 to 3'/><xsl:param name='p' select=\"'value'\"/>" +
            "<xsl:template name='helper'><xsl:param name='p'/><xsl:sequence select='$p'/></xsl:template>" +
            "<xsl:template match='item'>" +
            "<xsl:apply-templates select='$items'><xsl:sort select='.'/><xsl:with-param name='p' select='$p' as='xs:string'/></xsl:apply-templates>" +
            "<xsl:call-template name='helper'><xsl:with-param name='p' select='$p'/></xsl:call-template>" +
            "<xsl:if test='exists($items)'><xsl:value-of select='$p'/></xsl:if>" +
            "<xsl:choose><xsl:when test='empty($items)'><xsl:text>empty</xsl:text></xsl:when><xsl:otherwise><xsl:sequence select='$items'/></xsl:otherwise></xsl:choose>" +
            "<xsl:for-each select='$items'><xsl:sort select='.'/><xsl:sequence select='.'/></xsl:for-each>" +
            "<xsl:perform-sort select='$items'><xsl:sort select='.'/></xsl:perform-sort>" +
            "<xsl:next-match><xsl:with-param name='p' select='$p'/></xsl:next-match>" +
            "<xsl:apply-imports><xsl:with-param name='p' select='$p'/></xsl:apply-imports>" +
            "</xsl:template>",
            "xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));
        XsltInstructionPlan apply = Instruction(stylesheet, XsltInstructionKind.ApplyTemplates);
        XsltInstructionPlan parameter = stylesheet.Instructions.First(item => item.Kind == XsltInstructionKind.WithParam && item.Attributes.Any(attribute => attribute.SequenceType != null));
        XsltInstructionPlan call = Instruction(stylesheet, XsltInstructionKind.CallTemplate);

        Assert.Multiple(() =>
        {
            Assert.That(apply.Attributes.Single(attribute => attribute.Name == "select").Expression, Is.Not.Null);
            Assert.That(parameter.Attributes.Single(attribute => attribute.Name == "as").SequenceType!.AtomicType!.Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(parameter.Attributes.Single(attribute => attribute.Name == "name").ResolvedName!.LocalName, Is.EqualTo("p"));
            Assert.That(call.Attributes.Single(attribute => attribute.Name == "name").ResolvedName!.LocalName, Is.EqualTo("helper"));
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.When && item.IsHelper), Is.True);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.Otherwise && item.IsHelper), Is.True);
            Assert.That(stylesheet.Instructions.Count(item => item.Kind == XsltInstructionKind.Sort), Is.EqualTo(3));
            Assert.That(stylesheet.Instructions.Where(item => item.Kind == XsltInstructionKind.Sort).All(item => item.Attributes.Single(attribute => attribute.Name == "select").Expression != null), Is.True);
            Assert.That(stylesheet.Templates.Single(item => item.Name == "Q{}helper").Body.Items.Single().Instruction, Is.Not.Null);
        });
    }

    [Test]
    public void CompilesNodeMapAndArrayConstructionInstructions()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:document><xsl:element name='item'><xsl:attribute name='code' select=\"'v'\"/>" +
            "<xsl:namespace name='p' select=\"'urn:p'\"/><xsl:comment select=\"'note'\"/>" +
            "<xsl:processing-instruction name='go' select=\"'now'\"/></xsl:element></xsl:document>" +
            "<xsl:copy select='.'><xsl:value-of select='.'/></xsl:copy><xsl:copy-of select='.'/>" +
            "<xsl:map><xsl:map-entry key=\"'key'\" select='1'/></xsl:map>" +
            "<xsl:array><xsl:array-member select='1'/><xsl:sequence select='2'/></xsl:array>" +
            "</xsl:template>",
            "");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));

        Assert.Multiple(() =>
        {
            Assert.That(Instruction(stylesheet, XsltInstructionKind.Attribute).Attributes.Single(attribute => attribute.Name == "select").Expression, Is.Not.Null);
            Assert.That(Instruction(stylesheet, XsltInstructionKind.MapEntry).Attributes.Single(attribute => attribute.Name == "key").Expression, Is.Not.Null);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.Array), Is.True);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.ArrayMember && item.IsHelper), Is.True);
            Assert.That(stylesheet.Instructions.Select(item => item.Kind), Does.Contain(XsltInstructionKind.ProcessingInstruction));
            Assert.That(stylesheet.Instructions.Select(item => item.Kind), Does.Contain(XsltInstructionKind.CopyOf));
        });
    }

    [Test]
    public void CompilesIterationGroupingRegexTryAndConditionalOutputHelpers()
    {
        string source = Stylesheet(
            "<xsl:param name='items' select='1 to 3'/>" +
            "<xsl:template name='entry'>" +
            "<xsl:iterate select='$items'><xsl:param name='state' select='0'/><xsl:on-completion><xsl:sequence select='$state'/></xsl:on-completion>" +
            "<xsl:next-iteration><xsl:with-param name='state' select='$state + 1'/></xsl:next-iteration><xsl:break select='$state'/></xsl:iterate>" +
            "<xsl:for-each-group select='$items' group-starting-with='item'><xsl:sequence select='current-group()'/></xsl:for-each-group>" +
            "<xsl:analyze-string select=\"'abc'\" regex='a'><xsl:matching-substring><xsl:sequence select='.'/></xsl:matching-substring>" +
            "<xsl:non-matching-substring><xsl:sequence select='.'/></xsl:non-matching-substring></xsl:analyze-string>" +
            "<xsl:try><xsl:sequence select='1'/><xsl:catch errors='*'><xsl:sequence select='$err:code'/></xsl:catch></xsl:try>" +
            "<xsl:where-populated><xsl:on-empty><xsl:text>empty</xsl:text></xsl:on-empty><xsl:on-non-empty><xsl:text>full</xsl:text></xsl:on-non-empty></xsl:where-populated>" +
            "</xsl:template>",
            "xmlns:err='http://www.w3.org/2005/xqt-errors'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));
        XsltInstructionPlan grouping = Instruction(stylesheet, XsltInstructionKind.ForEachGroup);

        Assert.Multiple(() =>
        {
            Assert.That(grouping.Attributes.Single(attribute => attribute.Name == "group-starting-with").Pattern!.IsDeferred, Is.False);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.OnCompletion && item.IsHelper), Is.True);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.Catch && item.IsHelper), Is.True);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.MatchingSubstring && item.IsHelper), Is.True);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.NonMatchingSubstring && item.IsHelper), Is.True);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.OnEmpty), Is.True);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.OnNonEmpty), Is.True);
        });
    }

    [Test]
    public void CompilesAdvancedEvaluationMergeAndDestinationInstructions()
    {
        string source = Stylesheet(
            "<xsl:param name='items' select='1 to 3'/>" +
            "<xsl:template name='entry'>" +
            "<xsl:evaluate xpath=\"'1 + 1'\" as='item()*' context-item='.'/>" +
            "<xsl:result-document href='result.xml'><xsl:sequence select='$items'/></xsl:result-document>" +
            "<xsl:source-document href='source.xml'><xsl:sequence select='.'/></xsl:source-document>" +
            "<xsl:fork><xsl:sequence select='1'/><xsl:sequence select='2'/></xsl:fork>" +
            "<xsl:merge><xsl:merge-source name='source' select='$items'><xsl:merge-key select='.'/></xsl:merge-source>" +
            "<xsl:merge-action><xsl:sequence select='current-merge-group()'/></xsl:merge-action></xsl:merge>" +
            "<xsl:assert test='exists($items)' select=\"'missing'\"/><xsl:message select=\"'done'\"/>" +
            "<xsl:number select='.' count='item' from='root'/>" +
            "</xsl:template>",
            "");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));
        XsltInstructionPlan evaluate = Instruction(stylesheet, XsltInstructionKind.Evaluate);
        XsltInstructionPlan mergeSource = Instruction(stylesheet, XsltInstructionKind.MergeSource);
        XsltInstructionPlan number = Instruction(stylesheet, XsltInstructionKind.Number);

        Assert.Multiple(() =>
        {
            Assert.That(evaluate.Attributes.Single(attribute => attribute.Name == "xpath").Expression, Is.Not.Null);
            Assert.That(evaluate.Attributes.Single(attribute => attribute.Name == "as").SequenceType, Is.Not.Null);
            Assert.That(mergeSource.Attributes.Single(attribute => attribute.Name == "select").Expression, Is.Not.Null);
            Assert.That(number.Attributes.Single(attribute => attribute.Name == "count").Pattern, Is.Not.Null);
            Assert.That(number.Attributes.Single(attribute => attribute.Name == "from").Pattern, Is.Not.Null);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.MergeAction && item.IsHelper), Is.True);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.ResultDocument), Is.True);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.SourceDocument), Is.True);
            Assert.That(stylesheet.Instructions.Any(item => item.Kind == XsltInstructionKind.Fork), Is.True);
        });
    }

    [Test]
    public void RetainsForwardCompatibleUnknownInstructionsAndFallbackPlans()
    {
        string source = Stylesheet("<xsl:template name='entry'><xsl:future version='4.0'><xsl:fallback><xsl:sequence select='1'/></xsl:fallback></xsl:future></xsl:template>", "");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));

        Assert.Multiple(() =>
        {
            Assert.That(Instruction(stylesheet, XsltInstructionKind.Unknown).IsForwardCompatible, Is.True);
            Assert.That(Instruction(stylesheet, XsltInstructionKind.Fallback).IsHelper, Is.False);
            Assert.That(stylesheet.Fallbacks, Has.Length.EqualTo(1));
        });
    }

    [TestCase("<xsl:if/>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:sequence select='1'><xsl:text>content</xsl:text></xsl:sequence>", XsltErrorCodes.XTSE3185)]
    [TestCase("<xsl:value-of select='1'>content</xsl:value-of>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:if test='true()' unexpected='value'/>", XsltErrorCodes.XTSE0090)]
    [TestCase("<xsl:choose><xsl:otherwise/></xsl:choose>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:try><xsl:sequence select='1'/></xsl:try>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:map><xsl:sequence select='1'/></xsl:map>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:for-each-group select='.' group-by='.' group-adjacent='.'/>", XsltErrorCodes.XTSE1080)]
    [TestCase("<xsl:document validation='strict'/>", XsltErrorCodes.XTSE1660)]
    [TestCase("<xsl:copy validation='strip' type='xs:string' xmlns:xs='http://www.w3.org/2001/XMLSchema'/>", XsltErrorCodes.XTSE1505)]
    [TestCase("<xsl:call-template name='entry'><xsl:with-param name='p'/><xsl:with-param name='p'/></xsl:call-template>", XsltErrorCodes.XTSE0670)]
    public void RejectsInvalidInstructionAttributesAndStructure(string instruction, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet("<xsl:template name='entry'>" + instruction + "</xsl:template>", "")))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [TestCase("<xsl:when test='true()'/>")]
    [TestCase("<xsl:catch errors='*'/>")]
    [TestCase("<xsl:map-entry key='1'/>")]
    [TestCase("<xsl:array-member/>")]
    [TestCase("<xsl:matching-substring/>")]
    [TestCase("<xsl:merge-key select='.'/>")]
    [TestCase("<xsl:sort/>")]
    [TestCase("<xsl:with-param name='p'/>")]
    [TestCase("<xsl:on-completion/>")]
    [TestCase("<xsl:fallback/>")]
    public void RejectsHelperElementsOutsideTheirRequiredParent(string helper)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet("<xsl:template name='entry'>" + helper + "</xsl:template>", "")))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0010));
    }

    private static XsltInstructionPlan Instruction(XsltStylesheetPlan stylesheet, XsltInstructionKind kind)
    {
        return stylesheet.Instructions.First(item => item.Kind == kind);
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source)
    {
        XsltCompileOptions options = new XsltCompileOptions
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
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, new XmlDefaultResolver());
    }
}
