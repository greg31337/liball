using System;
using System.Collections.Generic;

namespace token;

/// <summary>
/// Configures JWT cryptographic, claim, type, lifetime, and replay validation.
/// </summary>
public class JwtValidationPolicy
{
    /// <summary>Contains JOSE algorithm controls and resource limits.</summary>
    public TokenPolicy? TokenPolicy;

    /// <summary>Supplies current UTC time for deterministic lifetime validation.</summary>
    public ITokenClock? Clock;

    /// <summary>Indicates whether an issuer must match <see cref="ValidIssuers"/>.</summary>
    public bool ValidateIssuer;

    /// <summary>Indicates whether an audience must match <see cref="ValidAudiences"/>.</summary>
    public bool ValidateAudience;

    /// <summary>Indicates whether the expiration claim must be validated when present.</summary>
    public bool ValidateExpirationTime;

    /// <summary>Indicates whether the not-before claim must be validated when present.</summary>
    public bool ValidateNotBefore;

    /// <summary>Indicates whether the issued-at claim must be validated when present.</summary>
    public bool ValidateIssuedAt;

    /// <summary>Indicates whether an expiration claim is required.</summary>
    public bool RequireExpirationTime;

    /// <summary>Indicates whether a token type must match <see cref="ValidTokenTypes"/>.</summary>
    public bool ValidateTokenType;

    /// <summary>Indicates whether replicated encrypted-JWT header claims must equal payload claims.</summary>
    public bool ValidateHeaderClaimReplication;

    /// <summary>Indicates whether signed compact JWTs are accepted.</summary>
    public bool AllowSignedTokens;

    /// <summary>Indicates whether encrypted compact JWTs are accepted.</summary>
    public bool AllowEncryptedTokens;

    /// <summary>Indicates whether nested signed-then-encrypted JWTs are accepted.</summary>
    public bool AllowNestedTokens;

    /// <summary>The maximum token age in seconds, or zero when no maximum age is imposed.</summary>
    public long MaximumTokenAgeSeconds;

    /// <summary>The exact issuer values accepted when issuer validation is enabled.</summary>
    public string[]? ValidIssuers;

    /// <summary>The exact audience values accepted when audience validation is enabled.</summary>
    public string[]? ValidAudiences;

    /// <summary>The accepted protected <c>typ</c> values when token-type validation is enabled.</summary>
    public string[]? ValidTokenTypes;

    /// <summary>An optional callback for application-specific subject validation.</summary>
    public Func<JwtClaims, TokenResult>? SubjectValidator;

    /// <summary>An optional callback that validates each claim name and value.</summary>
    public Func<string, JsonValue, TokenResult>? ClaimValidator;

    /// <summary>An optional callback that rejects replayed JWT identifiers.</summary>
    public Func<string, DateTimeOffset, TokenResult>? ReplayValidator;

    /// <summary>Initializes a JWT policy with conservative validation defaults.</summary>
    public JwtValidationPolicy()
    {
        Initialize(new TokenPolicy());
    }

    /// <summary>Creates a JWT policy with an independent copy of JOSE controls and limits.</summary>
    /// <param name="tokenPolicy">The base token policy to copy.</param>
    /// <returns>A JWT validation policy initialized from the common controls.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>tokenPolicy</c> is null.</exception>
    internal static JwtValidationPolicy Create(TokenPolicy tokenPolicy)
    {
        if (tokenPolicy == null)
        {
            throw new ArgumentNullException("tokenPolicy");
        }

        JwtValidationPolicy result = new JwtValidationPolicy();
        result.Initialize(CopyTokenPolicy(tokenPolicy));
        return result;
    }

    /// <summary>Creates an independent copy of this validation policy.</summary>
    /// <returns>A policy whose mutable arrays and base policy are independent.</returns>
    internal JwtValidationPolicy Copy()
    {
        JwtValidationPolicy result = TokenPolicy == null
            ? new JwtValidationPolicy()
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
        result.ValidateTokenType = ValidateTokenType;
        result.ValidateHeaderClaimReplication = ValidateHeaderClaimReplication;
        result.AllowSignedTokens = AllowSignedTokens;
        result.AllowEncryptedTokens = AllowEncryptedTokens;
        result.AllowNestedTokens = AllowNestedTokens;
        result.MaximumTokenAgeSeconds = MaximumTokenAgeSeconds;
        result.ValidIssuers = CopyStrings(ValidIssuers);
        result.ValidAudiences = CopyStrings(ValidAudiences);
        result.ValidTokenTypes = CopyStrings(ValidTokenTypes);
        result.SubjectValidator = SubjectValidator;
        result.ClaimValidator = ClaimValidator;
        result.ReplayValidator = ReplayValidator;
        return result;
    }

    /// <summary>Checks the policy for missing, contradictory, or invalid configuration.</summary>
    /// <returns>A successful result when the policy can be used, or stable failure details.</returns>
    internal TokenResult Validate()
    {
        if (TokenPolicy == null || Clock == null)
        {
            return Failure("invalid_policy", "The JWT validation policy is incomplete.");
        }

        if (TokenPolicy.MaximumTokenSizeInBytes <= 0
            || TokenPolicy.MaximumJsonDepth <= 0
            || TokenPolicy.ClockSkewSeconds < 0
            || TokenPolicy.AllowedAlgorithms == null
            || TokenPolicy.UnderstoodCriticalHeaders == null
            || TokenPolicy.RequiredClaims == null
            || ValidIssuers == null
            || ValidAudiences == null
            || ValidTokenTypes == null
            || MaximumTokenAgeSeconds < 0)
        {
            return Failure("invalid_policy", "The JWT validation policy contains an invalid value.");
        }

        if (!AllowSignedTokens && !AllowEncryptedTokens)
        {
            return Failure("invalid_policy", "The JWT validation policy rejects every authenticated token form.");
        }

        if (AllowNestedTokens && (!AllowSignedTokens || !AllowEncryptedTokens))
        {
            return Failure("invalid_policy", "Nested JWT validation requires signed and encrypted token support.");
        }

        if (RequireExpirationTime && !ValidateExpirationTime)
        {
            return Failure("invalid_policy", "A required expiration claim must also be validated.");
        }

        if (MaximumTokenAgeSeconds > 0 && !ValidateIssuedAt)
        {
            return Failure("invalid_policy", "A maximum token age requires issued-at validation.");
        }

        if (ValidateIssuer && ValidIssuers.Length == 0
            || ValidateAudience && ValidAudiences.Length == 0
            || ValidateTokenType && ValidTokenTypes.Length == 0)
        {
            return Failure("invalid_policy", "An enabled JWT value check requires at least one accepted value.");
        }

        if (!ValidStrings(ValidIssuers)
            || !ValidStrings(ValidAudiences)
            || !ValidStrings(ValidTokenTypes)
            || !ValidStrings(TokenPolicy.RequiredClaims))
        {
            return Failure("invalid_policy", "JWT validation policy names must be nonempty and unique.");
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
        ValidateTokenType = false;
        ValidateHeaderClaimReplication = true;
        AllowSignedTokens = true;
        AllowEncryptedTokens = true;
        AllowNestedTokens = true;
        MaximumTokenAgeSeconds = 0;
        ValidIssuers = Array.Empty<string>();
        ValidAudiences = Array.Empty<string>();
        ValidTokenTypes = Array.Empty<string>();
        SubjectValidator = null;
        ClaimValidator = null;
        ReplayValidator = null;
    }

    internal static TokenPolicy CopyTokenPolicy(TokenPolicy source)
    {
        TokenPolicy result = new TokenPolicy();
        result.MaximumTokenSizeInBytes = source.MaximumTokenSizeInBytes;
        result.MaximumJsonDepth = source.MaximumJsonDepth;
        result.MaximumCborDepth = source.MaximumCborDepth;
        result.ClockSkewSeconds = source.ClockSkewSeconds;
        result.AllowUnsecuredTokens = source.AllowUnsecuredTokens;
        result.AllowLegacyRsaPkcs1Encryption = source.AllowLegacyRsaPkcs1Encryption;
        result.AllowUnauthenticatedDecode = source.AllowUnauthenticatedDecode;
        result.AllowCompression = source.AllowCompression;
        result.MaximumDecompressedSizeInBytes = source.MaximumDecompressedSizeInBytes;
        result.MaximumCompressionRatio = source.MaximumCompressionRatio;
        result.AllowRemoteKeys = source.AllowRemoteKeys;
        result.AllowedRemoteKeyHosts = CopyStrings(source.AllowedRemoteKeyHosts);
        result.MaximumRemoteKeySetSizeInBytes = source.MaximumRemoteKeySetSizeInBytes;
        result.MaximumRemoteKeyCacheEntries = source.MaximumRemoteKeyCacheEntries;
        result.RemoteKeyRefreshSeconds = source.RemoteKeyRefreshSeconds;
        result.RemoteKeyStaleIfErrorSeconds = source.RemoteKeyStaleIfErrorSeconds;
        result.RemoteKeyTimeoutSeconds = source.RemoteKeyTimeoutSeconds;
        result.AllowedAlgorithms = CopyStrings(source.AllowedAlgorithms);
        result.UnderstoodCriticalHeaders = CopyStrings(source.UnderstoodCriticalHeaders);
        result.RequiredClaims = CopyStrings(source.RequiredClaims);
        return result;
    }

    internal static string[]? CopyStrings(string[]? values)
    {
        if (values == null)
        {
            return null;
        }

        return values.AsSpan().ToArray();
    }

    internal static bool ValidStrings(string[] values)
    {
        HashSet<string> found = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < values.Length)
        {
            string value = values[index];
            if (value == null || value.Length == 0 || !found.Add(value))
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
