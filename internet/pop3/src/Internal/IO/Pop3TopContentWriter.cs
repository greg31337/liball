namespace pop3;

internal class Pop3TopContentWriter
{
    Pop3LineWriter LineWriter;
    Pop3ClientTransfer? Transfer;
    int MessageNumber;
    int RequestedBodyLines;
    int WrittenBodyLines;
    bool InBody;
    bool RequireUtf8;
    long TransmittedOctets;

    internal Pop3TopContentWriter(Pop3LineWriter lineWriter)
    {
        if (lineWriter == null)
        {
            throw new System.ArgumentNullException("lineWriter");
        }
        LineWriter = lineWriter;
    }

    internal long WriteTopContent(
        System.IO.Stream source,
        int bodyLineCount,
        int maximumLineOctets,
        IPop3TransferObserver? observer,
        int messageNumber,
        bool requireUtf8,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateInputs(
            source,
            bodyLineCount,
            maximumLineOctets,
            messageNumber);
        cancellationToken.ThrowIfCancellationRequested();
        Transfer = new Pop3ClientTransfer(observer);
        Transfer.ValidateMetadata(
            Pop3TransferKind.Top,
            messageNumber,
            -1);
        MessageNumber = messageNumber;
        RequestedBodyLines = bodyLineCount;
        WrittenBodyLines = 0;
        InBody = false;
        RequireUtf8 = requireUtf8;
        TransmittedOctets = 0;

        byte[] readBuffer = new byte[Pop3Defaults.DefaultBufferSize];
        byte[] lineBuffer = new byte[maximumLineOctets - 2];
        int lineLength = 0;
        bool pendingCarriageReturn = false;
        bool endOfStream = false;
        while (!endOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int readCount = source.ReadAsync(
                readBuffer,
                0,
                readBuffer.Length,
                cancellationToken).GetAwaiter().GetResult();
            if (readCount == 0)
            {
                endOfStream = true;
            }
            else
            {
                int readIndex = 0;
                while (readIndex < readCount)
                {
                    byte value = readBuffer[readIndex];
                    if (pendingCarriageReturn)
                    {
                        CompleteLine(
                            lineBuffer,
                            lineLength,
                            maximumLineOctets,
                            cancellationToken);
                        lineLength = 0;
                        pendingCarriageReturn = false;
                        if (value == 10)
                        {
                            readIndex++;
                            continue;
                        }
                    }

                    if (value == 13)
                    {
                        pendingCarriageReturn = true;
                    }
                    else if (value == 10)
                    {
                        CompleteLine(
                            lineBuffer,
                            lineLength,
                            maximumLineOctets,
                            cancellationToken);
                        lineLength = 0;
                    }
                    else if (ShouldRetainLine())
                    {
                        if (lineLength == lineBuffer.Length)
                        {
                            throw new System.IO.InvalidDataException(
                                "A POP3 TOP source line exceeds its permitted octet count after CRLF conversion.");
                        }
                        lineBuffer[lineLength] = value;
                        lineLength++;
                    }
                    readIndex++;
                }
            }
        }

        if (pendingCarriageReturn || lineLength > 0)
        {
            CompleteLine(
                lineBuffer,
                lineLength,
                maximumLineOctets,
                cancellationToken);
        }
        if (!InBody)
        {
            WriteSelectedLine(
                lineBuffer,
                0,
                maximumLineOctets,
                cancellationToken);
            InBody = true;
        }
        Transfer = null;
        return TransmittedOctets;
    }

    void ValidateInputs(
        System.IO.Stream source,
        int bodyLineCount,
        int maximumLineOctets,
        int messageNumber)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }
        if (!source.CanRead)
        {
            throw new System.ArgumentException(
                "The POP3 TOP source must be readable.",
                "source");
        }
        if (bodyLineCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bodyLineCount");
        }
        if (maximumLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("maximumLineOctets");
        }
        if (messageNumber < 1)
        {
            throw new System.ArgumentOutOfRangeException("messageNumber");
        }
    }

    bool ShouldRetainLine()
    {
        return !InBody || WrittenBodyLines < RequestedBodyLines;
    }

    void CompleteLine(
        byte[] line,
        int lineLength,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        if (!InBody)
        {
            WriteSelectedLine(
                line,
                lineLength,
                maximumLineOctets,
                cancellationToken);
            if (lineLength == 0)
            {
                InBody = true;
            }
            return;
        }
        if (WrittenBodyLines >= RequestedBodyLines)
        {
            return;
        }
        WriteSelectedLine(
            line,
            lineLength,
            maximumLineOctets,
            cancellationToken);
        WrittenBodyLines++;
    }

    void WriteSelectedLine(
        byte[] line,
        int lineLength,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        if (RequireUtf8 &&
            !new Pop3Utf8Validator().IsValidUtf8(
                new System.ReadOnlySpan<byte>(line, 0, lineLength)))
        {
            throw new System.IO.InvalidDataException(
                "A POP3 UTF-8 TOP representation contains malformed UTF-8.");
        }
        int normalizedLineOctets = lineLength + 2;
        if (TransmittedOctets > long.MaxValue - normalizedLineOctets)
        {
            throw new System.IO.InvalidDataException(
                "A POP3 TOP payload exceeds the supported size.");
        }
        bool stuffDot = lineLength > 0 && line[0] == 46;
        int stuffedLength = stuffDot ? lineLength + 1 : lineLength;
        byte[] stuffedLine = new byte[stuffedLength];
        int destinationOffset = 0;
        if (stuffDot)
        {
            stuffedLine[0] = 46;
            destinationOffset = 1;
        }
        System.Array.Copy(
            line,
            0,
            stuffedLine,
            destinationOffset,
            lineLength);
        int wireMaximumLineOctets = maximumLineOctets;
        if (stuffDot && maximumLineOctets < int.MaxValue)
        {
            wireMaximumLineOctets++;
        }
        LineWriter.WriteDataLine(
            stuffedLine,
            wireMaximumLineOctets,
            cancellationToken);
        TransmittedOctets += normalizedLineOctets;
        if (Transfer != null)
        {
            Transfer.ReportProgress(
                Pop3TransferKind.Top,
                MessageNumber,
                TransmittedOctets,
                -1,
                false);
        }
    }
}
