using xml;

namespace xml.Tests;

[TestFixture]
public class XsltAttributeSetCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void CompilesAttributeInstructionsAndBindsThePackageComponent()
    {
        string source = Package(
            "<xsl:attribute-set name='a:style' streamable='yes' visibility='public'>" +
            "<xsl:attribute name='class'>{$value}</xsl:attribute>" +
            "</xsl:attribute-set><xsl:variable name='value' select=\"'ready'\"/>",
            "xmlns:a='urn:attributes' expand-text='yes'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));
        XsltAttributeSetPlan set = stylesheet.AttributeSets.Single();
        XsltAttributeSetAttributePlan attribute = set.ExpandedAttributes.Single();
        XsltComponent component = stylesheet.Package!.LocalComponents.Single(item => item.Kind == XsltComponentKind.AttributeSet);

        Assert.Multiple(() =>
        {
            Assert.That(set.Name, Is.EqualTo("Q{urn:attributes}style"));
            Assert.That(set.Streamable, Is.True);
            Assert.That(set.Declarations, Has.Length.EqualTo(1));
            Assert.That(attribute.Item.Kind, Is.EqualTo(XsltSequenceItemKind.Instruction));
            Assert.That(attribute.Item.LocalName, Is.EqualTo("attribute"));
            Assert.That(attribute.Item.Content!.Items.Single().TextTemplate, Is.Not.Null);
            Assert.That(set.Component, Is.SameAs(component));
            Assert.That(component.AttributeSet, Is.SameAs(set));
            Assert.That(component.Visibility, Is.EqualTo(XsltComponentVisibility.Public));
        });
    }

    [Test]
    public void MergesDeclarationsByPrecedenceAndDeclarationOrder()
    {
        AttributeSetResolver resolver = new AttributeSetResolver();
        resolver.Resources.Add(
            "https://example.test/base.xsl",
            Stylesheet(
                "<xsl:attribute-set name='base'><xsl:attribute name='base'/></xsl:attribute-set>" +
                "<xsl:attribute-set name='merged' use-attribute-sets='base'><xsl:attribute name='imported'/></xsl:attribute-set>",
                ""));
        string source = Stylesheet(
            "<xsl:import href='base.xsl'/>" +
            "<xsl:attribute-set name='merged'><xsl:attribute name='local-one'/></xsl:attribute-set>" +
            "<xsl:attribute-set name='addon'><xsl:attribute name='addon'/></xsl:attribute-set>" +
            "<xsl:attribute-set name='merged' use-attribute-sets='addon'><xsl:attribute name='local-two'/></xsl:attribute-set>",
            "");

        XsltAttributeSetPlan set = Plan(Compile(source, resolver)).AttributeSets.Single(item => item.Name == "Q{}merged");

        Assert.Multiple(() =>
        {
            Assert.That(set.Declarations, Has.Length.EqualTo(3));
            Assert.That(set.Declarations.Select(item => item.ImportPrecedence), Is.Ordered.Ascending);
            Assert.That(set.Declarations[1].DeclarationOrder, Is.LessThan(set.Declarations[2].DeclarationOrder));
            Assert.That(set.ExpandedAttributes.Select(AttributeName), Is.EqualTo(new[] { "base", "imported", "local-one", "addon", "local-two" }));
            Assert.That(set.Component, Is.Not.Null);
            Assert.That(Plan(Compile(source, resolver)).Package!.LocalComponents.Count(item => item.Kind == XsltComponentKind.AttributeSet && item.Name == "Q{}merged"), Is.EqualTo(1));
        });
    }

    [Test]
    public void ExpandsUseChainsInListOrderAndPreservesRepeatedUses()
    {
        string source = Stylesheet(
            "<xsl:attribute-set name='a'><xsl:attribute name='a'/></xsl:attribute-set>" +
            "<xsl:attribute-set name='b' use-attribute-sets='a'><xsl:attribute name='b'/></xsl:attribute-set>" +
            "<xsl:attribute-set name='c'><xsl:attribute name='c'/></xsl:attribute-set>" +
            "<xsl:attribute-set name='result' use-attribute-sets='b c b'><xsl:attribute name='result'/></xsl:attribute-set>",
            "");

        XsltAttributeSetPlan set = Plan(Compile(source)).AttributeSets.Single(item => item.Name == "Q{}result");

        Assert.Multiple(() =>
        {
            Assert.That(set.Declarations.Single().UsedNames, Is.EqualTo(new[] { "Q{}b", "Q{}c", "Q{}b" }));
            Assert.That(set.Declarations.Single().UsedSets.Select(item => item.Name), Is.EqualTo(new[] { "Q{}b", "Q{}c", "Q{}b" }));
            Assert.That(set.ExpandedAttributes.Select(AttributeName), Is.EqualTo(new[] { "a", "b", "c", "a", "b", "result" }));
        });
    }

    [Test]
    public void CompilesInstructionAndLiteralResultUseSites()
    {
        string source = Stylesheet(
            "<xsl:attribute-set name='first'><xsl:attribute name='first'/></xsl:attribute-set>" +
            "<xsl:attribute-set name='second'><xsl:attribute name='second'/></xsl:attribute-set>" +
            "<xsl:template name='entry'><xsl:element name='one' use-attribute-sets='first second'/>" +
            "<xsl:copy use-attribute-sets='second'/><result xsl:use-attribute-sets='second first'/></xsl:template>",
            "");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));
        XsltAttributeSetUsePlan element = stylesheet.AttributeSetUses.Single(item => item.Node.Name!.LocalName == "element");
        XsltAttributeSetUsePlan copy = stylesheet.AttributeSetUses.Single(item => item.Node.Name!.LocalName == "copy");
        XsltAttributeSetUsePlan literal = stylesheet.AttributeSetUses.Single(item => item.Node.Name!.LocalName == "result");

        Assert.Multiple(() =>
        {
            Assert.That(element.Names, Is.EqualTo(new[] { "Q{}first", "Q{}second" }));
            Assert.That(element.ExpandedAttributes.Select(AttributeName), Is.EqualTo(new[] { "first", "second" }));
            Assert.That(copy.ExpandedAttributes.Select(AttributeName), Is.EqualTo(new[] { "second" }));
            Assert.That(literal.Sets.Select(item => item.Name), Is.EqualTo(new[] { "Q{}second", "Q{}first" }));
            Assert.That(literal.ExpandedAttributes.Select(AttributeName), Is.EqualTo(new[] { "second", "first" }));
        });
    }

    [TestCase("<xsl:attribute-set name='loop' use-attribute-sets='loop'/>")]
    [TestCase("<xsl:attribute-set name='first' use-attribute-sets='second'/><xsl:attribute-set name='second' use-attribute-sets='first'/>")]
    public void RejectsCircularAttributeSetUses(string declarations)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declarations, "")))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0720));
            Assert.That(error.Location, Is.Not.Null);
        });
    }

    [Test]
    public void ResolvesPublicAttributeSetsFromUsedPackages()
    {
        AttributeSetResolver resolver = new AttributeSetResolver();
        resolver.Resources.Add(
            "https://example.test/library.xsl",
            Package("<xsl:attribute-set name='library' visibility='public'><xsl:attribute name='library'/></xsl:attribute-set>", "name='urn:library'"));
        XsltCompileOptions options = Options(resolver);
        options.Packages =
        [
            new XsltPackageReference
            {
                Name = new XmlName { LocalName = "urn:library", QualifiedName = "urn:library" },
                Version = "1",
                LocationUri = "library.xsl"
            }
        ];
        string source = Package(
            "<xsl:use-package name='urn:library'/>" +
            "<xsl:attribute-set name='combined' use-attribute-sets='library'><xsl:attribute name='local'/></xsl:attribute-set>",
            "name='urn:main'");

        XsltAttributeSetPlan set = Plan(Compile(source, resolver, options)).AttributeSets.Single();

        Assert.That(set.ExpandedAttributes.Select(AttributeName), Is.EqualTo(new[] { "library", "local" }));
    }

    [Test]
    public void LinksAUseSiteDirectlyToAUsedPackageAttributeSet()
    {
        AttributeSetResolver resolver = new AttributeSetResolver();
        resolver.Resources.Add(
            "https://example.test/library.xsl",
            Package("<xsl:attribute-set name='library' visibility='public'><xsl:attribute name='library'/></xsl:attribute-set>", "name='urn:library'"));
        XsltCompileOptions options = Options(resolver);
        options.Packages =
        [
            new XsltPackageReference
            {
                Name = new XmlName { LocalName = "urn:library", QualifiedName = "urn:library" },
                Version = "1",
                LocationUri = "library.xsl"
            }
        ];
        string source = Package(
            "<xsl:use-package name='urn:library'/><xsl:template name='entry'><result xsl:use-attribute-sets='library'/></xsl:template>",
            "name='urn:main'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source, resolver, options));

        Assert.Multiple(() =>
        {
            Assert.That(stylesheet.AttributeSets, Is.Empty);
            Assert.That(stylesheet.AttributeSetUses.Single().Sets.Single().Name, Is.EqualTo("Q{}library"));
            Assert.That(stylesheet.AttributeSetUses.Single().ExpandedAttributes.Select(AttributeName), Is.EqualTo(new[] { "library" }));
        });
    }

    [Test]
    public void CompilesAnAttributeSetOverrideThatUsesTheOriginalComponent()
    {
        AttributeSetResolver resolver = new AttributeSetResolver();
        resolver.Resources.Add(
            "https://example.test/library.xsl",
            Package("<xsl:attribute-set name='style' visibility='public'><xsl:attribute name='library'/></xsl:attribute-set>", "name='urn:library'"));
        XsltCompileOptions options = Options(resolver);
        options.Packages =
        [
            new XsltPackageReference
            {
                Name = new XmlName { LocalName = "urn:library", QualifiedName = "urn:library" },
                Version = "1",
                LocationUri = "library.xsl"
            }
        ];
        string source = Package(
            "<xsl:use-package name='urn:library'><xsl:override>" +
            "<xsl:attribute-set name='style' use-attribute-sets='xsl:original'><xsl:attribute name='local'/></xsl:attribute-set>" +
            "</xsl:override></xsl:use-package><xsl:template name='entry'><result xsl:use-attribute-sets='style'/></xsl:template>",
            "name='urn:main'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source, resolver, options));
        XsltAttributeSetPlan set = stylesheet.AttributeSets.Single();

        Assert.Multiple(() =>
        {
            Assert.That(set.Component!.IsOverride, Is.True);
            Assert.That(set.Declarations.Single().UsedSets.Single().Component!.ContainingPackage.Name, Is.EqualTo("urn:library"));
            Assert.That(set.ExpandedAttributes.Select(AttributeName), Is.EqualTo(new[] { "library", "local" }));
            Assert.That(stylesheet.AttributeSetUses.Last().ExpandedAttributes.Select(AttributeName), Is.EqualTo(new[] { "library", "local" }));
        });
    }

    [TestCase("<xsl:attribute-set name='set' use-attribute-sets='missing'/>", XsltErrorCodes.XTSE0710)]
    [TestCase("<xsl:attribute-set name='set'>text</xsl:attribute-set>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:attribute-set name='set'><xsl:value-of select='1'/></xsl:attribute-set>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:attribute-set name='set' streamable='perhaps'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:attribute-set name='set' streamable='yes'/><xsl:attribute-set name='set'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:attribute-set name='set' visibility='public'/><xsl:attribute-set name='set'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:attribute-set name='base'/><xsl:attribute-set name='set' streamable='yes' use-attribute-sets='base'/>", XsltErrorCodes.XTSE0730)]
    [TestCase("<xsl:attribute-set name='set'/><xsl:template name='entry'><xsl:copy use-attribute-sets='bad:name'/></xsl:template>", XsltErrorCodes.XTSE0710)]
    [TestCase("<xsl:attribute-set name='set'/><xsl:template name='entry'><result xsl:use-attribute-sets='missing'/></xsl:template>", XsltErrorCodes.XTSE0710)]
    public void RejectsInvalidAttributeSetDeclarations(string declaration, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declaration, "")))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    private static string AttributeName(XsltAttributeSetAttributePlan attribute)
    {
        return attribute.Instruction.Attributes!.Single(item => item.Name!.LocalName == "name").Value!;
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static string Package(string content, string attributes)
    {
        return "<xsl:package xmlns:xsl='" + Xsl + "' package-version='1' " + attributes + " version='3.0'>" + content + "</xsl:package>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source, IXmlResourceResolver? resolver = null, XsltCompileOptions? configured = null)
    {
        IXmlResourceResolver effectiveResolver = resolver == null ? new XmlDefaultResolver() : resolver;
        XsltCompileOptions options = configured == null ? Options(resolver) : configured;
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, effectiveResolver);
    }

    private static XsltCompileOptions Options(IXmlResourceResolver? resolver)
    {
        return new XsltCompileOptions
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
    }

    private sealed class AttributeSetResolver : IXmlResourceResolver
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
