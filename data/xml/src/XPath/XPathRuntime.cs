using System;

namespace xml;

internal class XPathRuntime
{
    internal XPathPlan Plan;
    internal XPathRuntimeContext Context;
    internal XPathFocusStack Focus;
    internal XPathVariableStack Variables;
    internal XPathFunctionCallStack FunctionCalls;
    internal XPathResourceRegistry Resources;
    internal XPathRuntimeLimiter Limiter;
    internal XdmSequenceOperations Sequences;
    internal XdmFactory Factory;
    internal XPathPathEvaluator Paths;
    internal XPathComparisonEvaluator Comparisons;
    internal XPathOperatorEvaluator Operators;
    internal XPathFlowEvaluator Flows;
    internal XPathFunctionEvaluator FunctionEvaluator;
    internal XPathTypeEvaluator TypeEvaluator;

    internal XPathRuntime(XPathPlan plan, XPathDynamicContext context)
    {
        Guard.NotNull(plan, "plan");
        Plan = plan;
        Context = new XPathRuntimeContext(context);
        Focus = new XPathFocusStack(Context.Snapshot.ContextItem, Context.Snapshot.Position, Context.Snapshot.Size);
        Variables = new XPathVariableStack(plan.Environment.Variables);
        XPathVariableBinding[] dynamicVariables = Context.Snapshot.Variables == null ? [] : Context.Snapshot.Variables;
        int index = 0;
        while (index < dynamicVariables.Length)
        {
            Variables.SetXPathVariable(dynamicVariables[index]);
            index = index + 1;
        }

        FunctionCalls = new XPathFunctionCallStack(Context.MaximumDepth);
        Resources = new XPathResourceRegistry(Context.Snapshot.CollectionResolver, Context.ResourcePolicy, Context.MaximumResources, Context.HasExplicitResolverPolicy);
        Resources.ResourceResolver = Context.Snapshot.ResourceResolver == null ? new XmlDefaultResolver() : Context.Snapshot.ResourceResolver;
        Resources.MaximumResourceBytes = Context.Limits.MaximumResourceBytes > 0 ? Context.Limits.MaximumResourceBytes : 67108864;
        Limiter = new XPathRuntimeLimiter(Context.MaximumOperations, Context.MaximumDepth, Context.CancellationPollingInterval, Context.Snapshot.Cancellation);
        Sequences = new XdmSequenceOperations();
        Factory = new XdmFactory();
        Paths = new XPathPathEvaluator(this);
        Comparisons = new XPathComparisonEvaluator(this);
        Operators = new XPathOperatorEvaluator(this);
        Flows = new XPathFlowEvaluator(this);
        FunctionEvaluator = new XPathFunctionEvaluator(this);
        TypeEvaluator = new XPathTypeEvaluator(this);
    }

    internal XdmSequence EvaluateXPathPlan()
    {
        Limiter.PollXPathCancellation();
        return EvaluateXPathInstruction(Plan.Root);
    }

    internal XdmSequence EvaluateXPathInstruction(XPathPlanInstruction instruction)
    {
        Limiter.EnterXPathInstruction(instruction.Range);
        XdmSequence result;
        switch (instruction.Opcode)
        {
            case XPathPlanOpcode.Constant:
                result = EvaluateXPathConstant(instruction);
                break;
            case XPathPlanOpcode.Sequence:
                result = EvaluateXPathSequence(instruction);
                break;
            case XPathPlanOpcode.ContextItem:
                result = EvaluateXPathContextItem(instruction);
                break;
            case XPathPlanOpcode.Variable:
                result = EvaluateXPathVariable(instruction);
                break;
            case XPathPlanOpcode.Path:
                result = Paths.EvaluateXPathPath(instruction);
                break;
            case XPathPlanOpcode.Name:
            case XPathPlanOpcode.ParentStep:
            case XPathPlanOpcode.AttributeStep:
            case XPathPlanOpcode.AxisStep:
                result = Paths.EvaluateXPathAxis(instruction);
                break;
            case XPathPlanOpcode.Predicate:
                result = Paths.EvaluateXPathPredicate(instruction);
                break;
            case XPathPlanOpcode.For:
            case XPathPlanOpcode.Let:
            case XPathPlanOpcode.Quantified:
            case XPathPlanOpcode.If:
            case XPathPlanOpcode.SimpleMap:
            case XPathPlanOpcode.Arrow:
                result = Flows.EvaluateXPathFlow(instruction);
                break;
            case XPathPlanOpcode.StaticFunctionCall:
            case XPathPlanOpcode.NamedFunctionReference:
            case XPathPlanOpcode.InlineFunction:
            case XPathPlanOpcode.DynamicFunctionCall:
            case XPathPlanOpcode.MapConstructor:
            case XPathPlanOpcode.SquareArrayConstructor:
            case XPathPlanOpcode.CurlyArrayConstructor:
            case XPathPlanOpcode.Lookup:
            case XPathPlanOpcode.UnaryLookup:
                result = FunctionEvaluator.EvaluateXPathFunction(instruction);
                break;
            case XPathPlanOpcode.InstanceOf:
            case XPathPlanOpcode.TreatAs:
            case XPathPlanOpcode.CastableAs:
            case XPathPlanOpcode.CastAs:
                result = TypeEvaluator.EvaluateXPathTypeOperation(instruction);
                break;
            case XPathPlanOpcode.GeneralComparison:
            case XPathPlanOpcode.ValueComparison:
            case XPathPlanOpcode.NodeComparison:
                result = Comparisons.EvaluateXPathComparison(instruction);
                break;
            case XPathPlanOpcode.Or:
            case XPathPlanOpcode.And:
            case XPathPlanOpcode.StringConcatenation:
            case XPathPlanOpcode.Range:
            case XPathPlanOpcode.Addition:
            case XPathPlanOpcode.Subtraction:
            case XPathPlanOpcode.Multiplication:
            case XPathPlanOpcode.Division:
            case XPathPlanOpcode.IntegerDivision:
            case XPathPlanOpcode.Modulo:
            case XPathPlanOpcode.Union:
            case XPathPlanOpcode.Intersect:
            case XPathPlanOpcode.Except:
            case XPathPlanOpcode.UnaryPlus:
            case XPathPlanOpcode.UnaryMinus:
                result = Operators.EvaluateXPathOperator(instruction);
                break;
            case XPathPlanOpcode.Parenthesized:
                result = EvaluateXPathParenthesized(instruction);
                break;
            default:
                throw CreateXPathRuntimeError("The XPath runtime operation '" + instruction.Opcode + "' is not available in this evaluation stage.", XPathErrorCodes.FOER0000, instruction.Range);
        }

        Limiter.ExitXPathInstruction();
        return result;
    }

    internal XdmSequence EvaluateXPathConstant(XPathPlanInstruction instruction)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        int index = 0;
        while (index < instruction.Constants.Length)
        {
            builder.AddXdmAtomicValue(CreateXPathLiteralValue(instruction.Constants[index]));
            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal XdmAtomicValue CreateXPathLiteralValue(XPathPlanLiteral literal)
    {
        if (literal.Kind == XPathPlanLiteralKind.Integer)
        {
            return Factory.CreateXdmIntegerAtomicValue(literal.Value, XdmAtomicType.Integer);
        }

        if (literal.Kind == XPathPlanLiteralKind.Decimal)
        {
            return Factory.CreateXdmDecimalAtomicValue(literal.Value);
        }

        if (literal.Kind == XPathPlanLiteralKind.Double)
        {
            return Factory.CreateXdmFloatingAtomicValue(literal.Value, XdmAtomicType.Double);
        }

        if (literal.Kind == XPathPlanLiteralKind.Boolean)
        {
            return Factory.CreateXdmBooleanAtomicValue(literal.Value);
        }

        return Factory.CreateXdmStringAtomicValue(literal.Value, XdmAtomicType.String);
    }

    internal XdmSequence EvaluateXPathSequence(XPathPlanInstruction instruction)
    {
        XdmSequence[] values = new XdmSequence[instruction.Children.Length];
        int index = 0;
        while (index < instruction.Children.Length)
        {
            values[index] = EvaluateXPathInstruction(instruction.Children[index]);
            index = index + 1;
        }

        return Sequences.ConcatenateXdmSequences(values);
    }

    internal XdmSequence EvaluateXPathContextItem(XPathPlanInstruction instruction)
    {
        XPathFocusFrame focus = Focus.ReadXPathFocus();
        if (focus.ContextItem == null || focus.ContextItem.Count == 0)
        {
            throw CreateXPathRuntimeError("The XPath context item is absent.", XPathErrorCodes.XPDY0002, instruction.Range);
        }

        Paths.RequireXPathStreamSequenceAvailable(focus.ContextItem, instruction.Range);
        return Sequences.FreezeXdmSequenceStorage(focus.ContextItem);
    }

    internal XdmSequence EvaluateXPathVariable(XPathPlanInstruction instruction)
    {
        XmlName? name = instruction.ResolvedName;
        if (name == null)
        {
            throw CreateXPathRuntimeError("The XPath variable instruction has no expanded name.", XPathErrorCodes.XPDY0002, instruction.Range);
        }

        XPathVariableBinding? variable = Variables.FindXPathVariable(name);
        if (variable == null)
        {
            throw CreateXPathRuntimeError("No dynamic value is available for XPath variable $" + (name.LocalName == null ? "" : name.LocalName) + ".", XPathErrorCodes.XPDY0002, instruction.Range);
        }

        if (variable.Value != null)
        {
            Paths.RequireXPathStreamSequenceAvailable(variable.Value, instruction.Range);
            return Sequences.FreezeXdmSequenceStorage(variable.Value);
        }

        return ResolveXPathExternalVariable(variable, name, instruction.Range);
    }

    internal XdmSequence ResolveXPathExternalVariable(XPathVariableBinding declaration, XmlName name, SourceRange range)
    {
        IXPathVariableResolver? resolver = Context.Snapshot.VariableResolver;
        XPathVariableBinding? resolved = resolver == null ? null : resolver.ResolveXPathVariable(CopyXPathRuntimeName(name), Context.Snapshot);
        XdmSequence? supplied = null;
        if (resolved != null)
        {
            if (resolved.Name == null || !Variables.SameXPathVariableName(resolved.Name, name))
            {
                throw CreateXPathRuntimeError("The XPath variable resolver returned a binding for a different expanded name.", XPathErrorCodes.XPDY0002, range);
            }

            supplied = resolved.Value;
        }

        if (supplied == null && (!declaration.IsExternal || declaration.IsRequired))
        {
            throw CreateXPathRuntimeError("No dynamic value is available for required XPath variable $" + (name.LocalName == null ? "" : name.LocalName) + ".", XPathErrorCodes.XPDY0002, range);
        }

        XdmSequence value = supplied == null ? Sequences.CreateEmptyXdmSequence() : supplied;
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = declaration.DeclaredType;
        expected.Occurrence = declaration.DeclaredOccurrence;
        XPathStaticContext staticContext = new XPathFunctionContextFactory().CreateXPathFunctionStaticContext(Plan.Environment);
        XdmSequence converted = new XdmTypeConverter().ApplyXdmFunctionConversion(value, expected, staticContext);
        XPathVariableBinding cached = new XPathVariableBinding();
        cached.Name = name;
        cached.Value = converted;
        cached.DeclaredType = declaration.DeclaredType;
        cached.DeclaredOccurrence = declaration.DeclaredOccurrence;
        cached.IsExternal = declaration.IsExternal;
        cached.IsRequired = declaration.IsRequired;
        Variables.SetXPathVariable(cached);
        return Sequences.FreezeXdmSequenceStorage(converted);
    }

    internal XdmSequence EvaluateXPathParenthesized(XPathPlanInstruction instruction)
    {
        if (instruction.Operand == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        return EvaluateXPathInstruction(instruction.Operand);
    }

    internal XPathDynamicException CreateXPathRuntimeError(string message, string code, SourceRange range)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathRuntimeRange(range);
        return new XPathDynamicException(message, diagnostic, diagnostic.Range.Start);
    }

    internal XmlName CopyXPathRuntimeName(XmlName source)
    {
        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = source.NamespaceUri;
        result.QualifiedName = source.QualifiedName;
        return result;
    }

    internal SourceRange CopyXPathRuntimeRange(SourceRange range)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathRuntimeLocation(range.Start);
        result.End = CopyXPathRuntimeLocation(range.End);
        return result;
    }

    internal SourceLocation? CopyXPathRuntimeLocation(SourceLocation? location)
    {
        if (location == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = location.SourceUri;
        result.CharacterOffset = location.CharacterOffset;
        result.ByteOffset = location.ByteOffset;
        result.Line = location.Line;
        result.Column = location.Column;
        return result;
    }
}
