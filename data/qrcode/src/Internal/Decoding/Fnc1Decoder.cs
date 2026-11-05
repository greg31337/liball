namespace qrcode;

internal class Fnc1Decoder
{
    internal static string DecodeGs1AlphanumericText(string encodedText)
    {
        if (encodedText == null)
        {
            throw new QrInputException("The encoded GS1 text cannot be null.", "encodedText");
        }

        if (encodedText.Length > QrLimits.DefaultMaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The encoded GS1 text exceeds the configured input limit.",
                "MaximumInputBytes",
                encodedText.Length,
                QrLimits.DefaultMaximumInputBytes);
        }

        int decodedLength = 0;
        int index = 0;
        while (index < encodedText.Length)
        {
            char character = encodedText[index];
            AlphanumericEncoder.GetValue(character);
            if (character == '%' && index + 1 < encodedText.Length && encodedText[index + 1] == '%')
            {
                index += 2;
            }
            else
            {
                index++;
            }

            decodedLength++;
        }

        CheckedDimensions.CalculateAllocationBytes(
            decodedLength,
            2,
            QrLimits.DefaultMaximumAllocationBytes);
        char[] decoded = new char[decodedLength];
        int outputIndex = 0;
        index = 0;
        while (index < encodedText.Length)
        {
            char character = encodedText[index];
            if (character == '%')
            {
                if (index + 1 < encodedText.Length && encodedText[index + 1] == '%')
                {
                    decoded[outputIndex] = '%';
                    index += 2;
                }
                else
                {
                    decoded[outputIndex] = Fnc1Encoder.GroupSeparator;
                    index++;
                }
            }
            else
            {
                decoded[outputIndex] = character;
                index++;
            }

            outputIndex++;
        }

        return new string(decoded);
    }

    internal static string DecodeFirstPositionAlphanumeric(
        BitReader reader,
        int characterCount)
    {
        string encodedText = AlphanumericDecoder.DecodeText(reader, characterCount);
        return DecodeGs1AlphanumericText(encodedText);
    }

    internal static QrSegment CreateFirstPositionControlSegment()
    {
        return new QrSegment
        {
            Mode = QrEncodingMode.Fnc1First
        };
    }

    internal static void ApplyFirstPositionMetadata(QrDecodeResult result, bool hasEci)
    {
        if (result == null)
        {
            throw new QrInputException("The decode result cannot be null.", "result");
        }

        QrSymbolKind kind = result.Symbol == null ? QrSymbolKind.Model2 : result.Symbol.SymbolKind;
        string identifier = SymbologyIdentifierSelector.GetIdentifier(kind, hasEci, true, result.Fnc1SecondApplicationIndicator);
        result.UsesFnc1First = true;
        result.SymbologyIdentifier = identifier;
    }

    internal static int DecodeSecondPositionIndicator(BitReader reader)
    {
        int value = TryDecodeSecondPositionIndicator(reader);
        if (value == -1) { throw new QrDecodeException("The encoded value is invalid or truncated.", QrFailureReason.InvalidData, -1, 0); }
        return value;
    }

    internal static int TryDecodeSecondPositionIndicator(BitReader reader)
    {
        if (reader == null)
        {
            throw new QrInputException("The bit reader cannot be null.", "reader");
        }

        int startPosition = reader.GetPosition();
        if (reader.GetRemainingBitCount() < 8)
        {
            return -1;
        }

        int applicationIndicator = (int)reader.ReadBits(8);
        if (!Fnc1Encoder.IsValidSecondPositionIndicator(applicationIndicator))
        {
            reader.SeekBitPosition(startPosition);
            return -1;
        }

        return applicationIndicator;
    }

    internal static char GetSecondPositionIndicatorLetter(int applicationIndicator)
    {
        if (applicationIndicator < 165 || applicationIndicator > 190)
        {
            throw new QrInputException(
                "The FNC1 application indicator does not represent a letter.",
                "applicationIndicator");
        }

        return (char)(applicationIndicator - 100);
    }

    internal static string DecodeSecondPositionAlphanumeric(
        BitReader reader,
        int characterCount)
    {
        string encodedText = AlphanumericDecoder.DecodeText(reader, characterCount);
        return DecodeGs1AlphanumericText(encodedText);
    }

    internal static QrSegment CreateSecondPositionControlSegment(BitReader reader)
    {
        int applicationIndicator = DecodeSecondPositionIndicator(reader);
        return new QrSegment
        {
            Mode = QrEncodingMode.Fnc1Second,
            Data = new byte[] { (byte)applicationIndicator },
            Fnc1ApplicationIndicator = applicationIndicator
        };
    }

    internal static void ApplySecondPositionMetadata(
        QrDecodeResult result,
        int applicationIndicator,
        bool hasEci)
    {
        if (result == null)
        {
            throw new QrInputException("The decode result cannot be null.", "result");
        }

        Fnc1Encoder.ValidateSecondPositionIndicator(applicationIndicator);
        QrSymbolKind kind = result.Symbol == null ? QrSymbolKind.Model2 : result.Symbol.SymbolKind;
        string identifier = SymbologyIdentifierSelector.GetIdentifier(kind, hasEci, result.UsesFnc1First, applicationIndicator);
        result.Fnc1SecondApplicationIndicator = applicationIndicator;
        result.SymbologyIdentifier = identifier;
    }
}
