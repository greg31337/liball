namespace qrcode;

internal class KanjiEncoder
{
    internal const int FirstRangeMinimum = 0x8140;
    internal const int FirstRangeMaximum = 0x9FFC;
    internal const int SecondRangeMinimum = 0xE040;
    internal const int SecondRangeMaximum = 0xEBBF;

    internal static bool IsPermittedShiftJisValue(int shiftJisValue)
    {
        return shiftJisValue >= FirstRangeMinimum && shiftJisValue <= FirstRangeMaximum
            || shiftJisValue >= SecondRangeMinimum && shiftJisValue <= SecondRangeMaximum;
    }

    internal static bool IsEncodableCharacter(char character)
    {
        int shiftJisValue = ShiftJisTable.GetStandardEncodedValue(character);
        return IsPermittedShiftJisValue(shiftJisValue);
    }

    internal static int GetShiftJisValue(char character)
    {
        int shiftJisValue = ShiftJisTable.GetStandardEncodedValue(character);
        if (!IsPermittedShiftJisValue(shiftJisValue))
        {
            throw new QrInputException(
                "Kanji mode accepts only characters assigned in the permitted double-byte Shift JIS ranges.",
                "character");
        }

        return shiftJisValue;
    }

    internal static int TransformShiftJisValue(int shiftJisValue)
    {
        int adjustedValue;
        if (shiftJisValue >= FirstRangeMinimum && shiftJisValue <= FirstRangeMaximum)
        {
            adjustedValue = shiftJisValue - FirstRangeMinimum;
        }
        else if (shiftJisValue >= SecondRangeMinimum && shiftJisValue <= SecondRangeMaximum)
        {
            adjustedValue = shiftJisValue - 0xC140;
        }
        else
        {
            throw new QrInputException(
                "The Shift JIS value is outside the ranges permitted by QR Kanji mode.",
                "shiftJisValue");
        }

        return (adjustedValue >> 8) * 0xC0 + (adjustedValue & 0xFF);
    }

    internal static int CalculateBitLength(int characterCount)
    {
        if (characterCount < 0)
        {
            throw new QrInputException("The character count cannot be negative.", "characterCount");
        }

        return CheckedDimensions.CalculateProduct(
            characterCount,
            13,
            int.MaxValue,
            "KanjiBitLength");
    }

    internal static void ValidateText(string text)
    {
        if (text == null)
        {
            throw new QrInputException("The Kanji text cannot be null.", "text");
        }

        if (text.Length > QrLimits.DefaultMaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The Kanji text exceeds the configured input limit.",
                "MaximumInputBytes",
                text.Length,
                QrLimits.DefaultMaximumInputBytes);
        }

        for (int index = 0; index < text.Length; index++)
        {
            GetShiftJisValue(text[index]);
        }
    }

    internal static QrEncodingMode SelectEncodingMode(string text, bool allowByteFallback)
    {
        if (text == null)
        {
            throw new QrInputException("The Kanji text cannot be null.", "text");
        }

        if (text.Length > QrLimits.DefaultMaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The text exceeds the configured input limit.",
                "MaximumInputBytes",
                text.Length,
                QrLimits.DefaultMaximumInputBytes);
        }

        for (int index = 0; index < text.Length; index++)
        {
            if (!IsEncodableCharacter(text[index]))
            {
                if (allowByteFallback)
                {
                    return QrEncodingMode.Byte;
                }

                throw new QrInputException(
                    "The text contains a character that cannot be represented in QR Kanji mode.",
                    "text");
            }
        }

        return QrEncodingMode.Kanji;
    }

    internal static void EncodeText(string text, BitBuffer destination)
    {
        if (destination == null)
        {
            throw new QrInputException("The destination bit buffer cannot be null.", "destination");
        }

        ValidateText(text);
        int encodedBitLength = CalculateBitLength(text.Length);
        int requiredBitLength = CheckedDimensions.CalculateSum(
            destination.GetBitLength(),
            encodedBitLength,
            int.MaxValue,
            "BitLength");
        destination.EnsureBitCapacity(requiredBitLength);

        for (int index = 0; index < text.Length; index++)
        {
            int shiftJisValue = GetShiftJisValue(text[index]);
            int transformedValue = TransformShiftJisValue(shiftJisValue);
            destination.AppendBits((uint)transformedValue, 13);
        }
    }
}
