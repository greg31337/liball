using System;

namespace dns;

internal static class SecurityRecordDataCodec
{
    internal static DnsDecodeResult DecodeRecordData(
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        if (recordTypeValue == 24 || recordTypeValue == 46)
        {
            return DecodeSignatureData(
                recordTypeValue, message, offset, length, options);
        }

        if (recordTypeValue == 25 || recordTypeValue == 48 ||
            recordTypeValue == 60)
        {
            return DecodeKeyData(message, offset, length);
        }

        if (recordTypeValue == 43 || recordTypeValue == 59)
        {
            return DecodeDsData(message, offset, length);
        }

        if (recordTypeValue == 44)
        {
            return DecodeSshfpData(message, offset, length);
        }

        if (recordTypeValue == 45)
        {
            return DecodeIpsecKeyData(message, offset, length);
        }

        if (recordTypeValue == 49)
        {
            return DecodeDhcidData(message, offset, length);
        }

        if (recordTypeValue == 52 || recordTypeValue == 53)
        {
            return DecodeTlsaData(message, offset, length);
        }

        if (recordTypeValue == 55)
        {
            return DecodeHipData(message, offset, length);
        }

        if (recordTypeValue == 61)
        {
            return DecodeOpenPgpKeyData(message, offset, length);
        }

        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of this security record codec family.",
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

        if (recordTypeValue == 24 || recordTypeValue == 46)
        {
            if (data.Signature == null) return MissingSchema(writer, "signature");
            return EncodeSignatureData(
                writer, table, recordTypeValue, data.Signature, options);
        }

        if (recordTypeValue == 25 || recordTypeValue == 48 ||
            recordTypeValue == 60)
        {
            if (data.Key == null) return MissingSchema(writer, "key");
            return EncodeKeyData(writer, data.Key);
        }

        if (recordTypeValue == 43 || recordTypeValue == 59)
        {
            if (data.Ds == null) return MissingSchema(writer, "ds");
            return EncodeDsData(writer, data.Ds);
        }

        if (recordTypeValue == 44)
        {
            if (data.Sshfp == null) return MissingSchema(writer, "sshfp");
            return EncodeSshfpData(writer, data.Sshfp);
        }

        if (recordTypeValue == 45)
        {
            if (data.IpsecKey == null) return MissingSchema(writer, "ipsecKey");
            return EncodeIpsecKeyData(
                writer, table, data.IpsecKey, options);
        }

        if (recordTypeValue == 49)
        {
            if (data.Dhcid == null) return MissingSchema(writer, "dhcid");
            return EncodeDhcidData(writer, data.Dhcid);
        }

        if (recordTypeValue == 52 || recordTypeValue == 53)
        {
            if (data.Tlsa == null) return MissingSchema(writer, "tlsa");
            return EncodeTlsaData(writer, data.Tlsa);
        }

        if (recordTypeValue == 55)
        {
            if (data.Hip == null) return MissingSchema(writer, "hip");
            return EncodeHipData(writer, table, data.Hip, options);
        }

        if (recordTypeValue == 61)
        {
            if (data.FixedBytes == null) return MissingSchema(writer, "fixedBytes");
            return EncodeOpenPgpKeyData(writer, data.FixedBytes);
        }

        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of this security record codec family.",
            writer.WriteOffset());
    }

    internal static DnsDecodeResult DecodeSignatureData(
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (options == null)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange, "options",
                "DNS decode options are required.", offset);
        }

        if (length < 19)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd, "signature",
                "Signature data is missing fixed fields or a signer name.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult typeCovered = reader.ReadUnsigned16();
        WireReadResult algorithm = reader.ReadUnsigned8();
        WireReadResult labels = reader.ReadUnsigned8();
        WireReadResult originalTimeToLive = reader.ReadUnsigned32();
        WireReadResult expiration = reader.ReadUnsigned32();
        WireReadResult inception = reader.ReadUnsigned32();
        WireReadResult keyTag = reader.ReadUnsigned16();
        int nameOffset = reader.ReadOffset();
        int nameMaximumLength = offset + length - nameOffset;
        DnsDecodeResult signer = new DnsDecodeResult();
        if (recordTypeValue == 24)
        {
            signer = CompressedDnsNameDecoder.DecodeName(message, nameOffset, options);
        }
        else
        {
            signer = SharedRecordDataCodec.DecodeUncompressedNamePrefix(
                message, nameOffset, nameMaximumLength, "rrsig.signerName");
        }

        if (!signer.Success || signer.Name == null) return signer;
        if (signer.BytesConsumed > nameMaximumLength)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.RecordDataLengthMismatch,
                "signature.signerName",
                "The signer name exceeds the signature record data.",
                nameOffset);
        }

        int signatureOffset = nameOffset + signer.BytesConsumed;
        DnsSignatureData signature = new DnsSignatureData();
        signature.TypeCovered = (DnsRecordType)typeCovered.UnsignedValue;
        signature.TypeCoveredValue = (int)typeCovered.UnsignedValue;
        signature.Algorithm = (DnsDnssecAlgorithm)algorithm.UnsignedValue;
        signature.AlgorithmValue = (int)algorithm.UnsignedValue;
        signature.Labels = (int)labels.UnsignedValue;
        signature.OriginalTimeToLive = (uint)originalTimeToLive.UnsignedValue;
        signature.SignatureExpiration = (uint)expiration.UnsignedValue;
        signature.SignatureInception = (uint)inception.UnsignedValue;
        signature.KeyTag = (int)keyTag.UnsignedValue;
        signature.SignerName = signer.Name;
        signature.Signature = SharedRecordDataCodec.CopyRegion(
            message, signatureOffset, offset + length - signatureOffset);
        DnsRecordData data = new DnsRecordData();
        data.Signature = signature;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeSignatureData(
        WireWriter writer,
        CompressionTable table,
        int recordTypeValue,
        DnsSignatureData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.Signature == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "signature", "Complete signature data is required.");
        }

        int typeCovered = DnsHeaderCodec.ResolveCode(
            data.TypeCoveredValue, (int)data.TypeCovered);
        int algorithm = DnsHeaderCodec.ResolveCode(
            data.AlgorithmValue, (int)data.Algorithm);
        if (!Unsigned16IsValid(typeCovered) || !Unsigned8IsValid(algorithm) ||
            !Unsigned8IsValid(data.Labels) || !Unsigned16IsValid(data.KeyTag))
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange, "signature",
                "A signature numeric field exceeds its wire width.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult typeResult = writer.WriteUnsigned16((ulong)typeCovered);
        if (!typeResult.Success) return typeResult;
        WireWriteResult algorithmResult = writer.WriteUnsigned8((ulong)algorithm);
        if (!algorithmResult.Success) return algorithmResult;
        WireWriteResult labelsResult = writer.WriteUnsigned8((ulong)data.Labels);
        if (!labelsResult.Success) return labelsResult;
        WireWriteResult ttlResult = writer.WriteUnsigned32(data.OriginalTimeToLive);
        if (!ttlResult.Success) return ttlResult;
        WireWriteResult expirationResult = writer.WriteUnsigned32(
            data.SignatureExpiration);
        if (!expirationResult.Success) return expirationResult;
        WireWriteResult inceptionResult = writer.WriteUnsigned32(
            data.SignatureInception);
        if (!inceptionResult.Success) return inceptionResult;
        WireWriteResult keyTagResult = writer.WriteUnsigned16((ulong)data.KeyTag);
        if (!keyTagResult.Success) return keyTagResult;
        WireWriteResult signerResult = table.WriteName(
            writer, data.SignerName, options, recordTypeValue == 24);
        if (!signerResult.Success) return signerResult;
        WireWriteResult signatureResult = writer.WriteBytes(data.Signature);
        if (!signatureResult.Success) return signatureResult;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeKeyData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 4)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd, "key",
                "Key data requires flags, protocol, and algorithm fields.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult flags = reader.ReadUnsigned16();
        WireReadResult protocol = reader.ReadUnsigned8();
        WireReadResult algorithm = reader.ReadUnsigned8();
        WireReadResult publicKey = reader.ReadBytes(reader.RemainingLength());
        DnsKeyData key = new DnsKeyData();
        key.Flags = (int)flags.UnsignedValue;
        key.Protocol = (int)protocol.UnsignedValue;
        key.Algorithm = (DnsDnssecAlgorithm)algorithm.UnsignedValue;
        key.AlgorithmValue = (int)algorithm.UnsignedValue;
        key.PublicKey = publicKey.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Key = key;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeKeyData(WireWriter writer, DnsKeyData data)
    {
        if (writer == null || data == null || data.PublicKey == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "key", "Complete key data is required.");
        }

        int algorithm = DnsHeaderCodec.ResolveCode(
            data.AlgorithmValue, (int)data.Algorithm);
        if (!Unsigned16IsValid(data.Flags) || !Unsigned8IsValid(data.Protocol) ||
            !Unsigned8IsValid(algorithm))
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange, "key",
                "A key numeric field exceeds its wire width.", writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult flags = writer.WriteUnsigned16((ulong)data.Flags);
        if (!flags.Success) return flags;
        WireWriteResult protocol = writer.WriteUnsigned8((ulong)data.Protocol);
        if (!protocol.Success) return protocol;
        WireWriteResult algorithmResult = writer.WriteUnsigned8((ulong)algorithm);
        if (!algorithmResult.Success) return algorithmResult;
        WireWriteResult publicKey = writer.WriteBytes(data.PublicKey);
        if (!publicKey.Success) return publicKey;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeDsData(
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
                DnsDiagnosticCode.UnexpectedEnd, "ds",
                "Delegation digest data requires a nonempty digest.", offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult keyTag = reader.ReadUnsigned16();
        WireReadResult algorithm = reader.ReadUnsigned8();
        WireReadResult digestAlgorithm = reader.ReadUnsigned8();
        WireReadResult digest = reader.ReadBytes(reader.RemainingLength());
        if (!DigestLengthIsValid((int)digestAlgorithm.UnsignedValue, digest.Bytes.Length))
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength, "ds.digest",
                "The delegation digest length does not match its algorithm.",
                reader.ReadOffset() - digest.Bytes.Length);
        }

        DnsDsData ds = new DnsDsData();
        ds.KeyTag = (int)keyTag.UnsignedValue;
        ds.Algorithm = (DnsDnssecAlgorithm)algorithm.UnsignedValue;
        ds.AlgorithmValue = (int)algorithm.UnsignedValue;
        ds.DigestAlgorithm = (DnsDigestAlgorithm)digestAlgorithm.UnsignedValue;
        ds.DigestAlgorithmValue = (int)digestAlgorithm.UnsignedValue;
        ds.Digest = digest.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Ds = ds;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeDsData(WireWriter writer, DnsDsData data)
    {
        if (writer == null || data == null || data.Digest == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "ds", "Complete delegation digest data is required.");
        }

        int algorithm = DnsHeaderCodec.ResolveCode(
            data.AlgorithmValue, (int)data.Algorithm);
        int digestAlgorithm = DnsHeaderCodec.ResolveCode(
            data.DigestAlgorithmValue, (int)data.DigestAlgorithm);
        if (!Unsigned16IsValid(data.KeyTag) || !Unsigned8IsValid(algorithm) ||
            !Unsigned8IsValid(digestAlgorithm) ||
            !DigestLengthIsValid(digestAlgorithm, data.Digest.Length))
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange, "ds",
                "A delegation digest field is out of range or has the wrong length.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult keyTag = writer.WriteUnsigned16((ulong)data.KeyTag);
        if (!keyTag.Success) return keyTag;
        WireWriteResult algorithmResult = writer.WriteUnsigned8((ulong)algorithm);
        if (!algorithmResult.Success) return algorithmResult;
        WireWriteResult digestAlgorithmResult = writer.WriteUnsigned8(
            (ulong)digestAlgorithm);
        if (!digestAlgorithmResult.Success) return digestAlgorithmResult;
        WireWriteResult digest = writer.WriteBytes(data.Digest);
        if (!digest.Success) return digest;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeSshfpData(
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
                DnsDiagnosticCode.UnexpectedEnd, "sshfp",
                "SSHFP data requires a nonempty fingerprint.", offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult algorithm = reader.ReadUnsigned8();
        WireReadResult fingerprintType = reader.ReadUnsigned8();
        WireReadResult fingerprint = reader.ReadBytes(reader.RemainingLength());
        if (!FingerprintLengthIsValid(
            (int)fingerprintType.UnsignedValue, fingerprint.Bytes.Length))
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength, "sshfp.fingerprint",
                "The fingerprint length does not match its type.", offset + 2);
        }

        DnsSshfpData sshfp = new DnsSshfpData();
        sshfp.Algorithm = (int)algorithm.UnsignedValue;
        sshfp.FingerprintType = (int)fingerprintType.UnsignedValue;
        sshfp.Fingerprint = fingerprint.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Sshfp = sshfp;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeSshfpData(
        WireWriter writer,
        DnsSshfpData data)
    {
        if (writer == null || data == null || data.Fingerprint == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "sshfp", "Complete SSH fingerprint data is required.");
        }

        if (!Unsigned8IsValid(data.Algorithm) ||
            !Unsigned8IsValid(data.FingerprintType) ||
            !FingerprintLengthIsValid(data.FingerprintType, data.Fingerprint.Length))
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange, "sshfp",
                "An SSH fingerprint field is out of range or has the wrong length.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult algorithm = writer.WriteUnsigned8((ulong)data.Algorithm);
        if (!algorithm.Success) return algorithm;
        WireWriteResult fingerprintType = writer.WriteUnsigned8(
            (ulong)data.FingerprintType);
        if (!fingerprintType.Success) return fingerprintType;
        WireWriteResult fingerprint = writer.WriteBytes(data.Fingerprint);
        if (!fingerprint.Success) return fingerprint;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeIpsecKeyData(
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
                DnsDiagnosticCode.UnexpectedEnd, "ipsecKey",
                "IPSECKEY data requires precedence, gateway type, and algorithm.",
                offset + length);
        }

        int precedence = message[offset];
        int gatewayType = message[offset + 1];
        int algorithm = message[offset + 2];
        int cursor = offset + 3;
        int end = offset + length;
        byte[] gatewayAddress = [];
        DnsName? gatewayName = null;
        if (gatewayType == 1 || gatewayType == 2)
        {
            int addressLength = gatewayType == 1 ? 4 : 16;
            if (end - cursor < addressLength)
            {
                return SharedRecordDataCodec.DecodeFailure(
                    DnsDiagnosticCode.UnexpectedEnd, "ipsecKey.gateway",
                    "The IPSECKEY gateway address is truncated.", cursor);
            }

            gatewayAddress = SharedRecordDataCodec.CopyRegion(
                message, cursor, addressLength);
            cursor = cursor + addressLength;
        }
        else if (gatewayType == 3)
        {
            DnsDecodeResult gateway =
                SharedRecordDataCodec.DecodeUncompressedNamePrefix(
                    message, cursor, end - cursor, "ipsecKey.gateway");
            if (!gateway.Success || gateway.Name == null) return gateway;
            gatewayName = gateway.Name;
            cursor = cursor + gateway.BytesConsumed;
        }
        else if (gatewayType != 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData, "ipsecKey.gatewayType",
                "The IPSECKEY gateway type is not defined.", offset + 1);
        }

        byte[] publicKey = SharedRecordDataCodec.CopyRegion(
            message, cursor, end - cursor);
        if (algorithm == 0 && publicKey.Length != 0 ||
            algorithm != 0 && publicKey.Length == 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData, "ipsecKey.publicKey",
                "The public key must be absent only for algorithm zero.", cursor);
        }

        DnsIpsecKeyData ipsecKey = new DnsIpsecKeyData();
        ipsecKey.Precedence = precedence;
        ipsecKey.GatewayType = gatewayType;
        ipsecKey.Algorithm = algorithm;
        ipsecKey.GatewayAddress = gatewayAddress;
        ipsecKey.GatewayName = gatewayName;
        ipsecKey.PublicKey = publicKey;
        DnsRecordData data = new DnsRecordData();
        data.IpsecKey = ipsecKey;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeIpsecKeyData(
        WireWriter writer,
        CompressionTable table,
        DnsIpsecKeyData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.GatewayAddress == null || data.PublicKey == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "ipsecKey", "Complete IPSECKEY data is required.");
        }

        bool gatewayValid = data.GatewayType == 0 &&
            data.GatewayAddress.Length == 0 && data.GatewayName == null;
        gatewayValid = gatewayValid || data.GatewayType == 1 &&
            data.GatewayAddress.Length == 4 && data.GatewayName == null;
        gatewayValid = gatewayValid || data.GatewayType == 2 &&
            data.GatewayAddress.Length == 16 && data.GatewayName == null;
        gatewayValid = gatewayValid || data.GatewayType == 3 &&
            data.GatewayAddress.Length == 0 && data.GatewayName != null;
        bool keyValid = data.Algorithm == 0 && data.PublicKey.Length == 0 ||
            data.Algorithm != 0 && data.PublicKey.Length > 0;
        if (!Unsigned8IsValid(data.Precedence) ||
            !Unsigned8IsValid(data.Algorithm) || !gatewayValid || !keyValid)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData, "ipsecKey",
                "IPSECKEY gateway or key fields are inconsistent.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult precedence = writer.WriteUnsigned8((ulong)data.Precedence);
        if (!precedence.Success) return precedence;
        WireWriteResult gatewayType = writer.WriteUnsigned8((ulong)data.GatewayType);
        if (!gatewayType.Success) return gatewayType;
        WireWriteResult algorithm = writer.WriteUnsigned8((ulong)data.Algorithm);
        if (!algorithm.Success) return algorithm;
        if (data.GatewayType == 1 || data.GatewayType == 2)
        {
            WireWriteResult address = writer.WriteBytes(data.GatewayAddress);
            if (!address.Success) return address;
        }
        else if (data.GatewayType == 3 && data.GatewayName != null)
        {
            WireWriteResult name = table.WriteName(
                writer, data.GatewayName, options, false);
            if (!name.Success) return name;
        }

        WireWriteResult publicKey = writer.WriteBytes(data.PublicKey);
        if (!publicKey.Success) return publicKey;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeDhcidData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 4)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd, "dhcid",
                "DHCID data requires identifier, digest type, and digest fields.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult identifierType = reader.ReadUnsigned16();
        WireReadResult digestType = reader.ReadUnsigned8();
        WireReadResult digest = reader.ReadBytes(reader.RemainingLength());
        if ((digestType.UnsignedValue == 1 && digest.Bytes.Length != 32) ||
            digest.Bytes.Length == 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength, "dhcid.digest",
                "The DHCID digest length does not match its digest type.", offset + 3);
        }

        DnsDhcidData dhcid = new DnsDhcidData();
        dhcid.IdentifierType = (int)identifierType.UnsignedValue;
        dhcid.DigestType = (int)digestType.UnsignedValue;
        dhcid.Digest = digest.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Dhcid = dhcid;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeDhcidData(
        WireWriter writer,
        DnsDhcidData data)
    {
        if (writer == null || data == null || data.Digest == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "dhcid", "Complete DHCID data is required.");
        }

        if (!Unsigned16IsValid(data.IdentifierType) ||
            !Unsigned8IsValid(data.DigestType) || data.Digest.Length == 0 ||
            data.DigestType == 1 && data.Digest.Length != 32)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidLength, "dhcid",
                "A DHCID field is out of range or has the wrong length.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult identifier = writer.WriteUnsigned16(
            (ulong)data.IdentifierType);
        if (!identifier.Success) return identifier;
        WireWriteResult digestType = writer.WriteUnsigned8((ulong)data.DigestType);
        if (!digestType.Success) return digestType;
        WireWriteResult digest = writer.WriteBytes(data.Digest);
        if (!digest.Success) return digest;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeTlsaData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 4)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd, "tlsa",
                "Certificate association data must be nonempty.", offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult usage = reader.ReadUnsigned8();
        WireReadResult selector = reader.ReadUnsigned8();
        WireReadResult matchingType = reader.ReadUnsigned8();
        WireReadResult association = reader.ReadBytes(reader.RemainingLength());
        if (!AssociationLengthIsValid(
            (int)matchingType.UnsignedValue, association.Bytes.Length))
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength, "tlsa.associationData",
                "The association-data length does not match its type.", offset + 3);
        }

        DnsTlsaData tlsa = new DnsTlsaData();
        tlsa.CertificateUsage = (int)usage.UnsignedValue;
        tlsa.Selector = (int)selector.UnsignedValue;
        tlsa.MatchingType = (int)matchingType.UnsignedValue;
        tlsa.AssociationData = association.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Tlsa = tlsa;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeTlsaData(
        WireWriter writer,
        DnsTlsaData data)
    {
        if (writer == null || data == null || data.AssociationData == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "tlsa", "Complete certificate association data is required.");
        }

        if (!Unsigned8IsValid(data.CertificateUsage) ||
            !Unsigned8IsValid(data.Selector) ||
            !Unsigned8IsValid(data.MatchingType) ||
            !AssociationLengthIsValid(
                data.MatchingType, data.AssociationData.Length))
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidLength, "tlsa",
                "A certificate association field is out of range or has the wrong length.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult usage = writer.WriteUnsigned8(
            (ulong)data.CertificateUsage);
        if (!usage.Success) return usage;
        WireWriteResult selector = writer.WriteUnsigned8((ulong)data.Selector);
        if (!selector.Success) return selector;
        WireWriteResult matchingType = writer.WriteUnsigned8(
            (ulong)data.MatchingType);
        if (!matchingType.Success) return matchingType;
        WireWriteResult association = writer.WriteBytes(data.AssociationData);
        if (!association.Success) return association;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeOpenPgpKeyData(
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
                DnsDiagnosticCode.InvalidLength, "openPgpKey",
                "An OPENPGPKEY record must contain key bytes.", offset);
        }

        DnsFixedBytesData fixedBytes = new DnsFixedBytesData();
        fixedBytes.Bytes = SharedRecordDataCodec.CopyRegion(message, offset, length);
        DnsRecordData data = new DnsRecordData();
        data.FixedBytes = fixedBytes;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeOpenPgpKeyData(
        WireWriter writer,
        DnsFixedBytesData data)
    {
        if (writer == null || data == null || data.Bytes == null ||
            data.Bytes.Length == 0)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "openPgpKey", "OPENPGPKEY key bytes are required.");
        }

        return writer.WriteBytes(data.Bytes);
    }

    internal static DnsDecodeResult DecodeHipData(
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
                DnsDiagnosticCode.UnexpectedEnd, "hip",
                "HIP data requires lengths, an algorithm, and a HIT.", offset + length);
        }

        int hitLength = message[offset];
        int algorithm = message[offset + 1];
        int publicKeyLength = (message[offset + 2] << 8) | message[offset + 3];
        int cursor = offset + 4;
        int end = offset + length;
        if (hitLength == 0 || hitLength > end - cursor ||
            publicKeyLength > end - cursor - hitLength)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd, "hip.length",
                "The HIP HIT or public key is empty or truncated.", cursor);
        }

        byte[] hit = SharedRecordDataCodec.CopyRegion(message, cursor, hitLength);
        cursor = cursor + hitLength;
        byte[] publicKey = SharedRecordDataCodec.CopyRegion(
            message, cursor, publicKeyLength);
        cursor = cursor + publicKeyLength;
        if (algorithm == 0 && publicKeyLength != 0 ||
            algorithm != 0 && publicKeyLength == 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData, "hip.publicKey",
                "The HIP public key must be absent only for algorithm zero.", cursor);
        }

        DnsHipData hip = new DnsHipData();
        hip.PublicKeyAlgorithm = algorithm;
        hip.HostIdentityTag = hit;
        hip.PublicKey = publicKey;
        while (cursor < end)
        {
            DnsDecodeResult server =
                SharedRecordDataCodec.DecodeUncompressedNamePrefix(
                    message, cursor, end - cursor, "hip.rendezvousServer");
            if (!server.Success || server.Name == null) return server;
            hip.RendezvousServers.Add(server.Name);
            cursor = cursor + server.BytesConsumed;
        }

        DnsRecordData data = new DnsRecordData();
        data.Hip = hip;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeHipData(
        WireWriter writer,
        CompressionTable table,
        DnsHipData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.HostIdentityTag == null || data.PublicKey == null ||
            data.RendezvousServers == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "hip", "Complete HIP data and encoding state are required.");
        }

        bool keyValid = data.PublicKeyAlgorithm == 0 && data.PublicKey.Length == 0 ||
            data.PublicKeyAlgorithm != 0 && data.PublicKey.Length > 0;
        if (!Unsigned8IsValid(data.PublicKeyAlgorithm) ||
            data.HostIdentityTag.Length == 0 ||
            data.HostIdentityTag.Length > 255 ||
            data.PublicKey.Length > 65535 || !keyValid)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData, "hip",
                "HIP lengths or public-key fields are invalid.", writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult hitLength = writer.WriteUnsigned8(
            (ulong)data.HostIdentityTag.Length);
        if (!hitLength.Success) return hitLength;
        WireWriteResult algorithm = writer.WriteUnsigned8(
            (ulong)data.PublicKeyAlgorithm);
        if (!algorithm.Success) return algorithm;
        WireWriteResult publicKeyLength = writer.WriteUnsigned16(
            (ulong)data.PublicKey.Length);
        if (!publicKeyLength.Success) return publicKeyLength;
        WireWriteResult hit = writer.WriteBytes(data.HostIdentityTag);
        if (!hit.Success) return hit;
        WireWriteResult publicKey = writer.WriteBytes(data.PublicKey);
        if (!publicKey.Success) return publicKey;
        int index = 0;
        while (index < data.RendezvousServers.Count)
        {
            DnsName server = data.RendezvousServers[index];
            if (server == null)
            {
                return writer.FailAt(
                    DnsDiagnosticCode.InvalidRecordData,
                    "hip.rendezvousServer",
                    "A HIP rendezvous server name is required.",
                    writer.WriteOffset());
            }

            WireWriteResult serverResult = table.WriteName(
                writer, server, options, false);
            if (!serverResult.Success) return serverResult;
            index = index + 1;
        }

        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static bool DigestLengthIsValid(int algorithm, int length)
    {
        if (length <= 0) return false;
        if (algorithm == 1) return length == 20;
        if (algorithm == 2 || algorithm == 3 || algorithm == 6)
            return length == 32;
        if (algorithm == 4) return length == 48;
        return true;
    }

    internal static bool FingerprintLengthIsValid(int fingerprintType, int length)
    {
        if (length <= 0) return false;
        if (fingerprintType == 1) return length == 20;
        if (fingerprintType == 2) return length == 32;
        return true;
    }

    internal static bool AssociationLengthIsValid(int matchingType, int length)
    {
        if (length <= 0) return false;
        if (matchingType == 1) return length == 32;
        if (matchingType == 2) return length == 64;
        return true;
    }

    internal static bool Unsigned8IsValid(int value)
    {
        return value >= 0 && value <= 255;
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
