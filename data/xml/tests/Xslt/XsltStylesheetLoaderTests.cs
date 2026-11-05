using xml;

namespace xml.Tests;

[TestFixture]
public class XsltStylesheetLoaderTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [TestCase("stylesheet", 0)]
    [TestCase("transform", 1)]
    [TestCase("package", 3)]
    public void CompilesRecognizedPrincipalRootForms(string localName, int expectedKind)
    {
        XmlInput input = new XmlInput { Text = "<xsl:" + localName + " xmlns:xsl='" + Xsl + "' version='3.0'/>" };
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/styles/main.xsl",
            ConformanceLevel = XsltConformanceLevel.SchemaAware
        };

        XsltExecutable executable = new XsltCompiler().CompileXslt(input, options, new StylesheetResolver());
        XsltStylesheetPlan plan = (XsltStylesheetPlan)executable.Plan!;
        XsltStylesheetModule module = plan.PrincipalModule;
        options.BaseUri = "https://changed.test/";

        Assert.Multiple(() =>
        {
            Assert.That((int)module.Kind, Is.EqualTo(expectedKind));
            Assert.That(module.Relation, Is.EqualTo(XsltStylesheetModuleRelation.Principal));
            Assert.That(module.Version, Is.EqualTo("3.0"));
            Assert.That(module.BaseUri, Is.EqualTo("https://example.test/styles/main.xsl"));
            Assert.That(module.Document.BaseUri, Is.EqualTo(module.BaseUri));
            Assert.That(module.Document.DocumentUri, Is.EqualTo(module.BaseUri));
            Assert.That(module.Document.IsReadOnly, Is.True);
            Assert.That(module.Root.IsReadOnly, Is.True);
            Assert.That(module.IsComplete, Is.True);
            Assert.That(plan.Modules, Is.EqualTo(new[] { module }));
            Assert.That(plan.IsImmutable, Is.True);
            Assert.That(executable.IsImmutable, Is.True);
            Assert.That(executable.IsSchemaAware, Is.True);
            Assert.That(executable.Diagnostics, Is.Empty);
            Assert.That(executable.CompileOptions, Is.Not.SameAs(options));
            Assert.That(executable.CompileOptions!.BaseUri, Is.EqualTo("https://example.test/styles/main.xsl"));
            Assert.That(input.BaseUri, Is.Null);
        });
    }

    [Test]
    public void RecognizesSimplifiedStylesheetsAndTheirXsltVersion()
    {
        string source = "<html xmlns='http://www.w3.org/1999/xhtml' xmlns:xsl='" + Xsl + "' xsl:version=' 3.0 '>" +
            "<body><xsl:value-of select='.'/></body></html>";

        XsltExecutable executable = Compile(source, "https://example.test/simple.xsl", new StylesheetResolver(), null);
        XsltStylesheetModule module = ((XsltStylesheetPlan)executable.Plan!).PrincipalModule;

        Assert.Multiple(() =>
        {
            Assert.That(module.Kind, Is.EqualTo(XsltStylesheetModuleKind.Simplified));
            Assert.That(module.Version, Is.EqualTo("3.0"));
            Assert.That(module.Root.Name!.LocalName, Is.EqualTo("html"));
            Assert.That(module.Root.Name.NamespaceUri, Is.EqualTo("http://www.w3.org/1999/xhtml"));
            Assert.That(module.References, Is.Empty);
        });
    }

    [Test]
    public void LoadsIncludeAndImportGraphsUsingEffectiveBaseUris()
    {
        StylesheetResolver resolver = new StylesheetResolver();
        resolver.Resources.Add("https://example.test/styles/modules/shared.xsl", "<xsl:transform xmlns:xsl='" + Xsl + "' version='2.0'><xsl:import href='../base.xsl'/></xsl:transform>");
        resolver.Resources.Add("https://example.test/styles/base.xsl", "<result xmlns:xsl='" + Xsl + "' xsl:version='4.0'/>");
        XmlResolverPolicy policy = AllowedPolicy();
        XsltCompileOptions options = new XsltCompileOptions { BaseUri = "https://example.test/styles/main.xsl", ResolverPolicy = policy };
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:include xml:base='modules/' href='shared.xsl'/></xsl:stylesheet>";

        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver);
        XsltStylesheetPlan plan = (XsltStylesheetPlan)executable.Plan!;
        policy.AllowedSchemes = ["file"];

        Assert.Multiple(() =>
        {
            Assert.That(plan.Modules, Has.Length.EqualTo(3));
            Assert.That(plan.Modules.Select(module => module.Relation).ToArray(), Is.EqualTo(new[]
            {
                XsltStylesheetModuleRelation.Principal,
                XsltStylesheetModuleRelation.Include,
                XsltStylesheetModuleRelation.Import
            }));
            Assert.That(plan.Modules.Select(module => module.Kind).ToArray(), Is.EqualTo(new[]
            {
                XsltStylesheetModuleKind.Stylesheet,
                XsltStylesheetModuleKind.Transform,
                XsltStylesheetModuleKind.Simplified
            }));
            Assert.That(plan.Modules.Select(module => module.Version).ToArray(), Is.EqualTo(new[] { "3.0", "2.0", "4.0" }));
            Assert.That(plan.Modules.Select(module => module.BaseUri).ToArray(), Is.EqualTo(new[]
            {
                "https://example.test/styles/main.xsl",
                "https://example.test/styles/modules/shared.xsl",
                "https://example.test/styles/base.xsl"
            }));
            Assert.That(plan.PrincipalModule.References[0].BaseUri, Is.EqualTo("https://example.test/styles/modules/"));
            Assert.That(plan.PrincipalModule.References[0].Target, Is.SameAs(plan.Modules[1]));
            Assert.That(plan.Modules[1].References[0].Target, Is.SameAs(plan.Modules[2]));
            Assert.That(resolver.RequestedUris, Is.EqualTo(new[] { "shared.xsl", "../base.xsl" }));
            Assert.That(resolver.BaseUris, Is.EqualTo(new[] { "https://example.test/styles/modules/", "https://example.test/styles/modules/shared.xsl" }));
            Assert.That(resolver.Policies.All(value => value.AllowedSchemes!.SequenceEqual(new[] { "https" })), Is.True);
            Assert.That(executable.CompileOptions!.ResolverPolicy!.AllowedSchemes, Is.EqualTo(new[] { "https" }));
        });
    }

    [TestCase("include", XsltErrorCodes.XTSE0180)]
    [TestCase("import", XsltErrorCodes.XTSE0210)]
    public void RejectsDirectModuleCycles(string directive, string expectedCode)
    {
        StylesheetResolver resolver = new StylesheetResolver();
        resolver.Resources.Add("https://example.test/main.xsl", "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'/>");
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:" + directive + " href='main.xsl'/></xsl:stylesheet>";

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, "https://example.test/main.xsl", resolver, AllowedPolicy()))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
            Assert.That(error.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XsltStatic));
            Assert.That(error.Diagnostic.IncludeStack, Is.EqualTo(new[] { "https://example.test/main.xsl" }));
        });
    }

    [TestCase("<xsl:template xmlns:xsl='http://www.w3.org/1999/XSL/Transform'/>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform'/>", XsltErrorCodes.XTSE0110)]
    [TestCase("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='three'/>", XsltErrorCodes.XTSE0110)]
    [TestCase("<html/>", XsltErrorCodes.XTSE0150)]
    [TestCase("<html xmlns:xsl='http://www.w3.org/1999/XSL/Transform' xsl:version='3e0'/>", XsltErrorCodes.XTSE0150)]
    [TestCase("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='3.0'>", XsltErrorCodes.XTSE0010)]
    public void RejectsInvalidPrincipalModuleRootsAndVersions(string source, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, "https://example.test/main.xsl", new StylesheetResolver(), null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void ReportsUnavailableMalformedAndNonStylesheetReferencedModules()
    {
        string principal = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:include href='module.xsl'/></xsl:stylesheet>";
        StylesheetResolver unavailableResolver = new StylesheetResolver { ThrowOnResolve = true };
        StylesheetResolver malformedResolver = new StylesheetResolver();
        malformedResolver.Resources.Add("https://example.test/module.xsl", "<xsl:stylesheet");
        StylesheetResolver packageResolver = new StylesheetResolver();
        packageResolver.Resources.Add("https://example.test/module.xsl", "<xsl:package xmlns:xsl='" + Xsl + "' version='3.0'/>");
        StylesheetResolver wrongKindResolver = new StylesheetResolver { ReturnWrongKind = true };
        wrongKindResolver.Resources.Add("https://example.test/module.xsl", "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'/>");

        XsltStaticException unavailable = Assert.Throws<XsltStaticException>(() => Compile(principal, "https://example.test/main.xsl", unavailableResolver, AllowedPolicy()))!;
        XsltStaticException malformed = Assert.Throws<XsltStaticException>(() => Compile(principal, "https://example.test/main.xsl", malformedResolver, AllowedPolicy()))!;
        XsltStaticException package = Assert.Throws<XsltStaticException>(() => Compile(principal, "https://example.test/main.xsl", packageResolver, AllowedPolicy()))!;
        XsltStaticException wrongKind = Assert.Throws<XsltStaticException>(() => Compile(principal, "https://example.test/main.xsl", wrongKindResolver, AllowedPolicy()))!;

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0165));
            Assert.That(malformed.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0165));
            Assert.That(package.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0165));
            Assert.That(wrongKind.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0165));
        });
    }

    [Test]
    public void AppliesTheResolverGuardPolicyBeforeCallingTheResolver()
    {
        StylesheetResolver resolver = new StylesheetResolver();
        resolver.Resources.Add("https://example.test/module.xsl", "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'/>");
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:include href='module.xsl'/></xsl:stylesheet>";
        XmlResolverPolicy policy = AllowedPolicy();
        policy.AllowedSchemes = ["file"];

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, "https://example.test/main.xsl", resolver, policy))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0165));
            Assert.That(resolver.RequestedUris, Is.Empty);
        });
    }

    [Test]
    public void AppliesTheResolverPolicyDepthAcrossTheModuleGraph()
    {
        StylesheetResolver resolver = new StylesheetResolver();
        resolver.Resources.Add("https://example.test/first.xsl", "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:include href='second.xsl'/></xsl:stylesheet>");
        resolver.Resources.Add("https://example.test/second.xsl", "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'/>");
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:include href='first.xsl'/></xsl:stylesheet>";
        XmlResolverPolicy policy = AllowedPolicy();
        policy.MaximumDepth = 1;

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, "https://example.test/main.xsl", resolver, policy))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0165));
            Assert.That(resolver.RequestedUris, Is.EqualTo(new[] { "first.xsl" }));
        });
    }

    private static XsltExecutable Compile(string source, string baseUri, StylesheetResolver resolver, XmlResolverPolicy? policy)
    {
        XsltCompileOptions options = new XsltCompileOptions { BaseUri = baseUri, ResolverPolicy = policy };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver);
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        return new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            AllowedSchemes = ["https"],
            MaximumDepth = 16,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 1048576
        };
    }

    private sealed class StylesheetResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>(StringComparer.Ordinal);
        internal List<string> RequestedUris = new List<string>();
        internal List<string?> BaseUris = new List<string?>();
        internal List<XmlResolverPolicy> Policies = new List<XmlResolverPolicy>();
        internal bool ThrowOnResolve;
        internal bool ReturnWrongKind;

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            RequestedUris.Add(systemIdentifier == null ? "" : systemIdentifier);
            BaseUris.Add(baseUri);
            Policies.Add(policy);
            if (ThrowOnResolve)
            {
                throw new InvalidOperationException("unavailable");
            }

            string absolute = new Uri(new Uri(baseUri!), systemIdentifier!).AbsoluteUri;
            string source = Resources[absolute];
            return new XmlResolvedResource
            {
                Kind = ReturnWrongKind ? XmlResourceKind.Schema : kind,
                Access = XmlResourceAccess.Allowed,
                Input = new XmlInput { Text = source },
                RequestedUri = systemIdentifier,
                AbsoluteUri = absolute
            };
        }
    }
}
