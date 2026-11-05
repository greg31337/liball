using xml;

namespace xml.Tests;

[TestFixture]
public class XsltSortRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void PerformSortOrdersNumericEmptyAndNanKeys()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:perform-sort select=\"('10', 'NaN', '2', 'empty')\">" +
            "<xsl:sort select=\"if (. = 'empty') then () else .\" data-type='number'/>" +
            "</xsl:perform-sort></xsl:template>");

        XdmSequence result = Transform(source);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "empty", "NaN", "2", "10" }));
    }

    [Test]
    public void ForEachUsesMultipleKeysAndPreservesEqualKeyOrder()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each select=\"('b2-first', 'a1', 'b2-second', 'a2')\">" +
            "<xsl:sort select='substring(., 1, 1)'/>" +
            "<xsl:sort select='substring(., 2, 1)' data-type='number' order='descending'/>" +
            "<xsl:sequence select='.'/></xsl:for-each></xsl:template>");

        XdmSequence result = Transform(source);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "a2", "a1", "b2-first", "b2-second" }));
    }

    [Test]
    public void SortKeySequenceConstructorUsesOriginalFocus()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:perform-sort select=\"('one', 'two', 'three')\">" +
            "<xsl:sort data-type='number'><xsl:sequence select='0 - position()'/></xsl:sort>" +
            "</xsl:perform-sort></xsl:template>");

        XdmSequence result = Transform(source);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "three", "two", "one" }));
    }

    [Test]
    public void PerformSortBuildsAndAtomizesItsContentSequence()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:perform-sort>" +
            "<xsl:sort select='@n' data-type='number'/><item n='10'/><item n='2'/>" +
            "</xsl:perform-sort></xsl:template>");

        XdmSequence result = Transform(source);

        Assert.That(result.Nodes!.Select(node => node.Attributes!.Single().Value), Is.EqualTo(new[] { "2", "10" }));
    }

    [Test]
    public void CaseOrderAndExplicitCollationControlTextOrdering()
    {
        string upper = Stylesheet(
            "<xsl:template name='entry'><xsl:perform-sort select=\"('b', 'a', 'B', 'A')\">" +
            "<xsl:sort lang='en-US' case-order='upper-first'/></xsl:perform-sort></xsl:template>");
        string insensitive = Stylesheet(
            "<xsl:template name='entry'><xsl:perform-sort select=\"('b', 'A', 'a', 'B')\">" +
            "<xsl:sort collation='http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive'/>" +
            "</xsl:perform-sort></xsl:template>");

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(Transform(upper)), Is.EqualTo(new[] { "A", "a", "B", "b" }));
            Assert.That(StringValues(Transform(insensitive)), Is.EqualTo(new[] { "A", "a", "b", "B" }));
        });
    }

    [Test]
    public void ApplyTemplatesReceivesSortedSelectionAndFocus()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:apply-templates select=\"('10', '2', '1')\">" +
            "<xsl:sort data-type='number' stable='{\"yes\"}'/>" +
            "</xsl:apply-templates></xsl:template>");

        XdmSequence result = Transform(source);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "1", "2", "10" }));
    }

    [Test]
    public void ReportsSortCardinalityCollationAndComparisonErrors()
    {
        string cardinality = Stylesheet(
            "<xsl:template name='entry'><xsl:perform-sort select='1 to 2'><xsl:sort select='(1, 2)'/></xsl:perform-sort></xsl:template>");
        string collation = Stylesheet(
            "<xsl:template name='entry'><xsl:perform-sort select='2 to 1'><xsl:sort collation='urn:unsupported'/></xsl:perform-sort></xsl:template>");
        string comparison = Stylesheet(
            "<xsl:template name='entry'><xsl:perform-sort select=\"(1, 'text')\"><xsl:sort/></xsl:perform-sort></xsl:template>");

        XsltTypeException cardinalityError = Assert.Throws<XsltTypeException>(() => Transform(cardinality))!;
        XsltDynamicException collationError = Assert.Throws<XsltDynamicException>(() => Transform(collation))!;
        XsltDynamicException comparisonError = Assert.Throws<XsltDynamicException>(() => Transform(comparison))!;

        Assert.Multiple(() =>
        {
            Assert.That(cardinalityError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE1020));
            Assert.That(collationError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1035));
            Assert.That(comparisonError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1030));
        });
    }

    [Test]
    public void RejectsInvalidSortContentAndStablePlacement()
    {
        string content = Stylesheet(
            "<xsl:template name='entry'><xsl:for-each select='1'><xsl:sort select='.'><xsl:sequence select='.'/></xsl:sort></xsl:for-each></xsl:template>");
        string stable = Stylesheet(
            "<xsl:template name='entry'><xsl:perform-sort select='1'><xsl:sort/><xsl:sort stable='yes'/></xsl:perform-sort></xsl:template>");

        XsltStaticException contentError = Assert.Throws<XsltStaticException>(() => Compile(content))!;
        XsltStaticException stableError = Assert.Throws<XsltStaticException>(() => Compile(stable))!;

        Assert.Multiple(() =>
        {
            Assert.That(contentError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE1015));
            Assert.That(stableError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE1017));
        });
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
