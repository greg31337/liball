using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace token;

/// <summary>Implements local key import, serialization, conversion, selection, policy, and clearing operations.</summary>
public class TokenKeyService : ITokenKeyService
{
    const string CoseThumbprintUriPrefix = "urn:ietf:params:oauth:ckt:sha-256:";

    /// <summary>Initializes a stateless key service.</summary>
    public TokenKeyService()
    {
    }

    /// <summary>Imports an independent copy of symmetric key material.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportSymmetricKey(byte[] keyBytes)
    {
        return SymmetricKeyImporterOperations.Import(keyBytes);
    }

    /// <summary>Imports symmetric key material bound to a JOSE algorithm.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <param name="algorithm">The JOSE algorithm name.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportJoseSymmetricKey(byte[] keyBytes, string algorithm)
    {
        return SymmetricKeyImporterOperations.ImportJose(keyBytes, algorithm);
    }

    /// <summary>Imports symmetric key material bound to a COSE algorithm.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportCoseSymmetricKey(byte[] keyBytes, int algorithm)
    {
        return SymmetricKeyImporterOperations.ImportCose(keyBytes, algorithm);
    }

    /// <summary>Imports a caller-owned RSA key.</summary>
    /// <param name="rsa">The native RSA key.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportRsaKey(RSA rsa, bool privateMaterial)
    {
        return RsaKeyImporterOperations.Import(rsa, privateMaterial);
    }

    /// <summary>Imports a caller-owned RSA key bound to a JOSE algorithm.</summary>
    /// <param name="rsa">The native RSA key.</param>
    /// <param name="algorithm">The JOSE algorithm name.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportJoseRsaKey(RSA rsa, string algorithm, bool privateMaterial)
    {
        return RsaKeyImporterOperations.ImportJose(rsa, algorithm, privateMaterial);
    }

    /// <summary>Imports a caller-owned RSA key bound to a COSE algorithm.</summary>
    /// <param name="rsa">The native RSA key.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportCoseRsaKey(RSA rsa, int algorithm, bool privateMaterial)
    {
        return RsaKeyImporterOperations.ImportCose(rsa, algorithm, privateMaterial);
    }

    /// <summary>Imports a caller-owned EC2 key.</summary>
    /// <param name="ecdsa">The native ECDSA key.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportEc2Key(ECDsa ecdsa, bool privateMaterial)
    {
        return EcdsaKeyImporter.Import(ecdsa, privateMaterial);
    }

    /// <summary>Imports a caller-owned EC2 key bound to a JOSE algorithm.</summary>
    /// <param name="ecdsa">The native ECDSA key.</param>
    /// <param name="algorithm">The JOSE algorithm name.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportJoseEc2Key(ECDsa ecdsa, string algorithm, bool privateMaterial)
    {
        return EcdsaKeyImporter.ImportJose(ecdsa, algorithm, privateMaterial);
    }

    /// <summary>Imports a caller-owned EC2 key bound to a COSE algorithm.</summary>
    /// <param name="ecdsa">The native ECDSA key.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportCoseEc2Key(ECDsa ecdsa, int algorithm, bool privateMaterial)
    {
        return EcdsaKeyImporter.ImportCose(ecdsa, algorithm, privateMaterial);
    }

    /// <summary>Imports independent raw OKP public and optional private material.</summary>
    /// <param name="publicKey">The raw public key bytes.</param>
    /// <param name="privateKey">The raw private key bytes, or <c>null</c>.</param>
    /// <param name="curve">The registered OKP curve name.</param>
    /// <param name="algorithm">The JOSE algorithm name, or <c>null</c>.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportOkpKey(
        byte[] publicKey,
        byte[]? privateKey,
        string curve,
        string? algorithm)
    {
        if (publicKey == null)
        {
            throw new ArgumentNullException("publicKey");
        }

        if (curve == null)
        {
            throw new ArgumentNullException("curve");
        }

        JwkOkpCurve curveInfo = JsonWebKey.GetOkpCurve(curve);
        if (curveInfo.Error != null)
        {
            return KeyFailureError(curveInfo.Error);
        }

        if (publicKey.Length != curveInfo.Size
            || privateKey != null && privateKey.Length != curveInfo.Size)
        {
            return KeyFailure(
                "invalid_okp_key",
                "The OKP key material does not have the required curve length.");
        }

        TokenError? algorithmError = JsonWebKey.ValidateOkpAlgorithm(curve, algorithm);
        if (algorithmError != null)
        {
            return KeyFailureError(algorithmError);
        }

        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.OctetKeyPair;
        key.CoseKeyType = CoseKeyLabels.KeyTypeOkp;
        key.Curve = curve;
        key.CoseCurve = curveInfo.CoseCurve;
        key.Algorithm = algorithm;
        key.OkpPublicKey = publicKey.AsSpan().ToArray();
        if (privateKey != null)
        {
            key.OkpPrivateKey = privateKey.AsSpan().ToArray();
        }

        key.NativePublicMaterial = true;
        key.NativePrivateMaterial = privateKey != null;
        return KeySuccess(key);
    }

    /// <summary>Imports an ML-DSA public key when the built-in platform primitive is available.</summary>
    /// <param name="publicKey">The FIPS 204 public key bytes.</param>
    /// <param name="algorithm">The fully specified ML-DSA algorithm name.</param>
    /// <returns>The imported key or stable unsupported or validation failure details.</returns>
    public TokenKeyResult ImportMlDsaPublicKey(byte[] publicKey, string algorithm)
    {
        if (publicKey == null)
        {
            throw new ArgumentNullException("publicKey");
        }

        return MlDsaKeyImporter.ImportPublic(publicKey, algorithm);
    }

    /// <summary>Imports an ML-DSA private seed when the built-in platform primitive is available.</summary>
    /// <param name="privateKey">The 32-byte FIPS 204 private seed.</param>
    /// <param name="algorithm">The fully specified ML-DSA algorithm name.</param>
    /// <returns>The imported key or stable unsupported or validation failure details.</returns>
    public TokenKeyResult ImportMlDsaPrivateKey(byte[] privateKey, string algorithm)
    {
        if (privateKey == null)
        {
            throw new ArgumentNullException("privateKey");
        }

        return MlDsaKeyImporter.ImportPrivateSeed(privateKey, algorithm);
    }

    /// <summary>Imports a caller-owned X.509 certificate key.</summary>
    /// <param name="certificate">The end-entity certificate.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportCertificateKey(X509Certificate2 certificate)
    {
        return X509KeyHelpers.Import(certificate);
    }

    /// <summary>Imports a caller-owned X.509 certificate key and leaf-first chain.</summary>
    /// <param name="certificate">The end-entity certificate.</param>
    /// <param name="certificateChain">The leaf-first certificate chain.</param>
    /// <returns>The imported key or stable failure details.</returns>
    public TokenKeyResult ImportCertificateChainKey(
        X509Certificate2 certificate,
        X509Certificate2[] certificateChain)
    {
        return X509KeyHelpers.Import(certificate, certificateChain);
    }

    /// <summary>Parses a strict JSON Web Key.</summary>
    /// <param name="json">The JWK JSON text.</param>
    /// <returns>The parsed key or stable failure details.</returns>
    public JwkResult ParseJwk(string json)
    {
        return JsonWebKey.Read(json);
    }

    /// <summary>Parses a strict JSON Web Key Set.</summary>
    /// <param name="json">The JWKS JSON text.</param>
    /// <returns>The parsed set or stable failure details.</returns>
    public JwksResult ParseJwks(string json)
    {
        return JsonWebKeySet.Read(json);
    }

    /// <summary>Parses a strict COSE_Key.</summary>
    /// <param name="cbor">The COSE_Key CBOR bytes.</param>
    /// <returns>The parsed key or stable failure details.</returns>
    public CoseKeyResult ParseCoseKey(byte[] cbor)
    {
        return CoseKey.Read(cbor);
    }

    /// <summary>Writes a complete JWK.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    public TokenTextResult WriteJwk(TokenKey key)
    {
        return JsonWebKey.Write(key);
    }

    /// <summary>Writes the public JWK projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    public TokenTextResult WritePublicJwk(TokenKey key)
    {
        return JsonWebKey.WritePublic(key);
    }

    /// <summary>Writes the private JWK projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    public TokenTextResult WritePrivateJwk(TokenKey key)
    {
        return JsonWebKey.WritePrivate(key);
    }

    /// <summary>Writes the minimal JWK projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    public TokenTextResult WriteMinimalJwk(TokenKey key)
    {
        return JsonWebKey.WriteMinimal(key);
    }

    /// <summary>Writes a complete JWKS.</summary>
    /// <param name="set">The key set to write.</param>
    /// <returns>The JWKS JSON or stable failure details.</returns>
    public TokenTextResult WriteJwks(JsonWebKeySet set)
    {
        return WriteJwksProjection(set, 0);
    }

    /// <summary>Writes a JWKS using public projections.</summary>
    /// <param name="set">The key set to write.</param>
    /// <returns>The JWKS JSON or stable failure details.</returns>
    public TokenTextResult WritePublicJwks(JsonWebKeySet set)
    {
        return WriteJwksProjection(set, 1);
    }

    /// <summary>Writes a JWKS using private projections.</summary>
    /// <param name="set">The key set to write.</param>
    /// <returns>The JWKS JSON or stable failure details.</returns>
    public TokenTextResult WritePrivateJwks(JsonWebKeySet set)
    {
        return WriteJwksProjection(set, 2);
    }

    /// <summary>Writes a JWKS using minimal projections.</summary>
    /// <param name="set">The key set to write.</param>
    /// <returns>The JWKS JSON or stable failure details.</returns>
    public TokenTextResult WriteMinimalJwks(JsonWebKeySet set)
    {
        return WriteJwksProjection(set, 3);
    }

    /// <summary>Writes a complete deterministic COSE_Key.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    public TokenBytesResult WriteCoseKey(TokenKey key)
    {
        return CoseKeyWriter.WriteProjection(key, CoseKeyWriteProjection.Complete);
    }

    /// <summary>Writes the public deterministic COSE_Key projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    public TokenBytesResult WritePublicCoseKey(TokenKey key)
    {
        return CoseKeyWriter.WriteProjection(key, CoseKeyWriteProjection.Public);
    }

    /// <summary>Writes the private deterministic COSE_Key projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    public TokenBytesResult WritePrivateCoseKey(TokenKey key)
    {
        return CoseKeyWriter.WriteProjection(key, CoseKeyWriteProjection.Private);
    }

    /// <summary>Writes the minimal deterministic COSE_Key projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    public TokenBytesResult WriteMinimalCoseKey(TokenKey key)
    {
        return CoseKeyWriter.WriteProjection(key, CoseKeyWriteProjection.Minimal);
    }

    /// <summary>Calculates an RFC 7638 SHA-256 JWK thumbprint.</summary>
    /// <param name="key">The key to identify.</param>
    /// <returns>The thumbprint bytes or stable failure details.</returns>
    public TokenBytesResult CalculateJwkThumbprint(TokenKey key)
    {
        return JsonWebKey.Thumbprint(key);
    }

    /// <summary>Calculates an RFC 9278 SHA-256 JWK thumbprint URI.</summary>
    /// <param name="key">The key to identify.</param>
    /// <returns>The thumbprint URI or stable failure details.</returns>
    public TokenTextResult CalculateJwkThumbprintUri(TokenKey key)
    {
        return JsonWebKey.ThumbprintUri(key);
    }

    /// <summary>Calculates an RFC 9679 SHA-256 COSE key thumbprint.</summary>
    /// <param name="key">The key to identify.</param>
    /// <returns>The thumbprint bytes or stable failure details.</returns>
    public TokenBytesResult CalculateCoseKeyThumbprint(TokenKey key)
    {
        TokenBytesResult encoded = CoseKeyWriter.WriteProjection(
            key,
            CoseKeyWriteProjection.Thumbprint);
        if (!encoded.Success)
        {
            return encoded;
        }

        TokenBytesResult result = new TokenBytesResult();
        byte[] canonical = encoded.Bytes ?? throw new InvalidOperationException("A required value is null.");
        result.Bytes = SHA256.HashData(canonical);
        CryptographicOperations.ZeroMemory(canonical);
        result.Success = true;
        return result;
    }

    /// <summary>Calculates an RFC 9679 SHA-256 COSE key thumbprint URI.</summary>
    /// <param name="key">The key to identify.</param>
    /// <returns>The thumbprint URI or stable failure details.</returns>
    public TokenTextResult CalculateCoseKeyThumbprintUri(TokenKey key)
    {
        TokenBytesResult thumbprint = CalculateCoseKeyThumbprint(key);
        if (!thumbprint.Success)
        {
            return TextFailureError(thumbprint.Error);
        }

        TokenTextResult encoded = Base64Url.Encode((thumbprint.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        CryptographicOperations.ZeroMemory(thumbprint.Bytes);
        if (!encoded.Success)
        {
            return encoded;
        }

        TokenTextResult result = new TokenTextResult();
        result.Text = CoseThumbprintUriPrefix + encoded.Text;
        result.Success = true;
        return result;
    }

    /// <summary>Converts normalized JOSE metadata and key material to a JWK.</summary>
    /// <param name="key">The normalized key.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    public TokenTextResult ConvertKeyToJwk(TokenKey key)
    {
        return TokenKeyConverter.ToJwk(key);
    }

    /// <summary>Converts normalized JOSE metadata and key material to a COSE_Key.</summary>
    /// <param name="key">The normalized key.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    public TokenBytesResult ConvertKeyToCoseKey(TokenKey key)
    {
        return TokenKeyConverter.ToCoseKey(key);
    }

    /// <summary>Converts serialized JWK JSON to a deterministic COSE_Key.</summary>
    /// <param name="jwk">The JWK JSON text.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    public TokenBytesResult ConvertJwkToCoseKey(string jwk)
    {
        return TokenKeyConverter.JwkToCoseKey(jwk);
    }

    /// <summary>Converts serialized COSE_Key bytes to compact JWK JSON.</summary>
    /// <param name="coseKey">The COSE_Key bytes.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    public TokenTextResult ConvertCoseKeyToJwk(byte[] coseKey)
    {
        return TokenKeyConverter.CoseKeyToJwk(coseKey);
    }

    /// <summary>Selects the unique key matching every active query criterion.</summary>
    /// <param name="set">The key set to search.</param>
    /// <param name="query">The selection criteria.</param>
    /// <returns>The matching key or stable failure details.</returns>
    public TokenKeyResult SelectKey(JsonWebKeySet set, TokenKeyQuery query)
    {
        if (set == null)
        {
            throw new ArgumentNullException("set");
        }

        return set.FindKey(query);
    }

    /// <summary>Enforces JOSE and COSE metadata for a requested normalized key operation.</summary>
    /// <param name="key">The key to inspect.</param>
    /// <param name="operation">The normalized operation name.</param>
    /// <returns>Success when the operation is permitted; otherwise, stable failure details.</returns>
    public TokenResult EnforceKeyOperation(TokenKey key, string operation)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (operation == null)
        {
            throw new ArgumentNullException("operation");
        }

        int coseOperation = GetCoseOperation(operation);
        if (coseOperation == 0)
        {
            return ResultFailure("unknown_key_operation", "The requested key operation is not registered.");
        }

        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !ContainsOperation(key.KeyOperations, operation))
        {
            return ResultFailure("key_operation_not_permitted", "The JWK key operations do not permit the requested operation.");
        }

        if (key.CoseKeyOperations != null
            && key.CoseKeyOperations.Length > 0
            && !ContainsCoseOperation(key.CoseKeyOperations, coseOperation))
        {
            return ResultFailure("key_operation_not_permitted", "The COSE key operations do not permit the requested operation.");
        }

        if (!MatchesUse(key.Use, operation))
        {
            return ResultFailure("key_operation_not_permitted", "The public key use does not permit the requested operation.");
        }

        if (!HasOperationMaterial(key, operation))
        {
            return ResultFailure("key_operation_unavailable", "The key lacks material required by the requested operation.");
        }

        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    /// <summary>Enforces JOSE and COSE metadata for a requested COSE key operation.</summary>
    /// <param name="key">The key to inspect.</param>
    /// <param name="operation">The COSE key operation label.</param>
    /// <returns>Success when the operation is permitted; otherwise, stable failure details.</returns>
    public TokenResult EnforceCoseKeyOperation(TokenKey key, int operation)
    {
        string? name = GetKeyOperationName(operation);
        if (name == null)
        {
            return ResultFailure("unknown_key_operation", "The requested COSE key operation is not registered.");
        }

        return EnforceKeyOperation(key, name);
    }

    /// <summary>Clears owned secret and native material from a key.</summary>
    /// <param name="key">The key to clear.</param>
    public void ClearKeyMaterial(TokenKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        key.ReleaseOwnedMaterial();
    }

    /// <summary>Clears every byte in a temporary secret buffer.</summary>
    /// <param name="temporaryMaterial">The temporary secret buffer.</param>
    public void ClearTemporaryMaterial(byte[] temporaryMaterial)
    {
        if (temporaryMaterial == null)
        {
            throw new ArgumentNullException("temporaryMaterial");
        }

        CryptographicOperations.ZeroMemory(temporaryMaterial);
    }

    internal static TokenTextResult WriteJwksProjection(JsonWebKeySet set, int projection)
    {
        if (set == null)
        {
            throw new ArgumentNullException("set");
        }

        if (set.Keys == null)
        {
            return TextFailure("invalid_jwks_keys", "The JWK Set keys array is null.");
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("{\"keys\":[");
        int index = 0;
        while (index < set.Keys.Length)
        {
            TokenKey key = set.Keys[index];
            if (key == null)
            {
                return TextFailure("invalid_jwks_key", "The JWK Set contains a null key.");
            }

            TokenTextResult written;
            if (projection == 1)
            {
                written = JsonWebKey.WritePublic(key);
            }
            else if (projection == 2)
            {
                written = JsonWebKey.WritePrivate(key);
            }
            else if (projection == 3)
            {
                written = JsonWebKey.WriteMinimal(key);
            }
            else
            {
                written = JsonWebKey.Write(key);
            }

            if (!written.Success)
            {
                return written;
            }

            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(written.Text);
            index++;
        }

        builder.Append("]}");
        TokenTextResult result = new TokenTextResult();
        result.Text = builder.ToString();
        result.Success = true;
        return result;
    }

    internal static int GetCoseOperation(string operation)
    {
        if (string.Equals(operation, TokenKeyOperations.Sign, StringComparison.Ordinal)) return CoseKeyLabels.OperationSign;
        if (string.Equals(operation, TokenKeyOperations.Verify, StringComparison.Ordinal)) return CoseKeyLabels.OperationVerify;
        if (string.Equals(operation, TokenKeyOperations.Encrypt, StringComparison.Ordinal)) return CoseKeyLabels.OperationEncrypt;
        if (string.Equals(operation, TokenKeyOperations.Decrypt, StringComparison.Ordinal)) return CoseKeyLabels.OperationDecrypt;
        if (string.Equals(operation, TokenKeyOperations.WrapKey, StringComparison.Ordinal)) return CoseKeyLabels.OperationWrapKey;
        if (string.Equals(operation, TokenKeyOperations.UnwrapKey, StringComparison.Ordinal)) return CoseKeyLabels.OperationUnwrapKey;
        if (string.Equals(operation, TokenKeyOperations.DeriveKey, StringComparison.Ordinal)) return CoseKeyLabels.OperationDeriveKey;
        if (string.Equals(operation, TokenKeyOperations.DeriveBits, StringComparison.Ordinal)) return CoseKeyLabels.OperationDeriveBits;
        if (string.Equals(operation, TokenKeyOperations.MacCreate, StringComparison.Ordinal)) return CoseKeyLabels.OperationMacCreate;
        if (string.Equals(operation, TokenKeyOperations.MacVerify, StringComparison.Ordinal)) return CoseKeyLabels.OperationMacVerify;
        return 0;
    }

    internal static string? GetKeyOperationName(int operation)
    {
        if (operation == CoseKeyLabels.OperationSign) return TokenKeyOperations.Sign;
        if (operation == CoseKeyLabels.OperationVerify) return TokenKeyOperations.Verify;
        if (operation == CoseKeyLabels.OperationEncrypt) return TokenKeyOperations.Encrypt;
        if (operation == CoseKeyLabels.OperationDecrypt) return TokenKeyOperations.Decrypt;
        if (operation == CoseKeyLabels.OperationWrapKey) return TokenKeyOperations.WrapKey;
        if (operation == CoseKeyLabels.OperationUnwrapKey) return TokenKeyOperations.UnwrapKey;
        if (operation == CoseKeyLabels.OperationDeriveKey) return TokenKeyOperations.DeriveKey;
        if (operation == CoseKeyLabels.OperationDeriveBits) return TokenKeyOperations.DeriveBits;
        if (operation == CoseKeyLabels.OperationMacCreate) return TokenKeyOperations.MacCreate;
        if (operation == CoseKeyLabels.OperationMacVerify) return TokenKeyOperations.MacVerify;
        return null;
    }

    internal static bool ContainsOperation(string[] operations, string operation)
    {
        int index = 0;
        while (index < operations.Length)
        {
            if (string.Equals(operations[index], operation, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static bool ContainsCoseOperation(int[] operations, int operation)
    {
        int index = 0;
        while (index < operations.Length)
        {
            if (operations[index] == operation)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static bool MatchesUse(string? use, string operation)
    {
        if (use == null)
        {
            return true;
        }

        bool signature = string.Equals(operation, TokenKeyOperations.Sign, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.Verify, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.MacCreate, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.MacVerify, StringComparison.Ordinal);
        if (string.Equals(use, TokenKeyUses.Signature, StringComparison.Ordinal))
        {
            return signature;
        }

        if (string.Equals(use, TokenKeyUses.Encryption, StringComparison.Ordinal))
        {
            return !signature;
        }

        return false;
    }

    internal static bool HasOperationMaterial(TokenKey key, string operation)
    {
        bool secretOperation = string.Equals(operation, TokenKeyOperations.Sign, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.Decrypt, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.UnwrapKey, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.DeriveKey, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.DeriveBits, StringComparison.Ordinal)
            || string.Equals(operation, TokenKeyOperations.MacCreate, StringComparison.Ordinal);
        if (secretOperation)
        {
            return key.ContainsPrivateMaterial();
        }

        return key.ContainsPublicMaterial() || key.ContainsPrivateMaterial();
    }

    internal static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Key = key;
        result.Success = true;
        return result;
    }

    internal static TokenKeyResult KeyFailure(string code, string message)
    {
        return KeyFailureError(new TokenError(code, message, -1));
    }

    internal static TokenKeyResult KeyFailureError(TokenError? error)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = error;
        return result;
    }

    internal static TokenResult ResultFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    internal static TokenTextResult TextFailure(string code, string message)
    {
        return TextFailureError(new TokenError(code, message, -1));
    }

    internal static TokenTextResult TextFailureError(TokenError? error)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = error;
        return result;
    }
}
