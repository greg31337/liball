namespace qrcode;

internal class NumericDecoder
{
    internal static string DecodeDigits(BitReader reader, int characterCount)
    {
        string? value = TryDecodeDigits(reader, characterCount);
        if (value == null) { throw new QrDecodeException("The encoded value is invalid or truncated.", QrFailureReason.InvalidData, -1, 0); }
        return value;
    }

    internal static string? TryDecodeDigits(BitReader reader, int characterCount)
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
                "The numeric character count exceeds the configured limit.",
                "MaximumInputBytes",
                characterCount,
                QrLimits.DefaultMaximumInputBytes);
        }

        CheckedDimensions.CalculateAllocationBytes(
            characterCount,
            2,
            QrLimits.DefaultMaximumAllocationBytes);
        int requiredBitCount = NumericEncoder.CalculateBitLength(characterCount);
        if (requiredBitCount > reader.GetRemainingBitCount())
        {
            return null;
        }

        char[] characters = new char[characterCount];
        int outputIndex = 0;
        while (characterCount - outputIndex >= 3)
        {
            int value = (int)reader.ReadBits(10);
            if (value > 999)
            {
                return null;
            }

            characters[outputIndex] = (char)('0' + value / 100);
            characters[outputIndex + 1] = (char)('0' + value / 10 % 10);
            characters[outputIndex + 2] = (char)('0' + value % 10);
            outputIndex += 3;
        }

        int remainingCharacterCount = characterCount - outputIndex;
        if (remainingCharacterCount == 2)
        {
            int value = (int)reader.ReadBits(7);
            if (value > 99)
            {
                return null;
            }

            characters[outputIndex] = (char)('0' + value / 10);
            characters[outputIndex + 1] = (char)('0' + value % 10);
        }
        else if (remainingCharacterCount == 1)
        {
            int value = (int)reader.ReadBits(4);
            if (value > 9)
            {
                return null;
            }

            characters[outputIndex] = (char)('0' + value);
        }

        return new string(characters);
    }
}
