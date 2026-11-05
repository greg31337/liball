namespace sdp;

/// <summary>
/// Defines SRTP crypto-suite tokens standardized for SDP security descriptions.
/// </summary>
public static class SdpCryptoSuites
{
    /// <summary>The 128-bit AES counter mode suite with an 80-bit HMAC-SHA1 tag.</summary>
    public const string AesCounterMode128HmacSha1_80 = "AES_CM_128_HMAC_SHA1_80";

    /// <summary>The 128-bit AES counter mode suite with a 32-bit HMAC-SHA1 tag.</summary>
    public const string AesCounterMode128HmacSha1_32 = "AES_CM_128_HMAC_SHA1_32";

    /// <summary>The 128-bit AES F8 mode suite with an 80-bit HMAC-SHA1 tag.</summary>
    public const string AesF8Mode128HmacSha1_80 = "F8_128_HMAC_SHA1_80";
}
