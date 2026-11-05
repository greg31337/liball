namespace sip;

internal static class SipDnsWireCodec
{
    internal static byte[] CreateQuery(string? name, SipDnsRecordKind kind, int transactionId)
    {
        string? normalized = NormalizeQueryName(name);
        int recordType = GetRecordType(kind);
        if (normalized == null)
        {
            throw new System.ArgumentException("A valid DNS query name is required.", "name");
        }
        if (recordType < 0)
        {
            throw new System.ArgumentException("The DNS record kind is not queryable.", "kind");
        }
        if (transactionId < 0 || transactionId > 65535)
        {
            throw new System.ArgumentOutOfRangeException("transactionId");
        }

        System.Collections.Generic.List<byte> bytes = new System.Collections.Generic.List<byte>();
        AppendUInt16(bytes, transactionId);
        AppendUInt16(bytes, 256);
        AppendUInt16(bytes, 1);
        AppendUInt16(bytes, 0);
        AppendUInt16(bytes, 0);
        AppendUInt16(bytes, 0);
        AppendName(bytes, normalized);
        AppendUInt16(bytes, recordType);
        AppendUInt16(bytes, 1);
        return bytes.ToArray();
    }

    internal static SipDnsWireResult ParseResponse(byte[]? packet, int transactionId,
        string? questionName, SipDnsRecordKind questionKind, int maximumRecordCount)
    {
        string? normalizedQuestion = NormalizeQueryName(questionName);
        int expectedType = GetRecordType(questionKind);
        if (packet == null)
        {
            return CreateError(0, "A DNS response packet is required.");
        }
        if (packet.Length < 12)
        {
            return CreateError(packet.Length, "The DNS response header is truncated.");
        }
        if (packet.Length > 65535)
        {
            return CreateError(65535, "The DNS response exceeds the wire message limit.");
        }
        if (transactionId < 0 || transactionId > 65535 || normalizedQuestion == null
            || expectedType < 0 || maximumRecordCount < 0)
        {
            return CreateError(0, "The DNS response matching inputs are invalid.");
        }

        int responseId = ReadUInt16(packet, 0);
        int flags = ReadUInt16(packet, 2);
        int questionCount = ReadUInt16(packet, 4);
        int answerCount = ReadUInt16(packet, 6);
        int authorityCount = ReadUInt16(packet, 8);
        int additionalCount = ReadUInt16(packet, 10);
        if (responseId != transactionId)
        {
            return CreateUnrelatedError(0,
                "The DNS response transaction identifier does not match.");
        }
        if ((flags & 32768) == 0)
        {
            return CreateError(2, "The DNS packet is not a response.");
        }
        if ((flags & 30720) != 0 || (flags & 64) != 0)
        {
            return CreateError(2, "The DNS response contains unsupported header flags.");
        }
        if (questionCount != 1)
        {
            return CreateError(4, "The DNS response must repeat exactly one question.");
        }
        int totalRecords = answerCount + authorityCount + additionalCount;

        int offset = 12;
        SipDnsNameReadResult question = ReadName(packet, offset, packet.Length);
        if (!question.Success || question.Name == null)
        {
            return CreateError(question.ErrorOffset, question.ErrorMessage);
        }
        offset = question.NextOffset;
        if (offset + 4 > packet.Length)
        {
            return CreateError(offset, "The DNS response question is truncated.");
        }
        int questionType = ReadUInt16(packet, offset);
        int questionClass = ReadUInt16(packet, offset + 2);
        offset = offset + 4;
        if (!SipAscii.EqualsIgnoreCase(question.Name, normalizedQuestion)
            || questionType != expectedType || questionClass != 1)
        {
            return CreateUnrelatedError(12,
                "The DNS response question does not match the query.");
        }
        if (totalRecords > maximumRecordCount)
        {
            return CreateError(6,
                "The DNS response exceeds the configured record limit.");
        }

        SipDnsWireResult result = new SipDnsWireResult();
        result.Success = true;
        result.Truncated = (flags & 512) != 0;
        result.Authoritative = (flags & 1024) != 0;
        result.RecursionAvailable = (flags & 128) != 0;
        result.ResponseCode = flags & 15;
        if (result.Truncated)
        {
            return result;
        }

        System.Collections.Generic.List<SipDnsWireRecord> records =
            new System.Collections.Generic.List<SipDnsWireRecord>();
        int recordIndex = 0;
        while (recordIndex < totalRecords)
        {
            SipDnsNameReadResult owner = ReadName(packet, offset, packet.Length);
            if (!owner.Success || owner.Name == null)
            {
                return CreateError(owner.ErrorOffset, owner.ErrorMessage);
            }
            offset = owner.NextOffset;
            if (offset + 10 > packet.Length)
            {
                return CreateError(offset, "A DNS resource record header is truncated.");
            }
            int recordType = ReadUInt16(packet, offset);
            int recordClass = ReadUInt16(packet, offset + 2);
            long ttl = ReadUInt32(packet, offset + 4);
            int dataLength = ReadUInt16(packet, offset + 8);
            int dataStart = offset + 10;
            int dataEnd = dataStart + dataLength;
            if (dataEnd > packet.Length)
            {
                return CreateError(dataStart, "A DNS resource record value is truncated.");
            }

            int section = recordIndex < answerCount ? 0
                : recordIndex < answerCount + authorityCount ? 1 : 2;
            SipDnsWireRecord? record = recordClass == 1
                ? ParseRecord(packet, owner.Name, recordType, ttl, section, dataStart, dataEnd)
                : null;
            if (recordClass == 1 && IsSupportedRecordType(recordType) && record == null)
            {
                return CreateError(dataStart, "A supported DNS resource record value is invalid.");
            }
            if (record != null)
            {
                records.Add(record);
            }
            offset = dataEnd;
            recordIndex = recordIndex + 1;
        }
        if (offset != packet.Length)
        {
            return CreateError(offset, "The DNS response has trailing bytes outside its records.");
        }
        result.Records = records.ToArray();
        return result;
    }

    static SipDnsWireRecord? ParseRecord(byte[] packet, string owner, int recordType,
        long ttl, int section, int dataStart, int dataEnd)
    {
        SipDnsWireRecord? record = null;
        if (recordType == 1 && dataEnd - dataStart == 4)
        {
            byte[] address = CopyRange(packet, dataStart, 4);
            record = CreateRecord(owner, SipDnsRecordKind.A, ttl, section);
            record.Address = new System.Net.IPAddress(address).ToString();
        }
        else if (recordType == 28 && dataEnd - dataStart == 16)
        {
            byte[] address = CopyRange(packet, dataStart, 16);
            record = CreateRecord(owner, SipDnsRecordKind.Aaaa, ttl, section);
            record.Address = new System.Net.IPAddress(address).ToString();
        }
        else if (recordType == 5)
        {
            SipDnsNameReadResult name = ReadName(packet, dataStart, dataEnd);
            if (name.Success && name.Name != null && name.NextOffset == dataEnd)
            {
                record = CreateRecord(owner, SipDnsRecordKind.Cname, ttl, section);
                record.Target = name.Name;
            }
        }
        else if (recordType == 33 && dataEnd - dataStart >= 7)
        {
            SipDnsNameReadResult target = ReadName(packet, dataStart + 6, dataEnd);
            if (target.Success && target.Name != null && target.NextOffset == dataEnd)
            {
                record = CreateRecord(owner, SipDnsRecordKind.Srv, ttl, section);
                record.Priority = ReadUInt16(packet, dataStart);
                record.Weight = ReadUInt16(packet, dataStart + 2);
                record.Port = ReadUInt16(packet, dataStart + 4);
                record.Target = target.Name;
            }
        }
        else if (recordType == 35 && dataEnd - dataStart >= 7)
        {
            SipDnsTextReadResult flags = ReadCharacterString(packet, dataStart + 4, dataEnd);
            SipDnsTextReadResult services = flags.Success
                ? ReadCharacterString(packet, flags.NextOffset, dataEnd) : CreateTextError(
                    flags.ErrorOffset, flags.ErrorMessage);
            SipDnsTextReadResult regexp = services.Success
                ? ReadCharacterString(packet, services.NextOffset, dataEnd) : CreateTextError(
                    services.ErrorOffset, services.ErrorMessage);
            SipDnsNameReadResult replacement = regexp.Success
                ? ReadName(packet, regexp.NextOffset, dataEnd) : CreateNameError(
                    regexp.ErrorOffset, regexp.ErrorMessage);
            if (flags.Success && services.Success && regexp.Success && replacement.Success
                && replacement.Name != null && replacement.NextOffset == dataEnd)
            {
                record = CreateRecord(owner, SipDnsRecordKind.Naptr, ttl, section);
                record.NaptrOrder = ReadUInt16(packet, dataStart);
                record.NaptrPreference = ReadUInt16(packet, dataStart + 2);
                record.NaptrFlags = flags.Text;
                record.NaptrServices = services.Text;
                record.NaptrRegexp = regexp.Text;
                record.NaptrReplacement = replacement.Name;
            }
        }
        return record;
    }

    static SipDnsWireRecord CreateRecord(string owner, SipDnsRecordKind kind,
        long ttl, int section)
    {
        SipDnsWireRecord record = new SipDnsWireRecord();
        record.Name = owner;
        record.Kind = kind;
        record.TtlSeconds = ttl;
        record.Section = section;
        return record;
    }

    static SipDnsNameReadResult ReadName(byte[] packet, int start, int initialLimit)
    {
        if (start < 0 || start >= initialLimit || initialLimit > packet.Length)
        {
            return CreateNameError(start, "A DNS name starts outside its containing field.");
        }
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int cursor = start;
        int nextOffset = -1;
        int pointerCount = 0;
        int expandedBytes = 1;
        bool jumped = false;
        while (true)
        {
            int limit = jumped ? packet.Length : initialLimit;
            if (cursor >= limit)
            {
                return CreateNameError(cursor, "A DNS name is truncated.");
            }
            int length = packet[cursor];
            if ((length & 192) == 192)
            {
                if (cursor + 1 >= limit)
                {
                    return CreateNameError(cursor, "A DNS compression pointer is truncated.");
                }
                int pointer = (length & 63) * 256 + packet[cursor + 1];
                if (pointer < 12 || pointer >= cursor
                    || pointer >= packet.Length)
                {
                    return CreateNameError(cursor,
                        "A DNS compression pointer must point backward into the DNS name area.");
                }
                if (nextOffset < 0)
                {
                    nextOffset = cursor + 2;
                }
                cursor = pointer;
                jumped = true;
                pointerCount = pointerCount + 1;
                if (pointerCount > 128)
                {
                    return CreateNameError(cursor, "A DNS name has too many compression pointers.");
                }
                continue;
            }
            if ((length & 192) != 0 || length > 63)
            {
                return CreateNameError(cursor, "A DNS name label has an invalid length.");
            }
            cursor = cursor + 1;
            if (length == 0)
            {
                if (nextOffset < 0)
                {
                    nextOffset = cursor;
                }
                SipDnsNameReadResult success = new SipDnsNameReadResult();
                success.Success = true;
                success.Name = builder.Length == 0 ? "." : builder.ToString();
                success.NextOffset = nextOffset;
                return success;
            }
            if (cursor + length > limit)
            {
                return CreateNameError(cursor, "A DNS name label is truncated.");
            }
            expandedBytes = expandedBytes + length + 1;
            if (expandedBytes > 255)
            {
                return CreateNameError(cursor, "A DNS name exceeds 255 wire bytes.");
            }
            if (builder.Length > 0)
            {
                builder.Append('.');
            }
            int labelIndex = 0;
            while (labelIndex < length)
            {
                int character = packet[cursor + labelIndex];
                if (character < 33 || character > 126 || character == '.')
                {
                    return CreateNameError(cursor + labelIndex,
                        "A DNS name contains an unsupported label character.");
                }
                builder.Append((char)character);
                labelIndex = labelIndex + 1;
            }
            cursor = cursor + length;
        }
    }

    static SipDnsTextReadResult ReadCharacterString(byte[] packet, int start, int limit)
    {
        if (start < 0 || start >= limit || limit > packet.Length)
        {
            return CreateTextError(start, "A DNS character-string is truncated.");
        }
        int length = packet[start];
        if (start + 1 + length > limit)
        {
            return CreateTextError(start, "A DNS character-string exceeds its record value.");
        }
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < length)
        {
            builder.Append((char)packet[start + 1 + index]);
            index = index + 1;
        }
        SipDnsTextReadResult result = new SipDnsTextReadResult();
        result.Success = true;
        result.Text = builder.ToString();
        result.NextOffset = start + 1 + length;
        return result;
    }

    internal static string? NormalizeQueryName(string? name)
    {
        if (name == null)
        {
            return null;
        }
        string normalized = name;
        if (normalized == ".")
        {
            return normalized;
        }
        if (normalized.Length > 0 && normalized[normalized.Length - 1] == '.')
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }
        if (normalized.Length == 0 || normalized.Length > 253)
        {
            return null;
        }
        string[] labels = normalized.Split('.');
        int labelIndex = 0;
        while (labelIndex < labels.Length)
        {
            string label = labels[labelIndex];
            if (label.Length == 0 || label.Length > 63)
            {
                return null;
            }
            int index = 0;
            while (index < label.Length)
            {
                int character = label[index];
                if (character > 127 || character <= 32 || character == '.')
                {
                    return null;
                }
                index = index + 1;
            }
            labelIndex = labelIndex + 1;
        }
        return normalized.ToLowerInvariant();
    }

    static void AppendName(System.Collections.Generic.List<byte> bytes, string name)
    {
        if (name == ".")
        {
            bytes.Add(0);
            return;
        }
        string[] labels = name.Split('.');
        int labelIndex = 0;
        while (labelIndex < labels.Length)
        {
            byte[] label = System.Text.Encoding.ASCII.GetBytes(labels[labelIndex]);
            bytes.Add((byte)label.Length);
            int index = 0;
            while (index < label.Length)
            {
                bytes.Add(label[index]);
                index = index + 1;
            }
            labelIndex = labelIndex + 1;
        }
        bytes.Add(0);
    }

    static int GetRecordType(SipDnsRecordKind kind)
    {
        switch (kind)
        {
            case SipDnsRecordKind.A: return 1;
            case SipDnsRecordKind.Cname: return 5;
            case SipDnsRecordKind.Aaaa: return 28;
            case SipDnsRecordKind.Srv: return 33;
            case SipDnsRecordKind.Naptr: return 35;
            default: return -1;
        }
    }

    static bool IsSupportedRecordType(int recordType)
    {
        return recordType == 1 || recordType == 5 || recordType == 28
            || recordType == 33 || recordType == 35;
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] * 256 + bytes[offset + 1];
    }

    static long ReadUInt32(byte[] bytes, int offset)
    {
        return (long)bytes[offset] * 16777216 + (long)bytes[offset + 1] * 65536
            + (long)bytes[offset + 2] * 256 + bytes[offset + 3];
    }

    static void AppendUInt16(System.Collections.Generic.List<byte> bytes, int value)
    {
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)(value & 255));
    }

    static byte[] CopyRange(byte[] source, int start, int length)
    {
        byte[] result = new byte[length];
        System.Array.Copy(source, start, result, 0, length);
        return result;
    }

    static SipDnsWireResult CreateError(int offset, string? message)
    {
        SipDnsWireResult result = new SipDnsWireResult();
        result.ErrorOffset = offset;
        result.ErrorMessage = message == null ? "The DNS response is invalid." : message;
        return result;
    }

    static SipDnsWireResult CreateUnrelatedError(int offset, string message)
    {
        SipDnsWireResult result = CreateError(offset, message);
        result.Unrelated = true;
        return result;
    }

    static SipDnsNameReadResult CreateNameError(int offset, string? message)
    {
        SipDnsNameReadResult result = new SipDnsNameReadResult();
        result.ErrorOffset = offset;
        result.ErrorMessage = message == null ? "The DNS name is invalid." : message;
        return result;
    }

    static SipDnsTextReadResult CreateTextError(int offset, string? message)
    {
        SipDnsTextReadResult result = new SipDnsTextReadResult();
        result.ErrorOffset = offset;
        result.ErrorMessage = message == null ? "The DNS text is invalid." : message;
        return result;
    }
}
