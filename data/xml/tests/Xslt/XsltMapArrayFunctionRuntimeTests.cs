using xml;

namespace xml.Tests;

[TestFixture]
public class XsltMapArrayFunctionRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void ConstructsMapsFromSelectedAndSequenceConstructorValues()
    {
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:map>" +
            "<xsl:map-entry key=\"'first'\" select='(1, 2)'/>" +
            "<xsl:map-entry key=\"'second'\"><xsl:sequence select=\"'value'\"/></xsl:map-entry>" +
            "</xsl:map></xsl:template>",
            "");

        XdmMapValue map = Transform(stylesheet).Maps!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(map.Count, Is.EqualTo(2));
            Assert.That(AtomicStrings(map.Entries![0].Value!), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(AtomicStrings(map.Entries[1].Value!), Is.EqualTo(new[] { "value" }));
        });
    }

    [Test]
    public void RejectsDuplicateMapKeysAndInvalidMapEntryKeyCardinality()
    {
        string duplicate = Stylesheet(
            "<xsl:template name='entry'><xsl:map>" +
            "<xsl:map-entry key=\"'same'\" select='1'/><xsl:map-entry key=\"'same'\" select='2'/>" +
            "</xsl:map></xsl:template>",
            "");
        string key = Stylesheet(
            "<xsl:template name='entry'><xsl:map><xsl:map-entry key='(1, 2)' select='3'/></xsl:map></xsl:template>",
            "");

        XsltDynamicException duplicateError = Assert.Throws<XsltDynamicException>(() => Transform(duplicate))!;
        XsltTypeException keyError = Assert.Throws<XsltTypeException>(() => Transform(key))!;

        Assert.Multiple(() =>
        {
            Assert.That(duplicateError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE3365));
            Assert.That(keyError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void ConstructsSingletonAndExplicitSequenceArrayMembers()
    {
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:array>" +
            "<xsl:sequence select='1 to 2'/><xsl:array-member select='(3, 4)'/><xsl:array-member/>" +
            "</xsl:array></xsl:template>",
            "");

        XdmArrayValue array = Transform(stylesheet).Arrays!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(array.Count, Is.EqualTo(4));
            Assert.That(AtomicStrings(array.Members![0]), Is.EqualTo(new[] { "1" }));
            Assert.That(AtomicStrings(array.Members[1]), Is.EqualTo(new[] { "2" }));
            Assert.That(AtomicStrings(array.Members[2]), Is.EqualTo(new[] { "3", "4" }));
            Assert.That(array.Members[3].Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void InvokesStylesheetFunctionsThroughStaticAndHigherOrderCalls()
    {
        string declarations =
            "<xsl:function name='f:add' as='xs:integer'>" +
            "<xsl:param name='left' as='xs:integer'/><xsl:param name='right' as='xs:integer'/>" +
            "<xsl:sequence select='$left + $right'/></xsl:function>" +
            "<xsl:function name='f:adder' as='function(*)'><xsl:param name='amount' as='xs:integer'/>" +
            "<xsl:sequence select='function($value) { $value + $amount }'/></xsl:function>" +
            "<xsl:function name='f:sum' as='xs:integer'><xsl:param name='value' as='xs:integer'/>" +
            "<xsl:sequence select='if ($value eq 0) then 0 else $value + f:sum($value - 1)'/></xsl:function>" +
            "<xsl:template name='entry'>" +
            "<xsl:variable name='offset' select='10'/>" +
            "<xsl:variable name='named' as='function(*)' select='f:add#2'/>" +
            "<xsl:variable name='partial' as='function(*)' select='f:add(5, ?)'/>" +
            "<xsl:variable name='looked-up' as='function(*)' select=\"function-lookup(QName('urn:functions', 'f:add'), 2)\"/>" +
            "<xsl:variable name='closure' as='function(*)' select='function($value) { $value + $offset }'/>" +
            "<xsl:variable name='returned-closure' as='function(*)' select='f:adder(20)'/>" +
            "<xsl:sequence select='(f:add(1, 2), $named(2, 3), $partial(7), $looked-up(4, 6), $closure(8), $returned-closure(2), f:sum(4))'/>" +
            "</xsl:template>";
        string stylesheet = Stylesheet(declarations, "xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        Assert.That(AtomicStrings(Transform(stylesheet)), Is.EqualTo(new[] { "3", "5", "12", "10", "18", "22", "10" }));
    }

    [Test]
    public void ClearsContextualXsltStateInsideStylesheetFunctions()
    {
        string stylesheet = Stylesheet(
            "<xsl:function name='f:capture' as='xs:string'><xsl:sequence select='regex-group(1)'/></xsl:function>" +
            "<xsl:template name='entry'><xsl:analyze-string select=\"'a'\" regex='(a)'><xsl:matching-substring>" +
            "<xsl:variable name='capture' as='function(*)' select='f:capture#0'/>" +
            "<xsl:sequence select=\"concat(regex-group(1), ':', f:capture(), ':', $capture())\"/>" +
            "</xsl:matching-substring></xsl:analyze-string></xsl:template>",
            "xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        Assert.That(AtomicStrings(Transform(stylesheet)), Is.EqualTo(new[] { "a::" }));
    }

    [Test]
    public void ConvertsComplexFunctionParametersAndResultsAndReportsResultTypeErrors()
    {
        string declarations =
            "<xsl:function name='f:array' as='array(*)'><xsl:param name='value' as='item()*'/>" +
            "<xsl:array><xsl:array-member select='$value'/></xsl:array></xsl:function>" +
            "<xsl:function name='f:bad' as='xs:integer'><xsl:sequence select=\"'not-an-integer'\"/></xsl:function>" +
            "<xsl:template name='entry'><xsl:sequence select='f:array((1, 2))'/></xsl:template>";
        string stylesheet = Stylesheet(declarations, "xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema'");
        XdmArrayValue array = Transform(stylesheet).Arrays!.Single();

        string invalid = Stylesheet(
            "<xsl:function name='f:bad' as='xs:integer'><xsl:sequence select=\"'not-an-integer'\"/></xsl:function>" +
            "<xsl:template name='entry'><xsl:sequence select='f:bad()'/></xsl:template>",
            "xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema'");
        XsltTypeException error = Assert.Throws<XsltTypeException>(() => Transform(invalid))!;

        Assert.Multiple(() =>
        {
            Assert.That(array.Count, Is.EqualTo(1));
            Assert.That(AtomicStrings(array.Members![0]), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE0780));
        });
    }

    [Test]
    public void ExecutesPublicZeroArityInitialFunctions()
    {
        string stylesheet =
            "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' name='urn:main' package-version='1' version='3.0'>" +
            "<xsl:function name='f:start' visibility='public'><xsl:sequence select=\"'started'\"/></xsl:function>" +
            "</xsl:package>";
        XsltExecutable executable = Compile(stylesheet, new RuntimeResolver(), null);
        XsltTransformOptions options = new XsltTransformOptions { InitialFunction = new XmlName { LocalName = "start", NamespaceUri = "urn:functions" } };
        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(executable, options);

        Assert.That(AtomicStrings(state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation()), Is.EqualTo(new[] { "started" }));
    }

    [Test]
    public void InvokesOverriddenFunctionsAndOriginalComponents()
    {
        RuntimeResolver resolver = new RuntimeResolver();
        resolver.Resources.Add("https://example.test/library.xsl",
            "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' name='urn:library' package-version='1' version='3.0'>" +
            "<xsl:variable name='suffix' select=\"'base'\"/>" +
            "<xsl:function name='f:value' visibility='public'><xsl:sequence select='$suffix'/></xsl:function>" +
            "<xsl:function name='f:get' visibility='public' as='function(*)'><xsl:sequence select='f:value#0'/></xsl:function>" +
            "</xsl:package>");
        XsltPackageReference reference = new XsltPackageReference
        {
            Name = new XmlName { LocalName = "urn:library" },
            Version = "1",
            LocationUri = "library.xsl"
        };
        string stylesheet =
            "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' name='urn:main' package-version='1' version='3.0'>" +
            "<xsl:use-package name='urn:library'><xsl:override>" +
            "<xsl:function name='f:value'><xsl:sequence select=\"concat(xsl:original(), '!')\"/></xsl:function>" +
            "</xsl:override></xsl:use-package>" +
            "<xsl:template name='entry' visibility='public'><xsl:variable name='get' as='function(*)' select='f:get()'/>" +
            "<xsl:sequence select='($get(), f:value())'/></xsl:template>" +
            "</xsl:package>";

        Assert.That(AtomicStrings(Transform(Compile(stylesheet, resolver, new[] { reference }))), Is.EqualTo(new[] { "base!", "base!" }));
    }

    private static string[] AtomicStrings(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "").ToArray();
    }

    private static XdmSequence Transform(string stylesheet)
    {
        return Transform(Compile(stylesheet, new RuntimeResolver(), null));
    }

    private static XdmSequence Transform(XsltExecutable executable)
    {
        XsltTransformOptions options = new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } };
        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
        return state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
    }

    private static XsltExecutable Compile(string source, RuntimeResolver resolver, XsltPackageReference[]? packages)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            Packages = packages,
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver);
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private sealed class RuntimeResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>(StringComparer.Ordinal);

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string requested = systemIdentifier ?? "";
            string absolute = new Uri(new Uri(baseUri!), requested).AbsoluteUri;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                Input = new XmlInput { Text = Resources[absolute] },
                RequestedUri = requested,
                AbsoluteUri = absolute
            };
        }
    }
}
