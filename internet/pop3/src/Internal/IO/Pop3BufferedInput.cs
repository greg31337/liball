namespace pop3;

internal class Pop3BufferedInput
{
    System.IO.Stream Stream;
    byte[] Buffer;
    int Start;
    int End;
    int MaximumBufferedOctets;
    bool EndOfStream;

    internal Pop3BufferedInput(System.IO.Stream stream, int bufferSize, int maximumBufferedOctets)
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
        MaximumBufferedOctets = maximumBufferedOctets;
    }

    internal int GetAvailableCount()
    {
        return End - Start;
    }

    internal bool EnsureBytes(int count, System.Threading.CancellationToken cancellationToken)
    {
        if (count < 0 || count > MaximumBufferedOctets)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }
        cancellationToken.ThrowIfCancellationRequested();

        while (End - Start < count && !EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PrepareWriteSpace(count);
            int bytesRead = Stream.ReadAsync(Buffer, End, Buffer.Length - End, cancellationToken).GetAwaiter().GetResult();
            if (bytesRead == 0)
            {
                EndOfStream = true;
            }
            else
            {
                End += bytesRead;
            }
        }

        return End - Start >= count;
    }

    internal int ReadByteValue(System.Threading.CancellationToken cancellationToken)
    {
        if (!EnsureBytes(1, cancellationToken))
        {
            return -1;
        }

        int value = Buffer[Start];
        Start++;
        ResetEmptyBuffer();
        return value;
    }

    internal int ReadBytes(byte[] destination, int offset, int count, System.Threading.CancellationToken cancellationToken)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }
        if (offset < 0 || count < 0 || offset > destination.Length - count)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }
        if (count == 0)
        {
            return 0;
        }
        if (!EnsureBytes(1, cancellationToken))
        {
            return 0;
        }

        int available = End - Start;
        int copyCount = count < available ? count : available;
        System.Array.Copy(Buffer, Start, destination, offset, copyCount);
        Start += copyCount;
        ResetEmptyBuffer();
        return copyCount;
    }

    internal void DiscardBufferedBytes()
    {
        System.Array.Clear(Buffer, 0, End);
        Start = 0;
        End = 0;
    }

    internal System.IO.Stream GetUnderlyingStream()
    {
        return Stream;
    }

    void PrepareWriteSpace(int requiredCount)
    {
        int available = End - Start;
        if (Start > 0 && Buffer.Length - End < requiredCount - available)
        {
            System.Array.Copy(Buffer, Start, Buffer, 0, available);
            Start = 0;
            End = available;
        }

        if (Buffer.Length - End >= requiredCount - available)
        {
            return;
        }

        int newSize = Buffer.Length;
        while (newSize < requiredCount && newSize < MaximumBufferedOctets)
        {
            if (newSize > MaximumBufferedOctets / 2)
            {
                newSize = MaximumBufferedOctets;
            }
            else
            {
                newSize *= 2;
            }
        }

        byte[] newBuffer = new byte[newSize];
        System.Array.Copy(Buffer, Start, newBuffer, 0, available);
        Buffer = newBuffer;
        Start = 0;
        End = available;
    }

    void ResetEmptyBuffer()
    {
        if (Start == End)
        {
            Start = 0;
            End = 0;
        }
    }
}
