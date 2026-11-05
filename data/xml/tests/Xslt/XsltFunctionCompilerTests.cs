using xml;

namespace xml.Tests;

[TestFixture]
public class XsltFunctionCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void CompilesFunctionSignatureFlagsAndBody()
    {
        string source = Stylesheet(
            "<xsl:function name='f:format' as='xs:string' visibility='final' cache='yes' new-each-time='no' " +
            "override-extension-function='no' override='false' streamability='filter'>" +
            "<xsl:param name='input' as='element()'/><xsl:param name='suffix' as='xs:string?'/>" +
            "<xsl:sequence select='string($input) || $suffix'/></xsl:function>",
            "xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        XsltFunctionPlan function = Plan(Compile(source, new FunctionResolver(), null, null)).Functions.Single();

        Assert.Multiple(() =>
        {
            Assert.That(function.ExpandedName, Is.EqualTo("Q{urn:functions}format"));
            Assert.That(function.Arity, Is.EqualTo(2));
            Assert.That(function.Parameters.Select(parameter => parameter.Name.LocalName), Is.EqualTo(new[] { "input", "suffix" }));
            Assert.That(function.ParameterTypes[0]!.NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Element));
            Assert.That(function.ParameterTypes[1]!.AtomicType!.Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(function.ParameterTypes[1]!.Occurrence, Is.EqualTo("?"));
            Assert.That(function.ReturnType!.AtomicType!.Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(function.Cache, Is.True);
            Assert.That(function.Determinism, Is.EqualTo(XsltFunctionDeterminism.Deterministic));
            Assert.That(function.OverrideExtensionFunction, Is.False);
            Assert.That(function.HasOverrideExtensionFunction, Is.True);
            Assert.That(function.Streamability, Is.EqualTo("filter"));
            Assert.That(function.Visibility, Is.EqualTo(XsltComponentVisibility.Final));
            Assert.That(function.Binding.IsDeterministic, Is.True);
            Assert.That(function.Body.Items.Single().LocalName, Is.EqualTo("sequence"));
            Assert.That(function.Expressions.Single().Name, Is.EqualTo("select"));
        });
    }

    [Test]
    public void ResolvesForwardAndRecursiveFunctionCalls()
    {
        string source = Stylesheet(
            "<xsl:function name='f:first' as='xs:integer'><xsl:param name='value' as='xs:integer'/>" +
            "<xsl:sequence select='f:second($value)'/></xsl:function>" +
            "<xsl:function name='f:second' as='xs:integer'><xsl:param name='value' as='xs:integer'/>" +
            "<xsl:sequence select='if ($value eq 0) then 0 else f:second($value - 1)'/></xsl:function>",
            "xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        XsltStylesheetPlan plan = Plan(Compile(source, new FunctionResolver(), null, null));
        XsltFunctionPlan first = Function(plan, "first", 1);
        XsltFunctionPlan second = Function(plan, "second", 1);

        Assert.Multiple(() =>
        {
            Assert.That(first.References.Single().TargetFunction, Is.SameAs(second));
            Assert.That(first.References.Single().TargetComponent, Is.SameAs(second.Component));
            Assert.That(second.References.Single().TargetFunction, Is.SameAs(second));
            Assert.That(second.References.Single().IsRecursive, Is.True);
            Assert.That(second.Component!.References.Single(reference => reference.Kind == XsltComponentKind.Function).Target, Is.SameAs(second.Component));
        });
    }

    [Test]
    public void MakesFunctionsAvailableToGlobalVariablesAndValueTemplates()
    {
        string source = Stylesheet(
            "<xsl:variable name='state' select='f:later(1)'/>" +
            "<xsl:template name='entry'><result value='{f:later(2)}'/></xsl:template>" +
            "<xsl:function name='f:later' as='xs:integer'><xsl:param name='value' as='xs:integer'/>" +
            "<xsl:sequence select='$value + 1'/></xsl:function>",
            "xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        XsltStylesheetPlan plan = Plan(Compile(source, new FunctionResolver(), null, null));
        XsltComponent state = Component(plan, XsltComponentKind.Variable, "Q{}state", -1);
        XsltComponent entry = Component(plan, XsltComponentKind.Template, "Q{}entry", -1);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Variables.Single(variable => variable.Name.LocalName == "state").SelectPlan!.Root.FunctionBinding!.BindingKey,
                Is.EqualTo("xslt-function:Q{urn:functions}later#1"));
            Assert.That(state.References.Single(reference => reference.Kind == XsltComponentKind.Function).Target!.Name, Is.EqualTo("Q{urn:functions}later"));
            Assert.That(entry.References.Single(reference => reference.Kind == XsltComponentKind.Function).Target!.Name, Is.EqualTo("Q{urn:functions}later"));
        });
    }

    [Test]
    public void AllowsSameFunctionNameAtDifferentArities()
    {
        string source = Stylesheet(
            "<xsl:function name='f:value'><xsl:sequence select='1'/></xsl:function>" +
            "<xsl:function name='f:value'><xsl:param name='input'/><xsl:sequence select='$input'/></xsl:function>",
            "xmlns:f='urn:functions'");

        XsltStylesheetPlan plan = Plan(Compile(source, new FunctionResolver(), null, null));

        Assert.That(plan.Functions.Select(function => function.Arity), Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void PropagatesStructuralFunctionTypesIntoXPathAnalysis()
    {
        string source = Stylesheet(
            "<xsl:function name='f:children' as='element()*'><xsl:param name='parent' as='element()'/>" +
            "<xsl:sequence select='$parent/*'/></xsl:function>" +
            "<xsl:function name='f:grandchildren' as='element()*'><xsl:param name='parent' as='element()'/>" +
            "<xsl:sequence select='f:children($parent)/*'/></xsl:function>",
            "xmlns:f='urn:functions'");

        XsltFunctionPlan function = Function(Plan(Compile(source, new FunctionResolver(), null, null)), "grandchildren", 1);

        Assert.Multiple(() =>
        {
            Assert.That(function.Expressions.Single().Plan.Root.StaticType.ItemKind, Is.EqualTo(XPathStaticItemKind.Node));
            Assert.That(function.References.Single().TargetFunction!.Name.LocalName, Is.EqualTo("children"));
        });
    }

    [Test]
    public void EnforcesStylesheetFunctionArgumentTypes()
    {
        string source = Stylesheet(
            "<xsl:function name='f:accept'><xsl:param name='value' as='element()'/><xsl:sequence select='$value'/></xsl:function>" +
            "<xsl:function name='f:call'><xsl:sequence select='f:accept(1)'/></xsl:function>",
            "xmlns:f='urn:functions'");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new FunctionResolver(), null, null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
    }

    [Test]
    public void SelectsFunctionAtHighestImportPrecedence()
    {
        FunctionResolver resolver = new FunctionResolver();
        resolver.Resources.Add("https://example.test/base.xsl", Stylesheet("<xsl:function name='f:value'><xsl:sequence select='1'/></xsl:function>", "xmlns:f='urn:functions'"));
        string source = Stylesheet(
            "<xsl:import href='base.xsl'/><xsl:function name='f:value'><xsl:sequence select='2'/></xsl:function>" +
            "<xsl:variable name='selected' select='f:value()'/>",
            "xmlns:f='urn:functions'");

        XsltStylesheetPlan plan = Plan(Compile(source, resolver, null, null));
        XsltFunctionPlan[] functions = plan.Functions.Where(function => function.Name.LocalName == "value").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(functions, Has.Length.EqualTo(2));
            Assert.That(functions.Count(function => function.IsEffective), Is.EqualTo(1));
            Assert.That(functions.Single(function => function.IsEffective).Expressions.Single().Source, Is.EqualTo("2"));
            Assert.That(plan.Variables.Single(variable => variable.Name.LocalName == "selected").SelectPlan!.Root.FunctionBinding!.BindingKey,
                Is.EqualTo(functions.Single(function => function.IsEffective).Binding.BindingKey));
        });
    }

    [Test]
    public void UsesRegisteredExtensionWhenOverrideIsDisabled()
    {
        XmlName name = new XmlName { Prefix = "f", LocalName = "fast", NamespaceUri = "urn:functions", QualifiedName = "f:fast" };
        XsltExtensionBinding extension = new XsltExtensionBinding { Name = name, BindingKey = "extension:fast", IsFunction = true };
        string source = Stylesheet(
            "<xsl:function name='f:fast' override-extension-function='no'><xsl:param name='value'/><xsl:sequence select='$value'/></xsl:function>" +
            "<xsl:variable name='result' select='f:fast(1)'/>",
            "xmlns:f='urn:functions'");

        XsltStylesheetPlan plan = Plan(Compile(source, new FunctionResolver(), null, new[] { extension }));
        XsltFunctionPlan function = Function(plan, "fast", 1);

        Assert.Multiple(() =>
        {
            Assert.That(function.IsOverriddenByExtension, Is.True);
            Assert.That(function.IsEffective, Is.False);
            Assert.That(function.Component, Is.Null);
            Assert.That(plan.Package!.Components.Any(component => component.Kind == XsltComponentKind.Function && component.Name == function.ExpandedName), Is.False);
            Assert.That(plan.Variables.Single(variable => variable.Name.LocalName == "result").SelectPlan!.Root.FunctionBinding!.BindingKey, Is.EqualTo("extension:fast"));
        });
    }

    [Test]
    public void BindsOriginalFunctionInsidePackageOverride()
    {
        FunctionResolver resolver = new FunctionResolver();
        string library = Package("urn:library",
            "<xsl:function name='f:calculate' as='xs:integer' visibility='public'><xsl:param name='value' as='xs:integer'/><xsl:sequence select='$value'/></xsl:function>",
            "xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema'");
        resolver.Resources.Add("https://example.test/library.xsl", library);
        string source = Package("urn:main",
            "<xsl:use-package name='urn:library'><xsl:override>" +
            "<xsl:function name='f:calculate' as='xs:integer'><xsl:param name='value' as='xs:integer'/>" +
            "<xsl:sequence select='xsl:original($value) + 1'/></xsl:function>" +
            "</xsl:override></xsl:use-package>",
            "xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema'");

        XsltPackageReference reference = Reference("urn:library", "library.xsl");
        XsltStylesheetPlan plan = Plan(Compile(source, resolver, new[] { reference }, null));
        XsltFunctionPlan function = Function(plan, "calculate", 1);
        XsltFunctionReferencePlan original = function.References.Single();

        Assert.Multiple(() =>
        {
            Assert.That(function.IsOverride, Is.True);
            Assert.That(original.IsOriginal, Is.True);
            Assert.That(original.TargetComponent, Is.SameAs(function.Component!.OriginalComponent));
            Assert.That(original.TargetFunction, Is.Not.Null);
            Assert.That(original.TargetFunction!.ExpandedName, Is.EqualTo(function.ExpandedName));
        });
    }

    [Test]
    public void AllowsAbstractFunctionWithParametersAndNoBody()
    {
        string source = Package("urn:library", "<xsl:function name='f:hook' visibility='abstract'><xsl:param name='value'/></xsl:function>", "xmlns:f='urn:functions'");

        XsltFunctionPlan function = Plan(CompileLibrary(source)).Functions.Single();

        Assert.Multiple(() =>
        {
            Assert.That(function.IsAbstract, Is.True);
            Assert.That(function.Parameters, Has.Length.EqualTo(1));
            Assert.That(function.Body.Items, Is.Empty);
        });
    }

    [TestCase("<xsl:function/>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:function name='local'/>", XsltErrorCodes.XTSE0740)]
    [TestCase("<xsl:function name='fn:reserved'/>", XsltErrorCodes.XTSE0080)]
    [TestCase("<xsl:function name='f:value' cache='sometimes'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:function name='f:value' new-each-time='often'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:function name='f:value' override='yes' override-extension-function='no'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:function name='f:value' as='xs:missing'/>", XPathErrorCodes.XPST0051)]
    [TestCase("<xsl:function name='f:value' streamability='filter'/>", XsltErrorCodes.XTSE3155)]
    [TestCase("<xsl:function name='f:value'><xsl:sequence select='1'/><xsl:param name='late'/></xsl:function>", XsltErrorCodes.XTSE0010)]
    [TestCase("<xsl:function name='f:value'><xsl:function name='f:nested'/></xsl:function>", XsltErrorCodes.XTSE0010)]
    public void ReportsInvalidFunctionDeclarations(string declaration, string expectedCode)
    {
        string namespaces = "xmlns:f='urn:functions' xmlns:fn='http://www.w3.org/2005/xpath-functions' xmlns:xs='http://www.w3.org/2001/XMLSchema'";

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declaration, namespaces), new FunctionResolver(), null, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
            Assert.That(error.Location, Is.Not.Null);
        });
    }

    [Test]
    public void RejectsDuplicateFunctionsAtTheSamePrecedence()
    {
        string declaration = "<xsl:function name='f:value'><xsl:sequence select='1'/></xsl:function>";

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(
            Stylesheet(declaration + declaration, "xmlns:f='urn:functions'"), new FunctionResolver(), null, null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0770));
    }

    [Test]
    public void RejectsUnknownFunctionCallsInFunctionBodies()
    {
        string source = Stylesheet("<xsl:function name='f:value'><xsl:sequence select='f:missing()'/></xsl:function>", "xmlns:f='urn:functions'");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, new FunctionResolver(), null, null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
    }

    [Test]
    public void RejectsAbstractFunctionWithABody()
    {
        string source = Package("urn:library", "<xsl:function name='f:hook' visibility='abstract'><xsl:sequence select='1'/></xsl:function>", "xmlns:f='urn:functions'");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => CompileLibrary(source))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0020));
    }

    private static XsltFunctionPlan Function(XsltStylesheetPlan plan, string localName, int arity)
    {
        return plan.Functions.Single(function => function.Name.LocalName == localName && function.Arity == arity);
    }

    private static XsltComponent Component(XsltStylesheetPlan plan, XsltComponentKind kind, string name, int arity)
    {
        return plan.Package!.Components.Single(component => component.Kind == kind && component.Name == name && component.Arity == arity && component.Visibility != XsltComponentVisibility.Hidden);
    }

    private static string Stylesheet(string content, string namespaces)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + namespaces + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static string Package(string name, string content, string namespaces)
    {
        return "<xsl:package xmlns:xsl='" + Xsl + "' " + namespaces + " name='" + name + "' package-version='1' version='3.0'>" + content + "</xsl:package>";
    }

    private static XsltPackageReference Reference(string name, string location)
    {
        return new XsltPackageReference
        {
            Name = new XmlName { LocalName = name },
            Version = "1",
            LocationUri = location
        };
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source, FunctionResolver resolver, XsltPackageReference[]? packages, XsltExtensionBinding[]? extensions)
    {
        XsltCompileOptions options = Options(packages, extensions);
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver);
    }

    private static XsltExecutable CompileLibrary(string source)
    {
        XsltCompileOptions options = Options(null, null);
        return new XsltStylesheetLoader(options, new FunctionResolver(), new XsltPackageCompilationContext(), true).CompileXsltStylesheet(
            new XmlInput { Text = source, BaseUri = "https://example.test/main.xsl" });
    }

    private static XsltCompileOptions Options(XsltPackageReference[]? packages, XsltExtensionBinding[]? extensions)
    {
        return new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            Packages = packages,
            Extensions = extensions,
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
    }

    private sealed class FunctionResolver : IXmlResourceResolver
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
