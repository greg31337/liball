using xml;

namespace xml.Tests;

[TestFixture]
public class XsltRegexRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void PartitionsInputAndProvidesSubstringFocusAndCaptureGroups()
    {
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:analyze-string select=\"'A1,C15,,D24, X50,'\" regex='([A-Z])([0-9]+)'>" +
            "<xsl:matching-substring><xsl:sequence select=\"concat('M', position(), '/', last(), ':', ., ':', regex-group(0), ':', regex-group(1), ':', regex-group(2), ':', regex-group(9))\"/></xsl:matching-substring>" +
            "<xsl:non-matching-substring><xsl:sequence select=\"concat('N', position(), '/', last(), ':', ., ':', regex-group(1))\"/></xsl:non-matching-substring>" +
            "</xsl:analyze-string></xsl:template>",
            "");

        XdmSequence result = Transform(stylesheet);

        Assert.That(StringValues(result), Is.EqualTo(new[]
        {
            "M1/8:A1:A1:A:1:",
            "N2/8:,:",
            "M3/8:C15:C15:C:15:",
            "N4/8:,,:",
            "M5/8:D24:D24:D:24:",
            "N6/8:, :",
            "M7/8:X50:X50:X:50:",
            "N8/8:,:",
        }));
    }

    [Test]
    public void EvaluatesRegexAndFlagsAsAttributeValueTemplates()
    {
        string stylesheet = Stylesheet(
            "<xsl:variable name='pattern' select=\"'[a-z]+'\"/>" +
            "<xsl:template name='entry'>" +
            "<xsl:analyze-string select=\"'a B'\" regex='{$pattern}' flags=\"{'i'}\">" +
            "<xsl:matching-substring><xsl:sequence select=\"concat('M:', .)\"/></xsl:matching-substring>" +
            "<xsl:non-matching-substring><xsl:sequence select=\"concat('N:', .)\"/></xsl:non-matching-substring>" +
            "</xsl:analyze-string></xsl:template>",
            "");

        XdmSequence result = Transform(stylesheet);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "M:a", "N: ", "M:B" }));
    }

    [Test]
    public void SkipsAbsentBranchesAndDoesNotEvaluateBranchesForEmptyInput()
    {
        string absent = Stylesheet(
            "<xsl:template name='entry'><xsl:analyze-string select=\"'a-b'\" regex='-'>" +
            "<xsl:non-matching-substring><xsl:sequence select='.'/></xsl:non-matching-substring>" +
            "</xsl:analyze-string></xsl:template>",
            "");
        string empty = Stylesheet(
            "<xsl:template name='entry'><xsl:analyze-string select=\"''\" regex='x'>" +
            "<xsl:matching-substring><xsl:sequence select='1 div 0'/></xsl:matching-substring>" +
            "</xsl:analyze-string></xsl:template>",
            "");

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(Transform(absent)), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(Transform(empty).Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void RestoresOuterCaptureGroupsAfterNestedAnalyzeString()
    {
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:analyze-string select=\"'a'\" regex='(a)'>" +
            "<xsl:matching-substring>" +
            "<xsl:sequence select='regex-group(1)'/>" +
            "<xsl:analyze-string select=\"'b'\" regex='(b)'><xsl:matching-substring><xsl:sequence select='regex-group(1)'/></xsl:matching-substring></xsl:analyze-string>" +
            "<xsl:sequence select='regex-group(1)'/>" +
            "</xsl:matching-substring></xsl:analyze-string></xsl:template>",
            "");

        XdmSequence result = Transform(stylesheet);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "a", "b", "a" }));
    }

    [Test]
    public void ClearsCaptureGroupsForDynamicFunctionCalls()
    {
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:analyze-string select=\"'ab'\" regex='(a)(b)'>" +
            "<xsl:matching-substring>" +
            "<xsl:variable name='named' as='function(*)' select='regex-group#1'/>" +
            "<xsl:variable name='partial' as='function(*)' select='regex-group(?)'/>" +
            "<xsl:variable name='looked-up' as='function(*)' select=\"function-lookup(QName('http://www.w3.org/2005/xpath-functions', 'regex-group'), 1)\"/>" +
            "<xsl:sequence select=\"concat(regex-group(1), ':', $named(1), ':', $partial(2), ':', $looked-up(1))\"/>" +
            "</xsl:matching-substring></xsl:analyze-string></xsl:template>",
            "");

        XdmSequence result = Transform(stylesheet);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "a:::" }));
    }

    [Test]
    public void RegexGroupReturnsAnEmptyStringOutsideMatchingSubstring()
    {
        string stylesheet = Stylesheet("<xsl:template name='entry'><xsl:sequence select=\"concat('[', regex-group(-1), ':', regex-group(0), ']')\"/></xsl:template>", "");

        Assert.That(StringValues(Transform(stylesheet)), Is.EqualTo(new[] { "[:]" }));
    }

    [Test]
    public void ReportsInvalidPatternsFlagsZeroLengthMatchesAndInputTypes()
    {
        XsltDynamicException pattern = Assert.Throws<XsltDynamicException>(() => Transform(Analyze("'x'", "[", null)))!;
        XsltDynamicException flags = Assert.Throws<XsltDynamicException>(() => Transform(Analyze("'x'", "x", "z")))!;
        XsltDynamicException empty = Assert.Throws<XsltDynamicException>(() => Transform(Analyze("'x'", "a*", null)))!;
        XsltTypeException input = Assert.Throws<XsltTypeException>(() => Transform(Analyze("42", "x", null)))!;

        Assert.Multiple(() =>
        {
            Assert.That(pattern.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1140));
            Assert.That(flags.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1145));
            Assert.That(empty.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1150));
            Assert.That(input.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    private static string Analyze(string select, string regex, string? flags)
    {
        string flagAttribute = flags == null ? "" : " flags='" + flags + "'";
        return Stylesheet(
            "<xsl:template name='entry'><xsl:analyze-string select=\"" + select + "\" regex='" + regex + "'" + flagAttribute + ">" +
            "<xsl:matching-substring><xsl:sequence select='.'/></xsl:matching-substring>" +
            "</xsl:analyze-string></xsl:template>",
            "");
    }

    private static string[] StringValues(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "").ToArray();
    }

    private static XdmSequence Transform(string stylesheet)
    {
        XsltTransformOptions options = new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } };
        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(Compile(stylesheet), options);
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

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }
}
