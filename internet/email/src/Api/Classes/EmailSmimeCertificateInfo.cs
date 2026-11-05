using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Represents a S/MIME certificate info.
/// </summary>
public class EmailSmimeCertificateInfo : IEmailSmimeCertificateInfoContract
{
    byte[] rawData;
    string subject;
    string issuer;
    string serialNumber;
    string thumbprint;
    DateTime notBefore;
    DateTime notAfter;
    /// <summary>
    /// Creates a new S/MIME certificate info.
    /// </summary>
    /// <param name = "certificate">The certificate used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeCertificateInfo(X509Certificate2 certificate)
    {
        if (certificate == null)
            throw new ArgumentNullException("certificate");
        rawData = Copy(certificate.RawData);
        subject = certificate.Subject;
        issuer = certificate.Issuer;
        serialNumber = certificate.SerialNumber;
        thumbprint = certificate.Thumbprint;
        notBefore = certificate.NotBefore;
        notAfter = certificate.NotAfter;
    }

    /// <summary>
    /// Gets the raw data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw data.</returns>
    public byte[] GetRawData()
    {
        return Copy(rawData);
    }

    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <returns>The current subject.</returns>
    public string GetSubject()
    {
        return subject;
    }

    /// <summary>
    /// Gets the issuer.
    /// </summary>
    /// <returns>The current issuer.</returns>
    public string GetIssuer()
    {
        return issuer;
    }

    /// <summary>
    /// Gets the serial number.
    /// </summary>
    /// <returns>The current serial number.</returns>
    public string GetSerialNumber()
    {
        return serialNumber;
    }

    /// <summary>
    /// Gets the thumbprint.
    /// </summary>
    /// <returns>The current thumbprint.</returns>
    public string GetThumbprint()
    {
        return thumbprint;
    }

    /// <summary>
    /// Gets the not before.
    /// </summary>
    /// <returns>The current not before.</returns>
    public DateTime GetNotBefore()
    {
        return notBefore;
    }

    /// <summary>
    /// Gets the not after.
    /// </summary>
    /// <returns>The current not after.</returns>
    public DateTime GetNotAfter()
    {
        return notAfter;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
