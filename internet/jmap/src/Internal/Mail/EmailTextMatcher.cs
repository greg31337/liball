using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace jmap;

internal class EmailSearchTerm
{
    internal string Text = "";
    internal bool IsPhrase;
}

internal class EmailTextRange
{
    internal int Start;
    internal int Length;
}

internal class EmailTextMatchResult
{
    internal string SearchText = "";
    internal List<EmailSearchTerm> Terms = new List<EmailSearchTerm>();
    internal List<EmailTextRange> Ranges = new List<EmailTextRange>();
    internal bool IsMatch;
}

internal class HtmlSearchTag
{
    internal string Name = "";
    internal string PresentationText = "";
    internal int NextIndex;
    internal bool IsValid;
    internal bool IsClosing;
    internal bool IsSelfClosing;
}

internal class EmailTextMatcher
{
    internal HeaderValueParser HeaderValues = new HeaderValueParser();

    internal List<EmailSearchTerm> ParseTerms(string? query)
    {
        List<EmailSearchTerm> terms = new List<EmailSearchTerm>();
        if (query == null)
        {
            return terms;
        }

        string value = RepairUnicode(query);
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }

            if (index >= value.Length)
            {
                break;
            }

            char quote = value[index];
            if (quote == '\'' || quote == '"')
            {
                int end = FindPhraseEnd(value, index + 1, quote);
                if (end >= 0)
                {
                    string phrase = DecodePhrase(value, index + 1, end, quote);
                    AddSearchTerm(phrase, true, terms);
                    index = end + 1;
                    continue;
                }
            }

            int tokenEnd = index + 1;
            while (tokenEnd < value.Length && !char.IsWhiteSpace(value[tokenEnd]))
            {
                tokenEnd = tokenEnd + 1;
            }

            AddSearchTerm(value.Substring(index, tokenEnd - index), false, terms);
            index = tokenEnd;
        }

        return terms;
    }

    internal EmailTextMatchResult MatchPlainText(string? value, string? query)
    {
        EmailTextMatchResult result = new EmailTextMatchResult();
        result.SearchText = PrepareText(value);
        result.Terms = ParseTerms(query);
        result.IsMatch = true;
        foreach (EmailSearchTerm term in result.Terms)
        {
            int first = result.SearchText.IndexOf(term.Text, 0, StringComparison.OrdinalIgnoreCase);
            if (first < 0)
            {
                result.IsMatch = false;
                result.Ranges.Clear();
                return result;
            }

            AddTermRanges(result.SearchText, term.Text, result.Ranges);
        }

        SortAndMergeRanges(result.Ranges);
        return result;
    }

    internal EmailTextMatchResult MatchHtmlText(string? html, string? query)
    {
        return MatchPlainText(ExtractHtmlText(html), query);
    }

    internal bool MatchesDecodedHeaderValue(string? rawValue, string? query)
    {
        string? decoded = HeaderValues.DecodeText(rawValue);
        return MatchPlainText(decoded, query).IsMatch;
    }

    internal bool MatchesHeaderList(List<EmailHeader>? headers, string? name, string? query)
    {
        if (headers == null || name == null)
        {
            return false;
        }

        foreach (EmailHeader header in headers)
        {
            if (header != null && header.Name != null &&
                string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                if (query == null || MatchesDecodedHeaderValue(header.Value, query))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal EmailTextMatchResult MatchEmailSearchText(Email? email, string? query)
    {
        return MatchPlainText(BuildEmailSearchText(email), query);
    }

    internal EmailTextMatchResult MatchEmailBodyText(Email? email, string? query)
    {
        return MatchPlainText(BuildEmailBodyText(email), query);
    }

    internal string BuildEmailSearchText(Email? email)
    {
        if (email == null)
        {
            return "";
        }

        StringBuilder result = new StringBuilder();
        if (email.Headers != null)
        {
            foreach (EmailHeader header in email.Headers)
            {
                if (header != null && IsSearchHeaderName(header.Name))
                {
                    AppendTextValue(result, HeaderValues.DecodeText(header.Value));
                }
            }
        }

        AppendTextValue(result, email.Subject);
        AppendAddressValues(result, email.From);
        AppendAddressValues(result, email.To);
        AppendAddressValues(result, email.Cc);
        AppendAddressValues(result, email.Bcc);
        AppendTextValue(result, BuildEmailBodyText(email));
        return result.ToString();
    }

    internal string BuildEmailBodyText(Email? email)
    {
        if (email == null || email.BodyValues == null)
        {
            return "";
        }

        StringBuilder result = new StringBuilder();
        foreach (KeyValuePair<string, EmailBodyValue> item in email.BodyValues)
        {
            if (item.Value == null || item.Value.Value == null)
            {
                continue;
            }

            string? type = FindBodyPartType(email.BodyStructure, item.Key);
            if (type == null || type.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            {
                string text = string.Equals(type, "text/html", StringComparison.OrdinalIgnoreCase) ?
                    ExtractHtmlText(item.Value.Value) : item.Value.Value;
                AppendTextValue(result, text);
            }
        }

        return result.ToString();
    }

    internal string ExtractHtmlText(string? html)
    {
        if (html == null)
        {
            return "";
        }

        string value = RepairUnicode(html);
        StringBuilder result = new StringBuilder();
        string? suppressed = null;
        int index = 0;
        while (index < value.Length)
        {
            int tagStart = value.IndexOf('<', index);
            if (tagStart < 0)
            {
                if (suppressed == null)
                {
                    AppendHtmlText(result, value.Substring(index));
                }

                break;
            }

            if (suppressed == null && tagStart > index)
            {
                AppendHtmlText(result, value.Substring(index, tagStart - index));
            }

            HtmlSearchTag tag = ReadHtmlTag(value, tagStart);
            if (!tag.IsValid)
            {
                if (suppressed == null && !LooksLikeHtmlTag(value, tagStart))
                {
                    result.Append('<');
                    index = tagStart + 1;
                    continue;
                }

                break;
            }

            if (suppressed != null)
            {
                if (tag.IsClosing && string.Equals(tag.Name, suppressed, StringComparison.OrdinalIgnoreCase))
                {
                    suppressed = null;
                    AppendTextBoundary(result);
                }

                index = tag.NextIndex;
                continue;
            }

            if (!tag.IsClosing && IsSuppressedHtmlElement(tag.Name))
            {
                suppressed = tag.Name;
            }
            else if (!tag.IsClosing && tag.PresentationText.Length != 0)
            {
                AppendTextBoundary(result);
                AppendHtmlText(result, tag.PresentationText);
            }

            AppendTextBoundary(result);
            index = tag.NextIndex;
        }

        return PrepareText(result.ToString());
    }

    internal HtmlSearchTag ReadHtmlTag(string value, int start)
    {
        HtmlSearchTag result = new HtmlSearchTag();
        result.NextIndex = start + 1;
        if (start < 0 || start >= value.Length || value[start] != '<')
        {
            return result;
        }

        if (StartsAt(value, "<!--", start))
        {
            int commentEnd = value.IndexOf("-->", start + 4, StringComparison.Ordinal);
            result.IsValid = true;
            result.NextIndex = commentEnd < 0 ? value.Length : commentEnd + 3;
            return result;
        }

        int index = start + 1;
        if (index < value.Length && (value[index] == '!' || value[index] == '?'))
        {
            int declarationEnd = FindTagEnd(value, index + 1);
            if (declarationEnd < 0)
            {
                return result;
            }

            result.IsValid = true;
            result.NextIndex = declarationEnd + 1;
            return result;
        }

        while (index < value.Length && char.IsWhiteSpace(value[index]))
        {
            index = index + 1;
        }

        if (index < value.Length && value[index] == '/')
        {
            result.IsClosing = true;
            index = index + 1;
            while (index < value.Length && char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }
        }

        int nameStart = index;
        if (index >= value.Length || !IsHtmlNameStartCharacter(value[index]))
        {
            return result;
        }

        while (index < value.Length && IsHtmlNameCharacter(value[index]))
        {
            index = index + 1;
        }

        if (index == nameStart)
        {
            return result;
        }

        int end = FindTagEnd(value, index);
        if (end < 0)
        {
            return result;
        }

        result.Name = value.Substring(nameStart, index - nameStart).ToLowerInvariant();
        result.NextIndex = end + 1;
        int final = end - 1;
        while (final >= index && char.IsWhiteSpace(value[final]))
        {
            final = final - 1;
        }

        result.IsSelfClosing = final >= index && value[final] == '/';
        if (!result.IsClosing)
        {
            result.PresentationText = ReadPresentationAttributes(value, index, end);
        }

        result.IsValid = true;
        return result;
    }

    internal int FindPhraseEnd(string value, int start, char quote)
    {
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == '\\' && index + 1 < value.Length && IsPhraseEscapable(value[index + 1]))
            {
                index = index + 2;
            }
            else if (value[index] == quote)
            {
                return index;
            }
            else
            {
                index = index + 1;
            }
        }

        return -1;
    }

    internal string DecodePhrase(string value, int start, int end, char quote)
    {
        StringBuilder result = new StringBuilder();
        int index = start;
        while (index < end)
        {
            if (value[index] == '\\' && index + 1 < end && IsPhraseEscapable(value[index + 1]))
            {
                result.Append(value[index + 1]);
                index = index + 2;
            }
            else
            {
                result.Append(value[index]);
                index = index + 1;
            }
        }

        return result.ToString();
    }

    internal bool IsPhraseEscapable(char character)
    {
        return character == '\'' || character == '"' || character == '\\';
    }

    internal void AddSearchTerm(string value, bool phrase, List<EmailSearchTerm> terms)
    {
        string text = PrepareText(value);
        if (text.Length == 0)
        {
            return;
        }

        EmailSearchTerm term = new EmailSearchTerm();
        term.Text = text;
        term.IsPhrase = phrase;
        terms.Add(term);
    }

    internal void AddTermRanges(string value, string term, List<EmailTextRange> ranges)
    {
        int start = 0;
        while (start <= value.Length - term.Length)
        {
            int found = value.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                return;
            }

            EmailTextRange range = new EmailTextRange();
            range.Start = found;
            range.Length = term.Length;
            ranges.Add(range);
            start = found + term.Length;
        }
    }

    internal void SortAndMergeRanges(List<EmailTextRange> ranges)
    {
        int left = 0;
        while (left < ranges.Count)
        {
            int smallest = left;
            int right = left + 1;
            while (right < ranges.Count)
            {
                if (ranges[right].Start < ranges[smallest].Start ||
                    ranges[right].Start == ranges[smallest].Start && ranges[right].Length > ranges[smallest].Length)
                {
                    smallest = right;
                }

                right = right + 1;
            }

            if (smallest != left)
            {
                EmailTextRange temporary = ranges[left];
                ranges[left] = ranges[smallest];
                ranges[smallest] = temporary;
            }

            left = left + 1;
        }

        int index = 1;
        while (index < ranges.Count)
        {
            EmailTextRange previous = ranges[index - 1];
            EmailTextRange current = ranges[index];
            int previousEnd = previous.Start + previous.Length;
            int currentEnd = current.Start + current.Length;
            if (current.Start <= previousEnd)
            {
                if (currentEnd > previousEnd)
                {
                    previous.Length = currentEnd - previous.Start;
                }

                ranges.RemoveAt(index);
            }
            else
            {
                index = index + 1;
            }
        }
    }

    internal string PrepareText(string? value)
    {
        if (value == null)
        {
            return "";
        }

        string normalized = RepairUnicode(value).Normalize(NormalizationForm.FormC);
        StringBuilder result = new StringBuilder();
        bool spacing = true;
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (char.IsWhiteSpace(character))
            {
                if (!spacing)
                {
                    result.Append(' ');
                    spacing = true;
                }
            }
            else if (!char.IsControl(character))
            {
                result.Append(character);
                spacing = false;
            }

            index = index + 1;
        }

        if (result.Length != 0 && result[result.Length - 1] == ' ')
        {
            result.Length = result.Length - 1;
        }

        return result.ToString();
    }

    internal string RepairUnicode(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                {
                    result.Append(character);
                    result.Append(value[index + 1]);
                    index = index + 2;
                }
                else
                {
                    result.Append('\ufffd');
                    index = index + 1;
                }
            }
            else if (char.IsLowSurrogate(character))
            {
                result.Append('\ufffd');
                index = index + 1;
            }
            else
            {
                result.Append(character);
                index = index + 1;
            }
        }

        return result.ToString();
    }

    internal int FindTagEnd(string value, int start)
    {
        char quote = '\0';
        int index = start;
        while (index < value.Length)
        {
            char character = value[index];
            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
            }
            else if (character == '\'' || character == '"')
            {
                quote = character;
            }
            else if (character == '>')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal string ReadPresentationAttributes(string value, int start, int end)
    {
        StringBuilder result = new StringBuilder();
        int index = start;
        while (index < end)
        {
            while (index < end && (char.IsWhiteSpace(value[index]) || value[index] == '/'))
            {
                index = index + 1;
            }

            int nameStart = index;
            while (index < end && IsHtmlAttributeNameCharacter(value[index]))
            {
                index = index + 1;
            }

            if (index == nameStart)
            {
                index = index + 1;
                continue;
            }

            string name = value.Substring(nameStart, index - nameStart);
            while (index < end && char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }

            string attributeValue = "";
            if (index < end && value[index] == '=')
            {
                index = index + 1;
                while (index < end && char.IsWhiteSpace(value[index]))
                {
                    index = index + 1;
                }

                if (index < end && (value[index] == '\'' || value[index] == '"'))
                {
                    char quote = value[index];
                    index = index + 1;
                    int valueStart = index;
                    while (index < end && value[index] != quote)
                    {
                        index = index + 1;
                    }

                    attributeValue = value.Substring(valueStart, index - valueStart);
                    if (index < end)
                    {
                        index = index + 1;
                    }
                }
                else
                {
                    int valueStart = index;
                    while (index < end && !char.IsWhiteSpace(value[index]) && value[index] != '/')
                    {
                        index = index + 1;
                    }

                    attributeValue = value.Substring(valueStart, index - valueStart);
                }
            }

            if ((string.Equals(name, "alt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "title", StringComparison.OrdinalIgnoreCase)) && attributeValue.Length != 0)
            {
                AppendTextValue(result, WebUtility.HtmlDecode(attributeValue));
            }
        }

        return result.ToString();
    }

    internal void AppendHtmlText(StringBuilder destination, string value)
    {
        string decoded = WebUtility.HtmlDecode(value);
        if (decoded.Length != 0)
        {
            destination.Append(decoded);
        }
    }

    internal void AppendTextBoundary(StringBuilder destination)
    {
        if (destination.Length != 0 && !char.IsWhiteSpace(destination[destination.Length - 1]))
        {
            destination.Append(' ');
        }
    }

    internal void AppendTextValue(StringBuilder destination, string? value)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }

        AppendTextBoundary(destination);
        destination.Append(value);
    }

    internal void AppendAddressValues(StringBuilder destination, List<EmailAddress>? addresses)
    {
        if (addresses == null)
        {
            return;
        }

        foreach (EmailAddress address in addresses)
        {
            if (address != null)
            {
                AppendTextValue(destination, address.Name);
                AppendTextValue(destination, address.Email);
            }
        }
    }

    internal string? FindBodyPartType(EmailBodyPart? part, string partId)
    {
        if (part == null)
        {
            return null;
        }

        if (part.PartId == partId)
        {
            return part.Type;
        }

        if (part.SubParts != null)
        {
            foreach (EmailBodyPart child in part.SubParts)
            {
                string? found = FindBodyPartType(child, partId);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    internal bool IsSearchHeaderName(string? name)
    {
        if (name == null)
        {
            return false;
        }

        return string.Equals(name, "From", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "To", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "Cc", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "Bcc", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "Subject", StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsSuppressedHtmlElement(string name)
    {
        return name == "head" || name == "script" || name == "style" || name == "template";
    }

    internal bool IsHtmlNameCharacter(char character)
    {
        return character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' ||
            character >= '0' && character <= '9' || character == ':' || character == '-';
    }

    internal bool IsHtmlNameStartCharacter(char character)
    {
        return character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z';
    }

    internal bool IsHtmlAttributeNameCharacter(char character)
    {
        return !char.IsWhiteSpace(character) && character != '=' && character != '/' && character != '>' &&
            character != '\'' && character != '"';
    }

    internal bool LooksLikeHtmlTag(string value, int start)
    {
        int index = start + 1;
        while (index < value.Length && char.IsWhiteSpace(value[index]))
        {
            index = index + 1;
        }

        if (index < value.Length && value[index] == '/')
        {
            index = index + 1;
        }

        return index < value.Length && (IsHtmlNameStartCharacter(value[index]) || value[index] == '!' ||
            value[index] == '?');
    }

    internal bool StartsAt(string value, string expected, int start)
    {
        if (start < 0 || start + expected.Length > value.Length)
        {
            return false;
        }

        return string.Equals(value.Substring(start, expected.Length), expected, StringComparison.Ordinal);
    }
}
