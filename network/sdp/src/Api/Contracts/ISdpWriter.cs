using System;

namespace sdp;

/// <summary>Generates bounded SDP output from session descriptions.</summary>
public interface ISdpWriter
{
    /// <summary>Generates canonical or lossless SDP text according to the supplied options.</summary>
    /// <param name="description">The session description to generate.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <returns>A result containing complete generated text or structured diagnostics.</returns>
    SdpWriteResult WriteText(SdpSessionDescription description, SdpWriteOptions options);

    /// <summary>Generates canonical or lossless SDP text and throws when generation cannot complete.</summary>
    /// <param name="description">The session description to generate.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <returns>The complete generated text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or options argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the options contain an unsupported value.</exception>
    /// <exception cref="SdpGenerationException">Thrown when the description cannot be represented according to the supplied options or an output limit is exceeded.</exception>
    string WriteTextOrThrow(SdpSessionDescription description, SdpWriteOptions options);

    /// <summary>Generates canonical or lossless SDP as UTF-8 bytes without a byte-order mark.</summary>
    /// <param name="description">The session description to generate.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <returns>A result containing complete generated UTF-8 data or structured diagnostics.</returns>
    SdpWriteResult WriteUtf8Bytes(SdpSessionDescription description, SdpWriteOptions options);

    /// <summary>Generates canonical or lossless SDP as UTF-8 bytes and throws when generation cannot complete.</summary>
    /// <param name="description">The session description to generate.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <returns>The complete generated UTF-8 data without a byte-order mark.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or options argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the options contain an unsupported value.</exception>
    /// <exception cref="SdpGenerationException">Thrown when the description cannot be represented according to the supplied options or an output limit is exceeded.</exception>
    byte[] WriteUtf8BytesOrThrow(SdpSessionDescription description, SdpWriteOptions options);

    /// <summary>Writes complete canonical or lossless SDP into a caller-provided UTF-8 buffer.</summary>
    /// <param name="description">The session description to generate.</param>
    /// <param name="destination">The destination that receives complete UTF-8 output on success.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <returns>A result containing the exact required capacity and write outcome.</returns>
    SdpBufferWriteResult WriteUtf8Buffer(SdpSessionDescription description, Span<byte> destination, SdpWriteOptions options);
}
