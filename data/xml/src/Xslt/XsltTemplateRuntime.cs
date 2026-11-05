using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal enum XsltTemplateDispatchKind
{
    Named = 0,
    Rule = 1,
    BuiltIn = 2
}

internal class XsltTemplateInvocation
{
    internal XsltTemplateDispatchKind Kind;
    internal XsltTemplatePlan? Template;
    internal XsltTemplateRulePlan? Rule;
    internal string Mode;
    internal XdmSequence ContextItem;
    internal XsltVariableFrame? Frame;
    internal int Position;
    internal int Size;
    internal int Depth;

    internal XsltTemplateInvocation(XsltTemplateDispatchKind kind, XsltTemplatePlan? template, XsltTemplateRulePlan? rule, string mode, XdmSequence contextItem, int position, int size, int depth)
    {
        Kind = kind;
        Template = template;
        Rule = rule;
        Mode = mode;
        ContextItem = contextItem;
        Frame = null;
        Position = position;
        Size = size;
        Depth = depth;
    }
}

internal class XsltGlobalVariableResolver : IXPathVariableResolver
{
    internal XsltVariableRuntime Runtime;

    internal XsltGlobalVariableResolver(XsltVariableRuntime runtime)
    {
        Runtime = runtime;
    }

    public XPathVariableBinding? ResolveXPathVariable(XmlName name, XPathDynamicContext context)
    {
        XsltVariableEvaluation? evaluation = Runtime.ResolveXsltGlobalEvaluation(name, context.XsltCurrentComponent);
        if (evaluation == null)
        {
            return null;
        }

        XPathVariableBinding result = new XPathVariableBinding();
        result.Name = name;
        result.Value = Runtime.EvaluateXsltGlobalSlot(evaluation);
        result.DeclaredType = evaluation.Variable.Binding.DeclaredType;
        result.DeclaredOccurrence = evaluation.Variable.Binding.DeclaredOccurrence;
        result.IsExternal = true;
        result.IsRequired = true;
        return result;
    }
}

internal class XsltTemplateRuntime
{
    internal XsltTransformationState State;
    internal XsltVariableRuntime Variables;
    internal XsltSequenceConstructorRuntime SequenceRuntime;
    internal List<XsltTemplateInvocation> Invocations;
    internal List<XmlDiagnostic> Diagnostics;
    internal int Depth;

    internal XsltTemplateRuntime(XsltTransformationState state)
    {
        State = state;
        Variables = state.VariableRuntime == null ? throw new InvalidOperationException("The variable runtime must be initialized before the template runtime.") : state.VariableRuntime;
        SequenceRuntime = new XsltSequenceConstructorRuntime(this);
        Invocations = new List<XsltTemplateInvocation>();
        Diagnostics = new List<XmlDiagnostic>();
        Depth = 0;
    }

    internal XdmSequence ExecuteXsltInitialTemplateInvocation()
    {
        Task<XdmSequence> pending = Task.Run(() => ExecuteXsltCompletedInitialInvocation());
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return pending.Result;
        }

        Exception failure = ReadXsltInvocationFailure(pending.Exception);
        if (State.ResultDocumentRuntime != null)
        {
            State.ResultDocumentRuntime.CompleteXsltResultDocuments(false);
        }

        State.Result.Succeeded = false;
        AttachXsltErrorContext(failure, State.InitialTemplate == null ? State.InitialFunction == null ? null : State.InitialFunction.Declaration : State.InitialTemplate.Declaration);
        Depth = 0;
        throw failure;
    }

    internal XdmSequence ExecuteXsltCompletedInitialInvocation()
    {
        XdmSequence result = ExecuteXsltInitialInvocationCore();
        new XsltTransformationEngine().CompleteXsltPrincipalResult(State, result);
        if (State.ResultDocumentRuntime != null)
        {
            State.ResultDocumentRuntime.CompleteXsltResultDocuments(true);
        }

        return result;
    }

    internal XdmSequence ExecuteXsltInitialInvocationCore()
    {
        if (State.InvocationKind == XsltInvocationKind.NamedTemplate && State.InitialTemplate != null)
        {
            return InvokeXsltNamedTemplatePlan(State.InitialTemplate, State.InitialContextItem, State.InitialTemplateParameters, null, State.InitialModeName == null ? State.Stylesheet.InitialMode : State.InitialModeName, State.InitialContextItem.Count == 0 ? 0 : 1, State.InitialContextItem.Count == 0 ? 0 : 1);
        }

        if (State.InvocationKind == XsltInvocationKind.ApplyTemplates)
        {
            string mode = State.InitialModeName == null ? State.Stylesheet.InitialMode : State.InitialModeName;
            return ApplyXsltTemplateSelection(State.InitialMatchSelection, mode, null);
        }

        if (State.InvocationKind == XsltInvocationKind.Function && State.InitialFunction != null && State.FunctionRuntime != null)
        {
            return State.FunctionRuntime.InvokeXsltFunctionPlan(State.InitialFunction, State.FunctionParameters, State.DynamicContext);
        }

        return new XdmSequenceOperations().CreateEmptyXdmSequence();
    }

    internal XdmSequence ApplyXsltTemplateSelection(XdmSequence selection, string mode, XsltVariableFrame? caller)
    {
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        sequences.ValidateXdmSequenceStorage(selection);
        XsltModePlan? tracedMode = FindXsltRuntimeMode(mode);
        RecordXsltRuntimeTrace(XsltTraceEventKind.EnterMode, tracedMode == null ? null : tracedMode.Component, null, mode, tracedMode == null ? null : tracedMode.Origin.Range, selection, Depth);
        XdmSequenceBuilder output = new XdmSequenceBuilder();
        XsltStreamingRuntime? streaming = State.StreamingRuntime;
        if (streaming != null)
        {
            streaming.EnterXsltStreamingSelection(mode);
        }

        int index = 0;
        while (index < selection.Count)
        {
            XdmSequence candidate = CreateXsltTemplateCandidate(selection, index);
            if (streaming != null)
            {
                streaming.BeginXsltStreamingCandidate(candidate, mode);
            }

            XPathDynamicContext context = CreateXsltTemplateFocusContext(caller == null ? State.DynamicContext : caller.Context, candidate, index + 1, selection.Count);
            XsltTemplateSelectionResult selected = new XsltTemplateSelector().SelectXsltTemplateRule(State.Stylesheet, mode, candidate, context);
            AddXsltSelectionDiagnostics(selected);
            XdmSequence candidateResult;
            if (selected.Template != null && selected.Rule != null)
            {
                candidateResult = InvokeXsltTemplateRule(selected.Template, selected.Rule, candidate, mode, caller == null ? State.InitialTemplateParameters : [], caller, index + 1, selection.Count);
            }
            else
            {
                candidateResult = InvokeXsltBuiltInTemplate(candidate, mode, caller, index + 1, selection.Count);
            }

            output.AppendXdmSequence(candidateResult);
            if (streaming != null)
            {
                streaming.EndXsltStreamingCandidate(candidate, mode);
                streaming.DeliverXsltStreamingCandidateResult(candidateResult, mode);
            }

            index = index + 1;
        }

        XdmSequence result = output.BuildImmutableXdmSequence();
        if (streaming != null)
        {
            streaming.LeaveXsltStreamingSelection(mode);
        }

        RecordXsltRuntimeTrace(XsltTraceEventKind.ExitMode, tracedMode == null ? null : tracedMode.Component, null, mode, tracedMode == null ? null : tracedMode.Origin.Range, selection, Depth);
        return result;
    }

    internal XdmSequence InvokeXsltNamedTemplate(XmlName name, XdmSequence contextItem, XsltParameter[] supplied, XsltVariableFrame? caller, string mode)
    {
        XsltTemplatePlan? template = FindXsltNamedTemplate(name);
        if (template == null)
        {
            throw CreateXsltTemplateDynamicError("The requested named template does not exist.", XsltErrorCodes.XTDE0040, null);
        }

        return InvokeXsltNamedTemplatePlan(template, contextItem, supplied, caller, mode, contextItem.Count == 0 ? 0 : 1, contextItem.Count == 0 ? 0 : 1);
    }

    internal XdmSequence InvokeXsltNamedTemplatePlan(XsltTemplatePlan template, XdmSequence contextItem, XsltParameter[] supplied, XsltVariableFrame? caller, string mode, int position, int size)
    {
        XdmSequence effectiveContext = ValidateXsltTemplateContext(template, contextItem);
        XPathDynamicContext dynamicContext = CreateXsltTemplateFocusContext(caller == null ? State.DynamicContext : caller.Context, effectiveContext, position, size);
        ClearXsltTemplateMergeContext(dynamicContext);
        dynamicContext.XsltCurrentComponent = template.Component;
        XsltVariableFrame frame = BindXsltInvocationParameters(template, supplied, caller, dynamicContext);
        XsltTemplateInvocation invocation = new XsltTemplateInvocation(XsltTemplateDispatchKind.Named, template, null, mode, effectiveContext, position, size, Depth);
        invocation.Frame = frame;
        Invocations.Add(invocation);
        RecordXsltRuntimeTrace(XsltTraceEventKind.EnterComponent, template.Component, null, template.Name, template.Declaration.Range, effectiveContext, Depth);
        RecordXsltRuntimeTrace(XsltTraceEventKind.EnterTemplate, template.Component, null, template.Name, template.Declaration.Range, effectiveContext, Depth);
        Depth = Depth + 1;
        XdmSequence result = ExecuteXsltTemplateBody(template.Body, invocation);
        Depth = Depth - 1;
        RecordXsltRuntimeTrace(XsltTraceEventKind.ExitTemplate, template.Component, null, template.Name, template.Declaration.Range, effectiveContext, Depth);
        RecordXsltRuntimeTrace(XsltTraceEventKind.ExitComponent, template.Component, null, template.Name, template.Declaration.Range, effectiveContext, Depth);
        return result;
    }

    internal XdmSequence InvokeXsltTemplateRule(XsltTemplatePlan template, XsltTemplateRulePlan rule, XdmSequence contextItem, string mode, XsltParameter[] supplied, XsltVariableFrame? caller, int position, int size)
    {
        XdmSequence effectiveContext = ValidateXsltTemplateContext(template, contextItem);
        XPathDynamicContext dynamicContext = CreateXsltTemplateFocusContext(caller == null ? State.DynamicContext : caller.Context, effectiveContext, position, size);
        ClearXsltTemplateMergeContext(dynamicContext);
        dynamicContext.XsltCurrentComponent = template.Component;
        XsltVariableFrame frame = BindXsltInvocationParameters(template, supplied, caller, dynamicContext);
        XsltTemplateInvocation invocation = new XsltTemplateInvocation(XsltTemplateDispatchKind.Rule, template, rule, mode, effectiveContext, position, size, Depth);
        invocation.Frame = frame;
        Invocations.Add(invocation);
        RecordXsltRuntimeTrace(XsltTraceEventKind.EnterComponent, template.Component, null, template.Name == null ? template.Match : template.Name, template.Declaration.Range, effectiveContext, Depth);
        RecordXsltRuntimeTrace(XsltTraceEventKind.EnterTemplate, template.Component, null, template.Name == null ? template.Match : template.Name, template.Declaration.Range, effectiveContext, Depth);
        Depth = Depth + 1;
        XdmSequence result = ExecuteXsltTemplateBody(template.Body, invocation);
        Depth = Depth - 1;
        RecordXsltRuntimeTrace(XsltTraceEventKind.ExitTemplate, template.Component, null, template.Name == null ? template.Match : template.Name, template.Declaration.Range, effectiveContext, Depth);
        RecordXsltRuntimeTrace(XsltTraceEventKind.ExitComponent, template.Component, null, template.Name == null ? template.Match : template.Name, template.Declaration.Range, effectiveContext, Depth);
        return result;
    }

    internal void ClearXsltTemplateMergeContext(XPathDynamicContext context)
    {
        context.XsltCurrentMergeGroupAvailable = false;
        context.XsltCurrentMergeGroup = null;
        context.XsltCurrentMergeSourceNames = null;
        context.XsltCurrentMergeSourceGroups = null;
        context.XsltCurrentMergeKeyAvailable = false;
        context.XsltCurrentMergeKey = null;
    }

    internal Exception ReadXsltInvocationFailure(AggregateException? aggregate)
    {
        Exception failure = aggregate == null ? new InvalidOperationException("An XSLT invocation failed without an exception.") : aggregate;
        while (failure.GetType().FullName == "System.AggregateException")
        {
            AggregateException current = (AggregateException)failure;
            if (current.InnerExceptions.Count == 0)
            {
                break;
            }

            failure = current.InnerExceptions[0];
        }

        return failure;
    }

    internal void AttachXsltErrorContext(Exception failure, XmlNode? fallback)
    {
        XmlDiagnostic? diagnostic = ReadXsltExceptionDiagnostic(failure);
        if (diagnostic == null)
        {
            return;
        }

        if (diagnostic.Range == null && fallback != null)
        {
            diagnostic.Range = fallback.Range;
        }

        string[] stack = CreateXsltTemplateStack();
        string[] existing = diagnostic.TemplateStack == null ? [] : diagnostic.TemplateStack;
        if (stack.Length > existing.Length)
        {
            diagnostic.TemplateStack = stack;
        }

        string[] components = CreateXsltTemplateComponentStack();
        string[] existingComponents = diagnostic.ComponentStack == null ? [] : diagnostic.ComponentStack;
        if (components.Length > existingComponents.Length)
        {
            diagnostic.ComponentStack = components;
        }
    }

    internal XmlDiagnostic? ReadXsltExceptionDiagnostic(Exception failure)
    {
        string? optionalName = failure.GetType().FullName;
        string name = optionalName == null ? "" : optionalName;
        if (name == "xml.XsltDynamicException") return ((XsltDynamicException)failure).Diagnostic;
        if (name == "xml.XsltTypeException") return ((XsltTypeException)failure).Diagnostic;
        if (name == "xml.XsltTerminationException") return ((XsltTerminationException)failure).Diagnostic;
        if (name == "xml.XsltException") return ((XsltException)failure).Diagnostic;
        if (name == "xml.XPathDynamicException") return ((XPathDynamicException)failure).Diagnostic;
        if (name == "xml.XPathTypeException") return ((XPathTypeException)failure).Diagnostic;
        if (name == "xml.XPathException") return ((XPathException)failure).Diagnostic;
        return null;
    }

    internal string[] CreateXsltTemplateStack()
    {
        string[] result = new string[Depth];
        int depth = 0;
        while (depth < Depth)
        {
            int index = Invocations.Count - 1;
            while (index >= 0)
            {
                XsltTemplateInvocation invocation = Invocations[index];
                if (invocation.Depth == depth && invocation.Template != null)
                {
                    string? optionalName = invocation.Template.Name;
                    result[depth] = optionalName == null ? "match:" + (invocation.Template.Match == null ? "" : invocation.Template.Match) : optionalName;
                    break;
                }

                index = index - 1;
            }

            if (result[depth] == null)
            {
                result[depth] = "built-in";
            }

            depth = depth + 1;
        }

        return result;
    }

    internal string[] CreateXsltTemplateComponentStack()
    {
        string[] result = new string[Depth];
        int depth = 0;
        while (depth < Depth)
        {
            result[depth] = "template";
            int index = Invocations.Count - 1;
            while (index >= 0)
            {
                XsltTemplateInvocation invocation = Invocations[index];
                if (invocation.Depth == depth && invocation.Template != null)
                {
                    XsltComponent? component = invocation.Template.Component;
                    result[depth] = component == null ? invocation.Template.Name == null ? "match:" + (invocation.Template.Match == null ? "" : invocation.Template.Match) : invocation.Template.Name : component.SymbolicIdentifier;
                    break;
                }

                index = index - 1;
            }

            depth = depth + 1;
        }

        return result;
    }

    internal void RecordXsltRuntimeTrace(XsltTraceEventKind kind, XsltComponent? component, string? instructionName, string? message, SourceRange? range, XdmSequence? contextItem, int depth)
    {
        if (!State.Options.EnableTracing) return;
        XsltTraceEvent value = new XsltTraceEvent();
        value.Kind = kind;
        value.ComponentName = CreateXsltTraceComponentName(component);
        value.InstructionName = instructionName;
        value.Message = message;
        value.Range = range;
        value.Depth = depth;
        value.ContextItem = contextItem == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(contextItem);
        XsltTraceEvent[] existing = State.Result.TraceEvents == null ? [] : State.Result.TraceEvents;
        XsltTraceEvent[] result = new XsltTraceEvent[existing.Length + 1];
        System.Array.Copy(existing, result, existing.Length);
        result[existing.Length] = value;
        State.Result.TraceEvents = result;
        if (State.Options.TraceReceiver != null)
        {
            State.Options.TraceReceiver.ReceiveXsltTrace(value);
        }
    }

    internal XmlName? CreateXsltTraceComponentName(XsltComponent? component)
    {
        if (component == null) return null;
        string value = component.Name;
        XmlName result = new XmlName();
        if (value.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = value.IndexOf('}');
            if (close >= 2 && close < value.Length - 1)
            {
                result.NamespaceUri = value.Substring(2, close - 2);
                result.LocalName = value.Substring(close + 1);
                result.QualifiedName = value;
                return result;
            }
        }

        result.LocalName = value;
        result.QualifiedName = value;
        result.NamespaceUri = "";
        return result;
    }

    internal XsltVariableFrame BindXsltInvocationParameters(XsltTemplatePlan template, XsltParameter[] supplied, XsltVariableFrame? caller, XPathDynamicContext context)
    {
        XsltVariableFrame? tunnelParent = null;
        if (caller != null)
        {
            tunnelParent = new XsltVariableFrame(null, caller.TunnelParameters, context);
        }

        return Variables.BindXsltTemplateParameters(template.Parameters, supplied, tunnelParent, context);
    }

    internal XdmSequence ExecuteXsltTemplateBody(XsltSequenceConstructorPlan body, XsltTemplateInvocation invocation)
    {
        return SequenceRuntime.EvaluateXsltSequenceConstructor(body, invocation);
    }

    internal XdmSequence ExecuteXsltTemplateInstruction(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan? instruction = item.Instruction;
        if (instruction == null || invocation.Frame == null)
        {
            return new XdmSequenceOperations().CreateEmptyXdmSequence();
        }

        if (instruction.Kind == XsltInstructionKind.ApplyTemplates)
        {
            return ExecuteXsltApplyTemplates(item, instruction, invocation);
        }

        if (instruction.Kind == XsltInstructionKind.CallTemplate)
        {
            return ExecuteXsltCallTemplate(item, instruction, invocation);
        }

        if (instruction.Kind == XsltInstructionKind.NextMatch)
        {
            return ExecuteXsltRelativeTemplateCall(item, invocation, false);
        }

        if (instruction.Kind == XsltInstructionKind.ApplyImports)
        {
            return ExecuteXsltRelativeTemplateCall(item, invocation, true);
        }

        return new XdmSequenceOperations().CreateEmptyXdmSequence();
    }

    internal XdmSequence ExecuteXsltApplyTemplates(XsltSequenceItemPlan item, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XsltVariableFrame frame = invocation.Frame == null ? throw new InvalidOperationException("A template invocation has no variable frame.") : invocation.Frame;
        XsltInstructionAttributePlan? select = FindXsltInstructionAttribute(instruction, "select");
        XdmSequence selection = select == null || select.Expression == null ? SelectXsltDefaultChildren(invocation.ContextItem) : EvaluateXsltInstructionExpression(select.Expression, frame);
        selection = new XsltSortRuntime(this).SortXsltSequence(selection, item, invocation);
        XsltParameter[] parameters = EvaluateXsltWithParameters(item, frame);
        string mode = ResolveXsltInstructionMode(instruction, invocation.Mode);
        return ApplyXsltTemplateSelectionWithParameters(selection, mode, parameters, frame);
    }

    internal XdmSequence ApplyXsltTemplateSelectionWithParameters(XdmSequence selection, string mode, XsltParameter[] supplied, XsltVariableFrame caller)
    {
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        sequences.ValidateXdmSequenceStorage(selection);
        XdmSequenceBuilder output = new XdmSequenceBuilder();
        XsltStreamingRuntime? streaming = State.StreamingRuntime;
        if (streaming != null)
        {
            streaming.EnterXsltStreamingSelection(mode);
        }

        int index = 0;
        while (index < selection.Count)
        {
            XdmSequence candidate = CreateXsltTemplateCandidate(selection, index);
            if (streaming != null)
            {
                streaming.BeginXsltStreamingCandidate(candidate, mode);
            }

            XPathDynamicContext context = CreateXsltTemplateFocusContext(caller.Context, candidate, index + 1, selection.Count);
            XsltTemplateSelectionResult selected = new XsltTemplateSelector().SelectXsltTemplateRule(State.Stylesheet, mode, candidate, context);
            AddXsltSelectionDiagnostics(selected);
            XdmSequence candidateResult;
            if (selected.Template != null && selected.Rule != null)
            {
                candidateResult = InvokeXsltTemplateRule(selected.Template, selected.Rule, candidate, mode, supplied, caller, index + 1, selection.Count);
            }
            else
            {
                candidateResult = InvokeXsltBuiltInTemplate(candidate, mode, caller, index + 1, selection.Count);
            }

            output.AppendXdmSequence(candidateResult);
            if (streaming != null)
            {
                streaming.EndXsltStreamingCandidate(candidate, mode);
                streaming.DeliverXsltStreamingCandidateResult(candidateResult, mode);
            }

            index = index + 1;
        }

        if (streaming != null)
        {
            streaming.LeaveXsltStreamingSelection(mode);
        }

        return output.BuildImmutableXdmSequence();
    }

    internal XdmSequence ExecuteXsltCallTemplate(XsltSequenceItemPlan item, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? name = FindXsltInstructionAttribute(instruction, "name");
        if (name == null || name.ResolvedName == null || invocation.Frame == null)
        {
            throw CreateXsltTemplateDynamicError("A call-template instruction has no resolved template name.", XsltErrorCodes.XTDE0040, instruction.Instruction);
        }

        XsltParameter[] parameters = EvaluateXsltWithParameters(item, invocation.Frame);
        XsltTemplatePlan? referenced = FindXsltReferencedTemplate(name.ResolvedName, invocation);
        if (referenced != null)
        {
            return InvokeXsltNamedTemplatePlan(referenced, invocation.ContextItem, parameters, invocation.Frame, invocation.Mode, invocation.Position, invocation.Size);
        }

        if (name.ResolvedName.NamespaceUri == XsltNamespaces.Xslt && name.ResolvedName.LocalName == "original")
        {
            throw CreateXsltTemplateDynamicError("The original template component is unavailable.", XsltErrorCodes.XTDE0040, instruction.Instruction);
        }

        return InvokeXsltNamedTemplate(name.ResolvedName, invocation.ContextItem, parameters, invocation.Frame, invocation.Mode);
    }

    internal XsltTemplatePlan? FindXsltReferencedTemplate(XmlName name, XsltTemplateInvocation invocation)
    {
        XsltComponent? component = invocation.Template == null ? null : invocation.Template.Component;
        if (component == null && invocation.Frame != null)
        {
            component = invocation.Frame.Context.XsltCurrentComponent;
        }

        XsltSymbolicReference[] references = component == null ? [] : component.References;
        string expandedName = "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name.LocalName == null ? "" : name.LocalName);
        int index = 0;
        while (index < references.Length)
        {
            XsltSymbolicReference reference = references[index];
            if (reference.Kind == XsltComponentKind.Template && reference.Name == expandedName && reference.Target != null && reference.Target.Template != null)
            {
                return reference.Target.Template;
            }

            index = index + 1;
        }

        return null;
    }

    internal XdmSequence ExecuteXsltRelativeTemplateCall(XsltSequenceItemPlan item, XsltTemplateInvocation invocation, bool importsOnly)
    {
        if (invocation.Rule == null || invocation.Frame == null)
        {
            throw CreateXsltTemplateDynamicError("A relative template-rule instruction has no current template rule.", XsltErrorCodes.XTDE0560, item.Node);
        }

        XsltParameter[] parameters = EvaluateXsltWithParameters(item, invocation.Frame);
        XPathDynamicContext context = CreateXsltTemplateFocusContext(invocation.Frame.Context, invocation.ContextItem, invocation.Position, invocation.Size);
        XsltTemplateSelectionResult selected = new XsltTemplateSelector().SelectXsltTemplateRuleBelow(State.Stylesheet, invocation.Mode, invocation.ContextItem, context, invocation.Rule, importsOnly);
        AddXsltSelectionDiagnostics(selected);
        if (selected.Template != null && selected.Rule != null)
        {
            return InvokeXsltTemplateRule(selected.Template, selected.Rule, invocation.ContextItem, invocation.Mode, parameters, invocation.Frame, invocation.Position, invocation.Size);
        }

        return InvokeXsltBuiltInTemplate(invocation.ContextItem, invocation.Mode, invocation.Frame, invocation.Position, invocation.Size);
    }

    internal XsltParameter[] EvaluateXsltWithParameters(XsltSequenceItemPlan owner, XsltVariableFrame frame)
    {
        List<XsltParameter> result = new List<XsltParameter>();
        XsltSequenceConstructorPlan? content = owner.Content;
        XsltSequenceItemPlan[] items = content == null ? [] : content.Items;
        int index = 0;
        while (index < items.Length)
        {
            XsltInstructionPlan? instruction = items[index].Instruction;
            if (instruction != null && instruction.Kind == XsltInstructionKind.WithParam)
            {
                XsltInstructionAttributePlan? name = FindXsltInstructionAttribute(instruction, "name");
                if (name != null && name.ResolvedName != null)
                {
                    XsltParameter parameter = new XsltParameter();
                    parameter.Name = name.ResolvedName;
                    parameter.Value = EvaluateXsltWithParameterValue(items[index], instruction, frame);
                    parameter.IsTunnel = ReadXsltInstructionBoolean(instruction, "tunnel");
                    result.Add(parameter);
                }
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XdmSequence EvaluateXsltWithParameterValue(XsltSequenceItemPlan item, XsltInstructionPlan instruction, XsltVariableFrame frame)
    {
        XsltInstructionAttributePlan? select = FindXsltInstructionAttribute(instruction, "select");
        XdmSequence value;
        if (select != null && select.Expression != null)
        {
            value = EvaluateXsltInstructionExpression(select.Expression, frame);
        }
        else if (item.Content != null && item.Content.Items.Length != 0)
        {
            XdmSequence contextItem = frame.Context.ContextItem == null ? new XdmSequenceOperations().CreateEmptyXdmSequence() : frame.Context.ContextItem;
            XsltTemplateInvocation invocation = new XsltTemplateInvocation(XsltTemplateDispatchKind.Named, null, null, State.Stylesheet.InitialMode, contextItem, frame.Context.Position, frame.Context.Size, Depth);
            invocation.Frame = frame;
            value = SequenceRuntime.EvaluateXsltSequenceConstructor(item.Content, invocation);
        }
        else if (FindXsltInstructionAttribute(instruction, "as") != null)
        {
            value = new XdmSequenceOperations().CreateEmptyXdmSequence();
        }
        else
        {
            XdmAtomicValue empty = new XdmFactory().CreateXdmStringAtomicValue("", XdmAtomicType.String);
            value = new XdmSequenceOperations().CreateXdmAtomicSequence(empty);
        }

        XsltInstructionAttributePlan? type = FindXsltInstructionAttribute(instruction, "as");
        if (type == null || type.SequenceType == null)
        {
            return value;
        }

        Task<XdmSequence> pending = Task.Run(() => new XPathFunctionTypeConverter().ConvertXPathFunctionTypedValue(value, type.SequenceType, CreateXsltInstructionConversionContext(instruction)));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return pending.Result;
        }

        throw CreateXsltTemplateTypeError("The xsl:with-param value does not match its declared type.", XsltErrorCodes.XTTE0590, instruction.Instruction);
    }

    internal XPathStaticContext CreateXsltInstructionConversionContext(XsltInstructionPlan instruction)
    {
        XPathStaticContext context = new XPathStaticContext();
        context.LanguageVersion = XPathLanguageVersion.XPath31;
        context.CompatibilityMode = instruction.StaticContext.XPathCompatibilityMode;
        context.Namespaces = instruction.StaticContext.Namespaces;
        context.DefaultElementNamespace = instruction.StaticContext.XPathDefaultNamespace;
        context.DefaultFunctionNamespace = XPathNamespaces.Functions;
        context.BaseUri = instruction.StaticContext.BaseUri;
        context.DefaultCollation = instruction.StaticContext.DefaultCollation;
        context.Collations = [XPathNamespaces.CodepointCollation, XPathAsciiCaseInsensitiveCollation.Uri];
        context.SchemaSet = State.Stylesheet.SchemaSet;
        context.DecimalFormats = instruction.StaticContext.DecimalFormats;
        context.ResolverPolicy = State.DynamicContext.ResolverPolicy;
        context.SchemaAware = State.Executable.IsSchemaAware;
        context.AllowHigherOrderFunctions = true;
        return context;
    }

    internal XdmSequence EvaluateXsltInstructionExpression(XPathPlan plan, XsltVariableFrame frame)
    {
        XPathDynamicContext context = Variables.CreateXsltVariableDynamicContext(frame);
        context.XsltCurrentItem = frame.Context.ContextItem;
        XPathRuntime runtime = new XPathRuntime(plan, context);
        runtime.Resources = State.DocumentPool;
        return runtime.EvaluateXPathPlan();
    }

    internal XdmSequence InvokeXsltBuiltInTemplate(XdmSequence candidate, string mode, XsltVariableFrame? caller, int position, int size)
    {
        XsltModePlan? modePlan = FindXsltRuntimeMode(mode);
        XsltOnNoMatchBehavior behavior = modePlan == null ? XsltOnNoMatchBehavior.TextOnlyCopy : modePlan.OnNoMatch;
        XsltTemplateInvocation invocation = new XsltTemplateInvocation(XsltTemplateDispatchKind.BuiltIn, null, null, mode, candidate, position, size, Depth);
        Invocations.Add(invocation);
        if (modePlan != null && modePlan.WarningOnNoMatch)
        {
            Diagnostics.Add(CreateXsltTemplateDiagnostic("No explicit template rule matched the selected item.", XsltErrorCodes.XTDE0555, XmlDiagnosticSeverity.Warning, modePlan.Origin));
        }

        if (behavior == XsltOnNoMatchBehavior.Fail)
        {
            throw CreateXsltTemplateDynamicError("No template rule matched the selected item and the mode rejects fallback.", XsltErrorCodes.XTDE0555, modePlan == null ? null : modePlan.Origin);
        }

        if (behavior == XsltOnNoMatchBehavior.DeepSkip)
        {
            return new XdmSequenceOperations().CreateEmptyXdmSequence();
        }

        XdmSequenceItem item = new XdmSequenceOperations().ReadXdmSequenceItem(candidate, 0);
        if (item.Node == null)
        {
            return new XsltTransformationEngine().CopyXsltRuntimeSequence(candidate);
        }

        XdmNode node = new XdmNodeAccessor().WrapXmlNode(item.Node);
        bool container = node.Kind == XdmNodeKind.Document || node.Kind == XdmNodeKind.Element;
        if (behavior == XsltOnNoMatchBehavior.DeepCopy)
        {
            return CopyXsltBuiltInNode(item.Node, true);
        }

        if (container)
        {
            XdmSequence children = SelectXsltDefaultChildren(candidate);
            XdmSequence content = ApplyXsltTemplateSelection(children, mode, caller);
            return behavior == XsltOnNoMatchBehavior.ShallowCopy ? ConstructXsltBuiltInShallowCopy(item.Node, content) : content;
        }

        if (behavior == XsltOnNoMatchBehavior.ShallowCopy)
        {
            return CopyXsltBuiltInNode(item.Node, true);
        }

        if (behavior == XsltOnNoMatchBehavior.TextOnlyCopy && (node.Kind == XdmNodeKind.Text || node.Kind == XdmNodeKind.Attribute))
        {
            string value = new XdmNodeAccessor().GetXdmNodeStringValue(node);
            return new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String));
        }

        return new XdmSequenceOperations().CreateEmptyXdmSequence();
    }

    internal XdmSequence CopyXsltBuiltInNode(XmlNode source, bool deep)
    {
        XsltStreamedNodeRuntime? streamed = State.StreamedNodeRuntime;
        if (deep && streamed != null && streamed.IsXsltStreamedNode(source) && !streamed.ReserveXsltStreamGrounding(source, false))
        {
            throw CreateXsltTemplateDynamicError("The streamed node cannot be copied within the configured grounding limit.", XPathErrorCodes.XPDY0130, source);
        }

        XmlDocument document = SequenceRuntime.CreateXsltResultDocument(source.OwnerDocument == null ? null : source.OwnerDocument.BaseUri);
        XmlNode copy = new TreeMutation().ImportXmlTreeNodeOperation(document, source, deep);
        if (source.Kind == XmlNodeKind.Document || source.Kind == XmlNodeKind.DocumentFragment)
        {
            document.DocumentNode = copy;
        }

        return new XdmSequenceOperations().CreateXdmNodeSequence(copy);
    }

    internal XdmSequence ConstructXsltBuiltInShallowCopy(XmlNode source, XdmSequence content)
    {
        XdmSequence result = CopyXsltBuiltInNode(source, false);
        XdmSequenceItem item = new XdmSequenceOperations().ReadXdmSequenceItem(result, 0);
        XmlNode copy = item.Node == null ? throw new InvalidOperationException("A built-in node copy is unavailable.") : item.Node;
        SequenceRuntime.AppendXsltConstructedContent(copy, content, source.Kind == XmlNodeKind.Element);
        return result;
    }

    internal XdmSequence SelectXsltDefaultChildren(XdmSequence contextItem)
    {
        if (contextItem.Count != 1)
        {
            return new XdmSequenceOperations().CreateEmptyXdmSequence();
        }

        XdmSequenceItem item = new XdmSequenceOperations().ReadXdmSequenceItem(contextItem, 0);
        if (item.Node == null)
        {
            return new XdmSequenceOperations().CreateEmptyXdmSequence();
        }

        XdmNodeAccessor accessor = new XdmNodeAccessor();
        XsltStreamedNodeRuntime? streamed = State.StreamedNodeRuntime;
        if (streamed != null && streamed.IsXsltStreamedNode(item.Node))
        {
            XsltStreamNavigationStatus status = streamed.AuthorizeXsltStreamNavigation(item.Node, XPathAxis.Child);
            if (status != XsltStreamNavigationStatus.Allowed)
            {
                throw CreateXsltTemplateDynamicError("The streamed node children are no longer available for template dispatch.", XPathErrorCodes.XPDY0130, item.Node);
            }
        }

        XdmNode[] children = accessor.GetXdmNodeChildren(accessor.WrapXmlNode(item.Node));
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        XmlNode[] streamedChildren = new XmlNode[children.Length];
        int index = 0;
        while (index < children.Length)
        {
            result.AddXdmNode(children[index].Node);
            streamedChildren[index] = children[index].Node;
            index = index + 1;
        }

        if (streamed != null && streamed.IsXsltStreamedNode(item.Node))
        {
            streamed.RegisterXsltStreamNavigationResult(streamedChildren);
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence CreateXsltTemplateCandidate(XdmSequence selection, int index)
    {
        XdmSequenceItem item = new XdmSequenceOperations().ReadXdmSequenceItem(selection, index);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        if (item.Node != null) result.AddXdmNode(item.Node);
        else if (item.AtomicValue != null) result.AddXdmAtomicValue(item.AtomicValue);
        else if (item.Function != null) result.AddXdmFunction(item.Function);
        else if (item.Map != null) result.AddXdmMap(item.Map);
        else if (item.Array != null) result.AddXdmArray(item.Array);
        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence ValidateXsltTemplateContext(XsltTemplatePlan template, XdmSequence supplied)
    {
        XsltContextItemPlan? declaration = template.ContextItem;
        if (declaration != null && declaration.Use == XsltContextItemUse.Absent)
        {
            return new XdmSequenceOperations().CreateEmptyXdmSequence();
        }

        if (declaration != null && declaration.Use == XsltContextItemUse.Required && supplied.Count == 0)
        {
            throw CreateXsltTemplateTypeError("The invoked template requires a context item.", XsltErrorCodes.XTTE3090, declaration.Declaration);
        }

        if (declaration != null && declaration.Type != null && supplied.Count != 0)
        {
            XdmSequenceType expected = new XPathFunctionTypeConverter().CreateXPathFunctionSequenceType(declaration.Type);
            if (!new XdmTypeConverter().InstanceOfXdmSequence(supplied, expected))
            {
                throw CreateXsltTemplateTypeError("The context item does not match the template declaration.", XsltErrorCodes.XTTE3090, declaration.Declaration);
            }
        }

        return supplied;
    }

    internal XPathDynamicContext CreateXsltTemplateFocusContext(XPathDynamicContext basis, XdmSequence contextItem, int position, int size)
    {
        XPathDynamicContext result = new XPathFunctionContextFactory().CopyXPathFunctionDynamicComponents(basis);
        result.ContextItem = contextItem;
        result.Position = position;
        result.Size = size;
        return result;
    }

    internal string ResolveXsltInstructionMode(XsltInstructionPlan instruction, string current)
    {
        XsltInstructionAttributePlan? attribute = FindXsltInstructionAttribute(instruction, "mode");
        string token = attribute == null ? "#default" : attribute.Source.Trim();
        if (token == "#current") return current;
        if (token == "#default") return instruction.StaticContext.DefaultMode;
        if (token == "#unnamed") return "#unnamed";
        if (token.StartsWith("Q{", StringComparison.Ordinal)) return token;
        int colon = token.IndexOf(':');
        if (colon < 0) return "Q{}" + token;
        string prefix = token.Substring(0, colon);
        string localName = token.Substring(colon + 1);
        int index = 0;
        while (index < instruction.StaticContext.Namespaces.Length)
        {
            XmlNamespaceBinding binding = instruction.StaticContext.Namespaces[index];
            if (!binding.IsUndeclaration && binding.Prefix == prefix)
            {
                return "Q{" + (binding.NamespaceUri == null ? "" : binding.NamespaceUri) + "}" + localName;
            }

            index = index + 1;
        }

        return "Q{}" + localName;
    }

    internal XsltTemplatePlan? FindXsltNamedTemplate(XmlName name)
    {
        string expanded = "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name.LocalName == null ? "" : name.LocalName);
        XsltTemplatePlan? selected = null;
        int index = 0;
        while (index < State.Stylesheet.Templates.Length)
        {
            XsltTemplatePlan candidate = State.Stylesheet.Templates[index];
            if (candidate.IsNamed && candidate.Name == expanded && !candidate.IsAbstract && (selected == null || candidate.ImportPrecedence > selected.ImportPrecedence || candidate.ImportPrecedence == selected.ImportPrecedence && candidate.DeclarationOrder > selected.DeclarationOrder))
            {
                selected = candidate;
            }

            index = index + 1;
        }

        return selected;
    }

    internal XsltModePlan? FindXsltRuntimeMode(string mode)
    {
        int index = 0;
        while (index < State.Stylesheet.Modes.Length)
        {
            if (State.Stylesheet.Modes[index].Name == mode)
            {
                return State.Stylesheet.Modes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltInstructionAttributePlan? FindXsltInstructionAttribute(XsltInstructionPlan instruction, string name)
    {
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

    internal bool ReadXsltInstructionBoolean(XsltInstructionPlan instruction, string name)
    {
        XsltInstructionAttributePlan? attribute = FindXsltInstructionAttribute(instruction, name);
        if (attribute == null)
        {
            return false;
        }

        string value = attribute.Source.Trim();
        return value == "yes" || value == "true" || value == "1";
    }

    internal void AddXsltSelectionDiagnostics(XsltTemplateSelectionResult selection)
    {
        int index = 0;
        while (index < selection.Diagnostics.Length)
        {
            Diagnostics.Add(selection.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal XmlDiagnostic CreateXsltTemplateDiagnostic(string message, string code, XmlDiagnosticSeverity severity, XmlNode? node)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltDynamic;
        diagnostic.Severity = severity;
        diagnostic.Message = message;
        diagnostic.Range = node == null ? null : node.Range;
        return diagnostic;
    }

    internal XsltDynamicException CreateXsltTemplateDynamicError(string message, string code, XmlNode? node)
    {
        XmlDiagnostic diagnostic = CreateXsltTemplateDiagnostic(message, code, XmlDiagnosticSeverity.Fatal, node);
        return new XsltDynamicException(message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
    }

    internal XsltTypeException CreateXsltTemplateTypeError(string message, string code, XmlNode node)
    {
        XmlDiagnostic diagnostic = CreateXsltTemplateDiagnostic(message, code, XmlDiagnosticSeverity.Fatal, node);
        diagnostic.Category = XmlErrorCategory.XsltType;
        return new XsltTypeException(message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
    }
}
