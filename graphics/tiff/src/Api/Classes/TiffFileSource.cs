using System;
using System.IO;

namespace tiff;

/// <summary>Provides random access to a file using built-in .NET file APIs.</summary>
public class TiffFileSource : ITiffRandomAccessSource
{
    internal FileStream Stream;
    internal bool IsOpen;

    /// <summary>Opens a file for shared, read-only random access.</summary>
    /// <param name="path">The file path to open. Closing the source closes the file handle.</param>
    public TiffFileSource(string path)
    {
        Stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
        IsOpen = true;
    }

    /// <summary>Indicates whether the source remains available for reads.</summary>
    /// <returns><c>true</c> while reads are permitted.</returns>
    public bool GetIsOpen()
    {
        return IsOpen && Stream != null && Stream.CanRead;
    }

    /// <summary>Returns the current file length.</summary>
    /// <returns>The file length in bytes, or zero when the source is closed.</returns>
    public long GetLength()
    {
        if (!GetIsOpen())
        {
            return 0;
        }

        return Stream.Length;
    }

    /// <summary>Reads bytes beginning at an absolute file offset.</summary>
    /// <param name="offset">The zero-based byte offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The number of bytes read.</returns>
    public int Read(long offset, Span<byte> destination)
    {
        if (!GetIsOpen() || offset < 0 || destination.Length == 0)
        {
            return 0;
        }

        return RandomAccess.Read(Stream.SafeFileHandle, destination, offset);
    }

    /// <summary>Closes the source and its file handle.</summary>
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
