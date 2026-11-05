using System;
using System.Collections.Generic;

namespace dns;

internal class TsigPlacementResult
{
    internal bool Success;
    internal bool HasTsig;
    internal DnsResourceRecord? Record;
    internal DnsMessageSection Section;
    internal int RecordIndex;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class TransactionRecordDataCodec
{
    internal static DnsDecodeResult DecodeRecordData(
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        if (recordTypeValue == 249)
        {
            return DecodeTkeyData(message, offset, length);
        }

        if (recordTypeValue == 250)
        {
            return DecodeTsigData(message, offset, length);
        }

        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not TKEY or TSIG.",
            offset);
    }

    internal static WireWriteResult EncodeRecordData(
        WireWriter writer,
        CompressionTable table,
        int recordTypeValue,
        DnsRecordData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "recordData",
                "Record data and encoding state are required.",
                0);
        }

        if (recordTypeValue == 249)
        {
            if (data.Tkey == null)
            {
                return WriteFailure(writer, "tkey", "TKEY data is required.");
            }

            return EncodeTkeyData(writer, table, data.Tkey, options);
        }

        if (recordTypeValue == 250)
        {
            if (data.Tsig == null)
            {
                return WriteFailure(writer, "tsig", "TSIG data is required.");
            }

            return EncodeTsigData(writer, table, data.Tsig, options);
        }

        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not TKEY or TSIG.",
            writer.WriteOffset());
    }

    internal static DnsDecodeResult DecodeTkeyData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult region = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!region.Success)
        {
            return region;
        }

        if (length < 17)
        {
            return DecodeFailure(
                "tkey",
                "TKEY data is too short to contain its fixed fields.",
                offset + length);
        }

        DnsDecodeResult algorithm =
            SharedRecordDataCodec.DecodeUncompressedNamePrefix(
                message, offset, length, "tkey.algorithmName");
        if (!algorithm.Success || algorithm.Name == null)
        {
            return algorithm;
        }

        int cursor = offset + algorithm.BytesConsumed;
        int end = offset + length;
        if (end - cursor < 16)
        {
            return DecodeFailure(
                "tkey",
                "TKEY data is too short to contain its fixed fields.",
                cursor);
        }

        DnsTkeyData tkey = new DnsTkeyData();
        tkey.AlgorithmName = algorithm.Name;
        tkey.Inception = ReadUnsigned32(message, cursor);
        tkey.Expiration = ReadUnsigned32(message, cursor + 4);
        tkey.Mode = DnsHeaderCodec.ReadUnsigned16(message, cursor + 8);
        tkey.Error = DnsHeaderCodec.ReadUnsigned16(message, cursor + 10);
        int keyLength = DnsHeaderCodec.ReadUnsigned16(message, cursor + 12);
        cursor = cursor + 14;
        if (keyLength > end - cursor - 2)
        {
            return DecodeFailure(
                "tkey.key",
                "The TKEY key data is truncated.",
                cursor);
        }

        tkey.Key = SharedRecordDataCodec.CopyRegion(
            message, cursor, keyLength);
        cursor = cursor + keyLength;
        int otherLength = DnsHeaderCodec.ReadUnsigned16(message, cursor);
        cursor = cursor + 2;
        if (otherLength != end - cursor)
        {
            return DecodeFailure(
                "tkey.otherData",
                otherLength > end - cursor ?
                    "The TKEY other data is truncated." :
                    "The TKEY other-data length does not consume the RDATA.",
                cursor);
        }

        tkey.OtherData = SharedRecordDataCodec.CopyRegion(
            message, cursor, otherLength);
        DnsRecordData data = new DnsRecordData();
        data.Tkey = tkey;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeTkeyData(
        WireWriter writer,
        CompressionTable table,
        DnsTkeyData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.AlgorithmName == null || data.Key == null || data.OtherData == null)
        {
            return WriteFailure(
                writer, "tkey", "Complete TKEY data and encoding state are required.");
        }

        if (!Unsigned16IsValid(data.Mode) || !Unsigned16IsValid(data.Error) ||
            data.Key.Length > 65535 || data.OtherData.Length > 65535)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "tkey",
                "TKEY integer fields and byte strings must fit their wire widths.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult algorithm = table.WriteName(
            writer, data.AlgorithmName, options, false);
        if (!algorithm.Success) return algorithm;
        WireWriteResult inception = writer.WriteUnsigned32(data.Inception);
        if (!inception.Success) return inception;
        WireWriteResult expiration = writer.WriteUnsigned32(data.Expiration);
        if (!expiration.Success) return expiration;
        WireWriteResult mode = writer.WriteUnsigned16((ulong)data.Mode);
        if (!mode.Success) return mode;
        WireWriteResult error = writer.WriteUnsigned16((ulong)data.Error);
        if (!error.Success) return error;
        WireWriteResult keyLength = writer.WriteUnsigned16((ulong)data.Key.Length);
        if (!keyLength.Success) return keyLength;
        WireWriteResult key = writer.WriteBytes(data.Key);
        if (!key.Success) return key;
        WireWriteResult otherLength = writer.WriteUnsigned16(
            (ulong)data.OtherData.Length);
        if (!otherLength.Success) return otherLength;
        WireWriteResult other = writer.WriteBytes(data.OtherData);
        if (!other.Success) return other;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeTsigData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult region = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!region.Success)
        {
            return region;
        }

        if (length < 17)
        {
            return DecodeFailure(
                "tsig",
                "TSIG data is too short to contain its fixed fields.",
                offset + length);
        }

        DnsDecodeResult algorithm =
            SharedRecordDataCodec.DecodeUncompressedNamePrefix(
                message, offset, length, "tsig.algorithmName");
        if (!algorithm.Success || algorithm.Name == null)
        {
            return algorithm;
        }

        int cursor = offset + algorithm.BytesConsumed;
        int end = offset + length;
        if (end - cursor < 16)
        {
            return DecodeFailure(
                "tsig",
                "TSIG data is too short to contain its fixed fields.",
                cursor);
        }

        DnsTsigData tsig = new DnsTsigData();
        tsig.AlgorithmName = algorithm.Name;
        tsig.TimeSigned = ReadUnsigned48(message, cursor);
        tsig.Fudge = DnsHeaderCodec.ReadUnsigned16(message, cursor + 6);
        int macLength = DnsHeaderCodec.ReadUnsigned16(message, cursor + 8);
        cursor = cursor + 10;
        if (macLength > end - cursor - 6)
        {
            return DecodeFailure(
                "tsig.mac",
                "The TSIG message authentication code is truncated.",
                cursor);
        }

        tsig.Mac = SharedRecordDataCodec.CopyRegion(
            message, cursor, macLength);
        cursor = cursor + macLength;
        tsig.OriginalIdentifier = DnsHeaderCodec.ReadUnsigned16(message, cursor);
        int errorValue = DnsHeaderCodec.ReadUnsigned16(message, cursor + 2);
        tsig.Error = (DnsTsigError)errorValue;
        tsig.ErrorValue = errorValue;
        int otherLength = DnsHeaderCodec.ReadUnsigned16(message, cursor + 4);
        cursor = cursor + 6;
        if (otherLength != end - cursor)
        {
            return DecodeFailure(
                "tsig.otherData",
                otherLength > end - cursor ?
                    "The TSIG other data is truncated." :
                    "The TSIG other-data length does not consume the RDATA.",
                cursor);
        }

        tsig.OtherData = SharedRecordDataCodec.CopyRegion(
            message, cursor, otherLength);
        DnsRecordData data = new DnsRecordData();
        data.Tsig = tsig;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeTsigData(
        WireWriter writer,
        CompressionTable table,
        DnsTsigData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.AlgorithmName == null || data.Mac == null || data.OtherData == null)
        {
            return WriteFailure(
                writer, "tsig", "Complete TSIG data and encoding state are required.");
        }

        int errorValue = DnsHeaderCodec.ResolveCode(
            data.ErrorValue, (int)data.Error);
        if (data.TimeSigned > 281474976710655 ||
            !Unsigned16IsValid(data.Fudge) ||
            !Unsigned16IsValid(data.OriginalIdentifier) ||
            !Unsigned16IsValid(errorValue) ||
            data.Mac.Length > 65535 || data.OtherData.Length > 65535)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "tsig",
                "TSIG integer fields and byte strings must fit their wire widths.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult algorithm = table.WriteName(
            writer, data.AlgorithmName, options, false);
        if (!algorithm.Success) return algorithm;
        WireWriteResult time = writer.WriteUnsigned48(data.TimeSigned);
        if (!time.Success) return time;
        WireWriteResult fudge = writer.WriteUnsigned16((ulong)data.Fudge);
        if (!fudge.Success) return fudge;
        WireWriteResult macLength = writer.WriteUnsigned16((ulong)data.Mac.Length);
        if (!macLength.Success) return macLength;
        WireWriteResult mac = writer.WriteBytes(data.Mac);
        if (!mac.Success) return mac;
        WireWriteResult identifier = writer.WriteUnsigned16(
            (ulong)data.OriginalIdentifier);
        if (!identifier.Success) return identifier;
        WireWriteResult error = writer.WriteUnsigned16((ulong)errorValue);
        if (!error.Success) return error;
        WireWriteResult otherLength = writer.WriteUnsigned16(
            (ulong)data.OtherData.Length);
        if (!otherLength.Success) return otherLength;
        WireWriteResult other = writer.WriteBytes(data.OtherData);
        if (!other.Success) return other;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static TsigPlacementResult InspectTsigPlacement(DnsMessage message)
    {
        if (message == null || message.Answers == null ||
            message.Authorities == null || message.Additionals == null)
        {
            return PlacementFailure(
                DnsMessageSection.Additional,
                0,
                "A complete DNS message is required to inspect TSIG placement.");
        }

        int index = 0;
        while (index < message.Answers.Count)
        {
            if (RecordIsTsig(message.Answers[index]))
            {
                return PlacementFailure(
                    DnsMessageSection.Answer,
                    index,
                    "A TSIG record cannot appear in the Answer section.");
            }

            index = index + 1;
        }

        index = 0;
        while (index < message.Authorities.Count)
        {
            if (RecordIsTsig(message.Authorities[index]))
            {
                return PlacementFailure(
                    DnsMessageSection.Authority,
                    index,
                    "A TSIG record cannot appear in the Authority section.");
            }

            index = index + 1;
        }

        TsigPlacementResult result = new TsigPlacementResult();
        result.Success = true;
        index = 0;
        while (index < message.Additionals.Count)
        {
            DnsResourceRecord record = message.Additionals[index];
            if (RecordIsTsig(record))
            {
                if (index != message.Additionals.Count - 1)
                {
                    return PlacementFailure(
                        DnsMessageSection.Additional,
                        index,
                        "A TSIG record must be the final Additional record.");
                }

                result.HasTsig = true;
                result.Record = record;
                result.Section = DnsMessageSection.Additional;
                result.RecordIndex = index;
            }

            index = index + 1;
        }

        return result;
    }

    static uint ReadUnsigned32(ReadOnlySpan<byte> source, int offset)
    {
        return ((uint)source[offset] << 24) |
            ((uint)source[offset + 1] << 16) |
            ((uint)source[offset + 2] << 8) |
            source[offset + 3];
    }

    static ulong ReadUnsigned48(ReadOnlySpan<byte> source, int offset)
    {
        return ((ulong)source[offset] << 40) |
            ((ulong)source[offset + 1] << 32) |
            ((ulong)source[offset + 2] << 24) |
            ((ulong)source[offset + 3] << 16) |
            ((ulong)source[offset + 4] << 8) |
            source[offset + 5];
    }

    static bool Unsigned16IsValid(int value)
    {
        return value >= 0 && value <= 65535;
    }

    static bool RecordIsTsig(DnsResourceRecord record)
    {
        if (record == null)
        {
            return false;
        }

        return DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue, (int)record.RecordType) == 250;
    }

    static TsigPlacementResult PlacementFailure(
        DnsMessageSection section,
        int recordIndex,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidTsigPlacement;
        diagnostic.HasSection = true;
        diagnostic.Section = section;
        diagnostic.HasRecordIndex = true;
        diagnostic.RecordIndex = recordIndex;
        diagnostic.Field = "tsig.placement";
        diagnostic.Reason = reason;
        TsigPlacementResult result = new TsigPlacementResult();
        result.Section = section;
        result.RecordIndex = recordIndex;
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    static DnsDecodeResult DecodeFailure(
        string field,
        string reason,
        int offset)
    {
        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidRecordData,
            field,
            reason,
            offset);
    }

    static WireWriteResult WriteFailure(
        WireWriter? writer,
        string field,
        string reason)
    {
        if (writer == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.InvalidRecordData,
                field,
                reason,
                0);
        }

        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            field,
            reason,
            writer.WriteOffset());
    }
}
