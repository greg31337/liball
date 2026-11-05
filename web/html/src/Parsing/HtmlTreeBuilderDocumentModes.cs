using System;

namespace html;

internal static class HtmlTreeBuilderDocumentModes
{
    internal static void Configure(HtmlTreeBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException("builder");
        }

        builder.SetModeHandler(HtmlInsertionMode.Initial, ProcessInitial);
        builder.SetModeHandler(HtmlInsertionMode.BeforeHtml,
            ProcessBeforeHtml);
        builder.SetModeHandler(HtmlInsertionMode.BeforeHead,
            ProcessBeforeHead);
        builder.SetModeHandler(HtmlInsertionMode.InHead, ProcessInHead);
        builder.SetModeHandler(HtmlInsertionMode.InHeadNoScript,
            ProcessInHeadNoScript);
        builder.SetModeHandler(HtmlInsertionMode.AfterHead, ProcessAfterHead);
        builder.SetModeHandler(HtmlInsertionMode.Text, ProcessText);
        builder.SetModeHandler(HtmlInsertionMode.AfterBody, ProcessAfterBody);
        builder.SetModeHandler(HtmlInsertionMode.InFrameset,
            ProcessInFrameset);
        builder.SetModeHandler(HtmlInsertionMode.AfterFrameset,
            ProcessAfterFrameset);
        builder.SetModeHandler(HtmlInsertionMode.AfterAfterBody,
            ProcessAfterAfterBody);
        builder.SetModeHandler(HtmlInsertionMode.AfterAfterFrameset,
            ProcessAfterAfterFrameset);
    }

    internal static HtmlTreeBuilderAction ProcessInitial(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        if (IsWhitespace(token))
        {
            return HtmlTreeBuilderAction.Consume();
        }

        if (token.GetKind() == HtmlTokenKind.Comment)
        {
            builder.InsertCommentAtTarget(token,
                builder.GetDocument().GetRootStorage());
            return HtmlTreeBuilderAction.Consume();
        }

        if (token.GetKind() == HtmlTokenKind.Doctype)
        {
            if (!IsConformingDoctype(token))
            {
                builder.ReportParseError(token,
                    "The document type is not the conforming HTML document type.");
            }

            builder.InsertDoctype(token);
            builder.GetDocument().SetModeStorage(GetDocumentMode(token));
            builder.SetInsertionMode(HtmlInsertionMode.BeforeHtml);
            return HtmlTreeBuilderAction.Consume();
        }

        builder.ReportParseError(token,
            "A document type was expected before the document element.");
        builder.GetDocument().SetModeStorage(HtmlDocumentMode.Quirks);
        builder.SetInsertionMode(HtmlInsertionMode.BeforeHtml);
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessBeforeHtml(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind == HtmlTokenKind.Doctype)
        {
            builder.ReportParseError(token,
                "A document type is not allowed before the html element.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.Comment)
        {
            builder.InsertCommentAtTarget(token,
                builder.GetDocument().GetRootStorage());
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsWhitespace(token))
        {
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "html"))
        {
            builder.InsertHtmlElement(token);
            builder.SetInsertionMode(HtmlInsertionMode.BeforeHead);
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.EndTag)
        {
            string name = token.GetName();
            if (name != "head" && name != "body" && name != "html"
                && name != "br")
            {
                builder.ReportParseError(token,
                    "This end tag is not allowed before the html element.");
                return HtmlTreeBuilderAction.Consume();
            }
        }

        builder.InsertGeneratedHtmlElement("html");
        builder.SetInsertionMode(HtmlInsertionMode.BeforeHead);
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessBeforeHead(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (IsWhitespace(token))
        {
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
                "A document type is not allowed before the head element.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "html"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (IsStartTag(token, "head"))
        {
            HtmlNode? head = builder.InsertHtmlElement(token);
            if (head != null)
            {
                builder.SetHeadElement(head);
            }

            builder.SetInsertionMode(HtmlInsertionMode.InHead);
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.EndTag)
        {
            string name = token.GetName();
            if (name != "head" && name != "body" && name != "html"
                && name != "br")
            {
                builder.ReportParseError(token,
                    "This end tag is not allowed before the head element.");
                return HtmlTreeBuilderAction.Consume();
            }
        }

        HtmlNode? generated = builder.InsertGeneratedHtmlElement("head");
        if (generated != null)
        {
            builder.SetHeadElement(generated);
        }

        builder.SetInsertionMode(HtmlInsertionMode.InHead);
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessInHead(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (IsWhitespace(token))
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
                "A document type is not allowed in the head element.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "html"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (IsHeadVoidStartTag(token))
        {
            InsertVoidElement(builder, token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "title"))
        {
            return EnterTextMode(builder, token, HtmlTokenizerState.Rcdata);
        }

        if (IsStartTag(token, "noscript"))
        {
            if (builder.IsScriptingEnabled())
            {
                return EnterTextMode(builder, token,
                    HtmlTokenizerState.RawText);
            }

            builder.InsertHtmlElement(token);
            builder.SetInsertionMode(HtmlInsertionMode.InHeadNoScript);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "noframes") || IsStartTag(token, "style"))
        {
            return EnterTextMode(builder, token, HtmlTokenizerState.RawText);
        }

        if (IsStartTag(token, "script"))
        {
            return EnterTextMode(builder, token,
                HtmlTokenizerState.ScriptData);
        }

        if (IsEndTag(token, "head"))
        {
            builder.GetOpenElements().Pop();
            builder.SetInsertionMode(HtmlInsertionMode.AfterHead);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "template"))
        {
            return HtmlTreeBuilderTemplateModes.ProcessTemplateStartTag(
                builder, token);
        }

        if (IsEndTag(token, "template"))
        {
            return HtmlTreeBuilderTemplateModes.ProcessTemplateEndTag(
                builder, token);
        }

        if (IsStartTag(token, "head"))
        {
            builder.ReportParseError(token,
                "A second head start tag is not allowed.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.EndTag)
        {
            string name = token.GetName();
            if (name != "body" && name != "html" && name != "br")
            {
                builder.ReportParseError(token,
                    "This end tag is not allowed in the head element.");
                return HtmlTreeBuilderAction.Consume();
            }
        }

        builder.GetOpenElements().Pop();
        builder.SetInsertionMode(HtmlInsertionMode.AfterHead);
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessInHeadNoScript(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind == HtmlTokenKind.Doctype)
        {
            builder.ReportParseError(token,
                "A document type is not allowed in a noscript element.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "html"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (IsEndTag(token, "noscript"))
        {
            builder.GetOpenElements().Pop();
            builder.SetInsertionMode(HtmlInsertionMode.InHead);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsWhitespace(token) || kind == HtmlTokenKind.Comment
            || IsNoScriptHeadStartTag(token))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        if (IsStartTag(token, "head") || IsStartTag(token, "noscript")
            || kind == HtmlTokenKind.EndTag && !IsEndTag(token, "br"))
        {
            builder.ReportParseError(token,
                "This token is not allowed in a noscript element in the head.");
            return HtmlTreeBuilderAction.Consume();
        }

        builder.ReportParseError(token,
            "The noscript element in the head was closed implicitly.");
        builder.GetOpenElements().Pop();
        builder.SetInsertionMode(HtmlInsertionMode.InHead);
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessAfterHead(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (IsWhitespace(token))
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
                "A document type is not allowed after the head element.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "html"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (IsStartTag(token, "body"))
        {
            builder.InsertHtmlElement(token);
            builder.SetFramesetOk(false);
            builder.SetInsertionMode(HtmlInsertionMode.InBody);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "frameset"))
        {
            builder.InsertHtmlElement(token);
            builder.SetInsertionMode(HtmlInsertionMode.InFrameset);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsAfterHeadHeadToken(token))
        {
            builder.ReportParseError(token,
                "Head content appeared after the head element was closed.");
            HtmlNode? head = builder.GetHeadElement();
            if (head == null)
            {
                return HtmlTreeBuilderAction.Consume();
            }

            if (!builder.PushOpenElement(head, token.GetSourceSpan()))
            {
                return HtmlTreeBuilderAction.Consume();
            }

            HtmlTreeBuilderAction action = ProcessInHead(builder, token);
            builder.GetOpenElements().Remove(head);
            return action;
        }

        if (IsStartTag(token, "head"))
        {
            builder.ReportParseError(token,
                "A head start tag is not allowed after the head element.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.EndTag)
        {
            string name = token.GetName();
            if (name != "body" && name != "html" && name != "br")
            {
                builder.ReportParseError(token,
                    "This end tag is not allowed after the head element.");
                return HtmlTreeBuilderAction.Consume();
            }
        }

        builder.InsertGeneratedHtmlElement("body");
        builder.SetInsertionMode(HtmlInsertionMode.InBody);
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessText(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind == HtmlTokenKind.Character)
        {
            string data = token.GetData();
            if (builder.TakeIgnoreNextLineFeed() && data.Length > 0
                && data[0] == '\n')
            {
                data = data.Substring(1);
            }

            builder.InsertCharacterData(data, token.GetSourceSpan());
            return HtmlTreeBuilderAction.Consume();
        }

        builder.TakeIgnoreNextLineFeed();

        if (kind == HtmlTokenKind.EndOfFile)
        {
            builder.ReportParseError(token,
                "The end of the input closed a text element.");
            builder.GetOpenElements().Pop();
            builder.SetInsertionMode(builder.GetOriginalInsertionMode());
            return HtmlTreeBuilderAction.Reprocess();
        }

        if (kind == HtmlTokenKind.EndTag)
        {
            builder.GetOpenElements().Pop();
            builder.SetInsertionMode(builder.GetOriginalInsertionMode());
            return HtmlTreeBuilderAction.Consume();
        }

        builder.ReportParseError(token,
            "This token is not allowed in the text insertion mode.");
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessAfterBody(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (IsWhitespace(token))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (kind == HtmlTokenKind.Comment)
        {
            HtmlOpenElementStack stack = builder.GetOpenElements();
            HtmlNode? target = stack.GetCount() == 0
                ? builder.GetDocument().GetRootStorage()
                : stack.GetNode(0);
            builder.InsertCommentAtTarget(token, target);
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.Doctype)
        {
            builder.ReportParseError(token,
                "A document type is not allowed after the body.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "html"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (IsEndTag(token, "html"))
        {
            if (builder.GetOpenElements().GetFragmentContext() != null)
            {
                builder.ReportParseError(token,
                    "An html end tag was ignored while parsing a fragment.");
                return HtmlTreeBuilderAction.Consume();
            }

            builder.SetInsertionMode(HtmlInsertionMode.AfterAfterBody);
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.EndOfFile)
        {
            return HtmlTreeBuilderAction.Stop();
        }

        builder.ReportParseError(token,
            "Content after the body was processed inside the body.");
        builder.SetInsertionMode(HtmlInsertionMode.InBody);
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessInFrameset(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (IsWhitespace(token))
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
                "A document type is not allowed in a frameset.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "html"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (IsStartTag(token, "frameset"))
        {
            builder.InsertHtmlElement(token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsEndTag(token, "frameset"))
        {
            HtmlOpenElementStack stack = builder.GetOpenElements();
            if (IsCurrentHtmlElement(stack, "html"))
            {
                builder.ReportParseError(token,
                    "A frameset end tag was ignored at the root element.");
                return HtmlTreeBuilderAction.Consume();
            }

            stack.Pop();
            if (stack.GetFragmentContext() == null
                && !IsCurrentHtmlElement(stack, "frameset"))
            {
                builder.SetInsertionMode(HtmlInsertionMode.AfterFrameset);
            }

            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "frame"))
        {
            InsertVoidElement(builder, token);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "noframes"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        if (kind == HtmlTokenKind.EndOfFile)
        {
            if (!IsCurrentHtmlElement(builder.GetOpenElements(), "html"))
            {
                builder.ReportParseError(token,
                    "The input ended with an open frameset element.");
            }

            return HtmlTreeBuilderAction.Stop();
        }

        builder.ReportParseError(token,
            "The token was ignored in a frameset.");
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessAfterFrameset(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (IsWhitespace(token))
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
                "A document type is not allowed after a frameset.");
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "html"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (IsEndTag(token, "html"))
        {
            builder.SetInsertionMode(HtmlInsertionMode.AfterAfterFrameset);
            return HtmlTreeBuilderAction.Consume();
        }

        if (IsStartTag(token, "noframes"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        if (kind == HtmlTokenKind.EndOfFile)
        {
            return HtmlTreeBuilderAction.Stop();
        }

        builder.ReportParseError(token,
            "The token was ignored after the frameset.");
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessAfterAfterBody(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind == HtmlTokenKind.Comment)
        {
            builder.InsertCommentAtTarget(token,
                builder.GetDocument().GetRootStorage());
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.Doctype || IsWhitespace(token)
            || IsStartTag(token, "html"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (kind == HtmlTokenKind.EndOfFile)
        {
            return HtmlTreeBuilderAction.Stop();
        }

        builder.ReportParseError(token,
            "Content after the html element was processed in the body.");
        builder.SetInsertionMode(HtmlInsertionMode.InBody);
        return HtmlTreeBuilderAction.Reprocess();
    }

    internal static HtmlTreeBuilderAction ProcessAfterAfterFrameset(
        HtmlTreeBuilder builder, HtmlToken token)
    {
        HtmlTokenKind kind = token.GetKind();
        if (kind == HtmlTokenKind.Comment)
        {
            builder.InsertCommentAtTarget(token,
                builder.GetDocument().GetRootStorage());
            return HtmlTreeBuilderAction.Consume();
        }

        if (kind == HtmlTokenKind.Doctype || IsWhitespace(token)
            || IsStartTag(token, "html"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InBody);
        }

        if (kind == HtmlTokenKind.EndOfFile)
        {
            return HtmlTreeBuilderAction.Stop();
        }

        if (IsStartTag(token, "noframes"))
        {
            return HtmlTreeBuilderAction.ProcessUsingMode(
                HtmlInsertionMode.InHead);
        }

        builder.ReportParseError(token,
            "The token was ignored after the frameset document.");
        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction EnterTextMode(HtmlTreeBuilder builder,
        HtmlToken token, HtmlTokenizerState tokenizerState)
    {
        HtmlNode? element = builder.InsertHtmlElement(token);
        if (element != null)
        {
            builder.RequestTokenizerState(tokenizerState);
            builder.SetOriginalInsertionMode(builder.GetInsertionMode());
            builder.SetInsertionMode(HtmlInsertionMode.Text);
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static void InsertVoidElement(HtmlTreeBuilder builder, HtmlToken token)
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

    internal static bool IsConformingDoctype(HtmlToken token)
    {
        if (!token.HasName()
            || !Ascii.EqualsIgnoreCase(token.GetName(), "html")
            || token.HasPublicIdentifier() || token.IsForceQuirks())
        {
            return false;
        }

        return !token.HasSystemIdentifier()
            || Ascii.EqualsIgnoreCase(token.GetSystemIdentifier(),
                "about:legacy-compat");
    }

    internal static HtmlDocumentMode GetDocumentMode(HtmlToken token)
    {
        bool hasSystemIdentifier = token.HasSystemIdentifier();
        bool hasNonEmptySystemIdentifier = hasSystemIdentifier
            && token.GetSystemIdentifier().Length != 0;
        if (token.IsForceQuirks() || !token.HasName()
            || !Ascii.EqualsIgnoreCase(token.GetName(), "html")
            || token.HasPublicIdentifier()
                && HtmlDoctypeTables.IsQuirksPublicIdentifier(
                    token.GetPublicIdentifier(), !hasSystemIdentifier)
            || hasSystemIdentifier
                && HtmlDoctypeTables.IsQuirksSystemIdentifier(
                    token.GetSystemIdentifier()))
        {
            return HtmlDocumentMode.Quirks;
        }

        if (token.HasPublicIdentifier()
            && HtmlDoctypeTables.IsLimitedQuirksPublicIdentifier(
                token.GetPublicIdentifier(), hasNonEmptySystemIdentifier))
        {
            return HtmlDocumentMode.LimitedQuirks;
        }

        return HtmlDocumentMode.NoQuirks;
    }

    internal static bool IsWhitespace(HtmlToken token)
    {
        if (token.GetKind() != HtmlTokenKind.Character)
        {
            return false;
        }

        string data = token.GetData();
        int index = 0;
        while (index < data.Length)
        {
            if (!Ascii.IsHtmlWhitespace(data[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
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

    internal static bool IsHeadVoidStartTag(HtmlToken token)
    {
        if (token.GetKind() != HtmlTokenKind.StartTag)
        {
            return false;
        }

        string name = token.GetName();
        return name == "base" || name == "basefont" || name == "bgsound"
            || name == "link" || name == "meta";
    }

    internal static bool IsNoScriptHeadStartTag(HtmlToken token)
    {
        if (token.GetKind() != HtmlTokenKind.StartTag)
        {
            return false;
        }

        string name = token.GetName();
        return name == "basefont" || name == "bgsound" || name == "link"
            || name == "meta" || name == "noframes" || name == "style";
    }

    internal static bool IsAfterHeadHeadToken(HtmlToken token)
    {
        if (IsEndTag(token, "template"))
        {
            return true;
        }

        if (token.GetKind() != HtmlTokenKind.StartTag)
        {
            return false;
        }

        string name = token.GetName();
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
