using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Represents a S/MIME encryption policy.
/// </summary>
public class EmailSmimeEncryptionPolicy : IEmailSmimeEncryptionPolicyContract
{
    bool allowAes128Cbc;
    bool allowAes256Cbc;
    bool allowAes128Gcm;
    bool allowAes256Gcm;
    bool allowRsaOaepSha256;
    bool allowRsaPkcs1Encryption;
    bool allowRsaPkcs1Decryption;
    int minimumRsaKeyBits;
    int maximumRsaKeyBits;
    /// <summary>
    /// Creates a new S/MIME encryption policy.
    /// </summary>
    public EmailSmimeEncryptionPolicy()
    {
        allowAes128Cbc = true;
        allowAes256Cbc = true;
        allowAes128Gcm = true;
        allowAes256Gcm = true;
        allowRsaOaepSha256 = true;
        allowRsaPkcs1Encryption = false;
        allowRsaPkcs1Decryption = true;
        minimumRsaKeyBits = 2048;
        maximumRsaKeyBits = 16384;
    }

    /// <summary>
    /// Gets the allow aes 128 CBC.
    /// </summary>
    /// <returns><c>true</c> when the allow aes 128 CBC setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowAes128Cbc()
    {
        return allowAes128Cbc;
    }

    /// <summary>
    /// Sets the allow aes 128 CBC used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow aes 128 CBC behavior.</param>
    public void SetAllowAes128Cbc(bool value)
    {
        allowAes128Cbc = value;
    }

    /// <summary>
    /// Gets the allow aes 256 CBC.
    /// </summary>
    /// <returns><c>true</c> when the allow aes 256 CBC setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowAes256Cbc()
    {
        return allowAes256Cbc;
    }

    /// <summary>
    /// Sets the allow aes 256 CBC used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow aes 256 CBC behavior.</param>
    public void SetAllowAes256Cbc(bool value)
    {
        allowAes256Cbc = value;
    }

    /// <summary>
    /// Gets the allow aes 128 GCM.
    /// </summary>
    /// <returns><c>true</c> when the allow aes 128 GCM setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowAes128Gcm()
    {
        return allowAes128Gcm;
    }

    /// <summary>
    /// Sets the allow aes 128 GCM used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow aes 128 GCM behavior.</param>
    public void SetAllowAes128Gcm(bool value)
    {
        allowAes128Gcm = value;
    }

    /// <summary>
    /// Gets the allow aes 256 GCM.
    /// </summary>
    /// <returns><c>true</c> when the allow aes 256 GCM setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowAes256Gcm()
    {
        return allowAes256Gcm;
    }

    /// <summary>
    /// Sets the allow aes 256 GCM used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow aes 256 GCM behavior.</param>
    public void SetAllowAes256Gcm(bool value)
    {
        allowAes256Gcm = value;
    }

    /// <summary>
    /// Gets the allow rsa OAEP sha 256.
    /// </summary>
    /// <returns><c>true</c> when the allow rsa OAEP sha 256 setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowRsaOaepSha256()
    {
        return allowRsaOaepSha256;
    }

    /// <summary>
    /// Sets the allow rsa OAEP sha 256 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow rsa OAEP sha 256 behavior.</param>
    public void SetAllowRsaOaepSha256(bool value)
    {
        allowRsaOaepSha256 = value;
    }

    /// <summary>
    /// Gets the allow RSA pkcs 1 encryption.
    /// </summary>
    /// <returns><c>true</c> when the allow RSA pkcs 1 encryption setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowRsaPkcs1Encryption()
    {
        return allowRsaPkcs1Encryption;
    }

    /// <summary>
    /// Sets the allow RSA pkcs 1 encryption used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow RSA pkcs 1 encryption behavior.</param>
    public void SetAllowRsaPkcs1Encryption(bool value)
    {
        allowRsaPkcs1Encryption = value;
    }

    /// <summary>
    /// Gets the allow RSA pkcs 1 decryption.
    /// </summary>
    /// <returns><c>true</c> when the allow RSA pkcs 1 decryption setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowRsaPkcs1Decryption()
    {
        return allowRsaPkcs1Decryption;
    }

    /// <summary>
    /// Sets the allow RSA pkcs 1 decryption used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow RSA pkcs 1 decryption behavior.</param>
    public void SetAllowRsaPkcs1Decryption(bool value)
    {
        allowRsaPkcs1Decryption = value;
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
    /// Determines whether the current value or state is content algorithm allowed.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns><c>true</c> when the value or state is content algorithm allowed; otherwise, <c>false</c>.</returns>
    public bool IsContentAlgorithmAllowed(EmailSmimeContentEncryptionAlgorithm algorithm)
    {
        if (algorithm == EmailSmimeContentEncryptionAlgorithm.Aes128Cbc)
            return allowAes128Cbc;
        if (algorithm == EmailSmimeContentEncryptionAlgorithm.Aes256Cbc)
            return allowAes256Cbc;
        if (algorithm == EmailSmimeContentEncryptionAlgorithm.Aes128Gcm)
            return allowAes128Gcm;
        if (algorithm == EmailSmimeContentEncryptionAlgorithm.Aes256Gcm)
            return allowAes256Gcm;
        return false;
    }

    /// <summary>
    /// Determines whether the current value or state is key transport allowed.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "encryption">Whether to enable the encryption behavior.</param>
    /// <returns><c>true</c> when the value or state is key transport allowed; otherwise, <c>false</c>.</returns>
    public bool IsKeyTransportAllowed(EmailSmimeKeyTransportAlgorithm algorithm, bool encryption)
    {
        if (algorithm == EmailSmimeKeyTransportAlgorithm.RsaOaepSha256)
            return allowRsaOaepSha256;
        if (algorithm == EmailSmimeKeyTransportAlgorithm.RsaPkcs1)
            return encryption ? allowRsaPkcs1Encryption : allowRsaPkcs1Decryption;
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
    public EmailSmimeEncryptionPolicy Clone()
    {
        EmailSmimeEncryptionPolicy copy = new EmailSmimeEncryptionPolicy();
        copy.SetAllowAes128Cbc(allowAes128Cbc);
        copy.SetAllowAes256Cbc(allowAes256Cbc);
        copy.SetAllowAes128Gcm(allowAes128Gcm);
        copy.SetAllowAes256Gcm(allowAes256Gcm);
        copy.SetAllowRsaOaepSha256(allowRsaOaepSha256);
        copy.SetAllowRsaPkcs1Encryption(allowRsaPkcs1Encryption);
        copy.SetAllowRsaPkcs1Decryption(allowRsaPkcs1Decryption);
        copy.SetMinimumRsaKeyBits(minimumRsaKeyBits);
        copy.SetMaximumRsaKeyBits(maximumRsaKeyBits);
        return copy;
    }
}
