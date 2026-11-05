using System.Globalization;
using xml;

namespace xml.Tests;

[TestFixture]
public class XPathFunctionItemFunctionTests
{
    [Test]
    public void LooksUpBuiltinsConstructorsMathMapsAndArrays()
    {
        string functions = XPathNamespaces.Functions;
        string schema = XmlNamespaces.XmlSchema;
        string source = "(function-lookup(QName('" + functions + "','substring'),2)('abcd',2), " +
            "function-lookup(QName('" + schema + "','integer'),1)('42'), " +
            "function-lookup(QName('" + XPathNamespaces.Math + "','sqrt'),1)(9), " +
            "function-lookup(QName('" + XPathNamespaces.Map + "','size'),1)(map{'a':1}), " +
            "function-lookup(QName('" + XPathNamespaces.Array + "','size'),1)([1,2]))";

        XdmSequence result = Evaluate(source, Context(), new XPathDynamicContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "bcd", "42", "3", "1", "2" }));
    }

    [Test]
    public void LookupReturnsEmptyForUnknownNamesAndInvalidArities()
    {
        string source = "(count(function-lookup(QName('urn:missing','unknown'),1)), " +
            "count(function-lookup(QName('" + XPathNamespaces.Functions + "','substring'),-1)), " +
            "count(function-lookup(QName('" + XPathNamespaces.Functions + "','substring'),999999999999999999999999)))";

        XdmSequence result = Evaluate(source, Context(), new XPathDynamicContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "0", "0", "0" }));
    }

    [Test]
    public void ReportsNamesAndAritiesForEveryFunctionItemKind()
    {
        XdmSequence named = Evaluate("function-name(substring#2)", Context(), new XPathDynamicContext());
        XdmSequence anonymous = Evaluate("function-name(function($value){$value})", Context(), new XPathDynamicContext());
        XdmSequence containers = Evaluate("(function-name(map{}), function-name([]))", Context(), new XPathDynamicContext());
        string arities = "(function-arity(substring#2), function-arity(function($x,$y){$x}), " +
            "function-arity(map{'a':1}), function-arity([1,2]), function-arity(concat('a',?,?)))";
        XdmSequence result = Evaluate(arities, Context(), new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(named.AtomicValues![0].NameValue!.LocalName, Is.EqualTo("substring"));
            Assert.That(named.AtomicValues[0].NameValue!.NamespaceUri, Is.EqualTo(XPathNamespaces.Functions));
            Assert.That(anonymous.Count, Is.EqualTo(0));
            Assert.That(containers.Count, Is.EqualTo(0));
            Assert.That(StringValues(result), Is.EqualTo(new[] { "2", "2", "1", "1", "2" }));
        });
    }

    [Test]
    public void AppliesNamedAnonymousMapAndArrayFunctions()
    {
        string source = "(apply(concat#3,['a','b','c']), " +
            "apply(function($left,$right){$left + $right},[2,3]), " +
            "apply(map{'x':(6,7)},['x']), apply([8,9],[2]))";

        XdmSequence result = Evaluate(source, Context(), new XPathDynamicContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "abc", "5", "6", "7", "9" }));
    }

    [Test]
    public void ApplyReportsItsDedicatedArityError()
    {
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("apply(concat#2,['a'])", Context(), new XPathDynamicContext()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAP0001));
    }

    [Test]
    public void LooksUpRegisteredExtensionFunctions()
    {
        XPathStaticContext context = Context();
        context.Functions = [ExtensionFunctionBinding()];
        context.FunctionResolver = new ExtensionFunctionResolver();
        string source = "apply(function-lookup(QName('urn:extensions','ext:twice'),1),[5])";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());

        Assert.That(result.AtomicValues![0].IntegerValue, Is.EqualTo(10));
    }

    [Test]
    public void LookedUpFunctionsCaptureTheLookupFocus()
    {
        string source = "let $functions := array {(10,20) ! function-lookup(QName('" + XPathNamespaces.Functions + "','position'),0)} " +
            "return (apply(array:get($functions,1),[]), apply(array:get($functions,2),[]))";

        XdmSequence result = Evaluate(source, Context(), new XPathDynamicContext());

        Assert.That(StringValues(result), Is.EqualTo(new[] { "1", "2" }));
    }

    private static XPathStaticContext Context()
    {
        return new XPathStaticContext { AllowHigherOrderFunctions = true };
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

    private static XPathFunctionBinding ExtensionFunctionBinding()
    {
        return new XPathFunctionBinding
        {
            Name = new XmlName { Prefix = "ext", LocalName = "twice", NamespaceUri = "urn:extensions", QualifiedName = "ext:twice" },
            Arity = 1,
            ParameterTypes = [XdmAtomicType.Integer],
            ParameterOccurrences = [XdmOccurrence.ExactlyOne],
            ResultType = XdmAtomicType.Integer,
            ResultOccurrence = XdmOccurrence.ExactlyOne,
            BindingKey = "twice",
            IsDeterministic = true
        };
    }

    private sealed class ExtensionFunctionResolver : IXPathFunctionResolver
    {
        public IXPathFunction? ResolveXPathFunction(XmlName name, int arity, XPathStaticContext context)
        {
            return new ExtensionFunction();
        }
    }

    private sealed class ExtensionFunction : IXPathFunction
    {
        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            long input = arguments[0].AtomicValues![0].IntegerValue;
            string lexical = (input * 2).ToString(CultureInfo.InvariantCulture);
            XdmAtomicValue result = new XdmFactory().CreateXdmIntegerAtomicValue(lexical, XdmAtomicType.Integer);
            return new XdmSequenceOperations().CreateXdmAtomicSequence(result);
        }
    }
}
