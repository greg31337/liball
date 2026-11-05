namespace email;
/// <summary>
/// Represents a parsing limits.
/// </summary>
public class EmailParsingLimits : IEmailParsingLimitsContract
{
    /// <summary>
    /// The default max message bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxMessageBytes = 104857600;
    /// <summary>
    /// The default max header bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxHeaderBytes = 262144;
    /// <summary>
    /// The default max header field count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxHeaderFieldCount = 1000;
    /// <summary>
    /// The default max line bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxLineBytes = 65536;
    /// <summary>
    /// The default max nesting depth used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxNestingDepth = 30;
    /// <summary>
    /// The default max part count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxPartCount = 1000;
    /// <summary>
    /// The default max parameters per header used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxParametersPerHeader = 100;
    /// <summary>
    /// The default max addresses per header used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxAddressesPerHeader = 1000;
    /// <summary>
    /// The default max encoded words per header used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxEncodedWordsPerHeader = 256;
    long maxMessageBytes;
    long maxHeaderBytes;
    int maxHeaderFieldCount;
    int maxLineBytes;
    int maxNestingDepth;
    int maxPartCount;
    int maxParametersPerHeader;
    int maxAddressesPerHeader;
    int maxEncodedWordsPerHeader;
    /// <summary>
    /// Creates a new parsing limits.
    /// </summary>
    public EmailParsingLimits()
    {
        maxMessageBytes = DefaultMaxMessageBytes;
        maxHeaderBytes = DefaultMaxHeaderBytes;
        maxHeaderFieldCount = DefaultMaxHeaderFieldCount;
        maxLineBytes = DefaultMaxLineBytes;
        maxNestingDepth = DefaultMaxNestingDepth;
        maxPartCount = DefaultMaxPartCount;
        maxParametersPerHeader = DefaultMaxParametersPerHeader;
        maxAddressesPerHeader = DefaultMaxAddressesPerHeader;
        maxEncodedWordsPerHeader = DefaultMaxEncodedWordsPerHeader;
    }

    /// <summary>
    /// Gets the max message bytes.
    /// </summary>
    /// <returns>The current max message bytes.</returns>
    public long GetMaxMessageBytes()
    {
        return maxMessageBytes;
    }

    /// <summary>
    /// Sets the max message bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxMessageBytes(long value)
    {
        maxMessageBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max header bytes.
    /// </summary>
    /// <returns>The current max header bytes.</returns>
    public long GetMaxHeaderBytes()
    {
        return maxHeaderBytes;
    }

    /// <summary>
    /// Sets the max header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxHeaderBytes(long value)
    {
        maxHeaderBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max header field count.
    /// </summary>
    /// <returns>The current max header field count.</returns>
    public int GetMaxHeaderFieldCount()
    {
        return maxHeaderFieldCount;
    }

    /// <summary>
    /// Sets the max header field count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxHeaderFieldCount(int value)
    {
        maxHeaderFieldCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max line bytes.
    /// </summary>
    /// <returns>The current max line bytes.</returns>
    public int GetMaxLineBytes()
    {
        return maxLineBytes;
    }

    /// <summary>
    /// Sets the max line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxLineBytes(int value)
    {
        maxLineBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max nesting depth.
    /// </summary>
    /// <returns>The current max nesting depth.</returns>
    public int GetMaxNestingDepth()
    {
        return maxNestingDepth;
    }

    /// <summary>
    /// Sets the max nesting depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxNestingDepth(int value)
    {
        maxNestingDepth = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max part count.
    /// </summary>
    /// <returns>The current max part count.</returns>
    public int GetMaxPartCount()
    {
        return maxPartCount;
    }

    /// <summary>
    /// Sets the max part count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxPartCount(int value)
    {
        maxPartCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max parameters per header.
    /// </summary>
    /// <returns>The current max parameters per header.</returns>
    public int GetMaxParametersPerHeader()
    {
        return maxParametersPerHeader;
    }

    /// <summary>
    /// Sets the max parameters per header used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxParametersPerHeader(int value)
    {
        maxParametersPerHeader = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max addresses per header.
    /// </summary>
    /// <returns>The current max addresses per header.</returns>
    public int GetMaxAddressesPerHeader()
    {
        return maxAddressesPerHeader;
    }

    /// <summary>
    /// Sets the max addresses per header used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxAddressesPerHeader(int value)
    {
        maxAddressesPerHeader = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max encoded words per header.
    /// </summary>
    /// <returns>The current max encoded words per header.</returns>
    public int GetMaxEncodedWordsPerHeader()
    {
        return maxEncodedWordsPerHeader;
    }

    /// <summary>
    /// Sets the max encoded words per header used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxEncodedWordsPerHeader(int value)
    {
        maxEncodedWordsPerHeader = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailParsingLimits Clone()
    {
        EmailParsingLimits copy = new EmailParsingLimits();
        copy.SetMaxMessageBytes(maxMessageBytes);
        copy.SetMaxHeaderBytes(maxHeaderBytes);
        copy.SetMaxHeaderFieldCount(maxHeaderFieldCount);
        copy.SetMaxLineBytes(maxLineBytes);
        copy.SetMaxNestingDepth(maxNestingDepth);
        copy.SetMaxPartCount(maxPartCount);
        copy.SetMaxParametersPerHeader(maxParametersPerHeader);
        copy.SetMaxAddressesPerHeader(maxAddressesPerHeader);
        copy.SetMaxEncodedWordsPerHeader(maxEncodedWordsPerHeader);
        return copy;
    }
}
