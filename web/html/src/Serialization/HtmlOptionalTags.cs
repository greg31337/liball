namespace html;

internal static class HtmlOptionalTags
{
    internal static readonly string[] paragraphFollowingNames =
    [
        "address", "article", "aside", "blockquote", "details", "dialog",
        "div", "dl", "fieldset", "figcaption", "figure", "footer", "form",
        "h1", "h2", "h3", "h4", "h5", "h6", "header", "hgroup", "hr",
        "main", "menu", "nav", "ol", "p", "pre", "search", "section",
        "table", "ul",
    ];

    internal static readonly string[] paragraphParentExceptions =
    [
        "a", "audio", "del", "ins", "map", "noscript", "video",
    ];

    internal static readonly string[] bodyStartExceptions =
    [
        "link", "meta", "noscript", "script", "style", "template",
    ];

    internal static bool CanOmitStartTag(HtmlNode node)
    {
        if (!IsHtmlElement(node) || node.GetAttributesStorage().Length != 0)
        {
            return false;
        }

        string name = GetName(node);
        if (name == "html")
        {
            HtmlNode? parent = node.GetParentStorage();
            return parent != null
                && parent.GetKindStorage()
                    == HtmlNodeKind.Document
                && FirstChildIsNotComment(node);
        }

        if (name == "head")
        {
            return IsHtmlParent(node, "html")
                && HeadStartCanBeOmitted(node);
        }

        if (name == "body")
        {
            return IsHtmlParent(node, "html")
                && IsHtmlElement(node.GetPreviousSiblingStorage(), "head")
                && BodyStartCanBeOmitted(node);
        }

        if (name == "colgroup")
        {
            return IsHtmlParent(node, "table")
                && TableSectionStartCanBeOmitted(node, "col", false);
        }

        if (name == "tbody")
        {
            return IsHtmlParent(node, "table")
                && TableSectionStartCanBeOmitted(node, "tr", true);
        }

        return false;
    }

    internal static bool CanOmitEndTag(HtmlNode node)
    {
        if (!IsHtmlElement(node))
        {
            return false;
        }

        string name = GetName(node);
        HtmlNode? next = node.GetNextSiblingStorage();
        if (name == "html")
        {
            HtmlNode? parent = node.GetParentStorage();
            return parent != null
                && parent.GetKindStorage()
                    == HtmlNodeKind.Document
                && !IsComment(next);
        }

        if (name == "head")
        {
            return IsHtmlParent(node, "html")
                && !StartsWithWhitespaceOrComment(next);
        }

        if (name == "colgroup" || name == "caption")
        {
            return IsHtmlParent(node, "table")
                && !StartsWithWhitespaceOrComment(next);
        }

        if (name == "body")
        {
            return IsHtmlParent(node, "html") && !IsComment(next);
        }

        if (name == "li")
        {
            return IsHtmlListParent(node)
                && (IsHtmlElement(next, "li") || next == null);
        }

        if (name == "dt")
        {
            return IsHtmlParent(node, "dl")
                && (IsHtmlElement(next, "dt")
                    || IsHtmlElement(next, "dd"));
        }

        if (name == "dd")
        {
            return IsHtmlParent(node, "dl")
                && (IsHtmlElement(next, "dd")
                    || IsHtmlElement(next, "dt") || next == null);
        }

        if (name == "p")
        {
            return ParagraphEndCanBeOmitted(node, next);
        }

        if (name == "rt" || name == "rp")
        {
            return IsHtmlParent(node, "ruby")
                && (IsHtmlElement(next, "rt")
                    || IsHtmlElement(next, "rp") || next == null);
        }

        if (name == "optgroup")
        {
            return IsHtmlParent(node, "select")
                && (IsHtmlElement(next, "optgroup")
                    || IsHtmlElement(next, "hr") || next == null);
        }

        if (name == "option")
        {
            return IsHtmlOptionParent(node)
                && (IsHtmlElement(next, "option")
                    || IsHtmlElement(next, "optgroup")
                    || IsHtmlElement(next, "hr") || next == null);
        }

        if (name == "thead")
        {
            return IsHtmlParent(node, "table")
                && (IsHtmlElement(next, "tbody")
                    || IsHtmlElement(next, "tfoot"));
        }

        if (name == "tbody")
        {
            return IsHtmlParent(node, "table")
                && (IsHtmlElement(next, "tbody")
                    || IsHtmlElement(next, "tfoot") || next == null);
        }

        if (name == "tfoot")
        {
            return next == null && IsHtmlParent(node, "table");
        }

        if (name == "tr")
        {
            return IsHtmlRowGroupParent(node)
                && (IsHtmlElement(next, "tr") || next == null);
        }

        if (name == "td" || name == "th")
        {
            return IsHtmlParent(node, "tr")
                && (IsHtmlElement(next, "td")
                    || IsHtmlElement(next, "th") || next == null);
        }

        return false;
    }

    internal static bool FirstChildIsNotComment(HtmlNode node)
    {
        HtmlNode? first = node.GetFirstChildStorage();
        return first == null || first.GetKindStorage() != HtmlNodeKind.Comment;
    }

    internal static bool HeadStartCanBeOmitted(HtmlNode node)
    {
        HtmlNode? first = node.GetFirstChildStorage();
        return first == null || first.GetKindStorage() == HtmlNodeKind.Element;
    }

    internal static bool BodyStartCanBeOmitted(HtmlNode node)
    {
        HtmlNode? first = node.GetFirstChildStorage();
        if (first == null)
        {
            return true;
        }

        if (first.GetKindStorage() == HtmlNodeKind.Comment)
        {
            return false;
        }

        if (first.GetKindStorage() == HtmlNodeKind.Text)
        {
            string data = first.GetDataStorage();
            return data.Length > 0 && !Ascii.IsHtmlWhitespace(data[0]);
        }

        if (first.GetKindStorage() == HtmlNodeKind.Element)
        {
            return !Contains(bodyStartExceptions, GetName(first));
        }

        return true;
    }

    internal static bool TableSectionStartCanBeOmitted(HtmlNode node,
        string firstChildName, bool rowGroup)
    {
        if (!IsHtmlElement(node.GetFirstChildStorage(), firstChildName))
        {
            return false;
        }

        HtmlNode? previous = node.GetPreviousSiblingStorage();
        if (previous == null || !CanOmitEndTag(previous))
        {
            return true;
        }

        string previousName = IsHtmlElement(previous) ? GetName(previous) : "";
        if (!rowGroup)
        {
            return previousName != "colgroup";
        }

        return previousName != "tbody" && previousName != "thead"
            && previousName != "tfoot";
    }

    internal static bool ParagraphEndCanBeOmitted(HtmlNode node, HtmlNode? next)
    {
        HtmlNode? parent = node.GetParentStorage();
        if (!IsHtmlElement(parent))
        {
            return false;
        }

        if (next != null)
        {
            return IsHtmlElement(next)
                && Contains(paragraphFollowingNames, GetName(next));
        }

        return !Contains(paragraphParentExceptions, GetName(parent));
    }

    internal static bool StartsWithWhitespaceOrComment(HtmlNode? node)
    {
        if (IsComment(node))
        {
            return true;
        }

        if (node == null || node.GetKindStorage() != HtmlNodeKind.Text)
        {
            return false;
        }

        string data = node.GetDataStorage();
        return data.Length == 0 || Ascii.IsHtmlWhitespace(data[0]);
    }

    internal static bool IsComment(HtmlNode? node)
    {
        return node != null
            && node.GetKindStorage() == HtmlNodeKind.Comment;
    }

    internal static bool IsHtmlListParent(HtmlNode node)
    {
        HtmlNode? parent = node.GetParentStorage();
        return IsHtmlElement(parent, "ol") || IsHtmlElement(parent, "ul")
            || IsHtmlElement(parent, "menu");
    }

    internal static bool IsHtmlOptionParent(HtmlNode node)
    {
        HtmlNode? parent = node.GetParentStorage();
        return IsHtmlElement(parent, "select")
            || IsHtmlElement(parent, "datalist")
            || IsHtmlElement(parent, "optgroup");
    }

    internal static bool IsHtmlRowGroupParent(HtmlNode node)
    {
        HtmlNode? parent = node.GetParentStorage();
        return IsHtmlElement(parent, "thead")
            || IsHtmlElement(parent, "tbody")
            || IsHtmlElement(parent, "tfoot");
    }

    internal static bool IsHtmlParent(HtmlNode node, string parentName)
    {
        return node != null
            && IsHtmlElement(node.GetParentStorage(), parentName);
    }

    internal static bool IsHtmlElement(HtmlNode? node)
    {
        return node != null
            && node.GetKindStorage() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html;
    }

    internal static bool IsHtmlElement(HtmlNode? node, string name)
    {
        return IsHtmlElement(node) && GetName(node) == name;
    }

    internal static string GetName(HtmlNode? node)
    {
        if (node == null)
        {
            return "";
        }

        return FoldAscii(node.GetLocalNameStorage());
    }

    internal static bool Contains(string[] values, string value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string FoldAscii(string value)
    {
        char[] result = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        return new string(result);
    }
}
