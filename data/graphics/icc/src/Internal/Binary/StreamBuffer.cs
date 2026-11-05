namespace icc;

internal class StreamBuffer
{
    internal System.IO.Stream stream;
    internal byte[] scratchBuffer;
    internal long absoluteOffset;

    internal StreamBuffer(System.IO.Stream stream, int bufferSize)
    {
        if (stream == null)
        {
            throw new IccProfileFormatException("A stream cannot be null.");
        }

        if (bufferSize <= 0)
        {
            throw new IccProfileFormatException("A stream buffer size must be positive.");
        }

        this.stream = stream;
        int validatedBufferSize = CheckedSize.MultiplyCount(bufferSize, 1, System.Array.MaxLength);
        scratchBuffer = new byte[validatedBufferSize];
        absoluteOffset = stream.CanSeek ? stream.Position : 0;
    }

    internal long GetAbsoluteOffset()
    {
        return absoluteOffset;
    }

    internal byte ReadByteExact()
    {
        byte[] result = ReadExactBytes(1);
        return result[0];
    }

    internal byte[] ReadExactBytes(int count)
    {
        if (count < 0)
        {
            throw new IccProfileFormatException("A stream read byte count must not be negative.");
        }

        int byteCount = CheckedSize.MultiplyCount(count, 1, System.Array.MaxLength);
        byte[] result = new byte[byteCount];
        ReadExactInto(result, 0, byteCount);
        return result;
    }

    internal byte[] ReadExactSlice(int count)
    {
        return ReadExactBytes(count);
    }

    internal void ReadExactInto(byte[] destination, int destinationOffset, int count)
    {
        if (!stream.CanRead)
        {
            throw new IccProfileFormatException("The stream is not readable.");
        }

        if (destination == null)
        {
            throw new IccProfileFormatException("A stream read destination cannot be null.");
        }

        CheckedSize.ValidateRange(destinationOffset, count, destination.Length);
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = stream.Read(destination, destinationOffset + totalRead, count - totalRead);
            if (read == 0)
            {
                throw new IccProfileFormatException("The stream ended before the requested bytes were read.");
            }

            totalRead = totalRead + read;
            absoluteOffset = CheckedSize.AddOffsets(absoluteOffset, read, long.MaxValue);
        }
    }

    internal void SkipReadBytes(long count)
    {
        if (!stream.CanRead)
        {
            throw new IccProfileFormatException("The stream is not readable.");
        }

        if (count < 0)
        {
            throw new IccProfileFormatException("A stream skip byte count must not be negative.");
        }

        if (stream.CanSeek)
        {
            CheckedSize.ValidateRange(absoluteOffset, count, stream.Length);
            stream.Seek(count, System.IO.SeekOrigin.Current);
            absoluteOffset = CheckedSize.AddOffsets(absoluteOffset, count, long.MaxValue);
            return;
        }

        long remaining = count;
        while (remaining > 0)
        {
            int requested = scratchBuffer.Length;
            if (remaining < requested)
            {
                requested = (int)remaining;
            }

            int read = stream.Read(scratchBuffer, 0, requested);
            if (read == 0)
            {
                throw new IccProfileFormatException("The stream ended before the requested skip completed.");
            }

            remaining = remaining - read;
            absoluteOffset = CheckedSize.AddOffsets(absoluteOffset, read, long.MaxValue);
        }
    }

    internal void AlignReadPosition(int alignment, long origin)
    {
        long padding = CalculatePadding(alignment, origin);
        SkipReadBytes(padding);
    }

    internal void WriteByteExact(byte value)
    {
        byte[] values = { value };
        WriteExactBytes(values);
    }

    internal void WriteExactBytes(byte[] values)
    {
        if (!stream.CanWrite)
        {
            throw new IccProfileWriteException("The stream is not writable.");
        }

        if (values == null)
        {
            throw new IccProfileWriteException("Bytes to write cannot be null.");
        }

        stream.Write(values, 0, values.Length);
        absoluteOffset = CheckedSize.AddOffsets(absoluteOffset, values.Length, long.MaxValue);
    }

    internal void WriteZeroBytes(long count)
    {
        if (count < 0)
        {
            throw new IccProfileWriteException("A stream write byte count must not be negative.");
        }

        System.Array.Clear(scratchBuffer, 0, scratchBuffer.Length);
        long remaining = count;
        while (remaining > 0)
        {
            int requested = scratchBuffer.Length;
            if (remaining < requested)
            {
                requested = (int)remaining;
            }

            byte[] values = scratchBuffer;
            if (requested != scratchBuffer.Length)
            {
                values = new byte[requested];
            }

            WriteExactBytes(values);
            remaining = remaining - requested;
        }
    }

    internal void AlignWritePosition(int alignment, long origin)
    {
        long padding = CalculatePadding(alignment, origin);
        WriteZeroBytes(padding);
    }

    internal long CalculatePadding(int alignment, long origin)
    {
        if (alignment <= 0 || origin < 0 || origin > absoluteOffset)
        {
            throw new IccProfileFormatException("Alignment must be positive and its origin must not follow the current position.");
        }

        long relativePosition = absoluteOffset - origin;
        long remainder = relativePosition % alignment;
        if (remainder == 0)
        {
            return 0;
        }

        return alignment - remainder;
    }
}
