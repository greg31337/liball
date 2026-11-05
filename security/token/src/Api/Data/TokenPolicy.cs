using System;
namespace token;

/// <summary>
/// Defines common safety limits and opt-in behavior for token operations.
/// </summary>
public class TokenPolicy
{
    /// <summary>
    /// The maximum accepted encoded token size in bytes.
    /// </summary>
    public int MaximumTokenSizeInBytes;

    /// <summary>
    /// The maximum accepted JSON nesting depth.
    /// </summary>
    public int MaximumJsonDepth;

    /// <summary>
    /// The maximum accepted CBOR nesting depth.
    /// </summary>
    public int MaximumCborDepth;

    /// <summary>
    /// The allowed clock skew in seconds for time claim validation.
    /// </summary>
    public int ClockSkewSeconds;

    /// <summary>
    /// Indicates whether unsecured tokens may be created or validated.
    /// </summary>
    public bool AllowUnsecuredTokens;

    /// <summary>
    /// Indicates whether legacy RSA PKCS#1 v1.5 encryption may be used.
    /// </summary>
    public bool AllowLegacyRsaPkcs1Encryption;

    /// <summary>
    /// Indicates whether unverified or unauthenticated content may be decoded.
    /// </summary>
    public bool AllowUnauthenticatedDecode;

    /// <summary>
    /// Indicates whether JWE compression and decompression may be used.
    /// </summary>
    public bool AllowCompression;

    /// <summary>
    /// The maximum accepted decompressed JWE plaintext size in bytes.
    /// </summary>
    public int MaximumDecompressedSizeInBytes;

    /// <summary>
    /// The maximum decompressed-to-compressed size ratio.
    /// </summary>
    public int MaximumCompressionRatio;

    /// <summary>
    /// Indicates whether remote key resolution may be used.
    /// </summary>
    public bool AllowRemoteKeys;

    /// <summary>
    /// The exact DNS hosts allowed for remote key resolution.
    /// </summary>
    public string[]? AllowedRemoteKeyHosts;

    /// <summary>
    /// The maximum accepted remote JWK Set response size in bytes.
    /// </summary>
    public int MaximumRemoteKeySetSizeInBytes;

    /// <summary>
    /// The maximum remote JWK Set responses retained by one resolver, or zero to disable caching.
    /// </summary>
    public int MaximumRemoteKeyCacheEntries;

    /// <summary>
    /// The number of seconds a cached remote JWK Set remains fresh.
    /// </summary>
    public int RemoteKeyRefreshSeconds;

    /// <summary>
    /// The additional seconds a stale cached JWK Set may be used after a refresh error.
    /// </summary>
    public int RemoteKeyStaleIfErrorSeconds;

    /// <summary>
    /// The remote JWK Set request timeout in seconds.
    /// </summary>
    public int RemoteKeyTimeoutSeconds;

    /// <summary>
    /// The algorithms explicitly allowed by the caller.
    /// </summary>
    public string[]? AllowedAlgorithms;

    /// <summary>
    /// The critical JOSE extension parameters the caller understands and processes.
    /// </summary>
    public string[]? UnderstoodCriticalHeaders;

    /// <summary>
    /// The claims required by the caller.
    /// </summary>
    public string[]? RequiredClaims;

    /// <summary>
    /// Initializes a policy with conservative limits and optional behavior disabled.
    /// </summary>
    public TokenPolicy()
    {
        MaximumTokenSizeInBytes = 1048576;
        MaximumJsonDepth = 64;
        MaximumCborDepth = 64;
        ClockSkewSeconds = 300;
        AllowUnsecuredTokens = false;
        AllowLegacyRsaPkcs1Encryption = false;
        AllowUnauthenticatedDecode = false;
        AllowCompression = false;
        MaximumDecompressedSizeInBytes = 1048576;
        MaximumCompressionRatio = 100;
        AllowRemoteKeys = false;
        AllowedRemoteKeyHosts = System.Array.Empty<string>();
        MaximumRemoteKeySetSizeInBytes = 1048576;
        MaximumRemoteKeyCacheEntries = 16;
        RemoteKeyRefreshSeconds = 300;
        RemoteKeyStaleIfErrorSeconds = 300;
        RemoteKeyTimeoutSeconds = 15;
        AllowedAlgorithms = System.Array.Empty<string>();
        UnderstoodCriticalHeaders = System.Array.Empty<string>();
        RequiredClaims = System.Array.Empty<string>();
    }
}
