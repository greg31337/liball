using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeEncryptionPolicy"/>.
/// </summary>
public interface IEmailSmimeEncryptionPolicyContract
{
    /// <summary>
    /// Gets the allow aes 128 CBC.
    /// </summary>
    /// <returns><c>true</c> when the allow aes 128 CBC setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowAes128Cbc();
    /// <summary>
    /// Sets the allow aes 128 CBC used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow aes 128 CBC behavior.</param>
    void SetAllowAes128Cbc(bool value);
    /// <summary>
    /// Gets the allow aes 256 CBC.
    /// </summary>
    /// <returns><c>true</c> when the allow aes 256 CBC setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowAes256Cbc();
    /// <summary>
    /// Sets the allow aes 256 CBC used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow aes 256 CBC behavior.</param>
    void SetAllowAes256Cbc(bool value);
    /// <summary>
    /// Gets the allow aes 128 GCM.
    /// </summary>
    /// <returns><c>true</c> when the allow aes 128 GCM setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowAes128Gcm();
    /// <summary>
    /// Sets the allow aes 128 GCM used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow aes 128 GCM behavior.</param>
    void SetAllowAes128Gcm(bool value);
    /// <summary>
    /// Gets the allow aes 256 GCM.
    /// </summary>
    /// <returns><c>true</c> when the allow aes 256 GCM setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowAes256Gcm();
    /// <summary>
    /// Sets the allow aes 256 GCM used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow aes 256 GCM behavior.</param>
    void SetAllowAes256Gcm(bool value);
    /// <summary>
    /// Gets the allow rsa OAEP sha 256.
    /// </summary>
    /// <returns><c>true</c> when the allow rsa OAEP sha 256 setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowRsaOaepSha256();
    /// <summary>
    /// Sets the allow rsa OAEP sha 256 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow rsa OAEP sha 256 behavior.</param>
    void SetAllowRsaOaepSha256(bool value);
    /// <summary>
    /// Gets the allow RSA pkcs 1 encryption.
    /// </summary>
    /// <returns><c>true</c> when the allow RSA pkcs 1 encryption setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowRsaPkcs1Encryption();
    /// <summary>
    /// Sets the allow RSA pkcs 1 encryption used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow RSA pkcs 1 encryption behavior.</param>
    void SetAllowRsaPkcs1Encryption(bool value);
    /// <summary>
    /// Gets the allow RSA pkcs 1 decryption.
    /// </summary>
    /// <returns><c>true</c> when the allow RSA pkcs 1 decryption setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowRsaPkcs1Decryption();
    /// <summary>
    /// Sets the allow RSA pkcs 1 decryption used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow RSA pkcs 1 decryption behavior.</param>
    void SetAllowRsaPkcs1Decryption(bool value);
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
    /// Determines whether the current value or state is content algorithm allowed.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns><c>true</c> when the value or state is content algorithm allowed; otherwise, <c>false</c>.</returns>
    bool IsContentAlgorithmAllowed(EmailSmimeContentEncryptionAlgorithm algorithm);
    /// <summary>
    /// Determines whether the current value or state is key transport allowed.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "encryption">Whether to enable the encryption behavior.</param>
    /// <returns><c>true</c> when the value or state is key transport allowed; otherwise, <c>false</c>.</returns>
    bool IsKeyTransportAllowed(EmailSmimeKeyTransportAlgorithm algorithm, bool encryption);
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
    EmailSmimeEncryptionPolicy Clone();
}
