using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal enum XsltAccumulatorEvaluationStatus
{
    Pending = 0,
    Building = 1,
    Ready = 2
}

internal class XsltAccumulatorValue
{
    internal XdmSequence? Value;
    internal Exception? Failure;

    internal XsltAccumulatorValue(XdmSequence? value, Exception? failure)
    {
        Value = value;
        Failure = failure;
    }
}

internal class XsltAccumulatorNodeValue
{
    internal XmlNode Node;
    internal XsltAccumulatorValue Before;
    internal XsltAccumulatorValue After;
    internal bool AfterReady;

    internal XsltAccumulatorNodeValue(XmlNode node, XsltAccumulatorValue before)
    {
        Node = node;
        Before = before;
        After = before;
        AfterReady = false;
    }
}

internal class XsltStreamingAccumulatorEvaluation
{
    internal XmlNode Root;
    internal XsltAccumulatorPlan Accumulator;
    internal XsltAccumulatorValue Current;
    internal List<XsltAccumulatorNodeValue> Nodes;

    internal XsltStreamingAccumulatorEvaluation(XmlNode root, XsltAccumulatorPlan accumulator, XsltAccumulatorValue current)
    {
        Root = root;
        Accumulator = accumulator;
        Current = current;
        Nodes = new List<XsltAccumulatorNodeValue>();
    }
}

internal class XsltTreeAccumulatorEvaluation
{
    internal XmlNode Root;
    internal XsltAccumulatorPlan Accumulator;
    internal long MutationVersion;
    internal XsltAccumulatorEvaluationStatus Status;
    internal List<XsltAccumulatorNodeValue> Nodes;

    internal XsltTreeAccumulatorEvaluation(XmlNode root, XsltAccumulatorPlan accumulator, long mutationVersion)
    {
        Root = root;
        Accumulator = accumulator;
        MutationVersion = mutationVersion;
        Status = XsltAccumulatorEvaluationStatus.Pending;
        Nodes = new List<XsltAccumulatorNodeValue>();
    }
}

internal class XsltAccumulatorRuntime
{
    internal XsltTransformationState State;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XsltPatternMatcher Patterns;
    internal List<XsltTreeAccumulatorEvaluation> Evaluations;
    internal List<XsltStreamingAccumulatorEvaluation> StreamingEvaluations;

    internal XsltAccumulatorRuntime(XsltTransformationState state)
    {
        State = state;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Patterns = new XsltPatternMatcher();
        Evaluations = new List<XsltTreeAccumulatorEvaluation>();
        StreamingEvaluations = new List<XsltStreamingAccumulatorEvaluation>();
    }

    internal XdmSequence EvaluateXsltAccumulator(XdmSequence nameArgument, XPathDynamicContext context, XmlNamespaceBinding[] namespaces, bool after)
    {
        string lexicalName = ReadXsltAccumulatorNameArgument(nameArgument);
        string? expandedName = ResolveXsltAccumulatorName(lexicalName, namespaces);
        XsltAccumulatorPlan? accumulator = expandedName == null ? null : FindXsltAccumulatorPlan(expandedName);
        if (accumulator == null)
        {
            throw CreateXsltAccumulatorDynamicError("The accumulator name is invalid or does not identify a declared accumulator.", XsltErrorCodes.XTDE3340);
        }

        XmlNode node = ReadXsltAccumulatorContextNode(context);
        XmlNode? source = FindXsltCopiedAccumulatorSource(node);
        if (source != null)
        {
            return EvaluateXsltAccumulatorNode(accumulator, source, after);
        }

        return EvaluateXsltAccumulatorNode(accumulator, node, after);
    }

    internal string ReadXsltAccumulatorNameArgument(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count != 1)
        {
            throw CreateXsltAccumulatorTypeError("The accumulator name must be a single string.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null || !new XdmMapOperations().IsXdmStringMapKey(value.Type))
        {
            throw CreateXsltAccumulatorTypeError("The accumulator name cannot be converted to a string.", XPathErrorCodes.XPTY0004);
        }

        return Converter.ReadXdmAtomicStringValue(value).Trim();
    }

    internal string? ResolveXsltAccumulatorName(string lexicalName, XmlNamespaceBinding[] namespaces)
    {
        string namespaceUri = "";
        string localName = lexicalName;
        if (lexicalName.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = lexicalName.IndexOf('}');
            if (close < 2 || close == lexicalName.Length - 1 || lexicalName.IndexOf('{', 2) >= 0)
            {
                return null;
            }

            namespaceUri = lexicalName.Substring(2, close - 2);
            localName = lexicalName.Substring(close + 1);
        }
        else
        {
            int colon = lexicalName.IndexOf(':');
            if (colon >= 0)
            {
                if (colon == 0 || colon == lexicalName.Length - 1 || colon != lexicalName.LastIndexOf(':'))
                {
                    return null;
                }

                string prefix = lexicalName.Substring(0, colon);
                localName = lexicalName.Substring(colon + 1);
                string? resolved = FindXsltAccumulatorNamespace(prefix, namespaces);
                if (resolved == null)
                {
                    return null;
                }

                namespaceUri = resolved;
            }
        }

        QualifiedNameParser names = new QualifiedNameParser(XmlNamespaceHandling.Namespaces10, null);
        return names.IsValidXmlNcName(localName) ? "Q{" + namespaceUri + "}" + localName : null;
    }

    internal string? FindXsltAccumulatorNamespace(string prefix, XmlNamespaceBinding[] namespaces)
    {
        int index = 0;
        while (index < namespaces.Length)
        {
            XmlNamespaceBinding binding = namespaces[index];
            if (!binding.IsUndeclaration && binding.Prefix == prefix)
            {
                return binding.NamespaceUri;
            }

            index = index + 1;
        }

        return prefix == "xml" ? XmlNamespaces.Xml : null;
    }

    internal XsltAccumulatorPlan? FindXsltAccumulatorPlan(string expandedName)
    {
        int index = 0;
        while (index < State.Stylesheet.Accumulators.Length)
        {
            if (State.Stylesheet.Accumulators[index].Name == expandedName)
            {
                return State.Stylesheet.Accumulators[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XmlNode ReadXsltAccumulatorContextNode(XPathDynamicContext context)
    {
        XdmSequence? supplied = context.ContextItem;
        if (supplied == null || supplied.Count == 0)
        {
            throw CreateXsltAccumulatorDynamicError("The accumulator function requires a context item.", XsltErrorCodes.XTDE3350);
        }

        if (supplied.Count != 1)
        {
            throw CreateXsltAccumulatorTypeError("The accumulator function context item must be a node.", XsltErrorCodes.XTTE3360);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Node == null || item.Node.Kind == XmlNodeKind.Attribute || item.Node.Kind == XmlNodeKind.Namespace)
        {
            throw CreateXsltAccumulatorTypeError("The accumulator function context item must be a non-attribute node.", XsltErrorCodes.XTTE3360);
        }

        return item.Node;
    }

    internal XmlNode? FindXsltCopiedAccumulatorSource(XmlNode node)
    {
        int index = State.AccumulatorCopies.Count - 1;
        while (index >= 0)
        {
            if (object.ReferenceEquals(State.AccumulatorCopies[index].Copy, node))
            {
                return State.AccumulatorCopies[index].Source;
            }

            index = index - 1;
        }

        return null;
    }

    internal XdmSequence EvaluateXsltAccumulatorNode(XsltAccumulatorPlan accumulator, XmlNode node, bool after)
    {
        XsltAccumulatorNodeValue? streamedValue = FindXsltStreamingAccumulatorNodeValue(accumulator, node);
        if (streamedValue != null)
        {
            if (after && !streamedValue.AfterReady)
            {
                throw CreateXsltAccumulatorDynamicError("The accumulator-after value is not available until the streamed node end event.", XPathErrorCodes.XPDY0130);
            }

            return ReadXsltAccumulatorStoredValue(after ? streamedValue.After : streamedValue.Before);
        }

        XmlNode root = FindXsltAccumulatorRoot(node);
        if (!IsXsltAccumulatorApplicable(accumulator, root))
        {
            throw CreateXsltAccumulatorDynamicError("The selected accumulator is not applicable to the context tree.", XsltErrorCodes.XTDE3362);
        }

        XsltTreeAccumulatorEvaluation evaluation = GetXsltTreeAccumulatorEvaluation(root, accumulator);
        XsltAccumulatorNodeValue? nodeValue = FindXsltAccumulatorNodeValue(evaluation, node);
        if (nodeValue == null)
        {
            throw new InvalidOperationException("An accumulator traversal did not record the context node.");
        }

        return ReadXsltAccumulatorStoredValue(after ? nodeValue.After : nodeValue.Before);
    }

    internal XdmSequence ReadXsltAccumulatorStoredValue(XsltAccumulatorValue selected)
    {
        Exception? failure = selected.Failure;
        if (failure != null)
        {
            throw failure;
        }

        XdmSequence? value = selected.Value;
        if (value == null)
        {
            throw new InvalidOperationException("A successful accumulator snapshot has no value.");
        }

        return new XsltTransformationEngine().CopyXsltRuntimeSequence(value);
    }

    internal void BeginXsltStreamingAccumulatorNode(XsltModePlan mode, XmlNode node)
    {
        int index = 0;
        while (index < mode.Accumulators.Length)
        {
            XsltAccumulatorPlan accumulator = mode.Accumulators[index];
            XmlNode root = FindXsltAccumulatorRoot(node);
            XsltStreamingAccumulatorEvaluation? evaluation = FindXsltStreamingAccumulatorEvaluation(root, accumulator);
            if (evaluation == null)
            {
                XsltAccumulatorValue initial = EvaluateXsltAccumulatorInitialValue(root, accumulator.EffectiveDeclaration);
                evaluation = new XsltStreamingAccumulatorEvaluation(root, accumulator, initial);
                StreamingEvaluations.Add(evaluation);
            }

            if (FindXsltStreamingNodeValue(evaluation, node) == null)
            {
                XsltAccumulatorRulePlan? start = SelectXsltAccumulatorRule(accumulator.EffectiveDeclaration, node, XsltAccumulatorPhase.Start);
                XsltAccumulatorValue current = start == null ? evaluation.Current : EvaluateXsltAccumulatorRuleValue(node, accumulator.EffectiveDeclaration, start, evaluation.Current);
                evaluation.Current = current;
                evaluation.Nodes.Add(new XsltAccumulatorNodeValue(node, current));
            }

            index = index + 1;
        }
    }

    internal void EndXsltStreamingAccumulatorNode(XsltModePlan mode, XmlNode node)
    {
        int index = 0;
        while (index < mode.Accumulators.Length)
        {
            XsltAccumulatorPlan accumulator = mode.Accumulators[index];
            XsltStreamingAccumulatorEvaluation? evaluation = FindXsltStreamingAccumulatorEvaluation(FindXsltAccumulatorRoot(node), accumulator);
            XsltAccumulatorNodeValue? value = evaluation == null ? null : FindXsltStreamingNodeValue(evaluation, node);
            if (evaluation != null && value != null && !value.AfterReady)
            {
                XsltAccumulatorRulePlan? end = SelectXsltAccumulatorRule(accumulator.EffectiveDeclaration, node, XsltAccumulatorPhase.End);
                XsltAccumulatorValue current = end == null ? evaluation.Current : EvaluateXsltAccumulatorRuleValue(node, accumulator.EffectiveDeclaration, end, evaluation.Current);
                evaluation.Current = current;
                value.After = current;
                value.AfterReady = true;
            }

            index = index + 1;
        }
    }

    internal XsltStreamingAccumulatorEvaluation? FindXsltStreamingAccumulatorEvaluation(XmlNode root, XsltAccumulatorPlan accumulator)
    {
        int index = 0;
        while (index < StreamingEvaluations.Count)
        {
            XsltStreamingAccumulatorEvaluation evaluation = StreamingEvaluations[index];
            if (object.ReferenceEquals(evaluation.Root, root) && object.ReferenceEquals(evaluation.Accumulator, accumulator))
            {
                return evaluation;
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltAccumulatorNodeValue? FindXsltStreamingAccumulatorNodeValue(XsltAccumulatorPlan accumulator, XmlNode node)
    {
        XsltStreamingAccumulatorEvaluation? evaluation = FindXsltStreamingAccumulatorEvaluation(FindXsltAccumulatorRoot(node), accumulator);
        return evaluation == null ? null : FindXsltStreamingNodeValue(evaluation, node);
    }

    internal XsltAccumulatorNodeValue? FindXsltStreamingNodeValue(XsltStreamingAccumulatorEvaluation evaluation, XmlNode node)
    {
        int index = evaluation.Nodes.Count - 1;
        while (index >= 0)
        {
            if (object.ReferenceEquals(evaluation.Nodes[index].Node, node))
            {
                return evaluation.Nodes[index];
            }

            index = index - 1;
        }

        return null;
    }

    internal XmlNode FindXsltAccumulatorRoot(XmlNode node)
    {
        XmlNode result = node;
        while (result.Parent != null)
        {
            result = result.Parent;
        }

        return result;
    }

    internal bool IsXsltAccumulatorApplicable(XsltAccumulatorPlan accumulator, XmlNode root)
    {
        if (State.InvocationKind != XsltInvocationKind.ApplyTemplates || !ContainsXsltInitialSelectionTree(root))
        {
            return true;
        }

        XsltModePlan? mode = State.InitialMode;
        if (mode == null)
        {
            return false;
        }

        int index = 0;
        while (index < mode.Accumulators.Length)
        {
            if (object.ReferenceEquals(mode.Accumulators[index], accumulator))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsXsltInitialSelectionTree(XmlNode root)
    {
        int index = 0;
        while (index < State.InitialMatchSelection.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(State.InitialMatchSelection, index);
            if (item.Node != null && object.ReferenceEquals(FindXsltAccumulatorRoot(item.Node), root))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltTreeAccumulatorEvaluation GetXsltTreeAccumulatorEvaluation(XmlNode root, XsltAccumulatorPlan accumulator)
    {
        long mutationVersion = ReadXsltAccumulatorMutationVersion(root);
        XsltTreeAccumulatorEvaluation? evaluation = FindXsltTreeAccumulatorEvaluation(root, accumulator);
        if (evaluation == null)
        {
            evaluation = new XsltTreeAccumulatorEvaluation(root, accumulator, mutationVersion);
            Evaluations.Add(evaluation);
        }
        else if (evaluation.MutationVersion != mutationVersion)
        {
            evaluation.MutationVersion = mutationVersion;
            evaluation.Status = XsltAccumulatorEvaluationStatus.Pending;
            evaluation.Nodes = new List<XsltAccumulatorNodeValue>();
        }

        if (evaluation.Status == XsltAccumulatorEvaluationStatus.Building)
        {
            throw CreateXsltAccumulatorDynamicError("Accumulator dependencies contain a circularity.", XsltErrorCodes.XTDE3400);
        }

        if (evaluation.Status == XsltAccumulatorEvaluationStatus.Pending)
        {
            BuildXsltTreeAccumulatorEvaluation(evaluation);
        }

        return evaluation;
    }

    internal long ReadXsltAccumulatorMutationVersion(XmlNode root)
    {
        return root.OwnerDocument == null ? root.MutationVersion : root.OwnerDocument.MutationVersion;
    }

    internal XsltTreeAccumulatorEvaluation? FindXsltTreeAccumulatorEvaluation(XmlNode root, XsltAccumulatorPlan accumulator)
    {
        int index = 0;
        while (index < Evaluations.Count)
        {
            XsltTreeAccumulatorEvaluation evaluation = Evaluations[index];
            if (object.ReferenceEquals(evaluation.Root, root) && object.ReferenceEquals(evaluation.Accumulator, accumulator))
            {
                return evaluation;
            }

            index = index + 1;
        }

        return null;
    }

    internal void BuildXsltTreeAccumulatorEvaluation(XsltTreeAccumulatorEvaluation evaluation)
    {
        evaluation.Status = XsltAccumulatorEvaluationStatus.Building;
        XsltAccumulatorValue initial = EvaluateXsltAccumulatorInitialValue(evaluation.Root, evaluation.Accumulator.EffectiveDeclaration);
        TraverseXsltAccumulatorNode(evaluation, evaluation.Root, initial);
        evaluation.Status = XsltAccumulatorEvaluationStatus.Ready;
    }

    internal XsltAccumulatorValue TraverseXsltAccumulatorNode(XsltTreeAccumulatorEvaluation evaluation, XmlNode node, XsltAccumulatorValue supplied)
    {
        XsltAccumulatorDeclarationPlan declaration = evaluation.Accumulator.EffectiveDeclaration;
        XsltAccumulatorRulePlan? start = SelectXsltAccumulatorRule(declaration, node, XsltAccumulatorPhase.Start);
        XsltAccumulatorValue current = start == null ? supplied : EvaluateXsltAccumulatorRuleValue(node, declaration, start, supplied);
        XsltAccumulatorNodeValue snapshot = new XsltAccumulatorNodeValue(node, current);
        evaluation.Nodes.Add(snapshot);
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            current = TraverseXsltAccumulatorNode(evaluation, children[index], current);
            index = index + 1;
        }

        XsltAccumulatorRulePlan? end = SelectXsltAccumulatorRule(declaration, node, XsltAccumulatorPhase.End);
        current = end == null ? current : EvaluateXsltAccumulatorRuleValue(node, declaration, end, current);
        snapshot.After = current;
        snapshot.AfterReady = true;
        return current;
    }

    internal XsltAccumulatorRulePlan? SelectXsltAccumulatorRule(XsltAccumulatorDeclarationPlan declaration, XmlNode node, XsltAccumulatorPhase phase)
    {
        XsltAccumulatorRulePlan? result = null;
        int index = 0;
        while (index < declaration.Rules.Length)
        {
            XsltAccumulatorRulePlan rule = declaration.Rules[index];
            if (rule.Phase == phase && MatchesXsltAccumulatorRule(rule, node))
            {
                result = rule;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool MatchesXsltAccumulatorRule(XsltAccumulatorRulePlan rule, XmlNode node)
    {
        XdmSequence candidate = Sequences.CreateXdmNodeSequence(node);
        XPathDynamicContext context = CreateXsltAccumulatorFocus(candidate);
        Task<bool> pending = Task.Run(() => Patterns.MatchesXsltPattern(rule.Match, candidate, context));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        return pending.Status == TaskStatus.RanToCompletion && pending.Result;
    }

    internal XsltAccumulatorValue EvaluateXsltAccumulatorInitialValue(XmlNode root, XsltAccumulatorDeclarationPlan declaration)
    {
        XdmSequence candidate = Sequences.CreateXdmNodeSequence(root);
        XPathDynamicContext context = CreateXsltAccumulatorFocus(candidate);
        XsltVariableFrame frame = new XsltVariableFrame(null, [], context);
        Task<XdmSequence> pending = Task.Run(() => EvaluateAndConvertXsltAccumulatorInitialValue(declaration, frame));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        return ReadXsltAccumulatorTaskValue(pending);
    }

    internal XdmSequence EvaluateAndConvertXsltAccumulatorInitialValue(XsltAccumulatorDeclarationPlan declaration, XsltVariableFrame frame)
    {
        if (State.TemplateRuntime == null)
        {
            throw new InvalidOperationException("The template runtime is unavailable during accumulator evaluation.");
        }

        XdmSequence value = State.TemplateRuntime.EvaluateXsltInstructionExpression(declaration.InitialValue, frame);
        XPathStaticContext context = new XPathFunctionContextFactory().CreateXPathFunctionStaticContext(declaration.InitialValue.Environment);
        XdmSequence converted = new XPathFunctionTypeConverter().ConvertXPathFunctionTypedValue(value, declaration.SequenceType, context);
        return new XsltTransformationEngine().CopyXsltRuntimeSequence(converted);
    }

    internal XsltAccumulatorValue EvaluateXsltAccumulatorRuleValue(XmlNode node, XsltAccumulatorDeclarationPlan declaration, XsltAccumulatorRulePlan rule, XsltAccumulatorValue supplied)
    {
        if (supplied.Failure != null)
        {
            return supplied;
        }

        XdmSequence? oldValue = supplied.Value;
        if (oldValue == null)
        {
            return new XsltAccumulatorValue(null, new InvalidOperationException("An accumulator rule has no previous value."));
        }

        XdmSequence candidate = Sequences.CreateXdmNodeSequence(node);
        XPathDynamicContext context = CreateXsltAccumulatorFocus(candidate);
        XsltVariableFrame frame = new XsltVariableFrame(null, [], context);
        frame.DynamicBindings.Add(CreateXsltAccumulatorValueBinding(declaration.SequenceType, oldValue));
        Task<XdmSequence> pending = Task.Run(() => EvaluateAndConvertXsltAccumulatorRuleValue(candidate, declaration, rule, frame));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        return ReadXsltAccumulatorTaskValue(pending);
    }

    internal XdmSequence EvaluateAndConvertXsltAccumulatorRuleValue(XdmSequence candidate, XsltAccumulatorDeclarationPlan declaration, XsltAccumulatorRulePlan rule, XsltVariableFrame frame)
    {
        if (State.TemplateRuntime == null)
        {
            throw new InvalidOperationException("The template runtime is unavailable during accumulator evaluation.");
        }

        XdmSequence value;
        if (rule.ValueKind == XsltAccumulatorValueKind.Expression)
        {
            XPathPlan plan = rule.Select == null ? throw new InvalidOperationException("An accumulator rule select expression has no plan.") : rule.Select;
            value = State.TemplateRuntime.EvaluateXsltInstructionExpression(plan, frame);
        }
        else
        {
            XsltSequenceConstructorPlan content = rule.Content == null ? throw new InvalidOperationException("An accumulator rule sequence constructor has no plan.") : rule.Content;
            XsltTemplateInvocation invocation = new XsltTemplateInvocation(XsltTemplateDispatchKind.Named, null, null, State.Stylesheet.InitialMode, candidate, 1, 1, State.TemplateRuntime.Depth);
            invocation.Frame = frame;
            value = State.TemplateRuntime.SequenceRuntime.EvaluateXsltSequenceConstructor(content, invocation);
        }

        XPathStaticContext conversion = CreateXsltAccumulatorConversionContext(declaration, rule);
        XdmSequence converted = new XPathFunctionTypeConverter().ConvertXPathFunctionTypedValue(value, declaration.SequenceType, conversion);
        return new XsltTransformationEngine().CopyXsltRuntimeSequence(converted);
    }

    internal XPathStaticContext CreateXsltAccumulatorConversionContext(XsltAccumulatorDeclarationPlan declaration, XsltAccumulatorRulePlan rule)
    {
        if (rule.Select != null)
        {
            return new XPathFunctionContextFactory().CreateXPathFunctionStaticContext(rule.Select.Environment);
        }

        XsltStaticContext source = declaration.StaticContext;
        XPathStaticContext result = new XPathStaticContext();
        result.LanguageVersion = XPathLanguageVersion.XPath31;
        result.CompatibilityMode = source.XPathCompatibilityMode;
        result.Namespaces = source.Namespaces;
        result.DefaultElementNamespace = source.XPathDefaultNamespace;
        result.DefaultFunctionNamespace = XPathNamespaces.Functions;
        result.BaseUri = source.BaseUri;
        result.DefaultCollation = source.DefaultCollation;
        result.Collations = [XPathNamespaces.CodepointCollation, XPathAsciiCaseInsensitiveCollation.Uri];
        result.SchemaSet = State.Stylesheet.SchemaSet;
        result.DecimalFormats = source.DecimalFormats;
        result.ResolverPolicy = State.DynamicContext.ResolverPolicy;
        result.SchemaAware = State.Executable.IsSchemaAware;
        result.AllowHigherOrderFunctions = true;
        return result;
    }

    internal XPathVariableBinding CreateXsltAccumulatorValueBinding(XPathTypeSyntax type, XdmSequence value)
    {
        XPathVariableBinding result = new XPathVariableBinding();
        result.Name = new XmlName { LocalName = "value", QualifiedName = "value", NamespaceUri = "" };
        result.Value = value;
        result.DeclaredType = type.AtomicType == null ? XdmAtomicType.AnyAtomicType : type.AtomicType.Type;
        result.DeclaredOccurrence = new XPathFunctionTypeConverter().ReadXPathFunctionOccurrence(type.Occurrence);
        result.StaticTypeSyntax = type;
        return result;
    }

    internal XPathDynamicContext CreateXsltAccumulatorFocus(XdmSequence candidate)
    {
        XPathDynamicContext focused = State.TemplateRuntime == null ? new XPathFunctionContextFactory().CopyXPathFunctionDynamicComponents(State.DynamicContext) : State.TemplateRuntime.CreateXsltTemplateFocusContext(State.DynamicContext, candidate, 1, 1);
        focused.ContextItem = candidate;
        focused.Position = 1;
        focused.Size = 1;
        focused.XsltAccumulators = this;
        return focused;
    }

    internal XsltAccumulatorValue ReadXsltAccumulatorTaskValue(Task<XdmSequence> pending)
    {
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return new XsltAccumulatorValue(pending.Result, null);
        }

        return new XsltAccumulatorValue(null, ReadXsltAccumulatorFailure(pending.Exception));
    }

    internal Exception ReadXsltAccumulatorFailure(AggregateException? aggregate)
    {
        Exception failure = aggregate == null ? new InvalidOperationException("Accumulator evaluation failed without an exception.") : aggregate;
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

    internal XsltAccumulatorNodeValue? FindXsltAccumulatorNodeValue(XsltTreeAccumulatorEvaluation evaluation, XmlNode node)
    {
        int index = 0;
        while (index < evaluation.Nodes.Count)
        {
            if (object.ReferenceEquals(evaluation.Nodes[index].Node, node))
            {
                return evaluation.Nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltDynamicException CreateXsltAccumulatorDynamicError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XsltDynamicException(message, diagnostic, null);
    }

    internal XsltTypeException CreateXsltAccumulatorTypeError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XsltTypeException(message, diagnostic, null);
    }
}
