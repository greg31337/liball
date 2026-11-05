using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Configures RFC 9901 SD-JWT parsing, authentication, and Disclosure processing.
/// </summary>
public class SelectiveDisclosureJwtValidationPolicy
{
    /// <summary>Contains generic JWT cryptographic, claim, type, and lifetime settings.</summary>
    public JwtValidationPolicy JwtPolicy;

    /// <summary>Contains the case-sensitive Disclosure hash algorithms accepted by the verifier.</summary>
    public string[] AllowedHashAlgorithms;

    /// <summary>Contains the maximum number of Disclosures accepted in one SD-JWT.</summary>
    public int MaximumDisclosures;

    /// <summary>Contains the maximum decoded size of one Disclosure in bytes.</summary>
    public int MaximumDisclosureSizeInBytes;

    /// <summary>Indicates whether an authenticated RFC 9901 Key Binding JWT is required.</summary>
    public bool RequireKeyBinding;

    /// <summary>Contains the exact verifier audience required in a Key Binding JWT.</summary>
    public string? ExpectedKeyBindingAudience;

    /// <summary>Contains the exact transaction nonce required in a Key Binding JWT.</summary>
    public string? ExpectedKeyBindingNonce;

    /// <summary>Contains the maximum accepted Key Binding JWT age in seconds.</summary>
    public int MaximumKeyBindingAgeSeconds;

    /// <summary>Initializes a policy accepting SHA-256 and at most 256 Disclosures.</summary>
    public SelectiveDisclosureJwtValidationPolicy()
    {
        JwtPolicy = new JwtValidationPolicy();
        AllowedHashAlgorithms = new string[]
        {
            SelectiveDisclosureHashAlgorithms.Sha256
        };
        MaximumDisclosures = 256;
        MaximumDisclosureSizeInBytes = 65536;
        RequireKeyBinding = false;
        ExpectedKeyBindingAudience = null;
        ExpectedKeyBindingNonce = null;
        MaximumKeyBindingAgeSeconds = 300;
    }

    /// <summary>Checks the SD-JWT policy for missing, duplicate, or unsupported values.</summary>
    /// <returns>A successful result when validation can proceed, or stable failure details.</returns>
    internal TokenResult Validate()
    {
        if (JwtPolicy == null)
        {
            return Failure(
                "invalid_sd_jwt_policy",
                "The SD-JWT validation policy is missing its JWT policy.");
        }

        TokenResult jwtPolicy = JwtPolicy.Validate();
        if (!jwtPolicy.Success)
        {
            return jwtPolicy;
        }

        if (AllowedHashAlgorithms == null
            || AllowedHashAlgorithms.Length == 0
            || MaximumDisclosures < 0
            || MaximumDisclosures > 65536
            || MaximumDisclosureSizeInBytes <= 0
            || MaximumDisclosureSizeInBytes
                > (JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).MaximumTokenSizeInBytes
            || MaximumKeyBindingAgeSeconds < 0
            || ExpectedKeyBindingAudience != null
                && (ExpectedKeyBindingAudience.Length == 0
                    || !Utf8Codec.Encode(ExpectedKeyBindingAudience).Success)
            || ExpectedKeyBindingNonce != null
                && (ExpectedKeyBindingNonce.Length == 0
                    || !Utf8Codec.Encode(ExpectedKeyBindingNonce).Success)
            || RequireKeyBinding
                && (ExpectedKeyBindingAudience == null
                    || ExpectedKeyBindingNonce == null
                    || MaximumKeyBindingAgeSeconds == 0))
        {
            return Failure(
                "invalid_sd_jwt_policy",
                "The SD-JWT validation policy contains an invalid limit or hash allowlist.");
        }

        int index = 0;
        while (index < AllowedHashAlgorithms.Length)
        {
            string algorithm = AllowedHashAlgorithms[index];
            if (!SupportedAlgorithm(algorithm))
            {
                return Failure(
                    "invalid_sd_jwt_policy",
                    "The SD-JWT hash allowlist contains an unsupported algorithm.");
            }

            int previous = 0;
            while (previous < index)
            {
                if (string.Equals(
                    AllowedHashAlgorithms[previous],
                    algorithm,
                    StringComparison.Ordinal))
                {
                    return Failure(
                        "invalid_sd_jwt_policy",
                        "The SD-JWT hash allowlist contains a duplicate algorithm.");
                }

                previous += 1;
            }

            index += 1;
        }

        return Success();
    }

    internal static bool SupportedAlgorithm(string algorithm)
    {
        return string.Equals(
            algorithm,
            SelectiveDisclosureHashAlgorithms.Sha256,
            StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                SelectiveDisclosureHashAlgorithms.Sha384,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                SelectiveDisclosureHashAlgorithms.Sha512,
                StringComparison.Ordinal);
    }

    internal static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static TokenResult Failure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
