using System;

namespace raw;

internal class BoundedInput
{
    internal System.IO.Stream? Stream;
    internal System.ReadOnlyMemory<byte> Memory;
    internal RawLimits? Limits;
    internal byte[]? SkipBuffer;
    internal long Start;
    internal long MemoryOffset;
    internal long Length;
    internal long Position;
    internal bool UsesMemory;
    internal bool Seekable;

    internal BoundedInput(System.ReadOnlyMemory<byte> source, long offset, long length, RawLimits? limits)
    {
        CheckedMath.RequireLimits(limits);
        CheckedMath.FileSize(source.Length, limits);
        CheckedMath.RangeEnd(offset, length, source.Length);
        Stream = null;
        Memory = source;
        Limits = limits;
        SkipBuffer = null;
        Start = offset;
        MemoryOffset = offset;
        Length = length;
        Position = 0;
        UsesMemory = true;
        Seekable = true;
    }

    internal static BoundedInput CreateWithAbsoluteOrigin(System.ReadOnlyMemory<byte> source, long offset, long length, long absoluteOrigin, RawLimits? limits)
    {
        if (absoluteOrigin < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "The absolute memory origin cannot be negative.", absoluteOrigin);
        }
        BoundedInput input = new BoundedInput(source, offset, length, limits);
        input.Start = absoluteOrigin;
        return input;
    }

    internal static BoundedInput CreateStream(System.IO.Stream? source, long offset, long length, RawLimits? limits)
    {
        CheckedMath.RequireLimits(limits);
        CheckedMath.RequirePositiveLimit((limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxFileSizeBytes, "The configured file-size limit must be greater than zero.");
        if (source == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "An input stream is required.", -1);
        }
        if (!source.CanRead)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The input stream must be readable.", -1);
        }

        long requestedEnd = CheckedMath.Add(offset, length);
        if (requestedEnd > limits.MaxFileSizeBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The bounded stream range exceeds the configured file-size limit.", offset);
        }

        BoundedInput input = new BoundedInput(default(System.ReadOnlyMemory<byte>), 0, 0, limits);
        input.Stream = source;
        input.Memory = default(System.ReadOnlyMemory<byte>);
        input.Limits = limits;
        input.Start = offset;
        input.MemoryOffset = 0;
        input.Length = length;
        input.Position = 0;
        input.UsesMemory = false;
        input.Seekable = source.CanSeek;

        if (input.Seekable)
        {
            long sourceLength = CheckedMath.FileSize(source.Length, limits);
            CheckedMath.RangeEnd(offset, length, sourceLength);
            source.Position = offset;
            input.SkipBuffer = null;
        }
        else
        {
            input.SkipBuffer = new byte[8192];
            SkipStreamExactly(source, offset, 0, input.SkipBuffer);
        }
        return input;
    }

    internal long GetLength()
    {
        return Length;
    }

    internal long GetPosition()
    {
        return Position;
    }

    internal long GetAbsolutePosition()
    {
        return CheckedMath.Add(Start, Position);
    }

    internal long GetRemaining()
    {
        return Length - Position;
    }

    internal int Read(System.Span<byte> destination)
    {
        if (destination.Length == 0)
        {
            return 0;
        }

        long remaining = GetRemaining();
        if (remaining == 0)
        {
            return 0;
        }

        int count = destination.Length;
        if (remaining < count)
        {
            count = (int)remaining;
        }

        int read;
        if (UsesMemory)
        {
            int sourceOffset = CheckedMath.ToInt32(CheckedMath.Add(MemoryOffset, Position));
            Memory.Span.Slice(sourceOffset, count).CopyTo(destination);
            read = count;
        }
        else
        {
            PositionStream();
            read = (Stream ?? throw new System.InvalidOperationException("A required value is unavailable.")).Read(destination.Slice(0, count));
            if (read < 0 || read > count)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The stream returned an invalid read count.", GetAbsolutePosition());
            }
        }

        Position = CheckedMath.Add(Position, read);
        return read;
    }

    internal void ReadExactly(System.Span<byte> destination)
    {
        if (destination.Length > GetRemaining())
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The exact read exceeds the bounded input window.", GetAbsolutePosition());
        }

        int filled = 0;
        while (filled < destination.Length)
        {
            int read = Read(destination.Slice(filled));
            if (read == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The input ended before the exact read completed.", GetAbsolutePosition());
            }
            filled += read;
        }
    }

    internal byte ReadByte()
    {
        if (GetRemaining() == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The bounded input window contains no remaining byte.", GetAbsolutePosition());
        }

        int value;
        if (UsesMemory)
        {
            int sourceOffset = CheckedMath.ToInt32(CheckedMath.Add(MemoryOffset, Position));
            value = Memory.Span[sourceOffset];
        }
        else
        {
            PositionStream();
            value = (Stream ?? throw new System.InvalidOperationException("A required value is unavailable.")).ReadByte();
            if (value < 0)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The input ended before a byte could be read.", GetAbsolutePosition());
            }
        }

        Position = CheckedMath.Add(Position, 1);
        return (byte)value;
    }

    internal void ReadAt(long offset, System.Span<byte> destination)
    {
        CheckedMath.RangeEnd(offset, destination.Length, Length);
        if (destination.Length == 0)
        {
            return;
        }

        long absoluteOffset = CheckedMath.Add(Start, offset);
        if (UsesMemory)
        {
            int sourceOffset = CheckedMath.ToInt32(CheckedMath.Add(MemoryOffset, offset));
            Memory.Span.Slice(sourceOffset, destination.Length).CopyTo(destination);
            return;
        }
        if (!Seekable)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Random reads require a seekable stream or memory input.", absoluteOffset);
        }

        (Stream ?? throw new System.InvalidOperationException("A required value is unavailable.")).Position = absoluteOffset;
        int filled = 0;
        while (filled < destination.Length)
        {
            int read = Stream.Read(destination.Slice(filled));
            if (read < 0 || read > destination.Length - filled)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The stream returned an invalid read count.", CheckedMath.Add(absoluteOffset, filled));
            }
            if (read == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The input ended before the random read completed.", CheckedMath.Add(absoluteOffset, filled));
            }
            filled += read;
        }
    }

    internal void Skip(long count)
    {
        long target = CheckedMath.RangeEnd(Position, count, Length);
        if (count == 0)
        {
            return;
        }

        if (UsesMemory)
        {
            Position = target;
            return;
        }

        if (Seekable)
        {
            Position = target;
            (Stream ?? throw new System.InvalidOperationException("A required value is unavailable.")).Position = GetAbsolutePosition();
            return;
        }

        while (Position < target)
        {
            long remaining = target - Position;
            int chunk = (SkipBuffer ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length;
            if (remaining < chunk)
            {
                chunk = (int)remaining;
            }
            int read = Read(SkipBuffer.AsSpan(0, chunk));
            if (read == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The input ended before the exact skip completed.", GetAbsolutePosition());
            }
        }
    }

    internal void PositionStream()
    {
        if (Seekable)
        {
            long absolutePosition = GetAbsolutePosition();
            if ((Stream ?? throw new System.InvalidOperationException("A required value is unavailable.")).Position != absolutePosition)
            {
                Stream.Position = absolutePosition;
            }
        }
    }

    internal static void SkipStreamExactly(System.IO.Stream stream, long count, long absoluteOffset, byte[] buffer)
    {
        long skipped = 0;
        while (skipped < count)
        {
            long remaining = count - skipped;
            int chunk = buffer.Length;
            if (remaining < chunk)
            {
                chunk = (int)remaining;
            }
            int read = stream.Read(buffer, 0, chunk);
            if (read < 0 || read > chunk)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The stream returned an invalid read count.", CheckedMath.Add(absoluteOffset, skipped));
            }
            if (read == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The input ended before the exact skip completed.", CheckedMath.Add(absoluteOffset, skipped));
            }
            skipped = CheckedMath.Add(skipped, read);
        }
    }
}
