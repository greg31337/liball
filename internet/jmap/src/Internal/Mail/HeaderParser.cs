using System;
using System.Collections.Generic;
using System.Text;

namespace jmap;

internal class HeaderParseResult
{
    internal List<EmailHeader> Headers = new List<EmailHeader>();
    internal int BodyOffset;
    internal bool HasHeaderBodySeparator;
    internal bool HasMalformedInput;
    internal bool IsLimitExceeded;
}

internal class HeaderLine
{
    internal int Start;
    internal int Length;
    internal int SeparatorLength;
    internal int NextOffset;
}

internal class HeaderParser
{
    internal const int RfcMaximumLineBytes = SecurityLimits.MaximumHeaderLineBytes;
    internal const int DefaultMaximumHeaderCount = SecurityLimits.MaximumHeaderCount;
    internal const int DefaultMaximumHeaderBytes = SecurityLimits.MaximumHeaderBytes;
    internal int MaximumLineBytes = RfcMaximumLineBytes;
    internal int MaximumHeaderCount = DefaultMaximumHeaderCount;
    internal int MaximumHeaderBytes = DefaultMaximumHeaderBytes;
    internal Encoding Utf8 = new UTF8Encoding(false, false);

    internal HeaderParser()
    {
    }

    internal HeaderParseResult ParseHeaders(byte[]? message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (MaximumLineBytes <= 0 || MaximumHeaderCount < 0 || MaximumHeaderBytes <= 0)
        {
            throw new ArgumentOutOfRangeException("MaximumLineBytes");
        }

        HeaderParseResult result = new HeaderParseResult();
        EmailHeader? current = null;
        List<byte>? rawValue = null;
        int offset = 0;
        int headerBytes = 0;
        bool discardContinuation = false;
        while (offset < message.Length)
        {
            HeaderLine line = ReadLine(message, offset);
            headerBytes = headerBytes + line.Length + line.SeparatorLength;
            if (line.Length == 0)
            {
                FinishHeader(result, current, rawValue);
                current = null;
                rawValue = null;
                result.BodyOffset = line.NextOffset;
                result.HasHeaderBodySeparator = true;
                if (line.SeparatorLength != 2)
                {
                    result.HasMalformedInput = true;
                }

                return result;
            }

            bool continuation = IsContinuation(message, line);
            bool overLimit = line.Length > MaximumLineBytes || headerBytes > MaximumHeaderBytes;
            if (overLimit)
            {
                result.HasMalformedInput = true;
                result.IsLimitExceeded = true;
                if (continuation)
                {
                    current = null;
                    rawValue = null;
                    discardContinuation = true;
                }
                else
                {
                    FinishHeader(result, current, rawValue);
                    current = null;
                    rawValue = null;
                    discardContinuation = true;
                }

                MarkNonStandardSeparator(line, result);
                offset = line.NextOffset;
                continue;
            }

            if (continuation)
            {
                if (current == null || rawValue == null || discardContinuation)
                {
                    result.HasMalformedInput = true;
                }
                else
                {
                    AppendSeparator(message, line, rawValue, result);
                    AppendBytes(message, line.Start, line.Length, rawValue);
                }
            }
            else
            {
                FinishHeader(result, current, rawValue);
                current = null;
                rawValue = null;
                discardContinuation = false;
                int colon = FindColon(message, line);
                int nameLength = ValidNameLength(message, line, colon, result);
                if (nameLength <= 0)
                {
                    result.HasMalformedInput = true;
                    discardContinuation = true;
                }
                else if (result.Headers.Count >= MaximumHeaderCount)
                {
                    result.HasMalformedInput = true;
                    result.IsLimitExceeded = true;
                    discardContinuation = true;
                }
                else
                {
                    current = new EmailHeader();
                    current.Name = Encoding.ASCII.GetString(message, line.Start, nameLength);
                    rawValue = new List<byte>();
                    int valueStart = colon + 1;
                    AppendBytes(message, valueStart, line.Start + line.Length - valueStart, rawValue);
                }
            }

            MarkNonStandardSeparator(line, result);
            offset = line.NextOffset;
        }

        FinishHeader(result, current, rawValue);
        result.BodyOffset = message.Length;
        return result;
    }

    internal HeaderLine ReadLine(byte[] message, int offset)
    {
        HeaderLine line = new HeaderLine();
        line.Start = offset;
        int index = offset;
        while (index < message.Length && message[index] != 13 && message[index] != 10)
        {
            index = index + 1;
        }

        line.Length = index - offset;
        if (index == message.Length)
        {
            line.NextOffset = index;
            return line;
        }

        if (message[index] == 13 && index + 1 < message.Length && message[index + 1] == 10)
        {
            line.SeparatorLength = 2;
        }
        else
        {
            line.SeparatorLength = 1;
        }

        line.NextOffset = index + line.SeparatorLength;
        return line;
    }

    internal bool IsContinuation(byte[] message, HeaderLine line)
    {
        return line.Length != 0 && (message[line.Start] == 32 || message[line.Start] == 9);
    }

    internal int FindColon(byte[] message, HeaderLine line)
    {
        int index = line.Start;
        int end = line.Start + line.Length;
        while (index < end)
        {
            if (message[index] == 58)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int ValidNameLength(byte[] message, HeaderLine line, int colon, HeaderParseResult result)
    {
        if (colon <= line.Start)
        {
            return -1;
        }

        int nameEnd = colon;
        while (nameEnd > line.Start && (message[nameEnd - 1] == 32 || message[nameEnd - 1] == 9))
        {
            nameEnd = nameEnd - 1;
            result.HasMalformedInput = true;
        }

        if (nameEnd == line.Start)
        {
            return -1;
        }

        int index = line.Start;
        while (index < nameEnd)
        {
            int octet = message[index];
            if (octet < 33 || octet > 126 || octet == 58)
            {
                return -1;
            }

            index = index + 1;
        }

        return nameEnd - line.Start;
    }

    internal void AppendSeparator(
        byte[] message,
        HeaderLine line,
        List<byte> destination,
        HeaderParseResult result)
    {
        if (line.Start >= 2 && message[line.Start - 2] == 13 && message[line.Start - 1] == 10)
        {
            destination.Add(13);
            destination.Add(10);
            return;
        }

        if (line.Start >= 1 && (message[line.Start - 1] == 13 || message[line.Start - 1] == 10))
        {
            destination.Add(message[line.Start - 1]);
            result.HasMalformedInput = true;
            return;
        }

        if (line.Start == 0)
        {
            result.HasMalformedInput = true;
        }
    }

    internal void AppendBytes(byte[] source, int offset, int count, List<byte> destination)
    {
        int index = 0;
        while (index < count)
        {
            destination.Add(source[offset + index]);
            index = index + 1;
        }
    }

    internal void FinishHeader(
        HeaderParseResult result,
        EmailHeader? header,
        List<byte>? rawValue)
    {
        if (header == null || rawValue == null)
        {
            return;
        }

        header.Value = DecodeRawValue(rawValue, result);
        result.Headers.Add(header);
    }

    internal string DecodeRawValue(List<byte> rawValue, HeaderParseResult result)
    {
        List<byte> cleaned = new List<byte>();
        foreach (byte octet in rawValue)
        {
            if (octet == 0)
            {
                result.HasMalformedInput = true;
            }
            else
            {
                cleaned.Add(octet);
            }
        }

        byte[] bytes = cleaned.ToArray();
        string value = Utf8.GetString(bytes);
        byte[] encoded = Utf8.GetBytes(value);
        if (!BytesEqual(bytes, encoded))
        {
            result.HasMalformedInput = true;
        }

        return value;
    }

    internal bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void MarkNonStandardSeparator(HeaderLine line, HeaderParseResult result)
    {
        if (line.SeparatorLength == 1)
        {
            result.HasMalformedInput = true;
        }
    }
}
