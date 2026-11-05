using System;

namespace html;

internal static class HtmlTreeBuilderForeignContent
{
    internal static void Configure(HtmlTreeBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException("builder");
        }

        builder.SetForeignHandler(Process);
    }

    internal static HtmlNode? InsertElement(HtmlTreeBuilder builder,
        HtmlToken token, HtmlNamespace namespaceKind)
    {
        string name = namespaceKind == HtmlNamespace.Svg
            ? HtmlForeignNames.AdjustSvgTagName(token.GetName())
            : token.GetName();
        HtmlAttribute[] attributes = AdjustAttributes(token, namespaceKind);
        ValidateNamespaceDeclarations(builder, token, attributes,
            namespaceKind);
        HtmlToken adjusted = HtmlToken.CreateStartTag(name, attributes,
            token.IsSelfClosing(), token.GetSourceSpan());
        return builder.InsertElement(adjusted, namespaceKind,
            HtmlNamespaceData.GetUri(namespaceKind), name, name, "", false);
    }

    internal static HtmlTreeBuilderAction Process(HtmlTreeBuilder builder,
        HtmlToken token)
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
                "A document type is not allowed in foreign content.");
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
            builder.GetInsertionMode());
    }

    internal static HtmlTreeBuilderAction ProcessCharacter(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        string data = token.GetData();
        char[]? replaced = null;
        bool nonWhitespace = false;
        int index = 0;
        while (index < data.Length)
        {
            char value = data[index];
            if (value == '\0')
            {
                if (replaced == null)
                {
                    replaced = data.ToCharArray();
                }

                replaced[index] = '\uFFFD';
            }
            else if (!Ascii.IsHtmlWhitespace(value))
            {
                nonWhitespace = true;
            }

            index = index + 1;
        }

        if (replaced != null)
        {
            builder.ReportParseError(token,
                "A null character in foreign content was replaced.");
            data = new string(replaced);
        }

        builder.InsertCharacterData(data, token.GetSourceSpan());
        if (nonWhitespace)
        {
            builder.SetFramesetOk(false);
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessStartTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        if (IsHtmlBreakoutStartTag(token))
        {
            return BreakOutToHtml(builder, token);
        }

        HtmlNode? adjusted = builder.GetOpenElements()
            .GetAdjustedCurrentNode();
        if (adjusted == null)
        {
            throw new InvalidOperationException(
                "Foreign-content processing requires a current node.");
        }

        HtmlNamespace namespaceKind = adjusted.GetNamespaceStorage();
        HtmlNode? element = InsertElement(builder, token, namespaceKind);
        if (element != null && token.IsSelfClosing())
        {
            builder.GetOpenElements().Pop();
            token.AcknowledgeSelfClosing();
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction ProcessEndTag(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        HtmlOpenElementStack stack = builder.GetOpenElements();
        HtmlNode current = stack.GetRequiredCurrentNode();
        if (current.GetNamespaceStorage() == HtmlNamespace.Svg
            && current.GetLocalNameStorage() == "script"
            && token.GetName() == "script")
        {
            stack.Pop();
            return HtmlTreeBuilderAction.Consume();
        }

        if (token.GetName() == "br" || token.GetName() == "p")
        {
            return BreakOutToHtml(builder, token);
        }

        if (!Ascii.EqualsIgnoreCase(current.GetLocalNameStorage(),
            token.GetName()))
        {
            builder.ReportParseError(token,
                "A foreign-content end tag did not match the current node.");
        }

        int index = stack.GetCount() - 1;
        while (index >= 0)
        {
            HtmlNode node = stack.GetRequiredNode(index);
            if (Ascii.EqualsIgnoreCase(node.GetLocalNameStorage(),
                token.GetName()))
            {
                while (stack.GetCount() > index)
                {
                    stack.Pop();
                }

                return HtmlTreeBuilderAction.Consume();
            }

            index = index - 1;
            if (index >= 0 && stack.GetRequiredNode(index).GetNamespaceStorage()
                == HtmlNamespace.Html)
            {
                return HtmlTreeBuilderAction.ProcessUsingMode(
                    builder.GetInsertionMode());
            }
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static HtmlTreeBuilderAction BreakOutToHtml(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        builder.ReportParseError(token,
            "An HTML token ended the current foreign-content context.");
        HtmlOpenElementStack stack = builder.GetOpenElements();
        HtmlNode? current = stack.GetCurrentNode();
        while (current != null
            && current.GetNamespaceStorage() != HtmlNamespace.Html
            && !IsMathMlTextIntegrationPoint(current)
            && !IsHtmlIntegrationPoint(current))
        {
            stack.Pop();
            current = stack.GetCurrentNode();
        }

        return HtmlTreeBuilderAction.ProcessUsingMode(
            builder.GetInsertionMode());
    }

    internal static HtmlAttribute[] AdjustAttributes(HtmlToken token,
        HtmlNamespace namespaceKind)
    {
        HtmlAttribute[] source = token.GetAttributes();
        HtmlAttribute[] result = new HtmlAttribute[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            HtmlAttribute attribute = source[index];
            HtmlForeignAttributeAdjustment foreign =
                HtmlForeignNames.GetForeignAttributeAdjustment(
                    attribute.GetName());
            if (foreign.GetFound())
            {
                string qualifiedName = foreign.GetPrefix().Length == 0
                    ? foreign.GetLocalName()
                    : foreign.GetPrefix() + ":" + foreign.GetLocalName();
                result[index] = HtmlAttribute.CreateNamespaced(
                    qualifiedName, foreign.GetLocalName(),
                    foreign.GetPrefix(), foreign.GetNamespaceUri(),
                    attribute.GetValue(), attribute.GetSourceSpan());
            }
            else if (attribute.GetNamespaceUri().Length != 0
                || attribute.GetPrefix().Length != 0)
            {
                result[index] = attribute;
            }
            else
            {
                string name = attribute.GetName();
                if (namespaceKind == HtmlNamespace.MathMl)
                {
                    name = HtmlForeignNames.AdjustMathMlAttributeName(name);
                }
                else if (namespaceKind == HtmlNamespace.Svg)
                {
                    name = HtmlForeignNames.AdjustSvgAttributeName(name);
                }

                result[index] = HtmlAttribute.CreateNamespaced(name, name,
                    "", "", attribute.GetValue(),
                    attribute.GetSourceSpan());
            }

            index = index + 1;
        }

        return result;
    }

    internal static void ValidateNamespaceDeclarations(HtmlTreeBuilder builder,
        HtmlToken token, HtmlAttribute[] attributes,
        HtmlNamespace namespaceKind)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            HtmlAttribute attribute = attributes[index];
            if (attribute.GetNamespaceUri()
                    == HtmlNamespaceData.GetUri(HtmlNamespace.Xmlns)
                && attribute.GetPrefix().Length == 0
                && attribute.GetLocalName() == "xmlns"
                && attribute.GetValue()
                    != HtmlNamespaceData.GetUri(namespaceKind))
            {
                builder.ReportParseError(token,
                    "The default namespace declaration does not match the foreign element namespace.");
            }
            else if (attribute.GetNamespaceUri()
                    == HtmlNamespaceData.GetUri(HtmlNamespace.Xmlns)
                && attribute.GetPrefix() == "xmlns"
                && attribute.GetLocalName() == "xlink"
                && attribute.GetValue()
                    != HtmlNamespaceData.GetUri(HtmlNamespace.XLink))
            {
                builder.ReportParseError(token,
                    "The xlink namespace declaration has an invalid value.");
            }

            index = index + 1;
        }
    }

    internal static bool IsHtmlBreakoutStartTag(HtmlToken token)
    {
        string name = token.GetName();
        if (name == "b" || name == "big" || name == "blockquote"
            || name == "body" || name == "br" || name == "center"
            || name == "code" || name == "dd" || name == "div"
            || name == "dl" || name == "dt" || name == "em"
            || name == "embed" || name == "h1" || name == "h2"
            || name == "h3" || name == "h4" || name == "h5"
            || name == "h6" || name == "head" || name == "hr"
            || name == "i" || name == "img" || name == "li"
            || name == "listing" || name == "menu" || name == "meta"
            || name == "nobr" || name == "ol" || name == "p"
            || name == "pre" || name == "ruby" || name == "s"
            || name == "small" || name == "span" || name == "strong"
            || name == "strike" || name == "sub" || name == "sup"
            || name == "table" || name == "tt" || name == "u"
            || name == "ul" || name == "var")
        {
            return true;
        }

        if (name != "font")
        {
            return false;
        }

        HtmlAttribute[] attributes = token.GetAttributes();
        int index = 0;
        while (index < attributes.Length)
        {
            string attributeName = attributes[index].GetName();
            if (Ascii.EqualsIgnoreCase(attributeName, "color")
                || Ascii.EqualsIgnoreCase(attributeName, "face")
                || Ascii.EqualsIgnoreCase(attributeName, "size"))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsMathMlTextIntegrationPoint(HtmlNode node)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.MathMl)
        {
            return false;
        }

        string name = node.GetLocalNameStorage();
        return name == "mi" || name == "mo" || name == "mn"
            || name == "ms" || name == "mtext";
    }

    internal static bool IsHtmlIntegrationPoint(HtmlNode node)
    {
        HtmlNamespace namespaceKind = node.GetNamespaceStorage();
        string name = node.GetLocalNameStorage();
        if (namespaceKind == HtmlNamespace.Svg)
        {
            return name == "foreignObject" || name == "desc"
                || name == "title";
        }

        if (namespaceKind != HtmlNamespace.MathMl
            || name != "annotation-xml")
        {
            return false;
        }

        string? encoding = node.GetAttribute("encoding");
        return encoding != null
            && (Ascii.EqualsIgnoreCase(encoding, "text/html")
                || Ascii.EqualsIgnoreCase(encoding,
                    "application/xhtml+xml"));
    }
}
