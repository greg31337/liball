using System;
using System.IO;

namespace avif;

internal class StreamInput : IInputSource
{
    internal Stream Source;
    internal bool LeaveOpen;
    internal bool Seekable;
    internal bool HasKnownLength;
    internal long KnownLength;
    internal long CurrentOffset;
    internal bool Closed;
    internal AvifResultCode Code;

    internal StreamInput(Stream source, bool leaveOpen)
    {
        Source = source;
        LeaveOpen = leaveOpen;
        Seekable = false;
        HasKnownLength = false;
        KnownLength = 0;
        CurrentOffset = 0;
        Closed = false;
        if (source == null || !source.CanRead)
        {
            Code = AvifResultCode.InvalidArgument;
        }
        else
        {
            Seekable = source.CanSeek;
            if (Seekable)
            {
                HasKnownLength = true;
                KnownLength = source.Length;
                CurrentOffset = source.Position;
            }

            Code = KnownLength < 0 ? AvifResultCode.InvalidData : AvifResultCode.Success;
        }
    }

    /// <summary>Reads up to the requested byte count from the stream.</summary>
    /// <param name="offset">The absolute source offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The read outcome and transferred byte count.</returns>
    public InputReadResult Read(long offset, Span<byte> destination)
    {
        InputReadResult? unavailable = CheckAvailable(offset);
        if (unavailable != null)
        {
            return unavailable;
        }

        if (Seekable && offset > KnownLength)
        {
            return new InputReadResult(AvifResultCode.EndOfData, 0, offset, destination.Length);
        }

        if (destination.Length == 0)
        {
            return new InputReadResult(AvifResultCode.Success, 0, 0, 0);
        }

        if (Seekable)
        {
            if (offset == KnownLength)
            {
                return new InputReadResult(AvifResultCode.EndOfData, 0, offset, destination.Length);
            }

            if (Source.Position != offset)
            {
                Source.Position = offset;
            }

            CurrentOffset = offset;
        }

        int requested = destination.Length;
        if (Seekable)
        {
            long available = KnownLength - offset;
            if (available < requested)
            {
                requested = (int)available;
            }
        }

        int count = Source.Read(destination.Slice(0, requested));
        CurrentOffset += count;
        if (count == 0)
        {
            return new InputReadResult(AvifResultCode.EndOfData, 0, offset, destination.Length);
        }

        return new InputReadResult(AvifResultCode.Success, count, 0, 0);
    }

    /// <summary>Reads an entire stream range or reports the missing suffix.</summary>
    /// <param name="offset">The absolute source offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The read outcome and any missing range.</returns>
    public InputReadResult ReadExact(long offset, Span<byte> destination)
    {
        InputReadResult? unavailable = CheckAvailable(offset);
        if (unavailable != null)
        {
            return unavailable;
        }

        if (Seekable)
        {
            CheckedRangeResult range = CheckedMath.ResolveRange(offset, 0, destination.Length, KnownLength);
            if (!range.Success)
            {
                CheckedIntegerResult requestedEndResult = CheckedMath.AddInt64(offset, destination.Length);
                if (!requestedEndResult.Success)
                {
                    return new InputReadResult(requestedEndResult.Code, 0, 0, 0);
                }

                long requestedEnd = requestedEndResult.Value;
                long missingOffset = KnownLength;
                long missingLength = requestedEnd - KnownLength;
                return new InputReadResult(range.Code, 0, missingOffset, missingLength);
            }

            if (Source.Position != offset)
            {
                Source.Position = offset;
            }

            CurrentOffset = offset;
        }

        int total = 0;
        while (total < destination.Length)
        {
            int count = Source.Read(destination.Slice(total));
            if (count == 0)
            {
                break;
            }

            total += count;
            CurrentOffset += count;
        }

        if (total != destination.Length)
        {
            Code = AvifResultCode.TruncatedData;
            return new InputReadResult(
                Code,
                total,
                offset + total,
                destination.Length - total);
        }

        return new InputReadResult(AvifResultCode.Success, total, 0, 0);
    }

    /// <summary>Releases stream input state and honors the configured ownership.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!Closed)
        {
            Closed = true;
            if (Source != null && !LeaveOpen)
            {
                Source.Close();
            }
        }

        return AvifResultCode.Success;
    }

    internal InputReadResult? CheckAvailable(long offset)
    {
        if (Closed)
        {
            return new InputReadResult(AvifResultCode.Closed, 0, 0, 0);
        }

        if (Code != AvifResultCode.Success)
        {
            return new InputReadResult(Code, 0, 0, 0);
        }

        if (offset < 0)
        {
            return new InputReadResult(AvifResultCode.InvalidArgument, 0, 0, 0);
        }

        if (!Seekable && offset != CurrentOffset)
        {
            return new InputReadResult(AvifResultCode.InvalidState, 0, CurrentOffset, 0);
        }

        return null;
    }
}
