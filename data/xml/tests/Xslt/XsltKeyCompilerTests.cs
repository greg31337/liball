using xml;

namespace xml.Tests;

[TestFixture]
public class XsltKeyCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string CaseInsensitive = "http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive";

    [Test]
    public void CompilesMatchUseTypeCompositeCollationAndDependencies()
    {
        string source = Stylesheet(
            "<xsl:variable name='suffix' select=\"'x'\"/>" +
            "<xsl:key name='k:by-code' match='item[@active]' use='concat(@code, $suffix)' composite='yes' collation='" + CaseInsensitive + "'/>",
            "xmlns:k='urn:keys'");

        XsltKeyPlan key = Plan(Compile(source)).Keys.Single();
        XsltKeyDeclarationPlan declaration = key.Declarations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(key.Name, Is.EqualTo("Q{urn:keys}by-code"));
            Assert.That(key.ResolvedName.LocalName, Is.EqualTo("by-code"));
            Assert.That(key.Composite, Is.True);
            Assert.That(key.Collation, Is.EqualTo(CaseInsensitive));
            Assert.That(declaration.MatchSource, Is.EqualTo("item[@active]"));
            Assert.That(declaration.Match.IsDeferred, Is.False);
            Assert.That(declaration.Match.EquivalentExpression, Is.Not.Null);
            Assert.That(declaration.ValueKind, Is.EqualTo(XsltKeyValueKind.Expression));
            Assert.That(declaration.UseSource, Is.EqualTo("concat(@code, $suffix)"));
            Assert.That(declaration.Use, Is.Not.Null);
            Assert.That(declaration.UseType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Atomic));
            Assert.That(declaration.UseType.AtomicType, Is.EqualTo(XdmAtomicType.String));
            Assert.That(declaration.Dependencies.Single().Name.LocalName, Is.EqualTo("suffix"));
        });
    }

    [Test]
    public void CompilesASequenceConstructorKeyValue()
    {
        string source = Stylesheet(
            "<xsl:key name='content' match='item'><xsl:sequence select='@code'/><xsl:text>-fixed</xsl:text></xsl:key>",
            "");

        XsltKeyDeclarationPlan declaration = Plan(Compile(source)).Keys.Single().Declarations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(declaration.ValueKind, Is.EqualTo(XsltKeyValueKind.Content));
            Assert.That(declaration.Use, Is.Null);
            Assert.That(declaration.UseType, Is.Null);
            Assert.That(declaration.Content, Is.Not.Null);
            Assert.That(declaration.Content!.Items.Select(item => item.LocalName), Is.EqualTo(new[] { "sequence", "text" }));
            Assert.That(declaration.Content.Items.All(item => item.Kind == XsltSequenceItemKind.Instruction), Is.True);
        });
    }

    [Test]
    public void MergesEverySameNameDeclarationAcrossImportPrecedence()
    {
        KeyResolver resolver = new KeyResolver();
        resolver.Resources.Add(
            "https://example.test/base.xsl",
            Stylesheet("<xsl:key name='shared' match='base' use='@id' composite='yes'/>", ""));
        string source = Stylesheet(
            "<xsl:import href='base.xsl'/>" +
            "<xsl:key name='shared' match='local-one' use='@code' composite='true'/>" +
            "<xsl:key name='shared' match='local-two' use='@name' composite='1'/>",
            "");

        XsltStylesheetPlan stylesheet = Plan(Compile(source, resolver));
        XsltKeyPlan key = stylesheet.Keys.Single();

        Assert.Multiple(() =>
        {
            Assert.That(key.Declarations, Has.Length.EqualTo(3));
            Assert.That(key.Declarations.Select(item => item.MatchSource), Is.EqualTo(new[] { "base", "local-one", "local-two" }));
            Assert.That(key.Declarations.Select(item => item.ImportPrecedence), Is.Ordered.Ascending);
            Assert.That(key.Declarations[1].DeclarationOrder, Is.LessThan(key.Declarations[2].DeclarationOrder));
            Assert.That(key.Declarations.All(item => item.Use != null), Is.True);
        });
    }

    [Test]
    public void MergesDifferentPrefixesForTheSameExpandedName()
    {
        string source = Stylesheet(
            "<xsl:key name='a:shared' match='first' use='@id'/><xsl:key name='b:shared' match='second' use='@id'/>",
            "xmlns:a='urn:key' xmlns:b='urn:key'");

        XsltKeyPlan key = Plan(Compile(source)).Keys.Single();

        Assert.Multiple(() =>
        {
            Assert.That(key.Name, Is.EqualTo("Q{urn:key}shared"));
            Assert.That(key.Declarations, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void UsesTheInScopeDefaultCollation()
    {
        string source = Stylesheet(
            "<xsl:key name='defaulted' match='item' use='@name'/>",
            "default-collation='" + CaseInsensitive + "'");

        XsltKeyPlan key = Plan(Compile(source)).Keys.Single();

        Assert.That(key.Collation, Is.EqualTo(CaseInsensitive));
    }

    [Test]
    public void MakesTheXsltKeyFunctionAvailableToMatchAndUseExpressions()
    {
        string source = Stylesheet(
            "<xsl:key name='base' match='item' use='@id'/>" +
            "<xsl:key name='dependent' match=\"key('base', 'selected')\" use=\"key('base', @ref)/@code\"/>",
            "");

        XsltKeyDeclarationPlan declaration = Plan(Compile(source)).Keys.Single(item => item.Name == "Q{}dependent").Declarations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(declaration.Match.HasKeyCall, Is.True);
            Assert.That(declaration.Use, Is.Not.Null);
            Assert.That(declaration.Use!.Root.Opcode, Is.EqualTo(XPathPlanOpcode.Path));
            Assert.That(declaration.Use.Root.Children[0].FunctionBinding!.BindingKey, Is.EqualTo("xslt:key#2"));
        });
    }

    [TestCase("<xsl:key match='item' use='@id'/>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:key name='key' use='@id'/>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:key name='key' match='item'/>", XsltErrorCodes.XTSE1205)]
    [TestCase("<xsl:key name='key' match='item' use='@id'><xsl:text>value</xsl:text></xsl:key>", XsltErrorCodes.XTSE1205)]
    [TestCase("<xsl:key name='key' match='item' use='@id' composite='perhaps'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:key name='key' match='item' use='@id' collation='urn:unknown'/>", XsltErrorCodes.XTSE1210)]
    [TestCase("<xsl:key name='key' match='item[' use='@id'/>", XsltErrorCodes.XTSE0340)]
    [TestCase("<xsl:key name='key' match='item' use='1 +'/>", XPathErrorCodes.XPST0003)]
    [TestCase("<xsl:key name='key' match='item' use='$missing'/>", XPathErrorCodes.XPST0008)]
    [TestCase("<xsl:key name='key' match='one' use='@id'/><xsl:key name='key' match='two' use='@id' collation='http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive'/>", XsltErrorCodes.XTSE1220)]
    [TestCase("<xsl:key name='key' match='one' use='@id'/><xsl:key name='key' match='two' use='@id' composite='yes'/>", XsltErrorCodes.XTSE1222)]
    public void RejectsInvalidKeyDeclarations(string declaration, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declaration, "")))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
            Assert.That(error.Location, Is.Not.Null);
        });
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source, IXmlResourceResolver? resolver = null)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = resolver == null ? XmlResourceAccess.Denied : XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver == null ? new XmlDefaultResolver() : resolver);
    }

    private sealed class KeyResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>();

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string absolute = new Uri(new Uri(baseUri!), systemIdentifier!).AbsoluteUri;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                Input = new XmlInput { Text = Resources[absolute] },
                RequestedUri = systemIdentifier,
                AbsoluteUri = absolute
            };
        }
    }
}
