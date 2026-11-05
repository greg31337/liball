using System.Collections.Generic;

namespace cbor;

internal class CddlFeatureDescriptor
{
    internal string Name = "";
    internal CborValue? Detail;
}

internal class CddlFeatureControlMatcher
{
    internal CddlCoreMatcher Matcher;
    internal CddlValidationState State;

    internal CddlFeatureControlMatcher(
        CddlCoreMatcher matcher,
        CddlValidationState state)
    {
        Matcher = matcher;
        State = state;
    }

    internal CddlMatchResult MatchFeatureControl(
        CddlNode node,
        CborValue value)
    {
        CddlFeatureDescriptor? descriptor = EvaluateFeatureController(
            node.Children[1],
            value
        );
        if (descriptor == null)
        {
            return Matcher.RejectInvalidSchema(
                node,
                "A feature controller must be a feature-name string or "
                    + "a two-element [name, detail] singleton array."
            );
        }
        if (!State.Features.ContainsFeature(descriptor.Name))
        {
            CddlValidationIssue disabled = State.CreateIssue(
                CddlValidationCode.UnsupportedFeature,
                "The matching CDDL alternative requires a disabled feature.",
                node,
                "an enabled feature",
                descriptor.Name
            );
            return Matcher.CreateRejection(disabled);
        }
        CddlControlResult result = new CddlControlResult();
        result.Status = CddlControlStatus.Applied;
        result.FeatureName = descriptor.Name;
        result.FeatureDetail = descriptor.Detail;
        State.AddControlResult(result);
        return Matcher.CreateMatch();
    }

    internal CddlFeatureDescriptor? EvaluateFeatureController(
        CddlNode controller,
        CborValue value)
    {
        CborValue? singleton = new CddlComputedLiteralMatcher(
            Matcher,
            State
        ).EvaluateSingleValue(controller);
        if (singleton != null
            && singleton.Kind == CborValueKind.TextString
            && singleton.TextStringValue != null)
        {
            CddlFeatureDescriptor simple = new CddlFeatureDescriptor();
            simple.Name = singleton.TextStringValue;
            simple.Detail = value;
            return simple;
        }
        CddlNode? array = ResolveFeatureArray(
            controller,
            new List<CddlRule>()
        );
        if (array == null || array.Children.Count != 1)
        {
            return null;
        }
        CddlNode? group = ResolveFeatureGroup(array.Children[0]);
        if (group == null || group.Children.Count != 2)
        {
            return null;
        }
        CddlNode? nameNode = FeatureEntryValue(group.Children[0]);
        CddlNode? detailNode = FeatureEntryValue(group.Children[1]);
        if (nameNode == null || detailNode == null)
        {
            return null;
        }
        CddlComputedLiteralMatcher evaluator =
            new CddlComputedLiteralMatcher(Matcher, State);
        CborValue? name = evaluator.EvaluateSingleValue(nameNode);
        CborValue? detail = evaluator.EvaluateSingleValue(detailNode);
        if (name == null
            || name.Kind != CborValueKind.TextString
            || name.TextStringValue == null
            || detail == null)
        {
            return null;
        }
        CddlFeatureDescriptor explicitDetail = new CddlFeatureDescriptor();
        explicitDetail.Name = name.TextStringValue;
        explicitDetail.Detail = detail;
        return explicitDetail;
    }

    internal CddlNode? ResolveFeatureArray(
        CddlNode node,
        List<CddlRule> visited)
    {
        if (node.Kind == CddlNodeKind.Array)
        {
            return node;
        }
        if ((node.Kind == CddlNodeKind.ParenthesizedType
                || node.Kind == CddlNodeKind.TypeChoice)
            && node.Children.Count == 1)
        {
            return ResolveFeatureArray(node.Children[0], visited);
        }
        if (node.Kind == CddlNodeKind.RuleReference && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null
                && rule.Value != null
                && !Matcher.ContainsRule(visited, rule))
            {
                visited.Add(rule);
                CddlNode? resolved = ResolveFeatureArray(rule.Value, visited);
                visited.RemoveAt(visited.Count - 1);
                return resolved;
            }
        }
        return null;
    }

    internal CddlNode? ResolveFeatureGroup(CddlNode node)
    {
        CddlNode current = node;
        while ((current.Kind == CddlNodeKind.ParenthesizedGroup
                || current.Kind == CddlNodeKind.GroupChoice)
            && current.Children.Count == 1)
        {
            current = current.Children[0];
        }
        return current.Kind == CddlNodeKind.Group ? current : null;
    }

    internal CddlNode? FeatureEntryValue(CddlNode entry)
    {
        if (entry.Kind != CddlNodeKind.GroupEntry
            || entry.MinimumOccurrence != 1
            || entry.MaximumOccurrence != 1
            || entry.Children.Count == 0)
        {
            return null;
        }
        return entry.Children[entry.Children.Count - 1];
    }
}
