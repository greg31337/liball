using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace token;

internal enum JwkWriteProjection
{
    Complete = 0,
    Public = 1,
    Private = 2,
    Minimal = 3,
    Thumbprint = 4
}

internal static class JwkWriter
{
    internal static TokenTextResult Write(TokenKey key, JwkWriteProjection projection)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        JwkWriteState state = new JwkWriteState();
        string? keyType = ResolveKeyType(key, state);
        if (state.Error != null)
        {
            return Failure(state.Error);
        }

        string? algorithm = null;
        if (projection != JwkWriteProjection.Thumbprint
            || string.Equals(keyType, TokenKeyTypes.AlgorithmKeyPair, StringComparison.Ordinal))
        {
            algorithm = ResolveAlgorithm(key, state);
            if (state.Error != null)
            {
                return Failure(state.Error);
            }
        }

        if (projection != JwkWriteProjection.Thumbprint)
        {
            bool publicProjection = projection == JwkWriteProjection.Public;
            TokenResult policy = JwkKeyPolicy.Validate(key, algorithm, publicProjection);
            if (!policy.Success)
            {
                return Failure(policy.Error);
            }
        }

        AddString(state, JwkNames.KeyType, keyType);
        if (string.Equals(keyType, TokenKeyTypes.Symmetric, StringComparison.Ordinal))
        {
            WriteSymmetric(state, key, projection);
        }
        else if (string.Equals(keyType, TokenKeyTypes.Rsa, StringComparison.Ordinal))
        {
            WriteRsa(state, key, algorithm, projection);
        }
        else if (string.Equals(
            keyType,
            TokenKeyTypes.EllipticCurve,
            StringComparison.Ordinal))
        {
            WriteEc(state, key, algorithm, projection);
        }
        else if (string.Equals(
            keyType,
            TokenKeyTypes.OctetKeyPair,
            StringComparison.Ordinal))
        {
            WriteOkp(state, key, algorithm, projection);
        }
        else
        {
            WriteAlgorithmKeyPair(state, key, algorithm, projection);
        }

        if (state.Error != null)
        {
            return Failure(state.Error);
        }

        if (projection != JwkWriteProjection.Minimal
            && projection != JwkWriteProjection.Thumbprint)
        {
            WriteCommon(state, key, algorithm);
        }
        if (state.Error != null)
        {
            return Failure(state.Error);
        }

        return WriteDocument(state);
    }

    static void WriteSymmetric(
        JwkWriteState state,
        TokenKey key,
        JwkWriteProjection projection)
    {
        if (projection == JwkWriteProjection.Public)
        {
            state.Error = Error(
                "public_projection_unavailable",
                "A symmetric JWK has no public projection.");
            return;
        }

        if (HasAsymmetricMaterial(key))
        {
            state.Error = Error(
                "conflicting_key_material",
                "A symmetric JWK contains asymmetric key material.");
            return;
        }

        if (projection == JwkWriteProjection.Thumbprint)
        {
            if (key.SymmetricKey == null)
            {
                state.Error = Error(
                    "missing_key_material",
                    "Symmetric key material is required.");
                return;
            }

            if (key.SymmetricKey.Length < SymmetricKeyImporter.MinimumKeySizeInBytes)
            {
                state.Error = Error(
                    "weak_key",
                    "The symmetric key is shorter than the required minimum size.");
                return;
            }

            AddBytes(state, JwkNames.SymmetricKey, key.SymmetricKey);
            return;
        }

        TokenResult validation = SymmetricKeyImporterOperations.Validate(key);
        if (!validation.Success)
        {
            state.Error = validation.Error;
            return;
        }

        AddBytes(state, JwkNames.SymmetricKey, key.SymmetricKey);
    }

    static void WriteRsa(
        JwkWriteState state,
        TokenKey key,
        string? algorithm,
        JwkWriteProjection projection)
    {
        if (key.SymmetricKey != null || key.Ecdsa != null
            || key.OkpPublicKey != null || key.OkpPrivateKey != null)
        {
            state.Error = Error(
                "conflicting_key_material",
                "An RSA JWK contains material for another key type.");
            return;
        }

        X509Certificate2? certificate = GetLeafCertificate(key, state);
        if (state.Error != null)
        {
            return;
        }

        RSA? rsa = key.Rsa;
        bool dispose = false;
        bool nativePrivate = key.Rsa != null && key.NativePrivateMaterial;
        bool certificatePrivate = key.Rsa == null
            && certificate != null && certificate.HasPrivateKey;
        bool includePrivate = projection != JwkWriteProjection.Public
            && projection != JwkWriteProjection.Thumbprint
            && nativePrivate;
        if (projection == JwkWriteProjection.Private)
        {
            if (!nativePrivate && !certificatePrivate)
            {
                state.Error = Error(
                    "private_projection_unavailable",
                    "The RSA key has no private projection.");
                return;
            }

            includePrivate = true;
        }

        if (rsa == null)
        {
            if (certificate == null)
            {
                state.Error = Error(
                    "missing_key_material",
                    "An RSA key or certificate is required.");
                return;
            }

            if (includePrivate)
            {
                rsa = certificate.GetRSAPrivateKey();
            }
            else
            {
                rsa = certificate.GetRSAPublicKey();
            }
            dispose = true;
            if (rsa == null)
            {
                state.Error = Error(
                    "invalid_certificate_key",
                    "The certificate does not contain an RSA public key.");
                return;
            }
        }

        TokenKeyResult imported = RsaKeyImporterOperations.Import(rsa, includePrivate);
        if (!imported.Success)
        {
            if (dispose)
            {
                rsa.Dispose();
            }

            state.Error = imported.Error;
            return;
        }

        (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Algorithm = algorithm;
        TokenResult validation = RsaKeyImporterOperations.Validate(imported.Key);
        if (!validation.Success)
        {
            if (dispose)
            {
                rsa.Dispose();
            }

            state.Error = validation.Error;
            return;
        }

        RSAParameters parameters = rsa.ExportParameters(includePrivate);
        if (dispose)
        {
            rsa.Dispose();
        }

        AddUnsignedInteger(state, JwkNames.RsaModulus, parameters.Modulus, false);
        AddUnsignedInteger(state, JwkNames.RsaExponent, parameters.Exponent, false);
        if (includePrivate)
        {
            AddUnsignedInteger(state, JwkNames.PrivateKey, parameters.D, true);
            AddUnsignedInteger(state, JwkNames.RsaFirstPrime, parameters.P, true);
            AddUnsignedInteger(state, JwkNames.RsaSecondPrime, parameters.Q, true);
            AddUnsignedInteger(state, JwkNames.RsaFirstExponent, parameters.DP, true);
            AddUnsignedInteger(state, JwkNames.RsaSecondExponent, parameters.DQ, true);
            AddUnsignedInteger(state, JwkNames.RsaCoefficient, parameters.InverseQ, true);
            ClearRsaPrivate(parameters);
        }
    }

    static void WriteEc(
        JwkWriteState state,
        TokenKey key,
        string? algorithm,
        JwkWriteProjection projection)
    {
        if (key.SymmetricKey != null || key.Rsa != null
            || key.OkpPublicKey != null || key.OkpPrivateKey != null)
        {
            state.Error = Error(
                "conflicting_key_material",
                "An EC JWK contains material for another key type.");
            return;
        }

        X509Certificate2? certificate = GetLeafCertificate(key, state);
        if (state.Error != null)
        {
            return;
        }

        ECDsa? ecdsa = key.Ecdsa;
        bool dispose = false;
        bool nativePrivate = key.Ecdsa != null && key.NativePrivateMaterial;
        bool certificatePrivate = key.Ecdsa == null
            && certificate != null && certificate.HasPrivateKey;
        bool includePrivate = projection != JwkWriteProjection.Public
            && projection != JwkWriteProjection.Thumbprint
            && nativePrivate;
        if (projection == JwkWriteProjection.Private)
        {
            if (!nativePrivate && !certificatePrivate)
            {
                state.Error = Error(
                    "private_projection_unavailable",
                    "The EC key has no private projection.");
                return;
            }

            includePrivate = true;
        }

        if (ecdsa == null)
        {
            if (certificate == null)
            {
                state.Error = Error(
                    "missing_key_material",
                    "An ECDSA key or certificate is required.");
                return;
            }

            if (includePrivate)
            {
                ecdsa = certificate.GetECDsaPrivateKey();
            }
            else
            {
                ecdsa = certificate.GetECDsaPublicKey();
            }
            dispose = true;
            if (ecdsa == null)
            {
                state.Error = Error(
                    "invalid_certificate_key",
                    "The certificate does not contain an ECDSA public key.");
                return;
            }
        }

        TokenKeyResult imported = EcdsaKeyImporter.Import(ecdsa, includePrivate);
        if (!imported.Success)
        {
            if (dispose)
            {
                ecdsa.Dispose();
            }

            state.Error = imported.Error;
            return;
        }

        if (key.Curve != null
            && !string.Equals(key.Curve, (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Curve, StringComparison.Ordinal)
            || key.CoseCurve != 0 && key.CoseCurve != (imported.Key ?? throw new InvalidOperationException("A required value is null.")).CoseCurve)
        {
            if (dispose)
            {
                ecdsa.Dispose();
            }

            state.Error = Error(
                "conflicting_key_curve",
                "The EC curve metadata does not match the native key.");
            return;
        }

        (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Algorithm = algorithm;
        TokenResult validation = EcdsaKeyImporter.Validate(imported.Key);
        if (!validation.Success)
        {
            if (dispose)
            {
                ecdsa.Dispose();
            }

            state.Error = validation.Error;
            return;
        }

        ECParameters parameters = ecdsa.ExportParameters(includePrivate);
        if (dispose)
        {
            ecdsa.Dispose();
        }

        AddString(state, JwkNames.Curve, imported.Key.Curve);
        AddBytes(state, JwkNames.X, parameters.Q.X);
        AddBytes(state, JwkNames.Y, parameters.Q.Y);
        if (includePrivate)
        {
            AddBytes(state, JwkNames.PrivateKey, parameters.D);
            CryptographicOperations.ZeroMemory(parameters.D);
        }
    }

    static void WriteOkp(
        JwkWriteState state,
        TokenKey key,
        string? algorithm,
        JwkWriteProjection projection)
    {
        if (key.SymmetricKey != null || key.Rsa != null || key.Ecdsa != null
            || key.Certificate != null
            || key.CertificateChain != null && key.CertificateChain.Length > 0)
        {
            state.Error = Error(
                "conflicting_key_material",
                "An OKP JWK contains material for another key type.");
            return;
        }

        string? curve = key.Curve;
        if (curve == null && key.CoseCurve != 0)
        {
            curve = CoseKeyLabelsOperations.GetCurveName(key.CoseCurve);
        }

        if (curve == null)
        {
            state.Error = Error("missing_key_curve", "An OKP curve is required.");
            return;
        }

        JwkOkpCurve curveInfo = JsonWebKey.GetOkpCurve(curve);
        if (curveInfo.Error != null)
        {
            state.Error = curveInfo.Error;
            return;
        }

        if (key.CoseCurve != 0 && key.CoseCurve != curveInfo.CoseCurve)
        {
            state.Error = Error(
                "conflicting_key_curve",
                "The OKP curve name and COSE curve label do not match.");
            return;
        }

        if (key.OkpPublicKey == null || key.OkpPublicKey.Length != curveInfo.Size)
        {
            state.Error = Error(
                "invalid_okp_key",
                "The OKP public key does not have the required curve length.");
            return;
        }

        if (key.OkpPrivateKey != null && key.OkpPrivateKey.Length != curveInfo.Size)
        {
            state.Error = Error(
                "invalid_okp_key",
                "The OKP private key does not have the required curve length.");
            return;
        }

        if (projection == JwkWriteProjection.Private && key.OkpPrivateKey == null)
        {
            state.Error = Error(
                "private_projection_unavailable",
                "The OKP key has no private projection.");
            return;
        }

        TokenError? algorithmError = JsonWebKey.ValidateOkpAlgorithm(curve, algorithm);
        if (algorithmError != null)
        {
            state.Error = algorithmError;
            return;
        }

        AddString(state, JwkNames.Curve, curve);
        AddBytes(state, JwkNames.X, key.OkpPublicKey);
        if (projection != JwkWriteProjection.Public
            && projection != JwkWriteProjection.Thumbprint
            && key.OkpPrivateKey != null)
        {
            AddBytes(state, JwkNames.PrivateKey, key.OkpPrivateKey);
        }
    }

    static void WriteAlgorithmKeyPair(
        JwkWriteState state,
        TokenKey key,
        string? algorithm,
        JwkWriteProjection projection)
    {
        if (algorithm == null)
        {
            state.Error = Error(
                "missing_key_algorithm",
                "An AKP key requires a fully specified algorithm.");
            return;
        }

        MLDsaAlgorithm? selected = MlDsaKeyImporter.GetAlgorithm(algorithm);
        if (selected == null)
        {
            state.Error = Error(
                "unsupported_algorithm",
                "The AKP algorithm is not supported.");
            return;
        }

        if (key.AlgorithmPublicKey == null
            || key.AlgorithmPublicKey.Length != selected.PublicKeySizeInBytes)
        {
            state.Error = Error(
                "invalid_mldsa_key",
                "The ML-DSA public key has an invalid length.");
            return;
        }

        if (key.AlgorithmPrivateKey != null
            && key.AlgorithmPrivateKey.Length != selected.PrivateSeedSizeInBytes)
        {
            state.Error = Error(
                "invalid_mldsa_key",
                "The ML-DSA private seed has an invalid length.");
            return;
        }

        if (projection == JwkWriteProjection.Private
            && key.AlgorithmPrivateKey == null)
        {
            state.Error = Error(
                "missing_private_key",
                "The private JWK projection requires an ML-DSA private seed.");
            return;
        }

        if (projection == JwkWriteProjection.Minimal
            || projection == JwkWriteProjection.Thumbprint)
        {
            AddString(state, JwkNames.Algorithm, algorithm);
        }

        AddBytes(state, JwkNames.AlgorithmPublicKey, key.AlgorithmPublicKey);
        if (projection != JwkWriteProjection.Public
            && projection != JwkWriteProjection.Thumbprint
            && key.AlgorithmPrivateKey != null)
        {
            AddBytes(state, JwkNames.AlgorithmPrivateKey, key.AlgorithmPrivateKey);
        }
    }

    static void WriteCommon(JwkWriteState state, TokenKey key, string? algorithm)
    {
        AddString(state, JwkNames.Algorithm, algorithm);
        AddString(state, JwkNames.KeyId, key.KeyId);
        AddString(state, JwkNames.Use, key.Use);
        if (key.KeyOperations != null && key.KeyOperations.Length > 0)
        {
            AddArray(state, JwkNames.KeyOperations, key.KeyOperations);
        }

        X509Certificate2? certificate = GetLeafCertificate(key, state);
        if (state.Error != null)
        {
            return;
        }

        WriteCertificates(state, key, certificate);
    }

    static void WriteCertificates(
        JwkWriteState state,
        TokenKey key,
        X509Certificate2? certificate)
    {
        byte[]? sha1 = key.CertificateSha1Thumbprint;
        byte[]? sha256 = key.CertificateSha256Thumbprint;
        if (certificate != null)
        {
            byte[] actualSha1 = certificate.GetCertHash(HashAlgorithmName.SHA1);
            byte[] actualSha256 = certificate.GetCertHash(HashAlgorithmName.SHA256);
            if (sha1 != null && !ConstantTime.AreEqual(sha1, actualSha1)
                || sha256 != null && !ConstantTime.AreEqual(sha256, actualSha256))
            {
                state.Error = Error(
                    "certificate_thumbprint_mismatch",
                    "A stored certificate thumbprint does not match the certificate.");
                return;
            }

            sha1 = actualSha1;
            sha256 = actualSha256;
        }

        if (sha1 != null && sha1.Length != 20
            || sha256 != null && sha256.Length != 32)
        {
            state.Error = Error(
                "invalid_certificate_thumbprint",
                "A certificate thumbprint has an invalid digest length.");
            return;
        }

        AddBytes(state, JwkNames.X509Sha1Thumbprint, sha1);
        AddBytes(state, JwkNames.X509Sha256Thumbprint, sha256);

        if (key.CertificateChain != null && key.CertificateChain.Length > 0)
        {
            string[] encoded = new string[key.CertificateChain.Length];
            int index = 0;
            while (index < key.CertificateChain.Length)
            {
                if (key.CertificateChain[index] == null)
                {
                    state.Error = Error(
                        "invalid_certificate_chain",
                        "The certificate chain contains a null certificate.");
                    return;
                }

                encoded[index] = (Base64Codec.Encode(
                    key.CertificateChain[index].RawData).Text ?? throw new InvalidOperationException("A required value is null."));
                index++;
            }

            AddArray(state, JwkNames.X509CertificateChain, encoded);
        }
        else if (certificate != null)
        {
            AddArray(
                state,
                JwkNames.X509CertificateChain,
                new string[] { (Base64Codec.Encode(certificate.RawData).Text ?? throw new InvalidOperationException("A required value is null.")) });
        }
    }

    static string? ResolveKeyType(TokenKey key, JwkWriteState state)
    {
        string? textType = key.KeyType;
        if (textType != null && GetCoseKeyType(textType) == 0)
        {
            state.Error = Error(
                "unsupported_key_type",
                "The key type cannot be represented as a JWK.");
            return null;
        }

        string? coseType = GetKeyType(key.CoseKeyType);
        if (key.CoseKeyType != 0 && coseType == null)
        {
            state.Error = Error(
                "unsupported_key_type",
                "The COSE key type cannot be represented as a JWK.");
            return null;
        }

        if (textType != null && coseType != null
            && !string.Equals(textType, coseType, StringComparison.Ordinal))
        {
            state.Error = Error(
                "conflicting_key_type",
                "The text and COSE key types do not match.");
            return null;
        }

        string? result = textType == null ? coseType : textType;
        string? materialType = GetMaterialType(key, state);
        if (state.Error != null)
        {
            return null;
        }

        if (result == null)
        {
            result = materialType;
        }
        else if (materialType != null
            && !string.Equals(result, materialType, StringComparison.Ordinal))
        {
            state.Error = Error(
                "conflicting_key_type",
                "The key type does not match the available key material.");
            return null;
        }

        if (result == null)
        {
            state.Error = Error(
                "missing_key_material",
                "A supported JWK key type and material are required.");
        }

        return result;
    }

    static string? GetMaterialType(TokenKey key, JwkWriteState state)
    {
        string? result = null;
        result = MergeMaterialType(result, key.SymmetricKey != null, TokenKeyTypes.Symmetric, state);
        result = MergeMaterialType(result, key.Rsa != null, TokenKeyTypes.Rsa, state);
        result = MergeMaterialType(result, key.Ecdsa != null, TokenKeyTypes.EllipticCurve, state);
        bool okp = key.OkpPublicKey != null || key.OkpPrivateKey != null;
        result = MergeMaterialType(result, okp, TokenKeyTypes.OctetKeyPair, state);
        bool algorithmKey = key.AlgorithmPublicKey != null
            || key.AlgorithmPrivateKey != null;
        result = MergeMaterialType(
            result,
            algorithmKey,
            TokenKeyTypes.AlgorithmKeyPair,
            state);
        X509Certificate2? certificate = GetLeafCertificate(key, state);
        if (state.Error != null || certificate == null)
        {
            return result;
        }

        TokenKeyResult imported = X509KeyHelpers.Import(certificate);
        if (!imported.Success)
        {
            state.Error = imported.Error;
            return result;
        }

        return MergeMaterialType(result, true, (imported.Key ?? throw new InvalidOperationException("A required value is null.")).KeyType, state);
    }

    static string? MergeMaterialType(
        string? current,
        bool present,
        string? next,
        JwkWriteState state)
    {
        if (!present)
        {
            return current;
        }

        if (current != null && !string.Equals(current, next, StringComparison.Ordinal))
        {
            state.Error = Error(
                "conflicting_key_material",
                "The container has material for more than one key type.");
            return current;
        }

        return next;
    }

    static string? ResolveAlgorithm(TokenKey key, JwkWriteState state)
    {
        string? result = key.Algorithm;
        if (!key.HasCoseAlgorithm)
        {
            return result;
        }

        AlgorithmMappingResult mapping = JoseCoseAlgorithmMap.FromCose(key.CoseAlgorithm);
        if (!mapping.Mapped)
        {
            state.Error = Error(
                "unmapped_algorithm",
                "The COSE algorithm has no equivalent JOSE algorithm name.");
            return null;
        }

        if (result != null
            && !string.Equals(result, mapping.JoseName, StringComparison.Ordinal))
        {
            state.Error = Error(
                "conflicting_key_algorithm",
                "The JOSE and COSE algorithm identifiers are not equivalent.");
            return null;
        }

        return mapping.JoseName;
    }

    static X509Certificate2? GetLeafCertificate(TokenKey key, JwkWriteState state)
    {
        X509Certificate2? certificate = key.Certificate;
        if (key.CertificateChain == null || key.CertificateChain.Length == 0)
        {
            return certificate;
        }

        if (key.CertificateChain[0] == null)
        {
            state.Error = Error(
                "invalid_certificate_chain",
                "The certificate chain contains a null leaf certificate.");
            return null;
        }

        if (certificate != null
            && !ConstantTime.AreEqual(certificate.RawData, key.CertificateChain[0].RawData))
        {
            state.Error = Error(
                "certificate_chain_mismatch",
                "The certificate does not match the first chain element.");
            return null;
        }

        return key.CertificateChain[0];
    }

    static bool HasAsymmetricMaterial(TokenKey key)
    {
        return key.Rsa != null || key.Ecdsa != null
            || key.OkpPublicKey != null || key.OkpPrivateKey != null
            || key.AlgorithmPublicKey != null || key.AlgorithmPrivateKey != null
            || key.Certificate != null
            || key.CertificateChain != null && key.CertificateChain.Length > 0;
    }

    static int GetCoseKeyType(string keyType)
    {
        if (string.Equals(keyType, TokenKeyTypes.Symmetric, StringComparison.Ordinal))
        {
            return CoseKeyLabels.KeyTypeSymmetric;
        }

        if (string.Equals(keyType, TokenKeyTypes.Rsa, StringComparison.Ordinal))
        {
            return CoseKeyLabels.KeyTypeRsa;
        }

        if (string.Equals(keyType, TokenKeyTypes.EllipticCurve, StringComparison.Ordinal))
        {
            return CoseKeyLabels.KeyTypeEc2;
        }

        if (string.Equals(keyType, TokenKeyTypes.OctetKeyPair, StringComparison.Ordinal))
        {
            return CoseKeyLabels.KeyTypeOkp;
        }

        if (string.Equals(keyType, TokenKeyTypes.AlgorithmKeyPair, StringComparison.Ordinal))
        {
            return CoseKeyLabels.KeyTypeAlgorithmKeyPair;
        }

        return 0;
    }

    static string? GetKeyType(int coseKeyType)
    {
        if (coseKeyType == CoseKeyLabels.KeyTypeSymmetric)
        {
            return TokenKeyTypes.Symmetric;
        }

        if (coseKeyType == CoseKeyLabels.KeyTypeRsa)
        {
            return TokenKeyTypes.Rsa;
        }

        if (coseKeyType == CoseKeyLabels.KeyTypeEc2)
        {
            return TokenKeyTypes.EllipticCurve;
        }

        if (coseKeyType == CoseKeyLabels.KeyTypeOkp)
        {
            return TokenKeyTypes.OctetKeyPair;
        }

        if (coseKeyType == CoseKeyLabels.KeyTypeAlgorithmKeyPair)
        {
            return TokenKeyTypes.AlgorithmKeyPair;
        }

        return null;
    }

    static void AddUnsignedInteger(
        JwkWriteState state,
        string name,
        byte[]? value,
        bool clear)
    {
        TokenBytesResult minimal = BigEndianInteger.ToMinimal((value ?? throw new InvalidOperationException("A required value is null.")));
        AddBytes(state, name, minimal.Bytes);
        if (clear)
        {
            CryptographicOperations.ZeroMemory(minimal.Bytes);
        }
    }

    static void AddBytes(JwkWriteState state, string name, byte[]? value)
    {
        if (value == null)
        {
            return;
        }

        AddString(state, name, Base64Url.Encode(value).Text);
    }

    static void AddString(JwkWriteState state, string name, string? value)
    {
        if (value == null)
        {
            return;
        }

        JwkWriteMember member = new JwkWriteMember();
        member.Name = name;
        member.StringValue = value;
        state.Members.Add(member);
    }

    static void AddArray(JwkWriteState state, string name, string[] values)
    {
        JwkWriteMember member = new JwkWriteMember();
        member.Name = name;
        member.ArrayValue = values;
        member.IsArray = true;
        state.Members.Add(member);
    }

    static TokenTextResult WriteDocument(JwkWriteState state)
    {
        SortMembers(state.Members);
        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        int index = 0;
        while (index < state.Members.Count)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            JwkWriteMember member = state.Members[index];
            TokenTextResult name = JsonWriter.WriteStringValue(member.Name);
            if (!name.Success)
            {
                return name;
            }

            builder.Append(name.Text);
            builder.Append(':');
            if (member.IsArray)
            {
                TokenTextResult array = WriteArray(member.ArrayValue);
                if (!array.Success)
                {
                    return array;
                }

                builder.Append(array.Text);
            }
            else
            {
                TokenTextResult value = JsonWriter.WriteStringValue(member.StringValue);
                if (!value.Success)
                {
                    return value;
                }

                builder.Append(value.Text);
            }

            index++;
        }

        builder.Append('}');
        TokenTextResult result = new TokenTextResult();
        result.Text = builder.ToString();
        result.Success = true;
        return result;
    }

    static TokenTextResult WriteArray(string[]? values)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('[');
        int index = 0;
        while (index < (values ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            if (values[index] == null)
            {
                return Failure(
                    Error("invalid_json_string", "A JWK string array contains null."));
            }

            TokenTextResult encoded = JsonWriter.WriteStringValue(values[index]);
            if (!encoded.Success)
            {
                return encoded;
            }

            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(encoded.Text);
            index++;
        }

        builder.Append(']');
        TokenTextResult result = new TokenTextResult();
        result.Text = builder.ToString();
        result.Success = true;
        return result;
    }

    static void SortMembers(List<JwkWriteMember> members)
    {
        int index = 1;
        while (index < members.Count)
        {
            JwkWriteMember current = members[index];
            int insertion = index;
            while (insertion > 0
                && string.CompareOrdinal(members[insertion - 1].Name, current.Name) > 0)
            {
                members[insertion] = members[insertion - 1];
                insertion--;
            }

            members[insertion] = current;
            index++;
        }
    }

    static void ClearRsaPrivate(RSAParameters parameters)
    {
        Clear(parameters.D);
        Clear(parameters.P);
        Clear(parameters.Q);
        Clear(parameters.DP);
        Clear(parameters.DQ);
        Clear(parameters.InverseQ);
    }

    static void Clear(byte[]? value)
    {
        if (value != null)
        {
            CryptographicOperations.ZeroMemory(value);
        }
    }

    static TokenTextResult Failure(TokenError? error)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = error;
        return result;
    }

    static TokenError Error(string code, string message)
    {
        return new TokenError(code, message, -1);
    }
}

internal class JwkWriteState
{
    internal List<JwkWriteMember> Members;
    internal TokenError? Error;

    internal JwkWriteState()
    {
        Members = new List<JwkWriteMember>();
        Error = null;
    }
}

internal class JwkWriteMember
{
    internal string? Name;
    internal string? StringValue;
    internal string[]? ArrayValue;
    internal bool IsArray;
}
