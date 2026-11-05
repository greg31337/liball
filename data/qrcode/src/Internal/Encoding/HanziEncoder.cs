namespace qrcode;

internal class HanziEncoder
{
    internal const int Subset1 = 1;
    internal const int SubsetIndicatorBitCount = 4;
    internal const int FirstRangeMinimum = 0xA1A1;
    internal const int FirstRangeMaximum = 0xAAFE;
    internal const int SecondRangeMinimum = 0xB0A1;
    internal const int SecondRangeMaximum = 0xFAFE;

    internal static bool IsPermittedGb2312Value(int gb2312Value)
    {
        return gb2312Value >= FirstRangeMinimum && gb2312Value <= FirstRangeMaximum
            || gb2312Value >= SecondRangeMinimum && gb2312Value <= SecondRangeMaximum;
    }

    internal static bool IsEncodableCharacter(char character)
    {
        int gb2312Value = Gb2312Table.GetStandardEncodedValue(character);
        return IsPermittedGb2312Value(gb2312Value);
    }

    internal static int GetGb2312Value(char character)
    {
        int gb2312Value = Gb2312Table.GetStandardEncodedValue(character);
        if (!IsPermittedGb2312Value(gb2312Value))
        {
            throw new QrInputException(
                "Hanzi subset 1 accepts only characters assigned in its standard GB2312 ranges.",
                "character");
        }

        return gb2312Value;
    }

    internal static int TransformGb2312Value(int gb2312Value)
    {
        int adjustedValue;
        if (gb2312Value >= FirstRangeMinimum && gb2312Value <= FirstRangeMaximum)
        {
            adjustedValue = gb2312Value - FirstRangeMinimum;
        }
        else if (gb2312Value >= SecondRangeMinimum && gb2312Value <= SecondRangeMaximum)
        {
            adjustedValue = gb2312Value - 0xA6A1;
        }
        else
        {
            throw new QrInputException(
                "The GB2312 value is outside the ranges permitted by Hanzi subset 1.",
                "gb2312Value");
        }

        return (adjustedValue >> 8) * 0x60 + (adjustedValue & 0xFF);
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
            "HanziBitLength");
    }

    internal static void ValidateCompatibilityEnabled(bool enableHanziCompatibility)
    {
        if (!enableHanziCompatibility)
        {
            throw new QrInputException(
                "Hanzi subset 1 requires explicit compatibility opt-in.",
                "enableHanziCompatibility");
        }
    }

    internal static void ValidateText(string text, bool enableHanziCompatibility)
    {
        ValidateCompatibilityEnabled(enableHanziCompatibility);
        if (text == null)
        {
            throw new QrInputException("The Hanzi text cannot be null.", "text");
        }

        if (text.Length > QrLimits.DefaultMaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The Hanzi text exceeds the configured input limit.",
                "MaximumInputBytes",
                text.Length,
                QrLimits.DefaultMaximumInputBytes);
        }

        for (int index = 0; index < text.Length; index++)
        {
            GetGb2312Value(text[index]);
        }
    }

    internal static void EncodeSubsetIndicator(
        BitBuffer destination,
        bool enableHanziCompatibility)
    {
        if (destination == null)
        {
            throw new QrInputException("The destination bit buffer cannot be null.", "destination");
        }

        ValidateCompatibilityEnabled(enableHanziCompatibility);
        int requiredBitLength = CheckedDimensions.CalculateSum(
            destination.GetBitLength(),
            SubsetIndicatorBitCount,
            int.MaxValue,
            "BitLength");
        destination.EnsureBitCapacity(requiredBitLength);
        destination.AppendBits((uint)Subset1, SubsetIndicatorBitCount);
    }

    internal static void EncodeText(
        string text,
        BitBuffer destination,
        bool enableHanziCompatibility)
    {
        if (destination == null)
        {
            throw new QrInputException("The destination bit buffer cannot be null.", "destination");
        }

        ValidateText(text, enableHanziCompatibility);
        int encodedBitLength = CalculateBitLength(text.Length);
        int requiredBitLength = CheckedDimensions.CalculateSum(
            destination.GetBitLength(),
            encodedBitLength,
            int.MaxValue,
            "BitLength");
        destination.EnsureBitCapacity(requiredBitLength);

        for (int index = 0; index < text.Length; index++)
        {
            int gb2312Value = GetGb2312Value(text[index]);
            int transformedValue = TransformGb2312Value(gb2312Value);
            destination.AppendBits((uint)transformedValue, 13);
        }
    }

    internal static QrSegment CreateCompatibilitySegment(
        string text,
        bool enableHanziCompatibility)
    {
        ValidateText(text, enableHanziCompatibility);
        int dataLength = CheckedDimensions.CalculateAllocationBytes(
            text.Length,
            2,
            QrLimits.DefaultMaximumAllocationBytes);
        byte[] data = new byte[dataLength];
        for (int index = 0; index < text.Length; index++)
        {
            int gb2312Value = GetGb2312Value(text[index]);
            data[index * 2] = (byte)(gb2312Value >> 8);
            data[index * 2 + 1] = (byte)(gb2312Value & 0xFF);
        }

        return new QrSegment
        {
            Mode = QrEncodingMode.Hanzi,
            CharacterCount = text.Length,
            Data = data,
            Text = text,
            IsHanziCompatibilityMode = true
        };
    }
}
