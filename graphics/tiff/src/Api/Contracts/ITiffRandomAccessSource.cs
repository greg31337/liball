using System;

namespace tiff;

/// <summary>Defines bounded random-access reads over an open TIFF data source.</summary>
/// <remarks>Implementations must read from the supplied absolute offset without changing the logical position observed by other callers.</remarks>
public interface ITiffRandomAccessSource
{
    /// <summary>Indicates whether the source remains available for reads.</summary>
    /// <returns><c>true</c> while reads are permitted.</returns>
    bool GetIsOpen();
    /// <summary>Returns the current source length.</summary>
    /// <returns>The source length in bytes.</returns>
    long GetLength();
    /// <summary>Reads bytes beginning at an absolute source offset.</summary>
    /// <param name="offset">The zero-based byte offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The number of bytes read, which can be smaller than the destination near the end of the source.</returns>
    int Read(long offset, Span<byte> destination);
    /// <summary>Closes the source and releases resources it owns.</summary>
    void Close();
}
