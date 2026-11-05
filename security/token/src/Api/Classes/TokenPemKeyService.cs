using System;
using System.Security.Cryptography;
namespace token;

/// <summary>
/// Implements PEM import and export for RSA and ECDSA keys.
/// </summary>
public class TokenPemKeyService : ITokenPemKeyService
{
    /// <summary>Initializes a stateless PEM key service.</summary>
    public TokenPemKeyService()
    {
    }

    /// <summary>Imports an RSA public key from a strict PEM block.</summary>
    /// <param name="pem">A PUBLIC KEY or RSA PUBLIC KEY PEM block.</param>
    /// <returns>An owned RSA key, or stable failure details.</returns>
    /// <exception cref="CryptographicException">Thrown when the decoded key data is not a valid RSA key.</exception>
    public TokenKeyResult ImportRsaPublicKeyPem(string pem)
    {
        PemReadResult readResult = PemCodec.Read(pem);
        if (!readResult.Success)
        {
            return CreatePemKeyFailure(readResult.Error);
        }

        string label = GetPemLabel(readResult);
        if (!string.Equals(label, "PUBLIC KEY", StringComparison.Ordinal)
            && !string.Equals(label, "RSA PUBLIC KEY", StringComparison.Ordinal))
        {
            return CreatePemKeyTypeFailure("An RSA public key PEM block is required.");
        }

        return ImportRsaPemCore(pem, false);
    }

    /// <summary>Imports an RSA private key from a strict PEM block.</summary>
    /// <param name="pem">A PRIVATE KEY or RSA PRIVATE KEY PEM block.</param>
    /// <returns>An owned RSA key, or stable failure details.</returns>
    /// <exception cref="CryptographicException">Thrown when the decoded key data is not a valid RSA private key.</exception>
    public TokenKeyResult ImportRsaPrivateKeyPem(string pem)
    {
        PemReadResult readResult = PemCodec.Read(pem);
        if (!readResult.Success)
        {
            return CreatePemKeyFailure(readResult.Error);
        }

        string label = GetPemLabel(readResult);
        if (!string.Equals(label, "PRIVATE KEY", StringComparison.Ordinal)
            && !string.Equals(label, "RSA PRIVATE KEY", StringComparison.Ordinal))
        {
            return CreatePemKeyTypeFailure("An RSA private key PEM block is required.");
        }

        return ImportRsaPemCore(pem, true);
    }

    /// <summary>Imports an ECDSA public key from a strict PEM block.</summary>
    /// <param name="pem">A PUBLIC KEY PEM block.</param>
    /// <returns>An owned ECDSA key, or stable failure details.</returns>
    /// <exception cref="CryptographicException">Thrown when the decoded key data is not a valid ECDSA key.</exception>
    public TokenKeyResult ImportEcdsaPublicKeyPem(string pem)
    {
        PemReadResult readResult = PemCodec.Read(pem);
        if (!readResult.Success)
        {
            return CreatePemKeyFailure(readResult.Error);
        }

        if (!string.Equals(GetPemLabel(readResult), "PUBLIC KEY", StringComparison.Ordinal))
        {
            return CreatePemKeyTypeFailure("An ECDSA public key PEM block is required.");
        }

        return ImportEcdsaPemCore(pem, false);
    }

    /// <summary>Imports an ECDSA private key from a strict PEM block.</summary>
    /// <param name="pem">A PRIVATE KEY or EC PRIVATE KEY PEM block.</param>
    /// <returns>An owned ECDSA key, or stable failure details.</returns>
    /// <exception cref="CryptographicException">Thrown when the decoded key data is not a valid ECDSA private key.</exception>
    public TokenKeyResult ImportEcdsaPrivateKeyPem(string pem)
    {
        PemReadResult readResult = PemCodec.Read(pem);
        if (!readResult.Success)
        {
            return CreatePemKeyFailure(readResult.Error);
        }

        string label = GetPemLabel(readResult);
        if (!string.Equals(label, "PRIVATE KEY", StringComparison.Ordinal)
            && !string.Equals(label, "EC PRIVATE KEY", StringComparison.Ordinal))
        {
            return CreatePemKeyTypeFailure("An ECDSA private key PEM block is required.");
        }

        return ImportEcdsaPemCore(pem, true);
    }

    /// <summary>Exports an RSA public key as a PUBLIC KEY PEM block.</summary>
    /// <param name="key">The RSA key to export.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    public TokenTextResult ExportRsaPublicKeyPem(TokenKey key)
    {
        TokenResult validation = RsaKeyImporterOperations.Validate(key, false, false);
        if (!validation.Success)
        {
            return CreatePemTextFailure(validation.Error);
        }

        RSA rsa = key.Rsa ?? throw new InvalidOperationException("A required value is null.");
        return CreatePemTextSuccess(rsa.ExportSubjectPublicKeyInfoPem());
    }

    /// <summary>Exports an RSA private key as a PRIVATE KEY PEM block.</summary>
    /// <param name="key">The RSA private key to export.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    /// <exception cref="CryptographicException">Thrown when the native key cannot export private material.</exception>
    public TokenTextResult ExportRsaPrivateKeyPem(TokenKey key)
    {
        TokenResult validation = RsaKeyImporterOperations.Validate(key, true, false);
        if (!validation.Success)
        {
            return CreatePemTextFailure(validation.Error);
        }

        RSA rsa = key.Rsa ?? throw new InvalidOperationException("A required value is null.");
        return CreatePemTextSuccess(rsa.ExportPkcs8PrivateKeyPem());
    }

    /// <summary>Exports an ECDSA public key as a PUBLIC KEY PEM block.</summary>
    /// <param name="key">The ECDSA key to export.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    public TokenTextResult ExportEcdsaPublicKeyPem(TokenKey key)
    {
        TokenResult validation = EcdsaKeyImporter.Validate(key, false, false);
        if (!validation.Success)
        {
            return CreatePemTextFailure(validation.Error);
        }

        ECDsa ecdsa = key.Ecdsa ?? throw new InvalidOperationException("A required value is null.");
        return CreatePemTextSuccess(ecdsa.ExportSubjectPublicKeyInfoPem());
    }

    /// <summary>Exports an ECDSA private key as a PRIVATE KEY PEM block.</summary>
    /// <param name="key">The ECDSA private key to export.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    /// <exception cref="CryptographicException">Thrown when the native key cannot export private material.</exception>
    public TokenTextResult ExportEcdsaPrivateKeyPem(TokenKey key)
    {
        TokenResult validation = EcdsaKeyImporter.Validate(key, true, false);
        if (!validation.Success)
        {
            return CreatePemTextFailure(validation.Error);
        }

        ECDsa ecdsa = key.Ecdsa ?? throw new InvalidOperationException("A required value is null.");
        return CreatePemTextSuccess(ecdsa.ExportPkcs8PrivateKeyPem());
    }

    /// <summary>Releases native material owned by a key returned from a PEM import.</summary>
    /// <param name="key">The imported key to release.</param>
    public void ReleaseImportedKey(TokenKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        key.ReleaseOwnedMaterial();
    }

    internal static TokenKeyResult ImportRsaPemCore(string pem, bool privateMaterial)
    {
        RSA rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        TokenKeyResult result = RsaKeyImporterOperations.Import(rsa, privateMaterial);
        if (!result.Success)
        {
            rsa.Dispose();
            return result;
        }

        TokenKey key = result.Key ?? throw new InvalidOperationException("A required value is null.");
        key.OwnsNativeKey = true;
        return result;
    }

    internal static TokenKeyResult ImportEcdsaPemCore(string pem, bool privateMaterial)
    {
        ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(pem);
        TokenKeyResult result = EcdsaKeyImporter.Import(ecdsa, privateMaterial);
        if (!result.Success)
        {
            ecdsa.Dispose();
            return result;
        }

        TokenKey key = result.Key ?? throw new InvalidOperationException("A required value is null.");
        key.OwnsNativeKey = true;
        return result;
    }

    internal static string GetPemLabel(PemReadResult readResult)
    {
        PemBlock block = readResult.Block ?? throw new InvalidOperationException("A required value is null.");
        return block.Label ?? throw new InvalidOperationException("A required value is null.");
    }

    internal static TokenKeyResult CreatePemKeyTypeFailure(string message)
    {
        return CreatePemKeyFailure(new TokenError("invalid_pem_key_type", message, 0));
    }

    internal static TokenKeyResult CreatePemKeyFailure(TokenError? error)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = error;
        return result;
    }

    internal static TokenTextResult CreatePemTextFailure(TokenError? error)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = error;
        return result;
    }

    internal static TokenTextResult CreatePemTextSuccess(string text)
    {
        TokenTextResult result = new TokenTextResult();
        result.Text = text;
        result.Success = true;
        return result;
    }
}
