using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;

namespace dns;

internal class DnsSignatureAdapterResult
{
    internal bool Success;
    internal bool Supported;
    internal bool Valid;
    internal byte[] Signature = [];
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal static class DnssecSignatureAdapters
{
    internal static DnsSignatureAdapterResult SignSignature(
        DnsKeyMaterial material,
        byte[] signedData)
    {
        if (material == null || material.Key == null ||
            signedData == null || !material.HasPrivateKey ||
            material.PrivateKey == null || material.PrivateKey.Length == 0)
        {
            return CreateFailure(
                true,
                DnsDiagnosticCode.InvalidRecordData,
                "privateKey",
                "A DNSKEY, signed data, and caller-owned private key are required.");
        }

        int algorithmValue = EffectiveValue(
            material.Key.AlgorithmValue,
            (int)material.Key.Algorithm);
        if (!IsSupportedAlgorithm(algorithmValue))
        {
            return CreateFailure(
                false,
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "dnskey.algorithm",
                "The DNSSEC signing algorithm is not supported.");
        }

        DnsPrivateKeyTextResult privateKey = ReadPrivateKeyText(
            material,
            IsRsaAlgorithm(algorithmValue));
        if (!privateKey.Success)
        {
            DnsSignatureAdapterResult failed =
                new DnsSignatureAdapterResult();
            failed.Supported = true;
            AppendDiagnostics(
                failed.Diagnostics,
                privateKey.Diagnostics);
            return failed;
        }

        DnsSignatureAdapterResult result;
        if (IsRsaAlgorithm(algorithmValue))
        {
            result = SignRsa(
                privateKey.Text,
                algorithmValue,
                signedData);
        }
        else
        {
            result = SignEcdsa(
                privateKey.Text,
                algorithmValue,
                signedData);
        }

        if (result.Success)
        {
            DnsSignatureAdapterResult verified = VerifySignature(
                material.Key,
                signedData,
                result.Signature);
            if (!verified.Success || !verified.Valid)
            {
                return CreateFailure(
                    true,
                    DnsDiagnosticCode.InvalidRecordData,
                    "privateKey",
                    "The caller-owned private key does not match the supplied DNSKEY.");
            }
        }

        return result;
    }

    internal static DnsSignatureAdapterResult VerifySignature(
        DnsKeyData key,
        byte[] signedData,
        byte[] signature)
    {
        if (key == null || signedData == null || signature == null)
        {
            return CreateFailure(
                true,
                DnsDiagnosticCode.InvalidRecordData,
                "signature",
                "A DNSKEY, signed data, and signature are required.");
        }

        int algorithmValue = EffectiveValue(
            key.AlgorithmValue,
            (int)key.Algorithm);
        if (algorithmValue == (int)DnsDnssecAlgorithm.Ed25519)
        {
            return CreateFailure(
                false,
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "dnskey.algorithm",
                "Ed25519 verification is not exposed by this .NET target.");
        }

        if (!IsSupportedAlgorithm(algorithmValue))
        {
            return CreateFailure(
                false,
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "dnskey.algorithm",
                "The DNSSEC signature algorithm is not supported.");
        }

        DnsPublicKeyResult publicKey =
            DnssecAlgorithms.DecodePublicKey(key);
        if (!publicKey.Success)
        {
            return FromPublicKeyFailure(publicKey);
        }

        if (publicKey.Rsa)
        {
            return VerifyRsa(
                publicKey,
                algorithmValue,
                signedData,
                signature);
        }

        return VerifyEcdsa(
            publicKey,
            algorithmValue,
            signedData,
            signature);
    }

    static DnsSignatureAdapterResult VerifyRsa(
        DnsPublicKeyResult publicKey,
        int algorithmValue,
        byte[] signedData,
        byte[] signature)
    {
        if (!RsaKeyIsUsable(publicKey))
        {
            return CreateFailure(
                true,
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey.publicKey",
                "The RSA exponent and modulus are not usable verification parameters.");
        }

        if (signature.Length != publicKey.Modulus.Length)
        {
            return CreateVerificationResult(false);
        }

        HashAlgorithmName hashAlgorithm =
            new HashAlgorithmName("SHA1");
        if (algorithmValue == (int)DnsDnssecAlgorithm.RsaSha256)
        {
            hashAlgorithm = new HashAlgorithmName("SHA256");
        }
        else if (algorithmValue ==
            (int)DnsDnssecAlgorithm.RsaSha512)
        {
            hashAlgorithm = new HashAlgorithmName("SHA512");
        }

        RSAParameters parameters = new RSAParameters();
        parameters.Exponent = publicKey.Exponent;
        parameters.Modulus = publicKey.Modulus;
        using (RSA rsa = RSA.Create())
        {
            rsa.ImportParameters(parameters);
            bool valid = rsa.VerifyData(
                signedData,
                signature,
                hashAlgorithm,
                RSASignaturePadding.Pkcs1);
            return CreateVerificationResult(valid);
        }
    }

    static DnsSignatureAdapterResult SignRsa(
        string privateKey,
        int algorithmValue,
        byte[] signedData)
    {
        HashAlgorithmName hashAlgorithm =
            new HashAlgorithmName("SHA1");
        if (algorithmValue == (int)DnsDnssecAlgorithm.RsaSha256)
        {
            hashAlgorithm = new HashAlgorithmName("SHA256");
        }
        else if (algorithmValue ==
            (int)DnsDnssecAlgorithm.RsaSha512)
        {
            hashAlgorithm = new HashAlgorithmName("SHA512");
        }

        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(privateKey);
            DnsSignatureAdapterResult result =
                new DnsSignatureAdapterResult();
            result.Success = true;
            result.Supported = true;
            result.Valid = true;
            result.Signature = rsa.SignData(
                signedData,
                hashAlgorithm,
                RSASignaturePadding.Pkcs1);
            return result;
        }
    }

    static DnsSignatureAdapterResult VerifyEcdsa(
        DnsPublicKeyResult publicKey,
        int algorithmValue,
        byte[] signedData,
        byte[] signature)
    {
        int coordinateLength = 32;
        HashAlgorithmName hashAlgorithm =
            new HashAlgorithmName("SHA256");
        ECCurve curve = ECCurve.NamedCurves.nistP256;
        if (algorithmValue ==
            (int)DnsDnssecAlgorithm.EcdsaP384Sha384)
        {
            coordinateLength = 48;
            hashAlgorithm = new HashAlgorithmName("SHA384");
            curve = ECCurve.NamedCurves.nistP384;
        }

        if (signature.Length != coordinateLength * 2)
        {
            return CreateVerificationResult(false);
        }

        if (!EcdsaPointIsOnCurve(
                publicKey.X,
                publicKey.Y,
                algorithmValue))
        {
            return CreateFailure(
                true,
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey.publicKey",
                "The ECDSA public-key point is not on the selected curve.");
        }

        ECPoint point = new ECPoint();
        point.X = publicKey.X;
        point.Y = publicKey.Y;
        ECParameters parameters = new ECParameters();
        parameters.Curve = curve;
        parameters.Q = point;
        using (ECDsa ecdsa = ECDsa.Create())
        {
            ecdsa.ImportParameters(parameters);
            bool valid = ecdsa.VerifyData(
                signedData,
                signature,
                hashAlgorithm,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            return CreateVerificationResult(valid);
        }
    }

    static DnsSignatureAdapterResult SignEcdsa(
        string privateKey,
        int algorithmValue,
        byte[] signedData)
    {
        HashAlgorithmName hashAlgorithm =
            new HashAlgorithmName("SHA256");
        if (algorithmValue ==
            (int)DnsDnssecAlgorithm.EcdsaP384Sha384)
        {
            hashAlgorithm = new HashAlgorithmName("SHA384");
        }

        using (ECDsa ecdsa = ECDsa.Create())
        {
            ecdsa.ImportFromPem(privateKey);
            DnsSignatureAdapterResult result =
                new DnsSignatureAdapterResult();
            result.Success = true;
            result.Supported = true;
            result.Valid = true;
            result.Signature = ecdsa.SignData(
                signedData,
                hashAlgorithm,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            return result;
        }
    }

    static bool RsaKeyIsUsable(DnsPublicKeyResult publicKey)
    {
        if (publicKey.Exponent.Length == 0 ||
            publicKey.Modulus.Length < 64)
        {
            return false;
        }

        int exponentLast =
            publicKey.Exponent[publicKey.Exponent.Length - 1];
        int modulusLast =
            publicKey.Modulus[publicKey.Modulus.Length - 1];
        if ((exponentLast & 1) == 0 || (modulusLast & 1) == 0)
        {
            return false;
        }

        int index = 0;
        while (index < publicKey.Exponent.Length - 1)
        {
            if (publicKey.Exponent[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return exponentLast > 1;
    }

    static bool EcdsaPointIsOnCurve(
        byte[] xBytes,
        byte[] yBytes,
        int algorithmValue)
    {
        string primeText =
            "FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFF";
        string coefficientText =
            "5AC635D8AA3A93E7B3EBBD55769886BC651D06B0CC53B0F63BCE3C3E27D2604B";
        if (algorithmValue ==
            (int)DnsDnssecAlgorithm.EcdsaP384Sha384)
        {
            primeText =
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFFFF0000000000000000FFFFFFFF";
            coefficientText =
                "B3312FA7E23EE7E4988E056BE3F82D19181D9C6EFE8141120314088F5013875AC656398D8A2ED19D2A85C8EDD3EC2AEF";
        }

        BigInteger prime = new BigInteger(
            System.Convert.FromHexString(primeText),
            true,
            true);
        BigInteger coefficient = new BigInteger(
            System.Convert.FromHexString(coefficientText),
            true,
            true);
        BigInteger x = new BigInteger(xBytes, true, true);
        BigInteger y = new BigInteger(yBytes, true, true);
        if (x >= prime || y >= prime || (x == 0 && y == 0))
        {
            return false;
        }

        BigInteger left = y * y % prime;
        BigInteger right = (x * x % prime) * x % prime;
        right = (right - 3 * x + coefficient) % prime;
        if (right < 0)
        {
            right = right + prime;
        }

        return left == right;
    }

    static bool IsSupportedAlgorithm(int algorithmValue)
    {
        return algorithmValue == (int)DnsDnssecAlgorithm.RsaSha1 ||
            algorithmValue ==
                (int)DnsDnssecAlgorithm.RsaNsec3Sha1 ||
            algorithmValue == (int)DnsDnssecAlgorithm.RsaSha256 ||
            algorithmValue == (int)DnsDnssecAlgorithm.RsaSha512 ||
            algorithmValue ==
                (int)DnsDnssecAlgorithm.EcdsaP256Sha256 ||
            algorithmValue ==
                (int)DnsDnssecAlgorithm.EcdsaP384Sha384;
    }

    static bool IsRsaAlgorithm(int algorithmValue)
    {
        return algorithmValue == (int)DnsDnssecAlgorithm.RsaSha1 ||
            algorithmValue ==
                (int)DnsDnssecAlgorithm.RsaNsec3Sha1 ||
            algorithmValue == (int)DnsDnssecAlgorithm.RsaSha256 ||
            algorithmValue == (int)DnsDnssecAlgorithm.RsaSha512;
    }

    static DnsPrivateKeyTextResult ReadPrivateKeyText(
        DnsKeyMaterial material,
        bool rsa)
    {
        DnsPrivateKeyTextResult result =
            new DnsPrivateKeyTextResult();
        string text = System.Text.Encoding.ASCII.GetString(
            material.PrivateKey);
        if (text.StartsWith(
                "-----BEGIN ",
                System.StringComparison.Ordinal))
        {
            result.Success = true;
            result.Text = text;
            return result;
        }

        string? format = material.PrivateKeyFormat;
        string label = "PRIVATE KEY";
        if (format != null &&
            (string.Equals(
                format,
                "PKCS1",
                System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(
                format,
                "RSA",
                System.StringComparison.OrdinalIgnoreCase)))
        {
            if (!rsa)
            {
                return PrivateKeyFailure(
                    "PKCS1 private keys can only be used with RSA DNSKEY algorithms.");
            }

            label = "RSA PRIVATE KEY";
        }
        else if (format != null &&
            (string.Equals(
                format,
                "SEC1",
                System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(
                format,
                "EC",
                System.StringComparison.OrdinalIgnoreCase)))
        {
            if (rsa)
            {
                return PrivateKeyFailure(
                    "SEC1 private keys can only be used with ECDSA DNSKEY algorithms.");
            }

            label = "EC PRIVATE KEY";
        }
        else if (format != null &&
            !string.Equals(
                format,
                "PKCS8",
                System.StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                format,
                "DER",
                System.StringComparison.OrdinalIgnoreCase))
        {
            return PrivateKeyFailure(
                "The private-key format must be PKCS8, PKCS1, SEC1, DER, or PEM data.");
        }

        result.Success = true;
        result.Text = PemEncoding.WriteString(
            label,
            material.PrivateKey);
        return result;
    }

    static DnsPrivateKeyTextResult PrivateKeyFailure(string reason)
    {
        DnsPrivateKeyTextResult result =
            new DnsPrivateKeyTextResult();
        AddDiagnostic(
            result.Diagnostics,
            DnsDiagnosticCode.InvalidRecordData,
            "privateKey.format",
            reason);
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

    static DnsSignatureAdapterResult CreateVerificationResult(
        bool valid)
    {
        DnsSignatureAdapterResult result =
            new DnsSignatureAdapterResult();
        result.Success = true;
        result.Supported = true;
        result.Valid = valid;
        return result;
    }

    static DnsSignatureAdapterResult CreateFailure(
        bool supported,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsSignatureAdapterResult result =
            new DnsSignatureAdapterResult();
        result.Supported = supported;
        AddDiagnostic(result.Diagnostics, code, field, reason);
        return result;
    }

    static DnsSignatureAdapterResult FromPublicKeyFailure(
        DnsPublicKeyResult source)
    {
        DnsSignatureAdapterResult result =
            new DnsSignatureAdapterResult();
        result.Supported = true;
        int index = 0;
        while (index < source.Diagnostics.Count)
        {
            result.Diagnostics.Add(source.Diagnostics[index]);
            index = index + 1;
        }

        return result;
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
}

internal class DnsPrivateKeyTextResult
{
    internal bool Success;
    internal string Text = string.Empty;
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}
