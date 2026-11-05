using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace token;

internal enum CoseKeyWriteProjection
{
    Complete = 0,
    Public = 1,
    Private = 2,
    Minimal = 3,
    Thumbprint = 4
}

internal class CoseKeyWriteState
{
    internal List<CborMapEntry> Entries;
    internal List<byte[]> TemporarySecrets;
    internal TokenError? Error;

    internal CoseKeyWriteState()
    {
        Entries = new List<CborMapEntry>();
        TemporarySecrets = new List<byte[]>();
        Error = null;
    }
}

internal static class CoseKeyWriter
{
    internal static TokenBytesResult Write(TokenKey key)
    {
        return WriteProjection(key, CoseKeyWriteProjection.Complete);
    }

    internal static TokenBytesResult WriteProjection(
        TokenKey key,
        CoseKeyWriteProjection projection)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        CoseKeyWriteState state = new CoseKeyWriteState();
        int keyType = ResolveKeyType(key, state);
        if (state.Error != null)
        {
            return Failure(state.Error);
        }

        CoseIntegerValue algorithm = new CoseIntegerValue();
        if (projection != CoseKeyWriteProjection.Minimal
            && projection != CoseKeyWriteProjection.Thumbprint
            || keyType == CoseKeyLabels.KeyTypeAlgorithmKeyPair)
        {
            algorithm = ResolveAlgorithm(key, state);
            if (state.Error != null)
            {
                return Failure(state.Error);
            }
        }

        if (projection != CoseKeyWriteProjection.Minimal
            && projection != CoseKeyWriteProjection.Thumbprint)
        {
            ValidateOperations(key.CoseKeyOperations, state);
            if (state.Error != null)
            {
                return Failure(state.Error);
            }
        }

        AddInteger(state, CoseKeyLabels.KeyType, keyType);
        if (projection != CoseKeyWriteProjection.Minimal
            && projection != CoseKeyWriteProjection.Thumbprint)
        {
            AddBytes(state, CoseKeyLabels.KeyId, key.KeyIdBytes);
            AddBytes(
                state,
                CoseKeyLabels.BaseInitializationVector,
                key.BaseInitializationVector);
            if (algorithm.Present)
            {
                AddInteger(state, CoseKeyLabels.Algorithm, algorithm.Value);
            }

            AddOperations(state, key.CoseKeyOperations);
        }

        if (keyType == CoseKeyLabels.KeyTypeSymmetric)
        {
            WriteSymmetric(state, key, projection);
        }
        else if (keyType == CoseKeyLabels.KeyTypeRsa)
        {
            WriteRsa(state, key, algorithm, projection);
        }
        else if (keyType == CoseKeyLabels.KeyTypeEc2)
        {
            WriteEc2(state, key, algorithm, projection);
        }
        else if (keyType == CoseKeyLabels.KeyTypeOkp)
        {
            WriteOkp(state, key, algorithm, projection);
        }
        else
        {
            WriteAlgorithmKeyPair(state, key, algorithm, projection);
        }

        if (state.Error != null)
        {
            ClearTemporarySecrets(state);
            return Failure(state.Error);
        }

        CborWriter writer = CborWriter.Create();
        TokenResult written = writer.WriteDeterministicMap(state.Entries.ToArray());
        ClearTemporarySecrets(state);
        if (!written.Success)
        {
            return Failure(written.Error);
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    static void WriteSymmetric(
        CoseKeyWriteState state,
        TokenKey key,
        CoseKeyWriteProjection projection)
    {
        if (projection == CoseKeyWriteProjection.Public)
        {
            state.Error = Error(
                "public_projection_unavailable",
                "A symmetric COSE key has no public projection.");
            return;
        }

        if (HasAsymmetricMaterial(key))
        {
            state.Error = Error(
                "conflicting_key_material",
                "A symmetric COSE key contains asymmetric key material.");
            return;
        }

        if (projection == CoseKeyWriteProjection.Thumbprint)
        {
            if (key.SymmetricKey == null
                || key.SymmetricKey.Length < SymmetricKeyImporterOperations.MinimumKeySizeInBytes)
            {
                state.Error = Error(
                    "weak_key",
                    "The symmetric key is shorter than the required minimum size.");
                return;
            }

            AddBytes(state, CoseKeyLabels.SymmetricKey, key.SymmetricKey);
            return;
        }

        TokenResult validation = SymmetricKeyImporterOperations.Validate(key);
        if (!validation.Success)
        {
            state.Error = validation.Error;
            return;
        }

        AddBytes(state, CoseKeyLabels.SymmetricKey, key.SymmetricKey);
    }

    static void WriteRsa(
        CoseKeyWriteState state,
        TokenKey key,
        CoseIntegerValue algorithm,
        CoseKeyWriteProjection projection)
    {
        if (key.Rsa == null)
        {
            state.Error = Error("missing_key_material", "A native RSA key is required.");
            return;
        }

        bool includePrivate = key.NativePrivateMaterial
            && projection != CoseKeyWriteProjection.Public
            && projection != CoseKeyWriteProjection.Thumbprint;
        if (projection == CoseKeyWriteProjection.Private
            && !key.NativePrivateMaterial)
        {
            state.Error = Error(
                "missing_private_key",
                "The private COSE key projection requires private RSA material.");
            return;
        }

        TokenKeyResult validation;
        if (algorithm.Present)
        {
            validation = RsaKeyImporterOperations.ImportCose(
                key.Rsa,
                algorithm.Value,
                includePrivate);
        }
        else
        {
            validation = RsaKeyImporterOperations.Import(key.Rsa, includePrivate);
        }

        if (!validation.Success)
        {
            state.Error = validation.Error;
            return;
        }

        RSAParameters parameters = key.Rsa.ExportParameters(includePrivate);
        AddUnsigned(state, CoseKeyLabels.RsaModulus, parameters.Modulus, false);
        AddUnsigned(state, CoseKeyLabels.RsaPublicExponent, parameters.Exponent, false);
        if (includePrivate)
        {
            AddUnsigned(state, CoseKeyLabels.RsaPrivateExponent, parameters.D, true);
            AddUnsigned(state, CoseKeyLabels.RsaFirstPrimeFactor, parameters.P, true);
            AddUnsigned(state, CoseKeyLabels.RsaSecondPrimeFactor, parameters.Q, true);
            AddUnsigned(state, CoseKeyLabels.RsaFirstFactorCrtExponent, parameters.DP, true);
            AddUnsigned(state, CoseKeyLabels.RsaSecondFactorCrtExponent, parameters.DQ, true);
            AddUnsigned(state, CoseKeyLabels.RsaCrtCoefficient, parameters.InverseQ, true);
            ClearRsa(parameters);
        }
    }

    static void WriteEc2(
        CoseKeyWriteState state,
        TokenKey key,
        CoseIntegerValue algorithm,
        CoseKeyWriteProjection projection)
    {
        if (key.Ecdsa == null)
        {
            state.Error = Error("missing_key_material", "A native EC key is required.");
            return;
        }

        bool includePrivate = key.NativePrivateMaterial
            && projection != CoseKeyWriteProjection.Public
            && projection != CoseKeyWriteProjection.Thumbprint;
        if (projection == CoseKeyWriteProjection.Private
            && !key.NativePrivateMaterial)
        {
            state.Error = Error(
                "missing_private_key",
                "The private COSE key projection requires private EC material.");
            return;
        }

        TokenKeyResult imported = EcdsaKeyImporter.Import(
            key.Ecdsa,
            includePrivate);
        if (!imported.Success)
        {
            state.Error = imported.Error;
            return;
        }

        if (key.Curve != null
            && !string.Equals(key.Curve, (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Curve, StringComparison.Ordinal)
            || key.CoseCurve != 0 && key.CoseCurve != (imported.Key ?? throw new InvalidOperationException("A required value is null.")).CoseCurve)
        {
            state.Error = Error(
                "conflicting_key_curve",
                "The EC curve metadata does not match the native key.");
            return;
        }

        if (algorithm.Present && !IsEcdhAlgorithm(algorithm.Value))
        {
            TokenKeyResult algorithmValidation = EcdsaKeyImporter.ImportCose(
                key.Ecdsa,
                algorithm.Value,
                includePrivate);
            if (!algorithmValidation.Success)
            {
                state.Error = algorithmValidation.Error;
                return;
            }
        }

        ECParameters parameters = key.Ecdsa.ExportParameters(includePrivate);
        AddInteger(state, CoseKeyLabels.Curve, (imported.Key ?? throw new InvalidOperationException("A required value is null.")).CoseCurve);
        AddBytes(state, CoseKeyLabels.X, parameters.Q.X);
        AddBytes(state, CoseKeyLabels.Y, parameters.Q.Y);
        if (includePrivate)
        {
            AddTemporaryBytes(state, CoseKeyLabels.PrivateKey, parameters.D);
        }
    }

    static void WriteOkp(
        CoseKeyWriteState state,
        TokenKey key,
        CoseIntegerValue algorithm,
        CoseKeyWriteProjection projection)
    {
        int curve = ResolveOkpCurve(key, state);
        if (state.Error != null)
        {
            return;
        }

        string? curveName = CoseKeyLabelsOperations.GetCurveName(curve);
        JwkOkpCurve curveInfo = JsonWebKey.GetOkpCurve(curveName);
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

        if (projection == CoseKeyWriteProjection.Private
            && key.OkpPrivateKey == null)
        {
            state.Error = Error(
                "missing_private_key",
                "The private COSE key projection requires private OKP material.");
            return;
        }

        TokenKey check = new TokenKey();
        check.CoseKeyType = CoseKeyLabels.KeyTypeOkp;
        check.CoseCurve = curve;
        check.CoseAlgorithm = algorithm.Value;
        check.HasCoseAlgorithm = algorithm.Present;
        TokenResult validation = ValidateOkp(check);
        if (!validation.Success)
        {
            state.Error = validation.Error;
            return;
        }

        AddInteger(state, CoseKeyLabels.Curve, curve);
        AddBytes(state, CoseKeyLabels.X, key.OkpPublicKey);
        if (projection != CoseKeyWriteProjection.Public
            && projection != CoseKeyWriteProjection.Thumbprint)
        {
            AddBytes(state, CoseKeyLabels.PrivateKey, key.OkpPrivateKey);
        }
    }

    static void WriteAlgorithmKeyPair(
        CoseKeyWriteState state,
        TokenKey key,
        CoseIntegerValue algorithm,
        CoseKeyWriteProjection projection)
    {
        if (!algorithm.Present)
        {
            state.Error = Error(
                "missing_key_algorithm",
                "An AKP COSE key requires a fully specified algorithm.");
            return;
        }

        AlgorithmMappingResult mapping = JoseCoseAlgorithmMap.FromCose(
            algorithm.Value);
        MLDsaAlgorithm? selected = mapping.Mapped && mapping.JoseName != null
            ? MlDsaKeyImporter.GetAlgorithm(mapping.JoseName)
            : null;
        if (selected == null)
        {
            state.Error = Error(
                "unsupported_algorithm",
                "The AKP COSE algorithm is not supported.");
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

        if (projection == CoseKeyWriteProjection.Private
            && key.AlgorithmPrivateKey == null)
        {
            state.Error = Error(
                "missing_private_key",
                "The private COSE key projection requires an ML-DSA private seed.");
            return;
        }

        if (projection == CoseKeyWriteProjection.Minimal
            || projection == CoseKeyWriteProjection.Thumbprint)
        {
            AddInteger(state, CoseKeyLabels.Algorithm, algorithm.Value);
        }

        AddBytes(
            state,
            CoseKeyLabels.AlgorithmKeyPairPublicKey,
            key.AlgorithmPublicKey);
        if (projection != CoseKeyWriteProjection.Public
            && projection != CoseKeyWriteProjection.Thumbprint)
        {
            AddBytes(
                state,
                CoseKeyLabels.AlgorithmKeyPairPrivateKey,
                key.AlgorithmPrivateKey);
        }
    }

    static int ResolveKeyType(TokenKey key, CoseKeyWriteState state)
    {
        int textType = GetCoseKeyType(key.KeyType);
        if (key.KeyType != null && textType == 0)
        {
            state.Error = Error(
                "unsupported_key_type",
                "The text key type cannot be represented as a COSE key.");
            return 0;
        }

        if (key.CoseKeyType != 0 && !IsSupportedType(key.CoseKeyType))
        {
            state.Error = Error(
                "unsupported_key_type",
                "The COSE key type is not supported.");
            return 0;
        }

        if (textType != 0 && key.CoseKeyType != 0 && textType != key.CoseKeyType)
        {
            state.Error = Error(
                "conflicting_key_type",
                "The text and COSE key types do not match.");
            return 0;
        }

        int result = key.CoseKeyType == 0 ? textType : key.CoseKeyType;
        int materialType = GetMaterialType(key, state);
        if (state.Error != null)
        {
            return 0;
        }

        if (result == 0)
        {
            result = materialType;
        }
        else if (materialType != 0 && result != materialType)
        {
            state.Error = Error(
                "conflicting_key_type",
                "The key type does not match the available key material.");
            return 0;
        }

        if (result == 0)
        {
            state.Error = Error(
                "missing_key_material",
                "A supported COSE key type and material are required.");
        }

        return result;
    }

    static CoseIntegerValue ResolveAlgorithm(TokenKey key, CoseKeyWriteState state)
    {
        CoseIntegerValue result = new CoseIntegerValue();
        if (key.Algorithm != null)
        {
            AlgorithmMappingResult mapping = JoseCoseAlgorithmMap.FromJose(key.Algorithm);
            if (!mapping.Mapped)
            {
                state.Error = Error(
                    "unmapped_algorithm",
                    "The JOSE algorithm has no equivalent COSE algorithm label.");
                return result;
            }

            result.Present = true;
            result.Value = mapping.CoseLabel;
        }

        if (key.HasCoseAlgorithm)
        {
            if (result.Present)
            {
                int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels((key.Algorithm ?? throw new InvalidOperationException("A required value is null.")));
                if (!RegistryLookup.Contains(mapped, key.CoseAlgorithm))
                {
                    state.Error = Error(
                        "conflicting_key_algorithm",
                        "The JOSE and COSE algorithm identifiers are not equivalent.");
                    return result;
                }
            }

            result.Present = true;
            result.Value = key.CoseAlgorithm;
        }

        return result;
    }

    static int GetMaterialType(TokenKey key, CoseKeyWriteState state)
    {
        int result = 0;
        result = MergeType(result, key.SymmetricKey != null, CoseKeyLabels.KeyTypeSymmetric, state);
        result = MergeType(result, key.Rsa != null, CoseKeyLabels.KeyTypeRsa, state);
        result = MergeType(result, key.Ecdsa != null, CoseKeyLabels.KeyTypeEc2, state);
        bool okp = key.OkpPublicKey != null || key.OkpPrivateKey != null;
        result = MergeType(result, okp, CoseKeyLabels.KeyTypeOkp, state);
        bool algorithmKey = key.AlgorithmPublicKey != null
            || key.AlgorithmPrivateKey != null;
        return MergeType(
            result,
            algorithmKey,
            CoseKeyLabels.KeyTypeAlgorithmKeyPair,
            state);
    }

    static int MergeType(
        int current,
        bool present,
        int next,
        CoseKeyWriteState state)
    {
        if (!present)
        {
            return current;
        }

        if (current != 0 && current != next)
        {
            state.Error = Error(
                "conflicting_key_material",
                "The container has material for more than one key type.");
            return current;
        }

        return next;
    }

    static int ResolveOkpCurve(TokenKey key, CoseKeyWriteState state)
    {
        int textCurve = 0;
        if (key.Curve != null)
        {
            int[] curves = CoseKeyLabelsOperations.GetCurves();
            int index = 0;
            while (index < curves.Length)
            {
                if (string.Equals(
                    key.Curve,
                    CoseKeyLabelsOperations.GetCurveName(curves[index]),
                    StringComparison.Ordinal))
                {
                    textCurve = curves[index];
                    break;
                }

                index++;
            }

            if (!IsOkpCurve(textCurve))
            {
                state.Error = Error("unsupported_curve", "The OKP curve is not supported.");
                return 0;
            }
        }

        if (key.CoseCurve != 0 && !IsOkpCurve(key.CoseCurve))
        {
            state.Error = Error("unsupported_curve", "The COSE OKP curve is not supported.");
            return 0;
        }

        if (textCurve != 0 && key.CoseCurve != 0 && textCurve != key.CoseCurve)
        {
            state.Error = Error(
                "conflicting_key_curve",
                "The OKP curve name and COSE curve label do not match.");
            return 0;
        }

        int result = key.CoseCurve == 0 ? textCurve : key.CoseCurve;
        if (result == 0)
        {
            state.Error = Error("missing_key_curve", "An OKP curve is required.");
        }

        return result;
    }

    static void ValidateOperations(int[]? operations, CoseKeyWriteState state)
    {
        if (operations == null)
        {
            return;
        }

        int index = 0;
        while (index < operations.Length)
        {
            if (!CoseKeyLabelsOperations.IsRegisteredOperation(operations[index]))
            {
                state.Error = Error(
                    "unknown_key_operation",
                    "The COSE key contains an unsupported key operation.");
                return;
            }

            int previous = 0;
            while (previous < index)
            {
                if (operations[previous] == operations[index])
                {
                    state.Error = Error(
                        "duplicate_key_operation",
                        "The COSE key contains a duplicate key operation.");
                    return;
                }

                previous++;
            }

            index++;
        }
    }

    static void AddOperations(CoseKeyWriteState state, int[]? operations)
    {
        if (operations == null || operations.Length == 0)
        {
            return;
        }

        CborItem[] items = new CborItem[operations.Length];
        int index = 0;
        while (index < items.Length)
        {
            items[index] = Integer(operations[index]);
            index++;
        }

        CborItem array = new CborItem();
        array.Kind = TokenCborValueKind.Array;
        array.ArrayValue = items;
        state.Entries.Add(new CborMapEntry(
            Integer(CoseKeyLabels.KeyOperations),
            array));
    }

    static void AddInteger(CoseKeyWriteState state, int label, int value)
    {
        state.Entries.Add(new CborMapEntry(Integer(label), Integer(value)));
    }

    static void AddBytes(CoseKeyWriteState state, int label, byte[]? value)
    {
        if (value == null)
        {
            return;
        }

        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        state.Entries.Add(new CborMapEntry(Integer(label), item));
    }

    static void AddTemporaryBytes(CoseKeyWriteState state, int label, byte[]? value)
    {
        AddBytes(state, label, value);
        if (value != null)
        {
            state.TemporarySecrets.Add(value);
        }
    }

    static void AddUnsigned(
        CoseKeyWriteState state,
        int label,
        byte[]? value,
        bool secret)
    {
        int index = 0;
        while (index < (value ?? throw new InvalidOperationException("A required value is null.")).Length - 1 && value[index] == 0)
        {
            index++;
        }

        byte[] minimal = value.AsSpan(index).ToArray();
        AddBytes(state, label, minimal);
        if (secret)
        {
            state.TemporarySecrets.Add(minimal);
        }
    }

    static CborItem Integer(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(value + 1));
        }

        return item;
    }

    static void ClearRsa(RSAParameters parameters)
    {
        Clear(parameters.D);
        Clear(parameters.P);
        Clear(parameters.Q);
        Clear(parameters.DP);
        Clear(parameters.DQ);
        Clear(parameters.InverseQ);
    }

    static void ClearTemporarySecrets(CoseKeyWriteState state)
    {
        int index = 0;
        while (index < state.TemporarySecrets.Count)
        {
            Clear(state.TemporarySecrets[index]);
            index++;
        }

        state.TemporarySecrets.Clear();
    }

    static void Clear(byte[]? bytes)
    {
        if (bytes != null)
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    static bool HasAsymmetricMaterial(TokenKey key)
    {
        return key.Rsa != null || key.Ecdsa != null
            || key.OkpPublicKey != null || key.OkpPrivateKey != null
            || key.AlgorithmPublicKey != null || key.AlgorithmPrivateKey != null
            || key.Certificate != null
            || key.CertificateChain != null && key.CertificateChain.Length > 0;
    }

    static bool IsSupportedType(int keyType)
    {
        return keyType == CoseKeyLabels.KeyTypeSymmetric
            || keyType == CoseKeyLabels.KeyTypeRsa
            || keyType == CoseKeyLabels.KeyTypeEc2
            || keyType == CoseKeyLabels.KeyTypeOkp
            || keyType == CoseKeyLabels.KeyTypeAlgorithmKeyPair;
    }

    static int GetCoseKeyType(string? keyType)
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

    static bool IsOkpCurve(int curve)
    {
        return curve == CoseKeyLabels.CurveX25519
            || curve == CoseKeyLabels.CurveX448
            || curve == CoseKeyLabels.CurveEd25519
            || curve == CoseKeyLabels.CurveEd448;
    }

    static bool IsEcdhAlgorithm(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhEsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhEsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes256KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes256KeyWrap;
    }

    static TokenResult ValidateOkp(TokenKey key)
    {
        if (!key.HasCoseAlgorithm)
        {
            return Success();
        }

        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetCose(key.CoseAlgorithm);
        if (!metadata.Known)
        {
            return ValidationFailure("unknown_algorithm", "The COSE algorithm is not registered.");
        }

        bool valid;
        if (key.CoseCurve == CoseKeyLabels.CurveEd25519)
        {
            valid = key.CoseAlgorithm == CoseAlgorithms.EdDsa
                || key.CoseAlgorithm == CoseAlgorithms.Ed25519;
        }
        else if (key.CoseCurve == CoseKeyLabels.CurveEd448)
        {
            valid = key.CoseAlgorithm == CoseAlgorithms.EdDsa
                || key.CoseAlgorithm == CoseAlgorithms.Ed448;
        }
        else
        {
            valid = IsEcdhAlgorithm(key.CoseAlgorithm);
        }

        if (!valid)
        {
            return ValidationFailure(
                "invalid_key_algorithm",
                "The COSE algorithm does not use the selected OKP curve.");
        }

        return Success();
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = Error(code, message);
        return result;
    }

    static TokenError Error(string code, string message)
    {
        return new TokenError(code, message, -1);
    }

    static TokenBytesResult Failure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
