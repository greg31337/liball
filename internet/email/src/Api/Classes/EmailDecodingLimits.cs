namespace email;
/// <summary>
/// Represents a decoding limits.
/// </summary>
public class EmailDecodingLimits : IEmailDecodingLimitsContract
{
    /// <summary>
    /// The default max decoded bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxDecodedBytes = 104857600;
    /// <summary>
    /// The default max decompressed bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaxDecompressedBytes = 104857600;
    /// <summary>
    /// The default max expansion ratio used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxExpansionRatio = 100;
    /// <summary>
    /// The default max text characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxTextCharacters = 10000000;
    /// <summary>
    /// The default max charset name length used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaxCharsetNameLength = 128;
    long maxDecodedBytes;
    long maxDecompressedBytes;
    int maxExpansionRatio;
    int maxTextCharacters;
    int maxCharsetNameLength;
    /// <summary>
    /// Creates a new decoding limits.
    /// </summary>
    public EmailDecodingLimits()
    {
        maxDecodedBytes = DefaultMaxDecodedBytes;
        maxDecompressedBytes = DefaultMaxDecompressedBytes;
        maxExpansionRatio = DefaultMaxExpansionRatio;
        maxTextCharacters = DefaultMaxTextCharacters;
        maxCharsetNameLength = DefaultMaxCharsetNameLength;
    }

    /// <summary>
    /// Gets the max decoded bytes.
    /// </summary>
    /// <returns>The current max decoded bytes.</returns>
    public long GetMaxDecodedBytes()
    {
        return maxDecodedBytes;
    }

    /// <summary>
    /// Sets the max decoded bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxDecodedBytes(long value)
    {
        maxDecodedBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max decompressed bytes.
    /// </summary>
    /// <returns>The current max decompressed bytes.</returns>
    public long GetMaxDecompressedBytes()
    {
        return maxDecompressedBytes;
    }

    /// <summary>
    /// Sets the max decompressed bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxDecompressedBytes(long value)
    {
        maxDecompressedBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the max expansion ratio.
    /// </summary>
    /// <returns>The current max expansion ratio.</returns>
    public int GetMaxExpansionRatio()
    {
        return maxExpansionRatio;
    }

    /// <summary>
    /// Sets the max expansion ratio used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxExpansionRatio(int value)
    {
        maxExpansionRatio = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max text characters.
    /// </summary>
    /// <returns>The current max text characters.</returns>
    public int GetMaxTextCharacters()
    {
        return maxTextCharacters;
    }

    /// <summary>
    /// Sets the max text characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxTextCharacters(int value)
    {
        maxTextCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the max charset name length.
    /// </summary>
    /// <returns>The current max charset name length.</returns>
    public int GetMaxCharsetNameLength()
    {
        return maxCharsetNameLength;
    }

    /// <summary>
    /// Sets the max charset name length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaxCharsetNameLength(int value)
    {
        maxCharsetNameLength = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailDecodingLimits Clone()
    {
        EmailDecodingLimits copy = new EmailDecodingLimits();
        copy.SetMaxDecodedBytes(maxDecodedBytes);
        copy.SetMaxDecompressedBytes(maxDecompressedBytes);
        copy.SetMaxExpansionRatio(maxExpansionRatio);
        copy.SetMaxTextCharacters(maxTextCharacters);
        copy.SetMaxCharsetNameLength(maxCharsetNameLength);
        return copy;
    }
}
