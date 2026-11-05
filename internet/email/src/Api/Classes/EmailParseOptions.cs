using System;

namespace email;
/// <summary>
/// Controls how parse operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailParseOptions : IEmailParseOptionsContract
{
    /// <summary>
    /// The untrusted max message bytes value exposed by this API.
    /// </summary>
    internal const long UntrustedMaxMessageBytes = 10485760;
    /// <summary>
    /// The untrusted max header bytes value exposed by this API.
    /// </summary>
    internal const long UntrustedMaxHeaderBytes = 65536;
    /// <summary>
    /// The untrusted max header field count value exposed by this API.
    /// </summary>
    internal const int UntrustedMaxHeaderFieldCount = 200;
    /// <summary>
    /// The untrusted max line bytes value exposed by this API.
    /// </summary>
    internal const int UntrustedMaxLineBytes = 16384;
    /// <summary>
    /// The untrusted max nesting depth value exposed by this API.
    /// </summary>
    internal const int UntrustedMaxNestingDepth = 15;
    /// <summary>
    /// The untrusted max part count value exposed by this API.
    /// </summary>
    internal const int UntrustedMaxPartCount = 250;
    /// <summary>
    /// The untrusted max parameters per header value exposed by this API.
    /// </summary>
    internal const int UntrustedMaxParametersPerHeader = 50;
    /// <summary>
    /// The untrusted max addresses per header value exposed by this API.
    /// </summary>
    internal const int UntrustedMaxAddressesPerHeader = 200;
    /// <summary>
    /// The untrusted max encoded words per header value exposed by this API.
    /// </summary>
    internal const int UntrustedMaxEncodedWordsPerHeader = 100;
    /// <summary>
    /// The untrusted max decoded bytes value exposed by this API.
    /// </summary>
    internal const long UntrustedMaxDecodedBytes = 20971520;
    /// <summary>
    /// The untrusted max expansion ratio value exposed by this API.
    /// </summary>
    internal const int UntrustedMaxExpansionRatio = 20;
    /// <summary>
    /// The untrusted memory threshold bytes value exposed by this API.
    /// </summary>
    internal const long UntrustedMemoryThresholdBytes = 262144;
    /// <summary>
    /// The untrusted max temporary bytes value exposed by this API.
    /// </summary>
    internal const long UntrustedMaxTemporaryBytes = 20971520;
    /// <summary>
    /// The untrusted max temporary file count value exposed by this API.
    /// </summary>
    internal const int UntrustedMaxTemporaryFileCount = 16;
    /// <summary>
    /// The untrusted max open stream count value exposed by this API.
    /// </summary>
    internal const int UntrustedMaxOpenStreamCount = 16;
    /// <summary>
    /// The untrusted max materialized bytes value exposed by this API.
    /// </summary>
    internal const long UntrustedMaxMaterializedBytes = 10485760;
    EmailParseMode mode;
    EmailLimits limits;
    EmailStorageOptions storageOptions;
    EmailBodyIngestOptions bodyIngestOptions;
    EmailCompatibilityOptions compatibilityOptions;
    bool preserveSource;
    bool allowSmtpUtf8;
    bool exchangeCompatibilityContext;
    bool gmailCompatibilityContext;
    /// <summary>
    /// Creates a new parse options object.
    /// </summary>
    public EmailParseOptions()
    {
        mode = EmailParseMode.ConformingTolerant;
        limits = new EmailLimits();
        storageOptions = new EmailStorageOptions();
        bodyIngestOptions = new EmailBodyIngestOptions();
        compatibilityOptions = CreateTolerantCompatibility();
        preserveSource = true;
        allowSmtpUtf8 = false;
        exchangeCompatibilityContext = false;
        gmailCompatibilityContext = false;
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailParseMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMode(EmailParseMode value)
    {
        if (value < EmailParseMode.Strict || value > EmailParseMode.Forensic)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        mode = value;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        limits = value.Clone();
    }

    /// <summary>
    /// Gets the storage options.
    /// </summary>
    /// <returns>The current storage options.</returns>
    public EmailStorageOptions GetStorageOptions()
    {
        return storageOptions.Clone();
    }

    /// <summary>
    /// Sets the storage options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetStorageOptions(EmailStorageOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        storageOptions = value.Clone();
    }

    /// <summary>
    /// Gets the compatibility options.
    /// </summary>
    /// <returns>The current compatibility options.</returns>
    public EmailCompatibilityOptions GetCompatibilityOptions()
    {
        return compatibilityOptions.Clone();
    }

    /// <summary>
    /// Gets the body ingest options.
    /// </summary>
    /// <returns>The current body ingest options.</returns>
    public EmailBodyIngestOptions GetBodyIngestOptions()
    {
        return bodyIngestOptions.Clone();
    }

    /// <summary>
    /// Sets the body ingest options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetBodyIngestOptions(EmailBodyIngestOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        bodyIngestOptions = value.Clone();
    }

    /// <summary>
    /// Sets the compatibility options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetCompatibilityOptions(EmailCompatibilityOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        compatibilityOptions = value.Clone();
    }

    /// <summary>
    /// Gets the preserve source.
    /// </summary>
    /// <returns><c>true</c> when the preserve source setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetPreserveSource()
    {
        return preserveSource;
    }

    /// <summary>
    /// Sets the preserve source used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the preserve source behavior.</param>
    public void SetPreserveSource(bool value)
    {
        preserveSource = value;
    }

    /// <summary>
    /// Gets the allow smtp UTF-8.
    /// </summary>
    /// <returns><c>true</c> when the allow smtp UTF-8 setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowSmtpUtf8()
    {
        return allowSmtpUtf8;
    }

    /// <summary>
    /// Sets the allow smtp UTF-8 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow smtp UTF-8 behavior.</param>
    public void SetAllowSmtpUtf8(bool value)
    {
        allowSmtpUtf8 = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailParseOptions Clone()
    {
        EmailParseOptions copy = new EmailParseOptions();
        copy.SetMode(mode);
        copy.SetLimits(limits);
        copy.SetStorageOptions(storageOptions);
        copy.SetBodyIngestOptions(bodyIngestOptions);
        copy.SetCompatibilityOptions(compatibilityOptions);
        copy.SetPreserveSource(preserveSource);
        copy.SetAllowSmtpUtf8(allowSmtpUtf8);
        copy.SetExchangeCompatibilityContext(exchangeCompatibilityContext);
        copy.SetGmailCompatibilityContext(gmailCompatibilityContext);
        return copy;
    }

    internal bool GetExchangeCompatibilityContext()
    {
        return exchangeCompatibilityContext;
    }

    internal void SetExchangeCompatibilityContext(bool value)
    {
        exchangeCompatibilityContext = value;
    }

    internal bool GetGmailCompatibilityContext()
    {
        return gmailCompatibilityContext;
    }

    internal void SetGmailCompatibilityContext(bool value)
    {
        gmailCompatibilityContext = value;
    }

    internal static EmailParseOptions CreateStrictImplementation()
    {
        EmailParseOptions options = new EmailParseOptions();
        options.SetMode(EmailParseMode.Strict);
        options.SetCompatibilityOptions(CreateStrictCompatibility());
        options.SetPreserveSource(false);
        options.SetAllowSmtpUtf8(false);
        return options;
    }

    internal static EmailParseOptions CreateConformingTolerantImplementation()
    {
        EmailParseOptions options = new EmailParseOptions();
        options.SetMode(EmailParseMode.ConformingTolerant);
        options.SetCompatibilityOptions(CreateTolerantCompatibility());
        options.SetPreserveSource(false);
        options.SetAllowSmtpUtf8(false);
        return options;
    }

    internal static EmailParseOptions CreateForensicImplementation()
    {
        EmailParseOptions options = new EmailParseOptions();
        options.SetMode(EmailParseMode.Forensic);
        options.SetCompatibilityOptions(CreateForensicCompatibility());
        options.SetPreserveSource(true);
        options.SetAllowSmtpUtf8(true);
        return options;
    }

    internal static EmailParseOptions CreatePreservingImplementation()
    {
        EmailParseOptions options = CreateConformingTolerantImplementation();
        options.SetPreserveSource(true);
        return options;
    }

    internal static EmailParseOptions CreateSmtpUtf8Implementation()
    {
        EmailParseOptions options = CreateConformingTolerantImplementation();
        options.SetAllowSmtpUtf8(true);
        return options;
    }

    internal static EmailParseOptions CreateBoundedUntrustedImplementation()
    {
        EmailParseOptions options = CreateConformingTolerantImplementation();
        EmailLimits boundedLimits = new EmailLimits();
        EmailParsingLimits parsing = boundedLimits.GetParsingLimits();
        parsing.SetMaxMessageBytes(UntrustedMaxMessageBytes);
        parsing.SetMaxHeaderBytes(UntrustedMaxHeaderBytes);
        parsing.SetMaxHeaderFieldCount(UntrustedMaxHeaderFieldCount);
        parsing.SetMaxLineBytes(UntrustedMaxLineBytes);
        parsing.SetMaxNestingDepth(UntrustedMaxNestingDepth);
        parsing.SetMaxPartCount(UntrustedMaxPartCount);
        parsing.SetMaxParametersPerHeader(UntrustedMaxParametersPerHeader);
        parsing.SetMaxAddressesPerHeader(UntrustedMaxAddressesPerHeader);
        parsing.SetMaxEncodedWordsPerHeader(UntrustedMaxEncodedWordsPerHeader);
        EmailDecodingLimits decoding = boundedLimits.GetDecodingLimits();
        decoding.SetMaxDecodedBytes(UntrustedMaxDecodedBytes);
        decoding.SetMaxDecompressedBytes(UntrustedMaxDecodedBytes);
        decoding.SetMaxExpansionRatio(UntrustedMaxExpansionRatio);
        EmailTemporaryStorageLimits temporary = boundedLimits.GetTemporaryStorageLimits();
        ApplyUntrustedTemporaryLimits(temporary);
        options.SetLimits(boundedLimits);
        EmailStorageOptions storage = new EmailStorageOptions();
        EmailTemporaryStorageLimits storageLimits = storage.GetLimits();
        ApplyUntrustedTemporaryLimits(storageLimits);
        options.SetStorageOptions(storage);
        options.SetPreserveSource(false);
        options.SetAllowSmtpUtf8(false);
        return options;
    }

    static EmailCompatibilityOptions CreateStrictCompatibility()
    {
        EmailCompatibilityOptions compatibility = new EmailCompatibilityOptions();
        compatibility.SetAllowBareLineEndings(false);
        compatibility.SetAllowMissingHeaderBodySeparator(false);
        compatibility.SetAllowMalformedHeaderNames(false);
        compatibility.SetAllowOrphanHeaderContinuations(false);
        compatibility.SetAllowBrokenMimeBoundaries(false);
        compatibility.SetAllowOutlookDeviations(false);
        compatibility.SetAllowExchangeDeviations(false);
        compatibility.SetAllowGmailDeviations(false);
        compatibility.SetAllowCommonMimeTypeAliases(false);
        compatibility.SetAllowBrokenEncodedWords(false);
        compatibility.SetAllowBrokenRfc2231Parameters(false);
        compatibility.SetAllowLegacyAttachmentRecovery(false);
        compatibility.SetAllowTruncatedInput(false);
        compatibility.SetAllowDuplicateSingletonHeaders(false);
        compatibility.SetReturnPartialResults(false);
        return compatibility;
    }

    static EmailCompatibilityOptions CreateTolerantCompatibility()
    {
        EmailCompatibilityOptions compatibility = new EmailCompatibilityOptions();
        compatibility.SetAllowBareLineEndings(true);
        compatibility.SetAllowMissingHeaderBodySeparator(true);
        compatibility.SetAllowMalformedHeaderNames(true);
        compatibility.SetAllowOrphanHeaderContinuations(true);
        compatibility.SetAllowBrokenMimeBoundaries(false);
        compatibility.SetAllowOutlookDeviations(true);
        compatibility.SetAllowExchangeDeviations(true);
        compatibility.SetAllowGmailDeviations(true);
        compatibility.SetAllowCommonMimeTypeAliases(true);
        compatibility.SetAllowBrokenEncodedWords(true);
        compatibility.SetAllowBrokenRfc2231Parameters(true);
        compatibility.SetAllowLegacyAttachmentRecovery(true);
        compatibility.SetAllowTruncatedInput(true);
        compatibility.SetAllowDuplicateSingletonHeaders(true);
        compatibility.SetReturnPartialResults(true);
        return compatibility;
    }

    static EmailCompatibilityOptions CreateForensicCompatibility()
    {
        EmailCompatibilityOptions compatibility = CreateTolerantCompatibility();
        compatibility.SetAllowBrokenMimeBoundaries(true);
        return compatibility;
    }

    static void ApplyUntrustedTemporaryLimits(EmailTemporaryStorageLimits limits)
    {
        limits.SetMemoryThresholdBytes(UntrustedMemoryThresholdBytes);
        limits.SetMaxTemporaryBytes(UntrustedMaxTemporaryBytes);
        limits.SetMaxTemporaryFileCount(UntrustedMaxTemporaryFileCount);
        limits.SetMaxOpenStreamCount(UntrustedMaxOpenStreamCount);
        limits.SetMaxMaterializedBytes(UntrustedMaxMaterializedBytes);
    }
}
