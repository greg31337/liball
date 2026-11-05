namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCompatibilityOptions"/>.
/// </summary>
public interface IEmailCompatibilityOptionsContract
{
    /// <summary>
    /// Gets the allow bare line endings.
    /// </summary>
    /// <returns><c>true</c> when the allow bare line endings setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowBareLineEndings();
    /// <summary>
    /// Sets the allow bare line endings used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow bare line endings behavior.</param>
    void SetAllowBareLineEndings(bool value);
    /// <summary>
    /// Gets the allow missing header body separator.
    /// </summary>
    /// <returns><c>true</c> when the allow missing header body separator setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowMissingHeaderBodySeparator();
    /// <summary>
    /// Sets the allow missing header body separator used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow missing header body separator behavior.</param>
    void SetAllowMissingHeaderBodySeparator(bool value);
    /// <summary>
    /// Gets the allow malformed header names.
    /// </summary>
    /// <returns><c>true</c> when the allow malformed header names setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowMalformedHeaderNames();
    /// <summary>
    /// Sets the allow malformed header names used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow malformed header names behavior.</param>
    void SetAllowMalformedHeaderNames(bool value);
    /// <summary>
    /// Gets the allow orphan header continuations.
    /// </summary>
    /// <returns><c>true</c> when the allow orphan header continuations setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowOrphanHeaderContinuations();
    /// <summary>
    /// Sets the allow orphan header continuations used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow orphan header continuations behavior.</param>
    void SetAllowOrphanHeaderContinuations(bool value);
    /// <summary>
    /// Gets the allow broken MIME boundaries.
    /// </summary>
    /// <returns><c>true</c> when the allow broken MIME boundaries setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowBrokenMimeBoundaries();
    /// <summary>
    /// Sets the allow broken MIME boundaries used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow broken MIME boundaries behavior.</param>
    void SetAllowBrokenMimeBoundaries(bool value);
    /// <summary>
    /// Gets whether Outlook deviations may be recovered.
    /// </summary>
    /// <returns><c>true</c> when Outlook compatibility recovery is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowOutlookDeviations();
    /// <summary>
    /// Sets whether Outlook deviations may be recovered.
    /// </summary>
    /// <param name = "value">Whether to enable Outlook compatibility recovery.</param>
    void SetAllowOutlookDeviations(bool value);
    /// <summary>
    /// Gets whether Exchange deviations may be recovered.
    /// </summary>
    /// <returns><c>true</c> when Exchange compatibility recovery is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowExchangeDeviations();
    /// <summary>
    /// Sets whether Exchange deviations may be recovered.
    /// </summary>
    /// <param name = "value">Whether to enable Exchange compatibility recovery.</param>
    void SetAllowExchangeDeviations(bool value);
    /// <summary>
    /// Gets whether Gmail deviations may be recovered.
    /// </summary>
    /// <returns><c>true</c> when Gmail compatibility recovery is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowGmailDeviations();
    /// <summary>
    /// Sets whether Gmail deviations may be recovered.
    /// </summary>
    /// <param name = "value">Whether to enable Gmail compatibility recovery.</param>
    void SetAllowGmailDeviations(bool value);
    /// <summary>
    /// Gets whether common nonstandard MIME type aliases may be normalized.
    /// </summary>
    /// <returns><c>true</c> when common MIME type alias recovery is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowCommonMimeTypeAliases();
    /// <summary>
    /// Sets whether common nonstandard MIME type aliases may be normalized.
    /// </summary>
    /// <param name = "value">Whether to enable common MIME type alias recovery.</param>
    void SetAllowCommonMimeTypeAliases(bool value);
    /// <summary>
    /// Gets whether broken encoded words may be decoded tolerantly.
    /// </summary>
    /// <returns><c>true</c> when broken encoded-word recovery is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowBrokenEncodedWords();
    /// <summary>
    /// Sets whether broken encoded words may be decoded tolerantly.
    /// </summary>
    /// <param name = "value">Whether to enable broken encoded-word recovery.</param>
    void SetAllowBrokenEncodedWords(bool value);
    /// <summary>
    /// Gets whether broken RFC 2231 parameters may produce decoded values.
    /// </summary>
    /// <returns><c>true</c> when broken RFC 2231 parameter recovery is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowBrokenRfc2231Parameters();
    /// <summary>
    /// Sets whether broken RFC 2231 parameters may produce decoded values.
    /// </summary>
    /// <param name = "value">Whether to enable broken RFC 2231 parameter recovery.</param>
    void SetAllowBrokenRfc2231Parameters(bool value);
    /// <summary>
    /// Gets whether legacy attachment metadata may be recovered.
    /// </summary>
    /// <returns><c>true</c> when legacy attachment recovery is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowLegacyAttachmentRecovery();
    /// <summary>
    /// Sets whether legacy attachment metadata may be recovered.
    /// </summary>
    /// <param name = "value">Whether to enable legacy attachment recovery.</param>
    void SetAllowLegacyAttachmentRecovery(bool value);
    /// <summary>
    /// Gets the allow truncated input.
    /// </summary>
    /// <returns><c>true</c> when the allow truncated input setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowTruncatedInput();
    /// <summary>
    /// Sets the allow truncated input used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow truncated input behavior.</param>
    void SetAllowTruncatedInput(bool value);
    /// <summary>
    /// Gets the allow duplicate singleton headers.
    /// </summary>
    /// <returns><c>true</c> when the allow duplicate singleton headers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowDuplicateSingletonHeaders();
    /// <summary>
    /// Sets the allow duplicate singleton headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow duplicate singleton headers behavior.</param>
    void SetAllowDuplicateSingletonHeaders(bool value);
    /// <summary>
    /// Gets the return partial results.
    /// </summary>
    /// <returns><c>true</c> when the return partial results setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetReturnPartialResults();
    /// <summary>
    /// Sets the return partial results used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the return partial results behavior.</param>
    void SetReturnPartialResults(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailCompatibilityOptions Clone();
}
