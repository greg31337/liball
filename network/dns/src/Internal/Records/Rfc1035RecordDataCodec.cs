using System;

namespace dns;

internal static class Rfc1035RecordDataCodec
{
    internal static DnsDecodeResult DecodeRecordData(
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        if (recordTypeValue == 1)
        {
            return SharedRecordDataCodec.DecodeAddressData(
                message, offset, length, 4);
        }

        if (IsNameType(recordTypeValue))
        {
            return SharedRecordDataCodec.DecodeNameData(
                message, offset, length, options);
        }

        if (recordTypeValue == 6)
        {
            return DecodeSoaData(message, offset, length, options);
        }

        if (recordTypeValue == 10)
        {
            return SharedRecordDataCodec.DecodeOpaqueData(message, offset, length);
        }

        if (recordTypeValue == 11)
        {
            return DecodeWksData(message, offset, length);
        }

        if (recordTypeValue == 13 || recordTypeValue == 16)
        {
            DnsDecodeResult result = SharedRecordDataCodec.DecodeCharacterStringsData(
                message, offset, length);
            return ValidateCharacterStringCount(result, recordTypeValue, offset);
        }

        if (recordTypeValue == 14)
        {
            return SharedRecordDataCodec.DecodeTwoNameData(
                message, offset, length, options);
        }

        if (recordTypeValue == 15)
        {
            return SharedRecordDataCodec.DecodePreferenceNameData(
                message, offset, length, options);
        }

        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of the RFC 1035 codec family.",
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

        if (recordTypeValue == 1)
        {
            if (data.Address == null)
            {
                return MissingSchema(writer, "address");
            }

            return SharedRecordDataCodec.EncodeAddressData(writer, data.Address, 4);
        }

        if (IsNameType(recordTypeValue))
        {
            if (data.Name == null)
            {
                return MissingSchema(writer, "name");
            }

            return SharedRecordDataCodec.EncodeNameData(
                writer, table, data.Name, options, true);
        }

        if (recordTypeValue == 6)
        {
            if (data.Soa == null)
            {
                return MissingSchema(writer, "soa");
            }

            return EncodeSoaData(writer, table, data.Soa, options);
        }

        if (recordTypeValue == 10)
        {
            if (data.Opaque == null)
            {
                return MissingSchema(writer, "opaque");
            }

            return SharedRecordDataCodec.EncodeOpaqueData(writer, data.Opaque);
        }

        if (recordTypeValue == 11)
        {
            if (data.Wks == null)
            {
                return MissingSchema(writer, "wks");
            }

            return EncodeWksData(writer, data.Wks);
        }

        if (recordTypeValue == 13 || recordTypeValue == 16)
        {
            if (data.CharacterStrings == null)
            {
                return MissingSchema(writer, "characterStrings");
            }

            WireWriteResult countValidation = ValidateCharacterStringWriteCount(
                writer, data.CharacterStrings, recordTypeValue);
            if (!countValidation.Success)
            {
                return countValidation;
            }

            return SharedRecordDataCodec.EncodeCharacterStringsData(
                writer, data.CharacterStrings);
        }

        if (recordTypeValue == 14)
        {
            if (data.TwoName == null)
            {
                return MissingSchema(writer, "twoNames");
            }

            return SharedRecordDataCodec.EncodeTwoNameData(
                writer, table, data.TwoName, options, true);
        }

        if (recordTypeValue == 15)
        {
            if (data.PreferenceName == null)
            {
                return MissingSchema(writer, "preferenceName");
            }

            return SharedRecordDataCodec.EncodePreferenceNameData(
                writer, table, data.PreferenceName, options, true);
        }

        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of the RFC 1035 codec family.",
            writer.WriteOffset());
    }

    internal static DnsDecodeResult DecodeSoaData(
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

        DnsDecodeResult primaryResult = CompressedDnsNameDecoder.DecodeName(
            message, offset, options);
        if (!primaryResult.Success || primaryResult.Name == null)
        {
            return primaryResult;
        }

        int mailboxOffset = offset + primaryResult.BytesConsumed;
        if (mailboxOffset >= offset + length)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "soa.responsibleMailbox",
                "The SOA responsible-mailbox name is missing.",
                mailboxOffset);
        }

        DnsDecodeResult mailboxResult = CompressedDnsNameDecoder.DecodeName(
            message, mailboxOffset, options);
        if (!mailboxResult.Success || mailboxResult.Name == null)
        {
            return mailboxResult;
        }

        int integerOffset = mailboxOffset + mailboxResult.BytesConsumed;
        int integerLength = offset + length - integerOffset;
        if (integerLength < 20)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "soa.integers",
                "The five SOA integer fields are truncated.",
                integerOffset + integerLength);
        }

        if (integerLength > 20)
        {
            return SharedRecordDataCodec.LengthFailure(integerOffset + 20, "soa");
        }

        WireReader reader = new WireReader(message.ToArray(), integerOffset, 20);
        WireReadResult serial = reader.ReadUnsigned32();
        WireReadResult refresh = reader.ReadUnsigned32();
        WireReadResult retry = reader.ReadUnsigned32();
        WireReadResult expire = reader.ReadUnsigned32();
        WireReadResult minimum = reader.ReadUnsigned32();
        if (!serial.Success || !refresh.Success || !retry.Success ||
            !expire.Success || !minimum.Success)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "soa.integers",
                "The five SOA integer fields are truncated.",
                reader.ReadOffset());
        }

        DnsSoaData soa = new DnsSoaData();
        soa.PrimaryNameServer = primaryResult.Name;
        soa.ResponsibleMailbox = mailboxResult.Name;
        soa.Serial = (uint)serial.UnsignedValue;
        soa.Refresh = (uint)refresh.UnsignedValue;
        soa.Retry = (uint)retry.UnsignedValue;
        soa.Expire = (uint)expire.UnsignedValue;
        soa.Minimum = (uint)minimum.UnsignedValue;
        DnsRecordData data = new DnsRecordData();
        data.Soa = soa;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeSoaData(
        WireWriter writer,
        CompressionTable table,
        DnsSoaData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "soa",
                "SOA data and encoding state are required.",
                0);
        }

        int startOffset = writer.WriteOffset();
        WireWriteResult primary = table.WriteName(
            writer, data.PrimaryNameServer, options, true);
        if (!primary.Success)
        {
            return primary;
        }

        WireWriteResult mailbox = table.WriteName(
            writer, data.ResponsibleMailbox, options, true);
        if (!mailbox.Success)
        {
            return mailbox;
        }

        WireWriteResult serial = writer.WriteUnsigned32(data.Serial);
        if (!serial.Success)
        {
            return serial;
        }

        WireWriteResult refresh = writer.WriteUnsigned32(data.Refresh);
        if (!refresh.Success)
        {
            return refresh;
        }

        WireWriteResult retry = writer.WriteUnsigned32(data.Retry);
        if (!retry.Success)
        {
            return retry;
        }

        WireWriteResult expire = writer.WriteUnsigned32(data.Expire);
        if (!expire.Success)
        {
            return expire;
        }

        WireWriteResult minimum = writer.WriteUnsigned32(data.Minimum);
        if (!minimum.Success)
        {
            return minimum;
        }

        return writer.Succeed(
            startOffset,
            writer.WriteOffset() - startOffset,
            -1);
    }

    internal static DnsDecodeResult DecodeWksData(
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

        if (length < 5)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "wks",
                "WKS data requires an IPv4 address and protocol octet.",
                offset + length);
        }

        DnsWksData wks = new DnsWksData();
        wks.Address = SharedRecordDataCodec.CopyRegion(message, offset, 4);
        wks.Protocol = message[offset + 4];
        wks.PortBitmap = SharedRecordDataCodec.CopyRegion(
            message, offset + 5, length - 5);
        DnsRecordData data = new DnsRecordData();
        data.Wks = wks;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeWksData(
        WireWriter writer,
        DnsWksData data)
    {
        if (writer == null || data == null || data.Address == null ||
            data.PortBitmap == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "wks", "Complete WKS data is required.");
        }

        if (data.Address.Length != 4 || data.Protocol < 0 || data.Protocol > 255)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "wks",
                "WKS requires a four-octet address and an eight-bit protocol.",
                writer.WriteOffset());
        }

        int startOffset = writer.WriteOffset();
        WireWriteResult address = writer.WriteIpv4Address(data.Address);
        if (!address.Success)
        {
            return address;
        }

        WireWriteResult protocol = writer.WriteUnsigned8((ulong)data.Protocol);
        if (!protocol.Success)
        {
            return protocol;
        }

        WireWriteResult bitmap = writer.WriteBytes(data.PortBitmap);
        if (!bitmap.Success)
        {
            return bitmap;
        }

        return writer.Succeed(
            startOffset,
            writer.WriteOffset() - startOffset,
            -1);
    }

    internal static DnsDecodeResult ValidateCharacterStringCount(
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
        if (recordTypeValue == 13 && count != 2)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "hinfo",
                "HINFO data must contain exactly two character strings.",
                offset);
        }

        if (recordTypeValue == 16 && count == 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "txt",
                "TXT data must contain at least one character string.",
                offset);
        }

        return result;
    }

    internal static WireWriteResult ValidateCharacterStringWriteCount(
        WireWriter writer,
        DnsCharacterStringsData data,
        int recordTypeValue)
    {
        int count = data.Strings.Count;
        if (recordTypeValue == 13 && count != 2)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "hinfo",
                "HINFO data must contain exactly two character strings.",
                writer.WriteOffset());
        }

        if (recordTypeValue == 16 && count == 0)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "txt",
                "TXT data must contain at least one character string.",
                writer.WriteOffset());
        }

        WireWriteResult result = new WireWriteResult();
        result.Success = true;
        result.StartOffset = writer.WriteOffset();
        result.NextOffset = writer.WriteOffset();
        return result;
    }

    internal static bool IsNameType(int recordTypeValue)
    {
        switch (recordTypeValue)
        {
            case 2:
            case 3:
            case 4:
            case 5:
            case 7:
            case 8:
            case 9:
            case 12:
                return true;
            default:
                return false;
        }
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
