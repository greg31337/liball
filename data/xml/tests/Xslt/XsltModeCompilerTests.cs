using xml;

namespace xml.Tests;

[TestFixture]
public class XsltModeCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [TestCase("deep-copy", 0)]
    [TestCase("shallow-copy", 1)]
    [TestCase("deep-skip", 2)]
    [TestCase("shallow-skip", 3)]
    [TestCase("text-only-copy", 4)]
    [TestCase("fail", 5)]
    public void CompilesEveryBuiltInOnNoMatchBehavior(string lexical, int expected)
    {
        XsltModePlan mode = Plan(Compile(Stylesheet("<xsl:mode name='work' on-no-match='" + lexical + "'/>", ""), true)).Modes.Single(item => item.Name == "Q{}work");

        Assert.That((int)mode.OnNoMatch, Is.EqualTo(expected));
    }

    [Test]
    public void CompilesModePropertiesAndBindsThePackageComponent()
    {
        string source = Package(
            "<xsl:accumulator name='a:first' initial-value='0' streamable='yes'><xsl:accumulator-rule match='node()' select='$value'/></xsl:accumulator>" +
            "<xsl:accumulator name='Q{urn:a}second' initial-value='0' streamable='yes'><xsl:accumulator-rule match='node()' select='$value'/></xsl:accumulator>" +
            "<xsl:mode name='m:work' streamable='true' use-accumulators='a:first Q{urn:a}second' " +
            "on-no-match='shallow-copy' on-multiple-match='fail' warning-on-no-match='yes' " +
            "warning-on-multiple-match='true' typed='lax' visibility='public'/>",
            "xmlns:m='urn:modes' xmlns:a='urn:a'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source, true));
        XsltModePlan mode = stylesheet.Modes.Single();
        XsltComponent component = stylesheet.Package!.LocalComponents.Single(item => item.Kind == XsltComponentKind.Mode && item.Name == mode.Name);

        Assert.Multiple(() =>
        {
            Assert.That(stylesheet.DeclaredModes, Is.True);
            Assert.That(mode.Name, Is.EqualTo("Q{urn:modes}work"));
            Assert.That(mode.Streamable, Is.True);
            Assert.That(mode.UseAllAccumulators, Is.False);
            Assert.That(mode.AccumulatorNames, Is.EqualTo(new[] { "Q{urn:a}first", "Q{urn:a}second" }));
            Assert.That(mode.Accumulators.Select(item => item.Name), Is.EqualTo(mode.AccumulatorNames));
            Assert.That(mode.OnNoMatch, Is.EqualTo(XsltOnNoMatchBehavior.ShallowCopy));
            Assert.That(mode.OnMultipleMatch, Is.EqualTo(XsltOnMultipleMatchBehavior.Fail));
            Assert.That(mode.WarningOnNoMatch, Is.True);
            Assert.That(mode.WarningOnMultipleMatch, Is.True);
            Assert.That(mode.Typed, Is.EqualTo(XsltTypedMode.Lax));
            Assert.That(mode.HasDeclaredVisibility, Is.True);
            Assert.That(mode.DeclaredVisibility, Is.EqualTo(XsltComponentVisibility.Public));
            Assert.That(mode.Component, Is.SameAs(component));
            Assert.That(component.Mode, Is.SameAs(mode));
            Assert.That(component.Visibility, Is.EqualTo(XsltComponentVisibility.Public));
        });
    }

    [Test]
    public void AppliesModeDefaultsAndCompilesAllAccumulatorsToken()
    {
        string source = Stylesheet("<xsl:mode name='defaults'/><xsl:mode name='all' use-accumulators='#all'/>", "");

        XsltStylesheetPlan stylesheet = Plan(Compile(source, false));
        XsltModePlan defaults = stylesheet.Modes.Single(mode => mode.Name == "Q{}defaults");
        XsltModePlan all = stylesheet.Modes.Single(mode => mode.Name == "Q{}all");

        Assert.Multiple(() =>
        {
            Assert.That(stylesheet.DeclaredModes, Is.False);
            Assert.That(defaults.Streamable, Is.False);
            Assert.That(defaults.AccumulatorNames, Is.Empty);
            Assert.That(defaults.OnNoMatch, Is.EqualTo(XsltOnNoMatchBehavior.TextOnlyCopy));
            Assert.That(defaults.OnMultipleMatch, Is.EqualTo(XsltOnMultipleMatchBehavior.UseLast));
            Assert.That(defaults.WarningOnNoMatch, Is.False);
            Assert.That(defaults.WarningOnMultipleMatch, Is.False);
            Assert.That(defaults.Typed, Is.EqualTo(XsltTypedMode.Unspecified));
            Assert.That(all.UseAllAccumulators, Is.True);
            Assert.That(all.AccumulatorNames, Is.Empty);
        });
    }

    [Test]
    public void CreatesImplicitModesForTemplateAndApplyTemplatesReferences()
    {
        string source = Stylesheet(
            "<xsl:template match='item' mode='rule'/><xsl:template name='entry'><xsl:apply-templates mode='apply'/></xsl:template>",
            "");

        XsltStylesheetPlan stylesheet = Plan(Compile(source, false));
        XsltModePlan rule = stylesheet.Modes.Single(mode => mode.Name == "Q{}rule");
        XsltModePlan apply = stylesheet.Modes.Single(mode => mode.Name == "Q{}apply");

        Assert.Multiple(() =>
        {
            Assert.That(rule.IsImplicit, Is.True);
            Assert.That(apply.IsImplicit, Is.True);
            Assert.That(rule.Component, Is.Not.Null);
            Assert.That(apply.Component, Is.Not.Null);
            Assert.That(rule.Component!.Visibility, Is.EqualTo(XsltComponentVisibility.Private));
            Assert.That(apply.Component!.Visibility, Is.EqualTo(XsltComponentVisibility.Private));
        });
    }

    [Test]
    public void MergesComplementaryDeclarationsIntoOneModeComponent()
    {
        string source = Package(
            "<xsl:mode name='work' streamable='yes' warning-on-no-match='true' visibility='public'/>" +
            "<xsl:mode name='work' streamable='true' on-no-match='deep-skip' on-multiple-match='fail' typed='no'/>",
            "");

        XsltStylesheetPlan stylesheet = Plan(Compile(source, false));
        XsltModePlan mode = stylesheet.Modes.Single();

        Assert.Multiple(() =>
        {
            Assert.That(mode.Declarations, Has.Length.EqualTo(2));
            Assert.That(mode.Streamable, Is.True);
            Assert.That(mode.WarningOnNoMatch, Is.True);
            Assert.That(mode.OnNoMatch, Is.EqualTo(XsltOnNoMatchBehavior.DeepSkip));
            Assert.That(mode.OnMultipleMatch, Is.EqualTo(XsltOnMultipleMatchBehavior.Fail));
            Assert.That(mode.Typed, Is.EqualTo(XsltTypedMode.No));
            Assert.That(stylesheet.Package!.LocalComponents.Count(component => component.Kind == XsltComponentKind.Mode && component.Name == "Q{}work"), Is.EqualTo(1));
            Assert.That(mode.Component!.Visibility, Is.EqualTo(XsltComponentVisibility.Public));
        });
    }

    [Test]
    public void HigherImportPrecedenceOverridesOnlyThePropertiesItSpecifies()
    {
        ModeResolver resolver = new ModeResolver();
        resolver.Resources.Add("https://example.test/base.xsl", Stylesheet("<xsl:mode name='work' streamable='yes' on-no-match='deep-copy' warning-on-no-match='yes'/>", ""));
        string source = Stylesheet("<xsl:import href='base.xsl'/><xsl:mode name='work' on-no-match='shallow-skip'/>", "");

        XsltModePlan mode = Plan(Compile(source, true, resolver)).Modes.Single(item => item.Name == "Q{}work");

        Assert.Multiple(() =>
        {
            Assert.That(mode.Declarations, Has.Length.EqualTo(2));
            Assert.That(mode.Streamable, Is.True);
            Assert.That(mode.OnNoMatch, Is.EqualTo(XsltOnNoMatchBehavior.ShallowSkip));
            Assert.That(mode.WarningOnNoMatch, Is.True);
        });
    }

    [Test]
    public void EnforcesDeclaredModesForExplicitPackages()
    {
        string source = Package("<xsl:template name='entry'><xsl:apply-templates mode='missing'/></xsl:template>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, false))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3085));
    }

    [Test]
    public void AllowsImplicitModesWhenExplicitPackageOptsOut()
    {
        string source = Package("<xsl:template name='entry'><xsl:apply-templates mode='created'/></xsl:template>", "declared-modes='no'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source, false));
        XsltModePlan mode = stylesheet.Modes.Single(item => item.Name == "Q{}created");

        Assert.Multiple(() =>
        {
            Assert.That(stylesheet.DeclaredModes, Is.False);
            Assert.That(mode.IsImplicit, Is.True);
            Assert.That(mode.Component, Is.Not.Null);
        });
    }

    [TestCase("<xsl:mode name='work' on-no-match='deep-copy'/><xsl:mode name='work' on-no-match='fail'/>")]
    [TestCase("<xsl:mode name='work' warning-on-no-match='yes'/><xsl:mode name='work' warning-on-no-match='no'/>")]
    [TestCase("<xsl:mode name='work' use-accumulators='first'/><xsl:mode name='work' use-accumulators='second'/>")]
    [TestCase("<xsl:mode name='work' visibility='public'/><xsl:mode name='work' visibility='final'/>")]
    public void RejectsConflictingPropertiesAtTheSameImportPrecedence(string declarations)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Package(declarations, ""), true))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0545));
    }

    [TestCase("on-no-match='unknown'", XsltErrorCodes.XTSE0020)]
    [TestCase("on-multiple-match='unknown'", XsltErrorCodes.XTSE0020)]
    [TestCase("typed='unknown'", XsltErrorCodes.XTSE0020)]
    [TestCase("streamable='perhaps'", XsltErrorCodes.XTSE0020)]
    [TestCase("use-accumulators='#all first'", XsltErrorCodes.XTSE3300)]
    [TestCase("use-accumulators='first first'", XsltErrorCodes.XTSE3300)]
    [TestCase("use-accumulators='bad:name'", XsltErrorCodes.XTSE3300)]
    public void RejectsInvalidModeProperties(string attribute, string expectedCode)
    {
        string source = Stylesheet("<xsl:mode name='work' " + attribute + "/>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, true))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [TestCase("yes")]
    [TestCase("strict")]
    public void TypedModesThatRequireTypeAnnotationsNeedSchemaAwareness(string typed)
    {
        string source = Stylesheet("<xsl:mode name='work' typed='" + typed + "'/>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, false))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE1660));
    }

    [TestCase("lax", 4)]
    [TestCase("no", 2)]
    [TestCase("unspecified", 0)]
    public void BasicProcessorAcceptsOtherTypedModeValues(string lexical, int expected)
    {
        string source = Stylesheet("<xsl:mode name='work' typed='" + lexical + "'/>", "");

        Assert.That((int)Plan(Compile(source, false)).Modes.Single(mode => mode.Name == "Q{}work").Typed, Is.EqualTo(expected));
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static string Package(string content, string attributes)
    {
        return "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:test' package-version='1' " + attributes + " version='3.0'>" + content + "</xsl:package>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source, bool schemaAware, IXmlResourceResolver? resolver = null)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ConformanceLevel = schemaAware ? XsltConformanceLevel.SchemaAware : XsltConformanceLevel.Basic,
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

    private sealed class ModeResolver : IXmlResourceResolver
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
