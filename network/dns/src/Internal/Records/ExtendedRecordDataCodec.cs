using System;

namespace dns;

internal static class ExtendedRecordDataCodec
{
    internal static DnsDecodeResult DecodeRecordData(
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        if (recordTypeValue == 63)
        {
            return DecodeZonemdData(message, offset, length);
        }

        if (recordTypeValue == 64 || recordTypeValue == 65)
        {
            return SvcbRecordDataCodec.DecodeRecordData(
                message, offset, length);
        }

        if (recordTypeValue == 66)
        {
            return DecodeDsyncData(message, offset, length);
        }

        if (recordTypeValue == 67 || recordTypeValue == 68)
        {
            return DecodeCborData(message, offset, length, recordTypeValue);
        }

        if (recordTypeValue == 104 || recordTypeValue == 105 ||
            recordTypeValue == 106)
        {
            return DecodeIlNpData(message, offset, length, recordTypeValue);
        }

        if (recordTypeValue == 107)
        {
            return DecodeLpData(message, offset, length);
        }

        if (recordTypeValue == 128)
        {
            return MetaTypeFailure(offset);
        }

        if (recordTypeValue == 249 || recordTypeValue == 250)
        {
            return TransactionRecordDataCodec.DecodeRecordData(
                recordTypeValue, message, offset, length);
        }

        if (recordTypeValue == 260)
        {
            return DecodeAmtRelayData(message, offset, length);
        }

        if (recordTypeValue == 261)
        {
            return DecodeResolverInformationData(message, offset, length);
        }

        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of this extended record codec family.",
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

        if (recordTypeValue == 63)
        {
            if (data.Zonemd == null) return MissingSchema(writer, "zonemd");
            return EncodeZonemdData(writer, data.Zonemd);
        }

        if (recordTypeValue == 64 || recordTypeValue == 65)
        {
            if (data.Svcb == null) return MissingSchema(writer, "svcb");
            return SvcbRecordDataCodec.EncodeRecordData(
                writer, table, data.Svcb, options);
        }

        if (recordTypeValue == 66)
        {
            if (data.Dsync == null) return MissingSchema(writer, "dsync");
            return EncodeDsyncData(writer, table, data.Dsync, options);
        }

        if (recordTypeValue == 67 || recordTypeValue == 68)
        {
            if (data.Opaque == null) return MissingSchema(writer, "opaque");
            return EncodeCborData(writer, data.Opaque, recordTypeValue);
        }

        if (recordTypeValue == 104 || recordTypeValue == 105 ||
            recordTypeValue == 106)
        {
            if (data.IlNp == null) return MissingSchema(writer, "ilNp");
            return EncodeIlNpData(writer, data.IlNp, recordTypeValue);
        }

        if (recordTypeValue == 107)
        {
            if (data.PreferenceName == null)
                return MissingSchema(writer, "preferenceName");
            return EncodeLpData(writer, table, data.PreferenceName, options);
        }

        if (recordTypeValue == 128)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "nxname",
                "NXNAME is a bitmap-only meta type and has no record data.",
                writer.WriteOffset());
        }

        if (recordTypeValue == 249 || recordTypeValue == 250)
        {
            return TransactionRecordDataCodec.EncodeRecordData(
                writer, table, recordTypeValue, data, options);
        }

        if (recordTypeValue == 260)
        {
            if (data.AmtRelay == null) return MissingSchema(writer, "amtRelay");
            return EncodeAmtRelayData(writer, table, data.AmtRelay, options);
        }

        if (recordTypeValue == 261)
        {
            if (data.ResolverInformation == null)
                return MissingSchema(writer, "resolverInformation");
            return EncodeResolverInformationData(writer, data.ResolverInformation);
        }

        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of this extended record codec family.",
            writer.WriteOffset());
    }

    internal static DnsDecodeResult DecodeIlNpData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        int recordTypeValue)
    {
        int valueLength = recordTypeValue == 105 ? 4 : 8;
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateFixedRegion(
            message.Length, offset, length, valueLength + 2, "ilNp");
        if (!validation.Success) return validation;
        DnsIlNpData ilNp = new DnsIlNpData();
        ilNp.Preference = (message[offset] << 8) | message[offset + 1];
        ilNp.Value = SharedRecordDataCodec.CopyRegion(
            message, offset + 2, valueLength);
        DnsRecordData data = new DnsRecordData();
        data.IlNp = ilNp;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeIlNpData(
        WireWriter writer,
        DnsIlNpData data,
        int recordTypeValue)
    {
        if (writer == null || data == null || data.Value == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "ilNp", "Complete ILNP data is required.");
        }

        int valueLength = recordTypeValue == 105 ? 4 : 8;
        if (!Unsigned16IsValid(data.Preference) ||
            data.Value.Length != valueLength)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidLength,
                "ilNp",
                "The ILNP preference or value width is invalid.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult preference = writer.WriteUnsigned16(
            (ulong)data.Preference);
        if (!preference.Success) return preference;
        WireWriteResult value = writer.WriteBytes(data.Value);
        if (!value.Success) return value;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeLpData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 3)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "lp",
                "LP data requires a preference and target name.",
                offset + length);
        }

        DnsDecodeResult target = SharedRecordDataCodec.DecodeUncompressedName(
            message, offset + 2, length - 2, "lp.target");
        if (!target.Success || target.Name == null) return target;
        DnsPreferenceNameData preferenceName = new DnsPreferenceNameData();
        preferenceName.Preference = (message[offset] << 8) | message[offset + 1];
        preferenceName.Name = target.Name;
        DnsRecordData data = new DnsRecordData();
        data.PreferenceName = preferenceName;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeLpData(
        WireWriter writer,
        CompressionTable table,
        DnsPreferenceNameData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "lp", "Complete LP data and encoding state are required.");
        }

        if (!Unsigned16IsValid(data.Preference))
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "lp.preference",
                "An LP preference must fit in sixteen bits.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult preference = writer.WriteUnsigned16(
            (ulong)data.Preference);
        if (!preference.Success) return preference;
        WireWriteResult target = table.WriteName(
            writer, data.Name, options, false);
        if (!target.Success) return target;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeAmtRelayData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 2)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "amtRelay",
                "AMTRELAY data requires precedence and relay type fields.",
                offset + length);
        }

        int precedence = message[offset];
        int typeFlags = message[offset + 1];
        int relayType = typeFlags & 127;
        int relayOffset = offset + 2;
        int relayLength = length - 2;
        byte[] relayAddress = [];
        DnsName? relayName = null;
        if (relayType == 0)
        {
            if (relayLength != 0)
            {
                return AmtRelayLengthFailure(relayOffset);
            }
        }
        else if (relayType == 1 || relayType == 2)
        {
            int expectedLength = relayType == 1 ? 4 : 16;
            if (relayLength != expectedLength)
            {
                return AmtRelayLengthFailure(relayOffset);
            }

            relayAddress = SharedRecordDataCodec.CopyRegion(
                message, relayOffset, relayLength);
        }
        else if (relayType == 3)
        {
            DnsDecodeResult name = SharedRecordDataCodec.DecodeUncompressedName(
                message, relayOffset, relayLength, "amtRelay.relayName");
            if (!name.Success || name.Name == null) return name;
            relayName = name.Name;
        }
        else
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "amtRelay.relayType",
                "The AMTRELAY relay type is not defined.",
                offset + 1);
        }

        DnsAmtRelayData relay = new DnsAmtRelayData();
        relay.Precedence = precedence;
        relay.DiscoveryOptional = (typeFlags & 128) != 0;
        relay.RelayType = relayType;
        relay.RelayAddress = relayAddress;
        relay.RelayName = relayName;
        DnsRecordData data = new DnsRecordData();
        data.AmtRelay = relay;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeAmtRelayData(
        WireWriter writer,
        CompressionTable table,
        DnsAmtRelayData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.RelayAddress == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "amtRelay", "Complete AMTRELAY data is required.");
        }

        bool relayValid = data.RelayType == 0 &&
            data.RelayAddress.Length == 0 && data.RelayName == null;
        relayValid = relayValid || data.RelayType == 1 &&
            data.RelayAddress.Length == 4 && data.RelayName == null;
        relayValid = relayValid || data.RelayType == 2 &&
            data.RelayAddress.Length == 16 && data.RelayName == null;
        relayValid = relayValid || data.RelayType == 3 &&
            data.RelayAddress.Length == 0 && data.RelayName != null;
        if (!Unsigned8IsValid(data.Precedence) || !relayValid)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "amtRelay",
                "AMTRELAY relay fields are inconsistent.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult precedence = writer.WriteUnsigned8((ulong)data.Precedence);
        if (!precedence.Success) return precedence;
        int typeFlags = data.RelayType;
        if (data.DiscoveryOptional) typeFlags = typeFlags | 128;
        WireWriteResult type = writer.WriteUnsigned8((ulong)typeFlags);
        if (!type.Success) return type;
        if (data.RelayType == 1 || data.RelayType == 2)
        {
            WireWriteResult address = writer.WriteBytes(data.RelayAddress);
            if (!address.Success) return address;
        }
        else if (data.RelayType == 3 && data.RelayName != null)
        {
            WireWriteResult name = table.WriteName(
                writer, data.RelayName, options, false);
            if (!name.Success) return name;
        }

        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeZonemdData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 18)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength,
                "zonemd",
                "ZONEMD data requires fixed fields and at least twelve digest octets.",
                offset);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult serial = reader.ReadUnsigned32();
        WireReadResult scheme = reader.ReadUnsigned8();
        WireReadResult hashAlgorithm = reader.ReadUnsigned8();
        WireReadResult digest = reader.ReadBytes(reader.RemainingLength());
        if (!ZonemdDigestLengthIsValid(
            (int)hashAlgorithm.UnsignedValue, digest.Bytes.Length))
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength,
                "zonemd.digest",
                "The ZONEMD digest length does not match its hash algorithm.",
                offset + 6);
        }

        DnsZonemdData zonemd = new DnsZonemdData();
        zonemd.Serial = (uint)serial.UnsignedValue;
        zonemd.Scheme = (int)scheme.UnsignedValue;
        zonemd.HashAlgorithm = (int)hashAlgorithm.UnsignedValue;
        zonemd.Digest = digest.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Zonemd = zonemd;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeZonemdData(
        WireWriter writer,
        DnsZonemdData data)
    {
        if (writer == null || data == null || data.Digest == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "zonemd", "Complete ZONEMD data is required.");
        }

        if (!Unsigned8IsValid(data.Scheme) ||
            !Unsigned8IsValid(data.HashAlgorithm) ||
            !ZonemdDigestLengthIsValid(data.HashAlgorithm, data.Digest.Length))
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidLength,
                "zonemd",
                "A ZONEMD field is out of range or has the wrong length.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult serial = writer.WriteUnsigned32(data.Serial);
        if (!serial.Success) return serial;
        WireWriteResult scheme = writer.WriteUnsigned8((ulong)data.Scheme);
        if (!scheme.Success) return scheme;
        WireWriteResult hashAlgorithm = writer.WriteUnsigned8(
            (ulong)data.HashAlgorithm);
        if (!hashAlgorithm.Success) return hashAlgorithm;
        WireWriteResult digest = writer.WriteBytes(data.Digest);
        if (!digest.Success) return digest;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeResolverInformationData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length == 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength,
                "resolverInformation",
                "RESINFO requires at least one key or key-value string.",
                offset);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        DnsResolverInformationData resolverInformation =
            new DnsResolverInformationData();
        while (reader.RemainingLength() > 0)
        {
            WireReadResult value = reader.ReadCharacterString();
            if (!value.Success) return SharedRecordDataCodec.FromReadFailure(value);
            resolverInformation.Values.Add(value.Bytes);
        }

        if (!ResolverValuesAreValid(resolverInformation))
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "resolverInformation",
                "RESINFO keys must be nonempty printable ASCII.",
                offset);
        }

        DnsRecordData data = new DnsRecordData();
        data.ResolverInformation = resolverInformation;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeResolverInformationData(
        WireWriter writer,
        DnsResolverInformationData data)
    {
        if (writer == null || data == null || data.Values == null ||
            data.Values.Count == 0 || !ResolverValuesAreValid(data))
        {
            return SharedRecordDataCodec.WriteFailure(
                writer,
                "resolverInformation",
                "RESINFO requires valid key or key-value strings.");
        }

        int start = writer.WriteOffset();
        int index = 0;
        while (index < data.Values.Count)
        {
            WireWriteResult value = writer.WriteCharacterString(data.Values[index]);
            if (!value.Success) return value;
            index = index + 1;
        }

        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeDsyncData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 6)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "dsync",
                "DSYNC data requires type, scheme, port, and target fields.",
                offset + length);
        }

        int recordType = (message[offset] << 8) | message[offset + 1];
        int scheme = message[offset + 2];
        int port = (message[offset + 3] << 8) | message[offset + 4];
        DnsDecodeResult target = SharedRecordDataCodec.DecodeUncompressedName(
            message, offset + 5, length - 5, "dsync.target");
        if (!target.Success || target.Name == null) return target;
        DnsDsyncData dsync = new DnsDsyncData();
        dsync.RecordType = (DnsRecordType)recordType;
        dsync.RecordTypeValue = recordType;
        dsync.Scheme = scheme;
        dsync.Port = port;
        dsync.Target = target.Name;
        DnsRecordData data = new DnsRecordData();
        data.Dsync = dsync;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeDsyncData(
        WireWriter writer,
        CompressionTable table,
        DnsDsyncData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "dsync", "Complete DSYNC data and encoding state are required.");
        }

        int recordType = DnsHeaderCodec.ResolveCode(
            data.RecordTypeValue, (int)data.RecordType);
        if (!Unsigned16IsValid(recordType) || !Unsigned8IsValid(data.Scheme) ||
            !Unsigned16IsValid(data.Port))
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "dsync",
                "A DSYNC numeric field exceeds its wire width.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult type = writer.WriteUnsigned16((ulong)recordType);
        if (!type.Success) return type;
        WireWriteResult scheme = writer.WriteUnsigned8((ulong)data.Scheme);
        if (!scheme.Success) return scheme;
        WireWriteResult port = writer.WriteUnsigned16((ulong)data.Port);
        if (!port.Success) return port;
        WireWriteResult target = table.WriteName(
            writer, data.Target, options, false);
        if (!target.Success) return target;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeCborData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        int recordTypeValue)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length == 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength,
                recordTypeValue == 67 ? "hhit" : "brid",
                "A CBOR record payload must be nonempty.",
                offset);
        }

        return SharedRecordDataCodec.DecodeOpaqueData(
            message, offset, length);
    }

    internal static WireWriteResult EncodeCborData(
        WireWriter writer,
        DnsOpaqueData data,
        int recordTypeValue)
    {
        if (writer == null || data == null || data.Bytes == null ||
            data.Bytes.Length == 0)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer,
                recordTypeValue == 67 ? "hhit" : "brid",
                "A nonempty CBOR record payload is required.");
        }

        return writer.WriteBytes(data.Bytes);
    }

    internal static bool ZonemdDigestLengthIsValid(int algorithm, int length)
    {
        if (length < 12) return false;
        if (algorithm == 1) return length == 48;
        if (algorithm == 2) return length == 64;
        return true;
    }

    internal static bool ResolverValuesAreValid(
        DnsResolverInformationData data)
    {
        if (data == null || data.Values == null || data.Values.Count == 0)
            return false;
        int index = 0;
        while (index < data.Values.Count)
        {
            byte[] value = data.Values[index];
            if (value == null || value.Length == 0 || value.Length > 255 ||
                !ResolverKeyIsValid(value))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ResolverKeyIsValid(byte[] value)
    {
        int index = 0;
        while (index < value.Length && value[index] != 61)
        {
            if (value[index] < 32 || value[index] > 126) return false;
            index = index + 1;
        }

        return index > 0;
    }

    internal static bool Unsigned8IsValid(int value)
    {
        return value >= 0 && value <= 255;
    }

    internal static bool Unsigned16IsValid(int value)
    {
        return value >= 0 && value <= 65535;
    }

    internal static DnsDecodeResult AmtRelayLengthFailure(int offset)
    {
        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidLength,
            "amtRelay.relay",
            "The AMTRELAY relay length does not match its relay type.",
            offset);
    }

    internal static DnsDecodeResult MetaTypeFailure(int offset)
    {
        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "nxname",
            "NXNAME is a bitmap-only meta type and has no record data.",
            offset);
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
