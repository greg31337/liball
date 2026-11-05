namespace qrcode;

internal class AlphanumericDecoder
{
    internal static string DecodeText(BitReader reader, int characterCount)
    {
        string? value = TryDecodeText(reader, characterCount);
        if (value == null) { throw new QrDecodeException("The encoded value is invalid or truncated.", QrFailureReason.InvalidData, -1, 0); }
        return value;
    }

    internal static string? TryDecodeText(BitReader reader, int characterCount)
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
                "The alphanumeric character count exceeds the configured limit.",
                "MaximumInputBytes",
                characterCount,
                QrLimits.DefaultMaximumInputBytes);
        }

        CheckedDimensions.CalculateAllocationBytes(
            characterCount,
            2,
            QrLimits.DefaultMaximumAllocationBytes);
        int requiredBitCount = AlphanumericEncoder.CalculateBitLength(characterCount);
        if (requiredBitCount > reader.GetRemainingBitCount())
        {
            return null;
        }

        char[] characters = new char[characterCount];
        int outputIndex = 0;
        while (characterCount - outputIndex >= 2)
        {
            int value = (int)reader.ReadBits(11);
            if (value >= 45 * 45)
            {
                return null;
            }

            int firstValue = value / 45;
            int secondValue = value % 45;
            characters[outputIndex] = QrCharacterSets.Alphanumeric[firstValue];
            characters[outputIndex + 1] = QrCharacterSets.Alphanumeric[secondValue];
            outputIndex += 2;
        }

        if (outputIndex < characterCount)
        {
            int value = (int)reader.ReadBits(6);
            if (value >= 45)
            {
                return null;
            }

            characters[outputIndex] = QrCharacterSets.Alphanumeric[value];
        }

        return new string(characters);
    }
}
