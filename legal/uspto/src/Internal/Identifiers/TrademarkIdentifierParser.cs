namespace uspto;

internal class TrademarkIdentifierResult
{
    internal bool IsValid = false;
    internal string? FailureCode = null;
    internal TrademarkIdentifierKind Kind = TrademarkIdentifierKind.Unknown;
    internal string NormalizedInput = string.Empty;
    internal string CanonicalNumber = string.Empty;
    internal string DisplayNumber = string.Empty;
    internal string CountryCode = string.Empty;
    internal string ApiPrefix = string.Empty;
    internal string IdentifierValue = string.Empty;
    internal bool HasExplicitPrefix = false;
    internal bool WasPadded = false;
}

internal static class TrademarkIdentifierParser
{
    internal static TrademarkIdentifierResult ParseTrademarkIdentifier(string input)
    {
        TrademarkIdentifierResult result = new TrademarkIdentifierResult();
        string? normalizedInput = NormalizeTrademarkCharacters(input, result);
        if (normalizedInput == null)
        {
            return result;
        }

        result.NormalizedInput = normalizedInput;
        string prefix = string.Empty;
        string identifierValue = normalizedInput;
        if (normalizedInput.StartsWith("REF", global::System.StringComparison.Ordinal))
        {
            prefix = "ref";
            identifierValue = normalizedInput.Substring(3);
            result.HasExplicitPrefix = true;
        }
        else if (normalizedInput.StartsWith("SN", global::System.StringComparison.Ordinal))
        {
            prefix = "sn";
            identifierValue = normalizedInput.Substring(2);
            result.HasExplicitPrefix = true;
        }
        else if (normalizedInput.StartsWith("RN", global::System.StringComparison.Ordinal))
        {
            prefix = "rn";
            identifierValue = normalizedInput.Substring(2);
            result.HasExplicitPrefix = true;
        }
        else if (normalizedInput[0] >= 'A' && normalizedInput[0] <= 'Z')
        {
            result.FailureCode = "UNSUPPORTED_TRADEMARK_PREFIX";
            return result;
        }
        else if (normalizedInput.Length == 8)
        {
            prefix = "sn";
        }
        else if (normalizedInput.Length >= 1 && normalizedInput.Length <= 7)
        {
            prefix = "rn";
        }
        else
        {
            result.FailureCode = "INVALID_TRADEMARK_FORMAT";
            return result;
        }

        if (prefix == "sn")
        {
            ParseSerialNumber(identifierValue, result);
            return result;
        }

        if (prefix == "rn")
        {
            ParseRegistrationNumber(identifierValue, result);
            return result;
        }

        ParseReferenceNumber(identifierValue, result);
        return result;
    }

    internal static string? NormalizeTrademarkCharacters(string input, TrademarkIdentifierResult result)
    {
        if (input == null)
        {
            result.FailureCode = "REQUIRED";
            return null;
        }

        global::System.ReadOnlySpan<char> characters = global::System.MemoryExtensions.AsSpan(input);
        global::System.Text.StringBuilder normalized = new global::System.Text.StringBuilder(characters.Length);
        for (int index = 0; index < characters.Length; index += 1)
        {
            char character = characters[index];
            if (character >= '0' && character <= '9' || character >= 'A' && character <= 'Z')
            {
                normalized.Append(character);
            }
            else if (character >= 'a' && character <= 'z')
            {
                normalized.Append((char)(character - 'a' + 'A'));
            }
            else if (IsTrademarkSeparator(character))
            {
                continue;
            }
            else
            {
                result.FailureCode = "INVALID_CHARACTER";
                return null;
            }
        }

        if (normalized.Length == 0)
        {
            result.FailureCode = "EMPTY_IDENTIFIER";
            return null;
        }

        return normalized.ToString();
    }

    internal static void ParseSerialNumber(string identifierValue, TrademarkIdentifierResult result)
    {
        if (identifierValue.Length != 8 || !ContainsOnlyTrademarkDigits(identifierValue) || !PatentIdentifierParser.ContainsNonzeroDigit(identifierValue))
        {
            result.FailureCode = "INVALID_TRADEMARK_SERIAL";
            return;
        }

        result.Kind = TrademarkIdentifierKind.SerialNumber;
        result.CanonicalNumber = "sn" + identifierValue;
        result.DisplayNumber = "SN " + identifierValue.Substring(0, 2) + "/" + identifierValue.Substring(2, 3) + "," + identifierValue.Substring(5, 3);
        result.CountryCode = "US";
        result.ApiPrefix = "sn";
        result.IdentifierValue = identifierValue;
        result.IsValid = true;
    }

    internal static void ParseRegistrationNumber(string identifierValue, TrademarkIdentifierResult result)
    {
        if (identifierValue.Length == 0 || identifierValue.Length > 7 || !ContainsOnlyTrademarkDigits(identifierValue) || !PatentIdentifierParser.ContainsNonzeroDigit(identifierValue))
        {
            result.FailureCode = "INVALID_TRADEMARK_REGISTRATION";
            return;
        }

        string paddedValue = identifierValue;
        if (identifierValue.Length < 7)
        {
            paddedValue = new string('0', 7 - identifierValue.Length) + identifierValue;
        }

        result.Kind = TrademarkIdentifierKind.RegistrationNumber;
        result.CanonicalNumber = "rn" + paddedValue;
        result.DisplayNumber = "RN " + PatentIdentifierParser.FormatGrantDigits(paddedValue);
        result.CountryCode = "US";
        result.ApiPrefix = "rn";
        result.IdentifierValue = paddedValue;
        result.WasPadded = paddedValue.Length != identifierValue.Length;
        result.IsValid = true;
    }

    internal static void ParseReferenceNumber(string identifierValue, TrademarkIdentifierResult result)
    {
        if (identifierValue.Length != 8 || identifierValue[0] < 'A' || identifierValue[0] > 'Z')
        {
            result.FailureCode = "INVALID_TRADEMARK_REFERENCE";
            return;
        }

        string digits = identifierValue.Substring(1);
        if (!ContainsOnlyTrademarkDigits(digits) || !PatentIdentifierParser.ContainsNonzeroDigit(digits))
        {
            result.FailureCode = "INVALID_TRADEMARK_REFERENCE";
            return;
        }

        result.Kind = TrademarkIdentifierKind.ReferenceNumber;
        result.CanonicalNumber = "ref" + identifierValue;
        result.DisplayNumber = "REF " + identifierValue;
        result.CountryCode = "US";
        result.ApiPrefix = "ref";
        result.IdentifierValue = identifierValue;
        result.IsValid = true;
    }

    internal static bool ContainsOnlyTrademarkDigits(string value)
    {
        for (int index = 0; index < value.Length; index += 1)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsTrademarkSeparator(char character)
    {
        return global::System.Char.IsWhiteSpace(character) || character == '/' || character == ',' || character == '.' || character == '-' || character == ':' || character == '#';
    }
}
