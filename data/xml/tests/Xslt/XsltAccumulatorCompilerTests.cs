using xml;

namespace xml.Tests;

[TestFixture]
public class XsltAccumulatorCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void CompilesInitialValuesRulesTypesPhasesAndDependencies()
    {
        string source = Stylesheet(
            "<xsl:variable name='seed' select='2'/>" +
            "<xsl:accumulator name='count' as='xs:integer' initial-value='0' streamable='yes'>" +
            "<xsl:accumulator-rule match='item' select='$value + 1'/>" +
            "</xsl:accumulator>" +
            "<xsl:accumulator name='a:total' as='xs:integer' initial-value='$seed' streamable='true'>" +
            "<xsl:accumulator-rule match='item' phase='end' select='$value + count(accumulator-before(\"count\"))'/>" +
            "</xsl:accumulator>",
            "xmlns:a='urn:acc' xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));
        XsltAccumulatorPlan total = stylesheet.Accumulators.Single(item => item.Name == "Q{urn:acc}total");
        XsltAccumulatorDeclarationPlan declaration = total.EffectiveDeclaration;
        XsltAccumulatorRulePlan rule = declaration.Rules.Single();

        Assert.Multiple(() =>
        {
            Assert.That(stylesheet.Accumulators, Has.Length.EqualTo(2));
            Assert.That(declaration.ResolvedName.LocalName, Is.EqualTo("total"));
            Assert.That(declaration.InitialValueSource, Is.EqualTo("$seed"));
            Assert.That(declaration.InitialValueType.ItemKind, Is.EqualTo(XPathStaticItemKind.Atomic));
            Assert.That(declaration.SequenceTypeSource, Is.EqualTo("xs:integer"));
            Assert.That(declaration.SequenceType.AtomicType!.Type, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(declaration.Streamable, Is.True);
            Assert.That(declaration.Dependencies.Single().Name.LocalName, Is.EqualTo("seed"));
            Assert.That(declaration.InitialValueIsGrounded, Is.True);
            Assert.That(declaration.InitialValueIsMotionless, Is.True);
            Assert.That(rule.MatchSource, Is.EqualTo("item"));
            Assert.That(rule.Match.IsMotionless, Is.True);
            Assert.That(rule.Phase, Is.EqualTo(XsltAccumulatorPhase.End));
            Assert.That(rule.ValueKind, Is.EqualTo(XsltAccumulatorValueKind.Expression));
            Assert.That(rule.SelectType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Atomic));
            Assert.That(rule.ValueIsGrounded, Is.True);
            Assert.That(rule.ValueIsMotionless, Is.True);
            Assert.That(rule.AccumulatorDependencies.Single().Name, Is.EqualTo("Q{}count"));
            Assert.That(rule.AccumulatorDependencies.Single().ValuePhase, Is.EqualTo(XsltAccumulatorPhase.Start));
            Assert.That(rule.AccumulatorDependencies.Single().Target, Is.SameAs(stylesheet.Accumulators.Single(item => item.Name == "Q{}count")));
            Assert.That(total.Dependencies.Single().Name, Is.EqualTo("Q{}count"));
            Assert.That(declaration.IsGuaranteedStreamableCandidate, Is.True);
            Assert.That(stylesheet.Accumulators.Single(item => item.Name == "Q{}count").EffectiveDeclaration.Rules.Single().Phase, Is.EqualTo(XsltAccumulatorPhase.Start));
        });
    }

    [Test]
    public void CompilesASequenceConstructorRuleValue()
    {
        string source = Stylesheet(
            "<xsl:accumulator name='content' initial-value='()'>" +
            "<xsl:accumulator-rule match='item'><xsl:sequence select='$value'/><xsl:text>next</xsl:text></xsl:accumulator-rule>" +
            "</xsl:accumulator>",
            "");

        XsltAccumulatorRulePlan rule = Plan(Compile(source)).Accumulators.Single().EffectiveDeclaration.Rules.Single();

        Assert.Multiple(() =>
        {
            Assert.That(rule.ValueKind, Is.EqualTo(XsltAccumulatorValueKind.Content));
            Assert.That(rule.Select, Is.Null);
            Assert.That(rule.Content, Is.Not.Null);
            Assert.That(rule.Content!.Items.Select(item => item.LocalName), Is.EqualTo(new[] { "sequence", "text" }));
        });
    }

    [Test]
    public void CollectsDependenciesFromRulePatterns()
    {
        string source = Stylesheet(
            "<xsl:variable name='enabled' select='true()'/>" +
            "<xsl:accumulator name='base' initial-value='0'><xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator>" +
            "<xsl:accumulator name='dependent' initial-value='0'>" +
            "<xsl:accumulator-rule match=\"item[$enabled and exists(accumulator-before('base'))]\" select='$value'/>" +
            "</xsl:accumulator>",
            "");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));
        XsltAccumulatorPlan accumulator = stylesheet.Accumulators.Single(item => item.Name == "Q{}dependent");
        XsltAccumulatorRulePlan rule = accumulator.EffectiveDeclaration.Rules.Single();

        Assert.Multiple(() =>
        {
            Assert.That(rule.Dependencies.Single().Name.LocalName, Is.EqualTo("enabled"));
            Assert.That(rule.AccumulatorDependencies.Single().Name, Is.EqualTo("Q{}base"));
            Assert.That(accumulator.Dependencies.Single().Name, Is.EqualTo("Q{}base"));
        });
    }

    [Test]
    public void UsesTheHighestImportPrecedenceDeclaration()
    {
        AccumulatorResolver resolver = new AccumulatorResolver();
        resolver.Resources.Add(
            "https://example.test/base.xsl",
            Stylesheet(
                "<xsl:accumulator name='shared' initial-value='0'><xsl:accumulator-rule match='first' select='$value'/></xsl:accumulator>" +
                "<xsl:accumulator name='shared' initial-value='1'><xsl:accumulator-rule match='second' select='$value'/></xsl:accumulator>",
                ""));
        string source = Stylesheet(
            "<xsl:import href='base.xsl'/>" +
            "<xsl:accumulator name='shared' initial-value='2'><xsl:accumulator-rule match='local' select='$value'/></xsl:accumulator>",
            "");

        XsltAccumulatorPlan accumulator = Plan(Compile(source, resolver)).Accumulators.Single();

        Assert.Multiple(() =>
        {
            Assert.That(accumulator.Declarations, Has.Length.EqualTo(3));
            Assert.That(accumulator.Declarations.Select(item => item.ImportPrecedence), Is.Ordered.Ascending);
            Assert.That(accumulator.Declarations.Count(item => item.IsEffective), Is.EqualTo(1));
            Assert.That(accumulator.EffectiveDeclaration.InitialValueSource, Is.EqualTo("2"));
            Assert.That(accumulator.EffectiveDeclaration.Rules.Single().MatchSource, Is.EqualTo("local"));
        });
    }

    [Test]
    public void LinksModeAccumulatorNamesAndAllToken()
    {
        string source = Stylesheet(
            "<xsl:accumulator name='first' initial-value='0' streamable='yes'><xsl:accumulator-rule match='node()' select='$value'/></xsl:accumulator>" +
            "<xsl:accumulator name='second' initial-value='0'><xsl:accumulator-rule match='node()' select='$value'/></xsl:accumulator>" +
            "<xsl:mode name='named' use-accumulators='first'/>" +
            "<xsl:mode name='all' use-accumulators='#all'/>",
            "");

        XsltStylesheetPlan stylesheet = Plan(Compile(source));
        XsltModePlan named = stylesheet.Modes.Single(item => item.Name == "Q{}named");
        XsltModePlan all = stylesheet.Modes.Single(item => item.Name == "Q{}all");

        Assert.Multiple(() =>
        {
            Assert.That(named.Accumulators.Select(item => item.Name), Is.EqualTo(new[] { "Q{}first" }));
            Assert.That(all.Accumulators.Select(item => item.Name), Is.EqualTo(new[] { "Q{}first", "Q{}second" }));
        });
    }

    [Test]
    public void RecordsNonStreamableExpressionHooksWithoutRejectingCompilation()
    {
        string source = Stylesheet(
            "<xsl:accumulator name='nodes' initial-value='/' streamable='yes'>" +
            "<xsl:accumulator-rule match='item' select='descendant::item'/>" +
            "</xsl:accumulator>",
            "");

        XsltAccumulatorDeclarationPlan declaration = Plan(Compile(source)).Accumulators.Single().EffectiveDeclaration;

        Assert.Multiple(() =>
        {
            Assert.That(declaration.InitialValueIsGrounded, Is.False);
            Assert.That(declaration.InitialValueIsMotionless, Is.False);
            Assert.That(declaration.Rules.Single().ValueIsGrounded, Is.False);
            Assert.That(declaration.Rules.Single().ValueIsMotionless, Is.False);
            Assert.That(declaration.IsGuaranteedStreamableCandidate, Is.False);
        });
    }

    [TestCase("<xsl:accumulator initial-value='0'><xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:accumulator xmlns:fn='http://www.w3.org/2005/xpath-functions' name='fn:a' initial-value='0'><xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator>", XsltErrorCodes.XTSE0080)]
    [TestCase("<xsl:accumulator name='a'><xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:accumulator name='a' initial-value='0'/>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:accumulator name='a' initial-value='0'><xsl:accumulator-rule select='$value'/></xsl:accumulator>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:accumulator name='a' initial-value='0'><xsl:accumulator-rule match='item' phase='middle' select='$value'/></xsl:accumulator>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:accumulator name='a' initial-value='0'><xsl:accumulator-rule match='item' select='$value'><xsl:text>also</xsl:text></xsl:accumulator-rule></xsl:accumulator>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:accumulator name='a' as='bad(' initial-value='0'><xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator>", XPathErrorCodes.XPST0003)]
    [TestCase("<xsl:accumulator name='a' initial-value='1 +'><xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator>", XPathErrorCodes.XPST0003)]
    [TestCase("<xsl:accumulator name='a' initial-value='0'><xsl:accumulator-rule match='item[' select='$value'/></xsl:accumulator>", XsltErrorCodes.XTSE0340)]
    public void RejectsInvalidAccumulatorDeclarations(string declaration, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declaration, "")))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void RejectsDuplicateEffectiveDeclarations()
    {
        string declaration = "<xsl:accumulator name='a' initial-value='0'><xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator>";

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declaration + declaration, "")))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3350));
    }

    [Test]
    public void AccumulatorValueShadowsAGlobalVariableWithTheSameName()
    {
        string source = Stylesheet(
            "<xsl:variable name='value' select='99'/>" +
            "<xsl:accumulator name='a' as='xs:integer' initial-value='0'>" +
            "<xsl:accumulator-rule match='item' select='$value + 1'/>" +
            "</xsl:accumulator>",
            "xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        XsltAccumulatorRulePlan rule = Plan(Compile(source)).Accumulators.Single().EffectiveDeclaration.Rules.Single();

        Assert.Multiple(() =>
        {
            Assert.That(rule.Select, Is.Not.Null);
            Assert.That(rule.Dependencies, Is.Empty);
            Assert.That(rule.SelectType!.AtomicType, Is.EqualTo(XdmAtomicType.Integer));
        });
    }

    [Test]
    public void RejectsLiteralDependencyCycles()
    {
        string source = Stylesheet(
            "<xsl:accumulator name='a' initial-value='0'><xsl:accumulator-rule match='item' select='accumulator-after(\"b\")'/></xsl:accumulator>" +
            "<xsl:accumulator name='b' initial-value='0'><xsl:accumulator-rule match='item' select='accumulator-before(\"a\")'/></xsl:accumulator>",
            "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE3400));
    }

    [TestCase("<xsl:mode name='m' use-accumulators='missing'/>")]
    [TestCase("<xsl:accumulator name='a' initial-value='0'><xsl:accumulator-rule match='item' select='$value'/></xsl:accumulator><xsl:mode name='m' use-accumulators='a'/>")]
    public void RejectsModeReferencesThatAreNotDeclaredStreamable(string declarations)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declarations, "")))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3300));
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

    private sealed class AccumulatorResolver : IXmlResourceResolver
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
