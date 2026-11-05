using xml;

namespace xml.Tests;

[TestFixture]
public class XsltVariableCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void CompilesGlobalLocalAndTemplateParameterDeclarations()
    {
        string source = Stylesheet(
            "<xsl:param name='configuration' as='xs:string' required='yes'/>" +
            "<xsl:param name='implicit' as='xs:integer'/>" +
            "<xsl:variable name='compile-time' static='yes' select='5'/>" +
            "<xsl:variable name='base' as='xs:integer' select='2'/>" +
            "<xsl:template name='entry'><xsl:param name='input' as='node()*' required='yes' tunnel='yes'/>" +
            "<xsl:param name='optional'/><xsl:variable name='total' as='xs:integer' select='$base + 1'/>" +
            "<xsl:variable name='tree'><item/></xsl:variable></xsl:template>", "xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        XsltStylesheetPlan plan = Plan(Compile(source, new VariableResolver()));
        XsltVariablePlan configuration = Variable(plan, "configuration");
        XsltVariablePlan implicitParameter = Variable(plan, "implicit");
        XsltVariablePlan compileTime = Variable(plan, "compile-time");
        XsltVariablePlan input = Variable(plan, "input");
        XsltVariablePlan optional = Variable(plan, "optional");
        XsltVariablePlan total = Variable(plan, "total");
        XsltVariablePlan tree = Variable(plan, "tree");

        Assert.Multiple(() =>
        {
            Assert.That(configuration.Scope, Is.EqualTo(XsltVariableScope.Global));
            Assert.That(configuration.IsParameter, Is.True);
            Assert.That(configuration.IsRequired, Is.True);
            Assert.That(configuration.IsExplicitlyRequired, Is.True);
            Assert.That(configuration.ValueKind, Is.EqualTo(XsltVariableValueKind.Required));
            Assert.That(configuration.SequenceType!.AtomicType!.Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(implicitParameter.IsRequired, Is.True);
            Assert.That(implicitParameter.IsExplicitlyRequired, Is.False);
            Assert.That(implicitParameter.ValueKind, Is.EqualTo(XsltVariableValueKind.Required));
            Assert.That(compileTime.IsStatic, Is.True);
            Assert.That(compileTime.ValueKind, Is.EqualTo(XsltVariableValueKind.StaticValue));
            Assert.That(compileTime.Binding.Value, Is.Not.Null);
            Assert.That(input.Scope, Is.EqualTo(XsltVariableScope.TemplateParameter));
            Assert.That(input.IsTunnel, Is.True);
            Assert.That(input.Binding.StaticTypeSyntax!.NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.AnyKind));
            Assert.That(optional.ValueKind, Is.EqualTo(XsltVariableValueKind.EmptyString));
            Assert.That(total.Scope, Is.EqualTo(XsltVariableScope.Local));
            Assert.That(total.SelectPlan, Is.Not.Null);
            Assert.That(total.Dependencies.Single().ExpandedName, Is.EqualTo("Q{}base"));
            Assert.That(tree.ValueKind, Is.EqualTo(XsltVariableValueKind.Content));
            Assert.That(tree.Content.Any(node => node.Kind == XmlNodeKind.Element), Is.True);
        });
    }

    [Test]
    public void CompilesFunctionAndIterationParametersWithTheirRequiredSemantics()
    {
        string source = Stylesheet(
            "<xsl:function xmlns:f='urn:functions' name='f:calculate'><xsl:param name='argument' as='item()'/><xsl:sequence select='$argument'/></xsl:function>" +
            "<xsl:template name='entry'><xsl:iterate select='1 to 2'><xsl:param name='state' select='0'/><xsl:sequence select='$state'/></xsl:iterate></xsl:template>", "");

        XsltStylesheetPlan plan = Plan(Compile(source, new VariableResolver()));
        XsltVariablePlan argument = Variable(plan, "argument");
        XsltVariablePlan state = Variable(plan, "state");

        Assert.Multiple(() =>
        {
            Assert.That(argument.Scope, Is.EqualTo(XsltVariableScope.FunctionParameter));
            Assert.That(argument.IsRequired, Is.True);
            Assert.That(argument.ValueKind, Is.EqualTo(XsltVariableValueKind.Required));
            Assert.That(state.Scope, Is.EqualTo(XsltVariableScope.IterationParameter));
            Assert.That(state.IsRequired, Is.False);
            Assert.That(state.ValueKind, Is.EqualTo(XsltVariableValueKind.Select));
            Assert.That(state.SelectPlan, Is.Not.Null);
        });
    }

    [Test]
    public void PreservesLexicalScopesForInitializersAndAttributeValueTemplates()
    {
        string source = Stylesheet(
            "<xsl:variable name='global' select='1'/>" +
            "<xsl:template name='entry'><xsl:param name='parameter' select='2'/><xsl:variable name='local' select='$global + $parameter'/>" +
            "<outer value='{$local}'><xsl:variable name='nested' select='$local'/><inner value='{$nested}'/></outer><after value='{$local}'/></xsl:template>", "");

        XsltStylesheetPlan plan = Plan(Compile(source, new VariableResolver()));
        XsltVariablePlan local = Variable(plan, "local");
        XsltVariablePlan nested = Variable(plan, "nested");
        XsltValueTemplate inner = plan.ValueTemplates.Single(template => template.Node.Parent!.Name!.LocalName == "inner");
        XsltValueTemplate after = plan.ValueTemplates.Single(template => template.Node.Parent!.Name!.LocalName == "after");
        string[] innerNames = inner.Parts.Single(part => part.ExpressionPlan != null).ExpressionPlan!.Environment.Variables.Select(binding => binding.Name!.LocalName!).ToArray();
        string[] afterNames = after.Parts.Single(part => part.ExpressionPlan != null).ExpressionPlan!.Environment.Variables.Select(binding => binding.Name!.LocalName!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(local.Dependencies.Select(variable => variable.Name.LocalName), Is.EquivalentTo(new[] { "global", "parameter" }));
            Assert.That(nested.Dependencies.Single().Name.LocalName, Is.EqualTo("local"));
            Assert.That(innerNames, Does.Contain("nested"));
            Assert.That(innerNames, Does.Contain("local"));
            Assert.That(afterNames, Does.Contain("local"));
            Assert.That(afterNames, Does.Not.Contain("nested"));
        });
    }

    [Test]
    public void AllowsForwardGlobalReferencesAndBuildsDependencyEdges()
    {
        string source = Stylesheet("<xsl:variable name='first' select='$second + 1'/><xsl:variable name='second' select='2'/>", "");

        XsltStylesheetPlan plan = Plan(Compile(source, new VariableResolver()));
        XsltVariablePlan first = Variable(plan, "first");

        Assert.Multiple(() =>
        {
            Assert.That(first.SelectPlan, Is.Not.Null);
            Assert.That(first.Dependencies, Has.Length.EqualTo(1));
            Assert.That(first.Dependencies[0].ExpandedName, Is.EqualTo("Q{}second"));
        });
    }

    [Test]
    public void BuildsDependenciesFromSequenceConstructorExpressionsAndValueTemplates()
    {
        string source = Stylesheet("<xsl:variable name='first' select='1'/><xsl:variable name='second' select='2'/>" +
            "<xsl:variable name='constructed' expand-text='yes'><xsl:sequence select='$first'/><item value='{$second}'>{$first}</item></xsl:variable>", "");

        XsltVariablePlan constructed = Variable(Plan(Compile(source, new VariableResolver())), "constructed");

        Assert.That(constructed.Dependencies.Select(variable => variable.Name.LocalName), Is.EquivalentTo(new[] { "first", "second" }));
    }

    [Test]
    public void SelectsTheHighestPrecedenceGlobalBinding()
    {
        VariableResolver resolver = new VariableResolver();
        resolver.Resources.Add("https://example.test/base.xsl", Stylesheet("<xsl:variable name='setting' select='1'/>", ""));
        string source = Stylesheet("<xsl:import href='base.xsl'/><xsl:variable name='setting' select='2'/><xsl:variable name='dependent' select='$setting'/>", "");

        XsltStylesheetPlan plan = Plan(Compile(source, resolver));
        XsltVariablePlan[] settings = plan.Variables.Where(variable => variable.ExpandedName == "Q{}setting").ToArray();
        XsltVariablePlan dependent = Variable(plan, "dependent");

        Assert.Multiple(() =>
        {
            Assert.That(settings, Has.Length.EqualTo(2));
            Assert.That(settings.Count(variable => variable.IsEffective), Is.EqualTo(1));
            Assert.That(settings.Single(variable => variable.IsEffective).Select, Is.EqualTo("2"));
            Assert.That(dependent.Dependencies.Single(), Is.SameAs(settings.Single(variable => variable.IsEffective)));
        });
    }

    [Test]
    public void RetainsStructuralSequenceTypesDuringDependentExpressionAnalysis()
    {
        string source = Stylesheet("<xsl:param name='nodes' as='node()*'/><xsl:variable name='children' select='$nodes/child::*'/>", "");

        XsltVariablePlan children = Variable(Plan(Compile(source, new VariableResolver())), "children");

        Assert.That(children.SelectPlan, Is.Not.Null);
    }

    [TestCase("<xsl:variable name='value' select='1'><item/></xsl:variable>", XsltErrorCodes.XTSE0620)]
    [TestCase("<xsl:template name='entry'><xsl:variable name='same'/><xsl:variable name='same'/></xsl:template>", XsltErrorCodes.XTSE0580)]
    [TestCase("<xsl:variable name='value' as='xs:missing'/>", XPathErrorCodes.XPST0051)]
    [TestCase("<xsl:variable name='value' as='xs:string extra'/>", XPathErrorCodes.XPST0003)]
    [TestCase("<xsl:param name='value' required='yes' select='1'/>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:param name='value' tunnel='yes'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:function xmlns:f='urn:f' name='f:run'><xsl:param name='value' select='1'/></xsl:function>", XsltErrorCodes.XTSE0760)]
    [TestCase("<xsl:template name='entry'><xsl:variable select='1'/></xsl:template>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:variable name='value' select='$missing'/>", XPathErrorCodes.XPST0008)]
    [TestCase("<xsl:variable name='value' select=''/>", XPathErrorCodes.XPST0003)]
    public void ReportsInvalidDeclarationsAndInitializers(string content, string expectedCode)
    {
        string namespaces = content.Contains("xs:") ? "xmlns:xs='http://www.w3.org/2001/XMLSchema'" : "";

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(content, namespaces), new VariableResolver()))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
            Assert.That(error.Location, Is.Not.Null);
        });
    }

    [Test]
    public void RejectsCircularGlobalDependencies()
    {
        string source = Stylesheet("<xsl:variable name='first' select='$second'/><xsl:variable name='second' select='$first'/>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new VariableResolver()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0640));
    }

    [Test]
    public void RejectsCircularDependenciesInsideSequenceConstructors()
    {
        string source = Stylesheet("<xsl:variable name='cycle'><xsl:sequence select='$cycle'/></xsl:variable>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new VariableResolver()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0640));
    }

    [Test]
    public void RejectsDuplicateEffectiveGlobalBindings()
    {
        string source = Stylesheet("<xsl:variable name='same' select='1'/><xsl:param name='same' select='2'/>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new VariableResolver()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0630));
    }

    private static XsltVariablePlan Variable(XsltStylesheetPlan plan, string localName)
    {
        return plan.Variables.Single(variable => variable.Name.LocalName == localName);
    }

    private static string Stylesheet(string content, string namespaces)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + namespaces + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source, VariableResolver resolver)
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

    private sealed class VariableResolver : IXmlResourceResolver
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
