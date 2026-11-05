using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSecuritySummary"/>.
/// </summary>
public interface IEmailSecuritySummaryContract
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
    /// Gets the DKIM signature header count.
    /// </summary>
    /// <returns>The current DKIM signature header count.</returns>
    int GetDkimSignatureHeaderCount();
    /// <summary>
    /// Gets the ARC header count.
    /// </summary>
    /// <returns>The current ARC header count.</returns>
    int GetArcHeaderCount();
    /// <summary>
    /// Gets the authentication results header count.
    /// </summary>
    /// <returns>The current authentication results header count.</returns>
    int GetAuthenticationResultsHeaderCount();
    /// <summary>
    /// Gets the received spf header count.
    /// </summary>
    /// <returns>The current received spf header count.</returns>
    int GetReceivedSpfHeaderCount();
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
    /// Determines whether the authentication evidence is present or available.
    /// </summary>
    /// <returns><c>true</c> when the authentication evidence is available; otherwise, <c>false</c>.</returns>
    bool HasAuthenticationEvidence();
    /// <summary>
    /// Determines whether the security evidence is present or available.
    /// </summary>
    /// <returns><c>true</c> when the security evidence is available; otherwise, <c>false</c>.</returns>
    bool HasSecurityEvidence();
    /// <summary>
    /// Determines whether the available evidence claims verification.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ClaimsVerification();
}
