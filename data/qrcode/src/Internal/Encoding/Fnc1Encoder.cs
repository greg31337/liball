namespace qrcode;

internal class Fnc1Encoder
{
    internal const char GroupSeparator = (char)0x1D;

    internal static int CalculateEscapedLength(string text)
    {
        if (text == null)
        {
            throw new QrInputException("The GS1 text cannot be null.", "text");
        }

        int escapedLength = 0;
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (character == GroupSeparator && index + 1 < text.Length
                && (text[index + 1] == '%' || text[index + 1] == GroupSeparator))
            {
                throw new QrInputException(
                    "A group separator followed by a percent sign or another separator requires byte mode or separate alphanumeric segments.",
                    "text");
            }

            if (character != GroupSeparator)
            {
                AlphanumericEncoder.GetValue(character);
            }

            int appendedCharacterCount = character == '%' ? 2 : 1;
            escapedLength = CheckedDimensions.CalculateSum(
                escapedLength,
                appendedCharacterCount,
                QrLimits.DefaultMaximumInputBytes,
                "MaximumInputBytes");
        }

        return escapedLength;
    }

    internal static string EscapeGs1AlphanumericText(string text)
    {
        int escapedLength = CalculateEscapedLength(text);
        CheckedDimensions.CalculateAllocationBytes(
            escapedLength,
            2,
            QrLimits.DefaultMaximumAllocationBytes);
        char[] escaped = new char[escapedLength];
        int outputIndex = 0;
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (character == GroupSeparator)
            {
                escaped[outputIndex] = '%';
                outputIndex++;
            }
            else if (character == '%')
            {
                escaped[outputIndex] = '%';
                escaped[outputIndex + 1] = '%';
                outputIndex += 2;
            }
            else
            {
                escaped[outputIndex] = character;
                outputIndex++;
            }
        }

        return new string(escaped);
    }

    internal static void EncodeFirstPositionText(string text, BitBuffer destination)
    {
        if (destination == null)
        {
            throw new QrInputException("The destination bit buffer cannot be null.", "destination");
        }

        string escapedText = EscapeGs1AlphanumericText(text);
        AlphanumericEncoder.EncodeText(escapedText, destination);
    }

    internal static QrSegment CreateFirstPositionControlSegment()
    {
        return new QrSegment
        {
            Mode = QrEncodingMode.Fnc1First
        };
    }

    internal static bool IsValidSecondPositionIndicator(int applicationIndicator)
    {
        return applicationIndicator >= 0 && applicationIndicator <= 99
            || applicationIndicator >= 165 && applicationIndicator <= 190;
    }

    internal static int GetLetterApplicationIndicator(char letter)
    {
        if (letter < 'A' || letter > 'Z')
        {
            throw new QrInputException(
                "An alphabetic FNC1 application indicator must be an uppercase ASCII letter.",
                "letter");
        }

        return letter + 100;
    }

    internal static void ValidateSecondPositionIndicator(int applicationIndicator)
    {
        if (!IsValidSecondPositionIndicator(applicationIndicator))
        {
            throw new QrInputException(
                "The FNC1 application indicator must represent 00-99 or uppercase A-Z.",
                "applicationIndicator");
        }
    }

    internal static void EncodeSecondPositionIndicator(
        int applicationIndicator,
        BitBuffer destination)
    {
        if (destination == null)
        {
            throw new QrInputException("The destination bit buffer cannot be null.", "destination");
        }

        ValidateSecondPositionIndicator(applicationIndicator);
        int requiredBitLength = CheckedDimensions.CalculateSum(
            destination.GetBitLength(),
            8,
            int.MaxValue,
            "BitLength");
        destination.EnsureBitCapacity(requiredBitLength);
        destination.AppendBits((uint)applicationIndicator, 8);
    }

    internal static void EncodeSecondPositionText(string text, BitBuffer destination)
    {
        EncodeFirstPositionText(text, destination);
    }

    internal static QrSegment CreateSecondPositionControlSegment(int applicationIndicator)
    {
        ValidateSecondPositionIndicator(applicationIndicator);
        return new QrSegment
        {
            Mode = QrEncodingMode.Fnc1Second,
            Data = new byte[] { (byte)applicationIndicator },
            Fnc1ApplicationIndicator = applicationIndicator
        };
    }
}
