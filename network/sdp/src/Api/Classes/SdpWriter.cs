using System;

namespace sdp;

/// <summary>Generates bounded SDP output from session descriptions.</summary>
public class SdpWriter : ISdpWriter
{
    /// <summary>Initializes an SDP writer.</summary>
    public SdpWriter()
    {
    }

    /// <summary>Generates canonical or lossless SDP text according to the supplied options.</summary>
    /// <param name="description">The session description to generate.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <returns>A result containing complete generated text or structured diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or options argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the options contain an unsupported value.</exception>
    public SdpWriteResult WriteText(SdpSessionDescription description, SdpWriteOptions options)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        return SdpWriterCore.WriteText(description, options);
    }

    /// <summary>Generates canonical or lossless SDP text and throws when generation cannot complete.</summary>
    /// <param name="description">The session description to generate.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <returns>The complete generated text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or options argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the options contain an unsupported value.</exception>
    /// <exception cref="SdpGenerationException">Thrown when the description cannot be represented according to the supplied options or an output limit is exceeded.</exception>
    public string WriteTextOrThrow(SdpSessionDescription description, SdpWriteOptions options)
    {
        SdpWriteResult result = WriteText(description, options);
        if (!result.Success)
        {
            throw new SdpGenerationException(SdpWriterCore.ReadFailureCode(result), null);
        }
        return result.Text;
    }

    /// <summary>Generates canonical or lossless SDP as UTF-8 bytes without a byte-order mark.</summary>
    /// <param name="description">The session description to generate.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <returns>A result containing complete generated UTF-8 data or structured diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or options argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the options contain an unsupported value.</exception>
    public SdpWriteResult WriteUtf8Bytes(SdpSessionDescription description, SdpWriteOptions options)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        return SdpUtf8WriterCore.WriteUtf8Bytes(description, options);
    }

    /// <summary>Generates canonical or lossless SDP as UTF-8 bytes and throws when generation cannot complete.</summary>
    /// <param name="description">The session description to generate.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <returns>The complete generated UTF-8 data without a byte-order mark.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or options argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the options contain an unsupported value.</exception>
    /// <exception cref="SdpGenerationException">Thrown when the description cannot be represented according to the supplied options or an output limit is exceeded.</exception>
    public byte[] WriteUtf8BytesOrThrow(SdpSessionDescription description, SdpWriteOptions options)
    {
        SdpWriteResult result = WriteUtf8Bytes(description, options);
        if (!result.Success)
        {
            throw new SdpGenerationException(SdpWriterCore.ReadFailureCode(result), null);
        }
        return result.Utf8Bytes;
    }

    /// <summary>Writes complete canonical or lossless SDP into a caller-provided UTF-8 buffer.</summary>
    /// <param name="description">The session description to generate.</param>
    /// <param name="destination">The destination that receives complete UTF-8 output on success.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <returns>A result containing the exact required capacity and write outcome.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or options argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the options contain an unsupported value.</exception>
    public SdpBufferWriteResult WriteUtf8Buffer(SdpSessionDescription description, Span<byte> destination, SdpWriteOptions options)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        return SdpUtf8WriterCore.WriteUtf8Buffer(description, destination, options);
    }
}
