using System;
using System.Text;

namespace fix;

internal static class ScalarParser
{
    internal static ScalarParseResult ParseScalar(ReadOnlySpan<byte> source, FixFieldType fieldType, int maximumLength)
    {
        LimitCheckResult limit = WireLimit.CheckValue(source.Length, maximumLength);
        if (!limit.Allowed)
        {
            ScalarParseResult limited = new ScalarParseResult();
            limited.FieldType = fieldType;
            limited.ErrorCode = limit.ErrorCode;
            return limited;
        }
        if (fieldType == FixFieldType.Int || fieldType == FixFieldType.SeqNum || fieldType == FixFieldType.NumInGroup || fieldType == FixFieldType.Length || fieldType == FixFieldType.TagNum)
        {
            return ParseIntegerValue(source, fieldType);
        }
        if (fieldType == FixFieldType.Float || fieldType == FixFieldType.Qty || fieldType == FixFieldType.Price || fieldType == FixFieldType.PriceOffset || fieldType == FixFieldType.Amt || fieldType == FixFieldType.Percentage)
        {
            return ParseDecimalValue(source, fieldType);
        }
        if (fieldType == FixFieldType.Char)
        {
            return ParseCharacterValue(source, fieldType);
        }
        if (fieldType == FixFieldType.Boolean)
        {
            return ParseBooleanValue(source, fieldType);
        }
        if (fieldType == FixFieldType.String || fieldType == FixFieldType.Country || fieldType == FixFieldType.Currency || fieldType == FixFieldType.Exchange)
        {
            return ParseTextValue(source, fieldType);
        }
        if (fieldType == FixFieldType.MultipleCharValue || fieldType == FixFieldType.MultipleStringValue)
        {
            return ParseMultipleValue(source, fieldType);
        }

        ScalarParseResult unsupported = new ScalarParseResult();
        unsupported.FieldType = fieldType;
        unsupported.ErrorCode = "unsupported-field-type";
        return unsupported;
    }

    internal static ScalarParseResult ParseIntegerValue(ReadOnlySpan<byte> source, FixFieldType fieldType)
    {
        ScalarParseResult result = new ScalarParseResult();
        result.FieldType = fieldType;
        if (source.Length == 0)
        {
            result.Required = 1;
            result.ErrorIndex = 0;
            result.ErrorCode = "incomplete";
            return result;
        }

        bool negative = source[0] == 45;
        int start = negative ? 1 : 0;
        if (start == source.Length)
        {
            result.Required = source.Length + 1;
            result.ErrorIndex = start;
            result.ErrorCode = "incomplete";
            return result;
        }
        if (fieldType != FixFieldType.Int && negative)
        {
            result.ErrorIndex = 0;
            result.ErrorCode = "negative-value";
            return result;
        }
        if (fieldType == FixFieldType.TagNum && source.Length - start > 1 && source[start] == 48)
        {
            result.ErrorIndex = start;
            result.ErrorCode = "leading-zero";
            return result;
        }

        long maximum = negative ? 2147483648L : int.MaxValue;
        long value = 0;
        for (int index = start; index < source.Length; index++)
        {
            byte current = source[index];
            if (!AsciiDigits.IsDigit(current))
            {
                result.ErrorIndex = index;
                result.ErrorCode = "invalid-digit";
                return result;
            }
            int digit = current - 48;
            if (value > (maximum - digit) / 10L)
            {
                result.ErrorIndex = index;
                result.ErrorCode = "overflow";
                return result;
            }
            value = value * 10L + digit;
        }

        int parsedValue;
        if (negative && value == 2147483648L)
        {
            parsedValue = int.MinValue;
        }
        else
        {
            parsedValue = (int)value;
            if (negative)
            {
                parsedValue = -parsedValue;
            }
        }
        if ((fieldType == FixFieldType.SeqNum || fieldType == FixFieldType.Length || fieldType == FixFieldType.TagNum) && parsedValue == 0)
        {
            result.ErrorIndex = start;
            result.ErrorCode = "zero-not-allowed";
            return result;
        }

        result.Success = true;
        result.IntegerValue = parsedValue;
        result.Consumed = source.Length;
        result.Required = source.Length;
        return result;
    }

    internal static ScalarParseResult ParseDecimalValue(ReadOnlySpan<byte> source, FixFieldType fieldType)
    {
        ScalarParseResult result = new ScalarParseResult();
        result.FieldType = fieldType;
        if (source.Length == 0)
        {
            result.Required = 1;
            result.ErrorIndex = 0;
            result.ErrorCode = "incomplete";
            return result;
        }

        bool negative = source[0] == 45;
        int start = negative ? 1 : 0;
        if (start == source.Length)
        {
            result.Required = source.Length + 1;
            result.ErrorIndex = start;
            result.ErrorCode = "incomplete";
            return result;
        }

        decimal value = 0m;
        bool decimalPoint = false;
        int fractionalDigits = 0;
        int digits = 0;
        for (int index = start; index < source.Length; index++)
        {
            byte current = source[index];
            if (current == 46)
            {
                if (decimalPoint || digits == 0)
                {
                    result.ErrorIndex = index;
                    result.ErrorCode = "invalid-decimal-point";
                    return result;
                }
                decimalPoint = true;
                continue;
            }
            if (!AsciiDigits.IsDigit(current))
            {
                result.ErrorIndex = index;
                result.ErrorCode = "invalid-digit";
                return result;
            }
            int digit = current - 48;
            if (value > (decimal.MaxValue - digit) / 10m)
            {
                result.ErrorIndex = index;
                result.ErrorCode = "overflow";
                return result;
            }
            value = value * 10m + digit;
            digits++;
            if (decimalPoint)
            {
                fractionalDigits++;
                if (fractionalDigits > 28)
                {
                    result.ErrorIndex = index;
                    result.ErrorCode = "precision-exceeded";
                    return result;
                }
            }
        }

        for (int index = 0; index < fractionalDigits; index++)
        {
            value = value / 10m;
        }
        if (negative)
        {
            value = -value;
        }
        result.Success = true;
        result.DecimalValue = value;
        result.FractionalDigits = fractionalDigits;
        result.Consumed = source.Length;
        result.Required = source.Length;
        return result;
    }

    internal static ScalarParseResult ParseCharacterValue(ReadOnlySpan<byte> source, FixFieldType fieldType)
    {
        ScalarParseResult result = new ScalarParseResult();
        result.FieldType = fieldType;
        if (source.Length != 1)
        {
            result.Required = 1;
            result.ErrorIndex = source.Length == 0 ? 0 : 1;
            result.ErrorCode = source.Length == 0 ? "incomplete" : "invalid-length";
            return result;
        }
        if (source[0] == FixDefaults.Soh)
        {
            result.ErrorIndex = 0;
            result.ErrorCode = "delimiter-not-allowed";
            return result;
        }
        result.Success = true;
        result.CharacterValue = (char)source[0];
        result.Consumed = 1;
        result.Required = 1;
        return result;
    }

    internal static ScalarParseResult ParseBooleanValue(ReadOnlySpan<byte> source, FixFieldType fieldType)
    {
        ScalarParseResult result = new ScalarParseResult();
        result.FieldType = fieldType;
        if (source.Length != 1)
        {
            result.Required = 1;
            result.ErrorIndex = source.Length == 0 ? 0 : 1;
            result.ErrorCode = source.Length == 0 ? "incomplete" : "invalid-length";
            return result;
        }
        if (source[0] != 89 && source[0] != 78)
        {
            result.ErrorIndex = 0;
            result.ErrorCode = "invalid-boolean";
            return result;
        }
        result.Success = true;
        result.CharacterValue = (char)source[0];
        result.BooleanValue = source[0] == 89;
        result.Consumed = 1;
        result.Required = 1;
        return result;
    }

    internal static ScalarParseResult ParseTextValue(ReadOnlySpan<byte> source, FixFieldType fieldType)
    {
        ScalarParseResult result = new ScalarParseResult();
        result.FieldType = fieldType;
        if (source.Length == 0)
        {
            result.Required = 1;
            result.ErrorIndex = 0;
            result.ErrorCode = "incomplete";
            return result;
        }
        int delimiter = ByteSearch.IndexOfByte(source, FixDefaults.Soh);
        if (delimiter >= 0)
        {
            result.ErrorIndex = delimiter;
            result.ErrorCode = "delimiter-not-allowed";
            return result;
        }
        if (fieldType == FixFieldType.Country && source.Length != 2)
        {
            result.Required = 2;
            result.ErrorCode = "invalid-length";
            return result;
        }
        if (fieldType == FixFieldType.Currency && source.Length != 3)
        {
            result.Required = 3;
            result.ErrorCode = "invalid-length";
            return result;
        }
        result.Success = true;
        result.TextValue = Encoding.Latin1.GetString(source);
        result.Consumed = source.Length;
        result.Required = source.Length;
        return result;
    }

    internal static ScalarParseResult ParseMultipleValue(ReadOnlySpan<byte> source, FixFieldType fieldType)
    {
        ScalarParseResult result = new ScalarParseResult();
        result.FieldType = fieldType;
        if (source.Length == 0)
        {
            result.Required = 1;
            result.ErrorIndex = 0;
            result.ErrorCode = "incomplete";
            return result;
        }

        int tokenCount = 1;
        for (int index = 0; index < source.Length; index++)
        {
            byte current = source[index];
            if (current == FixDefaults.Soh)
            {
                result.ErrorIndex = index;
                result.ErrorCode = "delimiter-not-allowed";
                return result;
            }
            if (current == 32)
            {
                if (index == 0 || index == source.Length - 1 || source[index - 1] == 32)
                {
                    result.ErrorIndex = index;
                    result.ErrorCode = "empty-multivalue-token";
                    return result;
                }
                tokenCount++;
            }
        }

        string[] values = new string[tokenCount];
        int tokenStart = 0;
        int tokenIndex = 0;
        for (int index = 0; index <= source.Length; index++)
        {
            if (index == source.Length || source[index] == 32)
            {
                int tokenLength = index - tokenStart;
                if (fieldType == FixFieldType.MultipleCharValue)
                {
                    if (tokenLength != 1 || !IsAsciiAlphaNumeric(source[tokenStart]))
                    {
                        result.ErrorIndex = tokenStart;
                        result.ErrorCode = "invalid-multiple-char-value";
                        return result;
                    }
                }
                values[tokenIndex] = Encoding.Latin1.GetString(source.Slice(tokenStart, tokenLength));
                tokenIndex++;
                tokenStart = index + 1;
            }
        }

        result.Success = true;
        result.TextValue = Encoding.Latin1.GetString(source);
        result.Values = values;
        result.Consumed = source.Length;
        result.Required = source.Length;
        return result;
    }

    internal static bool IsAsciiAlphaNumeric(byte value)
    {
        return value >= 48 && value <= 57 || value >= 65 && value <= 90 || value >= 97 && value <= 122;
    }
}
