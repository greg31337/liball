using xml;

namespace xml.Tests;

[TestFixture]
public class XsltStaticPreprocessorTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void EvaluatesStaticBindingsUseWhenAndShadowHrefInStylesheetTreeOrder()
    {
        PreprocessingResolver resolver = new PreprocessingResolver();
        resolver.Resources.Add("https://example.test/module-selected.xsl", Stylesheet("<xsl:template name='included'/>", ""));
        string source = Stylesheet(
            "<xsl:param name='enabled' static='yes' select='true()'/>" +
            "<xsl:variable name='variant' static='yes' select=\"'selected'\"/>" +
            "<xsl:include _href=\"{'module-' || $variant || '.xsl'}\" use-when='$enabled'/>" +
            "<xsl:template name='discarded' use-when='false()'><xsl:not-an-instruction/></xsl:template>", "");

        XsltStylesheetPlan plan = Plan(Compile(source, resolver, null));

        Assert.Multiple(() =>
        {
            Assert.That(plan.StaticVariables.Select(variable => variable.ExpandedName), Is.EqualTo(new[] { "Q{}enabled", "Q{}variant" }));
            Assert.That(AtomicString(plan.StaticVariables[0].Value), Is.EqualTo("true"));
            Assert.That(AtomicString(plan.StaticVariables[1].Value), Is.EqualTo("selected"));
            Assert.That(plan.Modules, Has.Length.EqualTo(2));
            Assert.That(resolver.RequestedUris, Is.EqualTo(new[] { "module-selected.xsl" }));
            Assert.That(plan.Declarations.Any(declaration => declaration.LocalName == "template" && declaration.Node.Name != null && declaration.Node.Name.LocalName == "not-an-instruction"), Is.False);
            Assert.That(plan.Package!.Components.Any(component => component.Name == "Q{}discarded"), Is.False);
            Assert.That(plan.Package.Components.Any(component => component.Name == "Q{}included"), Is.True);
        });
    }

    [Test]
    public void SuppliedRequiredStaticParameterControlsUseWhenAndOverridesOrdinaryAttribute()
    {
        XsltParameter supplied = StaticParameter("choice", "on");
        string source = Stylesheet(
            "<xsl:param name='choice' static='yes' required='yes'/>" +
            "<xsl:template name='not a qname' _name=\"{'entry-' || $choice}\" use-when=\"$choice = 'on'\"/>", "");

        XsltStylesheetPlan plan = Plan(Compile(source, new PreprocessingResolver(), [supplied]));
        XsltStaticVariable parameter = plan.StaticVariables.Single();

        Assert.Multiple(() =>
        {
            Assert.That(parameter.IsParameter, Is.True);
            Assert.That(parameter.IsRequired, Is.True);
            Assert.That(parameter.WasSupplied, Is.True);
            Assert.That(AtomicString(parameter.Value), Is.EqualTo("on"));
            Assert.That(plan.Package!.Components.Any(component => component.Name == "Q{}entry-on"), Is.True);
            Assert.That(plan.Package.Components.Any(component => component.Name == "Q{}not a qname"), Is.False);
        });
    }

    [Test]
    public void SharesEarlierStaticBindingsWithIncludedModules()
    {
        PreprocessingResolver resolver = new PreprocessingResolver();
        resolver.Resources.Add("https://example.test/included.xsl",
            "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0' _xpath-default-namespace=\"{$namespace}\">" +
            "<xsl:template name='included' use-when=\"$namespace = 'urn:selected'\"/></xsl:stylesheet>");
        string source = Stylesheet("<xsl:variable name='namespace' static='yes' select=\"'urn:selected'\"/><xsl:include href='included.xsl'/>", "");

        XsltStylesheetPlan plan = Plan(Compile(source, resolver, null));
        XsltStaticContext includedRoot = plan.StaticContexts.Single(context => context.Module.Relation == XsltStylesheetModuleRelation.Include && object.ReferenceEquals(context.Node, context.Module.Root));

        Assert.Multiple(() =>
        {
            Assert.That(includedRoot.XPathDefaultNamespace, Is.EqualTo("urn:selected"));
            Assert.That(plan.Package!.Components.Any(component => component.Name == "Q{}included"), Is.True);
        });
    }

    [Test]
    public void DoesNotResolveAnIncludeExcludedByUseWhen()
    {
        string source = Stylesheet("<xsl:include href='missing.xsl' use-when='false()'/><xsl:template name='kept'/>", "");
        PreprocessingResolver resolver = new PreprocessingResolver();

        XsltStylesheetPlan plan = Plan(Compile(source, resolver, null));

        Assert.Multiple(() =>
        {
            Assert.That(resolver.RequestedUris, Is.Empty);
            Assert.That(plan.Modules, Has.Length.EqualTo(1));
            Assert.That(plan.Package!.Components.Any(component => component.Name == "Q{}kept"), Is.True);
        });
    }

    [Test]
    public void ProvidesXsltAvailabilityFunctionsToStaticExpressions()
    {
        string condition = "system-property('xsl:version') = '3.0' and element-available('xsl:if') and function-available('fn:concat', 2) and type-available('xs:string')";
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:fn='http://www.w3.org/2005/xpath-functions' xmlns:xs='http://www.w3.org/2001/XMLSchema' version='3.0'>" +
            "<xsl:template name='available' use-when=\"" + condition + "\"/></xsl:stylesheet>";

        XsltStylesheetPlan plan = Plan(Compile(source, new PreprocessingResolver(), null));

        Assert.That(plan.Package!.Components.Any(component => component.Name == "Q{}available"), Is.True);
    }

    [Test]
    public void ExcludingTheModuleRootRetainsTheRootAndRemovesItsChildren()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0' use-when='false()'><xsl:not-an-instruction/></xsl:stylesheet>";

        XsltStylesheetPlan plan = Plan(Compile(source, new PreprocessingResolver(), null));

        Assert.Multiple(() =>
        {
            Assert.That(plan.PrincipalModule.Root, Is.Not.Null);
            Assert.That(plan.PrincipalModule.Root.Children, Is.Empty);
            Assert.That(plan.Declarations, Is.Empty);
        });
    }

    [Test]
    public void PermitsConsistentStaticOverridesAtHigherImportPrecedence()
    {
        PreprocessingResolver resolver = new PreprocessingResolver();
        resolver.Resources.Add("https://example.test/base.xsl", Stylesheet("<xsl:variable name='setting' static='yes' select=\"'same'\"/>", ""));
        string source = Stylesheet("<xsl:import href='base.xsl'/><xsl:variable name='setting' static='yes' select=\"'same'\"/>", "");

        XsltStylesheetPlan plan = Plan(Compile(source, resolver, null));

        Assert.Multiple(() =>
        {
            Assert.That(plan.StaticVariables, Has.Length.EqualTo(2));
            Assert.That(plan.StaticVariables[0].ImportPrecedence, Is.LessThan(plan.StaticVariables[1].ImportPrecedence));
        });
    }

    [TestCase("<xsl:param name='required' static='yes' required='yes'/>", XsltErrorCodes.XTDE0050)]
    [TestCase("<xsl:variable name='cycle' static='yes' select='$cycle + 1'/>", XsltErrorCodes.XTDE0640)]
    [TestCase("<xsl:template name='entry' use-when='.'/>", XPathErrorCodes.XPDY0002)]
    [TestCase("<xsl:template name='entry'><xsl:variable name='local' static='yes' select='1'/></xsl:template>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:variable name='missing' static='yes'/>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:variable name='content' static='yes' select='1'><xsl:sequence select='1'/></xsl:variable>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:template _name='{1'/>", XsltErrorCodes.XTSE0350)]
    public void ReportsStaticPreprocessingFailures(string content, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(content, ""), new PreprocessingResolver(), null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void RejectsAmbiguousAndInconsistentStaticDeclarations()
    {
        string duplicate = Stylesheet("<xsl:variable name='setting' static='yes' select='1'/><xsl:variable name='setting' static='yes' select='1'/>", "");
        PreprocessingResolver resolver = new PreprocessingResolver();
        resolver.Resources.Add("https://example.test/base.xsl", Stylesheet("<xsl:variable name='setting' static='yes' select='1'/>", ""));
        string inconsistent = Stylesheet("<xsl:import href='base.xsl'/><xsl:variable name='setting' static='yes' select='2'/>", "");

        XsltStaticException duplicateError = Assert.Throws<XsltStaticException>(() => Compile(duplicate, new PreprocessingResolver(), null))!;
        XsltStaticException inconsistentError = Assert.Throws<XsltStaticException>(() => Compile(inconsistent, resolver, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(duplicateError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0630));
            Assert.That(inconsistentError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3450));
        });
    }

    [Test]
    public void KeepsStaticBindingsInsideTheirCompiledPackage()
    {
        PreprocessingResolver resolver = new PreprocessingResolver();
        resolver.Resources.Add("https://example.test/library.xsl",
            "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:library' package-version='1' version='3.0'>" +
            "<xsl:variable name='secret' static='yes' select='true()'/></xsl:package>");
        XsltPackageReference package = new XsltPackageReference
        {
            Name = new XmlName { LocalName = "urn:library" },
            Version = "1",
            LocationUri = "library.xsl"
        };
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:main' package-version='1' version='3.0'>" +
            "<xsl:use-package name='urn:library'/><xsl:template name='entry' use-when='$secret'/></xsl:package>";

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, resolver, null, [package]))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0008));
    }

    private static string AtomicString(XdmSequence value)
    {
        XdmSequenceItem item = new XdmSequenceOperations().ReadXdmSequenceItem(value, 0);
        return new XdmTypeConverter().ReadXdmAtomicStringValue(item.AtomicValue!);
    }

    private static XsltParameter StaticParameter(string name, string value)
    {
        XdmAtomicValue atomic = new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String);
        return new XsltParameter
        {
            Name = new XmlName { LocalName = name, NamespaceUri = "", QualifiedName = name },
            Value = new XdmSequenceOperations().CreateXdmAtomicSequence(atomic),
            IsStatic = true
        };
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0' " + attributes + ">" + content + "</xsl:stylesheet>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source, PreprocessingResolver resolver, XsltParameter[]? parameters, XsltPackageReference[]? packages = null)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            StaticParameters = parameters,
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

    private sealed class PreprocessingResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>(StringComparer.Ordinal);
        internal List<string> RequestedUris = new List<string>();

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string requested = systemIdentifier ?? "";
            RequestedUris.Add(requested);
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
