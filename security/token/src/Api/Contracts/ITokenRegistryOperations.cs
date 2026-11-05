using System;
namespace token;

/// <summary>Maps registered algorithm and claim identifiers between token formats.</summary>
public interface ITokenRegistryOperations
{
    /// <summary>Gets security and implementation metadata for a JOSE algorithm.</summary>
    /// <param name="algorithm">The case-sensitive algorithm name.</param>
    /// <returns>A new metadata object, including unknown-name state.</returns>
    AlgorithmMetadata GetJoseAlgorithmMetadata(string algorithm);

    /// <summary>Gets security and implementation metadata for a COSE algorithm.</summary>
    /// <param name="algorithm">The integer algorithm label.</param>
    /// <returns>A new metadata object, including unknown-label state.</returns>
    AlgorithmMetadata GetCoseAlgorithmMetadata(int algorithm);

    /// <summary>Returns all registered JOSE algorithm names known by the library.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetJoseAlgorithms();

    /// <summary>Returns all registered COSE algorithm labels known by the library.</summary>
    /// <returns>A defensive array snapshot.</returns>
    int[] GetCoseAlgorithms();

    /// <summary>Returns JOSE algorithm identifiers classified as fully specified.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetFullySpecifiedJoseAlgorithms();

    /// <summary>Returns COSE algorithm identifiers classified as fully specified.</summary>
    /// <returns>A defensive array snapshot.</returns>
    int[] GetFullySpecifiedCoseAlgorithms();

    /// <summary>Determines whether a JOSE algorithm is registered for signatures or MACs.</summary>
    /// <param name="algorithm">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a registered JWS algorithm; otherwise, <c>false</c>.</returns>
    bool IsJoseSignatureAlgorithm(string algorithm);

    /// <summary>Determines whether a JOSE algorithm is registered for key management.</summary>
    /// <param name="algorithm">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a registered JWE key-management algorithm; otherwise, <c>false</c>.</returns>
    bool IsJoseKeyManagementAlgorithm(string algorithm);

    /// <summary>Determines whether a JOSE algorithm is registered for content encryption.</summary>
    /// <param name="algorithm">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a registered JWE content-encryption algorithm; otherwise, <c>false</c>.</returns>
    bool IsJoseContentEncryptionAlgorithm(string algorithm);

    /// <summary>Determines whether a JOSE algorithm is registered only as JWK metadata.</summary>
    /// <param name="algorithm">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a JWK-only algorithm; otherwise, <c>false</c>.</returns>
    bool IsJoseJwkOnlyAlgorithm(string algorithm);

    /// <summary>Maps a JOSE algorithm to its canonical COSE equivalent.</summary>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <returns>The exact mapping, or an unmapped result.</returns>
    AlgorithmMappingResult MapJoseAlgorithmToCose(string algorithm);

    /// <summary>Maps a COSE algorithm to its JOSE equivalent.</summary>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <returns>The exact mapping, including recognized legacy labels, or an unmapped result.</returns>
    AlgorithmMappingResult MapCoseAlgorithmToJose(int algorithm);

    /// <summary>Returns every current and recognized legacy COSE mapping for a JOSE algorithm.</summary>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <returns>A defensive array with the canonical label first.</returns>
    int[] GetMappedCoseAlgorithms(string algorithm);

    /// <summary>Returns all registered JOSE header parameter names.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetJoseHeaderNames();

    /// <summary>Determines whether a JOSE header parameter is registered for JWS.</summary>
    /// <param name="name">The case-sensitive parameter name.</param>
    /// <returns><c>true</c> for a JWS parameter; otherwise, <c>false</c>.</returns>
    bool IsJoseHeaderForJws(string name);

    /// <summary>Determines whether a JOSE header parameter is registered for JWE.</summary>
    /// <param name="name">The case-sensitive parameter name.</param>
    /// <returns><c>true</c> for a JWE parameter; otherwise, <c>false</c>.</returns>
    bool IsJoseHeaderForJwe(string name);

    /// <summary>Returns all registered JOSE compression algorithms.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetJoseCompressionAlgorithms();

    /// <summary>Returns all registered JOSE, JWK, and JWT media types known by the library.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetJoseMediaTypes();

    /// <summary>Normalizes a full media type or abbreviated application subtype.</summary>
    /// <param name="value">The media type or subtype.</param>
    /// <returns>The canonical media type, or <c>null</c> when unknown.</returns>
    string? NormalizeJoseMediaType(string? value);

    /// <summary>Returns all registered JWT claim names known by the library.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetJwtClaimNames();

    /// <summary>Returns all registered JWK member names used by the library.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetJwkParameterNames();

    /// <summary>Returns registered JWK key types supported by the normalized key model.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetJwkKeyTypes();

    /// <summary>Returns registered JWK public-key uses.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetJwkKeyUses();

    /// <summary>Returns registered JWK key operations.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetJwkKeyOperations();

    /// <summary>Returns registered JWK curve names known by the library.</summary>
    /// <returns>A defensive array snapshot.</returns>
    string[] GetJwkCurveNames();

    /// <summary>Gets the registered name of a COSE algorithm label.</summary>
    /// <param name="algorithm">The algorithm label.</param>
    /// <returns>The registered name, or <c>null</c> when unknown.</returns>
    string? GetCoseAlgorithmName(int algorithm);

    /// <summary>Returns all registered COSE header labels known by the library.</summary>
    /// <returns>A defensive array snapshot.</returns>
    int[] GetCoseHeaderLabels();

    /// <summary>Gets the registered name of a COSE header label.</summary>
    /// <param name="label">The header label.</param>
    /// <returns>The registered name, or <c>null</c> when unknown.</returns>
    string? GetCoseHeaderName(int label);

    /// <summary>Returns all registered algorithm-specific COSE header labels.</summary>
    /// <returns>A defensive array snapshot.</returns>
    int[] GetCoseAlgorithmHeaderLabels();

    /// <summary>Gets the registered name of an algorithm-specific COSE header label.</summary>
    /// <param name="label">The header label.</param>
    /// <returns>The registered name, or <c>null</c> when unknown.</returns>
    string? GetCoseAlgorithmHeaderName(int label);

    /// <summary>Returns all registered COSE key types known by the library.</summary>
    /// <returns>A defensive array snapshot.</returns>
    int[] GetCoseKeyTypes();

    /// <summary>Gets the registered name of a COSE key type.</summary>
    /// <param name="keyType">The key type label.</param>
    /// <returns>The registered name, or <c>null</c> when unknown.</returns>
    string? GetCoseKeyTypeName(int keyType);

    /// <summary>Returns common COSE key parameter labels.</summary>
    /// <returns>A defensive array snapshot.</returns>
    int[] GetCoseCommonKeyParameters();

    /// <summary>Returns type-specific COSE key parameter labels.</summary>
    /// <param name="keyType">The registered key type.</param>
    /// <returns>A defensive array, or an empty array when unknown.</returns>
    int[] GetCoseKeyParameters(int keyType);

    /// <summary>Gets a common or type-specific COSE key parameter name.</summary>
    /// <param name="keyType">The registered key type.</param>
    /// <param name="label">The parameter label.</param>
    /// <returns>The parameter name, or <c>null</c> when unknown.</returns>
    string? GetCoseKeyParameterName(int keyType, int label);

    /// <summary>Returns all registered COSE key operations.</summary>
    /// <returns>A defensive array snapshot.</returns>
    int[] GetCoseKeyOperations();

    /// <summary>Gets the registered name of a COSE key operation.</summary>
    /// <param name="operation">The operation label.</param>
    /// <returns>The operation name, or <c>null</c> when unknown.</returns>
    string? GetCoseKeyOperationName(int operation);

    /// <summary>Returns all registered COSE curve labels.</summary>
    /// <returns>A defensive array snapshot.</returns>
    int[] GetCoseCurves();

    /// <summary>Gets the registered name of a COSE curve.</summary>
    /// <param name="curve">The curve label.</param>
    /// <returns>The curve name, or <c>null</c> when unknown.</returns>
    string? GetCoseCurveName(int curve);

    /// <summary>Returns all registered CWT claim labels known by the library.</summary>
    /// <returns>A defensive array snapshot.</returns>
    int[] GetCwtClaimLabels();

    /// <summary>Gets the registered name of a CWT claim label.</summary>
    /// <param name="label">The claim label.</param>
    /// <returns>The registered name, or <c>null</c> when unknown.</returns>
    string? GetCwtClaimName(int label);

    /// <summary>Returns all registered CWT confirmation method labels known by the library.</summary>
    /// <returns>A defensive array snapshot.</returns>
    int[] GetCwtConfirmationMethodLabels();

    /// <summary>Gets the registered name of a CWT confirmation method.</summary>
    /// <param name="label">The confirmation method label.</param>
    /// <returns>The registered name, or <c>null</c> when unknown.</returns>
    string? GetCwtConfirmationMethodName(int label);

    /// <summary>Maps a JWT claim name to its registered CWT claim label.</summary>
    /// <param name="claimName">The case-sensitive JWT claim name.</param>
    /// <returns>The standards-defined mapping, or an unmapped result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>claimName</c> is null.</exception>
    ClaimMappingResult MapJwtClaimToCwt(string claimName);

    /// <summary>Maps a CWT integer claim label to its registered JWT claim name.</summary>
    /// <param name="claimLabel">The CWT integer claim label.</param>
    /// <returns>The standards-defined mapping, or an unmapped result.</returns>
    ClaimMappingResult MapCwtClaimToJwt(int claimLabel);
}
