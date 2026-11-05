using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltPatternMatcher
{
    internal bool MatchesXsltPattern(XsltMatchPatternPlan pattern, XdmSequence candidate, XPathDynamicContext dynamicContext)
    {
        Guard.NotNull(pattern, "pattern");
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(dynamicContext, "dynamicContext");
        new XdmSequenceOperations().ValidateXdmSequenceStorage(candidate);
        if (candidate.Count != 1 || pattern.IsDeferred || pattern.Expression == null)
        {
            return false;
        }

        if (pattern.EquivalentExpression == null)
        {
            return false;
        }

        XPathDynamicContext context = CopyXsltPatternDynamicContext(dynamicContext, candidate);
        if (pattern.HasKeyCall)
        {
            XdmSequence direct = new XPathRuntime(pattern.EquivalentExpression, context).EvaluateXPathPlan();
            XdmSequenceItem directItem = new XdmSequenceOperations().ReadXdmSequenceItem(candidate, 0);
            return directItem.Node != null && ContainsXsltPatternNode(direct, directItem.Node);
        }

        Task<XdmSequence> pending = Task.Run(() => new XPathRuntime(pattern.EquivalentExpression, context).EvaluateXPathPlan());
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            return false;
        }

        XdmSequence result = pending.Result;
        if (pattern.IsPredicatePattern)
        {
            return result.Count != 0;
        }

        XdmSequenceItem supplied = new XdmSequenceOperations().ReadXdmSequenceItem(candidate, 0);
        XmlNode? node = supplied.Node;
        if (node == null)
        {
            return false;
        }

        if (ContainsXsltPatternNode(result, node))
        {
            return true;
        }

        if (node.Parent != null)
        {
            return false;
        }

        Task<bool> fallback = Task.Run(() => MatchesXsltParentlessPattern(pattern.Expression, node, pattern.EquivalentExpression, pattern.Predicates, context));
        ((IAsyncResult)fallback).AsyncWaitHandle.WaitOne();
        return fallback.Status == TaskStatus.RanToCompletion && fallback.Result;
    }

    internal bool ContainsXsltPatternNode(XdmSequence sequence, XmlNode node)
    {
        XdmSequenceOperations operations = new XdmSequenceOperations();
        int index = 0;
        while (index < sequence.Count)
        {
            XdmSequenceItem item = operations.ReadXdmSequenceItem(sequence, index);
            if (item.Node == node)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool MatchesXsltParentlessPattern(XPathAstNode expression, XmlNode node, XPathPlan equivalent, XsltPatternPredicatePlan[] predicates, XPathDynamicContext context)
    {
        if (expression.Kind == XPathAstKind.Union)
        {
            return expression.Left != null && MatchesXsltParentlessPattern(expression.Left, node, equivalent, predicates, context) || expression.Right != null && MatchesXsltParentlessPattern(expression.Right, node, equivalent, predicates, context);
        }

        if (expression.Kind == XPathAstKind.Intersect)
        {
            return expression.Left != null && expression.Right != null && MatchesXsltParentlessPattern(expression.Left, node, equivalent, predicates, context) && MatchesXsltParentlessPattern(expression.Right, node, equivalent, predicates, context);
        }

        if (expression.Kind == XPathAstKind.Except)
        {
            return expression.Left != null && MatchesXsltParentlessPattern(expression.Left, node, equivalent, predicates, context) && (expression.Right == null || !MatchesXsltParentlessPattern(expression.Right, node, equivalent, predicates, context));
        }

        if (expression.Kind == XPathAstKind.Parenthesized)
        {
            return expression.Operand != null && MatchesXsltParentlessPattern(expression.Operand, node, equivalent, predicates, context);
        }

        if (expression.Kind == XPathAstKind.Path)
        {
            if (expression.Absolute || expression.Children.Length != 1)
            {
                return false;
            }

            return MatchesXsltParentlessStep(expression.Children[0], node, equivalent, predicates, context);
        }

        return MatchesXsltParentlessStep(expression, node, equivalent, predicates, context);
    }

    internal bool MatchesXsltParentlessStep(XPathAstNode step, XmlNode node, XPathPlan equivalent, XsltPatternPredicatePlan[] predicatePlans, XPathDynamicContext context)
    {
        List<XPathAstNode> predicates = new List<XPathAstNode>();
        XPathAstNode baseStep = step;
        while (baseStep.Kind == XPathAstKind.Predicate && baseStep.Left != null && baseStep.Right != null)
        {
            predicates.Add(baseStep.Right);
            baseStep = baseStep.Left;
        }

        if (baseStep.Kind != XPathAstKind.Name && baseStep.Kind != XPathAstKind.AttributeStep && baseStep.Kind != XPathAstKind.AxisStep || baseStep.NodeTest == null)
        {
            return false;
        }

        if (baseStep.Axis == XPathAxis.Descendant)
        {
            return false;
        }

        XdmNode wrapped = new XdmNodeAccessor().WrapXmlNode(node);
        if (baseStep.Axis == XPathAxis.Child)
        {
            if (baseStep.NodeTest.Kind == XPathNodeTestKind.Document)
            {
                string token = baseStep.Token == null || baseStep.Token.Value == null ? "" : baseStep.Token.Value;
                if (token == "child")
                {
                    return false;
                }
            }
            else if (wrapped.Kind != XdmNodeKind.Element && wrapped.Kind != XdmNodeKind.Text && wrapped.Kind != XdmNodeKind.Comment && wrapped.Kind != XdmNodeKind.ProcessingInstruction)
            {
                return false;
            }
        }
        else if (baseStep.Axis == XPathAxis.Attribute && wrapped.Kind != XdmNodeKind.Attribute || baseStep.Axis == XPathAxis.Namespace && wrapped.Kind != XdmNodeKind.Namespace)
        {
            return false;
        }

        XPathRuntime runtime = new XPathRuntime(equivalent, context);
        if (!runtime.Paths.MatchesXPathNodeTest(wrapped, baseStep.NodeTest, baseStep.Axis))
        {
            return false;
        }

        int index = predicates.Count - 1;
        while (index >= 0)
        {
            XPathPlan? predicate = FindXsltPatternPredicate(predicatePlans, step, predicates[index]);
            if (predicate == null)
            {
                return false;
            }

            XPathRuntime predicateRuntime = new XPathRuntime(predicate, context);
            XdmSequence value = predicateRuntime.EvaluateXPathPlan();
            if (!predicateRuntime.Paths.MatchesXPathPredicatePosition(value, 1, predicate.Range))
            {
                return false;
            }

            index = index - 1;
        }

        return true;
    }

    internal XPathPlan? FindXsltPatternPredicate(XsltPatternPredicatePlan[] plans, XPathAstNode step, XPathAstNode expression)
    {
        XPathAstNode current = step;
        while (current.Kind == XPathAstKind.Predicate)
        {
            if (current.Right == expression)
            {
                int index = 0;
                while (index < plans.Length)
                {
                    if (plans[index].Predicate == current)
                    {
                        return plans[index].Expression;
                    }

                    index = index + 1;
                }
            }

            if (current.Left == null)
            {
                break;
            }

            current = current.Left;
        }

        return null;
    }

    internal XPathDynamicContext CopyXsltPatternDynamicContext(XPathDynamicContext source, XdmSequence candidate)
    {
        XPathDynamicContext result = new XPathDynamicContext();
        result.ContextItem = candidate;
        result.Position = 1;
        result.Size = 1;
        result.Variables = source.Variables;
        result.VariableResolver = source.VariableResolver;
        result.BaseUri = source.BaseUri;
        result.DefaultCollectionUri = source.DefaultCollectionUri;
        result.ImplicitTimezoneMinutes = source.ImplicitTimezoneMinutes;
        result.DefaultLanguage = source.DefaultLanguage;
        result.CurrentDateTime = source.CurrentDateTime;
        result.EnvironmentNames = source.EnvironmentNames;
        result.EnvironmentValues = source.EnvironmentValues;
        result.ResolverPolicy = source.ResolverPolicy;
        result.CollectionResolver = source.CollectionResolver;
        result.ResourceResolver = source.ResourceResolver;
        result.TraceReceiver = source.TraceReceiver;
        result.Cancellation = source.Cancellation;
        result.SecurityLimits = source.SecurityLimits;
        result.MaximumOperations = source.MaximumOperations;
        result.XsltKeys = source.XsltKeys;
        result.XsltAccumulators = source.XsltAccumulators;
        return result;
    }
}
