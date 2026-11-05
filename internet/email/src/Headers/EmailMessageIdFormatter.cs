using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailMessageIdFormatterWorker
{
    EmailMessageIdFormatOptions options;
    List<byte> output;
    EmailIssueList issues;
    int column;
    int foldCount;
    bool stopped;

    internal EmailMessageIdFormatterWorker(EmailMessageIdFormatOptions options)
    {
        this.options = options;
        output = new List<byte>();
        issues = new EmailIssueList();
        column = options.GetFirstLineColumn();
        foldCount = 0;
        stopped = false;
    }

    internal void FormatIdentifier(EmailMessageId identifier)
    {
        if (stopped)
        {
            return;
        }

        string? canonical = BuildCanonical(identifier);
        if (canonical == null)
        {
            AddFatal(EmailIssueCode.MessageIdFormattingInvalid, "The identifier cannot be represented by the current RFC message-id syntax.", identifier.GetSourcePosition(), "No identifier bytes were generated.");
            return;
        }

        Append(canonical, identifier.GetSourcePosition());
    }

    internal void FormatList(EmailMessageIdList identifiers)
    {
        if (identifiers.GetCount() == 0)
        {
            AddFatal(EmailIssueCode.MessageIdFormattingInvalid, "An identifier list cannot be empty.", new EmailSourcePosition(0, 1, 1), "No identifier bytes were generated.");
            return;
        }

        int index = 0;
        while (index < identifiers.GetCount() && !stopped)
        {
            EmailMessageId identifier = identifiers.GetAt(index);
            string? canonical = BuildCanonical(identifier);
            if (canonical == null)
            {
                AddFatal(EmailIssueCode.MessageIdFormattingInvalid, "An identifier in the list cannot be represented by the current RFC message-id syntax.", identifier.GetSourcePosition(), "No identifier bytes were generated.");
                break;
            }

            if (index > 0)
            {
                if (column + 1 + canonical.Length > options.GetMaximumLineLength())
                {
                    Append("\r\n ", identifier.GetSourcePosition());
                }
                else
                {
                    Append(" ", identifier.GetSourcePosition());
                }
            }

            Append(canonical, identifier.GetSourcePosition());
            index++;
        }
    }

    internal EmailMessageIdFormatResult CreateResult()
    {
        if (stopped)
        {
            return new EmailMessageIdFormatResult(new byte[0], issues, 0, options.GetFirstLineColumn());
        }

        return new EmailMessageIdFormatResult(output.ToArray(), issues, foldCount, column);
    }

    string? BuildCanonical(EmailMessageId identifier)
    {
        if (!IsDotAtomText(identifier.GetIdLeft()))
        {
            return null;
        }

        if (identifier.IsRightDomainLiteral())
        {
            if (!IsDomainLiteralText(identifier.GetIdRight()))
            {
                return null;
            }

            return "<" + identifier.GetIdLeft() + "@[" + identifier.GetIdRight() + "]>";
        }

        if (!IsDotAtomText(identifier.GetIdRight()))
        {
            return null;
        }

        return "<" + identifier.GetIdLeft() + "@" + identifier.GetIdRight() + ">";
    }

    void Append(string value, EmailSourcePosition position)
    {
        if (stopped)
        {
            return;
        }

        byte[] bytes = Encoding.ASCII.GetBytes(value);
        if ((long)output.Count + bytes.Length > options.GetMaximumOutputBytes())
        {
            AddFatal(EmailIssueCode.MessageIdOutputLimitExceeded, "The formatted identifier value exceeds the configured output-byte limit.", position, "No partial identifier value was returned.");
            return;
        }

        int index = 0;
        while (index < bytes.Length)
        {
            byte current = bytes[index];
            output.Add(current);
            if (current == 13 && index + 1 < bytes.Length && bytes[index + 1] == 10)
            {
                output.Add(10);
                column = 0;
                foldCount++;
                index += 2;
                continue;
            }

            if (current == 10)
            {
                column = 0;
            }
            else
            {
                column++;
            }

            index++;
        }
    }

    void AddFatal(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, code, description, position, null, null, recovery));
        stopped = true;
    }

    static bool IsDotAtomText(string value)
    {
        if (value == null || value.Length == 0 || value[0] == '.' || value[value.Length - 1] == '.')
        {
            return false;
        }

        bool previousDot = false;
        int index = 0;
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
                if (character > 127 || !new EmailByteScanner().IsAtext((byte)character))
                {
                    return false;
                }

                previousDot = false;
            }

            index++;
        }

        return true;
    }

    static bool IsDomainLiteralText(string value)
    {
        if (value == null)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool allowed = character >= 33 && character <= 90 || character >= 94 && character <= 126;
            if (!allowed)
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
