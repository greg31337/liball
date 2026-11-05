namespace whois;

internal static class IdnNormalizer
{
    internal static WhoisQuery NormalizeDomain(string originalText)
    {
        InputGuard.ValidateQueryText(originalText, WhoisQueryKind.Domain);
        string preservedText = originalText;
        string candidate = originalText.Trim();

        if (candidate.Length > 0 && IsLabelSeparator(candidate[candidate.Length - 1]))
        {
            candidate = candidate.Substring(0, candidate.Length - 1);
        }

        if (candidate.Length == 0)
        {
            throw InputGuard.CreateQueryError("A domain must contain at least one label.", "QueryText", preservedText, WhoisQueryKind.Domain, null);
        }

        ValidateInputLabels(candidate, preservedText);

        System.Globalization.IdnMapping mapping = new System.Globalization.IdnMapping();
        mapping.AllowUnassigned = false;
        mapping.UseStd3AsciiRules = true;
        string ascii = mapping.GetAscii(candidate).ToLowerInvariant();

        if (ascii.Length > 253)
        {
            throw InputGuard.CreateQueryError("The normalized domain exceeds 253 ASCII characters.", "QueryText", preservedText, WhoisQueryKind.Domain, null);
        }

        ValidateAsciiLabels(ascii, preservedText);

        WhoisQuery query = new WhoisQuery();
        query.OriginalText = preservedText;
        query.NormalizedText = ascii;
        query.RoutingKey = ascii;
        query.Kind = WhoisQueryKind.Domain;
        query.IsValid = true;
        query.ValidationMessage = string.Empty;
        return query;
    }

    internal static void ValidateInputLabels(string candidate, string originalText)
    {
        if (candidate.Length > 253)
        {
            throw InputGuard.CreateQueryError("The domain exceeds 253 characters.", "QueryText", originalText, WhoisQueryKind.Domain, null);
        }

        int index = 0;
        int labelLength = 0;
        while (index < candidate.Length)
        {
            char value = candidate[index];
            if (IsLabelSeparator(value))
            {
                if (labelLength == 0)
                {
                    throw InputGuard.CreateQueryError("A domain must not contain an empty label.", "QueryText", originalText, WhoisQueryKind.Domain, null);
                }
                if (candidate[index - 1] == '-')
                {
                    throw InputGuard.CreateQueryError("A domain label must not end with a hyphen.", "QueryText", originalText, WhoisQueryKind.Domain, null);
                }
                labelLength = 0;
            }
            else
            {
                if (value == '\ufffd')
                {
                    throw InputGuard.CreateQueryError("A domain label contains an invalid Unicode replacement character.", "QueryText", originalText, WhoisQueryKind.Domain, null);
                }
                if (char.IsHighSurrogate(value))
                {
                    if (index + 1 >= candidate.Length || !char.IsLowSurrogate(candidate[index + 1]))
                    {
                        throw InputGuard.CreateQueryError("A domain label contains an unpaired Unicode surrogate.", "QueryText", originalText, WhoisQueryKind.Domain, null);
                    }
                }
                else if (char.IsLowSurrogate(value))
                {
                    if (index == 0 || !char.IsHighSurrogate(candidate[index - 1]))
                    {
                        throw InputGuard.CreateQueryError("A domain label contains an unpaired Unicode surrogate.", "QueryText", originalText, WhoisQueryKind.Domain, null);
                    }
                }
                if (labelLength == 0 && value == '-')
                {
                    throw InputGuard.CreateQueryError("A domain label must not begin with a hyphen.", "QueryText", originalText, WhoisQueryKind.Domain, null);
                }
                if (value < 128 && !IsAsciiLetterOrDigit(value) && value != '-')
                {
                    throw InputGuard.CreateQueryError("A domain label contains an invalid ASCII character.", "QueryText", originalText, WhoisQueryKind.Domain, null);
                }
                labelLength = labelLength + 1;
                if (labelLength > 63)
                {
                    throw InputGuard.CreateQueryError("A domain label exceeds 63 characters.", "QueryText", originalText, WhoisQueryKind.Domain, null);
                }
            }
            index = index + 1;
        }

        if (labelLength == 0)
        {
            throw InputGuard.CreateQueryError("A domain must not contain an empty label.", "QueryText", originalText, WhoisQueryKind.Domain, null);
        }
        if (candidate[candidate.Length - 1] == '-')
        {
            throw InputGuard.CreateQueryError("A domain label must not end with a hyphen.", "QueryText", originalText, WhoisQueryKind.Domain, null);
        }
    }

    internal static void ValidateAsciiLabels(string ascii, string originalText)
    {
        int index = 0;
        int labelLength = 0;
        while (index < ascii.Length)
        {
            if (ascii[index] == '.')
            {
                if (labelLength == 0)
                {
                    throw InputGuard.CreateQueryError("The normalized domain contains an empty label.", "QueryText", originalText, WhoisQueryKind.Domain, null);
                }
                labelLength = 0;
            }
            else
            {
                labelLength = labelLength + 1;
                if (labelLength > 63)
                {
                    throw InputGuard.CreateQueryError("A normalized domain label exceeds 63 ASCII characters.", "QueryText", originalText, WhoisQueryKind.Domain, null);
                }
            }
            index = index + 1;
        }

        if (labelLength == 0)
        {
            throw InputGuard.CreateQueryError("The normalized domain contains an empty label.", "QueryText", originalText, WhoisQueryKind.Domain, null);
        }
    }

    internal static bool IsLabelSeparator(char value)
    {
        return value == '.' || value == '\u3002' || value == '\uff0e' || value == '\uff61';
    }

    internal static bool IsAsciiLetterOrDigit(char value)
    {
        return value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z' || value >= '0' && value <= '9';
    }
}
