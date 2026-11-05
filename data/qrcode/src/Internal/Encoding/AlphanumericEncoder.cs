namespace qrcode;

internal class AlphanumericEncoder
{
    internal static int GetValue(char character)
    {
        int value = QrCharacterSets.Alphanumeric.IndexOf(character);
        if (value < 0)
        {
            throw new QrInputException(
                "Alphanumeric mode accepts only the standardized 45-character repertoire.",
                "character");
        }

        return value;
    }

    internal static void ValidateText(string text)
    {
        if (text == null)
        {
            throw new QrInputException("The alphanumeric text cannot be null.", "text");
        }

        for (int index = 0; index < text.Length; index++)
        {
            GetValue(text[index]);
        }
    }

    internal static int CalculateBitLength(int characterCount)
    {
        if (characterCount < 0)
        {
            throw new QrInputException("The character count cannot be negative.", "characterCount");
        }

        int pairCount = characterCount / 2;
        int bitLength = CheckedDimensions.CalculateProduct(
            pairCount,
            11,
            int.MaxValue,
            "AlphanumericBitLength");
        if (characterCount % 2 != 0)
        {
            bitLength = CheckedDimensions.CalculateSum(
                bitLength,
                6,
                int.MaxValue,
                "AlphanumericBitLength");
        }

        return bitLength;
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

        int index = 0;
        while (index + 2 <= text.Length)
        {
            int firstValue = GetValue(text[index]);
            int secondValue = GetValue(text[index + 1]);
            int combinedValue = firstValue * 45 + secondValue;
            destination.AppendBits((uint)combinedValue, 11);
            index += 2;
        }

        if (index < text.Length)
        {
            destination.AppendBits((uint)GetValue(text[index]), 6);
        }
    }
}
