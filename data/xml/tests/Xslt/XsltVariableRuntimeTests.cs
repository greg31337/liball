using xml;

namespace xml.Tests;

[TestFixture]
public class XsltVariableRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void EvaluatesGlobalDependenciesLazilyAndCachesSuccessfulValues()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:xs='" + Xs + "' version='3.0'>" +
            "<xsl:variable name='base' as='xs:integer' select='2'/>" +
            "<xsl:variable name='total' as='xs:integer' select='$base + 3'/>" +
            "<xsl:variable name='unused' select='1 idiv 0'/>" +
            "<xsl:template name='entry'/></xsl:stylesheet>";
        XsltTransformationState state = Initialize(source, []);
        XsltVariableRuntime runtime = state.VariableRuntime!;

        XdmSequence first = runtime.EvaluateXsltGlobalVariable(Name("total"));
        XdmSequence second = runtime.EvaluateXsltGlobalVariable(Name("total"));
        XsltVariableEvaluation baseValue = FindGlobal(runtime, "base");
        XsltVariableEvaluation total = FindGlobal(runtime, "total");
        XsltVariableEvaluation unused = FindGlobal(runtime, "unused");

        Assert.Multiple(() =>
        {
            Assert.That(first.AtomicValues![0].IntegerValue, Is.EqualTo(5));
            Assert.That(second, Is.SameAs(first));
            Assert.That(baseValue.Status, Is.EqualTo(XsltVariableEvaluationStatus.Succeeded));
            Assert.That(baseValue.EvaluationCount, Is.EqualTo(1));
            Assert.That(total.EvaluationCount, Is.EqualTo(1));
            Assert.That(unused.Status, Is.EqualTo(XsltVariableEvaluationStatus.Uninitialized));
            Assert.That(unused.EvaluationCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void PreservesEmptySequenceCardinalityWhenGlobalVariableIsReferenced()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" +
            "<xsl:variable name='empty' as='empty-sequence()' select='()'/>" +
            "<xsl:template name='entry'><xsl:sequence select='$empty instance of empty-sequence()'/></xsl:template>" +
            "</xsl:stylesheet>";
        XsltExecutable executable = Compile(source);

        XsltTransformResult result = new XsltTransformer().TransformXslt(executable, new XsltTransformOptions { InitialTemplate = Name("entry") });

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PrincipalValue!.AtomicValues!.Single().BooleanValue, Is.True);
            Assert.That(((XsltStylesheetPlan)executable.Plan!).Globals.Single().Binding.DeclaredOccurrence, Is.EqualTo(XdmOccurrence.ZeroOrMore));
        });
    }

    [Test]
    public void UsesSuppliedAndDefaultGlobalParametersAndAppliesFunctionConversion()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:xs='" + Xs + "' version='3.0'>" +
            "<xsl:param name='required' as='xs:integer' required='yes'/>" +
            "<xsl:param name='defaulted' as='xs:string' select=\"'default'\"/>" +
            "<xsl:variable name='combined' as='xs:integer' select='$required + 1'/>" +
            "<xsl:template name='entry'/></xsl:stylesheet>";
        XsltParameter supplied = new XsltParameter { Name = Name("required"), Value = AtomicString("4", XdmAtomicType.UntypedAtomic) };
        XsltTransformationState state = Initialize(source, [supplied]);
        XsltVariableRuntime runtime = state.VariableRuntime!;

        XdmSequence combined = runtime.EvaluateXsltGlobalVariable(Name("combined"));
        XdmSequence required = runtime.EvaluateXsltGlobalVariable(Name("required"));
        XdmSequence defaulted = runtime.EvaluateXsltGlobalVariable(Name("defaulted"));

        Assert.Multiple(() =>
        {
            Assert.That(combined.AtomicValues![0].IntegerValue, Is.EqualTo(5));
            Assert.That(required.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(required.AtomicValues[0].IntegerValue, Is.EqualTo(4));
            Assert.That(defaulted.AtomicValues![0].StringValue, Is.EqualTo("default"));
        });
    }

    [Test]
    public void RejectsMissingRequiredGlobalAndTemplateParameters()
    {
        string globalSource = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" +
            "<xsl:param name='required' required='yes'/><xsl:template name='entry'/></xsl:stylesheet>";
        string localSource = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" +
            "<xsl:template name='entry'><xsl:param name='required' required='yes'/></xsl:template></xsl:stylesheet>";

        XsltDynamicException global = Assert.Throws<XsltDynamicException>(() => Initialize(globalSource, []))!;
        XsltDynamicException local = Assert.Throws<XsltDynamicException>(() => Initialize(localSource, []))!;

        Assert.Multiple(() =>
        {
            Assert.That(global.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0050));
            Assert.That(local.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0700));
        });
    }

    [Test]
    public void BindsOrdinaryAndTunnelParametersInSeparateChannels()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:xs='" + Xs + "' version='3.0'>" +
            "<xsl:template name='entry'>" +
            "<xsl:param name='n' as='xs:integer' required='yes'/>" +
            "<xsl:param name='token' as='xs:string' tunnel='yes' required='yes'/>" +
            "<xsl:variable name='local' as='xs:integer' select='$n + 1'/></xsl:template>" +
            "<xsl:template name='child'>" +
            "<xsl:param name='n' as='xs:integer' select='9'/>" +
            "<xsl:param name='token' as='xs:string' tunnel='yes' required='yes'/>" +
            "<xsl:variable name='copied' select='$token'/></xsl:template>" +
            "</xsl:stylesheet>";
        XsltParameter number = new XsltParameter { Name = Name("n"), Value = AtomicString("4", XdmAtomicType.UntypedAtomic) };
        XsltParameter token = new XsltParameter { Name = Name("token"), Value = AtomicString("secret", XdmAtomicType.String), IsTunnel = true };
        XsltTransformationState state = Initialize(source, [number, token]);
        XsltVariableRuntime runtime = state.VariableRuntime!;
        XsltVariableFrame parent = state.InitialVariableFrame!;
        XsltStylesheetPlan plan = (XsltStylesheetPlan)state.Executable.Plan!;
        XsltVariablePlan local = plan.Variables.Single(item => item.Scope == XsltVariableScope.Local && item.Name.LocalName == "local");
        XsltTemplatePlan child = plan.Templates.Single(item => item.Name == "Q{}child");

        XdmSequence localValue = runtime.EvaluateXsltLocalVariable(local, parent);
        XsltVariableFrame childFrame = runtime.BindXsltTemplateParameters(child.Parameters, [], parent, state.DynamicContext);
        XsltRuntimeVariableBinding childNumber = runtime.FindXsltFrameBinding(childFrame, Name("n"), false)!;
        XsltRuntimeVariableBinding childToken = runtime.FindXsltFrameBinding(childFrame, Name("token"), true)!;

        Assert.Multiple(() =>
        {
            Assert.That(localValue.AtomicValues![0].IntegerValue, Is.EqualTo(5));
            Assert.That(childNumber.Value.AtomicValues![0].IntegerValue, Is.EqualTo(9));
            Assert.That(childToken.Value.AtomicValues![0].StringValue, Is.EqualTo("secret"));
            Assert.That(runtime.FindXsltFrameBinding(childFrame, Name("token"), false), Is.Null);
        });
    }

    [Test]
    public void MemoizesInitializerAndConversionFailures()
    {
        string initializerSource = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" +
            "<xsl:variable name='bad' select='1 idiv 0'/><xsl:template name='entry'/></xsl:stylesheet>";
        XsltTransformationState initializerState = Initialize(initializerSource, []);
        XsltVariableRuntime initializerRuntime = initializerState.VariableRuntime!;

        Exception firstInitializer = Assert.Catch<Exception>(() => initializerRuntime.EvaluateXsltGlobalVariable(Name("bad")))!;
        Exception secondInitializer = Assert.Catch<Exception>(() => initializerRuntime.EvaluateXsltGlobalVariable(Name("bad")))!;

        string variableConversionSource = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:xs='" + Xs + "' version='3.0'>" +
            "<xsl:variable name='bad' as='xs:integer' select=\"'not-an-integer'\"/><xsl:template name='entry'/></xsl:stylesheet>";
        XsltTransformationState variableConversionState = Initialize(variableConversionSource, []);
        XsltVariableRuntime variableConversionRuntime = variableConversionState.VariableRuntime!;
        XsltTypeException variableConversion = Assert.Throws<XsltTypeException>(() => variableConversionRuntime.EvaluateXsltGlobalVariable(Name("bad")))!;

        string conversionSource = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:xs='" + Xs + "' version='3.0'>" +
            "<xsl:param name='bad' as='xs:integer' required='yes'/><xsl:template name='entry'/></xsl:stylesheet>";
        XsltTransformationState conversionState = Initialize(conversionSource,
            [new XsltParameter { Name = Name("bad"), Value = AtomicString("not-an-integer", XdmAtomicType.String) }]);
        XsltVariableRuntime conversionRuntime = conversionState.VariableRuntime!;

        XsltTypeException firstConversion = Assert.Throws<XsltTypeException>(() => conversionRuntime.EvaluateXsltGlobalVariable(Name("bad")))!;
        XsltTypeException secondConversion = Assert.Throws<XsltTypeException>(() => conversionRuntime.EvaluateXsltGlobalVariable(Name("bad")))!;

        Assert.Multiple(() =>
        {
            Assert.That(secondInitializer, Is.SameAs(firstInitializer));
            Assert.That(FindGlobal(initializerRuntime, "bad").EvaluationCount, Is.EqualTo(1));
            Assert.That(variableConversion.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE0570));
            Assert.That(secondConversion, Is.SameAs(firstConversion));
            Assert.That(firstConversion.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE0590));
            Assert.That(FindGlobal(conversionRuntime, "bad").EvaluationCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DetectsAReentrantGlobalDependencyAtRuntime()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" +
            "<xsl:variable name='a' select='1'/><xsl:variable name='b' select='$a + 1'/>" +
            "<xsl:template name='entry'/></xsl:stylesheet>";
        XsltExecutable executable = Compile(source);
        XsltStylesheetPlan plan = (XsltStylesheetPlan)executable.Plan!;
        XsltVariablePlan a = plan.Globals.Single(item => item.Name.LocalName == "a");
        XsltVariablePlan b = plan.Globals.Single(item => item.Name.LocalName == "b");
        a.Dependencies = [b];
        XsltTransformationState state = Initialize(executable, []);
        XsltVariableRuntime runtime = state.VariableRuntime!;

        XsltDynamicException first = Assert.Throws<XsltDynamicException>(() => runtime.EvaluateXsltGlobalVariable(Name("b")))!;
        XsltDynamicException second = Assert.Throws<XsltDynamicException>(() => runtime.EvaluateXsltGlobalVariable(Name("b")))!;

        Assert.Multiple(() =>
        {
            Assert.That(first.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0640));
            Assert.That(second, Is.SameAs(first));
            Assert.That(FindGlobal(runtime, "a").EvaluationCount, Is.EqualTo(1));
            Assert.That(FindGlobal(runtime, "b").EvaluationCount, Is.EqualTo(1));
        });
    }

    private static XsltVariableEvaluation FindGlobal(XsltVariableRuntime runtime, string localName)
    {
        return runtime.Globals.Single(item => item.Variable.Name.LocalName == localName);
    }

    private static XmlName Name(string localName)
    {
        return new XmlName { LocalName = localName };
    }

    private static XdmSequence AtomicString(string value, XdmAtomicType type)
    {
        XdmAtomicValue atomic = type == XdmAtomicType.UntypedAtomic
            ? new XdmTypeConverter().CreateXdmUntypedAtomicValue(value)
            : new XdmFactory().CreateXdmStringAtomicValue(value, type);
        return new XdmSequenceOperations().CreateXdmAtomicSequence(atomic);
    }

    private static XsltTransformationState Initialize(string source, XsltParameter[] parameters)
    {
        return Initialize(Compile(source), parameters);
    }

    private static XsltTransformationState Initialize(XsltExecutable executable, XsltParameter[] parameters)
    {
        XsltTransformOptions options = new XsltTransformOptions
        {
            InitialTemplate = Name("entry"),
            Parameters = parameters
        };
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
    }

    private static XsltExecutable Compile(string source)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Denied,
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, new XmlDefaultResolver());
    }
}
