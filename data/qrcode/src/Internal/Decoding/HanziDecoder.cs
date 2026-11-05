namespace qrcode;

internal class HanziDecoder
{
    internal static int RestoreGb2312Value(int transformedValue)
    {
        int value = TryRestoreGb2312Value(transformedValue);
        if (value == -1) { throw new QrDecodeException("The encoded value is invalid or truncated.", QrFailureReason.InvalidData, -1, 0); }
        return value;
    }

    internal static int TryRestoreGb2312Value(int transformedValue)
    {
        if (transformedValue < 0 || transformedValue > 0x1FFF)
        {
            return -1;
        }

        int adjustedValue = transformedValue / 0x60 << 8 | transformedValue % 0x60;
        int gb2312Value;
        if (transformedValue < 0x3C0)
        {
            gb2312Value = adjustedValue + HanziEncoder.FirstRangeMinimum;
        }
        else
        {
            gb2312Value = adjustedValue + 0xA6A1;
        }

        if (!HanziEncoder.IsPermittedGb2312Value(gb2312Value))
        {
            return -1;
        }

        return gb2312Value;
    }

    internal static char GetCharacter(int gb2312Value)
    {
        if (!HanziEncoder.IsPermittedGb2312Value(gb2312Value))
        {
            throw new QrDecodeException(
                "The GB2312 value is outside the ranges permitted by Hanzi subset 1.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        int unicodeValue = Gb2312Table.GetStandardUnicode(gb2312Value);
        if (unicodeValue == Gb2312Table.NotMapped)
        {
            throw new QrDecodeException(
                "The GB2312 value is unassigned in the standard Hanzi mapping.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        return (char)unicodeValue;
    }

    internal static int ReadSubsetIndicator(
        BitReader reader,
        bool enableHanziCompatibility)
    {
        if (reader == null)
        {
            throw new QrInputException("The bit reader cannot be null.", "reader");
        }

        EnsureCompatibilityEnabled(enableHanziCompatibility);
        if (reader.GetRemainingBitCount() < HanziEncoder.SubsetIndicatorBitCount)
        {
            throw new QrDecodeException(
                "The Hanzi segment ends before its subset indicator can be read.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        int subset = (int)reader.ReadBits(HanziEncoder.SubsetIndicatorBitCount);
        if (subset != HanziEncoder.Subset1)
        {
            throw new QrDecodeException(
                "Only Hanzi subset 1 is supported by the compatibility mode.",
                QrFailureReason.InvalidMode,
                -1,
                0);
        }

        return subset;
    }

    internal static QrSegment DecodeCompatibilitySegment(
        BitReader reader,
        int characterCount,
        bool enableHanziCompatibility)
    {
        if (reader == null)
        {
            throw new QrInputException("The bit reader cannot be null.", "reader");
        }

        EnsureCompatibilityEnabled(enableHanziCompatibility);
        if (characterCount < 0)
        {
            throw new QrInputException("The character count cannot be negative.", "characterCount");
        }

        if (characterCount > QrLimits.DefaultMaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The Hanzi character count exceeds the configured limit.",
                "MaximumInputBytes",
                characterCount,
                QrLimits.DefaultMaximumInputBytes);
        }

        int dataLength = CheckedDimensions.CalculateAllocationBytes(
            characterCount,
            2,
            QrLimits.DefaultMaximumAllocationBytes);
        int requiredBitCount = HanziEncoder.CalculateBitLength(characterCount);
        if (requiredBitCount > reader.GetRemainingBitCount())
        {
            throw new QrDecodeException(
                "The Hanzi segment ends before all declared characters can be read.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        char[] characters = new char[characterCount];
        byte[] data = new byte[dataLength];
        for (int index = 0; index < characterCount; index++)
        {
            int transformedValue = (int)reader.ReadBits(13);
            int gb2312Value = RestoreGb2312Value(transformedValue);
            characters[index] = GetCharacter(gb2312Value);
            data[index * 2] = (byte)(gb2312Value >> 8);
            data[index * 2 + 1] = (byte)(gb2312Value & 0xFF);
        }

        return new QrSegment
        {
            Mode = QrEncodingMode.Hanzi,
            CharacterCount = characterCount,
            Data = data,
            Text = new string(characters),
            IsHanziCompatibilityMode = true
        };
    }

    internal static string DecodeText(
        BitReader reader,
        int characterCount,
        bool enableHanziCompatibility)
    {
        QrSegment segment = DecodeCompatibilitySegment(
            reader,
            characterCount,
            enableHanziCompatibility);
        if (segment.Text == null)
        {
            return string.Empty;
        }

        return segment.Text;
    }

    static void EnsureCompatibilityEnabled(bool enableHanziCompatibility)
    {
        if (!enableHanziCompatibility)
        {
            throw new QrDecodeException(
                "Hanzi subset 1 decoding requires explicit compatibility opt-in.",
                QrFailureReason.InvalidMode,
                -1,
                0);
        }
    }
}
