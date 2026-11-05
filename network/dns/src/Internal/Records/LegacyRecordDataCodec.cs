using System;

namespace dns;

internal static class LegacyRecordDataCodec
{
    internal static DnsDecodeResult DecodeRecordData(
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        if (recordTypeValue == 17)
        {
            return SharedRecordDataCodec.DecodeTwoNameData(
                message, offset, length, options);
        }

        if (recordTypeValue == 18 || recordTypeValue == 21 ||
            recordTypeValue == 36)
        {
            return SharedRecordDataCodec.DecodePreferenceNameData(
                message, offset, length, options);
        }

        if (recordTypeValue == 19 || recordTypeValue == 20 ||
            recordTypeValue == 27)
        {
            DnsDecodeResult result = SharedRecordDataCodec.DecodeCharacterStringsData(
                message, offset, length);
            return ValidateStringCount(result, recordTypeValue, offset);
        }

        if (recordTypeValue == 22)
        {
            return DecodeNsapData(message, offset, length);
        }

        if (recordTypeValue == 23)
        {
            return SharedRecordDataCodec.DecodeNameData(
                message, offset, length, options);
        }

        if (recordTypeValue == 26)
        {
            return DecodePxData(message, offset, length, options);
        }

        if (recordTypeValue == 38)
        {
            return DecodeA6Data(message, offset, length);
        }

        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of the legacy shared-schema codec family.",
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

        if (recordTypeValue == 17)
        {
            if (data.TwoName == null)
            {
                return MissingSchema(writer, "twoNames");
            }

            return SharedRecordDataCodec.EncodeTwoNameData(
                writer, table, data.TwoName, options, true);
        }

        if (recordTypeValue == 18 || recordTypeValue == 21 ||
            recordTypeValue == 36)
        {
            if (data.PreferenceName == null)
            {
                return MissingSchema(writer, "preferenceName");
            }

            return SharedRecordDataCodec.EncodePreferenceNameData(
                writer, table, data.PreferenceName, options, true);
        }

        if (recordTypeValue == 19 || recordTypeValue == 20 ||
            recordTypeValue == 27)
        {
            if (data.CharacterStrings == null)
            {
                return MissingSchema(writer, "characterStrings");
            }

            WireWriteResult validation = ValidateStringWriteCount(
                writer, data.CharacterStrings, recordTypeValue);
            if (!validation.Success)
            {
                return validation;
            }

            return SharedRecordDataCodec.EncodeCharacterStringsData(
                writer, data.CharacterStrings);
        }

        if (recordTypeValue == 22)
        {
            if (data.FixedBytes == null)
            {
                return MissingSchema(writer, "nsap");
            }

            return EncodeNsapData(writer, data.FixedBytes);
        }

        if (recordTypeValue == 23)
        {
            if (data.Name == null)
            {
                return MissingSchema(writer, "name");
            }

            return SharedRecordDataCodec.EncodeNameData(
                writer, table, data.Name, options, true);
        }

        if (recordTypeValue == 26)
        {
            if (data.Px == null)
            {
                return MissingSchema(writer, "px");
            }

            return EncodePxData(writer, table, data.Px, options);
        }

        if (recordTypeValue == 38)
        {
            if (data.A6 == null)
            {
                return MissingSchema(writer, "a6");
            }

            return EncodeA6Data(writer, table, data.A6, options);
        }

        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of the legacy shared-schema codec family.",
            writer.WriteOffset());
    }

    internal static DnsDecodeResult DecodeA6Data(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success)
        {
            return validation;
        }

        if (length == 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "a6.prefixLength",
                "An A6 record requires a prefix-length octet.",
                offset);
        }

        int prefixLength = message[offset];
        if (prefixLength > 128)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "a6.prefixLength",
                "An A6 prefix length cannot exceed 128 bits.",
                offset);
        }

        int suffixLength = (128 - prefixLength + 7) / 8;
        if (length < suffixLength + 1)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "a6.addressSuffix",
                "The A6 address suffix is truncated.",
                offset + 1);
        }

        DnsA6Data a6 = new DnsA6Data();
        a6.PrefixLength = prefixLength;
        a6.AddressSuffix = SharedRecordDataCodec.CopyRegion(
            message, offset + 1, suffixLength);
        int nameOffset = offset + 1 + suffixLength;
        int nameLength = length - 1 - suffixLength;
        if (prefixLength == 0)
        {
            if (nameLength != 0)
            {
                return SharedRecordDataCodec.DecodeFailure(
                    DnsDiagnosticCode.RecordDataLengthMismatch,
                    "a6.prefixName",
                    "An A6 record with prefix length zero cannot contain a prefix name.",
                    nameOffset);
            }
        }
        else
        {
            DnsDecodeResult name = SharedRecordDataCodec.DecodeUncompressedName(
                message, nameOffset, nameLength, "a6.prefixName");
            if (!name.Success || name.Name == null)
            {
                return name;
            }

            a6.PrefixName = name.Name;
        }

        DnsRecordData data = new DnsRecordData();
        data.A6 = a6;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeA6Data(
        WireWriter writer,
        CompressionTable table,
        DnsA6Data data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.AddressSuffix == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "a6", "Complete A6 data and encoding state are required.");
        }

        if (data.PrefixLength < 0 || data.PrefixLength > 128)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "a6.prefixLength",
                "An A6 prefix length must be between zero and 128 bits.",
                writer.WriteOffset());
        }

        int suffixLength = (128 - data.PrefixLength + 7) / 8;
        if (data.AddressSuffix.Length != suffixLength ||
            (data.PrefixLength == 0 && data.PrefixName != null) ||
            (data.PrefixLength != 0 && data.PrefixName == null))
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "a6",
                "The A6 suffix width or prefix-name presence is invalid.",
                writer.WriteOffset());
        }

        int suffixBitCount = 128 - data.PrefixLength;
        int paddingBitCount = suffixLength * 8 - suffixBitCount;
        if (paddingBitCount > 0 && data.AddressSuffix.Length > 0 &&
            (data.AddressSuffix[0] & (255 << (8 - paddingBitCount))) != 0)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "a6.addressSuffix",
                "Leading A6 address-suffix padding bits must be zero.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult prefixLength = writer.WriteUnsigned8(
            (ulong)data.PrefixLength);
        if (!prefixLength.Success) return prefixLength;
        WireWriteResult suffix = writer.WriteBytes(data.AddressSuffix);
        if (!suffix.Success) return suffix;
        if (data.PrefixName != null)
        {
            WireWriteResult prefixName = table.WriteName(
                writer, data.PrefixName, options, false);
            if (!prefixName.Success) return prefixName;
        }

        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeNsapData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success)
        {
            return validation;
        }

        if (length < 1 || length > 20)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength,
                "nsap",
                "An NSAP address must contain between one and twenty octets.",
                offset);
        }

        return SharedRecordDataCodec.DecodeFixedBytesData(
            message, offset, length, length);
    }

    internal static WireWriteResult EncodeNsapData(
        WireWriter writer,
        DnsFixedBytesData data)
    {
        if (writer == null || data == null || data.Bytes == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "nsap", "NSAP address octets are required.");
        }

        if (data.Bytes.Length < 1 || data.Bytes.Length > 20)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidLength,
                "nsap",
                "An NSAP address must contain between one and twenty octets.",
                writer.WriteOffset());
        }

        return SharedRecordDataCodec.EncodeFixedBytesData(
            writer, data, data.Bytes.Length);
    }

    internal static DnsDecodeResult DecodePxData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success)
        {
            return validation;
        }

        if (length < 4)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "px",
                "PX data requires a preference and two DNS names.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult preferenceResult = reader.ReadUnsigned16();
        if (!preferenceResult.Success)
        {
            return SharedRecordDataCodec.FromReadFailure(preferenceResult);
        }

        int map822Offset = reader.ReadOffset();
        DnsDecodeResult map822Result = CompressedDnsNameDecoder.DecodeName(
            message, map822Offset, options);
        if (!map822Result.Success || map822Result.Name == null)
        {
            return map822Result;
        }

        int mapX400Offset = map822Offset + map822Result.BytesConsumed;
        if (mapX400Offset >= offset + length)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "px.mapX400",
                "The PX X.400 mapping name is missing.",
                mapX400Offset);
        }

        DnsDecodeResult mapX400Result = CompressedDnsNameDecoder.DecodeName(
            message, mapX400Offset, options);
        if (!mapX400Result.Success || mapX400Result.Name == null)
        {
            return mapX400Result;
        }

        int consumed = 2 + map822Result.BytesConsumed + mapX400Result.BytesConsumed;
        if (consumed != length)
        {
            return SharedRecordDataCodec.LengthFailure(offset + consumed, "px");
        }

        DnsPxData px = new DnsPxData();
        px.Preference = (int)preferenceResult.UnsignedValue;
        px.Map822 = map822Result.Name;
        px.MapX400 = mapX400Result.Name;
        DnsRecordData data = new DnsRecordData();
        data.Px = px;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodePxData(
        WireWriter writer,
        CompressionTable table,
        DnsPxData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "px",
                "PX data and encoding state are required.",
                0);
        }

        if (data.Preference < 0 || data.Preference > 65535)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "px.preference",
                "A PX preference must fit in sixteen bits.",
                writer.WriteOffset());
        }

        int startOffset = writer.WriteOffset();
        WireWriteResult preference = writer.WriteUnsigned16(
            (ulong)data.Preference);
        if (!preference.Success)
        {
            return preference;
        }

        WireWriteResult map822 = table.WriteName(
            writer, data.Map822, options, true);
        if (!map822.Success)
        {
            return map822;
        }

        WireWriteResult mapX400 = table.WriteName(
            writer, data.MapX400, options, true);
        if (!mapX400.Success)
        {
            return mapX400;
        }

        return writer.Succeed(
            startOffset,
            writer.WriteOffset() - startOffset,
            -1);
    }

    internal static DnsDecodeResult ValidateStringCount(
        DnsDecodeResult result,
        int recordTypeValue,
        int offset)
    {
        if (!result.Success || result.RecordData == null ||
            result.RecordData.CharacterStrings == null)
        {
            return result;
        }

        int count = result.RecordData.CharacterStrings.Strings.Count;
        bool valid = false;
        if (recordTypeValue == 19 && count == 1)
        {
            valid = true;
        }
        else if (recordTypeValue == 20 && (count == 1 || count == 2))
        {
            valid = true;
        }
        else if (recordTypeValue == 27 && count == 3)
        {
            valid = true;
        }

        if (!valid)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                StringField(recordTypeValue),
                StringCountReason(recordTypeValue),
                offset);
        }

        return result;
    }

    internal static WireWriteResult ValidateStringWriteCount(
        WireWriter writer,
        DnsCharacterStringsData data,
        int recordTypeValue)
    {
        DnsDecodeResult validation = new DnsDecodeResult();
        validation.Success = true;
        DnsRecordData recordData = new DnsRecordData();
        recordData.CharacterStrings = data;
        validation.RecordData = recordData;
        DnsDecodeResult countResult = ValidateStringCount(
            validation, recordTypeValue, writer.WriteOffset());
        if (!countResult.Success)
        {
            DnsDiagnostic diagnostic = countResult.Diagnostics[0];
            return writer.FailAt(
                diagnostic.Code,
                diagnostic.Field == null ? "characterStrings" : diagnostic.Field,
                diagnostic.Reason,
                writer.WriteOffset());
        }

        WireWriteResult result = new WireWriteResult();
        result.Success = true;
        result.StartOffset = writer.WriteOffset();
        result.NextOffset = writer.WriteOffset();
        return result;
    }

    internal static string StringField(int recordTypeValue)
    {
        if (recordTypeValue == 19)
        {
            return "x25";
        }

        if (recordTypeValue == 20)
        {
            return "isdn";
        }

        return "gpos";
    }

    internal static string StringCountReason(int recordTypeValue)
    {
        if (recordTypeValue == 19)
        {
            return "X25 data must contain exactly one character string.";
        }

        if (recordTypeValue == 20)
        {
            return "ISDN data must contain one or two character strings.";
        }

        return "GPOS data must contain exactly three character strings.";
    }

    internal static WireWriteResult MissingSchema(WireWriter writer, string field)
    {
        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            field,
            "The record data does not contain its required typed schema.",
            writer.WriteOffset());
    }
}
