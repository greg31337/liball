using System;
using System.Text;

namespace ftp;

internal static class FtpHumanListingParser
{
    internal static FtpListing ParseListing(string[]? lines)
    {
        string[] checkedLines = FtpListingParsing.CopyLines(lines);
        string formatName = DetectFormat(checkedLines);
        FtpEntry[] entries = new FtpEntry[checkedLines.Length];
        int index = 0;
        while (index < checkedLines.Length)
        {
            FtpListingLineParseResult parsed = ParseDetectedLine(
                checkedLines[index], formatName);
            entries[index] = parsed.Matched && parsed.Entry != null ?
                parsed.Entry : FtpListingParsing.CreateRawEntry(checkedLines[index]);
            index++;
        }

        FtpListing listing = new FtpListing();
        listing.Entries = entries;
        listing.RawLines = checkedLines;
        listing.MachineReadable = false;
        listing.Complete = true;
        listing.FormatName = formatName;
        return listing;
    }

    internal static string DetectFormat(string[] lines)
    {
        int unix = 0;
        int dos = 0;
        int eplf = 0;
        int vms = 0;
        int os400 = 0;
        int index = 0;
        while (index < lines.Length)
        {
            if (FtpUnixListingParser.ParseLine(lines[index]).Matched)
            {
                unix++;
            }
            if (FtpDosListingParser.ParseLine(lines[index]).Matched)
            {
                dos++;
            }
            if (FtpEplfListingParser.ParseLine(lines[index]).Matched)
            {
                eplf++;
            }
            if (FtpVmsListingParser.ParseLine(lines[index]).Matched)
            {
                vms++;
            }
            if (FtpOs400ListingParser.ParseLine(lines[index]).Matched)
            {
                os400++;
            }
            index++;
        }

        string selected = "Raw";
        int selectedCount = 0;
        if (unix > selectedCount)
        {
            selected = "Unix";
            selectedCount = unix;
        }
        if (dos > selectedCount)
        {
            selected = "DOS/Windows";
            selectedCount = dos;
        }
        if (eplf > selectedCount)
        {
            selected = "EPLF";
            selectedCount = eplf;
        }
        if (vms > selectedCount)
        {
            selected = "VMS";
            selectedCount = vms;
        }
        if (os400 > selectedCount)
        {
            selected = "OS/400";
        }
        return selected;
    }

    internal static FtpListingLineParseResult ParseDetectedLine(
        string line,
        string formatName)
    {
        switch (formatName)
        {
            case "Unix":
                return FtpUnixListingParser.ParseLine(line);
            case "DOS/Windows":
                return FtpDosListingParser.ParseLine(line);
            case "EPLF":
                return FtpEplfListingParser.ParseLine(line);
            case "VMS":
                return FtpVmsListingParser.ParseLine(line);
            case "OS/400":
                return FtpOs400ListingParser.ParseLine(line);
            default:
                return new FtpListingLineParseResult();
        }
    }
}

internal static class FtpUnixListingParser
{
    internal static FtpListingLineParseResult ParseLine(string line)
    {
        FtpListingLineParseResult result = new FtpListingLineParseResult();
        string content = FtpListingParsing.RemoveLeadingWhitespace(line);
        FtpListingColumns columns = FtpListingParsing.SplitColumns(content, 9);
        if (!columns.Valid || !IsMode(columns.Values[0]))
        {
            return result;
        }

        FtpListingNumberResult size = FtpListingParsing.ParseUnsignedNumber(
            columns.Values[4]);
        FtpListingNumberResult day = FtpListingParsing.ParseUnsignedNumber(
            columns.Values[6]);
        int month = FtpListingParsing.ParseMonthName(columns.Values[5]);
        if (!size.Valid || !day.Valid || day.Value > 31 || month == 0)
        {
            return result;
        }

        FtpListingDateTimeResult timestamp = ParseUnixTimestamp(
            columns.Values[7], month, (int)day.Value);
        if (!timestamp.Valid)
        {
            return result;
        }

        string name = columns.Values[8];
        string? linkTarget = null;
        FtpEntryType type = ParseModeType(columns.Values[0][0]);
        if (type == FtpEntryType.SymbolicLink)
        {
            int arrow = name.IndexOf(" -> ", StringComparison.Ordinal);
            if (arrow >= 1)
            {
                linkTarget = name.Substring(arrow + 4);
                name = name.Substring(0, arrow);
            }
        }
        if (name.Length == 0)
        {
            return result;
        }

        FtpEntry entry = new FtpEntry();
        entry.Name = name;
        entry.Type = FtpListingParsing.ApplyPseudoDirectoryType(name, type);
        entry.HasSize = true;
        entry.Size = size.Value;
        entry.Modified = timestamp.Timestamp;
        entry.Owner = columns.Values[2];
        entry.Group = columns.Values[3];
        entry.HasUnixMode = true;
        entry.UnixMode = ParseModeBits(columns.Values[0]);
        entry.LinkTarget = linkTarget;
        entry.Hidden = FtpListingParsing.IsHiddenName(name);
        entry.RawLine = line;
        result.Matched = true;
        result.Entry = entry;
        return result;
    }

    internal static FtpListingDateTimeResult ParseUnixTimestamp(
        string yearOrTime,
        int month,
        int day)
    {
        int colon = yearOrTime.IndexOf(':');
        if (colon >= 0)
        {
            FtpListingClockResult clock = FtpListingParsing.ParseClock(
                yearOrTime, false);
            if (!clock.Valid)
            {
                return new FtpListingDateTimeResult();
            }
            DateTime now = DateTime.UtcNow;
            FtpListingDateTimeResult current = FtpListingParsing.CreateTimestamp(
                now.Year, month, day, clock.Hour, clock.Minute, clock.Second,
                yearOrTime);
            if (current.Valid && current.Timestamp.ValueUtc > now.AddDays(1))
            {
                return FtpListingParsing.CreateTimestamp(
                    now.Year - 1, month, day, clock.Hour, clock.Minute,
                    clock.Second, yearOrTime);
            }
            return current;
        }

        FtpListingNumberResult year = FtpListingParsing.ParseUnsignedNumber(yearOrTime);
        if (!year.Valid || year.Value < 1 || year.Value > 9999)
        {
            return new FtpListingDateTimeResult();
        }
        return FtpListingParsing.CreateTimestamp(
            (int)year.Value, month, day, 0, 0, 0, yearOrTime);
    }

    internal static bool IsMode(string mode)
    {
        if (mode.Length != 10 || "-dlbcps".IndexOf(mode[0]) < 0)
        {
            return false;
        }
        int index = 1;
        while (index < mode.Length)
        {
            int position = (index - 1) % 3;
            char value = mode[index];
            bool valid = position == 0 ? value == 'r' || value == '-' :
                position == 1 ? value == 'w' || value == '-' :
                value == 'x' || value == 's' || value == 'S' ||
                value == 't' || value == 'T' || value == '-';
            if (!valid)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static FtpEntryType ParseModeType(char mode)
    {
        if (mode == 'd')
        {
            return FtpEntryType.Directory;
        }
        if (mode == 'l')
        {
            return FtpEntryType.SymbolicLink;
        }
        if (mode == '-')
        {
            return FtpEntryType.File;
        }
        return FtpEntryType.Unknown;
    }

    internal static int ParseModeBits(string mode)
    {
        int value = 0;
        int index = 1;
        while (index < 10)
        {
            int bit = 8 - (index - 1);
            if (mode[index] != '-' && mode[index] != 'S' && mode[index] != 'T')
            {
                value = value | (1 << bit);
            }
            index++;
        }
        if (mode[3] == 's' || mode[3] == 'S')
        {
            value = value | 2048;
        }
        if (mode[6] == 's' || mode[6] == 'S')
        {
            value = value | 1024;
        }
        if (mode[9] == 't' || mode[9] == 'T')
        {
            value = value | 512;
        }
        return value;
    }
}

internal static class FtpDosListingParser
{
    internal static FtpListingLineParseResult ParseLine(string line)
    {
        FtpListingLineParseResult result = new FtpListingLineParseResult();
        string content = FtpListingParsing.RemoveLeadingWhitespace(line);
        FtpListingColumns columns = FtpListingParsing.SplitColumns(content, 4);
        if (!columns.Valid)
        {
            return result;
        }

        FtpListingDateParts date = FtpListingParsing.ParseNumericDate(
            columns.Values[0], '-', true);
        FtpListingClockResult clock = FtpListingParsing.ParseClock(
            columns.Values[1], true);
        if (!date.Valid || !clock.Valid || columns.Values[3].Length == 0)
        {
            return result;
        }
        FtpListingDateTimeResult timestamp = FtpListingParsing.CreateTimestamp(
            date.Year, date.Month, date.Day, clock.Hour, clock.Minute,
            clock.Second, columns.Values[0] + " " + columns.Values[1]);
        if (!timestamp.Valid)
        {
            return result;
        }

        bool directory = string.Equals(
            columns.Values[2], "<DIR>", StringComparison.OrdinalIgnoreCase);
        FtpListingNumberResult size = FtpListingParsing.ParseUnsignedNumber(
            columns.Values[2]);
        if (!directory && !size.Valid)
        {
            return result;
        }

        FtpEntry entry = new FtpEntry();
        entry.Name = columns.Values[3];
        entry.Type = FtpListingParsing.ApplyPseudoDirectoryType(
            entry.Name, directory ? FtpEntryType.Directory : FtpEntryType.File);
        entry.HasSize = !directory;
        entry.Size = directory ? 0 : size.Value;
        entry.Modified = timestamp.Timestamp;
        entry.Hidden = FtpListingParsing.IsHiddenName(entry.Name);
        entry.RawLine = line;
        result.Matched = true;
        result.Entry = entry;
        return result;
    }
}

internal static class FtpEplfListingParser
{
    internal static FtpListingLineParseResult ParseLine(string line)
    {
        FtpListingLineParseResult result = new FtpListingLineParseResult();
        string content = FtpListingParsing.RemoveLeadingWhitespace(line);
        int tab = content.IndexOf('\t');
        if (content.Length < 3 || content[0] != '+' || tab < 2 ||
            tab == content.Length - 1)
        {
            return result;
        }

        string factsText = content.Substring(1, tab - 1);
        string[] facts = factsText.Split(',');
        FtpEntryType type = FtpEntryType.Unknown;
        FtpListingNumberResult size = new FtpListingNumberResult();
        FtpListingDateTimeResult modified = new FtpListingDateTimeResult();
        string? unique = null;
        int index = 0;
        while (index < facts.Length)
        {
            string fact = facts[index];
            if (fact == "/")
            {
                type = FtpEntryType.Directory;
            }
            else if (fact == "r")
            {
                type = FtpEntryType.File;
            }
            else if (fact.Length > 1 && fact[0] == 's')
            {
                size = FtpListingParsing.ParseUnsignedNumber(fact.Substring(1));
                if (!size.Valid)
                {
                    return result;
                }
            }
            else if (fact.Length > 1 && fact[0] == 'm')
            {
                FtpListingNumberResult seconds = FtpListingParsing.ParseUnsignedNumber(
                    fact.Substring(1));
                modified = FtpListingParsing.CreateUnixTimestamp(seconds, fact.Substring(1));
                if (!modified.Valid)
                {
                    return result;
                }
            }
            else if (fact.Length > 1 && fact[0] == 'i')
            {
                unique = fact.Substring(1);
            }
            else if (fact.Length == 0 && index != facts.Length - 1)
            {
                return result;
            }
            index++;
        }
        if (type == FtpEntryType.Unknown)
        {
            return result;
        }

        string name = content.Substring(tab + 1);
        FtpEntry entry = new FtpEntry();
        entry.Name = name;
        entry.Type = FtpListingParsing.ApplyPseudoDirectoryType(name, type);
        entry.HasSize = size.Valid;
        entry.Size = size.Valid ? size.Value : 0;
        if (modified.Valid)
        {
            entry.Modified = modified.Timestamp;
        }
        entry.Unique = unique;
        entry.Hidden = FtpListingParsing.IsHiddenName(name);
        entry.RawLine = line;
        result.Matched = true;
        result.Entry = entry;
        return result;
    }
}

internal static class FtpVmsListingParser
{
    internal static FtpListingLineParseResult ParseLine(string line)
    {
        FtpListingLineParseResult result = new FtpListingLineParseResult();
        string content = FtpListingParsing.RemoveLeadingWhitespace(line);
        FtpListingColumns columns = FtpListingParsing.SplitColumns(content, 6);
        if (!columns.Valid)
        {
            return result;
        }

        string versionedName = columns.Values[0];
        int semicolon = versionedName.LastIndexOf(';');
        if (semicolon < 1 || semicolon == versionedName.Length - 1 ||
            !FtpListingParsing.ParseUnsignedNumber(
                versionedName.Substring(semicolon + 1)).Valid)
        {
            return result;
        }
        string name = versionedName.Substring(0, semicolon);
        bool directory = name.EndsWith(".DIR", StringComparison.OrdinalIgnoreCase);
        if (directory)
        {
            name = name.Substring(0, name.Length - 4);
        }

        string usedBlocks = columns.Values[1];
        int slash = usedBlocks.IndexOf('/');
        string blockText = slash < 0 ? usedBlocks : usedBlocks.Substring(0, slash);
        if (!FtpListingParsing.ParseUnsignedNumber(blockText).Valid)
        {
            return result;
        }

        FtpListingDateParts date = FtpListingParsing.ParseNamedDate(
            columns.Values[2]);
        FtpListingClockResult clock = FtpListingParsing.ParseClock(
            columns.Values[3], false);
        if (!date.Valid || !clock.Valid || columns.Values[4].Length < 3 ||
            columns.Values[4][0] != '[' ||
            columns.Values[4][columns.Values[4].Length - 1] != ']')
        {
            return result;
        }
        FtpListingDateTimeResult timestamp = FtpListingParsing.CreateTimestamp(
            date.Year, date.Month, date.Day, clock.Hour, clock.Minute,
            clock.Second, columns.Values[2] + " " + columns.Values[3]);
        if (!timestamp.Valid)
        {
            return result;
        }

        string ownership = columns.Values[4].Substring(1, columns.Values[4].Length - 2);
        int comma = ownership.IndexOf(',');
        FtpEntry entry = new FtpEntry();
        entry.Name = name;
        entry.Type = FtpListingParsing.ApplyPseudoDirectoryType(
            name, directory ? FtpEntryType.Directory : FtpEntryType.File);
        entry.Modified = timestamp.Timestamp;
        entry.Owner = comma < 0 ? ownership : ownership.Substring(0, comma);
        entry.Group = comma < 0 ? null : ownership.Substring(comma + 1);
        entry.Hidden = FtpListingParsing.IsHiddenName(name);
        entry.RawLine = line;
        result.Matched = true;
        result.Entry = entry;
        return result;
    }
}

internal static class FtpOs400ListingParser
{
    internal static FtpListingLineParseResult ParseLine(string line)
    {
        FtpListingLineParseResult result = new FtpListingLineParseResult();
        string content = FtpListingParsing.RemoveLeadingWhitespace(line);
        FtpListingColumns columns = FtpListingParsing.SplitColumns(content, 6);
        if (!columns.Valid)
        {
            return result;
        }

        FtpListingNumberResult size = FtpListingParsing.ParseUnsignedNumber(
            columns.Values[1]);
        FtpListingDateParts date = FtpListingParsing.ParseNumericDate(
            columns.Values[2], '/', true);
        FtpListingClockResult clock = FtpListingParsing.ParseClock(
            columns.Values[3], false);
        FtpEntryType type = ParseObjectType(columns.Values[4]);
        if (!size.Valid || !date.Valid || !clock.Valid ||
            type == FtpEntryType.Unknown || columns.Values[5].Length == 0)
        {
            return result;
        }
        FtpListingDateTimeResult timestamp = FtpListingParsing.CreateTimestamp(
            date.Year, date.Month, date.Day, clock.Hour, clock.Minute,
            clock.Second, columns.Values[2] + " " + columns.Values[3]);
        if (!timestamp.Valid)
        {
            return result;
        }

        FtpEntry entry = new FtpEntry();
        entry.Name = columns.Values[5];
        entry.Type = FtpListingParsing.ApplyPseudoDirectoryType(entry.Name, type);
        entry.HasSize = type == FtpEntryType.File;
        entry.Size = entry.HasSize ? size.Value : 0;
        entry.Modified = timestamp.Timestamp;
        entry.Owner = columns.Values[0];
        entry.Hidden = FtpListingParsing.IsHiddenName(entry.Name);
        entry.RawLine = line;
        result.Matched = true;
        result.Entry = entry;
        return result;
    }

    internal static FtpEntryType ParseObjectType(string value)
    {
        if (string.Equals(value, "*DIR", StringComparison.OrdinalIgnoreCase))
        {
            return FtpEntryType.Directory;
        }
        if (string.Equals(value, "*FILE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "*STMF", StringComparison.OrdinalIgnoreCase))
        {
            return FtpEntryType.File;
        }
        if (string.Equals(value, "*SYMLNK", StringComparison.OrdinalIgnoreCase))
        {
            return FtpEntryType.SymbolicLink;
        }
        return FtpEntryType.Unknown;
    }
}

internal static class FtpListingParsing
{
    internal static string[] CopyLines(string[]? lines)
    {
        if (lines == null)
        {
            throw new ArgumentNullException("lines");
        }
        if (lines.Length > FtpDefaults.MaximumListingEntryCount)
        {
            throw new FtpProtocolException(
                "The FTP listing exceeded the configured entry limit.", null);
        }

        string[] copy = new string[lines.Length];
        int index = 0;
        while (index < lines.Length)
        {
            string? line = lines[index];
            if (line == null)
            {
                throw new FtpProtocolException(
                    "An FTP listing line cannot be null.", null);
            }
            if (Encoding.UTF8.GetByteCount(line) > FtpDefaults.MaximumControlLineBytes)
            {
                throw new FtpProtocolException(
                    "An FTP listing entry exceeded the configured line limit.", null);
            }
            int characterIndex = 0;
            while (characterIndex < line.Length)
            {
                char character = line[characterIndex];
                if (character == '\0' || character == '\r' || character == '\n')
                {
                    throw new FtpProtocolException(
                        "An FTP listing line contains an invalid control character.", null);
                }
                characterIndex++;
            }
            copy[index] = line;
            index++;
        }
        return copy;
    }

    internal static FtpListingColumns SplitColumns(string line, int columnCount)
    {
        FtpListingColumns result = new FtpListingColumns();
        if (columnCount < 1)
        {
            return result;
        }
        string[] values = new string[columnCount];
        int position = 0;
        int column = 0;
        while (column < columnCount)
        {
            while (position < line.Length && char.IsWhiteSpace(line[position]))
            {
                position++;
            }
            if (position >= line.Length)
            {
                return result;
            }
            if (column == columnCount - 1)
            {
                values[column] = line.Substring(position);
                position = line.Length;
            }
            else
            {
                int start = position;
                while (position < line.Length && !char.IsWhiteSpace(line[position]))
                {
                    position++;
                }
                values[column] = line.Substring(start, position - start);
            }
            column++;
        }
        result.Valid = true;
        result.Values = values;
        return result;
    }

    internal static FtpListingNumberResult ParseUnsignedNumber(string text)
    {
        FtpListingNumberResult result = new FtpListingNumberResult();
        if (text.Length == 0)
        {
            return result;
        }
        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return result;
            }
            int digit = character - '0';
            if (value > (long.MaxValue - digit) / 10)
            {
                return result;
            }
            value = (value * 10) + digit;
            index++;
        }
        result.Valid = true;
        result.Value = value;
        return result;
    }

    internal static FtpListingClockResult ParseClock(
        string text,
        bool allowMeridiem)
    {
        FtpListingClockResult result = new FtpListingClockResult();
        string clockText = text;
        bool morning = false;
        bool evening = false;
        if (allowMeridiem && text.Length > 2)
        {
            string suffix = text.Substring(text.Length - 2).ToUpperInvariant();
            morning = suffix == "AM";
            evening = suffix == "PM";
            if (morning || evening)
            {
                clockText = text.Substring(0, text.Length - 2);
            }
        }

        string[] fields = clockText.Split(':');
        if (fields.Length < 2 || fields.Length > 3)
        {
            return result;
        }
        FtpListingNumberResult hour = ParseUnsignedNumber(fields[0]);
        FtpListingNumberResult minute = ParseUnsignedNumber(fields[1]);
        FtpListingNumberResult second = fields.Length == 3 ?
            ParseUnsignedNumber(fields[2]) : CreateZeroNumber();
        if (!hour.Valid || !minute.Valid || !second.Valid ||
            minute.Value > 59 || second.Value > 59)
        {
            return result;
        }

        int hourValue = (int)hour.Value;
        if (morning || evening)
        {
            if (hourValue < 1 || hourValue > 12)
            {
                return result;
            }
            if (morning && hourValue == 12)
            {
                hourValue = 0;
            }
            else if (evening && hourValue != 12)
            {
                hourValue = hourValue + 12;
            }
        }
        else if (hourValue > 23)
        {
            return result;
        }

        result.Valid = true;
        result.Hour = hourValue;
        result.Minute = (int)minute.Value;
        result.Second = (int)second.Value;
        return result;
    }

    internal static FtpListingDateParts ParseNumericDate(
        string text,
        char separator,
        bool monthFirst)
    {
        FtpListingDateParts result = new FtpListingDateParts();
        string[] fields = text.Split(separator);
        if (fields.Length != 3)
        {
            return result;
        }
        FtpListingNumberResult first = ParseUnsignedNumber(fields[0]);
        FtpListingNumberResult second = ParseUnsignedNumber(fields[1]);
        FtpListingNumberResult year = ParseUnsignedNumber(fields[2]);
        if (!first.Valid || !second.Valid || !year.Valid || year.Value > 9999)
        {
            return result;
        }
        int yearValue = (int)year.Value;
        if (fields[2].Length == 2)
        {
            yearValue = yearValue < 70 ? yearValue + 2000 : yearValue + 1900;
        }
        result.Year = yearValue;
        result.Month = (int)(monthFirst ? first.Value : second.Value);
        result.Day = (int)(monthFirst ? second.Value : first.Value);
        result.Valid = IsCalendarDate(result.Year, result.Month, result.Day);
        return result;
    }

    internal static FtpListingDateParts ParseNamedDate(string text)
    {
        FtpListingDateParts result = new FtpListingDateParts();
        string[] fields = text.Split('-');
        if (fields.Length != 3)
        {
            return result;
        }
        FtpListingNumberResult day = ParseUnsignedNumber(fields[0]);
        FtpListingNumberResult year = ParseUnsignedNumber(fields[2]);
        int month = ParseMonthName(fields[1]);
        if (!day.Valid || !year.Valid || year.Value > 9999 || month == 0)
        {
            return result;
        }
        result.Year = (int)year.Value;
        result.Month = month;
        result.Day = (int)day.Value;
        result.Valid = IsCalendarDate(result.Year, result.Month, result.Day);
        return result;
    }

    internal static FtpListingDateTimeResult CreateTimestamp(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        string rawValue)
    {
        FtpListingDateTimeResult result = new FtpListingDateTimeResult();
        if (!IsCalendarDate(year, month, day) || hour < 0 || hour > 23 ||
            minute < 0 || minute > 59 || second < 0 || second > 59)
        {
            return result;
        }
        FtpTimestamp timestamp = new FtpTimestamp();
        timestamp.HasValue = true;
        timestamp.ValueUtc = new DateTime(
            year, month, day, hour, minute, second, DateTimeKind.Utc);
        timestamp.RawValue = rawValue;
        result.Valid = true;
        result.Timestamp = timestamp;
        return result;
    }

    internal static FtpListingDateTimeResult CreateUnixTimestamp(
        FtpListingNumberResult seconds,
        string rawValue)
    {
        FtpListingDateTimeResult result = new FtpListingDateTimeResult();
        if (!seconds.Valid || seconds.Value > 253402300799)
        {
            return result;
        }
        FtpTimestamp timestamp = new FtpTimestamp();
        timestamp.HasValue = true;
        timestamp.ValueUtc = DateTime.UnixEpoch.AddSeconds(seconds.Value);
        timestamp.RawValue = rawValue;
        result.Valid = true;
        result.Timestamp = timestamp;
        return result;
    }

    internal static bool IsCalendarDate(int year, int month, int day)
    {
        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
        {
            return false;
        }
        return day <= DateTime.DaysInMonth(year, month);
    }

    internal static int ParseMonthName(string value)
    {
        switch (value.ToUpperInvariant())
        {
            case "JAN":
                return 1;
            case "FEB":
                return 2;
            case "MAR":
                return 3;
            case "APR":
                return 4;
            case "MAY":
                return 5;
            case "JUN":
                return 6;
            case "JUL":
                return 7;
            case "AUG":
                return 8;
            case "SEP":
                return 9;
            case "OCT":
                return 10;
            case "NOV":
                return 11;
            case "DEC":
                return 12;
            default:
                return 0;
        }
    }

    internal static string RemoveLeadingWhitespace(string line)
    {
        int index = 0;
        while (index < line.Length && char.IsWhiteSpace(line[index]))
        {
            index++;
        }
        return index == 0 ? line : line.Substring(index);
    }

    internal static FtpEntryType ApplyPseudoDirectoryType(
        string name,
        FtpEntryType type)
    {
        if (type == FtpEntryType.Directory && name == ".")
        {
            return FtpEntryType.CurrentDirectory;
        }
        if (type == FtpEntryType.Directory && name == "..")
        {
            return FtpEntryType.ParentDirectory;
        }
        return type;
    }

    internal static bool IsHiddenName(string name)
    {
        return name.Length > 0 && name[0] == '.' && name != "." && name != "..";
    }

    internal static FtpEntry CreateRawEntry(string line)
    {
        FtpEntry entry = new FtpEntry();
        entry.Name = line;
        entry.RawLine = line;
        return entry;
    }

    internal static FtpListingNumberResult CreateZeroNumber()
    {
        FtpListingNumberResult result = new FtpListingNumberResult();
        result.Valid = true;
        result.Value = 0;
        return result;
    }
}

internal class FtpListingLineParseResult
{
    internal bool Matched;
    internal FtpEntry? Entry;

    internal FtpListingLineParseResult()
    {
        Matched = false;
        Entry = null;
    }
}

internal class FtpListingColumns
{
    internal bool Valid;
    internal string[] Values;

    internal FtpListingColumns()
    {
        Valid = false;
        Values = new string[0];
    }
}

internal class FtpListingNumberResult
{
    internal bool Valid;
    internal long Value;

    internal FtpListingNumberResult()
    {
        Valid = false;
        Value = 0;
    }
}

internal class FtpListingClockResult
{
    internal bool Valid;
    internal int Hour;
    internal int Minute;
    internal int Second;

    internal FtpListingClockResult()
    {
        Valid = false;
        Hour = 0;
        Minute = 0;
        Second = 0;
    }
}

internal class FtpListingDateParts
{
    internal bool Valid;
    internal int Year;
    internal int Month;
    internal int Day;

    internal FtpListingDateParts()
    {
        Valid = false;
        Year = 0;
        Month = 0;
        Day = 0;
    }
}

internal class FtpListingDateTimeResult
{
    internal bool Valid;
    internal FtpTimestamp Timestamp;

    internal FtpListingDateTimeResult()
    {
        Valid = false;
        Timestamp = new FtpTimestamp();
    }
}
