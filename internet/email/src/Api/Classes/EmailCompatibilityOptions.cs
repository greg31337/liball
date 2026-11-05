namespace email;
/// <summary>
/// Controls how compatibility operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailCompatibilityOptions : IEmailCompatibilityOptionsContract
{
    bool allowBareLineEndings;
    bool allowMissingHeaderBodySeparator;
    bool allowMalformedHeaderNames;
    bool allowOrphanHeaderContinuations;
    bool allowBrokenMimeBoundaries;
    bool allowOutlookDeviations;
    bool allowExchangeDeviations;
    bool allowGmailDeviations;
    bool allowCommonMimeTypeAliases;
    bool allowBrokenEncodedWords;
    bool allowBrokenRfc2231Parameters;
    bool allowLegacyAttachmentRecovery;
    bool allowTruncatedInput;
    bool allowDuplicateSingletonHeaders;
    bool returnPartialResults;
    /// <summary>
    /// Creates a new compatibility options object.
    /// </summary>
    public EmailCompatibilityOptions()
    {
        allowBareLineEndings = true;
        allowMissingHeaderBodySeparator = true;
        allowMalformedHeaderNames = true;
        allowOrphanHeaderContinuations = true;
        allowBrokenMimeBoundaries = false;
        allowOutlookDeviations = true;
        allowExchangeDeviations = true;
        allowGmailDeviations = true;
        allowCommonMimeTypeAliases = true;
        allowBrokenEncodedWords = true;
        allowBrokenRfc2231Parameters = true;
        allowLegacyAttachmentRecovery = true;
        allowTruncatedInput = true;
        allowDuplicateSingletonHeaders = true;
        returnPartialResults = true;
    }

    /// <summary>
    /// Gets the allow bare line endings.
    /// </summary>
    /// <returns><c>true</c> when the allow bare line endings setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowBareLineEndings()
    {
        return allowBareLineEndings;
    }

    /// <summary>
    /// Sets the allow bare line endings used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow bare line endings behavior.</param>
    public void SetAllowBareLineEndings(bool value)
    {
        allowBareLineEndings = value;
    }

    /// <summary>
    /// Gets the allow missing header body separator.
    /// </summary>
    /// <returns><c>true</c> when the allow missing header body separator setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowMissingHeaderBodySeparator()
    {
        return allowMissingHeaderBodySeparator;
    }

    /// <summary>
    /// Sets the allow missing header body separator used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow missing header body separator behavior.</param>
    public void SetAllowMissingHeaderBodySeparator(bool value)
    {
        allowMissingHeaderBodySeparator = value;
    }

    /// <summary>
    /// Gets the allow malformed header names.
    /// </summary>
    /// <returns><c>true</c> when the allow malformed header names setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowMalformedHeaderNames()
    {
        return allowMalformedHeaderNames;
    }

    /// <summary>
    /// Sets the allow malformed header names used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow malformed header names behavior.</param>
    public void SetAllowMalformedHeaderNames(bool value)
    {
        allowMalformedHeaderNames = value;
    }

    /// <summary>
    /// Gets the allow orphan header continuations.
    /// </summary>
    /// <returns><c>true</c> when the allow orphan header continuations setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowOrphanHeaderContinuations()
    {
        return allowOrphanHeaderContinuations;
    }

    /// <summary>
    /// Sets the allow orphan header continuations used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow orphan header continuations behavior.</param>
    public void SetAllowOrphanHeaderContinuations(bool value)
    {
        allowOrphanHeaderContinuations = value;
    }

    /// <summary>
    /// Gets the allow broken MIME boundaries.
    /// </summary>
    /// <returns><c>true</c> when the allow broken MIME boundaries setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowBrokenMimeBoundaries()
    {
        return allowBrokenMimeBoundaries;
    }

    /// <summary>
    /// Sets the allow broken MIME boundaries used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow broken MIME boundaries behavior.</param>
    public void SetAllowBrokenMimeBoundaries(bool value)
    {
        allowBrokenMimeBoundaries = value;
    }

    /// <summary>
    /// Gets whether Outlook deviations may be recovered.
    /// </summary>
    /// <returns><c>true</c> when Outlook compatibility recovery is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowOutlookDeviations()
    {
        return allowOutlookDeviations;
    }

    /// <summary>
    /// Sets whether Outlook deviations may be recovered.
    /// </summary>
    /// <param name = "value">Whether to enable Outlook compatibility recovery.</param>
    public void SetAllowOutlookDeviations(bool value)
    {
        allowOutlookDeviations = value;
    }

    /// <summary>
    /// Gets whether Exchange deviations may be recovered.
    /// </summary>
    /// <returns><c>true</c> when Exchange compatibility recovery is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowExchangeDeviations()
    {
        return allowExchangeDeviations;
    }

    /// <summary>
    /// Sets whether Exchange deviations may be recovered.
    /// </summary>
    /// <param name = "value">Whether to enable Exchange compatibility recovery.</param>
    public void SetAllowExchangeDeviations(bool value)
    {
        allowExchangeDeviations = value;
    }

    /// <summary>
    /// Gets whether Gmail deviations may be recovered.
    /// </summary>
    /// <returns><c>true</c> when Gmail compatibility recovery is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowGmailDeviations()
    {
        return allowGmailDeviations;
    }

    /// <summary>
    /// Sets whether Gmail deviations may be recovered.
    /// </summary>
    /// <param name = "value">Whether to enable Gmail compatibility recovery.</param>
    public void SetAllowGmailDeviations(bool value)
    {
        allowGmailDeviations = value;
    }

    /// <summary>
    /// Gets whether common nonstandard MIME type aliases may be normalized.
    /// </summary>
    /// <returns><c>true</c> when common MIME type alias recovery is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowCommonMimeTypeAliases()
    {
        return allowCommonMimeTypeAliases;
    }

    /// <summary>
    /// Sets whether common nonstandard MIME type aliases may be normalized.
    /// </summary>
    /// <param name = "value">Whether to enable common MIME type alias recovery.</param>
    public void SetAllowCommonMimeTypeAliases(bool value)
    {
        allowCommonMimeTypeAliases = value;
    }

    /// <summary>
    /// Gets whether broken encoded words may be decoded tolerantly.
    /// </summary>
    /// <returns><c>true</c> when broken encoded-word recovery is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowBrokenEncodedWords()
    {
        return allowBrokenEncodedWords;
    }

    /// <summary>
    /// Sets whether broken encoded words may be decoded tolerantly.
    /// </summary>
    /// <param name = "value">Whether to enable broken encoded-word recovery.</param>
    public void SetAllowBrokenEncodedWords(bool value)
    {
        allowBrokenEncodedWords = value;
    }

    /// <summary>
    /// Gets whether broken RFC 2231 parameters may produce decoded values.
    /// </summary>
    /// <returns><c>true</c> when broken RFC 2231 parameter recovery is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowBrokenRfc2231Parameters()
    {
        return allowBrokenRfc2231Parameters;
    }

    /// <summary>
    /// Sets whether broken RFC 2231 parameters may produce decoded values.
    /// </summary>
    /// <param name = "value">Whether to enable broken RFC 2231 parameter recovery.</param>
    public void SetAllowBrokenRfc2231Parameters(bool value)
    {
        allowBrokenRfc2231Parameters = value;
    }

    /// <summary>
    /// Gets whether legacy attachment metadata may be recovered.
    /// </summary>
    /// <returns><c>true</c> when legacy attachment recovery is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowLegacyAttachmentRecovery()
    {
        return allowLegacyAttachmentRecovery;
    }

    /// <summary>
    /// Sets whether legacy attachment metadata may be recovered.
    /// </summary>
    /// <param name = "value">Whether to enable legacy attachment recovery.</param>
    public void SetAllowLegacyAttachmentRecovery(bool value)
    {
        allowLegacyAttachmentRecovery = value;
    }

    /// <summary>
    /// Gets the allow truncated input.
    /// </summary>
    /// <returns><c>true</c> when the allow truncated input setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowTruncatedInput()
    {
        return allowTruncatedInput;
    }

    /// <summary>
    /// Sets the allow truncated input used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow truncated input behavior.</param>
    public void SetAllowTruncatedInput(bool value)
    {
        allowTruncatedInput = value;
    }

    /// <summary>
    /// Gets the allow duplicate singleton headers.
    /// </summary>
    /// <returns><c>true</c> when the allow duplicate singleton headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowDuplicateSingletonHeaders()
    {
        return allowDuplicateSingletonHeaders;
    }

    /// <summary>
    /// Sets the allow duplicate singleton headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow duplicate singleton headers behavior.</param>
    public void SetAllowDuplicateSingletonHeaders(bool value)
    {
        allowDuplicateSingletonHeaders = value;
    }

    /// <summary>
    /// Gets the return partial results.
    /// </summary>
    /// <returns><c>true</c> when the return partial results setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetReturnPartialResults()
    {
        return returnPartialResults;
    }

    /// <summary>
    /// Sets the return partial results used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the return partial results behavior.</param>
    public void SetReturnPartialResults(bool value)
    {
        returnPartialResults = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailCompatibilityOptions Clone()
    {
        EmailCompatibilityOptions copy = new EmailCompatibilityOptions();
        copy.SetAllowBareLineEndings(allowBareLineEndings);
        copy.SetAllowMissingHeaderBodySeparator(allowMissingHeaderBodySeparator);
        copy.SetAllowMalformedHeaderNames(allowMalformedHeaderNames);
        copy.SetAllowOrphanHeaderContinuations(allowOrphanHeaderContinuations);
        copy.SetAllowBrokenMimeBoundaries(allowBrokenMimeBoundaries);
        copy.SetAllowOutlookDeviations(allowOutlookDeviations);
        copy.SetAllowExchangeDeviations(allowExchangeDeviations);
        copy.SetAllowGmailDeviations(allowGmailDeviations);
        copy.SetAllowCommonMimeTypeAliases(allowCommonMimeTypeAliases);
        copy.SetAllowBrokenEncodedWords(allowBrokenEncodedWords);
        copy.SetAllowBrokenRfc2231Parameters(allowBrokenRfc2231Parameters);
        copy.SetAllowLegacyAttachmentRecovery(allowLegacyAttachmentRecovery);
        copy.SetAllowTruncatedInput(allowTruncatedInput);
        copy.SetAllowDuplicateSingletonHeaders(allowDuplicateSingletonHeaders);
        copy.SetReturnPartialResults(returnPartialResults);
        return copy;
    }
}
