using System;

namespace email;
/// <summary>
/// Represents a security summary.
/// </summary>
public class EmailSecuritySummary : IEmailSecuritySummaryContract
{
    int signedContentCount;
    int encryptedContentCount;
    int signaturePartCount;
    int encryptionPartCount;
    int reportPartCount;
    int dkimSignatureHeaderCount;
    int arcHeaderCount;
    int authenticationResultsHeaderCount;
    int receivedSpfHeaderCount;
    /// <summary>
    /// Creates a new security summary.
    /// </summary>
    /// <param name = "signedContentCount">The number of items or bytes to process.</param>
    /// <param name = "encryptedContentCount">The number of items or bytes to process.</param>
    /// <param name = "signaturePartCount">The number of items or bytes to process.</param>
    /// <param name = "encryptionPartCount">The number of items or bytes to process.</param>
    /// <param name = "reportPartCount">The number of items or bytes to process.</param>
    /// <param name = "dkimSignatureHeaderCount">The number of items or bytes to process.</param>
    /// <param name = "arcHeaderCount">The number of items or bytes to process.</param>
    /// <param name = "authenticationResultsHeaderCount">The number of items or bytes to process.</param>
    /// <param name = "receivedSpfHeaderCount">The number of items or bytes to process.</param>
    public EmailSecuritySummary(int signedContentCount, int encryptedContentCount, int signaturePartCount, int encryptionPartCount, int reportPartCount, int dkimSignatureHeaderCount, int arcHeaderCount, int authenticationResultsHeaderCount, int receivedSpfHeaderCount)
    {
        ValidateCount(signedContentCount, "signedContentCount");
        ValidateCount(encryptedContentCount, "encryptedContentCount");
        ValidateCount(signaturePartCount, "signaturePartCount");
        ValidateCount(encryptionPartCount, "encryptionPartCount");
        ValidateCount(reportPartCount, "reportPartCount");
        ValidateCount(dkimSignatureHeaderCount, "dkimSignatureHeaderCount");
        ValidateCount(arcHeaderCount, "arcHeaderCount");
        ValidateCount(authenticationResultsHeaderCount, "authenticationResultsHeaderCount");
        ValidateCount(receivedSpfHeaderCount, "receivedSpfHeaderCount");
        this.signedContentCount = signedContentCount;
        this.encryptedContentCount = encryptedContentCount;
        this.signaturePartCount = signaturePartCount;
        this.encryptionPartCount = encryptionPartCount;
        this.reportPartCount = reportPartCount;
        this.dkimSignatureHeaderCount = dkimSignatureHeaderCount;
        this.arcHeaderCount = arcHeaderCount;
        this.authenticationResultsHeaderCount = authenticationResultsHeaderCount;
        this.receivedSpfHeaderCount = receivedSpfHeaderCount;
    }

    /// <summary>
    /// Gets the signed content count.
    /// </summary>
    /// <returns>The current signed content count.</returns>
    public int GetSignedContentCount()
    {
        return signedContentCount;
    }

    /// <summary>
    /// Gets the encrypted content count.
    /// </summary>
    /// <returns>The current encrypted content count.</returns>
    public int GetEncryptedContentCount()
    {
        return encryptedContentCount;
    }

    /// <summary>
    /// Gets the signature part count.
    /// </summary>
    /// <returns>The current signature part count.</returns>
    public int GetSignaturePartCount()
    {
        return signaturePartCount;
    }

    /// <summary>
    /// Gets the encryption part count.
    /// </summary>
    /// <returns>The current encryption part count.</returns>
    public int GetEncryptionPartCount()
    {
        return encryptionPartCount;
    }

    /// <summary>
    /// Gets the report part count.
    /// </summary>
    /// <returns>The current report part count.</returns>
    public int GetReportPartCount()
    {
        return reportPartCount;
    }

    /// <summary>
    /// Gets the DKIM signature header count.
    /// </summary>
    /// <returns>The current DKIM signature header count.</returns>
    public int GetDkimSignatureHeaderCount()
    {
        return dkimSignatureHeaderCount;
    }

    /// <summary>
    /// Gets the ARC header count.
    /// </summary>
    /// <returns>The current ARC header count.</returns>
    public int GetArcHeaderCount()
    {
        return arcHeaderCount;
    }

    /// <summary>
    /// Gets the authentication results header count.
    /// </summary>
    /// <returns>The current authentication results header count.</returns>
    public int GetAuthenticationResultsHeaderCount()
    {
        return authenticationResultsHeaderCount;
    }

    /// <summary>
    /// Gets the received spf header count.
    /// </summary>
    /// <returns>The current received spf header count.</returns>
    public int GetReceivedSpfHeaderCount()
    {
        return receivedSpfHeaderCount;
    }

    /// <summary>
    /// Determines whether the signed content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the signed content is available; otherwise, <c>false</c>.</returns>
    public bool HasSignedContent()
    {
        return signedContentCount > 0 || signaturePartCount > 0;
    }

    /// <summary>
    /// Determines whether the encrypted content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the encrypted content is available; otherwise, <c>false</c>.</returns>
    public bool HasEncryptedContent()
    {
        return encryptedContentCount > 0 || encryptionPartCount > 0;
    }

    /// <summary>
    /// Determines whether the report content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the report content is available; otherwise, <c>false</c>.</returns>
    public bool HasReportContent()
    {
        return reportPartCount > 0;
    }

    /// <summary>
    /// Determines whether the authentication evidence is present or available.
    /// </summary>
    /// <returns><c>true</c> when the authentication evidence is available; otherwise, <c>false</c>.</returns>
    public bool HasAuthenticationEvidence()
    {
        return dkimSignatureHeaderCount > 0 || arcHeaderCount > 0 || authenticationResultsHeaderCount > 0 || receivedSpfHeaderCount > 0;
    }

    /// <summary>
    /// Determines whether the security evidence is present or available.
    /// </summary>
    /// <returns><c>true</c> when the security evidence is available; otherwise, <c>false</c>.</returns>
    public bool HasSecurityEvidence()
    {
        return HasSignedContent() || HasEncryptedContent() || HasReportContent() || HasAuthenticationEvidence();
    }

    /// <summary>
    /// Determines whether the available evidence claims verification.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ClaimsVerification()
    {
        return false;
    }

    static void ValidateCount(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
