using System;

namespace email;
/// <summary>
/// Represents a display security summary.
/// </summary>
public class EmailDisplaySecuritySummary : IEmailDisplaySecuritySummaryContract
{
    int signedContentCount;
    int encryptedContentCount;
    int signaturePartCount;
    int encryptionPartCount;
    int reportPartCount;
    /// <summary>
    /// Creates a new display security summary.
    /// </summary>
    /// <param name = "signedContentCount">The number of items or bytes to process.</param>
    /// <param name = "encryptedContentCount">The number of items or bytes to process.</param>
    /// <param name = "signaturePartCount">The number of items or bytes to process.</param>
    /// <param name = "encryptionPartCount">The number of items or bytes to process.</param>
    /// <param name = "reportPartCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDisplaySecuritySummary(int signedContentCount, int encryptedContentCount, int signaturePartCount, int encryptionPartCount, int reportPartCount)
    {
        if (signedContentCount < 0)
            throw new ArgumentOutOfRangeException("signedContentCount");
        if (encryptedContentCount < 0)
            throw new ArgumentOutOfRangeException("encryptedContentCount");
        if (signaturePartCount < 0)
            throw new ArgumentOutOfRangeException("signaturePartCount");
        if (encryptionPartCount < 0)
            throw new ArgumentOutOfRangeException("encryptionPartCount");
        if (reportPartCount < 0)
            throw new ArgumentOutOfRangeException("reportPartCount");
        this.signedContentCount = signedContentCount;
        this.encryptedContentCount = encryptedContentCount;
        this.signaturePartCount = signaturePartCount;
        this.encryptionPartCount = encryptionPartCount;
        this.reportPartCount = reportPartCount;
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
    /// Determines whether the available evidence claims verification.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ClaimsVerification()
    {
        return false;
    }
}
