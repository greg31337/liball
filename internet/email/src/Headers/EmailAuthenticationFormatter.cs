using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailAuthenticationFormatterWorker
{
    EmailAuthenticationFormatOptions options;
    List<byte> output;
    EmailIssueList issues;
    int column;
    int foldCount;
    bool stopped;

    internal EmailAuthenticationFormatterWorker(EmailAuthenticationFormatOptions options)
    {
        this.options = options;
        output = new List<byte>();
        issues = new EmailIssueList();
        column = options.GetFirstLineColumn();
        foldCount = 0;
        stopped = false;
    }

    internal void FormatAuthenticationResults(EmailAuthenticationResults value)
    {
        string? identifier = FormatValue(value.GetAuthenticationServiceIdentifier());
        if (identifier == null)
        {
            AddFatal("The authentication service identifier cannot be formatted safely.", value.GetSourcePosition());
            return;
        }

        Append(identifier, value.GetSourcePosition());
        if (value.HasVersion())
        {
            AppendPiece(" " + value.GetVersion().ToString(), value.GetSourcePosition());
        }

        if (options.GetIncludeComments())
        {
            int commentIndex = 0;
            while (commentIndex < value.GetCommentCount() && !stopped)
            {
                AppendPiece(" " + FormatComment(value.GetComment(commentIndex).GetValue()), value.GetComment(commentIndex).GetSourcePosition());
                commentIndex++;
            }
        }

        if (value.HasNoResults())
        {
            AppendPiece("; none", value.GetSourcePosition());
            return;
        }

        if (value.GetResultCount() == 0)
        {
            AddFatal("Authentication-Results requires a method result or the no-results marker.", value.GetSourcePosition());
            return;
        }

        int index = 0;
        while (index < value.GetResultCount() && !stopped)
        {
            FormatMethod(value.GetResult(index));
            index++;
        }
    }

    internal void FormatReceivedSpf(EmailReceivedSpf value)
    {
        if (!IsKeyword(value.GetResult()))
        {
            AddFatal("The Received-SPF result is not a valid keyword.", value.GetSourcePosition());
            return;
        }

        Append(value.GetResult().ToLowerInvariant(), value.GetSourcePosition());
        if (options.GetIncludeComments())
        {
            int commentIndex = 0;
            while (commentIndex < value.GetCommentCount() && !stopped)
            {
                AppendPiece(" " + FormatComment(value.GetComment(commentIndex).GetValue()), value.GetComment(commentIndex).GetSourcePosition());
                commentIndex++;
            }
        }

        int index = 0;
        while (index < value.GetParameterCount() && !stopped)
        {
            EmailReceivedSpfParameter parameter = value.GetParameter(index);
            if (!IsKeyword(parameter.GetName()))
            {
                AddFatal("A Received-SPF parameter name is not a valid keyword.", parameter.GetSourcePosition());
                break;
            }

            string? formattedValue = FormatValue(parameter.GetValue());
            if (formattedValue == null)
            {
                AddFatal("A Received-SPF parameter value cannot be formatted safely.", parameter.GetSourcePosition());
                break;
            }

            string prefix = index == 0 ? " " : "; ";
            AppendPiece(prefix + parameter.GetName().ToLowerInvariant() + "=" + formattedValue, parameter.GetSourcePosition());
            index++;
        }

        if (!stopped && value.GetParameterCount() > 0)
        {
            Append(";", value.GetSourcePosition());
        }
    }

    void FormatMethod(EmailAuthenticationMethodResult method)
    {
        if (!IsKeyword(method.GetMethod()) || !IsKeyword(method.GetResult()))
        {
            AddFatal("An authentication method or result is not a valid keyword.", method.GetSourcePosition());
            return;
        }

        StringBuilder primary = new StringBuilder();
        primary.Append("; ");
        primary.Append(method.GetMethod().ToLowerInvariant());
        if (method.HasVersion())
        {
            primary.Append('/');
            primary.Append(method.GetVersion());
        }

        primary.Append('=');
        primary.Append(method.GetResult().ToLowerInvariant());
        AppendPiece(primary.ToString(), method.GetSourcePosition());
        if (method.HasReason())
        {
            string? reason = FormatValue(method.GetReason());
            if (reason == null)
            {
                AddFatal("An authentication reason cannot be formatted safely.", method.GetSourcePosition());
                return;
            }

            AppendPiece(" reason=" + reason, method.GetSourcePosition());
        }

        int index = 0;
        while (index < method.GetPropertyCount() && !stopped)
        {
            EmailAuthenticationProperty property = method.GetProperty(index);
            if (!IsKeyword(property.GetPropertyType()) || !IsKeyword(property.GetProperty()))
            {
                AddFatal("An authentication property name or type is not a valid keyword.", property.GetSourcePosition());
                break;
            }

            string? value = FormatValue(property.GetValue());
            if (value == null)
            {
                AddFatal("An authentication property value cannot be formatted safely.", property.GetSourcePosition());
                break;
            }

            AppendPiece(" " + property.GetPropertyType().ToLowerInvariant() + "." + property.GetProperty().ToLowerInvariant() + "=" + value, property.GetSourcePosition());
            index++;
        }

        if (options.GetIncludeComments())
        {
            int commentIndex = 0;
            while (commentIndex < method.GetCommentCount() && !stopped)
            {
                AppendPiece(" " + FormatComment(method.GetComment(commentIndex).GetValue()), method.GetComment(commentIndex).GetSourcePosition());
                commentIndex++;
            }
        }
    }

    internal EmailAuthenticationFormatResult CreateResult()
    {
        if (stopped)
        {
            return new EmailAuthenticationFormatResult(new byte[0], issues, 0, options.GetFirstLineColumn());
        }

        return new EmailAuthenticationFormatResult(output.ToArray(), issues, foldCount, column);
    }

    void AppendPiece(string value, EmailSourcePosition position)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        if (column > 0 && column + bytes.Length > options.GetMaximumLineLength())
        {
            Append("\r\n\t", position);
            value = value.TrimStart();
        }

        Append(value, position);
    }

    void Append(string value, EmailSourcePosition position)
    {
        if (stopped)
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        if ((long)output.Count + bytes.Length > options.GetMaximumOutputBytes())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.AuthenticationOutputLimitExceeded, "The formatted authentication value exceeds the configured output-byte limit.", position, null, null, "No partial formatted value was returned."));
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
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.AuthenticationFormattingInvalid, description, position, null, null, "No formatted authentication value was returned."));
        stopped = true;
    }

    static string? FormatValue(string? value)
    {
        if (value == null || ContainsNewline(value))
        {
            return null;
        }

        if (IsMimeToken(value))
        {
            return value;
        }

        StringBuilder quoted = new StringBuilder();
        quoted.Append('"');
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '"' || current == '\\')
            {
                quoted.Append('\\');
            }

            quoted.Append(current);
            index++;
        }

        quoted.Append('"');
        return quoted.ToString();
    }

    static string FormatComment(string value)
    {
        StringBuilder comment = new StringBuilder();
        comment.Append('(');
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '(' || current == ')' || current == '\\')
            {
                comment.Append('\\');
            }

            if (current == '\r' || current == '\n')
            {
                comment.Append(' ');
            }
            else
            {
                comment.Append(current);
            }

            index++;
        }

        comment.Append(')');
        return comment.ToString();
    }

    static bool IsKeyword(string value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool valid = current >= 'A' && current <= 'Z' || current >= 'a' && current <= 'z' || current >= '0' && current <= '9' || current == '-';
            if (!valid)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsMimeToken(string value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        string special = "()<>@,;:\\\"/[]?=";
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current <= 32 || current >= 127 || special.IndexOf(current) >= 0)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool ContainsNewline(string value)
    {
        return value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0;
    }
}
