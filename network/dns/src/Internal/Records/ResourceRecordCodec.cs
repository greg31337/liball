using System;

namespace dns;

internal static class ResourceRecordCodec
{
    internal static DnsDecodeResult DecodeRecordData(
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        DnsDecodeResult validation = ValidateDecodeRegion(
            message, offset, length, options);
        if (!validation.Success)
        {
            return validation;
        }

        if (recordTypeValue != 41 &&
            OpaqueRecordDataCodec.ShouldUseOpaqueData(
            recordTypeValue, options))
        {
            return OpaqueRecordDataCodec.DecodeRecordData(
                recordTypeValue, message, offset, length, options);
        }

        DnsDecodeResult result = DispatchTypedDecode(
            recordTypeValue, message, offset, length, options);
        if (recordTypeValue != 41 && !result.Success &&
            options.FallBackToOpaqueRecordData &&
            TypedFallbackIsPermitted(recordTypeValue))
        {
            return SharedRecordDataCodec.DecodeOpaqueData(
                message, offset, length);
        }

        if (result.Success && result.BytesConsumed != length)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.RecordDataLengthMismatch,
                "recordDataLength",
                "The typed record codec did not consume the exact RDLENGTH.",
                offset + result.BytesConsumed);
        }

        return result;
    }

    internal static WireWriteResult WriteRecordData(
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

        if (data.Opaque != null)
        {
            return OpaqueRecordDataCodec.EncodeRecordData(
                writer, recordTypeValue, data);
        }

        return DispatchTypedWrite(
            writer, table, recordTypeValue, data, options);
    }

    internal static DnsEncodeResult EncodeRecordData(
        int recordTypeValue,
        DnsRecordData data,
        DnsEncodeOptions options)
    {
        DnsEncodeResult validation = ValidateEncodeOptions(options);
        if (!validation.Success)
        {
            return validation;
        }

        byte[] destination = new byte[options.MaximumMessageLength];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        WireWriteResult write = WriteRecordData(
            writer,
            new CompressionTable(0),
            recordTypeValue,
            data,
            options);
        if (!write.Success)
        {
            return FromWriteFailure(write);
        }

        if (writer.WrittenLength() > 65535)
        {
            return EncodeFailure(
                DnsDiagnosticCode.InvalidLength,
                "recordDataLength",
                "Encoded RDATA cannot exceed 65535 octets.",
                0);
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.Bytes = writer.CopyWrittenBytes();
        result.BytesWritten = writer.WrittenLength();
        return result;
    }

    internal static DnsDecodeResult DecodeResourceRecord(
        ReadOnlySpan<byte> message,
        int offset,
        DnsDecodeOptions options)
    {
        return DecodeResourceRecordCore(
            message, offset, options, false);
    }

    internal static DnsDecodeResult DecodeUpdateResourceRecord(
        ReadOnlySpan<byte> message,
        int offset,
        DnsDecodeOptions options)
    {
        return DecodeResourceRecordCore(
            message, offset, options, true);
    }

    static DnsDecodeResult DecodeResourceRecordCore(
        ReadOnlySpan<byte> message,
        int offset,
        DnsDecodeOptions options,
        bool allowUpdateControl)
    {
        DnsDecodeResult optionValidation = ValidateDecodeRegion(
            message, offset, 0, options);
        if (!optionValidation.Success)
        {
            return optionValidation;
        }

        DnsDecodeResult owner = CompressedDnsNameDecoder.DecodeName(
            message, offset, options);
        if (!owner.Success || owner.Name == null)
        {
            return owner;
        }

        int headerOffset = offset + owner.BytesConsumed;
        if (message.Length - headerOffset < 10)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "resourceRecord.header",
                "A resource-record header requires ten octets after its owner name.",
                headerOffset);
        }

        int recordTypeValue = DnsHeaderCodec.ReadUnsigned16(
            message, headerOffset);
        int classValue = DnsHeaderCodec.ReadUnsigned16(
            message, headerOffset + 2);
        uint timeToLive = ReadUnsigned32(message, headerOffset + 4);
        int recordDataLength = DnsHeaderCodec.ReadUnsigned16(
            message, headerOffset + 8);
        int recordDataOffset = headerOffset + 10;
        if (recordDataLength > options.MaximumRecordDataLength)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "resourceRecord.recordDataLength",
                "The declared RDLENGTH exceeds the configured limit.",
                headerOffset + 8);
        }

        if (recordDataLength > message.Length - recordDataOffset)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.RecordDataLengthMismatch,
                "resourceRecord.recordDataLength",
                "The declared RDLENGTH exceeds the available message bytes.",
                headerOffset + 8);
        }

        DnsDecodeResult dataResult;
        if (allowUpdateControl && recordDataLength == 0 &&
            (classValue == (int)DnsClass.Any ||
             classValue == (int)DnsClass.None))
        {
            dataResult = new DnsDecodeResult();
            dataResult.Success = true;
            dataResult.RecordData = new DnsRecordData();
            dataResult.RecordData.Opaque = new DnsOpaqueData();
        }
        else
        {
            dataResult = DecodeRecordData(
                recordTypeValue,
                message,
                recordDataOffset,
                recordDataLength,
                options);
        }
        if (!dataResult.Success || dataResult.RecordData == null)
        {
            return dataResult;
        }

        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = owner.Name;
        record.RecordType = (DnsRecordType)recordTypeValue;
        record.RecordTypeValue = recordTypeValue;
        record.Class = (DnsClass)classValue;
        record.ClassValue = classValue;
        record.TimeToLive = timeToLive;
        record.Data = dataResult.RecordData;
        record.RawData = SharedRecordDataCodec.CopyRegion(
            message, recordDataOffset, recordDataLength);
        if (recordTypeValue == 41)
        {
            DnsDecodeResult edns =
                EdnsRecordDataCodec.PopulateDecodedRecord(record, offset);
            if (!edns.Success)
            {
                return edns;
            }
        }

        int continuationOffset = recordDataOffset + recordDataLength;
        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        result.Record = record;
        result.RecordData = dataResult.RecordData;
        result.BytesConsumed = continuationOffset - offset;
        result.TrailingBytes = SharedRecordDataCodec.CopyRegion(
            message, continuationOffset, message.Length - continuationOffset);
        return result;
    }

    internal static WireWriteResult WriteResourceRecord(
        WireWriter writer,
        CompressionTable table,
        DnsResourceRecord record,
        DnsEncodeOptions options)
    {
        return WriteResourceRecordCore(
            writer, table, record, options, false);
    }

    internal static WireWriteResult WriteUpdateResourceRecord(
        WireWriter writer,
        CompressionTable table,
        DnsResourceRecord record,
        DnsEncodeOptions options)
    {
        return WriteResourceRecordCore(
            writer, table, record, options, true);
    }

    static WireWriteResult WriteResourceRecordCore(
        WireWriter writer,
        CompressionTable table,
        DnsResourceRecord record,
        DnsEncodeOptions options,
        bool allowUpdateControl)
    {
        if (writer == null || table == null || record == null || options == null ||
            record.Name == null || record.RawData == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "resourceRecord",
                "A complete resource record and encoding state are required.",
                0);
        }

        int recordTypeValue = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue, (int)record.RecordType);
        int classValue = DnsHeaderCodec.ResolveCode(
            record.ClassValue, (int)record.Class);
        uint timeToLive = record.TimeToLive;
        if (recordTypeValue == 41)
        {
            EdnsRecordHeaderResult edns =
                EdnsRecordDataCodec.ReadEncodedHeader(record);
            if (!edns.Success)
            {
                DnsDiagnostic diagnostic = edns.Diagnostics[0];
                return writer.FailAt(
                    diagnostic.Code,
                    diagnostic.Field == null ? "edns" : diagnostic.Field,
                    diagnostic.Reason,
                    writer.WriteOffset());
            }

            classValue = edns.UdpPayloadSize;
            timeToLive = edns.TimeToLive;
        }

        if (recordTypeValue < 0 || recordTypeValue > 65535 ||
            classValue < 0 || classValue > 65535)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "resourceRecord",
                "Resource-record type and class values must fit in sixteen bits.",
                writer.WriteOffset());
        }

        bool updateControl = allowUpdateControl &&
            (classValue == (int)DnsClass.Any ||
             classValue == (int)DnsClass.None) &&
            timeToLive == 0 && UpdateControlDataIsEmpty(record);
        if (RecordDataModelCatalog.ReadRecordDataModel(recordTypeValue) ==
            RecordDataModel.MetaQuery && !updateControl)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "resourceRecord.type",
                "A query-only or metadata-only type cannot be encoded as a resource record.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult owner = table.WriteName(
            writer, record.Name, options, true);
        if (!owner.Success) return owner;
        WireWriteResult type = writer.WriteUnsigned16((ulong)recordTypeValue);
        if (!type.Success) return type;
        WireWriteResult recordClass = writer.WriteUnsigned16((ulong)classValue);
        if (!recordClass.Success) return recordClass;
        WireWriteResult ttl = writer.WriteUnsigned32(timeToLive);
        if (!ttl.Success) return ttl;
        WireWriteResult reservation = writer.ReserveRecordDataLength();
        if (!reservation.Success) return reservation;
        int recordDataOffset = writer.WriteOffset();
        WireWriteResult dataResult;
        if (updateControl)
        {
            dataResult = writer.WriteBytes([]);
        }
        else if (record.UseRawData)
        {
            dataResult = writer.WriteBytes(record.RawData);
        }
        else if (record.Data == null)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "resourceRecord.data",
                "Typed or opaque resource-record data is required.",
                recordDataOffset);
        }
        else
        {
            dataResult = WriteRecordData(
                writer, table, recordTypeValue, record.Data, options);
        }

        if (!dataResult.Success) return dataResult;
        WireWriteResult patch = writer.PatchRecordDataLength(
            reservation.PatchOffset, recordDataOffset);
        if (!patch.Success) return patch;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    static bool UpdateControlDataIsEmpty(DnsResourceRecord record)
    {
        if (record.RawData == null || record.RawData.Length != 0)
        {
            return false;
        }

        if (record.UseRawData)
        {
            return true;
        }

        return record.Data != null && record.Data.Opaque != null &&
            record.Data.Opaque.Bytes != null &&
            record.Data.Opaque.Bytes.Length == 0;
    }

    internal static DnsEncodeResult EncodeResourceRecord(
        DnsResourceRecord record,
        DnsEncodeOptions options)
    {
        DnsEncodeResult validation = ValidateEncodeOptions(options);
        if (!validation.Success)
        {
            return validation;
        }

        byte[] destination = new byte[options.MaximumMessageLength];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        WireWriteResult write = WriteResourceRecord(
            writer,
            new CompressionTable(0),
            record,
            options);
        if (!write.Success)
        {
            return FromWriteFailure(write);
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.Bytes = writer.CopyWrittenBytes();
        result.BytesWritten = writer.WrittenLength();
        return result;
    }

    static DnsDecodeResult DispatchTypedDecode(
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        if (recordTypeValue >= 1 && recordTypeValue <= 16)
        {
            return Rfc1035RecordDataCodec.DecodeRecordData(
                recordTypeValue, message, offset, length, options);
        }

        if (recordTypeValue == 41)
        {
            return EdnsRecordDataCodec.DecodeRecordData(
                message, offset, length, options);
        }

        if ((recordTypeValue >= 17 && recordTypeValue <= 23) ||
            recordTypeValue == 26 || recordTypeValue == 27 ||
            recordTypeValue == 36 || recordTypeValue == 38)
        {
            return LegacyRecordDataCodec.DecodeRecordData(
                recordTypeValue, message, offset, length, options);
        }

        if (SecurityTypeIsValid(recordTypeValue))
        {
            return SecurityRecordDataCodec.DecodeRecordData(
                recordTypeValue, message, offset, length, options);
        }

        if (recordTypeValue == 30 || recordTypeValue == 47 ||
            recordTypeValue == 50 || recordTypeValue == 51 ||
            recordTypeValue == 62)
        {
            return DenialRecordDataCodec.DecodeRecordData(
                recordTypeValue, message, offset, length, options);
        }

        if (ModernTypeIsValid(recordTypeValue))
        {
            return ModernRecordDataCodec.DecodeRecordData(
                recordTypeValue, message, offset, length, options);
        }

        if (ExtendedTypeIsValid(recordTypeValue))
        {
            return ExtendedRecordDataCodec.DecodeRecordData(
                recordTypeValue, message, offset, length, options);
        }

        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "No typed record-data codec is available for this type.",
            offset);
    }

    static WireWriteResult DispatchTypedWrite(
        WireWriter writer,
        CompressionTable table,
        int recordTypeValue,
        DnsRecordData data,
        DnsEncodeOptions options)
    {
        if (recordTypeValue >= 1 && recordTypeValue <= 16)
        {
            return Rfc1035RecordDataCodec.EncodeRecordData(
                writer, table, recordTypeValue, data, options);
        }

        if (recordTypeValue == 41)
        {
            return EdnsRecordDataCodec.EncodeRecordData(writer, data);
        }

        if ((recordTypeValue >= 17 && recordTypeValue <= 23) ||
            recordTypeValue == 26 || recordTypeValue == 27 ||
            recordTypeValue == 36 || recordTypeValue == 38)
        {
            return LegacyRecordDataCodec.EncodeRecordData(
                writer, table, recordTypeValue, data, options);
        }

        if (SecurityTypeIsValid(recordTypeValue))
        {
            return SecurityRecordDataCodec.EncodeRecordData(
                writer, table, recordTypeValue, data, options);
        }

        if (recordTypeValue == 30 || recordTypeValue == 47 ||
            recordTypeValue == 50 || recordTypeValue == 51 ||
            recordTypeValue == 62)
        {
            return DenialRecordDataCodec.EncodeRecordData(
                writer, table, recordTypeValue, data, options);
        }

        if (ModernTypeIsValid(recordTypeValue))
        {
            return ModernRecordDataCodec.EncodeRecordData(
                writer, table, recordTypeValue, data, options);
        }

        if (ExtendedTypeIsValid(recordTypeValue))
        {
            return ExtendedRecordDataCodec.EncodeRecordData(
                writer, table, recordTypeValue, data, options);
        }

        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "No typed record-data codec is available for this type.",
            writer.WriteOffset());
    }

    static bool SecurityTypeIsValid(int recordTypeValue)
    {
        return recordTypeValue == 24 || recordTypeValue == 25 ||
            (recordTypeValue >= 43 && recordTypeValue <= 46) ||
            recordTypeValue == 48 || recordTypeValue == 49 ||
            recordTypeValue == 52 || recordTypeValue == 53 ||
            recordTypeValue == 55 ||
            (recordTypeValue >= 59 && recordTypeValue <= 61);
    }

    static bool ModernTypeIsValid(int recordTypeValue)
    {
        return recordTypeValue == 28 || recordTypeValue == 29 ||
            recordTypeValue == 33 || recordTypeValue == 35 ||
            recordTypeValue == 37 || recordTypeValue == 39 ||
            recordTypeValue == 42 || recordTypeValue == 108 ||
            recordTypeValue == 109 || recordTypeValue == 256 ||
            recordTypeValue == 257;
    }

    static bool ExtendedTypeIsValid(int recordTypeValue)
    {
        return (recordTypeValue >= 63 && recordTypeValue <= 68) ||
            (recordTypeValue >= 104 && recordTypeValue <= 107) ||
            recordTypeValue == 128 || recordTypeValue == 249 ||
            recordTypeValue == 250 || recordTypeValue == 260 ||
            recordTypeValue == 261;
    }

    static bool TypedFallbackIsPermitted(int recordTypeValue)
    {
        if (recordTypeValue < 0 || recordTypeValue > 65535 ||
            OpaqueRecordDataCodec.IsAutomaticallyOpaqueType(recordTypeValue))
        {
            return false;
        }

        RecordDataModel model =
            RecordDataModelCatalog.ReadRecordDataModel(recordTypeValue);
        return model != RecordDataModel.Unassigned &&
            model != RecordDataModel.MetaQuery;
    }

    static DnsDecodeResult ValidateDecodeRegion(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        if (options == null)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS decoding options are required.",
                offset);
        }

        if (options.MaximumMessageLength < 0 ||
            options.MaximumMessageLength > DnsProtocolLimits.MaximumTcpMessageLength)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumMessageLength",
                "The maximum message length must fit the DNS message size.",
                offset);
        }

        if (options.MaximumRecordDataLength < 0 ||
            options.MaximumRecordDataLength > DnsProtocolLimits.MaximumTcpMessageLength)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumRecordDataLength",
                "The maximum RDATA length must fit the DNS wire field.",
                offset);
        }

        if (message.Length > options.MaximumMessageLength)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "message",
                "The DNS message exceeds the configured length limit.",
                0);
        }

        if (length > options.MaximumRecordDataLength)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "recordDataLength",
                "The RDATA exceeds the configured length limit.",
                offset);
        }

        return SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
    }

    static DnsEncodeResult ValidateEncodeOptions(DnsEncodeOptions options)
    {
        if (options == null)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS encoding options are required.",
                0);
        }

        if (options.MaximumMessageLength < 0 ||
            options.MaximumMessageLength > DnsProtocolLimits.MaximumTcpMessageLength)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumMessageLength",
                "The maximum message length must fit the DNS message size.",
                0);
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        return result;
    }

    static uint ReadUnsigned32(ReadOnlySpan<byte> source, int offset)
    {
        return ((uint)source[offset] << 24) |
            ((uint)source[offset + 1] << 16) |
            ((uint)source[offset + 2] << 8) |
            source[offset + 3];
    }

    static DnsEncodeResult FromWriteFailure(WireWriteResult write)
    {
        DnsEncodeResult result = new DnsEncodeResult();
        int index = 0;
        while (index < write.Diagnostics.Count)
        {
            result.Diagnostics.Add(write.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static DnsEncodeResult EncodeFailure(
        DnsDiagnosticCode code,
        string field,
        string reason,
        int offset)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.HasOffset = true;
        diagnostic.Offset = offset;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        DnsEncodeResult result = new DnsEncodeResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
