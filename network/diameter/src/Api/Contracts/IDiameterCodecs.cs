using System;
using System.Collections.Generic;
using System.Net;

namespace diameter;

/// <summary>
/// Encodes, decodes, and incrementally frames Diameter messages.
/// </summary>
public interface IDiameterMessageCodec
{
    /// <summary>Measures the encoded size of a Diameter message.</summary>
    /// <param name="message">The message to measure.</param>
    /// <returns>The measurement result.</returns>
    DiameterEncodeResult MeasureMessage(DiameterMessage? message);

    /// <summary>Encodes a Diameter message into a caller-provided destination.</summary>
    /// <param name="destination">The destination that receives the encoded message.</param>
    /// <param name="message">The message to encode.</param>
    /// <returns>The encoding result.</returns>
    DiameterEncodeResult EncodeMessage(Span<byte> destination, DiameterMessage? message);

    /// <summary>Encodes a Diameter message into an owned byte array.</summary>
    /// <param name="message">The message to encode.</param>
    /// <returns>The encoding result containing owned bytes on success.</returns>
    DiameterEncodeResult EncodeMessageToArray(DiameterMessage? message);

    /// <summary>Decodes one Diameter message from the supplied bytes.</summary>
    /// <param name="source">The bytes containing the message.</param>
    /// <param name="permissive">Allows recoverable flag and padding violations as warnings.</param>
    /// <param name="limits">The limits applied while decoding.</param>
    /// <returns>The message decoding result.</returns>
    DiameterDecodeResult DecodeMessage(
        ReadOnlySpan<byte> source,
        bool permissive,
        DiameterLimitConfiguration? limits);

    /// <summary>Extracts complete messages from retained and newly received stream bytes.</summary>
    /// <param name="retained">Bytes retained from the previous framing operation.</param>
    /// <param name="input">Newly received stream bytes.</param>
    /// <param name="endOfStream">Indicates that no more stream bytes will arrive.</param>
    /// <param name="maximumFrameSize">The largest accepted frame size in octets.</param>
    /// <returns>The framing result with owned frames and remainder bytes.</returns>
    DiameterFramingResult FrameMessageStream(
        ReadOnlySpan<byte> retained,
        ReadOnlySpan<byte> input,
        bool endOfStream,
        int maximumFrameSize);
}

/// <summary>
/// Encodes and decodes Diameter AVPs and their standard value representations.
/// </summary>
public interface IDiameterAvpCodec
{
    /// <summary>Measures the encoded size of a complete AVP.</summary>
    /// <param name="avp">The AVP to measure.</param>
    /// <returns>The measurement result.</returns>
    DiameterEncodeResult MeasureAvp(DiameterAvp? avp);

    /// <summary>Encodes a complete AVP into a caller-provided destination.</summary>
    /// <param name="destination">The destination that receives the encoded AVP.</param>
    /// <param name="avp">The AVP to encode.</param>
    /// <returns>The encoding result.</returns>
    DiameterEncodeResult EncodeAvp(Span<byte> destination, DiameterAvp? avp);

    /// <summary>Encodes a complete AVP into an owned byte array.</summary>
    /// <param name="avp">The AVP to encode.</param>
    /// <returns>The encoding result containing owned bytes on success.</returns>
    DiameterEncodeResult EncodeAvpToArray(DiameterAvp? avp);

    /// <summary>Decodes one complete AVP.</summary>
    /// <param name="source">The bytes containing the AVP and its padding.</param>
    /// <param name="permissive">Allows recoverable flag and padding violations as warnings.</param>
    /// <returns>The AVP decoding result.</returns>
    DiameterAvpDecodeResult DecodeAvp(ReadOnlySpan<byte> source, bool permissive);

    /// <summary>Encodes an Integer32 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeInteger32(Span<byte> destination, int value);

    /// <summary>Decodes an Integer32 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeInteger32(ReadOnlySpan<byte> source);

    /// <summary>Encodes an Integer64 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeInteger64(Span<byte> destination, long value);

    /// <summary>Decodes an Integer64 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeInteger64(ReadOnlySpan<byte> source);

    /// <summary>Encodes an Unsigned32 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeUnsigned32(Span<byte> destination, uint value);

    /// <summary>Decodes an Unsigned32 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeUnsigned32(ReadOnlySpan<byte> source);

    /// <summary>Encodes an Unsigned64 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeUnsigned64(Span<byte> destination, ulong value);

    /// <summary>Decodes an Unsigned64 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeUnsigned64(ReadOnlySpan<byte> source);

    /// <summary>Encodes a Float32 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeFloat32(Span<byte> destination, float value);

    /// <summary>Decodes a Float32 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeFloat32(ReadOnlySpan<byte> source);

    /// <summary>Encodes a Float64 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeFloat64(Span<byte> destination, double value);

    /// <summary>Decodes a Float64 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeFloat64(ReadOnlySpan<byte> source);

    /// <summary>Encodes an Enumerated value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The integer enumeration value.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeEnumerated(Span<byte> destination, int value);

    /// <summary>Decodes an Enumerated value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeEnumerated(ReadOnlySpan<byte> source);

    /// <summary>Encodes an OctetString value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The octets to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeOctetString(Span<byte> destination, ReadOnlySpan<byte> value);

    /// <summary>Decodes an OctetString value into owned storage.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeOctetString(ReadOnlySpan<byte> source);

    /// <summary>Encodes a UTF8String value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The text to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeUtf8String(Span<byte> destination, string? value);

    /// <summary>Decodes a UTF8String value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeUtf8String(ReadOnlySpan<byte> source);

    /// <summary>Encodes an Address value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="address">The IP address to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeAddress(Span<byte> destination, IPAddress? address);

    /// <summary>Decodes an Address value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeAddress(ReadOnlySpan<byte> source);

    /// <summary>Encodes a Time value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The time to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeTime(Span<byte> destination, DateTimeOffset value);

    /// <summary>Decodes a Time value relative to a selected NTP era.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <param name="referenceTime">The time used to select the closest NTP era.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeTime(ReadOnlySpan<byte> source, DateTimeOffset referenceTime);

    /// <summary>Encodes a DiameterIdentity value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="identity">The identity to encode and normalize.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeDiameterIdentity(
        Span<byte> destination,
        DiameterIdentity? identity);

    /// <summary>Decodes and normalizes a DiameterIdentity value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeDiameterIdentity(ReadOnlySpan<byte> source);

    /// <summary>Encodes a DiameterURI value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="uri">The URI to encode and normalize.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeDiameterUri(Span<byte> destination, DiameterUri? uri);

    /// <summary>Decodes and normalizes a DiameterURI value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeDiameterUri(ReadOnlySpan<byte> source);

    /// <summary>Encodes an IPFilterRule value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The filter rule to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeIpFilterRule(Span<byte> destination, string? value);

    /// <summary>Decodes and validates an IPFilterRule value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeIpFilterRule(ReadOnlySpan<byte> source);

    /// <summary>Encodes a QoSFilterRule value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The filter rule to encode.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeQosFilterRule(Span<byte> destination, string? value);

    /// <summary>Decodes and validates a QoSFilterRule value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeQosFilterRule(ReadOnlySpan<byte> source);

    /// <summary>Measures a Grouped value and validates configured nested AVP types.</summary>
    /// <param name="avps">The ordered AVPs in the Grouped value.</param>
    /// <param name="groupedAvpTypes">AVPs whose code and Vendor-ID identify nested Grouped values.</param>
    /// <param name="limits">The Grouped value resource limits.</param>
    /// <returns>The value measurement result.</returns>
    DiameterValueResult MeasureGrouped(
        List<DiameterAvp>? avps,
        List<DiameterAvp>? groupedAvpTypes,
        DiameterLimitConfiguration? limits);

    /// <summary>Encodes a Grouped value into a caller-provided destination.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="avps">The ordered AVPs in the Grouped value.</param>
    /// <param name="groupedAvpTypes">AVPs whose code and Vendor-ID identify nested Grouped values.</param>
    /// <param name="limits">The Grouped value resource limits.</param>
    /// <returns>The value encoding result.</returns>
    DiameterValueResult EncodeGrouped(
        Span<byte> destination,
        List<DiameterAvp>? avps,
        List<DiameterAvp>? groupedAvpTypes,
        DiameterLimitConfiguration? limits);

    /// <summary>Decodes a Grouped value and validates configured nested AVP types.</summary>
    /// <param name="source">The encoded Grouped value bytes.</param>
    /// <param name="groupedAvpTypes">AVPs whose code and Vendor-ID identify nested Grouped values.</param>
    /// <param name="limits">The Grouped value resource limits.</param>
    /// <returns>The value decoding result.</returns>
    DiameterValueResult DecodeGrouped(
        ReadOnlySpan<byte> source,
        List<DiameterAvp>? groupedAvpTypes,
        DiameterLimitConfiguration? limits);
}
