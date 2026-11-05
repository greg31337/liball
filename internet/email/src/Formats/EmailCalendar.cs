using System;
using System.Collections.Generic;

namespace email;
internal class EmailCalendarSyntax
{
    internal static bool IsName(string? value)
    {
        if (value == null || value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (!(current >= 'A' && current <= 'Z') && !(current >= 'a' && current <= 'z') && !(current >= '0' && current <= '9') && current != '-') return false;
            index++;
        }

        return true;
    }

    internal static string EscapeText(string value)
    {
        string result = value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,");
        result = result.Replace("\r\n", "\\n").Replace("\r", "\\n").Replace("\n", "\\n");
        return result;
    }

    internal static string UnescapeText(string? value)
    {
        System.Text.StringBuilder output = new System.Text.StringBuilder();
        int index = 0;
        while (index < (value ?? throw new InvalidOperationException()).Length)
        {
            if (value[index] == '\\' && index + 1 < value.Length)
            {
                char next = value[index + 1];
                if (next == 'n' || next == 'N') output.Append('\n');
                else output.Append(next);
                index += 2;
            }
            else
            {
                output.Append(value[index]);
                index++;
            }
        }

        return output.ToString();
    }

    internal static bool IsValidDate(int year, int month, int day)
    {
        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1) return false;
        return day <= DateTime.DaysInMonth(year, month);
    }

    internal static string FormatDate(int year, int month, int day)
    {
        if (!IsValidDate(year, month, day)) throw new ArgumentOutOfRangeException("day");
        return year.ToString("D4") + month.ToString("D2") + day.ToString("D2");
    }

    internal static string FormatDateTime(int year, int month, int day, int hour, int minute, int second, bool utc)
    {
        if (!IsValidDate(year, month, day) || hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 60) throw new ArgumentOutOfRangeException("day");
        return FormatDate(year, month, day) + "T" + hour.ToString("D2") + minute.ToString("D2") + second.ToString("D2") + (utc ? "Z" : "");
    }

    internal static List<EmailCalendarProperty> CopyProperties(List<EmailCalendarProperty> source, string parameterName)
    {
        List<EmailCalendarProperty> result = new List<EmailCalendarProperty>();
        int index = 0;
        while (index < source.Count)
        {
            if (source[index] == null) throw new ArgumentException("Calendar property lists cannot contain null entries.", parameterName);
            result.Add(source[index]);
            index++;
        }

        return result;
    }

    internal static List<EmailCalendarComponent> CopyComponents(List<EmailCalendarComponent> source, string parameterName)
    {
        List<EmailCalendarComponent> result = new List<EmailCalendarComponent>();
        int index = 0;
        while (index < source.Count)
        {
            if (source[index] == null) throw new ArgumentException("Calendar component lists cannot contain null entries.", parameterName);
            result.Add(source[index]);
            index++;
        }

        return result;
    }

    internal static List<EmailCalendarProperty> FindProperties(List<EmailCalendarProperty> source, string name)
    {
        if (name == null) throw new ArgumentNullException("name");
        List<EmailCalendarProperty> result = new List<EmailCalendarProperty>();
        int index = 0;
        while (index < source.Count)
        {
            if (string.Equals(source[index].GetName(), name, StringComparison.OrdinalIgnoreCase)) result.Add(source[index]);
            index++;
        }

        return result;
    }
}
internal class EmailCalendarValueParser
{
    internal static string? GetText(EmailCalendarProperty? property)
    {
        return property == null || (property.GetRawValue() ?? throw new InvalidOperationException()).Length == 0 ? null : property.GetTextValue();
    }

    internal static string? GetToken(EmailCalendarProperty? property)
    {
        if (property == null || !EmailCalendarSyntax.IsName(property.GetRawValue())) return null;
        return (property.GetRawValue() ?? throw new InvalidOperationException()).ToUpperInvariant();
    }

    internal static int ParseNonnegativeInteger(EmailCalendarProperty? property)
    {
        if (property == null || (property.GetRawValue() ?? throw new InvalidOperationException()).Length == 0) return -1;
        int value;
        if (!int.TryParse(property.GetRawValue(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out value) || value < 0) return -1;
        return value;
    }

    internal static EmailCalendarDateTime? ParseDate(EmailCalendarProperty? property)
    {
        if (property == null) return null;
        string? value = property.GetRawValue();
        EmailCalendarParameter? valueParameter = property.GetFirstParameter("VALUE");
        bool explicitDate = valueParameter != null && valueParameter.GetValueCount() == 1 && string.Equals(valueParameter.GetValueAt(0), "DATE", StringComparison.OrdinalIgnoreCase);
        bool explicitDateTime = valueParameter != null && valueParameter.GetValueCount() == 1 && string.Equals(valueParameter.GetValueAt(0), "DATE-TIME", StringComparison.OrdinalIgnoreCase);
        if (valueParameter != null && !explicitDate && !explicitDateTime) return null;
        if (explicitDate || valueParameter == null && (value ?? throw new InvalidOperationException()).Length == 8)
        {
            if ((value ?? throw new InvalidOperationException()).Length != 8 || !Digits(value) || property.GetFirstParameter("TZID") != null) return null;
            int year = int.Parse(value.Substring(0, 4), System.Globalization.CultureInfo.InvariantCulture);
            int month = int.Parse(value.Substring(4, 2), System.Globalization.CultureInfo.InvariantCulture);
            int day = int.Parse(value.Substring(6, 2), System.Globalization.CultureInfo.InvariantCulture);
            if (!EmailCalendarSyntax.IsValidDate(year, month, day)) return null;
            return new EmailCalendarDateTimeOperations().CreateWithValueTypeAndYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneIdAndOriginalValue(EmailCalendarValueType.Date, year, month, day, 0, 0, 0, false, null, value);
        }

        bool utc = (value ?? throw new InvalidOperationException()).Length == 16 && value[15] == 'Z';
        if (value.Length != 15 && !utc || value[8] != 'T') return null;
        string digits = value.Substring(0, 8) + value.Substring(9, 6);
        if (!Digits(digits)) return null;
        int parsedYear = int.Parse(value.Substring(0, 4), System.Globalization.CultureInfo.InvariantCulture);
        int parsedMonth = int.Parse(value.Substring(4, 2), System.Globalization.CultureInfo.InvariantCulture);
        int parsedDay = int.Parse(value.Substring(6, 2), System.Globalization.CultureInfo.InvariantCulture);
        int hour = int.Parse(value.Substring(9, 2), System.Globalization.CultureInfo.InvariantCulture);
        int minute = int.Parse(value.Substring(11, 2), System.Globalization.CultureInfo.InvariantCulture);
        int second = int.Parse(value.Substring(13, 2), System.Globalization.CultureInfo.InvariantCulture);
        if (!EmailCalendarSyntax.IsValidDate(parsedYear, parsedMonth, parsedDay) || hour > 23 || minute > 59 || second > 60) return null;
        EmailCalendarParameter? zoneParameter = property.GetFirstParameter("TZID");
        string? zone = zoneParameter != null && zoneParameter.GetValueCount() == 1 ? zoneParameter.GetValueAt(0) : null;
        if (utc && zone != null) return null;
        return new EmailCalendarDateTimeOperations().CreateWithValueTypeAndYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneIdAndOriginalValue(EmailCalendarValueType.DateTime, parsedYear, parsedMonth, parsedDay, hour, minute, second, utc, zone, value);
    }

    internal static EmailCalendarParticipant? ParseParticipant(EmailCalendarProperty? property)
    {
        if (property == null || (property.GetRawValue() ?? throw new InvalidOperationException()).Length == 0) return null;
        string? commonName = ParameterValue(property, "CN");
        string? role = ParameterValue(property, "ROLE");
        string? status = ParameterValue(property, "PARTSTAT");
        string? rsvpValue = ParameterValue(property, "RSVP");
        bool hasRsvp = string.Equals(rsvpValue, "TRUE", StringComparison.OrdinalIgnoreCase) || string.Equals(rsvpValue, "FALSE", StringComparison.OrdinalIgnoreCase);
        bool rsvp = string.Equals(rsvpValue, "TRUE", StringComparison.OrdinalIgnoreCase);
        return new EmailCalendarParticipant(property.GetRawValue(), commonName, role, status, hasRsvp, rsvp, property);
    }

    static string? ParameterValue(EmailCalendarProperty property, string name)
    {
        EmailCalendarParameter? parameter = property.GetFirstParameter(name);
        return parameter == null || parameter.GetValueCount() != 1 ? null : parameter.GetValueAt(0);
    }

    internal static EmailCalendarMethod ParseMethod(string? name)
    {
        if (name == null) return EmailCalendarMethod.None;
        if (string.Equals(name, "PUBLISH", StringComparison.Ordinal)) return EmailCalendarMethod.Publish;
        if (string.Equals(name, "REQUEST", StringComparison.Ordinal)) return EmailCalendarMethod.Request;
        if (string.Equals(name, "REPLY", StringComparison.Ordinal)) return EmailCalendarMethod.Reply;
        if (string.Equals(name, "ADD", StringComparison.Ordinal)) return EmailCalendarMethod.Add;
        if (string.Equals(name, "CANCEL", StringComparison.Ordinal)) return EmailCalendarMethod.Cancel;
        if (string.Equals(name, "REFRESH", StringComparison.Ordinal)) return EmailCalendarMethod.Refresh;
        if (string.Equals(name, "COUNTER", StringComparison.Ordinal)) return EmailCalendarMethod.Counter;
        if (string.Equals(name, "DECLINECOUNTER", StringComparison.Ordinal)) return EmailCalendarMethod.DeclineCounter;
        return EmailCalendarMethod.Extension;
    }

    internal static EmailCalendarStatus ParseStatus(string? name)
    {
        if (name == null) return EmailCalendarStatus.None;
        if (string.Equals(name, "TENTATIVE", StringComparison.Ordinal)) return EmailCalendarStatus.Tentative;
        if (string.Equals(name, "CONFIRMED", StringComparison.Ordinal)) return EmailCalendarStatus.Confirmed;
        if (string.Equals(name, "CANCELLED", StringComparison.Ordinal)) return EmailCalendarStatus.Cancelled;
        if (string.Equals(name, "NEEDS-ACTION", StringComparison.Ordinal)) return EmailCalendarStatus.NeedsAction;
        if (string.Equals(name, "COMPLETED", StringComparison.Ordinal)) return EmailCalendarStatus.Completed;
        if (string.Equals(name, "IN-PROCESS", StringComparison.Ordinal)) return EmailCalendarStatus.InProcess;
        if (string.Equals(name, "DRAFT", StringComparison.Ordinal)) return EmailCalendarStatus.Draft;
        if (string.Equals(name, "FINAL", StringComparison.Ordinal)) return EmailCalendarStatus.Final;
        return EmailCalendarStatus.Extension;
    }

    internal static bool IsKnownComponentProperty(string? name)
    {
        string[] known = new string[] { "UID", "ORGANIZER", "ATTENDEE", "DTSTART", "DTEND", "DUE", "DTSTAMP", "CREATED", "LAST-MODIFIED", "SEQUENCE", "STATUS", "TZID", "SUMMARY", "DESCRIPTION", "LOCATION", "URL", "RRULE", "RDATE", "EXDATE", "RECURRENCE-ID", "DURATION", "PERCENT-COMPLETE", "COMPLETED", "CLASS", "PRIORITY", "TRANSP", "GEO", "FREEBUSY", "TZURL", "TZNAME", "TZOFFSETFROM", "TZOFFSETTO", "ACTION", "TRIGGER", "REPEAT", "ATTACH", "CATEGORIES", "COMMENT", "CONTACT", "RELATED-TO", "RESOURCES", "REQUEST-STATUS" };
        int index = 0;
        while (index < known.Length)
        {
            if (string.Equals(name, known[index], StringComparison.OrdinalIgnoreCase)) return true;
            index++;
        }

        return false;
    }

    static bool Digits(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9') return false;
            index++;
        }

        return true;
    }
}
