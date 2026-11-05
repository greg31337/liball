using System;
using System.Text;
using System.Threading;

namespace html;

internal class HtmlMarkdownInlineCode
{
    internal string value;
    internal bool leadingWhitespace;
    internal bool trailingWhitespace;

    internal HtmlMarkdownInlineCode(string valueValue,
        bool leadingWhitespaceValue, bool trailingWhitespaceValue)
    {
        this.value = valueValue;
        this.leadingWhitespace = leadingWhitespaceValue;
        this.trailingWhitespace = trailingWhitespaceValue;
    }

    internal string GetValue()
    {
        return this.value;
    }

    internal bool HasLeadingWhitespace()
    {
        return this.leadingWhitespace;
    }

    internal bool HasTrailingWhitespace()
    {
        return this.trailingWhitespace;
    }
}

internal static class HtmlMarkdownCode
{
    internal static HtmlMarkdownInlineCode RenderInline(HtmlNode code,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken)
    {
        ValidateElement(code, "code");
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string raw = GetCodeText(code, cancellationToken);
        bool leadingWhitespace = raw.Length > 0
            && UnicodeTools.IsUnicodeWhitespace(raw[0]);
        bool trailingWhitespace = raw.Length > 0
            && UnicodeTools.IsUnicodeWhitespace(raw[raw.Length - 1]);
        string content = HtmlTextContent.Normalize(raw);
        if (content.Length == 0)
        {
            return new HtmlMarkdownInlineCode("", leadingWhitespace,
                trailingWhitespace);
        }

        int length = GetLongestRun(content, '`') + 1;
        string delimiter = CreateRepeated('`', length);
        bool pad = content[0] == '`'
            || content[content.Length - 1] == '`';
        string value = pad
            ? delimiter + " " + content + " " + delimiter
            : delimiter + content + delimiter;
        return new HtmlMarkdownInlineCode(value, leadingWhitespace,
            trailingWhitespace);
    }

    internal static string RenderBlock(HtmlNode pre,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken)
    {
        ValidateElement(pre, "pre");
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string raw = GetCodeText(pre, cancellationToken);
        bool contentEndsWithNewLine = raw.Length > 0
            && EndsWithNewLine(raw);
        string content = NormalizeLineEndings(raw, options.GetNewLine());
        HtmlNode? code = GetDirectCodeChild(pre);
        string info = GetInfoString(pre, code) ?? "";
        int backtickLength = GetFenceLength(content, '`');
        int tildeLength = GetFenceLength(content, '~');
        char fenceCharacter;
        int fenceLength;
        HtmlMarkdownCodeFenceStyle fenceStyle =
            options.GetCodeFenceStyle();
        if (fenceStyle == HtmlMarkdownCodeFenceStyle.Tildes
            || fenceStyle == HtmlMarkdownCodeFenceStyle.Backticks
                && info.IndexOf('`') >= 0
            || fenceStyle == HtmlMarkdownCodeFenceStyle.Adaptive
                && (info.IndexOf('`') >= 0
                    || tildeLength < backtickLength))
        {
            fenceCharacter = '~';
            fenceLength = tildeLength;
        }
        else
        {
            fenceCharacter = '`';
            fenceLength = backtickLength;
        }

        string fence = CreateRepeated(fenceCharacter, fenceLength);
        string newLine = options.GetNewLine();
        StringBuilder builder = new StringBuilder();
        builder.Append(fence);
        if (info.Length > 0 && info[0] == fenceCharacter)
        {
            builder.Append(' ');
        }

        builder.Append(info);
        builder.Append(newLine);
        builder.Append(content);
        if (content.Length > 0 && !contentEndsWithNewLine)
        {
            builder.Append(newLine);
        }

        builder.Append(fence);
        return builder.ToString();
    }

    internal static string GetCodeText(HtmlNode node,
        CancellationToken cancellationToken)
    {
        StringBuilder builder = new StringBuilder();
        AppendCodeText(node, builder, cancellationToken);
        return builder.ToString();
    }

    internal static void AppendCodeText(HtmlNode node, StringBuilder builder,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        HtmlNodeKind kind = node.GetKindStorage();
        if (kind == HtmlNodeKind.Text)
        {
            builder.Append(node.GetDataStorage());
            return;
        }

        if (kind != HtmlNodeKind.Element && kind != HtmlNodeKind.Fragment)
        {
            return;
        }

        if (kind == HtmlNodeKind.Element && IsHiddenOrIgnored(node))
        {
            return;
        }

        if (kind == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == "br")
        {
            builder.Append('\n');
            return;
        }

        HtmlNode? container = node.GetTemplateContentStorage();
        if (container == null)
        {
            container = node;
        }

        HtmlNode[] children = container.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            AppendCodeText(children[index], builder, cancellationToken);
            index = index + 1;
        }
    }

    internal static HtmlNode? GetDirectCodeChild(HtmlNode pre)
    {
        HtmlNode[] children = pre.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            if (IsHtmlElement(children[index], "code"))
            {
                return children[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static string? GetInfoString(HtmlNode pre, HtmlNode? code)
    {
        string? info = code == null ? null : GetNodeInfoString(code);
        if (info != null)
        {
            return info;
        }

        info = GetNodeInfoString(pre);
        return info == null ? "" : info;
    }

    internal static string? GetNodeInfoString(HtmlNode node)
    {
        if (node == null)
        {
            return null;
        }

        if (node.HasAttribute("data-info"))
        {
            string info = NormalizeInfo(node.GetAttribute("data-info"));
            if (info.Length > 0)
            {
                return info;
            }
        }

        if (node.HasAttribute("data-language"))
        {
            string language = NormalizeInfo(
                node.GetAttribute("data-language"));
            if (language.Length > 0)
            {
                return language;
            }
        }

        string? classLanguage = GetClassLanguage(node);
        return classLanguage == null ? null : NormalizeInfo(classLanguage);
    }

    internal static string? GetClassLanguage(HtmlNode node)
    {
        if (node == null)
        {
            return null;
        }

        string? classes = node.GetAttribute("class");
        if (classes == null)
        {
            return null;
        }

        string[] tokens = HtmlTokenLists.ParseSpaceSeparated(classes);
        int index = 0;
        while (index < tokens.Length)
        {
            string token = tokens[index];
            if (token.StartsWith("language-", StringComparison.Ordinal)
                && token.Length > 9)
            {
                return token.Substring(9);
            }

            if (token.StartsWith("lang-", StringComparison.Ordinal)
                && token.Length > 5)
            {
                return token.Substring(5);
            }

            index = index + 1;
        }

        return null;
    }

    internal static string NormalizeInfo(string? value)
    {
        if (value == null)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder(value.Length);
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (UnicodeTools.IsUnicodeWhitespace(current)
                || char.IsControl(current))
            {
                pendingSpace = builder.Length > 0;
            }
            else
            {
                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string NormalizeLineEndings(string value, string newLine)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r')
            {
                if (index + 1 < value.Length && value[index + 1] == '\n')
                {
                    index = index + 1;
                }

                builder.Append(newLine);
            }
            else if (current == '\n')
            {
                builder.Append(newLine);
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static int GetFenceLength(string content, char value)
    {
        int longest = GetLongestRun(content, value) + 1;
        return longest < 3 ? 3 : longest;
    }

    internal static int GetLongestRun(string value, char expected)
    {
        int longest = 0;
        int current = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == expected)
            {
                current = current + 1;
                if (current > longest)
                {
                    longest = current;
                }
            }
            else
            {
                current = 0;
            }

            index = index + 1;
        }

        return longest;
    }

    internal static bool EndsWithNewLine(string value)
    {
        char last = value[value.Length - 1];
        return last == '\r' || last == '\n';
    }

    internal static string CreateRepeated(char value, int count)
    {
        return new string(value, count);
    }

    internal static void ValidateElement(HtmlNode node, string name)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (!IsHtmlElement(node, name))
        {
            throw new ArgumentException(
                "Markdown code rendering requires an HTML " + name
                + " element.", "node");
        }
    }

    internal static bool IsHiddenOrIgnored(HtmlNode node)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        if (node.HasAttribute("hidden") || node.HasAttribute("inert")
            || EqualsTrimmedIgnoreCase(node.GetAttribute("aria-hidden"),
                "true"))
        {
            return true;
        }

        string name = node.GetLocalNameStorage();
        return name == "script" || name == "style" || name == "template"
            || name == "head" || name == "noscript"
            || name == "input"
            && EqualsTrimmedIgnoreCase(node.GetAttribute("type"), "hidden");
    }

    internal static bool EqualsTrimmedIgnoreCase(string? value, string expected)
    {
        if (value == null)
        {
            return false;
        }

        int start = 0;
        while (start < value.Length && Ascii.IsHtmlWhitespace(value[start]))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && Ascii.IsHtmlWhitespace(value[end - 1]))
        {
            end = end - 1;
        }

        return Ascii.EqualsIgnoreCase(value.AsSpan(start, end - start),
            expected.AsSpan());
    }

    internal static bool IsHtmlElement(HtmlNode node, string name)
    {
        return node.GetKindStorage() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == name;
    }
}
