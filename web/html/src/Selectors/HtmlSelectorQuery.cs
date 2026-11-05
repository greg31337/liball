using System;
using System.Collections.Generic;
using System.Threading;

namespace html;

internal static class HtmlSelectorQuery
{
    internal static bool Matches(HtmlSelector selector, HtmlNode node,
        HtmlNode scope, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        ValidateSelectorAndLimits(selector, limits);
        if (selector == null)
        {
            throw new ArgumentNullException("selector");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (scope == null)
        {
            throw new ArgumentNullException("scope");
        }

        HtmlBudget budget = new HtmlBudget(limits, cancellationToken);
        if (!budget.TryConsume(HtmlLimitKind.Visits, 1))
        {
            throw CreateLimitException(HtmlLimitKind.Visits);
        }

        bool result = MatchesPlans(selector.GetMatchPlansStorage(), node,
            scope, budget);
        ThrowIfMatchingLimitExceeded(budget);
        return result;
    }

    internal static HtmlNode? Closest(HtmlSelector? selector, HtmlNode node,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        ValidateSelectorAndLimits(selector, limits);
        if (selector == null)
        {
            throw new ArgumentNullException("selector");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        HtmlBudget budget = new HtmlBudget(limits, cancellationToken);
        HtmlNode scope = node;
        HtmlNode? current = node;
        while (current != null)
        {
            HtmlNode? parent = current.GetParentStorage();
            if (!budget.TryConsume(HtmlLimitKind.Visits, 1))
            {
                throw CreateLimitException(HtmlLimitKind.Visits);
            }

            if (MatchesPlans(selector.GetMatchPlansStorage(), current,
                scope, budget))
            {
                ThrowIfMatchingLimitExceeded(budget);
                return current;
            }

            ThrowIfMatchingLimitExceeded(budget);
            current = parent;
        }

        return null;
    }

    internal static HtmlNode? FindFirst(HtmlSelector selector, HtmlNode root,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        HtmlNode[] candidates = GetCandidates(selector, root, limits,
            cancellationToken, true);
        return candidates.Length == 0 ? null : candidates[0];
    }

    internal static HtmlNode[] FindAll(HtmlSelector selector, HtmlNode root,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return GetCandidates(selector, root, limits, cancellationToken,
            false);
    }

    internal static HtmlNode[] GetCandidates(HtmlSelector selector, HtmlNode root,
        HtmlLimits limits, CancellationToken cancellationToken,
        bool firstOnly)
    {
        ValidateSelectorAndLimits(selector, limits);
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        HtmlBudget budget = new HtmlBudget(limits, cancellationToken);
        HtmlSelectorMatchPlan[] plans = selector.GetMatchPlansStorage();
        List<HtmlNode> candidates = IsConnected(root)
            ? GetIndexedCandidates(root, plans, budget)
            : GetTreeCandidates(root, plans, budget);
        List<HtmlNode> result = new List<HtmlNode>();
        int maximumResults = limits.GetMaximumSelectorResults();
        int index = 0;
        while (index < candidates.Count)
        {
            HtmlNode candidate = candidates[index];
            if (MatchesPlans(plans, candidate, root, budget))
            {
                if (!budget.TryConsume(HtmlLimitKind.SelectorResults, 1))
                {
                    return result.ToArray();
                }

                result.Add(candidate);
                if (firstOnly || result.Count == maximumResults)
                {
                    return result.ToArray();
                }
            }

            ThrowIfMatchingLimitExceeded(budget);
            index = index + 1;
        }

        return result.ToArray();
    }

    internal static List<HtmlNode> GetIndexedCandidates(HtmlNode root,
        HtmlSelectorMatchPlan[] plans, HtmlBudget budget)
    {
        HtmlDocument document = root.GetOwnerDocumentStorage();
        HtmlDocumentIndex documentIndex =
            document.GetSearchIndexStorage();
        HashSet<HtmlNode> unique = new HashSet<HtmlNode>();
        int planIndex = 0;
        while (planIndex < plans.Length)
        {
            HtmlNode[] seeded = SeedCandidates(documentIndex,
                plans[planIndex]);
            int candidateIndex = 0;
            while (candidateIndex < seeded.Length)
            {
                if (!budget.TryConsume(HtmlLimitKind.Visits, 1))
                {
                    throw CreateLimitException(HtmlLimitKind.Visits);
                }

                HtmlNode candidate = seeded[candidateIndex];
                if (IsDescendantOf(candidate, root))
                {
                    unique.Add(candidate);
                }

                candidateIndex = candidateIndex + 1;
            }

            planIndex = planIndex + 1;
        }

        List<HtmlNode> result = new List<HtmlNode>(unique);
        result.Sort((left, right) => documentIndex.GetDocumentOrder(left)
            .CompareTo(documentIndex.GetDocumentOrder(right)));
        return result;
    }

    internal static List<HtmlNode> GetTreeCandidates(HtmlNode root,
        HtmlSelectorMatchPlan[] plans, HtmlBudget budget)
    {
        List<HtmlNode> result = new List<HtmlNode>();
        List<HtmlNode> pending = new List<HtmlNode>();
        AddChildrenInReverse(pending, root);
        while (pending.Count != 0)
        {
            int last = pending.Count - 1;
            HtmlNode current = pending[last];
            pending.RemoveAt(last);
            if (!budget.TryConsume(HtmlLimitKind.Visits, 1))
            {
                throw CreateLimitException(HtmlLimitKind.Visits);
            }

            AddChildrenInReverse(pending, current);
            if (current.GetKindStorage() == HtmlNodeKind.Element
                && MatchesAnyCandidateHint(plans, current))
            {
                result.Add(current);
            }
        }

        return result;
    }

    internal static HtmlNode[] SeedCandidates(HtmlDocumentIndex index,
        HtmlSelectorMatchPlan plan)
    {
        switch (plan.GetCandidateKind())
        {
            case HtmlSelectorCandidateKind.Id:
                return index.FindAllById(plan.GetCandidateValue());
            case HtmlSelectorCandidateKind.Class:
                return index.FindByClass(plan.GetCandidateValue());
            case HtmlSelectorCandidateKind.Type:
                return index.FindByTag(plan.GetCandidateValue());
            default:
                return index.GetAllElements();
        }
    }

    internal static bool MatchesPlans(HtmlSelectorMatchPlan[] plans,
        HtmlNode node, HtmlNode scope, HtmlBudget budget)
    {
        if (node.GetKindStorage() != HtmlNodeKind.Element)
        {
            return false;
        }

        int index = 0;
        while (index < plans.Length)
        {
            if (MatchesCandidateHint(plans[index], node)
                && HtmlSelectorMatcher.Matches(node,
                    plans[index].GetSelectorStorage(), scope, budget))
            {
                return true;
            }

            if (budget.HasExceededLimit())
            {
                return false;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool MatchesAnyCandidateHint(HtmlSelectorMatchPlan[] plans,
        HtmlNode node)
    {
        int index = 0;
        while (index < plans.Length)
        {
            if (MatchesCandidateHint(plans[index], node))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool MatchesCandidateHint(HtmlSelectorMatchPlan plan,
        HtmlNode node)
    {
        switch (plan.GetCandidateKind())
        {
            case HtmlSelectorCandidateKind.Id:
                return node.GetAttribute("id") == plan.GetCandidateValue();
            case HtmlSelectorCandidateKind.Class:
                return HasClass(node.GetAttribute("class"),
                    plan.GetCandidateValue());
            case HtmlSelectorCandidateKind.Type:
                return node.GetNamespaceStorage() == HtmlNamespace.Html
                    ? Ascii.EqualsIgnoreCase(
                        node.GetLocalNameStorage().AsSpan(),
                        plan.GetCandidateValue().AsSpan())
                    : node.GetLocalNameStorage()
                        == plan.GetCandidateValue();
            default:
                return true;
        }
    }

    internal static bool HasClass(string? classes, string expected)
    {
        if (classes == null)
        {
            return false;
        }

        int position = 0;
        while (position < classes.Length)
        {
            while (position < classes.Length
                && Ascii.IsHtmlWhitespace(classes[position]))
            {
                position = position + 1;
            }

            int start = position;
            while (position < classes.Length
                && !Ascii.IsHtmlWhitespace(classes[position]))
            {
                position = position + 1;
            }

            if (position - start == expected.Length
                && string.CompareOrdinal(classes, start, expected, 0,
                    expected.Length) == 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsConnected(HtmlNode root)
    {
        HtmlDocument document = root.GetOwnerDocumentStorage();
        return document != null && root.GetRoot() == document.GetRootStorage();
    }

    internal static bool IsDescendantOf(HtmlNode node, HtmlNode root)
    {
        HtmlNode? current = node.GetParentStorage();
        while (current != null)
        {
            if (current == root)
            {
                return true;
            }

            current = current.GetParentStorage();
        }

        return false;
    }

    internal static void AddChildrenInReverse(List<HtmlNode> pending,
        HtmlNode parent)
    {
        HtmlNode[] children = parent.GetChildrenStorage();
        int index = children.Length - 1;
        while (index >= 0)
        {
            pending.Add(children[index]);
            index = index - 1;
        }
    }

    internal static void ValidateSelectorAndLimits(HtmlSelector? selector,
        HtmlLimits limits)
    {
        if (selector == null)
        {
            throw new ArgumentNullException("selector");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
    }

    internal static void ThrowIfMatchingLimitExceeded(HtmlBudget budget)
    {
        if (budget.HasExceededLimit())
        {
            throw CreateLimitException(budget.GetFirstExceededLimit());
        }
    }

    internal static InvalidOperationException CreateLimitException(
        HtmlLimitKind kind)
    {
        return new InvalidOperationException(
            kind == HtmlLimitKind.Visits
                ? "The selector query visit limit was exceeded."
                : "The selector matching work limit was exceeded.");
    }
}
