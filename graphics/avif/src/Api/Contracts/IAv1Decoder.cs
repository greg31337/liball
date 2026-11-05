using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="Av1Decoder"/>.</summary>
public interface IAv1Decoder
{
    /// <summary>Returns the outcome of the most recent decoder operation.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Replaces decoder settings before a stream is loaded.</summary>
    /// <param name="requestedOptions">The options and limits to snapshot.</param>
    /// <returns>The operation result.</returns>
    AvifResultCode Configure(Av1DecoderOptions requestedOptions);

    /// <summary>Copies a complete stream and decodes its first supported frame.</summary>
    /// <param name="encoded">The complete low-overhead AV1 stream.</param>
    /// <returns>The first decoded frame result.</returns>
    AvifDecodeResult Decode(ReadOnlySpan<byte> encoded);

    /// <summary>Decodes the next supported frame from the loaded stream.</summary>
    /// <returns>The decoded frame result or <c>EndOfData</c>.</returns>
    AvifDecodeResult DecodeNext();

    /// <summary>Selects an AV1 operating-point index for subsequent input.</summary>
    /// <param name="operatingPoint">The nonnegative index, or negative one for automatic selection.</param>
    /// <returns>The operation result.</returns>
    AvifResultCode SelectOperatingPoint(int operatingPoint);

    /// <summary>Discards the loaded stream while retaining configured options.</summary>
    /// <returns>The operation result.</returns>
    AvifResultCode Reset();

    /// <summary>Releases the loaded stream and decoder state.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
