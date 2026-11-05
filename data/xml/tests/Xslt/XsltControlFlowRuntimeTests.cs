using xml;

namespace xml.Tests;

[TestFixture]
public class XsltControlFlowRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void EvaluatesIfAndTheFirstMatchingChooseBranch()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:if test='true()'><xsl:sequence select='1'/></xsl:if>" +
            "<xsl:if test='false()'><xsl:sequence select='99'/></xsl:if>" +
            "<xsl:choose><xsl:when test='false()'><xsl:sequence select='98'/></xsl:when>" +
            "<xsl:when test='2 eq 2'><xsl:sequence select='2'/></xsl:when>" +
            "<xsl:otherwise><xsl:sequence select='97'/></xsl:otherwise></xsl:choose>" +
            "</xsl:template>");

        XdmSequence value = Transform(source);

        Assert.That(value.AtomicValues!.Select(item => item.IntegerValue), Is.EqualTo(new long[] { 1, 2 }));
    }

    [Test]
    public void ForEachEstablishesFocusAndAnIndependentLocalVariableFrame()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each select='2 to 4'>" +
            "<xsl:variable name='doubled' select='. * 2'/>" +
            "<xsl:sequence select=\"concat(position(), '/', last(), ':', $doubled)\"/>" +
            "</xsl:for-each></xsl:template>");

        XdmSequence value = Transform(source);

        Assert.That(value.AtomicValues!.Select(item => item.StringValue), Is.EqualTo(new[] { "1/3:4", "2/3:6", "3/3:8" }));
    }

    [Test]
    public void IterateCarriesParametersAndRunsCompletionWithTheFinalValues()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:iterate select='1 to 3'>" +
            "<xsl:param name='sum' select='0'/>" +
            "<xsl:on-completion select='$sum'/>" +
            "<xsl:sequence select='$sum + .'/>" +
            "<xsl:next-iteration><xsl:with-param name='sum'><xsl:sequence select='$sum + .'/></xsl:with-param></xsl:next-iteration>" +
            "</xsl:iterate></xsl:template>");

        XdmSequence value = Transform(source);

        Assert.That(value.AtomicValues!.Select(item => item.IntegerValue), Is.EqualTo(new long[] { 1, 3, 6, 6 }));
    }

    [Test]
    public void BreakStopsTheInnermostIterationAndSuppressesCompletion()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:iterate select='1 to 4'>" +
            "<xsl:param name='sum' select='0'/>" +
            "<xsl:on-completion select='99'/>" +
            "<xsl:if test='. eq 2'><xsl:break select='$sum + .'/></xsl:if>" +
            "<xsl:sequence select='$sum + .'/>" +
            "<xsl:next-iteration><xsl:with-param name='sum' select='$sum + .'/></xsl:next-iteration>" +
            "</xsl:iterate></xsl:template>");

        XdmSequence value = Transform(source);

        Assert.That(value.AtomicValues!.Select(item => item.IntegerValue), Is.EqualTo(new long[] { 1, 3 }));
    }

    [Test]
    public void CompletionUsesInitialParametersWhenTheIterationIsEmpty()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:iterate select='()'>" +
            "<xsl:param name='value' select=\"'initial'\"/>" +
            "<xsl:on-completion select='$value'/>" +
            "</xsl:iterate></xsl:template>");

        XdmSequence value = Transform(source);

        Assert.That(value.AtomicValues!.Single().StringValue, Is.EqualTo("initial"));
    }

    [Test]
    public void NestedBreakIsConsumedByTheInnermostIteration()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:iterate select='1'>" +
            "<xsl:iterate select='1 to 3'><xsl:choose>" +
            "<xsl:when test='. eq 2'><xsl:break select=\"'inner-break'\"/></xsl:when>" +
            "<xsl:otherwise><xsl:sequence select=\"'inner-step'\"/></xsl:otherwise>" +
            "</xsl:choose></xsl:iterate>" +
            "<xsl:sequence select=\"'outer-continues'\"/>" +
            "</xsl:iterate></xsl:template>");

        XdmSequence value = Transform(source);

        Assert.That(value.AtomicValues!.Select(item => item.StringValue), Is.EqualTo(new[] { "inner-step", "inner-break", "outer-continues" }));
    }

    [Test]
    public void ConditionalOutputUsesVacuityAndPreservesSiblingOrder()
    {
        string emptySource = Stylesheet(
            "<xsl:template name='entry'><xsl:sequence select=\"''\"/>" +
            "<xsl:on-non-empty select=\"'wrong'\"/><xsl:on-empty select=\"'empty'\"/>" +
            "</xsl:template>");
        string populatedSource = Stylesheet(
            "<xsl:template name='entry'><xsl:sequence select=\"''\"/>" +
            "<xsl:on-non-empty select=\"'middle'\"/><item/><xsl:on-empty select=\"'wrong'\"/>" +
            "</xsl:template>");

        XdmSequence empty = Transform(emptySource);
        XdmSequence populated = Transform(populatedSource);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Count, Is.EqualTo(1));
            Assert.That(empty.AtomicValues!.Single().StringValue, Is.EqualTo("empty"));
            Assert.That(populated.Count, Is.EqualTo(3));
            Assert.That(populated.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic, XdmItemKind.Atomic, XdmItemKind.Node }));
            Assert.That(populated.AtomicValues!.Select(item => item.StringValue), Is.EqualTo(new[] { "", "middle" }));
            Assert.That(populated.Nodes!.Single().Name!.LocalName, Is.EqualTo("item"));
        });
    }

    [Test]
    public void WherePopulatedRemovesDeemedEmptyItems()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:where-populated>" +
            "<empty/><full>text</full><xsl:sequence select=\"('', map{}, [()])\"/>" +
            "<xsl:sequence select=\"('kept', ['member'])\"/>" +
            "</xsl:where-populated></xsl:template>");

        XdmSequence value = Transform(source);

        Assert.Multiple(() =>
        {
            Assert.That(value.Count, Is.EqualTo(3));
            Assert.That(value.Nodes!.Single().Name!.LocalName, Is.EqualTo("full"));
            Assert.That(value.AtomicValues!.Single().StringValue, Is.EqualTo("kept"));
            Assert.That(value.Arrays!.Single().Count, Is.EqualTo(1));
        });
    }

    private static XdmSequence Transform(string source)
    {
        XsltCompileOptions compileOptions = new XsltCompileOptions
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
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, compileOptions, new XmlDefaultResolver());
        XsltTransformOptions transformOptions = new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } };
        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(executable, transformOptions);
        return state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
    }

    private static string Stylesheet(string content)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" + content + "</xsl:stylesheet>";
    }
}
