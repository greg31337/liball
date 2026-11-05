using System;

namespace html;

internal static class HtmlPrettyPrinter
{
    internal static bool ShouldFormatChildren(HtmlNode node,
        HtmlNode[] children)
    {
        if (children.Length == 0 || IsSensitiveContainer(node))
        {
            return false;
        }

        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            HtmlNodeKind kind = child.GetKindStorage();
            if (kind == HtmlNodeKind.Text
                || kind == HtmlNodeKind.Document
                || kind == HtmlNodeKind.Fragment)
            {
                return false;
            }

            if (kind == HtmlNodeKind.Element
                && !IsBlockLikeHtmlElement(child))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsSensitiveContainer(HtmlNode node)
    {
        if (node.GetKindStorage() != HtmlNodeKind.Element)
        {
            return false;
        }

        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return true;
        }

        string name = FoldAscii(node.GetLocalNameStorage());
        return HtmlNames.IsPhrasingElement(name.AsSpan())
            || HtmlNames.IsPreformattedElement(name.AsSpan())
            || HtmlNames.IsRawTextElement(name.AsSpan())
            || HtmlNames.IsEscapableRawTextElement(name.AsSpan())
            || HasPhrasingOnlyContent(name);
    }

    internal static bool IsBlockLikeHtmlElement(HtmlNode node)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string name = FoldAscii(node.GetLocalNameStorage());
        return !HtmlNames.IsPhrasingElement(name.AsSpan());
    }

    internal static bool HasPhrasingOnlyContent(string name)
    {
        return name == "p" || name == "h1" || name == "h2" || name == "h3"
            || name == "h4" || name == "h5" || name == "h6"
            || name == "legend" || name == "option" || name == "summary";
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
