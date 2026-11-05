using xml;

namespace xml.Tests;

[TestFixture]
public class XsltExecutableBuilderTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void BuildsLinkedExecutionIndexesAndCollectsEveryXPathPlanFamily()
    {
        string source =
            "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema' name='urn:main' package-version='2.1' version='3.0' declared-modes='yes'>" +
            "<xsl:mode name='work' use-accumulators='count'/>" +
            "<xsl:output name='detail' method='xml'/>" +
            "<xsl:variable name='seed' select='2'/>" +
            "<xsl:key name='by-id' match='item' use='@id'/>" +
            "<xsl:accumulator name='count' as='xs:integer' initial-value='0' streamable='yes'><xsl:accumulator-rule match='item' select='$value + 1'/></xsl:accumulator>" +
            "<xsl:attribute-set name='common'><xsl:attribute name='class'/></xsl:attribute-set>" +
            "<xsl:function name='f:double'><xsl:param name='value'/><xsl:sequence select='$value'/></xsl:function>" +
            "<xsl:template name='entry' match='item' mode='work'><xsl:value-of select='f:double($seed)'/></xsl:template>" +
            "</xsl:package>";

        XsltExecutable executable = Compile(source, XsltConformanceLevel.Basic, null, null);
        XsltStylesheetPlan plan = (XsltStylesheetPlan)executable.Plan!;
        XsltPackagePlan package = plan.Package!;
        XsltTemplatePlan template = plan.Templates.Single(item => item.Name == "Q{}entry");
        XsltVariablePlan variable = plan.Variables.Single(item => item.Name.LocalName == "seed");
        XsltFunctionPlan function = plan.Functions.Single();
        XsltModePlan mode = plan.Modes.Single(item => item.Name == "Q{}work");
        XsltAttributeSetPlan attributeSet = plan.AttributeSets.Single();
        XsltKeyDeclarationPlan key = plan.Keys.Single().Declarations.Single();
        XsltAccumulatorDeclarationPlan accumulator = plan.Accumulators.Single().EffectiveDeclaration;
        XsltComponent templateComponent = package.Components.Single(item => item.Kind == XsltComponentKind.Template);
        XsltComponent variableComponent = package.Components.Single(item => item.Kind == XsltComponentKind.Variable && item.Name == "Q{}seed");

        Assert.Multiple(() =>
        {
            Assert.That(plan.IsImmutable, Is.True);
            Assert.That(executable.IsImmutable, Is.True);
            Assert.That(plan.Components, Is.EqualTo(package.Components));
            Assert.That(plan.Globals, Is.EqualTo(new[] { variable }));
            Assert.That(plan.PatternIndexes, Is.EqualTo(plan.TemplateRuleIndexes));
            Assert.That(plan.Outputs, Is.EqualTo(plan.OutputFormats));
            Assert.That(plan.Packages, Is.EqualTo(new[] { package }));
            Assert.That(templateComponent.Template, Is.SameAs(template));
            Assert.That(template.Component, Is.SameAs(templateComponent));
            Assert.That(variableComponent.Variable, Is.SameAs(variable));
            Assert.That(variable.Component, Is.SameAs(variableComponent));
            Assert.That(function.Component!.Function, Is.SameAs(function));
            Assert.That(mode.Component!.Mode, Is.SameAs(mode));
            Assert.That(attributeSet.Component!.AttributeSet, Is.SameAs(attributeSet));
            Assert.That(plan.XPathPlans, Does.Contain(variable.SelectPlan));
            Assert.That(plan.XPathPlans, Does.Contain(key.Use));
            Assert.That(plan.XPathPlans, Does.Contain(accumulator.InitialValue));
            Assert.That(plan.XPathPlans, Does.Contain(accumulator.Rules.Single().Select));
            Assert.That(plan.XPathPlans, Does.Contain(template.MatchPlan!.EquivalentExpression));
            Assert.That(plan.XPathPlans, Does.Contain(function.Expressions.Single(item => item.Name == "select").Plan));
            Assert.That(plan.XPathPlans.Distinct().Count(), Is.EqualTo(plan.XPathPlans.Length));
        });
    }

    [Test]
    public void SnapshotsConformanceOptionsAndCopiedSourceLocations()
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/styles/main.xsl",
            ConformanceLevel = XsltConformanceLevel.Streaming,
            Packages = [],
            ResolverPolicy = Policy()
        };
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='4.0'><xsl:template match='item'><xsl:value-of select='@name'/></xsl:template></xsl:stylesheet>";

        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, new ExecutableResolver());
        XsltStylesheetPlan plan = (XsltStylesheetPlan)executable.Plan!;
        XsltTemplatePlan template = plan.Templates.Single();
        XsltInstructionPlan instruction = plan.Instructions.Single(item => item.Kind == XsltInstructionKind.ValueOf);
        XPathPlan xpath = instruction.Attributes.Single(item => item.Name == "select").Expression!;
        XsltSourceMapEntry templateMap = plan.SourceMap.Single(item => object.ReferenceEquals(item.Item, template));
        XsltSourceMapEntry xpathMap = plan.SourceMap.Single(item => object.ReferenceEquals(item.Item, xpath));
        SourceRange originalRange = template.Declaration.Range!;
        options.BaseUri = "https://changed.test/changed.xsl";
        options.ConformanceLevel = XsltConformanceLevel.Basic;
        options.Packages = [new XsltPackageReference()];

        Assert.Multiple(() =>
        {
            Assert.That(plan.Conformance.Level, Is.EqualTo(XsltConformanceLevel.Streaming));
            Assert.That(plan.Conformance.Streaming, Is.True);
            Assert.That(plan.Conformance.SchemaAware, Is.False);
            Assert.That(executable.IsStreaming, Is.True);
            Assert.That(executable.IsSchemaAware, Is.False);
            Assert.That(plan.Conformance.HasForwardsCompatibleCode, Is.True);
            Assert.That(plan.Conformance.HasBackwardsCompatibleCode, Is.False);
            Assert.That(executable.CompileOptions, Is.Not.SameAs(options));
            Assert.That(executable.CompileOptions!.BaseUri, Is.EqualTo("https://example.test/styles/main.xsl"));
            Assert.That(executable.CompileOptions.ConformanceLevel, Is.EqualTo(XsltConformanceLevel.Streaming));
            Assert.That(executable.CompileOptions.Packages, Is.Empty);
            Assert.That(templateMap.Kind, Is.EqualTo("template"));
            Assert.That(templateMap.SourceUri, Is.EqualTo("https://example.test/styles/main.xsl"));
            Assert.That(templateMap.Range, Is.Not.SameAs(originalRange));
            Assert.That(templateMap.Range!.Start, Is.Not.SameAs(originalRange.Start));
            Assert.That(templateMap.Range.Start!.Line, Is.EqualTo(originalRange.Start!.Line));
            Assert.That(xpathMap.Kind, Is.EqualTo("xpath"));
            Assert.That(xpathMap.Range!.Start!.CharacterOffset, Is.EqualTo(xpath.Range.Start!.CharacterOffset));
            Assert.That(plan.SourceMap.Select(item => item.Item).Distinct().Count(), Is.EqualTo(plan.SourceMap.Length));
        });
    }

    [Test]
    public void RetainsUsedPackageGraphAndImportedComponentLinks()
    {
        string library =
            "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:library' package-version='1.0' version='3.0'>" +
            "<xsl:template name='entry' visibility='public'/>" +
            "<xsl:variable name='value' visibility='public' select='1'/>" +
            "</xsl:package>";
        string source =
            "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:main' package-version='1.0' version='3.0'>" +
            "<xsl:use-package name='urn:library' package-version='1.0'/>" +
            "</xsl:package>";
        ExecutableResolver resolver = new ExecutableResolver();
        resolver.Resources.Add("https://example.test/lib.xsl", library);
        XsltPackageReference reference = new XsltPackageReference
        {
            Name = new XmlName { LocalName = "urn:library" },
            Version = "1.0",
            LocationUri = "lib.xsl"
        };

        XsltStylesheetPlan plan = (XsltStylesheetPlan)Compile(source, XsltConformanceLevel.Basic, resolver, new[] { reference }).Plan!;
        XsltComponent importedTemplate = plan.Components.Single(item => item.Kind == XsltComponentKind.Template && item.Name == "Q{}entry");
        XsltComponent importedVariable = plan.Components.Single(item => item.Kind == XsltComponentKind.Variable && item.Name == "Q{}value");

        Assert.Multiple(() =>
        {
            Assert.That(plan.Packages.Select(item => item.Name), Is.EqualTo(new[] { "urn:main", "urn:library" }));
            Assert.That(plan.Conformance.UsesPackages, Is.True);
            Assert.That(importedTemplate.CorrespondingComponent, Is.Not.Null);
            Assert.That(importedTemplate.Template, Is.SameAs(importedTemplate.CorrespondingComponent!.Template));
            Assert.That(importedVariable.CorrespondingComponent, Is.Not.Null);
            Assert.That(importedVariable.Variable, Is.SameAs(importedVariable.CorrespondingComponent!.Variable));
            Assert.That(plan.SourceMap.Any(item => object.ReferenceEquals(item.Item, plan.Packages[1])), Is.True);
        });
    }

    [TestCase("2.0", true, false)]
    [TestCase("3.0", false, false)]
    [TestCase("4.0", false, true)]
    public void CapturesCompatibilityBehaviorInConformanceFlags(string version, bool backwards, bool forwards)
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='" + version + "'><xsl:template name='entry'/></xsl:stylesheet>";

        XsltStylesheetPlan plan = (XsltStylesheetPlan)Compile(source, XsltConformanceLevel.Basic, null, null).Plan!;

        Assert.Multiple(() =>
        {
            Assert.That(plan.Conformance.HasBackwardsCompatibleCode, Is.EqualTo(backwards));
            Assert.That(plan.Conformance.HasForwardsCompatibleCode, Is.EqualTo(forwards));
        });
    }

    private static XsltExecutable Compile(string source, XsltConformanceLevel level, ExecutableResolver? resolver, XsltPackageReference[]? packages)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ConformanceLevel = level,
            Packages = packages,
            ResolverPolicy = Policy()
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver == null ? new ExecutableResolver() : resolver);
    }

    private static XmlResolverPolicy Policy()
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

    private sealed class ExecutableResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>();

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string requested = systemIdentifier == null ? "" : systemIdentifier;
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
