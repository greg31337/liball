using xml;

namespace xml.Tests;

[TestFixture]
public class XsltKeyRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string CaseInsensitive = "http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive";

    [Test]
    public void BuildsMergedIndexesLazilyAndReturnsDocumentOrderedDistinctNodes()
    {
        string stylesheet = Stylesheet(
            "<xsl:key name='by-code' match='item' use='@code'/>" +
            "<xsl:key name='by-code' match='item[@alternate]' use='@alternate'/>" +
            "<xsl:template name='entry'><xsl:sequence select=\"string-join(key('by-code', 'A')/@name, ',')\"/></xsl:template>",
            "");
        XsltTransformationState state = Initialize(stylesheet, "<root><item name='one' code='A' alternate='A'/><item name='two' code='B' alternate='A'/><item name='three' code='A'/></root>");

        Assert.That(state.KeyRuntime!.Indexes, Is.Empty);
        XdmSequence result = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(StringValue(result), Is.EqualTo("one,two,three"));
            Assert.That(state.KeyRuntime.Indexes, Has.Count.EqualTo(1));
            Assert.That(state.KeyRuntime.Indexes.Single().Status, Is.EqualTo(XsltKeyIndexStatus.Ready));
        });
    }

    [Test]
    public void MatchesNonCompositeValuesWithAtomizationAndCollation()
    {
        string stylesheet = Stylesheet(
            "<xsl:key name='ci' match='item' use='(@code, @alias)' collation='" + CaseInsensitive + "'/>" +
            "<xsl:template name='entry'><xsl:sequence select=\"string-join(key('ci', ('ALPHA', 'b'))/@name, ',')\"/></xsl:template>",
            "");

        XdmSequence result = Transform(stylesheet, "<root><item name='one' code='alpha' alias='ALPHA'/><item name='two' code='B'/><item name='three' alias='Alpha'/></root>");

        Assert.That(StringValue(result), Is.EqualTo("one,two,three"));
    }

    [Test]
    public void MatchesCompositeAndEmptyCompositeKeys()
    {
        string stylesheet = Stylesheet(
            "<xsl:key name='pair' match='item' use='(@kind, @code)' composite='yes'/>" +
            "<xsl:key name='empty' match='item' use='@missing' composite='yes'/>" +
            "<xsl:template name='entry'>" +
            "<xsl:sequence select=\"concat(string-join(key('pair', ('book', '2'))/@name, ','), ':', count(key('pair', ('2', 'book'))), ':', count(key('empty', ())))\"/>" +
            "</xsl:template>",
            "");

        XdmSequence result = Transform(stylesheet, "<root><item name='one' kind='book' code='1'/><item name='two' kind='book' code='2'/></root>");

        Assert.That(StringValue(result), Is.EqualTo("two:0:2"));
    }

    [Test]
    public void EvaluatesContentAndKeyDependentDeclarations()
    {
        string stylesheet = Stylesheet(
            "<xsl:key name='content' match='item'><xsl:sequence select='@id'/></xsl:key>" +
            "<xsl:key name='base' match='item' use='@id'/>" +
            "<xsl:key name='dependent' match=\"key('base', 'selected')\" use=\"key('base', @ref)/@code\"/>" +
            "<xsl:template name='entry'>" +
            "<xsl:sequence select=\"concat(key('content', 'target')/@name, ':', key('dependent', 'C')/@name)\"/>" +
            "</xsl:template>",
            "");

        XdmSequence result = Transform(stylesheet, "<root><item id='selected' ref='target' name='chosen'/><item id='target' code='C' name='found'/></root>");

        Assert.That(StringValue(result), Is.EqualTo("found:chosen"));
    }

    [Test]
    public void ResolvesNamesAndRestrictsThreeArgumentLookupsToTheTopSubtree()
    {
        string stylesheet = Stylesheet(
            "<xsl:key name='k:by-code' match='item' use='@code'/>" +
            "<xsl:template name='entry'><xsl:sequence select=\"string-join(key('k:by-code', 'x', /*/part[2])/@name, ',')\"/></xsl:template>",
            "xmlns:k='urn:keys'");

        XdmSequence result = Transform(stylesheet, "<root><part><item code='x' name='one'/></part><part><item code='x' name='two'/><item code='y' name='three'/></part></root>");

        Assert.That(StringValue(result), Is.EqualTo("two"));
    }

    [Test]
    public void KeepsIndexesIsolatedByTransformationAndDocumentIdentity()
    {
        string stylesheet = Stylesheet(
            "<xsl:key name='by-id' match='item' use='@id'/>" +
            "<xsl:template name='entry'><xsl:sequence select=\"key('by-id', 'same')/@name/string()\"/></xsl:template>",
            "");
        XsltExecutable executable = Compile(stylesheet);
        XsltTransformationState first = Initialize(executable, "<root><item id='same' name='first'/></root>");
        XsltTransformationState second = Initialize(executable, "<root><item id='same' name='second'/></root>");

        XdmSequence firstResult = first.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        XdmSequence secondResult = second.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(StringValue(firstResult), Is.EqualTo("first"));
            Assert.That(StringValue(secondResult), Is.EqualTo("second"));
            Assert.That(first.KeyRuntime, Is.Not.SameAs(second.KeyRuntime));
            Assert.That(first.KeyRuntime!.Indexes.Single().Document, Is.Not.SameAs(second.KeyRuntime!.Indexes.Single().Document));
        });
    }

    [Test]
    public void RebuildsAnIndexAfterTheSourceDocumentChanges()
    {
        string stylesheet = Stylesheet(
            "<xsl:key name='by-id' match='item' use='@id'/>" +
            "<xsl:template name='entry'><xsl:sequence select=\"count(key('by-id', 'changed'))\"/></xsl:template>",
            "");
        XsltTransformationState state = Initialize(stylesheet, "<root><item id='original'/></root>");

        XdmSequence before = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        XsltDocumentKeyIndex index = state.KeyRuntime!.Indexes.Single();
        XmlNode document = state.GlobalContextItem.Nodes!.Single();
        XmlNode attribute = document.Children!.Single().Children!.Single().Attributes!.Single();
        new XmlTreeEditor().SetXmlTreeText(attribute, "changed");
        XdmSequence after = state.TemplateRuntime.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(StringValue(before), Is.EqualTo("0"));
            Assert.That(StringValue(after), Is.EqualTo("1"));
            Assert.That(state.KeyRuntime.Indexes.Single(), Is.SameAs(index));
            Assert.That(index.MutationVersion, Is.EqualTo(document.OwnerDocument!.MutationVersion));
        });
    }

    [Test]
    public void ReportsInvalidNamesAndMissingDocumentContexts()
    {
        string unknown = Stylesheet("<xsl:template name='entry'><xsl:sequence select=\"key('unknown', 'x')\"/></xsl:template>", "");
        string noContext = Stylesheet("<xsl:key name='known' match='item' use='@id'/><xsl:template name='entry'><xsl:sequence select=\"key('known', 'x')\"/></xsl:template>", "");
        string invalidTop = Stylesheet(
            "<xsl:key name='known' match='item' use='@id'/><xsl:template name='entry'><xsl:variable name='top'><temporary/></xsl:variable>" +
            "<xsl:sequence select=\"key('known', 'x', $top)\"/></xsl:template>",
            "");

        XPathDynamicException unknownError = Assert.Throws<XPathDynamicException>(() => Transform(unknown, "<root/>"))!;
        XPathDynamicException contextError = Assert.Throws<XPathDynamicException>(() => Transform(noContext, null))!;
        XPathDynamicException topError = Assert.Throws<XPathDynamicException>(() => Transform(invalidTop, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(unknownError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1260));
            Assert.That(contextError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1270));
            Assert.That(topError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1270));
        });
    }

    [Test]
    public void TreatsUntypedValuesAsStringsAndValidatesTheKeyNameType()
    {
        string untyped = Stylesheet(
            "<xsl:key name='typed' match='item' use='xs:integer(@code)'/>" +
            "<xsl:template name='entry'><xsl:sequence select=\"count(key('typed', /root/@ref))\"/></xsl:template>",
            "xmlns:xs='http://www.w3.org/2001/XMLSchema'");
        string invalidNameType = Stylesheet(
            "<xsl:key name='typed' match='item' use='@code'/>" +
            "<xsl:template name='entry'><xsl:sequence select=\"key(1, '7')\"/></xsl:template>",
            "");

        XdmSequence result = Transform(untyped, "<root ref='7'><item code='7'/></root>");
        XPathTypeException error = Assert.Throws<XPathTypeException>(() => Transform(invalidNameType, "<root/>"))!;

        Assert.Multiple(() =>
        {
            Assert.That(StringValue(result), Is.EqualTo("0"));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void ClearsTheNamedKeyContextForDynamicFunctionCalls()
    {
        string stylesheet = Stylesheet(
            "<xsl:key name='known' match='item' use='@id'/>" +
            "<xsl:template name='entry'><xsl:sequence select=\"let $lookup := key#2 return $lookup('known', 'x')\"/></xsl:template>",
            "");

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Transform(stylesheet, "<root><item id='x'/></root>"))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1260));
    }

    private static string StringValue(XdmSequence sequence)
    {
        XdmAtomicValue value = sequence.AtomicValues!.Single();
        return value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "";
    }

    private static XdmSequence Transform(string stylesheet, string? document)
    {
        XsltTransformationState state = Initialize(stylesheet, document);
        return state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
    }

    private static XsltTransformationState Initialize(string stylesheet, string? document)
    {
        return Initialize(Compile(stylesheet), document);
    }

    private static XsltTransformationState Initialize(XsltExecutable executable, string? document)
    {
        XsltTransformOptions options = new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } };
        if (document != null)
        {
            XmlDocument parsed = new XmlParser().ParseXmlDocument(new XmlInput { Text = document }, new XmlParserOptions(), null, null, null);
            options.GlobalContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(parsed.DocumentNode!);
        }

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
