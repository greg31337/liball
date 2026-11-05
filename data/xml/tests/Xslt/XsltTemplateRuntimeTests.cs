using xml;

namespace xml.Tests;

[TestFixture]
public class XsltTemplateRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void AppliesIndexedRulesRecursivelyAndPreservesModeFocus()
    {
        string source = Stylesheet(
            "<xsl:mode name='work' on-no-match='shallow-skip'/>" +
            "<xsl:template name='root-rule' match='root' mode='work'><xsl:apply-templates mode='#current'/></xsl:template>" +
            "<xsl:template name='item-rule' match='item' mode='work'/>",
            "default-mode='work'");
        XsltTransformationState state = Initialize(source, Parse("<root><item/><item/></root>"), []);
        XsltTemplateRuntime runtime = state.TemplateRuntime!;

        XdmSequence result = runtime.ExecuteXsltInitialTemplateInvocation();
        XsltTemplateInvocation[] rules = runtime.Invocations.Where(item => item.Kind == XsltTemplateDispatchKind.Rule).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(0));
            Assert.That(runtime.Invocations[0].Kind, Is.EqualTo(XsltTemplateDispatchKind.BuiltIn));
            Assert.That(rules.Select(item => item.Template!.Name), Is.EqualTo(new[] { "Q{}root-rule", "Q{}item-rule", "Q{}item-rule" }));
            Assert.That(rules.All(item => item.Mode == "Q{}work"), Is.True);
            Assert.That(rules[1].Position, Is.EqualTo(1));
            Assert.That(rules[2].Position, Is.EqualTo(2));
            Assert.That(rules[2].Size, Is.EqualTo(2));
            Assert.That(rules[0].Depth, Is.EqualTo(0));
            Assert.That(rules[1].Depth, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvokesNamedTemplatesWithLocalOrdinaryAndTunnelParameters()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:param name='n' as='xs:integer' required='yes'/>" +
            "<xsl:param name='token' as='xs:string' tunnel='yes' required='yes'/>" +
            "<xsl:variable name='incremented' as='xs:integer' select='$n + 1'/>" +
            "<xsl:call-template name='helper'>" +
            "<xsl:with-param name='n' select='$incremented'/>" +
            "<xsl:with-param name='token' select='$token' tunnel='yes'/>" +
            "</xsl:call-template></xsl:template>" +
            "<xsl:template name='helper'>" +
            "<xsl:param name='n' as='xs:integer' required='yes'/>" +
            "<xsl:param name='token' as='xs:string' tunnel='yes' required='yes'/>" +
            "</xsl:template>",
            "xmlns:xs='" + Xs + "'");
        XsltParameter number = new XsltParameter { Name = Name("n"), Value = Atomic("4", XdmAtomicType.UntypedAtomic) };
        XsltParameter token = new XsltParameter { Name = Name("token"), Value = Atomic("secret", XdmAtomicType.String), IsTunnel = true };
        XsltTransformationState state = InitializeNamed(source, "entry", [number, token], null);
        XsltTemplateRuntime runtime = state.TemplateRuntime!;

        runtime.ExecuteXsltInitialTemplateInvocation();
        XsltTemplateInvocation helper = runtime.Invocations.Single(item => item.Template!.Name == "Q{}helper");
        XsltRuntimeVariableBinding ordinary = runtime.Variables.FindXsltFrameBinding(helper.Frame!, Name("n"), false)!;
        XsltRuntimeVariableBinding tunnel = runtime.Variables.FindXsltFrameBinding(helper.Frame!, Name("token"), true)!;

        Assert.Multiple(() =>
        {
            Assert.That(runtime.Invocations.Select(item => item.Template!.Name), Is.EqualTo(new[] { "Q{}entry", "Q{}helper" }));
            Assert.That(ordinary.Value.AtomicValues![0].IntegerValue, Is.EqualTo(5));
            Assert.That(tunnel.Value.AtomicValues![0].StringValue, Is.EqualTo("secret"));
            Assert.That(runtime.Variables.FindXsltFrameBinding(helper.Frame!, Name("incremented"), false), Is.Null);
        });
    }

    [Test]
    public void PassesWithParametersToAppliedTemplatesAndUsesDefaultMode()
    {
        string source = Stylesheet(
            "<xsl:mode name='work' on-no-match='shallow-skip'/>" +
            "<xsl:template match='root' mode='work'><xsl:apply-templates>" +
            "<xsl:with-param name='label' select=\"'sent'\"/>" +
            "</xsl:apply-templates></xsl:template>" +
            "<xsl:template name='item' match='item' mode='work'><xsl:param name='label' as='xs:string' required='yes'/></xsl:template>",
            "xmlns:xs='" + Xs + "' default-mode='work'");
        XsltTemplateRuntime runtime = Initialize(source, Parse("<root><item/></root>"), []).TemplateRuntime!;

        runtime.ExecuteXsltInitialTemplateInvocation();
        XsltTemplateInvocation item = runtime.Invocations.Single(value => value.Template != null && value.Template.Name == "Q{}item");
        XsltRuntimeVariableBinding label = runtime.Variables.FindXsltFrameBinding(item.Frame!, Name("label"), false)!;

        Assert.Multiple(() =>
        {
            Assert.That(item.Mode, Is.EqualTo("Q{}work"));
            Assert.That(label.Value.AtomicValues![0].StringValue, Is.EqualTo("sent"));
        });
    }

    [Test]
    public void NextMatchWalksLowerPriorityRulesInOrder()
    {
        string source = Stylesheet(
            "<xsl:template name='low' match='item' priority='1'><xsl:param name='p' select=\"'low-default'\"/></xsl:template>" +
            "<xsl:template name='middle' match='item' priority='2'><xsl:param name='p'/><xsl:next-match/></xsl:template>" +
            "<xsl:template name='high' match='item' priority='3'><xsl:next-match><xsl:with-param name='p' select=\"'from-high'\"/></xsl:next-match></xsl:template>",
            "");
        XsltTemplateRuntime runtime = Initialize(source, Parse("<item/>"), []).TemplateRuntime!;

        runtime.ExecuteXsltInitialTemplateInvocation();
        XsltTemplateInvocation[] rules = runtime.Invocations.Where(item => item.Kind == XsltTemplateDispatchKind.Rule).ToArray();
        XsltRuntimeVariableBinding middle = runtime.Variables.FindXsltFrameBinding(rules[1].Frame!, Name("p"), false)!;
        XsltRuntimeVariableBinding low = runtime.Variables.FindXsltFrameBinding(rules[2].Frame!, Name("p"), false)!;

        Assert.Multiple(() =>
        {
            Assert.That(rules.Select(item => item.Template!.Name), Is.EqualTo(new[] { "Q{}high", "Q{}middle", "Q{}low" }));
            Assert.That(middle.Value.AtomicValues![0].StringValue, Is.EqualTo("from-high"));
            Assert.That(low.Value.AtomicValues![0].StringValue, Is.EqualTo("low-default"));
        });
    }

    [Test]
    public void ApplyImportsSelectsOnlyALowerImportPrecedenceRule()
    {
        string imported = Stylesheet(
            "<xsl:template name='imported' match='item' priority='100'><xsl:param name='p' required='yes'/></xsl:template>",
            "");
        string principal = Stylesheet(
            "<xsl:import href='imported.xsl'/>" +
            "<xsl:template name='principal' match='item' priority='-100'><xsl:apply-imports><xsl:with-param name='p' select=\"'imported-value'\"/></xsl:apply-imports></xsl:template>",
            "");
        RuntimeResolver resolver = new RuntimeResolver();
        resolver.Resources.Add("https://example.test/imported.xsl", imported);
        XsltExecutable executable = Compile(principal, resolver, XmlResourceAccess.Allowed);
        XsltTransformationState state = Initialize(executable, Parse("<item/>"), []);
        XsltTemplateRuntime runtime = state.TemplateRuntime!;

        runtime.ExecuteXsltInitialTemplateInvocation();
        XsltTemplateInvocation[] rules = runtime.Invocations.Where(item => item.Kind == XsltTemplateDispatchKind.Rule).ToArray();
        XsltRuntimeVariableBinding parameter = runtime.Variables.FindXsltFrameBinding(rules[1].Frame!, Name("p"), false)!;

        Assert.Multiple(() =>
        {
            Assert.That(rules.Select(item => item.Template!.Name), Is.EqualTo(new[] { "Q{}principal", "Q{}imported" }));
            Assert.That(rules[1].Rule!.ImportPrecedence, Is.LessThan(rules[0].Rule!.ImportPrecedence));
            Assert.That(parameter.Value.AtomicValues![0].StringValue, Is.EqualTo("imported-value"));
        });
    }

    [Test]
    public void EnforcesTemplateContextDeclarationsDuringInvocation()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'/>" +
            "<xsl:template name='element-only'><xsl:context-item use='required' as='element()'/></xsl:template>" +
            "<xsl:template name='absent'><xsl:context-item use='absent'/></xsl:template>",
            "");
        XsltTemplateRuntime runtime = InitializeNamed(source, "entry", [], null).TemplateRuntime!;

        XsltTypeException error = Assert.Throws<XsltTypeException>(() => runtime.InvokeXsltNamedTemplate(Name("element-only"), Atomic("wrong", XdmAtomicType.String), [], null, "#unnamed"))!;
        runtime.InvokeXsltNamedTemplate(Name("absent"), Node(Parse("<item/>")), [], null, "#unnamed");
        XsltTemplateInvocation absent = runtime.Invocations.Single(item => item.Template!.Name == "Q{}absent");

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE3090));
            Assert.That(absent.ContextItem.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void ImplementsTextOnlyAndFailingBuiltInRules()
    {
        XsltTemplateRuntime textRuntime = Initialize(Stylesheet("", ""), Parse("<root>hello</root>"), []).TemplateRuntime!;
        XdmSequence text = textRuntime.ExecuteXsltInitialTemplateInvocation();
        string failSource = Stylesheet("<xsl:mode visibility='private' on-no-match='fail'/>", "");
        XsltTemplateRuntime failRuntime = Initialize(failSource, Parse("<root/>"), []).TemplateRuntime!;

        XsltDynamicException failure = Assert.Throws<XsltDynamicException>(() => failRuntime.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(text.AtomicValues![0].StringValue, Is.EqualTo("hello"));
            Assert.That(textRuntime.Invocations.All(item => item.Kind == XsltTemplateDispatchKind.BuiltIn), Is.True);
            Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0555));
        });
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XsltTransformationState Initialize(string source, XmlDocument document, XsltParameter[] parameters)
    {
        return Initialize(Compile(source, new RuntimeResolver(), XmlResourceAccess.Denied), document, parameters);
    }

    private static XsltTransformationState Initialize(XsltExecutable executable, XmlDocument document, XsltParameter[] parameters)
    {
        XsltTransformOptions options = new XsltTransformOptions { SourceDocument = document, Parameters = parameters };
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
    }

    private static XsltTransformationState InitializeNamed(string source, string name, XsltParameter[] parameters, XdmSequence? context)
    {
        XsltTransformOptions options = new XsltTransformOptions { InitialTemplate = Name(name), Parameters = parameters, GlobalContextItem = context };
        return new XsltTransformationEngine().InitializeXsltTransformation(Compile(source, new RuntimeResolver(), XmlResourceAccess.Denied), options);
    }

    private static XsltExecutable Compile(string source, RuntimeResolver resolver, XmlResourceAccess access)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = access,
                AllowedSchemes = ["https"],
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, resolver);
    }

    private static XmlDocument Parse(string source)
    {
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source, BaseUri = "https://example.test/source.xml" }, new XmlParserOptions(), null, null, null);
    }

    private static XmlName Name(string localName)
    {
        return new XmlName { LocalName = localName };
    }

    private static XdmSequence Atomic(string value, XdmAtomicType type)
    {
        XdmAtomicValue atomic = type == XdmAtomicType.UntypedAtomic
            ? new XdmTypeConverter().CreateXdmUntypedAtomicValue(value)
            : new XdmFactory().CreateXdmStringAtomicValue(value, type);
        return new XdmSequenceOperations().CreateXdmAtomicSequence(atomic);
    }

    private static XdmSequence Node(XmlDocument document)
    {
        return new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentElement!);
    }

    private sealed class RuntimeResolver : IXmlResourceResolver
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
