using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Describes conjunctive criteria for selecting a <see cref="TokenKey"/>.
/// </summary>
/// <remarks>
/// Null text and byte-array fields, zero integer fields, and false requirement fields are
/// inactive filters. Text identifiers are compared using ordinal case-sensitive comparison.
/// </remarks>
public class TokenKeyQuery
{
    /// <summary>The required JOSE text key identifier.</summary>
    public string? KeyId;

    /// <summary>The required COSE byte-string key identifier.</summary>
    public byte[]? KeyIdBytes;

    /// <summary>The required normalized key type.</summary>
    public string? KeyType;

    /// <summary>The required COSE key type label, or zero when not filtered.</summary>
    public int CoseKeyType;

    /// <summary>The required JOSE algorithm name.</summary>
    public string? Algorithm;

    /// <summary>The required COSE algorithm label.</summary>
    public int CoseAlgorithm;

    /// <summary>Indicates whether <see cref="CoseAlgorithm"/> is an active filter.</summary>
    public bool HasCoseAlgorithm;

    /// <summary>The required JOSE key use.</summary>
    public string? Use;

    /// <summary>The required normalized text key operation.</summary>
    public string? KeyOperation;

    /// <summary>The required COSE key operation, or zero when not filtered.</summary>
    public int CoseKeyOperation;

    /// <summary>The required JOSE curve name.</summary>
    public string? Curve;

    /// <summary>The required COSE curve label, or zero when not filtered.</summary>
    public int CoseCurve;

    /// <summary>The required SHA-1 certificate thumbprint bytes.</summary>
    public byte[]? CertificateSha1Thumbprint;

    /// <summary>The required SHA-256 certificate thumbprint bytes.</summary>
    public byte[]? CertificateSha256Thumbprint;

    /// <summary>Indicates that matching keys must contain private or symmetric secret material.</summary>
    public bool RequirePrivateMaterial;

    /// <summary>Indicates that matching keys must contain public material.</summary>
    public bool RequirePublicMaterial;

    /// <summary>Initializes a query with no active filters.</summary>
    public TokenKeyQuery()
    {
        KeyId = null;
        KeyIdBytes = null;
        KeyType = null;
        CoseKeyType = 0;
        Algorithm = null;
        CoseAlgorithm = 0;
        HasCoseAlgorithm = false;
        Use = null;
        KeyOperation = null;
        CoseKeyOperation = 0;
        Curve = null;
        CoseCurve = 0;
        CertificateSha1Thumbprint = null;
        CertificateSha256Thumbprint = null;
        RequirePrivateMaterial = false;
        RequirePublicMaterial = false;
    }

    /// <summary>Determines whether a key satisfies every active query criterion.</summary>
    /// <param name="key">The key to inspect.</param>
    /// <returns><c>true</c> when the key satisfies the query; otherwise, <c>false</c>.</returns>
    internal bool Matches(TokenKey key)
    {
        if (key == null)
        {
            return false;
        }

        if (KeyId != null && !string.Equals(KeyId, key.KeyId, StringComparison.Ordinal))
        {
            return false;
        }

        if (KeyIdBytes != null
            && (key.KeyIdBytes == null
                || !ConstantTime.AreEqual(KeyIdBytes, key.KeyIdBytes)))
        {
            return false;
        }

        if (!MatchesKeyType(key))
        {
            return false;
        }

        if (!MatchesAlgorithm(key))
        {
            return false;
        }

        if (Use != null && !string.Equals(Use, key.Use, StringComparison.Ordinal))
        {
            return false;
        }

        if (!MatchesOperation(key))
        {
            return false;
        }

        if (!MatchesCurve(key))
        {
            return false;
        }

        if (!MatchesThumbprint(
            CertificateSha1Thumbprint,
            key.CertificateSha1Thumbprint,
            key,
            HashAlgorithmName.SHA1))
        {
            return false;
        }

        if (!MatchesThumbprint(
            CertificateSha256Thumbprint,
            key.CertificateSha256Thumbprint,
            key,
            HashAlgorithmName.SHA256))
        {
            return false;
        }

        if (RequirePrivateMaterial && !key.ContainsPrivateMaterial())
        {
            return false;
        }

        if (RequirePublicMaterial && !key.ContainsPublicMaterial())
        {
            return false;
        }

        return true;
    }

    bool MatchesKeyType(TokenKey key)
    {
        if (KeyType != null)
        {
            bool matched;
            if (key.KeyType != null)
            {
                matched = string.Equals(KeyType, key.KeyType, StringComparison.Ordinal);
            }
            else
            {
                int queryCoseKeyType = GetCoseKeyType(KeyType);
                matched = queryCoseKeyType != 0 && queryCoseKeyType == key.CoseKeyType;
            }

            if (!matched)
            {
                return false;
            }
        }

        if (CoseKeyType != 0)
        {
            bool matched;
            if (key.CoseKeyType != 0)
            {
                matched = CoseKeyType == key.CoseKeyType;
            }
            else
            {
                string? queryKeyType = GetKeyType(CoseKeyType);
                matched = queryKeyType != null
                    && string.Equals(queryKeyType, key.KeyType, StringComparison.Ordinal);
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    bool MatchesAlgorithm(TokenKey key)
    {
        if (Algorithm != null)
        {
            bool matched;
            if (key.Algorithm != null)
            {
                matched = string.Equals(Algorithm, key.Algorithm, StringComparison.Ordinal);
            }
            else if (key.HasCoseAlgorithm)
            {
                int[] mappedLabels = JoseCoseAlgorithmMap.GetCoseLabels(Algorithm);
                matched = RegistryLookup.Contains(mappedLabels, key.CoseAlgorithm);
            }
            else
            {
                matched = false;
            }

            if (!matched)
            {
                return false;
            }
        }

        if (HasCoseAlgorithm)
        {
            bool matched;
            if (key.HasCoseAlgorithm)
            {
                matched = CoseAlgorithm == key.CoseAlgorithm;
            }
            else if (key.Algorithm != null)
            {
                AlgorithmMappingResult mapping = JoseCoseAlgorithmMap.FromCose(CoseAlgorithm);
                matched = mapping.Mapped
                    && string.Equals(mapping.JoseName, key.Algorithm, StringComparison.Ordinal);
            }
            else
            {
                matched = false;
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    bool MatchesOperation(TokenKey key)
    {
        if (KeyOperation != null)
        {
            bool hasTextOperations = key.KeyOperations != null && key.KeyOperations.Length > 0;
            bool matched;
            if (hasTextOperations)
            {
                matched = RegistryLookup.Contains(key.KeyOperations, KeyOperation, false);
            }
            else
            {
                int coseOperation = GetCoseOperation(KeyOperation);
                matched = coseOperation != 0
                    && key.CoseKeyOperations != null
                    && RegistryLookup.Contains(key.CoseKeyOperations, coseOperation);
            }

            if (!matched)
            {
                return false;
            }
        }

        if (CoseKeyOperation != 0)
        {
            bool hasCoseOperations = key.CoseKeyOperations != null
                && key.CoseKeyOperations.Length > 0;
            bool matched;
            if (hasCoseOperations)
            {
                matched = RegistryLookup.Contains(key.CoseKeyOperations, CoseKeyOperation);
            }
            else
            {
                string? operation = GetKeyOperation(CoseKeyOperation);
                matched = operation != null
                    && key.KeyOperations != null
                    && RegistryLookup.Contains(key.KeyOperations, operation, false);
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    bool MatchesCurve(TokenKey key)
    {
        if (Curve != null)
        {
            bool matched;
            if (key.Curve != null)
            {
                matched = string.Equals(Curve, key.Curve, StringComparison.Ordinal);
            }
            else
            {
                int curve = GetCoseCurve(Curve);
                matched = curve != 0 && curve == key.CoseCurve;
            }

            if (!matched)
            {
                return false;
            }
        }

        if (CoseCurve != 0)
        {
            bool matched;
            if (key.CoseCurve != 0)
            {
                matched = CoseCurve == key.CoseCurve;
            }
            else
            {
                string? curve = CoseKeyLabelsOperations.GetCurveName(CoseCurve);
                matched = curve != null
                    && string.Equals(curve, key.Curve, StringComparison.Ordinal);
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool MatchesThumbprint(
        byte[]? required,
        byte[]? stored,
        TokenKey key,
        HashAlgorithmName hashAlgorithm)
    {
        if (required == null)
        {
            return true;
        }

        if (key.Certificate != null)
        {
            byte[] calculated = key.Certificate.GetCertHash(hashAlgorithm);
            return ConstantTime.AreEqual(required, calculated);
        }

        return stored != null && ConstantTime.AreEqual(required, stored);
    }

    internal static int GetCoseKeyType(string keyType)
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

    internal static string? GetKeyType(int coseKeyType)
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

    internal static int GetCoseCurve(string curve)
    {
        int[] curves = CoseKeyLabelsOperations.GetCurves();
        int index = 0;
        while (index < curves.Length)
        {
            string? name = CoseKeyLabelsOperations.GetCurveName(curves[index]);
            if (string.Equals(curve, name, StringComparison.Ordinal))
            {
                return curves[index];
            }

            index += 1;
        }

        return 0;
    }

    internal static int GetCoseOperation(string operation)
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

        if (string.Equals(operation, TokenKeyOperations.MacCreate, StringComparison.Ordinal))
        {
            return CoseKeyLabels.OperationMacCreate;
        }

        if (string.Equals(operation, TokenKeyOperations.MacVerify, StringComparison.Ordinal))
        {
            return CoseKeyLabels.OperationMacVerify;
        }

        return 0;
    }

    internal static string? GetKeyOperation(int coseOperation)
    {
        if (coseOperation == CoseKeyLabels.OperationSign)
        {
            return TokenKeyOperations.Sign;
        }

        if (coseOperation == CoseKeyLabels.OperationVerify)
        {
            return TokenKeyOperations.Verify;
        }

        if (coseOperation == CoseKeyLabels.OperationEncrypt)
        {
            return TokenKeyOperations.Encrypt;
        }

        if (coseOperation == CoseKeyLabels.OperationDecrypt)
        {
            return TokenKeyOperations.Decrypt;
        }

        if (coseOperation == CoseKeyLabels.OperationWrapKey)
        {
            return TokenKeyOperations.WrapKey;
        }

        if (coseOperation == CoseKeyLabels.OperationUnwrapKey)
        {
            return TokenKeyOperations.UnwrapKey;
        }

        if (coseOperation == CoseKeyLabels.OperationDeriveKey)
        {
            return TokenKeyOperations.DeriveKey;
        }

        if (coseOperation == CoseKeyLabels.OperationDeriveBits)
        {
            return TokenKeyOperations.DeriveBits;
        }

        if (coseOperation == CoseKeyLabels.OperationMacCreate)
        {
            return TokenKeyOperations.MacCreate;
        }

        if (coseOperation == CoseKeyLabels.OperationMacVerify)
        {
            return TokenKeyOperations.MacVerify;
        }

        return null;
    }
}
