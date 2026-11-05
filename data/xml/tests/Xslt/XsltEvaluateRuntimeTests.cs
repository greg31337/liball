using xml;

namespace xml.Tests;

[TestFixture]
public class XsltEvaluateRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void EvaluatesTargetWithExplicitContextAndStaticParameters()
    {
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:evaluate xpath=\"'$value + .'\" context-item='5'>" +
            "<xsl:with-param name='value' as='xs:integer' select='7'/>" +
            "</xsl:evaluate></xsl:template>",
            "xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        Assert.That(AtomicStrings(Transform(stylesheet)), Is.EqualTo(new[] { "12" }));
    }

    [Test]
    public void DynamicParameterMapOverridesStaticParametersAndSupportsPublicFunctions()
    {
        string stylesheet =
            "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' name='urn:evaluate' package-version='1' version='3.0'>" +
            "<xsl:function name='f:twice' visibility='public'><xsl:param name='value'/><xsl:sequence select='$value * 2'/></xsl:function>" +
            "<xsl:template name='entry' visibility='public'><xsl:evaluate xpath=\"'f:twice($value)'\" " +
            "with-params=\"map{QName('', 'value'): 9}\"><xsl:with-param name='value' select='2'/></xsl:evaluate></xsl:template>" +
            "</xsl:package>";

        Assert.That(AtomicStrings(Transform(stylesheet)), Is.EqualTo(new[] { "18" }));
    }

    [Test]
    public void UsesSuppliedNamespaceContextAndDynamicStaticControls()
    {
        string stylesheet = Stylesheet(
            "<xsl:variable name='tail' select=\"'dynamic/'\"/><xsl:variable name='aware' select=\"'no'\"/>" +
            "<xsl:template name='entry'><xsl:variable name='context'><n:root><n:value/></n:root></xsl:variable>" +
            "<xsl:evaluate xpath=\"'count(n:value)'\" context-item='$context' namespace-context='$context'/>" +
            "<xsl:evaluate xpath=\"'static-base-uri()'\" base-uri=\"{'https://example.test/' || $tail}\" schema-aware='{$aware}'/>" +
            "</xsl:template>",
            "xmlns:n='urn:names'");

        Assert.That(AtomicStrings(Transform(stylesheet)), Is.EqualTo(new[] { "1", "https://example.test/dynamic/" }));
    }

    [Test]
    public void ReportsTargetAndSuppliedContextErrors()
    {
        XsltDynamicException invalid = Assert.Throws<XsltDynamicException>(() => Transform(Evaluate("'1 +'", "")))!;
        XsltTypeException parameters = Assert.Throws<XsltTypeException>(() => Transform(Evaluate("'$value'", " with-params='42'")))!;
        XsltTypeException namespaces = Assert.Throws<XsltTypeException>(() => Transform(Evaluate("'1'", " namespace-context='42'")))!;
        XsltTypeException context = Assert.Throws<XsltTypeException>(() => Transform(Evaluate("'1'", " context-item='(1, 2)'")))!;

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE3160));
            Assert.That(parameters.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE3165));
            Assert.That(namespaces.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE3170));
            Assert.That(context.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE3210));
        });
    }

    [Test]
    public void AppliesResultTypeConversion()
    {
        string valid = Stylesheet("<xsl:template name='entry'><xsl:evaluate xpath=\"'7'\" as='xs:integer'/></xsl:template>", "xmlns:xs='http://www.w3.org/2001/XMLSchema'");
        string invalid = Stylesheet("<xsl:template name='entry'><xsl:evaluate xpath=\"'7'\" as='xs:string'/></xsl:template>", "xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        XPathTypeException error = Assert.Throws<XPathTypeException>(() => Transform(invalid))!;

        Assert.Multiple(() =>
        {
            Assert.That(AtomicStrings(Transform(valid)), Is.EqualTo(new[] { "7" }));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void KeepsCompiledExpressionCachesPerTransformation()
    {
        XsltExecutable executable = Compile(Evaluate("'1 + 1'", ""));
        XsltTransformationState first = Initialize(executable, null);
        XsltTransformationState second = Initialize(executable, null);

        first.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        first.TemplateRuntime.ExecuteXsltInitialTemplateInvocation();
        second.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(first.EvaluateRuntime!.Cache, Has.Count.EqualTo(1));
            Assert.That(second.EvaluateRuntime!.Cache, Has.Count.EqualTo(1));
            Assert.That(first.EvaluateRuntime.Cache[0].Plan, Is.Not.SameAs(second.EvaluateRuntime.Cache[0].Plan));
        });
    }

    [Test]
    public void EnforcesExpressionAndOperationLimits()
    {
        XmlSecurityLimits limits = new XPathRuntimeContext(new XPathDynamicContext()).Limits;
        limits.MaximumTokenLength = 5;
        XsltDynamicException length = Assert.Throws<XsltDynamicException>(() => Transform(Compile(Evaluate("'1 + 2 + 3'", "")), limits))!;
        string attributes = " options=\"map{'maximum-operations': 2}\"";
        XmlLimitException operations = Assert.Throws<XmlLimitException>(() => Transform(Evaluate("'for $i in 1 to 10 return $i + 1'", attributes)))!;

        Assert.Multiple(() =>
        {
            Assert.That(length.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(operations.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        });
    }

    private static string Evaluate(string target, string attributes)
    {
        return Stylesheet("<xsl:template name='entry'><xsl:evaluate xpath=\"" + target + "\"" + attributes + "/></xsl:template>", "");
    }

    private static string[] AtomicStrings(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "").ToArray();
    }

    private static XdmSequence Transform(string stylesheet)
    {
        return Transform(Compile(stylesheet), null);
    }

    private static XdmSequence Transform(XsltExecutable executable, XmlSecurityLimits? limits)
    {
        XsltTransformationState state = Initialize(executable, limits);
        return state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
    }

    private static XsltTransformationState Initialize(XsltExecutable executable, XmlSecurityLimits? limits)
    {
        XsltTransformOptions options = new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            SecurityLimits = limits
        };
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
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
