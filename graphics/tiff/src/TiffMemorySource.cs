using System;

namespace tiff;

internal class TiffMemorySource : ITiffRandomAccessSource
{
    internal byte[] Bytes;
    internal bool IsOpen;

    internal TiffMemorySource(byte[] bytes)
    {
        if (bytes == null)
        {
            Bytes = new byte[0];
        }
        else
        {
            Bytes = bytes;
        }

        IsOpen = true;
    }

    public bool GetIsOpen()
    {
        return IsOpen;
    }

    public long GetLength()
    {
        if (!IsOpen)
        {
            return 0;
        }

        return Bytes.Length;
    }

    public int Read(long offset, Span<byte> destination)
    {
        if (!IsOpen || offset < 0 || offset > Bytes.Length || destination.Length == 0)
        {
            return 0;
        }

        int count = destination.Length;
        long remaining = Bytes.Length - offset;
        if (remaining < count)
        {
            count = (int)remaining;
        }

        Bytes.AsSpan((int)offset, count).CopyTo(destination);
        return count;
    }

    public void Close()
    {
        IsOpen = false;
        Bytes = new byte[0];
    }
}
