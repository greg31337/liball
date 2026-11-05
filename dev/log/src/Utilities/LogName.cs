namespace log;

internal class LogName
{
    internal const int MaximumFieldNameLength = 128;
    internal const int MaximumLoggerNameLength = 256;

    internal static bool IsValidFieldName(string? name)
    {
        return LogName.ValidateFieldName(name).Length == 0;
    }

    internal static string ValidateFieldName(string? name)
    {
        return LogName.ValidateDottedName(name, "field", LogName.MaximumFieldNameLength);
    }

    internal static bool IsValidLoggerName(string? name)
    {
        return LogName.ValidateLoggerName(name).Length == 0;
    }

    internal static string ValidateLoggerName(string? name)
    {
        return LogName.ValidateDottedName(name, "logger", LogName.MaximumLoggerNameLength);
    }

    internal static string NormalizeLoggerName(string? name)
    {
        if (name == null)
        {
            return "";
        }

        return name.Trim();
    }

    internal static string NormalizeTypeName(string? typeName)
    {
        string normalizedName = LogName.NormalizeLoggerName(typeName);
        if (normalizedName.StartsWith("global::", System.StringComparison.Ordinal))
        {
            normalizedName = normalizedName.Substring(8);
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < normalizedName.Length)
        {
            char character = normalizedName[index];
            if (character == ',')
            {
                break;
            }

            if (character == '`')
            {
                index = index + 1;
                while (index < normalizedName.Length && char.IsDigit(normalizedName[index]))
                {
                    index = index + 1;
                }

                continue;
            }

            if (character == '<')
            {
                index = LogName.SkipBalancedTypeText(normalizedName, index, '<', '>');
                continue;
            }

            if (character == '[')
            {
                index = LogName.SkipBalancedTypeText(normalizedName, index, '[', ']');
                continue;
            }

            if (character == ']' || character == '*' || character == '&')
            {
                index = index + 1;
                continue;
            }

            if (character == '.' || character == '+')
            {
                if (builder.Length > 0 && builder[builder.Length - 1] != '.')
                {
                    builder.Append('.');
                }

                index = index + 1;
                continue;
            }

            if (char.IsLetter(character) || character == '_')
            {
                builder.Append(character);
                index = index + 1;
                continue;
            }

            if (char.IsDigit(character))
            {
                if (builder.Length == 0 || builder[builder.Length - 1] == '.')
                {
                    builder.Append('_');
                }

                builder.Append(character);
                index = index + 1;
                continue;
            }

            if (builder.Length == 0 || builder[builder.Length - 1] != '_')
            {
                builder.Append('_');
            }

            index = index + 1;
        }

        if (builder.Length > 0 && builder[builder.Length - 1] == '.')
        {
            builder.Length = builder.Length - 1;
        }

        return builder.ToString();
    }

    internal static int SkipBalancedTypeText(string value, int startIndex, char openingCharacter, char closingCharacter)
    {
        int depth = 0;
        int index = startIndex;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == openingCharacter)
            {
                depth = depth + 1;
            }
            else if (character == closingCharacter)
            {
                depth = depth - 1;
            }

            index = index + 1;
            if (depth == 0)
            {
                return index;
            }
        }

        return index;
    }

    internal static string ValidateDottedName(string? name, string subject, int maximumLength)
    {
        string label = LogResult.NormalizeText(subject);
        if (label.Length == 0)
        {
            label = "name";
        }

        if (name == null)
        {
            return label + " name is required";
        }

        if (name.Length == 0)
        {
            return label + " name is required";
        }

        if (name.Length > maximumLength)
        {
            return label + " name is too long";
        }

        string trimmed = name.Trim();
        if (trimmed.Length != name.Length)
        {
            return label + " name cannot contain leading or trailing whitespace";
        }

        char first = name[0];
        if (!char.IsLetter(first) && first != '_')
        {
            return label + " name must start with a letter or underscore";
        }

        char previous = first;
        int index = 1;
        while (index < name.Length)
        {
            char current = name[index];
            if (!char.IsLetterOrDigit(current) && current != '_' && current != '.')
            {
                return label + " name can contain only letters, digits, underscores, and dots";
            }

            if (current == '.' && previous == '.')
            {
                return label + " name cannot contain empty segments";
            }

            previous = current;
            index = index + 1;
        }

        if (previous == '.')
        {
            return label + " name cannot contain empty segments";
        }

        return "";
    }
}
