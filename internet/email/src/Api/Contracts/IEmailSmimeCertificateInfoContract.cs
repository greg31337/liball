using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeCertificateInfo"/>.
/// </summary>
public interface IEmailSmimeCertificateInfoContract
{
    /// <summary>
    /// Gets the raw data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw data.</returns>
    byte[] GetRawData();
    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <returns>The current subject.</returns>
    string GetSubject();
    /// <summary>
    /// Gets the issuer.
    /// </summary>
    /// <returns>The current issuer.</returns>
    string GetIssuer();
    /// <summary>
    /// Gets the serial number.
    /// </summary>
    /// <returns>The current serial number.</returns>
    string GetSerialNumber();
    /// <summary>
    /// Gets the thumbprint.
    /// </summary>
    /// <returns>The current thumbprint.</returns>
    string GetThumbprint();
    /// <summary>
    /// Gets the not before.
    /// </summary>
    /// <returns>The current not before.</returns>
    DateTime GetNotBefore();
    /// <summary>
    /// Gets the not after.
    /// </summary>
    /// <returns>The current not after.</returns>
    DateTime GetNotAfter();
}
