using System;

namespace email;
/// <summary>
/// Validates date and reports policy violations.
/// </summary>
public class EmailDateValidator : IEmailDateValidatorContract
{
    internal static EmailDateValidationResult ValidateImplementation(EmailDateTime value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        EmailIssueList issues = new EmailIssueList();
        EmailSourcePosition position = value.GetSourcePosition();
        if (value.GetYear() < 1900)
        {
            AddInvalid(issues, "The year is earlier than 1900.", position);
        }

        if (value.GetDay() > EmailDateMath.GetDaysInMonth(value.GetYear(), value.GetMonth()))
        {
            AddInvalid(issues, "The day does not exist in the stated month and year.", position);
        }

        bool expectedLeapSecond = value.HasSecond() && value.GetSecond() == 60;
        if (value.IsLeapSecond() != expectedLeapSecond)
        {
            AddInvalid(issues, "The leap-second metadata is inconsistent with the clock value.", position);
        }

        if (!value.HasSecond() && value.GetSecond() != 0)
        {
            AddInvalid(issues, "A date without an explicit seconds component has a nonzero second value.", position);
        }

        if (value.HasDayOfWeek())
        {
            bool matches = EmailDateMath.GetDayOfWeek(value.GetYear(), value.GetMonth(), value.GetDay()) == value.GetDayOfWeek();
            if (!matches || value.DoesDayOfWeekMatch() != matches)
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.DateDayOfWeekMismatch, "The stated day-of-week or its parse metadata does not match the calendar date.", position, null, null, "Recompute the day-of-week from the calendar date."));
            }
        }
        else if (!value.DoesDayOfWeekMatch())
        {
            AddInvalid(issues, "A date without a day-of-week is marked as having a day-of-week mismatch.", position);
        }

        if (value.HasKnownZoneOffset())
        {
            if (value.GetZoneOffsetMinutes() < -5999 || value.GetZoneOffsetMinutes() > 5999)
            {
                AddInvalid(issues, "The known zone offset cannot be represented by the RFC four-digit zone form.", position);
            }

            if (value.GetZoneKind() == EmailDateZoneKind.ObsoleteMilitary || value.GetZoneKind() == EmailDateZoneKind.UnknownNamed)
            {
                AddInvalid(issues, "An unreliable alphabetic zone cannot have a known offset without out-of-band information.", position);
            }
        }
        else
        {
            if (value.GetZoneOffsetMinutes() != 0)
            {
                AddInvalid(issues, "An unknown zone must not carry a numeric offset.", position);
            }

            if (value.GetZoneKind() == EmailDateZoneKind.ObsoleteNamed)
            {
                AddInvalid(issues, "A recognized obsolete named zone must carry its defined numeric offset.", position);
            }
        }

        return new EmailDateValidationResult(issues);
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDateValidationResult Validate(EmailDateTime value)
    {
        return EmailDateValidator.ValidateImplementation(value);
    }

    static void AddInvalid(EmailIssueList issues, string description, EmailSourcePosition position)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.DateSemanticInvalid, description, position, null, null, "Correct the inconsistent date value before using it."));
    }
}
