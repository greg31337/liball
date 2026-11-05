using System;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Formats date for email output.
/// </summary>
public class EmailDateFormatter : IEmailDateFormatterContract
{
    internal static EmailDateFormatResult FormatImplementationWithValue(EmailDateTime? value)
    {
        return FormatImplementationWithValueAndOptions(value, new EmailDateFormatOptions());
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The date format result produced by the operation.</returns>
    public EmailDateFormatResult FormatWithValue(EmailDateTime? value)
    {
        return EmailDateFormatter.FormatImplementationWithValue(value);
    }

    internal static EmailDateFormatResult FormatImplementationWithValueAndOptions(EmailDateTime? value, EmailDateFormatOptions options)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailDateFormatOptions snapshot = options.Clone();
        EmailIssueList issues = ValidateForFormatting(value);
        if (issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal))
        {
            return new EmailDateFormatResult(new byte[0], issues);
        }

        string formatted = BuildCanonicalValue(value);
        byte[] bytes = Encoding.ASCII.GetBytes(formatted);
        if (bytes.Length > snapshot.GetMaximumOutputBytes())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.DateOutputLimitExceeded, "The canonical date exceeds the configured output-byte limit.", value.GetSourcePosition(), null, null, "No partial date value was returned."));
            return new EmailDateFormatResult(new byte[0], issues);
        }

        return new EmailDateFormatResult(bytes, issues);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The date format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDateFormatResult FormatWithValueAndOptions(EmailDateTime? value, EmailDateFormatOptions options)
    {
        return EmailDateFormatter.FormatImplementationWithValueAndOptions(value, options);
    }

    static EmailIssueList ValidateForFormatting(EmailDateTime value)
    {
        EmailIssueList issues = new EmailIssueList();
        EmailIssueList validationIssues = new EmailDateValidator().Validate(value).GetIssues();
        int index = 0;
        while (index < validationIssues.GetCount())
        {
            EmailIssue issue = validationIssues.GetAt(index);
            if (issue.GetCode() == EmailIssueCode.DateDayOfWeekMismatch)
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.DateDayOfWeekMismatch, issue.GetDescription(), issue.GetSourcePosition(), null, null, "The canonical day-of-week was recomputed from the calendar date."));
            }
            else
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.DateFormattingInvalid, issue.GetDescription(), issue.GetSourcePosition(), null, null, "No date bytes were generated."));
            }

            index++;
        }

        return issues;
    }

    static string BuildCanonicalValue(EmailDateTime value)
    {
        string[] dayNames = new string[]
        {
            "Sun",
            "Mon",
            "Tue",
            "Wed",
            "Thu",
            "Fri",
            "Sat"
        };
        string[] monthNames = new string[]
        {
            "Jan",
            "Feb",
            "Mar",
            "Apr",
            "May",
            "Jun",
            "Jul",
            "Aug",
            "Sep",
            "Oct",
            "Nov",
            "Dec"
        };
        EmailDateDayOfWeek calculatedDay = EmailDateMath.GetDayOfWeek(value.GetYear(), value.GetMonth(), value.GetDay());
        StringBuilder output = new StringBuilder();
        output.Append(dayNames[(int)calculatedDay]);
        output.Append(", ");
        AppendTwoDigits(output, value.GetDay());
        output.Append(' ');
        output.Append(monthNames[value.GetMonth() - 1]);
        output.Append(' ');
        output.Append(value.GetYear().ToString("D4", CultureInfo.InvariantCulture));
        output.Append(' ');
        AppendTwoDigits(output, value.GetHour());
        output.Append(':');
        AppendTwoDigits(output, value.GetMinute());
        output.Append(':');
        AppendTwoDigits(output, value.HasSecond() ? value.GetSecond() : 0);
        output.Append(' ');
        AppendZone(output, value);
        return output.ToString();
    }

    static void AppendZone(StringBuilder output, EmailDateTime value)
    {
        if (!value.HasKnownZoneOffset())
        {
            output.Append("-0000");
            return;
        }

        int offset = value.GetZoneOffsetMinutes();
        if (offset < 0)
        {
            output.Append('-');
            offset = -offset;
        }
        else
        {
            output.Append('+');
        }

        AppendTwoDigits(output, offset / 60);
        AppendTwoDigits(output, offset % 60);
    }

    static void AppendTwoDigits(StringBuilder output, int value)
    {
        output.Append((char)('0' + value / 10));
        output.Append((char)('0' + value % 10));
    }
}
