using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace xml;

internal class XsltConstructionContentState
{
    internal XmlNode Parent;
    internal StringBuilder PendingText;
    internal bool HasChild;
    internal bool PreviousAtomic;

    internal XsltConstructionContentState(XmlNode parent)
    {
        Parent = parent;
        PendingText = new StringBuilder();
        HasChild = false;
        PreviousAtomic = false;
    }
}

internal enum XsltIterationSignal
{
    None = 0,
    Next = 1,
    Break = 2
}

internal class XsltIterationControl
{
    internal XsltIterationControl? Parent;
    internal XsltIterationSignal Signal;
    internal XsltParameter[] Parameters;
    internal XdmSequence BreakValue;

    internal XsltIterationControl(XsltIterationControl? parent)
    {
        Parent = parent;
        Signal = XsltIterationSignal.None;
        Parameters = [];
        BreakValue = new XdmSequenceOperations().CreateEmptyXdmSequence();
    }
}

internal class XsltRuntimeErrorDetails
{
    internal Exception Failure;
    internal XmlDiagnostic Diagnostic;
    internal XmlName Code;
    internal XdmSequence Value;

    internal XsltRuntimeErrorDetails(Exception failure, XmlDiagnostic diagnostic, XmlName code, XdmSequence value)
    {
        Failure = failure;
        Diagnostic = diagnostic;
        Code = code;
        Value = value;
    }
}

internal class XsltRuntimeErrorAssociation
{
    internal Exception Failure;
    internal XmlName Code;
    internal XdmSequence Value;

    internal XsltRuntimeErrorAssociation(Exception failure, XmlName code, XdmSequence value)
    {
        Failure = failure;
        Code = code;
        Value = value;
    }
}

internal class XsltSequenceConstructorRuntime
{
    internal XsltTemplateRuntime Templates;
    internal XsltTransformationState State;
    internal TreeMutation Mutation;
    internal TreeContentMutation ContentMutation;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal TreeNavigation Navigation;
    internal XsltSchemaIntegration Schemas;
    internal XsltIterationControl? Iteration;
    internal List<XsltRuntimeErrorAssociation> ErrorAssociations;

    internal XsltSequenceConstructorRuntime(XsltTemplateRuntime templates)
    {
        Templates = templates;
        State = templates.State;
        Mutation = new TreeMutation();
        ContentMutation = new TreeContentMutation(Mutation);
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Navigation = new TreeNavigation();
        XsltCompileOptions options = State.Executable.CompiledOptions == null ? State.Executable.CompileOptions == null ? new XsltCompileOptions() : State.Executable.CompileOptions : State.Executable.CompiledOptions;
        Schemas = new XsltSchemaIntegration(options);
        Schemas.SchemaSet = State.Stylesheet.SchemaSet;
        Iteration = null;
        ErrorAssociations = new List<XsltRuntimeErrorAssociation>();
    }

    internal XdmSequence EvaluateXsltSequenceConstructor(XsltSequenceConstructorPlan constructor, XsltTemplateInvocation invocation)
    {
        XdmSequence[] values = new XdmSequence[constructor.Items.Length];
        bool hasConditional = false;
        bool hasNonVacuous = false;
        int evaluatedCount = constructor.Items.Length;
        int index = 0;
        while (index < constructor.Items.Length)
        {
            XsltSequenceItemPlan item = constructor.Items[index];
            XsltInstructionKind kind = item.Instruction == null ? XsltInstructionKind.Unknown : item.Instruction.Kind;
            if (kind == XsltInstructionKind.OnEmpty || kind == XsltInstructionKind.OnNonEmpty)
            {
                values[index] = Sequences.CreateEmptyXdmSequence();
                hasConditional = true;
            }
            else
            {
                values[index] = EvaluateXsltSequenceItem(item, invocation);
                if (!IsXsltVacuousSequence(values[index]))
                {
                    hasNonVacuous = true;
                }

                if (Iteration != null && Iteration.Signal != XsltIterationSignal.None)
                {
                    evaluatedCount = index + 1;
                    break;
                }
            }

            index = index + 1;
        }

        if (!hasConditional)
        {
            return ConcatenateXsltEvaluatedItems(values, evaluatedCount);
        }

        XdmSequenceBuilder result = new XdmSequenceBuilder();
        index = 0;
        while (index < evaluatedCount)
        {
            XsltSequenceItemPlan item = constructor.Items[index];
            XsltInstructionKind kind = item.Instruction == null ? XsltInstructionKind.Unknown : item.Instruction.Kind;
            if (kind == XsltInstructionKind.OnEmpty && !hasNonVacuous)
            {
                result.AppendXdmSequence(EvaluateXsltSequenceInstruction(item, invocation));
            }
            else if (kind == XsltInstructionKind.OnNonEmpty && hasNonVacuous)
            {
                result.AppendXdmSequence(EvaluateXsltSequenceInstruction(item, invocation));
            }
            else if (kind != XsltInstructionKind.OnEmpty && kind != XsltInstructionKind.OnNonEmpty && hasNonVacuous)
            {
                result.AppendXdmSequence(values[index]);
            }

            if (Iteration != null && Iteration.Signal != XsltIterationSignal.None)
            {
                break;
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXsltSequenceItem(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan? instruction = item.Instruction;
        if (instruction != null)
        {
            Templates.RecordXsltRuntimeTrace(XsltTraceEventKind.EnterInstruction, invocation.Template == null ? null : invocation.Template.Component, instruction.Kind.ToString(), null, item.Node.Range, invocation.ContextItem, Templates.Depth);
        }

        XdmSequence result = EvaluateXsltSequenceItemCore(item, invocation);
        if (instruction != null)
        {
            Templates.RecordXsltRuntimeTrace(XsltTraceEventKind.ExitInstruction, invocation.Template == null ? null : invocation.Template.Component, instruction.Kind.ToString(), null, item.Node.Range, invocation.ContextItem, Templates.Depth);
        }

        return result;
    }

    internal XdmSequence EvaluateXsltSequenceItemCore(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        if (item.Kind == XsltSequenceItemKind.Variable && item.Variable != null && invocation.Frame != null)
        {
            Templates.Variables.EvaluateXsltLocalVariable(item.Variable, invocation.Frame);
            return Sequences.CreateEmptyXdmSequence();
        }

        if (item.Kind == XsltSequenceItemKind.Text)
        {
            return CreateXsltTextNodeSequence(EvaluateXsltTextItem(item, invocation), item.Node);
        }

        if (item.Kind == XsltSequenceItemKind.LiteralResultElement)
        {
            return ConstructXsltLiteralResultElement(item, invocation);
        }

        if (item.Kind == XsltSequenceItemKind.Extension)
        {
            return EvaluateXsltExtensionInstruction(item, invocation);
        }

        if (item.Kind != XsltSequenceItemKind.Instruction || item.Instruction == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XsltInstructionKind kind = item.Instruction.Kind;
        if (kind == XsltInstructionKind.ApplyTemplates || kind == XsltInstructionKind.CallTemplate || kind == XsltInstructionKind.NextMatch || kind == XsltInstructionKind.ApplyImports)
        {
            return Templates.ExecuteXsltTemplateInstruction(item, invocation);
        }

        if (kind == XsltInstructionKind.Sequence)
        {
            return EvaluateXsltSequenceInstruction(item, invocation);
        }

        if (kind == XsltInstructionKind.ResultDocument)
        {
            if (State.ResultDocumentRuntime == null)
            {
                throw new InvalidOperationException("The result-document runtime is not initialized.");
            }

            return State.ResultDocumentRuntime.EvaluateXsltResultDocument(item, invocation, this);
        }

        if (kind == XsltInstructionKind.If)
        {
            return EvaluateXsltIf(item, invocation);
        }

        if (kind == XsltInstructionKind.Choose)
        {
            return EvaluateXsltChoose(item, invocation);
        }

        if (kind == XsltInstructionKind.ForEach)
        {
            return EvaluateXsltForEach(item, invocation);
        }

        if (kind == XsltInstructionKind.Fork)
        {
            return item.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltStreamingFork(item.Content, invocation);
        }

        if (kind == XsltInstructionKind.ForEachGroup)
        {
            return new XsltGroupingRuntime(Templates).EvaluateXsltForEachGroup(item, invocation);
        }

        if (kind == XsltInstructionKind.AnalyzeString)
        {
            return new XsltRegexRuntime(this).EvaluateXsltAnalyzeString(item, invocation);
        }

        if (kind == XsltInstructionKind.Evaluate)
        {
            XsltEvaluateRuntime? evaluate = State.EvaluateRuntime;
            return evaluate == null ? throw new InvalidOperationException("The dynamic XPath runtime is unavailable.") : evaluate.EvaluateXsltDynamicExpression(item, invocation);
        }

        if (kind == XsltInstructionKind.SourceDocument)
        {
            return new XsltDocumentRuntime(this).EvaluateXsltSourceDocument(item, invocation);
        }

        if (kind == XsltInstructionKind.Merge)
        {
            return new XsltMergeRuntime(this).EvaluateXsltMerge(item, invocation);
        }

        if (kind == XsltInstructionKind.Map || kind == XsltInstructionKind.MapEntry || kind == XsltInstructionKind.Array || kind == XsltInstructionKind.ArrayMember)
        {
            return new XsltMapArrayRuntime(this).EvaluateXsltMapArrayInstruction(item, invocation);
        }

        if (kind == XsltInstructionKind.PerformSort)
        {
            return EvaluateXsltPerformSort(item, invocation);
        }

        if (kind == XsltInstructionKind.Iterate)
        {
            return EvaluateXsltIterate(item, invocation);
        }

        if (kind == XsltInstructionKind.NextIteration)
        {
            return EvaluateXsltNextIteration(item, invocation);
        }

        if (kind == XsltInstructionKind.Break)
        {
            return EvaluateXsltBreak(item, invocation);
        }

        if (kind == XsltInstructionKind.WherePopulated)
        {
            return EvaluateXsltWherePopulated(item, invocation);
        }

        if (kind == XsltInstructionKind.Try)
        {
            return EvaluateXsltTry(item, invocation);
        }

        if (kind == XsltInstructionKind.Assert)
        {
            return EvaluateXsltAssert(item, invocation);
        }

        if (kind == XsltInstructionKind.Message)
        {
            return EvaluateXsltMessage(item, invocation, false, XsltErrorCodes.XTMM9000);
        }

        if (kind == XsltInstructionKind.Number)
        {
            return new XsltNumberRuntime(this).EvaluateXsltNumber(item, invocation);
        }

        if (kind == XsltInstructionKind.Fallback)
        {
            return item.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltSequenceConstructor(item.Content, invocation);
        }

        if (kind == XsltInstructionKind.Unknown && item.Instruction.IsForwardCompatible)
        {
            return EvaluateXsltFallbacks(item, invocation);
        }

        if (kind == XsltInstructionKind.ValueOf)
        {
            return ConstructXsltValueOf(item, invocation);
        }

        if (kind == XsltInstructionKind.Copy)
        {
            return ConstructXsltCopy(item, invocation);
        }

        if (kind == XsltInstructionKind.CopyOf)
        {
            return ConstructXsltCopyOf(item, invocation);
        }

        if (kind == XsltInstructionKind.Document)
        {
            return ConstructXsltDocument(item, invocation);
        }

        if (kind == XsltInstructionKind.Element)
        {
            return ConstructXsltComputedElement(item, invocation);
        }

        if (kind == XsltInstructionKind.Attribute)
        {
            return ConstructXsltComputedAttribute(item, invocation);
        }

        if (kind == XsltInstructionKind.Namespace)
        {
            return ConstructXsltNamespace(item, invocation);
        }

        if (kind == XsltInstructionKind.Text)
        {
            return ConstructXsltTextInstruction(item, invocation);
        }

        if (kind == XsltInstructionKind.Comment)
        {
            return ConstructXsltComment(item, invocation);
        }

        if (kind == XsltInstructionKind.ProcessingInstruction)
        {
            return ConstructXsltProcessingInstruction(item, invocation);
        }

        return Sequences.CreateEmptyXdmSequence();
    }

    internal XdmSequence EvaluateXsltStreamingFork(XsltSequenceConstructorPlan content, XsltTemplateInvocation invocation)
    {
        XsltStreamedNodeRuntime? baseline = State.StreamedNodeRuntime;
        if (baseline == null || invocation.Frame == null || invocation.ContextItem.Count != 1)
        {
            return EvaluateXsltSequenceConstructor(content, invocation);
        }

        XdmSequenceItem focus = Sequences.ReadXdmSequenceItem(invocation.ContextItem, 0);
        if (focus.Node == null || !baseline.IsXsltStreamedNode(focus.Node))
        {
            return EvaluateXsltSequenceConstructor(content, invocation);
        }

        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < content.Items.Length)
        {
            XsltStreamedNodeRuntime branch = baseline.CloneXsltStreamedNodeRuntime();
            State.StreamedNodeRuntime = branch;
            State.DynamicContext.XsltStreamedNodes = branch;
            invocation.Frame.Context.XsltStreamedNodes = branch;
            result.AppendXdmSequence(EvaluateXsltSequenceItem(content.Items[index], invocation));
            index = index + 1;
        }

        State.StreamedNodeRuntime = baseline;
        State.DynamicContext.XsltStreamedNodes = baseline;
        invocation.Frame.Context.XsltStreamedNodes = baseline;
        baseline.AuthorizeXsltStreamNavigation(focus.Node, XPathAxis.DescendantOrSelf);

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence ConcatenateXsltEvaluatedItems(XdmSequence[] values, int count)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < count)
        {
            if (values[index] != null)
            {
                result.AppendXdmSequence(values[index]);
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXsltIf(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? test = FindXsltAttribute(item.Instruction, "test");
        XdmSequence value = test == null || test.Expression == null || invocation.Frame == null ? Sequences.CreateEmptyXdmSequence() : Templates.EvaluateXsltInstructionExpression(test.Expression, invocation.Frame);
        if (!Converter.EffectiveBooleanValueXdmSequence(value))
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        return item.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltSequenceConstructor(item.Content, invocation);
    }

    internal XdmSequence EvaluateXsltChoose(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltSequenceItemPlan[] branches = item.Content == null ? [] : item.Content.Items;
        int index = 0;
        while (index < branches.Length)
        {
            XsltSequenceItemPlan branch = branches[index];
            XsltInstructionKind kind = branch.Instruction == null ? XsltInstructionKind.Unknown : branch.Instruction.Kind;
            if (kind == XsltInstructionKind.Otherwise)
            {
                return branch.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltSequenceConstructor(branch.Content, invocation);
            }

            if (kind == XsltInstructionKind.When)
            {
                XsltInstructionAttributePlan? test = FindXsltAttribute(branch.Instruction, "test");
                XdmSequence value = test == null || test.Expression == null || invocation.Frame == null ? Sequences.CreateEmptyXdmSequence() : Templates.EvaluateXsltInstructionExpression(test.Expression, invocation.Frame);
                if (Converter.EffectiveBooleanValueXdmSequence(value))
                {
                    return branch.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltSequenceConstructor(branch.Content, invocation);
                }
            }

            index = index + 1;
        }

        return Sequences.CreateEmptyXdmSequence();
    }

    internal XdmSequence EvaluateXsltForEach(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? select = FindXsltAttribute(item.Instruction, "select");
        XdmSequence selected = select == null || select.Expression == null || invocation.Frame == null ? Sequences.CreateEmptyXdmSequence() : Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        selected = new XsltSortRuntime(Templates).SortXsltSequence(selected, item, invocation);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < selected.Count)
        {
            XdmSequence candidate = Templates.CreateXsltTemplateCandidate(selected, index);
            XPathDynamicContext context = Templates.CreateXsltTemplateFocusContext(invocation.Frame == null ? State.DynamicContext : invocation.Frame.Context, candidate, index + 1, selected.Count);
            XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame == null ? [] : invocation.Frame.TunnelParameters, context);
            XsltTemplateInvocation focused = CreateXsltFocusedInvocation(invocation, candidate, frame, index + 1, selected.Count);
            if (item.Content != null)
            {
                result.AppendXdmSequence(EvaluateXsltSequenceConstructor(item.Content, focused));
            }

            if (Iteration != null && Iteration.Signal != XsltIterationSignal.None)
            {
                break;
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXsltPerformSort(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? select = FindXsltAttribute(item.Instruction, "select");
        XdmSequence selected;
        if (select != null && select.Expression != null && invocation.Frame != null)
        {
            selected = Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        }
        else
        {
            XdmSequenceBuilder builder = new XdmSequenceBuilder();
            XsltSequenceItemPlan[] content = item.Content == null ? [] : item.Content.Items;
            int index = 0;
            while (index < content.Length)
            {
                XsltInstructionPlan? instruction = content[index].Instruction;
                if (instruction == null || instruction.Kind != XsltInstructionKind.Sort)
                {
                    builder.AppendXdmSequence(EvaluateXsltSequenceItem(content[index], invocation));
                }

                index = index + 1;
            }

            selected = builder.BuildImmutableXdmSequence();
        }

        return new XsltSortRuntime(Templates).SortXsltSequence(selected, item, invocation);
    }

    internal XdmSequence EvaluateXsltIterate(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? select = FindXsltAttribute(item.Instruction, "select");
        XdmSequence selected = select == null || select.Expression == null || invocation.Frame == null ? Sequences.CreateEmptyXdmSequence() : Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        XsltVariablePlan[] declarations = FindXsltIterationParameters(item);
        XPathDynamicContext outerContext = invocation.Frame == null ? State.DynamicContext : invocation.Frame.Context;
        XsltVariableFrame parameters = Templates.Variables.BindXsltTemplateParameters(declarations, [], invocation.Frame, outerContext);
        XsltIterationControl control = new XsltIterationControl(Iteration);
        Iteration = control;
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < selected.Count)
        {
            XdmSequence candidate = Templates.CreateXsltTemplateCandidate(selected, index);
            XPathDynamicContext context = Templates.CreateXsltTemplateFocusContext(outerContext, candidate, index + 1, selected.Count);
            XsltVariableFrame frame = new XsltVariableFrame(parameters, parameters.TunnelParameters, context);
            XsltTemplateInvocation focused = CreateXsltFocusedInvocation(invocation, candidate, frame, index + 1, selected.Count);
            result.AppendXdmSequence(EvaluateXsltIterationBody(item, focused));
            if (control.Signal == XsltIterationSignal.Break)
            {
                result.AppendXdmSequence(control.BreakValue);
                Iteration = control.Parent;
                return result.BuildImmutableXdmSequence();
            }

            if (control.Signal == XsltIterationSignal.Next)
            {
                parameters = BindXsltNextIterationParameters(declarations, parameters, control.Parameters, invocation.Frame, outerContext);
                control.Signal = XsltIterationSignal.None;
                control.Parameters = [];
            }

            index = index + 1;
        }

        XsltSequenceItemPlan? completion = FindXsltOnCompletion(item);
        if (completion != null)
        {
            XdmSequence empty = Sequences.CreateEmptyXdmSequence();
            XPathDynamicContext context = Templates.CreateXsltTemplateFocusContext(outerContext, empty, 0, 0);
            XsltVariableFrame frame = new XsltVariableFrame(parameters, parameters.TunnelParameters, context);
            XsltTemplateInvocation completed = CreateXsltFocusedInvocation(invocation, empty, frame, 0, 0);
            result.AppendXdmSequence(EvaluateXsltSequenceInstruction(completion, completed));
        }

        Iteration = control.Parent;
        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXsltIterationBody(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltSequenceItemPlan[] items = item.Content == null ? [] : item.Content.Items;
        XsltSequenceItemPlan[] body = new XsltSequenceItemPlan[items.Length];
        int count = 0;
        int index = 0;
        while (index < items.Length)
        {
            XsltSequenceItemPlan current = items[index];
            XsltInstructionKind kind = current.Instruction == null ? XsltInstructionKind.Unknown : current.Instruction.Kind;
            bool parameter = current.Variable != null && current.Variable.Scope == XsltVariableScope.IterationParameter;
            if (!parameter && kind != XsltInstructionKind.OnCompletion)
            {
                body[count] = current;
                count = count + 1;
            }

            index = index + 1;
        }

        XsltSequenceItemPlan[] effective = new XsltSequenceItemPlan[count];
        global::System.Array.Copy(body, effective, count);
        return EvaluateXsltSequenceConstructor(new XsltSequenceConstructorPlan(item.Node, effective), invocation);
    }

    internal XsltVariablePlan[] FindXsltIterationParameters(XsltSequenceItemPlan item)
    {
        List<XsltVariablePlan> result = new List<XsltVariablePlan>();
        XsltSequenceItemPlan[] items = item.Content == null ? [] : item.Content.Items;
        int index = 0;
        while (index < items.Length)
        {
            XsltVariablePlan? variable = items[index].Variable;
            if (variable != null && variable.Scope == XsltVariableScope.IterationParameter)
            {
                result.Add(variable);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltSequenceItemPlan? FindXsltOnCompletion(XsltSequenceItemPlan item)
    {
        XsltSequenceItemPlan[] items = item.Content == null ? [] : item.Content.Items;
        int index = 0;
        while (index < items.Length)
        {
            XsltSequenceItemPlan current = items[index];
            if (current.Instruction != null && current.Instruction.Kind == XsltInstructionKind.OnCompletion)
            {
                return current;
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltVariableFrame BindXsltNextIterationParameters(XsltVariablePlan[] declarations, XsltVariableFrame current, XsltParameter[] updates, XsltVariableFrame? parent, XPathDynamicContext context)
    {
        XsltParameter[] supplied = new XsltParameter[declarations.Length];
        int index = 0;
        while (index < declarations.Length)
        {
            XsltVariablePlan declaration = declarations[index];
            XsltParameter? update = Templates.Variables.FindXsltSuppliedParameter(updates, declaration.Name, false);
            XsltParameter parameter = new XsltParameter();
            parameter.Name = declaration.Name;
            parameter.Value = update == null ? current.Bindings[index].Value : update.Value;
            supplied[index] = parameter;
            index = index + 1;
        }

        return Templates.Variables.BindXsltTemplateParameters(declarations, supplied, parent, context);
    }

    internal XdmSequence EvaluateXsltNextIteration(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        if (Iteration != null && invocation.Frame != null)
        {
            Iteration.Parameters = Templates.EvaluateXsltWithParameters(item, invocation.Frame);
            Iteration.Signal = XsltIterationSignal.Next;
        }

        return Sequences.CreateEmptyXdmSequence();
    }

    internal XdmSequence EvaluateXsltBreak(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        if (Iteration != null)
        {
            Iteration.BreakValue = EvaluateXsltSequenceInstruction(item, invocation);
            Iteration.Signal = XsltIterationSignal.Break;
        }

        return Sequences.CreateEmptyXdmSequence();
    }

    internal XdmSequence EvaluateXsltWherePopulated(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XdmSequence content = item.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltSequenceConstructor(item.Content, invocation);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < content.Count)
        {
            XdmSequenceItem current = Sequences.ReadXdmSequenceItem(content, index);
            if (!IsXsltDeemedEmptyItem(current))
            {
                AppendXsltSequenceItem(result, current);
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXsltTry(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        if (invocation.Frame == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XsltVariableFrame bodyFrame = new XsltVariableFrame(invocation.Frame, invocation.Frame.TunnelParameters, invocation.Frame.Context);
        XsltTemplateInvocation bodyInvocation = CreateXsltFocusedInvocation(invocation, invocation.ContextItem, bodyFrame, invocation.Position, invocation.Size);
        XsltIterationControl? savedIteration = Iteration;
        int savedDepth = Templates.Depth;
        Task<XdmSequence> pending = Task.Run(() => EvaluateXsltTryBody(item, bodyInvocation));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        Iteration = savedIteration;
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return pending.Result;
        }

        Exception failure = Templates.ReadXsltInvocationFailure(pending.Exception);
        Templates.AttachXsltErrorContext(failure, item.Node);
        Templates.Depth = savedDepth;
        XsltRuntimeErrorDetails? details = CreateXsltRuntimeErrorDetails(failure, item.Node);
        if (details == null)
        {
            throw failure;
        }

        XsltSequenceItemPlan? handler = FindXsltCatch(item, details.Code);
        if (handler == null)
        {
            throw failure;
        }

        XsltVariableFrame catchFrame = new XsltVariableFrame(invocation.Frame, invocation.Frame.TunnelParameters, invocation.Frame.Context);
        AddXsltCatchBindings(catchFrame, details);
        XsltTemplateInvocation catchInvocation = CreateXsltFocusedInvocation(invocation, invocation.ContextItem, catchFrame, invocation.Position, invocation.Size);
        return EvaluateXsltSequenceInstruction(handler, catchInvocation);
    }

    internal XdmSequence EvaluateXsltTryBody(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? select = FindXsltAttribute(item.Instruction, "select");
        if (select != null && select.Expression != null && invocation.Frame != null)
        {
            return Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        }

        XsltSequenceItemPlan[] items = item.Content == null ? [] : item.Content.Items;
        XsltSequenceItemPlan[] body = new XsltSequenceItemPlan[items.Length];
        int count = 0;
        int index = 0;
        while (index < items.Length)
        {
            XsltSequenceItemPlan current = items[index];
            XsltInstructionKind kind = current.Instruction == null ? XsltInstructionKind.Unknown : current.Instruction.Kind;
            if (kind != XsltInstructionKind.Catch && kind != XsltInstructionKind.Fallback)
            {
                body[count] = current;
                count = count + 1;
            }

            index = index + 1;
        }

        XsltSequenceItemPlan[] effective = new XsltSequenceItemPlan[count];
        global::System.Array.Copy(body, effective, count);
        return EvaluateXsltSequenceConstructor(new XsltSequenceConstructorPlan(item.Node, effective), invocation);
    }

    internal XsltSequenceItemPlan? FindXsltCatch(XsltSequenceItemPlan item, XmlName code)
    {
        XsltSequenceItemPlan[] items = item.Content == null ? [] : item.Content.Items;
        int index = 0;
        while (index < items.Length)
        {
            XsltInstructionPlan? instruction = items[index].Instruction;
            if (instruction != null && instruction.Kind == XsltInstructionKind.Catch && MatchesXsltCatchErrors(instruction, code))
            {
                return items[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool MatchesXsltCatchErrors(XsltInstructionPlan instruction, XmlName code)
    {
        XsltInstructionAttributePlan? attribute = FindXsltAttribute(instruction, "errors");
        string source = attribute == null ? "*" : attribute.Source.Trim();
        string[] tokens = source.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        while (index < tokens.Length)
        {
            if (MatchesXsltErrorNameTest(tokens[index], instruction.StaticContext.Namespaces, code))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool MatchesXsltErrorNameTest(string token, XmlNamespaceBinding[] namespaces, XmlName code)
    {
        string codeNamespace = code.NamespaceUri == null ? "" : code.NamespaceUri;
        string codeLocal = code.LocalName == null ? "" : code.LocalName;
        if (token == "*")
        {
            return true;
        }

        if (token.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = token.IndexOf('}');
            return close >= 2 && token.Substring(2, close - 2) == codeNamespace && token.Substring(close + 1) == codeLocal;
        }

        int colon = token.IndexOf(':');
        if (colon < 0)
        {
            return codeNamespace.Length == 0 && token == codeLocal;
        }

        string prefix = token.Substring(0, colon);
        string local = token.Substring(colon + 1);
        if (prefix == "*")
        {
            return local == codeLocal;
        }

        string? namespaceUri = FindXsltNamespaceUri(namespaces, prefix);
        return namespaceUri != null && namespaceUri == codeNamespace && (local == "*" || local == codeLocal);
    }

    internal XsltRuntimeErrorDetails? CreateXsltRuntimeErrorDetails(Exception failure, XmlNode source)
    {
        XmlDiagnostic? diagnostic = Templates.ReadXsltExceptionDiagnostic(failure);
        if (diagnostic == null)
        {
            return null;
        }

        if (diagnostic.Range == null)
        {
            diagnostic.Range = source.Range;
        }

        XsltRuntimeErrorAssociation? association = FindXsltErrorAssociation(failure);
        XmlName code = association == null ? ParseXsltDiagnosticCode(diagnostic.Code) : association.Code;
        XdmSequence value = association == null ? Sequences.CreateEmptyXdmSequence() : association.Value;
        string? typeName = failure.GetType().FullName;
        if (association == null && typeName == "xml.XPathDynamicException")
        {
            XPathDynamicException xpath = (XPathDynamicException)failure;
            if (xpath.ErrorName != null)
            {
                code = xpath.ErrorName;
            }

            if (xpath.ErrorValue != null)
            {
                value = xpath.ErrorValue;
            }
        }

        return new XsltRuntimeErrorDetails(failure, diagnostic, code, value);
    }

    internal XsltRuntimeErrorAssociation? FindXsltErrorAssociation(Exception failure)
    {
        int index = ErrorAssociations.Count - 1;
        while (index >= 0)
        {
            if (object.ReferenceEquals(ErrorAssociations[index].Failure, failure))
            {
                return ErrorAssociations[index];
            }

            index = index - 1;
        }

        return null;
    }

    internal XmlName ParseXsltDiagnosticCode(string? source)
    {
        string value = source == null || source.Length == 0 ? XPathErrorCodes.FOER0000 : source;
        if (value.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = value.IndexOf('}');
            if (close >= 2 && close < value.Length - 1)
            {
                return new XmlName { LocalName = value.Substring(close + 1), NamespaceUri = value.Substring(2, close - 2) };
            }
        }

        return new XmlName { Prefix = "err", LocalName = value, QualifiedName = "err:" + value, NamespaceUri = XPathNamespaces.Errors };
    }

    internal void AddXsltCatchBindings(XsltVariableFrame frame, XsltRuntimeErrorDetails details)
    {
        string lexicalCode = details.Code.Prefix == null || details.Code.Prefix.Length == 0 ? "err:" + details.Code.LocalName : details.Code.Prefix + ":" + details.Code.LocalName;
        XdmSequence code = Sequences.CreateXdmAtomicSequence(new XdmQNameDatatype().CreateXdmQNameValue(details.Code.NamespaceUri, lexicalCode));
        XdmSequence description = Sequences.CreateXdmAtomicSequence(new XdmFactory().CreateXdmStringAtomicValue(details.Diagnostic.Message == null ? details.Failure.Message : details.Diagnostic.Message, XdmAtomicType.String));
        SourceLocation? location = details.Diagnostic.Range == null ? null : details.Diagnostic.Range.Start;
        XdmSequence module = location == null || location.SourceUri == null ? Sequences.CreateEmptyXdmSequence() : Sequences.CreateXdmAtomicSequence(new XdmFactory().CreateXdmStringAtomicValue(location.SourceUri, XdmAtomicType.String));
        XdmSequence line = location == null || location.Line <= 0 ? Sequences.CreateEmptyXdmSequence() : Sequences.CreateXdmAtomicSequence(new XdmFactory().CreateXdmIntegerAtomicValue(location.Line.ToString(System.Globalization.CultureInfo.InvariantCulture), XdmAtomicType.Integer));
        XdmSequence column = location == null || location.Column <= 0 ? Sequences.CreateEmptyXdmSequence() : Sequences.CreateXdmAtomicSequence(new XdmFactory().CreateXdmIntegerAtomicValue(location.Column.ToString(System.Globalization.CultureInfo.InvariantCulture), XdmAtomicType.Integer));
        frame.DynamicBindings.Add(CreateXsltCatchBinding("code", code, XdmAtomicType.QName, XdmOccurrence.ExactlyOne));
        frame.DynamicBindings.Add(CreateXsltCatchBinding("description", description, XdmAtomicType.String, XdmOccurrence.ZeroOrOne));
        frame.DynamicBindings.Add(CreateXsltCatchBinding("value", details.Value, XdmAtomicType.AnyAtomicType, XdmOccurrence.ZeroOrMore));
        frame.DynamicBindings.Add(CreateXsltCatchBinding("module", module, XdmAtomicType.String, XdmOccurrence.ZeroOrOne));
        frame.DynamicBindings.Add(CreateXsltCatchBinding("line-number", line, XdmAtomicType.Integer, XdmOccurrence.ZeroOrOne));
        frame.DynamicBindings.Add(CreateXsltCatchBinding("column-number", column, XdmAtomicType.Integer, XdmOccurrence.ZeroOrOne));
        frame.DynamicBindings.Add(CreateXsltCatchBinding("additional", Sequences.CreateEmptyXdmSequence(), XdmAtomicType.AnyAtomicType, XdmOccurrence.ZeroOrMore));
    }

    internal XPathVariableBinding CreateXsltCatchBinding(string localName, XdmSequence value, XdmAtomicType type, XdmOccurrence occurrence)
    {
        XPathVariableBinding result = new XPathVariableBinding();
        result.Name = new XmlName { Prefix = "err", LocalName = localName, QualifiedName = "err:" + localName, NamespaceUri = XPathNamespaces.Errors };
        result.Value = value;
        result.DeclaredType = type;
        result.DeclaredOccurrence = occurrence;
        return result;
    }

    internal XdmSequence EvaluateXsltAssert(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        if (!State.Options.EnableAssertions)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XsltInstructionAttributePlan? test = FindXsltAttribute(item.Instruction, "test");
        Task<bool> pending = Task.Run(() => test != null && test.Expression != null && invocation.Frame != null && Converter.EffectiveBooleanValueXdmSequence(Templates.EvaluateXsltInstructionExpression(test.Expression, invocation.Frame)));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion && pending.Result)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        return EvaluateXsltMessage(item, invocation, true, XsltErrorCodes.XTMM9001);
    }

    internal XdmSequence EvaluateXsltMessage(XsltSequenceItemPlan item, XsltTemplateInvocation invocation, bool forceTerminate, string defaultCode)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A message instruction has no plan.") : item.Instruction;
        XsltIterationControl? savedIteration = Iteration;
        Task<XdmSequence> pending = Task.Run(() => CreateXsltEvaluatedMessageDocument(item, invocation));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        Iteration = savedIteration;
        XdmSequence document = pending.Status == TaskStatus.RanToCompletion ? pending.Result : CreateXsltMessageDocument(Sequences.CreateEmptyXdmSequence());
        XmlNode root = document.Nodes == null || document.Nodes.Length == 0 ? throw new InvalidOperationException("A message document has no root node.") : document.Nodes[0];
        XdmNodeAccessor accessor = new XdmNodeAccessor();
        string text = accessor.GetXdmNodeStringValue(accessor.WrapXmlNode(root));
        bool terminate = forceTerminate || ReadXsltEffectiveBooleanAttribute(instruction, "terminate", invocation, false);
        XmlName code = ResolveXsltMessageErrorCode(instruction, invocation, defaultCode);
        XsltMessage message = new XsltMessage();
        message.Text = text;
        message.Code = code;
        message.Terminate = terminate;
        message.Range = item.Node.Range;
        message.Content = document;
        RecordXsltMessage(message);
        if (terminate)
        {
            string diagnosticCode = code.NamespaceUri == XPathNamespaces.Errors ? code.LocalName == null ? defaultCode : code.LocalName : "Q{" + (code.NamespaceUri == null ? "" : code.NamespaceUri) + "}" + code.LocalName;
            XmlDiagnostic diagnostic = new XmlDiagnostic();
            diagnostic.Code = diagnosticCode;
            diagnostic.Category = XmlErrorCategory.XsltDynamic;
            diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
            diagnostic.Message = text;
            diagnostic.Range = item.Node.Range;
            XsltTerminationException failure = new XsltTerminationException(text, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
            ErrorAssociations.Add(new XsltRuntimeErrorAssociation(failure, code, document));
            throw failure;
        }

        return Sequences.CreateEmptyXdmSequence();
    }

    internal XdmSequence CreateXsltEvaluatedMessageDocument(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        return CreateXsltMessageDocument(EvaluateXsltMessageContent(item, invocation));
    }

    internal XdmSequence EvaluateXsltMessageContent(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        XsltInstructionAttributePlan? select = FindXsltAttribute(item.Instruction, "select");
        if (select != null && select.Expression != null && invocation.Frame != null)
        {
            result.AppendXdmSequence(Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame));
        }

        if (item.Content != null)
        {
            result.AppendXdmSequence(EvaluateXsltSequenceConstructor(item.Content, invocation));
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence CreateXsltMessageDocument(XdmSequence content)
    {
        XmlDocument document = CreateXsltResultDocument(null);
        document.BaseUri = null;
        document.DocumentUri = null;
        XmlNode root = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.DocumentFragment, null, null);
        document.DocumentNode = root;
        AppendXsltConstructedContent(root, content, false);
        root.Kind = XmlNodeKind.Document;
        return Sequences.CreateXdmNodeSequence(root);
    }

    internal bool ReadXsltEffectiveBooleanAttribute(XsltInstructionPlan instruction, string name, XsltTemplateInvocation invocation, bool fallback)
    {
        XsltInstructionAttributePlan? attribute = FindXsltAttribute(instruction, name);
        if (attribute == null)
        {
            return fallback;
        }

        string value = EvaluateXsltAttributeTemplate(attribute, invocation, "").Trim();
        return value == "yes" || value == "true" || value == "1";
    }

    internal XmlName ResolveXsltMessageErrorCode(XsltInstructionPlan instruction, XsltTemplateInvocation invocation, string fallback)
    {
        XsltInstructionAttributePlan? attribute = FindXsltAttribute(instruction, "error-code");
        string value = attribute == null ? "" : EvaluateXsltAttributeTemplate(attribute, invocation, "").Trim();
        if (value.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = value.IndexOf('}');
            string local = close < 2 ? "" : value.Substring(close + 1);
            if (close >= 2 && new XdmStringDatatype().IsValidXdmName(local, true))
            {
                return new XmlName { LocalName = local, NamespaceUri = value.Substring(2, close - 2) };
            }
        }
        else
        {
            int colon = value.IndexOf(':');
            string prefix = colon < 0 ? "" : value.Substring(0, colon);
            string local = colon < 0 ? value : value.Substring(colon + 1);
            string? namespaceUri = colon < 0 ? "" : FindXsltNamespaceUri(instruction.StaticContext.Namespaces, prefix);
            if (namespaceUri != null && new XdmStringDatatype().IsValidXdmName(local, true) && (prefix.Length == 0 || new XdmStringDatatype().IsValidXdmName(prefix, true)))
            {
                return new XmlName { Prefix = prefix, LocalName = local, QualifiedName = value, NamespaceUri = namespaceUri };
            }
        }

        return new XmlName { Prefix = "err", LocalName = fallback, QualifiedName = "err:" + fallback, NamespaceUri = XPathNamespaces.Errors };
    }

    internal void RecordXsltMessage(XsltMessage message)
    {
        XsltMessage[] existing = State.Result.Messages == null ? [] : State.Result.Messages;
        XsltMessage[] result = new XsltMessage[existing.Length + 1];
        global::System.Array.Copy(existing, result, existing.Length);
        result[existing.Length] = message;
        State.Result.Messages = result;
        if (State.Options.MessageReceiver != null)
        {
            State.Options.MessageReceiver.ReceiveXsltMessage(message);
        }
    }

    internal XdmSequence EvaluateXsltFallbacks(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltSequenceItemPlan[] items = item.Content == null ? [] : item.Content.Items;
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < items.Length)
        {
            XsltSequenceItemPlan current = items[index];
            if (current.Instruction != null && current.Instruction.Kind == XsltInstructionKind.Fallback)
            {
                result.AppendXdmSequence(EvaluateXsltSequenceItem(current, invocation));
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal bool IsXsltVacuousSequence(XdmSequence sequence)
    {
        int index = 0;
        while (index < sequence.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(sequence, index);
            if (!IsXsltVacuousItem(item))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsXsltVacuousItem(XdmSequenceItem item)
    {
        if (item.Node != null)
        {
            if (item.Node.Kind == XmlNodeKind.Document || item.Node.Kind == XmlNodeKind.DocumentFragment)
            {
                XdmNodeAccessor accessor = new XdmNodeAccessor();
                return accessor.GetXdmNodeChildren(accessor.WrapXmlNode(item.Node)).Length == 0;
            }

            return (item.Node.Kind == XmlNodeKind.Text || item.Node.Kind == XmlNodeKind.CData) && (item.Node.Value == null || item.Node.Value.Length == 0);
        }

        if (item.AtomicValue != null)
        {
            return Converter.ReadXdmAtomicStringValue(item.AtomicValue).Length == 0;
        }

        if (item.Array != null)
        {
            XdmSequence[] members = item.Array.Members == null ? [] : item.Array.Members;
            int index = 0;
            while (index < members.Length)
            {
                if (!IsXsltVacuousSequence(members[index]))
                {
                    return false;
                }

                index = index + 1;
            }

            return true;
        }

        return false;
    }

    internal bool IsXsltDeemedEmptyItem(XdmSequenceItem item)
    {
        if (item.Node != null)
        {
            XdmNodeAccessor accessor = new XdmNodeAccessor();
            if (item.Node.Kind == XmlNodeKind.Document || item.Node.Kind == XmlNodeKind.DocumentFragment || item.Node.Kind == XmlNodeKind.Element)
            {
                return accessor.GetXdmNodeChildren(accessor.WrapXmlNode(item.Node)).Length == 0;
            }

            return accessor.GetXdmNodeStringValue(accessor.WrapXmlNode(item.Node)).Length == 0;
        }

        if (item.AtomicValue != null)
        {
            return Converter.ReadXdmAtomicStringValue(item.AtomicValue).Length == 0;
        }

        if (item.Map != null)
        {
            return item.Map.Count == 0;
        }

        if (item.Array != null)
        {
            XdmSequence[] members = item.Array.Members == null ? [] : item.Array.Members;
            int index = 0;
            while (index < members.Length)
            {
                int memberIndex = 0;
                while (memberIndex < members[index].Count)
                {
                    if (!IsXsltDeemedEmptyItem(Sequences.ReadXdmSequenceItem(members[index], memberIndex)))
                    {
                        return false;
                    }

                    memberIndex = memberIndex + 1;
                }

                index = index + 1;
            }

            return true;
        }

        return false;
    }

    internal void AppendXsltSequenceItem(XdmSequenceBuilder builder, XdmSequenceItem item)
    {
        if (item.Node != null) builder.AddXdmNode(item.Node);
        else if (item.AtomicValue != null) builder.AddXdmAtomicValue(item.AtomicValue);
        else if (item.Function != null) builder.AddXdmFunction(item.Function);
        else if (item.Map != null) builder.AddXdmMap(item.Map);
        else if (item.Array != null) builder.AddXdmArray(item.Array);
    }

    internal XsltTemplateInvocation CreateXsltFocusedInvocation(XsltTemplateInvocation basis, XdmSequence contextItem, XsltVariableFrame frame, int position, int size)
    {
        XsltTemplateInvocation result = new XsltTemplateInvocation(basis.Kind, basis.Template, basis.Rule, basis.Mode, contextItem, position, size, basis.Depth);
        result.Frame = frame;
        return result;
    }

    internal XdmSequence EvaluateXsltVariableContent(XsltVariablePlan variable, XsltVariableFrame? frame)
    {
        XsltSequenceConstructorPlan plan = CreateXsltRuntimeSequencePlan(variable.Declaration, variable.Content, false);
        XdmSequence context = frame == null || frame.Context.ContextItem == null ? State.GlobalContextItem : frame.Context.ContextItem;
        XsltTemplateInvocation invocation = new XsltTemplateInvocation(XsltTemplateDispatchKind.Named, null, null, State.Stylesheet.InitialMode, context, frame == null ? 0 : frame.Context.Position, frame == null ? 0 : frame.Context.Size, Templates.Depth);
        if (frame == null)
        {
            XPathDynamicContext variableContext = Templates.Variables.CreateXsltVariableDynamicContext(null);
            variableContext.XsltCurrentComponent = variable.Component;
            invocation.Frame = new XsltVariableFrame(null, [], variableContext);
        }
        else
        {
            invocation.Frame = frame;
        }
        return EvaluateXsltSequenceConstructor(plan, invocation);
    }

    internal XsltSequenceConstructorPlan CreateXsltRuntimeSequencePlan(XmlNode owner, XmlNode[] nodes, bool preserveWhitespace)
    {
        XsltSequenceItemPlan[] items = new XsltSequenceItemPlan[nodes.Length];
        int count = 0;
        int index = 0;
        while (index < nodes.Length)
        {
            XmlNode node = nodes[index];
            if (node.Kind == XmlNodeKind.Element)
            {
                items[count] = CreateXsltRuntimeElementPlan(node);
                count = count + 1;
            }
            else if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
            {
                string value = node.Value == null ? "" : node.Value;
                if (preserveWhitespace || !IsXsltRuntimeWhitespace(value))
                {
                    XsltSequenceItemPlan text = new XsltSequenceItemPlan(node, XsltSequenceItemKind.Text, "", "#text");
                    text.TextTemplate = FindXsltValueTemplate(node);
                    items[count] = text;
                    count = count + 1;
                }
            }

            index = index + 1;
        }

        XsltSequenceItemPlan[] result = new XsltSequenceItemPlan[count];
        global::System.Array.Copy(items, result, count);
        return new XsltSequenceConstructorPlan(owner, result);
    }

    internal bool IsXsltRuntimeWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != ' ' && character != '\t' && character != '\r' && character != '\n')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal XsltSequenceItemPlan CreateXsltRuntimeElementPlan(XmlNode node)
    {
        XsltVariablePlan? variable = FindXsltVariablePlan(node);
        XsltInstructionPlan? instruction = FindXsltInstructionPlan(node);
        string namespaceUri = node.Name == null || node.Name.NamespaceUri == null ? "" : node.Name.NamespaceUri;
        string localName = node.Name == null || node.Name.LocalName == null ? "" : node.Name.LocalName;
        XsltStaticContext? staticContext = FindXsltStaticContext(node);
        XsltExtensionBinding? extension = FindXsltRuntimeInstructionExtension(namespaceUri, localName, staticContext);
        bool extensionElement = false;
        int namespaceIndex = 0;
        while (staticContext != null && namespaceIndex < staticContext.ExtensionNamespaceUris.Length)
        {
            if (staticContext.ExtensionNamespaceUris[namespaceIndex] == namespaceUri)
            {
                extensionElement = true;
            }

            namespaceIndex = namespaceIndex + 1;
        }

        XsltSequenceItemKind kind = variable != null ? XsltSequenceItemKind.Variable : namespaceUri == XsltNamespaces.Xslt ? XsltSequenceItemKind.Instruction : extensionElement ? XsltSequenceItemKind.Extension : XsltSequenceItemKind.LiteralResultElement;
        XsltSequenceItemPlan result = new XsltSequenceItemPlan(node, kind, namespaceUri, localName);
        result.Variable = variable;
        result.Instruction = instruction;
        result.Extension = extension;
        result.ExtensionPolicy = staticContext == null ? null : staticContext.ExtensionPolicy;
        result.AttributeTemplates = FindXsltValueTemplates(node);
        bool preserveWhitespace = namespaceUri == XsltNamespaces.Xslt && localName == "text";
        result.Content = CreateXsltRuntimeSequencePlan(node, node.Children == null ? [] : node.Children, preserveWhitespace);
        return result;
    }

    internal XdmSequence EvaluateXsltExtensionInstruction(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltExtensionBinding? binding = item.Extension;
        IXsltExtensionInstruction? implementation = binding == null ? null : binding.InstructionImplementation;
        if (binding == null || implementation == null)
        {
            if (HasXsltExtensionFallback(item))
            {
                return EvaluateXsltFallbacks(item, invocation);
            }

            throw CreateXsltConstructionError("No implementation or fallback is available for the extension instruction.", XsltErrorCodes.XTDE1450, item.Node);
        }

        XdmSequence content = EvaluateXsltExtensionContent(item, invocation);
        XPathDynamicContext context = invocation.Frame == null ? State.DynamicContext : invocation.Frame.Context;
        XmlExtensionPolicy policy = item.ExtensionPolicy == null ? new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(null) : item.ExtensionPolicy;
        XmlExtensionRuntimeState state = context.ExtensionState == null ? new XmlExtensionRuntimeState() : context.ExtensionState;
        context.ExtensionState = state;
        if (context.Cancellation != null && context.Cancellation.IsXmlCancellationRequested())
        {
            throw new System.OperationCanceledException("XSLT extension execution was canceled.");
        }

        if (!policy.AllowReentrancy && state.Depth > 0)
        {
            throw CreateXsltConstructionError("Reentrant extension execution is denied by policy.", XsltErrorCodes.XTDE1450, item.Node);
        }

        if (state.InvocationCount >= policy.MaximumInvocations)
        {
            throw CreateXsltConstructionError("The extension invocation limit was exceeded.", XsltErrorCodes.XTDE1450, item.Node);
        }

        state.InvocationCount = state.InvocationCount + 1;
        state.Depth = state.Depth + 1;
        System.Threading.Tasks.Task<XdmSequence> pending = System.Threading.Tasks.Task.Run(() => implementation.ExecuteXsltExtensionInstruction(binding, content, context));
        ((System.IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        state.Depth = state.Depth - 1;
        if (pending.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && pending.Result != null)
        {
            if (context.Cancellation != null && context.Cancellation.IsXmlCancellationRequested())
            {
                throw new System.OperationCanceledException("XSLT extension execution was canceled.");
            }

            return new XdmSequenceOperations().FreezeXdmSequenceStorage(pending.Result);
        }

        string detail = new XmlExtensionPolicyEvaluator().ReadXmlExtensionFailureMessage(pending.Exception);
        throw CreateXsltConstructionError("The extension instruction failed: " + detail, XsltErrorCodes.XTDE1450, item.Node);
    }

    internal XdmSequence EvaluateXsltExtensionContent(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltSequenceItemPlan[] items = item.Content == null ? [] : item.Content.Items;
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < items.Length)
        {
            XsltInstructionPlan? instruction = items[index].Instruction;
            if (instruction == null || instruction.Kind != XsltInstructionKind.Fallback)
            {
                result.AppendXdmSequence(EvaluateXsltSequenceItem(items[index], invocation));
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal bool HasXsltExtensionFallback(XsltSequenceItemPlan item)
    {
        XsltSequenceItemPlan[] items = item.Content == null ? [] : item.Content.Items;
        int index = 0;
        while (index < items.Length)
        {
            XsltInstructionPlan? instruction = items[index].Instruction;
            if (instruction != null && instruction.Kind == XsltInstructionKind.Fallback)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltExtensionBinding? FindXsltRuntimeInstructionExtension(string namespaceUri, string localName, XsltStaticContext? context)
    {
        if (context == null)
        {
            return null;
        }

        int namespaceIndex = 0;
        bool enabled = false;
        while (namespaceIndex < context.ExtensionNamespaceUris.Length)
        {
            if (context.ExtensionNamespaceUris[namespaceIndex] == namespaceUri)
            {
                enabled = true;
            }

            namespaceIndex = namespaceIndex + 1;
        }

        XsltExtensionBinding[] bindings = context.Extensions;
        int index = 0;
        while (enabled && index < bindings.Length)
        {
            XmlName? name = bindings[index].Name;
            if (bindings[index].IsInstruction && bindings[index].InstructionImplementation != null && name != null && name.LocalName == localName && (name.NamespaceUri == null ? "" : name.NamespaceUri) == namespaceUri)
            {
                return bindings[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XdmSequence EvaluateXsltSequenceInstruction(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? select = FindXsltAttribute(item.Instruction, "select");
        if (select != null && select.Expression != null && invocation.Frame != null)
        {
            return Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        }

        return item.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltSequenceConstructor(item.Content, invocation);
    }

    internal XdmSequence ConstructXsltValueOf(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? select = FindXsltAttribute(item.Instruction, "select");
        XdmSequence value = select == null || select.Expression == null || invocation.Frame == null ? Sequences.CreateEmptyXdmSequence() : Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        string separator = EvaluateXsltAttributeTemplate(item.Instruction == null ? null : FindXsltAttribute(item.Instruction, "separator"), invocation, " ");
        return CreateXsltTextNodeSequence(ConvertXsltSequenceToString(value, separator), item.Node);
    }

    internal XdmSequence ConstructXsltCopy(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A copy instruction has no plan.") : item.Instruction;
        XsltInstructionAttributePlan? select = FindXsltAttribute(instruction, "select");
        XdmSequence selected;
        if (select != null && select.Expression != null && invocation.Frame != null)
        {
            selected = Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        }
        else
        {
            selected = invocation.ContextItem;
            if (selected.Count == 0)
            {
                throw CreateXsltConstructionTypeError("xsl:copy requires a context item when select is absent.", XsltErrorCodes.XTTE0945, instruction.Instruction);
            }
        }

        if (selected.Count == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        if (selected.Count != 1)
        {
            throw CreateXsltConstructionTypeError("The select expression of xsl:copy must return at most one item.", XsltErrorCodes.XTTE3180, instruction.Instruction);
        }

        XdmSequenceItem selectedItem = Sequences.ReadXdmSequenceItem(selected, 0);
        if (selectedItem.Node == null)
        {
            return selected;
        }

        XmlNode source = selectedItem.Node;
        bool copyNamespaces = ReadXsltBooleanAttribute(instruction, "copy-namespaces", true);
        XsltSchemaValidationMode validation = ReadXsltValidationMode(instruction);
        ValidateXsltNamespaceSensitiveCopy(source, copyNamespaces, validation, instruction.Instruction);
        if (source.Kind != XmlNodeKind.Element && source.Kind != XmlNodeKind.Document && source.Kind != XmlNodeKind.DocumentFragment)
        {
            XmlNode simpleCopy = CreateXsltCopiedNode(source, instruction, false, copyNamespaces);
            ApplyXsltCopyValidation(simpleCopy, instruction, validation, false);
            return Sequences.CreateXdmNodeSequence(simpleCopy);
        }

        XmlDocument document = CreateXsltResultDocument(Navigation.GetXmlNodeBaseUriOperation(source));
        XmlNode copy = Mutation.CreateXmlTreeNodeOperation(document, source.Kind, source.Name, source.Value);
        copy.Range = source.Range;
        if (source.Kind == XmlNodeKind.Document || source.Kind == XmlNodeKind.DocumentFragment)
        {
            document.DocumentNode = copy;
        }
        else
        {
            if (copyNamespaces)
            {
                CopyXsltNodeNamespaces(copy, source);
            }

            EnsureXsltCopiedNameBindings(copy, instruction.Instruction);
        }

        XdmSequence content = item.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltCopyContent(item.Content, invocation, selected, select != null);
        AppendXsltConstructedContent(copy, content, source.Kind == XmlNodeKind.Element);
        if (source.Kind == XmlNodeKind.Element && HasXsltXmlBaseAttribute(copy))
        {
            document.BaseUri = instruction.StaticContext.BaseUri;
            document.DocumentUri = instruction.StaticContext.BaseUri;
        }

        if (source.Kind == XmlNodeKind.Element && ReadXsltBooleanAttribute(instruction, "inherit-namespaces", true))
        {
            InheritXsltCopiedNamespaces(copy);
        }

        ApplyXsltCopyValidation(copy, instruction, validation, true);
        return Sequences.CreateXdmNodeSequence(copy);
    }

    internal XdmSequence ConstructXsltCopyOf(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A copy-of instruction has no plan.") : item.Instruction;
        XsltInstructionAttributePlan? select = FindXsltAttribute(instruction, "select");
        XdmSequence selected = select == null || select.Expression == null || invocation.Frame == null ? Sequences.CreateEmptyXdmSequence() : Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        bool copyNamespaces = ReadXsltBooleanAttribute(instruction, "copy-namespaces", true);
        bool copyAccumulators = ReadXsltBooleanAttribute(instruction, "copy-accumulators", false);
        XsltSchemaValidationMode validation = ReadXsltValidationMode(instruction);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < selected.Count)
        {
            XdmSequenceItem selectedItem = Sequences.ReadXdmSequenceItem(selected, index);
            if (selectedItem.Node == null)
            {
                AppendXsltCopiedItem(result, selectedItem);
            }
            else
            {
                XsltStreamedNodeRuntime? streamed = State.StreamedNodeRuntime;
                if (streamed != null && streamed.IsXsltStreamedNode(selectedItem.Node) && !streamed.IsXsltStreamedNodeAvailable(selectedItem.Node))
                {
                    throw CreateXsltConstructionError("A streamed node cannot be copied after its focus lifetime has ended.", XPathErrorCodes.XPDY0130, instruction.Instruction);
                }

                if (streamed != null && streamed.IsXsltStreamedNodeConsumed(selectedItem.Node))
                {
                    throw CreateXsltConstructionError("A streamed node cannot be copied after its subtree has been consumed.", XPathErrorCodes.XPDY0130, instruction.Instruction);
                }

                if (streamed != null && !streamed.ReserveXsltStreamGrounding(selectedItem.Node, false))
                {
                    throw CreateXsltConstructionError("Copying the streamed node would exceed the configured node limit.", XPathErrorCodes.XPDY0130, instruction.Instruction);
                }

                ValidateXsltNamespaceSensitiveCopy(selectedItem.Node, copyNamespaces, validation, instruction.Instruction);
                XmlNode copy = CreateXsltCopiedNode(selectedItem.Node, instruction, true, copyNamespaces);
                ApplyXsltCopyValidation(copy, instruction, validation, false);
                if (copyAccumulators)
                {
                    RecordXsltAccumulatorCopies(selectedItem.Node, copy);
                }

                result.AddXdmNode(copy);
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXsltCopyContent(XsltSequenceConstructorPlan content, XsltTemplateInvocation invocation, XdmSequence selected, bool changeFocus)
    {
        if (!changeFocus || invocation.Frame == null)
        {
            return EvaluateXsltSequenceConstructor(content, invocation);
        }

        XPathDynamicContext context = Templates.CreateXsltTemplateFocusContext(invocation.Frame.Context, selected, 1, 1);
        XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame.TunnelParameters, context);
        XsltTemplateInvocation focused = new XsltTemplateInvocation(invocation.Kind, invocation.Template, invocation.Rule, invocation.Mode, selected, 1, 1, invocation.Depth);
        focused.Frame = frame;
        return EvaluateXsltSequenceConstructor(content, focused);
    }

    internal void AppendXsltCopiedItem(XdmSequenceBuilder result, XdmSequenceItem item)
    {
        if (item.AtomicValue != null) result.AddXdmAtomicValue(item.AtomicValue);
        else if (item.Function != null) result.AddXdmFunction(item.Function);
        else if (item.Map != null) result.AddXdmMap(item.Map);
        else if (item.Array != null) result.AddXdmArray(item.Array);
        else throw new InvalidOperationException("A copied XDM item is incomplete.");
    }

    internal XmlNode CreateXsltCopiedNode(XmlNode source, XsltInstructionPlan instruction, bool deep, bool copyNamespaces)
    {
        XmlDocument document = CreateXsltCopyDocument(source, instruction);
        XmlNode copy = Mutation.ImportXmlTreeNodeOperation(document, source, deep);
        if (copy.Kind == XmlNodeKind.Document)
        {
            document.DocumentNode = copy;
            XmlNode[] children = copy.Children == null ? [] : copy.Children;
            int childIndex = 0;
            while (childIndex < children.Length)
            {
                if (children[childIndex].Kind == XmlNodeKind.Element)
                {
                    document.DocumentElement = children[childIndex];
                    break;
                }

                childIndex = childIndex + 1;
            }
        }

        if (!copyNamespaces)
        {
            RemoveXsltCopiedNamespaces(copy, instruction.Instruction);
        }
        else if (deep)
        {
            CopyXsltDeepNamespaces(source, copy);
        }

        return copy;
    }

    internal XmlDocument CreateXsltCopyDocument(XmlNode source, XsltInstructionPlan instruction)
    {
        string? baseUri = Navigation.GetXmlNodeBaseUriOperation(source);
        if (source.Kind == XmlNodeKind.Element && HasXsltXmlBaseAttribute(source))
        {
            baseUri = instruction.StaticContext.BaseUri;
        }

        return CreateXsltResultDocument(baseUri);
    }

    internal bool HasXsltXmlBaseAttribute(XmlNode element)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null && name.LocalName == "base" && (name.NamespaceUri == XmlNamespaces.Xml || name.QualifiedName == "xml:base"))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void RemoveXsltCopiedNamespaces(XmlNode node, XmlNode source)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            node.NamespaceBindings = [];
            EnsureXsltCopiedNameBindings(node, source);
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            RemoveXsltCopiedNamespaces(children[index], source);
            index = index + 1;
        }
    }

    internal void EnsureXsltCopiedNameBindings(XmlNode element, XmlNode source)
    {
        XmlName? elementName = element.Name;
        if (elementName != null)
        {
            EnsureXsltElementNameBinding(element, elementName, source);
        }

        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            EnsureXsltAttributeNameBinding(element, attributes[index], source);
            index = index + 1;
        }
    }

    internal void EnsureXsltAttributeNameBinding(XmlNode element, XmlNode attribute, XmlNode source)
    {
        XmlName? name = attribute.Name;
        if (name == null)
        {
            return;
        }

        string prefix = name.Prefix == null ? "" : name.Prefix;
        string uri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        if (prefix.Length == 0 && uri.Length != 0)
        {
            prefix = uri == XmlNamespaces.Xml ? "xml" : CreateXsltNamespacePrefix(element);
            name.Prefix = prefix;
            name.QualifiedName = prefix + ":" + (name.LocalName == null ? "" : name.LocalName);
        }

        if (prefix.Length != 0 && uri.Length != 0)
        {
            AddXsltElementNamespace(element, prefix, uri, source);
        }
    }

    internal string CreateXsltNamespacePrefix(XmlNode element)
    {
        int suffix = 0;
        string prefix = "ns" + suffix;
        while (HasXsltNamespacePrefix(element, prefix))
        {
            suffix = suffix + 1;
            prefix = "ns" + suffix;
        }

        return prefix;
    }

    internal void CopyXsltNodeNamespaces(XmlNode target, XmlNode source)
    {
        target.NamespaceBindings = Mutation.CopyXmlMutationNamespaces(Navigation.SelectXmlNamespacesOperation(source));
    }

    internal void CopyXsltDeepNamespaces(XmlNode source, XmlNode copy)
    {
        if (source.Kind == XmlNodeKind.Element && copy.Kind == XmlNodeKind.Element)
        {
            CopyXsltNodeNamespaces(copy, source);
        }

        XmlNode[] sourceChildren = source.Children == null ? [] : source.Children;
        XmlNode[] copyChildren = copy.Children == null ? [] : copy.Children;
        int index = 0;
        while (index < sourceChildren.Length && index < copyChildren.Length)
        {
            CopyXsltDeepNamespaces(sourceChildren[index], copyChildren[index]);
            index = index + 1;
        }
    }

    internal void InheritXsltCopiedNamespaces(XmlNode parent)
    {
        XmlNode[] children = parent.Children == null ? [] : parent.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            XmlNode child = children[childIndex];
            if (child.Kind == XmlNodeKind.Element)
            {
                XmlNamespaceBinding[] inherited = parent.NamespaceBindings == null ? [] : parent.NamespaceBindings;
                int namespaceIndex = 0;
                while (namespaceIndex < inherited.Length)
                {
                    string? optionalPrefix = inherited[namespaceIndex].Prefix;
                    string prefix = optionalPrefix == null ? "" : optionalPrefix;
                    if (!inherited[namespaceIndex].IsUndeclaration && !HasXsltNamespacePrefix(child, prefix))
                    {
                        string? optionalUri = inherited[namespaceIndex].NamespaceUri;
                        string uri = optionalUri == null ? "" : optionalUri;
                        AddXsltElementNamespace(child, prefix, uri, parent);
                    }

                    namespaceIndex = namespaceIndex + 1;
                }

                InheritXsltCopiedNamespaces(child);
            }

            childIndex = childIndex + 1;
        }
    }

    internal bool HasXsltNamespacePrefix(XmlNode element, string prefix)
    {
        XmlNamespaceBinding[] bindings = element.NamespaceBindings == null ? [] : element.NamespaceBindings;
        int index = 0;
        while (index < bindings.Length)
        {
            string? optionalPrefix = bindings[index].Prefix;
            string current = optionalPrefix == null ? "" : optionalPrefix;
            if (current == prefix)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ReadXsltBooleanAttribute(XsltInstructionPlan instruction, string name, bool fallback)
    {
        XsltInstructionAttributePlan? attribute = FindXsltAttribute(instruction, name);
        if (attribute == null)
        {
            return fallback;
        }

        string value = attribute.Source.Trim();
        return value == "yes" || value == "true" || value == "1";
    }

    internal XsltSchemaValidationMode ReadXsltValidationMode(XsltInstructionPlan instruction)
    {
        XsltInstructionAttributePlan? attribute = FindXsltAttribute(instruction, "validation");
        string value = attribute == null ? instruction.StaticContext.Validation == XsltDefaultValidation.Preserve ? "preserve" : "strip" : attribute.Source.Trim();
        if (value == "preserve") return XsltSchemaValidationMode.Preserve;
        if (value == "strict") return XsltSchemaValidationMode.Strict;
        if (value == "lax") return XsltSchemaValidationMode.Lax;
        return XsltSchemaValidationMode.Strip;
    }

    internal void ApplyXsltCopyValidation(XmlNode node, XsltInstructionPlan instruction, XsltSchemaValidationMode validation, bool shallow)
    {
        XsltInstructionAttributePlan? type = FindXsltAttribute(instruction, "type");
        Schemas.ApplyXsltSchemaNodeValidation(node, validation, type == null ? null : type.ResolvedName, shallow);
    }

    internal void ValidateXsltNamespaceSensitiveCopy(XmlNode node, bool copyNamespaces, XsltSchemaValidationMode validation, XmlNode source)
    {
        if (validation != XsltSchemaValidationMode.Preserve)
        {
            return;
        }

        if (node.Kind == XmlNodeKind.Attribute && IsXsltNamespaceSensitiveNode(node) || !copyNamespaces && ContainsXsltNamespaceSensitiveNode(node))
        {
            throw CreateXsltConstructionTypeError("Namespace-sensitive typed content cannot be copied without its namespace context.", XsltErrorCodes.XTTE0950, source);
        }
    }

    internal bool ContainsXsltNamespaceSensitiveNode(XmlNode node)
    {
        if (IsXsltNamespaceSensitiveNode(node))
        {
            return true;
        }

        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            if (IsXsltNamespaceSensitiveNode(attributes[attributeIndex]))
            {
                return true;
            }

            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            if (ContainsXsltNamespaceSensitiveNode(children[childIndex]))
            {
                return true;
            }

            childIndex = childIndex + 1;
        }

        return false;
    }

    internal bool IsXsltNamespaceSensitiveNode(XmlNode node)
    {
        XmlSchemaTypeInfo? type = node.TypeInfo;
        return type != null && (type.AtomicType == XdmAtomicType.QName || type.AtomicType == XdmAtomicType.Notation);
    }

    internal void RecordXsltAccumulatorCopies(XmlNode source, XmlNode copy)
    {
        if (source.Kind != XmlNodeKind.Attribute && source.Kind != XmlNodeKind.Namespace)
        {
            State.AccumulatorCopies.Add(new XsltAccumulatorCopyMapping(source, copy));
        }

        XmlNode[] sourceChildren = source.Children == null ? [] : source.Children;
        XmlNode[] copyChildren = copy.Children == null ? [] : copy.Children;
        int index = 0;
        while (index < sourceChildren.Length && index < copyChildren.Length)
        {
            RecordXsltAccumulatorCopies(sourceChildren[index], copyChildren[index]);
            index = index + 1;
        }
    }

    internal XdmSequence ConstructXsltDocument(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XmlDocument document = CreateXsltResultDocument(item.Instruction == null ? null : item.Instruction.StaticContext.BaseUri);
        XmlNode root = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Document, null, null);
        document.DocumentNode = root;
        XdmSequence content = item.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltSequenceConstructor(item.Content, invocation);
        AppendXsltConstructedContent(root, content, false);
        return Sequences.CreateXdmNodeSequence(root);
    }

    internal XdmSequence ConstructXsltComputedElement(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A computed element has no instruction plan.") : item.Instruction;
        string lexicalName = EvaluateXsltAttributeTemplate(FindXsltAttribute(instruction, "name"), invocation, "");
        XsltInstructionAttributePlan? namespaceAttribute = FindXsltAttribute(instruction, "namespace");
        string? namespaceUri = namespaceAttribute == null ? null : EvaluateXsltAttributeTemplate(namespaceAttribute, invocation, "");
        XmlName name = ResolveXsltComputedName(lexicalName, namespaceUri, instruction, false);
        XmlDocument document = CreateXsltResultDocument(instruction.StaticContext.BaseUri);
        XmlNode element = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Element, name, null);
        EnsureXsltElementNameBinding(element, name, instruction.Instruction);
        XdmSequence content = item.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltSequenceConstructor(item.Content, invocation);
        AppendXsltConstructedContent(element, content, true);
        return Sequences.CreateXdmNodeSequence(element);
    }

    internal XdmSequence ConstructXsltLiteralResultElement(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltStaticContext? context = FindXsltStaticContext(item.Node);
        XmlName sourceName = item.Node.Name == null ? new XmlName() : item.Node.Name;
        XmlName name = ApplyXsltNamespaceAlias(sourceName);
        XmlDocument document = CreateXsltResultDocument(context == null ? null : context.BaseUri);
        XmlNode element = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Element, name, null);
        CopyXsltLiteralNamespaces(element, item.Node, context);
        EnsureXsltElementNameBinding(element, name, item.Node);
        ConstructXsltLiteralAttributes(element, item, invocation);
        XdmSequence content = item.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltSequenceConstructor(item.Content, invocation);
        AppendXsltConstructedContent(element, content, true);
        return Sequences.CreateXdmNodeSequence(element);
    }

    internal void ConstructXsltLiteralAttributes(XmlNode element, XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XmlNode[] attributes = item.Node.Attributes == null ? [] : item.Node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode source = attributes[index];
            string namespaceUri = source.Name == null || source.Name.NamespaceUri == null ? "" : source.Name.NamespaceUri;
            if (namespaceUri != XsltNamespaces.Xslt && namespaceUri != XmlNamespaces.Xmlns)
            {
                XmlName name = ApplyXsltNamespaceAlias(source.Name == null ? new XmlName() : source.Name);
                string value = EvaluateXsltNodeValueTemplate(source, invocation, source.Value == null ? "" : source.Value);
                XmlNode attribute = Mutation.CreateXmlTreeNodeOperation(element.OwnerDocument == null ? throw new InvalidOperationException("A result element has no owner document.") : element.OwnerDocument, XmlNodeKind.Attribute, name, value);
                EnsureXsltNoDuplicateAttribute(element, attribute, source);
                ContentMutation.SetXmlTreeAttributeOperation(element, attribute);
            }

            index = index + 1;
        }
    }

    internal XdmSequence ConstructXsltComputedAttribute(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A computed attribute has no instruction plan.") : item.Instruction;
        string lexicalName = EvaluateXsltAttributeTemplate(FindXsltAttribute(instruction, "name"), invocation, "");
        XsltInstructionAttributePlan? namespaceAttribute = FindXsltAttribute(instruction, "namespace");
        string? namespaceUri = namespaceAttribute == null ? null : EvaluateXsltAttributeTemplate(namespaceAttribute, invocation, "");
        XmlName name = ResolveXsltComputedName(lexicalName, namespaceUri, instruction, true);
        string value = EvaluateXsltInstructionStringValue(item, invocation, " ");
        XmlDocument document = CreateXsltResultDocument(instruction.StaticContext.BaseUri);
        XmlNode attribute = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Attribute, name, value);
        return Sequences.CreateXdmNodeSequence(attribute);
    }

    internal XdmSequence ConstructXsltNamespace(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A namespace constructor has no instruction plan.") : item.Instruction;
        string prefix = EvaluateXsltAttributeTemplate(FindXsltAttribute(instruction, "name"), invocation, "").Trim();
        if (prefix.Length != 0 && !new XdmStringDatatype().IsValidXdmName(prefix, true) || prefix.IndexOf(':') >= 0 || prefix == "xmlns")
        {
            throw CreateXsltConstructionError("A computed namespace prefix must be an NCName or empty.", XsltErrorCodes.XTDE0920, instruction.Instruction);
        }

        string uri = EvaluateXsltInstructionStringValue(item, invocation, " ");
        ValidateXsltNamespaceBinding(prefix, uri, instruction.Instruction);
        XmlDocument document = CreateXsltResultDocument(instruction.StaticContext.BaseUri);
        XmlName name = new XmlName();
        name.LocalName = prefix;
        name.QualifiedName = prefix;
        name.NamespaceUri = "";
        XmlNode result = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Namespace, name, uri);
        return Sequences.CreateXdmNodeSequence(result);
    }

    internal XdmSequence ConstructXsltTextInstruction(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        string value = "";
        if (item.Content != null)
        {
            XdmSequence content = EvaluateXsltSequenceConstructor(item.Content, invocation);
            value = ConvertXsltSequenceToString(content, "");
        }

        return CreateXsltTextNodeSequence(value, item.Node);
    }

    internal XdmSequence ConstructXsltComment(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        string value = EvaluateXsltInstructionStringValue(item, invocation, " ");
        if (value.IndexOf("--", StringComparison.Ordinal) >= 0 || value.EndsWith("-", StringComparison.Ordinal))
        {
            throw CreateXsltConstructionError("A comment value must not contain '--' or end with '-'.", XsltErrorCodes.XTDE0980, item.Node);
        }

        XmlDocument document = CreateXsltResultDocument(item.Instruction == null ? null : item.Instruction.StaticContext.BaseUri);
        XmlNode comment = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Comment, null, value);
        return Sequences.CreateXdmNodeSequence(comment);
    }

    internal XdmSequence ConstructXsltProcessingInstruction(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A processing-instruction constructor has no plan.") : item.Instruction;
        string target = EvaluateXsltAttributeTemplate(FindXsltAttribute(instruction, "name"), invocation, "").Trim();
        if (!new XdmStringDatatype().IsValidXdmName(target, true) || target.IndexOf(':') >= 0 || string.Equals(target, "xml", StringComparison.OrdinalIgnoreCase))
        {
            throw CreateXsltConstructionError("A processing-instruction target must be an NCName other than xml.", XsltErrorCodes.XTDE0890, instruction.Instruction);
        }

        string value = EvaluateXsltInstructionStringValue(item, invocation, " ");
        if (value.IndexOf("?>", StringComparison.Ordinal) >= 0)
        {
            throw CreateXsltConstructionError("Processing-instruction content must not contain '?>'.", XsltErrorCodes.XTDE0905, instruction.Instruction);
        }

        XmlName name = new XmlName();
        name.LocalName = target;
        name.QualifiedName = target;
        name.NamespaceUri = "";
        XmlDocument document = CreateXsltResultDocument(instruction.StaticContext.BaseUri);
        XmlNode result = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.ProcessingInstruction, name, value);
        return Sequences.CreateXdmNodeSequence(result);
    }

    internal string EvaluateXsltInstructionStringValue(XsltSequenceItemPlan item, XsltTemplateInvocation invocation, string separator)
    {
        XsltInstructionAttributePlan? select = FindXsltAttribute(item.Instruction, "select");
        XdmSequence value;
        if (select != null && select.Expression != null && invocation.Frame != null)
        {
            value = Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        }
        else
        {
            value = item.Content == null ? Sequences.CreateEmptyXdmSequence() : EvaluateXsltSequenceConstructor(item.Content, invocation);
        }

        return ConvertXsltSequenceToString(value, separator);
    }

    internal string ConvertXsltSequenceToString(XdmSequence value, string separator)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(value);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < atomized.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, index);
            if (item.AtomicValue != null)
            {
                if (index > 0)
                {
                    result.Append(separator);
                }

                result.Append(Converter.ReadXdmAtomicStringValue(item.AtomicValue));
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string EvaluateXsltTextItem(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        return item.TextTemplate == null ? item.Node.Value == null ? "" : item.Node.Value : EvaluateXsltValueTemplate(item.TextTemplate, invocation);
    }

    internal string EvaluateXsltAttributeTemplate(XsltInstructionAttributePlan? attribute, XsltTemplateInvocation invocation, string fallback)
    {
        if (attribute == null)
        {
            return fallback;
        }

        XsltValueTemplate? template = FindXsltValueTemplate(attribute.Attribute);
        return template == null ? attribute.Source : EvaluateXsltValueTemplate(template, invocation);
    }

    internal string EvaluateXsltNodeValueTemplate(XmlNode node, XsltTemplateInvocation invocation, string fallback)
    {
        XsltValueTemplate? template = FindXsltValueTemplate(node);
        return template == null ? fallback : EvaluateXsltValueTemplate(template, invocation);
    }

    internal string EvaluateXsltValueTemplate(XsltValueTemplate template, XsltTemplateInvocation invocation)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < template.Parts.Length)
        {
            XsltValueTemplatePart part = template.Parts[index];
            if (part.Kind == XsltValueTemplatePartKind.Fixed)
            {
                result.Append(part.Value);
            }
            else if (part.ExpressionPlan != null && invocation.Frame != null)
            {
                XdmSequence value = Templates.EvaluateXsltInstructionExpression(part.ExpressionPlan, invocation.Frame);
                bool firstOnly = template.Kind == XsltValueTemplateKind.Attribute && template.StaticContext != null && template.StaticContext.XPathCompatibilityMode == XPathCompatibilityMode.BackwardsCompatible;
                result.Append(firstOnly ? ConvertXsltFirstItemToString(value) : ConvertXsltSequenceToString(value, " "));
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string ConvertXsltFirstItemToString(XdmSequence value)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(value);
        if (atomized.Count == 0)
        {
            return "";
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        return item.AtomicValue == null ? "" : Converter.ReadXdmAtomicStringValue(item.AtomicValue);
    }

    internal XdmSequence CreateXsltTextNodeSequence(string value, XmlNode source)
    {
        XmlDocument document = CreateXsltResultDocument(source.OwnerDocument == null ? null : source.OwnerDocument.BaseUri);
        XmlNode text = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Text, null, value);
        text.Range = source.Range;
        return Sequences.CreateXdmNodeSequence(text);
    }

    internal XmlDocument CreateXsltResultDocument(string? baseUri)
    {
        XmlDocument result = new XmlDocument();
        result.Version = XmlVersion.Xml10;
        result.BaseUri = baseUri;
        result.DocumentUri = baseUri;
        result.IsReadOnly = false;
        return result;
    }

    internal void AppendXsltConstructedContent(XmlNode parent, XdmSequence content, bool allowAttributes)
    {
        XsltConstructionContentState construction = new XsltConstructionContentState(parent);
        int index = 0;
        while (index < content.Count)
        {
            AppendXsltConstructedItem(construction, Sequences.ReadXdmSequenceItem(content, index), allowAttributes);
            index = index + 1;
        }

        FlushXsltConstructedText(construction);
    }

    internal void AppendXsltConstructedItem(XsltConstructionContentState construction, XdmSequenceItem item, bool allowAttributes)
    {
        if (item.AtomicValue != null)
        {
            if (construction.PreviousAtomic)
            {
                construction.PendingText.Append(' ');
            }

            construction.PendingText.Append(Converter.ReadXdmAtomicStringValue(item.AtomicValue));
            construction.PreviousAtomic = true;
            return;
        }

        XmlNode? node = item.Node;
        if (node == null)
        {
            throw CreateXsltConstructionError("A function item cannot be added directly to constructed node content.", XsltErrorCodes.XTDE0450, construction.Parent);
        }

        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            construction.PendingText.Append(node.Value == null ? "" : node.Value);
            construction.PreviousAtomic = false;
            return;
        }

        if (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment)
        {
            XmlNode[] children = node.Children == null ? [] : node.Children;
            int index = 0;
            while (index < children.Length)
            {
                AppendXsltConstructedItem(construction, CreateXsltNodeItem(children[index]), allowAttributes);
                index = index + 1;
            }

            return;
        }

        if (node.Kind == XmlNodeKind.Attribute)
        {
            if (!allowAttributes || construction.HasChild || construction.PendingText.Length != 0)
            {
                string code = allowAttributes ? XsltErrorCodes.XTDE0410 : XsltErrorCodes.XTDE0420;
                throw CreateXsltConstructionError("An attribute node must be created before child content.", code, node);
            }

            XmlDocument document = construction.Parent.OwnerDocument == null ? throw new InvalidOperationException("A constructed parent has no owner document.") : construction.Parent.OwnerDocument;
            XmlNode copy = Mutation.ImportXmlTreeNodeOperation(document, node, true);
            EnsureXsltNoDuplicateAttribute(construction.Parent, copy, node);
            EnsureXsltAttributeNameBinding(construction.Parent, copy, node);
            ContentMutation.SetXmlTreeAttributeOperation(construction.Parent, copy);
            construction.PreviousAtomic = false;
            return;
        }

        if (node.Kind == XmlNodeKind.Namespace)
        {
            if (!allowAttributes || construction.HasChild || construction.PendingText.Length != 0)
            {
                string code = allowAttributes ? XsltErrorCodes.XTDE0410 : XsltErrorCodes.XTDE0420;
                throw CreateXsltConstructionError("A namespace node must be created before child content.", code, node);
            }

            string prefix = node.Name == null || node.Name.LocalName == null ? "" : node.Name.LocalName;
            string uri = node.Value == null ? "" : node.Value;
            AddXsltElementNamespace(construction.Parent, prefix, uri, node);
            construction.PreviousAtomic = false;
            return;
        }

        FlushXsltConstructedText(construction);
        XmlDocument target = construction.Parent.OwnerDocument == null ? throw new InvalidOperationException("A constructed parent has no owner document.") : construction.Parent.OwnerDocument;
        XmlNode imported = Mutation.ImportXmlTreeNodeOperation(target, node, true);
        Mutation.AppendXmlTreeChildOperation(construction.Parent, imported);
        construction.HasChild = true;
        construction.PreviousAtomic = false;
    }

    internal XdmSequenceItem CreateXsltNodeItem(XmlNode node)
    {
        return new XdmSequenceItem(XdmItemKind.Node, node, null, null, null, null);
    }

    internal void FlushXsltConstructedText(XsltConstructionContentState construction)
    {
        if (construction.PendingText.Length == 0)
        {
            construction.PreviousAtomic = false;
            return;
        }

        string value = construction.PendingText.ToString();
        XmlNode[] children = construction.Parent.Children == null ? [] : construction.Parent.Children;
        if (children.Length > 0 && children[children.Length - 1].Kind == XmlNodeKind.Text)
        {
            XmlNode existing = children[children.Length - 1];
            ContentMutation.SetXmlTreeTextOperation(existing, (existing.Value == null ? "" : existing.Value) + value);
        }
        else
        {
            XmlDocument document = construction.Parent.OwnerDocument == null ? throw new InvalidOperationException("A constructed parent has no owner document.") : construction.Parent.OwnerDocument;
            XmlNode text = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Text, null, value);
            Mutation.AppendXmlTreeChildOperation(construction.Parent, text);
        }

        construction.PendingText.Clear();
        construction.HasChild = true;
        construction.PreviousAtomic = false;
    }

    internal XmlName ResolveXsltComputedName(string lexicalName, string? explicitNamespace, XsltInstructionPlan instruction, bool attribute)
    {
        string value = lexicalName.Trim();
        string prefix = "";
        string localName = "";
        string? namespaceUri = explicitNamespace;
        if (value.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = value.IndexOf('}');
            if (close >= 2)
            {
                namespaceUri = explicitNamespace == null ? value.Substring(2, close - 2) : explicitNamespace;
                localName = value.Substring(close + 1);
            }
        }
        else
        {
            int colon = value.IndexOf(':');
            if (colon < 0)
            {
                localName = value;
            }
            else if (colon == value.LastIndexOf(':'))
            {
                prefix = value.Substring(0, colon);
                localName = value.Substring(colon + 1);
            }
        }

        string code = attribute ? XsltErrorCodes.XTDE0850 : XsltErrorCodes.XTDE0820;
        if (!new XdmStringDatatype().IsValidXdmName(localName, true) || prefix.Length != 0 && !new XdmStringDatatype().IsValidXdmName(prefix, true))
        {
            throw CreateXsltConstructionError("A computed node name must be a valid EQName.", code, instruction.Instruction);
        }

        if (namespaceUri == null)
        {
            namespaceUri = prefix.Length == 0 ? attribute ? "" : FindXsltNamespaceUri(instruction.StaticContext.Namespaces, "") : FindXsltNamespaceUri(instruction.StaticContext.Namespaces, prefix);
        }

        if (namespaceUri == null)
        {
            throw CreateXsltConstructionError("A computed node name uses an unbound namespace prefix.", attribute ? XsltErrorCodes.XTDE0860 : XsltErrorCodes.XTDE0830, instruction.Instruction);
        }

        if (namespaceUri.Length != 0 && !Uri.IsWellFormedUriString(namespaceUri, UriKind.RelativeOrAbsolute))
        {
            throw CreateXsltConstructionError("A computed node namespace must be a URI reference.", attribute ? XsltErrorCodes.XTDE0855 : XsltErrorCodes.XTDE0830, instruction.Instruction);
        }

        if (explicitNamespace != null && explicitNamespace.Length == 0)
        {
            prefix = "";
        }

        if (attribute && namespaceUri.Length != 0 && prefix.Length == 0)
        {
            prefix = FindXsltPrefix(instruction.StaticContext.Namespaces, namespaceUri);
            if (prefix.Length == 0)
            {
                prefix = "ns0";
            }
        }

        ValidateXsltNamedNamespace(prefix, namespaceUri, attribute, instruction.Instruction);
        XmlName result = new XmlName();
        result.Prefix = prefix;
        result.LocalName = localName;
        result.NamespaceUri = namespaceUri;
        result.QualifiedName = prefix.Length == 0 ? localName : prefix + ":" + localName;
        return result;
    }

    internal void ValidateXsltNamedNamespace(string prefix, string namespaceUri, bool attribute, XmlNode node)
    {
        if (prefix == "xmlns" || attribute && prefix.Length == 0 && namespaceUri == XmlNamespaces.Xmlns || namespaceUri == XmlNamespaces.Xmlns && prefix != "xmlns")
        {
            throw CreateXsltConstructionError("A computed name cannot create an xmlns name.", attribute ? XsltErrorCodes.XTDE0865 : XsltErrorCodes.XTDE0835, node);
        }

        if (prefix == "xml" && namespaceUri != XmlNamespaces.Xml || prefix != "xml" && namespaceUri == XmlNamespaces.Xml)
        {
            throw CreateXsltConstructionError("The xml prefix and namespace must be used together.", attribute ? XsltErrorCodes.XTDE0860 : XsltErrorCodes.XTDE0835, node);
        }
    }

    internal void ValidateXsltNamespaceBinding(string prefix, string uri, XmlNode node)
    {
        if (uri.Length != 0 && !Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
        {
            throw CreateXsltConstructionError("A namespace value must be a URI reference.", XsltErrorCodes.XTDE0930, node);
        }

        if (prefix.Length != 0 && uri.Length == 0)
        {
            throw CreateXsltConstructionError("A non-empty namespace prefix requires a namespace URI.", XsltErrorCodes.XTDE0925, node);
        }

        if (prefix == "xml" && uri != XmlNamespaces.Xml || prefix != "xml" && uri == XmlNamespaces.Xml || uri == XmlNamespaces.Xmlns)
        {
            throw CreateXsltConstructionError("The namespace binding uses a reserved prefix or URI.", XsltErrorCodes.XTDE0930, node);
        }
    }

    internal void EnsureXsltElementNameBinding(XmlNode element, XmlName name, XmlNode source)
    {
        string prefix = name.Prefix == null ? "" : name.Prefix;
        string uri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        if (prefix.Length != 0 || uri.Length != 0)
        {
            AddXsltElementNamespace(element, prefix, uri, source);
        }
    }

    internal void AddXsltElementNamespace(XmlNode element, string prefix, string uri, XmlNode source)
    {
        ValidateXsltNamespaceBinding(prefix, uri, source);
        XmlNamespaceBinding[] existing = element.NamespaceBindings == null ? [] : element.NamespaceBindings;
        int index = 0;
        while (index < existing.Length)
        {
            XmlNamespaceBinding existingBinding = existing[index];
            string? optionalPrefix = existingBinding.Prefix;
            string currentPrefix = optionalPrefix == null ? "" : optionalPrefix;
            if (currentPrefix == prefix)
            {
                string? optionalUri = existingBinding.NamespaceUri;
                string currentUri = optionalUri == null ? "" : optionalUri;
                if (currentUri != uri)
                {
                    throw CreateXsltConstructionError("Two namespace nodes use the same prefix with different URIs.", XsltErrorCodes.XTDE0430, source);
                }

                return;
            }

            index = index + 1;
        }

        XmlNamespaceBinding[] result = new XmlNamespaceBinding[existing.Length + 1];
        global::System.Array.Copy(existing, result, existing.Length);
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = uri;
        result[existing.Length] = binding;
        element.NamespaceBindings = result;
    }

    internal void EnsureXsltNoDuplicateAttribute(XmlNode element, XmlNode attribute, XmlNode source)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? optionalLeftName = attributes[index].Name;
            XmlName? optionalRightName = attribute.Name;
            string? optionalLeftNamespace = optionalLeftName == null ? null : optionalLeftName.NamespaceUri;
            string? optionalRightNamespace = optionalRightName == null ? null : optionalRightName.NamespaceUri;
            string? optionalLeftLocal = optionalLeftName == null ? null : optionalLeftName.LocalName;
            string? optionalRightLocal = optionalRightName == null ? null : optionalRightName.LocalName;
            string leftNamespace = optionalLeftNamespace == null ? "" : optionalLeftNamespace;
            string rightNamespace = optionalRightNamespace == null ? "" : optionalRightNamespace;
            string leftLocal = optionalLeftLocal == null ? "" : optionalLeftLocal;
            string rightLocal = optionalRightLocal == null ? "" : optionalRightLocal;
            if (leftNamespace == rightNamespace && leftLocal == rightLocal)
            {
                throw CreateXsltConstructionError("A constructed element cannot contain duplicate attributes.", XsltErrorCodes.XTDE0410, source);
            }

            index = index + 1;
        }
    }

    internal void CopyXsltLiteralNamespaces(XmlNode target, XmlNode source, XsltStaticContext? context)
    {
        XmlNamespaceBinding[] bindings = source.NamespaceBindings == null ? [] : source.NamespaceBindings;
        int index = 0;
        while (index < bindings.Length)
        {
            XmlNamespaceBinding binding = bindings[index];
            string? optionalPrefix = binding.Prefix;
            string? optionalUri = binding.NamespaceUri;
            string prefix = optionalPrefix == null ? "" : optionalPrefix;
            string uri = optionalUri == null ? "" : optionalUri;
            if (uri != XsltNamespaces.Xslt && !IsXsltExcludedNamespace(context, uri))
            {
                AddXsltElementNamespace(target, prefix, uri, source);
            }

            index = index + 1;
        }
    }

    internal bool IsXsltExcludedNamespace(XsltStaticContext? context, string uri)
    {
        if (context == null)
        {
            return false;
        }

        int index = 0;
        while (index < context.ExcludedNamespaceUris.Length)
        {
            if (context.ExcludedNamespaceUris[index] == uri)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XmlName ApplyXsltNamespaceAlias(XmlName source)
    {
        string sourceUri = source.NamespaceUri == null ? "" : source.NamespaceUri;
        int index = 0;
        while (index < State.Stylesheet.NamespaceAliases.Length)
        {
            XsltNamespaceAlias alias = State.Stylesheet.NamespaceAliases[index];
            if (alias.LiteralNamespaceUri == sourceUri)
            {
                XmlName aliased = new XmlName();
                aliased.Prefix = alias.ResultPrefix;
                aliased.LocalName = source.LocalName;
                aliased.NamespaceUri = alias.TargetNamespaceUri;
                aliased.QualifiedName = alias.ResultPrefix.Length == 0 ? source.LocalName : alias.ResultPrefix + ":" + source.LocalName;
                return aliased;
            }

            index = index + 1;
        }

        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = source.NamespaceUri;
        result.QualifiedName = source.QualifiedName;
        return result;
    }

    internal string? FindXsltNamespaceUri(XmlNamespaceBinding[] namespaces, string prefix)
    {
        int index = namespaces.Length - 1;
        while (index >= 0)
        {
            XmlNamespaceBinding binding = namespaces[index];
            string? optionalPrefix = binding.Prefix;
            string current = optionalPrefix == null ? "" : optionalPrefix;
            if (current == prefix && !binding.IsUndeclaration)
            {
                string? optionalUri = binding.NamespaceUri;
                return optionalUri == null ? "" : optionalUri;
            }

            index = index - 1;
        }

        return prefix.Length == 0 ? "" : null;
    }

    internal string FindXsltPrefix(XmlNamespaceBinding[] namespaces, string uri)
    {
        int index = namespaces.Length - 1;
        while (index >= 0)
        {
            XmlNamespaceBinding binding = namespaces[index];
            string? optionalPrefix = binding.Prefix;
            string prefix = optionalPrefix == null ? "" : optionalPrefix;
            if (prefix.Length != 0 && binding.NamespaceUri == uri && !binding.IsUndeclaration)
            {
                return prefix;
            }

            index = index - 1;
        }

        return "";
    }

    internal XsltInstructionAttributePlan? FindXsltAttribute(XsltInstructionPlan? instruction, string name)
    {
        if (instruction == null)
        {
            return null;
        }

        int index = 0;
        while (index < instruction.Attributes.Length)
        {
            if (instruction.Attributes[index].Name == name)
            {
                return instruction.Attributes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltVariablePlan? FindXsltVariablePlan(XmlNode node)
    {
        int index = 0;
        while (index < State.Stylesheet.Variables.Length)
        {
            if (object.ReferenceEquals(State.Stylesheet.Variables[index].Declaration, node))
            {
                return State.Stylesheet.Variables[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltInstructionPlan? FindXsltInstructionPlan(XmlNode node)
    {
        int index = 0;
        while (index < State.Stylesheet.Instructions.Length)
        {
            if (object.ReferenceEquals(State.Stylesheet.Instructions[index].Instruction, node))
            {
                return State.Stylesheet.Instructions[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltValueTemplate? FindXsltValueTemplate(XmlNode node)
    {
        int index = 0;
        while (index < State.Stylesheet.ValueTemplates.Length)
        {
            if (object.ReferenceEquals(State.Stylesheet.ValueTemplates[index].Node, node))
            {
                return State.Stylesheet.ValueTemplates[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltValueTemplate[] FindXsltValueTemplates(XmlNode parent)
    {
        List<XsltValueTemplate> result = new List<XsltValueTemplate>();
        int index = 0;
        while (index < State.Stylesheet.ValueTemplates.Length)
        {
            XsltValueTemplate template = State.Stylesheet.ValueTemplates[index];
            if (template.Kind == XsltValueTemplateKind.Attribute && object.ReferenceEquals(template.Node.Parent, parent))
            {
                result.Add(template);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltStaticContext? FindXsltStaticContext(XmlNode node)
    {
        int index = 0;
        while (index < State.Stylesheet.StaticContexts.Length)
        {
            if (object.ReferenceEquals(State.Stylesheet.StaticContexts[index].Node, node))
            {
                return State.Stylesheet.StaticContexts[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltDynamicException CreateXsltConstructionError(string message, string code, XmlNode? node)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node == null ? null : node.Range;
        return new XsltDynamicException(message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
    }

    internal XsltTypeException CreateXsltConstructionTypeError(string message, string code, XmlNode? node)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node == null ? null : node.Range;
        return new XsltTypeException(message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
    }
}
