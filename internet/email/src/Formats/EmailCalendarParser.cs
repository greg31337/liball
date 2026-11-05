using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailCalendarParserWorker
{
    string input;
    EmailCalendarParseOptions options;
    EmailIssueList issues;
    List<EmailCalendarLogicalLine> lines;
    int propertyCount;
    int componentCount;

    internal EmailCalendarParserWorker(string input, EmailCalendarParseOptions options, EmailIssueList issues)
    {
        this.input = input;
        this.options = options;
        this.issues = issues;
        lines = new List<EmailCalendarLogicalLine>();
        propertyCount = 0;
        componentCount = 0;
    }

    internal EmailCalendarParseResult Parse()
    {
        UnfoldLines();
        EmailCalendar? calendar = ParseStructure();
        if (calendar != null) ValidateCalendar(calendar);
        return new EmailCalendarParseResult(calendar, issues, null, false, true);
    }

    void UnfoldLines()
    {
        int start = 0;
        int lineNumber = 1;
        int physicalCount = 0;
        long byteOffset = 0;
        bool noncanonicalReported = false;
        while (start < input.Length)
        {
            if (physicalCount >= options.GetMaximumContentLines()) throw new EmailLimitException("MaxCalendarContentLines", options.GetMaximumContentLines(), physicalCount + 1);
            int end = start;
            while (end < input.Length && input[end] != '\r' && input[end] != '\n') end++;
            int endingLength = 0;
            bool canonical = false;
            if (end < input.Length)
            {
                if (input[end] == '\r' && end + 1 < input.Length && input[end + 1] == '\n')
                {
                    endingLength = 2;
                    canonical = true;
                }
                else endingLength = 1;
            }

            string physical = input.Substring(start, end - start);
            int physicalBytes = Encoding.UTF8.GetByteCount(physical);
            if (physicalBytes > options.GetMaximumPhysicalLineBytes()) throw new EmailLimitException("MaxCalendarPhysicalLineBytes", options.GetMaximumPhysicalLineBytes(), physicalBytes);
            if (!canonical && !noncanonicalReported)
            {
                issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarNonCanonicalLineEnding, "The iCalendar object uses a non-CRLF or missing line ending.", null, "Content lines were accepted and will be generated with CRLF."));
                noncanonicalReported = true;
            }

            EmailSourcePosition position = new EmailSourcePosition(byteOffset, lineNumber, 1);
            if (physical.Length > 0 && (physical[0] == ' ' || physical[0] == '\t'))
            {
                if (lines.Count == 0)
                {
                    issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarMalformed, "A folded continuation appears before any content line.", null, "The orphan continuation was ignored."));
                }
                else
                {
                    EmailCalendarLogicalLine previous = lines[lines.Count - 1];
                    previous.Append(physical.Substring(1), options.GetMaximumUnfoldedLineBytes());
                }
            }
            else
            {
                if (lines.Count >= options.GetMaximumContentLines()) throw new EmailLimitException("MaxCalendarContentLines", options.GetMaximumContentLines(), lines.Count + 1);
                lines.Add(new EmailCalendarLogicalLine(physical, position, options.GetMaximumUnfoldedLineBytes()));
            }

            physicalCount++;
            byteOffset += physicalBytes + endingLength;
            start = end + endingLength;
            lineNumber++;
            if (endingLength == 0) break;
        }

        if (input.Length == 0) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarMalformed, "The iCalendar object is empty.", null, "No calendar was produced."));
    }

    EmailCalendar? ParseStructure()
    {
        List<EmailCalendarComponentBuilder> stack = new List<EmailCalendarComponentBuilder>();
        EmailCalendar? calendar = null;
        int index = 0;
        while (index < lines.Count)
        {
            EmailCalendarProperty? property = ParseProperty(lines[index]);
            if (property != null)
            {
                string? name = property.GetName();
                if (string.Equals(name, "BEGIN", StringComparison.OrdinalIgnoreCase))
                {
                    string componentName = (property.GetRawValue() ?? throw new InvalidOperationException()).ToUpperInvariant();
                    if (!EmailCalendarSyntax.IsName(componentName)) issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarComponentMismatch, "BEGIN has an invalid component name.", "BEGIN", "The invalid component boundary was ignored."));
                    else if (stack.Count == 0)
                    {
                        if (!string.Equals(componentName, "VCALENDAR", StringComparison.Ordinal)) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarComponentMismatch, "The outer iCalendar component must be VCALENDAR.", "BEGIN", "No calendar root was inferred."));
                        else stack.Add(new EmailCalendarComponentBuilder(componentName));
                    }
                    else
                    {
                        if (stack.Count >= options.GetMaximumComponentDepth()) throw new EmailLimitException("MaxCalendarComponentDepth", options.GetMaximumComponentDepth(), stack.Count + 1);
                        if (componentCount >= options.GetMaximumComponents()) throw new EmailLimitException("MaxCalendarComponents", options.GetMaximumComponents(), componentCount + 1);
                        componentCount++;
                        stack.Add(new EmailCalendarComponentBuilder(componentName));
                    }
                }
                else if (string.Equals(name, "END", StringComparison.OrdinalIgnoreCase))
                {
                    string componentName = (property.GetRawValue() ?? throw new InvalidOperationException()).ToUpperInvariant();
                    if (stack.Count == 0)
                    {
                        issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarComponentMismatch, "END appears without a matching BEGIN.", "END", "The unmatched component boundary was ignored."));
                    }
                    else
                    {
                        EmailCalendarComponentBuilder current = stack[stack.Count - 1];
                        if (!string.Equals(current.GetName(), componentName, StringComparison.Ordinal)) issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarComponentMismatch, "END does not match the currently open component.", "END", "The current component was closed to retain its parsed properties."));
                        stack.RemoveAt(stack.Count - 1);
                        if (string.Equals(current.GetName(), "VCALENDAR", StringComparison.Ordinal))
                        {
                            if (calendar != null) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarMalformed, "More than one VCALENDAR object is present.", "BEGIN", "Only the first complete calendar was retained."));
                            else calendar = new EmailCalendar(current.GetProperties(), current.GetComponents());
                        }
                        else if (stack.Count == 0)
                        {
                            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarComponentMismatch, "A calendar component was closed outside VCALENDAR.", "END", "The detached component was discarded."));
                        }
                        else stack[stack.Count - 1].AddComponent(current.Build());
                    }
                }
                else if (stack.Count == 0)
                {
                    if ((name ?? throw new InvalidOperationException()).Length > 0) issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarMalformed, "A property appears outside VCALENDAR.", name, "The out-of-component property was ignored."));
                }
                else
                {
                    stack[stack.Count - 1].AddProperty(property);
                }
            }

            index++;
        }

        if (stack.Count > 0)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarComponentMismatch, "One or more calendar components have no matching END.", "END", "Open components were closed at end of input and retained."));
            while (stack.Count > 1)
            {
                EmailCalendarComponentBuilder child = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                stack[stack.Count - 1].AddComponent(child.Build());
            }

            if (string.Equals(stack[0].GetName(), "VCALENDAR", StringComparison.Ordinal)) calendar = new EmailCalendar(stack[0].GetProperties(), stack[0].GetComponents());
        }

        if (calendar == null) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarMalformed, "No VCALENDAR object was found.", "BEGIN", "No calendar was produced."));
        return calendar;
    }

    EmailCalendarProperty? ParseProperty(EmailCalendarLogicalLine line)
    {
        string value = line.GetValue();
        if (value.Length == 0) return null;
        int colon = FindDelimiter(value, ':');
        if (colon <= 0)
        {
            issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarMalformed, "A content line has no property/value colon.", null, "The malformed content line was ignored."));
            return null;
        }

        string prefix = value.Substring(0, colon);
        List<string> segments = SplitOutsideQuotes(prefix, ';');
        string name = segments[0];
        if (!EmailCalendarSyntax.IsName(name))
        {
            issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarPropertyInvalid, "A content line has an invalid property name.", name, "The malformed content line was ignored."));
            return null;
        }

        List<EmailCalendarParameter> parameters = new List<EmailCalendarParameter>();
        int index = 1;
        while (index < segments.Count)
        {
            if (parameters.Count >= options.GetMaximumParametersPerProperty()) throw new EmailLimitException("MaxCalendarParametersPerProperty", options.GetMaximumParametersPerProperty(), parameters.Count + 1);
            EmailCalendarParameter? parameter = ParseParameter(segments[index], name);
            if (parameter != null) parameters.Add(parameter);
            index++;
        }

        if (propertyCount >= options.GetMaximumProperties()) throw new EmailLimitException("MaxCalendarProperties", options.GetMaximumProperties(), propertyCount + 1);
        propertyCount++;
        return new EmailCalendarPropertyOperations().CreateWithNameAndParametersAndRawValueAndSourcePosition(name, parameters, value.Substring(colon + 1), line.GetSourcePosition());
    }

    EmailCalendarParameter? ParseParameter(string segment, string propertyName)
    {
        int equals = FindDelimiter(segment, '=');
        if (equals <= 0 || equals == segment.Length - 1)
        {
            issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarPropertyInvalid, "A calendar property parameter is malformed.", propertyName, "The malformed parameter was omitted while the property value was retained."));
            return null;
        }

        string name = segment.Substring(0, equals);
        if (!EmailCalendarSyntax.IsName(name))
        {
            issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarPropertyInvalid, "A calendar property parameter has an invalid name.", propertyName, "The malformed parameter was omitted."));
            return null;
        }

        List<string> rawValues = SplitOutsideQuotes(segment.Substring(equals + 1), ',');
        if (rawValues.Count > options.GetMaximumParameterValues()) throw new EmailLimitException("MaxCalendarParameterValues", options.GetMaximumParameterValues(), rawValues.Count);
        List<string> values = new List<string>();
        int index = 0;
        while (index < rawValues.Count)
        {
            string raw = rawValues[index];
            if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"') raw = raw.Substring(1, raw.Length - 2);
            else if (raw.IndexOf('"') >= 0)
            {
                issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarPropertyInvalid, "A calendar parameter contains an unmatched quote.", propertyName, "The quote characters were retained in the parameter value."));
                raw = raw.Replace("\"", "");
            }

            if (raw.Length == 0)
            {
                issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarPropertyInvalid, "A calendar parameter contains an empty value.", propertyName, "The empty parameter value was omitted."));
            }
            else values.Add(raw);
            index++;
        }

        if (values.Count == 0) return null;
        return new EmailCalendarParameterOperations().CreateWithNameAndValues(name, values);
    }

    void ValidateCalendar(EmailCalendar calendar)
    {
        ValidateRequiredSingleton(calendar.GetPropertiesNamed("VERSION"), "VERSION", true);
        if (calendar.GetPropertiesNamed("VERSION").Count == 1 && !string.Equals(calendar.GetVersion(), "2.0", StringComparison.Ordinal)) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "VERSION must be 2.0.", "VERSION", "The unsupported version value was retained."));
        ValidateRequiredSingleton(calendar.GetPropertiesNamed("PRODID"), "PRODID", true);
        ValidateRequiredSingleton(calendar.GetPropertiesNamed("METHOD"), "METHOD", false);
        if (calendar.GetPropertiesNamed("METHOD").Count == 1 && calendar.GetMethodName() == null) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "METHOD must be a nonempty iCalendar token.", "METHOD", "The original method value was retained."));
        List<string> zones = new List<string>();
        List<EmailCalendarComponent> timeZones = calendar.GetTimeZones();
        int index = 0;
        while (index < timeZones.Count)
        {
            ValidateComponent(timeZones[index], zones);
            string? zone = timeZones[index].GetTimeZoneId();
            if (zone != null) zones.Add(zone);
            index++;
        }

        List<EmailCalendarComponent> components = calendar.GetComponents();
        index = 0;
        while (index < components.Count)
        {
            if (!string.Equals(components[index].GetName(), "VTIMEZONE", StringComparison.Ordinal)) ValidateComponent(components[index], zones);
            index++;
        }
    }

    void ValidateComponent(EmailCalendarComponent component, List<string> zones)
    {
        string name = component.GetName();
        if (string.Equals(name, "VEVENT", StringComparison.Ordinal) || string.Equals(name, "VTODO", StringComparison.Ordinal) || string.Equals(name, "VJOURNAL", StringComparison.Ordinal) || string.Equals(name, "VFREEBUSY", StringComparison.Ordinal))
        {
            ValidateRequiredSingleton(component.GetPropertiesNamed("UID"), "UID", true);
            ValidateRequiredSingleton(component.GetPropertiesNamed("DTSTAMP"), "DTSTAMP", true);
        }

        if (string.Equals(name, "VTIMEZONE", StringComparison.Ordinal)) ValidateRequiredSingleton(component.GetPropertiesNamed("TZID"), "TZID", true);
        ValidateRequiredSingleton(component.GetPropertiesNamed("ORGANIZER"), "ORGANIZER", false);
        ValidateRequiredSingleton(component.GetPropertiesNamed("DTSTART"), "DTSTART", false);
        ValidateRequiredSingleton(component.GetPropertiesNamed("DTEND"), "DTEND", false);
        ValidateRequiredSingleton(component.GetPropertiesNamed("DUE"), "DUE", false);
        ValidateRequiredSingleton(component.GetPropertiesNamed("DTSTAMP"), "DTSTAMP", false);
        ValidateRequiredSingleton(component.GetPropertiesNamed("SEQUENCE"), "SEQUENCE", false);
        ValidateRequiredSingleton(component.GetPropertiesNamed("STATUS"), "STATUS", false);
        ValidateDates(component, zones);
        if (component.GetPropertiesNamed("UID").Count == 1 && !component.HasUid()) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "UID must contain a nonempty text value.", "UID", "The empty UID property was retained."));
        List<EmailCalendarProperty> organizer = component.GetPropertiesNamed("ORGANIZER");
        if (organizer.Count == 1 && !IsValidParticipant(organizer[0])) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "ORGANIZER has an invalid calendar address or parameter value.", "ORGANIZER", "The property was retained."));
        List<EmailCalendarProperty> attendees = component.GetPropertiesNamed("ATTENDEE");
        int index = 0;
        while (index < attendees.Count)
        {
            if (!IsValidParticipant(attendees[index])) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "ATTENDEE has an invalid calendar address or parameter value.", "ATTENDEE", "The property was retained."));
            index++;
        }

        if (component.GetPropertiesNamed("SEQUENCE").Count == 1 && !component.HasSequence()) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "SEQUENCE must be a nonnegative integer.", "SEQUENCE", "The original sequence value was retained."));
        if (component.GetPropertiesNamed("STATUS").Count == 1 && component.GetStatusName() == null) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "STATUS must be an iCalendar token.", "STATUS", "The original status value was retained."));
        else if (component.GetPropertiesNamed("STATUS").Count == 1 && !IsAllowedStatus(name, component.GetStatus())) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "STATUS is not allowed for the containing calendar component.", "STATUS", "The original status value was retained."));
        List<EmailCalendarComponent> children = component.GetComponents();
        index = 0;
        while (index < children.Count)
        {
            ValidateComponent(children[index], zones);
            index++;
        }
    }

    void ValidateDates(EmailCalendarComponent component, List<string> zones)
    {
        string[] names = new string[] { "DTSTART", "DTEND", "DUE", "DTSTAMP", "CREATED", "LAST-MODIFIED", "RECURRENCE-ID" };
        int index = 0;
        while (index < names.Length)
        {
            List<EmailCalendarProperty> properties = component.GetPropertiesNamed(names[index]);
            int propertyIndex = 0;
            while (propertyIndex < properties.Count)
            {
                EmailCalendarDateTime? value = EmailCalendarValueParser.ParseDate(properties[propertyIndex]);
                if (value == null) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarDateInvalid, names[index] + " is not a valid iCalendar DATE or DATE-TIME.", names[index], "The original date value was retained."));
                else if ((string.Equals(names[index], "DTSTAMP", StringComparison.Ordinal) || string.Equals(names[index], "CREATED", StringComparison.Ordinal) || string.Equals(names[index], "LAST-MODIFIED", StringComparison.Ordinal)) && (value.GetValueType() != EmailCalendarValueType.DateTime || !value.IsUtc())) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarDateInvalid, names[index] + " must use UTC DATE-TIME form.", names[index], "The non-UTC value was retained."));
                else if (value.HasTimeZoneId() && !Contains(zones, value.GetTimeZoneId())) issues.Add(Issue(RecoverableSeverity(), EmailIssueCode.CalendarTimeZoneInvalid, names[index] + " references a TZID without a matching VTIMEZONE component.", names[index], "The unresolved TZID and local date-time were retained."));
                propertyIndex++;
            }

            index++;
        }
    }

    static bool IsValidParticipant(EmailCalendarProperty property)
    {
        EmailCalendarParticipant? participant = EmailCalendarValueParser.ParseParticipant(property);
        if (participant == null) return false;
        Uri? address;
        if (!Uri.TryCreate(participant.GetCalendarAddress(), UriKind.Absolute, out address)) return false;
        EmailCalendarParameter? rsvp = property.GetFirstParameter("RSVP");
        if (rsvp != null && !participant.HasRsvp()) return false;
        return true;
    }

    static bool IsAllowedStatus(string componentName, EmailCalendarStatus status)
    {
        if (string.Equals(componentName, "VEVENT", StringComparison.Ordinal)) return status == EmailCalendarStatus.Tentative || status == EmailCalendarStatus.Confirmed || status == EmailCalendarStatus.Cancelled;
        if (string.Equals(componentName, "VTODO", StringComparison.Ordinal)) return status == EmailCalendarStatus.NeedsAction || status == EmailCalendarStatus.Completed || status == EmailCalendarStatus.InProcess || status == EmailCalendarStatus.Cancelled;
        if (string.Equals(componentName, "VJOURNAL", StringComparison.Ordinal)) return status == EmailCalendarStatus.Draft || status == EmailCalendarStatus.Final || status == EmailCalendarStatus.Cancelled;
        return false;
    }

    void ValidateRequiredSingleton(List<EmailCalendarProperty> values, string name, bool required)
    {
        if (required && values.Count == 0) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarRequiredPropertyMissing, "A required " + name + " property is missing.", name, "The remaining calendar data was retained."));
        if (values.Count > 1) issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyDuplicate, name + " occurs more than once where at most one value is allowed.", name, "All duplicate properties were retained in source order."));
    }

    EmailIssueSeverity RecoverableSeverity()
    {
        return options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
    }

    static bool Contains(List<string> values, string? value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal)) return true;
            index++;
        }

        return false;
    }

    static int FindDelimiter(string value, char delimiter)
    {
        bool quoted = false;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '"') quoted = !quoted;
            else if (value[index] == delimiter && !quoted) return index;
            index++;
        }

        return -1;
    }

    static List<string> SplitOutsideQuotes(string value, char delimiter)
    {
        List<string> result = new List<string>();
        bool quoted = false;
        int start = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '"') quoted = !quoted;
            else if (value[index] == delimiter && !quoted)
            {
                result.Add(value.Substring(start, index - start));
                start = index + 1;
            }

            index++;
        }

        result.Add(value.Substring(start));
        return result;
    }

    static EmailIssue Issue(EmailIssueSeverity severity, EmailIssueCode code, string description, string? fieldName, string recovery)
    {
        return new EmailCalendarParser().Issue(severity, code, description, fieldName, recovery);
    }
}
internal class EmailCalendarLogicalLine
{
    StringBuilder value;
    EmailSourcePosition sourcePosition;
    int byteCount;

    internal EmailCalendarLogicalLine(string value, EmailSourcePosition sourcePosition, int maximumBytes)
    {
        this.value = new StringBuilder(value);
        this.sourcePosition = sourcePosition;
        byteCount = Encoding.UTF8.GetByteCount(value);
        Check(maximumBytes);
    }

    internal void Append(string continuation, int maximumBytes)
    {
        value.Append(continuation);
        byteCount = (int)new EmailSafeArithmetic().AddNonNegative(byteCount, Encoding.UTF8.GetByteCount(continuation));
        Check(maximumBytes);
    }

    void Check(int maximumBytes)
    {
        if (byteCount > maximumBytes) throw new EmailLimitException("MaxCalendarUnfoldedLineBytes", maximumBytes, byteCount);
    }

    internal string GetValue() { return value.ToString(); }
    internal EmailSourcePosition GetSourcePosition() { return sourcePosition; }
}
internal class EmailCalendarComponentBuilder
{
    string name;
    List<EmailCalendarProperty> properties;
    List<EmailCalendarComponent> components;

    internal EmailCalendarComponentBuilder(string name)
    {
        this.name = name;
        properties = new List<EmailCalendarProperty>();
        components = new List<EmailCalendarComponent>();
    }

    internal string GetName() { return name; }
    internal void AddProperty(EmailCalendarProperty value) { properties.Add(value); }
    internal void AddComponent(EmailCalendarComponent value) { components.Add(value); }
    internal List<EmailCalendarProperty> GetProperties() { return properties; }
    internal List<EmailCalendarComponent> GetComponents() { return components; }
    internal EmailCalendarComponent Build() { return new EmailCalendarComponent(name, properties, components); }
}
