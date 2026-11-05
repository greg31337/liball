using xml;

namespace xml.Tests;

[TestFixture]
public class XPathPlanCompilerTests
{
    [Test]
    public void PublicCompilerProducesAThreadSafePlanWithSourceMetadata()
    {
        XPathVariableBinding variable = new XPathVariableBinding
        {
            Name = Name("value"),
            DeclaredType = XdmAtomicType.Integer,
            DeclaredOccurrence = XdmOccurrence.ExactlyOne,
            IsExternal = true
        };
        XPathStaticContext context = new XPathStaticContext
        {
            BaseUri = "urn:plan-source",
            Variables = [variable]
        };

        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("$value + 1", context);
        XPathPlan plan = (XPathPlan)expression.Plan!;

        Assert.Multiple(() =>
        {
            Assert.That(plan.Source, Is.EqualTo("$value + 1"));
            Assert.That(plan.Root.Opcode, Is.EqualTo(XPathPlanOpcode.Addition));
            Assert.That(plan.Root.Left!.Opcode, Is.EqualTo(XPathPlanOpcode.Variable));
            Assert.That(plan.Root.Right!.Opcode, Is.EqualTo(XPathPlanOpcode.Constant));
            Assert.That(plan.Range.Start!.SourceUri, Is.EqualTo("urn:plan-source"));
            Assert.That(expression.Range, Is.Not.SameAs(plan.Range));
            Assert.That(expression.Range!.Start!.CharacterOffset, Is.EqualTo(plan.Range.Start!.CharacterOffset));
            Assert.That(expression.Diagnostics, Is.Empty);
            Assert.That(expression.IsThreadSafe, Is.True);
            Assert.That(expression.StaticContext, Is.SameAs(context));
        });

        expression.Range!.Start!.SourceUri = "urn:public-mutation";
        Assert.That(plan.Range.Start!.SourceUri, Is.EqualTo("urn:plan-source"));
    }

    [Test]
    public void PlanSnapshotsBoundNamesRangesAndStaticTypes()
    {
        XdmSequence value = new XdmSequence
        {
            AtomicValues = [new XdmAtomicValue { Type = XdmAtomicType.Integer, LexicalValue = "4" }],
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            Count = 1,
            IsImmutable = true
        };
        XPathVariableBinding variable = new XPathVariableBinding
        {
            Name = Name("number"),
            Value = value,
            DeclaredType = XdmAtomicType.Integer,
            DeclaredOccurrence = XdmOccurrence.ExactlyOne
        };
        XPathStaticContext context = new XPathStaticContext { Variables = [variable] };
        XPathAstNode ast = new XPathParser("$number + 1", "urn:original").ParseXPathExpression();
        XPathPlan plan = new XPathPlanCompiler(context).CompileXPathExpression(ast);

        ast.Operator = "changed";
        ast.Range.Start!.SourceUri = "urn:changed";
        ast.Left!.ResolvedName!.LocalName = "changed";
        variable.Name!.LocalName = "changed";
        variable.DeclaredType = XdmAtomicType.String;
        value.AtomicValues = [];

        Assert.Multiple(() =>
        {
            Assert.That(plan.Root.Operator, Is.EqualTo("+"));
            Assert.That(plan.Root.Range.Start!.SourceUri, Is.EqualTo("urn:original"));
            Assert.That(plan.Root.Left!.ResolvedName!.LocalName, Is.EqualTo("number"));
            Assert.That(plan.Root.Left.VariableBinding!.Name!.LocalName, Is.EqualTo("number"));
            Assert.That(plan.Root.Left.VariableBinding.DeclaredType, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(plan.Root.Left.VariableBinding.Value!.AtomicValues!.Length, Is.EqualTo(1));
            Assert.That(plan.Environment.Variables[0].Value!.AtomicValues!.Length, Is.EqualTo(1));
            Assert.That(plan.Root.StaticType.AtomicType, Is.EqualTo(XdmAtomicType.Integer));
        });
    }

    [Test]
    public void FoldsContextFreeIntegerStringBooleanAndSequenceConstants()
    {
        XPathPlan plan = Compile("((1 + 2) * 3, 'a' || 'b', 4 eq 4, -5)", new XPathStaticContext());
        XPathPlan unboundedInteger = Compile("999999999999999999999999999999 + 1", new XPathStaticContext());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Root.Opcode, Is.EqualTo(XPathPlanOpcode.Constant));
            Assert.That(plan.Root.Constants.Length, Is.EqualTo(4));
            Assert.That(plan.Root.Constants[0].Kind, Is.EqualTo(XPathPlanLiteralKind.Integer));
            Assert.That(plan.Root.Constants[0].Value, Is.EqualTo("9"));
            Assert.That(plan.Root.Constants[1].Kind, Is.EqualTo(XPathPlanLiteralKind.String));
            Assert.That(plan.Root.Constants[1].Value, Is.EqualTo("ab"));
            Assert.That(plan.Root.Constants[2].Kind, Is.EqualTo(XPathPlanLiteralKind.Boolean));
            Assert.That(plan.Root.Constants[2].Value, Is.EqualTo("true"));
            Assert.That(plan.Root.Constants[3].Value, Is.EqualTo("-5"));
            Assert.That(unboundedInteger.Root.Opcode, Is.EqualTo(XPathPlanOpcode.Constant));
            Assert.That(unboundedInteger.Root.Constants[0].Value, Is.EqualTo("1000000000000000000000000000000"));
        });
    }

    [TestCase("if (true()) then 7 else 1 idiv 0", "7")]
    [TestCase("if (false()) then 1 idiv 0 else 8", "8")]
    public void SelectsOnlyTheReachableBranchForConstantConditions(string source, string expected)
    {
        XPathPlan plan = Compile(source, new XPathStaticContext());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Root.Opcode, Is.EqualTo(XPathPlanOpcode.Constant));
            Assert.That(plan.Root.Constants.Length, Is.EqualTo(1));
            Assert.That(plan.Root.Constants[0].Value, Is.EqualTo(expected));
        });
    }

    [Test]
    public void KeepsPotentialDynamicErrorsAndUnknownBranchesInEvaluationOrder()
    {
        XPathVariableBinding flag = new XPathVariableBinding
        {
            Name = Name("flag"),
            DeclaredType = XdmAtomicType.Boolean,
            DeclaredOccurrence = XdmOccurrence.ExactlyOne
        };
        XPathStaticContext context = new XPathStaticContext { Variables = [flag] };

        XPathPlan division = Compile("1 idiv 0", context);
        XPathPlan logical = Compile("true() or (1 idiv 0 eq 0)", context);
        XPathPlan conditional = Compile("if ($flag) then 1 else 2", context);
        XPathPlan focusDependent = Compile("position() + 0", context);

        Assert.Multiple(() =>
        {
            Assert.That(division.Root.Opcode, Is.EqualTo(XPathPlanOpcode.IntegerDivision));
            Assert.That(division.Root.Left!.Constants[0].Value, Is.EqualTo("1"));
            Assert.That(division.Root.Right!.Constants[0].Value, Is.EqualTo("0"));
            Assert.That(logical.Root.Opcode, Is.EqualTo(XPathPlanOpcode.Or));
            Assert.That(logical.Root.Left!.Constants[0].Value, Is.EqualTo("true"));
            Assert.That(logical.Root.Right!.Opcode, Is.EqualTo(XPathPlanOpcode.ValueComparison));
            Assert.That(conditional.Root.Opcode, Is.EqualTo(XPathPlanOpcode.If));
            Assert.That(conditional.Root.ThenInstruction!.Constants[0].Value, Is.EqualTo("1"));
            Assert.That(conditional.Root.ElseInstruction!.Constants[0].Value, Is.EqualTo("2"));
            Assert.That(focusDependent.Root.Opcode, Is.EqualTo(XPathPlanOpcode.Addition));
            Assert.That(focusDependent.Root.Left!.Opcode, Is.EqualTo(XPathPlanOpcode.StaticFunctionCall));
        });
    }

    [Test]
    public void DoesNotFoldARegisteredFunctionThatUsesABuiltInName()
    {
        XPathFunctionBinding function = new XPathFunctionBinding
        {
            Name = new XmlName
            {
                NamespaceUri = XPathNamespaces.Functions,
                LocalName = "true",
                QualifiedName = "fn:true"
            },
            Arity = 0,
            ResultType = XdmAtomicType.Boolean,
            ResultOccurrence = XdmOccurrence.ExactlyOne,
            BindingKey = "custom-true"
        };
        XPathStaticContext context = new XPathStaticContext { Functions = [function] };

        XPathPlan plan = Compile("if (fn:true()) then 1 else 2", context);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Root.Opcode, Is.EqualTo(XPathPlanOpcode.If));
            Assert.That(plan.Root.Condition!.Opcode, Is.EqualTo(XPathPlanOpcode.StaticFunctionCall));
            Assert.That(plan.Root.Condition.FunctionBinding!.BindingKey, Is.EqualTo("custom-true"));
        });
    }

    [Test]
    public void PreservesBindingAndSequenceOrderWhileRemovingOnlyEmptyConstants()
    {
        XPathVariableBinding value = new XPathVariableBinding
        {
            Name = Name("value"),
            DeclaredType = XdmAtomicType.Integer,
            DeclaredOccurrence = XdmOccurrence.ExactlyOne
        };
        XPathStaticContext context = new XPathStaticContext { Variables = [value] };

        XPathPlan sequence = Compile("($value, 1, (), 2)", context);
        XPathPlan binding = Compile("for $x in (1, 2) return $x", context);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Root.Opcode, Is.EqualTo(XPathPlanOpcode.Sequence));
            Assert.That(sequence.Root.Children.Length, Is.EqualTo(3));
            Assert.That(sequence.Root.Children[0].Opcode, Is.EqualTo(XPathPlanOpcode.Variable));
            Assert.That(sequence.Root.Children[1].Constants[0].Value, Is.EqualTo("1"));
            Assert.That(sequence.Root.Children[2].Constants[0].Value, Is.EqualTo("2"));
            Assert.That(binding.Root.Opcode, Is.EqualTo(XPathPlanOpcode.For));
            Assert.That(binding.Root.Bindings.Length, Is.EqualTo(1));
            Assert.That(binding.Root.Bindings[0].Name, Is.EqualTo("x"));
            Assert.That(binding.Root.Bindings[0].Expression.Constants.Select(item => item.Value), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(binding.Root.Body!.Opcode, Is.EqualTo(XPathPlanOpcode.Variable));
        });
    }

    [Test]
    public void CopiesPathNodeTestAndFunctionMetadataIntoInstructions()
    {
        XPathPlan path = Compile("child::element(book, xs:string)/@id", new XPathStaticContext());
        XPathPlan function = Compile("fn:concat('a', $value)", new XPathStaticContext
        {
            Variables =
            [
                new XPathVariableBinding
                {
                    Name = Name("value"),
                    DeclaredType = XdmAtomicType.String,
                    DeclaredOccurrence = XdmOccurrence.ExactlyOne
                }
            ]
        });

        Assert.Multiple(() =>
        {
            Assert.That(path.Root.Opcode, Is.EqualTo(XPathPlanOpcode.Path));
            Assert.That(path.Root.Children[0].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Element));
            Assert.That(path.Root.Children[0].NodeTest!.ResolvedName!.LocalName, Is.EqualTo("book"));
            Assert.That(path.Root.Children[0].NodeTest!.Type!.ResolvedName!.LocalName, Is.EqualTo("string"));
            Assert.That(path.Root.Children[1].Axis, Is.EqualTo(XPathAxis.Attribute));
            Assert.That(function.Root.Opcode, Is.EqualTo(XPathPlanOpcode.StaticFunctionCall));
            Assert.That(function.Root.ResolvedName!.NamespaceUri, Is.EqualTo(XPathNamespaces.Functions));
            Assert.That(function.Root.Children.Length, Is.EqualTo(2));
        });
    }

    [Test]
    public void CompilesBindingsParametersMapsAndArraysWithoutRetainingAstNodes()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XPathPlan inlineFunction = Compile("function($x as xs:integer) as xs:integer {$x}", context);
        XPathPlan map = Compile("map {'key': 1}", context);
        XPathPlan squareArray = Compile("[1, 2]", context);
        XPathPlan curlyArray = Compile("array {(1, 2)}", context);

        Assert.Multiple(() =>
        {
            Assert.That(inlineFunction.Root.Opcode, Is.EqualTo(XPathPlanOpcode.InlineFunction));
            Assert.That(inlineFunction.Root.Parameters.Length, Is.EqualTo(1));
            Assert.That(inlineFunction.Root.Parameters[0].Name, Is.EqualTo("x"));
            Assert.That(inlineFunction.Root.Parameters[0].Type!.ResolvedName!.LocalName, Is.EqualTo("integer"));
            Assert.That(inlineFunction.Root.Type!.ResolvedName!.LocalName, Is.EqualTo("integer"));
            Assert.That(inlineFunction.Root.Body!.Opcode, Is.EqualTo(XPathPlanOpcode.Variable));
            Assert.That(map.Root.MapEntries.Length, Is.EqualTo(1));
            Assert.That(map.Root.MapEntries[0].Key.Constants[0].Value, Is.EqualTo("key"));
            Assert.That(map.Root.MapEntries[0].Value.Constants[0].Value, Is.EqualTo("1"));
            Assert.That(squareArray.Root.Children.Select(item => item.Constants[0].Value), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(curlyArray.Root.Body!.Constants.Select(item => item.Value), Is.EqualTo(new[] { "1", "2" }));
        });
    }

    private static XPathPlan Compile(string source, XPathStaticContext context)
    {
        return new XPathPlanCompiler(context).CompileXPathSource(source, "urn:plan-test");
    }

    private static XmlName Name(string localName)
    {
        return new XmlName { NamespaceUri = "", LocalName = localName, QualifiedName = localName };
    }
}
