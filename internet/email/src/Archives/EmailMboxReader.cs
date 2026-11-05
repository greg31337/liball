using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
internal class EmailMboxLineReader
{
    Stream input;
    byte[] buffer;
    int bufferOffset;
    int bufferCount;
    long maximumArchiveBytes;
    long maximumLineBytes;
    long physicalBytes;
    long logicalBytes;
    long lineNumber;

    internal EmailMboxLineReader(Stream input, int bufferBytes, long maximumArchiveBytes, long maximumLineBytes)
    {
        this.input = input;
        buffer = new byte[bufferBytes];
        bufferOffset = 0;
        bufferCount = 0;
        this.maximumArchiveBytes = maximumArchiveBytes;
        this.maximumLineBytes = maximumLineBytes;
        physicalBytes = 0;
        logicalBytes = 0;
        lineNumber = 1;
    }

    internal EmailMboxLine? ReadLine()
    {
        long offset = logicalBytes;
        long currentLine = lineNumber;
        using (MemoryStream output = new MemoryStream())
        {
            while (true)
            {
                int value = ReadByte();
                if (value < 0)
                {
                    if (output.Length == 0) return null;
                    break;
                }

                if (output.Length >= maximumLineBytes) throw new EmailLimitException("MaxMboxEntryBytes", maximumLineBytes, output.Length + 1);
                output.WriteByte((byte)value);
                if (value == 10) break;
                if (value == 13)
                {
                    int next = ReadByte();
                    if (next == 10)
                    {
                        if (output.Length >= maximumLineBytes) throw new EmailLimitException("MaxMboxEntryBytes", maximumLineBytes, output.Length + 1);
                        output.WriteByte((byte)next);
                    }
                    else if (next >= 0) UnreadByte();
                    break;
                }
            }

            lineNumber++;
            return new EmailMboxLine(output.ToArray(), offset, currentLine);
        }
    }

    internal long GetPhysicalByteCount() { return physicalBytes; }
    internal long GetLogicalByteCount() { return logicalBytes; }

    int ReadByte()
    {
        if (bufferOffset >= bufferCount && !Fill()) return -1;
        int value = buffer[bufferOffset];
        bufferOffset++;
        logicalBytes++;
        return value;
    }

    void UnreadByte()
    {
        bufferOffset--;
        logicalBytes--;
    }

    bool Fill()
    {
        long remaining = maximumArchiveBytes - physicalBytes;
        int requested = buffer.Length;
        if (remaining < requested)
        {
            long bounded = remaining + 1;
            requested = bounded > int.MaxValue ? int.MaxValue : (int)bounded;
        }

        int read = input.Read(buffer, 0, requested);
        if (read == 0)
        {
            bufferOffset = 0;
            bufferCount = 0;
            return false;
        }

        physicalBytes = new EmailSafeArithmetic().AddNonNegative(physicalBytes, read);
        if (physicalBytes > maximumArchiveBytes) throw new EmailLimitException("MaxMboxArchiveBytes", maximumArchiveBytes, physicalBytes);
        bufferOffset = 0;
        bufferCount = read;
        return true;
    }
}
internal class EmailMboxLine
{
    byte[] bytes;
    long offset;
    long lineNumber;

    internal EmailMboxLine(byte[] bytes, long offset, long lineNumber)
    {
        this.bytes = bytes;
        this.offset = offset;
        this.lineNumber = lineNumber;
    }

    internal byte[] GetBytes() { return bytes; }
    internal long GetOffset() { return offset; }
    internal long GetLength() { return bytes.LongLength; }
    internal long GetEndOffset() { return offset + bytes.LongLength; }
    internal long GetLineNumber() { return lineNumber; }
}
internal class EmailMboxEnvelopeData
{
    byte[] rawLine;
    string? senderText;
    string? dateText;
    EmailAddress? sender;
    bool attachRawLine;

    internal EmailMboxEnvelopeData(byte[] rawLine, string? senderText, string? dateText, EmailAddress? sender, bool attachRawLine)
    {
        this.rawLine = rawLine;
        this.senderText = senderText;
        this.dateText = dateText;
        this.sender = sender;
        this.attachRawLine = attachRawLine;
    }

    internal byte[] GetRawLine() { return rawLine; }
    internal string? GetSenderText() { return senderText; }
    internal string? GetDateText() { return dateText; }
    internal bool HasSender() { return sender != null; }
    internal EmailAddress? GetSender() { return sender; }
    internal bool CanAttachRawLine() { return attachRawLine; }
}
