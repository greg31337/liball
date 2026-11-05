using System;

namespace dns;

internal static class OpaqueRecordDataCodec
{
    internal static bool IsAutomaticallyOpaqueType(int recordTypeValue)
    {
        if (!RecordTypeIsDataBearing(recordTypeValue))
        {
            return false;
        }

        RecordDataModel model =
            RecordDataModelCatalog.ReadRecordDataModel(recordTypeValue);
        if (model == RecordDataModel.Unassigned)
        {
            return true;
        }

        if (model != RecordDataModel.Opaque)
        {
            return false;
        }

        return recordTypeValue != 67 && recordTypeValue != 68;
    }

    internal static bool ShouldUseOpaqueData(
        int recordTypeValue,
        DnsDecodeOptions options)
    {
        if (options == null || !RecordTypeIsDataBearing(recordTypeValue))
        {
            return false;
        }

        if (IsAutomaticallyOpaqueType(recordTypeValue))
        {
            return true;
        }

        return !options.DecodeTypedRecordData;
    }

    internal static DnsDecodeResult DecodeRecordData(
        int recordTypeValue,
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

        if (!RecordTypeIsDataBearing(recordTypeValue))
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "recordType",
                "The record type cannot carry opaque resource data.",
                offset);
        }

        if (!ShouldUseOpaqueData(recordTypeValue, options))
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "recordType",
                "Typed decoding is selected for this record type.",
                offset);
        }

        return SharedRecordDataCodec.DecodeOpaqueData(
            message, offset, length);
    }

    internal static WireWriteResult EncodeRecordData(
        WireWriter writer,
        int recordTypeValue,
        DnsRecordData data)
    {
        if (writer == null || data == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "recordData",
                "Record data and encoding state are required.",
                0);
        }

        if (!RecordTypeIsDataBearing(recordTypeValue))
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "recordType",
                "The record type cannot carry opaque resource data.",
                writer.WriteOffset());
        }

        if (data.Opaque == null)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "opaque",
                "Opaque record data is required.",
                writer.WriteOffset());
        }

        return SharedRecordDataCodec.EncodeOpaqueData(writer, data.Opaque);
    }

    static bool RecordTypeIsDataBearing(int recordTypeValue)
    {
        if (recordTypeValue < 0 || recordTypeValue > 65535)
        {
            return false;
        }

        return RecordDataModelCatalog.ReadRecordDataModel(recordTypeValue) !=
            RecordDataModel.MetaQuery;
    }
}
