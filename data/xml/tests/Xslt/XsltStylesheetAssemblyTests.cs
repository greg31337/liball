using xml;

namespace xml.Tests;

[TestFixture]
public class XsltStylesheetAssemblyTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void AssemblesIncludesInPlaceAndAssignsPostOrderImportPrecedence()
    {
        AssemblyResolver resolver = new AssemblyResolver();
        resolver.Resources.Add("https://example.test/inc.xsl", Stylesheet("<xsl:template name='included-before'/><xsl:import href='low.xsl'/><xsl:template name='included-after'/><xsl:include href='tail.xsl'/>", "3.0"));
        resolver.Resources.Add("https://example.test/low.xsl", Stylesheet("<xsl:template name='low'/>", "3.0"));
        resolver.Resources.Add("https://example.test/tail.xsl", Stylesheet("<xsl:template name='tail'/>", "3.0"));
        resolver.Resources.Add("https://example.test/other.xsl", Stylesheet("<xsl:template name='other'/>", "3.0"));
        string source = Stylesheet("<xsl:template name='principal-before'/><xsl:include href='inc.xsl'/><xsl:template name='principal-after'/><xsl:import href='other.xsl'/>", "3.0");

        XsltStylesheetPlan plan = Compile(source, resolver).Plan as XsltStylesheetPlan ?? throw new AssertionException("Missing stylesheet plan.");
        XsltStylesheetLevel root = plan.RootLevel;

        Assert.Multiple(() =>
        {
            Assert.That(plan.Levels.Select(level => level.ImportPrecedence), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(plan.Levels.Select(level => level.InitialModule.BaseUri), Is.EqualTo(new[]
            {
                "https://example.test/low.xsl",
                "https://example.test/other.xsl",
                "https://example.test/main.xsl"
            }));
            Assert.That(root.Modules.Select(module => module.BaseUri), Is.EqualTo(new[]
            {
                "https://example.test/main.xsl",
                "https://example.test/inc.xsl",
                "https://example.test/tail.xsl"
            }));
            Assert.That(root.ImportedLevels.Select(level => level.InitialModule.BaseUri), Is.EqualTo(new[]
            {
                "https://example.test/low.xsl",
                "https://example.test/other.xsl"
            }));
            Assert.That(root.Declarations.Select(DeclarationName), Is.EqualTo(new[]
            {
                "principal-before",
                "included-before",
                "import:low.xsl",
                "included-after",
                "tail",
                "principal-after",
                "import:other.xsl"
            }));
            Assert.That(root.Declarations.Select(declaration => declaration.DeclarationOrder), Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
            Assert.That(root.Declarations.All(declaration => declaration.ImportPrecedence == 2), Is.True);
            Assert.That(plan.Declarations.Select(declaration => declaration.ImportPrecedence), Is.EqualTo(new[] { 0, 1, 2, 2, 2, 2, 2, 2, 2 }));
        });
    }

    [Test]
    public void ConvertsASimplifiedModuleToASyntheticTemplateDeclaration()
    {
        AssemblyResolver resolver = new AssemblyResolver();
        resolver.Resources.Add("https://example.test/simple.xsl", "<result xmlns='urn:result' xmlns:xsl='" + Xsl + "' xsl:version='2.0'/>");
        string source = Stylesheet("<xsl:include href='simple.xsl'/>", "3.0");

        XsltStylesheetPlan plan = Compile(source, resolver).Plan as XsltStylesheetPlan ?? throw new AssertionException("Missing stylesheet plan.");
        XsltStylesheetDeclaration declaration = plan.RootLevel.Declarations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(declaration.LocalName, Is.EqualTo("template"));
            Assert.That(declaration.IsSynthetic, Is.True);
            Assert.That(declaration.Node.Name!.LocalName, Is.EqualTo("result"));
            Assert.That(declaration.EffectiveVersion, Is.EqualTo("2.0"));
            Assert.That(declaration.CompatibilityBehavior, Is.EqualTo(XsltCompatibilityBehavior.Backwards));
        });
    }

    [Test]
    public void RecordsEffectiveCompatibilityBehaviorPerDeclaration()
    {
        string source = Stylesheet("<xsl:variable name='old' version='1.0'/><xsl:variable name='current'/><xsl:variable name='future' version='+0004.00'/>", "3.0");

        XsltStylesheetPlan plan = Compile(source, new AssemblyResolver()).Plan as XsltStylesheetPlan ?? throw new AssertionException("Missing stylesheet plan.");

        Assert.Multiple(() =>
        {
            Assert.That(plan.Declarations.Select(declaration => declaration.EffectiveVersion), Is.EqualTo(new[] { "1.0", "3.0", "+0004.00" }));
            Assert.That(plan.Declarations.Select(declaration => declaration.CompatibilityBehavior), Is.EqualTo(new[]
            {
                XsltCompatibilityBehavior.Backwards,
                XsltCompatibilityBehavior.Standard,
                XsltCompatibilityBehavior.Forwards
            }));
        });
    }

    [Test]
    public void IgnoresUnknownFutureTopLevelDeclarationsAndTheirContents()
    {
        AssemblyResolver resolver = new AssemblyResolver();
        string source = Stylesheet("<xsl:future version='4.0'><xsl:include href='not-loaded.xsl'/></xsl:future><xsl:template name='kept'/>", "3.0");

        XsltStylesheetPlan plan = Compile(source, resolver).Plan as XsltStylesheetPlan ?? throw new AssertionException("Missing stylesheet plan.");

        Assert.Multiple(() =>
        {
            Assert.That(plan.RootLevel.Declarations.Select(DeclarationName), Is.EqualTo(new[] { "kept" }));
            Assert.That(resolver.RequestedUris, Is.Empty);
        });
    }

    [Test]
    public void SelectsFallbackChildrenForUnknownFutureInstructions()
    {
        string source = Stylesheet("<xsl:template name='entry'><xsl:future version='4.0'><xsl:value-of select='bad'/><xsl:fallback><xsl:text>first</xsl:text></xsl:fallback><xsl:fallback><xsl:text>second</xsl:text></xsl:fallback></xsl:future></xsl:template>", "3.0");

        XsltStylesheetPlan plan = Compile(source, new AssemblyResolver()).Plan as XsltStylesheetPlan ?? throw new AssertionException("Missing stylesheet plan.");
        XsltFallbackPlan fallback = plan.Fallbacks.Single();

        Assert.Multiple(() =>
        {
            Assert.That(fallback.Instruction.Name!.LocalName, Is.EqualTo("future"));
            Assert.That(fallback.EffectiveVersion, Is.EqualTo("4.0"));
            Assert.That(fallback.Fallbacks.Select(node => node.Children![0].Name!.LocalName), Is.EqualTo(new[] { "text", "text" }));
            Assert.That(fallback.Fallbacks.Select(node => node.Children![0].Children![0].Value), Is.EqualTo(new[] { "first", "second" }));
        });
    }

    [TestCase("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='3.0'>text</xsl:stylesheet>", XsltErrorCodes.XTSE0120)]
    [TestCase("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='3.0'><data/></xsl:stylesheet>", XsltErrorCodes.XTSE0130)]
    [TestCase("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='3.0'><xsl:unknown/></xsl:stylesheet>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='3.0'><xsl:template name='t'><xsl:include href='a.xsl'/></xsl:template></xsl:stylesheet>", XsltErrorCodes.XTSE0170)]
    [TestCase("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='3.0'><xsl:template name='t'><xsl:import href='a.xsl'/></xsl:template></xsl:stylesheet>", XsltErrorCodes.XTSE0190)]
    [TestCase("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='3.0'><xsl:template name='t'><xsl:unknown version='4.0'/></xsl:template></xsl:stylesheet>", XsltErrorCodes.XTSE0010)]
    public void EnforcesTopLevelAndFallbackConstraints(string source, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new AssemblyResolver()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void AllowsWhitespaceAndNamespacedUserDataAtTopLevel()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:data='urn:data' version='3.0'>\n  <data:metadata><xsl:include href='data-only.xsl'/></data:metadata>\n  <xsl:template name='entry'/>\n</xsl:stylesheet>";

        AssemblyResolver resolver = new AssemblyResolver();
        XsltStylesheetPlan plan = Compile(source, resolver).Plan as XsltStylesheetPlan ?? throw new AssertionException("Missing stylesheet plan.");

        Assert.Multiple(() =>
        {
            Assert.That(plan.Declarations.Select(DeclarationName), Is.EqualTo(new[] { "entry" }));
            Assert.That(resolver.RequestedUris, Is.Empty);
        });
    }

    [Test]
    public void RejectsAnInvalidEffectiveVersionOnANestedElement()
    {
        string source = Stylesheet("<xsl:template name='entry'><xsl:value-of version='future' select='.'/></xsl:template>", "3.0");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new AssemblyResolver()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0110));
    }

    [Test]
    public void ReportsTheImportedModuleAndOffendingNodeLocation()
    {
        AssemblyResolver resolver = new AssemblyResolver();
        resolver.Resources.Add("https://example.test/bad.xsl", "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>\n  <data/>\n</xsl:stylesheet>");
        string source = Stylesheet("<xsl:include href='bad.xsl'/>", "3.0");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, resolver))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0130));
            Assert.That(error.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("https://example.test/bad.xsl"));
            Assert.That(error.Diagnostic.Range.Start.Line, Is.EqualTo(2));
            Assert.That(error.Diagnostic.Range.Start.Column, Is.EqualTo(3));
            Assert.That(error.Diagnostic.IncludeStack, Is.EqualTo(new[] { "https://example.test/main.xsl", "https://example.test/bad.xsl" }));
        });
    }

    private static string DeclarationName(XsltStylesheetDeclaration declaration)
    {
        if (declaration.LocalName == "import")
        {
            XmlNode? href = declaration.Node.Attributes!.Single(attribute => attribute.Name!.LocalName == "href");
            return "import:" + href.Value;
        }

        XmlNode? name = declaration.Node.Attributes == null ? null : declaration.Node.Attributes.SingleOrDefault(attribute => attribute.Name!.LocalName == "name");
        return name == null ? declaration.LocalName : name.Value!;
    }

    private static string Stylesheet(string content, string version)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='" + version + "'>" + content + "</xsl:stylesheet>";
    }

    private static XsltExecutable Compile(string source, AssemblyResolver resolver)
    {
        XmlResolverPolicy policy = new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            AllowedSchemes = ["https"],
            MaximumDepth = 16,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 1048576
        };
        XsltCompileOptions options = new XsltCompileOptions { BaseUri = "https://example.test/main.xsl", ResolverPolicy = policy };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver);
    }

    private sealed class AssemblyResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>(StringComparer.Ordinal);
        internal List<string> RequestedUris = new List<string>();

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            RequestedUris.Add(systemIdentifier ?? "");
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
