using System;
using System.Collections.Generic;

namespace html;

internal static class HtmlTreeSearch
{
    internal static HtmlNode? FindFirst(HtmlNode root,
        Func<HtmlNode, bool> predicate, int maximumVisits)
    {
        ValidatePredicate(predicate);
        ValidateMaximumVisits(maximumVisits);
        List<HtmlNode> pending = CreatePending(root);
        int visitCount = 0;
        while (pending.Count > 0)
        {
            if (visitCount == maximumVisits)
            {
                throw CreateVisitLimitException();
            }

            HtmlNode current = TakeLast(pending);
            AddChildrenInReverse(pending, current.GetChildrenStorage());
            visitCount = visitCount + 1;
            if (predicate(current))
            {
                return current;
            }
        }

        return null;
    }

    internal static HtmlNode[] FindAll(HtmlNode root,
        Func<HtmlNode, bool> predicate, int maximumResults,
        int maximumVisits)
    {
        ValidatePredicate(predicate);
        ValidateMaximumResults(maximumResults);
        ValidateMaximumVisits(maximumVisits);
        List<HtmlNode> result = new List<HtmlNode>();
        List<HtmlNode> pending = CreatePending(root);
        int visitCount = 0;
        while (pending.Count > 0)
        {
            if (visitCount == maximumVisits)
            {
                throw CreateVisitLimitException();
            }

            HtmlNode current = TakeLast(pending);
            AddChildrenInReverse(pending, current.GetChildrenStorage());
            visitCount = visitCount + 1;
            if (predicate(current))
            {
                result.Add(current);
                if (result.Count == maximumResults)
                {
                    return result.ToArray();
                }
            }
        }

        return result.ToArray();
    }

    internal static HtmlNode? FindById(HtmlNode root, string id,
        int maximumVisits)
    {
        ValidateValue(id, "id");
        return FindFirst(root, node => MatchesAttributeValue(node, "id", id),
            maximumVisits);
    }

    internal static HtmlNode[] FindByTag(HtmlNode root, string tagName,
        int maximumResults, int maximumVisits)
    {
        ValidateValue(tagName, "tagName");
        return FindAll(root, node => MatchesTag(node, tagName),
            maximumResults, maximumVisits);
    }

    internal static HtmlNode[] FindByClass(HtmlNode root, string className,
        int maximumResults, int maximumVisits)
    {
        HtmlTreeMutation.ValidateClassToken(className);
        return FindAll(root, node => MatchesClass(node, className),
            maximumResults, maximumVisits);
    }

    internal static HtmlNode[] FindByName(HtmlNode root, string name,
        int maximumResults, int maximumVisits)
    {
        ValidateValue(name, "name");
        return FindAll(root,
            node => MatchesAttributeValue(node, "name", name),
            maximumResults, maximumVisits);
    }

    internal static HtmlNode[] FindByAttribute(HtmlNode root,
        string attributeName, int maximumResults, int maximumVisits)
    {
        ValidateValue(attributeName, "attributeName");
        return FindAll(root,
            node => MatchesAttribute(node, attributeName),
            maximumResults, maximumVisits);
    }

    internal static HtmlNode[] FindByAttribute(HtmlNode root,
        string attributeName, string value, int maximumResults,
        int maximumVisits)
    {
        ValidateValue(attributeName, "attributeName");
        ValidateValue(value, "value");
        return FindAll(root,
            node => MatchesAttributeValue(node, attributeName, value),
            maximumResults, maximumVisits);
    }

    internal static HtmlNode? Closest(HtmlNode node,
        Func<HtmlNode, bool> predicate, int maximumVisits)
    {
        ValidatePredicate(predicate);
        ValidateMaximumVisits(maximumVisits);
        HtmlNode? current = node;
        int visitCount = 0;
        while (current != null)
        {
            if (visitCount == maximumVisits)
            {
                throw CreateVisitLimitException();
            }

            HtmlNode? parent = current.GetParentStorage();
            visitCount = visitCount + 1;
            if (predicate(current))
            {
                return current;
            }

            current = parent;
        }

        return null;
    }

    internal static int GetDefaultMaximumResults()
    {
        return HtmlLimits.CreateDefault().GetMaximumVisits();
    }

    internal static int GetDefaultMaximumVisits()
    {
        return HtmlLimits.CreateDefault().GetMaximumVisits();
    }

    internal static bool MatchesTag(HtmlNode node, string tagName)
    {
        if (node.GetKindStorage() != HtmlNodeKind.Element)
        {
            return false;
        }

        if (tagName == "*")
        {
            return true;
        }

        string localName = node.GetLocalNameStorage();
        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return localName == tagName;
        }

        return AsciiEqualsIgnoreCase(localName, tagName);
    }

    internal static bool MatchesClass(HtmlNode node, string className)
    {
        if (node.GetKindStorage() != HtmlNodeKind.Element)
        {
            return false;
        }

        string? value = node.GetAttribute("class");
        return value != null
            && HtmlTreeMutation.ContainsClassToken(value, className);
    }

    internal static bool MatchesAttribute(HtmlNode node, string attributeName)
    {
        return node.GetKindStorage() == HtmlNodeKind.Element
            && node.HasAttribute(attributeName);
    }

    internal static bool MatchesAttributeValue(HtmlNode node, string attributeName,
        string value)
    {
        return node.GetKindStorage() == HtmlNodeKind.Element
            && node.GetAttribute(attributeName) == value;
    }

    internal static bool AsciiEqualsIgnoreCase(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (Ascii.ToLower(left[index]) != Ascii.ToLower(right[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static List<HtmlNode> CreatePending(HtmlNode root)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        List<HtmlNode> result = new List<HtmlNode>();
        AddChildrenInReverse(result, root.GetChildrenStorage());
        return result;
    }

    internal static HtmlNode TakeLast(List<HtmlNode> nodes)
    {
        int index = nodes.Count - 1;
        HtmlNode result = nodes[index];
        nodes.RemoveAt(index);
        return result;
    }

    internal static void AddChildrenInReverse(List<HtmlNode> pending,
        HtmlNode[] children)
    {
        int index = children.Length - 1;
        while (index >= 0)
        {
            pending.Add(children[index]);
            index = index - 1;
        }
    }

    internal static void ValidatePredicate(Func<HtmlNode, bool> predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException("predicate");
        }
    }

    internal static void ValidateValue(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    internal static void ValidateMaximumResults(int maximumResults)
    {
        if (maximumResults <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumResults",
                maximumResults, "A search result limit must be positive.");
        }
    }

    internal static void ValidateMaximumVisits(int maximumVisits)
    {
        if (maximumVisits <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumVisits",
                maximumVisits, "A search visit limit must be positive.");
        }
    }

    internal static InvalidOperationException CreateVisitLimitException()
    {
        return new InvalidOperationException(
            "The tree search visit limit was exceeded.");
    }
}
