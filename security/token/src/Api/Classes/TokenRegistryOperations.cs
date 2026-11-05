using System;
namespace token;

/// <summary>
/// Implements registry inspection and cross-format identifier mappings.
/// </summary>
public class TokenRegistryOperations : ITokenRegistryOperations
{
    /// <summary>Initializes a stateless registry operations service.</summary>
    public TokenRegistryOperations()
    {
    }

    /// <summary>Gets metadata for a JOSE algorithm.</summary>
    /// <param name="algorithm">The case-sensitive algorithm name.</param>
    /// <returns>A new metadata object.</returns>
    public AlgorithmMetadata GetJoseAlgorithmMetadata(string algorithm)
    {
        return AlgorithmMetadataRegistry.GetJose(algorithm);
    }

    /// <summary>Gets metadata for a COSE algorithm.</summary>
    /// <param name="algorithm">The integer algorithm label.</param>
    /// <returns>A new metadata object.</returns>
    public AlgorithmMetadata GetCoseAlgorithmMetadata(int algorithm)
    {
        return AlgorithmMetadataRegistry.GetCose(algorithm);
    }

    /// <summary>Returns all registered JOSE algorithms.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetJoseAlgorithms()
    {
        return JoseAlgorithmsOperations.GetAll();
    }

    /// <summary>Returns all registered COSE algorithms.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public int[] GetCoseAlgorithms()
    {
        return CoseAlgorithmsOperations.GetAll();
    }

    /// <summary>Returns fully specified JOSE algorithm identifiers.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetFullySpecifiedJoseAlgorithms()
    {
        return AlgorithmMetadataRegistry.GetFullySpecifiedJose();
    }

    /// <summary>Returns fully specified COSE algorithm identifiers.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public int[] GetFullySpecifiedCoseAlgorithms()
    {
        return AlgorithmMetadataRegistry.GetFullySpecifiedCose();
    }

    /// <summary>Determines whether a JOSE algorithm is registered for signatures or MACs.</summary>
    /// <param name="algorithm">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a JWS algorithm; otherwise, <c>false</c>.</returns>
    public bool IsJoseSignatureAlgorithm(string algorithm)
    {
        return JoseAlgorithmsOperations.IsSignature(algorithm);
    }

    /// <summary>Determines whether a JOSE algorithm is registered for key management.</summary>
    /// <param name="algorithm">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a JWE key-management algorithm; otherwise, <c>false</c>.</returns>
    public bool IsJoseKeyManagementAlgorithm(string algorithm)
    {
        return JoseAlgorithmsOperations.IsKeyManagement(algorithm);
    }

    /// <summary>Determines whether a JOSE algorithm is registered for content encryption.</summary>
    /// <param name="algorithm">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a JWE content-encryption algorithm; otherwise, <c>false</c>.</returns>
    public bool IsJoseContentEncryptionAlgorithm(string algorithm)
    {
        return JoseAlgorithmsOperations.IsContentEncryption(algorithm);
    }

    /// <summary>Determines whether a JOSE algorithm is registered only for JWK metadata.</summary>
    /// <param name="algorithm">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a JWK-only algorithm; otherwise, <c>false</c>.</returns>
    public bool IsJoseJwkOnlyAlgorithm(string algorithm)
    {
        return JoseAlgorithmsOperations.IsJwkOnly(algorithm);
    }

    /// <summary>Maps a JOSE algorithm to its canonical COSE equivalent.</summary>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <returns>The exact mapping, or an unmapped result.</returns>
    public AlgorithmMappingResult MapJoseAlgorithmToCose(string algorithm)
    {
        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        return JoseCoseAlgorithmMap.FromJose(algorithm);
    }

    /// <summary>Maps a COSE algorithm to its JOSE equivalent.</summary>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <returns>The exact mapping, or an unmapped result.</returns>
    public AlgorithmMappingResult MapCoseAlgorithmToJose(int algorithm)
    {
        return JoseCoseAlgorithmMap.FromCose(algorithm);
    }

    /// <summary>Returns every current and recognized legacy COSE mapping for a JOSE algorithm.</summary>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <returns>A defensive array with the canonical label first.</returns>
    public int[] GetMappedCoseAlgorithms(string algorithm)
    {
        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        return JoseCoseAlgorithmMap.GetCoseLabels(algorithm);
    }

    /// <summary>Returns all registered JOSE header names.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetJoseHeaderNames()
    {
        return JoseHeaderNamesOperations.GetAll();
    }

    /// <summary>Determines whether a JOSE header is registered for JWS.</summary>
    /// <param name="name">The case-sensitive parameter name.</param>
    /// <returns><c>true</c> for a JWS parameter; otherwise, <c>false</c>.</returns>
    public bool IsJoseHeaderForJws(string name)
    {
        return JoseHeaderNamesOperations.IsForJws(name);
    }

    /// <summary>Determines whether a JOSE header is registered for JWE.</summary>
    /// <param name="name">The case-sensitive parameter name.</param>
    /// <returns><c>true</c> for a JWE parameter; otherwise, <c>false</c>.</returns>
    public bool IsJoseHeaderForJwe(string name)
    {
        return JoseHeaderNamesOperations.IsForJwe(name);
    }

    /// <summary>Returns registered JOSE compression algorithms.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetJoseCompressionAlgorithms()
    {
        return JoseCompressionAlgorithmsOperations.GetAll();
    }

    /// <summary>Returns registered JOSE, JWK, and JWT media types.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetJoseMediaTypes()
    {
        return JoseMediaTypesOperations.GetAll();
    }

    /// <summary>Normalizes a full media type or abbreviated application subtype.</summary>
    /// <param name="value">The media type or subtype.</param>
    /// <returns>The canonical media type, or <c>null</c> when unknown.</returns>
    public string? NormalizeJoseMediaType(string? value)
    {
        return JoseMediaTypesOperations.Normalize(value);
    }

    /// <summary>Returns all registered JWT claim names.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetJwtClaimNames()
    {
        return JwtClaimNamesOperations.GetAll();
    }

    /// <summary>Returns registered JWK member names.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetJwkParameterNames()
    {
        return JwkRegistryOperations.GetParameters();
    }

    /// <summary>Returns registered JWK key types.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetJwkKeyTypes()
    {
        return JwkRegistryOperations.GetKeyTypes();
    }

    /// <summary>Returns registered JWK public-key uses.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetJwkKeyUses()
    {
        return JwkRegistryOperations.GetKeyUses();
    }

    /// <summary>Returns registered JWK key operations.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetJwkKeyOperations()
    {
        return JwkRegistryOperations.GetKeyOperations();
    }

    /// <summary>Returns registered JWK curve names.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public string[] GetJwkCurveNames()
    {
        return JwkRegistryOperations.GetCurves();
    }

    /// <summary>Gets the registered name of a COSE algorithm.</summary>
    /// <param name="algorithm">The algorithm label.</param>
    /// <returns>The name, or <c>null</c> when unknown.</returns>
    public string? GetCoseAlgorithmName(int algorithm)
    {
        return CoseAlgorithmsOperations.GetName(algorithm);
    }

    /// <summary>Returns all registered COSE header labels.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public int[] GetCoseHeaderLabels()
    {
        return CoseHeaderLabelsOperations.GetAll();
    }

    /// <summary>Gets the registered name of a COSE header label.</summary>
    /// <param name="label">The header label.</param>
    /// <returns>The name, or <c>null</c> when unknown.</returns>
    public string? GetCoseHeaderName(int label)
    {
        return CoseHeaderLabelsOperations.GetName(label);
    }

    /// <summary>Returns all registered algorithm-specific COSE header labels.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public int[] GetCoseAlgorithmHeaderLabels()
    {
        return CoseHeaderLabelsOperations.GetAllAlgorithmParameters();
    }

    /// <summary>Gets the registered name of an algorithm-specific COSE header label.</summary>
    /// <param name="label">The header label.</param>
    /// <returns>The name, or <c>null</c> when unknown.</returns>
    public string? GetCoseAlgorithmHeaderName(int label)
    {
        return CoseHeaderLabelsOperations.GetAlgorithmParameterName(label);
    }

    /// <summary>Returns all registered COSE key types.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public int[] GetCoseKeyTypes()
    {
        return CoseKeyLabelsOperations.GetKeyTypes();
    }

    /// <summary>Gets the registered name of a COSE key type.</summary>
    /// <param name="keyType">The key type label.</param>
    /// <returns>The name, or <c>null</c> when unknown.</returns>
    public string? GetCoseKeyTypeName(int keyType)
    {
        return CoseKeyLabelsOperations.GetKeyTypeName(keyType);
    }

    /// <summary>Returns common COSE key parameter labels.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public int[] GetCoseCommonKeyParameters()
    {
        return CoseKeyLabelsOperations.GetCommonParameters();
    }

    /// <summary>Returns type-specific COSE key parameter labels.</summary>
    /// <param name="keyType">The registered key type.</param>
    /// <returns>A defensive array, or an empty array when unknown.</returns>
    public int[] GetCoseKeyParameters(int keyType)
    {
        return CoseKeyLabelsOperations.GetParametersForKeyType(keyType);
    }

    /// <summary>Gets a common or type-specific COSE key parameter name.</summary>
    /// <param name="keyType">The registered key type.</param>
    /// <param name="label">The parameter label.</param>
    /// <returns>The name, or <c>null</c> when unknown.</returns>
    public string? GetCoseKeyParameterName(int keyType, int label)
    {
        return CoseKeyLabelsOperations.GetParameterName(keyType, label);
    }

    /// <summary>Returns all registered COSE key operations.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public int[] GetCoseKeyOperations()
    {
        return CoseKeyLabelsOperations.GetOperations();
    }

    /// <summary>Gets the registered name of a COSE key operation.</summary>
    /// <param name="operation">The operation label.</param>
    /// <returns>The name, or <c>null</c> when unknown.</returns>
    public string? GetCoseKeyOperationName(int operation)
    {
        return CoseKeyLabelsOperations.GetOperationName(operation);
    }

    /// <summary>Returns all registered COSE curve labels.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public int[] GetCoseCurves()
    {
        return CoseKeyLabelsOperations.GetCurves();
    }

    /// <summary>Gets the registered name of a COSE curve.</summary>
    /// <param name="curve">The curve label.</param>
    /// <returns>The name, or <c>null</c> when unknown.</returns>
    public string? GetCoseCurveName(int curve)
    {
        return CoseKeyLabelsOperations.GetCurveName(curve);
    }

    /// <summary>Returns all registered CWT claim labels.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public int[] GetCwtClaimLabels()
    {
        return CwtClaimLabelsOperations.GetAll();
    }

    /// <summary>Gets the registered name of a CWT claim label.</summary>
    /// <param name="label">The claim label.</param>
    /// <returns>The name, or <c>null</c> when unknown.</returns>
    public string? GetCwtClaimName(int label)
    {
        return CwtClaimLabelsOperations.GetName(label);
    }

    /// <summary>Returns all registered CWT confirmation method labels.</summary>
    /// <returns>A defensive array snapshot.</returns>
    public int[] GetCwtConfirmationMethodLabels()
    {
        return CwtConfirmationMethodLabelsOperations.GetAll();
    }

    /// <summary>Gets the registered name of a CWT confirmation method.</summary>
    /// <param name="label">The confirmation method label.</param>
    /// <returns>The name, or <c>null</c> when unknown.</returns>
    public string? GetCwtConfirmationMethodName(int label)
    {
        return CwtConfirmationMethodLabelsOperations.GetName(label);
    }

    /// <summary>Maps a JWT claim name to its registered CWT claim label.</summary>
    /// <param name="claimName">The case-sensitive JWT claim name.</param>
    /// <returns>The mapping, or an unmapped result.</returns>
    public ClaimMappingResult MapJwtClaimToCwt(string claimName)
    {
        if (claimName == null)
        {
            throw new ArgumentNullException("claimName");
        }

        return JwtCwtClaimMap.FromJwt(claimName);
    }

    /// <summary>Maps a CWT claim label to its registered JWT claim name.</summary>
    /// <param name="claimLabel">The CWT claim label.</param>
    /// <returns>The mapping, or an unmapped result.</returns>
    public ClaimMappingResult MapCwtClaimToJwt(int claimLabel)
    {
        return JwtCwtClaimMap.FromCwt(claimLabel);
    }
}
