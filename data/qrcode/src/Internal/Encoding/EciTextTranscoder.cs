namespace qrcode;

internal class EciTextTranscoder
{
    internal static bool IsSupportedAssignment(int assignment)
    {
        EciEncoder.ValidateAssignment(assignment);
        return assignment == QrEciAssignments.ShiftJis
            || assignment == QrEciAssignments.Gb2312
            || EciRegistry.HasDotNetEncoding(assignment);
    }

    internal static byte[] EncodeText(string text, int assignment)
    {
        if (text == null)
        {
            throw new QrInputException("The text to encode cannot be null.", "text");
        }

        EnsureSupportedAssignment(assignment);
        if (assignment == QrEciAssignments.ShiftJis)
        {
            return EncodeShiftJis(text);
        }

        if (assignment == QrEciAssignments.Gb2312)
        {
            return EncodeGb2312(text);
        }

        global::System.Text.Encoding encoding = EciRegistry.GetDotNetEncoding(assignment);
        int byteCount = encoding.GetByteCount(text);
        ValidateEncodedByteCount(byteCount);
        CheckedDimensions.CalculateAllocationBytes(
            byteCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        return encoding.GetBytes(text);
    }

    internal static string DecodeBytes(byte[] bytes, int assignment)
    {
        if (bytes == null)
        {
            throw new QrInputException("The encoded bytes cannot be null.", "bytes");
        }

        EnsureSupportedAssignment(assignment);
        ValidateEncodedByteCount(bytes.Length);
        if (assignment == QrEciAssignments.ShiftJis)
        {
            return DecodeShiftJis(bytes);
        }

        if (assignment == QrEciAssignments.Gb2312)
        {
            return DecodeGb2312(bytes);
        }

        global::System.Text.Encoding encoding = EciRegistry.GetDotNetEncoding(assignment);
        int characterCount = encoding.GetCharCount(bytes);
        CheckedDimensions.CalculateAllocationBytes(
            characterCount,
            2,
            QrLimits.DefaultMaximumAllocationBytes);
        return encoding.GetString(bytes);
    }

    // Missing mappings are ordinary decode outcomes. Validate without exception
    // handling or replacement text so callers can retain the original bytes.
    internal static string? TryDecodeBytes(byte[] bytes, int assignment)
    {
        if (bytes == null)
        {
            throw new QrInputException("The encoded bytes cannot be null.", "bytes");
        }

        ValidateEncodedByteCount(bytes.Length);
        if (!IsSupportedAssignment(assignment))
        {
            return null;
        }

        if (assignment == QrEciAssignments.ShiftJis || assignment == QrEciAssignments.Gb2312)
        {
            if (!HasMappedLegacyBytes(bytes, assignment == QrEciAssignments.ShiftJis))
            {
                return null;
            }

            return DecodeBytes(bytes, assignment);
        }

        global::System.Text.Encoding encoding = EciRegistry.GetDotNetEncoding(assignment);
        global::System.Text.Encoding probe = (global::System.Text.Encoding)encoding.Clone();
        probe.DecoderFallback = new global::System.Text.DecoderReplacementFallback("");
        int withoutReplacement = probe.GetCharCount(bytes);
        probe.DecoderFallback = new global::System.Text.DecoderReplacementFallback("\uFFFD");
        if (probe.GetCharCount(bytes) != withoutReplacement)
        {
            return null;
        }

        // A legitimate encoded U+FFFD contributes equally to both counts.
        return DecodeBytes(bytes, assignment);
    }

    static bool HasMappedLegacyBytes(byte[] bytes, bool shiftJis)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            int first = bytes[index];
            if (shiftJis ? first <= 0x1F || first == 0x7F || ShiftJisTable.GetStandardUnicode(first) != ShiftJisTable.NotMapped
                : first <= 0x7F)
            {
                index++;
                continue;
            }

            if (index + 1 >= bytes.Length)
            {
                return false;
            }

            int pair = first << 8 | bytes[index + 1];
            int value = shiftJis ? ShiftJisTable.GetStandardUnicode(pair) : Gb2312Table.GetStandardUnicode(pair);
            if (value < 0)
            {
                return false;
            }

            index += 2;
        }

        return true;
    }

    static byte[] EncodeShiftJis(string text)
    {
        int byteCount = 0;
        for (int index = 0; index < text.Length; index++)
        {
            int unicodeValue = text[index];
            int encodedValue;
            if (unicodeValue <= 0x1F || unicodeValue == 0x7F)
            {
                encodedValue = unicodeValue;
            }
            else
            {
                encodedValue = ShiftJisTable.GetStandardEncodedValue(unicodeValue);
            }

            if (encodedValue == ShiftJisTable.NotMapped)
            {
                throw new QrInputException(
                    "The text contains a character that is not assigned in standard Shift JIS.",
                    "text");
            }

            int encodedCharacterByteCount = encodedValue <= 0xFF ? 1 : 2;
            byteCount = CheckedDimensions.CalculateSum(
                byteCount,
                encodedCharacterByteCount,
                QrLimits.DefaultMaximumInputBytes,
                "MaximumInputBytes");
        }

        CheckedDimensions.CalculateAllocationBytes(
            byteCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        byte[] bytes = new byte[byteCount];
        int byteIndex = 0;
        for (int index = 0; index < text.Length; index++)
        {
            int unicodeValue = text[index];
            int encodedValue;
            if (unicodeValue <= 0x1F || unicodeValue == 0x7F)
            {
                encodedValue = unicodeValue;
            }
            else
            {
                encodedValue = ShiftJisTable.GetStandardEncodedValue(unicodeValue);
            }

            if (encodedValue > 0xFF)
            {
                bytes[byteIndex] = (byte)(encodedValue >> 8);
                byteIndex++;
            }

            bytes[byteIndex] = (byte)(encodedValue & 0xFF);
            byteIndex++;
        }

        return bytes;
    }

    static string DecodeShiftJis(byte[] bytes)
    {
        int characterCount = CountShiftJisCharacters(bytes);
        CheckedDimensions.CalculateAllocationBytes(
            characterCount,
            2,
            QrLimits.DefaultMaximumAllocationBytes);
        char[] characters = new char[characterCount];
        int byteIndex = 0;
        int characterIndex = 0;
        while (byteIndex < bytes.Length)
        {
            int firstByte = bytes[byteIndex];
            int unicodeValue;
            if (firstByte <= 0x1F || firstByte == 0x7F)
            {
                unicodeValue = firstByte;
                byteIndex++;
            }
            else
            {
                unicodeValue = ShiftJisTable.GetStandardUnicode(firstByte);
                if (unicodeValue != ShiftJisTable.NotMapped)
                {
                    byteIndex++;
                }
                else
                {
                    int encodedValue = firstByte << 8 | bytes[byteIndex + 1];
                    unicodeValue = ShiftJisTable.GetStandardUnicode(encodedValue);
                    byteIndex += 2;
                }
            }

            characters[characterIndex] = (char)unicodeValue;
            characterIndex++;
        }

        return new string(characters);
    }

    static int CountShiftJisCharacters(byte[] bytes)
    {
        int byteIndex = 0;
        int characterCount = 0;
        while (byteIndex < bytes.Length)
        {
            int firstByte = bytes[byteIndex];
            if (firstByte <= 0x1F || firstByte == 0x7F)
            {
                byteIndex++;
            }
            else if (ShiftJisTable.GetStandardUnicode(firstByte) != ShiftJisTable.NotMapped)
            {
                byteIndex++;
            }
            else
            {
                if (byteIndex + 1 >= bytes.Length)
                {
                    throw new QrDecodeException(
                        "The Shift JIS byte sequence ends with an incomplete double-byte character.",
                        QrFailureReason.InvalidData,
                        -1,
                        0);
                }

                int encodedValue = firstByte << 8 | bytes[byteIndex + 1];
                if (ShiftJisTable.GetStandardUnicode(encodedValue) == ShiftJisTable.NotMapped)
                {
                    throw new QrDecodeException(
                        "The byte sequence contains a value that is not assigned in standard Shift JIS.",
                        QrFailureReason.InvalidData,
                        -1,
                        0);
                }

                byteIndex += 2;
            }

            characterCount = CheckedDimensions.CalculateSum(
                characterCount,
                1,
                QrLimits.DefaultMaximumInputBytes,
                "MaximumInputBytes");
        }

        return characterCount;
    }

    static byte[] EncodeGb2312(string text)
    {
        int byteCount = 0;
        for (int index = 0; index < text.Length; index++)
        {
            int unicodeValue = text[index];
            int encodedValue;
            if (unicodeValue <= 0x7F)
            {
                encodedValue = unicodeValue;
            }
            else
            {
                encodedValue = Gb2312Table.GetStandardEncodedValue(unicodeValue);
            }

            if (encodedValue == Gb2312Table.NotMapped)
            {
                throw new QrInputException(
                    "The text contains a character that is not assigned in standard GB2312.",
                    "text");
            }

            int encodedCharacterByteCount = encodedValue <= 0x7F ? 1 : 2;
            byteCount = CheckedDimensions.CalculateSum(
                byteCount,
                encodedCharacterByteCount,
                QrLimits.DefaultMaximumInputBytes,
                "MaximumInputBytes");
        }

        CheckedDimensions.CalculateAllocationBytes(
            byteCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        byte[] bytes = new byte[byteCount];
        int byteIndex = 0;
        for (int index = 0; index < text.Length; index++)
        {
            int unicodeValue = text[index];
            int encodedValue;
            if (unicodeValue <= 0x7F)
            {
                encodedValue = unicodeValue;
            }
            else
            {
                encodedValue = Gb2312Table.GetStandardEncodedValue(unicodeValue);
            }

            if (encodedValue > 0x7F)
            {
                bytes[byteIndex] = (byte)(encodedValue >> 8);
                byteIndex++;
            }

            bytes[byteIndex] = (byte)(encodedValue & 0xFF);
            byteIndex++;
        }

        return bytes;
    }

    static string DecodeGb2312(byte[] bytes)
    {
        int characterCount = CountGb2312Characters(bytes);
        CheckedDimensions.CalculateAllocationBytes(
            characterCount,
            2,
            QrLimits.DefaultMaximumAllocationBytes);
        char[] characters = new char[characterCount];
        int byteIndex = 0;
        int characterIndex = 0;
        while (byteIndex < bytes.Length)
        {
            int firstByte = bytes[byteIndex];
            int unicodeValue;
            if (firstByte <= 0x7F)
            {
                unicodeValue = firstByte;
                byteIndex++;
            }
            else
            {
                int encodedValue = firstByte << 8 | bytes[byteIndex + 1];
                unicodeValue = Gb2312Table.GetStandardUnicode(encodedValue);
                byteIndex += 2;
            }

            characters[characterIndex] = (char)unicodeValue;
            characterIndex++;
        }

        return new string(characters);
    }

    static int CountGb2312Characters(byte[] bytes)
    {
        int byteIndex = 0;
        int characterCount = 0;
        while (byteIndex < bytes.Length)
        {
            int firstByte = bytes[byteIndex];
            if (firstByte <= 0x7F)
            {
                byteIndex++;
            }
            else
            {
                if (byteIndex + 1 >= bytes.Length)
                {
                    throw new QrDecodeException(
                        "The GB2312 byte sequence ends with an incomplete double-byte character.",
                        QrFailureReason.InvalidData,
                        -1,
                        0);
                }

                int encodedValue = firstByte << 8 | bytes[byteIndex + 1];
                if (Gb2312Table.GetStandardUnicode(encodedValue) == Gb2312Table.NotMapped)
                {
                    throw new QrDecodeException(
                        "The byte sequence contains a value that is not assigned in standard GB2312.",
                        QrFailureReason.InvalidData,
                        -1,
                        0);
                }

                byteIndex += 2;
            }

            characterCount = CheckedDimensions.CalculateSum(
                characterCount,
                1,
                QrLimits.DefaultMaximumInputBytes,
                "MaximumInputBytes");
        }

        return characterCount;
    }

    static void EnsureSupportedAssignment(int assignment)
    {
        if (!IsSupportedAssignment(assignment))
        {
            throw new QrUnsupportedFeatureException(
                "The ECI assignment has no built-in text transcoder.",
                QrFailureReason.UnsupportedTextEncoding,
                "ECI",
                assignment);
        }
    }

    static void ValidateEncodedByteCount(int byteCount)
    {
        if (byteCount > QrLimits.DefaultMaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The encoded text exceeds the configured input limit.",
                "MaximumInputBytes",
                byteCount,
                QrLimits.DefaultMaximumInputBytes);
        }
    }
}
