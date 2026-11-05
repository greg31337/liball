using System;

namespace html;

internal static class HtmlTreeBuilderTableModes
{
    internal static void Configure(HtmlTreeBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException("builder");
        }

        builder.SetModeHandler(HtmlInsertionMode.InTable, ProcessInTable);
        builder.SetModeHandler(HtmlInsertionMode.InTableText,
            ProcessInTableText);
        builder.SetModeHandler(HtmlInsertionMode.InCaption,
            ProcessInCaption);
        builder.SetModeHandler(HtmlInsertionMode.InColumnGroup,
            ProcessInColumnGroup);
        builder.SetModeHandler(HtmlInsertionMode.InTableBody,
            ProcessInTableBody);
        builder.SetModeHandler(HtmlInsertionMode.InRow, ProcessInRow);
        builder.SetModeHandler(HtmlInsertionMode.InCell, ProcessInCell);
    }

    internal static HtmlTreeBuilderAction ProcessInTable(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind == HtmlTokenKind.Character
            && IsTableTextCurrent(builder.GetOpenElements()))
        {
            builder.ClearPendingTableCharacters();
            builder.SetOriginalInsertionMode(builder.GetInsertionMode());
            builder.SetInsertionMode(HtmlInsertionMode.InTableText);
            return HtmlTreeBuilderAction.Reprocess();
        }

        if (kind == HtmlTokenKind.Comment)
        {
            builder.InsertComment(token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.Doctype)
        {
            builder.ReportParseError(token,
                "A document type is not allowed in a table.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.StartTag)
        {
            return ProcessInTableStartTag(builder, token);
        }

        if (kind == HtmlTokenKind.EndTag)
        {
            return ProcessInTableEndTag(builder, token);
        }

        if (kind == HtmlTokenKind.EndOfFile)
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        return ProcessTableAnythingElse(builder, token);
    }

    internal static HtmlTreeBuilderAction ProcessInTableText(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        if (token.GetKind() == HtmlTokenKind.Character)
        {
            string data = token.GetData();
            int keptCount = 0;
            char[] kept = new char[data.Length];
            bool hadNull = false;
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
                    "Null characters are not allowed in table text.");
            }

            if (keptCount > 0)
            {
                HtmlToken pending = keptCount == data.Length ? token
                    : HtmlToken.CreateCharacter(
                        new string(kept, 0, keptCount),
                        token.GetSourceSpan());
                builder.AddPendingTableCharacter(pending);
            }

            return HtmlTreeBuilderAction.Consume();
        }

        FlushPendingTableCharacters(builder);
        builder.SetInsertionMode(builder.GetOriginalInsertionMode());
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessInCaption(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        if (IsEndTag(token, "caption"))
        {
            CloseCaption(builder, token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsCaptionBoundary(token))
        {
            if (!CloseCaption(builder, token))
            {
                return HtmlTreeBuilderAction.Consume();
            }

            return HtmlTreeBuilderAction.Reprocess();
        }

        if (token.GetKind() == HtmlTokenKind.EndTag
            && IsCaptionIgnoredEndTag(token.GetName()))
        {
            builder.ReportParseError(token,
                "The end tag is not allowed in a table caption.");
            return HtmlTreeBuilderAction.Consume();
        }

        return HtmlTreeBuilderAction.ProcessUsingMode(
            HtmlInsertionMode.InBody);
    }

    internal static HtmlTreeBuilderAction ProcessInColumnGroup(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind == HtmlTokenKind.Character && IsWhitespace(token.GetData()))
        {
            builder.InsertCharacter(token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.Comment)
        {
            builder.InsertComment(token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.Doctype)
        {
            builder.ReportParseError(token,
                "A document type is not allowed in a column group.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "html"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (IsStartTag(token, "col"))
        {
            InsertVoid(builder, token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsEndTag(token, "colgroup"))
        {
            if (!IsCurrentHtmlElement(builder.GetOpenElements(),
                "colgroup"))
            {
                builder.ReportParseError(token,
                    "A column-group end tag was ignored outside a column group.");
                return HtmlTreeBuilderAction.Consume();
            }

            builder.GetOpenElements().Pop();
            builder.SetInsertionMode(HtmlInsertionMode.InTable);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsEndTag(token, "col"))
        {
            builder.ReportParseError(token,
                "A col end tag was ignored.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsTemplateToken(token))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        if (kind == HtmlTokenKind.EndOfFile)
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (!IsCurrentHtmlElement(builder.GetOpenElements(), "colgroup"))
        {
            builder.ReportParseError(token,
                "The token was ignored outside a column group.");
            return HtmlTreeBuilderAction.Consume();
        }

        builder.GetOpenElements().Pop();
        builder.SetInsertionMode(HtmlInsertionMode.InTable);
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessInTableBody(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        if (IsStartTag(token, "tr"))
        {
            ClearStackToTableBodyContext(builder.GetOpenElements());
            builder.InsertHtmlElement(token);
            builder.SetInsertionMode(HtmlInsertionMode.InRow);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "td") || IsStartTag(token, "th"))
        {
            builder.ReportParseError(token,
                "A table row was generated before the cell.");
            ClearStackToTableBodyContext(builder.GetOpenElements());
            builder.InsertGeneratedHtmlElement("tr");
            builder.SetInsertionMode(HtmlInsertionMode.InRow);
            return HtmlTreeBuilderAction.Reprocess();
        }

        if (token.GetKind() == HtmlTokenKind.EndTag
            && IsTableSectionName(token.GetName()))
        {
            if (!builder.GetOpenElements().HasHtmlElementInTableScope(
                token.GetName()))
            {
                builder.ReportParseError(token,
                    "The table-section end tag was ignored outside its section.");
                return HtmlTreeBuilderAction.Consume();
            }

            CloseTableBody(builder);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsTableBodyBoundary(token))
        {
            if (!HasTableSectionInScope(builder.GetOpenElements()))
            {
                builder.ReportParseError(token,
                    "The table-section boundary was ignored outside a section.");
                return HtmlTreeBuilderAction.Consume();
            }

            CloseTableBody(builder);
            return HtmlTreeBuilderAction.Reprocess();
        }

        if (token.GetKind() == HtmlTokenKind.EndTag
            && IsTableBodyIgnoredEndTag(token.GetName()))
        {
            builder.ReportParseError(token,
                "The end tag is not allowed in a table section.");
            return HtmlTreeBuilderAction.Consume();
        }

        return HtmlTreeBuilderAction.ProcessUsingMode(
            HtmlInsertionMode.InTable);
    }

    internal static HtmlTreeBuilderAction ProcessInRow(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        if (IsStartTag(token, "td") || IsStartTag(token, "th"))
        {
            ClearStackToTableRowContext(builder.GetOpenElements());
            builder.InsertHtmlElement(token);
            builder.SetInsertionMode(HtmlInsertionMode.InCell);
            builder.GetFormattingElements().AddMarker();
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsEndTag(token, "tr"))
        {
            if (!CloseRow(builder, token))
            {
                return HtmlTreeBuilderAction.Consume();
            }

            return HtmlTreeBuilderAction.Consume();
        }

        if (IsRowBoundary(token))
        {
            if (!CloseRow(builder, token))
            {
                return HtmlTreeBuilderAction.Consume();
            }

            return HtmlTreeBuilderAction.Reprocess();
        }

        if (token.GetKind() == HtmlTokenKind.EndTag
            && IsTableSectionName(token.GetName()))
        {
            HtmlOpenElementStack stack = builder.GetOpenElements();
            if (!stack.HasHtmlElementInTableScope(token.GetName())
                || !stack.HasHtmlElementInTableScope("tr"))
            {
                builder.ReportParseError(token,
                    "The table-section end tag was ignored in a row.");
                return HtmlTreeBuilderAction.Consume();
            }

            CloseRow(builder, token);
            return HtmlTreeBuilderAction.Reprocess();
        }

        if (token.GetKind() == HtmlTokenKind.EndTag
            && IsRowIgnoredEndTag(token.GetName()))
        {
            builder.ReportParseError(token,
                "The end tag is not allowed in a table row.");
            return HtmlTreeBuilderAction.Consume();
        }

        return HtmlTreeBuilderAction.ProcessUsingMode(
            HtmlInsertionMode.InTable);
    }

    internal static HtmlTreeBuilderAction ProcessInCell(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        if (IsEndTag(token, "td") || IsEndTag(token, "th"))
        {
            if (!builder.GetOpenElements().HasHtmlElementInTableScope(
                token.GetName()))
            {
                builder.ReportParseError(token,
                    "The cell end tag was ignored outside its cell.");
                return HtmlTreeBuilderAction.Consume();
            }

            CloseCell(builder, token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsCellStartBoundary(token))
        {
            if (!HasCellInTableScope(builder.GetOpenElements()))
            {
                builder.ReportParseError(token,
                    "The table token was ignored outside a cell.");
                return HtmlTreeBuilderAction.Consume();
            }

            CloseCell(builder, token);
            return HtmlTreeBuilderAction.Reprocess();
        }

        if (token.GetKind() == HtmlTokenKind.EndTag
            && IsCellAlwaysIgnoredEndTag(token.GetName()))
        {
            builder.ReportParseError(token,
                "The end tag is not allowed in a table cell.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (token.GetKind() == HtmlTokenKind.EndTag
            && IsCellEndBoundary(token.GetName()))
        {
            if (!builder.GetOpenElements().HasHtmlElementInTableScope(
                token.GetName()))
            {
                builder.ReportParseError(token,
                    "The table end tag was ignored outside its scope.");
                return HtmlTreeBuilderAction.Consume();
            }

            CloseCell(builder, token);
            return HtmlTreeBuilderAction.Reprocess();
        }

        return HtmlTreeBuilderAction.ProcessUsingMode(
            HtmlInsertionMode.InBody);
    }

    internal static HtmlTreeBuilderAction ProcessInTableStartTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        string name = token.GetName();
        if (name == "html")
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (name == "caption")
        {
            ClearStackToTableContext(builder.GetOpenElements());
            builder.GetFormattingElements().AddMarker();
            builder.InsertHtmlElement(token);
            builder.SetInsertionMode(HtmlInsertionMode.InCaption);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "colgroup")
        {
            ClearStackToTableContext(builder.GetOpenElements());
            builder.InsertHtmlElement(token);
            builder.SetInsertionMode(HtmlInsertionMode.InColumnGroup);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "col")
        {
            ClearStackToTableContext(builder.GetOpenElements());
            builder.InsertGeneratedHtmlElement("colgroup");
            builder.SetInsertionMode(HtmlInsertionMode.InColumnGroup);
            return HtmlTreeBuilderAction.Reprocess();
        }

        if (IsTableSectionName(name))
        {
            ClearStackToTableContext(builder.GetOpenElements());
            builder.InsertHtmlElement(token);
            builder.SetInsertionMode(HtmlInsertionMode.InTableBody);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "td" || name == "th" || name == "tr")
        {
            builder.ReportParseError(token,
                "A tbody element was generated before the table row or cell.");
            ClearStackToTableContext(builder.GetOpenElements());
            builder.InsertGeneratedHtmlElement("tbody");
            builder.SetInsertionMode(HtmlInsertionMode.InTableBody);
            return HtmlTreeBuilderAction.Reprocess();
        }

        if (name == "table")
        {
            HtmlOpenElementStack stack = builder.GetOpenElements();
            builder.ReportParseError(token,
                "A nested table start tag implicitly closed the table.");
            if (!stack.HasHtmlElementInTableScope("table"))
            {
                return HtmlTreeBuilderAction.Consume();
            }

            stack.PopUntilHtmlElement("table");
            builder.ResetInsertionModeAppropriately();
            return HtmlTreeBuilderAction.Reprocess();
        }

        if (name == "style" || name == "script" || name == "template")
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        if (name == "input" && IsHiddenInput(token))
        {
            builder.ReportParseError(token,
                "A hidden input was inserted directly in a table.");
            InsertVoid(builder, token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "form")
        {
            builder.ReportParseError(token,
                "A form start tag occurred directly in a table.");
            HtmlOpenElementStack stack = builder.GetOpenElements();
            if (builder.GetFormElement() != null
                || stack.GetLastHtmlElement("template") != null)
            {
                return HtmlTreeBuilderAction.Consume();
            }

            HtmlNode? form = builder.InsertHtmlElement(token);
            if (form != null)
            {
                builder.SetFormElement(form);
                stack.Remove(form);
            }

            return HtmlTreeBuilderAction.Consume();
        }

        return ProcessTableAnythingElse(builder, token);
    }

    internal static HtmlTreeBuilderAction ProcessInTableEndTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        string name = token.GetName();
        if (name == "table")
        {
            HtmlOpenElementStack stack = builder.GetOpenElements();
            if (!stack.HasHtmlElementInTableScope("table"))
            {
                builder.ReportParseError(token,
                    "A table end tag was ignored outside table scope.");
                return HtmlTreeBuilderAction.Consume();
            }

            stack.PopUntilHtmlElement("table");
            builder.ResetInsertionModeAppropriately();
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "template")
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        if (IsInTableIgnoredEndTag(name))
        {
            builder.ReportParseError(token,
                "The end tag is not allowed directly in a table.");
            return HtmlTreeBuilderAction.Consume();
        }

        return ProcessTableAnythingElse(builder, token);
    }

    internal static HtmlTreeBuilderAction ProcessTableAnythingElse(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        builder.ReportParseError(token,
            "The token was foster-parented outside the table.");
        builder.SetFosterParenting(true);
        HtmlTreeBuilderAction action = HtmlTreeBuilderInBodyMode.Process(
            builder, token);
        builder.SetFosterParenting(false);
        return action;
    }

    internal static void FlushPendingTableCharacters(HtmlTreeBuilder builder)
    {
        HtmlToken[] pending = builder.GetPendingTableCharacters();
        bool hasNonWhitespace = false;
        int index = 0;
        while (index < pending.Length)
        {
            if (!IsWhitespace(pending[index].GetData()))
            {
                hasNonWhitespace = true;
            }

            index = index + 1;
        }

        if (hasNonWhitespace && pending.Length > 0)
        {
            builder.ReportParseError(pending[0],
                "Non-whitespace table text was foster-parented.");
        }

        index = 0;
        while (index < pending.Length)
        {
            if (hasNonWhitespace)
            {
                ProcessTableAnythingElse(builder, pending[index]);
            }
            else
            {
                builder.InsertCharacter(pending[index]);
            }

            index = index + 1;
        }

        builder.ClearPendingTableCharacters();
    }

    internal static bool CloseCaption(HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (!stack.HasHtmlElementInTableScope("caption"))
        {
            builder.ReportParseError(token,
                "The caption end was ignored outside caption scope.");
            return false;
        }

        stack.GenerateImpliedEndTags();
        if (!IsCurrentHtmlElement(stack, "caption"))
        {
            builder.ReportParseError(token,
                "Open elements were implicitly closed with the caption.");
        }

        stack.PopUntilHtmlElement("caption");
        builder.GetFormattingElements().ClearToLastMarker();
        builder.SetInsertionMode(HtmlInsertionMode.InTable);
        return true;
    }

    internal static void CloseTableBody(HtmlTreeBuilder builder)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        ClearStackToTableBodyContext(stack);
        stack.Pop();
        builder.SetInsertionMode(HtmlInsertionMode.InTable);
    }

    internal static bool CloseRow(HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (!stack.HasHtmlElementInTableScope("tr"))
        {
            builder.ReportParseError(token,
                "The row boundary was ignored outside row scope.");
            return false;
        }

        ClearStackToTableRowContext(stack);
        stack.Pop();
        builder.SetInsertionMode(HtmlInsertionMode.InTableBody);
        return true;
    }

    internal static void CloseCell(HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        stack.GenerateImpliedEndTags();
        HtmlNode? current = stack.GetCurrentNode();
        if (current == null || current.GetNamespaceStorage()
                != HtmlNamespace.Html
            || current.GetLocalNameStorage() != "td"
                && current.GetLocalNameStorage() != "th")
        {
            builder.ReportParseError(token,
                "Open elements were implicitly closed with the table cell.");
        }

        bool running = true;
        while (stack.GetCount() > 0 && running)
        {
            HtmlNode popped = stack.PopRequired();
            if (popped.GetNamespaceStorage() == HtmlNamespace.Html
                && (popped.GetLocalNameStorage() == "td"
                    || popped.GetLocalNameStorage() == "th"))
            {
                running = false;
            }
        }

        builder.GetFormattingElements().ClearToLastMarker();
        builder.SetInsertionMode(HtmlInsertionMode.InRow);
    }

    internal static void ClearStackToTableContext(HtmlOpenElementStack stack)
    {
        while (stack.GetCount() > 0
            && !IsCurrentHtmlElement(stack, "table")
            && !IsCurrentHtmlElement(stack, "template")
            && !IsCurrentHtmlElement(stack, "html"))
        {
            stack.Pop();
        }
    }

    internal static void ClearStackToTableBodyContext(HtmlOpenElementStack stack)
    {
        while (stack.GetCount() > 0
            && !IsCurrentHtmlElement(stack, "tbody")
            && !IsCurrentHtmlElement(stack, "tfoot")
            && !IsCurrentHtmlElement(stack, "thead")
            && !IsCurrentHtmlElement(stack, "template")
            && !IsCurrentHtmlElement(stack, "html"))
        {
            stack.Pop();
        }
    }

    internal static void ClearStackToTableRowContext(HtmlOpenElementStack stack)
    {
        while (stack.GetCount() > 0
            && !IsCurrentHtmlElement(stack, "tr")
            && !IsCurrentHtmlElement(stack, "template")
            && !IsCurrentHtmlElement(stack, "html"))
        {
            stack.Pop();
        }
    }

    internal static void InsertVoid(HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlNode? element = builder.InsertHtmlElement(token);
        if (element != null)
        {
            builder.GetOpenElements().Pop();
        }

        if (token.RequiresSelfClosingAcknowledgement())
        {
            token.AcknowledgeSelfClosing();
        }
    }

    internal static bool IsCurrentHtmlElement(HtmlOpenElementStack stack,
        string localName)
    {
        HtmlNode? current = stack.GetCurrentNode();
        return current != null
            && current.GetNamespaceStorage() == HtmlNamespace.Html
            && current.GetLocalNameStorage() == localName;
    }

    internal static bool IsTableTextCurrent(HtmlOpenElementStack stack)
    {
        HtmlNode? current = stack.GetCurrentNode();
        if (current == null
            || current.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string name = current.GetLocalNameStorage();
        return name == "table" || name == "tbody" || name == "template"
            || name == "tfoot" || name == "thead" || name == "tr";
    }

    internal static bool IsStartTag(HtmlToken token, string name)
    {
        return token.GetKind() == HtmlTokenKind.StartTag
            && token.GetName() == name;
    }

    internal static bool IsEndTag(HtmlToken token, string name)
    {
        return token.GetKind() == HtmlTokenKind.EndTag
            && token.GetName() == name;
    }

    internal static bool IsWhitespace(string data)
    {
        int index = 0;
        while (index < data.Length)
        {
            char value = data[index];
            if (value != '\t' && value != '\n' && value != '\f'
                && value != '\r' && value != ' ')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsHiddenInput(HtmlToken token)
    {
        HtmlAttribute[] attributes = token.GetAttributes();
        int index = 0;
        while (index < attributes.Length)
        {
            HtmlAttribute attribute = attributes[index];
            if (attribute.GetNamespaceUri().Length == 0
                && attribute.GetLocalName() == "type"
                && Ascii.EqualsIgnoreCase(attribute.GetValue(), "hidden"))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsTemplateToken(HtmlToken token)
    {
        return token.GetName() == "template"
            && (token.GetKind() == HtmlTokenKind.StartTag
                || token.GetKind() == HtmlTokenKind.EndTag);
    }

    internal static bool IsTableSectionName(string name)
    {
        return name == "tbody" || name == "tfoot" || name == "thead";
    }

    internal static bool HasTableSectionInScope(HtmlOpenElementStack stack)
    {
        return stack.HasHtmlElementInTableScope("tbody")
            || stack.HasHtmlElementInTableScope("tfoot")
            || stack.HasHtmlElementInTableScope("thead");
    }

    internal static bool HasCellInTableScope(HtmlOpenElementStack stack)
    {
        return stack.HasHtmlElementInTableScope("td")
            || stack.HasHtmlElementInTableScope("th");
    }

    internal static bool IsCaptionBoundary(HtmlToken token)
    {
        if (IsEndTag(token, "table"))
        {
            return true;
        }

        if (token.GetKind() != HtmlTokenKind.StartTag)
        {
            return false;
        }

        string name = token.GetName();
        return name == "caption" || name == "col" || name == "colgroup"
            || name == "tbody" || name == "td" || name == "tfoot"
            || name == "th" || name == "thead" || name == "tr";
    }

    internal static bool IsCaptionIgnoredEndTag(string name)
    {
        return name == "body" || name == "col" || name == "colgroup"
            || name == "html" || name == "tbody" || name == "td"
            || name == "tfoot" || name == "th" || name == "thead"
            || name == "tr";
    }

    internal static bool IsTableBodyBoundary(HtmlToken token)
    {
        if (IsEndTag(token, "table"))
        {
            return true;
        }

        if (token.GetKind() != HtmlTokenKind.StartTag)
        {
            return false;
        }

        string name = token.GetName();
        return name == "caption" || name == "col" || name == "colgroup"
            || name == "tbody" || name == "tfoot" || name == "thead";
    }

    internal static bool IsTableBodyIgnoredEndTag(string name)
    {
        return name == "body" || name == "caption" || name == "col"
            || name == "colgroup" || name == "html" || name == "td"
            || name == "th" || name == "tr";
    }

    internal static bool IsRowBoundary(HtmlToken token)
    {
        if (IsEndTag(token, "table"))
        {
            return true;
        }

        if (token.GetKind() != HtmlTokenKind.StartTag)
        {
            return false;
        }

        string name = token.GetName();
        return name == "caption" || name == "col" || name == "colgroup"
            || name == "tbody" || name == "tfoot" || name == "thead"
            || name == "tr";
    }

    internal static bool IsRowIgnoredEndTag(string name)
    {
        return name == "body" || name == "caption" || name == "col"
            || name == "colgroup" || name == "html" || name == "td"
            || name == "th";
    }

    internal static bool IsCellStartBoundary(HtmlToken token)
    {
        if (token.GetKind() != HtmlTokenKind.StartTag)
        {
            return false;
        }

        string name = token.GetName();
        return name == "caption" || name == "col" || name == "colgroup"
            || name == "tbody" || name == "td" || name == "tfoot"
            || name == "th" || name == "thead" || name == "tr";
    }

    internal static bool IsCellAlwaysIgnoredEndTag(string name)
    {
        return name == "body" || name == "caption" || name == "col"
            || name == "colgroup" || name == "html";
    }

    internal static bool IsCellEndBoundary(string name)
    {
        return name == "table" || name == "tbody" || name == "tfoot"
            || name == "thead" || name == "tr";
    }

    internal static bool IsInTableIgnoredEndTag(string name)
    {
        return name == "body" || name == "caption" || name == "col"
            || name == "colgroup" || name == "html" || name == "tbody"
            || name == "td" || name == "tfoot" || name == "th"
            || name == "thead" || name == "tr";
    }
}
