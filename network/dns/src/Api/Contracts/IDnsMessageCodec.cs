using System;

namespace dns;

/// <summary>Encodes and decodes complete DNS wire-format messages.</summary>
public interface IDnsMessageCodec
{
    /// <summary>Strictly decodes one DNS question.</summary>
    /// <param name="source">The wire-format question bytes.</param>
    /// <param name="options">The decoding limits and strictness policy.</param>
    /// <returns>The decoded question and byte count.</returns>
    DnsDecodeResult DecodeQuestion(ReadOnlySpan<byte> source, DnsDecodeOptions options);

    /// <summary>Attempts to decode one DNS question.</summary>
    /// <param name="source">The wire-format question bytes.</param>
    /// <param name="options">The decoding limits and strictness policy.</param>
    /// <returns>The decoded question or diagnostics describing the failure.</returns>
    DnsDecodeResult TryDecodeQuestion(ReadOnlySpan<byte> source, DnsDecodeOptions options);

    /// <summary>Strictly encodes one DNS question.</summary>
    /// <param name="question">The question to encode.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The encoded question bytes.</returns>
    DnsEncodeResult EncodeQuestion(DnsQuestion question, DnsEncodeOptions options);

    /// <summary>Attempts to encode one DNS question.</summary>
    /// <param name="question">The question to encode.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The encoded bytes or diagnostics describing the failure.</returns>
    DnsEncodeResult TryEncodeQuestion(DnsQuestion question, DnsEncodeOptions options);

    /// <summary>Strictly decodes one DNS message.</summary>
    /// <param name="source">The wire-format message bytes.</param>
    /// <param name="options">The decoding limits and strictness policy.</param>
    /// <returns>The successful decoded message result.</returns>
    DnsDecodeResult DecodeMessage(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options);

    /// <summary>Attempts to decode one DNS message without using exceptions for malformed input.</summary>
    /// <param name="source">The wire-format message bytes.</param>
    /// <param name="options">The decoding limits and strictness policy.</param>
    /// <returns>The decoded value or diagnostics describing the failure.</returns>
    DnsDecodeResult TryDecodeMessage(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options);

    /// <summary>Strictly encodes one DNS message.</summary>
    /// <param name="message">The message to encode.</param>
    /// <param name="options">The encoding, ordering, and size policy.</param>
    /// <returns>The successful encoded message result.</returns>
    DnsEncodeResult EncodeMessage(
        DnsMessage message,
        DnsEncodeOptions options);

    /// <summary>Attempts to encode one DNS message without using exceptions for size failures.</summary>
    /// <param name="message">The message to encode.</param>
    /// <param name="options">The encoding, ordering, and size policy.</param>
    /// <returns>The encoded bytes or diagnostics describing the failure.</returns>
    DnsEncodeResult TryEncodeMessage(
        DnsMessage message,
        DnsEncodeOptions options);

    /// <summary>Strictly decodes one DNS-over-TCP frame.</summary>
    /// <param name="source">The length prefix and available message bytes.</param>
    /// <param name="options">The frame and message size limits.</param>
    /// <returns>The decoded complete or partial frame.</returns>
    DnsDecodeResult DecodeTcpFrame(ReadOnlySpan<byte> source, DnsDecodeOptions options);

    /// <summary>Attempts to decode one DNS-over-TCP frame.</summary>
    /// <param name="source">The length prefix and available message bytes.</param>
    /// <param name="options">The frame and message size limits.</param>
    /// <returns>The decoded frame or diagnostics describing the failure.</returns>
    DnsDecodeResult TryDecodeTcpFrame(ReadOnlySpan<byte> source, DnsDecodeOptions options);

    /// <summary>Strictly encodes one DNS-over-TCP frame.</summary>
    /// <param name="frame">The frame and unprefixed message bytes.</param>
    /// <param name="options">The frame and message size limits.</param>
    /// <returns>The length-prefixed frame bytes.</returns>
    DnsEncodeResult EncodeTcpFrame(DnsTcpFrame frame, DnsEncodeOptions options);

    /// <summary>Attempts to encode one DNS-over-TCP frame.</summary>
    /// <param name="frame">The frame and unprefixed message bytes.</param>
    /// <param name="options">The frame and message size limits.</param>
    /// <returns>The encoded frame or diagnostics describing the failure.</returns>
    DnsEncodeResult TryEncodeTcpFrame(DnsTcpFrame frame, DnsEncodeOptions options);

    /// <summary>Creates an independently mutable copy of one DNS message.</summary>
    /// <param name="message">The source message.</param>
    /// <returns>The cloned message.</returns>
    DnsMessage CloneMessage(DnsMessage message);

    /// <summary>Tests two messages for normalized semantic equality.</summary>
    /// <param name="left">The first message.</param>
    /// <param name="right">The second message.</param>
    /// <param name="options">The canonical ordering and TTL policy.</param>
    /// <returns>True when the normalized messages are equal.</returns>
    bool MessagesEqual(DnsMessage left, DnsMessage right, DnsCanonicalOrderOptions options);

    /// <summary>Estimates the encoded size of one DNS message.</summary>
    /// <param name="message">The message to measure.</param>
    /// <param name="options">The compression, canonical, and size policy.</param>
    /// <returns>The estimated encoded size in octets.</returns>
    int EstimateMessageSize(DnsMessage message, DnsEncodeOptions options);

    /// <summary>Reads the effective response code from the header and EDNS data.</summary>
    /// <param name="message">The DNS message to inspect.</param>
    /// <returns>The complete response-code integer.</returns>
    int ReadEffectiveResponseCode(DnsMessage message);

    /// <summary>Writes a complete response code into the header and EDNS data.</summary>
    /// <param name="message">The DNS message to update.</param>
    /// <param name="responseCodeValue">The complete response-code integer.</param>
    void WriteEffectiveResponseCode(DnsMessage message, int responseCodeValue);
}
