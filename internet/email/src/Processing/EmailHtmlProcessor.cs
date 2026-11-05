using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace email;

class EmailHtmlTextConverter
    {
        EmailHtmlTokenizationResult tokenization;
        EmailHtmlTextOptions options;
        int maximumOutputCharacters;
        int maximumUrlCharacters;
        StringBuilder output;
        List<EmailHtmlListState> lists;
        List<EmailHtmlLinkState> links;
        List<string> hiddenElements;
        int preDepth;
        int quoteDepth;
        int tableCellIndex;
        bool pendingSpace;
        bool trimInitialPreNewline;
        int skippedHiddenElementCount;
        int appendedLinkTargetCount;
        int appendedImageAlternativeCount;

        internal EmailHtmlTextConverter(EmailHtmlTokenizationResult tokenization, EmailHtmlTextOptions options, int maximumOutputCharacters)
        {
            this.tokenization = tokenization;
            this.options = options;
            this.maximumOutputCharacters = maximumOutputCharacters;
            maximumUrlCharacters = options.GetLimits().GetMaxUrlCharacters();
            output = new StringBuilder();
            lists = new List<EmailHtmlListState>();
            links = new List<EmailHtmlLinkState>();
            hiddenElements = new List<string>();
            preDepth = 0;
            quoteDepth = 0;
            tableCellIndex = 0;
            pendingSpace = false;
            trimInitialPreNewline = false;
            skippedHiddenElementCount = 0;
            appendedLinkTargetCount = 0;
            appendedImageAlternativeCount = 0;
        }

        internal string Convert()
        {
            int index = 0;
            while (index < tokenization.GetTokenCount())
            {
                Handle(tokenization.GetToken(index));
                index++;
            }

            while (links.Count > 0) CloseLink();
            pendingSpace = false;
            TrimEnd();
            return output.ToString();
        }

        internal int GetSkippedHiddenElementCount() { return skippedHiddenElementCount; }
        internal int GetAppendedLinkTargetCount() { return appendedLinkTargetCount; }
        internal int GetAppendedImageAlternativeCount() { return appendedImageAlternativeCount; }

        void Handle(EmailHtmlToken token)
        {
            if (hiddenElements.Count > 0)
            {
                HandleHidden(token);
                return;
            }

            if (token.GetKind() == EmailHtmlTokenKind.Text)
            {
                HandleText(token.GetData());
                return;
            }

            if (token.GetKind() == EmailHtmlTokenKind.StartTag)
            {
                if (IsHidden(token))
                {
                    skippedHiddenElementCount++;
                    if (!token.IsSelfClosing() && !IsVoidElement(token.GetName())) hiddenElements.Add(token.GetName());
                    return;
                }

                HandleStartTag(token);
                return;
            }

            if (token.GetKind() == EmailHtmlTokenKind.EndTag) HandleEndTag(token);
        }

        void HandleHidden(EmailHtmlToken token)
        {
            if (token.GetKind() == EmailHtmlTokenKind.StartTag && !token.IsSelfClosing() && !IsVoidElement(token.GetName()))
            {
                hiddenElements.Add(token.GetName());
            }
            else if (token.GetKind() == EmailHtmlTokenKind.EndTag)
            {
                int index = hiddenElements.Count - 1;
                while (index >= 0 && !string.Equals(hiddenElements[index], token.GetName(), StringComparison.OrdinalIgnoreCase)) index--;
                if (index >= 0) hiddenElements.RemoveRange(index, hiddenElements.Count - index);
            }
        }

        void HandleText(string value)
        {
            RecordLinkText(value);
            if (preDepth > 0)
            {
                string selected = value;
                if (trimInitialPreNewline)
                {
                    selected = RemoveInitialNewline(selected);
                    trimInitialPreNewline = false;
                }

                AppendPreformatted(selected);
            }
            else
            {
                AppendCollapsed(value);
            }
        }

        void HandleStartTag(EmailHtmlToken token)
        {
            string name = token.GetName();
            if (string.Equals(name, "br", StringComparison.Ordinal))
            {
                Break(1);
            }
            else if (string.Equals(name, "hr", StringComparison.Ordinal))
            {
                Break(1);
                AppendLiteral("---");
                Break(1);
            }
            else if (string.Equals(name, "blockquote", StringComparison.Ordinal))
            {
                Break(2);
                quoteDepth++;
            }
            else if (string.Equals(name, "pre", StringComparison.Ordinal) || string.Equals(name, "listing", StringComparison.Ordinal) || string.Equals(name, "xmp", StringComparison.Ordinal))
            {
                Break(2);
                preDepth++;
                trimInitialPreNewline = true;
            }
            else if (string.Equals(name, "ul", StringComparison.Ordinal) || string.Equals(name, "ol", StringComparison.Ordinal))
            {
                Break(1);
                lists.Add(CreateListState(token));
            }
            else if (string.Equals(name, "li", StringComparison.Ordinal))
            {
                StartListItem(token);
            }
            else if (string.Equals(name, "a", StringComparison.Ordinal))
            {
                StartLink(token);
            }
            else if (string.Equals(name, "img", StringComparison.Ordinal))
            {
                AppendImageAlternative(token);
            }
            else if (string.Equals(name, "q", StringComparison.Ordinal))
            {
                AppendContent("“");
            }
            else if (string.Equals(name, "tr", StringComparison.Ordinal))
            {
                Break(1);
                tableCellIndex = 0;
            }
            else if (string.Equals(name, "td", StringComparison.Ordinal) || string.Equals(name, "th", StringComparison.Ordinal))
            {
                if (tableCellIndex > 0) AppendLiteral("\t");
                tableCellIndex++;
            }
            else if (IsParagraphElement(name))
            {
                Break(2);
            }
            else if (IsLineBlockElement(name))
            {
                Break(1);
            }
        }

        void HandleEndTag(EmailHtmlToken token)
        {
            string name = token.GetName();
            if (string.Equals(name, "blockquote", StringComparison.Ordinal))
            {
                Break(1);
                if (quoteDepth > 0) quoteDepth--;
                Break(2);
            }
            else if (string.Equals(name, "pre", StringComparison.Ordinal) || string.Equals(name, "listing", StringComparison.Ordinal) || string.Equals(name, "xmp", StringComparison.Ordinal))
            {
                if (preDepth > 0) preDepth--;
                trimInitialPreNewline = false;
                Break(2);
            }
            else if (string.Equals(name, "ul", StringComparison.Ordinal) || string.Equals(name, "ol", StringComparison.Ordinal))
            {
                if (lists.Count > 0) lists.RemoveAt(lists.Count - 1);
                Break(1);
            }
            else if (string.Equals(name, "li", StringComparison.Ordinal) || string.Equals(name, "tr", StringComparison.Ordinal))
            {
                Break(1);
            }
            else if (string.Equals(name, "a", StringComparison.Ordinal))
            {
                CloseLink();
            }
            else if (string.Equals(name, "q", StringComparison.Ordinal))
            {
                AppendContent("”");
            }
            else if (IsParagraphElement(name))
            {
                Break(2);
            }
            else if (IsLineBlockElement(name))
            {
                Break(1);
            }
        }

        void StartListItem(EmailHtmlToken token)
        {
            Break(1);
            int depthValue = lists.Count;
            int indent = depthValue > 0 ? (depthValue - 1) * 2 : 0;
            int index = 0;
            while (index < indent)
            {
                AppendLiteral(" ");
                index++;
            }

            if (lists.Count == 0 || !lists[lists.Count - 1].IsOrdered())
            {
                AppendLiteral("- ");
            }
            else
            {
                EmailHtmlListState state = lists[lists.Count - 1];
                int number = GetIntegerAttribute(token, "value", state.GetNextNumber());
                AppendLiteral(number.ToString(CultureInfo.InvariantCulture) + ". ");
                state.SetNextNumber(number == int.MaxValue ? int.MaxValue : number + 1);
            }
        }

        EmailHtmlListState CreateListState(EmailHtmlToken token)
        {
            bool ordered = string.Equals(token.GetName(), "ol", StringComparison.Ordinal);
            int start = ordered ? GetIntegerAttribute(token, "start", 1) : 1;
            return new EmailHtmlListState(ordered, start);
        }

        void StartLink(EmailHtmlToken token)
        {
            EmailHtmlAttribute? href = token.GetAttributeNamed("href");
            string value = href == null ? "" : href.GetValue().Trim();
            if (value.Length > maximumUrlCharacters) throw new EmailLimitException("MaxHtmlUrlCharacters", maximumUrlCharacters, value.Length);
            links.Add(new EmailHtmlLinkState(value));
        }

        void CloseLink()
        {
            if (links.Count == 0) return;
            EmailHtmlLinkState link = links[links.Count - 1];
            links.RemoveAt(links.Count - 1);
            if (!options.GetIncludeLinkTargets()) return;
            string href = link.GetHref();
            if (href.Length == 0 || href[0] == '#') return;
            string visible = CollapseWhitespace(link.GetText()).Trim();
            if (visible.Length == 0)
            {
                AppendCollapsed(href);
                appendedLinkTargetCount++;
            }
            else if (!string.Equals(visible, href, StringComparison.OrdinalIgnoreCase))
            {
                AppendCollapsed(" <" + href + ">");
                appendedLinkTargetCount++;
            }
        }

        void AppendImageAlternative(EmailHtmlToken token)
        {
            if (!options.GetIncludeImageAlternativeText()) return;
            EmailHtmlAttribute? alternative = token.GetAttributeNamed("alt");
            if (alternative == null || alternative.GetValue().Length == 0) return;
            AppendContent(alternative.GetValue());
            appendedImageAlternativeCount++;
        }

        void AppendContent(string value)
        {
            RecordLinkText(value);
            if (preDepth > 0) AppendPreformatted(value);
            else AppendCollapsed(value);
        }

        void RecordLinkText(string value)
        {
            if (links.Count > 0) links[links.Count - 1].AppendText(value);
        }

        void AppendCollapsed(string value)
        {
            int index = 0;
            while (index < value.Length)
            {
                char current = value[index];
                if (char.IsWhiteSpace(current))
                {
                    if (!AtLineStart()) pendingSpace = true;
                }
                else
                {
                    EnsureLinePrefix();
                    if (pendingSpace && !AtLineStart()) AppendCharacter(' ');
                    pendingSpace = false;
                    AppendCharacter(current);
                }

                index++;
            }
        }

        void AppendPreformatted(string value)
        {
            pendingSpace = false;
            int index = 0;
            while (index < value.Length)
            {
                char current = value[index];
                if (current == '\r')
                {
                    if (index + 1 < value.Length && value[index + 1] == '\n') index++;
                    AppendNewline();
                }
                else if (current == '\n')
                {
                    AppendNewline();
                }
                else
                {
                    EnsureLinePrefix();
                    AppendCharacter(current);
                }

                index++;
            }
        }

        void AppendLiteral(string value)
        {
            pendingSpace = false;
            int index = 0;
            while (index < value.Length)
            {
                if (value[index] == '\r' || value[index] == '\n') AppendNewline();
                else
                {
                    EnsureLinePrefix();
                    AppendCharacter(value[index]);
                }

                index++;
            }
        }

        void Break(int count)
        {
            pendingSpace = false;
            if (output.Length == 0) return;
            int existing = 0;
            int index = output.Length - 1;
            while (index >= 0 && output[index] == '\n')
            {
                existing++;
                index--;
            }

            while (existing < count)
            {
                AppendNewline();
                existing++;
            }
        }

        void EnsureLinePrefix()
        {
            if (!AtLineStart() || quoteDepth == 0) return;
            int index = 0;
            while (index < quoteDepth)
            {
                AppendCharacter('>');
                index++;
            }

            AppendCharacter(' ');
        }

        bool AtLineStart()
        {
            return output.Length == 0 || output[output.Length - 1] == '\n';
        }

        void AppendNewline()
        {
            AppendCharacter('\n');
        }

        void AppendCharacter(char value)
        {
            if (output.Length >= maximumOutputCharacters) throw new EmailLimitException("MaxHtmlOutputCharacters", maximumOutputCharacters, (long)output.Length + 1);
            output.Append(value);
        }

        void TrimEnd()
        {
            while (output.Length > 0 && char.IsWhiteSpace(output[output.Length - 1])) output.Remove(output.Length - 1, 1);
        }

        static string RemoveInitialNewline(string value)
        {
            if (value.StartsWith("\r\n", StringComparison.Ordinal)) return value.Substring(2);
            if (value.StartsWith("\r", StringComparison.Ordinal) || value.StartsWith("\n", StringComparison.Ordinal)) return value.Substring(1);
            return value;
        }

        static string CollapseWhitespace(string value)
        {
            StringBuilder result = new StringBuilder();
            bool space = false;
            int index = 0;
            while (index < value.Length)
            {
                if (char.IsWhiteSpace(value[index]))
                {
                    space = result.Length > 0;
                }
                else
                {
                    if (space) result.Append(' ');
                    result.Append(value[index]);
                    space = false;
                }

                index++;
            }

            return result.ToString();
        }

        static int GetIntegerAttribute(EmailHtmlToken token, string name, int fallback)
        {
            EmailHtmlAttribute? attribute = token.GetAttributeNamed(name);
            if (attribute == null) return fallback;
            string value = attribute.GetValue().Trim();
            if (value.Length == 0) return fallback;
            int sign = 1;
            int index = 0;
            if (value[0] == '-')
            {
                sign = -1;
                index++;
            }
            else if (value[0] == '+')
            {
                index++;
            }

            if (index >= value.Length) return fallback;
            int result = 0;
            while (index < value.Length)
            {
                if (value[index] < '0' || value[index] > '9') return fallback;
                int digit = value[index] - '0';
                if (result > (int.MaxValue - digit) / 10) return fallback;
                result = result * 10 + digit;
                index++;
            }

            return sign < 0 ? -result : result;
        }

        static bool IsHidden(EmailHtmlToken token)
        {
            string name = token.GetName();
            if (IsAlwaysHiddenElement(name)) return true;
            if (token.GetAttributeNamed("hidden") != null) return true;
            EmailHtmlAttribute? ariaHidden = token.GetAttributeNamed("aria-hidden");
            if (ariaHidden != null && string.Equals(ariaHidden.GetValue().Trim(), "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(name, "input", StringComparison.Ordinal))
            {
                EmailHtmlAttribute? type = token.GetAttributeNamed("type");
                if (type != null && string.Equals(type.GetValue().Trim(), "hidden", StringComparison.OrdinalIgnoreCase)) return true;
            }

            if (string.Equals(name, "dialog", StringComparison.Ordinal) && token.GetAttributeNamed("open") == null) return true;
            EmailHtmlAttribute? style = token.GetAttributeNamed("style");
            return style != null && HidesWithInlineStyle(style.GetValue());
        }

        static bool HidesWithInlineStyle(string value)
        {
            StringBuilder compact = new StringBuilder();
            int index = 0;
            while (index < value.Length)
            {
                if (!char.IsWhiteSpace(value[index])) compact.Append(char.ToLowerInvariant(value[index]));
                index++;
            }

            string normalized = compact.ToString();
            return normalized.Contains("display:none", StringComparison.Ordinal) || normalized.Contains("visibility:hidden", StringComparison.Ordinal) || normalized.Contains("mso-hide:all", StringComparison.Ordinal);
        }

        static bool IsAlwaysHiddenElement(string name)
        {
            return string.Equals(name, "area", StringComparison.Ordinal)
                || string.Equals(name, "base", StringComparison.Ordinal)
                || string.Equals(name, "datalist", StringComparison.Ordinal)
                || string.Equals(name, "head", StringComparison.Ordinal)
                || string.Equals(name, "link", StringComparison.Ordinal)
                || string.Equals(name, "meta", StringComparison.Ordinal)
                || string.Equals(name, "noembed", StringComparison.Ordinal)
                || string.Equals(name, "noframes", StringComparison.Ordinal)
                || string.Equals(name, "param", StringComparison.Ordinal)
                || string.Equals(name, "rp", StringComparison.Ordinal)
                || string.Equals(name, "script", StringComparison.Ordinal)
                || string.Equals(name, "style", StringComparison.Ordinal)
                || string.Equals(name, "template", StringComparison.Ordinal)
                || string.Equals(name, "title", StringComparison.Ordinal);
        }

        static bool IsParagraphElement(string name)
        {
            return string.Equals(name, "p", StringComparison.Ordinal)
                || string.Equals(name, "h1", StringComparison.Ordinal)
                || string.Equals(name, "h2", StringComparison.Ordinal)
                || string.Equals(name, "h3", StringComparison.Ordinal)
                || string.Equals(name, "h4", StringComparison.Ordinal)
                || string.Equals(name, "h5", StringComparison.Ordinal)
                || string.Equals(name, "h6", StringComparison.Ordinal)
                || string.Equals(name, "address", StringComparison.Ordinal)
                || string.Equals(name, "article", StringComparison.Ordinal)
                || string.Equals(name, "section", StringComparison.Ordinal)
                || string.Equals(name, "header", StringComparison.Ordinal)
                || string.Equals(name, "footer", StringComparison.Ordinal)
                || string.Equals(name, "main", StringComparison.Ordinal)
                || string.Equals(name, "nav", StringComparison.Ordinal)
                || string.Equals(name, "aside", StringComparison.Ordinal)
                || string.Equals(name, "figure", StringComparison.Ordinal)
                || string.Equals(name, "figcaption", StringComparison.Ordinal)
                || string.Equals(name, "form", StringComparison.Ordinal)
                || string.Equals(name, "fieldset", StringComparison.Ordinal);
        }

        static bool IsLineBlockElement(string name)
        {
            return string.Equals(name, "div", StringComparison.Ordinal)
                || string.Equals(name, "center", StringComparison.Ordinal)
                || string.Equals(name, "details", StringComparison.Ordinal)
                || string.Equals(name, "summary", StringComparison.Ordinal)
                || string.Equals(name, "table", StringComparison.Ordinal)
                || string.Equals(name, "caption", StringComparison.Ordinal)
                || string.Equals(name, "dt", StringComparison.Ordinal)
                || string.Equals(name, "dd", StringComparison.Ordinal);
        }

        static bool IsVoidElement(string name)
        {
            return string.Equals(name, "area", StringComparison.Ordinal)
                || string.Equals(name, "base", StringComparison.Ordinal)
                || string.Equals(name, "br", StringComparison.Ordinal)
                || string.Equals(name, "col", StringComparison.Ordinal)
                || string.Equals(name, "embed", StringComparison.Ordinal)
                || string.Equals(name, "hr", StringComparison.Ordinal)
                || string.Equals(name, "img", StringComparison.Ordinal)
                || string.Equals(name, "input", StringComparison.Ordinal)
                || string.Equals(name, "link", StringComparison.Ordinal)
                || string.Equals(name, "meta", StringComparison.Ordinal)
                || string.Equals(name, "param", StringComparison.Ordinal)
                || string.Equals(name, "source", StringComparison.Ordinal)
                || string.Equals(name, "track", StringComparison.Ordinal)
                || string.Equals(name, "wbr", StringComparison.Ordinal);
        }
    }

class EmailHtmlListState
    {
        bool ordered;
        int nextNumber;

        internal EmailHtmlListState(bool ordered, int nextNumber)
        {
            this.ordered = ordered;
            this.nextNumber = nextNumber;
        }

        internal bool IsOrdered() { return ordered; }
        internal int GetNextNumber() { return nextNumber; }
        internal void SetNextNumber(int value) { nextNumber = value; }
    }

class EmailHtmlLinkState
    {
        string href;
        StringBuilder text;

        internal EmailHtmlLinkState(string href)
        {
            this.href = href;
            text = new StringBuilder();
        }

        internal string GetHref() { return href; }
        internal void AppendText(string value) { text.Append(value); }
        internal string GetText() { return text.ToString(); }
}
