using System;

namespace email;
/// <summary>
/// Controls how write operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailWriteOptions : IEmailWriteOptionsContract
{
    EmailWriteMode mode;
    EmailTransferEncodingConstraint transportConstraint;
    EmailHeaderWriteOptions headerWriteOptions;
    bool deterministicOutput;
    bool includeBlindRecipientHeaders;
    byte[] deterministicSeed;
    long maximumOutputBytes;
    int maximumNestingDepth;
    int maximumPartCount;
    /// <summary>
    /// Creates a new write options object.
    /// </summary>
    public EmailWriteOptions()
    {
        mode = EmailWriteMode.MinimalRegeneration;
        transportConstraint = EmailTransferEncodingConstraint.SevenBit;
        headerWriteOptions = new EmailHeaderWriteOptions();
        deterministicOutput = false;
        includeBlindRecipientHeaders = false;
        deterministicSeed = new byte[0];
        maximumOutputBytes = EmailParsingLimitsConstants.DefaultMaxMessageBytes;
        maximumNestingDepth = EmailParsingLimitsConstants.DefaultMaxNestingDepth;
        maximumPartCount = EmailParsingLimitsConstants.DefaultMaxPartCount;
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailWriteMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMode(EmailWriteMode value)
    {
        if (value < EmailWriteMode.ExactPreservation || value > EmailWriteMode.Canonical)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        mode = value;
    }

    /// <summary>
    /// Gets the transport constraint.
    /// </summary>
    /// <returns>The current transport constraint.</returns>
    public EmailTransferEncodingConstraint GetTransportConstraint()
    {
        return transportConstraint;
    }

    /// <summary>
    /// Sets the transport constraint used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetTransportConstraint(EmailTransferEncodingConstraint value)
    {
        if (value < EmailTransferEncodingConstraint.SevenBit || value > EmailTransferEncodingConstraint.Binary)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        transportConstraint = value;
    }

    /// <summary>
    /// Gets the allow smtp UTF-8.
    /// </summary>
    /// <returns><c>true</c> when the allow smtp UTF-8 setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowSmtpUtf8()
    {
        return headerWriteOptions.GetAllowInternationalized();
    }

    /// <summary>
    /// Sets the allow smtp UTF-8 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow smtp UTF-8 behavior.</param>
    public void SetAllowSmtpUtf8(bool value)
    {
        headerWriteOptions.SetAllowInternationalized(value);
    }

    /// <summary>
    /// Gets the header write options.
    /// </summary>
    /// <returns>The current header write options.</returns>
    public EmailHeaderWriteOptions GetHeaderWriteOptions()
    {
        return headerWriteOptions.Clone();
    }

    /// <summary>
    /// Sets the header write options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetHeaderWriteOptions(EmailHeaderWriteOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        headerWriteOptions = value.Clone();
    }

    /// <summary>
    /// Gets the deterministic output.
    /// </summary>
    /// <returns><c>true</c> when the deterministic output setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetDeterministicOutput()
    {
        return deterministicOutput;
    }

    /// <summary>
    /// Sets the deterministic output used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the deterministic output behavior.</param>
    public void SetDeterministicOutput(bool value)
    {
        deterministicOutput = value;
    }

    /// <summary>
    /// Gets the deterministic seed.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current deterministic seed.</returns>
    public byte[] GetDeterministicSeed()
    {
        byte[] copy = new byte[deterministicSeed.Length];
        Array.Copy(deterministicSeed, copy, deterministicSeed.Length);
        return copy;
    }

    /// <summary>
    /// Sets the deterministic seed used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetDeterministicSeed(byte[] value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        deterministicSeed = new byte[value.Length];
        Array.Copy(value, deterministicSeed, value.Length);
    }

    /// <summary>
    /// Gets the include blind recipient headers.
    /// </summary>
    /// <returns><c>true</c> when the include blind recipient headers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeBlindRecipientHeaders()
    {
        return includeBlindRecipientHeaders;
    }

    /// <summary>
    /// Sets the include blind recipient headers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include blind recipient headers behavior.</param>
    public void SetIncludeBlindRecipientHeaders(bool value)
    {
        includeBlindRecipientHeaders = value;
    }

    /// <summary>
    /// Gets the maximum output bytes.
    /// </summary>
    /// <returns>The current maximum output bytes.</returns>
    public long GetMaximumOutputBytes()
    {
        return maximumOutputBytes;
    }

    /// <summary>
    /// Sets the maximum output bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumOutputBytes(long value)
    {
        maximumOutputBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum nesting depth.
    /// </summary>
    /// <returns>The current maximum nesting depth.</returns>
    public int GetMaximumNestingDepth()
    {
        return maximumNestingDepth;
    }

    /// <summary>
    /// Sets the maximum nesting depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumNestingDepth(int value)
    {
        maximumNestingDepth = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum part count.
    /// </summary>
    /// <returns>The current maximum part count.</returns>
    public int GetMaximumPartCount()
    {
        return maximumPartCount;
    }

    /// <summary>
    /// Sets the maximum part count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumPartCount(int value)
    {
        maximumPartCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Determines whether the current value or state is exact preservation.
    /// </summary>
    /// <returns><c>true</c> when the value or state is exact preservation; otherwise, <c>false</c>.</returns>
    public bool IsExactPreservation()
    {
        return mode == EmailWriteMode.ExactPreservation;
    }

    /// <summary>
    /// Determines whether the current value or state is minimal regeneration.
    /// </summary>
    /// <returns><c>true</c> when the value or state is minimal regeneration; otherwise, <c>false</c>.</returns>
    public bool IsMinimalRegeneration()
    {
        return mode == EmailWriteMode.MinimalRegeneration;
    }

    /// <summary>
    /// Determines whether the current value or state is canonical.
    /// </summary>
    /// <returns><c>true</c> when the value or state is canonical; otherwise, <c>false</c>.</returns>
    public bool IsCanonical()
    {
        return mode == EmailWriteMode.Canonical;
    }

    /// <summary>
    /// Gets the header write mode.
    /// </summary>
    /// <returns>The current header write mode.</returns>
    public EmailHeaderWriteMode GetHeaderWriteMode()
    {
        if (mode == EmailWriteMode.ExactPreservation)
        {
            return EmailHeaderWriteMode.Raw;
        }

        return mode == EmailWriteMode.MinimalRegeneration ? EmailHeaderWriteMode.Preserving : EmailHeaderWriteMode.Canonical;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailWriteOptions Clone()
    {
        EmailWriteOptions copy = new EmailWriteOptions();
        copy.SetMode(mode);
        copy.SetTransportConstraint(transportConstraint);
        copy.SetHeaderWriteOptions(headerWriteOptions);
        copy.SetDeterministicOutput(deterministicOutput);
        copy.SetDeterministicSeed(deterministicSeed);
        copy.SetIncludeBlindRecipientHeaders(includeBlindRecipientHeaders);
        copy.SetMaximumOutputBytes(maximumOutputBytes);
        copy.SetMaximumNestingDepth(maximumNestingDepth);
        copy.SetMaximumPartCount(maximumPartCount);
        return copy;
    }

    internal static EmailWriteOptions CreateExactPreservationImplementation()
    {
        EmailWriteOptions options = new EmailWriteOptions();
        options.SetMode(EmailWriteMode.ExactPreservation);
        options.SetIncludeBlindRecipientHeaders(true);
        return options;
    }

    internal static EmailWriteOptions CreateMinimalRegenerationImplementation()
    {
        return new EmailWriteOptions();
    }

    internal static EmailWriteOptions CreatePreservingImplementation()
    {
        return CreateMinimalRegenerationImplementation();
    }

    internal static EmailWriteOptions CreateCanonicalInternetImplementation()
    {
        EmailWriteOptions options = new EmailWriteOptions();
        options.SetMode(EmailWriteMode.Canonical);
        options.SetAllowSmtpUtf8(false);
        options.SetTransportConstraint(EmailTransferEncodingConstraint.SevenBit);
        return options;
    }

    internal static EmailWriteOptions CreateCanonicalSmtpUtf8Implementation()
    {
        EmailWriteOptions options = new EmailWriteOptions();
        options.SetMode(EmailWriteMode.Canonical);
        options.SetAllowSmtpUtf8(true);
        options.SetTransportConstraint(EmailTransferEncodingConstraint.EightBit);
        return options;
    }
}
