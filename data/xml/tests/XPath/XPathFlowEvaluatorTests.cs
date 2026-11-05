using xml;

namespace xml.Tests;

[TestFixture]
public class XPathFlowEvaluatorTests
{
    [Test]
    public void ExpandsForBindingsInCartesianTupleOrder()
    {
        XdmSequence result = Evaluate("for $x in (1, 2), $y in ($x, $x + 10) return $y", new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(Integers(result), Is.EqualTo(new long[] { 1, 11, 2, 12 }));
    }

    [Test]
    public void ResolvesPrefixedForVariablesByExpandedName()
    {
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:variables" }]
        };

        XdmSequence result = Evaluate("for $p:value in (3, 4) return $p:value", context, new XPathDynamicContext());

        Assert.That(Integers(result), Is.EqualTo(new long[] { 3, 4 }));
    }

    [Test]
    public void EvaluatesLetBindingsSequentiallyAndBindsWholeSequences()
    {
        XdmSequence result = Evaluate("let $x := (1, 2), $y := ($x, 3) return $y", new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(Integers(result), Is.EqualTo(new long[] { 1, 2, 3 }));
    }

    [Test]
    public void RestoresShadowedVariablesAfterBindingExpressions()
    {
        XPathVariableBinding declaration = Variable("value", null, XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        XPathStaticContext staticContext = new XPathStaticContext { Variables = [declaration] };
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            Variables = [Variable("value", AtomicSequence(Integer("9")), XdmAtomicType.Integer, XdmOccurrence.ExactlyOne)]
        };

        XdmSequence result = Evaluate("(for $value in (1, 2) return $value, $value)", staticContext, dynamicContext);

        Assert.That(Integers(result), Is.EqualTo(new long[] { 1, 2, 9 }));
    }

    [Test]
    public void EvaluatesSomeEveryAndVacuousQuantification()
    {
        string source = "(some $x in (1, 2), $y in ($x, 3) satisfies $y eq 3, " +
            "every $x in (1, 2), $y in ($x, 3) satisfies $y ge $x, " +
            "some $x in () satisfies true(), every $x in () satisfies false())";

        XdmSequence result = Evaluate(source, new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(Booleans(result), Is.EqualTo(new[] { true, true, false, true }));
    }

    [Test]
    public void ShortCircuitsQuantifiedTuplesWhenTheResultIsKnown()
    {
        string source = "(some $x in (1, 2) satisfies if ($x eq 1) then true() else 1 idiv 0 eq 0, " +
            "every $x in (1, 2) satisfies if ($x eq 1) then false() else 1 idiv 0 eq 0)";

        XdmSequence result = Evaluate(source, new XPathStaticContext(), new XPathDynamicContext());

        Assert.That(Booleans(result), Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public void SelectsOnlyOneIfBranchAndReportsInvalidConditionEbv()
    {
        XPathVariableBinding flagDeclaration = Variable("flag", null, XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne);
        XPathStaticContext flagContext = new XPathStaticContext { Variables = [flagDeclaration] };
        XPathDynamicContext falseContext = new XPathDynamicContext
        {
            Variables = [Variable("flag", AtomicSequence(Boolean("false")), XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne)]
        };
        XdmSequence selected = Evaluate("if ($flag) then 1 idiv 0 else 8", flagContext, falseContext);

        XPathVariableBinding valueDeclaration = Variable("value", null, XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne);
        XPathStaticContext valueContext = new XPathStaticContext { Variables = [valueDeclaration] };
        XPathDynamicContext manyContext = new XPathDynamicContext
        {
            Variables = [Variable("value", AtomicSequence(Integer("1"), Integer("2")), XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne)]
        };
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("if ($value) then 1 else 2", valueContext, manyContext))!;

        Assert.Multiple(() =>
        {
            Assert.That(Integers(selected), Is.EqualTo(new long[] { 8 }));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0006));
            Assert.That(error.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void MapsEachInputItemWithoutSortingOrDuplicateElimination()
    {
        XmlDocument document = Parse("<root><item id='a'/><item id='b'/></root>");
        XPathDynamicContext context = new XPathDynamicContext
        {
            ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!)
        };

        XdmSequence atomics = Evaluate("(10, 20) ! (., . + 1)", new XPathStaticContext(), new XPathDynamicContext());
        XdmSequence nodes = Evaluate("/root/item ! (@id, @id)", new XPathStaticContext(), context);

        Assert.Multiple(() =>
        {
            Assert.That(Integers(atomics), Is.EqualTo(new long[] { 10, 11, 20, 21 }));
            Assert.That(nodes.Nodes!.Select(node => node.Value).ToArray(), Is.EqualTo(new[] { "a", "a", "b", "b" }));
        });
    }

    [Test]
    public void InsertsArrowInputFirstAndPassesSimpleMapFocus()
    {
        RecordingArrowFunction implementation = new RecordingArrowFunction();
        XPathFunctionBinding binding = FunctionBinding(1);
        XdmFunctionItem function = new XdmFunctionOperations().CreateXdmFunctionItem(binding, implementation, []);
        XPathPlan plan = CreateMappedArrowPlan();
        XPathDynamicContext context = new XPathDynamicContext
        {
            Variables = [Variable("function", new XdmSequenceOperations().CreateXdmFunctionSequence(function), XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne)]
        };

        XdmSequence result = new XPathRuntime(plan, context).EvaluateXPathPlan();

        Assert.That(Strings(result), Is.EqualTo(new[] { "10:1:2", "20:2:2" }));
    }

    [Test]
    public void ReportsArrowTargetAndArityErrorsWithRanges()
    {
        XPathPlan nonFunctionPlan = CreateArrowPlan(1);
        XPathDynamicContext nonFunctionContext = new XPathDynamicContext
        {
            Variables = [Variable("function", AtomicSequence(String("not callable")), XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne)]
        };
        XPathDynamicException target = Assert.Throws<XPathDynamicException>(() => new XPathRuntime(nonFunctionPlan, nonFunctionContext).EvaluateXPathPlan())!;

        XdmFunctionItem function = new XdmFunctionOperations().CreateXdmFunctionItem(FunctionBinding(2), new RecordingArrowFunction(), []);
        XPathDynamicContext arityContext = new XPathDynamicContext
        {
            Variables = [Variable("function", new XdmSequenceOperations().CreateXdmFunctionSequence(function), XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne)]
        };
        XPathDynamicException arity = Assert.Throws<XPathDynamicException>(() => new XPathRuntime(CreateArrowPlan(1), arityContext).EvaluateXPathPlan())!;

        Assert.Multiple(() =>
        {
            Assert.That(target.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(target.Diagnostic.Range, Is.Not.Null);
            Assert.That(arity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(arity.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void LowersNamedArrowsToCallsWithTheInputAsFirstArgument()
    {
        XPathPlan plan = (XPathPlan)new XPathCompiler().CompileXPath("1 => fn:boolean()", new XPathStaticContext()).Plan!;
        XPathRuntime runtime = new XPathRuntime(plan, new XPathDynamicContext());

        XPathPlanInstruction call = runtime.Flows.CreateXPathNamedArrowCall(plan.Root, plan.Root.Right!, plan.Root.Left!);

        Assert.Multiple(() =>
        {
            Assert.That(call.Opcode, Is.EqualTo(XPathPlanOpcode.StaticFunctionCall));
            Assert.That(call.ResolvedName!.LocalName, Is.EqualTo("boolean"));
            Assert.That(call.Children.Length, Is.EqualTo(1));
            Assert.That(call.Children[0], Is.SameAs(plan.Root.Left));
        });
    }

    [Test]
    public void CountsForTupleExpansionAgainstTheRuntimeWorkLimit()
    {
        XPathVariableBinding declaration = Variable("values", null, XdmAtomicType.Integer, XdmOccurrence.ZeroOrMore);
        XPathStaticContext staticContext = new XPathStaticContext { Variables = [declaration] };
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            Variables = [Variable("values", AtomicSequence(Integer("1"), Integer("2"), Integer("3")), XdmAtomicType.Integer, XdmOccurrence.ZeroOrMore)],
            MaximumOperations = 4
        };

        XmlLimitException error = Assert.Throws<XmlLimitException>(() => Evaluate("for $value in $values return $value", staticContext, dynamicContext))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(error.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void CountsTupleExpansionAgainstTheRuntimeWorkLimit()
    {
        XPathVariableBinding declaration = Variable("values", null, XdmAtomicType.Integer, XdmOccurrence.ZeroOrMore);
        XPathStaticContext staticContext = new XPathStaticContext { Variables = [declaration] };
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            Variables = [Variable("values", AtomicSequence(Integer("1"), Integer("2"), Integer("3")), XdmAtomicType.Integer, XdmOccurrence.ZeroOrMore)],
            MaximumOperations = 4
        };

        XmlLimitException error = Assert.Throws<XmlLimitException>(() => Evaluate("for $value in $values return $value", staticContext, dynamicContext))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(error.Diagnostic.Range, Is.Not.Null);
        });
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static XPathPlan CreateMappedArrowPlan()
    {
        SourceRange range = Range();
        XPathPlanInstruction map = Instruction(XPathPlanOpcode.SimpleMap, range);
        XPathPlanInstruction input = Instruction(XPathPlanOpcode.Constant, range);
        input.Constants = [new XPathPlanLiteral(XPathPlanLiteralKind.Integer, "10"), new XPathPlanLiteral(XPathPlanLiteralKind.Integer, "20")];
        map.Left = input;
        XPathPlanInstruction arrow = Instruction(XPathPlanOpcode.Arrow, range);
        arrow.Left = Instruction(XPathPlanOpcode.ContextItem, range);
        arrow.Right = VariableInstruction(range);
        map.Right = arrow;
        return new XPathPlan("mapped arrow", range, map, new XPathStaticEnvironment(new XPathStaticContext { AllowHigherOrderFunctions = true }));
    }

    private static XPathPlan CreateArrowPlan(int input)
    {
        SourceRange range = Range();
        XPathPlanInstruction arrow = Instruction(XPathPlanOpcode.Arrow, range);
        XPathPlanInstruction value = Instruction(XPathPlanOpcode.Constant, range);
        value.Constants = [new XPathPlanLiteral(XPathPlanLiteralKind.Integer, input.ToString(System.Globalization.CultureInfo.InvariantCulture))];
        arrow.Left = value;
        arrow.Right = VariableInstruction(range);
        return new XPathPlan("arrow", range, arrow, new XPathStaticEnvironment(new XPathStaticContext { AllowHigherOrderFunctions = true }));
    }

    private static XPathPlanInstruction VariableInstruction(SourceRange range)
    {
        XPathPlanInstruction variable = Instruction(XPathPlanOpcode.Variable, range);
        variable.ResolvedName = Name("function");
        return variable;
    }

    private static XPathPlanInstruction Instruction(XPathPlanOpcode opcode, SourceRange range)
    {
        return new XPathPlanInstruction(opcode, range, new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.ZeroOrMore));
    }

    private static XPathFunctionBinding FunctionBinding(int arity)
    {
        return new XPathFunctionBinding
        {
            Name = Name("record"),
            Arity = arity,
            ResultType = XdmAtomicType.String,
            ResultOccurrence = XdmOccurrence.ZeroOrMore
        };
    }

    private static XPathVariableBinding Variable(string name, XdmSequence? value, XdmAtomicType type, XdmOccurrence occurrence)
    {
        return new XPathVariableBinding
        {
            Name = Name(name),
            Value = value,
            DeclaredType = type,
            DeclaredOccurrence = occurrence
        };
    }

    private static XmlName Name(string localName)
    {
        return new XmlName { LocalName = localName, NamespaceUri = "", QualifiedName = localName };
    }

    private static XdmSequence AtomicSequence(params XdmAtomicValue[] values)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        int index = 0;
        while (index < values.Length)
        {
            builder.AddXdmAtomicValue(values[index]);
            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    private static XdmAtomicValue Integer(string value)
    {
        return new XdmFactory().CreateXdmIntegerAtomicValue(value, XdmAtomicType.Integer);
    }

    private static XdmAtomicValue Boolean(string value)
    {
        return new XdmFactory().CreateXdmBooleanAtomicValue(value);
    }

    private static XdmAtomicValue String(string value)
    {
        return new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String);
    }

    private static long[] Integers(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.IntegerValue).ToArray();
    }

    private static bool[] Booleans(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.BooleanValue).ToArray();
    }

    private static string[] Strings(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue!).ToArray();
    }

    private static SourceRange Range()
    {
        SourceLocation start = new SourceLocation { SourceUri = "urn:flow", CharacterOffset = 0, Line = 1, Column = 1 };
        SourceLocation end = new SourceLocation { SourceUri = "urn:flow", CharacterOffset = 1, Line = 1, Column = 2 };
        return new SourceRange { Start = start, End = end };
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source }, options, null, null, null);
    }

    private sealed class RecordingArrowFunction : IXPathFunction
    {
        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            long input = arguments[0].AtomicValues![0].IntegerValue;
            string value = input.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
                context.Position.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
                context.Size.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return AtomicSequence(String(value));
        }
    }
}
