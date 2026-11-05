using System;

namespace email;
/// <summary>
/// Controls how TNEF reader operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailTnefReaderOptions : IEmailTnefReaderOptionsContract
{
    /// <summary>
    /// The default read buffer bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultReadBufferBytes = 8192;
    /// <summary>
    /// The default maximum property count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumPropertyCount = 10000;
    /// <summary>
    /// The default maximum property value count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumPropertyValueCount = 10000;
    /// <summary>
    /// The default maximum recipient count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumRecipientCount = 10000;
    /// <summary>
    /// The default maximum embedded depth used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumEmbeddedDepth = 16;
    /// <summary>
    /// The default maximum named property characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumNamedPropertyCharacters = 4096;
    EmailArchiveLimits limits;
    int readBufferBytes;
    bool requireValidChecksums;
    int maximumPropertyCount;
    int maximumPropertyValueCount;
    int maximumRecipientCount;
    int maximumEmbeddedDepth;
    int maximumNamedPropertyCharacters;
    long maximumDecodedRtfBytes;
    /// <summary>
    /// Creates a new TNEF reader options object.
    /// </summary>
    public EmailTnefReaderOptions()
    {
        limits = new EmailArchiveLimits();
        readBufferBytes = DefaultReadBufferBytes;
        requireValidChecksums = true;
        maximumPropertyCount = DefaultMaximumPropertyCount;
        maximumPropertyValueCount = DefaultMaximumPropertyValueCount;
        maximumRecipientCount = DefaultMaximumRecipientCount;
        maximumEmbeddedDepth = DefaultMaximumEmbeddedDepth;
        maximumNamedPropertyCharacters = DefaultMaximumNamedPropertyCharacters;
        maximumDecodedRtfBytes = EmailArchiveLimitsConstants.DefaultMaxEntryBytes;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailArchiveLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailArchiveLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the read buffer bytes.
    /// </summary>
    /// <returns>The current read buffer bytes.</returns>
    public int GetReadBufferBytes()
    {
        return readBufferBytes;
    }

    /// <summary>
    /// Sets the read buffer bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetReadBufferBytes(int value)
    {
        readBufferBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the require valid checksums.
    /// </summary>
    /// <returns><c>true</c> when the require valid checksums setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireValidChecksums()
    {
        return requireValidChecksums;
    }

    /// <summary>
    /// Sets the require valid checksums used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require valid checksums behavior.</param>
    public void SetRequireValidChecksums(bool value)
    {
        requireValidChecksums = value;
    }

    /// <summary>
    /// Gets the maximum property count.
    /// </summary>
    /// <returns>The current maximum property count.</returns>
    public int GetMaximumPropertyCount()
    {
        return maximumPropertyCount;
    }

    /// <summary>
    /// Sets the maximum property count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumPropertyCount(int value)
    {
        maximumPropertyCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum property value count.
    /// </summary>
    /// <returns>The current maximum property value count.</returns>
    public int GetMaximumPropertyValueCount()
    {
        return maximumPropertyValueCount;
    }

    /// <summary>
    /// Sets the maximum property value count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumPropertyValueCount(int value)
    {
        maximumPropertyValueCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum recipient count.
    /// </summary>
    /// <returns>The current maximum recipient count.</returns>
    public int GetMaximumRecipientCount()
    {
        return maximumRecipientCount;
    }

    /// <summary>
    /// Sets the maximum recipient count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumRecipientCount(int value)
    {
        maximumRecipientCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum embedded depth.
    /// </summary>
    /// <returns>The current maximum embedded depth.</returns>
    public int GetMaximumEmbeddedDepth()
    {
        return maximumEmbeddedDepth;
    }

    /// <summary>
    /// Sets the maximum embedded depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumEmbeddedDepth(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException("value");
        maximumEmbeddedDepth = value;
    }

    /// <summary>
    /// Gets the maximum named property characters.
    /// </summary>
    /// <returns>The current maximum named property characters.</returns>
    public int GetMaximumNamedPropertyCharacters()
    {
        return maximumNamedPropertyCharacters;
    }

    /// <summary>
    /// Sets the maximum named property characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumNamedPropertyCharacters(int value)
    {
        maximumNamedPropertyCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum decoded rtf bytes.
    /// </summary>
    /// <returns>The current maximum decoded rtf bytes.</returns>
    public long GetMaximumDecodedRtfBytes()
    {
        return maximumDecodedRtfBytes;
    }

    /// <summary>
    /// Sets the maximum decoded rtf bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumDecodedRtfBytes(long value)
    {
        maximumDecodedRtfBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailTnefReaderOptions Clone()
    {
        EmailTnefReaderOptions copy = new EmailTnefReaderOptions();
        copy.SetLimits(limits);
        copy.SetReadBufferBytes(readBufferBytes);
        copy.SetRequireValidChecksums(requireValidChecksums);
        copy.SetMaximumPropertyCount(maximumPropertyCount);
        copy.SetMaximumPropertyValueCount(maximumPropertyValueCount);
        copy.SetMaximumRecipientCount(maximumRecipientCount);
        copy.SetMaximumEmbeddedDepth(maximumEmbeddedDepth);
        copy.SetMaximumNamedPropertyCharacters(maximumNamedPropertyCharacters);
        copy.SetMaximumDecodedRtfBytes(maximumDecodedRtfBytes);
        return copy;
    }
}
