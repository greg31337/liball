using System;

namespace rtp;

/// <summary>
/// Provides typed codecs for standardized RTP header-extension payloads.
/// </summary>
public class RtpKnownHeaderExtensionCodec : IRtpKnownHeaderExtensionCodec
{
    /// <summary>
    /// Initializes a known RTP header-extension payload codec.
    /// </summary>
    public RtpKnownHeaderExtensionCodec()
    {
    }

    /// <summary>Calculates the encoded size of an RFC 6464 client audio level.</summary>
    /// <param name="value">The audio level to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    public int GetAudioLevelExtensionEncodedSize(RtpAudioLevelExtension value)
    {
        return RtpKnownHeaderExtensionWireCodec.GetAudioLevelEncodedSize(value);
    }

    /// <summary>Encodes an RFC 6464 client audio-level payload.</summary>
    /// <param name="value">The audio level to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The number of bytes written.</returns>
    public int EncodeAudioLevelExtension(
        RtpAudioLevelExtension value,
        Span<byte> destination)
    {
        return RtpKnownHeaderExtensionWireCodec.EncodeAudioLevel(value, destination);
    }

    /// <summary>Attempts to encode a client audio level without expected exceptions.</summary>
    /// <param name="value">The audio level to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The encode outcome and written count.</returns>
    public RtpCodecResult TryEncodeAudioLevelExtension(
        RtpAudioLevelExtension value,
        Span<byte> destination)
    {
        return RtpKnownHeaderExtensionWireCodec.TryEncodeAudioLevel(value, destination);
    }

    /// <summary>Decodes an RFC 6464 client audio-level payload.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The decoded audio level.</returns>
    public RtpAudioLevelExtension DecodeAudioLevelExtension(ReadOnlySpan<byte> source)
    {
        return RtpKnownHeaderExtensionWireCodec.DecodeAudioLevel(source);
    }

    /// <summary>Attempts to decode a client audio level without expected exceptions.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The outcome and decoded audio-level value.</returns>
    public RtpCodecResult TryDecodeAudioLevelExtension(ReadOnlySpan<byte> source)
    {
        return RtpKnownHeaderExtensionWireCodec.TryDecodeAudioLevel(source);
    }

    /// <summary>Calculates the encoded size of RFC 6465 mixer audio levels.</summary>
    /// <param name="value">The CSRC audio levels to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    public int GetMixerAudioLevelExtensionEncodedSize(RtpMixerAudioLevelExtension value)
    {
        return RtpKnownHeaderExtensionWireCodec.GetMixerAudioLevelEncodedSize(value);
    }

    /// <summary>Encodes RFC 6465 mixer audio-level payloads.</summary>
    /// <param name="value">The CSRC audio levels to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The number of bytes written.</returns>
    public int EncodeMixerAudioLevelExtension(
        RtpMixerAudioLevelExtension value,
        Span<byte> destination)
    {
        return RtpKnownHeaderExtensionWireCodec.EncodeMixerAudioLevel(value, destination);
    }

    /// <summary>Attempts to encode mixer audio levels without expected exceptions.</summary>
    /// <param name="value">The CSRC audio levels to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The encode outcome and written count.</returns>
    public RtpCodecResult TryEncodeMixerAudioLevelExtension(
        RtpMixerAudioLevelExtension value,
        Span<byte> destination)
    {
        return RtpKnownHeaderExtensionWireCodec.TryEncodeMixerAudioLevel(
            value,
            destination);
    }

    /// <summary>Decodes RFC 6465 mixer audio-level payloads.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The decoded CSRC audio levels.</returns>
    public RtpMixerAudioLevelExtension DecodeMixerAudioLevelExtension(
        ReadOnlySpan<byte> source)
    {
        return RtpKnownHeaderExtensionWireCodec.DecodeMixerAudioLevel(source);
    }

    /// <summary>Attempts to decode mixer audio levels without expected exceptions.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The outcome and decoded mixer audio-level values.</returns>
    public RtpCodecResult TryDecodeMixerAudioLevelExtension(ReadOnlySpan<byte> source)
    {
        return RtpKnownHeaderExtensionWireCodec.TryDecodeMixerAudioLevel(source);
    }

    /// <summary>Calculates the encoded size of an RFC 6051 NTP-64 value.</summary>
    /// <param name="value">The NTP timestamp to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    public int GetNtp64ExtensionEncodedSize(RtpNtp64Extension value)
    {
        return RtpKnownHeaderExtensionWireCodec.GetNtp64EncodedSize(value);
    }

    /// <summary>Encodes an RFC 6051 NTP-64 payload.</summary>
    /// <param name="value">The NTP timestamp to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The number of bytes written.</returns>
    public int EncodeNtp64Extension(RtpNtp64Extension value, Span<byte> destination)
    {
        return RtpKnownHeaderExtensionWireCodec.EncodeNtp64(value, destination);
    }

    /// <summary>Attempts to encode an NTP-64 value without expected exceptions.</summary>
    /// <param name="value">The NTP timestamp to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The encode outcome and written count.</returns>
    public RtpCodecResult TryEncodeNtp64Extension(
        RtpNtp64Extension value,
        Span<byte> destination)
    {
        return RtpKnownHeaderExtensionWireCodec.TryEncodeNtp64(value, destination);
    }

    /// <summary>Decodes an RFC 6051 NTP-64 payload.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The decoded NTP timestamp.</returns>
    public RtpNtp64Extension DecodeNtp64Extension(ReadOnlySpan<byte> source)
    {
        return RtpKnownHeaderExtensionWireCodec.DecodeNtp64(source);
    }

    /// <summary>Attempts to decode an NTP-64 value without expected exceptions.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The outcome and decoded NTP-64 value.</returns>
    public RtpCodecResult TryDecodeNtp64Extension(ReadOnlySpan<byte> source)
    {
        return RtpKnownHeaderExtensionWireCodec.TryDecodeNtp64(source);
    }

    /// <summary>Calculates the encoded size of a MID, RID, or repaired RID value.</summary>
    /// <param name="value">The stream identifier to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    public int GetStreamIdentifierExtensionEncodedSize(RtpStreamIdentifierExtension value)
    {
        return RtpKnownHeaderExtensionWireCodec.GetStreamIdentifierEncodedSize(value);
    }

    /// <summary>Encodes a MID, RID, or repaired RID payload.</summary>
    /// <param name="value">The stream identifier to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The number of bytes written.</returns>
    public int EncodeStreamIdentifierExtension(
        RtpStreamIdentifierExtension value,
        Span<byte> destination)
    {
        return RtpKnownHeaderExtensionWireCodec.EncodeStreamIdentifier(value, destination);
    }

    /// <summary>Attempts to encode a stream identifier without expected exceptions.</summary>
    /// <param name="value">The stream identifier to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The encode outcome and written count.</returns>
    public RtpCodecResult TryEncodeStreamIdentifierExtension(
        RtpStreamIdentifierExtension value,
        Span<byte> destination)
    {
        return RtpKnownHeaderExtensionWireCodec.TryEncodeStreamIdentifier(
            value,
            destination);
    }

    /// <summary>Decodes a MID, RID, or repaired RID payload.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <param name="uri">The registered URI identifying the value kind.</param>
    /// <returns>The decoded stream identifier.</returns>
    public RtpStreamIdentifierExtension DecodeStreamIdentifierExtension(
        ReadOnlySpan<byte> source,
        string uri)
    {
        return RtpKnownHeaderExtensionWireCodec.DecodeStreamIdentifier(source, uri);
    }

    /// <summary>Attempts to decode a stream identifier without expected exceptions.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <param name="uri">The registered URI identifying the value kind.</param>
    /// <returns>The outcome and decoded stream identifier.</returns>
    public RtpCodecResult TryDecodeStreamIdentifierExtension(
        ReadOnlySpan<byte> source,
        string uri)
    {
        return RtpKnownHeaderExtensionWireCodec.TryDecodeStreamIdentifier(source, uri);
    }
}
