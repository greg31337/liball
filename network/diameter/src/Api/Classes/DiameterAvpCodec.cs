using System;
using System.Collections.Generic;
using System.Net;

namespace diameter;

/// <summary>
/// Encodes and decodes Diameter AVPs and their standard value representations.
/// </summary>
public class DiameterAvpCodec : IDiameterAvpCodec
{
    /// <summary>Initializes a Diameter AVP codec.</summary>
    public DiameterAvpCodec()
    {
    }

    /// <summary>Measures the encoded size of a complete AVP.</summary>
    /// <param name="avp">The AVP to measure.</param>
    /// <returns>The measurement result.</returns>
    public DiameterEncodeResult MeasureAvp(DiameterAvp? avp)
    {
        return AvpEncoder.MeasureAvp(avp);
    }

    /// <summary>Encodes a complete AVP into a caller-provided destination.</summary>
    /// <param name="destination">The destination that receives the encoded AVP.</param>
    /// <param name="avp">The AVP to encode.</param>
    /// <returns>The encoding result.</returns>
    public DiameterEncodeResult EncodeAvp(Span<byte> destination, DiameterAvp? avp)
    {
        return AvpEncoder.EncodeAvp(destination, avp);
    }

    /// <summary>Encodes a complete AVP into an owned byte array.</summary>
    /// <param name="avp">The AVP to encode.</param>
    /// <returns>The encoding result containing owned bytes on success.</returns>
    public DiameterEncodeResult EncodeAvpToArray(DiameterAvp? avp)
    {
        DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avp);
        if (measurement.Status != DiameterCodecStatus.Success)
        {
            return measurement;
        }

        byte[] data = new byte[measurement.RequiredCount];
        DiameterEncodeResult encoding = AvpEncoder.EncodeAvp(data, avp);
        if (encoding.Status == DiameterCodecStatus.Success)
        {
            encoding.Data = data;
        }

        return encoding;
    }

    /// <summary>Decodes one complete AVP.</summary>
    /// <param name="source">The bytes containing the AVP and its padding.</param>
    /// <param name="permissive">Allows recoverable flag and padding violations as warnings.</param>
    /// <returns>The AVP decoding result.</returns>
    public DiameterAvpDecodeResult DecodeAvp(ReadOnlySpan<byte> source, bool permissive)
    {
        DiameterDecodeProfile profile = DiameterDecodeProfile.Strict;
        if (permissive)
        {
            profile = DiameterDecodeProfile.Permissive;
        }

        return AvpDecoder.DecodeAvpWithProfile(source, profile);
    }

    /// <summary>Encodes an Integer32 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeInteger32(Span<byte> destination, int value)
    {
        return ValueCodecs.EncodeInteger32(destination, value);
    }

    /// <summary>Decodes an Integer32 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeInteger32(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeInteger32(source);
    }

    /// <summary>Encodes an Integer64 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeInteger64(Span<byte> destination, long value)
    {
        return ValueCodecs.EncodeInteger64(destination, value);
    }

    /// <summary>Decodes an Integer64 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeInteger64(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeInteger64(source);
    }

    /// <summary>Encodes an Unsigned32 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeUnsigned32(Span<byte> destination, uint value)
    {
        return ValueCodecs.EncodeUnsigned32(destination, value);
    }

    /// <summary>Decodes an Unsigned32 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeUnsigned32(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeUnsigned32(source);
    }

    /// <summary>Encodes an Unsigned64 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeUnsigned64(Span<byte> destination, ulong value)
    {
        return ValueCodecs.EncodeUnsigned64(destination, value);
    }

    /// <summary>Decodes an Unsigned64 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeUnsigned64(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeUnsigned64(source);
    }

    /// <summary>Encodes a Float32 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeFloat32(Span<byte> destination, float value)
    {
        return ValueCodecs.EncodeFloat32(destination, value);
    }

    /// <summary>Decodes a Float32 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeFloat32(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeFloat32(source);
    }

    /// <summary>Encodes a Float64 value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeFloat64(Span<byte> destination, double value)
    {
        return ValueCodecs.EncodeFloat64(destination, value);
    }

    /// <summary>Decodes a Float64 value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeFloat64(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeFloat64(source);
    }

    /// <summary>Encodes an Enumerated value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The integer enumeration value.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeEnumerated(Span<byte> destination, int value)
    {
        return ValueCodecs.EncodeEnumerated(destination, value);
    }

    /// <summary>Decodes an Enumerated value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeEnumerated(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeEnumerated(source);
    }

    /// <summary>Encodes an OctetString value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The octets to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeOctetString(
        Span<byte> destination,
        ReadOnlySpan<byte> value)
    {
        return ValueCodecs.EncodeOctetString(destination, value);
    }

    /// <summary>Decodes an OctetString value into owned storage.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeOctetString(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeOctetString(source);
    }

    /// <summary>Encodes a UTF8String value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The text to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeUtf8String(Span<byte> destination, string? value)
    {
        return ValueCodecs.EncodeUtf8String(destination, value);
    }

    /// <summary>Decodes a UTF8String value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeUtf8String(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeUtf8String(source);
    }

    /// <summary>Encodes an Address value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="address">The IP address to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeAddress(Span<byte> destination, IPAddress? address)
    {
        return ValueCodecs.EncodeAddress(destination, address);
    }

    /// <summary>Decodes an Address value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeAddress(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeAddress(source);
    }

    /// <summary>Encodes a Time value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The time to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeTime(Span<byte> destination, DateTimeOffset value)
    {
        return ValueCodecs.EncodeTime(destination, value);
    }

    /// <summary>Decodes a Time value relative to a selected NTP era.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <param name="referenceTime">The time used to select the closest NTP era.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeTime(
        ReadOnlySpan<byte> source,
        DateTimeOffset referenceTime)
    {
        return ValueCodecs.DecodeTime(source, referenceTime);
    }

    /// <summary>Encodes a DiameterIdentity value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="identity">The identity to encode and normalize.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeDiameterIdentity(
        Span<byte> destination,
        DiameterIdentity? identity)
    {
        return ValueCodecs.EncodeDiameterIdentity(destination, identity);
    }

    /// <summary>Decodes and normalizes a DiameterIdentity value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeDiameterIdentity(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeDiameterIdentity(source);
    }

    /// <summary>Encodes a DiameterURI value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="uri">The URI to encode and normalize.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeDiameterUri(Span<byte> destination, DiameterUri? uri)
    {
        return ValueCodecs.EncodeDiameterUri(destination, uri);
    }

    /// <summary>Decodes and normalizes a DiameterURI value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeDiameterUri(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeDiameterUri(source);
    }

    /// <summary>Encodes an IPFilterRule value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The filter rule to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeIpFilterRule(Span<byte> destination, string? value)
    {
        return ValueCodecs.EncodeIpFilterRule(destination, value);
    }

    /// <summary>Decodes and validates an IPFilterRule value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeIpFilterRule(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeIpFilterRule(source);
    }

    /// <summary>Encodes a QoSFilterRule value.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="value">The filter rule to encode.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeQosFilterRule(Span<byte> destination, string? value)
    {
        return ValueCodecs.EncodeQosFilterRule(destination, value);
    }

    /// <summary>Decodes and validates a QoSFilterRule value.</summary>
    /// <param name="source">The encoded value bytes.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeQosFilterRule(ReadOnlySpan<byte> source)
    {
        return ValueCodecs.DecodeQosFilterRule(source);
    }

    /// <summary>Measures a Grouped value and validates configured nested AVP types.</summary>
    /// <param name="avps">The ordered AVPs in the Grouped value.</param>
    /// <param name="groupedAvpTypes">AVPs whose code and Vendor-ID identify nested Grouped values.</param>
    /// <param name="limits">The Grouped value resource limits.</param>
    /// <returns>The value measurement result.</returns>
    public DiameterValueResult MeasureGrouped(
        List<DiameterAvp>? avps,
        List<DiameterAvp>? groupedAvpTypes,
        DiameterLimitConfiguration? limits)
    {
        return ValueCodecs.MeasureGrouped(
            avps,
            CreateGroupedTypes(groupedAvpTypes),
            limits);
    }

    /// <summary>Encodes a Grouped value into a caller-provided destination.</summary>
    /// <param name="destination">The destination that receives the value.</param>
    /// <param name="avps">The ordered AVPs in the Grouped value.</param>
    /// <param name="groupedAvpTypes">AVPs whose code and Vendor-ID identify nested Grouped values.</param>
    /// <param name="limits">The Grouped value resource limits.</param>
    /// <returns>The value encoding result.</returns>
    public DiameterValueResult EncodeGrouped(
        Span<byte> destination,
        List<DiameterAvp>? avps,
        List<DiameterAvp>? groupedAvpTypes,
        DiameterLimitConfiguration? limits)
    {
        return ValueCodecs.EncodeGrouped(
            destination,
            avps,
            CreateGroupedTypes(groupedAvpTypes),
            limits);
    }

    /// <summary>Decodes a Grouped value and validates configured nested AVP types.</summary>
    /// <param name="source">The encoded Grouped value bytes.</param>
    /// <param name="groupedAvpTypes">AVPs whose code and Vendor-ID identify nested Grouped values.</param>
    /// <param name="limits">The Grouped value resource limits.</param>
    /// <returns>The value decoding result.</returns>
    public DiameterValueResult DecodeGrouped(
        ReadOnlySpan<byte> source,
        List<DiameterAvp>? groupedAvpTypes,
        DiameterLimitConfiguration? limits)
    {
        return ValueCodecs.DecodeGrouped(
            source,
            CreateGroupedTypes(groupedAvpTypes),
            limits);
    }

    internal List<GroupedAvpType>? CreateGroupedTypes(List<DiameterAvp>? avps)
    {
        if (avps == null)
        {
            return null;
        }

        List<GroupedAvpType> groupedTypes = new List<GroupedAvpType>();
        int index = 0;
        while (index < avps.Count)
        {
            DiameterAvp avp = avps[index];
            if (avp != null)
            {
                GroupedAvpType type = new GroupedAvpType();
                type.Code = avp.Code;
                type.VendorId = avp.VendorId;
                groupedTypes.Add(type);
            }

            index = index + 1;
        }

        return groupedTypes;
    }
}
