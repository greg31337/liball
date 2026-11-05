using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace diameter;

internal static class DnsMessageCodec
{
    internal const int Ipv4RecordType = 1;
    internal const int SoaRecordType = 6;
    internal const int Ipv6RecordType = 28;
    internal const int SrvRecordType = 33;
    internal const int NaptrRecordType = 35;
    internal const int InternetClass = 1;
    internal const int HeaderLength = 12;
    internal const int MaximumNameLength = 255;
    internal const int MaximumLabelLength = 63;
    internal const int MaximumRecordCount = 512;
    internal const int MaximumPointerCount = 64;

    internal static byte[] EncodeDnsQuery(string name, int recordType, int identifier)
    {
        if (recordType != Ipv4RecordType && recordType != Ipv6RecordType &&
            recordType != SrvRecordType && recordType != NaptrRecordType)
        {
            throw new ArgumentOutOfRangeException("recordType");
        }

        if (identifier < 0 || identifier > 65535)
        {
            throw new ArgumentOutOfRangeException("identifier");
        }

        string normalizedName = NormalizeDnsQueryName(name);
        byte[] encodedName = EncodeDnsName(normalizedName);
        byte[] result = new byte[HeaderLength + encodedName.Length + 4];
        WriteDnsUInt16(result, 0, identifier);
        WriteDnsUInt16(result, 2, 256);
        WriteDnsUInt16(result, 4, 1);
        Buffer.BlockCopy(encodedName, 0, result, HeaderLength, encodedName.Length);
        int questionOffset = HeaderLength + encodedName.Length;
        WriteDnsUInt16(result, questionOffset, recordType);
        WriteDnsUInt16(result, questionOffset + 2, InternetClass);
        return result;
    }

    internal static DnsResponseHeader ReadDnsResponseHeader(
        ReadOnlySpan<byte> source,
        int expectedIdentifier,
        string expectedName,
        int expectedType)
    {
        if (source.Length < HeaderLength)
        {
            throw new DiameterCodecException("The DNS response header is truncated.");
        }

        int identifier = ReadDnsUInt16(source, 0);
        int flags = ReadDnsUInt16(source, 2);
        int questionCount = ReadDnsUInt16(source, 4);
        int answerCount = ReadDnsUInt16(source, 6);
        int authorityCount = ReadDnsUInt16(source, 8);
        int additionalCount = ReadDnsUInt16(source, 10);
        if (identifier != expectedIdentifier)
        {
            throw new DiameterCodecException("The DNS response identifier does not match the query.");
        }

        if ((flags & 32768) == 0 || ((flags >> 11) & 15) != 0)
        {
            throw new DiameterCodecException("The DNS response header is not a standard response.");
        }

        if (questionCount != 1)
        {
            throw new DiameterCodecException("The DNS response must repeat exactly one question.");
        }

        long recordCount = (long)answerCount + authorityCount + additionalCount;
        if (recordCount > MaximumRecordCount)
        {
            throw new DiameterCodecException("The DNS response contains too many records.");
        }

        DnsNameResult questionName = ReadDnsName(source, HeaderLength);
        if (questionName.NextOffset + 4 > source.Length)
        {
            throw new DiameterCodecException("The DNS response question is truncated.");
        }

        int questionType = ReadDnsUInt16(source, questionName.NextOffset);
        int questionClass = ReadDnsUInt16(source, questionName.NextOffset + 2);
        string normalizedExpectedName = NormalizeDnsQueryName(expectedName);
        if (!string.Equals(
            questionName.Name,
            normalizedExpectedName,
            StringComparison.OrdinalIgnoreCase) ||
            questionType != expectedType || questionClass != InternetClass)
        {
            throw new DiameterCodecException("The DNS response question does not match the query.");
        }

        DnsResponseHeader result = new DnsResponseHeader();
        result.Flags = flags;
        result.AnswerCount = answerCount;
        result.AuthorityCount = authorityCount;
        result.AdditionalCount = additionalCount;
        result.RecordOffset = questionName.NextOffset + 4;
        return result;
    }

    internal static DiameterDnsQueryResult DecodeDnsResponse(
        ReadOnlySpan<byte> source,
        int expectedIdentifier,
        string expectedName,
        int expectedType,
        DateTimeOffset receivedAt)
    {
        DnsResponseHeader header = ReadDnsResponseHeader(
            source,
            expectedIdentifier,
            expectedName,
            expectedType);
        if ((header.Flags & 512) != 0)
        {
            throw new DiameterCodecException("The truncated DNS response requires a TCP retry.");
        }

        DiameterDnsQueryResult result = new DiameterDnsQueryResult();
        result.QueryName = NormalizeDnsQueryName(expectedName);
        result.RecordType = expectedType;
        result.ResponseCode = header.Flags & 15;
        result.Successful = result.ResponseCode == 0;
        result.Authoritative = (header.Flags & 1024) != 0;
        result.RecursionAvailable = (header.Flags & 128) != 0;
        SetDnsOutcome(result);

        int recordCount = header.AnswerCount +
            header.AuthorityCount + header.AdditionalCount;
        int offset = header.RecordOffset;
        int recordIndex = 0;
        while (recordIndex < recordCount)
        {
            offset = DecodeDnsRecord(source, offset, receivedAt, result);
            recordIndex = recordIndex + 1;
        }

        return result;
    }

    internal static int DecodeDnsRecord(
        ReadOnlySpan<byte> source,
        int offset,
        DateTimeOffset receivedAt,
        DiameterDnsQueryResult result)
    {
        DnsNameResult owner = ReadDnsName(source, offset);
        int headerOffset = owner.NextOffset;
        if (headerOffset + 10 > source.Length)
        {
            throw new DiameterCodecException("The DNS resource-record header is truncated.");
        }

        int recordType = ReadDnsUInt16(source, headerOffset);
        int recordClass = ReadDnsUInt16(source, headerOffset + 2);
        uint timeToLive = NetworkByteOrder.ReadUInt32(source.Slice(headerOffset + 4, 4));
        int dataLength = ReadDnsUInt16(source, headerOffset + 8);
        int dataOffset = headerOffset + 10;
        int dataEnd = dataOffset + dataLength;
        if (dataEnd < dataOffset || dataEnd > source.Length)
        {
            throw new DiameterCodecException("The DNS resource-record data is truncated.");
        }

        if (recordClass == InternetClass)
        {
            if (recordType == NaptrRecordType)
            {
                DecodeDnsNaptrRecord(
                    source,
                    owner.Name,
                    dataOffset,
                    dataEnd,
                    timeToLive,
                    receivedAt,
                    result);
            }
            else if (recordType == SrvRecordType)
            {
                DecodeDnsSrvRecord(
                    source,
                    owner.Name,
                    dataOffset,
                    dataEnd,
                    timeToLive,
                    receivedAt,
                    result);
            }
            else if (recordType == Ipv4RecordType || recordType == Ipv6RecordType)
            {
                DecodeDnsAddressRecord(
                    source,
                    owner.Name,
                    recordType,
                    dataOffset,
                    dataLength,
                    timeToLive,
                    receivedAt,
                    result);
            }
            else if (recordType == SoaRecordType)
            {
                DecodeDnsSoaRecord(
                    source,
                    dataOffset,
                    dataEnd,
                    timeToLive,
                    result);
            }
        }

        return dataEnd;
    }

    internal static void DecodeDnsNaptrRecord(
        ReadOnlySpan<byte> source,
        string ownerName,
        int dataOffset,
        int dataEnd,
        uint timeToLive,
        DateTimeOffset receivedAt,
        DiameterDnsQueryResult result)
    {
        if (dataOffset + 4 > dataEnd)
        {
            throw new DiameterCodecException("The DNS NAPTR record is truncated.");
        }

        DiameterNaptrRecord record = new DiameterNaptrRecord();
        record.Name = ownerName;
        record.Order = ReadDnsUInt16(source, dataOffset);
        record.Preference = ReadDnsUInt16(source, dataOffset + 2);
        DnsCharacterStringResult flags = ReadDnsCharacterString(
            source,
            dataOffset + 4,
            dataEnd);
        DnsCharacterStringResult service = ReadDnsCharacterString(
            source,
            flags.NextOffset,
            dataEnd);
        DnsCharacterStringResult expression = ReadDnsCharacterString(
            source,
            service.NextOffset,
            dataEnd);
        DnsNameResult replacement = ReadDnsName(source, expression.NextOffset);
        if (replacement.NextOffset != dataEnd)
        {
            throw new DiameterCodecException("The DNS NAPTR record length is invalid.");
        }

        record.Flags = flags.Value;
        record.Service = service.Value;
        record.RegularExpression = expression.Value;
        record.Replacement = replacement.Name;
        record.TimeToLiveSeconds = timeToLive;
        record.ExpiresAt = receivedAt.AddSeconds(timeToLive);
        result.NaptrRecords.Add(record);
    }

    internal static void DecodeDnsSrvRecord(
        ReadOnlySpan<byte> source,
        string ownerName,
        int dataOffset,
        int dataEnd,
        uint timeToLive,
        DateTimeOffset receivedAt,
        DiameterDnsQueryResult result)
    {
        if (dataOffset + 6 > dataEnd)
        {
            throw new DiameterCodecException("The DNS SRV record is truncated.");
        }

        DiameterSrvRecord record = new DiameterSrvRecord();
        record.Name = ownerName;
        record.Priority = ReadDnsUInt16(source, dataOffset);
        record.Weight = ReadDnsUInt16(source, dataOffset + 2);
        record.Port = ReadDnsUInt16(source, dataOffset + 4);
        DnsNameResult target = ReadDnsName(source, dataOffset + 6);
        if (target.NextOffset != dataEnd)
        {
            throw new DiameterCodecException("The DNS SRV record length is invalid.");
        }

        record.Target = target.Name;
        record.TimeToLiveSeconds = timeToLive;
        record.ExpiresAt = receivedAt.AddSeconds(timeToLive);
        result.SrvRecords.Add(record);
    }

    internal static void DecodeDnsAddressRecord(
        ReadOnlySpan<byte> source,
        string ownerName,
        int recordType,
        int dataOffset,
        int dataLength,
        uint timeToLive,
        DateTimeOffset receivedAt,
        DiameterDnsQueryResult result)
    {
        if ((recordType == Ipv4RecordType && dataLength != 4) ||
            (recordType == Ipv6RecordType && dataLength != 16))
        {
            throw new DiameterCodecException("The DNS address record length is invalid.");
        }

        DiameterAddressRecord record = new DiameterAddressRecord();
        record.Name = ownerName;
        record.Address = new IPAddress(source.Slice(dataOffset, dataLength).ToArray());
        record.TimeToLiveSeconds = timeToLive;
        record.ExpiresAt = receivedAt.AddSeconds(timeToLive);
        result.AddressRecords.Add(record);
    }

    internal static void DecodeDnsSoaRecord(
        ReadOnlySpan<byte> source,
        int dataOffset,
        int dataEnd,
        uint timeToLive,
        DiameterDnsQueryResult result)
    {
        DnsNameResult primaryName = ReadDnsName(source, dataOffset);
        DnsNameResult responsibleName = ReadDnsName(source, primaryName.NextOffset);
        if (responsibleName.NextOffset + 20 != dataEnd)
        {
            throw new DiameterCodecException("The DNS SOA record length is invalid.");
        }

        uint minimum = NetworkByteOrder.ReadUInt32(
            source.Slice(responsibleName.NextOffset + 16, 4));
        uint negativeLifetime = timeToLive;
        if (minimum < negativeLifetime)
        {
            negativeLifetime = minimum;
        }

        if (negativeLifetime > 0 &&
            (result.NegativeTimeToLiveSeconds == 0 ||
            negativeLifetime < result.NegativeTimeToLiveSeconds))
        {
            result.NegativeTimeToLiveSeconds = negativeLifetime;
        }
    }

    internal static DnsNameResult ReadDnsName(ReadOnlySpan<byte> source, int startOffset)
    {
        if (startOffset < 0 || startOffset >= source.Length)
        {
            throw new DiameterCodecException("The DNS name offset is outside the message.");
        }

        StringBuilder builder = new StringBuilder();
        bool[] visitedPointers = new bool[source.Length];
        int offset = startOffset;
        int nextOffset = startOffset;
        int expandedLength = 1;
        int pointerCount = 0;
        bool jumped = false;
        bool complete = false;
        while (!complete)
        {
            if (offset >= source.Length)
            {
                throw new DiameterCodecException("The DNS name is truncated.");
            }

            int labelLength = source[offset];
            if ((labelLength & 192) == 192)
            {
                if (offset + 1 >= source.Length)
                {
                    throw new DiameterCodecException("The DNS compression pointer is truncated.");
                }

                int pointer = ((labelLength & 63) << 8) | source[offset + 1];
                if (pointer >= offset || pointer >= source.Length ||
                    visitedPointers[pointer] || pointerCount >= MaximumPointerCount)
                {
                    throw new DiameterCodecException("The DNS compression pointer is invalid.");
                }

                if (!jumped)
                {
                    nextOffset = offset + 2;
                    jumped = true;
                }

                visitedPointers[pointer] = true;
                pointerCount = pointerCount + 1;
                offset = pointer;
            }
            else if ((labelLength & 192) != 0 || labelLength > MaximumLabelLength)
            {
                throw new DiameterCodecException("The DNS label length is invalid.");
            }
            else if (labelLength == 0)
            {
                if (!jumped)
                {
                    nextOffset = offset + 1;
                }

                complete = true;
            }
            else
            {
                if (offset + 1 + labelLength > source.Length)
                {
                    throw new DiameterCodecException("The DNS label is truncated.");
                }

                expandedLength = expandedLength + labelLength + 1;
                if (expandedLength > MaximumNameLength)
                {
                    throw new DiameterCodecException("The expanded DNS name is too long.");
                }

                if (builder.Length > 0)
                {
                    builder.Append('.');
                }

                int labelIndex = 0;
                while (labelIndex < labelLength)
                {
                    byte value = source[offset + 1 + labelIndex];
                    if (value < 33 || value > 126)
                    {
                        throw new DiameterCodecException("The DNS name contains unsupported text.");
                    }

                    builder.Append((char)value);
                    labelIndex = labelIndex + 1;
                }

                offset = offset + 1 + labelLength;
            }
        }

        DnsNameResult result = new DnsNameResult();
        result.Name = builder.ToString();
        result.NextOffset = nextOffset;
        return result;
    }

    internal static DnsCharacterStringResult ReadDnsCharacterString(
        ReadOnlySpan<byte> source,
        int offset,
        int endOffset)
    {
        if (offset < 0 || offset >= endOffset || endOffset > source.Length)
        {
            throw new DiameterCodecException("The DNS character string is truncated.");
        }

        int length = source[offset];
        int nextOffset = offset + 1 + length;
        if (nextOffset > endOffset)
        {
            throw new DiameterCodecException("The DNS character string is truncated.");
        }

        int index = 0;
        while (index < length)
        {
            byte value = source[offset + 1 + index];
            if (value < 32 || value > 126)
            {
                throw new DiameterCodecException("The DNS character string contains unsupported text.");
            }

            index = index + 1;
        }

        DnsCharacterStringResult result = new DnsCharacterStringResult();
        result.Value = Encoding.ASCII.GetString(source.Slice(offset + 1, length));
        result.NextOffset = nextOffset;
        return result;
    }

    internal static string NormalizeDnsQueryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A DNS query name is required.");
        }

        string result = name.Trim();
        if (result.EndsWith(".", StringComparison.Ordinal))
        {
            result = result.Substring(0, result.Length - 1);
        }

        if (result.Length == 0)
        {
            throw new ArgumentException("A non-root DNS query name is required.");
        }

        byte[] encoded = EncodeDnsName(result);
        if (encoded.Length > MaximumNameLength)
        {
            throw new ArgumentException("The DNS query name is too long.");
        }

        return result;
    }

    internal static byte[] EncodeDnsName(string name)
    {
        string[] labels = name.Split('.');
        List<byte> bytes = new List<byte>();
        int labelIndex = 0;
        while (labelIndex < labels.Length)
        {
            string label = labels[labelIndex];
            if (label.Length == 0 || label.Length > MaximumLabelLength)
            {
                throw new ArgumentException("The DNS query contains an invalid label length.");
            }

            bytes.Add((byte)label.Length);
            int characterIndex = 0;
            while (characterIndex < label.Length)
            {
                char value = label[characterIndex];
                if (value < 33 || value > 126)
                {
                    throw new ArgumentException("The DNS query name must contain ASCII DNS text.");
                }

                bytes.Add((byte)value);
                characterIndex = characterIndex + 1;
            }

            labelIndex = labelIndex + 1;
        }

        bytes.Add(0);
        if (bytes.Count > MaximumNameLength)
        {
            throw new ArgumentException("The DNS query name is too long.");
        }

        return bytes.ToArray();
    }

    internal static int ReadDnsUInt16(ReadOnlySpan<byte> source, int offset)
    {
        if (offset < 0 || offset + 2 > source.Length)
        {
            throw new DiameterCodecException("The DNS integer is truncated.");
        }

        return (source[offset] << 8) | source[offset + 1];
    }

    internal static void WriteDnsUInt16(byte[] destination, int offset, int value)
    {
        if (offset < 0 || offset + 2 > destination.Length || value < 0 || value > 65535)
        {
            throw new ArgumentException("The DNS integer destination or value is invalid.");
        }

        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)(value & 255);
    }

    internal static void SetDnsOutcome(DiameterDnsQueryResult result)
    {
        if (result.ResponseCode == 0)
        {
            result.Code = "dns-success";
            result.Message = "The DNS query completed successfully.";
        }
        else if (result.ResponseCode == 3)
        {
            result.Code = "dns-name-error";
            result.Message = "The DNS name does not exist.";
        }
        else
        {
            result.Code = "dns-response-error";
            result.Message = "The DNS server returned an error response.";
        }
    }
}

internal class DnsResponseHeader
{
    internal int Flags;
    internal int AnswerCount;
    internal int AuthorityCount;
    internal int AdditionalCount;
    internal int RecordOffset;
}

internal class DnsNameResult
{
    internal string Name = string.Empty;
    internal int NextOffset;
}

internal class DnsCharacterStringResult
{
    internal string Value = string.Empty;
    internal int NextOffset;
}
