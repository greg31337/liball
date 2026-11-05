using System;
using System.Collections.Generic;
using System.Text;

namespace email;

class EmailHtmlTokenizerEngine
    {
        string html;
        EmailHtmlLimits limits;
        List<EmailHtmlToken> tokens;
        int index;
        int outputCharacters;
        int malformedMarkupCount;
        int malformedEntityCount;
        int depth;
        int maximumObservedDepth;
        string? rawElementName;

        internal EmailHtmlTokenizerEngine(string html, EmailHtmlLimits limits)
        {
            this.html = html;
            this.limits = limits;
            tokens = new List<EmailHtmlToken>();
            index = 0;
            outputCharacters = 0;
            malformedMarkupCount = 0;
            malformedEntityCount = 0;
            depth = 0;
            maximumObservedDepth = 0;
            rawElementName = null;
        }

        internal EmailHtmlTokenizationResult Run()
        {
            while (index < html.Length)
            {
                if (rawElementName != null)
                {
                    ParseRawText();
                }
                else if (CanStartMarkup(html, index))
                {
                    ParseMarkup();
                }
                else
                {
                    ParseText();
                }
            }

            EmailIssueList issues = new EmailIssueList();
            if (malformedMarkupCount > 0)
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.HtmlMarkupMalformed, "HTML tokenization recovered from " + malformedMarkupCount + " malformed markup construct(s).", null, null, null, "Malformed constructs were treated conservatively as text or ignorable markup; no browser-equivalent DOM is implied."));
            }

            if (malformedEntityCount > 0)
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.HtmlEntityMalformed, "HTML tokenization found " + malformedEntityCount + " malformed or invalid character reference(s).", null, null, null, "Invalid references were preserved when possible; invalid scalar values and null characters were replaced with U+FFFD."));
            }

            return new EmailHtmlTokenizationResult(tokens, issues, html.Length, malformedMarkupCount, malformedEntityCount, maximumObservedDepth);
        }

        void ParseMarkup()
        {
            if (StartsWith(html, index, "<!--"))
            {
                ParseComment();
                return;
            }

            if (StartsWithIgnoreCase(html, index, "<!doctype") && HasMarkupBoundary(index + 9))
            {
                ParseDoctype();
                return;
            }

            if (StartsWith(html, index, "<!") || StartsWith(html, index, "<?"))
            {
                ParseBogusComment();
                return;
            }

            if (StartsWith(html, index, "</"))
            {
                ParseEndTag();
                return;
            }

            ParseStartTag();
        }

        void ParseText()
        {
            int start = index;
            while (index < html.Length && !CanStartMarkup(html, index)) index++;
            AddText(start, index, false, true);
        }

        void ParseRawText()
        {
            int closing = FindRawEnd(html, index, rawElementName);
            if (closing < 0)
            {
                bool decode = IsRcDataElement(rawElementName);
                AddText(index, html.Length, true, decode);
                index = html.Length;
                malformedMarkupCount++;
                rawElementName = null;
                return;
            }

            if (closing > index)
            {
                bool decode = IsRcDataElement(rawElementName);
                AddText(index, closing, true, decode);
                index = closing;
                return;
            }

            ParseEndTag();
        }

        void ParseComment()
        {
            int start = index;
            int close = html.IndexOf("-->", index + 4, StringComparison.Ordinal);
            bool malformed = close < 0;
            int dataEnd = malformed ? html.Length : close;
            string data = html.Substring(index + 4, dataEnd - index - 4);
            int end = malformed ? html.Length : close + 3;
            if (malformed) malformedMarkupCount++;
            AddOutput(data.Length);
            AddToken(new EmailHtmlToken(EmailHtmlTokenKind.Comment, "", data, new List<EmailHtmlAttribute>(), false, false, malformed, start, end - start));
            index = end;
        }

        void ParseBogusComment()
        {
            int start = index;
            int close = html.IndexOf('>', index + 2);
            bool malformed = true;
            int dataEnd = close < 0 ? html.Length : close;
            string data = html.Substring(index + 2, dataEnd - index - 2);
            int end = close < 0 ? html.Length : close + 1;
            malformedMarkupCount++;
            AddOutput(data.Length);
            AddToken(new EmailHtmlToken(EmailHtmlTokenKind.Comment, "", data, new List<EmailHtmlAttribute>(), false, false, malformed, start, end - start));
            index = end;
        }

        void ParseDoctype()
        {
            int start = index;
            int close = FindTagEnd(html, index + 9);
            bool malformed = close < 0;
            int dataEnd = malformed ? html.Length : close;
            string data = html.Substring(index + 9, dataEnd - index - 9).Trim();
            int end = malformed ? html.Length : close + 1;
            if (malformed) malformedMarkupCount++;
            AddOutput(data.Length);
            AddToken(new EmailHtmlToken(EmailHtmlTokenKind.Doctype, "", data, new List<EmailHtmlAttribute>(), false, false, malformed, start, end - start));
            index = end;
        }

        void ParseStartTag()
        {
            int start = index;
            int close = FindTagEnd(html, index + 1);
            if (close < 0)
            {
                malformedMarkupCount++;
                AddText(index, html.Length, false, true);
                index = html.Length;
                return;
            }

            int cursor = index + 1;
            int nameStart = cursor;
            while (cursor < close && IsNameCharacter(html[cursor])) cursor++;
            string name = html.Substring(nameStart, cursor - nameStart).ToLowerInvariant();
            List<EmailHtmlAttribute> attributes = new List<EmailHtmlAttribute>();
            bool malformed = false;
            bool selfClosing = false;
            int physicalAttributeCount = 0;
            while (cursor < close)
            {
                bool hadWhitespace = false;
                while (cursor < close && char.IsWhiteSpace(html[cursor]))
                {
                    hadWhitespace = true;
                    cursor++;
                }
                if (cursor >= close) break;
                if (html[cursor] == '/')
                {
                    cursor++;
                    while (cursor < close && char.IsWhiteSpace(html[cursor])) cursor++;
                    if (cursor == close) selfClosing = true;
                    else malformed = true;
                    continue;
                }

                if (!hadWhitespace) malformed = true;

                int attributeStart = cursor;
                while (cursor < close && IsNameCharacter(html[cursor])) cursor++;
                if (cursor == attributeStart)
                {
                    malformed = true;
                    cursor++;
                    continue;
                }

                physicalAttributeCount++;
                if (physicalAttributeCount > limits.GetMaxAttributesPerTag()) throw new EmailLimitException("MaxHtmlAttributesPerTag", limits.GetMaxAttributesPerTag(), physicalAttributeCount);
                string attributeName = html.Substring(attributeStart, cursor - attributeStart).ToLowerInvariant();
                while (cursor < close && char.IsWhiteSpace(html[cursor])) cursor++;
                bool hasValue = cursor < close && html[cursor] == '=';
                string rawValue = "";
                if (hasValue)
                {
                    cursor++;
                    while (cursor < close && char.IsWhiteSpace(html[cursor])) cursor++;
                    if (cursor >= close)
                    {
                        malformed = true;
                    }
                    else if (html[cursor] == '"' || html[cursor] == '\'')
                    {
                        char quote = html[cursor];
                        cursor++;
                        int valueStart = cursor;
                        while (cursor < close && html[cursor] != quote) cursor++;
                        rawValue = html.Substring(valueStart, cursor - valueStart);
                        if (cursor < close) cursor++;
                        else malformed = true;
                    }
                    else
                    {
                        int valueStart = cursor;
                        while (cursor < close && !char.IsWhiteSpace(html[cursor])) cursor++;
                        rawValue = html.Substring(valueStart, cursor - valueStart);
                    }
                }

                EmailHtmlEntityDecodeResult decoded = Decode(rawValue);
                int attributeEnd = cursor;
                if (HasAttribute(attributes, attributeName))
                {
                    malformed = true;
                }
                else
                {
                    attributes.Add(new EmailHtmlAttribute(attributeName, rawValue, decoded.GetText(), hasValue, attributeStart, Math.Max(1, attributeEnd - attributeStart)));
                }
            }

            if (malformed) malformedMarkupCount++;
            AddToken(new EmailHtmlToken(EmailHtmlTokenKind.StartTag, name, "", attributes, selfClosing, false, malformed, start, close - start + 1));
            index = close + 1;
            if (!selfClosing && !IsVoidElement(name))
            {
                depth++;
                if (depth > maximumObservedDepth) maximumObservedDepth = depth;
                if (depth > limits.GetMaxNestingDepth()) throw new EmailLimitException("MaxHtmlNestingDepth", limits.GetMaxNestingDepth(), depth);
                if (IsRawElement(name)) rawElementName = name;
            }
        }

        void ParseEndTag()
        {
            int start = index;
            int close = FindTagEnd(html, index + 2);
            if (close < 0)
            {
                malformedMarkupCount++;
                AddText(index, html.Length, false, true);
                index = html.Length;
                rawElementName = null;
                return;
            }

            int cursor = index + 2;
            int nameStart = cursor;
            while (cursor < close && IsNameCharacter(html[cursor])) cursor++;
            string name = html.Substring(nameStart, cursor - nameStart).ToLowerInvariant();
            while (cursor < close && char.IsWhiteSpace(html[cursor])) cursor++;
            bool malformed = cursor < close;
            if (malformed) malformedMarkupCount++;
            AddToken(new EmailHtmlToken(EmailHtmlTokenKind.EndTag, name, "", new List<EmailHtmlAttribute>(), false, false, malformed, start, close - start + 1));
            index = close + 1;
            if (depth > 0 && !IsVoidElement(name)) depth--;
            if (rawElementName != null && string.Equals(rawElementName, name, StringComparison.OrdinalIgnoreCase)) rawElementName = null;
        }

        void AddText(int start, int end, bool rawText, bool decodeEntities)
        {
            if (end <= start) return;
            string raw = html.Substring(start, end - start);
            string data;
            if (decodeEntities)
            {
                data = Decode(raw).GetText();
            }
            else
            {
                data = ReplaceNulls(raw);
            }

            AddToken(new EmailHtmlToken(EmailHtmlTokenKind.Text, "", data, new List<EmailHtmlAttribute>(), false, rawText, false, start, end - start));
        }

        EmailHtmlEntityDecodeResult Decode(string value)
        {
            int remaining = limits.GetMaxOutputCharacters() - outputCharacters;
            if (remaining < 1 && value.Length > 0) throw new EmailLimitException("MaxHtmlOutputCharacters", limits.GetMaxOutputCharacters(), (long)outputCharacters + value.Length);
            EmailHtmlEntityDecodeResult decoded = new EmailHtmlEntityDecoder().DecodeResultWithValueAndMaximumOutputCharacters(value, remaining < 1 ? 1 : remaining);
            malformedEntityCount += decoded.GetMalformedReferenceCount();
            AddOutput(decoded.GetText().Length);
            return decoded;
        }

        string ReplaceNulls(string value)
        {
            StringBuilder result = new StringBuilder();
            int cursor = 0;
            while (cursor < value.Length)
            {
                if (value[cursor] == '\0')
                {
                    result.Append('\uFFFD');
                    malformedEntityCount++;
                }
                else
                {
                    result.Append(value[cursor]);
                }

                cursor++;
            }

            AddOutput(result.Length);
            return result.ToString();
        }

        void AddOutput(int count)
        {
            long observed = (long)outputCharacters + count;
            if (observed > limits.GetMaxOutputCharacters()) throw new EmailLimitException("MaxHtmlOutputCharacters", limits.GetMaxOutputCharacters(), observed);
            outputCharacters += count;
        }

        void AddToken(EmailHtmlToken token)
        {
            if (tokens.Count >= limits.GetMaxTokenCount()) throw new EmailLimitException("MaxHtmlTokenCount", limits.GetMaxTokenCount(), (long)tokens.Count + 1);
            tokens.Add(token);
        }

        static bool HasAttribute(List<EmailHtmlAttribute> attributes, string name)
        {
            int cursor = 0;
            while (cursor < attributes.Count)
            {
                if (string.Equals(attributes[cursor].GetName(), name, StringComparison.OrdinalIgnoreCase)) return true;
                cursor++;
            }

            return false;
        }

        static int FindTagEnd(string value, int start)
        {
            char quote = '\0';
            int cursor = start;
            while (cursor < value.Length)
            {
                char current = value[cursor];
                if (quote == '\0' && (current == '"' || current == '\'')) quote = current;
                else if (quote != '\0' && current == quote) quote = '\0';
                else if (quote == '\0' && current == '>') return cursor;
                cursor++;
            }

            return -1;
        }

        static int FindRawEnd(string value, int start, string? name)
        {
            int cursor = start;
            while (cursor < value.Length)
            {
                int candidate = value.IndexOf("</", cursor, StringComparison.Ordinal);
                if (candidate < 0) return -1;
                int nameStart = candidate + 2;
                if (nameStart + (name ?? throw new InvalidOperationException()).Length <= value.Length && string.Equals(value.Substring(nameStart, name.Length), name, StringComparison.OrdinalIgnoreCase))
                {
                    int boundary = nameStart + name.Length;
                    if (boundary >= value.Length || char.IsWhiteSpace(value[boundary]) || value[boundary] == '/' || value[boundary] == '>') return candidate;
                }

                cursor = candidate + 2;
            }

            return -1;
        }

        static bool CanStartMarkup(string value, int offset)
        {
            if (offset >= value.Length || value[offset] != '<' || offset + 1 >= value.Length) return false;
            char next = value[offset + 1];
            if (IsAsciiLetter(next) || next == '!' || next == '?') return true;
            return next == '/' && offset + 2 < value.Length && IsAsciiLetter(value[offset + 2]);
        }

        static bool IsNameCharacter(char value)
        {
            return IsAsciiLetter(value) || value >= '0' && value <= '9' || value == '-' || value == '_' || value == ':' || value == '.';
        }

        static bool IsAsciiLetter(char value)
        {
            return value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z';
        }

        static bool StartsWith(string value, int offset, string expected)
        {
            if (offset + expected.Length > value.Length) return false;
            return string.Equals(value.Substring(offset, expected.Length), expected, StringComparison.Ordinal);
        }

        static bool StartsWithIgnoreCase(string value, int offset, string expected)
        {
            if (offset + expected.Length > value.Length) return false;
            return string.Equals(value.Substring(offset, expected.Length), expected, StringComparison.OrdinalIgnoreCase);
        }

        bool HasMarkupBoundary(int offset)
        {
            return offset >= html.Length || char.IsWhiteSpace(html[offset]) || html[offset] == '>';
        }

        static bool IsRawElement(string name)
        {
            return string.Equals(name, "script", StringComparison.Ordinal) || string.Equals(name, "style", StringComparison.Ordinal) || IsRcDataElement(name);
        }

        static bool IsRcDataElement(string? name)
        {
            return string.Equals(name, "title", StringComparison.Ordinal) || string.Equals(name, "textarea", StringComparison.Ordinal);
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
