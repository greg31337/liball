using System;

namespace email;
/// <summary>
/// Controls how thread operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailThreadOptions : IEmailThreadOptionsContract
{
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderCount = 10000;
    /// <summary>
    /// The default maximum identifier count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumIdentifierCount = 1000;
    /// <summary>
    /// The default maximum identifier bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumIdentifierBytes = 1048576;
    /// <summary>
    /// The default maximum subject bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSubjectBytes = 1048576;
    /// <summary>
    /// The default maximum subject characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSubjectCharacters = 10000;
    /// <summary>
    /// The default maximum graph message count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumGraphMessageCount = 10000;
    /// <summary>
    /// The default maximum graph identifier count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumGraphIdentifierCount = 100000;
    int maximumHeaderCount;
    int maximumIdentifierCount;
    int maximumIdentifierBytes;
    int maximumSubjectBytes;
    int maximumSubjectCharacters;
    int maximumGraphMessageCount;
    int maximumGraphIdentifierCount;
    bool allowRecoveredIdentifiers;
    bool enableSubjectFallback;
    /// <summary>
    /// Creates a new thread options object.
    /// </summary>
    public EmailThreadOptions()
    {
        maximumHeaderCount = DefaultMaximumHeaderCount;
        maximumIdentifierCount = DefaultMaximumIdentifierCount;
        maximumIdentifierBytes = DefaultMaximumIdentifierBytes;
        maximumSubjectBytes = DefaultMaximumSubjectBytes;
        maximumSubjectCharacters = DefaultMaximumSubjectCharacters;
        maximumGraphMessageCount = DefaultMaximumGraphMessageCount;
        maximumGraphIdentifierCount = DefaultMaximumGraphIdentifierCount;
        allowRecoveredIdentifiers = false;
        enableSubjectFallback = true;
    }

    /// <summary>
    /// Gets the maximum header count.
    /// </summary>
    /// <returns>The current maximum header count.</returns>
    public int GetMaximumHeaderCount()
    {
        return maximumHeaderCount;
    }

    /// <summary>
    /// Sets the maximum header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHeaderCount(int value)
    {
        maximumHeaderCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum identifier count.
    /// </summary>
    /// <returns>The current maximum identifier count.</returns>
    public int GetMaximumIdentifierCount()
    {
        return maximumIdentifierCount;
    }

    /// <summary>
    /// Sets the maximum identifier count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumIdentifierCount(int value)
    {
        maximumIdentifierCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum identifier bytes.
    /// </summary>
    /// <returns>The current maximum identifier bytes.</returns>
    public int GetMaximumIdentifierBytes()
    {
        return maximumIdentifierBytes;
    }

    /// <summary>
    /// Sets the maximum identifier bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumIdentifierBytes(int value)
    {
        maximumIdentifierBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum subject bytes.
    /// </summary>
    /// <returns>The current maximum subject bytes.</returns>
    public int GetMaximumSubjectBytes()
    {
        return maximumSubjectBytes;
    }

    /// <summary>
    /// Sets the maximum subject bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumSubjectBytes(int value)
    {
        maximumSubjectBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum subject characters.
    /// </summary>
    /// <returns>The current maximum subject characters.</returns>
    public int GetMaximumSubjectCharacters()
    {
        return maximumSubjectCharacters;
    }

    /// <summary>
    /// Sets the maximum subject characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumSubjectCharacters(int value)
    {
        maximumSubjectCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum graph message count.
    /// </summary>
    /// <returns>The current maximum graph message count.</returns>
    public int GetMaximumGraphMessageCount()
    {
        return maximumGraphMessageCount;
    }

    /// <summary>
    /// Sets the maximum graph message count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumGraphMessageCount(int value)
    {
        maximumGraphMessageCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum graph identifier count.
    /// </summary>
    /// <returns>The current maximum graph identifier count.</returns>
    public int GetMaximumGraphIdentifierCount()
    {
        return maximumGraphIdentifierCount;
    }

    /// <summary>
    /// Sets the maximum graph identifier count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumGraphIdentifierCount(int value)
    {
        maximumGraphIdentifierCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the allow recovered identifiers.
    /// </summary>
    /// <returns><c>true</c> when the allow recovered identifiers setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowRecoveredIdentifiers()
    {
        return allowRecoveredIdentifiers;
    }

    /// <summary>
    /// Sets the allow recovered identifiers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow recovered identifiers behavior.</param>
    public void SetAllowRecoveredIdentifiers(bool value)
    {
        allowRecoveredIdentifiers = value;
    }

    /// <summary>
    /// Gets the enable subject fallback.
    /// </summary>
    /// <returns><c>true</c> when the enable subject fallback setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableSubjectFallback()
    {
        return enableSubjectFallback;
    }

    /// <summary>
    /// Sets the enable subject fallback used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the enable subject fallback behavior.</param>
    public void SetEnableSubjectFallback(bool value)
    {
        enableSubjectFallback = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailThreadOptions Clone()
    {
        EmailThreadOptions copy = new EmailThreadOptions();
        copy.SetMaximumHeaderCount(maximumHeaderCount);
        copy.SetMaximumIdentifierCount(maximumIdentifierCount);
        copy.SetMaximumIdentifierBytes(maximumIdentifierBytes);
        copy.SetMaximumSubjectBytes(maximumSubjectBytes);
        copy.SetMaximumSubjectCharacters(maximumSubjectCharacters);
        copy.SetMaximumGraphMessageCount(maximumGraphMessageCount);
        copy.SetMaximumGraphIdentifierCount(maximumGraphIdentifierCount);
        copy.SetAllowRecoveredIdentifiers(allowRecoveredIdentifiers);
        copy.SetEnableSubjectFallback(enableSubjectFallback);
        return copy;
    }
}
