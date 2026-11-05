using System;

namespace dns;

/// <summary>Encodes, decodes, and inspects complete DNS messages and frames.</summary>
public class DnsMessageCodec : IDnsMessageCodec
{
    /// <summary>Creates a DNS message codec.</summary>
    public DnsMessageCodec()
    {
    }

    /// <summary>Strictly decodes one DNS question.</summary>
    /// <param name="source">The wire-format question bytes.</param>
    /// <param name="options">The decoding limits and strictness policy.</param>
    /// <returns>The successful decoded question result.</returns>
    /// <exception cref="DnsFormatException">The question is malformed.</exception>
    public DnsDecodeResult DecodeQuestion(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        DnsDecodeResult result = this.TryDecodeQuestion(source, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The DNS question could not be decoded."));
        }

        return result;
    }

    /// <summary>Attempts to decode one DNS question.</summary>
    /// <param name="source">The wire-format question bytes.</param>
    /// <param name="options">The decoding limits and strictness policy.</param>
    /// <returns>The decoded question or failure diagnostics.</returns>
    public DnsDecodeResult TryDecodeQuestion(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        if (options == null)
        {
            return DnsHeaderCodec.DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS decode options are required.",
                0);
        }

        return DnsQuestionCodec.DecodeQuestion(
            source, 0, options, options.RequireCompleteInput);
    }

    /// <summary>Strictly encodes one DNS question.</summary>
    /// <param name="question">The question to encode.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The successful encoded question result.</returns>
    /// <exception cref="DnsFormatException">The question cannot be encoded.</exception>
    public DnsEncodeResult EncodeQuestion(
        DnsQuestion question,
        DnsEncodeOptions options)
    {
        DnsEncodeResult result = this.TryEncodeQuestion(question, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The DNS question could not be encoded."));
        }

        return result;
    }

    /// <summary>Attempts to encode one DNS question.</summary>
    /// <param name="question">The question to encode.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The encoded question or failure diagnostics.</returns>
    public DnsEncodeResult TryEncodeQuestion(
        DnsQuestion question,
        DnsEncodeOptions options)
    {
        return DnsQuestionCodec.EncodeQuestion(question, options);
    }

    /// <summary>Strictly decodes one complete DNS message.</summary>
    /// <param name="source">The wire-format message bytes.</param>
    /// <param name="options">The decoding limits and strictness policy.</param>
    /// <returns>The successful decoded message result.</returns>
    /// <exception cref="DnsFormatException">The message is malformed.</exception>
    public DnsDecodeResult DecodeMessage(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        DnsDecodeResult result = this.TryDecodeMessage(source, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The DNS message could not be decoded."));
        }

        return result;
    }

    /// <summary>Attempts to decode one complete DNS message.</summary>
    /// <param name="source">The wire-format message bytes.</param>
    /// <param name="options">The decoding limits and strictness policy.</param>
    /// <returns>The decoded message or failure diagnostics.</returns>
    public DnsDecodeResult TryDecodeMessage(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        return DnsMessageDecoder.DecodeMessage(source, options);
    }

    /// <summary>Strictly encodes one complete DNS message.</summary>
    /// <param name="message">The message to encode.</param>
    /// <param name="options">The ordering and size policy.</param>
    /// <returns>The successful encoded message result.</returns>
    /// <exception cref="DnsFormatException">The message cannot be encoded.</exception>
    public DnsEncodeResult EncodeMessage(
        DnsMessage message,
        DnsEncodeOptions options)
    {
        DnsEncodeResult result = this.TryEncodeMessage(message, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The DNS message could not be encoded."));
        }

        return result;
    }

    /// <summary>Attempts to encode one complete DNS message.</summary>
    /// <param name="message">The message to encode.</param>
    /// <param name="options">The ordering and size policy.</param>
    /// <returns>The encoded message or failure diagnostics.</returns>
    public DnsEncodeResult TryEncodeMessage(
        DnsMessage message,
        DnsEncodeOptions options)
    {
        return DnsMessageEncoder.EncodeMessage(message, options);
    }

    /// <summary>Strictly decodes one DNS-over-TCP frame.</summary>
    /// <param name="source">The length prefix and available message bytes.</param>
    /// <param name="options">The frame and message limits.</param>
    /// <returns>The successful complete or partial frame result.</returns>
    /// <exception cref="DnsFormatException">The frame prefix is invalid.</exception>
    public DnsDecodeResult DecodeTcpFrame(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        DnsDecodeResult result = this.TryDecodeTcpFrame(source, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The DNS-over-TCP frame could not be decoded."));
        }

        return result;
    }

    /// <summary>Attempts to decode one DNS-over-TCP frame.</summary>
    /// <param name="source">The length prefix and available message bytes.</param>
    /// <param name="options">The frame and message limits.</param>
    /// <returns>The decoded frame or failure diagnostics.</returns>
    public DnsDecodeResult TryDecodeTcpFrame(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        return DnsTcpFrameCodec.DecodeTcpFrame(source, options);
    }

    /// <summary>Strictly encodes one DNS-over-TCP frame.</summary>
    /// <param name="frame">The complete frame to encode.</param>
    /// <param name="options">The frame and message limits.</param>
    /// <returns>The successful encoded frame result.</returns>
    /// <exception cref="DnsFormatException">The frame cannot be encoded.</exception>
    public DnsEncodeResult EncodeTcpFrame(
        DnsTcpFrame frame,
        DnsEncodeOptions options)
    {
        DnsEncodeResult result = this.TryEncodeTcpFrame(frame, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The DNS-over-TCP frame could not be encoded."));
        }

        return result;
    }

    /// <summary>Attempts to encode one DNS-over-TCP frame.</summary>
    /// <param name="frame">The complete frame to encode.</param>
    /// <param name="options">The frame and message limits.</param>
    /// <returns>The encoded frame or failure diagnostics.</returns>
    public DnsEncodeResult TryEncodeTcpFrame(
        DnsTcpFrame frame,
        DnsEncodeOptions options)
    {
        return DnsTcpFrameCodec.EncodeTcpFrame(frame, options);
    }

    /// <summary>Creates an independently mutable copy of one DNS message.</summary>
    /// <param name="message">The source message.</param>
    /// <returns>The cloned message.</returns>
    public DnsMessage CloneMessage(DnsMessage message)
    {
        return MessageOperations.CloneMessage(message);
    }

    /// <summary>Tests two messages for normalized semantic equality.</summary>
    /// <param name="left">The first message.</param>
    /// <param name="right">The second message.</param>
    /// <param name="options">The canonical comparison policy.</param>
    /// <returns>True when both normalized messages are equal.</returns>
    public bool MessagesEqual(
        DnsMessage left,
        DnsMessage right,
        DnsCanonicalOrderOptions options)
    {
        return MessageOperations.MessagesEqual(left, right, options);
    }

    /// <summary>Estimates the encoded size of one DNS message.</summary>
    /// <param name="message">The message to measure.</param>
    /// <param name="options">The compression and canonical policy.</param>
    /// <returns>The estimated encoded size in octets.</returns>
    public int EstimateMessageSize(
        DnsMessage message,
        DnsEncodeOptions options)
    {
        return MessageOperations.EstimateMessageSize(message, options);
    }

    /// <summary>Reads the complete base and extended response code.</summary>
    /// <param name="message">The message to inspect.</param>
    /// <returns>The effective response-code integer.</returns>
    public int ReadEffectiveResponseCode(DnsMessage message)
    {
        return MessageOperations.ReadEffectiveResponseCode(message);
    }

    /// <summary>Writes a complete base and extended response code.</summary>
    /// <param name="message">The message to update.</param>
    /// <param name="responseCodeValue">The complete response-code integer.</param>
    public void WriteEffectiveResponseCode(
        DnsMessage message,
        int responseCodeValue)
    {
        MessageOperations.WriteEffectiveResponseCode(
            message, responseCodeValue);
    }
}
