using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;

internal class HtmlTextWriter
{
    internal HtmlCompiledTextOptions options;
    internal HtmlCompiledTextRule[] rules;
    internal string[] unorderedListBullets;
    internal HtmlTextOperationContext context;
    internal CancellationToken cancellationToken;
    internal StringBuilder output;
    internal StringBuilder pendingWord;
    internal HtmlWordWrapper wordWrapper;
    internal bool pendingSpace;
    internal int pendingLineBreaks;
    internal bool stopped;
    internal HtmlNode? scope;
    internal int preformattedDepth;
    internal int rubyDepth;
    internal int listDepth;
    internal string linePrefix;
    internal string? pendingListItemMarker;
    internal string pendingListItemContinuation;
    internal bool preformattedLineStart;
    internal List<string> footnoteUrls;
    internal bool footnotesWritten;
    internal List<HtmlNode> processedFormControls;
    internal List<HtmlNode> taskFormControls;
    internal string pendingFormControlLabel;

    internal HtmlTextWriter(HtmlCompiledTextOptions optionsValue,
        HtmlTextOperationContext contextValue,
        CancellationToken cancellationTokenValue)
    {
        if (optionsValue == null)
        {
            throw new ArgumentNullException("optionsValue");
        }

        if (contextValue == null)
        {
            throw new ArgumentNullException("contextValue");
        }

        this.options = optionsValue;
        this.rules = optionsValue.GetRules();
        this.unorderedListBullets = optionsValue.GetUnorderedListBullets();
        this.context = contextValue;
        this.cancellationToken = cancellationTokenValue;
        this.output = new StringBuilder();
        this.pendingWord = new StringBuilder();
        this.wordWrapper = new HtmlWordWrapper(optionsValue.GetWrapColumn());
        this.pendingSpace = false;
        this.pendingLineBreaks = 0;
        this.stopped = false;
        this.scope = null;
        this.preformattedDepth = 0;
        this.rubyDepth = 0;
        this.listDepth = 0;
        this.linePrefix = "";
        this.pendingListItemMarker = null;
        this.pendingListItemContinuation = "";
        this.preformattedLineStart = false;
        this.footnoteUrls = new List<string>();
        this.footnotesWritten = false;
        this.processedFormControls = new List<HtmlNode>();
        this.taskFormControls = new List<HtmlNode>();
        this.pendingFormControlLabel = "";
    }

    internal void WriteNode(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        this.scope = node;
        this.VisitNode(node, 0);
    }

    internal void WriteText(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int index = 0;
        while (index < value.Length && !this.stopped)
        {
            char current = value[index];
            if (current == '\u00A0'
                && this.options.GetNonBreakingSpaceMode()
                    == HtmlTextNonBreakingSpaceMode.ConvertToSpace)
            {
                current = ' ';
            }

            if (Ascii.IsHtmlWhitespace(current))
            {
                this.FlushWord();
                this.pendingSpace = true;
            }
            else
            {
                this.pendingWord.Append(current);
            }

            index = index + 1;
        }
    }

    internal void WriteSourceText(string value)
    {
        if (this.options.GetWhitespaceMode()
            == HtmlTextWhitespaceMode.Preserve)
        {
            this.WritePreformatted(value);
        }
        else
        {
            this.WriteText(value);
        }
    }

    internal void WritePreformatted(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.FlushPending();
        int index = 0;
        while (index < value.Length && !this.stopped)
        {
            char current = value[index];
            if (current == '\r')
            {
                if (index + 1 < value.Length && value[index + 1] == '\n')
                {
                    index = index + 1;
                }

                this.AppendValue(this.options.GetNewLine());
                this.preformattedLineStart = true;
            }
            else if (current == '\n')
            {
                this.AppendValue(this.options.GetNewLine());
                this.preformattedLineStart = true;
            }
            else
            {
                if (this.preformattedLineStart)
                {
                    this.AppendValue(this.linePrefix);
                    this.preformattedLineStart = false;
                }

                this.AppendCharacter(current == '\u00A0'
                        && this.options.GetNonBreakingSpaceMode()
                            == HtmlTextNonBreakingSpaceMode.ConvertToSpace
                    ? ' ' : current);
            }

            index = index + 1;
        }
    }

    internal void WriteLineBreak()
    {
        this.FlushWord();
        if (this.stopped)
        {
            return;
        }

        this.RequestBoundary(this.options.GetLineBoundaryBreaks());
    }

    internal void WriteBlockBoundary()
    {
        this.FlushWord();
        if (this.stopped)
        {
            return;
        }

        this.RequestBoundary(this.options.GetBlockBoundaryBreaks());
    }

    internal bool TryVisitTableNode(int depth)
    {
        if (this.stopped || !this.context.TryVisit(depth))
        {
            this.stopped = true;
            return false;
        }

        return true;
    }

    internal bool TryConsumeTableCells(int cells)
    {
        return this.context.TryConsumeTableCells(cells);
    }

    internal HtmlCompiledTextRule? FindTableRule(HtmlNode node)
    {
        return this.FindRule(node);
    }

    internal string Complete()
    {
        this.FlushWord();
        this.WriteFootnotes();
        if (!this.options.GetTrim())
        {
            this.FlushPending();
            return this.output.ToString();
        }

        this.pendingSpace = false;
        this.pendingLineBreaks = 0;
        int start = 0;
        int end = this.output.Length;
        while (start < end && Ascii.IsHtmlWhitespace(this.output[start]))
        {
            start = start + 1;
        }

        while (end > start && Ascii.IsHtmlWhitespace(this.output[end - 1]))
        {
            end = end - 1;
        }

        return this.output.ToString(start, end - start);
    }

    internal void VisitNode(HtmlNode node, int depth)
    {
        if (this.stopped || !this.context.TryVisit(depth))
        {
            this.stopped = true;
            return;
        }

        HtmlNodeKind kind = node.GetKindStorage();
        if (kind == HtmlNodeKind.Text)
        {
            if (this.preformattedDepth > 0)
            {
                this.WritePreformatted(node.GetData());
            }
            else
            {
                this.WriteSourceText(node.GetData());
            }

            return;
        }

        if (kind != HtmlNodeKind.Document
            && kind != HtmlNodeKind.Fragment
            && kind != HtmlNodeKind.Element)
        {
            return;
        }

        if (kind == HtmlNodeKind.Element)
        {
            if (!this.options.GetIncludeHiddenContent()
                && IsHiddenOrInert(node))
            {
                return;
            }

            HtmlCompiledTextRule? rule = this.FindRule(node);
            if (rule != null)
            {
                HtmlTextRuleAction action = rule.GetAction();
                if (action == HtmlTextRuleAction.Exclude)
                {
                    return;
                }

                if (action == HtmlTextRuleAction.Replace)
                {
                    if (node.GetNamespaceStorage() == HtmlNamespace.Html
                        && node.GetLocalNameStorage() == "li"
                        && this.pendingListItemMarker != null)
                    {
                        this.WriteListItemReplacement(rule.GetReplacement());
                    }
                    else
                    {
                        this.WriteReplacement(node, rule.GetReplacement());
                    }

                    return;
                }
            }

            this.VisitElement(node, depth);
            return;
        }

        this.VisitChildren(node, depth);
    }

    internal void VisitElement(HtmlNode node, int depth)
    {
        string localName = node.GetLocalNameStorage();
        if (localName == "a")
        {
            this.VisitLink(node, depth);
            return;
        }

        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            this.VisitChildren(node, depth);
            return;
        }

        string name = localName;
        if (name == "label")
        {
            this.VisitFormLabel(node, depth);
            return;
        }

        if (IsFormControlName(name))
        {
            this.VisitFormControl(node);
            return;
        }

        if (name == "img")
        {
            this.WriteImage(node);
            return;
        }

        if (name == "audio" || name == "video" || name == "iframe"
            || name == "embed" || name == "object")
        {
            this.WriteMedia(node, name);
            return;
        }

        if (name == "source" || name == "track")
        {
            return;
        }

        if (name == "table")
        {
            HtmlTableTextWriter tableWriter = new HtmlTableTextWriter(this,
                this.options);
            tableWriter.Write(node, depth);
            return;
        }

        if (name == "ol" || name == "ul" || name == "menu")
        {
            this.VisitList(node, depth, name == "ol");
            return;
        }

        if (name == "li" && this.pendingListItemMarker != null)
        {
            this.VisitListItem(node, depth);
            return;
        }

        if (name == "br")
        {
            this.WriteLineBreak();
            return;
        }

        if (name == "hr")
        {
            this.WriteBlockBoundary();
            this.WriteText("----------");
            this.WriteBlockBoundary();
            return;
        }

        if (name == "rp" && this.rubyDepth > 0)
        {
            return;
        }

        bool block = IsBlockElement(name);
        bool lineBlock = IsLineBlockElement(name);
        if (block)
        {
            this.WriteBlockBoundary();
        }
        else if (lineBlock)
        {
            this.WriteLineBoundary();
        }

        if (name == "details" && !node.HasAttribute("open"))
        {
            this.VisitClosedDetails(node, depth);
        }
        else if (name == "ruby")
        {
            this.VisitRuby(node, depth);
        }
        else if ((name == "rt" || name == "rtc")
            && this.rubyDepth > 0
            && (name == "rt" || !HasHtmlChild(node, "rt")))
        {
            this.VisitRubyAnnotation(node, depth);
        }
        else
        {
            this.VisitOrdinaryElement(node, depth, name);
        }

        if (block)
        {
            this.WriteBlockBoundary();
        }
        else if (lineBlock)
        {
            this.WriteLineBoundary();
        }
    }

    internal void VisitOrdinaryElement(HtmlNode node, int depth, string name)
    {
        string bidiStart = GetBidiStart(node, name);
        string bidiEnd = GetBidiEnd(name, bidiStart);
        if (name == "sub")
        {
            this.WriteText("_");
        }
        else if (name == "sup")
        {
            this.WriteText("^");
        }

        if (bidiStart.Length > 0)
        {
            this.WriteText(bidiStart);
        }

        bool preformatted = name == "pre" || name == "listing"
            || name == "plaintext" || name == "xmp";
        if (preformatted)
        {
            this.preformattedDepth = this.preformattedDepth + 1;
            this.preformattedLineStart = false;
        }

        this.VisitChildren(node, depth);

        if (preformatted)
        {
            this.preformattedDepth = this.preformattedDepth - 1;
            this.preformattedLineStart = false;
        }

        if (bidiEnd.Length > 0)
        {
            this.WriteText(bidiEnd);
        }
    }

    internal void VisitClosedDetails(HtmlNode node, int depth)
    {
        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child.GetKindStorage() == HtmlNodeKind.Element
                && child.GetNamespaceStorage() == HtmlNamespace.Html
                && child.GetLocalNameStorage() == "summary")
            {
                this.VisitNode(child, depth + 1);
                return;
            }

            index = index + 1;
        }
    }

    internal void VisitRuby(HtmlNode node, int depth)
    {
        this.rubyDepth = this.rubyDepth + 1;
        this.VisitOrdinaryElement(node, depth, "ruby");
        this.rubyDepth = this.rubyDepth - 1;
    }

    internal void VisitRubyAnnotation(HtmlNode node, int depth)
    {
        this.WriteText(" (");
        this.VisitOrdinaryElement(node, depth,
            node.GetLocalNameStorage());
        this.WriteText(")");
    }

    internal void VisitChildren(HtmlNode node, int depth)
    {
        HtmlNode? container = node.GetTemplateContentStorage();
        if (container == null)
        {
            container = node;
        }

        HtmlNode[] children = container.GetChildrenStorage();
        int index = 0;
        while (index < children.Length && !this.stopped)
        {
            this.VisitNode(children[index], depth + 1);
            index = index + 1;
        }
    }

    internal void VisitLink(HtmlNode node, int depth)
    {
        string url = this.GetSafeAttributeUrl(node, "href");
        HtmlTextLinkStyle style = this.options.GetLinkStyle();
        if (style == HtmlTextLinkStyle.UrlOnly)
        {
            if (url.Length > 0)
            {
                this.WriteText(url);
            }

            return;
        }

        int contentStart = this.output.Length;
        if (node.GetNamespaceStorage() == HtmlNamespace.Html)
        {
            this.VisitOrdinaryElement(node, depth,
                node.GetLocalNameStorage());
        }
        else
        {
            this.VisitChildren(node, depth);
        }

        this.FlushWord();
        if (style == HtmlTextLinkStyle.TextOnly || url.Length == 0)
        {
            return;
        }

        bool hasContent = this.output.Length > contentStart;
        if (style == HtmlTextLinkStyle.TextAndUrl)
        {
            if (hasContent)
            {
                this.WriteText(" (" + url + ")");
            }
            else
            {
                this.WriteText(url);
            }

            return;
        }

        int footnote = this.GetFootnoteNumber(url);
        this.WriteText("[" + footnote.ToString(
            System.Globalization.CultureInfo.InvariantCulture) + "]");
    }

    internal void WriteImage(HtmlNode node)
    {
        HtmlTextImageStyle style = this.options.GetImageStyle();
        if (style == HtmlTextImageStyle.Omit)
        {
            return;
        }

        if (style == HtmlTextImageStyle.Placeholder)
        {
            this.WriteText("[image]");
            return;
        }

        string label = GetImageLabel(node) ?? "";
        if (style != HtmlTextImageStyle.UrlOnly
            && node.HasAttribute("alt") && label.Length == 0)
        {
            return;
        }

        string url = this.GetSafeAttributeUrl(node, "src");
        this.WriteMediaValue(label, url, style);
    }

    internal void WriteMedia(HtmlNode node, string name)
    {
        HtmlTextImageStyle style = this.options.GetImageStyle();
        if (style == HtmlTextImageStyle.Omit)
        {
            return;
        }

        if (style == HtmlTextImageStyle.Placeholder)
        {
            this.WriteText(GetMediaPlaceholder(name));
            return;
        }

        string? label = node.GetAttribute("title");
        if (label == null)
        {
            label = "";
        }

        string attributeName = name == "object" ? "data" : "src";
        string url = this.GetSafeAttributeUrl(node, attributeName);
        if (url.Length == 0 && (name == "audio" || name == "video"))
        {
            url = this.GetFirstSafeSourceUrl(node);
        }

        this.WriteMediaValue(label, url, style);
    }

    internal void WriteMediaValue(string? label, string url,
        HtmlTextImageStyle style)
    {
        label = label == null ? "" : label;
        if (style == HtmlTextImageStyle.UrlOnly)
        {
            if (url.Length > 0)
            {
                this.WriteText(url);
            }

            return;
        }

        if (label.Length > 0)
        {
            this.WriteText(label);
        }

        if (style == HtmlTextImageStyle.AltTextAndUrl && url.Length > 0)
        {
            if (label.Length > 0)
            {
                this.WriteText(" (" + url + ")");
            }
            else
            {
                this.WriteText(url);
            }
        }
    }

    internal void VisitFormLabel(HtmlNode node, int depth)
    {
        if (!this.options.GetIncludeFormControls())
        {
            this.VisitOrdinaryElement(node, depth, "label");
            return;
        }

        HtmlNode? control = this.GetLabelControl(node);
        if (control == null)
        {
            this.VisitOrdinaryElement(node, depth, "label");
            return;
        }

        if (this.processedFormControls.Contains(control))
        {
            if (this.taskFormControls.Contains(control))
            {
                this.VisitOrdinaryElement(node, depth, "label");
            }

            return;
        }

        if (!node.Contains(control))
        {
            return;
        }

        string savedLabel = this.pendingFormControlLabel;
        this.pendingFormControlLabel = GetLabelText(node);
        this.VisitNode(control, depth + 1);
        this.pendingFormControlLabel = savedLabel;
    }

    internal void VisitFormControl(HtmlNode node)
    {
        if (!this.options.GetIncludeFormControls()
            || this.processedFormControls.Contains(node))
        {
            return;
        }

        string label = this.pendingFormControlLabel.Length > 0
            ? this.pendingFormControlLabel : this.GetAssociatedLabel(node);
        this.processedFormControls.Add(node);
        string name = node.GetLocalNameStorage();
        if (name == "input")
        {
            this.WriteInputControl(node, label);
            return;
        }

        string? value;
        if (name == "select")
        {
            value = GetSelectedOptionText(node);
        }
        else if (name == "textarea" || name == "button" || name == "output")
        {
            value = HtmlTextContent.Normalize(node.GetTextContent());
        }
        else
        {
            value = node.GetAttribute("value");
            if (value == null)
            {
                value = "";
            }
        }

        this.WriteLabeledValue(label, value);
    }

    internal void WriteInputControl(HtmlNode node, string label)
    {
        string type = GetInputType(node);
        if (type == "hidden")
        {
            return;
        }

        if (type == "checkbox" || type == "radio")
        {
            bool selected = node.HasAttribute("checked");
            string marker;
            if (type == "checkbox")
            {
                marker = selected ? "[x]" : "[ ]";
            }
            else
            {
                marker = selected ? "(x)" : "( )";
            }

            this.WriteText(label.Length == 0
                ? marker : marker + " " + label);
            return;
        }

        string? value = "";
        if (type == "image")
        {
            value = GetImageLabel(node);
        }
        else if (type != "password" && type != "file")
        {
            string? attributeValue = node.GetAttribute("value");
            if (attributeValue != null)
            {
                value = attributeValue;
            }
        }

        this.WriteLabeledValue(label, value);
    }

    internal void WriteLabeledValue(string label, string? value)
    {
        value = value == null ? "" : value;
        if (label.Length == 0)
        {
            if (value.Length > 0)
            {
                this.WriteText(value);
            }

            return;
        }

        if (value.Length == 0 || label == value)
        {
            this.WriteText(label);
            return;
        }

        string separator = label[label.Length - 1] == ':' ? " " : ": ";
        this.WriteText(label + separator + value);
    }

    internal string GetAssociatedLabel(HtmlNode control)
    {
        string? ariaLabel = control.GetAttribute("aria-label");
        if (ariaLabel != null)
        {
            string normalized = HtmlTextContent.Normalize(ariaLabel);
            if (normalized.Length > 0)
            {
                return normalized;
            }
        }

        HtmlNode? current = control.GetParent();
        while (current != null)
        {
            if (current.GetKindStorage() == HtmlNodeKind.Element
                && current.GetNamespaceStorage() == HtmlNamespace.Html
                && current.GetLocalNameStorage() == "label")
            {
                return GetLabelText(current);
            }

            current = current.GetParent();
        }

        HtmlNode? explicitLabel = this.FindExplicitLabel(control);
        if (explicitLabel != null)
        {
            return GetLabelText(explicitLabel);
        }

        string? title = control.GetAttribute("title");
        return title == null ? "" : HtmlTextContent.Normalize(title);
    }

    internal HtmlNode? GetLabelControl(HtmlNode label)
    {
        string? target = label.GetAttribute("for");
        if (target != null && target.Length > 0 && this.scope != null)
        {
            HtmlNode? control = FindElementById(this.scope, target,
                this.options.GetLimits().GetMaximumVisits());
            return IsFormControl(control) ? control : null;
        }

        return FindDescendantFormControl(label);
    }

    internal HtmlNode? FindExplicitLabel(HtmlNode control)
    {
        string? identifier = control.GetAttribute("id");
        if (identifier == null || identifier.Length == 0 || this.scope == null)
        {
            return null;
        }

        List<HtmlNode> pending = new List<HtmlNode>();
        pending.Add(this.scope);
        int visits = 0;
        int maximumVisits = this.options.GetLimits().GetMaximumVisits();
        while (pending.Count > 0 && visits < maximumVisits)
        {
            int last = pending.Count - 1;
            HtmlNode current = pending[last];
            pending.RemoveAt(last);
            visits = visits + 1;
            if (current.GetKindStorage() == HtmlNodeKind.Element
                && current.GetNamespaceStorage() == HtmlNamespace.Html
                && current.GetLocalNameStorage() == "label"
                && current.GetAttribute("for") == identifier)
            {
                return current;
            }

            HtmlNode[] children = current.GetChildrenStorage();
            int index = children.Length - 1;
            while (index >= 0)
            {
                pending.Add(children[index]);
                index = index - 1;
            }
        }

        return null;
    }

    internal string GetFirstSafeSourceUrl(HtmlNode node)
    {
        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child.GetKindStorage() == HtmlNodeKind.Element
                && child.GetNamespaceStorage() == HtmlNamespace.Html
                && child.GetLocalNameStorage() == "source")
            {
                string url = this.GetSafeAttributeUrl(child, "src");
                if (url.Length > 0)
                {
                    return url;
                }
            }

            index = index + 1;
        }

        return "";
    }

    internal string GetSafeAttributeUrl(HtmlNode node, string attributeName)
    {
        HtmlUrlResult result = node.GetAbsoluteUrlWithLimits(attributeName,
            this.options.GetLimits(), this.cancellationToken);
        if (!result.IsSuccess())
        {
            return "";
        }

        string scheme = result.GetScheme();
        if (scheme != "http" && scheme != "https"
            && scheme != "mailto" && scheme != "tel")
        {
            return "";
        }

        string url = result.GetUrl();
        if (scheme == "http" || scheme == "https")
        {
            Uri parsed = new Uri(url, UriKind.Absolute);
            if (parsed.UserInfo.Length > 0)
            {
                return "";
            }
        }

        return url;
    }

    internal int GetFootnoteNumber(string url)
    {
        int index = this.footnoteUrls.IndexOf(url);
        if (index < 0)
        {
            this.footnoteUrls.Add(url);
            index = this.footnoteUrls.Count - 1;
        }

        return index + 1;
    }

    internal void WriteFootnotes()
    {
        if (this.footnotesWritten || this.footnoteUrls.Count == 0)
        {
            return;
        }

        this.footnotesWritten = true;
        this.WriteBlockBoundary();
        int index = 0;
        while (index < this.footnoteUrls.Count && !this.stopped)
        {
            string number = (index + 1).ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            this.WriteText("[" + number + "] "
                + this.footnoteUrls[index]);
            this.WriteLineBoundary();
            index = index + 1;
        }
    }

    internal void VisitList(HtmlNode node, int depth, bool ordered)
    {
        int sourceLevel = this.listDepth;
        int maximumDepth = this.options.GetMaximumListDepth();
        int formatLevel = sourceLevel < maximumDepth
            ? sourceLevel : maximumDepth - 1;
        bool increaseDepth = sourceLevel < maximumDepth;
        if (sourceLevel == 0)
        {
            this.WriteBlockBoundary();
        }
        else
        {
            this.WriteLineBoundary();
        }

        if (increaseDepth)
        {
            this.listDepth = this.listDepth + 1;
        }

        int[] ordinals = ordered
            ? HtmlNumbers.GetOrderedListOrdinals(node) : new int[0];
        HtmlOrderedListMarkerStyle markerStyle = ordered
            ? HtmlNumbers.GetOrderedListMarkerStyle(node)
            : HtmlOrderedListMarkerStyle.Decimal;
        HtmlNode[] children = node.GetChildrenStorage();
        int itemIndex = 0;
        int index = 0;
        while (index < children.Length && !this.stopped)
        {
            HtmlNode child = children[index];
            bool item = child.GetKindStorage() == HtmlNodeKind.Element
                && child.GetNamespaceStorage() == HtmlNamespace.Html
                && child.GetLocalNameStorage() == "li";
            if (item)
            {
                string marker = ordered
                    ? FormatOrderedMarker(ordinals[itemIndex], markerStyle)
                    : this.GetUnorderedMarker(formatLevel);
                HtmlNode? checkbox = FindTaskCheckbox(child);
                if (checkbox != null)
                {
                    marker = marker + (checkbox.HasAttribute("checked")
                        ? " [x]" : " [ ]");
                    if (this.options.GetIncludeFormControls()
                        && !this.processedFormControls.Contains(checkbox))
                    {
                        this.processedFormControls.Add(checkbox);
                        this.taskFormControls.Add(checkbox);
                    }
                }

                string indentation = CreateIndent(formatLevel * 2);
                string displayedMarker = indentation + marker;
                string continuation = CreateIndent(
                    displayedMarker.Length + 1);
                string? savedMarker = this.pendingListItemMarker;
                string savedContinuation =
                    this.pendingListItemContinuation;
                this.pendingListItemMarker = displayedMarker;
                this.pendingListItemContinuation = continuation;
                this.VisitNode(child, depth + 1);
                this.pendingListItemMarker = savedMarker;
                this.pendingListItemContinuation = savedContinuation;
                itemIndex = itemIndex + 1;
            }
            else
            {
                this.VisitNode(child, depth + 1);
            }

            index = index + 1;
        }

        if (increaseDepth)
        {
            this.listDepth = this.listDepth - 1;
        }

        if (sourceLevel == 0)
        {
            this.WriteBlockBoundary();
        }
        else
        {
            this.WriteLineBoundary();
        }
    }

    internal void VisitListItem(HtmlNode node, int depth)
    {
        string savedPrefix = this.linePrefix;
        this.WriteListMarker(this.pendingListItemMarker,
            this.pendingListItemContinuation);
        this.VisitChildren(node, depth);
        this.FlushWord();
        this.linePrefix = savedPrefix;
        this.WriteLineBoundary();
    }

    internal void WriteListItemReplacement(string replacement)
    {
        string savedPrefix = this.linePrefix;
        this.WriteListMarker(this.pendingListItemMarker,
            this.pendingListItemContinuation);
        this.WriteText(replacement);
        this.FlushWord();
        this.linePrefix = savedPrefix;
        this.WriteLineBoundary();
    }

    internal void WriteListMarker(string? marker, string continuation)
    {
        this.WriteLineBoundary();
        string savedPrefix = this.linePrefix;
        this.linePrefix = "";
        this.FlushPending();
        this.AppendValue(marker);
        this.pendingSpace = true;
        this.linePrefix = continuation;
        if (this.stopped)
        {
            this.linePrefix = savedPrefix;
        }
    }

    internal string GetUnorderedMarker(int level)
    {
        return this.unorderedListBullets[
            level % this.unorderedListBullets.Length];
    }

    internal void WriteReplacement(HtmlNode node, string replacement)
    {
        bool block = node.GetNamespaceStorage() == HtmlNamespace.Html
            && IsBlockElement(node.GetLocalNameStorage());
        bool lineBlock = node.GetNamespaceStorage() == HtmlNamespace.Html
            && IsLineBlockElement(node.GetLocalNameStorage());
        if (block)
        {
            this.WriteBlockBoundary();
        }
        else if (lineBlock)
        {
            this.WriteLineBoundary();
        }

        this.WriteText(replacement);

        if (block)
        {
            this.WriteBlockBoundary();
        }
        else if (lineBlock)
        {
            this.WriteLineBoundary();
        }
    }

    internal void WriteLineBoundary()
    {
        this.FlushWord();
        if (this.stopped)
        {
            return;
        }

        this.RequestBoundary(this.options.GetLineBoundaryBreaks());
    }

    internal void RequestBoundary(int lineBreaks)
    {
        int maximum = this.GetMaximumLineBreaks();
        int requested = lineBreaks < maximum ? lineBreaks : maximum;
        if (this.pendingLineBreaks < requested)
        {
            this.pendingLineBreaks = requested;
        }

        this.pendingSpace = false;
    }

    internal HtmlCompiledTextRule? FindRule(HtmlNode node)
    {
        HtmlNode? scope = this.scope;
        if (scope == null)
        {
            throw new InvalidOperationException(
                "Text rule matching requires an active conversion scope.");
        }

        HtmlCompiledTextRule? matched = null;
        int index = 0;
        while (index < this.rules.Length)
        {
            if (this.rules[index].Matches(node, scope,
                this.cancellationToken))
            {
                matched = this.rules[index];
            }

            index = index + 1;
        }

        return matched;
    }

    internal void FlushPending()
    {
        this.FlushWord();
        this.FlushPendingLayout();
    }

    internal void FlushWord()
    {
        if (this.stopped || this.pendingWord.Length == 0)
        {
            return;
        }

        string word = this.pendingWord.ToString();
        this.pendingWord.Clear();
        if (this.pendingLineBreaks == 0 && this.pendingSpace
            && this.wordWrapper.ShouldWrap(word,
                this.output.Length > 0 || !this.options.GetTrim()))
        {
            this.pendingSpace = false;
            this.AppendValue(this.options.GetNewLine());
            if (!this.stopped && this.linePrefix.Length > 0)
            {
                this.AppendValue(this.linePrefix);
            }
        }
        else
        {
            this.FlushPendingLayout();
        }

        this.AppendWord(word);
    }

    internal void FlushPendingLayout()
    {
        if (this.stopped)
        {
            return;
        }

        if (this.pendingLineBreaks > 0)
        {
            if (this.output.Length > 0 || !this.options.GetTrim())
            {
                int count = this.pendingLineBreaks;
                this.pendingLineBreaks = 0;
                int index = this.GetTrailingLineBreaks(count);
                while (index < count && !this.stopped)
                {
                    this.AppendValue(this.options.GetNewLine());
                    index = index + 1;
                }

                if (!this.stopped && this.linePrefix.Length > 0)
                {
                    this.AppendValue(this.linePrefix);
                }
            }

            this.pendingLineBreaks = 0;
            this.pendingSpace = false;
            return;
        }

        if (this.pendingSpace
            && (this.output.Length > 0 || !this.options.GetTrim()))
        {
            this.AppendCharacter(' ');
        }

        this.pendingSpace = false;
    }

    internal void AppendCharacter(char value)
    {
        if (this.stopped)
        {
            return;
        }

        if (!this.context.TryConsumeOutput(1))
        {
            this.stopped = true;
            return;
        }

        this.output.Append(value);
        this.wordWrapper.ObserveCharacter(value);
    }

    internal void AppendValue(string? value)
    {
        if (this.stopped || value == null || value.Length == 0)
        {
            return;
        }

        if (!this.context.TryConsumeOutput(value.Length))
        {
            this.stopped = true;
            return;
        }

        this.output.Append(value);
        this.wordWrapper.Observe(value);
    }

    internal void AppendWord(string value)
    {
        TextElementEnumerator elements =
            System.Globalization.StringInfo.GetTextElementEnumerator(value);
        while (elements.MoveNext() && !this.stopped)
        {
            this.AppendValue(elements.GetTextElement());
        }
    }

    internal int GetMaximumLineBreaks()
    {
        int maximumBlankLines = this.options.GetMaximumBlankLines();
        return maximumBlankLines == int.MaxValue
            ? int.MaxValue : maximumBlankLines + 1;
    }

    internal int GetTrailingLineBreaks(int maximum)
    {
        string newLine = this.options.GetNewLine();
        int count = 0;
        int end = this.output.Length;
        while (count < maximum && end >= newLine.Length)
        {
            int index = 0;
            bool matches = true;
            while (index < newLine.Length)
            {
                if (this.output[end - newLine.Length + index]
                    != newLine[index])
                {
                    matches = false;
                    break;
                }

                index = index + 1;
            }

            if (!matches)
            {
                break;
            }

            count = count + 1;
            end = end - newLine.Length;
        }

        return count;
    }

    internal static bool IsBlockElement(string name)
    {
        return name == "address" || name == "article" || name == "aside"
            || name == "blockquote" || name == "body" || name == "center"
            || name == "details" || name == "dialog" || name == "div"
            || name == "fieldset" || name == "figure" || name == "footer"
            || name == "form" || name == "h1" || name == "h2"
            || name == "h3" || name == "h4" || name == "h5"
            || name == "h6" || name == "header" || name == "hgroup"
            || name == "main" || name == "nav" || name == "p"
            || name == "search" || name == "section" || name == "dl"
            || name == "pre" || name == "listing" || name == "plaintext"
            || name == "xmp";
    }

    internal static bool IsLineBlockElement(string name)
    {
        return name == "dt" || name == "dd" || name == "figcaption"
            || name == "summary";
    }

    internal static bool HasHtmlChild(HtmlNode node, string localName)
    {
        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child.GetKindStorage() == HtmlNodeKind.Element
                && child.GetNamespaceStorage() == HtmlNamespace.Html
                && child.GetLocalNameStorage() == localName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string GetBidiStart(HtmlNode node, string name)
    {
        string? direction = node.GetAttribute("dir");
        bool leftToRight = EqualsTrimmedIgnoreCase(direction, "ltr");
        bool rightToLeft = EqualsTrimmedIgnoreCase(direction, "rtl");
        bool automatic = EqualsTrimmedIgnoreCase(direction, "auto");
        if (name == "bdo")
        {
            if (leftToRight)
            {
                return "\u202D";
            }

            return rightToLeft ? "\u202E" : "";
        }

        if (leftToRight)
        {
            return "\u2066";
        }

        if (rightToLeft)
        {
            return "\u2067";
        }

        return automatic || name == "bdi" ? "\u2068" : "";
    }

    internal static string GetBidiEnd(string name, string start)
    {
        if (start.Length == 0)
        {
            return "";
        }

        return name == "bdo" ? "\u202C" : "\u2069";
    }

    internal static string? GetImageLabel(HtmlNode node)
    {
        if (node.HasAttribute("alt"))
        {
            return node.GetAttribute("alt");
        }

        string? title = node.GetAttribute("title");
        return title == null ? "" : title;
    }

    internal static string GetMediaPlaceholder(string name)
    {
        if (name == "video")
        {
            return "[video]";
        }

        if (name == "audio")
        {
            return "[audio]";
        }

        if (name == "iframe")
        {
            return "[frame]";
        }

        return "[media]";
    }

    internal static bool IsFormControlName(string name)
    {
        return name == "input" || name == "select" || name == "textarea"
            || name == "button" || name == "meter" || name == "output"
            || name == "progress";
    }

    internal static bool IsFormControl(HtmlNode? node)
    {
        return node != null
            && node.GetKindStorage() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && IsFormControlName(node.GetLocalNameStorage())
            && (node.GetLocalNameStorage() != "input"
                || GetInputType(node) != "hidden");
    }

    internal static HtmlNode? FindDescendantFormControl(HtmlNode node)
    {
        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (IsFormControl(child))
            {
                return child;
            }

            if (child.GetKindStorage() == HtmlNodeKind.Element)
            {
                HtmlNode? descendant = FindDescendantFormControl(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal static HtmlNode? FindElementById(HtmlNode root, string identifier,
        int maximumVisits)
    {
        List<HtmlNode> pending = new List<HtmlNode>();
        pending.Add(root);
        int visits = 0;
        while (pending.Count > 0 && visits < maximumVisits)
        {
            int last = pending.Count - 1;
            HtmlNode current = pending[last];
            pending.RemoveAt(last);
            visits = visits + 1;
            if (current.GetKindStorage() == HtmlNodeKind.Element
                && current.GetAttribute("id") == identifier)
            {
                return current;
            }

            HtmlNode[] children = current.GetChildrenStorage();
            int index = children.Length - 1;
            while (index >= 0)
            {
                pending.Add(children[index]);
                index = index - 1;
            }
        }

        return null;
    }

    internal static string GetLabelText(HtmlNode label)
    {
        StringBuilder builder = new StringBuilder();
        AppendLabelText(label, builder);
        return HtmlTextContent.Normalize(builder.ToString());
    }

    internal static void AppendLabelText(HtmlNode node, StringBuilder builder)
    {
        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            HtmlNodeKind kind = child.GetKindStorage();
            if (kind == HtmlNodeKind.Text)
            {
                builder.Append(child.GetData());
            }
            else if (kind == HtmlNodeKind.Element
                && !IsFormControl(child))
            {
                AppendLabelText(child, builder);
            }

            index = index + 1;
        }
    }

    internal static string GetInputType(HtmlNode node)
    {
        string? type = node.GetAttribute("type");
        if (type == null)
        {
            return "text";
        }

        int start = 0;
        while (start < type.Length && Ascii.IsHtmlWhitespace(type[start]))
        {
            start = start + 1;
        }

        int end = type.Length;
        while (end > start && Ascii.IsHtmlWhitespace(type[end - 1]))
        {
            end = end - 1;
        }

        string normalized = type.Substring(start, end - start)
            .ToLowerInvariant();
        return normalized.Length == 0 ? "text" : normalized;
    }

    internal static string GetSelectedOptionText(HtmlNode select)
    {
        StringBuilder builder = new StringBuilder();
        AppendSelectedOptionText(select, builder);
        return builder.ToString();
    }

    internal static void AppendSelectedOptionText(HtmlNode node,
        StringBuilder builder)
    {
        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child.GetKindStorage() == HtmlNodeKind.Element
                && child.GetNamespaceStorage() == HtmlNamespace.Html)
            {
                if (child.GetLocalNameStorage() == "option")
                {
                    if (child.HasAttribute("selected"))
                    {
                        string? label = child.GetAttribute("label");
                        if (label == null || label.Length == 0)
                        {
                            label = HtmlTextContent.Normalize(
                                child.GetTextContent());
                        }

                        if (label.Length > 0)
                        {
                            if (builder.Length > 0)
                            {
                                builder.Append(", ");
                            }

                            builder.Append(label);
                        }
                    }
                }
                else
                {
                    AppendSelectedOptionText(child, builder);
                }
            }

            index = index + 1;
        }
    }

    internal static string FormatOrderedMarker(int number,
        HtmlOrderedListMarkerStyle style)
    {
        string marker;
        if (style == HtmlOrderedListMarkerStyle.LowerAlpha
            || style == HtmlOrderedListMarkerStyle.UpperAlpha)
        {
            marker = FormatAlphabetic(number);
            if (marker.Length > 0
                && style == HtmlOrderedListMarkerStyle.UpperAlpha)
            {
                marker = marker.ToUpperInvariant();
            }
        }
        else if (style == HtmlOrderedListMarkerStyle.LowerRoman
            || style == HtmlOrderedListMarkerStyle.UpperRoman)
        {
            marker = FormatRoman(number);
            if (marker.Length > 0
                && style == HtmlOrderedListMarkerStyle.LowerRoman)
            {
                marker = marker.ToLowerInvariant();
            }
        }
        else
        {
            marker = "";
        }

        if (marker.Length == 0)
        {
            marker = number.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        return marker + ".";
    }

    internal static string FormatAlphabetic(int number)
    {
        if (number <= 0)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        int value = number;
        while (value > 0)
        {
            value = value - 1;
            builder.Insert(0, (char)('a' + value % 26));
            value = value / 26;
        }

        return builder.ToString();
    }

    internal static string FormatRoman(int number)
    {
        if (number < 1 || number > 3999)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        int value = number;
        value = AppendRoman(builder, value, 1000, "M");
        value = AppendRoman(builder, value, 900, "CM");
        value = AppendRoman(builder, value, 500, "D");
        value = AppendRoman(builder, value, 400, "CD");
        value = AppendRoman(builder, value, 100, "C");
        value = AppendRoman(builder, value, 90, "XC");
        value = AppendRoman(builder, value, 50, "L");
        value = AppendRoman(builder, value, 40, "XL");
        value = AppendRoman(builder, value, 10, "X");
        value = AppendRoman(builder, value, 9, "IX");
        value = AppendRoman(builder, value, 5, "V");
        value = AppendRoman(builder, value, 4, "IV");
        value = AppendRoman(builder, value, 1, "I");
        return builder.ToString();
    }

    internal static int AppendRoman(StringBuilder builder, int value,
        int amount, string digits)
    {
        int remaining = value;
        while (remaining >= amount)
        {
            builder.Append(digits);
            remaining = remaining - amount;
        }

        return remaining;
    }

    internal static string CreateIndent(int count)
    {
        return count == 0 ? "" : new string(' ', count);
    }

    internal static HtmlNode? FindTaskCheckbox(HtmlNode item)
    {
        HtmlNode[] children = item.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            HtmlNodeKind kind = child.GetKindStorage();
            if (kind == HtmlNodeKind.Text)
            {
                if (!ContainsOnlyHtmlWhitespace(child.GetData()))
                {
                    return null;
                }
            }
            else if (kind == HtmlNodeKind.Element)
            {
                if (child.GetNamespaceStorage() == HtmlNamespace.Html)
                {
                    string name = child.GetLocalNameStorage();
                    if (name == "input"
                        && EqualsTrimmedIgnoreCase(
                            child.GetAttribute("type"), "checkbox"))
                    {
                        return child;
                    }

                    if (name == "ol" || name == "ul" || name == "menu")
                    {
                        return null;
                    }

                    HtmlNode? nested = FindTaskCheckbox(child);
                    if (nested != null)
                    {
                        return nested;
                    }
                }

                if (HtmlTextContent.GetDescendantText(child).Length > 0)
                {
                    return null;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool ContainsOnlyHtmlWhitespace(string value)
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

    internal static bool IsHiddenOrInert(HtmlNode node)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        if (node.HasAttribute("hidden") || node.HasAttribute("inert"))
        {
            return true;
        }

        string? ariaHidden = node.GetAttribute("aria-hidden");
        if (EqualsTrimmedIgnoreCase(ariaHidden, "true"))
        {
            return true;
        }

        return node.GetLocalNameStorage() == "input"
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
}
