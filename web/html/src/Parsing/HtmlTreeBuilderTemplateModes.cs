using System;

namespace html;

internal static class HtmlTreeBuilderTemplateModes
{
    internal static void Configure(HtmlTreeBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException("builder");
        }

        builder.SetModeHandler(HtmlInsertionMode.InTemplate,
            ProcessInTemplate);
    }

    internal static HtmlTreeBuilderAction ProcessTemplateStartTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        builder.InsertHtmlElement(token);
        builder.GetFormattingElements().AddMarker();
        builder.SetFramesetOk(false);
        builder.PushTemplateInsertionMode(HtmlInsertionMode.InTemplate);
        builder.SetInsertionMode(HtmlInsertionMode.InTemplate);
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessTemplateEndTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (!stack.HasHtmlElementInScope("template"))
        {
            builder.ReportParseError(token,
                "A template end tag was ignored outside template scope.");
            return HtmlTreeBuilderAction.Consume();
        }

        stack.GenerateAllImpliedEndTagsThoroughly();
        if (!IsCurrentHtmlElement(stack, "template"))
        {
            builder.ReportParseError(token,
                "Open elements were implicitly closed with the template.");
        }

        stack.PopUntilHtmlElement("template");
        builder.GetFormattingElements().ClearToLastMarker();
        if (builder.GetTemplateInsertionModeCount() > 0)
        {
            builder.PopTemplateInsertionMode();
        }
        else
        {
            builder.ReportParseError(token,
                "The template insertion-mode stack was unexpectedly empty.");
        }

        builder.ResetInsertionModeAppropriately();
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessInTemplate(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind == HtmlTokenKind.Character || kind == HtmlTokenKind.Comment
            || kind == HtmlTokenKind.Doctype)
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (kind == HtmlTokenKind.StartTag)
        {
            string name = token.GetName();
            if (IsHeadStartTag(name))
            {
                return HtmlTreeBuilderAction.ProcessUsingMode(
                    HtmlInsertionMode.InHead);
            }

            if (name == "caption" || name == "colgroup"
                || name == "tbody" || name == "tfoot" || name == "thead")
            {
                return SwitchTemplateMode(builder, HtmlInsertionMode.InTable);
            }

            if (name == "col")
            {
                return SwitchTemplateMode(builder,
                    HtmlInsertionMode.InColumnGroup);
            }

            if (name == "tr")
            {
                return SwitchTemplateMode(builder,
                    HtmlInsertionMode.InTableBody);
            }

            if (name == "td" || name == "th")
            {
                return SwitchTemplateMode(builder, HtmlInsertionMode.InRow);
            }

            return SwitchTemplateMode(builder, HtmlInsertionMode.InBody);
        }

        if (kind == HtmlTokenKind.EndTag)
        {
            if (token.GetName() == "template")
            {
                return HtmlTreeBuilderAction.ProcessUsingMode(
                    HtmlInsertionMode.InHead);
            }

            builder.ReportParseError(token,
                "The end tag was ignored in the template insertion mode.");
            return HtmlTreeBuilderAction.Consume();
        }

        return ProcessEndOfFile(builder, token);
    }

    internal static HtmlTreeBuilderAction SwitchTemplateMode(HtmlTreeBuilder builder,
        HtmlInsertionMode mode)
    {
        builder.ReplaceCurrentTemplateInsertionMode(mode);
        builder.SetInsertionMode(mode);
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessEndOfFile(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (stack.GetLastHtmlElement("template") == null)
        {
            if (builder.GetTemplateInsertionModeCount() > 0)
            {
                builder.PopTemplateInsertionMode();
            }

            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        builder.ReportParseError(token,
            "The end of input implicitly closed an open template.");
        stack.PopUntilHtmlElement("template");
        builder.GetFormattingElements().ClearToLastMarker();
        if (builder.GetTemplateInsertionModeCount() > 0)
        {
            builder.PopTemplateInsertionMode();
        }

        builder.ResetInsertionModeAppropriately();
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static bool IsHeadStartTag(string name)
    {
        return name == "base" || name == "basefont" || name == "bgsound"
            || name == "link" || name == "meta" || name == "noframes"
            || name == "script" || name == "style" || name == "template"
            || name == "title";
    }

    internal static bool IsCurrentHtmlElement(HtmlOpenElementStack stack,
        string localName)
    {
        HtmlNode? current = stack.GetCurrentNode();
        return current != null
            && current.GetNamespaceStorage() == HtmlNamespace.Html
            && current.GetLocalNameStorage() == localName;
    }
}
