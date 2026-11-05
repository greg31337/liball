namespace qrcode;

internal class ByteEncodingInference
{
    internal const int DefaultAssignment = 3;

    internal static int ResolveAssignment(
        byte[] bytes,
        int explicitAssignment,
        bool enableInference,
        bool requireExplicitEci)
    {
        if (bytes == null)
        {
            throw new QrInputException("The byte payload cannot be null.", "bytes");
        }

        if (bytes.Length > QrLimits.DefaultMaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The byte payload exceeds the configured input limit.",
                "MaximumInputBytes",
                bytes.Length,
                QrLimits.DefaultMaximumInputBytes);
        }

        if (explicitAssignment < -1)
        {
            throw new QrInputException(
                "The explicit ECI assignment must be -1 or a valid QR assignment.",
                "explicitAssignment");
        }

        if (explicitAssignment >= 0)
        {
            EciEncoder.ValidateAssignment(explicitAssignment);
            return explicitAssignment;
        }

        if (!enableInference)
        {
            return DefaultAssignment;
        }

        int inferredAssignment = InferAssignment(bytes);
        if (requireExplicitEci && inferredAssignment != DefaultAssignment)
        {
            throw new QrDecodeException(
                "A non-default byte encoding was inferred without an explicit ECI designator.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        return inferredAssignment;
    }

    internal static string DecodeText(
        byte[] bytes,
        int explicitAssignment,
        bool enableInference,
        bool requireExplicitEci)
    {
        int assignment = ResolveAssignment(
            bytes,
            explicitAssignment,
            enableInference,
            requireExplicitEci);
        return EciTextTranscoder.DecodeBytes(bytes, assignment);
    }

    static int InferAssignment(byte[] bytes)
    {
        int bomAssignment = GetBomAssignment(bytes);
        if (bomAssignment >= 0)
        {
            return bomAssignment;
        }

        int utf8MultibyteCount = GetUtf8MultibyteCount(bytes);
        if (utf8MultibyteCount > 0)
        {
            return QrEciAssignments.Utf8;
        }

        int shiftJisDoubleByteCount = GetShiftJisDoubleByteCount(bytes);
        int gb2312DoubleByteCount = GetGb2312DoubleByteCount(bytes);
        if (shiftJisDoubleByteCount > 0 && gb2312DoubleByteCount <= 0)
        {
            return QrEciAssignments.ShiftJis;
        }

        if (gb2312DoubleByteCount > 0 && shiftJisDoubleByteCount <= 0)
        {
            return QrEciAssignments.Gb2312;
        }

        if (shiftJisDoubleByteCount > 0 && gb2312DoubleByteCount > 0)
        {
            return QrEciAssignments.ShiftJis;
        }

        if (shiftJisDoubleByteCount >= 0 && gb2312DoubleByteCount < 0)
        {
            return QrEciAssignments.ShiftJis;
        }

        if (gb2312DoubleByteCount >= 0 && shiftJisDoubleByteCount < 0)
        {
            return QrEciAssignments.Gb2312;
        }

        return DefaultAssignment;
    }

    static int GetBomAssignment(byte[] bytes)
    {
        if (bytes.Length >= 4
            && bytes[0] == 0x00
            && bytes[1] == 0x00
            && bytes[2] == 0xFE
            && bytes[3] == 0xFF)
        {
            return QrEciAssignments.Utf32BigEndian;
        }

        if (bytes.Length >= 4
            && bytes[0] == 0xFF
            && bytes[1] == 0xFE
            && bytes[2] == 0x00
            && bytes[3] == 0x00)
        {
            return QrEciAssignments.Utf32LittleEndian;
        }

        if (bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF)
        {
            return QrEciAssignments.Utf8;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return QrEciAssignments.Utf16BigEndian;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return QrEciAssignments.Utf16LittleEndian;
        }

        return -1;
    }

    static int GetUtf8MultibyteCount(byte[] bytes)
    {
        int index = 0;
        int multibyteCount = 0;
        while (index < bytes.Length)
        {
            int firstByte = bytes[index];
            if (firstByte <= 0x7F)
            {
                index++;
                continue;
            }

            if (firstByte >= 0xC2 && firstByte <= 0xDF)
            {
                if (index + 1 >= bytes.Length || !IsContinuationByte(bytes[index + 1]))
                {
                    return -1;
                }

                index += 2;
                multibyteCount++;
                continue;
            }

            if (firstByte >= 0xE0 && firstByte <= 0xEF)
            {
                if (index + 2 >= bytes.Length)
                {
                    return -1;
                }

                int secondByte = bytes[index + 1];
                if (!IsContinuationByte(secondByte)
                    || !IsContinuationByte(bytes[index + 2])
                    || firstByte == 0xE0 && secondByte < 0xA0
                    || firstByte == 0xED && secondByte > 0x9F)
                {
                    return -1;
                }

                index += 3;
                multibyteCount++;
                continue;
            }

            if (firstByte >= 0xF0 && firstByte <= 0xF4)
            {
                if (index + 3 >= bytes.Length)
                {
                    return -1;
                }

                int secondByte = bytes[index + 1];
                if (!IsContinuationByte(secondByte)
                    || !IsContinuationByte(bytes[index + 2])
                    || !IsContinuationByte(bytes[index + 3])
                    || firstByte == 0xF0 && secondByte < 0x90
                    || firstByte == 0xF4 && secondByte > 0x8F)
                {
                    return -1;
                }

                index += 4;
                multibyteCount++;
                continue;
            }

            return -1;
        }

        return multibyteCount;
    }

    static bool IsContinuationByte(int value)
    {
        return value >= 0x80 && value <= 0xBF;
    }

    static int GetShiftJisDoubleByteCount(byte[] bytes)
    {
        int index = 0;
        int doubleByteCount = 0;
        while (index < bytes.Length)
        {
            int firstByte = bytes[index];
            if (firstByte <= 0x1F || firstByte == 0x7F)
            {
                index++;
                continue;
            }

            if (ShiftJisTable.GetStandardUnicode(firstByte) != ShiftJisTable.NotMapped)
            {
                index++;
                continue;
            }

            if (index + 1 >= bytes.Length)
            {
                return -1;
            }

            int encodedValue = firstByte << 8 | bytes[index + 1];
            if (ShiftJisTable.GetStandardUnicode(encodedValue) == ShiftJisTable.NotMapped)
            {
                return -1;
            }

            index += 2;
            doubleByteCount++;
        }

        return doubleByteCount;
    }

    static int GetGb2312DoubleByteCount(byte[] bytes)
    {
        int index = 0;
        int doubleByteCount = 0;
        while (index < bytes.Length)
        {
            int firstByte = bytes[index];
            if (firstByte <= 0x7F)
            {
                index++;
                continue;
            }

            if (index + 1 >= bytes.Length)
            {
                return -1;
            }

            int encodedValue = firstByte << 8 | bytes[index + 1];
            if (Gb2312Table.GetStandardUnicode(encodedValue) == Gb2312Table.NotMapped)
            {
                return -1;
            }

            index += 2;
            doubleByteCount++;
        }

        return doubleByteCount;
    }
}
