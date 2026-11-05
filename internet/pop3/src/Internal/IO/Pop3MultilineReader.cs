namespace pop3;

internal class Pop3MultilineReader
{
    Pop3LineReader LineReader;

    internal Pop3MultilineReader(Pop3LineReader lineReader)
    {
        if (lineReader == null)
        {
            throw new System.ArgumentNullException("lineReader");
        }

        LineReader = lineReader;
    }

    internal long CopyMultilineData(
        System.IO.Stream destination,
        int maximumLineOctets,
        IPop3TransferObserver? observer,
        Pop3TransferKind kind,
        int messageNumber,
        long expectedBytes,
        System.Threading.CancellationToken cancellationToken)
    {
        Pop3ClientTransfer transfer = new Pop3ClientTransfer(observer);
        ValidateTransfer(destination, maximumLineOctets, transfer, kind, messageNumber, expectedBytes);
        return CopyPayload(
            destination,
            maximumLineOctets,
            -1,
            transfer,
            kind,
            messageNumber,
            expectedBytes,
            cancellationToken);
    }

    internal byte[] ReadMultilineData(
        int maximumLineOctets,
        int memoryCeilingBytes,
        IPop3TransferObserver? observer,
        Pop3TransferKind kind,
        int messageNumber,
        long expectedBytes,
        System.Threading.CancellationToken cancellationToken)
    {
        if (memoryCeilingBytes < 0)
        {
            throw new System.ArgumentOutOfRangeException("memoryCeilingBytes");
        }

        using (System.IO.MemoryStream destination = new System.IO.MemoryStream())
        {
            Pop3ClientTransfer transfer = new Pop3ClientTransfer(observer);
            ValidateTransfer(destination, maximumLineOctets, transfer, kind, messageNumber, expectedBytes);
            CopyPayload(
                destination,
                maximumLineOctets,
                memoryCeilingBytes,
                transfer,
                kind,
                messageNumber,
                expectedBytes,
                cancellationToken);
            return destination.ToArray();
        }
    }

    long CopyPayload(
        System.IO.Stream destination,
        int maximumLineOctets,
        long memoryCeilingBytes,
        Pop3ClientTransfer transfer,
        Pop3TransferKind kind,
        int messageNumber,
        long expectedBytes,
        System.Threading.CancellationToken cancellationToken)
    {
        long bytesTransferred = 0;
        byte[] crlf = new byte[] { 13, 10 };
        while (true)
        {
            byte[]? line = LineReader.ReadDataLine(maximumLineOctets, cancellationToken);
            if (line == null)
            {
                throw new System.IO.EndOfStreamException("The POP3 stream ended before the multiline terminator.");
            }
            if (line.Length == 1 && line[0] == 46)
            {
                transfer.ReportProgress(kind, messageNumber, bytesTransferred, expectedBytes, true);
                return bytesTransferred;
            }

            int offset = line.Length > 0 && line[0] == 46 ? 1 : 0;
            int contentLength = line.Length - offset;
            int payloadLength = contentLength + 2;
            if (bytesTransferred > long.MaxValue - payloadLength)
            {
                throw new System.IO.InvalidDataException("A POP3 multiline payload exceeds the supported size.");
            }
            if (memoryCeilingBytes >= 0 && bytesTransferred + payloadLength > memoryCeilingBytes)
            {
                throw new System.IO.InvalidDataException("A POP3 multiline payload exceeds its configured memory ceiling.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (contentLength > 0)
            {
                destination.WriteAsync(line, offset, contentLength, cancellationToken).GetAwaiter().GetResult();
            }
            destination.WriteAsync(crlf, 0, crlf.Length, cancellationToken).GetAwaiter().GetResult();
            bytesTransferred += payloadLength;
            transfer.ReportProgress(kind, messageNumber, bytesTransferred, expectedBytes, false);
        }
    }

    void ValidateTransfer(
        System.IO.Stream destination,
        int maximumLineOctets,
        Pop3ClientTransfer transfer,
        Pop3TransferKind kind,
        int messageNumber,
        long expectedBytes)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }
        if (!destination.CanWrite)
        {
            throw new System.ArgumentException("The POP3 multiline destination must be writable.", "destination");
        }
        if (maximumLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("maximumLineOctets");
        }
        transfer.ValidateMetadata(kind, messageNumber, expectedBytes);
    }
}
