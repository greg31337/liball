using System;
using System.Collections.Generic;

namespace email;
internal class EmailDispositionNotificationRequestValueParser
{
    internal static List<EmailDispositionNotificationOption>? ParseOptions(EmailHeader field)
    {
        if (field == null) return new List<EmailDispositionNotificationOption>();
        List<string>? parameters = Split(EmailDispositionNotificationValueParserText(field), ';');
        if (parameters == null || parameters.Count == 0) return null;
        List<EmailDispositionNotificationOption> result = new List<EmailDispositionNotificationOption>();
        int index = 0;
        while (index < parameters.Count)
        {
            string parameter = parameters[index].Trim();
            int equals = FindUnquotedWithValueAndExpected(parameter, '=');
            if (equals <= 0 || equals >= parameter.Length - 1 || FindUnquotedWithValueAndExpectedAndStart(parameter, '=', equals + 1) >= 0) return null;
            string attribute = parameter.Substring(0, equals).Trim();
            if (!IsAtom(attribute)) return null;
            List<string>? components = Split(parameter.Substring(equals + 1), ',');
            if (components == null || components.Count < 2) return null;
            string importanceText = components[0].Trim();
            EmailDispositionNotificationOptionImportance importance;
            if (string.Equals(importanceText, "required", StringComparison.OrdinalIgnoreCase)) importance = EmailDispositionNotificationOptionImportance.Required;
            else if (string.Equals(importanceText, "optional", StringComparison.OrdinalIgnoreCase)) importance = EmailDispositionNotificationOptionImportance.Optional;
            else return null;
            List<string> values = new List<string>();
            int valueIndex = 1;
            while (valueIndex < components.Count)
            {
                string? value = ParseWord(components[valueIndex].Trim());
                if (value == null) return null;
                values.Add(value);
                valueIndex++;
            }

            result.Add(new EmailDispositionNotificationOption(attribute, importance, values));
            index++;
        }

        return result;
    }

    internal static string FormatOptions(List<EmailDispositionNotificationOption> options)
    {
        List<string> parameters = new List<string>();
        int index = 0;
        while (index < options.Count)
        {
            EmailDispositionNotificationOption option = options[index];
            string value = option.GetAttribute() + "=" + (option.GetImportance() == EmailDispositionNotificationOptionImportance.Required ? "required" : "optional");
            int valueIndex = 0;
            while (valueIndex < option.GetValueCount())
            {
                value += "," + FormatWord(option.GetValueAt(valueIndex));
                valueIndex++;
            }

            parameters.Add(value);
            index++;
        }

        return string.Join("; ", parameters);
    }

    internal static bool IsAtom(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool alphaNumeric = current >= 'A' && current <= 'Z' || current >= 'a' && current <= 'z' || current >= '0' && current <= '9';
            if (!alphaNumeric && "!#$%&'*+-/=?^_`{|}~".IndexOf(current) < 0) return false;
            index++;
        }

        return true;
    }

    internal static bool IsAsciiWordValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 32 || current > 126) return false;
            index++;
        }

        return value.Length > 0;
    }

    static string EmailDispositionNotificationValueParserText(EmailHeader field)
    {
        return (field.GetDecodedValue() ?? throw new InvalidOperationException()).Trim();
    }

    static string? ParseWord(string value)
    {
        if (IsAtom(value)) return value;
        if (value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"') return null;
        string result = "";
        int index = 1;
        while (index < value.Length - 1)
        {
            char current = value[index];
            if (current == '\\')
            {
                index++;
                if (index >= value.Length - 1) return null;
                current = value[index];
            }

            if (current < 32 || current > 126 || current == 13 || current == 10) return null;
            result += current;
            index++;
        }

        return result.Length == 0 ? null : result;
    }

    static string FormatWord(string value)
    {
        if (IsAtom(value)) return value;
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    static List<string>? Split(string value, char delimiter)
    {
        List<string> result = new List<string>();
        bool quoted = false;
        bool escaped = false;
        int start = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (escaped) escaped = false;
            else if (quoted && current == '\\') escaped = true;
            else if (current == '"') quoted = !quoted;
            else if (!quoted && current == delimiter)
            {
                result.Add(value.Substring(start, index - start));
                start = index + 1;
            }

            index++;
        }

        if (quoted || escaped) return null;
        result.Add(value.Substring(start));
        return result;
    }

    static int FindUnquotedWithValueAndExpected(string value, char expected)
    {
        return FindUnquotedWithValueAndExpectedAndStart(value, expected, 0);
    }

    static int FindUnquotedWithValueAndExpectedAndStart(string value, char expected, int start)
    {
        bool quoted = false;
        bool escaped = false;
        int index = start;
        while (index < value.Length)
        {
            char current = value[index];
            if (escaped) escaped = false;
            else if (quoted && current == '\\') escaped = true;
            else if (current == '"') quoted = !quoted;
            else if (!quoted && current == expected) return index;
            index++;
        }

        return -1;
    }
}
