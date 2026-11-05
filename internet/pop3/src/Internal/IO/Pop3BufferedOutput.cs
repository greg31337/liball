namespace pop3;

internal class Pop3BufferedOutput
{
    System.IO.Stream Stream;
    byte[] Buffer;
    int Count;

    internal Pop3BufferedOutput(System.IO.Stream stream, int bufferSize, int maximumBufferedOctets)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (bufferSize <= 0)
        {
            throw new System.ArgumentOutOfRangeException("bufferSize");
        }
        if (maximumBufferedOctets < bufferSize)
        {
            throw new System.ArgumentOutOfRangeException("maximumBufferedOctets");
        }

        Stream = stream;
        Buffer = new byte[bufferSize];
    }

    internal int GetBufferedCount()
    {
        return Count;
    }

    internal void WriteByteValue(byte value, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Count == Buffer.Length)
        {
            FlushBufferedBytes(cancellationToken);
        }

        Buffer[Count] = value;
        Count++;
    }

    internal void WriteBytes(byte[] source, int offset, int count, System.Threading.CancellationToken cancellationToken)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }
        if (offset < 0 || count < 0 || offset > source.Length - count)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        int sourceOffset = offset;
        int remaining = count;
        while (remaining > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Count == Buffer.Length)
            {
                FlushBufferedBytes(cancellationToken);
            }

            int available = Buffer.Length - Count;
            int copyCount = remaining < available ? remaining : available;
            System.Array.Copy(source, sourceOffset, Buffer, Count, copyCount);
            Count += copyCount;
            sourceOffset += copyCount;
            remaining -= copyCount;
        }
    }

    internal void FlushOutput(System.Threading.CancellationToken cancellationToken)
    {
        FlushBufferedBytes(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        Stream.FlushAsync(cancellationToken).GetAwaiter().GetResult();
    }

    internal System.IO.Stream GetUnderlyingStream()
    {
        return Stream;
    }

    void FlushBufferedBytes(System.Threading.CancellationToken cancellationToken)
    {
        if (Count == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        Stream.WriteAsync(Buffer, 0, Count, cancellationToken).GetAwaiter().GetResult();
        Count = 0;
    }
}
