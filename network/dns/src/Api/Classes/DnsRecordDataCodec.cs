using System;

namespace dns;

/// <summary>Encodes and decodes DNS resource records, RDATA, and opaque EDNS options.</summary>
public class DnsRecordDataCodec : IDnsRecordDataCodec
{
    /// <summary>Creates a DNS resource-record data codec.</summary>
    public DnsRecordDataCodec()
    {
    }

    /// <summary>Strictly decodes one schema-aware RDATA value.</summary>
    /// <param name="recordType">The recognized record type.</param>
    /// <param name="recordTypeValue">The raw record-type integer.</param>
    /// <param name="message">The complete containing message.</param>
    /// <param name="offset">The RDATA starting offset.</param>
    /// <param name="length">The declared RDATA length.</param>
    /// <param name="options">The decoding and fallback policy.</param>
    /// <returns>The successful decoded RDATA result.</returns>
    /// <exception cref="DnsFormatException">The RDATA is malformed.</exception>
    public DnsDecodeResult DecodeRecordData(
        DnsRecordType recordType,
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        DnsDecodeResult result = this.TryDecodeRecordData(
            recordType,
            recordTypeValue,
            message,
            offset,
            length,
            options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The DNS record data could not be decoded."));
        }

        return result;
    }

    /// <summary>Attempts to decode one schema-aware RDATA value.</summary>
    /// <param name="recordType">The recognized record type.</param>
    /// <param name="recordTypeValue">The raw record-type integer.</param>
    /// <param name="message">The complete containing message.</param>
    /// <param name="offset">The RDATA starting offset.</param>
    /// <param name="length">The declared RDATA length.</param>
    /// <param name="options">The decoding and fallback policy.</param>
    /// <returns>The decoded data or failure diagnostics.</returns>
    public DnsDecodeResult TryDecodeRecordData(
        DnsRecordType recordType,
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        int typeValue = DnsHeaderCodec.ResolveCode(
            recordTypeValue, (int)recordType);
        return ResourceRecordCodec.DecodeRecordData(
            typeValue, message, offset, length, options);
    }

    /// <summary>Strictly encodes one schema-aware RDATA value.</summary>
    /// <param name="recordType">The recognized record type.</param>
    /// <param name="recordTypeValue">The raw record-type integer.</param>
    /// <param name="data">The schema-specific data.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The successful encoded RDATA result.</returns>
    /// <exception cref="DnsFormatException">The RDATA cannot be encoded.</exception>
    public DnsEncodeResult EncodeRecordData(
        DnsRecordType recordType,
        int recordTypeValue,
        DnsRecordData data,
        DnsEncodeOptions options)
    {
        DnsEncodeResult result = this.TryEncodeRecordData(
            recordType, recordTypeValue, data, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The DNS record data could not be encoded."));
        }

        return result;
    }

    /// <summary>Attempts to encode one schema-aware RDATA value.</summary>
    /// <param name="recordType">The recognized record type.</param>
    /// <param name="recordTypeValue">The raw record-type integer.</param>
    /// <param name="data">The schema-specific data.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The encoded bytes or failure diagnostics.</returns>
    public DnsEncodeResult TryEncodeRecordData(
        DnsRecordType recordType,
        int recordTypeValue,
        DnsRecordData data,
        DnsEncodeOptions options)
    {
        int typeValue = DnsHeaderCodec.ResolveCode(
            recordTypeValue, (int)recordType);
        return ResourceRecordCodec.EncodeRecordData(
            typeValue, data, options);
    }

    /// <summary>Strictly decodes one complete resource record.</summary>
    /// <param name="message">The complete containing message.</param>
    /// <param name="offset">The record starting offset.</param>
    /// <param name="options">The decoding and resource-limit policy.</param>
    /// <returns>The successful decoded resource-record result.</returns>
    /// <exception cref="DnsFormatException">The record is malformed.</exception>
    public DnsDecodeResult DecodeResourceRecord(
        ReadOnlySpan<byte> message,
        int offset,
        DnsDecodeOptions options)
    {
        DnsDecodeResult result = this.TryDecodeResourceRecord(
            message, offset, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The DNS resource record could not be decoded."));
        }

        return result;
    }

    /// <summary>Attempts to decode one complete resource record.</summary>
    /// <param name="message">The complete containing message.</param>
    /// <param name="offset">The record starting offset.</param>
    /// <param name="options">The decoding and resource-limit policy.</param>
    /// <returns>The decoded record or failure diagnostics.</returns>
    public DnsDecodeResult TryDecodeResourceRecord(
        ReadOnlySpan<byte> message,
        int offset,
        DnsDecodeOptions options)
    {
        return ResourceRecordCodec.DecodeResourceRecord(
            message, offset, options);
    }

    /// <summary>Strictly encodes one complete resource record.</summary>
    /// <param name="record">The record to encode.</param>
    /// <param name="options">The compression, canonical, and size policy.</param>
    /// <returns>The successful encoded resource-record result.</returns>
    /// <exception cref="DnsFormatException">The record cannot be encoded.</exception>
    public DnsEncodeResult EncodeResourceRecord(
        DnsResourceRecord record,
        DnsEncodeOptions options)
    {
        DnsEncodeResult result = this.TryEncodeResourceRecord(record, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The DNS resource record could not be encoded."));
        }

        return result;
    }

    /// <summary>Attempts to encode one complete resource record.</summary>
    /// <param name="record">The record to encode.</param>
    /// <param name="options">The compression, canonical, and size policy.</param>
    /// <returns>The encoded bytes or failure diagnostics.</returns>
    public DnsEncodeResult TryEncodeResourceRecord(
        DnsResourceRecord record,
        DnsEncodeOptions options)
    {
        return ResourceRecordCodec.EncodeResourceRecord(record, options);
    }

    /// <summary>Strictly decodes one typed or opaque EDNS option.</summary>
    /// <param name="source">The option code, length, and payload.</param>
    /// <param name="options">The decoding and resource-limit policy.</param>
    /// <returns>The decoded EDNS option.</returns>
    /// <exception cref="DnsFormatException">The option is malformed.</exception>
    public DnsEdnsOption DecodeEdnsOption(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        DnsEdnsOptionResult result = this.TryDecodeEdnsOption(
            source, options);
        if (!result.Success || result.Option == null)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The EDNS option could not be decoded."));
        }

        return result.Option;
    }

    /// <summary>Attempts to decode one typed or opaque EDNS option.</summary>
    /// <param name="source">The option code, length, and payload.</param>
    /// <param name="options">The decoding and resource-limit policy.</param>
    /// <returns>The decoded option or failure diagnostics.</returns>
    public DnsEdnsOptionResult TryDecodeEdnsOption(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        return TypedEdnsOptionCodec.DecodeOption(source, options);
    }

    /// <summary>Strictly encodes one typed or opaque EDNS option.</summary>
    /// <param name="option">The typed or raw-data option to encode.</param>
    /// <returns>The successful encoded option bytes.</returns>
    /// <exception cref="DnsFormatException">The option cannot be encoded.</exception>
    public DnsEncodeResult EncodeEdnsOption(DnsEdnsOption option)
    {
        DnsEdnsOptionResult optionResult = this.TryEncodeEdnsOption(option);
        if (!optionResult.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    optionResult.Diagnostics,
                    "The EDNS option could not be encoded."));
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.Bytes = optionResult.Bytes;
        result.BytesWritten = optionResult.BytesProcessed;
        return result;
    }

    /// <summary>Attempts to encode one typed or opaque EDNS option.</summary>
    /// <param name="option">The typed or raw-data option to encode.</param>
    /// <returns>The encoded bytes or failure diagnostics.</returns>
    public DnsEdnsOptionResult TryEncodeEdnsOption(DnsEdnsOption option)
    {
        return TypedEdnsOptionCodec.EncodeOption(option);
    }
}
