using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;

internal static class HtmlMarkdownExtensions
{
    internal static string RenderDefinitionList(HtmlNode list,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, HtmlMarkdownLinks links)
    {
        ValidateHtmlElement(list, "dl");
        ValidateOptionsAndLinks(options, links);
        StringBuilder builder = new StringBuilder();
        HtmlNode[] children = list.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlNode child = children[index];
            bool term = IsHtmlElement(child, "dt");
            bool definition = IsHtmlElement(child, "dd");
            if ((term || definition) && !IsHiddenOrIgnored(child))
            {
                string content = RenderNode(child, options,
                    cancellationToken, links);
                if (term)
                {
                    content = HtmlTextContent.Normalize(content);
                }

                if (content.Length > 0)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(options.GetNewLine());
                    }

                    if (definition)
                    {
                        AppendPrefixed(builder, content, ": ", "  ",
                            options.GetNewLine());
                    }
                    else
                    {
                        builder.Append(content);
                    }
                }
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string RenderFootnoteContainer(HtmlNode container,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, HtmlMarkdownLinks links)
    {
        if (container == null)
        {
            throw new ArgumentNullException("container");
        }

        ValidateOptionsAndLinks(options, links);
        List<HtmlNode> definitions = new List<HtmlNode>();
        CollectFootnoteDefinitions(container, definitions, true,
            cancellationToken);
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < definitions.Count)
        {
            string rendered = RenderFootnoteDefinition(definitions[index],
                options, cancellationToken, links);
            if (rendered.Length > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Append(options.GetNewLine());
                    builder.Append(options.GetNewLine());
                }

                builder.Append(rendered);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string RenderFootnoteDefinition(HtmlNode definition,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, HtmlMarkdownLinks links)
    {
        if (definition == null)
        {
            throw new ArgumentNullException("definition");
        }

        ValidateOptionsAndLinks(options, links);
        string identifier = GetFootnoteIdentifier(
            definition.GetAttribute("id"));
        if (identifier.Length == 0)
        {
            return "";
        }

        HtmlMarkdownWriter writer = HtmlMarkdownWriter.Create(options,
            cancellationToken, links);
        HtmlNode[] children = definition.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            writer.WriteNode(children[index]);
            index = index + 1;
        }

        string content = writer.Complete();
        if (content.Length == 0)
        {
            return "[^" + identifier + "]:";
        }

        StringBuilder builder = new StringBuilder();
        AppendPrefixed(builder, content, "[^" + identifier + "]: ",
            "    ", options.GetNewLine());
        return builder.ToString();
    }

    internal static string GetFootnoteReferenceIdentifier(HtmlNode node)
    {
        if (!IsHtmlElement(node, "a") || !HasRole(node, "doc-noteref"))
        {
            return "";
        }

        string? href = node.GetAttribute("href");
        if (href == null || href.Length < 2 || href[0] != '#')
        {
            return "";
        }

        return GetFootnoteIdentifier(href.Substring(1));
    }

    internal static bool IsFootnoteBacklink(HtmlNode node)
    {
        return IsHtmlElement(node, "a")
            && HasRole(node, "doc-backlink");
    }

    internal static bool IsFootnoteContainer(HtmlNode node)
    {
        return node != null && HasRole(node, "doc-endnotes");
    }

    internal static bool IsFootnoteDefinition(HtmlNode node)
    {
        return node != null && (HasRole(node, "doc-endnote")
            || HasRole(node, "doc-footnote"));
    }

    internal static bool IsFootnoteDefinitionList(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (!IsHtmlElement(node, "ol") && !IsHtmlElement(node, "ul"))
        {
            return false;
        }

        HtmlNode[] children = node.GetChildrenStorage();
        bool found = false;
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (IsHtmlElement(child, "li") && !IsHiddenOrIgnored(child))
            {
                if (!IsFootnoteDefinition(child))
                {
                    return false;
                }

                found = true;
            }

            index = index + 1;
        }

        return found;
    }

    internal static bool ContainsFootnoteReference(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (GetFootnoteReferenceIdentifier(node).Length > 0)
        {
            return true;
        }

        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            if (ContainsFootnoteReference(children[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string EscapeDelimitedContent(string value,
        char delimiter)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        StringBuilder builder = new StringBuilder(value.Length);
        int backslashes = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == delimiter)
            {
                if (backslashes % 2 == 0)
                {
                    builder.Append('\\');
                }

                builder.Append(current);
                backslashes = 0;
            }
            else if (current == ' ')
            {
                builder.Append("\\ ");
                backslashes = 0;
            }
            else
            {
                builder.Append(current);
                backslashes = current == '\\' ? backslashes + 1 : 0;
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static bool IsTrustedRawInlineExtension(HtmlNode node)
    {
        if (node == null || node.GetKindStorage() != HtmlNodeKind.Element
            || node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string name = node.GetLocalNameStorage();
        return name == "ins" || name == "kbd" || name == "mark"
            || name == "u";
    }

    internal static bool IsTrustedRawBlockExtension(HtmlNode node)
    {
        return IsHtmlElement(node, "details");
    }

    internal static string RenderTrustedRaw(HtmlNode node,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlSerializerOptions serializer =
            HtmlSerializerOptions.CreateCanonical();
        serializer.SetNewLine(options.GetNewLine());
        serializer.SetLimits(options.GetLimits());
        string html = HtmlSerializer.Serialize(node, serializer,
            cancellationToken);
        return HtmlMarkdownEscaper.Escape(html,
            HtmlMarkdownContextKind.RawHtml, "", false, options);
    }

    internal static void CollectFootnoteDefinitions(HtmlNode node,
        List<HtmlNode> definitions, bool containerRoot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!containerRoot && IsFootnoteDefinition(node))
        {
            definitions.Add(node);
            return;
        }

        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (!IsHiddenOrIgnored(child))
            {
                CollectFootnoteDefinitions(child, definitions, false,
                    cancellationToken);
            }

            index = index + 1;
        }

        if (containerRoot && definitions.Count == 0)
        {
            CollectIdentifiedListItems(node, definitions,
                cancellationToken);
        }
    }

    internal static void CollectIdentifiedListItems(HtmlNode node,
        List<HtmlNode> definitions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsHtmlElement(node, "li")
            && node.GetAttribute("id") != null)
        {
            definitions.Add(node);
            return;
        }

        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            if (!IsHiddenOrIgnored(children[index]))
            {
                CollectIdentifiedListItems(children[index], definitions,
                    cancellationToken);
            }

            index = index + 1;
        }
    }

    internal static string GetFootnoteIdentifier(string? value)
    {
        if (value == null)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder(value.Length);
        bool pendingDash = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool allowed = current >= 'a' && current <= 'z'
                || current >= 'A' && current <= 'Z'
                || current >= '0' && current <= '9'
                || current == '_' || current == '-' || current == '.';
            if (allowed)
            {
                if (pendingDash && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(current);
                pendingDash = false;
            }
            else
            {
                pendingDash = builder.Length > 0;
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string RenderNode(HtmlNode node,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, HtmlMarkdownLinks links)
    {
        HtmlMarkdownWriter writer = HtmlMarkdownWriter.Create(options,
            cancellationToken, links);
        writer.WriteNode(node);
        return writer.Complete();
    }

    internal static void AppendPrefixed(StringBuilder builder, string value,
        string firstPrefix, string continuationPrefix, string newLine)
    {
        builder.Append(firstPrefix);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n')
            {
                if (current == '\r' && index + 1 < value.Length
                    && value[index + 1] == '\n')
                {
                    index = index + 1;
                }

                builder.Append(newLine);
                int next = index + 1;
                if (next < value.Length && value[next] != '\r'
                    && value[next] != '\n')
                {
                    builder.Append(continuationPrefix);
                }
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }
    }

    internal static bool HasRole(HtmlNode node, string expected)
    {
        if (node.GetKindStorage() != HtmlNodeKind.Element)
        {
            return false;
        }

        string? role = node.GetAttribute("role");
        if (role == null)
        {
            return false;
        }

        string[] tokens = HtmlTokenLists.ParseSpaceSeparated(role);
        return HtmlTokenLists.ContainsAsciiInsensitive(tokens, expected);
    }

    internal static bool IsHiddenOrIgnored(HtmlNode node)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        return node.HasAttribute("hidden") || node.HasAttribute("inert")
            || EqualsTrimmedIgnoreCase(node.GetAttribute("aria-hidden"),
                "true");
    }

    internal static bool EqualsTrimmedIgnoreCase(string? value, string expected)
    {
        if (value == null)
        {
            return false;
        }

        return Ascii.EqualsIgnoreCase(value.Trim().AsSpan(),
            expected.AsSpan());
    }

    internal static void ValidateOptionsAndLinks(
        HtmlCompiledMarkdownOptions options, HtmlMarkdownLinks links)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (links == null)
        {
            throw new ArgumentNullException("links");
        }
    }

    internal static void ValidateHtmlElement(HtmlNode node, string name)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (!IsHtmlElement(node, name))
        {
            throw new ArgumentException(
                "Markdown extension rendering requires an HTML " + name
                + " element.", "node");
        }
    }

    internal static bool IsHtmlElement(HtmlNode node, string name)
    {
        return node.GetKindStorage() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == name;
    }
}
