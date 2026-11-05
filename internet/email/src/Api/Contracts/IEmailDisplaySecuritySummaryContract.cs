using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDisplaySecuritySummary"/>.
/// </summary>
public interface IEmailDisplaySecuritySummaryContract
{
    /// <summary>
    /// Gets the signed content count.
    /// </summary>
    /// <returns>The current signed content count.</returns>
    int GetSignedContentCount();
    /// <summary>
    /// Gets the encrypted content count.
    /// </summary>
    /// <returns>The current encrypted content count.</returns>
    int GetEncryptedContentCount();
    /// <summary>
    /// Gets the signature part count.
    /// </summary>
    /// <returns>The current signature part count.</returns>
    int GetSignaturePartCount();
    /// <summary>
    /// Gets the encryption part count.
    /// </summary>
    /// <returns>The current encryption part count.</returns>
    int GetEncryptionPartCount();
    /// <summary>
    /// Gets the report part count.
    /// </summary>
    /// <returns>The current report part count.</returns>
    int GetReportPartCount();
    /// <summary>
    /// Determines whether the signed content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the signed content is available; otherwise, <c>false</c>.</returns>
    bool HasSignedContent();
    /// <summary>
    /// Determines whether the encrypted content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the encrypted content is available; otherwise, <c>false</c>.</returns>
    bool HasEncryptedContent();
    /// <summary>
    /// Determines whether the report content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the report content is available; otherwise, <c>false</c>.</returns>
    bool HasReportContent();
    /// <summary>
    /// Determines whether the available evidence claims verification.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ClaimsVerification();
}
