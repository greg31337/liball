using System;
using System.Globalization;

namespace html;

internal class HtmlTextDirectionResult
{
    internal int direction;
    internal bool hasValue;

    internal HtmlTextDirectionResult(int directionValue, bool hasValueValue)
    {
        this.direction = directionValue;
        this.hasValue = hasValueValue;
    }

    internal int GetDirection()
    {
        return this.direction;
    }

    internal bool HasValue()
    {
        return this.hasValue;
    }
}

internal static class HtmlSelectorMarkupMatcher
{
    internal const string XmlNamespace =
        "http://www.w3.org/XML/1998/namespace";

    internal static bool Matches(HtmlNode node,
        HtmlSimpleSelectorAst pseudoClass, HtmlBudget budget)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (pseudoClass == null)
        {
            throw new ArgumentNullException("pseudoClass");
        }

        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        HtmlSelectorPseudoClassKind kind =
            pseudoClass.GetPseudoClassKind();
        if (pseudoClass.GetKind() != HtmlSelectorSimpleKind.PseudoClass
            || !IsMarkupPseudoClass(kind))
        {
            throw new ArgumentException(
                "A markup-observable pseudo-class is required.",
                "pseudoClass");
        }

        if (!ConsumeWork(budget))
        {
            return false;
        }

        bool result = MatchesStorage(node, pseudoClass, budget);
        return budget.HasExceededLimit() ? false : result;
    }

    internal static bool MatchesStorage(HtmlNode node,
        HtmlSimpleSelectorAst pseudoClass, HtmlBudget budget)
    {
        if (node.GetKind() != HtmlNodeKind.Element)
        {
            return false;
        }

        HtmlSelectorPseudoClassKind kind =
            pseudoClass.GetPseudoClassKind();
        switch (kind)
        {
            case HtmlSelectorPseudoClassKind.Lang:
                return MatchesLanguage(node,
                    pseudoClass.GetPseudoClassArgumentsStorage(), budget);
            case HtmlSelectorPseudoClassKind.Dir:
                return MatchesDirection(node,
                    pseudoClass.GetPseudoClassArgumentsStorage()[0],
                    budget);
            case HtmlSelectorPseudoClassKind.AnyLink:
            case HtmlSelectorPseudoClassKind.Link:
                return IsLink(node);
            case HtmlSelectorPseudoClassKind.Enabled:
                return IsDisableable(node)
                    && !IsActuallyDisabled(node, budget);
            case HtmlSelectorPseudoClassKind.Disabled:
                return IsDisableable(node)
                    && IsActuallyDisabled(node, budget);
            case HtmlSelectorPseudoClassKind.Checked:
                return IsChecked(node);
            case HtmlSelectorPseudoClassKind.Indeterminate:
                return IsIndeterminate(node, budget);
            case HtmlSelectorPseudoClassKind.Required:
                return SupportsRequired(node)
                    && node.HasAttribute("required");
            case HtmlSelectorPseudoClassKind.Optional:
                return SupportsRequired(node)
                    && !node.HasAttribute("required");
            case HtmlSelectorPseudoClassKind.ReadOnly:
                return IsHtml(node) && !IsReadWrite(node, budget);
            case HtmlSelectorPseudoClassKind.ReadWrite:
                return IsReadWrite(node, budget);
            case HtmlSelectorPseudoClassKind.PlaceholderShown:
                return IsPlaceholderShown(node, budget);
            case HtmlSelectorPseudoClassKind.Default:
                return IsDefault(node, budget);
            default:
                return false;
        }
    }

    internal static bool IsMarkupPseudoClass(
        HtmlSelectorPseudoClassKind kind)
    {
        return (int)kind >= (int)HtmlSelectorPseudoClassKind.Lang
            && (int)kind <= (int)HtmlSelectorPseudoClassKind.Default;
    }

    internal static bool MatchesLanguage(HtmlNode node, string[] ranges,
        HtmlBudget budget)
    {
        string? language = GetContentLanguage(node, budget);
        if (budget.HasExceededLimit())
        {
            return false;
        }

        int index = 0;
        while (index < ranges.Length)
        {
            if (MatchesLanguageRange(language, ranges[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string? GetContentLanguage(HtmlNode node, HtmlBudget budget)
    {
        HtmlNode? current = node;
        while (current != null)
        {
            if (!ConsumeWork(budget))
            {
                return null;
            }

            if (current.GetKind() == HtmlNodeKind.Element)
            {
                string? language = current.GetAttributeInNamespace(XmlNamespace,
                    "lang");
                if (language != null)
                {
                    return language;
                }

                language = current.GetAttribute("lang");
                if (language != null)
                {
                    return language;
                }
            }

            current = current.GetParent();
        }

        return null;
    }

    internal static bool MatchesLanguageRange(string? language, string range)
    {
        bool untagged = language == null || language.Length == 0;
        if (range.Length == 0)
        {
            return untagged;
        }

        if (untagged)
        {
            return false;
        }

        if (range == "*")
        {
            return true;
        }

        if (language == null)
        {
            return false;
        }

        string[] tagParts = language.Split('-');
        string[] rangeParts = range.Split('-');
        int tagIndex = 0;
        int rangeIndex = 0;
        if (rangeParts[0] != "*"
            && !Ascii.EqualsIgnoreCase(rangeParts[0].AsSpan(),
                tagParts[0].AsSpan()))
        {
            return false;
        }

        tagIndex = 1;
        rangeIndex = 1;
        while (rangeIndex < rangeParts.Length)
        {
            string part = rangeParts[rangeIndex];
            if (part == "*")
            {
                rangeIndex = rangeIndex + 1;
                continue;
            }

            bool found = false;
            while (tagIndex < tagParts.Length)
            {
                if (Ascii.EqualsIgnoreCase(part.AsSpan(),
                    tagParts[tagIndex].AsSpan()))
                {
                    found = true;
                    tagIndex = tagIndex + 1;
                    break;
                }

                if (tagParts[tagIndex].Length == 1)
                {
                    return false;
                }

                tagIndex = tagIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            rangeIndex = rangeIndex + 1;
        }

        return true;
    }

    internal static bool MatchesDirection(HtmlNode node, string expected,
        HtmlBudget budget)
    {
        if (!Ascii.EqualsIgnoreCase(expected.AsSpan(), "ltr".AsSpan())
            && !Ascii.EqualsIgnoreCase(expected.AsSpan(), "rtl".AsSpan()))
        {
            return false;
        }

        int direction = GetDirection(node, budget);
        return direction == 0
            ? Ascii.EqualsIgnoreCase(expected.AsSpan(), "ltr".AsSpan())
            : Ascii.EqualsIgnoreCase(expected.AsSpan(), "rtl".AsSpan());
    }

    internal static int GetDirection(HtmlNode node, HtmlBudget budget)
    {
        HtmlNode? current = node;
        while (current != null)
        {
            if (!ConsumeWork(budget))
            {
                return 0;
            }

            if (current.GetKind() == HtmlNodeKind.Element)
            {
                string? direction = IsHtml(current)
                    ? current.GetAttribute("dir")
                    : null;
                if (EqualsAscii(direction, "ltr"))
                {
                    return 0;
                }

                if (EqualsAscii(direction, "rtl"))
                {
                    return 1;
                }

                bool auto = EqualsAscii(direction, "auto")
                    || direction == null && IsHtmlElement(current, "bdi");
                if (auto)
                {
                    int resolved = GetAutoDirection(current, budget);
                    return resolved < 0 ? 0 : resolved;
                }

                if (direction == null && IsHtmlElement(current, "input")
                    && InputTypeIs(current, "tel"))
                {
                    return 0;
                }
            }

            current = current.GetParent();
        }

        return 0;
    }

    internal static int GetAutoDirection(HtmlNode node, HtmlBudget budget)
    {
        if (IsAutoDirectionFormElement(node))
        {
            bool hasValue;
            int direction;
            if (IsHtmlElement(node, "input"))
            {
                string value = node.GetAttribute("value") ?? "";
                hasValue = value.Length != 0;
                direction = FindStrongDirection(value);
            }
            else
            {
                HtmlTextDirectionResult result = FindTextValueDirection(node,
                    budget);
                direction = result.GetDirection();
                hasValue = result.HasValue();
            }

            if (direction >= 0)
            {
                return direction;
            }

            return hasValue ? 0 : -1;
        }

        HtmlNode? current = node.GetFirstChild();
        while (current != null)
        {
            if (!ConsumeWork(budget))
            {
                return -1;
            }

            bool descend = true;
            if (current.GetKind() == HtmlNodeKind.Text)
            {
                int direction = FindStrongDirection(current.GetData());
                if (direction >= 0)
                {
                    return direction;
                }
            }
            else if (current.GetKind() == HtmlNodeKind.Element
                && ExcludesAutoDirectionText(current))
            {
                descend = false;
            }

            current = GetNextDescendant(current, node, descend);
        }

        return -1;
    }

    internal static bool IsAutoDirectionFormElement(HtmlNode node)
    {
        if (IsHtmlElement(node, "textarea"))
        {
            return true;
        }

        return IsHtmlElement(node, "input")
            && (InputTypeIs(node, "hidden")
                || InputTypeIs(node, "text")
                || InputTypeIs(node, "search")
                || InputTypeIs(node, "tel")
                || InputTypeIs(node, "url")
                || InputTypeIs(node, "email")
                || InputTypeIs(node, "password")
                || InputTypeIs(node, "submit")
                || InputTypeIs(node, "reset")
                || InputTypeIs(node, "button"));
    }

    internal static bool ExcludesAutoDirectionText(HtmlNode node)
    {
        if (IsHtmlElement(node, "bdi") || IsHtmlElement(node, "script")
            || IsHtmlElement(node, "style")
            || IsHtmlElement(node, "textarea"))
        {
            return true;
        }

        if (!IsHtml(node))
        {
            return false;
        }

        string? direction = node.GetAttribute("dir");
        return EqualsAscii(direction, "ltr")
            || EqualsAscii(direction, "rtl")
            || EqualsAscii(direction, "auto");
    }

    internal static HtmlNode? GetNextDescendant(HtmlNode current, HtmlNode root,
        bool descend)
    {
        if (descend && current.GetFirstChild() != null)
        {
            return current.GetFirstChild();
        }

        HtmlNode? node = current;
        while (node != root && node.GetNextSibling() == null)
        {
            HtmlNode? parent = node.GetParent();
            if (parent == null)
            {
                return null;
            }

            node = parent;
        }

        return node == root ? null : node.GetNextSibling();
    }

    internal static int FindStrongDirection(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char first = value[index];
            bool pair = char.IsHighSurrogate(first)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]);
            int codePoint = pair
                ? char.ConvertToUtf32(first, value[index + 1])
                : first;
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(
                value, index);
            if (IsLetter(category))
            {
                return IsRtlCodePoint(codePoint) ? 1 : 0;
            }

            index = index + (pair ? 2 : 1);
        }

        return -1;
    }

    internal static bool IsLetter(UnicodeCategory category)
    {
        return category == UnicodeCategory.UppercaseLetter
            || category == UnicodeCategory.LowercaseLetter
            || category == UnicodeCategory.TitlecaseLetter
            || category == UnicodeCategory.ModifierLetter
            || category == UnicodeCategory.OtherLetter
            || category == UnicodeCategory.LetterNumber;
    }

    internal static bool IsRtlCodePoint(int value)
    {
        return value >= 0x0590 && value <= 0x08ff
            || value >= 0xfb1d && value <= 0xfdff
            || value >= 0xfe70 && value <= 0xfefc
            || value >= 0x10800 && value <= 0x10fff
            || value >= 0x1e800 && value <= 0x1eeff;
    }

    internal static bool IsLink(HtmlNode node)
    {
        if (IsHtmlElement(node, "a") || IsHtmlElement(node, "area"))
        {
            return node.HasAttribute("href");
        }

        if (node.GetNamespaceStorage() == HtmlNamespace.Svg
            && node.GetLocalName() == "a")
        {
            return node.GetAttribute("href") != null
                || node.GetAttributeInNamespace(HtmlNamespaceData.GetUri(
                    HtmlNamespace.XLink), "href") != null;
        }

        return false;
    }

    internal static bool IsDisableable(HtmlNode node)
    {
        return IsHtmlElement(node, "button")
            || IsHtmlElement(node, "input")
            || IsHtmlElement(node, "select")
            || IsHtmlElement(node, "textarea")
            || IsHtmlElement(node, "optgroup")
            || IsHtmlElement(node, "option")
            || IsHtmlElement(node, "fieldset");
    }

    internal static bool IsActuallyDisabled(HtmlNode node, HtmlBudget budget)
    {
        if (IsHtmlElement(node, "fieldset"))
        {
            return node.HasAttribute("disabled")
                || IsDisabledByFieldset(node, budget);
        }

        if (node.HasAttribute("disabled"))
        {
            return true;
        }

        if (IsHtmlElement(node, "optgroup")
            || IsHtmlElement(node, "option"))
        {
            if (IsHtmlElement(node, "option"))
            {
                HtmlNode? parent = node.GetParent();
                if (parent != null && IsHtmlElement(parent, "optgroup")
                    && parent.HasAttribute("disabled"))
                {
                    return true;
                }
            }

            HtmlNode? select = FindAncestor(node, "select", budget);
            return select != null && IsActuallyDisabled(select, budget);
        }

        if (IsHtmlElement(node, "button")
            || IsHtmlElement(node, "input")
            || IsHtmlElement(node, "select")
            || IsHtmlElement(node, "textarea"))
        {
            return IsDisabledByFieldset(node, budget);
        }

        return false;
    }

    internal static bool IsDisabledByFieldset(HtmlNode node, HtmlBudget budget)
    {
        HtmlNode? ancestor = node.GetParent();
        while (ancestor != null)
        {
            if (!ConsumeWork(budget))
            {
                return false;
            }

            if (IsHtmlElement(ancestor, "fieldset")
                && ancestor.HasAttribute("disabled"))
            {
                HtmlNode? legend = FindFirstElementChild(ancestor, "legend",
                    budget);
                if (legend == null
                    || !IsInclusiveDescendant(node, legend, budget))
                {
                    return true;
                }
            }

            ancestor = ancestor.GetParent();
        }

        return false;
    }

    internal static HtmlNode? FindFirstElementChild(HtmlNode parent, string name,
        HtmlBudget budget)
    {
        HtmlNode? child = parent.GetFirstChild();
        while (child != null)
        {
            if (!ConsumeWork(budget))
            {
                return null;
            }

            if (IsHtmlElement(child, name))
            {
                return child;
            }

            child = child.GetNextSibling();
        }

        return null;
    }

    internal static bool IsInclusiveDescendant(HtmlNode node, HtmlNode ancestor,
        HtmlBudget budget)
    {
        HtmlNode? current = node;
        while (current != null)
        {
            if (!ConsumeWork(budget))
            {
                return false;
            }

            if (current == ancestor)
            {
                return true;
            }

            current = current.GetParent();
        }

        return false;
    }

    internal static HtmlNode? FindAncestor(HtmlNode node, string name,
        HtmlBudget budget)
    {
        HtmlNode? current = node.GetParent();
        while (current != null)
        {
            if (!ConsumeWork(budget))
            {
                return null;
            }

            if (IsHtmlElement(current, name))
            {
                return current;
            }

            current = current.GetParent();
        }

        return null;
    }

    internal static bool IsChecked(HtmlNode node)
    {
        if (IsHtmlElement(node, "option"))
        {
            return node.HasAttribute("selected");
        }

        return IsHtmlElement(node, "input")
            && (InputTypeIs(node, "checkbox")
                || InputTypeIs(node, "radio"))
            && node.HasAttribute("checked");
    }

    internal static bool IsIndeterminate(HtmlNode node, HtmlBudget budget)
    {
        if (IsHtmlElement(node, "progress"))
        {
            return !node.HasAttribute("value");
        }

        if (!IsHtmlElement(node, "input")
            || !InputTypeIs(node, "radio") || node.HasAttribute("checked"))
        {
            return false;
        }

        string? name = node.GetAttribute("name");
        if (name == null || name.Length == 0)
        {
            return false;
        }

        HtmlNode root = GetTreeRoot(node, budget);
        if (budget.HasExceededLimit())
        {
            return false;
        }

        if (IsCheckedRadioInGroup(root, node, budget))
        {
            return false;
        }

        HtmlSelectorRelationCursor cursor = new HtmlSelectorRelationCursor(
            root, HtmlSelectorCombinator.Descendant, true);
        HtmlNode? candidate = cursor.MoveNext(budget);
        while (candidate != null)
        {
            if (IsCheckedRadioInGroup(candidate, node, budget))
            {
                return false;
            }

            if (budget.HasExceededLimit())
            {
                return false;
            }

            candidate = cursor.MoveNext(budget);
        }

        return !budget.HasExceededLimit();
    }

    internal static bool IsCheckedRadioInGroup(HtmlNode candidate,
        HtmlNode subject, HtmlBudget budget)
    {
        if (!IsHtmlElement(candidate, "input")
            || !InputTypeIs(candidate, "radio")
            || !candidate.HasAttribute("checked")
            || candidate.GetAttribute("name")
                != subject.GetAttribute("name"))
        {
            return false;
        }

        HtmlNode? candidateOwner = GetFormOwner(candidate, budget);
        HtmlNode? subjectOwner = GetFormOwner(subject, budget);
        return !budget.HasExceededLimit() && candidateOwner == subjectOwner;
    }

    internal static bool SupportsRequired(HtmlNode node)
    {
        if (IsHtmlElement(node, "select")
            || IsHtmlElement(node, "textarea"))
        {
            return true;
        }

        if (!IsHtmlElement(node, "input"))
        {
            return false;
        }

        return !InputTypeIs(node, "hidden")
            && !InputTypeIs(node, "range")
            && !InputTypeIs(node, "color")
            && !InputTypeIs(node, "button")
            && !InputTypeIs(node, "submit")
            && !InputTypeIs(node, "reset")
            && !InputTypeIs(node, "image");
    }

    internal static bool IsReadWrite(HtmlNode node, HtmlBudget budget)
    {
        if (IsHtmlElement(node, "input"))
        {
            return SupportsReadOnly(node) && !node.HasAttribute("readonly")
                && !IsActuallyDisabled(node, budget);
        }

        if (IsHtmlElement(node, "textarea"))
        {
            return !node.HasAttribute("readonly")
                && !IsActuallyDisabled(node, budget);
        }

        if (!IsHtml(node))
        {
            return false;
        }

        return IsContentEditable(node, budget);
    }

    internal static bool SupportsReadOnly(HtmlNode node)
    {
        return InputTypeIs(node, "text")
            || InputTypeIs(node, "search")
            || InputTypeIs(node, "tel")
            || InputTypeIs(node, "url")
            || InputTypeIs(node, "email")
            || InputTypeIs(node, "password")
            || InputTypeIs(node, "date")
            || InputTypeIs(node, "month")
            || InputTypeIs(node, "week")
            || InputTypeIs(node, "time")
            || InputTypeIs(node, "datetime-local")
            || InputTypeIs(node, "number");
    }

    internal static bool IsContentEditable(HtmlNode node, HtmlBudget budget)
    {
        HtmlNode? current = node;
        while (current != null)
        {
            if (!ConsumeWork(budget))
            {
                return false;
            }

            if (IsHtml(current))
            {
                string? value = current.GetAttribute("contenteditable");
                if (value != null)
                {
                    if (value.Length == 0 || EqualsAscii(value, "true")
                        || EqualsAscii(value, "plaintext-only"))
                    {
                        return true;
                    }

                    if (EqualsAscii(value, "false"))
                    {
                        return false;
                    }
                }
            }

            current = current.GetParent();
        }

        return false;
    }

    internal static bool IsPlaceholderShown(HtmlNode node, HtmlBudget budget)
    {
        if (!node.HasAttribute("placeholder"))
        {
            return false;
        }

        if (IsHtmlElement(node, "textarea"))
        {
            return !HasTextValue(node, budget);
        }

        return IsHtmlElement(node, "input")
            && SupportsPlaceholder(node)
            && (node.GetAttribute("value") ?? "").Length == 0;
    }

    internal static bool SupportsPlaceholder(HtmlNode node)
    {
        return InputTypeIs(node, "text")
            || InputTypeIs(node, "search")
            || InputTypeIs(node, "tel")
            || InputTypeIs(node, "url")
            || InputTypeIs(node, "email")
            || InputTypeIs(node, "password");
    }

    internal static bool HasTextValue(HtmlNode node, HtmlBudget budget)
    {
        HtmlNode? current = node.GetFirstChild();
        while (current != null)
        {
            if (!ConsumeWork(budget))
            {
                return false;
            }

            if (current.GetKind() == HtmlNodeKind.Text
                && current.GetData().Length != 0)
            {
                return true;
            }

            current = GetNextDescendant(current, node, true);
        }

        return false;
    }

    internal static HtmlTextDirectionResult FindTextValueDirection(
        HtmlNode node, HtmlBudget budget)
    {
        bool hasValue = false;
        HtmlNode? current = node.GetFirstChild();
        while (current != null)
        {
            if (!ConsumeWork(budget))
            {
                return new HtmlTextDirectionResult(-1, hasValue);
            }

            if (current.GetKind() == HtmlNodeKind.Text)
            {
                string data = current.GetData();
                hasValue = hasValue || data.Length != 0;
                int direction = FindStrongDirection(data);
                if (direction >= 0)
                {
                    return new HtmlTextDirectionResult(direction, hasValue);
                }
            }

            current = GetNextDescendant(current, node, true);
        }

        return new HtmlTextDirectionResult(-1, hasValue);
    }

    internal static bool IsDefault(HtmlNode node, HtmlBudget budget)
    {
        if (IsHtmlElement(node, "option"))
        {
            return node.HasAttribute("selected");
        }

        if (IsHtmlElement(node, "input")
            && (InputTypeIs(node, "checkbox")
                || InputTypeIs(node, "radio")))
        {
            return node.HasAttribute("checked");
        }

        if (!IsSubmitButton(node))
        {
            return false;
        }

        HtmlNode? owner = GetFormOwner(node, budget);
        if (owner == null || budget.HasExceededLimit())
        {
            return false;
        }

        HtmlNode root = GetTreeRoot(node, budget);
        if (IsSubmitButton(root)
            && GetFormOwner(root, budget) == owner)
        {
            return root == node;
        }

        HtmlSelectorRelationCursor cursor = new HtmlSelectorRelationCursor(
            root, HtmlSelectorCombinator.Descendant, true);
        HtmlNode? candidate = cursor.MoveNext(budget);
        while (candidate != null)
        {
            if (IsSubmitButton(candidate)
                && GetFormOwner(candidate, budget) == owner)
            {
                return candidate == node;
            }

            if (budget.HasExceededLimit())
            {
                return false;
            }

            candidate = cursor.MoveNext(budget);
        }

        return false;
    }

    internal static bool IsSubmitButton(HtmlNode node)
    {
        if (IsHtmlElement(node, "input"))
        {
            return InputTypeIs(node, "submit");
        }

        if (!IsHtmlElement(node, "button"))
        {
            return false;
        }

        string? type = node.GetAttribute("type");
        if (EqualsAscii(type, "submit"))
        {
            return true;
        }

        bool auto = type == null || !EqualsAscii(type, "reset")
            && !EqualsAscii(type, "button");
        return auto && !node.HasAttribute("command")
            && !node.HasAttribute("commandfor")
            && !IsHtmlElement(node.GetParent(), "select");
    }

    internal static HtmlNode? GetFormOwner(HtmlNode node, HtmlBudget budget)
    {
        string? formId = node.GetAttribute("form");
        if (formId != null)
        {
            if (formId.Length == 0)
            {
                return null;
            }

            HtmlNode root = GetTreeRoot(node, budget);
            if (IsHtmlElement(root, "form")
                && root.GetAttribute("id") == formId)
            {
                return root;
            }

            HtmlSelectorRelationCursor cursor =
                new HtmlSelectorRelationCursor(root,
                    HtmlSelectorCombinator.Descendant, true);
            HtmlNode? candidate = cursor.MoveNext(budget);
            while (candidate != null)
            {
                if (IsHtmlElement(candidate, "form")
                    && candidate.GetAttribute("id") == formId)
                {
                    return candidate;
                }

                candidate = cursor.MoveNext(budget);
            }

            return null;
        }

        return FindAncestor(node, "form", budget);
    }

    internal static HtmlNode GetTreeRoot(HtmlNode node, HtmlBudget budget)
    {
        HtmlNode current = node;
        HtmlNode? parent = current.GetParent();
        while (parent != null)
        {
            if (!ConsumeWork(budget))
            {
                return current;
            }

            current = parent;
            parent = current.GetParent();
        }

        return current;
    }

    internal static bool InputTypeIs(HtmlNode node, string expected)
    {
        string? type = node.GetAttribute("type");
        if (type == null || type.Length == 0)
        {
            return expected == "text";
        }

        if (expected == "text" && !IsKnownInputType(type))
        {
            return true;
        }

        return EqualsAscii(type, expected);
    }

    internal static bool IsKnownInputType(string value)
    {
        return EqualsAscii(value, "hidden")
            || EqualsAscii(value, "text")
            || EqualsAscii(value, "search")
            || EqualsAscii(value, "tel")
            || EqualsAscii(value, "url")
            || EqualsAscii(value, "email")
            || EqualsAscii(value, "password")
            || EqualsAscii(value, "date")
            || EqualsAscii(value, "month")
            || EqualsAscii(value, "week")
            || EqualsAscii(value, "time")
            || EqualsAscii(value, "datetime-local")
            || EqualsAscii(value, "number")
            || EqualsAscii(value, "range")
            || EqualsAscii(value, "color")
            || EqualsAscii(value, "checkbox")
            || EqualsAscii(value, "radio")
            || EqualsAscii(value, "file")
            || EqualsAscii(value, "submit")
            || EqualsAscii(value, "image")
            || EqualsAscii(value, "reset")
            || EqualsAscii(value, "button");
    }

    internal static bool IsHtml(HtmlNode? node)
    {
        return node != null && node.GetKind() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html;
    }

    internal static bool IsHtmlElement(HtmlNode? node, string localName)
    {
        return node != null && IsHtml(node)
            && node.GetLocalName() == localName;
    }

    internal static bool EqualsAscii(string? value, string expected)
    {
        return value != null && Ascii.EqualsIgnoreCase(value.AsSpan(),
            expected.AsSpan());
    }

    internal static bool ConsumeWork(HtmlBudget budget)
    {
        return budget.TryConsume(HtmlLimitKind.HasWork, 1);
    }
}
