using System.Collections.Generic;

namespace dns;

internal class DnsDnssecIntegerResult
{
    internal bool Success;
    internal int Value;
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal class DnsPublicKeyResult
{
    internal bool Success;
    internal int AlgorithmValue;
    internal bool Rsa;
    internal bool Ecdsa;
    internal bool Edwards;
    internal byte[] Exponent = [];
    internal byte[] Modulus = [];
    internal byte[] X = [];
    internal byte[] Y = [];
    internal byte[] PublicKey = [];
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal class DnsDelegationDigestResult
{
    internal bool Success;
    internal DnsDsData? DelegationSigner;
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal class DnsSignatureTimeResult
{
    internal bool Success;
    internal bool Valid;
    internal bool NotYetValid;
    internal bool Expired;
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal class DnsAlgorithmPolicyResult
{
    internal bool Success;
    internal DnsAlgorithmPolicy Policy;
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal static class DnssecAlgorithms
{
    const int RsaMaximumLength = 512;
    const int RsaSha256MinimumModulusLength = 64;
    const int RsaSha512MinimumModulusLength = 128;

    internal static DnsDnssecIntegerResult CalculateKeyTag(
        DnsKeyData key)
    {
        DnsEncodeResult encoded = EncodeDnskeyRecordData(key);
        if (!encoded.Success)
        {
            return FromKeyTagEncodingFailure(encoded);
        }

        int algorithmValue = EffectiveValue(
            key.AlgorithmValue,
            (int)key.Algorithm);
        if (algorithmValue == (int)DnsDnssecAlgorithm.RsaMd5)
        {
            DnsPublicKeyResult decoded = DecodePublicKey(key);
            if (!decoded.Success || decoded.Modulus.Length < 3)
            {
                return FromPublicKeyTagFailure(decoded);
            }

            DnsDnssecIntegerResult legacy =
                new DnsDnssecIntegerResult();
            int offset = decoded.Modulus.Length - 3;
            legacy.Success = true;
            legacy.Value = decoded.Modulus[offset] * 256 +
                decoded.Modulus[offset + 1];
            return legacy;
        }

        ulong accumulator = 0;
        int index = 0;
        while (index < encoded.Bytes.Length)
        {
            if (index % 2 == 0)
            {
                accumulator = accumulator +
                    (ulong)encoded.Bytes[index] * 256;
            }
            else
            {
                accumulator = accumulator + encoded.Bytes[index];
            }

            index = index + 1;
        }

        accumulator = accumulator +
            ((accumulator >> 16) & 65535);
        DnsDnssecIntegerResult result =
            new DnsDnssecIntegerResult();
        result.Success = true;
        result.Value = (int)(accumulator & 65535);
        return result;
    }

    internal static DnsPublicKeyResult DecodePublicKey(DnsKeyData key)
    {
        if (key == null || key.PublicKey == null)
        {
            return CreatePublicKeyFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey.publicKey",
                "Complete DNSKEY public-key data is required.");
        }

        if (key.Protocol != 3)
        {
            return CreatePublicKeyFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey.protocol",
                "A DNSSEC DNSKEY protocol value must be three.");
        }

        int algorithmValue = EffectiveValue(
            key.AlgorithmValue,
            (int)key.Algorithm);
        if (algorithmValue == (int)DnsDnssecAlgorithm.RsaMd5 ||
            algorithmValue == (int)DnsDnssecAlgorithm.RsaSha1 ||
            algorithmValue == (int)DnsDnssecAlgorithm.RsaNsec3Sha1 ||
            algorithmValue == (int)DnsDnssecAlgorithm.RsaSha256 ||
            algorithmValue == (int)DnsDnssecAlgorithm.RsaSha512)
        {
            return DecodeRsaPublicKey(key.PublicKey, algorithmValue);
        }

        if (algorithmValue ==
                (int)DnsDnssecAlgorithm.EcdsaP256Sha256 ||
            algorithmValue ==
                (int)DnsDnssecAlgorithm.EcdsaP384Sha384)
        {
            return DecodeEcdsaPublicKey(key.PublicKey, algorithmValue);
        }

        if (algorithmValue == (int)DnsDnssecAlgorithm.Ed25519 ||
            algorithmValue == (int)DnsDnssecAlgorithm.Ed448)
        {
            return DecodeEdwardsPublicKey(key.PublicKey, algorithmValue);
        }

        return CreatePublicKeyFailure(
            DnsDiagnosticCode.UnsupportedAlgorithm,
            "dnskey.algorithm",
            "The DNSKEY public-key algorithm is not supported.");
    }

    internal static DnsDelegationDigestResult CreateDelegationDigest(
        DnsName owner,
        DnsKeyData key,
        DnsDigestAlgorithm digestAlgorithm,
        int digestAlgorithmValue)
    {
        if (key == null || (key.Flags & 256) == 0)
        {
            return CreateDigestFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey.flags",
                "A DS or CDS digest requires a DNSSEC zone key.");
        }

        DnsEncodeResult ownerData =
            CanonicalDnsData.EncodeCanonicalName(owner);
        if (!ownerData.Success)
        {
            return FromDigestEncodingFailure(ownerData);
        }

        DnsEncodeResult keyData = EncodeDnskeyRecordData(key);
        if (!keyData.Success)
        {
            return FromDigestEncodingFailure(keyData);
        }

        DnsDnssecIntegerResult keyTag = CalculateKeyTag(key);
        if (!keyTag.Success)
        {
            return FromDigestKeyTagFailure(keyTag);
        }

        int digestValue = EffectiveValue(
            digestAlgorithmValue,
            (int)digestAlgorithm);
        byte[] input = ConcatenateBytes(ownerData.Bytes, keyData.Bytes);
        byte[] digest;
        if (digestValue == (int)DnsDigestAlgorithm.Sha1)
        {
            digest = System.Security.Cryptography.SHA1.HashData(input);
        }
        else if (digestValue == (int)DnsDigestAlgorithm.Sha256)
        {
            digest = System.Security.Cryptography.SHA256.HashData(input);
        }
        else if (digestValue == (int)DnsDigestAlgorithm.Sha384)
        {
            digest = System.Security.Cryptography.SHA384.HashData(input);
        }
        else
        {
            return CreateDigestFailure(
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "ds.digestAlgorithm",
                "The DS or CDS digest algorithm is not supported.");
        }

        int keyAlgorithmValue = EffectiveValue(
            key.AlgorithmValue,
            (int)key.Algorithm);
        DnsDsData delegationSigner = new DnsDsData();
        delegationSigner.KeyTag = keyTag.Value;
        delegationSigner.Algorithm =
            (DnsDnssecAlgorithm)keyAlgorithmValue;
        delegationSigner.AlgorithmValue = keyAlgorithmValue;
        delegationSigner.DigestAlgorithm =
            (DnsDigestAlgorithm)digestValue;
        delegationSigner.DigestAlgorithmValue = digestValue;
        delegationSigner.Digest = digest;
        DnsDelegationDigestResult result =
            new DnsDelegationDigestResult();
        result.Success = true;
        result.DelegationSigner = delegationSigner;
        return result;
    }

    internal static DnsSignatureTimeResult CheckSignatureTime(
        DnsSignatureData signature,
        uint validationTime,
        int clockSkewSeconds)
    {
        if (signature == null || clockSkewSeconds < 0)
        {
            return CreateTimeFailure(
                "rrsig.time",
                "RRSIG data and a nonnegative clock skew are required.");
        }

        DnsSerialComparison interval = DnsSerialArithmetic.CompareSerials(
            signature.SignatureExpiration,
            signature.SignatureInception);
        if (!interval.Defined || interval.Comparison <= 0)
        {
            return CreateTimeFailure(
                "rrsig.validity",
                "The RRSIG validity interval is undefined or empty.");
        }

        uint skew = (uint)clockSkewSeconds;
        uint earliestAccepted = SubtractSerial(validationTime, skew);
        uint latestAccepted = AddSerial(validationTime, skew);
        DnsSerialComparison afterInception =
            DnsSerialArithmetic.CompareSerials(
                latestAccepted,
                signature.SignatureInception);
        DnsSerialComparison beforeExpiration =
            DnsSerialArithmetic.CompareSerials(
                signature.SignatureExpiration,
                earliestAccepted);
        if (!afterInception.Defined || !beforeExpiration.Defined)
        {
            return CreateTimeFailure(
                "rrsig.validity",
                "The RRSIG time comparison is undefined.");
        }

        DnsSignatureTimeResult result = new DnsSignatureTimeResult();
        result.Success = true;
        result.NotYetValid = afterInception.Comparison < 0;
        result.Expired = beforeExpiration.Comparison < 0;
        result.Valid = !result.NotYetValid && !result.Expired;
        return result;
    }

    internal static DnsAlgorithmPolicyResult ResolveAlgorithmPolicy(
        DnsSecurityPolicy policy,
        int algorithmValue)
    {
        if (policy == null || policy.AlgorithmValues == null ||
            policy.AlgorithmPolicies == null || algorithmValue < 0 ||
            algorithmValue > 255)
        {
            return CreatePolicyFailure(
                "policy.algorithms",
                "A valid DNSSEC algorithm policy is required.");
        }

        return ResolvePolicyValue(
            policy.AlgorithmValues,
            policy.AlgorithmPolicies,
            policy.DefaultAlgorithmPolicy,
            algorithmValue,
            "policy.algorithms");
    }

    internal static DnsAlgorithmPolicyResult ResolveDigestPolicy(
        DnsSecurityPolicy policy,
        int digestAlgorithmValue)
    {
        if (policy == null || policy.DigestAlgorithmValues == null ||
            policy.DigestAlgorithmPolicies == null ||
            digestAlgorithmValue < 0 || digestAlgorithmValue > 255)
        {
            return CreatePolicyFailure(
                "policy.digestAlgorithms",
                "A valid DNSSEC digest algorithm policy is required.");
        }

        return ResolvePolicyValue(
            policy.DigestAlgorithmValues,
            policy.DigestAlgorithmPolicies,
            policy.DefaultDigestAlgorithmPolicy,
            digestAlgorithmValue,
            "policy.digestAlgorithms");
    }

    static DnsPublicKeyResult DecodeRsaPublicKey(
        byte[] publicKey,
        int algorithmValue)
    {
        if (publicKey.Length < 3)
        {
            return CreatePublicKeyFailure(
                DnsDiagnosticCode.InvalidLength,
                "dnskey.publicKey",
                "An RSA DNSKEY is missing its exponent or modulus.");
        }

        int exponentLength = publicKey[0];
        int exponentOffset = 1;
        if (exponentLength == 0)
        {
            if (publicKey.Length < 5)
            {
                return CreatePublicKeyFailure(
                    DnsDiagnosticCode.InvalidLength,
                    "dnskey.publicKey",
                    "An RSA DNSKEY extended exponent length is incomplete.");
            }

            exponentLength = publicKey[1] * 256 + publicKey[2];
            exponentOffset = 3;
        }

        int modulusOffset = exponentOffset + exponentLength;
        int modulusLength = publicKey.Length - modulusOffset;
        if (exponentLength < 1 || exponentLength > RsaMaximumLength ||
            modulusLength < 1 || modulusLength > RsaMaximumLength ||
            modulusOffset > publicKey.Length ||
            publicKey[exponentOffset] == 0 ||
            publicKey[modulusOffset] == 0)
        {
            return CreatePublicKeyFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey.publicKey",
                "An RSA exponent and modulus must be nonempty, bounded, and minimally encoded.");
        }

        if (algorithmValue == (int)DnsDnssecAlgorithm.RsaSha256 &&
            modulusLength < RsaSha256MinimumModulusLength)
        {
            return CreatePublicKeyFailure(
                DnsDiagnosticCode.InvalidLength,
                "dnskey.publicKey",
                "An RSA/SHA-256 modulus must contain at least 512 bits.");
        }

        if (algorithmValue == (int)DnsDnssecAlgorithm.RsaSha512 &&
            modulusLength < RsaSha512MinimumModulusLength)
        {
            return CreatePublicKeyFailure(
                DnsDiagnosticCode.InvalidLength,
                "dnskey.publicKey",
                "An RSA/SHA-512 modulus must contain at least 1024 bits.");
        }

        DnsPublicKeyResult result = new DnsPublicKeyResult();
        result.Success = true;
        result.AlgorithmValue = algorithmValue;
        result.Rsa = true;
        result.Exponent = CopyRegion(
            publicKey,
            exponentOffset,
            exponentLength);
        result.Modulus = CopyRegion(
            publicKey,
            modulusOffset,
            modulusLength);
        result.PublicKey = RecordSetOperations.CopyBytes(publicKey);
        return result;
    }

    static DnsPublicKeyResult DecodeEcdsaPublicKey(
        byte[] publicKey,
        int algorithmValue)
    {
        int coordinateLength = 32;
        if (algorithmValue ==
            (int)DnsDnssecAlgorithm.EcdsaP384Sha384)
        {
            coordinateLength = 48;
        }

        if (publicKey.Length != coordinateLength * 2)
        {
            return CreatePublicKeyFailure(
                DnsDiagnosticCode.InvalidLength,
                "dnskey.publicKey",
                "An ECDSA DNSKEY must contain exact X and Y coordinates.");
        }

        DnsPublicKeyResult result = new DnsPublicKeyResult();
        result.Success = true;
        result.AlgorithmValue = algorithmValue;
        result.Ecdsa = true;
        result.X = CopyRegion(publicKey, 0, coordinateLength);
        result.Y = CopyRegion(
            publicKey,
            coordinateLength,
            coordinateLength);
        result.PublicKey = RecordSetOperations.CopyBytes(publicKey);
        return result;
    }

    static DnsPublicKeyResult DecodeEdwardsPublicKey(
        byte[] publicKey,
        int algorithmValue)
    {
        int expectedLength = 32;
        if (algorithmValue == (int)DnsDnssecAlgorithm.Ed448)
        {
            expectedLength = 57;
        }

        if (publicKey.Length != expectedLength)
        {
            return CreatePublicKeyFailure(
                DnsDiagnosticCode.InvalidLength,
                "dnskey.publicKey",
                "An Edwards-curve DNSKEY has an invalid public-key length.");
        }

        DnsPublicKeyResult result = new DnsPublicKeyResult();
        result.Success = true;
        result.AlgorithmValue = algorithmValue;
        result.Edwards = true;
        result.PublicKey = RecordSetOperations.CopyBytes(publicKey);
        return result;
    }

    static DnsAlgorithmPolicyResult ResolvePolicyValue(
        List<int> values,
        List<DnsAlgorithmPolicy> policies,
        DnsAlgorithmPolicy defaultPolicy,
        int requestedValue,
        string field)
    {
        if (values.Count != policies.Count ||
            !PolicyValueIsValid(defaultPolicy))
        {
            return CreatePolicyFailure(
                field,
                "Algorithm values and policies must be aligned and valid.");
        }

        DnsAlgorithmPolicyResult result =
            new DnsAlgorithmPolicyResult();
        result.Policy = defaultPolicy;
        int matches = 0;
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] < 0 || values[index] > 255 ||
                !PolicyValueIsValid(policies[index]))
            {
                return CreatePolicyFailure(
                    field,
                    "Algorithm values and policies must fit their registries.");
            }

            if (values[index] == requestedValue)
            {
                result.Policy = policies[index];
                matches = matches + 1;
            }

            index = index + 1;
        }

        if (matches > 1)
        {
            return CreatePolicyFailure(
                field,
                "An algorithm may have only one explicit policy.");
        }

        result.Success = true;
        return result;
    }

    static bool PolicyValueIsValid(DnsAlgorithmPolicy policy)
    {
        int value = (int)policy;
        return value >= (int)DnsAlgorithmPolicy.Unsupported &&
            value <= (int)DnsAlgorithmPolicy.Forbidden;
    }

    static DnsEncodeResult EncodeDnskeyRecordData(DnsKeyData key)
    {
        if (key == null || key.PublicKey == null ||
            key.PublicKey.Length == 0 || key.Protocol != 3)
        {
            return CreateEncodingFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey",
                "A DNSKEY requires protocol three and nonempty public-key data.");
        }

        DnsRecordData data = new DnsRecordData();
        data.Key = key;
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.Canonical = true;
        options.EnableCompression = false;
        return ResourceRecordCodec.EncodeRecordData(
            (int)DnsRecordType.Dnskey,
            data,
            options);
    }

    static uint AddSerial(uint value, uint amount)
    {
        ulong sum = (ulong)value + amount;
        return (uint)(sum % 4294967296UL);
    }

    static uint SubtractSerial(uint value, uint amount)
    {
        if (value >= amount)
        {
            return value - amount;
        }

        return (uint)(4294967296UL + value - amount);
    }

    static byte[] ConcatenateBytes(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        System.Array.Copy(first, 0, result, 0, first.Length);
        System.Array.Copy(
            second,
            0,
            result,
            first.Length,
            second.Length);
        return result;
    }

    static byte[] CopyRegion(byte[] source, int offset, int length)
    {
        byte[] result = new byte[length];
        System.Array.Copy(source, offset, result, 0, length);
        return result;
    }

    static int EffectiveValue(int rawValue, int enumValue)
    {
        if (rawValue != 0 || enumValue == 0)
        {
            return rawValue;
        }

        return enumValue;
    }

    static DnsPublicKeyResult CreatePublicKeyFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsPublicKeyResult result = new DnsPublicKeyResult();
        AddDiagnostic(result.Diagnostics, code, field, reason);
        return result;
    }

    static DnsDelegationDigestResult CreateDigestFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDelegationDigestResult result =
            new DnsDelegationDigestResult();
        AddDiagnostic(result.Diagnostics, code, field, reason);
        return result;
    }

    static DnsSignatureTimeResult CreateTimeFailure(
        string field,
        string reason)
    {
        DnsSignatureTimeResult result = new DnsSignatureTimeResult();
        AddDiagnostic(
            result.Diagnostics,
            DnsDiagnosticCode.InvalidRecordData,
            field,
            reason);
        return result;
    }

    static DnsAlgorithmPolicyResult CreatePolicyFailure(
        string field,
        string reason)
    {
        DnsAlgorithmPolicyResult result =
            new DnsAlgorithmPolicyResult();
        AddDiagnostic(
            result.Diagnostics,
            DnsDiagnosticCode.ValueOutOfRange,
            field,
            reason);
        return result;
    }

    static DnsEncodeResult CreateEncodingFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsEncodeResult result = new DnsEncodeResult();
        AddDiagnostic(result.Diagnostics, code, field, reason);
        return result;
    }

    static DnsDnssecIntegerResult FromKeyTagEncodingFailure(
        DnsEncodeResult source)
    {
        DnsDnssecIntegerResult result =
            new DnsDnssecIntegerResult();
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsDnssecIntegerResult FromPublicKeyTagFailure(
        DnsPublicKeyResult source)
    {
        DnsDnssecIntegerResult result =
            new DnsDnssecIntegerResult();
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        if (result.Diagnostics.Count == 0)
        {
            AddDiagnostic(
                result.Diagnostics,
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey.publicKey",
                "The RSA/MD5 modulus cannot produce a key tag.");
        }

        return result;
    }

    static DnsDelegationDigestResult FromDigestEncodingFailure(
        DnsEncodeResult source)
    {
        DnsDelegationDigestResult result =
            new DnsDelegationDigestResult();
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsDelegationDigestResult FromDigestKeyTagFailure(
        DnsDnssecIntegerResult source)
    {
        DnsDelegationDigestResult result =
            new DnsDelegationDigestResult();
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static void AddDiagnostic(
        List<DnsDiagnostic> diagnostics,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        diagnostics.Add(diagnostic);
    }

    static void AppendDiagnostics(
        List<DnsDiagnostic> destination,
        List<DnsDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
}
