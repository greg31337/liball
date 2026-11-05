using System;
using System.Collections.Generic;
using System.Text;

namespace email;

internal class EmailHtmlReferenceScanner
{
    int maximumReferences;
    int maximumUrlCharacters;
    List<string> references;

    internal EmailHtmlReferenceScanner(int maximumReferences, int maximumUrlCharacters)
    {
        if (maximumReferences < 1)
        {
            throw new ArgumentOutOfRangeException("maximumReferences");
        }

        if (maximumUrlCharacters < 1)
        {
            throw new ArgumentOutOfRangeException("maximumUrlCharacters");
        }

        this.maximumReferences = maximumReferences;
        this.maximumUrlCharacters = maximumUrlCharacters;
        references = new List<string>();
    }

    internal List<string> Find(string html)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        int index = 0;
        bool cssContent = false;
        while (index < html.Length)
        {
            if (html[index] == '<')
            {
                if (StartsWith(html, index, "<!--"))
                {
                    index = SkipComment(html, index + 4);
                    continue;
                }

                int end = FindTagEnd(html, index + 1);
                string tagName = GetTagName(html, index + 1, end);
                bool closingTag = IsClosingTag(html, index + 1, end);
                ScanAttributes(html, index + 1, end);
                if (string.Equals(tagName, "style", StringComparison.OrdinalIgnoreCase))
                {
                    cssContent = !closingTag;
                }

                index = end < html.Length ? end + 1 : html.Length;
                continue;
            }

            if (cssContent && StartsWithIgnoreCase(html, index, "url("))
            {
                index = ScanCssUrl(html, index + 4);
                continue;
            }

            index++;
        }

        return new List<string>(references);
    }

    void ScanAttributes(string html, int start, int end)
    {
        int index = start;
        while (index < end && (char.IsWhiteSpace(html[index]) || html[index] == '/'))
        {
            index++;
        }

        while (index < end && IsNameCharacter(html[index]))
        {
            index++;
        }

        while (index < end)
        {
            while (index < end && (char.IsWhiteSpace(html[index]) || html[index] == '/'))
            {
                index++;
            }

            int nameStart = index;
            while (index < end && IsNameCharacter(html[index]))
            {
                index++;
            }

            if (index == nameStart)
            {
                index++;
                continue;
            }

            string name = html.Substring(nameStart, index - nameStart);
            while (index < end && char.IsWhiteSpace(html[index]))
            {
                index++;
            }

            if (index >= end || html[index] != '=')
            {
                continue;
            }

            index++;
            while (index < end && char.IsWhiteSpace(html[index]))
            {
                index++;
            }

            char quote = index < end && (html[index] == '"' || html[index] == '\'') ? html[index] : '\0';
            if (quote != '\0')
            {
                index++;
            }

            int valueStart = index;
            if (quote != '\0')
            {
                while (index < end && html[index] != quote)
                {
                    index++;
                }
            }
            else
            {
                while (index < end && !char.IsWhiteSpace(html[index]) && html[index] != '>')
                {
                    index++;
                }
            }

            string value = DecodeEntities(html.Substring(valueStart, index - valueStart));
            if (IsUrlAttribute(name))
            {
                Add(value);
            }
            else if (string.Equals(name, "srcset", StringComparison.OrdinalIgnoreCase))
            {
                AddSrcSet(value);
            }
            else if (string.Equals(name, "style", StringComparison.OrdinalIgnoreCase))
            {
                AddCssReferences(value);
            }

            if (quote != '\0' && index < end)
            {
                index++;
            }
        }
    }

    void AddCssReferences(string css)
    {
        int index = 0;
        while (index < css.Length)
        {
            if (StartsWithIgnoreCase(css, index, "url("))
            {
                index = ScanCssUrl(css, index + 4);
            }
            else
            {
                index++;
            }
        }
    }

    int ScanCssUrl(string html, int start)
    {
        int index = start;
        while (index < html.Length && char.IsWhiteSpace(html[index]))
        {
            index++;
        }

        char quote = index < html.Length && (html[index] == '"' || html[index] == '\'') ? html[index] : '\0';
        if (quote != '\0')
        {
            index++;
        }

        int valueStart = index;
        if (quote != '\0')
        {
            while (index < html.Length && html[index] != quote)
            {
                index++;
            }
        }
        else
        {
            while (index < html.Length && html[index] != ')')
            {
                index++;
            }
        }

        Add(DecodeEntities(html.Substring(valueStart, index - valueStart)).Trim());
        while (index < html.Length && html[index] != ')')
        {
            index++;
        }

        return index < html.Length ? index + 1 : html.Length;
    }

    void AddSrcSet(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && (char.IsWhiteSpace(value[index]) || value[index] == ','))
            {
                index++;
            }

            int start = index;
            bool data = StartsWithIgnoreCase(value, index, "data:");
            while (index < value.Length && !char.IsWhiteSpace(value[index]) && (data || value[index] != ','))
            {
                index++;
            }

            Add(value.Substring(start, index - start));
            while (index < value.Length && value[index] != ',')
            {
                index++;
            }
        }
    }

    void Add(string value)
    {
        string selected = value.Trim();
        if (selected.Length == 0 || selected[0] == '#')
        {
            return;
        }

        if (selected.Length > maximumUrlCharacters)
        {
            throw new EmailLimitException("MaxRelatedUrlCharacters", maximumUrlCharacters, selected.Length);
        }

        if (references.Count >= maximumReferences)
        {
            throw new EmailLimitException("MaxRelatedReferenceCount", maximumReferences, (long)references.Count + 1);
        }

        references.Add(selected);
    }

    static int FindTagEnd(string html, int start)
    {
        char quote = '\0';
        int index = start;
        while (index < html.Length)
        {
            char current = html[index];
            if (quote == '\0' && (current == '"' || current == '\''))
            {
                quote = current;
            }
            else if (quote != '\0' && current == quote)
            {
                quote = '\0';
            }
            else if (quote == '\0' && current == '>')
            {
                return index;
            }

            index++;
        }

        return html.Length;
    }

    static int SkipComment(string html, int start)
    {
        int index = start;
        while (index + 2 < html.Length)
        {
            if (html[index] == '-' && html[index + 1] == '-' && html[index + 2] == '>')
            {
                return index + 3;
            }

            index++;
        }

        return html.Length;
    }

    static bool IsUrlAttribute(string name)
    {
        return string.Equals(name, "src", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "href", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "poster", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "background", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "xlink:href", StringComparison.OrdinalIgnoreCase);
    }

    static string GetTagName(string html, int start, int end)
    {
        int index = start;
        while (index < end && (char.IsWhiteSpace(html[index]) || html[index] == '/'))
        {
            index++;
        }

        int nameStart = index;
        while (index < end && IsNameCharacter(html[index]))
        {
            index++;
        }

        return html.Substring(nameStart, index - nameStart);
    }

    static bool IsClosingTag(string html, int start, int end)
    {
        int index = start;
        while (index < end && char.IsWhiteSpace(html[index]))
        {
            index++;
        }

        return index < end && html[index] == '/';
    }

    static bool IsNameCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value == '-' || value == '_' || value == ':';
    }

    static bool StartsWith(string value, int index, string expected)
    {
        if (index + expected.Length > value.Length)
        {
            return false;
        }

        int offset = 0;
        while (offset < expected.Length)
        {
            if (value[index + offset] != expected[offset])
            {
                return false;
            }

            offset++;
        }

        return true;
    }

    static bool StartsWithIgnoreCase(string value, int index, string expected)
    {
        if (index + expected.Length > value.Length)
        {
            return false;
        }

        return string.Equals(value.Substring(index, expected.Length), expected, StringComparison.OrdinalIgnoreCase);
    }

    static string DecodeEntities(string value)
    {
        return value.Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
            .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase)
            .Replace("&apos;", "'", StringComparison.OrdinalIgnoreCase)
            .Replace("&#39;", "'", StringComparison.OrdinalIgnoreCase);
    }
}
