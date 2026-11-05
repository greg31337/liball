namespace svg;

/// <summary>Parses SMIL clock values, timing specifiers, and begin/end lists.</summary>
public class SvgTimingParser : ISvgTimingParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgTimingParser()
    {
    }

    /// <summary>Parses a signed SMIL clock value.</summary>
    /// <param name="value">The clock text.</param>
    /// <param name="maxTokenCharacters">The maximum clock token length.</param>
    /// <returns>A finite seconds value or stable failure.</returns>
    public SvgClockParseResult ParseClock(string? value, int maxTokenCharacters)
    {
        if (value == null || maxTokenCharacters < 1) return new SvgTimingParser().ClockFailure(SvgTimingParseError.InvalidArgument, 0);
        return new SvgTimingParser().ParseClockRange(value, 0, value.Length, maxTokenCharacters);
    }

    /// <summary>Parses one offset, sync, event, repeat, access-key, wallclock, or indefinite timing specifier.</summary>
    /// <param name="value">The timing text.</param>
    /// <param name="maxTokenCharacters">The maximum specifier and clock token length.</param>
    /// <returns>A typed timing specifier or stable failure.</returns>
    public SvgTimingParseResult ParseTiming(string? value, int maxTokenCharacters)
    {
        if (value == null || maxTokenCharacters < 1) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidArgument, 0);
        return new SvgTimingParser().ParseTimingRange(value, 0, value.Length, maxTokenCharacters);
    }

    /// <summary>Parses a semicolon-separated SVG begin timing list.</summary>
    /// <param name="value">The begin-list text.</param>
    /// <param name="maxItems">The maximum timing count.</param>
    /// <param name="maxTokenCharacters">The maximum item length.</param>
    /// <returns>A timing list or valid prefix and failure.</returns>
    public SvgTimingListParseResult ParseBeginList(string value, int maxItems, int maxTokenCharacters)
    {
        return new SvgTimingParser().ParseTimingList(value, maxItems, maxTokenCharacters);
    }

    /// <summary>Parses a semicolon-separated SVG end timing list.</summary>
    /// <param name="value">The end-list text.</param>
    /// <param name="maxItems">The maximum timing count.</param>
    /// <param name="maxTokenCharacters">The maximum item length.</param>
    /// <returns>A timing list or valid prefix and failure.</returns>
    public SvgTimingListParseResult ParseEndList(string value, int maxItems, int maxTokenCharacters)
    {
        return new SvgTimingParser().ParseTimingList(value, maxItems, maxTokenCharacters);
    }

    /// <summary>Parses a bounded semicolon-separated timing list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum timing count.</param>
    /// <param name="maxTokenCharacters">The maximum item length.</param>
    /// <returns>A timing list or valid prefix and failure.</returns>
    public SvgTimingListParseResult ParseTimingList(string? value, int maxItems, int maxTokenCharacters)
    {
        System.Collections.Generic.List<SvgTiming> items = new System.Collections.Generic.List<SvgTiming>();
        if (value == null || maxItems < 1 || maxTokenCharacters < 1) return new SvgTimingParser().ListResult(false, SvgTimingParseError.InvalidArgument, items, 0);
        int position = new SvgTimingParser().SkipWhitespace(value, 0, value.Length);
        if (position == value.Length) return new SvgTimingParser().ListResult(false, SvgTimingParseError.ExpectedValue, items, position);
        while (position < value.Length)
        {
            if (items.Count >= maxItems) return new SvgTimingParser().ListResult(false, SvgTimingParseError.ItemLimitExceeded, items, position);
            int end = new SvgTimingParser().FindSemicolon(value, position);
            int trimmedEnd = new SvgTimingParser().TrimEnd(value, position, end);
            if (trimmedEnd == position) return new SvgTimingParser().ListResult(false, SvgTimingParseError.ExpectedValue, items, position);
            SvgTimingParseResult parsed = new SvgTimingParser().ParseTimingRange(value, position, trimmedEnd, maxTokenCharacters);
            if (!parsed.Success) return new SvgTimingParser().ListResult(false, parsed.Error, items, parsed.ErrorOffset);
            items.Add((parsed.Timing ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            if (end == value.Length) return new SvgTimingParser().ListResult(true, SvgTimingParseError.None, items, -1);
            position = new SvgTimingParser().SkipWhitespace(value, end + 1, value.Length);
            if (position == value.Length) return new SvgTimingParser().ListResult(false, SvgTimingParseError.InvalidSeparator, items, position);
        }
        return new SvgTimingParser().ListResult(true, SvgTimingParseError.None, items, -1);
    }
SvgTimingParseResult ParseTimingRange(string source, int rangeStart, int rangeEnd, int maximum)
    {
        int start = new SvgTimingParser().SkipWhitespace(source, rangeStart, rangeEnd);
        int end = new SvgTimingParser().TrimEnd(source, start, rangeEnd);
        if (start == end) return new SvgTimingParser().TimingFailure(SvgTimingParseError.ExpectedValue, start);
        if (end - start > maximum) return new SvgTimingParser().TimingFailure(SvgTimingParseError.TokenTooLong, start + maximum);
        SvgClockParseResult clock = new SvgTimingParser().ParseClockRange(source, start, end, maximum);
        if (clock.Success) return new SvgTimingParser().TimingSuccess(new SvgTiming(SvgTimingKind.Offset, (clock.Clock ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Seconds, "", "", 0, "", "", source, start, end));
        if (new SvgTimingParser().EqualsText(source, start, end, "indefinite")) return new SvgTimingParser().TimingSuccess(new SvgTiming(SvgTimingKind.Indefinite, 0, "", "", 0, "", "", source, start, end));
        if (new SvgTimingParser().StartsWith(source, start, end, "wallclock("))
        {
            if (source[end - 1] != ')' || end <= start + 11) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidWallclock, start);
            string preserved = source.Substring(start + 10, end - start - 11);
            if (preserved.Length == 0) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidWallclock, start + 10);
            return new SvgTimingParser().TimingSuccess(new SvgTiming(SvgTimingKind.Wallclock, 0, "", "", 0, "", preserved, source, start, end));
        }

        SvgTimingOffsetSplit split = new SvgTimingParser().FindOffset(source, start, end, maximum);
        int coreEnd = new SvgTimingParser().TrimEnd(source, start, split.CoreEnd);
        if (coreEnd <= start) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidSpecifier, start);
        if (new SvgTimingParser().EqualsText(source, start, coreEnd, "indefinite")) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidSpecifier, start);
        if (new SvgTimingParser().EndsWith(source, start, coreEnd, ".begin")) return new SvgTimingParser().ParseSync(source, start, coreEnd, end, split.OffsetSeconds, "begin", 6);
        if (new SvgTimingParser().EndsWith(source, start, coreEnd, ".end")) return new SvgTimingParser().ParseSync(source, start, coreEnd, end, split.OffsetSeconds, "end", 4);
        if (new SvgTimingParser().EndsWith(source, start, coreEnd, ")"))
        {
            SvgTimingParseResult repeat = new SvgTimingParser().ParseRepeat(source, start, coreEnd, end, split.OffsetSeconds, maximum);
            if (repeat.Success || repeat.Error == SvgTimingParseError.InvalidRepeat) return repeat;
            SvgTimingParseResult access = new SvgTimingParser().ParseAccessKey(source, start, coreEnd, end, split.OffsetSeconds);
            if (access.Success || access.Error == SvgTimingParseError.InvalidAccessKey) return access;
        }
        return new SvgTimingParser().ParseEvent(source, start, coreEnd, end, split.OffsetSeconds);
    }
SvgTimingParseResult ParseSync(string source, int start, int coreEnd, int end, double offset, string symbol, int suffixLength)
    {
        int idEnd = coreEnd - suffixLength;
        if (idEnd <= start || !new SvgTimingParser().IsIdentifier(source, start, idEnd)) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidIdentifier, start);
        return new SvgTimingParser().TimingSuccess(new SvgTiming(SvgTimingKind.SyncBase, offset, source.Substring(start, idEnd - start), symbol, 0, "", "", source, start, end));
    }
SvgTimingParseResult ParseRepeat(string source, int start, int coreEnd, int end, double offset, int maximum)
    {
        string core = source.Substring(start, coreEnd - start);
        int marker = core.LastIndexOf("repeat(", System.StringComparison.Ordinal);
        if (marker < 0) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidSpecifier, start);
        if (marker > 0 && core[marker - 1] != '.') return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidRepeat, start + marker);
        int numberStart = marker + 7;
        int numberLength = core.Length - numberStart - 1;
        if (numberLength < 1) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidRepeat, start + numberStart);
        SvgIntegerParseResult iteration = SvgNumberParser.ParseInteger(core.Substring(numberStart, numberLength), maximum);
        if (!iteration.Success || iteration.Value < 1) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidRepeat, start + numberStart + (iteration.ErrorOffset < 0 ? 0 : iteration.ErrorOffset));
        string baseId = marker == 0 ? "" : core.Substring(0, marker - 1);
        if (baseId.Length > 0 && !new SvgTimingParser().IsIdentifier(baseId, 0, baseId.Length)) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidIdentifier, start);
        return new SvgTimingParser().TimingSuccess(new SvgTiming(SvgTimingKind.Repeat, offset, baseId, "repeat", iteration.Value, "", "", source, start, end));
    }
SvgTimingParseResult ParseAccessKey(string source, int start, int coreEnd, int end, double offset)
    {
        if (!new SvgTimingParser().StartsWith(source, start, coreEnd, "accessKey(")) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidSpecifier, start);
        string key = source.Substring(start + 10, coreEnd - start - 11);
        if (!new SvgTimingParser().IsOneScalar(key)) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidAccessKey, start + 10);
        return new SvgTimingParser().TimingSuccess(new SvgTiming(SvgTimingKind.AccessKey, offset, "", "accessKey", 0, key, "", source, start, end));
    }
SvgTimingParseResult ParseEvent(string source, int start, int coreEnd, int end, double offset)
    {
        int dot = new SvgTimingParser().LastIndexOf(source, '.', start, coreEnd);
        string baseId = "";
        int symbolStart = start;
        if (dot >= start)
        {
            if (dot == start || dot + 1 == coreEnd || !new SvgTimingParser().IsIdentifier(source, start, dot)) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidIdentifier, start);
            baseId = source.Substring(start, dot - start);
            symbolStart = dot + 1;
        }
        if (!new SvgTimingParser().IsIdentifier(source, symbolStart, coreEnd)) return new SvgTimingParser().TimingFailure(SvgTimingParseError.InvalidIdentifier, symbolStart);
        string symbol = source.Substring(symbolStart, coreEnd - symbolStart);
        return new SvgTimingParser().TimingSuccess(new SvgTiming(SvgTimingKind.EventBase, offset, baseId, symbol, 0, "", "", source, start, end));
    }
SvgTimingOffsetSplit FindOffset(string source, int start, int end, int maximum)
    {
        int position = end - 1;
        while (position > start)
        {
            char current = source[position];
            if (current == '+' || current == '-')
            {
                SvgClockParseResult parsed = new SvgTimingParser().ParseClockRange(source, position, end, maximum);
                if (parsed.Success) return new SvgTimingOffsetSplit(position, (parsed.Clock ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Seconds);
            }
            position = position - 1;
        }
        return new SvgTimingOffsetSplit(end, 0);
    }
SvgClockParseResult ParseClockRange(string source, int rangeStart, int rangeEnd, int maximum)
    {
        int start = new SvgTimingParser().SkipWhitespace(source, rangeStart, rangeEnd);
        int end = new SvgTimingParser().TrimEnd(source, start, rangeEnd);
        if (start == end) return new SvgTimingParser().ClockFailure(SvgTimingParseError.ExpectedValue, start);
        if (end - start > maximum) return new SvgTimingParser().ClockFailure(SvgTimingParseError.TokenTooLong, start + maximum);
        int position = start;
        double sign = 1;
        if (source[position] == '+' || source[position] == '-')
        {
            if (source[position] == '-') sign = -1;
            position = new SvgTimingParser().SkipWhitespace(source, position + 1, end);
            if (position == end) return new SvgTimingParser().ClockFailure(SvgTimingParseError.InvalidClock, position);
        }
        int firstColon = new SvgTimingParser().IndexOf(source, ':', position, end);
        double seconds;
        if (firstColon >= 0)
        {
            int secondColon = new SvgTimingParser().IndexOf(source, ':', firstColon + 1, end);
            if (secondColon >= 0 && new SvgTimingParser().IndexOf(source, ':', secondColon + 1, end) >= 0) return new SvgTimingParser().ClockFailure(SvgTimingParseError.InvalidClock, secondColon + 1);
            if (secondColon < 0)
            {
                if (!new SvgTimingParser().IsTwoDigits(source, position, firstColon)) return new SvgTimingParser().ClockFailure(SvgTimingParseError.InvalidClock, position);
                int minutes = new SvgTimingParser().TwoDigits(source, position);
                double secondValue = new SvgTimingParser().ReadSecondComponent(source, firstColon + 1, end, maximum);
                if (minutes > 59 || secondValue < 0 || secondValue >= 60) return new SvgTimingParser().ClockFailure(SvgTimingParseError.InvalidClock, position);
                seconds = minutes * 60 + secondValue;
            }
            else
            {
                if (!new SvgTimingParser().IsDigits(source, position, firstColon) || !new SvgTimingParser().IsTwoDigits(source, firstColon + 1, secondColon)) return new SvgTimingParser().ClockFailure(SvgTimingParseError.InvalidClock, position);
                SvgNumberParseResult hours = SvgNumberParser.ParseNumber(source.Substring(position, firstColon - position), maximum);
                int minutes = new SvgTimingParser().TwoDigits(source, firstColon + 1);
                double secondValue = new SvgTimingParser().ReadSecondComponent(source, secondColon + 1, end, maximum);
                if (!hours.Success || minutes > 59 || secondValue < 0 || secondValue >= 60) return new SvgTimingParser().ClockFailure(SvgTimingParseError.InvalidClock, position);
                seconds = hours.Value * 3600 + minutes * 60 + secondValue;
            }
        }
        else
        {
            int unitStart = position;
            while (unitStart < end && (source[unitStart] >= '0' && source[unitStart] <= '9' || source[unitStart] == '.')) unitStart = unitStart + 1;
            if (unitStart == position || !new SvgTimingParser().IsDecimal(source, position, unitStart)) return new SvgTimingParser().ClockFailure(SvgTimingParseError.InvalidClock, position);
            string unit = source.Substring(unitStart, end - unitStart);
            if (unit != "" && unit != "h" && unit != "min" && unit != "s" && unit != "ms") return new SvgTimingParser().ClockFailure(SvgTimingParseError.InvalidClock, unitStart);
            SvgNumberParseResult number = SvgNumberParser.ParseNumber(source.Substring(position, unitStart - position), maximum);
            if (!number.Success) return new SvgTimingParser().ClockFailure(number.Error == SvgNumberParseError.TokenTooLong ? SvgTimingParseError.TokenTooLong : SvgTimingParseError.InvalidClock, position + number.ErrorOffset);
            seconds = number.Value;
            if (unit == "h") seconds = seconds * 3600;
            else if (unit == "min") seconds = seconds * 60;
            else if (unit == "ms") seconds = seconds / 1000;
        }
        seconds = seconds * sign;
        if (!SvgFiniteMath.IsFinite(seconds)) return new SvgTimingParser().ClockFailure(SvgTimingParseError.OutOfRange, start);
        return new SvgClockParseResult(true, SvgTimingParseError.None, new SvgClockValue(seconds, source, start, end), -1);
    }
double ReadSecondComponent(string source, int start, int end, int maximum)
    {
        if (end - start < 2 || source[start] < '0' || source[start] > '9' || source[start + 1] < '0' || source[start + 1] > '9') return -1;
        if (end - start > 2)
        {
            if (source[start + 2] != '.' || end - start == 3 || !new SvgTimingParser().IsDigits(source, start + 3, end)) return -1;
        }
        SvgNumberParseResult parsed = SvgNumberParser.ParseNumber(source.Substring(start, end - start), maximum);
        return parsed.Success ? parsed.Value : -1;
    }
bool IsDecimal(string source, int start, int end)
    {
        int point = -1;
        int position = start;
        while (position < end)
        {
            char current = source[position];
            if (current == '.')
            {
                if (point >= 0 || position == start || position + 1 == end) return false;
                point = position;
            }
            else if (current < '0' || current > '9') return false;
            position = position + 1;
        }
        return true;
    }
bool IsDigits(string source, int start, int end)
    {
        if (start == end) return false;
        int position = start;
        while (position < end)
        {
            if (source[position] < '0' || source[position] > '9') return false;
            position = position + 1;
        }
        return true;
    }
bool IsTwoDigits(string source, int start, int end)
    {
        return end - start == 2 && new SvgTimingParser().IsDigits(source, start, end);
    }
int TwoDigits(string source, int start)
    {
        return (source[start] - '0') * 10 + source[start + 1] - '0';
    }
bool IsIdentifier(string source, int start, int end)
    {
        if (start >= end || !new SvgTimingParser().IsNameStart(source[start])) return false;
        int position = start + 1;
        while (position < end)
        {
            if (!new SvgTimingParser().IsNameCharacter(source[position])) return false;
            position = position + 1;
        }
        return true;
    }
bool IsNameStart(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value == '_' || value >= 128;
    }
bool IsNameCharacter(char value)
    {
        return new SvgTimingParser().IsNameStart(value) || value >= '0' && value <= '9' || value == '-' || value == '.' || value == ':';
    }
bool IsOneScalar(string value)
    {
        if (value.Length == 1) return !char.IsSurrogate(value[0]);
        return value.Length == 2 && char.IsHighSurrogate(value[0]) && char.IsLowSurrogate(value[1]);
    }
int FindSemicolon(string source, int start)
    {
        int depth = 0;
        int position = start;
        while (position < source.Length)
        {
            if (source[position] == '(') depth = depth + 1;
            else if (source[position] == ')' && depth > 0) depth = depth - 1;
            else if (source[position] == ';' && depth == 0) return position;
            position = position + 1;
        }
        return source.Length;
    }
int SkipWhitespace(string source, int position, int end)
    {
        while (position < end && SvgSpanCharacters.IsSvgWhitespace(source[position])) position = position + 1;
        return position;
    }
int TrimEnd(string source, int start, int end)
    {
        while (end > start && SvgSpanCharacters.IsSvgWhitespace(source[end - 1])) end = end - 1;
        return end;
    }
int IndexOf(string source, char value, int start, int end)
    {
        int position = start;
        while (position < end)
        {
            if (source[position] == value) return position;
            position = position + 1;
        }
        return -1;
    }
int LastIndexOf(string source, char value, int start, int end)
    {
        int position = end - 1;
        while (position >= start)
        {
            if (source[position] == value) return position;
            position = position - 1;
        }
        return -1;
    }
bool StartsWith(string source, int start, int end, string text)
    {
        if (end - start < text.Length) return false;
        int index = 0;
        while (index < text.Length)
        {
            if (source[start + index] != text[index]) return false;
            index = index + 1;
        }
        return true;
    }
bool EndsWith(string source, int start, int end, string text)
    {
        if (end - start < text.Length) return false;
        return new SvgTimingParser().EqualsText(source, end - text.Length, end, text);
    }
bool EqualsText(string source, int start, int end, string text)
    {
        if (end - start != text.Length) return false;
        int index = 0;
        while (index < text.Length)
        {
            if (source[start + index] != text[index]) return false;
            index = index + 1;
        }
        return true;
    }
SvgClockParseResult ClockFailure(SvgTimingParseError error, int offset)
    {
        return new SvgClockParseResult(false, error, null, offset);
    }
SvgTimingParseResult TimingSuccess(SvgTiming timing)
    {
        return new SvgTimingParseResult(true, SvgTimingParseError.None, timing, -1);
    }
SvgTimingParseResult TimingFailure(SvgTimingParseError error, int offset)
    {
        return new SvgTimingParseResult(false, error, null, offset);
    }
SvgTimingListParseResult ListResult(bool success, SvgTimingParseError error, System.Collections.Generic.List<SvgTiming> items, int offset)
    {
        return new SvgTimingListParseResult(success, error, items, offset);
    }
}
