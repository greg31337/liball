using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses calendar into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailCalendarParser : IEmailCalendarParserContract
{
    internal static EmailCalendarParseResult ParseImplementationWithInputUsingReadOnlySpan(ReadOnlySpan<byte> input)
    {
        return ParseImplementationWithInputAndOptionsUsingReadOnlySpanAndEmailCalendarParseOptions(input, new EmailCalendarParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailCalendarParseResult ParseWithInputUsingReadOnlySpan(ReadOnlySpan<byte> input)
    {
        return EmailCalendarParser.ParseImplementationWithInputUsingReadOnlySpan(input);
    }

    internal static EmailCalendarParseResult ParseImplementationWithInputAndOptionsUsingReadOnlySpanAndEmailCalendarParseOptions(ReadOnlySpan<byte> input, EmailCalendarParseOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCalendarParseOptions snapshot = options.Clone();
        if (input.Length > snapshot.GetMaximumCalendarBytes())
            throw new EmailLimitException("MaxCalendarBytes", snapshot.GetMaximumCalendarBytes(), input.Length);
        byte[] bytes = input.ToArray();
        EmailIssueList issues = new EmailIssueList();
        byte[] unfoldedBytes = UnfoldUtf8Bytes(bytes, snapshot);
        string text = Encoding.UTF8.GetString(unfoldedBytes);
        if (!new EmailUtf8().IsValid(unfoldedBytes))
        {
            issues.Add(IssueImplementation(EmailIssueSeverity.Error, EmailIssueCode.CalendarUtf8Invalid, "The iCalendar object is not valid UTF-8.", null, "Invalid byte sequences were replaced so recoverable content lines could still be inspected."));
        }

        if (text.IndexOf('\0') >= 0)
        {
            issues.Add(IssueImplementation(EmailIssueSeverity.Error, EmailIssueCode.CalendarMalformed, "The iCalendar object contains NUL characters.", null, "NUL characters were replaced so other properties could be retained."));
            text = text.Replace("\0", "\uFFFD");
        }

        EmailCalendarParseOptions workerOptions = snapshot.Clone();
        workerOptions.SetMaximumPhysicalLineBytes(snapshot.GetMaximumUnfoldedLineBytes());
        EmailCalendarParserWorker worker = new EmailCalendarParserWorker(text, workerOptions, issues);
        return worker.Parse();
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailCalendarParseResult ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCalendarParseOptions(ReadOnlySpan<byte> input, EmailCalendarParseOptions options)
    {
        return EmailCalendarParser.ParseImplementationWithInputAndOptionsUsingReadOnlySpanAndEmailCalendarParseOptions(input, options);
    }

    internal static EmailCalendarParseResult ParseImplementationWithInputUsingString(string input)
    {
        return ParseImplementationWithInputAndOptionsUsingStringAndEmailCalendarParseOptions(input, new EmailCalendarParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailCalendarParseResult ParseWithInputUsingString(string input)
    {
        return EmailCalendarParser.ParseImplementationWithInputUsingString(input);
    }

    internal static EmailCalendarParseResult ParseImplementationWithInputAndOptionsUsingStringAndEmailCalendarParseOptions(string input, EmailCalendarParseOptions options)
    {
        if (input == null)
            throw new ArgumentNullException("input");
        return ParseImplementationWithInputAndOptionsUsingReadOnlySpanAndEmailCalendarParseOptions(Encoding.UTF8.GetBytes(input), options);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCalendarParseResult ParseWithInputAndOptionsUsingStringAndEmailCalendarParseOptions(string input, EmailCalendarParseOptions options)
    {
        return EmailCalendarParser.ParseImplementationWithInputAndOptionsUsingStringAndEmailCalendarParseOptions(input, options);
    }

    internal static EmailCalendarParseResult ParseImplementationWithEntity(MimeEntity entity)
    {
        return ParseImplementationWithEntityAndOptions(entity, new EmailCalendarParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailCalendarParseResult ParseWithEntity(MimeEntity entity)
    {
        return EmailCalendarParser.ParseImplementationWithEntity(entity);
    }

    internal static EmailCalendarParseResult ParseImplementationWithEntityAndOptions(MimeEntity entity, EmailCalendarParseOptions options)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCalendarParseOptions snapshot = options.Clone();
        if (!(entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "calendar"))
        {
            EmailIssueList wrongType = new EmailIssueList();
            wrongType.Add(IssueImplementation(EmailIssueSeverity.Error, EmailIssueCode.CalendarMalformed, "The MIME entity is not text/calendar.", "Content-Type", "No calendar was produced."));
            return new EmailCalendarParseResult(null, wrongType, null, false, false);
        }

        if (!entity.HasContent())
        {
            EmailIssueList missing = new EmailIssueList();
            missing.Add(IssueImplementation(EmailIssueSeverity.Error, EmailIssueCode.CalendarMalformed, "The text/calendar entity has no direct content.", null, "No calendar was produced."));
            return new EmailCalendarParseResult(null, missing, GetMediaTypeParameter(entity.GetMediaType(), "method"), false, false);
        }

        EmailTextDecodeOptions textOptions = new EmailTextDecodeOptions();
        textOptions.SetFallbackCharset("utf-8");
        textOptions.SetMaximumDecodedBytes(snapshot.GetMaximumCalendarBytes());
        EmailTextDecodeResult decoded = new EmailTextDecoder().DecodeWithEntityAndOptionsAndPathAndLimitNameAndReportedLimitAndReportedObserved(entity, textOptions, null, "MaxCalendarBytes", snapshot.GetMaximumCalendarBytes(), (long)snapshot.GetMaximumCalendarBytes() + 1);
        EmailCalendarParseResult parsed = ParseImplementationWithInputAndOptionsUsingStringAndEmailCalendarParseOptions(decoded.GetText(), snapshot);
        EmailIssueList issues = parsed.GetIssues();
        issues.AddRange(decoded.GetIssues());
        string? mimeMethod = GetMediaTypeParameter(entity.GetMediaType(), "method");
        bool consistent = true;
        bool imip = false;
        if (mimeMethod != null)
        {
            string? calendarMethod = parsed.HasCalendar() ? (parsed.GetCalendar() ?? throw new InvalidOperationException()).GetMethodName() : null;
            consistent = calendarMethod != null && string.Equals(mimeMethod, calendarMethod, StringComparison.OrdinalIgnoreCase);
            if (!consistent)
            {
                issues.Add(IssueImplementation(EmailIssueSeverity.Error, EmailIssueCode.CalendarMethodMismatch, calendarMethod == null ? "The iMIP method MIME parameter has no matching METHOD calendar property." : "The iMIP method MIME parameter does not match the METHOD calendar property.", "Content-Type", "Both method values were retained for comparison."));
            }

            imip = consistent;
            ValidateImipParticipants(parsed.GetCalendar(), issues);
            ValidateImipCharset(entity, decoded, issues);
            ValidateComponentParameter(entity, parsed.GetCalendar(), issues);
        }

        return new EmailCalendarParseResult(parsed.GetCalendar(), issues, mimeMethod, imip, consistent);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCalendarParseResult ParseWithEntityAndOptions(MimeEntity entity, EmailCalendarParseOptions options)
    {
        return EmailCalendarParser.ParseImplementationWithEntityAndOptions(entity, options);
    }

    internal static EmailCalendarParseResult ParseImplementationWithMessage(EmailMessage message)
    {
        return ParseImplementationWithMessageAndOptions(message, new EmailCalendarParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailCalendarParseResult ParseWithMessage(EmailMessage message)
    {
        return EmailCalendarParser.ParseImplementationWithMessage(message);
    }

    internal static EmailCalendarParseResult ParseImplementationWithMessageAndOptions(EmailMessage message, EmailCalendarParseOptions options)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCalendarParseOptions snapshot = options.Clone();
        if (!message.HasBody())
        {
            EmailIssueList missing = new EmailIssueList();
            missing.Add(IssueImplementation(EmailIssueSeverity.Error, EmailIssueCode.CalendarMalformed, "The message has no MIME body containing a calendar.", null, "No calendar was produced."));
            return new EmailCalendarParseResult(null, missing, null, false, false);
        }

        List<MimeEntity> entities = new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(message.GetBody(), snapshot.GetMaximumTraversalDepth(), snapshot.GetMaximumTraversalEntityCount());
        MimeEntity? selected = null;
        int calendarCount = 0;
        int index = 0;
        while (index < entities.Count)
        {
            if ((entities[index].GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "calendar"))
            {
                if (selected == null)
                    selected = entities[index];
                calendarCount++;
            }

            index++;
        }

        if (selected == null)
        {
            EmailIssueList absent = new EmailIssueList();
            absent.Add(IssueImplementation(EmailIssueSeverity.Error, EmailIssueCode.CalendarMalformed, "The message contains no text/calendar MIME entity.", "Content-Type", "No calendar was produced."));
            return new EmailCalendarParseResult(null, absent, null, false, false);
        }

        EmailCalendarParseResult result = ParseImplementationWithEntityAndOptions(selected, snapshot);
        if (calendarCount == 1)
            return result;
        EmailIssueList issues = result.GetIssues();
        issues.Add(IssueImplementation(EmailIssueSeverity.Warning, EmailIssueCode.CalendarMalformed, "The message contains multiple text/calendar entities.", "Content-Type", "Only the first calendar in depth-first MIME order was parsed."));
        return new EmailCalendarParseResult(result.GetCalendar(), issues, result.GetMimeMethod(), result.IsImip(), result.IsMethodConsistent());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCalendarParseResult ParseWithMessageAndOptions(EmailMessage message, EmailCalendarParseOptions options)
    {
        return EmailCalendarParser.ParseImplementationWithMessageAndOptions(message, options);
    }

    static void ValidateImipParticipants(EmailCalendar? calendar, EmailIssueList issues)
    {
        if (calendar == null)
            return;
        List<EmailCalendarComponent> components = calendar.GetComponents();
        int index = 0;
        while (index < components.Count)
        {
            EmailCalendarParticipant? organizer = components[index].GetOrganizer();
            if (organizer != null && !organizer.IsMailto())
                issues.Add(IssueImplementation(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "An iMIP ORGANIZER value must be a mailto URI.", "ORGANIZER", "The original calendar address was retained."));
            List<EmailCalendarParticipant> attendees = components[index].GetAttendees();
            int attendeeIndex = 0;
            while (attendeeIndex < attendees.Count)
            {
                if (!attendees[attendeeIndex].IsMailto())
                    issues.Add(IssueImplementation(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "An iMIP ATTENDEE value must be a mailto URI.", "ATTENDEE", "The original calendar address was retained."));
                attendeeIndex++;
            }

            index++;
        }
    }

    static void ValidateImipCharset(MimeEntity entity, EmailTextDecodeResult decoded, EmailIssueList issues)
    {
        if (!ContainsNonAscii(decoded.GetText()))
            return;
        string? charset = GetMediaTypeParameter(entity.GetMediaType(), "charset");
        if (!string.Equals(charset, "utf-8", StringComparison.OrdinalIgnoreCase))
            issues.Add(IssueImplementation(EmailIssueSeverity.Error, EmailIssueCode.CalendarPropertyInvalid, "An iMIP calendar containing non-ASCII text requires charset=UTF-8.", "Content-Type", "The decoded calendar text was retained."));
    }

    static void ValidateComponentParameter(MimeEntity entity, EmailCalendar? calendar, EmailIssueList issues)
    {
        string? component = GetMediaTypeParameter(entity.GetMediaType(), "component");
        if (component == null || calendar == null)
            return;
        List<EmailCalendarComponent> components = calendar.GetComponentsNamed(component);
        if (components.Count == 0)
            issues.Add(IssueImplementation(EmailIssueSeverity.Warning, EmailIssueCode.CalendarComponentMismatch, "The optional iMIP component MIME parameter does not name a contained calendar component.", "Content-Type", "The MIME parameter and all calendar components were retained."));
    }

    static string? GetMediaTypeParameter(EmailMediaType? mediaType, string name)
    {
        EmailRfc2231DecodedParameter? decoded = (mediaType ?? throw new InvalidOperationException()).GetParameters().GetDecoded(name);
        if (decoded != null)
            return Encoding.ASCII.GetString(decoded.GetDecodedBytes()).Trim();
        EmailParameter? physical = mediaType.GetParameters().GetFirst(name);
        return physical == null ? null : Encoding.ASCII.GetString(physical.GetValueBytes()).Trim().Trim('"');
    }

    static bool ContainsNonAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
                return true;
            index++;
        }

        return false;
    }

    static byte[] UnfoldUtf8Bytes(byte[] input, EmailCalendarParseOptions options)
    {
        List<byte> output = new List<byte>();
        int lineStart = 0;
        int physicalLines = 0;
        int index = 0;
        while (index < input.Length)
        {
            int contentEnd = index;
            while (contentEnd < input.Length && input[contentEnd] != 13 && input[contentEnd] != 10)
                contentEnd++;
            int length = contentEnd - index;
            if (length > options.GetMaximumPhysicalLineBytes())
                throw new EmailLimitException("MaxCalendarPhysicalLineBytes", options.GetMaximumPhysicalLineBytes(), length);
            if (physicalLines >= options.GetMaximumContentLines())
                throw new EmailLimitException("MaxCalendarContentLines", options.GetMaximumContentLines(), physicalLines + 1);
            int endingLength = 0;
            if (contentEnd < input.Length)
                endingLength = input[contentEnd] == 13 && contentEnd + 1 < input.Length && input[contentEnd + 1] == 10 ? 2 : 1;
            int copyIndex = index;
            while (copyIndex < contentEnd)
            {
                output.Add(input[copyIndex]);
                copyIndex++;
            }

            bool folded = endingLength == 2 && contentEnd + 2 < input.Length && (input[contentEnd + 2] == 32 || input[contentEnd + 2] == 9);
            if (folded)
            {
                index = contentEnd + 3;
            }
            else
            {
                copyIndex = 0;
                while (copyIndex < endingLength)
                {
                    output.Add(input[contentEnd + copyIndex]);
                    copyIndex++;
                }

                index = contentEnd + endingLength;
            }

            physicalLines++;
            lineStart = index;
            if (endingLength == 0)
                break;
        }

        if (lineStart < input.Length)
        {
            int remaining = input.Length - lineStart;
            if (remaining > options.GetMaximumPhysicalLineBytes())
                throw new EmailLimitException("MaxCalendarPhysicalLineBytes", options.GetMaximumPhysicalLineBytes(), remaining);
        }

        return output.ToArray();
    }

    internal static EmailIssue IssueImplementation(EmailIssueSeverity severity, EmailIssueCode code, string description, string? fieldName, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, null, null, fieldName, recovery);
    }

    /// <summary>
    /// Determines whether the current value or state is sue.
    /// </summary>
    /// <param name = "severity">The severity used by the operation.</param>
    /// <param name = "code">The code used by the operation.</param>
    /// <param name = "description">The description used by the operation.</param>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "recovery">The recovery used by the operation.</param>
    /// <returns>The issue produced by the operation.</returns>
    public EmailIssue Issue(EmailIssueSeverity severity, EmailIssueCode code, string description, string? fieldName, string recovery)
    {
        return EmailCalendarParser.IssueImplementation(severity, code, description, fieldName, recovery);
    }
}
