using xml;

namespace xml.Tests;

[TestFixture]
public class XsltAccumulatorRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void BuildsValuesLazilyAndUsesLastMatchingRulesForStartAndEndSnapshots()
    {
        string stylesheet = Stylesheet(
            "<xsl:accumulator name='a:count' as='xs:integer' initial-value='0' streamable='yes'>" +
            "<xsl:accumulator-rule match='item' select='$value + 1'/>" +
            "<xsl:accumulator-rule match='item' select='$value + 2'/>" +
            "<xsl:accumulator-rule match='item' phase='end' select='$value + 10'/>" +
            "</xsl:accumulator>" +
            "<xsl:template name='entry'><xsl:for-each select='/root/item'>" +
            "<xsl:sequence select=\"concat(accumulator-before('a:count'), ':', accumulator-after('a:count'))\"/>" +
            "</xsl:for-each></xsl:template>",
            "xmlns:a='urn:acc' xmlns:xs='" + Xs + "'");
        XsltTransformationState state = InitializeNamed(stylesheet, "<root><item/><item/></root>");

        Assert.That(state.AccumulatorRuntime!.Evaluations, Is.Empty);
        XdmSequence result = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(result), Is.EqualTo(new[] { "2:12", "14:24" }));
            Assert.That(state.AccumulatorRuntime.Evaluations, Has.Count.EqualTo(1));
            Assert.That(state.AccumulatorRuntime.Evaluations.Single().Status, Is.EqualTo(XsltAccumulatorEvaluationStatus.Ready));
        });
    }

    [Test]
    public void EvaluatesSequenceConstructorRulesWithValueAndAccumulatorDependencies()
    {
        string stylesheet = Stylesheet(
            "<xsl:accumulator name='base' as='xs:integer' initial-value='0'>" +
            "<xsl:accumulator-rule match='item' select='$value + 2'/>" +
            "</xsl:accumulator>" +
            "<xsl:accumulator name='dependent' initial-value='0'>" +
            "<xsl:accumulator-rule match='item'><xsl:sequence select=\"$value + accumulator-before('base')\"/></xsl:accumulator-rule>" +
            "</xsl:accumulator>" +
            "<xsl:template name='entry'><xsl:for-each select='/root/item'>" +
            "<xsl:sequence select=\"accumulator-before('dependent')\"/>" +
            "</xsl:for-each></xsl:template>",
            "xmlns:xs='" + Xs + "'");

        XdmSequence result = TransformNamed(stylesheet, "<root><item/><item/></root>");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "2", "6" }));
    }

    [Test]
    public void PreservesNodeValuedAccumulatorResultsAndTreatsPatternFailuresAsNonMatches()
    {
        string stylesheet = Stylesheet(
            "<xsl:accumulator name='nodes' as='node()' initial-value='.'>" +
            "<xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator>" +
            "<xsl:accumulator name='safe' as='xs:integer' initial-value='7'>" +
            "<xsl:accumulator-rule match='item[1 div 0]' select='99'/></xsl:accumulator>" +
            "<xsl:template name='entry'><xsl:sequence select=\"accumulator-before('nodes') is .\"/>" +
            "<xsl:for-each select='/root/item'><xsl:sequence select=\"accumulator-after('safe')\"/></xsl:for-each>" +
            "</xsl:template>",
            "xmlns:xs='" + Xs + "'");

        XdmSequence result = TransformNamed(stylesheet, "<root><item/></root>");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "true", "7" }));
    }

    [Test]
    public void AppliesOnlyAccumulatorsEnabledForTheInitialMode()
    {
        string enabled = Stylesheet(
            "<xsl:accumulator name='count' as='xs:integer' initial-value='0' streamable='yes'>" +
            "<xsl:accumulator-rule match='item' select='$value + 1'/></xsl:accumulator>" +
            "<xsl:mode visibility='private' use-accumulators='count'/>" +
            "<xsl:template match='item'><xsl:sequence select=\"accumulator-before('count')\"/></xsl:template>",
            "xmlns:xs='" + Xs + "'");
        string disabled = Stylesheet(
            "<xsl:accumulator name='count' as='xs:integer' initial-value='0' streamable='yes'>" +
            "<xsl:accumulator-rule match='item' select='$value + 1'/></xsl:accumulator>" +
            "<xsl:template match='item'><xsl:sequence select=\"accumulator-before('count')\"/></xsl:template>",
            "xmlns:xs='" + Xs + "'");

        XdmSequence result = TransformApply(enabled, "<root><item/><item/></root>");
        XsltDynamicException error = Assert.Throws<XsltDynamicException>(() => TransformApply(disabled, "<root><item/></root>"))!;

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(result), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE3362));
        });
    }

    [Test]
    public void PreservesAccumulatorValuesForCopiedNodesWhenRequested()
    {
        string stylesheet = Stylesheet(
            "<xsl:accumulator name='count' as='xs:integer' initial-value='0'>" +
            "<xsl:accumulator-rule match='item' select='$value + 1'/></xsl:accumulator>" +
            "<xsl:template name='entry'>" +
            "<xsl:variable name='copy' as='node()'><xsl:copy-of select='/root' copy-accumulators='yes'/></xsl:variable>" +
            "<xsl:for-each select='$copy/item[2]'><xsl:sequence select=\"accumulator-before('count')\"/></xsl:for-each>" +
            "</xsl:template>",
            "xmlns:xs='" + Xs + "'");
        XsltTransformationState state = InitializeNamed(stylesheet, "<root><item/><item/></root>");

        XdmSequence result = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(result), Is.EqualTo(new[] { "2" }));
            Assert.That(state.AccumulatorCopies, Is.Not.Empty);
        });
    }

    [Test]
    public void ReportsNameContextAndContextItemErrors()
    {
        string unknown = Stylesheet("<xsl:template name='entry'><xsl:sequence select=\"accumulator-before('missing')\"/></xsl:template>", "");
        string noContext = Stylesheet(
            "<xsl:accumulator name='known' initial-value='0'><xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator>" +
            "<xsl:template name='entry'><xsl:sequence select=\"accumulator-before('known')\"/></xsl:template>",
            "");
        string atomicContext = Stylesheet(
            "<xsl:accumulator name='known' initial-value='0'><xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator>" +
            "<xsl:template name='entry'><xsl:for-each select='1'><xsl:sequence select=\"accumulator-before('known')\"/></xsl:for-each></xsl:template>",
            "");

        XsltDynamicException unknownError = Assert.Throws<XsltDynamicException>(() => TransformNamed(unknown, "<root/>"))!;
        XsltDynamicException contextError = Assert.Throws<XsltDynamicException>(() => TransformNamed(noContext, null))!;
        XsltTypeException itemError = Assert.Throws<XsltTypeException>(() => TransformNamed(atomicContext, "<root/>"))!;

        Assert.Multiple(() =>
        {
            Assert.That(unknownError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE3340));
            Assert.That(contextError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE3350));
            Assert.That(itemError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE3360));
        });
    }

    [Test]
    public void DefersRuleFailuresUntilAValueThatDependsOnTheFailureIsRequested()
    {
        string declaration =
            "<xsl:accumulator name='bad' as='xs:integer' initial-value='0'>" +
            "<xsl:accumulator-rule match='item' select=\"'not-an-integer'\"/></xsl:accumulator>";
        string early = Stylesheet(
            declaration + "<xsl:template name='entry'><xsl:sequence select=\"accumulator-before('bad')\"/></xsl:template>",
            "xmlns:xs='" + Xs + "'");
        string affected = Stylesheet(
            declaration + "<xsl:template name='entry'><xsl:for-each select='/root/item'><xsl:sequence select=\"accumulator-before('bad')\"/></xsl:for-each></xsl:template>",
            "xmlns:xs='" + Xs + "'");

        XdmSequence result = TransformNamed(early, "<root><item/></root>");
        XPathTypeException error = Assert.Throws<XPathTypeException>(() => TransformNamed(affected, "<root><item/></root>"))!;

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(result), Is.EqualTo(new[] { "0" }));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void DetectsDynamicAccumulatorDependencyCycles()
    {
        string stylesheet = Stylesheet(
            "<xsl:accumulator name='a' initial-value='0'>" +
            "<xsl:accumulator-rule match='item' select=\"accumulator-before(concat('a', ''))\"/>" +
            "</xsl:accumulator>" +
            "<xsl:template name='entry'><xsl:for-each select='/root/item'><xsl:sequence select=\"accumulator-before('a')\"/></xsl:for-each></xsl:template>",
            "");

        XsltDynamicException error = Assert.Throws<XsltDynamicException>(() => TransformNamed(stylesheet, "<root><item/></root>"))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE3400));
    }

    [Test]
    public void NamedAccumulatorFunctionReferencesRetainTheirCreationContext()
    {
        string stylesheet = Stylesheet(
            "<xsl:accumulator name='count' as='xs:integer' initial-value='0'>" +
            "<xsl:accumulator-rule match='item' select='$value + 1'/></xsl:accumulator>" +
            "<xsl:template name='entry'>" +
            "<xsl:variable name='lookup' as='function(*)' select='accumulator-before#1'/>" +
            "<xsl:for-each select='/root/item[2]'><xsl:sequence select=\"$lookup('count')\"/></xsl:for-each>" +
            "</xsl:template>",
            "xmlns:xs='" + Xs + "'");

        XdmSequence result = TransformNamed(stylesheet, "<root><item/><item/></root>");

        Assert.That(StringValues(result), Is.EqualTo(new[] { "0" }));
    }

    private static string[] StringValues(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "").ToArray();
    }

    private static XdmSequence TransformNamed(string stylesheet, string? document)
    {
        XsltTransformationState state = InitializeNamed(stylesheet, document);
        return state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
    }

    private static XsltTransformationState InitializeNamed(string stylesheet, string? document)
    {
        XsltTransformOptions options = new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } };
        if (document != null)
        {
            XmlDocument parsed = Parse(document);
            options.GlobalContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(parsed.DocumentNode!);
        }

        return new XsltTransformationEngine().InitializeXsltTransformation(Compile(stylesheet), options);
    }

    private static XdmSequence TransformApply(string stylesheet, string document)
    {
        XsltTransformOptions options = new XsltTransformOptions { SourceDocument = Parse(document) };
        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(Compile(stylesheet), options);
        return state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
    }

    private static XmlDocument Parse(string document)
    {
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = document }, new XmlParserOptions(), null, null, null);
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

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }
}
