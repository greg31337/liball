using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;

internal static class HtmlLanguageTags
{
    internal static HtmlLanguageTagResult Parse(string value,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        if (value.Length > limits.GetMaximumAttributeValueCharacters())
        {
            return CreateFailure(value, 0, value.Length, true,
                "The language tag exceeds the attribute-value character limit.");
        }

        int start = 0;
        int end = value.Length;
        while (start < end && Ascii.IsHtmlWhitespace(value[start]))
        {
            cancellationToken.ThrowIfCancellationRequested();
            start = start + 1;
        }

        while (end > start && Ascii.IsHtmlWhitespace(value[end - 1]))
        {
            cancellationToken.ThrowIfCancellationRequested();
            end = end - 1;
        }

        if (start == end)
        {
            return CreateFailure(value, start, end, false,
                "A language tag cannot be empty.");
        }

        List<string> subtags = new List<string>();
        List<int> positions = new List<int>();
        int position = start;
        while (position <= end)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int subtagStart = position;
            while (position < end && value[position] != '-')
            {
                if (!Ascii.IsAlphanumeric(value[position]))
                {
                    return CreateFailure(value, position, position + 1,
                        false,
                        "A language subtag can contain only ASCII letters and digits.");
                }

                position = position + 1;
            }

            int length = position - subtagStart;
            if (length == 0 || length > 8)
            {
                return CreateFailure(value, subtagStart, position, false,
                    "A language subtag must contain between one and eight characters.");
            }

            positions.Add(subtagStart);
            subtags.Add(value.Substring(subtagStart, length));
            if (position == end)
            {
                break;
            }

            position = position + 1;
        }

        string primary = subtags[0];
        bool privateUse = IsAsciiEqual(primary, "x");
        bool privateUseStart = privateUse;
        bool grandfathered = IsAsciiEqual(primary, "i");
        if (!privateUse && !grandfathered
            && (primary.Length < 2 || !IsAllAlpha(primary)))
        {
            return CreateFailure(value, positions[0],
                positions[0] + primary.Length, false,
                "The primary language subtag must contain two to eight ASCII letters.");
        }

        if ((privateUse || grandfathered) && subtags.Count < 2)
        {
            return CreateFailure(value, positions[0], end, false,
                "This language-tag prefix requires a following subtag.");
        }

        int index = 1;
        while (index < subtags.Count)
        {
            string current = subtags[index];
            if (!privateUse && current.Length == 1)
            {
                if (index + 1 == subtags.Count)
                {
                    return CreateFailure(value, positions[index],
                        positions[index] + current.Length, false,
                        "An extension or private-use singleton requires a following subtag.");
                }

                if (IsAsciiEqual(current, "x"))
                {
                    privateUse = true;
                }
                else if (subtags[index + 1].Length < 2)
                {
                    return CreateFailure(value, positions[index],
                        positions[index] + current.Length, false,
                        "An extension singleton requires a following subtag.");
                }
            }

            index = index + 1;
        }

        StringBuilder normalized = new StringBuilder();
        index = 0;
        bool lowerOnly = privateUseStart || grandfathered;
        while (index < subtags.Count)
        {
            if (index > 0)
            {
                normalized.Append('-');
            }

            string current = subtags[index];
            string folded;
            if (index == 0 || lowerOnly)
            {
                folded = FoldLower(current);
            }
            else if (current.Length == 4 && IsAllAlpha(current))
            {
                folded = FoldTitle(current);
            }
            else if (current.Length == 2 && IsAllAlpha(current)
                || current.Length == 3 && IsAllDigits(current))
            {
                folded = FoldUpper(current);
            }
            else
            {
                folded = FoldLower(current);
            }

            normalized.Append(folded);
            if (current.Length == 1)
            {
                lowerOnly = true;
            }

            index = index + 1;
        }

        string normalizedTag = normalized.ToString();
        return new HtmlLanguageTagResult(true, false, normalizedTag,
            FoldLower(subtags[0]), new HtmlDiagnostic[0]);
    }

    internal static HtmlLanguageTagResult CreateFailure(string source,
        int start, int end, bool limitExceeded, string message)
    {
        HtmlDiagnosticCode code = limitExceeded
            ? HtmlDiagnosticCode.MicrosyntaxLimitExceeded
            : HtmlDiagnosticCode.MicrosyntaxInvalid;
        HtmlDiagnostic diagnostic = HtmlMicrosyntaxDiagnostics.Create(source,
            start, end, code, message);
        return new HtmlLanguageTagResult(false, limitExceeded, "", "",
            new[] { diagnostic });
    }

    internal static bool IsAllAlpha(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!Ascii.IsAlpha(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsAllDigits(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!Ascii.IsDigit(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsAsciiEqual(string value, string expected)
    {
        return Ascii.EqualsIgnoreCase(value.AsSpan(), expected.AsSpan());
    }

    internal static string FoldLower(string value)
    {
        char[] result = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        return new string(result);
    }

    internal static string FoldUpper(string value)
    {
        char[] result = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            result[index] = current >= 'a' && current <= 'z'
                ? (char)(current - 32) : current;
            index = index + 1;
        }

        return new string(result);
    }

    internal static string FoldTitle(string value)
    {
        string lower = FoldLower(value);
        char[] result = lower.ToCharArray();
        if (result[0] >= 'a' && result[0] <= 'z')
        {
            result[0] = (char)(result[0] - 32);
        }

        return new string(result);
    }
}
