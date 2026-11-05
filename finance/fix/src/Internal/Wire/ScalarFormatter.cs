using System;
using System.Globalization;

namespace fix;

internal static class ScalarFormatter
{
    internal static ScalarFormatResult FormatInteger(int value, FixFieldType fieldType, Span<byte> destination)
    {
        ScalarFormatResult result = new ScalarFormatResult();
        if (fieldType != FixFieldType.Int && fieldType != FixFieldType.SeqNum && fieldType != FixFieldType.NumInGroup && fieldType != FixFieldType.Length && fieldType != FixFieldType.TagNum)
        {
            result.ErrorCode = "unsupported-field-type";
            return result;
        }
        if (fieldType != FixFieldType.Int && value < 0)
        {
            result.ErrorCode = "negative-value";
            return result;
        }
        if ((fieldType == FixFieldType.SeqNum || fieldType == FixFieldType.Length || fieldType == FixFieldType.TagNum) && value == 0)
        {
            result.ErrorCode = "zero-not-allowed";
            return result;
        }

        long magnitude = value;
        bool negative = magnitude < 0;
        if (negative)
        {
            magnitude = -magnitude;
        }
        int digits = 1;
        long remaining = magnitude;
        while (remaining >= 10L)
        {
            remaining = remaining / 10L;
            digits++;
        }
        int required = negative ? digits + 1 : digits;
        result.Required = required;
        if (destination.Length < required)
        {
            result.ErrorCode = "destination-too-small";
            return result;
        }

        remaining = magnitude;
        int index = required - 1;
        while (index >= (negative ? 1 : 0))
        {
            destination[index] = (byte)(48 + remaining % 10L);
            remaining = remaining / 10L;
            index--;
        }
        if (negative)
        {
            destination[0] = 45;
        }
        result.Success = true;
        result.Written = required;
        return result;
    }

    internal static ScalarFormatResult FormatDecimal(decimal value, FixFieldType fieldType, Span<byte> destination)
    {
        ScalarFormatResult result = new ScalarFormatResult();
        if (fieldType != FixFieldType.Float && fieldType != FixFieldType.Qty && fieldType != FixFieldType.Price && fieldType != FixFieldType.PriceOffset && fieldType != FixFieldType.Amt && fieldType != FixFieldType.Percentage)
        {
            result.ErrorCode = "unsupported-field-type";
            return result;
        }
        string text = value.ToString("0.############################", CultureInfo.InvariantCulture);
        result.Required = text.Length;
        if (destination.Length < text.Length)
        {
            result.ErrorCode = "destination-too-small";
            return result;
        }
        for (int index = 0; index < text.Length; index++)
        {
            destination[index] = (byte)text[index];
        }
        result.Success = true;
        result.Written = text.Length;
        return result;
    }

    internal static ScalarFormatResult FormatCharacter(char value, FixFieldType fieldType, Span<byte> destination)
    {
        ScalarFormatResult result = new ScalarFormatResult();
        result.Required = 1;
        if (fieldType != FixFieldType.Char)
        {
            result.ErrorCode = "unsupported-field-type";
            return result;
        }
        if (value > 255 || value == (char)FixDefaults.Soh)
        {
            result.ErrorCode = "invalid-character";
            return result;
        }
        if (destination.Length < 1)
        {
            result.ErrorCode = "destination-too-small";
            return result;
        }
        destination[0] = (byte)value;
        result.Success = true;
        result.Written = 1;
        return result;
    }

    internal static ScalarFormatResult FormatBoolean(bool value, Span<byte> destination)
    {
        ScalarFormatResult result = new ScalarFormatResult();
        result.Required = 1;
        if (destination.Length < 1)
        {
            result.ErrorCode = "destination-too-small";
            return result;
        }
        destination[0] = value ? (byte)89 : (byte)78;
        result.Success = true;
        result.Written = 1;
        return result;
    }

    internal static ScalarFormatResult FormatText(string value, FixFieldType fieldType, Span<byte> destination)
    {
        ScalarFormatResult result = new ScalarFormatResult();
        if (fieldType != FixFieldType.String && fieldType != FixFieldType.Country && fieldType != FixFieldType.Currency && fieldType != FixFieldType.Exchange)
        {
            result.ErrorCode = "unsupported-field-type";
            return result;
        }
        if (value.Length == 0)
        {
            result.Required = 1;
            result.ErrorCode = "empty-value";
            return result;
        }
        if (fieldType == FixFieldType.Country && value.Length != 2 || fieldType == FixFieldType.Currency && value.Length != 3)
        {
            result.ErrorCode = "invalid-length";
            result.Required = fieldType == FixFieldType.Country ? 2 : 3;
            return result;
        }
        result.Required = value.Length;
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] > 255 || value[index] == (char)FixDefaults.Soh)
            {
                result.ErrorCode = "invalid-character";
                return result;
            }
        }
        if (destination.Length < value.Length)
        {
            result.ErrorCode = "destination-too-small";
            return result;
        }
        for (int index = 0; index < value.Length; index++)
        {
            destination[index] = (byte)value[index];
        }
        result.Success = true;
        result.Written = value.Length;
        return result;
    }

    internal static ScalarFormatResult FormatMultipleValues(string[] values, FixFieldType fieldType, Span<byte> destination)
    {
        ScalarFormatResult result = new ScalarFormatResult();
        if (fieldType != FixFieldType.MultipleCharValue && fieldType != FixFieldType.MultipleStringValue)
        {
            result.ErrorCode = "unsupported-field-type";
            return result;
        }
        if (values.Length == 0)
        {
            result.Required = 1;
            result.ErrorCode = "empty-value";
            return result;
        }

        int required = values.Length - 1;
        for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
        {
            string value = values[valueIndex];
            if (value.Length == 0)
            {
                result.ErrorCode = "empty-multivalue-token";
                return result;
            }
            if (fieldType == FixFieldType.MultipleCharValue && (value.Length != 1 || value[0] > 127 || !ScalarParser.IsAsciiAlphaNumeric((byte)value[0])))
            {
                result.ErrorCode = "invalid-multiple-char-value";
                return result;
            }
            for (int index = 0; index < value.Length; index++)
            {
                if (value[index] > 255 || value[index] == (char)FixDefaults.Soh || value[index] == 32)
                {
                    result.ErrorCode = "invalid-multivalue-token";
                    return result;
                }
            }
            if (required > int.MaxValue - value.Length)
            {
                result.ErrorCode = "length-overflow";
                return result;
            }
            required += value.Length;
        }
        result.Required = required;
        if (destination.Length < required)
        {
            result.ErrorCode = "destination-too-small";
            return result;
        }

        int written = 0;
        for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
        {
            if (valueIndex > 0)
            {
                destination[written] = 32;
                written++;
            }
            string value = values[valueIndex];
            for (int index = 0; index < value.Length; index++)
            {
                destination[written] = (byte)value[index];
                written++;
            }
        }
        result.Success = true;
        result.Written = written;
        return result;
    }
}
