using System;

namespace diameter;

/// <summary>
/// Encodes, decodes, and incrementally frames Diameter messages.
/// </summary>
public class DiameterMessageCodec : IDiameterMessageCodec
{
    /// <summary>Initializes a Diameter message codec.</summary>
    public DiameterMessageCodec()
    {
    }

    /// <summary>Measures the encoded size of a Diameter message.</summary>
    /// <param name="message">The message to measure.</param>
    /// <returns>The measurement result.</returns>
    public DiameterEncodeResult MeasureMessage(DiameterMessage? message)
    {
        return MessageEncoder.MeasureMessage(message);
    }

    /// <summary>Encodes a Diameter message into a caller-provided destination.</summary>
    /// <param name="destination">The destination that receives the encoded message.</param>
    /// <param name="message">The message to encode.</param>
    /// <returns>The encoding result.</returns>
    public DiameterEncodeResult EncodeMessage(
        Span<byte> destination,
        DiameterMessage? message)
    {
        return MessageEncoder.EncodeMessage(destination, message);
    }

    /// <summary>Encodes a Diameter message into an owned byte array.</summary>
    /// <param name="message">The message to encode.</param>
    /// <returns>The encoding result containing owned bytes on success.</returns>
    public DiameterEncodeResult EncodeMessageToArray(DiameterMessage? message)
    {
        return MessageEncoder.EncodeMessageOwned(message);
    }

    /// <summary>Decodes one Diameter message from the supplied bytes.</summary>
    /// <param name="source">The bytes containing the message.</param>
    /// <param name="permissive">Allows recoverable flag and padding violations as warnings.</param>
    /// <param name="limits">The limits applied while decoding.</param>
    /// <returns>The message decoding result.</returns>
    public DiameterDecodeResult DecodeMessage(
        ReadOnlySpan<byte> source,
        bool permissive,
        DiameterLimitConfiguration? limits)
    {
        DiameterDecodeProfile profile = DiameterDecodeProfile.Strict;
        if (permissive)
        {
            profile = DiameterDecodeProfile.Permissive;
        }

        return MessageDecoder.DecodeMessage(source, profile, limits);
    }

    /// <summary>Extracts complete messages from retained and newly received stream bytes.</summary>
    /// <param name="retained">Bytes retained from the previous framing operation.</param>
    /// <param name="input">Newly received stream bytes.</param>
    /// <param name="endOfStream">Indicates that no more stream bytes will arrive.</param>
    /// <param name="maximumFrameSize">The largest accepted frame size in octets.</param>
    /// <returns>The framing result with owned frames and remainder bytes.</returns>
    public DiameterFramingResult FrameMessageStream(
        ReadOnlySpan<byte> retained,
        ReadOnlySpan<byte> input,
        bool endOfStream,
        int maximumFrameSize)
    {
        return StreamFramer.FrameStreamData(
            retained,
            input,
            endOfStream,
            maximumFrameSize);
    }
}
