using xml;

namespace xml.Tests;

[TestFixture]
public class XsltNumberRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void NumbersNodesAtSingleMultipleAndAnyLevels()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:number select='/*/chapter[2]/section/item[2]' count='item'/>" +
            "<xsl:number select='/*/chapter[2]/section/item[2]' level='multiple' count='chapter|section|item' format='1.1.1'/>" +
            "<xsl:number select='/*/chapter[2]/section/item[2]' level='any' count='item' from='chapter'/>" +
            "</xsl:template>");
        string document = "<book><chapter><section><item/></section></chapter><chapter><section><item/><item/></section></chapter></book>";

        XdmSequence result = Transform(source, document);

        Assert.That(TextValues(result), Is.EqualTo(new[] { "2", "2.1.2", "2" }));
    }

    [Test]
    public void UsesDefaultCountPatternAndHonorsFromBoundaries()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:number select='/*/section[2]'/>" +
            "<xsl:number select='/*/chapter[2]/box/note[2]' level='any' count='note' from='chapter'/>" +
            "<xsl:number select='/*/chapter[2]/box/note[2]' count='note' from='missing'/>" +
            "<xsl:number select='/*/@b' level='any'/>" +
            "</xsl:template>");
        string document = "<book a='x' b='y'><section/><section/><chapter><note/></chapter><chapter><note/><box><note/><note/></box></chapter></book>";

        XdmSequence result = Transform(source, document);

        Assert.That(TextValues(result), Is.EqualTo(new[] { "2", "3", "", "1" }));
    }

    [Test]
    public void FormatsSuppliedSequencesWithTokensAndStartOffsets()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:number value='(5, 13, 7)' format='A-001(i)'/>" +
            "<xsl:number value='(1, 1, 1, 1)' start-at='3 0 0' format='1.1.1.1'/>" +
            "<xsl:number value='14' format='I' letter-value='traditional'/>" +
            "</xsl:template>");

        XdmSequence result = Transform(source, null);

        Assert.That(TextValues(result), Is.EqualTo(new[] { "E-013(vii)", "3.0.0.0", "XIV" }));
    }

    [Test]
    public void AppliesAlphabeticWordOrdinalLanguageAndGroupingOptions()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:number value='27' format='a' letter-value='alphabetic'/>" +
            "<xsl:number value='23' format='Ww' ordinal='yes' lang='en-US'/>" +
            "<xsl:number value='22' ordinal='true'/>" +
            "<xsl:number value='1000000' grouping-separator=',' grouping-size='3'/>" +
            "<xsl:number value='1' start-at='-1000' grouping-separator=',' grouping-size='+3'/>" +
            "<xsl:number value='12' format='&#x0661;'/>" +
            "<xsl:number value='12' format='&#x1D7CF;'/>" +
            "</xsl:template>");

        XdmSequence result = Transform(source, null);

        Assert.That(TextValues(result), Is.EqualTo(new[] { "aa", "Twenty-Third", "22nd", "1,000,000", "-1,000", "١٢", "𝟏𝟐" }));
    }

    [Test]
    public void ReportsNumberValueSelectionAndFormattingErrors()
    {
        string invalidValue = Stylesheet("<xsl:template name='entry'><xsl:number value=\"'not-a-number'\"/></xsl:template>");
        string invalidSelect = Stylesheet("<xsl:template name='entry'><xsl:number select='1'/></xsl:template>");
        string invalidStart = Stylesheet("<xsl:template name='entry'><xsl:number value='1' start-at='1 bad'/></xsl:template>");
        string invalidLanguage = Stylesheet("<xsl:template name='entry'><xsl:number value='1' lang='not_a_language'/></xsl:template>");

        XsltDynamicException valueError = Assert.Throws<XsltDynamicException>(() => Transform(invalidValue, null))!;
        XsltTypeException selectError = Assert.Throws<XsltTypeException>(() => Transform(invalidSelect, null))!;
        XsltDynamicException startError = Assert.Throws<XsltDynamicException>(() => Transform(invalidStart, null))!;
        XsltDynamicException languageError = Assert.Throws<XsltDynamicException>(() => Transform(invalidLanguage, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(valueError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0980));
            Assert.That(selectError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE1000));
            Assert.That(startError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0030));
            Assert.That(languageError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0030));
        });
    }

    [Test]
    public void RejectsInvalidStaticNumberAttributeCombinations()
    {
        string conflicting = Stylesheet("<xsl:template name='entry'><xsl:number value='1' count='item'/></xsl:template>");
        string level = Stylesheet("<xsl:template name='entry'><xsl:number level='all'/></xsl:template>");
        string content = Stylesheet("<xsl:template name='entry'><xsl:number>not empty</xsl:number></xsl:template>");

        XsltStaticException conflictingError = Assert.Throws<XsltStaticException>(() => Compile(conflicting))!;
        XsltStaticException levelError = Assert.Throws<XsltStaticException>(() => Compile(level))!;
        XsltStaticException contentError = Assert.Throws<XsltStaticException>(() => Compile(content))!;

        Assert.Multiple(() =>
        {
            Assert.That(conflictingError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0975));
            Assert.That(levelError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0020));
            Assert.That(contentError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0010));
        });
    }

    private static string[] TextValues(XdmSequence sequence)
    {
        return sequence.Nodes!.Select(node => node.Value ?? "").ToArray();
    }

    private static XdmSequence Transform(string stylesheet, string? document)
    {
        XsltExecutable executable = Compile(stylesheet);
        XsltTransformOptions options = new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } };
        if (document != null)
        {
            XmlDocument parsed = new XmlParser().ParseXmlDocument(new XmlInput { Text = document }, new XmlParserOptions(), null, null, null);
            options.GlobalContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(parsed.DocumentNode!);
        }

        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
        return state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
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

    private static string Stylesheet(string content)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" + content + "</xsl:stylesheet>";
    }
}
