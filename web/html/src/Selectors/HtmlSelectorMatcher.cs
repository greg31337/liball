using System;
using System.Collections.Generic;

namespace html;

internal static class HtmlSelectorMatcher
{
    internal static bool Matches(HtmlNode subject,
        HtmlComplexSelectorAst selector, HtmlNode scopeRoot,
        HtmlBudget budget)
    {
        if (subject == null)
        {
            throw new ArgumentNullException("subject");
        }

        if (selector == null)
        {
            throw new ArgumentNullException("selector");
        }

        if (scopeRoot == null)
        {
            throw new ArgumentNullException("scopeRoot");
        }

        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        return MatchesComplex(subject, selector, scopeRoot, budget);
    }

    internal static bool MatchesHas(HtmlNode anchor,
        HtmlRelativeSelectorAst[] selectors, HtmlBudget budget)
    {
        if (anchor == null)
        {
            throw new ArgumentNullException("anchor");
        }

        if (selectors == null)
        {
            throw new ArgumentNullException("selectors");
        }

        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        if (anchor.GetKind() != HtmlNodeKind.Element)
        {
            return false;
        }

        int index = 0;
        while (index < selectors.Length)
        {
            if (selectors[index] == null)
            {
                throw new ArgumentException(
                    "A relative selector cannot be null.", "selectors");
            }

            if (!ConsumeWork(budget))
            {
                return false;
            }

            if (MatchesRelative(anchor, selectors[index], budget))
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

    internal static bool MatchesHas(HtmlNode anchor,
        HtmlSimpleSelectorAst pseudoClass, HtmlBudget budget)
    {
        if (pseudoClass == null)
        {
            throw new ArgumentNullException("pseudoClass");
        }

        if (pseudoClass.GetKind() != HtmlSelectorSimpleKind.PseudoClass
            || pseudoClass.GetPseudoClassKind()
                != HtmlSelectorPseudoClassKind.Has)
        {
            throw new ArgumentException(
                "A :has() pseudo-class is required.", "pseudoClass");
        }

        return MatchesHas(anchor,
            pseudoClass.GetRelativeSelectorArgumentsStorage(), budget);
    }

    internal static bool MatchesRelative(HtmlNode anchor,
        HtmlRelativeSelectorAst relative, HtmlBudget budget)
    {
        HtmlComplexSelectorAst selector = relative.GetSelector();
        HtmlCompoundSelectorAst[] compounds =
            selector.GetCompoundsStorage();
        HtmlSelectorCombinator[] combinators =
            selector.GetCombinatorsStorage();
        List<HtmlSelectorMatchFrame> frames =
            new List<HtmlSelectorMatchFrame>();
        frames.Add(new HtmlSelectorMatchFrame(0,
            new HtmlSelectorRelationCursor(anchor,
                relative.GetLeadingCombinator(), true)));

        while (frames.Count != 0)
        {
            HtmlSelectorMatchFrame frame = frames[frames.Count - 1];
            HtmlNode? candidate = frame.GetCursor().MoveNext(budget);
            if (candidate == null)
            {
                frames.RemoveAt(frames.Count - 1);
                if (budget.HasExceededLimit())
                {
                    return false;
                }

                continue;
            }

            int compoundIndex = frame.GetCompoundIndex();
            if (!MatchesCompound(candidate, compounds[compoundIndex],
                anchor, budget))
            {
                if (budget.HasExceededLimit())
                {
                    return false;
                }

                continue;
            }

            if (compoundIndex + 1 == compounds.Length)
            {
                return true;
            }

            frames.Add(new HtmlSelectorMatchFrame(compoundIndex + 1,
                new HtmlSelectorRelationCursor(candidate,
                    combinators[compoundIndex], true)));
        }

        return false;
    }

    internal static bool MatchesComplex(HtmlNode subject,
        HtmlComplexSelectorAst selector, HtmlNode scopeRoot,
        HtmlBudget budget)
    {
        HtmlCompoundSelectorAst[] compounds =
            selector.GetCompoundsStorage();
        HtmlSelectorCombinator[] combinators =
            selector.GetCombinatorsStorage();
        int last = compounds.Length - 1;
        if (!MatchesCompound(subject, compounds[last], scopeRoot, budget))
        {
            return false;
        }

        if (last == 0)
        {
            return true;
        }

        List<HtmlSelectorMatchFrame> frames =
            new List<HtmlSelectorMatchFrame>();
        frames.Add(new HtmlSelectorMatchFrame(last - 1,
            new HtmlSelectorRelationCursor(subject,
                combinators[last - 1], false)));

        while (frames.Count != 0)
        {
            HtmlSelectorMatchFrame frame = frames[frames.Count - 1];
            HtmlNode? candidate = frame.GetCursor().MoveNext(budget);
            if (candidate == null)
            {
                frames.RemoveAt(frames.Count - 1);
                if (budget.HasExceededLimit())
                {
                    return false;
                }

                continue;
            }

            int compoundIndex = frame.GetCompoundIndex();
            if (!MatchesCompound(candidate, compounds[compoundIndex],
                scopeRoot, budget))
            {
                if (budget.HasExceededLimit())
                {
                    return false;
                }

                continue;
            }

            if (compoundIndex == 0)
            {
                return true;
            }

            frames.Add(new HtmlSelectorMatchFrame(compoundIndex - 1,
                new HtmlSelectorRelationCursor(candidate,
                    combinators[compoundIndex - 1], false)));
        }

        return false;
    }

    internal static bool MatchesCompound(HtmlNode node,
        HtmlCompoundSelectorAst compound, HtmlNode scopeRoot,
        HtmlBudget budget)
    {
        if (!ConsumeWork(budget))
        {
            return false;
        }

        if (node.GetKind() != HtmlNodeKind.Element)
        {
            return MatchesVirtualFragmentScope(node, compound, scopeRoot);
        }

        HtmlSimpleSelectorAst[] simpleSelectors =
            compound.GetSimpleSelectorsStorage();
        int index = 0;
        while (index < simpleSelectors.Length)
        {
            if (!ConsumeWork(budget))
            {
                return false;
            }

            bool matches = MatchesSimple(node, simpleSelectors[index],
                scopeRoot, budget);
            if (budget.HasExceededLimit() || !matches)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool MatchesVirtualFragmentScope(HtmlNode node,
        HtmlCompoundSelectorAst compound, HtmlNode scopeRoot)
    {
        if (node != scopeRoot
            || node.GetKind() != HtmlNodeKind.Fragment)
        {
            return false;
        }

        HtmlSimpleSelectorAst[] simpleSelectors =
            compound.GetSimpleSelectorsStorage();
        return simpleSelectors.Length == 1
            && simpleSelectors[0].GetKind()
                == HtmlSelectorSimpleKind.PseudoClass
            && simpleSelectors[0].GetPseudoClassKind()
                == HtmlSelectorPseudoClassKind.Scope;
    }

    internal static bool MatchesSimple(HtmlNode node,
        HtmlSimpleSelectorAst simple, HtmlNode scopeRoot,
        HtmlBudget budget)
    {
        switch (simple.GetKind())
        {
            case HtmlSelectorSimpleKind.Type:
            case HtmlSelectorSimpleKind.Universal:
                return MatchesQualifiedName(node,
                    simple.GetQualifiedName());
            case HtmlSelectorSimpleKind.Id:
                return node.GetAttribute("id") == simple.GetName();
            case HtmlSelectorSimpleKind.Class:
                return HasClass(node.GetAttribute("class"),
                    simple.GetName());
            case HtmlSelectorSimpleKind.Attribute:
                return MatchesAttribute(node, simple, budget);
            case HtmlSelectorSimpleKind.PseudoClass:
                return MatchesPseudoClass(node, simple, scopeRoot, budget);
            default:
                return false;
        }
    }

    internal static bool MatchesQualifiedName(HtmlNode node,
        HtmlSelectorQualifiedNameAst? name)
    {
        if (name == null)
        {
            return false;
        }

        if (!MatchesNamespace(name.GetNamespaceKind(),
            name.GetResolvedNamespaceUri(),
            node.GetNamespaceUri()))
        {
            return false;
        }

        if (name.IsUniversal())
        {
            return true;
        }

        return node.GetNamespaceStorage() == HtmlNamespace.Html
            ? Ascii.EqualsIgnoreCase(node.GetLocalName().AsSpan(),
                name.GetLocalName().AsSpan())
            : node.GetLocalName() == name.GetLocalName();
    }

    internal static bool MatchesAttribute(HtmlNode node,
        HtmlSimpleSelectorAst simple, HtmlBudget budget)
    {
        HtmlSelectorQualifiedNameAst? name = simple.GetQualifiedName();
        if (name == null)
        {
            return false;
        }

        HtmlAttribute[] attributes = node.GetAttributes();
        int index = 0;
        while (index < attributes.Length)
        {
            if (!ConsumeWork(budget))
            {
                return false;
            }

            HtmlAttribute attribute = attributes[index];
            if (MatchesAttributeName(node, attribute, name)
                && HtmlSelectorAttributeMatcher.Matches(
                    attribute.GetValue(), simple.GetAttributeOperator(),
                    simple.GetAttributeValue(), simple.GetAttributeCase(),
                    node.GetNamespaceStorage(), attribute.GetNamespaceUri(),
                    attribute.GetLocalName()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool MatchesAttributeName(HtmlNode node,
        HtmlAttribute attribute, HtmlSelectorQualifiedNameAst? name)
    {
        if (name == null)
        {
            return false;
        }

        if (!MatchesAttributeNamespace(name.GetNamespaceKind(),
            name.GetResolvedNamespaceUri(),
            attribute.GetNamespaceUri()))
        {
            return false;
        }

        if (name.IsUniversal())
        {
            return true;
        }

        return node.GetNamespaceStorage() == HtmlNamespace.Html
            && attribute.GetNamespaceUri().Length == 0
            ? Ascii.EqualsIgnoreCase(attribute.GetLocalName().AsSpan(),
                name.GetLocalName().AsSpan())
            : attribute.GetLocalName() == name.GetLocalName();
    }

    internal static bool MatchesNamespace(HtmlSelectorNamespaceKind kind,
        string resolvedNamespaceUri, string namespaceUri)
    {
        switch (kind)
        {
            case HtmlSelectorNamespaceKind.Unspecified:
            case HtmlSelectorNamespaceKind.Any:
                return true;
            case HtmlSelectorNamespaceKind.None:
                return namespaceUri.Length == 0;
            case HtmlSelectorNamespaceKind.Prefix:
                return namespaceUri == resolvedNamespaceUri;
            default:
                return false;
        }
    }

    internal static bool MatchesAttributeNamespace(HtmlSelectorNamespaceKind kind,
        string resolvedNamespaceUri, string namespaceUri)
    {
        if (kind == HtmlSelectorNamespaceKind.Any)
        {
            return true;
        }

        if (kind == HtmlSelectorNamespaceKind.Unspecified
            || kind == HtmlSelectorNamespaceKind.None)
        {
            return namespaceUri.Length == 0;
        }

        if (kind == HtmlSelectorNamespaceKind.Prefix)
        {
            return namespaceUri == resolvedNamespaceUri;
        }

        return false;
    }

    internal static bool HasClass(string? classes, string expected)
    {
        if (classes == null || expected.Length == 0)
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

    internal static bool MatchesPseudoClass(HtmlNode node,
        HtmlSimpleSelectorAst simple, HtmlNode scopeRoot,
        HtmlBudget budget)
    {
        HtmlSelectorPseudoClassKind kind = simple.GetPseudoClassKind();
        if ((int)kind >= (int)HtmlSelectorPseudoClassKind.Root
            && (int)kind
                <= (int)HtmlSelectorPseudoClassKind.OnlyOfType)
        {
            return MatchesStructural(node, kind,
                simple.GetNthExpression(), scopeRoot, budget);
        }

        if (kind == HtmlSelectorPseudoClassKind.Is
            || kind == HtmlSelectorPseudoClassKind.Where
            || kind == HtmlSelectorPseudoClassKind.Not)
        {
            HtmlComplexSelectorAst[] arguments =
                simple.GetSelectorArgumentsStorage();
            int index = 0;
            while (index < arguments.Length)
            {
                bool argumentMatches = MatchesComplex(node,
                    arguments[index], scopeRoot, budget);
                if (budget.HasExceededLimit())
                {
                    return false;
                }

                if (argumentMatches)
                {
                    return kind != HtmlSelectorPseudoClassKind.Not;
                }

                index = index + 1;
            }

            return kind == HtmlSelectorPseudoClassKind.Not;
        }

        if (kind == HtmlSelectorPseudoClassKind.Has)
        {
            return MatchesHas(node,
                simple.GetRelativeSelectorArgumentsStorage(), budget);
        }

        if (HtmlSelectorMarkupMatcher.IsMarkupPseudoClass(kind))
        {
            return HtmlSelectorMarkupMatcher.MatchesStorage(node, simple,
                budget);
        }

        return false;
    }

    internal static bool MatchesStructural(HtmlNode node,
        HtmlSelectorPseudoClassKind kind, HtmlNthExpression? nthExpression,
        HtmlNode scopeRoot, HtmlBudget budget)
    {
        switch (kind)
        {
            case HtmlSelectorPseudoClassKind.Root:
                HtmlNode? parent = node.GetParent();
                return parent != null
                    && parent.GetKind() == HtmlNodeKind.Document;
            case HtmlSelectorPseudoClassKind.Scope:
                return scopeRoot == null
                    ? node.GetParent() != null
                        && node.GetParent()?.GetKind()
                            == HtmlNodeKind.Document
                    : scopeRoot.GetKind() == HtmlNodeKind.Element
                        ? node == scopeRoot
                        : scopeRoot.GetKind() == HtmlNodeKind.Document
                            && node.GetParent() == scopeRoot;
            case HtmlSelectorPseudoClassKind.Empty:
                return IsEmpty(node, budget);
            case HtmlSelectorPseudoClassKind.NthChild:
                return MatchesIndex(node, nthExpression, false, false,
                    budget);
            case HtmlSelectorPseudoClassKind.NthLastChild:
                return MatchesIndex(node, nthExpression, true, false,
                    budget);
            case HtmlSelectorPseudoClassKind.FirstChild:
                return !HasElementSibling(node, false, false, budget);
            case HtmlSelectorPseudoClassKind.LastChild:
                return !HasElementSibling(node, true, false, budget);
            case HtmlSelectorPseudoClassKind.OnlyChild:
                return !HasElementSibling(node, false, false, budget)
                    && !HasElementSibling(node, true, false, budget);
            case HtmlSelectorPseudoClassKind.NthOfType:
                return MatchesIndex(node, nthExpression, false, true,
                    budget);
            case HtmlSelectorPseudoClassKind.NthLastOfType:
                return MatchesIndex(node, nthExpression, true, true,
                    budget);
            case HtmlSelectorPseudoClassKind.FirstOfType:
                return !HasElementSibling(node, false, true, budget);
            case HtmlSelectorPseudoClassKind.LastOfType:
                return !HasElementSibling(node, true, true, budget);
            case HtmlSelectorPseudoClassKind.OnlyOfType:
                return !HasElementSibling(node, false, true, budget)
                    && !HasElementSibling(node, true, true, budget);
            default:
                return false;
        }
    }

    internal static bool IsEmpty(HtmlNode node, HtmlBudget budget)
    {
        HtmlNode? child = node.GetFirstChild();
        while (child != null)
        {
            if (!ConsumeWork(budget))
            {
                return false;
            }

            if (child.GetKind() == HtmlNodeKind.Element
                || child.GetKind() == HtmlNodeKind.Text
                    && HasNonWhitespace(child.GetData()))
            {
                return false;
            }

            child = child.GetNextSibling();
        }

        return true;
    }

    internal static bool MatchesIndex(HtmlNode node,
        HtmlNthExpression? expression, bool fromEnd, bool ofType,
        HtmlBudget budget)
    {
        if (expression == null)
        {
            throw new InvalidOperationException(
                "An nth selector requires an expression.");
        }

        int index = 1;
        HtmlNode? sibling = fromEnd
            ? node.GetNextSibling()
            : node.GetPreviousSibling();
        while (sibling != null)
        {
            if (!ConsumeWork(budget))
            {
                return false;
            }

            if (sibling.GetKind() == HtmlNodeKind.Element
                && (!ofType || HasSameType(node, sibling)))
            {
                index = index + 1;
            }

            sibling = fromEnd
                ? sibling.GetNextSibling()
                : sibling.GetPreviousSibling();
        }

        return expression.Matches(index);
    }

    internal static bool HasElementSibling(HtmlNode node, bool after,
        bool ofType, HtmlBudget budget)
    {
        HtmlNode? sibling = after
            ? node.GetNextSibling()
            : node.GetPreviousSibling();
        while (sibling != null)
        {
            if (!ConsumeWork(budget))
            {
                return false;
            }

            if (sibling.GetKind() == HtmlNodeKind.Element
                && (!ofType || HasSameType(node, sibling)))
            {
                return true;
            }

            sibling = after
                ? sibling.GetNextSibling()
                : sibling.GetPreviousSibling();
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

    internal static bool ConsumeWork(HtmlBudget budget)
    {
        return budget.TryConsume(HtmlLimitKind.HasWork, 1);
    }
}

internal class HtmlSelectorMatchFrame
{
    internal int compoundIndex;
    internal HtmlSelectorRelationCursor cursor;

    internal HtmlSelectorMatchFrame(int compoundIndex,
        HtmlSelectorRelationCursor cursor)
    {
        this.compoundIndex = compoundIndex;
        this.cursor = cursor;
    }

    internal int GetCompoundIndex()
    {
        return this.compoundIndex;
    }

    internal HtmlSelectorRelationCursor GetCursor()
    {
        return this.cursor;
    }
}

internal class HtmlSelectorRelationCursor
{
    internal HtmlNode source;
    internal HtmlSelectorCombinator combinator;
    internal bool forward;
    internal HtmlNode? current;
    internal bool finished;

    internal HtmlSelectorRelationCursor(HtmlNode source,
        HtmlSelectorCombinator combinator, bool forward)
    {
        this.source = source;
        this.combinator = combinator;
        this.forward = forward;
        this.current = null;
        this.finished = false;
    }

    internal HtmlNode? MoveNext(HtmlBudget budget)
    {
        while (!this.finished)
        {
            HtmlNode? next = this.GetNextNode();
            if (next == null)
            {
                this.finished = true;
                return null;
            }

            this.current = next;
            if (!budget.TryConsume(HtmlLimitKind.HasWork, 1))
            {
                this.finished = true;
                return null;
            }

            if (next.GetKind() == HtmlNodeKind.Element
                || next.GetKind() == HtmlNodeKind.Fragment)
            {
                return next;
            }
        }

        return null;
    }

    internal HtmlNode? GetNextNode()
    {
        if (this.forward)
        {
            return this.GetNextForwardNode();
        }

        return this.GetNextBackwardNode();
    }

    internal HtmlNode? GetNextForwardNode()
    {
        switch (this.combinator)
        {
            case HtmlSelectorCombinator.Descendant:
                return this.GetNextDescendant();
            case HtmlSelectorCombinator.Child:
                return this.current == null
                    ? this.source.GetFirstChild()
                    : this.current.GetNextSibling();
            case HtmlSelectorCombinator.NextSibling:
                return this.GetNextAdjacentSibling();
            case HtmlSelectorCombinator.SubsequentSibling:
                return this.current == null
                    ? this.source.GetNextSibling()
                    : this.current.GetNextSibling();
            default:
                return null;
        }
    }

    internal HtmlNode? GetNextBackwardNode()
    {
        switch (this.combinator)
        {
            case HtmlSelectorCombinator.Descendant:
                return this.current == null
                    ? this.source.GetParent()
                    : this.current.GetParent();
            case HtmlSelectorCombinator.Child:
                if (this.current != null)
                {
                    return null;
                }

                return this.source.GetParent();
            case HtmlSelectorCombinator.NextSibling:
                return this.GetPreviousAdjacentSibling();
            case HtmlSelectorCombinator.SubsequentSibling:
                return this.current == null
                    ? this.source.GetPreviousSibling()
                    : this.current.GetPreviousSibling();
            default:
                return null;
        }
    }

    internal HtmlNode? GetNextDescendant()
    {
        if (this.current == null)
        {
            return this.source.GetFirstChild();
        }

        if (this.current.GetFirstChild() != null)
        {
            return this.current.GetFirstChild();
        }

        HtmlNode? node = this.current;
        while (node != this.source && node.GetNextSibling() == null)
        {
            HtmlNode? parent = node.GetParent();
            if (parent == null)
            {
                return null;
            }

            node = parent;
        }

        return node == this.source ? null : node.GetNextSibling();
    }

    internal HtmlNode? GetNextAdjacentSibling()
    {
        if (this.current != null
            && this.current.GetKind() == HtmlNodeKind.Element)
        {
            return null;
        }

        return this.current == null
            ? this.source.GetNextSibling()
            : this.current.GetNextSibling();
    }

    internal HtmlNode? GetPreviousAdjacentSibling()
    {
        if (this.current != null
            && this.current.GetKind() == HtmlNodeKind.Element)
        {
            return null;
        }

        return this.current == null
            ? this.source.GetPreviousSibling()
            : this.current.GetPreviousSibling();
    }
}
