using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailDeliveryStatusValueParser
{
    internal static EmailDeliveryStatusTypedValue? ParseTyped(EmailHeader? field)
    {
        string? text = GetText(field);
        if (text == null) return null;
        int semicolon = text.IndexOf(';');
        if (semicolon <= 0 || semicolon >= text.Length - 1) return null;
        string type = text.Substring(0, semicolon).Trim();
        string value = text.Substring(semicolon + 1).Trim();
        if (!IsToken(type) || value.Length == 0) return null;
        return new EmailDeliveryStatusTypedValue(type, value);
    }

    internal static EmailDeliveryStatusAction ParseAction(EmailHeader? field)
    {
        string? text = GetText(field);
        if (string.Equals(text, "failed", StringComparison.OrdinalIgnoreCase)) return EmailDeliveryStatusAction.Failed;
        if (string.Equals(text, "delayed", StringComparison.OrdinalIgnoreCase)) return EmailDeliveryStatusAction.Delayed;
        if (string.Equals(text, "delivered", StringComparison.OrdinalIgnoreCase)) return EmailDeliveryStatusAction.Delivered;
        if (string.Equals(text, "relayed", StringComparison.OrdinalIgnoreCase)) return EmailDeliveryStatusAction.Relayed;
        if (string.Equals(text, "expanded", StringComparison.OrdinalIgnoreCase)) return EmailDeliveryStatusAction.Expanded;
        return EmailDeliveryStatusAction.Unknown;
    }

    internal static EmailDeliveryStatusCode? ParseStatus(EmailHeader? field)
    {
        string? text = GetText(field);
        if (text == null) return null;
        string[] parts = text.Split('.');
        int statusClass;
        int subject;
        int detail;
        if (parts.Length != 3 || parts[0].Length != 1 || parts[1].Length < 1 || parts[1].Length > 3 || parts[2].Length < 1 || parts[2].Length > 3) return null;
        if (!IsDigits(parts[0]) || !IsDigits(parts[1]) || !IsDigits(parts[2])) return null;
        if (!int.TryParse(parts[0], out statusClass) || !int.TryParse(parts[1], out subject) || !int.TryParse(parts[2], out detail)) return null;
        if (statusClass != 2 && statusClass != 4 && statusClass != 5) return null;
        return new EmailDeliveryStatusCode(statusClass, subject, detail);
    }

    internal static EmailDateTime? ParseDate(EmailHeader? field)
    {
        if (field == null) return null;
        EmailDateParseResult parsed = new EmailDateParser().ParseWithInputAndMode(field.GetUnfoldedValue(), EmailDateParseMode.Tolerant);
        return parsed.HasValue() ? parsed.GetValue() : null;
    }

    internal static string? GetText(EmailHeader? field)
    {
        if (field == null) return null;
        string value = RemoveComments(field.GetDecodedValue()).Trim();
        return value.Length == 0 ? null : value;
    }

    static string RemoveComments(string? value)
    {
        StringBuilder output = new StringBuilder();
        int commentDepth = 0;
        bool quoted = false;
        bool escaped = false;
        int index = 0;
        while (index < (value ?? throw new InvalidOperationException()).Length)
        {
            char current = value[index];
            if (escaped)
            {
                if (commentDepth == 0) output.Append(current);
                escaped = false;
            }
            else if (current == '\\')
            {
                if (commentDepth == 0) output.Append(current);
                escaped = true;
            }
            else if (commentDepth > 0)
            {
                if (current == '(') commentDepth++;
                else if (current == ')') commentDepth--;
            }
            else if (current == '"')
            {
                quoted = !quoted;
                output.Append(current);
            }
            else if (!quoted && current == '(')
            {
                if (output.Length > 0 && !char.IsWhiteSpace(output[output.Length - 1])) output.Append(' ');
                commentDepth = 1;
            }
            else output.Append(current);
            index++;
        }

        return output.ToString();
    }

    internal static List<EmailHeader> GetExtensions(EmailReportFieldBlock block, string[] known)
    {
        List<EmailHeader> result = new List<EmailHeader>();
        int index = 0;
        while (index < block.GetFieldCount())
        {
            EmailHeader field = block.GetFieldAt(index);
            if (!Contains(known, field.GetName())) result.Add(field);
            index++;
        }

        return result;
    }

    internal static string[] MessageFieldNames()
    {
        return new string[] { "Reporting-MTA", "DSN-Gateway", "Received-From-MTA", "Arrival-Date", "Original-Envelope-Id" };
    }

    internal static string[] RecipientFieldNames()
    {
        return new string[] { "Original-Recipient", "Final-Recipient", "Action", "Status", "Remote-MTA", "Diagnostic-Code", "Last-Attempt-Date", "Final-Log-ID", "Will-Retry-Until" };
    }

    static bool Contains(string[] values, string expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (string.Equals(values[index], expected, StringComparison.OrdinalIgnoreCase)) return true;
            index++;
        }

        return false;
    }

    static bool IsToken(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == '(' || current == ')' || current == '<' || current == '>' || current == '@' || current == ',' || current == ';' || current == ':' || current == '\\' || current == '"' || current == '/' || current == '[' || current == ']' || current == '?' || current == '=') return false;
            index++;
        }

        return value.Length > 0;
    }

    static bool IsDigits(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9') return false;
            index++;
        }

        return value.Length > 0;
    }
}
