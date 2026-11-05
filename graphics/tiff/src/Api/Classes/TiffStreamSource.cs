using System;
using System.IO;

namespace tiff;

/// <summary>Provides synchronized random access to a readable, seekable stream.</summary>
public class TiffStreamSource : ITiffRandomAccessSource
{
    internal Stream Stream;
    internal bool IsOpen;
    internal long Origin;

    /// <summary>Creates a random-access source over a readable, seekable stream.</summary>
    /// <param name="stream">The stream to use. Closing the source closes this stream.</param>
    public TiffStreamSource(Stream stream)
    {
        Stream = stream;
        IsOpen = stream != null;
        if (stream != null && stream.CanSeek)
        {
            Origin = stream.Position;
        }
        else
        {
            Origin = 0;
        }
    }

    /// <summary>Indicates whether the source remains available for reads.</summary>
    /// <returns><c>true</c> while reads are permitted.</returns>
    public bool GetIsOpen()
    {
        return IsOpen && Stream != null && Stream.CanRead && Stream.CanSeek;
    }

    /// <summary>Returns the current stream length.</summary>
    /// <returns>The stream length in bytes, or zero when the source is closed.</returns>
    public long GetLength()
    {
        if (!GetIsOpen())
        {
            return 0;
        }

        lock (Stream)
        {
            if (Stream.Length < Origin)
            {
                return 0;
            }

            return Stream.Length - Origin;
        }
    }

    /// <summary>Reads bytes beginning at an absolute stream offset.</summary>
    /// <param name="offset">The zero-based byte offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The number of bytes read.</returns>
    public int Read(long offset, Span<byte> destination)
    {
        if (!GetIsOpen() || offset < 0 || offset > long.MaxValue - Origin || destination.Length == 0)
        {
            return 0;
        }

        lock (Stream)
        {
            long originalPosition = Stream.Position;
            Stream.Position = Origin + offset;
            int read = Stream.Read(destination);
            Stream.Position = originalPosition;
            return read;
        }
    }

    /// <summary>Closes the source and its underlying stream.</summary>
    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        if (Stream != null)
        {
            Stream.Dispose();
        }
    }
}
