using System;
using System.Collections.Generic;

namespace email;
internal class EmailDispositionNotificationValueParser
{
    internal static EmailDispositionNotificationUserAgent? ParseUserAgent(EmailHeader? field)
    {
        string? text = GetText(field, true);
        if (text == null) return null;
        int separator = text.IndexOf(';');
        string name = separator < 0 ? text.Trim() : text.Substring(0, separator).Trim();
        string? product = separator < 0 ? null : text.Substring(separator + 1).Trim();
        if (name.Length == 0) return null;
        return new EmailDispositionNotificationUserAgent(name, product);
    }

    internal static EmailDispositionNotificationTypedValue? ParseTyped(EmailHeader? field)
    {
        string? text = GetText(field, true);
        if (text == null) return null;
        int separator = text.IndexOf(';');
        if (separator <= 0 || separator >= text.Length - 1) return null;
        string type = text.Substring(0, separator).Trim();
        string value = text.Substring(separator + 1).Trim();
        if (!IsToken(type) || value.Length == 0) return null;
        return new EmailDispositionNotificationTypedValue(type, value);
    }

    internal static EmailMessageId? ParseMessageId(EmailHeader? field)
    {
        if (field == null) return null;
        EmailMessageIdParseResult parsed = new EmailMessageIdParser().ParseMessageIdWithInputAndMode(field.GetUnfoldedValue(), EmailMessageIdParseMode.Strict);
        return parsed.IsSuccess() && parsed.IsComplete() && parsed.HasSingleIdentifier() ? parsed.GetIdentifier() : null;
    }

    internal static EmailDispositionNotificationDisposition? ParseDisposition(EmailHeader? field)
    {
        string? text = GetText(field, true);
        if (text == null) return null;
        int semicolon = text.IndexOf(';');
        if (semicolon <= 0 || semicolon >= text.Length - 1 || text.IndexOf(';', semicolon + 1) >= 0) return null;
        string modeText = text.Substring(0, semicolon).Trim();
        string dispositionText = text.Substring(semicolon + 1).Trim();
        int modeSlash = modeText.IndexOf('/');
        if (modeSlash <= 0 || modeSlash >= modeText.Length - 1 || modeText.IndexOf('/', modeSlash + 1) >= 0) return null;
        EmailDispositionActionMode action = ParseActionMode(modeText.Substring(0, modeSlash).Trim());
        EmailDispositionSendingMode sending = ParseSendingMode(modeText.Substring(modeSlash + 1).Trim());
        if (action == EmailDispositionActionMode.Unknown || sending == EmailDispositionSendingMode.Unknown) return null;

        int typeSlash = dispositionText.IndexOf('/');
        string typeText = typeSlash < 0 ? dispositionText.Trim() : dispositionText.Substring(0, typeSlash).Trim();
        EmailDispositionNotificationType type = ParseType(typeText);
        if (type == EmailDispositionNotificationType.Unknown) return null;
        List<string> modifiers = new List<string>();
        if (typeSlash >= 0)
        {
            string modifierText = dispositionText.Substring(typeSlash + 1);
            if (modifierText.IndexOf('/') >= 0) return null;
            string[] values = modifierText.Split(',');
            int index = 0;
            while (index < values.Length)
            {
                string modifier = values[index].Trim();
                if (!IsToken(modifier)) return null;
                modifiers.Add(modifier);
                index++;
            }
        }

        return new EmailDispositionNotificationDisposition(action, sending, type, modifiers);
    }

    internal static List<string> GetTextValues(EmailReportFieldBlock? block, string name)
    {
        List<EmailHeader> fields = (block ?? throw new InvalidOperationException()).GetFieldsNamed(name);
        List<string> result = new List<string>();
        int index = 0;
        while (index < fields.Count)
        {
            result.Add((fields[index].GetDecodedValue() ?? throw new InvalidOperationException()).Trim());
            index++;
        }

        return result;
    }

    internal static List<EmailHeader> GetExtensions(EmailReportFieldBlock? block)
    {
        string[] known = new string[] { "Reporting-UA", "MDN-Gateway", "Original-Recipient", "Final-Recipient", "Original-Message-ID", "Disposition", "Failure", "Error", "Warning" };
        List<EmailHeader> result = new List<EmailHeader>();
        int index = 0;
        while (index < (block ?? throw new InvalidOperationException()).GetFieldCount())
        {
            EmailHeader field = block.GetFieldAt(index);
            if (!Contains(known, field.GetName())) result.Add(field);
            index++;
        }

        return result;
    }

    internal static string? TypeName(EmailDispositionNotificationType value)
    {
        if (value == EmailDispositionNotificationType.Displayed) return "displayed";
        if (value == EmailDispositionNotificationType.Deleted) return "deleted";
        if (value == EmailDispositionNotificationType.Dispatched) return "dispatched";
        if (value == EmailDispositionNotificationType.Processed) return "processed";
        if (value == EmailDispositionNotificationType.Denied) return "denied";
        if (value == EmailDispositionNotificationType.Failed) return "failed";
        return null;
    }

    internal static bool IsToken(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == '(' || current == ')' || current == '<' || current == '>' || current == '@' || current == ',' || current == ';' || current == ':' || current == '\\' || current == '"' || current == '/' || current == '[' || current == ']' || current == '?' || current == '=') return false;
            index++;
        }

        return true;
    }

    static EmailDispositionActionMode ParseActionMode(string value)
    {
        if (string.Equals(value, "manual-action", StringComparison.OrdinalIgnoreCase)) return EmailDispositionActionMode.Manual;
        if (string.Equals(value, "automatic-action", StringComparison.OrdinalIgnoreCase)) return EmailDispositionActionMode.Automatic;
        return EmailDispositionActionMode.Unknown;
    }

    static EmailDispositionSendingMode ParseSendingMode(string value)
    {
        if (string.Equals(value, "MDN-sent-manually", StringComparison.OrdinalIgnoreCase)) return EmailDispositionSendingMode.Manual;
        if (string.Equals(value, "MDN-sent-automatically", StringComparison.OrdinalIgnoreCase)) return EmailDispositionSendingMode.Automatic;
        return EmailDispositionSendingMode.Unknown;
    }

    static EmailDispositionNotificationType ParseType(string value)
    {
        if (string.Equals(value, "displayed", StringComparison.OrdinalIgnoreCase)) return EmailDispositionNotificationType.Displayed;
        if (string.Equals(value, "deleted", StringComparison.OrdinalIgnoreCase)) return EmailDispositionNotificationType.Deleted;
        if (string.Equals(value, "dispatched", StringComparison.OrdinalIgnoreCase)) return EmailDispositionNotificationType.Dispatched;
        if (string.Equals(value, "processed", StringComparison.OrdinalIgnoreCase)) return EmailDispositionNotificationType.Processed;
        if (string.Equals(value, "denied", StringComparison.OrdinalIgnoreCase)) return EmailDispositionNotificationType.Denied;
        if (string.Equals(value, "failed", StringComparison.OrdinalIgnoreCase)) return EmailDispositionNotificationType.Failed;
        return EmailDispositionNotificationType.Unknown;
    }

    static string? GetText(EmailHeader? field, bool emptyIsNull)
    {
        if (field == null) return null;
        string value = (field.GetDecodedValue() ?? throw new InvalidOperationException()).Trim();
        return emptyIsNull && value.Length == 0 ? null : value;
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
}
