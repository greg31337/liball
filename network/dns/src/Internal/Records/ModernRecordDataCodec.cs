using System;

namespace dns;

internal static class ModernRecordDataCodec
{
    internal static DnsDecodeResult DecodeRecordData(
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        if (recordTypeValue == 28)
        {
            return SharedRecordDataCodec.DecodeAddressData(
                message, offset, length, 16);
        }

        if (recordTypeValue == 29)
        {
            return DecodeLocData(message, offset, length);
        }

        if (recordTypeValue == 33)
        {
            return DecodeSrvData(message, offset, length);
        }

        if (recordTypeValue == 35)
        {
            return DecodeNaptrData(message, offset, length);
        }

        if (recordTypeValue == 37)
        {
            return DecodeCertData(message, offset, length);
        }

        if (recordTypeValue == 39)
        {
            DnsDecodeResult nameResult = SharedRecordDataCodec.DecodeUncompressedName(
                message, offset, length, "dname.target");
            return WrapNameResult(nameResult);
        }

        if (recordTypeValue == 42)
        {
            return DecodeAplData(message, offset, length);
        }

        if (recordTypeValue == 256)
        {
            return DecodeUriData(message, offset, length);
        }

        if (recordTypeValue == 257)
        {
            return DecodeCaaData(message, offset, length);
        }

        if (recordTypeValue == 108 || recordTypeValue == 109)
        {
            return DecodeEuiData(message, offset, length, recordTypeValue);
        }

        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of this modern record codec family.",
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

        if (recordTypeValue == 28)
        {
            if (data.Address == null)
            {
                return MissingSchema(writer, "address");
            }

            return SharedRecordDataCodec.EncodeAddressData(writer, data.Address, 16);
        }

        if (recordTypeValue == 29)
        {
            return data.Loc == null ? MissingSchema(writer, "loc") :
                EncodeLocData(writer, data.Loc);
        }

        if (recordTypeValue == 33)
        {
            return data.Srv == null ? MissingSchema(writer, "srv") :
                EncodeSrvData(writer, table, data.Srv, options);
        }

        if (recordTypeValue == 35)
        {
            return data.Naptr == null ? MissingSchema(writer, "naptr") :
                EncodeNaptrData(writer, table, data.Naptr, options);
        }

        if (recordTypeValue == 37)
        {
            return data.Cert == null ? MissingSchema(writer, "cert") :
                EncodeCertData(writer, data.Cert);
        }

        if (recordTypeValue == 39)
        {
            if (data.Name == null)
            {
                return MissingSchema(writer, "name");
            }

            return SharedRecordDataCodec.EncodeNameData(
                writer, table, data.Name, options, false);
        }

        if (recordTypeValue == 42)
        {
            return data.Apl == null ? MissingSchema(writer, "apl") :
                EncodeAplData(writer, data.Apl);
        }

        if (recordTypeValue == 256)
        {
            return data.Uri == null ? MissingSchema(writer, "uri") :
                EncodeUriData(writer, data.Uri);
        }

        if (recordTypeValue == 257)
        {
            return data.Caa == null ? MissingSchema(writer, "caa") :
                EncodeCaaData(writer, data.Caa);
        }

        if (recordTypeValue == 108 || recordTypeValue == 109)
        {
            return data.Eui == null ? MissingSchema(writer, "eui") :
                EncodeEuiData(writer, data.Eui, recordTypeValue);
        }

        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of this modern record codec family.",
            writer.WriteOffset());
    }

    internal static DnsDecodeResult DecodeLocData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateFixedRegion(
            message.Length, offset, length, 16, "loc");
        if (!validation.Success)
        {
            return validation;
        }

        int version = message[offset];
        int size = message[offset + 1];
        int horizontal = message[offset + 2];
        int vertical = message[offset + 3];
        if (version != 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnsupportedVersion,
                "loc.version",
                "Only LOC version zero is supported.",
                offset);
        }

        if (!PrecisionValueIsValid(size) ||
            !PrecisionValueIsValid(horizontal) ||
            !PrecisionValueIsValid(vertical))
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "loc.precision",
                "A LOC size or precision has an invalid mantissa or exponent.",
                offset + 1);
        }

        WireReader reader = new WireReader(message.ToArray(), offset + 4, 12);
        WireReadResult latitude = reader.ReadUnsigned32();
        WireReadResult longitude = reader.ReadUnsigned32();
        WireReadResult altitude = reader.ReadUnsigned32();
        DnsLocData loc = new DnsLocData();
        loc.Version = version;
        loc.Size = size;
        loc.HorizontalPrecision = horizontal;
        loc.VerticalPrecision = vertical;
        loc.Latitude = (uint)latitude.UnsignedValue;
        loc.Longitude = (uint)longitude.UnsignedValue;
        loc.Altitude = (uint)altitude.UnsignedValue;
        DnsRecordData data = new DnsRecordData();
        data.Loc = loc;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeLocData(WireWriter writer, DnsLocData data)
    {
        if (writer == null || data == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "loc", "LOC data is required.");
        }

        if (data.Version != 0 || !PrecisionValueIsValid(data.Size) ||
            !PrecisionValueIsValid(data.HorizontalPrecision) ||
            !PrecisionValueIsValid(data.VerticalPrecision))
        {
            return writer.FailAt(
                data.Version != 0 ? DnsDiagnosticCode.UnsupportedVersion :
                    DnsDiagnosticCode.ValueOutOfRange,
                "loc",
                "LOC requires version zero and valid precision values.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult version = writer.WriteUnsigned8((ulong)data.Version);
        if (!version.Success) return version;
        WireWriteResult size = writer.WriteUnsigned8((ulong)data.Size);
        if (!size.Success) return size;
        WireWriteResult horizontal = writer.WriteUnsigned8(
            (ulong)data.HorizontalPrecision);
        if (!horizontal.Success) return horizontal;
        WireWriteResult vertical = writer.WriteUnsigned8(
            (ulong)data.VerticalPrecision);
        if (!vertical.Success) return vertical;
        WireWriteResult latitude = writer.WriteUnsigned32(data.Latitude);
        if (!latitude.Success) return latitude;
        WireWriteResult longitude = writer.WriteUnsigned32(data.Longitude);
        if (!longitude.Success) return longitude;
        WireWriteResult altitude = writer.WriteUnsigned32(data.Altitude);
        if (!altitude.Success) return altitude;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeSrvData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 7)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "srv",
                "SRV data requires three integers and an uncompressed target name.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult priority = reader.ReadUnsigned16();
        WireReadResult weight = reader.ReadUnsigned16();
        WireReadResult port = reader.ReadUnsigned16();
        DnsDecodeResult target = SharedRecordDataCodec.DecodeUncompressedName(
            message, offset + 6, length - 6, "srv.target");
        if (!target.Success || target.Name == null) return target;
        DnsSrvData srv = new DnsSrvData();
        srv.Priority = (int)priority.UnsignedValue;
        srv.Weight = (int)weight.UnsignedValue;
        srv.Port = (int)port.UnsignedValue;
        srv.Target = target.Name;
        DnsRecordData data = new DnsRecordData();
        data.Srv = srv;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeSrvData(
        WireWriter writer,
        CompressionTable table,
        DnsSrvData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "srv", "SRV data and encoding state are required.");
        }

        if (!Unsigned16IsValid(data.Priority) || !Unsigned16IsValid(data.Weight) ||
            !Unsigned16IsValid(data.Port))
        {
            return writer.FailAt(DnsDiagnosticCode.ValueOutOfRange, "srv",
                "SRV priority, weight, and port must fit in sixteen bits.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult priority = writer.WriteUnsigned16((ulong)data.Priority);
        if (!priority.Success) return priority;
        WireWriteResult weight = writer.WriteUnsigned16((ulong)data.Weight);
        if (!weight.Success) return weight;
        WireWriteResult port = writer.WriteUnsigned16((ulong)data.Port);
        if (!port.Success) return port;
        WireWriteResult target = table.WriteName(writer, data.Target, options, false);
        if (!target.Success) return target;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeNaptrData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 8)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "naptr",
                "NAPTR data is truncated.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult order = reader.ReadUnsigned16();
        WireReadResult preference = reader.ReadUnsigned16();
        WireReadResult flags = reader.ReadCharacterString();
        if (!flags.Success) return SharedRecordDataCodec.FromReadFailure(flags);
        WireReadResult services = reader.ReadCharacterString();
        if (!services.Success) return SharedRecordDataCodec.FromReadFailure(services);
        WireReadResult regularExpression = reader.ReadCharacterString();
        if (!regularExpression.Success)
            return SharedRecordDataCodec.FromReadFailure(regularExpression);
        int replacementLength = offset + length - reader.ReadOffset();
        DnsDecodeResult replacement = SharedRecordDataCodec.DecodeUncompressedName(
            message, reader.ReadOffset(), replacementLength, "naptr.replacement");
        if (!replacement.Success || replacement.Name == null) return replacement;
        if (regularExpression.Bytes.Length > 0 && replacement.Name.Labels.Count > 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "naptr.substitution",
                "NAPTR regular-expression and replacement fields are mutually exclusive.",
                reader.ReadOffset());
        }

        DnsNaptrData naptr = new DnsNaptrData();
        naptr.Order = (int)order.UnsignedValue;
        naptr.Preference = (int)preference.UnsignedValue;
        naptr.Flags = flags.Bytes;
        naptr.Services = services.Bytes;
        naptr.RegularExpression = regularExpression.Bytes;
        naptr.Replacement = replacement.Name;
        DnsRecordData data = new DnsRecordData();
        data.Naptr = naptr;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeNaptrData(
        WireWriter writer,
        CompressionTable table,
        DnsNaptrData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.Flags == null || data.Services == null ||
            data.RegularExpression == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "naptr", "Complete NAPTR data is required.");
        }

        if (!Unsigned16IsValid(data.Order) || !Unsigned16IsValid(data.Preference) ||
            data.RegularExpression.Length > 0 && data.Replacement.Labels.Count > 0)
        {
            return writer.FailAt(DnsDiagnosticCode.InvalidRecordData, "naptr",
                "NAPTR values are out of range or substitution fields conflict.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult order = writer.WriteUnsigned16((ulong)data.Order);
        if (!order.Success) return order;
        WireWriteResult preference = writer.WriteUnsigned16((ulong)data.Preference);
        if (!preference.Success) return preference;
        WireWriteResult flags = writer.WriteCharacterString(data.Flags);
        if (!flags.Success) return flags;
        WireWriteResult services = writer.WriteCharacterString(data.Services);
        if (!services.Success) return services;
        WireWriteResult regularExpression = writer.WriteCharacterString(
            data.RegularExpression);
        if (!regularExpression.Success) return regularExpression;
        WireWriteResult replacement = table.WriteName(
            writer, data.Replacement, options, false);
        if (!replacement.Success) return replacement;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeCertData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 5)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd, "cert",
                "CERT data requires certificate type, key tag, and algorithm fields.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult certificateType = reader.ReadUnsigned16();
        WireReadResult keyTag = reader.ReadUnsigned16();
        WireReadResult algorithm = reader.ReadUnsigned8();
        WireReadResult certificate = reader.ReadBytes(reader.RemainingLength());
        DnsCertData cert = new DnsCertData();
        cert.CertificateType = (int)certificateType.UnsignedValue;
        cert.KeyTag = (int)keyTag.UnsignedValue;
        cert.Algorithm = (DnsDnssecAlgorithm)algorithm.UnsignedValue;
        cert.AlgorithmValue = (int)algorithm.UnsignedValue;
        cert.Certificate = certificate.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Cert = cert;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeCertData(WireWriter writer, DnsCertData data)
    {
        if (writer == null || data == null || data.Certificate == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "cert", "Complete CERT data is required.");
        }

        int algorithm = DnsHeaderCodec.ResolveCode(
            data.AlgorithmValue, (int)data.Algorithm);
        if (!Unsigned16IsValid(data.CertificateType) ||
            !Unsigned16IsValid(data.KeyTag) || algorithm < 0 || algorithm > 255)
        {
            return writer.FailAt(DnsDiagnosticCode.ValueOutOfRange, "cert",
                "CERT numeric fields exceed their wire widths.", writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult certificateType = writer.WriteUnsigned16(
            (ulong)data.CertificateType);
        if (!certificateType.Success) return certificateType;
        WireWriteResult keyTag = writer.WriteUnsigned16((ulong)data.KeyTag);
        if (!keyTag.Success) return keyTag;
        WireWriteResult algorithmResult = writer.WriteUnsigned8((ulong)algorithm);
        if (!algorithmResult.Success) return algorithmResult;
        WireWriteResult certificate = writer.WriteBytes(data.Certificate);
        if (!certificate.Success) return certificate;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeAplData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        DnsAplData apl = new DnsAplData();
        int cursor = offset;
        int end = offset + length;
        while (cursor < end)
        {
            if (end - cursor < 4)
            {
                return SharedRecordDataCodec.DecodeFailure(
                    DnsDiagnosticCode.UnexpectedEnd, "apl.item",
                    "An APL item header is truncated.", cursor);
            }

            int family = (message[cursor] << 8) | message[cursor + 1];
            int prefix = message[cursor + 2];
            int lengthFlags = message[cursor + 3];
            int addressLength = lengthFlags & 127;
            if (addressLength > end - cursor - 4)
            {
                return SharedRecordDataCodec.DecodeFailure(
                    DnsDiagnosticCode.UnexpectedEnd, "apl.address",
                    "An APL address part is truncated.", cursor + 4);
            }

            byte[] address = SharedRecordDataCodec.CopyRegion(
                message, cursor + 4, addressLength);
            if (!AplItemIsValid(family, prefix, address))
            {
                return SharedRecordDataCodec.DecodeFailure(
                    DnsDiagnosticCode.InvalidRecordData, "apl.item",
                    "An APL item has an invalid prefix or non-canonical address part.",
                    cursor);
            }

            DnsAplItem item = new DnsAplItem();
            item.AddressFamily = family;
            item.PrefixLength = prefix;
            item.Negated = (lengthFlags & 128) != 0;
            item.Address = address;
            apl.Items.Add(item);
            cursor = cursor + 4 + addressLength;
        }

        DnsRecordData data = new DnsRecordData();
        data.Apl = apl;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeAplData(WireWriter writer, DnsAplData data)
    {
        if (writer == null || data == null || data.Items == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "apl", "APL item data is required.");
        }

        int start = writer.WriteOffset();
        int index = 0;
        while (index < data.Items.Count)
        {
            DnsAplItem item = data.Items[index];
            if (item == null || item.AddressFamily < 0 || item.AddressFamily > 65535 ||
                item.PrefixLength < 0 || item.PrefixLength > 255 ||
                item.Address == null || item.Address.Length > 127 ||
                !AplItemIsValid(item.AddressFamily, item.PrefixLength, item.Address))
            {
                return writer.FailAt(DnsDiagnosticCode.InvalidRecordData, "apl.item",
                    "An APL item has invalid or non-canonical values.",
                    writer.WriteOffset());
            }

            WireWriteResult family = writer.WriteUnsigned16((ulong)item.AddressFamily);
            if (!family.Success) return family;
            WireWriteResult prefix = writer.WriteUnsigned8((ulong)item.PrefixLength);
            if (!prefix.Success) return prefix;
            int lengthFlags = item.Address.Length;
            if (item.Negated) lengthFlags = lengthFlags | 128;
            WireWriteResult addressLength = writer.WriteUnsigned8((ulong)lengthFlags);
            if (!addressLength.Success) return addressLength;
            WireWriteResult address = writer.WriteBytes(item.Address);
            if (!address.Success) return address;
            index = index + 1;
        }

        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeUriData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 5)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength, "uri",
                "URI data requires priority, weight, and a nonempty target.", offset);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult priority = reader.ReadUnsigned16();
        WireReadResult weight = reader.ReadUnsigned16();
        WireReadResult target = reader.ReadBytes(reader.RemainingLength());
        DnsUriData uri = new DnsUriData();
        uri.Priority = (int)priority.UnsignedValue;
        uri.Weight = (int)weight.UnsignedValue;
        uri.Target = target.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Uri = uri;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeUriData(WireWriter writer, DnsUriData data)
    {
        if (writer == null || data == null || data.Target == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "uri", "Complete URI data is required.");
        }

        if (!Unsigned16IsValid(data.Priority) || !Unsigned16IsValid(data.Weight) ||
            data.Target.Length == 0)
        {
            return writer.FailAt(DnsDiagnosticCode.InvalidRecordData, "uri",
                "URI priority and weight must fit in sixteen bits and target must be nonempty.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult priority = writer.WriteUnsigned16((ulong)data.Priority);
        if (!priority.Success) return priority;
        WireWriteResult weight = writer.WriteUnsigned16((ulong)data.Weight);
        if (!weight.Success) return weight;
        WireWriteResult target = writer.WriteBytes(data.Target);
        if (!target.Success) return target;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeCaaData(
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
                DnsDiagnosticCode.InvalidLength, "caa",
                "CAA data requires flags, a nonempty tag, and a tag length.", offset);
        }

        int flags = message[offset];
        int tagLength = message[offset + 1];
        if ((flags & 127) != 0 || tagLength < 1 || tagLength > length - 2)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData, "caa",
                "CAA flags or tag length are invalid.", offset);
        }

        byte[] tag = SharedRecordDataCodec.CopyRegion(message, offset + 2, tagLength);
        if (!CaaTagIsValid(tag))
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData, "caa.tag",
                "A CAA tag may contain only ASCII letters and digits.", offset + 2);
        }

        DnsCaaData caa = new DnsCaaData();
        caa.Flags = flags;
        caa.Tag = tag;
        caa.Value = SharedRecordDataCodec.CopyRegion(
            message, offset + 2 + tagLength, length - 2 - tagLength);
        DnsRecordData data = new DnsRecordData();
        data.Caa = caa;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeCaaData(WireWriter writer, DnsCaaData data)
    {
        if (writer == null || data == null || data.Tag == null || data.Value == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "caa", "Complete CAA data is required.");
        }

        if ((data.Flags != 0 && data.Flags != 128) || data.Tag.Length < 1 ||
            data.Tag.Length > 255 || !CaaTagIsValid(data.Tag))
        {
            return writer.FailAt(DnsDiagnosticCode.InvalidRecordData, "caa",
                "CAA flags or tag bytes are invalid.", writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult flags = writer.WriteUnsigned8((ulong)data.Flags);
        if (!flags.Success) return flags;
        WireWriteResult tagLength = writer.WriteUnsigned8((ulong)data.Tag.Length);
        if (!tagLength.Success) return tagLength;
        WireWriteResult tag = writer.WriteBytes(data.Tag);
        if (!tag.Success) return tag;
        WireWriteResult value = writer.WriteBytes(data.Value);
        if (!value.Success) return value;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeEuiData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        int recordTypeValue)
    {
        int expectedLength = recordTypeValue == 108 ? 6 : 8;
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateFixedRegion(
            message.Length, offset, length, expectedLength, "eui");
        if (!validation.Success) return validation;
        DnsEuiData eui = new DnsEuiData();
        eui.Address = SharedRecordDataCodec.CopyRegion(
            message, offset, expectedLength);
        DnsRecordData data = new DnsRecordData();
        data.Eui = eui;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeEuiData(
        WireWriter writer,
        DnsEuiData data,
        int recordTypeValue)
    {
        if (writer == null || data == null || data.Address == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "eui", "EUI address data is required.");
        }

        int expectedLength = recordTypeValue == 108 ? 6 : 8;
        if (data.Address.Length != expectedLength)
        {
            return writer.FailAt(DnsDiagnosticCode.InvalidLength, "eui",
                "The EUI address does not have the required width.",
                writer.WriteOffset());
        }

        return writer.WriteBytes(data.Address);
    }

    internal static DnsDecodeResult WrapNameResult(DnsDecodeResult nameResult)
    {
        if (!nameResult.Success || nameResult.Name == null) return nameResult;
        DnsNameData name = new DnsNameData();
        name.Name = nameResult.Name;
        DnsRecordData data = new DnsRecordData();
        data.Name = name;
        return SharedRecordDataCodec.DecodeSuccess(data, nameResult.BytesConsumed);
    }

    internal static bool PrecisionValueIsValid(int value)
    {
        if (value < 0 || value > 255) return false;
        int mantissa = value >> 4;
        int exponent = value & 15;
        return mantissa <= 9 && exponent <= 9 &&
            (mantissa != 0 || exponent == 0);
    }

    internal static bool AplItemIsValid(
        int family,
        int prefix,
        byte[] address)
    {
        if (address == null || address.Length > 127) return false;
        int maximumBits = -1;
        int maximumBytes = 127;
        if (family == 1)
        {
            maximumBits = 32;
            maximumBytes = 4;
        }
        else if (family == 2)
        {
            maximumBits = 128;
            maximumBytes = 16;
        }
        else
        {
            maximumBits = 255;
            maximumBytes = 32;
        }

        if (prefix < 0 || prefix > maximumBits || address.Length > maximumBytes ||
            address.Length > (prefix + 7) / 8)
        {
            return false;
        }

        if (address.Length > 0 && address[address.Length - 1] == 0) return false;
        if (address.Length > 0 && address.Length == (prefix + 7) / 8 &&
            prefix % 8 != 0)
        {
            int hostMask = 255 >> (prefix % 8);
            if ((address[address.Length - 1] & hostMask) != 0) return false;
        }

        return true;
    }

    internal static bool CaaTagIsValid(byte[] tag)
    {
        if (tag == null || tag.Length == 0) return false;
        int index = 0;
        while (index < tag.Length)
        {
            byte value = tag[index];
            bool letter = value >= 65 && value <= 90 || value >= 97 && value <= 122;
            bool digit = value >= 48 && value <= 57;
            if (!letter && !digit) return false;
            index = index + 1;
        }

        return true;
    }

    internal static bool Unsigned16IsValid(int value)
    {
        return value >= 0 && value <= 65535;
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
