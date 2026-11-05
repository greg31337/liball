namespace log;

internal class LogTemplateToken
{
    internal bool IsLiteral;
    internal bool IsPlaceholder;
    internal string Text;
    internal string Name;
    internal string Format;
    internal bool HasFormat;
    internal bool IsDestructured;
    internal bool IsStringified;
    internal int Position;
    internal int StartIndex;
    internal int Length;
    internal bool IsValid;
    internal string ErrorMessage;

    internal LogTemplateToken(bool isLiteral, bool isPlaceholder, string text, string name, string format, bool isDestructured, bool isStringified, int position, int startIndex, int length)
    {
        IsLiteral = isLiteral;
        IsPlaceholder = isPlaceholder;
        Text = LogResult.NormalizeText(text);
        Name = LogResult.NormalizeText(name);
        Format = LogResult.NormalizeText(format);
        HasFormat = Format.Length > 0;
        IsDestructured = isDestructured;
        IsStringified = isStringified;
        Position = position;
        StartIndex = LogEvent.NormalizeNonNegative(startIndex);
        Length = LogEvent.NormalizeNonNegative(length);
        IsValid = true;
        ErrorMessage = "";

        Validate();
    }

    internal static LogTemplateToken Literal(string text, int startIndex)
    {
        string normalizedText = LogResult.NormalizeText(text);
        return new LogTemplateToken(true, false, normalizedText, "", "", false, false, -1, startIndex, normalizedText.Length);
    }

    internal static LogTemplateToken LiteralSpan(string text, int startIndex, int length)
    {
        string normalizedText = LogResult.NormalizeText(text);
        return new LogTemplateToken(true, false, normalizedText, "", "", false, false, -1, startIndex, length);
    }

    internal static LogTemplateToken Placeholder(string name, int position, int startIndex, int length)
    {
        return new LogTemplateToken(false, true, "", name, "", false, false, position, startIndex, length);
    }

    internal static LogTemplateToken PlaceholderFormat(string name, string format, bool isDestructured, bool isStringified, int position, int startIndex, int length)
    {
        return new LogTemplateToken(false, true, "", name, format, isDestructured, isStringified, position, startIndex, length);
    }

    internal static LogTemplateToken Copy(LogTemplateToken token)
    {
        if (token == null)
        {
            return new LogTemplateToken(false, false, "", "", "", false, false, -1, 0, 0);
        }

        return new LogTemplateToken(token.IsLiteral, token.IsPlaceholder, token.Text, token.Name, token.Format, token.IsDestructured, token.IsStringified, token.Position, token.StartIndex, token.Length);
    }

    internal void Validate()
    {
        if (IsLiteral && IsPlaceholder)
        {
            IsValid = false;
            ErrorMessage = "template token cannot be both literal and placeholder";
            return;
        }

        if (!IsLiteral && !IsPlaceholder)
        {
            IsValid = false;
            ErrorMessage = "template token kind is required";
            return;
        }

        if (IsLiteral)
        {
            return;
        }

        if (IsDestructured && IsStringified)
        {
            IsValid = false;
            ErrorMessage = "placeholder cannot be both destructured and stringified";
            return;
        }

        string nameError = LogName.ValidateFieldName(Name);
        if (nameError.Length > 0)
        {
            IsValid = false;
            ErrorMessage = nameError;
            return;
        }
    }
}
