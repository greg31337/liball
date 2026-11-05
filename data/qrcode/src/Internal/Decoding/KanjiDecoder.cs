namespace qrcode;

internal class KanjiDecoder
{
    internal static int RestoreShiftJisValue(int transformedValue)
    {
        int value = TryRestoreShiftJisValue(transformedValue);
        if (value == -1) { throw new QrDecodeException("The encoded value is invalid or truncated.", QrFailureReason.InvalidData, -1, 0); }
        return value;
    }

    internal static int TryRestoreShiftJisValue(int transformedValue)
    {
        if (transformedValue < 0 || transformedValue > 0x1FFF)
        {
            return -1;
        }

        int adjustedValue = transformedValue / 0xC0 << 8 | transformedValue % 0xC0;
        int shiftJisValue;
        if (adjustedValue < 0x1F00)
        {
            shiftJisValue = adjustedValue + KanjiEncoder.FirstRangeMinimum;
        }
        else
        {
            shiftJisValue = adjustedValue + 0xC140;
        }

        if (!KanjiEncoder.IsPermittedShiftJisValue(shiftJisValue))
        {
            return -1;
        }

        return shiftJisValue;
    }

    internal static char GetCharacter(int shiftJisValue)
    {
        if (!KanjiEncoder.IsPermittedShiftJisValue(shiftJisValue))
        {
            throw new QrDecodeException(
                "The Shift JIS value is outside the ranges permitted by QR Kanji mode.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        int unicodeValue = ShiftJisTable.GetStandardUnicode(shiftJisValue);
        if (unicodeValue == ShiftJisTable.NotMapped)
        {
            throw new QrDecodeException(
                "The Shift JIS value is unassigned in the standard Kanji mapping.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        return (char)unicodeValue;
    }

    internal static string DecodeText(BitReader reader, int characterCount)
    {
        if (reader == null)
        {
            throw new QrInputException("The bit reader cannot be null.", "reader");
        }

        if (characterCount < 0)
        {
            throw new QrInputException("The character count cannot be negative.", "characterCount");
        }

        if (characterCount > QrLimits.DefaultMaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The Kanji character count exceeds the configured limit.",
                "MaximumInputBytes",
                characterCount,
                QrLimits.DefaultMaximumInputBytes);
        }

        CheckedDimensions.CalculateAllocationBytes(
            characterCount,
            2,
            QrLimits.DefaultMaximumAllocationBytes);
        int requiredBitCount = KanjiEncoder.CalculateBitLength(characterCount);
        if (requiredBitCount > reader.GetRemainingBitCount())
        {
            throw new QrDecodeException(
                "The Kanji segment ends before all declared characters can be read.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        char[] characters = new char[characterCount];
        for (int index = 0; index < characterCount; index++)
        {
            int transformedValue = (int)reader.ReadBits(13);
            int shiftJisValue = RestoreShiftJisValue(transformedValue);
            characters[index] = GetCharacter(shiftJisValue);
        }

        return new string(characters);
    }
}
