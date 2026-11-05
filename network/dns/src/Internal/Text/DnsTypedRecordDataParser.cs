using System.Collections.Generic;

namespace dns;

internal class DnsRecordDataTextResult
{
    internal bool Success;
    internal DnsRecordData? Data;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class DnsTypedRecordDataParser
{
    internal static DnsRecordDataTextResult ParseRecordData(
        int recordTypeValue,
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        RecordDataModel model = RecordDataModelCatalog.ReadRecordDataModel(
            recordTypeValue);
        switch (model)
        {
            case RecordDataModel.Address:
                return ParseAddressData(tokens, start, recordTypeValue);
            case RecordDataModel.Name:
                return ParseNameData(tokens, start, options);
            case RecordDataModel.TwoName:
                return ParseTwoNameData(tokens, start, options);
            case RecordDataModel.PreferenceName:
                return ParsePreferenceNameData(tokens, start, options);
            case RecordDataModel.CharacterStrings:
                return ParseCharacterStringsData(tokens, start);
            case RecordDataModel.FixedBytes:
                return ParseFixedBytesData(tokens, start, recordTypeValue);
            case RecordDataModel.Soa:
                return ParseSoaData(tokens, start, options);
            case RecordDataModel.Wks:
                return ParseWksData(tokens, start);
            case RecordDataModel.A6:
                return ParseA6Data(tokens, start, options);
            case RecordDataModel.Px:
                return ParsePxData(tokens, start, options);
            case RecordDataModel.Loc:
                return ParseLocData(tokens, start);
            case RecordDataModel.Srv:
                return ParseSrvData(tokens, start, options);
            case RecordDataModel.Naptr:
                return ParseNaptrData(tokens, start, options);
            case RecordDataModel.Cert:
                return ParseCertData(tokens, start);
            case RecordDataModel.Apl:
                return ParseAplData(tokens, start);
            case RecordDataModel.Signature:
                return ParseSignatureData(tokens, start, options);
            case RecordDataModel.Key:
                return ParseKeyData(tokens, start);
            case RecordDataModel.Ds:
                return ParseDsData(tokens, start);
            case RecordDataModel.Sshfp:
                return ParseSshfpData(tokens, start);
            case RecordDataModel.IpsecKey:
                return ParseIpsecKeyData(tokens, start, options);
            case RecordDataModel.Nsec:
                return ParseNsecData(tokens, start, options);
            case RecordDataModel.Nsec3:
                return ParseNsec3Data(tokens, start, options);
            case RecordDataModel.Nsec3Param:
                return ParseNsec3ParamData(tokens, start);
            case RecordDataModel.Dhcid:
                return ParseDhcidData(tokens, start);
            case RecordDataModel.Tlsa:
                return ParseTlsaData(tokens, start);
            case RecordDataModel.Hip:
                return ParseHipData(tokens, start, options);
            case RecordDataModel.Csync:
                return ParseCsyncData(tokens, start, options);
            case RecordDataModel.Zonemd:
                return ParseZonemdData(tokens, start);
            case RecordDataModel.Svcb:
                return ParseSvcbData(tokens, start, options);
            case RecordDataModel.Dsync:
                return ParseDsyncData(tokens, start, options);
            case RecordDataModel.IlNp:
                return ParseIlNpData(tokens, start, recordTypeValue);
            case RecordDataModel.Eui:
                return ParseEuiData(tokens, start, recordTypeValue);
            case RecordDataModel.Uri:
                return ParseUriData(tokens, start);
            case RecordDataModel.Caa:
                return ParseCaaData(tokens, start);
            case RecordDataModel.AmtRelay:
                return ParseAmtRelayData(tokens, start, options);
            case RecordDataModel.ResolverInformation:
                return ParseResolverInformationData(tokens, start);
            case RecordDataModel.Tkey:
                return ParseTkeyData(tokens, start, options);
            case RecordDataModel.Tsig:
                return ParseTsigData(tokens, start, options);
            default:
                return Failure("This typed RDATA presentation form is not supported.");
        }
    }

    static DnsRecordDataTextResult ParseAddressData(
        List<string> tokens,
        int start,
        int recordTypeValue)
    {
        if (tokens.Count != start + 1) return FieldCount("Address RDATA requires one field.");
        int length = 4;
        if (recordTypeValue == 28) length = 16;
        DnsTextBytesResult address = DnsTextBytes.ParseAddress(tokens[start], length);
        if (!address.Success) return FromBytesFailure(address);
        DnsAddressData value = new DnsAddressData();
        value.Address = address.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Address = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseNameData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 1) return FieldCount("Name RDATA requires one field.");
        DnsTextResult name = DnsPresentationNameText.ParseNameToken(
            tokens[start], options);
        if (!name.Success || name.Name == null) return FromTextFailure(name);
        DnsNameData value = new DnsNameData();
        value.Name = name.Name;
        DnsRecordData data = new DnsRecordData();
        data.Name = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseTwoNameData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 2) return FieldCount("Two-name RDATA requires two fields.");
        DnsTextResult first = DnsPresentationNameText.ParseNameToken(
            tokens[start], options);
        if (!first.Success || first.Name == null) return FromTextFailure(first);
        DnsTextResult second = DnsPresentationNameText.ParseNameToken(
            tokens[start + 1], options);
        if (!second.Success || second.Name == null) return FromTextFailure(second);
        DnsTwoNameData value = new DnsTwoNameData();
        value.FirstName = first.Name;
        value.SecondName = second.Name;
        DnsRecordData data = new DnsRecordData();
        data.TwoName = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParsePreferenceNameData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 2) return FieldCount("Preference RDATA requires two fields.");
        DnsTextNumberResult preference = DnsTextNumber.ParseDecimal(
            tokens[start], 65535, "preference");
        if (!preference.Success) return FromNumberFailure(preference);
        DnsTextResult name = DnsPresentationNameText.ParseNameToken(
            tokens[start + 1], options);
        if (!name.Success || name.Name == null) return FromTextFailure(name);
        DnsPreferenceNameData value = new DnsPreferenceNameData();
        value.Preference = (int)preference.Value;
        value.Name = name.Name;
        DnsRecordData data = new DnsRecordData();
        data.PreferenceName = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseCharacterStringsData(
        List<string> tokens,
        int start)
    {
        if (tokens.Count <= start) return FieldCount("Character-string RDATA requires a value.");
        DnsCharacterStringsData value = new DnsCharacterStringsData();
        int index = start;
        while (index < tokens.Count)
        {
            DnsTextBytesResult text = DnsTextBytes.ParseCharacterString(
                tokens[index], "characterString");
            if (!text.Success) return FromBytesFailure(text);
            if (text.Bytes.Length > 255)
            {
                return Failure("A DNS character string cannot exceed 255 octets.");
            }

            value.Strings.Add(text.Bytes);
            index = index + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.CharacterStrings = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseFixedBytesData(
        List<string> tokens,
        int start,
        int recordTypeValue)
    {
        if (tokens.Count != start + 1) return FieldCount("Binary RDATA requires one field.");
        DnsTextBytesResult bytes;
        if (recordTypeValue == 61)
        {
            bytes = DnsTextBytes.ParseBase64(tokens[start], "recordData");
        }
        else
        {
            bytes = DnsTextBytes.ParseHex(tokens[start], "recordData");
        }

        if (!bytes.Success) return FromBytesFailure(bytes);
        DnsFixedBytesData value = new DnsFixedBytesData();
        value.Bytes = bytes.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.FixedBytes = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseSoaData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 7) return FieldCount("SOA RDATA requires seven fields.");
        DnsTextResult primary = DnsPresentationNameText.ParseNameToken(tokens[start], options);
        if (!primary.Success || primary.Name == null) return FromTextFailure(primary);
        DnsTextResult mailbox = DnsPresentationNameText.ParseNameToken(tokens[start + 1], options);
        if (!mailbox.Success || mailbox.Name == null) return FromTextFailure(mailbox);
        DnsTextNumberResult serial = ReadUnsigned32(tokens[start + 2], "serial");
        if (!serial.Success) return FromNumberFailure(serial);
        DnsTextNumberResult refresh = DnsTimeToLiveText.ParseTimeToLive(tokens[start + 3]);
        if (!refresh.Success) return FromNumberFailure(refresh);
        DnsTextNumberResult retry = DnsTimeToLiveText.ParseTimeToLive(tokens[start + 4]);
        if (!retry.Success) return FromNumberFailure(retry);
        DnsTextNumberResult expire = DnsTimeToLiveText.ParseTimeToLive(tokens[start + 5]);
        if (!expire.Success) return FromNumberFailure(expire);
        DnsTextNumberResult minimum = DnsTimeToLiveText.ParseTimeToLive(tokens[start + 6]);
        if (!minimum.Success) return FromNumberFailure(minimum);
        DnsSoaData value = new DnsSoaData();
        value.PrimaryNameServer = primary.Name;
        value.ResponsibleMailbox = mailbox.Name;
        value.Serial = (uint)serial.Value;
        value.Refresh = (uint)refresh.Value;
        value.Retry = (uint)retry.Value;
        value.Expire = (uint)expire.Value;
        value.Minimum = (uint)minimum.Value;
        DnsRecordData data = new DnsRecordData();
        data.Soa = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseSrvData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 4) return FieldCount("SRV RDATA requires four fields.");
        DnsTextNumberResult priority = ReadUnsigned16(tokens[start], "priority");
        if (!priority.Success) return FromNumberFailure(priority);
        DnsTextNumberResult weight = ReadUnsigned16(tokens[start + 1], "weight");
        if (!weight.Success) return FromNumberFailure(weight);
        DnsTextNumberResult port = ReadUnsigned16(tokens[start + 2], "port");
        if (!port.Success) return FromNumberFailure(port);
        DnsTextResult target = DnsPresentationNameText.ParseNameToken(tokens[start + 3], options);
        if (!target.Success || target.Name == null) return FromTextFailure(target);
        DnsSrvData value = new DnsSrvData();
        value.Priority = (int)priority.Value;
        value.Weight = (int)weight.Value;
        value.Port = (int)port.Value;
        value.Target = target.Name;
        DnsRecordData data = new DnsRecordData();
        data.Srv = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseWksData(List<string> tokens, int start)
    {
        if (tokens.Count < start + 2) return FieldCount("WKS RDATA requires an address and protocol.");
        DnsTextBytesResult address = DnsTextBytes.ParseAddress(tokens[start], 4);
        if (!address.Success) return FromBytesFailure(address);
        DnsTextNumberResult protocol = ReadUnsigned8(tokens[start + 1], "protocol");
        if (!protocol.Success) return FromNumberFailure(protocol);
        int maximumPort = -1;
        int index = start + 2;
        while (index < tokens.Count)
        {
            DnsTextNumberResult port = ReadUnsigned16(tokens[index], "port");
            if (!port.Success) return FromNumberFailure(port);
            if (port.Value > maximumPort) maximumPort = (int)port.Value;
            index = index + 1;
        }

        byte[] bitmap = [];
        if (maximumPort >= 0) bitmap = new byte[(maximumPort / 8) + 1];
        index = start + 2;
        while (index < tokens.Count)
        {
            DnsTextNumberResult port = ReadUnsigned16(tokens[index], "port");
            int portValue = (int)port.Value;
            bitmap[portValue / 8] = (byte)(bitmap[portValue / 8] | (128 >> (portValue & 7)));
            index = index + 1;
        }

        DnsWksData value = new DnsWksData();
        value.Address = address.Bytes;
        value.Protocol = (int)protocol.Value;
        value.PortBitmap = bitmap;
        DnsRecordData data = new DnsRecordData();
        data.Wks = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseA6Data(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count < start + 2 || tokens.Count > start + 3)
            return FieldCount("A6 RDATA requires prefix length, address, and optional name.");
        DnsTextNumberResult prefix = DnsTextNumber.ParseDecimal(tokens[start], 128, "prefixLength");
        if (!prefix.Success) return FromNumberFailure(prefix);
        DnsTextBytesResult address = DnsTextBytes.ParseAddress(tokens[start + 1], 16);
        if (!address.Success) return FromBytesFailure(address);
        int suffixLength = (128 - (int)prefix.Value + 7) / 8;
        DnsA6Data value = new DnsA6Data();
        value.PrefixLength = (int)prefix.Value;
        value.AddressSuffix = CopyBytes(address.Bytes, 16 - suffixLength);
        if (prefix.Value > 0)
        {
            if (tokens.Count != start + 3) return Failure("A6 prefix names are required for nonzero prefixes.");
            DnsTextResult name = DnsPresentationNameText.ParseNameToken(tokens[start + 2], options);
            if (!name.Success || name.Name == null) return FromTextFailure(name);
            value.PrefixName = name.Name;
        }

        DnsRecordData data = new DnsRecordData();
        data.A6 = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParsePxData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 3) return FieldCount("PX RDATA requires three fields.");
        DnsTextNumberResult preference = ReadUnsigned16(tokens[start], "preference");
        if (!preference.Success) return FromNumberFailure(preference);
        DnsTextResult first = DnsPresentationNameText.ParseNameToken(tokens[start + 1], options);
        if (!first.Success || first.Name == null) return FromTextFailure(first);
        DnsTextResult second = DnsPresentationNameText.ParseNameToken(tokens[start + 2], options);
        if (!second.Success || second.Name == null) return FromTextFailure(second);
        DnsPxData value = new DnsPxData();
        value.Preference = (int)preference.Value;
        value.Map822 = first.Name;
        value.MapX400 = second.Name;
        DnsRecordData data = new DnsRecordData();
        data.Px = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseLocData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 12)
            return FieldCount("LOC RDATA requires latitude, longitude, altitude, size, and precision fields.");
        DnsTextNumberResult latitude = ParseLocCoordinate(
            tokens[start], tokens[start + 1], tokens[start + 2],
            tokens[start + 3], true);
        if (!latitude.Success) return FromNumberFailure(latitude);
        DnsTextNumberResult longitude = ParseLocCoordinate(
            tokens[start + 4], tokens[start + 5], tokens[start + 6],
            tokens[start + 7], false);
        if (!longitude.Success) return FromNumberFailure(longitude);
        DnsTextNumberResult altitude = ParseScaledDecimal(
            tokens[start + 8], 2, true, "altitude");
        if (!altitude.Success) return FromNumberFailure(altitude);
        long altitudeWire = altitude.Value + 10000000;
        if (altitudeWire < 0 || altitudeWire > uint.MaxValue)
            return Failure("The LOC altitude is outside the wire range.");
        DnsTextNumberResult size = ParseScaledDecimal(
            tokens[start + 9], 2, false, "size");
        if (!size.Success) return FromNumberFailure(size);
        DnsTextNumberResult horizontal = ParseScaledDecimal(
            tokens[start + 10], 2, false, "horizontalPrecision");
        if (!horizontal.Success) return FromNumberFailure(horizontal);
        DnsTextNumberResult vertical = ParseScaledDecimal(
            tokens[start + 11], 2, false, "verticalPrecision");
        if (!vertical.Success) return FromNumberFailure(vertical);
        int sizeValue = EncodeLocPrecision(size.Value);
        int horizontalValue = EncodeLocPrecision(horizontal.Value);
        int verticalValue = EncodeLocPrecision(vertical.Value);
        if (sizeValue < 0 || horizontalValue < 0 || verticalValue < 0)
            return Failure("LOC precision values require one significant decimal digit.");
        DnsLocData value = new DnsLocData();
        value.Latitude = (uint)latitude.Value;
        value.Longitude = (uint)longitude.Value;
        value.Altitude = (uint)altitudeWire;
        value.Size = sizeValue;
        value.HorizontalPrecision = horizontalValue;
        value.VerticalPrecision = verticalValue;
        DnsRecordData data = new DnsRecordData();
        data.Loc = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseNaptrData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 6) return FieldCount("NAPTR RDATA requires six fields.");
        DnsTextNumberResult order = ReadUnsigned16(tokens[start], "order");
        if (!order.Success) return FromNumberFailure(order);
        DnsTextNumberResult preference = ReadUnsigned16(tokens[start + 1], "preference");
        if (!preference.Success) return FromNumberFailure(preference);
        DnsTextBytesResult flags = DnsTextBytes.ParseCharacterString(tokens[start + 2], "flags");
        if (!flags.Success) return FromBytesFailure(flags);
        DnsTextBytesResult services = DnsTextBytes.ParseCharacterString(tokens[start + 3], "services");
        if (!services.Success) return FromBytesFailure(services);
        DnsTextBytesResult expression = DnsTextBytes.ParseCharacterString(tokens[start + 4], "regularExpression");
        if (!expression.Success) return FromBytesFailure(expression);
        DnsTextResult replacement = DnsPresentationNameText.ParseNameToken(tokens[start + 5], options);
        if (!replacement.Success || replacement.Name == null) return FromTextFailure(replacement);
        DnsNaptrData value = new DnsNaptrData();
        value.Order = (int)order.Value;
        value.Preference = (int)preference.Value;
        value.Flags = flags.Bytes;
        value.Services = services.Bytes;
        value.RegularExpression = expression.Bytes;
        value.Replacement = replacement.Name;
        DnsRecordData data = new DnsRecordData();
        data.Naptr = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseSignatureData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 9) return FieldCount("Signature RDATA requires nine fields.");
        DnsTextNumberResult covered = DnsMnemonicText.ParseRecordType(tokens[start], true);
        if (!covered.Success) return FromNumberFailure(covered);
        DnsTextNumberResult algorithm = ReadUnsigned8(tokens[start + 1], "algorithm");
        if (!algorithm.Success) return FromNumberFailure(algorithm);
        DnsTextNumberResult labels = ReadUnsigned8(tokens[start + 2], "labels");
        if (!labels.Success) return FromNumberFailure(labels);
        DnsTextNumberResult ttl = ReadUnsigned32(tokens[start + 3], "originalTimeToLive");
        if (!ttl.Success) return FromNumberFailure(ttl);
        DnsTextNumberResult expiration = ParseDnsTime(
            tokens[start + 4], "signatureExpiration");
        if (!expiration.Success) return FromNumberFailure(expiration);
        DnsTextNumberResult inception = ParseDnsTime(
            tokens[start + 5], "signatureInception");
        if (!inception.Success) return FromNumberFailure(inception);
        DnsTextNumberResult keyTag = ReadUnsigned16(tokens[start + 6], "keyTag");
        if (!keyTag.Success) return FromNumberFailure(keyTag);
        DnsTextResult signer = DnsPresentationNameText.ParseNameToken(tokens[start + 7], options);
        if (!signer.Success || signer.Name == null) return FromTextFailure(signer);
        DnsTextBytesResult signature = DnsTextBytes.ParseBase64(tokens[start + 8], "signature");
        if (!signature.Success) return FromBytesFailure(signature);
        DnsSignatureData value = new DnsSignatureData();
        value.TypeCovered = (DnsRecordType)covered.Value;
        value.TypeCoveredValue = (int)covered.Value;
        value.Algorithm = (DnsDnssecAlgorithm)algorithm.Value;
        value.AlgorithmValue = (int)algorithm.Value;
        value.Labels = (int)labels.Value;
        value.OriginalTimeToLive = (uint)ttl.Value;
        value.SignatureExpiration = (uint)expiration.Value;
        value.SignatureInception = (uint)inception.Value;
        value.KeyTag = (int)keyTag.Value;
        value.SignerName = signer.Name;
        value.Signature = signature.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Signature = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseCertData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 4) return FieldCount("CERT RDATA requires four fields.");
        DnsTextNumberResult type = ReadUnsigned16(tokens[start], "certificateType");
        if (!type.Success) return FromNumberFailure(type);
        DnsTextNumberResult keyTag = ReadUnsigned16(tokens[start + 1], "keyTag");
        if (!keyTag.Success) return FromNumberFailure(keyTag);
        DnsTextNumberResult algorithm = ReadUnsigned8(tokens[start + 2], "algorithm");
        if (!algorithm.Success) return FromNumberFailure(algorithm);
        DnsTextBytesResult certificate = DnsTextBytes.ParseBase64(tokens[start + 3], "certificate");
        if (!certificate.Success) return FromBytesFailure(certificate);
        DnsCertData value = new DnsCertData();
        value.CertificateType = (int)type.Value;
        value.KeyTag = (int)keyTag.Value;
        value.Algorithm = (DnsDnssecAlgorithm)algorithm.Value;
        value.AlgorithmValue = (int)algorithm.Value;
        value.Certificate = certificate.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Cert = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseAplData(List<string> tokens, int start)
    {
        if (tokens.Count <= start) return FieldCount("APL RDATA requires at least one prefix.");
        DnsAplData value = new DnsAplData();
        int index = start;
        while (index < tokens.Count)
        {
            string token = tokens[index];
            bool negated = token.Length > 0 && token[0] == '!';
            int itemStart = 0;
            if (negated) itemStart = 1;
            int colon = token.IndexOf(':', itemStart);
            int slash = token.LastIndexOf('/');
            if (colon < 0 || slash <= colon)
                return Failure("An APL item requires family:address/prefix syntax.");
            DnsTextNumberResult family = DnsTextNumber.ParseDecimal(
                token.Substring(itemStart, colon - itemStart), 65535, "addressFamily");
            if (!family.Success) return FromNumberFailure(family);
            DnsTextNumberResult prefix = DnsTextNumber.ParseDecimal(
                token.Substring(slash + 1), 255, "prefixLength");
            if (!prefix.Success) return FromNumberFailure(prefix);
            string addressText = token.Substring(colon + 1, slash - colon - 1);
            DnsTextBytesResult address;
            if (family.Value == 1 || family.Value == 2)
            {
                int length = 4;
                if (family.Value == 2) length = 16;
                address = DnsTextBytes.ParseAddress(addressText, length);
            }
            else
            {
                address = DnsTextBytes.ParseHex(addressText, "address");
            }

            if (!address.Success) return FromBytesFailure(address);
            DnsAplItem item = new DnsAplItem();
            item.AddressFamily = (int)family.Value;
            item.PrefixLength = (int)prefix.Value;
            item.Negated = negated;
            item.Address = TrimTrailingZeroBytes(address.Bytes);
            value.Items.Add(item);
            index = index + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.Apl = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseKeyData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 4) return FieldCount("Key RDATA requires four fields.");
        DnsTextNumberResult flags = ReadUnsigned16(tokens[start], "flags");
        if (!flags.Success) return FromNumberFailure(flags);
        DnsTextNumberResult protocol = ReadUnsigned8(tokens[start + 1], "protocol");
        if (!protocol.Success) return FromNumberFailure(protocol);
        DnsTextNumberResult algorithm = ReadUnsigned8(tokens[start + 2], "algorithm");
        if (!algorithm.Success) return FromNumberFailure(algorithm);
        DnsTextBytesResult key = DnsTextBytes.ParseBase64(tokens[start + 3], "publicKey");
        if (!key.Success) return FromBytesFailure(key);
        DnsKeyData value = new DnsKeyData();
        value.Flags = (int)flags.Value;
        value.Protocol = (int)protocol.Value;
        value.Algorithm = (DnsDnssecAlgorithm)algorithm.Value;
        value.AlgorithmValue = (int)algorithm.Value;
        value.PublicKey = key.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Key = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseDsData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 4) return FieldCount("DS RDATA requires four fields.");
        DnsTextNumberResult keyTag = ReadUnsigned16(tokens[start], "keyTag");
        if (!keyTag.Success) return FromNumberFailure(keyTag);
        DnsTextNumberResult algorithm = ReadUnsigned8(tokens[start + 1], "algorithm");
        if (!algorithm.Success) return FromNumberFailure(algorithm);
        DnsTextNumberResult digestType = ReadUnsigned8(tokens[start + 2], "digestType");
        if (!digestType.Success) return FromNumberFailure(digestType);
        DnsTextBytesResult digest = DnsTextBytes.ParseHex(tokens[start + 3], "digest");
        if (!digest.Success) return FromBytesFailure(digest);
        DnsDsData value = new DnsDsData();
        value.KeyTag = (int)keyTag.Value;
        value.Algorithm = (DnsDnssecAlgorithm)algorithm.Value;
        value.AlgorithmValue = (int)algorithm.Value;
        value.DigestAlgorithm = (DnsDigestAlgorithm)digestType.Value;
        value.DigestAlgorithmValue = (int)digestType.Value;
        value.Digest = digest.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Ds = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseSshfpData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 3) return FieldCount("SSHFP RDATA requires three fields.");
        DnsTextNumberResult algorithm = ReadUnsigned8(tokens[start], "algorithm");
        if (!algorithm.Success) return FromNumberFailure(algorithm);
        DnsTextNumberResult type = ReadUnsigned8(tokens[start + 1], "fingerprintType");
        if (!type.Success) return FromNumberFailure(type);
        DnsTextBytesResult fingerprint = DnsTextBytes.ParseHex(tokens[start + 2], "fingerprint");
        if (!fingerprint.Success) return FromBytesFailure(fingerprint);
        DnsSshfpData value = new DnsSshfpData();
        value.Algorithm = (int)algorithm.Value;
        value.FingerprintType = (int)type.Value;
        value.Fingerprint = fingerprint.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Sshfp = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseNsecData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count < start + 2) return FieldCount("NSEC RDATA requires a name and type list.");
        DnsTextResult name = DnsPresentationNameText.ParseNameToken(tokens[start], options);
        if (!name.Success || name.Name == null) return FromTextFailure(name);
        DnsNsecData value = new DnsNsecData();
        value.NextDomainName = name.Name;
        DnsRecordDataTextResult types = ReadRecordTypes(tokens, start + 1, value.RecordTypeValues);
        if (!types.Success) return types;
        DnsRecordData data = new DnsRecordData();
        data.Nsec = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseIpsecKeyData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 5) return FieldCount("IPSECKEY RDATA requires five fields.");
        DnsTextNumberResult precedence = ReadUnsigned8(tokens[start], "precedence");
        if (!precedence.Success) return FromNumberFailure(precedence);
        DnsTextNumberResult gatewayType = DnsTextNumber.ParseDecimal(tokens[start + 1], 3, "gatewayType");
        if (!gatewayType.Success) return FromNumberFailure(gatewayType);
        DnsTextNumberResult algorithm = ReadUnsigned8(tokens[start + 2], "algorithm");
        if (!algorithm.Success) return FromNumberFailure(algorithm);
        DnsIpsecKeyData value = new DnsIpsecKeyData();
        value.Precedence = (int)precedence.Value;
        value.GatewayType = (int)gatewayType.Value;
        value.Algorithm = (int)algorithm.Value;
        if (gatewayType.Value == 1 || gatewayType.Value == 2)
        {
            int length = 4;
            if (gatewayType.Value == 2) length = 16;
            DnsTextBytesResult address = DnsTextBytes.ParseAddress(tokens[start + 3], length);
            if (!address.Success) return FromBytesFailure(address);
            value.GatewayAddress = address.Bytes;
        }
        else if (gatewayType.Value == 3)
        {
            DnsTextResult name = DnsPresentationNameText.ParseNameToken(tokens[start + 3], options);
            if (!name.Success || name.Name == null) return FromTextFailure(name);
            value.GatewayName = name.Name;
        }

        DnsTextBytesResult key = DnsTextBytes.ParseBase64(tokens[start + 4], "publicKey");
        if (!key.Success) return FromBytesFailure(key);
        value.PublicKey = key.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.IpsecKey = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseNsec3Data(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count < start + 6) return FieldCount("NSEC3 RDATA requires hashing fields and a type list.");
        DnsTextNumberResult algorithm = ReadUnsigned8(tokens[start], "hashAlgorithm");
        if (!algorithm.Success) return FromNumberFailure(algorithm);
        DnsTextNumberResult flags = ReadUnsigned8(tokens[start + 1], "flags");
        if (!flags.Success) return FromNumberFailure(flags);
        DnsTextNumberResult iterations = ReadUnsigned16(tokens[start + 2], "iterations");
        if (!iterations.Success) return FromNumberFailure(iterations);
        DnsTextBytesResult salt = ReadOptionalHex(tokens[start + 3], "salt");
        if (!salt.Success) return FromBytesFailure(salt);
        DnsTextBytesResult next = ParseBase32Hex(tokens[start + 4]);
        if (!next.Success) return FromBytesFailure(next);
        DnsNsec3Data value = new DnsNsec3Data();
        value.HashAlgorithm = (DnsNsec3HashAlgorithm)algorithm.Value;
        value.HashAlgorithmValue = (int)algorithm.Value;
        value.Flags = (int)flags.Value;
        value.Iterations = (int)iterations.Value;
        value.Salt = salt.Bytes;
        value.NextHashedOwnerName = next.Bytes;
        DnsRecordDataTextResult types = ReadRecordTypes(tokens, start + 5, value.RecordTypeValues);
        if (!types.Success) return types;
        DnsRecordData data = new DnsRecordData();
        data.Nsec3 = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseNsec3ParamData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 4) return FieldCount("NSEC3PARAM RDATA requires four fields.");
        DnsTextNumberResult algorithm = ReadUnsigned8(tokens[start], "hashAlgorithm");
        if (!algorithm.Success) return FromNumberFailure(algorithm);
        DnsTextNumberResult flags = ReadUnsigned8(tokens[start + 1], "flags");
        if (!flags.Success) return FromNumberFailure(flags);
        DnsTextNumberResult iterations = ReadUnsigned16(tokens[start + 2], "iterations");
        if (!iterations.Success) return FromNumberFailure(iterations);
        DnsTextBytesResult salt = ReadOptionalHex(tokens[start + 3], "salt");
        if (!salt.Success) return FromBytesFailure(salt);
        DnsNsec3ParamData value = new DnsNsec3ParamData();
        value.HashAlgorithm = (DnsNsec3HashAlgorithm)algorithm.Value;
        value.HashAlgorithmValue = (int)algorithm.Value;
        value.Flags = (int)flags.Value;
        value.Iterations = (int)iterations.Value;
        value.Salt = salt.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Nsec3Param = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseDhcidData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 1) return FieldCount("DHCID RDATA requires one base64 field.");
        DnsTextBytesResult bytes = DnsTextBytes.ParseBase64(tokens[start], "dhcid");
        if (!bytes.Success) return FromBytesFailure(bytes);
        if (bytes.Bytes.Length < 3) return Failure("DHCID data requires a three-octet header.");
        DnsDhcidData value = new DnsDhcidData();
        value.IdentifierType = (bytes.Bytes[0] << 8) | bytes.Bytes[1];
        value.DigestType = bytes.Bytes[2];
        value.Digest = CopyBytes(bytes.Bytes, 3);
        DnsRecordData data = new DnsRecordData();
        data.Dhcid = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseTlsaData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 4) return FieldCount("TLSA RDATA requires four fields.");
        DnsTextNumberResult usage = ReadUnsigned8(tokens[start], "certificateUsage");
        if (!usage.Success) return FromNumberFailure(usage);
        DnsTextNumberResult selector = ReadUnsigned8(tokens[start + 1], "selector");
        if (!selector.Success) return FromNumberFailure(selector);
        DnsTextNumberResult matching = ReadUnsigned8(tokens[start + 2], "matchingType");
        if (!matching.Success) return FromNumberFailure(matching);
        DnsTextBytesResult association = DnsTextBytes.ParseHex(tokens[start + 3], "associationData");
        if (!association.Success) return FromBytesFailure(association);
        DnsTlsaData value = new DnsTlsaData();
        value.CertificateUsage = (int)usage.Value;
        value.Selector = (int)selector.Value;
        value.MatchingType = (int)matching.Value;
        value.AssociationData = association.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Tlsa = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseCsyncData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count < start + 3) return FieldCount("CSYNC RDATA requires a serial, flags, and types.");
        DnsTextNumberResult serial = ReadUnsigned32(tokens[start], "serial");
        if (!serial.Success) return FromNumberFailure(serial);
        DnsTextNumberResult flags = ReadUnsigned16(tokens[start + 1], "flags");
        if (!flags.Success) return FromNumberFailure(flags);
        DnsCsyncData value = new DnsCsyncData();
        value.Serial = (uint)serial.Value;
        value.Flags = (int)flags.Value;
        DnsRecordDataTextResult types = ReadRecordTypes(tokens, start + 2, value.RecordTypeValues);
        if (!types.Success) return types;
        DnsRecordData data = new DnsRecordData();
        data.Csync = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseHipData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count < start + 3) return FieldCount("HIP RDATA requires algorithm, HIT, and key fields.");
        DnsTextNumberResult algorithm = ReadUnsigned8(tokens[start], "publicKeyAlgorithm");
        if (!algorithm.Success) return FromNumberFailure(algorithm);
        DnsTextBytesResult hit = DnsTextBytes.ParseHex(tokens[start + 1], "hostIdentityTag");
        if (!hit.Success) return FromBytesFailure(hit);
        DnsTextBytesResult key = DnsTextBytes.ParseBase64(tokens[start + 2], "publicKey");
        if (!key.Success) return FromBytesFailure(key);
        DnsHipData value = new DnsHipData();
        value.PublicKeyAlgorithm = (int)algorithm.Value;
        value.HostIdentityTag = hit.Bytes;
        value.PublicKey = key.Bytes;
        int index = start + 3;
        while (index < tokens.Count)
        {
            DnsTextResult name = DnsPresentationNameText.ParseNameToken(tokens[index], options);
            if (!name.Success || name.Name == null) return FromTextFailure(name);
            value.RendezvousServers.Add(name.Name);
            index = index + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.Hip = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseZonemdData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 4) return FieldCount("ZONEMD RDATA requires four fields.");
        DnsTextNumberResult serial = ReadUnsigned32(tokens[start], "serial");
        if (!serial.Success) return FromNumberFailure(serial);
        DnsTextNumberResult scheme = ReadUnsigned8(tokens[start + 1], "scheme");
        if (!scheme.Success) return FromNumberFailure(scheme);
        DnsTextNumberResult hash = ReadUnsigned8(tokens[start + 2], "hashAlgorithm");
        if (!hash.Success) return FromNumberFailure(hash);
        DnsTextBytesResult digest = DnsTextBytes.ParseHex(tokens[start + 3], "digest");
        if (!digest.Success) return FromBytesFailure(digest);
        DnsZonemdData value = new DnsZonemdData();
        value.Serial = (uint)serial.Value;
        value.Scheme = (int)scheme.Value;
        value.HashAlgorithm = (int)hash.Value;
        value.Digest = digest.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Zonemd = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseDsyncData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 4) return FieldCount("DSYNC RDATA requires four fields.");
        DnsTextNumberResult type = DnsMnemonicText.ParseRecordType(tokens[start], true);
        if (!type.Success) return FromNumberFailure(type);
        DnsTextNumberResult scheme = ReadUnsigned8(tokens[start + 1], "scheme");
        if (!scheme.Success) return FromNumberFailure(scheme);
        DnsTextNumberResult port = ReadUnsigned16(tokens[start + 2], "port");
        if (!port.Success) return FromNumberFailure(port);
        DnsTextResult target = DnsPresentationNameText.ParseNameToken(tokens[start + 3], options);
        if (!target.Success || target.Name == null) return FromTextFailure(target);
        DnsDsyncData value = new DnsDsyncData();
        value.RecordType = (DnsRecordType)type.Value;
        value.RecordTypeValue = (int)type.Value;
        value.Scheme = (int)scheme.Value;
        value.Port = (int)port.Value;
        value.Target = target.Name;
        DnsRecordData data = new DnsRecordData();
        data.Dsync = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseSvcbData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count < start + 2) return FieldCount("SVCB RDATA requires priority and target fields.");
        DnsTextNumberResult priority = ReadUnsigned16(tokens[start], "priority");
        if (!priority.Success) return FromNumberFailure(priority);
        DnsTextResult target = DnsPresentationNameText.ParseNameToken(tokens[start + 1], options);
        if (!target.Success || target.Name == null) return FromTextFailure(target);
        DnsSvcbData value = new DnsSvcbData();
        value.Priority = (int)priority.Value;
        value.TargetName = target.Name;
        int index = start + 2;
        while (index < tokens.Count)
        {
            DnsRecordDataTextResult parameter = ParseSvcbParameter(
                tokens[index], value.Parameters);
            if (!parameter.Success) return parameter;
            index = index + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.Svcb = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseSvcbParameter(
        string token,
        List<DnsSvcbParameter> destination)
    {
        int equals = token.IndexOf('=');
        string keyText = token;
        string valueText = string.Empty;
        if (equals >= 0)
        {
            keyText = token.Substring(0, equals);
            valueText = token.Substring(equals + 1);
        }

        int key = ReadSvcbKey(keyText);
        if (key < 0) return Failure("The SVCB parameter key is invalid.");
        DnsSvcbParameter parameter = new DnsSvcbParameter();
        parameter.Key = (DnsSvcbParameterKey)key;
        parameter.KeyValue = key;
        if (valueText.StartsWith("\\#", System.StringComparison.Ordinal))
        {
            DnsTextBytesResult raw = DnsTextBytes.ParseHex(
                valueText.Substring(2), "parameterValue");
            if (!raw.Success) return FromBytesFailure(raw);
            parameter.RawValue = raw.Bytes;
            parameter.UseRawValue = true;
        }
        else if (key == 0)
        {
            DnsRecordDataTextResult list = ReadSvcbKeys(
                valueText, parameter.MandatoryKeyValues);
            if (!list.Success) return list;
        }
        else if (key == 1)
        {
            DnsTextBytesResult alpn = DnsTextBytes.ParseCharacterString(
                valueText, "alpn");
            if (!alpn.Success) return FromBytesFailure(alpn);
            SplitAlpnIdentifiers(alpn.Bytes, parameter.AlpnIdentifiers);
        }
        else if (key == 2)
        {
            if (equals >= 0) return Failure("no-default-alpn has no value.");
            parameter.NoDefaultAlpn = true;
        }
        else if (key == 3)
        {
            DnsTextNumberResult port = ReadUnsigned16(valueText, "port");
            if (!port.Success) return FromNumberFailure(port);
            parameter.HasPort = true;
            parameter.Port = (int)port.Value;
        }
        else if (key == 4 || key == 6)
        {
            DnsRecordDataTextResult addresses = ReadSvcbAddresses(
                valueText, key, parameter);
            if (!addresses.Success) return addresses;
        }
        else if (key == 5 || key == 8)
        {
            DnsTextBytesResult bytes = DnsTextBytes.ParseBase64(
                valueText, "parameterValue");
            if (!bytes.Success) return FromBytesFailure(bytes);
            if (key == 5) parameter.Ech = bytes.Bytes;
            if (key == 8) parameter.ObliviousHttp = bytes.Bytes;
        }
        else if (key == 7)
        {
            parameter.DohPath = valueText;
        }
        else
        {
            return Failure("Unknown SVCB parameters require the \\# hexadecimal form.");
        }

        int insertion = destination.Count;
        while (insertion > 0 && destination[insertion - 1].KeyValue > key)
            insertion = insertion - 1;
        if (insertion > 0 && destination[insertion - 1].KeyValue == key ||
            insertion < destination.Count && destination[insertion].KeyValue == key)
            return Failure("SVCB parameter keys must be unique.");
        destination.Insert(insertion, parameter);
        DnsRecordDataTextResult result = new DnsRecordDataTextResult();
        result.Success = true;
        return result;
    }

    static DnsRecordDataTextResult ParseIlNpData(
        List<string> tokens,
        int start,
        int recordTypeValue)
    {
        if (tokens.Count != start + 2) return FieldCount("ILNP RDATA requires preference and value fields.");
        DnsTextNumberResult preference = ReadUnsigned16(tokens[start], "preference");
        if (!preference.Success) return FromNumberFailure(preference);
        DnsTextBytesResult bytes;
        if (recordTypeValue == 105)
        {
            bytes = DnsTextBytes.ParseAddress(tokens[start + 1], 4);
        }
        else
        {
            bytes = DnsTextBytes.ParseHex(tokens[start + 1].Replace(":", string.Empty), "value");
        }

        if (!bytes.Success) return FromBytesFailure(bytes);
        DnsIlNpData value = new DnsIlNpData();
        value.Preference = (int)preference.Value;
        value.Value = bytes.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.IlNp = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseEuiData(
        List<string> tokens,
        int start,
        int recordTypeValue)
    {
        if (tokens.Count != start + 1) return FieldCount("EUI RDATA requires one field.");
        string hex = tokens[start].Replace("-", string.Empty);
        DnsTextBytesResult bytes = DnsTextBytes.ParseHex(hex, "address");
        if (!bytes.Success) return FromBytesFailure(bytes);
        int expected = 6;
        if (recordTypeValue == 109) expected = 8;
        if (bytes.Bytes.Length != expected) return Failure("The EUI address length is invalid.");
        DnsEuiData value = new DnsEuiData();
        value.Address = bytes.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Eui = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseUriData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 3) return FieldCount("URI RDATA requires three fields.");
        DnsTextNumberResult priority = ReadUnsigned16(tokens[start], "priority");
        if (!priority.Success) return FromNumberFailure(priority);
        DnsTextNumberResult weight = ReadUnsigned16(tokens[start + 1], "weight");
        if (!weight.Success) return FromNumberFailure(weight);
        DnsTextBytesResult target = DnsTextBytes.ParseCharacterString(tokens[start + 2], "target");
        if (!target.Success) return FromBytesFailure(target);
        DnsUriData value = new DnsUriData();
        value.Priority = (int)priority.Value;
        value.Weight = (int)weight.Value;
        value.Target = target.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Uri = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseCaaData(List<string> tokens, int start)
    {
        if (tokens.Count != start + 3) return FieldCount("CAA RDATA requires three fields.");
        DnsTextNumberResult flags = ReadUnsigned8(tokens[start], "flags");
        if (!flags.Success) return FromNumberFailure(flags);
        DnsTextBytesResult tag = DnsTextBytes.ParseCharacterString(tokens[start + 1], "tag");
        if (!tag.Success) return FromBytesFailure(tag);
        DnsTextBytesResult valueText = DnsTextBytes.ParseCharacterString(tokens[start + 2], "value");
        if (!valueText.Success) return FromBytesFailure(valueText);
        DnsCaaData value = new DnsCaaData();
        value.Flags = (int)flags.Value;
        value.Tag = tag.Bytes;
        value.Value = valueText.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Caa = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseAmtRelayData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 4) return FieldCount("AMTRELAY RDATA requires four fields.");
        DnsTextNumberResult precedence = ReadUnsigned8(tokens[start], "precedence");
        if (!precedence.Success) return FromNumberFailure(precedence);
        DnsTextNumberResult optional = DnsTextNumber.ParseDecimal(tokens[start + 1], 1, "discoveryOptional");
        if (!optional.Success) return FromNumberFailure(optional);
        DnsTextNumberResult relayType = DnsTextNumber.ParseDecimal(tokens[start + 2], 3, "relayType");
        if (!relayType.Success) return FromNumberFailure(relayType);
        DnsAmtRelayData value = new DnsAmtRelayData();
        value.Precedence = (int)precedence.Value;
        value.DiscoveryOptional = optional.Value == 1;
        value.RelayType = (int)relayType.Value;
        if (relayType.Value == 1 || relayType.Value == 2)
        {
            int length = 4;
            if (relayType.Value == 2) length = 16;
            DnsTextBytesResult address = DnsTextBytes.ParseAddress(tokens[start + 3], length);
            if (!address.Success) return FromBytesFailure(address);
            value.RelayAddress = address.Bytes;
        }
        else if (relayType.Value == 3)
        {
            DnsTextResult name = DnsPresentationNameText.ParseNameToken(tokens[start + 3], options);
            if (!name.Success || name.Name == null) return FromTextFailure(name);
            value.RelayName = name.Name;
        }

        DnsRecordData data = new DnsRecordData();
        data.AmtRelay = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseResolverInformationData(List<string> tokens, int start)
    {
        if (tokens.Count <= start) return FieldCount("RESINFO RDATA requires at least one value.");
        DnsResolverInformationData value = new DnsResolverInformationData();
        int index = start;
        while (index < tokens.Count)
        {
            DnsTextBytesResult item = DnsTextBytes.ParseCharacterString(tokens[index], "value");
            if (!item.Success) return FromBytesFailure(item);
            value.Values.Add(item.Bytes);
            index = index + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.ResolverInformation = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseTkeyData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 7) return FieldCount("TKEY RDATA requires seven fields.");
        DnsTextResult name = DnsPresentationNameText.ParseNameToken(tokens[start], options);
        if (!name.Success || name.Name == null) return FromTextFailure(name);
        DnsTextNumberResult inception = ReadUnsigned32(tokens[start + 1], "inception");
        if (!inception.Success) return FromNumberFailure(inception);
        DnsTextNumberResult expiration = ReadUnsigned32(tokens[start + 2], "expiration");
        if (!expiration.Success) return FromNumberFailure(expiration);
        DnsTextNumberResult mode = ReadUnsigned16(tokens[start + 3], "mode");
        if (!mode.Success) return FromNumberFailure(mode);
        DnsTextNumberResult error = ReadUnsigned16(tokens[start + 4], "error");
        if (!error.Success) return FromNumberFailure(error);
        DnsTextBytesResult key = DnsTextBytes.ParseBase64(tokens[start + 5], "key");
        if (!key.Success) return FromBytesFailure(key);
        DnsTextBytesResult other = DnsTextBytes.ParseBase64(tokens[start + 6], "otherData");
        if (!other.Success) return FromBytesFailure(other);
        DnsTkeyData value = new DnsTkeyData();
        value.AlgorithmName = name.Name;
        value.Inception = (uint)inception.Value;
        value.Expiration = (uint)expiration.Value;
        value.Mode = (int)mode.Value;
        value.Error = (int)error.Value;
        value.Key = key.Bytes;
        value.OtherData = other.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Tkey = value;
        return Success(data);
    }

    static DnsRecordDataTextResult ParseTsigData(
        List<string> tokens,
        int start,
        DnsTextOptions options)
    {
        if (tokens.Count != start + 7) return FieldCount("TSIG RDATA requires seven fields.");
        DnsTextResult name = DnsPresentationNameText.ParseNameToken(tokens[start], options);
        if (!name.Success || name.Name == null) return FromTextFailure(name);
        DnsTextNumberResult signed = DnsTextNumber.ParseDecimal(tokens[start + 1], 281474976710655, "timeSigned");
        if (!signed.Success) return FromNumberFailure(signed);
        DnsTextNumberResult fudge = ReadUnsigned16(tokens[start + 2], "fudge");
        if (!fudge.Success) return FromNumberFailure(fudge);
        DnsTextBytesResult mac = DnsTextBytes.ParseBase64(tokens[start + 3], "mac");
        if (!mac.Success) return FromBytesFailure(mac);
        DnsTextNumberResult identifier = ReadUnsigned16(tokens[start + 4], "originalIdentifier");
        if (!identifier.Success) return FromNumberFailure(identifier);
        DnsTextNumberResult error = ReadUnsigned16(tokens[start + 5], "error");
        if (!error.Success) return FromNumberFailure(error);
        DnsTextBytesResult other = DnsTextBytes.ParseBase64(tokens[start + 6], "otherData");
        if (!other.Success) return FromBytesFailure(other);
        DnsTsigData value = new DnsTsigData();
        value.AlgorithmName = name.Name;
        value.TimeSigned = (ulong)signed.Value;
        value.Fudge = (int)fudge.Value;
        value.Mac = mac.Bytes;
        value.OriginalIdentifier = (int)identifier.Value;
        value.Error = (DnsTsigError)error.Value;
        value.ErrorValue = (int)error.Value;
        value.OtherData = other.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Tsig = value;
        return Success(data);
    }

    static DnsTextNumberResult ParseLocCoordinate(
        string degreesText,
        string minutesText,
        string secondsText,
        string directionText,
        bool latitude)
    {
        int maximumDegrees = 180;
        if (latitude) maximumDegrees = 90;
        DnsTextNumberResult degrees = DnsTextNumber.ParseDecimal(
            degreesText, maximumDegrees, "coordinateDegrees");
        if (!degrees.Success) return degrees;
        DnsTextNumberResult minutes = DnsTextNumber.ParseDecimal(
            minutesText, 59, "coordinateMinutes");
        if (!minutes.Success) return minutes;
        DnsTextNumberResult seconds = ParseScaledDecimal(
            secondsText, 3, false, "coordinateSeconds");
        if (!seconds.Success) return seconds;
        if (seconds.Value >= 60000 ||
            degrees.Value == maximumDegrees &&
                (minutes.Value != 0 || seconds.Value != 0))
            return DnsTextNumber.Failure(
                "coordinate", "The LOC coordinate is outside its range.", 0);
        string direction = directionText.ToUpperInvariant();
        bool negative = direction == "S" || direction == "W";
        if (latitude && direction != "N" && direction != "S" ||
            !latitude && direction != "E" && direction != "W")
            return DnsTextNumber.Failure(
                "coordinate", "The LOC coordinate direction is invalid.", 0);
        long offset = (degrees.Value * 3600000) +
            (minutes.Value * 60000) + seconds.Value;
        long value = 2147483648 + offset;
        if (negative) value = 2147483648 - offset;
        DnsTextNumberResult result = new DnsTextNumberResult();
        result.Success = true;
        result.Value = value;
        return result;
    }

    static DnsTextNumberResult ParseScaledDecimal(
        string text,
        int fractionalDigits,
        bool allowNegative,
        string field)
    {
        string selected = text;
        if (selected.EndsWith("m", System.StringComparison.OrdinalIgnoreCase))
            selected = selected.Substring(0, selected.Length - 1);
        bool negative = selected.StartsWith("-", System.StringComparison.Ordinal);
        if (negative)
        {
            if (!allowNegative)
                return DnsTextNumber.Failure(field, "The value cannot be negative.", 0);
            selected = selected.Substring(1);
        }

        int point = selected.IndexOf('.');
        string integerText = selected;
        string fractionText = string.Empty;
        if (point >= 0)
        {
            integerText = selected.Substring(0, point);
            fractionText = selected.Substring(point + 1);
            if (fractionText.Length > fractionalDigits)
                return DnsTextNumber.Failure(field, "The value has too many fractional digits.", point);
        }

        DnsTextNumberResult integer = DnsTextNumber.ParseDecimal(
            integerText, 1000000000, field);
        if (!integer.Success) return integer;
        long scale = 1;
        int index = 0;
        while (index < fractionalDigits)
        {
            scale = scale * 10;
            index = index + 1;
        }

        long fraction = 0;
        if (fractionText.Length > 0)
        {
            DnsTextNumberResult parsedFraction = DnsTextNumber.ParseDecimal(
                fractionText, scale - 1, field);
            if (!parsedFraction.Success) return parsedFraction;
            fraction = parsedFraction.Value;
            index = fractionText.Length;
            while (index < fractionalDigits)
            {
                fraction = fraction * 10;
                index = index + 1;
            }
        }

        long value = (integer.Value * scale) + fraction;
        if (negative) value = -value;
        DnsTextNumberResult result = new DnsTextNumberResult();
        result.Success = true;
        result.Value = value;
        return result;
    }

    static int EncodeLocPrecision(long centimeters)
    {
        if (centimeters < 0) return -1;
        int exponent = 0;
        while (centimeters > 9 && centimeters % 10 == 0 && exponent < 9)
        {
            centimeters = centimeters / 10;
            exponent = exponent + 1;
        }

        if (centimeters > 9) return -1;
        return ((int)centimeters << 4) | exponent;
    }

    static byte[] TrimTrailingZeroBytes(byte[] source)
    {
        int length = source.Length;
        while (length > 0 && source[length - 1] == 0) length = length - 1;
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    static int ReadSvcbKey(string text)
    {
        string normalized = text.ToLowerInvariant();
        switch (normalized)
        {
            case "mandatory": return 0;
            case "alpn": return 1;
            case "no-default-alpn": return 2;
            case "nodefaultalpn": return 2;
            case "port": return 3;
            case "ipv4hint": return 4;
            case "ech": return 5;
            case "ipv6hint": return 6;
            case "dohpath": return 7;
            case "ohttp": return 8;
            case "oblivioushttp": return 8;
        }

        if (!normalized.StartsWith("key", System.StringComparison.Ordinal) ||
            normalized.Length <= 3) return -1;
        DnsTextNumberResult number = DnsTextNumber.ParseDecimal(
            normalized.Substring(3), 65535, "parameterKey");
        if (!number.Success) return -1;
        return (int)number.Value;
    }

    static DnsRecordDataTextResult ReadSvcbKeys(
        string text,
        List<int> destination)
    {
        List<string> values = SplitCommaText(text);
        int index = 0;
        while (index < values.Count)
        {
            int key = ReadSvcbKey(values[index]);
            if (key < 0) return Failure("A mandatory SVCB key is invalid.");
            destination.Add(key);
            index = index + 1;
        }

        DnsRecordDataTextResult result = new DnsRecordDataTextResult();
        result.Success = true;
        return result;
    }

    static void SplitAlpnIdentifiers(
        byte[] source,
        List<byte[]> destination)
    {
        int start = 0;
        int index = 0;
        while (index <= source.Length)
        {
            if (index == source.Length || source[index] == ',')
            {
                byte[] value = new byte[index - start];
                int valueIndex = 0;
                while (valueIndex < value.Length)
                {
                    value[valueIndex] = source[start + valueIndex];
                    valueIndex = valueIndex + 1;
                }

                destination.Add(value);
                start = index + 1;
            }

            index = index + 1;
        }
    }

    static DnsRecordDataTextResult ReadSvcbAddresses(
        string text,
        int key,
        DnsSvcbParameter parameter)
    {
        List<string> values = SplitCommaText(text);
        int length = 4;
        if (key == 6) length = 16;
        int index = 0;
        while (index < values.Count)
        {
            DnsTextBytesResult address = DnsTextBytes.ParseAddress(
                values[index], length);
            if (!address.Success) return FromBytesFailure(address);
            if (key == 4) parameter.Ipv4Hints.Add(address.Bytes);
            if (key == 6) parameter.Ipv6Hints.Add(address.Bytes);
            index = index + 1;
        }

        DnsRecordDataTextResult result = new DnsRecordDataTextResult();
        result.Success = true;
        return result;
    }

    static List<string> SplitCommaText(string text)
    {
        List<string> values = new List<string>();
        int start = 0;
        int index = 0;
        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == ',')
            {
                values.Add(text.Substring(start, index - start));
                start = index + 1;
            }

            index = index + 1;
        }

        return values;
    }

    static DnsRecordDataTextResult ReadRecordTypes(
        List<string> tokens,
        int start,
        List<int> destination)
    {
        int index = start;
        while (index < tokens.Count)
        {
            DnsTextNumberResult type = DnsMnemonicText.ParseRecordType(tokens[index], true);
            if (!type.Success) return FromNumberFailure(type);
            destination.Add((int)type.Value);
            index = index + 1;
        }

        DnsRecordDataTextResult result = new DnsRecordDataTextResult();
        result.Success = true;
        return result;
    }

    static DnsTextBytesResult ParseBase32Hex(string text)
    {
        List<byte> bytes = new List<byte>();
        int accumulator = 0;
        int bitCount = 0;
        int index = 0;
        while (index < text.Length)
        {
            int value = ReadBase32HexValue(text[index]);
            if (value < 0)
            {
                return BytesFailure("nextHashedOwnerName", "The value is not base32hex text.");
            }

            accumulator = (accumulator << 5) | value;
            bitCount = bitCount + 5;
            if (bitCount >= 8)
            {
                bitCount = bitCount - 8;
                bytes.Add((byte)(accumulator >> bitCount));
                accumulator = accumulator & ((1 << bitCount) - 1);
            }

            index = index + 1;
        }

        DnsTextBytesResult result = new DnsTextBytesResult();
        result.Success = true;
        result.Bytes = bytes.ToArray();
        return result;
    }

    static int ReadBase32HexValue(char value)
    {
        char normalized = char.ToUpperInvariant(value);
        if (normalized >= '0' && normalized <= '9') return normalized - '0';
        if (normalized >= 'A' && normalized <= 'V') return normalized - 'A' + 10;
        return -1;
    }

    static DnsTextBytesResult ReadOptionalHex(string text, string field)
    {
        if (text == "-")
        {
            DnsTextBytesResult empty = new DnsTextBytesResult();
            empty.Success = true;
            return empty;
        }

        return DnsTextBytes.ParseHex(text, field);
    }

    static DnsTextNumberResult ReadUnsigned8(string text, string field)
    {
        return DnsTextNumber.ParseDecimal(text, 255, field);
    }

    static DnsTextNumberResult ReadUnsigned16(string text, string field)
    {
        return DnsTextNumber.ParseDecimal(text, 65535, field);
    }

    static DnsTextNumberResult ReadUnsigned32(string text, string field)
    {
        return DnsTextNumber.ParseDecimal(text, uint.MaxValue, field);
    }

    static DnsTextNumberResult ParseDnsTime(string text, string field)
    {
        if (text.Length != 14)
        {
            return ReadUnsigned32(text, field);
        }

        DnsTextNumberResult year = DnsTextNumber.ParseDecimal(
            text.Substring(0, 4), 9999, field);
        DnsTextNumberResult month = DnsTextNumber.ParseDecimal(
            text.Substring(4, 2), 12, field);
        DnsTextNumberResult day = DnsTextNumber.ParseDecimal(
            text.Substring(6, 2), 31, field);
        DnsTextNumberResult hour = DnsTextNumber.ParseDecimal(
            text.Substring(8, 2), 23, field);
        DnsTextNumberResult minute = DnsTextNumber.ParseDecimal(
            text.Substring(10, 2), 59, field);
        DnsTextNumberResult second = DnsTextNumber.ParseDecimal(
            text.Substring(12, 2), 59, field);
        if (!year.Success || !month.Success || !day.Success ||
            !hour.Success || !minute.Success || !second.Success ||
            year.Value < 1970 || month.Value < 1 || day.Value < 1)
        {
            return DnsTextNumber.Failure(
                field, "The DNS timestamp is not a valid UTC date.", 0);
        }

        int maximumDay = DaysInMonth((int)year.Value, (int)month.Value);
        if (day.Value > maximumDay)
        {
            return DnsTextNumber.Failure(
                field, "The DNS timestamp day is invalid for its month.", 6);
        }

        long days = 0;
        int currentYear = 1970;
        while (currentYear < year.Value)
        {
            days = days + 365;
            if (IsLeapYear(currentYear)) days = days + 1;
            currentYear = currentYear + 1;
        }

        int currentMonth = 1;
        while (currentMonth < month.Value)
        {
            days = days + DaysInMonth((int)year.Value, currentMonth);
            currentMonth = currentMonth + 1;
        }

        days = days + day.Value - 1;
        long value = (days * 86400) + (hour.Value * 3600) +
            (minute.Value * 60) + second.Value;
        if (value > uint.MaxValue)
        {
            return DnsTextNumber.Failure(
                field, "The DNS timestamp exceeds serial-time range.", 0);
        }

        DnsTextNumberResult result = new DnsTextNumberResult();
        result.Success = true;
        result.Value = value;
        return result;
    }

    static int DaysInMonth(int year, int month)
    {
        switch (month)
        {
            case 2:
                if (IsLeapYear(year)) return 29;
                return 28;
            case 4: return 30;
            case 6: return 30;
            case 9: return 30;
            case 11: return 30;
            default: return 31;
        }
    }

    static bool IsLeapYear(int year)
    {
        if (year % 400 == 0) return true;
        if (year % 100 == 0) return false;
        return year % 4 == 0;
    }

    static byte[] CopyBytes(byte[] source, int start)
    {
        byte[] bytes = new byte[source.Length - start];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = source[start + index];
            index = index + 1;
        }

        return bytes;
    }

    static DnsRecordDataTextResult Success(DnsRecordData data)
    {
        DnsRecordDataTextResult result = new DnsRecordDataTextResult();
        result.Success = true;
        result.Data = data;
        return result;
    }

    static DnsRecordDataTextResult FieldCount(string reason)
    {
        return Failure(reason);
    }

    static DnsRecordDataTextResult Failure(string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidText;
        diagnostic.Field = "recordData";
        diagnostic.Reason = reason;
        DnsRecordDataTextResult result = new DnsRecordDataTextResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    static DnsTextBytesResult BytesFailure(string field, string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidText;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        DnsTextBytesResult result = new DnsTextBytesResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    static DnsRecordDataTextResult FromNumberFailure(DnsTextNumberResult value)
    {
        DnsRecordDataTextResult result = new DnsRecordDataTextResult();
        int index = 0;
        while (index < value.Diagnostics.Count)
        {
            result.Diagnostics.Add(value.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static DnsRecordDataTextResult FromBytesFailure(DnsTextBytesResult value)
    {
        DnsRecordDataTextResult result = new DnsRecordDataTextResult();
        int index = 0;
        while (index < value.Diagnostics.Count)
        {
            result.Diagnostics.Add(value.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static DnsRecordDataTextResult FromTextFailure(DnsTextResult value)
    {
        DnsRecordDataTextResult result = new DnsRecordDataTextResult();
        int index = 0;
        while (index < value.Diagnostics.Count)
        {
            result.Diagnostics.Add(value.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }
}
