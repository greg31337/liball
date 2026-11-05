using System;

namespace rtp;

/// <summary>
/// Defines sizing, encoding, and decoding of standardized RTP header-extension payloads.
/// </summary>
public interface IRtpKnownHeaderExtensionCodec
{
    /// <summary>Calculates the encoded size of an RFC 6464 client audio level.</summary>
    /// <param name="value">The audio level to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    int GetAudioLevelExtensionEncodedSize(RtpAudioLevelExtension value);

    /// <summary>Encodes an RFC 6464 client audio-level payload.</summary>
    /// <param name="value">The audio level to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The number of bytes written.</returns>
    int EncodeAudioLevelExtension(
        RtpAudioLevelExtension value,
        Span<byte> destination);

    /// <summary>Attempts to encode a client audio level without expected exceptions.</summary>
    /// <param name="value">The audio level to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The encode outcome and written count.</returns>
    RtpCodecResult TryEncodeAudioLevelExtension(
        RtpAudioLevelExtension value,
        Span<byte> destination);

    /// <summary>Decodes an RFC 6464 client audio-level payload.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The decoded audio level.</returns>
    RtpAudioLevelExtension DecodeAudioLevelExtension(ReadOnlySpan<byte> source);

    /// <summary>Attempts to decode a client audio level without expected exceptions.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The outcome and decoded audio-level value.</returns>
    RtpCodecResult TryDecodeAudioLevelExtension(ReadOnlySpan<byte> source);

    /// <summary>Calculates the encoded size of RFC 6465 mixer audio levels.</summary>
    /// <param name="value">The CSRC audio levels to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    int GetMixerAudioLevelExtensionEncodedSize(RtpMixerAudioLevelExtension value);

    /// <summary>Encodes RFC 6465 mixer audio-level payloads.</summary>
    /// <param name="value">The CSRC audio levels to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The number of bytes written.</returns>
    int EncodeMixerAudioLevelExtension(
        RtpMixerAudioLevelExtension value,
        Span<byte> destination);

    /// <summary>Attempts to encode mixer audio levels without expected exceptions.</summary>
    /// <param name="value">The CSRC audio levels to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The encode outcome and written count.</returns>
    RtpCodecResult TryEncodeMixerAudioLevelExtension(
        RtpMixerAudioLevelExtension value,
        Span<byte> destination);

    /// <summary>Decodes RFC 6465 mixer audio-level payloads.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The decoded CSRC audio levels.</returns>
    RtpMixerAudioLevelExtension DecodeMixerAudioLevelExtension(ReadOnlySpan<byte> source);

    /// <summary>Attempts to decode mixer audio levels without expected exceptions.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The outcome and decoded mixer audio-level values.</returns>
    RtpCodecResult TryDecodeMixerAudioLevelExtension(ReadOnlySpan<byte> source);

    /// <summary>Calculates the encoded size of an RFC 6051 NTP-64 value.</summary>
    /// <param name="value">The NTP timestamp to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    int GetNtp64ExtensionEncodedSize(RtpNtp64Extension value);

    /// <summary>Encodes an RFC 6051 NTP-64 payload.</summary>
    /// <param name="value">The NTP timestamp to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The number of bytes written.</returns>
    int EncodeNtp64Extension(RtpNtp64Extension value, Span<byte> destination);

    /// <summary>Attempts to encode an NTP-64 value without expected exceptions.</summary>
    /// <param name="value">The NTP timestamp to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The encode outcome and written count.</returns>
    RtpCodecResult TryEncodeNtp64Extension(
        RtpNtp64Extension value,
        Span<byte> destination);

    /// <summary>Decodes an RFC 6051 NTP-64 payload.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The decoded NTP timestamp.</returns>
    RtpNtp64Extension DecodeNtp64Extension(ReadOnlySpan<byte> source);

    /// <summary>Attempts to decode an NTP-64 value without expected exceptions.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <returns>The outcome and decoded NTP-64 value.</returns>
    RtpCodecResult TryDecodeNtp64Extension(ReadOnlySpan<byte> source);

    /// <summary>Calculates the encoded size of a MID, RID, or repaired RID value.</summary>
    /// <param name="value">The stream identifier to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    int GetStreamIdentifierExtensionEncodedSize(RtpStreamIdentifierExtension value);

    /// <summary>Encodes a MID, RID, or repaired RID payload.</summary>
    /// <param name="value">The stream identifier to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The number of bytes written.</returns>
    int EncodeStreamIdentifierExtension(
        RtpStreamIdentifierExtension value,
        Span<byte> destination);

    /// <summary>Attempts to encode a stream identifier without expected exceptions.</summary>
    /// <param name="value">The stream identifier to encode.</param>
    /// <param name="destination">The destination receiving the payload.</param>
    /// <returns>The encode outcome and written count.</returns>
    RtpCodecResult TryEncodeStreamIdentifierExtension(
        RtpStreamIdentifierExtension value,
        Span<byte> destination);

    /// <summary>Decodes a MID, RID, or repaired RID payload.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <param name="uri">The registered URI identifying the value kind.</param>
    /// <returns>The decoded stream identifier.</returns>
    RtpStreamIdentifierExtension DecodeStreamIdentifierExtension(
        ReadOnlySpan<byte> source,
        string uri);

    /// <summary>Attempts to decode a stream identifier without expected exceptions.</summary>
    /// <param name="source">The extension payload bytes.</param>
    /// <param name="uri">The registered URI identifying the value kind.</param>
    /// <returns>The outcome and decoded stream identifier.</returns>
    RtpCodecResult TryDecodeStreamIdentifierExtension(
        ReadOnlySpan<byte> source,
        string uri);
}
