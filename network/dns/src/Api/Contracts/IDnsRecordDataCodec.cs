using System;

namespace dns;

/// <summary>Encodes and decodes DNS resource records, RDATA, and EDNS options.</summary>
public interface IDnsRecordDataCodec
{
    /// <summary>Strictly decodes one schema-aware RDATA value.</summary>
    /// <param name="recordType">The recognized resource-record type.</param>
    /// <param name="recordTypeValue">The raw resource-record type integer.</param>
    /// <param name="message">The complete containing DNS message.</param>
    /// <param name="offset">The RDATA starting message offset.</param>
    /// <param name="length">The declared RDATA length.</param>
    /// <param name="options">The decoding and opaque-fallback policy.</param>
    /// <returns>The decoded schema-specific RDATA.</returns>
    DnsDecodeResult DecodeRecordData(DnsRecordType recordType, int recordTypeValue, ReadOnlySpan<byte> message, int offset, int length, DnsDecodeOptions options);

    /// <summary>Attempts to decode one schema-aware RDATA value.</summary>
    /// <param name="recordType">The recognized resource-record type.</param>
    /// <param name="recordTypeValue">The raw resource-record type integer.</param>
    /// <param name="message">The complete containing DNS message.</param>
    /// <param name="offset">The RDATA starting message offset.</param>
    /// <param name="length">The declared RDATA length.</param>
    /// <param name="options">The decoding and opaque-fallback policy.</param>
    /// <returns>The decoded RDATA or diagnostics describing the failure.</returns>
    DnsDecodeResult TryDecodeRecordData(DnsRecordType recordType, int recordTypeValue, ReadOnlySpan<byte> message, int offset, int length, DnsDecodeOptions options);

    /// <summary>Strictly encodes one schema-aware RDATA value.</summary>
    /// <param name="recordType">The recognized resource-record type.</param>
    /// <param name="recordTypeValue">The raw resource-record type integer.</param>
    /// <param name="data">The schema-specific RDATA.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The encoded RDATA bytes.</returns>
    DnsEncodeResult EncodeRecordData(DnsRecordType recordType, int recordTypeValue, DnsRecordData data, DnsEncodeOptions options);

    /// <summary>Attempts to encode one schema-aware RDATA value.</summary>
    /// <param name="recordType">The recognized resource-record type.</param>
    /// <param name="recordTypeValue">The raw resource-record type integer.</param>
    /// <param name="data">The schema-specific RDATA.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The encoded bytes or diagnostics describing the failure.</returns>
    DnsEncodeResult TryEncodeRecordData(DnsRecordType recordType, int recordTypeValue, DnsRecordData data, DnsEncodeOptions options);

    /// <summary>Strictly decodes one complete resource record.</summary>
    /// <param name="message">The complete containing DNS message.</param>
    /// <param name="offset">The resource-record starting offset.</param>
    /// <param name="options">The decoding and resource-limit policy.</param>
    /// <returns>The decoded resource record and continuation offset.</returns>
    DnsDecodeResult DecodeResourceRecord(ReadOnlySpan<byte> message, int offset, DnsDecodeOptions options);

    /// <summary>Attempts to decode one complete resource record.</summary>
    /// <param name="message">The complete containing DNS message.</param>
    /// <param name="offset">The resource-record starting offset.</param>
    /// <param name="options">The decoding and resource-limit policy.</param>
    /// <returns>The decoded record or diagnostics describing the failure.</returns>
    DnsDecodeResult TryDecodeResourceRecord(ReadOnlySpan<byte> message, int offset, DnsDecodeOptions options);

    /// <summary>Strictly encodes one complete resource record.</summary>
    /// <param name="record">The resource record to encode.</param>
    /// <param name="options">The compression, canonical, and size policy.</param>
    /// <returns>The encoded resource-record bytes.</returns>
    DnsEncodeResult EncodeResourceRecord(DnsResourceRecord record, DnsEncodeOptions options);

    /// <summary>Attempts to encode one complete resource record.</summary>
    /// <param name="record">The resource record to encode.</param>
    /// <param name="options">The compression, canonical, and size policy.</param>
    /// <returns>The encoded bytes or diagnostics describing the failure.</returns>
    DnsEncodeResult TryEncodeResourceRecord(DnsResourceRecord record, DnsEncodeOptions options);

    /// <summary>Strictly decodes one complete EDNS option.</summary>
    /// <param name="source">The option code, length, and payload bytes.</param>
    /// <param name="options">The decoding and resource-limit policy.</param>
    /// <returns>The decoded EDNS option.</returns>
    DnsEdnsOption DecodeEdnsOption(ReadOnlySpan<byte> source, DnsDecodeOptions options);

    /// <summary>Attempts to decode one complete EDNS option.</summary>
    /// <param name="source">The option code, length, and payload bytes.</param>
    /// <param name="options">The decoding and resource-limit policy.</param>
    /// <returns>The decoded option or diagnostics describing the failure.</returns>
    DnsEdnsOptionResult TryDecodeEdnsOption(ReadOnlySpan<byte> source, DnsDecodeOptions options);

    /// <summary>Strictly encodes one complete EDNS option.</summary>
    /// <param name="option">The typed or opaque option to encode.</param>
    /// <returns>The encoded option bytes.</returns>
    DnsEncodeResult EncodeEdnsOption(DnsEdnsOption option);

    /// <summary>Attempts to encode one complete EDNS option.</summary>
    /// <param name="option">The typed or opaque option to encode.</param>
    /// <returns>The encoded bytes or diagnostics describing the failure.</returns>
    DnsEdnsOptionResult TryEncodeEdnsOption(DnsEdnsOption option);
}
