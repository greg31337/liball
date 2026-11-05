using System;
using System.IO;

namespace avif;

internal class MemoryOutput
{
    internal byte[] Buffer;
    internal int Length;
    internal bool Expandable;
    internal long MaxBytes;
    internal bool Closed;
    internal AvifResultCode Code;

    internal static MemoryOutput FromDestination(byte[] destination)
    {
        int length = destination == null ? 0 : destination.Length;
        MemoryOutput result = new MemoryOutput(0, length);
        result.Buffer = destination == null
            ? System.Array.Empty<byte>() : destination;
        result.Expandable = false;
        result.Code = destination == null
            ? AvifResultCode.InvalidArgument : AvifResultCode.Success;
        return result;
    }

    internal MemoryOutput(int initialCapacity, long maxBytes)
    {
        Length = 0;
        Expandable = true;
        MaxBytes = maxBytes;
        Closed = false;
        if (initialCapacity < 0
            || maxBytes < 0
            || initialCapacity > maxBytes
            || initialCapacity > System.Array.MaxLength)
        {
            Buffer = System.Array.Empty<byte>();
            Code = AvifResultCode.InvalidArgument;
        }
        else
        {
            Buffer = new byte[initialCapacity];
            Code = AvifResultCode.Success;
        }
    }

    internal static MemoryOutput Create(byte[] destination)
    {
        return FromDestination(destination);
    }

    internal static MemoryOutput Create(int initialCapacity, long maxBytes)
    {
        return new MemoryOutput(initialCapacity, maxBytes);
    }

    internal AvifResultCode Write(ReadOnlySpan<byte> source)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (!EnsureCapacity(source.Length))
        {
            return Code;
        }

        source.CopyTo(Buffer.AsSpan(Length, source.Length));
        Length += source.Length;
        return Code;
    }

    internal byte[] ToArray()
    {
        byte[] result = new byte[Length];
        Buffer.AsSpan(0, Length).CopyTo(result);
        return result;
    }

    internal AvifResultCode Close()
    {
        Closed = true;
        return AvifResultCode.Success;
    }

    internal bool EnsureCapacity(int appendLength)
    {
        if (appendLength < 0 || Length > System.Array.MaxLength - appendLength)
        {
            Code = AvifResultCode.LimitExceeded;
            return false;
        }

        int required = Length + appendLength;
        if (required > MaxBytes || required > System.Array.MaxLength)
        {
            Code = Expandable ? AvifResultCode.LimitExceeded : AvifResultCode.BufferTooSmall;
            return false;
        }

        if (required <= Buffer.Length)
        {
            return true;
        }

        if (!Expandable)
        {
            Code = AvifResultCode.BufferTooSmall;
            return false;
        }

        long doubled = Buffer.Length == 0 ? 256 : (long)Buffer.Length * 2;
        long capacity = doubled < required ? required : doubled;
        if (capacity > MaxBytes)
        {
            capacity = MaxBytes;
        }

        if (capacity > System.Array.MaxLength)
        {
            capacity = System.Array.MaxLength;
        }

        if (capacity < required)
        {
            Code = AvifResultCode.LimitExceeded;
            return false;
        }

        byte[] expanded = new byte[(int)capacity];
        Buffer.AsSpan(0, Length).CopyTo(expanded);
        Buffer = expanded;
        return true;
    }
}

internal class StreamOutput
{
    internal Stream Destination;
    internal bool LeaveOpen;
    internal long BytesWritten;
    internal bool Closed;
    internal AvifResultCode Code;

    internal StreamOutput(Stream destination, bool leaveOpen)
    {
        Destination = destination;
        LeaveOpen = leaveOpen;
        BytesWritten = 0;
        Closed = false;
        Code = destination == null || !destination.CanWrite
            ? AvifResultCode.InvalidArgument
            : AvifResultCode.Success;
    }

    internal AvifResultCode Write(ReadOnlySpan<byte> source)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (BytesWritten > long.MaxValue - source.Length)
        {
            Code = AvifResultCode.LimitExceeded;
            return Code;
        }

        Destination.Write(source);
        BytesWritten += source.Length;
        return Code;
    }

    internal AvifResultCode Flush()
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code == AvifResultCode.Success)
        {
            Destination.Flush();
        }

        return Code;
    }

    internal AvifResultCode Close()
    {
        if (!Closed)
        {
            Closed = true;
            if (Destination != null && !LeaveOpen)
            {
                Destination.Close();
            }
        }

        return AvifResultCode.Success;
    }
}
