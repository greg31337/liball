using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Controls how S/MIME verification operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailSmimeVerificationOptions : IEmailSmimeVerificationOptionsContract
{
    EmailCryptographicLimits limits;
    EmailSmimeAlgorithmPolicy algorithmPolicy;
    bool verifyCertificateChain;
    X509RevocationMode revocationMode;
    bool hasVerificationTime;
    DateTime verificationTime;
    List<X509Certificate2> customTrustRoots;
    List<X509Certificate2> extraCertificates;
    /// <summary>
    /// Creates a new S/MIME verification options object.
    /// </summary>
    public EmailSmimeVerificationOptions()
    {
        limits = new EmailCryptographicLimits();
        algorithmPolicy = new EmailSmimeAlgorithmPolicy();
        verifyCertificateChain = true;
        revocationMode = X509RevocationMode.NoCheck;
        hasVerificationTime = false;
        verificationTime = DateTime.UtcNow;
        customTrustRoots = new List<X509Certificate2>();
        extraCertificates = new List<X509Certificate2>();
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailCryptographicLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailCryptographicLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the algorithm policy.
    /// </summary>
    /// <returns>The current algorithm policy.</returns>
    public EmailSmimeAlgorithmPolicy GetAlgorithmPolicy()
    {
        return algorithmPolicy.Clone();
    }

    /// <summary>
    /// Sets the algorithm policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetAlgorithmPolicy(EmailSmimeAlgorithmPolicy value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        algorithmPolicy = value.Clone();
    }

    /// <summary>
    /// Gets the verify certificate chain.
    /// </summary>
    /// <returns><c>true</c> when the verify certificate chain setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetVerifyCertificateChain()
    {
        return verifyCertificateChain;
    }

    /// <summary>
    /// Sets the verify certificate chain used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the verify certificate chain behavior.</param>
    public void SetVerifyCertificateChain(bool value)
    {
        verifyCertificateChain = value;
    }

    /// <summary>
    /// Gets the revocation mode.
    /// </summary>
    /// <returns>The current revocation mode.</returns>
    public X509RevocationMode GetRevocationMode()
    {
        return revocationMode;
    }

    /// <summary>
    /// Sets the revocation mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetRevocationMode(X509RevocationMode value)
    {
        if (value < X509RevocationMode.NoCheck || value > X509RevocationMode.Offline)
            throw new ArgumentOutOfRangeException("value");
        revocationMode = value;
    }

    /// <summary>
    /// Determines whether the verification time is present or available.
    /// </summary>
    /// <returns><c>true</c> when the verification time is available; otherwise, <c>false</c>.</returns>
    public bool HasVerificationTime()
    {
        return hasVerificationTime;
    }

    /// <summary>
    /// Gets the verification time.
    /// </summary>
    /// <returns>The current verification time.</returns>
    public DateTime GetVerificationTime()
    {
        return verificationTime;
    }

    /// <summary>
    /// Sets the verification time used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetVerificationTime(DateTime value)
    {
        verificationTime = value.ToUniversalTime();
        hasVerificationTime = true;
    }

    /// <summary>
    /// Clears the stored verification time value or values.
    /// </summary>
    public void ClearVerificationTime()
    {
        hasVerificationTime = false;
    }

    /// <summary>
    /// Adds a custom trust root to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddCustomTrustRoot(X509Certificate2 value)
    {
        AddCertificate(customTrustRoots, value, "value");
    }

    /// <summary>
    /// Adds an extra certificate to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddExtraCertificate(X509Certificate2 value)
    {
        AddCertificate(extraCertificates, value, "value");
    }

    /// <summary>
    /// Gets the custom trust root count.
    /// </summary>
    /// <returns>The current custom trust root count.</returns>
    public int GetCustomTrustRootCount()
    {
        return customTrustRoots.Count;
    }

    /// <summary>
    /// Gets the extra certificate count.
    /// </summary>
    /// <returns>The current extra certificate count.</returns>
    public int GetExtraCertificateCount()
    {
        return extraCertificates.Count;
    }

    /// <summary>
    /// Gets the custom trust root at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested custom trust root.</returns>
    public X509Certificate2 GetCustomTrustRootAt(int index)
    {
        return CopyCertificate(GetAt(customTrustRoots, index));
    }

    /// <summary>
    /// Gets the extra certificate at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested extra certificate.</returns>
    public X509Certificate2 GetExtraCertificateAt(int index)
    {
        return CopyCertificate(GetAt(extraCertificates, index));
    }

    /// <summary>
    /// Copies the custom trust roots to a separate destination.
    /// </summary>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<X509Certificate2> CopyCustomTrustRoots()
    {
        return CopyCertificates(customTrustRoots);
    }

    /// <summary>
    /// Copies the extra certificates to a separate destination.
    /// </summary>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<X509Certificate2> CopyExtraCertificates()
    {
        return CopyCertificates(extraCertificates);
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailSmimeVerificationOptions Clone()
    {
        EmailSmimeVerificationOptions copy = new EmailSmimeVerificationOptions();
        copy.SetLimits(limits);
        copy.SetAlgorithmPolicy(algorithmPolicy);
        copy.SetVerifyCertificateChain(verifyCertificateChain);
        copy.SetRevocationMode(revocationMode);
        if (hasVerificationTime)
            copy.SetVerificationTime(verificationTime);
        List<X509Certificate2> roots = customTrustRoots;
        int index = 0;
        while (index < roots.Count)
        {
            copy.AddCustomTrustRoot(roots[index]);
            index++;
        }

        index = 0;
        while (index < extraCertificates.Count)
        {
            copy.AddExtraCertificate(extraCertificates[index]);
            index++;
        }

        return copy;
    }

    static void AddCertificate(List<X509Certificate2> destination, X509Certificate2 value, string parameter)
    {
        if (value == null)
            throw new ArgumentNullException(parameter);
        destination.Add(CopyCertificate(value));
    }

    static X509Certificate2 GetAt(List<X509Certificate2> values, int index)
    {
        if (index < 0 || index >= values.Count)
            throw new ArgumentOutOfRangeException("index");
        return values[index];
    }

    static List<X509Certificate2> CopyCertificates(List<X509Certificate2> values)
    {
        List<X509Certificate2> copy = new List<X509Certificate2>();
        int index = 0;
        while (index < values.Count)
        {
            copy.Add(CopyCertificate(values[index]));
            index++;
        }

        return copy;
    }

    static X509Certificate2 CopyCertificate(X509Certificate2 value)
    {
        return X509CertificateLoader.LoadCertificate(value.RawData);
    }
}
