using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailDateZoneValue
{
    internal bool success;
    internal EmailDateZoneKind kind;
    internal string? original;
    internal int offsetMinutes;
    internal bool offsetKnown;
    internal bool obsolete;

    internal EmailDateZoneValue()
    {
        success = false;
        kind = EmailDateZoneKind.Numeric;
        original = null;
        offsetMinutes = 0;
        offsetKnown = false;
        obsolete = false;
    }
}
internal class EmailDateParserWorker
{
    byte[] input;
    List<EmailHeaderToken> tokens;
    List<int> significant;
    EmailDateParserOptions options;
    EmailSourcePosition initialPosition;
    EmailIssueList issues;
    int lexicalConsumedByteCount;
    bool obsolete;
    bool obsoleteIssueAdded;

    internal EmailDateParserWorker(byte[] input, EmailHeaderLexicalResult lexical, EmailDateParserOptions options, EmailSourcePosition initialPosition)
    {
        this.input = input;
        tokens = lexical.GetTokens();
        significant = new List<int>();
        this.options = options;
        this.initialPosition = initialPosition;
        issues = lexical.GetIssues();
        lexicalConsumedByteCount = lexical.GetConsumedByteCount();
        obsolete = false;
        obsoleteIssueAdded = false;
        int index = 0;
        while (index < tokens.Count)
        {
            if (tokens[index].GetKind() != EmailHeaderTokenKind.Cfws)
            {
                significant.Add(index);
            }

            index++;
        }
    }

    internal EmailDateParseResult Parse()
    {
        if (issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal))
        {
            return new EmailDateParseResult(null, issues, lexicalConsumedByteCount, input.Length);
        }

        if (significant.Count == 0)
        {
            AddMalformed("The date value is empty.", initialPosition, "No date value was produced.");
            return new EmailDateParseResult(null, issues, input.Length, input.Length);
        }

        int cursor = 0;
        bool dayOfWeekPresent = false;
        EmailDateDayOfWeek dayOfWeek = EmailDateDayOfWeek.Sunday;
        int dayOfWeekToken = -1;
        int parsedDayOfWeek = ParseDayOfWeek(GetTextAtSignificant(cursor));
        if (parsedDayOfWeek >= 0)
        {
            dayOfWeekPresent = true;
            dayOfWeek = (EmailDateDayOfWeek)parsedDayOfWeek;
            dayOfWeekToken = significant[cursor];
            cursor++;
            if (cursor < significant.Count && IsSpecialAtSignificant(cursor, 44))
            {
                cursor++;
            }
            else
            {
                MarkObsolete(GetPositionAtToken(dayOfWeekToken), "The day-of-week is not followed by a comma.", "The day-of-week was retained.");
            }
        }
        else if (IsSpecialAtSignificant(cursor, 44))
        {
            AddMalformed("The date value starts with a comma but has no valid day-of-week.", GetPositionAtSignificant(cursor), "No date value was produced.");
            return new EmailDateParseResult(null, issues, input.Length, input.Length);
        }

        int remaining = significant.Count - cursor;
        bool secondPresent = false;
        if (remaining == 9)
        {
            secondPresent = true;
        }
        else if (remaining != 7)
        {
            AddMalformed("The date value does not match the RFC date-time token structure.", GetPositionAtSignificant(cursor), "No date value was produced.");
            return new EmailDateParseResult(null, issues, input.Length, input.Length);
        }

        int dayIndex = cursor;
        int monthIndex = cursor + 1;
        int yearIndex = cursor + 2;
        int hourIndex = cursor + 3;
        int firstColonIndex = cursor + 4;
        int minuteIndex = cursor + 5;
        int secondColonIndex = -1;
        int secondIndex = -1;
        int zoneIndex = cursor + 6;
        if (secondPresent)
        {
            secondColonIndex = cursor + 6;
            secondIndex = cursor + 7;
            zoneIndex = cursor + 8;
        }

        if (!IsAtomAtSignificant(dayIndex) || !IsAtomAtSignificant(monthIndex) || !IsAtomAtSignificant(yearIndex) || !IsAtomAtSignificant(hourIndex) || !IsSpecialAtSignificant(firstColonIndex, 58) || !IsAtomAtSignificant(minuteIndex) || secondPresent && (!IsSpecialAtSignificant(secondColonIndex, 58) || !IsAtomAtSignificant(secondIndex)) || !IsAtomAtSignificant(zoneIndex))
        {
            AddMalformed("The date value contains an unexpected token.", GetPositionAtSignificant(cursor), "No date value was produced.");
            return new EmailDateParseResult(null, issues, input.Length, input.Length);
        }

        if (!HasCfwsBetweenSignificant(dayIndex, monthIndex) || !HasCfwsBetweenSignificant(monthIndex, yearIndex) || !HasCfwsBetweenSignificant(yearIndex, hourIndex) || !HasCfwsBetweenSignificant(secondPresent ? secondIndex : minuteIndex, zoneIndex))
        {
            AddMalformed("Required whitespace is missing between date-time components.", GetPositionAtSignificant(cursor), "No date value was produced.");
            return new EmailDateParseResult(null, issues, input.Length, input.Length);
        }

        MarkObsoleteCfws(dayOfWeekPresent, dayOfWeekToken, significant[zoneIndex]);
        MarkObsoleteAroundColon(hourIndex, firstColonIndex, minuteIndex);
        if (secondPresent)
        {
            MarkObsoleteAroundColon(minuteIndex, secondColonIndex, secondIndex);
        }

        string dayText = GetTextAtSignificant(dayIndex);
        string monthText = GetTextAtSignificant(monthIndex);
        string yearText = GetTextAtSignificant(yearIndex);
        string hourText = GetTextAtSignificant(hourIndex);
        string minuteText = GetTextAtSignificant(minuteIndex);
        string secondText = secondPresent ? GetTextAtSignificant(secondIndex) : "0";
        int day = ParseDigits(dayText, 1, 2);
        int month = ParseMonth(monthText);
        int originalYearDigitCount = IsAllDigits(yearText) ? yearText.Length : 0;
        int year = ParseYear(yearText, GetPositionAtSignificant(yearIndex));
        int hour = ParseDigits(hourText, 2, 2);
        int minute = ParseDigits(minuteText, 2, 2);
        int second = ParseDigits(secondText, 2, 2);
        if (!secondPresent)
        {
            second = 0;
        }

        EmailDateZoneValue zone = ParseZone(GetTextAtSignificant(zoneIndex), GetPositionAtSignificant(zoneIndex));
        if (day < 1 || month < 1 || year < 1900 || hour < 0 || minute < 0 || second < 0 || !zone.success)
        {
            AddSemanticInvalid("One or more date-time components are malformed or outside the RFC syntax.", GetPositionAtSignificant(cursor), "No date value was produced.");
            return new EmailDateParseResult(null, issues, input.Length, input.Length);
        }

        if (day > EmailDateMath.GetDaysInMonth(year, month) || hour > 23 || minute > 59 || second > 60)
        {
            AddSemanticInvalid("The date-time contains an impossible calendar date or clock time.", GetPositionAtSignificant(cursor), "No date value was produced.");
            return new EmailDateParseResult(null, issues, input.Length, input.Length);
        }

        if (zone.obsolete)
        {
            obsolete = true;
        }

        bool dayOfWeekMatches = true;
        if (dayOfWeekPresent)
        {
            dayOfWeekMatches = EmailDateMath.GetDayOfWeek(year, month, day) == dayOfWeek;
            if (!dayOfWeekMatches)
            {
                AddRecoverable(EmailIssueCode.DateDayOfWeekMismatch, "The stated day-of-week does not match the calendar date.", GetPositionAtToken(dayOfWeekToken), "The date was retained and marked as having a mismatched day-of-week.");
            }
        }

        EmailSourcePosition valuePosition = GetPositionAtSignificant(0);
        EmailDateTime value = new EmailDateTime(year, month, day, hour, minute, second, secondPresent, second == 60, dayOfWeekPresent, dayOfWeek, dayOfWeekMatches, zone.kind, zone.original, zone.offsetMinutes, zone.offsetKnown, obsolete, originalYearDigitCount, input, valuePosition);
        return new EmailDateParseResult(value, issues, input.Length, input.Length);
    }

    EmailDateZoneValue ParseZone(string text, EmailSourcePosition position)
    {
        EmailDateZoneValue value = new EmailDateZoneValue();
        value.original = text;
        if (text.Length == 5 && (text[0] == '+' || text[0] == '-') && IsDigits(text, 1, 4))
        {
            int hours = (text[1] - '0') * 10 + text[2] - '0';
            int minutes = (text[3] - '0') * 10 + text[4] - '0';
            if (minutes > 59)
            {
                AddSemanticInvalid("The numeric zone contains an invalid minute component.", position, "No date value was produced.");
                return value;
            }

            int offset = hours * 60 + minutes;
            if (text[0] == '-')
            {
                offset = -offset;
            }

            value.success = true;
            value.kind = EmailDateZoneKind.Numeric;
            value.offsetMinutes = offset;
            value.offsetKnown = !(text[0] == '-' && offset == 0);
            return value;
        }

        if (!IsAsciiLetters(text))
        {
            AddMalformed("The time zone is neither a numeric nor alphabetic RFC zone.", position, "No date value was produced.");
            return value;
        }

        int knownOffset = GetNamedZoneOffset(text);
        value.success = true;
        value.obsolete = true;
        if (knownOffset != int.MinValue)
        {
            value.kind = EmailDateZoneKind.ObsoleteNamed;
            value.offsetMinutes = knownOffset;
            value.offsetKnown = true;
            MarkObsolete(position, "The date uses an obsolete named time zone.", "The named zone was mapped to its RFC-defined numeric offset.");
            return value;
        }

        value.kind = text.Length == 1 && !string.Equals(text, "J", StringComparison.OrdinalIgnoreCase) ? EmailDateZoneKind.ObsoleteMilitary : EmailDateZoneKind.UnknownNamed;
        value.offsetMinutes = 0;
        value.offsetKnown = false;
        MarkObsolete(position, "The date uses an obsolete alphabetic time zone.", "The original zone token was preserved.");
        AddRecoverable(EmailIssueCode.DateUnknownZone, "The alphabetic time zone does not identify a reliable UTC offset.", position, "The original zone token was preserved and its offset was marked unknown.");
        return value;
    }

    int ParseYear(string text, EmailSourcePosition position)
    {
        if (!IsAllDigits(text) || text.Length < 2)
        {
            return -1;
        }

        int parsed = ParseDigits(text, text.Length, text.Length);
        if (parsed < 0)
        {
            return -1;
        }

        if (text.Length == 2)
        {
            MarkObsolete(position, "The date uses an obsolete two-digit year.", "The RFC 5322 century mapping was applied.");
            return parsed < 50 ? parsed + 2000 : parsed + 1900;
        }

        if (text.Length == 3)
        {
            MarkObsolete(position, "The date uses an obsolete three-digit year.", "1900 was added as required by RFC 5322.");
            return parsed + 1900;
        }

        return parsed;
    }

    void MarkObsoleteCfws(bool dayOfWeekPresent, int dayOfWeekToken, int zoneToken)
    {
        int index = 0;
        while (index < tokens.Count)
        {
            EmailHeaderToken token = tokens[index];
            if (token.IsObsolete())
            {
                MarkObsolete(token.GetSourcePosition(), "The date contains obsolete folding or lexical syntax.", "The original syntax was retained.");
            }

            if (token.GetKind() == EmailHeaderTokenKind.Cfws && ContainsComment(token.GetRawBytes()))
            {
                bool trailing = index > zoneToken;
                if (!trailing)
                {
                    MarkObsolete(token.GetSourcePosition(), "The date contains obsolete comment placement.", "The comment was ignored while parsing the date.");
                }
            }

            index++;
        }

        if (dayOfWeekPresent && dayOfWeekToken > 0 && HasCfwsBetweenTokens(-1, dayOfWeekToken))
        {
            int first = FindCfwsBetweenTokens(-1, dayOfWeekToken);
            if (first >= 0 && ContainsComment(tokens[first].GetRawBytes()))
            {
                MarkObsolete(tokens[first].GetSourcePosition(), "The date contains a comment before its day-of-week.", "The comment was ignored while parsing the date.");
            }
        }
    }

    void MarkObsoleteAroundColon(int leftSignificant, int colonSignificant, int rightSignificant)
    {
        if (HasCfwsBetweenSignificant(leftSignificant, colonSignificant) || HasCfwsBetweenSignificant(colonSignificant, rightSignificant))
        {
            MarkObsolete(GetPositionAtSignificant(colonSignificant), "The time contains obsolete whitespace around a colon.", "The whitespace was ignored while parsing the time.");
        }
    }

    void MarkObsolete(EmailSourcePosition position, string description, string recovery)
    {
        obsolete = true;
        if (!obsoleteIssueAdded)
        {
            AddRecoverable(EmailIssueCode.DateObsoleteSyntax, description, position, recovery);
            obsoleteIssueAdded = true;
        }
    }

    void AddMalformed(string description, EmailSourcePosition position, string recovery)
    {
        AddRecoverable(EmailIssueCode.DateMalformed, description, position, recovery);
    }

    void AddSemanticInvalid(string description, EmailSourcePosition position, string recovery)
    {
        if (!issues.ContainsCode(EmailIssueCode.DateSemanticInvalid))
        {
            AddRecoverable(EmailIssueCode.DateSemanticInvalid, description, position, recovery);
        }
    }

    void AddRecoverable(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailDateParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    bool IsAtomAtSignificant(int index)
    {
        if (index < 0 || index >= significant.Count)
        {
            return false;
        }

        EmailHeaderTokenKind kind = tokens[significant[index]].GetKind();
        return kind == EmailHeaderTokenKind.Atom;
    }

    bool IsSpecialAtSignificant(int index, byte value)
    {
        if (index < 0 || index >= significant.Count)
        {
            return false;
        }

        EmailHeaderToken token = tokens[significant[index]];
        byte[] bytes = token.GetValueBytes();
        return token.GetKind() == EmailHeaderTokenKind.Special && bytes.Length == 1 && bytes[0] == value;
    }

    bool HasCfwsBetweenSignificant(int left, int right)
    {
        return HasCfwsBetweenTokens(significant[left], significant[right]);
    }

    bool HasCfwsBetweenTokens(int left, int right)
    {
        return FindCfwsBetweenTokens(left, right) >= 0;
    }

    int FindCfwsBetweenTokens(int left, int right)
    {
        int index = left + 1;
        while (index < right)
        {
            if (tokens[index].GetKind() == EmailHeaderTokenKind.Cfws)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    string GetTextAtSignificant(int index)
    {
        if (index < 0 || index >= significant.Count)
        {
            return "";
        }

        return Encoding.ASCII.GetString(tokens[significant[index]].GetValueBytes());
    }

    EmailSourcePosition GetPositionAtSignificant(int index)
    {
        if (index < 0 || index >= significant.Count)
        {
            return initialPosition;
        }

        return GetPositionAtToken(significant[index]);
    }

    EmailSourcePosition GetPositionAtToken(int index)
    {
        if (index < 0 || index >= tokens.Count)
        {
            return initialPosition;
        }

        return tokens[index].GetSourcePosition();
    }

    static int ParseDigits(string text, int minimumLength, int maximumLength)
    {
        if (text == null || text.Length < minimumLength || text.Length > maximumLength)
        {
            return -1;
        }

        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9' || value > (int.MaxValue - (text[index] - '0')) / 10)
            {
                return -1;
            }

            value = value * 10 + text[index] - '0';
            index++;
        }

        return value;
    }

    static bool IsAllDigits(string text)
    {
        return text != null && text.Length > 0 && IsDigits(text, 0, text.Length);
    }

    static bool IsDigits(string text, int start, int count)
    {
        int index = start;
        int end = start + count;
        while (index < end)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsAsciiLetters(string text)
    {
        if (text == null || text.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (!(value >= 'A' && value <= 'Z') && !(value >= 'a' && value <= 'z'))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool ContainsComment(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] == 40)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static int ParseMonth(string text)
    {
        string[] names = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        int index = 0;
        while (index < names.Length)
        {
            if (string.Equals(text, names[index], StringComparison.OrdinalIgnoreCase))
            {
                return index + 1;
            }

            index++;
        }

        return -1;
    }

    static int ParseDayOfWeek(string text)
    {
        string[] names = new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        int index = 0;
        while (index < names.Length)
        {
            if (string.Equals(text, names[index], StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static int GetNamedZoneOffset(string text)
    {
        string[] names = new string[] { "UT", "GMT", "EDT", "EST", "CDT", "CST", "MDT", "MST", "PDT", "PST" };
        int[] offsets = new int[] { 0, 0, -240, -300, -300, -360, -360, -420, -420, -480 };
        int index = 0;
        while (index < names.Length)
        {
            if (string.Equals(text, names[index], StringComparison.OrdinalIgnoreCase))
            {
                return offsets[index];
            }

            index++;
        }

        return int.MinValue;
    }
}
