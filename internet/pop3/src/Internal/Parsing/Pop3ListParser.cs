namespace pop3;

internal class Pop3ListParser
{
    internal Pop3MailboxStatus ParseMailboxStatus(System.ReadOnlySpan<byte> payload)
    {
        int separator = FindRequiredSeparator(payload);
        int secondEnd = FindTokenEnd(payload, separator + 1);

        Pop3MailboxStatus status = new Pop3MailboxStatus();
        status.MessageCount = (int)ParseUnsignedField(payload.Slice(0, separator), 0, int.MaxValue, "message count");
        status.TotalOctets = ParseUnsignedField(
            payload.Slice(separator + 1, secondEnd - separator - 1),
            0,
            long.MaxValue,
            "maildrop octet count");
        ValidateTrailingText(payload, secondEnd);
        return status;
    }

    internal Pop3MessageInfo ParseListEntry(System.ReadOnlySpan<byte> payload)
    {
        int separator = FindRequiredSeparator(payload);
        int secondEnd = FindTokenEnd(payload, separator + 1);

        Pop3MessageInfo message = new Pop3MessageInfo();
        message.MessageNumber = (int)ParseUnsignedField(
            payload.Slice(0, separator),
            1,
            int.MaxValue,
            "message number");
        message.OctetCount = ParseUnsignedField(
            payload.Slice(separator + 1, secondEnd - separator - 1),
            0,
            long.MaxValue,
            "message octet count");
        ValidateTrailingText(payload, secondEnd);
        return message;
    }

    internal Pop3MessageInfo[] ParseListEntries(byte[][] lines, Pop3MailboxStatus? expectedStatus)
    {
        if (lines == null)
        {
            throw new System.ArgumentNullException("lines");
        }
        if (expectedStatus != null)
        {
            if (expectedStatus.MessageCount < 0 || expectedStatus.TotalOctets < 0)
            {
                throw new System.ArgumentOutOfRangeException("expectedStatus");
            }
            if (expectedStatus.MessageCount != lines.Length)
            {
                throw new System.IO.InvalidDataException("The POP3 LIST entry count does not match the expected mailbox count.");
            }
        }

        Pop3MessageInfo[] messages = new Pop3MessageInfo[lines.Length];
        long totalOctets = 0;
        int index = 0;
        while (index < lines.Length)
        {
            if (lines[index] == null)
            {
                throw new System.IO.InvalidDataException("A POP3 LIST line cannot be null.");
            }
            Pop3MessageInfo message = ParseListEntry(lines[index]);
            int previousIndex = 0;
            while (previousIndex < index)
            {
                if (messages[previousIndex].MessageNumber == message.MessageNumber)
                {
                    throw new System.IO.InvalidDataException("POP3 LIST message numbers must be unique.");
                }
                previousIndex++;
            }
            if (totalOctets > long.MaxValue - message.OctetCount)
            {
                throw new System.IO.InvalidDataException("The POP3 LIST octet total exceeds Int64.");
            }
            messages[index] = message;
            totalOctets += message.OctetCount;
            index++;
        }

        if (expectedStatus != null && totalOctets != expectedStatus.TotalOctets)
        {
            throw new System.IO.InvalidDataException("The POP3 LIST octet total does not match the expected mailbox size.");
        }
        return messages;
    }

    int FindRequiredSeparator(System.ReadOnlySpan<byte> payload)
    {
        int index = 0;
        while (index < payload.Length && payload[index] != 32)
        {
            index++;
        }
        if (index == 0 ||
            index == payload.Length ||
            index == payload.Length - 1 ||
            payload[index + 1] == 32)
        {
            throw new System.IO.InvalidDataException("A POP3 numeric payload requires two fields separated by one space.");
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
        if (index == start)
        {
            throw new System.IO.InvalidDataException("A POP3 numeric payload contains an empty field.");
        }
        return index;
    }

    long ParseUnsignedField(System.ReadOnlySpan<byte> field, long minimum, long maximum, string fieldName)
    {
        int index = 0;
        while (index < field.Length)
        {
            if (field[index] < 48 || field[index] > 57)
            {
                throw new System.IO.InvalidDataException("A POP3 " + fieldName + " must contain only decimal digits.");
            }
            index++;
        }

        Pop3NumberParseResult result = new Pop3NumberParser().ParseSignedDecimal(field, minimum, maximum);
        if (!result.Success)
        {
            throw new System.IO.InvalidDataException("A POP3 " + fieldName + " is outside its permitted range.");
        }
        return result.Value;
    }

    void ValidateTrailingText(System.ReadOnlySpan<byte> payload, int tokenEnd)
    {
        if (tokenEnd == payload.Length)
        {
            return;
        }
        if (payload[tokenEnd] != 32)
        {
            throw new System.IO.InvalidDataException("A POP3 numeric payload has invalid trailing information.");
        }

        int index = tokenEnd + 1;
        while (index < payload.Length)
        {
            if (payload[index] < 32 || payload[index] == 127)
            {
                throw new System.IO.InvalidDataException("A POP3 numeric payload contains a control octet.");
            }
            index++;
        }
    }
}
