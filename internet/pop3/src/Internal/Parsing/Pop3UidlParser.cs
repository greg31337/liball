namespace pop3;

internal class Pop3UidlParser
{
    internal Pop3UniqueId ParseSingleUniqueIdResponse(System.ReadOnlySpan<byte> payload)
    {
        int separator = FindSeparator(payload);
        int uniqueIdEnd = FindTokenEnd(payload, separator + 1);
        Pop3UniqueId result = ParseUniqueId(payload.Slice(0, uniqueIdEnd));
        ValidateTrailingText(payload, uniqueIdEnd);
        return result;
    }

    internal Pop3UniqueId ParseUniqueId(System.ReadOnlySpan<byte> payload)
    {
        int separator = FindSeparator(payload);
        System.ReadOnlySpan<byte> number = payload.Slice(0, separator);
        System.ReadOnlySpan<byte> uniqueId = payload.Slice(separator + 1, payload.Length - separator - 1);
        if (uniqueId.Length < 1 || uniqueId.Length > 70)
        {
            throw new System.IO.InvalidDataException("A POP3 unique-id must contain from one to 70 octets.");
        }

        int index = 0;
        while (index < uniqueId.Length)
        {
            if (uniqueId[index] < 33 || uniqueId[index] > 126)
            {
                throw new System.IO.InvalidDataException("A POP3 unique-id must contain only visible ASCII characters.");
            }
            index++;
        }

        Pop3UniqueId result = new Pop3UniqueId();
        result.MessageNumber = ParseMessageNumber(number);
        result.Value = System.Text.Encoding.ASCII.GetString(uniqueId);
        return result;
    }

    internal Pop3UniqueId[] ParseUniqueIds(byte[][] lines, Pop3MessageInfo[]? expectedMessages)
    {
        if (lines == null)
        {
            throw new System.ArgumentNullException("lines");
        }
        if (expectedMessages != null && expectedMessages.Length != lines.Length)
        {
            throw new System.IO.InvalidDataException("The POP3 UIDL entry count does not match the LIST entry count.");
        }

        Pop3UniqueId[] uniqueIds = new Pop3UniqueId[lines.Length];
        int index = 0;
        while (index < lines.Length)
        {
            if (lines[index] == null)
            {
                throw new System.IO.InvalidDataException("A POP3 UIDL line cannot be null.");
            }
            Pop3UniqueId uniqueId = ParseUniqueId(lines[index]);
            int previousIndex = 0;
            while (previousIndex < index)
            {
                if (uniqueIds[previousIndex].MessageNumber == uniqueId.MessageNumber)
                {
                    throw new System.IO.InvalidDataException("POP3 UIDL message numbers must be unique.");
                }
                previousIndex++;
            }
            if (expectedMessages != null)
            {
                bool foundExpectedMessage = false;
                int expectedIndex = 0;
                while (expectedIndex < expectedMessages.Length)
                {
                    if (expectedMessages[expectedIndex] == null)
                    {
                        throw new System.IO.InvalidDataException("An expected POP3 LIST entry cannot be null.");
                    }
                    if (expectedMessages[expectedIndex].MessageNumber == uniqueId.MessageNumber)
                    {
                        foundExpectedMessage = true;
                    }
                    expectedIndex++;
                }
                if (!foundExpectedMessage)
                {
                    throw new System.IO.InvalidDataException("The POP3 UIDL message numbers do not match the LIST entries.");
                }
            }
            uniqueIds[index] = uniqueId;
            index++;
        }
        return uniqueIds;
    }

    int FindSeparator(System.ReadOnlySpan<byte> payload)
    {
        int index = 0;
        while (index < payload.Length && payload[index] != 32)
        {
            index++;
        }
        if (index == 0 || index == payload.Length || index == payload.Length - 1)
        {
            throw new System.IO.InvalidDataException("A POP3 UIDL payload requires a message number and unique-id separated by one space.");
        }
        if (payload[index + 1] == 32)
        {
            throw new System.IO.InvalidDataException("A POP3 UIDL payload contains invalid spacing.");
        }
        return index;
    }

    int FindTokenEnd(System.ReadOnlySpan<byte> payload, int start)
    {
        int index = start;
        while (index < payload.Length && payload[index] != 32)
        {
            index++;
        }
        return index;
    }

    void ValidateTrailingText(System.ReadOnlySpan<byte> payload, int tokenEnd)
    {
        int index = tokenEnd;
        if (index < payload.Length)
        {
            index++;
        }
        while (index < payload.Length)
        {
            if (payload[index] < 32 || payload[index] == 127)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 UIDL response contains invalid response text.");
            }
            index++;
        }
    }

    int ParseMessageNumber(System.ReadOnlySpan<byte> field)
    {
        int index = 0;
        while (index < field.Length)
        {
            if (field[index] < 48 || field[index] > 57)
            {
                throw new System.IO.InvalidDataException("A POP3 message number must contain only decimal digits.");
            }
            index++;
        }

        Pop3NumberParseResult parsed = new Pop3NumberParser().ParseSignedDecimal(field, 1, int.MaxValue);
        if (!parsed.Success)
        {
            throw new System.IO.InvalidDataException("A POP3 message number is outside its permitted range.");
        }
        return (int)parsed.Value;
    }
}
