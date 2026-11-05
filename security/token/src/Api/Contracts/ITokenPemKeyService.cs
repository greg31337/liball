using System;
namespace token;

/// <summary>
/// Provides PEM import, export, and owned-key release operations for RSA and ECDSA keys.
/// </summary>
public interface ITokenPemKeyService
{
    /// <summary>Imports an RSA public key from a strict PEM block.</summary>
    /// <param name="pem">A PUBLIC KEY or RSA PUBLIC KEY PEM block.</param>
    /// <returns>An owned RSA key, or stable failure details.</returns>
    TokenKeyResult ImportRsaPublicKeyPem(string pem);

    /// <summary>Imports an RSA private key from a strict PEM block.</summary>
    /// <param name="pem">A PRIVATE KEY or RSA PRIVATE KEY PEM block.</param>
    /// <returns>An owned RSA key, or stable failure details.</returns>
    TokenKeyResult ImportRsaPrivateKeyPem(string pem);

    /// <summary>Imports an ECDSA public key from a strict PEM block.</summary>
    /// <param name="pem">A PUBLIC KEY PEM block.</param>
    /// <returns>An owned ECDSA key, or stable failure details.</returns>
    TokenKeyResult ImportEcdsaPublicKeyPem(string pem);

    /// <summary>Imports an ECDSA private key from a strict PEM block.</summary>
    /// <param name="pem">A PRIVATE KEY or EC PRIVATE KEY PEM block.</param>
    /// <returns>An owned ECDSA key, or stable failure details.</returns>
    TokenKeyResult ImportEcdsaPrivateKeyPem(string pem);

    /// <summary>Exports an RSA public key as a PUBLIC KEY PEM block.</summary>
    /// <param name="key">The RSA key to export.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    TokenTextResult ExportRsaPublicKeyPem(TokenKey key);

    /// <summary>Exports an RSA private key as a PRIVATE KEY PEM block.</summary>
    /// <param name="key">The RSA private key to export.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    TokenTextResult ExportRsaPrivateKeyPem(TokenKey key);

    /// <summary>Exports an ECDSA public key as a PUBLIC KEY PEM block.</summary>
    /// <param name="key">The ECDSA key to export.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    TokenTextResult ExportEcdsaPublicKeyPem(TokenKey key);

    /// <summary>Exports an ECDSA private key as a PRIVATE KEY PEM block.</summary>
    /// <param name="key">The ECDSA private key to export.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    TokenTextResult ExportEcdsaPrivateKeyPem(TokenKey key);

    /// <summary>Releases native material owned by a key returned from a PEM import.</summary>
    /// <param name="key">The imported key to release.</param>
    void ReleaseImportedKey(TokenKey key);
}
