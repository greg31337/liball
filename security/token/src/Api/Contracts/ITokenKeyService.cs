using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace token;

/// <summary>Provides local cryptographic key import, serialization, conversion, selection, policy, and clearing operations.</summary>
public interface ITokenKeyService
{
    /// <summary>Imports an independent copy of symmetric key material.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportSymmetricKey(byte[] keyBytes);

    /// <summary>Imports symmetric key material bound to a JOSE algorithm.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <param name="algorithm">The JOSE algorithm name.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportJoseSymmetricKey(byte[] keyBytes, string algorithm);

    /// <summary>Imports symmetric key material bound to a COSE algorithm.</summary>
    /// <param name="keyBytes">The secret key bytes.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportCoseSymmetricKey(byte[] keyBytes, int algorithm);

    /// <summary>Imports a caller-owned RSA key.</summary>
    /// <param name="rsa">The native RSA key.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportRsaKey(RSA rsa, bool privateMaterial);

    /// <summary>Imports a caller-owned RSA key bound to a JOSE algorithm.</summary>
    /// <param name="rsa">The native RSA key.</param>
    /// <param name="algorithm">The JOSE algorithm name.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportJoseRsaKey(RSA rsa, string algorithm, bool privateMaterial);

    /// <summary>Imports a caller-owned RSA key bound to a COSE algorithm.</summary>
    /// <param name="rsa">The native RSA key.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportCoseRsaKey(RSA rsa, int algorithm, bool privateMaterial);

    /// <summary>Imports a caller-owned EC2 key.</summary>
    /// <param name="ecdsa">The native ECDSA key.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportEc2Key(ECDsa ecdsa, bool privateMaterial);

    /// <summary>Imports a caller-owned EC2 key bound to a JOSE algorithm.</summary>
    /// <param name="ecdsa">The native ECDSA key.</param>
    /// <param name="algorithm">The JOSE algorithm name.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportJoseEc2Key(ECDsa ecdsa, string algorithm, bool privateMaterial);

    /// <summary>Imports a caller-owned EC2 key bound to a COSE algorithm.</summary>
    /// <param name="ecdsa">The native ECDSA key.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <param name="privateMaterial">Whether private operations are available.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportCoseEc2Key(ECDsa ecdsa, int algorithm, bool privateMaterial);

    /// <summary>Imports independent raw OKP public and optional private material.</summary>
    /// <param name="publicKey">The raw public key bytes.</param>
    /// <param name="privateKey">The raw private key bytes, or <c>null</c>.</param>
    /// <param name="curve">The registered OKP curve name.</param>
    /// <param name="algorithm">The JOSE algorithm name, or <c>null</c>.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportOkpKey(byte[] publicKey, byte[]? privateKey, string curve, string? algorithm);

    /// <summary>Imports an ML-DSA public key when the built-in platform primitive is available.</summary>
    /// <param name="publicKey">The FIPS 204 public key bytes.</param>
    /// <param name="algorithm">The fully specified ML-DSA algorithm name.</param>
    /// <returns>The imported key or stable unsupported or validation failure details.</returns>
    TokenKeyResult ImportMlDsaPublicKey(byte[] publicKey, string algorithm);

    /// <summary>Imports an ML-DSA private seed when the built-in platform primitive is available.</summary>
    /// <param name="privateKey">The 32-byte FIPS 204 private seed.</param>
    /// <param name="algorithm">The fully specified ML-DSA algorithm name.</param>
    /// <returns>The imported key or stable unsupported or validation failure details.</returns>
    TokenKeyResult ImportMlDsaPrivateKey(byte[] privateKey, string algorithm);

    /// <summary>Imports a caller-owned X.509 certificate key.</summary>
    /// <param name="certificate">The end-entity certificate.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportCertificateKey(X509Certificate2 certificate);

    /// <summary>Imports a caller-owned X.509 certificate key and leaf-first chain.</summary>
    /// <param name="certificate">The end-entity certificate.</param>
    /// <param name="certificateChain">The leaf-first certificate chain.</param>
    /// <returns>The imported key or stable failure details.</returns>
    TokenKeyResult ImportCertificateChainKey(X509Certificate2 certificate, X509Certificate2[] certificateChain);

    /// <summary>Parses a strict JSON Web Key.</summary>
    /// <param name="json">The JWK JSON text.</param>
    /// <returns>The parsed key or stable failure details.</returns>
    JwkResult ParseJwk(string json);

    /// <summary>Parses a strict JSON Web Key Set.</summary>
    /// <param name="json">The JWKS JSON text.</param>
    /// <returns>The parsed set or stable failure details.</returns>
    JwksResult ParseJwks(string json);

    /// <summary>Parses a strict COSE_Key.</summary>
    /// <param name="cbor">The COSE_Key CBOR bytes.</param>
    /// <returns>The parsed key or stable failure details.</returns>
    CoseKeyResult ParseCoseKey(byte[] cbor);

    /// <summary>Writes a complete JWK.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    TokenTextResult WriteJwk(TokenKey key);

    /// <summary>Writes the public JWK projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    TokenTextResult WritePublicJwk(TokenKey key);

    /// <summary>Writes the private JWK projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    TokenTextResult WritePrivateJwk(TokenKey key);

    /// <summary>Writes the minimal JWK projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    TokenTextResult WriteMinimalJwk(TokenKey key);

    /// <summary>Writes a complete JWKS.</summary>
    /// <param name="set">The key set to write.</param>
    /// <returns>The JWKS JSON or stable failure details.</returns>
    TokenTextResult WriteJwks(JsonWebKeySet set);

    /// <summary>Writes a JWKS using public projections.</summary>
    /// <param name="set">The key set to write.</param>
    /// <returns>The JWKS JSON or stable failure details.</returns>
    TokenTextResult WritePublicJwks(JsonWebKeySet set);

    /// <summary>Writes a JWKS using private projections.</summary>
    /// <param name="set">The key set to write.</param>
    /// <returns>The JWKS JSON or stable failure details.</returns>
    TokenTextResult WritePrivateJwks(JsonWebKeySet set);

    /// <summary>Writes a JWKS using minimal projections.</summary>
    /// <param name="set">The key set to write.</param>
    /// <returns>The JWKS JSON or stable failure details.</returns>
    TokenTextResult WriteMinimalJwks(JsonWebKeySet set);

    /// <summary>Writes a complete deterministic COSE_Key.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    TokenBytesResult WriteCoseKey(TokenKey key);

    /// <summary>Writes the public deterministic COSE_Key projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    TokenBytesResult WritePublicCoseKey(TokenKey key);

    /// <summary>Writes the private deterministic COSE_Key projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    TokenBytesResult WritePrivateCoseKey(TokenKey key);

    /// <summary>Writes the minimal deterministic COSE_Key projection.</summary>
    /// <param name="key">The key to write.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    TokenBytesResult WriteMinimalCoseKey(TokenKey key);

    /// <summary>Calculates an RFC 7638 SHA-256 JWK thumbprint.</summary>
    /// <param name="key">The key to identify.</param>
    /// <returns>The thumbprint bytes or stable failure details.</returns>
    TokenBytesResult CalculateJwkThumbprint(TokenKey key);

    /// <summary>Calculates an RFC 9278 SHA-256 JWK thumbprint URI.</summary>
    /// <param name="key">The key to identify.</param>
    /// <returns>The thumbprint URI or stable failure details.</returns>
    TokenTextResult CalculateJwkThumbprintUri(TokenKey key);

    /// <summary>Calculates an RFC 9679 SHA-256 COSE key thumbprint.</summary>
    /// <param name="key">The key to identify.</param>
    /// <returns>The thumbprint bytes or stable failure details.</returns>
    TokenBytesResult CalculateCoseKeyThumbprint(TokenKey key);

    /// <summary>Calculates an RFC 9679 SHA-256 COSE key thumbprint URI.</summary>
    /// <param name="key">The key to identify.</param>
    /// <returns>The thumbprint URI or stable failure details.</returns>
    TokenTextResult CalculateCoseKeyThumbprintUri(TokenKey key);

    /// <summary>Converts normalized JOSE metadata and key material to a JWK.</summary>
    /// <param name="key">The normalized key.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    TokenTextResult ConvertKeyToJwk(TokenKey key);

    /// <summary>Converts normalized JOSE metadata and key material to a COSE_Key.</summary>
    /// <param name="key">The normalized key.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    TokenBytesResult ConvertKeyToCoseKey(TokenKey key);

    /// <summary>Converts serialized JWK JSON to a deterministic COSE_Key.</summary>
    /// <param name="jwk">The JWK JSON text.</param>
    /// <returns>The COSE_Key bytes or stable failure details.</returns>
    TokenBytesResult ConvertJwkToCoseKey(string jwk);

    /// <summary>Converts serialized COSE_Key bytes to compact JWK JSON.</summary>
    /// <param name="coseKey">The COSE_Key bytes.</param>
    /// <returns>The JWK JSON or stable failure details.</returns>
    TokenTextResult ConvertCoseKeyToJwk(byte[] coseKey);

    /// <summary>Selects the unique key matching every active query criterion.</summary>
    /// <param name="set">The key set to search.</param>
    /// <param name="query">The selection criteria.</param>
    /// <returns>The matching key or stable failure details.</returns>
    TokenKeyResult SelectKey(JsonWebKeySet set, TokenKeyQuery query);

    /// <summary>Enforces JOSE and COSE metadata for a requested normalized key operation.</summary>
    /// <param name="key">The key to inspect.</param>
    /// <param name="operation">The normalized operation name.</param>
    /// <returns>Success when the operation is permitted; otherwise, stable failure details.</returns>
    TokenResult EnforceKeyOperation(TokenKey key, string operation);

    /// <summary>Enforces JOSE and COSE metadata for a requested COSE key operation.</summary>
    /// <param name="key">The key to inspect.</param>
    /// <param name="operation">The COSE key operation label.</param>
    /// <returns>Success when the operation is permitted; otherwise, stable failure details.</returns>
    TokenResult EnforceCoseKeyOperation(TokenKey key, int operation);

    /// <summary>Clears owned secret and native material from a key.</summary>
    /// <param name="key">The key to clear.</param>
    void ClearKeyMaterial(TokenKey key);

    /// <summary>Clears every byte in a temporary secret buffer.</summary>
    /// <param name="temporaryMaterial">The temporary secret buffer.</param>
    void ClearTemporaryMaterial(byte[] temporaryMaterial);
}
