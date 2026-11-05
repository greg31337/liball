using System;

namespace avif;

internal interface IInputSource
{
    /// <summary>Reads up to the requested byte count at an absolute offset.</summary>
    /// <param name="offset">The absolute source offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The read outcome and transferred byte count.</returns>
    InputReadResult Read(long offset, Span<byte> destination);

    /// <summary>Reads the entire requested range or reports it unavailable atomically.</summary>
    /// <param name="offset">The absolute source offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The read outcome and any missing range.</returns>
    InputReadResult ReadExact(long offset, Span<byte> destination);

    /// <summary>Releases source-specific state.</summary>
    /// <returns>The cleanup outcome.</returns>
    AvifResultCode Close();
}

internal class InputReadResult
{
    internal AvifResultCode Code;
    internal int BytesRead;
    internal long MissingOffset;
    internal long MissingLength;

    internal InputReadResult(
        AvifResultCode code,
        int bytesRead,
        long missingOffset,
        long missingLength)
    {
        Code = code;
        BytesRead = bytesRead;
        MissingOffset = missingOffset;
        MissingLength = missingLength;
    }
}
