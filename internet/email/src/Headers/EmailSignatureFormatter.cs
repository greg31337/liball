using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailSignatureFormatterWorker
{
    EmailSignatureFormatOptions options;
    List<byte> output;
    EmailIssueList issues;
    int column;
    int foldCount;
    bool stopped;

    internal EmailSignatureFormatterWorker(EmailSignatureFormatOptions options)
    {
        this.options = options;
        output = new List<byte>();
        issues = new EmailIssueList();
        column = options.GetFirstLineColumn();
        foldCount = 0;
        stopped = false;
    }

    internal void FormatTags(EmailSignatureTagList tags)
    {
        if (tags.GetCount() == 0)
        {
            AddFatal("A signature value requires at least one tag.", new EmailSourcePosition(0, 1, 1));
            return;
        }

        int index = 0;
        while (index < tags.GetCount() && !stopped)
        {
            EmailSignatureTag tag = tags.GetAt(index);
            string name = tag.GetNormalizedName();
            string value = GetCanonicalValue(tag);
            if (tag.IsMalformed() || !IsTagName(name) || !IsSafeValue(value))
            {
                AddFatal("A signature tag cannot be formatted safely.", tag.GetSourcePosition());
                break;
            }

            if (tags.HasDuplicate(name))
            {
                AddFatal("A signature value with duplicate tags cannot be formatted canonically.", tag.GetSourcePosition());
                break;
            }

            if (index > 0)
            {
                Append(";", tag.GetSourcePosition());
                if (column + 1 + Encoding.UTF8.GetByteCount(name) + 1 + Encoding.UTF8.GetByteCount(value) > options.GetMaximumLineLength())
                {
                    Append("\r\n\t", tag.GetSourcePosition());
                }
                else
                {
                    Append(" ", tag.GetSourcePosition());
                }
            }
            else if (column > 0 && column + Encoding.UTF8.GetByteCount(name) + 1 + Encoding.UTF8.GetByteCount(value) > options.GetMaximumLineLength())
            {
                Append("\r\n\t", tag.GetSourcePosition());
            }

            Append(name + "=", tag.GetSourcePosition());
            if (name == "b" || name == "bh")
            {
                AppendBase64(value, tag.GetSourcePosition());
            }
            else
            {
                Append(value, tag.GetSourcePosition());
            }

            index++;
        }
    }

    internal void FormatArcAuthenticationResults(EmailArcAuthenticationResults value)
    {
        if (value.GetInstance() < 1 || value.GetInstance() > 50)
        {
            AddFatal("ARC-Authentication-Results requires an instance from 1 through 50.", value.GetSourcePosition());
            return;
        }

        string prefix = "i=" + value.GetInstance().ToString() + "; ";
        Append(prefix, value.GetSourcePosition());
        if (stopped)
        {
            return;
        }

        EmailAuthenticationFormatOptions authenticationOptions = new EmailAuthenticationFormatOptions();
        authenticationOptions.SetFirstLineColumn(column);
        authenticationOptions.SetMaximumLineLength(options.GetMaximumLineLength());
        authenticationOptions.SetMaximumOutputBytes(options.GetMaximumOutputBytes());
        EmailAuthenticationFormatResult formatted = value.GetAuthenticationResults().FormatWithOptions(authenticationOptions);
        if (!formatted.IsSuccess())
        {
            issues.AddRange(formatted.GetIssues());
            stopped = true;
            return;
        }

        AppendBytes(formatted.GetBytes(), value.GetAuthenticationResults().GetSourcePosition());
    }

    internal EmailSignatureFormatResult CreateResult()
    {
        if (stopped)
        {
            return new EmailSignatureFormatResult(new byte[0], issues, 0, options.GetFirstLineColumn());
        }

        return new EmailSignatureFormatResult(output.ToArray(), issues, foldCount, column);
    }

    void AppendBase64(string value, EmailSourcePosition position)
    {
        int index = 0;
        while (index < value.Length && !stopped)
        {
            int available = options.GetMaximumLineLength() - column;
            if (available <= 0)
            {
                Append("\r\n\t", position);
                available = options.GetMaximumLineLength() - column;
            }

            int count = value.Length - index;
            if (count > available)
            {
                count = available;
            }

            if (count <= 0)
            {
                AddFatal("The configured line length cannot accommodate signature data.", position);
                return;
            }

            Append(value.Substring(index, count), position);
            index += count;
            if (index < value.Length)
            {
                Append("\r\n\t", position);
            }
        }
    }

    void Append(string value, EmailSourcePosition position)
    {
        AppendBytes(Encoding.UTF8.GetBytes(value), position);
    }

    void AppendBytes(byte[] bytes, EmailSourcePosition position)
    {
        if (stopped)
        {
            return;
        }

        if ((long)output.Count + bytes.Length > options.GetMaximumOutputBytes())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.SignatureOutputLimitExceeded, "The formatted signature value exceeds the configured output-byte limit.", position, null, null, "No partial formatted value was returned."));
            stopped = true;
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

            column = current == 10 ? 0 : column + 1;
            index++;
        }
    }

    void AddFatal(string description, EmailSourcePosition position)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.SignatureFormattingInvalid, description, position, null, null, "No formatted signature value was returned."));
        stopped = true;
    }

    static string GetCanonicalValue(EmailSignatureTag tag)
    {
        string name = tag.GetNormalizedName();
        if (name == "b" || name == "bh" || name == "v" || name == "l" || name == "t" || name == "x")
        {
            return tag.GetCompactValue();
        }

        if (name == "a" || name == "c" || name == "cv" || name == "q")
        {
            return tag.GetCompactValue().ToLowerInvariant();
        }

        return tag.GetValue();
    }

    static bool IsTagName(string value)
    {
        if (value.Length == 0 || !IsAlpha(value[0]))
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            char current = value[index];
            if (!IsAlpha(current) && !(current >= '0' && current <= '9') && current != '_')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsSafeValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n' || current == ';' || current < 32 && current != '\t' || current == 127)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsAlpha(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }
}
