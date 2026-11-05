using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace dns;

internal static class DnsTypedRecordDataText
{
    internal static DnsTextResult FormatRecordData(
        int recordTypeValue,
        DnsRecordData data,
        DnsTextOptions options)
    {
        if (data == null || options == null)
        {
            return Missing("Typed record data and text options are required.");
        }

        RecordDataModel model = RecordDataModelCatalog.ReadRecordDataModel(
            recordTypeValue);
        switch (model)
        {
            case RecordDataModel.Address:
                return FormatAddressData(data.Address, recordTypeValue);
            case RecordDataModel.Name:
                return FormatNameData(data.Name, options);
            case RecordDataModel.TwoName:
                return FormatTwoNameData(data.TwoName, options);
            case RecordDataModel.PreferenceName:
                return FormatPreferenceNameData(data.PreferenceName, options);
            case RecordDataModel.CharacterStrings:
                return FormatCharacterStringsData(data.CharacterStrings);
            case RecordDataModel.FixedBytes:
                return FormatFixedBytesData(data.FixedBytes, recordTypeValue);
            case RecordDataModel.Soa:
                return FormatSoaData(data.Soa, options);
            case RecordDataModel.Wks:
                return FormatWksData(data.Wks);
            case RecordDataModel.A6:
                return FormatA6Data(data.A6, options);
            case RecordDataModel.Px:
                return FormatPxData(data.Px, options);
            case RecordDataModel.Loc:
                return FormatLocData(data.Loc);
            case RecordDataModel.Srv:
                return FormatSrvData(data.Srv, options);
            case RecordDataModel.Naptr:
                return FormatNaptrData(data.Naptr, options);
            case RecordDataModel.Cert:
                return FormatCertData(data.Cert);
            case RecordDataModel.Apl:
                return FormatAplData(data.Apl);
            case RecordDataModel.Signature:
                return FormatSignatureData(data.Signature, options);
            case RecordDataModel.Key:
                return FormatKeyData(data.Key);
            case RecordDataModel.Ds:
                return FormatDsData(data.Ds);
            case RecordDataModel.Sshfp:
                return FormatSshfpData(data.Sshfp);
            case RecordDataModel.IpsecKey:
                return FormatIpsecKeyData(data.IpsecKey, options);
            case RecordDataModel.Nsec:
                return FormatNsecData(data.Nsec, options);
            case RecordDataModel.Nsec3:
                return FormatNsec3Data(data.Nsec3, options);
            case RecordDataModel.Nsec3Param:
                return FormatNsec3ParamData(data.Nsec3Param);
            case RecordDataModel.Dhcid:
                return FormatDhcidData(data.Dhcid);
            case RecordDataModel.Tlsa:
                return FormatTlsaData(data.Tlsa);
            case RecordDataModel.Hip:
                return FormatHipData(data.Hip, options);
            case RecordDataModel.Csync:
                return FormatCsyncData(data.Csync, options);
            case RecordDataModel.Zonemd:
                return FormatZonemdData(data.Zonemd);
            case RecordDataModel.Svcb:
                return FormatSvcbData(data.Svcb, options);
            case RecordDataModel.Dsync:
                return FormatDsyncData(data.Dsync, options);
            case RecordDataModel.IlNp:
                return FormatIlNpData(data.IlNp, recordTypeValue);
            case RecordDataModel.Eui:
                return FormatEuiData(data.Eui);
            case RecordDataModel.Uri:
                return FormatUriData(data.Uri);
            case RecordDataModel.Caa:
                return FormatCaaData(data.Caa);
            case RecordDataModel.AmtRelay:
                return FormatAmtRelayData(data.AmtRelay, options);
            case RecordDataModel.ResolverInformation:
                return FormatResolverInformationData(data.ResolverInformation);
            case RecordDataModel.Tkey:
                return FormatTkeyData(data.Tkey, options);
            case RecordDataModel.Tsig:
                return FormatTsigData(data.Tsig, options);
            default:
                return Missing("The record type has no typed presentation schema.");
        }
    }

    static DnsTextResult FormatAddressData(
        DnsAddressData? data,
        int recordTypeValue)
    {
        if (data == null)
        {
            return Missing("Address data is required.");
        }

        int expectedLength = 4;
        if (recordTypeValue == 28)
        {
            expectedLength = 16;
        }

        if (data.Address.Length != expectedLength)
        {
            return Missing("The address length does not match the record type.");
        }

        return DnsTextOperations.SuccessText(
            DnsTextEncoding.FormatAddress(data.Address));
    }

    static DnsTextResult FormatNameData(
        DnsNameData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("Name data is required.");
        return DnsPresentationNameText.FormatName(data.Name, options);
    }

    static DnsTextResult FormatTwoNameData(
        DnsTwoNameData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("Two-name data is required.");
        DnsTextResult first = DnsPresentationNameText.FormatName(
            data.FirstName, options);
        if (!first.Success) return first;
        DnsTextResult second = DnsPresentationNameText.FormatName(
            data.SecondName, options);
        if (!second.Success) return second;
        return DnsTextOperations.SuccessText(first.Text + " " + second.Text);
    }

    static DnsTextResult FormatPreferenceNameData(
        DnsPreferenceNameData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("Preference-and-name data is required.");
        DnsTextResult name = DnsPresentationNameText.FormatName(
            data.Name, options);
        if (!name.Success) return name;
        return DnsTextOperations.SuccessText(
            DnsTextEncoding.FormatSigned(data.Preference) + " " + name.Text);
    }

    static DnsTextResult FormatCharacterStringsData(
        DnsCharacterStringsData? data)
    {
        if (data == null) return Missing("Character-string data is required.");
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < data.Strings.Count)
        {
            if (index > 0) builder.Append(' ');
            builder.Append(DnsTextEncoding.FormatCharacterString(
                data.Strings[index]));
            index = index + 1;
        }

        return DnsTextOperations.SuccessText(builder.ToString());
    }

    static DnsTextResult FormatFixedBytesData(
        DnsFixedBytesData? data,
        int recordTypeValue)
    {
        if (data == null) return Missing("Fixed-byte data is required.");
        if (recordTypeValue == 61)
        {
            return DnsTextOperations.SuccessText(
                DnsTextEncoding.FormatBase64(data.Bytes));
        }

        return DnsTextOperations.SuccessText(
            DnsTextEncoding.FormatHex(data.Bytes));
    }

    static DnsTextResult FormatSoaData(
        DnsSoaData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("SOA data is required.");
        DnsTextResult primary = DnsPresentationNameText.FormatName(
            data.PrimaryNameServer, options);
        if (!primary.Success) return primary;
        DnsTextResult mailbox = DnsPresentationNameText.FormatName(
            data.ResponsibleMailbox, options);
        if (!mailbox.Success) return mailbox;
        string text = primary.Text + " " + mailbox.Text + " " +
            DnsTextEncoding.FormatUnsigned(data.Serial) + " " +
            DnsTextEncoding.FormatUnsigned(data.Refresh) + " " +
            DnsTextEncoding.FormatUnsigned(data.Retry) + " " +
            DnsTextEncoding.FormatUnsigned(data.Expire) + " " +
            DnsTextEncoding.FormatUnsigned(data.Minimum);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatWksData(DnsWksData? data)
    {
        if (data == null || data.Address.Length != 4)
        {
            return Missing("WKS data requires an IPv4 address.");
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(DnsTextEncoding.FormatAddress(data.Address));
        builder.Append(' ');
        builder.Append(DnsTextEncoding.FormatSigned(data.Protocol));
        int byteIndex = 0;
        while (byteIndex < data.PortBitmap.Length)
        {
            int bitIndex = 0;
            while (bitIndex < 8)
            {
                if ((data.PortBitmap[byteIndex] & (128 >> bitIndex)) != 0)
                {
                    builder.Append(' ');
                    builder.Append(DnsTextEncoding.FormatSigned(
                        (byteIndex * 8) + bitIndex));
                }

                bitIndex = bitIndex + 1;
            }

            byteIndex = byteIndex + 1;
        }

        return DnsTextOperations.SuccessText(builder.ToString());
    }

    static DnsTextResult FormatA6Data(
        DnsA6Data? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("A6 data is required.");
        byte[] address = new byte[16];
        if (data.AddressSuffix.Length > 16)
        {
            return Missing("The A6 address suffix is too long.");
        }

        int destination = 16 - data.AddressSuffix.Length;
        data.AddressSuffix.CopyTo(address, destination);
        string text = DnsTextEncoding.FormatSigned(data.PrefixLength) + " " +
            DnsTextEncoding.FormatAddress(address);
        if (data.PrefixLength > 0)
        {
            if (data.PrefixName == null)
            {
                return Missing("A nonzero A6 prefix requires a prefix name.");
            }

            DnsTextResult name = DnsPresentationNameText.FormatName(
                data.PrefixName, options);
            if (!name.Success) return name;
            text = text + " " + name.Text;
        }

        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatPxData(
        DnsPxData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("PX data is required.");
        DnsTextResult map822 = DnsPresentationNameText.FormatName(
            data.Map822, options);
        if (!map822.Success) return map822;
        DnsTextResult mapX400 = DnsPresentationNameText.FormatName(
            data.MapX400, options);
        if (!mapX400.Success) return mapX400;
        return DnsTextOperations.SuccessText(
            DnsTextEncoding.FormatSigned(data.Preference) + " " +
            map822.Text + " " + mapX400.Text);
    }

    static DnsTextResult FormatLocData(DnsLocData? data)
    {
        if (data == null) return Missing("LOC data is required.");
        if (data.Version != 0)
        {
            return Missing("Only LOC version zero has human-readable text.");
        }

        string latitude = FormatLocCoordinate(data.Latitude, true);
        string longitude = FormatLocCoordinate(data.Longitude, false);
        decimal altitude = ((decimal)data.Altitude - 10000000m) / 100m;
        decimal size = DecodeLocPrecision(data.Size);
        decimal horizontal = DecodeLocPrecision(data.HorizontalPrecision);
        decimal vertical = DecodeLocPrecision(data.VerticalPrecision);
        string text = latitude + " " + longitude + " " +
            altitude.ToString("0.00", CultureInfo.InvariantCulture) + "m " +
            size.ToString("0.00", CultureInfo.InvariantCulture) + "m " +
            horizontal.ToString("0.00", CultureInfo.InvariantCulture) + "m " +
            vertical.ToString("0.00", CultureInfo.InvariantCulture) + "m";
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatSrvData(
        DnsSrvData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("SRV data is required.");
        DnsTextResult target = DnsPresentationNameText.FormatName(
            data.Target, options);
        if (!target.Success) return target;
        string text = DnsTextEncoding.FormatSigned(data.Priority) + " " +
            DnsTextEncoding.FormatSigned(data.Weight) + " " +
            DnsTextEncoding.FormatSigned(data.Port) + " " + target.Text;
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatNaptrData(
        DnsNaptrData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("NAPTR data is required.");
        DnsTextResult replacement = DnsPresentationNameText.FormatName(
            data.Replacement, options);
        if (!replacement.Success) return replacement;
        string text = DnsTextEncoding.FormatSigned(data.Order) + " " +
            DnsTextEncoding.FormatSigned(data.Preference) + " " +
            DnsTextEncoding.FormatCharacterString(data.Flags) + " " +
            DnsTextEncoding.FormatCharacterString(data.Services) + " " +
            DnsTextEncoding.FormatCharacterString(data.RegularExpression) + " " +
            replacement.Text;
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatCertData(DnsCertData? data)
    {
        if (data == null) return Missing("CERT data is required.");
        int algorithm = DnsHeaderCodec.ResolveCode(
            data.AlgorithmValue, (int)data.Algorithm);
        string text = DnsTextEncoding.FormatSigned(data.CertificateType) + " " +
            DnsTextEncoding.FormatSigned(data.KeyTag) + " " +
            DnsTextEncoding.FormatSigned(algorithm) + " " +
            DnsTextEncoding.FormatBase64(data.Certificate);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatAplData(DnsAplData? data)
    {
        if (data == null) return Missing("APL data is required.");
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < data.Items.Count)
        {
            DnsAplItem item = data.Items[index];
            if (index > 0) builder.Append(' ');
            if (item.Negated) builder.Append('!');
            builder.Append(DnsTextEncoding.FormatSigned(item.AddressFamily));
            builder.Append(':');
            builder.Append(FormatAplAddress(item));
            builder.Append('/');
            builder.Append(DnsTextEncoding.FormatSigned(item.PrefixLength));
            index = index + 1;
        }

        return DnsTextOperations.SuccessText(builder.ToString());
    }

    static DnsTextResult FormatSignatureData(
        DnsSignatureData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("Signature data is required.");
        DnsTextResult signer = DnsPresentationNameText.FormatName(
            data.SignerName, options);
        if (!signer.Success) return signer;
        int covered = DnsHeaderCodec.ResolveCode(
            data.TypeCoveredValue, (int)data.TypeCovered);
        int algorithm = DnsHeaderCodec.ResolveCode(
            data.AlgorithmValue, (int)data.Algorithm);
        string text = DnsMnemonicText.FormatRecordType(covered, true) + " " +
            DnsTextEncoding.FormatSigned(algorithm) + " " +
            DnsTextEncoding.FormatSigned(data.Labels) + " " +
            DnsTextEncoding.FormatUnsigned(data.OriginalTimeToLive) + " " +
            DnsTextEncoding.FormatDnsTime(data.SignatureExpiration) + " " +
            DnsTextEncoding.FormatDnsTime(data.SignatureInception) + " " +
            DnsTextEncoding.FormatSigned(data.KeyTag) + " " + signer.Text + " " +
            DnsTextEncoding.FormatBase64(data.Signature);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatKeyData(DnsKeyData? data)
    {
        if (data == null) return Missing("Key data is required.");
        int algorithm = DnsHeaderCodec.ResolveCode(
            data.AlgorithmValue, (int)data.Algorithm);
        string text = DnsTextEncoding.FormatSigned(data.Flags) + " " +
            DnsTextEncoding.FormatSigned(data.Protocol) + " " +
            DnsTextEncoding.FormatSigned(algorithm) + " " +
            DnsTextEncoding.FormatBase64(data.PublicKey);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatDsData(DnsDsData? data)
    {
        if (data == null) return Missing("Delegation-signer data is required.");
        int algorithm = DnsHeaderCodec.ResolveCode(
            data.AlgorithmValue, (int)data.Algorithm);
        int digest = DnsHeaderCodec.ResolveCode(
            data.DigestAlgorithmValue, (int)data.DigestAlgorithm);
        string text = DnsTextEncoding.FormatSigned(data.KeyTag) + " " +
            DnsTextEncoding.FormatSigned(algorithm) + " " +
            DnsTextEncoding.FormatSigned(digest) + " " +
            DnsTextEncoding.FormatHex(data.Digest);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatSshfpData(DnsSshfpData? data)
    {
        if (data == null) return Missing("SSHFP data is required.");
        string text = DnsTextEncoding.FormatSigned(data.Algorithm) + " " +
            DnsTextEncoding.FormatSigned(data.FingerprintType) + " " +
            DnsTextEncoding.FormatHex(data.Fingerprint);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatIpsecKeyData(
        DnsIpsecKeyData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("IPSECKEY data is required.");
        string gateway = ".";
        if (data.GatewayType == 1 || data.GatewayType == 2)
        {
            gateway = DnsTextEncoding.FormatAddress(data.GatewayAddress);
            if (gateway.Length == 0)
            {
                return Missing("The IPSECKEY gateway address is invalid.");
            }
        }
        else if (data.GatewayType == 3)
        {
            if (data.GatewayName == null)
            {
                return Missing("The IPSECKEY gateway name is required.");
            }

            DnsTextResult name = DnsPresentationNameText.FormatName(
                data.GatewayName, options);
            if (!name.Success) return name;
            gateway = name.Text;
        }

        string text = DnsTextEncoding.FormatSigned(data.Precedence) + " " +
            DnsTextEncoding.FormatSigned(data.GatewayType) + " " +
            DnsTextEncoding.FormatSigned(data.Algorithm) + " " + gateway + " " +
            DnsTextEncoding.FormatBase64(data.PublicKey);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatNsecData(
        DnsNsecData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("NSEC data is required.");
        DnsTextResult next = DnsPresentationNameText.FormatName(
            data.NextDomainName, options);
        if (!next.Success) return next;
        return DnsTextOperations.SuccessText(
            next.Text + FormatRecordTypeList(data.RecordTypeValues));
    }

    static DnsTextResult FormatNsec3Data(
        DnsNsec3Data? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("NSEC3 data is required.");
        int algorithm = DnsHeaderCodec.ResolveCode(
            data.HashAlgorithmValue, (int)data.HashAlgorithm);
        string salt = "-";
        if (data.Salt.Length > 0) salt = DnsTextEncoding.FormatHex(data.Salt);
        string text = DnsTextEncoding.FormatSigned(algorithm) + " " +
            DnsTextEncoding.FormatSigned(data.Flags) + " " +
            DnsTextEncoding.FormatSigned(data.Iterations) + " " + salt + " " +
            DnsTextEncoding.FormatBase32Hex(data.NextHashedOwnerName) +
            FormatRecordTypeList(data.RecordTypeValues);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatNsec3ParamData(DnsNsec3ParamData? data)
    {
        if (data == null) return Missing("NSEC3PARAM data is required.");
        int algorithm = DnsHeaderCodec.ResolveCode(
            data.HashAlgorithmValue, (int)data.HashAlgorithm);
        string salt = "-";
        if (data.Salt.Length > 0) salt = DnsTextEncoding.FormatHex(data.Salt);
        string text = DnsTextEncoding.FormatSigned(algorithm) + " " +
            DnsTextEncoding.FormatSigned(data.Flags) + " " +
            DnsTextEncoding.FormatSigned(data.Iterations) + " " + salt;
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatDhcidData(DnsDhcidData? data)
    {
        if (data == null) return Missing("DHCID data is required.");
        byte[] bytes = new byte[data.Digest.Length + 3];
        bytes[0] = (byte)(data.IdentifierType >> 8);
        bytes[1] = (byte)(data.IdentifierType & 255);
        bytes[2] = (byte)data.DigestType;
        data.Digest.CopyTo(bytes, 3);
        return DnsTextOperations.SuccessText(
            DnsTextEncoding.FormatBase64(bytes));
    }

    static DnsTextResult FormatTlsaData(DnsTlsaData? data)
    {
        if (data == null) return Missing("TLSA data is required.");
        string text = DnsTextEncoding.FormatSigned(data.CertificateUsage) + " " +
            DnsTextEncoding.FormatSigned(data.Selector) + " " +
            DnsTextEncoding.FormatSigned(data.MatchingType) + " " +
            DnsTextEncoding.FormatHex(data.AssociationData);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatHipData(
        DnsHipData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("HIP data is required.");
        string text = DnsTextEncoding.FormatSigned(data.PublicKeyAlgorithm) + " " +
            DnsTextEncoding.FormatHex(data.HostIdentityTag) + " " +
            DnsTextEncoding.FormatBase64(data.PublicKey);
        int index = 0;
        while (index < data.RendezvousServers.Count)
        {
            DnsTextResult name = DnsPresentationNameText.FormatName(
                data.RendezvousServers[index], options);
            if (!name.Success) return name;
            text = text + " " + name.Text;
            index = index + 1;
        }

        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatCsyncData(
        DnsCsyncData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("CSYNC data is required.");
        string text = DnsTextEncoding.FormatUnsigned(data.Serial) + " " +
            DnsTextEncoding.FormatSigned(data.Flags) +
            FormatRecordTypeList(data.RecordTypeValues);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatZonemdData(DnsZonemdData? data)
    {
        if (data == null) return Missing("ZONEMD data is required.");
        string text = DnsTextEncoding.FormatUnsigned(data.Serial) + " " +
            DnsTextEncoding.FormatSigned(data.Scheme) + " " +
            DnsTextEncoding.FormatSigned(data.HashAlgorithm) + " " +
            DnsTextEncoding.FormatHex(data.Digest);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatSvcbData(
        DnsSvcbData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("SVCB data is required.");
        DnsTextResult target = DnsPresentationNameText.FormatName(
            data.TargetName, options);
        if (!target.Success) return target;
        StringBuilder builder = new StringBuilder();
        builder.Append(DnsTextEncoding.FormatSigned(data.Priority));
        builder.Append(' ');
        builder.Append(target.Text);
        int index = 0;
        while (index < data.Parameters.Count)
        {
            builder.Append(' ');
            builder.Append(FormatSvcbParameter(data.Parameters[index]));
            index = index + 1;
        }

        return DnsTextOperations.SuccessText(builder.ToString());
    }

    static DnsTextResult FormatDsyncData(
        DnsDsyncData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("DSYNC data is required.");
        int recordType = DnsHeaderCodec.ResolveCode(
            data.RecordTypeValue, (int)data.RecordType);
        DnsTextResult target = DnsPresentationNameText.FormatName(
            data.Target, options);
        if (!target.Success) return target;
        string text = DnsMnemonicText.FormatRecordType(recordType, true) + " " +
            DnsTextEncoding.FormatSigned(data.Scheme) + " " +
            DnsTextEncoding.FormatSigned(data.Port) + " " + target.Text;
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatIlNpData(
        DnsIlNpData? data,
        int recordTypeValue)
    {
        if (data == null) return Missing("ILNP data is required.");
        string value;
        if (recordTypeValue == 105 && data.Value.Length == 4)
        {
            value = DnsTextEncoding.FormatAddress(data.Value);
        }
        else
        {
            value = FormatColonHex(data.Value);
        }

        return DnsTextOperations.SuccessText(
            DnsTextEncoding.FormatSigned(data.Preference) + " " + value);
    }

    static DnsTextResult FormatEuiData(DnsEuiData? data)
    {
        if (data == null) return Missing("EUI data is required.");
        return DnsTextOperations.SuccessText(FormatDashHex(data.Address));
    }

    static DnsTextResult FormatUriData(DnsUriData? data)
    {
        if (data == null) return Missing("URI data is required.");
        string text = DnsTextEncoding.FormatSigned(data.Priority) + " " +
            DnsTextEncoding.FormatSigned(data.Weight) + " " +
            DnsTextEncoding.FormatCharacterString(data.Target);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatCaaData(DnsCaaData? data)
    {
        if (data == null) return Missing("CAA data is required.");
        string text = DnsTextEncoding.FormatSigned(data.Flags) + " " +
            DnsTextEncoding.FormatCharacterString(data.Tag) + " " +
            DnsTextEncoding.FormatCharacterString(data.Value);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatAmtRelayData(
        DnsAmtRelayData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("AMTRELAY data is required.");
        string relay = ".";
        if (data.RelayType == 1 || data.RelayType == 2)
        {
            relay = DnsTextEncoding.FormatAddress(data.RelayAddress);
            if (relay.Length == 0) return Missing("The relay address is invalid.");
        }
        else if (data.RelayType == 3)
        {
            if (data.RelayName == null) return Missing("The relay name is required.");
            DnsTextResult name = DnsPresentationNameText.FormatName(
                data.RelayName, options);
            if (!name.Success) return name;
            relay = name.Text;
        }

        string optional = "0";
        if (data.DiscoveryOptional) optional = "1";
        string text = DnsTextEncoding.FormatSigned(data.Precedence) + " " +
            optional + " " + DnsTextEncoding.FormatSigned(data.RelayType) + " " +
            relay;
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatResolverInformationData(
        DnsResolverInformationData? data)
    {
        if (data == null) return Missing("RESINFO data is required.");
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < data.Values.Count)
        {
            if (index > 0) builder.Append(' ');
            builder.Append(DnsTextEncoding.FormatCharacterString(
                data.Values[index]));
            index = index + 1;
        }

        return DnsTextOperations.SuccessText(builder.ToString());
    }

    static DnsTextResult FormatTkeyData(
        DnsTkeyData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("TKEY data is required.");
        DnsTextResult algorithm = DnsPresentationNameText.FormatName(
            data.AlgorithmName, options);
        if (!algorithm.Success) return algorithm;
        string text = algorithm.Text + " " +
            DnsTextEncoding.FormatUnsigned(data.Inception) + " " +
            DnsTextEncoding.FormatUnsigned(data.Expiration) + " " +
            DnsTextEncoding.FormatSigned(data.Mode) + " " +
            DnsTextEncoding.FormatSigned(data.Error) + " " +
            DnsTextEncoding.FormatBase64(data.Key) + " " +
            DnsTextEncoding.FormatBase64(data.OtherData);
        return DnsTextOperations.SuccessText(text);
    }

    static DnsTextResult FormatTsigData(
        DnsTsigData? data,
        DnsTextOptions options)
    {
        if (data == null) return Missing("TSIG data is required.");
        DnsTextResult algorithm = DnsPresentationNameText.FormatName(
            data.AlgorithmName, options);
        if (!algorithm.Success) return algorithm;
        int error = DnsHeaderCodec.ResolveCode(
            data.ErrorValue, (int)data.Error);
        string text = algorithm.Text + " " +
            DnsTextEncoding.FormatUnsignedLong(data.TimeSigned) + " " +
            DnsTextEncoding.FormatSigned(data.Fudge) + " " +
            DnsTextEncoding.FormatBase64(data.Mac) + " " +
            DnsTextEncoding.FormatSigned(data.OriginalIdentifier) + " " +
            DnsTextEncoding.FormatSigned(error) + " " +
            DnsTextEncoding.FormatBase64(data.OtherData);
        return DnsTextOperations.SuccessText(text);
    }

    static string FormatRecordTypeList(List<int> values)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < values.Count)
        {
            builder.Append(' ');
            builder.Append(DnsMnemonicText.FormatRecordType(values[index], true));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string FormatSvcbParameter(DnsSvcbParameter parameter)
    {
        int key = DnsHeaderCodec.ResolveCode(
            parameter.KeyValue, (int)parameter.Key);
        string keyText = FormatSvcbKeyName(key);

        if (parameter.UseRawValue)
        {
            return keyText + "=\\#" +
                DnsTextEncoding.FormatHex(parameter.RawValue);
        }

        switch (key)
        {
            case 0: return keyText + "=" +
                FormatSvcbKeyList(parameter.MandatoryKeyValues);
            case 1: return keyText + "=" +
                FormatAlpnList(parameter.AlpnIdentifiers);
            case 2: return keyText;
            case 3: return keyText + "=" +
                DnsTextEncoding.FormatSigned(parameter.Port);
            case 4: return keyText + "=" +
                FormatAddressList(parameter.Ipv4Hints);
            case 5: return keyText + "=" +
                DnsTextEncoding.FormatBase64(parameter.Ech);
            case 6: return keyText + "=" +
                FormatAddressList(parameter.Ipv6Hints);
            case 7: return keyText + "=" + (parameter.DohPath ?? string.Empty);
            case 8: return keyText + "=" +
                DnsTextEncoding.FormatBase64(parameter.ObliviousHttp);
            default: return keyText + "=\\#" +
                DnsTextEncoding.FormatHex(parameter.RawValue);
        }
    }

    static string FormatSvcbKeyList(List<int> values)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0) builder.Append(',');
            int key = values[index];
            if (key >= 0 && key <= 8)
            {
                builder.Append(FormatSvcbKeyName(key));
            }
            else
            {
                builder.Append("key");
                builder.Append(DnsTextEncoding.FormatSigned(key));
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    static string FormatAlpnList(List<byte[]> values)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('"');
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0) builder.Append(',');
            byte[] value = values[index];
            int byteIndex = 0;
            while (byteIndex < value.Length)
            {
                builder.Append((char)value[byteIndex]);
                byteIndex = byteIndex + 1;
            }

            index = index + 1;
        }

        builder.Append('"');
        return builder.ToString();
    }

    static string FormatAddressList(List<byte[]> values)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0) builder.Append(',');
            builder.Append(DnsTextEncoding.FormatAddress(values[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string FormatSvcbKeyName(int key)
    {
        switch (key)
        {
            case 0: return "mandatory";
            case 1: return "alpn";
            case 2: return "no-default-alpn";
            case 3: return "port";
            case 4: return "ipv4hint";
            case 5: return "ech";
            case 6: return "ipv6hint";
            case 7: return "dohpath";
            case 8: return "ohttp";
            default: return "key" + DnsTextEncoding.FormatSigned(key);
        }
    }

    static string FormatAplAddress(DnsAplItem item)
    {
        int length = 4;
        if (item.AddressFamily == 2) length = 16;
        if ((item.AddressFamily == 1 || item.AddressFamily == 2) &&
            item.Address.Length <= length)
        {
            byte[] address = new byte[length];
            item.Address.CopyTo(address, 0);
            return DnsTextEncoding.FormatAddress(address);
        }

        return DnsTextEncoding.FormatHex(item.Address);
    }

    static string FormatLocCoordinate(uint value, bool latitude)
    {
        long center = 2147483648;
        long signed = (long)value - center;
        char direction;
        if (latitude)
        {
            direction = 'N';
            if (signed < 0) direction = 'S';
        }
        else
        {
            direction = 'E';
            if (signed < 0) direction = 'W';
        }

        if (signed < 0) signed = -signed;
        long degrees = signed / 3600000;
        long remainder = signed % 3600000;
        long minutes = remainder / 60000;
        decimal seconds = (decimal)(remainder % 60000) / 1000m;
        return degrees.ToString(CultureInfo.InvariantCulture) + " " +
            minutes.ToString(CultureInfo.InvariantCulture) + " " +
            seconds.ToString("0.000", CultureInfo.InvariantCulture) + " " +
            direction;
    }

    static decimal DecodeLocPrecision(int value)
    {
        int mantissa = (value >> 4) & 15;
        int exponent = value & 15;
        decimal centimeters = mantissa;
        int index = 0;
        while (index < exponent)
        {
            centimeters = centimeters * 10m;
            index = index + 1;
        }

        return centimeters / 100m;
    }

    static string FormatColonHex(byte[] data)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < data.Length)
        {
            if (index > 0 && (index & 1) == 0) builder.Append(':');
            builder.Append(DnsTextEncoding.FormatHex(
                new byte[] { data[index] }));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string FormatDashHex(byte[] data)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < data.Length)
        {
            if (index > 0) builder.Append('-');
            builder.Append(DnsTextEncoding.FormatHex(
                new byte[] { data[index] }));
            index = index + 1;
        }

        return builder.ToString();
    }

    static DnsTextResult Missing(string reason)
    {
        return DnsTextOperations.Failure(
            DnsDiagnosticCode.InvalidRecordData,
            "recordData",
            reason,
            0);
    }
}
