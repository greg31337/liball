using System;
using System.Text;
using System.Threading;

namespace html;

internal enum HtmlMarkdownElementAction
{
    Standard = 0,
    Unwrap = 1,
    Drop = 2,
    PreserveHtml = 3,
    EscapeHtml = 4,
    Text = 5,
    Error = 6
}

internal static class HtmlMarkdownElementRules
{
    internal static HtmlMarkdownElementAction Resolve(HtmlNode node,
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

        HtmlCompiledMarkdownRule? rule = options.FindRule(node,
            node.GetRoot(), cancellationToken);
        if (rule != null)
        {
            HtmlMarkdownRuleAction ruleAction = rule.GetAction();
            if (ruleAction == HtmlMarkdownRuleAction.Keep)
            {
                return HtmlMarkdownElementAction.PreserveHtml;
            }

            if (ruleAction == HtmlMarkdownRuleAction.Remove)
            {
                return HtmlMarkdownElementAction.Drop;
            }
        }

        if (IsKnown(node))
        {
            return HtmlMarkdownElementAction.Standard;
        }

        switch (options.GetUnknownElementAction())
        {
            case HtmlMarkdownUnknownElementAction.Unwrap:
                return HtmlMarkdownElementAction.Unwrap;
            case HtmlMarkdownUnknownElementAction.Drop:
                return HtmlMarkdownElementAction.Drop;
            case HtmlMarkdownUnknownElementAction.PreserveHtml:
                return HtmlMarkdownElementAction.PreserveHtml;
            case HtmlMarkdownUnknownElementAction.EscapeHtml:
                return HtmlMarkdownElementAction.EscapeHtml;
            case HtmlMarkdownUnknownElementAction.Text:
                return HtmlMarkdownElementAction.Text;
            case HtmlMarkdownUnknownElementAction.Error:
                return HtmlMarkdownElementAction.Error;
            default:
                throw new InvalidOperationException(
                    "Unknown compiled Markdown element action.");
        }
    }

    internal static string RenderHtml(HtmlNode node,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, bool escape)
    {
        HtmlSerializerOptions serializer =
            HtmlSerializerOptions.CreateCanonical();
        serializer.SetNewLine(options.GetNewLine());
        serializer.SetLimits(options.GetLimits());
        string html = HtmlSerializer.Serialize(node, serializer,
            cancellationToken);
        return escape ? HtmlMarkdownEscaper.EscapeHtmlLiteral(html)
            : HtmlMarkdownEscaper.Escape(html,
                HtmlMarkdownContextKind.RawHtml, "", false, options);
    }

    internal static Exception CreateUnknownElementException(
        HtmlNode node)
    {
        InvalidOperationException exception = new InvalidOperationException(
            "Unknown element '" + node.GetName()
                + "' cannot be converted to Markdown.");
        exception.Data.Add("HtmlMarkdownUnknownElement", node);
        return exception;
    }

    internal static string GetVisibleText(HtmlNode node,
        CancellationToken cancellationToken)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        StringBuilder builder = new StringBuilder();
        AppendVisibleText(node, builder, cancellationToken, true);
        return builder.ToString();
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

    internal static bool IsKnown(HtmlNode node)
    {
        return node.GetNamespaceStorage() == HtmlNamespace.Html
            && HtmlNames.IsKnownElement(node.GetLocalNameStorage());
    }

    internal static void AppendVisibleText(HtmlNode node, StringBuilder builder,
        CancellationToken cancellationToken, bool root)
    {
        cancellationToken.ThrowIfCancellationRequested();
        HtmlNodeKind kind = node.GetKindStorage();
        if (kind == HtmlNodeKind.Text)
        {
            builder.Append(node.GetDataStorage());
            return;
        }

        if (kind != HtmlNodeKind.Element
            && kind != HtmlNodeKind.Fragment)
        {
            return;
        }

        if (!root && kind == HtmlNodeKind.Element
            && IsHiddenOrIgnored(node))
        {
            return;
        }

        bool separator = !root && kind == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && (node.GetLocalNameStorage() == "br"
                || !HtmlNames.IsPhrasingElement(
                    node.GetLocalNameStorage()));
        if (separator)
        {
            builder.Append(' ');
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
            AppendVisibleText(children[index], builder,
                cancellationToken, false);
            index = index + 1;
        }

        if (separator && node.GetLocalNameStorage() != "br")
        {
            builder.Append(' ');
        }
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
}
