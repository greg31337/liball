using System;

namespace email;
/// <summary>
/// Groups the parsing, decoding, HTML, archive, temporary-storage, and cryptographic resource limits used for untrusted input.
/// </summary>
public class EmailLimits : IEmailLimitsContract
{
    EmailParsingLimits parsingLimits;
    EmailDecodingLimits decodingLimits;
    EmailHtmlLimits htmlLimits;
    EmailArchiveLimits archiveLimits;
    EmailTemporaryStorageLimits temporaryStorageLimits;
    EmailCryptographicLimits cryptographicLimits;
    /// <summary>
    /// Creates a new limits.
    /// </summary>
    public EmailLimits()
    {
        parsingLimits = new EmailParsingLimits();
        decodingLimits = new EmailDecodingLimits();
        htmlLimits = new EmailHtmlLimits();
        archiveLimits = new EmailArchiveLimits();
        temporaryStorageLimits = new EmailTemporaryStorageLimits();
        cryptographicLimits = new EmailCryptographicLimits();
    }

    /// <summary>
    /// Gets the parsing limits.
    /// </summary>
    /// <returns>The current parsing limits.</returns>
    public EmailParsingLimits GetParsingLimits()
    {
        return parsingLimits;
    }

    /// <summary>
    /// Sets the parsing limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetParsingLimits(EmailParsingLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        parsingLimits = value;
    }

    /// <summary>
    /// Gets the decoding limits.
    /// </summary>
    /// <returns>The current decoding limits.</returns>
    public EmailDecodingLimits GetDecodingLimits()
    {
        return decodingLimits;
    }

    /// <summary>
    /// Sets the decoding limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetDecodingLimits(EmailDecodingLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        decodingLimits = value;
    }

    /// <summary>
    /// Gets the HTML limits.
    /// </summary>
    /// <returns>The current HTML limits.</returns>
    public EmailHtmlLimits GetHtmlLimits()
    {
        return htmlLimits;
    }

    /// <summary>
    /// Sets the HTML limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetHtmlLimits(EmailHtmlLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        htmlLimits = value;
    }

    /// <summary>
    /// Gets the archive limits.
    /// </summary>
    /// <returns>The current archive limits.</returns>
    public EmailArchiveLimits GetArchiveLimits()
    {
        return archiveLimits;
    }

    /// <summary>
    /// Sets the archive limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetArchiveLimits(EmailArchiveLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        archiveLimits = value;
    }

    /// <summary>
    /// Gets the temporary storage limits.
    /// </summary>
    /// <returns>The current temporary storage limits.</returns>
    public EmailTemporaryStorageLimits GetTemporaryStorageLimits()
    {
        return temporaryStorageLimits;
    }

    /// <summary>
    /// Sets the temporary storage limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetTemporaryStorageLimits(EmailTemporaryStorageLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        temporaryStorageLimits = value;
    }

    /// <summary>
    /// Gets the cryptographic limits.
    /// </summary>
    /// <returns>The current cryptographic limits.</returns>
    public EmailCryptographicLimits GetCryptographicLimits()
    {
        return cryptographicLimits;
    }

    /// <summary>
    /// Sets the cryptographic limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetCryptographicLimits(EmailCryptographicLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        cryptographicLimits = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailLimits Clone()
    {
        EmailLimits copy = new EmailLimits();
        copy.SetParsingLimits(parsingLimits.Clone());
        copy.SetDecodingLimits(decodingLimits.Clone());
        copy.SetHtmlLimits(htmlLimits.Clone());
        copy.SetArchiveLimits(archiveLimits.Clone());
        copy.SetTemporaryStorageLimits(temporaryStorageLimits.Clone());
        copy.SetCryptographicLimits(cryptographicLimits.Clone());
        return copy;
    }
}
