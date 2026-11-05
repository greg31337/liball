using System;
using System.IO;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifEncodeResult"/>.</summary>
public interface IAvifEncodeResult
{
    /// <summary>Returns the encode outcome.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns an independent copy of the encoded AVIF or AV1 bytes.</summary>
    /// <returns>The encoded bytes, or an empty array on failure.</returns>
    byte[] GetData();

    /// <summary>Returns the encoded byte count.</summary>
    /// <returns>The output length, or zero on failure.</returns>
    int GetDataLength();

    /// <summary>Copies encoded AVIF or AV1 bytes into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer, which may be larger than the output.</param>
    /// <returns>Success, BufferTooSmall, or the encode failure code.</returns>
    AvifResultCode CopyDataTo(Span<byte> destination);

    /// <summary>Writes encoded AVIF or AV1 bytes without closing the destination stream.</summary>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, InvalidArgument, or the encode failure code.</returns>
    AvifResultCode WriteDataTo(Stream destination);
}
