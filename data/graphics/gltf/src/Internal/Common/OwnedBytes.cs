using System;

namespace gltf;

internal static class OwnedBytes
{
    internal static byte[] CopyArray(byte[] source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        byte[] copy = new byte[source.Length];
        source.CopyTo(copy, 0);
        return copy;
    }

    internal static byte[] CopySpan(ReadOnlySpan<byte> source)
    {
        byte[] copy = new byte[source.Length];
        source.CopyTo(copy);
        return copy;
    }

    internal static byte[] CopyRange(byte[] source, int offset, int count)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        if (count > source.Length || offset > source.Length - count)
        {
            throw new ArgumentException("The requested range is outside the source array.");
        }

        byte[] copy = new byte[count];
        Array.Copy(source, offset, copy, 0, count);
        return copy;
    }
}
