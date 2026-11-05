using System;
namespace token;

internal static class JwkKeyPolicy
{
    internal static TokenResult Validate(
        TokenKey key,
        string? algorithm,
        bool publicProjection)
    {
        TokenResult use = ValidateUse(key.Use);
        if (!use.Success)
        {
            return use;
        }

        TokenResult useAlgorithm = ValidateUseAlgorithm(key.Use, algorithm);
        if (!useAlgorithm.Success)
        {
            return useAlgorithm;
        }

        TokenResult useKeyType = ValidateUseKeyType(key.Use, key);
        if (!useKeyType.Success)
        {
            return useKeyType;
        }

        string[] operations = key.KeyOperations;
        if (operations == null || operations.Length == 0)
        {
            return Success();
        }

        int index = 0;
        while (index < operations.Length)
        {
            string operation = operations[index];
            if (!IsRegisteredOperation(operation))
            {
                return Failure(
                    "unknown_key_operation",
                    "The JWK key_ops member contains an unsupported operation.");
            }

            int previous = 0;
            while (previous < index)
            {
                if (string.Equals(
                    operations[previous],
                    operation,
                    StringComparison.Ordinal))
                {
                    return Failure(
                        "duplicate_key_operation",
                        "The JWK key_ops member contains a duplicate operation.");
                }

                previous++;
            }

            if (!MatchesUse(operation, key.Use))
            {
                return Failure(
                    "key_use_operation_mismatch",
                    "A JWK key operation conflicts with the declared public key use.");
            }

            if (!MatchesKeyType(operation, key))
            {
                return Failure(
                    "key_type_operation_mismatch",
                    "A JWK key operation is not supported by the key type.");
            }

            if (!MatchesAlgorithm(operation, algorithm))
            {
                return Failure(
                    "key_algorithm_operation_mismatch",
                    "A JWK key operation conflicts with the declared algorithm.");
            }

            if (!HasCapability(operation, key, publicProjection))
            {
                return Failure(
                    "key_operation_unavailable",
                    "The key does not contain the material required by a JWK operation.");
            }

            index++;
        }

        return Success();
    }

    static TokenResult ValidateUse(string? use)
    {
        if (use == null
            || string.Equals(use, TokenKeyUses.Signature, StringComparison.Ordinal)
            || string.Equals(use, TokenKeyUses.Encryption, StringComparison.Ordinal))
        {
            return Success();
        }

        return Failure(
            "unknown_key_use",
            "The JWK use member is not a supported public key use.");
    }

    static TokenResult ValidateUseAlgorithm(string? use, string? algorithm)
    {
        if (use == null || algorithm == null)
        {
            return Success();
        }

        if (string.Equals(use, TokenKeyUses.Signature, StringComparison.Ordinal)
            && !JoseAlgorithmsOperations.IsSignature(algorithm))
        {
            return Failure(
                "key_use_algorithm_mismatch",
                "The JWK signature use conflicts with the declared algorithm.");
        }

        if (string.Equals(use, TokenKeyUses.Encryption, StringComparison.Ordinal)
            && JoseAlgorithmsOperations.IsSignature(algorithm))
        {
            return Failure(
                "key_use_algorithm_mismatch",
                "The JWK encryption use conflicts with the declared algorithm.");
        }

        return Success();
    }

    static TokenResult ValidateUseKeyType(string? use, TokenKey key)
    {
        if (use == null
            || !string.Equals(
                ResolveKeyType(key),
                TokenKeyTypes.OctetKeyPair,
                StringComparison.Ordinal))
        {
            return Success();
        }

        bool edwards = IsEdwardsCurve(key);
        bool montgomery = IsMontgomeryCurve(key);
        if (string.Equals(use, TokenKeyUses.Signature, StringComparison.Ordinal)
            && !edwards
            || string.Equals(use, TokenKeyUses.Encryption, StringComparison.Ordinal)
            && !montgomery)
        {
            return Failure(
                "key_use_type_mismatch",
                "The JWK public key use conflicts with the OKP curve family.");
        }

        return Success();
    }

    static bool IsRegisteredOperation(string operation)
    {
        return string.Equals(operation, TokenKeyOperations.Sign, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.Verify, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.Encrypt, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.Decrypt, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.WrapKey, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.UnwrapKey, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.DeriveKey, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.DeriveBits, StringComparison.Ordinal);
    }

    static bool MatchesUse(string operation, string? use)
    {
        if (use == null)
        {
            return true;
        }

        bool signature = string.Equals(operation, TokenKeyOperations.Sign, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.Verify, StringComparison.Ordinal);
        if (string.Equals(use, TokenKeyUses.Signature, StringComparison.Ordinal))
        {
            return signature;
        }

        return !signature;
    }

    static bool MatchesKeyType(string operation, TokenKey key)
    {
        string? keyType = ResolveKeyType(key);
        if (string.Equals(keyType, TokenKeyTypes.Symmetric, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(keyType, TokenKeyTypes.Rsa, StringComparison.Ordinal))
        {
            return !string.Equals(operation, TokenKeyOperations.DeriveKey, StringComparison.Ordinal)
                && !string.Equals(operation, TokenKeyOperations.DeriveBits, StringComparison.Ordinal);
        }

        if (string.Equals(keyType, TokenKeyTypes.EllipticCurve, StringComparison.Ordinal))
        {
            return string.Equals(operation, TokenKeyOperations.Sign, StringComparison.Ordinal)
                || string.Equals(operation, TokenKeyOperations.Verify, StringComparison.Ordinal)
                || string.Equals(operation, TokenKeyOperations.DeriveKey, StringComparison.Ordinal)
                || string.Equals(operation, TokenKeyOperations.DeriveBits, StringComparison.Ordinal);
        }

        if (string.Equals(keyType, TokenKeyTypes.OctetKeyPair, StringComparison.Ordinal))
        {
            bool edwards = IsEdwardsCurve(key);
            if (edwards)
            {
                return string.Equals(operation, TokenKeyOperations.Sign, StringComparison.Ordinal)
                    || string.Equals(operation, TokenKeyOperations.Verify, StringComparison.Ordinal);
            }

            if (IsMontgomeryCurve(key))
            {
                return string.Equals(
                    operation,
                    TokenKeyOperations.DeriveKey,
                    StringComparison.Ordinal)
                    || string.Equals(
                        operation,
                        TokenKeyOperations.DeriveBits,
                        StringComparison.Ordinal);
            }

            return false;
        }

        if (string.Equals(keyType, TokenKeyTypes.AlgorithmKeyPair, StringComparison.Ordinal))
        {
            return string.Equals(operation, TokenKeyOperations.Sign, StringComparison.Ordinal)
                || string.Equals(operation, TokenKeyOperations.Verify, StringComparison.Ordinal);
        }

        return false;
    }

    static bool MatchesAlgorithm(string operation, string? algorithm)
    {
        if (algorithm == null)
        {
            return true;
        }

        if (JoseAlgorithmsOperations.IsSignature(algorithm))
        {
            return string.Equals(operation, TokenKeyOperations.Sign, StringComparison.Ordinal)
                || string.Equals(operation, TokenKeyOperations.Verify, StringComparison.Ordinal);
        }

        if (JoseAlgorithmsOperations.IsContentEncryption(algorithm)
            || string.Equals(algorithm, JoseAlgorithms.Direct, StringComparison.Ordinal))
        {
            return string.Equals(operation, TokenKeyOperations.Encrypt, StringComparison.Ordinal)
                || string.Equals(operation, TokenKeyOperations.Decrypt, StringComparison.Ordinal);
        }

        if (IsEcdhAlgorithm(algorithm))
        {
            return string.Equals(operation, TokenKeyOperations.DeriveKey, StringComparison.Ordinal)
                || string.Equals(operation, TokenKeyOperations.DeriveBits, StringComparison.Ordinal);
        }

        if (JoseAlgorithmsOperations.IsKeyManagement(algorithm))
        {
            return string.Equals(operation, TokenKeyOperations.WrapKey, StringComparison.Ordinal)
                || string.Equals(operation, TokenKeyOperations.UnwrapKey, StringComparison.Ordinal);
        }

        return true;
    }

    static bool IsEcdhAlgorithm(string algorithm)
    {
        return string.Equals(algorithm, JoseAlgorithms.EcdhEs, StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes192KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes256KeyWrap,
                StringComparison.Ordinal);
    }

    static bool IsEdwardsCurve(TokenKey key)
    {
        return string.Equals(key.Curve, "Ed25519", StringComparison.Ordinal)
            || string.Equals(key.Curve, "Ed448", StringComparison.Ordinal)
            || key.CoseCurve == CoseKeyLabels.CurveEd25519
            || key.CoseCurve == CoseKeyLabels.CurveEd448;
    }

    static bool IsMontgomeryCurve(TokenKey key)
    {
        return string.Equals(key.Curve, "X25519", StringComparison.Ordinal)
            || string.Equals(key.Curve, "X448", StringComparison.Ordinal)
            || key.CoseCurve == CoseKeyLabels.CurveX25519
            || key.CoseCurve == CoseKeyLabels.CurveX448;
    }

    static bool HasCapability(
        string operation,
        TokenKey key,
        bool publicProjection)
    {
        string? keyType = ResolveKeyType(key);
        bool symmetric = string.Equals(
            keyType,
            TokenKeyTypes.Symmetric,
            StringComparison.Ordinal);
        bool privateOperation = string.Equals(
            operation,
            TokenKeyOperations.Sign,
            StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.Decrypt, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.UnwrapKey, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.DeriveKey, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.DeriveBits, StringComparison.Ordinal);
        if (privateOperation)
        {
            if (publicProjection)
            {
                return false;
            }

            return symmetric || key.ContainsPrivateMaterial();
        }

        return symmetric || key.ContainsPublicMaterial();
    }

    static string? ResolveKeyType(TokenKey key)
    {
        if (key.KeyType != null)
        {
            return key.KeyType;
        }

        if (key.CoseKeyType == CoseKeyLabels.KeyTypeSymmetric)
        {
            return TokenKeyTypes.Symmetric;
        }

        if (key.CoseKeyType == CoseKeyLabels.KeyTypeRsa)
        {
            return TokenKeyTypes.Rsa;
        }

        if (key.CoseKeyType == CoseKeyLabels.KeyTypeEc2)
        {
            return TokenKeyTypes.EllipticCurve;
        }

        if (key.CoseKeyType == CoseKeyLabels.KeyTypeOkp)
        {
            return TokenKeyTypes.OctetKeyPair;
        }

        if (key.CoseKeyType == CoseKeyLabels.KeyTypeAlgorithmKeyPair)
        {
            return TokenKeyTypes.AlgorithmKeyPair;
        }

        return null;
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult Failure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
