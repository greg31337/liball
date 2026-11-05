using System;

namespace email;
/// <summary>
/// Represents a S/MIME algorithm policy.
/// </summary>
public class EmailSmimeAlgorithmPolicy : IEmailSmimeAlgorithmPolicyContract
{
    bool allowSha1Verification;
    bool allowSha256;
    bool allowSha384;
    bool allowSha512;
    bool allowRsaPkcs1;
    bool allowEcdsa;
    int minimumRsaKeyBits;
    int maximumRsaKeyBits;
    /// <summary>
    /// Creates a new S/MIME algorithm policy.
    /// </summary>
    public EmailSmimeAlgorithmPolicy()
    {
        allowSha1Verification = false;
        allowSha256 = true;
        allowSha384 = true;
        allowSha512 = true;
        allowRsaPkcs1 = true;
        allowEcdsa = true;
        minimumRsaKeyBits = 2048;
        maximumRsaKeyBits = 16384;
    }

    /// <summary>
    /// Gets the allow sha 1 verification.
    /// </summary>
    /// <returns><c>true</c> when the allow sha 1 verification setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowSha1Verification()
    {
        return allowSha1Verification;
    }

    /// <summary>
    /// Sets the allow sha 1 verification used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow sha 1 verification behavior.</param>
    public void SetAllowSha1Verification(bool value)
    {
        allowSha1Verification = value;
    }

    /// <summary>
    /// Gets the allow sha 256.
    /// </summary>
    /// <returns><c>true</c> when the allow sha 256 setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowSha256()
    {
        return allowSha256;
    }

    /// <summary>
    /// Sets the allow sha 256 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow sha 256 behavior.</param>
    public void SetAllowSha256(bool value)
    {
        allowSha256 = value;
    }

    /// <summary>
    /// Gets the allow sha 384.
    /// </summary>
    /// <returns><c>true</c> when the allow sha 384 setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowSha384()
    {
        return allowSha384;
    }

    /// <summary>
    /// Sets the allow sha 384 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow sha 384 behavior.</param>
    public void SetAllowSha384(bool value)
    {
        allowSha384 = value;
    }

    /// <summary>
    /// Gets the allow sha 512.
    /// </summary>
    /// <returns><c>true</c> when the allow sha 512 setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowSha512()
    {
        return allowSha512;
    }

    /// <summary>
    /// Sets the allow sha 512 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow sha 512 behavior.</param>
    public void SetAllowSha512(bool value)
    {
        allowSha512 = value;
    }

    /// <summary>
    /// Gets the allow RSA pkcs 1.
    /// </summary>
    /// <returns><c>true</c> when the allow RSA pkcs 1 setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowRsaPkcs1()
    {
        return allowRsaPkcs1;
    }

    /// <summary>
    /// Sets the allow RSA pkcs 1 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow RSA pkcs 1 behavior.</param>
    public void SetAllowRsaPkcs1(bool value)
    {
        allowRsaPkcs1 = value;
    }

    /// <summary>
    /// Gets the allow ECDSA.
    /// </summary>
    /// <returns><c>true</c> when the allow ECDSA setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowEcdsa()
    {
        return allowEcdsa;
    }

    /// <summary>
    /// Sets the allow ECDSA used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow ECDSA behavior.</param>
    public void SetAllowEcdsa(bool value)
    {
        allowEcdsa = value;
    }

    /// <summary>
    /// Gets the minimum RSA key bits.
    /// </summary>
    /// <returns>The current minimum RSA key bits.</returns>
    public int GetMinimumRsaKeyBits()
    {
        return minimumRsaKeyBits;
    }

    /// <summary>
    /// Sets the minimum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMinimumRsaKeyBits(int value)
    {
        minimumRsaKeyBits = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum RSA key bits.
    /// </summary>
    /// <returns>The current maximum RSA key bits.</returns>
    public int GetMaximumRsaKeyBits()
    {
        return maximumRsaKeyBits;
    }

    /// <summary>
    /// Sets the maximum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumRsaKeyBits(int value)
    {
        maximumRsaKeyBits = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Determines whether the current value or state is digest allowed.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "signing">Whether to enable the signing behavior.</param>
    /// <returns><c>true</c> when the value or state is digest allowed; otherwise, <c>false</c>.</returns>
    public bool IsDigestAllowed(EmailSmimeDigestAlgorithm algorithm, bool signing)
    {
        if (algorithm == EmailSmimeDigestAlgorithm.Sha1)
            return !signing && allowSha1Verification;
        if (algorithm == EmailSmimeDigestAlgorithm.Sha256)
            return allowSha256;
        if (algorithm == EmailSmimeDigestAlgorithm.Sha384)
            return allowSha384;
        if (algorithm == EmailSmimeDigestAlgorithm.Sha512)
            return allowSha512;
        return false;
    }

    /// <summary>
    /// Determines whether the current value or state is RSA key size allowed.
    /// </summary>
    /// <param name = "bits">The bits used by the operation.</param>
    /// <returns><c>true</c> when the value or state is RSA key size allowed; otherwise, <c>false</c>.</returns>
    public bool IsRsaKeySizeAllowed(int bits)
    {
        return bits >= minimumRsaKeyBits && bits <= maximumRsaKeyBits;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailSmimeAlgorithmPolicy Clone()
    {
        EmailSmimeAlgorithmPolicy copy = new EmailSmimeAlgorithmPolicy();
        copy.SetAllowSha1Verification(allowSha1Verification);
        copy.SetAllowSha256(allowSha256);
        copy.SetAllowSha384(allowSha384);
        copy.SetAllowSha512(allowSha512);
        copy.SetAllowRsaPkcs1(allowRsaPkcs1);
        copy.SetAllowEcdsa(allowEcdsa);
        copy.SetMinimumRsaKeyBits(minimumRsaKeyBits);
        copy.SetMaximumRsaKeyBits(maximumRsaKeyBits);
        return copy;
    }
}
