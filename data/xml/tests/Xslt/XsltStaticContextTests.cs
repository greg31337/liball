using xml;

namespace xml.Tests;

[TestFixture]
public class XsltStaticContextTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void CreatesInheritedElementSnapshotsForStandardAttributesAndNamespaces()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns='urn:default' xmlns:m='urn:modes' xmlns:ext='urn:extension' xmlns:drop='urn:drop' version='3.0' " +
            "xpath-default-namespace='urn:xpath' default-collation='urn:missing http://www.w3.org/2005/xpath-functions/collation/codepoint' default-mode='m:render' " +
            "default-validation='preserve' expand-text='yes' extension-element-prefixes='ext' exclude-result-prefixes='drop'>" +
            "<xsl:template name='entry'><result xmlns:local='urn:local' xsl:xpath-default-namespace='urn:nested' xsl:default-mode='#unnamed' " +
            "xsl:default-validation='strip' xsl:expand-text='0' xsl:exclude-result-prefixes='local'/></xsl:template></xsl:stylesheet>";

        XsltStylesheetPlan plan = Plan(Compile(source, new StaticContextResolver()));
        XsltStaticContext root = Context(plan, "stylesheet", Xsl);
        XsltStaticContext result = Context(plan, "result", "urn:default");

        Assert.Multiple(() =>
        {
            Assert.That(root.XPathDefaultNamespace, Is.EqualTo("urn:xpath"));
            Assert.That(root.DefaultCollation, Is.EqualTo(XPathNamespaces.CodepointCollation));
            Assert.That(root.DefaultMode, Is.EqualTo("Q{urn:modes}render"));
            Assert.That(root.Validation, Is.EqualTo(XsltDefaultValidation.Preserve));
            Assert.That(root.ExpandText, Is.True);
            Assert.That(root.ExtensionNamespaceUris, Is.EqualTo(new[] { "urn:extension" }));
            Assert.That(root.ExcludedNamespaceUris, Does.Contain(Xsl));
            Assert.That(root.ExcludedNamespaceUris, Does.Contain("urn:extension"));
            Assert.That(root.ExcludedNamespaceUris, Does.Contain("urn:drop"));
            Assert.That(root.BaseUri, Is.EqualTo("https://example.test/main.xsl"));
            Assert.That(root.Namespaces.Single(binding => binding.Prefix == "m").NamespaceUri, Is.EqualTo("urn:modes"));
            Assert.That(result.XPathDefaultNamespace, Is.EqualTo("urn:nested"));
            Assert.That(result.DefaultCollation, Is.EqualTo(root.DefaultCollation));
            Assert.That(result.DefaultMode, Is.EqualTo("#unnamed"));
            Assert.That(result.Validation, Is.EqualTo(XsltDefaultValidation.Strip));
            Assert.That(result.ExpandText, Is.False);
            Assert.That(result.ExtensionNamespaceUris, Is.EqualTo(new[] { "urn:extension" }));
            Assert.That(result.ExcludedNamespaceUris, Does.Contain("urn:drop"));
            Assert.That(result.ExcludedNamespaceUris, Does.Contain("urn:local"));
            Assert.That(plan.InitialMode, Is.EqualTo("Q{urn:modes}render"));
        });
    }

    [Test]
    public void StartsEachIncludedModuleWithTheStandardDefaults()
    {
        StaticContextResolver resolver = new StaticContextResolver();
        resolver.Resources.Add("https://example.test/included.xsl", "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='included'/></xsl:stylesheet>");
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0' xpath-default-namespace='urn:principal' expand-text='yes'><xsl:include href='included.xsl'/></xsl:stylesheet>";

        XsltStylesheetPlan plan = Plan(Compile(source, resolver));
        XsltStaticContext included = plan.StaticContexts.Single(context => context.Module.Relation == XsltStylesheetModuleRelation.Include && object.ReferenceEquals(context.Node, context.Module.Root));

        Assert.Multiple(() =>
        {
            Assert.That(included.XPathDefaultNamespace, Is.Empty);
            Assert.That(included.DefaultCollation, Is.EqualTo(XPathNamespaces.CodepointCollation));
            Assert.That(included.DefaultMode, Is.EqualTo("#unnamed"));
            Assert.That(included.Validation, Is.EqualTo(XsltDefaultValidation.Strip));
            Assert.That(included.ExpandText, Is.False);
            Assert.That(included.ExcludedNamespaceUris, Is.EqualTo(new[] { Xsl }));
        });
    }

    [Test]
    public void TreatsExcludeAllAsANamespaceSnapshotAndAccumulatesDescendantExclusions()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:p='urn:parent' version='3.0' exclude-result-prefixes='#all'>" +
            "<xsl:template name='entry'><outer xmlns:q='urn:child'><inner xsl:exclude-result-prefixes='q'/></outer></xsl:template></xsl:stylesheet>";

        XsltStylesheetPlan plan = Plan(Compile(source, new StaticContextResolver()));
        XsltStaticContext outer = Context(plan, "outer", "");
        XsltStaticContext inner = Context(plan, "inner", "");

        Assert.Multiple(() =>
        {
            Assert.That(outer.ExcludedNamespaceUris, Does.Contain("urn:parent"));
            Assert.That(outer.ExcludedNamespaceUris, Does.Not.Contain("urn:child"));
            Assert.That(inner.ExcludedNamespaceUris, Does.Contain("urn:parent"));
            Assert.That(inner.ExcludedNamespaceUris, Does.Contain("urn:child"));
        });
    }

    [Test]
    public void AppliesVersionBehaviorAtEachElement()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='entry'>" +
            "<xsl:if test='true()' version='1.00'><xsl:sequence select='.' version='2.0'/></xsl:if><xsl:sequence select='.' version='4.0'/>" +
            "</xsl:template></xsl:stylesheet>";

        XsltStylesheetPlan plan = Plan(Compile(source, new StaticContextResolver()));
        XsltStaticContext one = plan.StaticContexts.Single(context => context.EffectiveVersion == "1.00");
        XsltStaticContext two = plan.StaticContexts.Single(context => context.EffectiveVersion == "2.0");
        XsltStaticContext four = plan.StaticContexts.Single(context => context.EffectiveVersion == "4.0");

        Assert.Multiple(() =>
        {
            Assert.That(one.CompatibilityBehavior, Is.EqualTo(XsltCompatibilityBehavior.Backwards));
            Assert.That(one.XPathCompatibilityMode, Is.EqualTo(XPathCompatibilityMode.BackwardsCompatible));
            Assert.That(two.CompatibilityBehavior, Is.EqualTo(XsltCompatibilityBehavior.Backwards));
            Assert.That(two.XPathCompatibilityMode, Is.EqualTo(XPathCompatibilityMode.Strict));
            Assert.That(four.CompatibilityBehavior, Is.EqualTo(XsltCompatibilityBehavior.Forwards));
            Assert.That(four.XPathCompatibilityMode, Is.EqualTo(XPathCompatibilityMode.Strict));
        });
    }

    [Test]
    public void UsesTheDefaultModeForImplicitAndDefaultApplyTemplatesReferences()
    {
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:m='urn:modes' name='urn:package' package-version='1' version='3.0' default-mode='m:render'>" +
            "<xsl:mode name='m:render'/><xsl:template name='entry'><xsl:apply-templates/><xsl:apply-templates mode='#default'/></xsl:template></xsl:package>";

        XsltPackagePlan package = Plan(Compile(source, new StaticContextResolver())).Package!;
        XsltComponent entry = package.Components.Single(component => component.Kind == XsltComponentKind.Template && component.Name == "Q{}entry");

        Assert.Multiple(() =>
        {
            Assert.That(entry.References, Has.Length.EqualTo(2));
            Assert.That(entry.References.Select(reference => reference.Name), Is.EqualTo(new[] { "Q{urn:modes}render", "Q{urn:modes}render" }));
            Assert.That(entry.References.All(reference => reference.Target!.Name == "Q{urn:modes}render"), Is.True);
        });
    }

    [Test]
    public void ResolvesNamespaceAliasesByImportPrecedenceAndDeclarationOrder()
    {
        StaticContextResolver resolver = new StaticContextResolver();
        resolver.Resources.Add("https://example.test/low.xsl", "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:s='urn:source' xmlns:r='urn:low' version='3.0'><xsl:namespace-alias stylesheet-prefix='s' result-prefix='r'/></xsl:stylesheet>");
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:s='urn:source' xmlns:first='urn:high' xmlns:last='urn:high' version='3.0'>" +
            "<xsl:import href='low.xsl'/><xsl:namespace-alias stylesheet-prefix='s' result-prefix='first'/><xsl:namespace-alias stylesheet-prefix='s' result-prefix='last'/></xsl:stylesheet>";

        XsltStylesheetPlan plan = Plan(Compile(source, resolver));
        XsltNamespaceAlias alias = plan.NamespaceAliases.Single();

        Assert.Multiple(() =>
        {
            Assert.That(alias.LiteralNamespaceUri, Is.EqualTo("urn:source"));
            Assert.That(alias.TargetNamespaceUri, Is.EqualTo("urn:high"));
            Assert.That(alias.ResultPrefix, Is.EqualTo("last"));
            Assert.That(alias.ImportPrecedence, Is.GreaterThan(0));
        });
    }

    [TestCase("expand-text='sometimes'", XsltErrorCodes.XTSE0020)]
    [TestCase("default-validation='lax'", XsltErrorCodes.XTSE0020)]
    [TestCase("default-mode='missing:name'", XsltErrorCodes.XTSE0280)]
    [TestCase("default-collation='urn:unavailable'", XsltErrorCodes.XTSE0125)]
    [TestCase("exclude-result-prefixes='missing'", XsltErrorCodes.XTSE0808)]
    [TestCase("exclude-result-prefixes='#default'", XsltErrorCodes.XTSE0809)]
    [TestCase("extension-element-prefixes='missing'", XsltErrorCodes.XTSE1430)]
    [TestCase("extension-element-prefixes='xsl'", XsltErrorCodes.XTSE0085)]
    [TestCase("exclude-result-prefixes='#all xsl'", XsltErrorCodes.XTSE0020)]
    public void RejectsInvalidStandardAttributeValues(string attribute, string expectedCode)
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0' " + attribute + "/>";

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new StaticContextResolver()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void RejectsConflictingAndUnboundNamespaceAliases()
    {
        string conflict = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:s='urn:source' xmlns:a='urn:a' xmlns:b='urn:b' version='3.0'>" +
            "<xsl:namespace-alias stylesheet-prefix='s' result-prefix='a'/><xsl:namespace-alias stylesheet-prefix='s' result-prefix='b'/></xsl:stylesheet>";
        string unbound = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:namespace-alias stylesheet-prefix='missing' result-prefix='#default'/></xsl:stylesheet>";

        XsltStaticException conflictError = Assert.Throws<XsltStaticException>(() => Compile(conflict, new StaticContextResolver()))!;
        XsltStaticException unboundError = Assert.Throws<XsltStaticException>(() => Compile(unbound, new StaticContextResolver()))!;

        Assert.Multiple(() =>
        {
            Assert.That(conflictError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0810));
            Assert.That(unboundError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0812));
        });
    }

    private static XsltStaticContext Context(XsltStylesheetPlan plan, string localName, string namespaceUri)
    {
        return plan.StaticContexts.Single(context => context.Node.Name!.LocalName == localName && (context.Node.Name.NamespaceUri ?? "") == namespaceUri);
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source, StaticContextResolver resolver)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
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

    private sealed class StaticContextResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>(StringComparer.Ordinal);

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
