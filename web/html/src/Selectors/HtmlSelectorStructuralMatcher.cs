using System;

namespace html;

internal static class HtmlSelectorStructuralMatcher
{
    internal static bool Matches(HtmlNode node,
        HtmlSelectorPseudoClassKind pseudoClassKind,
        HtmlNthExpression? nthExpression, HtmlNode scopeRoot)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if ((int)pseudoClassKind < (int)HtmlSelectorPseudoClassKind.Root
            || (int)pseudoClassKind
                > (int)HtmlSelectorPseudoClassKind.OnlyOfType)
        {
            throw new ArgumentOutOfRangeException("pseudoClassKind",
                (int)pseudoClassKind, "Unknown selector pseudo-class.");
        }

        bool nth = IsNthPseudoClass(pseudoClassKind);
        if (nth && nthExpression == null)
        {
            throw new ArgumentNullException("nthExpression");
        }

        if (!nth && nthExpression != null)
        {
            throw new ArgumentException(
                "Only an nth pseudo-class can use an An+B expression.",
                "nthExpression");
        }

        if (node.GetKind() != HtmlNodeKind.Element)
        {
            return false;
        }

        switch (pseudoClassKind)
        {
            case HtmlSelectorPseudoClassKind.Root:
                return IsRoot(node);
            case HtmlSelectorPseudoClassKind.Scope:
                return IsScope(node, scopeRoot);
            case HtmlSelectorPseudoClassKind.Empty:
                return IsEmpty(node);
            case HtmlSelectorPseudoClassKind.NthChild:
                return GetRequiredNth(nthExpression).Matches(
                    GetIndex(node, false, false));
            case HtmlSelectorPseudoClassKind.NthLastChild:
                return GetRequiredNth(nthExpression).Matches(
                    GetIndex(node, true, false));
            case HtmlSelectorPseudoClassKind.FirstChild:
                return node.GetPreviousElementSibling() == null;
            case HtmlSelectorPseudoClassKind.LastChild:
                return node.GetNextElementSibling() == null;
            case HtmlSelectorPseudoClassKind.OnlyChild:
                return node.GetPreviousElementSibling() == null
                    && node.GetNextElementSibling() == null;
            case HtmlSelectorPseudoClassKind.NthOfType:
                return GetRequiredNth(nthExpression).Matches(
                    GetIndex(node, false, true));
            case HtmlSelectorPseudoClassKind.NthLastOfType:
                return GetRequiredNth(nthExpression).Matches(
                    GetIndex(node, true, true));
            case HtmlSelectorPseudoClassKind.FirstOfType:
                return !HasTypeSibling(node, false);
            case HtmlSelectorPseudoClassKind.LastOfType:
                return !HasTypeSibling(node, true);
            case HtmlSelectorPseudoClassKind.OnlyOfType:
                return !HasTypeSibling(node, false)
                    && !HasTypeSibling(node, true);
            default:
                return false;
        }
    }

    internal static HtmlNthExpression GetRequiredNth(
        HtmlNthExpression? expression)
    {
        if (expression == null)
        {
            throw new InvalidOperationException(
                "An nth pseudo-class requires an expression.");
        }

        return expression;
    }

    internal static bool IsRoot(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        HtmlNode? parent = node.GetParent();
        return node.GetKind() == HtmlNodeKind.Element && parent != null
            && parent.GetKind() == HtmlNodeKind.Document;
    }

    internal static bool IsEmpty(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.GetKind() != HtmlNodeKind.Element)
        {
            return false;
        }

        HtmlNode? current = node.GetFirstChild();
        while (current != null)
        {
            if (current.GetKind() == HtmlNodeKind.Element)
            {
                return false;
            }

            if (current.GetKind() == HtmlNodeKind.Text
                && HasNonWhitespace(current.GetData()))
            {
                return false;
            }

            current = current.GetNextSibling();
        }

        return true;
    }

    internal static bool IsScope(HtmlNode node, HtmlNode scopeRoot)
    {
        if (scopeRoot == null)
        {
            return IsRoot(node);
        }

        if (scopeRoot.GetKind() == HtmlNodeKind.Element)
        {
            return node == scopeRoot;
        }

        if (scopeRoot.GetKind() == HtmlNodeKind.Document)
        {
            return node.GetParent() == scopeRoot;
        }

        return false;
    }

    internal static int GetIndex(HtmlNode node, bool fromEnd, bool ofType)
    {
        int index = 1;
        HtmlNode? sibling = fromEnd
            ? node.GetNextElementSibling()
            : node.GetPreviousElementSibling();
        while (sibling != null)
        {
            if (!ofType || HasSameType(node, sibling))
            {
                index = index + 1;
            }

            sibling = fromEnd
                ? sibling.GetNextElementSibling()
                : sibling.GetPreviousElementSibling();
        }

        return index;
    }

    internal static bool HasTypeSibling(HtmlNode node, bool after)
    {
        HtmlNode? sibling = after
            ? node.GetNextElementSibling()
            : node.GetPreviousElementSibling();
        while (sibling != null)
        {
            if (HasSameType(node, sibling))
            {
                return true;
            }

            sibling = after
                ? sibling.GetNextElementSibling()
                : sibling.GetPreviousElementSibling();
        }

        return false;
    }

    internal static bool HasSameType(HtmlNode left, HtmlNode right)
    {
        return left.GetNamespaceUri() == right.GetNamespaceUri()
            && left.GetLocalName() == right.GetLocalName();
    }

    internal static bool HasNonWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!Ascii.IsHtmlWhitespace(value[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsNthPseudoClass(
        HtmlSelectorPseudoClassKind pseudoClassKind)
    {
        return pseudoClassKind == HtmlSelectorPseudoClassKind.NthChild
            || pseudoClassKind == HtmlSelectorPseudoClassKind.NthLastChild
            || pseudoClassKind == HtmlSelectorPseudoClassKind.NthOfType
            || pseudoClassKind
                == HtmlSelectorPseudoClassKind.NthLastOfType;
    }
}
