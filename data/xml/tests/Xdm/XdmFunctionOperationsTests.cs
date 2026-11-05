using xml;

namespace xml.Tests;

[TestFixture]
public class XdmFunctionOperationsTests
{
    [Test]
    public void FunctionCreationSnapshotsMetadataAndCapturedVariables()
    {
        RecordingFunction implementation = new RecordingFunction { Result = Sequence(Atomic("true", XdmAtomicType.Boolean)) };
        XmlName functionName = Name("urn:functions", "check");
        XdmAtomicType[] parameterTypes = [XdmAtomicType.String];
        XdmOccurrence[] parameterOccurrences = [XdmOccurrence.ExactlyOne];
        XPathFunctionBinding binding = new XPathFunctionBinding
        {
            Name = functionName,
            Arity = 1,
            ParameterTypes = parameterTypes,
            ParameterOccurrences = parameterOccurrences,
            ResultType = XdmAtomicType.Boolean,
            ResultOccurrence = XdmOccurrence.ExactlyOne,
            BindingKey = "check#1",
            IsDeterministic = true
        };
        XmlName variableName = Name("urn:variables", "captured");
        XdmSequence capturedValue = Sequence(Atomic("original", XdmAtomicType.String));
        XPathVariableBinding captured = new XPathVariableBinding { Name = variableName, Value = capturedValue };
        XPathVariableBinding[] closure = [captured];

        XdmFunctionItem function = new XdmFunctionOperations().CreateXdmFunctionItem(binding, implementation, closure);

        functionName.LocalName = "changed";
        parameterTypes[0] = XdmAtomicType.Integer;
        parameterOccurrences[0] = XdmOccurrence.ZeroOrMore;
        variableName.LocalName = "changed";
        capturedValue.ItemKinds![0] = XdmItemKind.Node;
        closure[0] = new XPathVariableBinding();

        Assert.Multiple(() =>
        {
            Assert.That(function.Name!.LocalName, Is.EqualTo("check"));
            Assert.That(function.ParameterTypes, Is.EqualTo(new[] { XdmAtomicType.String }));
            Assert.That(function.ParameterOccurrences, Is.EqualTo(new[] { XdmOccurrence.ExactlyOne }));
            Assert.That(function.ResultType, Is.EqualTo(XdmAtomicType.Boolean));
            Assert.That(function.BindingKey, Is.EqualTo("check#1"));
            Assert.That(function.Implementation, Is.SameAs(implementation));
            Assert.That(function.ClosureVariables, Is.Not.SameAs(closure));
            Assert.That(function.ClosureVariables![0], Is.Not.SameAs(captured));
            Assert.That(function.ClosureVariables[0].Name!.LocalName, Is.EqualTo("captured"));
            Assert.That(function.ClosureVariables[0].Value!.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic }));
            Assert.That(function.IsDeterministic, Is.True);
            Assert.That(function.IsImmutable, Is.True);
        });
    }

    [Test]
    public void ExtensionInvocationConvertsArgumentsAndResultAndUsesClosureContext()
    {
        RecordingFunction implementation = new RecordingFunction { Result = Sequence(Atomic("2.5", XdmAtomicType.Decimal)) };
        XPathFunctionBinding binding = new XPathFunctionBinding
        {
            Name = Name("urn:functions", "convert"),
            Arity = 2,
            ParameterTypes = [XdmAtomicType.Integer, XdmAtomicType.String],
            ParameterOccurrences = [XdmOccurrence.ExactlyOne, XdmOccurrence.ExactlyOne],
            ResultType = XdmAtomicType.Double,
            ResultOccurrence = XdmOccurrence.ExactlyOne,
            BindingKey = "convert#2"
        };
        XPathVariableBinding captured = Variable("urn:variables", "shared", "closure");
        XPathDynamicContext callerContext = new XPathDynamicContext
        {
            Variables = [Variable("urn:variables", "caller", "caller"), Variable("urn:variables", "shared", "dynamic")],
            BaseUri = "urn:base",
            ImplicitTimezoneMinutes = -480,
            EnvironmentNames = ["A"],
            EnvironmentValues = ["B"],
            MaximumOperations = 99
        };
        XdmFunctionOperations functions = new XdmFunctionOperations();
        XdmFunctionItem function = functions.CreateXdmFunctionItem(binding, implementation, [captured]);

        XdmSequence result = functions.InvokeXdmFunctionItem(
            function,
            [Sequence(new XdmTypeConverter().CreateXdmUntypedAtomicValue("7")), Sequence(new XdmAnyUriDatatype().ParseXdmAnyUri("urn:value"))],
            callerContext,
            new XPathStaticContext());

        Assert.Multiple(() =>
        {
            Assert.That(implementation.Binding!.BindingKey, Is.EqualTo("convert#2"));
            Assert.That(implementation.Binding.Name, Is.Not.SameAs(binding.Name));
            Assert.That(implementation.Arguments![0].AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(implementation.Arguments![0].AtomicValues![0].CanonicalValue, Is.EqualTo("7"));
            Assert.That(implementation.Arguments[1].AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(implementation.Context, Is.Not.SameAs(callerContext));
            Assert.That(implementation.Context!.Variables, Has.Length.EqualTo(2));
            Assert.That(VariableValue(implementation.Context.Variables!, "caller"), Is.EqualTo("caller"));
            Assert.That(VariableValue(implementation.Context.Variables!, "shared"), Is.EqualTo("closure"));
            Assert.That(implementation.Context.BaseUri, Is.EqualTo("urn:base"));
            Assert.That(implementation.Context.ImplicitTimezoneMinutes, Is.EqualTo(-480));
            Assert.That(implementation.Context.EnvironmentNames, Is.Not.SameAs(callerContext.EnvironmentNames));
            Assert.That(result.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(result.AtomicValues[0].FloatingValue, Is.EqualTo(2.5d));
            Assert.That(result.IsImmutable, Is.True);
            Assert.That(callerContext.Variables, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void UnknownSignaturesPreserveHeterogeneousArgumentsAndResults()
    {
        XmlNode node = new XmlNode { Kind = XmlNodeKind.Element };
        XdmMapValue map = new XdmMapOperations().CreateEmptyXdmMap();
        XdmSequenceBuilder argumentBuilder = new XdmSequenceBuilder();
        argumentBuilder.AddXdmNode(node);
        argumentBuilder.AddXdmMap(map);
        XdmSequence argument = argumentBuilder.BuildImmutableXdmSequence();
        XdmSequenceBuilder resultBuilder = new XdmSequenceBuilder();
        resultBuilder.AddXdmMap(map);
        RecordingFunction implementation = new RecordingFunction { Result = resultBuilder.BuildImmutableXdmSequence() };
        XPathFunctionBinding binding = new XPathFunctionBinding
        {
            Arity = 1,
            ResultType = XdmAtomicType.AnyAtomicType,
            ResultOccurrence = XdmOccurrence.ZeroOrMore
        };
        XdmFunctionOperations functions = new XdmFunctionOperations();
        XdmFunctionItem function = functions.CreateXdmFunctionItem(binding, implementation, []);

        XdmSequence result = functions.InvokeXdmFunctionItem(function, [argument], new XPathDynamicContext(), new XPathStaticContext());

        Assert.Multiple(() =>
        {
            Assert.That(implementation.Arguments![0], Is.Not.SameAs(argument));
            Assert.That(implementation.Arguments[0].ItemKinds, Is.EqualTo(new[] { XdmItemKind.Node, XdmItemKind.Map }));
            Assert.That(result.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Map }));
            Assert.That(result.Maps![0], Is.SameAs(map));
            Assert.That(result.IsImmutable, Is.True);
        });
    }

    [Test]
    public void MapFunctionItemsUseAtomicSameKeyLookupAndSnapshotTheMap()
    {
        XdmMapOperations maps = new XdmMapOperations();
        XdmMapValue source = maps.CreateXdmMap(
        [
            maps.CreateXdmMapEntry(Atomic("key", XdmAtomicType.String), Sequence(Atomic("value", XdmAtomicType.String)))
        ]);
        XdmFunctionOperations functions = new XdmFunctionOperations();
        XdmFunctionItem function = functions.CreateXdmMapFunctionItem(source);
        source.Entries![0].Key!.StringValue = "changed";

        XdmSequence result = functions.InvokeXdmFunctionItem(
            function,
            [Sequence(new XdmAnyUriDatatype().ParseXdmAnyUri("key"))],
            new XPathDynamicContext(),
            new XPathStaticContext());
        XdmSequence missing = functions.InvokeXdmFunctionItem(
            function,
            [Sequence(Atomic("missing", XdmAtomicType.String))],
            new XPathDynamicContext(),
            new XPathStaticContext());

        Assert.Multiple(() =>
        {
            Assert.That(function.Arity, Is.EqualTo(1));
            Assert.That(function.Name, Is.Null);
            Assert.That(function.IsMap, Is.True);
            Assert.That(function.IsArray, Is.False);
            Assert.That(function.MapValue, Is.Not.SameAs(source));
            Assert.That(function.MapValue!.IsImmutable, Is.True);
            Assert.That(result.AtomicValues![0].StringValue, Is.EqualTo("value"));
            Assert.That(missing.Count, Is.Zero);
        });
    }

    [Test]
    public void ArrayFunctionItemsConvertIntegerArgumentsAndSnapshotMembers()
    {
        XdmArrayOperations arrays = new XdmArrayOperations();
        XdmArrayValue source = arrays.CreateXdmArray(
        [
            Sequence(Atomic("first", XdmAtomicType.String)),
            Sequence(Atomic("second", XdmAtomicType.String), Atomic("member", XdmAtomicType.String))
        ]);
        XdmFunctionOperations functions = new XdmFunctionOperations();
        XdmFunctionItem function = functions.CreateXdmArrayFunctionItem(source);
        source.Members![1].ItemKinds![0] = XdmItemKind.Node;

        XdmSequence result = functions.InvokeXdmFunctionItem(
            function,
            [Sequence(new XdmTypeConverter().CreateXdmUntypedAtomicValue("2"))],
            new XPathDynamicContext(),
            new XPathStaticContext());

        Assert.Multiple(() =>
        {
            Assert.That(function.Arity, Is.EqualTo(1));
            Assert.That(function.IsMap, Is.False);
            Assert.That(function.IsArray, Is.True);
            Assert.That(function.ArrayValue, Is.Not.SameAs(source));
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic, XdmItemKind.Atomic }));
            Assert.That(result.AtomicValues![0].StringValue, Is.EqualTo("second"));
        });
    }

    [Test]
    public void InvocationReportsArityTypeBoundsAndMissingImplementationErrors()
    {
        XdmFunctionOperations functions = new XdmFunctionOperations();
        XPathFunctionBinding binding = new XPathFunctionBinding
        {
            Arity = 1,
            ParameterTypes = [XdmAtomicType.String],
            ParameterOccurrences = [XdmOccurrence.ExactlyOne],
            ResultType = XdmAtomicType.String,
            ResultOccurrence = XdmOccurrence.ExactlyOne
        };
        XdmFunctionItem function = functions.CreateXdmFunctionItem(binding, new RecordingFunction { Result = Sequence(Atomic("ok", XdmAtomicType.String)) }, []);
        XPathTypeException arity = Assert.Throws<XPathTypeException>(() => functions.InvokeXdmFunctionItem(function, [], new XPathDynamicContext(), new XPathStaticContext()))!;
        XPathTypeException type = Assert.Throws<XPathTypeException>(() => functions.InvokeXdmFunctionItem(function, [Sequence(Atomic("1", XdmAtomicType.Integer))], new XPathDynamicContext(), new XPathStaticContext()))!;
        XdmFunctionItem array = functions.CreateXdmArrayFunctionItem(new XdmArrayOperations().CreateEmptyXdmArray());
        XPathDynamicException bounds = Assert.Throws<XPathDynamicException>(() => functions.InvokeXdmFunctionItem(array, [Sequence(Atomic("999999999999999999999999", XdmAtomicType.Integer))], new XPathDynamicContext(), new XPathStaticContext()))!;
        XdmFunctionItem missing = new XdmFunctionItem
        {
            Arity = 0,
            ResultType = XdmAtomicType.AnyAtomicType,
            ResultOccurrence = XdmOccurrence.ZeroOrMore
        };
        XPathDynamicException unavailable = Assert.Throws<XPathDynamicException>(() => functions.InvokeXdmFunctionItem(missing, [], new XPathDynamicContext(), new XPathStaticContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(arity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(type.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(bounds.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(unavailable.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
        });
    }

    [Test]
    public void InvocationRejectsInvalidMetadataClosuresAndNullResults()
    {
        XdmFunctionOperations functions = new XdmFunctionOperations();
        XPathFunctionBinding mismatched = new XPathFunctionBinding
        {
            Arity = 2,
            ParameterTypes = [XdmAtomicType.String],
            ParameterOccurrences = [XdmOccurrence.ExactlyOne],
            ResultType = XdmAtomicType.String,
            ResultOccurrence = XdmOccurrence.ExactlyOne
        };
        XPathVariableBinding duplicate1 = Variable("urn:variables", "same", "one");
        XPathVariableBinding duplicate2 = Variable("urn:variables", "same", "two");
        XPathFunctionBinding empty = new XPathFunctionBinding
        {
            Arity = 0,
            ParameterTypes = [],
            ParameterOccurrences = [],
            ResultType = XdmAtomicType.String,
            ResultOccurrence = XdmOccurrence.ExactlyOne
        };
        XdmFunctionItem nullResult = functions.CreateXdmFunctionItem(empty, new RecordingFunction(), []);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => functions.CreateXdmFunctionItem(mismatched, new RecordingFunction(), []));
            Assert.Throws<ArgumentException>(() => functions.CreateXdmFunctionItem(empty, new RecordingFunction(), [duplicate1, duplicate2]));
            Assert.Throws<ArgumentException>(() => functions.ValidateXdmFunctionItem(new XdmFunctionItem { Arity = -1 }));
            Assert.Throws<ArgumentException>(() => functions.ValidateXdmFunctionItem(new XdmFunctionItem { Arity = 1, IsMap = true, IsArray = true }));
            Assert.That(Assert.Throws<XPathDynamicException>(() => functions.InvokeXdmFunctionItem(nullResult, [], new XPathDynamicContext(), new XPathStaticContext()))!.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
        });
    }

    private sealed class RecordingFunction : IXPathFunction
    {
        internal XPathFunctionBinding? Binding;
        internal XdmSequence[]? Arguments;
        internal XPathDynamicContext? Context;
        internal XdmSequence? Result;

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            Binding = binding;
            Arguments = arguments;
            Context = context;
            return Result!;
        }
    }

    private static XPathVariableBinding Variable(string namespaceUri, string localName, string value)
    {
        return new XPathVariableBinding
        {
            Name = Name(namespaceUri, localName),
            Value = Sequence(Atomic(value, XdmAtomicType.String))
        };
    }

    private static string? VariableValue(XPathVariableBinding[] variables, string localName)
    {
        XPathVariableBinding? variable = variables.SingleOrDefault(candidate => candidate.Name!.LocalName == localName);
        return variable?.Value?.AtomicValues?[0].StringValue;
    }

    private static XmlName Name(string namespaceUri, string localName)
    {
        return new XmlName
        {
            Prefix = "p",
            LocalName = localName,
            NamespaceUri = namespaceUri,
            QualifiedName = "p:" + localName
        };
    }

    private static XdmAtomicValue Atomic(string lexical, XdmAtomicType type)
    {
        if (type == XdmAtomicType.Boolean)
        {
            return new XdmNumericDatatype().ParseXdmBoolean(lexical);
        }

        if (type == XdmAtomicType.Decimal)
        {
            return new XdmNumericDatatype().ParseXdmDecimal(lexical);
        }

        if (new XdmTypeSystem().IsXdmAtomicSubtypeOf(type, XdmAtomicType.Integer))
        {
            return new XdmNumericDatatype().ParseXdmInteger(lexical, type);
        }

        if (type == XdmAtomicType.Float || type == XdmAtomicType.Double)
        {
            return new XdmFloatingDatatype().ParseXdmFloating(lexical, type);
        }

        return new XdmStringDatatype().ParseXdmStringAtomic(lexical, type);
    }

    private static XdmSequence Sequence(params XdmAtomicValue[] values)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        foreach (XdmAtomicValue value in values)
        {
            builder.AddXdmAtomicValue(value);
        }

        return builder.BuildImmutableXdmSequence();
    }
}
