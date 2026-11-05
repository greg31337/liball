using xml;

namespace xml.Tests;

[TestFixture]
public class XPathEnvironmentFunctionTests
{
    [Test]
    public void ExposesOnlyInjectedAllowlistedEnvironmentValues()
    {
        XPathDynamicContext context = new XPathDynamicContext
        {
            EnvironmentNames = ["APP_MODE", "EMPTY_VALUE", "CaseSensitive"],
            EnvironmentValues = ["test", "", "exact"]
        };

        XdmSequence names = Evaluate("available-environment-variables()", new XPathStaticContext(), context);
        XdmSequence values = Evaluate("(environment-variable('APP_MODE'), environment-variable('EMPTY_VALUE'), count(environment-variable('MISSING')))", new XPathStaticContext(), context);

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(names), Is.EqualTo(new[] { "APP_MODE", "EMPTY_VALUE", "CaseSensitive" }));
            Assert.That(StringValues(values), Is.EqualTo(new[] { "test", "", "0" }));
            Assert.That(names.IsImmutable, Is.True);
            Assert.That(values.IsImmutable, Is.True);
        });
    }

    [Test]
    public void DefaultContextDoesNotExposeHostEnvironmentVariables()
    {
        XdmSequence result = Evaluate("(count(available-environment-variables()), count(environment-variable('PATH')), count(environment-variable('HOME')))", new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "0", "0", "0" }));
    }

    [Test]
    public void EnvironmentNamesUseExactOrdinalMatching()
    {
        XPathDynamicContext context = new XPathDynamicContext
        {
            EnvironmentNames = ["MixedCase"],
            EnvironmentValues = ["value"]
        };

        XdmSequence result = Evaluate("(environment-variable('MixedCase'), count(environment-variable('mixedcase')))", new XPathStaticContext(), context);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "value", "0" }));
    }

    [Test]
    public void EvaluationUsesOneDeterministicEnvironmentSnapshot()
    {
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            EnvironmentNames = ["MODE"],
            EnvironmentValues = ["before"]
        };
        MutatingEnvironmentFunction mutation = new MutatingEnvironmentFunction(dynamicContext);
        XPathStaticContext staticContext = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "ext", NamespaceUri = "urn:environment-tests" }],
            Functions = [MutationBinding()],
            FunctionResolver = new MutationResolver(mutation)
        };
        string source = "(environment-variable('MODE'), ext:mutate(), environment-variable('MODE'), " +
            "count(environment-variable('NEW')), available-environment-variables())";

        XdmSequence result = Evaluate(source, staticContext, dynamicContext);

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(result), Is.EqualTo(new[] { "before", "before", "0", "MODE" }));
            Assert.That(dynamicContext.EnvironmentNames, Is.EqualTo(new[] { "MODE", "NEW" }));
            Assert.That(dynamicContext.EnvironmentValues, Is.EqualTo(new[] { "after", "new" }));
        });
    }

    [Test]
    public void EnvironmentFunctionsAreAvailableAsNamedFunctionItems()
    {
        XPathStaticContext staticContext = new XPathStaticContext { AllowHigherOrderFunctions = true };
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            EnvironmentNames = ["NAME"],
            EnvironmentValues = ["value"]
        };

        XdmSequence result = Evaluate("((environment-variable#1)('NAME'), (available-environment-variables#0)())", staticContext, dynamicContext);

        Assert.That(StringValues(result), Is.EqualTo(new[] { "value", "NAME" }));
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static string[] StringValues(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue!).ToArray();
    }

    private static XPathFunctionBinding MutationBinding()
    {
        return new XPathFunctionBinding
        {
            Name = new XmlName { Prefix = "ext", LocalName = "mutate", NamespaceUri = "urn:environment-tests", QualifiedName = "ext:mutate" },
            Arity = 0,
            ResultType = XdmAtomicType.AnyAtomicType,
            ResultOccurrence = XdmOccurrence.ZeroOrMore,
            BindingKey = "mutate",
            IsDeterministic = false
        };
    }

    private sealed class MutationResolver : IXPathFunctionResolver
    {
        internal IXPathFunction Function;

        internal MutationResolver(IXPathFunction function)
        {
            Function = function;
        }

        public IXPathFunction? ResolveXPathFunction(XmlName name, int arity, XPathStaticContext context)
        {
            return Function;
        }
    }

    private sealed class MutatingEnvironmentFunction : IXPathFunction
    {
        internal XPathDynamicContext Context;

        internal MutatingEnvironmentFunction(XPathDynamicContext context)
        {
            Context = context;
        }

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            Context.EnvironmentNames = ["MODE", "NEW"];
            Context.EnvironmentValues = ["after", "new"];
            return new XdmSequenceOperations().CreateEmptyXdmSequence();
        }
    }
}
