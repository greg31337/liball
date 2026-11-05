namespace qrcode;

internal class NumericEncoder
{
    internal static void ValidateDigits(string digits)
    {
        if (digits == null)
        {
            throw new QrInputException("The numeric text cannot be null.", "digits");
        }

        for (int index = 0; index < digits.Length; index++)
        {
            char value = digits[index];
            if (value < '0' || value > '9')
            {
                throw new QrInputException("Numeric mode accepts only ASCII digits.", "digits");
            }
        }
    }

    internal static int CalculateBitLength(int characterCount)
    {
        if (characterCount < 0)
        {
            throw new QrInputException("The character count cannot be negative.", "characterCount");
        }

        int fullGroupCount = characterCount / 3;
        int terminalCharacterCount = characterCount % 3;
        int bitLength = CheckedDimensions.CalculateProduct(
            fullGroupCount,
            10,
            int.MaxValue,
            "NumericBitLength");
        if (terminalCharacterCount == 1)
        {
            bitLength = CheckedDimensions.CalculateSum(
                bitLength,
                4,
                int.MaxValue,
                "NumericBitLength");
        }
        else if (terminalCharacterCount == 2)
        {
            bitLength = CheckedDimensions.CalculateSum(
                bitLength,
                7,
                int.MaxValue,
                "NumericBitLength");
        }

        return bitLength;
    }

    internal static void EncodeDigits(string digits, BitBuffer destination)
    {
        if (destination == null)
        {
            throw new QrInputException("The destination bit buffer cannot be null.", "destination");
        }

        ValidateDigits(digits);
        int encodedBitLength = CalculateBitLength(digits.Length);
        int requiredBitLength = CheckedDimensions.CalculateSum(
            destination.GetBitLength(),
            encodedBitLength,
            int.MaxValue,
            "BitLength");
        destination.EnsureBitCapacity(requiredBitLength);

        int index = 0;
        while (index + 3 <= digits.Length)
        {
            int value = (digits[index] - '0') * 100
                + (digits[index + 1] - '0') * 10
                + digits[index + 2] - '0';
            destination.AppendBits((uint)value, 10);
            index += 3;
        }

        int remainingCharacterCount = digits.Length - index;
        if (remainingCharacterCount == 2)
        {
            int value = (digits[index] - '0') * 10 + digits[index + 1] - '0';
            destination.AppendBits((uint)value, 7);
        }
        else if (remainingCharacterCount == 1)
        {
            int value = digits[index] - '0';
            destination.AppendBits((uint)value, 4);
        }
    }
}
