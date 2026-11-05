using System;

namespace html;

internal static class HtmlTreeBuilderSelectModes
{
    internal static void Configure(HtmlTreeBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException("builder");
        }

        builder.SetModeHandler(HtmlInsertionMode.InSelect, ProcessInSelect);
        builder.SetModeHandler(HtmlInsertionMode.InSelectInTable,
            ProcessInSelectInTable);
    }

    internal static HtmlTreeBuilderAction ProcessInSelect(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind == HtmlTokenKind.Character)
        {
            return ProcessCharacter(builder, token);
        }

        if (kind == HtmlTokenKind.Comment)
        {
            builder.InsertComment(token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.Doctype)
        {
            builder.ReportParseError(token,
                "A document type is not allowed in a select element.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.StartTag)
        {
            return ProcessStartTag(builder, token);
        }

        if (kind == HtmlTokenKind.EndTag)
        {
            return ProcessEndTag(builder, token);
        }

        return HtmlTreeBuilderAction.ProcessUsingMode(
            HtmlInsertionMode.InBody);
    }

    internal static HtmlTreeBuilderAction ProcessInSelectInTable(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        if (IsTableBoundary(token))
        {
            builder.ReportParseError(token,
                "A table token implicitly closed the select element.");
            if (token.GetKind() == HtmlTokenKind.EndTag
                && !builder.GetOpenElements()
                    .HasHtmlElementInTableScope(token.GetName()))
            {
                return HtmlTreeBuilderAction.Consume();
            }

            if (!CloseSelect(builder, token))
            {
                return HtmlTreeBuilderAction.Consume();
            }

            return HtmlTreeBuilderAction.Reprocess();
        }

        return HtmlTreeBuilderAction.ProcessUsingMode(
            HtmlInsertionMode.InSelect);
    }

    internal static HtmlTreeBuilderAction ProcessCharacter(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        string data = token.GetData();
        bool hadNull = false;
        int keptCount = 0;
        char[] kept = new char[data.Length];
        int index = 0;
        while (index < data.Length)
        {
            if (data[index] == '\0')
            {
                hadNull = true;
            }
            else
            {
                kept[keptCount] = data[index];
                keptCount = keptCount + 1;
            }

            index = index + 1;
        }

        if (hadNull)
        {
            builder.ReportParseError(token,
                "Null characters are not allowed in a select element.");
        }

        if (keptCount > 0)
        {
            string inserted = keptCount == data.Length ? data
                : new string(kept, 0, keptCount);
            builder.InsertCharacterData(inserted, token.GetSourceSpan());
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessStartTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        string name = token.GetName();
        if (name == "html")
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (name == "option")
        {
            PopCurrentOption(builder.GetOpenElements());
            builder.InsertHtmlElement(token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "optgroup")
        {
            HtmlOpenElementStack stack = builder.GetOpenElements();
            PopCurrentOption(stack);
            if (IsCurrentHtmlElement(stack, "optgroup"))
            {
                stack.Pop();
            }

            builder.InsertHtmlElement(token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "select")
        {
            builder.ReportParseError(token,
                "A nested select start tag closed the open select element.");
            CloseSelect(builder, token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "input" || name == "keygen" || name == "textarea")
        {
            builder.ReportParseError(token,
                "A form control implicitly closed the select element.");
            if (!CloseSelect(builder, token))
            {
                return HtmlTreeBuilderAction.Consume();
            }

            return HtmlTreeBuilderAction.Reprocess();
        }

        if (name == "script" || name == "template")
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        builder.ReportParseError(token,
            "The start tag was ignored in a select element.");
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessEndTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        string name = token.GetName();
        if (name == "optgroup")
        {
            HtmlOpenElementStack stack = builder.GetOpenElements();
            int count = stack.GetCount();
            if (IsCurrentHtmlElement(stack, "option") && count > 1
                && IsHtmlElement(stack.GetNode(count - 2), "optgroup"))
            {
                stack.Pop();
            }

            if (IsCurrentHtmlElement(stack, "optgroup"))
            {
                stack.Pop();
            }
            else
            {
                builder.ReportParseError(token,
                    "An optgroup end tag was ignored outside its group.");
            }

            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "option")
        {
            HtmlOpenElementStack stack = builder.GetOpenElements();
            if (IsCurrentHtmlElement(stack, "option"))
            {
                stack.Pop();
            }
            else
            {
                builder.ReportParseError(token,
                    "An option end tag was ignored outside an option.");
            }

            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "select")
        {
            CloseSelect(builder, token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "template")
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        builder.ReportParseError(token,
            "The end tag was ignored in a select element.");
        return HtmlTreeBuilderAction.Consume();
    }

    internal static bool CloseSelect(HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (!stack.HasHtmlElementInSelectScope("select"))
        {
            builder.ReportParseError(token,
                "The select token was ignored outside select scope.");
            return false;
        }

        stack.PopUntilHtmlElement("select");
        builder.ResetInsertionModeAppropriately();
        return true;
    }

    internal static void PopCurrentOption(HtmlOpenElementStack stack)
    {
        if (IsCurrentHtmlElement(stack, "option"))
        {
            stack.Pop();
        }
    }

    internal static bool IsCurrentHtmlElement(HtmlOpenElementStack stack,
        string localName)
    {
        HtmlNode? current = stack.GetCurrentNode();
        return current != null && IsHtmlElement(current, localName);
    }

    internal static bool IsHtmlElement(HtmlNode? node, string localName)
    {
        return node != null
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == localName;
    }

    internal static bool IsTableBoundary(HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind != HtmlTokenKind.StartTag && kind != HtmlTokenKind.EndTag)
        {
            return false;
        }

        string name = token.GetName();
        return name == "caption" || name == "table" || name == "tbody"
            || name == "tfoot" || name == "thead" || name == "tr"
            || name == "td" || name == "th";
    }
}
