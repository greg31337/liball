using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace jmap;

internal class DnsSrvParser
{
    internal const int HeaderLength = 12;
    internal const int SrvRecordType = 33;
    internal const int InternetClass = 1;

    internal DnsSrvResponse ParseSrvResponse(byte[]? message)
    {
        if (message == null || message.Length < HeaderLength)
        {
            throw ProtocolFailure("The DNS response is shorter than its header.");
        }

        int flags = ReadUnsignedShort(message, 2);
        if ((flags & 32768) == 0)
        {
            throw ProtocolFailure("The DNS packet is not a response.");
        }

        if ((flags & 512) != 0)
        {
            throw ProtocolFailure("A truncated DNS response must be retried over TCP.");
        }

        DnsSrvResponse response = new DnsSrvResponse();
        response.ResponseCode = flags & 15;
        int questionCount = ReadUnsignedShort(message, 4);
        int answerCount = ReadUnsignedShort(message, 6);
        int authorityCount = ReadUnsignedShort(message, 8);
        int additionalCount = ReadUnsignedShort(message, 10);
        int offset = HeaderLength;
        int questionIndex = 0;
        while (questionIndex < questionCount)
        {
            DnsNameResult questionName = ReadName(message, offset);
            offset = questionName.NextOffset;
            RequireAvailable(message, offset, 4);
            offset = offset + 4;
            questionIndex = questionIndex + 1;
        }

        int answerIndex = 0;
        while (answerIndex < answerCount)
        {
            DnsNameResult owner = ReadName(message, offset);
            offset = owner.NextOffset;
            RequireAvailable(message, offset, 10);
            int recordType = ReadUnsignedShort(message, offset);
            int recordClass = ReadUnsignedShort(message, offset + 2);
            int dataLength = ReadUnsignedShort(message, offset + 8);
            int dataOffset = offset + 10;
            RequireAvailable(message, dataOffset, dataLength);
            if (recordType == SrvRecordType && recordClass == InternetClass)
            {
                response.Records.Add(ParseRecord(message, dataOffset, dataLength));
            }

            offset = dataOffset + dataLength;
            answerIndex = answerIndex + 1;
        }

        int remainingIndex = 0;
        int remainingCount = authorityCount + additionalCount;
        while (remainingIndex < remainingCount)
        {
            offset = SkipRecord(message, offset);
            remainingIndex = remainingIndex + 1;
        }

        if (offset != message.Length)
        {
            throw ProtocolFailure("The DNS response contains trailing data outside its declared records.");
        }

        return response;
    }

    internal int SkipRecord(byte[] message, int offset)
    {
        DnsNameResult owner = ReadName(message, offset);
        RequireAvailable(message, owner.NextOffset, 10);
        int dataLength = ReadUnsignedShort(message, owner.NextOffset + 8);
        int nextOffset = owner.NextOffset + 10;
        RequireAvailable(message, nextOffset, dataLength);
        return nextOffset + dataLength;
    }

    internal DnsSrvRecord ParseRecord(byte[] message, int offset, int length)
    {
        if (length < 7)
        {
            throw ProtocolFailure("A DNS SRV record is shorter than its fixed fields and target.");
        }

        RequireAvailable(message, offset, length);
        DnsNameResult target = ReadName(message, offset + 6);
        if (target.NextOffset != offset + length)
        {
            throw ProtocolFailure("The DNS SRV target does not consume the complete record data.");
        }

        DnsSrvRecord record = new DnsSrvRecord();
        record.Priority = ReadUnsignedShort(message, offset);
        record.Weight = ReadUnsignedShort(message, offset + 2);
        record.Port = ReadUnsignedShort(message, offset + 4);
        record.Target = target.Name;
        return record;
    }

    internal DnsNameResult ReadName(byte[] message, int offset)
    {
        if (offset < 0 || offset >= message.Length)
        {
            throw ProtocolFailure("A DNS name starts outside the response.");
        }

        List<string> labels = new List<string>();
        HashSet<int> visited = new HashSet<int>();
        int current = offset;
        int nextOffset = -1;
        int expandedLength = 1;
        bool complete = false;
        while (!complete)
        {
            if (current < 0 || current >= message.Length || !visited.Add(current))
            {
                throw ProtocolFailure("A DNS compression pointer is invalid or cyclic.");
            }

            int labelLength = message[current];
            if ((labelLength & 192) == 192)
            {
                RequireAvailable(message, current, 2);
                int pointer = ((labelLength & 63) << 8) | message[current + 1];
                if (pointer >= current)
                {
                    throw ProtocolFailure("A DNS compression pointer does not refer to an earlier name.");
                }

                if (nextOffset < 0)
                {
                    nextOffset = current + 2;
                }

                current = pointer;
                continue;
            }

            if ((labelLength & 192) != 0 || labelLength > 63)
            {
                throw ProtocolFailure("A DNS label has an invalid length marker.");
            }

            current = current + 1;
            if (labelLength == 0)
            {
                if (nextOffset < 0)
                {
                    nextOffset = current;
                }

                complete = true;
                continue;
            }

            RequireAvailable(message, current, labelLength);
            int labelIndex = 0;
            while (labelIndex < labelLength)
            {
                if (message[current + labelIndex] > 127)
                {
                    throw ProtocolFailure("A DNS label contains a non-ASCII wire octet.");
                }

                labelIndex = labelIndex + 1;
            }

            expandedLength = expandedLength + labelLength + 1;
            if (expandedLength > 255)
            {
                throw ProtocolFailure("A decompressed DNS name exceeds 255 octets.");
            }

            labels.Add(Encoding.ASCII.GetString(message, current, labelLength));
            current = current + labelLength;
        }

        DnsNameResult result = new DnsNameResult();
        result.Name = labels.Count == 0 ? "." : string.Join(".", labels);
        result.NextOffset = nextOffset;
        return result;
    }

    internal int ReadUnsignedShort(byte[] message, int offset)
    {
        RequireAvailable(message, offset, 2);
        return (message[offset] << 8) | message[offset + 1];
    }

    internal void RequireAvailable(byte[] message, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > message.Length - length)
        {
            throw ProtocolFailure("A DNS field extends beyond the response.");
        }
    }

    internal JmapProtocolException ProtocolFailure(string message)
    {
        return new JmapProtocolException(message, "dns", default(JsonElement));
    }
}
