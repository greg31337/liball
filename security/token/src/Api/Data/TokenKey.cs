using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace token;

/// <summary>
/// Contains normalized JOSE, COSE, native, and raw representations of one cryptographic key.
/// </summary>
/// <remarks>
/// Import and format-specific validation are performed by dedicated key importers. Native-key
/// importers retain caller ownership. Format parsers can mark native objects as owned so
/// <see cref="ReleaseOwnedMaterial"/> can dispose them.
/// </remarks>
public class TokenKey
{
    /// <summary>The text key identifier used by JOSE formats.</summary>
    public string? KeyId;

    /// <summary>The byte-string key identifier used by COSE formats.</summary>
    public byte[]? KeyIdBytes;

    /// <summary>The normalized key type, such as <c>oct</c>, <c>RSA</c>, <c>EC</c>, or <c>OKP</c>.</summary>
    public string? KeyType;

    /// <summary>The COSE key type label, or zero when none is assigned.</summary>
    public int CoseKeyType;

    /// <summary>The JOSE algorithm name, or <c>null</c> when unspecified.</summary>
    public string? Algorithm;

    /// <summary>The COSE algorithm label.</summary>
    public int CoseAlgorithm;

    /// <summary>Indicates whether <see cref="CoseAlgorithm"/> is present.</summary>
    public bool HasCoseAlgorithm;

    /// <summary>The JOSE key use, or <c>null</c> when unrestricted.</summary>
    public string? Use;

    /// <summary>The normalized text key operations.</summary>
    public string[] KeyOperations;

    /// <summary>The COSE integer key operations.</summary>
    public int[]? CoseKeyOperations;

    /// <summary>The base initialization vector used with COSE Partial IV headers.</summary>
    public byte[]? BaseInitializationVector;

    /// <summary>The JOSE curve name, or <c>null</c> when not applicable.</summary>
    public string? Curve;

    /// <summary>The COSE curve label, or zero when not applicable.</summary>
    public int CoseCurve;

    /// <summary>The symmetric secret key bytes.</summary>
    public byte[]? SymmetricKey;

    /// <summary>A native RSA key.</summary>
    public RSA? Rsa;

    /// <summary>A native elliptic-curve signing key.</summary>
    public ECDsa? Ecdsa;

    /// <summary>The raw public bytes for an OKP-like key.</summary>
    public byte[]? OkpPublicKey;

    /// <summary>The raw private bytes for an OKP-like key.</summary>
    public byte[]? OkpPrivateKey;

    /// <summary>The raw public bytes for an algorithm-specific key pair.</summary>
    public byte[]? AlgorithmPublicKey;

    /// <summary>The raw private bytes for an algorithm-specific key pair.</summary>
    public byte[]? AlgorithmPrivateKey;

    /// <summary>An associated end-entity certificate.</summary>
    public X509Certificate2? Certificate;

    /// <summary>An associated certificate chain, ordered from leaf toward the trust anchor.</summary>
    public X509Certificate2[] CertificateChain;

    /// <summary>The SHA-1 certificate thumbprint bytes, or <c>null</c> when unavailable.</summary>
    public byte[]? CertificateSha1Thumbprint;

    /// <summary>The SHA-256 certificate thumbprint bytes, or <c>null</c> when unavailable.</summary>
    public byte[]? CertificateSha256Thumbprint;

    /// <summary>Indicates that a native or certificate-backed RSA or EC key contains private material.</summary>
    public bool NativePrivateMaterial;

    /// <summary>Indicates that a native or certificate-backed RSA or EC key contains public material.</summary>
    public bool NativePublicMaterial;

    /// <summary>Indicates that this container owns and may dispose its native RSA or EC object.</summary>
    public bool OwnsNativeKey;

    /// <summary>Initializes an empty key container.</summary>
    public TokenKey()
    {
        KeyId = null;
        KeyIdBytes = null;
        KeyType = null;
        CoseKeyType = 0;
        Algorithm = null;
        CoseAlgorithm = 0;
        HasCoseAlgorithm = false;
        Use = null;
        KeyOperations = System.Array.Empty<string>();
        CoseKeyOperations = System.Array.Empty<int>();
        BaseInitializationVector = null;
        Curve = null;
        CoseCurve = 0;
        SymmetricKey = null;
        Rsa = null;
        Ecdsa = null;
        OkpPublicKey = null;
        OkpPrivateKey = null;
        AlgorithmPublicKey = null;
        AlgorithmPrivateKey = null;
        Certificate = null;
        CertificateChain = System.Array.Empty<X509Certificate2>();
        CertificateSha1Thumbprint = null;
        CertificateSha256Thumbprint = null;
        NativePrivateMaterial = false;
        NativePublicMaterial = false;
        OwnsNativeKey = false;
    }

    /// <summary>Determines whether the container has private or symmetric secret material.</summary>
    /// <returns><c>true</c> when private material is available; otherwise, <c>false</c>.</returns>
    internal bool ContainsPrivateMaterial()
    {
        if (SymmetricKey != null && SymmetricKey.Length > 0)
        {
            return true;
        }

        if (OkpPrivateKey != null && OkpPrivateKey.Length > 0)
        {
            return true;
        }

        if (AlgorithmPrivateKey != null && AlgorithmPrivateKey.Length > 0)
        {
            return true;
        }

        return NativePrivateMaterial;
    }

    /// <summary>Determines whether the container has public key material.</summary>
    /// <returns><c>true</c> when public material is available; otherwise, <c>false</c>.</returns>
    internal bool ContainsPublicMaterial()
    {
        if (OkpPublicKey != null && OkpPublicKey.Length > 0)
        {
            return true;
        }

        if (AlgorithmPublicKey != null && AlgorithmPublicKey.Length > 0)
        {
            return true;
        }

        if (Certificate != null)
        {
            return true;
        }

        if (Rsa != null || Ecdsa != null)
        {
            return true;
        }

        return NativePublicMaterial;
    }

    /// <summary>Disposes owned native keys and zeroes secret byte arrays held by this container.</summary>
    /// <remarks>Caller-owned RSA and ECDSA objects are detached but are not disposed.</remarks>
    internal void ReleaseOwnedMaterial()
    {
        if (SymmetricKey != null)
        {
            CryptographicOperations.ZeroMemory(SymmetricKey);
            SymmetricKey = null;
        }

        if (OkpPrivateKey != null)
        {
            CryptographicOperations.ZeroMemory(OkpPrivateKey);
            OkpPrivateKey = null;
        }

        if (AlgorithmPrivateKey != null)
        {
            CryptographicOperations.ZeroMemory(AlgorithmPrivateKey);
            AlgorithmPrivateKey = null;
        }

        if (OwnsNativeKey)
        {
            if (Rsa != null)
            {
                Rsa.Dispose();
            }

            if (Ecdsa != null)
            {
                Ecdsa.Dispose();
            }
        }

        Rsa = null;
        Ecdsa = null;
        NativePrivateMaterial = false;
        NativePublicMaterial = Certificate != null;
        OwnsNativeKey = false;
    }
}
