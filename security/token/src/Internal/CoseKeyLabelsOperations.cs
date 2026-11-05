using System;
namespace token;

/// <summary>
/// Provides COSE key types, common and type-specific parameters, operations, and curves.
/// </summary>
internal static class CoseKeyLabelsOperations
{
    /// <summary>The OKP key type.</summary>
    internal const int KeyTypeOkp = CoseKeyLabels.KeyTypeOkp;
    /// <summary>The elliptic-curve key type with affine coordinates.</summary>
    internal const int KeyTypeEc2 = CoseKeyLabels.KeyTypeEc2;
    /// <summary>The RSA key type.</summary>
    internal const int KeyTypeRsa = CoseKeyLabels.KeyTypeRsa;
    /// <summary>The symmetric key type.</summary>
    internal const int KeyTypeSymmetric = CoseKeyLabels.KeyTypeSymmetric;
    /// <summary>The HSS/LMS key type.</summary>
    internal const int KeyTypeHssLms = CoseKeyLabels.KeyTypeHssLms;
    /// <summary>The WalnutDSA key type.</summary>
    internal const int KeyTypeWalnutDsa = CoseKeyLabels.KeyTypeWalnutDsa;
    /// <summary>The algorithm key pair key type.</summary>
    internal const int KeyTypeAlgorithmKeyPair = CoseKeyLabels.KeyTypeAlgorithmKeyPair;

    /// <summary>The key type common parameter.</summary>
    internal const int KeyType = CoseKeyLabels.KeyType;
    /// <summary>The key identifier common parameter.</summary>
    internal const int KeyId = CoseKeyLabels.KeyId;
    /// <summary>The algorithm common parameter.</summary>
    internal const int Algorithm = CoseKeyLabels.Algorithm;
    /// <summary>The key operations common parameter.</summary>
    internal const int KeyOperations = CoseKeyLabels.KeyOperations;
    /// <summary>The base initialization vector common parameter.</summary>
    internal const int BaseInitializationVector = CoseKeyLabels.BaseInitializationVector;

    /// <summary>The curve parameter for OKP and EC2 keys.</summary>
    internal const int Curve = CoseKeyLabels.Curve;
    /// <summary>The public x-coordinate or public-key parameter for OKP and EC2 keys.</summary>
    internal const int X = CoseKeyLabels.X;
    /// <summary>The y-coordinate parameter for EC2 keys.</summary>
    internal const int Y = CoseKeyLabels.Y;
    /// <summary>The private key parameter for OKP and EC2 keys.</summary>
    internal const int PrivateKey = CoseKeyLabels.PrivateKey;
    /// <summary>The RSA modulus parameter.</summary>
    internal const int RsaModulus = CoseKeyLabels.RsaModulus;
    /// <summary>The RSA public exponent parameter.</summary>
    internal const int RsaPublicExponent = CoseKeyLabels.RsaPublicExponent;
    /// <summary>The RSA private exponent parameter.</summary>
    internal const int RsaPrivateExponent = CoseKeyLabels.RsaPrivateExponent;
    /// <summary>The first RSA prime factor parameter.</summary>
    internal const int RsaFirstPrimeFactor = CoseKeyLabels.RsaFirstPrimeFactor;
    /// <summary>The second RSA prime factor parameter.</summary>
    internal const int RsaSecondPrimeFactor = CoseKeyLabels.RsaSecondPrimeFactor;
    /// <summary>The first RSA CRT exponent parameter.</summary>
    internal const int RsaFirstFactorCrtExponent = CoseKeyLabels.RsaFirstFactorCrtExponent;
    /// <summary>The second RSA CRT exponent parameter.</summary>
    internal const int RsaSecondFactorCrtExponent = CoseKeyLabels.RsaSecondFactorCrtExponent;
    /// <summary>The RSA CRT coefficient parameter.</summary>
    internal const int RsaCrtCoefficient = CoseKeyLabels.RsaCrtCoefficient;
    /// <summary>The RSA other-primes information parameter.</summary>
    internal const int RsaOtherPrimes = CoseKeyLabels.RsaOtherPrimes;
    /// <summary>An RSA additional prime factor parameter.</summary>
    internal const int RsaAdditionalPrimeFactor = CoseKeyLabels.RsaAdditionalPrimeFactor;
    /// <summary>An RSA additional factor CRT exponent parameter.</summary>
    internal const int RsaAdditionalFactorCrtExponent = CoseKeyLabels.RsaAdditionalFactorCrtExponent;
    /// <summary>An RSA additional factor CRT coefficient parameter.</summary>
    internal const int RsaAdditionalFactorCrtCoefficient = CoseKeyLabels.RsaAdditionalFactorCrtCoefficient;
    /// <summary>The symmetric key material parameter.</summary>
    internal const int SymmetricKey = CoseKeyLabels.SymmetricKey;
    /// <summary>The HSS/LMS public key parameter.</summary>
    internal const int HssLmsPublicKey = CoseKeyLabels.HssLmsPublicKey;
    /// <summary>The algorithm key pair public key parameter.</summary>
    internal const int AlgorithmKeyPairPublicKey = CoseKeyLabels.AlgorithmKeyPairPublicKey;
    /// <summary>The algorithm key pair private key parameter.</summary>
    internal const int AlgorithmKeyPairPrivateKey = CoseKeyLabels.AlgorithmKeyPairPrivateKey;

    /// <summary>The sign key operation.</summary>
    internal const int OperationSign = CoseKeyLabels.OperationSign;
    /// <summary>The verify key operation.</summary>
    internal const int OperationVerify = CoseKeyLabels.OperationVerify;
    /// <summary>The encrypt key operation.</summary>
    internal const int OperationEncrypt = CoseKeyLabels.OperationEncrypt;
    /// <summary>The decrypt key operation.</summary>
    internal const int OperationDecrypt = CoseKeyLabels.OperationDecrypt;
    /// <summary>The wrap-key operation.</summary>
    internal const int OperationWrapKey = CoseKeyLabels.OperationWrapKey;
    /// <summary>The unwrap-key operation.</summary>
    internal const int OperationUnwrapKey = CoseKeyLabels.OperationUnwrapKey;
    /// <summary>The derive-key operation.</summary>
    internal const int OperationDeriveKey = CoseKeyLabels.OperationDeriveKey;
    /// <summary>The derive-bits operation.</summary>
    internal const int OperationDeriveBits = CoseKeyLabels.OperationDeriveBits;
    /// <summary>The MAC-create operation.</summary>
    internal const int OperationMacCreate = CoseKeyLabels.OperationMacCreate;
    /// <summary>The MAC-verify operation.</summary>
    internal const int OperationMacVerify = CoseKeyLabels.OperationMacVerify;

    /// <summary>The NIST P-256 curve.</summary>
    internal const int CurveP256 = CoseKeyLabels.CurveP256;
    /// <summary>The NIST P-384 curve.</summary>
    internal const int CurveP384 = CoseKeyLabels.CurveP384;
    /// <summary>The NIST P-521 curve.</summary>
    internal const int CurveP521 = CoseKeyLabels.CurveP521;
    /// <summary>The X25519 curve.</summary>
    internal const int CurveX25519 = CoseKeyLabels.CurveX25519;
    /// <summary>The X448 curve.</summary>
    internal const int CurveX448 = CoseKeyLabels.CurveX448;
    /// <summary>The Ed25519 curve.</summary>
    internal const int CurveEd25519 = CoseKeyLabels.CurveEd25519;
    /// <summary>The Ed448 curve.</summary>
    internal const int CurveEd448 = CoseKeyLabels.CurveEd448;
    /// <summary>The secp256k1 curve.</summary>
    internal const int CurveSecp256k1 = CoseKeyLabels.CurveSecp256k1;
    /// <summary>The brainpoolP256r1 curve.</summary>
    internal const int CurveBrainpoolP256r1 = CoseKeyLabels.CurveBrainpoolP256r1;
    /// <summary>The brainpoolP320r1 curve.</summary>
    internal const int CurveBrainpoolP320r1 = CoseKeyLabels.CurveBrainpoolP320r1;
    /// <summary>The brainpoolP384r1 curve.</summary>
    internal const int CurveBrainpoolP384r1 = CoseKeyLabels.CurveBrainpoolP384r1;
    /// <summary>The brainpoolP512r1 curve.</summary>
    internal const int CurveBrainpoolP512r1 = CoseKeyLabels.CurveBrainpoolP512r1;

    static readonly int[] keyTypes = new int[]
    {
        KeyTypeOkp, KeyTypeEc2, KeyTypeRsa, KeyTypeSymmetric, KeyTypeHssLms,
        KeyTypeWalnutDsa, KeyTypeAlgorithmKeyPair
    };

    static readonly string[] keyTypeNames = new string[]
    {
        "OKP", "EC2", "RSA", "Symmetric", "HSS-LMS", "WalnutDSA", "AKP"
    };

    static readonly int[] commonParameters = new int[]
    {
        KeyType, KeyId, Algorithm, KeyOperations, BaseInitializationVector
    };

    static readonly string[] commonParameterNames = new string[]
    {
        "kty", "kid", "alg", "key_ops", "Base IV"
    };

    static readonly int[] okpParameters = new int[] { Curve, X, PrivateKey };
    static readonly string[] okpParameterNames = new string[] { "crv", "x", "d" };
    static readonly int[] ec2Parameters = new int[] { Curve, X, Y, PrivateKey };
    static readonly string[] ec2ParameterNames = new string[] { "crv", "x", "y", "d" };
    static readonly int[] rsaParameters = new int[]
    {
        RsaModulus, RsaPublicExponent, RsaPrivateExponent, RsaFirstPrimeFactor,
        RsaSecondPrimeFactor, RsaFirstFactorCrtExponent,
        RsaSecondFactorCrtExponent, RsaCrtCoefficient, RsaOtherPrimes,
        RsaAdditionalPrimeFactor, RsaAdditionalFactorCrtExponent,
        RsaAdditionalFactorCrtCoefficient
    };
    static readonly string[] rsaParameterNames = new string[]
    {
        "n", "e", "d", "p", "q", "dP", "dQ", "qInv", "other", "r_i",
        "d_i", "t_i"
    };
    static readonly int[] symmetricParameters = new int[] { SymmetricKey };
    static readonly string[] symmetricParameterNames = new string[] { "k" };
    static readonly int[] hssLmsParameters = new int[] { HssLmsPublicKey };
    static readonly string[] hssLmsParameterNames = new string[] { "pub" };
    static readonly int[] walnutDsaParameters = new int[] { -1, -2, -3, -4, -5, -6 };
    static readonly string[] walnutDsaParameterNames = new string[]
    {
        "N", "q", "t-values", "matrix1", "permutation1", "matrix2"
    };
    static readonly int[] algorithmKeyPairParameters = new int[]
    {
        AlgorithmKeyPairPublicKey, AlgorithmKeyPairPrivateKey
    };
    static readonly string[] algorithmKeyPairParameterNames = new string[] { "pub", "priv" };

    static readonly int[] operations = new int[]
    {
        OperationSign, OperationVerify, OperationEncrypt, OperationDecrypt,
        OperationWrapKey, OperationUnwrapKey, OperationDeriveKey,
        OperationDeriveBits, OperationMacCreate, OperationMacVerify
    };

    static readonly string[] operationNames = new string[]
    {
        "sign", "verify", "encrypt", "decrypt", "wrap key", "unwrap key",
        "derive key", "derive bits", "MAC create", "MAC verify"
    };

    static readonly int[] curves = new int[]
    {
        CurveP256, CurveP384, CurveP521, CurveX25519, CurveX448, CurveEd25519,
        CurveEd448, CurveSecp256k1, CurveBrainpoolP256r1,
        CurveBrainpoolP320r1, CurveBrainpoolP384r1, CurveBrainpoolP512r1
    };

    static readonly string[] curveNames = new string[]
    {
        "P-256", "P-384", "P-521", "X25519", "X448", "Ed25519", "Ed448",
        "secp256k1", "brainpoolP256r1", "brainpoolP320r1", "brainpoolP384r1",
        "brainpoolP512r1"
    };

    /// <summary>Determines whether a COSE key type is registered.</summary>
    /// <param name="keyType">The integer key type.</param>
    /// <returns><c>true</c> when the key type is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegisteredKeyType(int keyType)
    {
        return RegistryLookup.Contains(keyTypes, keyType);
    }

    /// <summary>Gets the registered name of a COSE key type.</summary>
    /// <param name="keyType">The integer key type.</param>
    /// <returns>The key type name, or <c>null</c> when it is unknown.</returns>
    internal static string? GetKeyTypeName(int keyType)
    {
        return FindName(keyTypes, keyTypeNames, keyType);
    }

    /// <summary>Determines whether a label is a common COSE key parameter.</summary>
    /// <param name="label">The integer parameter label.</param>
    /// <returns><c>true</c> when the label is common; otherwise, <c>false</c>.</returns>
    internal static bool IsCommonParameter(int label)
    {
        return RegistryLookup.Contains(commonParameters, label);
    }

    /// <summary>Determines whether a parameter is registered for a COSE key type.</summary>
    /// <param name="keyType">The integer key type.</param>
    /// <param name="label">The integer parameter label.</param>
    /// <returns><c>true</c> when the common or type-specific parameter is registered.</returns>
    internal static bool IsParameterForKeyType(int keyType, int label)
    {
        if (!IsRegisteredKeyType(keyType))
        {
            return false;
        }

        if (IsCommonParameter(label))
        {
            return true;
        }

        int[] parameters = GetParametersForKeyTypeInternal(keyType);
        return RegistryLookup.Contains(parameters, label);
    }

    /// <summary>Gets a common or type-specific COSE key parameter name.</summary>
    /// <param name="keyType">The integer key type.</param>
    /// <param name="label">The integer parameter label.</param>
    /// <returns>The parameter name, or <c>null</c> when the combination is unknown.</returns>
    internal static string? GetParameterName(int keyType, int label)
    {
        if (!IsRegisteredKeyType(keyType))
        {
            return null;
        }

        string? commonName = FindName(commonParameters, commonParameterNames, label);
        if (commonName != null)
        {
            return commonName;
        }

        int[] parameters = GetParametersForKeyTypeInternal(keyType);
        string[] names = GetParameterNamesForKeyTypeInternal(keyType);
        return FindName(parameters, names, label);
    }

    /// <summary>Determines whether a COSE key operation is registered.</summary>
    /// <param name="operation">The integer key operation.</param>
    /// <returns><c>true</c> when the operation is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegisteredOperation(int operation)
    {
        return RegistryLookup.Contains(operations, operation);
    }

    /// <summary>Gets the registered name of a COSE key operation.</summary>
    /// <param name="operation">The integer key operation.</param>
    /// <returns>The operation name, or <c>null</c> when it is unknown.</returns>
    internal static string? GetOperationName(int operation)
    {
        return FindName(operations, operationNames, operation);
    }

    /// <summary>Determines whether a COSE elliptic curve is registered.</summary>
    /// <param name="curve">The integer curve label.</param>
    /// <returns><c>true</c> when the curve is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegisteredCurve(int curve)
    {
        return RegistryLookup.Contains(curves, curve);
    }

    /// <summary>Gets the registered name of a COSE elliptic curve.</summary>
    /// <param name="curve">The integer curve label.</param>
    /// <returns>The curve name, or <c>null</c> when it is unknown.</returns>
    internal static string? GetCurveName(int curve)
    {
        return FindName(curves, curveNames, curve);
    }

    /// <summary>Returns all registered COSE key types.</summary>
    /// <returns>A new array containing the key types.</returns>
    internal static int[] GetKeyTypes()
    {
        return RegistryLookup.Copy(keyTypes);
    }

    /// <summary>Returns all common COSE key parameter labels.</summary>
    /// <returns>A new array containing the common labels.</returns>
    internal static int[] GetCommonParameters()
    {
        return RegistryLookup.Copy(commonParameters);
    }

    /// <summary>Returns all type-specific parameter labels for a registered key type.</summary>
    /// <param name="keyType">The integer key type.</param>
    /// <returns>A new array, or an empty array when the key type is unknown.</returns>
    internal static int[] GetParametersForKeyType(int keyType)
    {
        int[] parameters = GetParametersForKeyTypeInternal(keyType);
        return RegistryLookup.Copy(parameters);
    }

    /// <summary>Returns all registered COSE key operations.</summary>
    /// <returns>A new array containing the operations.</returns>
    internal static int[] GetOperations()
    {
        return RegistryLookup.Copy(operations);
    }

    /// <summary>Returns all registered COSE elliptic curves.</summary>
    /// <returns>A new array containing the curve labels.</returns>
    internal static int[] GetCurves()
    {
        return RegistryLookup.Copy(curves);
    }

    static int[] GetParametersForKeyTypeInternal(int keyType)
    {
        if (keyType == KeyTypeOkp)
        {
            return okpParameters;
        }

        if (keyType == KeyTypeEc2)
        {
            return ec2Parameters;
        }

        if (keyType == KeyTypeRsa)
        {
            return rsaParameters;
        }

        if (keyType == KeyTypeSymmetric)
        {
            return symmetricParameters;
        }

        if (keyType == KeyTypeHssLms)
        {
            return hssLmsParameters;
        }

        if (keyType == KeyTypeWalnutDsa)
        {
            return walnutDsaParameters;
        }

        if (keyType == KeyTypeAlgorithmKeyPair)
        {
            return algorithmKeyPairParameters;
        }

        return new int[0];
    }

    static string[] GetParameterNamesForKeyTypeInternal(int keyType)
    {
        if (keyType == KeyTypeOkp)
        {
            return okpParameterNames;
        }

        if (keyType == KeyTypeEc2)
        {
            return ec2ParameterNames;
        }

        if (keyType == KeyTypeRsa)
        {
            return rsaParameterNames;
        }

        if (keyType == KeyTypeSymmetric)
        {
            return symmetricParameterNames;
        }

        if (keyType == KeyTypeHssLms)
        {
            return hssLmsParameterNames;
        }

        if (keyType == KeyTypeWalnutDsa)
        {
            return walnutDsaParameterNames;
        }

        if (keyType == KeyTypeAlgorithmKeyPair)
        {
            return algorithmKeyPairParameterNames;
        }

        return new string[0];
    }

    static string? FindName(int[] values, string[] names, int value)
    {
        int index = RegistryLookup.IndexOf(values, value);
        if (index < 0)
        {
            return null;
        }

        return names[index];
    }
}
