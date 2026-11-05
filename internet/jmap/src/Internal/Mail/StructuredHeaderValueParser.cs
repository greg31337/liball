using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace jmap;

internal class HeaderPositionResult
{
    internal int Index;
    internal bool IsValid;
}

internal class HeaderTimeParts
{
    internal int Hour;
    internal int Minute;
    internal int Second;
}

internal class HeaderDateParts
{
    internal int Year;
    internal int Month;
    internal int Day;
    internal int Hour;
    internal int Minute;
    internal int Second;
    internal int OffsetMinutes;
}

internal class StructuredHeaderValueParser
{
    internal HeaderValueParser Values = new HeaderValueParser();

    internal List<string>? ParseMessageIds(string? rawValue)
    {
        if (rawValue == null)
        {
            return null;
        }

        string value = Values.UnfoldValue(rawValue);
        List<string> result = new List<string>();
        HeaderPositionResult position = SkipCommentsAndWhitespace(value, 0);
        if (!position.IsValid)
        {
            return null;
        }

        int index = position.Index;
        while (index < value.Length)
        {
            if (value[index] != '<')
            {
                return null;
            }

            int end = FindMessageIdEnd(value, index + 1);
            if (end < 0)
            {
                return null;
            }

            string messageId = value.Substring(index + 1, end - index - 1);
            if (!IsMessageIdValue(messageId))
            {
                return null;
            }

            result.Add(messageId.Normalize(NormalizationForm.FormC));
            position = SkipCommentsAndWhitespace(value, end + 1);
            if (!position.IsValid)
            {
                return null;
            }

            index = position.Index;
        }

        return result.Count == 0 ? null : result;
    }

    internal string? ParseContentId(string? rawValue)
    {
        List<string>? values = ParseMessageIds(rawValue);
        if (values == null || values.Count != 1)
        {
            return null;
        }

        return values[0];
    }

    internal string? ParseDate(string? rawValue)
    {
        HeaderDateParts? parts = ParseDateParts(rawValue);
        if (parts == null)
        {
            return null;
        }

        return FormatDateParts(parts);
    }

    internal string? ParseUtcDate(string? rawValue)
    {
        HeaderDateParts? parts = ParseDateParts(rawValue);
        if (parts == null)
        {
            return null;
        }

        if (parts.Year == 9999 && parts.OffsetMinutes < 0)
        {
            return null;
        }

        int safeSecond = parts.Second == 60 ? 59 : parts.Second;
        DateTime utc = new DateTime(parts.Year, parts.Month, parts.Day, parts.Hour, parts.Minute, safeSecond,
            DateTimeKind.Unspecified);
        if (parts.Second == 60)
        {
            utc = utc.AddSeconds(1);
        }

        utc = utc.AddMinutes(-parts.OffsetMinutes);
        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
    }

    internal List<string>? ParseUrls(string? rawValue)
    {
        if (rawValue == null)
        {
            return null;
        }

        string value = Values.UnfoldValue(rawValue);
        List<string> result = new List<string>();
        HeaderPositionResult position = SkipCommentsAndWhitespace(value, 0);
        if (!position.IsValid || position.Index >= value.Length || value[position.Index] != '<')
        {
            return null;
        }

        int index = position.Index;
        while (index < value.Length)
        {
            if (value[index] != '<')
            {
                break;
            }

            int end = FindUrlEnd(value, index + 1);
            if (end < 0)
            {
                return null;
            }

            string url = RemoveUrlWhitespace(value.Substring(index + 1, end - index - 1));
            if (!IsAbsoluteUrl(url))
            {
                return null;
            }

            result.Add(url);
            position = SkipCommentsAndWhitespace(value, end + 1);
            if (!position.IsValid)
            {
                return null;
            }

            index = position.Index;
            if (index >= value.Length || value[index] != ',')
            {
                break;
            }

            position = SkipCommentsAndWhitespace(value, index + 1);
            if (!position.IsValid)
            {
                return null;
            }

            index = position.Index;
            if (index >= value.Length || value[index] != '<')
            {
                break;
            }
        }

        return result.Count == 0 ? null : result;
    }

    internal string? ParseContentLocation(string? rawValue)
    {
        if (rawValue == null)
        {
            return null;
        }

        string unfolded = Values.UnfoldValue(rawValue);
        string withoutComments = RemoveHeaderComments(unfolded);
        string value = TrimHeaderWhitespace(withoutComments);
        if (!IsUriReference(value))
        {
            return null;
        }

        return value;
    }

    internal HeaderDateParts? ParseDateParts(string? rawValue)
    {
        if (rawValue == null)
        {
            return null;
        }

        string? stripped = StripDateComments(rawValue);
        if (stripped == null)
        {
            return null;
        }

        string value = RemoveWhitespaceAroundColons(stripped);
        int comma = FindCharacter(value, ',');
        string? dayName = null;
        if (comma >= 0)
        {
            dayName = TrimHeaderWhitespace(value.Substring(0, comma));
            if (!IsDayName(dayName))
            {
                return null;
            }

            value = value.Substring(comma + 1);
        }

        List<string> tokens = SplitHeaderWhitespace(value);
        if (tokens.Count != 5 || tokens[0].Length < 1 || tokens[0].Length > 2)
        {
            return null;
        }

        int day = ParseUnsignedNumber(tokens[0]);
        int month = ParseMonthName(tokens[1]);
        int year = ParseHeaderYear(tokens[2]);
        HeaderTimeParts? time = ParseHeaderTime(tokens[3]);
        int offset = ParseZoneOffset(tokens[4]);
        if (day < 1 || month < 1 || year < 1900 || time == null || offset == int.MinValue)
        {
            return null;
        }

        if (year > 9999 || day > DateTime.DaysInMonth(year, month))
        {
            return null;
        }

        DateTime date = new DateTime(year, month, day);
        if (dayName != null && !string.Equals(dayName, date.ToString("ddd", CultureInfo.InvariantCulture),
            global::System.StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        HeaderDateParts result = new HeaderDateParts();
        result.Year = year;
        result.Month = month;
        result.Day = day;
        result.Hour = time.Hour;
        result.Minute = time.Minute;
        result.Second = time.Second;
        result.OffsetMinutes = offset;
        return result;
    }

    internal string FormatDateParts(HeaderDateParts parts)
    {
        int offset = parts.OffsetMinutes;
        char sign = '+';
        if (offset < 0)
        {
            sign = '-';
            offset = -offset;
        }

        return parts.Year.ToString("D4", CultureInfo.InvariantCulture) + "-" +
            parts.Month.ToString("D2", CultureInfo.InvariantCulture) + "-" +
            parts.Day.ToString("D2", CultureInfo.InvariantCulture) + "T" +
            parts.Hour.ToString("D2", CultureInfo.InvariantCulture) + ":" +
            parts.Minute.ToString("D2", CultureInfo.InvariantCulture) + ":" +
            parts.Second.ToString("D2", CultureInfo.InvariantCulture) + sign +
            (offset / 60).ToString("D2", CultureInfo.InvariantCulture) + ":" +
            (offset % 60).ToString("D2", CultureInfo.InvariantCulture);
    }

    internal HeaderTimeParts? ParseHeaderTime(string value)
    {
        int firstColon = FindCharacter(value, ':');
        if (firstColon != 2)
        {
            return null;
        }

        int secondColon = FindCharacterAfter(value, ':', firstColon + 1);
        string hourText = value.Substring(0, firstColon);
        string minuteText;
        string secondText = "00";
        if (secondColon < 0)
        {
            minuteText = value.Substring(firstColon + 1);
        }
        else
        {
            minuteText = value.Substring(firstColon + 1, secondColon - firstColon - 1);
            secondText = value.Substring(secondColon + 1);
        }

        if (minuteText.Length != 2 || secondText.Length != 2)
        {
            return null;
        }

        int hour = ParseUnsignedNumber(hourText);
        int minute = ParseUnsignedNumber(minuteText);
        int second = ParseUnsignedNumber(secondText);
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 60)
        {
            return null;
        }

        HeaderTimeParts result = new HeaderTimeParts();
        result.Hour = hour;
        result.Minute = minute;
        result.Second = second;
        return result;
    }

    internal int ParseZoneOffset(string value)
    {
        if (value.Length == 5 && (value[0] == '+' || value[0] == '-'))
        {
            int hour = ParseUnsignedNumber(value.Substring(1, 2));
            int minute = ParseUnsignedNumber(value.Substring(3, 2));
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
            {
                return int.MinValue;
            }

            int offset = (hour * 60) + minute;
            return value[0] == '-' ? -offset : offset;
        }

        if (EqualsToken(value, "UT") || EqualsToken(value, "GMT") || EqualsToken(value, "Z"))
        {
            return 0;
        }

        if (EqualsToken(value, "EDT"))
        {
            return -240;
        }

        if (EqualsToken(value, "EST") || EqualsToken(value, "CDT"))
        {
            return -300;
        }

        if (EqualsToken(value, "CST") || EqualsToken(value, "MDT"))
        {
            return -360;
        }

        if (EqualsToken(value, "MST") || EqualsToken(value, "PDT"))
        {
            return -420;
        }

        if (EqualsToken(value, "PST"))
        {
            return -480;
        }

        if (value.Length == 1 && IsMilitaryZone(value[0]))
        {
            return 0;
        }

        if (value.Length > 1 && IsAlphabeticZone(value))
        {
            return 0;
        }

        return int.MinValue;
    }

    internal int ParseHeaderYear(string value)
    {
        int year = ParseUnsignedNumber(value);
        if (year < 0)
        {
            return -1;
        }

        if (value.Length == 2)
        {
            return year < 50 ? year + 2000 : year + 1900;
        }

        if (value.Length == 3)
        {
            return year + 1900;
        }

        return value.Length >= 4 ? year : -1;
    }

    internal int ParseMonthName(string value)
    {
        string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        int index = 0;
        while (index < months.Length)
        {
            if (EqualsToken(value, months[index]))
            {
                return index + 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int ParseUnsignedNumber(string value)
    {
        if (value.Length == 0)
        {
            return -1;
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            if (result > 1000000)
            {
                return -1;
            }

            result = (result * 10) + (character - '0');
            index = index + 1;
        }

        return result;
    }

    internal bool IsDayName(string value)
    {
        return EqualsToken(value, "Mon") || EqualsToken(value, "Tue") || EqualsToken(value, "Wed") ||
            EqualsToken(value, "Thu") || EqualsToken(value, "Fri") || EqualsToken(value, "Sat") ||
            EqualsToken(value, "Sun");
    }

    internal bool IsMilitaryZone(char character)
    {
        char upper = char.ToUpperInvariant(character);
        return upper >= 'A' && upper <= 'Z' && upper != 'J';
    }

    internal bool IsAlphabeticZone(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!IsAsciiLetter(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return value.Length > 0;
    }

    internal bool EqualsToken(string left, string right)
    {
        return string.Equals(left, right, global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal string? StripDateComments(string value)
    {
        string unfolded = Values.UnfoldValue(value);
        StringBuilder result = new StringBuilder();
        int index = 0;
        int depth = 0;
        bool escaped = false;
        bool pendingWhitespace = false;
        while (index < unfolded.Length)
        {
            char character = unfolded[index];
            if (depth > 0)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '(')
                {
                    depth = depth + 1;
                }
                else if (character == ')')
                {
                    depth = depth - 1;
                    pendingWhitespace = true;
                }
            }
            else if (character == '(')
            {
                depth = 1;
                pendingWhitespace = true;
            }
            else if (IsHeaderWhitespace(character))
            {
                pendingWhitespace = true;
            }
            else
            {
                if (pendingWhitespace && result.Length > 0)
                {
                    result.Append(' ');
                }

                result.Append(character);
                pendingWhitespace = false;
            }

            index = index + 1;
        }

        return depth == 0 ? result.ToString() : null;
    }

    internal string RemoveWhitespaceAroundColons(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == ' ')
            {
                int next = index;
                while (next < value.Length && value[next] == ' ')
                {
                    next = next + 1;
                }

                bool besideColon = (result.Length > 0 && result[result.Length - 1] == ':') ||
                    (next < value.Length && value[next] == ':');
                if (!besideColon)
                {
                    result.Append(' ');
                }

                index = next;
            }
            else
            {
                result.Append(value[index]);
                index = index + 1;
            }
        }

        return TrimHeaderWhitespace(result.ToString());
    }

    internal List<string> SplitHeaderWhitespace(string value)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && IsHeaderWhitespace(value[index]))
            {
                index = index + 1;
            }

            int start = index;
            while (index < value.Length && !IsHeaderWhitespace(value[index]))
            {
                index = index + 1;
            }

            if (index > start)
            {
                result.Add(value.Substring(start, index - start));
            }
        }

        return result;
    }

    internal HeaderPositionResult SkipCommentsAndWhitespace(string value, int start)
    {
        HeaderPositionResult result = new HeaderPositionResult();
        result.IsValid = true;
        int index = start;
        while (index < value.Length)
        {
            if (IsHeaderWhitespace(value[index]))
            {
                index = index + 1;
            }
            else if (value[index] == '(')
            {
                int depth = 1;
                index = index + 1;
                bool escaped = false;
                while (index < value.Length && depth > 0)
                {
                    char character = value[index];
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (character == '\\')
                    {
                        escaped = true;
                    }
                    else if (character == '(')
                    {
                        depth = depth + 1;
                    }
                    else if (character == ')')
                    {
                        depth = depth - 1;
                    }

                    index = index + 1;
                }

                if (depth != 0)
                {
                    result.IsValid = false;
                    result.Index = value.Length;
                    return result;
                }
            }
            else
            {
                break;
            }
        }

        result.Index = index;
        return result;
    }

    internal int FindMessageIdEnd(string value, int start)
    {
        int index = start;
        bool quoted = false;
        bool escaped = false;
        int bracketDepth = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\' && quoted)
            {
                escaped = true;
            }
            else if (character == '"' && bracketDepth == 0)
            {
                quoted = !quoted;
            }
            else if (character == '[' && !quoted)
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (character == ']' && !quoted && bracketDepth > 0)
            {
                bracketDepth = bracketDepth - 1;
            }
            else if (character == '>' && !quoted && bracketDepth == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool IsMessageIdValue(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int at = -1;
        int bracketDepth = 0;
        bool quoted = false;
        bool escaped = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\' && quoted)
            {
                escaped = true;
            }
            else if (character == '"' && bracketDepth == 0)
            {
                quoted = !quoted;
            }
            else if (character == '[' && !quoted)
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (character == ']' && !quoted)
            {
                if (bracketDepth == 0)
                {
                    return false;
                }

                bracketDepth = bracketDepth - 1;
            }
            else if (character == '@' && !quoted && bracketDepth == 0)
            {
                if (at >= 0)
                {
                    return false;
                }

                at = index;
            }
            else if ((character <= 32 || character == 127 || character == '<' || character == '>' ||
                character == '(' || character == ')') && !quoted && bracketDepth == 0)
            {
                return false;
            }

            index = index + 1;
        }

        if (quoted || bracketDepth != 0 || escaped || at <= 0 || at + 1 >= value.Length)
        {
            return false;
        }

        return IsMessageIdLeft(value.Substring(0, at)) && IsMessageIdRight(value.Substring(at + 1));
    }

    internal bool IsMessageIdLeft(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
        {
            int index = 1;
            bool escaped = false;
            while (index + 1 < value.Length)
            {
                char character = value[index];
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"' || character == '\r' || character == '\n')
                {
                    return false;
                }

                index = index + 1;
            }

            return !escaped;
        }

        return IsDotAtomText(value);
    }

    internal bool IsMessageIdRight(string value)
    {
        if (value.Length >= 2 && value[0] == '[' && value[value.Length - 1] == ']')
        {
            int index = 1;
            bool escaped = false;
            while (index + 1 < value.Length)
            {
                char character = value[index];
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '[' || character == ']' || character == '\r' || character == '\n')
                {
                    return false;
                }

                index = index + 1;
            }

            return !escaped;
        }

        return IsDotAtomText(value);
    }

    internal bool IsDotAtomText(string value)
    {
        if (value.Length == 0 || value[0] == '.' || value[value.Length - 1] == '.')
        {
            return false;
        }

        int index = 0;
        bool previousDot = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                if (previousDot)
                {
                    return false;
                }

                previousDot = true;
            }
            else
            {
                if (!IsAtextCharacter(character))
                {
                    return false;
                }

                previousDot = false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsAtextCharacter(char character)
    {
        if (IsAsciiLetter(character) || IsAsciiDigit(character) || character > 127)
        {
            return true;
        }

        return character == '!' || character == '#' || character == '$' || character == '%' || character == '&' ||
            character == '\'' || character == '*' || character == '+' || character == '-' || character == '/' ||
            character == '=' || character == '?' || character == '^' || character == '_' || character == '`' ||
            character == '{' || character == '|' || character == '}' || character == '~';
    }

    internal int FindUrlEnd(string value, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == '>')
            {
                return index;
            }

            if (value[index] == '<')
            {
                return -1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal string RemoveUrlWhitespace(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (!IsHeaderWhitespace(value[index]))
            {
                result.Append(value[index]);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal bool IsAbsoluteUrl(string value)
    {
        if (value.Length < 3 || !IsAsciiLetter(value[0]))
        {
            return false;
        }

        int index = 1;
        while (index < value.Length && value[index] != ':')
        {
            char character = value[index];
            if (!IsAsciiLetter(character) && !IsAsciiDigit(character) && character != '+' && character != '-' &&
                character != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return index > 0 && index + 1 < value.Length && index < value.Length &&
            IsValidUriCharacters(value);
    }

    internal bool IsUriReference(string value)
    {
        if (value.Length == 0 || !IsValidUriCharacters(value))
        {
            return false;
        }

        return true;
    }

    internal bool IsValidUriCharacters(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= 32 || character > 126 || character == '"' || character == '<' || character == '>')
            {
                return false;
            }

            if (character == '%')
            {
                if (index + 2 >= value.Length || !IsHexDigit(value[index + 1]) || !IsHexDigit(value[index + 2]))
                {
                    return false;
                }

                index = index + 2;
            }

            index = index + 1;
        }

        return true;
    }

    internal string RemoveHeaderComments(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        int depth = 0;
        bool escaped = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (depth > 0)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '(')
                {
                    depth = depth + 1;
                }
                else if (character == ')')
                {
                    depth = depth - 1;
                }
            }
            else if (character == '(')
            {
                depth = 1;
            }
            else
            {
                result.Append(character);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal int FindCharacter(string value, char character)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindCharacterAfter(string value, char character, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal string TrimHeaderWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && IsHeaderWhitespace(value[start]))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && IsHeaderWhitespace(value[end - 1]))
        {
            end = end - 1;
        }

        return start == 0 && end == value.Length ? value : value.Substring(start, end - start);
    }

    internal bool IsHeaderWhitespace(char character)
    {
        return character == ' ' || character == '\t' || character == '\r' || character == '\n';
    }

    internal bool IsAsciiLetter(char character)
    {
        return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
    }

    internal bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal bool IsHexDigit(char character)
    {
        return IsAsciiDigit(character) || (character >= 'A' && character <= 'F') ||
            (character >= 'a' && character <= 'f');
    }
}
