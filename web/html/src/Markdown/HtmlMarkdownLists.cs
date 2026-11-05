using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;

internal class HtmlMarkdownListPart
{
    internal string text;
    internal bool nestedList;

    internal HtmlMarkdownListPart(string textValue, bool nestedListValue)
    {
        if (textValue == null)
        {
            throw new ArgumentNullException("textValue");
        }

        this.text = textValue;
        this.nestedList = nestedListValue;
    }

    internal string GetText()
    {
        return this.text;
    }

    internal bool IsNestedList()
    {
        return this.nestedList;
    }
}

internal class HtmlMarkdownListItemContent
{
    internal string text;
    internal bool startsWithNestedList;

    internal HtmlMarkdownListItemContent(string textValue,
        bool startsWithNestedListValue)
    {
        if (textValue == null)
        {
            throw new ArgumentNullException("textValue");
        }

        this.text = textValue;
        this.startsWithNestedList = startsWithNestedListValue;
    }

    internal string GetText()
    {
        return this.text;
    }

    internal bool StartsWithNestedList()
    {
        return this.startsWithNestedList;
    }
}

internal static class HtmlMarkdownLists
{
    internal static string Render(HtmlNode list,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, HtmlMarkdownLinks links)
    {
        if (list == null)
        {
            throw new ArgumentNullException("list");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (links == null)
        {
            throw new ArgumentNullException("links");
        }

        bool ordered = IsHtmlElement(list, "ol");
        bool reversed = ordered && list.HasAttribute("reversed");
        HtmlNode[] items = GetOwnedItems(list);
        int[] ordinals = ordered
            ? HtmlNumbers.GetOrderedListOrdinals(list) : new int[0];
        bool loose = IsLoose(items);
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < items.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string marker = ordered
                ? GetOrderedMarker(ordinals, index,
                    options.GetOrderedListNumbering(), reversed)
                : options.GetUnorderedListMarker();
            HtmlMarkdownListItemContent content = RenderItem(items[index],
                options, cancellationToken, loose, links);
            AppendItem(builder, marker, content, options.GetNewLine());
            if (index + 1 < items.Length)
            {
                builder.Append(options.GetNewLine());
                if (loose)
                {
                    builder.Append(options.GetNewLine());
                }
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static HtmlMarkdownListItemContent RenderItem(HtmlNode item,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, bool loose,
        HtmlMarkdownLinks links)
    {
        List<HtmlMarkdownListPart> parts =
            new List<HtmlMarkdownListPart>();
        HtmlMarkdownWriter segment = HtmlMarkdownWriter.Create(options,
            cancellationToken, links);
        HtmlNode[] children = item.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (IsList(child))
            {
                AddSegment(parts, segment.Complete());
                segment = HtmlMarkdownWriter.Create(options,
                    cancellationToken, links);
                string nested = Render(child, options, cancellationToken,
                    links);
                if (nested.Length > 0)
                {
                    parts.Add(new HtmlMarkdownListPart(nested, true));
                }
            }
            else
            {
                segment.WriteNode(child);
            }

            index = index + 1;
        }

        AddSegment(parts, segment.Complete());
        string separator = loose
            ? options.GetNewLine() + options.GetNewLine()
            : options.GetNewLine();
        StringBuilder builder = new StringBuilder();
        index = 0;
        while (index < parts.Count)
        {
            if (builder.Length > 0)
            {
                builder.Append(separator);
            }

            builder.Append(parts[index].GetText());
            index = index + 1;
        }

        bool startsNested = parts.Count > 0
            && parts[0].IsNestedList();
        string text = builder.ToString();
        HtmlNode? checkbox = options.GetEnableTaskLists()
            ? FindLeadingTaskCheckbox(item) : null;
        if (checkbox != null)
        {
            string task = checkbox.HasAttribute("checked") ? "[x]" : "[ ]";
            text = text.Length == 0 ? task : task + " " + text;
            startsNested = false;
        }

        return new HtmlMarkdownListItemContent(text, startsNested);
    }

    internal static void AddSegment(List<HtmlMarkdownListPart> parts, string text)
    {
        if (text.Length > 0)
        {
            parts.Add(new HtmlMarkdownListPart(text, false));
        }
    }

    internal static void AppendItem(StringBuilder builder, string marker,
        HtmlMarkdownListItemContent content, string newLine)
    {
        builder.Append(marker);
        string text = content.GetText();
        if (text.Length == 0)
        {
            return;
        }

        string continuation = CreateIndent(marker.Length + 1);
        if (content.StartsWithNestedList())
        {
            builder.Append(newLine);
            builder.Append(continuation);
        }
        else
        {
            builder.Append(' ');
        }

        AppendContinuationText(builder, text, continuation, newLine);
    }

    internal static void AppendContinuationText(StringBuilder builder, string text,
        string continuation, string newLine)
    {
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '\r' || current == '\n')
            {
                if (current == '\r' && index + 1 < text.Length
                    && text[index + 1] == '\n')
                {
                    index = index + 1;
                }

                builder.Append(newLine);
                int next = index + 1;
                if (next < text.Length && text[next] != '\r'
                    && text[next] != '\n')
                {
                    builder.Append(continuation);
                }
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }
    }

    internal static string GetOrderedMarker(int[] ordinals, int index,
        HtmlMarkdownOrderedListNumbering numbering, bool reversed)
    {
        long number;
        if (numbering == HtmlMarkdownOrderedListNumbering.One)
        {
            number = 1;
        }
        else if (numbering == HtmlMarkdownOrderedListNumbering.Sequential)
        {
            number = ordinals.Length == 0 ? 1
                : (long)ordinals[0] + (reversed ? -index : index);
        }
        else
        {
            number = index < ordinals.Length ? ordinals[index] : index + 1;
        }

        if (number < 0 || number > 999999999)
        {
            number = 1;
        }

        return number.ToString(CultureInfo.InvariantCulture) + ".";
    }

    internal static HtmlNode[] GetOwnedItems(HtmlNode list)
    {
        HtmlNode[] children = list.GetChildrenStorage();
        List<HtmlNode> items = new List<HtmlNode>();
        int index = 0;
        while (index < children.Length)
        {
            if (IsHtmlElement(children[index], "li"))
            {
                items.Add(children[index]);
            }

            index = index + 1;
        }

        return items.ToArray();
    }

    internal static bool IsLoose(HtmlNode[] items)
    {
        int itemIndex = 0;
        while (itemIndex < items.Length)
        {
            HtmlNode[] children = items[itemIndex].GetChildrenStorage();
            int blockCount = 0;
            int index = 0;
            while (index < children.Length)
            {
                HtmlNode child = children[index];
                if (IsHtmlElement(child, "p"))
                {
                    return true;
                }

                if (child.GetKindStorage() == HtmlNodeKind.Element
                    && child.GetNamespaceStorage() == HtmlNamespace.Html
                    && IsItemBlock(child.GetLocalNameStorage()))
                {
                    blockCount = blockCount + 1;
                    if (blockCount > 1)
                    {
                        return true;
                    }
                }

                index = index + 1;
            }

            itemIndex = itemIndex + 1;
        }

        return false;
    }

    internal static bool IsItemBlock(string name)
    {
        return name == "address" || name == "article" || name == "aside"
            || name == "blockquote" || name == "details" || name == "div"
            || name == "dl" || name == "figure" || name == "form"
            || name == "h1" || name == "h2" || name == "h3"
            || name == "h4" || name == "h5" || name == "h6"
            || name == "hr" || name == "pre" || name == "section"
            || name == "table";
    }

    internal static HtmlNode? FindLeadingTaskCheckbox(HtmlNode item)
    {
        HtmlNode[] children = item.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child.GetKindStorage() == HtmlNodeKind.Text)
            {
                if (HtmlTextContent.Normalize(child.GetData()).Length > 0)
                {
                    return null;
                }
            }
            else if (IsCheckbox(child))
            {
                return child;
            }
            else if (child.GetKindStorage() == HtmlNodeKind.Element)
            {
                if (IsList(child))
                {
                    return null;
                }

                HtmlNode? nested = FindLeadingTaskCheckbox(child);
                if (nested != null)
                {
                    return nested;
                }

                if (HtmlTextContent.Normalize(
                    HtmlTextContent.GetDescendantText(child)).Length > 0)
                {
                    return null;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool IsCheckbox(HtmlNode node)
    {
        if (!IsHtmlElement(node, "input"))
        {
            return false;
        }

        string? type = node.GetAttribute("type");
        if (type == null)
        {
            return false;
        }

        int start = 0;
        while (start < type.Length && Ascii.IsHtmlWhitespace(type[start]))
        {
            start = start + 1;
        }

        int end = type.Length;
        while (end > start && Ascii.IsHtmlWhitespace(type[end - 1]))
        {
            end = end - 1;
        }

        return Ascii.EqualsIgnoreCase(type.AsSpan(start, end - start),
            "checkbox".AsSpan());
    }

    internal static bool IsList(HtmlNode node)
    {
        return IsHtmlElement(node, "ol") || IsHtmlElement(node, "ul")
            || IsHtmlElement(node, "menu");
    }

    internal static bool IsHtmlElement(HtmlNode node, string name)
    {
        return node.GetKindStorage() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == name;
    }

    internal static string CreateIndent(int count)
    {
        StringBuilder builder = new StringBuilder(count);
        int index = 0;
        while (index < count)
        {
            builder.Append(' ');
            index = index + 1;
        }

        return builder.ToString();
    }
}
