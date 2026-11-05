using System;
using System.Collections.Generic;

namespace token;

/// <summary>
/// Validates integrity-protected JOSE critical header parameters.
/// </summary>
internal static class JoseCriticalHeaderValidator
{
    static readonly string[] definedParameters = new string[]
    {
        JoseHeaderNames.Algorithm, JoseHeaderNames.JwkSetUrl,
        JoseHeaderNames.Jwk, JoseHeaderNames.KeyId, JoseHeaderNames.X509Url,
        JoseHeaderNames.X509CertificateChain,
        JoseHeaderNames.X509Sha1Thumbprint,
        JoseHeaderNames.X509Sha256Thumbprint, JoseHeaderNames.Type,
        JoseHeaderNames.ContentType, JoseHeaderNames.Critical,
        JoseHeaderNames.EncryptionAlgorithm,
        JoseHeaderNames.CompressionAlgorithm,
        JoseHeaderNames.EphemeralPublicKey,
        JoseHeaderNames.AgreementPartyUInfo,
        JoseHeaderNames.AgreementPartyVInfo,
        JoseHeaderNames.InitializationVector,
        JoseHeaderNames.AuthenticationTag, JoseHeaderNames.Pbes2SaltInput,
        JoseHeaderNames.Pbes2Count
    };

    /// <summary>Validates critical parameters in a fully protected JOSE header.</summary>
    /// <param name="protectedHeader">The integrity-protected JOSE header.</param>
    /// <param name="understoodParameters">Extension parameter names the caller understands and will process.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult Validate(
        JoseHeader protectedHeader,
        string[] understoodParameters)
    {
        return Validate(
            protectedHeader,
            Array.Empty<JoseHeader>(),
            understoodParameters);
    }

    /// <summary>Validates critical parameters and uniqueness across protected and unprotected JOSE headers.</summary>
    /// <param name="protectedHeader">The integrity-protected JOSE header.</param>
    /// <param name="unprotectedHeaders">The shared, per-signature, or per-recipient unprotected headers.</param>
    /// <param name="understoodParameters">Extension parameter names the caller understands and will process.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult Validate(
        JoseHeader protectedHeader,
        JoseHeader[] unprotectedHeaders,
        string[] understoodParameters)
    {
        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
        }

        if (unprotectedHeaders == null)
        {
            throw new ArgumentNullException("unprotectedHeaders");
        }

        if (understoodParameters == null)
        {
            throw new ArgumentNullException("understoodParameters");
        }

        TokenResult structure = ValidateStructure(
            protectedHeader,
            unprotectedHeaders);
        if (!structure.Success)
        {
            return structure;
        }

        int headerIndex = 0;
        while (headerIndex < unprotectedHeaders.Length)
        {
            if (unprotectedHeaders[headerIndex].Get(JoseHeaderNames.Critical) != null)
            {
                return Failure(
                    "critical_header_not_protected",
                    "The JOSE crit parameter must occur only in the protected header.",
                    headerIndex);
            }

            headerIndex += 1;
        }

        JsonValue? critical = protectedHeader.Get(JoseHeaderNames.Critical);
        if (critical == null)
        {
            return Success();
        }

        if (critical.Kind != TokenJsonValueKind.Array
            || critical.ArrayValue == null)
        {
            return Failure(
                "invalid_critical_header",
                "The JOSE crit parameter must be an array of strings.",
                -1);
        }

        if (critical.ArrayValue.Length == 0)
        {
            return Failure(
                "invalid_critical_header",
                "The JOSE crit parameter must not be empty.",
                -1);
        }

        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < critical.ArrayValue.Length)
        {
            JsonValue item = critical.ArrayValue[index];
            if (item == null || item.Kind != TokenJsonValueKind.String
                || item.StringValue == null)
            {
                return Failure(
                    "invalid_critical_header",
                    "The JOSE crit parameter must contain only strings.",
                    index);
            }

            string name = item.StringValue;
            if (!names.Add(name))
            {
                return Failure(
                    "duplicate_critical_header",
                    "The JOSE crit parameter contains a duplicate name.",
                    index);
            }

            if (RegistryLookup.Contains(definedParameters, name, false))
            {
                return Failure(
                    "invalid_critical_header",
                    "The JOSE crit parameter lists a header defined by JWS, JWE, or JWA.",
                    index);
            }

            if (protectedHeader.Get(name) == null)
            {
                return Failure(
                    "missing_critical_header",
                    "A parameter listed by crit is absent from the protected header.",
                    index);
            }

            if (!RegistryLookup.Contains(understoodParameters, name, false))
            {
                return Failure(
                    "unknown_critical_header",
                    "A critical JOSE header parameter is not understood and supported.",
                    index);
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult ValidateStructure(
        JoseHeader protectedHeader,
        JoseHeader[] unprotectedHeaders)
    {
        if (protectedHeader.Parameters == null
            || protectedHeader.Parameters.Members == null)
        {
            return Failure(
                "invalid_header",
                "The protected JOSE header is incomplete.",
                -1);
        }

        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        TokenResult protectedResult = AddNames(
            protectedHeader,
            names,
            -1);
        if (!protectedResult.Success)
        {
            return protectedResult;
        }

        int index = 0;
        while (index < unprotectedHeaders.Length)
        {
            if (unprotectedHeaders[index] == null)
            {
                return Failure(
                    "invalid_header",
                    "An unprotected JOSE header is missing.",
                    index);
            }

            if (unprotectedHeaders[index].Parameters == null
                || unprotectedHeaders[index].Parameters.Members == null)
            {
                return Failure(
                    "invalid_header",
                    "An unprotected JOSE header is incomplete.",
                    index);
            }

            TokenResult added = AddNames(
                unprotectedHeaders[index],
                names,
                index);
            if (!added.Success)
            {
                return added;
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult AddNames(
        JoseHeader header,
        HashSet<string> names,
        int headerIndex)
    {
        int index = 0;
        while (index < header.Parameters.Members.Length)
        {
            JsonMember member = header.Parameters.Members[index];
            if (member == null || member.Name == null || member.Value == null)
            {
                return Failure(
                    "invalid_header",
                    "A JOSE header member is incomplete.",
                    headerIndex);
            }

            if (!names.Add(member.Name))
            {
                return Failure(
                    "duplicate_header_parameter",
                    "A JOSE header parameter occurs in more than one header location.",
                    headerIndex);
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult Failure(
        string code,
        string message,
        int position)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, position);
        return result;
    }
}
