using System;
using System.Collections.Generic;

namespace token;

/// <summary>Configures registered CWT claim and lifetime validation.</summary>
public class CwtValidationPolicy
{
    /// <summary>Contains common resource and clock-skew controls.</summary>
    public TokenPolicy? TokenPolicy;

    /// <summary>Supplies current UTC time for deterministic lifetime validation.</summary>
    public ITokenClock? Clock;

    /// <summary>Indicates whether the issuer must match <see cref="ValidIssuers"/>.</summary>
    public bool ValidateIssuer;

    /// <summary>Indicates whether the audience must match <see cref="ValidAudiences"/>.</summary>
    public bool ValidateAudience;

    /// <summary>Indicates whether an expiration claim is validated when present.</summary>
    public bool ValidateExpirationTime;

    /// <summary>Indicates whether a not-before claim is validated when present.</summary>
    public bool ValidateNotBefore;

    /// <summary>Indicates whether an issued-at claim is validated when present.</summary>
    public bool ValidateIssuedAt;

    /// <summary>Indicates whether an expiration claim is required.</summary>
    public bool RequireExpirationTime;

    /// <summary>The maximum token age in seconds, or zero when unlimited.</summary>
    public long MaximumTokenAgeSeconds;

    /// <summary>Contains exact issuer values accepted by the policy.</summary>
    public string[]? ValidIssuers;

    /// <summary>Contains exact audience values accepted by the policy.</summary>
    public string[]? ValidAudiences;

    /// <summary>Contains integer claim labels required by the policy.</summary>
    public int[]? RequiredClaims;

    /// <summary>Contains case-sensitive text claim labels required by the policy.</summary>
    public string[]? RequiredTextClaims;

    /// <summary>Indicates whether signed CWT protection layers are accepted.</summary>
    public bool AllowSignedTokens;

    /// <summary>Indicates whether deprecated polymorphic COSE signature labels are accepted.</summary>
    public bool AllowCompatibilitySignatures;

    /// <summary>Indicates whether MACed CWT protection layers are accepted.</summary>
    public bool AllowMacTokens;

    /// <summary>Indicates whether encrypted CWT protection layers are accepted.</summary>
    public bool AllowEncryptedTokens;

    /// <summary>Indicates whether more than one CWT protection layer is accepted.</summary>
    public bool AllowNestedTokens;

    /// <summary>The maximum number of authenticated CWT protection layers.</summary>
    public int MaximumProtectionLayers;

    /// <summary>Indicates whether the complete CWT must use CBOR tag 61.</summary>
    public bool RequireCwtTag;

    /// <summary>Contains external authenticated data in outermost-to-innermost layer order.</summary>
    public byte[]?[]? LayerExternalAuthenticatedData;

    /// <summary>Initializes conservative registered-claim validation defaults.</summary>
    public CwtValidationPolicy()
    {
        Initialize(new TokenPolicy());
    }

    /// <summary>Creates a policy from an independent copy of common controls.</summary>
    /// <param name="tokenPolicy">The common policy to copy.</param>
    /// <returns>A CWT validation policy initialized from the common controls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>tokenPolicy</c> is null.</exception>
    internal static CwtValidationPolicy Create(TokenPolicy tokenPolicy)
    {
        if (tokenPolicy == null)
        {
            throw new ArgumentNullException("tokenPolicy");
        }

        CwtValidationPolicy result = new CwtValidationPolicy();
        result.Initialize(CopyTokenPolicy(tokenPolicy));
        return result;
    }

    /// <summary>Creates an independent copy of this policy.</summary>
    /// <returns>A policy with independent mutable arrays.</returns>
    internal CwtValidationPolicy Copy()
    {
        CwtValidationPolicy result = TokenPolicy == null
            ? new CwtValidationPolicy()
            : Create(TokenPolicy);
        if (TokenPolicy == null)
        {
            result.TokenPolicy = null;
        }

        result.Clock = Clock;
        result.ValidateIssuer = ValidateIssuer;
        result.ValidateAudience = ValidateAudience;
        result.ValidateExpirationTime = ValidateExpirationTime;
        result.ValidateNotBefore = ValidateNotBefore;
        result.ValidateIssuedAt = ValidateIssuedAt;
        result.RequireExpirationTime = RequireExpirationTime;
        result.MaximumTokenAgeSeconds = MaximumTokenAgeSeconds;
        result.ValidIssuers = CopyStrings(ValidIssuers);
        result.ValidAudiences = CopyStrings(ValidAudiences);
        result.RequiredClaims = CopyIntegers(RequiredClaims);
        result.RequiredTextClaims = CopyStrings(RequiredTextClaims);
        result.AllowSignedTokens = AllowSignedTokens;
        result.AllowCompatibilitySignatures = AllowCompatibilitySignatures;
        result.AllowMacTokens = AllowMacTokens;
        result.AllowEncryptedTokens = AllowEncryptedTokens;
        result.AllowNestedTokens = AllowNestedTokens;
        result.MaximumProtectionLayers = MaximumProtectionLayers;
        result.RequireCwtTag = RequireCwtTag;
        result.LayerExternalAuthenticatedData = CopyByteArrays(
            LayerExternalAuthenticatedData);
        return result;
    }

    /// <summary>Checks this policy for missing or contradictory configuration.</summary>
    /// <returns>A successful result when the policy can be used.</returns>
    internal TokenResult Validate()
    {
        if (TokenPolicy == null
            || Clock == null
            || ValidIssuers == null
            || ValidAudiences == null
            || RequiredClaims == null
            || RequiredTextClaims == null
            || LayerExternalAuthenticatedData == null)
        {
            return Failure(
                "invalid_policy",
                "The CWT validation policy is incomplete.");
        }

        if (TokenPolicy.MaximumTokenSizeInBytes <= 0
            || TokenPolicy.MaximumCborDepth <= 0
            || TokenPolicy.ClockSkewSeconds < 0
            || MaximumTokenAgeSeconds < 0
            || MaximumProtectionLayers <= 0)
        {
            return Failure(
                "invalid_policy",
                "The CWT validation policy contains an invalid value.");
        }

        if (RequireExpirationTime && !ValidateExpirationTime)
        {
            return Failure(
                "invalid_policy",
                "A required expiration claim must also be validated.");
        }

        if (MaximumTokenAgeSeconds > 0 && !ValidateIssuedAt)
        {
            return Failure(
                "invalid_policy",
                "A maximum token age requires issued-at validation.");
        }

        if (ValidateIssuer && ValidIssuers.Length == 0
            || ValidateAudience && ValidAudiences.Length == 0)
        {
            return Failure(
                "invalid_policy",
                "An enabled CWT value check requires an accepted value.");
        }

        if (!AllowSignedTokens
            && !AllowMacTokens
            && !AllowEncryptedTokens)
        {
            return Failure(
                "invalid_policy",
                "CWT validation requires at least one allowed protection form.");
        }

        if (AllowNestedTokens && MaximumProtectionLayers < 2)
        {
            return Failure(
                "invalid_policy",
                "Nested CWT validation requires at least two protection layers.");
        }

        int dataIndex = 0;
        while (dataIndex < LayerExternalAuthenticatedData.Length)
        {
            if (LayerExternalAuthenticatedData[dataIndex] == null)
            {
                return Failure(
                    "invalid_policy",
                    "CWT external authenticated data entries cannot be null.");
            }

            dataIndex += 1;
        }

        if (!ValidStrings(ValidIssuers)
            || !ValidStrings(ValidAudiences)
            || !ValidStrings(RequiredTextClaims)
            || !ValidIntegers(RequiredClaims))
        {
            return Failure(
                "invalid_policy",
                "CWT validation policy values must be nonempty and unique.");
        }

        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal void Initialize(TokenPolicy tokenPolicy)
    {
        TokenPolicy = tokenPolicy;
        Clock = TokenClock.CreateSystem();
        ValidateIssuer = false;
        ValidateAudience = false;
        ValidateExpirationTime = true;
        ValidateNotBefore = true;
        ValidateIssuedAt = true;
        RequireExpirationTime = false;
        MaximumTokenAgeSeconds = 0;
        ValidIssuers = Array.Empty<string>();
        ValidAudiences = Array.Empty<string>();
        RequiredClaims = Array.Empty<int>();
        RequiredTextClaims = Array.Empty<string>();
        AllowSignedTokens = true;
        AllowCompatibilitySignatures = false;
        AllowMacTokens = true;
        AllowEncryptedTokens = true;
        AllowNestedTokens = true;
        MaximumProtectionLayers = 8;
        RequireCwtTag = false;
        LayerExternalAuthenticatedData = Array.Empty<byte[]?>();
    }

    internal static TokenPolicy CopyTokenPolicy(TokenPolicy source)
    {
        TokenPolicy result = new TokenPolicy();
        result.MaximumTokenSizeInBytes = source.MaximumTokenSizeInBytes;
        result.MaximumJsonDepth = source.MaximumJsonDepth;
        result.MaximumCborDepth = source.MaximumCborDepth;
        result.ClockSkewSeconds = source.ClockSkewSeconds;
        result.AllowUnsecuredTokens = source.AllowUnsecuredTokens;
        result.AllowLegacyRsaPkcs1Encryption =
            source.AllowLegacyRsaPkcs1Encryption;
        result.AllowUnauthenticatedDecode = source.AllowUnauthenticatedDecode;
        result.AllowCompression = source.AllowCompression;
        result.MaximumDecompressedSizeInBytes =
            source.MaximumDecompressedSizeInBytes;
        result.MaximumCompressionRatio = source.MaximumCompressionRatio;
        result.AllowRemoteKeys = source.AllowRemoteKeys;
        result.AllowedRemoteKeyHosts = CopyStrings(
            source.AllowedRemoteKeyHosts);
        result.MaximumRemoteKeySetSizeInBytes =
            source.MaximumRemoteKeySetSizeInBytes;
        result.MaximumRemoteKeyCacheEntries =
            source.MaximumRemoteKeyCacheEntries;
        result.RemoteKeyRefreshSeconds = source.RemoteKeyRefreshSeconds;
        result.RemoteKeyStaleIfErrorSeconds =
            source.RemoteKeyStaleIfErrorSeconds;
        result.RemoteKeyTimeoutSeconds = source.RemoteKeyTimeoutSeconds;
        result.AllowedAlgorithms = CopyStrings(source.AllowedAlgorithms);
        result.UnderstoodCriticalHeaders = CopyStrings(
            source.UnderstoodCriticalHeaders);
        result.RequiredClaims = CopyStrings(source.RequiredClaims);
        return result;
    }

    internal static string[]? CopyStrings(string[]? values)
    {
        return values == null ? null : values.AsSpan().ToArray();
    }

    internal static int[]? CopyIntegers(int[]? values)
    {
        return values == null ? null : values.AsSpan().ToArray();
    }

    internal static byte[]?[]? CopyByteArrays(byte[]?[]? values)
    {
        if (values == null)
        {
            return null;
        }

        byte[]?[] result = new byte[]?[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index] == null
                ? null
                : values[index].AsSpan().ToArray();
            index += 1;
        }

        return result;
    }

    internal static bool ValidStrings(string[] values)
    {
        HashSet<string> found = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < values.Length)
        {
            string value = values[index];
            if (value == null
                || value.Length == 0
                || !Utf8Codec.Encode(value).Success
                || !found.Add(value))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool ValidIntegers(int[] values)
    {
        HashSet<int> found = new HashSet<int>();
        int index = 0;
        while (index < values.Length)
        {
            if (!found.Add(values[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static TokenResult Failure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
