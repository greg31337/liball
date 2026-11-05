using System;
using System.Text;
using System.Threading;

namespace html;

internal class HtmlMarkdownInlineWriter
{
    internal HtmlCompiledMarkdownOptions options;
    internal CancellationToken cancellationToken;
    internal StringBuilder output;
    internal HtmlMarkdownContextStack contexts;
    internal HtmlMarkdownLinks links;
    internal bool pendingSpace;
    internal int pendingBreak;
    internal bool leadingWhitespace;

    internal HtmlMarkdownInlineWriter(HtmlCompiledMarkdownOptions optionsValue,
        CancellationToken cancellationTokenValue, bool lineStart,
        HtmlMarkdownLinks linksValue)
    {
        if (optionsValue == null)
        {
            throw new ArgumentNullException("optionsValue");
        }

        if (linksValue == null)
        {
            throw new ArgumentNullException("linksValue");
        }

        this.options = optionsValue;
        this.cancellationToken = cancellationTokenValue;
        this.output = new StringBuilder();
        this.contexts = new HtmlMarkdownContextStack(optionsValue);
        this.links = linksValue;
        this.contexts.SetLineStart(lineStart);
        this.pendingSpace = false;
        this.pendingBreak = 0;
        this.leadingWhitespace = false;
    }

    internal void WriteText(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        StringBuilder token = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (Ascii.IsHtmlWhitespace(current))
            {
                this.FlushToken(token);
                this.RequestSpace();
            }
            else
            {
                token.Append(current);
            }

            index = index + 1;
        }

        this.FlushToken(token);
    }

    internal void WriteSoftBreak()
    {
        this.RequestBreak(1);
    }

    internal void WriteHardBreak()
    {
        this.RequestBreak(2);
    }

    internal void VisitChildren(HtmlNode node)
    {
        HtmlNode? container = node.GetTemplateContentStorage();
        if (container == null)
        {
            container = node;
        }

        HtmlNode[] children = container.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            this.VisitNode(children[index]);
            index = index + 1;
        }
    }

    internal void VisitNode(HtmlNode node)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        HtmlNodeKind kind = node.GetKindStorage();
        if (kind == HtmlNodeKind.Text)
        {
            this.WriteText(node.GetData());
            return;
        }

        if (kind != HtmlNodeKind.Element && kind != HtmlNodeKind.Fragment)
        {
            return;
        }

        if (kind == HtmlNodeKind.Fragment)
        {
            this.VisitChildren(node);
            return;
        }

        if (HtmlMarkdownElementRules.IsHiddenOrIgnored(node))
        {
            return;
        }

        HtmlMarkdownElementAction elementAction =
            HtmlMarkdownElementRules.Resolve(node, this.options,
                this.cancellationToken);
        if (elementAction == HtmlMarkdownElementAction.Drop)
        {
            return;
        }

        if (elementAction == HtmlMarkdownElementAction.Unwrap)
        {
            this.VisitChildren(node);
            return;
        }

        if (elementAction == HtmlMarkdownElementAction.PreserveHtml
            || elementAction == HtmlMarkdownElementAction.EscapeHtml)
        {
            this.WriteLiteral(HtmlMarkdownElementRules.RenderHtml(node,
                this.options, this.cancellationToken,
                elementAction == HtmlMarkdownElementAction.EscapeHtml));
            return;
        }

        if (elementAction == HtmlMarkdownElementAction.Text)
        {
            this.WriteText(HtmlMarkdownElementRules.GetVisibleText(node,
                this.cancellationToken));
            return;
        }

        if (elementAction == HtmlMarkdownElementAction.Error)
        {
            throw HtmlMarkdownElementRules.CreateUnknownElementException(
                node);
        }

        string name = node.GetNamespaceStorage() == HtmlNamespace.Html
            ? node.GetLocalNameStorage() : "";
        if (name == "br")
        {
            this.WriteHardBreak();
            return;
        }

        if (this.options.GetEnableFootnotes() && name == "a")
        {
            string footnote =
                HtmlMarkdownExtensions.GetFootnoteReferenceIdentifier(node);
            if (footnote.Length > 0)
            {
                this.FlushPending();
                this.AppendLiteral("[^" + footnote + "]");
                return;
            }

            if (HtmlMarkdownExtensions.IsFootnoteBacklink(node))
            {
                return;
            }
        }

        if (this.options.GetEnableTrustedRawHtml()
            && HtmlMarkdownExtensions.IsTrustedRawInlineExtension(node))
        {
            this.FlushPending();
            this.AppendLiteral(HtmlMarkdownExtensions.RenderTrustedRaw(node,
                this.options, this.cancellationToken));
            return;
        }

        if (name == "sub" && this.options.GetEnableSubscript())
        {
            this.WriteExtensionSpan(node, '~');
            return;
        }

        if (name == "sup" && this.options.GetEnableSuperscript())
        {
            if (this.options.GetEnableFootnotes()
                && HtmlMarkdownExtensions.ContainsFootnoteReference(node))
            {
                this.VisitChildren(node);
            }
            else
            {
                this.WriteExtensionSpan(node, '^');
            }

            return;
        }

        if (name == "a")
        {
            this.WriteLink(node);
            return;
        }

        if (name == "img")
        {
            this.WriteImage(node);
            return;
        }

        if (name == "em" || name == "i")
        {
            this.WriteFormatted(node, HtmlMarkdownContextKind.Emphasis,
                this.options.GetEmphasisMarker());
            return;
        }

        if (name == "strong" || name == "b")
        {
            this.WriteFormatted(node, HtmlMarkdownContextKind.Strong,
                this.options.GetStrongMarker());
            return;
        }

        if ((name == "del" || name == "s" || name == "strike")
            && this.options.GetEnableStrikethrough())
        {
            this.WriteFormatted(node, HtmlMarkdownContextKind.Text,
                "~~");
            return;
        }

        if (name == "code")
        {
            this.WriteCodeSpan(node);
            return;
        }

        this.VisitChildren(node);
    }

    internal string Complete()
    {
        this.pendingBreak = 0;
        return this.output.ToString();
    }

    internal bool HadLeadingWhitespace()
    {
        return this.leadingWhitespace;
    }

    internal bool HasTrailingWhitespace()
    {
        return this.pendingSpace;
    }

    internal void WriteLiteral(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.FlushPending();
        this.AppendLiteral(value);
    }

    internal void WriteFormatted(HtmlNode node, HtmlMarkdownContextKind kind,
        string delimiter)
    {
        HtmlMarkdownInlineWriter content = new HtmlMarkdownInlineWriter(
            this.options, this.cancellationToken, false, this.links);
        content.contexts.PushWithDelimiter(kind, delimiter);
        content.VisitChildren(node);
        string value = content.Complete();
        content.contexts.Pop();
        if (content.HadLeadingWhitespace())
        {
            this.RequestSpace();
        }

        if (value.Length > 0)
        {
            this.FlushPending();
            this.AppendLiteral(delimiter);
            this.AppendLiteral(value);
            this.AppendLiteral(delimiter);
        }

        if (content.HasTrailingWhitespace())
        {
            this.RequestSpace();
        }
    }

    internal void WriteCodeSpan(HtmlNode node)
    {
        HtmlMarkdownInlineCode code = HtmlMarkdownCode.RenderInline(node,
            this.options, this.cancellationToken);
        string value = code.GetValue();
        if (code.HasLeadingWhitespace())
        {
            this.RequestSpace();
        }

        if (value.Length > 0)
        {
            this.FlushPending();
            this.AppendLiteral(value);
        }

        if (code.HasTrailingWhitespace())
        {
            this.RequestSpace();
        }
    }

    internal void WriteLink(HtmlNode node)
    {
        HtmlMarkdownInlineWriter content = new HtmlMarkdownInlineWriter(
            this.options, this.cancellationToken, false, this.links);
        content.contexts.PushWithDelimiter(HtmlMarkdownContextKind.LinkText, "");
        content.VisitChildren(node);
        string label = content.Complete();
        content.contexts.Pop();
        if (content.HadLeadingWhitespace())
        {
            this.RequestSpace();
        }

        string value = this.links.RenderLink(node, label);
        if (value.Length > 0)
        {
            this.FlushPending();
            this.AppendLiteral(value);
        }

        if (content.HasTrailingWhitespace())
        {
            this.RequestSpace();
        }
    }

    internal void WriteImage(HtmlNode node)
    {
        string value = this.links.RenderImage(node);
        if (value.Length > 0)
        {
            this.FlushPending();
            this.AppendLiteral(value);
        }
    }

    internal void WriteExtensionSpan(HtmlNode node, char delimiter)
    {
        HtmlMarkdownInlineWriter content = new HtmlMarkdownInlineWriter(
            this.options, this.cancellationToken, false, this.links);
        content.VisitChildren(node);
        string value = content.Complete();
        if (content.HadLeadingWhitespace())
        {
            this.RequestSpace();
        }

        if (value.Length > 0)
        {
            this.FlushPending();
            this.AppendLiteral(delimiter.ToString());
            this.AppendLiteral(
                HtmlMarkdownExtensions.EscapeDelimitedContent(value,
                    delimiter));
            this.AppendLiteral(delimiter.ToString());
        }

        if (content.HasTrailingWhitespace())
        {
            this.RequestSpace();
        }
    }

    internal void RequestSpace()
    {
        if (this.output.Length == 0 && this.pendingBreak == 0)
        {
            this.leadingWhitespace = true;
        }

        this.pendingSpace = true;
    }

    internal void RequestBreak(int strength)
    {
        this.pendingSpace = false;
        if (this.output.Length == 0)
        {
            return;
        }

        if (this.pendingBreak > 0)
        {
            this.EmitBreak(this.pendingBreak);
        }

        this.pendingBreak = strength;
    }

    internal void FlushToken(StringBuilder token)
    {
        if (token.Length == 0)
        {
            return;
        }

        this.FlushPending();
        this.output.Append(this.contexts.Escape(token.ToString()));
        token.Clear();
    }

    internal void FlushPending()
    {
        if (this.pendingBreak > 0)
        {
            this.EmitBreak(this.pendingBreak);
            this.pendingBreak = 0;
            this.pendingSpace = false;
            return;
        }

        if (this.pendingSpace && this.output.Length > 0)
        {
            this.AppendLiteral(" ");
        }

        this.pendingSpace = false;
    }

    internal void EmitBreak(int strength)
    {
        if (strength == 2)
        {
            this.AppendLiteral(this.options.GetHardBreakStyle()
                    == HtmlMarkdownHardBreakStyle.Backslash
                ? "\\" : "  ");
        }

        this.AppendLiteral(this.options.GetNewLine());
    }

    internal void AppendLiteral(string value)
    {
        this.output.Append(value);
        this.contexts.ObserveLiteral(value);
    }

}

internal class HtmlMarkdownWriter
{
    internal HtmlCompiledMarkdownOptions options;
    internal CancellationToken cancellationToken;
    internal StringBuilder output;
    internal HtmlMarkdownInlineWriter flow;
    internal HtmlMarkdownLinks links;
    internal bool ownsLinks;

    internal HtmlMarkdownWriter(HtmlCompiledMarkdownOptions optionsValue,
        CancellationToken cancellationTokenValue,
        HtmlMarkdownLinks? linksValue)
    {
        if (optionsValue == null)
        {
            throw new ArgumentNullException("optionsValue");
        }

        this.options = optionsValue;
        this.cancellationToken = cancellationTokenValue;
        this.output = new StringBuilder();
        this.links = linksValue == null
            ? new HtmlMarkdownLinks(optionsValue, cancellationTokenValue)
            : linksValue;
        this.ownsLinks = linksValue == null;
        this.flow = new HtmlMarkdownInlineWriter(optionsValue,
            cancellationTokenValue, true, this.links);
    }

    internal static HtmlMarkdownWriter Create(
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken)
    {
        return new HtmlMarkdownWriter(options, cancellationToken, null);
    }

    internal static HtmlMarkdownWriter Create(
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, HtmlMarkdownLinks links)
    {
        if (links == null)
        {
            throw new ArgumentNullException("links");
        }

        return new HtmlMarkdownWriter(options, cancellationToken, links);
    }

    internal void WriteNode(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        this.VisitNode(node);
    }

    internal void WriteText(string value)
    {
        this.flow.WriteText(value);
    }

    internal void WriteSoftBreak()
    {
        this.flow.WriteSoftBreak();
    }

    internal void WriteHardBreak()
    {
        this.flow.WriteHardBreak();
    }

    internal string Complete()
    {
        this.FlushFlow();
        string body = this.output.ToString();
        return this.ownsLinks ? this.links.AppendDefinitions(body) : body;
    }

    internal void VisitNode(HtmlNode node)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        HtmlNodeKind kind = node.GetKindStorage();
        if (kind == HtmlNodeKind.Text)
        {
            this.flow.VisitNode(node);
            return;
        }

        if (kind != HtmlNodeKind.Document
            && kind != HtmlNodeKind.Fragment
            && kind != HtmlNodeKind.Element)
        {
            return;
        }

        if (kind == HtmlNodeKind.Document || kind == HtmlNodeKind.Fragment)
        {
            this.VisitChildren(node);
            return;
        }

        if (HtmlMarkdownElementRules.IsHiddenOrIgnored(node))
        {
            return;
        }

        HtmlMarkdownElementAction elementAction =
            HtmlMarkdownElementRules.Resolve(node, this.options,
                this.cancellationToken);
        if (elementAction == HtmlMarkdownElementAction.Drop)
        {
            return;
        }

        if (elementAction == HtmlMarkdownElementAction.Unwrap)
        {
            this.VisitChildren(node);
            return;
        }

        if (elementAction == HtmlMarkdownElementAction.PreserveHtml
            || elementAction == HtmlMarkdownElementAction.EscapeHtml)
        {
            this.flow.WriteLiteral(HtmlMarkdownElementRules.RenderHtml(node,
                this.options, this.cancellationToken,
                elementAction == HtmlMarkdownElementAction.EscapeHtml));
            return;
        }

        if (elementAction == HtmlMarkdownElementAction.Text)
        {
            this.flow.WriteText(HtmlMarkdownElementRules.GetVisibleText(node,
                this.cancellationToken));
            return;
        }

        if (elementAction == HtmlMarkdownElementAction.Error)
        {
            throw HtmlMarkdownElementRules.CreateUnknownElementException(
                node);
        }

        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            this.flow.VisitNode(node);
            return;
        }

        string name = node.GetLocalNameStorage();
        int headingLevel = GetHeadingLevel(name);
        if (name == "pre")
        {
            this.FlushFlow();
            this.AppendBlock(HtmlMarkdownCode.RenderBlock(node,
                this.options, this.cancellationToken));
            return;
        }

        if (name == "table")
        {
            this.FlushFlow();
            this.AppendBlock(HtmlMarkdownTables.Render(node, this.options,
                this.cancellationToken, this.links));
            return;
        }

        if (this.options.GetEnableFootnotes()
            && HtmlMarkdownExtensions.IsFootnoteContainer(node))
        {
            this.FlushFlow();
            this.AppendBlock(
                HtmlMarkdownExtensions.RenderFootnoteContainer(node,
                    this.options, this.cancellationToken, this.links));
            return;
        }

        if (this.options.GetEnableFootnotes()
            && HtmlMarkdownExtensions.IsFootnoteDefinition(node))
        {
            this.FlushFlow();
            this.AppendBlock(
                HtmlMarkdownExtensions.RenderFootnoteDefinition(node,
                    this.options, this.cancellationToken, this.links));
            return;
        }

        if (name == "dl" && this.options.GetEnableDefinitionLists())
        {
            this.FlushFlow();
            this.AppendBlock(HtmlMarkdownExtensions.RenderDefinitionList(
                node, this.options, this.cancellationToken, this.links));
            return;
        }

        if (this.options.GetEnableTrustedRawHtml()
            && HtmlMarkdownExtensions.IsTrustedRawBlockExtension(node))
        {
            this.FlushFlow();
            this.AppendBlock(HtmlMarkdownExtensions.RenderTrustedRaw(node,
                this.options, this.cancellationToken));
            return;
        }

        if (name == "blockquote")
        {
            this.FlushFlow();
            this.WriteBlockquote(node);
            return;
        }

        if (name == "ol" || name == "ul" || name == "menu")
        {
            this.FlushFlow();
            if (this.options.GetEnableFootnotes()
                && HtmlMarkdownExtensions.IsFootnoteDefinitionList(node))
            {
                this.AppendBlock(
                    HtmlMarkdownExtensions.RenderFootnoteContainer(node,
                        this.options, this.cancellationToken, this.links));
            }
            else
            {
                this.AppendBlock(HtmlMarkdownLists.Render(node, this.options,
                    this.cancellationToken, this.links));
            }

            return;
        }

        if (name == "p")
        {
            this.FlushFlow();
            this.AppendBlock(this.RenderInlineChildren(node, true));
            return;
        }

        if (headingLevel > 0)
        {
            this.FlushFlow();
            this.WriteHeading(node, headingLevel);
            return;
        }

        if (name == "hr")
        {
            this.FlushFlow();
            this.AppendBlock(this.options.GetThematicBreakMarker());
            return;
        }

        if (IsBlockContainer(name))
        {
            this.FlushFlow();
            this.VisitChildren(node);
            this.FlushFlow();
            return;
        }

        this.flow.VisitNode(node);
    }

    internal void VisitChildren(HtmlNode node)
    {
        HtmlNode? container = node.GetTemplateContentStorage();
        if (container == null)
        {
            container = node;
        }

        HtmlNode[] children = container.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            this.VisitNode(children[index]);
            index = index + 1;
        }
    }

    internal void WriteHeading(HtmlNode node, int level)
    {
        bool setext = this.options.GetHeadingStyle()
                == HtmlMarkdownHeadingStyle.Setext
            && level <= 2;
        string content = this.RenderInlineChildren(node, setext);
        if (content.Length == 0)
        {
            return;
        }

        if (setext && content.IndexOf('\n') < 0
            && content.IndexOf('\r') < 0)
        {
            this.AppendBlock(content + this.options.GetNewLine()
                + (level == 1 ? "===" : "---"));
            return;
        }

        this.AppendBlock(CreateRepeated('#', level) + " " + content);
    }

    internal void WriteBlockquote(HtmlNode node)
    {
        HtmlMarkdownWriter nested = HtmlMarkdownWriter.Create(this.options,
            this.cancellationToken, this.links);
        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            nested.WriteNode(children[index]);
            index = index + 1;
        }

        string content = nested.Complete();
        if (content.Length == 0)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        index = 0;
        bool lineStart = true;
        while (index < content.Length)
        {
            char current = content[index];
            if (lineStart)
            {
                builder.Append(current == '\r' || current == '\n'
                    ? ">" : "> ");
                lineStart = false;
            }

            builder.Append(current);
            if (current == '\r')
            {
                if (index + 1 < content.Length
                    && content[index + 1] == '\n')
                {
                    index = index + 1;
                    builder.Append('\n');
                }

                lineStart = true;
            }
            else if (current == '\n')
            {
                lineStart = true;
            }

            index = index + 1;
        }

        this.AppendBlock(builder.ToString());
    }

    internal string RenderInlineChildren(HtmlNode node, bool lineStart)
    {
        HtmlMarkdownInlineWriter inline = new HtmlMarkdownInlineWriter(
            this.options, this.cancellationToken, lineStart, this.links);
        inline.VisitChildren(node);
        return inline.Complete();
    }

    internal void FlushFlow()
    {
        string value = this.flow.Complete();
        if (value.Length > 0)
        {
            this.AppendBlock(value);
        }

        this.flow = new HtmlMarkdownInlineWriter(this.options,
            this.cancellationToken, true, this.links);
    }

    internal void AppendBlock(string value)
    {
        if (value.Length == 0)
        {
            return;
        }

        if (this.output.Length > 0)
        {
            this.output.Append(this.options.GetNewLine());
            this.output.Append(this.options.GetNewLine());
        }

        this.output.Append(value);
    }

    internal static string CreateRepeated(char value, int count)
    {
        StringBuilder builder = new StringBuilder(count);
        int index = 0;
        while (index < count)
        {
            builder.Append(value);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static int GetHeadingLevel(string name)
    {
        if (name == "h1")
        {
            return 1;
        }

        if (name == "h2")
        {
            return 2;
        }

        if (name == "h3")
        {
            return 3;
        }

        if (name == "h4")
        {
            return 4;
        }

        if (name == "h5")
        {
            return 5;
        }

        return name == "h6" ? 6 : 0;
    }

    internal static bool IsBlockContainer(string name)
    {
        return name == "address" || name == "article" || name == "aside"
            || name == "blockquote" || name == "body" || name == "center"
            || name == "details" || name == "dialog" || name == "div"
            || name == "dl" || name == "fieldset" || name == "figure"
            || name == "figcaption" || name == "footer" || name == "form"
            || name == "header" || name == "hgroup" || name == "html"
            || name == "li"
            || name == "main" || name == "menu" || name == "nav"
            || name == "ol" || name == "pre" || name == "search"
            || name == "section" || name == "summary" || name == "table"
            || name == "tbody" || name == "td" || name == "tfoot"
            || name == "th" || name == "thead" || name == "tr"
            || name == "ul" || name == "dt" || name == "dd";
    }

}
