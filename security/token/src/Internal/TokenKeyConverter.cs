using System;
namespace token;

/// <summary>Converts normalized keys and serialized JWK and COSE_Key representations.</summary>
/// <remarks>
/// Text key identifiers are represented as strict UTF-8 COSE byte-string identifiers. Only key
/// operations shared by JWK and COSE are converted. Input keys and serialized inputs are not
/// modified, and serialized outputs are independent of their inputs.
/// </remarks>
internal static class TokenKeyConverter
{
    /// <summary>Writes a normalized key as a JWK after converting COSE metadata.</summary>
    /// <param name="key">The normalized key.</param>
    /// <returns>A result containing compact JWK JSON or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenTextResult ToJwk(TokenKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        TokenKeyConversionState converted = PrepareJwk(key);
        if (converted.Error != null)
        {
            return TextFailure(converted.Error);
        }

        return JsonWebKey.Write((converted.Key ?? throw new InvalidOperationException("A required value is null.")));
    }

    /// <summary>Writes a normalized key as a deterministic COSE_Key after converting JWK metadata.</summary>
    /// <param name="key">The normalized key.</param>
    /// <returns>A result containing deterministic CBOR or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenBytesResult ToCoseKey(TokenKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        TokenKeyConversionState converted = PrepareCose(key);
        if (converted.Error != null)
        {
            return BytesFailure(converted.Error);
        }

        return CoseKey.Write((converted.Key ?? throw new InvalidOperationException("A required value is null.")));
    }

    /// <summary>Converts serialized JWK JSON to a deterministic COSE_Key.</summary>
    /// <param name="jwk">The strict JWK JSON text.</param>
    /// <returns>A result containing deterministic CBOR or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>jwk</c> is null.</exception>
    internal static TokenBytesResult JwkToCoseKey(string jwk)
    {
        if (jwk == null)
        {
            throw new ArgumentNullException("jwk");
        }

        JwkResult parsed = JsonWebKey.Read(jwk);
        if (!parsed.Success)
        {
            return BytesFailure(parsed.Error);
        }

        TokenBytesResult result = ToCoseKey((parsed.Key ?? throw new InvalidOperationException("A required value is null.")));
        parsed.Key.ReleaseOwnedMaterial();
        return result;
    }

    /// <summary>Converts serialized COSE_Key CBOR to compact JWK JSON.</summary>
    /// <param name="coseKey">The strict COSE_Key bytes.</param>
    /// <returns>A result containing compact JWK JSON or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>coseKey</c> is null.</exception>
    internal static TokenTextResult CoseKeyToJwk(byte[] coseKey)
    {
        if (coseKey == null)
        {
            throw new ArgumentNullException("coseKey");
        }

        CoseKeyResult parsed = CoseKey.Read(coseKey);
        if (!parsed.Success)
        {
            return TextFailure(parsed.Error);
        }

        TokenTextResult result = ToJwk((parsed.Key ?? throw new InvalidOperationException("A required value is null.")));
        parsed.Key.ReleaseOwnedMaterial();
        return result;
    }

    static TokenKeyConversionState PrepareJwk(TokenKey source)
    {
        TokenKeyConversionState result = new TokenKeyConversionState();
        result.Key = Copy(source);
        if (source.KeyIdBytes != null)
        {
            TokenTextResult decoded = Utf8Codec.Decode(source.KeyIdBytes);
            if (!decoded.Success)
            {
                result.Error = decoded.Error;
                return result;
            }

            if (source.KeyId != null
                && !string.Equals(source.KeyId, decoded.Text, StringComparison.Ordinal))
            {
                result.Error = Error(
                    "conflicting_key_id",
                    "The JWK and COSE key identifiers are not equivalent.");
                return result;
            }

            result.Key.KeyId = decoded.Text;
        }

        if (source.CoseKeyOperations != null && source.CoseKeyOperations.Length > 0)
        {
            string[] mapped = MapToJwkOperations(source.CoseKeyOperations, result);
            if (result.Error != null)
            {
                return result;
            }

            if (source.KeyOperations != null && source.KeyOperations.Length > 0
                && !Equal(source.KeyOperations, mapped))
            {
                result.Error = Error(
                    "conflicting_key_operations",
                    "The JWK and COSE key operations are not equivalent.");
                return result;
            }

            result.Key.KeyOperations = mapped;
        }

        return result;
    }

    static TokenKeyConversionState PrepareCose(TokenKey source)
    {
        TokenKeyConversionState result = new TokenKeyConversionState();
        result.Key = Copy(source);
        if (source.KeyId != null)
        {
            TokenBytesResult encoded = Utf8Codec.Encode(source.KeyId);
            if (!encoded.Success)
            {
                result.Error = encoded.Error;
                return result;
            }

            if (source.KeyIdBytes != null
                && !ConstantTime.AreEqual(source.KeyIdBytes, (encoded.Bytes ?? throw new InvalidOperationException("A required value is null."))))
            {
                result.Error = Error(
                    "conflicting_key_id",
                    "The JWK and COSE key identifiers are not equivalent.");
                return result;
            }

            result.Key.KeyIdBytes = encoded.Bytes;
        }

        if (source.KeyOperations != null && source.KeyOperations.Length > 0)
        {
            int[] mapped = MapToCoseOperations(source.KeyOperations, result);
            if (result.Error != null)
            {
                return result;
            }

            if (source.CoseKeyOperations != null && source.CoseKeyOperations.Length > 0
                && !Equal(source.CoseKeyOperations, mapped))
            {
                result.Error = Error(
                    "conflicting_key_operations",
                    "The JWK and COSE key operations are not equivalent.");
                return result;
            }

            result.Key.CoseKeyOperations = mapped;
        }

        return result;
    }

    static string[] MapToJwkOperations(
        int[] operations,
        TokenKeyConversionState state)
    {
        string[] result = new string[operations.Length];
        int index = 0;
        while (index < operations.Length)
        {
            string? operation = ToJwkOperation(operations[index]);
            if (operation == null)
            {
                state.Error = Error(
                    "unmapped_key_operation",
                    "A COSE key operation has no JWK equivalent.");
                return result;
            }

            result[index] = operation;
            index++;
        }

        return result;
    }

    static int[] MapToCoseOperations(
        string[] operations,
        TokenKeyConversionState state)
    {
        int[] result = new int[operations.Length];
        int index = 0;
        while (index < operations.Length)
        {
            result[index] = ToCoseOperation(operations[index]);
            if (result[index] == 0)
            {
                state.Error = Error(
                    "unmapped_key_operation",
                    "A JWK key operation has no COSE equivalent.");
                return result;
            }

            index++;
        }

        return result;
    }

    static int ToCoseOperation(string operation)
    {
        if (string.Equals(operation, TokenKeyOperations.Sign, StringComparison.Ordinal))
        {
            return CoseKeyLabels.OperationSign;
        }

        if (string.Equals(operation, TokenKeyOperations.Verify, StringComparison.Ordinal))
        {
            return CoseKeyLabels.OperationVerify;
        }

        if (string.Equals(operation, TokenKeyOperations.Encrypt, StringComparison.Ordinal))
        {
            return CoseKeyLabels.OperationEncrypt;
        }

        if (string.Equals(operation, TokenKeyOperations.Decrypt, StringComparison.Ordinal))
        {
            return CoseKeyLabels.OperationDecrypt;
        }

        if (string.Equals(operation, TokenKeyOperations.WrapKey, StringComparison.Ordinal))
        {
            return CoseKeyLabels.OperationWrapKey;
        }

        if (string.Equals(operation, TokenKeyOperations.UnwrapKey, StringComparison.Ordinal))
        {
            return CoseKeyLabels.OperationUnwrapKey;
        }

        if (string.Equals(operation, TokenKeyOperations.DeriveKey, StringComparison.Ordinal))
        {
            return CoseKeyLabels.OperationDeriveKey;
        }

        if (string.Equals(operation, TokenKeyOperations.DeriveBits, StringComparison.Ordinal))
        {
            return CoseKeyLabels.OperationDeriveBits;
        }

        return 0;
    }

    static string? ToJwkOperation(int operation)
    {
        if (operation == CoseKeyLabels.OperationSign)
        {
            return TokenKeyOperations.Sign;
        }

        if (operation == CoseKeyLabels.OperationVerify)
        {
            return TokenKeyOperations.Verify;
        }

        if (operation == CoseKeyLabels.OperationEncrypt)
        {
            return TokenKeyOperations.Encrypt;
        }

        if (operation == CoseKeyLabels.OperationDecrypt)
        {
            return TokenKeyOperations.Decrypt;
        }

        if (operation == CoseKeyLabels.OperationWrapKey)
        {
            return TokenKeyOperations.WrapKey;
        }

        if (operation == CoseKeyLabels.OperationUnwrapKey)
        {
            return TokenKeyOperations.UnwrapKey;
        }

        if (operation == CoseKeyLabels.OperationDeriveKey)
        {
            return TokenKeyOperations.DeriveKey;
        }

        if (operation == CoseKeyLabels.OperationDeriveBits)
        {
            return TokenKeyOperations.DeriveBits;
        }

        return null;
    }

    static bool Equal(string[] left, string[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool Equal(int[] left, int[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static TokenKey Copy(TokenKey source)
    {
        TokenKey result = new TokenKey();
        result.KeyId = source.KeyId;
        result.KeyIdBytes = source.KeyIdBytes;
        result.KeyType = source.KeyType;
        result.CoseKeyType = source.CoseKeyType;
        result.Algorithm = source.Algorithm;
        result.CoseAlgorithm = source.CoseAlgorithm;
        result.HasCoseAlgorithm = source.HasCoseAlgorithm;
        result.Use = source.Use;
        result.KeyOperations = source.KeyOperations;
        result.CoseKeyOperations = source.CoseKeyOperations;
        result.BaseInitializationVector = source.BaseInitializationVector;
        result.Curve = source.Curve;
        result.CoseCurve = source.CoseCurve;
        result.SymmetricKey = source.SymmetricKey;
        result.Rsa = source.Rsa;
        result.Ecdsa = source.Ecdsa;
        result.OkpPublicKey = source.OkpPublicKey;
        result.OkpPrivateKey = source.OkpPrivateKey;
        result.AlgorithmPublicKey = source.AlgorithmPublicKey;
        result.AlgorithmPrivateKey = source.AlgorithmPrivateKey;
        result.Certificate = source.Certificate;
        result.CertificateChain = source.CertificateChain;
        result.CertificateSha1Thumbprint = source.CertificateSha1Thumbprint;
        result.CertificateSha256Thumbprint = source.CertificateSha256Thumbprint;
        result.NativePrivateMaterial = source.NativePrivateMaterial;
        result.NativePublicMaterial = source.NativePublicMaterial;
        result.OwnsNativeKey = false;
        return result;
    }

    static TokenError Error(string code, string message)
    {
        return new TokenError(code, message, -1);
    }

    static TokenTextResult TextFailure(TokenError? error)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = error;
        return result;
    }

    static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
