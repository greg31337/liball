using System.Collections.Generic;
using System.IO;

namespace image;

internal class BoundedStreamReader
{
    internal const int ScratchBufferLength = 4096;

    internal Stream? Source;
    internal long StartPosition;
    internal long Length;
    internal long Limit;
    internal long Position;
    internal List<MetadataDiagnostic> Diagnostics;

    internal BoundedStreamReader(Stream? source, long length, long limit, List<MetadataDiagnostic> diagnostics)
    {
        Source = source;
        StartPosition = 0;
        Length = length;
        Limit = limit;
        Position = 0;
        Diagnostics = diagnostics;

        if (Source != null && Source.CanSeek)
        {
            StartPosition = Source.Position;
        }

        if (Length < 0)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.length.negative", "Stream length was negative.");
            Length = 0;
        }

        if (Limit < 0)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.limit.negative", "Read limit was negative.");
            Limit = 0;
        }
    }

    internal static BoundedStreamReader FromStream(Stream? source, long limit, List<MetadataDiagnostic> diagnostics)
    {
        long length = limit;

        if (source != null && source.CanSeek)
        {
            length = source.Length - source.Position;
        }

        return new BoundedStreamReader(source, length, limit, diagnostics);
    }

    internal long AccessibleLength()
    {
        if (Length < Limit)
        {
            return Length;
        }

        return Limit;
    }

    internal long RemainingLength()
    {
        long remaining = Length - Position;

        if (remaining < 0)
        {
            return 0;
        }

        return remaining;
    }

    internal long RemainingLimit()
    {
        long remaining = Limit - Position;

        if (remaining < 0)
        {
            return 0;
        }

        return remaining;
    }

    internal long Remaining()
    {
        long accessible = AccessibleLength();
        long remaining = accessible - Position;

        if (remaining < 0)
        {
            return 0;
        }

        return remaining;
    }

    internal bool EndOfRange()
    {
        return Remaining() == 0;
    }

    internal bool LimitReached()
    {
        return Position >= Limit;
    }

    internal bool CanRead(long count)
    {
        if (count < 0)
        {
            return false;
        }

        return count <= Remaining();
    }

    internal int ReadByte()
    {
        byte[] buffer = new byte[1];
        int count = Read(buffer, 0, 1);

        if (count != 1)
        {
            return -1;
        }

        return buffer[0];
    }

    internal int Read(byte[]? buffer, int offset, int count)
    {
        if (!ValidateReadBuffer(buffer, offset, count))
        {
            return 0;
        }

        if (buffer == null)
        {
            return 0;
        }

        int allowed = GetAllowedReadCount(count, "stream.read");

        if (allowed == 0)
        {
            return 0;
        }

        if (Source == null)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.source.null", "Source stream was null.");
            return 0;
        }

        int read = 0;

        while (read < allowed)
        {
            int current = Source.Read(buffer, offset + read, allowed - read);

            if (current <= 0)
            {
                break;
            }

            read = read + current;
        }

        Position = Position + read;

        if (read < allowed)
        {
            MetadataDiagnostic.AddTruncated(Diagnostics, "stream.read.short", "Source stream returned fewer bytes than requested.", "stream", Position, allowed - read);
        }

        return read;
    }

    internal byte[] ReadBytes(int count)
    {
        if (count <= 0)
        {
            if (count < 0)
            {
                MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.read.negative", "Read count was negative.");
            }

            return new byte[0];
        }

        int allowed = GetAllowedReadCount(count, "stream.read.bytes");

        if (allowed == 0)
        {
            return new byte[0];
        }

        byte[] buffer = new byte[allowed];
        int read = Read(buffer, 0, allowed);

        if (read == allowed)
        {
            return buffer;
        }

        byte[] actual = new byte[read];
        int index = 0;
        while (index < read)
        {
            actual[index] = buffer[index];
            index = index + 1;
        }

        return actual;
    }

    internal bool SkipBytes(long count)
    {
        if (count < 0)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.skip.negative", "Skip count was negative.");
            return false;
        }

        long allowed = GetAllowedLongCount(count, "stream.skip");

        if (allowed == 0)
        {
            return count == 0;
        }

        if (Source == null)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.source.null", "Source stream was null.");
            return false;
        }

        bool skipped = SkipAllowed(allowed);
        return skipped && allowed == count;
    }

    internal bool SeekAbsolute(long position)
    {
        if (position < 0)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.seek.negative", "Seek position was negative.");
            return false;
        }

        long accessible = AccessibleLength();
        long target = position;
        bool inRange = true;

        if (target > accessible)
        {
            ReportRangeDiagnostic(position - Position, "stream.seek");
            target = accessible;
            inRange = false;
        }

        if (Source == null)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.source.null", "Source stream was null.");
            return false;
        }

        if (Source.CanSeek)
        {
            if (!SeekSourceToRelativePosition(target))
            {
                return false;
            }

            Position = target;
            return inRange;
        }

        if (target < Position)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.seek.backward", "Source stream does not support backward seeking.");
            return false;
        }

        bool skipped = SkipBytes(target - Position);
        return skipped && inRange;
    }

    internal int GetAllowedReadCount(int requested, string operation)
    {
        if (requested <= 0)
        {
            if (requested < 0)
            {
                MetadataDiagnostic.AddRecoverableError(Diagnostics, operation + ".negative", "Requested byte count was negative.");
            }

            return 0;
        }

        long allowed = GetAllowedLongCount(requested, operation);

        if (allowed > requested)
        {
            return requested;
        }

        return (int)allowed;
    }

    internal long GetAllowedLongCount(long requested, string operation)
    {
        if (requested <= 0)
        {
            if (requested < 0)
            {
                MetadataDiagnostic.AddRecoverableError(Diagnostics, operation + ".negative", "Requested byte count was negative.");
            }

            return 0;
        }

        long remaining = Remaining();

        if (requested > remaining)
        {
            ReportRangeDiagnostic(requested, operation);
            return remaining;
        }

        return requested;
    }

    internal bool ValidateReadBuffer(byte[]? buffer, int offset, int count)
    {
        if (buffer == null)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.buffer.null", "Read buffer was null.");
            return false;
        }

        if (offset < 0)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.buffer.offset.negative", "Read buffer offset was negative.");
            return false;
        }

        if (count < 0)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.buffer.count.negative", "Read buffer count was negative.");
            return false;
        }

        if (offset > buffer.Length)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.buffer.offset.large", "Read buffer offset exceeded the buffer length.");
            return false;
        }

        if (count > buffer.Length - offset)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, "stream.buffer.range.large", "Read buffer range exceeded the buffer length.");
            return false;
        }

        return true;
    }

    internal void ReportRangeDiagnostic(long requested, string operation)
    {
        if (RemainingLimit() < RemainingLength())
        {
            MetadataDiagnostic.AddLimitExceeded(Diagnostics, operation + ".limit", "Requested byte count exceeded the configured read limit.", "stream", Position, requested);
            return;
        }

        MetadataDiagnostic.AddTruncated(Diagnostics, operation + ".truncated", "Requested byte count exceeded the bounded stream length.", "stream", Position, requested);
    }

    internal bool SkipAllowed(long count)
    {
        if (Source == null)
        {
            return false;
        }

        if (Source.CanSeek)
        {
            if (!SeekSourceToRelativePosition(Position + count))
            {
                return false;
            }

            Position = Position + count;
            return true;
        }

        byte[] scratch = new byte[ScratchBufferLength];
        long remaining = count;

        while (remaining > 0)
        {
            int request = ScratchBufferLength;
            if (remaining < request)
            {
                request = (int)remaining;
            }

            int read = Source.Read(scratch, 0, request);
            Position = Position + read;
            remaining = remaining - read;

            if (read == 0)
            {
                MetadataDiagnostic.AddTruncated(Diagnostics, "stream.skip.short", "Source stream ended before the requested skip completed.", "stream", Position, remaining);
                return false;
            }
        }

        return true;
    }

    internal bool SeekSourceToRelativePosition(long relativePosition)
    {
        if (Source == null)
        {
            return false;
        }

        if (StartPosition > long.MaxValue - relativePosition)
        {
            MetadataDiagnostic.AddLimitExceeded(Diagnostics, "stream.seek.overflow", "Seek target overflowed the stream position range.", "stream", Position, relativePosition);
            return false;
        }

        Source.Seek(StartPosition + relativePosition, SeekOrigin.Begin);
        return true;
    }
}
