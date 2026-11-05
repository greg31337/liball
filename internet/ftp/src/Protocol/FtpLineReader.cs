using System.Collections.Generic;

namespace ftp;

internal class FtpLineReader
{
    internal byte[] LineBuffer;
    internal int LineLength;
    internal bool SawCarriageReturn;
    internal bool Completed;
    internal bool Failed;
    internal int MaximumPendingLines;
    internal Queue<byte[]> PendingLines;

    internal FtpLineReader(int maximumLineBytes, int maximumPendingLines)
    {
        if (maximumLineBytes < 2)
        {
            throw new FtpConfigurationException("A line limit must reserve at least two bytes for CRLF.", null);
        }

        if (maximumPendingLines <= 0)
        {
            throw new FtpConfigurationException("The pending-line limit must be positive.", null);
        }

        LineBuffer = new byte[maximumLineBytes - 2];
        LineLength = 0;
        SawCarriageReturn = false;
        Completed = false;
        Failed = false;
        MaximumPendingLines = maximumPendingLines;
        PendingLines = new Queue<byte[]>(maximumPendingLines);
    }

    internal void AppendBytes(byte[] bytes, int offset, int count)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        if (offset < 0 || count < 0 || offset > bytes.Length - count)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (Completed)
        {
            throw new FtpProtocolException("FTP line data arrived after the input was completed.", null);
        }

        if (Failed)
        {
            throw new FtpProtocolException("The FTP line reader is in a failed state.", null);
        }

        int index = offset;
        int end = offset + count;
        while (index < end)
        {
            byte value = bytes[index];
            if (SawCarriageReturn)
            {
                if (value != 10)
                {
                    FailLine("A carriage return in FTP line data must be followed by a line feed.");
                }

                CompleteLine();
            }
            else if (value == 13)
            {
                SawCarriageReturn = true;
            }
            else if (value == 10)
            {
                FailLine("A bare line feed is not a valid FTP line terminator.");
            }
            else
            {
                if (LineLength >= LineBuffer.Length)
                {
                    FailLine("The FTP line exceeded its configured byte limit.");
                }

                LineBuffer[LineLength] = value;
                LineLength++;
            }

            index++;
        }
    }

    internal byte[]? ReadLine()
    {
        if (PendingLines.Count == 0)
        {
            return null;
        }

        return PendingLines.Dequeue();
    }

    internal int GetPendingLineCount()
    {
        return PendingLines.Count;
    }

    internal bool HasPartialLine()
    {
        return LineLength != 0 || SawCarriageReturn;
    }

    internal void CompleteInput()
    {
        if (Completed)
        {
            return;
        }

        if (Failed)
        {
            throw new FtpProtocolException("The FTP line reader is in a failed state.", null);
        }

        if (HasPartialLine())
        {
            FailLine("The FTP input ended before a complete CRLF-terminated line was received.");
        }

        Completed = true;
    }

    internal void CompleteLine()
    {
        if (PendingLines.Count >= MaximumPendingLines)
        {
            FailLine("The pending FTP line limit was exceeded.");
        }

        byte[] line = new byte[LineLength];
        if (LineLength != 0)
        {
            System.Array.Copy(LineBuffer, 0, line, 0, LineLength);
        }

        PendingLines.Enqueue(line);
        LineLength = 0;
        SawCarriageReturn = false;
    }

    internal void FailLine(string message)
    {
        Failed = true;
        throw new FtpProtocolException(message, null);
    }
}
