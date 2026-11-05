using xml;

namespace xml.Tests;

[TestFixture]
public class XsltGroupingRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void GroupBySupportsMultipleKeysAndIgnoresDuplicateKeysPerItem()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select=\"('one', 'two')\" " +
            "group-by=\"if (. = 'one') then ('x', 'y') else ('x', 'x')\">" +
            "<xsl:sequence select=\"concat(current-grouping-key(), ':', string-join(current-group(), ','))\"/>" +
            "</xsl:for-each-group></xsl:template>");

        XdmSequence result = Transform(source);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "x:one,two", "y:one" }));
    }

    [Test]
    public void GroupBySupportsCompositeAndCollatedKeys()
    {
        string composite = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select=\"('A1-one', 'A1-two', 'A2-three')\" " +
            "group-by='(substring(., 1, 1), substring(., 2, 1))' composite='yes'>" +
            "<xsl:sequence select=\"concat(string-join(current-grouping-key(), '-'), ':', count(current-group()))\"/>" +
            "</xsl:for-each-group></xsl:template>");
        string collated = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select=\"('A', 'a', 'B')\" group-by='.' " +
            "collation='http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive'>" +
            "<xsl:sequence select=\"concat(current-grouping-key(), ':', count(current-group()))\"/>" +
            "</xsl:for-each-group></xsl:template>");

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(Transform(composite)), Is.EqualTo(new[] { "A-1:2", "A-2:1" }));
            Assert.That(StringValues(Transform(collated)), Is.EqualTo(new[] { "A:2", "B:1" }));
        });
    }

    [Test]
    public void GroupAdjacentCreatesMaximalRuns()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select=\"('a1', 'a2', 'b1', 'a3')\" group-adjacent='substring(., 1, 1)'>" +
            "<xsl:sequence select=\"concat(current-grouping-key(), ':', string-join(current-group(), ','))\"/>" +
            "</xsl:for-each-group></xsl:template>");

        XdmSequence result = Transform(source);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "a:a1,a2", "b:b1", "a:a3" }));
    }

    [Test]
    public void PatternGroupingRecognizesStartingAndEndingItems()
    {
        string starting = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select='/*/*' group-starting-with='h'>" +
            "<xsl:sequence select=\"string-join(current-group()/@id, ',')\"/>" +
            "</xsl:for-each-group></xsl:template>");
        string ending = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select='/*/*' group-ending-with='e'>" +
            "<xsl:sequence select=\"string-join(current-group()/@id, ',')\"/>" +
            "</xsl:for-each-group></xsl:template>");

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(TransformWithSource(starting, "<items><h id='1'/><p id='2'/><h id='3'/><p id='4'/></items>")), Is.EqualTo(new[] { "1,2", "3,4" }));
            Assert.That(StringValues(TransformWithSource(ending, "<items><p id='1'/><e id='2'/><p id='3'/><e id='4'/><p id='5'/></items>")), Is.EqualTo(new[] { "1,2", "3,4", "5" }));
        });
    }

    [Test]
    public void SortsGroupsWithGroupingContextAndProcessingFocus()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select=\"('b', 'a', 'b')\" group-by='.'>" +
            "<xsl:sort select='current-grouping-key()' order='descending'/>" +
            "<xsl:sequence select=\"concat(position(), ':', current-grouping-key(), ':', count(current-group()))\"/>" +
            "</xsl:for-each-group></xsl:template>");

        XdmSequence result = Transform(source);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "1:b:2", "2:a:1" }));
    }

    [Test]
    public void GroupContextFlowsIntoValueTemplatesAndCalledTemplates()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select=\"('x', 'x')\" group-by='.'>" +
            "<xsl:call-template name='called'/></xsl:for-each-group></xsl:template>" +
            "<xsl:template name='called'><out key='{current-grouping-key()}' size='{count(current-group())}'/></xsl:template>");

        XmlNode result = Transform(source).Nodes!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Attributes!.Single(attribute => attribute.Name!.LocalName == "key").Value, Is.EqualTo("x"));
            Assert.That(result.Attributes!.Single(attribute => attribute.Name!.LocalName == "size").Value, Is.EqualTo("2"));
        });
    }

    [Test]
    public void ReportsGroupingKeyCollationAndContextErrors()
    {
        string cardinality = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select='1 to 2' group-adjacent='(1, 2)'/></xsl:template>");
        string collation = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select='1' group-by='.' collation='urn:unsupported'/></xsl:template>");
        string groupAbsent = Stylesheet(
            "<xsl:template name='entry'><xsl:sequence select='current-group()'/></xsl:template>");
        string keyAbsent = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select='/*/*' group-starting-with='p'>" +
            "<xsl:sequence select='current-grouping-key()'/></xsl:for-each-group></xsl:template>");

        XsltTypeException cardinalityError = Assert.Throws<XsltTypeException>(() => Transform(cardinality))!;
        XsltDynamicException collationError = Assert.Throws<XsltDynamicException>(() => Transform(collation))!;
        XPathDynamicException groupError = Assert.Throws<XPathDynamicException>(() => Transform(groupAbsent))!;
        XPathDynamicException keyError = Assert.Throws<XPathDynamicException>(() => TransformWithSource(keyAbsent, "<items><p/></items>"))!;

        Assert.Multiple(() =>
        {
            Assert.That(cardinalityError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE1100));
            Assert.That(collationError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1110));
            Assert.That(groupError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1061));
            Assert.That(keyError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1071));
        });
    }

    [Test]
    public void RejectsKeyOptionsWithPatternGrouping()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each-group select='.' group-starting-with='*' composite='yes'/></xsl:template>");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE1090));
    }

    private static string[] StringValues(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "").ToArray();
    }

    private static XdmSequence Transform(string source)
    {
        XsltExecutable executable = Compile(source);
        XsltTransformOptions options = new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } };
        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
        return state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
    }

    private static XdmSequence TransformWithSource(string source, string document)
    {
        XsltExecutable executable = Compile(source);
        XmlDocument parsed = new XmlParser().ParseXmlDocument(new XmlInput { Text = document }, new XmlParserOptions(), null, null, null);
        XsltTransformOptions options = new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            GlobalContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(parsed.DocumentNode!)
        };
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
