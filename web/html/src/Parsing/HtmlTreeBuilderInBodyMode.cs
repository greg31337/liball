using System;

namespace html;

internal static class HtmlTreeBuilderInBodyMode
{
    internal static void Configure(HtmlTreeBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException("builder");
        }

        builder.SetModeHandler(HtmlInsertionMode.InBody, Process);
    }

    internal static HtmlTreeBuilderAction Process(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind == HtmlTokenKind.Character)
        {
            return ProcessCharacter(builder, token);
        }

        builder.TakeIgnoreNextLineFeed();
        if (kind == HtmlTokenKind.Comment)
        {
            builder.InsertComment(token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.Doctype)
        {
            builder.ReportParseError(token,
                "A document type is not allowed in the body.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.EndOfFile)
        {
            return ProcessEndOfFile(builder, token);
        }

        if (kind == HtmlTokenKind.StartTag)
        {
            return ProcessStartTag(builder, token);
        }

        return ProcessEndTag(builder, token);
    }

    internal static HtmlTreeBuilderAction ProcessCharacter(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        string data = token.GetData();
        if (builder.TakeIgnoreNextLineFeed() && data.Length > 0
            && data[0] == '\n')
        {
            data = data.Substring(1);
        }

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
                "Null characters are not allowed in the body.");
        }

        if (keptCount == 0)
        {
            return HtmlTreeBuilderAction.Consume();
        }

        string inserted = keptCount == kept.Length
            ? data : new string(kept, 0, keptCount);
        if (!builder.ReconstructActiveFormattingElements(
            token.GetSourceSpan()))
        {
            return HtmlTreeBuilderAction.Consume();
        }

        builder.InsertCharacterData(inserted, token.GetSourceSpan());
        if (!IsWhitespace(inserted))
        {
            builder.SetFramesetOk(false);
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessStartTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        string name = token.GetName();
        if (name == "html")
        {
            return ProcessHtmlStartTag(builder, token);
        }

        if (IsHeadToken(name))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        if (name == "body")
        {
            return ProcessBodyStartTag(builder, token);
        }

        if (name == "frameset")
        {
            return ProcessFramesetStartTag(builder, token);
        }

        if (IsBlockStartTag(name))
        {
            CloseParagraphIfOpen(builder, token);
            builder.InsertHtmlElement(token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsHeading(name))
        {
            return ProcessHeadingStartTag(builder, token);
        }

        if (name == "pre" || name == "listing")
        {
            CloseParagraphIfOpen(builder, token);
            builder.InsertHtmlElement(token);
            builder.SetIgnoreNextLineFeed(true);
            builder.SetFramesetOk(false);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "form")
        {
            return ProcessFormStartTag(builder, token);
        }

        if (name == "li")
        {
            return ProcessListItemStartTag(builder, token);
        }

        if (name == "dd" || name == "dt")
        {
            return ProcessDescriptionStartTag(builder, token);
        }

        if (name == "plaintext")
        {
            CloseParagraphIfOpen(builder, token);
            builder.InsertHtmlElement(token);
            builder.RequestTokenizerState(HtmlTokenizerState.PlainText);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "button")
        {
            return ProcessButtonStartTag(builder, token);
        }

        if (HtmlNames.IsFormattingElement(name))
        {
            return ProcessFormattingStartTag(builder, token);
        }

        if (name == "applet" || name == "marquee" || name == "object")
        {
            Reconstruct(builder, token);
            builder.InsertHtmlElement(token);
            builder.GetFormattingElements().AddMarker();
            builder.SetFramesetOk(false);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "table")
        {
            if (builder.GetDocument().GetMode() != HtmlDocumentMode.Quirks)
            {
                CloseParagraphIfOpen(builder, token);
            }

            builder.InsertHtmlElement(token);
            builder.SetFramesetOk(false);
            builder.SetInsertionMode(HtmlInsertionMode.InTable);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsBodyVoidStartTag(name))
        {
            Reconstruct(builder, token);
            InsertVoid(builder, token);
            builder.SetFramesetOk(false);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "input")
        {
            Reconstruct(builder, token);
            InsertVoid(builder, token);
            if (!IsHiddenInput(token))
            {
                builder.SetFramesetOk(false);
            }

            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "param" || name == "source" || name == "track")
        {
            InsertVoid(builder, token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "hr")
        {
            CloseParagraphIfOpen(builder, token);
            InsertVoid(builder, token);
            builder.SetFramesetOk(false);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "image")
        {
            builder.ReportParseError(token,
                "The image start tag was treated as an img start tag.");
            HtmlToken adjusted = HtmlToken.CreateStartTag("img",
                token.GetAttributes(), token.IsSelfClosing(),
                token.GetSourceSpan());
            Reconstruct(builder, token);
            InsertVoid(builder, adjusted);
            if (token.RequiresSelfClosingAcknowledgement())
            {
                token.AcknowledgeSelfClosing();
            }

            builder.SetFramesetOk(false);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "textarea")
        {
            builder.InsertHtmlElement(token);
            builder.SetIgnoreNextLineFeed(true);
            builder.RequestTokenizerState(HtmlTokenizerState.Rcdata);
            builder.SetOriginalInsertionMode(builder.GetInsertionMode());
            builder.SetFramesetOk(false);
            builder.SetInsertionMode(HtmlInsertionMode.Text);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "xmp")
        {
            CloseParagraphIfOpen(builder, token);
            Reconstruct(builder, token);
            builder.SetFramesetOk(false);
            return EnterRawText(builder, token);
        }

        if (name == "iframe")
        {
            builder.SetFramesetOk(false);
            return EnterRawText(builder, token);
        }

        if (name == "noembed"
            || name == "noscript" && builder.IsScriptingEnabled())
        {
            return EnterRawText(builder, token);
        }

        if (name == "select")
        {
            Reconstruct(builder, token);
            builder.InsertHtmlElement(token);
            builder.SetFramesetOk(false);
            builder.SetInsertionMode(IsTableMode(builder.GetInsertionMode())
                ? HtmlInsertionMode.InSelectInTable
                : HtmlInsertionMode.InSelect);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "option" || name == "optgroup")
        {
            return ProcessChoiceStartTag(builder, token);
        }

        if (name == "rb" || name == "rtc" || name == "rp" || name == "rt")
        {
            return ProcessRubyStartTag(builder, token);
        }

        if (name == "math")
        {
            return InsertForeign(builder, token, HtmlNamespace.MathMl);
        }

        if (name == "svg")
        {
            return InsertForeign(builder, token, HtmlNamespace.Svg);
        }

        if (IsIgnoredBodyStartTag(name))
        {
            builder.ReportParseError(token,
                "This start tag is not allowed in the body insertion mode.");
            return HtmlTreeBuilderAction.Consume();
        }

        Reconstruct(builder, token);
        builder.InsertHtmlElement(token);
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessEndTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        string name = token.GetName();
        if (name == "body")
        {
            return ProcessBodyEndTag(builder, token, false);
        }

        if (name == "html")
        {
            return ProcessBodyEndTag(builder, token, true);
        }

        if (IsBlockEndTag(name))
        {
            return CloseScopedElement(builder, token, name, false);
        }

        if (name == "form")
        {
            return ProcessFormEndTag(builder, token);
        }

        if (name == "p")
        {
            if (!builder.GetOpenElements()
                .HasHtmlElementInButtonScope("p"))
            {
                builder.ReportParseError(token,
                    "A p end tag did not have an open p element.");
                builder.InsertGeneratedHtmlElement("p");
            }

            CloseParagraph(builder, token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "li")
        {
            if (!builder.GetOpenElements()
                .HasHtmlElementInListItemScope("li"))
            {
                builder.ReportParseError(token,
                    "An li end tag did not have an open li element.");
                return HtmlTreeBuilderAction.Consume();
            }

            CloseNamedElement(builder, token, "li");
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "dd" || name == "dt")
        {
            return CloseScopedElement(builder, token, name, true);
        }

        if (IsHeading(name))
        {
            return ProcessHeadingEndTag(builder, token);
        }

        if (HtmlNames.IsFormattingElement(name))
        {
            return HtmlAdoptionAgency.Run(builder, token);
        }

        if (name == "applet" || name == "marquee" || name == "object")
        {
            if (!builder.GetOpenElements().HasHtmlElementInScope(name))
            {
                builder.ReportParseError(token,
                    "The special element end tag was not in scope.");
                return HtmlTreeBuilderAction.Consume();
            }

            CloseNamedElement(builder, token, name);
            builder.GetFormattingElements().ClearToLastMarker();
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "br")
        {
            builder.ReportParseError(token,
                "A br end tag was treated as a br start tag.");
            Reconstruct(builder, token);
            HtmlToken adjusted = HtmlToken.CreateStartTag("br",
                new HtmlAttribute[0], false, token.GetSourceSpan());
            InsertVoid(builder, adjusted);
            builder.SetFramesetOk(false);
            return HtmlTreeBuilderAction.Consume();
        }

        if (name == "template")
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        return ProcessAnyOtherEndTag(builder, token);
    }

    internal static HtmlTreeBuilderAction ProcessHtmlStartTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        builder.ReportParseError(token,
            "An html start tag appeared after the document element.");
        if (builder.GetOpenElements().GetLastHtmlElement("template") == null
            && builder.GetOpenElements().GetCount() > 0)
        {
            builder.GetOpenElements().GetRequiredNode(0)
                .MergeAttributesStorage(token.GetAttributes());
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessBodyStartTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        builder.ReportParseError(token,
            "A second body start tag was ignored after merging attributes.");
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (stack.GetCount() < 2
            || !IsHtmlElement(stack.GetNode(1), "body")
            || stack.GetLastHtmlElement("template") != null)
        {
            return HtmlTreeBuilderAction.Consume();
        }

        builder.SetFramesetOk(false);
        stack.GetRequiredNode(1).MergeAttributesStorage(token.GetAttributes());
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessFramesetStartTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        builder.ReportParseError(token,
            "A frameset start tag appeared in the body.");
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (stack.GetCount() < 2 || !IsHtmlElement(stack.GetNode(1), "body")
            || !builder.IsFramesetOk())
        {
            return HtmlTreeBuilderAction.Consume();
        }

        HtmlNode body = stack.GetRequiredNode(1);
        body.DetachStorage();
        while (stack.GetCount() > 1)
        {
            stack.Pop();
        }

        builder.InsertHtmlElement(token);
        builder.SetInsertionMode(HtmlInsertionMode.InFrameset);
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessHeadingStartTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        CloseParagraphIfOpen(builder, token);
        HtmlNode? current = builder.GetOpenElements().GetCurrentNode();
        if (current != null && IsHeading(current.GetLocalNameStorage())
            && current.GetNamespaceStorage() == HtmlNamespace.Html)
        {
            builder.ReportParseError(token,
                "A heading start tag implicitly closed the previous heading.");
            builder.GetOpenElements().Pop();
        }

        builder.InsertHtmlElement(token);
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessFormStartTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (builder.GetFormElement() != null
            && stack.GetLastHtmlElement("template") == null)
        {
            builder.ReportParseError(token,
                "A nested form start tag was ignored.");
            return HtmlTreeBuilderAction.Consume();
        }

        CloseParagraphIfOpen(builder, token);
        HtmlNode? form = builder.InsertHtmlElement(token);
        if (form != null && stack.GetLastHtmlElement("template") == null)
        {
            builder.SetFormElement(form);
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessFormEndTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (stack.GetLastHtmlElement("template") == null)
        {
            HtmlNode? form = builder.GetFormElement();
            builder.SetFormElement(null);
            if (form == null || !stack.HasInScope(form))
            {
                builder.ReportParseError(token,
                    "A form end tag did not match the form pointer in scope.");
                return HtmlTreeBuilderAction.Consume();
            }

            stack.GenerateImpliedEndTags();
            if (stack.GetCurrentNode() != form)
            {
                builder.ReportParseError(token,
                    "The form end tag removed a non-current form element.");
            }

            stack.Remove(form);
            return HtmlTreeBuilderAction.Consume();
        }

        if (!stack.HasHtmlElementInScope("form"))
        {
            builder.ReportParseError(token,
                "A form end tag did not have a form in template scope.");
            return HtmlTreeBuilderAction.Consume();
        }

        stack.GenerateImpliedEndTags();
        if (!IsHtmlElement(stack.GetCurrentNode(), "form"))
        {
            builder.ReportParseError(token,
                "The template form end tag closed intervening elements.");
        }

        stack.PopUntilHtmlElement("form");
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessListItemStartTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        builder.SetFramesetOk(false);
        ClosePriorListOrDescriptionItem(builder, token, "li", "");
        CloseParagraphIfOpen(builder, token);
        builder.InsertHtmlElement(token);
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessDescriptionStartTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        builder.SetFramesetOk(false);
        ClosePriorListOrDescriptionItem(builder, token, "dd", "dt");
        CloseParagraphIfOpen(builder, token);
        builder.InsertHtmlElement(token);
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessButtonStartTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (stack.HasHtmlElementInScope("button"))
        {
            builder.ReportParseError(token,
                "A button start tag implicitly closed an open button.");
            stack.GenerateImpliedEndTags();
            stack.PopUntilHtmlElement("button");
        }

        Reconstruct(builder, token);
        builder.InsertHtmlElement(token);
        builder.SetFramesetOk(false);
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessFormattingStartTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        string name = token.GetName();
        HtmlFormattingList formatting = builder.GetFormattingElements();
        if (name == "a")
        {
            HtmlNode? existing = formatting.GetLastHtmlElement(name);
            if (existing != null)
            {
                builder.ReportParseError(token,
                    "A formatting start tag implicitly closed its active predecessor.");
                HtmlAdoptionAgency.Run(builder, token);
                formatting.RemoveFromListAndStack(existing,
                    builder.GetOpenElements());
            }
        }

        Reconstruct(builder, token);
        if (name == "nobr"
            && builder.GetOpenElements().HasHtmlElementInScope("nobr"))
        {
            builder.ReportParseError(token,
                "A nobr start tag implicitly closed an open nobr element.");
            HtmlAdoptionAgency.Run(builder, token);
            Reconstruct(builder, token);
        }

        HtmlNode? element = builder.InsertHtmlElement(token);
        if (element != null)
        {
            formatting.Add(element, token);
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessChoiceStartTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlNode? current = builder.GetOpenElements().GetCurrentNode();
        if (current != null && IsHtmlElement(current, "option"))
        {
            builder.GetOpenElements().Pop();
        }

        Reconstruct(builder, token);
        builder.InsertHtmlElement(token);
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessRubyStartTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (stack.HasHtmlElementInScope("ruby"))
        {
            if (token.GetName() == "rp" || token.GetName() == "rt")
            {
                stack.GenerateImpliedEndTagsExcept("rtc");
            }
            else
            {
                stack.GenerateImpliedEndTags();
            }

            HtmlNode? current = stack.GetCurrentNode();
            if (current != null && !IsHtmlElement(current, "ruby")
                && !IsHtmlElement(current, "rtc"))
            {
                builder.ReportParseError(token,
                    "Ruby annotation content required implied end tags.");
            }
        }

        builder.InsertHtmlElement(token);
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessBodyEndTag(HtmlTreeBuilder builder,
        HtmlToken token, bool reprocess)
    {
        if (!builder.GetOpenElements().HasHtmlElementInScope("body"))
        {
            builder.ReportParseError(token,
                "A body or html end tag did not have a body element in scope.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (HasUnexpectedOpenElement(builder.GetOpenElements()))
        {
            builder.ReportParseError(token,
                "The body was closed with unexpected open elements.");
        }

        builder.SetInsertionMode(HtmlInsertionMode.AfterBody);
        return reprocess ? HtmlTreeBuilderAction.Reprocess()
            : HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessHeadingEndTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (!HasHeadingInScope(stack))
        {
            builder.ReportParseError(token,
                "A heading end tag did not have a heading in scope.");
            return HtmlTreeBuilderAction.Consume();
        }

        stack.GenerateImpliedEndTags();
        HtmlNode? current = stack.GetCurrentNode();
        if (current == null || !IsHtmlElement(current, token.GetName()))
        {
            builder.ReportParseError(token,
                "The heading end tag did not match the current heading.");
        }

        bool running = true;
        while (running && stack.GetCount() > 0)
        {
            HtmlNode popped = stack.PopRequired();
            running = !IsHeading(popped.GetLocalNameStorage())
                || popped.GetNamespaceStorage() != HtmlNamespace.Html;
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction CloseScopedElement(HtmlTreeBuilder builder,
        HtmlToken token, string name, bool exceptNamed)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        if (!stack.HasHtmlElementInScope(name))
        {
            builder.ReportParseError(token,
                "The end tag did not have a matching element in scope.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (exceptNamed)
        {
            stack.GenerateImpliedEndTagsExcept(name);
        }
        else
        {
            stack.GenerateImpliedEndTags();
        }

        HtmlNode? current = stack.GetCurrentNode();
        if (current == null || !IsHtmlElement(current, name))
        {
            builder.ReportParseError(token,
                "The end tag implicitly closed intervening elements.");
        }

        stack.PopUntilHtmlElement(name);
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessAnyOtherEndTag(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        int index = stack.GetCount() - 1;
        while (index >= 0)
        {
            HtmlNode node = stack.GetRequiredNode(index);
            if (IsHtmlElement(node, token.GetName()))
            {
                stack.GenerateImpliedEndTagsExcept(token.GetName());
                if (node != stack.GetCurrentNode())
                {
                    builder.ReportParseError(token,
                        "The end tag implicitly closed intervening elements.");
                }

                stack.PopUntil(node);
                return HtmlTreeBuilderAction.Consume();
            }

            if (HtmlOpenElementStack.IsSpecialElement(node))
            {
                builder.ReportParseError(token,
                    "The end tag was blocked by a special element.");
                return HtmlTreeBuilderAction.Consume();
            }

            index = index - 1;
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessEndOfFile(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        if (builder.GetTemplateInsertionModeCount() > 0)
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InTemplate);
        }

        if (HasUnexpectedOpenElement(builder.GetOpenElements()))
        {
            builder.ReportParseError(token,
                "The input ended with unexpected open elements.");
        }

        return HtmlTreeBuilderAction.Stop();
    }

    internal static HtmlTreeBuilderAction EnterRawText(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        HtmlNode? element = builder.InsertHtmlElement(token);
        if (element != null)
        {
            builder.RequestTokenizerState(HtmlTokenizerState.RawText);
            builder.SetOriginalInsertionMode(builder.GetInsertionMode());
            builder.SetInsertionMode(HtmlInsertionMode.Text);
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction InsertForeign(HtmlTreeBuilder builder,
        HtmlToken token, HtmlNamespace namespaceKind)
    {
        Reconstruct(builder, token);
        HtmlNode? element = HtmlTreeBuilderForeignContent.InsertElement(
            builder, token, namespaceKind);
        if (element != null && token.IsSelfClosing())
        {
            builder.GetOpenElements().Pop();
            token.AcknowledgeSelfClosing();
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static void ClosePriorListOrDescriptionItem(HtmlTreeBuilder builder,
        HtmlToken token, string firstName, string secondName)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        int index = stack.GetCount() - 1;
        bool running = true;
        while (index >= 0 && running)
        {
            HtmlNode node = stack.GetRequiredNode(index);
            string name = node.GetLocalNameStorage();
            bool matching = node.GetNamespaceStorage() == HtmlNamespace.Html
                && (name == firstName || secondName.Length > 0
                    && name == secondName);
            if (matching)
            {
                CloseNamedElement(builder, token, name);
                running = false;
            }
            else if (HtmlOpenElementStack.IsSpecialElement(node)
                && !IsHtmlElement(node, "address")
                && !IsHtmlElement(node, "div")
                && !IsHtmlElement(node, "p"))
            {
                running = false;
            }

            index = index - 1;
        }
    }

    internal static void CloseParagraphIfOpen(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        if (builder.GetOpenElements().HasHtmlElementInButtonScope("p"))
        {
            CloseParagraph(builder, token);
        }
    }

    internal static void CloseParagraph(HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        stack.GenerateImpliedEndTagsExcept("p");
        HtmlNode? current = stack.GetCurrentNode();
        if (current == null || !IsHtmlElement(current, "p"))
        {
            builder.ReportParseError(token,
                "The p element was closed around intervening elements.");
        }

        stack.PopUntilHtmlElement("p");
    }

    internal static void CloseNamedElement(HtmlTreeBuilder builder, HtmlToken token,
        string name)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        stack.GenerateImpliedEndTagsExcept(name);
        HtmlNode? current = stack.GetCurrentNode();
        if (current == null || !IsHtmlElement(current, name))
        {
            builder.ReportParseError(token,
                "The element was closed around intervening elements.");
        }

        stack.PopUntilHtmlElement(name);
    }

    internal static void Reconstruct(HtmlTreeBuilder builder, HtmlToken token)
    {
        builder.ReconstructActiveFormattingElements(token.GetSourceSpan());
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

    internal static bool HasHeadingInScope(HtmlOpenElementStack stack)
    {
        return stack.HasHtmlElementInScope("h1")
            || stack.HasHtmlElementInScope("h2")
            || stack.HasHtmlElementInScope("h3")
            || stack.HasHtmlElementInScope("h4")
            || stack.HasHtmlElementInScope("h5")
            || stack.HasHtmlElementInScope("h6");
    }

    internal static bool HasUnexpectedOpenElement(HtmlOpenElementStack stack)
    {
        int index = 0;
        while (index < stack.GetCount())
        {
            HtmlNode? node = stack.GetNode(index);
            if (!IsAllowedAtBodyEnd(node))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsAllowedAtBodyEnd(HtmlNode? node)
    {
        if (node == null
            || node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string name = node.GetLocalNameStorage();
        return name == "dd" || name == "dt" || name == "li"
            || name == "optgroup" || name == "option" || name == "p"
            || name == "rb" || name == "rp" || name == "rt"
            || name == "rtc" || name == "tbody" || name == "td"
            || name == "tfoot" || name == "th" || name == "thead"
            || name == "tr" || name == "body" || name == "html";
    }

    internal static bool IsWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!Ascii.IsHtmlWhitespace(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsHtmlElement(HtmlNode? node, string name)
    {
        return node != null
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == name;
    }

    internal static bool IsHeadToken(string name)
    {
        return name == "base" || name == "basefont" || name == "bgsound"
            || name == "link" || name == "meta" || name == "noframes"
            || name == "script" || name == "style" || name == "template"
            || name == "title";
    }

    internal static bool IsBlockStartTag(string name)
    {
        return name == "address" || name == "article" || name == "aside"
            || name == "blockquote" || name == "center" || name == "details"
            || name == "dialog" || name == "dir" || name == "div"
            || name == "dl" || name == "fieldset" || name == "figcaption"
            || name == "figure" || name == "footer" || name == "header"
            || name == "hgroup" || name == "main" || name == "menu"
            || name == "nav" || name == "ol" || name == "p"
            || name == "search" || name == "section" || name == "summary"
            || name == "ul";
    }

    internal static bool IsBlockEndTag(string name)
    {
        return name == "address" || name == "article" || name == "aside"
            || name == "blockquote" || name == "button" || name == "center"
            || name == "details" || name == "dialog" || name == "dir"
            || name == "div" || name == "dl" || name == "fieldset"
            || name == "figcaption" || name == "figure" || name == "footer"
            || name == "header" || name == "hgroup" || name == "listing"
            || name == "main" || name == "menu" || name == "nav"
            || name == "ol" || name == "pre" || name == "search"
            || name == "section" || name == "select" || name == "summary"
            || name == "ul";
    }

    internal static bool IsHeading(string name)
    {
        return name == "h1" || name == "h2" || name == "h3"
            || name == "h4" || name == "h5" || name == "h6";
    }

    internal static bool IsBodyVoidStartTag(string name)
    {
        return name == "area" || name == "br" || name == "embed"
            || name == "img" || name == "keygen" || name == "wbr";
    }

    internal static bool IsIgnoredBodyStartTag(string name)
    {
        return name == "caption" || name == "col" || name == "colgroup"
            || name == "frame" || name == "head" || name == "tbody"
            || name == "td" || name == "tfoot" || name == "th"
            || name == "thead" || name == "tr";
    }

    internal static bool IsTableMode(HtmlInsertionMode mode)
    {
        return mode == HtmlInsertionMode.InTable
            || mode == HtmlInsertionMode.InCaption
            || mode == HtmlInsertionMode.InTableBody
            || mode == HtmlInsertionMode.InRow
            || mode == HtmlInsertionMode.InCell;
    }
}
