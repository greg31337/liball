namespace pop3;

internal class Pop3MultilineWriter
{
    Pop3LineWriter LineWriter;

    internal Pop3MultilineWriter(Pop3LineWriter lineWriter)
    {
        if (lineWriter == null)
        {
            throw new System.ArgumentNullException("lineWriter");
        }

        LineWriter = lineWriter;
    }

    internal long WriteMultilineData(
        System.IO.Stream source,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        return WriteObservedMultilineData(
            source,
            maximumLineOctets,
            null,
            Pop3TransferKind.ExtensionData,
            0,
            -1,
            cancellationToken);
    }

    internal long WriteObservedMultilineData(
        System.IO.Stream source,
        int maximumLineOctets,
        IPop3TransferObserver? observer,
        Pop3TransferKind kind,
        int messageNumber,
        long expectedBytes,
        System.Threading.CancellationToken cancellationToken)
    {
        long transmittedOctets = WriteObservedMultilineContent(
            source,
            maximumLineOctets,
            observer,
            kind,
            messageNumber,
            expectedBytes,
            false,
            cancellationToken);
        WriteObservedMultilineTerminator(
            observer,
            kind,
            messageNumber,
            transmittedOctets,
            expectedBytes,
            cancellationToken);
        return transmittedOctets;
    }

    internal long WriteObservedMultilineContent(
        System.IO.Stream source,
        int maximumLineOctets,
        IPop3TransferObserver? observer,
        Pop3TransferKind kind,
        int messageNumber,
        long expectedBytes,
        bool requireUtf8,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3ClientTransfer transfer = new Pop3ClientTransfer(observer);
        ValidateTransfer(source, maximumLineOctets, transfer, kind, messageNumber, expectedBytes);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] readBuffer = new byte[Pop3Defaults.DefaultBufferSize];
        byte[] lineBuffer = new byte[maximumLineOctets - 2];
        int lineLength = 0;
        bool pendingCarriageReturn = false;
        long transmittedOctets = 0;
        bool endOfStream = false;
        while (!endOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int readCount = source.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken).GetAwaiter().GetResult();
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
                        transmittedOctets = WriteCompletedLine(
                            lineBuffer,
                            lineLength,
                            maximumLineOctets,
                            transmittedOctets,
                            requireUtf8,
                            cancellationToken);
                        transfer.ReportProgress(kind, messageNumber, transmittedOctets, expectedBytes, false);
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
                        transmittedOctets = WriteCompletedLine(
                            lineBuffer,
                            lineLength,
                            maximumLineOctets,
                            transmittedOctets,
                            requireUtf8,
                            cancellationToken);
                        transfer.ReportProgress(kind, messageNumber, transmittedOctets, expectedBytes, false);
                        lineLength = 0;
                    }
                    else
                    {
                        if (lineLength == lineBuffer.Length)
                        {
                            throw new System.IO.InvalidDataException("A POP3 source data line exceeds its permitted octet count after CRLF conversion.");
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
            transmittedOctets = WriteCompletedLine(
                lineBuffer,
                lineLength,
                maximumLineOctets,
                transmittedOctets,
                requireUtf8,
                cancellationToken);
            transfer.ReportProgress(kind, messageNumber, transmittedOctets, expectedBytes, false);
        }

        return transmittedOctets;
    }

    internal void WriteObservedMultilineTerminator(
        IPop3TransferObserver? observer,
        Pop3TransferKind kind,
        int messageNumber,
        long transmittedOctets,
        long expectedBytes,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3ClientTransfer transfer = new Pop3ClientTransfer(observer);
        transfer.ValidateMetadata(kind, messageNumber, expectedBytes);
        if (transmittedOctets < 0)
        {
            throw new System.ArgumentOutOfRangeException("transmittedOctets");
        }
        cancellationToken.ThrowIfCancellationRequested();
        LineWriter.WriteDataLine(new byte[] { 46 }, 3, cancellationToken);
        transfer.ReportProgress(
            kind,
            messageNumber,
            transmittedOctets,
            expectedBytes,
            true);
    }

    void ValidateTransfer(
        System.IO.Stream source,
        int maximumLineOctets,
        Pop3ClientTransfer transfer,
        Pop3TransferKind kind,
        int messageNumber,
        long expectedBytes)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }
        if (!source.CanRead)
        {
            throw new System.ArgumentException("The POP3 multiline source must be readable.", "source");
        }
        if (maximumLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("maximumLineOctets");
        }

        transfer.ValidateMetadata(kind, messageNumber, expectedBytes);
    }

    long WriteCompletedLine(
        byte[] line,
        int lineLength,
        int maximumLineOctets,
        long transmittedOctets,
        bool requireUtf8,
        System.Threading.CancellationToken cancellationToken)
    {
        if (requireUtf8 &&
            !new Pop3Utf8Validator().IsValidUtf8(
                new System.ReadOnlySpan<byte>(line, 0, lineLength)))
        {
            throw new System.IO.InvalidDataException(
                "A POP3 UTF-8 message representation contains malformed UTF-8.");
        }
        int normalizedLineOctets = lineLength + 2;
        if (transmittedOctets > long.MaxValue - normalizedLineOctets)
        {
            throw new System.IO.InvalidDataException("A POP3 multiline payload exceeds the supported size.");
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
        System.Array.Copy(line, 0, stuffedLine, destinationOffset, lineLength);

        int wireMaximumLineOctets = maximumLineOctets;
        if (stuffDot && maximumLineOctets < int.MaxValue)
        {
            wireMaximumLineOctets++;
        }
        LineWriter.WriteDataLine(stuffedLine, wireMaximumLineOctets, cancellationToken);
        return transmittedOctets + normalizedLineOctets;
    }
}
