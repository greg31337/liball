using System;
using System.Collections.Generic;

namespace dns;

internal class IntegerNameReadResult
{
    internal bool Success;
    internal ulong IntegerValue;
    internal DnsName? Name;
    internal int BytesConsumed;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class SharedRecordDataCodec
{
    internal static DnsDecodeResult DecodeOpaqueData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = ValidateRegion(message.Length, offset, length);
        if (!validation.Success)
        {
            return validation;
        }

        DnsOpaqueData opaque = new DnsOpaqueData();
        opaque.Bytes = CopyRegion(message, offset, length);
        DnsRecordData data = new DnsRecordData();
        data.Opaque = opaque;
        return DecodeSuccess(data, length);
    }

    internal static DnsDecodeResult DecodeFixedBytesData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        int expectedLength)
    {
        DnsDecodeResult validation = ValidateFixedRegion(
            message.Length,
            offset,
            length,
            expectedLength,
            "fixedBytes");
        if (!validation.Success)
        {
            return validation;
        }

        DnsFixedBytesData fixedBytes = new DnsFixedBytesData();
        fixedBytes.Bytes = CopyRegion(message, offset, length);
        DnsRecordData data = new DnsRecordData();
        data.FixedBytes = fixedBytes;
        return DecodeSuccess(data, length);
    }

    internal static DnsDecodeResult DecodeAddressData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        int expectedLength)
    {
        if (expectedLength != 4 && expectedLength != 16)
        {
            return DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "address",
                "An address schema must contain four or sixteen octets.",
                offset);
        }

        DnsDecodeResult validation = ValidateFixedRegion(
            message.Length,
            offset,
            length,
            expectedLength,
            "address");
        if (!validation.Success)
        {
            return validation;
        }

        DnsAddressData address = new DnsAddressData();
        address.Address = CopyRegion(message, offset, length);
        DnsRecordData data = new DnsRecordData();
        data.Address = address;
        return DecodeSuccess(data, length);
    }

    internal static DnsDecodeResult DecodeNameData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        DnsDecodeResult validation = ValidateRegion(message.Length, offset, length);
        if (!validation.Success)
        {
            return validation;
        }

        DnsDecodeResult nameResult = CompressedDnsNameDecoder.DecodeName(
            message,
            offset,
            options);
        if (!nameResult.Success || nameResult.Name == null)
        {
            return nameResult;
        }

        if (nameResult.BytesConsumed != length)
        {
            return LengthFailure(offset + nameResult.BytesConsumed, "name");
        }

        DnsNameData name = new DnsNameData();
        name.Name = nameResult.Name;
        DnsRecordData data = new DnsRecordData();
        data.Name = name;
        return DecodeSuccess(data, length);
    }

    internal static DnsDecodeResult DecodeUncompressedName(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        string field)
    {
        DnsDecodeResult result = DecodeUncompressedNamePrefix(
            message, offset, length, field);
        if (!result.Success)
        {
            return result;
        }

        if (result.BytesConsumed != length)
        {
            return LengthFailure(offset + result.BytesConsumed, field);
        }

        return result;
    }

    internal static DnsDecodeResult DecodeUncompressedNamePrefix(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        string field)
    {
        DnsDecodeResult validation = ValidateRegion(message.Length, offset, length);
        if (!validation.Success)
        {
            return validation;
        }

        List<byte[]> labels = new List<byte[]>();
        int cursor = offset;
        int end = offset + length;
        while (cursor < end)
        {
            int labelLength = message[cursor];
            if ((labelLength & 192) != 0)
            {
                return DecodeFailure(
                    DnsDiagnosticCode.InvalidRecordData,
                    field,
                    "Name compression is not permitted in this field.",
                    cursor);
            }

            cursor = cursor + 1;
            if (labelLength == 0)
            {
                RawDnsNameResult raw = RawDnsName.CreateAbsoluteName(labels);
                if (!raw.Success || raw.Name == null)
                {
                    if (raw.Diagnostics.Count > 0)
                    {
                        DnsDiagnostic diagnostic = raw.Diagnostics[0];
                        return DecodeFailure(
                            diagnostic.Code,
                            field,
                            diagnostic.Reason,
                            cursor - 1);
                    }

                    return DecodeFailure(
                        DnsDiagnosticCode.InvalidLabel,
                        field,
                        "The uncompressed DNS name is invalid.",
                        cursor - 1);
                }

                DnsDecodeResult result = new DnsDecodeResult();
                result.Success = true;
                result.Name = raw.Name;
                result.BytesConsumed = cursor - offset;
                return result;
            }

            if (labelLength > DnsProtocolLimits.MaximumLabelLength ||
                labelLength > end - cursor)
            {
                return DecodeFailure(
                    labelLength > DnsProtocolLimits.MaximumLabelLength ?
                        DnsDiagnosticCode.InvalidLabel :
                        DnsDiagnosticCode.UnexpectedEnd,
                    field,
                    "The uncompressed DNS name contains an invalid or truncated label.",
                    cursor);
            }

            byte[] label = new byte[labelLength];
            message.Slice(cursor, labelLength).CopyTo(label);
            labels.Add(label);
            cursor = cursor + labelLength;
        }

        return DecodeFailure(
            DnsDiagnosticCode.UnexpectedEnd,
            field,
            "The uncompressed DNS name is missing its root label.",
            end);
    }

    internal static DnsDecodeResult DecodeTwoNameData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        DnsDecodeResult validation = ValidateRegion(message.Length, offset, length);
        if (!validation.Success)
        {
            return validation;
        }

        DnsDecodeResult firstResult = CompressedDnsNameDecoder.DecodeName(
            message,
            offset,
            options);
        if (!firstResult.Success || firstResult.Name == null)
        {
            return firstResult;
        }

        int secondOffset = offset + firstResult.BytesConsumed;
        if (secondOffset >= offset + length)
        {
            return DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "secondName",
                "A second DNS name is required.",
                secondOffset);
        }

        DnsDecodeResult secondResult = CompressedDnsNameDecoder.DecodeName(
            message,
            secondOffset,
            options);
        if (!secondResult.Success || secondResult.Name == null)
        {
            return secondResult;
        }

        if (firstResult.BytesConsumed + secondResult.BytesConsumed != length)
        {
            return LengthFailure(
                secondOffset + secondResult.BytesConsumed,
                "twoNames");
        }

        DnsTwoNameData twoName = new DnsTwoNameData();
        twoName.FirstName = firstResult.Name;
        twoName.SecondName = secondResult.Name;
        DnsRecordData data = new DnsRecordData();
        data.TwoName = twoName;
        return DecodeSuccess(data, length);
    }

    internal static DnsDecodeResult DecodePreferenceNameData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        IntegerNameReadResult shared = DecodeUnsigned16Name(
            message,
            offset,
            length,
            options);
        if (!shared.Success || shared.Name == null)
        {
            return FromIntegerNameFailure(shared);
        }

        DnsPreferenceNameData preferenceName = new DnsPreferenceNameData();
        preferenceName.Preference = (int)shared.IntegerValue;
        preferenceName.Name = shared.Name;
        DnsRecordData data = new DnsRecordData();
        data.PreferenceName = preferenceName;
        return DecodeSuccess(data, shared.BytesConsumed);
    }

    internal static DnsDecodeResult DecodeCharacterStringsData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = ValidateRegion(message.Length, offset, length);
        if (!validation.Success)
        {
            return validation;
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        DnsCharacterStringsData strings = new DnsCharacterStringsData();
        while (reader.RemainingLength() > 0)
        {
            WireReadResult stringResult = reader.ReadCharacterString();
            if (!stringResult.Success)
            {
                return FromReadFailure(stringResult);
            }

            strings.Strings.Add(stringResult.Bytes);
        }

        DnsRecordData data = new DnsRecordData();
        data.CharacterStrings = strings;
        return DecodeSuccess(data, length);
    }

    internal static IntegerNameReadResult DecodeUnsigned8Name(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        return DecodeIntegerName(message, offset, length, options, 1);
    }

    internal static IntegerNameReadResult DecodeUnsigned16Name(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        return DecodeIntegerName(message, offset, length, options, 2);
    }

    internal static IntegerNameReadResult DecodeUnsigned32Name(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        return DecodeIntegerName(message, offset, length, options, 4);
    }

    internal static WireWriteResult EncodeOpaqueData(
        WireWriter writer,
        DnsOpaqueData data)
    {
        if (writer == null || data == null || data.Bytes == null)
        {
            return WriteFailure(writer, "opaque", "Opaque byte data is required.");
        }

        return writer.WriteBytes(data.Bytes);
    }

    internal static WireWriteResult EncodeFixedBytesData(
        WireWriter writer,
        DnsFixedBytesData data,
        int expectedLength)
    {
        if (writer == null || data == null || data.Bytes == null)
        {
            return WriteFailure(writer, "fixedBytes", "Fixed byte data is required.");
        }

        if (expectedLength < 0 || data.Bytes.Length != expectedLength)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidLength,
                "fixedBytes",
                "The fixed byte data does not have its schema-defined length.",
                writer.WriteOffset());
        }

        return writer.WriteBytes(data.Bytes);
    }

    internal static WireWriteResult EncodeAddressData(
        WireWriter writer,
        DnsAddressData data,
        int expectedLength)
    {
        if (writer == null || data == null || data.Address == null)
        {
            return WriteFailure(writer, "address", "Address octets are required.");
        }

        if ((expectedLength != 4 && expectedLength != 16) ||
            data.Address.Length != expectedLength)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidLength,
                "address",
                "The address does not have the schema-defined length.",
                writer.WriteOffset());
        }

        return writer.WriteBytes(data.Address);
    }

    internal static WireWriteResult EncodeNameData(
        WireWriter writer,
        CompressionTable table,
        DnsNameData data,
        DnsEncodeOptions options,
        bool compressionPermitted)
    {
        if (writer == null || table == null || data == null || options == null)
        {
            return WriteFailure(writer, "name", "Name data and encoding state are required.");
        }

        return table.WriteName(
            writer,
            data.Name,
            options,
            compressionPermitted);
    }

    internal static WireWriteResult EncodeTwoNameData(
        WireWriter writer,
        CompressionTable table,
        DnsTwoNameData data,
        DnsEncodeOptions options,
        bool compressionPermitted)
    {
        if (writer == null || table == null || data == null || options == null)
        {
            return WriteFailure(
                writer,
                "twoNames",
                "Two-name data and encoding state are required.");
        }

        int startOffset = writer.WriteOffset();
        WireWriteResult firstResult = table.WriteName(
            writer,
            data.FirstName,
            options,
            compressionPermitted);
        if (!firstResult.Success)
        {
            return firstResult;
        }

        WireWriteResult secondResult = table.WriteName(
            writer,
            data.SecondName,
            options,
            compressionPermitted);
        if (!secondResult.Success)
        {
            return secondResult;
        }

        return writer.Succeed(
            startOffset,
            writer.WriteOffset() - startOffset,
            -1);
    }

    internal static WireWriteResult EncodePreferenceNameData(
        WireWriter writer,
        CompressionTable table,
        DnsPreferenceNameData data,
        DnsEncodeOptions options,
        bool compressionPermitted)
    {
        if (data == null)
        {
            return WriteFailure(
                writer,
                "preferenceName",
                "Preference-and-name data is required.");
        }

        return EncodeUnsigned16Name(
            writer,
            table,
            data.Preference,
            data.Name,
            options,
            compressionPermitted);
    }

    internal static WireWriteResult EncodeCharacterStringsData(
        WireWriter writer,
        DnsCharacterStringsData data)
    {
        if (writer == null || data == null || data.Strings == null)
        {
            return WriteFailure(
                writer,
                "characterStrings",
                "Character-string data is required.");
        }

        int startOffset = writer.WriteOffset();
        int index = 0;
        while (index < data.Strings.Count)
        {
            WireWriteResult stringResult = writer.WriteCharacterString(
                data.Strings[index]);
            if (!stringResult.Success)
            {
                return stringResult;
            }

            index = index + 1;
        }

        return writer.Succeed(
            startOffset,
            writer.WriteOffset() - startOffset,
            -1);
    }

    internal static WireWriteResult EncodeUnsigned8Name(
        WireWriter writer,
        CompressionTable table,
        int value,
        DnsName name,
        DnsEncodeOptions options,
        bool compressionPermitted)
    {
        return EncodeIntegerName(
            writer,
            table,
            (ulong)value,
            name,
            options,
            compressionPermitted,
            1);
    }

    internal static WireWriteResult EncodeUnsigned16Name(
        WireWriter writer,
        CompressionTable table,
        int value,
        DnsName name,
        DnsEncodeOptions options,
        bool compressionPermitted)
    {
        return EncodeIntegerName(
            writer,
            table,
            (ulong)value,
            name,
            options,
            compressionPermitted,
            2);
    }

    internal static WireWriteResult EncodeUnsigned32Name(
        WireWriter writer,
        CompressionTable table,
        uint value,
        DnsName name,
        DnsEncodeOptions options,
        bool compressionPermitted)
    {
        return EncodeIntegerName(
            writer,
            table,
            value,
            name,
            options,
            compressionPermitted,
            4);
    }

    internal static IntegerNameReadResult DecodeIntegerName(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options,
        int width)
    {
        DnsDecodeResult validation = ValidateRegion(message.Length, offset, length);
        if (!validation.Success)
        {
            return IntegerNameFailure(validation.Diagnostics);
        }

        if (length <= width)
        {
            return IntegerNameDiagnostic(
                DnsDiagnosticCode.UnexpectedEnd,
                "integerName",
                "An integer-and-name value is truncated.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult integerResult = new WireReadResult();
        if (width == 1)
        {
            integerResult = reader.ReadUnsigned8();
        }
        else if (width == 2)
        {
            integerResult = reader.ReadUnsigned16();
        }
        else
        {
            integerResult = reader.ReadUnsigned32();
        }

        if (!integerResult.Success)
        {
            return IntegerNameFailure(integerResult.Diagnostics);
        }

        DnsDecodeResult nameResult = CompressedDnsNameDecoder.DecodeName(
            message,
            reader.ReadOffset(),
            options);
        if (!nameResult.Success || nameResult.Name == null)
        {
            return IntegerNameFailure(nameResult.Diagnostics);
        }

        if (width + nameResult.BytesConsumed != length)
        {
            return IntegerNameDiagnostic(
                DnsDiagnosticCode.RecordDataLengthMismatch,
                "integerName",
                "Trailing data follows the integer-and-name value.",
                offset + width + nameResult.BytesConsumed);
        }

        IntegerNameReadResult result = new IntegerNameReadResult();
        result.Success = true;
        result.IntegerValue = integerResult.UnsignedValue;
        result.Name = nameResult.Name;
        result.BytesConsumed = length;
        return result;
    }

    internal static WireWriteResult EncodeIntegerName(
        WireWriter writer,
        CompressionTable table,
        ulong value,
        DnsName name,
        DnsEncodeOptions options,
        bool compressionPermitted,
        int width)
    {
        if (writer == null || table == null || name == null || options == null)
        {
            return WriteFailure(
                writer,
                "integerName",
                "An integer, name, and encoding state are required.");
        }

        int startOffset = writer.WriteOffset();
        WireWriteResult integerResult = new WireWriteResult();
        if (width == 1)
        {
            integerResult = writer.WriteUnsigned8(value);
        }
        else if (width == 2)
        {
            integerResult = writer.WriteUnsigned16(value);
        }
        else
        {
            integerResult = writer.WriteUnsigned32(value);
        }

        if (!integerResult.Success)
        {
            return integerResult;
        }

        WireWriteResult nameResult = table.WriteName(
            writer,
            name,
            options,
            compressionPermitted);
        if (!nameResult.Success)
        {
            return nameResult;
        }

        return writer.Succeed(
            startOffset,
            writer.WriteOffset() - startOffset,
            -1);
    }

    internal static DnsDecodeResult ValidateRegion(
        int messageLength,
        int offset,
        int length)
    {
        if (offset < 0 || length < 0 || offset > messageLength - length)
        {
            return DecodeFailure(
                DnsDiagnosticCode.InvalidLength,
                "recordData",
                "The resource-data region is outside the containing message.",
                offset);
        }

        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        return result;
    }

    internal static DnsDecodeResult ValidateFixedRegion(
        int messageLength,
        int offset,
        int length,
        int expectedLength,
        string field)
    {
        DnsDecodeResult validation = ValidateRegion(messageLength, offset, length);
        if (!validation.Success)
        {
            return validation;
        }

        if (expectedLength < 0 || length != expectedLength)
        {
            return DecodeFailure(
                DnsDiagnosticCode.InvalidLength,
                field,
                "The resource data does not have its schema-defined length.",
                offset);
        }

        return validation;
    }

    internal static byte[] CopyRegion(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        byte[] result = new byte[length];
        message.Slice(offset, length).CopyTo(result);
        return result;
    }

    internal static DnsDecodeResult DecodeSuccess(DnsRecordData data, int length)
    {
        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        result.RecordData = data;
        result.BytesConsumed = length;
        return result;
    }

    internal static DnsDecodeResult DecodeFailure(
        DnsDiagnosticCode code,
        string field,
        string reason,
        int offset)
    {
        return DnsHeaderCodec.DecodeFailure(code, field, reason, offset);
    }

    internal static DnsDecodeResult LengthFailure(int offset, string field)
    {
        return DecodeFailure(
            DnsDiagnosticCode.RecordDataLengthMismatch,
            field,
            "The decoded value does not consume the declared resource-data length.",
            offset);
    }

    internal static DnsDecodeResult FromReadFailure(WireReadResult readResult)
    {
        DnsDecodeResult result = new DnsDecodeResult();
        int index = 0;
        while (index < readResult.Diagnostics.Count)
        {
            result.Diagnostics.Add(readResult.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    internal static DnsDecodeResult FromIntegerNameFailure(
        IntegerNameReadResult shared)
    {
        DnsDecodeResult result = new DnsDecodeResult();
        int index = 0;
        while (index < shared.Diagnostics.Count)
        {
            result.Diagnostics.Add(shared.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    internal static IntegerNameReadResult IntegerNameFailure(
        List<DnsDiagnostic> diagnostics)
    {
        IntegerNameReadResult result = new IntegerNameReadResult();
        int index = 0;
        while (index < diagnostics.Count)
        {
            result.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    internal static IntegerNameReadResult IntegerNameDiagnostic(
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
        IntegerNameReadResult result = new IntegerNameReadResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    internal static WireWriteResult WriteFailure(
        WireWriter? writer,
        string field,
        string reason)
    {
        if (writer == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.ValueOutOfRange,
                field,
                reason,
                0);
        }

        return writer.FailAt(
            DnsDiagnosticCode.ValueOutOfRange,
            field,
            reason,
            writer.WriteOffset());
    }
}
