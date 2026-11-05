using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeAlgorithmPolicy"/>.
/// </summary>
public interface IEmailSmimeAlgorithmPolicyContract
{
    /// <summary>
    /// Gets the allow sha 1 verification.
    /// </summary>
    /// <returns><c>true</c> when the allow sha 1 verification setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowSha1Verification();
    /// <summary>
    /// Sets the allow sha 1 verification used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow sha 1 verification behavior.</param>
    void SetAllowSha1Verification(bool value);
    /// <summary>
    /// Gets the allow sha 256.
    /// </summary>
    /// <returns><c>true</c> when the allow sha 256 setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowSha256();
    /// <summary>
    /// Sets the allow sha 256 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow sha 256 behavior.</param>
    void SetAllowSha256(bool value);
    /// <summary>
    /// Gets the allow sha 384.
    /// </summary>
    /// <returns><c>true</c> when the allow sha 384 setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowSha384();
    /// <summary>
    /// Sets the allow sha 384 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow sha 384 behavior.</param>
    void SetAllowSha384(bool value);
    /// <summary>
    /// Gets the allow sha 512.
    /// </summary>
    /// <returns><c>true</c> when the allow sha 512 setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowSha512();
    /// <summary>
    /// Sets the allow sha 512 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow sha 512 behavior.</param>
    void SetAllowSha512(bool value);
    /// <summary>
    /// Gets the allow RSA pkcs 1.
    /// </summary>
    /// <returns><c>true</c> when the allow RSA pkcs 1 setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowRsaPkcs1();
    /// <summary>
    /// Sets the allow RSA pkcs 1 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow RSA pkcs 1 behavior.</param>
    void SetAllowRsaPkcs1(bool value);
    /// <summary>
    /// Gets the allow ECDSA.
    /// </summary>
    /// <returns><c>true</c> when the allow ECDSA setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowEcdsa();
    /// <summary>
    /// Sets the allow ECDSA used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow ECDSA behavior.</param>
    void SetAllowEcdsa(bool value);
    /// <summary>
    /// Gets the minimum RSA key bits.
    /// </summary>
    /// <returns>The current minimum RSA key bits.</returns>
    int GetMinimumRsaKeyBits();
    /// <summary>
    /// Sets the minimum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMinimumRsaKeyBits(int value);
    /// <summary>
    /// Gets the maximum RSA key bits.
    /// </summary>
    /// <returns>The current maximum RSA key bits.</returns>
    int GetMaximumRsaKeyBits();
    /// <summary>
    /// Sets the maximum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumRsaKeyBits(int value);
    /// <summary>
    /// Determines whether the current value or state is digest allowed.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "signing">Whether to enable the signing behavior.</param>
    /// <returns><c>true</c> when the value or state is digest allowed; otherwise, <c>false</c>.</returns>
    bool IsDigestAllowed(EmailSmimeDigestAlgorithm algorithm, bool signing);
    /// <summary>
    /// Determines whether the current value or state is RSA key size allowed.
    /// </summary>
    /// <param name = "bits">The bits used by the operation.</param>
    /// <returns><c>true</c> when the value or state is RSA key size allowed; otherwise, <c>false</c>.</returns>
    bool IsRsaKeySizeAllowed(int bits);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailSmimeAlgorithmPolicy Clone();
}
